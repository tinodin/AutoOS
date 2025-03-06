using AutoOS.Views.Installer.Actions;
using Microsoft.Win32;
using System.Diagnostics;

namespace AutoOS.Views.Installer.Stages;

public static class TimerStage
{
    public static async Task Run()
    {
        bool? Reserve = PreparingStage.Reserve;

        InstallPage.Status.Text = "Configuring Timer Resolution...";

        string previousTitle = string.Empty;
        int stagePercentage = 5;

        var actions = new List<(string Title, Func<Task> Action, Func<bool> Condition)>
        {
            // run benchmark


            // determine best timer resolution


            // apply manually
            ("Applying Timer Resolution " + Registry.GetValue(@"HKEY_CURRENT_USER\SOFTWARE\AutoOS", "RequestedResolution", null)?.ToString(), async () => await ProcessActions.RunCustom(async () => await Task.Run(() => Process.Start(new ProcessStartInfo { FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "TimerResolution", "SetTimerResolution.exe"), Arguments = "--resolution " + Registry.GetValue(@"HKEY_CURRENT_USER\SOFTWARE\AutoOS", "RequestedResolution", null)?.ToString() + " --no-console", CreateNoWindow = true }))), null),
        
            // reserve cpus
            ("Reserving CPU " + Registry.GetValue(@"HKEY_CURRENT_USER\SOFTWARE\AutoOS", "GpuAffinity", null)?.ToString() + " and" + Registry.GetValue(@"HKEY_CURRENT_USER\SOFTWARE\AutoOS", "RequestedResolution", null)?.ToString(), async () => await ProcessActions.RunCustom(async () => await Task.Run(() => { using (var key = Registry.LocalMachine.OpenSubKey("SYSTEM\\CurrentControlSet\\Control\\Session Manager\\kernel", true)) key.SetValue("ReservedCpuSets", BitConverter.GetBytes((long)(1 << (int)Registry.GetValue(@"HKEY_CURRENT_USER\SOFTWARE\AutoOS", "GpuAffinity", 0)) | (long)(1 << (int)Registry.GetValue(@"HKEY_CURRENT_USER\SOFTWARE\AutoOS", "XhciAffinity", 0))).Concat(new byte[8 - BitConverter.GetBytes((long)(1 << (int)Registry.GetValue(@"HKEY_CURRENT_USER\SOFTWARE\AutoOS", "GpuAffinity", 0)) | (long)(1 << (int)Registry.GetValue(@"HKEY_CURRENT_USER\SOFTWARE\AutoOS", "XhciAffinity", 0))).Length]).ToArray(), RegistryValueKind.Binary); })), null),
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
