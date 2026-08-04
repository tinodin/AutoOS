using System.Diagnostics;
using AutoOS.Core.Helpers.Registry;
using AutoOS.App.Views.Installer.Actions;
using Microsoft.Win32;

namespace AutoOS.App.Views.Installer.Stages;

public static class CleanupStage
{
	public static List<(string Title, Func<Task> Action, Func<bool> Condition)> GetActions()
	{
		var actions = new List<(string Title, Func<Task> Action, Func<bool> Condition)>
		{
			// clean temp directories
			("Cleaning temp directories", async () => await Task.WhenAll(Process.GetProcessesByName("TiWorker").Select(async process => { process.Kill(); await process.WaitForExitAsync(); })), null),
			("Cleaning temp directories", async () => await RegistryHelper.RunAs(RegistryHelper.Identity.TrustedInstaller, async () => ProcessActions.CleanDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Logs"))), null),
			("Cleaning temp directories", async () => await RegistryHelper.RunAs(RegistryHelper.Identity.TrustedInstaller, async () => ProcessActions.CleanDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Panther"))), null),
			("Cleaning temp directories", async () => await RegistryHelper.RunAs(RegistryHelper.Identity.TrustedInstaller, async () => ProcessActions.CleanDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "LogFiles"))), null),
			("Cleaning temp directories", async () => await RegistryHelper.RunAs(RegistryHelper.Identity.TrustedInstaller, async () => ProcessActions.CleanDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "SleepStudy"))), null),
			("Cleaning temp directories", async () => await RegistryHelper.RunAs(RegistryHelper.Identity.TrustedInstaller, async () => ProcessActions.CleanDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "sru"))), null),
			("Cleaning temp directories", async () => await RegistryHelper.RunAs(RegistryHelper.Identity.TrustedInstaller, async () => ProcessActions.CleanDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "WDI"))), null),
			("Cleaning temp directories", async () => await RegistryHelper.RunAs(RegistryHelper.Identity.TrustedInstaller, async () => ProcessActions.CleanDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "winevt", "Logs"))), null),
			("Cleaning temp directories", async () => await RegistryHelper.RunAs(RegistryHelper.Identity.TrustedInstaller, async () => ProcessActions.CleanDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "SystemTemp"))), null),
			("Cleaning temp directories", async () => await RegistryHelper.RunAs(RegistryHelper.Identity.TrustedInstaller, async () => ProcessActions.CleanDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Temp"))), null),
			("Cleaning temp directories", async () => ProcessActions.CleanDirectory(Path.GetTempPath()), null),
			("Cleaning temp directories", async () => File.Delete(Path.Combine(Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.System)), "DumpStack.log")), null),

			// run disk cleanup
			("Running disk cleanup", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Active Setup Temp Folders", "StateFlags0000", 2, RegistryValueKind.DWord), null),
			("Running disk cleanup", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\BranchCache", "StateFlags0000", 2, RegistryValueKind.DWord), null),
			("Running disk cleanup", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Content Indexer Cleaner", "StateFlags0000", 2, RegistryValueKind.DWord), null),
			("Running disk cleanup", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Delivery Optimization Files", "StateFlags0000", 2, RegistryValueKind.DWord), null),
			("Running disk cleanup", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Device Driver Packages", "StateFlags0000", 2, RegistryValueKind.DWord), null),
			("Running disk cleanup", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Diagnostic Data Viewer database files", "StateFlags0000", 2, RegistryValueKind.DWord), null),
			("Running disk cleanup", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Downloaded Program Files", "StateFlags0000", 2, RegistryValueKind.DWord), null),
			("Running disk cleanup", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Feedback Hub Archive log files", "StateFlags0000", 2, RegistryValueKind.DWord), null),
			("Running disk cleanup", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Internet Cache Files", "StateFlags0000", 2, RegistryValueKind.DWord), null),
			("Running disk cleanup", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Language Pack", "StateFlags0000", 2, RegistryValueKind.DWord), null),
			("Running disk cleanup", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Offline Pages Files", "StateFlags0000", 2, RegistryValueKind.DWord), null),
			("Running disk cleanup", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Old ChkDsk Files", "StateFlags0000", 2, RegistryValueKind.DWord), null),
			("Running disk cleanup", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\RetailDemo Offline Content", "StateFlags0000", 2, RegistryValueKind.DWord), null),
			("Running disk cleanup", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Setup Log Files", "StateFlags0000", 2, RegistryValueKind.DWord), null),
			("Running disk cleanup", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\System error memory dump files", "StateFlags0000", 2, RegistryValueKind.DWord), null),
			("Running disk cleanup", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\System error minidump files", "StateFlags0000", 2, RegistryValueKind.DWord), null),
			("Running disk cleanup", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Temporary Files", "StateFlags0000", 2, RegistryValueKind.DWord), null),
			("Running disk cleanup", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Temporary Setup Files", "StateFlags0000", 2, RegistryValueKind.DWord), null),
			("Running disk cleanup", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Thumbnail Cache", "StateFlags0000", 2, RegistryValueKind.DWord), null),
			("Running disk cleanup", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Update Cleanup", "StateFlags0000", 2, RegistryValueKind.DWord), null),
			("Running disk cleanup", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Upgrade Discarded Files", "StateFlags0000", 2, RegistryValueKind.DWord), null),
			("Running disk cleanup", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\User file versions", "StateFlags0000", 2, RegistryValueKind.DWord), null),
			("Running disk cleanup", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Windows Defender", "StateFlags0000", 2, RegistryValueKind.DWord), null),
			("Running disk cleanup", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Windows Error Reporting Files", "StateFlags0000", 2, RegistryValueKind.DWord), null),
			("Running disk cleanup", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Windows ESD installation files", "StateFlags0000", 2, RegistryValueKind.DWord), null),
			("Running disk cleanup", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Windows Reset Log Files", "StateFlags0000", 2, RegistryValueKind.DWord), null),
			("Running disk cleanup", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Windows Upgrade Log Files", "StateFlags0000", 2, RegistryValueKind.DWord), null),
			("Running disk cleanup", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "cleanmgr.exe"), Arguments = "/sagerun:0", UseShellExecute = false, CreateNoWindow = true })!.WaitForExitAsync(), null),
		
			// enable system restore
			("Enabling system restore", async () => await ProcessActions.RunPowerShell($"Enable-ComputerRestore -Drive '{Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.System))}'"), null),
			("Enabling system restore", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "vssadmin.exe"), Arguments = $"resize shadowstorage /for={Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.System)).TrimEnd('\\')} /on={Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.System)).TrimEnd('\\')} /maxsize=10%", UseShellExecute = false, CreateNoWindow = true })!.WaitForExitAsync(), null),
			("Enabling system restore", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\SystemRestore", "SystemRestorePointCreationFrequency", 0, RegistryValueKind.DWord), null),

			// create a restore point
			("Creating a restore point", async () => await ProcessActions.RunPowerShell(@"Checkpoint-Computer -Description AutoOS -RestorePointType MODIFY_SETTINGS"), null)
		};

		return actions;
	}
}

