using Microsoft.Win32;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.ServiceProcess;

namespace AutoOS.Views.Settings;

public sealed partial class SecurityPage: Page
{
    private string nsudoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "NSudo", "NSudoLC.exe");
    private bool initialUACState = false;
    private bool initialSpectreMeltdownState = false;
    private bool initialProcessMitigationsState = false;
    private bool isInitializingWindowsDefenderState = true;
    private bool isInitializingUACState = true;
    private bool isInitializingDEPState = true;
    private bool isInitializingSpectreMeltdownState = true;
    private bool isInitializingProcessMitigationsState = true;

    [DllImport("kernel32.dll")]
    static extern int GetSystemDEPPolicy();
    public SecurityPage()
    {
        InitializeComponent();
        GetWindowsDefenderState();
        GetUACState();
        GetDEPState();
        GetSpectreMeltdownState();
        GetProcessMitigationsState();
    }

    private void GetWindowsDefenderState()
    {
        // check state
        if (Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Microsoft Antimalware\Real-Time Protection", "DisableScanOnRealtimeEnable", null) == null)
        {
            WindowsDefender.IsOn = true;
        }

        var serviceController = new ServiceController("WinDefend");
        bool isRunning = serviceController.Status == ServiceControllerStatus.Running;
        if (WindowsDefender.IsOn && !isRunning || !WindowsDefender.IsOn && isRunning)
        {
            // remove infobar
            WindowsDefenderInfo.Children.Clear();

            // add infobar
            var infoBar = new InfoBar
            {
                Title = WindowsDefender.IsOn ? "Successfully enabled Windows Defender. A restart is required to apply the change." : "Successfully disabled Windows Defender. A restart is required to apply the change.",
                IsClosable = false,
                IsOpen = true,
                Severity = InfoBarSeverity.Success,
                Margin = new Thickness(5)
            };
            infoBar.ActionButton = new Button
            {
                Content = "Restart",
                HorizontalAlignment = HorizontalAlignment.Right
            };
            ((Button)infoBar.ActionButton).Click += (s, args) => Process.Start("shutdown", "/r /f /t 0");
            WindowsDefenderInfo.Children.Add(infoBar);
        }
        isInitializingWindowsDefenderState = false;
    }

    private async void WindowsDefender_Toggled(object sender, RoutedEventArgs e)
    {
        if (isInitializingWindowsDefenderState) return;

        // remove infobar
        WindowsDefenderInfo.Children.Clear();

        // add infobar
        WindowsDefenderInfo.Children.Add(new InfoBar
        {
            Title = WindowsDefender.IsOn ? "Enabling Windows Defender..." : "Disabling Windows Defender...",
            IsClosable = false,
            IsOpen = true,
            Severity = InfoBarSeverity.Informational,
            Margin = new Thickness(5)
        });

        // toggle windows defender
        if (WindowsDefender.IsOn)
        {
            await Task.Run(() =>
            {
                Process.Start(new ProcessStartInfo { FileName = nsudoPath, Arguments = @"-U:T -P:E -Wait -ShowWindowMode:Hide cmd /c reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows Defender"" /v DisableAntiSpyware /f", CreateNoWindow = true }).WaitForExit();
                Process.Start(new ProcessStartInfo { FileName = nsudoPath, Arguments = @"-U:T -P:E -Wait -ShowWindowMode:Hide cmd /c reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Microsoft Antimalware\Real-Time Protection"" /v DisableScanOnRealtimeEnable /f", CreateNoWindow = true }).WaitForExit();
                Process.Start(new ProcessStartInfo { FileName = nsudoPath, Arguments = @"-U:T -P:E -Wait -ShowWindowMode:Hide cmd /c reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Microsoft Antimalware\Real-Time Protection"" /v DisableOnAccessProtection /f", CreateNoWindow = true }).WaitForExit();
                Process.Start(new ProcessStartInfo { FileName = nsudoPath, Arguments = @"-U:T -P:E -Wait -ShowWindowMode:Hide cmd /c reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows Defender\Real-Time Protection"" /v DisableScanOnRealtimeEnable /f", CreateNoWindow = true }).WaitForExit();
                Process.Start(new ProcessStartInfo { FileName = nsudoPath, Arguments = @"-U:T -P:E -Wait -ShowWindowMode:Hide cmd /c reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows Defender\Real-Time Protection"" /v DisableBehaviorMonitoring /f", CreateNoWindow = true }).WaitForExit();
                Process.Start(new ProcessStartInfo { FileName = nsudoPath, Arguments = @"-U:T -P:E -Wait -ShowWindowMode:Hide cmd /c reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\MsSecCore"" /v Start /t REG_DWORD /d 0 /f", CreateNoWindow = true }).WaitForExit();
                Process.Start(new ProcessStartInfo { FileName = nsudoPath, Arguments = @"-U:T -P:E -Wait -ShowWindowMode:Hide cmd /c reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\SecurityHealthService"" /v Start /t REG_DWORD /d 3 /f", CreateNoWindow = true }).WaitForExit();
                Process.Start(new ProcessStartInfo { FileName = nsudoPath, Arguments = @"-U:T -P:E -Wait -ShowWindowMode:Hide cmd /c reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Sense"" /v Start /t REG_DWORD /d 3 /f", CreateNoWindow = true }).WaitForExit();
                Process.Start(new ProcessStartInfo { FileName = nsudoPath, Arguments = @"-U:T -P:E -Wait -ShowWindowMode:Hide cmd /c reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\WdBoot"" /v Start /t REG_DWORD /d 0 /f", CreateNoWindow = true }).WaitForExit();
                Process.Start(new ProcessStartInfo { FileName = nsudoPath, Arguments = @"-U:T -P:E -Wait -ShowWindowMode:Hide cmd /c reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\WdFilter"" /v Start /t REG_DWORD /d 0 /f", CreateNoWindow = true }).WaitForExit();
                Process.Start(new ProcessStartInfo { FileName = nsudoPath, Arguments = @"-U:T -P:E -Wait -ShowWindowMode:Hide cmd /c reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\WdNisDrv"" /v Start /t REG_DWORD /d 3 /f", CreateNoWindow = true }).WaitForExit();
                Process.Start(new ProcessStartInfo { FileName = nsudoPath, Arguments = @"-U:T -P:E -Wait -ShowWindowMode:Hide cmd /c reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\WdNisSvc"" /v Start /t REG_DWORD /d 3 /f", CreateNoWindow = true }).WaitForExit();
                Process.Start(new ProcessStartInfo { FileName = nsudoPath, Arguments = @"-U:T -P:E -Wait -ShowWindowMode:Hide cmd /c reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\webthreatdefsvc"" /v Start /t REG_DWORD /d 3 /f", CreateNoWindow = true }).WaitForExit();
                Process.Start(new ProcessStartInfo { FileName = nsudoPath, Arguments = @"-U:T -P:E -Wait -ShowWindowMode:Hide cmd /c reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\webthreatdefusersvc"" /v Start /t REG_DWORD /d 2 /f", CreateNoWindow = true }).WaitForExit();
                Process.Start(new ProcessStartInfo { FileName = nsudoPath, Arguments = @"-U:T -P:E -Wait -ShowWindowMode:Hide cmd /c reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\WinDefend"" /v Start /t REG_DWORD /d 2 /f", CreateNoWindow = true }).WaitForExit();
                Process.Start(new ProcessStartInfo { FileName = nsudoPath, Arguments = @"-U:T -P:E -Wait -ShowWindowMode:Hide cmd /c reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\wscsvc"" /v Start /t REG_DWORD /d 2 /f", CreateNoWindow = true }).WaitForExit();
                Process.Start(new ProcessStartInfo { FileName = nsudoPath, Arguments = @"-U:T -P:E -Wait -ShowWindowMode:Hide cmd /c reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\MRT"" /v DontOfferThroughWUAU /f", CreateNoWindow = true }).WaitForExit();
            });
        }
        else
        {
            await Task.Run(() =>
            {
                Process.Start(new ProcessStartInfo { FileName = nsudoPath, Arguments = @"-U:T -P:E -Wait -ShowWindowMode:Hide cmd /c reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows Defender"" /v DisableAntiSpyware /t REG_DWORD /d 1 /f", CreateNoWindow = true }).WaitForExit();
                Process.Start(new ProcessStartInfo { FileName = nsudoPath, Arguments = @"-U:T -P:E -Wait -ShowWindowMode:Hide cmd /c reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Microsoft Antimalware\Real-Time Protection"" /v DisableScanOnRealtimeEnable /t REG_DWORD /d 1 /f", CreateNoWindow = true }).WaitForExit();
                Process.Start(new ProcessStartInfo { FileName = nsudoPath, Arguments = @"-U:T -P:E -Wait -ShowWindowMode:Hide cmd /c reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Microsoft Antimalware\Real-Time Protection"" /v DisableOnAccessProtection /t REG_DWORD /d 1 /f", CreateNoWindow = true }).WaitForExit();
                Process.Start(new ProcessStartInfo { FileName = nsudoPath, Arguments = @"-U:T -P:E -Wait -ShowWindowMode:Hide cmd /c reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows Defender\Real-Time Protection"" /v DisableScanOnRealtimeEnable /t REG_DWORD /d 1 /f", CreateNoWindow = true }).WaitForExit();
                Process.Start(new ProcessStartInfo { FileName = nsudoPath, Arguments = @"-U:T -P:E -Wait -ShowWindowMode:Hide cmd /c reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows Defender\Real-Time Protection"" /v DisableBehaviorMonitoring /t REG_DWORD /d 1 /f", CreateNoWindow = true }).WaitForExit();
                Process.Start(new ProcessStartInfo { FileName = nsudoPath, Arguments = @"-U:T -P:E -Wait -ShowWindowMode:Hide cmd /c reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\MsSecCore"" /v Start /t REG_DWORD /d 4 /f", CreateNoWindow = true }).WaitForExit();
                Process.Start(new ProcessStartInfo { FileName = nsudoPath, Arguments = @"-U:T -P:E -Wait -ShowWindowMode:Hide cmd /c reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\SecurityHealthService"" /v Start /t REG_DWORD /d 4 /f", CreateNoWindow = true }).WaitForExit();
                Process.Start(new ProcessStartInfo { FileName = nsudoPath, Arguments = @"-U:T -P:E -Wait -ShowWindowMode:Hide cmd /c reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Sense"" /v Start /t REG_DWORD /d 4 /f", CreateNoWindow = true }).WaitForExit();
                Process.Start(new ProcessStartInfo { FileName = nsudoPath, Arguments = @"-U:T -P:E -Wait -ShowWindowMode:Hide cmd /c reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\WdBoot"" /v Start /t REG_DWORD /d 4 /f", CreateNoWindow = true }).WaitForExit();
                Process.Start(new ProcessStartInfo { FileName = nsudoPath, Arguments = @"-U:T -P:E -Wait -ShowWindowMode:Hide cmd /c reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\WdFilter"" /v Start /t REG_DWORD /d 4 /f", CreateNoWindow = true }).WaitForExit();
                Process.Start(new ProcessStartInfo { FileName = nsudoPath, Arguments = @"-U:T -P:E -Wait -ShowWindowMode:Hide cmd /c reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\WdNisDrv"" /v Start /t REG_DWORD /d 4 /f", CreateNoWindow = true }).WaitForExit();
                Process.Start(new ProcessStartInfo { FileName = nsudoPath, Arguments = @"-U:T -P:E -Wait -ShowWindowMode:Hide cmd /c reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\WdNisSvc"" /v Start /t REG_DWORD /d 4 /f", CreateNoWindow = true }).WaitForExit();
                Process.Start(new ProcessStartInfo { FileName = nsudoPath, Arguments = @"-U:T -P:E -Wait -ShowWindowMode:Hide cmd /c reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\webthreatdefsvc"" /v Start /t REG_DWORD /d 4 /f", CreateNoWindow = true }).WaitForExit();
                Process.Start(new ProcessStartInfo { FileName = nsudoPath, Arguments = @"-U:T -P:E -Wait -ShowWindowMode:Hide cmd /c reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\webthreatdefusersvc"" /v Start /t REG_DWORD /d 4 /f", CreateNoWindow = true }).WaitForExit();
                Process.Start(new ProcessStartInfo { FileName = nsudoPath, Arguments = @"-U:T -P:E -Wait -ShowWindowMode:Hide cmd /c reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\WinDefend"" /v Start /t REG_DWORD /d 4 /f", CreateNoWindow = true }).WaitForExit();
                Process.Start(new ProcessStartInfo { FileName = nsudoPath, Arguments = @"-U:T -P:E -Wait -ShowWindowMode:Hide cmd /c reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\wscsvc"" /v Start /t REG_DWORD /d 4 /f", CreateNoWindow = true }).WaitForExit();
                Process.Start(new ProcessStartInfo { FileName = nsudoPath, Arguments = @"-U:T -P:E -Wait -ShowWindowMode:Hide cmd /c reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\MRT"" /v DontOfferThroughWUAU /t REG_DWORD /d 1 /f", CreateNoWindow = true }).WaitForExit();
            });
        }

        // delay
        await Task.Delay(200);

        // remove infobar
        WindowsDefenderInfo.Children.Clear();

        // add infobar
        var infoBar = new InfoBar
        {
            Title = WindowsDefender.IsOn ? "Successfully enabled Windows Defender." : "Successfully disabled Windows Defender.",
            IsClosable = false,
            IsOpen = true,
            Severity = InfoBarSeverity.Success,
            Margin = new Thickness(5)
        };
        WindowsDefenderInfo.Children.Add(infoBar);

        // add restart button
        var serviceController = new ServiceController("WinDefend");
        bool isRunning = serviceController.Status == ServiceControllerStatus.Running;

        if (WindowsDefender.IsOn && !isRunning || !WindowsDefender.IsOn && isRunning)
        {
            infoBar.Title += " A restart is required to apply the change.";
            infoBar.ActionButton = new Button
            {
                Content = "Restart",
                HorizontalAlignment = HorizontalAlignment.Right
            };
            ((Button)infoBar.ActionButton).Click += (s, args) => Process.Start("shutdown", "/r /f /t 0");
        }
        else
        {
            // delay
            await Task.Delay(2000);

            // remove infobar
            WindowsDefenderInfo.Children.Clear();
        }
    }

    private void GetUACState()
    {
        // check registry value
        if ((int?)Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System")?.GetValue("EnableLUA") == 1)
        {
            UAC.IsOn = true;
            initialUACState = true;
        }
        isInitializingUACState = false;
    }

    private async void UAC_Toggled(object sender, RoutedEventArgs e)
    {
        if (isInitializingUACState) return;

        // remove infobar
        WindowsDefenderInfo.Children.Clear();

        // add infobar
        WindowsDefenderInfo.Children.Add(new InfoBar
        {
            Title = UAC.IsOn ? "Enabling User Account Control (UAC)..." : "Disabling User Account Control (UAC)...",
            IsClosable = false,
            IsOpen = true,
            Severity = InfoBarSeverity.Informational,
            Margin = new Thickness(5)
        });

        // toggle uac
        using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", true))
        {
            key.SetValue("EnableLUA", UAC.IsOn ? 1 : 0, RegistryValueKind.DWord);
        }

        // delay
        await Task.Delay(500);

        // remove infobar
        WindowsDefenderInfo.Children.Clear();

        // add infobar
        var infoBar = new InfoBar
        {
            Title = UAC.IsOn ? "Successfully enabled User Account Control (UAC)." : "Successfully disabled User Account Control (UAC).",
            IsClosable = false,
            IsOpen = true,
            Severity = InfoBarSeverity.Success,
            Margin = new Thickness(5)
        };
        WindowsDefenderInfo.Children.Add(infoBar);

        // add restart button if needed
        if (UAC.IsOn != initialUACState)
        {
            infoBar.Title += " A restart is required to apply the change.";
            infoBar.ActionButton = new Button
            {
                Content = "Restart",
                HorizontalAlignment = HorizontalAlignment.Right
            };
            ((Button)infoBar.ActionButton).Click += (s, args) =>
            Process.Start("shutdown", "/r /f /t 0");
        }
        else
        {
            // delay
            await Task.Delay(2000);

            // remove infobar
            WindowsDefenderInfo.Children.Clear();
        }
    }

    private void GetDEPState()
    {
        // get active state
        int policy = GetSystemDEPPolicy();

        // get state
        var output = Process.Start(new ProcessStartInfo("cmd.exe", "/c bcdedit /enum {current}") { CreateNoWindow = true, RedirectStandardOutput = true }).StandardOutput.ReadToEnd();

        if (output.Contains("nx                      OptIn"))
        {
            if (policy == 0)
            {
                // remove infobar
                WindowsDefenderInfo.Children.Clear();

                // add infobar
                var infoBar = new InfoBar
                {
                    Title = DEP.IsOn ? "Successfully disabled Data Execution Prevention (DEP). A restart is required to apply the change." : "Successfully enabled Data Execution Prevention (DEP). A restart is required to apply the change.",
                    IsClosable = false,
                    IsOpen = true,
                    Severity = InfoBarSeverity.Success,
                    Margin = new Thickness(5)
                };
                infoBar.ActionButton = new Button
                {
                    Content = "Restart",
                    HorizontalAlignment = HorizontalAlignment.Right
                };
                ((Button)infoBar.ActionButton).Click += (s, args) =>
                Process.Start("shutdown", "/r /f /t 0");
                WindowsDefenderInfo.Children.Add(infoBar);
            }

            DEP.IsOn = true;
        }
        else if (output.Contains("nx                      AlwaysOff"))
        {
            if (policy == 2)
            {
                // remove infobar
                WindowsDefenderInfo.Children.Clear();

                // add infobar
                var infoBar = new InfoBar
                {
                    Title = DEP.IsOn ? "Successfully enabled Data Execution Prevention (DEP). A restart is required to apply the change." : "Successfully disabled Data Execution Prevention (DEP). A restart is required to apply the change.",
                    IsClosable = false,
                    IsOpen = true,
                    Severity = InfoBarSeverity.Success,
                    Margin = new Thickness(5)
                };
                infoBar.ActionButton = new Button
                {
                    Content = "Restart",
                    HorizontalAlignment = HorizontalAlignment.Right
                };
                ((Button)infoBar.ActionButton).Click += (s, args) =>
                Process.Start("shutdown", "/r /f /t 0");
                WindowsDefenderInfo.Children.Add(infoBar);
            }
        }

        isInitializingDEPState = false;
    }

    private async void DEP_Toggled(object sender, RoutedEventArgs e)
    {
        if (isInitializingDEPState) return;

        // get active state
        int policy = GetSystemDEPPolicy();

        // remove infobar
        WindowsDefenderInfo.Children.Clear();

        // add infobar
        WindowsDefenderInfo.Children.Add(new InfoBar
        {
            Title = DEP.IsOn ? "Enabling Data Execution Prevention (DEP)..." : "Disabling Data Execution Prevention (DEP)...",
            IsClosable = false,
            IsOpen = true,
            Severity = InfoBarSeverity.Informational,
            Margin = new Thickness(5)
        });

        // toggle dep
        var output = Process.Start(new ProcessStartInfo("cmd.exe", $"/c {(DEP.IsOn ? "bcdedit /set nx OptIn" : "bcdedit /set nx AlwaysOff")}"  ) { CreateNoWindow = true, RedirectStandardOutput = true }).StandardOutput.ReadToEnd();

        if (output.Contains("error"))
        {
            // delay
            await Task.Delay(500);

            // remove infobar
            WindowsDefenderInfo.Children.Clear();

            // toggle back
            isInitializingDEPState = true;
            DEP.IsOn = !DEP.IsOn;
            isInitializingDEPState = false;

            // add infobar
            WindowsDefenderInfo.Children.Add(new InfoBar
            {
                Title = "Failed to disable Data Execution Prevention (DEP) because secure boot is enabled.",
                IsClosable = false,
                IsOpen = true,
                Severity = InfoBarSeverity.Error,
                Margin = new Thickness(5)
            });

            // delay
            await Task.Delay(2000);

            // remove infobar
            WindowsDefenderInfo.Children.Clear();
        }
        else
        {
            // delay
            await Task.Delay(500);

            // remove infobar
            WindowsDefenderInfo.Children.Clear();

            // add infobar
            var infoBar = new InfoBar
            {
                Title = DEP.IsOn ? "Successfully enabled Data Execution Prevention (DEP)." : "Successfully disabled Data Execution Prevention (DEP).",
                IsClosable = false,
                IsOpen = true,
                Severity = InfoBarSeverity.Success,
                Margin = new Thickness(5)
            };
            WindowsDefenderInfo.Children.Add(infoBar);

            // add restart button if needed
            if ((DEP.IsOn && policy == 0) || (!DEP.IsOn && policy == 2))
            {
                infoBar.Title += " A restart is required to apply the change.";
                infoBar.ActionButton = new Button
                {
                    Content = "Restart",
                    HorizontalAlignment = HorizontalAlignment.Right
                };
                ((Button)infoBar.ActionButton).Click += (s, args) =>
                Process.Start("shutdown", "/r /f /t 0");
            }
            else
            {
                // delay
                await Task.Delay(2000);

                // remove infobar
                WindowsDefenderInfo.Children.Clear();
            }
        }  
    }

    private void GetSpectreMeltdownState()
    {
        // check registry
        string cpuVendor = (string)Registry.GetValue(@"HKEY_LOCAL_MACHINE\HARDWARE\DESCRIPTION\System\CentralProcessor\0", "VendorIdentifier", null);
        int? featureSettings = (int?)Registry.GetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "FeatureSettings", null);
        int? featureSettingsOverrideMask = (int?)Registry.GetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "FeatureSettingsOverrideMask", null);
        int? featureSettingsOverride = (int?)Registry.GetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "FeatureSettingsOverride", null);

        if (cpuVendor.Contains("GenuineIntel"))
        {
            if (featureSettings == 0 && featureSettingsOverrideMask == null && featureSettingsOverride == null)
            {
                initialSpectreMeltdownState = true;
                SpectreMeltdown.IsOn = true;
            }
        }
        else if (cpuVendor.Contains("AuthenticAMD"))
        {
            if (featureSettings == 1 && featureSettingsOverrideMask == 3 && featureSettingsOverride == 64)
            {
                initialSpectreMeltdownState = true;
                SpectreMeltdown.IsOn = true;
            }
        }
        isInitializingSpectreMeltdownState = false;
    }

    private async void SpectreMeltdown_Toggled(object sender, RoutedEventArgs e)
    {
        if (isInitializingSpectreMeltdownState) return;

        // remove infobar
        WindowsDefenderInfo.Children.Clear();

        // add infobar
        WindowsDefenderInfo.Children.Add(new InfoBar
        {
            Title = SpectreMeltdown.IsOn ? "Enabling Spectre & Meltdown Mitigations..." : "Disabling Spectre & Meltdown Mitigations...",
            IsClosable = false,
            IsOpen = true,
            Severity = InfoBarSeverity.Informational,
            Margin = new Thickness(5)
        });

        if (SpectreMeltdown.IsOn)
        {
            string cpuVendor = (string)Registry.GetValue(@"HKEY_LOCAL_MACHINE\HARDWARE\DESCRIPTION\System\CentralProcessor\0", "VendorIdentifier", null);

            if (cpuVendor.Contains("GenuineIntel"))
            {
                // restore default values for enabling on intel
                Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "FeatureSettings", 0, RegistryValueKind.DWord);
                Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", writable: true)
                .DeleteValue("FeatureSettingsOverrideMask", throwOnMissingValue: false);
                Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", writable: true)
                .DeleteValue("FeatureSettingsOverride", throwOnMissingValue: false);
            }
            else if (cpuVendor.Contains("AuthenticAMD"))
            {
                // set custom values to enable all mitigations on amd
                Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "FeatureSettings", 1, RegistryValueKind.DWord);
                Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "FeatureSettingsOverrideMask", 3, RegistryValueKind.DWord);
                Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "FeatureSettingsOverride", 64, RegistryValueKind.DWord);
            }

            // rename to enable microcode updates
            Process.Start(new ProcessStartInfo { FileName = nsudoPath, Arguments = @"-U:T -P:E -Wait -ShowWindowMode:Hide cmd /c ren C:\Windows\System32\mcupdate_GenuineIntel.dlll mcupdate_GenuineIntel.dll", CreateNoWindow = true }).WaitForExit();
            Process.Start(new ProcessStartInfo { FileName = nsudoPath, Arguments = @"-U:T -P:E -Wait -ShowWindowMode:Hide cmd /c ren C:\Windows\System32\mcupdate_AuthenticAMD.dlll mcupdate_AuthenticAMD.dll", CreateNoWindow = true }).WaitForExit();
        }
        else
        {
            // disable spectre & meltdown
            Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "FeatureSettings", 1, RegistryValueKind.DWord);
            Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "FeatureSettingsOverrideMask", 3, RegistryValueKind.DWord);
            Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "FeatureSettingsOverride", 3, RegistryValueKind.DWord);

            // rename to disable microcode updates
            Process.Start(new ProcessStartInfo { FileName = nsudoPath, Arguments = @"-U:T -P:E -Wait -ShowWindowMode:Hide cmd /c ren C:\Windows\System32\mcupdate_GenuineIntel.dll mcupdate_GenuineIntel.dlll", CreateNoWindow = true }).WaitForExit();
            Process.Start(new ProcessStartInfo { FileName = nsudoPath, Arguments = @"-U:T -P:E -Wait -ShowWindowMode:Hide cmd /c ren C:\Windows\System32\mcupdate_AuthenticAMD.dll mcupdate_AuthenticAMD.dlll", CreateNoWindow = true }).WaitForExit();
        }

        // delay
        await Task.Delay(500);

        // remove infobar
        WindowsDefenderInfo.Children.Clear();

        // add infobar
        var infoBar = new InfoBar
        {
            Title = SpectreMeltdown.IsOn ? "Successfully enabled Spectre & Meltdown Mitigations." : "Successfully disabled Spectre & Meltdown Mitigations.",
            IsClosable = false,
            IsOpen = true,
            Severity = InfoBarSeverity.Success,
            Margin = new Thickness(5)
        };
        WindowsDefenderInfo.Children.Add(infoBar);

        // add restart button if needed
        if (SpectreMeltdown.IsOn != initialSpectreMeltdownState)
        {
            infoBar.Title += " A restart is required to apply the change.";
            infoBar.ActionButton = new Button
            {
                Content = "Restart",
                HorizontalAlignment = HorizontalAlignment.Right
            };
            ((Button)infoBar.ActionButton).Click += (s, args) =>
            Process.Start("shutdown", "/r /f /t 0");
        }
        else
        {
            // delay
            await Task.Delay(2000);

            // remove infobar
            WindowsDefenderInfo.Children.Clear();
        }
    }

    private void GetProcessMitigationsState()
    {
        // get state
        if (Registry.GetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\kernel", "MitigationOptions", null) == null && Registry.GetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\kernel", "MitigationAuditOptions", null) == null)
        {
            ProcessMitigations.IsOn = true;
            initialProcessMitigationsState = true;
        }
        isInitializingProcessMitigationsState = false;
    }

    private async void ProcessMitigations_Toggled(object sender, RoutedEventArgs e)
    {
        if (isInitializingProcessMitigationsState) return;

        // remove infobar
        WindowsDefenderInfo.Children.Clear();

        // add infobar
        WindowsDefenderInfo.Children.Add(new InfoBar
        {
            Title = ProcessMitigations.IsOn ? "Enabling Process Mitigations..." : "Disabling Process Mitigations...",
            IsClosable = false,
            IsOpen = true,
            Severity = InfoBarSeverity.Informational,
            Margin = new Thickness(5)
        });

        // toggle process mitigations
        var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Session Manager\kernel", true);
        if (key != null)
        {
            if (ProcessMitigations.IsOn)
            {
                key.DeleteValue("MitigationAuditOptions", false);
                key.DeleteValue("MitigationOptions", false);
            }
            else
            {
                key.SetValue("MitigationAuditOptions", new byte[] { 0x22, 0x22, 0x22, 0x22, 0x22, 0x22, 0x22, 0x22, 0x22, 0x22, 0x22, 0x22, 0x22, 0x22, 0x22, 0x22, 0x22, 0x22, 0x22, 0x22, 0x22, 0x22, 0x22, 0x22 });
                key.SetValue("MitigationOptions", new byte[] { 0x22, 0x22, 0x22, 0x22, 0x22, 0x22, 0x22, 0x22, 0x22, 0x22, 0x22, 0x22, 0x22, 0x22, 0x22, 0x22, 0x22, 0x22, 0x22, 0x22, 0x22, 0x22, 0x22, 0x22 });
            }
        }

        // delay
        await Task.Delay(500);

        // remove infobar
        WindowsDefenderInfo.Children.Clear();

        // add infobar
        var infoBar = new InfoBar
        {
            Title = ProcessMitigations.IsOn ? "Successfully enabled Process Mitigations." : "Successfully disabled Process Mitigations.",
            IsClosable = false,
            IsOpen = true,
            Severity = InfoBarSeverity.Success,
            Margin = new Thickness(5)
        };
        WindowsDefenderInfo.Children.Add(infoBar);

        // add restart button if needed
        if (ProcessMitigations.IsOn != initialProcessMitigationsState)
        {
            infoBar.Title += " A restart is required to apply the change.";
            infoBar.ActionButton = new Button
            {
                Content = "Restart",
                HorizontalAlignment = HorizontalAlignment.Right
            };
            ((Button)infoBar.ActionButton).Click += (s, args) =>
            Process.Start("shutdown", "/r /f /t 0");
        }
        else
        {
            // delay
            await Task.Delay(2000);

            // remove infobar
            WindowsDefenderInfo.Children.Clear();
        }
    }
}

