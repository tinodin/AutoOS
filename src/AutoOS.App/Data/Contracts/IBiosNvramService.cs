using AutoOS.App.Data.Models.Bios;
using AutoOS.Core.Data.Models.Bios;
using AutoOS.Core.Helpers.Bios;

namespace AutoOS.App.Data.Contracts;

public interface IBiosNvramService
{
	void LoadCurrentValues(List<Setting> settings, Dictionary<ushort, QidTarget> qidMap, AmiSmmTransport? transport = null);

	bool PatchVariable(IEnumerable<KeyValuePair<Setting, SettingState>> settings, out byte[]? patched, out uint attributes, AmiSmmTransport? transport = null);

	bool TryGetCurrentBlob(Setting setting, out byte[]? blob, out uint attributes, AmiSmmTransport? transport = null);
}