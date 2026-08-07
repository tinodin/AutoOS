using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using AutoOS.App.Common;
using AutoOS.Core.Common;
using AutoOS.Core.Helpers.Database;
using AutoOS.Core.Helpers.Download;
using AutoOS.Core.Helpers.Extract;
using AutoOS.Core.Helpers.Games;
using AutoOS.Core.Helpers.Processes;
using AutoOS.Core.Helpers.Registry;
using AutoOS.Core.Helpers.Services;
using AutoOS.Core.Helpers.Shortcut;
using AutoOS.Core.Helpers.Store;
using AutoOS.Core.Helpers.TaskScheduler;
using AutoOS.App.Views.Installer.Actions;
using Microsoft.VisualBasic.FileIO;
using Microsoft.Win32;
using Windows.Win32;
using Windows.Win32.Foundation;

namespace AutoOS.App.Views.Installer.Stages;

public class ApplicationSelection
{
	public bool iCloud { get; set; }
	public bool Bitwarden { get; set; }
	public bool OnePassword { get; set; }
	public bool Discord { get; set; }
	public bool DiscordAccount { get; set; }
	public bool DiscordKeybinds { get; set; }
	public bool WhatsApp { get; set; }
	public bool Telegram { get; set; }
	public bool Unigram { get; set; }
	public bool ZoomWorkplace { get; set; }
	public bool Thunderbird { get; set; }
	public bool Signal { get; set; }
	public bool EpicGames { get; set; }
	public bool EpicGamesAccount { get; set; }
	public bool EpicGamesGames { get; set; }
	public bool Steam { get; set; }
	public bool SteamGames { get; set; }
	public bool RiotClient { get; set; }
	public bool RiotClientAccount { get; set; }
	public bool RiotClientGames { get; set; }
	public bool UbisoftConnect { get; set; }
	public bool EA { get; set; }
	public bool BattleNet { get; set; }
	public bool MinecraftLauncher { get; set; }
	public bool CurseForge { get; set; }
	public bool LunarClient { get; set; }
	public bool FeatherClient { get; set; }
	public bool NoRiskClient { get; set; }
	public bool PrismLauncher { get; set; }
	public bool Bloxstrap { get; set; }
	public bool Froststrap { get; set; }
	public bool RockstarGamesLauncher { get; set; }
	public bool FiveM { get; set; }
	public bool FACEIT { get; set; }
	public bool FACEITAC { get; set; }
	public bool Eden { get; set; }
	public bool AppleMusic { get; set; }
	public bool Tidal { get; set; }
	public bool Qobuz { get; set; }
	public bool AmazonMusic { get; set; }
	public bool DeezerMusic { get; set; }
	public bool Spotify { get; set; }
	public bool MusicBee { get; set; }
	public bool LogitechGHub { get; set; }
	public bool LogitechOnboardMemoryManager { get; set; }
	public bool Wootility { get; set; }
	public bool GMenu { get; set; }
	public bool EndgameGear { get; set; }
	public bool GloriousCORE { get; set; }
	public bool MCHOSE { get; set; }
	public bool SteelSeriesGG { get; set; }
	public bool RazerSynapse { get; set; }
	public bool CorsairICue { get; set; }
	public bool OpenRGB { get; set; }
	public bool FanControl { get; set; }
	public bool GHelper { get; set; }
	public bool ViGEmBus { get; set; }
	public bool HidHide { get; set; }
	public bool DualSenseY { get; set; }
	public bool RaceElement { get; set; }
	public bool PlaystationAccessories { get; set; }
	public bool XboxAccessories { get; set; }
	public bool VisualStudio { get; set; }
	public bool VisualStudioCode { get; set; }
	public bool Antigravity { get; set; }
	public bool Cursor { get; set; }
	public bool Devin { get; set; }
	public bool Kiro { get; set; }
	public bool OpenCode { get; set; }
	public bool SublimeText { get; set; }
	public bool IDEA { get; set; }
	public bool WinMerge { get; set; }
	public bool Git { get; set; }
	public bool CMake { get; set; }
	public bool Python { get; set; }
	public bool Nodejs { get; set; }
	public bool Rust { get; set; }
	public bool Java { get; set; }
	public bool Go { get; set; }
	public bool Trello { get; set; }
	public bool Autoruns { get; set; }
	public bool ProcessExplorer { get; set; }
	public bool ProcessMonitor { get; set; }
	public bool HWInfo { get; set; }
	public bool TimingConfigurator { get; set; }
	public bool ZenTimings { get; set; }
	public bool RamTestPro { get; set; }
	public bool TestMem5 { get; set; }
	public bool Prime95 { get; set; }
	public bool yCruncher { get; set; }
	public bool OCCT { get; set; }
	public bool AIDA64Extreme { get; set; }
	public bool MemtestVulkan { get; set; }
	public bool Reaper { get; set; }
	public bool FLStudio { get; set; }
	public bool Audacity { get; set; }
	public bool FlexASIO { get; set; }
	public bool ASIO4ALL { get; set; }
	public bool ArturiaMidiControlCenter { get; set; }
	public bool Voicemeeter { get; set; }
	public bool DaVinciResolve { get; set; }
	public bool Blender { get; set; }
	public bool CapCut { get; set; }
	public bool LosslessCut { get; set; }
	public bool Netflix { get; set; }
	public bool DisneyPlus { get; set; }
	public bool PrimeVideo { get; set; }
	public bool MpcQt { get; set; }
	public bool MPV { get; set; }
	public bool VLC { get; set; }
	public bool MediaInfo { get; set; }
	public bool Word { get; set; }
	public bool Excel { get; set; }
	public bool PowerPoint { get; set; }
	public bool OneNote { get; set; }
	public bool Teams { get; set; }
	public bool Outlook { get; set; }
	public bool OneDrive { get; set; }
	public bool CapFrameX { get; set; }
	public bool MinitoolPartitionWizard { get; set; }
	public bool AomeiPartitionAssistant { get; set; }
	public bool WizTree { get; set; }
	public bool CrystalDiskInfo { get; set; }
	public bool CrystalDiskMark { get; set; }
	public bool ProtonVPN { get; set; }
	public bool BulkCrapUninstaller { get; set; }
	public bool BluetoothAudioReceiver { get; set; }
	public bool AnyDesk { get; set; }
	public bool RustDesk { get; set; }
	public bool Apollo { get; set; }
	public bool Moonlight { get; set; }
	public bool AutoHotkey { get; set; }
	public bool EmEditor { get; set; }
	public bool WinDbg { get; set; }
	public bool QBittorrent { get; set; }
	public bool Deluge { get; set; }
	public bool FreeDownloadManager { get; set; }
}

public static class AppsStage
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
		bool LeftTaskbarAlignment = selection != null ? true : PreparingStage.LeftTaskbarAlignment;

		bool Discord = selection?.Discord ?? PreparingStage.Discord;
		bool DiscordAccount = selection?.DiscordAccount ?? PreparingStage.DiscordAccount;
		bool DiscordKeybinds = selection?.DiscordKeybinds ?? PreparingStage.DiscordKeybinds;
		bool WhatsApp = selection?.WhatsApp ?? PreparingStage.WhatsApp;
		bool Telegram = selection?.Telegram ?? PreparingStage.Telegram;
		bool Unigram = selection?.Unigram ?? PreparingStage.Unigram;
		bool ZoomWorkplace = selection?.ZoomWorkplace ?? PreparingStage.ZoomWorkplace;
		bool Thunderbird = selection?.Thunderbird ?? PreparingStage.Thunderbird;
		bool Signal = selection?.Signal ?? PreparingStage.Signal;

		bool EpicGames = selection?.EpicGames ?? PreparingStage.EpicGames;
		bool EpicGamesAccount = selection?.EpicGamesAccount ?? PreparingStage.EpicGamesAccount;
		bool EpicGamesGames = selection?.EpicGamesGames ?? PreparingStage.EpicGamesGames;
		bool Steam = selection?.Steam ?? PreparingStage.Steam;
		bool SteamGames = selection?.SteamGames ?? PreparingStage.SteamGames;
		bool RiotClient = selection?.RiotClient ?? PreparingStage.RiotClient;
		bool RiotClientAccount = selection?.RiotClientAccount ?? PreparingStage.RiotClientAccount;
		bool RiotClientGames = selection?.RiotClientGames ?? PreparingStage.RiotClientGames;
		bool UbisoftConnect = selection?.UbisoftConnect ?? PreparingStage.UbisoftConnect;
		bool EA = selection?.EA ?? PreparingStage.EA;
		bool BattleNet = selection?.BattleNet ?? PreparingStage.BattleNet;
		bool MinecraftLauncher = selection?.MinecraftLauncher ?? PreparingStage.MinecraftLauncher;
		bool CurseForge = selection?.CurseForge ?? PreparingStage.CurseForge;
		bool LunarClient = selection?.LunarClient ?? PreparingStage.LunarClient;
		bool FeatherClient = selection?.FeatherClient ?? PreparingStage.FeatherClient;
		bool NoRiskClient = selection?.NoRiskClient ?? PreparingStage.NoRiskClient;
		bool PrismLauncher = selection?.PrismLauncher ?? PreparingStage.PrismLauncher;
		bool Bloxstrap = selection?.Bloxstrap ?? PreparingStage.Bloxstrap;
		bool Froststrap = selection?.Froststrap ?? PreparingStage.Froststrap;
		bool RockstarGamesLauncher = selection?.RockstarGamesLauncher ?? PreparingStage.RockstarGamesLauncher;
		bool FiveM = selection?.FiveM ?? PreparingStage.FiveM;
		bool FACEIT = selection?.FACEIT ?? PreparingStage.FACEIT;
		bool FACEITAC = selection?.FACEITAC ?? PreparingStage.FACEITAC;
		bool Eden = selection?.Eden ?? PreparingStage.Eden;

		bool AppleMusic = selection?.AppleMusic ?? PreparingStage.AppleMusic;
		bool Tidal = selection?.Tidal ?? PreparingStage.Tidal;
		bool Qobuz = selection?.Qobuz ?? PreparingStage.Qobuz;
		bool AmazonMusic = selection?.AmazonMusic ?? PreparingStage.AmazonMusic;
		bool DeezerMusic = selection?.DeezerMusic ?? PreparingStage.DeezerMusic;
		bool Spotify = selection?.Spotify ?? PreparingStage.Spotify;
		bool MusicBee = selection?.MusicBee ?? PreparingStage.MusicBee;

		bool LogitechGHub = selection?.LogitechGHub ?? PreparingStage.LogitechGHub;
		bool LogitechOnboardMemoryManager = selection?.LogitechOnboardMemoryManager ?? PreparingStage.LogitechOnboardMemoryManager;
		bool Wootility = selection?.Wootility ?? PreparingStage.Wootility;
		bool GMenu = selection?.GMenu ?? PreparingStage.GMenu;
		bool EndgameGear = selection?.EndgameGear ?? PreparingStage.EndgameGear;
		bool GloriousCORE = selection?.GloriousCORE ?? PreparingStage.GloriousCORE;
		bool MCHOSE = selection?.MCHOSE ?? PreparingStage.MCHOSE;
		bool SteelSeriesGG = selection?.SteelSeriesGG ?? PreparingStage.SteelSeriesGG;
		bool RazerSynapse = selection?.RazerSynapse ?? PreparingStage.RazerSynapse;
		bool CorsairICue = selection?.CorsairICue ?? PreparingStage.CorsairICue;
		bool OpenRGB = selection?.OpenRGB ?? PreparingStage.OpenRGB;
		bool FanControl = selection?.FanControl ?? PreparingStage.FanControl;
		bool GHelper = selection?.GHelper ?? PreparingStage.GHelper;

		bool ViGEmBus = selection?.ViGEmBus ?? PreparingStage.ViGEmBus;
		bool HidHide = selection?.HidHide ?? PreparingStage.HidHide;
		bool DualSenseY = selection?.DualSenseY ?? PreparingStage.DualSenseY;
		bool RaceElement = selection?.RaceElement ?? PreparingStage.RaceElement;
		bool PlaystationAccessories = selection?.PlaystationAccessories ?? PreparingStage.PlaystationAccessories;
		bool XboxAccessories = selection?.XboxAccessories ?? PreparingStage.XboxAccessories;

		bool VisualStudio = selection?.VisualStudio ?? PreparingStage.VisualStudio;
		bool VisualStudioCode = selection?.VisualStudioCode ?? PreparingStage.VisualStudioCode;
		bool Antigravity = selection?.Antigravity ?? PreparingStage.Antigravity;
		bool Cursor = selection?.Cursor ?? PreparingStage.Cursor;
		bool Devin = selection?.Devin ?? PreparingStage.Devin;
		bool Kiro = selection?.Kiro ?? PreparingStage.Kiro;
		bool OpenCode = selection?.OpenCode ?? PreparingStage.OpenCode;
		bool SublimeText = selection?.SublimeText ?? PreparingStage.SublimeText;
		bool IDEA = selection?.IDEA ?? PreparingStage.IDEA;
		bool WinMerge = selection?.WinMerge ?? PreparingStage.WinMerge;
		bool Git = selection?.Git ?? PreparingStage.Git;
		bool CMake = selection?.CMake ?? PreparingStage.CMake;
		bool Python = selection?.Python ?? PreparingStage.Python;
		bool Nodejs = selection?.Nodejs ?? PreparingStage.Nodejs;
		bool Rust = selection?.Rust ?? PreparingStage.Rust;
		bool Java = selection?.Java ?? PreparingStage.Java;
		bool Go = selection?.Go ?? PreparingStage.Go;
		bool Trello = selection?.Trello ?? PreparingStage.Trello;

		bool Autoruns = selection?.Autoruns ?? PreparingStage.Autoruns;
		bool ProcessExplorer = selection?.ProcessExplorer ?? PreparingStage.ProcessExplorer;
		bool ProcessMonitor = selection?.ProcessMonitor ?? PreparingStage.ProcessMonitor;

		bool HWInfo = selection?.HWInfo ?? PreparingStage.HWInfo;
		bool TimingConfigurator = selection?.TimingConfigurator ?? PreparingStage.TimingConfigurator;
		bool ZenTimings = selection?.ZenTimings ?? PreparingStage.ZenTimings;
		bool TestMem5 = selection?.TestMem5 ?? PreparingStage.TestMem5;
		bool Prime95 = selection?.Prime95 ?? PreparingStage.Prime95;
		bool yCruncher = selection?.yCruncher ?? PreparingStage.yCruncher;
		bool OCCT = selection?.OCCT ?? PreparingStage.OCCT;
		bool AIDA64Extreme = selection?.AIDA64Extreme ?? PreparingStage.AIDA64Extreme;
		bool RamTestPro = selection?.RamTestPro ?? PreparingStage.RamTestPro;
		bool MemtestVulkan = selection?.MemtestVulkan ?? PreparingStage.MemtestVulkan;

		bool Reaper = selection?.Reaper ?? PreparingStage.Reaper;
		bool FLStudio = selection?.FLStudio ?? PreparingStage.FLStudio;
		bool Audacity = selection?.Audacity ?? PreparingStage.Audacity;
		bool FlexASIO = selection?.FlexASIO ?? PreparingStage.FlexASIO;
		bool ASIO4ALL = selection?.ASIO4ALL ?? PreparingStage.ASIO4ALL;
		bool ArturiaMidiControlCenter = selection?.ArturiaMidiControlCenter ?? PreparingStage.ArturiaMidiControlCenter;
		bool Voicemeeter = selection?.Voicemeeter ?? PreparingStage.Voicemeeter;

		bool DaVinciResolve = selection?.DaVinciResolve ?? PreparingStage.DaVinciResolve;
		bool Blender = selection?.Blender ?? PreparingStage.Blender;
		bool CapCut = selection?.CapCut ?? PreparingStage.CapCut;
		bool LosslessCut = selection?.LosslessCut ?? PreparingStage.LosslessCut;

		bool Netflix = selection?.Netflix ?? PreparingStage.Netflix;
		bool DisneyPlus = selection?.DisneyPlus ?? PreparingStage.DisneyPlus;
		bool PrimeVideo = selection?.PrimeVideo ?? PreparingStage.PrimeVideo;
		bool MpcQt = selection?.MpcQt ?? PreparingStage.MpcQt;
		bool MPV = selection?.MPV ?? PreparingStage.MPV;
		bool VLC = selection?.VLC ?? PreparingStage.VLC;
		bool MediaInfo = selection?.MediaInfo ?? PreparingStage.MediaInfo;

		bool Word = selection?.Word ?? PreparingStage.Word;
		bool Excel = selection?.Excel ?? PreparingStage.Excel;
		bool PowerPoint = selection?.PowerPoint ?? PreparingStage.PowerPoint;
		bool OneNote = selection?.OneNote ?? PreparingStage.OneNote;
		bool Teams = selection?.Teams ?? PreparingStage.Teams;
		bool Outlook = selection?.Outlook ?? PreparingStage.Outlook;
		bool OneDrive = selection?.OneDrive ?? PreparingStage.OneDrive;

		bool CapFrameX = selection?.CapFrameX ?? PreparingStage.CapFrameX;
		bool MinitoolPartitionWizard = selection?.MinitoolPartitionWizard ?? PreparingStage.MinitoolPartitionWizard;
		bool AomeiPartitionAssistant = selection?.AomeiPartitionAssistant ?? PreparingStage.AomeiPartitionAssistant;
		bool WizTree = selection?.WizTree ?? PreparingStage.WizTree;
		bool CrystalDiskInfo = selection?.CrystalDiskInfo ?? PreparingStage.CrystalDiskInfo;
		bool CrystalDiskMark = selection?.CrystalDiskMark ?? PreparingStage.CrystalDiskMark;
		bool ProtonVPN = selection?.ProtonVPN ?? PreparingStage.ProtonVPN;
		bool BulkCrapUninstaller = selection?.BulkCrapUninstaller ?? PreparingStage.BulkCrapUninstaller;
		bool BluetoothAudioReceiver = selection?.BluetoothAudioReceiver ?? PreparingStage.BluetoothAudioReceiver;
		bool AnyDesk = selection?.AnyDesk ?? PreparingStage.AnyDesk;
		bool RustDesk = selection?.RustDesk ?? PreparingStage.RustDesk;
		bool Apollo = selection?.Apollo ?? PreparingStage.Apollo;
		bool Moonlight = selection?.Moonlight ?? PreparingStage.Moonlight;
		bool AutoHotkey = selection?.AutoHotkey ?? PreparingStage.AutoHotkey;
		bool EmEditor = selection?.EmEditor ?? PreparingStage.EmEditor;
		bool WinDbg = selection?.WinDbg ?? PreparingStage.WinDbg;
		bool QBittorrent = selection?.QBittorrent ?? PreparingStage.QBittorrent;
		bool Deluge = selection?.Deluge ?? PreparingStage.Deluge;
		bool FreeDownloadManager = selection?.FreeDownloadManager ?? PreparingStage.FreeDownloadManager;

		string rockstarGamesLauncherVersion = "";
		string spotifyVersion = "";

		string scheduleMode = ScheduleMode switch
		{
			"Sunset to sunrise" => "LocationService",
			"Custom" => "Custom",
			_ => ScheduleMode
		};

		var actions = new List<(string Title, Func<Task> Action, Func<bool> Condition)>
		{
			// optimize notepad settings
			("Optimizing Notepad settings", async () => await StoreHelper.KillProcesses("Microsoft.WindowsNotepad_8wekyb3d8bbwe"), () => selection == null),
			("Optimizing Notepad settings", async () => await Process.Start(new ProcessStartInfo { FileName = "reg.exe", Arguments = $@"load HKU\TEMP ""{Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Packages", "Microsoft.WindowsNotepad_8wekyb3d8bbwe", "Settings", "settings.dat")}""", CreateNoWindow = true })!.WaitForExitAsync(), () => selection == null),
			("Optimizing Notepad settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_USERS\TEMP\LocalState", "AutoCorrect", RegistryHelper.ApplicationDataBoolean, [0x00, 0xcd, 0xff, 0x04, 0x45, 0x95, 0x13, 0xdc, 0x01]), () => selection == null),
			("Optimizing Notepad settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_USERS\TEMP\LocalState", "GhostFile", RegistryHelper.ApplicationDataBoolean, [0x00, 0xfc, 0x13, 0x31, 0x4b, 0x95, 0x13, 0xdc, 0x01]), () => selection == null),
			("Optimizing Notepad settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_USERS\TEMP\LocalState", "OpenFile", RegistryHelper.ApplicationDataBool, [0x01, 0x00, 0x00, 0x00, 0x9f, 0x01, 0x46, 0x4c, 0x95, 0x13, 0xdc, 0x01]), () => selection == null),
			("Optimizing Notepad settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_USERS\TEMP\LocalState", "RecentFilesEnabled", RegistryHelper.ApplicationDataBoolean, [0x00, 0x64, 0x5d, 0x84, 0x4a, 0x95, 0x13, 0xdc, 0x01]), () => selection == null),
			("Optimizing Notepad settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_USERS\TEMP\LocalState", "WordWrap", RegistryHelper.ApplicationDataBoolean, [0x00, 0xce, 0x88, 0x91, 0xac, 0x84, 0x8c, 0xdc, 0x01]), () => selection == null),
			("Optimizing Notepad settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_USERS\TEMP\LocalState", "SpellCheckState", RegistryHelper.ApplicationDataString, [0x7b, 0x00, 0x22, 0x00, 0x45, 0x00, 0x6e, 0x00, 0x61, 0x00, 0x62, 0x00, 0x6c, 0x00, 0x65, 0x00, 0x64, 0x00, 0x22, 0x00, 0x3a, 0x00, 0x66, 0x00, 0x61, 0x00, 0x6c, 0x00, 0x73, 0x00, 0x65, 0x00, 0x2c, 0x00, 0x22, 0x00, 0x46, 0x00, 0x69, 0x00, 0x6c, 0x00, 0x65, 0x00, 0x45, 0x00, 0x78, 0x00, 0x74, 0x00, 0x65, 0x00, 0x6e, 0x00, 0x73, 0x00, 0x69, 0x00, 0x6f, 0x00, 0x6e, 0x00, 0x73, 0x00, 0x4f, 0x00, 0x76, 0x00, 0x65, 0x00, 0x72, 0x00, 0x72, 0x00, 0x69, 0x00, 0x64, 0x00, 0x65, 0x00, 0x73, 0x00, 0x22, 0x00, 0x3a, 0x00, 0x5b, 0x00, 0x5b, 0x00, 0x22, 0x00, 0x2e, 0x00, 0x6d, 0x00, 0x64, 0x00, 0x22, 0x00, 0x2c, 0x00, 0x74, 0x00, 0x72, 0x00, 0x75, 0x00, 0x65, 0x00, 0x5d, 0x00, 0x2c, 0x00, 0x5b, 0x00, 0x22, 0x00, 0x2e, 0x00, 0x61, 0x00, 0x73, 0x00, 0x73, 0x00, 0x22, 0x00, 0x2c, 0x00, 0x74, 0x00, 0x72, 0x00, 0x75, 0x00, 0x65, 0x00, 0x5d, 0x00, 0x2c, 0x00, 0x5b, 0x00, 0x22, 0x00, 0x2e, 0x00, 0x6c, 0x00, 0x69, 0x00, 0x63, 0x00, 0x22, 0x00, 0x2c, 0x00, 0x74, 0x00, 0x72, 0x00, 0x75, 0x00, 0x65, 0x00, 0x5d, 0x00, 0x2c, 0x00, 0x5b, 0x00, 0x22, 0x00, 0x2e, 0x00, 0x73, 0x00, 0x72, 0x00, 0x74, 0x00, 0x22, 0x00, 0x2c, 0x00, 0x74, 0x00, 0x72, 0x00, 0x75, 0x00, 0x65, 0x00, 0x5d, 0x00, 0x2c, 0x00, 0x5b, 0x00, 0x22, 0x00, 0x2e, 0x00, 0x6c, 0x00, 0x72, 0x00, 0x63, 0x00, 0x22, 0x00, 0x2c, 0x00, 0x74, 0x00, 0x72, 0x00, 0x75, 0x00, 0x65, 0x00, 0x5d, 0x00, 0x2c, 0x00, 0x5b, 0x00, 0x22, 0x00, 0x2e, 0x00, 0x74, 0x00, 0x78, 0x00, 0x74, 0x00, 0x22, 0x00, 0x2c, 0x00, 0x74, 0x00, 0x72, 0x00, 0x75, 0x00, 0x65, 0x00, 0x5d, 0x00, 0x5d, 0x00, 0x7d, 0x00, 0x00, 0x00, 0x02, 0xde, 0x19, 0xb1, 0x84, 0x8c, 0xdc, 0x01]), () => selection == null),
			("Optimizing Notepad settings", async () => await Process.Start(new ProcessStartInfo { FileName = "reg.exe", Arguments = @"unload HKU\TEMP", CreateNoWindow = true })!.WaitForExitAsync(), () => selection == null),
			("Optimizing Notepad settings", async () => { try { FileSystem.CopyDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Packages", "Microsoft.WindowsNotepad_8wekyb3d8bbwe"), Path.Combine(Path.GetPathRoot(Environment.SystemDirectory)!, "Users", "Default", "AppData", "Local", "Packages", "Microsoft.WindowsNotepad_8wekyb3d8bbwe"), true); } catch { } }, () => selection == null),
			
			// optimize xbox gaming overlay settings
			("Optimizing Xbox Gaming Overlay settings", async () => await StoreHelper.KillProcesses("Microsoft.XboxGamingOverlay_8wekyb3d8bbwe"), () => selection == null),
			("Optimizing Xbox Gaming Overlay settings", async () => await Process.Start(new ProcessStartInfo { FileName = "reg.exe", Arguments = $@"load HKU\TEMP ""{Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Packages", "Microsoft.XboxGamingOverlay_8wekyb3d8bbwe", "Settings", "settings.dat")}""", CreateNoWindow = true })!.WaitForExitAsync(), () => selection == null),
			("Optimizing Xbox Gaming Overlay settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_USERS\TEMP\LocalState", "AppTheme", RegistryHelper.ApplicationDataInt32, [0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x1f, 0x43, 0x21, 0xaf, 0xc9, 0xd4, 0xdc, 0x01]), () => selection == null),
			("Optimizing Xbox Gaming Overlay settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_USERS\TEMP\LocalState", "ClosedControllerBarTip", RegistryHelper.ApplicationDataBoolean, [0x01, 0x43, 0x8e, 0x00, 0xd5, 0xc9, 0xd4, 0xdc, 0x01]), () => selection == null),
			("Optimizing Xbox Gaming Overlay settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_USERS\TEMP\LocalState", "CompactModeEnabled", RegistryHelper.ApplicationDataBoolean, [0x01, 0xa9, 0x17, 0x87, 0xdc, 0xc9, 0xd4, 0xdc, 0x01]), () => selection == null),
			("Optimizing Xbox Gaming Overlay settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_USERS\TEMP\LocalState", "DesktopCompactModeUserPreference", RegistryHelper.ApplicationDataBoolean, [0x01, 0xa9, 0x17, 0x87, 0xdc, 0xc9, 0xd4, 0xdc, 0x01]), () => selection == null),
			("Optimizing Xbox Gaming Overlay settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_USERS\TEMP\LocalState", "FeedbackNotifications", RegistryHelper.ApplicationDataBoolean, [0x00, 0xe7, 0xda, 0x9e, 0x92, 0xc9, 0xd4, 0xdc, 0x01]), () => selection == null),
			("Optimizing Xbox Gaming Overlay settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_USERS\TEMP\LocalState", "HdrNotifications", RegistryHelper.ApplicationDataBoolean, [0x00, 0x61, 0x28, 0x3e, 0x95, 0xc9, 0xd4, 0xdc, 0x01]), () => selection == null),
			("Optimizing Xbox Gaming Overlay settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_USERS\TEMP\LocalState", "RecordingNotifications", RegistryHelper.ApplicationDataBoolean, [0x01, 0xc7, 0x40, 0x17, 0x88, 0xc9, 0xd4, 0xdc, 0x01]), () => selection == null),
			("Optimizing Xbox Gaming Overlay settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_USERS\TEMP\LocalState", "SuppressEnableCompactModeFlyout", RegistryHelper.ApplicationDataBoolean, [0x01, 0xa9, 0x17, 0x87, 0xdc, 0xc9, 0xd4, 0xdc, 0x01]), () => selection == null),
			("Optimizing Xbox Gaming Overlay settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_USERS\TEMP\LocalState", "SuppressFullscreenNotifications", RegistryHelper.ApplicationDataBoolean, [0x01, 0x81, 0x23, 0x85, 0x97, 0xc9, 0xd4, 0xdc, 0x01]), () => selection == null),
			("Optimizing Xbox Gaming Overlay settings", async () => await Process.Start(new ProcessStartInfo { FileName = "reg.exe", Arguments = @"unload HKU\TEMP", CreateNoWindow = true })!.WaitForExitAsync(), () => selection == null),
			("Optimizing Xbox Gaming Overlay settings", async () => { try { FileSystem.CopyDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Packages", "Microsoft.XboxGamingOverlay_8wekyb3d8bbwe"), Path.Combine(Path.GetPathRoot(Environment.SystemDirectory)!, "Users", "Default", "AppData", "Local", "Packages", "Microsoft.XboxGamingOverlay_8wekyb3d8bbwe"), true); } catch { } }, () => selection == null),
			
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

			// download 1password
			("Downloading 1Password", async () => await StoreHelper.Download("DC5C6510.2032887045529_2v019pwa6amcg", reporter: reporter), () => OnePassword == true),

			// install 1password
			("Installing 1Password", async () => await StoreHelper.Install("DC5C6510.2032887045529_2v019pwa6amcg"), () => OnePassword == true),
			("Installing 1Password", async () => { string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "1Password", "settings", "settings.json"); Directory.CreateDirectory(Path.GetDirectoryName(path) !); await File.WriteAllTextAsync(path, "{ \"version\": 1, \"updates.updateChannel\": \"PRODUCTION\", \"authTags\": {}, \"app.keepInTray\": false }"); }, () => OnePassword == true),

			// disable 1password startup entry
			("Disabling 1Password startup entry", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Classes\Local Settings\Software\Microsoft\Windows\CurrentVersion\AppModel\SystemAppData\DC5C6510.2032887045529_2v019pwa6amcg\1PasswordStartup", "State", 1, RegistryValueKind.DWord), () => OnePassword == true),

			// download nanazip
			("Downloading NanaZip", async () => await StoreHelper.Download("40174MouriNaruto.NanaZip_gnj4mf6z9tkrc", reporter: reporter), () => selection == null),

			// install nanazip
			("Installing NanaZip", async () => await StoreHelper.Install("40174MouriNaruto.NanaZip_gnj4mf6z9tkrc"), () => selection == null),

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
			("Installing Everything", async () => await DownloadHelper.Download("https://raw.githubusercontent.com/tinodin/AutoOS-Resources/main/Files/Everything/Everything-1.5a.ini", Path.GetTempPath(), "Everything-1.5a.ini", reporter: reporter), () => selection == null),
			("Installing Everything", async () => File.Copy(Path.Combine(Path.GetTempPath(), "Everything-1.5a.ini"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Everything", "Everything-1.5a.ini"), true), () => selection == null),
			("Installing Everything", async () => Directory.CreateDirectory(Path.Combine(Path.GetPathRoot(Environment.SystemDirectory)!, "Users", "Default", "AppData", "Roaming", "Everything")), () => selection == null),
			("Installing Everything", async () => File.Move(Path.Combine(Path.GetTempPath(), "Everything-1.5a.ini"), Path.Combine(Path.GetPathRoot(Environment.SystemDirectory)!, "Users", "Default", "AppData", "Roaming", "Everything", "Everything-1.5a.ini"), true), () => selection == null),
			("Installing Everything", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Everything 1.5a", "Everything.exe"), WindowStyle = ProcessWindowStyle.Hidden, Arguments = "-install-run-on-system-startup"})!.WaitForExitAsync(), () => selection == null),
			("Installing Everything", async () => Process.Start(new ProcessStartInfo { FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Everything 1.5a", "Everything.exe"), WindowStyle = ProcessWindowStyle.Hidden, Arguments = "-startup" }), () => selection == null),
			("Cleaning up Everything files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "Everything.exe")), () => selection == null),

			// remove everything desktop shortcut 
			("Removing Everything desktop shortcut", async () => File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Everything 1.5a.lnk")), () => selection == null),

			// download windhawk
			("Downloading Windhawk", async () => await DownloadHelper.Download(JsonDocument.Parse(await new HttpClient { DefaultRequestHeaders = { { "User-Agent", "AutoOS" } } }.GetStringAsync("https://api.github.com/repos/ramensoftware/windhawk/releases")).RootElement[0].GetProperty("assets").EnumerateArray().First(a => a.GetProperty("name").GetString().Contains("windhawk_setup_offline.exe")).GetProperty("browser_download_url").GetString(), Path.GetTempPath(), "windhawk_setup_offline.exe", reporter: reporter), () => selection == null),

			// install windhawk
			("Installing Windhawk", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Path.GetTempPath(), "windhawk_setup_offline.exe"), Arguments = "/S", WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), () => selection == null),
			("Installing Windhawk", async () => await DownloadHelper.Download("https://raw.githubusercontent.com/tinodin/AutoOS-Resources/main/Files/Windhawk/windhawk.reg", Path.GetTempPath(), "windhawk.reg", reporter: reporter), () => selection == null),

			("Installing Windhawk", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Windhawk", "windhawk-cli.exe"), Arguments = "mod install auto-theme-switcher --quiet", WorkingDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Windhawk"), WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), () => selection == null),
			("Installing Windhawk", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Windhawk", "windhawk-cli.exe"), Arguments = "mod install explorer-details-better-file-sizes --quiet", WorkingDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Windhawk"), WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), () => selection == null),
			("Installing Windhawk", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Windhawk", "windhawk-cli.exe"), Arguments = "mod install extension-change-no-warning --quiet", WorkingDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Windhawk"), WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), () => selection == null),
			("Installing Windhawk", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Windhawk", "windhawk-cli.exe"), Arguments = "mod install f1-blocker --quiet", WorkingDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Windhawk"), WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), () => selection == null),
			("Installing Windhawk", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Windhawk", "windhawk-cli.exe"), Arguments = "mod install modernize-folder-picker-dialog --quiet", WorkingDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Windhawk"), WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), () => selection == null),
			("Installing Windhawk", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Windhawk", "windhawk-cli.exe"), Arguments = "mod install slick-window-arrangement --quiet", WorkingDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Windhawk"), WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), () => selection == null),
			("Installing Windhawk", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Windhawk", "windhawk-cli.exe"), Arguments = "mod install start-menu-open-location --quiet", WorkingDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Windhawk"), WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), () => selection == null),
			("Installing Windhawk", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Windhawk", "windhawk-cli.exe"), Arguments = "mod install taskbar-fluent-media-player --quiet", WorkingDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Windhawk"), WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), () => selection == null),
			("Installing Windhawk", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Windhawk", "windhawk-cli.exe"), Arguments = "mod install taskbar-notification-icons-show-all --quiet", WorkingDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Windhawk"), WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), () => selection == null),
			("Installing Windhawk", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Windhawk", "windhawk-cli.exe"), Arguments = "mod install windows-11-start-menu-styler --quiet", WorkingDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Windhawk"), WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), () => selection == null),
			("Installing Windhawk", async () => await Process.Start(new ProcessStartInfo { FileName = "reg.exe", Arguments = @$"import ""{Path.Combine(Path.GetTempPath(), "windhawk.reg")}""", CreateNoWindow = true })!.WaitForExitAsync(), () => selection == null),
			//("Installing Windhawk", async () => await RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Windhawk\Engine\Mods\auto-theme-switcher\Settings", "LightThemePath", LightThemePath, RegistryValueKind.String), () => selection == null),
			//("Installing Windhawk", async () => await RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Windhawk\Engine\Mods\auto-theme-switcher\Settings", "DarkThemePath", DarkThemePath, RegistryValueKind.String), () => selection == null),
			("Installing Windhawk", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Windhawk\Engine\Mods\auto-theme-switcher", "Disabled", 1, RegistryValueKind.DWord), () => ScheduleMode == "Always Light" || ScheduleMode == "Always Dark"),
			("Installing Windhawk", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Windhawk\Engine\Mods\auto-theme-switcher\Settings", "ScheduleMode", scheduleMode, RegistryValueKind.String), () => !string.IsNullOrEmpty(ScheduleMode) && ScheduleMode != "Always Light" && ScheduleMode != "Always Dark"),
			("Installing Windhawk", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Windhawk\Engine\Mods\auto-theme-switcher\Settings", "CustomLight", LightTime, RegistryValueKind.String), () => selection == null),
			("Installing Windhawk", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Windhawk\Engine\Mods\auto-theme-switcher\Settings", "CustomDark", DarkTime, RegistryValueKind.String), () => selection == null),
			("Installing Windhawk", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Windhawk\Engine\Mods\taskbar-fluent-media-player\Settings", "MainSettings.PlayerSetting.position", "tray_left", RegistryValueKind.String), () => selection == null && LeftTaskbarAlignment == true),
			("Installing Windhawk", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Windhawk\Engine\Mods\taskbar-notification-icons-show-all", "Disabled", 1, RegistryValueKind.DWord), () => selection == null && AlwaysShowTrayIcons == false),
			("Cleaning up Windhawk files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "windhawk_setup_offline.exe")), () => selection == null),
			("Cleaning up Windhawk files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "windhawk.reg")), () => selection == null),
			
			// remove windhawk desktop shortcut 
			("Removing Windhawk desktop shortcut", async () => File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory), "Windhawk.lnk")), () => selection == null),

			// download startallback
			("Downloading StartAllBack", async () => await DownloadHelper.Download("https://www.startallback.com/download.php", Path.GetTempPath(), "StartAllBackSetup.exe", reporter: reporter), () => selection == null),

			// install startallback
			("Installing StartAllBack", async () => await Process.Start(new ProcessStartInfo { FileName = "reg.exe", Arguments = $@"load HKU\DefaultUser ""{Path.Combine(Path.GetPathRoot(Environment.SystemDirectory)!, "Users", "Default", "NTUSER.DAT")}""", CreateNoWindow = true })!.WaitForExitAsync(), () => selection == null),
			("Installing StartAllBack", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\StartIsBack", "AutoUpdates", 0, RegistryValueKind.DWord, true), () => selection == null),
			("Installing StartAllBack", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\StartIsBack", "SettingsVersion", 6, RegistryValueKind.DWord, true), () => selection == null),
			("Installing StartAllBack", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\StartIsBack", "WelcomeShown", 3, RegistryValueKind.DWord, true), () => selection == null),
			("Installing StartAllBack", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\StartIsBack", "FrameStyle", 0, RegistryValueKind.DWord, true), () => selection == null),
			("Installing StartAllBack", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\StartIsBack", "NoXAMLMenus", 0, RegistryValueKind.DWord, true), () => selection == null),
			("Installing StartAllBack", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\StartIsBack", "WinkeyFunction", 1, RegistryValueKind.DWord, true), () => selection == null),
			("Installing StartAllBack", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\StartIsBack", "LegacyTaskbar", 0, RegistryValueKind.DWord, true), () => selection == null),
			("Installing StartAllBack", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\StartIsBack", "DriveGrouping", 1, RegistryValueKind.DWord, true), () => selection == null),
			("Installing StartAllBack", async () => await Process.Start(new ProcessStartInfo { FileName = "reg.exe", Arguments = @"unload HKU\DefaultUser", CreateNoWindow = true })!.WaitForExitAsync(), () => selection == null),
			("Installing StartAllBack", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Path.GetTempPath(), "StartAllBackSetup.exe"), Arguments = "/silent /allusers" , WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), () => selection == null),
			("Installing StartAllBack", async () => TaskSchedulerHelper.Toggle(@"StartAllBack Update", false), () => selection == null),
			("Cleaning up StartAllBack files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "StartAllBackSetup.exe")), () => selection == null),

			// activate startallback
			("Activating StartAllBack", async () => await ProcessActions.PatchStartAllBack(), () => selection == null),
			("Activating StartAllBack", async () => await Task.Delay(2000), () => selection == null),

			// download discord
			("Downloading Discord", async () => await DownloadHelper.Download("https://discord.com/api/downloads/distributions/app/installers/latest?channel=stable&platform=win&arch=x64", Path.GetTempPath(), "DiscordSetup.exe", reporter: reporter), () => Discord == true),

			// install discord
			("Installing Discord", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Path.GetTempPath(), "DiscordSetup.exe"), Arguments = "/silent" , WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), () => Discord == true),
			("Installing Discord", async () => File.Copy(Path.Combine(Directory.GetDirectories(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Discord"), "app-*").FirstOrDefault(), "installer.db"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Discord", "installer.db"), true), () => Discord == true),
			("Cleaning up Discord files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "DiscordSetup.exe")), () => Discord == true),

			// pin discord to the taskbar
			("Pinning Discord to the taskbar", async () => await ProcessActions.PinToTaskbar("Link", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft", "Windows", "Start Menu", "Programs", "Discord Inc", "Discord.lnk")), () => Discord == true),

			// remove discord desktop shortcut 
			("Removing Discord desktop shortcut", async () => File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Discord.lnk")), () => Discord == true),

			// disable discord startup entry
			("Disabling Discord startup entry", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run", "Discord", new byte[] { 0x01 }, RegistryValueKind.Binary), () => Discord == true),

			// optimize discord settings
			("Optimizing Discord settings", async () => await File.WriteAllTextAsync(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Discord", "settings.json"), "{\n  \"OPEN_ON_STARTUP\": false,\n  \"MINIMIZE_TO_TRAY\": false,\n  \"debugLogging\": false,\n  \"openasar\": {\n    \"setup\": true,\n    \"noTrack\": true\n  }\n}"), () => Discord == true),

			// download vencord
			("Downloading Vencord", async () => await DownloadHelper.Download("https://github.com/Vencord/Installer/releases/latest/download/VencordInstallerCli.exe", Path.GetTempPath(), "VencordInstallerCli.exe", reporter: reporter), () => Discord == true),

			// install vencord
			("Installing Vencord", async () => await Process.Start(new ProcessStartInfo { FileName = "cmd.exe", Arguments = $@"/c """"{Path.Combine(Path.GetTempPath(), "VencordInstallerCli.exe")}"" -install -install-openasar -branch auto""" , CreateNoWindow = true })!.WaitForExitAsync(), () => Discord == true),
			("Installing OpenAsar", async () => await Process.Start(new ProcessStartInfo { FileName = "cmd.exe", Arguments = $@"/c """"{Path.Combine(Path.GetTempPath(), "VencordInstallerCli.exe")}"" -install-openasar -branch auto""" , CreateNoWindow = true })!.WaitForExitAsync(), () => Discord == true),
			("Cleaning up Vencord files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "VencordInstallerCli.exe")), () => Discord == true),

			// import vencord settings
			("Importing Vencord settings", async () => Directory.CreateDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Vencord", "settings")), () => Discord == true),
			("Importing Vencord settings", async () => await DownloadHelper.Download("https://raw.githubusercontent.com/tinodin/AutoOS-Resources/main/Files/Vencord/settings.json", Path.GetTempPath(), "settings.json", reporter: reporter), () => Discord == true),
			("Importing Vencord settings", async () => File.Copy(Path.Combine(Path.GetTempPath(), "settings.json"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Vencord", "settings", "settings.json"), true), () => Discord == true),
			("Importing Vencord settings", async () => await Task.Delay(500), () => Discord == true),

			// import discord account
			("Importing Discord Account", async () => { Process.Start(new ProcessStartInfo { FileName = Path.Combine(Directory.GetDirectories(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Discord"), "app-*").FirstOrDefault(), "Discord.exe"), WindowStyle = ProcessWindowStyle.Hidden }); while (Process.GetProcessesByName("OpenWith").Length == 0 && Process.GetProcessesByName("msedge").Length == 0) { await Task.Delay(500); } }, () => Discord == true && DiscordAccount == true),
			("Importing Discord Account", async () => await Task.Delay(4000), () => Discord == true && DiscordAccount == true),
			("Importing Discord Account", async () => { foreach (Process process in Process.GetProcessesByName("OpenWith")) { process.Kill(); process.WaitForExit(); } foreach (Process process in Process.GetProcessesByName("msedge")) { process.Kill(); process.WaitForExit(); } }, () => Discord == true && DiscordAccount == true),
			("Importing Discord Account", async () => { foreach (Process process in Process.GetProcessesByName("Discord")) { if (process.MainWindowHandle != IntPtr.Zero) { PInvoke.PostMessage((HWND)process.MainWindowHandle, PInvoke.WM_CLOSE, default(WPARAM), default(LPARAM)); process.WaitForExit(); } } }, () => Discord == true && DiscordAccount == true),
			("Importing Discord Account", async () => await DiscordHelper.ImportAccount(reporter), () => Discord == true && DiscordAccount == true),

			// import discord keybinds
			("Importing Discord Keybinds", async () => await DiscordHelper.ImportKeybinds(reporter), () => Discord == true && DiscordKeybinds == true),

			// set appearance to system
			("Setting appearance to system", async () => await DiscordHelper.SetSystemAppearance(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "discord", "Local Storage", "leveldb")), () => Discord == true),

			// disable game overlay
			("Disabling game overlay", async () => await DiscordHelper.DisableGameOverlay(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "discord", "Local Storage", "leveldb")), () => Discord == true),

			// disable clips
			("Disabling clips", async () => await DiscordHelper.DisableClips(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "discord", "Local Storage", "leveldb")), () => Discord == true),

			// remove discord desktop shortcut 
			("Removing Discord desktop shortcut", async () => File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Discord.lnk")), () => Discord == true),

			// download whatsapp
			("Downloading WhatsApp", async () => await StoreHelper.Download("5319275A.WhatsAppDesktop_cv1g1gvanyjgm", reporter: reporter), () => WhatsApp == true),

			// install whatsapp
			("Installing WhatsApp", async () => await StoreHelper.Install("5319275A.WhatsAppDesktop_cv1g1gvanyjgm"), () => WhatsApp == true),

			// disable "minimize to system tray"
			(@"Disabling ""Minimize to system tray""", async () => await Process.Start(new ProcessStartInfo { FileName = "reg.exe", Arguments = $@"load HKU\TEMP ""{Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Packages", "5319275A.WhatsAppDesktop_cv1g1gvanyjgm", "Settings", "settings.dat")}""", CreateNoWindow = true })!.WaitForExitAsync(), () => WhatsApp == true),
			(@"Disabling ""Minimize to system tray""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_USERS\TEMP\LocalState\web_preferences", "WindowsIsSystemTrayEnabled", RegistryHelper.ApplicationDataString, [0x66, 0x00, 0x61, 0x00, 0x6c, 0x00, 0x73, 0x00, 0x65, 0x00, 0x00, 0x00, 0x2d, 0xb8, 0x83, 0xd6, 0xf4, 0x98, 0xdc, 0x01]), () => WhatsApp == true),
			(@"Disabling ""Minimize to system tray""", async () => await Process.Start(new ProcessStartInfo { FileName = "reg.exe", Arguments = @"unload HKU\TEMP", CreateNoWindow = true })!.WaitForExitAsync(), () => WhatsApp == true),
			(@"Disabling ""Minimize to system tray""", async () => { try { FileSystem.CopyDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Packages", "5319275A.WhatsAppDesktop_cv1g1gvanyjgm"), Path.Combine(Path.GetPathRoot(Environment.SystemDirectory)!, "Users", "Default", "AppData", "Local", "Packages", "5319275A.WhatsAppDesktop_cv1g1gvanyjgm"), true); } catch { } }, () => WhatsApp == true),

			// pin whatsapp to the taskbar
			("Pinning WhatsApp to the taskbar", async () => await ProcessActions.PinToTaskbar("UWA", "5319275A.WhatsAppDesktop_cv1g1gvanyjgm!App"), () => WhatsApp == true),

			// disable whatsapp startup entry
			("Disabling WhatsApp startup entry", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Classes\Local Settings\Software\Microsoft\Windows\CurrentVersion\AppModel\SystemAppData\5319275A.WhatsAppDesktop_cv1g1gvanyjgm\2defd21c-0b9e-4e4e-873a-2a68c47d7da5", "State", 1, RegistryValueKind.DWord), () => WhatsApp == true),

			// download telegram desktop
			("Downloading Telegram Desktop", async () => await StoreHelper.Download("TelegramMessengerLLP.TelegramDesktop_t4vj0pshhgkwm", reporter: reporter), () => Telegram == true),

			// install telegram desktop
			("Installing Telegram Desktop", async () => await StoreHelper.Install("TelegramMessengerLLP.TelegramDesktop_t4vj0pshhgkwm"), () => Telegram == true),

			// pin telegram desktop to the taskbar
			("Pinning Telegram Desktop to the taskbar", async () => await ProcessActions.PinToTaskbar("UWA", "TelegramMessengerLLP.TelegramDesktop_t4vj0pshhgkwm!Telegram.TelegramDesktop.Store"), () => Telegram == true),

			// download unigram
			("Downloading Unigram", async () => await StoreHelper.Download("38833FF26BA1D.UnigramPreview_g9c9v27vpyspw", reporter: reporter), () => Unigram == true),

			// install unigram
			("Installing Unigram", async () => await StoreHelper.Install("38833FF26BA1D.UnigramPreview_g9c9v27vpyspw"), () => Unigram == true),

			// pin unigram to the taskbar
			("Pinning Unigram to the taskbar", async () => await ProcessActions.PinToTaskbar("UWA", "38833FF26BA1D.UnigramPreview_g9c9v27vpyspw!App"), () => Unigram == true),

			// download zoom workplace
			("Downloading Zoom Workplace", async () => await DownloadHelper.Download("https://cdn.zoom.us/prod/7.0.5.38856/x64/ZoomInstallerFull.msi", Path.GetTempPath(), "ZoomInstallerFull.msi", reporter: reporter), () => ZoomWorkplace == true),

			// install zoom workplace
			("Installing Zoom Workplace", async () => await Process.Start(new ProcessStartInfo { FileName = "msiexec.exe", Arguments = $@"/i ""{Path.Combine(Path.GetTempPath(), "ZoomInstallerFull.msi")}"" /qn" , WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), () => ZoomWorkplace ==  true),
			("Cleaning up Zoom Workplace files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "ZoomInstallerFull.msi")), () => ZoomWorkplace ==  true),

			// pin zoom to taskbar
			("Pinning Zoom Workplace to the taskbar", async () => await ProcessActions.PinToTaskbar("Link", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Microsoft", "Windows", "Start Menu", "Programs", "Zoom", "Zoom Workplace.lnk")), () => ZoomWorkplace == true),

			// disable zoom service
			("Disabling Zoom service", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\ZoomCptService", "Start", 3, RegistryValueKind.DWord), () => ZoomWorkplace == true),
			("Disabling Zoom service", async () => ServicesHelper.StopService("ZoomCptService"), () => ZoomWorkplace == true),

			// remove zoom workplace desktop shortcut
			("Removing Zoom Workplace desktop shortcut", async () => File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory), "Zoom Workplace.lnk")), () => ZoomWorkplace == true),

			// download thunderbird
			("Downloading Thunderbird", async () => await DownloadHelper.Download("https://download.mozilla.org/?product=thunderbird-151.0.1-msi-SSL&os=win64&lang=en-US", Path.GetTempPath(), "Thunderbird Setup.msi", reporter: reporter), () => Thunderbird == true),

			// install thunderbird
			("Installing Thunderbird", async () => await Process.Start(new ProcessStartInfo { FileName = "msiexec.exe", Arguments = $@"/i ""{Path.Combine(Path.GetTempPath(), "Thunderbird Setup.msi")}"" /qn" , WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), () => Thunderbird ==  true),
			("Cleaning up Thunderbird files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "Thunderbird Setup.msi")), () => Thunderbird ==  true),

			// pin thunderbird to taskbar
			("Pinning Thunderbird to the taskbar", async () => await ProcessActions.PinToTaskbar("Link", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Microsoft", "Windows", "Start Menu", "Programs", "Thunderbird.lnk")), () => Thunderbird == true),

			// disable thunderbird service
			("Disabling Thunderbird service", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\MozillaMaintenance", "Start", 4, RegistryValueKind.DWord), () => Thunderbird == true),
			("Disabling Thunderbird service", async () => ServicesHelper.StopService("MozillaMaintenance"), () => Thunderbird == true),

			// remove thunderbird desktop shortcut
			("Removing Thunderbird desktop shortcut", async () => File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory), "Thunderbird.lnk")), () => Thunderbird == true),

			// download signal
			("Downloading Signal", async () => await DownloadHelper.Download("https://updates.signal.org/desktop/signal-desktop-win-8.14.0.exe", Path.GetTempPath(), "SignalSetup.exe", reporter: reporter), () => Signal == true),

			// install signal
			("Installing Signal", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Path.GetTempPath(), "SignalSetup.exe"), Arguments = "/S" , WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), () => Signal == true),
			("Cleaning up Signal files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "SignalSetup.exe")), () => Signal == true),

			// remove signal desktop shortcut
			("Removing Signal desktop shortcut", async () => File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Signal.lnk")), () => Signal == true),

			// download epic games launcher
			("Downloading Epic Games Launcher", async () => await DownloadHelper.Download("https://launcher-public-service-prod06.ol.epicgames.com/launcher/api/installer/download/EpicGamesLauncherInstaller.msi", Path.GetTempPath(), "EpicGamesLauncherInstaller.msi", reporter: reporter), () => EpicGames == true),

			// install epic games launcher
			("Installing Epic Games Launcher", async () => await Process.Start(new ProcessStartInfo { FileName = "msiexec.exe", Arguments = $@"/i ""{Path.Combine(Path.GetTempPath(), "EpicGamesLauncherInstaller.msi")}"" /qn" , WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), () => EpicGames == true),
			("Cleaning up Epic Games Launcher files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "EpicGamesLauncherInstaller.msi")), () => EpicGames == true),

			// remove epic games launcher desktop shortcut
			("Removing Epic Games Launcher desktop shortcut", async () => File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory), "Epic Games Launcher.lnk")), () => EpicGames == true),

			// update epic games launcher
			("Updating Epic Games Launcher", async () => await Process.Start(new ProcessStartInfo { FileName = File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Epic Games", "Launcher", "Portal", "Binaries", "Win64", "EpicGamesLauncher.exe")) ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Epic Games", "Launcher", "Portal", "Binaries", "Win64", "EpicGamesLauncher.exe") : File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Epic Games", "Launcher", "Portal", "Binaries", "Win32", "EpicGamesLauncher.exe")) ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Epic Games", "Launcher", "Portal", "Binaries", "Win32", "EpicGamesLauncher.exe") : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Epic Games", "Launcher", "Engine", "Binaries", "Win64", "EpicGamesLauncher.exe") })!.WaitForExitAsync(), () => EpicGames == true),
			("Updating Epic Games Launcher", async () => { while (true) { foreach (Process proc in Process.GetProcessesByName("EpicGamesLauncher")) { if (ProcessesHelper.GetCommandLine(proc).Contains("-AllowSoftwareRendering -SaveToUserDir -Messaging", StringComparison.OrdinalIgnoreCase)) { EpicGamesHelper.CloseEpicGames(); return; } } await Task.Delay(100); } }, () => EpicGames == true),
			
			// import epic games launcher account
			("Importing Epic Games Launcher Account", async () => await EpicGamesHelper.ImportAccount(), () => EpicGames == true && EpicGamesAccount == true),

			// import epic games launcher games
			("Importing Epic Games Launcher Games", async () => await EpicGamesHelper.ImportGames(), () => EpicGames == true && EpicGamesGames == true),
			("Importing Epic Games Launcher Games", async () => Fortnite = File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Epic", "UnrealEngineLauncher", "LauncherInstalled.dat")) && (JsonNode.Parse(await File.ReadAllTextAsync(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Epic", "UnrealEngineLauncher", "LauncherInstalled.dat")))?["InstallationList"] is JsonArray installations) && installations.Any(entry => entry?["AppName"]?.ToString() == "Fortnite") , () => EpicGames == true && EpicGamesGames == true),
			("Importing Epic Games Launcher Games", async () => await Task.Delay(1000), () => EpicGames == true && EpicGamesGames == true),
			("Importing Epic Games Launcher Games", async () => EpicGamesHelper.CloseEpicGames(), () => EpicGames == true && EpicGamesGames == true),

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

			// import steam games
			("Importing Steam Games", async () => await SteamHelper.ImportGames(), () => Steam == true && SteamGames == true),

			// remove steam desktop shortcut
			("Removing Steam desktop shortcut", async () => File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory), "Steam.lnk")), () => Steam == true),

			// disable steam startup entry
			("Disabling Steam startup entry", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run", "Steam", new byte[] { 0x01 }, RegistryValueKind.Binary), () => Steam == true),

			// download riot client
			("Downloading Riot Client", async () => await DownloadHelper.Download("https://github.com/tinodin/AutoOS-Resources/releases/download/v1.0.0.0/Riot.Games.zip", Path.GetTempPath(), "Riot Games.zip", reporter: reporter), () => RiotClient == true),

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

			// optimize riot client settings
			("Optimizing Riot Client settings", async () => { string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Riot Games\Riot Client\Config\RiotClientSettings.yaml"); await File.WriteAllTextAsync(path, Regex.Replace(await File.ReadAllTextAsync(path), @"(launch_on_computer_set_by_default|enable_run_in_background_set_by_player|enable_launch_on_computer_start_set_by_player):.*", "$1: false")); }, () => RiotClient == true),

			// disable riot client startup entry
			("Disabling Riot Client startup entry", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run", "RiotClient", new byte[] { 0x01 }, RegistryValueKind.Binary), () => RiotClient == true),

			// remove riot client desktop shortcut
			("Removing Riot Client desktop shortcut", async () => File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory), "Riot Client.lnk")), () => RiotClient == true),

			// download vanguard
			("Downloading Vanguard", async () => await DownloadHelper.Download("https://github.com/tinodin/AutoOS-Resources/releases/download/v1.0.0.0/setup.exe", Path.GetTempPath(), "setup.exe", new InstallPageReporter()), () => RiotClient == true),

			// install vanguard
			("Installing Vanguard", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Path.GetTempPath(), "setup.exe"), WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), () => RiotClient == true),
			("Installing Vanguard", async () => { Directory.CreateDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Riot Games", "Riot Vanguard")); await File.WriteAllTextAsync(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Riot Games", "Riot Vanguard", "vgtray-settings.json"), "{\"theme\":0,\"language\":0,\"precheck_option\":1,\"precheck_completed_after_settings_changes\":1,\"precheck_completed_once\":1}"); }, () => RiotClient == true),
			("Installing Vanguard", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run", "Riot Vanguard", new byte[] { 0x03 }, RegistryValueKind.Binary), () => RiotClient == true),
			("Cleaning up Vanguard files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "setup.exe")), () => RiotClient == true),

			// download ubisoft connect
			("Downloading Ubisoft Connect", async () => await DownloadHelper.Download("https://static3.cdn.ubi.com/orbit/launcher_installer/UbisoftConnectInstaller.exe", Path.GetTempPath(), "UbisoftConnectInstaller.exe", reporter: reporter), () => UbisoftConnect == true),

			// install ubisoft connect
			("Installing Ubisoft Connect", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Path.GetTempPath(), "UbisoftConnectInstaller.exe"), Arguments = "/S" , WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), () => UbisoftConnect == true),
			("Cleaning up Ubisoft Connect files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "UbisoftConnectInstaller.exe")), () => UbisoftConnect == true),

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

			// disable ea startup entry
			("Disabling EA startup entry", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run", "EADM", new byte[] { 0x01 }, RegistryValueKind.Binary), () => EA == true),

			// remove ea desktop shortcut
			("Removing EA desktop shortcut", async () => File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory), "EA.lnk")), () => EA == true),

			// download battle.net
			("Downloading Battle.Net", async () => await DownloadHelper.Download("https://downloader.battle.net//download/getInstallerForGame?os=win&gameProgram=BATTLENET_APP&version=Live", Path.GetTempPath(), "Battle.net-Setup.exe", reporter: reporter), () => BattleNet == true),

			// install battle.net
			("Installing Battle.Net", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Path.GetTempPath(), "Battle.net-Setup.exe"), Arguments = $@"--lang=enUS --installpath=""{Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)}\Battle.net""" , WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), () => BattleNet == true),
			("Cleaning up Battle.Net files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "Battle.net-Setup.exe")), () => BattleNet == true),

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

			// remove minecraft launcher desktop shortcut
			("Removing Minecraft Launcher desktop shortcut", async () => File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory), "Minecraft Launcher.lnk")), () => MinecraftLauncher == true),

			// download curseforge
			("Downloading CurseForge", async () => await DownloadHelper.Download("https://curseforge.overwolf.com/downloads/curseforge-latest-win64.exe", Path.GetTempPath(), "CurseForge-Setup.exe", reporter: reporter), () => CurseForge == true),

			// install curseforge
			("Installing CurseForge", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Path.GetTempPath(), "CurseForge-Setup.exe"), Arguments = "/S" , WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), () => CurseForge == true),
			("Cleaning up CurseForge files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "CurseForge-Setup.exe")), () => CurseForge == true),

			// remove curseforge desktop shortcut
			("Removing CurseForge desktop shortcut", async () => File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "CurseForge.lnk")), () => CurseForge == true),

			// download lunar client
			("Downloading Lunar Client", async () => await DownloadHelper.Download("https://launcherupdates.lunarclientcdn.com/Lunar%20Client%20v3.4.9.exe", Path.GetTempPath(), "Lunar Client.exe", reporter: reporter), () => LunarClient == true),

			// install lunar client
			("Installing Lunar Client", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Path.GetTempPath(), "Lunar Client.exe"), Arguments = "/S" , WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), () => LunarClient == true),
			("Cleaning up Lunar Client files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "Lunar Client.exe")), () => LunarClient == true),

			// remove lunar client desktop shortcut
			("Removing Lunar Client desktop shortcut", async () => File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Lunar Client.lnk")), () => LunarClient == true),

			// download feather client
			("Downloading Feather Client", async () => await DownloadHelper.Download("https://feathermc.com/download/windows", Path.GetTempPath(), "Feather Launcher Setup.exe", reporter: reporter), () => FeatherClient == true),

			// install feather client
			("Installing Feather Client", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Path.GetTempPath(), "Feather Launcher Setup.exe"), Arguments = "/S" , WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), () => FeatherClient == true),
			("Cleaning up Feather Client files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "Feather Launcher Setup.exe")), () => FeatherClient == true),

			// remove feather client desktop shortcut
			("Removing Feather Client desktop shortcut", async () => File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Feather Launcher.lnk")), () => FeatherClient == true),

			// download norisk client
			("Downloading NoRisk Client", async () => await DownloadHelper.Download("https://github.com/NoRiskClient/noriskclient-launcher/releases/latest/download/NoRiskClient-Windows-setup.exe", Path.GetTempPath(), "NoRiskClient-Windows-setup.exe", reporter: reporter), () => NoRiskClient == true),

			// install norisk client
			("Installing NoRisk Client", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Path.GetTempPath(), "NoRiskClient-Windows-setup.exe"), Arguments = "/S" , WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), () => NoRiskClient == true),
			("Cleaning up NoRisk Client files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "NoRiskClient-Windows-setup.exe")), () => NoRiskClient == true),

			// remove norisk client desktop shortcut 
			("Removing NoRisk Client desktop shortcut", async () => File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "NoRisk Launcher.lnk")), () => NoRiskClient == true),

			// download prism launcher
			("Downloading Prism Launcher", async () => await DownloadHelper.Download(JsonDocument.Parse(await new HttpClient { DefaultRequestHeaders = { { "User-Agent", "AutoOS" } } }.GetStringAsync("https://api.github.com/repos/PrismLauncher/PrismLauncher/releases")).RootElement.EnumerateArray().First(release => !release.GetProperty("prerelease").GetBoolean() && release.GetProperty("assets").EnumerateArray().Any(asset => asset.GetProperty("name").GetString().Contains("Windows-MSVC") && !asset.GetProperty("name").GetString().Contains("arm64") && !asset.GetProperty("name").GetString().Contains("Portable") && asset.GetProperty("name").GetString().EndsWith(".exe"))).GetProperty("assets").EnumerateArray().First(asset => asset.GetProperty("name").GetString().Contains("Windows-MSVC") && !asset.GetProperty("name").GetString().Contains("arm64") && !asset.GetProperty("name").GetString().Contains("Portable") && asset.GetProperty("name").GetString().EndsWith(".exe")).GetProperty("browser_download_url").GetString(), Path.GetTempPath(), "PrismLauncher-Windows-MSVC-Setup.exe", reporter: reporter), () => PrismLauncher == true),

			// install prism launcher
			("Installing Prism Launcher", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Path.GetTempPath(), "PrismLauncher-Windows-MSVC-Setup.exe"), Arguments = "/S" , WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), () => PrismLauncher == true),
			("Cleaning up Prism Launcher files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "PrismLauncher-Windows-MSVC-Setup.exe")), () => PrismLauncher == true),

			// download bloxstrap
			("Downloading Bloxstrap", async () => await DownloadHelper.Download(JsonDocument.Parse(await new HttpClient { DefaultRequestHeaders = { { "User-Agent", "AutoOS" } } }.GetStringAsync("https://api.github.com/repos/bloxstraplabs/bloxstrap/releases")).RootElement.EnumerateArray().First(release => !release.GetProperty("prerelease").GetBoolean() && release.GetProperty("assets").EnumerateArray().Any(asset => asset.GetProperty("name").GetString().StartsWith("Bloxstrap-v") && asset.GetProperty("name").GetString().EndsWith(".exe"))).GetProperty("assets").EnumerateArray().First(asset => asset.GetProperty("name").GetString().StartsWith("Bloxstrap-v") && asset.GetProperty("name").GetString().EndsWith(".exe")).GetProperty("browser_download_url").GetString(), Path.GetTempPath(), "Bloxstrap.exe", reporter: reporter), () => Bloxstrap == true),

			// install bloxstrap
			("Installing Bloxstrap", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Path.GetTempPath(), "Bloxstrap.exe"), Arguments = "-quiet -nolaunch" , WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), () => Bloxstrap == true),
			("Cleaning up Bloxstrap files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "Bloxstrap.exe")), () => Bloxstrap == true),

			// remove bloxstrap desktop shortcut
			("Removing Bloxstrap desktop shortcut", async () => File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Bloxstrap.lnk")), () => Bloxstrap == true),

			// download froststrap
			("Downloading Froststrap", async () => await DownloadHelper.Download(JsonDocument.Parse(await new HttpClient { DefaultRequestHeaders = { { "User-Agent", "AutoOS" } } }.GetStringAsync("https://api.github.com/repos/Froststrap/Froststrap/releases")).RootElement.EnumerateArray().First(release => release.GetProperty("assets").EnumerateArray().Any(asset => asset.GetProperty("name").GetString().EndsWith(".exe"))).GetProperty("assets").EnumerateArray().First(asset => asset.GetProperty("name").GetString().EndsWith(".exe")).GetProperty("browser_download_url").GetString(), Path.GetTempPath(), "Froststrap.exe", reporter: reporter), () => Froststrap == true),

			// install froststrap
			("Installing Froststrap", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Path.GetTempPath(), "Froststrap.exe"), Arguments = "/S" , WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), () => Froststrap == true),
			("Installing Froststrap", async () => Directory.CreateDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft", "Windows", "Start Menu", "Programs", "Froststrap")), () => Froststrap == true),
			("Installing Froststrap", async () => ShortcutHelper.Create(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft", "Windows", "Start Menu", "Programs", "Froststrap", "Froststrap.lnk"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Froststrap", "Froststrap.exe")), () => Froststrap == true),
			("Cleaning up Froststrap files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "Froststrap.exe")), () => Froststrap == true),

			// remove froststrap desktop shortcut
			("Removing Froststrap desktop shortcut", async () => File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Froststrap.lnk")), () => Froststrap == true),

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
			("Installing Rockstar Games Launcher", async () => Directory.CreateDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft", "Windows", "Start Menu", "Programs", "Rockstar Games")), () => RockstarGamesLauncher == true),
			("Installing Rockstar Games Launcher", async () => ShortcutHelper.Create(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft", "Windows", "Start Menu", "Programs", "Rockstar Games", "Rockstar Games Launcher.lnk"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Rockstar Games", "Launcher", "LauncherPatcher.exe")), () => RockstarGamesLauncher == true),
			("Installing Rockstar Games Launcher", async () => Directory.CreateDirectory(Path.Combine(Path.GetPathRoot(Environment.SystemDirectory)!, "Users", "Default", "AppData", "Roaming", "Microsoft", "Windows", "Start Menu", "Programs", "Rockstar Games")), () => RockstarGamesLauncher == true),
			("Installing Rockstar Games Launcher", async () => ShortcutHelper.Create(Path.Combine(Path.GetPathRoot(Environment.SystemDirectory)!, "Users", "Default", "AppData", "Roaming", "Microsoft", "Windows", "Start Menu", "Programs", "Rockstar Games", "Rockstar Games Launcher.lnk"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Rockstar Games", "Launcher", "LauncherPatcher.exe")), () => RockstarGamesLauncher == true),
			("Cleaning up Rockstar Games Launcher files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "Rockstar-Games-Launcher.exe")), () => RockstarGamesLauncher == true),
			("Cleaning up Rockstar Games Launcher files", async () => Directory.Delete(Path.Combine(Path.GetTempPath(), "Rockstar-Games-Launcher"), true), () => RockstarGamesLauncher == true),

			// update rock star games launcher
			("Updating Rockstar Games Launcher", async () => { await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Rockstar Games", "Launcher", "LauncherPatcher.exe") , WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(); while (Process.GetProcessesByName("dxdiag").Length > 1) await Task.Delay(500); while (Process.GetProcessesByName("SocialClubHelper").Length == 0) await Task.Delay(500); }, () => RockstarGamesLauncher == true),

			// download fivem
			("Downloading FiveM", async () => await DownloadHelper.Download("https://github.com/tinodin/AutoOS-Resources/releases/download/v1.0.0.0/FiveM.zip", Path.GetTempPath(), "FiveM.zip", new InstallPageReporter()), () => FiveM == true),

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
			("Installing FiveM", async () => ShortcutHelper.Create(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft", "Windows", "Start Menu", "Programs", "FiveM.lnk"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FiveM", "FiveM.exe")), () => FiveM == true),
			("Installing FiveM", async () => ShortcutHelper.Create(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft", "Windows", "Start Menu", "Programs", "FiveM - Cfx.re Development Kit (FxDK).lnk"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FiveM", "FiveM - Cfx.re Development Kit (FxDK).lnk")), () => FiveM == true),
			("Cleaning up FiveM files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "FiveM.zip")), () => FiveM == true),
		
			// download faceit
			("Downloading FACEIT", async () => await DownloadHelper.Download("https://faceit-client.faceit-cdn.net/release/FACEIT-setup-latest.exe", Path.GetTempPath(), "FACEIT-setup-latest.exe", new InstallPageReporter()), () => FACEIT == true),

			// install faceit
			("Installing FACEIT", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Path.GetTempPath(), "FACEIT-setup-latest.exe"), Arguments = "/S", WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), () => FACEIT == true),
			("Cleaning up FACEIT files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "FACEIT-setup-latest.exe")), () => FACEIT == true),

			// remove faceit desktop shortcut 
			("Removing FACEIT desktop shortcut", async () => File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "FACEIT.lnk")), () => FACEIT == true),

			// disable faceit startup entry
			("Disabling FACEIT startup entry", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run", "FACEIT", new byte[] { 0x01 }, RegistryValueKind.Binary), () => FACEIT == true),

			// download faceit ac
			("Downloading FACEIT AC", async () => await DownloadHelper.Download("https://anticheat-client.faceit-cdn.net/FACEITInstaller_64.exe", Path.GetTempPath(), "FACEITInstaller_64.exe", new InstallPageReporter()), () => FACEITAC == true),

			// install faceit ac
			("Installing FACEIT AC", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Path.GetTempPath(), "FACEITInstaller_64.exe"), Arguments = @"/VERYSILENT /TASKS=""""", WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), () => FACEITAC == true),
			("Cleaning up FACEIT AC files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "FACEITInstaller_64.exe")), () => FACEITAC == true),

			// download Eden
			("Downloading Eden", async () => await DownloadHelper.Download("https://stable.eden-emu.dev/v0.2.1/Eden-Windows-v0.2.1-amd64-clang-pgo.zip", Path.GetTempPath(), "Eden-Windows-amd64-clang-pgo.zip"), () => Eden == true),

			// install eden
			("Installing Eden", async () => await ExtractHelper.Extract(Path.Combine(Path.GetTempPath(), "Eden-Windows-amd64-clang-pgo.zip"), Path.Combine(Path.GetTempPath(), "Eden")), () => Eden == true),
			("Installing Eden", async () => Directory.Move(Path.Combine(Path.GetTempPath(), "Eden"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Eden")), () => Eden == true),
			("Installing Eden", async () => ShortcutHelper.Create(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Microsoft", "Windows", "Start Menu", "Programs", "Eden.lnk"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Eden", "eden.exe")), () => Eden == true),
			("Installing Eden", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Eden", "DisplayName", "Eden", RegistryValueKind.String), () => Eden == true),
			("Installing Eden", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Eden", "UninstallString", $@"cmd /c rd /s /q ""{Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Eden")}"" & del ""{Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Microsoft", "Windows", "Start Menu", "Programs", "Eden.lnk")}"" & reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Eden"" /f", RegistryValueKind.String), () => Eden == true),
			("Installing Eden", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Eden", "DisplayIcon", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Eden", "eden.exe"), RegistryValueKind.String), () => Eden == true),
			("Installing Eden", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Eden", "Publisher", "Eden Emulator Project", RegistryValueKind.String), () => Eden == true),
			("Cleaning up Eden files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "Eden-Windows-amd64-clang-pgo.zip")), () => Eden == true),

			// download dolby access
			("Downloading Dolby Access", async () => await StoreHelper.Download("DolbyLaboratories.DolbyAccess_rz1tebttyb220", 1, reporter: reporter), () => AppleMusic == true),

			// install dolby access
			("Installing Dolby Access", async () => await StoreHelper.Install("DolbyLaboratories.DolbyAccess_rz1tebttyb220"), () => AppleMusic == true),

			// download apple music
			("Downloading Apple Music", async () => await StoreHelper.Download("AppleInc.AppleMusicWin_nzyj5cx40ttqa", reporter: reporter), () => AppleMusic == true),

			// install apple music
			("Installing Apple Music", async () => await StoreHelper.Install("AppleInc.AppleMusicWin_nzyj5cx40ttqa"), () => AppleMusic == true),
			
			// enable "keep miniplayer on top of all other windows"
			(@"Enabling ""Keep Miniplayer on top of all other windows""", async () => await Process.Start(new ProcessStartInfo { FileName = "reg.exe", Arguments = $@"load HKU\TEMP ""{Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Packages", "AppleInc.AppleMusicWin_nzyj5cx40ttqa", "Settings", "settings.dat")}""", CreateNoWindow = true })!.WaitForExitAsync(), () => AppleMusic == true),
			(@"Enabling ""Keep Miniplayer on top of all other windows""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_USERS\TEMP\LocalState", "KeepMiniplayerOnTop", RegistryHelper.ApplicationDataBoolean, [0x01, 0xb9, 0x5d, 0xcc, 0xe4, 0x9a, 0x13, 0xdc, 0x01]), () => AppleMusic == true),
			(@"Enabling ""Keep Miniplayer on top of all other windows""", async () => await Process.Start(new ProcessStartInfo { FileName = "reg.exe", Arguments = @"unload HKU\TEMP", CreateNoWindow = true })!.WaitForExitAsync(), () => AppleMusic == true),
			(@"Enabling ""Keep Miniplayer on top of all other windows""", async () => { try { FileSystem.CopyDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Packages", "AppleInc.AppleMusicWin_nzyj5cx40ttqa"), Path.Combine(Path.GetPathRoot(Environment.SystemDirectory)!, "Users", "Default", "AppData", "Local", "Packages", "AppleInc.AppleMusicWin_nzyj5cx40ttqa"), true); } catch { } }, () => AppleMusic == true),

			// pin apple music to the taskbar
			("Pinning Apple Music to the taskbar", async () => await ProcessActions.PinToTaskbar("UWA", "AppleInc.AppleMusicWin_nzyj5cx40ttqa!App"), () => AppleMusic == true),

			// download tidal
			("Downloading TIDAL", async () => await StoreHelper.Download("WiMPMusic.27241E05630EA_kn85bz84x7te4", reporter: reporter), () => Tidal == true),

			// install tidal
			("Installing TIDAL", async () => await StoreHelper.Install("WiMPMusic.27241E05630EA_kn85bz84x7te4"), () => Tidal == true),

			// pin tidal to the taskbar
			("Pinning TIDAL to the taskbar", async () => await ProcessActions.PinToTaskbar("UWA", "WiMPMusic.27241E05630EA_kn85bz84x7te4!TIDAL"), () => Tidal == true),

			// download qobuz
			("Downloading Qobuz", async () => await DownloadHelper.Download("https://desktop.qobuz.com/releases/win32/x64/windows7_8_10/8.1.0-b019/Qobuz_Installer.exe", Path.GetTempPath(), "Qobuz_Installer.exe", reporter: reporter), () => Qobuz == true),

			// install qobuz
			("Installing Qobuz", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Path.GetTempPath(), "Qobuz_Installer.exe"), Arguments = "-s" , WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), () => Qobuz == true),
			("Cleaning up Qobuz files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "Qobuz_Installer.exe")), () => Qobuz == true),

			// pin qobuz to the taskbar
			("Pinning Qobuz to the taskbar", async () => await ProcessActions.PinToTaskbar("Link", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft", "Windows", "Start Menu", "Programs", "Qobuz", "Qobuz.lnk")), () => Qobuz == true),

			// download amazon music
			("Downloading Amazon Music", async () => await StoreHelper.Download("AmazonMobileLLC.AmazonMusic_kc6t79cpj4tp0", reporter: reporter), () => AmazonMusic == true),

			// install amazon music
			("Installing Amazon Music", async () => await StoreHelper.Install("AmazonMobileLLC.AmazonMusic_kc6t79cpj4tp0"), () => AmazonMusic == true),

			// pin amazon music to the taskbar
			("Pinning Amazon Music to the taskbar", async () => await ProcessActions.PinToTaskbar("UWA", "AmazonMobileLLC.AmazonMusic_kc6t79cpj4tp0!AmazonMobileLLC.AmazonMusic"), () => AmazonMusic == true),

			// download deezer music
			("Downloading Deezer Music", async () => await StoreHelper.Download("Deezer.62021768415AF_q7m17pa7q8kj0", reporter: reporter), () => DeezerMusic == true),

			// install deezer music
			("Installing Deezer Music", async () => await StoreHelper.Install("Deezer.62021768415AF_q7m17pa7q8kj0"), () => DeezerMusic == true),

			// pin deezer music to the taskbar
			("Pinning Deezer Music to the taskbar", async () => await ProcessActions.PinToTaskbar("UWA", "Deezer.62021768415AF_q7m17pa7q8kj0!Deezer.Music"), () => DeezerMusic == true),

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
			("Installing Spotify", async () => ShortcutHelper.Create(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft", "Windows", "Start Menu", "Programs", "Spotify.lnk"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Spotify", "Spotify.exe")), () => Spotify == true),
			("Cleaning up Spotify files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "SpotifyFullSetupX64.exe")), () => Spotify == true),

			// pin spotify to the taskbar
			("Pinning Spotify to the taskbar", async () => await ProcessActions.PinToTaskbar("Link", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft", "Windows", "Start Menu", "Programs", "Spotify.lnk")), () => Spotify == true),

			// download spotx
			("Downloading SpotX", async () => await DownloadHelper.Download("https://raw.githubusercontent.com/SpotX-Official/SpotX/main/run.ps1", Path.GetTempPath(), "run.ps1", reporter: reporter), () => Spotify == true),

			// install spotx
			("Installing SpotX", async () => await ProcessActions.RunPowerShell($@"& ""{Path.Combine(Path.GetTempPath(), "run.ps1")}"" -new_theme -adsections_off -podcasts_off -block_update_off -version {spotifyVersion}-1234"), () => Spotify == true),

			// remove spotify desktop shortcut
			("Removing Spotify desktop shortcut", async () => File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Spotify.lnk")), () => Spotify == true),

			// disable spotify startup entry
			("Disabling Spotify startup entry", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run", "Spotify", new byte[] { 0x01 }, RegistryValueKind.Binary), () => Spotify == true),

			// download musicbee
			("Downloading MusicBee", async () => await StoreHelper.Download("50072StevenMayall.MusicBee_kcr266et74avj", reporter: reporter), () => MusicBee == true),

			// install musicbee
			("Installing MusicBee", async () => await StoreHelper.Install("50072StevenMayall.MusicBee_kcr266et74avj"), () => MusicBee == true),

			// pin musicbee to the taskbar
			("Pinning MusicBee to the taskbar", async () => await ProcessActions.PinToTaskbar("UWA", "50072StevenMayall.MusicBee_kcr266et74avj!MusicBeePackage"), () => MusicBee == true),

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
			("Installing Logitech Onboard Memory Manager", async () => ShortcutHelper.Create(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Microsoft", "Windows", "Start Menu", "Programs", "Logitech Onboard Memory Manager.lnk"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Logitech Onboard Memory Manager", "OnboardMemoryManager.exe")), () => LogitechOnboardMemoryManager == true),
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
			("Installing Wootility", async () => ShortcutHelper.Create(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft", "Windows", "Start Menu", "Programs", "Wootility.lnk"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "wootility", "Wootility.exe")), () => Wootility == true),
			("Cleaning up Wootility files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "WootilitySetup.exe")), () => Wootility == true),
			("Cleaning up Wootility files", async () => Directory.Delete(Path.Combine(Path.GetTempPath(), "WootilitySetup"), true), () => Wootility == true),

			// download g-menu
			("Downloading G-Menu", async () => await DownloadHelper.Download("https://gz-dlsw.tpv-tech.com/MNT/AOC/Software/G-menu/G-Menu.zip", Path.GetTempPath(), "G-Menu.zip", reporter: reporter), () => GMenu == true),

			// install g-menu
			("Installing G-Menu", async () => await ExtractHelper.Extract(Path.Combine(Path.GetTempPath(), "G-Menu.zip"), Path.Combine(Path.GetTempPath(), "G-Menu")), () => GMenu == true),
			("Installing G-Menu", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Path.GetTempPath(), "G-Menu", "G-Menu Setup 3.34.0.exe"), Arguments = "/S", WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), () => GMenu == true),
			("Cleaning up G-Menu files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "G-Menu.zip")), () => GMenu == true),
			("Cleaning up G-Menu files", async () => Directory.Delete(Path.Combine(Path.GetTempPath(), "G-Menu"), true), () => GMenu == true),

			// remove g-menu desktop shortcut
			("Removing G-Menu desktop shortcut", async () => File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "G-Menu.lnk")), () => GMenu == true),

			// download endgame gear
			("Downloading Endgame Gear", async () => await DownloadHelper.Download("https://img.endgamegear.com/downloads/Endgame_Gear_Setup_V1.0.19.06232.exe", Path.GetTempPath(), "Endgame_Gear_Setup.exe", reporter: reporter), () => EndgameGear == true),

			// install endgame gear
			("Installing Endgame Gear", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Path.GetTempPath(), "Endgame_Gear_Setup.exe"), Arguments = "/VERYSILENT /SUPPRESSMSGBOXES /NORESTART" , WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), () => EndgameGear == true),
			("Cleaning up Endgame Gear files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "Endgame_Gear_Setup.exe")), () => EndgameGear == true),

			// disabling endgame gear startup entries
			("Disabling Endgame Gear startup entries", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run", "Endgame Gear Utility Startup", new byte[] { 0x03 }, RegistryValueKind.Binary), () => EndgameGear == true),

			// remove endgame gear desktop shortcut
			("Removing Endgame Gear desktop shortcut", async () => File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory), "Endgame Gear.lnk")), () => EndgameGear == true),

			// download glorious core
			("Downloading Glorious CORE", async () => await DownloadHelper.Download("https://gloriouscore.nyc3.digitaloceanspaces.com/CORE2/app/GloriousCORE_2.1.15_Setup.zip", Path.GetTempPath(), "GloriousCORE_Setup.zip"), () => GloriousCORE == true),

			// install glorious core
			("Installing Glorious CORE", async () => await ExtractHelper.Extract(Path.Combine(Path.GetTempPath(), "GloriousCORE_Setup.zip"), Path.Combine(Path.GetTempPath(), "GloriousCORE_Setup")), () => GloriousCORE == true),
			("Installing Glorious CORE", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Path.GetTempPath(), "GloriousCORE_Setup", "GloriousCORE_2.1.15_Setup.exe"), Arguments = "/VERYSILENT /SUPPRESSMSGBOXES /NORESTART" , WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), () => GloriousCORE == true),
			("Cleaning up Glorious CORE files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "GloriousCORE_Setup.zip")), () => GloriousCORE == true),
			("Cleaning up Glorious CORE files", async () => Directory.Delete(Path.Combine(Path.GetTempPath(), "GloriousCORE_Setup"), true), () => GloriousCORE == true),

			// remove glorious core desktop shortcut
			("Removing Glorious CORE desktop shortcut", async () => File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory), "Glorious CORE.lnk")), () => GloriousCORE == true),
			
			// download mchose hub
			("Downloading MCHOSE HUB", async () => await DownloadHelper.Download("https://github.com/tinodin/AutoOS-Resources/releases/download/v1.0.0.0/MCHOSE.HUB.installer.exe", Path.GetTempPath(), "MCHOSE.HUB.installer.exe", reporter: reporter), () => MCHOSE == true),

			// install mchose hub
			("Installing MCHOSE HUB", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Path.GetTempPath(), "MCHOSE.HUB.installer.exe"), Arguments = "/S" , WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), () => MCHOSE == true),
			("Cleaning up MCHOSE HUB files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "MCHOSE.HUB.installer.exe")), () => MCHOSE == true),

			// remove mchose hub desktop shortcut
			("Removing MCHOSE HUB desktop shortcut", async () => File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory), "MCHOSE HUB.lnk")), () => MCHOSE == true),

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
			("Downloading Razer Synapse", async () => await DownloadHelper.Download("https://raw.githubusercontent.com/tinodin/AutoOS-Resources/main/Files/Razer/Razer.zip", Path.GetTempPath(), "Razer.zip", reporter: reporter), () => RazerSynapse == true),
			("Downloading Razer Synapse", async () => await DownloadHelper.Download("https://raw.githubusercontent.com/tinodin/AutoOS-Resources/main/Files/Razer/leveldb.zip", Path.GetTempPath(), "leveldb.zip", reporter: reporter), () => RazerSynapse == true),

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
			("Disabling Corsair iCUE services", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\CorsairService", "Start", 3, RegistryValueKind.DWord), () => CorsairICue == true),
			("Disabling Corsair iCUE services", async () => ServicesHelper.StopService("CorsairService"), () => CorsairICue == true),

			// disable corsair icue startup entry
			("Disabling Corsair iCUE startup entry", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run", "Corsair iCUE5 Software", new byte[] { 0x01 }, RegistryValueKind.Binary), () => CorsairICue == true),

			// remove corsair icue desktop shortcut
			("Removing Corsair iCUE desktop shortcut", async () => File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory), "iCUE.lnk")), () => CorsairICue == true),

			// download openrgb
			("Downloading OpenRGB", async () => await DownloadHelper.Download(JsonDocument.Parse(await new HttpClient { DefaultRequestHeaders = { { "User-Agent", "AutoOS" } } }.GetStringAsync("https://api.github.com/repos/CalcProgrammer1/OpenRGB/releases")).RootElement.EnumerateArray().First(release => release.GetProperty("assets").EnumerateArray().Any(asset => asset.GetProperty("name").GetString().Contains("Windows_64") && asset.GetProperty("name").GetString().EndsWith(".msi"))).GetProperty("assets").EnumerateArray().First(asset => asset.GetProperty("name").GetString().Contains("Windows_64") && asset.GetProperty("name").GetString().EndsWith(".msi")).GetProperty("browser_download_url").GetString(), Path.GetTempPath(), "OpenRGB_Windows_64.msi", reporter: reporter), () => OpenRGB == true),

			// install openrgb
			("Installing OpenRGB", async () => await Process.Start(new ProcessStartInfo { FileName = "msiexec.exe", Arguments = $@"/i ""{Path.Combine(Path.GetTempPath(), "OpenRGB_Windows_64.msi")}"" /qn" , WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), () => OpenRGB ==  true),
			("Cleaning up OpenRGB files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "OpenRGB_Windows_64.msi")), () => OpenRGB ==  true),

			// download fancontrol
			("Downloading FanControl", async () => await DownloadHelper.Download(JsonDocument.Parse(await new HttpClient { DefaultRequestHeaders = { { "User-Agent", "AutoOS" } } }.GetStringAsync("https://api.github.com/repos/Rem0o/FanControl.Releases/releases")).RootElement.EnumerateArray().First(release => !release.GetProperty("prerelease").GetBoolean() && release.GetProperty("assets").EnumerateArray().Any(asset => asset.GetProperty("name").GetString().Contains("Installer.exe"))).GetProperty("assets").EnumerateArray().First(asset => asset.GetProperty("name").GetString().Contains("Installer.exe")).GetProperty("browser_download_url").GetString(), Path.GetTempPath(), "FanControl.exe", reporter: reporter), () => FanControl == true),

			// install fancontrol
			("Installing FanControl", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Path.GetTempPath(), "FanControl.exe"), Arguments = "/VERYSILENT /SUPPRESSMSGBOXES /NORESTART", WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), () => FanControl == true),
			("Cleaning up FanControl files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "FanControl.exe")), () => FanControl == true),

			// download ghelper
			("Downloading GHelper", async () => await DownloadHelper.Download("https://github.com/seerge/g-helper/releases/latest/download/GHelper.exe", Path.GetTempPath(), "GHelper.exe"), () => GHelper == true),

			// install ghelper
			("Installing GHelper", async () => Directory.CreateDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "GHelper")), () => GHelper == true),
			("Installing GHelper", async () => File.Move(Path.Combine(Path.GetTempPath(), "GHelper.exe"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "GHelper", "GHelper.exe"), true), () => GHelper == true),
			("Installing GHelper", async () => ShortcutHelper.Create(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Microsoft", "Windows", "Start Menu", "Programs", "GHelper.lnk"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "GHelper", "GHelper.exe")), () => GHelper == true),
			("Installing GHelper", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\GHelper", "DisplayName", "GHelper", RegistryValueKind.String), () => GHelper == true),
			("Installing GHelper", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\GHelper", "UninstallString", $@"cmd /c rd /s /q ""{Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "GHelper")}"" & del ""{Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Microsoft", "Windows", "Start Menu", "Programs", "GHelper.lnk")}"" & reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\GHelper"" /f", RegistryValueKind.String), () => GHelper == true),
			("Installing GHelper", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\GHelper", "DisplayIcon", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), @"GHelper\GHelper.exe"), RegistryValueKind.String), () => GHelper == true),
			("Installing GHelper", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\GHelper", "Publisher", "Seerge", RegistryValueKind.String), () => GHelper == true),
			("Installing GHelper", async () => await Task.Delay(500), () => GHelper == true),

			//download vigembus
			("Downloading ViGEmBus", async () => await DownloadHelper.Download(JsonDocument.Parse(await new HttpClient { DefaultRequestHeaders = { { "User-Agent", "AutoOS" } } }.GetStringAsync("https://api.github.com/repos/nefarius/ViGEmBus/releases/latest")).RootElement.GetProperty("assets").EnumerateArray().First(a => a.GetProperty("name").GetString().Contains("x64_x86_arm64.exe")).GetProperty("browser_download_url").GetString(), Path.GetTempPath(), "ViGEmBusx64_x86_arm64.exe", reporter: reporter), () => ViGEmBus == true),

			// install vigembus
			("Installing ViGEmBus", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Path.GetTempPath(), "ViGEmBusx64_x86_arm64.exe"), Arguments = "/qn /NORESTART" , WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), () => ViGEmBus == true),
			("Cleaning up ViGEmBus files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "ViGEmBusx64_x86_arm64.exe")), () => ViGEmBus == true),

			//download hidhide
			("Downloading HidHide", async () => await DownloadHelper.Download(JsonDocument.Parse(await new HttpClient { DefaultRequestHeaders = { { "User-Agent", "AutoOS" } } }.GetStringAsync("https://api.github.com/repos/nefarius/HidHide/releases/latest")).RootElement.GetProperty("assets").EnumerateArray().First(a => a.GetProperty("name").GetString().Contains("x64.exe")).GetProperty("browser_download_url").GetString(), Path.GetTempPath(), "HidHide_x64.exe", reporter: reporter), () => HidHide == true),

			// install hidhide
			("Installing HidHide", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Path.GetTempPath(), "HidHide_x64.exe"), Arguments = "/qn /NORESTART" , WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), () => HidHide == true),
			("Cleaning up HidHide files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "HidHide_x64.exe")), () => HidHide == true),

			// disable hidhide service
			("Disabling HidHide service", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\HidHideWatchdog.exe", "Start", 4, RegistryValueKind.DWord), () => HidHide == true),
			("Disabling HidHide service", async () => ServicesHelper.StopService("HidHideWatchdog.exe"), () => HidHide == true),

			// disable hidhide startup entry
			("Disabling HidHide startup entry", async () => TaskSchedulerHelper.Toggle(@"nefarius_HidHide_Updater", false), () => HidHide == true),

			// download dualsensey
			("Downloading DualSenseY", async () => await DownloadHelper.Download("https://github.com/WujekFoliarz/DualSenseY-v2/releases/latest/download/x64-release.zip", Path.GetTempPath(), "x64-release.zip"), () => DualSenseY == true),

			// install dualsensey
			("Installing DualSenseY", async () => await ExtractHelper.Extract(Path.Combine(Path.GetTempPath(), "x64-release.zip"), Path.Combine(Path.GetTempPath(), "DualSenseY")), () => DualSenseY == true),
			("Installing DualSenseY", async () => Directory.Move(Path.Combine(Path.GetTempPath(), "DualSenseY"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "DualSenseY")), () => DualSenseY == true),
			("Installing DualSenseY", async () => ShortcutHelper.Create(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Microsoft", "Windows", "Start Menu", "Programs", "DualSenseY.lnk"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "DualSenseY", "DualSenseY.exe")), () => DualSenseY == true),
			("Installing DualSenseY", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\DualSenseY", "DisplayName", "DualSenseY", RegistryValueKind.String), () => DualSenseY == true),
			("Installing DualSenseY", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\DualSenseY", "UninstallString", $@"cmd /c rd /s /q ""{Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "DualSenseY")}"" & del ""{Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Microsoft", "Windows", "Start Menu", "Programs", "DualSenseY.lnk")}"" & reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\DualSenseY"" /f", RegistryValueKind.String), () => DualSenseY == true),
			("Installing DualSenseY", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\DualSenseY", "DisplayIcon", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), @"DualSenseY\DualSenseY.exe"), RegistryValueKind.String), () => DualSenseY == true),
			("Installing DualSenseY", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\DualSenseY", "Publisher", "WujekFoliarz", RegistryValueKind.String), () => DualSenseY == true),
			("Installing DualSenseY", async () => await Task.Delay(500), () => DualSenseY == true),
			("Cleaning up DualSenseY files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "x64-release.zip")), () => DualSenseY == true),

			// download raceelement
			("Downloading RaceElement", async () => await DownloadHelper.Download("https://github.com/RiddleTime/Race-Element/releases/latest/download/RaceElement.exe", Path.GetTempPath(), "RaceElement.exe"), () => RaceElement == true),

			// install raceelement
			("Installing RaceElement", async () => Directory.CreateDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "RaceElement")), () => RaceElement == true),
			("Installing RaceElement", async () => File.Move(Path.Combine(Path.GetTempPath(), "RaceElement.exe"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "RaceElement", "RaceElement.exe"), true), () => RaceElement == true),
			("Installing RaceElement", async () => ShortcutHelper.Create(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Microsoft", "Windows", "Start Menu", "Programs", "RaceElement.lnk"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "RaceElement", "RaceElement.exe")), () => RaceElement == true),
			("Installing RaceElement", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\RaceElement", "DisplayName", "RaceElement", RegistryValueKind.String), () => RaceElement == true),
			("Installing RaceElement", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\RaceElement", "UninstallString", $@"cmd /c rd /s /q ""{Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "RaceElement")}"" & del ""{Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Microsoft", "Windows", "Start Menu", "Programs", "RaceElement.lnk")}"" & reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\RaceElement"" /f", RegistryValueKind.String), () => RaceElement == true),
			("Installing RaceElement", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\RaceElement", "DisplayIcon", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "RaceElement", "RaceElement.exe"), RegistryValueKind.String), () => RaceElement == true),
			("Installing RaceElement", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\RaceElement", "Publisher", "RiddleTime", RegistryValueKind.String), () => RaceElement == true),
			("Installing RaceElement", async () => await Task.Delay(500), () => RaceElement == true),

			//download playstation accessories
			("Downloading PlayStation® Accessories", async () => await DownloadHelper.Download("https://fwupdater.dl.playstation.net/fwupdater/PlayStationAccessoriesInstaller.exe", Path.GetTempPath(), "PlayStationAccessoriesInstaller.exe", reporter: reporter), () => PlaystationAccessories == true),

			// install playstation accessories
			("Installing PlayStation® Accessories", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Path.GetTempPath(), "PlayStationAccessoriesInstaller.exe"), Arguments = "/S /v/qn" , WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), () => PlaystationAccessories == true),
			("Cleaning up PlayStation® Accessories files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "PlayStationAccessoriesInstaller.exe")), () => PlaystationAccessories == true),

			// set playstation accessories data collection to limited
			("Setting PlayStation® Accessories data collection to limited", async () => await File.WriteAllTextAsync(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Sony Corporation", "PlayStationAccessories", "AppSettings.json"), new JsonObject { ["IsAgreedFullDataCollection"] = false, ["IsAnsweredDataCollection"] = true, ["IsCheckedDisabledButtonMessage"] = false, ["IsCheckedUpdateInfo2_0_0_0"] = true, ["IsCompletedEdgeWelcomeFlow"] = false }.ToString()), () => PlaystationAccessories == true),

			// remove playstation accessories desktop shortcut
			("Removing PlayStation® Accessories desktop shortcut", async () => File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory), "PlayStation® Accessories.lnk")), () => PlaystationAccessories == true),

			//download xbox accessories
			("Downloading Xbox Accessories", async () => await StoreHelper.Download("Microsoft.XboxDevices_8wekyb3d8bbwe", reporter: reporter), () => XboxAccessories == true),

			// install xbox accessories
			("Installing Xbox Accessories", async () => await StoreHelper.Install("Microsoft.XboxDevices_8wekyb3d8bbwe"), () => XboxAccessories == true),

			// download visual studio
			("Downloading Visual Studio", async () => await DownloadHelper.Download("https://aka.ms/vs/stable/vs_community.exe", Path.GetTempPath(), "vs_Community.exe", reporter: reporter), () => VisualStudio == true),

			// install visual studio
			("Installing Visual Studio", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Path.GetTempPath(), "vs_Community.exe"), Arguments = "--quiet --wait" , WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), () => VisualStudio == true),
			("Cleaning up Visual Studio files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "vs_Community.exe")), () => VisualStudio == true),

			// optimize visual studio
			("Optimizing Visual Studio", async () => { while (Process.GetProcessesByName("VSNgenRunner").Length == 1) await Task.Delay(500); }, () => VisualStudio == true),

			// disable visual studio startup entry
			("Disabling Visual Studio startup entry", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\VSStandardCollectorService150", "Start", 4, RegistryValueKind.DWord), () => VisualStudio == true),
			("Disabling Visual Studio startup entry", async () => ServicesHelper.StopService("VSStandardCollectorService150"), () => VisualStudio == true),

			// pin visual studio to the taskbar
			("Pinning Visual Studio to the taskbar", async () => await ProcessActions.PinToTaskbar("Link", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Microsoft", "Windows", "Start Menu", "Programs", "Visual Studio.lnk")), () => VisualStudio == true),

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
			("Pinning Visual Studio Code to the taskbar", async () => await ProcessActions.PinToTaskbar("Link", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft", "Windows", "Start Menu", "Programs", "Visual Studio Code", "Visual Studio Code.lnk")), () => VisualStudioCode == true),

			// download antigravity ide
			("Downloading Antigravity IDE", async () => await DownloadHelper.Download("https://edgedl.me.gvt1.com/edgedl/release2/j0qc3/antigravity/stable/2.1.1-6123990880747520/windows-x64/Antigravity%20IDE.exe", Path.GetTempPath(), "Antigravity.exe", reporter: reporter), () => Antigravity == true),

			// install antigravity ide
			("Installing Antigravity IDE", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Path.GetTempPath(), "Antigravity.exe"), Arguments = "/VERYSILENT /NORESTART /MERGETASKS=!runcode" , WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), () => Antigravity == true),
			("Cleaning up Antigravity IDE files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "Antigravity.exe")), () => Antigravity == true),

			// pin antigravity ide to the taskbar
			("Pinning Antigravity IDE to the taskbar", async () => await ProcessActions.PinToTaskbar("Link", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft", "Windows", "Start Menu", "Programs", "Antigravity IDE", "Antigravity IDE.lnk")), () => Antigravity == true),

			// download cursor
			("Downloading Cursor", async () => await DownloadHelper.Download("https://api2.cursor.sh/updates/download/golden/win32-x64/cursor/3.5", Path.GetTempPath(), "CursorSetup-x64.exe", reporter: reporter), () => Cursor == true),

			// install cursor
			("Installing Cursor", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Path.GetTempPath(), "CursorSetup-x64.exe"), Arguments = "/VERYSILENT /NORESTART /MERGETASKS=!runcode" , WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), () => Cursor == true),
			("Cleaning up Cursor files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "CursorSetup-x64.exe")), () => Cursor == true),

			// pin cursor to the taskbar
			("Pinning Cursor to the taskbar", async () => await ProcessActions.PinToTaskbar("Link", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Microsoft", "Windows", "Start Menu", "Programs", "Cursor", "Cursor.lnk")), () => Cursor == true),

			// download devin
			("Downloading Devin", async () => await DownloadHelper.Download("https://windsurf.com/api/windsurf/download-redirect?build=win32-x64-user&isNext=false", Path.GetTempPath(), "DevinUserSetup-x64.exe", reporter: reporter), () => Devin == true),

			// install devin
			("Installing Devin", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Path.GetTempPath(), "DevinUserSetup-x64.exe"), Arguments = "/VERYSILENT /NORESTART /MERGETASKS=!runcode" , WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), () => Devin == true),
			("Cleaning up Devin files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "DevinUserSetup-x64.exe")), () => Devin == true),

			// pin devin to the taskbar
			("Pinning Devin to the taskbar", async () => await ProcessActions.PinToTaskbar("Link", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft", "Windows", "Start Menu", "Programs", "Devin", "Devin.lnk")), () => Devin == true),

			// download kiro
			("Downloading Kiro", async () => await DownloadHelper.Download("https://prod.download.desktop.kiro.dev/releases/stable/win32-x64/signed/1.0.182/kiro-ide-1.0.182-stable-win32-x64.exe", Path.GetTempPath(), "kiro-ide-win32-x64.exe", reporter: reporter), () => Kiro == true),

			// install kiro
			("Installing Kiro", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Path.GetTempPath(), "kiro-ide-win32-x64.exe"), Arguments = "/SP- /VERYSILENT /SUPPRESSMSGBOXES /NORESTART", WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), () => Kiro == true),
			("Installing Kiro", async () => { foreach (Process process in Process.GetProcessesByName("Kiro")) { process.Kill(); process.WaitForExit(); } }, () => Kiro == true),
			("Cleaning up Kiro files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "kiro-ide-win32-x64.exe")), () => Kiro == true),

			// pin kiro to the taskbar
			("Pinning Kiro to the taskbar", async () => await ProcessActions.PinToTaskbar("Link", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft", "Windows", "Start Menu", "Programs", "Kiro", "Kiro.lnk")), () => Kiro == true),

			// download opencode
			("Downloading OpenCode", async () => await DownloadHelper.Download("https://opencode.ai/download/stable/windows-x64-nsis", Path.GetTempPath(), "OpenCode Desktop Installer.exe", reporter: reporter), () => OpenCode == true),

			// install opencode
			("Installing OpenCode", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Path.GetTempPath(), "OpenCode Desktop Installer.exe"), Arguments = "/S", WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), () => OpenCode == true),
			("Cleaning up OpenCode files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "OpenCode Desktop Installer.exe")), () => OpenCode == true),
			("Removing OpenCode desktop shortcut", async () => File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "OpenCode.lnk")), () => OpenCode == true),

			// pin opencode to the taskbar
			("Pinning OpenCode to the taskbar", async () => await ProcessActions.PinToTaskbar("Link", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft", "Windows", "Start Menu", "Programs", "OpenCode.lnk")), () => OpenCode == true),

			// download sublime text
			("Downloading Sublime Text", async () => await DownloadHelper.Download("https://download.sublimetext.com/sublime_text_build_4200_x64_setup.exe", Path.GetTempPath(), "sublime_text_x64_setup.exe", reporter: reporter), () => SublimeText == true),

			// install sublime text
			("Installing Sublime Text", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Path.GetTempPath(), "sublime_text_x64_setup.exe"), Arguments = "/SP- /VERYSILENT /SUPPRESSMSGBOXES /NORESTART", WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), () => SublimeText == true),
			("Cleaning up Sublime Text files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "sublime_text_x64_setup.exe")), () => SublimeText == true),

			// download intellij idea
			("Downloading IntelliJ IDEA", async () => await DownloadHelper.Download("https://download.jetbrains.com/idea/idea-2026.1.3.exe", Path.GetTempPath(), "idea.exe", reporter: reporter), () => IDEA == true),
			
			// install intellij idea
			("Installing IntelliJ IDEA", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Path.GetTempPath(), "idea.exe"), Arguments = "/S /NCRC", WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), () => IDEA == true),
			("Cleaning up IntelliJ IDEA files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "idea.exe")), () => IDEA == true),

			// pin intellij idea to taskbar
			("Pinning IntelliJ IDEA to the taskbar", async () => await ProcessActions.PinToTaskbar("Link", @"C:\ProgramData\Microsoft\Windows\Start Menu\Programs\JetBrains\IntelliJ IDEA 2026.1.3.lnk"), () => IDEA == true),

			// download winmerge
			("Downloading WinMerge", async () => await DownloadHelper.Download(JsonDocument.Parse(await new HttpClient { DefaultRequestHeaders = { { "User-Agent", "AutoOS" } } }.GetStringAsync("https://api.github.com/repos/WinMerge/winmerge/releases/latest")).RootElement.GetProperty("assets").EnumerateArray().First(a => a.GetProperty("name").GetString().Contains("x64-Setup.exe")).GetProperty("browser_download_url").GetString(), Path.GetTempPath(), "WinMerge-x64-Setup.exe", reporter: reporter), () => WinMerge == true),

			// install winmerge
			("Installing WinMerge", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Path.GetTempPath(), "WinMerge-x64-Setup.exe"), Arguments = "/SP- /VERYSILENT /SUPPRESSMSGBOXES /NORESTART" , WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), () => WinMerge == true),
			("Cleaning up WinMerge files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "WinMerge-x64-Setup.exe")), () => WinMerge == true),

			//// set winmerge color mode to follow system
			//(@"Setting WinMerge ""Color mode"" to ""Follow system""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Thingamahoochie\WinMerge\Settings", "ColorMode", 2, RegistryValueKind.DWord), () => WinMerge == true),

			// download git
			("Downloading Git", async () => await DownloadHelper.Download(JsonDocument.Parse(await new HttpClient { DefaultRequestHeaders = { { "User-Agent", "AutoOS" } } }.GetStringAsync("https://api.github.com/repos/git-for-windows/git/releases")).RootElement.EnumerateArray().First(release => release.GetProperty("assets").EnumerateArray().Any(asset => asset.GetProperty("name").GetString().Contains("64-bit.exe"))).GetProperty("assets").EnumerateArray().First(asset => asset.GetProperty("name").GetString().Contains("64-bit.exe")).GetProperty("browser_download_url").GetString(), Path.GetTempPath(), "Git64-bit.exe", reporter: reporter), () => Git == true),

			// install git
			("Installing Git", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Path.GetTempPath(), "Git64-bit.exe"), Arguments = "/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /NOICONS /COMPONENTS=GitLFS,GitGUI,GitCore" , WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), () => Git ==  true),
			("Cleaning up Git files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "Git64-bit.exe")), () => Git ==  true),

			// download cmake
			("Downloading CMake", async () => await DownloadHelper.Download(JsonDocument.Parse(await new HttpClient { DefaultRequestHeaders = { { "User-Agent", "AutoOS" } } }.GetStringAsync("https://api.github.com/repos/Kitware/CMake/releases")).RootElement.EnumerateArray().First(release => release.GetProperty("assets").EnumerateArray().Any(asset => asset.GetProperty("name").GetString().Contains("windows-x86_64.msi"))).GetProperty("assets").EnumerateArray().First(asset => asset.GetProperty("name").GetString().Contains("windows-x86_64.msi")).GetProperty("browser_download_url").GetString(), Path.GetTempPath(), "cmake-windows-x86_64.msi", reporter: reporter), () => CMake == true),

			// install cmake
			("Installing CMake", async () => await Process.Start(new ProcessStartInfo { FileName = "msiexec.exe", Arguments = $@"/i ""{Path.Combine(Path.GetTempPath(), "cmake-windows-x86_64.msi")}"" /qn" , WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), () => CMake ==  true),
			("Cleaning up CMake files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "cmake-windows-x86_64.msi")), () => CMake ==  true),

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

			// download rust
			("Downloading Rust", async () => await DownloadHelper.Download("https://static.rust-lang.org/rustup/dist/x86_64-pc-windows-msvc/rustup-init.exe", Path.GetTempPath(), "rustup-init.exe", reporter: reporter), () => Rust == true),

			// install rust
			("Installing Rust", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Path.GetTempPath(), "rustup-init.exe"), Arguments = "-y --default-toolchain stable", WindowStyle = ProcessWindowStyle.Hidden, UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true })!.WaitForExitAsync(), () => Rust == true),
			("Cleaning up Rust files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "rustup-init.exe")), () => Rust == true),

			// download java
			("Downloading Java", async () => await DownloadHelper.Download("https://javadl.oracle.com/webapps/download/AutoDL?BundleId=253458_ba687cb3cbb24342adc8fdf890b993dc", Path.GetTempPath(), "jre-windows-x64.exe", reporter: reporter), () => Java == true),

			// install java
			("Installing Java", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Path.GetTempPath(), "jre-windows-x64.exe"), Arguments = "/s REBOOT=0", WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), () => Java == true),
			("Cleaning up Java files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "jre-windows-x64.exe")), () => Java == true),

			// disable java update scheduler startup entry
			("Disabling Java Update Scheduler startup entry", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run32", "SunJavaUpdateSched", new byte[] { 0x03 }, RegistryValueKind.Binary), () => Java == true),

			// download go
			("Downloading Go", async () => await DownloadHelper.Download("https://go.dev/dl/go1.26.4.windows-amd64.msi", Path.GetTempPath(), "gowindows-amd64.msi", reporter: reporter), () => Go == true),

			// install go
			("Installing Go", async () => await Process.Start(new ProcessStartInfo { FileName = "msiexec.exe", Arguments = $@"/i ""{Path.Combine(Path.GetTempPath(), "gowindows-amd64.msi")}"" /qn" , WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), () => Go == true),
			("Cleaning up Go files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "gowindows-amd64.msi")), () => Go == true),

			// download trello
			("Downloading Trello", async () => await StoreHelper.Download("45273LiamForsyth.PawsforTrello_7pb5ddty8z1pa", reporter: reporter), () => Trello == true),

			// install trello
			("Installing Trello", async () => await StoreHelper.Install("45273LiamForsyth.PawsforTrello_7pb5ddty8z1pa"), () => Trello == true),

			// pin trello to the taskbar
			("Pinning Trello to the taskbar", async () => await ProcessActions.PinToTaskbar("UWA", "45273LiamForsyth.PawsforTrello_7pb5ddty8z1pa!trello"), () => Trello == true),

			// download autoruns
			("Downloading Autoruns", async () => await DownloadHelper.Download("https://raw.githubusercontent.com/tinodin/AutoOS-Resources/main/Files/Autoruns/Autoruns64.exe", Path.GetTempPath(), "Autoruns64.exe", reporter: reporter), () => Autoruns == true),
			// ("Downloading Autoruns", async () => await DownloadHelper.Download("https://download.sysinternals.com/files/Autoruns.zip", Path.GetTempPath(), "Autoruns.zip", reporter: reporter), () => Autoruns == true),

			// install autoruns
			// ("Installing Autoruns", async () => await ExtractHelper.Extract(Path.Combine(Path.GetTempPath(), "Autoruns.zip"), Path.Combine(Path.GetTempPath(), "Autoruns")), () => Autoruns == true),
			// ("Installing Autoruns", async () => Directory.Move(Path.Combine(Path.GetTempPath(), "Autoruns"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Autoruns")), () => Autoruns == true),
			("Installing Autoruns", async () => Directory.CreateDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Autoruns")), () => Autoruns == true),
			("Installing Autoruns", async () => File.Move(Path.Combine(Path.GetTempPath(), "Autoruns64.exe"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Autoruns", "Autoruns64.exe"), true), () => Autoruns == true),
			("Installing Autoruns", async () => ShortcutHelper.Create(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Microsoft", "Windows", "Start Menu", "Programs", "Autoruns.lnk"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Autoruns", "Autoruns64.exe")), () => Autoruns == true),
			("Installing Autoruns", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Autoruns", "DisplayName", "Autoruns", RegistryValueKind.String), () => Autoruns == true),
			("Installing Autoruns", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Autoruns", "UninstallString", $@"cmd /c rd /s /q ""{Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Autoruns")}"" & del ""{Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), @"Microsoft\Windows\Start Menu\Programs\Autoruns.lnk")}"" & reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Autoruns"" /f", RegistryValueKind.String), () => Autoruns == true),
			("Installing Autoruns", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Autoruns", "DisplayIcon", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Autoruns", "Autoruns64.exe"), RegistryValueKind.String), () => Autoruns == true),
			("Installing Autoruns", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Autoruns", "Publisher", "Sysinternals", RegistryValueKind.String), () => Autoruns == true),
			("Installing Autoruns", async () => await Process.Start(new ProcessStartInfo { FileName = "reg.exe", Arguments = $@"load HKU\DefaultUser ""{Path.Combine(Path.GetPathRoot(Environment.SystemDirectory)!, "Users", "Default", "NTUSER.DAT")}""", CreateNoWindow = true })!.WaitForExitAsync(), () => Autoruns == true),
			("Installing Autoruns", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Sysinternals\Autoruns", "EulaAccepted", 1, RegistryValueKind.DWord, true), () => Autoruns == true),
			("Installing Autoruns", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Sysinternals\Autoruns", "MainWindowPlacement", new byte[] { 0x2c, 0x00, 0x00, 0x00, 0x02, 0x00, 0x00, 0x00, 0x03, 0x00, 0x00, 0x00, 0xff, 0xff, 0xff, 0xff, 0xff }, RegistryValueKind.Binary, true), () => Autoruns == true),
			("Installing Autoruns", async () => await Process.Start(new ProcessStartInfo { FileName = "reg.exe", Arguments = @"unload HKU\DefaultUser", CreateNoWindow = true })!.WaitForExitAsync(), () => Autoruns == true),
			//("Cleaning up Autoruns files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "Autoruns.zip")), () => Autoruns == true),

			// download process explorer
			("Downloading Process Explorer", async () => await DownloadHelper.Download("https://raw.githubusercontent.com/tinodin/AutoOS-Resources/main/Files/Process%20Explorer/procexp64.exe", Path.GetTempPath(), "procexp64.exe", reporter: reporter), () => ProcessExplorer == true),
			//("Downloading Process Explorer", async () => await DownloadHelper.Download("https://download.sysinternals.com/files/ProcessExplorer.zip", Path.GetTempPath(), "ProcessExplorer.zip", new InstallPageReporter()), () => ProcessExplorer == true),

			// install process explorer
			//("Installing Process Explorer", async () => await ExtractHelper.Extract(Path.Combine(Path.GetTempPath(), "ProcessExplorer.zip"), Path.Combine(Path.GetTempPath(), "ProcessExplorer")), () => ProcessExplorer == true),
			//("Installing Process Explorer", async () => File.Copy(Path.Combine(Path.GetTempPath(), "ProcessExplorer", "procexp64.exe"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "procexp64.exe"), true), () => ProcessExplorer == true),
			("Installing Process Explorer", async () => Directory.CreateDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Process Explorer")), () => ProcessExplorer == true),
			("Installing Process Explorer", async () => File.Copy(Path.Combine(Path.GetTempPath(), "procexp64.exe"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Process Explorer", "procexp64.exe"), true), () => ProcessExplorer == true),
			("Installing Process Explorer", async () => ShortcutHelper.Create(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Microsoft", "Windows", "Start Menu", "Programs", "Process Explorer.lnk"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Process Explorer", "procexp64.exe")), () => ProcessExplorer == true),
			("Installing Process Explorer", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\ProcessExplorer", "DisplayName", "Process Explorer", RegistryValueKind.String), () => ProcessExplorer == true),
			("Installing Process Explorer", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\ProcessExplorer", "UninstallString", $@"cmd /c rd /s /q ""{Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Process Explorer")}"" & del ""{Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), @"Microsoft\Windows\Start Menu\Programs\Process Explorer.lnk")}"" & reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\ProcessExplorer"" /f", RegistryValueKind.String), () => ProcessExplorer == true),
			("Installing Process Explorer", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\ProcessExplorer", "DisplayIcon", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Process Explorer", "procexp64.exe"), RegistryValueKind.String), () => ProcessExplorer == true),
			("Installing Process Explorer", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\ProcessExplorer", "Publisher", "Sysinternals", RegistryValueKind.String), () => ProcessExplorer == true),
			("Installing Process Explorer", async () => await Process.Start(new ProcessStartInfo { FileName = "reg.exe", Arguments = $@"load HKU\DefaultUser ""{Path.Combine(Path.GetPathRoot(Environment.SystemDirectory)!, "Users", "Default", "NTUSER.DAT")}""", CreateNoWindow = true })!.WaitForExitAsync(), () => ProcessExplorer == true),
			("Installing Process Explorer", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Sysinternals\Process Explorer", "EulaAccepted", 1, RegistryValueKind.DWord, true), () => ProcessExplorer == true),
			("Installing Process Explorer", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Sysinternals\Process Explorer", "WindowPlacement", new byte[] { 0x2c, 0x00, 0x00, 0x00, 0x02, 0x00, 0x00, 0x00, 0x03, 0x00, 0x00, 0x00, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0x68, 0x00, 0x00, 0x00, 0x68, 0x00, 0x00, 0x00, 0x08, 0x06, 0x00, 0x00, 0x59, 0x03, 0x00, 0x00 }, RegistryValueKind.Binary, true), () => ProcessExplorer == true),
			("Installing Process Explorer", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Sysinternals\Process Explorer", "ConfirmKill", 0, RegistryValueKind.DWord, true), () => ProcessExplorer == true),
			("Installing Process Explorer", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Sysinternals\Process Explorer", "PrcessColumnCount", 13, RegistryValueKind.DWord, true), () => ProcessExplorer == true),
			("Installing Process Explorer", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Sysinternals\Process Explorer", "DllColumnCount", 4, RegistryValueKind.DWord, true), () => ProcessExplorer == true),
			("Installing Process Explorer", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Sysinternals\Process Explorer", "HandleColumnCount", 2, RegistryValueKind.DWord, true), () => ProcessExplorer == true),
			("Installing Process Explorer", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Sysinternals\Process Explorer", "TrayCPUHistory", 0, RegistryValueKind.DWord, true), () => ProcessExplorer == true),
			("Installing Process Explorer", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Sysinternals\Process Explorer", "ProcessImageColumnWidth", 280, RegistryValueKind.DWord, true), () => ProcessExplorer == true),
			("Installing Process Explorer", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Sysinternals\Process Explorer\DllColumnMap", "0", 26, RegistryValueKind.DWord, true), () => ProcessExplorer == true),
			("Installing Process Explorer", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Sysinternals\Process Explorer\DllColumnMap", "1", 42, RegistryValueKind.DWord, true), () => ProcessExplorer == true),
			("Installing Process Explorer", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Sysinternals\Process Explorer\DllColumnMap", "2", 1033, RegistryValueKind.DWord, true), () => ProcessExplorer == true),
			("Installing Process Explorer", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Sysinternals\Process Explorer\DllColumnMap", "3", 1111, RegistryValueKind.DWord, true), () => ProcessExplorer == true),
			("Installing Process Explorer", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Sysinternals\Process Explorer\DllColumns", "0", 110, RegistryValueKind.DWord, true), () => ProcessExplorer == true),
			("Installing Process Explorer", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Sysinternals\Process Explorer\DllColumns", "1", 180, RegistryValueKind.DWord, true), () => ProcessExplorer == true),
			("Installing Process Explorer", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Sysinternals\Process Explorer\DllColumns", "2", 140, RegistryValueKind.DWord, true), () => ProcessExplorer == true),
			("Installing Process Explorer", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Sysinternals\Process Explorer\DllColumns", "3", 300, RegistryValueKind.DWord, true), () => ProcessExplorer == true),
			("Installing Process Explorer", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Sysinternals\Process Explorer\HandleColumnMap", "0", 21, RegistryValueKind.DWord, true), () => ProcessExplorer == true),
			("Installing Process Explorer", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Sysinternals\Process Explorer\HandleColumnMap", "1", 22, RegistryValueKind.DWord, true), () => ProcessExplorer == true),
			("Installing Process Explorer", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Sysinternals\Process Explorer\HandleColumns", "0", 100, RegistryValueKind.DWord, true), () => ProcessExplorer == true),
			("Installing Process Explorer", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Sysinternals\Process Explorer\HandleColumns", "1", 450, RegistryValueKind.DWord, true), () => ProcessExplorer == true),
			("Installing Process Explorer", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Sysinternals\Process Explorer\ProcessColumnMap", "0", 3, RegistryValueKind.DWord, true), () => ProcessExplorer == true),
			("Installing Process Explorer", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Sysinternals\Process Explorer\ProcessColumnMap", "1", 1055, RegistryValueKind.DWord, true), () => ProcessExplorer == true),
			("Installing Process Explorer", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Sysinternals\Process Explorer\ProcessColumnMap", "2", 1060, RegistryValueKind.DWord, true), () => ProcessExplorer == true),
			("Installing Process Explorer", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Sysinternals\Process Explorer\ProcessColumnMap", "3", 1063, RegistryValueKind.DWord, true), () => ProcessExplorer == true),
			("Installing Process Explorer", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Sysinternals\Process Explorer\ProcessColumnMap", "4", 1650, RegistryValueKind.DWord, true), () => ProcessExplorer == true),
			("Installing Process Explorer", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Sysinternals\Process Explorer\ProcessColumnMap", "5", 4, RegistryValueKind.DWord, true), () => ProcessExplorer == true),
			("Installing Process Explorer", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Sysinternals\Process Explorer\ProcessColumnMap", "6", 38, RegistryValueKind.DWord, true), () => ProcessExplorer == true),
			("Installing Process Explorer", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Sysinternals\Process Explorer\ProcessColumnMap", "7", 1033, RegistryValueKind.DWord, true), () => ProcessExplorer == true),
			("Installing Process Explorer", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Sysinternals\Process Explorer\ProcessColumnMap", "8", 1200, RegistryValueKind.DWord, true), () => ProcessExplorer == true),
			("Installing Process Explorer", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Sysinternals\Process Explorer\ProcessColumnMap", "9", 1092, RegistryValueKind.DWord, true), () => ProcessExplorer == true),
			("Installing Process Explorer", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Sysinternals\Process Explorer\ProcessColumnMap", "10", 5, RegistryValueKind.DWord, true), () => ProcessExplorer == true),
			("Installing Process Explorer", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Sysinternals\Process Explorer\ProcessColumnMap", "11", 1065, RegistryValueKind.DWord, true), () => ProcessExplorer == true),
			("Installing Process Explorer", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Sysinternals\Process Explorer\ProcessColumnMap", "12", 18, RegistryValueKind.DWord, true), () => ProcessExplorer == true),
			("Installing Process Explorer", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Sysinternals\Process Explorer\ProcessColumnMap", "13", 18, RegistryValueKind.DWord, true), () => ProcessExplorer == true),
			("Installing Process Explorer", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Sysinternals\Process Explorer\ProcessColumnMap", "14", 1087, RegistryValueKind.DWord, true), () => ProcessExplorer == true),
			("Installing Process Explorer", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Sysinternals\Process Explorer\ProcessColumnMap", "15", 1195, RegistryValueKind.DWord, true), () => ProcessExplorer == true),
			("Installing Process Explorer", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Sysinternals\Process Explorer\ProcessColumns", "0", 200, RegistryValueKind.DWord, true), () => ProcessExplorer == true),
			("Installing Process Explorer", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Sysinternals\Process Explorer\ProcessColumns", "1", 60, RegistryValueKind.DWord, true), () => ProcessExplorer == true),
			("Installing Process Explorer", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Sysinternals\Process Explorer\ProcessColumns", "2", 84, RegistryValueKind.DWord, true), () => ProcessExplorer == true),
			("Installing Process Explorer", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Sysinternals\Process Explorer\ProcessColumns", "3", 80, RegistryValueKind.DWord, true), () => ProcessExplorer == true),
			("Installing Process Explorer", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Sysinternals\Process Explorer\ProcessColumns", "4", 39, RegistryValueKind.DWord, true), () => ProcessExplorer == true),
			("Installing Process Explorer", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Sysinternals\Process Explorer\ProcessColumns", "5", 44, RegistryValueKind.DWord, true), () => ProcessExplorer == true),
			("Installing Process Explorer", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Sysinternals\Process Explorer\ProcessColumns", "6", 257, RegistryValueKind.DWord, true), () => ProcessExplorer == true),
			("Installing Process Explorer", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Sysinternals\Process Explorer\ProcessColumns", "7", 181, RegistryValueKind.DWord, true), () => ProcessExplorer == true),
			("Installing Process Explorer", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Sysinternals\Process Explorer\ProcessColumns", "8", 100, RegistryValueKind.DWord, true), () => ProcessExplorer == true),
			("Installing Process Explorer", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Sysinternals\Process Explorer\ProcessColumns", "9", 100, RegistryValueKind.DWord, true), () => ProcessExplorer == true),
			("Installing Process Explorer", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Sysinternals\Process Explorer\ProcessColumns", "10", 100, RegistryValueKind.DWord, true), () => ProcessExplorer == true),
			("Installing Process Explorer", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Sysinternals\Process Explorer\ProcessColumns", "11", 100, RegistryValueKind.DWord, true), () => ProcessExplorer == true),
			("Installing Process Explorer", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Sysinternals\Process Explorer\ProcessColumns", "12", 100, RegistryValueKind.DWord, true), () => ProcessExplorer == true),
			("Installing Process Explorer", async () => await Process.Start(new ProcessStartInfo { FileName = "reg.exe", Arguments = @"unload HKU\DefaultUser", CreateNoWindow = true })!.WaitForExitAsync(), () => ProcessExplorer == true),
			("Cleaning up Process Explorer files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "procexp64.exe")), () => ProcessExplorer == true),

			// download process monitor
			("Downloading Process Monitor", async () => await DownloadHelper.Download("https://download.sysinternals.com/files/ProcessMonitor.zip", Path.GetTempPath(), "ProcessMonitor.zip", reporter: reporter), () => ProcessMonitor == true),

			// install process monitor
			("Installing Process Monitor", async () => await ExtractHelper.Extract(Path.Combine(Path.GetTempPath(), "ProcessMonitor.zip"), Path.Combine(Path.GetTempPath(), "ProcessMonitor")), () => ProcessMonitor == true),
			("Installing Process Monitor", async () => Directory.Move(Path.Combine(Path.GetTempPath(), "ProcessMonitor"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Process Monitor")), () => ProcessMonitor == true),
			("Installing Process Monitor", async () => ShortcutHelper.Create(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Microsoft", "Windows", "Start Menu", "Programs", "Process Monitor.lnk"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Process Monitor", "Procmon64.exe")), () => ProcessMonitor == true),
			("Installing Process Monitor", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\ProcessMonitor", "DisplayName", "Process Monitor", RegistryValueKind.String), () => ProcessMonitor == true),
			("Installing Process Monitor", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\ProcessMonitor", "UninstallString", $@"cmd /c rd /s /q ""{Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Process Monitor")}"" & del ""{Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), @"Microsoft\Windows\Start Menu\Programs\Process Monitor.lnk")}"" & reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\ProcessMonitor"" /f", RegistryValueKind.String), () => ProcessMonitor == true),
			("Installing Process Monitor", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\ProcessMonitor", "DisplayIcon", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Process Monitor", "Procmon64.exe"), RegistryValueKind.String), () => ProcessMonitor == true),
			("Installing Process Monitor", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\ProcessMonitor", "Publisher", "Sysinternals", RegistryValueKind.String), () => ProcessMonitor == true),
			("Installing Process Monitor", async () => await Process.Start(new ProcessStartInfo { FileName = "reg.exe", Arguments = $@"load HKU\DefaultUser ""{Path.Combine(Path.GetPathRoot(Environment.SystemDirectory)!, "Users", "Default", "NTUSER.DAT")}""", CreateNoWindow = true })!.WaitForExitAsync(), () => ProcessMonitor == true),
			("Installing Process Monitor", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Sysinternals\Process Monitor", "EulaAccepted", 1, RegistryValueKind.DWord, true), () => ProcessMonitor == true),
			("Installing Process Monitor", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Sysinternals\Process Monitor", "MainWindow", new byte[] { 0x2c, 0x00, 0x00, 0x00, 0x02, 0x00, 0x00, 0x00, 0x03, 0x00, 0x00, 0x00, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0x68, 0x00, 0x00, 0x00, 0x68, 0x00, 0x00, 0x00, 0x08, 0x06, 0x00, 0x00, 0x59, 0x03, 0x00, 0x00 }, RegistryValueKind.Binary, true), () => ProcessMonitor == true),
			("Installing Process Monitor", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Sysinternals\Process Monitor", "FilterRules", new byte[] { 0x01, 0x18, 0x00, 0x00, 0x00, 0x77, 0x9c, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x18, 0x00, 0x00, 0x00, 0x52, 0x00, 0x65, 0x00, 0x67, 0x00, 0x53, 0x00, 0x65, 0x00, 0x74, 0x00, 0x56, 0x00, 0x61, 0x00, 0x6c, 0x00, 0x75, 0x00, 0x65, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x75, 0x9c, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x18, 0x00, 0x00, 0x00, 0x50, 0x00, 0x72, 0x00, 0x6f, 0x00, 0x63, 0x00, 0x6d, 0x00, 0x6f, 0x00, 0x6e, 0x00, 0x2e, 0x00, 0x65, 0x00, 0x78, 0x00, 0x65, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x75, 0x9c, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x18, 0x00, 0x00, 0x00, 0x50, 0x00, 0x72, 0x00, 0x6f, 0x00, 0x63, 0x00, 0x65, 0x00, 0x78, 0x00, 0x70, 0x00, 0x2e, 0x00, 0x65, 0x00, 0x78, 0x00, 0x65, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x75, 0x9c, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x1a, 0x00, 0x00, 0x00, 0x41, 0x00, 0x75, 0x00, 0x74, 0x00, 0x6f, 0x00, 0x72, 0x00, 0x75, 0x00, 0x6e, 0x00, 0x73, 0x00, 0x2e, 0x00, 0x65, 0x00, 0x78, 0x00, 0x65, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x75, 0x9c, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x1c, 0x00, 0x00, 0x00, 0x50, 0x00, 0x72, 0x00, 0x6f, 0x00, 0x63, 0x00, 0x6d, 0x00, 0x6f, 0x00, 0x6e, 0x00, 0x36, 0x00, 0x34, 0x00, 0x2e, 0x00, 0x65, 0x00, 0x78, 0x00, 0x65, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x75, 0x9c, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x1c, 0x00, 0x00, 0x00, 0x50, 0x00, 0x72, 0x00, 0x6f, 0x00, 0x63, 0x00, 0x65, 0x00, 0x78, 0x00, 0x70, 0x00, 0x36, 0x00, 0x34, 0x00, 0x2e, 0x00, 0x65, 0x00, 0x78, 0x00, 0x65, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x75, 0x9c, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0e, 0x00, 0x00, 0x00, 0x53, 0x00, 0x79, 0x00, 0x73, 0x00, 0x74, 0x00, 0x65, 0x00, 0x6d, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x77, 0x9c, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00, 0x00, 0x10, 0x00, 0x00, 0x00, 0x49, 0x00, 0x52, 0x00, 0x50, 0x00, 0x5f, 0x00, 0x4d, 0x00, 0x4a, 0x00, 0x5f, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x77, 0x9c, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00, 0x00, 0x10, 0x00, 0x00, 0x00, 0x46, 0x00, 0x41, 0x00, 0x53, 0x00, 0x54, 0x00, 0x49, 0x00, 0x4f, 0x00, 0x5f, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x78, 0x9c, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00, 0x00, 0x10, 0x00, 0x00, 0x00, 0x46, 0x00, 0x41, 0x00, 0x53, 0x00, 0x54, 0x00, 0x20, 0x00, 0x49, 0x00, 0x4f, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x87, 0x9c, 0x00, 0x00, 0x05, 0x00, 0x00, 0x00, 0x00, 0x1a, 0x00, 0x00, 0x00, 0x70, 0x00, 0x61, 0x00, 0x67, 0x00, 0x65, 0x00, 0x66, 0x00, 0x69, 0x00, 0x6c, 0x00, 0x65, 0x00, 0x2e, 0x00, 0x73, 0x00, 0x79, 0x00, 0x73, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x87, 0x9c, 0x00, 0x00, 0x05, 0x00, 0x00, 0x00, 0x00, 0x0a, 0x00, 0x00, 0x00, 0x24, 0x00, 0x4d, 0x00, 0x66, 0x00, 0x74, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x87, 0x9c, 0x00, 0x00, 0x05, 0x00, 0x00, 0x00, 0x00, 0x12, 0x00, 0x00, 0x00, 0x24, 0x00, 0x4d, 0x00, 0x66, 0x00, 0x74, 0x00, 0x4d, 0x00, 0x69, 0x00, 0x72, 0x00, 0x72, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x87, 0x9c, 0x00, 0x00, 0x05, 0x00, 0x00, 0x00, 0x00, 0x12, 0x00, 0x00, 0x00, 0x24, 0x00, 0x4c, 0x00, 0x6f, 0x00, 0x67, 0x00, 0x46, 0x00, 0x69, 0x00, 0x6c, 0x00, 0x65, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x87, 0x9c, 0x00, 0x00, 0x05, 0x00, 0x00, 0x00, 0x00, 0x10, 0x00, 0x00, 0x00, 0x24, 0x00, 0x56, 0x00, 0x6f, 0x00, 0x6c, 0x00, 0x75, 0x00, 0x6d, 0x00, 0x65, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x87, 0x9c, 0x00, 0x00, 0x05, 0x00, 0x00, 0x00, 0x00, 0x12, 0x00, 0x00, 0x00, 0x24, 0x00, 0x41, 0x00, 0x74, 0x00, 0x74, 0x00, 0x72, 0x00, 0x44, 0x00, 0x65, 0x00, 0x66, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x87, 0x9c, 0x00, 0x00, 0x05, 0x00, 0x00, 0x00, 0x00, 0x0c, 0x00, 0x00, 0x00, 0x24, 0x00, 0x52, 0x00, 0x6f, 0x00, 0x6f, 0x00, 0x74, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x87, 0x9c, 0x00, 0x00, 0x05, 0x00, 0x00, 0x00, 0x00, 0x10, 0x00, 0x00, 0x00, 0x24, 0x00, 0x42, 0x00, 0x69, 0x00, 0x74, 0x00, 0x6d, 0x00, 0x61, 0x00, 0x70, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x87, 0x9c, 0x00, 0x00, 0x05, 0x00, 0x00, 0x00, 0x00, 0x0c, 0x00, 0x00, 0x00, 0x24, 0x00, 0x42, 0x00, 0x6f, 0x00, 0x6f, 0x00, 0x74, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x87, 0x9c, 0x00, 0x00, 0x05, 0x00, 0x00, 0x00, 0x00, 0x12, 0x00, 0x00, 0x00, 0x24, 0x00, 0x42, 0x00, 0x61, 0x00, 0x64, 0x00, 0x43, 0x00, 0x6c, 0x00, 0x75, 0x00, 0x73, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x87, 0x9c, 0x00, 0x00, 0x05, 0x00, 0x00, 0x00, 0x00, 0x10, 0x00, 0x00, 0x00, 0x24, 0x00, 0x53, 0x00, 0x65, 0x00, 0x63, 0x00, 0x75, 0x00, 0x72, 0x00, 0x65, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x87, 0x9c, 0x00, 0x00, 0x05, 0x00, 0x00, 0x00, 0x00, 0x10, 0x00, 0x00, 0x00, 0x24, 0x00, 0x55, 0x00, 0x70, 0x00, 0x43, 0x00, 0x61, 0x00, 0x73, 0x00, 0x65, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x87, 0x9c, 0x00, 0x00, 0x06, 0x00, 0x00, 0x00, 0x00, 0x10, 0x00, 0x00, 0x00, 0x24, 0x00, 0x45, 0x00, 0x78, 0x00, 0x74, 0x00, 0x65, 0x00, 0x6e, 0x00, 0x64, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x92, 0x9c, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x14, 0x00, 0x00, 0x00, 0x50, 0x00, 0x72, 0x00, 0x6f, 0x00, 0x66, 0x00, 0x69, 0x00, 0x6c, 0x00, 0x69, 0x00, 0x6e, 0x00, 0x67, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, RegistryValueKind.Binary, true), () => ProcessMonitor == true),
			("Installing Process Monitor", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Sysinternals\Process Monitor", "HighlightRules", new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00 }, RegistryValueKind.Binary, true), () => ProcessMonitor == true),
			("Installing Process Monitor", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Sysinternals\Process Monitor", "ColumnCount", 7, RegistryValueKind.DWord, true), () => ProcessMonitor == true),
			("Installing Process Monitor", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Sysinternals\Process Monitor", "Columns", new byte[] { 0x63, 0x00, 0x8e, 0x00, 0x28, 0x00, 0x64, 0x00, 0x6a, 0x04, 0x64, 0x00, 0xff, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, RegistryValueKind.Binary, true), () => ProcessMonitor == true),
			("Installing Process Monitor", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Sysinternals\Process Monitor", "ColumnMap", new byte[] { 0x8e, 0x9c, 0x00, 0x00, 0x75, 0x9c, 0x00, 0x00, 0x76, 0x9c, 0x00, 0x00, 0x77, 0x9c, 0x00, 0x00, 0x87, 0x9c, 0x00, 0x00, 0x78, 0x9c, 0x00, 0x00, 0x79, 0x9c, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, RegistryValueKind.Binary, true), () => ProcessMonitor == true),
			("Installing Process Monitor", async () => await Process.Start(new ProcessStartInfo { FileName = "reg.exe", Arguments = @"unload HKU\DefaultUser", CreateNoWindow = true })!.WaitForExitAsync(), () => ProcessMonitor == true),
			("Cleaning up Process Monitor files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "ProcessMonitor.zip")), () => ProcessMonitor == true),
			
			// download hwinfo
			("Downloading HWiNFO® 64", async () => await DownloadHelper.Download("https://www.sac.sk/download/utildiag/hwi_850x.exe", Path.GetTempPath(), "hwi64.exe", reporter: reporter), () => HWInfo == true),

			// install hwinfo
			("Installing HWiNFO® 64", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Path.GetTempPath(), "hwi64.exe"), Arguments = "/SP- /VERYSILENT /SUPPRESSMSGBOXES /NORESTART" , WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), () => HWInfo == true),
			("Installing HWiNFO® 64", async () => { while (Process.GetProcessesByName("HWiNFO64").Length == 0) await Task.Delay(100); foreach (Process process in Process.GetProcessesByName("HWiNFO64")) { process.Kill(); process.WaitForExit(); } }, () => HWInfo == true),
			("Cleaning up HWiNFO® 64", async () => File.Delete(Path.Combine(Path.GetTempPath(), "hwi64.exe")), () => HWInfo == true),

			// download timing configurator
			("Downloading ASRock Timing Configurator", async () => await DownloadHelper.Download("https://download.asrock.com/Utility/Formula/TimingConfigurator(v4.1.7).zip", Path.GetTempPath(), "TimingConfigurator.zip"), () => TimingConfigurator == true),

			// install timing configurator
			("Installing ASRock Timing Configurator", async () => await ExtractHelper.Extract(Path.Combine(Path.GetTempPath(), "TimingConfigurator.zip"), Path.Combine(Path.GetTempPath(), "TimingConfigurator")), () => TimingConfigurator == true),
			("Installing ASRock Timing Configurator", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Path.GetTempPath(), "TimingConfigurator", "TimingConfigurator(v4.1.7)", "AsrTCSetup(v4.1.7).exe"), Arguments = "/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /NOCLOSEAPPLICATIONS" , WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), () => TimingConfigurator == true),
			("Cleaning up ASRock Timing Configurator files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "TimingConfigurator.zip")), () => TimingConfigurator == true),
			("Cleaning up ASRock Timing Configurator files", async () => Directory.Delete(Path.Combine(Path.GetTempPath(), "TimingConfigurator"), true), () => TimingConfigurator == true),

			// remove asrock timing configurator desktop shortcut
			("Removing ASRock Timing Configurator desktop shortcut", async () => File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory), "ASRock Timing Configurator.lnk")), () => TimingConfigurator == true),

			// download zentimings
			("Downloading ZenTimings", async () => await DownloadHelper.Download(JsonDocument.Parse(await new HttpClient { DefaultRequestHeaders = { { "User-Agent", "AutoOS" } } }.GetStringAsync("https://api.github.com/repos/irusanov/ZenTimings/releases")).RootElement.EnumerateArray().First(release => !release.GetProperty("prerelease").GetBoolean() && release.GetProperty("assets").EnumerateArray().Any(asset => asset.GetProperty("name").GetString().EndsWith(".zip"))).GetProperty("assets").EnumerateArray().First(asset => asset.GetProperty("name").GetString().EndsWith(".zip")).GetProperty("browser_download_url").GetString(), Path.GetTempPath(), "ZenTimings.zip"), () => ZenTimings == true),

			// install zentimings
			("Installing ZenTimings", async () => await ExtractHelper.Extract(Path.Combine(Path.GetTempPath(), "ZenTimings.zip"), Path.Combine(Path.GetTempPath(), "ZenTimings")), () => ZenTimings == true),
			("Installing ZenTimings", async () => Directory.Move(Path.Combine(Path.GetTempPath(), "ZenTimings", "ZenTimings_v1.39"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "ZenTimings")), () => ZenTimings == true),
			("Installing ZenTimings", async () => ShortcutHelper.Create(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Microsoft", "Windows", "Start Menu", "Programs", "ZenTimings.lnk"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "ZenTimings", "ZenTimings.exe")), () => ZenTimings == true),
			("Installing ZenTimings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\ZenTimings", "DisplayName", "ZenTimings", RegistryValueKind.String), () => ZenTimings == true),
			("Installing ZenTimings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\ZenTimings", "UninstallString", $@"cmd /c rd /s /q ""{Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "ZenTimings")}"" & del ""{Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), @"Microsoft\Windows\Start Menu\Programs\ZenTimings.lnk")}"" & reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\ZenTimings"" /f", RegistryValueKind.String), () => ZenTimings == true),
			("Installing ZenTimings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\ZenTimings", "DisplayIcon", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), @"ZenTimings\ZenTimings.exe"), RegistryValueKind.String), () => ZenTimings == true),
			("Installing ZenTimings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\ZenTimings", "Publisher", "Irusanov", RegistryValueKind.String), () => ZenTimings == true),
			("Cleaning up ZenTimings files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "ZenTimings.zip")), () => ZenTimings == true),
			("Cleaning up ZenTimings files", async () => Directory.Delete(Path.Combine(Path.GetTempPath(), "ZenTimings")), () => ZenTimings == true),

			//download ram test pro
			("Downloading RAM Test Pro", async () => await DownloadHelper.Download("https://media.pcstonks.com/ram-test-pro/1.5.0/ram_test_pro_1.5.0.zip", Path.GetTempPath(), "ram_test_pro.zip", reporter: reporter), () => RamTestPro == true),

			// install ram test pro
			("Installing RAM Test Pro", async () => await ExtractHelper.Extract(Path.Combine(Path.GetTempPath(), "ram_test_pro.zip"), Path.GetTempPath()), () => RamTestPro == true),
			("Installing RAM Test Pro", async () => Directory.Move(Path.Combine(Path.GetTempPath(), "RAMTestPro1.5.0"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "RAM Test Pro")), () => RamTestPro == true),
			("Installing RAM Test Pro", async () => ShortcutHelper.Create(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Microsoft", "Windows", "Start Menu", "Programs", "RAM Test Pro.lnk"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "RAM Test Pro", "RAM Test Pro.exe")), () => RamTestPro == true),
			("Installing RAM Test Pro", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\RAM Test Pro", "DisplayName", "RAM Test Pro", RegistryValueKind.String), () => RamTestPro == true),
			("Installing RAM Test Pro", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\RAM Test Pro", "UninstallString", $@"cmd /c rd /s /q ""{Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "RAM Test Pro")}"" & del ""{Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), @"Microsoft\Windows\Start Menu\Programs\RAM Test Pro.lnk")}"" & reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\RAM Test Pro"" /f", RegistryValueKind.String), () => RamTestPro == true),
			("Installing RAM Test Pro", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\RAM Test Pro", "DisplayIcon", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "RAM Test Pro", "RAM Test Pro.exe"), RegistryValueKind.String), () => RamTestPro == true),
			("Installing RAM Test Pro", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\RAM Test Pro", "Publisher", "PCStonks", RegistryValueKind.String), () => RamTestPro == true),

			// cleanup ram test pro
			("Cleaning up RAM Test Pro files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "ram_test_pro.zip")), () => RamTestPro == true),

			// download testmem5
			("Downloading TestMem5", async () => await DownloadHelper.Download(JsonDocument.Parse(await new HttpClient { DefaultRequestHeaders = { { "User-Agent", "AutoOS" } } }.GetStringAsync("https://api.github.com/repos/CoolCmd/TestMem5/releases")).RootElement.EnumerateArray().First(release => !release.GetProperty("prerelease").GetBoolean() && release.GetProperty("assets").EnumerateArray().Any(asset => asset.GetProperty("name").GetString().EndsWith(".7z"))).GetProperty("assets").EnumerateArray().First(asset => asset.GetProperty("name").GetString().EndsWith(".7z")).GetProperty("browser_download_url").GetString(), Path.GetTempPath(), "TestMem5.7z"), () => TestMem5 == true),

			// install testmem5
			("Installing TestMem5", async () => await ExtractHelper.Extract(Path.Combine(Path.GetTempPath(), "TestMem5.7z"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "TestMem5")), () => TestMem5 == true),
			("Installing TestMem5", async () => ShortcutHelper.Create(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Microsoft", "Windows", "Start Menu", "Programs", "TestMem5.lnk"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "TestMem5", "TM5.exe")), () => TestMem5 == true),
			("Installing TestMem5", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\TestMem5", "DisplayName", "TestMem5", RegistryValueKind.String), () => TestMem5 == true),
			("Installing TestMem5", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\TestMem5", "UninstallString", $@"cmd /c rd /s /q ""{Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "TestMem5")}"" & del ""{Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), @"Microsoft\Windows\Start Menu\Programs\TestMem5.lnk")}"" & reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\TestMem5"" /f", RegistryValueKind.String), () => TestMem5 == true),
			("Installing TestMem5", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\TestMem5", "DisplayIcon", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), @"TestMem5\TM5.exe"), RegistryValueKind.String), () => TestMem5 == true),
			("Installing TestMem5", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\TestMem5", "Publisher", "CoolCmd", RegistryValueKind.String), () => TestMem5 == true),
			("Cleaning up TestMem5 files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "TestMem5.7z")), () => TestMem5 == true),

			// download prime95
			("Downloading Prime95", async () => await DownloadHelper.Download("https://download.mersenne.ca/gimps/v30/30.19/p95v3019b20.win64.zip", Path.GetTempPath(), "Prime95.zip"), () => Prime95 == true),

			// install prime95
			("Installing Prime95", async () => await ExtractHelper.Extract(Path.Combine(Path.GetTempPath(), "Prime95.zip"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Prime95")), () => Prime95 == true),
			("Installing Prime95", async () => ShortcutHelper.Create(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Microsoft", "Windows", "Start Menu", "Programs", "Prime95.lnk"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Prime95", "prime95.exe")), () => Prime95 == true),
			("Installing Prime95", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Prime95", "DisplayName", "Prime95", RegistryValueKind.String), () => Prime95 == true),
			("Installing Prime95", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Prime95", "UninstallString", $@"cmd /c rd /s /q ""{Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Prime95")}"" & del ""{Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), @"Microsoft\Windows\Start Menu\Programs\Prime95.lnk")}"" & reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Prime95"" /f", RegistryValueKind.String), () => Prime95 == true),
			("Installing Prime95", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Prime95", "DisplayIcon", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), @"Prime95\prime95.exe"), RegistryValueKind.String), () => Prime95 == true),
			("Installing Prime95", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Prime95", "Publisher", "Mersenne Research, Inc.", RegistryValueKind.String), () => Prime95 == true),
			("Cleaning up Prime95 files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "Prime95.zip")), () => Prime95 == true),

			// download y-cruncher
			("Downloading y-cruncher", async () => await DownloadHelper.Download(JsonDocument.Parse(await new HttpClient { DefaultRequestHeaders = { { "User-Agent", "AutoOS" } } }.GetStringAsync("https://api.github.com/repos/Mysticial/y-cruncher/releases/latest")).RootElement.GetProperty("assets").EnumerateArray().First(a => a.GetProperty("name").GetString().Contains(".zip")).GetProperty("browser_download_url").GetString(), Path.GetTempPath(), "y-cruncher.zip", reporter: reporter), () => yCruncher == true),

			// install y-cruncher
			("Installing y-cruncher", async () => await ExtractHelper.Extract(Path.Combine(Path.GetTempPath(), "y-cruncher.zip"), Path.GetTempPath()), () => yCruncher == true),
			("Installing y-cruncher", async () => Directory.Move(Path.Combine(Path.GetTempPath(), "y-cruncher v0.8.7.9547b"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "y-cruncher")), () => yCruncher == true),
			("Installing y-cruncher", async () => ShortcutHelper.Create(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Microsoft", "Windows", "Start Menu", "Programs", "y-cruncher.lnk"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "y-cruncher", "y-cruncher.exe")), () => yCruncher == true),
			("Installing y-cruncher", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\y-cruncher", "DisplayName", "y-cruncher", RegistryValueKind.String), () => yCruncher == true),
			("Installing y-cruncher", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\y-cruncher", "UninstallString", $@"cmd /c rd /s /q ""{Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "y-cruncher")}"" & del ""{Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), @"Microsoft\Windows\Start Menu\Programs\y-cruncher.lnk")}"" & reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\y-cruncher"" /f", RegistryValueKind.String), () => yCruncher == true),
			("Installing y-cruncher", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\y-cruncher", "DisplayIcon", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "y-cruncher", "y-cruncher.exe"), RegistryValueKind.String), () => yCruncher == true),
			("Installing y-cruncher", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\y-cruncher", "Publisher", "Mysticial", RegistryValueKind.String), () => yCruncher == true),
			("Cleaning up y-cruncher files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "y-cruncher.zip")), () => yCruncher == true),

			// download occt
			("Downloading OCCT", async () => await DownloadHelper.Download("https://www.ocbase.com/download/edition:Personal/os:Windows", Path.GetTempPath(), "OCCT.exe"), () => OCCT == true),

			// install occt
			("Installing OCCT", async () => Directory.CreateDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "OCCT")), () => OCCT == true),
			("Installing OCCT", async () => File.Move(Path.Combine(Path.GetTempPath(), "OCCT.exe"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "OCCT", "OCCT.exe"), true), () => OCCT == true),
			("Installing OCCT", async () => ShortcutHelper.Create(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Microsoft", "Windows", "Start Menu", "Programs", "OCCT.lnk"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "OCCT", "OCCT.exe")), () => OCCT == true),
			("Installing OCCT", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\OCCT", "DisplayName", "OCCT", RegistryValueKind.String), () => OCCT == true),
			("Installing OCCT", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\OCCT", "UninstallString", $@"cmd /c rd /s /q ""{Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "OCCT")}"" & del ""{Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), @"Microsoft\Windows\Start Menu\Programs\OCCT.lnk")}"" & reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\OCCT"" /f", RegistryValueKind.String), () => OCCT == true),
			("Installing OCCT", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\OCCT", "DisplayIcon", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), @"OCCT\OCCT.exe"), RegistryValueKind.String), () => OCCT == true),
			("Installing OCCT", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\OCCT", "Publisher", "OCBASE", RegistryValueKind.String), () => OCCT == true),

			// download aida64 extreme
			("Downloading AIDA64 Extreme", async () => await DownloadHelper.Download("https://download.aida64.com/aida64extreme830.exe", Path.GetTempPath(), "aida64extreme.exe", reporter: reporter), () => AIDA64Extreme == true),

			// install aida64 extreme
			("Installing AIDA64 Extreme", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Path.GetTempPath(), "aida64extreme.exe"), Arguments = "/SP- /VERYSILENT /SUPPRESSMSGBOXES /NORESTART" , WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), () => AIDA64Extreme == true),
			("Cleaning up AIDA64 Extreme files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "aida64extreme.exe")), () => AIDA64Extreme == true),

			// remove aida64 extreme desktop shortcut
			("Removing AIDA64 Extreme desktop shortcut", async () => File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "AIDA64 Extreme.lnk")), () => AIDA64Extreme == true),

			// download memtest vulkan
			("Downloading Memtest Vulkan", async () => await DownloadHelper.Download(JsonDocument.Parse(await new HttpClient { DefaultRequestHeaders = { { "User-Agent", "AutoOS" } } }.GetStringAsync("https://api.github.com/repos/GpuZelenograd/memtest_vulkan/releases")).RootElement.EnumerateArray().First(release => !release.GetProperty("prerelease").GetBoolean() && release.GetProperty("assets").EnumerateArray().Any(asset => asset.GetProperty("name").GetString().EndsWith(".exe"))).GetProperty("assets").EnumerateArray().First(asset => asset.GetProperty("name").GetString().EndsWith(".exe")).GetProperty("browser_download_url").GetString(), Path.GetTempPath(), "memtest_vulkan.exe", reporter: reporter), () => MemtestVulkan == true),

			// install memtest vulkan
			("Installing Memtest Vulkan", async () => Directory.CreateDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Memtest Vulkan")), () => MemtestVulkan == true),
			("Installing Memtest Vulkan", async () => File.Move(Path.Combine(Path.GetTempPath(), "memtest_vulkan.exe"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Memtest Vulkan", "memtest_vulkan.exe"), true), () => MemtestVulkan == true),
			("Installing Memtest Vulkan", async () => ShortcutHelper.Create(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Microsoft", "Windows", "Start Menu", "Programs", "Memtest Vulkan.lnk"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Memtest Vulkan", "memtest_vulkan.exe")), () => MemtestVulkan == true),
			("Installing Memtest Vulkan", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Memtest Vulkan", "DisplayName", "Memtest Vulkan", RegistryValueKind.String), () => MemtestVulkan == true),
			("Installing Memtest Vulkan", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Memtest Vulkan", "UninstallString", $@"cmd /c rd /s /q ""{Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Memtest Vulkan")}"" & del ""{Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), @"Microsoft\Windows\Start Menu\Programs\Memtest Vulkan.lnk")}"" & reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Memtest Vulkan"" /f", RegistryValueKind.String), () => MemtestVulkan == true),
			("Installing Memtest Vulkan", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Memtest Vulkan", "DisplayIcon", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Memtest Vulkan", "memtest_vulkan.exe"), RegistryValueKind.String), () => MemtestVulkan == true),
			("Installing Memtest Vulkan", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Memtest Vulkan", "Publisher", "GpuZelenograd", RegistryValueKind.String), () => MemtestVulkan == true),
			("Cleaning up Memtest Vulkan files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "memtest_vulkan.exe")), () => MemtestVulkan == true),

			// download reaper
			("Downloading Reaper", async () => await DownloadHelper.Download("https://www.reaper.fm/files/7.x/reaper774_x64-install.exe", Path.GetTempPath(), "reaper_x64-install.exe", reporter: reporter), () => Reaper == true),

			// install reaper
			("Installing Reaper", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Path.GetTempPath(), "reaper_x64-install.exe"), Arguments = "/S" , WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), () => Reaper == true),
			("Cleaning up Reaper files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "reaper_x64-install.exe")), () => Reaper == true),

			// remove reaper desktop shortcut
			("Removing Reaper desktop shortcut", async () => File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory), "REAPER (x64).lnk")), () => Reaper == true),

			// pin reaper to the taskbar
			("Pinning Reaper to the taskbar", async () => await ProcessActions.PinToTaskbar("Link", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Microsoft", "Windows", "Start Menu", "Programs", "REAPER (x64)", "REAPER (x64).lnk")), () => Reaper == true),

			// download fl studio
			("Downloading FL Studio", async () => await DownloadHelper.Download("https://support.image-line.com/redirect/flstudio_win_installer", Path.GetTempPath(), "flstudio_win64.exe", reporter: reporter), () => FLStudio == true),

			// install fl studio
			("Installing FL Studio", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Path.GetTempPath(), "flstudio_win64.exe"), Arguments = "/S /ALLUSERS=1" , WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), () => FLStudio == true),
			("Cleaning up FL Studio files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "flstudio_win64.exe")), () => FLStudio == true),

			// remove fl studio desktop shortcut
			("Removing FL Studio desktop shortcut", async () => File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory), "FL Studio 2025.lnk")), () => FLStudio == true),

			// pin fl studio to the taskbar
			("Pinning FL Studio to the taskbar", async () => await ProcessActions.PinToTaskbar("Link", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft", "Windows", "Start Menu", "Programs", "Image-Line", "FL Studio 2025.lnk")), () => FLStudio == true),

			// download audacity
			("Downloading Audacity", async () => await DownloadHelper.Download(JsonDocument.Parse(await new HttpClient { DefaultRequestHeaders = { { "User-Agent", "AutoOS" } } }.GetStringAsync("https://api.github.com/repos/audacity/audacity/releases")).RootElement.EnumerateArray().First(release => release.GetProperty("assets").EnumerateArray().Any(asset => asset.GetProperty("name").GetString().EndsWith("x86_64.msi"))).GetProperty("assets").EnumerateArray().First(asset => asset.GetProperty("name").GetString().EndsWith("x86_64.msi")).GetProperty("browser_download_url").GetString(), Path.GetTempPath(), "audacity_setup.msi", reporter: reporter), () => Audacity == true),
									
			// install audacity
			("Installing Audacity", async () => await Process.Start(new ProcessStartInfo { FileName = "msiexec.exe", Arguments = $@"/i ""{Path.Combine(Path.GetTempPath(), "audacity_setup.msi")}"" /qn", WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), () => Audacity == true),
			("Cleaning up Audacity files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "audacity_setup.msi")), () => Audacity == true),

			// pin audacity to the taskbar
			("Pinning Audacity to the taskbar", async () => await ProcessActions.PinToTaskbar("Link", Directory.GetFiles(Directory.GetDirectories(Environment.GetFolderPath(Environment.SpecialFolder.Programs), "Audacity*").FirstOrDefault(), "Audacity*.lnk").FirstOrDefault()), () => Audacity == true),

			// remove audacity desktop shortcut
			("Removing Audacity desktop shortcut", async () => File.Delete(Directory.GetFiles(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Audacity*.lnk").FirstOrDefault()), () => Audacity == true),

			// download flexasio
			("Downloading FlexASIO", async () => await DownloadHelper.Download(JsonDocument.Parse(await new HttpClient { DefaultRequestHeaders = { { "User-Agent", "AutoOS" } } }.GetStringAsync("https://api.github.com/repos/dechamps/FlexASIO/releases")).RootElement.EnumerateArray().First(release => !release.GetProperty("prerelease").GetBoolean() && release.GetProperty("assets").EnumerateArray().Any(asset => asset.GetProperty("name").GetString().EndsWith(".exe"))).GetProperty("assets").EnumerateArray().First(asset => asset.GetProperty("name").GetString().EndsWith(".exe")).GetProperty("browser_download_url").GetString(), Path.GetTempPath(), "FlexASIO.exe", reporter: reporter), () => FlexASIO == true),

			// install flexasio
			("Installing FlexASIO", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Path.GetTempPath(), "FlexASIO.exe"), Arguments = "/VERYSILENT /NORESTART", WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), () => FlexASIO == true),
			("Cleaning up FlexASIO files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "FlexASIO.exe")), () => FlexASIO == true),

			// download flexasio gui
			("Downloading FlexASIO GUI", async () => await DownloadHelper.Download(JsonDocument.Parse(await new HttpClient { DefaultRequestHeaders = { { "User-Agent", "AutoOS" } } }.GetStringAsync("https://api.github.com/repos/flipswitchingmonkey/FlexASIO_GUI/releases")).RootElement.EnumerateArray().First(release => !release.GetProperty("prerelease").GetBoolean() && release.GetProperty("assets").EnumerateArray().Any(asset => asset.GetProperty("name").GetString().EndsWith(".exe"))).GetProperty("assets").EnumerateArray().First(asset => asset.GetProperty("name").GetString().EndsWith(".exe")).GetProperty("browser_download_url").GetString(), Path.GetTempPath(), "FlexASIO.GUIInstaller.exe", reporter: reporter), () => FlexASIO == true),

			// install flexasio gui
			("Installing FlexASIO GUI", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Path.GetTempPath(), "FlexASIO.GUIInstaller.exe"), Arguments = "/VERYSILENT /NORESTART", WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), () => FlexASIO == true),
			("Cleaning up FlexASIO GUI files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "FlexASIO.GUIInstaller.exe")), () => FlexASIO == true),

			// download asio4all
			("Downloading ASIO4ALL", async () => await DownloadHelper.Download("https://asio4all.org/downloads/ASIO4ALL_2_21.exe", Path.GetTempPath(), "ASIO4ALL.exe", reporter: reporter), () => ASIO4ALL == true),

			// install asio4all
			("Installing ASIO4ALL", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Path.GetTempPath(), "ASIO4ALL.exe"), Arguments = "/S", WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), () => ASIO4ALL == true),
			("Cleaning up ASIO4ALL files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "ASIO4ALL.exe")), () => ASIO4ALL == true),

			// remove asio4all desktop shortcut
			("Removing ASIO4ALL desktop shortcut", async () => File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "ASIO4ALL Web Site.lnk")), () => ASIO4ALL == true),

			// download arturia midi driver
			("Downloading Arturia MIDI Driver", async () => await DownloadHelper.Download("https://dl.arturia.net/products/midi-driver/soft/Arturia_USBMidi_v1.7.0_2025-08-20_setup__1_7_0_0.exe", Path.GetTempPath(), "Arturia_USBMidi.exe", reporter: reporter), () => ArturiaMidiControlCenter == true),

			// install arturia midi driver
			("Installing Arturia MIDI Driver", async () => await ExtractHelper.Extract(Path.Combine(Path.GetTempPath(), "Arturia_USBMidi.exe"), Path.Combine(Path.GetTempPath(), "Arturia_USBMidi")), () => ArturiaMidiControlCenter == true),
			("Installing Arturia MIDI Driver", async () => await Process.Start(new ProcessStartInfo { FileName = "msiexec.exe", Arguments = $@"/i ""{Path.Combine(Path.GetTempPath(), "Arturia_USBMidi", "x64", "Arturia_USBMidi_v1.7.0_2025-08-20.msi")}"" /qn", WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), () => ArturiaMidiControlCenter == true),
			("Cleaning up Arturia MIDI Driver files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "Arturia_USBMidi.exe")), () => ArturiaMidiControlCenter == true),
			("Cleaning up Arturia MIDI Driver files", async () => Directory.Delete(Path.Combine(Path.GetTempPath(), "Arturia_USBMidi"), true), () => ArturiaMidiControlCenter == true),

			// download midi control center
			("Downloading Arturia MIDI Control Center", async () => await DownloadHelper.Download("https://dl.arturia.net/products/mccu/soft/MIDI_Control_Center__1_23_0_134.exe", Path.GetTempPath(), "MIDI_Control_Center.exe", reporter: reporter), () => ArturiaMidiControlCenter == true),

			// install midi control center
			("Installing Arturia MIDI Control Center", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Path.GetTempPath(), "MIDI_Control_Center.exe"), Arguments = @"/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /COMPONENTS=""mcc""", WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), () => ArturiaMidiControlCenter == true),
			("Cleaning up Arturia MIDI Control Center files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "MIDI_Control_Center.exe")), () => ArturiaMidiControlCenter == true),

			// remove midi control center desktop shortcut
			("Removing Arturia MIDI Control Center desktop shortcut", async () => File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "MIDI Control Center.lnk")), () => ArturiaMidiControlCenter == true),

			// download voicemeeter
			("Downloading Voicemeeter", async () => await DownloadHelper.Download("https://download.vb-audio.com/Download_CABLE/VoicemeeterSetup_v1122.zip", Path.GetTempPath(), "VoicemeeterSetup.zip", reporter: reporter), () => Voicemeeter == true),

			// install voicemeeter
			("Installing Voicemeeter", async () => await ExtractHelper.Extract(Path.Combine(Path.GetTempPath(), "VoicemeeterSetup.zip"), Path.Combine(Path.GetTempPath(), "VoicemeeterSetup")), () => Voicemeeter == true),
			("Installing Voicemeeter", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Path.GetTempPath(), "VoicemeeterSetup", "voicemeetersetup.exe"), Arguments = "-i -h", WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), () => Voicemeeter == true),
			("Cleaning up Voicemeeter files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "VoicemeeterSetup.zip")), () => Voicemeeter == true),
			("Cleaning up Voicemeeter files", async () => Directory.Delete(Path.Combine(Path.GetTempPath(), "VoicemeeterSetup"), true), () => Voicemeeter == true),

			// download davinci resolve
			("Downloading DaVinci Resolve", async () => await DownloadHelper.Download(new[] { "https://github.com/tinodin/AutoOS-Resources/releases/download/v1.0.0.0/DaVinci_Resolve_21.0_Windows.part1.rar", "https://github.com/tinodin/AutoOS-Resources/releases/download/v1.0.0.0/DaVinci_Resolve_21.0_Windows.part2.rar" }, Path.GetTempPath(), new[] { "DaVinci_Resolve.part1.rar", "DaVinci_Resolve.part2.rar" }, reporter: reporter), () => DaVinciResolve == true),

			// install davinci resolve
			("Installing DaVinci Resolve", async () => await ExtractHelper.Extract(Path.Combine(Path.GetTempPath(), "DaVinci_Resolve.part1.rar"), Path.Combine(Path.GetTempPath(), "DaVinci_Resolve")), () => DaVinciResolve == true),
			("Installing DaVinci Resolve", async () => await ExtractHelper.Extract(Path.Combine(Path.GetTempPath(), "DaVinci_Resolve", "DaVinci_Resolve_21.0_Windows.exe"), Path.Combine(Path.GetTempPath(), "DaVinci_Resolve", "DaVinci_Resolve_21.0_Windows")), () => DaVinciResolve == true),
			("Installing DaVinci Resolve", async () => await Process.Start(new ProcessStartInfo { FileName = "msiexec.exe", Arguments = $@"/i ""{Path.Combine(Path.GetTempPath(), "DaVinci_Resolve", "DaVinci_Resolve_21.0_Windows", "ResolveInstaller.msi")}"" /qn", WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), () => DaVinciResolve == true),
			("Cleaning up DaVinci Resolve files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "DaVinci_Resolve.part1.rar")), () => DaVinciResolve == true),
			("Cleaning up DaVinci Resolve files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "DaVinci_Resolve.part2.rar")), () => DaVinciResolve == true),
			("Cleaning up DaVinci Resolve files", async () => Directory.Delete(Path.Combine(Path.GetTempPath(), "DaVinci_Resolve"), true), () => DaVinciResolve == true),

			// download blender
			("Downloading Blender", async () => await DownloadHelper.Download("https://download.blender.org/release/Blender5.1/blender-5.1.2-windows-x64.msi", Path.GetTempPath(), "blender-windows-x64.msi", reporter: reporter), () => Blender == true),

			// install blender
			("Installing Blender", async () => await Process.Start(new ProcessStartInfo { FileName = "msiexec.exe", Arguments = $@"/i ""{Path.Combine(Path.GetTempPath(), "blender-windows-x64.msi")}"" /qn", WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), () => Blender == true),
			("Cleaning up Blender files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "blender-windows-x64.msi")), () => Blender == true),

			// remove blender desktop shortcut
			("Removing Blender desktop shortcut", async () => File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Blender 5.1.lnk")), () => Blender == true),

			// download capcut
			("Downloading CapCut", async () => await DownloadHelper.Download("https://sf16-web-tos-buz.capcutcdn-us.com/obj/capcut-web-buz-tx/packages/CapCut_8_8_0_3774_capcutpc_0_creatortool.exe", Path.GetTempPath(), "CapCut.exe", reporter: reporter), () => CapCut == true),

			// install capcut
			("Installing CapCut", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Path.GetTempPath(), "CapCut.exe"), Arguments = "/silent_install=1", WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), () => CapCut == true),
			("Cleaning up CapCut files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "CapCut.exe")), () => CapCut == true),

			// remove capcut desktop shortcut
			("Removing CapCut desktop shortcut", async () => File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "CapCut.lnk")), () => CapCut == true),

			// download losslesscut
			("Downloading LosslessCut", async () => await DownloadHelper.Download(JsonDocument.Parse(await new HttpClient { DefaultRequestHeaders = { { "User-Agent", "AutoOS" } } }.GetStringAsync("https://api.github.com/repos/mifi/lossless-cut/releases")).RootElement.EnumerateArray().First(release => !release.GetProperty("prerelease").GetBoolean() && release.GetProperty("assets").EnumerateArray().Any(asset => asset.GetProperty("name").GetString().Contains("win-x64.7z"))).GetProperty("assets").EnumerateArray().First(asset => asset.GetProperty("name").GetString().Contains("win-x64.7z")).GetProperty("browser_download_url").GetString(), Path.GetTempPath(), "LosslessCut-win-x64.7z", reporter: reporter), () => LosslessCut == true),

			// install losslesscut
			("Installing LosslessCut", async () => await ExtractHelper.Extract(Path.Combine(Path.GetTempPath(), "LosslessCut-win-x64.7z"), Path.Combine(Path.GetTempPath(), "LosslessCut")), () => LosslessCut == true),
			("Installing LosslessCut", async () => Directory.Move(Path.Combine(Path.GetTempPath(), "LosslessCut"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "LosslessCut")), () => LosslessCut == true),
			("Installing LosslessCut", async () => ShortcutHelper.Create(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Microsoft", "Windows", "Start Menu", "Programs", "LosslessCut.lnk"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "LosslessCut", "LosslessCut.exe")), () => LosslessCut == true),
			("Installing LosslessCut", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\LosslessCut", "DisplayName", "LosslessCut", RegistryValueKind.String), () => LosslessCut == true),
			("Installing LosslessCut", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\LosslessCut", "UninstallString", $@"cmd /c rd /s /q ""{Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "LosslessCut")}"" & del ""{Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Microsoft", "Windows", "Start Menu", "Programs", "LosslessCut.lnk")}"" & reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\LosslessCut"" /f", RegistryValueKind.String), () => LosslessCut == true),
			("Installing LosslessCut", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\LosslessCut", "DisplayIcon", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), @"LosslessCut\LosslessCut.exe"), RegistryValueKind.String), () => LosslessCut == true),
			("Installing LosslessCut", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\LosslessCut", "Version", FileVersionInfo.GetVersionInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "LosslessCut", "LosslessCut.exe")).ProductVersion, RegistryValueKind.String), () => LosslessCut == true),
			("Installing LosslessCut", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\LosslessCut", "Publisher", "Mikael Finstad", RegistryValueKind.String), () => LosslessCut == true),
			("Installing LosslessCut", async () => await Task.Delay(500), () => LosslessCut == true),
			("Cleaning up LosslessCut files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "LosslessCut-win-x64.7z")), () => LosslessCut == true),

			// download netflix
			("Downloading Netflix", async () => await StoreHelper.Download("4DF9E0F8.Netflix_mcm4njqhnhss8", reporter: reporter), () => Netflix == true),

			// install netflix
			("Installing Netflix", async () => await StoreHelper.Install("4DF9E0F8.Netflix_mcm4njqhnhss8"), () => Netflix == true),

			// download disney+
			("Downloading Disney+", async () => await StoreHelper.Download("Disney.37853FC22B2CE_6rarf9sa4v8jt", reporter: reporter), () => DisneyPlus == true),

			// install disney+
			("Installing Disney+", async () => await StoreHelper.Install("Disney.37853FC22B2CE_6rarf9sa4v8jt"), () => DisneyPlus == true),

			// download prime video
			("Downloading Prime Video", async () => await StoreHelper.Download("AmazonVideo.PrimeVideo_pwbj9vvecjh7j", reporter: reporter), () => PrimeVideo == true),

			// install prime video
			("Installing Prime Video", async () => await StoreHelper.Install("AmazonVideo.PrimeVideo_pwbj9vvecjh7j"), () => PrimeVideo == true),

			// download mpc-qt
			("Downloading MPC-QT", async () => await DownloadHelper.Download(JsonDocument.Parse(await new HttpClient { DefaultRequestHeaders = { { "User-Agent", "AutoOS" } } }.GetStringAsync("https://api.github.com/repos/mpc-qt/mpc-qt/releases")).RootElement.EnumerateArray().First(release => !release.GetProperty("prerelease").GetBoolean() && release.GetProperty("assets").EnumerateArray().Any(asset => asset.GetProperty("name").GetString().StartsWith("mpc-qt-win-x64-") && asset.GetProperty("name").GetString().EndsWith("-installer.exe"))).GetProperty("assets").EnumerateArray().First(asset => asset.GetProperty("name").GetString().StartsWith("mpc-qt-win-x64-") && asset.GetProperty("name").GetString().EndsWith("-installer.exe")).GetProperty("browser_download_url").GetString(), Path.GetTempPath(), "mpc-qt-win-x64-installer.exe", reporter: reporter), () => MpcQt == true),

			// install mpc-qt
			("Installing MPC-QT", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Path.GetTempPath(), "mpc-qt-win-x64-installer.exe"), Arguments = "/S", WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), () => MpcQt == true),
			("Cleaning up MPC-QT files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "mpc-qt-win-x64-installer.exe")), () => MpcQt == true),

			// download mpv
			("Downloading mpv", async () => await DownloadHelper.Download(JsonDocument.Parse(await new HttpClient { DefaultRequestHeaders = { { "User-Agent", "AutoOS" } } }.GetStringAsync("https://api.github.com/repos/zhongfly/mpv-winbuild/releases")).RootElement.EnumerateArray().First(release => !release.GetProperty("prerelease").GetBoolean() && release.GetProperty("assets").EnumerateArray().Any(asset => asset.GetProperty("name").GetString().StartsWith("mpv-x86_64-v3-") && asset.GetProperty("name").GetString().EndsWith(".7z"))).GetProperty("assets").EnumerateArray().First(asset => asset.GetProperty("name").GetString().StartsWith("mpv-x86_64-v3-") && asset.GetProperty("name").GetString().EndsWith(".7z")).GetProperty("browser_download_url").GetString(), Path.GetTempPath(), "mpv-x86_64-v3.7z"), () => MPV == true),

			// install mpv
			("Installing mpv", async () => await ExtractHelper.Extract(Path.Combine(Path.GetTempPath(), "mpv-x86_64-v3.7z"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "mpv")), () => MPV == true),
			("Installing mpv", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "mpv", "mpv.exe"), Arguments = "--register", WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), () => MPV == true),
			("Installing mpv", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\mpv", "UninstallString", $@"cmd.exe /c """"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\mpv\mpv.exe"" --no-config --unregister && rmdir /s /q ""{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\mpv""""", RegistryValueKind.String), () => MPV == true),
			("Cleaning up mpv files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "mpv-x86_64-v3.7z")), () => MPV == true),

			// download vlc
			("Downloading VLC", async () => await DownloadHelper.Download("https://mirror.solnet.ch/videolan/vlc/3.0.23/win64/vlc-3.0.23-win64.exe", Path.GetTempPath(), "vlc-win64.exe", reporter: reporter), () => VLC == true),

			// install vlc
			("Installing VLC", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Path.GetTempPath(), "vlc-win64.exe"), Arguments = "/L=1033 /S", WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), () => VLC == true),
			("Cleaning up VLC files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "vlc-win64.exe")), () => VLC == true),

			// remove vlc desktop shortcut
			("Removing VLC desktop shortcut", async () => File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory), "VLC media player.lnk")), () => VLC == true),

			// download mediainfo
			("Downloading MediaInfo", async () => await StoreHelper.Download("MediaArea.net.MediaInfo_9bzbd7xajy7ar", reporter: reporter), () => MediaInfo == true),

			// install mediainfo
			("Installing MediaInfo", async () => await StoreHelper.Install("MediaArea.net.MediaInfo_9bzbd7xajy7ar"), () => MediaInfo == true),

			// download office
			("Downloading Office", async () => await DownloadHelper.Download("https://officecdn.microsoft.com/pr/wsus/setup.exe", Path.GetTempPath(), "setup.exe", reporter: reporter), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),

			// install office
			("Installing Office", async () => await DownloadHelper.Download("https://raw.githubusercontent.com/tinodin/AutoOS-Resources/main/Files/Office/configuration.xml", Path.GetTempPath(), "configuration.xml", reporter: reporter), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
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
			("Disabling Office startup entries", async () => TaskSchedulerHelper.Toggle(@"\Microsoft\Office\Office Startup Maintenance", false), () => Word || Excel || PowerPoint || OneNote || Teams || Outlook || OneDrive),
			("Disabling Office startup entries", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_CLASSES_ROOT\PROTOCOLS\Handler\AutorunsDisabled\mso-minsb.16\CLSID", "", "{42089D2D-912D-4018-9087-2B87803E93FB}", RegistryValueKind.String), () => OneDrive == true),
			("Disabling Office startup entries", async () => RegistryHelper.DeleteKey(RegistryHelper.Identity.TrustedInstaller, @"HKEY_CLASSES_ROOT\PROTOCOLS\Handler\mso-minsb.16"), () => OneDrive == true),
			("Disabling Office startup entries", async () => TaskSchedulerHelper.Toggle("OneDrive Per-Machine Standalone Update Task", false), () => OneDrive == true),
			("Disabling Office startup entries", async () => TaskSchedulerHelper.Toggle("OneDrive Reporting Task", false), () => OneDrive == true),
			("Disabling Office startup entries", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\FileSyncHelper", "Start", 4, RegistryValueKind.DWord), () => OneDrive == true),
			("Disabling Office startup entries", async () => ServicesHelper.StopService("FileSyncHelper"), () => OneDrive == true),
			("Disabling Office startup entries", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\OneDrive Updater Service", "Start", 4, RegistryValueKind.DWord), () => OneDrive == true),
			("Disabling Office startup entries", async () => ServicesHelper.StopService("OneDrive Updater Service"), () => OneDrive == true),

			// disable office telemetry
			("Disabling Office telemetry", async () => await Process.Start(new ProcessStartInfo { FileName = "reg.exe", Arguments = $@"load HKU\DefaultUser ""{Path.Combine(Path.GetPathRoot(Environment.SystemDirectory)!, "Users", "Default", "NTUSER.DAT")}""", CreateNoWindow = true })!.WaitForExitAsync(), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
			("Disabling Office telemetry", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Microsoft\Office\Common\ClientTelemetry", "DisableTelemetry", 1, RegistryValueKind.DWord, true), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
			("Disabling Office telemetry", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Microsoft\Office\Common\ClientTelemetry", "SendTelemetry", 3, RegistryValueKind.DWord, true), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
			("Disabling Office telemetry", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Microsoft\Office\16.0\Common", "qmenable", 0, RegistryValueKind.DWord, true), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
			("Disabling Office telemetry", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Microsoft\Office\16.0\Common", "sendcustomerdata", 0, RegistryValueKind.DWord, true), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
			("Disabling Office telemetry", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Microsoft\Office\16.0\Common", "updatereliabilitydata", 0, RegistryValueKind.DWord, true), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
			("Disabling Office telemetry", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Microsoft\Office\16.0\Common", "linkedin", 0, RegistryValueKind.DWord, true), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
			("Disabling Office telemetry", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Microsoft\Office\16.0\Common\General", "disablecomingsoon", 1, RegistryValueKind.DWord, true), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
			("Disabling Office telemetry", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Microsoft\Office\16.0\Common\General", "optindisable", 1, RegistryValueKind.DWord, true), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
			("Disabling Office telemetry", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Microsoft\Office\16.0\Common\General", "shownfirstrunoptin", 1, RegistryValueKind.DWord, true), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
			("Disabling Office telemetry", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Microsoft\Office\16.0\Common\General", "ShownFileFmtPrompt", 1, RegistryValueKind.DWord, true), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
			("Disabling Office telemetry", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Microsoft\Office\16.0\Common\General", "skydrivesigninoption", 0, RegistryValueKind.DWord, true), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
			("Disabling Office telemetry", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Microsoft\Office\16.0\Common\Feedback", "enabled", 0, RegistryValueKind.DWord, true), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
			("Disabling Office telemetry", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Microsoft\Office\16.0\Common\Feedback", "includescreenshot", 0, RegistryValueKind.DWord, true), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
			("Disabling Office telemetry", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Microsoft\Office\16.0\Common\Feedback", "includeemail", 0, RegistryValueKind.DWord, true), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
			("Disabling Office telemetry", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Microsoft\Office\16.0\Common\Feedback", "surveyenabled", 0, RegistryValueKind.DWord, true), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
			("Disabling Office telemetry", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Microsoft\Office\16.0\Common\Internet", "useonlinecontent", 0, RegistryValueKind.DWord, true), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
			("Disabling Office telemetry", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Microsoft\Office\16.0\Common\PTWatson", "PTWOptIn", 0, RegistryValueKind.DWord, true), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
			("Disabling Office telemetry", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Microsoft\Office\16.0\Common\Privacy", "disconnectedstate", 2, RegistryValueKind.DWord, true), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
			("Disabling Office telemetry", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Microsoft\Office\16.0\Common\Privacy", "usercontentdisabled", 2, RegistryValueKind.DWord, true), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
			("Disabling Office telemetry", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Microsoft\Office\16.0\Common\Privacy", "downloadcontentdisabled", 2, RegistryValueKind.DWord, true), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
			("Disabling Office telemetry", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Microsoft\Office\16.0\Common\Privacy", "controllerconnectedservicesenabled", 2, RegistryValueKind.DWord, true), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
			("Disabling Office telemetry", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Microsoft\Office\16.0\Common\Security\FileValidation", "disablereporting", 1, RegistryValueKind.DWord, true), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
			("Disabling Office telemetry", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Microsoft\Office\16.0\Firstrun", "BootedRTM", 1, RegistryValueKind.DWord, true), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
			("Disabling Office telemetry", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Microsoft\Office\16.0\Firstrun", "disablemovie", 1, RegistryValueKind.DWord, true), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
			("Disabling Office telemetry", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Microsoft\Office\16.0\Lync", "disableautomaticsendtracing", 1, RegistryValueKind.DWord, true), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
			("Disabling Office telemetry", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Microsoft\Office\16.0\Outlook\Options\Mail", "EnableLogging", 0, RegistryValueKind.DWord, true), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
			("Disabling Office telemetry", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Microsoft\Office\16.0\Word\Options", "EnableLogging", 0, RegistryValueKind.DWord, true), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
			("Disabling Office telemetry", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Microsoft\Office\16.0\OSM", "Enablelogging", 0, RegistryValueKind.DWord, true), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
			("Disabling Office telemetry", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Microsoft\Office\16.0\OSM", "EnableUpload", 0, RegistryValueKind.DWord, true), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
			("Disabling Office telemetry", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Microsoft\Office\16.0\OSM\PreventedApplications", "accesssolution", 1, RegistryValueKind.DWord, true), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
			("Disabling Office telemetry", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Microsoft\Office\16.0\OSM\PreventedApplications", "olksolution", 1, RegistryValueKind.DWord, true), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
			("Disabling Office telemetry", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Microsoft\Office\16.0\OSM\PreventedApplications", "onenotesolution", 1, RegistryValueKind.DWord, true), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
			("Disabling Office telemetry", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Microsoft\Office\16.0\OSM\PreventedApplications", "pptsolution", 1, RegistryValueKind.DWord, true), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
			("Disabling Office telemetry", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Microsoft\Office\16.0\OSM\PreventedApplications", "projectsolution", 1, RegistryValueKind.DWord, true), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
			("Disabling Office telemetry", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Microsoft\Office\16.0\OSM\PreventedApplications", "publishersolution", 1, RegistryValueKind.DWord, true), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
			("Disabling Office telemetry", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Microsoft\Office\16.0\OSM\PreventedApplications", "visiosolution", 1, RegistryValueKind.DWord, true), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
			("Disabling Office telemetry", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Microsoft\Office\16.0\OSM\PreventedApplications", "wdsolution", 1, RegistryValueKind.DWord, true), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
			("Disabling Office telemetry", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Microsoft\Office\16.0\OSM\PreventedApplications", "xlsolution", 1, RegistryValueKind.DWord, true), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
			("Disabling Office telemetry", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Microsoft\Office\16.0\OSM\PreventedSolutiontypes", "agave", 1, RegistryValueKind.DWord, true), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
			("Disabling Office telemetry", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Microsoft\Office\16.0\OSM\PreventedSolutiontypes", "appaddins", 1, RegistryValueKind.DWord, true), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
			("Disabling Office telemetry", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Microsoft\Office\16.0\OSM\PreventedSolutiontypes", "comaddins", 1, RegistryValueKind.DWord, true), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
			("Disabling Office telemetry", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Microsoft\Office\16.0\OSM\PreventedSolutiontypes", "documentfiles", 1, RegistryValueKind.DWord, true), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
			("Disabling Office telemetry", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Microsoft\Office\16.0\OSM\PreventedSolutiontypes", "templatefiles", 1, RegistryValueKind.DWord, true), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
			("Disabling Office telemetry", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Policies\Microsoft\Office\Common\ClientTelemetry", "SendTelemetry", 3, RegistryValueKind.DWord, true), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
			("Disabling Office telemetry", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Policies\Microsoft\Office\16.0\Common", "qmenable", 0, RegistryValueKind.DWord, true), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
			("Disabling Office telemetry", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Policies\Microsoft\Office\16.0\Common", "sendcustomerdata", 0, RegistryValueKind.DWord, true), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
			("Disabling Office telemetry", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Policies\Microsoft\Office\16.0\Common", "updatereliabilitydata", 0, RegistryValueKind.DWord, true), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
			("Disabling Office telemetry", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Policies\Microsoft\Office\16.0\Common", "linkedin", 0, RegistryValueKind.DWord, true), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
			("Disabling Office telemetry", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Policies\Microsoft\Office\16.0\Common\General", "disablecomingsoon", 1, RegistryValueKind.DWord, true), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
			("Disabling Office telemetry", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Policies\Microsoft\Office\16.0\Common\General", "optindisable", 1, RegistryValueKind.DWord, true), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
			("Disabling Office telemetry", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Policies\Microsoft\Office\16.0\Common\General", "shownfirstrunoptin", 0, RegistryValueKind.DWord, true), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
			("Disabling Office telemetry", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Policies\Microsoft\Office\16.0\Common\General", "ShownFileFmtPrompt", 0, RegistryValueKind.DWord, true), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
			("Disabling Office telemetry", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Policies\Microsoft\Office\16.0\Common\General", "skydrivesigninoption", 0, RegistryValueKind.DWord, true), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
			("Disabling Office telemetry", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Policies\Microsoft\Office\16.0\Common\Feedback", "enabled", 0, RegistryValueKind.DWord, true), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
			("Disabling Office telemetry", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Policies\Microsoft\Office\16.0\Common\Feedback", "includescreenshot", 0, RegistryValueKind.DWord, true), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
			("Disabling Office telemetry", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Policies\Microsoft\Office\16.0\Common\Feedback", "includeemail", 0, RegistryValueKind.DWord, true), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
			("Disabling Office telemetry", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Policies\Microsoft\Office\16.0\Common\Feedback", "surveyenabled", 0, RegistryValueKind.DWord, true), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
			("Disabling Office telemetry", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Policies\Microsoft\Office\16.0\Common\Internet", "useonlinecontent", 0, RegistryValueKind.DWord, true), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
			("Disabling Office telemetry", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Policies\Microsoft\Office\16.0\Common\PTWatson", "PTWOptIn", 0, RegistryValueKind.DWord, true), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
			("Disabling Office telemetry", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Policies\Microsoft\Office\16.0\Common\Privacy", "disconnectedstate", 2, RegistryValueKind.DWord, true), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
			("Disabling Office telemetry", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Policies\Microsoft\Office\16.0\Common\Privacy", "usercontentdisabled", 2, RegistryValueKind.DWord, true), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
			("Disabling Office telemetry", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Policies\Microsoft\Office\16.0\Common\Privacy", "downloadcontentdisabled", 2, RegistryValueKind.DWord, true), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
			("Disabling Office telemetry", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Policies\Microsoft\Office\16.0\Common\Privacy", "controllerconnectedservicesenabled", 2, RegistryValueKind.DWord, true), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
			("Disabling Office telemetry", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Policies\Microsoft\Office\16.0\Common\Security\FileValidation", "disablereporting", 1, RegistryValueKind.DWord, true), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
			("Disabling Office telemetry", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Policies\Microsoft\Office\16.0\Firstrun", "BootedRTM", 1, RegistryValueKind.DWord, true), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
			("Disabling Office telemetry", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Policies\Microsoft\Office\16.0\Firstrun", "disablemovie", 1, RegistryValueKind.DWord, true), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
			("Disabling Office telemetry", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Policies\Microsoft\Office\16.0\Lync", "disableautomaticsendtracing", 1, RegistryValueKind.DWord, true), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
			("Disabling Office telemetry", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Policies\Microsoft\Office\16.0\Outlook\Options\Mail", "EnableLogging", 0, RegistryValueKind.DWord, true), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
			("Disabling Office telemetry", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Policies\Microsoft\Office\16.0\Word\Options", "EnableLogging", 0, RegistryValueKind.DWord, true), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
			("Disabling Office telemetry", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Policies\Microsoft\Office\16.0\OSM", "Enablelogging", 0, RegistryValueKind.DWord, true), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
			("Disabling Office telemetry", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Policies\Microsoft\Office\16.0\OSM", "EnableUpload", 0, RegistryValueKind.DWord, true), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
			("Disabling Office telemetry", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Policies\Microsoft\Office\16.0\OSM\PreventedApplications", "accesssolution", 1, RegistryValueKind.DWord, true), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
			("Disabling Office telemetry", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Policies\Microsoft\Office\16.0\OSM\PreventedApplications", "olksolution", 1, RegistryValueKind.DWord, true), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
			("Disabling Office telemetry", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Policies\Microsoft\Office\16.0\OSM\PreventedApplications", "onenotesolution", 1, RegistryValueKind.DWord, true), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
			("Disabling Office telemetry", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Policies\Microsoft\Office\16.0\OSM\PreventedApplications", "pptsolution", 1, RegistryValueKind.DWord, true), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
			("Disabling Office telemetry", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Policies\Microsoft\Office\16.0\OSM\PreventedApplications", "projectsolution", 1, RegistryValueKind.DWord, true), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
			("Disabling Office telemetry", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Policies\Microsoft\Office\16.0\OSM\PreventedApplications", "publishersolution", 1, RegistryValueKind.DWord, true), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
			("Disabling Office telemetry", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Policies\Microsoft\Office\16.0\OSM\PreventedApplications", "visiosolution", 1, RegistryValueKind.DWord, true), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
			("Disabling Office telemetry", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Policies\Microsoft\Office\16.0\OSM\PreventedApplications", "wdsolution", 1, RegistryValueKind.DWord, true), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
			("Disabling Office telemetry", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Policies\Microsoft\Office\16.0\OSM\PreventedApplications", "xlsolution", 1, RegistryValueKind.DWord, true), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
			("Disabling Office telemetry", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Policies\Microsoft\Office\16.0\OSM\PreventedSolutiontypes", "agave", 1, RegistryValueKind.DWord, true), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
			("Disabling Office telemetry", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Policies\Microsoft\Office\16.0\OSM\PreventedSolutiontypes", "appaddins", 1, RegistryValueKind.DWord, true), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
			("Disabling Office telemetry", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Policies\Microsoft\Office\16.0\OSM\PreventedSolutiontypes", "comaddins", 1, RegistryValueKind.DWord, true), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
			("Disabling Office telemetry", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Policies\Microsoft\Office\16.0\OSM\PreventedSolutiontypes", "documentfiles", 1, RegistryValueKind.DWord, true), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
			("Disabling Office telemetry", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Policies\Microsoft\Office\16.0\OSM\PreventedSolutiontypes", "templatefiles", 1, RegistryValueKind.DWord, true), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
			("Disabling Office telemetry", async () => await Process.Start(new ProcessStartInfo { FileName = "reg.exe", Arguments = @"unload HKU\DefaultUser", CreateNoWindow = true })!.WaitForExitAsync(), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
			
			// download capframex
			("Downloading CapFrameX", async () => await DownloadHelper.Download("https://cxblobs.blob.core.windows.net/releases/release_1.8.5_installer.zip", Path.GetTempPath(), "release_installer.zip"), () => CapFrameX == true),

			// install capframex
			("Installing CapFrameX", async () => await ExtractHelper.Extract(Path.Combine(Path.GetTempPath(), "release_installer.zip"), Path.Combine(Path.GetTempPath(), "CapFrameX")), () => CapFrameX == true),
			("Installing CapFrameX", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Path.GetTempPath(), "CapFrameX", "CapFrameXBootstrapper.exe"), Arguments = "/quiet /norestart" , WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), () => CapFrameX == true),
			("Cleaning up CapFrameX files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "release_installer.zip")), () => CapFrameX == true),
			("Cleaning up CapFrameX files", async () => Directory.Delete(Path.Combine(Path.GetTempPath(), "CapFrameX"), true), () => CapFrameX == true),

			// remove capframex desktop shortcut
			("Removing CapFrameX desktop shortcut", async () => File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory), "CapFrameX.lnk")), () => CapFrameX == true),

			// download minitool partition wizard
			("Downloading MiniTool Partition Wizard", async () => await DownloadHelper.Download("https://cdn2.minitool.com/?p=pw&e=pw-free-offline", Path.GetTempPath(), "pw-free-offline.exe", reporter: reporter), () => MinitoolPartitionWizard == true),

			// install minitool partition wizard
			("Installing MiniTool Partition Wizard", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Path.GetTempPath(), "pw-free-offline.exe"), Arguments = "/VERYSILENT /SUPPRESSMSGBOXES /NORESTART" , WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), () => MinitoolPartitionWizard == true),
			("Installing MiniTool Partition Wizard", async () => { while (new[] { "partitionwizard", "OpenWith", "msedge" }.All(name => Process.GetProcessesByName(name).Length == 0)) await Task.Delay(500); foreach (Process process in new[] { "partitionwizard", "OpenWith", "msedge" }.SelectMany(Process.GetProcessesByName)) { process.Kill(); process.WaitForExit(); } }, () => MinitoolPartitionWizard == true),
			("Cleaning up MiniTool Partition Wizard files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "pw-free-offline.exe")), () => MinitoolPartitionWizard == true),

			// remove minitool partition wizard desktop shortcut
			("Removing MiniTool Partition Wizard desktop shortcut", async () => File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory), "MiniTool Partition Wizard.lnk")), () => MinitoolPartitionWizard == true),

			// disable minitool partition wizard notifications
			("Disabling MiniTool Partition Wizard notifications", async () => await Process.Start(new ProcessStartInfo { FileName = "reg.exe", Arguments = $@"load HKU\DefaultUser ""{Path.Combine(Path.GetPathRoot(Environment.SystemDirectory)!, "Users", "Default", "NTUSER.DAT")}""", CreateNoWindow = true })!.WaitForExitAsync(), () => MinitoolPartitionWizard == true),
			("Disabling MiniTool Partition Wizard notifications", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\MiniTool Software Limited\MiniTool Partition Wizard", "00cfb691-7786-46e4-a4af-7e2cb0eb10c5", "2", RegistryValueKind.DWord), () => MinitoolPartitionWizard == true),
			("Disabling MiniTool Partition Wizard notifications", async () => await Process.Start(new ProcessStartInfo { FileName = "reg.exe", Arguments = @"unload HKU\DefaultUser", CreateNoWindow = true })!.WaitForExitAsync(), () => MinitoolPartitionWizard == true),

			// disable minitool partition wizard startup entries
			("Disabling MiniTool Partition Wizard startup entries", async () => TaskSchedulerHelper.Toggle(@"MiniToolPartitionWizard", false), () => MinitoolPartitionWizard == true),
			("Disabling MiniTool Partition Wizard startup entries", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run", "MTPW", new byte[] { 0x01 }, RegistryValueKind.Binary), () => MinitoolPartitionWizard == true),

			// download aomei partition assistant
			("Downloading AOMEI Partition Assistant", async () => await DownloadHelper.Download("https://www2.aomeisoftware.com/download/pa/PAssist_ProDemo.exe", Path.GetTempPath(), "PAssist_ProDemo.exe", reporter: reporter), () => AomeiPartitionAssistant == true),

			// install aomei partition assistant
			("Installing AOMEI Partition Assistant", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Path.GetTempPath(), "PAssist_ProDemo.exe"), Arguments = "/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /SP-" , WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), () => AomeiPartitionAssistant == true),
			("Cleaning up AOMEI Partition Assistant files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "PAssist_ProDemo.exe")), () => AomeiPartitionAssistant == true),

			// activate aomei partition assistant
			("Activating AOMEI Partition Assistant", async () => { var iniHelper = new InIHelper(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "AOMEI Partition Assistant", "cfg.ini")); iniHelper.AddValue("KEY", "AOPR-CM948-83ZJZ-4NQW1", "CONFIG"); }, () => AomeiPartitionAssistant == true),

			// remove aomei partition assistant desktop shortcut
			("Removing AOMEI Partition Assistant desktop shortcut", async () => File.Delete(Directory.GetFiles(Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory), "AOMEI Partition Assistant*.lnk").FirstOrDefault()), () => AomeiPartitionAssistant == true),

			// download wiztree
			("Downloading WizTree", async () => await DownloadHelper.Download("https://diskanalyzer.com/files/wiztree_4_31_setup.exe", Path.GetTempPath(), "wiztree_setup.exe", reporter: reporter), () => WizTree == true),

			// install wiztree
			("Installing WizTree", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Path.GetTempPath(), "wiztree_setup.exe"), Arguments = "/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /SP- /MERGETASKS=!desktopicon" , WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), () => WizTree == true),
			("Cleaning up WizTree files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "wiztree_setup.exe")), () => WizTree == true),

			// download crystal disk info
			("Downloading CrystalDiskInfo", async () => await DownloadHelper.Download("https://deac-ams.dl.sourceforge.net/project/crystaldiskinfo/9.9.1/CrystalDiskInfo9_9_1.exe?viasf=1&fid=8679e6d10c13a1ea", Path.GetTempPath(), "CrystalDiskInfo.exe", reporter: reporter), () => CrystalDiskInfo == true),

			// install crystal disk info
			("Installing CrystalDiskInfo", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Path.GetTempPath(), "CrystalDiskInfo.exe"), Arguments = "/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /SP- /MERGETASKS=!desktopicon" , WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), () => CrystalDiskInfo == true),
			("Cleaning up CrystalDiskInfo files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "CrystalDiskInfo.exe")), () => CrystalDiskInfo == true),

			// download crystal disk mark
			("Downloading CrystalDiskMark", async () => await DownloadHelper.Download("https://sf-eu-introserv-3.dl.sourceforge.net/project/crystaldiskmark/9.0.3/CrystalDiskMark9_0_3.exe?viasf=1&fid=3146e97b3c195781", Path.GetTempPath(), "CrystalDiskMark.exe", reporter: reporter), () => CrystalDiskMark == true),

			// install crystal disk mark
			("Installing CrystalDiskMark", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Path.GetTempPath(), "CrystalDiskMark.exe"), Arguments = "/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /SP- /MERGETASKS=!desktopicon" , WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), () => CrystalDiskMark == true),
			("Cleaning up CrystalDiskMark files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "CrystalDiskMark.exe")), () => CrystalDiskMark == true),

			// remove crystal disk mark desktop shortcut
			("Removing CrystalDiskMark desktop shortcut", async () => File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "CrystalDiskMark 9.lnk")), () => CrystalDiskMark == true),

			// download proton vpn
			("Downloading Proton VPN", async () => await DownloadHelper.Download("https://vpn.protondownload.com/download/ProtonVPN_v5.1.6_x64.exe", Path.GetTempPath(), "ProtonVPN_x64.exe", reporter: reporter), () => ProtonVPN == true),

			// install proton vpn
			("Installing Proton VPN", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Path.GetTempPath(), "ProtonVPN_x64.exe"), Arguments = "/SP- /VERYSILENT /SUPPRESSMSGBOXES /NORESTART" , WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), () => ProtonVPN == true),
			("Installing Proton VPN", async () => { foreach (Process process in Process.GetProcessesByName("ProtonVPN.Client")) { process.Kill(); process.WaitForExit(); } }, () => ProtonVPN == true),
			("Installing Proton VPN", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run", "Proton VPN", new byte[] { 0x03 }, RegistryValueKind.Binary), () => ProtonVPN == true),
			("Cleaning up Proton VPN files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "ProtonVPN_x64.exe")), () => ProtonVPN == true),

			// remove proton vpn desktop shortcut
			("Removing Proton VPN desktop shortcut", async () => File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory), "Proton VPN.lnk")), () => ProtonVPN == true),

			// download bulk crap uninstaller
			("Downloading Bulk Crap Uninstaller", async () => await DownloadHelper.Download(JsonDocument.Parse(await new HttpClient { DefaultRequestHeaders = { { "User-Agent", "AutoOS" } } }.GetStringAsync("https://api.github.com/repos/Klocman/Bulk-Crap-Uninstaller/releases/latest")).RootElement.GetProperty("assets").EnumerateArray().First(a => a.GetProperty("name").GetString().Contains("setup.exe")).GetProperty("browser_download_url").GetString(), Path.GetTempPath(), "BCUninstaller_setup.exe", reporter: reporter), () => BulkCrapUninstaller == true),
			
			// install bulk crap uninstaller
			("Installing Bulk Crap Uninstaller", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Path.GetTempPath(), "BCUninstaller_setup.exe"), Arguments = "/VERYSILENT /SUPPRESSMSGBOXES /NORESTART" , WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), () => BulkCrapUninstaller == true),
			("Cleaning up Bulk Crap Uninstaller files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "BCUninstaller_setup.exe")), () => BulkCrapUninstaller == true),

			// remove bulk crap uninstaller desktop shortcut
			("Removing Bulk Crap Uninstaller desktop shortcut", async () => File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory), "BCUninstaller.lnk")), () => BulkCrapUninstaller == true),
		  
			// download bluetooth audio receiver
			("Downloading Bluetooth Audio Receiver", async () => await StoreHelper.Download("55746MarkSmirnov.BluetoothAudioReveicer_xwrbx6997tsfc", reporter: reporter), () => BluetoothAudioReceiver == true),

			// install bluetooth audio receiver
			("Installing Bluetooth Audio Receiver", async () => await StoreHelper.Install("55746MarkSmirnov.BluetoothAudioReveicer_xwrbx6997tsfc"), () => BluetoothAudioReceiver == true),

			// download anydesk
			("Downloading AnyDesk", async () => await DownloadHelper.Download("https://download.anydesk.com/AnyDesk.exe", Path.GetTempPath(), "AnyDesk.exe", reporter: reporter), () => AnyDesk == true),
			
			// install anydesk
			("Installing AnyDesk", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Path.GetTempPath(), "AnyDesk.exe"), Arguments = @"--install ""C:\Program Files (x86)\AnyDesk"" --start-with-win --silent --create-shortcuts --create-desktop-icon" , WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), () => AnyDesk == true),
			("Installing AnyDesk", async () => { foreach (Process process in Process.GetProcessesByName("AnyDesk")) { process.Kill(); process.WaitForExit(); } }, () => AnyDesk == true),
			("Cleaning up AnyDesk files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "AnyDesk.exe")), () => AnyDesk == true),
		
			// disable anydesk startup entries 
			("Disabling AnyDesk startup entries", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\AnyDesk", "Start", 3, RegistryValueKind.DWord), () => AnyDesk == true),
			("Disabling AnyDesk startup entries", async () => ServicesHelper.StopService("AnyDesk"), () => AnyDesk == true),
			("Disabling AnyDesk startup entries", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\StartupFolder", "AnyDesk.lnk", new byte[] { 0x03 }, RegistryValueKind.Binary), () => AnyDesk == true),

			// remove anydesk desktop shortcut
			("Removing AnyDesk desktop shortcut", async () => File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory), "AnyDesk.lnk")), () => AnyDesk == true),

			// download rustdesk
			("Downloading RustDesk", async () => await DownloadHelper.Download(JsonDocument.Parse(await new HttpClient { DefaultRequestHeaders = { { "User-Agent", "AutoOS" } } }.GetStringAsync("https://api.github.com/repos/rustdesk/rustdesk/releases")).RootElement.EnumerateArray().First(release => !release.GetProperty("prerelease").GetBoolean() && release.GetProperty("assets").EnumerateArray().Any(asset => asset.GetProperty("name").GetString().Contains("x86_64") && asset.GetProperty("name").GetString().EndsWith(".msi"))).GetProperty("assets").EnumerateArray().First(asset => asset.GetProperty("name").GetString().Contains("x86_64") && asset.GetProperty("name").GetString().EndsWith(".msi")).GetProperty("browser_download_url").GetString(), Path.GetTempPath(), "rustdesk-x86_64.msi", reporter: reporter), () => RustDesk == true),
			
			// install rustdesk
			("Installing RustDesk", async () => await Process.Start(new ProcessStartInfo { FileName = "msiexec.exe", Arguments = $@"/i ""{Path.Combine(Path.GetTempPath(), "rustdesk-x86_64.msi")}"" /qn" , WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), () => RustDesk == true),
			("Installing RustDesk", async () => { foreach (Process process in Process.GetProcessesByName("RustDesk")) { process.Kill(); process.WaitForExit(); } }, () => RustDesk == true),
			("Cleaning up RustDesk files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "rustdesk-x86_64.msi")), () => RustDesk == true),

			// disable rustdesk startup entry
			("Disabling RustDesk startup entry", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\RustDesk", "Start", 4, RegistryValueKind.DWord), () => RustDesk == true),
			("Disabling RustDesk startup entry", async () => ServicesHelper.StopService("RustDesk"), () => RustDesk == true),
			("Disabling RustDesk startup entry", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\StartupFolder", "RustDesk Tray.lnk", new byte[] { 0x03 }, RegistryValueKind.Binary), () => RustDesk == true),

			// remove rustdesk desktop shortcut
			("Removing RustDesk desktop shortcut", async () => File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory), "RustDesk.lnk")), () => RustDesk == true),

			// download apollo
			("Downloading Apollo", async () => await DownloadHelper.Download(JsonDocument.Parse(await new HttpClient { DefaultRequestHeaders = { { "User-Agent", "AutoOS" } } }.GetStringAsync("https://api.github.com/repos/ClassicOldSong/Apollo/releases")).RootElement.EnumerateArray().First(release => release.GetProperty("assets").EnumerateArray().Any(asset => asset.GetProperty("name").GetString().EndsWith(".exe"))).GetProperty("assets").EnumerateArray().First(asset => asset.GetProperty("name").GetString().EndsWith(".exe")).GetProperty("browser_download_url").GetString(), Path.GetTempPath(), "Apollo.exe", reporter: reporter), () => Apollo == true),
			
			// install apollo
			("Installing Apollo", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Path.GetTempPath(), "Apollo.exe"), Arguments = "/S", WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), () => Apollo == true),
			("Cleaning up Apollo files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "Apollo.exe")), () => Apollo == true),

			// download moonlight
			("Downloading Moonlight", async () => await DownloadHelper.Download(JsonDocument.Parse(await new HttpClient { DefaultRequestHeaders = { { "User-Agent", "AutoOS" } } }.GetStringAsync("https://api.github.com/repos/moonlight-stream/moonlight-qt/releases")).RootElement.EnumerateArray().First(release => !release.GetProperty("prerelease").GetBoolean() && release.GetProperty("assets").EnumerateArray().Any(asset => asset.GetProperty("name").GetString().EndsWith(".exe"))).GetProperty("assets").EnumerateArray().First(asset => asset.GetProperty("name").GetString().EndsWith(".exe")).GetProperty("browser_download_url").GetString(), Path.GetTempPath(), "MoonlightSetup.exe", reporter: reporter), () => Moonlight == true),
			
			// install moonlight
			("Installing Moonlight", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Path.GetTempPath(), "MoonlightSetup.exe"), Arguments = "/quiet /norestart", WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), () => Moonlight == true),
			("Cleaning up Moonlight files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "MoonlightSetup.exe")), () => Moonlight == true),

			// remove moonlight desktop shortcut
			("Removing Moonlight desktop shortcut", async () => File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory), "Moonlight.lnk")), () => Moonlight == true),
			
			// download autohotkey
			("Downloading AutoHotkey", async () => await DownloadHelper.Download(JsonDocument.Parse(await new HttpClient { DefaultRequestHeaders = { { "User-Agent", "AutoOS" } } }.GetStringAsync("https://api.github.com/repos/AutoHotkey/AutoHotkey/releases")).RootElement.EnumerateArray().First(release => !release.GetProperty("prerelease").GetBoolean() && release.GetProperty("assets").EnumerateArray().Any(asset => asset.GetProperty("name").GetString().EndsWith("_setup.exe"))).GetProperty("assets").EnumerateArray().First(asset => asset.GetProperty("name").GetString().EndsWith("_setup.exe")).GetProperty("browser_download_url").GetString(), Path.GetTempPath(), "AutoHotkey_setup.exe", reporter: reporter), () => AutoHotkey == true),
			
			// install autohotkey
			("Installing AutoHotkey", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Path.GetTempPath(), "AutoHotkey_setup.exe"), Arguments = "/silent", WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), () => AutoHotkey == true),
			("Cleaning up AutoHotkey files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "AutoHotkey_setup.exe")), () => AutoHotkey == true),

			// download emeditor
			("Downloading EmEditor", async () => await StoreHelper.Download("Emurasoft.EmEditor64UWP_ws7rg9hnwrpxm", reporter: reporter), () => EmEditor == true),

			// install emeditor
			("Installing EmEditor", async () => await StoreHelper.Install("Emurasoft.EmEditor64UWP_ws7rg9hnwrpxm"), () => EmEditor == true),

			// disable emeditor startup entry
			("Disabling EmEditor startup entry", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\StartupFolder", "EmEditor.lnk", new byte[] { 0x03 }, RegistryValueKind.Binary), () => EmEditor == true),

			// download windbg
			("Downloading WinDbg", async () => await StoreHelper.Download("Microsoft.WinDbg_8wekyb3d8bbwe", reporter: reporter), () => WinDbg == true),

			// install windbg
			("Installing WinDbg", async () => await StoreHelper.Install("Microsoft.WinDbg_8wekyb3d8bbwe"), () => WinDbg == true),

			// download qbittorrent
			("Downloading qBittorrent", async () => await DownloadHelper.Download(JsonDocument.Parse(await new HttpClient { DefaultRequestHeaders = { { "User-Agent", "AutoOS" } } }.GetStringAsync("https://api.github.com/repos/qbittorrent/qBittorrent/releases")).RootElement.EnumerateArray().First(release => !release.GetProperty("prerelease").GetBoolean() && release.GetProperty("assets").EnumerateArray().Any(asset => asset.GetProperty("name").GetString().Contains("x64_setup.exe"))).GetProperty("assets").EnumerateArray().First(asset => asset.GetProperty("name").GetString().Contains("x64_setup.exe")).GetProperty("browser_download_url").GetString(), Path.GetTempPath(), "qbittorrent_x64_setup.exe", reporter: reporter), () => QBittorrent == true),

			// install qbittorrent
			("Installing qBittorrent", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Path.GetTempPath(), "qbittorrent_x64_setup.exe"), Arguments = "/S", WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), () => QBittorrent == true),
			("Cleaning up qBittorrent files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "qbittorrent_x64_setup.exe")), () => QBittorrent == true),

			// download deluge
			("Downloading Deluge", async () => await DownloadHelper.Download("http://download.deluge-torrent.org/windows/deluge-2.2.0-win64-setup.exe", Path.GetTempPath(), "deluge-win64-setup.exe", reporter: reporter), () => Deluge == true),

			// install deluge
			("Installing Deluge", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Path.GetTempPath(), "deluge-win64-setup.exe"), Arguments = "/S", WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), () => Deluge == true),
			("Cleaning up Deluge files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "deluge-win64-setup.exe")), () => Deluge == true),

			// download free download manager
			("Downloading Free Download Manager", async () => await DownloadHelper.Download("https://files2.freedownloadmanager.org/6/latest/fdm_x64_setup.exe", Path.GetTempPath(), "fdm_x64_setup.exe", reporter: reporter), () => FreeDownloadManager == true),

			// install free download manager
			("Installing Free Download Manager", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Path.GetTempPath(), "fdm_x64_setup.exe"), Arguments = "/SP- /VERYSILENT /SUPPRESSMSGBOXES /NORESTART", WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), () => FreeDownloadManager == true),
			("Cleaning up Free Download Manager files", async () => File.Delete(Path.Combine(Path.GetTempPath(), "fdm_x64_setup.exe")), () => FreeDownloadManager == true),

            // remove free download manager desktop shortcut
			("Removing Free Download Manager desktop shortcut", async () => File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory), "Free Download Manager.lnk")), () => FreeDownloadManager == true),
			("Disabling Free Download Manager startup entry", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run", "Free Download Manager", new byte[] { 0x03 }, RegistryValueKind.Binary), () => FreeDownloadManager == true),
        }; 

		if (selection != null)
		{
			return [.. actions.Where(action => action.Condition != null && action.Condition.Invoke())];
		}

		return actions;
	}
}

