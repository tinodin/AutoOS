using AutoOS.Views.Installer.Actions;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace AutoOS.Views.Installer.Stages;

public static class EventTraceSessionsStage
{
    public static async Task Run()
    {
        InstallPage.Status.Text = "Configuring Event Trace Sessions...";

        int validActionsCount = 0;
        int stagePercentage = 2;

        var actions = new List<(Func<Task> Action, Func<bool> Condition)>
        {
            // create toggle scripts
            (async () => await ProcessActions.RunNsudo("Create toggle scripts", "TrustedInstaller", $"reg export \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\WMI\\Autologger\" \"{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Scripts", "ets-enable.reg")}\""), null),
            (async () => await ProcessActions.Sleep("Create toggle scripts", 500), null),

            // disable event trace sessions
            (async () => await ProcessActions.RunNsudo("Disabling Event Trace Sessions (ETS)", "TrustedInstaller", $"cmd /c reg import \"{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Scripts", "ets-disable.reg")}\""), null),

            // disable sleep study
            (async () => await ProcessActions.RunNsudo("Disabling sleep study.", "TrustedInstaller", @"cmd /c for %a in (""SleepStudy"" ""Kernel-Processor-Power"" ""UserModePowerService"") do (wevtutil sl Microsoft-Windows-%~a/Diagnostic /e:false)"), null),
            (async () => await ProcessActions.Sleep("Create toggle scripts", 500), null),
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
