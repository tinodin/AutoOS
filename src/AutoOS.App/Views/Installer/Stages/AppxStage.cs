using AutoOS.Common;
using AutoOS.Core.Helpers.Processes;
using AutoOS.Core.Helpers.Registry;
using AutoOS.Core.Helpers.Store;
using AutoOS.Core.Helpers.TaskScheduler;
using System.Diagnostics;

namespace AutoOS.Views.Installer.Stages;

public static class AppxStage
{
	public static List<(string Title, Func<Task> Action, Func<bool> Condition)> GetActions()
	{
		var actions = new List<(string Title, Func<Task> Action, Func<bool> Condition)>
		{
			// onedrive
			("Uninstalling OneDrive", async () => { foreach (Process process in new[] { "OneDrive", "OneDrive.Sync.Service", "UserOOBEBroker", "FileCoAuth", "OneDrivePatcher" }.SelectMany(Process.GetProcessesByName)) { process.Kill(); process.WaitForExit(); }}, null),
			("Uninstalling OneDrive", async () => await Process.Start(new ProcessStartInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "OneDriveSetup.exe"), "/uninstall") { CreateNoWindow = true })!.WaitForExitAsync(), null),
			("Uninstalling OneDrive", async () => await Task.Delay(2000), null),
			("Uninstalling OneDrive", async () => { var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Microsoft OneDrive"); foreach (var process in ProcessesHelper.GetLockingProcesses(path)) { process.Kill(); process.WaitForExit(); } Directory.Delete(path, true); }, null),
			("Uninstalling OneDrive", async () => { var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "OneDrive"); foreach (var process in ProcessesHelper.GetLockingProcesses(path)) { process.Kill(); process.WaitForExit(); } Directory.Delete(path, true); }, null),
			("Uninstalling OneDrive", async () => await RegistryHelper.RunAs(RegistryHelper.Identity.TrustedInstaller, async () => File.Move(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "OneDriveSetup.exe"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "OneDriveSetup.exee"))), null),
			("Uninstalling OneDrive", async () => TaskSchedulerHelper.Unregister("OneDrive Startup Task"), null)
		};

		// add uninstall actions
		var packagesToRemove = new List<string>
		{
			"Clipchamp.Clipchamp_yxz26nhyzhsrt",
			"Microsoft.BingNews_8wekyb3d8bbwe",
			"Microsoft.BingSearch_8wekyb3d8bbwe",
			"Microsoft.BingWeather_8wekyb3d8bbwe",
			"Microsoft.GetHelp_8wekyb3d8bbwe",
			"Microsoft.MicrosoftOfficeHub_8wekyb3d8bbwe",
			"Microsoft.MicrosoftSolitaireCollection_8wekyb3d8bbwe",
			"Microsoft.MicrosoftStickyNotes_8wekyb3d8bbwe",
			"Microsoft.OutlookForWindows_8wekyb3d8bbwe",
			"Microsoft.PowerAutomateDesktop_8wekyb3d8bbwe",
			"Microsoft.Todos_8wekyb3d8bbwe",
			"Microsoft.Windows.DevHome_8wekyb3d8bbwe",
			"Microsoft.WindowsFeedbackHub_8wekyb3d8bbwe",
			"Microsoft.WindowsTerminal_8wekyb3d8bbwe",
			"Microsoft.XboxSpeechToTextOverlay_8wekyb3d8bbwe",
			"Microsoft.YourPhone_8wekyb3d8bbwe",
			"MicrosoftCorporationII.MicrosoftFamily_8wekyb3d8bbwe",
			"MicrosoftCorporationII.QuickAssist_8wekyb3d8bbwe",
			"MSTeams_8wekyb3d8bbwe",
			"MicrosoftWindows.Client.WebExperience_cw5n1h2txyewy"
		};

		foreach (var package in packagesToRemove)
		{
			actions.Add(($"Deprovisioning {package}", async () => await StoreHelper.Deprovision(package), null));
			actions.Add(($"Uninstalling {package}", async () => await StoreHelper.Remove(package), null));
		}

		// add update actions
		var packagesToUpdate = new List<string>
		{
			"Microsoft.StorePurchaseApp_8wekyb3d8bbwe",
			"Microsoft.WindowsStore_8wekyb3d8bbwe",
			"Microsoft.DesktopAppInstaller_8wekyb3d8bbwe",
			"Microsoft.WindowsNotepad_8wekyb3d8bbwe",
			"Microsoft.WindowsCalculator_8wekyb3d8bbwe",
			"Microsoft.ZuneMusic_8wekyb3d8bbwe",
			"Microsoft.WindowsCamera_8wekyb3d8bbwe",
			"Microsoft.Windows.Photos_8wekyb3d8bbwe",
			"Microsoft.ScreenSketch_8wekyb3d8bbwe",
			"Microsoft.Paint_8wekyb3d8bbwe",
			"Microsoft.WindowsSoundRecorder_8wekyb3d8bbwe",
			"Microsoft.WindowsAlarms_8wekyb3d8bbwe",
			"MicrosoftWindows.CrossDevice_cw5n1h2txyewy",
			"Microsoft.XboxIdentityProvider_8wekyb3d8bbwe",
			"Microsoft.Xbox.TCUI_8wekyb3d8bbwe",
			"Microsoft.GamingApp_8wekyb3d8bbwe",
			"Microsoft.XboxGamingOverlay_8wekyb3d8bbwe",
			"Microsoft.ApplicationCompatibilityEnhancements_8wekyb3d8bbwe",
			"Microsoft.HEIFImageExtension_8wekyb3d8bbwe",
			"Microsoft.VP9VideoExtensions_8wekyb3d8bbwe",
			"Microsoft.WebMediaExtensions_8wekyb3d8bbwe",
			"Microsoft.WebpImageExtension_8wekyb3d8bbwe",
			"Microsoft.HEVCVideoExtension_8wekyb3d8bbwe",
			"Microsoft.RawImageExtension_8wekyb3d8bbwe",
			"Microsoft.MPEG2VideoExtension_8wekyb3d8bbwe",
			"Microsoft.AV1VideoExtension_8wekyb3d8bbwe",
			"Microsoft.AVCEncoderVideoExtension_8wekyb3d8bbwe"
		};

		foreach (var package in packagesToUpdate)
		{
			actions.Add(($"Updating {package}", async () => await StoreHelper.Update(package, new InstallPageReporter()), null));
		}

		return actions;
	}
}

