using AutoOS.Core.Data.Models.Device;
using AutoOS.Core.Data.Models.Network;

namespace AutoOS.App.Data.Contracts;

/// <summary>Reads adapter settings, prepares recommendations, and persists explicit saves.</summary>
public interface INetworkSettingsService
{
	Task<IReadOnlyList<DeviceInfo>> LoadAdaptersAsync();

	IReadOnlyDictionary<Setting, string> GetOptimizedValues(DeviceInfo adapter);

	Task<bool> SaveChangesAsync(DeviceInfo adapter, IReadOnlyDictionary<Setting, string> changes);
}
