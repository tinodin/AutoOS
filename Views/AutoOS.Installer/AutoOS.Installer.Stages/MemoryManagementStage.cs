using AutoOS.Views.Installer.Actions;

namespace AutoOS.Views.Installer.Stages;

public static class MemoryManagementStage
{
    public static async Task Run()
    {
        bool? SSD = PreparingStage.SSD;

        InstallPage.Status.Text = "Configuring Memory Management...";

        string previousTitle = string.Empty;
        int stagePercentage = 2;

        var actions = new List<(string Title, Func<Task> Action, Func<bool> Condition)>
        {
            // disable application launch prefetching
            ("Disabling application launch prefetching", async () => await ProcessActions.RunPowerShell(@"Disable-MMAgent -ApplicationLaunchPrefetching"),() => SSD == true),

            // disable application pre launch
            ("Disabling application pre launch", async () => await ProcessActions.RunPowerShell(@"Disable-MMAgent -ApplicationPreLaunch"), null),

            // disable memory compression
            ("Disabling memory compression", async () => await ProcessActions.RunPowerShell(@"Disable-MMAgent -MemoryCompression"), null),

            // disable operation apu
            ("Disabling operation api", async () => await ProcessActions.RunPowerShell(@"Disable-MMAgent -OperationAPI"), null),

            // disable page combining
            ("Disabling page combining", async () => await ProcessActions.RunPowerShell(@"Disable-MMAgent -PageCombining"), null),
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
                InstallPage.Info.Title = actionTitle;

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
