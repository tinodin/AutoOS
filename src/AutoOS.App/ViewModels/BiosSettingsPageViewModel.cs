using System.Collections.ObjectModel;
using AutoOS.App.Data.Contracts;
using AutoOS.App.Data.Enums;
using AutoOS.App.Data.Enums.Bios;
using AutoOS.App.Data.Models.Bios;
using AutoOS.App.Extensions;
using AutoOS.App.ViewModels.Dialogs.Bios;
using AutoOS.Core.Data.Models.Bios;
using AutoOS.Core.Helpers.Bios;
using AutoOS.Core.Helpers.Shutdown;
using AutoOS.App.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Diagnostics;
using System.Text.Json;

namespace AutoOS.App.ViewModels;

public sealed partial class BiosSettingsPageViewModel(IBiosSettingsService biosService, IBiosBackupService backupService, IFilePickerService filePickerService, IDialogService dialogService) : ObservableObject
{
	private readonly Stack<Dictionary<Setting, string?>> _undoStates = [];

	private readonly Stack<Dictionary<Setting, string?>> _redoStates = [];

	private readonly Dictionary<Setting, SettingState> _settingStates = [];

	private readonly List<Setting> _settings = [];

	private TreeState? _recommendedTree;

	private TreeState? _allTree;

	private TreeState? _compareTree;

	private TreeState? _changesTree;

	public Action? RefreshFilterAction { get; set; }

	public Action? RefreshFilterOnlyAction { get; set; }

	public ObservableCollection<Node> TreeNodes { get; } = [];

	public ObservableCollection<Node> CompareNodes { get; } = [];

	public ObservableCollection<Node> DiffNodes { get; } = [];

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(SwitchPresenterValue))]
	public partial PageMode PageState { get; set; } = PageMode.Reading;

	public string SwitchPresenterValue => PageState switch
	{
		PageMode.Reading => "Reading",
		PageMode.Writing => "Writing",
		PageMode.Loaded => "Loaded",
		PageMode.Unsupported => "Unsupported",
		PageMode.DriverLoadFailed => "Driver Load Failed",
		PageMode.HiiResourcesRegular => "HII Resources (Regular)",
		PageMode.HiiResourcesProtected => "HII Resources (Protected)",
		PageMode.HiiResourcesOther => "HII Resources (Other)",
		PageMode.WriteProtectedAsus => "Write Protected (ASUS)",
		PageMode.WriteProtectedAsRock => "Write Protected (ASRock)",
		PageMode.WriteProtectedOther => "Write Protected (Other)",
		_ => throw new UnreachableException()
	};

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(CanUndo))]
	[NotifyPropertyChangedFor(nameof(CanRedo))]
	[NotifyPropertyChangedFor(nameof(CanMerge))]
	[NotifyPropertyChangedFor(nameof(CanApplyMerge))]
	[NotifyPropertyChangedFor(nameof(CanWrite))]
	[NotifyPropertyChangedFor(nameof(CanRestore))]
	[NotifyPropertyChangedFor(nameof(CanToggleCompareToDefaults))]
	[NotifyPropertyChangedFor(nameof(CanToggleViewChanges))]
	[NotifyCanExecuteChangedFor(nameof(UndoCommand))]
	[NotifyCanExecuteChangedFor(nameof(RedoCommand))]
	[NotifyCanExecuteChangedFor(nameof(ApplyRecommendationsCommand))]
	[NotifyCanExecuteChangedFor(nameof(WriteToNvramCommand))]
	[NotifyCanExecuteChangedFor(nameof(RestoreCommand))]
	[NotifyCanExecuteChangedFor(nameof(ToggleCompareToDefaultsCommand))]
	[NotifyCanExecuteChangedFor(nameof(ToggleViewChangesCommand))]
	public partial bool IsLoaded { get; set; }

	[ObservableProperty]
	public partial string SearchText { get; set; } = string.Empty;

	[ObservableProperty]
	public partial bool FilterPath { get; set; } = true;

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
	[NotifyPropertyChangedFor(nameof(CompareToDefaultsVisibility))]
	public partial bool CompareToDefaults { get; set; }

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(NormalVisibility))]
	[NotifyPropertyChangedFor(nameof(ViewChangesVisibility))]
	public partial bool ViewChanges { get; set; }

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(CompareToDefaultsLabel))]
	public partial int CompareToDefaultsCount { get; set; }

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(ViewChangesLabel))]
	[NotifyPropertyChangedFor(nameof(CanWrite))]
	[NotifyCanExecuteChangedFor(nameof(WriteToNvramCommand))]
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

	partial void OnSearchTextChanged(string value) => RefreshFilter();

	partial void OnFilterPathChanged(bool value) => RefreshFilter();

	partial void OnFilterSettingChanged(bool value) => RefreshFilter();

	partial void OnFilterDescriptionChanged(bool value) => RefreshFilter();

	partial void OnFilterCurrentChanged(bool value) => RefreshFilter();

	partial void OnFilterModeChanged(FilterMode value) => RefreshFilter();

	partial void OnCompareToDefaultsChanged(bool value)
	{
		if (value)
			ViewChanges = false;
	}

	partial void OnViewChangesChanged(bool value)
	{
		if (value)
			CompareToDefaults = false;
	}

	public void RefreshFilter() => RefreshFilterAction?.Invoke();

	[RelayCommand]
	private void SetFilterMode(string value) => FilterMode = Enum.Parse<FilterMode>(value);

	public Visibility NormalVisibility => ViewChanges || CompareToDefaults ? Visibility.Collapsed : Visibility.Visible;

	public Visibility CompareToDefaultsVisibility => CompareToDefaults ? Visibility.Visible : Visibility.Collapsed;

	public Visibility ViewChangesVisibility => ViewChanges ? Visibility.Visible : Visibility.Collapsed;

	public bool CanUndo => IsLoaded && _undoStates.Count > 0;

	public bool CanRedo => IsLoaded && _redoStates.Count > 0;

	public bool CanMerge => IsLoaded && HasRecommendations;

	public bool CanApplyMerge => CanMerge && MergeCount > 0;

	public bool CanToggleCompareToDefaults => IsLoaded;

	public bool CanToggleViewChanges => IsLoaded;

	public bool CanRestore => IsLoaded;

	public bool CanWrite => IsLoaded && ModifiedCount > 0;

	public string CompareToDefaultsLabel => $"Compare to Defaults ({CompareToDefaultsCount})";

	public string ViewChangesLabel => $"View Changes ({ModifiedCount})";

	public async Task ReadFromNvramAsync()
	{
		PageState = PageMode.Reading;
		IsLoaded = false;
		CompareToDefaults = false;
		ViewChanges = false;
		SearchText = string.Empty;

		(PageMode result, IReadOnlyList<Setting> settings) = await biosService.ReadFromNvramAsync();
		if (result != PageMode.Loaded)
		{
			PageState = result;
			return;
		}

		_settings.Clear();
		_settings.AddRange(settings);
		_settingStates.Clear();

		foreach (Setting setting in _settings)
		{
			_settingStates[setting] = new SettingState
			{
				Value = SettingState.GetCanonicalValue(setting, setting.Value),
				OriginalValue = SettingState.GetCanonicalValue(setting, setting.Value)
			};
		}

		_undoStates.Clear();
		_redoStates.Clear();
		MergeCount = 0;
		TreeNodes.Clear();
		CompareNodes.Clear();
		DiffNodes.Clear();

		_recommendedTree = BuildTree("Recommended", static node => node.HasPendingRecommendation);
		TreeNodes.Add(_recommendedTree.Root);

		_allTree = BuildTree("All Settings", static _ => true);
		TreeNodes.Add(_allTree.Root);

		_compareTree = BuildTree("Differences", static node => !string.IsNullOrEmpty(node.Setting?.Default) && !node.IsDefault);
		CompareNodes.Add(_compareTree.Root);

		_changesTree = BuildTree("Changes", static node => node.State?.IsModified == true);
		DiffNodes.Add(_changesTree.Root);

		UpdateState();
		SyncMergeCount();

		IsLoaded = true;
		PageState = PageMode.Loaded;

		_ = backupService.BackupAsync(_settings);
	}

	[RelayCommand(CanExecute = nameof(CanUndo))]
	private void Undo() => MoveState(_undoStates, _redoStates);

	[RelayCommand(CanExecute = nameof(CanRedo))]
	private void Redo() => MoveState(_redoStates, _undoStates);

	private void MoveState(Stack<Dictionary<Setting, string?>> from, Stack<Dictionary<Setting, string?>> to)
	{
		if (from.Count == 0)
			return;

		to.Push(CaptureState());
		RestoreState(from.Pop());
		UpdateState();
		SyncMergeCount();
	}

	[RelayCommand(CanExecute = nameof(CanApplyMerge))]
	private void ApplyRecommendations(int count)
	{
		Dictionary<Setting, string?> previous = CaptureState();
		var targets = _settings
			.Where(setting => SettingState.HasPendingRecommendation(setting, _settingStates[setting]))
			.Take(count)
			.ToList();

		if (targets.Count == 0)
			return;

		foreach (Setting setting in targets)
		{
			SettingState state = _settingStates[setting];
			if (setting.RecommendedOption != null)
				state.Value = setting.RecommendedOption.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
			else if (setting.RecommendedValue != null)
				state.Value = setting.RecommendedValue;
		}

		PushUndoState(previous);
		UpdateState();
	}

	[RelayCommand(CanExecute = nameof(CanToggleCompareToDefaults))]
	private void ToggleCompareToDefaults() => RefreshFilter();

	[RelayCommand(CanExecute = nameof(CanToggleViewChanges))]
	private void ToggleViewChanges() => RefreshFilter();

	[RelayCommand(CanExecute = nameof(CanRestore))]
	private async Task RestoreAsync()
	{
		string? file = await filePickerService.PickSingleFileAsync("HII Backup", ["*.json"], backupService.BackupDirectory);
		if (string.IsNullOrEmpty(file))
			return;

		PageState = PageMode.Writing;
		PageMode result = await biosService.RestoreFromBackupAsync(file);
		if (result != PageMode.Loaded)
		{
			PageState = result;
			return;
		}


		string json = await File.ReadAllTextAsync(file);
		BackupFile? backup = JsonSerializer.Deserialize(json, BackupJsonContext.Default.BackupFile);
		if (backup != null)
		{
			Dictionary<(string VariableName, string VariableGuid, uint Offset), string> backupMap = [with(backup.Settings.Count)];
			foreach (BackupSetting s in backup.Settings)
				backupMap[(s.VariableName, s.VariableGuid, s.Offset)] = s.Value;

			foreach (Setting setting in _settings)
			{
				string guidStr = HiiHelper.GetGuidString(setting.VariableGuid);
				if (backupMap.TryGetValue((setting.VariableName, guidStr, setting.Offset), out string? value))
				{
					setting.Value = value;
					_settingStates[setting].Value = SettingState.GetCanonicalValue(setting, value);
					_settingStates[setting].Commit();
				}
			}
		}


		UpdateState();
		SyncMergeCount();
		_ = backupService.BackupAsync(_settings);
		PageState = PageMode.Loaded;
	}

	[RelayCommand(CanExecute = nameof(CanWrite))]
	private async Task WriteToNvramAsync()
	{
		PageState = PageMode.Writing;

		var modified = _settingStates
			.Where(pair => pair.Value.IsModified)
			.ToDictionary(pair => pair.Key, pair => pair.Value);

		(PageMode result, IReadOnlyList<Setting> failed) = await biosService.WriteToNvramAsync(modified);
		if (result != PageMode.Loaded)
		{
			PageState = result;
			return;
		}

		if (failed.Count == 0)
		{
			foreach (Setting setting in _settings)
			{
				if (_settingStates[setting].IsModified)
				{
					_settingStates[setting].Value = setting.Value;
					_settingStates[setting].Commit();
				}
			}

			UpdateState();
			SyncMergeCount();
			PageState = PageMode.Loaded;
			return;
		}

		foreach (Setting setting in _settings)
		{
			if (failed.Contains(setting))
				continue;

			_settingStates[setting].Value = setting.Value;
			_settingStates[setting].Commit();
		}

		UpdateState();
		SyncMergeCount();
		PageState = PageMode.Loaded;
	}

	[RelayCommand]
	private async Task UnlockWithPasswordAsync()
	{
		BiosPasswordDialogViewModel vm = new();
		DialogResult result = await dialogService.ShowDialogAsync(vm);
		if (result != DialogResult.Primary)
			return;

		if (_settingStates.Any(pair => pair.Value.IsModified))
			await WriteToNvramAsync();
		else
			await ReadFromNvramAsync();
	}

	[RelayCommand]
	private async Task RestartIntoBiosAsync()
	{
		if (await dialogService.ShowConfirmationDialogAsync("Restart into BIOS", "Are you sure you want to restart into the BIOS/UEFI firmware settings?", "Restart", "Cancel") != DialogResult.Primary)
			return;

		if (!ShutdownHelper.TrySetOsIndications(out int win32Error))
		{
			string details = ShutdownHelper.FormatWin32Error(win32Error);
			DialogResult result = await dialogService.ShowConfirmationDialogAsync("Restart into BIOS failed", $"Failed to set OsIndications to request firmware UI: {details} (0x{win32Error:X}).\n\nThe firmware may be write-protected or not support this feature. Restart anyway?", "Restart anyway", "Cancel");

			if (result != DialogResult.Primary)
				return;

			ShutdownHelper.Restart();
			return;
		}

		ShutdownHelper.Restart();
	}

	public void BeginEdit(Node? node)
	{
		if (node == null)
			return;

		node.BeginCellEdit();
	}

	public bool CommitEdit(Node? node)
	{
		if (node == null)
			return false;

		Dictionary<Setting, string?> previous = CaptureState();
		bool changed = node.CommitCellEdit();
		if (changed)
			PushUndoState(previous);
		return changed;
	}

	public void RefreshAfterEdit()
	{
		UpdateState();
	}

	public void UpdateNodeCounts()
	{
		foreach (Node root in TreeNodes)
			Recount(root, static _ => 1);
		foreach (Node root in CompareNodes)
			Recount(root, static _ => 1);
		foreach (Node root in DiffNodes)
			Recount(root, static _ => 1);
	}

	private int Recount(Node node, Func<Node, int> leafWeight)
	{
		int count = 0;
		foreach (Node child in node.Children)
		{
			count += child.NodeKind == NodeKind.Setting
				? (MatchesFilter(child) ? leafWeight(child) : 0)
				: Recount(child, leafWeight);
		}

		node.DisplayName = node.NodeKind == NodeKind.Root && SearchText.Length > 0
			? $"Results ({count})"
			: $"{node.BaseDisplayName} ({count})";
		return count;
	}

	public bool MatchesFilter(object item)
	{
		if (item is not Node node)
			return false;

		if (node.NodeKind == NodeKind.Root)
		{
			if (!string.IsNullOrWhiteSpace(SearchText) && node.BaseDisplayName == "Recommended")
				return false;
			return CountVisibleChildren(node) > 0;
		}

		if (node.NodeKind == NodeKind.Path)
		{
			if (!string.IsNullOrWhiteSpace(SearchText) && FilterPath)
			{
				string t = SearchText.Trim();
				bool pm(string v) => FilterMode == FilterMode.ExactMatch ? string.Equals(v, t, StringComparison.OrdinalIgnoreCase) : v.Contains(t, StringComparison.OrdinalIgnoreCase);
				if (pm(node.DisplayName))
					return true;
			}
			return CountVisibleSettings(node.Children) > 0;
		}

		if (CompareToDefaults && node.IsDefault)
			return false;

		if (ViewChanges && !node.IsModified)
			return false;

		if (string.IsNullOrWhiteSpace(SearchText))
			return true;

		string term = SearchText.Trim();
		StringComparison comparison = StringComparison.OrdinalIgnoreCase;

		bool textMatches(string value) => FilterMode == FilterMode.ExactMatch
			? string.Equals(value, term, comparison)
			: value.Contains(term, comparison);

		if (FilterPath && IsAncestorPathMatch(node, textMatches))
			return true;

		if (FilterSetting && textMatches(node.DisplayName))
			return true;

		if (FilterDescription && textMatches(node.Description))
			return true;

		if (FilterCurrent && textMatches(node.DisplayCurrent))
			return true;

		return false;
	}

	private static bool IsAncestorPathMatch(Node node, Func<string, bool> textMatches)
	{
		Node? cur = node.Parent;
		while (cur != null)
		{
			if (cur.NodeKind == NodeKind.Path && textMatches(cur.DisplayName))
				return true;

			cur = cur.Parent;
		}

		return false;
	}

	private int CountVisibleChildren(Node parent)
	{
		int count = 0;
		foreach (Node child in parent.Children)
		{
			if (MatchesFilter(child))
				count++;
		}
		return count;
	}

	private int CountVisibleSettings(IEnumerable<Node> nodes)
	{
		int count = 0;
		foreach (Node node in nodes)
		{
			if (node.NodeKind == NodeKind.Setting)
			{
				if (MatchesFilter(node))
					count++;
			}
			else
			{
				count += CountVisibleSettings(node.Children);
			}
		}

		return count;
	}

	private TreeState BuildTree(string rootName, Func<Node, bool> include)
	{
		var rootNode = new Node(NodeKind.Root, rootName);
		var pathMap = new Dictionary<string, Node>(StringComparer.OrdinalIgnoreCase);
		var settingNodes = new Dictionary<Setting, Node>(_settings.Count);
		int order = 0;

		foreach (Setting setting in _settings)
		{
			string[] segs = setting.PathSegments;

			Node currentParent = rootNode;
			string currentChain = string.Empty;

			for (int i = 0; i < segs.Length; i++)
			{
				string seg = segs[i];
				currentChain = i == 0 ? seg : $"{currentChain} / {seg}";

				if (!pathMap.TryGetValue(currentChain, out Node? pathNode))
				{
					pathNode = new Node(NodeKind.Path, seg)
					{
						Parent = currentParent,
						Order = order
					};
					currentParent.Children.Add(pathNode);
					pathMap[currentChain] = pathNode;
				}
				currentParent = pathNode;
			}

			var settingNode = new Node(NodeKind.Setting, setting.Name, setting.Description, setting, _settingStates[setting])
			{
				Parent = currentParent,
				Order = order
			};
			order++;
			settingNodes[setting] = settingNode;
			if (include(settingNode))
				currentParent.Children.Add(settingNode);
		}

		SyncPaths(pathMap.Values);
		return new TreeState(rootNode, pathMap, settingNodes);
	}

	private static void SyncTree(TreeState? tree, Func<Node, bool> include)
	{
		if (tree == null)
			return;

		foreach (Node node in tree.SettingNodes.Values)
		{
			Node pathNode = node.Parent!;
			bool included = include(node);
			bool present = pathNode.Children.Contains(node);
			if (included && !present)
				pathNode.Children.InsertOrdered(node);
			else if (!included && present)
				pathNode.Children.Remove(node);
		}

		SyncPaths(tree.PathNodes.Values);
	}

	private static void SyncPaths(IEnumerable<Node> paths)
	{
		foreach (Node pathNode in paths.OrderByDescending(GetDepth))
		{
			Node parent = pathNode.Parent!;
			if (pathNode.Children.Count > 0 && !parent.Children.Contains(pathNode))
				parent.Children.InsertOrdered(pathNode);
			else if (pathNode.Children.Count == 0)
				parent.Children.Remove(pathNode);
		}
	}

	private static int GetDepth(Node node)
	{
		int depth = 0;
		while (node.Parent != null)
		{
			depth++;
			node = node.Parent;
		}
		return depth;
	}

	private void UpdateState()
	{
		int compare = 0;
		int modified = 0;
		int recommended = 0;
		foreach (Setting setting in _settings)
		{
			SettingState state = _settingStates[setting];
			if (!string.IsNullOrEmpty(setting.Default) && !SettingState.MatchesDefault(setting, state))
				compare++;

			if (state.IsModified)
				modified++;

			if (SettingState.HasPendingRecommendation(setting, state))
				recommended++;
		}

		CompareToDefaultsCount = compare;
		ModifiedCount = modified;
		RecommendedCount = recommended;
		HasRecommendations = RecommendedCount > 0;
		UndoCommand.NotifyCanExecuteChanged();
		RedoCommand.NotifyCanExecuteChanged();
		SyncTree(_recommendedTree, static node => node.HasPendingRecommendation);
		SyncTree(_compareTree, static node => !string.IsNullOrEmpty(node.Setting?.Default) && !node.IsDefault);
		SyncTree(_changesTree, static node => node.State?.IsModified == true);
		UpdateNodeCounts();
		RefreshFilterOnlyAction?.Invoke();
	}


	private void SyncMergeCount() => MergeCount = RecommendedCount;

	private void PushUndoState(Dictionary<Setting, string?> previousState)
	{
		Dictionary<Setting, string?> current = CaptureState();
		if (StatesEqual(previousState, current))
			return;

		_undoStates.Push(previousState);
		_redoStates.Clear();
	}

	private static Dictionary<Setting, string?> CaptureState(IEnumerable<Setting> settings, Dictionary<Setting, SettingState> states) =>
		settings.ToDictionary(setting => setting, setting => states[setting].Value);

	private Dictionary<Setting, string?> CaptureState() => CaptureState(_settings, _settingStates);

	private void RestoreState(IEnumerable<KeyValuePair<Setting, string?>> state)
	{
		foreach ((Setting setting, string? value) in state)
		{
			if (_settingStates.TryGetValue(setting, out SettingState? current))
			{
				current.Value = value;
			}
		}
	}

	private static bool StatesEqual(Dictionary<Setting, string?> left, Dictionary<Setting, string?> right) =>
		left.Count == right.Count && left.All(pair => right.TryGetValue(pair.Key, out string? other) && pair.Value == other);
}
