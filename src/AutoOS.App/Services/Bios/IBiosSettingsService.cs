using AutoOS.App.Data.Enums.Bios;
using AutoOS.App.Data.Models.Bios;

namespace AutoOS.App.Services.Bios;

public interface IBiosSettingsService
{
	string BackupDirectory { get; }

	Task<(PageMode State, IReadOnlyList<Setting> Settings)> LoadAsync();

	Task<PageMode> ImportToNvramAsync(IEnumerable<KeyValuePair<Setting, State>> modifiedSettings);

	Task<PageMode> RestoreFromBackupAsync(string filePath);
}
