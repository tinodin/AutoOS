using AutoOS.Views.Installer.Actions;

namespace AutoOS.Views.Installer.Stages;

public static class AudioStage
{
    public static async Task Run()
    {
        InstallPage.Status.Text = "Configuring Audio Devices...";

        string previousTitle = string.Empty;
        int stagePercentage = 5;

        var actions = new List<(string Title, Func<Task> Action, Func<bool> Condition)>
        {
            // disable startup sound
            ("Disabling startup sounds", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Authentication\LogonUI\BootAnimation"" /v DisableStartupSound /t REG_DWORD /d 1 /f"), null),
            ("Disabling startup sounds", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\EditionOverrides"" /v UserSetting_DisableStartupSound /t REG_DWORD /d 1 /f"), null),

            // disable system beeps
            ("Disabling system beeps", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Control Panel\Sound"" /v Beep /t REG_SZ /d no /f"), null),

            // set sound scheme none
            ("Setting sound scheme to none", async () => await ProcessActions.RunPowerShell(@"$Path = 'HKCU:\\AppEvents\\Schemes'; $Keyname = '(Default)'; $SetValue = '.None'; if (-Not(Test-Path $Path)) { New-Item -Path $Path -Force }; if ((Get-ItemProperty -Path $Path -Name $Keyname -ErrorAction SilentlyContinue).$Keyname -ne $SetValue) { Set-ItemProperty -Path $Path -Name $Keyname -Value $SetValue -Force; Get-ChildItem -Path 'HKCU:\\AppEvents\\Schemes\\Apps' | Get-ChildItem | Get-ChildItem | Where-Object { $_.PSChildName -eq '.Current' } | Set-ItemProperty -Name '(Default)' -Value '' }"), null),

            // set communications to do nothing
            ("Setting communications to do nothing", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Microsoft\Multimedia\Audio"" /v UserDuckingPreference /t REG_DWORD /d 3 /f"), null),

            // disable audio enhancements
            ("Disabling audio enhancements", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"powershell -Command ""$Keys = @('HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\MMDevices\Audio\Render'); foreach ($Key in $Keys) { Get-ChildItem $Key -Recurse | Where-Object { $_.PSPath -match '\\FxProperties$' } | ForEach-Object { Set-ItemProperty -Path $_.PSPath -Name '{1da5d803-d492-4edd-8c23-e0c0ffee7f0e},5' -Value 1 } }"""), null),
            ("Disabling audio enhancements", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"powershell -Command ""$Keys = @('HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\MMDevices\Audio\Capture'); foreach ($Key in $Keys) { Get-ChildItem $Key -Recurse | Where-Object { $_.PSPath -match '\\FxProperties$' } | ForEach-Object { Set-ItemProperty -Path $_.PSPath -Name '{1da5d803-d492-4edd-8c23-e0c0ffee7f0e},5' -Value 1 } }"""), null),

            // disable exclusive control
            ("Disabling exclusive control", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"cmd /c for %k in (Capture Render) do for /f ""delims="" %a in ('reg query ""HKLM\Software\Microsoft\Windows\CurrentVersion\MMDevices\Audio\%k""') do reg add ""%a\Properties"" /v ""{b3f8fa53-0004-438e-9003-51a46e139bfc},3"" /t REG_DWORD /d 0 /f && reg add ""%a\Properties"" /v ""{b3f8fa53-0004-438e-9003-51a46e139bfc},4"" /t REG_DWORD /d 0 /f"), null),

            // disable power management settings
            ("Disabling power management settings", async () => await ProcessActions.RunPowerShellScript("audiopowermanagement.ps1", ""), null),

            // split audio services
            ("Splitting audio services", async () => await ProcessActions.RunNsudo("CurrentUser", @"cmd /c copy /y %windir%\System32\svchost.exe %windir%\System32\audiosvchost.exe"), null),
            ("Splitting audio services", async () => await ProcessActions.RunPowerShell(@"Set-ItemProperty -Path 'HKLM:\SYSTEM\CurrentControlSet\Services\Audiosrv' -Name 'ImagePath' -Value '%systemroot%\system32\audiosvchost.exe -k LocalServiceNetworkRestricted -p' -Type ExpandString"), null),
            ("Splitting audio services", async () => await ProcessActions.RunPowerShell(@"Set-ItemProperty -Path 'HKLM:\SYSTEM\CurrentControlSet\Services\AudioEndpointBuilder' -Name 'ImagePath' -Value '%systemroot%\system32\audiosvchost.exe -k LocalSystemNetworkRestricted -p' -Type ExpandString"), null),

            // set speaker volume to 100
            ("Setting Speakers volume to 100%", async () => await ProcessActions.RunApplication("SoundVolumeView", "SoundVolumeView.exe", "/SetVolume Speakers 100"), null),

            // set microphone volume to 100
            ("Setting Microphone volume to 100%", async () => await ProcessActions.RunApplication("SoundVolumeView", "SoundVolumeView.exe", "/SetVolume Microphone 100"), null),
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
