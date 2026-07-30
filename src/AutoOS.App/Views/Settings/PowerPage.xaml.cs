using AutoOS.Core.Helpers.Picker;
using AutoOS.Core.Helpers.Power;
using AutoOS.Helpers.Picker;
using AutoOS.ViewModels;
using Microsoft.UI.Xaml.Input;
using Syncfusion.UI.Xaml.Data;
using Syncfusion.UI.Xaml.DataGrid;
using Syncfusion.UI.Xaml.Grids;
using Syncfusion.UI.Xaml.TreeGrid;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Windows.ApplicationModel.DataTransfer;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Power;
using Windows.System;

namespace AutoOS.Views.Settings;

public sealed partial class PowerPage : Page
{
	private bool _isChangingPowerPlans;
	private bool _isChangingViewMode;
	private bool _cancelCurrentEdit;
	private string _editingMappingName;

	public PowerPageViewModel ViewModel { get; } = new();

	public PowerPage()
	{
		InitializeComponent();
		ViewModel.RefreshFilterAction = RefreshSearchFilter;
		LoadPowerPlans();
	}

	private void LoadPowerPlans(Guid? selectedGuid = null)
	{
		ViewModel.SetIsLoaded(false);
		_isChangingPowerPlans = true;
		try
		{
			(List<PowerPlan> plans, Guid activeGuid) = ReadPowerPlans();

			bool hasTarget = selectedGuid.HasValue || activeGuid != Guid.Empty;
			Guid targetGuid = selectedGuid ?? activeGuid;
			ViewModel.SetPlans(plans, targetGuid, hasTarget);
			PowerPlan selectedPlan = ViewModel.ActivePlan;
			if (selectedPlan == null)
			{
				PowerPlanComboBox.SelectedItem = null;
				ComparePowerPlanComboBox.SelectedItem = ViewModel.ComparePlan;
				return;
			}

			if (selectedPlan.Guid != activeGuid && PowerHelper.PowerSetActiveScheme(selectedPlan.Guid) != 0)
			{
				ViewModel.SetPlans(plans, activeGuid);
				selectedPlan = ViewModel.ActivePlan;
			}

			IReadOnlyList<PowerSubgroupState> settings = LoadPowerPlanSettings(selectedPlan.Guid);
			ViewModel.LoadActivePlan(selectedPlan, settings);
			PowerPlanComboBox.SelectedItem = selectedPlan;
			ComparePowerPlanComboBox.SelectedItem = ViewModel.ComparePlan;
			ViewChanges.IsChecked = false;
			UpdateColumnEditing();
		}
		finally
		{
			_isChangingPowerPlans = false;
		}
	}

	private static unsafe (List<PowerPlan> Plans, Guid ActiveGuid) ReadPowerPlans()
	{
		List<PowerPlan> plans = [];
		uint index = 0;
		uint guidSize = (uint)sizeof(Guid);
		byte* buffer = stackalloc byte[(int)guidSize];

		while (true)
		{
			uint size = guidSize;
			uint result = (uint)PInvoke.PowerEnumerate(default, null, null, POWER_DATA_ACCESSOR.ACCESS_SCHEME, index++, new Span<byte>(buffer, (int)guidSize), ref size);
			if (result != 0)
				break;

			Guid guid = new(new ReadOnlySpan<byte>(buffer, (int)guidSize));
			plans.Add(new PowerPlan
			{
				Guid = guid,
				Name = PowerHelper.ReadFriendlyName(guid, null, null),
				Description = PowerHelper.ReadDescription(guid)
			});
		}

		Guid* activePointer;
		WIN32_ERROR activeResult = PInvoke.PowerGetActiveScheme(default, out activePointer);
		Guid activeGuid = activeResult == WIN32_ERROR.ERROR_SUCCESS && activePointer != null ? *activePointer : Guid.Empty;
		if (activePointer != null)
			PInvoke.LocalFree((HLOCAL)activePointer);

		return (plans, activeGuid);
	}

	private unsafe IReadOnlyList<PowerSubgroupState> LoadPowerPlanSettings(Guid scheme)
	{
		Guid noneSubgroupGuid = new("fea3413e-7e05-4911-9a71-700331f1c294");
		List<PowerSubgroupState> subgroups = [];
		var noneSubgroup = new PowerSubgroupState
		{
			Guid = noneSubgroupGuid,
			Name = "None"
		};
		foreach (var setting in EnumerateSettings(scheme, noneSubgroupGuid, null))
			noneSubgroup.Settings.Add(setting);
		subgroups.Add(noneSubgroup);

		uint subgroupIndex = 0;
		uint guidSize = (uint)Marshal.SizeOf<Guid>();
		byte* subgroupBuffer = stackalloc byte[(int)guidSize];

		while (true)
		{
			uint size = guidSize;
			uint result = (uint)PInvoke.PowerEnumerate(default, (Guid?)scheme, null, POWER_DATA_ACCESSOR.ACCESS_SUBGROUP, subgroupIndex++, new Span<byte>(subgroupBuffer, (int)guidSize), ref size);
			if (result != 0)
				break;

			Guid subgroupGuid = new(new ReadOnlySpan<byte>(subgroupBuffer, (int)guidSize));
			string name = subgroupGuid == new Guid("9596fb26-9850-41fd-ac3e-f7c3c00afd4b") ? "Multimedia settings" : PowerHelper.ReadFriendlyName(scheme, subgroupGuid, null);
			if (string.IsNullOrWhiteSpace(name))
				continue;

			var subgroup = new PowerSubgroupState
			{
				Guid = subgroupGuid,
				Name = name,
				Description = PowerHelper.ReadDescription(scheme, subgroupGuid)
			};
			foreach (var setting in EnumerateSettings(scheme, subgroupGuid, subgroupGuid))
				subgroup.Settings.Add(setting);
			if (subgroup.Settings.Count > 0)
				subgroups.Add(subgroup);
		}

		return [noneSubgroup, .. subgroups.Where(subgroup => subgroup != noneSubgroup)];
	}

	private static unsafe IReadOnlyList<PowerSettingState> EnumerateSettings(Guid scheme, Guid subgroupGuid, Guid? enumerationSubgroup)
	{
		List<PowerSettingState> settings = [];
		uint settingIndex = 0;
		uint guidSize = (uint)Marshal.SizeOf<Guid>();
		byte* settingBuffer = stackalloc byte[(int)guidSize];

		while (true)
		{
			uint size = guidSize;
			uint result = (uint)PInvoke.PowerEnumerate(default, (Guid?)scheme, enumerationSubgroup, POWER_DATA_ACCESSOR.ACCESS_INDIVIDUAL_SETTING, settingIndex++, new Span<byte>(settingBuffer, (int)guidSize), ref size);
			if (result != 0)
				break;

			Guid settingGuid = new(new ReadOnlySpan<byte>(settingBuffer, (int)guidSize));
			if (!PowerHelper.TryReadAcValueIndex(scheme, subgroupGuid, settingGuid, out uint acValue) ||
				!PowerHelper.TryReadDcValueIndex(scheme, subgroupGuid, settingGuid, out uint dcValue))
			{
				continue;
			}

			uint? minimum = PowerHelper.TryReadValueMin(subgroupGuid, settingGuid, out uint minimumValue) ? minimumValue : null;
			uint? maximum = PowerHelper.TryReadValueMax(subgroupGuid, settingGuid, out uint maximumValue) ? maximumValue : null;
			uint? increment = PowerHelper.TryReadValueIncrement(subgroupGuid, settingGuid, out uint incrementValue) ? incrementValue : null;
			var setting = new PowerSettingState
			{
				SubgroupGuid = subgroupGuid,
				Guid = settingGuid,
				Name = PowerHelper.ReadFriendlyName(scheme, subgroupGuid, settingGuid),
				Description = PowerHelper.ReadDescription(scheme, subgroupGuid, settingGuid),
				AcValue = acValue,
				DcValue = dcValue,
				OriginalAcValue = acValue,
				OriginalDcValue = dcValue,
				Minimum = minimum,
				Maximum = maximum,
				Increment = increment,
				Unit = PowerHelper.ReadValueUnitsSpecifier(subgroupGuid, settingGuid)
			};
			if (string.IsNullOrWhiteSpace(setting.Name))
				setting.Name = settingGuid.ToString();
			setting.PrimeDisplayData();
			settings.Add(setting);
		}

		return settings;
	}

	private async void PowerPlanComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (_isChangingPowerPlans || PowerPlanComboBox.SelectedItem is not PowerPlan selectedPlan || selectedPlan == ViewModel.ActivePlan)
			return;

		_isChangingPowerPlans = true;
		try
		{
			if (!TryEndCurrentEdit() || !await ResolvePendingChangesAsync())
			{
				PowerPlanComboBox.SelectedItem = ViewModel.ActivePlan;
				return;
			}

			uint activationResult = PowerHelper.PowerSetActiveScheme(selectedPlan.Guid);
			if (activationResult != 0)
			{
				await ShowDialogAsync("Unable to activate power plan", $"Windows returned error {activationResult}.");
				PowerPlanComboBox.SelectedItem = ViewModel.ActivePlan;
				return;
			}
			ViewModel.SetIsLoaded(false);
			IReadOnlyList<PowerSubgroupState> settings = LoadPowerPlanSettings(selectedPlan.Guid);
			ViewModel.LoadActivePlan(selectedPlan, settings);
			ComparePowerPlanComboBox.SelectedItem = ViewModel.ComparePlan;
			ViewChanges.IsChecked = false;
			UpdateColumnEditing();
		}
		finally
		{
			_isChangingPowerPlans = false;
		}
	}

	private void ComparePowerPlanComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (_isChangingPowerPlans || ComparePowerPlanComboBox.SelectedItem is not PowerPlan selectedPlan)
			return;

		if (!TryEndCurrentEdit())
		{
			_isChangingPowerPlans = true;
			ComparePowerPlanComboBox.SelectedItem = ViewModel.ComparePlan;
			_isChangingPowerPlans = false;
			return;
		}

		if (!selectedPlan.IsPlaceholder && ViewChanges.IsChecked == true)
		{
			_isChangingViewMode = true;
			ViewChanges.IsChecked = false;
			_isChangingViewMode = false;
		}

		ViewModel.SetComparePlan(selectedPlan);
		UpdateColumnEditing();
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
		if (_isChangingViewMode)
			return;

		if (!TryEndCurrentEdit())
		{
			_isChangingViewMode = true;
			ViewChanges.IsChecked = !value;
			_isChangingViewMode = false;
			return;
		}

		if (value)
		{
			_isChangingPowerPlans = true;
			ComparePowerPlanComboBox.SelectedItem = ViewModel.ComparePlans.FirstOrDefault(plan => plan.IsPlaceholder);
			_isChangingPowerPlans = false;
		}

		ViewModel.SetViewChanges(value);
		UpdateColumnEditing();
		DispatcherQueue.TryEnqueue(() => GetVisibleTreeGrid()?.ExpandAllNodes());
	}

	private async void SaveChanges_Click(object sender, RoutedEventArgs e)
	{
		if (!TryEndCurrentEdit())
			return;
		await SaveChangesAsync();
	}

	private async Task<bool> SaveChangesAsync()
	{
		PowerSaveResult result = ViewModel.SaveChanges();
		if (result.Succeeded)
		{
			return true;
		}

		await ShowDialogAsync("Unable to save power settings", result.ErrorMessage);
		return false;
	}

	private async Task<bool> ResolvePendingChangesAsync()
	{
		if (ViewModel.ModifiedCount == 0)
			return true;

		var dialog = new ContentDialog
		{
			Title = "Unsaved power settings",
			Content = "Save your power setting changes before continuing?",
			PrimaryButtonText = "Save",
			SecondaryButtonText = "Discard",
			CloseButtonText = "Cancel",
			DefaultButton = ContentDialogButton.Primary,
			XamlRoot = XamlRoot
		};

		ContentDialogResult result = await dialog.ShowAsync();
		if (result == ContentDialogResult.Primary)
			return await SaveChangesAsync();
		if (result == ContentDialogResult.Secondary)
		{
			ViewModel.DiscardChanges();
			return true;
		}
		return false;
	}

	private async void Restore_Click(object sender, RoutedEventArgs e)
	{
		if (!TryEndCurrentEdit())
			return;

		var dialog = new ContentDialog
		{
			Title = "Restore power plans",
			Content = "Are you sure you want to restore the default power schemes?",
			PrimaryButtonText = "Restore",
			CloseButtonText = "Cancel",
			DefaultButton = ContentDialogButton.Close,
			XamlRoot = XamlRoot
		};
		if (await dialog.ShowAsync() != ContentDialogResult.Primary)
			return;
		if (!await ResolvePendingChangesAsync())
			return;

		uint restoreResult = PowerHelper.RestoreDefaultPowerSchemes();
		if (restoreResult != 0)
		{
			await ShowDialogAsync("Unable to restore power plans", $"Windows returned error {restoreResult}.");
			return;
		}
		LoadPowerPlans();
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
		if (!await ResolvePendingChangesAsync())
			return;

		Guid importedGuid = ImportPowerSchemeUnsafe(file.Path);
		if (importedGuid == Guid.Empty)
		{
			await ShowDialogAsync("Unable to import power plan", "Windows could not import the selected power scheme.");
			return;
		}

		uint activationResult = PowerHelper.PowerSetActiveScheme(importedGuid);
		if (activationResult != 0)
		{
			await ShowDialogAsync("Unable to activate imported power plan", $"Windows returned error {activationResult}.");
			LoadPowerPlans();
			return;
		}
		LoadPowerPlans(importedGuid);
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

		bool nameSaved = PowerHelper.WriteSchemeFriendlyName(plan.Guid, nameTextBox.Text);
		bool descriptionSaved = PowerHelper.WriteSchemeDescription(plan.Guid, descriptionBox.Text);
		plan.Name = PowerHelper.ReadFriendlyName(plan.Guid, null, null);
		plan.Description = PowerHelper.ReadDescription(plan.Guid);
		ViewModel.NotifyPlanHeaders();
		if (!nameSaved || !descriptionSaved)
			await ShowDialogAsync("Unable to update power plan", "Windows did not save all power plan metadata.");
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
		if (!await ResolvePendingChangesAsync())
			return;

		int number = 1;
		string name;
		do
		{
			name = number == 1 ? $"{plan.Name} - Copy" : $"{plan.Name} - Copy ({number})";
			number++;
		}
		while (ViewModel.PowerPlans.Any(item => item.Name.Equals(name, StringComparison.CurrentCultureIgnoreCase)));

		Guid guid = PowerHelper.DuplicateScheme(plan.Guid, name, plan.Description);
		if (guid == Guid.Empty)
		{
			await ShowDialogAsync("Unable to duplicate power plan", "Windows could not duplicate the selected power scheme.");
			return;
		}

		uint activationResult = PowerHelper.PowerSetActiveScheme(guid);
		if (activationResult != 0)
		{
			await ShowDialogAsync("Unable to activate duplicated power plan", $"Windows returned error {activationResult}.");
			LoadPowerPlans();
			return;
		}
		LoadPowerPlans(guid);
	}

	private async void Delete_Click(object sender, RoutedEventArgs e)
	{
		if (!TryEndCurrentEdit() || PowerPlanComboBox.SelectedItem is not PowerPlan plan)
			return;

		if (ViewModel.PowerPlans.Count <= 1)
		{
			await ShowDialogAsync("Unable to delete power plan", "At least one other power plan must exist.");
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
		if (!await ResolvePendingChangesAsync())
			return;

		int index = ViewModel.PowerPlans.IndexOf(plan);
		PowerPlan nextPlan = index > 0 ? ViewModel.PowerPlans[index - 1] : ViewModel.PowerPlans[index + 1];
		uint activationResult = PowerHelper.PowerSetActiveScheme(nextPlan.Guid);
		if (activationResult != 0)
		{
			await ShowDialogAsync("Unable to activate replacement power plan", $"Windows returned error {activationResult}.");
			return;
		}
		if (!PowerHelper.DeleteScheme(plan.Guid))
		{
			PowerHelper.PowerSetActiveScheme(plan.Guid);
			await ShowDialogAsync("Unable to delete power plan", "Windows could not delete the selected power scheme.");
			return;
		}

		LoadPowerPlans(nextPlan.Guid);
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
				await ShowDialogAsync("Unable to export power plan", $"powercfg returned exit code {process.ExitCode}.");
		}
	}

	private static unsafe Guid ImportPowerSchemeUnsafe(string filePath)
	{
		Guid* destination = null;
		uint result = (uint)PInvoke.PowerImportPowerScheme(default, filePath, ref destination);
		if (result != 0 || destination == null)
			return Guid.Empty;

		try
		{
			return *destination;
		}
		finally
		{
			PInvoke.LocalFree((HLOCAL)destination);
		}
	}

	private void PowerTreeGrid_Loaded(object sender, RoutedEventArgs e)
	{
		if (sender is SfTreeGrid treeGrid)
			treeGrid.View?.Filter = ViewModel.MatchesFilter;
		UpdateColumnEditing();
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

		var node = treeGrid.GetNodeAtRowIndex(e.RowColumnIndex.RowIndex)?.Item as PowerTreeNode;
		if (node == null)
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
		foreach (var treeGrid in GetTreeGrids())
		{
			treeGrid.View?.Filter = ViewModel.MatchesFilter;
			treeGrid.View?.RefreshFilter();
		}
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

	private void UpdateColumnEditing()
	{
		foreach (var treeGrid in GetTreeGrids())
		{
			foreach (var column in treeGrid.Columns)
			{
				column.AllowEditing = ViewModel.IsLoaded && (column.MappingName is nameof(PowerTreeNode.DisplayAc) or nameof(PowerTreeNode.DisplayDc));
			}
		}
	}

	private IEnumerable<SfTreeGrid> GetTreeGrids()
	{
		if (PowerTreeGrid != null)
			yield return PowerTreeGrid;
		if (ComparePowerTreeGrid != null)
			yield return ComparePowerTreeGrid;
		if (ChangesPowerTreeGrid != null)
			yield return ChangesPowerTreeGrid;
	}

	private SfTreeGrid GetVisibleTreeGrid() => ViewModel.Mode switch
	{
		PowerPageMode.Comparison => ComparePowerTreeGrid,
		PowerPageMode.ViewChanges => ChangesPowerTreeGrid,
		_ => PowerTreeGrid
	};

	private async Task ShowDialogAsync(string title, string content)
	{
		await new ContentDialog
		{
			Title = title,
			Content = content,
			CloseButtonText = "OK",
			DefaultButton = ContentDialogButton.Close,
			XamlRoot = XamlRoot
		}.ShowAsync();
	}
}
