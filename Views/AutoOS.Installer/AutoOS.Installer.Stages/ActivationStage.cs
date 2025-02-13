using AutoOS.Views.Installer.Actions;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace AutoOS.Views.Installer.Stages;

public static class ActivationStage
{
    public static async Task Run()
    {
        InstallPage.Status.Text = "Activating Windows...";

        int validActionsCount = 0;
        int stagePercentage = 2;

        var actions = new List<(Func<Task> Action, Func<bool> Condition)>
        {
            // input the activation key
            (async () => await ProcessActions.RunNsudo("Inputting the activation key", "CurrentUser", "cmd /c slmgr //B /ipk W269N-WFGWX-YVC9B-4J6C9-T83GX"), null),

            // input the kms server
            (async () => await ProcessActions.RunNsudo("Inputting the KMS server", "CurrentUser", "cmd /c slmgr //B /skms kms8.msguides.com"), null),

            // activate windows
            (async () => await ProcessActions.RunNsudo("Activating Windows", "CurrentUser", "cmd /c slmgr //B /ato"), null),
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
