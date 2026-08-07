using AutoOS.App.Views.Installer.Actions;
using AutoOS.Core.Helpers.Services;

namespace AutoOS.App.Views.Installer.Stages;

public static class OptionalFeatureStage
{
	public static List<(string Title, Func<Task> Action, Func<bool> Condition)> GetActions()
	{
		return
		[
			// disable optional features
			(@"Disabling ""WorkFolders-Client"" optional feature", async () => ServicesHelper.StopService("TiWorker"), null),
			(@"Disabling ""WorkFolders-Client"" optional feature", async () => ServicesHelper.StopService("TrustedInstaller"), null),
			(@"Disabling ""WorkFolders-Client"" optional feature", async () => await ProcessActions.RunPowerShell(@"Disable-WindowsOptionalFeature -FeatureName WorkFolders-Client -Online -NoRestart -ErrorAction Stop"), null),
			(@"Disabling ""WCF-Services45"" optional feature", async () => await ProcessActions.RunPowerShell(@"Disable-WindowsOptionalFeature -FeatureName WCF-Services45 -Online -NoRestart -ErrorAction Stop"), null),
			(@"Disabling ""WCF-TCP-PortSharing45"" optional feature", async () => await ProcessActions.RunPowerShell(@"Disable-WindowsOptionalFeature -FeatureName WCF-TCP-PortSharing45 -Online -NoRestart -ErrorAction Stop"), null),
			(@"Disabling ""SmbDirect"" optional feature", async () => await ProcessActions.RunPowerShell(@"Disable-WindowsOptionalFeature -FeatureName SmbDirect -Online -NoRestart -ErrorAction Stop"), null),

			// remove capabilities 
			(@"Removing ""App.StepsRecorder"" capability", async () => await ProcessActions.RunPowerShell(@"Remove-WindowsCapability -Online -Name (Get-WindowsCapability -Online | Where Name -like ""App.StepsRecorder*"").Name"), null),
			(@"Removing ""Browser.InternetExplorer"" capability", async () => await ProcessActions.RunPowerShell(@"Remove-WindowsCapability -Online -Name (Get-WindowsCapability -Online | Where Name -like ""Browser.InternetExplorer*"").Name"), null),
			(@"Removing ""Microsoft.Windows.PowerShell.ISE"" capability", async () => await ProcessActions.RunPowerShell(@"Remove-WindowsCapability -Online -Name (Get-WindowsCapability -Online | Where Name -like ""Microsoft.Windows.PowerShell.ISE*"").Name"), null),
		];
	}
}

