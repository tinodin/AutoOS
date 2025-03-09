using AutoOS.Views.Installer.Actions;
using Microsoft.Win32;
using System.Diagnostics;

namespace AutoOS.Views.Installer.Stages;

public static class TimerStage
{
    public static async Task Run()
    {
        InstallPage.Status.Text = "Configuring Timer Resolution...";

        string previousTitle = string.Empty;
        int stagePercentage = 5;

        var actions = new List<(string Title, Func<Task> Action, Func<bool> Condition)>
        {
            // run benchmark


            // determine best timer resolution


            // apply manually
            ("Applying Timer Resolution " + Registry.GetValue(@"HKEY_CURRENT_USER\SOFTWARE\AutoOS", "RequestedResolution", null)?.ToString(), async () => await ProcessActions.RunCustom(async () => await Task.Run(() => Process.Start(new ProcessStartInfo { FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "TimerResolution", "SetTimerResolution.exe"), Arguments = "--resolution " + Registry.GetValue(@"HKEY_CURRENT_USER\SOFTWARE\AutoOS", "RequestedResolution", null)?.ToString() + " --no-console", CreateNoWindow = true }))), null),
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
