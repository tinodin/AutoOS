using AutoOS.Core.Helpers.Device.Models;
using AutoOS.Core.Helpers.Network.Models;
using Microsoft.Win32;

namespace AutoOS.Core.Helpers.Network;

public static partial class NetworkHelper
{
	public static List<NetworkAdvancedSetting> GetAdvancedSettings(DeviceInfo device)
	{
		var settings = new List<NetworkAdvancedSetting>();
		if (string.IsNullOrEmpty(device.RegistryPath)) return settings;

		using RegistryKey? deviceKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(device.RegistryPath);
		if (deviceKey == null) return settings;

		using RegistryKey? paramsKey = deviceKey.OpenSubKey(@"Ndi\Params");
		if (paramsKey == null) return settings;

		foreach (string paramKeyName in paramsKey.GetSubKeyNames())
		{
			using RegistryKey? paramKey = paramsKey.OpenSubKey(paramKeyName);
			if (paramKey == null) continue;

			string? typeValue = paramKey.GetValue("type")?.ToString()?.ToLowerInvariant();
			NetworkSettingType type = typeValue switch
			{
				"enum" => NetworkSettingType.Enum,
				"dword" => NetworkSettingType.Dword,
				"int" => NetworkSettingType.Int,
				"edit" => NetworkSettingType.Edit,
				_ => NetworkSettingType.Enum
			};

			string? desc = paramKey.GetValue("ParamDesc")?.ToString();
			var setting = new NetworkAdvancedSetting
			{
				Key = paramKeyName,
				Name = string.IsNullOrWhiteSpace(desc) ? paramKeyName : desc,
				CurrentValue = deviceKey.GetValue(paramKeyName)?.ToString() ?? string.Empty,
				DefaultValue = paramKey.GetValue("default")?.ToString() ?? string.Empty,
				Type = type
			};

			foreach (string vn in paramKey.GetValueNames())
				setting.RawMetadata[vn] = paramKey.GetValue(vn)?.ToString() ?? string.Empty;

			switch (type)
			{
				case NetworkSettingType.Enum:
					using (RegistryKey? enumKey = paramKey.OpenSubKey("Enum"))
					{
						if (enumKey != null)
						{
							foreach (string valueName in enumKey.GetValueNames())
							{
								setting.Options.Add(new NetworkSettingOption
								{
									Value = valueName,
									Name = enumKey.GetValue(valueName)?.ToString() ?? valueName
								});
							}
						}
					}
					break;

				case NetworkSettingType.Dword:
				case NetworkSettingType.Int:
					if (int.TryParse(paramKey.GetValue("min")?.ToString(), out int min))
						setting.Min = min;
					if (int.TryParse(paramKey.GetValue("max")?.ToString(), out int max))
						setting.Max = max;
					if (int.TryParse(paramKey.GetValue("step")?.ToString(), out int step))
						setting.Step = step;
					break;

				case NetworkSettingType.Edit:
					if (int.TryParse(paramKey.GetValue("LimitText")?.ToString(), out int limit))
						setting.LimitText = limit;

					string? upperCaseValue = paramKey.GetValue("UpperCase")?.ToString();
					setting.UpperCase = upperCaseValue == "1" || upperCaseValue?.ToLowerInvariant() == "true";

					string? optionalValue = paramKey.GetValue("Optional")?.ToString();
					setting.Optional = optionalValue == "1" || optionalValue?.ToLowerInvariant() == "true";
					break;
			}

			bool hasValidData = type switch
			{
				NetworkSettingType.Enum => setting.Options.Count > 0,
				NetworkSettingType.Dword or NetworkSettingType.Int => true,
				NetworkSettingType.Edit => true,
				_ => false
			};

			if (hasValidData)
				settings.Add(setting);
		}

		return settings;
	}

	public static void SetAdvancedSetting(DeviceInfo device, string key, string value)
	{
		Microsoft.Win32.Registry.LocalMachine.OpenSubKey(device.RegistryPath, true)?.SetValue(key, value, RegistryValueKind.String);
	}

	public static bool OptimizeAdapter(DeviceInfo device)
	{
		List<NetworkAdvancedSetting> settings = GetAdvancedSettings(device);
		bool anyChanged = false;

		if (device.NicType == NicDeviceType.WiFi)
		{
			anyChanged |= ApplySetting(device, settings, "2.4G Wireless Mode", "IEEE 802.11b/g/n/ax");
			anyChanged |= ApplySetting(device, settings, "5G Wireless Mode", "IEEE 802.11a/n/ac/ax");
			anyChanged |= ApplySetting(device, settings, "802.11ax/ac/n/abg", "1. 802.11ax");
			anyChanged |= ApplySetting(device, settings, "802.11n/ac/ax/be Wireless Mode", "5. 802.11be");
			anyChanged |= ApplySetting(device, settings, "802.11a/b/g Wireless Mode", "6. Dual Band 802.11a/b/g");
			anyChanged |= ApplySetting(device, settings, "802.11be/ax/ac/n/abg", "1. 802.11be");
			anyChanged |= ApplySetting(device, settings, "802.11d", "Disabled");
			anyChanged |= ApplySetting(device, settings, "802.11/ac/ax Wireless Mode", "4. 802.11ax");
			anyChanged |= ApplySetting(device, settings, "802.11n channel width for 2.4GHz", "Auto");
			anyChanged |= ApplySetting(device, settings, "802.11n channel width for 5.2GHz", "Auto");
			anyChanged |= ApplySetting(device, settings, "802.11n/ac Wireless Mode", "802.11ac");
			anyChanged |= ApplySetting(device, settings, "802.11n/ac/ax Wireless Mode", "4. 802.11ax");
			anyChanged |= ApplySetting(device, settings, "ARP offload for WoWLAN", "Enabled");
			anyChanged |= ApplySetting(device, settings, "Band Selection", "1. All Band");
			anyChanged |= ApplySetting(device, settings, "Beacon Interval", "100");
			anyChanged |= ApplySetting(device, settings, "Channel Width for 2.4GHz", "Auto");
			anyChanged |= ApplySetting(device, settings, "Channel Width for 5GHz", "Auto");
			anyChanged |= ApplySetting(device, settings, "Channel Width for 6GHz", "Auto");
			anyChanged |= ApplySetting(device, settings, "Channel-Load usage for AP Selection", "Disabled");
			anyChanged |= ApplySetting(device, settings, "D0 PacketCoalescing", "Disable");
			anyChanged |= ApplySetting(device, settings, "Dynamic MIMO Power Save", "Disable");
			anyChanged |= ApplySetting(device, settings, "EnableAdaptivity", "Auto");
			anyChanged |= ApplySetting(device, settings, "Fat Channel Intolerant", "Disabled");
			anyChanged |= ApplySetting(device, settings, "Global BG Scan blocking", "Always");
			anyChanged |= ApplySetting(device, settings, "GTK rekeying for WoWLAN", "Disabled");
			anyChanged |= ApplySetting(device, settings, "HLDiffForAdaptivity", "7");
			anyChanged |= ApplySetting(device, settings, "HT mode", "VHT mode");
			anyChanged |= ApplySetting(device, settings, "Idle Power Down Restriction", "Enabled");
			anyChanged |= ApplySetting(device, settings, "L2HForAdaptivity", "Auto");
			anyChanged |= ApplySetting(device, settings, "MIMO Power Save Mode", "No SMPS");
			anyChanged |= ApplySetting(device, settings, "Mixed Mode Protection", "CTS-to-self Enabled");
			anyChanged |= ApplySetting(device, settings, "Multi-Channel Concurrent", "Enabled + Hotspot");
			anyChanged |= ApplySetting(device, settings, "NS offload for WoWLAN", "Enabled");
			anyChanged |= ApplySetting(device, settings, "Packet Coalescing", "Disabled");
			anyChanged |= ApplySetting(device, settings, "Power Saving", "Disabled");
			anyChanged |= ApplySetting(device, settings, "Preferred Band", "1. No Preference");
			anyChanged |= ApplySetting(device, settings, "Preamble Mode", "Short & long");
			anyChanged |= ApplySetting(device, settings, "QoS Support", "Auto");
			anyChanged |= ApplySetting(device, settings, "Roaming Aggressiveness", "1. Lowest");
			anyChanged |= ApplySetting(device, settings, "Roaming Aggressiveness", "1. Disabled");
			anyChanged |= ApplySetting(device, settings, "Roaming aggressiveness", "1.Lowest");
			anyChanged |= ApplySetting(device, settings, "Roaming Sensitivity Level", "Disable");
			anyChanged |= ApplySetting(device, settings, "RscIPv4", "Enabled");
			anyChanged |= ApplySetting(device, settings, "RscIPv6", "Enabled");
			anyChanged |= ApplySetting(device, settings, "Sleep on WoWLAN Disconnect", "Disabled");
			anyChanged |= ApplySetting(device, settings, "Throughput Booster", "Disabled");
			anyChanged |= ApplySetting(device, settings, "Transmit Power", "5. Highest");
			anyChanged |= ApplySetting(device, settings, "Transmit Power Level", "1. Highest");
			anyChanged |= ApplySetting(device, settings, "U-APSD Support", "Disabled");
			anyChanged |= ApplySetting(device, settings, "U-APSD support", "Disabled");
			anyChanged |= ApplySetting(device, settings, "USB Mode", "Auto");
			anyChanged |= ApplySetting(device, settings, "Ultra High Band (6GHz)", "Enabled");
			anyChanged |= ApplySetting(device, settings, "VHT 2.4G", "Enable");
			anyChanged |= ApplySetting(device, settings, "Wake on Magic Packet", "Disabled");
			anyChanged |= ApplySetting(device, settings, "WakeOnMagicPacket", "Disable");
			anyChanged |= ApplySetting(device, settings, "Wake on Pattern Match", "Disabled");
			anyChanged |= ApplySetting(device, settings, "WakeOnPatternMatch", "Disable");
			anyChanged |= ApplySetting(device, settings, "Wireless Mode", "6. 802.11a/b/g");
			anyChanged |= ApplySetting(device, settings, "Wireless Mode", "12 - 11 a/b/g/n/ac");
		}
		else if (device.NicType == NicDeviceType.LAN)
		{
			anyChanged |= ApplySetting(device, settings, "Adaptive Inter-Frame Spacing", "Disabled");
			anyChanged |= ApplySetting(device, settings, "Advanced EEE", "Disabled");
			anyChanged |= ApplySetting(device, settings, "ARP Offload", "Enabled");
			anyChanged |= ApplySetting(device, settings, "Auto Disable Gigabit", "Disabled");
			anyChanged |= ApplySetting(device, settings, "DMA Coalescing", "Disabled");
			anyChanged |= ApplySetting(device, settings, "Downshift retries", "0");
			//anyChanged |= ApplySetting(device, settings, "Enable PME", "Disabled");
			anyChanged |= ApplySetting(device, settings, "Energy Efficient Ethernet", "Disabled");
			anyChanged |= ApplySetting(device, settings, "Energy Efficient Ethernet", "Off");
			anyChanged |= ApplySetting(device, settings, "Energy-Efficient Ethernet", "Disabled");
			anyChanged |= ApplySetting(device, settings, "Flow Control", "Disabled");
			anyChanged |= ApplySetting(device, settings, "Gigabit Lite", "Disabled");
			anyChanged |= ApplySetting(device, settings, "Gigabit Master Slave Mode", "Auto Detect");
			anyChanged |= ApplySetting(device, settings, "Gigabit PHY Mode", "Auto Detect");
			anyChanged |= ApplySetting(device, settings, "Green Ethernet", "Disabled");
			anyChanged |= ApplySetting(device, settings, "Idle power down restriction", "Enabled");
			if (settings.Any(s => s.Name == "Interrupt Moderation Rate"))
			{
				anyChanged |= ApplySetting(device, settings, "Interrupt Moderation", "Enabled");
				anyChanged |= ApplySetting(device, settings, "Interrupt Moderation Rate", "Medium");
			}
			else
			{
				anyChanged |= ApplySetting(device, settings, "Interrupt Moderation", "Disabled");
			}
			anyChanged |= ApplySetting(device, settings, "IPv4 Checksum Offload", "Rx & Tx Enabled");
			anyChanged |= ApplySetting(device, settings, "Jumbo Packet", "Disabled");
			anyChanged |= ApplySetting(device, settings, "JumboPacket", "Disabled");
			anyChanged |= ApplySetting(device, settings, "Large Send Offload Version 1", "Enabled");
			anyChanged |= ApplySetting(device, settings, "Large Send Offload V2 (IPv4)", "Enabled");
			anyChanged |= ApplySetting(device, settings, "Large Send Offload V2 (IPv6)", "Enabled");
			anyChanged |= ApplySetting(device, settings, "Link Speed Battery Saver", "Disabled");
			anyChanged |= ApplySetting(device, settings, "Locally Administered Address", "--");
			anyChanged |= ApplySetting(device, settings, "Log Link State Event", "Disabled");
			//anyChanged |= ApplySetting(device, settings, "Maximum Number of RSS Queues", "4 Queues");
			anyChanged |= ApplySetting(device, settings, "Multi-Channel Concurrent", "Disabled");
			anyChanged |= ApplySetting(device, settings, "NDIS QoS", "QoS Enabled");
			anyChanged |= ApplySetting(device, settings, "NS Offload", "Enabled");
			anyChanged |= ApplySetting(device, settings, "Packet Priority & VLAN", "Packet Priority & VLAN Enabled");
			anyChanged |= ApplySetting(device, settings, "PCI Express Link Power Saving", "Disabled");
			anyChanged |= ApplySetting(device, settings, "Power Saving", "Disabled");
			anyChanged |= ApplySetting(device, settings, "Power Saving Mode", "Disabled");
			anyChanged |= ApplySetting(device, settings, "Preamble Mode", "Short");
			anyChanged |= ApplySetting(device, settings, "Protocol ARP Offload", "Enabled");
			anyChanged |= ApplySetting(device, settings, "Protocol NS Offload", "Enabled");
			anyChanged |= ApplySetting(device, settings, "PTP Hardware Timestamp", "Disabled");
			anyChanged |= ApplySetting(device, settings, "Recv Segment Coalescing (IPv4)", "Enabled");
			anyChanged |= ApplySetting(device, settings, "Recv Segment Coalescing (IPv6)", "Enabled");
			anyChanged |= ApplySetting(device, settings, "Receive Side Scaling", "Enabled");
			anyChanged |= ApplySetting(device, settings, "Reduce Speed On Power Down", "Disabled");
			anyChanged |= ApplySetting(device, settings, "Selective Suspend", "Disabled");
			anyChanged |= ApplySetting(device, settings, "SelectiveSuspend", "Disabled");
			anyChanged |= ApplySetting(device, settings, "Selective Suspend Idle Timeout", "60");
			anyChanged |= ApplySetting(device, settings, "Software Timestamp ", "Disabled");
			//anyChanged |= ApplySetting(device, settings, "Shutdown Wake-On-Lan", "Disabled");
			anyChanged |= ApplySetting(device, settings, "Speed & Duplex", "Auto Negotiation");
			anyChanged |= ApplySetting(device, settings, "System Idle Power Saver", "Disabled");
			anyChanged |= ApplySetting(device, settings, "TCP Checksum Offload (IPv4)", "Rx & Tx Enabled");
			anyChanged |= ApplySetting(device, settings, "TCP Checksum Offload (IPv6)", "Rx & Tx Enabled");
			anyChanged |= ApplySetting(device, settings, "TCP/UDP Checksum Offload (IPv4)", "Rx & Tx Enabled");
			anyChanged |= ApplySetting(device, settings, "TCP/UDP Checksum Offload (IPv6)", "Rx & Tx Enabled");
			anyChanged |= ApplySetting(device, settings, "UDP Checksum Offload (IPv4)", "Rx & Tx Enabled");
			anyChanged |= ApplySetting(device, settings, "UDP Checksum Offload (IPv6)", "Rx & Tx Enabled");
			anyChanged |= ApplySetting(device, settings, "Ultra Low Power Mode", "Disabled");
			anyChanged |= ApplySetting(device, settings, "Wait for Link", "Off");
			// anyChanged |= ApplySetting(device, settings, "Wake from power off state", "Disabled");
			// anyChanged |= ApplySetting(device, settings, "Wake from S0ix on Magic Packet", "Disabled");
			anyChanged |= ApplySetting(device, settings, "Wake on Link", "Disabled");
			anyChanged |= ApplySetting(device, settings, "Wake on Link Settings", "Disabled");
			anyChanged |= ApplySetting(device, settings, "Wake On Link Up", "Disabled");
			// anyChanged |= ApplySetting(device, settings, "Wake on Magic Packet", "Disabled");
			// anyChanged |= ApplySetting(device, settings, "Wake On Magic Packet From S5", "Disabled");
			// anyChanged |= ApplySetting(device, settings, "Wake on magic packet when system is in the S0ix power state", "Disabled");
			anyChanged |= ApplySetting(device, settings, "Wake on Pattern Match", "Disabled");
			anyChanged |= ApplySetting(device, settings, "Wake on pattern match", "Disabled");
			anyChanged |= ApplySetting(device, settings, "Wake on Ping", "Disabled");
			anyChanged |= ApplySetting(device, settings, "WOL & Shutdown Link Speed", "Not Speed Down");
			anyChanged |= ApplySetting(device, settings, "WOL Link Power Saving", "Disabled");
		}

		return anyChanged;
	}

	private static bool ApplySetting(DeviceInfo device, List<NetworkAdvancedSetting> settings, string displayName, string displayValue)
	{
		NetworkAdvancedSetting? setting = settings.FirstOrDefault(s => s.Name == displayName);
		if (setting == null) return false;

		NetworkSettingOption? option = setting.Options.FirstOrDefault(o => o.Name == displayValue);
		if (option == null) return false;

		string currentVal = (setting.CurrentValue ?? "").Trim();
		string targetVal = (option.Value ?? "").Trim();

		if (!string.Equals(currentVal, targetVal, StringComparison.OrdinalIgnoreCase))
		{
			SetAdvancedSetting(device, setting.Key, option.Value ?? string.Empty);
			setting.CurrentValue = option.Value ?? string.Empty;
			return true;
		}
		return false;
	}
}
