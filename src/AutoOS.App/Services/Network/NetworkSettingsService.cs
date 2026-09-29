using AutoOS.App.Data.Contracts;
using AutoOS.Core.Data.Models.Device;
using AutoOS.Core.Data.Models.Network;
using AutoOS.Core.Helpers.Device;

namespace AutoOS.App.Services.Network;

public sealed class NetworkSettingsService : INetworkSettingsService
{
	public async Task<IReadOnlyList<DeviceInfo>> LoadAdaptersAsync() => await Task.Run(() =>
	{
		var adapters = DeviceHelper.GetDevices(DeviceType.NIC)
			.Where(device => device.NicType is NicDeviceType.WiFi or NicDeviceType.LAN).ToList();
		foreach (DeviceInfo adapter in adapters)
			adapter.AdvancedSettings = AutoOS.Core.Helpers.Network.NetworkHelper.GetAdvancedSettings(adapter);
		return adapters;
	});

	public IReadOnlyDictionary<Setting, string> GetOptimizedValues(DeviceInfo adapter) =>
		AutoOS.Core.Helpers.Network.NetworkHelper.GetOptimizedValues(adapter, adapter.AdvancedSettings);

	public async Task<bool> SaveChangesAsync(DeviceInfo adapter, IReadOnlyDictionary<Setting, string> changes) => await Task.Run(() =>
	{
		try
		{
			foreach ((Setting setting, string value) in changes)
				AutoOS.Core.Helpers.Network.NetworkHelper.SetAdvancedSetting(adapter, setting.Key, value);

			return DeviceHelper.RestartDevice(adapter);
		}
		catch
		{
			return false;
		}
	});
}
