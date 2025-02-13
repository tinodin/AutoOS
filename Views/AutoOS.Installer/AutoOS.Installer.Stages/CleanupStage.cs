using AutoOS.Views.Installer.Actions;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace AutoOS.Views.Installer.Stages;

public static class CleanupStage
{
    public static async Task Run()
    {
        InstallPage.Status.Text = "Cleaning up...";

        int validActionsCount = 0;
        int stagePercentage = 5;

        var actions = new List<(Func<Task> Action, Func<bool> Condition)>
        {


            // clean temp directories
            (async () => await ProcessActions.RunNsudo("Cleaning temp directories", "TrustedInstaller", @"cmd /c del /s /q ""C:\Windows\CbsTemp"""), null),
            //(async () => await ProcessActions.RunNsudo("Cleaning temp directories", "TrustedInstaller", @""), null),
            //(async () => await ProcessActions.RunNsudo("Cleaning temp directories", "TrustedInstaller", @""), null),
            //(async () => await ProcessActions.RunNsudo("Cleaning temp directories", "TrustedInstaller", @""), null),



            (async () => await ProcessActions.RunNsudo("Cleaning temp directories", "TrustedInstaller", @"cmd /c del /s /f /q %temp%\*.*"), null),
            (async () => await ProcessActions.RunNsudo("Cleaning temp directories", "TrustedInstaller", @"cmd /c rd /s /q %temp%"), null),
            (async () => await ProcessActions.RunNsudo("Cleaning temp directories", "TrustedInstaller", @"cmd /c md %temp%"), null),

            


            // clean the winsxs folder
            (async () => await ProcessActions.RunNsudo("Cleaning the WinSxs folder", "CurrentUser", @"DISM /Online /Cleanup-Image /StartComponentCleanup /ResetBase"), null),
             
            // clean event logs
            (async () => await ProcessActions.RunPowerShell("Cleaning Event Logs", @"Get-EventLog -LogName * | ForEach-Object { Clear-EventLog $_.Log }"), null),


            // restart (for xhci controller cpu affinity to apply + services refresh)

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
