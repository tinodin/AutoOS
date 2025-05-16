using AutoOS.Views.Installer.Actions;
using Microsoft.UI.Xaml.Media;

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
            //("Disabling startup sounds", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Authentication\LogonUI\BootAnimation"" /v DisableStartupSound /t REG_DWORD /d 1 /f"), null),
            //("Disabling startup sounds", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\EditionOverrides"" /v UserSetting_DisableStartupSound /t REG_DWORD /d 1 /f"), null),

            // disable system beeps
            ("Disabling system beeps", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Control Panel\Sound"" /v Beep /t REG_SZ /d no /f"), null),

            // set sound scheme none
            //("Setting sound scheme to none", async () => await ProcessActions.RunPowerShell(@"New-ItemProperty -Path 'HKCU:\AppEvents\Schemes' -Name '(Default)' -Value '.None' -Force; Get-ChildItem -Path 'HKCU:\AppEvents\Schemes\Apps' | Get-ChildItem | Get-ChildItem | Where-Object { $_.PSChildName -eq '.Current' } | Set-ItemProperty -Name '(Default)' -Value ''"), null),

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
        
            //// download microsoft dolby digital atmos pack
            //("Downloading Microsoft Dolby Digital Atmos Pack", async () => await ProcessActions.RunDownload("https://www.dl.dropboxusercontent.com/scl/fi/l847exz5nsvox9xnmbs7g/MicrosoftDolbyDigitalAtmosPack.zip?rlkey=ttaek3wm0z31r3dws808409q1&st=mrkaozqv&dl=0", Path.GetTempPath(), "MicrosoftDolbyDigitalAtmosPack.zip"), null),

            //// install microsoft dolby digital atmos pack
            //("Installing Microsoft Dolby Digital Atmos Pack", async () => await ProcessActions.RunExtract(Path.Combine(Path.GetTempPath(), "MicrosoftDolbyDigitalAtmosPack.zip"), Path.Combine(Path.GetTempPath(), "MicrosoftDolbyDigitalAtmosPack")), null),
            //("Installing Microsoft Dolby Digital Atmos Pack", async () => await ProcessActions.RunNsudo("CurrentUser", @"cmd /c xcopy %TEMP%\MicrosoftDolbyDigitalAtmosPack\System32\* ""C:\Windows\System32"" /s /e /y"), null),
            //("Installing Microsoft Dolby Digital Atmos Pack", async () => await ProcessActions.RunNsudo("CurrentUser", @"cmd /c xcopy %TEMP%\MicrosoftDolbyDigitalAtmosPack\SysWOW64\* ""C:\Windows\SysWOW64"" /s /e /y"), null),
            //("Installing Microsoft Dolby Digital Atmos Pack", async () => await ProcessActions.RunNsudo("CurrentUser", @$"cmd /c reg import %TEMP%\MicrosoftDolbyDigitalAtmosPack\AC3DS.reg"), null),
            //("Installing Microsoft Dolby Digital Atmos Pack", async () => await ProcessActions.RunNsudo("CurrentUser", @$"cmd /c reg import %TEMP%\MicrosoftDolbyDigitalAtmosPack\AC3EncMft.reg"), null),
            //("Installing Microsoft Dolby Digital Atmos Pack", async () => await ProcessActions.RunNsudo("CurrentUser", @$"cmd /c reg import %TEMP%\MicrosoftDolbyDigitalAtmosPack\AC3Mft.reg"), null),
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
