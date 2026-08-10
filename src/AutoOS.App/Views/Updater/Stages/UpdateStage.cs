using System.Diagnostics;
using AutoOS.Core.Helpers.Registry;
using Microsoft.Win32;

namespace AutoOS.App.Views.Updater.Stages;

public static class UpdateStage
{
	public static List<(string Title, Func<Task> Action, Func<bool>? Condition)> UpdateActions(UpdateDialog dialog)
	{
		var actions = new List<(string Title, Func<Task> Action, Func<bool>? Condition)>()
		{
			// remove startup delay
			("Removing Startup delay", async () => await Process.Start(new ProcessStartInfo { FileName = "reg.exe", Arguments = $@"load HKU\DefaultUser ""{Path.Combine(Path.GetPathRoot(Environment.SystemDirectory)!, "Users", "Default", "NTUSER.DAT")}""", CreateNoWindow = true })!.WaitForExitAsync(), null),
			("Removing Startup delay", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Serialize", "StartupDelayInMSec", 0, RegistryValueKind.DWord, true), null),
			("Removing Startup delay", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Serialize", "WaitForIdleState", 0, RegistryValueKind.DWord, true), null),
			("Removing Startup delay", async () => await Process.Start(new ProcessStartInfo { FileName = "reg.exe", Arguments = @"unload HKU\DefaultUser", CreateNoWindow = true })!.WaitForExitAsync(), null)
		};

		return actions;
	}
}
