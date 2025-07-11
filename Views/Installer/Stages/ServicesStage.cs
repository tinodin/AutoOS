using AutoOS.Views.Installer.Actions;
using Microsoft.UI.Xaml.Media;

namespace AutoOS.Views.Installer.Stages;

public static class ServicesStage
{
    public static async Task Run()
    {
        InstallPage.Status.Text = "Configuring Services and Drivers...";

        string previousTitle = string.Empty;
        int stagePercentage = 2;

        var actions = new List<(string Title, Func<Task> Action, Func<bool> Condition)>
        {
            // group services
            ("Grouping services", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"powershell -ExecutionPolicy Bypass -file ""{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Scripts", "groupservices.ps1")}"""), null),  

            // set failure actions
            ("Disabling failure actions", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\SoftwareProtectionPlatform"" /v ""InactivityShutdownDelay"" /t REG_DWORD /d 4294967295 /f"), null),
            ("Disabling failure actions", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\AudioEndpointBuilder"" /v ""FailureActions"" /t REG_BINARY /d 00000000000000000000000003000000010000000000000001000000000000000000000000000000 /f"), null),
            ("Disabling failure actions", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Appinfo"" /v ""FailureActions"" /t REG_BINARY /d 00000000000000000000000003000000010000000000000001000000000000000000000000000000 /f"), null),
            ("Disabling failure actions", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\AppXSvc"" /v ""FailureActions"" /t REG_BINARY /d 00000000000000000000000003000000010000000000000001000000000000000000000000000000 /f"), null),
            ("Disabling failure actions", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\CaptureService"" /v ""FailureActions"" /t REG_BINARY /d 00000000000000000000000003000000010000000000000001000000000000000000000000000000 /f"), null),
            ("Disabling failure actions", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\cbdhsvc"" /v ""FailureActions"" /t REG_BINARY /d 00000000000000000000000003000000010000000000000001000000000000000000000000000000 /f"), null),
            ("Disabling failure actions", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\ClipSvc"" /v ""FailureActions"" /t REG_BINARY /d 00000000000000000000000003000000010000000000000001000000000000000000000000000000 /f"), null),
            ("Disabling failure actions", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\CryptSvc"" /v ""FailureActions"" /t REG_BINARY /d 00000000000000000000000003000000010000000000000001000000000000000000000000000000 /f"), null),
            ("Disabling failure actions", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\DevicesFlowUserSvc"" /v ""FailureActions"" /t REG_BINARY /d 00000000000000000000000003000000010000000000000001000000000000000000000000000000 /f"), null),
            ("Disabling failure actions", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\DoSvc"" /v ""FailureActions"" /t REG_BINARY /d 00000000000000000000000003000000010000000000000001000000000000000000000000000000 /f"), null),
            ("Disabling failure actions", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\DsmSvc"" /v ""FailureActions"" /t REG_BINARY /d 00000000000000000000000003000000010000000000000001000000000000000000000000000000 /f"), null),
            ("Disabling failure actions", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\gpsvc"" /v ""FailureActions"" /t REG_BINARY /d 00000000000000000000000003000000010000000000000001000000000000000000000000000000 /f"), null),
            ("Disabling failure actions", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\InstallService"" /v ""FailureActions"" /t REG_BINARY /d 00000000000000000000000003000000010000000000000001000000000000000000000000000000 /f"), null),
            ("Disabling failure actions", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\msiserver"" /v ""FailureActions"" /t REG_BINARY /d 00000000000000000000000003000000010000000000000001000000000000000000000000000000 /f"), null),
            ("Disabling failure actions", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\NcbService"" /v ""FailureActions"" /t REG_BINARY /d 00000000000000000000000003000000010000000000000001000000000000000000000000000000 /f"), null),
            ("Disabling failure actions", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\netprofm"" /v ""FailureActions"" /t REG_BINARY /d 00000000000000000000000003000000010000000000000001000000000000000000000000000000 /f"), null),
            ("Disabling failure actions", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\nsi"" /v ""FailureActions"" /t REG_BINARY /d 00000000000000000000000003000000010000000000000001000000000000000000000000000000 /f"), null),
            ("Disabling failure actions", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\ProfSvc"" /v ""FailureActions"" /t REG_BINARY /d 00000000000000000000000003000000010000000000000001000000000000000000000000000000 /f"), null),
            ("Disabling failure actions", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\sppsvc"" /v ""FailureActions"" /t REG_BINARY /d 00000000000000000000000003000000010000000000000001000000000000000000000000000000 /f"), null),
            ("Disabling failure actions", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\StateRepository"" /v ""FailureActions"" /t REG_BINARY /d 00000000000000000000000003000000010000000000000001000000000000000000000000000000 /f"), null),
            ("Disabling failure actions", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\TextInputManagementService"" /v ""FailureActions"" /t REG_BINARY /d 00000000000000000000000003000000010000000000000001000000000000000000000000000000 /f"), null),
            ("Disabling failure actions", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\TrustedInstaller"" /v ""FailureActions"" /t REG_BINARY /d 00000000000000000000000003000000010000000000000001000000000000000000000000000000 /f"), null),
            ("Disabling failure actions", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\UdkUserSvc"" /v ""FailureActions"" /t REG_BINARY /d 00000000000000000000000003000000010000000000000001000000000000000000000000000000 /f"), null),
            ("Disabling failure actions", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\UserManager"" /v ""FailureActions"" /t REG_BINARY /d 00000000000000000000000003000000010000000000000001000000000000000000000000000000 /f"), null),
            ("Disabling failure actions", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\WFDSConMgrSvc"" /v ""FailureActions"" /t REG_BINARY /d 00000000000000000000000003000000010000000000000001000000000000000000000000000000 /f"), null),
            ("Disabling failure actions", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Winmgmt"" /v ""FailureActions"" /t REG_BINARY /d 00000000000000000000000003000000010000000000000001000000000000000000000000000000 /f"), null),

            // build service lists
            ("Building service lists", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"""{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "Service-list-builder", "service-list-builder.exe")}"" --config ""{Path.Combine(PathHelper.GetAppDataFolderPath(), "Service-list-builder", "lists.ini")}"" --disable-service-warning --output-dir ""{Path.Combine(PathHelper.GetAppDataFolderPath(), "Service-list-builder", "build")}"), null)
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
    }
}