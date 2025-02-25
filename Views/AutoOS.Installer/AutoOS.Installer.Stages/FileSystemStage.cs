using AutoOS.Views.Installer.Actions;

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
            ("Disabling the use of extended characters in short file names", async () => await ProcessActions.RunNsudo( "TrustedInstaller", @"fsutil behavior set allowextchar 0"), null),

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
                InstallPage.Info.Title = actionTitle;

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
