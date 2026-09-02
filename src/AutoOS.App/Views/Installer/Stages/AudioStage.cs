using AutoOS.Core.Data.Models.Device;
using AutoOS.Core.Helpers.Device;
using AutoOS.Core.Helpers.Download;
using AutoOS.Core.Helpers.Extract;
using AutoOS.Core.Helpers.Processes;
using AutoOS.Core.Helpers.Registry;
using AutoOS.Core.Helpers.Services;
using Microsoft.Win32;

namespace AutoOS.App.Views.Installer.Stages;

public static class AudioStage
{
	public static List<(string Title, Func<Task> Action, Func<bool>? Condition)> GetActions()
	{
		bool NetAdapterCx = PreparingStage.NetAdapterCx;

		return
		[
			// disable audio enhancements
			("Disabling audio enhancements", async () => await RegistryHelper.RunAs(RegistryHelper.Identity.TrustedInstaller, new ProcessStartInfo("cmd.exe", @"/c powershell -Command ""$Keys = @('HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\MMDevices\Audio\Render'); foreach ($Key in $Keys) { Get-ChildItem $Key -Recurse | Where-Object { $_.PSPath -match '\\FxProperties$' } | ForEach-Object { Set-ItemProperty -Path $_.PSPath -Name '{1da5d803-d492-4edd-8c23-e0c0ffee7f0e},5' -Value 1 } }""") { CreateNoWindow = true }), null),
			("Disabling audio enhancements", async () => await RegistryHelper.RunAs(RegistryHelper.Identity.TrustedInstaller, new ProcessStartInfo("cmd.exe", @"/c powershell -Command ""$Keys = @('HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\MMDevices\Audio\Capture'); foreach ($Key in $Keys) { Get-ChildItem $Key -Recurse | Where-Object { $_.PSPath -match '\\FxProperties$' } | ForEach-Object { Set-ItemProperty -Path $_.PSPath -Name '{1da5d803-d492-4edd-8c23-e0c0ffee7f0e},5' -Value 1 } }""") { CreateNoWindow = true }), null),

			// disable audio idle states
			("Disabling audio idle states", async () => DeviceHelper.GetDevices(DeviceType.HDAUD).Select(device => Registry.LocalMachine.OpenSubKey($@"{device.RegistryPath}\PowerSettings", true)).Where(key => key != null).ToList().ForEach(key => { key?.SetValue("PerformanceIdleTime", new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }, RegistryValueKind.Binary); key?.Dispose(); }), null),

			// optimize multimedia class scheduler service (mmcss)
			("Optimizing Multimedia Class Scheduler Service (MMCSS)", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", "NoLazyMode", 0, RegistryValueKind.DWord), null),
			("Optimizing Multimedia Class Scheduler Service (MMCSS)", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", "NetworkThrottlingIndex", 10, RegistryValueKind.DWord), null),
			("Optimizing Multimedia Class Scheduler Service (MMCSS)", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", "LazyModeTimeout", 4294967295, RegistryValueKind.DWord), null),
			("Optimizing Multimedia Class Scheduler Service (MMCSS)", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", "SchedulerPeriod", 1000000, RegistryValueKind.DWord), null),
			("Optimizing Multimedia Class Scheduler Service (MMCSS)", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", "IdleDetectionCycles", 1, RegistryValueKind.DWord), null),
			("Optimizing Multimedia Class Scheduler Service (MMCSS)", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", "SchedulerTimerResolution", 1, RegistryValueKind.DWord), null),
			("Optimizing Multimedia Class Scheduler Service (MMCSS)", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Audio", "Priority", 1, RegistryValueKind.DWord), null),
			("Optimizing Multimedia Class Scheduler Service (MMCSS)", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Audio", "Scheduling Category", "High", RegistryValueKind.String), null),
			("Optimizing Multimedia Class Scheduler Service (MMCSS)", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Audio", "Priority When Yielded", 13, RegistryValueKind.DWord), null),
			("Optimizing Multimedia Class Scheduler Service (MMCSS)", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Pro Audio", "Priority", 1, RegistryValueKind.DWord), null),
			("Optimizing Multimedia Class Scheduler Service (MMCSS)", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Pro Audio", "Scheduling Category", "High", RegistryValueKind.String), null),
			("Optimizing Multimedia Class Scheduler Service (MMCSS)", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Pro Audio", "Priority When Yielded", 13, RegistryValueKind.DWord), null),
			("Optimizing Multimedia Class Scheduler Service (MMCSS)", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Playback", "Priority", 1, RegistryValueKind.DWord), null),
			("Optimizing Multimedia Class Scheduler Service (MMCSS)", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Playback", "Scheduling Category", "High", RegistryValueKind.String), null),
			("Optimizing Multimedia Class Scheduler Service (MMCSS)", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Playback", "Priority When Yielded", 13, RegistryValueKind.DWord), null),

			// disable multimedia class scheduler service (mmcss)
			("Disabling Multimedia Class Scheduler Service (MMCSS)", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\MMCSS", "Start", 4, RegistryValueKind.DWord), () => NetAdapterCx == true),

			// download dolby ac-3 feature on demand
			("Downloading Dolby AC-3 Feature on Demand", async () => await DownloadHelper.Download("https://raw.githubusercontent.com/tinodin/AutoOS-Resources/main/Files/Dolby/Dolby-AC-3-FoD.zip", Path.GetTempPath(), "Dolby-AC-3-FoD.zip"), null),

			// install dolby ac-3 feature on demand
			("Installing Dolby AC-3 Feature on Demand", async () => await ExtractHelper.Extract(Path.Combine(Path.GetTempPath(), "Dolby-AC-3-FoD.zip"), Path.Combine(Path.GetTempPath(), "Dolby-AC-3-FoD")), null),
			("Installing Dolby AC-3 Feature on Demand", async () => ServicesHelper.StopService("TiWorker"), null),
			("Installing Dolby AC-3 Feature on Demand", async () => ServicesHelper.StopService("TrustedInstaller"), null),
			("Installing Dolby AC-3 Feature on Demand", async () => await Process.Start(new ProcessStartInfo { FileName = "dism.exe", Arguments = $@"/online /Add-Package /PackagePath:""{Path.Combine(Path.GetTempPath(), @"Dolby-AC-3-FoD\update.mum")}"" /norestart", UseShellExecute = false, CreateNoWindow = true })!.WaitForExitAsync(), null),
			("Cleaning up Dolby AC-3 Feature on Demand files", async () => { string path = Path.Combine(Path.GetTempPath(), "Dolby-AC-3-FoD.zip"); foreach (Process process in ProcessesHelper.GetLockingProcesses(path)) { process.Kill(); process.WaitForExit(); } RegistryHelper.RunAs(RegistryHelper.Identity.TrustedInstaller, () => File.Delete(path)); }, null),
			("Cleaning up Dolby AC-3 Feature on Demand files", async () => { string path = Path.Combine(Path.GetTempPath(), "Dolby-AC-3-FoD"); foreach (Process process in ProcessesHelper.GetLockingProcesses(path)) { process.Kill(); process.WaitForExit(); } RegistryHelper.RunAs(RegistryHelper.Identity.TrustedInstaller, () => Directory.Delete(path, true)); }, null)
		];
	}
}

