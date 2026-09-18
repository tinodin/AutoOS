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

			NetworkSettingType type = ParseSettingType(paramKey.GetValue("type")?.ToString());
			string? desc = paramKey.GetValue("ParamDesc")?.ToString();
			var setting = new Setting
			{
				Key = paramKeyName,
				Name = string.IsNullOrWhiteSpace(desc) ? paramKeyName : desc,
				CurrentValue = deviceKey.GetValue(paramKeyName)?.ToString() ?? string.Empty,
				DefaultValue = paramKey.GetValue("default")?.ToString() ?? string.Empty,
				Type = type,
				Base = int.TryParse(paramKey.GetValue("base")?.ToString(), out int numberBase) && numberBase == 16 ? 16 : 10,
				Min = ReadIntValue(paramKey, "min"),
				Max = ReadIntValue(paramKey, "max"),
				Step = ReadIntValue(paramKey, "step"),
				LimitText = type == NetworkSettingType.Edit ? ReadIntValue(paramKey, "LimitText") : null,
				UpperCase = type == NetworkSettingType.Edit && IsEnabledValue(paramKey.GetValue("UpperCase")?.ToString()),
				Optional = type == NetworkSettingType.Edit && IsEnabledValue(paramKey.GetValue("Optional")?.ToString())
			};

			foreach (string valueName in paramKey.GetValueNames())
				setting.RawMetadata[valueName] = paramKey.GetValue(valueName)?.ToString() ?? string.Empty;

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
		Dictionary<Setting, string> changes = GetOptimizedValues(device, settings)
			.Where(pair => !string.Equals(pair.Key.CurrentValue.Trim(), pair.Value.Trim(), StringComparison.OrdinalIgnoreCase))
			.ToDictionary(pair => pair.Key, pair => pair.Value);
		foreach ((Setting setting, string value) in changes)
			SetAdvancedSetting(device, setting.Key, value);
		return changes.Count > 0;
	}

	public static IReadOnlyDictionary<Setting, string> GetOptimizedValues(DeviceInfo device, List<Setting> settings)
	{
		var changes = new Dictionary<Setting, string>();

		foreach (Recommendation recommendation in Recommendations.Rules.Where(recommendation => recommendation.Type == device.NicType))
			RecommendSetting(changes, settings, recommendation.Name, recommendation.Recommended);

		if (device.NicType == NicDeviceType.LAN)
		{
			if (settings.Any(static setting => setting.Name == "Interrupt Moderation Rate"))
			{
				RecommendSetting(changes, settings, "Interrupt Moderation", "Enabled");
				RecommendSetting(changes, settings, "Interrupt Moderation Rate", "Medium");
			}
			else
			{
				RecommendSetting(changes, settings, "Interrupt Moderation", "Disabled");
			}
		}

		return changes;
	}

	private static NetworkSettingType ParseSettingType(string? typeValue) => typeValue?.ToLowerInvariant() switch
	{
		"dword" => NetworkSettingType.Dword,
		"int" => NetworkSettingType.Int,
		"edit" => NetworkSettingType.Edit,
		_ => NetworkSettingType.Enum
	};

	private static int? ReadIntValue(RegistryKey key, string valueName) =>
		int.TryParse(key.GetValue(valueName)?.ToString(), out int value) ? value : null;

	private static bool IsEnabledValue(string? value) =>
		value == "1" || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);

	private static void RecommendSetting(Dictionary<Setting, string> changes, List<Setting> settings, string displayName, string displayValue)
	{
		Setting? setting = settings.FirstOrDefault(setting => setting.Name == displayName);
		Option? option = setting?.Options.FirstOrDefault(option => option.Name == displayValue);
		if (setting != null && option != null)
			changes[setting] = option.Value;
	}
}
