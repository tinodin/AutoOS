using AutoOS.Views.Startup.Actions;
using Microsoft.Win32;
using System.Diagnostics;

namespace AutoOS.Views.Startup.Stages;

public static class StartupStage
{
    public static async Task Run()
    {
        //Registry.SetValue(@"HKEY_CURRENT_USER\SOFTWARE\AutoOS", "RequestedResolution", 5070, RegistryValueKind.DWord);
        //Registry.SetValue(@"HKEY_CURRENT_USER\SOFTWARE\AutoOS", "Messaging", "Discord", RegistryValueKind.String);
        bool MSI = Registry.GetValue(@"HKEY_CURRENT_USER\SOFTWARE\AutoOS", "MsiProfile", null) != null;
        bool HID = Registry.GetValue(@"HKEY_CURRENT_USER\SOFTWARE\AutoOS", "HumanInterfaceDevices", "0")?.ToString() == "1";
        bool IMOD = Registry.GetValue(@"HKEY_CURRENT_USER\SOFTWARE\AutoOS", "XhciInterruptModeration", "0")?.ToString() == "1";
        bool Discord = Registry.GetValue(@"HKEY_CURRENT_USER\SOFTWARE\AutoOS", "Messaging", "")?.ToString().Contains("Discord") == true;

        string discordVersion = "";
        
        int validActionsCount = 0;
        int stagePercentage = 100;

        var actions = new List<(Func<Task> Action, Func<bool> Condition)>
        {
            // sync the time
            (async () => await StartupActions.RunNsudo("Syncing time", "CurrentUser", "net start w32time"), null),
            (async () => await StartupActions.RunNsudo("Syncing time", "CurrentUser", "w32tm /resync"), null),
            (async () => await StartupActions.RunNsudo("Syncing time", "CurrentUser", "net stop w32time"), null),

            // apply msi afterburner profile
            (async () => await StartupActions.RunCustom("Applying MSI Afterburner profile", async () => await Task.Run(() => Process.Start(new ProcessStartInfo { FileName = @"C:\Program Files (x86)\MSI Afterburner\MSIAfterburner.exe", Arguments = "/Profile1 /q" }))), () => MSI == true),

            // apply timer resolution
            (async () => await StartupActions.RunApplication("Applying Timer Resolution " + Registry.GetValue(@"HKEY_CURRENT_USER\SOFTWARE\AutoOS", "RequestedResolution", null)?.ToString(), "TimerResolution", "SetTimerResolution.exe", "--resolution " + Registry.GetValue(@"HKEY_CURRENT_USER\SOFTWARE\AutoOS", "RequestedResolution", null)?.ToString() + " --no-console"), null),
            
            // launch dwmenablemmcss
            (async () => await StartupActions.RunApplication("Launching DWMEnableMMCSS", "DWMEnableMMCSS", "DWMEnableMMCSS.exe", "--no-console"), null),

            // disable hid devices
            (async () => await StartupActions.RunPowerShell("Disabling Human Interface Devices (HID)", "Get-PnpDevice -Class HIDClass | Where-Object { $_.FriendlyName -match 'HID-compliant (consumer control device|device|game controller|system controller|vendor-defined device)' -and $_.FriendlyName -notmatch 'Mouse|Keyboard'} | Disable-PnpDevice -Confirm:$false"), () => HID == false),

            // disable xhci interrupt moderation
            (async () => await StartupActions.RunPowerShellScript("Disabling XHCI Interrupt Moderation (IMOD)", "imod.ps1", $"-disable \"{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "RwEverything", "Rw.exe")}\""),() => IMOD == false),

            // disable event trace sessions (ets)
            (async () => await StartupActions.RunNsudo("Disabling Event Trace Sessions (ETS)", "TrustedInstaller", $"cmd /c reg import \"{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Scripts", "ets-disable.reg")}\""), null),

            // clean up devices
            (async () => await StartupActions.RunApplication("Cleaning up devices", "DeviceCleanup", "DeviceCleanupCmd.exe", "/s *"), null),

            // clean up drives
            (async () => await StartupActions.RunApplication("Cleaning up drives", "DriveCleanup", "DriveCleanup.exe", ""), null),

            // debloat discord
            (async () => await StartupActions.RunCustom("Debloating Discord", async () => await Task.Run(() => { discordVersion = new DirectoryInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Discord")).GetDirectories().FirstOrDefault(d => d.Name.StartsWith("app-"))?.Name.Substring(4); })), () => Discord == true),
            (async () => await StartupActions.RunCustom("Debloating Discord", async () => await Task.Run(() => { try { Directory.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Discord", "app-" + discordVersion, "modules", "discord_cloudsync-1"), true); } catch { } })), () => Discord == true),
            (async () => await StartupActions.RunCustom("Debloating Discord", async () => await Task.Run(() => { try { Directory.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Discord", "app-" + discordVersion, "modules", "discord_dispatch-1"), true); } catch { } })), () => Discord == true),
            (async () => await StartupActions.RunCustom("Debloating Discord", async () => await Task.Run(() => { try { Directory.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Discord", "app-" + discordVersion, "modules", "discord_erlpack-1"), true); } catch { } })), () => Discord == true),
            (async () => await StartupActions.RunCustom("Debloating Discord", async () => await Task.Run(() => { try { Directory.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Discord", "app-" + discordVersion, "modules", "discord_game_utils-1"), true); } catch { } })), () => Discord == true),
            (async () => await StartupActions.RunCustom("Debloating Discord", async () => await Task.Run(() => { try { Directory.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Discord", "app-" + discordVersion, "modules", "discord_hook-1"), true); } catch { } })), () => Discord == true),
            (async () => await StartupActions.RunCustom("Debloating Discord", async () => await Task.Run(() => { try { Directory.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Discord", "app-" + discordVersion, "modules", "discord_overlay2-1"), true); } catch { } })), () => Discord == true),
            (async () => await StartupActions.RunCustom("Debloating Discord", async () => await Task.Run(() => { try { Directory.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Discord", "app-" + discordVersion, "modules", "discord_rpc-1"), true); } catch { } })), () => Discord == true),
            (async () => await StartupActions.RunCustom("Debloating Discord", async () => await Task.Run(() => { try { Directory.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Discord", "app-" + discordVersion, "modules", "discord_spellcheck-1"), true); } catch { } })), () => Discord == true),
            (async () => await StartupActions.RunCustom("Debloating Discord", async () => await Task.Run(() => { try { Directory.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Discord", "app-" + discordVersion, "modules", "discord_zstd-1"), true); } catch { } })), () => Discord == true),

            // clean temp directories
            //(async () => await StartupActions.RunNsudo("Cleaning temp directories", "TrustedInstaller", @"cmd /c del /s /f /q ""C:\Windows\Logs"""), null),
            //(async () => await StartupActions.RunNsudo("Cleaning temp directories", "TrustedInstaller", @"cmd /c del /s /f /q ""C:\Windows\SoftwareDistribution"""), null),
            //(async () => await StartupActions.RunNsudo("Cleaning temp directories", "TrustedInstaller", @"cmd /c del /s /f /q ""C:\Windows\System32\LogFiles\*.*"""), null),
            //(async () => await StartupActions.RunNsudo("Cleaning temp directories", "TrustedInstaller", @"cmd /c del /s /f /q ""C:\Windows\System32\SleepStudy\*.*"""), null),
            //(async () => await StartupActions.RunNsudo("Cleaning temp directories", "TrustedInstaller", @"cmd /c del /s /f /q ""C:\Windows\System32\sru"""), null),
            //(async () => await StartupActions.RunNsudo("Cleaning temp directories", "TrustedInstaller", @"cmd /c del /s /f /q ""C:\Windows\System32\WDI\*.*"""), null),
            //(async () => await StartupActions.RunNsudo("Cleaning temp directories", "TrustedInstaller", @"cmd /c del /s /f /q ""C:\Windows\System32\winevt\Logs\*.*"""), null),
            //(async () => await StartupActions.RunNsudo("Cleaning temp directories", "TrustedInstaller", @"cmd /c del /s /f /q ""C:\Windows\SystemTemp\*.*"""), null),
            //(async () => await StartupActions.RunNsudo("Cleaning temp directories", "TrustedInstaller", @"cmd /c del /s /f /q ""C:\Windows\Temp\*.*"""), null),
            //(async () => await StartupActions.RunNsudo("Cleaning temp directories", "CurrentUser", @"cmd /c del /s /f /q %temp%\*.*"), null),
            //(async () => await StartupActions.RunNsudo("Cleaning temp directories", "CurrentUser", @"cmd /c rd /s /q %temp%"), null),
            //(async () => await StartupActions.RunNsudo("Cleaning temp directories", "CurrentUser", @"cmd /c md %temp%"), null),

            // clean event logs
            (async () => await StartupActions.RunPowerShell("Cleaning event logs", @"Get-EventLog -LogName * | ForEach-Object { Clear-EventLog $_.Log }"), null),

            // run disk cleanup
            //(async () => await StartupActions.RunCustom("Running disk cleanup", async () => await Task.Run(() => Process.Start(new ProcessStartInfo { FileName = @"C:\Windows\System32\cleanmgr", Arguments = "/sagerun:0" })!.WaitForExitAsync())), null),
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
                    StartupWindow.Status.Text = ex.Message;
                    StartupWindow.Progress.ShowError = true;
                    break;
                }

                StartupWindow.Progress.Value += incrementPerAction;

                if (StartupWindow.Status.Text != StartupActions.previousTitle)
                {
                    await Task.Delay(400);
                }
            }
        }
    }
}
