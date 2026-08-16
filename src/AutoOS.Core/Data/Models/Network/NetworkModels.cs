using WinRT;

namespace AutoOS.Core.Data.Models.Network;

public enum NetworkSettingType
{
	Enum,
	Dword,
	Int,
	Edit
}

[GeneratedBindableCustomProperty]
public partial class NetworkAdvancedSetting
{
	public string Key { get; set; } = string.Empty;
	public string Name { get; set; } = string.Empty;
	public string CurrentValue { get; set; } = string.Empty;
	public string DefaultValue { get; set; } = string.Empty;
	public NetworkSettingType Type { get; set; } = NetworkSettingType.Enum;
	public List<NetworkSettingOption> Options { get; set; } = [];
	public int Base { get; set; } = 10;
	public int? Min { get; set; }
	public int? Max { get; set; }
	public int? Step { get; set; }
	public int? LimitText { get; set; }
	public bool UpperCase { get; set; }
	public bool Optional { get; set; }
	public Dictionary<string, string> RawMetadata { get; set; } = [];
}

[GeneratedBindableCustomProperty]
public partial class NetworkSettingOption
{
	public string Name { get; set; } = string.Empty;
	public string Value { get; set; } = string.Empty;
}
