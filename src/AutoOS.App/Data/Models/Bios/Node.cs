using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using AutoOS.App.Data.Enums.Bios;
using AutoOS.Core.Data.Models.Bios;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AutoOS.App.Data.Models.Bios;

public sealed partial class Node : ObservableObject, INotifyDataErrorInfo, IOrderedNode
{
	internal Node(
		NodeKind nodeKind,
		string displayName,
		string? description = null,
		Setting? setting = null,
		SettingState? state = null,
		string? baseDisplayName = null)
	{
		NodeKind = nodeKind;
		DisplayName = displayName;
		BaseDisplayName = baseDisplayName ?? displayName;
		Description = description ?? string.Empty;
		Setting = setting;
		State = state;
	}

	public NodeKind NodeKind { get; }

	public Setting? Setting { get; }

	public string Description { get; }

	public Node? Parent { get; set; }

	public bool IsExpanded { get; set; } = true;

	public ObservableCollection<Node> Children { get; } = [];

	internal int Order { get; set; }

	int IOrderedNode.Order => Order;

	[ObservableProperty]
	public partial string DisplayName { get; set; }

	public string BaseDisplayName { get; }

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(DisplayCurrent))]
	[NotifyPropertyChangedFor(nameof(DisplayOriginal))]
	[NotifyPropertyChangedFor(nameof(SelectedOption))]
	[NotifyPropertyChangedFor(nameof(IsModified))]
	[NotifyPropertyChangedFor(nameof(HasPendingRecommendation))]
	[NotifyPropertyChangedFor(nameof(IsDefault))]
	[NotifyPropertyChangedFor(nameof(HasErrors))]
	public partial SettingState? State { get; set; }

	public string ToolTipText => NodeKind == NodeKind.Setting ? Setting?.Description ?? string.Empty : string.Empty;

	public string DisplayCurrent => State is { } state && Setting is { } setting ? SettingState.GetDisplayValue(setting, state.Value) : string.Empty;

	public IReadOnlyList<Option>? Options => Setting?.Options;

	public string DisplayDefault => Setting?.Default ?? string.Empty;

	public string DisplayRecommended => Setting?.RecommendedOption?.Label ?? Setting?.RecommendedValue ?? string.Empty;

	public string DisplayOriginal => State is { } state && Setting is { } setting ? SettingState.GetDisplayValue(setting, state.OriginalValue) : string.Empty;

	public bool HasPendingRecommendation => Setting is { } setting && State is { } state && SettingState.HasPendingRecommendation(setting, state);

	public bool IsModified => State?.IsModified == true;

	public bool IsDefault => Setting is { } setting && State is { } state && SettingState.MatchesDefault(setting, state);

	public Option? SelectedOption => State is { } state && Setting is { } setting ? SettingState.ResolveOption(setting, state.Value) : null;

	public string EditValue { get; set; } = string.Empty;

	public Option? EditOption { get; set; }

	public void BeginCellEdit()
	{
		if (Setting is { Options.Count: > 0 })
		{
			EditOption = SelectedOption;
		}
		else
		{
			EditValue = DisplayCurrent;
		}
	}

	public bool CommitCellEdit()
	{
		if (NodeKind != NodeKind.Setting || State == null || Setting == null)
			return false;

		if (Setting.Options.Count > 0)
		{
			if (EditOption == null)
				return false;

			string newValue = EditOption.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
			if (string.Equals(State.Value, newValue, StringComparison.Ordinal))
				return false;

			State.Value = newValue;
			return true;
		}

		if (string.Equals(State.Value, EditValue, StringComparison.Ordinal))
			return false;
		State.Value = EditValue;
		return true;
	}

	public bool HasErrors => NodeKind == NodeKind.Setting && State != null && Setting != null && Validation.GetErrors(State, Setting).Length > 0;

	public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

	public IEnumerable GetErrors(string? propertyName)
	{
		if (propertyName is not null and not nameof(DisplayCurrent))
			return Array.Empty<string>();

		if (NodeKind != NodeKind.Setting || State == null || Setting == null)
			return Array.Empty<string>();

		return Validation.GetErrors(State, Setting);
	}

	partial void OnStateChanged(SettingState? value)
	{
		if (value != null)
			value.PropertyChanged += OnStatePropertyChanged;
	}

	private void OnStatePropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName is not (nameof(SettingState.Value)
			or nameof(SettingState.OriginalValue)))
		{
			return;
		}

		OnPropertyChanged(nameof(DisplayCurrent));
		OnPropertyChanged(nameof(DisplayOriginal));
		OnPropertyChanged(nameof(SelectedOption));
		OnPropertyChanged(nameof(IsModified));
		OnPropertyChanged(nameof(HasPendingRecommendation));
		OnPropertyChanged(nameof(IsDefault));
		OnPropertyChanged(nameof(HasErrors));
		ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(DisplayCurrent)));
	}
}
