using AutoOS.Views.Startup.Actions;
using Microsoft.UI.Xaml.Media;
using Microsoft.Win32;
using System.Diagnostics;

namespace AutoOS.Views.Startup.Stages;

public static class StartupStage
{
    public static async Task Run()
    {
        bool MSI = Directory.Exists(@"C:\Program Files (x86)\MSI Afterburner\Profiles\") &&
           Directory.GetFiles(@"C:\Program Files (x86)\MSI Afterburner\Profiles\")
           .Any(f => !f.EndsWith("MSIAfterburner.cfg", StringComparison.OrdinalIgnoreCase));
        bool HID = Registry.GetValue(@"HKEY_CURRENT_USER\SOFTWARE\AutoOS", "HumanInterfaceDevices", "0")?.ToString() == "1";
        bool IMOD = Registry.GetValue(@"HKEY_CURRENT_USER\SOFTWARE\AutoOS", "XhciInterruptModeration", "0")?.ToString() == "1";
        bool WindowsUpdates = Registry.GetValue(@"HKEY_CURRENT_USER\SOFTWARE\AutoOS", "PauseWindowsUpdates", "0")?.ToString() == "1";
        bool Discord = Directory.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Discord"));

        string discordVersion = "";

        string previousTitle = string.Empty;
        int stagePercentage = 100;

        var actions = new List<(string Title, Func<Task> Action, Func<bool> Condition)>
        {
            // sync time
            ("Syncing time", async () => await StartupActions.RunNsudo("CurrentUser", "net start w32time"), null),
            ("Syncing time", async () => await StartupActions.RunNsudo("CurrentUser", "w32tm /resync"), null),
            ("Syncing time", async () => await StartupActions.RunNsudo("CurrentUser", "net stop w32time"), null),

            // disable exclusive control
            ("Disabling exclusive control", async () => await StartupActions.RunNsudo("TrustedInstaller", @"cmd /c for %k in (Capture Render) do for /f ""delims="" %a in ('reg query ""HKLM\Software\Microsoft\Windows\CurrentVersion\MMDevices\Audio\%k""') do reg add ""%a\Properties"" /v ""{b3f8fa53-0004-438e-9003-51a46e139bfc},3"" /t REG_DWORD /d 0 /f && reg add ""%a\Properties"" /v ""{b3f8fa53-0004-438e-9003-51a46e139bfc},4"" /t REG_DWORD /d 0 /f"), null),

            // apply msi afterburner profile
            ("Applying MSI Afterburner profile", async () => await StartupActions.RunCustom(async () => await Task.Run(() => Process.Start(new ProcessStartInfo { FileName = @"C:\Program Files (x86)\MSI Afterburner\MSIAfterburner.exe", Arguments = "/Profile1 /q" }))), () => MSI == true),

            // disable device power management
            ("Disabling device power management", async () => await StartupActions.RunPowerShellScript("devicepowermanagement.ps1", ""), null),

            // disable hid devices
            ("Disabling Human Interface Devices (HID)", async () => await StartupActions.RunPowerShell("Get-PnpDevice -Class HIDClass | Where-Object { $_.FriendlyName -match 'HID-compliant (consumer control device|device|game controller|system controller|vendor-defined device)' -and $_.FriendlyName -notmatch 'Mouse|Keyboard'} | Disable-PnpDevice -Confirm:$false"), () => HID == false),

            // disable xhci interrupt moderation
            ("Disabling XHCI Interrupt Moderation (IMOD)", async () => await StartupActions.RunPowerShellScript("imod.ps1", $"-disable \"{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "RwEverything", "Rw.exe")}\""), () => IMOD == false),

            // apply timer resolution
            ("Applying Timer Resolution", async () => await StartupActions.RunApplication("TimerResolution", "SetTimerResolution.exe", "--resolution 5067 --no-console"), null),

            // launch lowaudiolatency
            ("Launching LowAudioLatency", async () => await StartupActions.RunApplication("LowAudioLatency", "low_audio_latency_no_console.exe", ""), null),

            // disable event trace sessions (ets)
            ("Disabling Event Trace Sessions (ETS)", async () => await StartupActions.RunNsudo("TrustedInstaller", $"cmd /c reg import \"{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Scripts", "ets-disable.reg")}\""), null),
            
            // pause windows updates
            ("Pausing Windows Updates", async () => await StartupActions.RunPowerShellScript("pausewindowsupdates.ps1", ""), () => WindowsUpdates == false),

            // clean up devices
            ("Cleaning up devices", async () => await StartupActions.RunApplication("DeviceCleanup", "DeviceCleanupCmd.exe", "/s *"), null),

            // clean up drives
            ("Cleaning up drives" , async () => await StartupActions.RunApplication("DriveCleanup", "DriveCleanup.exe", ""), null),

            // debloat discord
            ("Debloating Discord", async () => await StartupActions.RunCustom(async () => await Task.Run(() => { discordVersion = new DirectoryInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Discord")).GetDirectories().FirstOrDefault(d => d.Name.StartsWith("app-"))?.Name.Substring(4); })), () => Discord == true),
            ("Debloating Discord", async () => await StartupActions.RunCustom(async () => await Task.Run(() => { try { Directory.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Discord", "app-" + discordVersion, "modules", "discord_cloudsync-1"), true); } catch { } })), () => Discord == true),
            ("Debloating Discord", async () => await StartupActions.RunCustom(async () => await Task.Run(() => { try { Directory.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Discord", "app-" + discordVersion, "modules", "discord_dispatch-1"), true); } catch { } })), () => Discord == true),
            ("Debloating Discord", async () => await StartupActions.RunCustom(async () => await Task.Run(() => { try { Directory.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Discord", "app-" + discordVersion, "modules", "discord_erlpack-1"), true); } catch { } })), () => Discord == true),
            ("Debloating Discord", async () => await StartupActions.RunCustom(async () => await Task.Run(() => { try { Directory.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Discord", "app-" + discordVersion, "modules", "discord_game_utils-1"), true); } catch { } })), () => Discord == true),
            ("Debloating Discord", async () => await StartupActions.RunCustom(async () => await Task.Run(() => { try { Directory.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Discord", "app-" + discordVersion, "modules", "discord_hook-1"), true); } catch { } })), () => Discord == true),
            ("Debloating Discord", async () => await StartupActions.RunCustom(async () => await Task.Run(() => { try { Directory.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Discord", "app-" + discordVersion, "modules", "discord_overlay2-1"), true); } catch { } })), () => Discord == true),
            ("Debloating Discord", async () => await StartupActions.RunCustom(async () => await Task.Run(() => { try { Directory.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Discord", "app-" + discordVersion, "modules", "discord_rpc-1"), true); } catch { } })), () => Discord == true),
            ("Debloating Discord", async () => await StartupActions.RunCustom(async () => await Task.Run(() => { try { Directory.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Discord", "app-" + discordVersion, "modules", "discord_spellcheck-1"), true); } catch { } })), () => Discord == true),
            ("Debloating Discord", async () => await StartupActions.RunCustom(async () => await Task.Run(() => { try { Directory.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Discord", "app-" + discordVersion, "modules", "discord_zstd-1"), true); } catch { } })), () => Discord == true),

            // clean temp directories
            ("Cleaning temp directories", async () => await StartupActions.RunNsudo("TrustedInstaller", @"cmd /c del /s /f /q ""C:\Windows\Logs"""), null),
            ("Cleaning temp directories", async () => await StartupActions.RunNsudo("TrustedInstaller", @"cmd /c del /s /f /q ""C:\Windows\SoftwareDistribution"""), null),
            ("Cleaning temp directories", async () => await StartupActions.RunNsudo("TrustedInstaller", @"cmd /c del /s /f /q ""C:\Windows\System32\LogFiles\*.*"""), null),
            ("Cleaning temp directories", async () => await StartupActions.RunNsudo("TrustedInstaller", @"cmd /c del /s /f /q ""C:\Windows\System32\SleepStudy\*.*"""), null),
            ("Cleaning temp directories", async () => await StartupActions.RunNsudo("TrustedInstaller", @"cmd /c del /s /f /q ""C:\Windows\System32\sru"""), null),
            ("Cleaning temp directories", async () => await StartupActions.RunNsudo("TrustedInstaller", @"cmd /c del /s /f /q ""C:\Windows\System32\WDI\*.*"""), null),
            ("Cleaning temp directories", async () => await StartupActions.RunNsudo("TrustedInstaller", @"cmd /c del /s /f /q ""C:\Windows\System32\winevt\Logs\*.*"""), null),
            ("Cleaning temp directories", async () => await StartupActions.RunNsudo("TrustedInstaller", @"cmd /c del /s /f /q ""C:\Windows\SystemTemp\*.*"""), null),
            ("Cleaning temp directories", async () => await StartupActions.RunNsudo("TrustedInstaller", @"cmd /c del /s /f /q ""C:\Windows\Temp\*.*"""), null),
            ("Cleaning temp directories", async () => await StartupActions.RunNsudo("CurrentUser", @"cmd /c del /s /f /q %temp%\*.*"), null),
            ("Cleaning temp directories", async () => await StartupActions.RunNsudo("CurrentUser", @"cmd /c rd /s /q %temp%"), null),
            ("Cleaning temp directories", async () => await StartupActions.RunNsudo("CurrentUser", @"cmd /c md %temp%"), null),
            ("Cleaning temp directories", async () => await StartupActions.RunNsudo("CurrentUser", @"sc stop TrustedInstaller"), null),
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
                        StartupWindow.Status.Text = ex.Message;
                        StartupWindow.Progress.Foreground = (Brush)Application.Current.Resources["SystemFillColorCriticalBrush"];
                    }
                }

                StartupWindow.Progress.Value += incrementPerTitle;
                await Task.Delay(150);
                currentGroup.Clear();
            }

            StartupWindow.Status.Text = title + "...";
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
                    StartupWindow.Status.Text = ex.Message;
                    StartupWindow.Progress.Foreground = (Brush)Application.Current.Resources["SystemFillColorCriticalBrush"];
                }
            }
            StartupWindow.Progress.Value += incrementPerTitle;
        }

        StartupWindow.Status.Text = "Done.";
        StartupWindow.Progress.Foreground = new SolidColorBrush((Windows.UI.Color)Application.Current.Resources["SystemFillColorSuccess"]);

        await Task.Delay(700);

        Application.Current.Exit();
    }
}