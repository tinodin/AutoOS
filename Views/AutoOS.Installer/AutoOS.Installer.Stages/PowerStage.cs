using AutoOS.Views.Installer.Actions;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace AutoOS.Views.Installer.Stages;

public static class PowerStage
{
    public static async Task Run()
    {
        bool? Desktop = PreparingStage.Desktop;
        bool? IdleStates = PreparingStage.IdleStates;
        bool? PowerService = PreparingStage.PowerService;

        InstallPage.Status.Text = "Configuring Power Options...";

        int validActionsCount = 0;
        int stagePercentage = 2;

        var actions = new List<(Func<Task> Action, Func<bool> Condition)>
        {
            // power plan
            (async () => await ProcessActions.RunNsudo("Switching to the high performance power plan", "CurrentUser", @"powercfg /setactive 8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c"), null),
            (async () => await ProcessActions.RunNsudo("Deleting balanced power scheme", "CurrentUser", @"powercfg /delete 381b4222-f694-41f0-9685-ff5bb260df2e"),() => Desktop == true),
            (async () => await ProcessActions.RunNsudo("Deleting power saver scheme", "CurrentUser", @"powercfg /delete a1841308-3541-4fab-bc81-f71556f20b4a"),() => Desktop == true),
            (async () => await ProcessActions.RunNsudo("Disabling USB 3 link power management", "CurrentUser", @"powercfg /setacvalueindex scheme_current 2a737441-1930-4402-8d77-b2bebba308a3 d4e98f31-5ffe-4ce1-be31-1b38b384c009 0"), null),
            (async () => await ProcessActions.RunNsudo("Disabling USB selective suspend", "CurrentUser", @"powercfg /setacvalueindex scheme_current 2a737441-1930-4402-8d77-b2bebba308a3 48e6b7a6-50f5-4782-a5d4-53bb8f07e226 0"), null),
            (async () => await ProcessActions.RunNsudo("Disabling CPU parking", "CurrentUser", @"powercfg /setacvalueindex scheme_current 54533251-82be-4824-96c1-47b60b740d00 0cc5b647-c1df-4637-891a-dec35c318583 100"), null),
            (async () => await ProcessActions.RunNsudo("Disabling CPU parking", "CurrentUser", @"powercfg /setacvalueindex scheme_current 54533251-82be-4824-96c1-47b60b740d00 0cc5b647-c1df-4637-891a-dec35c318584 100"), null),
            (async () => await ProcessActions.RunNsudo("Increasing the CPU performance time check interval to 5000", "CurrentUser", @"powercfg /setacvalueindex scheme_current 54533251-82be-4824-96c1-47b60b740d00 4d2b0152-7d5c-498b-88e2-34345392a2c5 5000"), null),
            (async () => await ProcessActions.RunNsudo("Disabling idle states", "CurrentUser", @"powercfg /setacvalueindex scheme_current sub_processor 5d76a2ca-e8c0-402f-a133-2158492d58ad 1"),() => IdleStates == false),
            (async () => await ProcessActions.RunNsudo("Saving the power plan configuration", "CurrentUser", @"powercfg /setactive scheme_current"), null),

            // disable power service
            (async () => await ProcessActions.RunNsudo("Disabling the power service", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Power"" /v ""Start"" /t REG_DWORD /d 4 /f"),() => PowerService == false),
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
