using AutoOS.Views.Installer.Actions;

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
            // create toggle scripts
            ("Create toggle scripts", async () => await ProcessActions.RunNsudo("TrustedInstaller", $"reg export \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\WMI\\Autologger\" \"{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Scripts", "ets-enable.reg")}\""), null),
            ("Create toggle scripts", async () => await ProcessActions.Sleep(500), null),

            // disable event trace sessions
            ("Disabling Event Trace Sessions (ETS)", async () => await ProcessActions.RunNsudo("TrustedInstaller", $"cmd /c reg import \"{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Scripts", "ets-disable.reg")}\""), null),

            // disable sleep study
            ("Disabling sleep study.", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"cmd /c for %a in (""SleepStudy"" ""Kernel-Processor-Power"" ""UserModePowerService"") do (wevtutil sl Microsoft-Windows-%~a/Diagnostic /e:false)"), null),
            ("Create toggle scripts", async () => await ProcessActions.Sleep(500), null),
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
