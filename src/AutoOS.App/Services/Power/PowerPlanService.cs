using System.Collections.Concurrent;
using AutoOS.App.Data.Enums.Power;
using AutoOS.App.Data.Models.Power;
using AutoOS.Core.Helpers.Power;

namespace AutoOS.App.Services.Power;

public sealed class PowerPlanService : IPowerPlanService
{
	private static readonly Guid NoneSubgroupGuid = new("fea3413e-7e05-4911-9a71-700331f1c294");
	private static readonly Guid MultimediaSubgroupGuid = new("9596fb26-9850-41fd-ac3e-f7c3c00afd4b");

	public IReadOnlyList<Plan> GetPlans()
	{
		var plans = new List<Plan>();
		foreach (Guid guid in PowerHelper.EnumerateSchemes())
			plans.Add(new Plan(guid, PowerHelper.ReadFriendlyName(guid, null, null), PowerHelper.ReadDescription(guid)));
		return plans;
	}

	public Guid GetActivePlanGuid() => PowerHelper.ReadActiveScheme();

	public (Plan Plan, IReadOnlyList<Subgroup> Subgroups, IReadOnlyDictionary<Setting, Value> Values) ReadCompleteScheme(Guid scheme)
	{
		Plan plan = new(scheme, PowerHelper.ReadFriendlyName(scheme, null, null), PowerHelper.ReadDescription(scheme));
		var values = new Dictionary<Setting, Value>();
		var subgroups = new List<Subgroup>();

		var noneSubgroup = new Subgroup(NoneSubgroupGuid, "None", string.Empty, EnumerateSettings(scheme, NoneSubgroupGuid, null, values));
		subgroups.Add(noneSubgroup);

		foreach (Guid subgroupGuid in PowerHelper.EnumerateSubgroups(scheme))
		{
			string name = subgroupGuid == MultimediaSubgroupGuid ? "Multimedia settings" : PowerHelper.ReadFriendlyName(scheme, subgroupGuid, null);
			if (string.IsNullOrWhiteSpace(name))
				continue;

			List<Setting> settings = EnumerateSettings(scheme, subgroupGuid, subgroupGuid, values);
			if (settings.Count == 0)
				continue;
			subgroups.Add(new Subgroup(subgroupGuid, name, PowerHelper.ReadDescription(scheme, subgroupGuid), settings));
		}

		subgroups.Remove(noneSubgroup);
		subgroups.Insert(0, noneSubgroup);
		return (plan, subgroups, values);
	}

	public IReadOnlyDictionary<Setting, Value> ReadValues(Guid scheme, IReadOnlyList<Setting> settings)
	{
		var values = new ConcurrentDictionary<Setting, Value>();
		Parallel.ForEach(settings, setting =>
		{
			if (PowerHelper.TryReadAcValueIndex(scheme, setting.SubgroupGuid, setting.Guid, out uint acValue) &&
				PowerHelper.TryReadDcValueIndex(scheme, setting.SubgroupGuid, setting.Guid, out uint dcValue))
			{
				values[setting] = new Value(acValue, dcValue);
			}
		});
		return new Dictionary<Setting, Value>(values);
	}

	public Value? ReadValues(Guid scheme, Guid subgroupGuid, Guid settingGuid)
	{
		if (PowerHelper.TryReadAcValueIndex(scheme, subgroupGuid, settingGuid, out uint acValue) &&
			PowerHelper.TryReadDcValueIndex(scheme, subgroupGuid, settingGuid, out uint dcValue))
		{
			return new Value(acValue, dcValue);
		}
		return null;
	}

	public void SetActiveScheme(Guid scheme) => PowerHelper.PowerSetActiveScheme(scheme);

	public void CommitChanges(Guid scheme, IEnumerable<(Setting Setting, Value Value)> changes)
	{
		foreach ((Setting setting, Value value) in changes)
		{
			PowerHelper.WriteACValueIndex(scheme, setting.SubgroupGuid, setting.Guid, value.AcValue);
			PowerHelper.WriteDCValueIndex(scheme, setting.SubgroupGuid, setting.Guid, value.DcValue);
		}

		PowerHelper.PowerSetActiveScheme(scheme);
	}

	public Plan UpdatePlanMetadata(Plan plan, string name, string description)
	{
		PowerHelper.WriteSchemeFriendlyName(plan.Guid, name);
		PowerHelper.WriteSchemeDescription(plan.Guid, description);
		return new Plan(plan.Guid, PowerHelper.ReadFriendlyName(plan.Guid, null, null), PowerHelper.ReadDescription(plan.Guid));
	}

	public Guid DuplicateScheme(Guid scheme, string name, string description) => PowerHelper.DuplicateScheme(scheme, name, description);

	public void DeleteScheme(Guid scheme) => PowerHelper.DeleteScheme(scheme);

	public Guid ImportScheme(string filePath) => PowerHelper.ImportPowerScheme(filePath);

	public void ExportScheme(Guid scheme, string path) => PowerHelper.ExportPowerScheme(scheme, path);

	private static List<Setting> EnumerateSettings(Guid scheme, Guid subgroupGuid, Guid? enumerationSubgroup, Dictionary<Setting, Value> values)
	{
		List<Setting> settings = [];
		foreach (Guid settingGuid in PowerHelper.EnumerateSettings(scheme, enumerationSubgroup))
		{
			if (!PowerHelper.TryReadAcValueIndex(scheme, subgroupGuid, settingGuid, out uint acValue) || !PowerHelper.TryReadDcValueIndex(scheme, subgroupGuid, settingGuid, out uint dcValue))
				continue;

			uint? minimum = PowerHelper.TryReadValueMin(subgroupGuid, settingGuid, out uint minimumValue) ? minimumValue : null;
			uint? maximum = PowerHelper.TryReadValueMax(subgroupGuid, settingGuid, out uint maximumValue) ? maximumValue : null;
			uint? increment = PowerHelper.TryReadValueIncrement(subgroupGuid, settingGuid, out uint incrementValue) ? incrementValue : null;
			string name = PowerHelper.ReadFriendlyName(scheme, subgroupGuid, settingGuid);

			List<Option> options = BuildOptions(subgroupGuid, settingGuid, minimum, maximum, increment);
			bool isOptions = options.Count > 0;

			var setting = new Setting(
				subgroupGuid,
				settingGuid,
				name,
				PowerHelper.ReadDescription(scheme, subgroupGuid, settingGuid),
				isOptions ? SettingType.Options : SettingType.Numeric,
				minimum,
				maximum,
				increment,
				PowerHelper.ReadValueUnitsSpecifier(subgroupGuid, settingGuid),
				options);
			settings.Add(setting);
			values[setting] = new Value(acValue, dcValue);
		}

		return settings;
	}

	private static List<Option> BuildOptions(Guid subgroupGuid, Guid settingGuid, uint? minimum, uint? maximum, uint? increment)
	{
		if (minimum.HasValue && maximum.HasValue && increment.HasValue && maximum.Value > minimum.Value && increment.Value > 0)
			return [];

		var options = new List<Option>();
		for (uint index = 0; index < 4096; index++)
		{
			string friendlyName = PowerHelper.ReadPossibleFriendlyName(subgroupGuid, settingGuid, index);
			if (string.IsNullOrWhiteSpace(friendlyName))
				break;

			string description = PowerHelper.ReadPossibleDescription(subgroupGuid, settingGuid, index);
			options.Add(new Option(index, friendlyName, description));
		}

		return options;
	}
}