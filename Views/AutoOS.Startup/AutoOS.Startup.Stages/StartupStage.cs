using AutoOS.Views.Startup.Actions;
using Microsoft.Win32;
using System.Diagnostics;

namespace AutoOS.Views.Startup.Stages;

public static class StartupStage
{
    public static async Task Run()
    {

        bool MSI = Registry.GetValue(@"HKEY_CURRENT_USER\SOFTWARE\AutoOS", "MsiProfile", null) != null;
        bool HID = Registry.GetValue(@"HKEY_CURRENT_USER\SOFTWARE\AutoOS", "HumanInterfaceDevices", "0")?.ToString() == "1"; 
        bool IMOD = Registry.GetValue(@"HKEY_CURRENT_USER\SOFTWARE\AutoOS", "XhciInterruptModeration", "0")?.ToString() == "1";
        bool WindowsUpdates = Registry.GetValue(@"HKEY_CURRENT_USER\SOFTWARE\AutoOS", "PauseWindowsUpdates", "0")?.ToString() == "1";
        bool Discord = Registry.GetValue(@"HKEY_CURRENT_USER\SOFTWARE\AutoOS", "Messaging", "")?.ToString().Contains("Discord") == true;

        string discordVersion = "";

        string previousTitle = string.Empty;
        int stagePercentage = 100;

        var actions = new List<(string Title, Func<Task> Action, Func<bool> Condition)>
        {
            // sync the time
            ("Syncing time", async () => await StartupActions.RunNsudo("CurrentUser", "net start w32time"), null),
            ("Syncing time", async () => await StartupActions.RunNsudo("CurrentUser", "w32tm /resync"), null),
            ("Syncing time", async () => await StartupActions.RunNsudo("CurrentUser", "net stop w32time"), null),

            // apply msi afterburner profile
            ("Applying MSI Afterburner profile", async () => await StartupActions.RunCustom(async () => await Task.Run(() => Process.Start(new ProcessStartInfo { FileName = @"C:\Program Files (x86)\MSI Afterburner\MSIAfterburner.exe", Arguments = "/Profile1 /q" }))), () => MSI == true),

            // apply timer resolution
            ("Applying Timer Resolution " + Registry.GetValue(@"HKEY_CURRENT_USER\SOFTWARE\AutoOS", "RequestedResolution", null)?.ToString(), async () => await StartupActions.RunApplication("TimerResolution", "SetTimerResolution.exe", "--resolution " + Registry.GetValue(@"HKEY_CURRENT_USER\SOFTWARE\AutoOS", "RequestedResolution", null)?.ToString() + " --no-console"), null),
            
            // launch dwmenablemmcss
            ("Launching DWMEnableMMCSS", async () => await StartupActions.RunApplication("DWMEnableMMCSS", "DWMEnableMMCSS.exe", "--no-console"), null),

            // disable hid devices
            ("Disabling Human Interface Devices (HID)", async () => await StartupActions.RunPowerShell("Get-PnpDevice -Class HIDClass | Where-Object { $_.FriendlyName -match 'HID-compliant (consumer control device|device|game controller|system controller|vendor-defined device)' -and $_.FriendlyName -notmatch 'Mouse|Keyboard'} | Disable-PnpDevice -Confirm:$false"), () => HID == false),

            // disable xhci interrupt moderation
            ("Disabling XHCI Interrupt Moderation (IMOD)", async () => await StartupActions.RunPowerShellScript("imod.ps1", $"-disable \"{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "RwEverything", "Rw.exe")}\""), () => IMOD == false),

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

            // clean event logs
            ("Cleaning event logs", async () => await StartupActions.RunPowerShell(@"Get-EventLog -LogName * | ForEach-Object { Clear-EventLog $_.Log }"), null),
            ("Cleaning event logs", async () => await StartupActions.RunNsudo("CurrentUser", @"sc stop TrustedInstaller"), null),
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
                StartupWindow.Status.Text = actionTitle + "...";

                try
                {
                    await action();
                }
                catch (Exception ex)
                {
                    StartupWindow.Status.Text = ex.Message;
                    StartupWindow.Progress.ShowError = true;
                    return;
                }
            }

            StartupWindow.Progress.Value += incrementPerTitle;

            previousTitle = title;
        }
    }
}
