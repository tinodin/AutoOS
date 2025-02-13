using AutoOS.Views.Installer.Actions;
using Microsoft.UI.Xaml.Media;
using Windows.UI;
using Microsoft.Win32;

namespace AutoOS.Views.Installer.Stages;

public static class SchedulingStage
{
    public static async Task Run()
    {
        InstallPage.Status.Text = "Configuring Affinities...";

        int validActionsCount = 0;
        int stagePercentage = 5;

        var actions = new List<(Func<Task> Action, Func<bool> Condition)>
        {
            // download windows adk
            (async () => await ProcessActions.RunDownload("Downloading Windows ADK", "https://go.microsoft.com/fwlink/?linkid=2289980", Path.GetTempPath(), "adksetup.exe"), null),

            // install windows performance toolkit
            (async () => await ProcessActions.RunNsudo("Installing Windows Performance Toolkit", "CurrentUser", @"cmd /c ""%TEMP%\adksetup.exe"" /features OptionId.WindowsPerformanceToolkit /quiet"), null),

            // run autogpuaffinity


            // determine best gpu cpu


            // reserve gpu cpu


            // determine best xhci cpu


            // reserve xhci cpu


            // apply manually
            (async () => await ProcessActions.RunNsudo("Applying GPU Affinity to CPU " + Registry.GetValue(@"HKEY_CURRENT_USER\SOFTWARE\AutoOS", "GpuAffinity", null)?.ToString(), "CurrentUser", $"\"{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "AutoGpuAffinity", "AutoGpuAffinity.exe")}\" --apply-affinity {Registry.GetValue(@"HKEY_CURRENT_USER\SOFTWARE\AutoOS", "GpuAffinity", null)?.ToString()}"), null),


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
