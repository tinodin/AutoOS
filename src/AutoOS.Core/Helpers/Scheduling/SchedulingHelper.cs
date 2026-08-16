using AutoOS.Core.Helpers.CPU;
using AutoOS.Core.Data.Models.CPU;
using AutoOS.Core.Helpers.Device;
using AutoOS.Core.Data.Models.Device;

namespace AutoOS.Core.Helpers.Scheduling;

public static partial class SchedulingHelper
{
	public static async Task OptimizeAffinities(DeviceInfo? device = null, Action<DeviceType, string, DeviceInfo>? onDeviceUpdated = null)
	{
		CpuSetsInfo cpuSetsInfo = CpuHelper.GetCpuSets();
		(List<CpuCore>? pCores, List<CpuCore>? eCores) = CpuHelper.GroupCpuSetsByEfficiencyClass(cpuSetsInfo);

		if (pCores.Count < 4)
			return;

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

		List<DeviceInfo> audioDevices = (device == null || device.DeviceType == DeviceType.AudioController) ? [.. DeviceHelper.GetDevices(DeviceType.AudioController).Where(d => d.SupportsIrq)] : new List<DeviceInfo>();
		List<DeviceInfo> gpuDevices = (device == null || device.DeviceType == DeviceType.GPU) ? [.. DeviceHelper.GetDevices(DeviceType.GPU).Where(d => d.SupportsIrq)] : new List<DeviceInfo>();
		List<DeviceInfo> xhciDevices = (device == null || device.DeviceType == DeviceType.XHCI) ? [.. DeviceHelper.GetDevices(DeviceType.XHCI).Where(d => d.SupportsIrq)] : new List<DeviceInfo>();
		List<DeviceInfo> nicDevices = (device == null || device.DeviceType == DeviceType.NIC) ? [.. DeviceHelper.GetDevices(DeviceType.NIC).Where(d => d.SupportsIrq)] : new List<DeviceInfo>();

		if (device != null)
		{
			audioDevices = [.. audioDevices.Where(device => device.PnpDeviceId == device.PnpDeviceId)];
			gpuDevices = [.. gpuDevices.Where(device => device.PnpDeviceId == device.PnpDeviceId)];
			xhciDevices = [.. xhciDevices.Where(device => device.PnpDeviceId == device.PnpDeviceId)];
			nicDevices = [.. nicDevices.Where(device => device.PnpDeviceId == device.PnpDeviceId)];
		}

		var allChangedDevices = new List<(DeviceInfo device, DeviceType deviceType)>();

		if (audioDevices.Count > 0)
		{
			ApplyResult result = ApplyAffinityOnly(audioDevices, audioMask, DeviceType.AudioController);
			allChangedDevices.AddRange(result.ChangedDevices.Select(d => (d, DeviceType.AudioController)));
		}
		if (gpuDevices.Count > 0)
		{
			ApplyResult result = ApplyAffinityOnly(gpuDevices, gpuMask, DeviceType.GPU);
			allChangedDevices.AddRange(result.ChangedDevices.Select(d => (d, DeviceType.GPU)));
		}
		if (xhciDevices.Count > 0)
		{
			ApplyResult result = ApplyAffinityOnly(xhciDevices, xhciMask, DeviceType.XHCI);
			allChangedDevices.AddRange(result.ChangedDevices.Select(d => (d, DeviceType.XHCI)));
		}
		if (nicDevices.Count > 0)
		{
			ApplyResult result = ApplyAffinityOnly(nicDevices, nicMask, DeviceType.NIC);
			allChangedDevices.AddRange(result.ChangedDevices.Select(d => (d, DeviceType.NIC)));
		}

		if (allChangedDevices.Count > 0)
		{
			if (onDeviceUpdated != null)
			{
				foreach ((DeviceInfo? changedDevice, DeviceType deviceType) in allChangedDevices)
				{
					onDeviceUpdated(deviceType, changedDevice.PnpDeviceId, changedDevice);
				}
			}

			foreach (DeviceInfo changedDevice in allChangedDevices.Select(d => d.device))
			{
				await Task.Run(() => DeviceHelper.RestartDevice(changedDevice));
			}
		}
	}

	private static ulong GetCoreMask(CpuCore core) => core.Threads.Aggregate(0UL, (mask, t) => mask | t.BitMask);

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
