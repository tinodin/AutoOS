using AutoOS.App.Data.Enums.Bios;
using AutoOS.Core.Data.Models.Bios;

namespace AutoOS.App.Data.Contracts;

public interface IBiosBackupService
{
	string BackupDirectory { get; }

	string? LastDriverError { get; }

	Task BackupAsync(List<Setting> settings);

	Task<PageMode> RestoreFromBackupAsync(string filePath);
}