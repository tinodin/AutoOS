using AutoOS.Views.Installer.Actions;
using Microsoft.UI.Xaml.Media;
using Microsoft.Win32;

namespace AutoOS.Views.Installer.Stages;

public static class EventTraceSessionsStage
{
    public static async Task Run()
    {
        InstallPage.Status.Text = "Configuring Event Trace Sessions...";

        string previousTitle = string.Empty;
        int stagePercentage = 2;

        var actions = new List<(string Title, Func<Task> Action, Func<bool> Condition)>
        {
            // saving event trace session (ets) data
            ("Saving Event Trace Session (ETS) data", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg export ""HKLM\SYSTEM\CurrentControlSet\Control\WMI\Autologger"" ""C:\ets-enable.reg"""), null),
            ("Saving Event Trace Session (ETS) data", async () => await ProcessActions.RunCustom(async () => await Task.Run(() => Directory.CreateDirectory(Path.Combine(PathHelper.GetAppDataFolderPath(), "EventTraceSessions")))), null),
            ("Saving Event Trace Session (ETS) data", async () => await ProcessActions.RunNsudo("TrustedInstaller", @$"cmd /c move ""C:\ets-enable.reg"" ""{Path.Combine(PathHelper.GetAppDataFolderPath(), "EventTraceSessions", "ets-enable.reg")}"""), null),
            ("Saving Event Trace Session (ETS) data", async () => await ProcessActions.Sleep(500), null),

            // disable event trace sessions
            ("Disabling Event Trace Sessions (ETS)", async () => await ProcessActions.RunCustom(async () => await Task.Run(() => Registry.LocalMachine.DeleteSubKeyTree(@"SYSTEM\CurrentControlSet\Control\WMI\Autologger", false) )), null),
            ("Disabling Event Trace Sessions (ETS)", async () => await ProcessActions.Sleep(500), null),

            // disable sleep study
            ("Disabling sleep study", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"cmd /c for %a in (""SleepStudy"" ""Kernel-Processor-Power"" ""UserModePowerService"") do (wevtutil sl Microsoft-Windows-%~a/Diagnostic /e:false)"), null),
            ("Disabling sleep study", async () => await ProcessActions.Sleep(500), null),
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
                    InstallPage.Info.Title = InstallPage.Info.Title + ": " + ex.Message;
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
