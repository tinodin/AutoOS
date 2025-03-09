using AutoOS.Views.Installer.Actions;
using Microsoft.UI.Xaml.Media;

namespace AutoOS.Views.Installer.Stages;

public static class PowerStage
{
    public static async Task Run()
    {
        bool? Desktop = PreparingStage.Desktop;
        bool? IdleStates = PreparingStage.IdleStates;
        bool? PowerService = PreparingStage.PowerService;

        InstallPage.Status.Text = "Configuring Power Options...";

        string previousTitle = string.Empty;
        int stagePercentage = 2;

        var actions = new List<(string Title, Func<Task> Action, Func<bool> Condition)>
        {
            // power plan
            ("Switching to the high performance power plan", async () => await ProcessActions.RunNsudo("CurrentUser", @"powercfg /setactive 8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c"), null),
            ("Deleting balanced power scheme", async () => await ProcessActions.RunNsudo("CurrentUser", @"powercfg /delete 381b4222-f694-41f0-9685-ff5bb260df2e"), () => Desktop == true),
            ("Deleting power saver scheme", async () => await ProcessActions.RunNsudo("CurrentUser", @"powercfg /delete a1841308-3541-4fab-bc81-f71556f20b4a"), () => Desktop == true),
            ("Disabling USB 3 link power management", async () => await ProcessActions.RunNsudo("CurrentUser", @"powercfg /setacvalueindex scheme_current 2a737441-1930-4402-8d77-b2bebba308a3 d4e98f31-5ffe-4ce1-be31-1b38b384c009 0"), null),
            ("Disabling USB selective suspend", async () => await ProcessActions.RunNsudo("CurrentUser", @"powercfg /setacvalueindex scheme_current 2a737441-1930-4402-8d77-b2bebba308a3 48e6b7a6-50f5-4782-a5d4-53bb8f07e226 0"), null),
            ("Disabling CPU parking", async () => await ProcessActions.RunNsudo("CurrentUser", @"powercfg /setacvalueindex scheme_current 54533251-82be-4824-96c1-47b60b740d00 0cc5b647-c1df-4637-891a-dec35c318583 100"), null),
            ("Disabling CPU parking", async () => await ProcessActions.RunNsudo("CurrentUser", @"powercfg /setacvalueindex scheme_current 54533251-82be-4824-96c1-47b60b740d00 0cc5b647-c1df-4637-891a-dec35c318584 100"), null),
            ("Increasing the CPU performance time check interval to 5000", async () => await ProcessActions.RunNsudo("CurrentUser", @"powercfg /setacvalueindex scheme_current 54533251-82be-4824-96c1-47b60b740d00 4d2b0152-7d5c-498b-88e2-34345392a2c5 5000"), null),
            ("Disabling idle states", async () => await ProcessActions.RunNsudo("CurrentUser", @"powercfg /setacvalueindex scheme_current sub_processor 5d76a2ca-e8c0-402f-a133-2158492d58ad 1"), () => IdleStates == false),
            ("Saving the power plan configuration", async () => await ProcessActions.RunNsudo("CurrentUser", @"powercfg /setactive scheme_current"), null),

            // disable power service
            ("Disabling the power service", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Power"" /v ""Start"" /t REG_DWORD /d 4 /f"), () => PowerService == false)
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
