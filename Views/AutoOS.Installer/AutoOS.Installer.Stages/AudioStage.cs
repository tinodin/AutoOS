using AutoOS.Views.Installer.Actions;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace AutoOS.Views.Installer.Stages;

public static class AudioStage
{
    public static async Task Run()
    {
        InstallPage.Status.Text = "Configuring Audio Devices...";

        int validActionsCount = 0;
        int stagePercentage = 5;

        var actions = new List<(Func<Task> Action, Func<bool> Condition)>
        {
            // disable startup sound
            (async () => await ProcessActions.RunNsudo("Disabling startup sounds", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Authentication\LogonUI\BootAnimation"" /v DisableStartupSound /t REG_DWORD /d 1 /f"), null),
            (async () => await ProcessActions.RunNsudo("Disabling startup sounds", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\EditionOverrides"" /v UserSetting_DisableStartupSound /t REG_DWORD /d 1 /f"), null),

            // disable system beeps
            (async () => await ProcessActions.RunNsudo("Disabling system beeps", "CurrentUser", @"reg add ""HKEY_CURRENT_USER\Control Panel\Sound"" /v Beep /t REG_SZ /d no /f"), null),

            // set sound scheme none
            (async () => await ProcessActions.RunPowerShell("Setting sound scheme to none", @"$Path = 'HKCU:\\AppEvents\\Schemes'; $Keyname = '(Default)'; $SetValue = '.None'; if (-Not(Test-Path $Path)) { New-Item -Path $Path -Force }; if ((Get-ItemProperty -Path $Path -Name $Keyname -ErrorAction SilentlyContinue).$Keyname -ne $SetValue) { Set-ItemProperty -Path $Path -Name $Keyname -Value $SetValue -Force; Get-ChildItem -Path 'HKCU:\\AppEvents\\Schemes\\Apps' | Get-ChildItem | Get-ChildItem | Where-Object { $_.PSChildName -eq '.Current' } | Set-ItemProperty -Name '(Default)' -Value '' }"), null),

            // set communications to do nothing
            (async () => await ProcessActions.RunNsudo("Setting communications to do nothing", "CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Microsoft\Multimedia\Audio"" /v UserDuckingPreference /t REG_DWORD /d 3 /f"), null),

            // disable audio enhancements
            (async () => await ProcessActions.RunNsudo("Disabling audio enhancements", "TrustedInstaller", @"powershell -Command ""$Keys = @('HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\MMDevices\Audio\Render'); foreach ($Key in $Keys) { Get-ChildItem $Key -Recurse | Where-Object { $_.PSPath -match '\\FxProperties$' } | ForEach-Object { Set-ItemProperty -Path $_.PSPath -Name '{1da5d803-d492-4edd-8c23-e0c0ffee7f0e},5' -Value 1 } }"""), null),
            (async () => await ProcessActions.RunNsudo("Disabling audio enhancements", "TrustedInstaller", @"powershell -Command ""$Keys = @('HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\MMDevices\Audio\Capture'); foreach ($Key in $Keys) { Get-ChildItem $Key -Recurse | Where-Object { $_.PSPath -match '\\FxProperties$' } | ForEach-Object { Set-ItemProperty -Path $_.PSPath -Name '{1da5d803-d492-4edd-8c23-e0c0ffee7f0e},5' -Value 1 } }"""), null),

            // disable exclusive control
            (async () => await ProcessActions.RunNsudo("Disabling exclusive control", "TrustedInstaller", @"cmd /c for %k in (Capture Render) do for /f ""delims="" %a in ('reg query ""HKLM\Software\Microsoft\Windows\CurrentVersion\MMDevices\Audio\%k""') do reg add ""%a\Properties"" /v ""{b3f8fa53-0004-438e-9003-51a46e139bfc},3"" /t REG_DWORD /d 0 /f && reg add ""%a\Properties"" /v ""{b3f8fa53-0004-438e-9003-51a46e139bfc},4"" /t REG_DWORD /d 0 /f"), null),

            // disable power management settings
            (async () => await ProcessActions.RunPowerShellScript("Disabling power management settings", "audiopowermanagement.ps1", ""), null),

            // split audio services
            (async () => await ProcessActions.RunNsudo("Splitting audio services", "CurrentUser", @"cmd /c copy /y %windir%\System32\svchost.exe %windir%\System32\audiosvchost.exe"), null),
            (async () => await ProcessActions.RunPowerShell("Splitting audio services", @"Set-ItemProperty -Path 'HKLM:\SYSTEM\CurrentControlSet\Services\Audiosrv' -Name 'ImagePath' -Value '%systemroot%\system32\audiosvchost.exe -k LocalServiceNetworkRestricted -p' -Type ExpandString"), null),
            (async () => await ProcessActions.RunPowerShell("Splitting audio services", @"Set-ItemProperty -Path 'HKLM:\SYSTEM\CurrentControlSet\Services\AudioEndpointBuilder' -Name 'ImagePath' -Value '%systemroot%\system32\audiosvchost.exe -k LocalSystemNetworkRestricted -p' -Type ExpandString"), null),

            // set speaker volume to 100
            (async () => await ProcessActions.RunApplication("Setting Speakers volume to 100%", "SoundVolumeView", "SoundVolumeView.exe", "/SetVolume Speakers 100"), null),

            // set microphone volume to 100
            (async () => await ProcessActions.RunApplication("Setting Microphone volume to 100%", "SoundVolumeView", "SoundVolumeView.exe", "/SetVolume Microphone 100"), null),
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
