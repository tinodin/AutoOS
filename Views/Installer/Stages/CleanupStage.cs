using AutoOS.Views.Installer.Actions;
using Microsoft.UI.Xaml.Media;
using Microsoft.Win32;
using System.Diagnostics;

namespace AutoOS.Views.Installer.Stages;

public static class CleanupStage
{
    public static async Task Run()
    {
        InstallPage.Status.Text = "Cleaning up...";

        string previousTitle = string.Empty;
        int stagePercentage = 2;

        var actions = new List<(string Title, Func<Task> Action, Func<bool> Condition)>
        {
            // clean up devices
            ("Cleaning up devices", async () => await ProcessActions.RunApplication("DeviceCleanup", "DeviceCleanupCmd.exe", "/s *"), null),

            // clean up drives
            ("Cleaning up drives", async () => await ProcessActions.RunApplication("DriveCleanup", "DriveCleanup.exe", ""), null),

            // clean temp directories
            ("Cleaning temp directories", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"cmd /c del /s /f /q ""C:\Windows\Logs"""), null),
            ("Cleaning temp directories", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"cmd /c del /s /f /q ""C:\Windows\Panther"""), null),
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
            ("Running disk cleanup", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Windows Upgrade Log Files"" /v StateFlags0000 /t REG_DWORD /d 2 /f"), null),
            ("Running disk cleanup", async () => await ProcessActions.RunCustom(async () => await Task.Run(() => Process.Start(new ProcessStartInfo { FileName = @"C:\Windows\System32\cleanmgr", Arguments = "/sagerun:0" })!.WaitForExitAsync())), null),

            // write stage
            ("Running disk cleanup", async () => await ProcessActions.RunCustom(async () => await Task.Run(() => Registry.SetValue(@"HKEY_CURRENT_USER\SOFTWARE\AutoOS", "Stage", "Installed", RegistryValueKind.String))), null)
        };

        var filteredActions = actions.Where(a => a.Condition == null || a.Condition.Invoke()).ToList();
        int groupedTitleCount = 0;

        List<Func<Task>> currentGroup = new();

        for (int i = 0; i < filteredActions.Count; i++)
        {
            if (i == 0 || filteredActions[i].Title != filteredActions[i - 1].Title)
            {
                groupedTitleCount++;
            }
        }

        double incrementPerTitle = groupedTitleCount > 0 ? stagePercentage / (double)groupedTitleCount : 0;

        foreach (var (title, action, condition) in filteredActions)
        {
            if (previousTitle != string.Empty && previousTitle != title && currentGroup.Count > 0)
            {
                foreach (var groupedAction in currentGroup)
                {
                    try
                    {
                        await groupedAction();
                    }
                    catch (Exception ex)
                    {
                        InstallPage.Info.Title += ": " + ex.Message;
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
                            InstallPage.Progress.Foreground = (Brush)Application.Current.Resources["AccentForegroundBrush"];
                            InstallPage.ProgressRingControl.Foreground = null;
                            InstallPage.ProgressRingControl.Visibility = Visibility.Visible;
                            InstallPage.ResumeButton.Visibility = Visibility.Collapsed;
                        };

                        await tcs.Task;
                    }
                }

                InstallPage.Progress.Value += incrementPerTitle;
                await Task.Delay(150);
                currentGroup.Clear();
            }

            InstallPage.Info.Title = title + "...";
            currentGroup.Add(action);
            previousTitle = title;
        }

        if (currentGroup.Count > 0)
        {
            foreach (var groupedAction in currentGroup)
            {
                try
                {
                    await groupedAction();
                }
                catch (Exception ex)
                {
                    InstallPage.Info.Title += ": " + ex.Message;
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
                        InstallPage.Progress.Foreground = (Brush)Application.Current.Resources["AccentForegroundBrush"];
                        InstallPage.ProgressRingControl.Foreground = null;
                        InstallPage.ProgressRingControl.Visibility = Visibility.Visible;
                        InstallPage.ResumeButton.Visibility = Visibility.Collapsed;
                    };

                    await tcs.Task;
                }
            }

            InstallPage.Progress.Value += incrementPerTitle;
        }

        InstallPage.Status.Text = "Installation finished.";
        InstallPage.Info.Severity = InfoBarSeverity.Success;
        InstallPage.Progress.Foreground = new SolidColorBrush((Windows.UI.Color)Application.Current.Resources["SystemFillColorSuccess"]);
        InstallPage.ProgressRingControl.Foreground = new SolidColorBrush((Windows.UI.Color)Application.Current.Resources["SystemFillColorSuccess"]);
        await ProcessActions.RunRestart();
    }
}