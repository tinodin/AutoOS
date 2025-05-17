using AutoOS.Views.Installer.Actions;
using Microsoft.UI.Xaml.Media;

namespace AutoOS.Views.Installer.Stages;

public static class FileSystemStage
{
    public static async Task Run()
    {
        InstallPage.Status.Text = "Configuring the file system...";

        string previousTitle = string.Empty;
        int stagePercentage = 2;

        var actions = new List<(string Title, Func<Task> Action, Func<bool> Condition)>
        {
            // disable the use of extended characters in short file names
            ("Disabling the use of extended characters in short file names", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"fsutil behavior set allowextchar 0"), null),

            // disable automatic system crash when corruption is detected
            ("Disabling automatic system crash when corruption is detected", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"fsutil behavior set Bugcheckoncorrupt 0"), null),

            // disable auto repair.
            ("Disabling auto repair.", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"fsutil repair set C: 0"), null),

            // disable the creation of 8.3 character-length file names
            ("Disabling the creation of 8.3 character-length file names", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"fsutil behavior set disable8dot3 1"), null),

            // disable ntfs file compression
            ("Disabling NTFS file compression", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"fsutil behavior set disablecompression 1"), null),

            // disable ntfs file encryption using encrypting file system (efs)
            ("Disabling NTFS file encryption using the Encrypting File System (EFS)", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"fsutil behavior set disableencryption 1"), null),

            // disable updates to the last access time stamp
            ("Disabling updates to the last access time stamp", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"fsutil behavior set disablelastaccess 1"), null),

            // disable spot corruption handling
            ("Disabling spot corruption handling", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"fsutil behavior set disablespotcorruptionhandling 1"), null),

            // disable encryption of the paging file
            ("Disabling encryption of the paging file", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"fsutil behavior set encryptpagingfile 0"), null),

            // set disk quota limit notifications to 24 hours
            ("Setting disk quota limit notifications to 24 hours", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"fsutil behavior set quotanotify 86400"), null),

            // enable local-to-local symbolic link evaluation
            ("Enabling local-to-local symbolic link evaluation", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"fsutil behavior set symlinkevaluation L2L:1"), null),

            // disable the use of extended characters in short file names
            ("Disabling the use of extended characters in short file names", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"fsutil behavior set allowextchar 0"), null),

            // enable trim support
            ("Enabling TRIM support", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"fsutil behavior set disabledeletenotify 0"), null),
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