using AutoOS.App.Data.Enums.Bios;
using AutoOS.Core.Data.Models.Bios;

namespace AutoOS.App.Data.Contracts.Bios;

public interface IBiosBackupService
{
	string BackupDirectory { get; }

	string? LastDriverError { get; }

	Task BackupAsync(List<Setting> settings);

	Task<(PageMode Result, IReadOnlyList<Setting> UnappliedSettings)> RestoreFromBackupAsync(string filePath);
}