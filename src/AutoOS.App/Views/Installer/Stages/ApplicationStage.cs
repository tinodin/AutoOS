using AutoOS.Common;
using AutoOS.Core.Common;
using AutoOS.Core.Helpers.Database;
using AutoOS.Core.Helpers.Download;
using AutoOS.Core.Helpers.Extract;
using AutoOS.Core.Helpers.Games;
using AutoOS.Core.Helpers.Processes;
using AutoOS.Core.Helpers.Registry;
using AutoOS.Core.Helpers.Services;
using AutoOS.Core.Helpers.Store;
using AutoOS.Core.Helpers.TaskScheduler;
using AutoOS.Views.Installer.Actions;
using Microsoft.Win32;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Windows.Win32;
using Windows.Win32.Foundation;

namespace AutoOS.Views.Installer.Stages;

public class ApplicationSelection
{
    public bool iCloud { get; set; }
    public bool Bitwarden { get; set; }
    public bool OnePassword { get; set; }
    public bool Discord { get; set; }
    public bool WhatsApp { get; set; }
    public bool EpicGames { get; set; }
    public bool Steam { get; set; }
    public bool RiotClient { get; set; }
    public bool UbisoftConnect { get; set; }
    public bool EA { get; set; }
    public bool BattleNet { get; set; }
    public bool MinecraftLauncher { get; set; }
    public bool RockstarGamesLauncher { get; set; }
    public bool FiveM { get; set; }
    public bool FACEIT { get; set; }
    public bool AppleMusic { get; set; }
    public bool Tidal { get; set; }
    public bool Qobuz { get; set; }
    public bool AmazonMusic { get; set; }
    public bool DeezerMusic { get; set; }
    public bool Spotify { get; set; }
    public bool LogitechGHub { get; set; }
    public bool LogitechOnboardMemoryManager { get; set; }
    public bool Wootility { get; set; }
    public bool SteelSeriesGG { get; set; }
    public bool RazerSynapse { get; set; }
    public bool CorsairICue { get; set; }
    public bool VisualStudio { get; set; }
    public bool VisualStudioCode { get; set; }
    public bool Antigravity { get; set; }
    public bool Git { get; set; }
    public bool Python { get; set; }
    public bool Nodejs { get; set; }
    public bool Trello { get; set; }
    public bool Word { get; set; }
    public bool Excel { get; set; }
    public bool PowerPoint { get; set; }
    public bool OneNote { get; set; }
    public bool Teams { get; set; }
    public bool Outlook { get; set; }
    public bool OneDrive { get; set; }
}

public static class ApplicationStage
{
    public static bool Fortnite;
    public static bool Valorant;

    public static List<(string Title, Func<Task> Action, Func<bool> Condition)> GetActions(IStatusReporter reporter = null, ApplicationSelection selection = null)
    {
        if (reporter == null && selection == null)
        {
            reporter = new InstallPageReporter();
        }

        string ScheduleMode = selection != null ? "" : PreparingStage.ScheduleMode;
        string LightTime = selection != null ? "" : PreparingStage.LightTime;
        string DarkTime = selection != null ? "" : PreparingStage.DarkTime;

        bool iCloud = selection?.iCloud ?? PreparingStage.iCloud;
        bool Bitwarden = selection?.Bitwarden ?? PreparingStage.Bitwarden;
        bool OnePassword = selection?.OnePassword ?? PreparingStage.OnePassword;
        bool AlwaysShowTrayIcons = selection != null ? true : PreparingStage.AlwaysShowTrayIcons;

        bool Discord = selection?.Discord ?? PreparingStage.Discord;
        bool DiscordAccount = selection != null ? false : PreparingStage.DiscordAccount;
        bool WhatsApp = selection?.WhatsApp ?? PreparingStage.WhatsApp;

        bool EpicGames = selection?.EpicGames ?? PreparingStage.EpicGames;
        bool EpicGamesAccount = selection != null ? false : PreparingStage.EpicGamesAccount;
        bool EpicGamesGames = selection != null ? false : PreparingStage.EpicGamesGames;
        bool Steam = selection?.Steam ?? PreparingStage.Steam;
        bool SteamGames = selection != null ? false : PreparingStage.SteamGames;
        bool RiotClient = selection?.RiotClient ?? PreparingStage.RiotClient;
        bool RiotClientAccount = selection != null ? false : PreparingStage.RiotClientAccount;
        bool RiotClientGames = selection != null ? false : PreparingStage.RiotClientAccount;
        bool UbisoftConnect = selection?.UbisoftConnect ?? PreparingStage.UbisoftConnect;
        bool EA = selection?.EA ?? PreparingStage.EA;
        bool BattleNet = selection?.BattleNet ?? PreparingStage.BattleNet;
        bool MinecraftLauncher = selection?.MinecraftLauncher ?? PreparingStage.MinecraftLauncher;
        bool RockstarGamesLauncher = selection?.RockstarGamesLauncher ?? PreparingStage.RockstarGamesLauncher;
        bool FiveM = selection?.FiveM ?? PreparingStage.FiveM;
        bool FACEIT = selection?.FACEIT ?? PreparingStage.FACEIT;

        bool AppleMusic = selection?.AppleMusic ?? PreparingStage.AppleMusic;
        bool Tidal = selection?.Tidal ?? PreparingStage.Tidal;
        bool Qobuz = selection?.Qobuz ?? PreparingStage.Qobuz;
        bool AmazonMusic = selection?.AmazonMusic ?? PreparingStage.AmazonMusic;
        bool DeezerMusic = selection?.DeezerMusic ?? PreparingStage.DeezerMusic;
        bool Spotify = selection?.Spotify ?? PreparingStage.Spotify;

        bool LogitechGHub = selection?.LogitechGHub ?? PreparingStage.LogitechGHub;
        bool LogitechOnboardMemoryManager = selection?.LogitechOnboardMemoryManager ?? PreparingStage.LogitechOnboardMemoryManager;
        bool Wootility = selection?.Wootility ?? PreparingStage.Wootility;
        bool SteelSeriesGG = selection?.SteelSeriesGG ?? PreparingStage.SteelSeriesGG;
        bool RazerSynapse = selection?.RazerSynapse ?? PreparingStage.RazerSynapse;
        bool CorsairICue = selection?.CorsairICue ?? PreparingStage.CorsairICue;

        bool VisualStudio = selection?.VisualStudio ?? PreparingStage.VisualStudio;
        bool VisualStudioCode = selection?.VisualStudioCode ?? PreparingStage.VisualStudioCode;
        bool Antigravity = selection?.Antigravity ?? PreparingStage.Antigravity;
        bool Git = selection?.Git ?? PreparingStage.Git;
        bool Python = selection?.Python ?? PreparingStage.Python;
        bool Nodejs = selection?.Nodejs ?? PreparingStage.Nodejs;
        bool Trello = selection?.Trello ?? PreparingStage.Trello;

        bool Word = selection?.Word ?? PreparingStage.Word;
        bool Excel = selection?.Excel ?? PreparingStage.Excel;
        bool PowerPoint = selection?.PowerPoint ?? PreparingStage.PowerPoint;
        bool OneNote = selection?.OneNote ?? PreparingStage.OneNote;
        bool Teams = selection?.Teams ?? PreparingStage.Teams;
        bool Outlook = selection?.Outlook ?? PreparingStage.Outlook;
        bool OneDrive = selection?.OneDrive ?? PreparingStage.OneDrive;
		
		string icloudVersion = "";
        string bitwardenVersion = "";
        string onePasswordVersion = "";
        string discordVersion = "";
        string whatsAppVersion = "";
        string rockstarGamesLauncherVersion = "";
        string dolbyAccessVersion = "";
        string appleMusicVersion = "";
        string tidalVersion = "";
        string amazonMusicVersion = "";
        string deezerMusicVersion = "";
        string spotifyVersion = "";
        string trelloVersion = "";

        string scheduleMode = ScheduleMode switch
        {
            "Sunset to sunrise" => "LocationService",
            "Custom" => "Custom",
            _ => ScheduleMode
        };

        var actions = new List<(string Title, Func<Task> Action, Func<bool> Condition)>
        {
            // optimize notepad settings
            ("Optimizing Notepad settings", async () => await ProcessActions.RunPowerShellScript("notepad.ps1", ""), () => selection == null),
            
            // optimize xbox gaming overlay settings
            ("Optimizing Xbox Gaming Overlay settings", async () => await ProcessActions.RunPowerShellScript("xboxgamingoverlay.ps1", ""), () => selection == null),

            // download heif image extension
            ("Downloading HEIF Image Extension", async () => await StoreHelper.Download("Microsoft.HEIFImageExtension_8wekyb3d8bbwe", reporter: reporter), () => selection == null),

            // install heif image extension
            ("Installing HEIF Image Extension", async () => await StoreHelper.Install("Microsoft.HEIFImageExtension_8wekyb3d8bbwe"), () => selection == null),

            // download mpeg-2 video extension
            ("Downloading MPEG-2 Video Extension", async () => await StoreHelper.Download("Microsoft.MPEG2VideoExtension_8wekyb3d8bbwe", reporter: reporter), () => selection == null),

            // install mpeg-2 video extension
            ("Installing MPEG-2 Video Extension", async () => await StoreHelper.Install("Microsoft.MPEG2VideoExtension_8wekyb3d8bbwe"), () => selection == null),

            // download av1 video extension
            ("Downloading AV1 Video Extension", async () => await StoreHelper.Download("Microsoft.AV1VideoExtension_8wekyb3d8bbwe", reporter: reporter), () => selection == null),

            // install av1 video extension
            ("Installing AV1 Video Extension", async () => await StoreHelper.Install("Microsoft.AV1VideoExtension_8wekyb3d8bbwe"), () => selection == null),

            // download avc encoder video extension
            ("Downloading AVC Encoder Video Extension", async () => await StoreHelper.Download("Microsoft.AVCEncoderVideoExtension_8wekyb3d8bbwe", reporter: reporter), () => selection == null),

            // install avc encoder video extension
            ("Installing AVC Encoder Video Extension", async () => await StoreHelper.Install("Microsoft.AVCEncoderVideoExtension_8wekyb3d8bbwe"), () => selection == null),

            // download dolby vision extension
            ("Downloading Dolby Vision Extension", async () => await StoreHelper.Download("DolbyLaboratories.DolbyVisionAccess_rz1tebttyb220", reporter: reporter), () => selection == null),

            // install dolby vision extension
            ("Installing Dolby Vision Extension", async () => await StoreHelper.Install("DolbyLaboratories.DolbyVisionAccess_rz1tebttyb220"), () => selection == null),

            // download gaming services
            ("Downloading Gaming Services", async () => await StoreHelper.Download("Microsoft.GamingServices_8wekyb3d8bbwe", 0, reporter: reporter), () => selection == null),

            // install gaming services
            ("Installing Gaming Services", async () => await StoreHelper.Install("Microsoft.GamingServices_8wekyb3d8bbwe"), () => selection == null),

            // download movies & tv
            ("Downloading Movies & TV", async () => await StoreHelper.Download("Microsoft.ZuneVideo_8wekyb3d8bbwe", 2, reporter: reporter), () => selection == null),

            // install movies & tv
            ("Installing Movies & TV", async () => await StoreHelper.Install("Microsoft.ZuneVideo_8wekyb3d8bbwe"), () => selection == null),

            // download icloud
            ("Downloading iCloud", async () => await StoreHelper.Download("AppleInc.iCloud_nzyj5cx40ttqa", reporter: reporter), () => iCloud == true),

            // install icloud
            ("Installing iCloud", async () => await StoreHelper.Install("AppleInc.iCloud_nzyj5cx40ttqa"), () => iCloud == true),
            ("Installing iCloud", async () => icloudVersion = StoreHelper.GetVersion("AppleInc.iCloud_nzyj5cx40ttqa"), () => iCloud == true),

            // log in to icloud
            ("Please log in to your iCloud account (Close to continue)", async () => await Task.Delay(1000), () => iCloud == true),
            ("Please log in to your iCloud account (Close to continue)", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "WindowsApps", $"AppleInc.iCloud_{icloudVersion}_x64__nzyj5cx40ttqa", "iCloud", "iCloudHome.exe"), WindowStyle = ProcessWindowStyle.Maximized })!.WaitForExitAsync(), () => iCloud == true),

            // disable icloud startup entries
            ("Disabling iCloud startup entries", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Classes\Local Settings\Software\Microsoft\Windows\CurrentVersion\AppModel\SystemAppData\AppleInc.iCloud_nzyj5cx40ttqa\iCloudHomeStartupTask", "State", 1, RegistryValueKind.DWord), () => iCloud == true),
            ("Disabling iCloud startup entries", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Classes\Local Settings\Software\Microsoft\Windows\CurrentVersion\AppModel\SystemAppData\AppleInc.iCloud_nzyj5cx40ttqa\iCloudDriveStartupTask", "State", 1, RegistryValueKind.DWord), () => iCloud == true),
            ("Disabling iCloud startup entries", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Classes\Local Settings\Software\Microsoft\Windows\CurrentVersion\AppModel\SystemAppData\AppleInc.iCloud_nzyj5cx40ttqa\iCloudCKKSStartupTask", "State", 1, RegistryValueKind.DWord), () => iCloud == true),
            ("Disabling iCloud startup entries", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Classes\Local Settings\Software\Microsoft\Windows\CurrentVersion\AppModel\SystemAppData\AppleInc.iCloud_nzyj5cx40ttqa\iCloudPhotosStartupTask", "State", 1, RegistryValueKind.DWord), () => iCloud == true),
            ("Disabling iCloud startup entries", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Classes\Local Settings\Software\Microsoft\Windows\CurrentVersion\AppModel\SystemAppData\AppleInc.iCloud_nzyj5cx40ttqa\iCloudPhotoStreamsStartupTask", "State", 1, RegistryValueKind.DWord), () => iCloud == true),

            // download bitwarden
            ("Downloading Bitwarden", async () => await StoreHelper.Download("8bitSolutionsLLC.bitwardendesktop_h4e712dmw3xyy", reporter: reporter), () => Bitwarden == true),

            // install bitwarden
            ("Installing Bitwarden", async () => await StoreHelper.Install("8bitSolutionsLLC.bitwardendesktop_h4e712dmw3xyy"), () => Bitwarden == true),
            ("Installing Bitwarden", async () => bitwardenVersion = StoreHelper.GetVersion("8bitSolutionsLLC.bitwardendesktop_h4e712dmw3xyy"), () => Bitwarden == true),

            // log in to bitwarden
            ("Please log in to your Bitwarden account (Close to continue)", async () => await Task.Delay(1000), () => Bitwarden == true),
            ("Please log in to your Bitwarden account (Close to continue)", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "WindowsApps", $"8bitSolutionsLLC.bitwardendesktop_{bitwardenVersion}_x64__h4e712dmw3xyy", "app", "Bitwarden.exe"), WindowStyle = ProcessWindowStyle.Maximized })!.WaitForExitAsync(), () => Bitwarden == true),

            // download 1password
            ("Downloading 1Password", async () => await StoreHelper.Download("DC5C6510.2032887045529_2v019pwa6amcg", reporter: reporter), () => OnePassword == true),

            // install 1password
            ("Installing 1Password", async () => await StoreHelper.Install("DC5C6510.2032887045529_2v019pwa6amcg"), () => OnePassword == true),
            ("Installing 1Password", async () => onePasswordVersion = StoreHelper.GetVersion("DC5C6510.2032887045529_2v019pwa6amcg"), () => OnePassword == true),

            // log in to 1password
            ("Please log in to your 1Password account (Close to continue)", async () => await Task.Delay(1000), () => OnePassword == true),
            ("Please log in to your 1Password account (Close to continue)", async () => { var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "1Password", "settings", "settings.json"); Directory.CreateDirectory(Path.GetDirectoryName(path) !); await File.WriteAllTextAsync(path, "{ \"version\": 1, \"updates.updateChannel\": \"PRODUCTION\", \"authTags\": {}, \"app.keepInTray\": false }"); }, () => OnePassword == true),
            ("Please log in to your 1Password account (Close to continue)", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "WindowsApps", $"DC5C6510.2032887045529_{onePasswordVersion}_x64__2v019pwa6amcg", "1Password.exe"), WindowStyle = ProcessWindowStyle.Maximized })!.WaitForExitAsync(), () => OnePassword == true),

            // disable 1password startup entry
            ("Disabling 1Password startup entry", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Classes\Local Settings\Software\Microsoft\Windows\CurrentVersion\AppModel\SystemAppData\DC5C6510.2032887045529_2v019pwa6amcg\1PasswordStartup", "State", 1, RegistryValueKind.DWord), () => OnePassword == true),

            // download nanazip
            ("Downloading NanaZip", async () => await StoreHelper.Download("40174MouriNaruto.NanaZip_8672y6p4v2rg0", reporter: reporter), () => selection == null),

            // install nanazip
            ("Installing NanaZip", async () => await StoreHelper.Install("40174MouriNaruto.NanaZip_8672y6p4v2rg0"), () => selection == null),

            //// download files
            //("Downloading Files", async () => await DownloadHelper.Download("https://files.community/appinstallers/Files.stable.appinstaller", Path.GetTempPath(), "Files.stable.appinstaller", new InstallPageReporter()), () => selection == null),

            //// install files
            //("Installing Files", async () => await ProcessActions.RunPowerShell($@"Add-AppxPackage -AppInstallerFile ""{Path.Combine(Path.GetTempPath(), "Files.stable.appinstaller")}"""), () => selection == null),
            //("Installing Files", async () => await DownloadHelper.Download("https://www.dl.dropboxusercontent.com/scl/fi/u2hcpijo21p8i0u6lj6qm/Files.zip?rlkey=e5pq2cbj4sevh5lf5jfmvv5hc&st=8o8frer3&dl=0", Path.GetTempPath(), "Files.zip", new InstallPageReporter()), () => selection == null),
            //("Installing Files", async () => await ExtractHelper.Extract(Path.Combine(Path.GetTempPath(), "Files.zip"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Packages", "Files_1y0xx7n9077q4", "LocalState")), () => selection == null),
            //("Installing Files", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Classes\Folder\shell\open\command", "", @"""%LOCALAPPDATA%\Files\Files.App.Launcher.exe"" ""%1""", RegistryValueKind.ExpandString), () => selection == null),
            //("Installing Files", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Classes\Folder\shell\open\command", "DelegateExecute", "2", RegistryValueKind.String), () => selection == null),
            //("Installing Files", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Classes\Folder\shell\explore\command", "", @"""%LOCALAPPDATA%\Files\Files.App.Launcher.exe"" ""%1""", RegistryValueKind.ExpandString), () => selection == null),
            //("Installing Files", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Classes\Folder\shell\explore\command", "DelegateExecute", "2", RegistryValueKind.String), () => selection == null),
            //("Installing Files", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Classes\CLSID\{52205fd8-5dfb-447d-801a-d0b52f2e83e1}\shell\opennewwindow\command", "", @"""%LOCALAPPDATA%\Files\Files.App.Launcher.exe""", RegistryValueKind.ExpandString), () => selection == null),
            //("Installing Files", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Classes\CLSID\{52205fd8-5dfb-447d-801a-d0b52f2e83e1}\shell\opennewwindow\command", "DelegateExecute", "2", RegistryValueKind.String), () => selection == null),

            // download everything
            ("Downloading Everything", async () => await DownloadHelper.Download("https://www.voidtools.com/Everything-1.5.0.1407a.x64-Setup.exe", Path.GetTempPath(), "Everything.exe", reporter: reporter), () => selection == null),
            
            // install everything
            ("Installing Everything", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Path.GetTempPath(), "Everything.exe"), Arguments = "/S" , WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), () => selection == null),
            ("Installing Everything", async () => Directory.CreateDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Everything")), () => selection == null),
            ("Installing Everything", async () => File.Copy(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Scripts", "Everything-1.5a.ini"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Everything", "Everything-1.5a.ini"), true), () => selection == null),
            ("Installing Everything", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Everything 1.5a", "Everything.exe"), WindowStyle = ProcessWindowStyle.Hidden, Arguments = "-install-run-on-system-startup"})!.WaitForExitAsync(), () => selection == null),
            ("Installing Everything", async () => Process.Start(new ProcessStartInfo { FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Everything 1.5a", "Everything.exe"), WindowStyle = ProcessWindowStyle.Hidden, Arguments = "-startup" }), () => selection == null),
            ("Cleaning up Everything files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "Everything.exe")), () => selection == null),

            // remove everything desktop shortcut 
            ("Removing Everything desktop shortcut", async () => File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Everything 1.5a.lnk")), () => selection == null),

            // download windhawk
            ("Downloading Windhawk", async () => await DownloadHelper.Download("https://www.dl.dropboxusercontent.com/scl/fi/1id8mn7onjmyvd5ky1hka/Windhawk.zip?rlkey=0y77dk0u8crd34gnhszpt4nqc&st=baqilmcq&dl=0", Path.GetTempPath(), "Windhawk.zip", reporter: reporter), () => selection == null),

            // install windhawk
            ("Installing Windhawk", async () => await ExtractHelper.Extract(Path.Combine(Path.GetTempPath(), "Windhawk.zip"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Windhawk")), () => selection == null),
            ("Installing Windhawk", async () => await Task.Run(() => Directory.Move(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Windhawk", "Windhawk"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Windhawk"))), () => selection == null),
            ("Installing Windhawk", async () => await Process.Start(new ProcessStartInfo { FileName = "reg.exe", Arguments = @$"import ""{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Scripts", "windhawk.reg")}""", CreateNoWindow = true })!.WaitForExitAsync(), () => selection == null),
            //("Installing Windhawk", async () => await RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Windhawk\Engine\Mods\auto-theme-switcher\Settings", "LightThemePath", LightThemePath, RegistryValueKind.String), () => selection == null),
            //("Installing Windhawk", async () => await RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Windhawk\Engine\Mods\auto-theme-switcher\Settings", "DarkThemePath", DarkThemePath, RegistryValueKind.String), () => selection == null),
            ("Installing Windhawk", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Windhawk\Engine\Mods\auto-theme-switcher", "Disabled", 1, RegistryValueKind.DWord), () => ScheduleMode == "Always Light" || ScheduleMode == "Always Dark"),
            ("Installing Windhawk", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Windhawk\Engine\Mods\auto-theme-switcher\Settings", "ScheduleMode", scheduleMode, RegistryValueKind.String), () => selection == null),
            ("Installing Windhawk", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Windhawk\Engine\Mods\auto-theme-switcher\Settings", "CustomLight", LightTime, RegistryValueKind.String), () => selection == null),
            ("Installing Windhawk", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Windhawk\Engine\Mods\auto-theme-switcher\Settings", "CustomDark", DarkTime, RegistryValueKind.String), () => selection == null),
            ("Installing Windhawk", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Windhawk\Engine\Mods\taskbar-notification-icons-show-all", "Disabled", 1, RegistryValueKind.DWord), () => AlwaysShowTrayIcons == false),
            ("Installing Windhawk", async () => await ProcessActions.RunPowerShell(@"$s=New-Object -ComObject WScript.Shell;$sc=$s.CreateShortcut([IO.Path]::Combine($env:APPDATA,'Microsoft\Windows\Start Menu\Programs\Windhawk.lnk'));$sc.TargetPath=[IO.Path]::Combine($env:ProgramFiles,'Windhawk\windhawk.exe');$sc.Save()"), () => selection == null),
            ("Installing Windhawk", async () => ServicesHelper.CreateService("Windhawk", $@"""{Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Windhawk", "windhawk.exe")}"" -service"), () => selection == null),
            ("Installing Windhawk", async () => ServicesHelper.StartService("Windhawk"), () => selection == null),
            ("Cleaning up Windhawk files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "Windhawk.zip")), () => selection == null),
            
            // download startallback
            ("Downloading StartAllBack", async () => await DownloadHelper.Download("https://www.startallback.com/download.php", Path.GetTempPath(), "StartAllBackSetup.exe", reporter: reporter), () => selection == null),

            // install startallback
            ("Installing StartAllBack", async () => await Process.Start(new ProcessStartInfo { FileName = "reg.exe", Arguments = @$"import ""{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Scripts", "startallback.reg")}""", CreateNoWindow = true })!.WaitForExitAsync(), () => selection == null),
            ("Installing StartAllBack", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Path.GetTempPath(), "StartAllBackSetup.exe"), Arguments = "/silent /allusers" , WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), () => selection == null),
            ("Installing StartAllBack", async () => TaskSchedulerHelper.Toggle(@"StartAllBack Update", false), () => selection == null),
            ("Cleaning up StartAllBack files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "StartAllBackSetup.exe")), () => selection == null),

            // activate startallback
            ("Activating StartAllBack", async () => await ProcessActions.PatchStartAllBack(), () => selection == null),
            ("Activating StartAllBack", async () => await Task.Delay(2000), () => selection == null),

            // download autoruns
            ("Downloading Autoruns", async () => await DownloadHelper.Download("https://dl.dropboxusercontent.com/scl/fi/8ze0we0fhe44xifzl6wj1/Autoruns64.exe?rlkey=798kd1lnap1d9zi24kfomdsos&st=thxzzmni&dl=0", Path.GetTempPath(), "Autoruns64.exe", reporter: reporter), () => selection == null),
            // ("Downloading Autoruns", async () => await DownloadHelper.Download("https://download.sysinternals.com/files/Autoruns.zip", Path.GetTempPath(), "Autoruns.zip", reporter: reporter), () => selection == null),

            // install autoruns
            // ("Installing Autoruns", async () => await ExtractHelper.Extract(Path.Combine(Path.GetTempPath(), "Autoruns.zip"), Path.Combine(Path.GetTempPath(), "Autoruns")), () => selection == null),
            // ("Installing Autoruns", async () => Directory.Move(Path.Combine(Path.GetTempPath(), "Autoruns"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Autoruns")), () => selection == null),
            ("Installing Autoruns", async () => Directory.CreateDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Autoruns")), () => selection == null),
            ("Installing Autoruns", async () => File.Move(Path.Combine(Path.GetTempPath(), "Autoruns64.exe"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Autoruns", "Autoruns64.exe"), true), () => selection == null),
            ("Installing Autoruns", async () => await ProcessActions.RunPowerShell(@"$Shell = New-Object -ComObject WScript.Shell; $Shortcut = $Shell.CreateShortcut([System.IO.Path]::Combine($env:ProgramData, 'Microsoft\Windows\Start Menu\Programs\Autoruns.lnk')); $Shortcut.TargetPath = [System.IO.Path]::Combine($env:ProgramFiles, 'Autoruns\Autoruns64.exe'); $Shortcut.Save()"), () => selection == null),
            ("Installing Autoruns", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Autoruns", "DisplayName", "Autoruns", RegistryValueKind.String), () => selection == null),
            ("Installing Autoruns", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Autoruns", "UninstallString", $@"cmd /c rd /s /q ""{Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Autoruns")}"" & del ""{Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), @"Microsoft\Windows\Start Menu\Programs\Autoruns.lnk")}"" & reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Autoruns"" /f", RegistryValueKind.String), () => selection == null),
            ("Installing Autoruns", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Autoruns", "DisplayIcon", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Autoruns", "Autoruns64.exe"), RegistryValueKind.String), () => selection == null),
            ("Installing Autoruns", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Autoruns", "Publisher", "Sysinternals", RegistryValueKind.String), () => selection == null),
            ("Installing Autoruns", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Sysinternals\Autoruns", "EulaAccepted", 1, RegistryValueKind.DWord), () => selection == null),
            ("Installing Autoruns", async () => await Task.Delay(500), () => selection == null),
            //("Cleaning up Autoruns files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "Autoruns.zip")), () => selection == null),

            // download process monitor
            ("Downloading Process Monitor", async () => await DownloadHelper.Download("https://download.sysinternals.com/files/ProcessMonitor.zip", Path.GetTempPath(), "ProcessMonitor.zip", reporter: reporter), () => selection == null),

            // install process monitor
            ("Installing Process Monitor", async () => await ExtractHelper.Extract(Path.Combine(Path.GetTempPath(), "ProcessMonitor.zip"), Path.Combine(Path.GetTempPath(), "ProcessMonitor")), () => selection == null),
            ("Installing Process Monitor", async () => Directory.Move(Path.Combine(Path.GetTempPath(), "ProcessMonitor"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Process Monitor")), () => selection == null),
            ("Installing Process Monitor", async () => await ProcessActions.RunPowerShell(@"$Shell = New-Object -ComObject WScript.Shell; $Shortcut = $Shell.CreateShortcut([System.IO.Path]::Combine($env:ProgramData, 'Microsoft\Windows\Start Menu\Programs\Process Monitor.lnk')); $Shortcut.TargetPath = [System.IO.Path]::Combine($env:ProgramFiles, 'Process Monitor\Procmon64.exe'); $Shortcut.Save()"), () => selection == null),
            ("Installing Process Monitor", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\ProcessMonitor", "DisplayName", "Process Monitor", RegistryValueKind.String), () => selection == null),
            ("Installing Process Monitor", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\ProcessMonitor", "UninstallString", $@"cmd /c rd /s /q ""{Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Process Monitor")}"" & del ""{Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), @"Microsoft\Windows\Start Menu\Programs\Process Monitor.lnk")}"" & reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\ProcessMonitor"" /f", RegistryValueKind.String), () => selection == null),
            ("Installing Process Monitor", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\ProcessMonitor", "DisplayIcon", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Process Monitor", "Procmon64.exe"), RegistryValueKind.String), () => selection == null),
            ("Installing Process Monitor", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\ProcessMonitor", "Publisher", "Sysinternals", RegistryValueKind.String), () => selection == null),
			("Installing Process Monitor", async () => await Process.Start(new ProcessStartInfo { FileName = "reg.exe", Arguments = $@"import ""{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Scripts", "processmonitor.reg")}""", CreateNoWindow = true })!.WaitForExitAsync(), () => selection == null),
			("Installing Process Monitor", async () => await Task.Delay(500), () => selection == null),
            ("Cleaning up Process Monitor files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "ProcessMonitor.zip")), () => selection == null),

            // download process explorer
            ("Downloading Process Explorer", async () => await DownloadHelper.Download("https://www.dl.dropboxusercontent.com/scl/fi/a8l16rp3cpcvkkryavix1/procexp64.exe?rlkey=5fec8mcmkfcxlum9a95o1xn3t&st=mjkrpc1f&dl=0", Path.GetTempPath(), "procexp64.exe", reporter: reporter), () => selection == null),
            //("Downloading Process Explorer", async () => await DownloadHelper.Download("https://download.sysinternals.com/files/ProcessExplorer.zip", Path.GetTempPath(), "ProcessExplorer.zip", new InstallPageReporter()), () => selection == null),

            // install process explorer
            //("Installing Process Explorer", async () => await ExtractHelper.Extract(Path.Combine(Path.GetTempPath(), "ProcessExplorer.zip"), Path.Combine(Path.GetTempPath(), "ProcessExplorer")), () => selection == null),
            //("Installing Process Explorer", async () => File.Copy(Path.Combine(Path.GetTempPath(), "ProcessExplorer", "procexp64.exe"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "procexp64.exe"), true), () => selection == null),
            ("Installing Process Explorer", async () => Directory.CreateDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Process Explorer")), () => selection == null),
            ("Installing Process Explorer", async () => File.Copy(Path.Combine(Path.GetTempPath(), "procexp64.exe"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Process Explorer", "procexp64.exe"), true), () => selection == null),
            ("Installing Process Explorer", async () => await ProcessActions.RunPowerShell(@"$Shell = New-Object -ComObject WScript.Shell; $Shortcut = $Shell.CreateShortcut([System.IO.Path]::Combine($env:ProgramData, 'Microsoft\Windows\Start Menu\Programs\Process Explorer.lnk')); $Shortcut.TargetPath = [System.IO.Path]::Combine($env:ProgramFiles, 'Process Explorer\procexp64.exe'); $Shortcut.Save()"), () => selection == null),
            ("Installing Process Explorer", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\ProcessExplorer", "DisplayName", "Process Explorer", RegistryValueKind.String), () => selection == null),
            ("Installing Process Explorer", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\ProcessExplorer", "UninstallString", $@"cmd /c rd /s /q ""{Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Process Explorer")}"" & del ""{Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), @"Microsoft\Windows\Start Menu\Programs\Process Explorer.lnk")}"" & reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\ProcessExplorer"" /f", RegistryValueKind.String), () => selection == null),
            ("Installing Process Explorer", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\ProcessExplorer", "DisplayIcon", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Process Explorer", "procexp64.exe"), RegistryValueKind.String), () => selection == null),
            ("Installing Process Explorer", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\ProcessExplorer", "Publisher", "Sysinternals", RegistryValueKind.String), () => selection == null),
            ("Installing Process Explorer", async () => await Process.Start(new ProcessStartInfo { FileName = "reg.exe", Arguments = $@"import ""{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Scripts", "processexplorer.reg")}""", CreateNoWindow = true })!.WaitForExitAsync(), () => selection == null),
            ("Installing Process Explorer", async () => await Task.Delay(500), () => selection == null),
            ("Cleaning up Process Explorer files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "procexp64.exe")), () => selection == null),

            // download discord
            ("Downloading Discord", async () => await DownloadHelper.Download("https://discord.com/api/downloads/distributions/app/installers/latest?channel=stable&platform=win&arch=x64", Path.GetTempPath(), "DiscordSetup.exe", reporter: reporter), () => Discord == true),

            // install discord
            ("Installing Discord", async () => discordVersion = FileVersionInfo.GetVersionInfo(Path.Combine(Path.GetTempPath(), "DiscordSetup.exe")).ProductVersion, () => Discord == true),
            ("Installing Discord", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Path.GetTempPath(), "DiscordSetup.exe"), Arguments = "/silent" , WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), () => Discord == true),
            ("Installing Discord", async () => File.Copy(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Discord", "app-" + discordVersion, "installer.db"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Discord", "installer.db"), true), () => Discord == true),
            ("Cleaning up Discord files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "DiscordSetup.exe")), () => Discord == true),

            // pin discord to the taskbar
            ("Pinning Discord to the taskbar", async () => await ProcessActions.RunPowerShellScript("taskbarpin.ps1", $@"-Type Link -Path ""{Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Microsoft\Windows\Start Menu\Programs\Discord Inc\Discord.lnk")}"""), () => Discord == true),

            // remove discord desktop shortcut 
            ("Removing Discord desktop shortcut", async () => File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Discord.lnk")), () => Discord == true),

            // disable discord startup entry
            ("Disabling Discord startup entry", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run", "Discord", new byte[] { 0x01 }, RegistryValueKind.Binary), () => Discord == true),

			// optimize discord settings
            ("Optimizing Discord settings", async () => await File.WriteAllTextAsync(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Discord", "settings.json"), "{\n  \"enableHardwareAcceleration\": false,\n  \"OPEN_ON_STARTUP\": false,\n  \"MINIMIZE_TO_TRAY\": false,\n  \"debugLogging\": false,\n  \"openasar\": {\n    \"setup\": true,\n    \"noTrack\": true\n  }\n}"), () => Discord == true),

            // download vencord
            ("Downloading Vencord", async () => await DownloadHelper.Download("https://github.com/Vencord/Installer/releases/latest/download/VencordInstallerCli.exe", Path.GetTempPath(), "VencordInstallerCli.exe", reporter: reporter), () => Discord == true),

            // install vencord
            ("Installing Vencord", async () => await Process.Start(new ProcessStartInfo { FileName = "cmd.exe", Arguments = $@"/c """"{Path.Combine(Path.GetTempPath(), "VencordInstallerCli.exe")}"" -install -install-openasar -branch auto""" , CreateNoWindow = true })!.WaitForExitAsync(), () => Discord == true),
			("Installing OpenAsar", async () => await Process.Start(new ProcessStartInfo { FileName = "cmd.exe", Arguments = $@"/c """"{Path.Combine(Path.GetTempPath(), "VencordInstallerCli.exe")}"" -install-openasar -branch auto""" , CreateNoWindow = true })!.WaitForExitAsync(), () => Discord == true),
			("Cleaning up Vencord files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "VencordInstallerCli.exe")), () => Discord == true),

            // import vencord settings
            ("Importing Vencord settings", async () => Directory.CreateDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Vencord", "settings")), () => Discord == true),
			("Importing Vencord settings", async () => File.Copy(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Scripts", "settings.json"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Vencord", "settings", "settings.json"), true), () => Discord == true),
			("Importing Vencord settings", async () => await Task.Delay(500), () => Discord == true),

            // import discord account
            ("Importing Discord Account", async () => { Process.Start(new ProcessStartInfo { FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Discord", "app-" + discordVersion, "Discord.exe"), WindowStyle = ProcessWindowStyle.Hidden }); while (!Process.GetProcessesByName("OpenWith").Any()) { await Task.Delay(500); } }, () => Discord == true && DiscordAccount == true),
            ("Importing Discord Account", async () => await Task.Delay(2000), () => Discord == true && DiscordAccount == true),
            ("Importing Discord Account", async () => { foreach (Process process in Process.GetProcessesByName("OpenWith")) { process.Kill(); process.WaitForExit(); }}, () => Discord == true && DiscordAccount == true),
            ("Importing Discord Account", async () => { foreach (Process process in Process.GetProcessesByName("Discord")) { if (process.MainWindowHandle != IntPtr.Zero) PInvoke.PostMessage((HWND)process.MainWindowHandle, PInvoke.WM_CLOSE, default(WPARAM), default(LPARAM)); else process.Kill(); } }, () => Discord == true && DiscordAccount == true),
			("Importing Discord Account", async () => await Task.Delay(2000), () => Discord == true && DiscordAccount == true),
            ("Importing Discord Account", async () => await DiscordHelper.ImportAccount(reporter), () => Discord == true && DiscordAccount == true),
			
            // log in to discord
            ("Please log in to your Discord account (Close to continue)", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Discord", "app-" + discordVersion, "Discord.exe"), WindowStyle = ProcessWindowStyle.Maximized }) !.WaitForExitAsync(), () => Discord == true && DiscordAccount == false),
			
			// set appearance to system
            ("Setting appearance to system", async () => DiscordHelper.SetSystemAppearance(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "discord", "Local Storage", "leveldb")), () => Discord == true),

            // disable game overlay
            ("Disabling game overlay", async () => DiscordHelper.DisableGameOverlay(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "discord", "Local Storage", "leveldb")), () => Discord == true),

			// disable clips
			("Disabling clips", async () => DiscordHelper.DisableClips(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "discord", "Local Storage", "leveldb")), () => Discord == true),

            // remove discord desktop shortcut 
            ("Removing Discord desktop shortcut", async () => File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Discord.lnk")), () => Discord == true),

            // download whatsapp
            ("Downloading WhatsApp", async () => await StoreHelper.Download("5319275A.WhatsAppDesktop_cv1g1gvanyjgm", reporter: reporter), () => WhatsApp == true),

            // install whatsapp
            ("Installing WhatsApp", async () => await StoreHelper.Install("5319275A.WhatsAppDesktop_cv1g1gvanyjgm"), () => WhatsApp == true),
            ("Installing WhatsApp", async () => whatsAppVersion = StoreHelper.GetVersion("5319275A.WhatsAppDesktop_cv1g1gvanyjgm"), () => WhatsApp == true),

            // disable "minimize to system tray"
			(@"Disabling ""Minimize to system tray""", async () => await ProcessActions.RunPowerShellScript("whatsapp.ps1", ""), () => WhatsApp == true),

            // pin whatsapp to the taskbar
            ("Pinning WhatsApp to the taskbar", async () => await ProcessActions.RunPowerShellScript("taskbarpin.ps1", @"-Type UWA -Path 5319275A.WhatsAppDesktop_cv1g1gvanyjgm!App"), () => WhatsApp == true),

            // log in to whatsapp
            ("Please log in to your WhatsApp account (Close to continue)", async () => await Task.Delay(1000), () => WhatsApp == true),
            ("Please log in to your WhatsApp account (Close to continue)", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "WindowsApps", $"5319275A.WhatsAppDesktop_{whatsAppVersion}_x64__cv1g1gvanyjgm", "WhatsApp.Root.exe"), WindowStyle = ProcessWindowStyle.Maximized }) !.WaitForExitAsync(), () => WhatsApp == true),

            // disable whatsapp startup entry
            ("Disabling WhatsApp startup entry", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Classes\Local Settings\Software\Microsoft\Windows\CurrentVersion\AppModel\SystemAppData\5319275A.WhatsAppDesktop_cv1g1gvanyjgm\2defd21c-0b9e-4e4e-873a-2a68c47d7da5", "State", 1, RegistryValueKind.DWord), () => WhatsApp == true),

            // download epic games launcher
            ("Downloading Epic Games Launcher", async () => await DownloadHelper.Download("https://launcher-public-service-prod06.ol.epicgames.com/launcher/api/installer/download/EpicGamesLauncherInstaller.msi", Path.GetTempPath(), "EpicGamesLauncherInstaller.msi", reporter: reporter), () => EpicGames == true),

            // install epic games launcher
            ("Installing Epic Games Launcher", async () => await Process.Start(new ProcessStartInfo { FileName = "msiexec.exe", Arguments = $@"/i ""{Path.Combine(Path.GetTempPath(), "EpicGamesLauncherInstaller.msi")}"" /qn" , WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), () => EpicGames == true),
            ("Cleaning up Epic Games Launcher files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "EpicGamesLauncherInstaller.msi")), () => EpicGames == true),

            // remove epic games launcher desktop shortcut
            ("Removing Epic Games Launcher desktop shortcut", async () => File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory), "Epic Games Launcher.lnk")), () => EpicGames == true),

            // update epic games launcher
            ("Updating Epic Games Launcher", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Epic Games", "Launcher", "Portal", "Binaries", "Win64", "EpicGamesLauncher.exe")}) !.WaitForExitAsync(), () => EpicGames == true),
            ("Updating Epic Games Launcher", async () => { while (true) { foreach (var proc in Process.GetProcessesByName("EpicGamesLauncher")) { if (ProcessesHelper.GetCommandLine(proc).Contains("-AllowSoftwareRendering -SaveToUserDir -Messaging", StringComparison.OrdinalIgnoreCase)) { proc.Kill(); return; } } await Task.Delay(100); } }, () => EpicGames == true),
            
            // import epic games launcher account
            ("Importing Epic Games Launcher Account", async () => await EpicGamesHelper.ImportAccount(), () => EpicGames == true && EpicGamesAccount == true),

            // import epic games launcher games
            ("Importing Epic Games Launcher Games", async () => await EpicGamesHelper.ImportGames(), () => EpicGames == true && EpicGamesGames == true),
            ("Importing Epic Games Launcher Games", async () => Fortnite = File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Epic", "UnrealEngineLauncher", "LauncherInstalled.dat")) && (JsonNode.Parse(await File.ReadAllTextAsync(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Epic", "UnrealEngineLauncher", "LauncherInstalled.dat")))?["InstallationList"] is JsonArray installations) && installations.Any(entry => entry?["AppName"]?.ToString() == "Fortnite") , () => EpicGames == true && EpicGamesGames == true),
            ("Importing Epic Games Launcher Games", async () => await Task.Delay(1000), () => EpicGames == true && EpicGamesGames == true),

            // log in to epic games launcher account
            ("Please log in to your Epic Games Launcher account (Close to continue)", async () => await EpicGamesHelper.EpicGamesLogin(), () => EpicGames == true && EpicGamesAccount == false),

            // disable epic games startup entries
            ("Disabling Epic Games startup entries", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\EpicOnlineServices", "Start", 4, RegistryValueKind.DWord), () => EpicGames == true),
            ("Disabling Epic Games startup entries", async () => ServicesHelper.StopService("EpicOnlineServices"), () => EpicGames == true),
            ("Disabling Epic Games startup entries", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\EpicGamesUpdater", "Start", 4, RegistryValueKind.DWord), () => EpicGames == true),
            ("Disabling Epic Games startup entries", async () => ServicesHelper.StopService("EpicGamesUpdater"), () => EpicGames == true),
            ("Disabling Epic Games startup entries", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run", "EpicGamesLauncher", new byte[] { 0x01 }, RegistryValueKind.Binary), () => EpicGames == true),
        
            // download steam
            ("Downloading Steam", async () => await DownloadHelper.Download("https://cdn.cloudflare.steamstatic.com/client/installer/SteamSetup.exe", Path.GetTempPath(), "SteamSetup.exe", reporter: reporter), () => Steam == true),

            // install steam
            ("Installing Steam", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Path.GetTempPath(), "SteamSetup.exe"), Arguments = "/S" , WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), () => Steam == true),
            ("Cleaning up Steam files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "SteamSetup.exe")), () => Steam == true),

            // update steam
            ("Updating Steam", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam", "Steam.exe") , WindowStyle = ProcessWindowStyle.Hidden }) !.WaitForExitAsync(), () => Steam == true),
            ("Updating Steam", async () => { while (Process.GetProcessesByName("steamwebhelper").Length == 0) await Task.Delay(500); }, () => Steam == true),

            // log in to steam
            ("Please log in to your Steam account (Close to continue)", async () => await SteamHelper.SteamLogin(), () => Steam == true),

            // import steam games
            ("Importing Steam Games", async () => await SteamHelper.ImportGames(), () => Steam == true && SteamGames == true),

            // remove steam desktop shortcut
            ("Removing Steam desktop shortcut", async () => File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory), "Steam.lnk")), () => Steam == true),

            // disable steam startup entry
            ("Disabling Steam startup entry", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run", "Steam", new byte[] { 0x01 }, RegistryValueKind.Binary), () => Steam == true),

            // download riot client
            ("Downloading Riot Client", async () => await DownloadHelper.Download("https://www.dl.dropboxusercontent.com/scl/fi/lhjc10gc9i31bptzw6ism/Riot-Games.zip?rlkey=07n3ek47oaus1olu86u08yw04&st=t0vspqv4&dl=0", Path.GetTempPath(), "Riot Games.zip", reporter: reporter), () => RiotClient == true),

            // install riot client
            ("Installing Riot Client", async () => await ExtractHelper.Extract(Path.Combine(Path.GetTempPath(), "Riot Games.zip"), Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.System))), () => RiotClient == true),
            ("Installing Riot Client", async () => { Process.Start(new ProcessStartInfo { FileName = Path.Combine(Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.System)), "Riot Games", "Riot Client", "RiotClientServices.exe"), WindowStyle = ProcessWindowStyle.Maximized }); while (Process.GetProcessesByName("RiotClientCrashHandler").Length == 0 || Process.GetProcessesByName("Riot Client").Length == 0 || Process.GetProcessesByName("Riot Client").Length != 7) await Task.Delay(500); }, () => RiotClient == true),
            ("Installing Riot Client", async () => { foreach (Process process in new[] { "Riot Client", "RiotClientServices", "RiotClientCrashHandler" }.SelectMany(Process.GetProcessesByName)) { process.Kill(); process.WaitForExit(); }}, () => RiotClient == true),
            ("Cleaning up Riot Client files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "Riot Games.zip")), () => RiotClient == true),

            // import riot client account
            ("Importing Riot Client Account", async () => await RiotHelper.ImportAccount(), () => RiotClient == true && RiotClientAccount == true),

			// import riot client games
            ("Importing Riot Client Games", async () => await RiotHelper.ImportGames(), () => RiotClient == true && RiotClientGames == true),
            ("Importing Riot Client Games", async () => Valorant = File.Exists(Path.Combine(RiotHelper.RiotGamesMetadataPath, "valorant.live", "valorant.live.product_settings.yaml")) && !string.IsNullOrEmpty(Regex.Match(await File.ReadAllTextAsync(Path.Combine(RiotHelper.RiotGamesMetadataPath, "valorant.live", "valorant.live.product_settings.yaml")), @"product_install_full_path:\s*(.+)").Groups[1].Value.Trim()), () => RiotClient == true && RiotClientGames == true),

            // log in to riot client
            ("Please log in to your Riot account (Close to continue)", async () => { Process.Start(new ProcessStartInfo { FileName = Path.Combine(Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.System)), "Riot Games", "Riot Client", "RiotClientServices.exe"), WindowStyle = ProcessWindowStyle.Maximized }); while (Process.GetProcessesByName("RiotClientCrashHandler").Length == 0 || Process.GetProcessesByName("Riot Client").Length == 0) await Task.Delay(500); while (Process.GetProcessesByName("Riot Client").Length > 0) await Task.Delay(500); }, () => RiotClient == true && RiotClientAccount == false),

            // optimize riot client settings
            ("Optimizing Riot Client settings", async () => { var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Riot Games\Riot Client\Config\RiotClientSettings.yaml"); await File.WriteAllTextAsync(path, Regex.Replace((await File.ReadAllTextAsync(path)).Replace("install:", "install:\n    hardware-acceleration: false"), @"(hardware-acceleration|launch_on_computer_set_by_default|enable_run_in_background_set_by_player|enable_launch_on_computer_start_set_by_player):.*", "$1: false")); }, () => RiotClient == true),

            // disable riot client startup entry
            ("Disabling Riot Client startup entry", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run", "RiotClient", new byte[] { 0x01 }, RegistryValueKind.Binary), () => RiotClient == true),

			// download vanguard
			("Downloading Vanguard", async () => await DownloadHelper.Download("https://www.dl.dropboxusercontent.com/scl/fi/emynbdc0oimyqtgh8ormc/setup.exe?rlkey=o4yii06fxauvaurqcdsgn8hna&st=3r4gvxt8&dl=0", Path.GetTempPath(), "setup.exe", new InstallPageReporter()), () => RiotClient == true),

            // install vanguard
            ("Installing Vanguard", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Path.GetTempPath(), "setup.exe"), WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), () => RiotClient == true),
            ("Cleaning up Vanguard files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "setup.exe")), () => RiotClient == true),

            // download ubisoft connect
            ("Downloading Ubisoft Connect", async () => await DownloadHelper.Download("https://static3.cdn.ubi.com/orbit/launcher_installer/UbisoftConnectInstaller.exe", Path.GetTempPath(), "UbisoftConnectInstaller.exe", reporter: reporter), () => UbisoftConnect == true),

            // install ubisoft connect
            ("Installing Ubisoft Connect", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Path.GetTempPath(), "UbisoftConnectInstaller.exe"), Arguments = "/S" , WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), () => UbisoftConnect == true),
            ("Cleaning up Ubisoft Connect files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "UbisoftConnectInstaller.exe")), () => UbisoftConnect == true),

            // log in to ubisoft connect
            ("Please log in to your Ubisoft Connect account (Close to continue)", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Ubisoft", "Ubisoft Game Launcher", "upc.exe") , WindowStyle = ProcessWindowStyle.Hidden }) !.WaitForExitAsync(), () => UbisoftConnect == true),

            // remove ubisoft connect desktop shortcut 
            ("Removing Ubisoft Connect desktop shortcut", async () => File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Ubisoft Connect.lnk")), () => UbisoftConnect == true),

            // disable ubisoft connect startup entries
            ("Disabling Ubisoft Connect startup entries", async () => TaskSchedulerHelper.Toggle(@"\Ubisoft\Ubisoft Connect Background Update", false), () => UbisoftConnect == true),
            ("Disabling Ubisoft Connect startup entries", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\UpcElevationService", "Start", 4, RegistryValueKind.DWord), () => UbisoftConnect == true),
            ("Disabling Ubisoft Connect startup entries", async () => ServicesHelper.StopService("UpcElevationService"), () => UbisoftConnect == true),

            // download ea
            ("Downloading EA", async () => await DownloadHelper.Download("https://origin-a.akamaihd.net/EA-Desktop-Client-Download/installer-releases/EAappInstaller.exe", Path.GetTempPath(), "EAappInstaller.exe", reporter: reporter), () => EA == true),

            // install ea
            ("Installing EA", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Path.GetTempPath(), "EAappInstaller.exe"), Arguments = "/s", WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), () => EA == true),
            ("Cleaning up EA files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "EAappInstaller.exe")), () => EA == true),

            // log in to ea
            ("Please log in to your EA account (Close to continue)", async () => { Process.Start(new ProcessStartInfo { FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Electronic Arts", "EA Desktop", "EA Desktop", "EADesktop.exe"), WindowStyle = ProcessWindowStyle.Maximized }); while (Process.GetProcessesByName("EADesktop").Length > 0) await Task.Delay(500); }, () => EA == true),

            // disable ea startup entry
            ("Disabling EA startup entry", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run", "EADM", new byte[] { 0x01 }, RegistryValueKind.Binary), () => EA == true),

            // remove ea desktop shortcut
            ("Removing EA desktop shortcut", async () => File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory), "EA.lnk")), () => EA == true),

            // download battle.net
            ("Downloading Battle.Net", async () => await DownloadHelper.Download("https://downloader.battle.net//download/getInstallerForGame?os=win&gameProgram=BATTLENET_APP&version=Live", Path.GetTempPath(), "Battle.net-Setup.exe", reporter: reporter), () => BattleNet == true),

            // install battle.net
            ("Installing Battle.Net", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Path.GetTempPath(), "Battle.net-Setup.exe"), Arguments = $@"--lang=enUS --installpath=""{Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)}\Battle.net""" , WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), () => BattleNet == true),
            ("Cleaning up Battle.Net files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "Battle.net-Setup.exe")), () => BattleNet == true),

            // log in to battle.net
            ("Please log in to your Battle.Net account (Close to continue)", async () => { while (Process.GetProcessesByName("Battle.net").Length >= 1) await Task.Delay(500); }, () => BattleNet == true),

            // disable battle.net startup entries
            ("Disabling Battle.Net startup entries", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\battlenet_helpersvc", "Start", 4, RegistryValueKind.DWord), () => BattleNet == true),
            ("Disabling Battle.Net startup entries", async () => ServicesHelper.StopService("battlenet_helpersvc"), () => BattleNet == true),
            ("Disabling Battle.Net startup entries", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run", "Battle.Net", new byte[] { 0x01 }, RegistryValueKind.Binary), () => BattleNet == true),

            // remove battle.net desktop shortcut
            ("Removing Battle.Net desktop shortcut", async () => File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory), "Battle.net.lnk")), () => BattleNet == true),

            // download minecraft launcher
            ("Downloading Minecraft Launcher", async () => await DownloadHelper.Download("https://launcher.mojang.com/download/MinecraftInstaller.msi", Path.GetTempPath(), "MinecraftInstaller.msi", reporter: reporter), () => MinecraftLauncher == true),

            // install minecraft launcher
            ("Installing Minecraft Launcher", async () => await Process.Start(new ProcessStartInfo { FileName = "msiexec.exe", Arguments = $@"/i ""{Path.Combine(Path.GetTempPath(), "MinecraftInstaller.msi")}"" /qn" , WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), () => MinecraftLauncher == true),
            ("Cleaning up Minecraft Launcher files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "MinecraftInstaller.msi")), () => MinecraftLauncher == true),

            // update minecraft launcher
            ("Updating Minecraft Launcher", async () => { Process.Start(new ProcessStartInfo { FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Minecraft Launcher", "MinecraftLauncher.exe") , WindowStyle = ProcessWindowStyle.Hidden }); while (Process.GetProcessesByName("MinecraftLauncher").Length == 1) await Task.Delay(500); while (Process.GetProcessesByName("MinecraftLauncher").Length == 0) await Task.Delay(500); while (Process.GetProcessesByName("MinecraftLauncher").Length == 1) await Task.Delay(100); }, () => MinecraftLauncher == true),

            // log in to minecraft launcher
            ("Please log in to your Minecraft Launcher account (Close to continue)", async () => { while (Process.GetProcessesByName("MinecraftLauncher").Length > 1) await Task.Delay(500); }, () => MinecraftLauncher == true),

            // remove minecraft launcher desktop shortcut
            ("Removing Minecraft Launcher desktop shortcut", async () => File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory), "Minecraft Launcher.lnk")), () => MinecraftLauncher == true),

            // download rockstar games launcher
            ("Downloading Rockstar Games Launcher", async () => await DownloadHelper.Download("https://gamedownloads.rockstargames.com/public/installer/Rockstar-Games-Launcher.exe", Path.GetTempPath(), "Rockstar-Games-Launcher.exe", reporter: reporter), () => RockstarGamesLauncher == true),
            
            // extract rockstar games launcher
            ("Extracting Rockstar Games Launcher", async () => rockstarGamesLauncherVersion = FileVersionInfo.GetVersionInfo(Path.Combine(Path.GetTempPath(), "Rockstar-Games-Launcher.exe")).ProductVersion, () => RockstarGamesLauncher == true),
            ("Extracting Rockstar Games Launcher", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "7-Zip", "7z.exe"), Arguments = @$"x ""{Path.Combine(Path.GetTempPath(), "Rockstar-Games-Launcher.exe")}"" -t# -aoa -bd -bb1 -o""{Path.Combine(Path.GetTempPath(), "Rockstar-Games-Launcher")}"" -y"  , CreateNoWindow = true })!.WaitForExitAsync(), () => RockstarGamesLauncher == true),
            ("Extracting Rockstar Games Launcher", async () => { Directory.CreateDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Rockstar Games", "Launcher")); Directory.CreateDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Rockstar Games", "Launcher", "Redistributables", "VCRed")); Directory.CreateDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Rockstar Games", "Launcher", "ThirdParty", "Steam")); Directory.CreateDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Rockstar Games", "Launcher", "ThirdParty", "Epic")); }, () => RockstarGamesLauncher == true),

            // install rockstar games launcher
            ("Installing Rockstar Games Launcher", async () => File.Copy(Path.Combine(Path.GetTempPath(), @"Rockstar-Games-Launcher\2.apisetstub"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Rockstar Games", "Launcher", "api-ms-win-core-console-l1-1-0.dll"), true), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => File.Copy(Path.Combine(Path.GetTempPath(), @"Rockstar-Games-Launcher\3.apisetstub"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Rockstar Games", "Launcher", "api-ms-win-core-datetime-l1-1-0.dll"), true), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => File.Copy(Path.Combine(Path.GetTempPath(), @"Rockstar-Games-Launcher\4.apisetstub"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Rockstar Games", "Launcher", "api-ms-win-core-debug-l1-1-0.dll"), true), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => File.Copy(Path.Combine(Path.GetTempPath(), @"Rockstar-Games-Launcher\5.apisetstub"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Rockstar Games", "Launcher", "api-ms-win-core-errorhandling-l1-1-0.dll"), true), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => File.Copy(Path.Combine(Path.GetTempPath(), @"Rockstar-Games-Launcher\6.apisetstub"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Rockstar Games", "Launcher", "api-ms-win-core-file-l1-1-0.dll"), true), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => File.Copy(Path.Combine(Path.GetTempPath(), @"Rockstar-Games-Launcher\7.apisetstub"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Rockstar Games", "Launcher", "api-ms-win-core-file-l1-2-0.dll"), true), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => File.Copy(Path.Combine(Path.GetTempPath(), @"Rockstar-Games-Launcher\8.apisetstub"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Rockstar Games", "Launcher", "api-ms-win-core-file-l2-1-0.dll"), true), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => File.Copy(Path.Combine(Path.GetTempPath(), @"Rockstar-Games-Launcher\9.apisetstub"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Rockstar Games", "Launcher", "api-ms-win-core-handle-l1-1-0.dll"), true), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => File.Copy(Path.Combine(Path.GetTempPath(), @"Rockstar-Games-Launcher\10.apisetstub"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Rockstar Games", "Launcher", "api-ms-win-core-heap-l1-1-0.dll"), true), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => File.Copy(Path.Combine(Path.GetTempPath(), @"Rockstar-Games-Launcher\11.apisetstub"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Rockstar Games", "Launcher", "api-ms-win-core-interlocked-l1-1-0.dll"), true), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => File.Copy(Path.Combine(Path.GetTempPath(), @"Rockstar-Games-Launcher\12.apisetstub"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Rockstar Games", "Launcher", "api-ms-win-core-libraryloader-l1-1-0.dll"), true), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => File.Copy(Path.Combine(Path.GetTempPath(), @"Rockstar-Games-Launcher\13.apisetstub"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Rockstar Games", "Launcher", "api-ms-win-core-localization-l1-2-0.dll"), true), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => File.Copy(Path.Combine(Path.GetTempPath(), @"Rockstar-Games-Launcher\14.apisetstub"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Rockstar Games", "Launcher", "api-ms-win-core-memory-l1-1-0.dll"), true), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => File.Copy(Path.Combine(Path.GetTempPath(), @"Rockstar-Games-Launcher\15.apisetstub"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Rockstar Games", "Launcher", "api-ms-win-core-namedpipe-l1-1-0.dll"), true), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => File.Copy(Path.Combine(Path.GetTempPath(), @"Rockstar-Games-Launcher\16.apisetstub"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Rockstar Games", "Launcher", "api-ms-win-core-processenvironment-l1-1-0.dll"), true), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => File.Copy(Path.Combine(Path.GetTempPath(), @"Rockstar-Games-Launcher\17.apisetstub"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Rockstar Games", "Launcher", "api-ms-win-core-processthreads-l1-1-0.dll"), true), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => File.Copy(Path.Combine(Path.GetTempPath(), @"Rockstar-Games-Launcher\18.apisetstub"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Rockstar Games", "Launcher", "api-ms-win-core-processthreads-l1-1-1.dll"), true), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => File.Copy(Path.Combine(Path.GetTempPath(), @"Rockstar-Games-Launcher\19.apisetstub"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Rockstar Games", "Launcher", "api-ms-win-core-profile-l1-1-0.dll"), true), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => File.Copy(Path.Combine(Path.GetTempPath(), @"Rockstar-Games-Launcher\20.apisetstub"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Rockstar Games", "Launcher", "api-ms-win-core-rtlsupport-l1-1-0.dll"), true), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => File.Copy(Path.Combine(Path.GetTempPath(), @"Rockstar-Games-Launcher\21.apisetstub"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Rockstar Games", "Launcher", "api-ms-win-core-string-l1-1-0.dll"), true), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => File.Copy(Path.Combine(Path.GetTempPath(), @"Rockstar-Games-Launcher\22.apisetstub"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Rockstar Games", "Launcher", "api-ms-win-core-synch-l1-1-0.dll"), true), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => File.Copy(Path.Combine(Path.GetTempPath(), @"Rockstar-Games-Launcher\23.apisetstub"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Rockstar Games", "Launcher", "api-ms-win-core-synch-l1-2-0.dll"), true), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => File.Copy(Path.Combine(Path.GetTempPath(), @"Rockstar-Games-Launcher\24.apisetstub"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Rockstar Games", "Launcher", "api-ms-win-core-sysinfo-l1-1-0.dll"), true), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => File.Copy(Path.Combine(Path.GetTempPath(), @"Rockstar-Games-Launcher\25.apisetstub"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Rockstar Games", "Launcher", "api-ms-win-core-timezone-l1-1-0.dll"), true), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => File.Copy(Path.Combine(Path.GetTempPath(), @"Rockstar-Games-Launcher\26.apisetstub"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Rockstar Games", "Launcher", "api-ms-win-core-util-l1-1-0.dll"), true), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => File.Copy(Path.Combine(Path.GetTempPath(), @"Rockstar-Games-Launcher\27.apisetstub"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Rockstar Games", "Launcher", "api-ms-win-crt-conio-l1-1-0.dll"), true), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => File.Copy(Path.Combine(Path.GetTempPath(), @"Rockstar-Games-Launcher\28.apisetstub"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Rockstar Games", "Launcher", "api-ms-win-crt-convert-l1-1-0.dll"), true), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => File.Copy(Path.Combine(Path.GetTempPath(), @"Rockstar-Games-Launcher\29.apisetstub"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Rockstar Games", "Launcher", "api-ms-win-crt-environment-l1-1-0.dll"), true), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => File.Copy(Path.Combine(Path.GetTempPath(), @"Rockstar-Games-Launcher\30.apisetstub"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Rockstar Games", "Launcher", "api-ms-win-crt-filesystem-l1-1-0.dll"), true), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => File.Copy(Path.Combine(Path.GetTempPath(), @"Rockstar-Games-Launcher\31.apisetstub"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Rockstar Games", "Launcher", "api-ms-win-crt-heap-l1-1-0.dll"), true), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => File.Copy(Path.Combine(Path.GetTempPath(), @"Rockstar-Games-Launcher\32.apisetstub"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Rockstar Games", "Launcher", "api-ms-win-crt-locale-l1-1-0.dll"), true), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => File.Copy(Path.Combine(Path.GetTempPath(), @"Rockstar-Games-Launcher\33.apisetstub"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Rockstar Games", "Launcher", "api-ms-win-crt-math-l1-1-0.dll"), true), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => File.Copy(Path.Combine(Path.GetTempPath(), @"Rockstar-Games-Launcher\34.apisetstub"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Rockstar Games", "Launcher", "api-ms-win-crt-multibyte-l1-1-0.dll"), true), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => File.Copy(Path.Combine(Path.GetTempPath(), @"Rockstar-Games-Launcher\35.apisetstub"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Rockstar Games", "Launcher", "api-ms-win-crt-private-l1-1-0.dll"), true), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => File.Copy(Path.Combine(Path.GetTempPath(), @"Rockstar-Games-Launcher\36.apisetstub"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Rockstar Games", "Launcher", "api-ms-win-crt-process-l1-1-0.dll"), true), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => File.Copy(Path.Combine(Path.GetTempPath(), @"Rockstar-Games-Launcher\37.apisetstub"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Rockstar Games", "Launcher", "api-ms-win-crt-runtime-l1-1-0.dll"), true), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => File.Copy(Path.Combine(Path.GetTempPath(), @"Rockstar-Games-Launcher\38.apisetstub"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Rockstar Games", "Launcher", "api-ms-win-crt-stdio-l1-1-0.dll"), true), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => File.Copy(Path.Combine(Path.GetTempPath(), @"Rockstar-Games-Launcher\39.apisetstub"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Rockstar Games", "Launcher", "api-ms-win-crt-string-l1-1-0.dll"), true), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => File.Copy(Path.Combine(Path.GetTempPath(), @"Rockstar-Games-Launcher\40.apisetstub"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Rockstar Games", "Launcher", "api-ms-win-crt-time-l1-1-0.dll"), true), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => File.Copy(Path.Combine(Path.GetTempPath(), @"Rockstar-Games-Launcher\41.apisetstub"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Rockstar Games", "Launcher", "api-ms-win-crt-utility-l1-1-0.dll"), true), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => File.Copy(Path.Combine(Path.GetTempPath(), @"Rockstar-Games-Launcher\42.Launcher.exe"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Rockstar Games", "Launcher", "Launcher.exe"), true), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => File.Copy(Path.Combine(Path.GetTempPath(), @"Rockstar-Games-Launcher\43"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Rockstar Games", "Launcher", "Launcher.rpf"), true), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => File.Copy(Path.Combine(Path.GetTempPath(), @"Rockstar-Games-Launcher\44.LauncherPatcher.exe"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Rockstar Games", "Launcher", "LauncherPatcher.exe"), true), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => File.Copy(Path.Combine(Path.GetTempPath(), @"Rockstar-Games-Launcher\45.dll"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Rockstar Games", "Launcher", "libovr.dll"), true), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => File.Copy(Path.Combine(Path.GetTempPath(), @"Rockstar-Games-Launcher\46.zip"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Rockstar Games", "Launcher", "offline.pak"), true), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => File.Copy(Path.Combine(Path.GetTempPath(), @"Rockstar-Games-Launcher\48.RockstarService.exe"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Rockstar Games", "Launcher", "RockstarService.exe"), true), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => File.Copy(Path.Combine(Path.GetTempPath(), @"Rockstar-Games-Launcher\49.RockstarSteamHelper.exe"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Rockstar Games", "Launcher", "RockstarSteamHelper.exe"), true), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => File.Copy(Path.Combine(Path.GetTempPath(), @"Rockstar-Games-Launcher\50.ucrtbase.dll"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Rockstar Games", "Launcher", "ucrtbase.dll"), true), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => File.Copy(Path.Combine(Path.GetTempPath(), @"Rockstar-Games-Launcher\51.Rockstar-Games-Launcher.exe"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Rockstar Games", "Launcher", "uninstall.exe"), true), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => File.Copy(Path.Combine(Path.GetTempPath(), @"Rockstar-Games-Launcher\52.exe"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Rockstar Games", "Launcher", "Redistributables", "VCRed", "vc_redist.x64.exe"), true), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => File.Copy(Path.Combine(Path.GetTempPath(), @"Rockstar-Games-Launcher\53.exe"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Rockstar Games", "Launcher", "Redistributables", "VCRed", "vc_redist.x86.exe"), true), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => File.Copy(Path.Combine(Path.GetTempPath(), @"Rockstar-Games-Launcher\54.steam_api.dll"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Rockstar Games", "Launcher", "ThirdParty", "Steam", "steam_api64.dll"), true), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => File.Copy(Path.Combine(Path.GetTempPath(), @"Rockstar-Games-Launcher\55.EOSSDK-Win64-Shipping.dll"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Rockstar Games", "Launcher", "ThirdParty", "Epic", "EOSSDK-Win64-Shipping.dll"), true), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => File.Copy(Path.Combine(Path.GetTempPath(), @"Rockstar-Games-Launcher\56.XboxHelper.dll"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Rockstar Games", "Launcher", "RockstarXboxHelper.dll"), true), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Rockstar Games Launcher", "DisplayName", "Rockstar Games Launcher", RegistryValueKind.String), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Rockstar Games Launcher", "Comments", "Rockstar Games Launcher", RegistryValueKind.String), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Rockstar Games Launcher", "UninstallString", $@"""{Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Rockstar Games", "Launcher", "uninstall.exe")}""", RegistryValueKind.String), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Rockstar Games Launcher", "QuietUninstallString", $@"""{Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Rockstar Games", "Launcher", "uninstall.exe")}"" /S", RegistryValueKind.String), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Rockstar Games Launcher", "InstallLocation", $@"""{Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Rockstar Games", "Launcher")}""", RegistryValueKind.String), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Rockstar Games Launcher", "DisplayIcon", $@"{Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Rockstar Games", "Launcher", "Launcher.exe")}, 0", RegistryValueKind.String), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Rockstar Games Launcher", "Publisher", "Rockstar Games", RegistryValueKind.String), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Rockstar Games Launcher", "HelpLink", "https://www.rockstargames.com/support", RegistryValueKind.String), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Rockstar Games Launcher", "Readme", "https://www.rockstargames.com/support", RegistryValueKind.String), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Rockstar Games Launcher", "URLUpdateInfo", "https://www.rockstargames.com", RegistryValueKind.String), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Rockstar Games Launcher", "URLInfoAbout", "https://www.rockstargames.com/support", RegistryValueKind.String), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Rockstar Games Launcher", "DisplayVersion", rockstarGamesLauncherVersion, RegistryValueKind.String), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Rockstar Games Launcher", "NoModify", 1, RegistryValueKind.DWord), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Rockstar Games Launcher", "NoRepair", 1, RegistryValueKind.DWord), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Rockstar Games Launcher", "EstimatedSize", 0x927c0, RegistryValueKind.DWord), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Rockstar Games\Launcher", "Version", rockstarGamesLauncherVersion, RegistryValueKind.String), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Rockstar Games\Launcher", "InstallFolder", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Rockstar Games", "Launcher"), RegistryValueKind.String), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Rockstar Games\Launcher", "Language", "en-US", RegistryValueKind.String), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Rockstar Games\Launcher", "Shortcut", 1, RegistryValueKind.DWord), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Rockstar Games\Launcher", "Silent", 0, RegistryValueKind.DWord), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Rockstar Games\Launcher", "RGL", 2552918, RegistryValueKind.DWord), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Rockstar Games\Launcher\Add", "AUTO", 1, RegistryValueKind.DWord), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Rockstar Games\Launcher\Add", "BOOT", 0, RegistryValueKind.DWord), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Rockstar Games\Launcher\Add", "DEFDIR", 1, RegistryValueKind.DWord), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Rockstar Games\Launcher\Add", "DPI", 100, RegistryValueKind.DWord), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Rockstar Games\Launcher\Add", "INSTVER", rockstarGamesLauncherVersion, RegistryValueKind.String), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Rockstar Games\Launcher\Add", "LANG", "en-US", RegistryValueKind.String), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Rockstar Games\Launcher\Add", "REDIST", 0, RegistryValueKind.DWord), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Rockstar Games\Launcher\Add", "SHRT", 1, RegistryValueKind.DWord), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Rockstar Games\Launcher\Add", "SIL", 0, RegistryValueKind.DWord), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Rockstar Games\Launcher\Add", "UPVER", 0, RegistryValueKind.DWord), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Rockstar Games\Launcher\Add", "PARPRO", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "explorer.exe"), RegistryValueKind.String), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Rockstar Games\Launcher\Add", "INSTPATH", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Rockstar Games", "Launcher"), RegistryValueKind.String), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => await ProcessActions.RunPowerShell(@"$Shell = New-Object -ComObject WScript.Shell; New-Item -Path ([System.IO.Path]::Combine($env:APPDATA, 'Microsoft\Windows\Start Menu\Programs\Rockstar Games')) -ItemType Directory -Force | Out-Null; $Shortcut = $Shell.CreateShortcut([System.IO.Path]::Combine($env:APPDATA, 'Microsoft\Windows\Start Menu\Programs\Rockstar Games\Rockstar Games Launcher.lnk')); $Shortcut.TargetPath = [System.IO.Path]::Combine($env:ProgramFiles, 'Rockstar Games\Launcher\LauncherPatcher.exe'); $Shortcut.Save()"), () => RockstarGamesLauncher == true),
            ("Cleaning up Rockstar Games Launcher files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "Rockstar-Games-Launcher.exe")), () => RockstarGamesLauncher == true),
            ("Cleaning up Rockstar Games Launcher files", async () => Directory.Delete(Path.Combine(Path.GetTempPath(), "Rockstar-Games-Launcher"), true), () => RockstarGamesLauncher == true),

            // update rock star games launcher
            ("Updating Rockstar Games Launcher", async () => { await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Rockstar Games", "Launcher", "LauncherPatcher.exe") , WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(); while (Process.GetProcessesByName("dxdiag").Length > 1) await Task.Delay(500); while (Process.GetProcessesByName("SocialClubHelper").Length == 0) await Task.Delay(500); }, () => RockstarGamesLauncher == true),

            // log in to rockstar games launcher
            ("Please log in to your Rockstar Games Launcher account (Close to continue)", async () => { while (Process.GetProcessesByName("Launcher").Length == 1) await Task.Delay(500); }, () => RockstarGamesLauncher == true),
        
            // download fivem
            ("Downloading FiveM", async () => await DownloadHelper.Download("https://www.dl.dropboxusercontent.com/scl/fi/tn48g2m1qisdsir80ixu8/FiveM.zip?rlkey=c54qzh36fr3p8yb09q4zlt0gi&st=ca6wjcgx&dl=0", Path.GetTempPath(), "FiveM.zip", new InstallPageReporter()), () => FiveM == true),

            // install fivem
            ("Installing FiveM", async () => await ExtractHelper.Extract(Path.Combine(Path.GetTempPath(), "FiveM.zip"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FiveM")), () => FiveM == true),
            ("Installing FiveM", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Uninstall\CitizenFX_FiveM", "DisplayName", "FiveM", RegistryValueKind.String), () => FiveM == true),
            ("Installing FiveM", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Uninstall\CitizenFX_FiveM", "DisplayIcon", $@"{Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FiveM", "FiveM.exe")},0", RegistryValueKind.String), () => FiveM == true),
            ("Installing FiveM", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Uninstall\CitizenFX_FiveM", "HelpLink", "https://cfx.re/", RegistryValueKind.String), () => FiveM == true),
            ("Installing FiveM", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Uninstall\CitizenFX_FiveM", "InstallLocation", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FiveM"), RegistryValueKind.String), () => FiveM == true),
            ("Installing FiveM", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Uninstall\CitizenFX_FiveM", "Publisher", "Cfx.re", RegistryValueKind.String), () => FiveM == true),
            ("Installing FiveM", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Uninstall\CitizenFX_FiveM", "UninstallString", $@"""{Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FiveM", "FiveM.exe")}"" -uninstall app", RegistryValueKind.String), () => FiveM == true),
            ("Installing FiveM", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Uninstall\CitizenFX_FiveM", "URLInfoAbout", "https://cfx.re/", RegistryValueKind.String), () => FiveM == true),
            ("Installing FiveM", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Uninstall\CitizenFX_FiveM", "NoModify", 1, RegistryValueKind.DWord), () => FiveM == true),
            ("Installing FiveM", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Uninstall\CitizenFX_FiveM", "NoRepair", 1, RegistryValueKind.DWord), () => FiveM == true),
            ("Installing FiveM", async () => await ProcessActions.RunPowerShell(@"$s=New-Object -ComObject WScript.Shell; $p=[System.IO.Path]::Combine($env:APPDATA,'Microsoft\Windows\Start Menu\Programs'); $sc1=$s.CreateShortcut([System.IO.Path]::Combine($p,'FiveM.lnk')); $sc1.TargetPath=[System.IO.Path]::Combine($env:LOCALAPPDATA,'FiveM\FiveM.exe'); $sc1.Description='FiveM is a modification framework based on the Cfx.re platform'; $sc1.Save(); $sc2=$s.CreateShortcut([System.IO.Path]::Combine($p,'FiveM - Cfx.re Development Kit (FxDK).lnk')); $sc2.TargetPath=[System.IO.Path]::Combine($env:LOCALAPPDATA,'FiveM\FiveM - Cfx.re Development Kit (FxDK).lnk'); $sc2.Save()"), () => FiveM == true),
            ("Cleaning up FiveM files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "FiveM.zip")), () => FiveM == true),
        
            // download faceit
            ("Downloading FACEIT", async () => await DownloadHelper.Download("https://faceit-client.faceit-cdn.net/release/FACEIT-setup-latest.exe", Path.GetTempPath(), "FACEIT-setup-latest.exe", new InstallPageReporter()), () => FACEIT == true),

            // install faceit
            ("Installing FACEIT", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Path.GetTempPath(), "FACEIT-setup-latest.exe"), Arguments = "/S", WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), () => FACEIT == true),
            ("Cleaning up FACEIT files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "FACEIT-setup-latest.exe")), () => FACEIT == true),

            // log in to faceit
            ("Please log in to your FACEIT account (Close to continue)", async () => { while (Process.GetProcessesByName("FACEIT").Length > 1) await Task.Delay(500); }, () => FACEIT == true),

            // remove faceit desktop shortcut 
            ("Removing FACEIT desktop shortcut", async () => File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "FACEIT.lnk")), () => FACEIT == true),

            // disable faceit startup entry
            ("Disabling FACEIT startup entry", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run", "FACEIT", new byte[] { 0x01 }, RegistryValueKind.Binary), () => FACEIT == true),

            // download dolby access
            ("Downloading Dolby Access", async () => await StoreHelper.Download("DolbyLaboratories.DolbyAccess_rz1tebttyb220", 1, reporter: reporter), () => AppleMusic == true),

            // install dolby access
            ("Installing Dolby Access", async () => await StoreHelper.Install("DolbyLaboratories.DolbyAccess_rz1tebttyb220"), () => AppleMusic == true),
            ("Installing Dolby Access", async () => dolbyAccessVersion = StoreHelper.GetVersion("DolbyLaboratories.DolbyAccess_rz1tebttyb220"), () => AppleMusic == true),

            // log in to dolby access
            ("Please log in to your Dolby Access account (Close to continue)", async () => await Task.Delay(1000), () => AppleMusic == true),
            ("Please log in to your Dolby Access account (Close to continue)", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "WindowsApps", $"DolbyLaboratories.DolbyAccess_{dolbyAccessVersion}_x64__rz1tebttyb220", "DolbyAccess.exe"), WindowStyle = ProcessWindowStyle.Maximized }) !.WaitForExitAsync(), () => AppleMusic == true),

            // download apple music
            ("Downloading Apple Music", async () => await StoreHelper.Download("AppleInc.AppleMusicWin_nzyj5cx40ttqa", reporter: reporter), () => AppleMusic == true),

            // install apple music
            ("Installing Apple Music", async () => await StoreHelper.Install("AppleInc.AppleMusicWin_nzyj5cx40ttqa"), () => AppleMusic == true),
            ("Installing Apple Music", async () => appleMusicVersion = StoreHelper.GetVersion("AppleInc.AppleMusicWin_nzyj5cx40ttqa"), () => AppleMusic == true),
            
            // enable "keep miniplayer on top of all other windows"
            (@"Enabling ""Keep Miniplayer on top of all other windows""", async () => await ProcessActions.RunPowerShellScript("applemusic.ps1", ""), () => AppleMusic == true),

            // pin apple music to the taskbar
            ("Pinning Apple Music to the taskbar", async () => await ProcessActions.RunPowerShellScript("taskbarpin.ps1", @"-Type UWA -Path AppleInc.AppleMusicWin_nzyj5cx40ttqa!App"), () => AppleMusic == true),

            // log in to apple music
            ("Please log in to your Apple Music account (Close to continue)", async () => await Task.Delay(1000), () => AppleMusic == true),
            ("Please log in to your Apple Music account (Close to continue)", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "WindowsApps", $"AppleInc.AppleMusicWin_{appleMusicVersion}_x64__nzyj5cx40ttqa", "AppleMusic.exe"), WindowStyle = ProcessWindowStyle.Maximized }) !.WaitForExitAsync(), () => AppleMusic == true),

            // download tidal
            ("Downloading TIDAL", async () => await StoreHelper.Download("WiMPMusic.27241E05630EA_kn85bz84x7te4", reporter: reporter), () => Tidal == true),

            // install tidal
            ("Installing TIDAL", async () => await StoreHelper.Install("WiMPMusic.27241E05630EA_kn85bz84x7te4"), () => Tidal == true),
            ("Installing TIDAL", async () => tidalVersion = StoreHelper.GetVersion("WiMPMusic.27241E05630EA_kn85bz84x7te4"), () => Tidal == true),

            // pin tidal to the taskbar
            ("Pinning TIDAL to the taskbar", async () => await ProcessActions.RunPowerShellScript("taskbarpin.ps1", @"-Type UWA -Path WiMPMusic.27241E05630EA_kn85bz84x7te4!TIDAL"), () => Tidal == true),

            // log in to tidal
            ("Please log in to your TIDAL account (Close to continue)", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "WindowsApps", $"WiMPMusic.27241E05630EA_{tidalVersion}_x86__kn85bz84x7te4", "app", "TIDAL.exe"), WindowStyle = ProcessWindowStyle.Maximized }) !.WaitForExitAsync(), () => Tidal == true),

            // download qobuz
            ("Downloading Qobuz", async () => await DownloadHelper.Download("https://desktop.qobuz.com/releases/win32/x64/windows7_8_10/8.1.0-b019/Qobuz_Installer.exe", Path.GetTempPath(), "Qobuz_Installer.exe", reporter: reporter), () => Qobuz == true),

            // install qobuz
            ("Installing Qobuz", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Path.GetTempPath(), "Qobuz_Installer.exe"), Arguments = "-s" , WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), () => Qobuz == true),
            ("Cleaning up Qobuz files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "Qobuz_Installer.exe")), () => Qobuz == true),

            // pin qobuz to the taskbar
            ("Pinning Qobuz to the taskbar", async () => await ProcessActions.RunPowerShellScript("taskbarpin.ps1", $@"-Type Link -Path ""{Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Microsoft\Windows\Start Menu\Programs\Qobuz\Qobuz.lnk")}"""), () => Qobuz == true),

            // log in to qobuz
            ("Please log in to your Qobuz account (Close to continue)", async () => await Task.Delay(1000), () => Qobuz == true),
            ("Please log in to your Qobuz account (Close to continue)", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Qobuz", "Qobuz.exe"), WindowStyle = ProcessWindowStyle.Maximized })!.WaitForExitAsync(), () => Qobuz == true),
            ("Please log in to your Qobuz account (Close to continue)", async () => { while (Process.GetProcessesByName("Qobuz").Length != 0) await Task.Delay(500); }, () => Qobuz == true),

            // download amazon music
            ("Downloading Amazon Music", async () => await StoreHelper.Download("AmazonMobileLLC.AmazonMusic_kc6t79cpj4tp0", reporter: reporter), () => AmazonMusic == true),

            // install amazon music
            ("Installing Amazon Music", async () => await StoreHelper.Install("AmazonMobileLLC.AmazonMusic_kc6t79cpj4tp0"), () => AmazonMusic == true),
            ("Installing Amazon Music", async () => amazonMusicVersion = StoreHelper.GetVersion("AmazonMobileLLC.AmazonMusic_kc6t79cpj4tp0"), () => AmazonMusic == true),

            // pin amazon music to the taskbar
            ("Pinning Amazon Music to the taskbar", async () => await ProcessActions.RunPowerShellScript("taskbarpin.ps1", @"-Type UWA -Path AmazonMobileLLC.AmazonMusic_kc6t79cpj4tp0!AmazonMobileLLC.AmazonMusic"), () => AmazonMusic == true),

            // log in to amazon music
            ("Please log in to your Amazon Music account (Close to continue)", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "WindowsApps", $"AmazonMobileLLC.AmazonMusic_{amazonMusicVersion}_x86__kc6t79cpj4tp0", "Amazon Music.exe"), WindowStyle = ProcessWindowStyle.Maximized }) !.WaitForExitAsync(), () => AmazonMusic == true),

            // download deezer music
            ("Downloading Deezer Music", async () => await StoreHelper.Download("Deezer.62021768415AF_q7m17pa7q8kj0", reporter: reporter), () => DeezerMusic == true),

            // install deezer music
            ("Installing Deezer Music", async () => await StoreHelper.Install("Deezer.62021768415AF_q7m17pa7q8kj0"), () => DeezerMusic == true),
            ("Installing Deezer Music", async () => deezerMusicVersion = StoreHelper.GetVersion("Deezer.62021768415AF_q7m17pa7q8kj0"), () => DeezerMusic == true),

            // pin deezer music to the taskbar
            ("Pinning Deezer Music to the taskbar", async () => await ProcessActions.RunPowerShellScript("taskbarpin.ps1", @"-Type UWA -Path Deezer.62021768415AF_q7m17pa7q8kj0!Deezer.Music"), () => DeezerMusic == true),

            // log in to deezer music
            ("Please log in to your Deezer Music account (Close to continue)", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "WindowsApps", $"Deezer.62021768415AF_{deezerMusicVersion}_x86__q7m17pa7q8kj0", "app", "Deezer.exe"), WindowStyle = ProcessWindowStyle.Maximized }) !.WaitForExitAsync(), () => DeezerMusic == true),

            // download spotify
            ("Downloading Spotify", async () => await DownloadHelper.Download("https://download.scdn.co/SpotifyFullSetupX64.exe", Path.GetTempPath(), "SpotifyFullSetupX64.exe", reporter: reporter), () => Spotify == true),

            // install spotify
            ("Installing Spotify", async () => spotifyVersion = FileVersionInfo.GetVersionInfo(Path.Combine(Path.GetTempPath(), "SpotifyFullSetupX64.exe")).ProductVersion, () => Spotify == true),
            ("Installing Spotify", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Path.GetTempPath(), "SpotifyFullSetupX64.exe"), Arguments = @$"/extract ""{Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Spotify")}""" , WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), () => Spotify == true),
            ("Installing Spotify", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Spotify", "DisplayIcon", $@"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\Spotify\Spotify.exe", RegistryValueKind.String), () => Spotify == true),
            ("Installing Spotify", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Spotify", "DisplayName", "Spotify", RegistryValueKind.String), () => Spotify == true),
            ("Installing Spotify", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Spotify", "DisplayVersion", spotifyVersion, RegistryValueKind.String), () => Spotify == true),
            ("Installing Spotify", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Spotify", "InstallDate", DateTime.Now.ToString("yyyyMMdd"), RegistryValueKind.String), () => Spotify == true),
            ("Installing Spotify", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Spotify", "InstallLocation", $@"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\Spotify", RegistryValueKind.String), () => Spotify == true),
            ("Installing Spotify", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Spotify", "NoModify", 1, RegistryValueKind.DWord), () => Spotify == true),
            ("Installing Spotify", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Spotify", "NoRepair", 1, RegistryValueKind.DWord), () => Spotify == true),
            ("Installing Spotify", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Spotify", "Publisher", "Spotify AB", RegistryValueKind.String), () => Spotify == true),
            ("Installing Spotify", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Spotify", "QuietUninstallString", $@"""{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\Spotify\uninstall.exe"" /silent", RegistryValueKind.String), () => Spotify == true),
            ("Installing Spotify", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Spotify", "UninstallString", $@"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\Spotify\uninstall.exe", RegistryValueKind.String), () => Spotify == true),
            ("Installing Spotify", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Spotify", "URLInfoAbout", "https://www.spotify.com", RegistryValueKind.String), () => Spotify == true),
            ("Installing Spotify", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Spotify", "Version", spotifyVersion, RegistryValueKind.String), () => Spotify == true),
            ("Installing Spotify", async () => await ProcessActions.RunPowerShell(@"$Shell = New-Object -ComObject WScript.Shell; $Shortcut = $Shell.CreateShortcut([System.IO.Path]::Combine($env:APPDATA, 'Microsoft\Windows\Start Menu\Programs\Spotify.lnk')); $Shortcut.TargetPath = [System.IO.Path]::Combine($env:APPDATA, 'Spotify\Spotify.exe'); $Shortcut.Save()"), () => Spotify == true),
            ("Cleaning up Spotify files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "SpotifyFullSetupX64.exe")), () => Spotify == true),

            // pin spotify to the taskbar
            ("Pinning Spotify to the taskbar", async () => await ProcessActions.RunPowerShellScript("taskbarpin.ps1", $@"-Type Link -Path ""{Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Microsoft\Windows\Start Menu\Programs\Spotify.lnk")}"""), () => Spotify == true),

            // disable spotify hardware acceleration
            ("Disabling Spotify hardware acceleration", async () => await File.WriteAllTextAsync(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Spotify", "prefs"), "ui.hardware_acceleration=false"), () => Spotify == true),

            // download spotx
            ("Downloading SpotX", async () => await DownloadHelper.Download("https://raw.githubusercontent.com/SpotX-Official/SpotX/main/run.ps1", Path.GetTempPath(), "run.ps1", reporter: reporter), () => Spotify == true),

            // install spotx
            ("Installing SpotX", async () => await ProcessActions.RunPowerShell($@"& ""{Path.Combine(Path.GetTempPath(), "run.ps1")}"" -new_theme -adsections_off -podcasts_off -block_update_off -version {spotifyVersion}-1234"), () => Spotify == true),

            // log in to spotify
            ("Please log in to your Spotify account (Close to continue)", async () => await Task.Delay(1000), () => Spotify == true),
            ("Please log in to your Spotify account (Close to continue)", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Spotify", "Spotify.exe"), WindowStyle = ProcessWindowStyle.Maximized }) !.WaitForExitAsync(), () => Spotify == true),
            
            // remove spotify desktop shortcut
            ("Removing Spotify desktop shortcut", async () => File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Spotify.lnk")), () => Spotify == true),

            // disable spotify startup entry
            ("Disabling Spotify startup entry", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run", "Spotify", new byte[] { 0x01 }, RegistryValueKind.Binary), () => Spotify == true),

            // download logitech g hub
            ("Downloading Logitech G HUB", async () => await DownloadHelper.Download("https://download01.logi.com/web/ftp/pub/techsupport/gaming/lghub_installer.exe", Path.GetTempPath(), "lghub_installer.exe", reporter: reporter), () => LogitechGHub == true),

            // install logitech g hub
            ("Installing Logitech G HUB", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Path.GetTempPath(), "lghub_installer.exe"), Arguments = "--silent" , WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), () => LogitechGHub == true),
			("Installing Logitech G HUB", async () => { foreach (Process process in new[] { "lghub", "lghub_system_tray", "lghub_agent" }.SelectMany(Process.GetProcessesByName)) { process.Kill(); process.WaitForExit(); }}, () => LogitechGHub == true),
			("Cleaning up Logitech G HUB files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "lghub_installer.exe")), () => LogitechGHub == true),

			// disable logitech g hub services
			("Disabling Logitech G HUB services", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\LGHUBUpdaterService", "Start", 3, RegistryValueKind.DWord), () => LogitechGHub == true),
			("Disabling Logitech G HUB services", async () => ServicesHelper.StopService("LGHUBUpdaterService"), () => LogitechGHub == true),
            ("Disabling Logitech G HUB services", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\logi_lamparray_service", "Start", 3, RegistryValueKind.DWord), () => LogitechGHub == true),
			("Disabling Logitech G HUB services", async () => ServicesHelper.StopService("logi_lamparray_service"), () => LogitechGHub == true),

            // remove logitech g hub desktop shortcut
            ("Removing Logitech G HUB desktop shortcut", async () => File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory), "Logitech G HUB.lnk")), () => LogitechGHub == true),

            // disable logitech g hub startup entry
            ("Disabling Logitech G HUB startup entry", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run", "LGHUB", new byte[] { 0x01 }, RegistryValueKind.Binary), () => LogitechGHub == true),

            // download logitech onboard memory manager
            ("Downloading Logitech Onboard Memory Manager", async () => await DownloadHelper.Download("https://download01.logi.com/web/ftp/pub/techsupport/gaming/OnboardMemoryManager_2.6.1749.exe", Path.GetTempPath(), "OnboardMemoryManager.exe", reporter: reporter), () => LogitechOnboardMemoryManager == true),

            // install logitech onboard memory manager
            ("Installing Logitech Onboard Memory Manager", async () => Directory.CreateDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Logitech Onboard Memory Manager")), () => LogitechOnboardMemoryManager == true),
            ("Installing Logitech Onboard Memory Manager", async () => File.Move(Path.Combine(Path.GetTempPath(), "OnboardMemoryManager.exe"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Logitech Onboard Memory Manager", "OnboardMemoryManager.exe"), true), () => LogitechOnboardMemoryManager == true),
            ("Installing Logitech Onboard Memory Manager", async () => await ProcessActions.RunPowerShell(@"$Shell = New-Object -ComObject WScript.Shell; $Shortcut = $Shell.CreateShortcut([System.IO.Path]::Combine($env:ProgramData, 'Microsoft\Windows\Start Menu\Programs\Logitech Onboard Memory Manager.lnk')); $Shortcut.TargetPath = [System.IO.Path]::Combine($env:ProgramFiles, 'Logitech Onboard Memory Manager', 'OnboardMemoryManager.exe'); $Shortcut.Save()"), () => LogitechOnboardMemoryManager == true),
            ("Installing Logitech Onboard Memory Manager", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Logitech Onboard Memory Manager", "DisplayName", "Logitech Onboard Memory Manager", RegistryValueKind.String), () => LogitechOnboardMemoryManager == true),
            ("Installing Logitech Onboard Memory Manager", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Logitech Onboard Memory Manager", "UninstallString", $@"cmd /c rd /s /q ""{Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Logitech Onboard Memory Manager")}"" & del ""{Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), @"Microsoft\Windows\Start Menu\Programs\Logitech Onboard Memory Manager.lnk")}"" & reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Logitech Onboard Memory Manager"" /f", RegistryValueKind.String), () => LogitechOnboardMemoryManager == true),
            ("Installing Logitech Onboard Memory Manager", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Logitech Onboard Memory Manager", "DisplayIcon", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Logitech Onboard Memory Manager", "OnboardMemoryManager.exe"), RegistryValueKind.String), () => LogitechOnboardMemoryManager == true),
            ("Installing Logitech Onboard Memory Manager", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Logitech Onboard Memory Manager", "Publisher", "Logitech", RegistryValueKind.String), () => LogitechOnboardMemoryManager == true),
            ("Installing Logitech Onboard Memory Manager", async () => await Task.Delay(500), () => LogitechOnboardMemoryManager == true),

            // download wootility
            ("Downloading Wootility", async () => await DownloadHelper.Download("https://api.wooting.io/public/wootility/download?os=win&version=5.3.1", Path.GetTempPath(), "WootilitySetup.exe", reporter: reporter), () => Wootility == true),

            // install wootility
            ("Installing Wootility", async () => await ExtractHelper.Extract(Path.Combine(Path.GetTempPath(), "WootilitySetup.exe"), Path.Combine(Path.GetTempPath(), "WootilitySetup")), () => Wootility == true),
            ("Installing Wootility", async () => await ExtractHelper.Extract(Path.Combine(Path.GetTempPath(), "WootilitySetup", "$PLUGINSDIR", "app-64.7z"), Path.Combine(Path.GetTempPath(), "WootilitySetup", "app-64")), () => Wootility == true),
            ("Installing Wootility", async () => Directory.CreateDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs")), () => Wootility == true),
            ("Installing Wootility", async () => Directory.Move(Path.Combine(Path.GetTempPath(), "WootilitySetup", "app-64"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "wootility")), () => Wootility == true),
            ("Installing Wootility", async () => File.Move(Path.Combine(Path.GetTempPath(), "WootilitySetup", "$R0", "Uninstall Wootility.exe"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "wootility", "Uninstall Wootility.exe"), true), () => Wootility == true),
            ("Installing Wootility", async () => File.Move(Path.Combine(Path.GetTempPath(), "WootilitySetup", "$R0", "wooting_analog_sdk.msi"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "wootility", "wooting_analog_sdk.msi"), true), () => Wootility == true),
            ("Installing Wootility", async () => await Process.Start(new ProcessStartInfo { FileName = "msiexec.exe", Arguments = $@"/i ""{Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "wootility", "wooting_analog_sdk.msi")}"" /qn" , WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), () => Wootility == true),
            ("Installing Wootility", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\e016c86c-37b5-5b46-9e05-2c20cb392812", "DisplayName", "Wootility 5.3.1", RegistryValueKind.String), () => Wootility == true),
            ("Installing Wootility", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\e016c86c-37b5-5b46-9e05-2c20cb392812", "UninstallString", $@"""{Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "wootility", "Uninstall Wootility.exe")}"" /currentuser", RegistryValueKind.String), () => Wootility == true),
            ("Installing Wootility", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\e016c86c-37b5-5b46-9e05-2c20cb392812", "QuietUninstallString", $@"""{Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "wootility", "Uninstall Wootility.exe")}"" /currentuser /S", RegistryValueKind.String), () => Wootility == true),
            ("Installing Wootility", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\e016c86c-37b5-5b46-9e05-2c20cb392812", "DisplayVersion", "5.3.1", RegistryValueKind.String), () => Wootility == true),
            ("Installing Wootility", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\e016c86c-37b5-5b46-9e05-2c20cb392812", "DisplayIcon", $@"{Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "wootility", "Wootility.exe")},0", RegistryValueKind.String), () => Wootility == true),
            ("Installing Wootility", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\e016c86c-37b5-5b46-9e05-2c20cb392812", "Publisher", "Wooting", RegistryValueKind.String), () => Wootility == true),
            ("Installing Wootility", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\e016c86c-37b5-5b46-9e05-2c20cb392812", "Comments", "Instantly edit your Wooting keyboard profiles and RGB.", RegistryValueKind.String), () => Wootility == true),
            ("Installing Wootility", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\e016c86c-37b5-5b46-9e05-2c20cb392812", "HelpLink", "https://wooting.io/wootility", RegistryValueKind.String), () => Wootility == true),
            ("Installing Wootility", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\e016c86c-37b5-5b46-9e05-2c20cb392812", "URLInfoAbout", "https://wooting.io/wootility", RegistryValueKind.String), () => Wootility == true),
            ("Installing Wootility", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\e016c86c-37b5-5b46-9e05-2c20cb392812", "URLUpdateInfo", "https://wooting.io/wootility", RegistryValueKind.String), () => Wootility == true),
            ("Installing Wootility", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\e016c86c-37b5-5b46-9e05-2c20cb392812", "Readme", "https://wooting.io/wootility", RegistryValueKind.String), () => Wootility == true),
            ("Installing Wootility", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\e016c86c-37b5-5b46-9e05-2c20cb392812", "NoModify", 1, RegistryValueKind.DWord), () => Wootility == true),
            ("Installing Wootility", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\e016c86c-37b5-5b46-9e05-2c20cb392812", "NoRepair", 1, RegistryValueKind.DWord), () => Wootility == true),
            ("Installing Wootility", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\e016c86c-37b5-5b46-9e05-2c20cb392812", "EstimatedSize", 0x000613d0, RegistryValueKind.DWord), () => Wootility == true),
            ("Installing Wootility", async () => await ProcessActions.RunPowerShell(@"$Shell = New-Object -ComObject WScript.Shell; $Shortcut = $Shell.CreateShortcut([System.IO.Path]::Combine($env:APPDATA, 'Microsoft\Windows\Start Menu\Programs\Wootility.lnk')); $Shortcut.TargetPath = [System.IO.Path]::Combine($env:LOCALAPPDATA, 'Programs\wootility\Wootility.exe'); $Shortcut.Save()"), () => Wootility == true),
            ("Cleaning up Wootility files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "WootilitySetup.exe")), () => Wootility == true),
            ("Cleaning up Wootility files", async () => Directory.Delete(Path.Combine(Path.GetTempPath(), "WootilitySetup"), true), () => Wootility == true),

            // download steelseries gg
            ("Downloading SteelSeries GG", async () => await DownloadHelper.Download("https://steelseries.com/gg/downloads/latest/windows", Path.GetTempPath(), "SteelSeriesGGSetup.exe", reporter: reporter), () => SteelSeriesGG == true),

            // install steelseries gg
            ("Installing SteelSeries GG", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Path.GetTempPath(), "SteelSeriesGGSetup.exe"), Arguments = "/S" , WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), () => SteelSeriesGG == true),
            ("Cleaning up SteelSeries GG files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "SteelSeriesGGSetup.exe")), () => SteelSeriesGG == true),

			// disable steelseries gg service
			("Disabling SteelSeries GG service", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\SteelSeriesGGUpdateServiceProxy", "Start", 4, RegistryValueKind.DWord), () => SteelSeriesGG == true),
			("Disabling SteelSeries GG service", async () => ServicesHelper.StopService("SteelSeriesGGUpdateServiceProxy"), () => SteelSeriesGG == true),

            // disable steelseries gg startup entry
            ("Disabling SteelSeries GG startup entry", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run", "SteelSeriesGG", new byte[] { 0x01 }, RegistryValueKind.Binary), () => SteelSeriesGG == true),

            // download razer synapse
            ("Downloading Razer Synapse", async () => await DownloadHelper.Download("https://manifest-assets.razersynapse.com/1773727105qjG1koNDRazerAppEngineSetup-v4.0.662.exe", Path.GetTempPath(), "RazerAppEngineSetup.exe", reporter: reporter), () => RazerSynapse == true),
            ("Downloading Razer Synapse", async () => await DownloadHelper.Download("https://manifest-assets.razersynapse.com/1773727025T17d7NVXRazerSynapse4-Web-v4.0.662.exe", Path.GetTempPath(), "RazerSynapse4-Web.exe", reporter: reporter), () => RazerSynapse == true),
            ("Downloading Razer Synapse", async () => await DownloadHelper.Download("https://www.dl.dropboxusercontent.com/scl/fi/ztob6pr106j39eqfpeyjs/Razer.zip?rlkey=mc5ry2nzyb7t2emt5nx53db9o&st=v4lkkua1&dl=0", Path.GetTempPath(), "Razer.zip", reporter: reporter), () => RazerSynapse == true),
            ("Downloading Razer Synapse", async () => await DownloadHelper.Download("https://www.dl.dropboxusercontent.com/scl/fi/il4e4pi2fkb2lb4pex2qq/leveldb.zip?rlkey=ub50jftdqjl1zau2qb58kiz2f&st=vcy6td1a&dl=0", Path.GetTempPath(), "leveldb.zip", reporter: reporter), () => RazerSynapse == true),

            // install razer synapse
            ("Installing Razer Synapse", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Path.GetTempPath(), "RazerAppEngineSetup.exe"), Arguments = "/S" , WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), () => RazerSynapse == true),
            ("Installing Razer Synapse", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Path.GetTempPath(), "RazerSynapse4-Web.exe"), Arguments = "/S" , WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), () => RazerSynapse == true),
            ("Installing Razer Synapse", async () => await ExtractHelper.Extract(Path.Combine(Path.GetTempPath(), "Razer.zip"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Razer")), () => RazerSynapse == true),
            ("Installing Razer Synapse", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), @"Razer\RazerAppEngine\RazerAppEngine.exe"), Arguments = "--url-params=apps=synapse" , WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), () => RazerSynapse == true),
            ("Installing Razer Synapse", async () => { while (Process.GetProcessesByName("GameManagerService3").Length == 0) await Task.Delay(500); }, () => RazerSynapse == true),
            ("Installing Razer Synapse", async () => { foreach (Process process in Process.GetProcessesByName("RazerAppEngine")) { process.Kill(); process.WaitForExit(); }}, () => RazerSynapse == true),
            ("Installing Razer Synapse", async () => await ExtractHelper.Extract(Path.Combine(Path.GetTempPath(), "leveldb.zip"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Razer\RazerAppEngine\User Data\Default\Local Storage")), () => RazerSynapse == true),
            ("Cleaning up Razer Synapse files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "RazerAppEngineSetup.exe")), () => RazerSynapse == true),
            ("Cleaning up Razer Synapse files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "RazerSynapse4-Web.exe")), () => RazerSynapse == true),
            ("Cleaning up Razer Synapse files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "Razer.zip")), () => RazerSynapse == true),
            ("Cleaning up Razer Synapse files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "leveldb.zip")), () => RazerSynapse == true),

			// disable razer synapse services
            ("Disabling Razer Synapse services", async () => File.Move(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), @"Razer\Razer Services\GMS3\GameManagerService3.exe"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), @"Razer\Razer Services\GMS3\GameManagerService3.exe.bak")), () => RazerSynapse == true),
            ("Disabling Razer Synapse services", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\Razer Game Manager Service 3", "Start", 4, RegistryValueKind.DWord), () => RazerSynapse == true),
			("Disabling Razer Synapse services", async () => ServicesHelper.StopService("Razer Game Manager Service 3"), () => RazerSynapse == true),
            ("Disabling Razer Synapse services", async () => File.Move(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), @"Razer\razer_elevation_service\razer_elevation_service.exe"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), @"Razer\razer_elevation_service\razer_elevation_service.exe.bak")), () => RazerSynapse == true),
			("Disabling Razer Synapse services", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\Razer Elevation Service", "Start", 4, RegistryValueKind.DWord), () => RazerSynapse == true),
			("Disabling Razer Synapse services", async () => ServicesHelper.StopService("Razer Elevation Service"), () => RazerSynapse == true),

			// disable razer synapse startup entry
            ("Disabling Razer Synapse startup entry", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run", "RazerAppEngine", new byte[] { 0x01 }, RegistryValueKind.Binary), () => RazerSynapse == true),

            // download corsair icue
            ("Downloading Corsair iCUE", async () => await DownloadHelper.Download("https://www3.corsair.com/software/CUE_V5/public/modules/windows/installer/Install%20iCUE.exe", Path.GetTempPath(), "Install iCUE.exe", reporter: reporter), () => CorsairICue == true),

            // install corsair icue
            ("Installing Corsair iCUE", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Path.GetTempPath(), "Install iCUE.exe"), Arguments = "--quiet" , WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), () => CorsairICue == true),
            ("Installing Corsair iCUE", async () => { while (Process.GetProcessesByName("icue-installer").Length >= 1) await Task.Delay(500); }, () => CorsairICue == true),
            ("Cleaning up Corsair iCUE files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "Install iCUE.exe")), () => CorsairICue == true),

			// disable corsair icue services
			("Disabling Corsair iCUE services", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\CorsairService", "Start", 4, RegistryValueKind.DWord), () => CorsairICue == true),
			("Disabling Corsair iCUE services", async () => ServicesHelper.StopService("CorsairService"), () => CorsairICue == true),

			// disable corsair icue startup entry
            ("Disabling Corsair iCUE startup entry", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run", "Corsair iCUE5 Software", new byte[] { 0x01 }, RegistryValueKind.Binary), () => CorsairICue == true),

            // download visual studio
            ("Downloading Visual Studio", async () => await DownloadHelper.Download("https://aka.ms/vs/stable/vs_community.exe", Path.GetTempPath(), "vs_Community.exe", reporter: reporter), () => VisualStudio == true),

            // install visual studio
            ("Installing Visual Studio", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Path.GetTempPath(), "vs_Community.exe"), Arguments = "--quiet --wait" , WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), () => VisualStudio == true),
            ("Cleaning up Visual Studio files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "vs_Community.exe")), () => VisualStudio == true),

            // optimize visual studio
            ("Optimizing Visual Studio", async () => { while (Process.GetProcessesByName("VSNgenRunner").Length == 1) await Task.Delay(500); }, () => VisualStudio == true),

            // pin visual studio to the taskbar
            ("Pinning Visual Studio to the taskbar", async () => await ProcessActions.RunPowerShellScript("taskbarpin.ps1", $@"-Type Link -Path ""{Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Microsoft", "Windows", "Start Menu", "Programs", "Visual Studio.lnk")}"""), () => VisualStudio == true),

            // download mica visual studio
            ("Downloading Mica Visual Studio", async () => await DownloadHelper.Download("https://github.com/Tech5G5G/Mica-Visual-Studio/releases/latest/download/MicaVisualStudio.vsix", Path.GetTempPath(), "MicaVisualStudio.vsix", reporter: reporter), () => VisualStudio == true),

            // install mica visual studio
            ("Installing Mica Visual Studio", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Microsoft Visual Studio", "Installer", "resources", "app", "ServiceHub", "Services", "Microsoft.VisualStudio.Setup.Service", "VSIXInstaller.exe"), Arguments = $"/quiet /admin {Path.Combine(Path.GetTempPath(), "MicaVisualStudio.vsix")}" , WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), () => VisualStudio == true),
            ("Cleaning up Mica Visual Studio files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "MicaVisualStudio.vsix")), () => VisualStudio == true),

            // download xaml styler
            ("Downloading XAML Styler", async () => await DownloadHelper.Download("https://marketplace.visualstudio.com/_apis/public/gallery/publishers/TeamXavalon/vsextensions/XAMLStyler2022/3.2501.8/vspackage", Path.GetTempPath(), "XamlStyler.Extension.Windows.VS2022.vsix", reporter: reporter), () => VisualStudio == true),

            // install xaml styler
            ("Installing XAML Styler", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Microsoft Visual Studio", "Installer", "resources", "app", "ServiceHub", "Services", "Microsoft.VisualStudio.Setup.Service", "VSIXInstaller.exe"), Arguments = $"/quiet /admin {Path.Combine(Path.GetTempPath(), "XamlStyler.Extension.Windows.VS2022.vsix")}" , WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), () => VisualStudio == true),
            ("Cleaning up XAML Styler files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "XamlStyler.Extension.Windows.VS2022.vsix")), () => VisualStudio == true),

            // download visual studio code
            ("Downloading Visual Studio Code", async () => await DownloadHelper.Download("https://code.visualstudio.com/sha/download?build=stable&os=win32-x64-user", Path.GetTempPath(), "VSCodeUserSetup-x64.exe", reporter: reporter), () => VisualStudioCode == true),

            // install visual studio code
            ("Installing Visual Studio Code", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Path.GetTempPath(), "VSCodeUserSetup-x64.exe"), Arguments = "/VERYSILENT /NORESTART /MERGETASKS=!runcode" , WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), () => VisualStudioCode == true),
            ("Cleaning up Visual Studio Code files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "VSCodeUserSetup-x64.exe")), () => VisualStudioCode ==  true),

            // pin visual studio code to the taskbar
            ("Pinning Visual Studio Code to the taskbar", async () => await ProcessActions.RunPowerShellScript("taskbarpin.ps1", $@"-Type Link -Path ""{Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Microsoft\Windows\Start Menu\Programs\Visual Studio Code\Visual Studio Code.lnk")}"""), () => VisualStudioCode == true),

            // download antigravity
            ("Downloading Antigravity", async () => await DownloadHelper.Download("https://edgedl.me.gvt1.com/edgedl/release2/j0qc3/antigravity/stable/1.23.2-4781536860569600/windows-x64/Antigravity.exe", Path.GetTempPath(), "Antigravity.exe", reporter: reporter), () => Antigravity == true),

            // install antigravity
            ("Installing Antigravity", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Path.GetTempPath(), "Antigravity.exe"), Arguments = "/VERYSILENT /NORESTART /MERGETASKS=!runcode" , WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), () => Antigravity == true),
            ("Cleaning up Antigravity files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "Antigravity.exe")), () => Antigravity == true),

            // pin antigravity to the taskbar
            ("Pinning Antigravity to the taskbar", async () => await ProcessActions.RunPowerShellScript("taskbarpin.ps1", $@"-Type Link -Path ""{Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Microsoft\Windows\Start Menu\Programs\Antigravity\Antigravity.lnk")}"""), () => Antigravity == true),

            // download git
            ("Downloading Git", async () => await DownloadHelper.Download(JsonDocument.Parse(await new HttpClient { DefaultRequestHeaders = { { "User-Agent", "AutoOS" } } }.GetStringAsync("https://api.github.com/repos/git-for-windows/git/releases")).RootElement.EnumerateArray().First(release => release.GetProperty("assets").EnumerateArray().Any(asset => asset.GetProperty("name").GetString().Contains("64-bit.exe"))).GetProperty("assets").EnumerateArray().First(asset => asset.GetProperty("name").GetString().Contains("64-bit.exe")).GetProperty("browser_download_url").GetString(), Path.GetTempPath(), "Git64-bit.exe", reporter: reporter), () => Git == true),

            // install git
            ("Installing Git", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Path.GetTempPath(), "Git64-bit.exe"), Arguments = "/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /NOICONS /COMPONENTS=GitLFS,GitGUI,GitCore" , WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), () => Git ==  true),
            ("Cleaning up Git files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "Git64-bit.exe")), () => Git ==  true),

            // download python
            ("Downloading Python", async () => await DownloadHelper.Download("https://www.python.org/ftp/python/3.14.5/python-3.14.5-amd64.exe", Path.GetTempPath(), "python-amd64.exe", reporter: reporter), () => Python == true),

            // install python
            ("Installing Python", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Path.GetTempPath(), "python-amd64.exe"), Arguments = "/quiet InstallAllUsers=1 PrependPath=1" , WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), () => Python == true),
            ("Cleaning up Python files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "python-amd64.exe")), () => Python == true),

            // download nodejs
            ("Downloading Node.js", async () => await DownloadHelper.Download("https://nodejs.org/dist/v24.12.0/node-v24.12.0-x64.msi", Path.GetTempPath(), "node-v24.12.0-x64.msi", reporter: reporter), () => Nodejs == true),

            // install nodejs
            ("Installing Node.js", async () => await Process.Start(new ProcessStartInfo { FileName = "msiexec.exe", Arguments = $@"/i ""{Path.Combine(Path.GetTempPath(), "node-v24.12.0-x64.msi")}"" /qn" , WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), () => Nodejs ==  true),
            ("Cleaning up Node.js files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "node-v24.12.0-x64.msi")), () => Nodejs ==  true),

            // download trello
            ("Downloading Trello", async () => await StoreHelper.Download("45273LiamForsyth.PawsforTrello_7pb5ddty8z1pa", reporter: reporter), () => Trello == true),

            // install trello
            ("Installing Trello", async () => await StoreHelper.Install("45273LiamForsyth.PawsforTrello_7pb5ddty8z1pa"), () => Trello == true),
            ("Installing Trello", async () => trelloVersion = StoreHelper.GetVersion("45273LiamForsyth.PawsforTrello_7pb5ddty8z1pa"), () => Trello == true),

            // pin trello to the taskbar
            ("Pinning Trello to the taskbar", async () => await ProcessActions.RunPowerShellScript("taskbarpin.ps1", @"-Type UWA -Path 45273LiamForsyth.PawsforTrello_7pb5ddty8z1pa!trello"), () => Trello == true),

            // log in to trello
            ("Please log in to your Trello account (Close to continue)", async () => await Task.Delay(1000), () => Trello == true),
            ("Please log in to your Trello account (Close to continue)", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "WindowsApps", $"45273LiamForsyth.PawsforTrello_{trelloVersion}_x64__7pb5ddty8z1pa", "app", "Trello.exe"), WindowStyle = ProcessWindowStyle.Maximized }) !.WaitForExitAsync(), () => Trello == true),
        
            // download office
            ("Downloading Office", async () => await DownloadHelper.Download("https://officecdn.microsoft.com/pr/wsus/setup.exe", Path.GetTempPath(), "setup.exe", reporter: reporter), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),

            // install office
            ("Installing Office", async () => File.Copy(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Scripts", "configuration.xml"), Path.Combine(Path.GetTempPath(), "configuration.xml"), true), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
            ("Installing Office", async () => { var doc = XDocument.Load(Path.Combine(Path.GetTempPath(), "configuration.xml")); doc.Root.Descendants("ExcludeApp").Where(x => (string)x.Attribute("ID") == "Word").Remove(); doc.Save(Path.Combine(Path.GetTempPath(), "configuration.xml")); }, () => Word == true),
            ("Installing Office", async () => { var doc = XDocument.Load(Path.Combine(Path.GetTempPath(), "configuration.xml")); doc.Root.Descendants("ExcludeApp").Where(x => (string)x.Attribute("ID") == "Excel").Remove(); doc.Save(Path.Combine(Path.GetTempPath(), "configuration.xml")); }, () => Excel == true),
            ("Installing Office", async () => { var doc = XDocument.Load(Path.Combine(Path.GetTempPath(), "configuration.xml")); doc.Root.Descendants("ExcludeApp").Where(x => (string)x.Attribute("ID") == "PowerPoint").Remove(); doc.Save(Path.Combine(Path.GetTempPath(), "configuration.xml")); }, () => PowerPoint == true),
            ("Installing Office", async () => { var doc = XDocument.Load(Path.Combine(Path.GetTempPath(), "configuration.xml")); doc.Root.Descendants("ExcludeApp").Where(x => (string)x.Attribute("ID") == "OneNote").Remove(); doc.Save(Path.Combine(Path.GetTempPath(), "configuration.xml")); }, () => OneNote == true),
            ("Installing Office", async () => { var doc = XDocument.Load(Path.Combine(Path.GetTempPath(), "configuration.xml")); doc.Root.Descendants("ExcludeApp").Where(x => (string)x.Attribute("ID") == "Teams").Remove(); doc.Save(Path.Combine(Path.GetTempPath(), "configuration.xml")); }, () => Teams == true),
            ("Installing Office", async () => { var doc = XDocument.Load(Path.Combine(Path.GetTempPath(), "configuration.xml")); doc.Root.Descendants("ExcludeApp").Where(x => (string)x.Attribute("ID") == "OutlookForWindows").Remove(); doc.Save(Path.Combine(Path.GetTempPath(), "configuration.xml")); }, () => Outlook == true),
            ("Installing Office", async () => { var doc = XDocument.Load(Path.Combine(Path.GetTempPath(), "configuration.xml")); doc.Root.Descendants("ExcludeApp").Where(x => (string)x.Attribute("ID") == "OneDrive").Remove(); doc.Save(Path.Combine(Path.GetTempPath(), "configuration.xml")); }, () => OneDrive == true),
            ("Installing Office", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Path.GetTempPath(), "setup.exe"), Arguments = $@"/configure ""{Path.Combine(Path.GetTempPath(), "configuration.xml")}""" , WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
            ("Cleaning up Office files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "setup.exe")), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
            ("Cleaning up Office files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "configuration.xml")), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
			
            // disable office startup entries
            ("Disabling Office startup entries", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_CLASSES_ROOT\PROTOCOLS\Filter\AutorunsDisabled\text/xml\CLSID", "", "{807583E5-5146-11D5-A672-00B0D022E945}", RegistryValueKind.String), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
            ("Disabling Office startup entries", async () => RegistryHelper.DeleteKey(RegistryHelper.Identity.TrustedInstaller, @"HKEY_CLASSES_ROOT\PROTOCOLS\Filter\text/xml"), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
            ("Disabling Office startup entries", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_CLASSES_ROOT\PROTOCOLS\Handler\AutorunsDisabled\mso-minsb-roaming.16\CLSID", "", "{83C25742-A9F7-49FB-9138-434302C88D07}", RegistryValueKind.String), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
            ("Disabling Office startup entries", async () => RegistryHelper.DeleteKey(RegistryHelper.Identity.TrustedInstaller, @"HKEY_CLASSES_ROOT\PROTOCOLS\Handler\mso-minsb-roaming.16"), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
            ("Disabling Office startup entries", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_CLASSES_ROOT\PROTOCOLS\Handler\AutorunsDisabled\mso-minsb.16\CLSID", "", "{42089D2D-912D-4018-9087-2B87803E93FB}", RegistryValueKind.String), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
            ("Disabling Office startup entries", async () => RegistryHelper.DeleteKey(RegistryHelper.Identity.TrustedInstaller, @"HKEY_CLASSES_ROOT\PROTOCOLS\Handler\AutorunsDisabled\mso-minsb.16"), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
            ("Disabling Office startup entries", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_CLASSES_ROOT\PROTOCOLS\Handler\AutorunsDisabled\osf-roaming.16\CLSID", "", "{42089D2D-912D-4018-9087-2B87803E93FB}", RegistryValueKind.String), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
            ("Disabling Office startup entries", async () => RegistryHelper.DeleteKey(RegistryHelper.Identity.TrustedInstaller, @"HKEY_CLASSES_ROOT\PROTOCOLS\Handler\osf-roaming.16"), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
            ("Disabling Office startup entries", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_CLASSES_ROOT\PROTOCOLS\Handler\AutorunsDisabled\osf.16\CLSID", "", "{5504BE45-A83B-4808-900A-3A5C36E7F77A}", RegistryValueKind.String), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
            ("Disabling Office startup entries", async () => RegistryHelper.DeleteKey(RegistryHelper.Identity.TrustedInstaller, @"HKEY_CLASSES_ROOT\PROTOCOLS\Handler\osf.16"), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
            ("Disabling Office startup entries", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Explorer\Browser Helper Objects\AutorunsDisabled\{31D09BA0-12F5-4CCE-BE8A-2923E76605DA}", "", "Lync Click to Call BHO", RegistryValueKind.String), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
            ("Disabling Office startup entries", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Explorer\Browser Helper Objects\AutorunsDisabled\{31D09BA0-12F5-4CCE-BE8A-2923E76605DA}", "NoExplorer", "1", RegistryValueKind.String), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
            ("Disabling Office startup entries", async () => RegistryHelper.DeleteKey(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Explorer\Browser Helper Objects\{31D09BA0-12F5-4CCE-BE8A-2923E76605DA}"), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
            ("Disabling Office startup entries", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Internet Explorer\Extensions\AutorunsDisabled\{31D09BA0-12F5-4CCE-BE8A-2923E76605DA}", "", "Lync Click to Call", RegistryValueKind.String), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
            ("Disabling Office startup entries", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Internet Explorer\Extensions\AutorunsDisabled\{31D09BA0-12F5-4CCE-BE8A-2923E76605DA}", "MenuText", "Lync Click to Call", RegistryValueKind.String), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
            ("Disabling Office startup entries", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Internet Explorer\Extensions\AutorunsDisabled\{31D09BA0-12F5-4CCE-BE8A-2923E76605DA}", "Icon", $@"{Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Microsoft Office", "root", "VFS", "ProgramFilesX86", "Microsoft Office", "Office16", "lync.exe")},1", RegistryValueKind.String), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
            ("Disabling Office startup entries", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Internet Explorer\Extensions\AutorunsDisabled\{31D09BA0-12F5-4CCE-BE8A-2923E76605DA}", "HotIcon", $@"{Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Microsoft Office", "root", "VFS", "ProgramFilesX86", "Microsoft Office", "Office16", "lync.exe")},1", RegistryValueKind.String), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
            ("Disabling Office startup entries", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Internet Explorer\Extensions\AutorunsDisabled\{31D09BA0-12F5-4CCE-BE8A-2923E76605DA}", "CLSID", "{1FBA04EE-3024-11d2-8F1F-0000F87ABD16}", RegistryValueKind.String), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
            ("Disabling Office startup entries", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Internet Explorer\Extensions\AutorunsDisabled\{31D09BA0-12F5-4CCE-BE8A-2923E76605DA}", "ClsidExtension", "{31D09BA0-12F5-4CCE-BE8A-2923E76605DA}", RegistryValueKind.String), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
            ("Disabling Office startup entries", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Internet Explorer\Extensions\AutorunsDisabled\{31D09BA0-12F5-4CCE-BE8A-2923E76605DA}", "Default Visible", "Yes", RegistryValueKind.String), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
            ("Disabling Office startup entries", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Internet Explorer\Extensions\AutorunsDisabled\{31D09BA0-12F5-4CCE-BE8A-2923E76605DA}", "ButtonText", "Lync Click to Call", RegistryValueKind.String), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
            ("Disabling Office startup entries", async () => RegistryHelper.DeleteKey(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Internet Explorer\Extensions\{31D09BA0-12F5-4CCE-BE8A-2923E76605DA}"), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
            ("Disabling Office startup entries", async () => TaskSchedulerHelper.Toggle(@"\Microsoft\Office\Office Actions Server", false), () => Word || Excel || PowerPoint || OneNote || Teams || Outlook || OneDrive),
            ("Disabling Office startup entries", async () => TaskSchedulerHelper.Toggle(@"\Microsoft\Office\Office Automatic Updates 2.0", false), () => Word || Excel || PowerPoint || OneNote || Teams || Outlook || OneDrive),
            ("Disabling Office startup entries", async () => TaskSchedulerHelper.Toggle(@"\Microsoft\Office\Office Background Push Maintenance", false), () => Word || Excel || PowerPoint || OneNote || Teams || Outlook || OneDrive),
            ("Disabling Office startup entries", async () => TaskSchedulerHelper.Toggle(@"\Microsoft\Office\Office ClickToRun Service Monitor", false), () => Word || Excel || PowerPoint || OneNote || Teams || Outlook || OneDrive),
            ("Disabling Office startup entries", async () => TaskSchedulerHelper.Toggle(@"\Microsoft\Office\Office Feature Updates", false), () => Word || Excel || PowerPoint || OneNote || Teams || Outlook || OneDrive),
            ("Disabling Office startup entries", async () => TaskSchedulerHelper.Toggle(@"\Microsoft\Office\Office Feature Updates Logon", false), () => Word || Excel || PowerPoint || OneNote || Teams || Outlook || OneDrive),
            ("Disabling Office startup entries", async () => TaskSchedulerHelper.Toggle(@"\Microsoft\Office\Office Performance Monitor", false), () => Word || Excel || PowerPoint || OneNote || Teams || Outlook || OneDrive),
            ("Disabling Office startup entries", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_CLASSES_ROOT\PROTOCOLS\Handler\AutorunsDisabled\mso-minsb.16\CLSID", "", "{42089D2D-912D-4018-9087-2B87803E93FB}", RegistryValueKind.String), () => OneDrive == true),
            ("Disabling Office startup entries", async () => RegistryHelper.DeleteKey(RegistryHelper.Identity.TrustedInstaller, @"HKEY_CLASSES_ROOT\PROTOCOLS\Handler\mso-minsb.16"), () => OneDrive == true),
            ("Disabling Office startup entries", async () => TaskSchedulerHelper.Toggle("OneDrive Per-Machine Standalone Update Task", false), () => OneDrive == true),
            ("Disabling Office startup entries", async () => TaskSchedulerHelper.Toggle("OneDrive Reporting Task", false), () => OneDrive == true),
            ("Disabling Office startup entries", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\FileSyncHelper", "Start", 4, RegistryValueKind.DWord), () => OneDrive == true),
            ("Disabling Office startup entries", async () => ServicesHelper.StopService("FileSyncHelper"), () => OneDrive == true),
            ("Disabling Office startup entries", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\OneDrive Updater Service", "Start", 4, RegistryValueKind.DWord), () => OneDrive == true),
            ("Disabling Office startup entries", async () => ServicesHelper.StopService("OneDrive Updater Service"), () => OneDrive == true),

            // disable office telemetry
            ("Disabling Office telemetry", async () => await ProcessActions.RunPowerShellScript("disableofficetelemetry.ps1", ""), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
        };

        if (selection != null)
        {
            return actions.Where(action => action.Condition != null && action.Condition.Invoke()).ToList();
        }

        return actions;
    }
}

