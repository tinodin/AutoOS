using AutoOS.Views.Installer.Actions;
using Microsoft.Win32;
using System.Management;
using Microsoft.UI.Xaml.Media;

namespace AutoOS.Views.Installer.Stages;

public static class SchedulingStage
{
    public static async Task Run()
    {
        bool? Scheduling = PreparingStage.Scheduling;
        bool? Hyperthreading = PreparingStage.Hyperthreading;
        bool? MSI = PreparingStage.MSI;
        bool? Reserve = PreparingStage.Reserve;

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
            ("Configuring AutoGpuAffinity", async () => await ProcessActions.RunNsudo("CurrentUser", $"cmd /c takeown /f \"{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "AutoGpuAffinity", "config.ini")}\" & icacls \"{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "AutoGpuAffinity", "config.ini")}\" /grant Everyone:F /T /C /Q"), null),
            ("Configuring AutoGpuAffinity", async () => await ProcessActions.RunCustom(async () => await Task.Run(() => File.WriteAllLines(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "AutoGpuAffinity", "config.ini"), File.ReadAllLines(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "AutoGpuAffinity", "config.ini")).Select(line => line.StartsWith("custom_cpus=") ? $"custom_cpus=[{string.Join(",", Enumerable.Range(2, Environment.ProcessorCount - 1).Where(i => i % 2 == 0 && i < Environment.ProcessorCount))}]" : line)))), () => Hyperthreading == true),
            ("Configuring AutoGpuAffinity", async () => await ProcessActions.RunCustom(async () => await Task.Run(() => File.WriteAllLines(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "AutoGpuAffinity", "config.ini"), File.ReadAllLines(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "AutoGpuAffinity", "config.ini")).Select(line => line.StartsWith("custom_cpus=") ? $"custom_cpus=[1..{Environment.ProcessorCount - 1}]" : line)))), () => Hyperthreading == false),
            ("Configuring AutoGpuAffinity", async () => await ProcessActions.RunCustom(async () => await Task.Run(() => File.WriteAllLines(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "AutoGpuAffinity", "config.ini"), File.ReadAllLines(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "AutoGpuAffinity", "config.ini")).Select(line => line.StartsWith("profile=") ? "profile=1" : line)))), () => MSI == true),

            // run autogpuaffinity


            // determine best gpu cpu


            // determine best xhci cpu


            // apply gpu affinity manually
            ("Applying GPU Affinity to CPU " + Registry.GetValue(@"HKEY_CURRENT_USER\SOFTWARE\AutoOS", "GpuAffinity", null)?.ToString(), async () => await ProcessActions.Sleep(500), () => Scheduling == false),
            ("Applying GPU Affinity to CPU " + Registry.GetValue(@"HKEY_CURRENT_USER\SOFTWARE\AutoOS", "GpuAffinity", null)?.ToString(), async () => await ProcessActions.RunNsudo("TrustedInstaller", $"cmd /c \"{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "AutoGpuAffinity", "AutoGpuAffinity.exe")}\" --apply-affinity {Registry.GetValue(@"HKEY_CURRENT_USER\SOFTWARE\AutoOS", "GpuAffinity", null)?.ToString()}"), () => Scheduling == false),

            // apply xhci affinity manually
            ("Applying XHCI Affinity to CPU " + Registry.GetValue(@"HKEY_CURRENT_USER\SOFTWARE\AutoOS", "XhciAffinity", null)?.ToString(), async () => await ProcessActions.Sleep(500), () => Scheduling == false),
            ("Applying XHCI Affinity to CPU " + Registry.GetValue(@"HKEY_CURRENT_USER\SOFTWARE\AutoOS", "XhciAffinity", null)?.ToString(), async () => await ProcessActions.RunCustom(async () => await Task.Run(() => { var query = "SELECT PNPDeviceID FROM Win32_USBController"; foreach (ManagementObject obj in new ManagementObjectSearcher(query).Get()) if (obj["PNPDeviceID"]?.ToString()?.StartsWith("PCI\\VEN_") == true) using (var key = Registry.LocalMachine.OpenSubKey($@"SYSTEM\CurrentControlSet\Enum\{obj["PNPDeviceID"]}\Device Parameters\Interrupt Management\Affinity Policy", true)) if (key != null && Registry.GetValue(@"HKEY_CURRENT_USER\SOFTWARE\AutoOS", "XhciAffinity", null) is int i && i >= 0) { key.SetValue("AssignmentSetOverride", new byte[(i / 8) + 1].Select((_, idx) => (byte)(idx == i / 8 ? 1 << (i % 8) : 0)).ToArray(), RegistryValueKind.Binary); key.SetValue("DevicePolicy", 4, RegistryValueKind.DWord); } })), () => Scheduling == false),

            // reserve cpus
            ("Reserving CPU " + Registry.GetValue(@"HKEY_CURRENT_USER\SOFTWARE\AutoOS", "GpuAffinity", null)?.ToString() + " and " + Registry.GetValue(@"HKEY_CURRENT_USER\SOFTWARE\AutoOS", "XhciAffinity", null)?.ToString(), async () => await ProcessActions.Sleep(500), () => Scheduling == false),
            ("Reserving CPU " + Registry.GetValue(@"HKEY_CURRENT_USER\SOFTWARE\AutoOS", "GpuAffinity", null)?.ToString() + " and " + Registry.GetValue(@"HKEY_CURRENT_USER\SOFTWARE\AutoOS", "XhciAffinity", null)?.ToString(), async () => await ProcessActions.RunCustom(async () => await Task.Run(() => { using (var key = Registry.LocalMachine.OpenSubKey("SYSTEM\\CurrentControlSet\\Control\\Session Manager\\kernel", true)) key.SetValue("ReservedCpuSets", BitConverter.GetBytes((long)(1 << (int)Registry.GetValue(@"HKEY_CURRENT_USER\SOFTWARE\AutoOS", "GpuAffinity", 0)) | (long)(1 << (int)Registry.GetValue(@"HKEY_CURRENT_USER\SOFTWARE\AutoOS", "XhciAffinity", 0))).Concat(new byte[8 - BitConverter.GetBytes((long)(1 << (int)Registry.GetValue(@"HKEY_CURRENT_USER\SOFTWARE\AutoOS", "GpuAffinity", 0)) | (long)(1 << (int)Registry.GetValue(@"HKEY_CURRENT_USER\SOFTWARE\AutoOS", "XhciAffinity", 0))).Length]).ToArray(), RegistryValueKind.Binary); })), () => Reserve == true),
        };

        var filteredActions = actions.Where(a => a.Condition == null || a.Condition.Invoke()).ToList();
        var uniqueTitles = filteredActions.Select(a => a.Title).Distinct().ToList();
        double incrementPerTitle = uniqueTitles.Count > 0 ? stagePercentage / (double)uniqueTitles.Count : 0;

        foreach (var title in uniqueTitles)
        {
            if (previousTitle != string.Empty && previousTitle != title)
            {
                await Task.Delay(2000);
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
                    InstallPage.Info.Title = InstallPage.Info.Title + ": " + ex.Message;
                    InstallPage.Info.Severity = InfoBarSeverity.Error;
                    InstallPage.Progress.Foreground = (Brush)Application.Current.Resources["SystemFillColorCriticalBrush"];
                    InstallPage.ProgressRingControl.Foreground = (Brush)Application.Current.Resources["SystemFillColorCriticalBrush"];
                    InstallPage.ProgressRingControl.Visibility = Visibility.Collapsed;
                    InstallPage.ResumeButton.Visibility = Visibility.Visible;

                    var tcs = new TaskCompletionSource<bool>();

                    InstallPage.ResumeButton.Click += (sender, e) =>
                    {
                        tcs.TrySetResult(true);
                        InstallPage.Info.Severity = InfoBarSeverity.Informational;
                        InstallPage.Progress.Foreground = ProcessActions.GetColor("LightNormal", "DarkNormal");
                        InstallPage.ProgressRingControl.Foreground = null;
                        InstallPage.ProgressRingControl.Visibility = Visibility.Visible;
                        InstallPage.ResumeButton.Visibility = Visibility.Collapsed;

                    };

                    await tcs.Task;
                }
            }

            InstallPage.Progress.Value += incrementPerTitle;

            previousTitle = title;
        }
    }
}
