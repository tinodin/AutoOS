using AutoOS.Core.Data.Models.Bios;

namespace AutoOS.App.Data.Contexts;

public sealed class BiosSettingsContext : IBiosSettingsContext
{
	public List<Setting>? LastSettings { get; set; }

	public Dictionary<ushort, QidTarget>? LastQidMap { get; set; }

	public List<BackupSetting>? LastBackupSettings { get; set; }
}
