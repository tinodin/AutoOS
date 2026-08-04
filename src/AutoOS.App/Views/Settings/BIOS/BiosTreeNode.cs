using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using AutoOS.Core.Helpers.BIOS;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AutoOS.App.Views.Settings.BIOS;

public enum NodeKind { Root, Group, Leaf }

public partial class BiosTreeNode : ObservableObject, INotifyDataErrorInfo
{
	public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

	public NodeKind NodeKind { get; init; }

	public bool IsRoot => NodeKind == NodeKind.Root;

	public int SortOrder { get; set; }

	private string _displayName = string.Empty;
	public string DisplayName
	{
		get => _displayName;
		set => SetProperty(ref _displayName, value);
	}

	public string DiffGroupKey { get; set; }

	private bool _isExpanded = true;
	public bool IsExpanded
	{
		get => _isExpanded;
		set => SetProperty(ref _isExpanded, value);
	}

	public string ToolTipText
	{
		get
		{
			if (NodeKind == NodeKind.Root && DisplayName.StartsWith("Recommended"))
				return null;

			if (NodeKind == NodeKind.Leaf)
				return Model?.HelpString;

			var leaves = GetLeaves().ToList();
			var distinct = leaves
				.Select(leaf => leaf.Model?.HelpString)
				.Where(str => !string.IsNullOrWhiteSpace(str))
				.Distinct()
				.ToList();

			return distinct.Count == 1 ? distinct[0] : null;
		}
	}

	public bool HasPendingRecommendation
	{
		get
		{
			if (Model == null)
			{
				if (NodeKind == NodeKind.Group)
				{
					return GetLeaves().Any(leaf => leaf.HasPendingRecommendation);
				}
				return false;
			}

			if (Model.RecommendedOption != null)
				return !ReferenceEquals(Model.SelectedOption, Model.RecommendedOption);

			return !string.IsNullOrEmpty(Model.RecommendedValue) &&
				!string.Equals(Model.Value, Model.RecommendedValue, StringComparison.Ordinal);
		}
	}

	private BiosSettingModel _model;
	public BiosSettingModel Model
	{
		get => _model;
		init
		{
			_model = value;
			SubscribeToModelErrors();
		}
	}

	public ObservableCollection<BiosTreeNode> Children { get; } = [];
	private List<GroupValueState> _mixedValues;
	private readonly Option _mixedOption = new() { Label = "Mixed", Index = "Mixed" };

	public string DisplayDefault
	{
		get
		{
			if (NodeKind == NodeKind.Leaf)
				return Model?.BiosDefault ?? string.Empty;

			if (NodeKind == NodeKind.Root || Children.Count == 0)
				return string.Empty;

			IEnumerable<BiosTreeNode> leaves = GetLeaves();
			var distinct = leaves.Select(leaf => leaf.Model?.BiosDefault ?? string.Empty).Distinct().ToList();
			if (distinct.Count == 1) return distinct[0];
			return "Mixed";
		}
	}

	public string DisplayCurrent
	{
		get
		{
			if (NodeKind == NodeKind.Leaf)
				return Model?.SelectedOption?.Label ?? Model?.Value ?? string.Empty;

			if (NodeKind == NodeKind.Root || Children.Count == 0)
				return string.Empty;

			IEnumerable<BiosTreeNode> leaves = GetLeaves();
			var distinct = leaves.Select(leaf => leaf.Model?.SelectedOption?.Label ?? leaf.Model?.Value ?? string.Empty).Distinct().ToList();
			if (distinct.Count == 1) return distinct[0];
			return "Mixed";
		}
		set
		{
			if (NodeKind == NodeKind.Leaf)
			{
				if (Model == null) return;
				if (Model.HasOptions)
				{
					Option? match = Model.Options.FirstOrDefault(option =>
						LabelsEqual(option.Label, value));
					if (match != null)
						Model.SelectedOption = match;
				}
				else
				{
					Model.Value = value;
				}
				OnPropertyChanged();
				OnPropertyChanged(nameof(IsModified));
			}
			else if (NodeKind == NodeKind.Group)
			{
				if (!CanEditCurrent) return;

				if (value == "Mixed" && _mixedValues != null)
				{
					RestoreMixedValues();
					NotifyGroupValueChanged();
					return;
				}

				if (DisplayCurrent == "Mixed" && _mixedValues == null)
					RememberMixedValues();

			foreach (BiosTreeNode? child in GetLeaves().ToList())
			{
				if (child.Model == null) continue;
				if (GroupUsesOptions)
				{
						Option? match = child.Model.Options.FirstOrDefault(option =>
						LabelsEqual(option.Label, value));
					if (match != null)
						child.Model.SelectedOption = match;
				}
				else
				{
					child.Model.Value = value;
				}
			}
			NotifyGroupValueChanged();
			}
		}
	}

	public string DisplayRecommended
	{
		get
		{
			if (NodeKind == NodeKind.Leaf)
				return Model?.RecommendedOption?.Label ?? Model?.RecommendedValue ?? string.Empty;

			if (NodeKind == NodeKind.Root || Children.Count == 0)
				return string.Empty;

			IEnumerable<BiosTreeNode> leaves = GetLeaves();
			bool hasOptions = leaves.Any(leaf => leaf.Model?.HasOptions == true);
			var distinct = leaves
				.Select(leaf => leaf.Model?.RecommendedOption?.Label ?? leaf.Model?.RecommendedValue ?? string.Empty)
				.Distinct().ToList();
			if (distinct.Count == 1) return distinct[0];
			return hasOptions ? "Mixed" : string.Empty;
		}
	}

	public string DisplayOriginal
	{
		get
		{
			if (NodeKind == NodeKind.Leaf)
				return Model?.OriginalSelectedOption?.Label ?? Model?.OriginalValue ?? string.Empty;

			if (NodeKind == NodeKind.Root || Children.Count == 0)
				return string.Empty;

			var distinct = GetLeaves()
				.Select(leaf => leaf.Model?.OriginalSelectedOption?.Label ?? leaf.Model?.OriginalValue ?? string.Empty)
				.Distinct()
				.ToList();
			return distinct.Count == 1 ? distinct[0] : "Mixed";
		}
	}

	private string _editValue = string.Empty;
	private Option _editOption;

	public string EditValue
	{
		get => _editValue;
		set => SetProperty(ref _editValue, value);
	}
	
	public Option EditOption
	{
		get => _editOption;
		set => SetProperty(ref _editOption, value);
	}

	public void BeginCellEdit()
	{
		if (HasOptions)
			EditOption = SelectedOption;
		else
			EditValue = DisplayCurrent == "Mixed" ? string.Empty : DisplayCurrent;
	}

	public void CommitCellEdit()
	{
		if (HasOptions)
		{
			if (EditOption != null)
				SelectedOption = EditOption;
		}
		else
		{
			DisplayCurrent = EditValue;
		}
	}

	public List<Option> Options
	{
		get
		{
			if (NodeKind == NodeKind.Leaf)
				return Model?.Options;

			if (NodeKind == NodeKind.Group && GroupUsesOptions)
			{
				var leaves = GetLeaves().ToList();

				var allOptions = leaves
					.SelectMany(leaf => leaf.Model.Options)
					.GroupBy(option => NormalizeLabel(option.Label), StringComparer.OrdinalIgnoreCase)
					.Where(group => leaves.All(leaf => leaf.Model.Options.Any(option =>
						LabelsEqual(option.Label, group.Key))))
					.Select(group => group.First())
					.ToList();

				if (DisplayCurrent == "Mixed" || _mixedValues != null)
					allOptions.Insert(0, _mixedOption);

				return allOptions;
			}

			return Model?.Options;
		}
	}

	public Option SelectedOption
	{
		get
		{
			if (NodeKind == NodeKind.Leaf)
				return Model?.SelectedOption;

			if (DisplayCurrent == "Mixed")
				return _mixedOption;

			return Options?.FirstOrDefault(option => LabelsEqual(option.Label, DisplayCurrent));
		}
		set => DisplayCurrent = value?.Label;
	}
	
	public bool HasOptions => Model?.HasOptions == true || (NodeKind == NodeKind.Group && GroupUsesOptions);

	public bool CanEditCurrent => NodeKind == NodeKind.Leaf || (NodeKind == NodeKind.Group && GetLeaves().Any() && GetLeaves().All(leaf => leaf.Model?.HasOptions == GroupUsesOptions));

	private bool GroupUsesOptions => NodeKind == NodeKind.Group && GetLeaves().Any() && GetLeaves().All(leaf => leaf.Model?.HasOptions == true);

	public bool IsModified
	{
		get
		{
			if (NodeKind == NodeKind.Leaf) return Model?.IsModified == true;
			return GetLeaves().Any(leaf => leaf.Model?.IsModified == true);
		}
	}

	internal IEnumerable<BiosTreeNode> GetLeaves()
	{
		if (NodeKind == NodeKind.Leaf)
		{
			yield return this;
			yield break;
		}
		foreach (BiosTreeNode child in Children)
			foreach (BiosTreeNode leaf in child.GetLeaves())
				yield return leaf;
	}

	private void RememberMixedValues() =>
		_mixedValues = [.. GetLeaves()
			.Where(child => child.Model != null)
			.Select(child => new GroupValueState(child.Model, child.Model.SelectedOption, child.Model.Value))];

	private void RestoreMixedValues()
	{
		foreach (GroupValueState saved in _mixedValues)
		{
			if (saved.Model.HasOptions)
				saved.Model.SelectedOption = saved.SelectedOption;
			else
				saved.Model.Value = saved.Value;
		}

		_mixedValues = null;
	}

	private void NotifyGroupValueChanged()
	{
		foreach (BiosTreeNode? child in GetLeaves().ToList())
		{
			child.OnPropertyChanged(nameof(DisplayCurrent));
			child.OnPropertyChanged(nameof(IsModified));
		}

		OnPropertyChanged(nameof(DisplayCurrent));
		OnPropertyChanged(nameof(IsModified));
		OnPropertyChanged(nameof(Options));
	}

	private sealed record GroupValueState(BiosSettingModel Model, Option SelectedOption, string Value);

	private static bool LabelsEqual(string left, string right) => string.Equals(NormalizeLabel(left), NormalizeLabel(right), StringComparison.OrdinalIgnoreCase);

	private static string NormalizeLabel(string label) => label?.Trim() ?? string.Empty;

	public void RaiseIsModifiedChanged() => OnPropertyChanged(nameof(IsModified));

	public void SubscribeToChildrenErrors()
	{
		if (NodeKind == NodeKind.Group)
		{
			foreach (BiosTreeNode child in Children)
			{
				child.PropertyChanged += (s, e) =>
				{
					if (e.PropertyName == nameof(HasErrors))
					{
						OnPropertyChanged(nameof(HasErrors));
						RaiseErrorsChanged(nameof(DisplayCurrent));
					}
				};
			}

			if (GetLeaves().All(leaf => leaf.Model?.HasErrors == true))
			{
				OnPropertyChanged(nameof(HasErrors));
				RaiseErrorsChanged(nameof(DisplayCurrent));
			}
		}
	}

	public void RaiseDisplayCurrentChanged() =>
		OnPropertyChanged(nameof(DisplayCurrent));

	public void RaiseHasPendingRecommendationChanged() =>
		OnPropertyChanged(nameof(HasPendingRecommendation));

	public void RaiseHasErrorsChanged() =>
		OnPropertyChanged(nameof(HasErrors));

	public void RaiseErrorsChanged(string propertyName) =>
		ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));

	public IEnumerable GetErrors(string propertyName)
	{
		if (NodeKind == NodeKind.Leaf)
		{
			if (_model == null)
				return null;
			
			if (propertyName == nameof(DisplayCurrent))
			{
				if (_model.HasOptions)
					return _model.GetErrors(nameof(BiosSettingModel.SelectedOption));
				else
					return _model.GetErrors(nameof(BiosSettingModel.Value));
			}

			return null;
		}

		if (NodeKind == NodeKind.Group && propertyName == nameof(DisplayCurrent))
		{
			if (!GetLeaves().All(leaf => leaf.Model?.HasErrors == true))
				return null;

			var allErrors = new List<string>();
			foreach (BiosTreeNode leaf in GetLeaves())
			{
				if (leaf.Model.HasOptions)
				{
					IEnumerable errors = leaf.Model.GetErrors(nameof(BiosSettingModel.SelectedOption));
					if (errors != null)
						allErrors.AddRange(errors.Cast<string>());
				}
				else
				{
					IEnumerable errors = leaf.Model.GetErrors(nameof(BiosSettingModel.Value));
					if (errors != null)
						allErrors.AddRange(errors.Cast<string>());
				}
			}

			return allErrors.Count > 0 ? allErrors : null;
		}

		return null;
	}

	public bool HasErrors
	{
		get
		{
			if (NodeKind == NodeKind.Leaf)
				return _model?.HasErrors == true;

			if (NodeKind == NodeKind.Group)
				return GetLeaves().All(leaf => leaf.Model?.HasErrors == true);

			return false;
		}
	}

	private void SubscribeToModelErrors()
	{
		if (_model != null)
		{
			_model.PropertyChanged += (s, e) =>
			{
				if (e.PropertyName == nameof(BiosSettingModel.Value) || e.PropertyName == nameof(BiosSettingModel.SelectedOption))
				{
					RaiseErrorsChanged(nameof(DisplayCurrent));
					OnPropertyChanged(nameof(HasErrors));
				}
			};

			_model.ErrorsChanged += (s, e) =>
			{
				OnPropertyChanged(nameof(HasErrors));
				RaiseErrorsChanged(nameof(DisplayCurrent));
			};
		}
	}
}
