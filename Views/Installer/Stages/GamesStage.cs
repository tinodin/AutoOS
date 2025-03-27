using AutoOS.Views.Installer.Actions;
using Microsoft.UI.Xaml.Media;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace AutoOS.Views.Installer.Stages;

public static class GamesStage
{
    [DllImport("user32.dll")] static extern IntPtr GetDC(IntPtr hwnd);
    [DllImport("gdi32.dll")] static extern int GetDeviceCaps(IntPtr hdc, int nIndex);
    [DllImport("user32.dll")] static extern int ReleaseDC(IntPtr hwnd, IntPtr hdc);
    public static async Task Run()
    {
        bool? Fortnite = ApplicationStage.Fortnite;
        bool? NVIDIA = PreparingStage.NVIDIA;

        InstallPage.Status.Text = "Configuring Games...";

        string previousTitle = string.Empty;
        int stagePercentage = 2;

        string fortnitePath = string.Empty;

        var actions = new List<(string Title, Func<Task> Action, Func<bool> Condition)>
        {
            // import nvidia profile
            ("Importing Fortnite profile", async () => await ProcessActions.ImportProfile("Fortnite.nip"), () => NVIDIA == true && Fortnite == true),

            // import fortnite settings
            ("Importing Fortnite settings", async () => await ProcessActions.RunNsudo("CurrentUser", @"cmd /c mkdir ""%LocalAppData%\FortniteGame\Saved\Config\WindowsClient"""), () => Fortnite == true),
            ("Importing Fortnite settings", async () => await ProcessActions.RunNsudo("CurrentUser", @"cmd /c copy /Y """ + Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Scripts", "GameUserSettings.ini") + @""" ""%LocalAppData%\FortniteGame\Saved\Config\WindowsClient\GameUserSettings.ini"""), () => Fortnite == true),
            ("Importing Fortnite settings", async () => await ProcessActions.RunNsudo("CurrentUser", @$"powershell -Command ""$content = Get-Content (Join-Path $env:LOCALAPPDATA 'FortniteGame\Saved\Config\WindowsClient\GameUserSettings.ini') -Raw; $content = $content.Replace('FrameRateLimit=', 'FrameRateLimit=' + '{GetDeviceCaps(GetDC(IntPtr.Zero), 116)}' + '.000000'); Set-Content -Path (Join-Path $env:LOCALAPPDATA 'FortniteGame\Saved\Config\WindowsClient\GameUserSettings.ini') -Value $content"""), () => Fortnite == true),

            // set gpu preference to high performance for fortnite
            ("Setting GPU Preference to high performance for Fortnite", async () => await ProcessActions.RunCustom(async () => fortnitePath = await Task.Run(() => JsonDocument.Parse(File.ReadAllText(@"C:\ProgramData\Epic\UnrealEngineLauncher\LauncherInstalled.dat")).RootElement.GetProperty("InstallationList").EnumerateArray().FirstOrDefault(e => e.GetProperty("AppName").GetString() == "Fortnite").GetProperty("InstallLocation").GetString())), () => Fortnite == true),
            ("Setting GPU Preference to high performance for Fortnite", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Microsoft\DirectX\UserGpuPreferences"" /v """ + fortnitePath + @"\FortniteGame\Binaries\Win64\FortniteClient-Win64-Shipping.exe"" /t REG_SZ /d ""SwapEffectUpgradeEnable=1;GpuPreference=2;"" /f"), () => Fortnite == true),

            // create fortnite qos policy
            ("Creating Fortnite QoS Policy", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\Software\Policies\Microsoft\Windows\QoS\FortniteClient-Win64-Shipping.exe"" /v ""Application Name"" /t REG_SZ /d ""FortniteClient-Win64-Shipping.exe"" /f"), () => Fortnite == true),
            ("Creating Fortnite QoS Policy", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\Software\Policies\Microsoft\Windows\QoS\FortniteClient-Win64-Shipping.exe"" /v ""Version"" /t REG_SZ /d ""1.0"" /f"), () => Fortnite == true),
            ("Creating Fortnite QoS Policy", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\Software\Policies\Microsoft\Windows\QoS\FortniteClient-Win64-Shipping.exe"" /v ""Protocol"" /t REG_SZ /d ""*"" /f"), () => Fortnite == true),
            ("Creating Fortnite QoS Policy", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\Software\Policies\Microsoft\Windows\QoS\FortniteClient-Win64-Shipping.exe"" /v ""Local Port"" /t REG_SZ /d ""*"" /f"), () => Fortnite == true),
            ("Creating Fortnite QoS Policy", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\Software\Policies\Microsoft\Windows\QoS\FortniteClient-Win64-Shipping.exe"" /v ""Local IP"" /t REG_SZ /d ""*"" /f"), () => Fortnite == true),
            ("Creating Fortnite QoS Policy", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\Software\Policies\Microsoft\Windows\QoS\FortniteClient-Win64-Shipping.exe"" /v ""Local IP Prefix Length"" /t REG_SZ /d ""*"" /f"), () => Fortnite == true),
            ("Creating Fortnite QoS Policy", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\Software\Policies\Microsoft\Windows\QoS\FortniteClient-Win64-Shipping.exe"" /v ""Remote Port"" /t REG_SZ /d ""*"" /f"), () => Fortnite == true),
            ("Creating Fortnite QoS Policy", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\Software\Policies\Microsoft\Windows\QoS\FortniteClient-Win64-Shipping.exe"" /v ""Remote IP"" /t REG_SZ /d ""*"" /f"), () => Fortnite == true),
            ("Creating Fortnite QoS Policy", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\Software\Policies\Microsoft\Windows\QoS\FortniteClient-Win64-Shipping.exe"" /v ""Remote IP Prefix Length"" /t REG_SZ /d ""*"" /f"), () => Fortnite == true),
            ("Creating Fortnite QoS Policy", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\Software\Policies\Microsoft\Windows\QoS\FortniteClient-Win64-Shipping.exe"" /v ""DSCP Value"" /t REG_SZ /d ""46"" /f"), () => Fortnite == true),
            ("Creating Fortnite QoS Policy", async () => await ProcessActions.RunPowerShell(@"New-NetQosPolicy -Name ""FortniteClient-Win64-Shipping.exe"" -AppPathNameMatchCondition ""FortniteClient-Win64-Shipping.exe"" -Precedence 127 -DSCPAction 46 -IPProtocol Both"), () => Fortnite == true),
        
            // create presentation mode entries
            ("Creating presentation mode entries", async () => await ProcessActions.RunCustom(async () => await Task.Run(async () => { var p = Process.Start(fortnitePath + @"\FortniteGame\Binaries\Win64\FortniteClient-Win64-Shipping.exe"); if (p != null) { p.WaitForInputIdle(); await Task.Delay(1000); p.Kill(); } })), () => Fortnite == true),
        
            // disable fullscreen optimizations
            ("Disabling fullscreen optimizations", async () => await ProcessActions.RunNsudo("CurrentUser", $@"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Layers"" /v ""{fortnitePath}\FortniteGame\Binaries\Win64\FortniteClient-Win64-Shipping.exe"" /t REG_SZ /d ""~ DISABLEDXMAXIMIZEDWINDOWEDMODE HIGHDPIAWARE"" /f"), () => Fortnite == true),
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
