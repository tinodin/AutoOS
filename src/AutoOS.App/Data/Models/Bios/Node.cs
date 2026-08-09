using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using AutoOS.App.Data.Enums.Bios;

namespace AutoOS.App.Data.Models.Bios;

public sealed partial class Node(
	NodeKind nodeKind,
	string displayName,
	string? description = null,
	string? groupKey = null,
	Setting? setting = null,
	State? state = null,
	string? baseDisplayName = null) : INotifyDataErrorInfo, INotifyPropertyChanged
{
	private readonly Option _mixedOption = new() { Label = "Mixed", Index = "Mixed" };
	private List<GroupValueState>? _mixedValues;
	private string _displayName = displayName;
	private State? _state;

	public NodeKind NodeKind { get; } = nodeKind;

	public Node? Parent { get; set; }

	public IEnumerable<Node> Ancestors
	{
		get
		{
			Node? current = Parent;
			while (current != null)
			{
				yield return current;
				current = current.Parent;
			}
		}
	}

	public bool IsRoot => NodeKind == NodeKind.Root;

	public string GroupKey { get; } = groupKey ?? string.Empty;

	public string DisplayName
	{
		get => _displayName;
		set
		{
			if (_displayName != value)
			{
				_displayName = value;
				OnPropertyChanged();
			}
		}
	}

	public event PropertyChangedEventHandler? PropertyChanged;

	private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

	public string BaseDisplayName { get; } = baseDisplayName ?? displayName;

	public string Description { get; } = description ?? string.Empty;

	public Setting? Setting { get; } = setting;

	public State? State
	{
		get
		{
			if (_state != null)
				return _state;
			if (state == null)
				return null;

			_state = state;
			_state.PropertyChanged += OnStatePropertyChanged;
			return _state;
		}
	}

	private void OnStatePropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (NodeKind != NodeKind.Setting)
			return;

		OnPropertyChanged(nameof(DisplayCurrent));
		OnPropertyChanged(nameof(DisplayOriginal));
		OnPropertyChanged(nameof(HasPendingRecommendation));
		OnPropertyChanged(nameof(IsModified));
		OnPropertyChanged(nameof(SelectedOption));

		foreach (Node ancestor in Ancestors)
		{
			if (ancestor.NodeKind == NodeKind.GroupedSetting)
			{
				ancestor.OnPropertyChanged(nameof(DisplayCurrent));
				ancestor.OnPropertyChanged(nameof(DisplayOriginal));
				ancestor.OnPropertyChanged(nameof(HasPendingRecommendation));
				ancestor.OnPropertyChanged(nameof(IsModified));
			}
		}
	}

	public bool IsExpanded { get; set; } = true;

	public ObservableCollection<Node> Children { get; } = [];

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
				return State?.SelectedOption?.Label ?? State?.Value ?? string.Empty;

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
				return State?.OriginalSelectedOption?.Label ?? State?.OriginalValue ?? string.Empty;

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

				if (DisplayCurrent == "Mixed" || _mixedValues != null)
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

	public void BeginCellEdit()
	{
		if (HasOptions)
		{
			if (NodeKind == NodeKind.GroupedSetting && DisplayCurrent == "Mixed")
				RememberMixedValues();
			EditOption = SelectedOption;
		}
		else
		{
			EditValue = DisplayCurrent == "Mixed" ? string.Empty : DisplayCurrent;
		}
	}

	public bool CommitCellEdit()
	{
		if (NodeKind == NodeKind.Setting)
		{
			if (State == null || Setting == null)
				return false;

			if (Setting.HasOptions)
			{
				if (State.SelectedOption == EditOption)
					return false;
				State.SelectedOption = EditOption;
				return true;
			}

			if (string.Equals(State.Value, EditValue, StringComparison.Ordinal))
				return false;
			State.Value = EditValue;
			return true;
		}

		if (NodeKind == NodeKind.GroupedSetting)
		{
			if (!CanEditCurrent)
				return false;

			if (EditOption == _mixedOption)
			{
				if (_mixedValues == null)
					return false;

				bool changed = false;
				foreach (GroupValueState saved in _mixedValues)
				{
					if (saved.Leaf.State == null)
						continue;

					Option? previousOption = saved.Leaf.State.SelectedOption;
					string? previousValue = saved.Leaf.State.Value;
					if (saved.Leaf.Setting?.HasOptions == true)
						saved.Leaf.State.SelectedOption = saved.SelectedOption;
					else
						saved.Leaf.State.Value = saved.Value;

					if (previousOption != saved.Leaf.State.SelectedOption || previousValue != saved.Leaf.State.Value)
						changed = true;
				}

				_mixedValues = null;
				return changed;
			}

			bool groupChanged = false;
			foreach (Node leaf in GetLeaves())
			{
				if (leaf.State == null || leaf.Setting == null)
					continue;

				Option? previousOption = leaf.State.SelectedOption;
				string? previousValue = leaf.State.Value;
				if (leaf.Setting.HasOptions && EditOption != null)
					leaf.State.SelectedOption = EditOption;
				else if (!leaf.Setting.HasOptions)
					leaf.State.Value = EditValue;

				if (previousOption != leaf.State.SelectedOption || previousValue != leaf.State.Value)
					groupChanged = true;
			}

			return groupChanged;
		}

		return false;
	}

	private void RememberMixedValues() =>
		_mixedValues = [.. GetLeaves()
			.Where(leaf => leaf.State != null)
			.Select(leaf => new GroupValueState(leaf, leaf.State!.SelectedOption, leaf.State.Value))];

	private sealed record GroupValueState(Node Leaf, Option? SelectedOption, string? Value);

	private static bool LabelsEqual(string left, string right) => string.Equals(NormalizeLabel(left), NormalizeLabel(right), StringComparison.OrdinalIgnoreCase);

	private static string NormalizeLabel(string? label) => label?.Trim() ?? string.Empty;

	public bool HasErrors => Errors.Length > 0;

	public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

	public IEnumerable GetErrors(string? propertyName)
	{
		if (propertyName != null && propertyName != nameof(DisplayCurrent))
			return Array.Empty<string>();

		return Errors;
	}

	public void RaiseErrorsChanged() =>
		ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(DisplayCurrent)));

	private string[] Errors
	{
		get
		{
			if (NodeKind == NodeKind.Setting)
				return State == null || Setting == null ? [] : Validation.GetErrors(State, Setting.HasOptions);

			if (NodeKind == NodeKind.GroupedSetting)
			{
				var leaves = GetLeaves().ToList();
				if (leaves.Count == 0 || !leaves.All(leaf => leaf.HasErrors))
					return [];

				return [.. leaves.SelectMany(leaf => leaf.Errors).Distinct()];
			}

			return [];
		}
	}
}
