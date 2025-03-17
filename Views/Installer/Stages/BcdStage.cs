using AutoOS.Views.Installer.Actions;
using Microsoft.UI.Xaml.Media;

namespace AutoOS.Views.Installer.Stages;

public static class BcdStage
{
    public static async Task Run()
    {
        InstallPage.Status.Text = "Configuring the BCD Store...";

        string previousTitle = string.Empty;
        int stagePercentage = 2;

        var actions = new List<(string Title, Func<Task> Action, Func<bool> Condition)>
        {
            // rename os to autoos
            ("Renaming OS to AutoOS", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"bcdedit /set {current} description ""AutoOS"""), null),

            // force the legacy bootloader
            ("Forcing the legacy bootloader", async () => await ProcessActions.RunNsudo("TrustedInstaller", "bcdedit /set bootmenupolicy legacy"), null),

            // set the boot loader timeout to 6 seconds
            ("Setting the bootloader timeout to 6 seconds", async () => await ProcessActions.RunNsudo("TrustedInstaller", "bcdedit /timeout 6"), null),

            // disable automatic repair
            ("Disabling automatic repair", async () => await ProcessActions.RunNsudo("TrustedInstaller", "bcdedit /set {current} recoveryenabled No"), null),

            // disable dynamic tick
            ("Disabling dynamic tick", async () => await ProcessActions.RunNsudo("TrustedInstaller", "bcdedit /set disabledynamictick yes"), null),

            // force the use of the platform clock as system timer
            ("Forcing the use of the platform clock as system timer", async () => await ProcessActions.RunNsudo("TrustedInstaller", "bcdedit /deletevalue useplatformclock"), null),
            ("Forcing the use of the platform clock as system timer", async () => await ProcessActions.RunNsudo("TrustedInstaller", "bcdedit /set useplatformtick yes"), null),

            // set tsc sync policy to enhanced
            ("Setting TSC Sync Policy to enhanced", async () => await ProcessActions.RunNsudo("TrustedInstaller", "bcdedit /set tscsyncpolicy enhanced"), null),

            // disable kernel debugging
            ("Disabling kernel debugging", async () => await ProcessActions.RunNsudo("TrustedInstaller", "bcdedit /set debug No"), null),

            // disabling isolated context
            ("Disabling isolated context", async () => await ProcessActions.RunNsudo("TrustedInstaller", "bcdedit /set isolatedcontext No"), null),

            // disable emergency management services (ems)
            ("Disables Emergency Management Services (EMS)", async () => await ProcessActions.RunNsudo("TrustedInstaller", "bcdedit /set bootems No"), null),

            // disable elam drivers
            ("Disabling ELAM drivers", async () => await ProcessActions.RunNsudo("TrustedInstaller", "bcdedit /set disableelamdrivers Yes"), null),

            // disable trusted platform module (tpm) boot entropy
            ("Disabling Trusted Platform Module (TPM) boot entropy", async () => await ProcessActions.RunNsudo("TrustedInstaller", "bcdedit /set tpmbootentropy ForceDisable"), null),

            // disable the virtual secure mode
            ("Disabling the Virtual Secure Mode (VSM)", async () => await ProcessActions.RunNsudo("TrustedInstaller", "bcdedit /set vsmlaunchtype Off"), null),

            // disable windows virtualization features
            ("Disabling windows virtualization features", async () => await ProcessActions.RunNsudo("TrustedInstaller", "bcdedit /set vm No"), null),
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
