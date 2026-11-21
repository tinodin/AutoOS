using AutoOS.App.Data.Enums.Power;
using AutoOS.App.Data.Models.Power;
using AutoOS.App.Helpers.TreeGrid;
using AutoOS.App.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using Syncfusion.UI.Xaml.DataGrid;
using Syncfusion.UI.Xaml.TreeGrid;

namespace AutoOS.App.Views.Settings;

public sealed partial class PowerPage : Page
{
	public PowerPageViewModel ViewModel { get; } = Ioc.Default.GetRequiredService<PowerPageViewModel>();

	public PowerPage()
	{
		InitializeComponent();
	}

	protected override void OnNavigatedTo(NavigationEventArgs e)
	{
		base.OnNavigatedTo(e);
		ViewModel.RefreshFilterAction = RefreshSearchFilter;
		ViewModel.RefreshFilterOnlyAction = RefreshFilterOnly;
		_ = ViewModel.LoadPlansAsync();
	}

	private void Search_AcceleratorInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
	{
		Search.Focus(FocusState.Programmatic);
		args.Handled = true;
	}

	private void CompareToggle_RightTapped(object sender, RightTappedRoutedEventArgs args) => ViewModel.ChangeComparePlansCommand.Execute(null);

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

	private void TreeGrid_CellToolTipOpening(object sender, TreeGridCellToolTipOpeningEventArgs e)
	{
		if (e.Record is not Node node)
		{
			e.ToolTip.Visibility = Visibility.Collapsed;
			return;
		}

		string? content = e.Column?.MappingName switch
		{
			nameof(Node.DisplayName) => node.Description,
			nameof(Node.DisplayAc) => node.AcToolTip,
			nameof(Node.DisplayDc) => node.DcToolTip,
			nameof(Node.DisplayCompare1Ac) => node.Compare1AcToolTip,
			nameof(Node.DisplayCompare2Ac) => node.Compare2AcToolTip,
			nameof(Node.DisplayCompare1Dc) => node.Compare1DcToolTip,
			nameof(Node.DisplayCompare2Dc) => node.Compare2DcToolTip,
			nameof(Node.DisplayOriginalAc) => node.OriginalAcToolTip,
			nameof(Node.DisplayOriginalDc) => node.OriginalDcToolTip,
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

	private void TreeGrid_CurrentCellBeginEdit(object sender, TreeGridCurrentCellBeginEditEventArgs e)
	{
		if (sender is not SfTreeGrid treeGrid)
			return;

		Node? node = treeGrid.GetNodeAtRowIndex(e.RowColumnIndex.RowIndex)?.Item as Node ?? treeGrid.CurrentItem as Node;
		if (node is not { NodeKind: NodeKind.Setting } || !node.IsAdjustable)
		{
			e.Cancel = true;
			return;
		}

		string mappingName = e.Column?.MappingName ?? string.Empty;
		bool isPlan1Column = mappingName is nameof(Node.DisplayCompare1Ac) or nameof(Node.DisplayCompare1Dc);
		bool isPlan2Column = mappingName is nameof(Node.DisplayCompare2Ac) or nameof(Node.DisplayCompare2Dc);
		if ((isPlan1Column && !ViewModel.IsComparePlan1Active) || (isPlan2Column && !ViewModel.IsComparePlan2Active))
		{
			e.Cancel = true;
			return;
		}

		ViewModel.BeginEdit(node, mappingName);
	}

	private void TreeGrid_CurrentCellEndEdit(object sender, CurrentCellEndEditEventArgs e)
	{
		if (sender is not SfTreeGrid treeGrid)
			return;

		Node? node = treeGrid.GetNodeAtRowIndex(e.RowColumnIndex.RowIndex)?.Item as Node ?? treeGrid.CurrentItem as Node;
		int visibleIndex = treeGrid.ResolveToGridVisibleColumnIndex(e.RowColumnIndex.ColumnIndex);
		string mappingName = treeGrid.Columns[visibleIndex].MappingName;
		if (ViewModel.CommitEdit(node, mappingName))
			ViewModel.RefreshAfterEdit();
	}

	private void EditControl_Loaded(object sender, RoutedEventArgs e)
	{
		if (sender is Control control)
			control.Focus(FocusState.Programmatic);
		if (sender is Microsoft.UI.Xaml.Controls.TextBox textBox)
			textBox.SelectAll();
	}

	private void EditComboBox_DropDownClosed(object sender, object e)
	{
		TreeGrid.SelectionController.CurrentCellManager.EndEdit();
		CompareTreeGrid.SelectionController.CurrentCellManager.EndEdit();
		ChangesTreeGrid.SelectionController.CurrentCellManager.EndEdit();
	}

	private void TreeGrid_ContextRequested(UIElement sender, ContextRequestedEventArgs args)
		=> TreeGridContextFlyoutHelper.HandleCellContextRequested<Node>(sender, args, PowerPageViewModel.GetContextFlyoutItems, ViewModel.CopyTextCommand);

	private void TreeGrid_TreeGridContextFlyoutOpening(object sender, TreeGridContextFlyoutEventArgs e) => TreeGridContextFlyoutHelper.ShowHeaderContextFlyout(sender, e);

	private void RefreshFilterOnly()
	{
		ApplyFilter(TreeGrid, ViewModel);
		ApplyFilter(CompareTreeGrid, ViewModel);
		ApplyFilter(ChangesTreeGrid, ViewModel);
	}

	private void RefreshSearchFilter()
	{
		ViewModel.UpdateNodeCounts();
		RefreshFilterOnly();
	}

	private static void ApplyFilter(SfTreeGrid treeGrid, PowerPageViewModel viewModel)
	{
		TreeGridView? view = treeGrid.View;
		if (view == null)
			return;
		view.Filter = viewModel.MatchesFilter;
		view.RefreshFilter();
	}
}
