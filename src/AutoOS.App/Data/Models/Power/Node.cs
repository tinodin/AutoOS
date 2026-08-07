using System.Collections.ObjectModel;
using System.Globalization;
using AutoOS.App.Data.Enums.Power;

namespace AutoOS.App.Data.Models.Power;

public sealed class Node
{
	private const uint DEFAULT_VALUE = 0;

	private readonly string _baseDisplayName;
	private readonly SettingState? _state;
	private readonly Value? _compareValues;

	internal Node(
		NodeKind nodeKind,
		PageMode mode,
		string displayName,
		string description,
		Guid guid,
		Setting? setting = null,
		SettingState? state = null,
		Value? compareValues = null,
		bool isAcDifferent = false,
		bool isDcDifferent = false,
		string? baseDisplayName = null)
	{
		NodeKind = nodeKind;
		Mode = mode;
		DisplayName = displayName;
		_baseDisplayName = baseDisplayName ?? displayName;
		Description = description;
		Guid = guid;
		Setting = setting;
		_state = state;
		_compareValues = compareValues;
		IsAcDifferent = isAcDifferent;
		IsDcDifferent = isDcDifferent;
	}

	public NodeKind NodeKind { get; }

	public PageMode Mode { get; }

	public Setting? Setting { get; }

	public Guid Guid { get; }

	public string Description { get; }

	public bool IsAcDifferent { get; }

	public bool IsDcDifferent { get; }

	public bool IsExpanded { get; set; } = true;

	public ObservableCollection<Node> Children { get; } = [];

	public string DisplayName { get; }

	public string BaseDisplayName => _baseDisplayName;

	public SettingState? State => _state;

	public bool HasValues => NodeKind == NodeKind.Setting;

	public uint AcValue => _state?.AcValue ?? DEFAULT_VALUE;

	public uint DcValue => _state?.DcValue ?? DEFAULT_VALUE;

	public uint OriginalAcValue => _state?.OriginalAcValue ?? DEFAULT_VALUE;

	public uint OriginalDcValue => _state?.OriginalDcValue ?? DEFAULT_VALUE;

	public string DisplayAc => HasValues && Setting != null ? GetDisplayValue(Setting, AcValue) : string.Empty;

	public string DisplayDc => HasValues && Setting != null ? GetDisplayValue(Setting, DcValue) : string.Empty;

	public string AcToolTip => HasValues && Setting != null ? GetValueToolTip(Setting, AcValue) : string.Empty;

	public string DcToolTip => HasValues && Setting != null ? GetValueToolTip(Setting, DcValue) : string.Empty;

	public string DisplayCompareAc => HasValues && Setting != null && _compareValues is { } compare ? GetDisplayValue(Setting, compare.AcValue) : string.Empty;

	public string DisplayCompareDc => HasValues && Setting != null && _compareValues is { } compare ? GetDisplayValue(Setting, compare.DcValue) : string.Empty;

	public string CompareAcToolTip => HasValues && Setting != null && _compareValues is { } compare ? GetValueToolTip(Setting, compare.AcValue) : string.Empty;

	public string CompareDcToolTip => HasValues && Setting != null && _compareValues is { } compare ? GetValueToolTip(Setting, compare.DcValue) : string.Empty;

	public string DisplayOriginalAc => HasValues && Setting != null ? GetDisplayValue(Setting, OriginalAcValue) : string.Empty;

	public string DisplayOriginalDc => HasValues && Setting != null ? GetDisplayValue(Setting, OriginalDcValue) : string.Empty;

	public string OriginalAcToolTip => HasValues && Setting != null ? GetValueToolTip(Setting, OriginalAcValue) : string.Empty;

	public string OriginalDcToolTip => HasValues && Setting != null ? GetValueToolTip(Setting, OriginalDcValue) : string.Empty;

	public IReadOnlyList<Option>? Options => Setting?.Options;

	public bool HasOptions => Setting is { Options.Count: > 0 };

	public bool IsAdjustable => HasOptions || (Setting != null && Setting.Minimum.HasValue && Setting.Maximum.HasValue && Setting.Increment.HasValue);

	public string EditAcToolTip => HasOptions ? State?.EditAcOption?.Description ?? string.Empty : Setting != null ? GetValueToolTip(Setting, AcValue) : string.Empty;

	public string EditDcToolTip => HasOptions ? State?.EditDcOption?.Description ?? string.Empty : Setting != null ? GetValueToolTip(Setting, DcValue) : string.Empty;

	public static string GetDisplayValue(Setting setting, uint value)
	{
		if (setting.Type == SettingType.Numeric)
			return value.ToString(CultureInfo.InvariantCulture);

		Option? option = setting.Options.FirstOrDefault(o => o.Index == value);
		return option != null && !string.IsNullOrWhiteSpace(option.FriendlyName) ? option.FriendlyName : value.ToString(CultureInfo.InvariantCulture);
	}

	public static string GetValueToolTip(Setting setting, uint value)
	{
		if (setting.Type != SettingType.Numeric)
			return setting.Options.FirstOrDefault(option => option.Index == value)?.Description ?? string.Empty;

		if (!setting.Minimum.HasValue || !setting.Maximum.HasValue || !setting.Increment.HasValue)
			return "Unadjustable";

		var lines = new List<string>
		{
			$"Range: {setting.Minimum.Value} - {setting.Maximum.Value}",
			$"Increment: {setting.Increment.Value}",
			$"Unit: {char.ToUpperInvariant(setting.Unit[0])}{setting.Unit[1..]}"
		};

		return string.Join(Environment.NewLine, lines);
	}
}