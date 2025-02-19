using AutoOS.Views.Installer.Actions;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace AutoOS.Views.Installer.Stages;

public static class NetworkStage
{
    public static async Task Run()
    {
        bool? AppleMusic = PreparingStage.AppleMusic;
        bool? WOL = PreparingStage.WOL;
        bool? Wifi = PreparingStage.Wifi;
        int? CoreCount = PreparingStage.CoreCount;
        bool? RSS = PreparingStage.RSS;
        bool? TxIntDelay = PreparingStage.TxIntDelay;

        InstallPage.Status.Text = "Configuring the Network Adapters...";

        int validActionsCount = 0;
        int stagePercentage = 5;

        var actions = new List<(Func<Task> Action, Func<bool> Condition)>
        {
            // set static ip (maybe same for all adapters)
            (async () => await ProcessActions.RunBatchScript("Setting a static ip", "static.bat", ""), null),
            (async () => await ProcessActions.Sleep("Setting a static ip", 500), null),

            // check connection
            (async () => await ProcessActions.RunConnectionCheck("Waiting for internet connection to reestablish"), null),

            // disable protocols
            (async () => await ProcessActions.RunPowerShell("Disabling unnecessary protocols", @"Get-NetAdapter | ForEach-Object { foreach ($item in $_ | Get-NetAdapterBinding -DisplayName '*') { if ($item.DisplayName -ne 'QoS Packet Scheduler' -and $item.DisplayName -ne 'Internet Protocol Version 4 (TCP/IPv4)') { Disable-NetAdapterBinding -Name $_.Name -ComponentID $item.ComponentID } } }"), () => AppleMusic == false),
            (async () => await ProcessActions.RunPowerShell("Disabling unnecessary protocols", @"Get-NetAdapter | ForEach-Object { foreach ($item in $_ | Get-NetAdapterBinding -DisplayName '*') { if ($item.DisplayName -ne 'QoS Packet Scheduler' -and $item.DisplayName -ne 'Internet Protocol Version 4 (TCP/IPv4)' -and $item.DisplayName -ne 'Internet Protocol Version 6 (TCP/IPv6)') { Disable-NetAdapterBinding -Name $_.Name -ComponentID $item.ComponentID } } }"), () => AppleMusic == true),

            // disable netbios over tcp
            (async () => await ProcessActions.RunNsudo("Disabling NetBIOS over TCP", "CurrentUser", @"cmd /c for %a in (NetbiosOptions) do for /f ""delims="" %b in ('reg query HKLM\SYSTEM\CurrentControlSet\Services\NetBT\Parameters\Interfaces /s /f %a ^| findstr HKEY') do reg add ""%b"" /v %a /t REG_DWORD /d 2 /f"), null),

            // adjust ethernet adapter advanced settings
            (async () => await ProcessActions.RunPowerShellScript("Adjusting Ethernet adapter advanced settings", "ethernet.ps1", ""), null),

            // check connection
            (async () => await ProcessActions.RunConnectionCheck("Waiting for internet connection to reestablish"), null),

            // adjust wifi adapter advanved settings
            (async () => await ProcessActions.RunPowerShellScript("Adjusting Wi-Fi adapter advanced settings", "wifi.ps1", ""), () => Wifi == true),

            // check connection
            (async () => await ProcessActions.RunConnectionCheck("Waiting for internet connection to reestablish"), () => Wifi == true),

            // disable power management settings
            (async () => await ProcessActions.RunPowerShellScript("Disabling power management settings", "networkpowermanagement.ps1", ""), null),

            // configure wake-on-lan
            (async () => await ProcessActions.RunPowerShellScript("Configuring Wake-On-Lan (WOL)", "wol.ps1", ""), () => WOL == true),

            // configure rss queues
            (async () => await ProcessActions.RunPowerShellScript("Configuring RSS queues", "rss.ps1", ""), () => RSS == true),
            (async () => await ProcessActions.RunNsudo("Disabling RSS", "TrustedInstaller", @"netsh int tcp set global rss=disabled"), () => RSS == false),

             // set txintdelay to 0
            (async () => await ProcessActions.RunPowerShellScript("Setting TxIntDelay to 0", "txintdelay.ps1", ""), () => TxIntDelay == true),

            // disable nagles algorithm
            (async () => await ProcessActions.RunPowerShell("Disabling Nagles Algorithm", @"Get-NetAdapter | ForEach-Object { New-ItemProperty -Path ""HKLM:\System\CurrentControlSet\Services\Tcpip\Parameters\Interfaces\$($_.InterfaceGuid)"" -Name ""TcpAckFrequency"" -PropertyType DWord -Value 1 -Force; New-ItemProperty -Path ""HKLM:\System\CurrentControlSet\Services\Tcpip\Parameters\Interfaces\$($_.InterfaceGuid)"" -Name ""TcpDelAckTicks"" -PropertyType DWord -Value 0 -Force; New-ItemProperty -Path ""HKLM:\System\CurrentControlSet\Services\Tcpip\Parameters\Interfaces\$($_.InterfaceGuid)"" -Name ""TCPNoDelay"" -PropertyType DWord -Value 1 -Force }"), null),

            // configure tcp/ip settings
            (async () => await ProcessActions.RunNsudo("Enabling task offload", "TrustedInstaller", @"netsh int ip set global taskoffload=enabled"), null),
            (async () => await ProcessActions.RunNsudo("Enabling WinSock autotuning", "TrustedInstaller", @"netsh winsock set autotuning on"), null),
            (async () => await ProcessActions.RunNsudo("Disabling RSC", "TrustedInstaller", @"netsh int tcp set global rsc=disabled"), null),
            (async () => await ProcessActions.RunNsudo("Setting neighbor cache limit", "TrustedInstaller", @"netsh int ip set global neighborcachelimit=4096"), null),
            (async () => await ProcessActions.RunNsudo("Setting route cache limit", "TrustedInstaller", @"netsh int ip set global routecachelimit=4096"), null),
            (async () => await ProcessActions.RunNsudo("Disabling source routing behavior", "TrustedInstaller", @"netsh int ip set global sourceroutingbehavior=drop"), null),
            (async () => await ProcessActions.RunNsudo("Disabling DHCP media sense", "TrustedInstaller", @"netsh int ip set global dhcpmediasense=disabled"), null),
            (async () => await ProcessActions.RunNsudo("Disabling media sense event log", "TrustedInstaller", @"netsh int ip set global mediasenseeventlog=disabled"), null),
            (async () => await ProcessActions.RunNsudo("Disabling MLD level", "TrustedInstaller", @"netsh int ip set global mldlevel=none"), null),
            (async () => await ProcessActions.RunNsudo("Enabling DCA", "TrustedInstaller", @"netsh int tcp set global dca=enabled"), null),
            (async () => await ProcessActions.RunNsudo("Disabling ECN Capability", "TrustedInstaller", @"netsh int tcp set global ecncapability=disabled"), null),
            (async () => await ProcessActions.RunNsudo("Disabling NetDMA", "TrustedInstaller", @"netsh int tcp set global netdma=disabled"), null),
            (async () => await ProcessActions.RunNsudo("Disabling Non-SACK RTT resiliency", "TrustedInstaller", @"netsh int tcp set global nonsackrttresiliency=disabled"), null),
            (async () => await ProcessActions.RunNsudo("Disabling TCP timestamps", "TrustedInstaller", @"netsh int tcp set global timestamps=disabled"), null),
            (async () => await ProcessActions.RunNsudo("Disabling TCP heuristics", "TrustedInstaller", @"netsh int tcp set heuristics disabled"), null),
            (async () => await ProcessActions.RunNsudo("Disabling WSH heuristics", "TrustedInstaller", @"netsh int tcp set heuristics wsh=disabled"), null),
            (async () => await ProcessActions.RunNsudo("Disabling security MPP", "TrustedInstaller", @"netsh int tcp set security mpp=disabled"), null),
            (async () => await ProcessActions.RunNsudo("Disabling security profiles", "TrustedInstaller", @"netsh int tcp set security profiles=disabled"), null),
            (async () => await ProcessActions.RunNsudo("Setting initial RTO", "TrustedInstaller", @"netsh int tcp set global initialRto=2000"), null),
            (async () => await ProcessActions.RunNsudo("Setting max SYN retransmissions", "TrustedInstaller", @"netsh int tcp set global maxsynretransmissions=2"), null),
            (async () => await ProcessActions.RunNsudo("Setting congestion provider to CTCP", "TrustedInstaller", @"netsh int tcp set supplemental internet congestionprovider=ctcp"), null),
            (async () => await ProcessActions.RunNsudo("Disabling teredo", "TrustedInstaller", @"netsh interface teredo set state disabled"), null),
            (async () => await ProcessActions.RunNsudo("Disabling ISATAP", "TrustedInstaller", @"netsh int isatap set state disabled"), null),
            (async () => await ProcessActions.RunNsudo("Disabling 6to4", "TrustedInstaller", @"netsh interface 6to4 set state disabled"), null),
            (async () => await ProcessActions.RunPowerShell("Disabling chimney offload", @"Set-NetOffloadGlobalSetting -Chimney Disabled"), null),
            (async () => await ProcessActions.RunPowerShell("Disabling packet coalescing filter", @"Set-NetOffloadGlobalSetting -PacketCoalescingFilter Disabled"), null),
            (async () => await ProcessActions.RunPowerShell("Disabling windows forced scaling",  @"Set-NetTCPSetting -SettingName ""*"" -ForceWS Disabled"), null),

            // enable qos policies outside of domain networks
            (async () => await ProcessActions.RunNsudo("Enabling QoS Policies outside of domain networks", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Tcpip\QoS"" /v ""Do not use NLA"" /t REG_SZ /d 1 /f"), null),

            // disable wifi services and drivers
            (async () => await ProcessActions.DisableWiFiServicesAndDrivers("Disabling Wi-Fi services and drivers"), () => Wifi == false),
        };

        foreach (var (action, condition) in actions)
        {
            if ((condition == null || condition.Invoke()))
            {
                validActionsCount++;
            }
        }

        double incrementPerAction = validActionsCount > 0 ? stagePercentage / (double)validActionsCount : 0;

        foreach (var (action, condition) in actions)
        {
            if ((condition == null || condition.Invoke()))
            {
                try
                {
                    await action();
                }
                catch (Exception ex)
                {
                    InstallPage.Info.Title = ex.Message;
                    InstallPage.Progress.ShowError = true;
                    InstallPage.Info.Severity = InfoBarSeverity.Error;
                    InstallPage.ProgressRingControl.Foreground = new SolidColorBrush(Color.FromArgb(255, 196, 43, 28));
                    break;
                }

                InstallPage.Progress.Value += incrementPerAction;

                if (InstallPage.Info.Title != ProcessActions.previousTitle)
                {
                    await Task.Delay(75);
                }
            }
        }
    }
}
