using AutoOS.Views.Installer.Actions;
namespace AutoOS.Views.Installer.Stages;

public static class SecurityStage
{
    public static async Task Run()
    {
        bool? WindowsDefender = PreparingStage.WindowsDefender;
        bool? UserAccountControl = PreparingStage.UserAccountControl;
        bool? DEP = PreparingStage.DEP;
        bool? INTELCPU = PreparingStage.INTELCPU;
        bool? AMDCPU = PreparingStage.AMDCPU;
        bool? SpectreMeltdownMitigations = PreparingStage.SpectreMeltdownMitigations;
        bool? ProcessMitigations = PreparingStage.ProcessMitigations;

        InstallPage.Status.Text = "Configuring Security...";

        int validActionsCount = 0;
        int stagePercentage = 2;

        var actions = new List<(Func<Task> Action, Func<bool> Condition)>
        {
            // optimize windows defender
            (async () => await ProcessActions.RunNsudo("Optimizing Windows Defender", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\WTDS\Components"" /v ServiceEnabled /t REG_DWORD /d 0 /f"), null),
            (async () => await ProcessActions.RunNsudo("Optimizing Windows Defender", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\MRT"" /v DontReportInfectionInformation /t REG_DWORD /d 1 /f"), null),
            (async () => await ProcessActions.RunNsudo("Optimizing Windows Defender", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity"" /v Enabled /t REG_DWORD /d 0 /f"), null),
            (async () => await ProcessActions.RunNsudo("Optimizing Windows Defender", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows Defender\Spynet"" /v SpyNetReporting /t REG_DWORD /d 0 /f"), null),
            (async () => await ProcessActions.RunNsudo("Optimizing Windows Defender", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows Defender\Spynet"" /v SubmitSamplesConsent /t REG_DWORD /d 0 /f"), null),
            (async () => await ProcessActions.RunNsudo("Optimizing Windows Defender", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\CI\Policy"" /v VerifiedAndReputablePolicyState /t REG_DWORD /d 0 /f"), null),
            (async () => await ProcessActions.RunNsudo("Optimizing Windows Defender", "CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\AppHost"" /v EnableWebContentEvaluation /t REG_DWORD /d 0 /f"), null),
            (async () => await ProcessActions.RunNsudo("Optimizing Windows Defender", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\Software\Policies\Microsoft\Windows Defender\Signature Updates"" /v SignatureDisableNotification /t REG_DWORD /d 0 /f"), null),
            (async () => await ProcessActions.RunNsudo("Optimizing Windows Defender", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\Software\Policies\Microsoft\Windows Defender\Signature Updates"" /v RealtimeSignatureDelivery /t REG_DWORD /d 0 /f"), null),
            (async () => await ProcessActions.RunNsudo("Optimizing Windows Defender", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\Software\Policies\Microsoft\Windows Defender\Signature Updates"" /v ForceUpdateFromMU /t REG_DWORD /d 0 /f"), null),
            (async () => await ProcessActions.RunNsudo("Optimizing Windows Defender", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\Software\Policies\Microsoft\Windows Defender\Signature Updates"" /v DisableScheduledSignatureUpdateOnBattery /t REG_DWORD /d 1 /f"), null),
            (async () => await ProcessActions.RunNsudo("Optimizing Windows Defender", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\Software\Policies\Microsoft\Windows Defender\Signature Updates"" /v UpdateOnStartUp /t REG_DWORD /d 0 /f"), null),
            (async () => await ProcessActions.RunNsudo("Optimizing Windows Defender", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\Software\Policies\Microsoft\Windows Defender\Signature Updates"" /v DisableUpdateOnStartupWithoutEngine /t REG_DWORD /d 1 /f"), null),
            (async () => await ProcessActions.RunNsudo("Optimizing Windows Defender", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\Software\Policies\Microsoft\Windows Defender\Signature Updates"" /v DisableScanOnUpdate /t REG_DWORD /d 1 /f"), null),
            (async () => await ProcessActions.RunNsudo("Optimizing Windows Defender", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\Software\Policies\Microsoft\Windows Defender\SmartScreen"" /v ConfigureAppInstallControlEnabled /t REG_DWORD /d 1 /f"), null),
            (async () => await ProcessActions.RunNsudo("Optimizing Windows Defender", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\Software\Policies\Microsoft\Windows Defender\SmartScreen"" /v ConfigureAppInstallControl /t REG_SZ /d Anywhere /f"), null),
            (async () => await ProcessActions.RunNsudo("Optimizing Windows Defender", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\Software\Policies\Microsoft\MicrosoftEdge\PhishingFilter"" /v EnabledV9 /t REG_DWORD /d 0 /f"), null),
            (async () => await ProcessActions.RunNsudo("Optimizing Windows Defender", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows Defender Security Center\Systray"" /v HideSystray /t REG_DWORD /d 1 /f"), null),
            (async () => await ProcessActions.RunNsudo("Optimizing Windows Defender", "TrustedInstaller", @"reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Run"" /v SecurityHealth /f"), null),
            (async () => await ProcessActions.RunNsudo("Optimizing Windows Defender", "TrustedInstaller", @"reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Run"" /v WindowsDefender /f"), null),

            // disable windows defender
            (async () => await ProcessActions.RunNsudo("Disabling Windows Defender", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows Defender"" /v DisableAntiSpyware /t REG_DWORD /d 1 /f"), () => WindowsDefender == false),
            (async () => await ProcessActions.RunNsudo("Disabling Windows Defender", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Microsoft Antimalware\Real-Time Protection"" /v DisableScanOnRealtimeEnable /t REG_DWORD /d 1 /f"), () => WindowsDefender == false),
            (async () => await ProcessActions.RunNsudo("Disabling Windows Defender", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Microsoft Antimalware\Real-Time Protection"" /v DisableOnAccessProtection /t REG_DWORD /d 1 /f"), () => WindowsDefender == false),
            (async () => await ProcessActions.RunNsudo("Disabling Windows Defender", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows Defender\Real-Time Protection"" /v DisableScanOnRealtimeEnable /t REG_DWORD /d 1 /f"), () => WindowsDefender == false),
            (async () => await ProcessActions.RunNsudo("Disabling Windows Defender", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows Defender\Real-Time Protection"" /v DisableBehaviorMonitoring /t REG_DWORD /d 1 /f"), () => WindowsDefender == false),
            (async () => await ProcessActions.RunNsudo("Disabling Windows Defender", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\MRT"" /v DontOfferThroughWUAU /t REG_DWORD /d 1 /f"), () => WindowsDefender == false),
            (async () => await ProcessActions.RunNsudo("Disabling Windows Defender", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\MsSecCore"" /v Start /t REG_DWORD /d 4 /f"), () => WindowsDefender == false),
            (async () => await ProcessActions.RunNsudo("Disabling Windows Defender", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\SecurityHealthService"" /v Start /t REG_DWORD /d 4 /f"), () => WindowsDefender == false),
            (async () => await ProcessActions.RunNsudo("Disabling Windows Defender", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Sense"" /v Start /t REG_DWORD /d 4 /f"), () => WindowsDefender == false),
            (async () => await ProcessActions.RunNsudo("Disabling Windows Defender", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\WdBoot"" /v Start /t REG_DWORD /d 4 /f"), () => WindowsDefender == false),
            (async () => await ProcessActions.RunNsudo("Disabling Windows Defender", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\WdFilter"" /v Start /t REG_DWORD /d 4 /f"), () => WindowsDefender == false),
            (async () => await ProcessActions.RunNsudo("Disabling Windows Defender", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\WdNisDrv"" /v Start /t REG_DWORD /d 4 /f"), () => WindowsDefender == false),
            (async () => await ProcessActions.RunNsudo("Disabling Windows Defender", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\WdNisSvc"" /v Start /t REG_DWORD /d 4 /f"), () => WindowsDefender == false),
            (async () => await ProcessActions.RunNsudo("Disabling Windows Defender", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\webthreatdefsvc"" /v Start /t REG_DWORD /d 4 /f"), () => WindowsDefender == false),
            (async () => await ProcessActions.RunNsudo("Disabling Windows Defender", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\webthreatdefusersvc"" /v Start /t REG_DWORD /d 4 /f"), () => WindowsDefender == false),
            (async () => await ProcessActions.RunNsudo("Disabling Windows Defender", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\WinDefend"" /v Start /t REG_DWORD /d 4 /f"), () => WindowsDefender == false),
            (async () => await ProcessActions.RunNsudo("Disabling Windows Defender", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\wscsvc"" /v Start /t REG_DWORD /d 4 /f"), () => WindowsDefender == false),

            // disable smartscreen
            (async () => await ProcessActions.RunNsudo("Disabling Smartscreen", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer"" /v SmartScreenEnabled /t REG_SZ /d ""Off"" /f"), null),
            (async () => await ProcessActions.RunNsudo("Disabling Smartscreen", "CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\Edge\SmartScreenEnabled"" /ve /t REG_DWORD /d 0 /f"), null),
            (async () => await ProcessActions.RunNsudo("Disabling Smartscreen", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\Software\Policies\Microsoft\System"" /v EnableSmartScreen /t REG_DWORD /d 0 /f"), null),
            (async () => await ProcessActions.RunNsudo("Disabling Smartscreen", "TrustedInstaller", @"cmd /c taskkill /f /im smartscreen.exe > nul 2>&1 & ren C:\Windows\System32\smartscreen.exe smartscreen.exee"), null),

            // disable windows marking file attachments with information about their zone of origin
            (async () => await ProcessActions.RunNsudo("Disabling windows marking file attachments with information about their zone of origin", "CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Attachments"" /v SaveZoneInformation /t REG_DWORD /d 1 /f"), null),

            // disable publisher could not be verified popup
            (async () => await ProcessActions.RunNsudo("Disabling publisher could not be verified popup", "CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Policies\Associations"" /v DefaultFileTypeRisk /t REG_DWORD /d 1808 /f"), null),

            // set moderate risk file types
            (async () => await ProcessActions.RunNsudo("Setting moderate risk file types", "CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Policies\Associations"" /v ModRiskFileTypes /t REG_SZ /d "".bat;.exe;.reg;.vbs;.chm;.msi;.js;.cmd"" /f"), null),

            // disable antivirus notification when opening attachments
            (async () => await ProcessActions.RunNsudo("Disabling antivirus notification when opening attachments", "CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Policies\Attachments"" /v ScanWithAntiVirus /t REG_DWORD /d 1 /f"), null),

            // disable tsx
            (async () => await ProcessActions.RunNsudo("Disabling Transactional Synchronization Extensions (TSX)", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\ControlSet001\Control\Session Manager\Kernel"" /v DisableTsx /t REG_DWORD /d 1 /f"), null),

            // set execution policy to unrestricted
            (async () => await ProcessActions.RunPowerShell("Setting execution policy to unrestricted", "Set-ExecutionPolicy Unrestricted -Force"), null),

            // disable uac
            (async () => await ProcessActions.RunNsudo("Disabling user account control (UAC)", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System"" /v EnableLUA /t REG_DWORD /d 0 /f"), () => UserAccountControl == false),

            // disable data execution prevention (dep)
            (async () => await ProcessActions.RunNsudo("Disabling data execution prevention (DEP)", "TrustedInstaller", "bcdedit /set nx AlwaysOff"), () => DEP == false),

            // enable spectre and meltdown mitigations
            (async () => await ProcessActions.RunNsudo("Enabling Spectre & Meltdown Mitigations", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management"" /v ""FeatureSettings"" /t REG_DWORD /d 1 /f"), () => AMDCPU == true && SpectreMeltdownMitigations == true),
            (async () => await ProcessActions.RunNsudo("Enabling Spectre & Meltdown Mitigations", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management"" /v ""FeatureSettingsOverrideMask"" /t REG_DWORD /d 3 /f"), () => AMDCPU == true && SpectreMeltdownMitigations == true),
            (async () => await ProcessActions.RunNsudo("Enabling Spectre & Meltdown Mitigations", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management"" /v ""FeatureSettingsOverride"" /t REG_DWORD /d 64 /f"), () => AMDCPU == true && SpectreMeltdownMitigations == true),
            (async () => await ProcessActions.RunNsudo("Enabling Spectre & Meltdown Mitigations", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management"" /v ""FeatureSettings"" /t REG_DWORD /d 0 /f"), () => INTELCPU == true && SpectreMeltdownMitigations == true),

            // disable spectre and meltdown mitigations
            (async () => await ProcessActions.RunNsudo("Enabling Spectre & Meltdown Mitigations", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management"" /v ""FeatureSettings"" /t REG_DWORD /d 1 /f"), () => SpectreMeltdownMitigations == false),
            (async () => await ProcessActions.RunNsudo("Enabling Spectre & Meltdown Mitigations", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management"" /v ""FeatureSettingsOverrideMask"" /t REG_DWORD /d 3 /f"), () => SpectreMeltdownMitigations == false),
            (async () => await ProcessActions.RunNsudo("Enabling Spectre & Meltdown Mitigations", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management"" /v ""FeatureSettingsOverride"" /t REG_DWORD /d 3 /f"), () => SpectreMeltdownMitigations == false),

            // disable microcode updates
            (async () => await ProcessActions.RunNsudo("Disable microcode updates", "TrustedInstaller", @"cmd /c ren C:\Windows\System32\mcupdate_GenuineIntel.dll mcupdate_GenuineIntel.dlll"), () => SpectreMeltdownMitigations == false),
            (async () => await ProcessActions.RunNsudo("Disable microcode updates", "TrustedInstaller", @"cmd /c ren C:\Windows\System32\mcupdate_AuthenticAMD.dll mcupdate_AuthenticAMD.dlll"), () => SpectreMeltdownMitigations == false),

            // disable process mitigations
            (async () => await ProcessActions.RunNsudo("Disabling process mitigations", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Kernel"" /v ""MitigationAuditOptions"" /t REG_BINARY /d 222222222222222222222222222222222222222222222222"), () => ProcessMitigations == false),
            (async () => await ProcessActions.RunNsudo("Disabling process mitigations", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Kernel"" /v ""MitigationOptions"" /t REG_BINARY /d 222222222222222222222222222222222222222222222222"), () => ProcessMitigations == false)
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
