using AutoOS.Core.Data.Models.Device;
using AutoOS.Core.Data.Models.Network;

namespace AutoOS.App.Data.Contracts.Network;

public interface INetworkSettingsService
{
	Task<IReadOnlyList<DeviceInfo>> LoadAdaptersAsync();

	Task<bool> SaveChangesAsync(DeviceInfo adapter, IReadOnlyDictionary<Setting, string> changes);
}
