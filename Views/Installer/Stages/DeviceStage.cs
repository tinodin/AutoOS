using AutoOS.Views.Installer.Actions;
using Microsoft.UI.Xaml.Media;

namespace AutoOS.Views.Installer.Stages;

public static class DeviceStage
{
    public static async Task Run()
    {
        bool? HID = PreparingStage.HID;
        bool? IMOD = PreparingStage.IMOD;
        bool? Bluetooth = PreparingStage.Bluetooth;

        InstallPage.Status.Text = "Configuring Devices...";

        string previousTitle = string.Empty;
        int stagePercentage = 5;

        var actions = new List<(string Title, Func<Task> Action, Func<bool> Condition)>
        {
            // disable write-cache buffer flushing on all drives
            ("Disabling write-cache buffer flushing on all drives", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"cmd /c for /f ""tokens=*"" %i in ('reg query ""HKLM\SYSTEM\CurrentControlSet\Enum\SCSI""^| findstr ""HKEY""') do for /f ""tokens=*"" %a in ('reg query ""%i""^| findstr ""HKEY""') do reg.exe add ""%a\Device Parameters\Disk"" /v ""CacheIsPowerProtected"" /t REG_DWORD /d 1 /f > NUL 2>&1 & for /f ""tokens=*"" %i in ('reg query ""HKLM\SYSTEM\CurrentControlSet\Enum\SCSI""^| findstr ""HKEY""') do for /f ""tokens=*"" %a in ('reg query ""%i""^| findstr ""HKEY""') do reg.exe add ""%a\Device Parameters\Disk"" /v ""UserWriteCacheSetting"" /t REG_DWORD /d 1 /f"), null),

            // disable drive powersaving features
            ("Disabling drive powersaving features", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"cmd /c for %a in (EnableHIPM EnableDIPM EnableHDDParking) do for /f ""delims="" %b in ('reg query ""HKLM\SYSTEM\CurrentControlSet\Services"" /s /f ""%a"" ^| findstr ""HKEY""') do reg.exe add ""%b"" /v ""%a"" /t REG_DWORD /d 0 /f"), null),
            ("Disabling drive powersaving features", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"cmd /c for /f ""tokens=*"" %%s in ('reg query ""HKLM\System\CurrentControlSet\Enum"" /S /F ""StorPort"" ^| findstr /e ""StorPort""') do Reg add ""%%s"" /v ""EnableIdlePowerManagement"" /t REG_DWORD /d ""0"" /f"), null),
            ("Disabling drive powersaving features", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"cmd /c for %a in (EnhancedPowerManagementEnabled AllowIdleIrpInD3 EnableSelectiveSuspend DeviceSelectiveSuspended SelectiveSuspendEnabled SelectiveSuspendOn EnumerationRetryCount ExtPropDescSemaphore WaitWakeEnabled D3ColdSupported WdfDirectedPowerTransitionEnable EnableIdlePowerManagement IdleInWorkingState) do for /f ""delims="" %b in ('reg query ""HKLM\SYSTEM\CurrentControlSet\Enum"" /s /f ""%a"" ^| findstr ""HKEY""') do reg add ""%b"" /v ""%a"" /t REG_DWORD /d 0 /f"), null),

            // disable dma remapping
            ("Disabling DMA remapping", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"cmd /c for %a in (DmaRemappingCompatible) do for /f ""delims="" %b in ('reg query ""HKLM\SYSTEM\CurrentControlSet\Services"" /s /f ""%a"" ^| findstr ""HKEY""') do reg.exe add ""%b"" /v ""%a"" /t REG_DWORD /d 0 /f "), null),

            // disable device power management settings
            ("Disabling device power management settings", async () => await ProcessActions.RunPowerShellScript("devicepowermanagement.ps1", ""), null),

            // enable msi mode for all devices
            ("Enabling MSI mode for all devices", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"cmd /c for /f ""tokens=*"" %i in ('reg query ""HKLM\SYSTEM\CurrentControlSet\Enum\PCI""^| findstr ""HKEY""') do for /f ""tokens=*"" %a in ('reg query ""%i""^| findstr ""HKEY""') do reg add ""%a\Device Parameters\Interrupt Management\MessageSignaledInterruptProperties"" /v ""MSISupported"" /t REG_DWORD /d 1 /f"), null),

            // set msi mode to undefined for all devices
            ("Setting MSI mode to undefined for all devices", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"cmd /c for /f ""tokens=*"" %i in ('reg query ""HKLM\SYSTEM\CurrentControlSet\Enum\PCI""^| findstr ""HKEY""') do for /f ""tokens=*"" %a in ('reg query ""%i""^| findstr ""HKEY""') do reg delete ""%a\Device Parameters\Interrupt Management\Affinity Policy"" /v ""DevicePriority"" /f"), null),

            // disable hid devices
            ("Disabling Human Interface Devices (HID)", async () => await ProcessActions.RunPowerShell("Get-PnpDevice -Class HIDClass | Where-Object { $_.FriendlyName -match 'HID-compliant (consumer control device|device|game controller|system controller|vendor-defined device)' -and $_.FriendlyName -notmatch 'Mouse|Keyboard'} | Disable-PnpDevice -Confirm:$false"), () => HID == false),

            // save xhci interrupt moderation (imod) data
            ("Saving XHCI Interrupt Moderation (IMOD) data", async () => await ProcessActions.RunPowerShellScript("imod.ps1", $"-save \"{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "RwEverything", "Rw.exe")}\""), null),

            // disable xhci interrupt moderation (imod)
            ("Disabling XHCI Interrupt Moderation (IMOD)", async () => await ProcessActions.RunPowerShellScript("imod.ps1", $"-disable \"{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "RwEverything", "Rw.exe")}\""), () => IMOD == false),

            // disable reserved storage
            ("Disabling reserved storage", async () => await ProcessActions.RunPowerShell(@"DISM /Online /Set-ReservedStorageState /State:Disabled"), null),

            // clean up devices
            ("Cleaning up devices", async () => await ProcessActions.RunApplication("DeviceCleanup", "DeviceCleanupCmd.exe", "/s *"), null),

            // clean up drives
            ("Cleaning up drives", async () => await ProcessActions.RunApplication("DriveCleanup", "DriveCleanup.exe", ""), null),

            // disable bluetooth services and drivers
            ("Disabling Bluetooth services and drivers", async () => await ProcessActions.DisableBluetoothServicesAndDrivers(), () => Bluetooth == false)
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
                    InstallPage.Progress.Foreground = (Brush)Application.Current.Resources["SystemFillColorCriticalBrush"];
                    InstallPage.ProgressRingControl.Foreground = (Brush)Application.Current.Resources["SystemFillColorCriticalBrush"];
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
