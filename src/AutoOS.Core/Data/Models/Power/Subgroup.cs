namespace AutoOS.Core.Data.Models.Power;

public sealed record Subgroup(
	Guid Guid,
	string Name,
	string Description,
	IReadOnlyList<Setting> Settings);
