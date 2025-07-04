﻿using AutoOS.Views.Installer.Actions;
using Microsoft.UI.Xaml.Media;

namespace AutoOS.Views.Installer.Stages;

public static class SecurityStage
{
    public static async Task Run()
    {
        bool? WindowsDefender = PreparingStage.WindowsDefender;
        bool? UserAccountControl = PreparingStage.UserAccountControl;
        bool? DEP = PreparingStage.DEP;
        bool? MemoryIntegrity = PreparingStage.MemoryIntegrity;
        bool? INTELCPU = PreparingStage.INTELCPU;
        bool? AMDCPU = PreparingStage.AMDCPU;
        bool? SpectreMeltdownMitigations = PreparingStage.SpectreMeltdownMitigations;
        bool? ProcessMitigations = PreparingStage.ProcessMitigations;

        InstallPage.Status.Text = "Configuring Security...";

        string previousTitle = string.Empty;
        int stagePercentage = 5;

        var actions = new List<(string Title, Func<Task> Action, Func<bool> Condition)>
        {
            // import hosts file
            ("Importing hosts file", async () => await ProcessActions.RunCustom(async () => await Task.Run(() => File.Copy(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Scripts", "hosts"), @"C:\Windows\System32\drivers\etc\hosts", true))), null),
            ("Importing hosts file", async () => await ProcessActions.RunNsudo("CurrentUser", @"ipconfig /flushdns"), null),

            // optimize windows defender
            ("Optimizing Windows Defender", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\WTDS\Components"" /v ServiceEnabled /t REG_DWORD /d 0 /f"), null),
            ("Optimizing Windows Defender", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\MRT"" /v DontReportInfectionInformation /t REG_DWORD /d 1 /f"), null),
            ("Optimizing Windows Defender", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows Defender\Spynet"" /v SpyNetReporting /t REG_DWORD /d 0 /f"), null),
            ("Optimizing Windows Defender", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows Defender\Spynet"" /v SubmitSamplesConsent /t REG_DWORD /d 0 /f"), null),
            ("Optimizing Windows Defender", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\CI\Policy"" /v VerifiedAndReputablePolicyState /t REG_DWORD /d 0 /f"), null),
            ("Optimizing Windows Defender", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\AppHost"" /v EnableWebContentEvaluation /t REG_DWORD /d 0 /f"), null),
            ("Optimizing Windows Defender", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\Software\Policies\Microsoft\Windows Defender\Signature Updates"" /v SignatureDisableNotification /t REG_DWORD /d 0 /f"), null),
            ("Optimizing Windows Defender", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\Software\Policies\Microsoft\Windows Defender\Signature Updates"" /v RealtimeSignatureDelivery /t REG_DWORD /d 0 /f"), null),
            ("Optimizing Windows Defender", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\Software\Policies\Microsoft\Windows Defender\Signature Updates"" /v ForceUpdateFromMU /t REG_DWORD /d 0 /f"), null),
            ("Optimizing Windows Defender", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\Software\Policies\Microsoft\Windows Defender\Signature Updates"" /v DisableScheduledSignatureUpdateOnBattery /t REG_DWORD /d 1 /f"), null),
            ("Optimizing Windows Defender", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\Software\Policies\Microsoft\Windows Defender\Signature Updates"" /v UpdateOnStartUp /t REG_DWORD /d 0 /f"), null),
            ("Optimizing Windows Defender", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\Software\Policies\Microsoft\Windows Defender\Signature Updates"" /v DisableUpdateOnStartupWithoutEngine /t REG_DWORD /d 1 /f"), null),
            ("Optimizing Windows Defender", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\Software\Policies\Microsoft\Windows Defender\Signature Updates"" /v DisableScanOnUpdate /t REG_DWORD /d 1 /f"), null),
            ("Optimizing Windows Defender", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\Software\Policies\Microsoft\Windows Defender\SmartScreen"" /v ConfigureAppInstallControlEnabled /t REG_DWORD /d 1 /f"), null),
            ("Optimizing Windows Defender", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\Software\Policies\Microsoft\Windows Defender\SmartScreen"" /v ConfigureAppInstallControl /t REG_SZ /d Anywhere /f"), null),
            ("Optimizing Windows Defender", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\Software\Policies\Microsoft\MicrosoftEdge\PhishingFilter"" /v EnabledV9 /t REG_DWORD /d 0 /f"), null),
            ("Optimizing Windows Defender", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows Defender Security Center\Systray"" /v HideSystray /t REG_DWORD /d 1 /f"), null),
            ("Optimizing Windows Defender", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Run"" /v SecurityHealth /f"), null),
            ("Optimizing Windows Defender", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Run"" /v WindowsDefender /f"), null),

            // disable windows defender
            ("Disabling Windows Defender", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows Defender"" /v DisableAntiSpyware /t REG_DWORD /d 1 /f"), () => WindowsDefender == false),
            ("Disabling Windows Defender", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Microsoft Antimalware\Real-Time Protection"" /v DisableScanOnRealtimeEnable /t REG_DWORD /d 1 /f"), () => WindowsDefender == false),
            ("Disabling Windows Defender", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Microsoft Antimalware\Real-Time Protection"" /v DisableOnAccessProtection /t REG_DWORD /d 1 /f"), () => WindowsDefender == false),
            ("Disabling Windows Defender", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows Defender\Real-Time Protection"" /v DisableScanOnRealtimeEnable /t REG_DWORD /d 1 /f"), () => WindowsDefender == false),
            ("Disabling Windows Defender", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows Defender\Real-Time Protection"" /v DisableBehaviorMonitoring /t REG_DWORD /d 1 /f"), () => WindowsDefender == false),
            ("Disabling Windows Defender", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\MRT"" /v DontOfferThroughWUAU /t REG_DWORD /d 1 /f"), () => WindowsDefender == false),
            ("Disabling Windows Defender", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\MsSecCore"" /v Start /t REG_DWORD /d 4 /f"), () => WindowsDefender == false),
            ("Disabling Windows Defender", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\SecurityHealthService"" /v Start /t REG_DWORD /d 4 /f"), () => WindowsDefender == false),
            ("Disabling Windows Defender", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Sense"" /v Start /t REG_DWORD /d 4 /f"), () => WindowsDefender == false),
            ("Disabling Windows Defender", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\WdBoot"" /v Start /t REG_DWORD /d 4 /f"), () => WindowsDefender == false),
            ("Disabling Windows Defender", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\WdFilter"" /v Start /t REG_DWORD /d 4 /f"), () => WindowsDefender == false),
            ("Disabling Windows Defender", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\WdNisDrv"" /v Start /t REG_DWORD /d 4 /f"), () => WindowsDefender == false),
            ("Disabling Windows Defender", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\WdNisSvc"" /v Start /t REG_DWORD /d 4 /f"), () => WindowsDefender == false),
            ("Disabling Windows Defender", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\webthreatdefsvc"" /v Start /t REG_DWORD /d 4 /f"), () => WindowsDefender == false),
            ("Disabling Windows Defender", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\webthreatdefusersvc"" /v Start /t REG_DWORD /d 4 /f"), () => WindowsDefender == false),
            ("Disabling Windows Defender", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\WinDefend"" /v Start /t REG_DWORD /d 4 /f"), () => WindowsDefender == false),
            ("Disabling Windows Defender", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\wscsvc"" /v Start /t REG_DWORD /d 4 /f"), () => WindowsDefender == false),

            // disable smartscreen
            ("Disabling Smartscreen", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer"" /v SmartScreenEnabled /t REG_SZ /d ""Off"" /f"), null),
            ("Disabling Smartscreen", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\Edge\SmartScreenEnabled"" /ve /t REG_DWORD /d 0 /f"), null),
            ("Disabling Smartscreen", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\Software\Policies\Microsoft\System"" /v EnableSmartScreen /t REG_DWORD /d 0 /f"), null),
            ("Disabling Smartscreen", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"cmd /c taskkill /f /im smartscreen.exe & ren C:\Windows\System32\smartscreen.exe smartscreen.exee"), null),

            // disable windows marking file attachments with information about their zone of origin
            ("Disabling windows marking file attachments with information about their zone of origin", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Attachments"" /v SaveZoneInformation /t REG_DWORD /d 1 /f"), null),

            // disable publisher could not be verified popup
            ("Disabling publisher could not be verified popup", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Policies\Associations"" /v DefaultFileTypeRisk /t REG_DWORD /d 1808 /f"), null),

            // set moderate risk file types
            ("Setting moderate risk file types", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Policies\Associations"" /v ModRiskFileTypes /t REG_SZ /d "".bat;.exe;.reg;.vbs;.chm;.msi;.js;.cmd"" /f"), null),

            // disable antivirus notification when opening attachments
            ("Disabling antivirus notification when opening attachments", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Policies\Attachments"" /v ScanWithAntiVirus /t REG_DWORD /d 1 /f"), null),

            // disable tsx
            ("Disabling Transactional Synchronization Extensions (TSX)", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\ControlSet001\Control\Session Manager\Kernel"" /v DisableTsx /t REG_DWORD /d 1 /f"), null),

            // set execution policy to unrestricted
            ("Setting execution policy to unrestricted", async () => await ProcessActions.RunPowerShell("Set-ExecutionPolicy Unrestricted -Force"), null),

            // enable uac
            ("Enabling user account control (UAC)", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System"" /v EnableLUA /t REG_DWORD /d 1 /f"), () => UserAccountControl == true),

            // disable data execution prevention (dep)
            ("Disabling data execution prevention (DEP)", async () => await ProcessActions.RunNsudo("TrustedInstaller", "bcdedit /set nx AlwaysOff"), () => DEP == false),

            // disable hypervisor enforced code integrity (hvci)
            ("Disabling Hypervisor Enforced Code Integrity (HVCI)", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity"" /v Enabled /t REG_DWORD /d 0 /f"), () => MemoryIntegrity == false),

            // enable spectre and meltdown mitigations
            ("Enabling Spectre & Meltdown Mitigations", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management"" /v ""FeatureSettings"" /t REG_DWORD /d 1 /f"), () => AMDCPU == true && SpectreMeltdownMitigations == true),
            ("Enabling Spectre & Meltdown Mitigations", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management"" /v ""FeatureSettingsOverrideMask"" /t REG_DWORD /d 3 /f"), () => AMDCPU == true && SpectreMeltdownMitigations == true),
            ("Enabling Spectre & Meltdown Mitigations", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management"" /v ""FeatureSettingsOverride"" /t REG_DWORD /d 64 /f"), () => AMDCPU == true && SpectreMeltdownMitigations == true),
            ("Enabling Spectre & Meltdown Mitigations", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management"" /v ""FeatureSettings"" /t REG_DWORD /d 0 /f"), () => INTELCPU == true && SpectreMeltdownMitigations == true),

            // disable spectre and meltdown mitigations
            ("Disabling Spectre & Meltdown Mitigations", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management"" /v ""FeatureSettings"" /t REG_DWORD /d 1 /f"), () => SpectreMeltdownMitigations == false),
            ("Disabling Spectre & Meltdown Mitigations", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management"" /v ""FeatureSettingsOverrideMask"" /t REG_DWORD /d 3 /f"), () => SpectreMeltdownMitigations == false),
            ("Disabling Spectre & Meltdown Mitigations", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management"" /v ""FeatureSettingsOverride"" /t REG_DWORD /d 3 /f"), () => SpectreMeltdownMitigations == false),
            ("Disabling Spectre & Meltdown Mitigations", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management"" /v ""MoveImages"" /t REG_DWORD /d 0 /f"), () => SpectreMeltdownMitigations == false),
            ("Disabling Spectre & Meltdown Mitigations", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\kernel"" /v ""DisableExceptionChainValidation"" /t REG_DWORD /d 1 /f"), () => SpectreMeltdownMitigations == false),
            ("Disabling Spectre & Meltdown Mitigations", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\kernel"" /v ""DisableControlFlowGuardXfg"" /t REG_DWORD /d 1 /f"), () => SpectreMeltdownMitigations == false),
            ("Disabling Spectre & Meltdown Mitigations", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\kernel"" /v ""DisableControlFlowGuardExportSuppression"" /t REG_DWORD /d 1 /f"), () => SpectreMeltdownMitigations == false),
            ("Disabling Spectre & Meltdown Mitigations", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\SCMConfig"" /v ""EnableSvchostMitigationsPolicy"" /t REG_DWORD /d 0 /f"), () => SpectreMeltdownMitigations == false),
            ("Disabling Spectre & Meltdown Mitigations", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"cmd /c for %a in (fontdrvhost.exe dwm.exe lsass.exe svchost.exe WmiPrvSE.exe winlogon.exe csrss.exe audiodg.exe ntoskrnl.exe services.exe) do reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\%a"" /v ""MitigationOptions"" /t REG_BINARY /d ""22222222222222222222222222222222"" /f && reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\%a"" /v ""MitigationAuditOptions"" /t REG_BINARY /d ""22222222222222222222222222222222"" /f"), () => SpectreMeltdownMitigations == false),
            ("Disabling Spectre & Meltdown Mitigations", async () => await ProcessActions.RunPowerShell(@"ForEach($v in (Get-Command -Name \""Set-ProcessMitigation\"").Parameters[\""Disable\""].Attributes.ValidValues){Set-ProcessMitigation -System -Disable $v.ToString().Replace(\"" \"", \""\"").Replace(\""`n\"", \""\"") -ErrorAction SilentlyContinue}"), () => SpectreMeltdownMitigations == false),

            // disable microcode updates
            ("Disable microcode updates", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"cmd /c ren C:\Windows\System32\mcupdate_GenuineIntel.dll mcupdate_GenuineIntel.dlll"), () => SpectreMeltdownMitigations == false),
            ("Disable microcode updates", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"cmd /c ren C:\Windows\System32\mcupdate_AuthenticAMD.dll mcupdate_AuthenticAMD.dlll"), () => SpectreMeltdownMitigations == false),

            // disable process mitigations
            ("Disabling process mitigations", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Kernel"" /v ""MitigationAuditOptions"" /t REG_BINARY /d 222222222222222222222222222222222222222222222222 /f"), () => ProcessMitigations == false),
            ("Disabling process mitigations", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Kernel"" /v ""MitigationOptions"" /t REG_BINARY /d 222222222222222222222222222222222222222222222222 /f"), () => ProcessMitigations == false)
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