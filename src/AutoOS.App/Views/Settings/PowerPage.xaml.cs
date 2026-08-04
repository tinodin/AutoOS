using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using AutoOS.Core.Helpers.Picker;
using AutoOS.Core.Helpers.Power;
using AutoOS.Helpers.Picker;
using AutoOS.App.Views.Settings.Power;
using Microsoft.UI.Text;
using Windows.Storage;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Power;

namespace AutoOS.App.Views.Settings;

public sealed partial class PowerPage : Page
{
	private bool isInitializingPowerPlans = true;

	private readonly ObservableCollection<PowerPlan> _powerPlans = [];
	private readonly ObservableCollection<PowerPlan> _comparePlans = [];
	private readonly ObservableCollection<PowerSubgroup> _allSubgroups = [];
	public ObservableCollection<PowerSubgroup> Subgroups { get; } = [];
	public ObservableCollection<PowerCompareSubgroup> CompareSubgroups { get; } = [];
	private PowerCompareSubgroup _identicalPlansPlaceholder;

	public PowerPage()
	{
		InitializeComponent();
		LoadPowerPlans();
		ActiveTreeView.UpdateLayout();
		Loaded += PowerPage_Loaded;
	}

	private void PowerPage_Loaded(object sender, RoutedEventArgs e)
	{
		CompareTreeView.Opacity = 0;
		CompareTreeView.IsHitTestVisible = false;
	}

	private unsafe void LoadPowerPlans()
	{
		isInitializingPowerPlans = true;
		_powerPlans.Clear();
		_comparePlans.Clear();

		var plansList = new List<PowerPlan>();
		uint index = 0;

		uint size = (uint)sizeof(Guid);
		byte* pBuffer = stackalloc byte[(int)size];
		while (true)
		{
			uint res;
			{
				res = (uint)PInvoke.PowerEnumerate(default, null, null, POWER_DATA_ACCESSOR.ACCESS_SCHEME, index++, new Span<byte>(pBuffer, (int)size), ref size);
			}
			if (res != 0) break;

			Guid schemeGuid = new(new ReadOnlySpan<byte>(pBuffer, (int)size));
			plansList.Add(new PowerPlan
			{
				Guid = schemeGuid,
				Name = PowerHelper.ReadFriendlyName(schemeGuid, null, null),
				Description = PowerHelper.ReadDescription(schemeGuid)
			});
		}

		Guid* activePtr;
		PInvoke.PowerGetActiveScheme(default, out activePtr);
		Guid activeScheme = activePtr != null ? *activePtr : Guid.Empty;
		if (activePtr != null) PInvoke.LocalFree((HLOCAL)activePtr);

		foreach (PowerPlan? plan in plansList.OrderBy(p => p.Name, StringComparer.CurrentCultureIgnoreCase))
		{
			_powerPlans.Add(plan);
			_comparePlans.Add(plan);
		}

		var selectPlanToCompare = new PowerPlan
		{
			Guid = Guid.Empty,
			Name = "Select plan to compare",
			Description = "Select a power plan to compare against the active plan."
		};

		_comparePlans.Insert(0, selectPlanToCompare);
		PowerPlanComboBox.ItemsSource = _powerPlans;
		PowerPlanComboBox.SelectedItem = _powerPlans.FirstOrDefault(p => p.Guid == activeScheme);
		ComparePowerPlanComboBox.ItemsSource = _comparePlans;
		ComparePowerPlanComboBox.SelectedItem = selectPlanToCompare;
		isInitializingPowerPlans = false;

		if (PowerPlanComboBox.SelectedItem is PowerPlan active)
			LoadPowerPlanSettings(active.Guid);
	}

	private unsafe void LoadPowerPlanSettings(Guid scheme)
	{
		var allSubgroupsList = new List<PowerSubgroup>();
		var compareSubgroupsList = new List<PowerCompareSubgroup>();

		Guid noneSubgroupGuid = new("fea3413e-7e05-4911-9a71-700331f1c294");
		uint guidSize = (uint)Marshal.SizeOf<Guid>();
		var noneSubgroup = new PowerSubgroup
		{
			Guid = noneSubgroupGuid,
			Name = "None"
		};

		var noneCompareSubgroup = new PowerCompareSubgroup
		{
			Guid = noneSubgroupGuid,
			Name = "None",
			IsExpanded = true,
			IsVisible = true
		};

		uint settingIndex = 0;
		uint res;
		byte* pSetBuffer = stackalloc byte[(int)guidSize];

		while (true)
		{
			uint size = guidSize;

			{
				res = (uint)PInvoke.PowerEnumerate(default, (Guid?)scheme, null, POWER_DATA_ACCESSOR.ACCESS_INDIVIDUAL_SETTING, settingIndex++, new Span<byte>(pSetBuffer, (int)guidSize), ref size);
			}
			if (res != 0) break;

			Guid settingGuid = new(new ReadOnlySpan<byte>(pSetBuffer, (int)guidSize));
			var setting = new PowerSetting
			{
				SubgroupGuid = noneSubgroupGuid,
				Guid = settingGuid,
				Name = PowerHelper.ReadFriendlyName(scheme, noneSubgroupGuid, settingGuid),
				Description = PowerHelper.ReadDescription(scheme, noneSubgroupGuid, settingGuid),
				AcValueIndex = PowerHelper.ReadAcValueIndex(scheme, noneSubgroupGuid, settingGuid),
				DcValueIndex = PowerHelper.ReadDcValueIndex(scheme, noneSubgroupGuid, settingGuid),
				Min = PowerHelper.ReadValueMin(noneSubgroupGuid, settingGuid),
				Max = PowerHelper.ReadValueMax(noneSubgroupGuid, settingGuid),
				Increment = PowerHelper.ReadValueIncrement(noneSubgroupGuid, settingGuid),
				Unit = PowerHelper.ReadValueUnitsSpecifier(noneSubgroupGuid, settingGuid)
			};


			string friendlyAc = setting.AcValueIndex.ToString();
			string friendlyDc = setting.DcValueIndex.ToString();

			if (setting.IsOption)
			{
				friendlyAc = PowerHelper.ReadPossibleFriendlyName(noneSubgroupGuid, settingGuid, setting.AcValueIndex);
				friendlyDc = PowerHelper.ReadPossibleFriendlyName(noneSubgroupGuid, settingGuid, setting.DcValueIndex);
			}

			setting.FriendlyAcValue = friendlyAc;
			setting.FriendlyDcValue = friendlyDc;

			noneSubgroup.Settings.Add(setting);

			var compareSetting = new PowerCompareSetting
			{
				SubgroupGuid = noneSubgroupGuid,
				Guid = settingGuid,
				Name = setting.Name,
				Description = setting.Description,
				Min = setting.Min,
				Max = setting.Max,
				Increment = setting.Increment,
				Unit = setting.Unit,
				Plan1AcValue = setting.AcValueIndex,
				Plan1DcValue = setting.DcValueIndex,
				Plan1AcFriendlyValue = friendlyAc,
				Plan1DcFriendlyValue = friendlyDc,
				Plan2AcValue = 0,
				Plan2DcValue = 0,
				Plan2AcFriendlyValue = "",
				Plan2DcFriendlyValue = "",
				IsAcDifferent = false,
				IsDcDifferent = false,
				IsVisible = true
			};
		}

		allSubgroupsList.Add(noneSubgroup);
		compareSubgroupsList.Add(noneCompareSubgroup);

		uint subgroupIndex = 0;
		byte* pSgBuffer = stackalloc byte[(int)guidSize];
		byte* pInnerSetBuffer = stackalloc byte[(int)guidSize];

		while (true)
		{
			uint size = guidSize;

			{
				res = (uint)PInvoke.PowerEnumerate(default, (Guid?)scheme, null, POWER_DATA_ACCESSOR.ACCESS_SUBGROUP, subgroupIndex++, new Span<byte>(pSgBuffer, (int)guidSize), ref size);
			}
			if (res != 0) break;

			Guid subgroupGuid = new(new ReadOnlySpan<byte>(pSgBuffer, (int)guidSize));
			PowerSubgroup subgroup = new()
			{
				Guid = subgroupGuid,
				Name = subgroupGuid == new Guid("9596fb26-9850-41fd-ac3e-f7c3c00afd4b") ? "Multimedia settings" : PowerHelper.ReadFriendlyName(scheme, subgroupGuid, null)
			};

			if (string.IsNullOrWhiteSpace(subgroup.Name))
			{
				continue;
			}

			PowerCompareSubgroup compareSubgroup = new()
			{
				Guid = subgroupGuid,
				Name = subgroup.Name,
				IsExpanded = true,
				IsVisible = true
			};


			uint settingIdx = 0;
			while (true)
			{
				size = guidSize;
				{
					res = (uint)PInvoke.PowerEnumerate(default, (Guid?)scheme, (Guid?)subgroupGuid, POWER_DATA_ACCESSOR.ACCESS_INDIVIDUAL_SETTING, settingIdx++, new Span<byte>(pInnerSetBuffer, (int)guidSize), ref size);
				}
				if (res != 0) break;

				Guid settingGuid = new(new ReadOnlySpan<byte>(pInnerSetBuffer, (int)guidSize));
				var setting = new PowerSetting
				{
					SubgroupGuid = subgroupGuid,
					Guid = settingGuid,
					Name = PowerHelper.ReadFriendlyName(scheme, subgroupGuid, settingGuid),
					Description = PowerHelper.ReadDescription(scheme, subgroupGuid, settingGuid),
					AcValueIndex = PowerHelper.ReadAcValueIndex(scheme, subgroupGuid, settingGuid),
					DcValueIndex = PowerHelper.ReadDcValueIndex(scheme, subgroupGuid, settingGuid),
					Min = PowerHelper.ReadValueMin(subgroupGuid, settingGuid),
					Max = PowerHelper.ReadValueMax(subgroupGuid, settingGuid),
					Increment = PowerHelper.ReadValueIncrement(subgroupGuid, settingGuid),
					Unit = PowerHelper.ReadValueUnitsSpecifier(subgroupGuid, settingGuid)
				};


				string friendlyAc = setting.AcValueIndex.ToString();
				string friendlyDc = setting.DcValueIndex.ToString();

				if (setting.IsOption)
				{
					friendlyAc = PowerHelper.ReadPossibleFriendlyName(subgroupGuid, settingGuid, setting.AcValueIndex);
					friendlyDc = PowerHelper.ReadPossibleFriendlyName(subgroupGuid, settingGuid, setting.DcValueIndex);
				}

				setting.FriendlyAcValue = friendlyAc;
				setting.FriendlyDcValue = friendlyDc;

				subgroup.Settings.Add(setting);

				var compareSetting = new PowerCompareSetting
				{
					SubgroupGuid = subgroupGuid,
					Guid = settingGuid,
					Name = setting.Name,
					Description = setting.Description,
					Min = setting.Min,
					Max = setting.Max,
					Increment = setting.Increment,
					Unit = setting.Unit,
					Plan1AcValue = setting.AcValueIndex,
					Plan1DcValue = setting.DcValueIndex,
					Plan1AcFriendlyValue = friendlyAc,
					Plan1DcFriendlyValue = friendlyDc,
					Plan2AcValue = 0,
					Plan2DcValue = 0,
					Plan2AcFriendlyValue = "",
					Plan2DcFriendlyValue = "",
					IsAcDifferent = false,
					IsDcDifferent = false,
					IsVisible = true
				};
				compareSubgroup.Settings.Add(compareSetting);
			}


			allSubgroupsList.Add(subgroup);
			compareSubgroupsList.Add(compareSubgroup);
		}



		_allSubgroups.Clear();
		Subgroups.Clear();
		CompareSubgroups.Clear();

		foreach (PowerSubgroup sg in allSubgroupsList)
		{
			var sortedSettings = sg.Settings.OrderBy(s => s.Name, StringComparer.CurrentCultureIgnoreCase).ToList();
			sg.Settings.Clear();
			foreach (PowerSetting? setting in sortedSettings)
			{
				sg.Settings.Add(setting);
			}

			_allSubgroups.Add(sg);
			Subgroups.Add(sg);
		}

		foreach (PowerCompareSubgroup csg in compareSubgroupsList)
		{
			var sortedSettings = csg.Settings.OrderBy(s => s.Name, StringComparer.CurrentCultureIgnoreCase).ToList();
			csg.Settings.Clear();
			foreach (PowerCompareSetting? setting in sortedSettings)
			{
				csg.Settings.Add(setting);
			}
			CompareSubgroups.Add(csg);
		}
	}

	private readonly PowerSubgroup noResultItem = new()
	{
		Name = "No result found",
		Settings = [],
		IsVisible = true,
		FontWeight = FontWeights.Normal
	};

	private readonly PowerCompareSubgroup noResultCompareItem = new()
	{
		Name = "No result found",
		Settings = [],
		IsVisible = true,
		FontWeight = FontWeights.Normal
	};

	private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
	{
		if (ComparePowerPlanComboBox.SelectedItem is PowerPlan comparePlanEmpty && comparePlanEmpty.Guid == Guid.Empty && CompareTreeView.Visibility == Visibility.Visible)
			CompareTreeView.Visibility = Visibility.Collapsed;

		string query = Search.Text.Trim();
		bool anyVisible = false;

		foreach (PowerSubgroup subgroup in _allSubgroups)
		{
			foreach (PowerSetting setting in subgroup.Settings)
			{
				setting.IsVisible = string.IsNullOrEmpty(query) ||
									setting.Name.Contains(query, StringComparison.CurrentCultureIgnoreCase) ||
									setting.Guid.ToString().Contains(query, StringComparison.CurrentCultureIgnoreCase) ||
									setting.SubgroupGuid.ToString().Contains(query, StringComparison.CurrentCultureIgnoreCase);
			}

			subgroup.IsVisible = subgroup.Settings.Any(s => s.IsVisible);

			if (subgroup.IsVisible) anyVisible = true;
		}

		if (!anyVisible)
		{
			if (!Subgroups.Contains(noResultItem))
				Subgroups.Add(noResultItem);
		}
		else
		{
			Subgroups.Remove(noResultItem);
		}

		if (ComparePowerPlanComboBox.SelectedItem is PowerPlan comparePlan && comparePlan.Guid != Guid.Empty)
		{
			bool anyCompareVisible = false;
			foreach (PowerCompareSubgroup subgroup in CompareSubgroups)
			{
				if (subgroup == _identicalPlansPlaceholder || subgroup == noResultCompareItem) continue;

				foreach (PowerCompareSetting setting in subgroup.Settings)
				{
					bool isDifferent = setting.IsAcDifferent || setting.IsDcDifferent;

					bool matchesSearch = string.IsNullOrEmpty(query) ||
										setting.Name.Contains(query, StringComparison.CurrentCultureIgnoreCase) ||
										setting.Guid.ToString().Contains(query, StringComparison.CurrentCultureIgnoreCase) ||
										setting.SubgroupGuid.ToString().Contains(query, StringComparison.CurrentCultureIgnoreCase);

					setting.IsVisible = isDifferent && matchesSearch;
				}

				subgroup.IsVisible = subgroup.Settings.Any(s => s.IsVisible);
				if (subgroup.IsVisible) anyCompareVisible = true;
			}

			if (!anyCompareVisible && !CompareSubgroups.Contains(_identicalPlansPlaceholder))
			{
				if (!CompareSubgroups.Contains(noResultCompareItem))
					CompareSubgroups.Add(noResultCompareItem);
			}
			else
			{
				CompareSubgroups.Remove(noResultCompareItem);
			}
		}
	}

	private async void PowerPlanComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (isInitializingPowerPlans) return;

		if (PowerPlanComboBox.SelectedItem is PowerPlan selectedPlan)
		{
			Guid schemeGuid = selectedPlan.Guid;
			PowerHelper.PowerSetActiveScheme(schemeGuid);

			foreach (PowerSubgroup subgroup in _allSubgroups)
			{
				foreach (PowerSetting setting in subgroup.Settings)
				{
					setting.AcValueIndex = PowerHelper.ReadAcValueIndex(schemeGuid, subgroup.Guid, setting.Guid);
					setting.DcValueIndex = PowerHelper.ReadDcValueIndex(schemeGuid, subgroup.Guid, setting.Guid);

					if (setting.IsOption)
					{
						setting.FriendlyAcValue = PowerHelper.ReadPossibleFriendlyName(subgroup.Guid, setting.Guid, setting.AcValueIndex);
						setting.FriendlyDcValue = PowerHelper.ReadPossibleFriendlyName(subgroup.Guid, setting.Guid, setting.DcValueIndex);
					}
					else
					{
						setting.FriendlyAcValue = setting.AcValueIndex.ToString();
						setting.FriendlyDcValue = setting.DcValueIndex.ToString();
					}
				}
			}

			if (ComparePowerPlanComboBox.SelectedItem is PowerPlan comparePlan && comparePlan.Guid != Guid.Empty)
			{
				await UpdateCompareData(comparePlan);
				ActiveTreeView.Visibility = Visibility.Collapsed;
				ActiveTreeView.Opacity = 0;
				ActiveTreeView.IsHitTestVisible = false;
				CompareTreeView.Opacity = 1;
				CompareTreeView.IsHitTestVisible = true;
			}
			else
			{
				ActiveTreeView.Visibility = Visibility.Visible;
				ActiveTreeView.Opacity = 1;
				ActiveTreeView.IsHitTestVisible = true;
				CompareTreeView.Opacity = 0;
				CompareTreeView.IsHitTestVisible = false;
			}

			SearchBox_TextChanged(null, null);
		}
	}

	private async void ComparePowerPlanComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (isInitializingPowerPlans) return;

		if (ComparePowerPlanComboBox.SelectedItem is PowerPlan comparePlan && comparePlan.Guid != Guid.Empty)
		{
			await UpdateCompareData(comparePlan);
			ActiveTreeView.Visibility = Visibility.Collapsed;
			ActiveTreeView.Opacity = 0;
			ActiveTreeView.IsHitTestVisible = false;
			CompareTreeView.Opacity = 1;
			CompareTreeView.IsHitTestVisible = true;
			CompareTreeView.Visibility = Visibility.Visible;
		}
		else
		{
			ActiveTreeView.Visibility = Visibility.Visible;
			ActiveTreeView.Opacity = 1;
			ActiveTreeView.IsHitTestVisible = true;
			CompareTreeView.Opacity = 0;
			CompareTreeView.IsHitTestVisible = false;
			CompareTreeView.Visibility = Visibility.Collapsed;
		}

		SearchBox_TextChanged(null, null);
	}

	private async Task UpdateCompareData(PowerPlan comparePlan)
	{
		if (comparePlan == null || comparePlan.Guid == Guid.Empty)
		{
			foreach (PowerCompareSubgroup sg in CompareSubgroups)
			{
				sg.IsVisible = true;
				foreach (PowerCompareSetting setting in sg.Settings)
					setting.IsVisible = true;
			}

			if (_identicalPlansPlaceholder != null && CompareSubgroups.Contains(_identicalPlansPlaceholder))
				CompareSubgroups.Remove(_identicalPlansPlaceholder);

			return;
		}

		var activePlan = PowerPlanComboBox.SelectedItem as PowerPlan;
		if (activePlan == null)
			return;

		foreach (PowerCompareSubgroup sg in CompareSubgroups)
		{
			if (sg == _identicalPlansPlaceholder) continue;
			foreach (PowerCompareSetting setting in sg.Settings)
			{
				uint p1Ac = PowerHelper.ReadAcValueIndex(activePlan.Guid, setting.SubgroupGuid, setting.Guid);
				uint p1Dc = PowerHelper.ReadDcValueIndex(activePlan.Guid, setting.SubgroupGuid, setting.Guid);
				uint p2Ac = PowerHelper.ReadAcValueIndex(comparePlan.Guid, setting.SubgroupGuid, setting.Guid);
				uint p2Dc = PowerHelper.ReadDcValueIndex(comparePlan.Guid, setting.SubgroupGuid, setting.Guid);

				setting.Plan1AcFriendlyValue = setting.IsOption ? PowerHelper.ReadPossibleFriendlyName(setting.SubgroupGuid, setting.Guid, p1Ac) : p1Ac.ToString();
				setting.Plan1DcFriendlyValue = setting.IsOption ? PowerHelper.ReadPossibleFriendlyName(setting.SubgroupGuid, setting.Guid, p1Dc) : p1Dc.ToString();
				setting.Plan2AcFriendlyValue = setting.IsOption ? PowerHelper.ReadPossibleFriendlyName(setting.SubgroupGuid, setting.Guid, p2Ac) : p2Ac.ToString();
				setting.Plan2DcFriendlyValue = setting.IsOption ? PowerHelper.ReadPossibleFriendlyName(setting.SubgroupGuid, setting.Guid, p2Dc) : p2Dc.ToString();

				setting.Plan1AcValue = p1Ac;
				setting.Plan1DcValue = p1Dc;
				setting.Plan2AcValue = p2Ac;
				setting.Plan2DcValue = p2Dc;

				setting.IsAcDifferent = p1Ac != p2Ac;
				setting.IsDcDifferent = p1Dc != p2Dc;
				setting.IsVisible = setting.IsAcDifferent || setting.IsDcDifferent;

			}
		}

		if (_identicalPlansPlaceholder == null)
		{
			_identicalPlansPlaceholder = new PowerCompareSubgroup
			{
				Name = "Power plans are identical",
				FontWeight = FontWeights.Normal,
				IsVisible = true
			};
		}

		bool anyDifferent = false;

		foreach (PowerCompareSubgroup sg in CompareSubgroups)
		{
			if (sg == _identicalPlansPlaceholder) continue;
			sg.IsVisible = sg.Settings.Any(s => s.IsVisible);
			if (sg.IsVisible) anyDifferent = true;
		}

		if (!anyDifferent)
		{
			_identicalPlansPlaceholder.IsVisible = true;
			if (!CompareSubgroups.Contains(_identicalPlansPlaceholder))
				CompareSubgroups.Add(_identicalPlansPlaceholder);
		}
		else
		{
			CompareSubgroups.Remove(_identicalPlansPlaceholder);
		}
	}

	private async void Edit_Click(object sender, RoutedEventArgs e)
	{
		if (PowerPlanComboBox.SelectedItem is not PowerPlan plan)
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

		var panel = new StackPanel
		{
			Spacing = 4
		};

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

		ContentDialogResult result = await dialog.ShowAsync();
		if (result != ContentDialogResult.Primary)
			return;

		object selectedCompare = ComparePowerPlanComboBox.SelectedItem;

		PowerHelper.WriteSchemeFriendlyName(plan.Guid, nameTextBox.Text);
		PowerHelper.WriteSchemeDescription(plan.Guid, descriptionBox.Text);

		_powerPlans.Remove(plan);
		_comparePlans.Remove(plan);

		plan.Name = nameTextBox.Text;
		plan.Description = descriptionBox.Text;

		int powerIndex = _powerPlans.Count(p => string.Compare(p.Name, plan.Name, StringComparison.CurrentCultureIgnoreCase) < 0);
		_powerPlans.Insert(powerIndex, plan);

		int compareIndex = _comparePlans.Count(p => p.Guid == Guid.Empty || string.Compare(p.Name, plan.Name, StringComparison.CurrentCultureIgnoreCase) < 0);
		_comparePlans.Insert(compareIndex, plan);

		PowerPlanComboBox.SelectedItem = plan;
		ComparePowerPlanComboBox.SelectedItem = selectedCompare;
	}

	private async void Duplicate_Click(object sender, RoutedEventArgs e)
	{
		if (PowerPlanComboBox.SelectedItem is not PowerPlan plan)
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

		ContentDialogResult result = await dialog.ShowAsync();
		if (result != ContentDialogResult.Primary)
			return;

		int i = 1;
		while (_powerPlans.Any(p => string.Equals(p.Name, i == 1 ? $"{plan.Name} - Copy" : $"{plan.Name} - Copy ({i})", StringComparison.CurrentCultureIgnoreCase)))
			i++;

		var newPlan = new PowerPlan
		{
			Guid = PowerHelper.DuplicateScheme(plan.Guid, i == 1 ? $"{plan.Name} - Copy" : $"{plan.Name} - Copy ({i})", plan.Description),
			Name = i == 1 ? $"{plan.Name} - Copy" : $"{plan.Name} - Copy ({i})",
			Description = plan.Description
		};

		int powerIndex = _powerPlans.Count(p => string.Compare(p.Name, newPlan.Name, StringComparison.CurrentCultureIgnoreCase) < 0);
		_powerPlans.Insert(powerIndex, newPlan);

		int compareIndex = _comparePlans.Count(p => p.Guid == Guid.Empty || string.Compare(p.Name, newPlan.Name, StringComparison.CurrentCultureIgnoreCase) < 0);
		_comparePlans.Insert(compareIndex, newPlan);

		PowerPlanComboBox.SelectedItem = newPlan;
	}

	private async void Restore_Click(object sender, RoutedEventArgs e)
	{
		var dialog = new ContentDialog
		{
			Title = "Restore power plans",
			Content = "Are you sure you want to restore the default power schemes?.",
			PrimaryButtonText = "Restore",
			CloseButtonText = "Cancel",
			DefaultButton = ContentDialogButton.Close,
			XamlRoot = XamlRoot
		};

		ContentDialogResult result = await dialog.ShowAsync();
		if (result != ContentDialogResult.Primary)
			return;

		PowerHelper.RestoreDefaultPowerSchemes();
		LoadPowerPlans();
	}

	private async void Delete_Click(object sender, RoutedEventArgs e)
	{
		if (PowerPlanComboBox.SelectedItem is not PowerPlan plan)
			return;

		if (_powerPlans.Count <= 1)
		{
			var errorDialog = new ContentDialog
			{
				Title = "Unable to delete power plan",
				Content = "At least one other power plan must exist.",
				CloseButtonText = "OK",
				DefaultButton = ContentDialogButton.Close,
				XamlRoot = XamlRoot
			};
			await errorDialog.ShowAsync();
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

		ContentDialogResult result = await dialog.ShowAsync();
		if (result != ContentDialogResult.Primary)
			return;

		int currentIndex = _powerPlans.IndexOf(plan);

		PowerPlan nextSelection;
		if (currentIndex > 0)
			nextSelection = _powerPlans[currentIndex - 1];
		else
			nextSelection = _powerPlans[currentIndex + 1];

		PowerPlanComboBox.SelectedItem = nextSelection;

		PowerHelper.DeleteScheme(plan.Guid);

		_powerPlans.Remove(plan);
		_comparePlans.Remove(_comparePlans.FirstOrDefault(p => p.Guid == plan.Guid));

		PowerPlanComboBox.SelectedItem = nextSelection;
		ComparePowerPlanComboBox.SelectedItem = _comparePlans.First(p => p.Guid == Guid.Empty);
	}

	private static unsafe Guid ImportPowerSchemeUnsafe(string filePath)
	{
		Guid* destSchemePtr = null;
		uint res = (uint)PInvoke.PowerImportPowerScheme(default, filePath, ref destSchemePtr);
		if (res != 0 || destSchemePtr == null)
			return Guid.Empty;

		try
		{
			return *destSchemePtr;
		}
		finally
		{
			PInvoke.LocalFree((HLOCAL)destSchemePtr);
		}
	}

	private async void Import_Click(object sender, RoutedEventArgs e)
	{
		var picker = new FilePicker(App.MainWindow)
		{
			ShowAllFilesOption = false
		};
		picker.FileTypeChoices.Add("Power Scheme Files", ["*.pow"]);

		StorageFile? file = await picker.PickSingleFileAsync();
		if (file == null)
			return;

		Guid importedGuid = ImportPowerSchemeUnsafe(file.Path);
		if (importedGuid == Guid.Empty)
			return;

		var plan = new PowerPlan
		{
			Guid = importedGuid,
			Name = PowerHelper.ReadFriendlyName(importedGuid, null, null),
			Description = PowerHelper.ReadDescription(importedGuid)
		};

		int powerIndex = _powerPlans.Count(p => string.Compare(p.Name, plan.Name, StringComparison.CurrentCultureIgnoreCase) < 0);
		_powerPlans.Insert(powerIndex, plan);

		int compareIndex = _comparePlans.Count(p => p.Guid == Guid.Empty || string.Compare(p.Name, plan.Name, StringComparison.CurrentCultureIgnoreCase) < 0);
		_comparePlans.Insert(compareIndex, plan);

		PowerPlanComboBox.SelectedItem = plan;
		ComparePowerPlanComboBox.SelectedItem = _comparePlans.FirstOrDefault(p => p.Guid == Guid.Empty);
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

		StorageFile? file = await picker.PickSaveFileAsync();
		if (file == null)
			return;

		var psi = new ProcessStartInfo
		{
			FileName = "powercfg.exe",
			Arguments = @$"-export ""{file.Path}.pow"" {plan.Guid:D}",
			UseShellExecute = false,
			CreateNoWindow = true
		};

		using var proc = Process.Start(psi);
		if (proc != null)
			await proc.WaitForExitAsync();
	}

	private async void TreeView_ItemInvoked(object sender, TreeViewItemInvokedEventArgs args)
	{
		switch (args.InvokedItem)
		{
			case PowerSubgroup subgroup:
				subgroup.IsExpanded = !subgroup.IsExpanded;
				break;

			case PowerSetting setting:
				var activePlan = PowerPlanComboBox.SelectedItem as PowerPlan;
				Guid activeScheme = activePlan.Guid;

				var dialog = new PowerDialog(setting);

				var contentDialog = new ContentDialog
				{
					Content = dialog,
					PrimaryButtonText = "Apply",
					CloseButtonText = "Cancel",
					DefaultButton = ContentDialogButton.Close,
					XamlRoot = XamlRoot,
					Title = setting.Name
				};
				contentDialog.Resources["ContentDialogMaxWidth"] = 600;

				ContentDialogResult result = await contentDialog.ShowAsync();
				if (result != ContentDialogResult.Primary) return;

				uint newAcValue = dialog.GetAcValue();
				uint newDcValue = dialog.GetDcValue();

				PowerHelper.WriteACValueIndex(activeScheme, setting.SubgroupGuid, setting.Guid, newAcValue);
				PowerHelper.WriteDCValueIndex(activeScheme, setting.SubgroupGuid, setting.Guid, newDcValue);
				PowerHelper.PowerSetActiveScheme(activeScheme);

				setting.AcValueIndex = newAcValue;
				setting.DcValueIndex = newDcValue;
				setting.FriendlyAcValue = setting.IsOption ? PowerHelper.ReadPossibleFriendlyName(setting.SubgroupGuid, setting.Guid, newAcValue) : newAcValue.ToString();
				setting.FriendlyDcValue = setting.IsOption ? PowerHelper.ReadPossibleFriendlyName(setting.SubgroupGuid, setting.Guid, newDcValue) : newDcValue.ToString();
				break;
		}
	}

	private async void CompareTreeView_ItemInvoked(object sender, TreeViewItemInvokedEventArgs args)
	{
		switch (args.InvokedItem)
		{
			case PowerCompareSubgroup subgroup:
				subgroup.IsExpanded = !subgroup.IsExpanded;
				break;

			case PowerCompareSetting compareSettings:
				var activePlan = PowerPlanComboBox.SelectedItem as PowerPlan;
				Guid activeScheme = activePlan.Guid;

				var proxySetting = new PowerSetting
				{
					SubgroupGuid = compareSettings.SubgroupGuid,
					Guid = compareSettings.Guid,
					Name = compareSettings.Name,
					Description = compareSettings.Description,
					Min = compareSettings.Min,
					Max = compareSettings.Max,
					Increment = compareSettings.Increment,
					Unit = compareSettings.Unit,
					AcValueIndex = compareSettings.Plan1AcValue,
					DcValueIndex = compareSettings.Plan1DcValue
				};

				var dialog = new PowerDialog(proxySetting);

				var contentDialog = new ContentDialog
				{
					Content = dialog,
					PrimaryButtonText = "Apply",
					CloseButtonText = "Cancel",
					DefaultButton = ContentDialogButton.Close,
					XamlRoot = XamlRoot,
					Title = compareSettings.Name
				};
				contentDialog.Resources["ContentDialogMaxWidth"] = 600;

				ContentDialogResult result = await contentDialog.ShowAsync();
				if (result != ContentDialogResult.Primary) return;

				uint newAcValue = dialog.GetAcValue();
				uint newDcValue = dialog.GetDcValue();

				PowerHelper.WriteACValueIndex(activeScheme, compareSettings.SubgroupGuid, compareSettings.Guid, newAcValue);
				PowerHelper.WriteDCValueIndex(activeScheme, compareSettings.SubgroupGuid, compareSettings.Guid, newDcValue);
				PowerHelper.PowerSetActiveScheme(activeScheme);

				compareSettings.Plan1AcValue = newAcValue;
				compareSettings.Plan1DcValue = newDcValue;

				compareSettings.Plan1AcFriendlyValue = compareSettings.IsOption ? PowerHelper.ReadPossibleFriendlyName(compareSettings.SubgroupGuid, compareSettings.Guid, newAcValue) : newAcValue.ToString();
				compareSettings.Plan1DcFriendlyValue = compareSettings.IsOption ? PowerHelper.ReadPossibleFriendlyName(compareSettings.SubgroupGuid, compareSettings.Guid, newDcValue) : newDcValue.ToString();

				compareSettings.IsAcDifferent = compareSettings.Plan1AcValue != compareSettings.Plan2AcValue;
				compareSettings.IsDcDifferent = compareSettings.Plan1DcValue != compareSettings.Plan2DcValue;
				compareSettings.IsVisible = compareSettings.IsAcDifferent || compareSettings.IsDcDifferent;

				PowerSubgroup? settingsSubgroup = _allSubgroups.FirstOrDefault(sg => sg.Guid == compareSettings.SubgroupGuid);
				PowerSetting? settings = settingsSubgroup?.Settings.FirstOrDefault(s => s.Guid == compareSettings.Guid);

				if (settings != null)
				{
					settings.AcValueIndex = newAcValue;
					settings.DcValueIndex = newDcValue;
					settings.FriendlyAcValue = compareSettings.Plan1AcFriendlyValue;
					settings.FriendlyDcValue = compareSettings.Plan1DcFriendlyValue;
				}

				if (_identicalPlansPlaceholder == null)
				{
					_identicalPlansPlaceholder = new PowerCompareSubgroup
					{
						Name = "Power plans are identical",
						FontWeight = FontWeights.Normal,
						IsVisible = true
					};
				}

				bool anyDifferent = false;
				foreach (PowerCompareSubgroup sg in CompareSubgroups)
				{
					if (sg == _identicalPlansPlaceholder) continue;
					sg.IsVisible = sg.Settings.Any(s => s.IsVisible);
					if (sg.IsVisible) anyDifferent = true;
				}

				if (!anyDifferent)
				{
					_identicalPlansPlaceholder.IsVisible = true;
					if (!CompareSubgroups.Contains(_identicalPlansPlaceholder))
						CompareSubgroups.Add(_identicalPlansPlaceholder);
				}
				else
				{
					CompareSubgroups.Remove(_identicalPlansPlaceholder);
				}
				break;
		}
	}

	private void CopyGuid_Click(object sender, RoutedEventArgs e)
	{
		if (sender is FrameworkElement { DataContext: PowerModelItem item })
		{
			var dataPackage = new Windows.ApplicationModel.DataTransfer.DataPackage();
			dataPackage.SetText(item.Guid.ToString());
			Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);
		}
		else if (sender is FrameworkElement { DataContext: PowerCompareModelItem compareItem })
		{
			var dataPackage = new Windows.ApplicationModel.DataTransfer.DataPackage();
			dataPackage.SetText(compareItem.Guid.ToString());
			Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);
		}
	}

	private void CopyName_Click(object sender, RoutedEventArgs e)
	{
		if (sender is FrameworkElement { DataContext: PowerModelItem item })
		{
			var dataPackage = new Windows.ApplicationModel.DataTransfer.DataPackage();
			dataPackage.SetText(item.Name ?? string.Empty);
			Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);
		}
		else if (sender is FrameworkElement { DataContext: PowerCompareModelItem compareItem })
		{
			var dataPackage = new Windows.ApplicationModel.DataTransfer.DataPackage();
			dataPackage.SetText(compareItem.Name ?? string.Empty);
			Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);
		}
	}

	private void CopyDescription_Click(object sender, RoutedEventArgs e)
	{
		if (sender is FrameworkElement { DataContext: PowerModelItem item })
		{
			var dataPackage = new Windows.ApplicationModel.DataTransfer.DataPackage();
			dataPackage.SetText(item.Description ?? string.Empty);
			Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);
		}
		else if (sender is FrameworkElement { DataContext: PowerCompareModelItem compareItem })
		{
			var dataPackage = new Windows.ApplicationModel.DataTransfer.DataPackage();
			dataPackage.SetText(compareItem.Description ?? string.Empty);
			Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);
		}
	}

	private void CopyAcValue_Click(object sender, RoutedEventArgs e)
	{
		if (sender is FrameworkElement { DataContext: PowerSetting item })
		{
			var dataPackage = new Windows.ApplicationModel.DataTransfer.DataPackage();
			string text = !string.IsNullOrWhiteSpace(item.FriendlyAcValue) ? item.FriendlyAcValue : item.AcValueIndex.ToString();
			dataPackage.SetText(text);
			Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);
		}
		else if (sender is FrameworkElement { DataContext: PowerCompareSetting compareItem })
		{
			var dataPackage = new Windows.ApplicationModel.DataTransfer.DataPackage();
			string text = !string.IsNullOrWhiteSpace(compareItem.Plan1AcFriendlyValue) ? compareItem.Plan1AcFriendlyValue : compareItem.Plan1AcValue.ToString();
			dataPackage.SetText(text);
			Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);
		}
	}

	private void CopyDcValue_Click(object sender, RoutedEventArgs e)
	{
		if (sender is FrameworkElement { DataContext: PowerSetting item })
		{
			var dataPackage = new Windows.ApplicationModel.DataTransfer.DataPackage();
			string text = !string.IsNullOrWhiteSpace(item.FriendlyDcValue) ? item.FriendlyDcValue : item.DcValueIndex.ToString();
			dataPackage.SetText(text);
			Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);
		}
		else if (sender is FrameworkElement { DataContext: PowerCompareSetting compareItem })
		{
			var dataPackage = new Windows.ApplicationModel.DataTransfer.DataPackage();
			string text = !string.IsNullOrWhiteSpace(compareItem.Plan1DcFriendlyValue) ? compareItem.Plan1DcFriendlyValue : compareItem.Plan1DcValue.ToString();
			dataPackage.SetText(text);
			Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);
		}
	}
	private void CopyAcValueDescription_Click(object sender, RoutedEventArgs e)
	{
		if (sender is FrameworkElement { DataContext: PowerSetting item })
		{
			var dataPackage = new Windows.ApplicationModel.DataTransfer.DataPackage();
			string text;
			if (item.IsOption)
			{
				text = PowerHelper.ReadPossibleDescription(item.SubgroupGuid, item.Guid, item.AcValueIndex);
			}
			else
			{
				string unit = !string.IsNullOrWhiteSpace(item.Unit) ? char.ToUpper(item.Unit[0]) + item.Unit[1..] : string.Empty;
				text = $"Range: {item.Min} - {item.Max}\nIncrement: {item.Increment}\nUnit: {unit}";
			}

			dataPackage.SetText(text ?? string.Empty);
			Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);
		}
		else if (sender is FrameworkElement { DataContext: PowerCompareSetting compareItem })
		{
			var dataPackage = new Windows.ApplicationModel.DataTransfer.DataPackage();
			string text;
			if (compareItem.IsOption)
			{
				text = PowerHelper.ReadPossibleDescription(compareItem.SubgroupGuid, compareItem.Guid, compareItem.Plan1AcValue);
			}
			else
			{
				string unit = !string.IsNullOrWhiteSpace(compareItem.Unit) ? char.ToUpper(compareItem.Unit[0]) + compareItem.Unit[1..] : string.Empty;
				text = $"Range: {compareItem.Min} - {compareItem.Max}\nIncrement: {compareItem.Increment}\nUnit: {unit}";
			}

			dataPackage.SetText(text ?? string.Empty);
			Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);
		}
	}

	private void CopyDcValueDescription_Click(object sender, RoutedEventArgs e)
	{
		if (sender is FrameworkElement { DataContext: PowerSetting item })
		{
			var dataPackage = new Windows.ApplicationModel.DataTransfer.DataPackage();
			string text;
			if (item.IsOption)
			{
				text = PowerHelper.ReadPossibleDescription(item.SubgroupGuid, item.Guid, item.DcValueIndex);
			}
			else
			{
				string unit = !string.IsNullOrWhiteSpace(item.Unit) ? char.ToUpper(item.Unit[0]) + item.Unit[1..] : string.Empty;
				text = $"Range: {item.Min} - {item.Max}\nIncrement: {item.Increment}\nUnit: {unit}";
			}

			dataPackage.SetText(text ?? string.Empty);
			Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);
		}
		else if (sender is FrameworkElement { DataContext: PowerCompareSetting compareItem })
		{
			var dataPackage = new Windows.ApplicationModel.DataTransfer.DataPackage();
			string text;
			if (compareItem.IsOption)
			{
				text = PowerHelper.ReadPossibleDescription(compareItem.SubgroupGuid, compareItem.Guid, compareItem.Plan1DcValue);
			}
			else
			{
				string unit = !string.IsNullOrWhiteSpace(compareItem.Unit) ? char.ToUpper(compareItem.Unit[0]) + compareItem.Unit[1..] : string.Empty;
				text = $"Range: {compareItem.Min} - {compareItem.Max}\nIncrement: {compareItem.Increment}\nUnit: {unit}";
			}

			dataPackage.SetText(text ?? string.Empty);
			Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);
		}
	}
}
