using System.Diagnostics;
using AutoOS.App.Common;
using AutoOS.App.Views.Installer.Actions;
using AutoOS.Core.Helpers.Device;
using AutoOS.Core.Helpers.Device.Models;
using AutoOS.Core.Helpers.Network;
using AutoOS.Core.Helpers.Network.Models;
using AutoOS.Core.Helpers.Registry;
using Microsoft.Win32;

namespace AutoOS.App.Views.Installer.Stages;

public static class InternetStage
{
	public static List<(string Title, Func<Task> Action, Func<bool>? Condition)> GetActions()
	{
		bool Wifi = PreparingStage.Wifi;
		bool TxIntDelay = PreparingStage.TxIntDelay;

		var actions = new List<(string Title, Func<Task> Action, Func<bool>? Condition)>
		{
			// disable protocols
			("Disabling unnecessary protocols", async () => await ProcessActions.RunPowerShell(@"& { Get-NetAdapterBinding | Where-Object { $_.Enabled -eq $true -and $_.ComponentID -in 'ms_msclient','ms_server','ms_implat','ms_lldp','ms_lltdio','ms_rspndr' } | ForEach-Object { Disable-NetAdapterBinding -Name $_.InterfaceAlias -ComponentID $_.ComponentID } }"), null),

			// advanced tcp/ip settings -> wins
			(@"Setting NetBIOS setting to ""Disable NetBIOS over TCP/IP""", async () => await ProcessActions.RunPowerShell(@"Get-ChildItem 'HKLM:\SYSTEM\CurrentControlSet\Services\NetBT\Parameters\Interfaces' | ForEach-Object { Set-ItemProperty -Path $_.PSPath -Name 'NetbiosOptions' -Value 2 -Type DWord -Force }"), null),

			// advanced tcp/ip settings -> wins
			(@"Disabling ""Enable LMHOSTS lookup""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\NetBT\Parameters", "EnableLMHOSTS", 0, RegistryValueKind.DWord), null),

			// set txintdelay to 0
			("Setting TxIntDelay to 0", async () => DeviceHelper.GetDevices(DeviceType.NIC).Where(d => Registry.LocalMachine.OpenSubKey(d.RegistryPath)?.GetValue("TxIntDelay") != null).ToList().ForEach(d => Registry.LocalMachine.OpenSubKey(d.RegistryPath, true)?.SetValue("TxIntDelay", 0, RegistryValueKind.DWord)), () => TxIntDelay == true),

			// set "congestion control provider" to "bbr2"
			(@"Setting ""Congestion Control Provider"" to ""BBR2""", async () => await Process.Start(new ProcessStartInfo { FileName = "netsh", Arguments = "int tcp set supplemental internet congestionprovider=bbr2", UseShellExecute = false, CreateNoWindow = true })!.WaitForExitAsync(), null),
			
			// disable loopback large mtu
			(@"Disabling ""Loopback Large Mtu"" for IPv4", async () => await Process.Start(new ProcessStartInfo { FileName = "netsh", Arguments = "int ipv4 set gl loopbacklargemtu=disable", UseShellExecute = false, CreateNoWindow = true })!.WaitForExitAsync(), null),
			(@"Disabling ""Loopback Large Mtu"" for IPv6", async () => await Process.Start(new ProcessStartInfo { FileName = "netsh", Arguments = "int ipv6 set gl loopbacklargemtu=disable", UseShellExecute = false, CreateNoWindow = true })!.WaitForExitAsync(), null),

			// disable "receive side scaling" (rss)
			(@"Disabling ""Receive Side Scaling"" (RSS)", async () => await ProcessActions.RunPowerShell(@"Set-NetOffloadGlobalSetting -ReceiveSideScaling Disabled"), null),
			
			// disable "packet coalescing filter"
			(@"Disabling ""Packet Coalescing Filter""", async () => await ProcessActions.RunPowerShell(@"Set-NetOffloadGlobalSetting -PacketCoalescingFilter Disabled"), null)
		};

		foreach (DeviceInfo adapter in DeviceHelper.GetDevices(DeviceType.NIC).Where(d => d.NicType == NicDeviceType.WiFi || d.NicType == NicDeviceType.LAN).ToList())
		{
			actions.Add(($@"Optimizing advanced network adapter settings for {adapter.FriendlyName}", async () => await Task.Run(() => Core.Helpers.Network.NetworkHelper.OptimizeAdapter(adapter)), null));
			actions.Add(($@"Optimizing advanced network adapter settings for {adapter.FriendlyName}", async () => await Task.Delay(500), null));
			actions.Add((@"Restarting " + adapter.FriendlyName, async () => await Task.Run(() => DeviceHelper.RestartDevice(adapter)), null));

			if (adapter.IsActive)
				actions.Add(("Waiting for internet connection to reestablish", async () => await ProcessActions.RunConnectionCheck(), null));
		}

		return actions;
	}
}

