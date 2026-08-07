using System.Collections.ObjectModel;
using System.Globalization;
using AutoOS.App.Data.Enums;
using AutoOS.App.Data.Enums.Power;
using AutoOS.App.Data.Models.Power;
using AutoOS.App.Services;
using AutoOS.App.Services.Power;
using AutoOS.App.ViewModels.Dialogs.Power;
using AutoOS.App.Views.Installer.Stages;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AutoOS.App.ViewModels;

public sealed partial class PowerPageViewModel(IPowerPlanService powerService, IDialogService dialogService, IFilePickerService filePickerService) : ObservableObject
{
	private readonly Stack<Dictionary<Setting, Value>> _undoStates = [];
	private readonly Stack<Dictionary<Setting, Value>> _redoStates = [];
	private readonly Dictionary<Setting, Value> _compareValues = [];
	private readonly Dictionary<Setting, SettingState> _settingStates = [];
	private readonly List<Setting> _settings = [];
	private IReadOnlyList<Subgroup> _subgroups = [];
	private CancellationTokenSource? _activePlanReloadCts;

	public Action? RefreshFilterAction { get; set; }

	public Action? RefreshFilterOnlyAction { get; set; }

	public ObservableCollection<Plan> Plans { get; } = [];

	public ObservableCollection<Plan> ComparePlans { get; } = [];

	public ObservableCollection<Node> TreeNodes { get; } = [];

	public ObservableCollection<Node> CompareNodes { get; } = [];

	public ObservableCollection<Node> ChangeNodes { get; } = [];

	private bool HasComparePlan => ComparePlan != null && ComparePlan.Guid != Guid.Empty;

	[ObservableProperty]
	public partial string SwitchValue { get; set; } = "Loading";

	[ObservableProperty]
	[NotifyCanExecuteChangedFor(nameof(UndoCommand))]
	[NotifyCanExecuteChangedFor(nameof(RedoCommand))]
	[NotifyCanExecuteChangedFor(nameof(SaveChangesCommand))]
	[NotifyCanExecuteChangedFor(nameof(ToggleViewChangesCommand))]
	[NotifyCanExecuteChangedFor(nameof(RestoreCommand))]
	[NotifyCanExecuteChangedFor(nameof(ImportCommand))]
	public partial bool IsLoaded { get; set; }

	[ObservableProperty]
	[NotifyCanExecuteChangedFor(nameof(EditCommand))]
	[NotifyCanExecuteChangedFor(nameof(DuplicateCommand))]
	[NotifyCanExecuteChangedFor(nameof(ExportCommand))]
	[NotifyPropertyChangedFor(nameof(ActivePlanToolTip))]
	[NotifyPropertyChangedFor(nameof(ActivePlanAcHeader))]
	[NotifyPropertyChangedFor(nameof(ActivePlanDcHeader))]
	public partial Plan ActivePlan { get; set; } = null!;

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(ComparePlanToolTip))]
	[NotifyPropertyChangedFor(nameof(ComparePlanAcHeader))]
	[NotifyPropertyChangedFor(nameof(ComparePlanDcHeader))]
	[NotifyPropertyChangedFor(nameof(Mode))]
	[NotifyPropertyChangedFor(nameof(NormalVisibility))]
	[NotifyPropertyChangedFor(nameof(ComparisonVisibility))]
	[NotifyPropertyChangedFor(nameof(ViewChangesVisibility))]
	public partial Plan ComparePlan { get; set; } = null!;

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
	public partial FilterMode FilterMode { get; set; } = FilterMode.Contains;

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(Mode))]
	[NotifyPropertyChangedFor(nameof(NormalVisibility))]
	[NotifyPropertyChangedFor(nameof(ComparisonVisibility))]
	[NotifyPropertyChangedFor(nameof(ViewChangesVisibility))]
	public partial bool ViewChanges { get; set; }

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(ViewChangesLabel))]
	[NotifyCanExecuteChangedFor(nameof(SaveChangesCommand))]
	public partial int ModifiedCount { get; set; }

	public PageMode Mode => ViewChanges ? PageMode.ViewChanges : HasComparePlan ? PageMode.Comparison : PageMode.Normal;

	public Visibility NormalVisibility => Mode == PageMode.Normal ? Visibility.Visible : Visibility.Collapsed;

	public Visibility ComparisonVisibility => Mode == PageMode.Comparison ? Visibility.Visible : Visibility.Collapsed;

	public Visibility ViewChangesVisibility => Mode == PageMode.ViewChanges ? Visibility.Visible : Visibility.Collapsed;

	public bool CanUndo => IsLoaded && _undoStates.Count > 0;

	public bool CanRedo => IsLoaded && _redoStates.Count > 0;

	public bool CanSave => IsLoaded && ModifiedCount > 0;

	public bool CanDelete => Plans.Count > 1;

	public bool CanRestore => IsLoaded;

	public bool CanEditPlan => ActivePlan != null;

	public bool CanToggleViewChanges => IsLoaded;

	public string ViewChangesLabel => $"View Changes ({ModifiedCount})";

	public string ActivePlanAcHeader => $"{ActivePlan?.Name ?? "Power Plan 1"} (AC)";

	public string ActivePlanDcHeader => $"{ActivePlan?.Name ?? "Power Plan 1"} (DC)";

	public string ComparePlanAcHeader => $"{(HasComparePlan ? ComparePlan.Name : "Power Plan 2")} (AC)";

	public string ComparePlanDcHeader => $"{(HasComparePlan ? ComparePlan.Name : "Power Plan 2")} (DC)";

	public string ActivePlanToolTip => ActivePlan is { Description.Length: > 0 } plan ? plan.Description : ActivePlan?.Name ?? string.Empty;

	public string ComparePlanToolTip => ComparePlan is { Description.Length: > 0 } plan ? plan.Description : ComparePlan?.Name ?? string.Empty;

	partial void OnSearchTextChanged(string value) => RefreshFilter();

	partial void OnFilterSettingChanged(bool value) => RefreshFilter();

	partial void OnFilterDescriptionChanged(bool value) => RefreshFilter();

	partial void OnFilterAcChanged(bool value) => RefreshFilter();

	partial void OnFilterDcChanged(bool value) => RefreshFilter();

	partial void OnFilterGuidChanged(bool value) => RefreshFilter();

	partial void OnFilterModeChanged(FilterMode value) => RefreshFilter();

	partial void OnActivePlanChanged(Plan value)
	{
		if (value == null || !IsLoaded)
			return;

		_activePlanReloadCts?.Cancel();
		_activePlanReloadCts?.Dispose();
		_activePlanReloadCts = new CancellationTokenSource();
		_ = ReloadActiveSchemeAsync(value.Guid, _activePlanReloadCts.Token);
	}

	private async Task ReloadActiveSchemeAsync(Guid planGuid, CancellationToken token)
	{
		try
		{
			powerService.SetActiveScheme(planGuid);
			RefreshComparePlans();
			await ReloadActiveValuesAsync(token);
		}
		catch (OperationCanceledException)
		{	}
	}

	partial void OnComparePlanChanged(Plan value)
	{
		if (value == null)
			return;
		if (HasComparePlan)
			ViewChanges = false;
		_compareValues.Clear();
		RefreshComparisonValues();
		RefreshTrees();
	}

	public async Task LoadPlansAsync(Guid? preferredGuid = null)
	{
		SwitchValue = "Loading";
		IsLoaded = false;

		IReadOnlyList<Plan> plans = await Task.Run(powerService.GetPlans);
		Guid activeGuid = await Task.Run(powerService.GetActivePlanGuid);
		bool hasTarget = preferredGuid.HasValue || activeGuid != Guid.Empty;
		SetPlans(plans, preferredGuid ?? activeGuid, hasTarget);

		(Plan plan, IReadOnlyList<Subgroup> subgroups, IReadOnlyDictionary<Setting, Value> initialValues) = await Task.Run(() => powerService.ReadCompleteScheme(ActivePlan.Guid));

		ActivePlan = plan;
		_subgroups = subgroups;
		_settings.Clear();
		_settings.AddRange(subgroups.SelectMany(subgroup => subgroup.Settings));
		_settingStates.Clear();
		foreach ((Setting setting, Value value) in initialValues)
		{
			_settingStates[setting] = new SettingState
			{
				AcValue = value.AcValue,
				DcValue = value.DcValue,
				OriginalAcValue = value.AcValue,
				OriginalDcValue = value.DcValue
			};
		}

		_compareValues.Clear();
		RefreshComparisonValues();
		ViewChanges = false;
		ResetHistory();
		RefreshState();
		IsLoaded = true;

		SwitchValue = "Loaded";
	}

	public void SetPlans(IEnumerable<Plan> plans, Guid activeGuid, bool selectFallback = true)
	{
		Plans.Clear();

		foreach (Plan plan in plans.OrderBy(plan => plan.Name, StringComparer.CurrentCultureIgnoreCase))
			Plans.Add(plan);

		ActivePlan = Plans.FirstOrDefault(plan => plan.Guid == activeGuid)!;
		if (ActivePlan == null && selectFallback)
			ActivePlan = Plans.FirstOrDefault()!;
		RefreshComparePlans();
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
		OnPropertyChanged(nameof(CanDelete));
	}

	private async Task ReloadActiveValuesAsync(CancellationToken token = default)
	{
		IReadOnlyDictionary<Setting, Value> values = await Task.Run(() => powerService.ReadValues(ActivePlan.Guid, _settings), token);
		token.ThrowIfCancellationRequested();
		foreach ((Setting setting, Value value) in values)
		{
			if (_settingStates.TryGetValue(setting, out SettingState? current) && current is not null)
			{
				current.AcValue = value.AcValue;
				current.DcValue = value.DcValue;
				current.OriginalAcValue = value.AcValue;
				current.OriginalDcValue = value.DcValue;
			}
		}

		ModifiedCount = 0;
		ResetHistory();
		ViewChanges = false;
		RefreshTrees();
	}

	private void RefreshComparisonValues()
	{
		if (!HasComparePlan)
			return;

		foreach (Setting setting in _settings)
		{
			Value? value = powerService.ReadValues(ComparePlan.Guid, setting.SubgroupGuid, setting.Guid);
			if (value.HasValue)
				_compareValues[setting] = value.Value;
		}
	}

	public void RefreshComparePlans()
	{
		var emptyPlan = new Plan(Guid.Empty, "None", "Select another Power Plan to compare.");
		Plan? previouslySelected = HasComparePlan ? ComparePlan : null;
		ComparePlans.Clear();
		ComparePlans.Add(emptyPlan);
		foreach (Plan plan in Plans.Where(plan => plan.Guid != ActivePlan?.Guid).OrderBy(plan => plan.Name, StringComparer.CurrentCultureIgnoreCase))
			ComparePlans.Add(plan);

		ComparePlan = previouslySelected != null && ComparePlans.Any(plan => plan.Guid == previouslySelected.Guid) ? previouslySelected : emptyPlan;
	}

	[RelayCommand(CanExecute = nameof(CanUndo))]
	public void Undo()
	{
		if (!CanUndo)
			return;

		_redoStates.Push(CaptureState());
		RestoreState(_undoStates.Pop());
		RefreshState();
	}

	[RelayCommand(CanExecute = nameof(CanRedo))]
	public void Redo()
	{
		if (!CanRedo)
			return;

		_undoStates.Push(CaptureState());
		RestoreState(_redoStates.Pop());
		RefreshState();
	}

	[RelayCommand(CanExecute = nameof(CanEditPlan))]
	private async Task EditAsync()
	{
		if (ActivePlan is not Plan plan)
			return;

		var editDialogViewModel = new EditDialogViewModel(plan.Name, plan.Description);
		if (await dialogService.ShowDialogAsync(editDialogViewModel) != DialogResult.Primary)
			return;

		Plan updated = powerService.UpdatePlanMetadata(plan, editDialogViewModel.Name, editDialogViewModel.Description);
		int index = Plans.IndexOf(plan);
		if (index < 0)
			return;
		if (ReferenceEquals(ActivePlan, plan))
			ActivePlan = updated;
		Plans[index] = updated;
	}

	[RelayCommand(CanExecute = nameof(CanEditPlan))]
	private async Task DuplicateAsync()
	{
		if (ActivePlan is not Plan plan)
			return;

		if (await dialogService.ShowConfirmationDialogAsync("Duplicate Power Plan", @$"Are you sure you want to duplicate ""{plan.Name}""?", "Yes", "No") != DialogResult.Primary)
			return;

		int number = 1;
		string name;
		do
		{
			name = number == 1 ? $"{plan.Name} - Copy" : $"{plan.Name} - Copy ({number})";
			number++;
		}
		while (Plans.Any(item => item.Name.Equals(name, StringComparison.CurrentCultureIgnoreCase)));

		Guid guid = powerService.DuplicateScheme(plan.Guid, name, plan.Description);
		SetPlans(await Task.Run(powerService.GetPlans), guid);
	}

	[RelayCommand(CanExecute = nameof(CanEditPlan))]
	private async Task ExportAsync()
	{
		if (ActivePlan is not Plan plan)
			return;

		string? filePath = await filePickerService.PickSaveFileAsync("Power Scheme Files", ["*.pow"], plan.Name);
		if (filePath == null)
			return;

		await Task.Run(() => powerService.ExportScheme(plan.Guid, filePath));
	}

	[RelayCommand(CanExecute = nameof(CanDelete))]
	private async Task DeleteAsync()
	{
		if (ActivePlan is not Plan planToDelete)
			return;

		if (await dialogService.ShowConfirmationDialogAsync("Delete power plan", @$"Are you sure that you want to delete ""{planToDelete.Name}""?", "Yes", "No") != DialogResult.Primary)
			return;

		int index = Plans.IndexOf(planToDelete);
		Plan nextPlan = index > 0 ? Plans[index - 1] : Plans[index + 1];
		powerService.SetActiveScheme(nextPlan.Guid);
		powerService.DeleteScheme(planToDelete.Guid);
		SetPlans(await Task.Run(powerService.GetPlans), nextPlan.Guid);
	}

	[RelayCommand(CanExecute = nameof(CanRestore))]
	private async Task ImportAsync()
	{
		string? filePath = await filePickerService.PickSingleFileAsync("Power Scheme Files", ["*.pow"], Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads"));
		if (filePath == null)
			return;

		Guid importedGuid = await Task.Run(() => powerService.ImportScheme(filePath));
		if (importedGuid == Guid.Empty)
			return;

		SetPlans(await Task.Run(powerService.GetPlans), importedGuid);
	}

	[RelayCommand(CanExecute = nameof(CanRestore))]
	private async Task RestoreAsync()
	{
		if (await dialogService.ShowConfirmationDialogAsync("Restore default power plans", "Are you sure that you want to restore the default power plans and re-apply the AutoOS power plan?", "Yes", "No") != DialogResult.Primary)
			return;

		foreach ((_, Func<Task>? action, Func<bool>? condition) in PowerStage.GetActions())
		{
			if (condition == null || condition())
				await action();
		}

		Guid activeGuid = await Task.Run(powerService.GetActivePlanGuid);
		SetPlans(await Task.Run(powerService.GetPlans), activeGuid, activeGuid != Guid.Empty);
	}

	[RelayCommand(CanExecute = nameof(CanToggleViewChanges))]
	private void ToggleViewChanges()
	{
		if (ViewChanges)
		{
			ComparePlan = ComparePlans.First(item => item.Guid == Guid.Empty)!;
			_compareValues.Clear();
		}
		RefreshTrees();
	}

	[RelayCommand(CanExecute = nameof(CanSave))]
	private void SaveChanges()
	{
		if (!CanSave || ActivePlan == null)
			return;

		var changes = new List<(Setting Setting, Value Value)>();
		foreach (Setting setting in _settings.Where(setting => _settingStates[setting].IsModified))
		{
			SettingState values = _settingStates[setting];
			changes.Add((setting, new Value(values.AcValue, values.DcValue)));
			values.OriginalAcValue = values.AcValue;
			values.OriginalDcValue = values.DcValue;
		}

		powerService.CommitChanges(ActivePlan.Guid, changes);
		ResetHistory();
		RefreshState();
	}

	public void BeginEdit(Node? node, string mappingName)
	{
		if (node is not { Setting: { } setting, IsAdjustable: true })
			return;
		if (!_settingStates.TryGetValue(setting, out SettingState? values) || values is null)
			return;

		if (mappingName == nameof(Node.DisplayAc))
		{
			if (node.HasOptions)
				values.EditAcOption = setting.Options.FirstOrDefault(option => option.Index == values.AcValue);
			else
				values.EditAcValue = values.AcValue.ToString(CultureInfo.InvariantCulture);
		}
		else if (mappingName == nameof(Node.DisplayDc))
		{
			if (node.HasOptions)
				values.EditDcOption = setting.Options.FirstOrDefault(option => option.Index == values.DcValue);
			else
				values.EditDcValue = values.DcValue.ToString(CultureInfo.InvariantCulture);
		}
	}

	public bool CommitEdit(Node? node, string mappingName)
	{
		if (node?.Setting is not Setting setting)
			return false;

		if (!_settingStates.TryGetValue(setting, out SettingState? values) || values is null)
			return false;

		Dictionary<Setting, Value> previous = CaptureState();
		if (!ApplyEditedValue(setting, values, mappingName, out bool changed))
			return false;

		if (!changed)
			return false;

		_undoStates.Push(previous);
		_redoStates.Clear();
		RefreshState(false);
		return true;
	}

	private static bool ApplyEditedValue(Setting setting, SettingState values, string mappingName, out bool changed)
	{
		changed = false;
		if (mappingName == nameof(Node.DisplayAc))
		{
			if (!TryGetEditedValue(setting, values.EditAcValue, values.EditAcOption, out uint value))
				return false;
			changed = value != values.AcValue;
			if (changed)
				values.AcValue = value;
		}
		else if (mappingName == nameof(Node.DisplayDc))
		{
			if (!TryGetEditedValue(setting, values.EditDcValue, values.EditDcOption, out uint value))
				return false;
			changed = value != values.DcValue;
			if (changed)
				values.DcValue = value;
		}

		return true;
	}

	private static bool TryGetEditedValue(Setting setting, string text, Option? option, out uint value)
	{
		if (setting.Options.Count > 0)
		{
			value = option?.Index ?? 0;
			return option != null;
		}

		value = 0;
		if (!setting.Minimum.HasValue || !setting.Maximum.HasValue || !setting.Increment.HasValue)
			return false;

		if (!ulong.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out ulong wide))
			return false;

		value = wide > uint.MaxValue ? uint.MaxValue : (uint)wide;
		if (value < setting.Minimum!.Value || value > setting.Maximum!.Value)
			return false;

		return true;
	}

	public bool MatchesFilter(object item)
	{
		if (item is not Node node)
			return true;

		string query = SearchText?.Trim() ?? string.Empty;
		if (query.Length == 0)
			return true;

		return NodeOrDescendantMatches(node, query);
	}

	private bool NodeOrDescendantMatches(Node node, string query)
	{
		if (NodeMatches(node, query))
			return true;

		return node.Children.Any(child => NodeOrDescendantMatches(child, query));
	}

	private bool NodeMatches(Node node, string query)
	{
		Setting? setting = node.Setting;
		if (setting == null)
			return false;

		if (FilterSetting && TextMatches(setting.Name, query))
			return true;
		if (FilterDescription && TextMatches(setting.Description, query))
			return true;
		if (FilterGuid && (TextMatches(setting.Guid.ToString(), query) || TextMatches(setting.SubgroupGuid.ToString(), query)))
			return true;
		if (FilterAc && ValuesMatch(node, query, true))
			return true;
		if (FilterDc && ValuesMatch(node, query, false))
			return true;

		return false;
	}

	private bool ValuesMatch(Node node, string query, bool isAc)
	{
		Setting setting = node.Setting!;
		SettingState values = _settingStates[setting];
		uint[] candidates = Mode switch
		{
			PageMode.Comparison when _compareValues.TryGetValue(setting, out Value comparison) => isAc ? [values.AcValue, comparison.AcValue] : [values.DcValue, comparison.DcValue],
			PageMode.ViewChanges => isAc ? [values.OriginalAcValue, values.AcValue] : [values.OriginalDcValue, values.DcValue],
			_ => isAc ? [values.AcValue] : [values.DcValue]
		};

		return candidates.Any(value => TextMatches(Node.GetDisplayValue(setting, value), query) || TextMatches(value.ToString(CultureInfo.InvariantCulture), query));
	}

	private bool TextMatches(string text, string query)
	{
		if (string.IsNullOrWhiteSpace(text))
			return false;

		return FilterMode == FilterMode.ExactMatch ? text.Equals(query, StringComparison.OrdinalIgnoreCase) : text.Contains(query, StringComparison.OrdinalIgnoreCase);
	}

	public void RefreshFilter()
	{
		RefreshFilterAction?.Invoke();
	}

	public void RefreshAfterEdit()
	{
		if (Mode == PageMode.Normal)
			(RefreshFilterOnlyAction ?? RefreshFilterAction)?.Invoke();
		else
			RefreshTrees();
	}

	private void RefreshTrees()
	{
		Node root = BuildTree(Mode);
		switch (Mode)
		{
			case PageMode.Normal:
				TreeNodes.Clear();
				TreeNodes.Add(root);
				break;
			case PageMode.Comparison:
				CompareNodes.Clear();
				CompareNodes.Add(root);
				break;
			case PageMode.ViewChanges:
				ChangeNodes.Clear();
				ChangeNodes.Add(root);
				break;
		}
		RefreshFilter();
	}

	private Node BuildTree(PageMode mode)
	{
		string baseRootName = mode switch
		{
			PageMode.Comparison => "Differences",
			PageMode.ViewChanges => "Changes",
			_ => "All Settings"
		};

		var rootChildren = new List<Node>();
		foreach (Subgroup subgroup in _subgroups)
		{
			var children = new List<Node>();
			foreach (Setting setting in subgroup.Settings)
			{
				if (CreateSettingNode(setting, mode) is { } settingNode)
					children.Add(settingNode);
			}

			if (children.Count == 0)
				continue;

			var subgroupNode = new Node(NodeKind.Subgroup, mode, $"{subgroup.Name} ({CountVisibleSettings(children)})", subgroup.Description, subgroup.Guid, baseDisplayName: subgroup.Name);
			foreach (Node child in children)
				subgroupNode.Children.Add(child);
			rootChildren.Add(subgroupNode);
		}

		int totalCount = CountVisibleSettings(rootChildren);
		string rootDisplayName = string.IsNullOrWhiteSpace(SearchText) ? $"{baseRootName} ({totalCount})" : $"Results ({totalCount})";
		var root = new Node(NodeKind.Root, mode, rootDisplayName, string.Empty, Guid.Empty, baseDisplayName: baseRootName);
		foreach (Node child in rootChildren)
			root.Children.Add(child);

		return root;
	}

	private Node? CreateSettingNode(Setting setting, PageMode mode)
	{
		if (!_settingStates.TryGetValue(setting, out SettingState? values) || values is null)
			return null;

		switch (mode)
		{
			case PageMode.Comparison:
				if (!_compareValues.TryGetValue(setting, out Value compare))
					return null;
				bool isAcDifferent = !string.Equals(Node.GetDisplayValue(setting, values.AcValue), Node.GetDisplayValue(setting, compare.AcValue), StringComparison.Ordinal);
				bool isDcDifferent = !string.Equals(Node.GetDisplayValue(setting, values.DcValue), Node.GetDisplayValue(setting, compare.DcValue), StringComparison.Ordinal);
				if (!isAcDifferent && !isDcDifferent)
					return null;
				return new Node(NodeKind.Setting, mode, setting.Name, setting.Description, setting.Guid, setting, values, compare, isAcDifferent, isDcDifferent);
			case PageMode.ViewChanges:
				if (!values.IsModified)
					return null;
				return new Node(NodeKind.Setting, mode, setting.Name, setting.Description, setting.Guid, setting, values, isAcDifferent: values.AcValue != values.OriginalAcValue, isDcDifferent: values.DcValue != values.OriginalDcValue);
			default:
				return new Node(NodeKind.Setting, mode, setting.Name, setting.Description, setting.Guid, setting, values);
		}
	}

	public void UpdateNodeCounts()
	{
		RecountCollection(TreeNodes);
		RecountCollection(CompareNodes);
		RecountCollection(ChangeNodes);
	}

	private void RecountCollection(ObservableCollection<Node> collection)
	{
		if (collection.Count == 0)
			return;

		Node oldRoot = collection[0];
		Node newRoot = RebuildCountedNode(oldRoot);
		newRoot.IsExpanded = oldRoot.IsExpanded;
		collection[0] = newRoot;
	}

	private Node RebuildCountedNode(Node node)
	{
		if (node.NodeKind == NodeKind.Setting)
			return node;

		var children = new List<Node>(node.Children.Count);
		foreach (Node child in node.Children)
			children.Add(RebuildCountedNode(child));

		int count = CountVisibleSettings(children);
		string displayName = node.NodeKind == NodeKind.Root && !string.IsNullOrWhiteSpace(SearchText) ? $"Results ({count})" : $"{node.BaseDisplayName} ({count})";

		var rebuilt = new Node(node.NodeKind, node.Mode, displayName, node.Description, node.Guid, baseDisplayName: node.BaseDisplayName)
		{
			IsExpanded = node.IsExpanded
		};
		foreach (Node child in children)
			rebuilt.Children.Add(child);
		return rebuilt;
	}

	private int CountVisibleSettings(IEnumerable<Node> nodes)
	{
		string query = SearchText?.Trim() ?? string.Empty;
		int count = 0;
		foreach (Node node in nodes)
		{
			if (node.NodeKind == NodeKind.Setting)
			{
				if (query.Length == 0 || NodeMatches(node, query))
				{
					if (node.Mode == PageMode.Normal)
						count++;
					else
					{
						if (node.IsAcDifferent)
							count++;
						if (node.IsDcDifferent)
							count++;
					}
				}
			}
			else
			{
				count += CountVisibleSettings(node.Children);
			}
		}

		return count;
	}

	private void RefreshState(bool rebuildTree = true)
	{
		int count = 0;
		foreach (Setting setting in _settings)
		{
			SettingState values = _settingStates[setting];
			if (values.AcValue != values.OriginalAcValue)
				count++;
			if (values.DcValue != values.OriginalDcValue)
				count++;
		}

		ModifiedCount = count;
		UndoCommand.NotifyCanExecuteChanged();
		RedoCommand.NotifyCanExecuteChanged();
		if (rebuildTree)
			RefreshTrees();
	}

	private Dictionary<Setting, Value> CaptureState() => _settings.ToDictionary(setting => setting, setting =>
	{
		SettingState values = _settingStates[setting];
		return new Value(values.AcValue, values.DcValue);
	});

	private void RestoreState(IEnumerable<KeyValuePair<Setting, Value>> state)
	{
		foreach ((Setting setting, Value value) in state)
		{
			SettingState values = _settingStates[setting];
			values.AcValue = value.AcValue;
			values.DcValue = value.DcValue;
		}
	}

	private void ResetHistory()
	{
		_undoStates.Clear();
		_redoStates.Clear();
		UndoCommand.NotifyCanExecuteChanged();
		RedoCommand.NotifyCanExecuteChanged();
	}
}
