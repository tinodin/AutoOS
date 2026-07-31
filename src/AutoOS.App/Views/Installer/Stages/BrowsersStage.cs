using AutoOS.Common;
using AutoOS.Core.Common;
using AutoOS.Core.Helpers.Download;
using AutoOS.Core.Helpers.Registry;
using AutoOS.Core.Helpers.Services;
using AutoOS.Core.Helpers.Store;
using AutoOS.Core.Helpers.TaskScheduler;
using AutoOS.Views.Installer.Actions;
using Microsoft.Win32;
using System.Diagnostics;
using System.Text.Json.Nodes;
using System.Text.Json;
using Windows.Management.Deployment;

namespace AutoOS.Views.Installer.Stages;

public class BrowserSelection
{
	public bool Chrome { get; set; }
	public bool Thorium { get; set; }
	public bool Helium { get; set; }
	public bool Brave { get; set; }
	public bool Vivaldi { get; set; }
	public bool Arc { get; set; }
	public bool Comet { get; set; }
	public bool Firefox { get; set; }
	public bool Zen { get; set; }
	public bool Waterfox { get; set; }
	public bool LibreWolf { get; set; }
	public bool Floorp { get; set; }
	public bool Mullvad { get; set; }
	public bool uBlock { get; set; }
	public bool PrivacyBadger { get; set; }
	public bool Decentraleyes { get; set; }
	public bool Cookies { get; set; }
	public bool Violentmonkey { get; set; }
	public bool Tampermonkey { get; set; }
	public bool SponsorBlock { get; set; }
	public bool ReturnYouTubeDislike { get; set; }
	public bool YouTubeNoTranslation { get; set; }
	public bool DarkReader { get; set; }
	public bool Shazam { get; set; }
	public bool WaybackMachine { get; set; }
	public bool iCloud { get; set; }
	public bool Bitwarden { get; set; }
	public bool OnePassword { get; set; }
}

public static class BrowsersStage
{
	public static List<(string Title, Func<Task> Action, Func<bool> Condition)> GetActions(IStatusReporter reporter = null, BrowserSelection selection = null)
	{
		if (reporter == null && selection == null)
		{
			reporter = new InstallPageReporter();
		}

		bool? Chrome = selection?.Chrome ?? PreparingStage.Chrome;
		bool? Thorium = selection?.Thorium ?? PreparingStage.Thorium;
		bool? Helium = selection?.Helium ?? PreparingStage.Helium;
		bool? Brave = selection?.Brave ?? PreparingStage.Brave;
		bool? Vivaldi = selection?.Vivaldi ?? PreparingStage.Vivaldi;
		bool? Arc = selection?.Arc ?? PreparingStage.Arc;
		bool? Comet = selection?.Comet ?? PreparingStage.Comet;
		bool? Firefox = selection?.Firefox ?? PreparingStage.Firefox;
		bool? Zen = selection?.Zen ?? PreparingStage.Zen;
		bool? Waterfox = selection?.Waterfox ?? PreparingStage.Waterfox;
		bool? LibreWolf = selection?.LibreWolf ?? PreparingStage.LibreWolf;
		bool? Floorp = selection?.Floorp ?? PreparingStage.Floorp;
		bool? Mullvad = selection?.Mullvad ?? PreparingStage.Mullvad;
		bool? uBlock = selection?.uBlock ?? PreparingStage.uBlock;
		bool? PrivacyBadger = selection?.PrivacyBadger ?? PreparingStage.PrivacyBadger;
		bool? Decentraleyes = selection?.Decentraleyes ?? PreparingStage.Decentraleyes;
		bool? Cookies = selection?.Cookies ?? PreparingStage.Cookies;
		bool? Violentmonkey = selection?.Violentmonkey ?? PreparingStage.Violentmonkey;
		bool? Tampermonkey = selection?.Tampermonkey ?? PreparingStage.Tampermonkey;
		bool? SponsorBlock = selection?.SponsorBlock ?? PreparingStage.SponsorBlock;
		bool? ReturnYouTubeDislike = selection?.ReturnYouTubeDislike ?? PreparingStage.ReturnYouTubeDislike;
		bool? YouTubeNoTranslation = selection?.YouTubeNoTranslation ?? PreparingStage.YouTubeNoTranslation;
		bool? DarkReader = selection?.DarkReader ?? PreparingStage.DarkReader;
		bool? Shazam = selection?.Shazam ?? PreparingStage.Shazam;
		bool? WaybackMachine = selection?.WaybackMachine ?? PreparingStage.WaybackMachine;
		bool? iCloud = selection?.iCloud ?? PreparingStage.iCloud;
		bool? Bitwarden = selection?.Bitwarden ?? PreparingStage.Bitwarden;
		bool? OnePassword = selection?.OnePassword ?? PreparingStage.OnePassword;

		string edgeVersion = "";
		string chromeVersion = "";
		string chromeVersion2 = "";
		string chromePlatformExperienceHelperVersion = "";
		string thoriumVersion = "";
		string heliumVersion = "";
		string braveVersion = "";
		string vivaldiVersion = "";
		string arcVersion = "";
		string cometVersion = "";
		string firefoxVersion = "";

		var actions = new List<(string Title, Func<Task> Action, Func<bool> Condition)>
		{
			// optimize microsoft edge settings
			(@"Enabling ""Configure Do Not Track"" policy", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge", "ConfigureDoNotTrack", 1, RegistryValueKind.DWord), () => selection == null),
			(@"Disabling ""Shopping in Microsoft Edge Enabled"" policy", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge", "EdgeShoppingAssistantEnabled", 0, RegistryValueKind.DWord), () => selection == null),
			(@"Disabling ""Allow Microsoft content on the new tab page"" policy", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge", "NewTabPageContentEnabled", 0, RegistryValueKind.DWord), () => selection == null),
			(@"Disbaling ""Allow user feedback"" policy", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge", "UserFeedbackAllowed", 0, RegistryValueKind.DWord), () => selection == null),
			(@"Disabling ""Create Desktop Shortcut upon install default"" policy", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\EdgeUpdate", "CreateDesktopShortcutDefault", 0, RegistryValueKind.DWord), () => selection == null),
			(@"Enabling ""Turn off tracking of app usage"" policy", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Policies\Microsoft\Windows\EdgeUI", "DisableMFUTracking", 1, RegistryValueKind.DWord), () => selection == null),

			// disable microsoft edge services
			("Disabling Microsoft Edge services", async () => edgeVersion = FileVersionInfo.GetVersionInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Microsoft", "Edge", "Application", "msedge.exe")).ProductVersion, () => selection == null),
			("Disabling Microsoft Edge services", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{9459C573-B17A-45AE-9F64-1857B5D58CEE}", "", "Microsoft Edge", RegistryValueKind.String), () => selection == null),
			("Disabling Microsoft Edge services", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{9459C573-B17A-45AE-9F64-1857B5D58CEE}", "Localized Name", "Microsoft Edge", RegistryValueKind.String), () => selection == null),
			("Disabling Microsoft Edge services", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{9459C573-B17A-45AE-9F64-1857B5D58CEE}", "StubPath", $@"""{Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Microsoft", "Edge", "Application", edgeVersion, "Installer", "setup.exe")}"" --configure-user-settings --verbose-logging --system-level --msedge --channel=stable", RegistryValueKind.String), () => selection == null),
			("Disabling Microsoft Edge services", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{9459C573-B17A-45AE-9F64-1857B5D58CEE}", "Version", "43,0,0,0", RegistryValueKind.String), () => selection == null),
			("Disabling Microsoft Edge services", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{9459C573-B17A-45AE-9F64-1857B5D58CEE}", "IsInstalled", 1, RegistryValueKind.DWord), () => selection == null),
			("Disabling Microsoft Edge services", async () => RegistryHelper.DeleteKey(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\{9459C573-B17A-45AE-9F64-1857B5D58CEE}"), () => selection == null),
			("Disabling Microsoft Edge services", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{89B4C1CD-B018-4511-B0A1-5476DBF70820}", "ComponentID", "DOTNETFRAMEWORKS", RegistryValueKind.String), () => selection == null),
			("Disabling Microsoft Edge services", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{89B4C1CD-B018-4511-B0A1-5476DBF70820}", "DontAsk", 2, RegistryValueKind.DWord), () => selection == null),
			("Disabling Microsoft Edge services", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{89B4C1CD-B018-4511-B0A1-5476DBF70820}", "Enabled", 0, RegistryValueKind.DWord), () => selection == null),
			("Disabling Microsoft Edge services", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{89B4C1CD-B018-4511-B0A1-5476DBF70820}", "IsInstalled", 1, RegistryValueKind.DWord), () => selection == null),
			("Disabling Microsoft Edge services", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{89B4C1CD-B018-4511-B0A1-5476DBF70820}", "StubPath", $@"""{Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "System32", "Rundll32.exe")}"" ""{Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "System32", "mscories.dll")}"",Install", RegistryValueKind.String), () => selection == null),
			("Disabling Microsoft Edge services", async () => RegistryHelper.DeleteKey(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\{89B4C1CD-B018-4511-B0A1-5476DBF70820}"), () => selection == null),
			("Disabling Microsoft Edge services", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{89B4C1CD-B018-4511-B0A1-5476DBF70820}", "ComponentID", "DOTNETFRAMEWORKS", RegistryValueKind.String), () => selection == null),
			("Disabling Microsoft Edge services", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{89B4C1CD-B018-4511-B0A1-5476DBF70820}", "DontAsk", 2, RegistryValueKind.DWord), () => selection == null),
			("Disabling Microsoft Edge services", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{89B4C1CD-B018-4511-B0A1-5476DBF70820}", "Enabled", 0, RegistryValueKind.DWord), () => selection == null),
			("Disabling Microsoft Edge services", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{89B4C1CD-B018-4511-B0A1-5476DBF70820}", "IsInstalled", 1, RegistryValueKind.DWord), () => selection == null),
			("Disabling Microsoft Edge services", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{89B4C1CD-B018-4511-B0A1-5476DBF70820}", "StubPath", $@"""{Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "SysWOW64", "Rundll32.exe")}"" ""{Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "SysWOW64", "mscories.dll")}"",Install", RegistryValueKind.String), () => selection == null),
			("Disabling Microsoft Edge services", async () => RegistryHelper.DeleteKey(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\Active Setup\Installed Components\{89B4C1CD-B018-4511-B0A1-5476DBF70820}"), () => selection == null),
			("Disabling Microsoft Edge services", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Browser Helper Objects\AutorunsDisabled\{1FD49718-1D00-4B19-AF5F-070AF6D5D54C}", "", "IEToEdge BHO", RegistryValueKind.String), () => selection == null),
			("Disabling Microsoft Edge services", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Browser Helper Objects\AutorunsDisabled\{1FD49718-1D00-4B19-AF5F-070AF6D5D54C}", "NoExplorer", 1, RegistryValueKind.String), () => selection == null),
			("Disabling Microsoft Edge services", async () => RegistryHelper.DeleteKey(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Browser Helper Objects\{1FD49718-1D00-4B19-AF5F-070AF6D5D54C}"), () => selection == null),
			("Disabling Microsoft Edge services", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Explorer\Browser Helper Objects\AutorunsDisabled\{1FD49718-1D00-4B19-AF5F-070AF6D5D54C}", "", "IEToEdge BHO", RegistryValueKind.String), () => selection == null),
			("Disabling Microsoft Edge services", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Explorer\Browser Helper Objects\AutorunsDisabled\{1FD49718-1D00-4B19-AF5F-070AF6D5D54C}", "NoExplorer", 1, RegistryValueKind.String), () => selection == null),
			("Disabling Microsoft Edge services", async () => RegistryHelper.DeleteKey(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Explorer\Browser Helper Objects\{1FD49718-1D00-4B19-AF5F-070AF6D5D54C}"), () => selection == null),
			("Disabling Microsoft Edge services", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\edgeupdate", "Start", 4, RegistryValueKind.DWord), () => selection == null),
			("Disabling Microsoft Edge services", async () => ServicesHelper.StopService("edgeupdate"), () => selection == null),
			("Disabling Microsoft Edge services", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\edgeupdatem", "Start", 4, RegistryValueKind.DWord), () => selection == null),
			("Disabling Microsoft Edge services", async () => ServicesHelper.StopService("edgeupdatem"), () => selection == null),
			("Disabling Microsoft Edge services", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\MicrosoftEdgeElevationService", "Start", 4, RegistryValueKind.DWord), () => selection == null),
			("Disabling Microsoft Edge services", async () => ServicesHelper.StopService("MicrosoftEdgeElevationService"), () => selection == null),
			("Disabling Microsoft Edge services", async () => TaskSchedulerHelper.Toggle("MicrosoftEdgeUpdateTaskMachineCore", false), () => selection == null),
			("Disabling Microsoft Edge services", async () => TaskSchedulerHelper.Toggle("MicrosoftEdgeUpdateTaskMachineUA", false), () => selection == null),

			// download google chrome
			("Downloading Google Chrome", async () => await DownloadHelper.Download("http://dl.google.com/chrome/install/375.126/chrome_installer.exe", Path.GetTempPath(), "ChromeSetup.exe", reporter ?? new InstallPageReporter()), () => Chrome == true),

			// install google chrome
			("Installing Google Chrome", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Path.GetTempPath(), "ChromeSetup.exe"), Arguments = "--silent --install --system-level --do-not-launch-chrome", WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), () => Chrome == true),
			("Installing Google Chrome", async () => chromeVersion = FileVersionInfo.GetVersionInfo(Path.Combine(Path.GetTempPath(), "ChromeSetup.exe")).ProductVersion, () => Chrome == true),
			("Installing Google Chrome", async () => chromeVersion2 = FileVersionInfo.GetVersionInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Google", "Chrome", "Application", "chrome.exe")).ProductVersion, () => Chrome == true),
			("Installing Google Chrome", async () => { using var process = Process.Start(new ProcessStartInfo { FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Google", "Chrome", "Application", "chrome.exe"), WindowStyle = ProcessWindowStyle.Maximized }); while (!File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Google", "Chrome", "Application", "PlatformExperienceHelper", "platform_experience_helper.exe"))) await Task.Delay(100); process.Kill(); }, () => Chrome == true),
            ("Cleaning up Google Chrome files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "ChromeSetup.exe")), () => Chrome == true),

			// pin google chrome to the taskbar
			("Pinning Google Chrome to the taskbar", async () => await ProcessActions.PinToTaskbar("Link", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Microsoft", "Windows", "Start Menu", "Programs", "Google Chrome.lnk")), () => Chrome == true),

			// install ublock origin extension
			("Installing uBlock Origin Extension", async () => await InstallChromiumExtension(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Google\Chrome\ExtensionInstallForcelist", "cjpalhdlnbpafiamejdnhcphjbkeiagm"), () => Chrome == true && uBlock == true),

			// install privacy badger extension
			("Installing Privacy Badger Extension", async () => await InstallChromiumExtension(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Google\Chrome\ExtensionInstallForcelist", "pkehgijcmpdhfbdbbnkijodmdjhbjlgp"), () => Chrome == true && PrivacyBadger == true),

			// install decentraleyes extension
			("Installing Decentraleyes Extension", async () => await InstallChromiumExtension(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Google\Chrome\ExtensionInstallForcelist", "ldpochfccmkkmhdbclfhpagapcfdljkj"), () => Chrome == true && Decentraleyes == true),

			// install i still don't care about cookies extension
			("Installing I still don't care about cookies Extension", async () => await InstallChromiumExtension(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Google\Chrome\ExtensionInstallForcelist", "edibdbjcniadpccecjdfdjjppcpchdlm"), () => Chrome == true && Cookies == true),

			// install violentmonkey extension
			("Installing Violentmonkey Extension", async () => await InstallChromiumExtension(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Google\Chrome\ExtensionInstallForcelist", "jinjaccalgkegednnccohejagnlnfdag"), () => Chrome == true && Violentmonkey == true),

			// install tampermonkey extension
			("Installing Tampermonkey Extension", async () => await InstallChromiumExtension(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Google\Chrome\ExtensionInstallForcelist", "dhdgffkkebhmkfjojejmpbldmpobfkfo"), () => Chrome == true && Tampermonkey == true),

			// install sponsorblock extension
			("Installing SponsorBlock Extension", async () => await InstallChromiumExtension(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Google\Chrome\ExtensionInstallForcelist", "mnjggcdmjocbbbhaepdhchncahnbgone"), () => Chrome == true && SponsorBlock == true),

			// install return youtube dislike extension
			("Installing Return YouTube Dislike Extension", async () => await InstallChromiumExtension(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Google\Chrome\ExtensionInstallForcelist", "gebbhagfogifgggkldgodflihgfeippi"), () => Chrome == true && ReturnYouTubeDislike == true),

			// install youtube no translation extension
			("Installing YouTube No Translation Extension", async () => await InstallChromiumExtension(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Google\Chrome\ExtensionInstallForcelist", "lmkeolibdeeglfglnncmfleojmakecjb"), () => Chrome == true && YouTubeNoTranslation == true),

			// install dark reader extension
			("Installing Dark Reader Extension", async () => await InstallChromiumExtension(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Google\Chrome\ExtensionInstallForcelist", "eimadpbcbfnmbkopoojfekhnkhdbieeh"), () => Chrome == true && DarkReader == true),
			
			// install shazam extension
			("Installing Shazam Extension", async () => await InstallChromiumExtension(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Google\Chrome\ExtensionInstallForcelist", "mmioliijnhnoblpgimnlajmefafdfilb"), () => Chrome == true && Shazam == true),

			// install wayback machine extension
			("Installing Wayback Machine Extension", async () => await InstallChromiumExtension(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Google\Chrome\ExtensionInstallForcelist", "fpnmgdkabkmnadcjpehmlllkndpkmiak"), () => Chrome == true && WaybackMachine == true),

			// install icloud passwords extension
			("Installing iCloud Passwords Extension", async () => await InstallChromiumExtension(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Google\Chrome\ExtensionInstallForcelist", "pejdijmoenmkgeppbflobdenhhabjlaj"), () => Chrome == true && iCloud == true),

			// install bitwarden extension
			("Installing Bitwarden Extension", async () => await InstallChromiumExtension(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Google\Chrome\ExtensionInstallForcelist", "nngceckbapebfimnlniiiahkandclblb"), () => Chrome == true && Bitwarden == true),

			// install 1password extension
			("Installing 1Password Extension", async () => await InstallChromiumExtension(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Google\Chrome\ExtensionInstallForcelist", "aeblfdkhhhdcdjpifhhbdiojplfjncoa"), () => Chrome == true && OnePassword == true),

			// remove google chrome shortcut from the desktop
			("Removing Google Chrome shortcut from the desktop", async () => File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory), "Google Chrome.lnk")), () => Chrome == true),

			// disable google chrome services
			("Disabling Google Chrome services", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\GoogleChromeElevationService", "Start", 4, RegistryValueKind.DWord), () => Chrome == true),
			("Disabling Google Chrome services", async () => ServicesHelper.StopService("GoogleChromeElevationService"), () => Chrome == true),
			("Disabling Google Chrome services", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, $@"HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\GoogleUpdaterInternalService{chromeVersion}", "Start", 4, RegistryValueKind.DWord), () => Chrome == true),
			("Disabling Google Chrome services", async () => ServicesHelper.StopService($@"GoogleUpdaterInternalService{chromeVersion}"), () => Chrome == true),
			("Disabling Google Chrome services", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, $@"HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\GoogleUpdaterService{chromeVersion}", "Start", 4, RegistryValueKind.DWord), () => Chrome == true),
			("Disabling Google Chrome services", async () => ServicesHelper.StopService($@"GoogleUpdaterService{chromeVersion}"), () => Chrome == true),
			("Disabling Google Chrome services", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{8A69D345-D564-463c-AFF1-A69D9E530F96}", "", "Google Chrome", RegistryValueKind.String), () => Chrome == true),
			("Disabling Google Chrome services", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{8A69D345-D564-463c-AFF1-A69D9E530F96}", "Localized Name", "Google Chrome", RegistryValueKind.String), () => Chrome == true),
			("Disabling Google Chrome services", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, $@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{{8A69D345-D564-463c-AFF1-A69D9E530F96}}", "StubPath", $@"""{Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Google", "Chrome", "Application", chromeVersion2, "Installer", "chrmstp.exe")}"" --configure-user-settings --verbose-logging --system-level --channel=stable", RegistryValueKind.String), () => Chrome == true),
			("Disabling Google Chrome services", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{8A69D345-D564-463c-AFF1-A69D9E530F96}", "Version", "43,0,0,0", RegistryValueKind.String), () => Chrome == true),
			("Disabling Google Chrome services", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{8A69D345-D564-463c-AFF1-A69D9E530F96}", "IsInstalled", 1, RegistryValueKind.DWord), () => Chrome == true),
			("Disabling Google Chrome services", async () => RegistryHelper.DeleteKey(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\{8A69D345-D564-463c-AFF1-A69D9E530F96}"), () => Chrome == true),
			("Disabling Google Chrome services", async () => chromePlatformExperienceHelperVersion = FileVersionInfo.GetVersionInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Google", "Chrome", "Application", "PlatformExperienceHelper", "platform_experience_helper.exe")).ProductVersion, () => Chrome == true),
			("Disabling Google Chrome services", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{49210152-871f-4ffa-961d-a172abcbc09d}", "", "Google Platform Experience Helper", RegistryValueKind.String), () => Chrome == true),
			("Disabling Google Chrome services", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{49210152-871f-4ffa-961d-a172abcbc09d}", "Localized Name", "Google Platform Experience Helper", RegistryValueKind.String), () => Chrome == true),
			("Disabling Google Chrome services", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, $@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{{49210152-871f-4ffa-961d-a172abcbc09d}}", "StubPath", $@"""{Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Google", "Chrome", "Application", "PlatformExperienceHelper", "platform_experience_helper.exe")}"" --first-run", RegistryValueKind.String), () => Chrome == true),
			("Disabling Google Chrome services", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{49210152-871f-4ffa-961d-a172abcbc09d}", "Version", chromePlatformExperienceHelperVersion.Replace('.', ','), RegistryValueKind.String), () => Chrome == true),
			("Disabling Google Chrome services", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{49210152-871f-4ffa-961d-a172abcbc09d}", "IsInstalled", 1, RegistryValueKind.DWord), () => Chrome == true),
			("Disabling Google Chrome services", async () => RegistryHelper.DeleteKey(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\{49210152-871f-4ffa-961d-a172abcbc09d}"), () => Chrome == true),
			("Disabling Google Chrome services", async () => TaskSchedulerHelper.Toggle("GoogleUpdaterTaskSystem", false), () => Chrome == true),
			("Disabling Google Chrome services", async () => TaskSchedulerHelper.Toggle(@"\GoogleUserPEH\RunPlatformExperienceHelper_Daily", false), () => Chrome == true),
			("Disabling Google Chrome services", async () => TaskSchedulerHelper.Toggle(@"\GoogleUserPEH\RunPlatformExperienceHelper_Metrics", false), () => Chrome == true),

			// download thorium
			("Downloading Thorium", async () => await DownloadHelper.Download(JsonDocument.Parse(await new HttpClient { DefaultRequestHeaders = { { "User-Agent", "AutoOS" } } }.GetStringAsync("https://api.github.com/repos/Alex313031/Thorium-Win/releases")).RootElement.EnumerateArray().First(r => r.GetProperty("assets").EnumerateArray().Any(a => a.GetProperty("name").GetString().Contains("thorium_SSE4_mini_installer.exe"))).GetProperty("assets").EnumerateArray().First(a => a.GetProperty("name").GetString().Contains("thorium_SSE4_mini_installer.exe")).GetProperty("browser_download_url").GetString(), Path.GetTempPath(), "ThoriumSetup.exe", reporter ?? new InstallPageReporter()), () => Thorium == true),
			
			// install thorium
			("Installing Thorium", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Path.GetTempPath(), "ThoriumSetup.exe"), Arguments = "--silent --install --system-level --do-not-launch-chrome", WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), () => Thorium == true),
			("Installing Thorium", async () => thoriumVersion = FileVersionInfo.GetVersionInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Thorium", "Application", "thorium.exe")).ProductVersion, () => Thorium == true),
			("Cleaning up Thorium files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "ThoriumSetup.exe")), () => Thorium == true),

			// disable thorium services
			("Disabling Thorium services", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{7D2B3E1D-D096-4594-9D8F-A6667F12E0AC}", "", "Thorium", RegistryValueKind.String), () => Thorium == true),
			("Disabling Thorium services", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{7D2B3E1D-D096-4594-9D8F-A6667F12E0AC}", "Localized Name", "Thorium", RegistryValueKind.String), () => Thorium == true),
			("Disabling Thorium services", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, $@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{{7D2B3E1D-D096-4594-9D8F-A6667F12E0AC}}", "StubPath", $@"""{Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Thorium", "Application", thoriumVersion, "Installer", "chrmstp.exe")}"" --configure-user-settings --verbose-logging --system-level", RegistryValueKind.String), () => Thorium == true),
			("Disabling Thorium services", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{7D2B3E1D-D096-4594-9D8F-A6667F12E0AC}", "Version", "43,0,0,0", RegistryValueKind.String), () => Thorium == true),
			("Disabling Thorium services", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{7D2B3E1D-D096-4594-9D8F-A6667F12E0AC}", "IsInstalled", 1, RegistryValueKind.DWord), () => Thorium == true),
			("Disabling Thorium services", async () => RegistryHelper.DeleteKey(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\{7D2B3E1D-D096-4594-9D8F-A6667F12E0AC}"), () => Thorium == true),

			// pin thorium to the taskbar
			("Pinning Thorium to the taskbar", async () => await ProcessActions.PinToTaskbar("Link", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Microsoft", "Windows", "Start Menu", "Programs", "Thorium.lnk")), () => Thorium == true),

			// install privacy badger extension
			("Installing Privacy Badger Extension", async () => await InstallChromiumExtension(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Chromium\ExtensionInstallForcelist", "pkehgijcmpdhfbdbbnkijodmdjhbjlgp"), () => Thorium == true && PrivacyBadger == true),

			// install decentraleyes extension
			("Installing Decentraleyes Extension", async () => await InstallChromiumExtension(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Chromium\ExtensionInstallForcelist", "ldpochfccmkkmhdbclfhpagapcfdljkj"), () => Thorium == true && Decentraleyes == true),

			// install sponsorblock extension
			("Installing SponsorBlock Extension", async () => await InstallChromiumExtension(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Chromium\ExtensionInstallForcelist", "mnjggcdmjocbbbhaepdhchncahnbgone"), () => Thorium == true && SponsorBlock == true),

			// install return youtube dislike extension
			("Installing ReturnYouTubeDislike Extension", async () => await InstallChromiumExtension(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Chromium\ExtensionInstallForcelist", "gebbhagfogifgggkldgodflihgfeippi"), () => Thorium == true && ReturnYouTubeDislike == true),

			// install youtube no translation extension
			("Installing YouTube No Translation Extension", async () => await InstallChromiumExtension(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Chromium\ExtensionInstallForcelist", "lmkeolibdeeglfglnncmfleojmakecjb"), () => Thorium == true && YouTubeNoTranslation == true),

			// install i still dont care about cookies extension
			("Installing I still don't care about cookies Extension", async () => await InstallChromiumExtension(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Chromium\ExtensionInstallForcelist", "edibdbjcniadpccecjdfdjjppcpchdlm"), () => Thorium == true && Cookies == true),

			// install dark reader extension
			("Installing Dark Reader Extension", async () => await InstallChromiumExtension(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Chromium\ExtensionInstallForcelist", "eimadpbcbfnmbkopoojfekhnkhdbieeh"), () => Thorium == true && DarkReader == true),
			
			// install violentmonkey extension
			("Installing Violentmonkey Extension", async () => await InstallChromiumExtension(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Chromium\ExtensionInstallForcelist", "jinjaccalgkegednnccohejagnlnfdag"), () => Thorium == true && Violentmonkey == true),

			// install tampermonkey extension
			("Installing Tampermonkey Extension", async () => await InstallChromiumExtension(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Chromium\ExtensionInstallForcelist", "dhdgffkkebhmkfjojejmpbldmpobfkfo"), () => Thorium == true && Tampermonkey == true),

			// install shazam extension
			("Installing Shazam Extension", async () => await InstallChromiumExtension(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Chromium\ExtensionInstallForcelist", "mmioliijnhnoblpgimnlajmefafdfilb"), () => Thorium == true && Shazam == true),

			// install wayback machine extension
			("Installing Wayback Machine Extension", async () => await InstallChromiumExtension(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Chromium\ExtensionInstallForcelist", "fpnmgdkabkmnadcjpehmlllkndpkmiak"), () => Thorium == true && WaybackMachine == true),

			// install icloud passwords extension
			("Installing iCloud Passwords Extension", async () => await InstallChromiumExtension(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Chromium\ExtensionInstallForcelist", "pejdijmoenmkgeppbflobdenhhabjlaj"), () => Thorium == true && iCloud == true),

			// install bitwarden extension
			("Installing Bitwarden Extension", async () => await InstallChromiumExtension(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Chromium\ExtensionInstallForcelist", "nngceckbapebfimnlniiiahkandclblb"), () => Thorium == true && Bitwarden == true),

			// install 1password extension
			("Installing 1Password Extension", async () => await InstallChromiumExtension(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Chromium\ExtensionInstallForcelist", "aeblfdkhhhdcdjpifhhbdiojplfjncoa"), () => Thorium == true && OnePassword == true),

			// remove thorium shortcut from the desktop
			("Removing Thorium shortcut from the desktop", async () => File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory), "Thorium.lnk")), () => Thorium == true),

			// download helium
			("Downloading Helium", async () => await DownloadHelper.Download(JsonDocument.Parse(await new HttpClient { DefaultRequestHeaders = { { "User-Agent", "AutoOS" } } }.GetStringAsync("https://api.github.com/repos/imputnet/helium-windows/releases/latest")).RootElement.GetProperty("assets").EnumerateArray().First(a => a.GetProperty("name").GetString().Contains("_x64-installer.exe")).GetProperty("browser_download_url").GetString(), Path.GetTempPath(), "helium_x64-installer.exe", reporter ?? new InstallPageReporter()), () => Helium == true),

			// install helium
			("Installing Helium", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Path.GetTempPath(), "helium_x64-installer.exe"), Arguments = "/S /SYSTEM", WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), () => Helium == true),
			("Installing Helium", async () => heliumVersion = FileVersionInfo.GetVersionInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "imput", "Helium", "Application", "chrome.exe")).ProductVersion, () => Helium == true),
			("Cleaning up Helium files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "helium_x64-installer.exe")), () => Helium == true),

			// disable helium services
			("Disabling Helium services", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\HeliumElevationService", "Start", 4, RegistryValueKind.DWord), () => Helium == true),
			("Disabling Helium services", async () => ServicesHelper.StopService("HeliumElevationService"), () => Helium == true),
			("Disabling Helium services", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{FB68A146-637A-48C2-A0C4-1565DE45FEBD}", "", "Helium", RegistryValueKind.String), () => Helium == true),
			("Disabling Helium services", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{FB68A146-637A-48C2-A0C4-1565DE45FEBD}", "Localized Name", "Helium", RegistryValueKind.String), () => Helium == true),
			("Disabling Helium services", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, $@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{{FB68A146-637A-48C2-A0C4-1565DE45FEBD}}", "StubPath", $@"""{Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Helium", "Application", heliumVersion, "Installer", "chrmstp.exe")}"" --configure-user-settings --verbose-logging --system-level", RegistryValueKind.String), () => Helium == true),
			("Disabling Helium services", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{FB68A146-637A-48C2-A0C4-1565DE45FEBD}", "Version", "43,0,0,0", RegistryValueKind.String), () => Helium == true),
			("Disabling Helium services", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{FB68A146-637A-48C2-A0C4-1565DE45FEBD}", "IsInstalled", 1, RegistryValueKind.DWord), () => Helium == true),
			("Disabling Helium services", async () => RegistryHelper.DeleteKey(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\{FB68A146-637A-48C2-A0C4-1565DE45FEBD}"), () => Helium == true),

			// pin helium to the taskbar
			("Pinning Helium to the taskbar", async () => await ProcessActions.PinToTaskbar("Link", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Microsoft", "Windows", "Start Menu", "Programs", "Helium.lnk")), () => Helium == true),

			// install privacy badger extension
			("Installing Privacy Badger Extension", async () => await InstallChromiumExtension(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Helium\ExtensionInstallForcelist", "pkehgijcmpdhfbdbbnkijodmdjhbjlgp"), () => Helium == true && PrivacyBadger == true),

			// install decentraleyes extension
			("Installing Decentraleyes Extension", async () => await InstallChromiumExtension(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Helium\ExtensionInstallForcelist", "ldpochfccmkkmhdbclfhpagapcfdljkj"), () => Helium == true && Decentraleyes == true),

			// install i still don't care about cookies extension
			("Installing I still don't care about cookies Extension", async () => await InstallChromiumExtension(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Helium\ExtensionInstallForcelist", "edibdbjcniadpccecjdfdjjppcpchdlm"), () => Helium == true && Cookies == true),

			// install violentmonkey extension
			("Installing Violentmonkey Extension", async () => await InstallChromiumExtension(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Helium\ExtensionInstallForcelist", "jinjaccalgkegednnccohejagnlnfdag"), () => Helium == true && Violentmonkey == true),

			// install tampermonkey extension
			("Installing Tampermonkey Extension", async () => await InstallChromiumExtension(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Helium\ExtensionInstallForcelist", "dhdgffkkebhmkfjojejmpbldmpobfkfo"), () => Helium == true && Tampermonkey == true),

			// install sponsorblock extension
			("Installing SponsorBlock Extension", async () => await InstallChromiumExtension(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Helium\ExtensionInstallForcelist", "mnjggcdmjocbbbhaepdhchncahnbgone"), () => Helium == true && SponsorBlock == true),

			// install return youtube dislike extension
			("Installing Return YouTube Dislike Extension", async () => await InstallChromiumExtension(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Helium\ExtensionInstallForcelist", "gebbhagfogifgggkldgodflihgfeippi"), () => Helium == true && ReturnYouTubeDislike == true),

			// install youtube no translation extension
			("Installing YouTube No Translation Extension", async () => await InstallChromiumExtension(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Helium\ExtensionInstallForcelist", "lmkeolibdeeglfglnncmfleojmakecjb"), () => Helium == true && YouTubeNoTranslation == true),

			// install dark reader extension
			("Installing Dark Reader Extension", async () => await InstallChromiumExtension(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Helium\ExtensionInstallForcelist", "eimadpbcbfnmbkopoojfekhnkhdbieeh"), () => Helium == true && DarkReader == true),
			
			// install shazam extension
			("Installing Shazam Extension", async () => await InstallChromiumExtension(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Helium\ExtensionInstallForcelist", "mmioliijnhnoblpgimnlajmefafdfilb"), () => Helium == true && Shazam == true),

			// install wayback machine extension
			("Installing Wayback Machine Extension", async () => await InstallChromiumExtension(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Helium\ExtensionInstallForcelist", "fpnmgdkabkmnadcjpehmlllkndpkmiak"), () => Helium == true && WaybackMachine == true),

			// install icloud passwords extension
			("Installing iCloud Passwords Extension", async () => await InstallChromiumExtension(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Helium\ExtensionInstallForcelist", "pejdijmoenmkgeppbflobdenhhabjlaj"), () => Helium == true && iCloud == true),

			// install bitwarden extension
			("Installing Bitwarden Extension", async () => await InstallChromiumExtension(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Helium\ExtensionInstallForcelist", "nngceckbapebfimnlniiiahkandclblb"), () => Helium == true && Bitwarden == true),

			// install 1password extension
			("Installing 1Password Extension", async () => await InstallChromiumExtension(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Helium\ExtensionInstallForcelist", "aeblfdkhhhdcdjpifhhbdiojplfjncoa"), () => Helium == true && OnePassword == true),

			// remove helium shortcut from the desktop
			("Removing Helium shortcut from the desktop", async () => File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory), "Helium.lnk")), () => Helium == true),

			// download brave
			("Downloading Brave", async () => await DownloadHelper.Download(JsonDocument.Parse(await new HttpClient { DefaultRequestHeaders = { { "User-Agent", "AutoOS" } } }.GetStringAsync("https://api.github.com/repos/brave/brave-browser/releases/latest")).RootElement.GetProperty("assets").EnumerateArray().First(a => a.GetProperty("name").GetString() == "BraveBrowserStandaloneSetup.exe").GetProperty("browser_download_url").GetString(), Path.GetTempPath(), "BraveBrowserStandaloneSetup.exe", reporter ?? new InstallPageReporter()), () => Brave == true),
			
			// install brave
			("Installing Brave", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Path.GetTempPath(), "BraveBrowserStandaloneSetup.exe"), Arguments = "/silent /install", WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), () => Brave == true),
			("Installing Brave", async () => braveVersion = FileVersionInfo.GetVersionInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "BraveSoftware", "Brave-Browser", "Application", "brave.exe")).ProductVersion, () => Brave == true),
			("Cleaning up Brave files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "BraveBrowserStandaloneSetup.exe")), () => Brave == true),

			// pin brave to the taskbar
			("Pinning Brave to the taskbar", async () => await ProcessActions.PinToTaskbar("Link", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Microsoft", "Windows", "Start Menu", "Programs", "Brave.lnk")), () => Brave == true),

			// remove brave shortcut from the desktop
			("Removing Brave shortcut from the desktop", async () => File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory), "Brave.lnk")), () => Brave == true),

			// optimize brave settings
			//("Optimizing Brave settings", async () => await Task.Run(() => File.Copy(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Scripts", "initial_preferences"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "BraveSoftware", "Brave-Browser", "Application", "initial_preferences"), true)), () => Brave == true),
			//("Optimizing Brave settings", async () => await Task.Run(() => Directory.CreateDirectory(Environment.ExpandEnvironmentVariables(@"%LOCALAPPDATA%\BraveSoftware\Brave-Browser\User Data"))), () => Brave == true),
			//("Optimizing Brave settings", async () => await Task.Run(() => File.Copy(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Scripts", "Local State"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BraveSoftware", "Brave-Browser", "User Data", "Local State"), true)), () => Brave == true),

			// disable brave services
			("Disabling Brave services", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\brave", "Start", 4, RegistryValueKind.DWord), () => Brave == true),
			("Disabling Brave services", async () => ServicesHelper.StopService("brave"), () => Brave == true),
			("Disabling Brave services", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\BraveElevationService", "Start", 4, RegistryValueKind.DWord), () => Brave == true),
			("Disabling Brave services", async () => ServicesHelper.StopService("BraveElevationService"), () => Brave == true),
			("Disabling Brave services", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\bravem", "Start", 4, RegistryValueKind.DWord), () => Brave == true),
			("Disabling Brave services", async () => ServicesHelper.StopService("bravem"), () => Brave == true),
			("Disabling Brave services", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{AFE6A462-C574-4B8A-AF43-4CC60DF4563B}", "", "Brave", RegistryValueKind.String), () => Brave == true),
			("Disabling Brave services", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{AFE6A462-C574-4B8A-AF43-4CC60DF4563B}", "Localized Name", "Brave", RegistryValueKind.String), () => Brave == true),
			("Disabling Brave services", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, $@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{{AFE6A462-C574-4B8A-AF43-4CC60DF4563B}}", "StubPath", $@"""{Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "BraveSoftware", "Brave-Browser", "Application", braveVersion, "Installer", "chrmstp.exe")}"" --configure-user-settings --verbose-logging --system-level", RegistryValueKind.String), () => Brave == true),
			("Disabling Brave services", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{AFE6A462-C574-4B8A-AF43-4CC60DF4563B}", "Version", "43,0,0,0", RegistryValueKind.String), () => Brave == true),
			("Disabling Brave services", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{AFE6A462-C574-4B8A-AF43-4CC60DF4563B}", "IsInstalled", 1, RegistryValueKind.DWord), () => Brave == true),
			("Disabling Brave services", async () => RegistryHelper.DeleteKey(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\{AFE6A462-C574-4B8A-AF43-4CC60DF4563B}"), () => Brave == true),
			("Disabling Brave services", async () => TaskSchedulerHelper.Toggle("BraveSoftwareUpdateTaskMachineCore", false), () => Brave == true),
			("Disabling Brave services", async () => TaskSchedulerHelper.Toggle("BraveSoftwareUpdateTaskMachineUA", false), () => Brave == true),

			// install ublock origin extension
			("Installing uBlock Origin Extension", async () => await InstallChromiumExtension(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\BraveSoftware\Brave\ExtensionInstallForcelist", "cjpalhdlnbpafiamejdnhcphjbkeiagm"), () => Brave == true && uBlock == true),

			// install privacy badger extension
			("Installing Privacy Badger Extension", async () => await InstallChromiumExtension(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\BraveSoftware\Brave\ExtensionInstallForcelist", "pkehgijcmpdhfbdbbnkijodmdjhbjlgp"), () => Brave == true && PrivacyBadger == true),

			// install decentraleyes extension
			("Installing Decentraleyes Extension", async () => await InstallChromiumExtension(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\BraveSoftware\Brave\ExtensionInstallForcelist", "ldpochfccmkkmhdbclfhpagapcfdljkj"), () => Brave == true && Decentraleyes == true),

			// install i still don't care about cookies extension
			("Installing I still don't care about cookies Extension", async () => await InstallChromiumExtension(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\BraveSoftware\Brave\ExtensionInstallForcelist", "edibdbjcniadpccecjdfdjjppcpchdlm"), () => Brave == true && Cookies == true),

			// install violentmonkey extension
			("Installing Violentmonkey Extension", async () => await InstallChromiumExtension(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\BraveSoftware\Brave\ExtensionInstallForcelist", "jinjaccalgkegednnccohejagnlnfdag"), () => Brave == true && Violentmonkey == true),

			// install tampermonkey extension
			("Installing Tampermonkey Extension", async () => await InstallChromiumExtension(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\BraveSoftware\Brave\ExtensionInstallForcelist", "dhdgffkkebhmkfjojejmpbldmpobfkfo"), () => Brave == true && Tampermonkey == true),

			// install sponsorblock extension
			("Installing SponsorBlock Extension", async () => await InstallChromiumExtension(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\BraveSoftware\Brave\ExtensionInstallForcelist", "mnjggcdmjocbbbhaepdhchncahnbgone"), () => Brave == true && SponsorBlock == true),

			// install return youtube dislike extension
			("Installing Return YouTube Dislike Extension", async () => await InstallChromiumExtension(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\BraveSoftware\Brave\ExtensionInstallForcelist", "gebbhagfogifgggkldgodflihgfeippi"), () => Brave == true && ReturnYouTubeDislike == true),

			// install youtube no translation extension
			("Installing YouTube No Translation Extension", async () => await InstallChromiumExtension(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\BraveSoftware\Brave\ExtensionInstallForcelist", "lmkeolibdeeglfglnncmfleojmakecjb"), () => Brave == true && YouTubeNoTranslation == true),

			// install dark reader extension
			("Installing Dark Reader Extension", async () => await InstallChromiumExtension(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\BraveSoftware\Brave\ExtensionInstallForcelist", "eimadpbcbfnmbkopoojfekhnkhdbieeh"), () => Brave == true && DarkReader == true),
			
			// install shazam extension
			("Installing Shazam Extension", async () => await InstallChromiumExtension(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\BraveSoftware\Brave\ExtensionInstallForcelist", "mmioliijnhnoblpgimnlajmefafdfilb"), () => Brave == true && Shazam == true),

			// install wayback machine extension
			("Installing Wayback Machine Extension", async () => await InstallChromiumExtension(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\BraveSoftware\Brave\ExtensionInstallForcelist", "fpnmgdkabkmnadcjpehmlllkndpkmiak"), () => Brave == true && WaybackMachine == true),

			// install icloud passwords extension
			("Installing iCloud Passwords Extension", async () => await InstallChromiumExtension(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\BraveSoftware\Brave\ExtensionInstallForcelist", "pejdijmoenmkgeppbflobdenhhabjlaj"), () => Brave == true && iCloud == true),

			// install bitwarden extension
			("Installing Bitwarden Extension", async () => await InstallChromiumExtension(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\BraveSoftware\Brave\ExtensionInstallForcelist", "nngceckbapebfimnlniiiahkandclblb"), () => Brave == true && Bitwarden == true),

			// install 1password extension
			("Installing 1Password Extension", async () => await InstallChromiumExtension(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\BraveSoftware\Brave\ExtensionInstallForcelist", "aeblfdkhhhdcdjpifhhbdiojplfjncoa"), () => Brave == true && OnePassword == true),

			// download vivaldi
			("Downloading Vivaldi", async () => await DownloadHelper.Download("https://vivaldi.com/download/Vivaldi.x64.exe", Path.GetTempPath(), "Vivaldi.x64.exe", reporter ?? new InstallPageReporter()), () => Vivaldi == true),

			// install vivaldi
			("Installing Vivaldi", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Path.GetTempPath(), "Vivaldi.x64.exe"), Arguments = "--vivaldi-silent --do-not-launch-chrome --system-level", WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), () => Vivaldi == true),
			("Installing Vivaldi", async () => vivaldiVersion = FileVersionInfo.GetVersionInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Vivaldi", "Application", "vivaldi.exe")).ProductVersion, () => Vivaldi == true),
			("Cleaning up Vivaldi files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "Vivaldi.x64.exe")), () => Vivaldi == true),

			// pin vivaldi to the taskbar
			("Pinning Vivaldi to the taskbar", async () => await ProcessActions.PinToTaskbar("Link", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Microsoft", "Windows", "Start Menu", "Programs", "Vivaldi.lnk")), () => Vivaldi == true),

			// install ublock origin extension
			("Installing uBlock Origin Extension", async () => await InstallChromiumExtension(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Vivaldi\ExtensionInstallForcelist", "cjpalhdlnbpafiamejdnhcphjbkeiagm"), () => Vivaldi == true && uBlock == true),

			// install privacy badger extension
			("Installing Privacy Badger Extension", async () => await InstallChromiumExtension(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Vivaldi\ExtensionInstallForcelist", "pkehgijcmpdhfbdbbnkijodmdjhbjlgp"), () => Vivaldi == true && PrivacyBadger == true),

			// install decentraleyes extension
			("Installing Decentraleyes Extension", async () => await InstallChromiumExtension(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Vivaldi\ExtensionInstallForcelist", "ldpochfccmkkmhdbclfhpagapcfdljkj"), () => Vivaldi == true && Decentraleyes == true),

			// install i still don't care about cookies extension
			("Installing I still don't care about cookies Extension", async () => await InstallChromiumExtension(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Vivaldi\ExtensionInstallForcelist", "edibdbjcniadpccecjdfdjjppcpchdlm"), () => Vivaldi == true && Cookies == true),

			// install violentmonkey extension
			("Installing Violentmonkey Extension", async () => await InstallChromiumExtension(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Vivaldi\ExtensionInstallForcelist", "jinjaccalgkegednnccohejagnlnfdag"), () => Vivaldi == true && Violentmonkey == true),

			// install tampermonkey extension
			("Installing Tampermonkey Extension", async () => await InstallChromiumExtension(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Vivaldi\ExtensionInstallForcelist", "dhdgffkkebhmkfjojejmpbldmpobfkfo"), () => Vivaldi == true && Tampermonkey == true),

			// install sponsorblock extension
			("Installing SponsorBlock Extension", async () => await InstallChromiumExtension(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Vivaldi\ExtensionInstallForcelist", "mnjggcdmjocbbbhaepdhchncahnbgone"), () => Vivaldi == true && SponsorBlock == true),

			// install return youtube dislike extension
			("Installing Return YouTube Dislike Extension", async () => await InstallChromiumExtension(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Vivaldi\ExtensionInstallForcelist", "gebbhagfogifgggkldgodflihgfeippi"), () => Vivaldi == true && ReturnYouTubeDislike == true),

			// install youtube no translation extension
			("Installing YouTube No Translation Extension", async () => await InstallChromiumExtension(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Vivaldi\ExtensionInstallForcelist", "lmkeolibdeeglfglnncmfleojmakecjb"), () => Vivaldi == true && YouTubeNoTranslation == true),

			// install dark reader extension
			("Installing Dark Reader Extension", async () => await InstallChromiumExtension(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Vivaldi\ExtensionInstallForcelist", "eimadpbcbfnmbkopoojfekhnkhdbieeh"), () => Vivaldi == true && DarkReader == true),
			
			// install shazam extension
			("Installing Shazam Extension", async () => await InstallChromiumExtension(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Vivaldi\ExtensionInstallForcelist", "mmioliijnhnoblpgimnlajmefafdfilb"), () => Vivaldi == true && Shazam == true),

			// install wayback machine extension
			("Installing Wayback Machine Extension", async () => await InstallChromiumExtension(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Vivaldi\ExtensionInstallForcelist", "fpnmgdkabkmnadcjpehmlllkndpkmiak"), () => Vivaldi == true && WaybackMachine == true),

			// install icloud passwords extension
			("Installing iCloud Passwords Extension", async () => await InstallChromiumExtension(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Vivaldi\ExtensionInstallForcelist", "pejdijmoenmkgeppbflobdenhhabjlaj"), () => Vivaldi == true && iCloud == true),

			// install bitwarden extension
			("Installing Bitwarden Extension", async () => await InstallChromiumExtension(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Vivaldi\ExtensionInstallForcelist", "nngceckbapebfimnlniiiahkandclblb"), () => Vivaldi == true && Bitwarden == true),

			// install 1password extension
			("Installing 1Password Extension", async () => await InstallChromiumExtension(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Vivaldi\ExtensionInstallForcelist", "aeblfdkhhhdcdjpifhhbdiojplfjncoa"), () => Vivaldi == true && OnePassword == true),

			// remove vivaldi shortcut from the desktop
			("Removing Vivaldi shortcut from the desktop", async () => File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory), "Vivaldi.lnk")), () => Vivaldi == true),

			// disable vivaldi services
			("Disabling Vivaldi services", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{9C142C0C-124C-4467-B117-EBCC62801D7B}", "", "Vivaldi", RegistryValueKind.String), () => Vivaldi == true),
			("Disabling Vivaldi services", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{9C142C0C-124C-4467-B117-EBCC62801D7B}", "Localized Name", "Vivaldi", RegistryValueKind.String), () => Vivaldi == true),
			("Disabling Vivaldi services", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, $@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{{9C142C0C-124C-4467-B117-EBCC62801D7B}}", "StubPath", $@"""{Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Vivaldi", "Application", vivaldiVersion, "Installer", "chrmstp.exe")}"" --configure-user-settings --verbose-logging --system-level --vivaldi-install-dir=""{Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)}\Vivaldi\""", RegistryValueKind.String), () => Vivaldi == true),
			("Disabling Vivaldi services", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{9C142C0C-124C-4467-B117-EBCC62801D7B}", "Version", "43,0,0,0", RegistryValueKind.String), () => Vivaldi == true),
			("Disabling Vivaldi services", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{9C142C0C-124C-4467-B117-EBCC62801D7B}", "IsInstalled", 1, RegistryValueKind.DWord), () => Vivaldi == true),
			("Disabling Vivaldi services", async () => RegistryHelper.DeleteKey(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\{9C142C0C-124C-4467-B117-EBCC62801D7B}"), () => Vivaldi == true),

			// download arc dependency
			("Downloading Arc Dependency", async () => await DownloadHelper.Download("https://releases.arc.net/windows/dependencies/x64/Microsoft.VCLibs.x64.14.00.Desktop.14.0.33728.0.appx", Path.GetTempPath(), "Microsoft.VCLibs.x64.14.00.Desktop.14.0.33728.0.appx", reporter ?? new InstallPageReporter()), () => Arc == true),

			// install arc dependency
			("Installing Arc Dependency", async () => await Process.Start(new ProcessStartInfo { FileName = "powershell", Arguments = @$"-Command ""Add-AppxPackage -Path {Path.Combine(Path.GetTempPath(), "Microsoft.VCLibs.x64.14.00.Desktop.14.0.33728.0.appx")}""", UseShellExecute = false, CreateNoWindow = true })!.WaitForExitAsync(), () => Arc == true),
			("Cleaning up Arc Dependency files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "Microsoft.VCLibs.x64.14.00.Desktop.14.0.33728.0.appx")), () => Arc == true),

			// download arc
			("Downloading Arc", async () => await DownloadHelper.Download("https://releases.arc.net/windows/prod/1.72.0.296/Arc.x64.msix", Path.GetTempPath(), "Arc.x64.msix", reporter ?? new InstallPageReporter()), () => Arc == true),

			// install arc
			("Installing Arc", async () => await new PackageManager().AddPackageAsync(new Uri(Path.Combine(Path.GetTempPath(), "Arc.x64.msix")), null, DeploymentOptions.None), () => Arc == true),
			("Installing Arc", async () => arcVersion = StoreHelper.GetVersion("TheBrowserCompany.Arc_ttt1ap7aakyb4"), () => Arc == true),
			("Cleaning up Arc files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "Arc.x64.msix")), () => Arc == true),

			// pin arc to the taskbar
			("Pinning Arc to the taskbar", async () => await ProcessActions.PinToTaskbar("UWA", "TheBrowserCompany.Arc_ttt1ap7aakyb4!Arc"), () => Arc == true),

			// install ublock origin extension
			("Installing uBlock Origin Extension", async () => await InstallChromiumExtension(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Chromium\ExtensionInstallForcelist", "cjpalhdlnbpafiamejdnhcphjbkeiagm"), () => Arc == true && uBlock == true),

			// install privacy badger extension
			("Installing Privacy Badger Extension", async () => await InstallChromiumExtension(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Chromium\ExtensionInstallForcelist", "pkehgijcmpdhfbdbbnkijodmdjhbjlgp"), () => Arc == true && PrivacyBadger == true),

			// install decentraleyes extension
			("Installing Decentraleyes Extension", async () => await InstallChromiumExtension(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Chromium\ExtensionInstallForcelist", "ldpochfccmkkmhdbclfhpagapcfdljkj"), () => Arc == true && Decentraleyes == true),

			// install i still don't care about cookies extension
			("Installing I still don't care about cookies Extension", async () => await InstallChromiumExtension(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Chromium\ExtensionInstallForcelist", "edibdbjcniadpccecjdfdjjppcpchdlm"), () => Arc == true && Cookies == true),

			// install violentmonkey extension
			("Installing Violentmonkey Extension", async () => await InstallChromiumExtension(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Chromium\ExtensionInstallForcelist", "jinjaccalgkegednnccohejagnlnfdag"), () => Arc == true && Violentmonkey == true),

			// install tampermonkey extension
			("Installing Tampermonkey Extension", async () => await InstallChromiumExtension(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Chromium\ExtensionInstallForcelist", "dhdgffkkebhmkfjojejmpbldmpobfkfo"), () => Arc == true && Tampermonkey == true),

			// install sponsorblock extension
			("Installing SponsorBlock Extension", async () => await InstallChromiumExtension(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Chromium\ExtensionInstallForcelist", "mnjggcdmjocbbbhaepdhchncahnbgone"), () => Arc == true && SponsorBlock == true),

			// install return youtube dislike extension
			("Installing Return YouTube Dislike Extension", async () => await InstallChromiumExtension(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Chromium\ExtensionInstallForcelist", "gebbhagfogifgggkldgodflihgfeippi"), () => Arc == true && ReturnYouTubeDislike == true),

			// install dark reader extension
			("Installing Dark Reader Extension", async () => await InstallChromiumExtension(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Chromium\ExtensionInstallForcelist", "eimadpbcbfnmbkopoojfekhnkhdbieeh"), () => Arc == true && DarkReader == true),
			
			// install shazam extension
			("Installing Shazam Extension", async () => await InstallChromiumExtension(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Chromium\ExtensionInstallForcelist", "mmioliijnhnoblpgimnlajmefafdfilb"), () => Arc == true && Shazam == true),

			// install wayback machine extension
			("Installing Wayback Machine Extension", async () => await InstallChromiumExtension(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Chromium\ExtensionInstallForcelist", "fpnmgdkabkmnadcjpehmlllkndpkmiak"), () => Arc == true && WaybackMachine == true),

			// install icloud passwords extension
			("Installing iCloud Passwords Extension", async () => await InstallChromiumExtension(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Chromium\ExtensionInstallForcelist", "pejdijmoenmkgeppbflobdenhhabjlaj"), () => Arc == true && iCloud == true),

			// install bitwarden extension
			("Installing Bitwarden Extension", async () => await InstallChromiumExtension(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Chromium\ExtensionInstallForcelist", "nngceckbapebfimnlniiiahkandclblb"), () => Arc == true && Bitwarden == true),

			// install 1password extension
			("Installing 1Password Extension", async () => await InstallChromiumExtension(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Chromium\ExtensionInstallForcelist", "aeblfdkhhhdcdjpifhhbdiojplfjncoa"), () => Arc == true && OnePassword == true),

			// download comet
			("Downloading Comet", async () => await DownloadHelper.Download("https://www.perplexity.ai/rest/browser/download?platform=win_x64&channel=stable", Path.GetTempPath(), "Comet.exe", reporter ?? new InstallPageReporter()), () => Comet == true),

			// install comet
			("Installing Comet", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Path.GetTempPath(), "Comet.exe"), Arguments = "-silent --do-not-launch-chrome --system-level", WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), () => Comet == true),
			("Installing Comet", async () => cometVersion = FileVersionInfo.GetVersionInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Perplexity", "Comet", "Application", "comet.exe")).ProductVersion, () => Comet == true),
			("Cleaning up Comet files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "Comet.exe")), () => Comet == true),

			// pin comet to the taskbar
			("Pinning Comet to the taskbar", async () => await ProcessActions.PinToTaskbar("Link", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Microsoft", "Windows", "Start Menu", "Programs", "Comet.lnk")), () => Comet == true),

			// install ublock origin extension
			("Installing uBlock Origin Extension", async () => await InstallChromiumExtension(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Chromium\ExtensionInstallForcelist", "cjpalhdlnbpafiamejdnhcphjbkeiagm"), () => Comet == true && uBlock == true),

			// install privacy badger extension
			("Installing Privacy Badger Extension", async () => await InstallChromiumExtension(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Chromium\ExtensionInstallForcelist", "pkehgijcmpdhfbdbbnkijodmdjhbjlgp"), () => Comet == true && PrivacyBadger == true),

			// install decentraleyes extension
			("Installing Decentraleyes Extension", async () => await InstallChromiumExtension(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Chromium\ExtensionInstallForcelist", "ldpochfccmkkmhdbclfhpagapcfdljkj"), () => Comet == true && Decentraleyes == true),

			// install i still don't care about cookies extension
			("Installing I still don't care about cookies Extension", async () => await InstallChromiumExtension(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Chromium\ExtensionInstallForcelist", "edibdbjcniadpccecjdfdjjppcpchdlm"), () => Comet == true && Cookies == true),

			// install violentmonkey extension
			("Installing Violentmonkey Extension", async () => await InstallChromiumExtension(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Chromium\ExtensionInstallForcelist", "jinjaccalgkegednnccohejagnlnfdag"), () => Comet == true && Violentmonkey == true),

			// install tampermonkey extension
			("Installing Tampermonkey Extension", async () => await InstallChromiumExtension(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Chromium\ExtensionInstallForcelist", "dhdgffkkebhmkfjojejmpbldmpobfkfo"), () => Comet == true && Tampermonkey == true),

			// install sponsorblock extension
			("Installing SponsorBlock Extension", async () => await InstallChromiumExtension(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Chromium\ExtensionInstallForcelist", "mnjggcdmjocbbbhaepdhchncahnbgone"), () => Comet == true && SponsorBlock == true),

			// install return youtube dislike extension
			("Installing Return YouTube Dislike Extension", async () => await InstallChromiumExtension(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Chromium\ExtensionInstallForcelist", "gebbhagfogifgggkldgodflihgfeippi"), () => Comet == true && ReturnYouTubeDislike == true),

			// install dark reader extension
			("Installing Dark Reader Extension", async () => await InstallChromiumExtension(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Chromium\ExtensionInstallForcelist", "eimadpbcbfnmbkopoojfekhnkhdbieeh"), () => Comet == true && DarkReader == true),
			
			// install shazam extension
			("Installing Shazam Extension", async () => await InstallChromiumExtension(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Chromium\ExtensionInstallForcelist", "mmioliijnhnoblpgimnlajmefafdfilb"), () => Comet == true && Shazam == true),

			// install wayback machine extension
			("Installing Wayback Machine Extension", async () => await InstallChromiumExtension(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Chromium\ExtensionInstallForcelist", "fpnmgdkabkmnadcjpehmlllkndpkmiak"), () => Comet == true && WaybackMachine == true),

			// install icloud passwords extension
			("Installing iCloud Passwords Extension", async () => await InstallChromiumExtension(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Chromium\ExtensionInstallForcelist", "pejdijmoenmkgeppbflobdenhhabjlaj"), () => Comet == true && iCloud == true),

			// install bitwarden extension
			("Installing Bitwarden Extension", async () => await InstallChromiumExtension(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Chromium\ExtensionInstallForcelist", "nngceckbapebfimnlniiiahkandclblb"), () => Comet == true && Bitwarden == true),

			// install 1password extension
			("Installing 1Password Extension", async () => await InstallChromiumExtension(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Chromium\ExtensionInstallForcelist", "aeblfdkhhhdcdjpifhhbdiojplfjncoa"), () => Comet == true && OnePassword == true),

			// remove comet shortcut from the desktop
			("Removing Comet shortcut from the desktop", async () => File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory), "Comet.lnk")), () => Comet == true),

			// disable comet services
			("Disabling Comet services", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{1F7C13D9-45E8-47E9-A2B5-6B2EF21B91F4}", "", "Comet", RegistryValueKind.String), () => Comet == true),
			("Disabling Comet services", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{1F7C13D9-45E8-47E9-A2B5-6B2EF21B91F4}", "Localized Name", "Comet", RegistryValueKind.String), () => Comet == true),
			("Disabling Comet services", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, $@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{{1F7C13D9-45E8-47E9-A2B5-6B2EF21B91F4}}", "StubPath", $@"""{Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Perplexity", "Comet", "Application", cometVersion, "Installer", "chrmstp.exe")}"" --configure-user-settings --verbose-logging --system-level", RegistryValueKind.String), () => Comet == true),
			("Disabling Comet services", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{1F7C13D9-45E8-47E9-A2B5-6B2EF21B91F4}", "Version", "43,0,0,0", RegistryValueKind.String), () => Comet == true),
			("Disabling Comet services", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{1F7C13D9-45E8-47E9-A2B5-6B2EF21B91F4}", "IsInstalled", 1, RegistryValueKind.DWord), () => Comet == true),
			("Disabling Comet services", async () => RegistryHelper.DeleteKey(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\{1F7C13D9-45E8-47E9-A2B5-6B2EF21B91F4}"), () => Comet == true),
			("Disabling Comet services", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\CometElevationService", "Start", 4, RegistryValueKind.DWord), () => Comet == true),
			("Disabling Comet services", async () => ServicesHelper.StopService("CometElevationService"), () => Comet == true),
			("Disabling Comet services", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, $@"HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\CometUpdaterInternalService{cometVersion}", "Start", 4, RegistryValueKind.DWord), () => Comet == true),
			("Disabling Comet services", async () => ServicesHelper.StopService($@"CometUpdaterInternalService{cometVersion}"), () => Comet == true),
			("Disabling Comet services", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, $@"HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\CometUpdaterService{cometVersion}", "Start", 4, RegistryValueKind.DWord), () => Comet == true),
			("Disabling Comet services", async () => ServicesHelper.StopService($@"CometUpdaterService{cometVersion}"), () => Comet == true),
			("Disabling Comet services", async () => TaskSchedulerHelper.Toggle("CometUpdaterTaskSystem", false), () => Comet == true),

			// download firefox
			("Downloading Firefox", async () => firefoxVersion = JsonDocument.Parse(await ProcessActions.httpClient.GetStringAsync("https://product-details.mozilla.org/1.0/firefox_versions.json")).RootElement.GetProperty("LATEST_FIREFOX_VERSION").GetString(), () => Firefox == true),
			("Downloading Firefox", async () => await DownloadHelper.Download($"https://releases.mozilla.org/pub/firefox/releases/{firefoxVersion}/win64/en-US/Firefox%20Setup%20{firefoxVersion}.exe", Path.GetTempPath(), "FirefoxSetup.exe", reporter ?? new InstallPageReporter()), () => Firefox == true),

			// install firefox
			("Installing Firefox", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Path.GetTempPath(), "FirefoxSetup.exe"), Arguments = "/S /MaintenanceService=false /DesktopShortcut=false /StartMenuShortcut=true", WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), () => Firefox == true),
			("Cleaning up Firefox files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "FirefoxSetup.exe")), () => Firefox == true),

			// pin firefox to the taskbar
			("Pinning Firefox to the taskbar", async () => await ProcessActions.PinToTaskbar("Link", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Microsoft", "Windows", "Start Menu", "Programs", "Firefox.lnk")), () => Firefox == true),

			// disable firefox startup entry
			("Disabling Firefox startup entry", async () => TaskSchedulerHelper.Toggle(@"\Mozilla\Firefox Default Browser Agent", false), () => Firefox == true),
			("Disabling Firefox startup entry", async () => TaskSchedulerHelper.Toggle(@"\Mozilla\Firefox Background Update", false), () => Firefox == true),

			// optimize firefox settings
			("Optimizing Firefox settings", async () => Directory.CreateDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Mozilla Firefox", "distribution")), () => Firefox == true),
			("Optimizing Firefox settings", async () => File.WriteAllText(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Mozilla Firefox", "defaults", "pref", "autoconfig.js"), "pref(\"general.config.filename\", \"firefox.cfg\");\npref(\"general.config.obscure_value\", 0);"), () => Firefox == true),
			("Optimizing Firefox settings", async () => File.WriteAllText(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Mozilla Firefox", "firefox.cfg"), "defaultPref(\"app.shield.optoutstudies.enabled\", false);\ndefaultPref(\"browser.search.serpEventTelemetryCategorization.enabled\", false);\ndefaultPref(\"dom.security.unexpected_system_load_telemetry_enabled\", false);\ndefaultPref(\"identity.fxaccounts.telemetry.clientAssociationPing.enabled\", false);\ndefaultPref(\"network.trr.confirmation_telemetry_enabled\", false);\ndefaultPref(\"nimbus.telemetry.targetingContextEnabled\", false);\ndefaultPref(\"reader.parse-on-load.enabled\", false);\ndefaultPref(\"telemetry.fog.init_on_shutdown\", false);\ndefaultPref(\"default-browser-agent.enabled\", false);\ndefaultPref(\"widget.windows.mica\", true);\ndefaultPref(\"widget.windows.mica.popups\", 1);\ndefaultPref(\"widget.windows.mica.toplevel-backdrop\", 0);"), () => Firefox == true),
			("Optimizing Firefox settings", async () => File.WriteAllText(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Mozilla Firefox", "distribution", "policies.json"), "{\r\n  \"policies\": {}\r\n}"), () => Firefox == true),

			// install ublock origin extension
			("Installing uBlock Origin Extension", async () => UpdatePolicies(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Mozilla Firefox", "distribution", "policies.json"), "https://addons.mozilla.org/firefox/downloads/latest/ublock-origin"), () => Firefox == true && uBlock == true),

			// install privacy badger extension
			("Installing Privacy Badger Extension", async () => UpdatePolicies(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Mozilla Firefox", "distribution", "policies.json"), "https://addons.mozilla.org/firefox/downloads/latest/privacy-badger17"), () => Firefox == true && PrivacyBadger == true),

			// install decentraleyes extension
			("Installing Decentraleyes Extension", async () => UpdatePolicies(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Mozilla Firefox", "distribution", "policies.json"), "https://addons.mozilla.org/firefox/downloads/latest/decentraleyes"), () => Firefox == true && Decentraleyes == true),

			// install i still don't care about cookies extension
			("Installing I still don't care about cookies Extension", async () => UpdatePolicies(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Mozilla Firefox", "distribution", "policies.json"), "https://addons.mozilla.org/firefox/downloads/latest/istilldontcareaboutcookies"), () => Firefox == true && Cookies == true),

			// install violentmonkey extension
			("Installing Violentmonkey Extension", async () => UpdatePolicies(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Mozilla Firefox", "distribution", "policies.json"), "https://addons.mozilla.org/firefox/downloads/latest/violentmonkey"), () => Firefox == true && Violentmonkey == true),

			// install tampermonkey extension
			("Installing Tampermonkey Extension", async () => UpdatePolicies(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Mozilla Firefox", "distribution", "policies.json"), "https://addons.mozilla.org/firefox/downloads/latest/tampermonkey"), () => Firefox == true && Tampermonkey == true),

			// install sponsorblock extension
			("Installing SponsorBlock Extension", async () => UpdatePolicies(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Mozilla Firefox", "distribution", "policies.json"), "https://addons.mozilla.org/firefox/downloads/latest/sponsorblock"), () => Firefox == true && SponsorBlock == true),

			// install return youtube dislike extension
			("Installing Return YouTube Dislike Extension", async () => UpdatePolicies(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Mozilla Firefox", "distribution", "policies.json"), "https://addons.mozilla.org/firefox/downloads/latest/return-youtube-dislikes"), () => Firefox == true && ReturnYouTubeDislike == true),

			// install youtube no translation extension
			("Installing YouTube No Translation Extension", async () => UpdatePolicies(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Mozilla Firefox", "distribution", "policies.json"), "https://addons.mozilla.org/firefox/downloads/latest/youtube-no-translation"), () => Firefox == true && YouTubeNoTranslation == true),

			// install dark reader extension
			("Installing Dark Reader Extension", async () => UpdatePolicies(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Mozilla Firefox", "distribution", "policies.json"), "https://addons.mozilla.org/firefox/downloads/latest/darkreader"), () => Firefox == true && DarkReader == true),

			// install wayback machine extension
			("Installing Wayback Machine Extension", async () => UpdatePolicies(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Mozilla Firefox", "distribution", "policies.json"), "https://addons.mozilla.org/firefox/downloads/latest/wayback-machine_new"), () => Firefox == true && WaybackMachine == true),

			// install icloud passwords extension
			("Installing iCloud Passwords Extension", async () => UpdatePolicies(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Mozilla Firefox", "distribution", "policies.json"), "https://addons.mozilla.org/firefox/downloads/latest/icloud-passwords"), () => Firefox == true && iCloud == true),

			// install bitwarden extension
			("Installing Bitwarden Extension", async () => UpdatePolicies(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Mozilla Firefox", "distribution", "policies.json"), "https://addons.mozilla.org/firefox/downloads/latest/bitwarden-password-manager"), () => Firefox == true && Bitwarden == true),

			// install 1password extension
			("Installing 1Password Extension", async () => UpdatePolicies(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Mozilla Firefox", "distribution", "policies.json"), "https://addons.mozilla.org/firefox/downloads/latest/1password-x-password-manager"), () => Firefox == true && OnePassword == true),

			// download zen
			("Downloading Zen", async () => await DownloadHelper.Download("https://github.com/zen-browser/desktop/releases/latest/download/zen.installer.exe", Path.GetTempPath(), "zen.installer.exe", reporter ?? new InstallPageReporter()), () => Zen == true),

			// install zen
			("Installing Zen", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Path.GetTempPath(), "zen.installer.exe"), Arguments = "/S /MaintenanceService=false /DesktopShortcut=false /StartMenuShortcut=true", WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), () => Zen == true),
			("Cleaning up Zen files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "zen.installer.exe")), () => Zen == true),

			// pin zen to the taskbar
			("Pinning Zen to the taskbar", async () => await ProcessActions.PinToTaskbar("Link", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Microsoft", "Windows", "Start Menu", "Programs", "Zen.lnk")), () => Zen == true),

			// disable zen startup entry
			("Disabling Zen startup entry", async () => TaskSchedulerHelper.Toggle(@"\Mozilla\Zen Default Browser Agent", false), () => Zen == true),
			("Disabling Zen startup entry", async () => TaskSchedulerHelper.Toggle(@"\Mozilla\Zen Background Update", false), () => Zen == true),

			// optimize zen settings
			("Optimizing Zen settings", async () => Directory.CreateDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Zen Browser", "distribution")), () => Zen == true),
			("Optimizing Zen settings", async () => File.WriteAllText(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Zen Browser", "defaults", "pref", "autoconfig.js"), "pref(\"general.config.filename\", \"zen.cfg\");\npref(\"general.config.obscure_value\", 0);"), () => Zen == true),
			("Optimizing Zen settings", async () => File.WriteAllText(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Zen Browser", "zen.cfg"), "defaultPref(\"app.shield.optoutstudies.enabled\", false);\ndefaultPref(\"browser.search.serpEventTelemetryCategorization.enabled\", false);\ndefaultPref(\"dom.security.unexpected_system_load_telemetry_enabled\", false);\ndefaultPref(\"identity.fxaccounts.telemetry.clientAssociationPing.enabled\", false);\ndefaultPref(\"network.trr.confirmation_telemetry_enabled\", false);\ndefaultPref(\"nimbus.telemetry.targetingContextEnabled\", false);\ndefaultPref(\"reader.parse-on-load.enabled\", false);\ndefaultPref(\"telemetry.fog.init_on_shutdown\", false);\ndefaultPref(\"default-browser-agent.enabled\", false);\ndefaultPref(\"zen.view.use-single-toolbar\", false);\ndefaultPref(\"zen.theme.accent-color\", \"#2c34fb\");\ndefaultPref(\"zen.urlbar.behavior\", \"float\");\ndefaultPref(\"zen.view.grey-out-inactive-windows\", false);\ndefaultPref(\"widget.windows.mica.popups\", 1);\ndefaultPref(\"widget.windows.mica.toplevel-backdrop\", 0);\ndefaultPref(\"browser.uiCustomization.state\", \"{\\\"placements\\\":{\\\"widget-overflow-fixed-list\\\":[],\\\"unified-extensions-area\\\":[],\\\"nav-bar\\\":[\\\"back-button\\\",\\\"forward-button\\\",\\\"stop-reload-button\\\",\\\"customizableui-special-spring1\\\",\\\"vertical-spacer\\\",\\\"urlbar-container\\\",\\\"customizableui-special-spring2\\\",\\\"zen-create-new-button\\\",\\\"unified-extensions-button\\\",\\\"reset-pbm-toolbar-button\\\"],\\\"toolbar-menubar\\\":[\\\"menubar-items\\\"],\\\"TabsToolbar\\\":[\\\"tabbrowser-tabs\\\"],\\\"vertical-tabs\\\":[],\\\"PersonalToolbar\\\":[\\\"import-button\\\",\\\"personal-bookmarks\\\"],\\\"zen-sidebar-top-buttons\\\":[\\\"zen-toggle-compact-mode\\\"],\\\"zen-sidebar-foot-buttons\\\":[\\\"preferences-button\\\",\\\"zen-workspaces-button\\\",\\\"downloads-button\\\"]},\\\"seen\\\":[\\\"developer-button\\\",\\\"screenshot-button\\\",\\\"reset-pbm-toolbar-button\\\"],\\\"dirtyAreaCache\\\":[\\\"nav-bar\\\",\\\"vertical-tabs\\\",\\\"zen-sidebar-foot-buttons\\\",\\\"PersonalToolbar\\\",\\\"toolbar-menubar\\\",\\\"TabsToolbar\\\",\\\"unified-extensions-area\\\",\\\"zen-sidebar-top-buttons\\\"],\\\"currentVersion\\\":24,\\\"newElementCount\\\":0}\"\n);"), () => Zen == true),
			("Optimizing Zen settings", async () => File.WriteAllText(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Zen Browser", "distribution", "policies.json"), "{\r\n  \"policies\": {}\r\n}"), () => Zen == true),

			// install ublock origin extension
			("Installing uBlock Origin Extension", async () => UpdatePolicies(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Zen Browser", "distribution", "policies.json"), "https://addons.mozilla.org/firefox/downloads/latest/ublock-origin"), () => Zen == true && uBlock == true),

			// install privacy badger extension
			("Installing Privacy Badger Extension", async () => UpdatePolicies(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Zen Browser", "distribution", "policies.json"), "https://addons.mozilla.org/firefox/downloads/latest/privacy-badger17"), () => Zen == true && PrivacyBadger == true),

			// install decentraleyes extension
			("Installing Decentraleyes Extension", async () => UpdatePolicies(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Zen Browser", "distribution", "policies.json"), "https://addons.mozilla.org/firefox/downloads/latest/decentraleyes"), () => Zen == true && Decentraleyes == true),

			// install i still don't care about cookies extension
			("Installing I still don't care about cookies Extension", async () => UpdatePolicies(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Zen Browser", "distribution", "policies.json"), "https://addons.mozilla.org/firefox/downloads/latest/istilldontcareaboutcookies"), () => Zen == true && Cookies == true),

			// install violentmonkey extension
			("Installing Violentmonkey Extension", async () => UpdatePolicies(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Zen Browser", "distribution", "policies.json"), "https://addons.mozilla.org/firefox/downloads/latest/violentmonkey"), () => Zen == true && Violentmonkey == true),

			// install tampermonkey extension
			("Installing Tampermonkey Extension", async () => UpdatePolicies(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Zen Browser", "distribution", "policies.json"), "https://addons.mozilla.org/firefox/downloads/latest/tampermonkey"), () => Zen == true && Tampermonkey == true),

			// install sponsorblock extension
			("Installing SponsorBlock Extension", async () => UpdatePolicies(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Zen Browser", "distribution", "policies.json"), "https://addons.mozilla.org/firefox/downloads/latest/sponsorblock"), () => Zen == true && SponsorBlock == true),

			// install return youtube dislike extension
			("Installing Return YouTube Dislike Extension", async () => UpdatePolicies(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Zen Browser", "distribution", "policies.json"), "https://addons.mozilla.org/firefox/downloads/latest/return-youtube-dislikes"), () => Zen == true && ReturnYouTubeDislike == true),

			// install youtube no translation extension
			("Installing YouTube No Translation Extension", async () => UpdatePolicies(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Zen Browser", "distribution", "policies.json"), "https://addons.mozilla.org/firefox/downloads/latest/youtube-no-translation"), () => Zen == true && YouTubeNoTranslation == true),

			// install dark reader extension
			("Installing Dark Reader Extension", async () => UpdatePolicies(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Zen Browser", "distribution", "policies.json"), "https://addons.mozilla.org/firefox/downloads/latest/darkreader"), () => Zen == true && DarkReader == true),

			// install wayback machine extension
			("Installing Wayback Machine Extension", async () => UpdatePolicies(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Zen Browser", "distribution", "policies.json"), "https://addons.mozilla.org/firefox/downloads/latest/wayback-machine_new"), () => Zen == true && WaybackMachine == true),

			// install icloud passwords extension
			("Installing iCloud Passwords Extension", async () => UpdatePolicies(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Zen Browser", "distribution", "policies.json"), "https://addons.mozilla.org/firefox/downloads/latest/icloud-passwords"), () => Zen == true && iCloud == true),

			// install bitwarden extension
			("Installing Bitwarden Extension", async () => UpdatePolicies(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Zen Browser", "distribution", "policies.json"), "https://addons.mozilla.org/firefox/downloads/latest/bitwarden-password-manager"), () => Zen == true && Bitwarden == true),

			// install 1password extension
			("Installing 1Password Extension", async () => UpdatePolicies(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Zen Browser", "distribution", "policies.json"), "https://addons.mozilla.org/firefox/downloads/latest/1password-x-password-manager"), () => Zen == true && OnePassword == true),

			// download waterfox
			("Downloading Waterfox", async () => await DownloadHelper.Download("https://cdn.waterfox.com/waterfox/releases/6.6.13/WINNT_x86_64/Waterfox%20Setup%206.6.13.exe", Path.GetTempPath(), "WaterfoxSetup.exe", reporter ?? new InstallPageReporter()), () => Waterfox == true),

			// install waterfox
			("Installing Waterfox", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Path.GetTempPath(), "WaterfoxSetup.exe"), Arguments = "/S /MaintenanceService=false /DesktopShortcut=false /StartMenuShortcut=true", WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), () => Waterfox == true),
			("Cleaning up Waterfox files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "WaterfoxSetup.exe")), () => Waterfox == true),

			// pin waterfox to the taskbar
			("Pinning Waterfox to the taskbar", async () => await ProcessActions.PinToTaskbar("Link", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Microsoft", "Windows", "Start Menu", "Programs", "Waterfox.lnk")), () => Waterfox == true),

			// disable waterfox startup entry
			("Disabling Waterfox startup entry", async () => TaskSchedulerHelper.Toggle(@"\BrowserWorks\Waterfox Default Browser Agent", false), () => Waterfox == true),
			//("Disabling Waterfox startup entry", async () => TaskSchedulerHelper.Toggle(@"\BrowserWorks\Waterfox Background Update", false), () => Waterfox == true),

			// optimize waterfox settings
			("Optimizing Waterfox settings", async () => Directory.CreateDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Waterfox", "distribution")), () => Waterfox == true),
			("Optimizing Waterfox settings", async () => File.WriteAllText(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Waterfox", "defaults", "pref", "autoconfig.js"), "pref(\"general.config.filename\", \"waterfox.cfg\");\npref(\"general.config.obscure_value\", 0);"), () => Waterfox == true),
			("Optimizing Waterfox settings", async () => File.WriteAllText(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Waterfox", "waterfox.cfg"), "defaultPref(\"app.shield.optoutstudies.enabled\", false);\ndefaultPref(\"browser.search.serpEventTelemetryCategorization.enabled\", false);\ndefaultPref(\"dom.security.unexpected_system_load_telemetry_enabled\", false);\ndefaultPref(\"identity.fxaccounts.telemetry.clientAssociationPing.enabled\", false);\ndefaultPref(\"network.trr.confirmation_telemetry_enabled\", false);\ndefaultPref(\"nimbus.telemetry.targetingContextEnabled\", false);\ndefaultPref(\"reader.parse-on-load.enabled\", false);\ndefaultPref(\"telemetry.fog.init_on_shutdown\", false);\ndefaultPref(\"default-browser-agent.enabled\", false);\ndefaultPref(\"widget.windows.mica\", true);\ndefaultPref(\"widget.windows.mica.popups\", 1);\ndefaultPref(\"widget.windows.mica.toplevel-backdrop\", 0);"), () => Waterfox == true),
			("Optimizing Waterfox settings", async () => File.WriteAllText(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Waterfox", "distribution", "policies.json"), "{\r\n  \"policies\": {}\r\n}"), () => Waterfox == true),

			// install ublock origin extension
			("Installing uBlock Origin Extension", async () => UpdatePolicies(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Waterfox", "distribution", "policies.json"), "https://addons.mozilla.org/firefox/downloads/latest/ublock-origin"), () => Waterfox == true && uBlock == true),

			// install privacy badger extension
			("Installing Privacy Badger Extension", async () => UpdatePolicies(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Waterfox", "distribution", "policies.json"), "https://addons.mozilla.org/firefox/downloads/latest/privacy-badger17"), () => Waterfox == true && PrivacyBadger == true),

			// install decentraleyes extension
			("Installing Decentraleyes Extension", async () => UpdatePolicies(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Waterfox", "distribution", "policies.json"), "https://addons.mozilla.org/firefox/downloads/latest/decentraleyes"), () => Waterfox == true && Decentraleyes == true),

			// install i still don't care about cookies extension
			("Installing I still don't care about cookies Extension", async () => UpdatePolicies(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Waterfox", "distribution", "policies.json"), "https://addons.mozilla.org/firefox/downloads/latest/istilldontcareaboutcookies"), () => Waterfox == true && Cookies == true),

			// install violentmonkey extension
			("Installing Violentmonkey Extension", async () => UpdatePolicies(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Waterfox", "distribution", "policies.json"), "https://addons.mozilla.org/firefox/downloads/latest/violentmonkey"), () => Waterfox == true && Violentmonkey == true),

			// install tampermonkey extension
			("Installing Tampermonkey Extension", async () => UpdatePolicies(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Waterfox", "distribution", "policies.json"), "https://addons.mozilla.org/firefox/downloads/latest/tampermonkey"), () => Waterfox == true && Tampermonkey == true),

			// install sponsorblock extension
			("Installing SponsorBlock Extension", async () => UpdatePolicies(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Waterfox", "distribution", "policies.json"), "https://addons.mozilla.org/firefox/downloads/latest/sponsorblock"), () => Waterfox == true && SponsorBlock == true),

			// install return youtube dislike extension
			("Installing Return YouTube Dislike Extension", async () => UpdatePolicies(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Waterfox", "distribution", "policies.json"), "https://addons.mozilla.org/firefox/downloads/latest/return-youtube-dislikes"), () => Waterfox == true && ReturnYouTubeDislike == true),

			// install youtube no translation extension
			("Installing YouTube No Translation Extension", async () => UpdatePolicies(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Waterfox", "distribution", "policies.json"), "https://addons.mozilla.org/firefox/downloads/latest/youtube-no-translation"), () => Waterfox == true && YouTubeNoTranslation == true),

			// install dark reader extension
			("Installing Dark Reader Extension", async () => UpdatePolicies(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Waterfox", "distribution", "policies.json"), "https://addons.mozilla.org/firefox/downloads/latest/darkreader"), () => Waterfox == true && DarkReader == true),

			// install wayback machine extension
			("Installing Wayback Machine Extension", async () => UpdatePolicies(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Waterfox", "distribution", "policies.json"), "https://addons.mozilla.org/firefox/downloads/latest/wayback-machine_new"), () => Waterfox == true && WaybackMachine == true),

			// install icloud passwords extension
			("Installing iCloud Passwords Extension", async () => UpdatePolicies(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Waterfox", "distribution", "policies.json"), "https://addons.mozilla.org/firefox/downloads/latest/icloud-passwords"), () => Waterfox == true && iCloud == true),

			// install bitwarden extension
			("Installing Bitwarden Extension", async () => UpdatePolicies(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Waterfox", "distribution", "policies.json"), "https://addons.mozilla.org/firefox/downloads/latest/bitwarden-password-manager"), () => Waterfox == true && Bitwarden == true),

			// install 1password extension
			("Installing 1Password Extension", async () => UpdatePolicies(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Waterfox", "distribution", "policies.json"), "https://addons.mozilla.org/firefox/downloads/latest/1password-x-password-manager"), () => Waterfox == true && OnePassword == true),
			
			// download librewolf
			("Downloading LibreWolf", async () => await DownloadHelper.Download(JsonDocument.Parse(await new HttpClient { DefaultRequestHeaders = { { "User-Agent", "AutoOS" } } }.GetStringAsync("https://codeberg.org/api/v1/repos/librewolf/bsys6/releases")).RootElement.EnumerateArray().First(release => release.GetProperty("assets").EnumerateArray().Any(asset => asset.GetProperty("name").GetString().Contains("windows-x86_64-setup.exe"))).GetProperty("assets").EnumerateArray().First(asset => asset.GetProperty("name").GetString().Contains("windows-x86_64-setup.exe")).GetProperty("browser_download_url").GetString(), Path.GetTempPath(), "librewolf-windows-x86_64-setup.exe", reporter: reporter), () => LibreWolf == true),
			
			// install librewolf
			("Installing LibreWolf", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Path.GetTempPath(), "librewolf-windows-x86_64-setup.exe"), Arguments = "/S /MaintenanceService=false /DesktopShortcut=false /StartMenuShortcut=true", WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), () => LibreWolf == true),
			("Cleaning up LibreWolf files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "librewolf-windows-x86_64-setup.exe")), () => LibreWolf == true),

			// optimize librewolf settings
			("Optimizing LibreWolf settings", async () => Directory.CreateDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "LibreWolf", "distribution")), () => LibreWolf == true),
			("Optimizing LibreWolf settings", async () => File.WriteAllText(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "LibreWolf", "defaults", "pref", "autoconfig.js"), "pref(\"general.config.filename\", \"librewolf.cfg\");\npref(\"general.config.obscure_value\", 0);"), () => LibreWolf == true),
			("Optimizing LibreWolf settings", async () => File.WriteAllText(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "LibreWolf", "librewolf.cfg"), "defaultPref(\"app.shield.optoutstudies.enabled\", false);\ndefaultPref(\"browser.search.serpEventTelemetryCategorization.enabled\", false);\ndefaultPref(\"dom.security.unexpected_system_load_telemetry_enabled\", false);\ndefaultPref(\"identity.fxaccounts.telemetry.clientAssociationPing.enabled\", false);\ndefaultPref(\"network.trr.confirmation_telemetry_enabled\", false);\ndefaultPref(\"nimbus.telemetry.targetingContextEnabled\", false);\ndefaultPref(\"reader.parse-on-load.enabled\", false);\ndefaultPref(\"telemetry.fog.init_on_shutdown\", false);\ndefaultPref(\"default-browser-agent.enabled\", false);\ndefaultPref(\"widget.windows.mica\", true);\ndefaultPref(\"widget.windows.mica.popups\", 1);\ndefaultPref(\"widget.windows.mica.toplevel-backdrop\", 0);"), () => LibreWolf == true),
			("Optimizing LibreWolf settings", async () => File.WriteAllText(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "LibreWolf", "distribution", "policies.json"), "{\r\n  \"policies\": {}\r\n}"), () => LibreWolf == true),

			// install ublock origin extension
			("Installing uBlock Origin Extension", async () => UpdatePolicies(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "LibreWolf", "distribution", "policies.json"), "https://addons.mozilla.org/firefox/downloads/latest/ublock-origin"), () => LibreWolf == true && uBlock == true),

			// install privacy badger extension
			("Installing Privacy Badger Extension", async () => UpdatePolicies(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "LibreWolf", "distribution", "policies.json"), "https://addons.mozilla.org/firefox/downloads/latest/privacy-badger17"), () => LibreWolf == true && PrivacyBadger == true),

			// install decentraleyes extension
			("Installing Decentraleyes Extension", async () => UpdatePolicies(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "LibreWolf", "distribution", "policies.json"), "https://addons.mozilla.org/firefox/downloads/latest/decentraleyes"), () => LibreWolf == true && Decentraleyes == true),

			// install i still don't care about cookies extension
			("Installing I still don't care about cookies Extension", async () => UpdatePolicies(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "LibreWolf", "distribution", "policies.json"), "https://addons.mozilla.org/firefox/downloads/latest/istilldontcareaboutcookies"), () => LibreWolf == true && Cookies == true),

			// install violentmonkey extension
			("Installing Violentmonkey Extension", async () => UpdatePolicies(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "LibreWolf", "distribution", "policies.json"), "https://addons.mozilla.org/firefox/downloads/latest/violentmonkey"), () => LibreWolf == true && Violentmonkey == true),

			// install tampermonkey extension
			("Installing Tampermonkey Extension", async () => UpdatePolicies(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "LibreWolf", "distribution", "policies.json"), "https://addons.mozilla.org/firefox/downloads/latest/tampermonkey"), () => LibreWolf == true && Tampermonkey == true),

			// install sponsorblock extension
			("Installing SponsorBlock Extension", async () => UpdatePolicies(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "LibreWolf", "distribution", "policies.json"), "https://addons.mozilla.org/firefox/downloads/latest/sponsorblock"), () => LibreWolf == true && SponsorBlock == true),

			// install return youtube dislike extension
			("Installing Return YouTube Dislike Extension", async () => UpdatePolicies(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "LibreWolf", "distribution", "policies.json"), "https://addons.mozilla.org/firefox/downloads/latest/return-youtube-dislikes"), () => LibreWolf == true && ReturnYouTubeDislike == true),

			// install youtube no translation extension
			("Installing YouTube No Translation Extension", async () => UpdatePolicies(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "LibreWolf", "distribution", "policies.json"), "https://addons.mozilla.org/firefox/downloads/latest/youtube-no-translation"), () => LibreWolf == true && YouTubeNoTranslation == true),

			// install dark reader extension
			("Installing Dark Reader Extension", async () => UpdatePolicies(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "LibreWolf", "distribution", "policies.json"), "https://addons.mozilla.org/firefox/downloads/latest/darkreader"), () => LibreWolf == true && DarkReader == true),

			// install wayback machine extension
			("Installing Wayback Machine Extension", async () => UpdatePolicies(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "LibreWolf", "distribution", "policies.json"), "https://addons.mozilla.org/firefox/downloads/latest/wayback-machine_new"), () => LibreWolf == true && WaybackMachine == true),

			// install icloud passwords extension
			("Installing iCloud Passwords Extension", async () => UpdatePolicies(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "LibreWolf", "distribution", "policies.json"), "https://addons.mozilla.org/firefox/downloads/latest/icloud-passwords"), () => LibreWolf == true && iCloud == true),

			// install bitwarden extension
			("Installing Bitwarden Extension", async () => UpdatePolicies(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "LibreWolf", "distribution", "policies.json"), "https://addons.mozilla.org/firefox/downloads/latest/bitwarden-password-manager"), () => LibreWolf == true && Bitwarden == true),

			// install 1password extension
			("Installing 1Password Extension", async () => UpdatePolicies(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "LibreWolf", "distribution", "policies.json"), "https://addons.mozilla.org/firefox/downloads/latest/1password-x-password-manager"), () => LibreWolf == true && OnePassword == true),

			// download floorp
			("Downloading Floorp", async () => await DownloadHelper.Download("https://github.com/Floorp-Projects/Floorp/releases/latest/download/floorp-windows-x86_64.installer.exe", Path.GetTempPath(), "FloorpSetup.exe", reporter ?? new InstallPageReporter()), () => Floorp == true),

			// install floorp
			("Installing Floorp", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Path.GetTempPath(), "FloorpSetup.exe"), Arguments = "/S /MaintenanceService=false /DesktopShortcut=false /StartMenuShortcut=true", WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), () => Floorp == true),
			("Cleaning up Floorp files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "FloorpSetup.exe")), () => Floorp == true),

			// pin floorp to the taskbar
			("Pinning Floorp to the taskbar", async () => await ProcessActions.PinToTaskbar("Link", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Microsoft", "Windows", "Start Menu", "Programs", "Floorp.lnk")), () => Floorp == true),

			// disable floorp startup entry
			("Disabling Floorp startup entry", async () => TaskSchedulerHelper.Toggle(@"\Floorp\Floorp Default Browser Agent", false), () => Floorp == true),

			// optimize floorp settings
			("Optimizing Floorp settings", async () => Directory.CreateDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Ablaze Floorp", "distribution")), () => Floorp == true),
			("Optimizing Floorp settings", async () => Directory.CreateDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Ablaze Floorp", "defaults", "pref")), () => Floorp == true),
			("Optimizing Floorp settings", async () => File.WriteAllText(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Ablaze Floorp", "defaults", "pref", "autoconfig.js"), "pref(\"general.config.filename\", \"floorp.cfg\");\npref(\"general.config.obscure_value\", 0);"), () => Floorp == true),
			("Optimizing Floorp settings", async () => File.WriteAllText(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Ablaze Floorp", "distribution", "floorp.cfg"), "// Disable telemetry\r\npref(\"datareporting.healthreport.uploadEnabled\", false);\r\npref(\"datareporting.policy.dataSubmissionEnabled\", false);\r\npref(\"toolkit.telemetry.enabled\", false);\r\npref(\"toolkit.telemetry.unified\", false);\r\npref(\"toolkit.telemetry.archive.enabled\", false);\r\npref(\"browser.pingCentre.telemetry\", false);\r\npref(\"dom.privateattribution.sendReport\", false);"), () => Floorp == true),
			("Optimizing Floorp settings", async () => File.WriteAllText(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Ablaze Floorp", "distribution", "policies.json"), "{\r\n  \"policies\": {}\r\n}"), () => Floorp == true),

			// install ublock origin extension
			("Installing uBlock Origin Extension", async () => UpdatePolicies(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Ablaze Floorp", "distribution", "policies.json"), "https://addons.mozilla.org/firefox/downloads/latest/ublock-origin"), () => Floorp == true && uBlock == true),

			// install privacy badger extension
			("Installing Privacy Badger Extension", async () => UpdatePolicies(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Ablaze Floorp", "distribution", "policies.json"), "https://addons.mozilla.org/firefox/downloads/latest/privacy-badger17"), () => Floorp == true && PrivacyBadger == true),

			// install decentraleyes extension
			("Installing Decentraleyes Extension", async () => UpdatePolicies(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Ablaze Floorp", "distribution", "policies.json"), "https://addons.mozilla.org/firefox/downloads/latest/decentraleyes"), () => Floorp == true && Decentraleyes == true),

			// install i still don't care about cookies extension
			("Installing I still don't care about cookies Extension", async () => UpdatePolicies(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Ablaze Floorp", "distribution", "policies.json"), "https://addons.mozilla.org/firefox/downloads/latest/istilldontcareaboutcookies"), () => Floorp == true && Cookies == true),

			// install violentmonkey extension
			("Installing Violentmonkey Extension", async () => UpdatePolicies(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Ablaze Floorp", "distribution", "policies.json"), "https://addons.mozilla.org/firefox/downloads/latest/violentmonkey"), () => Floorp == true && Violentmonkey == true),

			// install tampermonkey extension
			("Installing Tampermonkey Extension", async () => UpdatePolicies(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Ablaze Floorp", "distribution", "policies.json"), "https://addons.mozilla.org/firefox/downloads/latest/tampermonkey"), () => Floorp == true && Tampermonkey == true),

			// install sponsorblock extension
			("Installing SponsorBlock Extension", async () => UpdatePolicies(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Ablaze Floorp", "distribution", "policies.json"), "https://addons.mozilla.org/firefox/downloads/latest/sponsorblock"), () => Floorp == true && SponsorBlock == true),

			// install return youtube dislike extension
			("Installing Return YouTube Dislike Extension", async () => UpdatePolicies(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Ablaze Floorp", "distribution", "policies.json"), "https://addons.mozilla.org/firefox/downloads/latest/return-youtube-dislikes"), () => Floorp == true && ReturnYouTubeDislike == true),

			// install youtube no translation extension
			("Installing YouTube No Translation Extension", async () => UpdatePolicies(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Ablaze Floorp", "distribution", "policies.json"), "https://addons.mozilla.org/firefox/downloads/latest/youtube-no-translation"), () => Floorp == true && YouTubeNoTranslation == true),

			// install dark reader extension
			("Installing Dark Reader Extension", async () => UpdatePolicies(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Ablaze Floorp", "distribution", "policies.json"), "https://addons.mozilla.org/firefox/downloads/latest/darkreader"), () => Floorp == true && DarkReader == true),

			// install wayback machine extension
			("Installing Wayback Machine Extension", async () => UpdatePolicies(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Ablaze Floorp", "distribution", "policies.json"), "https://addons.mozilla.org/firefox/downloads/latest/wayback-machine_new"), () => Floorp == true && WaybackMachine == true),

			// install icloud passwords extension
			("Installing iCloud Passwords Extension", async () => UpdatePolicies(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Ablaze Floorp", "distribution", "policies.json"), "https://addons.mozilla.org/firefox/downloads/latest/icloud-passwords"), () => Floorp == true && iCloud == true),

			// install bitwarden extension
			("Installing Bitwarden Extension", async () => UpdatePolicies(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Ablaze Floorp", "distribution", "policies.json"), "https://addons.mozilla.org/firefox/downloads/latest/bitwarden-password-manager"), () => Floorp == true && Bitwarden == true),

			// install 1password extension
			("Installing 1Password Extension", async () => UpdatePolicies(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Ablaze Floorp", "distribution", "policies.json"), "https://addons.mozilla.org/firefox/downloads/latest/1password-x-password-manager"), () => Floorp == true && OnePassword == true),

			// download mullvad
			("Downloading Mullvad Browser", async () => await DownloadHelper.Download("https://mullvad.net/en/download/browser/win64/latest", Path.GetTempPath(), "mullvad-browser-windows-x86_64.exe", reporter ?? new InstallPageReporter()), () => Mullvad == true),

			// install mullvad
			("Installing Mullvad Browser", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Path.GetTempPath(), "mullvad-browser-windows-x86_64.exe"), Arguments = "/S /MaintenanceService=false /DesktopShortcut=false /StartMenuShortcut=true", WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), () => Mullvad == true),
			("Cleaning up Mullvad Browser files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "mullvad-browser-windows-x86_64.exe")), () => Mullvad == true),

			// optimize mullvad settings
			("Optimizing Mullvad Browser settings", async () => Directory.CreateDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Mullvad", "MullvadBrowser", "Release", "distribution")), () => Mullvad == true),
			("Optimizing Mullvad Browser settings", async () => File.WriteAllText(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Mullvad", "MullvadBrowser", "Release", "defaults", "pref", "autoconfig.js"), "pref(\"general.config.filename\", \"mullvad.cfg\");\npref(\"general.config.obscure_value\", 0);"), () => Mullvad == true),
			("Optimizing Mullvad Browser settings", async () => File.WriteAllText(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Mullvad", "MullvadBrowser", "Release", "mullvad.cfg"), "defaultPref(\"app.shield.optoutstudies.enabled\", false);\ndefaultPref(\"browser.search.serpEventTelemetryCategorization.enabled\", false);\ndefaultPref(\"dom.security.unexpected_system_load_telemetry_enabled\", false);\ndefaultPref(\"identity.fxaccounts.telemetry.clientAssociationPing.enabled\", false);\ndefaultPref(\"network.trr.confirmation_telemetry_enabled\", false);\ndefaultPref(\"nimbus.telemetry.targetingContextEnabled\", false);\ndefaultPref(\"reader.parse-on-load.enabled\", false);\ndefaultPref(\"telemetry.fog.init_on_shutdown\", false);\ndefaultPref(\"default-browser-agent.enabled\", false);\ndefaultPref(\"widget.windows.mica\", true);\ndefaultPref(\"widget.windows.mica.popups\", 1);\ndefaultPref(\"widget.windows.mica.toplevel-backdrop\", 0);"), () => Mullvad == true),
			("Optimizing Mullvad Browser settings", async () => File.WriteAllText(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Mullvad", "MullvadBrowser", "Release", "distribution", "policies.json"), "{\r\n  \"policies\": {}\r\n}"), () => Mullvad == true),

			// remove mullvad desktop shortcut
			("Removing Mullvad Browser desktop shortcut", async () => File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Mullvad Browser.lnk")), () => Mullvad == true),

			// install ublock origin extension
			("Installing uBlock Origin Extension", async () => UpdatePolicies(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Mullvad", "MullvadBrowser", "Release", "distribution", "policies.json"), "https://addons.mozilla.org/firefox/downloads/latest/ublock-origin"), () => Mullvad == true && uBlock == true),

			// install privacy badger extension
			("Installing Privacy Badger Extension", async () => UpdatePolicies(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Mullvad", "MullvadBrowser", "Release", "distribution", "policies.json"), "https://addons.mozilla.org/firefox/downloads/latest/privacy-badger17"), () => Mullvad == true && PrivacyBadger == true),

			// install decentraleyes extension
			("Installing Decentraleyes Extension", async () => UpdatePolicies(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Mullvad", "MullvadBrowser", "Release", "distribution", "policies.json"), "https://addons.mozilla.org/firefox/downloads/latest/decentraleyes"), () => Mullvad == true && Decentraleyes == true),

			// install i still don't care about cookies extension
			("Installing I still don't care about cookies Extension", async () => UpdatePolicies(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Mullvad", "MullvadBrowser", "Release", "distribution", "policies.json"), "https://addons.mozilla.org/firefox/downloads/latest/istilldontcareaboutcookies"), () => Mullvad == true && Cookies == true),

			// install violentmonkey extension
			("Installing Violentmonkey Extension", async () => UpdatePolicies(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Mullvad", "MullvadBrowser", "Release", "distribution", "policies.json"), "https://addons.mozilla.org/firefox/downloads/latest/violentmonkey"), () => Mullvad == true && Violentmonkey == true),

			// install tampermonkey extension
			("Installing Tampermonkey Extension", async () => UpdatePolicies(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Mullvad", "MullvadBrowser", "Release", "distribution", "policies.json"), "https://addons.mozilla.org/firefox/downloads/latest/tampermonkey"), () => Mullvad == true && Tampermonkey == true),

			// install sponsorblock extension
			("Installing SponsorBlock Extension", async () => UpdatePolicies(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Mullvad", "MullvadBrowser", "Release", "distribution", "policies.json"), "https://addons.mozilla.org/firefox/downloads/latest/sponsorblock"), () => Mullvad == true && SponsorBlock == true),

			// install return youtube dislike extension
			("Installing Return YouTube Dislike Extension", async () => UpdatePolicies(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Mullvad", "MullvadBrowser", "Release", "distribution", "policies.json"), "https://addons.mozilla.org/firefox/downloads/latest/return-youtube-dislikes"), () => Mullvad == true && ReturnYouTubeDislike == true),

			// install youtube no translation extension
			("Installing YouTube No Translation Extension", async () => UpdatePolicies(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Mullvad", "MullvadBrowser", "Release", "distribution", "policies.json"), "https://addons.mozilla.org/firefox/downloads/latest/youtube-no-translation"), () => Mullvad == true && YouTubeNoTranslation == true),

			// install dark reader extension
			("Installing Dark Reader Extension", async () => UpdatePolicies(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Mullvad", "MullvadBrowser", "Release", "distribution", "policies.json"), "https://addons.mozilla.org/firefox/downloads/latest/darkreader"), () => Mullvad == true && DarkReader == true),

			// install wayback machine extension
			("Installing Wayback Machine Extension", async () => UpdatePolicies(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Mullvad", "MullvadBrowser", "Release", "distribution", "policies.json"), "https://addons.mozilla.org/firefox/downloads/latest/wayback-machine_new"), () => Mullvad == true && WaybackMachine == true),

			// install icloud passwords extension
			("Installing iCloud Passwords Extension", async () => UpdatePolicies(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Mullvad", "MullvadBrowser", "Release", "distribution", "policies.json"), "https://addons.mozilla.org/firefox/downloads/latest/icloud-passwords"), () => Mullvad == true && iCloud == true),

			// install bitwarden extension
			("Installing Bitwarden Extension", async () => UpdatePolicies(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Mullvad", "MullvadBrowser", "Release", "distribution", "policies.json"), "https://addons.mozilla.org/firefox/downloads/latest/bitwarden-password-manager"), () => Mullvad == true && Bitwarden == true),

			// install 1password extension
			("Installing 1Password Extension", async () => UpdatePolicies(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Mullvad", "MullvadBrowser", "Release", "distribution", "policies.json"), "https://addons.mozilla.org/firefox/downloads/latest/1password-x-password-manager"), () => Mullvad == true && OnePassword == true),
		};

		if (selection != null)
		{
			return actions.Where(action => action.Condition != null && action.Condition.Invoke()).ToList();
		}

		return actions;
	}

	private static void UpdatePolicies(string policiesPath, string extensionUrl)
	{
		var json = File.ReadAllText(policiesPath);
		var root = JsonNode.Parse(json)?.AsObject();
		if (root != null && root.TryGetPropertyValue("policies", out var policiesNode))
		{
			var policies = policiesNode?.AsObject() ?? new JsonObject();
			JsonNode extensionNode = JsonValue.Create(extensionUrl)!;
			if (!policies.ContainsKey("Extensions"))
			{
				policies["Extensions"] = new JsonObject { ["Install"] = new JsonArray(extensionNode) };
			}
			else
			{
				var extensions = policies["Extensions"]?.AsObject();
				if (extensions != null)
				{
					if (!extensions.TryGetPropertyValue("Install", out var installNode))
					{
						extensions["Install"] = new JsonArray(JsonValue.Create(extensionUrl));
					}
					else
					{
						var installArray = installNode?.AsArray();
						if (installArray != null)
						{
							bool alreadyExists = false;
							foreach (var item in installArray)
							{
								if (item?.ToString() == extensionUrl)
								{
									alreadyExists = true;
									break;
								}
							}
							if (!alreadyExists)
							{
								installArray.Add(extensionNode);
							}
						}
					}
				}
			}
			root["policies"] = policies;
			File.WriteAllText(policiesPath, root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
		}
	}

	private static Task InstallChromiumExtension(string baseKey, string extensionId)
	{
		string[] valueNames = RegistryHelper.GetValueNames(RegistryHelper.Identity.TrustedInstaller, baseKey);
		int nextIndex = 0;

		if (valueNames.Length > 0)
		{
			var indices = valueNames
				.Select(n => int.TryParse(n, out int i) ? (int?)i : null)
				.Where(i => i.HasValue)
				.Select(i => i.Value)
				.ToList();

			if (indices.Count > 0)
			{
				nextIndex = indices.Max() + 1;
			}
		}

		RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, baseKey, nextIndex.ToString(), extensionId, RegistryValueKind.String);
		return Task.CompletedTask;
	}
}

