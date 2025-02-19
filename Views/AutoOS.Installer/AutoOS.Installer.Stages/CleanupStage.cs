using AutoOS.Views.Installer.Actions;
using Microsoft.UI.Xaml.Media;
using Windows.UI;
using Microsoft.Win32;
using System.Diagnostics;

namespace AutoOS.Views.Installer.Stages;

public static class CleanupStage
{
    public static async Task Run()
    {
        InstallPage.Status.Text = "Cleaning up...";

        int validActionsCount = 0;
        int stagePercentage = 5;

        var actions = new List<(Func<Task> Action, Func<bool> Condition)>
        {
            // clean the winsxs folder
            (async () => await ProcessActions.RunNsudo("Cleaning the WinSxS folder", "CurrentUser", @"DISM /Online /Cleanup-Image /StartComponentCleanup /ResetBase"), null),

            // clean temp directories
            (async () => await ProcessActions.RunNsudo("Cleaning temp directories", "TrustedInstaller", @"cmd /c del /s /f /q ""C:\Windows\Logs"""), null),
            (async () => await ProcessActions.RunNsudo("Cleaning temp directories", "TrustedInstaller", @"cmd /c del /s /f /q ""C:\Windows\SoftwareDistribution"""), null),
            (async () => await ProcessActions.RunNsudo("Cleaning temp directories", "TrustedInstaller", @"cmd /c del /s /f /q ""C:\Windows\System32\LogFiles\*.*"""), null),
            (async () => await ProcessActions.RunNsudo("Cleaning temp directories", "TrustedInstaller", @"cmd /c del /s /f /q ""C:\Windows\System32\SleepStudy\*.*"""), null),
            (async () => await ProcessActions.RunNsudo("Cleaning temp directories", "TrustedInstaller", @"cmd /c del /s /f /q ""C:\Windows\System32\sru"""), null),
            (async () => await ProcessActions.RunNsudo("Cleaning temp directories", "TrustedInstaller", @"cmd /c del /s /f /q ""C:\Windows\System32\WDI\*.*"""), null),
            (async () => await ProcessActions.RunNsudo("Cleaning temp directories", "TrustedInstaller", @"cmd /c del /s /f /q ""C:\Windows\System32\winevt\Logs\*.*"""), null),
            (async () => await ProcessActions.RunNsudo("Cleaning temp directories", "TrustedInstaller", @"cmd /c del /s /f /q ""C:\Windows\SystemTemp\*.*"""), null),
            (async () => await ProcessActions.RunNsudo("Cleaning temp directories", "TrustedInstaller", @"cmd /c del /s /f /q ""C:\Windows\Temp\*.*"""), null),
            (async () => await ProcessActions.RunNsudo("Cleaning temp directories", "CurrentUser", @"cmd /c del /s /f /q %temp%\*.*"), null),
            (async () => await ProcessActions.RunNsudo("Cleaning temp directories", "CurrentUser", @"cmd /c rd /s /q %temp%"), null),
            (async () => await ProcessActions.RunNsudo("Cleaning temp directories", "CurrentUser", @"cmd /c md %temp%"), null),

            // clean event logs
            (async () => await ProcessActions.RunPowerShell("Cleaning event logs", @"Get-EventLog -LogName * | ForEach-Object { Clear-EventLog $_.Log }"), null),

            // run disk cleanup
            (async () => await ProcessActions.RunNsudo("Running disk cleanup", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Active Setup Temp Folders"" /v StateFlags0000 /t REG_DWORD /d 2 /f"), null),
            (async () => await ProcessActions.RunNsudo("Running disk cleanup", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\BranchCache"" /v StateFlags0000 /t REG_DWORD /d 2 /f"), null),
            (async () => await ProcessActions.RunNsudo("Running disk cleanup", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Content Indexer Cleaner"" /v StateFlags0000 /t REG_DWORD /d 2 /f"), null),
            (async () => await ProcessActions.RunNsudo("Running disk cleanup", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Delivery Optimization Files"" /v StateFlags0000 /t REG_DWORD /d 2 /f"), null),
            (async () => await ProcessActions.RunNsudo("Running disk cleanup", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Device Driver Packages"" /v StateFlags0000 /t REG_DWORD /d 2 /f"), null),
            (async () => await ProcessActions.RunNsudo("Running disk cleanup", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Diagnostic Data Viewer database files"" /v StateFlags0000 /t REG_DWORD /d 2 /f"), null),
            (async () => await ProcessActions.RunNsudo("Running disk cleanup", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Downloaded Program Files"" /v StateFlags0000 /t REG_DWORD /d 2 /f"), null),
            (async () => await ProcessActions.RunNsudo("Running disk cleanup", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Feedback Hub Archive log files"" /v StateFlags0000 /t REG_DWORD /d 2 /f"), null),
            (async () => await ProcessActions.RunNsudo("Running disk cleanup", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Internet Cache Files"" /v StateFlags0000 /t REG_DWORD /d 2 /f"), null),
            (async () => await ProcessActions.RunNsudo("Running disk cleanup", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Language Pack"" /v StateFlags0000 /t REG_DWORD /d 2 /f"), null),
            (async () => await ProcessActions.RunNsudo("Running disk cleanup", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Offline Pages Files"" /v StateFlags0000 /t REG_DWORD /d 2 /f"), null),
            (async () => await ProcessActions.RunNsudo("Running disk cleanup", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Old ChkDsk Files"" /v StateFlags0000 /t REG_DWORD /d 2 /f"), null),
            (async () => await ProcessActions.RunNsudo("Running disk cleanup", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\RetailDemo Offline Content"" /v StateFlags0000 /t REG_DWORD /d 2 /f"), null),
            (async () => await ProcessActions.RunNsudo("Running disk cleanup", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Setup Log Files"" /v StateFlags0000 /t REG_DWORD /d 2 /f"), null),
            (async () => await ProcessActions.RunNsudo("Running disk cleanup", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\System error memory dump files"" /v StateFlags0000 /t REG_DWORD /d 2 /f"), null),
            (async () => await ProcessActions.RunNsudo("Running disk cleanup", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\System error minidump files"" /v StateFlags0000 /t REG_DWORD /d 2 /f"), null),
            (async () => await ProcessActions.RunNsudo("Running disk cleanup", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Temporary Files"" /v StateFlags0000 /t REG_DWORD /d 2 /f"), null),
            (async () => await ProcessActions.RunNsudo("Running disk cleanup", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Temporary Setup Files"" /v StateFlags0000 /t REG_DWORD /d 2 /f"), null),
            (async () => await ProcessActions.RunNsudo("Running disk cleanup", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Thumbnail Cache"" /v StateFlags0000 /t REG_DWORD /d 2 /f"), null),
            (async () => await ProcessActions.RunNsudo("Running disk cleanup", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Update Cleanup"" /v StateFlags0000 /t REG_DWORD /d 2 /f"), null),
            (async () => await ProcessActions.RunNsudo("Running disk cleanup", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Upgrade Discarded Files"" /v StateFlags0000 /t REG_DWORD /d 2 /f"), null),
            (async () => await ProcessActions.RunNsudo("Running disk cleanup", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\User file versions"" /v StateFlags0000 /t REG_DWORD /d 2 /f"), null),
            (async () => await ProcessActions.RunNsudo("Running disk cleanup", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Windows Defender"" /v StateFlags0000 /t REG_DWORD /d 2 /f"), null),
            (async () => await ProcessActions.RunNsudo("Running disk cleanup", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Windows Error Reporting Files"" /v StateFlags0000 /t REG_DWORD /d 2 /f"), null),
            (async () => await ProcessActions.RunNsudo("Running disk cleanup", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Windows ESD installation files"" /v StateFlags0000 /t REG_DWORD /d 2 /f"), null),
            (async () => await ProcessActions.RunNsudo("Running disk cleanup", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Windows Reset Log Files"" /v StateFlags0000 /t REG_DWORD /d 2 /f"), null),
            (async () => await ProcessActions.RunNsudo("Running disk cleanup", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Windows Upgrade Log Files"" /v StateFlags0000 /t REG_DWORD /d 2 /f"), null),
            (async () => await ProcessActions.RunCustom("Running disk cleanup", async () => await Task.Run(() => Process.Start(new ProcessStartInfo { FileName = @"C:\Windows\System32\cleanmgr", Arguments = "/sagerun:0" })!.WaitForExitAsync())), null),

            // write stage
            (async () => await ProcessActions.RunCustom("Running disk cleanup", async () => await Task.Run(() => Registry.SetValue(@"HKEY_CURRENT_USER\SOFTWARE\AutoOS", "Stage", "Installed", RegistryValueKind.String))), null)
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
