using AutoOS.Views.Installer.Actions;
using Microsoft.Win32;
using System.Diagnostics;

namespace AutoOS.Views.Installer.Stages;

public static class CleanupStage
{
    public static async Task Run()
    {
        InstallPage.Status.Text = "Cleaning up...";

        string previousTitle = string.Empty;
        int stagePercentage = 5;

        var actions = new List<(string Title, Func<Task> Action, Func<bool> Condition)>
        {
            // clean the winsxs folder
            ("Cleaning the WinSxS folder", async () => await ProcessActions.RunNsudo("CurrentUser", @"DISM /Online /Cleanup-Image /StartComponentCleanup /ResetBase"), null),

            // clean temp directories
            ("Cleaning temp directories", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"cmd /c del /s /f /q ""C:\Windows\Logs"""), null),
            ("Cleaning temp directories", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"cmd /c del /s /f /q ""C:\Windows\SoftwareDistribution"""), null),
            ("Cleaning temp directories", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"cmd /c del /s /f /q ""C:\Windows\System32\LogFiles\*.*"""), null),
            ("Cleaning temp directories", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"cmd /c del /s /f /q ""C:\Windows\System32\SleepStudy\*.*"""), null),
            ("Cleaning temp directories", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"cmd /c del /s /f /q ""C:\Windows\System32\sru"""), null),
            ("Cleaning temp directories", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"cmd /c del /s /f /q ""C:\Windows\System32\WDI\*.*"""), null),
            ("Cleaning temp directories", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"cmd /c del /s /f /q ""C:\Windows\System32\winevt\Logs\*.*"""), null),
            ("Cleaning temp directories", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"cmd /c del /s /f /q ""C:\Windows\SystemTemp\*.*"""), null),
            ("Cleaning temp directories", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"cmd /c del /s /f /q ""C:\Windows\Temp\*.*"""), null),
            ("Cleaning temp directories", async () => await ProcessActions.RunNsudo("CurrentUser", @"cmd /c del /s /f /q %temp%\*.*"), null),
            ("Cleaning temp directories", async () => await ProcessActions.RunNsudo("CurrentUser", @"cmd /c rd /s /q %temp%"), null),
            ("Cleaning temp directories", async () => await ProcessActions.RunNsudo("CurrentUser", @"cmd /c md %temp%"), null),

            // clean event logs
            ("Cleaning event logs", async () => await ProcessActions.RunPowerShell(@"Get-EventLog -LogName * | ForEach-Object { Clear-EventLog $_.Log }"), null),

            // run disk cleanup
            ("Running disk cleanup", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Active Setup Temp Folders"" /v StateFlags0000 /t REG_DWORD /d 2 /f"), null),
            ("Running disk cleanup", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\BranchCache"" /v StateFlags0000 /t REG_DWORD /d 2 /f"), null),
            ("Running disk cleanup", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Content Indexer Cleaner"" /v StateFlags0000 /t REG_DWORD /d 2 /f"), null),
            ("Running disk cleanup", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Delivery Optimization Files"" /v StateFlags0000 /t REG_DWORD /d 2 /f"), null),
            ("Running disk cleanup", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Device Driver Packages"" /v StateFlags0000 /t REG_DWORD /d 2 /f"), null),
            ("Running disk cleanup", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Diagnostic Data Viewer database files"" /v StateFlags0000 /t REG_DWORD /d 2 /f"), null),
            ("Running disk cleanup", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Downloaded Program Files"" /v StateFlags0000 /t REG_DWORD /d 2 /f"), null),
            ("Running disk cleanup", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Feedback Hub Archive log files"" /v StateFlags0000 /t REG_DWORD /d 2 /f"), null),
            ("Running disk cleanup", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Internet Cache Files"" /v StateFlags0000 /t REG_DWORD /d 2 /f"), null),
            ("Running disk cleanup", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Language Pack"" /v StateFlags0000 /t REG_DWORD /d 2 /f"), null),
            ("Running disk cleanup", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Offline Pages Files"" /v StateFlags0000 /t REG_DWORD /d 2 /f"), null),
            ("Running disk cleanup", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Old ChkDsk Files"" /v StateFlags0000 /t REG_DWORD /d 2 /f"), null),
            ("Running disk cleanup", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\RetailDemo Offline Content"" /v StateFlags0000 /t REG_DWORD /d 2 /f"), null),
            ("Running disk cleanup", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Setup Log Files"" /v StateFlags0000 /t REG_DWORD /d 2 /f"), null),
            ("Running disk cleanup", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\System error memory dump files"" /v StateFlags0000 /t REG_DWORD /d 2 /f"), null),
            ("Running disk cleanup", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\System error minidump files"" /v StateFlags0000 /t REG_DWORD /d 2 /f"), null),
            ("Running disk cleanup", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Temporary Files"" /v StateFlags0000 /t REG_DWORD /d 2 /f"), null),
            ("Running disk cleanup", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Temporary Setup Files"" /v StateFlags0000 /t REG_DWORD /d 2 /f"), null),
            ("Running disk cleanup", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Thumbnail Cache"" /v StateFlags0000 /t REG_DWORD /d 2 /f"), null),
            ("Running disk cleanup", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Update Cleanup"" /v StateFlags0000 /t REG_DWORD /d 2 /f"), null),
            ("Running disk cleanup", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Upgrade Discarded Files"" /v StateFlags0000 /t REG_DWORD /d 2 /f"), null),
            ("Running disk cleanup", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\User file versions"" /v StateFlags0000 /t REG_DWORD /d 2 /f"), null),
            ("Running disk cleanup", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Windows Defender"" /v StateFlags0000 /t REG_DWORD /d 2 /f"), null),
            ("Running disk cleanup", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Windows Error Reporting Files"" /v StateFlags0000 /t REG_DWORD /d 2 /f"), null),
            ("Running disk cleanup", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Windows ESD installation files"" /v StateFlags0000 /t REG_DWORD /d 2 /f"), null),
            ("Running disk cleanup", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Windows Reset Log Files"" /v StateFlags0000 /t REG_DWORD /d 2 /f"), null),
            ("Running disk cleanup", async () => await ProcessActions.RunNsudo( "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Windows Upgrade Log Files"" /v StateFlags0000 /t REG_DWORD /d 2 /f"), null),
            ("Running disk cleanup", async () => await ProcessActions.RunCustom(async () => await Task.Run(() => Process.Start(new ProcessStartInfo { FileName = @"C:\Windows\System32\cleanmgr", Arguments = "/sagerun:0" })!.WaitForExitAsync())), null),

            // write stage
            ("Running disk cleanup", async () => await ProcessActions.RunCustom(async() => await Task.Run(() => Registry.SetValue(@"HKEY_CURRENT_USER\SOFTWARE\AutoOS", "Stage", "Installed", RegistryValueKind.String))), null)
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
                    InstallPage.Progress.ShowError = true;
                    InstallPage.Info.Severity = InfoBarSeverity.Error;
                    InstallPage.ProgressRingControl.Foreground = ProcessActions.GetColor("LightError", "DarkError");
                    return;
                }
            }

            InstallPage.Progress.Value += incrementPerTitle;

            previousTitle = title;
        }
    }
}
