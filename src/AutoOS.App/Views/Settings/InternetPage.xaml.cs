using AutoOS.App.Data.Enums.Network;
using AutoOS.App.Data.Models.Network;
using AutoOS.App.Helpers.TreeGrid;
using AutoOS.App.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using Syncfusion.UI.Xaml.DataGrid;
using Syncfusion.UI.Xaml.TreeGrid;

namespace AutoOS.App.Views.Settings;

public sealed partial class InternetPage : Page
{

	public InternetPageViewModel ViewModel { get; } = Ioc.Default.GetRequiredService<InternetPageViewModel>();

	public InternetPage()
	{
		InitializeComponent();
	}

	protected override void OnNavigatedTo(NavigationEventArgs e)
	{
		base.OnNavigatedTo(e);
		ViewModel.RefreshFilterAction = RefreshSearchFilter;
		ViewModel.RefreshFilterOnlyAction = RefreshFilterOnly;
		_ = ViewModel.LoadAdaptersAsync();
	}

	protected override void OnNavigatedFrom(NavigationEventArgs e)
	{
		ViewModel.RefreshFilterAction = null;
		ViewModel.RefreshFilterOnlyAction = null;
		base.OnNavigatedFrom(e);
	}

	private void Search_AcceleratorInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
	{
		Search.Focus(FocusState.Programmatic);
		args.Handled = true;
	}

	private void TreeGrid_SizeChanged(object sender, SizeChangedEventArgs e)
	{
		if (e.NewSize.Width <= 0 || e.NewSize.Width == e.PreviousSize.Width)
			return;

		if (sender is not SfTreeGrid treeGrid)
			return;

		foreach (TreeGridColumn column in treeGrid.Columns)
			column.Width = double.NaN;
		treeGrid.InvalidateMeasure();
		treeGrid.UpdateLayout();

		RefreshFilterOnly();
	}

	private void TreeGrid_CellToolTipOpening(object? sender, TreeGridCellToolTipOpeningEventArgs e)
	{
		if (e.Record is not Node node)
		{
			e.ToolTip.Visibility = Visibility.Collapsed;
			return;
		}

		string? content = e.Column?.MappingName switch
		{
			nameof(Node.DisplayName) => node.NodeKind == NodeKind.Adapter ? null : node.Description,
			nameof(Node.DisplayCurrent) => node.ValueToolTip,
			nameof(Node.DisplayOriginal) => node.OriginalValueToolTip,
			nameof(Node.DisplayRecommended) => node.DisplayRecommended,
			nameof(Node.DisplayDefault) => node.DisplayDefault,
			_ => null
		};
		if (string.IsNullOrWhiteSpace(content))
		{
			e.ToolTip.Visibility = Visibility.Collapsed;
			return;
		}

		e.ToolTip.Content = content;
		e.ToolTip.Visibility = Visibility.Visible;
	}

	private void TreeGrid_CurrentCellBeginEdit(object? sender, TreeGridCurrentCellBeginEditEventArgs e)
	{
		if (sender is not SfTreeGrid treeGrid)
			return;

		Node? node = treeGrid.GetNodeAtRowIndex(e.RowColumnIndex.RowIndex)?.Item as Node ?? treeGrid.CurrentItem as Node;
		if (node is not { IsAdjustable: true } || !ViewModel.IsLoaded)
		{
			e.Cancel = true;
			return;
		}

		ViewModel.BeginEdit(node, e.Column?.MappingName ?? string.Empty);
	}

	private void TreeGrid_CurrentCellEndEdit(object? sender, CurrentCellEndEditEventArgs e)
	{
		if (sender is not SfTreeGrid treeGrid)
			return;

		Node? node = treeGrid.GetNodeAtRowIndex(e.RowColumnIndex.RowIndex)?.Item as Node ?? treeGrid.CurrentItem as Node;
		int visibleIndex = treeGrid.ResolveToGridVisibleColumnIndex(e.RowColumnIndex.ColumnIndex);
		string mappingName = treeGrid.Columns[visibleIndex].MappingName;
		if (ViewModel.CommitEdit(node, mappingName))
			ViewModel.RefreshAfterEdit();
	}

	private void EditControl_Loaded(object? sender, RoutedEventArgs e)
	{
		if (sender is Control control)
			control.Focus(FocusState.Programmatic);
		if (sender is Microsoft.UI.Xaml.Controls.TextBox textBox)
			textBox.SelectAll();
	}

	private void EditComboBox_DropDownClosed(object? sender, object e)
	{
		TreeGrid.SelectionController.CurrentCellManager.EndEdit();
		CompareTreeGrid.SelectionController.CurrentCellManager.EndEdit();
	ChangesTreeGrid.SelectionController.CurrentCellManager.EndEdit();
	}

	private void TreeGrid_ContextRequested(UIElement sender, ContextRequestedEventArgs args)
		=> TreeGridContextFlyoutHelper.HandleCellContextRequested<Node>(sender, args, InternetPageViewModel.GetContextFlyoutItems, ViewModel.CopyTextCommand);

	private void TreeGrid_TreeGridContextFlyoutOpening(object? sender, TreeGridContextFlyoutEventArgs e) => TreeGridContextFlyoutHelper.ShowHeaderContextFlyout(sender!, e);

	private void RefreshFilterOnly()
	{
		ViewModel.RefreshFilterSnapshot();
		ApplyFilter(TreeGrid, ViewModel);
		ApplyFilter(CompareTreeGrid, ViewModel);
		ApplyFilter(ChangesTreeGrid, ViewModel);
		TreeGrid.QueueRowHeightRefresh();
		CompareTreeGrid.QueueRowHeightRefresh();
		ChangesTreeGrid.QueueRowHeightRefresh();
	}

	private void RefreshSearchFilter()
	{
		ViewModel.UpdateNodeCounts();
		RefreshFilterOnly();
	}

	private static void ApplyFilter(SfTreeGrid treeGrid, InternetPageViewModel viewModel)
	{
		TreeGridView? view = treeGrid.View;
		if (view == null)
			return;
		view.Filter = viewModel.MatchesFilter;
		view.RefreshFilter();
	}
}
