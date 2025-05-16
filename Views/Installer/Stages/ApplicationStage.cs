using AutoOS.Views.Installer.Actions;
using Microsoft.UI.Xaml.Media;
using System.Diagnostics;
using System.Management;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace AutoOS.Views.Installer.Stages;

public static class ApplicationStage
{
    public static bool? Fortnite;
    public static async Task Run()
    {
        bool? iCloud = PreparingStage.iCloud;
        bool? Bitwarden = PreparingStage.Bitwarden;
        bool? OnePassword = PreparingStage.OnePassword;
        bool? TaskbarAlignment = PreparingStage.TaskbarAlignment;
        bool? Spotify = PreparingStage.Spotify;
        bool? AppleMusic = PreparingStage.AppleMusic;
        bool? AmazonMusic = PreparingStage.AmazonMusic;
        bool? DeezerMusic = PreparingStage.DeezerMusic;
        bool? WhatsApp = PreparingStage.WhatsApp;
        bool? Discord = PreparingStage.Discord;
        bool? EpicGames = PreparingStage.EpicGames;
        bool? EpicGamesAccount = PreparingStage.EpicGamesAccount;
        bool? EpicGamesGames = PreparingStage.EpicGamesGames;
        bool? Steam = PreparingStage.Steam;

        InstallPage.Status.Text = "Configuring Applications...";

        string previousTitle = string.Empty;
        int stagePercentage = 10;

        string icloudVersion = "";
        string bitwardenVersion = "";
        string onePasswordVersion = "";
        string spotifyVersion = "";
        string dolbyAccessVersion = "";
        string appleMusicVersion = "";
        string amazonMusicVersion = "";
        string deezerMusicVersion = "";
        string whatsAppVersion = "";
        string discordVersion = "";

        var actions = new List<(string Title, Func<Task> Action, Func<bool> Condition)>
        {
            // download icloud dependency
            ("Downloading iCloud Dependency", async () => await ProcessActions.RunMicrosoftStoreDownload("AppleInc.iCloud_nzyj5cx40ttqa", "msix", "x64", "iCloudDependency.msix", 0), () => iCloud == true),

            // install icloud
            ("Installing iCloud Dependency", async () => await ProcessActions.RunPowerShell(@"Add-AppxPackage -Path ""$env:TEMP\iCloudDependency.msix"""), () => iCloud == true),

            // download icloud
            ("Downloading iCloud", async () => await ProcessActions.RunMicrosoftStoreDownload("AppleInc.iCloud_nzyj5cx40ttqa", "appx", "x64", "iCloud.appx", 0), () => iCloud == true),

            // install icloud
            ("Installing iCloud", async () => await ProcessActions.RunPowerShell(@"Add-AppxPackage -Path ""$env:TEMP\iCloud.appx"""), () => iCloud == true),
            ("Installing iCloud", async () => await ProcessActions.RunCustom(async () => icloudVersion = (await Task.Run(() => { var process = new Process { StartInfo = new ProcessStartInfo("powershell.exe", "Get-AppxPackage -Name \"AppleInc.iCloud\" | Select-Object -ExpandProperty Version") { RedirectStandardOutput = true, CreateNoWindow = true } }; process.Start(); return process.StandardOutput.ReadToEnd().Trim();}))), () => iCloud == true),

            // log in to icloud
            ("Please log in to your iCloud account", async () => await ProcessActions.Sleep(1000), () => iCloud == true),
            ("Please log in to your iCloud account", async () => await ProcessActions.RunCustom(async () => await Task.Run(() => Process.Start(new ProcessStartInfo { FileName = Path.Combine(@"C:\Program Files\WindowsApps\AppleInc.iCloud_" + icloudVersion + "_x64__nzyj5cx40ttqa", "iCloud", "iCloudHome.exe"), WindowStyle = ProcessWindowStyle.Maximized }) !.WaitForExitAsync())), () => iCloud == true),

            // download bitwarden
            ("Downloading Bitwarden", async () => await ProcessActions.RunMicrosoftStoreDownload("8bitSolutionsLLC.bitwardendesktop_h4e712dmw3xyy", "appx", "", "Bitwarden.appx", 0), () => Bitwarden == true),

            // install bitwarden
            ("Installing Bitwarden", async () => await ProcessActions.RunPowerShell(@"Add-AppxPackage -Path ""$env:TEMP\Bitwarden.appx"""), () => Bitwarden == true),
            ("Installing Bitwarden", async () => await ProcessActions.RunCustom(async () => bitwardenVersion =(await Task.Run(() => { var process = new Process { StartInfo = new ProcessStartInfo("powershell.exe", "Get-AppxPackage -Name \"8bitSolutionsLLC.bitwardendesktop\" | Select-Object -ExpandProperty Version") { RedirectStandardOutput = true, CreateNoWindow = true } }; process.Start(); return process.StandardOutput.ReadToEnd().Trim(); }))), () => Bitwarden == true),

            // log in to bitwarden
            ("Please log in to your Bitwarden account", async () => await ProcessActions.Sleep(1000), () => Bitwarden == true),
            ("Please log in to your Bitwarden account", async () => await ProcessActions.RunCustom(async () => await Task.Run(() => Process.Start(new ProcessStartInfo { FileName = Path.Combine(@"C:\Program Files\WindowsApps\8bitSolutionsLLC.bitwardendesktop_" + bitwardenVersion + "_x64__h4e712dmw3xyy", "app", "Bitwarden.exe"), WindowStyle = ProcessWindowStyle.Maximized }) !.WaitForExitAsync())), () => Bitwarden == true),

            // download 1password
            ("Downloading 1Password", async () => await ProcessActions.RunDownload("https://downloads.1password.com/win/1PasswordSetup-latest.exe", Path.GetTempPath(), "1PasswordSetup-latest.exe"), () => OnePassword == true),

            // install 1password
            ("Installing 1Password", async () => await ProcessActions.RunNsudo("CurrentUser", @"""%TEMP%\1PasswordSetup-latest.exe"" --silent"), () => OnePassword == true),
            ("Installing 1Password", async () => await ProcessActions.RunCustom(async () => onePasswordVersion = await Task.Run(() => FileVersionInfo.GetVersionInfo(Environment.ExpandEnvironmentVariables(@"%TEMP%\1PasswordSetup-latest.exe")).ProductVersion)), () => OnePassword == true),
            ("Installing 1Password", async () => await ProcessActions.RunCustom(async () => { var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "1Password", "settings", "settings.json"); Directory.CreateDirectory(Path.GetDirectoryName(path) !); await File.WriteAllTextAsync(path, "{ \"version\": 1, \"updates.updateChannel\": \"PRODUCTION\", \"authTags\": {}, \"app.keepInTray\": false }"); }), () => OnePassword == true),

            // log in to 1password
            ("Please log in to your 1Password account", async () => await ProcessActions.RunCustom(async () => await Task.Run(() => Process.GetProcessesByName("1Password").ToList().ForEach(p => p.Kill()))), () => OnePassword == true),
            ("Please log in to your 1Password account", async () => await ProcessActions.RunCustom(async () => await Task.Run(() => Process.Start(new ProcessStartInfo { FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "1Password", "app", onePasswordVersion, "1Password.exe"), WindowStyle = ProcessWindowStyle.Maximized }) !.WaitForExitAsync())), () => OnePassword == true),

            // download nanazip
            ("Downloading NanaZip", async () => await ProcessActions.RunMicrosoftStoreDownload("40174MouriNaruto.NanaZip_gnj4mf6z9tkrc", "msixbundle", "", "NanaZip.Msixbundle", 0), null),

            // install nanazip
            ("Installing NanaZip", async () => await ProcessActions.RunPowerShell(@"Add-AppxPackage -Path ""$env:TEMP\NanaZip.Msixbundle"""), null),

            // download windhawk
            ("Downloading Windhawk", async () => await ProcessActions.RunDownload("https://www.dl.dropboxusercontent.com/scl/fi/11kr2znr49gcna1gfzwbo/Windhawk.zip?rlkey=0lsjjor8m84ewq85eixzrg1xm&st=7k835rgn&dl=0", Path.GetTempPath(), "Windhawk.zip"), null),
        
            // install windhawk
            ("Installing Windhawk", async () => await ProcessActions.RunExtract(Path.Combine(Path.GetTempPath(), "Windhawk.zip"), @"C:\Program Files\Windhawk"), null),
            ("Installing Windhawk", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"cmd /c move ""C:\Program Files\Windhawk\Windhawk"" ""%ProgramData%\Windhawk"""), null),
            ("Installing Windhawk", async () => await ProcessActions.RunNsudo("CurrentUser", $"cmd /c reg import \"{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Scripts", "windhawk.reg")}\""), null),
            ("Installing Windhawk", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"sc create Windhawk binPath= ""\""C:\Program Files\Windhawk\windhawk.exe\"" -service"" start= auto"), null),
            ("Installing Windhawk", async () => await ProcessActions.RunPowerShell(@"$s=New-Object -ComObject WScript.Shell;$sc=$s.CreateShortcut([IO.Path]::Combine($env:APPDATA,'Microsoft\Windows\Start Menu\Programs\Windhawk.lnk'));$sc.TargetPath='C:\Program Files\Windhawk\windhawk.exe';$sc.Save()"), null),
            ("Installing Windhawk", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"sc start Windhawk"), null),
            
            // download startallback
            ("Downloading StartAllBack", async () => await ProcessActions.RunDownload("https://www.startallback.com/download.php", Path.GetTempPath(), "StartAllBackSetup.exe"), null),

            // install startallback
            ("Installing StartAllBack", async () => await ProcessActions.RunNsudo("CurrentUser", $"cmd /c reg import \"{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Scripts", "startallback.reg")}\""), null),
            ("Installing StartAllBack", async () => await ProcessActions.RunNsudo("CurrentUser", @"""%TEMP%\StartAllBackSetup.exe"" /silent /allusers"), null),
            ("Installing StartAllBack", async () => await ProcessActions.RunNsudo("CurrentUser", @"SCHTASKS /Change /TN ""StartAllBack Update"" /Disable"), null),

            // activate startallback
            ("Activating StartAllBack", async () => await ProcessActions.RunPowerShellScript("startallback.ps1", ""), null),
            ("Activating StartAllBack", async () => await ProcessActions.Sleep(2000), null),

            // download process explorer
            ("Downloading Process Explorer", async () => await ProcessActions.RunDownload("https://download.sysinternals.com/files/ProcessExplorer.zip", Path.GetTempPath(), "ProcessExplorer.zip"), null),

            // install process explorer
            ("Installing Process Explorer", async () => await ProcessActions.RunExtract(Path.Combine(Path.GetTempPath(), "ProcessExplorer.zip"), Path.Combine(Path.GetTempPath(), "ProcessExplorer")), null),
            ("Installing Process Explorer", async () => await ProcessActions.RunCustom(async () => await Task.Run(() => File.Copy(Path.Combine(Path.GetTempPath(), "ProcessExplorer", "procexp64.exe"), @"C:\Windows\procexp64.exe", true))), null),
            ("Installing Process Explorer", async () => await ProcessActions.RunNsudo("CurrentUser", $"cmd /c reg import \"{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Scripts", "processexplorer.reg")}\""), null),
            ("Installing Process Explorer", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\taskmgr.exe"" /v Debugger /t REG_SZ /d ""\""C:\Windows\procexp64.exe\"""" /f"), null),
            ("Installing Process Explorer", async () => await ProcessActions.Sleep(500), null),

            // download spotify
            ("Downloading Spotify", async () => await ProcessActions.RunDownload("https://download.scdn.co/SpotifyFullSetupX64.exe", Path.GetTempPath(), "SpotifyFullSetupX64.exe"), () => Spotify == true),

            // install spotify
            ("Installing Spotify", async () => await ProcessActions.RunCustom(async () => spotifyVersion = await Task.Run(() => FileVersionInfo.GetVersionInfo(Environment.ExpandEnvironmentVariables(@"%TEMP%\SpotifyFullSetupX64.exe")).ProductVersion)), () => Spotify == true),
            ("Installing Spotify", async () => await ProcessActions.RunNsudo("CurrentUser", @"""%TEMP%\SpotifyFullSetupX64.exe"" /extract ""%APPDATA%\Spotify"""), () => Spotify == true),
            ("Installing Spotify", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Spotify"" /v ""DisplayIcon"" /t REG_SZ /d ""%AppData%\Spotify\Spotify.exe"" /f"), () => Spotify == true),
            ("Installing Spotify", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Spotify"" /v ""DisplayName"" /t REG_SZ /d ""Spotify"" /f"), () => Spotify == true),
            ("Installing Spotify", async () => await ProcessActions.RunNsudo("CurrentUser", $@"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Spotify"" /v ""DisplayVersion"" /t REG_SZ /d ""{spotifyVersion}"" /f"), () => Spotify == true),
            ("Installing Spotify", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Spotify"" /v ""InstallLocation"" /t REG_SZ /d ""%AppData%\Spotify"" /f"), () => Spotify == true),
            ("Installing Spotify", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Spotify"" /v ""NoModify"" /t REG_DWORD /d 1 /f"), () => Spotify == true),
            ("Installing Spotify", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Spotify"" /v ""NoRepair"" /t REG_DWORD /d 1 /f"), () => Spotify == true),
            ("Installing Spotify", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Spotify"" /v ""Publisher"" /t REG_SZ /d ""Spotify AB"" /f"), () => Spotify == true),
            ("Installing Spotify", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Spotify"" /v ""UninstallString"" /t REG_SZ /d ""%AppData%\Spotify\Spotify.exe /uninstall"" /f"), () => Spotify == true),
            ("Installing Spotify", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Spotify"" /v ""URLInfoAbout"" /t REG_SZ /d ""https://www.spotify.com"" /f"), () => Spotify == true),
            ("Installing Spotify", async () => await ProcessActions.RunNsudo("CurrentUser", $@"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Spotify"" /v ""Version"" /t REG_SZ /d ""{spotifyVersion}"" /f"), () => Spotify == true),
            ("Installing Spotify", async () => await ProcessActions.RunPowerShell(@"$Shell = New-Object -ComObject WScript.Shell; $Shortcut = $Shell.CreateShortcut([System.IO.Path]::Combine($env:APPDATA, 'Microsoft\Windows\Start Menu\Programs\Spotify.lnk')); $Shortcut.TargetPath = [System.IO.Path]::Combine($env:APPDATA, 'Spotify\Spotify.exe'); $Shortcut.Save()"), () => Spotify == true),

            // disable spotify hardware acceleration
            ("Disabling Spotify hardware acceleration", async () => await ProcessActions.RunCustom(async () => await File.WriteAllTextAsync(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Spotify", "prefs"), "ui.hardware_acceleration=false")), () => Spotify == true),

            // download block the spot
            ("Downloading BlockTheSpot", async () => await ProcessActions.RunDownload("https://github.com/mrpond/BlockTheSpot/releases/latest/download/chrome_elf.zip", Path.GetTempPath(), "chrome-elf.zip"), () => Spotify == true),

            // extract block the spot
            ("Installing BlockTheSpot", async () => await ProcessActions.RunExtract(Path.Combine(Path.GetTempPath(), "chrome-elf.zip"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Spotify")), () => Spotify == true),
            ("Installing BlockTheSpot", async () => await ProcessActions.Sleep(200), () => Spotify == true),

            // log in to spotify
            ("Please log in to your Spotify account", async () => await ProcessActions.Sleep(1000), () => Spotify == true),
            ("Please log in to your Spotify account", async () => await ProcessActions.RunCustom(async () => await Task.Run(() => Process.Start(new ProcessStartInfo { FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Spotify", "Spotify.exe"), WindowStyle = ProcessWindowStyle.Maximized }) !.WaitForExitAsync())), () => Spotify == true),

            // disable spotify startup entry
            ("Disabling Spotify startup entry", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run"" /v ""Spotify"" /t REG_BINARY /d ""01"" /f"), () => Spotify == true),

            // download dolby access
            ("Downloading Dolby Access", async () => await ProcessActions.RunMicrosoftStoreDownload("DolbyLaboratories.DolbyAccess_rz1tebttyb220", "msixbundle", "", "DolbyAccess.Msixbundle", 1), () => AppleMusic == true),

            // install dolby access
            ("Installing Dolby Access", async () => await ProcessActions.RunPowerShell(@"Add-AppxPackage -Path ""$env:TEMP\DolbyAccess.Msixbundle"""), () => AppleMusic == true),
            ("Installing Dolby Access", async () => await ProcessActions.RunCustom(async () => dolbyAccessVersion =(await Task.Run(() => { var process = new Process { StartInfo = new ProcessStartInfo("powershell.exe", "Get-AppxPackage -Name \"DolbyLaboratories.DolbyAccess\" | Select-Object -ExpandProperty Version") { RedirectStandardOutput = true, CreateNoWindow = true } }; process.Start(); return process.StandardOutput.ReadToEnd().Trim(); }))), () => AppleMusic == true),

            // log in to dolby access
            ("Please log in to your Dolby Access account", async () => await ProcessActions.Sleep(1000), () => AppleMusic == true),
            ("Please log in to your Dolby Access account", async () => await ProcessActions.RunCustom(async () => await Task.Run(() => Process.Start(new ProcessStartInfo { FileName = Path.Combine(@"C:\Program Files\WindowsApps\DolbyLaboratories.DolbyAccess_" + dolbyAccessVersion + "_x64__rz1tebttyb220", "DolbyAccess.exe"), WindowStyle = ProcessWindowStyle.Maximized })!.WaitForExitAsync())), () => AppleMusic == true),

            // download apple music
            ("Downloading Apple Music", async () => await ProcessActions.RunMicrosoftStoreDownload("AppleInc.AppleMusicWin_nzyj5cx40ttqa", "msixbundle", "", "AppleMusic.Msixbundle", 0), () => AppleMusic == true),

            // install apple music
            ("Installing Apple Music", async () => await ProcessActions.RunPowerShell(@"Add-AppxPackage -Path ""$env:TEMP\AppleMusic.Msixbundle"""), () => AppleMusic == true),
            ("Installing Apple Music", async () => await ProcessActions.RunCustom(async () => appleMusicVersion =(await Task.Run(() => { var process = new Process { StartInfo = new ProcessStartInfo("powershell.exe", "Get-AppxPackage -Name \"AppleInc.AppleMusicWin\" | Select-Object -ExpandProperty Version") { RedirectStandardOutput = true, CreateNoWindow = true } }; process.Start(); return process.StandardOutput.ReadToEnd().Trim(); }))), () => AppleMusic == true),

            // log in to apple music
            ("Please log in to your Apple Music account", async () => await ProcessActions.Sleep(1000), () => AppleMusic == true),
            ("Please log in to your Apple Music account", async () => await ProcessActions.RunCustom(async () => await Task.Run(() => Process.Start(new ProcessStartInfo { FileName = Path.Combine(@"C:\Program Files\WindowsApps\AppleInc.AppleMusicWin_" + appleMusicVersion + "_x64__nzyj5cx40ttqa", "AppleMusic.exe"), WindowStyle = ProcessWindowStyle.Maximized })!.WaitForExitAsync())), () => AppleMusic == true),

            // download amazon music
            ("Downloading Amazon Music", async () => await ProcessActions.RunMicrosoftStoreDownload("AmazonMobileLLC.AmazonMusic_kc6t79cpj4tp0", "appx", "x86", "AmazonMusic.appx", 0), () => AmazonMusic == true),

            // install amazon music
            ("Installing Amazon Music", async () => await ProcessActions.RunPowerShell(@"Add-AppxPackage -Path ""$env:TEMP\AmazonMusic.appx"""), () => AmazonMusic == true),
            ("Installing Amazon Music", async () => await ProcessActions.RunCustom(async () => amazonMusicVersion =(await Task.Run(() => { var process = new Process { StartInfo = new ProcessStartInfo("powershell.exe", "Get-AppxPackage -Name \"AmazonMobileLLC.AmazonMusic\" | Select-Object -ExpandProperty Version") { RedirectStandardOutput = true, CreateNoWindow = true } }; process.Start(); return process.StandardOutput.ReadToEnd().Trim(); }))), () => AmazonMusic == true),

            // log in to amazon music
            ("Please log in to your Amazon Music account", async () => await ProcessActions.RunCustom(async () => await Task.Run(() => Process.Start(new ProcessStartInfo { FileName = Path.Combine(@"C:\Program Files\WindowsApps\AmazonMobileLLC.AmazonMusic_" + amazonMusicVersion + "_x86__kc6t79cpj4tp0", "Amazon Music.exe"), WindowStyle = ProcessWindowStyle.Maximized })!.WaitForExitAsync())), () => AmazonMusic == true),

            // download deezer music
            ("Downloading Deezer Music", async () => await ProcessActions.RunMicrosoftStoreDownload("Deezer.62021768415AF_q7m17pa7q8kj0", "appxbundle", "", "DeezerMusic.appxbundle", 0), () => DeezerMusic == true),

            // install deezer music
            ("Installing Deezer Music", async () => await ProcessActions.RunPowerShell(@"Add-AppxPackage -Path ""$env:TEMP\DeezerMusic.appxbundle"""), () => DeezerMusic == true),
            ("Installing Deezer Music", async () => await ProcessActions.RunCustom(async () => deezerMusicVersion =(await Task.Run(() => { var process = new Process { StartInfo = new ProcessStartInfo("powershell.exe", "Get-AppxPackage -Name \"Deezer.62021768415AF\" | Select-Object -ExpandProperty Version") { RedirectStandardOutput = true, CreateNoWindow = true } }; process.Start(); return process.StandardOutput.ReadToEnd().Trim(); }))), () => DeezerMusic == true),

            // log in to deezer music
            ("Please log in to your Deezer Music account", async () => await ProcessActions.RunCustom(async () => await Task.Run(() => Process.Start(new ProcessStartInfo { FileName = Path.Combine(@"C:\Program Files\WindowsApps\Deezer.62021768415AF_" + deezerMusicVersion + @"_x86__q7m17pa7q8kj0\app", "Deezer.exe"), WindowStyle = ProcessWindowStyle.Maximized })!.WaitForExitAsync())), () => DeezerMusic == true),

            // download whatsapp
            ("Downloading WhatsApp", async () => await ProcessActions.RunMicrosoftStoreDownload("5319275A.WhatsAppDesktop_cv1g1gvanyjgm", "msixbundle", "", "WhatsApp.Msixbundle", 0), () => WhatsApp == true),

            // install whatsapp
            ("Installing WhatsApp", async () => await ProcessActions.RunPowerShell(@"Add-AppxPackage -Path ""$env:TEMP\WhatsApp.Msixbundle"""), () => WhatsApp == true),
            ("Installing WhatsApp", async () => await ProcessActions.RunCustom(async () => whatsAppVersion =(await Task.Run(() => { var process = new Process { StartInfo = new ProcessStartInfo("powershell.exe", "Get-AppxPackage -Name \"5319275A.WhatsAppDesktop\" | Select-Object -ExpandProperty Version") { RedirectStandardOutput = true, CreateNoWindow = true } }; process.Start(); return process.StandardOutput.ReadToEnd().Trim(); }))), () => WhatsApp == true),

            // log in to whatsapp
            ("Please log in to your WhatsApp account", async () => await ProcessActions.Sleep(1000), () => WhatsApp == true),
            ("Please log in to your WhatsApp account", async () => await ProcessActions.RunCustom(async () => await Task.Run(() => Process.Start(new ProcessStartInfo { FileName = Path.Combine(@"C:\Program Files\WindowsApps\5319275A.WhatsAppDesktop_" + whatsAppVersion + "_x64__cv1g1gvanyjgm", "WhatsApp.exe"), WindowStyle = ProcessWindowStyle.Maximized }) !.WaitForExitAsync())), () => WhatsApp == true),

            // download discord
            ("Downloading Discord", async () => await ProcessActions.RunDownload("https://discord.com/api/downloads/distributions/app/installers/latest?channel=stable&platform=win&arch=x64", Path.GetTempPath(), "DiscordSetup.exe"), () => Discord == true),

            // install discord
            ("Installing Discord", async () => await ProcessActions.RunNsudo("CurrentUser", @"""%TEMP%\DiscordSetup.exe"" /silent"), () => Discord == true),
            ("Installing Discord", async () => await ProcessActions.RunCustom(async () => discordVersion = await Task.Run(() => FileVersionInfo.GetVersionInfo(Environment.ExpandEnvironmentVariables(@"%TEMP%\DiscordSetup.exe")).ProductVersion)), () => Discord == true),
            ("Installing Discord", async () => await ProcessActions.RunCustom(async () => await Task.Run(() => File.Copy(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Discord", "app-" + discordVersion, "installer.db"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Discord", "installer.db"), true))), () => Discord == true),

            // remove discord desktop shortcut 
            ("Removing Discord desktop shortcut", async () => await ProcessActions.RunNsudo("CurrentUser", @"cmd /c del /f /q ""%HOMEPATH%\Desktop\Discord.lnk"""), () => Discord == true),

            // disable discord startup entry
            ("Disabling Discord startup entry", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run"" /v ""Discord"" /t REG_BINARY /d ""01"" /f"), () => Discord == true),

            // disable discord hardware acceleration
            ("Disabling Discord hardware acceleration", async () => await ProcessActions.RunCustom(async () => await File.WriteAllTextAsync(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Discord", "settings.json"), "{\"enableHardwareAcceleration\": false, \"OPEN_ON_STARTUP\": false, \"MINIMIZE_TO_TRAY\": false}")), () => Discord == true),

            // download vencord
            ("Downloading Vencord", async () => await ProcessActions.RunDownload("https://github.com/Vencord/Installer/releases/latest/download/VencordInstallerCli.exe", Path.GetTempPath(), "VencordInstallerCli.exe"), () => Discord == true),

            // install vencord
            ("Installing Vencord", async () => await ProcessActions.RunNsudo("CurrentUser", @"cmd /c ""%TEMP%\VencordInstallerCli.exe"" -install -branch auto"), () => Discord == true),

            // import vencord settings
            ("Importing Vencord settings", async () => await ProcessActions.RunCustom(async () => await Task.Run(() => Directory.CreateDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Vencord", "settings")))), () => Discord == true),
            ("Importing Vencord settings", async () => await ProcessActions.RunCustom(async () => await Task.Run(() => File.Copy(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Scripts", "settings.json"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Vencord", "settings", "settings.json"), true))), () => Discord == true),
            ("Importing Vencord settings", async () => await ProcessActions.Sleep(500), () => Discord == true),

            // log in to discord
            ("Please log in to your Discord account", async () => await ProcessActions.RunCustom(async () => await Task.Run(() => Process.Start(new ProcessStartInfo { FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Discord", "app-" + discordVersion, "Discord.exe"), WindowStyle = ProcessWindowStyle.Maximized }) !.WaitForExitAsync())), () => Discord == true),

            // remove discord desktop shortcut 
            ("Removing Discord desktop shortcut", async () => await ProcessActions.RunNsudo("CurrentUser", @"cmd /c del /f /q ""%HOMEPATH%\Desktop\Discord.lnk"""), () => Discord == true),

            // debloat discord
            ("Debloating Discord", async () => await ProcessActions.RunCustom(async () => await Task.Run(() => { try { Directory.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Discord", "app-" + discordVersion, "modules", "discord_cloudsync-1"), true); } catch { } })), () => Discord == true),
            ("Debloating Discord", async () => await ProcessActions.RunCustom(async () => await Task.Run(() => { try { Directory.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Discord", "app-" + discordVersion, "modules", "discord_dispatch-1"), true); } catch { } })), () => Discord == true),
            ("Debloating Discord", async () => await ProcessActions.RunCustom(async () => await Task.Run(() => { try { Directory.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Discord", "app-" + discordVersion, "modules", "discord_erlpack-1"), true); } catch { } })), () => Discord == true),
            ("Debloating Discord", async () => await ProcessActions.RunCustom(async () => await Task.Run(() => { try { Directory.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Discord", "app-" + discordVersion, "modules", "discord_game_utils-1"), true); } catch { } })), () => Discord == true),
            ("Debloating Discord", async () => await ProcessActions.RunCustom(async () => await Task.Run(() => { try { Directory.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Discord", "app-" + discordVersion, "modules", "discord_overlay2-1"), true); } catch { } })), () => Discord == true),
            ("Debloating Discord", async () => await ProcessActions.RunCustom(async () => await Task.Run(() => { try { Directory.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Discord", "app-" + discordVersion, "modules", "discord_rpc-1"), true); } catch { } })), () => Discord == true),
            ("Debloating Discord", async () => await ProcessActions.RunCustom(async () => await Task.Run(() => { try { Directory.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Discord", "app-" + discordVersion, "modules", "discord_spellcheck-1"), true); } catch { } })), () => Discord == true),
            ("Debloating Discord", async () => await ProcessActions.RunCustom(async () => await Task.Run(() => { try { Directory.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Discord", "app-" + discordVersion, "modules", "discord_zstd-1"), true); } catch { } })), () => Discord == true),

            // download epic games launcher
            ("Downloading Epic Games Launcher", async () => await ProcessActions.RunDownload("https://launcher-public-service-prod06.ol.epicgames.com/launcher/api/installer/download/EpicGamesLauncherInstaller.msi", Path.GetTempPath(), "EpicGamesLauncherInstaller.msi"), () => EpicGames == true),

            // install epic games launcher
            ("Installing Epic Games Launcher", async () => await ProcessActions.RunNsudo("CurrentUser", @"cmd /c ""%TEMP%\EpicGamesLauncherInstaller.msi"" /qn"), () => EpicGames == true),

            // remove epic games launcher desktop shortcut
            ("Removing Epic Games Launcher desktop shortcut", async () => await ProcessActions.RunNsudo("CurrentUser", @"cmd /c del /f /q ""C:\Users\Public\Desktop\Epic Games Launcher.lnk"""), () => EpicGames == true),

            // update epic games launcher
            ("Updating Epic Games Launcher", async () => await ProcessActions.RunCustom(async () => await Task.Run(() => Process.Start(new ProcessStartInfo { FileName = Path.Combine(@"C:\Program Files (x86)\Epic Games\Launcher\Portal\Binaries\Win32\EpicGamesLauncher.exe")})!.WaitForExitAsync())), () => EpicGames == true),
            ("Updating Epic Games Launcher", async () => await ProcessActions.RunCustom(async () => { while (true) { using (var searcher = new ManagementObjectSearcher($"SELECT ProcessId, CommandLine FROM Win32_Process WHERE Name = 'EpicGamesLauncher.exe'")) { foreach (ManagementObject obj in searcher.Get()) { string cmdLine = obj["CommandLine"]?.ToString() ?? ""; int pid = Convert.ToInt32(obj["ProcessId"]); if (cmdLine.Contains(@"""C:/Program Files (x86)/Epic Games/Launcher/Portal/Binaries/Win64/EpicGamesLauncher.exe""  -SaveToUserDir -Messaging", StringComparison.OrdinalIgnoreCase)) { try { Process.GetProcessById(pid).Kill(); return; } catch { } } } } await Task.Delay(100); } }), () => EpicGames == true),

            // import epic games launcher account
            ("Importing Epic Games Launcher Account", async () => await ProcessActions.RunImportEpicGamesLauncherAccount(), () => EpicGames == true && EpicGamesAccount == true),
            ("Importing Epic Games Launcher Account", async () => await ProcessActions.Sleep(1000), () => EpicGames == true && EpicGamesAccount == true),

            // import epic games launcher games
            ("Importing Epic Games Launcher Games", async () => await ProcessActions.RunImportEpicGamesLauncherGames(), () => EpicGames == true && EpicGamesGames == true),
            ("Importing Epic Games Launcher Games", async () => await ProcessActions.RunCustom(async () => Fortnite = File.Exists(@"C:\ProgramData\Epic\UnrealEngineLauncher\LauncherInstalled.dat") && (JsonNode.Parse(await File.ReadAllTextAsync(@"C:\ProgramData\Epic\UnrealEngineLauncher\LauncherInstalled.dat"))?["InstallationList"] is JsonArray installations) && installations.Any(entry => entry?["AppName"]?.ToString() == "Fortnite")) , () => EpicGames == true && EpicGamesGames == true),
            ("Importing Epic Games Launcher Games", async () => await ProcessActions.Sleep(1000), () => EpicGames == true && EpicGamesGames == true),

            // log in to epic games launcher account
            ("Please log in to your Epic Games Launcher account", async () => await ProcessActions.RunCustom(async () => await Task.Run(async () => { Process.Start(@"C:\Program Files (x86)\Epic Games\Launcher\Portal\Binaries\Win32\EpicGamesLauncher.exe"); string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "EpicGamesLauncher", "Saved", "Config", "Windows", "GameUserSettings.ini"); while (!File.Exists(path) || Regex.Match(await File.ReadAllTextAsync(path), @"Data=([^\r\n]+)").Groups[1].Value.Length < 1000) await Task.Delay(500); new[] { "EpicGamesLauncher", "EpicWebHelper" }.ToList().ForEach(name => Process.GetProcessesByName(name).ToList().ForEach(p => { p.Kill(); p.WaitForExit(); })); })), () => EpicGames == true && EpicGamesAccount == false),

            // disable epic games startup entries
            ("Disabling Epic Games startup entries", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\EpicOnlineServices"" /v ""Start"" /t REG_DWORD /d 4 /f"), () => EpicGames == true),
            ("Disabling Epic Games startup entries", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\EpicGamesUpdater"" /v ""Start"" /t REG_DWORD /d 4 /f"), () => EpicGames == true),
            ("Disabling Epic Games startup entries", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run"" /v ""EpicGamesLauncher"" /t REG_BINARY /d ""01"" /f"), () => EpicGames == true),
        
            // download steam
            ("Downloading Steam", async () => await ProcessActions.RunDownload("https://cdn.cloudflare.steamstatic.com/client/installer/SteamSetup.exe", Path.GetTempPath(), "SteamSetup.exe"), () => Steam == true),

            // install steam
            ("Installing Steam", async () => await ProcessActions.RunNsudo("CurrentUser", @"""%TEMP%\SteamSetup.exe"" /S"), () => Steam == true),

            // update steam
            ("Updating Steam", async () => await ProcessActions.RunCustom(async () => await Task.Run(() => Process.Start(new ProcessStartInfo { FileName = Path.Combine(@"C:\Program Files (x86)\Steam\Steam.exe")})!.WaitForExitAsync())), () => Steam == true),

            // log in to steam
            ("Please log in to your Steam account", async () => await ProcessActions.RunCustom(async () => await Task.Run(() => Process.Start(new ProcessStartInfo { FileName = Path.Combine(@"C:\Program Files (x86)\Steam\Steam.exe")})!.WaitForExitAsync())), () => Steam == true),

            // remove steam desktop shortcut
            ("Removing Steam desktop shortcut", async () => await ProcessActions.RunNsudo("CurrentUser", @"cmd /c del /f /q ""C:\Users\Public\Desktop\Steam.lnk"""), () => Steam == true),

            // disable steam startup entries
            ("Disabling Steam startup entries", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\Steam Client Service"" /v ""Start"" /t REG_DWORD /d 4 /f"), () => Steam == true),
            ("Disabling Steam startup entries", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run"" /v ""Steam"" /t REG_BINARY /d ""01"" /f"), () => Steam == true),
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
