using AutoOS.Views.Installer.Actions;
using Microsoft.Win32;

namespace AutoOS.Views.Installer.Stages;

public static class OptionalFeatureStage
{
    public static async Task Run()
    {
        InstallPage.Status.Text = "Configuring Optional Features...";

        string previousTitle = string.Empty;
        int stagePercentage = 2;

        var actions = new List<(string Title, Func<Task> Action, Func<bool> Condition)>
        {       
            // disable optional features
            ("Disabling optional features", async () => await ProcessActions.DisableOptionalFeatures(), null),

            // remove windows capabilities
            ("Removing windows capabilities", async () => await ProcessActions.RemoveWindowsCapabilities(), null),

            // write stage
            ("Removing windows capabilities", async () => await ProcessActions.RunCustom(async() => await Task.Run(() => Registry.SetValue(@"HKEY_CURRENT_USER\SOFTWARE\AutoOS", "Stage", 2, RegistryValueKind.DWord))), null),

            // restart
            ("", async () => await ProcessActions.RunRestart(), null)
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
