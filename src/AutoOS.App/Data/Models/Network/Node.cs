using System.Diagnostics.CodeAnalysis;
using AutoOS.App.Data.Enums.Network;
using AutoOS.Core.Data.Enums.Network;
using AutoOS.Core.Data.Models.Device;
using AutoOS.Core.Data.Models.Network;

namespace AutoOS.App.Data.Models.Network;

public sealed partial class Node : ObservableObject, INotifyDataErrorInfo, IOrderedNode
{
	[DynamicDependency(nameof(IsExpanded), typeof(Node))]
	[DynamicDependency(nameof(Children), typeof(Node))]
	[DynamicDependency(nameof(DisplayName), typeof(Node))]
	[DynamicDependency(nameof(DisplayCurrent), typeof(Node))]
	[DynamicDependency(nameof(DisplayRecommended), typeof(Node))]
	[DynamicDependency(nameof(HasPendingRecommendation), typeof(Node))]
	[DynamicDependency(nameof(DisplayOriginal), typeof(Node))]
	[DynamicDependency(nameof(DisplayDefault), typeof(Node))]
	[DynamicDependency(nameof(AdapterGlyph), typeof(Node))]
	[DynamicDependency(nameof(DriverVersion), typeof(Node))]
	[DynamicDependency(nameof(HasErrors), typeof(Node))]
	internal Node(NodeKind nodeKind, DeviceInfo adapter, SettingState? state = null)
	{
		NodeKind = nodeKind;
		Adapter = adapter;
		State = state;
		BaseDisplayName = state?.Setting.Name ?? adapter.FriendlyName;
		DisplayName = BaseDisplayName;
		if (state != null)
			state.PropertyChanged += OnStatePropertyChanged;
	}

	public NodeKind NodeKind { get; }

	public DeviceInfo Adapter { get; }

	public SettingState? State { get; }

	public Setting? Setting => State?.Setting;

	public string BaseDisplayName { get; }

	public string Description => Setting?.Key ?? $"{Adapter.DriverType} {Adapter.CurrentVersion}";

	public string AdapterGlyph => Adapter.IsWiFi ? "\uE701" : "\uE839";

	public string DriverVersion => $"{Adapter.DriverType} {Adapter.CurrentVersion}";

	public bool IsExpanded { get; set; } = true;

	public ObservableCollection<Node> Children { get; } = [];

	public int Order { get; init; }

	[ObservableProperty]
	public partial string DisplayName { get; set; }

	public bool IsAdjustable => Setting != null;

	public bool IsModified => State?.IsModified == true;

	public bool HasErrors => NodeKind == NodeKind.Setting && State?.HasErrors == true;

	public bool HasPendingRecommendation => State?.HasPendingRecommendation == true;

	public bool IsDefault => State?.IsDefault == true;

	public string DisplayCurrent => State?.DisplayCurrent ?? string.Empty;

	public string DisplayRecommended => State?.DisplayRecommended ?? string.Empty;

	public string DisplayOriginal => State?.DisplayOriginal ?? string.Empty;

	public string DisplayDefault => State == null || Setting == null ? string.Empty : State.GetDisplayValue(Setting.DefaultValue);

	public IReadOnlyList<Option>? Options => Setting?.Options;

	public string ValueToolTip => GetRangeToolTipOr(DisplayCurrent);

	public string OriginalValueToolTip => GetRangeToolTipOr(DisplayOriginal);

	private string GetRangeToolTipOr(string displayValue) =>
		Setting is { } setting && setting.Options.Count == 0 && (setting.Min.HasValue || setting.Max.HasValue) ? SettingState.GetRangeToolTip(setting) : displayValue;

	public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

	public IEnumerable GetErrors(string? propertyName)
	{
		if (propertyName is not null and not nameof(DisplayCurrent))
			return Array.Empty<string>();
		if (State == null || Setting == null)
			return Array.Empty<string>();

		return Validation.GetErrors(State, Setting);
	}

	public void Detach()
	{
		if (State != null)
			State.PropertyChanged -= OnStatePropertyChanged;
	}

	private void OnStatePropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		OnPropertyChanged(e.PropertyName ?? string.Empty);
		if (e.PropertyName == nameof(SettingState.Value))
		{
			OnPropertyChanged(nameof(HasErrors));
			ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(DisplayCurrent)));
		}
	}
}
