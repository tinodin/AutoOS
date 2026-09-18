using System.Globalization;
using AutoOS.Core.Data.Enums.Network;
using AutoOS.Core.Data.Models.Network;

namespace AutoOS.App.Data.Models.Network;

public sealed partial class SettingState(Setting setting) : ObservableObject
{
	public Setting Setting { get; } = setting;

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(IsModified))]
	[NotifyPropertyChangedFor(nameof(DisplayCurrent))]
	[NotifyPropertyChangedFor(nameof(IsDefault))]
	[NotifyPropertyChangedFor(nameof(HasPendingRecommendation))]
	public partial string Value { get; set; } = setting.CurrentValue;

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(IsModified))]
	[NotifyPropertyChangedFor(nameof(DisplayOriginal))]
	public partial string OriginalValue { get; set; } = setting.CurrentValue;

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(DisplayRecommended))]
	[NotifyPropertyChangedFor(nameof(HasPendingRecommendation))]
	public partial string RecommendedValue { get; set; } = string.Empty;

	[ObservableProperty]
	public partial string EditValue { get; set; } = string.Empty;

	[ObservableProperty]
	public partial Option? EditOption { get; set; }

	public bool IsModified => Value != OriginalValue;

	public bool HasErrors => Validation.GetErrors(this, Setting).Length > 0;

	public bool IsDefault
	{
		get
		{
			if (Setting.Type is NetworkSettingType.Int or NetworkSettingType.Dword
				&& TryParseNumber(Value, out long current) && TryParseNumber(Setting.DefaultValue, out long defaultValue))
				return current == defaultValue;

			return Value == Setting.DefaultValue;
		}
	}

	private bool TryParseNumber(string text, out long value)
	{
		if (Setting.Base == 16 && text.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
			text = text[2..];
		return long.TryParse(text, Setting.Base == 16 ? NumberStyles.HexNumber : NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
	}

	public static bool TryParseNumber(Setting setting, string text, out long value)
	{
		if (setting.Base == 16 && text.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
			text = text[2..];
		return long.TryParse(text, setting.Base == 16 ? NumberStyles.HexNumber : NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
	}

	public string DisplayCurrent => GetDisplayValue(Value);

	public string DisplayOriginal => GetDisplayValue(OriginalValue);

	public string DisplayRecommended => string.IsNullOrEmpty(RecommendedValue) ? string.Empty : GetDisplayValue(RecommendedValue);

	public bool HasPendingRecommendation
	{
		get
		{
			if (string.IsNullOrEmpty(RecommendedValue))
				return false;

			if (Setting.Type is NetworkSettingType.Int or NetworkSettingType.Dword
				&& TryParseNumber(Value, out long current) && TryParseNumber(RecommendedValue, out long recommended))
				return current != recommended;

			return !string.Equals(Value, RecommendedValue, StringComparison.Ordinal);
		}
	}

	public string GetDisplayValue(string value) => Setting.Options.FirstOrDefault(option => option.Value == value)?.Name ?? value;

	public static string GetRangeToolTip(Setting setting)
	{
		List<string> lines = [$"Range: {setting.Min} - {setting.Max}"];
		if (setting.Step.HasValue)
			lines.Add($"Increment: {setting.Step.Value}");
		if (setting.Type is NetworkSettingType.Int or NetworkSettingType.Dword)
			lines.Add($"Base: {setting.Base}");

		return string.Join(Environment.NewLine, lines);
	}

	public string GetEditedValue()
	{
		if (Setting.Type == NetworkSettingType.Enum)
			return EditOption?.Value ?? Value;

		return Setting.UpperCase ? EditValue.ToUpperInvariant() : EditValue;
	}
}
