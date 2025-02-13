using AutoOS.Views.Installer.Actions;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace AutoOS.Views.Installer.Stages;

public static class DeviceStage
{
    public static async Task Run()
    {
        bool? HID = PreparingStage.HID;
        bool? IMOD = PreparingStage.IMOD;
        bool? Bluetooth = PreparingStage.Bluetooth;

        InstallPage.Status.Text = "Configuring Devices...";

        int validActionsCount = 0;
        int stagePercentage = 2;

        var actions = new List<(Func<Task> Action, Func<bool> Condition)>
        {
            // disable motherboard resources
            (async () => await ProcessActions.RunApplication("Disabling motherboard resources", "DevManView", "DevManView.exe", @"/disable ""Motherboard resources"""), null),

            // disable write-cache buffer flushing on all drives
            (async () => await ProcessActions.RunNsudo("Disabling write-cache buffer flushing on all drives", "TrustedInstaller", @"cmd /c for /f ""tokens=*"" %i in ('reg query ""HKLM\SYSTEM\CurrentControlSet\Enum\SCSI""^| findstr ""HKEY""') do for /f ""tokens=*"" %a in ('reg query ""%i""^| findstr ""HKEY""') do reg.exe add ""%a\Device Parameters\Disk"" /v ""CacheIsPowerProtected"" /t REG_DWORD /d 1 /f > NUL 2>&1 & for /f ""tokens=*"" %i in ('reg query ""HKLM\SYSTEM\CurrentControlSet\Enum\SCSI""^| findstr ""HKEY""') do for /f ""tokens=*"" %a in ('reg query ""%i""^| findstr ""HKEY""') do reg.exe add ""%a\Device Parameters\Disk"" /v ""UserWriteCacheSetting"" /t REG_DWORD /d 1 /f"), null),

            // disable drive powersaving features
            (async () => await ProcessActions.RunNsudo("Disabling drive powersaving features", "TrustedInstaller", @"cmd /c for %a in (EnableHIPM EnableDIPM EnableHDDParking) do for /f ""delims="" %b in ('reg query ""HKLM\SYSTEM\CurrentControlSet\Services"" /s /f ""%a"" ^| findstr ""HKEY""') do reg.exe add ""%b"" /v ""%a"" /t REG_DWORD /d 0 /f"), null),
            (async () => await ProcessActions.RunNsudo("Disabling drive powersaving features", "TrustedInstaller", @"cmd /c for /f ""tokens=*"" %%s in ('reg query ""HKLM\System\CurrentControlSet\Enum"" /S /F ""StorPort"" ^| findstr /e ""StorPort""') do Reg add ""%%s"" /v ""EnableIdlePowerManagement"" /t REG_DWORD /d ""0"" /f"), null),
            (async () => await ProcessActions.RunNsudo("Disabling drive powersaving features", "TrustedInstaller", @"cmd /c for %a in (EnhancedPowerManagementEnabled AllowIdleIrpInD3 EnableSelectiveSuspend DeviceSelectiveSuspended SelectiveSuspendEnabled SelectiveSuspendOn EnumerationRetryCount ExtPropDescSemaphore WaitWakeEnabled D3ColdSupported WdfDirectedPowerTransitionEnable EnableIdlePowerManagement IdleInWorkingState) do for /f ""delims="" %b in ('reg query ""HKLM\SYSTEM\CurrentControlSet\Enum"" /s /f ""%a"" ^| findstr ""HKEY""') do reg add ""%b"" /v ""%a"" /t REG_DWORD /d 0 /f"), null),

            // disable dma remapping
            (async () => await ProcessActions.RunNsudo("Disabling DMA remapping", "TrustedInstaller", @"cmd /c for %a in (DmaRemappingCompatible) do for /f ""delims="" %b in ('reg query ""HKLM\SYSTEM\CurrentControlSet\Services"" /s /f ""%a"" ^| findstr ""HKEY""') do reg.exe add ""%b"" /v ""%a"" /t REG_DWORD /d 0 /f "), null),

            // disable device power management settings
            (async () => await ProcessActions.RunPowerShellScript("Disabling device power management settings", "devicepowermanagement.ps1", ""), null),

            // enable msi mode for all devices
            (async () => await ProcessActions.RunNsudo("Enabling MSI mode for all devices", "TrustedInstaller", @"cmd /c for /f ""tokens=*"" %i in ('reg query ""HKLM\SYSTEM\CurrentControlSet\Enum\PCI""^| findstr ""HKEY""') do for /f ""tokens=*"" %a in ('reg query ""%i""^| findstr ""HKEY""') do reg add ""%a\Device Parameters\Interrupt Management\MessageSignaledInterruptProperties"" /v ""MSISupported"" /t REG_DWORD /d 1 /f"), null),

            // set msi mode to undefined for all devices
            (async () => await ProcessActions.RunNsudo("Setting MSI mode to undefined for all devices", "TrustedInstaller", @"cmd /c for /f ""tokens=*"" %i in ('reg query ""HKLM\SYSTEM\CurrentControlSet\Enum\PCI""^| findstr ""HKEY""') do for /f ""tokens=*"" %a in ('reg query ""%i""^| findstr ""HKEY""') do reg delete ""%a\Device Parameters\Interrupt Management\Affinity Policy"" /v ""DevicePriority"" /f"), null),

            // disable hid devices
            (async () => await ProcessActions.RunPowerShell("Disabling HID devices", "Get-PnpDevice -Class HIDClass | Where-Object { $_.FriendlyName -match 'HID-compliant (consumer control device|device|game controller|system controller|vendor-defined device)' -and $_.FriendlyName -notmatch 'Mouse|Keyboard'} | Disable-PnpDevice -Confirm:$false"), () => HID == false),

            // save xhci interrupt moderation state
            (async () => await ProcessActions.RunPowerShellScript("Saving XHCI Interrupt Moderation data", "imod.ps1", $"-save \"{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "RwEverything", "Rw.exe")}\""),() => IMOD == false),

            // disable xhci interrupt moderation
            (async () => await ProcessActions.RunPowerShellScript("Disabling XHCI Interrupt Moderation", "imod.ps1", $"-disable \"{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "RwEverything", "Rw.exe")}\""),() => IMOD == false),

            // disable reserved storage
            (async () => await ProcessActions.RunPowerShell("Disabling reserved storage", @"DISM /Online /Set-ReservedStorageState /State:Disabled"), null),

            // clean up devices
            (async () => await ProcessActions.RunApplication("Cleaning up devices", "DeviceCleanup", "DeviceCleanupCmd.exe", "/s *"), null),

            // clean up drive
            (async () => await ProcessActions.RunApplication("Cleaning up drives", "DriveCleanup", "DriveCleanup.exe", ""), null),

            // disable bluetooth services and drivers
            (async () => await ProcessActions.DisableBluetoothServicesAndDrivers("Disabling Bluetooth services and drivers"), () => Bluetooth == false)
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
