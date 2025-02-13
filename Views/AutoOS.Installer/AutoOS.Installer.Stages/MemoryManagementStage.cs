using AutoOS.Views.Installer.Actions;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace AutoOS.Views.Installer.Stages;

public static class MemoryManagementStage
{
    public static async Task Run()
    {
        bool? SSD = PreparingStage.SSD;

        InstallPage.Status.Text = "Configuring Memory Management...";

        int validActionsCount = 0;
        int stagePercentage = 2;

        var actions = new List<(Func<Task> Action, Func<bool> Condition)>
        {
            // disable application launch prefetching
            (async () => await ProcessActions.RunPowerShell("Disabling application launch prefetching", @"Disable-MMAgent -ApplicationLaunchPrefetching"),() => SSD == true),

            // disable application pre launch
            (async () => await ProcessActions.RunPowerShell("Disabling application pre launch", @"Disable-MMAgent -ApplicationPreLaunch"), null),

            // disable memory compression
            (async () => await ProcessActions.RunPowerShell("Disabling memory compression", @"Disable-MMAgent -MemoryCompression"), null),

            // disable operation apu
            (async () => await ProcessActions.RunPowerShell("Disabling operation api", @"Disable-MMAgent -OperationAPI"), null),

            // disable page combining
            (async () => await ProcessActions.RunPowerShell("Disabling page combining", @"Disable-MMAgent -PageCombining"), null),
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
