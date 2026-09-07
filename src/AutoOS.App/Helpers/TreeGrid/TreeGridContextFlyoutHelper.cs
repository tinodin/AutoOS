using System.Windows.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Syncfusion.UI.Xaml.Data;
using Syncfusion.UI.Xaml.Grids;
using Syncfusion.UI.Xaml.TreeGrid;
using Windows.Foundation;
using Microsoft.UI.Xaml.Media;
using Syncfusion.UI.Xaml.Grids.ScrollAxis;

namespace AutoOS.App.Helpers.TreeGrid;

public static class TreeGridContextFlyoutHelper
{
	public static void HandleCellContextRequested<TNode>(object sender, ContextRequestedEventArgs args, Func<TNode, string, IReadOnlyList<(string Text, string Value)>> getItems, ICommand copyTextCommand) where TNode : class
	{
		if (sender is not SfTreeGrid treeGrid)
			return;

		if (treeGrid.SelectionController.CurrentCellManager.CurrentCell?.IsEditing == true)
			return;

		if (args.TryGetPosition(treeGrid, out _))
		{
			TreeGridCell? cell = FindCell(args.OriginalSource as DependencyObject);
			if (cell?.DataContext is not TNode pressedNode)
				return;
			string? pressedMapping = cell.ColumnBase?.TreeGridColumn?.MappingName;
			if (pressedMapping == null)
				return;
			args.TryGetPosition(cell, out Point pointerPosition);
			ShowCellContextFlyout(getItems(pressedNode, pressedMapping), copyTextCommand, cell, pointerPosition);
			return;
		}

		TreeGridCurrentCellManager manager = treeGrid.SelectionController.CurrentCellManager;
		RowColumnIndex rowColumnIndex = manager.CurrentRowColumnIndex;
		if (rowColumnIndex.RowIndex < 0 || rowColumnIndex.ColumnIndex < 0)
			return;
		int visibleIndex = treeGrid.ResolveToGridVisibleColumnIndex(rowColumnIndex.ColumnIndex);
		if (visibleIndex < 0 || visibleIndex >= treeGrid.Columns.Count)
			return;
		string mappingName = treeGrid.Columns[visibleIndex].MappingName;
		TNode? node = treeGrid.GetNodeAtRowIndex(rowColumnIndex.RowIndex)?.Item as TNode ?? treeGrid.CurrentItem as TNode;
		if (node == null)
			return;
		FrameworkElement target = manager.CurrentCell?.Element as FrameworkElement ?? treeGrid;
		ShowCellContextFlyout(getItems(node, mappingName), copyTextCommand, target, null);
	}

	public static void ShowHeaderContextFlyout(object sender, TreeGridContextFlyoutEventArgs e)
	{
		if (sender is not SfTreeGrid treeGrid)
			return;

		e.ContextFlyout.Items.Clear();

		MenuFlyout flyout = e.ContextFlyout;
		TreeGridColumn column = treeGrid.Columns[treeGrid.ResolveToGridVisibleColumnIndex(e.RowColumnIndex.ColumnIndex)];

		bool isAscending = treeGrid.SortColumnDescriptions.Any(description => description.ColumnName == column.MappingName && description.SortDirection == SortDirection.Ascending);
		bool isDescending = treeGrid.SortColumnDescriptions.Any(description => description.ColumnName == column.MappingName && description.SortDirection == SortDirection.Descending);

		var ascending = new RadioMenuFlyoutItem
		{
			Text = "Sort Ascending",
			IsChecked = isAscending && !isDescending
		};
		ascending.Click += (_, _) => SetSort(treeGrid, column.MappingName, SortDirection.Ascending);
		flyout.Items.Add(ascending);

		var descending = new RadioMenuFlyoutItem
		{
			Text = "Sort Descending",
			IsChecked = isDescending
		};
		descending.Click += (_, _) => SetSort(treeGrid, column.MappingName, SortDirection.Descending);
		flyout.Items.Add(descending);

		flyout.Items.Add(new MenuFlyoutSeparator());

		var clear = new MenuFlyoutItem
		{
			Text = "Clear Sorting"
		};
		clear.Click += (_, _) => treeGrid.SortColumnDescriptions.Clear();
		flyout.Items.Add(clear);
	}

	public static void ShowCellContextFlyout(IReadOnlyList<(string Text, string Value)> items, ICommand copyTextCommand, FrameworkElement target, Point? position)
	{
		if (items.Count == 0)
			return;

		var flyout = new MenuFlyout();

		foreach ((string Text, string Value) in items)
		{
			flyout.Items.Add(new MenuFlyoutItem
			{
				Text = Text,
				Command = copyTextCommand,
				CommandParameter = Value,
				Icon = new FontIcon { Glyph = "\uE8C8" }
			});
		}

		if (position.HasValue)
		{
			flyout.ShowAt(target, new FlyoutShowOptions
			{
				Position = position.Value,
				Placement = FlyoutPlacementMode.Auto
			});
		}
		else
		{
			flyout.ShowAt(target, new FlyoutShowOptions
			{
				Placement = FlyoutPlacementMode.Auto,
				ShowMode = FlyoutShowMode.Transient
			});
		}
	}

	private static void SetSort(SfTreeGrid treeGrid, string mappingName, SortDirection direction)
	{
		treeGrid.SortColumnDescriptions.Clear();
		treeGrid.SortColumnDescriptions.Add(new SortColumnDescription
		{
			ColumnName = mappingName,
			SortDirection = direction
		});
	}

	public static TreeGridCell? FindCell(DependencyObject? element)
	{
		DependencyObject? current = element;

		while (current != null)
		{
			if (current is TreeGridCell cell)
				return cell;

			DependencyObject? parent = VisualTreeHelper.GetParent(current);
			if (parent == null && current is FrameworkElement fe)
				parent = fe.Parent as DependencyObject;

			current = parent;
		}

		return null;
	}
}
