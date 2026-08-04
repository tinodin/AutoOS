using System.Collections.ObjectModel;
using AutoOS.Core.Helpers.BIOS;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AutoOS.App.Views.Settings.BIOS;

public partial class BiosSettingViewModel : ObservableObject
{
	public event EventHandler RecommendedNodeRestored;
	public event EventHandler RecommendationStateChanged;

	private bool _isAnyModified;
	public bool IsAnyModified
	{
		get => _isAnyModified;
		private set
		{
			if (SetProperty(ref _isAnyModified, value))
			{
				OnPropertyChanged(nameof(CanImport));
			}
		}
	}

	private bool _hasRecommendations;
	public bool HasRecommendations
	{
		get => _hasRecommendations;
		private set
		{
			if (SetProperty(ref _hasRecommendations, value))
			{
				OnPropertyChanged(nameof(CanMerge));
				OnPropertyChanged(nameof(CanApplyMerge));
			}
		}
	}

	private bool _isLoaded;
	public bool IsLoaded
	{
		get => _isLoaded;
		private set
		{
			if (SetProperty(ref _isLoaded, value))
			{
				OnPropertyChanged(nameof(CanMerge));
				OnPropertyChanged(nameof(CanApplyMerge));
				OnPropertyChanged(nameof(CanImport));
				OnPropertyChanged(nameof(CanUndo));
				OnPropertyChanged(nameof(CanRedo));
			}
		}
	}


	public bool CanMerge => IsLoaded && HasRecommendations;

	public bool CanApplyMerge => CanMerge && MergeCount > 0;

	public bool CanImport => IsLoaded && IsAnyModified;
	private int _mergeCount;
	private int _lastRecommendedCount;

	public int MergeCount
	{
		get => _mergeCount;
		set
		{
			int clamped = Math.Clamp(value, 0, RecommendedCount);
			if (SetProperty(ref _mergeCount, clamped))
			{
				OnPropertyChanged(nameof(CanApplyMerge));
			}
		}
	}

	private int _modifiedCount;
	public int ModifiedCount
	{
		get => _modifiedCount;
		private set
		{
			if (SetProperty(ref _modifiedCount, value))
			{
				OnPropertyChanged(nameof(ViewChangesLabel));
			}
		}
	}

	public string ViewChangesLabel => $"View Changes ({ModifiedCount})";

	public void SetIsLoaded(bool isLoaded) => IsLoaded = isLoaded;

	private readonly Stack<List<SettingState>> _undoStates = [];
	private readonly Stack<List<SettingState>> _redoStates = [];
	private List<SettingState> _currentState = [];
	private List<SettingState> _batchStartState;
	private bool _isRestoringHistory;
	private readonly Dictionary<BiosSettingModel, BiosTreeNode> _modelToLeafMap = [];

	public bool CanUndo => IsLoaded && _undoStates.Count > 0;

	public bool CanRedo => IsLoaded && _redoStates.Count > 0;


	private string _searchText = string.Empty;
	public string SearchText
	{
		get => _searchText;
		set
		{
			if (SetProperty(ref _searchText, value))
			{
				RefreshFilter();
			}
		}
	}

	private bool _viewChanges;
	public bool ViewChanges
	{
		get => _viewChanges;
		set
		{
			if (SetProperty(ref _viewChanges, value))
			{
				RefreshFilter();
			}
		}
	}

	public ObservableCollection<BiosTreeNode> TreeNodes { get; } = [];

	public ObservableCollection<BiosTreeNode> DiffNodes { get; } = [];

	private BiosTreeNode _recommendedRoot;

	private readonly List<BiosTreeNode> _allLeaves = [];

	private List<string> _originalLines;

	public Action RefreshFilterAction { get; set; }
	public Action ExpandDiffNodesAction { get; set; }
	public Action ExpandAllNodesAction { get; set; }

	private bool _filterSetting = true;
	public bool FilterSetting
	{
		get => _filterSetting;
		set { if (SetProperty(ref _filterSetting, value)) RefreshFilter(); }
	}

	private bool _filterDescription = false;
	public bool FilterDescription
	{
		get => _filterDescription;
		set { if (SetProperty(ref _filterDescription, value)) RefreshFilter(); }
	}

	private bool _filterCurrent = false;
	public bool FilterCurrent
	{
		get => _filterCurrent;
		set { if (SetProperty(ref _filterCurrent, value)) RefreshFilter(); }
	}

	public enum FilterModeType { Contains, ExactMatch }

	private FilterModeType _filterMode = FilterModeType.Contains;
	public FilterModeType FilterMode
	{
		get => _filterMode;
		set
		{
			if (SetProperty(ref _filterMode, value))
			{
				OnPropertyChanged(nameof(FilterContains));
				OnPropertyChanged(nameof(FilterExactMatch));
				RefreshFilter();
			}
		}
	}

	public bool FilterContains
	{
		get => _filterMode == FilterModeType.Contains;
		set { if (value) FilterMode = FilterModeType.Contains; }
	}

	public bool FilterExactMatch
	{
		get => _filterMode == FilterModeType.ExactMatch;
		set { if (value) FilterMode = FilterModeType.ExactMatch; }
	}

	private void RefreshFilter() => RefreshFilterAction?.Invoke();

	public void BuildTree(List<BiosSettingModel> parsed)
	{
		TreeNodes.Clear();
		_allLeaves.Clear();
		_modelToLeafMap.Clear();
		_recommendedRoot = null;
		IsAnyModified = false;
		ModifiedCount = 0;
		MergeCount = 0;
		HasRecommendations = false;

		if (parsed == null || parsed.Count == 0) return;

		_originalLines = parsed[0].OriginalLines;

		var groups = parsed.GroupBy(setting => setting.SetupQuestion?.Trim() ?? string.Empty, StringComparer.OrdinalIgnoreCase).ToList();

		var ruleOrder = BiosSettingRecommendationsList.Rules
			.Select((rule, i) => new { rule.SetupQuestion, rule.RecommendedOption, Index = i })
			.GroupBy(item => (item.SetupQuestion?.ToLowerInvariant(), item.RecommendedOption?.ToLowerInvariant()))
			.ToDictionary(group => group.Key, group => group.First().Index);

		var allGroupNodes = new List<BiosTreeNode>();

		foreach (IGrouping<string, BiosSettingModel>? grp in groups)
		{
			var members = grp.ToList();

			if (members.Count == 1)
			{
				BiosTreeNode leaf = MakeLeaf(members[0]);
				allGroupNodes.Add(leaf);
			}
			else
			{
				var groupNode = new BiosTreeNode
				{
					NodeKind = NodeKind.Group,
					DisplayName = $"{grp.Key} ({members.Count})"
				};

				foreach (BiosSettingModel? m in members)
				{
					BiosTreeNode leaf = MakeLeaf(m);
					groupNode.Children.Add(leaf);
				}

				groupNode.SubscribeToChildrenErrors();
				allGroupNodes.Add(groupNode);
			}
		}

		foreach (BiosTreeNode leaf in _allLeaves)
		{
			leaf.Model.ModifiedChanged += (_, _) => OnModelModified(leaf);
			leaf.Model.ErrorsChanged += (_, _) => OnModelErrorsChanged(leaf);
		}

		var recommendedRoot = new BiosTreeNode
		{
			NodeKind = NodeKind.Root,
			DisplayName = "Recommended"
		};

		foreach (BiosTreeNode node in allGroupNodes)
		{
			if (node.NodeKind == NodeKind.Leaf)
			{
				if (node.Model?.IsRecommended == true)
				{
					BiosTreeNode clone = CloneNode(node);
					recommendedRoot.Children.Add(clone);
				}
			}
			else
			{
				var recommendedChildren = node.Children.Where(child => child.Model?.IsRecommended == true).ToList();

				if (recommendedChildren.Count == 1)
				{
					BiosTreeNode clone = CloneNode(recommendedChildren[0]);
					recommendedRoot.Children.Add(clone);
				}
				else if (recommendedChildren.Count > 1)
				{
					var groupClone = new BiosTreeNode
					{
						NodeKind = NodeKind.Group,
						DisplayName = node.DisplayName
					};

					foreach (BiosTreeNode? child in recommendedChildren)
					{
						BiosTreeNode childClone = CloneNode(child);
						groupClone.Children.Add(childClone);
					}

					recommendedRoot.Children.Add(groupClone);
				}
			}
		}

		int recommendedCount = CountLeaves(recommendedRoot);
		recommendedRoot.DisplayName = $"Recommended ({recommendedCount})";

		var allRoot = new BiosTreeNode
		{
			NodeKind = NodeKind.Root,
			DisplayName = "All Settings",
			SortOrder = 1
		};

		foreach (BiosTreeNode node in allGroupNodes)
			allRoot.Children.Add(node);

		int allCount = CountLeaves(allRoot);
		allRoot.DisplayName = $"All Settings ({allCount})";

		_recommendedRoot = recommendedRoot;
		TreeNodes.Add(allRoot);
		if (recommendedCount > 0)
			TreeNodes.Insert(0, recommendedRoot);

		HasRecommendations = recommendedRoot.Children.Count > 0;
		OnPropertyChanged(nameof(RecommendedCount));
		ResetMergeCount();
		UpdateDiffNodes();
		ResetHistory();
	}

	private void UpdateDiffNodes()
	{
		DiffNodes.Clear();

		var modifiedLeaves = _allLeaves.Where(leaf => leaf.Model?.IsModified == true).ToList();
		ModifiedCount = modifiedLeaves.Count;

		var changesRoot = new BiosTreeNode
		{
			NodeKind = NodeKind.Root,
			DisplayName = $"Changes ({modifiedLeaves.Count})"
		};

		BiosTreeNode? allRoot = TreeNodes.LastOrDefault();
		if (allRoot != null)
		{
			foreach (BiosTreeNode node in allRoot.Children)
			{
				if (node.NodeKind == NodeKind.Leaf)
				{
					if (node.Model?.IsModified == true)
						changesRoot.Children.Add(node);
					continue;
				}

				var changedChildren = node.Children.Where(child => child.Model?.IsModified == true).ToList();
				if (changedChildren.Count == 0) continue;
				if (changedChildren.Count == 1)
				{
					changesRoot.Children.Add(changedChildren[0]);
					continue;
				}

				string baseName = GetGroupBaseName(node.DisplayName);
				var group = new BiosTreeNode
				{
					NodeKind = NodeKind.Group,
					DiffGroupKey = baseName,
					DisplayName = $"{baseName} ({changedChildren.Count})"
				};
				foreach (BiosTreeNode? child in changedChildren)
					group.Children.Add(child);
				group.SubscribeToChildrenErrors();
				changesRoot.Children.Add(group);
			}

			DiffNodes.Add(changesRoot);
			ExpandDiffNodesAction?.Invoke();
			RefreshFilterAction?.Invoke();
		}
	}

	private void UpdateDiffNodeIncremental(BiosTreeNode leaf)
	{
		BiosTreeNode? changesRoot = DiffNodes.FirstOrDefault();
		if (changesRoot == null)
		{
			UpdateDiffNodes();
			return;
		}

		if (leaf.Model?.IsModified == true)
			AddLeafToDiffTree(changesRoot, leaf);
		else
			RemoveLeafFromDiffTree(changesRoot, leaf);

		int count = CountDiffTreeLeaves(changesRoot);
		ModifiedCount = count;
		changesRoot.DisplayName = $"Changes ({count})";
		RefreshFilterAction?.Invoke();
	}

	private void UpdateDiffNodesBulk(HashSet<BiosSettingModel> modifiedModels)
	{
		BiosTreeNode? changesRoot = DiffNodes.FirstOrDefault();
		if (changesRoot == null)
		{
			UpdateDiffNodes();
			return;
		}

		foreach (BiosSettingModel model in modifiedModels)
		{
			if (_modelToLeafMap.TryGetValue(model, out BiosTreeNode? leaf))
			{
				if (leaf.Model?.IsModified == true)
					AddLeafToDiffTree(changesRoot, leaf);
				else
					RemoveLeafFromDiffTree(changesRoot, leaf);
			}
		}

		int count = CountDiffTreeLeaves(changesRoot);
		ModifiedCount = count;
		changesRoot.DisplayName = $"Changes ({count})";
	}

	private static int CountDiffTreeLeaves(BiosTreeNode root)
	{
		int count = 0;
		foreach (BiosTreeNode child in root.Children)
		{
			if (child.NodeKind != NodeKind.Group)
				count++;
			else
				count += child.Children.Count;
		}
		return count;
	}

	private static string GetGroupBaseName(string displayName)
	{
		int parenIndex = displayName.LastIndexOf(" (");
		return parenIndex > 0 ? displayName.Substring(0, parenIndex) : displayName;
	}

	private void AddLeafToDiffTree(BiosTreeNode changesRoot, BiosTreeNode leaf)
	{
		BiosTreeNode parentGroup = FindParentGroup(leaf);

		if (parentGroup == null)
		{
			if (!changesRoot.Children.Contains(leaf))
				changesRoot.Children.Add(leaf);
			return;
		}

		var modifiedSiblings = parentGroup.Children
			.Where(child => child.Model?.IsModified == true)
			.ToList();

		if (modifiedSiblings.Count < 2)
		{
			if (!changesRoot.Children.Contains(leaf))
				changesRoot.Children.Add(leaf);
			return;
		}

		string baseName = GetGroupBaseName(parentGroup.DisplayName);
		BiosTreeNode? diffGroup = changesRoot.Children
			.OfType<BiosTreeNode>()
			.FirstOrDefault(group => group.NodeKind == NodeKind.Group && group.DiffGroupKey == baseName);

		if (diffGroup != null)
		{
			if (!diffGroup.Children.Contains(leaf))
			{
				diffGroup.Children.Add(leaf);
				diffGroup.DisplayName = $"{baseName} ({diffGroup.Children.Count})";
			}
		}
		else
		{
			diffGroup = new BiosTreeNode
			{
				NodeKind = NodeKind.Group,
				DiffGroupKey = baseName,
				DisplayName = $"{baseName} ({modifiedSiblings.Count})"
			};
			foreach (BiosTreeNode? sibling in modifiedSiblings)
			{
				changesRoot.Children.Remove(sibling);
				diffGroup.Children.Add(sibling);
			}
			diffGroup.SubscribeToChildrenErrors();
			changesRoot.Children.Add(diffGroup);
		}
	}

	private void RemoveLeafFromDiffTree(BiosTreeNode changesRoot, BiosTreeNode leaf)
	{
		if (changesRoot.Children.Remove(leaf))
		{
			return;
		}

		BiosTreeNode? diffGroup = changesRoot.Children
			.OfType<BiosTreeNode>()
			.FirstOrDefault(group => group.NodeKind == NodeKind.Group && group.Children.Contains(leaf));

		if (diffGroup == null) return;

		diffGroup.Children.Remove(leaf);

		if (diffGroup.Children.Count == 0)
		{
			changesRoot.Children.Remove(diffGroup);
		}
		else if (diffGroup.Children.Count == 1)
		{
			BiosTreeNode remaining = diffGroup.Children[0];
			diffGroup.Children.Clear();
			changesRoot.Children.Remove(diffGroup);
			changesRoot.Children.Add(remaining);
		}
		else
		{
			diffGroup.DisplayName = $"{diffGroup.DiffGroupKey} ({diffGroup.Children.Count})";
		}
	}

	private void RebuildRecommendedTree()
	{
		BiosTreeNode recommendedRoot = _recommendedRoot;
		BiosTreeNode? allRoot = TreeNodes.LastOrDefault();
		if (recommendedRoot == null || allRoot == null)
			return;

		recommendedRoot.Children.Clear();
		foreach (BiosTreeNode node in allRoot.Children)
		{
			if (node.NodeKind == NodeKind.Leaf)
			{
				if (HasPendingRecommendation(node))
					recommendedRoot.Children.Add(CloneNode(node));
				continue;
			}

			var pendingChildren = node.Children.Where(HasPendingRecommendation).ToList();
			if (pendingChildren.Count == 1)
			{
				recommendedRoot.Children.Add(CloneNode(pendingChildren[0]));
			}
			else if (pendingChildren.Count > 1)
			{
				var groupClone = new BiosTreeNode
				{
					NodeKind = NodeKind.Group,
					DisplayName = node.DisplayName
				};

				foreach (BiosTreeNode? child in pendingChildren)
					groupClone.Children.Add(CloneNode(child));

				groupClone.SubscribeToChildrenErrors();
				recommendedRoot.Children.Add(groupClone);
			}
		}

		int count = CountLeaves(recommendedRoot);
		recommendedRoot.DisplayName = $"Recommended ({count})";
		HasRecommendations = count > 0;

		if (count == 0)
			TreeNodes.Remove(recommendedRoot);
		else if (!TreeNodes.Contains(recommendedRoot))
		{
			TreeNodes.Insert(0, recommendedRoot);
			RecommendedNodeRestored?.Invoke(this, EventArgs.Empty);
		}

		OnPropertyChanged(nameof(RecommendedCount));
		SyncMergeCount();
	}

	private void UpdateRecommendedTreeIncremental(HashSet<BiosSettingModel> modifiedModels)
	{
		BiosTreeNode recommendedRoot = _recommendedRoot;
		BiosTreeNode? allRoot = TreeNodes.LastOrDefault();
		if (recommendedRoot == null || allRoot == null)
			return;

		var groupLookup = new Dictionary<string, BiosTreeNode>();
		foreach (BiosTreeNode child in recommendedRoot.Children)
		{
			if (child.NodeKind == NodeKind.Group)
				groupLookup[child.DisplayName] = child;
		}

		var nodesToRemove = new List<BiosTreeNode>();
		var groupsToClean = new List<BiosTreeNode>();
		
		foreach (BiosTreeNode node in recommendedRoot.Children)
		{
			if (node.NodeKind == NodeKind.Leaf && node.Model != null && modifiedModels.Contains(node.Model))
			{
				if (!HasPendingRecommendation(node))
					nodesToRemove.Add(node);
			}
			else if (node.NodeKind == NodeKind.Group)
			{
				int originalCount = node.Children.Count;
				for (int i = node.Children.Count - 1; i >= 0; i--)
				{
					BiosTreeNode child = node.Children[i];
					if (child.NodeKind == NodeKind.Leaf && 
						child.Model != null && 
						modifiedModels.Contains(child.Model) && 
						!HasPendingRecommendation(child))
					{
						node.Children.RemoveAt(i);
					}
				}

				if (node.Children.Count == 0)
					nodesToRemove.Add(node);
				else if (node.Children.Count == 1 && originalCount > 1)
					groupsToClean.Add(node);
			}
		}

		foreach (BiosTreeNode node in nodesToRemove)
		{
			recommendedRoot.Children.Remove(node);
			if (node.NodeKind == NodeKind.Group)
				groupLookup.Remove(node.DisplayName);
		}

		foreach (BiosTreeNode group in groupsToClean)
		{
			BiosTreeNode remaining = group.Children[0];
			int index = recommendedRoot.Children.IndexOf(group);
			recommendedRoot.Children.RemoveAt(index);
			recommendedRoot.Children.Insert(index, CloneNode(remaining));
			groupLookup.Remove(group.DisplayName);
		}

		foreach (BiosSettingModel model in modifiedModels)
		{
			if (!_modelToLeafMap.TryGetValue(model, out BiosTreeNode? leaf))
				continue;
			
			if (!HasPendingRecommendation(leaf))
				continue;

			BiosTreeNode parentGroup = FindParentGroup(leaf);
			if (parentGroup == null)
			{
				bool exists = false;
				foreach (BiosTreeNode child in recommendedRoot.Children)
				{
					if (child.NodeKind == NodeKind.Leaf && child.Model == model)
					{
						exists = true;
						break;
					}
				}
				if (!exists)
					recommendedRoot.Children.Add(CloneNode(leaf));
			}
			else
			{
				if (!groupLookup.TryGetValue(parentGroup.DisplayName, out BiosTreeNode? groupNode))
				{
					var newGroup = new BiosTreeNode
					{
						NodeKind = NodeKind.Group,
						DisplayName = parentGroup.DisplayName
					};
					newGroup.Children.Add(CloneNode(leaf));
					newGroup.SubscribeToChildrenErrors();
					recommendedRoot.Children.Add(newGroup);
					groupLookup[parentGroup.DisplayName] = newGroup;
				}
				else
				{
					bool exists = false;
					foreach (BiosTreeNode child in groupNode.Children)
					{
						if (child.NodeKind == NodeKind.Leaf && child.Model == model)
						{
							exists = true;
							break;
						}
					}
					if (!exists)
						groupNode.Children.Add(CloneNode(leaf));
				}
			}
		}

		for (int i = recommendedRoot.Children.Count - 1; i >= 0; i--)
		{
			BiosTreeNode child = recommendedRoot.Children[i];
			if (child.NodeKind == NodeKind.Group && child.Children.Count == 1)
			{
				BiosTreeNode remaining = child.Children[0];
				recommendedRoot.Children.RemoveAt(i);
				recommendedRoot.Children.Insert(i, CloneNode(remaining));
				groupLookup.Remove(child.DisplayName);
			}
		}

		int count = CountLeaves(recommendedRoot);
		recommendedRoot.DisplayName = $"Recommended ({count})";
		HasRecommendations = count > 0;

		if (count == 0)
			TreeNodes.Remove(recommendedRoot);
		else if (!TreeNodes.Contains(recommendedRoot))
		{
			TreeNodes.Insert(0, recommendedRoot);
			RecommendedNodeRestored?.Invoke(this, EventArgs.Empty);
		}

		OnPropertyChanged(nameof(RecommendedCount));
		SyncMergeCount();
	}

	private void ResetMergeCount()
	{
		_lastRecommendedCount = RecommendedCount;
		OnPropertyChanged(nameof(RecommendedCount));
		_mergeCount = RecommendedCount;
		OnPropertyChanged(nameof(MergeCount));
		OnPropertyChanged(nameof(CanApplyMerge));
	}

	private void SyncMergeCount()
	{
		int newCount = RecommendedCount;
		OnPropertyChanged(nameof(RecommendedCount));

		if (_mergeCount == _lastRecommendedCount)
		{
			MergeCount = newCount;
		}
		else if (_mergeCount > newCount)
		{
			MergeCount = newCount;
		}
		else
		{
			OnPropertyChanged(nameof(CanApplyMerge));
		}

		_lastRecommendedCount = newCount;
	}

	private static bool HasPendingRecommendation(BiosTreeNode node)
	{
		BiosSettingModel model = node.Model;
		if (model == null)
			return false;

		if (model.RecommendedOption != null)
			return !ReferenceEquals(model.SelectedOption, model.RecommendedOption);

		return !string.IsNullOrEmpty(model.RecommendedValue) &&
			!string.Equals(model.Value, model.RecommendedValue, StringComparison.Ordinal);
	}

	private static int CountLeaves(BiosTreeNode node)
	{
		if (node.NodeKind == NodeKind.Leaf)
			return 1;

		return node.Children.Sum(CountLeaves);
	}

	private BiosTreeNode MakeLeaf(BiosSettingModel model)
	{
		var leaf = new BiosTreeNode
		{
			NodeKind = NodeKind.Leaf,
			DisplayName = model.SetupQuestion ?? string.Empty,
			Model = model
		};
		_allLeaves.Add(leaf);
		_modelToLeafMap[model] = leaf;
		return leaf;
	}

	private static BiosTreeNode CloneNode(BiosTreeNode source)
	{
		if (source.NodeKind == NodeKind.Leaf)
		{
			return new BiosTreeNode
			{
				NodeKind = NodeKind.Leaf,
				DisplayName = source.DisplayName,
				Model = source.Model
			};
		}

		var clone = new BiosTreeNode
		{
			NodeKind = source.NodeKind,
			DisplayName = source.DisplayName
		};
		foreach (BiosTreeNode child in source.Children)
			clone.Children.Add(CloneNode(child));
		return clone;
	}

	private BiosTreeNode FindParentGroup(BiosTreeNode leaf)
	{
		BiosTreeNode? allRoot = TreeNodes.LastOrDefault();
		if (allRoot == null) return null;

		foreach (BiosTreeNode node in allRoot.Children)
		{
			if (node.NodeKind == NodeKind.Group && node.Children.Contains(leaf))
				return node;
		}
		return null;
	}

	public void ApplyChangesToLines()
	{
		foreach (BiosTreeNode? leaf in _allLeaves.Where(leafItem => leafItem.Model?.IsModified == true))
		{
			if (leaf.Model.HasValueField)
				BiosSettingUpdater.UpdateValue(leaf.Model, _originalLines);
			else if (leaf.Model.HasOptions)
				BiosSettingUpdater.UpdateOption(leaf.Model, _originalLines);
		}
	}

	public void WriteToNvram(string nvramPath)
	{
		if (_originalLines != null)
			File.WriteAllLines(nvramPath, _originalLines);
	}

	public void ApplyRecommendations(int count)
	{
		BeginHistoryBatch();

		BiosTreeNode recommendedRoot = _recommendedRoot;
		if (recommendedRoot == null)
		{
			EndHistoryBatch();
			return;
		}

		var recommendedLeaves = recommendedRoot.Children
			.SelectMany(node => node.NodeKind == NodeKind.Leaf ? [node] : node.Children)
			.Where(node => node.NodeKind == NodeKind.Leaf && node.Model?.IsRecommended == true)
			.Take(count)
			.ToList();

		BiosSettingModel.IsBatchMode = true;

		try
		{
			foreach (BiosTreeNode? leaf in recommendedLeaves)
			{
				BiosSettingModel model = leaf.Model;
				model.OriginalValue ??= model.Value;
				model.OriginalSelectedOption ??= model.SelectedOption;

				if (model.RecommendedOption != null)
				{
					model.SelectedOption = model.RecommendedOption;
				}
				else if (!string.IsNullOrEmpty(model.RecommendedValue))
				{
					model.Value = model.RecommendedValue;
				}
			}
		}
		finally
		{
			BiosSettingModel.IsBatchMode = false;
		}

		var modifiedModels = recommendedLeaves.Select(leaf => leaf.Model).ToHashSet();
		BulkRefreshNodes(modifiedModels);
		UpdateDiffNodesBulk(modifiedModels);
		UpdateRecommendedTreeIncremental(modifiedModels);
		RefreshFilterAction?.Invoke();
		EndHistoryBatch();
	}

	public void Undo()
	{
		if (!CanUndo) return;

		_redoStates.Push(_currentState);
		RestoreState(_undoStates.Pop());
		ResetMergeCount();
		ExpandAllNodesAction?.Invoke();
		OnPropertyChanged(nameof(CanUndo));
		OnPropertyChanged(nameof(CanRedo));
	}

	public void Redo()
	{
		if (!CanRedo) return;

		_undoStates.Push(_currentState);
		RestoreState(_redoStates.Pop());
		ResetMergeCount();
		OnPropertyChanged(nameof(CanUndo));
		OnPropertyChanged(nameof(CanRedo));
	}

	private void OnModelModified(BiosTreeNode leaf)
	{
		if (BiosSettingModel.IsBatchMode)
			return;

		IsAnyModified = _allLeaves.Any(leaf => leaf.Model?.IsModified == true);

		BiosTreeNode? parent = FindParentGroup(leaf);
		parent?.RaiseIsModifiedChanged();
		parent?.RaiseDisplayCurrentChanged();
		parent?.RaiseHasPendingRecommendationChanged();
		leaf.RaiseIsModifiedChanged();
		leaf.RaiseDisplayCurrentChanged();
		leaf.RaiseHasPendingRecommendationChanged();
		UpdateDiffNodeIncremental(leaf);

		bool wasInRecommended = _recommendedRoot != null && GetAllNodes(_recommendedRoot).Any(node => node.NodeKind == NodeKind.Leaf && node.Model == leaf.Model);
		bool isPending = HasPendingRecommendation(leaf);

		if (wasInRecommended != isPending)
		{
			RebuildRecommendedTree();
		}

		RecommendationStateChanged?.Invoke(this, EventArgs.Empty);

		if (!_isRestoringHistory && _batchStartState == null)
			RecordCurrentState();
	}

	private void OnModelErrorsChanged(BiosTreeNode leaf)
	{
		BiosTreeNode? parent = FindParentGroup(leaf);
		parent?.RaiseDisplayCurrentChanged();
		parent?.RaiseHasErrorsChanged();
		parent?.RaiseErrorsChanged(nameof(BiosTreeNode.DisplayCurrent));
		leaf.RaiseDisplayCurrentChanged();
		leaf.RaiseHasErrorsChanged();
		leaf.RaiseErrorsChanged(nameof(BiosTreeNode.DisplayCurrent));

		foreach (BiosTreeNode node in GetAllNodes(TreeNodes.FirstOrDefault()))
			node.RaiseDisplayCurrentChanged();

		RefreshFilter();

		RecommendationStateChanged?.Invoke(this, EventArgs.Empty);
	}

	private void ResetHistory()
	{
		_undoStates.Clear();
		_redoStates.Clear();
		_currentState = CaptureState();
		OnPropertyChanged(nameof(CanUndo));
		OnPropertyChanged(nameof(CanRedo));
	}

	private void BeginHistoryBatch() => _batchStartState ??= _currentState;

	private void EndHistoryBatch()
	{
		if (_batchStartState == null) return;

		List<SettingState> nextState = CaptureState();
		if (!StatesEqual(_batchStartState, nextState))
		{
			_undoStates.Push(_batchStartState);
			_redoStates.Clear();
			_currentState = nextState;
			OnPropertyChanged(nameof(CanUndo));
			OnPropertyChanged(nameof(CanRedo));
		}

		_batchStartState = null;
	}

	public void BatchEdit(Action editAction)
	{
		BeginHistoryBatch();
		editAction();
		EndHistoryBatch();
	}

	private void RecordCurrentState()
	{
		List<SettingState> nextState = CaptureState();
		if (StatesEqual(_currentState, nextState)) return;

		_undoStates.Push(_currentState);
		_redoStates.Clear();
		_currentState = nextState;
		OnPropertyChanged(nameof(CanUndo));
		OnPropertyChanged(nameof(CanRedo));
	}

	private List<SettingState> CaptureState() =>
		[.. _allLeaves.Select(leaf => new SettingState(leaf.Model, leaf.Model.SelectedOption, leaf.Model.Value))];

	private void RestoreState(List<SettingState> state)
	{
		_isRestoringHistory = true;
		var modifiedModels = new HashSet<BiosSettingModel>();
		
		BiosSettingModel.IsBatchMode = true;
		
		try
		{
			foreach (SettingState setting in state)
			{
				if (setting.Model.HasOptions)
				{
					if (setting.Model.SelectedOption != setting.SelectedOption)
					{
						setting.Model.SelectedOption = setting.SelectedOption;
						modifiedModels.Add(setting.Model);
					}
				}
				else
				{
					if (setting.Model.Value != setting.Value)
					{
						setting.Model.Value = setting.Value;
						modifiedModels.Add(setting.Model);
					}
				}
			}
		}
		finally
		{
			BiosSettingModel.IsBatchMode = false;
			_isRestoringHistory = false;
		}

		if (modifiedModels.Count > 0)
		{
			BulkRefreshNodes(modifiedModels);
		}

		_currentState = CaptureState();
		UpdateDiffNodesBulk(modifiedModels);
		UpdateRecommendedTreeIncremental(modifiedModels);
		ExpandDiffNodesAction?.Invoke();
		RefreshFilterAction?.Invoke();
	}

	private void BulkRefreshNodes(HashSet<BiosSettingModel> modifiedModels)
	{
		if (modifiedModels.Count == 0) return;

		IsAnyModified = _allLeaves.Any(leaf => leaf.Model?.IsModified == true);

		var refreshedParents = new HashSet<BiosTreeNode>();
		
		foreach (BiosSettingModel model in modifiedModels)
		{
			if (_modelToLeafMap.TryGetValue(model, out BiosTreeNode? leaf))
			{
				leaf.RaiseDisplayCurrentChanged();
				leaf.RaiseHasPendingRecommendationChanged();
				leaf.RaiseIsModifiedChanged();

				BiosTreeNode parent = FindParentGroup(leaf);
				if (parent != null && refreshedParents.Add(parent))
				{
					parent.RaiseIsModifiedChanged();
					parent.RaiseDisplayCurrentChanged();
					parent.RaiseHasPendingRecommendationChanged();
				}
			}
		}

		RecommendationStateChanged?.Invoke(this, EventArgs.Empty);
	}

	private static bool StatesEqual(IReadOnlyList<SettingState> left, IReadOnlyList<SettingState> right) => left.Count == right.Count && left.Zip(right).All(pair => ReferenceEquals(pair.First.SelectedOption, pair.Second.SelectedOption) && pair.First.Value == pair.Second.Value);

	private sealed record SettingState(BiosSettingModel Model, Option SelectedOption, string Value);

	private static IEnumerable<BiosTreeNode> GetAllNodes(BiosTreeNode root)
	{
		yield return root;
		foreach (BiosTreeNode child in root.Children)
		{
			foreach (BiosTreeNode descendant in GetAllNodes(child))
			{
				yield return descendant;
			}
		}
	}

	public int RecommendedCount
	{
		get
		{
			BiosTreeNode recommendedRoot = _recommendedRoot;
			if (recommendedRoot == null) return 0;
			return CountLeaves(recommendedRoot);
		}
	}
}
