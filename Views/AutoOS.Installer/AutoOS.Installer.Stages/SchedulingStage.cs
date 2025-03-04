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

        string previousTitle = string.Empty;
        int stagePercentage = 5;

        var actions = new List<(string Title, Func<Task> Action, Func<bool> Condition)>
        {
            // download windows adk
            ("Downloading Windows ADK", async () => await ProcessActions.RunDownload("https://go.microsoft.com/fwlink/?linkid=2289980", Path.GetTempPath(), "adksetup.exe"), null),

            // install windows performance toolkit
            ("Installing Windows Performance Toolkit", async () => await ProcessActions.RunNsudo("CurrentUser", @"cmd /c ""%TEMP%\adksetup.exe"" /features OptionId.WindowsPerformanceToolkit /quiet"), null),

            // configure autogpuaffinity
            ("Configuring AutoGpuAffinity", async () => await ProcessActions.RunNsudo("CurrentUser", $"cmd /c takeown /f \"{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "AutoGpuAffinity", "config.ini")}\" & icacls \"{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "AutoGpuAffinity", "config.ini")}\" /grant Everyone:F /T /C /Q)"), null),
            ("Configuring AutoGpuAffinity", async () => await ProcessActions.RunCustom(async () => await Task.Run(() => File.WriteAllLines(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "AutoGpuAffinity", "config.ini"), File.ReadAllLines(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "AutoGpuAffinity", "config.ini")).Select(line => line.StartsWith("custom_cpus=") ? $"custom_cpus=[{string.Join(",", Enumerable.Range(2, Environment.ProcessorCount - 1).Where(i => i % 2 == 0 && i < Environment.ProcessorCount))}]" : line)))), () => Hyperthreading == true),
            ("Configuring AutoGpuAffinity", async () => await ProcessActions.RunCustom(async () => await Task.Run(() => File.WriteAllLines(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "AutoGpuAffinity", "config.ini"), File.ReadAllLines(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "AutoGpuAffinity", "config.ini")).Select(line => line.StartsWith("custom_cpus=") ? $"custom_cpus=[1..{Environment.ProcessorCount - 1}]" : line)))), () => Hyperthreading == false),
            ("Configuring AutoGpuAffinity", async () => await ProcessActions.RunCustom(async () => await Task.Run(() => File.WriteAllLines(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "AutoGpuAffinity", "config.ini"), File.ReadAllLines(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "AutoGpuAffinity", "config.ini")).Select(line => line.StartsWith("profile=") ? "profile=1" : line)))), () => MSI == true),

            // run autogpuaffinity


            // determine best gpu cpu


            // reserve gpu cpu


            // determine best xhci cpu


            // reserve xhci cpu


            // apply manually
            ("Applying GPU Affinity to CPU " + Registry.GetValue(@"HKEY_CURRENT_USER\SOFTWARE\AutoOS", "GpuAffinity", null)?.ToString(), async () => await ProcessActions.RunNsudo("CurrentUser", $"\"{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "AutoGpuAffinity", "AutoGpuAffinity.exe")}\" --apply-affinity {Registry.GetValue(@"HKEY_CURRENT_USER\SOFTWARE\AutoOS", "GpuAffinity", null)?.ToString()}"), () => Scheduling == false),

            //("Applying Timer Resolution " + Registry.GetValue(@"HKEY_CURRENT_USER\SOFTWARE\AutoOS", "RequestedResolution", null)?.ToString(), async () => await ProcessActions.RunCustom(async () => await Task.Run(() => Process.Start(new ProcessStartInfo { FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "TimerResolution", "SetTimerResolution.exe"), Arguments = "--resolution " + Registry.GetValue(@"HKEY_CURRENT_USER\SOFTWARE\AutoOS", "RequestedResolution", null)?.ToString() + " --no-console", CreateNoWindow = true }))), null),

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
                InstallPage.Info.Title = actionTitle + "...";

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
