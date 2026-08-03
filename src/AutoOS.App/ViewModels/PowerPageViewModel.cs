using AutoOS.Core.Helpers.Power;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using Syncfusion.UI.Xaml.TreeGrid;

namespace AutoOS.ViewModels;

public enum PowerPageMode
{
	Normal,
	Comparison,
	ViewChanges
}

public enum PowerNodeKind
{
	Subgroup,
	Setting,
	Message
}

public enum PowerProjectionKind
{
	Normal,
	Comparison,
	ViewChanges
}

public enum PowerFilterMode
{
	Contains,
	ExactMatch
}

public sealed partial class PowerPageViewModel : ObservableObject
{
	private static readonly PowerPlan EmptyComparePlan = new()
	{
		Guid = Guid.Empty,
		Name = "Select plan to compare",
		Description = "Select a power plan to compare against the active plan.",
		IsPlaceholder = true
	};

	private readonly Stack<List<PowerSettingValueState>> _undoStates = [];
	private readonly Stack<List<PowerSettingValueState>> _redoStates = [];
	private readonly Dictionary<PowerSettingKey, PowerValues> _comparisonValues = [];
	private readonly Dictionary<PowerExpansionKey, bool> _expansion = [];
	private readonly List<PowerSettingState> _settings = [];
	private PowerTreeNode _editingNode;
	private IReadOnlyList<PowerSubgroupState> _subgroups = [];

	public Action RefreshFilterAction { get; set; }

	public ObservableCollection<PowerPlan> PowerPlans { get; } = [];

	public ObservableCollection<PowerPlan> ComparePlans { get; } = [];

	public ObservableCollection<PowerTreeNode> TreeNodes { get; } = [];

	public ObservableCollection<PowerTreeNode> CompareNodes { get; } = [];

	public ObservableCollection<PowerTreeNode> ChangeNodes { get; } = [];

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(CanSave))]
	public partial bool IsLoaded { get; set; }

	[ObservableProperty]
	public partial PowerPlan ActivePlan { get; set; }

	[ObservableProperty]
	public partial PowerPlan ComparePlan { get; set; }

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(Mode))]
	public partial bool ViewChanges { get; set; }

	[ObservableProperty]
	public partial string SearchText { get; set; } = string.Empty;

	[ObservableProperty]
	public partial bool FilterSetting { get; set; } = true;

	[ObservableProperty]
	public partial bool FilterDescription { get; set; }

	[ObservableProperty]
	public partial bool FilterAc { get; set; }

	[ObservableProperty]
	public partial bool FilterDc { get; set; }

	[ObservableProperty]
	public partial bool FilterGuid { get; set; } = true;

	[ObservableProperty]
	public partial PowerFilterMode FilterMode { get; set; } = PowerFilterMode.Contains;

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(ViewChangesLabel))]
	[NotifyPropertyChangedFor(nameof(CanSave))]
	public partial int ModifiedCount { get; set; }

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(CanSave))]
	public partial bool HasValidationErrors { get; set; }

	[ObservableProperty]
	public partial bool HasSearchResults { get; set; } = true;

	[ObservableProperty]
	public partial string ComparisonState { get; set; } = "Content";

	public bool CanUndo => IsLoaded && _undoStates.Count > 0;

	public bool CanRedo => IsLoaded && _redoStates.Count > 0;

	public bool CanSave => IsLoaded && ModifiedCount > 0 && !HasValidationErrors;

	public string ViewChangesLabel => $"View Changes ({ModifiedCount})";

	public string ActivePlanAcHeader => $"{ActivePlan?.Name ?? "Power Plan 1"} (AC)";

	public string ActivePlanDcHeader => $"{ActivePlan?.Name ?? "Power Plan 1"} (DC)";

	public string ComparePlanAcHeader => $"{(ComparePlan is { IsPlaceholder: false } ? ComparePlan.Name : "Power Plan 2")} (AC)";

	public string ComparePlanDcHeader => $"{(ComparePlan is { IsPlaceholder: false } ? ComparePlan.Name : "Power Plan 2")} (DC)";

	public Visibility NormalVisibility => Mode == PowerPageMode.Normal ? Visibility.Visible : Visibility.Collapsed;

	public Visibility ComparisonVisibility => Mode == PowerPageMode.Comparison ? Visibility.Visible : Visibility.Collapsed;

	public Visibility ViewChangesVisibility => Mode == PowerPageMode.ViewChanges ? Visibility.Visible : Visibility.Collapsed;

	public bool FilterContains
	{
		get => FilterMode == PowerFilterMode.Contains;
		set
		{
			if (value)
				FilterMode = PowerFilterMode.Contains;
		}
	}

	public bool FilterExactMatch
	{
		get => FilterMode == PowerFilterMode.ExactMatch;
		set
		{
			if (value)
				FilterMode = PowerFilterMode.ExactMatch;
		}
	}

	public PowerPageMode Mode => ViewChanges ? PowerPageMode.ViewChanges : ComparePlan is { IsPlaceholder: false } ? PowerPageMode.Comparison : PowerPageMode.Normal;

	partial void OnSearchTextChanged(string value) => RefreshFilter();

	partial void OnFilterSettingChanged(bool value) => RefreshFilter();

	partial void OnFilterDescriptionChanged(bool value) => RefreshFilter();

	partial void OnFilterAcChanged(bool value) => RefreshFilter();

	partial void OnFilterDcChanged(bool value) => RefreshFilter();

	partial void OnFilterGuidChanged(bool value) => RefreshFilter();

	partial void OnFilterModeChanged(PowerFilterMode value)
	{
		OnPropertyChanged(nameof(FilterContains));
		OnPropertyChanged(nameof(FilterExactMatch));
		RefreshFilter();
	}

	public void SetIsLoaded(bool isLoaded)
	{
		IsLoaded = isLoaded;
		OnPropertyChanged(nameof(CanUndo));
		OnPropertyChanged(nameof(CanRedo));
	}

	public void SetPlans(IEnumerable<PowerPlan> plans, Guid activeGuid, bool selectFallback = true)
	{
		IsLoaded = false;
		PowerPlans.Clear();
		ComparePlans.Clear();

		foreach (var plan in plans)
			PowerPlans.Add(plan);

		ActivePlan = PowerPlans.FirstOrDefault(plan => plan.Guid == activeGuid);
		if (ActivePlan == null && selectFallback)
			ActivePlan = PowerPlans.FirstOrDefault();
		ComparePlans.Add(EmptyComparePlan);
		foreach (var plan in PowerPlans.Where(plan => plan.Guid != ActivePlan?.Guid))
			ComparePlans.Add(plan);
		ComparePlan = EmptyComparePlan;
		if (ActivePlan == null)
		{
			_subgroups = [];
			_settings.Clear();
			TreeNodes.Clear();
			CompareNodes.Clear();
			ChangeNodes.Clear();
			ModifiedCount = 0;
			ResetHistory();
		}
		NotifyModeChanged();
		NotifyPlanHeaders();
	}

	public void LoadActivePlan(PowerPlan plan, IReadOnlyList<PowerSubgroupState> subgroups)
	{
		IsLoaded = false;
		ActivePlan = plan;
		RefreshComparePlans();
		_subgroups = subgroups;
		_settings.Clear();
		_settings.AddRange(subgroups.SelectMany(subgroup => subgroup.Settings));
		_comparisonValues.Clear();
		ComparePlan = ComparePlans.FirstOrDefault(item => item.IsPlaceholder);
		ViewChanges = false;
		NotifyModeChanged();
		ResetHistory();
		RefreshState();
		IsLoaded = true;
		NotifyPlanHeaders();
		OnPropertyChanged(nameof(CanSave));
		OnPropertyChanged(nameof(CanUndo));
		OnPropertyChanged(nameof(CanRedo));
	}

	public void SetComparePlan(PowerPlan plan)
	{
		ComparePlan = plan ?? ComparePlans.FirstOrDefault(item => item.IsPlaceholder);
		if (ComparePlan is { IsPlaceholder: false })
			ViewChanges = false;
		_comparisonValues.Clear();

		if (ComparePlan is { IsPlaceholder: false })
		{
			foreach (var setting in _settings)
			{
				if (PowerHelper.TryReadAcValueIndex(ComparePlan.Guid, setting.SubgroupGuid, setting.Guid, out uint acValue) &&
					PowerHelper.TryReadDcValueIndex(ComparePlan.Guid, setting.SubgroupGuid, setting.Guid, out uint dcValue))
				{
					_comparisonValues[setting.Key] = new PowerValues(acValue, dcValue);
				}
			}
		}

		NotifyModeChanged();
		NotifyPlanHeaders();
		RebuildTrees();
	}

	public void SetViewChanges(bool value)
	{
		ViewChanges = value;
		if (value)
		{
			ComparePlan = ComparePlans.FirstOrDefault(item => item.IsPlaceholder);
			_comparisonValues.Clear();
		}
		NotifyModeChanged();
		NotifyPlanHeaders();
		RebuildTrees();
	}

	public void NotifyPlanHeaders()
	{
		OnPropertyChanged(nameof(ActivePlanAcHeader));
		OnPropertyChanged(nameof(ActivePlanDcHeader));
		OnPropertyChanged(nameof(ComparePlanAcHeader));
		OnPropertyChanged(nameof(ComparePlanDcHeader));
	}

	public void RefreshComparePlans()
	{
		ComparePlans.Clear();
		ComparePlans.Add(EmptyComparePlan);
		foreach (var plan in PowerPlans.Where(plan => plan.Guid != ActivePlan?.Guid))
			ComparePlans.Add(plan);
		ComparePlan = EmptyComparePlan;
	}

	public async Task LoadPowerPlansAsync(Guid? preferredGuid = null)
	{
		SetIsLoaded(false);
		(List<PowerPlan> plans, Guid activeGuid) = await Task.Run(ReadPowerPlans);

		bool hasTarget = preferredGuid.HasValue || activeGuid != Guid.Empty;
		SetPlans(plans, preferredGuid ?? activeGuid, hasTarget);
		if (ActivePlan == null)
			return;

		IReadOnlyList<PowerSubgroupState> settings = await Task.Run(() => LoadPowerPlanSettings(ActivePlan.Guid));
		LoadActivePlan(ActivePlan, settings);
	}

	public async Task SetActivePlanAsync(PowerPlan plan)
	{
		if (plan == null || plan == ActivePlan)
			return;

		PowerHelper.PowerSetActiveScheme(plan.Guid);
		SetIsLoaded(false);
		IReadOnlyList<PowerSubgroupState> settings = await Task.Run(() => LoadPowerPlanSettings(plan.Guid));
		LoadActivePlan(plan, settings);
	}

	public async Task RestoreDefaultPlansAsync()
	{
		PowerHelper.RestoreDefaultPowerSchemes();
		await LoadPowerPlansAsync();
	}

	public async Task ImportPowerPlanAsync(string filePath)
	{
		Guid importedGuid = PowerHelper.ImportPowerScheme(filePath);
		PowerHelper.PowerSetActiveScheme(importedGuid);
		await LoadPowerPlansAsync(importedGuid);
	}

	public async Task DuplicatePlanAsync(PowerPlan plan)
	{
		int number = 1;
		string name;
		do
		{
			name = number == 1 ? $"{plan.Name} - Copy" : $"{plan.Name} - Copy ({number})";
			number++;
		}
		while (PowerPlans.Any(item => item.Name.Equals(name, StringComparison.CurrentCultureIgnoreCase)));

		Guid guid = PowerHelper.DuplicateScheme(plan.Guid, name, plan.Description);
		PowerHelper.PowerSetActiveScheme(guid);
		await LoadPowerPlansAsync(guid);
	}

	public async Task DeletePlanAsync(PowerPlan plan)
	{
		if (PowerPlans.Count <= 1)
			return;

		int index = PowerPlans.IndexOf(plan);
		PowerPlan nextPlan = index > 0 ? PowerPlans[index - 1] : PowerPlans[index + 1];
		PowerHelper.PowerSetActiveScheme(nextPlan.Guid);
		PowerHelper.DeleteScheme(plan.Guid);
		await LoadPowerPlansAsync(nextPlan.Guid);
	}

	public void UpdatePlanMetadata(PowerPlan plan, string name, string description)
	{
		PowerHelper.WriteSchemeFriendlyName(plan.Guid, name);
		PowerHelper.WriteSchemeDescription(plan.Guid, description);
		plan.Name = PowerHelper.ReadFriendlyName(plan.Guid, null, null);
		plan.Description = PowerHelper.ReadDescription(plan.Guid);
		NotifyPlanHeaders();
	}

	private static (List<PowerPlan> Plans, Guid ActiveGuid) ReadPowerPlans()
	{
		List<PowerPlan> plans = [];
		foreach (Guid guid in PowerHelper.EnumerateSchemes())
			plans.Add(new PowerPlan
			{
				Guid = guid,
				Name = PowerHelper.ReadFriendlyName(guid, null, null),
				Description = PowerHelper.ReadDescription(guid)
			});

		return (plans, PowerHelper.ReadActiveScheme());
	}

	private static List<PowerSubgroupState> LoadPowerPlanSettings(Guid scheme)
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

		foreach (Guid subgroupGuid in PowerHelper.EnumerateSubgroups(scheme))
		{
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

		subgroups.Remove(noneSubgroup);
		subgroups.Insert(0, noneSubgroup);
		return subgroups;
	}

	private static List<PowerSettingState> EnumerateSettings(Guid scheme, Guid subgroupGuid, Guid? enumerationSubgroup)
	{
		List<PowerSettingState> settings = [];
		foreach (Guid settingGuid in PowerHelper.EnumerateSettings(scheme, enumerationSubgroup))
		{
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

	private void NotifyModeChanged()
	{
		OnPropertyChanged(nameof(Mode));
		OnPropertyChanged(nameof(NormalVisibility));
		OnPropertyChanged(nameof(ComparisonVisibility));
		OnPropertyChanged(nameof(ViewChangesVisibility));
	}

	public void BeginEdit(PowerTreeNode node, string mappingName)
	{
        _editingNode?.ErrorsChanged -= EditingNode_ErrorsChanged;
		_editingNode = node;
		if (_editingNode == null)
			return;
		_editingNode.ErrorsChanged += EditingNode_ErrorsChanged;
		_editingNode.BeginCellEdit(mappingName);
		HasValidationErrors = _editingNode.HasErrors;
	}

	public bool CommitEdit(PowerTreeNode node, string mappingName)
	{
		if (node?.Setting == null)
			return false;

		var previous = CaptureState();
		if (!node.CommitCellEdit(mappingName, out bool changed))
		{
			HasValidationErrors = node.HasErrors;
			return false;
		}

		FinishEdit();
		if (!changed)
			return false;

		_undoStates.Push(previous);
		_redoStates.Clear();
		RefreshState(false);
		return true;
	}

	public void CancelEdit(PowerTreeNode node)
	{
		(node ?? _editingNode)?.CancelCellEdit();
		FinishEdit();
	}

	public void Undo()
	{
		if (!CanUndo)
			return;

		_redoStates.Push(CaptureState());
		RestoreState(_undoStates.Pop());
		RefreshState();
	}

	public void Redo()
	{
		if (!CanRedo)
			return;

		_undoStates.Push(CaptureState());
		RestoreState(_redoStates.Pop());
		RefreshState();
	}

	public void DiscardChanges()
	{
		foreach (var setting in _settings)
			setting.SetValues(setting.OriginalAcValue, setting.OriginalDcValue);

		ResetHistory();
		RefreshState();
	}

	public PowerSaveResult SaveChanges()
	{
		if (!CanSave || ActivePlan == null)
			return new PowerSaveResult(false, "There are no power setting changes to save.");

		var modified = _settings.Where(setting => setting.IsModified).ToList();
		var errors = new List<string>();
		var successfulWrites = new List<SuccessfulPowerWrite>();

		foreach (var setting in modified)
		{
			if (setting.AcValue != setting.OriginalAcValue)
			{
				uint result = PowerHelper.WriteACValueIndex(ActivePlan.Guid, setting.SubgroupGuid, setting.Guid, setting.AcValue);
				if (result != 0)
					errors.Add($"{setting.Name} (AC): error {result}");
				else
					successfulWrites.Add(new SuccessfulPowerWrite(setting, true, setting.OriginalAcValue));
			}

			if (setting.DcValue != setting.OriginalDcValue)
			{
				uint result = PowerHelper.WriteDCValueIndex(ActivePlan.Guid, setting.SubgroupGuid, setting.Guid, setting.DcValue);
				if (result != 0)
					errors.Add($"{setting.Name} (DC): error {result}");
				else
					successfulWrites.Add(new SuccessfulPowerWrite(setting, false, setting.OriginalDcValue));
			}
		}

		if (errors.Count > 0)
		{
			errors.AddRange(RollbackWrites(successfulWrites));
			return new PowerSaveResult(false, string.Join(Environment.NewLine, errors));
		}

		foreach (var setting in modified)
		{
			bool readAc = PowerHelper.TryReadAcValueIndex(ActivePlan.Guid, setting.SubgroupGuid, setting.Guid, out uint acValue);
			bool readDc = PowerHelper.TryReadDcValueIndex(ActivePlan.Guid, setting.SubgroupGuid, setting.Guid, out uint dcValue);
			if (!readAc || !readDc || acValue != setting.AcValue || dcValue != setting.DcValue)
			{
				var readbackErrors = new List<string> { $"Windows did not retain the requested value for {setting.Name}." };
				readbackErrors.AddRange(RollbackWrites(successfulWrites));
				return new PowerSaveResult(false, string.Join(Environment.NewLine, readbackErrors));
			}
		}

		uint activationResult = PowerHelper.PowerSetActiveScheme(ActivePlan.Guid);
		if (activationResult != 0)
		{
			var activationErrors = new List<string> { $"Windows could not activate the updated power plan: error {activationResult}" };
			activationErrors.AddRange(RollbackWrites(successfulWrites));
			return new PowerSaveResult(false, string.Join(Environment.NewLine, activationErrors));
		}

		foreach (var setting in modified)
			setting.AcceptCurrentValues();

		ResetHistory();
		RefreshState();
		return new PowerSaveResult(true, string.Empty);
	}

	private IEnumerable<string> RollbackWrites(IEnumerable<SuccessfulPowerWrite> writes)
	{
		var errors = new List<string>();
		bool wroteAny = false;
		foreach (var write in writes.Reverse())
		{
			wroteAny = true;
			uint result;
			if (write.IsAc)
				result = PowerHelper.WriteACValueIndex(ActivePlan.Guid, write.Setting.SubgroupGuid, write.Setting.Guid, write.OriginalValue);
			else
				result = PowerHelper.WriteDCValueIndex(ActivePlan.Guid, write.Setting.SubgroupGuid, write.Setting.Guid, write.OriginalValue);
			if (result != 0)
				errors.Add($"Rollback failed for {write.Setting.Name} ({(write.IsAc ? "AC" : "DC")}): error {result}");
		}

		if (wroteAny && errors.Count == 0)
		{
			uint activationResult = PowerHelper.PowerSetActiveScheme(ActivePlan.Guid);
			if (activationResult != 0)
				errors.Add($"Rollback activation failed: error {activationResult}");
		}
		return errors;
	}

	public bool MatchesFilter(object item)
	{
		if (item is not PowerTreeNode node)
			return true;

		string query = SearchText?.Trim() ?? string.Empty;
		if (query.Length == 0)
			return true;

		if (node.NodeKind == PowerNodeKind.Message)
			return false;

		return NodeOrDescendantMatches(node, query);
	}

	private bool NodeOrDescendantMatches(PowerTreeNode node, string query)
	{
		if (NodeMatches(node, query))
			return true;

		return node.Children.Any(child => NodeOrDescendantMatches(child, query));
	}

	private bool NodeMatches(PowerTreeNode node, string query)
	{
		var setting = node.Setting;
		if (setting == null)
			return false;

		if (FilterSetting && TextMatches(setting.Name, query))
			return true;
		if (FilterDescription && TextMatches(setting.Description, query))
			return true;
		if (FilterGuid && (TextMatches(setting.Guid.ToString(), query) || TextMatches(setting.SubgroupGuid.ToString(), query)))
			return true;
		if (FilterAc && ValuesMatch(setting, query, true))
			return true;
		if (FilterDc && ValuesMatch(setting, query, false))
			return true;

		return false;
	}

	private bool ValuesMatch(PowerSettingState setting, string query, bool isAc)
	{
		uint[] values = Mode switch
		{
			PowerPageMode.Comparison when _comparisonValues.TryGetValue(setting.Key, out var comparison) => isAc ? new[] { setting.AcValue, comparison.AcValue } : new[] { setting.DcValue, comparison.DcValue },
			PowerPageMode.ViewChanges => isAc ? new[] { setting.OriginalAcValue, setting.AcValue } : new[] { setting.OriginalDcValue, setting.DcValue },
			_ => isAc ? new[] { setting.AcValue } : new[] { setting.DcValue }
		};

		return values.Any(value => TextMatches(setting.GetDisplayValue(value), query) || TextMatches(value.ToString(CultureInfo.InvariantCulture), query));
	}

	private bool TextMatches(string text, string query)
	{
		if (string.IsNullOrWhiteSpace(text))
			return false;

		return FilterMode == PowerFilterMode.ExactMatch ? text.Equals(query, StringComparison.OrdinalIgnoreCase) : text.Contains(query, StringComparison.OrdinalIgnoreCase);
	}

	public void RefreshFilter()
	{
		string query = SearchText?.Trim() ?? string.Empty;
		IEnumerable<PowerTreeNode> nodes = Mode switch
		{
			PowerPageMode.Comparison => CompareNodes,
			PowerPageMode.ViewChanges => ChangeNodes,
			_ => TreeNodes
		};
		HasSearchResults = query.Length == 0 || nodes.Any(node => MatchesFilter(node));
		RefreshFilterAction?.Invoke();
	}

	public void RefreshAfterEdit()
	{
		if (Mode == PowerPageMode.Normal)
			RefreshFilter();
		else
			RebuildTrees();
	}

	private void RefreshState(bool rebuildTree = true)
	{
		ModifiedCount = _settings.Count(setting => setting.IsModified);
		OnPropertyChanged(nameof(CanUndo));
		OnPropertyChanged(nameof(CanRedo));
		OnPropertyChanged(nameof(CanSave));
		if (rebuildTree)
			RebuildTrees();
	}

	private void EditingNode_ErrorsChanged(object sender, DataErrorsChangedEventArgs e)
	{
		HasValidationErrors = _editingNode?.HasErrors == true;
	}

	private void FinishEdit()
	{
		if (_editingNode != null)
			_editingNode.ErrorsChanged -= EditingNode_ErrorsChanged;
		_editingNode = null;
		HasValidationErrors = false;
	}

	private void RebuildTrees()
	{
		CaptureExpansion(TreeNodes, _expansion);
		CaptureExpansion(CompareNodes, _expansion);
		CaptureExpansion(ChangeNodes, _expansion);

		TreeNodes.Clear();
		CompareNodes.Clear();
		ChangeNodes.Clear();
		BuildNormalTree(_expansion);
		BuildComparisonTree(_expansion);
		BuildChangesTree(_expansion);

		RefreshFilter();
	}

	private void BuildNormalTree(IReadOnlyDictionary<PowerExpansionKey, bool> expansion)
	{
		foreach (var subgroup in _subgroups)
		{
			var subgroupNode = PowerTreeNode.CreateSubgroup(subgroup, GetExpansion(expansion, PowerNodeKind.Subgroup, subgroup.Guid));
			foreach (var setting in subgroup.Settings)
				subgroupNode.Children.Add(PowerTreeNode.CreateSetting(setting));
			TreeNodes.Add(subgroupNode);
		}

		if (TreeNodes.Count == 0)
			TreeNodes.Add(PowerTreeNode.CreateMessage("No power settings found"));
	}

	private void BuildComparisonTree(IReadOnlyDictionary<PowerExpansionKey, bool> expansion)
	{
		foreach (var subgroup in _subgroups)
		{
			var subgroupNode = PowerTreeNode.CreateSubgroup(subgroup, GetExpansion(expansion, PowerNodeKind.Subgroup, subgroup.Guid));
			foreach (var setting in subgroup.Settings)
			{
				if (!_comparisonValues.TryGetValue(setting.Key, out var compareValues))
					continue;

				bool isAcDifferent = !string.Equals(setting.GetDisplayValue(setting.AcValue), setting.GetDisplayValue(compareValues.AcValue), StringComparison.Ordinal);
				bool isDcDifferent = !string.Equals(setting.GetDisplayValue(setting.DcValue), setting.GetDisplayValue(compareValues.DcValue), StringComparison.Ordinal);
				if (!isAcDifferent && !isDcDifferent)
					continue;

				subgroupNode.Children.Add(PowerTreeNode.CreateComparison(setting, compareValues.AcValue, compareValues.DcValue, isAcDifferent, isDcDifferent));
			}

			if (subgroupNode.Children.Count > 0)
				CompareNodes.Add(subgroupNode);
		}

		ComparisonState = CompareNodes.Count == 0 ? "Identical" : "Content";
	}

	private void BuildChangesTree(IReadOnlyDictionary<PowerExpansionKey, bool> expansion)
	{
		foreach (var subgroup in _subgroups)
		{
			var subgroupNode = PowerTreeNode.CreateSubgroup(subgroup, GetExpansion(expansion, PowerNodeKind.Subgroup, subgroup.Guid));
			foreach (var setting in subgroup.Settings.Where(setting => setting.IsModified))
			{
				bool isAcDifferent = setting.AcValue != setting.OriginalAcValue;
				bool isDcDifferent = setting.DcValue != setting.OriginalDcValue;
				subgroupNode.Children.Add(PowerTreeNode.CreateViewChanges(setting, isAcDifferent, isDcDifferent));
			}

			if (subgroupNode.Children.Count > 0)
				ChangeNodes.Add(subgroupNode);
		}

		if (ChangeNodes.Count == 0)
			ChangeNodes.Add(PowerTreeNode.CreateMessage("No changes"));
	}

	private static void CaptureExpansion(IEnumerable<PowerTreeNode> nodes, IDictionary<PowerExpansionKey, bool> expansion)
	{
		foreach (var node in nodes)
		{
			if (node.Children.Count > 0)
				expansion[GetExpansionKey(node)] = node.IsExpanded;
			CaptureExpansion(node.Children, expansion);
		}
	}

	private static PowerExpansionKey GetExpansionKey(PowerTreeNode node) => new(node.NodeKind, node.Guid, node.Setting?.SubgroupGuid ?? Guid.Empty);

	private static bool GetExpansion(IReadOnlyDictionary<PowerExpansionKey, bool> expansion, PowerNodeKind kind, Guid guid, Guid parentGuid = default) => !expansion.TryGetValue(new PowerExpansionKey(kind, guid, parentGuid), out bool expanded) || expanded;

	private List<PowerSettingValueState> CaptureState() => [.. _settings.Select(setting => new PowerSettingValueState(setting, setting.AcValue, setting.DcValue))];

	private static void RestoreState(IEnumerable<PowerSettingValueState> state)
	{
		foreach (var item in state)
			item.Setting.SetValues(item.AcValue, item.DcValue);
	}

	private void ResetHistory()
	{
		FinishEdit();
		_undoStates.Clear();
		_redoStates.Clear();
		OnPropertyChanged(nameof(CanUndo));
		OnPropertyChanged(nameof(CanRedo));
	}

	private sealed record PowerSettingValueState(PowerSettingState Setting, uint AcValue, uint DcValue);

	private sealed record SuccessfulPowerWrite(PowerSettingState Setting, bool IsAc, uint OriginalValue);
}

public readonly record struct PowerSettingKey(Guid SubgroupGuid, Guid SettingGuid);

public readonly record struct PowerExpansionKey(PowerNodeKind NodeKind, Guid Guid, Guid ParentGuid);

public readonly record struct PowerValues(uint AcValue, uint DcValue);

public sealed record PowerSaveResult(bool Succeeded, string ErrorMessage);

[WinRT.GeneratedBindableCustomProperty]
public sealed partial class PowerPlan : ObservableObject
{
	[ObservableProperty]
	public partial Guid Guid { get; set; }

	[ObservableProperty]
	public partial string Name { get; set; } = string.Empty;

	[ObservableProperty]
	public partial string Description { get; set; } = string.Empty;

	[ObservableProperty]
	public partial bool IsPlaceholder { get; set; }
}

public sealed partial class PowerSubgroupState : ObservableObject
{
	[ObservableProperty]
	public partial Guid Guid { get; set; }

	[ObservableProperty]
	public partial string Name { get; set; } = string.Empty;

	[ObservableProperty]
	public partial string Description { get; set; } = string.Empty;

	public ObservableCollection<PowerSettingState> Settings { get; } = [];
}

public sealed partial class PowerSettingState : ObservableObject
{
	private readonly Dictionary<uint, string> _descriptionCache = [];
	private readonly Dictionary<uint, string> _friendlyNameCache = [];
	private bool _optionsLoaded;

	public PowerSettingKey Key => new(SubgroupGuid, Guid);

	[ObservableProperty]
	public partial Guid SubgroupGuid { get; set; }

	[ObservableProperty]
	public partial Guid Guid { get; set; }

	[ObservableProperty]
	public partial string Name { get; set; } = string.Empty;

	[ObservableProperty]
	public partial string Description { get; set; } = string.Empty;

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(IsModified))]
	public partial uint AcValue { get; set; }

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(IsModified))]
	public partial uint DcValue { get; set; }

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(IsModified))]
	public partial uint OriginalAcValue { get; set; }

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(IsModified))]
	public partial uint OriginalDcValue { get; set; }

	[ObservableProperty]
	public partial uint? Minimum { get; set; }

	[ObservableProperty]
	public partial uint? Maximum { get; set; }

	[ObservableProperty]
	public partial uint? Increment { get; set; }

	[ObservableProperty]
	public partial string Unit { get; set; } = string.Empty;

	public ObservableCollection<PowerSettingOption> Options { get; } = [];

	public bool HasOptions => Options.Count > 0;

	public bool IsOptionSetting => !(Minimum.HasValue && Maximum.HasValue && Increment.HasValue && Maximum.Value > Minimum.Value && Increment.Value > 0);

	public bool IsModified => AcValue != OriginalAcValue || DcValue != OriginalDcValue;

	public void LoadOptions()
	{
		if (_optionsLoaded)
			return;
		_optionsLoaded = true;
		Options.Clear();
		if (!IsOptionSetting)
			return;
		for (uint index = 0; index < 4096; index++)
		{
			string friendlyName = PowerHelper.ReadPossibleFriendlyName(SubgroupGuid, Guid, index);
			if (string.IsNullOrWhiteSpace(friendlyName))
				break;

			string description = PowerHelper.ReadPossibleDescription(SubgroupGuid, Guid, index);
			_friendlyNameCache[index] = friendlyName;
			_descriptionCache[index] = description;
			Options.Add(new PowerSettingOption
			{
				Index = index,
				FriendlyName = friendlyName,
				Description = description
			});
		}
	}

	public void PrimeDisplayData()
	{
		GetDisplayValue(AcValue);
		GetDisplayValue(DcValue);
		GetValueToolTip(AcValue);
		GetValueToolTip(DcValue);
	}

	public string GetDisplayValue(uint value)
	{
		if (!IsOptionSetting)
			return value.ToString(CultureInfo.InvariantCulture);

		if (!_friendlyNameCache.TryGetValue(value, out string friendlyName))
		{
			friendlyName = PowerHelper.ReadPossibleFriendlyName(SubgroupGuid, Guid, value);
			_friendlyNameCache[value] = friendlyName ?? string.Empty;
		}

		return string.IsNullOrWhiteSpace(friendlyName) ? value.ToString(CultureInfo.InvariantCulture) : friendlyName;
	}

	public string GetValueToolTip(uint value)
	{
		if (IsOptionSetting)
		{
			if (!_descriptionCache.TryGetValue(value, out string description))
			{
				description = PowerHelper.ReadPossibleDescription(SubgroupGuid, Guid, value);
				_descriptionCache[value] = description ?? string.Empty;
			}
			return description ?? string.Empty;
		}

		var lines = new List<string>();
		if (Minimum.HasValue && Maximum.HasValue)
			lines.Add($"Range: {Minimum.Value} - {Maximum.Value}");
		if (Increment.HasValue)
			lines.Add($"Increment: {Increment.Value}");
		if (!string.IsNullOrWhiteSpace(Unit))
			lines.Add($"Unit: {char.ToUpperInvariant(Unit[0])}{Unit[1..]}");
		return string.Join(Environment.NewLine, lines);
	}

	public PowerSettingOption GetOption(uint value) => Options.FirstOrDefault(option => option.Index == value);

	public bool TryParseValue(string text, out uint value, out string error)
	{
		if (!uint.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
		{
			error = "Enter a whole number from 0 to 4294967295.";
			return false;
		}

		if (Minimum.HasValue && value < Minimum.Value)
		{
			error = $"Value must be at least {Minimum.Value}.";
			return false;
		}

		if (Maximum.HasValue && value > Maximum.Value)
		{
			error = $"Value must not exceed {Maximum.Value}.";
			return false;
		}

		if (Increment.HasValue && Increment.Value > 0 && Minimum.HasValue && (value - Minimum.Value) % Increment.Value != 0)
		{
			error = $"Value must use increments of {Increment.Value} from {Minimum.Value}.";
			return false;
		}

		error = string.Empty;
		return true;
	}

	public void SetValues(uint acValue, uint dcValue)
	{
		AcValue = acValue;
		DcValue = dcValue;
		OnPropertyChanged(nameof(IsModified));
	}

	public void AcceptCurrentValues()
	{
		OriginalAcValue = AcValue;
		OriginalDcValue = DcValue;
		OnPropertyChanged(nameof(IsModified));
	}
}

[WinRT.GeneratedBindableCustomProperty]
public sealed partial class PowerSettingOption : ObservableObject
{
	[ObservableProperty]
	public partial uint Index { get; set; }

	[ObservableProperty]
	public partial string FriendlyName { get; set; } = string.Empty;

	[ObservableProperty]
	public partial string Description { get; set; } = string.Empty;
}

[WinRT.GeneratedBindableCustomProperty]
public sealed partial class PowerTreeNode : ObservableObject, INotifyDataErrorInfo
{
	private readonly Dictionary<string, string> _errors = [];
	private uint _compareAcValue;
	private uint _compareDcValue;
	private string _editAcValue = string.Empty;
	private string _editDcValue = string.Empty;
	private PowerSettingOption _editAcOption;
	private PowerSettingOption _editDcOption;

	public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

	public PowerNodeKind NodeKind { get; private init; }

	public PowerProjectionKind ProjectionKind { get; private init; }

	public PowerSettingState Setting { get; private init; }

	public ObservableCollection<PowerTreeNode> Children { get; } = [];

	[ObservableProperty]
	public partial Guid Guid { get; set; }

	[ObservableProperty]
	public partial string DisplayName { get; set; } = string.Empty;

	[ObservableProperty]
	public partial string Description { get; set; } = string.Empty;

	[ObservableProperty]
	public partial bool IsExpanded { get; set; } = true;

	public bool HasValues => NodeKind == PowerNodeKind.Setting;

	public uint AcValue => Setting?.AcValue ?? 0;

	public uint DcValue => Setting?.DcValue ?? 0;

	public string DisplayAc => HasValues ? Setting?.GetDisplayValue(AcValue) ?? string.Empty : string.Empty;

	public string DisplayDc => HasValues ? Setting?.GetDisplayValue(DcValue) ?? string.Empty : string.Empty;

	public string AcToolTip => HasValues ? Setting?.GetValueToolTip(AcValue) ?? string.Empty : string.Empty;

	public string DcToolTip => HasValues ? Setting?.GetValueToolTip(DcValue) ?? string.Empty : string.Empty;

	public string DisplayCompareAc => HasValues ? Setting?.GetDisplayValue(_compareAcValue) ?? string.Empty : string.Empty;

	public string DisplayCompareDc => HasValues ? Setting?.GetDisplayValue(_compareDcValue) ?? string.Empty : string.Empty;

	public string CompareAcToolTip => HasValues ? Setting?.GetValueToolTip(_compareAcValue) ?? string.Empty : string.Empty;

	public string CompareDcToolTip => HasValues ? Setting?.GetValueToolTip(_compareDcValue) ?? string.Empty : string.Empty;

	public string DisplayOriginalAc => HasValues ? Setting?.GetDisplayValue(Setting.OriginalAcValue) ?? string.Empty : string.Empty;

	public string DisplayOriginalDc => HasValues ? Setting?.GetDisplayValue(Setting.OriginalDcValue) ?? string.Empty : string.Empty;

	public string OriginalAcToolTip => HasValues ? Setting?.GetValueToolTip(Setting.OriginalAcValue) ?? string.Empty : string.Empty;

	public string OriginalDcToolTip => HasValues ? Setting?.GetValueToolTip(Setting.OriginalDcValue) ?? string.Empty : string.Empty;

	public ObservableCollection<PowerSettingOption> Options => Setting?.Options;

	public bool HasOptions => Setting?.HasOptions == true;

	public bool IsAcDifferent { get; private init; }

	public bool IsDcDifferent { get; private init; }

	public bool HasAcError => _errors.ContainsKey(nameof(DisplayAc));

	public bool HasDcError => _errors.ContainsKey(nameof(DisplayDc));

	public string EditAcValue
	{
		get => _editAcValue;
		set
		{
			if (SetProperty(ref _editAcValue, value))
				ValidateValue(nameof(DisplayAc), value);
		}
	}

	public string EditDcValue
	{
		get => _editDcValue;
		set
		{
			if (SetProperty(ref _editDcValue, value))
				ValidateValue(nameof(DisplayDc), value);
		}
	}

	public PowerSettingOption EditAcOption
	{
		get => _editAcOption;
		set
		{
			if (SetProperty(ref _editAcOption, value))
				OnPropertyChanged(nameof(EditAcToolTip));
		}
	}

	public PowerSettingOption EditDcOption
	{
		get => _editDcOption;
		set
		{
			if (SetProperty(ref _editDcOption, value))
				OnPropertyChanged(nameof(EditDcToolTip));
		}
	}

	public string EditAcToolTip => HasOptions ? EditAcOption?.Description ?? string.Empty : Setting?.GetValueToolTip(AcValue) ?? string.Empty;

	public string EditDcToolTip => HasOptions ? EditDcOption?.Description ?? string.Empty : Setting?.GetValueToolTip(DcValue) ?? string.Empty;

	public bool HasErrors => _errors.Count > 0;

	public static PowerTreeNode CreateSubgroup(PowerSubgroupState subgroup, bool isExpanded) => new()
	{
		NodeKind = PowerNodeKind.Subgroup,
		Guid = subgroup.Guid,
		DisplayName = subgroup.Name,
		Description = subgroup.Description,
		IsExpanded = isExpanded
	};

	public static PowerTreeNode CreateSetting(PowerSettingState setting) => new()
	{
		NodeKind = PowerNodeKind.Setting,
		ProjectionKind = PowerProjectionKind.Normal,
		Guid = setting.Guid,
		DisplayName = setting.Name,
		Description = setting.Description,
		Setting = setting
	};

	public static PowerTreeNode CreateComparison(PowerSettingState setting, uint compareAcValue, uint compareDcValue, bool isAcDifferent, bool isDcDifferent) => new()
	{
		NodeKind = PowerNodeKind.Setting,
		ProjectionKind = PowerProjectionKind.Comparison,
		Guid = setting.Guid,
		DisplayName = setting.Name,
		Description = setting.Description,
		Setting = setting,
		_compareAcValue = compareAcValue,
		_compareDcValue = compareDcValue,
		IsAcDifferent = isAcDifferent,
		IsDcDifferent = isDcDifferent
	};

	public static PowerTreeNode CreateViewChanges(PowerSettingState setting, bool isAcDifferent, bool isDcDifferent) => new()
	{
		NodeKind = PowerNodeKind.Setting,
		ProjectionKind = PowerProjectionKind.ViewChanges,
		Guid = setting.Guid,
		DisplayName = setting.Name,
		Description = setting.Description,
		Setting = setting,
		IsAcDifferent = isAcDifferent,
		IsDcDifferent = isDcDifferent
	};

	public static PowerTreeNode CreateMessage(string message) => new()
	{
		NodeKind = PowerNodeKind.Message,
		DisplayName = message
	};

	public void BeginCellEdit(string mappingName)
	{
		ClearErrors();
		Setting.LoadOptions();
		OnPropertyChanged(nameof(HasOptions));
		OnPropertyChanged(nameof(Options));
		if (mappingName == nameof(DisplayAc))
		{
			if (HasOptions)
				EditAcOption = Setting.GetOption(Setting.AcValue);
			else
				EditAcValue = Setting.AcValue.ToString(CultureInfo.InvariantCulture);
		}
		else if (mappingName == nameof(DisplayDc))
		{
			if (HasOptions)
				EditDcOption = Setting.GetOption(Setting.DcValue);
			else
				EditDcValue = Setting.DcValue.ToString(CultureInfo.InvariantCulture);
		}
	}

	public void CancelCellEdit() => ClearErrors();

	public bool CommitCellEdit(string mappingName, out bool changed)
	{
		changed = false;
		if (Setting == null || NodeKind != PowerNodeKind.Setting || Children.Count > 0)
			return false;

		if (mappingName == nameof(DisplayAc))
		{
			if (!TryGetEditedValue(mappingName, EditAcValue, EditAcOption, out uint value))
				return false;
			changed = value != Setting.AcValue;
			if (changed)
				Setting.AcValue = value;
		}
		else if (mappingName == nameof(DisplayDc))
		{
			if (!TryGetEditedValue(mappingName, EditDcValue, EditDcOption, out uint value))
				return false;
			changed = value != Setting.DcValue;
			if (changed)
				Setting.DcValue = value;
		}

		if (changed)
		{
			OnPropertyChanged(nameof(DisplayAc));
			OnPropertyChanged(nameof(DisplayDc));
			OnPropertyChanged(nameof(AcToolTip));
			OnPropertyChanged(nameof(DcToolTip));
		}

		return true;
	}

	public IEnumerable GetErrors(string propertyName)
	{
		if (propertyName != null && _errors.TryGetValue(propertyName, out string error))
			return new[] { error };
		return Array.Empty<string>();
	}

	private bool TryGetEditedValue(string mappingName, string text, PowerSettingOption option, out uint value)
	{
		if (HasOptions)
		{
			value = option?.Index ?? 0;
			if (option != null)
			{
				ClearError(mappingName);
				return true;
			}

			SetError(mappingName, "Select a value.");
			return false;
		}

		if (Setting.TryParseValue(text, out value, out string error))
		{
			ClearError(mappingName);
			return true;
		}

		SetError(mappingName, error);
		return false;
	}

	private void ValidateValue(string mappingName, string text)
	{
		if (Setting == null || HasOptions)
			return;

		if (Setting.TryParseValue(text, out _, out string error))
			ClearError(mappingName);
		else
			SetError(mappingName, error);
	}

	private void SetError(string propertyName, string error)
	{
		if (_errors.TryGetValue(propertyName, out string existing) && existing == error)
			return;

		_errors[propertyName] = error;
		ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
		OnPropertyChanged(nameof(HasErrors));
		OnPropertyChanged(propertyName == nameof(DisplayAc) ? nameof(HasAcError) : nameof(HasDcError));
	}

	private void ClearError(string propertyName)
	{
		if (!_errors.Remove(propertyName))
			return;

		ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
		OnPropertyChanged(nameof(HasErrors));
		OnPropertyChanged(propertyName == nameof(DisplayAc) ? nameof(HasAcError) : nameof(HasDcError));
	}

	private void ClearErrors()
	{
		foreach (string propertyName in _errors.Keys.ToList())
			ClearError(propertyName);
	}
}

public sealed partial class PowerEditTemplateSelector : DataTemplateSelector
{
	public DataTemplate ComboBoxTemplate { get; set; }

	public DataTemplate TextBoxTemplate { get; set; }

	protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
	{
		if (item is not PowerTreeNode { NodeKind: PowerNodeKind.Setting, HasValues: true } node)
			return null;
		return node.HasOptions ? ComboBoxTemplate : TextBoxTemplate;
	}
}

public sealed partial class PowerCellStyleSelector : StyleSelector
{
	public Style CriticalStyle { get; set; }

	public Style SuccessStyle { get; set; }

	public Style CautionStyle { get; set; }

	protected override Style SelectStyleCore(object item, DependencyObject container)
	{
		if (item is not PowerTreeNode node || container is not TreeGridCell cell)
			return null;

		string mappingName = cell.ColumnBase?.TreeGridColumn?.MappingName;
		bool isActiveAc = mappingName == nameof(PowerTreeNode.DisplayAc);
		bool isActiveDc = mappingName == nameof(PowerTreeNode.DisplayDc);
		bool isAc = mappingName is nameof(PowerTreeNode.DisplayAc) or nameof(PowerTreeNode.DisplayCompareAc) or nameof(PowerTreeNode.DisplayOriginalAc);
		bool isDc = mappingName is nameof(PowerTreeNode.DisplayDc) or nameof(PowerTreeNode.DisplayCompareDc) or nameof(PowerTreeNode.DisplayOriginalDc);
		if (!isAc && !isDc)
			return null;

		if (isActiveAc && node.HasAcError || isActiveDc && node.HasDcError)
			return CautionStyle;

		bool isDifferent = isAc ? node.IsAcDifferent : node.IsDcDifferent;
		if (!isDifferent)
			return null;

		if (mappingName is nameof(PowerTreeNode.DisplayCompareAc) or nameof(PowerTreeNode.DisplayCompareDc))
			return SuccessStyle;
		if (mappingName is nameof(PowerTreeNode.DisplayOriginalAc) or nameof(PowerTreeNode.DisplayOriginalDc))
			return CriticalStyle;

		return node.ProjectionKind switch
		{
			PowerProjectionKind.Comparison => CriticalStyle,
			PowerProjectionKind.ViewChanges => SuccessStyle,
			_ => null
		};
	}
}
