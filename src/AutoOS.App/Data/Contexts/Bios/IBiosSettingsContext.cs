using AutoOS.Core.Data.Models.Bios;

namespace AutoOS.App.Data.Contexts;

public interface IBiosSettingsContext
{
	List<Setting>? LastSettings { get; set; }

	Dictionary<ushort, QidTarget>? LastQidMap { get; set; }

	List<BackupSetting>? LastBackupSettings { get; set; }
}
