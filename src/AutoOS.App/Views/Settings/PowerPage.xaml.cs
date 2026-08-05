using AutoOS.Core.Helpers.Picker;
using AutoOS.Helpers.Picker;
using AutoOS.ViewModels;
using Microsoft.UI.Xaml.Input;
using Syncfusion.UI.Xaml.Data;
using Syncfusion.UI.Xaml.DataGrid;
using Syncfusion.UI.Xaml.Grids;
using Syncfusion.UI.Xaml.TreeGrid;
using System.Diagnostics;
using Windows.ApplicationModel.DataTransfer;
using Windows.System;

namespace AutoOS.Views.Settings;

public sealed partial class PowerPage : Page
{
	private bool _cancelCurrentEdit;
	private string _editingMappingName;

	public PowerPageViewModel ViewModel { get; } = new();

	public PowerPage()
	{
		InitializeComponent();
		ViewModel.RefreshFilterAction = RefreshSearchFilter;
	}

	protected override void OnNavigatedTo(NavigationEventArgs e)
	{
		base.OnNavigatedTo(e);
		_ = LoadPowerPlansAsync();
	}

	private async Task LoadPowerPlansAsync()
	{
		try
		{
			await ViewModel.LoadPowerPlansAsync();
			SyncPlanSelections();
		}
		catch (Exception ex)
		{
			await MessageBox.ShowErrorAsync(App.MainWindow, ex.Message, "Failure");
		}
	}

	private void SyncPlanSelections()
	{
		PowerPlanComboBox.SelectedItem = ViewModel.ActivePlan;
		ComparePowerPlanComboBox.SelectedItem = ViewModel.ComparePlan;
		if (ViewChanges.IsChecked == true)
			ViewChanges.IsChecked = false;
	}

	private async void PowerPlanComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (PowerPlanComboBox.SelectedItem is not PowerPlan selectedPlan || selectedPlan == ViewModel.ActivePlan)
			return;

		if (!TryEndCurrentEdit())
		{
			PowerPlanComboBox.SelectedItem = ViewModel.ActivePlan;
			return;
		}

		await ViewModel.SetActivePlanAsync(selectedPlan);
		SyncPlanSelections();
	}

	private void ComparePowerPlanComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (ComparePowerPlanComboBox.SelectedItem is not PowerPlan selectedPlan || selectedPlan == ViewModel.ComparePlan)
			return;

		if (!TryEndCurrentEdit())
		{
			ComparePowerPlanComboBox.SelectedItem = ViewModel.ComparePlan;
			return;
		}

		if (!selectedPlan.IsPlaceholder && ViewChanges.IsChecked == true)
			ViewChanges.IsChecked = false;

		ViewModel.SetComparePlan(selectedPlan);
		RefreshSearchFilter();
		DispatcherQueue.TryEnqueue(() => GetVisibleTreeGrid()?.ExpandAllNodes());
	}

	private void Search_AcceleratorInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
	{
		Search.Focus(FocusState.Programmatic);
		args.Handled = true;
	}

	private void Search_TextChanged(object sender, TextChangedEventArgs e)
	{
		ViewModel.SearchText = Search.Text;
		Search.Focus(FocusState.Programmatic);
	}

	private void Undo_Click(object sender, RoutedEventArgs e)
	{
		if (!TryEndCurrentEdit())
			return;
		ViewModel.Undo();
	}

	private void Redo_Click(object sender, RoutedEventArgs e)
	{
		if (!TryEndCurrentEdit())
			return;
		ViewModel.Redo();
	}

	private void ViewChanges_Checked(object sender, RoutedEventArgs e)
	{
		SetViewChanges(true);
	}

	private void ViewChanges_Unchecked(object sender, RoutedEventArgs e)
	{
		SetViewChanges(false);
	}

	private void SetViewChanges(bool value)
	{
		if (!TryEndCurrentEdit())
		{
			ViewChanges.IsChecked = !value;
			return;
		}

		if (value)
			ComparePowerPlanComboBox.SelectedItem = ViewModel.ComparePlans.FirstOrDefault(plan => plan.IsPlaceholder);

		ViewModel.SetViewChanges(value);
		RefreshSearchFilter();
		DispatcherQueue.TryEnqueue(() => GetVisibleTreeGrid()?.ExpandAllNodes());
	}

	private async void SaveChanges_Click(object sender, RoutedEventArgs e)
	{
		if (!TryEndCurrentEdit())
			return;

		PowerSaveResult result = ViewModel.SaveChanges();
		if (!result.Succeeded)
			await MessageBox.ShowErrorAsync(App.MainWindow, result.ErrorMessage, "Failure");
	}

	private async void Restore_Click(object sender, RoutedEventArgs e)
	{
		if (!TryEndCurrentEdit())
			return;

		await ViewModel.RestoreDefaultPlansAsync();
		SyncPlanSelections();
	}

	private async void Import_Click(object sender, RoutedEventArgs e)
	{
		if (!TryEndCurrentEdit())
			return;

		var picker = new FilePicker(App.MainWindow)
		{
			ShowAllFilesOption = false
		};
		picker.FileTypeChoices.Add("Power Scheme Files", ["*.pow"]);
		var file = await picker.PickSingleFileAsync();
		if (file == null)
			return;

		await ViewModel.ImportPowerPlanAsync(file.Path);
		SyncPlanSelections();
	}

	private async void Edit_Click(object sender, RoutedEventArgs e)
	{
		if (!TryEndCurrentEdit() || PowerPlanComboBox.SelectedItem is not PowerPlan plan)
			return;

		var nameTextBox = new Microsoft.UI.Xaml.Controls.TextBox
		{
			Text = plan.Name,
			Margin = new Thickness(0, 0, 0, 8)
		};
		var descriptionBox = new DevWinUI.TextBox
		{
			AcceptsReturn = true,
			Text = plan.Description
		};
		var panel = new StackPanel { Spacing = 4 };
		panel.Children.Add(new TextBlock { Text = "Name:" });
		panel.Children.Add(nameTextBox);
		panel.Children.Add(new TextBlock { Text = "Description:" });
		panel.Children.Add(descriptionBox);

		var dialog = new ContentDialog
		{
			Title = "Edit Power Plan",
			Content = panel,
			PrimaryButtonText = "Apply",
			CloseButtonText = "Cancel",
			DefaultButton = ContentDialogButton.Close,
			XamlRoot = XamlRoot
		};
		if (await dialog.ShowAsync() != ContentDialogResult.Primary)
			return;

		ViewModel.UpdatePlanMetadata(plan, nameTextBox.Text, descriptionBox.Text);
	}

	private async void Duplicate_Click(object sender, RoutedEventArgs e)
	{
		if (!TryEndCurrentEdit() || PowerPlanComboBox.SelectedItem is not PowerPlan plan)
			return;

		var dialog = new ContentDialog
		{
			Title = "Duplicate Power Plan",
			Content = @$"Are you sure you want to duplicate ""{plan.Name}""?",
			PrimaryButtonText = "Duplicate",
			CloseButtonText = "Cancel",
			DefaultButton = ContentDialogButton.Close,
			XamlRoot = XamlRoot
		};
		if (await dialog.ShowAsync() != ContentDialogResult.Primary)
			return;

		await ViewModel.DuplicatePlanAsync(plan);
		SyncPlanSelections();
	}

	private async void Delete_Click(object sender, RoutedEventArgs e)
	{
		if (!TryEndCurrentEdit() || PowerPlanComboBox.SelectedItem is not PowerPlan plan)
			return;

		if (ViewModel.PowerPlans.Count <= 1)
		{
			await MessageBox.ShowErrorAsync(App.MainWindow, "At least one other power plan must exist.", "Failure");
			return;
		}

		var dialog = new ContentDialog
		{
			Title = "Delete power plan",
			Content = $"Are you sure that you want to delete \"{plan.Name}\"?",
			PrimaryButtonText = "Yes",
			CloseButtonText = "No",
			DefaultButton = ContentDialogButton.Close,
			XamlRoot = XamlRoot
		};
		if (await dialog.ShowAsync() != ContentDialogResult.Primary)
			return;

		await ViewModel.DeletePlanAsync(plan);
		SyncPlanSelections();
	}

	private async void Export_Click(object sender, RoutedEventArgs e)
	{
		if (PowerPlanComboBox.SelectedItem is not PowerPlan plan)
			return;

		var picker = new SavePicker(App.MainWindow)
		{
			ShowAllFilesOption = false,
			SuggestedFileName = plan.Name
		};
		picker.FileTypeChoices.Add("Power Scheme Files", ["*.pow"]);
		var file = await picker.PickSaveFileAsync();
		if (file == null)
			return;

		string path = file.Path.EndsWith(".pow", StringComparison.OrdinalIgnoreCase) ? file.Path : $"{file.Path}.pow";
		var startInfo = new ProcessStartInfo
		{
			FileName = "powercfg.exe",
			Arguments = @$"-export ""{path}"" {plan.Guid:D}",
			UseShellExecute = false,
			CreateNoWindow = true
		};
		using var process = Process.Start(startInfo);
		if (process != null)
		{
			await process.WaitForExitAsync();
			if (process.ExitCode != 0)
				await MessageBox.ShowErrorAsync(App.MainWindow, $"powercfg returned exit code {process.ExitCode}.", "Failure");
		}
	}

	private void PowerTreeGrid_Loaded(object sender, RoutedEventArgs e)
	{
		if (sender is SfTreeGrid treeGrid)
			treeGrid.View?.Filter = ViewModel.MatchesFilter;
	}

	private void PowerTreeGrid_SizeChanged(object sender, SizeChangedEventArgs e)
	{
		if (e.NewSize.Width <= 0 || e.NewSize.Width == e.PreviousSize.Width)
			return;

		if (sender is not SfTreeGrid treeGrid)
			return;

		foreach (var column in treeGrid.Columns)
			column.Width = double.NaN;
		treeGrid.InvalidateMeasure();
		treeGrid.UpdateLayout();
	}

	private void PowerTreeGrid_CurrentCellBeginEdit(object sender, TreeGridCurrentCellBeginEditEventArgs e)
	{
		if (sender is not SfTreeGrid treeGrid)
			return;

		var node = treeGrid.GetNodeAtRowIndex(e.RowColumnIndex.RowIndex)?.Item as PowerTreeNode ?? treeGrid.CurrentItem as PowerTreeNode;
		if (node is not { NodeKind: PowerNodeKind.Setting, HasValues: true })
		{
			e.Cancel = true;
			return;
		}

		_cancelCurrentEdit = false;
		_editingMappingName = e.Column?.MappingName;
		ViewModel.BeginEdit(node, _editingMappingName);
	}

	private void PowerTreeGrid_CurrentCellEndEdit(object sender, CurrentCellEndEditEventArgs e)
	{
		if (sender is not SfTreeGrid treeGrid)
			return;

		var node = treeGrid.GetNodeAtRowIndex(e.RowColumnIndex.RowIndex)?.Item as PowerTreeNode ?? treeGrid.CurrentItem as PowerTreeNode;
		if (_cancelCurrentEdit)
			ViewModel.CancelEdit(node);
		else
		{
			bool changed = ViewModel.CommitEdit(node, _editingMappingName);
			if (!changed && node?.HasErrors == true)
				ViewModel.CancelEdit(node);
			else if (changed)
				DispatcherQueue.TryEnqueue(ViewModel.RefreshAfterEdit);
		}
		_cancelCurrentEdit = false;
		_editingMappingName = null;
	}

	private void EditControl_KeyDown(object sender, KeyRoutedEventArgs e)
	{
		if (e.Key != VirtualKey.Escape || sender is ComboBox { IsDropDownOpen: true })
			return;

		_cancelCurrentEdit = true;
		ViewModel.CancelEdit(GetVisibleTreeGrid()?.CurrentItem as PowerTreeNode);
	}

	private void EditControl_Loaded(object sender, RoutedEventArgs e)
	{
		if (sender is Control control)
			control.Focus(FocusState.Programmatic);
		if (sender is Microsoft.UI.Xaml.Controls.TextBox textBox)
			textBox.SelectAll();
	}

	private void PowerTreeGrid_CellToolTipOpening(object sender, TreeGridCellToolTipOpeningEventArgs e)
	{
		if (e.Record is not PowerTreeNode node)
		{
			e.ToolTip.Visibility = Visibility.Collapsed;
			return;
		}

		string content = e.Column?.MappingName switch
		{
			nameof(PowerTreeNode.DisplayName) => node.Description,
			nameof(PowerTreeNode.DisplayAc) => node.AcToolTip,
			nameof(PowerTreeNode.DisplayDc) => node.DcToolTip,
			nameof(PowerTreeNode.DisplayCompareAc) => node.CompareAcToolTip,
			nameof(PowerTreeNode.DisplayCompareDc) => node.CompareDcToolTip,
			nameof(PowerTreeNode.DisplayOriginalAc) => node.OriginalAcToolTip,
			nameof(PowerTreeNode.DisplayOriginalDc) => node.OriginalDcToolTip,
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

	private void PowerTreeGrid_TreeGridContextFlyoutOpening(object sender, TreeGridContextFlyoutEventArgs e)
	{
		if (sender is not SfTreeGrid treeGrid)
			return;

		if (e.ContextFlyoutType == Syncfusion.UI.Xaml.TreeGrid.ContextFlyoutType.HeaderCell)
		{
			CreateHeaderContextMenu(e, treeGrid);
			return;
		}

		if (treeGrid.GetNodeAtRowIndex(e.RowColumnIndex.RowIndex)?.Item is not PowerTreeNode node)
		{
			e.ContextFlyout.Items.Clear();
			return;
		}

		e.ContextFlyout.Items.Clear();
		if (node.NodeKind == PowerNodeKind.Subgroup)
		{
			AddCopyItem(e.ContextFlyout, "Copy GUID", node.Guid.ToString());
			AddCopyItem(e.ContextFlyout, "Copy Name", node.DisplayName);
			return;
		}
		if (node.Setting == null)
			return;
		AddCopyItem(e.ContextFlyout, "Copy GUID", node.Setting.Guid.ToString());
		AddCopyItem(e.ContextFlyout, "Copy Name", node.Setting.Name);
		AddCopyItem(e.ContextFlyout, "Copy Description", node.Setting.Description);
		if (!node.HasValues)
			return;
		AddCopyItem(e.ContextFlyout, "Copy AC Value", node.DisplayAc);
		AddCopyItem(e.ContextFlyout, "Copy AC Value Description", node.AcToolTip);
		AddCopyItem(e.ContextFlyout, "Copy DC Value", node.DisplayDc);
		AddCopyItem(e.ContextFlyout, "Copy DC Value Description", node.DcToolTip);
		if (node.ProjectionKind == PowerProjectionKind.Comparison)
		{
			AddCopyItem(e.ContextFlyout, "Copy Compare AC Value", node.DisplayCompareAc);
			AddCopyItem(e.ContextFlyout, "Copy Compare DC Value", node.DisplayCompareDc);
		}
		else if (node.ProjectionKind == PowerProjectionKind.ViewChanges)
		{
			AddCopyItem(e.ContextFlyout, "Copy Original AC Value", node.DisplayOriginalAc);
			AddCopyItem(e.ContextFlyout, "Copy Original DC Value", node.DisplayOriginalDc);
		}
	}

	private void CreateHeaderContextMenu(TreeGridContextFlyoutEventArgs e, SfTreeGrid treeGrid)
	{
		var column = treeGrid.Columns[treeGrid.ResolveToGridVisibleColumnIndex(e.RowColumnIndex.ColumnIndex)];
		e.ContextFlyout.Items.Clear();
		if (column == null || !column.AllowSorting)
			return;

		bool isAscending = treeGrid.SortColumnDescriptions.Any(description => description.ColumnName == column.MappingName && description.SortDirection == SortDirection.Ascending);
		bool isDescending = treeGrid.SortColumnDescriptions.Any(description => description.ColumnName == column.MappingName && description.SortDirection == SortDirection.Descending);
		var ascending = new RadioMenuFlyoutItem
		{
			Text = "Sort Ascending",
			GroupName = "PowerSortGroup",
			IsChecked = isAscending && !isDescending
		};
		ascending.Click += (_, _) => SetSort(treeGrid, column.MappingName, SortDirection.Ascending);
		e.ContextFlyout.Items.Add(ascending);

		var descending = new RadioMenuFlyoutItem
		{
			Text = "Sort Descending",
			GroupName = "PowerSortGroup",
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

	private static void AddCopyItem(MenuFlyout flyout, string label, string text)
	{
		var item = new MenuFlyoutItem { Text = label };
		item.Click += (_, _) => CopyText(text);
		flyout.Items.Add(item);
	}

	private static void CopyText(string text)
	{
		var package = new DataPackage();
		package.SetText(text ?? string.Empty);
		Clipboard.SetContent(package);
	}

	private void RefreshSearchFilter()
	{
		PowerTreeGrid.View?.Filter = ViewModel.MatchesFilter;
		PowerTreeGrid.View?.RefreshFilter();
		ComparePowerTreeGrid.View?.Filter = ViewModel.MatchesFilter;
		ComparePowerTreeGrid.View?.RefreshFilter();
		ChangesPowerTreeGrid.View?.Filter = ViewModel.MatchesFilter;
		ChangesPowerTreeGrid.View?.RefreshFilter();
	}

	private bool TryEndCurrentEdit()
	{
		SfTreeGrid treeGrid = GetVisibleTreeGrid();
		if (treeGrid?.SelectionController?.CurrentCellManager?.CurrentCell?.IsEditing != true)
		{
			if (ViewModel.HasValidationErrors)
				ViewModel.CancelEdit(null);
			return true;
		}
		return treeGrid.SelectionController.CurrentCellManager.EndEdit();
	}

	private SfTreeGrid GetVisibleTreeGrid() => ViewModel.Mode switch
	{
		PowerPageMode.Comparison => ComparePowerTreeGrid,
		PowerPageMode.ViewChanges => ChangesPowerTreeGrid,
		_ => PowerTreeGrid
	};
}
