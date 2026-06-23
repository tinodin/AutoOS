using AutoOS.Common;
using AutoOS.Views.Installer.Stages;
using AutoOS.Views.Updater;
using System.Collections.ObjectModel;
using WinRT;

namespace AutoOS.Views.Settings;

public sealed partial class ApplicationsPage : Page
{
	private readonly ObservableCollection<GridViewItem> messagingItems = [];
	private readonly ObservableCollection<GridViewItem> launchersItems = [];
	private readonly ObservableCollection<GridViewItem> musicItems = [];
	private readonly ObservableCollection<GridViewItem> peripheralsItems = [];
	private readonly ObservableCollection<GridViewItem> controllersItems = [];
	private readonly ObservableCollection<GridViewItem> developmentItems = [];
	private readonly ObservableCollection<GridViewItem> sysinternalsItems = [];
	private readonly ObservableCollection<GridViewItem> overclockingItems = [];
	private readonly ObservableCollection<GridViewItem> musicProductionItems = [];
	private readonly ObservableCollection<GridViewItem> videoProductionItems = [];
	private readonly ObservableCollection<GridViewItem> multimediaItems = [];
	private readonly ObservableCollection<GridViewItem> officeItems = [];
	private readonly ObservableCollection<GridViewItem> miscellaneousItems = [];


	public ApplicationsPage()
	{
		InitializeComponent();
		GetItems();

		Messaging.ItemsSource = messagingItems;
		Launchers.ItemsSource = launchersItems;
		Music.ItemsSource = musicItems;
		Peripherals.ItemsSource = peripheralsItems;
		Controllers.ItemsSource = controllersItems;
		Development.ItemsSource = developmentItems;
		Sysinternals.ItemsSource = sysinternalsItems;
		Overclocking.ItemsSource = overclockingItems;
		MusicProduction.ItemsSource = musicProductionItems;
		VideoProduction.ItemsSource = videoProductionItems;
		Multimedia.ItemsSource = multimediaItems;
		Office.ItemsSource = officeItems;
		Miscellaneous.ItemsSource = miscellaneousItems;
	}

	public Visibility GetVisibility(int count) => count > 0 ? Visibility.Visible : Visibility.Collapsed;

	private void GetItems()
	{
		var messagingList = new List<GridViewItem>
		{
			new() { Text = "Discord", ImageSource = "ms-appx:///Assets/Fluent/Discord.png", IsInstalled = Directory.GetDirectories(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Discord").SelectMany(directory => Directory.GetDirectories(directory, "app-*")).Any() },
			new() { Text = "WhatsApp", ImageSource = "ms-appx:///Assets/Fluent/Whatsapp.png", IsInstalled = Directory.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Packages", "5319275A.WhatsAppDesktop_cv1g1gvanyjgm")) },
			new() { Text = "Telegram Desktop", ImageSource = "ms-appx:///Assets/Fluent/Telegram.png", IsInstalled = Directory.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Packages", "TelegramMessengerLLP.TelegramDesktop_t4vj0pshhgkwm")) },
			new() { Text = "Unigram", ImageSource = "ms-appx:///Assets/Fluent/Unigram.png", IsInstalled = Directory.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Packages", "38833FF26BA1D.UnigramPreview_g9c9v27vpyspw")) },
			new() { Text = "Zoom Workplace", ImageSource = "ms-appx:///Assets/Fluent/Zoom.png", IsInstalled = File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Zoom", "bin", "Zoom.exe")) },
			new() { Text = "Thunderbird", ImageSource = "ms-appx:///Assets/Fluent/Thunderbird.png", IsInstalled = File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Mozilla Thunderbird", "thunderbird.exe")) },
			new() { Text = "Signal", ImageSource = "ms-appx:///Assets/Fluent/Signal.png", IsInstalled = File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "signal-desktop", "Signal.exe")) }
		};
		foreach (var item in messagingList.Where(item => !item.IsInstalled))
			messagingItems.Add(item);

		var launchersList = new List<GridViewItem>
		{
			new() { Text = "Epic Games", ImageSource = "ms-appx:///Assets/Fluent/EpicGames.png", IsInstalled = File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Epic Games", "Launcher", "Portal", "Binaries", "Win64", "EpicGamesLauncher.exe")) },
			new() { Text = "Steam", ImageSource = "ms-appx:///Assets/Fluent/Steam.png", IsInstalled = File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam", "steam.exe")) },
			new() { Text = "Riot Client", ImageSource = "ms-appx:///Assets/Fluent/RiotClient.png", IsInstalled = File.Exists(Path.Combine(Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.System)), "Riot Games", "Riot Client", "RiotClientServices.exe")) },
			new() { Text = "Ubisoft Connect", ImageSource = "ms-appx:///Assets/Fluent/UbisoftConnect.png", IsInstalled = File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Ubisoft", "Ubisoft Game Launcher", "UbisoftConnect.exe")) },
			new() { Text = "EA", ImageSource = "ms-appx:///Assets/Fluent/EA.png", IsInstalled = File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Electronic Arts", "EA Desktop", "EA Desktop", "EADesktop.exe")) },
			new() { Text = "Battle.Net", ImageSource = "ms-appx:///Assets/Fluent/BattleNet.png", IsInstalled = File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Battle.net", "Battle.net.exe")) },
			new() { Text = "Minecraft Launcher", ImageSource = "ms-appx:///Assets/Fluent/MinecraftLauncher.png", IsInstalled = File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Minecraft Launcher", "MinecraftLauncher.exe")) },
			new() { Text = "CurseForge", ImageSource = "ms-appx:///Assets/Fluent/CurseForge.png", IsInstalled = File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "CurseForge", "CurseForge.exe")) },
			new() { Text = "Prism Launcher", ImageSource = "ms-appx:///Assets/Fluent/PrismLauncher.png", IsInstalled = File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "PrismLauncher", "prismlauncher.exe")) },
			new() { Text = "Lunar Client", ImageSource = "ms-appx:///Assets/Fluent/LunarClient.png", IsInstalled = File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "launcher", "Lunar Client.exe")) },
			new() { Text = "Feather Client", ImageSource = "ms-appx:///Assets/Fluent/FeatherClient.png", IsInstalled = File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "feather", "Feather Launcher.exe")) },
			new() { Text = "Froststrap", ImageSource = "ms-appx:///Assets/Fluent/Froststrap.png", IsInstalled = File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "Froststrap", "Froststrap.exe")) },
			new() { Text = "Rockstar Games Launcher", ImageSource = "ms-appx:///Assets/Fluent/RockstarGamesLauncher.png", IsInstalled = File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Rockstar Games", "Launcher", "Launcher.exe")) },
			new() { Text = "FiveM", ImageSource = "ms-appx:///Assets/Fluent/FiveM.jpg", IsInstalled = File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FiveM", "FiveM.exe")) },
			new() { Text = "FACEIT", ImageSource = "ms-appx:///Assets/Fluent/FACEIT.png", IsInstalled = File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FACEIT", "FACEIT.exe")) },
			new() { Text = "Eden", ImageSource = "ms-appx:///Assets/Fluent/Eden.png", IsInstalled = File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Eden", "eden.exe")) }
		};
		foreach (var item in launchersList.Where(item => !item.IsInstalled))
			launchersItems.Add(item);

		var musicList = new List<GridViewItem>
		{
			new() { Text = "Apple Music", ImageSource = "ms-appx:///Assets/Fluent/AppleMusic.png", IsInstalled = Directory.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Packages", "AppleInc.AppleMusicWin_nzyj5cx40ttqa")) },
			new() { Text = "TIDAL", ImageSource = "ms-appx:///Assets/Fluent/Tidal.png", IsInstalled = Directory.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Packages", "WiMPMusic.27241E05630EA_kn85bz84x7te4")) },
			new() { Text = "Qobuz", ImageSource = "ms-appx:///Assets/Fluent/Qobuz.png", IsInstalled = Directory.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Qobuz")) },
			new() { Text = "Amazon Music", ImageSource = "ms-appx:///Assets/Fluent/AmazonMusic.png", IsInstalled = Directory.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Packages", "AmazonMobileLLC.AmazonMusic_kc6t79cpj4tp0")) },
			new() { Text = "Deezer Music", ImageSource = "ms-appx:///Assets/Fluent/DeezerMusic.png", IsInstalled = Directory.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Packages", "Deezer.62021768415AF_q7m17pa7q8kj0")) },
			new() { Text = "Spotify", ImageSource = "ms-appx:///Assets/Fluent/Spotify.png", IsInstalled = File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Spotify", "Spotify.exe")) },
			new() { Text = "MusicBee", ImageSource = "ms-appx:///Assets/Fluent/MusicBee.png", IsInstalled = Directory.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Packages", "50072StevenMayall.MusicBee_kcr266et74avj")) }
		};
		foreach (var item in musicList.Where(item => !item.IsInstalled))
			musicItems.Add(item);

		var peripheralsList = new List<GridViewItem>
		{
			new() { Text = "Logitech G HUB", ImageSource = "ms-appx:///Assets/Fluent/Logitech.png", IsInstalled = File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "LGHUB", "lghub.exe")) },
			new() { Text = "Logitech Onboard Memory Manager", ImageSource = "ms-appx:///Assets/Fluent/Logitech.png", IsInstalled = File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Logitech Onboard Memory Manager", "OnboardMemoryManager.exe")) },
			new() { Text = "Wootility", ImageSource = "ms-appx:///Assets/Fluent/Wootility.png", IsInstalled = File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "wootility", "Wootility.exe")) },
			new() { Text = "Endgame Gear", ImageSource = "ms-appx:///Assets/Fluent/EndgameGear.png", IsInstalled = File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Endgame", "GamingUtility", "ENDGAME GEAR.exe")) },
			new() { Text = "Glorious CORE", ImageSource = "ms-appx:///Assets/Fluent/GloriousCORE.png", IsInstalled = File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Glorious CORE", "Glorious Core.exe")) },
			new() { Text = "MCHOSE HUB", ImageSource = "ms-appx:///Assets/Fluent/MCHOSE.png", IsInstalled = File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "MCHOSE HUB", "MCHOSE HUB.exe")) },
			new() { Text = "SteelSeries GG", ImageSource = "ms-appx:///Assets/Fluent/SteelSeriesGG.png", IsInstalled = File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "SteelSeries", "GG", "SteelSeriesGGEZ.exe")) },
			new() { Text = "Razer Synapse", ImageSource = "ms-appx:///Assets/Fluent/RazerSynapse.png", IsInstalled = File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Razer", "RazerAppEngine", "RazerAppEngine.exe")) },
			new() { Text = "Corsair iCUE", ImageSource = "ms-appx:///Assets/Fluent/CorsairICue.png", IsInstalled = File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Corsair", "Corsair iCUE5 Software", "iCUE.exe")) },
			new() { Text = "OpenRGB", ImageSource = "ms-appx:///Assets/Fluent/OpenRGB.png", IsInstalled = File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "OpenRGB", "OpenRGB.exe")) },
			new() { Text = "FanControl", ImageSource = "ms-appx:///Assets/Fluent/FanControl.png", IsInstalled = File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "FanControl", "FanControl.exe")) },
			new() { Text = "GHelper", ImageSource = "ms-appx:///Assets/Fluent/GHelper.png", IsInstalled = File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "GHelper", "GHelper.exe")) }
		};
		foreach (var item in peripheralsList.Where(item => !item.IsInstalled))
			peripheralsItems.Add(item);

		var controllersList = new List<GridViewItem>
		{
			new() { Text = "ViGEmBus", ImageSource = "ms-appx:///Assets/Fluent/ViGEmBus.png", IsInstalled = File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "drivers", "ViGEmBus.sys")) },
			new() { Text = "HidHide", ImageSource = "ms-appx:///Assets/Fluent/HidHide.png", IsInstalled = File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "drivers", "HidHide.sys")) },
			new() { Text = "DualSenseY", ImageSource = "ms-appx:///Assets/Fluent/DualSenseY.png", IsInstalled = File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "DualSenseY", "DualSenseY.exe")) },
			new() { Text = "RaceElement", ImageSource = "ms-appx:///Assets/Fluent/RaceElement.png", IsInstalled = File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "RaceElement", "RaceElement.exe")) },
			new() { Text = "PlayStation® Accessories", ImageSource = "ms-appx:///Assets/Fluent/PlaystationAccessories.png", IsInstalled = File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Sony", "PlayStationAccessories", "PlayStationAccessories.exe")) },
			new() { Text = "Xbox Accessories", ImageSource = "ms-appx:///Assets/Fluent/XboxAccessories.png", IsInstalled = Directory.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Packages", "Microsoft.XboxDevices_8wekyb3d8bbwe")) },
		};
		foreach (var item in controllersList.Where(item => !item.IsInstalled))
			controllersItems.Add(item);

		var devList = new List<GridViewItem>
		{
			new() { Text = "Visual Studio", ImageSource = "ms-appx:///Assets/Fluent/VisualStudio.png", IsInstalled = Directory.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Microsoft Visual Studio")) },
			new() { Text = "Visual Studio Code", ImageSource = "ms-appx:///Assets/Fluent/VisualStudioCode.png", IsInstalled = File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "Microsoft VS Code", "Code.exe")) },
			new() { Text = "Antigravity IDE", ImageSource = "ms-appx:///Assets/Fluent/Antigravity.png", IsInstalled = File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "Antigravity IDE", "Antigravity IDE.exe")) },
			new() { Text = "Cursor", ImageSource = "ms-appx:///Assets/Fluent/Cursor.png", IsInstalled = File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "cursor", "Cursor.exe")) },
			new() { Text = "Devin", ImageSource = "ms-appx:///Assets/Fluent/Devin.png", IsInstalled = File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "Devin", "Devin.exe"))},
			new() { Text = "IntelliJ IDEA", ImageSource = "ms-appx:///Assets/Fluent/IDEA.png", IsInstalled = File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "JetBrains", "IntelliJ IDEA 2026.1.3", "bin", "idea64.exe")) },
			new() { Text = "WinMerge", ImageSource = "ms-appx:///Assets/Fluent/WinMerge.png", IsInstalled = File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "WinMerge", "WinMergeU.exe"))},
			new() { Text = "Git", ImageSource = "ms-appx:///Assets/Fluent/Git.png", IsInstalled = File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Git", "bin", "git.exe")) },
			new() { Text = "CMake", ImageSource = "ms-appx:///Assets/Fluent/CMake.png", IsInstalled = File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "CMake", "bin", "cmake-gui.exe")) },
			new() { Text = "Python", ImageSource = "ms-appx:///Assets/Fluent/Python.png", IsInstalled = File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "py.exe")) },
			new() { Text = "Node.js", ImageSource = "ms-appx:///Assets/Fluent/Nodejs.png", IsInstalled = File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "nodejs", "node.exe")) },
			new() { Text = "Rust", ImageSource = "ms-appx:///Assets/Fluent/Rust.png", IsInstalled = File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".cargo", "bin", "rustup.exe")) },
			new() { Text = "Java", ImageSource = "ms-appx:///Assets/Fluent/Java.png", IsInstalled = File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Java", "jdk-26.0.1", "bin", "java.exe")) },
			new() { Text = "Go", ImageSource = "ms-appx:///Assets/Fluent/Go.png", IsInstalled = File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Go", "bin", "go.exe")) },
			new() { Text = "Trello", ImageSource = "ms-appx:///Assets/Fluent/Trello.png", IsInstalled = Directory.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Packages", "45273LiamForsyth.PawsforTrello_7pb5ddty8z1pa")) }
		};
		foreach (var item in devList.Where(item => !item.IsInstalled))
			developmentItems.Add(item);

		var sysinternalsList = new List<GridViewItem>
		{
			new() { Text = "Autoruns", ImageSource = "ms-appx:///Assets/Fluent/Autoruns.png", IsInstalled = File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Autoruns", "Autoruns64.exe")) },
			new() { Text = "Process Explorer", ImageSource = "ms-appx:///Assets/Fluent/ProcessExplorer.png", IsInstalled = File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Process Explorer", "procexp64.exe")) },
			new() { Text = "Process Monitor", ImageSource = "ms-appx:///Assets/Fluent/ProcessMonitor.png", IsInstalled = File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Process Monitor", "Procmon64.exe")) }
		};
		foreach (var item in sysinternalsList.Where(item => !item.IsInstalled))
			sysinternalsItems.Add(item);

		var overclockingList = new List<GridViewItem>
		{
			new() { Text = "HWiNFO® 64", ImageSource = "ms-appx:///Assets/Fluent/HWInfo.png", IsInstalled = File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "HWiNFO64", "HWiNFO64.exe")) },
			new() { Text = "ASRock Timing Configurator", ImageSource = "ms-appx:///Assets/Fluent/TimingConfigurator.png", IsInstalled = File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "ASRock Utility", "Timing Configurator", "AsrTC.exe")) },
			new() { Text = "ZenTimings", ImageSource = "ms-appx:///Assets/Fluent/ZenTimings.png", IsInstalled = File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "ZenTimings", "ZenTimings.exe")) },
			new() { Text = "TestMem5", ImageSource = "ms-appx:///Assets/Fluent/TestMem5.png", IsInstalled = File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "TestMem5", "TM5.exe")) },
			new() { Text = "Prime95", ImageSource = "ms-appx:///Assets/Fluent/Prime95.png", IsInstalled = File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Prime95", "prime95.exe")) },
			new() { Text = "OCCT", ImageSource = "ms-appx:///Assets/Fluent/OCCT.png", IsInstalled = File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "OCCT", "OCCT.exe")) }
		};
		foreach (var item in overclockingList.Where(item => !item.IsInstalled))
			overclockingItems.Add(item);

		var musicProductionList = new List<GridViewItem>
		{
			new() { Text = "Reaper", ImageSource = "ms-appx:///Assets/Fluent/Reaper.png", IsInstalled = File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "REAPER (x64)", "reaper.exe")) },
			new() { Text = "FL Studio", ImageSource = "ms-appx:///Assets/Fluent/FLStudio.png", IsInstalled = File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Image-Line", "FL Studio 2025", "FL64.exe")) },
			new() { Text = "Audacity", ImageSource = "ms-appx:///Assets/Fluent/Audacity.png", IsInstalled = File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Audacity", "Audacity.exe")) },
			new() { Text = "FlexASIO", ImageSource = "ms-appx:///Assets/Fluent/FlexASIO.png", IsInstalled = Directory.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "FlexASIO")) },
			new() { Text = "ASIO4ALL", ImageSource = "ms-appx:///Assets/Fluent/ASIO4ALL.png", IsInstalled = File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "ASIO4ALL v2", "a4apanel.exe")) },
			new() { Text = "Arturia MIDI Control Center", ImageSource = "ms-appx:///Assets/Fluent/ArturiaMidiControlCenter.png", IsInstalled = File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Arturia", "MIDI Control Center", "MIDI Control Center.exe")) }
		};
		foreach (var item in musicProductionList.Where(item => !item.IsInstalled))
			musicProductionItems.Add(item);

		var videoProductionList = new List<GridViewItem>
		{
			new() { Text = "DaVinci Resolve", ImageSource = "ms-appx:///Assets/Fluent/DavinciResolve.png", IsInstalled = File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Blackmagic Design", "DaVinci Resolve", "Resolve.exe")) },
			new() { Text = "Blender", ImageSource = "ms-appx:///Assets/Fluent/Blender.png", IsInstalled = File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Blender Foundation", "Blender 5.1", "blender.exe")) },
			new() { Text = "CapCut", ImageSource = "ms-appx:///Assets/Fluent/CapCut.png", IsInstalled = File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CapCut", "Apps", "CapCut.exe")) },
			new() { Text = "LosslessCut", ImageSource = "ms-appx:///Assets/Fluent/LosslessCut.png", IsInstalled = File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "LosslessCut", "LosslessCut.exe")) }
		};
		foreach (var item in videoProductionList.Where(item => !item.IsInstalled))
			videoProductionItems.Add(item);

		var multimediaList = new List<GridViewItem>
		{
			new() { Text = "MPC-QT", ImageSource = "ms-appx:///Assets/Fluent/MpcQt.png", IsInstalled = File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "MPC-QT", "mpc-qt.exe")) },
			new() { Text = "mpv", ImageSource = "ms-appx:///Assets/Fluent/MPV.png", IsInstalled = File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "mpv", "mpv.exe")) },
			new() { Text = "VLC", ImageSource = "ms-appx:///Assets/Fluent/VLC.png", IsInstalled = File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "VideoLAN", "VLC", "vlc.exe")) },
			new() { Text = "MediaInfo", ImageSource = "ms-appx:///Assets/Fluent/MediaInfo.png", IsInstalled = File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "MediaInfo", "MediaInfo.exe")) || Directory.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Packages", "MediaArea.net.MediaInfo_9bzbd7xajy7ar")) }
		};
		foreach (var item in multimediaList.Where(item => !item.IsInstalled))
			multimediaItems.Add(item);

		var officeList = new List<GridViewItem>
		{
			new() { Text = "Word", ImageSource = "ms-appx:///Assets/Fluent/Word.png", IsInstalled = File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Microsoft Office", "root", "Office16", "WINWORD.EXE")) },
			new() { Text = "Excel", ImageSource = "ms-appx:///Assets/Fluent/Excel.png", IsInstalled = File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Microsoft Office", "root", "Office16", "EXCEL.EXE")) },
			new() { Text = "PowerPoint", ImageSource = "ms-appx:///Assets/Fluent/Powerpoint.png", IsInstalled = File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Microsoft Office", "root", "Office16", "POWERPNT.EXE")) },
			new() { Text = "OneNote", ImageSource = "ms-appx:///Assets/Fluent/OneNote.png", IsInstalled = File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Microsoft Office", "root", "Office16", "ONENOTE.EXE")) },
			new() { Text = "Teams", ImageSource = "ms-appx:///Assets/Fluent/Teams.png", IsInstalled = Directory.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "Teams")) },
			new() { Text = "Outlook", ImageSource = "ms-appx:///Assets/Fluent/Outlook.png", IsInstalled = File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Microsoft Office", "root", "Office16", "OUTLOOK.EXE")) },
			new() { Text = "OneDrive", ImageSource = "ms-appx:///Assets/Fluent/OneDrive.png", IsInstalled = File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "OneDrive", "OneDrive.exe")) }
		};
		foreach (var item in officeList.Where(item => !item.IsInstalled))
			officeItems.Add(item);

		var miscellaneousList = new List<GridViewItem>
		{
			new() { Text = "CapFrameX", ImageSource = "ms-appx:///Assets/Fluent/CapFrameX.png", IsInstalled = File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "CapFrameX", "CapFrameX.exe")) },
			new() { Text = "Minitool Partition Wizard", ImageSource = "ms-appx:///Assets/Fluent/MinitoolPartitionWizard.png", IsInstalled = File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "MiniTool Partition Wizard 13", "partitionwizard.exe")) },
			new() { Text = "AOMEI Partition Assistant", ImageSource = "ms-appx:///Assets/Fluent/AomeiPartitionAssistant.png", IsInstalled = File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "AOMEI Partition Assistant", "PartAssist.exe")) },
			new() { Text = "WizTree", ImageSource = "ms-appx:///Assets/Fluent/WizTree.png", IsInstalled = File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "WizTree", "WizTree64.exe")) },
			new() { Text = "CrystalDiskMark", ImageSource = "ms-appx:///Assets/Fluent/CrystalDiskMark.png", IsInstalled = File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "CrystalDiskMark9", "DiskMark64.exe")) },
			new() { Text = "Bulk Crap Uninstaller", ImageSource = "ms-appx:///Assets/Fluent/BulkCrapUninstaller.png", IsInstalled = File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "BCUninstaller", "BCUninstaller.exe")) },
			new() { Text = "Bluetooth Audio Receiver", ImageSource = "ms-appx:///Assets/Fluent/BluetoothAudioReceiver.png", IsInstalled = Directory.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Packages", "55746MarkSmirnov.BluetoothAudioReveicer_xwrbx6997tsfc")) },
			new() { Text = "AnyDesk", ImageSource = "ms-appx:///Assets/Fluent/AnyDesk.png", IsInstalled = File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "AnyDesk", "AnyDesk.exe")) },
			new() { Text = "RustDesk", ImageSource = "ms-appx:///Assets/Fluent/RustDesk.png", IsInstalled = File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "RustDesk", "RustDesk.exe")) },
			new() { Text = "Apollo", ImageSource = "ms-appx:///Assets/Fluent/Apollo.png", IsInstalled = File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Apollo", "sunshine.exe")) },
			new() { Text = "AutoHotkey", ImageSource = "ms-appx:///Assets/Fluent/AutoHotkey.png", IsInstalled = File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "AutoHotkey", "UX", "AutoHotkeyUX.exe")) },
			new() { Text = "EmEditor", ImageSource = "ms-appx:///Assets/Fluent/EmEditor.png", IsInstalled = Directory.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Packages", "Emurasoft.EmEditor64UWP_ws7rg9hnwrpxm")) },
			new() { Text = "WinDbg", ImageSource = "ms-appx:///Assets/Fluent/WinDbg.png", IsInstalled = Directory.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Packages", "Microsoft.WinDbg_8wekyb3d8bbwe")) }
		};
		foreach (var item in miscellaneousList.Where(item => !item.IsInstalled))
			miscellaneousItems.Add(item);
	}

	private void GridView_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		InstallButton.IsEnabled = Office.SelectedItems.Count > 0 || Development.SelectedItems.Count > 0 || Sysinternals.SelectedItems.Count > 0 || Overclocking.SelectedItems.Count > 0 || Music.SelectedItems.Count > 0 || Peripherals.SelectedItems.Count > 0 || Controllers.SelectedItems.Count > 0 || Messaging.SelectedItems.Count > 0 || Launchers.SelectedItems.Count > 0 || Miscellaneous.SelectedItems.Count > 0 || VideoProduction.SelectedItems.Count > 0 || MusicProduction.SelectedItems.Count > 0 || Multimedia.SelectedItems.Count > 0;
	}

	private async void InstallButton_Click(object sender, RoutedEventArgs e)
	{
		var selection = new ApplicationSelection();

		var selectedMessagingItems = Messaging.SelectedItems.Cast<GridViewItem>().ToList();
		var selectedMessaging = selectedMessagingItems.Select(item => item.Text).ToList();
		selection.Discord = selectedMessaging.Contains("Discord");
		selection.WhatsApp = selectedMessaging.Contains("WhatsApp");
		selection.Telegram = selectedMessaging.Contains("Telegram Desktop");
		selection.Unigram = selectedMessaging.Contains("Unigram");
		selection.ZoomWorkplace = selectedMessaging.Contains("Zoom Workplace");
		selection.Thunderbird = selectedMessaging.Contains("Thunderbird");
		selection.Signal = selectedMessaging.Contains("Signal");

		var selectedLaunchersItems = Launchers.SelectedItems.Cast<GridViewItem>().ToList();
		var selectedLaunchers = selectedLaunchersItems.Select(item => item.Text).ToList();
		selection.EpicGames = selectedLaunchers.Contains("Epic Games");
		selection.Steam = selectedLaunchers.Contains("Steam");
		selection.RiotClient = selectedLaunchers.Contains("Riot Client");
		selection.UbisoftConnect = selectedLaunchers.Contains("Ubisoft Connect");
		selection.EA = selectedLaunchers.Contains("EA");
		selection.BattleNet = selectedLaunchers.Contains("Battle.Net");
		selection.MinecraftLauncher = selectedLaunchers.Contains("Minecraft Launcher");
		selection.CurseForge = selectedLaunchers.Contains("CurseForge");
		selection.PrismLauncher = selectedLaunchers.Contains("Prism Launcher");
		selection.LunarClient = selectedLaunchers.Contains("Lunar Client");
		selection.FeatherClient = selectedLaunchers.Contains("Feather Client");
		selection.Froststrap = selectedLaunchers.Contains("Froststrap");
		selection.RockstarGamesLauncher = selectedLaunchers.Contains("Rockstar Games Launcher");
		selection.FiveM = selectedLaunchers.Contains("FiveM");
		selection.FACEIT = selectedLaunchers.Contains("FACEIT");
		selection.Eden = selectedLaunchers.Contains("Eden");

		var selectedMusicItems = Music.SelectedItems.Cast<GridViewItem>().ToList();
		var selectedMusic = selectedMusicItems.Select(item => item.Text).ToList();
		selection.AppleMusic = selectedMusic.Contains("Apple Music");
		selection.Tidal = selectedMusic.Contains("TIDAL");
		selection.Qobuz = selectedMusic.Contains("Qobuz");
		selection.AmazonMusic = selectedMusic.Contains("Amazon Music");
		selection.DeezerMusic = selectedMusic.Contains("Deezer Music");
		selection.Spotify = selectedMusic.Contains("Spotify");
		selection.MusicBee = selectedMusic.Contains("MusicBee");

		var selectedPeripheralsItems = Peripherals.SelectedItems.Cast<GridViewItem>().ToList();
		var selectedPeripherals = selectedPeripheralsItems.Select(item => item.Text).ToList();
		selection.SteelSeriesGG = selectedPeripherals.Contains("SteelSeries GG");
		selection.RazerSynapse = selectedPeripherals.Contains("Razer Synapse");
		selection.LogitechGHub = selectedPeripherals.Contains("Logitech G HUB");
		selection.LogitechOnboardMemoryManager = selectedPeripherals.Contains("Logitech Onboard Memory Manager");
		selection.Wootility = selectedPeripherals.Contains("Wootility");
		selection.EndgameGear = selectedPeripherals.Contains("Endgame Gear");
		selection.GloriousCORE = selectedPeripherals.Contains("Glorious CORE");
		selection.MCHOSE = selectedPeripherals.Contains("MCHOSE HUB");
		selection.CorsairICue = selectedPeripherals.Contains("Corsair iCUE");
		selection.OpenRGB = selectedPeripherals.Contains("OpenRGB");
		selection.FanControl = selectedPeripherals.Contains("FanControl");
		selection.GHelper = selectedPeripherals.Contains("GHelper");

		var selectedControllersItems = Controllers.SelectedItems.Cast<GridViewItem>().ToList();
		var selectedControllers = selectedControllersItems.Select(item => item.Text).ToList();
		selection.ViGEmBus = selectedControllers.Contains("ViGEmBus");
		selection.HidHide = selectedControllers.Contains("HidHide");
		selection.DualSenseY = selectedControllers.Contains("DualSenseY");
		selection.RaceElement = selectedControllers.Contains("RaceElement");
		selection.PlaystationAccessories = selectedControllers.Contains("PlayStation® Accessories");
		selection.XboxAccessories = selectedControllers.Contains("Xbox Accessories");

		var selectedDevItems = Development.SelectedItems.Cast<GridViewItem>().ToList();
		var selectedDev = selectedDevItems.Select(item => item.Text).ToList();
		selection.VisualStudio = selectedDev.Contains("Visual Studio");
		selection.VisualStudioCode = selectedDev.Contains("Visual Studio Code");
		selection.Antigravity = selectedDev.Contains("Antigravity IDE");
		selection.Cursor = selectedDev.Contains("Cursor");
		selection.Devin = selectedDev.Contains("Devin");
		selection.IDEA = selectedDev.Contains("IntelliJ IDEA");
		selection.WinMerge = selectedDev.Contains("WinMerge");
		selection.Git = selectedDev.Contains("Git");
		selection.CMake = selectedDev.Contains("CMake");
		selection.Python = selectedDev.Contains("Python");
		selection.Nodejs = selectedDev.Contains("Node.js");
		selection.Rust = selectedDev.Contains("Rust");
		selection.Java = selectedDev.Contains("Java");
		selection.Go = selectedDev.Contains("Go");
		selection.Trello = selectedDev.Contains("Trello");

		var selectedSysinternalsItems = Sysinternals.SelectedItems.Cast<GridViewItem>().ToList();
		var selectedSysinternals = selectedSysinternalsItems.Select(item => item.Text).ToList();
		selection.Autoruns = selectedSysinternals.Contains("Autoruns");
		selection.ProcessExplorer = selectedSysinternals.Contains("Process Explorer");
		selection.ProcessMonitor = selectedSysinternals.Contains("Process Monitor");

		var selectedOverclockingItems = Overclocking.SelectedItems.Cast<GridViewItem>().ToList();
		var selectedOverclocking = selectedOverclockingItems.Select(item => item.Text).ToList();
		selection.HWInfo = selectedOverclocking.Contains("HWiNFO® 64");
		selection.TimingConfigurator = selectedOverclocking.Contains("ASRock Timing Configurator");
		selection.ZenTimings = selectedOverclocking.Contains("ZenTimings");
		selection.TestMem5 = selectedOverclocking.Contains("TestMem5");
		selection.Prime95 = selectedOverclocking.Contains("Prime95");
		selection.OCCT = selectedOverclocking.Contains("OCCT");

		var selectedMusicProductionItems = MusicProduction.SelectedItems.Cast<GridViewItem>().ToList();
		var selectedMusicProduction = selectedMusicProductionItems.Select(item => item.Text).ToList();
		selection.Reaper = selectedMusicProduction.Contains("Reaper");
		selection.FLStudio = selectedMusicProduction.Contains("FL Studio");
		selection.Audacity = selectedMusicProduction.Contains("Audacity");
		selection.FlexASIO = selectedMusicProduction.Contains("FlexASIO");
		selection.ASIO4ALL = selectedMusicProduction.Contains("ASIO4ALL");
		selection.ArturiaMidiControlCenter = selectedMusicProduction.Contains("Arturia MIDI Control Center");

		var selectedVideoProductionItems = VideoProduction.SelectedItems.Cast<GridViewItem>().ToList();
		var selectedVideoProduction = selectedVideoProductionItems.Select(item => item.Text).ToList();
		selection.DaVinciResolve = selectedVideoProduction.Contains("DaVinci Resolve");
		selection.Blender = selectedVideoProduction.Contains("Blender");
		selection.CapCut = selectedVideoProduction.Contains("CapCut");
		selection.LosslessCut = selectedVideoProduction.Contains("LosslessCut");

		var selectedMultimediaItems = Multimedia.SelectedItems.Cast<GridViewItem>().ToList();
		var selectedMultimedia = selectedMultimediaItems.Select(item => item.Text).ToList();
		selection.MediaInfo = selectedMultimedia.Contains("MediaInfo");
		selection.MpcQt = selectedMultimedia.Contains("MPC-QT");
		selection.MPV = selectedMultimedia.Contains("mpv");
		selection.VLC = selectedMultimedia.Contains("VLC");

		var selectedOfficeItems = Office.SelectedItems.Cast<GridViewItem>().ToList();
		var selectedOffice = selectedOfficeItems.Select(item => item.Text).ToList();
		selection.Word = selectedOffice.Contains("Word");
		selection.Excel = selectedOffice.Contains("Excel");
		selection.PowerPoint = selectedOffice.Contains("PowerPoint");
		selection.OneNote = selectedOffice.Contains("OneNote");
		selection.Teams = selectedOffice.Contains("Teams");
		selection.Outlook = selectedOffice.Contains("Outlook");
		selection.OneDrive = selectedOffice.Contains("OneDrive");

		var selectedMiscellaneousItems = Miscellaneous.SelectedItems.Cast<GridViewItem>().ToList();
		var selectedMiscellaneous = selectedMiscellaneousItems.Select(item => item.Text).ToList();
		selection.CapFrameX = selectedMiscellaneous.Contains("CapFrameX");
		selection.MinitoolPartitionWizard = selectedMiscellaneous.Contains("Minitool Partition Wizard");
		selection.AomeiPartitionAssistant = selectedMiscellaneous.Contains("AOMEI Partition Assistant");
		selection.WizTree = selectedMiscellaneous.Contains("WizTree");
		selection.CrystalDiskMark = selectedMiscellaneous.Contains("CrystalDiskMark");
		selection.BulkCrapUninstaller = selectedMiscellaneous.Contains("Bulk Crap Uninstaller");
		selection.BluetoothAudioReceiver = selectedMiscellaneous.Contains("Bluetooth Audio Receiver");
		selection.AnyDesk = selectedMiscellaneous.Contains("AnyDesk");
		selection.RustDesk = selectedMiscellaneous.Contains("RustDesk");
		selection.Apollo = selectedMiscellaneous.Contains("Apollo");
		selection.AutoHotkey = selectedMiscellaneous.Contains("AutoHotkey");
		selection.EmEditor = selectedMiscellaneous.Contains("EmEditor");
		selection.WinDbg = selectedMiscellaneous.Contains("WinDbg");

		var updateDialog = new UpdateDialog();
		var reporter = new UpdateDialogReporter(updateDialog);
		var actions = ApplicationStage.GetActions(reporter, selection);

		if (actions.Count > 0)
		{
			var updater = new ContentDialog
			{
				Title = "Installing Applications",
				Content = updateDialog,
				Resources = new ResourceDictionary
				{
					["ContentDialogMinHeight"] = 0.0,
					["ContentDialogMinWidth"] = 550,
					["ContentDialogMaxWidth"] = 1000
				},
				XamlRoot = XamlRoot
			};

			_ = updater.ShowAsync();
			await updateDialog.RunActions(actions);
			await Task.Delay(500);
			updateDialog.SetStatus("Installation complete.");
			updateDialog.SetSuccess();
			await Task.Delay(1000);
			updater.Hide();

			foreach (var item in selectedMessagingItems)
				messagingItems.Remove(item);

			foreach (var item in selectedLaunchersItems)
				launchersItems.Remove(item);

			foreach (var item in selectedMusicItems)
				musicItems.Remove(item);

			foreach (var item in selectedPeripheralsItems)
				peripheralsItems.Remove(item);

			foreach (var item in selectedControllersItems)
				controllersItems.Remove(item);

			foreach (var item in selectedDevItems)
				developmentItems.Remove(item);

			foreach (var item in selectedSysinternalsItems)
				sysinternalsItems.Remove(item);

			foreach (var item in selectedOverclockingItems)
				overclockingItems.Remove(item);

			foreach (var item in selectedMusicProductionItems)
				musicProductionItems.Remove(item);

			foreach (var item in selectedVideoProductionItems)
				videoProductionItems.Remove(item);

			foreach (var item in selectedMultimediaItems)
				multimediaItems.Remove(item);

			foreach (var item in selectedOfficeItems)
				officeItems.Remove(item);

			foreach (var item in selectedMiscellaneousItems)
				miscellaneousItems.Remove(item);

			GridView_SelectionChanged(null, null);
		}
	}
}

[GeneratedBindableCustomProperty]
public partial class GridViewItem
{
	public string Text { get; set; }
	public string ImageSource { get; set; }
	public bool IsInstalled { get; set; }
}
