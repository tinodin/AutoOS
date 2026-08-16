using AutoOS.App.Data.Models.Power;

namespace AutoOS.App.Data.Contracts;

public interface IPowerPlanService
{
	IReadOnlyList<Plan> GetPowerPlans();

	Guid GetActivePowerPlan();

	(Plan Plan, IReadOnlyList<Subgroup> Subgroups, IReadOnlyDictionary<Setting, Value> Values) ReadPowerPlan(Guid scheme);

	IReadOnlyDictionary<Setting, Value> ReadValues(Guid scheme, IReadOnlyList<Setting> settings);

	Value? ReadValue(Guid scheme, Guid subgroupGuid, Guid settingGuid);

	void SetActivePowerPlan(Guid scheme);

	Plan UpdatePowerPlanMetadata(Plan plan, string name, string description);

	Guid DuplicatePowerPlan(Guid scheme, string name, string description);

	void ExportPowerPlan(Guid scheme, string path);

	void DeletePowerPlan(Guid scheme);

	Guid ImportPowerPlan(string filePath);

	Task RestoreDefaultPowerPlansAsync();

	void SaveChanges(Guid scheme, IEnumerable<(Setting Setting, Value Value)> changes);
}
