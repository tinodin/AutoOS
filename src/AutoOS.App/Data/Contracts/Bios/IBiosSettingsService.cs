using AutoOS.App.Data.Enums.Bios;
using AutoOS.App.Data.Models.Bios;
using AutoOS.Core.Data.Models.Bios;

namespace AutoOS.App.Data.Contracts.Bios;

public interface IBiosSettingsService
{
	string? LastDriverError { get; }

	Task<(PageMode Result, IReadOnlyList<Setting> Settings)> ReadFromNvramAsync();

	Task<(PageMode Result, IReadOnlyList<Setting> FailedSettings)> WriteToNvramAsync(IEnumerable<KeyValuePair<Setting, SettingState>> modifiedSettings);

	Task<(PageMode Result, IReadOnlyList<Setting> UnappliedSettings)> RestoreFromBackupAsync(string filePath);
}
