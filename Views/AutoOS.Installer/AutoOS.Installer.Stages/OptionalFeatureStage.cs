using AutoOS.Views.Installer.Actions;
using Microsoft.UI.Xaml.Media;
using Windows.UI;
using Microsoft.Win32;

namespace AutoOS.Views.Installer.Stages;

public static class OptionalFeatureStage
{
    public static async Task Run()
    {
        InstallPage.Status.Text = "Configuring Optional Features...";

        int validActionsCount = 0;
        int stagePercentage = 2;

        var actions = new List<(Func<Task> Action, Func<bool> Condition)>
        {       
            // disable optional features
            (async () => await ProcessActions.DisableOptionalFeatures("Disabling optional features"), null),

            // remove windows capabilities
            (async () => await ProcessActions.RemoveWindowsCapabilities("Removing windows capabilities"), null),

            // write stage
            (async () => await ProcessActions.RunCustom("Removing windows capabilities", async () => await Task.Run(() => Registry.SetValue(@"HKEY_CURRENT_USER\SOFTWARE\AutoOS", "Stage", 2, RegistryValueKind.DWord))), null),

            // restart
            (async () => await ProcessActions.RunRestart(), null)
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
