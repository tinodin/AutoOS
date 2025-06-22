using AutoOS.Views.Installer.Actions;
using Microsoft.UI.Xaml.Media;
using Microsoft.Win32;
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

        InstallPage.Status.Text = "Configuring Games...";

        string previousTitle = string.Empty;
        int stagePercentage = 2;

        string fortnitePath = string.Empty;

        Fortnite = true;

        var actions = new List<(string Title, Func<Task> Action, Func<bool> Condition)>
        {
            // import fortnite settings
            ("Importing Fortnite settings", async () => await ProcessActions.RunNsudo("CurrentUser", @"cmd /c mkdir ""%LocalAppData%\FortniteGame\Saved\Config\WindowsClient"""), () => Fortnite == true),
            ("Importing Fortnite settings", async () => await ProcessActions.RunNsudo("CurrentUser", @"cmd /c copy /Y """ + Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Scripts", "GameUserSettings.ini") + @""" ""%LocalAppData%\FortniteGame\Saved\Config\WindowsClient\GameUserSettings.ini"""), () => Fortnite == true),
            ("Importing Fortnite settings", async () => await ProcessActions.RunNsudo("CurrentUser", @$"powershell -Command ""$path = Join-Path $env:LOCALAPPDATA 'FortniteGame\Saved\Config\WindowsClient\GameUserSettings.ini'; $lines = Get-Content $path; $lines = $lines | ForEach-Object {{ if ($_ -like 'FrameRateLimit=*') {{ 'FrameRateLimit=' + {GetDeviceCaps(GetDC(IntPtr.Zero), 116)} + '.000000' }} else {{ $_ }} }}; Set-Content -Path $path -Value $lines"""), () => Fortnite == true),

            // set gpu preference to high performance for fortnite
            ("Setting GPU Preference to high performance for Fortnite", async () => await ProcessActions.RunCustom(async () => fortnitePath = await Task.Run(() => JsonDocument.Parse(File.ReadAllText(@"C:\ProgramData\Epic\UnrealEngineLauncher\LauncherInstalled.dat")).RootElement.GetProperty("InstallationList").EnumerateArray().FirstOrDefault(e => e.GetProperty("AppName").GetString() == "Fortnite").GetProperty("InstallLocation").GetString())), () => Fortnite == true),
            ("Setting GPU Preference to high performance for Fortnite", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Microsoft\DirectX\UserGpuPreferences"" /v """ + fortnitePath + @"\FortniteGame\Binaries\Win64\FortniteClient-Win64-Shipping.exe"" /t REG_SZ /d ""SwapEffectUpgradeEnable=1;GpuPreference=2;"" /f"), () => Fortnite == true),

            // install easyanticheat
            ("Installing EasyAntiCheat", async () => await ProcessActions.RunNsudo("CurrentUser", $@"""{fortnitePath}\FortniteGame\Binaries\Win64\EasyAntiCheat\EasyAntiCheat_EOS_Setup.exe"" install 4fe75bbc5a674f4f9b356b5c90567da5"), () => Fortnite == true),
            ("Disabling EasyAntiCheat startup entry", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\EasyAntiCheat_EOS"" /v ""Start"" /t REG_DWORD /d 4 /f"), () => Fortnite == true),

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
            ("Creating presentation mode entries", async () => await ProcessActions.RunCustom(async () => await Task.Run(() => Process.Start(new ProcessStartInfo("cmd", $"/c \"{fortnitePath}\\FortniteGame\\Binaries\\Win64\\FortniteClient-Win64-Shipping.exe\"") { CreateNoWindow = true }))), () => Fortnite == true),
            ("Creating presentation mode entries", async () => await ProcessActions.Sleep(500), () => Fortnite == true),
            ("Creating presentation mode entries", async () => await ProcessActions.RunCustom(async () => await Task.Run(() => Process.GetProcessesByName("FortniteClient-Win64-Shipping")[0].Kill())), () => Fortnite == true),
            ("Creating presentation mode entries", async () => await ProcessActions.RunCustom(async () => await Task.Run(() => Process.Start(new ProcessStartInfo("cmd", $"/c \"{fortnitePath}\\FortniteGame\\Binaries\\Win64\\FortniteClient-Win64-Shipping.exe\"") { CreateNoWindow = true }))), () => Fortnite == true),
            ("Creating presentation mode entries", async () => await ProcessActions.Sleep(500), () => Fortnite == true),
            ("Creating presentation mode entries", async () => await ProcessActions.RunCustom(async () => await Task.Run(() => Process.GetProcessesByName("FortniteClient-Win64-Shipping")[0].Kill())), () => Fortnite == true),

            // set the presentation mode to hardware: legacy flip
            ("Setting the presentation mode to Hardware: Legacy Flip", async () => await ProcessActions.RunCustom(async () => await Task.Run(() => Registry.CurrentUser.OpenSubKey(@"System\GameConfigStore\Children", true)?.GetSubKeyNames().ToList().ForEach(async name => { using var sub = Registry.CurrentUser.OpenSubKey($@"System\GameConfigStore\Children\{name}", true); if (sub?.GetValueNames().Any(v => sub.GetValue(v) is string s && s.Contains("Fortnite")) == true) { var p = Process.Start(new ProcessStartInfo { FileName = "cmd.exe", Arguments = $@"/c reg add ""HKCU\System\GameConfigStore\Children\{name}"" /v Flags /t REG_DWORD /d 0x211 /f", CreateNoWindow = true }); if (p != null) await p.WaitForExitAsync(); return; } }))), () => Fortnite == true),

            // disable fullscreen optimizations
            ("Disabling fullscreen optimizations", async () => await ProcessActions.RunNsudo("CurrentUser", $@"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Layers"" /v ""{fortnitePath}\FortniteGame\Binaries\Win64\FortniteClient-Win64-Shipping.exe"" /t REG_SZ /d ""~ DISABLEDXMAXIMIZEDWINDOWEDMODE HIGHDPIAWARE"" /f"), () => Fortnite == true),
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