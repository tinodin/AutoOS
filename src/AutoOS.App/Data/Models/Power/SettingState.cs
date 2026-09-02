using System.Globalization;

namespace AutoOS.App.Data.Models.Power;

public sealed partial class SettingState(Setting setting) : ObservableObject
{
	public Setting Setting { get; } = setting;

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(IsModified))]
	[NotifyPropertyChangedFor(nameof(IsAcModified))]
	[NotifyPropertyChangedFor(nameof(DisplayAc))]
	[NotifyPropertyChangedFor(nameof(EditAcToolTip))]
	[NotifyPropertyChangedFor(nameof(IsAcDifferent))]
	public partial uint AcValue { get; set; }

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(IsModified))]
	[NotifyPropertyChangedFor(nameof(IsDcModified))]
	[NotifyPropertyChangedFor(nameof(DisplayDc))]
	[NotifyPropertyChangedFor(nameof(EditDcToolTip))]
	[NotifyPropertyChangedFor(nameof(IsDcDifferent))]
	public partial uint DcValue { get; set; }

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(IsModified))]
	[NotifyPropertyChangedFor(nameof(IsAcModified))]
	[NotifyPropertyChangedFor(nameof(DisplayOriginalAc))]
	public partial uint OriginalAcValue { get; set; }

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(IsModified))]
	[NotifyPropertyChangedFor(nameof(IsDcModified))]
	[NotifyPropertyChangedFor(nameof(DisplayOriginalDc))]
	public partial uint OriginalDcValue { get; set; }

	[ObservableProperty]
	public partial string EditAcValue { get; set; } = string.Empty;

	[ObservableProperty]
	public partial string EditDcValue { get; set; } = string.Empty;

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(EditAcToolTip))]
	public partial Option? EditAcOption { get; set; }

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(EditDcToolTip))]
	public partial Option? EditDcOption { get; set; }

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(DisplayCompareAc))]
	[NotifyPropertyChangedFor(nameof(IsAcDifferent))]
	public partial uint? CompareAcValue { get; set; }

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(DisplayCompareDc))]
	[NotifyPropertyChangedFor(nameof(IsDcDifferent))]
	public partial uint? CompareDcValue { get; set; }

	public bool IsModified => AcValue != OriginalAcValue || DcValue != OriginalDcValue;

	public bool IsAcModified => AcValue != OriginalAcValue;

	public bool IsDcModified => DcValue != OriginalDcValue;

	public bool IsAcDifferent => CompareAcValue is { } compare && !string.Equals(GetDisplayValue(Setting, AcValue), GetDisplayValue(Setting, compare), StringComparison.Ordinal);

	public bool IsDcDifferent => CompareDcValue is { } compare && !string.Equals(GetDisplayValue(Setting, DcValue), GetDisplayValue(Setting, compare), StringComparison.Ordinal);

	public string DisplayAc => GetDisplayValue(Setting, AcValue);

	public string DisplayDc => GetDisplayValue(Setting, DcValue);

	public string DisplayCompareAc => CompareAcValue is { } compare ? GetDisplayValue(Setting, compare) : string.Empty;

	public string DisplayCompareDc => CompareDcValue is { } compare ? GetDisplayValue(Setting, compare) : string.Empty;

	public string DisplayOriginalAc => GetDisplayValue(Setting, OriginalAcValue);

	public string DisplayOriginalDc => GetDisplayValue(Setting, OriginalDcValue);

	public string AcToolTip => GetValueToolTip(Setting, AcValue);

	public string DcToolTip => GetValueToolTip(Setting, DcValue);

	public string CompareAcToolTip => CompareAcValue is { } compare ? GetValueToolTip(Setting, compare) : string.Empty;

	public string CompareDcToolTip => CompareDcValue is { } compare ? GetValueToolTip(Setting, compare) : string.Empty;

	public string OriginalAcToolTip => GetValueToolTip(Setting, OriginalAcValue);

	public string OriginalDcToolTip => GetValueToolTip(Setting, OriginalDcValue);

	public string EditAcToolTip => HasOptions ? EditAcOption?.Description ?? string.Empty : AcToolTip;

	public string EditDcToolTip => HasOptions ? EditDcOption?.Description ?? string.Empty : DcToolTip;

	public bool HasOptions => Setting.Options.Count > 0;

	public static string GetDisplayValue(Setting setting, uint value)
	{
		if (setting.Options is not { Count: > 0 })
			return value.ToString(CultureInfo.InvariantCulture);

		Option? option = setting.Options.FirstOrDefault(o => o.Index == value);
		return option != null && !string.IsNullOrWhiteSpace(option.FriendlyName) ? option.FriendlyName : value.ToString(CultureInfo.InvariantCulture);
	}

	public static string GetValueToolTip(Setting setting, uint value)
	{
		if (setting.Options is { Count: > 0 })
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
