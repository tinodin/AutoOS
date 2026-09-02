using System.Globalization;
using AutoOS.App.Data.Contracts;
using AutoOS.App.Data.Enums;
using AutoOS.App.Data.Enums.Power;
using AutoOS.App.Data.Models.Power;
using AutoOS.App.Extensions;
using AutoOS.App.Services;
using AutoOS.App.ViewModels.Dialogs.Power;
using Microsoft.UI.Xaml;

namespace AutoOS.App.ViewModels;

public sealed partial class PowerPageViewModel(IPowerPlanService powerService, IDialogService dialogService, IFilePickerService filePickerService) : ObservableObject
{
	private readonly Stack<Dictionary<Setting, Value>> _undoStates = [];
	private readonly Stack<Dictionary<Setting, Value>> _redoStates = [];
	private readonly Dictionary<Setting, SettingState> _settingStates = [];
	private readonly List<Setting> _settings = [];
	private IReadOnlyList<Subgroup> _subgroups = [];
	private CancellationTokenSource? _activePlanReloadCts;
	private TreeState? _compareTree;
	private TreeState? _changesTree;

	public Action? RefreshFilterAction { get; set; }

	public Action? RefreshFilterOnlyAction { get; set; }

	public ObservableCollection<Plan> Plans { get; } = [];

	public ObservableCollection<Plan> ComparePlans { get; } = [];

	public ObservableCollection<Node> TreeNodes { get; } = [];

	public ObservableCollection<Node> CompareNodes { get; } = [];

	public ObservableCollection<Node> ChangesNodes { get; } = [];

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(SwitchPresenterValue))]
	[NotifyPropertyChangedFor(nameof(IsLoaded))]
	[NotifyCanExecuteChangedFor(nameof(UndoCommand))]
	[NotifyCanExecuteChangedFor(nameof(RedoCommand))]
	[NotifyCanExecuteChangedFor(nameof(SaveChangesCommand))]
	[NotifyCanExecuteChangedFor(nameof(RestoreCommand))]
	[NotifyCanExecuteChangedFor(nameof(ImportCommand))]
	public partial PageMode PageState { get; set; } = PageMode.Loading;

	public string SwitchPresenterValue => PageState switch
	{
		PageMode.Loading => "Loading",
		PageMode.Loaded => "Loaded",
		_ => throw new UnreachableException()
	};

	public bool IsLoaded => PageState == PageMode.Loaded;

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(ActivePlanToolTip))]
	[NotifyPropertyChangedFor(nameof(ActivePlanAcHeader))]
	[NotifyPropertyChangedFor(nameof(ActivePlanDcHeader))]
	public partial Plan ActivePlan { get; set; } = null!;

	public string ActivePlanAcHeader => $"{ActivePlan?.Name} (AC)";

	public string ActivePlanDcHeader => $"{ActivePlan?.Name} (DC)";

	public string ActivePlanToolTip => ActivePlan is { Description.Length: > 0 } plan ? plan.Description : ActivePlan?.Name ?? string.Empty;

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(ComparePlanToolTip))]
	[NotifyPropertyChangedFor(nameof(ComparePlanAcHeader))]
	[NotifyPropertyChangedFor(nameof(ComparePlanDcHeader))]
	[NotifyPropertyChangedFor(nameof(NormalVisibility))]
	[NotifyPropertyChangedFor(nameof(ComparisonVisibility))]
	[NotifyPropertyChangedFor(nameof(ViewChangesVisibility))]
	public partial Plan ComparePlan { get; set; } = null!;

	public string ComparePlanAcHeader => $"{ComparePlan?.Name} (AC)";

	public string ComparePlanDcHeader => $"{ComparePlan?.Name} (DC)";

	public string ComparePlanToolTip => ComparePlan is { Description.Length: > 0 } plan ? plan.Description : ComparePlan?.Name ?? string.Empty;

	private bool HasComparePlan => ComparePlan != null && ComparePlan.Guid != Guid.Empty;

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(NormalVisibility))]
	[NotifyPropertyChangedFor(nameof(ComparisonVisibility))]
	[NotifyPropertyChangedFor(nameof(ViewChangesVisibility))]
	public partial bool ViewChangesActive { get; set; }

	public Visibility NormalVisibility => !ViewChangesActive && !HasComparePlan ? Visibility.Visible : Visibility.Collapsed;

	public Visibility ComparisonVisibility => HasComparePlan && !ViewChangesActive ? Visibility.Visible : Visibility.Collapsed;

	public Visibility ViewChangesVisibility => ViewChangesActive ? Visibility.Visible : Visibility.Collapsed;

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(ViewChangesLabel))]
	[NotifyCanExecuteChangedFor(nameof(SaveChangesCommand))]
	public partial int ModifiedCount { get; set; }

	public string ViewChangesLabel => $"View Changes ({ModifiedCount})";

	[ObservableProperty]
	public partial string SearchText { get; set; } = string.Empty;

	[ObservableProperty]
	public partial bool FilterSubgroup { get; set; } = true;

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

	public bool CanUndo => IsLoaded && _undoStates.Count > 0;

	public bool CanRedo => IsLoaded && _redoStates.Count > 0;

	public bool CanSave => IsLoaded && ModifiedCount > 0;

	public bool CanDelete => Plans.Count > 1;

	public bool CanRestore => IsLoaded;

	[RelayCommand(CanExecute = nameof(CanUndo))]
	private void Undo()
	{
		if (!CanUndo)
			return;

		MoveState(_undoStates, _redoStates);
	}

	[RelayCommand(CanExecute = nameof(CanRedo))]
	private void Redo()
	{
		if (!CanRedo)
			return;

		MoveState(_redoStates, _undoStates);
	}

	[RelayCommand]
	private async Task EditAsync()
	{
		if (ActivePlan is not Plan plan)
			return;

		var editDialogViewModel = new EditDialogViewModel(plan.Name, plan.Description);
		if (await dialogService.ShowDialogAsync(editDialogViewModel) != DialogResult.Primary)
			return;

		Plan updated = powerService.UpdatePowerPlanMetadata(plan, editDialogViewModel.Name, editDialogViewModel.Description);
		int index = Plans.IndexOf(plan);
		if (index < 0)
			return;

		Plans[index] = updated;
		ActivePlan = updated;
	}

	[RelayCommand]
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

		Guid guid = powerService.DuplicatePowerPlan(plan.Guid, name, plan.Description);
		SetPlans(await Task.Run(powerService.GetPowerPlans), guid);
	}

	[RelayCommand]
	private async Task ExportAsync()
	{
		if (ActivePlan is not Plan plan)
			return;

		string? filePath = await filePickerService.PickSaveFileAsync("Power Scheme Files", ["*.pow"], plan.Name);
		if (filePath == null)
			return;

		await Task.Run(() => powerService.ExportPowerPlan(plan.Guid, filePath));
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
		powerService.SetActivePowerPlan(nextPlan.Guid);
		powerService.DeletePowerPlan(planToDelete.Guid);
		SetPlans(await Task.Run(powerService.GetPowerPlans), nextPlan.Guid);
	}

	[RelayCommand(CanExecute = nameof(CanRestore))]
	private async Task ImportAsync()
	{
		string? filePath = await filePickerService.PickSingleFileAsync("Power Scheme Files", ["*.pow"], Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads"));
		if (filePath == null)
			return;

		Guid importedGuid = await Task.Run(() => powerService.ImportPowerPlan(filePath));
		if (importedGuid == Guid.Empty)
			return;

		SetPlans(await Task.Run(powerService.GetPowerPlans), importedGuid);
	}

	[RelayCommand(CanExecute = nameof(CanRestore))]
	private async Task RestoreAsync()
	{
		if (await dialogService.ShowConfirmationDialogAsync("Restore default power plans", "Are you sure that you want to restore the default power plans and re-apply the AutoOS power plan?", "Yes", "No") != DialogResult.Primary)
			return;

		await powerService.RestoreDefaultPowerPlansAsync();

		Guid activeGuid = await Task.Run(powerService.GetActivePowerPlan);
		SetPlans(await Task.Run(powerService.GetPowerPlans), activeGuid);
		await ReloadActiveValuesAsync();
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

		powerService.SaveChanges(ActivePlan.Guid, changes);
		ResetHistory();
		RefreshState();
		SyncCompareAndChanges();
		UpdateNodeCounts();
		RefreshFilterOnlyAction?.Invoke();
	}

	[RelayCommand]
	private void SetFilterMode(string value) => FilterMode = Enum.Parse<FilterMode>(value);

	public async Task LoadPlansAsync()
	{
		PageState = PageMode.Loading;

		IReadOnlyList<Plan> plans = await Task.Run(powerService.GetPowerPlans);
		Guid activeGuid = await Task.Run(powerService.GetActivePowerPlan);
		SetPlans(plans, activeGuid);

		(Plan plan, IReadOnlyList<Subgroup> subgroups, IReadOnlyDictionary<Setting, Value> initialValues) = await Task.Run(() => powerService.ReadPowerPlan(ActivePlan.Guid));

		ActivePlan = plan;
		_subgroups = subgroups;
		_settings.Clear();
		_settings.AddRange(subgroups.SelectMany(subgroup => subgroup.Settings));
		_settingStates.Clear();
		foreach ((Setting setting, Value value) in initialValues)
		{
			_settingStates[setting] = new SettingState(setting)
			{
				AcValue = value.AcValue,
				DcValue = value.DcValue,
				OriginalAcValue = value.AcValue,
				OriginalDcValue = value.DcValue
			};
		}

		RefreshComparisonValues();
		TreeNodes.Clear();
		CompareNodes.Clear();
		ChangesNodes.Clear();
		TreeNodes.Add(BuildTree("All Settings", static node => true).Root);
		_compareTree = BuildTree("Differences", static node => node.IsAcDifferent || node.IsDcDifferent);
		_changesTree = BuildTree("Changes", static node => node.IsAcModified || node.IsDcModified);
		CompareNodes.Add(_compareTree.Root);
		ChangesNodes.Add(_changesTree.Root);
		ViewChangesActive = false;
		ResetHistory();
		RefreshState();
		RefreshFilter();

		PageState = PageMode.Loaded;
	}

	private void SetPlans(IEnumerable<Plan> plans, Guid activeGuid)
	{
		Plans.Clear();

		foreach (Plan plan in plans.OrderBy(plan => plan.Name, StringComparer.CurrentCultureIgnoreCase))
			Plans.Add(plan);

		ActivePlan = activeGuid == Guid.Empty ? null! : (Plans.FirstOrDefault(plan => plan.Guid == activeGuid) ?? Plans.FirstOrDefault())!;
		RefreshComparePlans();
		if (ActivePlan == null)
		{
			_subgroups = [];
			_settings.Clear();
			TreeNodes.Clear();
			CompareNodes.Clear();
			ChangesNodes.Clear();
			_compareTree = null;
			_changesTree = null;
			RefreshState();
			ResetHistory();
		}
		OnPropertyChanged(nameof(CanDelete));
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

	partial void OnActivePlanChanged(Plan value)
	{
		if (value == null || !IsLoaded)
			return;

		_activePlanReloadCts?.Cancel();
		_activePlanReloadCts?.Dispose();
		_activePlanReloadCts = new CancellationTokenSource();
		_ = SwitchActivePowerPlanAsync(value.Guid, _activePlanReloadCts.Token);
	}

	partial void OnComparePlanChanged(Plan value)
	{
		if (value == null)
			return;
		if (HasComparePlan)
			ViewChangesActive = false;
		RefreshComparisonValues();
		SyncCompare();
		UpdateNodeCounts();
		RefreshFilterOnlyAction?.Invoke();
	}

	private async Task SwitchActivePowerPlanAsync(Guid planGuid, CancellationToken token)
	{
		try
		{
			powerService.SetActivePowerPlan(planGuid);
			RefreshComparePlans();
			await ReloadActiveValuesAsync(token);
		}
		catch (OperationCanceledException)
		{ }
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

		RefreshState();
		ResetHistory();
		ViewChangesActive = false;
		SyncCompareAndChanges();
		UpdateNodeCounts();
		RefreshFilterOnlyAction?.Invoke();
	}

	private void RefreshComparisonValues()
	{
		foreach (Setting setting in _settings)
		{
			if (!_settingStates.TryGetValue(setting, out SettingState? state) || state is null)
				continue;

			if (!HasComparePlan)
			{
				state.CompareAcValue = null;
				state.CompareDcValue = null;
				continue;
			}

			Value? value = powerService.ReadValue(ComparePlan.Guid, setting.SubgroupGuid, setting.Guid);
			state.CompareAcValue = value?.AcValue;
			state.CompareDcValue = value?.DcValue;
		}
	}

	public void BeginEdit(Node? node, string mappingName)
	{
		if (node is not { Setting: { } setting, IsAdjustable: true })
			return;
		if (!_settingStates.TryGetValue(setting, out SettingState? values) || values is null)
			return;

		if (mappingName == nameof(Node.DisplayAc))
		{
			if (setting.Options.Count > 0)
				values.EditAcOption = setting.Options.FirstOrDefault(option => option.Index == values.AcValue);
			else
				values.EditAcValue = values.AcValue.ToString(CultureInfo.InvariantCulture);
		}
		else if (mappingName == nameof(Node.DisplayDc))
		{
			if (setting.Options.Count > 0)
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
		RefreshState();
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

	public void RefreshAfterEdit()
	{
		SyncCompareAndChanges();
		UpdateNodeCounts();
		RefreshFilterOnlyAction?.Invoke();
	}

	partial void OnSearchTextChanged(string value) => RefreshFilter();

	partial void OnFilterSubgroupChanged(bool value) => RefreshFilter();

	partial void OnFilterSettingChanged(bool value) => RefreshFilter();

	partial void OnFilterDescriptionChanged(bool value) => RefreshFilter();

	partial void OnFilterAcChanged(bool value) => RefreshFilter();

	partial void OnFilterDcChanged(bool value) => RefreshFilter();

	partial void OnFilterGuidChanged(bool value) => RefreshFilter();

	partial void OnFilterModeChanged(FilterMode value) => RefreshFilter();

	public void RefreshFilter()
	{
		RefreshFilterAction?.Invoke();
	}

	public bool MatchesFilter(object item)
	{
		if (item is not Node node)
			return true;

		if (node.NodeKind == NodeKind.Subgroup)
		{
			if (SearchText.Length > 0 && FilterSubgroup && TextMatches(node.DisplayName, SearchText.Trim()))
				return true;
			return node.Children.Any(MatchesFilter);
		}

		if (node.NodeKind != NodeKind.Setting)
			return node.Children.Any(MatchesFilter);

		string query = SearchText;
		return query.Length == 0 || NodeMatches(node, query);
	}

	private bool NodeMatches(Node node, string query)
	{
		Setting? setting = node.Setting;
		if (setting == null)
			return false;

		string trimmed = query.Trim();
		if (FilterSubgroup)
		{
			Subgroup? sg = _subgroups.FirstOrDefault(s => s.Guid == setting.SubgroupGuid);
			if (sg != null && TextMatches(sg.Name, trimmed))
				return true;
		}
		if (FilterSetting && TextMatches(setting.Name, query))
			return true;
		if (FilterDescription && TextMatches(setting.Description, query))
			return true;
		if (FilterGuid && (TextMatches(setting.Guid.ToString(), query) || TextMatches(setting.SubgroupGuid.ToString(), query)))
			return true;
		if ((FilterAc || FilterDc) && ValuesMatch(node, query))
			return true;

		return false;
	}

	private bool ValuesMatch(Node node, string query)
	{
		Setting setting = node.Setting!;
		SettingState values = _settingStates[setting];
		uint[] acCandidates;
		uint[] dcCandidates;

		if (HasComparePlan && !ViewChangesActive)
		{
			acCandidates = [values.AcValue, values.CompareAcValue!.Value];
			dcCandidates = [values.DcValue, values.CompareDcValue!.Value];
		}
		else if (ViewChangesActive)
		{
			acCandidates = [values.OriginalAcValue, values.AcValue];
			dcCandidates = [values.OriginalDcValue, values.DcValue];
		}
		else
		{
			acCandidates = [values.AcValue];
			dcCandidates = [values.DcValue];
		}

		return (FilterAc && CandidatesMatch(acCandidates, setting, query)) || (FilterDc && CandidatesMatch(dcCandidates, setting, query));
	}

	private bool CandidatesMatch(uint[] candidates, Setting setting, string query) =>
		candidates.Any(value => TextMatches(SettingState.GetDisplayValue(setting, value), query) || TextMatches(value.ToString(CultureInfo.InvariantCulture), query));

	private bool TextMatches(string text, string query)
	{
		if (string.IsNullOrWhiteSpace(text))
			return false;

		return FilterMode == FilterMode.ExactMatch ? text.Equals(query, StringComparison.OrdinalIgnoreCase) : text.Contains(query, StringComparison.OrdinalIgnoreCase);
	}

	private TreeState BuildTree(string baseName, Func<Node, bool> include)
	{
		var root = new Node(NodeKind.Root, baseName, string.Empty, Guid.Empty, baseDisplayName: baseName);
		var subgroups = new Dictionary<Guid, Node>();
		var settingNodes = new Dictionary<Setting, Node>();
		int order = 0;
		foreach (Subgroup subgroup in _subgroups)
		{
			var subgroupNode = new Node(NodeKind.Subgroup, subgroup.Name, subgroup.Description, subgroup.Guid, baseDisplayName: subgroup.Name)
			{
				Order = order
			};
			order++;
			foreach (Setting setting in subgroup.Settings)
			{
				if (!_settingStates.TryGetValue(setting, out SettingState? values) || values is null)
					continue;

				var node = new Node(NodeKind.Setting, setting.Name, setting.Description, setting.Guid, setting, values)
				{
					Order = order
				};
				order++;
				settingNodes[setting] = node;
				if (include(node))
					subgroupNode.Children.Add(node);
			}

			if (subgroupNode.Children.Count > 0)
				root.Children.Add(subgroupNode);

			subgroups[subgroup.Guid] = subgroupNode;
		}

		return new TreeState(root, subgroups, settingNodes);
	}

	private void SyncCompareAndChanges()
	{
		SyncTree(CompareNodes, _compareTree, static node => node.IsAcDifferent || node.IsDcDifferent);
		SyncTree(ChangesNodes, _changesTree, static node => node.IsAcModified || node.IsDcModified);
	}

	private void SyncCompare()
	{
		SyncTree(CompareNodes, _compareTree, static node => node.IsAcDifferent || node.IsDcDifferent);
	}

	private void SyncTree(ObservableCollection<Node> collection, TreeState? tree, Func<Node, bool> include)
	{
		if (collection.Count == 0 || tree == null)
			return;

		Node root = tree.Root;
		foreach (Subgroup subgroup in _subgroups)
		{
			Node subgroupNode = tree.Subgroups[subgroup.Guid];
			foreach (Setting setting in subgroup.Settings)
			{
				if (!tree.SettingNodes.TryGetValue(setting, out Node? node))
					continue;

				bool included = include(node);
				bool present = subgroupNode.Children.Contains(node);
				if (included && !present)
					subgroupNode.Children.InsertOrdered(node);
				else if (!included && present)
					subgroupNode.Children.Remove(node);
			}

			if (subgroupNode.Children.Count > 0 && !root.Children.Contains(subgroupNode))
				root.Children.InsertOrdered(subgroupNode);
			else if (subgroupNode.Children.Count == 0)
				root.Children.Remove(subgroupNode);
		}
	}

	public void UpdateNodeCounts()
	{
		if (TreeNodes.Count > 0)
			Recount(TreeNodes[0], static _ => 1);
		if (CompareNodes.Count > 0)
			Recount(CompareNodes[0], static node => (node.IsAcDifferent ? 1 : 0) + (node.IsDcDifferent ? 1 : 0));
		if (ChangesNodes.Count > 0)
			Recount(ChangesNodes[0], static node => (node.IsAcModified ? 1 : 0) + (node.IsDcModified ? 1 : 0));
	}

	private int Recount(Node node, Func<Node, int> leafWeight)
	{
		int count = 0;
		foreach (Node child in node.Children)
		{
			count += child.NodeKind == NodeKind.Setting ? (SearchText.Length > 0 && !NodeMatches(child, SearchText) ? 0 : leafWeight(child)) : Recount(child, leafWeight);
		}

		node.DisplayName = node.NodeKind == NodeKind.Root && SearchText.Length > 0 ? $"Results ({count})" : $"{node.BaseDisplayName} ({count})";
		return count;
	}

	private void RefreshState()
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
	}

	private void MoveState(Stack<Dictionary<Setting, Value>> from, Stack<Dictionary<Setting, Value>> to)
	{
		to.Push(CaptureState());
		RestoreState(from.Pop());
		RefreshState();
		SyncCompareAndChanges();
		UpdateNodeCounts();
		RefreshFilterOnlyAction?.Invoke();
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
