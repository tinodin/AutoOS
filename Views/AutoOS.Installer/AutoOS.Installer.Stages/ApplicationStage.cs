using System.Diagnostics;
using AutoOS.Views.Installer.Actions;
using Microsoft.UI.Xaml.Media;
using Windows.System;
using Windows.UI;

namespace AutoOS.Views.Installer.Stages;

public static class ApplicationStage
{
    public static async Task Run()
    {
        bool? iCloud = PreparingStage.iCloud;
        bool? Bitwarden = PreparingStage.Bitwarden;
        bool? OnePassword = PreparingStage.OnePassword;
        bool? StartAllBack = PreparingStage.StartAllBack;
        bool? Spotify = PreparingStage.Spotify;
        bool? AppleMusic = PreparingStage.AppleMusic;
        bool? AmazonMusic = PreparingStage.AmazonMusic;
        bool? DeezerMusic = PreparingStage.DeezerMusic;
        bool? WhatsApp = PreparingStage.WhatsApp;
        bool? Discord = PreparingStage.Discord;
        bool? EpicGames = PreparingStage.EpicGames;
        bool? Steam = PreparingStage.Steam;

        InstallPage.Status.Text = "Configuring Applications...";

        int validActionsCount = 0;
        int stagePercentage = 5;

        string icloudVersion = "";
        string bitwardenVersion = "";
        string onePasswordVersion = "";
        string spotifyVersion = "";
        string appleMusicVersion = "";
        string amazonMusicVersion = "";
        string deezerMusicVersion = "";
        string whatsAppVersion = "";
        string discordVersion = "";

        var actions = new List<(Func<Task> Action, Func<bool> Condition)>
        {
            // download icloud dependency
            (async () => await ProcessActions.RunMicrosoftStoreDownload("Downloading iCloud Dependency", "AppleInc.iCloud_nzyj5cx40ttqa", "msix", "x64", "iCloudDependency.msix"), () => iCloud == true),

            // install icloud
            (async () => await ProcessActions.RunPowerShell("Installing iCloud Dependency", @"Add-AppxPackage -Path ""$env:TEMP\iCloudDependency.msix"""), () => iCloud == true),

            // download icloud
            (async () => await ProcessActions.RunMicrosoftStoreDownload("Downloading iCloud", "AppleInc.iCloud_nzyj5cx40ttqa", "appx", "x64", "iCloud.appx"), () => iCloud == true),

            // install icloud
            (async () => await ProcessActions.RunPowerShell("Installing iCloud", @"Add-AppxPackage -Path ""$env:TEMP\iCloud.appx"""), () => iCloud == true),
            (async () => await ProcessActions.RunCustom("Installing iCloud", async () => icloudVersion = (await Task.Run(() => { var process = new Process { StartInfo = new ProcessStartInfo("powershell.exe", "Get-AppxPackage -Name \"AppleInc.iCloud\" | Select-Object -ExpandProperty Version") { RedirectStandardOutput = true, CreateNoWindow = true } }; process.Start(); return process.StandardOutput.ReadToEnd().Trim();}))), () => iCloud == true),

            // log in to icloud
            (async () => await ProcessActions.Sleep("Please log in to your iCloud account", 1000), () => iCloud == true),
            (async () => await ProcessActions.RunCustom("Please log in to your iCloud account", async () => await Task.Run(() => Process.Start(new ProcessStartInfo { FileName = Path.Combine(@"C:\Program Files\WindowsApps\AppleInc.iCloud_" + icloudVersion + "_x64__nzyj5cx40ttqa", "iCloud",  "iCloudHome.exe"), WindowStyle = ProcessWindowStyle.Maximized })!.WaitForExitAsync())), () => iCloud == true),

            // download bitwarden
            (async () => await ProcessActions.RunMicrosoftStoreDownload("Downloading Bitwarden", "8bitSolutionsLLC.bitwardendesktop_h4e712dmw3xyy", "appx", "", "Bitwarden.appx"), () => Bitwarden == true),

            // install bitwarden
            (async () => await ProcessActions.RunPowerShell("Installing Bitwarden", @"Add-AppxPackage -Path ""$env:TEMP\Bitwarden.appx"""), () => Bitwarden == true),
            (async () => await ProcessActions.RunCustom("Installing Bitwarden", async () => bitwardenVersion = (await Task.Run(() => { var process = new Process { StartInfo = new ProcessStartInfo("powershell.exe", "Get-AppxPackage -Name \"8bitSolutionsLLC.bitwardendesktop\" | Select-Object -ExpandProperty Version") { RedirectStandardOutput = true, CreateNoWindow = true } }; process.Start(); return process.StandardOutput.ReadToEnd().Trim();}))), () => Bitwarden == true),

            // log in to bitwarden
            (async () => await ProcessActions.Sleep("Please log in to your Bitwarden account", 1000), () => Bitwarden == true),
            (async () => await ProcessActions.RunCustom("Please log in to your Bitwarden account", async () => await Task.Run(() => Process.Start(new ProcessStartInfo { FileName = Path.Combine(@"C:\Program Files\WindowsApps\8bitSolutionsLLC.bitwardendesktop_" + bitwardenVersion + "_x64__h4e712dmw3xyy", "app", "Bitwarden.exe"), WindowStyle = ProcessWindowStyle.Maximized })!.WaitForExitAsync())), () => Bitwarden == true),

            // download 1password
            (async () => await ProcessActions.RunDownload("Downloading 1Password", "https://downloads.1password.com/win/1PasswordSetup-latest.exe", Path.GetTempPath(), "1PasswordSetup-latest.exe"), () => OnePassword == true),

            // install 1password
            (async () => await ProcessActions.RunNsudo("Installing 1Password", "CurrentUser", @"""%TEMP%\1PasswordSetup-latest.exe"" --silent"), () => OnePassword == true),
            (async () => await ProcessActions.RunCustom("Installing 1Password", async () => { onePasswordVersion = FileVersionInfo.GetVersionInfo(Environment.ExpandEnvironmentVariables(@"%TEMP%\1PasswordSetup-latest.exe")).ProductVersion; }), () => OnePassword == true),
            (async () => await ProcessActions.RunCustom("Installing 1Password", async () => { var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "1Password", "settings", "settings.json"); Directory.CreateDirectory(Path.GetDirectoryName(path)!); await File.WriteAllTextAsync(path, "{ \"version\": 1, \"updates.updateChannel\": \"PRODUCTION\", \"authTags\": {}, \"app.keepInTray\": false }"); }), () => OnePassword == true),

            // log in to 1password
            (async () => await ProcessActions.RunCustom("Please log in to your 1Password account", async () => await Task.Run(() => Process.GetProcessesByName("1Password").ToList().ForEach(p => p.Kill()))), () => OnePassword == true),
            (async () => await ProcessActions.RunCustom("Please log in to your 1Password account", async () => await Task.Run(() => Process.Start(new ProcessStartInfo { FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "1Password", "app", onePasswordVersion, "1Password.exe"), WindowStyle = ProcessWindowStyle.Maximized })!.WaitForExitAsync())), () => OnePassword == true),

            // download nanazip
            (async () => await ProcessActions.RunMicrosoftStoreDownload("Downloading NanaZip", "40174MouriNaruto.NanaZip_gnj4mf6z9tkrc", "msixbundle", "", "NanaZip.Msixbundle"), null),

            // install nanazip
            (async () => await ProcessActions.RunPowerShell("Installing NanaZip", @"Add-AppxPackage -Path ""$env:TEMP\NanaZip.Msixbundle"""), null),

            // download startallback
            (async () => await ProcessActions.RunDownload("Downloading StartAllBack", "https://www.startallback.com/download.php", Path.GetTempPath(), "StartAllBackSetup.exe"), () => StartAllBack == true),

            // install startallback
            (async () => await ProcessActions.RunNsudo("Installing StartAllBack", "CurrentUser", $"cmd /c reg import \"{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Scripts", "startallback.reg")}\""), () => StartAllBack == true),
            (async () => await ProcessActions.RunNsudo("Installing StartAllBack", "CurrentUser", @"""%TEMP%\StartAllBackSetup.exe"" /silent /allusers"),() => StartAllBack == true),
            (async () => await ProcessActions.RunNsudo("Installing StartAllBack", "CurrentUser", @"SCHTASKS /Change /TN ""StartAllBack Update"" /Disable"),() => StartAllBack == true),

            // activate startallback
            (async () => await ProcessActions.RunPowerShellScript("Activating StartAllBack", "startallback.ps1", ""), () => StartAllBack == true),
            (async () => await ProcessActions.Sleep("Activating StartAllBack", 2000), () => StartAllBack == true),

            // download process explorer
            (async () => await ProcessActions.RunDownload("Downloading Process Explorer", "https://download.sysinternals.com/files/ProcessExplorer.zip", Path.GetTempPath(), "ProcessExplorer.zip"), null),

            // install process explorer
            (async () => await ProcessActions.RunExtract("Installing Process Explorer", Path.Combine(Path.GetTempPath(), "ProcessExplorer.zip"), Path.Combine(Path.GetTempPath(), "ProcessExplorer")), null),
            (async () => await ProcessActions.RunCustom("Installing Process Explorer", async () => await Task.Run(() => File.Copy(Path.Combine(Path.GetTempPath(), "ProcessExplorer", "procexp64.exe"), @"C:\Windows\procexp64.exe", true))), null),
            (async () => await ProcessActions.RunNsudo("Installing Process Explorer", "CurrentUser", $"cmd /c reg import \"{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Scripts", "processexplorer.reg")}\""), null),
            (async () => await ProcessActions.RunNsudo("Installing Process Explorer", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\taskmgr.exe"" /v Debugger /t REG_SZ /d ""\""C:\Windows\procexp64.exe\"""" /f"), null),
            (async () => await ProcessActions.Sleep("Installing Process Explorer", 500), null),

            // download spotify
            (async () => await ProcessActions.RunDownload("Downloading Spotify", "https://download.scdn.co/SpotifyFullSetupX64.exe", Path.GetTempPath(), "SpotifyFullSetupX64.exe"), () => Spotify == true),

            // install spotify
            (async () => await ProcessActions.RunCustom("Installing Spotify", async () => { spotifyVersion = FileVersionInfo.GetVersionInfo(Environment.ExpandEnvironmentVariables(@"%TEMP%\SpotifyFullSetupX64.exe")).ProductVersion; }), () => Spotify == true),
            (async () => await ProcessActions.RunNsudo("Installing Spotify", "CurrentUser", @"""%TEMP%\SpotifyFullSetupX64.exe"" /extract ""%APPDATA%\Spotify"""), () => Spotify == true),
            (async () => await ProcessActions.RunNsudo("Installing Spotify", "CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Spotify"" /v ""DisplayIcon"" /t REG_SZ /d ""%AppData%\Spotify\Spotify.exe"" /f"), () => Spotify == true),
            (async () => await ProcessActions.RunNsudo("Installing Spotify", "CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Spotify"" /v ""DisplayName"" /t REG_SZ /d ""Spotify"" /f"), () => Spotify == true),
            (async () => await ProcessActions.RunNsudo("Installing Spotify", "CurrentUser", $@"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Spotify"" /v ""DisplayVersion"" /t REG_SZ /d ""{spotifyVersion}"" /f"), () => Spotify == true),
            (async () => await ProcessActions.RunNsudo("Installing Spotify", "CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Spotify"" /v ""InstallLocation"" /t REG_SZ /d ""%AppData%\Spotify"" /f"), () => Spotify == true),
            (async () => await ProcessActions.RunNsudo("Installing Spotify", "CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Spotify"" /v ""NoModify"" /t REG_DWORD /d 1 /f"), () => Spotify == true),
            (async () => await ProcessActions.RunNsudo("Installing Spotify", "CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Spotify"" /v ""NoRepair"" /t REG_DWORD /d 1 /f"), () => Spotify == true),
            (async () => await ProcessActions.RunNsudo("Installing Spotify", "CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Spotify"" /v ""Publisher"" /t REG_SZ /d ""Spotify AB"" /f"), () => Spotify == true),
            (async () => await ProcessActions.RunNsudo("Installing Spotify", "CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Spotify"" /v ""UninstallString"" /t REG_SZ /d ""%AppData%\Spotify\Spotify.exe /uninstall"" /f"), () => Spotify == true),
            (async () => await ProcessActions.RunNsudo("Installing Spotify", "CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Spotify"" /v ""URLInfoAbout"" /t REG_SZ /d ""https://www.spotify.com"" /f"), () => Spotify == true),
            (async () => await ProcessActions.RunNsudo("Installing Spotify", "CurrentUser", $@"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Spotify"" /v ""Version"" /t REG_SZ /d ""{spotifyVersion}"" /f"), () => Spotify == true),
            (async () => await ProcessActions.RunPowerShell("Installing Spotify", @"$Shell = New-Object -ComObject WScript.Shell; $Shortcut = $Shell.CreateShortcut([System.IO.Path]::Combine($env:APPDATA, 'Microsoft\Windows\Start Menu\Programs\Spotify.lnk')); $Shortcut.TargetPath = [System.IO.Path]::Combine($env:APPDATA, 'Spotify\Spotify.exe'); $Shortcut.Save()"), () => Spotify == true),

            // disable hardware acceleration
            (async () => await ProcessActions.RunCustom("Disabling hardware acceleration", async () => await File.WriteAllTextAsync(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Spotify", "prefs"), "ui.hardware_acceleration=false")), () => Spotify == true),

            // download block the spot
            (async () => await ProcessActions.RunDownload("Downloading BlockTheSpot", "https://github.com/mrpond/BlockTheSpot/releases/latest/download/chrome_elf.zip", Path.GetTempPath(), "chrome-elf.zip"), () => Spotify == true),

            // extract block the spot
            (async () => await ProcessActions.RunExtract("Installing BlockTheSpot", Path.Combine(Path.GetTempPath(), "chrome-elf.zip"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Spotify")), () => Spotify == true),
            (async () => await ProcessActions.Sleep("Installing BlockTheSpot", 200), () => Spotify == true),

            // log in to spotify
            (async () => await ProcessActions.Sleep("Please log in to your Spotify account", 1000), () => Spotify == true),
            (async () => await ProcessActions.RunCustom("Please log in to your Spotify account", async () => await Task.Run(() => Process.Start(new ProcessStartInfo { FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Spotify", "Spotify.exe"), WindowStyle = ProcessWindowStyle.Maximized })!.WaitForExitAsync())), () => Spotify == true),

            // remove startup entry
            (async () => await ProcessActions.RunNsudo("Disabling startup entry", "CurrentUser", @"reg delete ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Run"" /v ""Spotify"" /f"), () => Spotify == true),

            // download apple music
            (async () => await ProcessActions.RunMicrosoftStoreDownload("Downloading Apple Music", "AppleInc.AppleMusicWin_nzyj5cx40ttqa", "msixbundle", "", "AppleMusic.Msixbundle"), () => AppleMusic == true),

            // install apple music
            (async () => await ProcessActions.RunPowerShell("Installing Apple Music", @"Add-AppxPackage -Path ""$env:TEMP\AppleMusic.Msixbundle"""), () => AppleMusic == true),
            (async () => await ProcessActions.RunCustom("Installing Apple Music", async () => appleMusicVersion = (await Task.Run(() => { var process = new Process { StartInfo = new ProcessStartInfo("powershell.exe", "Get-AppxPackage -Name \"AppleInc.AppleMusicWin\" | Select-Object -ExpandProperty Version") { RedirectStandardOutput = true, CreateNoWindow = true } }; process.Start(); return process.StandardOutput.ReadToEnd().Trim();}))), () => AppleMusic == true),

            // log in to apple music
            (async () => await ProcessActions.Sleep("Please log in to your Apple Music account", 1000), () => AppleMusic == true),
            (async () => await ProcessActions.RunCustom("Please log in to your Apple Music account", async () => await Task.Run(() => Process.Start(new ProcessStartInfo { FileName = Path.Combine(@"C:\Program Files\WindowsApps\AppleInc.AppleMusicWin_" + appleMusicVersion + "_x64__nzyj5cx40ttqa", "AppleMusic.exe"), WindowStyle = ProcessWindowStyle.Maximized })!.WaitForExitAsync())), () => AppleMusic == true),

            // download amazon music
            (async () => await ProcessActions.RunMicrosoftStoreDownload("Downloading Amazon Music", "AmazonMobileLLC.AmazonMusic_kc6t79cpj4tp0", "appx", "x86", "AmazonMusic.appx"), () => AmazonMusic == true),

            // install amazon music
            (async () => await ProcessActions.RunPowerShell("Installing Amazon Music", @"Add-AppxPackage -Path ""$env:TEMP\AmazonMusic.appx"""), () => AmazonMusic == true),
            (async () => await ProcessActions.RunCustom("Installing Amazon Music", async () => amazonMusicVersion = (await Task.Run(() => { var process = new Process { StartInfo = new ProcessStartInfo("powershell.exe", "Get-AppxPackage -Name \"AmazonMobileLLC.AmazonMusic\" | Select-Object -ExpandProperty Version") { RedirectStandardOutput = true, CreateNoWindow = true } }; process.Start(); return process.StandardOutput.ReadToEnd().Trim();}))), () => AmazonMusic == true),

            // log in to amazon music
            (async () => await ProcessActions.RunCustom("Please log in to your Amazon Music account", async () => await Task.Run(() => Process.Start(new ProcessStartInfo { FileName = Path.Combine(@"C:\Program Files\WindowsApps\AmazonMobileLLC.AmazonMusic_" + amazonMusicVersion + "_x86__kc6t79cpj4tp0", "Amazon Music.exe"), WindowStyle = ProcessWindowStyle.Maximized })!.WaitForExitAsync())), () => AmazonMusic == true),

            // download deezer music
            (async () => await ProcessActions.RunMicrosoftStoreDownload("Downloading Deezer Music", "Deezer.62021768415AF_q7m17pa7q8kj0", "appxbundle", "", "DeezerMusic.appxbundle"), () => DeezerMusic == true),

            // install deezer music
            (async () => await ProcessActions.RunPowerShell("Installing Deezer Music", @"Add-AppxPackage -Path ""$env:TEMP\DeezerMusic.appxbundle"""), () => DeezerMusic == true),
            (async () => await ProcessActions.RunCustom("Installing Deezer Music", async () => deezerMusicVersion = (await Task.Run(() => { var process = new Process { StartInfo = new ProcessStartInfo("powershell.exe", "Get-AppxPackage -Name \"Deezer.62021768415AF\" | Select-Object -ExpandProperty Version") { RedirectStandardOutput = true, CreateNoWindow = true } }; process.Start(); return process.StandardOutput.ReadToEnd().Trim();}))), () => DeezerMusic == true),

            // log in to deezer music
            (async () => await ProcessActions.RunCustom("Please log in to your Deezer Music account", async () => await Task.Run(() => Process.Start(new ProcessStartInfo { FileName = Path.Combine(@"C:\Program Files\WindowsApps\Deezer.62021768415AF_" + deezerMusicVersion + @"_x86__q7m17pa7q8kj0\app", "Deezer.exe"), WindowStyle = ProcessWindowStyle.Maximized })!.WaitForExitAsync())), () => DeezerMusic == true),

            // download whatsapp
            (async () => await ProcessActions.RunMicrosoftStoreDownload("Downloading WhatsApp", "5319275A.WhatsAppDesktop_cv1g1gvanyjgm", "msixbundle", "", "WhatsApp.Msixbundle"), () => WhatsApp == true),

            // install whatsapp
            (async () => await ProcessActions.RunPowerShell("Installing WhatsApp", @"Add-AppxPackage -Path ""$env:TEMP\WhatsApp.Msixbundle"""), () => WhatsApp == true),
            (async () => await ProcessActions.RunCustom("Installing WhatsApp", async () => whatsAppVersion = (await Task.Run(() => { var process = new Process { StartInfo = new ProcessStartInfo("powershell.exe", "Get-AppxPackage -Name \"5319275A.WhatsAppDesktop\" | Select-Object -ExpandProperty Version") { RedirectStandardOutput = true, CreateNoWindow = true } }; process.Start(); return process.StandardOutput.ReadToEnd().Trim();}))), () => WhatsApp == true),

            // log in to whatsapp
            (async () => await ProcessActions.Sleep("Please log in to your WhatsApp account", 1000), () => WhatsApp == true),
            (async () => await ProcessActions.RunCustom("Please log in to your WhatsApp account", async () => await Task.Run(() => Process.Start(new ProcessStartInfo { FileName = Path.Combine(@"C:\Program Files\WindowsApps\5319275A.WhatsAppDesktop_" + whatsAppVersion + "_x64__cv1g1gvanyjgm", "WhatsApp.exe"), WindowStyle = ProcessWindowStyle.Maximized })!.WaitForExitAsync())), () => WhatsApp == true),

            // download discord
            (async () => await ProcessActions.RunDownload("Downloading Discord", "https://discord.com/api/downloads/distributions/app/installers/latest?channel=stable&platform=win&arch=x64", Path.GetTempPath(), "DiscordSetup.exe"), () => Discord == true),

            // install discord
            (async () => await ProcessActions.RunNsudo("Installing Discord", "CurrentUser", @"""%TEMP%\DiscordSetup.exe"" /silent"),() => Discord == true),
            (async () => await ProcessActions.RunCustom("Installing Discord", async () => { string filePath = Environment.ExpandEnvironmentVariables(@"%TEMP%\DiscordSetup.exe"); discordVersion = FileVersionInfo.GetVersionInfo(filePath).ProductVersion; }), () => Discord == true),
            (async () => await ProcessActions.RunCustom("Installing Discord", async () => await Task.Run(() => File.Copy(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Discord", "app-" + discordVersion, "installer.db"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Discord", "installer.db"), true))), () => Discord == true),

            // remove discord shortcut from the desktop
            (async () => await ProcessActions.RunNsudo("Removing desktop shortcut", "CurrentUser", @"cmd /c del /f /q ""%HOMEPATH%\Desktop\Discord.lnk"""), () => Discord == true),

            //// remove startup entry
            (async () => await ProcessActions.RunNsudo("Removing startup entry", "CurrentUser", @"reg delete ""HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Run"" /v ""Discord"" /f"), () => Discord == true),

            // disable hardware acceleration
            (async () => await ProcessActions.RunCustom("Disabling hardware acceleration", async () => await File.WriteAllTextAsync(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Discord", "settings.json"), "{\"enableHardwareAcceleration\": false, \"OPEN_ON_STARTUP\": false, \"MINIMIZE_TO_TRAY\": false}")), () => Discord == true),

            // download vencord
            (async () => await ProcessActions.RunDownload("Downloading Vencord", "https://github.com/Vencord/Installer/releases/latest/download/VencordInstallerCli.exe", Path.GetTempPath(), "VencordInstallerCli.exe"), () => Discord == true),

            // install vencord
            (async () => await ProcessActions.RunNsudo("Installing Vencord", "CurrentUser", @"cmd /c ""%TEMP%\VencordInstallerCli.exe"" -install -branch auto"), () => Discord == true),

            // import vencord settings
            (async () => await ProcessActions.RunCustom("Importing Vencord settings", async () => await Task.Run(() => Directory.CreateDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Vencord", "settings")))), () => Discord == true),
            (async () => await ProcessActions.RunCustom("Importing Vencord settings", async () => await Task.Run(() => File.Copy(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Scripts", "settings.json"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Vencord", "settings", "settings.json"), true))), () => Discord == true),
            (async () => await ProcessActions.Sleep("Importing Vencord settings", 500), () => Discord == true),

            // log in to discord
            (async () => await ProcessActions.RunCustom("Please log in to your Discord account", async () => await Task.Run(() => Process.Start(new ProcessStartInfo { FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Discord", "app-" + discordVersion, "Discord.exe"), WindowStyle = ProcessWindowStyle.Maximized })!.WaitForExitAsync())), () => Discord == true),

            // remove discord shortcut from the desktop
            (async () => await ProcessActions.RunNsudo("Removing desktop shortcut", "CurrentUser", @"cmd /c del /f /q ""%HOMEPATH%\Desktop\Discord.lnk"""), () => Discord == true),

            // debloat discord
            (async () => await ProcessActions.RunCustom("Debloating Discord", async () => await Task.Run(() => { try { Directory.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Discord", "app-" + discordVersion, "modules", "discord_cloudsync-1"), true); } catch { } })), () => Discord == true),
            (async () => await ProcessActions.RunCustom("Debloating Discord", async () => await Task.Run(() => { try { Directory.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Discord", "app-" + discordVersion, "modules", "discord_dispatch-1"), true); } catch { } })), () => Discord == true),
            (async () => await ProcessActions.RunCustom("Debloating Discord", async () => await Task.Run(() => { try { Directory.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Discord", "app-" + discordVersion, "modules", "discord_erlpack-1"), true); } catch { } })), () => Discord == true),
            (async () => await ProcessActions.RunCustom("Debloating Discord", async () => await Task.Run(() => { try { Directory.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Discord", "app-" + discordVersion, "modules", "discord_game_utils-1"), true); } catch { } })), () => Discord == true),
            (async () => await ProcessActions.RunCustom("Debloating Discord", async () => await Task.Run(() => { try { Directory.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Discord", "app-" + discordVersion, "modules", "discord_overlay2-1"), true); } catch { } })), () => Discord == true),
            (async () => await ProcessActions.RunCustom("Debloating Discord", async () => await Task.Run(() => { try { Directory.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Discord", "app-" + discordVersion, "modules", "discord_rpc-1"), true); } catch { } })), () => Discord == true),
            (async () => await ProcessActions.RunCustom("Debloating Discord", async () => await Task.Run(() => { try { Directory.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Discord", "app-" + discordVersion, "modules", "discord_spellcheck-1"), true); } catch { } })), () => Discord == true),
            (async () => await ProcessActions.RunCustom("Debloating Discord", async () => await Task.Run(() => { try { Directory.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Discord", "app-" + discordVersion, "modules", "discord_zstd-1"), true); } catch { } })), () => Discord == true),

            // download epic games launcher
            (async () => await ProcessActions.RunDownload("Downloading Epic Games Launcher", "https://launcher-public-service-prod06.ol.epicgames.com/launcher/api/installer/download/EpicGamesLauncherInstaller.msi", Path.GetTempPath(), "EpicGamesLauncherInstaller.msi"), () => EpicGames == true),

            // install epic games launcher
            (async () => await ProcessActions.RunNsudo("Installing Epic Games Launcher", "CurrentUser", @"cmd /c ""%TEMP%\EpicGamesLauncherInstaller.msi"" /qn"),() => EpicGames == true),

            // remove desktop shortcut
            (async () => await ProcessActions.RunNsudo("Removing desktop shortcut", "CurrentUser", @"cmd /c del /f /q ""C:\Users\Public\Desktop\Epic Games Launcher.lnk"""), () => EpicGames == true),

            // download update
            (async () => await ProcessActions.RunCustom("Updating Epic Games Launcher", async () => await Task.Run(() => Process.Start(new ProcessStartInfo { FileName = Path.Combine(@"C:\Program Files (x86)\Epic Games\Launcher\Portal\Binaries\Win32\EpicGamesLauncher.exe")})!.WaitForExitAsync())), () => EpicGames == true),

            // log in to epic games launcher
            (async () => await ProcessActions.Sleep("Updating Epic Games Launcher", 10000), () => EpicGames == true),

            // disable epic games services
            (async () => await ProcessActions.RunNsudo("Disabling Epic Games services", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\EpicOnlineServices"" /v ""Start"" /t REG_DWORD /d 4 /f"), () => EpicGames == true),
            (async () => await ProcessActions.RunNsudo("Disabling Epic Games services", "CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Run\AutorunsDisabled"" /v ""EpicGamesLauncher"" /t REG_SZ /d """"C:\Program Files (x86)\Epic Games\Launcher\Portal\Binaries\Win64\EpicGamesLauncher.exe"" -silent -launchcontext=boot"""" /f"), () => EpicGames == true),
            (async () => await ProcessActions.RunNsudo("Disabling Epic Games services", "CurrentUser", @"reg delete ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Run"" /v ""EpicGamesLauncher"" /f"), () => EpicGames == true),

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
