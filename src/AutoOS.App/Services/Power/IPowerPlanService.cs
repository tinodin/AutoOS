using AutoOS.App.Data.Models.Power;

namespace AutoOS.App.Services.Power;

public interface IPowerPlanService
{
	IReadOnlyList<Plan> GetPlans();

	Guid GetActivePlanGuid();

	(Plan Plan, IReadOnlyList<Subgroup> Subgroups, IReadOnlyDictionary<Setting, Value> Values) ReadCompleteScheme(Guid scheme);

	IReadOnlyDictionary<Setting, Value> ReadValues(Guid scheme, IReadOnlyList<Setting> settings);

	Value? ReadValues(Guid scheme, Guid subgroupGuid, Guid settingGuid);

	void SetActiveScheme(Guid scheme);

	void CommitChanges(Guid scheme, IEnumerable<(Setting Setting, Value Value)> changes);

	Plan UpdatePlanMetadata(Plan plan, string name, string description);

	Guid DuplicateScheme(Guid scheme, string name, string description);

	void DeleteScheme(Guid scheme);

	Guid ImportScheme(string filePath);

	void ExportScheme(Guid scheme, string path);
}
