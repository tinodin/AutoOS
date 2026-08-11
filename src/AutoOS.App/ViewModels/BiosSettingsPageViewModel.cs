using System.Collections.ObjectModel;
using AutoOS.App.Data.Enums;
using AutoOS.App.Data.Models.Bios;
using AutoOS.App.Services;
using AutoOS.App.Services.Bios;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NodeKind = AutoOS.App.Data.Enums.Bios.NodeKind;
using PageMode = AutoOS.App.Data.Enums.Bios.PageMode;

namespace AutoOS.App.ViewModels;

public sealed partial class BiosSettingsPageViewModel(IBiosSettingsService biosService, IFilePickerService filePickerService) : ObservableObject
{
	private readonly Stack<Dictionary<Setting, State>> _undoStates = [];
	private readonly Stack<Dictionary<Setting, State>> _redoStates = [];
	private readonly Dictionary<Setting, State> _settingStates = [];
	private readonly Dictionary<Node, List<GroupValueState>> _mixedEdits = [];
	private readonly List<Setting> _settings = [];
	private int _lastRecommendedCount;

	private sealed record GroupValueState(Node Leaf, Option? SelectedOption, string? Value);

	public Action? RefreshFilterAction { get; set; }

	public Action? RefreshFilterOnlyAction { get; set; }

	public ObservableCollection<Node> TreeNodes { get; } = [];

	public ObservableCollection<Node> DiffNodes { get; } = [];

	[ObservableProperty]
	public partial string SwitchPresenterValue { get; set; } = "Export";

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(CanUndo))]
	[NotifyPropertyChangedFor(nameof(CanRedo))]
	[NotifyPropertyChangedFor(nameof(CanMerge))]
	[NotifyPropertyChangedFor(nameof(CanApplyMerge))]
	[NotifyPropertyChangedFor(nameof(CanImport))]
	[NotifyCanExecuteChangedFor(nameof(UndoCommand))]
	[NotifyCanExecuteChangedFor(nameof(RedoCommand))]
	[NotifyCanExecuteChangedFor(nameof(ApplyRecommendationsCommand))]
	[NotifyCanExecuteChangedFor(nameof(ImportToNvramCommand))]
	[NotifyCanExecuteChangedFor(nameof(RestoreCommand))]
	[NotifyCanExecuteChangedFor(nameof(ToggleViewChangesCommand))]
	public partial bool IsLoaded { get; set; }

	[ObservableProperty]
	public partial string SearchText { get; set; } = string.Empty;

	[ObservableProperty]
	public partial bool FilterSetting { get; set; } = true;

	[ObservableProperty]
	public partial bool FilterDescription { get; set; }

	[ObservableProperty]
	public partial bool FilterCurrent { get; set; }

	[ObservableProperty]
	public partial FilterMode FilterMode { get; set; } = FilterMode.Contains;

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(NormalVisibility))]
	[NotifyPropertyChangedFor(nameof(ViewChangesVisibility))]
	public partial bool ViewChanges { get; set; }

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(ViewChangesLabel))]
	[NotifyPropertyChangedFor(nameof(CanImport))]
	[NotifyCanExecuteChangedFor(nameof(ImportToNvramCommand))]
	public partial int ModifiedCount { get; set; }

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(CanApplyMerge))]
	[NotifyCanExecuteChangedFor(nameof(ApplyRecommendationsCommand))]
	public partial int MergeCount { get; set; }

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(CanMerge))]
	[NotifyPropertyChangedFor(nameof(CanApplyMerge))]
	[NotifyCanExecuteChangedFor(nameof(ApplyRecommendationsCommand))]
	public partial bool HasRecommendations { get; set; }

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(CanApplyMerge))]
	public partial int RecommendedCount { get; set; }

	partial void OnMergeCountChanged(int value)
	{
		int clamped = Math.Clamp(value, 0, RecommendedCount);
		if (clamped != value)
			MergeCount = clamped;
	}

	partial void OnSearchTextChanged(string value) => RefreshFilter();

	partial void OnFilterSettingChanged(bool value) => RefreshFilter();

	partial void OnFilterDescriptionChanged(bool value) => RefreshFilter();

	partial void OnFilterCurrentChanged(bool value) => RefreshFilter();

	partial void OnFilterModeChanged(FilterMode value) => RefreshFilter();

	public Visibility NormalVisibility => ViewChanges ? Visibility.Collapsed : Visibility.Visible;

	public Visibility ViewChangesVisibility => ViewChanges ? Visibility.Visible : Visibility.Collapsed;

	public bool CanUndo => IsLoaded && _undoStates.Count > 0;

	public bool CanRedo => IsLoaded && _redoStates.Count > 0;

	public bool CanMerge => IsLoaded && HasRecommendations;

	public bool CanApplyMerge => CanMerge && MergeCount > 0;

	public bool CanImport => IsLoaded && ModifiedCount > 0;

	public bool CanRestore => IsLoaded;

	public bool CanToggleViewChanges => IsLoaded;

	public string ViewChangesLabel => $"View Changes ({ModifiedCount})";

	public async Task LoadAsync()
	{
		SwitchPresenterValue = ToPresenterValue(PageMode.Exporting);
		IsLoaded = false;
		ViewChanges = false;

		(PageMode state, IReadOnlyList<Setting> settings) = await biosService.LoadAsync();
		if (state != PageMode.Loaded)
		{
			SwitchPresenterValue = ToPresenterValue(state);
			return;
		}

		_settings.Clear();
		_settings.AddRange(settings);
		_settingStates.Clear();
		foreach (Setting setting in _settings)
		{
			_settingStates[setting] = new State(setting)
			{
				Value = setting.Value,
				SelectedOption = setting.SelectedOption,
				OriginalValue = setting.OriginalValue,
				OriginalSelectedOption = setting.OriginalSelectedOption
			};
		}

		SearchText = string.Empty;
		ResetHistory();
		BuildTrees();
		RefreshState();
		RefreshFilter();
		IsLoaded = true;

		SwitchPresenterValue = ToPresenterValue(PageMode.Loaded);
	}

	[RelayCommand(CanExecute = nameof(CanUndo))]
	public void Undo() => MoveState(_undoStates, _redoStates);

	[RelayCommand(CanExecute = nameof(CanRedo))]
	public void Redo() => MoveState(_redoStates, _undoStates);

	private void MoveState(Stack<Dictionary<Setting, State>> from, Stack<Dictionary<Setting, State>> to)
	{
		to.Push(CaptureState());
		RestoreState(from.Pop());
		RefreshState();
		RefreshAfterEdit();
	}

	[RelayCommand(CanExecute = nameof(CanApplyMerge))]
	private void ApplyRecommendations(int count)
	{
		Dictionary<Setting, State> previous = CaptureState();

		var targets = _settings
			.Where(setting => HasPendingRecommendation(setting, _settingStates[setting]))
			.Take(count)
			.ToList();

		foreach (Setting setting in targets)
		{
			State state = _settingStates[setting];
			if (setting.RecommendedOption != null)
				state.SelectedOption = setting.RecommendedOption;
			else if (!string.IsNullOrEmpty(setting.RecommendedValue))
				state.Value = setting.RecommendedValue;
		}

		if (!StatesEqual(previous, CaptureState()))
		{
			_undoStates.Push(previous);
			_redoStates.Clear();
		}

		RefreshState();
		RefreshAfterEdit();
	}

	[RelayCommand(CanExecute = nameof(CanImport))]
	private async Task ImportToNvramAsync()
	{
		SearchText = string.Empty;
		ViewChanges = false;
		SwitchPresenterValue = ToPresenterValue(PageMode.Importing);
		IsLoaded = false;

		PageMode result = await biosService.ImportToNvramAsync(_settingStates.Where(pair => pair.Value.IsModified));
		if (result == PageMode.Loaded)
		{
			await LoadAsync();
		}
		else
		{
			SwitchPresenterValue = ToPresenterValue(result);
			IsLoaded = true;
		}
	}

	[RelayCommand(CanExecute = nameof(CanRestore))]
	private async Task RestoreAsync()
	{
		string? filePath = await filePickerService.PickSingleFileAsync("NVRAM Backup", ["*.txt"], biosService.BackupDirectory);
		if (filePath == null)
			return;

		ViewChanges = false;
		SwitchPresenterValue = ToPresenterValue(PageMode.Importing);
		IsLoaded = false;

		PageMode result = await biosService.RestoreFromBackupAsync(filePath);
		if (result == PageMode.Loaded)
		{
			await LoadAsync();
		}
		else
		{
			SwitchPresenterValue = ToPresenterValue(result);
			IsLoaded = true;
		}
	}

	[RelayCommand(CanExecute = nameof(CanToggleViewChanges))]
	private void ToggleViewChanges() => RefreshFilter();

	public void BeginEdit(Node? node)
	{
		if (node is not { NodeKind: NodeKind.Setting or NodeKind.GroupedSetting })
			return;

		if (node.HasOptions)
		{
			if (node.NodeKind == NodeKind.GroupedSetting && node.DisplayCurrent == "Mixed")
				RememberMixedValues(node);
			node.EditOption = node.SelectedOption;
		}
		else
		{
			node.EditValue = node.DisplayCurrent == "Mixed" ? string.Empty : node.DisplayCurrent;
		}
	}

	public bool CommitEdit(Node? node)
	{
		if (node is not { NodeKind: NodeKind.Setting or NodeKind.GroupedSetting })
			return false;

		Dictionary<Setting, State> previous = CaptureState();
		if (!ApplyEditedValue(node))
			return false;

		_undoStates.Push(previous);
		_redoStates.Clear();
		RefreshState();
		RefreshAfterEdit();
		return true;
	}

	private bool ApplyEditedValue(Node node)
	{
		if (node.NodeKind == NodeKind.Setting)
		{
			State? state = node.State;
			Setting? setting = node.Setting;
			if (state == null || setting == null)
				return false;

			if (setting.HasOptions)
			{
				if (state.SelectedOption == node.EditOption)
					return false;
				state.SelectedOption = node.EditOption;
				return true;
			}

			if (string.Equals(state.Value, node.EditValue, StringComparison.Ordinal))
				return false;
			state.Value = node.EditValue;
			return true;
		}

		if (node.NodeKind == NodeKind.GroupedSetting)
		{
			if (!node.CanEditCurrent)
				return false;

			if (ReferenceEquals(node.EditOption, node.MixedOption))
			{
				if (!_mixedEdits.Remove(node, out List<GroupValueState>? saved))
					return false;

				bool changed = false;
				foreach (GroupValueState savedState in saved)
				{
					State? leafState = savedState.Leaf.State;
					if (leafState == null)
						continue;

					Option? previousOption = leafState.SelectedOption;
					string? previousValue = leafState.Value;
					if (savedState.Leaf.Setting?.HasOptions == true)
						leafState.SelectedOption = savedState.SelectedOption;
					else
						leafState.Value = savedState.Value;

					if (previousOption != leafState.SelectedOption || previousValue != leafState.Value)
						changed = true;
				}

				return changed;
			}

			bool groupChanged = false;
			foreach (Node leaf in node.GetLeaves())
			{
				if (leaf.State == null || leaf.Setting == null)
					continue;

				Option? previousOption = leaf.State.SelectedOption;
				string? previousValue = leaf.State.Value;
				if (leaf.Setting.HasOptions && node.EditOption != null)
					leaf.State.SelectedOption = node.EditOption;
				else if (!leaf.Setting.HasOptions)
					leaf.State.Value = node.EditValue;

				if (previousOption != leaf.State.SelectedOption || previousValue != leaf.State.Value)
					groupChanged = true;
			}

			return groupChanged;
		}

		return false;
	}

	private void RememberMixedValues(Node node) =>
		_mixedEdits[node] = [.. node.GetLeaves()
			.Where(leaf => leaf.State != null)
			.Select(leaf => new GroupValueState(leaf, leaf.State!.SelectedOption, leaf.State.Value))];

	public bool MatchesFilter(object item)
	{
		if (item is not Node node)
			return true;

		if (node.NodeKind == NodeKind.Root)
		{
			if (node.BaseDisplayName.StartsWith("Recommended") && (ViewChanges || !string.IsNullOrWhiteSpace(SearchText)))
				return false;
			return node.Children.Any(MatchesFilter);
		}

		if (node.NodeKind != NodeKind.Setting)
			return node.Children.Any(MatchesFilter);

		if (ViewChanges && !node.IsModified)
			return false;

		string query = SearchText;
		if (query.Length == 0)
			return true;

		return NodeMatches(node, query);
	}

	private bool NodeMatches(Node node, string query)
	{
		Setting? setting = node.Setting;
		if (setting == null)
			return false;

		if (FilterSetting && TextMatches(setting.SetupQuestion, query))
			return true;
		if (FilterDescription && TextMatches(setting.HelpString, query))
			return true;
		if (FilterCurrent && TextMatches(node.DisplayCurrent, query))
			return true;

		return false;
	}

	private bool TextMatches(string? text, string query)
	{
		if (string.IsNullOrWhiteSpace(text))
			return false;

		return FilterMode == FilterMode.ExactMatch ? text.Equals(query, StringComparison.OrdinalIgnoreCase) : text.Contains(query, StringComparison.OrdinalIgnoreCase);
	}

	public void RefreshFilter() => RefreshFilterAction?.Invoke();

	public void RefreshAfterEdit()
	{
		RefreshRecommendedRoot();
		RefreshFilter();
	}

	public void UpdateNodeCounts()
	{
		RecountCollection(TreeNodes);
		RecountCollection(DiffNodes);
	}

	private void RecountCollection(ObservableCollection<Node> collection)
	{
		foreach (Node node in collection)
			RecountCounted(node);
	}

	private void RecountCounted(Node node)
	{
		if (node.NodeKind == NodeKind.Setting)
			return;

		foreach (Node child in node.Children)
			RecountCounted(child);

		int count = CountVisibleSettings(node.Children);
		string displayName = node.NodeKind == NodeKind.Root && !string.IsNullOrWhiteSpace(SearchText) ? $"Results ({count})" : $"{node.BaseDisplayName} ({count})";
		node.DisplayName = displayName;
	}

	private int CountVisibleSettings(IEnumerable<Node> nodes)
	{
		string query = SearchText;
		int count = 0;
		foreach (Node node in nodes)
		{
			if (node.NodeKind == NodeKind.Setting)
			{
				if (ViewChanges && !node.IsModified)
					continue;
				if (query.Length == 0 || NodeMatches(node, query))
					count++;
			}
			else
			{
				count += CountVisibleSettings(node.Children);
			}
		}

		return count;
	}

	private void BuildTrees()
	{
		TreeNodes.Clear();
		DiffNodes.Clear();

		var groups = _settings.GroupBy(setting => setting.SetupQuestion?.Trim() ?? string.Empty, StringComparer.OrdinalIgnoreCase).ToList();

		var allGroupNodes = new List<Node>(groups.Count);
		foreach (IGrouping<string, Setting>? grp in groups)
		{
			var members = grp.ToList();

			if (members.Count == 1)
			{
				allGroupNodes.Add(CreateSettingNode(members[0]));
			}
			else
			{
				var groupNode = new Node(NodeKind.GroupedSetting, $"{grp.Key} ({members.Count})", members[0].HelpString, grp.Key);
				foreach (Setting? member in members)
				{
					Node settingNode = CreateSettingNode(member);
					settingNode.Parent = groupNode;
					groupNode.Children.Add(settingNode);
				}
				allGroupNodes.Add(groupNode);
			}
		}

		Node recommendedRoot = BuildRecommendedRoot(allGroupNodes);
		if (CountLeaves(recommendedRoot) > 0)
			TreeNodes.Add(recommendedRoot);

		var allRoot = new Node(NodeKind.Root, "All Settings", baseDisplayName: "All Settings");
		foreach (Node node in allGroupNodes)
			node.Parent = allRoot;
		foreach (Node node in allGroupNodes)
			allRoot.Children.Add(node);
		allRoot.DisplayName = $"All Settings ({CountLeaves(allRoot)})";
		TreeNodes.Add(allRoot);

		DiffNodes.Add(BuildDiffTree());
	}

	private Node BuildRecommendedRoot(List<Node> allGroupNodes)
	{
		var recommendedRoot = new Node(NodeKind.Root, "Recommended", baseDisplayName: "Recommended");

		foreach (Node node in allGroupNodes)
		{
			if (node.NodeKind == NodeKind.Setting)
			{
				if (node.HasPendingRecommendation)
				{
					Node clone = CloneNode(node);
					clone.Parent = recommendedRoot;
					recommendedRoot.Children.Add(clone);
				}
				continue;
			}

			var pendingChildren = node.Children.Where(child => child.HasPendingRecommendation).ToList();
			if (pendingChildren.Count == 1)
			{
				Node clone = CloneNode(pendingChildren[0]);
				clone.Parent = recommendedRoot;
				recommendedRoot.Children.Add(clone);
			}
			else if (pendingChildren.Count > 1)
			{
				var groupClone = new Node(NodeKind.GroupedSetting, $"{node.GroupKey} ({pendingChildren.Count})", node.Description, node.GroupKey);
				foreach (Node child in pendingChildren)
				{
					Node clone = CloneNode(child);
					clone.Parent = groupClone;
					groupClone.Children.Add(clone);
				}
				groupClone.Parent = recommendedRoot;
				recommendedRoot.Children.Add(groupClone);
			}
		}

		recommendedRoot.DisplayName = $"Recommended ({CountLeaves(recommendedRoot)})";
		return recommendedRoot;
	}

	private void RefreshRecommendedRoot()
	{
		if (TreeNodes.Count == 0)
			return;

		Node? oldRecommended = TreeNodes.FirstOrDefault(node => node.NodeKind == NodeKind.Root && node.BaseDisplayName == "Recommended");
		Node allRoot = TreeNodes.Last();

		Node newRecommended = BuildRecommendedRoot(allRoot.Children.ToList());
		if (CountLeaves(newRecommended) == 0)
		{
			if (oldRecommended != null)
				TreeNodes.Remove(oldRecommended);
		}
		else if (oldRecommended != null)
		{
			int index = TreeNodes.IndexOf(oldRecommended);
			TreeNodes[index] = newRecommended;
		}
		else
		{
			TreeNodes.Insert(0, newRecommended);
		}
	}

	private Node BuildDiffTree()
	{
		var changesRoot = new Node(NodeKind.Root, "Changes", baseDisplayName: "Changes");

		var groups = _settings.GroupBy(setting => setting.SetupQuestion?.Trim() ?? string.Empty, StringComparer.OrdinalIgnoreCase);

		foreach (IGrouping<string, Setting>? grp in groups)
		{
			var members = grp.ToList();

			if (members.Count == 1)
			{
				Node node = CreateSettingNode(members[0]);
				node.Parent = changesRoot;
				changesRoot.Children.Add(node);
			}
			else
			{
				var groupNode = new Node(NodeKind.GroupedSetting, $"{grp.Key} ({members.Count})", members[0].HelpString, grp.Key);
				foreach (Setting? member in members)
				{
					Node node = CreateSettingNode(member);
					node.Parent = groupNode;
					groupNode.Children.Add(node);
				}
				groupNode.Parent = changesRoot;
				changesRoot.Children.Add(groupNode);
			}
		}

		changesRoot.DisplayName = $"Changes ({CountLeaves(changesRoot)})";
		return changesRoot;
	}

	private Node CreateSettingNode(Setting setting)
	{
		State state = _settingStates[setting];
		return new Node(NodeKind.Setting, setting.SetupQuestion ?? string.Empty, setting.HelpString, setting: setting, state: state);
	}

	private static Node CloneNode(Node source)
	{
		if (source.NodeKind == NodeKind.Setting)
		{
			return new Node(NodeKind.Setting, source.BaseDisplayName, source.Description, setting: source.Setting, state: source.State)
			{
				DisplayName = source.DisplayName
			};
		}

		var clone = new Node(source.NodeKind, source.BaseDisplayName, source.Description, source.GroupKey)
		{
			DisplayName = source.DisplayName,
			IsExpanded = source.IsExpanded
		};
		foreach (Node child in source.Children)
			clone.Children.Add(CloneNode(child));
		return clone;
	}

	private static int CountLeaves(Node node)
	{
		if (node.NodeKind == NodeKind.Setting)
			return 1;

		return node.Children.Sum(CountLeaves);
	}

	private static bool HasPendingRecommendation(Setting setting, State state)
	{
		if (setting.RecommendedOption != null)
			return state.SelectedOption != setting.RecommendedOption;

		return !string.IsNullOrEmpty(setting.RecommendedValue) &&
			!string.Equals(state.Value, setting.RecommendedValue, StringComparison.Ordinal);
	}

	private void RefreshState()
	{
		ModifiedCount = _settings.Count(setting => _settingStates[setting].IsModified);
		RecommendedCount = _settings.Count(setting => HasPendingRecommendation(setting, _settingStates[setting]));
		HasRecommendations = RecommendedCount > 0;
		SyncMergeCount();
		UndoCommand.NotifyCanExecuteChanged();
		RedoCommand.NotifyCanExecuteChanged();
	}

	private void SyncMergeCount()
	{
		int newCount = RecommendedCount;
		if (MergeCount == _lastRecommendedCount)
			MergeCount = newCount;
		else if (MergeCount > newCount)
			MergeCount = newCount;
		_lastRecommendedCount = newCount;
	}

	private Dictionary<Setting, State> CaptureState() => _settings.ToDictionary(setting => setting, setting =>
	{
		State values = _settingStates[setting];
		return new State(setting)
		{
			Value = values.Value,
			SelectedOption = values.SelectedOption
		};
	});

	private void RestoreState(IEnumerable<KeyValuePair<Setting, State>> state)
	{
		foreach ((Setting setting, State captured) in state)
		{
			State values = _settingStates[setting];
			values.Value = captured.Value;
			values.SelectedOption = captured.SelectedOption;
		}
	}

	private void ResetHistory()
	{
		_undoStates.Clear();
		_redoStates.Clear();
		UndoCommand.NotifyCanExecuteChanged();
		RedoCommand.NotifyCanExecuteChanged();
	}

	private static bool StatesEqual(Dictionary<Setting, State> left, Dictionary<Setting, State> right) =>
		left.Count == right.Count && left.All(pair => pair.Value.SelectedOption == right[pair.Key].SelectedOption && pair.Value.Value == right[pair.Key].Value);

	private static string ToPresenterValue(PageMode state) => state switch
	{
		PageMode.Exporting => "Export",
		PageMode.Importing => "Import",
		PageMode.Loaded => "Loaded",
		PageMode.Unsupported => "Unsupported",
		PageMode.HiiResourcesRegular => "HII Resources (Regular)",
		PageMode.HiiResourcesProtected => "HII Resources (Protected)",
		PageMode.HiiResourcesOther => "HII Resources (Other)",
		PageMode.WriteProtectedAsus => "Write Protected (ASUS)",
		PageMode.WriteProtectedAsRock => "Write Protected (ASRock)",
		PageMode.WriteProtectedOther => "Write Protected (Other)",
		_ => "Export"
	};
}
