using System.Collections.Specialized;
using AutoOS.App.Data.Enums.Network;
using AutoOS.App.Data.Models.Network;
using Microsoft.UI.Xaml;
using Syncfusion.UI.Xaml.TreeGrid;

namespace AutoOS.App.Helpers.TreeGrid;

public sealed partial class NetworkTreeGrid : SfTreeGrid
{
	private const double AdapterRowHeight = 48;
	private const double SettingRowHeight = 32;
	private INotifyCollectionChanged? _nodes;

	public NetworkTreeGrid()
	{
		RowHeight = SettingRowHeight;
		Loaded += OnLoaded;
		Unloaded += OnUnloaded;
		ItemsSourceChanged += (_, _) => QueueRowHeightRefresh();
		NodeExpanded += (_, _) => QueueRowHeightRefresh();
		NodeCollapsed += (_, _) => QueueRowHeightRefresh();
		SortColumnsChanged += (_, _) => QueueRowHeightRefresh();
	}

	protected override void OnApplyTemplate()
	{
		base.OnApplyTemplate();
		QueueRowHeightRefresh();
	}

	public void QueueRowHeightRefresh() =>
		DispatcherQueue.TryEnqueue(RefreshRowHeights);

	private void OnLoaded(object sender, RoutedEventArgs e)
	{
		AttachToNodes();
		QueueRowHeightRefresh();
	}

	private void OnUnloaded(object sender, RoutedEventArgs e) => DetachFromNodes();

	private void OnNodesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => QueueRowHeightRefresh();

	private void AttachToNodes()
	{
		INotifyCollectionChanged? nodes = View?.Nodes as INotifyCollectionChanged;
		if (ReferenceEquals(_nodes, nodes))
			return;

		DetachFromNodes();
		_nodes = nodes;
		if (_nodes != null)
			_nodes.CollectionChanged += OnNodesCollectionChanged;
	}

	private void DetachFromNodes()
	{
		if (_nodes != null)
			_nodes.CollectionChanged -= OnNodesCollectionChanged;
		_nodes = null;
	}

	private void RefreshRowHeights()
	{
		AttachToNodes();
		TreeGridPanel? panel = TreeGridPanel;
		if (panel == null || View?.Nodes == null)
			return;

		foreach (TreeNode treeNode in EnumerateNodes(View.Nodes))
		{
			int rowIndex = this.ResolveToRowIndex(treeNode);
			if (rowIndex < 0 || rowIndex >= panel.RowCount)
				continue;

			Node? node = treeNode.Item as Node;
			if (node == null)
				continue;

			panel.RowHeights[rowIndex] = node.NodeKind == NodeKind.Adapter
				? AdapterRowHeight
				: SettingRowHeight;
		}

		panel.InvalidateMeasure();
	}

	private static IEnumerable<TreeNode> EnumerateNodes(IEnumerable<TreeNode> nodes)
	{
		foreach (TreeNode node in nodes)
		{
			yield return node;
			foreach (TreeNode child in EnumerateNodes(node.ChildNodes))
				yield return child;
		}
	}
}
