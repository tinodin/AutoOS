using AutoOS.Views.Installer.Actions;

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

        string previousTitle = string.Empty;
        int stagePercentage = 5;

        var actions = new List<(string Title, Func<Task> Action, Func<bool> Condition)>
        {
            // set static ip
            ("Setting a static ip", async () => await ProcessActions.RunBatchScript("static.bat", ""), null),
            ("Setting a static ip", async () => await ProcessActions.Sleep(500), null),

            // check connection
            ("Waiting for internet connection to reestablish", async () => await ProcessActions.RunConnectionCheck(), null),

            // disable protocols
            ("Disabling unnecessary protocols", async () => await ProcessActions.RunPowerShell(@"Get-NetAdapter | ForEach-Object { foreach ($item in $_ | Get-NetAdapterBinding -DisplayName '*') { if ($item.DisplayName -ne 'QoS Packet Scheduler' -and $item.DisplayName -ne 'Internet Protocol Version 4 (TCP/IPv4)') { Disable-NetAdapterBinding -Name $_.Name -ComponentID $item.ComponentID } } }"), () => AppleMusic == false),
            ("Disabling unnecessary protocols", async () => await ProcessActions.RunPowerShell(@"Get-NetAdapter | ForEach-Object { foreach ($item in $_ | Get-NetAdapterBinding -DisplayName '*') { if ($item.DisplayName -ne 'QoS Packet Scheduler' -and $item.DisplayName -ne 'Internet Protocol Version 4 (TCP/IPv4)' -and $item.DisplayName -ne 'Internet Protocol Version 6 (TCP/IPv6)') { Disable-NetAdapterBinding -Name $_.Name -ComponentID $item.ComponentID } } }"), () => AppleMusic == true),

            // disable netbios over tcp
            ("Disabling NetBIOS over TCP", async () => await ProcessActions.RunNsudo("CurrentUser", @"cmd /c for %a in (NetbiosOptions) do for /f ""delims="" %b in ('reg query HKLM\SYSTEM\CurrentControlSet\Services\NetBT\Parameters\Interfaces /s /f %a ^| findstr HKEY') do reg add ""%b"" /v %a /t REG_DWORD /d 2 /f"), null),

            // adjust ethernet adapter advanced settings
            ("Adjusting Ethernet adapter advanced settings", async () => await ProcessActions.RunPowerShellScript("ethernet.ps1", ""), null),

            // check connection
            ("Waiting for internet connection to reestablish", async () => await ProcessActions.RunConnectionCheck(), null),

            // adjust wifi adapter advanved settings
            ("Adjusting Wi-Fi adapter advanced settings", async () => await ProcessActions.RunPowerShellScript("wifi.ps1", ""), () => Wifi == true),

            // check connection
            ("Waiting for internet connection to reestablish", async () => await ProcessActions.RunConnectionCheck(), () => Wifi == true),

            // disable power management settings
            ("Disabling power management settings", async () => await ProcessActions.RunPowerShellScript("networkpowermanagement.ps1", ""), null),

            // configure wake-on-lan
            ("Configuring Wake-On-Lan (WOL)", async () => await ProcessActions.RunPowerShellScript("wol.ps1", ""), () => WOL == true),

            // configure rss queues
            ("Configuring RSS queues", async () => await ProcessActions.RunPowerShellScript("rss.ps1", ""), () => RSS == true),
            ("Disabling RSS", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"netsh int tcp set global rss=disabled"), () => RSS == false),

             // set txintdelay to 0
            ("Setting TxIntDelay to 0", async () => await ProcessActions.RunPowerShellScript("txintdelay.ps1", ""), () => TxIntDelay == true),

            // disable nagles algorithm
            ("Disabling Nagles Algorithm", async () => await ProcessActions.RunPowerShell(@"Get-NetAdapter | ForEach-Object { New-ItemProperty -Path ""HKLM:\System\CurrentControlSet\Services\Tcpip\Parameters\Interfaces\$($_.InterfaceGuid)"" -Name ""TcpAckFrequency"" -PropertyType DWord -Value 1 -Force; New-ItemProperty -Path ""HKLM:\System\CurrentControlSet\Services\Tcpip\Parameters\Interfaces\$($_.InterfaceGuid)"" -Name ""TcpDelAckTicks"" -PropertyType DWord -Value 0 -Force; New-ItemProperty -Path ""HKLM:\System\CurrentControlSet\Services\Tcpip\Parameters\Interfaces\$($_.InterfaceGuid)"" -Name ""TCPNoDelay"" -PropertyType DWord -Value 1 -Force }"), null),

            // configure tcp/ip settings
            ("Enabling task offload", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"netsh int ip set global taskoffload=enabled"), null),
            ("Enabling WinSock autotuning", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"netsh winsock set autotuning on"), null),
            ("Disabling RSC", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"netsh int tcp set global rsc=disabled"), null),
            ("Setting neighbor cache limit", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"netsh int ip set global neighborcachelimit=4096"), null),
            ("Setting route cache limit", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"netsh int ip set global routecachelimit=4096"), null),
            ("Disabling source routing behavior", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"netsh int ip set global sourceroutingbehavior=drop"), null),
            ("Disabling DHCP media sense", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"netsh int ip set global dhcpmediasense=disabled"), null),
            ("Disabling media sense event log", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"netsh int ip set global mediasenseeventlog=disabled"), null),
            ("Disabling MLD level", async () => await ProcessActions.RunNsudo(  "TrustedInstaller", @"netsh int ip set global mldlevel=none"), null),
            ("Enabling DCA", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"netsh int tcp set global dca=enabled"), null),
            ("Disabling ECN Capability", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"netsh int tcp set global ecncapability=disabled"), null),
            ("Disabling NetDMA", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"netsh int tcp set global netdma=disabled"), null),
            ("Disabling Non-SACK RTT resiliency", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"netsh int tcp set global nonsackrttresiliency=disabled"), null),
            ("Disabling TCP timestamps", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"netsh int tcp set global timestamps=disabled"), null),
            ("Disabling TCP heuristics", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"netsh int tcp set heuristics disabled"), null),
            ("Disabling WSH heuristics", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"netsh int tcp set heuristics wsh=disabled"), null),
            ("Disabling security MPP", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"netsh int tcp set security mpp=disabled"), null),
            ("Disabling security profiles", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"netsh int tcp set security profiles=disabled"), null),
            ("Setting initial RTO", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"netsh int tcp set global initialRto=2000"), null),
            ("Setting max SYN retransmissions", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"netsh int tcp set global maxsynretransmissions=2"), null),
            ("Setting congestion provider to CTCP", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"netsh int tcp set supplemental internet congestionprovider=ctcp"), null),
            ("Disabling teredo", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"netsh interface teredo set state disabled"), null),
            ("Disabling ISATAP", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"netsh int isatap set state disabled"), null),
            ("Disabling 6to4", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"netsh interface 6to4 set state disabled"), null),
            ("Disabling chimney offload", async () => await ProcessActions.RunPowerShell(@"Set-NetOffloadGlobalSetting -Chimney Disabled"), null),
            ("Disabling packet coalescing filter", async () => await ProcessActions.RunPowerShell(@"Set-NetOffloadGlobalSetting -PacketCoalescingFilter Disabled"), null),
            ("Disabling windows forced scaling", async () => await ProcessActions.RunPowerShell(@"Set-NetTCPSetting -SettingName ""*"" -ForceWS Disabled"), null),

            // enable qos policies outside of domain networks
            ("Enabling QoS Policies outside of domain networks", async () => await ProcessActions.RunNsudo( "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Tcpip\QoS"" /v ""Do not use NLA"" /t REG_SZ /d 1 /f"), null),

            // disable wifi services and drivers
            ("Disabling Wi-Fi services and drivers", async () => await ProcessActions.DisableWiFiServicesAndDrivers(), () => Wifi == false),
        };

        var filteredActions = actions.Where(a => a.Condition == null || a.Condition.Invoke()).ToList();
        var uniqueTitles = filteredActions.Select(a => a.Title).Distinct().ToList();
        double incrementPerTitle = uniqueTitles.Count > 0 ? stagePercentage / (double)uniqueTitles.Count : 0;

        foreach (var title in uniqueTitles)
        {
            if (previousTitle != string.Empty && previousTitle != title)
            {
                await Task.Delay(150);
            }

            var actionsForTitle = filteredActions.Where(a => a.Title == title).ToList();
            int actionsForTitleCount = actionsForTitle.Count;

            foreach (var (actionTitle, action, condition) in actionsForTitle)
            {
                InstallPage.Info.Title = actionTitle + "...";

                try
                {
                    await action();
                }
                catch (Exception ex)
                {
                    InstallPage.Info.Title = ex.Message;
                    InstallPage.Info.Severity = InfoBarSeverity.Error;
                    InstallPage.Progress.Foreground = ProcessActions.GetColor("LightError", "DarkError");
                    InstallPage.ProgressRingControl.Foreground = ProcessActions.GetColor("LightError", "DarkError");
                    InstallPage.ProgressRingControl.Visibility = Visibility.Collapsed;
                    InstallPage.ResumeButton.Visibility = Visibility.Visible;

                    var tcs = new TaskCompletionSource<bool>();

                    InstallPage.ResumeButton.Click += (sender, e) =>
                    {
                        tcs.TrySetResult(true);
                        InstallPage.Info.Severity = InfoBarSeverity.Informational;
                        InstallPage.Progress.Foreground = ProcessActions.GetColor("LightNormal", "DarkNormal");
                        InstallPage.ProgressRingControl.Foreground = null;
                        InstallPage.ProgressRingControl.Visibility = Visibility.Visible;
                        InstallPage.ResumeButton.Visibility = Visibility.Collapsed;

                    };

                    await tcs.Task;
                }
            }

            InstallPage.Progress.Value += incrementPerTitle;

            previousTitle = title;
        }
    }
}
