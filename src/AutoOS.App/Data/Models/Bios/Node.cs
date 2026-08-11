using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using AutoOS.App.Data.Enums.Bios;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AutoOS.App.Data.Models.Bios;

public sealed partial class Node : ObservableObject
{
	private readonly Option _mixedOption = new() { Label = "Mixed", Index = "Mixed" };

	public Node(
		NodeKind nodeKind,
		string nodeDisplayName,
		string? description = null,
		string? groupKey = null,
		Setting? setting = null,
		State? state = null,
		string? baseDisplayName = null)
	{
		NodeKind = nodeKind;
		DisplayName = nodeDisplayName;
		BaseDisplayName = baseDisplayName ?? nodeDisplayName;
		Description = description ?? string.Empty;
		GroupKey = groupKey ?? string.Empty;
		Setting = setting;
		State = state;

		Children.CollectionChanged += OnChildrenChanged;
		SubscribeTo(State);
	}

	[ObservableProperty]
	public partial string DisplayName { get; set; }

	public NodeKind NodeKind { get; }

	public Node? Parent { get; set; }

	public bool IsRoot => NodeKind == NodeKind.Root;

	public string GroupKey { get; }

	public string BaseDisplayName { get; }

	public string Description { get; }

	public Setting? Setting { get; }

	public State? State { get; }

	public bool IsExpanded { get; set; } = true;

	public ObservableCollection<Node> Children { get; } = [];

	public Option MixedOption => _mixedOption;

	public string ToolTipText
	{
		get
		{
			if (NodeKind == NodeKind.Root)
				return string.Empty;

			if (NodeKind == NodeKind.Setting)
				return Setting?.HelpString ?? string.Empty;

			var distinct = GetLeaves()
				.Select(leaf => leaf.Setting?.HelpString)
				.Where(str => !string.IsNullOrWhiteSpace(str))
				.Distinct()
				.ToList();

			return distinct.Count == 1 ? distinct[0]! : string.Empty;
		}
	}

	public string DisplayDefault
	{
		get
		{
			if (NodeKind == NodeKind.Setting)
				return Setting?.BiosDefault ?? string.Empty;

			if (NodeKind == NodeKind.Root || Children.Count == 0)
				return string.Empty;

			var distinct = GetLeaves().Select(leaf => leaf.Setting?.BiosDefault ?? string.Empty).Distinct().ToList();
			return distinct.Count == 1 ? distinct[0] : "Mixed";
		}
	}

	public string DisplayCurrent
	{
		get
		{
			if (NodeKind == NodeKind.Setting)
				return State?.DisplayCurrent ?? string.Empty;

			if (NodeKind == NodeKind.Root || Children.Count == 0)
				return string.Empty;

			var distinct = GetLeaves().Select(leaf => leaf.DisplayCurrent).Distinct().ToList();
			return distinct.Count == 1 ? distinct[0] : "Mixed";
		}
	}

	public string DisplayRecommended
	{
		get
		{
			if (NodeKind == NodeKind.Setting)
				return Setting?.RecommendedOption?.Label ?? Setting?.RecommendedValue ?? string.Empty;

			if (NodeKind == NodeKind.Root || Children.Count == 0)
				return string.Empty;

			bool hasOptions = GetLeaves().Any(leaf => leaf.Setting?.HasOptions == true);
			var distinct = GetLeaves()
				.Select(leaf => leaf.Setting?.RecommendedOption?.Label ?? leaf.Setting?.RecommendedValue ?? string.Empty)
				.Distinct()
				.ToList();
			if (distinct.Count == 1)
				return distinct[0];
			return hasOptions ? "Mixed" : string.Empty;
		}
	}

	public string DisplayOriginal
	{
		get
		{
			if (NodeKind == NodeKind.Setting)
				return State?.DisplayOriginal ?? string.Empty;

			if (NodeKind == NodeKind.Root || Children.Count == 0)
				return string.Empty;

			var distinct = GetLeaves().Select(leaf => leaf.DisplayOriginal).Distinct().ToList();
			return distinct.Count == 1 ? distinct[0] : "Mixed";
		}
	}

	public bool HasPendingRecommendation
	{
		get
		{
			if (NodeKind == NodeKind.Setting)
			{
				Setting? setting = Setting;
				State? state = State;
				if (setting == null || state == null)
					return false;

				if (setting.RecommendedOption != null)
					return state.SelectedOption != setting.RecommendedOption;

				return !string.IsNullOrEmpty(setting.RecommendedValue) &&
					!string.Equals(state.Value, setting.RecommendedValue, StringComparison.Ordinal);
			}

			if (NodeKind == NodeKind.GroupedSetting)
				return GetLeaves().Any(leaf => leaf.HasPendingRecommendation);

			return false;
		}
	}

	public bool IsModified
	{
		get
		{
			if (NodeKind == NodeKind.Setting)
				return State?.IsModified == true;

			if (NodeKind == NodeKind.GroupedSetting)
				return GetLeaves().Any(leaf => leaf.IsModified);

			return false;
		}
	}

	public bool HasErrors
	{
		get
		{
			if (NodeKind == NodeKind.Setting)
				return State?.HasErrors == true;

			if (NodeKind == NodeKind.GroupedSetting)
			{
				var leaves = GetLeaves().ToList();
				return leaves.Count > 0 && leaves.All(leaf => leaf.HasErrors);
			}

			return false;
		}
	}

	public bool HasOptions => NodeKind == NodeKind.Setting
		? Setting?.HasOptions == true
		: NodeKind == NodeKind.GroupedSetting && GroupUsesOptions;

	private bool GroupUsesOptions => NodeKind == NodeKind.GroupedSetting && GetLeaves().Any() && GetLeaves().All(leaf => leaf.Setting?.HasOptions == true);

	public bool CanEditCurrent => NodeKind == NodeKind.Setting || (NodeKind == NodeKind.GroupedSetting && GetLeaves().Any() && GetLeaves().All(leaf => leaf.HasOptions == GroupUsesOptions));

	public List<Option>? Options
	{
		get
		{
			if (NodeKind == NodeKind.Setting)
				return Setting?.Options;

			if (NodeKind == NodeKind.GroupedSetting && GroupUsesOptions)
			{
				var leaves = GetLeaves().ToList();

				var allOptions = leaves
					.SelectMany(leaf => leaf.Setting?.Options ?? [])
					.GroupBy(option => NormalizeLabel(option.Label), StringComparer.OrdinalIgnoreCase)
					.Where(group => leaves.All(leaf => (leaf.Setting?.Options ?? []).Any(option => LabelsEqual(option.Label, group.Key))))
					.Select(group => group.First())
					.ToList();

				if (DisplayCurrent == "Mixed")
					allOptions.Insert(0, _mixedOption);

				return allOptions;
			}

			return Setting?.Options;
		}
	}

	public Option? SelectedOption
	{
		get
		{
			if (NodeKind == NodeKind.Setting)
				return State?.SelectedOption;

			if (DisplayCurrent == "Mixed")
				return _mixedOption;

			return Options?.FirstOrDefault(option => LabelsEqual(option.Label, DisplayCurrent));
		}
	}

	public string EditValue { get; set; } = string.Empty;

	public Option? EditOption { get; set; }

	public IEnumerable<Node> GetLeaves()
	{
		if (NodeKind == NodeKind.Setting)
		{
			yield return this;
			yield break;
		}

		foreach (Node child in Children)
			foreach (Node leaf in child.GetLeaves())
				yield return leaf;
	}

	private void OnChildrenChanged(object? sender, NotifyCollectionChangedEventArgs e)
	{
		if (e.NewItems != null)
			foreach (Node child in e.NewItems.Cast<Node>())
				SubscribeTo(child.State);

		if (e.OldItems != null)
			foreach (Node child in e.OldItems.Cast<Node>())
				UnsubscribeFrom(child.State);
	}

	private void SubscribeTo(State? state)
	{
		if (state != null)
			state.PropertyChanged += OnStatePropertyChanged;
	}

	private void UnsubscribeFrom(State? state)
	{
		if (state != null)
			state.PropertyChanged -= OnStatePropertyChanged;
	}

	private void OnStatePropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName is nameof(State.Value) or nameof(State.SelectedOption) or nameof(State.OriginalValue) or nameof(State.OriginalSelectedOption))
			RaiseDisplayChanges();
	}

	private void RaiseDisplayChanges()
	{
		OnPropertyChanged(nameof(DisplayCurrent));
		OnPropertyChanged(nameof(DisplayOriginal));
		OnPropertyChanged(nameof(IsModified));
		OnPropertyChanged(nameof(HasErrors));
		OnPropertyChanged(nameof(HasPendingRecommendation));
	}

	private static bool LabelsEqual(string left, string right) => string.Equals(NormalizeLabel(left), NormalizeLabel(right), StringComparison.OrdinalIgnoreCase);

	private static string NormalizeLabel(string? label) => label?.Trim() ?? string.Empty;
}
