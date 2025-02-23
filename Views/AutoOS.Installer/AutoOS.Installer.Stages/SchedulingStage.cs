using AutoOS.Views.Installer.Actions;
using Microsoft.Win32;
using System.Diagnostics;

namespace AutoOS.Views.Installer.Stages;

public static class SchedulingStage
{
    public static async Task Run()
    {
        bool? Scheduling = PreparingStage.Scheduling;
        bool? Hyperthreading = PreparingStage.Hyperthreading;
        bool? MSI = PreparingStage.MSI;

        InstallPage.Status.Text = "Configuring Affinities...";

        int validActionsCount = 0;
        int stagePercentage = 5;

        var actions = new List<(Func<Task> Action, Func<bool> Condition)>
        {
            // download windows adk
            (async () => await ProcessActions.RunDownload("Downloading Windows ADK", "https://go.microsoft.com/fwlink/?linkid=2289980", Path.GetTempPath(), "adksetup.exe"), null),

            // install windows performance toolkit
            (async () => await ProcessActions.RunNsudo("Installing Windows Performance Toolkit", "CurrentUser", @"cmd /c ""%TEMP%\adksetup.exe"" /features OptionId.WindowsPerformanceToolkit /quiet"), null),

            // configure autogpuaffinity
            (async () => await ProcessActions.RunNsudo("Configuring AutoGpuAffinity", "CurrentUser", $"cmd /c takeown /f \"{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "AutoGpuAffinity", "config.ini")}\" & icacls \"{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "AutoGpuAffinity", "config.ini")}\" /grant Everyone:F /T /C /Q)"), null),
            (async () => await ProcessActions.RunCustom("Configuring AutoGpuAffinity", async () => await Task.Run(() => File.WriteAllLines(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "AutoGpuAffinity", "config.ini"), File.ReadAllLines(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "AutoGpuAffinity", "config.ini")).Select(line => line.StartsWith("custom_cpus=") ? $"custom_cpus=[{string.Join(",", Enumerable.Range(2, Environment.ProcessorCount - 1).Where(i => i % 2 == 0 && i < Environment.ProcessorCount))}]" : line)))), () => Hyperthreading == true),
            (async () => await ProcessActions.RunCustom("Configuring AutoGpuAffinity", async () => await Task.Run(() => File.WriteAllLines(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "AutoGpuAffinity", "config.ini"), File.ReadAllLines(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "AutoGpuAffinity", "config.ini")).Select(line => line.StartsWith("custom_cpus=") ? $"custom_cpus=[1..{Environment.ProcessorCount - 1}]" : line)))), () => Hyperthreading == false),
            (async () => await ProcessActions.RunCustom("Configuring AutoGpuAffinity", async () => await Task.Run(() => File.WriteAllLines(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "AutoGpuAffinity", "config.ini"), File.ReadAllLines(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "AutoGpuAffinity", "config.ini")).Select(line => line.StartsWith("profile=") ? "profile=1" : line)))), () => MSI == true),

            // run autogpuaffinity


            // determine best gpu cpu


            // reserve gpu cpu


            // determine best xhci cpu


            // reserve xhci cpu


            // apply manually
            (async () => await ProcessActions.RunNsudo("Applying GPU Affinity to CPU " + Registry.GetValue(@"HKEY_CURRENT_USER\SOFTWARE\AutoOS", "GpuAffinity", null)?.ToString(), "CurrentUser", $"\"{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "AutoGpuAffinity", "AutoGpuAffinity.exe")}\" --apply-affinity {Registry.GetValue(@"HKEY_CURRENT_USER\SOFTWARE\AutoOS", "GpuAffinity", null)?.ToString()}"), () => Scheduling == false),

            //(async () => await ProcessActions.RunCustom("Applying Timer Resolution " + Registry.GetValue(@"HKEY_CURRENT_USER\SOFTWARE\AutoOS", "RequestedResolution", null)?.ToString(), async () => await Task.Run(() => Process.Start(new ProcessStartInfo { FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "TimerResolution", "SetTimerResolution.exe"), Arguments = "--resolution " + Registry.GetValue(@"HKEY_CURRENT_USER\SOFTWARE\AutoOS", "RequestedResolution", null)?.ToString() + " --no-console", CreateNoWindow = true }))), null),

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
                    InstallPage.ProgressRingControl.Foreground = ProcessActions.GetColor("LightError", "DarkError");
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
