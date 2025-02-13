using AutoOS.Views.Installer.Actions;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace AutoOS.Views.Installer.Stages;

public static class BcdStage
{
    public static async Task Run()
    {
        InstallPage.Status.Text = "Configuring the BCD Store...";

        int validActionsCount = 0;
        int stagePercentage = 2;

        var actions = new List<(Func<Task> Action, Func<bool> Condition)>
        {
            // rename os to autoos
            (async () => await ProcessActions.RunNsudo("Renaming OS to AutoOS", "TrustedInstaller", @"bcdedit /set {current} description ""AutoOS"""), null),

            // force the legacy bootloader
            (async () => await ProcessActions.RunNsudo("Forcing the legacy bootloader", "TrustedInstaller", "bcdedit /set bootmenupolicy legacy"), null),

            // set the boot loader timeout to 5 seconds
            (async () => await ProcessActions.RunNsudo("Setting the bootloader timeout to 5 seconds", "TrustedInstaller", "bcdedit /timeout 5"), null),

            // disable automatic repair
            (async () => await ProcessActions.RunNsudo("Disabling automatic repair", "TrustedInstaller", "bcdedit /set {current} recoveryenabled No"), null),

            // disable dynamic tick
            (async () => await ProcessActions.RunNsudo("Disabling dynamic tick", "TrustedInstaller", "bcdedit /set disabledynamictick yes"), null),

            // force the use of the platform clock as system timer
            (async () => await ProcessActions.RunNsudo("Forcing the use of the platform clock as system timer", "TrustedInstaller", "bcdedit /deletevalue useplatformclock"), null),
            (async () => await ProcessActions.RunNsudo("Forcing the use of the platform clock as system timer", "TrustedInstaller", "bcdedit /set useplatformtick yes"), null),

            // set tsc sync policy to enhanced
            (async () => await ProcessActions.RunNsudo("Setting TSC Sync Policy to enhanced", "TrustedInstaller", "bcdedit /set tscsyncpolicy enhanced"), null),

            // disable kernel debugging
            (async () => await ProcessActions.RunNsudo("Disabling kernel debugging", "TrustedInstaller", "bcdedit /set debug No"), null),

            // disabling isolated context
            (async () => await ProcessActions.RunNsudo("Disabling isolated context", "TrustedInstaller", "bcdedit /set isolatedcontext No"), null),

            // disable emergency management services (ems)
            (async () => await ProcessActions.RunNsudo("Disables Emergency Management Services (EMS)", "TrustedInstaller", "bcdedit /set bootems No"), null),

            // disable elam drivers
            (async () => await ProcessActions.RunNsudo("Disabling ELAM drivers", "TrustedInstaller", "bcdedit /set disableelamdrivers Yes"), null),

            // disable the trusted platform module (tpm)
            (async () => await ProcessActions.RunNsudo("Disabling the Trusted Platform Module (TPM)", "TrustedInstaller", "bcdedit /set tpmbootentropy ForceDisable"), null),

            // disable the virtual secure mode
            (async () => await ProcessActions.RunNsudo("Disabling the Virtual Secure Mode (VSM)", "TrustedInstaller", "bcdedit /set vsmlaunchtype Off"), null),

            // disable windows virtualization features
            (async () => await ProcessActions.RunNsudo("Disabling windows virtualization features", "TrustedInstaller", "bcdedit /set vm No"), null),
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
