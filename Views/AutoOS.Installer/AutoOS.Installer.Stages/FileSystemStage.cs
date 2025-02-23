using AutoOS.Views.Installer.Actions;

namespace AutoOS.Views.Installer.Stages;

public static class FileSystemStage
{
    public static async Task Run()
    {
        InstallPage.Status.Text = "Configuring the file system...";

        int validActionsCount = 0;
        int stagePercentage = 2;

        var actions = new List<(Func<Task> Action, Func<bool> Condition)>
        {
            // disable the use of extended characters in short file names
            (async () => await ProcessActions.RunNsudo("Disabling the use of extended characters in short file names", "TrustedInstaller", @"fsutil behavior set allowextchar 0"), null),

            // disable automatic system crash when corruption is detected
            (async () => await ProcessActions.RunNsudo("Disabling automatic system crash when corruption is detected", "TrustedInstaller", @"fsutil behavior set Bugcheckoncorrupt 0"), null),

            // disable auto repair.
            (async () => await ProcessActions.RunNsudo("Disabling auto repair.", "TrustedInstaller", @"fsutil repair set C: 0"), null),

            // disable the creation of 8.3 character-length file names
            (async () => await ProcessActions.RunNsudo("Disabling the creation of 8.3 character-length file names", "TrustedInstaller", @"fsutil behavior set disable8dot3 1"), null),

            // disable ntfs file compression
            (async () => await ProcessActions.RunNsudo("Disabling NTFS file compression", "TrustedInstaller", @"fsutil behavior set disablecompression 1"), null),

            // disable ntfs file encryption using encrypting file system (efs)
            (async () => await ProcessActions.RunNsudo("Disabling NTFS file encryption using the Encrypting File System (EFS)", "TrustedInstaller", @"fsutil behavior set disableencryption 1"), null),

            // disable updates to the last access time stamp
            (async () => await ProcessActions.RunNsudo("Disabling updates to the last access time stamp", "TrustedInstaller", @"fsutil behavior set disablelastaccess 1"), null),

            // disable spot corruption handling
            (async () => await ProcessActions.RunNsudo("Disabling spot corruption handling", "TrustedInstaller", @"fsutil behavior set disablespotcorruptionhandling 1"), null),

            // disable encryption of the paging file
            (async () => await ProcessActions.RunNsudo("Disabling encryption of the paging file", "TrustedInstaller", @"fsutil behavior set encryptpagingfile 0"), null),

            // set disk quota limit notifications to 24 hours
            (async () => await ProcessActions.RunNsudo("Setting disk quota limit notifications to 24 hours", "TrustedInstaller", @"fsutil behavior set quotanotify 86400"), null),

            // enable local-to-local symbolic link evaluation
            (async () => await ProcessActions.RunNsudo("Enabling local-to-local symbolic link evaluation", "TrustedInstaller", @"fsutil behavior set symlinkevaluation L2L:1"), null),

            // disable the use of extended characters in short file names
            (async () => await ProcessActions.RunNsudo("Disabling the use of extended characters in short file names", "TrustedInstaller", @"fsutil behavior set allowextchar 0"), null),

            // enable trim support
            (async () => await ProcessActions.RunNsudo("Enabling TRIM support", "TrustedInstaller", @"fsutil behavior set disabledeletenotify 0"), null),
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
