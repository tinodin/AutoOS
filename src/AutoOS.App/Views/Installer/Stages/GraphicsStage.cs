using AutoOS.Common;
using AutoOS.Core.Helpers.Download;
using AutoOS.Core.Helpers.Extract;
using AutoOS.Core.Helpers.GPU;
using AutoOS.Core.Helpers.Monitor;
using AutoOS.Core.Helpers.Registry;
using AutoOS.Core.Helpers.Shortcut;
using Microsoft.VisualBasic.FileIO;
using Microsoft.Win32;
using System.Diagnostics;
using System.Text.Json;
using Windows.Storage;

namespace AutoOS.Views.Installer.Stages;

public static class GraphicsStage
{
	private static readonly ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
	public static async Task<List<(string Title, Func<Task> Action, Func<bool> Condition)>> GetActions()
	{
		var GPUs = PreparingStage.GPUs;
		bool MSI = PreparingStage.MSI;
		bool CRU = PreparingStage.CRU;
		bool ImportMonitorConfig = PreparingStage.ImportMonitorConfig;
		bool NVIDIA = GPUs.Any(gpu => gpu.VendorId == "10de");
		bool AMD = GPUs.Any(gpu => gpu.VendorId == "1002");
		bool INTEL = GPUs.Any(gpu => gpu.VendorId == "8086");

		InIHelper iniHelper = new(Path.Combine(Path.GetTempPath(), "obs-studio", "basic", "profiles", "Untitled", "basic.ini"));
		string obsVersion = "";

		var actions = new List<(string Title, Func<Task> Action, Func<bool> Condition)>
		{
			// apply custom resolution utility (cru) profile
			("Importing Custom Resolution Utility (CRU) profile", async () => await Task.Delay(1500), () => CRU == true),
			("Importing Custom Resolution Utility (CRU) profile", async () => await Process.Start(new ProcessStartInfo { FileName = localSettings.Values["CruProfile"]?.ToString(), Arguments = "-i", WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), () => CRU == true),
			("Applying Custom Resolution Utility (CRU) profile", async () => await Task.Delay(1500), () => CRU == true),
			("Applying Custom Resolution Utility (CRU) profile", async () => await Process.Start(new ProcessStartInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "CRU", "restart64.exe"), "/q") { CreateNoWindow = true })!.WaitForExitAsync(), () => CRU == true),
			("Applying Custom Resolution Utility (CRU) profile", async () => await Task.Delay(2000), () => CRU == true),

			// import old monitor layout and settings
			("Importing old monitor layout and settings", async () => await Task.Delay(1500), () => ImportMonitorConfig == true),
			("Importing old monitor layout and settings", async () => await MonitorHelper.ImportMonitorConfig(), () => ImportMonitorConfig == true),
			("Importing old monitor layout and settings", async () => await Process.Start(new ProcessStartInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "CRU", "restart64.exe"), "/q") { CreateNoWindow = true })!.WaitForExitAsync(), () => ImportMonitorConfig == true),
			("Importing old monitor layout and settings", async () => await Task.Delay(2000), () => ImportMonitorConfig == true),

			// set the highest supported refresh rate for every monitor
			("Setting the highest supported refresh rate for every monitor", async () => await Task.Delay(1000), null),
			("Setting the highest supported refresh rate for every monitor", async () => MonitorHelper.SetHighestRefreshRates(), null),
			("Setting the highest supported refresh rate for every monitor", async () => await Task.Delay(3000), null),

			// download msi afterburner
			("Downloading MSI Afterburner", async () => await DownloadHelper.Download("https://github.com/tinodin/AutoOS-Resources/releases/download/v1.0.0.0/MSI.Afterburner.zip", Path.GetTempPath(), "MSI Afterburner.zip", new InstallPageReporter()), null),

			// install msi afterburner
			("Installing MSI Afterburner", async () => await ExtractHelper.Extract(Path.Combine(Path.GetTempPath(), "MSI Afterburner.zip"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "MSI Afterburner")), null),
			("Installing MSI Afterburner", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "MSI Afterburner", "Redist", "vc_redist.x86.exe"), Arguments = "/q", WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), null),
			("Installing MSI Afterburner", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Afterburner", "DisplayIcon", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "MSI Afterburner", "uninstall.exe"), RegistryValueKind.String), null),
			("Installing MSI Afterburner", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Afterburner", "DisplayName", "MSI Afterburner 4.6.6", RegistryValueKind.String), null),
			("Installing MSI Afterburner", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Afterburner", "DisplayVersion", "4.6.6", RegistryValueKind.String), null),
			("Installing MSI Afterburner", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Afterburner", "Publisher", "MSI Co., LTD", RegistryValueKind.String), null),
			("Installing MSI Afterburner", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Afterburner", "UninstallString", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "MSI Afterburner", "uninstall.exe"), RegistryValueKind.String), null),
			("Installing MSI Afterburner", async () => Directory.CreateDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft", "Windows", "Start Menu", "Programs", "MSI Afterburner")), null),
			("Installing MSI Afterburner", async () => Directory.CreateDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft", "Windows", "Start Menu", "Programs", "MSI Afterburner", "SDK")), null),
			("Installing MSI Afterburner", async () => ShortcutHelper.Create(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft", "Windows", "Start Menu", "Programs", "MSI Afterburner", "MSI Afterburner.lnk"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "MSI Afterburner", "MSIAfterburner.exe")), null),
			("Installing MSI Afterburner", async () => ShortcutHelper.Create(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft", "Windows", "Start Menu", "Programs", "MSI Afterburner", "ReadMe.lnk"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "MSI Afterburner", "Doc", "ReadMe.pdf")), null),
			("Installing MSI Afterburner", async () => ShortcutHelper.Create(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft", "Windows", "Start Menu", "Programs", "MSI Afterburner", "Uninstall.lnk"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "MSI Afterburner", "Uninstall.exe")), null),
			("Installing MSI Afterburner", async () => ShortcutHelper.Create(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft", "Windows", "Start Menu", "Programs", "MSI Afterburner", "SDK", "MSI Afterburner localization reference.lnk"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "MSI Afterburner", "SDK", "Doc", "Localization reference.pdf")), null),
			("Installing MSI Afterburner", async () => ShortcutHelper.Create(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft", "Windows", "Start Menu", "Programs", "MSI Afterburner", "SDK", "MSI Afterburner skin format reference.lnk"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "MSI Afterburner", "SDK", "Doc", "USF skin format reference.pdf")), null),
			("Installing MSI Afterburner", async () => ShortcutHelper.Create(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft", "Windows", "Start Menu", "Programs", "MSI Afterburner", "SDK", "Samples.lnk"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "MSI Afterburner", "SDK", "Samples")), null),
			("Installing MSI Afterburner", async () => Directory.CreateDirectory(Path.Combine(Path.GetPathRoot(Environment.SystemDirectory)!, "Users", "Default", "AppData", "Roaming", "Microsoft", "Windows", "Start Menu", "Programs", "MSI Afterburner")), null),
			("Installing MSI Afterburner", async () => Directory.CreateDirectory(Path.Combine(Path.GetPathRoot(Environment.SystemDirectory)!, "Users", "Default", "AppData", "Roaming", "Microsoft", "Windows", "Start Menu", "Programs", "MSI Afterburner", "SDK")), null),
			("Installing MSI Afterburner", async () => ShortcutHelper.Create(Path.Combine(Path.GetPathRoot(Environment.SystemDirectory)!, "Users", "Default", "AppData", "Roaming", "Microsoft", "Windows", "Start Menu", "Programs", "MSI Afterburner", "MSI Afterburner.lnk"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "MSI Afterburner", "MSIAfterburner.exe")), null),
			("Installing MSI Afterburner", async () => ShortcutHelper.Create(Path.Combine(Path.GetPathRoot(Environment.SystemDirectory)!, "Users", "Default", "AppData", "Roaming", "Microsoft", "Windows", "Start Menu", "Programs", "MSI Afterburner", "ReadMe.lnk"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "MSI Afterburner", "Doc", "ReadMe.pdf")), null),
			("Installing MSI Afterburner", async () => ShortcutHelper.Create(Path.Combine(Path.GetPathRoot(Environment.SystemDirectory)!, "Users", "Default", "AppData", "Roaming", "Microsoft", "Windows", "Start Menu", "Programs", "MSI Afterburner", "Uninstall.lnk"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "MSI Afterburner", "Uninstall.exe")), null),
			("Installing MSI Afterburner", async () => ShortcutHelper.Create(Path.Combine(Path.GetPathRoot(Environment.SystemDirectory)!, "Users", "Default", "AppData", "Roaming", "Microsoft", "Windows", "Start Menu", "Programs", "MSI Afterburner", "SDK", "MSI Afterburner localization reference.lnk"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "MSI Afterburner", "SDK", "Doc", "Localization reference.pdf")), null),
			("Installing MSI Afterburner", async () => ShortcutHelper.Create(Path.Combine(Path.GetPathRoot(Environment.SystemDirectory)!, "Users", "Default", "AppData", "Roaming", "Microsoft", "Windows", "Start Menu", "Programs", "MSI Afterburner", "SDK", "MSI Afterburner skin format reference.lnk"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "MSI Afterburner", "SDK", "Doc", "USF skin format reference.pdf")), null),
			("Installing MSI Afterburner", async () => ShortcutHelper.Create(Path.Combine(Path.GetPathRoot(Environment.SystemDirectory)!, "Users", "Default", "AppData", "Roaming", "Microsoft", "Windows", "Start Menu", "Programs", "MSI Afterburner", "SDK", "Samples.lnk"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "MSI Afterburner", "SDK", "Samples")), null),
			("Cleaning up MSI Afterburner files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "MSI Afterburner.zip")), null),

			// import msi afterburner profile
			("Importing MSI Afterburner profile", async () => File.Copy(localSettings.Values["MsiProfile"]?.ToString(), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "MSI Afterburner", "Profiles", Path.GetFileName(localSettings.Values["MsiProfile"]?.ToString()))), () => MSI == true),

			// apply msi afterburner profile
			("Applying MSI Afterburner profile", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "MSI Afterburner", "MSIAfterburner.exe"), Arguments = "/Profile1 /q", WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), () => MSI == true),
		
			// download obs studio
			("Downloading OBS Studio", async () => await DownloadHelper.Download(JsonDocument.Parse(await new HttpClient { DefaultRequestHeaders = { { "User-Agent", "AutoOS" } } }.GetStringAsync("https://api.github.com/repos/obsproject/obs-studio/releases/latest")).RootElement.GetProperty("assets").EnumerateArray().First(a => a.GetProperty("name").GetString().Contains("Windows-x64-Installer.exe")).GetProperty("browser_download_url").GetString(), Path.GetTempPath(), "OBS-Studio-Windows-x64-Installer.exe", new InstallPageReporter()), null),
			("Downloading OBS Studio", async () => await DownloadHelper.Download("https://raw.githubusercontent.com/tinodin/AutoOS-Resources/main/Files/OBS/obs-studio.zip", Path.GetTempPath(), "obs-studio.zip", new InstallPageReporter()), null),
			("Downloading OBS Studio", async () => await DownloadHelper.Download("https://raw.githubusercontent.com/tinodin/AutoOS-Resources/main/Files/OBS/uninstall.exe", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "obs-studio"), "uninstall.exe", new InstallPageReporter()), null),

			// install obs studio
			("Installing OBS Studio", async () => await ExtractHelper.Extract(Path.Combine(Path.GetTempPath(), "OBS-Studio-Windows-x64-Installer.exe"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "obs-studio")), null),
			("Installing OBS Studio", async () => await ExtractHelper.Extract(Path.Combine(Path.GetTempPath(), "obs-studio.zip"), Path.Combine(Path.GetTempPath(), "obs-studio")), null),
			("Installing OBS Studio", async () => iniHelper.AddValue("Encoder", "obs_qsv11_v2", "AdvOut"), () => NVIDIA == false && INTEL == true),
			("Installing OBS Studio", async () => iniHelper.AddValue("RecEncoder", "obs_qsv11_v2", "AdvOut"), () => NVIDIA == false && INTEL == true),
			("Installing OBS Studio", async () => iniHelper.AddValue("Encoder", "h264_texture_amf", "AdvOut"), () => NVIDIA == false && AMD == true),
			("Installing OBS Studio", async () => iniHelper.AddValue("RecEncoder", "h264_texture_amf", "AdvOut"), () => NVIDIA == false &&  AMD == true),
			("Installing OBS Studio", async () => FileSystem.CopyDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "obs-studio", "$APPDATA", "obs-studio-hook"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "obs-studio-hook"), true), null),
			("Installing OBS Studio", async () => FileSystem.CopyDirectory(Path.Combine(Path.GetTempPath(), "obs-studio"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "obs-studio"), true), null),
			("Installing OBS Studio", async () => FileSystem.CopyDirectory(Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "obs-studio")), Path.Combine(Directory.GetParent(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile))!.FullName, "Default", "AppData", "Roaming", "obs-studio"), true), null),
			("Installing OBS Studio", async () => Directory.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "obs-studio", "$PLUGINSDIR"), true), null),
			("Installing OBS Studio", async () => Directory.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "obs-studio", "$APPDATA"), true), null),
			("Installing OBS Studio", async () => obsVersion = FileVersionInfo.GetVersionInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "obs-studio", "bin", "64bit", "obs64.exe")).ProductVersion, null),
			("Installing OBS Studio", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\OBS Studio", "DisplayVersion", obsVersion, RegistryValueKind.String), null),
			("Installing OBS Studio", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\OBS Studio", "DisplayIcon", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "obs-studio", "bin", "64bit", "obs64.exe"), RegistryValueKind.String), null),
			("Installing OBS Studio", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\OBS Studio", "DisplayName", "OBS Studio", RegistryValueKind.String), null),
			("Installing OBS Studio", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\OBS Studio", "HelpLink", "https://obsproject.com", RegistryValueKind.String), null),
			("Installing OBS Studio", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\OBS Studio", "ProductID", "d16d2409-3151-4331-a9b1-dfd8cf3f0d9c", RegistryValueKind.String), null),
			("Installing OBS Studio", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\OBS Studio", "Publisher", "OBS Project", RegistryValueKind.String), null),
			("Installing OBS Studio", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\OBS Studio", "UninstallString", @$"""{Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "obs-studio", "uninstall.exe")}""", RegistryValueKind.String), null),
			("Installing OBS Studio", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\OBS Studio", "QuietUninstallString", @$"""{Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "obs-studio", "uninstall.exe")}"" /S", RegistryValueKind.String), null),
			("Installing OBS Studio", async () => ShortcutHelper.Create(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Microsoft", "Windows", "Start Menu", "Programs", "OBS Studio.lnk"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "obs-studio", "bin", "64bit", "obs64.exe"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "obs-studio", "bin", "64bit")), null),
			("Cleaning up OBS Studio files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "OBS-Studio-Windows-x64-Installer.exe")), null),
			("Cleaning up OBS Studio files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "obs-studio.zip")), null),
			("Cleaning up OBS Studio files", async () => Directory.Delete(Path.Combine(Path.GetTempPath(), "obs-studio")), null)
		};

		var gpus = PreparingStage.GPUs.Where(gpu => gpu.Install).ToList();

		var latestDrivers = new Dictionary<string, (string Version, string Url)>();
		var driverInstallActions = new List<(string Title, Func<Task> Action, Func<bool> Condition)>();
		var driverTweakActions = new List<(string Title, Func<Task> Action, Func<bool> Condition)>();

		foreach (var gpu in gpus)
		{
			(string newestVersion, string newestDownloadUrl) = gpu.VendorId switch
			{
				"10de" => await NvidiaHelper.CheckUpdate(gpu),
				"1002" => await AmdHelper.CheckUpdate(gpu),
				"8086" => await IntelHelper.CheckUpdate(gpu),
				_ => ("", "")
			};

			if (!latestDrivers.TryGetValue(gpu.VendorId, out var driver) || driver.Version != newestVersion)
			{
				latestDrivers[gpu.VendorId] = (newestVersion, newestDownloadUrl);

				switch (gpu.VendorId)
				{
					case "10de":
						driverInstallActions.AddRange(NvidiaHelper.InstallActions(gpu, newestVersion, newestDownloadUrl, new InstallPageReporter()));
						break;
					case "1002":
						driverInstallActions.AddRange(AmdHelper.InstallActions(gpu, newestVersion, newestDownloadUrl, new InstallPageReporter()));
						break;
					case "8086":
						driverInstallActions.AddRange(IntelHelper.InstallActions(gpu, newestVersion, newestDownloadUrl, new InstallPageReporter()));
						break;
				}
			}

			switch (gpu.VendorId)
			{
				case "10de":
					driverTweakActions.AddRange(NvidiaHelper.TweakActions(gpu, newestVersion));
					break;
				case "1002":
					driverTweakActions.AddRange(AmdHelper.TweakActions(gpu));
					break;
				case "8086":
					driverTweakActions.AddRange(IntelHelper.TweakActions(gpu));
					break;
			}
		}

		return [.. actions.Take(12), .. driverInstallActions, .. driverTweakActions, .. actions.Skip(12)];
	}
}

