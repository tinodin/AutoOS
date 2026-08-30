using AutoOS.App.Data.Enums.Bios;
using AutoOS.App.Data.Models.Bios;
using AutoOS.App.Helpers.TreeGrid;
using AutoOS.App.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml.Input;
using Syncfusion.UI.Xaml.Data;
using Syncfusion.UI.Xaml.DataGrid;
using Syncfusion.UI.Xaml.Grids;
using Syncfusion.UI.Xaml.TreeGrid;
using Windows.System;

namespace AutoOS.App.Views.Settings;

public sealed partial class BiosSettingsPage : Page
{
	public BiosSettingsPageViewModel ViewModel { get; } = Ioc.Default.GetRequiredService<BiosSettingsPageViewModel>();

	public BiosSettingsPage()
	{
		InitializeComponent();
	}

	protected override void OnNavigatedTo(NavigationEventArgs e)
	{
		base.OnNavigatedTo(e);
		ViewModel.RefreshFilterAction = RefreshSearchFilter;
		ViewModel.RefreshFilterOnlyAction = RefreshFilterOnly;
		_ = ViewModel.ReadFromNvramAsync();
	}

	private void Search_AcceleratorInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
	{
		Search.Focus(FocusState.Programmatic);
		args.Handled = true;
	}

	private void MergeCountBox_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
	{
		if (sender.Key == VirtualKey.Up)
		{
			if (ViewModel.MergeCount < ViewModel.RecommendedCount)
				ViewModel.MergeCount++;
		}
		else if (sender.Key == VirtualKey.Down)
		{
			if (ViewModel.MergeCount > 0)
				ViewModel.MergeCount--;
		}
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

	private void TreeGrid_CellToolTipOpening(object sender, TreeGridCellToolTipOpeningEventArgs e)
	{
		if (e.Record is not Node node)
		{
			e.ToolTip.Visibility = Visibility.Collapsed;
			return;
		}

		string? content = e.Column?.MappingName switch
		{
			nameof(Node.DisplayName) => node.ToolTipText,
			nameof(Node.DisplayCurrent) => node.DisplayCurrent,
			nameof(Node.DisplayOriginal) => node.DisplayOriginal,
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

	private void TreeGrid_CurrentCellBeginEdit(object sender, TreeGridCurrentCellBeginEditEventArgs e)
	{
		if (sender is not SfTreeGrid treeGrid)
			return;

		Node? node = treeGrid.GetNodeAtRowIndex(e.RowColumnIndex.RowIndex)?.Item as Node ?? treeGrid.CurrentItem as Node;
		if (node is not { NodeKind: NodeKind.Setting })
		{
			e.Cancel = true;
			return;
		}

		ViewModel.BeginEdit(node);
	}

	private void TreeGrid_CurrentCellEndEdit(object sender, CurrentCellEndEditEventArgs e)
	{
		if (sender is not SfTreeGrid treeGrid)
			return;

		Node? node = treeGrid.GetNodeAtRowIndex(e.RowColumnIndex.RowIndex)?.Item as Node ?? treeGrid.CurrentItem as Node;
		if (ViewModel.CommitEdit(node))
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
		if (TreeGridEditingHelper.EndEditingIfActive(TreeGrid))
			return;

		if (TreeGridEditingHelper.EndEditingIfActive(CompareTreeGrid))
			return;

		if (TreeGridEditingHelper.EndEditingIfActive(ChangesTreeGrid))
			return;
	}

	private void TreeGrid_TreeGridContextFlyoutOpening(object sender, TreeGridContextFlyoutEventArgs e)
	{
		if (sender is not SfTreeGrid treeGrid)
			return;

		if (e.ContextFlyoutType != Syncfusion.UI.Xaml.TreeGrid.ContextFlyoutType.HeaderCell)
			return;

		e.ContextFlyout.Items.Clear();

		TreeGridColumn column = treeGrid.Columns[treeGrid.ResolveToGridVisibleColumnIndex(e.RowColumnIndex.ColumnIndex)];

		bool isAscending = treeGrid.SortColumnDescriptions.Any(description => description.ColumnName == column.MappingName && description.SortDirection == SortDirection.Ascending);
		bool isDescending = treeGrid.SortColumnDescriptions.Any(description => description.ColumnName == column.MappingName && description.SortDirection == SortDirection.Descending);

		var ascending = new RadioMenuFlyoutItem
		{
			Text = "Sort Ascending",
			IsChecked = isAscending && !isDescending
		};
		ascending.Click += (_, _) => SetSort(treeGrid, column.MappingName, SortDirection.Ascending);
		e.ContextFlyout.Items.Add(ascending);

		var descending = new RadioMenuFlyoutItem
		{
			Text = "Sort Descending",
			IsChecked = isDescending
		};
		descending.Click += (_, _) => SetSort(treeGrid, column.MappingName, SortDirection.Descending);
		e.ContextFlyout.Items.Add(descending);

		e.ContextFlyout.Items.Add(new MenuFlyoutSeparator());

		var clear = new MenuFlyoutItem { Text = "Clear Sorting" };
		clear.Click += (_, _) => treeGrid.SortColumnDescriptions.Clear();
		e.ContextFlyout.Items.Add(clear);
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

	private static void ApplyFilter(SfTreeGrid treeGrid, BiosSettingsPageViewModel viewModel)
	{
		TreeGridView? view = treeGrid.View;
		if (view == null)
			return;
		view.Filter = viewModel.MatchesFilter;
		view.RefreshFilter();
	}
}
