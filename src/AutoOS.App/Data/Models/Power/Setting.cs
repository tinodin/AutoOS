using AutoOS.App.Data.Enums.Power;

namespace AutoOS.App.Data.Models.Power;

public sealed record Setting(
	Guid SubgroupGuid,
	Guid Guid,
	string Name,
	string Description,
	SettingType Type,
	uint? Minimum,
	uint? Maximum,
	uint? Increment,
	string Unit,
	IReadOnlyList<Option> Options);