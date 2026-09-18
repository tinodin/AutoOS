using AutoOS.Core.Data.Enums.Network;

namespace AutoOS.Core.Data.Models.Network;

public sealed class Setting
{
	public string Key { get; set; } = string.Empty;

	public string Name { get; set; } = string.Empty;

	public string CurrentValue { get; set; } = string.Empty;

	public string DefaultValue { get; set; } = string.Empty;

	public NetworkSettingType Type { get; set; } = NetworkSettingType.Enum;

	public List<Option> Options { get; set; } = [];

	public int Base { get; set; } = 10;

	public int? Min { get; set; }

	public int? Max { get; set; }

	public int? Step { get; set; }

	public int? LimitText { get; set; }

	public bool UpperCase { get; set; }

	public bool Optional { get; set; }

	public Dictionary<string, string> RawMetadata { get; set; } = [];
}
