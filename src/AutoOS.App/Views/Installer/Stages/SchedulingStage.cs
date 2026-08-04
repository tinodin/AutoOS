using AutoOS.Core.Helpers.Registry;
using AutoOS.Core.Helpers.Scheduling;
using Microsoft.Win32;

namespace AutoOS.App.Views.Installer.Stages;

public static class SchedulingStage
{
	public static IntPtr WindowHandle { get; private set; }
	public static List<(string Title, Func<Task> Action, Func<bool> Condition)> GetActions()
	{
		int PCores = PreparingStage.PCores;

		var actions = new List<(string Title, Func<Task> Action, Func<bool> Condition)>
		{
			// optimize affinities
			("Optimizing Affinities", async () => await Task.Delay(1000), () => PCores >= 4),
			("Optimizing Affinities", async () => await SchedulingHelper.OptimizeAffinities(), () => PCores >= 4),
			("Optimizing Affinities", async () => await Task.Delay(2000), () => PCores >= 4),

			// disable interrupt steering
			("Disabling interrupt steering", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\kernel", "InterruptSteeringFlags", 1, RegistryValueKind.DWord), null),
		
			// enable timer serialization
			("Enabling Timer Serialization", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\kernel", "SerializeTimerExpiration", 1, RegistryValueKind.DWord), null)
		};

		return actions;
	}
}

