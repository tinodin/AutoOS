using AutoOS.Core.Data.Enums.Network;
using AutoOS.Core.Data.Models.Network;

namespace AutoOS.App.Data.Models.Network;

public static class Validation
{
	public static string[] GetErrors(SettingState state, Setting setting)
	{
		if (setting.Options.Count > 0)
			return setting.Options.Any(option => option.Value == state.Value) ? [] : ["No option selected"];

		if (string.IsNullOrWhiteSpace(state.Value))
			return setting.Optional ? [] : ["Value is empty"];

		if (setting.Type == NetworkSettingType.Edit)
		{
			if (setting.LimitText is { } limit && state.Value.Length > limit)
				return [$"Value cannot exceed {limit} characters"];
			return [];
		}

		if (!SettingState.TryParseNumber(setting, state.Value, out long value))
			return ["Value must be a number"];

		long minimum = setting.Min ?? (setting.Type == NetworkSettingType.Dword ? 0L : int.MinValue);
		long maximum = setting.Max ?? (setting.Type == NetworkSettingType.Dword ? (long)uint.MaxValue : int.MaxValue);
		if (value < minimum || value > maximum)
			return [$"Value must be between {minimum} and {maximum}"];
		if (setting.Step is > 0 && (value - (setting.Min ?? 0)) % setting.Step.Value != 0)
			return [$"Value must use increments of {setting.Step.Value}"];

		return [];
	}
}
