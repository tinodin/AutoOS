using AutoOS.Core.Data.Models.Bios;
using AutoOS.Core.Helpers.Bios;

namespace AutoOS.App.Data.Models.Bios;

public static class Validation
{
	public static string[] GetErrors(SettingState state, Setting setting)
	{
		if (setting.Options.Count > 0)
			return SettingState.ResolveOption(setting, state.Value) == null ? ["No option selected"] : [];

		if (string.IsNullOrWhiteSpace(state.Value))
			return ["Value is empty"];

		if (HiiHelper.TryParseNumericValue(state.Value, setting.NumericFormat, out ulong val))
		{
			if (setting.Minimum != 0 || setting.Maximum != 0)
			{
				if (val < setting.Minimum || val > setting.Maximum)
					return [$"Value must be between {setting.Minimum} and {setting.Maximum}"];
			}
		}
		else if (setting.Width <= 8)
		{
			return ["Value must be a number"];
		}

		return [];
	}
}