using AutoOS.Core.Data.Enums.Network;
using AutoOS.Core.Data.Models.Device;
using AutoOS.Core.Data.Models.Network;
using Microsoft.Win32;

namespace AutoOS.Core.Helpers.Network;

public static partial class NetworkHelper
{
	public static List<Setting> GetAdvancedSettings(DeviceInfo device)
	{
		var settings = new List<Setting>();
		if (string.IsNullOrEmpty(device.RegistryPath))
			return settings;

		using RegistryKey? deviceKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(device.RegistryPath);
		if (deviceKey == null)
			return settings;

		using RegistryKey? paramsKey = deviceKey.OpenSubKey(@"Ndi\Params");
		if (paramsKey == null)
			return settings;

		foreach (string paramKeyName in paramsKey.GetSubKeyNames())
		{
			using RegistryKey? paramKey = paramsKey.OpenSubKey(paramKeyName);
			if (paramKey == null)
				continue;

			NetworkSettingType type = paramKey.GetValue("type")?.ToString()?.ToLowerInvariant() switch
			{
				"dword" => NetworkSettingType.Dword,
				"int" => NetworkSettingType.Int,
				"edit" => NetworkSettingType.Edit,
				_ => NetworkSettingType.Enum
			};

			bool isEdit = type == NetworkSettingType.Edit;
			string? desc = paramKey.GetValue("ParamDesc")?.ToString();
			string? upperCase = paramKey.GetValue("UpperCase")?.ToString();
			string? optional = paramKey.GetValue("Optional")?.ToString();
			var setting = new Setting
			{
				Key = paramKeyName,
				Name = string.IsNullOrWhiteSpace(desc) ? paramKeyName : desc,
				Value = deviceKey.GetValue(paramKeyName)?.ToString() ?? string.Empty,
				Default = paramKey.GetValue("default")?.ToString() ?? string.Empty,
				Type = type,
				Base = int.TryParse(paramKey.GetValue("base")?.ToString(), out int numberBase) && numberBase == 16 ? 16 : 10,
				Min = int.TryParse(paramKey.GetValue("min")?.ToString(), out int min) ? min : null,
				Max = int.TryParse(paramKey.GetValue("max")?.ToString(), out int max) ? max : null,
				Step = int.TryParse(paramKey.GetValue("step")?.ToString(), out int step) ? step : null,
				LimitText = isEdit && int.TryParse(paramKey.GetValue("LimitText")?.ToString(), out int limitText) ? limitText : null,
				UpperCase = isEdit && (upperCase == "1" || string.Equals(upperCase, "true", StringComparison.OrdinalIgnoreCase)),
				Optional = isEdit && (optional == "1" || string.Equals(optional, "true", StringComparison.OrdinalIgnoreCase))
			};

			if (type == NetworkSettingType.Enum)
			{
				using RegistryKey? enumKey = paramKey.OpenSubKey("Enum");
				if (enumKey != null)
				{
					foreach (string valueName in enumKey.GetValueNames())
					{
						setting.Options.Add(new Option
						{
							Value = valueName,
							Name = enumKey.GetValue(valueName)?.ToString() ?? valueName
						});
					}
				}

				if (setting.Options.Count == 0)
					continue;
			}

			settings.Add(setting);
		}

		ApplyRecommendations(device, settings);

		return settings;
	}

	public static void SetAdvancedSetting(DeviceInfo device, string key, string value)
	{
		using RegistryKey deviceKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(device.RegistryPath, true) ?? throw new InvalidOperationException($"Cannot open adapter settings: {device.FriendlyName}");
		deviceKey.SetValue(key, value, RegistryValueKind.String);
	}

	public static bool OptimizeAdapter(DeviceInfo device)
	{
		List<Setting> settings = GetAdvancedSettings(device);
		var changes = settings
			.Where(setting => !string.IsNullOrEmpty(setting.Recommended) && !string.Equals(setting.Value.Trim(), setting.Recommended.Trim(), StringComparison.OrdinalIgnoreCase))
			.ToDictionary(setting => setting, setting => setting.Recommended);
		foreach ((Setting setting, string value) in changes)
			SetAdvancedSetting(device, setting.Key, value);
		return changes.Count > 0;
	}

	private static void ApplyRecommendations(DeviceInfo device, List<Setting> settings)
	{
		foreach (Recommendation recommendation in Recommendations.Rules.Where(recommendation => recommendation.Type == device.NicType))
		{
			if (recommendation.Condition != null && !recommendation.Condition(settings))
				continue;

			Setting? setting = settings.FirstOrDefault(setting => setting.Name == recommendation.Name);
			Option? option = setting?.Options.FirstOrDefault(option => option.Name == recommendation.Recommended);
			if (setting != null && option != null)
				setting.Recommended = option.Value;
		}
	}
}
