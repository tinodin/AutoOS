using AutoOS.Views.Installer.Actions;
using Microsoft.UI.Xaml.Media;
using Windows.UI;
using Microsoft.Win32;
using System.Diagnostics;

namespace AutoOS.Views.Installer.Stages;

public static class ServicesStage
{
    public static async Task Run()
    {
        InstallPage.Status.Text = "Configuring Services and Drivers...";

        int validActionsCount = 0;
        int stagePercentage = 2;

        string folderName = "";

        var actions = new List<(Func<Task> Action, Func<bool> Condition)>
        {
            // group services
            (async () => await ProcessActions.RunNsudo("Grouping services", "TrustedInstaller", $@"powershell.exe -file ""{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Scripts", "groupservices.ps1")}"""), null),  

            // set failure actions
            (async () => await ProcessActions.RunNsudo("Disabling failure actions", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\SoftwareProtectionPlatform"" /v ""InactivityShutdownDelay"" /t REG_DWORD /d 4294967295 /f"), null),
            (async () => await ProcessActions.RunNsudo("Disabling failure actions", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Appinfo"" /v ""FailureActions"" /t REG_BINARY /d 00000000000000000000000003000000010000000000000001000000000000000000000000000000 /f"), null),
            (async () => await ProcessActions.RunNsudo("Disabling failure actions", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\AppXSvc"" /v ""FailureActions"" /t REG_BINARY /d 00000000000000000000000003000000010000000000000001000000000000000000000000000000 /f"), null),
            (async () => await ProcessActions.RunNsudo("Disabling failure actions", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\CryptSvc"" /v ""FailureActions"" /t REG_BINARY /d 00000000000000000000000003000000010000000000000001000000000000000000000000000000 /f"), null),
            (async () => await ProcessActions.RunNsudo("Disabling failure actions", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\gpsvc"" /v ""FailureActions"" /t REG_BINARY /d 00000000000000000000000003000000010000000000000001000000000000000000000000000000 /f"), null),
            (async () => await ProcessActions.RunNsudo("Disabling failure actions", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\netprofm"" /v ""FailureActions"" /t REG_BINARY /d 00000000000000000000000003000000010000000000000001000000000000000000000000000000 /f"), null),
            (async () => await ProcessActions.RunNsudo("Disabling failure actions", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\nsi"" /v ""FailureActions"" /t REG_BINARY /d 00000000000000000000000003000000010000000000000001000000000000000000000000000000 /f"), null),
            (async () => await ProcessActions.RunNsudo("Disabling failure actions", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\ProfSvc"" /v ""FailureActions"" /t REG_BINARY /d 00000000000000000000000003000000010000000000000001000000000000000000000000000000 /f"), null),
            (async () => await ProcessActions.RunNsudo("Disabling failure actions", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\sppsvc"" /v ""FailureActions"" /t REG_BINARY /d 00000000000000000000000003000000010000000000000001000000000000000000000000000000 /f"), null),
            (async () => await ProcessActions.RunNsudo("Disabling failure actions", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\StateRepository"" /v ""FailureActions"" /t REG_BINARY /d 00000000000000000000000003000000010000000000000001000000000000000000000000000000 /f"), null),
            (async () => await ProcessActions.RunNsudo("Disabling failure actions", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\TextInputManagementService"" /v ""FailureActions"" /t REG_BINARY /d 00000000000000000000000003000000010000000000000001000000000000000000000000000000 /f"), null),
            (async () => await ProcessActions.RunNsudo("Disabling failure actions", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Winmgmt"" /v ""FailureActions"" /t REG_BINARY /d 00000000000000000000000003000000010000000000000001000000000000000000000000000000 /f"), null),

            // build service lists
            (async () => await ProcessActions.RunNsudo("Building service lists", "TrustedInstaller", $@"""{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "Service-list-builder", "service-list-builder.exe")}"" --config ""{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "Service-list-builder", "lists.ini")}"" --disable-service-warning"), null),
            (async () => await ProcessActions.RunCustom("Building service lists", async () => { folderName = Directory.GetDirectories(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "Service-list-builder", "build")).OrderByDescending(d => Directory.GetLastWriteTime(d)).FirstOrDefault()?.Split('\\').Last(); }), null),


            // disable services and drivers
            (async () => await ProcessActions.RunNsudo("Disabling services and drivers", "TrustedInstaller", Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "Service-list-builder", "build", folderName, "Services-Disable.bat")), null),

            // write stage
            (async () => await ProcessActions.RunCustom("Building service lists", async () => await Task.Run(() => Registry.SetValue(@"HKEY_CURRENT_USER\SOFTWARE\AutoOS", "Stage", 3, RegistryValueKind.DWord))), null),

            // restart
            (async () => await ProcessActions.RunRestart(), null)
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
