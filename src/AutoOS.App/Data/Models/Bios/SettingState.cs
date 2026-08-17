using System.Globalization;
using AutoOS.Core.Data.Models.Bios;
using AutoOS.Core.Helpers.Bios;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AutoOS.App.Data.Models.Bios;

public sealed partial class SettingState : ObservableObject
{
	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(IsModified))]
	public partial string? Value { get; set; }

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(IsModified))]
	public partial string? OriginalValue { get; set; }

	public bool IsModified => !string.Equals(Value, OriginalValue, StringComparison.Ordinal);

	public void Commit()
	{
		OriginalValue = Value;
	}

	public static Option? ResolveOption(Setting setting, string? value)
	{
		if (setting.Options.Count == 0 || string.IsNullOrEmpty(value))
			return null;

		if (ulong.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out ulong numeric) || HiiHelper.TryParseNumericValue(value, setting.NumericFormat, out numeric))
		{
			Option? option = setting.Options.FirstOrDefault(opt => opt.Value == numeric);
			if (option != null)
				return option;
		}

		return setting.Options.FirstOrDefault(opt => string.Equals(opt.Index, value, StringComparison.OrdinalIgnoreCase)) ?? setting.Options.FirstOrDefault(option => string.Equals(option.Label, value, StringComparison.OrdinalIgnoreCase));
	}

	public static string GetDisplayValue(Setting setting, string? value) =>
		setting.Options.Count > 0 ? ResolveOption(setting, value)?.Label ?? value ?? string.Empty : value ?? string.Empty;

	public static string GetCanonicalValue(Setting setting, string? value) =>
		ResolveOption(setting, value)?.Value.ToString(CultureInfo.InvariantCulture) ?? value ?? string.Empty;

	public static bool MatchesDefault(Setting setting, SettingState state) =>
		!string.IsNullOrEmpty(setting.Default) &&
		string.Equals(GetDisplayValue(setting, state.Value), setting.Default, StringComparison.OrdinalIgnoreCase);

	public static bool HasPendingRecommendation(Setting setting, SettingState state)
	{
		if (setting.RecommendedOption != null)
			return ResolveOption(setting, state.Value)?.Value != setting.RecommendedOption.Value;

		return !string.IsNullOrEmpty(setting.RecommendedValue) &&
			!string.Equals(state.Value, setting.RecommendedValue, StringComparison.Ordinal);
	}
}
