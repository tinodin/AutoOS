﻿using AutoOS.Views.Installer.Actions;
using Microsoft.UI.Xaml.Media;
using Windows.Storage;

namespace AutoOS.Views.Installer.Stages;

public static class SchedulingStage
{
    private static readonly ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

    public static async Task Run()
    {
        bool? Scheduling = PreparingStage.Scheduling;
        int? CoreCount = PreparingStage.CoreCount;
        bool? Hyperthreading = PreparingStage.Hyperthreading;
        bool? MSI = PreparingStage.MSI;
        bool? Reserve = PreparingStage.Reserve;

        InstallPage.Status.Text = "Configuring Affinities...";

        string previousTitle = string.Empty;
        int stagePercentage = 10;

        var actions = new List<(string Title, Func<Task> Action, Func<bool> Condition)>
        {
            // configure autogpuaffinity
            ("Configuring AutoGpuAffinity", async () => await ProcessActions.RunCustom(async () => await Task.Run(() => { Directory.GetDirectories(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "AutoGpuAffinity"), "*", SearchOption.AllDirectories).ToList().ForEach(directory => Directory.CreateDirectory(directory.Replace(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "AutoGpuAffinity"), Path.Combine(PathHelper.GetAppDataFolderPath(), "AutoGpuAffinity")))); Directory.GetFiles(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "AutoGpuAffinity"), "*.*", SearchOption.AllDirectories).ToList().ForEach(file => File.Copy(file, file.Replace(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "AutoGpuAffinity"), Path.Combine(PathHelper.GetAppDataFolderPath(), "AutoGpuAffinity")), true)); })), null),
            ("Configuring AutoGpuAffinity", async () => await ProcessActions.RunCustom(async () => await Task.Run(() => File.WriteAllLines(Path.Combine(PathHelper.GetAppDataFolderPath(), "AutoGpuAffinity", "config.ini"), File.ReadAllLines(Path.Combine(PathHelper.GetAppDataFolderPath(), "AutoGpuAffinity", "config.ini")).Select(line => line.StartsWith("custom_cpus=") ? $"custom_cpus=[{string.Join(",", Enumerable.Range(2, Environment.ProcessorCount - 1).Where(i => i % 2 == 0 && i < Environment.ProcessorCount))}]" : line)))), () => Hyperthreading == true && CoreCount > 2),
            ("Configuring AutoGpuAffinity", async () => await ProcessActions.RunCustom(async () => await Task.Run(() => File.WriteAllLines(Path.Combine(PathHelper.GetAppDataFolderPath(), "AutoGpuAffinity", "config.ini"), File.ReadAllLines(Path.Combine(PathHelper.GetAppDataFolderPath(), "AutoGpuAffinity", "config.ini")).Select(line => line.StartsWith("custom_cpus=") ? $"custom_cpus=[1..{Environment.ProcessorCount - 1}]" : line)))), () => Hyperthreading == false && CoreCount > 2),
            ("Configuring AutoGpuAffinity", async () => await ProcessActions.RunCustom(async () => await Task.Run(() => File.WriteAllLines(Path.Combine(PathHelper.GetAppDataFolderPath(), "AutoGpuAffinity", "config.ini"), File.ReadAllLines(Path.Combine(PathHelper.GetAppDataFolderPath(), "AutoGpuAffinity", "config.ini")).Select(line => line.StartsWith("profile=") ? "profile=1" : line)))), () => MSI == true),
            
            // run autogpuaffinity
            ("Running AutoGpuAffinity", async () => await ProcessActions.RunAutoGpuAffinity(), () => Scheduling == true),

            // apply gpu affinity
            ("Applying GPU Affinity", async () => await ProcessActions.Sleep(1000), null),
            ("Applying GPU Affinity", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"cmd /c ""{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "AutoGpuAffinity", "AutoGpuAffinity.exe")}"" --apply-affinity {localSettings.Values["GpuAffinity"]}"), null),

            // apply xhci affinity
            ("Applying XHCI Affinity", async () => await ProcessActions.Sleep(2000), () => Scheduling == false),
            ("Applying XHCI Affinity", async () => await ProcessActions.ApplyXhciAffinity(), null),

            // apply nic affinity
            ("Applying NIC Affinity", async () => await ProcessActions.Sleep(2000), () => Scheduling == false),
            ("Applying NIC Affinity", async () => await ProcessActions.ApplyNicAffinity(), null),

            // reserve cpus
            ("Reserving CPUs", async () => await ProcessActions.Sleep(1000), () => Reserve == true),
            ("Reserving CPUs", async () => await ProcessActions.ReserveCpus(), () => Reserve == true)

        };

        var filteredActions = actions.Where(a => a.Condition == null || a.Condition.Invoke()).ToList();
        int groupedTitleCount = 0;

        List<Func<Task>> currentGroup = new();

        for (int i = 0; i < filteredActions.Count; i++)
        {
            if (i == 0 || filteredActions[i].Title != filteredActions[i - 1].Title)
            {
                groupedTitleCount++;
            }
        }

        double incrementPerTitle = groupedTitleCount > 0 ? stagePercentage / (double)groupedTitleCount : 0;

        foreach (var (title, action, condition) in filteredActions)
        {
            if (previousTitle != string.Empty && previousTitle != title && currentGroup.Count > 0)
            {
                foreach (var groupedAction in currentGroup)
                {
                    try
                    {
                        await groupedAction();
                    }
                    catch (Exception ex)
                    {
                        InstallPage.Info.Title += ": " + ex.Message;
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
                            InstallPage.Progress.Foreground = (Brush)Application.Current.Resources["AccentForegroundBrush"];
                            InstallPage.ProgressRingControl.Foreground = null;
                            InstallPage.ProgressRingControl.Visibility = Visibility.Visible;
                            InstallPage.ResumeButton.Visibility = Visibility.Collapsed;
                        };

                        await tcs.Task;
                    }
                }

                InstallPage.Progress.Value += incrementPerTitle;
                await Task.Delay(150);
                currentGroup.Clear();
            }

            InstallPage.Info.Title = title + "...";
            currentGroup.Add(action);
            previousTitle = title;
        }

        if (currentGroup.Count > 0)
        {
            foreach (var groupedAction in currentGroup)
            {
                try
                {
                    await groupedAction();
                }
                catch (Exception ex)
                {
                    InstallPage.Info.Title += ": " + ex.Message;
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
                        InstallPage.Progress.Foreground = (Brush)Application.Current.Resources["AccentForegroundBrush"];
                        InstallPage.ProgressRingControl.Foreground = null;
                        InstallPage.ProgressRingControl.Visibility = Visibility.Visible;
                        InstallPage.ResumeButton.Visibility = Visibility.Collapsed;
                    };

                    await tcs.Task;
                }
            }

            InstallPage.Progress.Value += incrementPerTitle;
        }
    }
}