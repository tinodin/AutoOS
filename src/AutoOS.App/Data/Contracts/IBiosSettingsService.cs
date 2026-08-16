using AutoOS.App.Data.Enums.Bios;
using AutoOS.App.Data.Models.Bios;
using AutoOS.Core.Data.Models.Bios;

namespace AutoOS.App.Data.Contracts;

public interface IBiosSettingsService
{
	Task<(PageMode Result, IReadOnlyList<Setting> Settings)> ReadFromNvramAsync();

	Task<(PageMode Result, IReadOnlyList<Setting> FailedSettings)> WriteToNvramAsync(IEnumerable<KeyValuePair<Setting, SettingState>> modifiedSettings);

	Task<PageMode> RestoreFromBackupAsync(string filePath);
}
