using AutoOS.Core.Helpers.CPU;
using AutoOS.Core.Data.Models.CPU;
using AutoOS.Core.Helpers.Device;
using AutoOS.Core.Data.Models.Device;

namespace AutoOS.Core.Helpers.Scheduling;

public static partial class SchedulingHelper
{
	public static async Task OptimizeAffinities(DeviceInfo? device = null, Action<DeviceType, string, DeviceInfo>? onDeviceUpdated = null)
	{
		if (device == null)
		{
			await OptimizeAffinities(deviceTypes: [DeviceType.AudioController, DeviceType.GPU, DeviceType.XHCI, DeviceType.NIC], onDeviceUpdated: onDeviceUpdated);
			return;
		}

		await OptimizeAffinities([(device.DeviceType, device.PnpDeviceId)], onDeviceUpdated);
	}

	public static async Task OptimizeAffinities(IEnumerable<(DeviceType DeviceType, string PnpDeviceId)> selectedDevices, Action<DeviceType, string, DeviceInfo>? onDeviceUpdated = null)
	{
		var selectedByType = selectedDevices
			.GroupBy(d => d.DeviceType)
			.ToDictionary(g => g.Key, g => new HashSet<string>(g.Select(x => x.PnpDeviceId), StringComparer.OrdinalIgnoreCase));

		if (selectedByType.Count == 0)
			return;

		await OptimizeAffinities(deviceTypes: [.. selectedByType.Keys], selectedIdsByType: selectedByType, onDeviceUpdated: onDeviceUpdated);
	}

	private static async Task OptimizeAffinities(DeviceType[] deviceTypes, Dictionary<DeviceType, HashSet<string>>? selectedIdsByType = null, Action<DeviceType, string, DeviceInfo>? onDeviceUpdated = null)
	{
		Dictionary<DeviceType, ulong>? affinityByType = GetAffinityMasks();
		if (affinityByType == null)
			return;

		var allChangedDevices = new List<(DeviceInfo device, DeviceType deviceType)>();

		foreach (DeviceType deviceType in deviceTypes)
		{
			IEnumerable<DeviceInfo> candidates = DeviceHelper.GetDevices(deviceType).Where(d => d.SupportsIrq);
			if (selectedIdsByType != null && selectedIdsByType.TryGetValue(deviceType, out HashSet<string>? selectedIds))
				candidates = candidates.Where(d => selectedIds.Contains(d.PnpDeviceId));

			List<DeviceInfo> devices = [.. candidates];
			if (devices.Count == 0)
				continue;

			ApplyResult result = ApplyAffinityOnly(devices, affinityByType[deviceType], deviceType);
			allChangedDevices.AddRange(result.ChangedDevices.Select(d => (d, deviceType)));
		}

		if (allChangedDevices.Count == 0)
			return;

		if (onDeviceUpdated != null)
		{
			foreach ((DeviceInfo? changedDevice, DeviceType deviceType) in allChangedDevices)
			{
				onDeviceUpdated(deviceType, changedDevice.PnpDeviceId, changedDevice);
			}
		}

		await DeviceHelper.RestartDevicesAsync([.. allChangedDevices.Select(d => d.device)]);
	}

	public static Dictionary<DeviceType, ulong>? GetAffinityMasks()
	{
		CpuSetsInfo cpuSetsInfo = CpuHelper.GetCpuSets();
		(List<CpuCore>? pCores, List<CpuCore>? _) = CpuHelper.GroupCpuSetsByEfficiencyClass(cpuSetsInfo);

		if (pCores == null || pCores.Count < 4)
			return null;

		int cores = pCores.Count;
		ulong nicMask, xhciMask, gpuMask, audioMask;

		if (cores == 4)
		{
			audioMask = GetCoreMask(pCores[0]);
			gpuMask = GetCoreMask(pCores[1]) | GetCoreMask(pCores[2]);
			xhciMask = pCores[3].Threads.First().BitMask;
			nicMask = pCores[3].Threads.Last().BitMask;
		}
		else
		{
			nicMask = GetCoreMask(pCores[cores - 1]);
			xhciMask = GetCoreMask(pCores[cores - 2]);
			gpuMask = GetCoreMask(pCores[cores - 3]) | GetCoreMask(pCores[cores - 4]);
			audioMask = GetCoreMask(pCores[cores - 5]);
		}

		return new Dictionary<DeviceType, ulong>
		{
			{ DeviceType.AudioController, audioMask },
			{ DeviceType.GPU, gpuMask },
			{ DeviceType.XHCI, xhciMask },
			{ DeviceType.NIC, nicMask }
		};
	}

	private static ulong GetCoreMask(CpuCore core) => CpuHelper.GetCoreMask(core);

	private static ApplyResult ApplyAffinityOnly(List<DeviceInfo> devices, ulong assignmentSetOverride, DeviceType deviceType)
	{
		var result = new ApplyResult();
		var changedDevices = new List<DeviceInfo>();

		foreach (DeviceInfo device in devices)
		{
			bool msiChanged = device.MsiSupported != 1;
			bool affinityChanged = device.DevicePolicy != 4 || device.AssignmentSetOverride != assignmentSetOverride;

			if (msiChanged)
			{
				uint msiLimit = device.MaxMsiLimit > 0 ? device.MaxMsiLimit : 1;
				DeviceHelper.SetMSIMode(device.PnpDeviceId, true, msiLimit);
				device.MsiSupported = 1;
				device.MsiLimit = msiLimit;
			}

			if (affinityChanged)
			{
				DeviceHelper.SetAffinityPolicy(device.PnpDeviceId, 4, 0, assignmentSetOverride);

				device.DevicePolicy = 4;
				device.DevicePriority = 0;
				device.AssignmentSetOverride = assignmentSetOverride;
			}

			if (msiChanged || affinityChanged)
			{
				if (!changedDevices.Contains(device))
					changedDevices.Add(device);
			}

			if (deviceType == DeviceType.NIC && device.DriverType == NicDriverType.NDIS && assignmentSetOverride != 0)
				DeviceHelper.SetRSS(device, assignmentSetOverride);
		}

		result.ChangedDevices = changedDevices;
		result.Success = changedDevices.Count > 0;
		result.NeedsRestart = changedDevices.Count > 0;

		return result;
	}
}
