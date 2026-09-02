using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Windows.Storage;
using WinRT;

namespace AutoOS.App.Views.Installer;

public sealed partial class AppsPage : Page
{
	private bool isInitializingMessagingState = true;
	private bool isInitializingLaunchersState = true;
	private bool isInitializingMusicState = true;
	private bool isInitializingPeripheralsState = true;
	private bool isInitializingControllersState = true;
	private bool isInitializingDevelopmentState = true;
	private bool isInitializingSysinternalsState = true;
	private bool isInitializingOverclockingState = true;
	private bool isInitializingMusicProductionState = true;
	private bool isInitializingVideoProductionState = true;
	private bool isInitializingMultimediaState = true;
	private bool isInitializingOfficeState = true;
	private bool isInitializingMiscellaneousState = true;

	private readonly ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

	public AppsPage()
	{
		InitializeComponent();
		GetItems();
		GetMessaging();
		GetLaunchers();
		GetMusic();
		GetPeripherals();
		GetControllers();
		GetDevelopment();
		GetSysinternals();
		GetOverclocking();
		GetMusicProduction();
		GetVideoProduction();
		GetMultimedia();
		GetOffice();
		GetMiscellaneous();
	}

	protected override void OnNavigatedTo(NavigationEventArgs e)
	{
		base.OnNavigatedTo(e);
		MainWindow.Instance.MarkVisited(nameof(AppsPage));
		MainWindow.Instance.CheckAllPagesVisited();
	}

	private void GetItems()
	{
		Messaging.ItemsSource = new List<GridViewItem>
		{
			new() { Text = "Discord", ImageSource = "ms-appx:///Assets/FluentIcons/Apps/Messaging/Discord.png" },
			new() { Text = "WhatsApp", ImageSource = "ms-appx:///Assets/FluentIcons/Apps/Messaging/Whatsapp.png" },
			new() { Text = "Telegram Desktop", ImageSource = "ms-appx:///Assets/FluentIcons/Apps/Messaging/Telegram.png" },
			new() { Text = "Unigram", ImageSource = "ms-appx:///Assets/FluentIcons/Apps/Messaging/Unigram.png" },
			new() { Text = "Zoom Workplace", ImageSource = "ms-appx:///Assets/FluentIcons/Apps/Messaging/Zoom.png" },
			new() { Text = "Thunderbird", ImageSource = "ms-appx:///Assets/FluentIcons/Apps/Messaging/Thunderbird.png" },
			new() { Text = "Signal", ImageSource = "ms-appx:///Assets/FluentIcons/Apps/Messaging/Signal.png" }
		};

		Launchers.ItemsSource = new List<GridViewItem>
		{
			new() { Text = "Epic Games", ImageSource = "ms-appx:///Assets/FluentIcons/Apps/Launchers/Epicgames.png" },
			new() { Text = "Steam", ImageSource = "ms-appx:///Assets/FluentIcons/Apps/Launchers/Steam.png" },
			new() { Text = "Riot Client", ImageSource = "ms-appx:///Assets/FluentIcons/Apps/Launchers/RiotClient.png" },
			new() { Text = "Ubisoft Connect", ImageSource = "ms-appx:///Assets/FluentIcons/Apps/Launchers/UbisoftConnect.png" },
			new() { Text = "EA", ImageSource = "ms-appx:///Assets/FluentIcons/Apps/Launchers/EA.png" },
			new() { Text = "Battle.Net", ImageSource = "ms-appx:///Assets/FluentIcons/Apps/Launchers/BattleNet.png" },
			new() { Text = "Minecraft Launcher", ImageSource = "ms-appx:///Assets/FluentIcons/Apps/Launchers/MinecraftLauncher.png" },
			new() { Text = "CurseForge", ImageSource = "ms-appx:///Assets/FluentIcons/Apps/Launchers/CurseForge.png" },
			new() { Text = "Lunar Client", ImageSource = "ms-appx:///Assets/FluentIcons/Apps/Launchers/LunarClient.png" },
			new() { Text = "Feather Client", ImageSource = "ms-appx:///Assets/FluentIcons/Apps/Launchers/FeatherClient.png" },
			new() { Text = "NoRisk Client", ImageSource = "ms-appx:///Assets/FluentIcons/Apps/Launchers/NoRiskClient.png" },
			new() { Text = "Prism Launcher", ImageSource = "ms-appx:///Assets/FluentIcons/Apps/Launchers/PrismLauncher.png" },
			new() { Text = "Bloxstrap", ImageSource = "ms-appx:///Assets/FluentIcons/Apps/Launchers/Bloxstrap.png" },
			new() { Text = "Froststrap", ImageSource = "ms-appx:///Assets/FluentIcons/Apps/Launchers/Froststrap.png" },
			new() { Text = "Rockstar Games Launcher", ImageSource = "ms-appx:///Assets/FluentIcons/Apps/Launchers/RockstarGamesLauncher.png" },
			new() { Text = "FiveM", ImageSource = "ms-appx:///Assets/FluentIcons/Apps/Launchers/FiveM.jpg" },
			new() { Text = "FACEIT", ImageSource = "ms-appx:///Assets/FluentIcons/Apps/Launchers/FACEIT.png" },
			new() { Text = "FACEIT AC", ImageSource = "ms-appx:///Assets/FluentIcons/Apps/Launchers/FACEITAC.png" },
			new() { Text = "Eden", ImageSource = "ms-appx:///Assets/FluentIcons/Pages/Settings/Eden.png" }
		};

		Music.ItemsSource = new List<GridViewItem>
		{
			new() { Text = "Apple Music", ImageSource = "ms-appx:///Assets/FluentIcons/Apps/Music/AppleMusic.png" },
			new() { Text = "TIDAL", ImageSource = "ms-appx:///Assets/FluentIcons/Apps/Music/Tidal.png" },
			new() { Text = "Qobuz", ImageSource = "ms-appx:///Assets/FluentIcons/Apps/Music/Qobuz.png" },
			new() { Text = "Amazon Music", ImageSource = "ms-appx:///Assets/FluentIcons/Apps/Music/AmazonMusic.png" },
			new() { Text = "Deezer Music", ImageSource = "ms-appx:///Assets/FluentIcons/Apps/Music/DeezerMusic.png" },
			new() { Text = "Spotify", ImageSource = "ms-appx:///Assets/FluentIcons/Apps/Music/Spotify.png" },
			new() { Text = "MusicBee", ImageSource = "ms-appx:///Assets/FluentIcons/Apps/Music/MusicBee.png" }
		};

		Peripherals.ItemsSource = new List<GridViewItem>
		{
			new() { Text = "Logitech G HUB", ImageSource = "ms-appx:///Assets/FluentIcons/Apps/Peripherals/Logitech.png" },
			new() { Text = "Logitech Onboard Memory Manager", ImageSource = "ms-appx:///Assets/FluentIcons/Apps/Peripherals/Logitech.png" },
			new() { Text = "Wootility", ImageSource = "ms-appx:///Assets/FluentIcons/Apps/Peripherals/Wootility.png" },
			new() { Text = "G-Menu", ImageSource = "ms-appx:///Assets/FluentIcons/Apps/Peripherals/G-Menu.png"},
			new() { Text = "Endgame Gear", ImageSource = "ms-appx:///Assets/FluentIcons/Apps/Peripherals/EndgameGear.png" },
			new() { Text = "Glorious CORE", ImageSource = "ms-appx:///Assets/FluentIcons/Apps/Peripherals/GloriousCORE.png" },
			new() { Text = "MCHOSE HUB", ImageSource = "ms-appx:///Assets/FluentIcons/Apps/Peripherals/MCHOSE.png" },
			new() { Text = "SteelSeries GG", ImageSource = "ms-appx:///Assets/FluentIcons/Apps/Peripherals/SteelSeriesGG.png" },
			new() { Text = "Razer Synapse", ImageSource = "ms-appx:///Assets/FluentIcons/Apps/Peripherals/RazerSynapse.png" },
			new() { Text = "Corsair iCUE", ImageSource = "ms-appx:///Assets/FluentIcons/Apps/Peripherals/CorsairICue.png" },
			new() { Text = "OpenRGB", ImageSource = "ms-appx:///Assets/FluentIcons/Apps/Peripherals/OpenRGB.png" },
			new() { Text = "FanControl", ImageSource = "ms-appx:///Assets/FluentIcons/Apps/Peripherals/FanControl.png" },
			new() { Text = "GHelper", ImageSource = "ms-appx:///Assets/FluentIcons/Apps/Peripherals/GHelper.png" }
		};

		Controllers.ItemsSource = new List<GridViewItem>
		{
			new() { Text = "ViGEmBus", ImageSource = "ms-appx:///Assets/FluentIcons/Apps/Controllers/ViGEmBus.png" },
			new() { Text = "HidHide", ImageSource = "ms-appx:///Assets/FluentIcons/Apps/Controllers/HidHide.png" },
			new() { Text = "DualSenseY", ImageSource = "ms-appx:///Assets/FluentIcons/Apps/Controllers/DualSenseY.png" },
			new() { Text = "RaceElement", ImageSource = "ms-appx:///Assets/FluentIcons/Apps/Controllers/RaceElement.png" },
			new() { Text = "PlayStation® Accessories", ImageSource = "ms-appx:///Assets/FluentIcons/Apps/Controllers/PlaystationAccessories.png" },
			new() { Text = "Xbox Accessories", ImageSource = "ms-appx:///Assets/FluentIcons/Apps/Controllers/XboxAccessories.png" }
		};

		Development.ItemsSource = new List<GridViewItem>
		{
			new() { Text = "Visual Studio", ImageSource = "ms-appx:///Assets/FluentIcons/Apps/Development/VisualStudio.png" },
			new() { Text = "Visual Studio Code", ImageSource = "ms-appx:///Assets/FluentIcons/Apps/Development/VisualStudioCode.png" },
			new() { Text = "Antigravity IDE", ImageSource = "ms-appx:///Assets/FluentIcons/Apps/Development/Antigravity.png" },
			new() { Text = "Cursor", ImageSource = "ms-appx:///Assets/FluentIcons/Apps/Development/Cursor.png" },
			new() { Text = "Devin", ImageSource = "ms-appx:///Assets/FluentIcons/Apps/Development/Devin.png" },
			new() { Text = "Kiro", ImageSource = "ms-appx:///Assets/FluentIcons/Apps/Development/Kiro.png" },
			new() { Text = "Freebuff", ImageSource = "ms-appx:///Assets/FluentIcons/Apps/Development/Freebuff.png" },
			new() { Text = "OpenCode", ImageSource = "ms-appx:///Assets/FluentIcons/Apps/Development/OpenCode.png" },
			new() { Text = "OpenChamber", ImageSource = "ms-appx:///Assets/FluentIcons/Apps/Development/OpenChamber.png" },
			new() { Text = "Sublime Text", ImageSource = "ms-appx:///Assets/FluentIcons/Apps/Development/SublimeText.png" },
			new() { Text = "IntelliJ IDEA", ImageSource = "ms-appx:///Assets/FluentIcons/Apps/Development/IDEA.png" },
			new() { Text = "WinMerge", ImageSource = "ms-appx:///Assets/FluentIcons/Apps/Development/WinMerge.png" },
			new() { Text = "Git", ImageSource = "ms-appx:///Assets/FluentIcons/Apps/Development/Git.png" },
			new() { Text = "CMake", ImageSource = "ms-appx:///Assets/FluentIcons/Apps/Development/CMake.png" },
			new() { Text = "Python", ImageSource = "ms-appx:///Assets/FluentIcons/Apps/Development/Python.png" },
			new() { Text = "Node.js", ImageSource = "ms-appx:///Assets/FluentIcons/Apps/Development/Nodejs.png" },
			new() { Text = "Rust", ImageSource = "ms-appx:///Assets/FluentIcons/Apps/Development/Rust.png" },
			new() { Text = "Java", ImageSource = "ms-appx:///Assets/FluentIcons/Apps/Development/Java.png" },
			new() { Text = "Go", ImageSource = "ms-appx:///Assets/FluentIcons/Apps/Development/Go.png" },
			new() { Text = "Trello", ImageSource = "ms-appx:///Assets/FluentIcons/Apps/Development/Trello.png" }
		};

		Sysinternals.ItemsSource = new List<GridViewItem>
		{
			new() { Text = "Autoruns", ImageSource = "ms-appx:///Assets/FluentIcons/Apps/Sysinternals/Autoruns.png" },
			new() { Text = "Process Explorer", ImageSource = "ms-appx:///Assets/FluentIcons/Apps/Sysinternals/ProcessExplorer.png" },
			new() { Text = "Process Monitor", ImageSource = "ms-appx:///Assets/FluentIcons/Apps/Sysinternals/ProcessMonitor.png" }
		};

		Overclocking.ItemsSource = new List<GridViewItem>
		{
			new() { Text = "HWiNFO® 64", ImageSource = "ms-appx:///Assets/FluentIcons/Apps/Overclocking/HWInfo.png" },
			new() { Text = "ASRock Timing Configurator", ImageSource = "ms-appx:///Assets/FluentIcons/Apps/Overclocking/TimingConfigurator.png" },
			new() { Text = "ZenTimings", ImageSource = "ms-appx:///Assets/FluentIcons/Apps/Overclocking/ZenTimings.png" },
			new() { Text = "RAM Test Pro", ImageSource = "ms-appx:///Assets/FluentIcons/Apps/Overclocking/RamTestPro.png" },
			new() { Text = "TestMem5", ImageSource = "ms-appx:///Assets/FluentIcons/Apps/Overclocking/TestMem5.png" },
			new() { Text = "Prime95", ImageSource = "ms-appx:///Assets/FluentIcons/Apps/Overclocking/Prime95.png" },
			new() { Text = "y-cruncher", ImageSource = "ms-appx:///Assets/FluentIcons/Apps/Overclocking/Default.png" },
			new() { Text = "OCCT", ImageSource = "ms-appx:///Assets/FluentIcons/Apps/Overclocking/OCCT.png" },
			new() { Text = "AIDA64 Extreme", ImageSource = "ms-appx:///Assets/FluentIcons/Apps/Overclocking/Aida64Extreme.png" },
			new() { Text = "Memtest Vulkan", ImageSource = "ms-appx:///Assets/FluentIcons/Apps/Overclocking/Default.png" }

		};

		MusicProduction.ItemsSource = new List<GridViewItem>
		{
			new() { Text = "Reaper", ImageSource = "ms-appx:///Assets/FluentIcons/Apps/Music Production/Reaper.png" },
			new() { Text = "FL Studio", ImageSource = "ms-appx:///Assets/FluentIcons/Apps/Music Production/FLStudio.png" },
			new() { Text = "Audacity", ImageSource = "ms-appx:///Assets/FluentIcons/Apps/Music Production/Audacity.png" },
			new() { Text = "FlexASIO", ImageSource = "ms-appx:///Assets/FluentIcons/Apps/Music Production/FlexASIO.png" },
			new() { Text = "ASIO4ALL", ImageSource = "ms-appx:///Assets/FluentIcons/Apps/Music Production/ASIO4ALL.png" },
			new() { Text = "Arturia MIDI Control Center", ImageSource = "ms-appx:///Assets/FluentIcons/Apps/Music Production/ArturiaMidiControlCenter.png" },
			new() { Text = "Voicemeeter", ImageSource = "ms-appx:///Assets/FluentIcons/Apps/Music Production/Voicemeeter.png" }
		};

		VideoProduction.ItemsSource = new List<GridViewItem>
		{
			new() { Text = "DaVinci Resolve", ImageSource = "ms-appx:///Assets/FluentIcons/Apps/Video Production/DavinciResolve.png" },
			new() { Text = "Blender", ImageSource = "ms-appx:///Assets/FluentIcons/Apps/Video Production/Blender.png" },
			new() { Text = "CapCut", ImageSource = "ms-appx:///Assets/FluentIcons/Apps/Video Production/CapCut.png" },
			new() { Text = "LosslessCut", ImageSource = "ms-appx:///Assets/FluentIcons/Apps/Video Production/LosslessCut.png" }
		};

		Multimedia.ItemsSource = new List<GridViewItem>
		{
			new() { Text = "Netflix", ImageSource = "ms-appx:///Assets/FluentIcons/Apps/Multimedia/Netflix.png" },
			new() { Text = "Disney+", ImageSource = "ms-appx:///Assets/FluentIcons/Apps/Multimedia/Disney+.png" },
			new() { Text = "Prime Video", ImageSource = "ms-appx:///Assets/FluentIcons/Apps/Multimedia/PrimeVideo.png" },
			new() { Text = "MPC-QT", ImageSource = "ms-appx:///Assets/FluentIcons/Apps/Multimedia/MpcQt.png" },
			new() { Text = "mpv", ImageSource = "ms-appx:///Assets/FluentIcons/Apps/Multimedia/MPV.png" },
			new() { Text = "VLC", ImageSource = "ms-appx:///Assets/FluentIcons/Apps/Multimedia/VLC.png" },
			new() { Text = "MediaInfo", ImageSource = "ms-appx:///Assets/FluentIcons/Apps/Multimedia/MediaInfo.png" }
		};

		Office.ItemsSource = new List<GridViewItem>
		{
			new() { Text = "Word", ImageSource = "ms-appx:///Assets/FluentIcons/Apps/Office/Word.png" },
			new() { Text = "Excel", ImageSource = "ms-appx:///Assets/FluentIcons/Apps/Office/Excel.png" },
			new() { Text = "PowerPoint", ImageSource = "ms-appx:///Assets/FluentIcons/Apps/Office/Powerpoint.png" },
			new() { Text = "OneNote", ImageSource = "ms-appx:///Assets/FluentIcons/Apps/Office/OneNote.png" },
			new() { Text = "Teams", ImageSource = "ms-appx:///Assets/FluentIcons/Apps/Office/Teams.png" },
			new() { Text = "Outlook", ImageSource = "ms-appx:///Assets/FluentIcons/Apps/Office/Outlook.png" },
			new() { Text = "OneDrive", ImageSource = "ms-appx:///Assets/FluentIcons/Apps/Office/OneDrive.png" }
		};

		Miscellaneous.ItemsSource = new List<GridViewItem>
		{
			new() { Text = "CapFrameX", ImageSource = "ms-appx:///Assets/FluentIcons/Apps/Miscellaneous/CapFrameX.png" },
			new() { Text = "Minitool Partition Wizard", ImageSource = "ms-appx:///Assets/FluentIcons/Apps/Miscellaneous/MinitoolPartitionWizard.png" },
			new() { Text = "AOMEI Partition Assistant", ImageSource = "ms-appx:///Assets/FluentIcons/Apps/Miscellaneous/AomeiPartitionAssistant.png" },
			new() { Text = "WizTree", ImageSource = "ms-appx:///Assets/FluentIcons/Apps/Miscellaneous/WizTree.png" },
			new() { Text = "CrystalDiskInfo", ImageSource = "ms-appx:///Assets/FluentIcons/Apps/Miscellaneous/CrystalDiskInfo.png" },
			new() { Text = "CrystalDiskMark", ImageSource = "ms-appx:///Assets/FluentIcons/Apps/Miscellaneous/CrystalDiskMark.png" },
			new() { Text = "Bulk Crap Uninstaller", ImageSource = "ms-appx:///Assets/FluentIcons/Apps/Miscellaneous/BulkCrapUninstaller.png" },
			new() { Text = "Bluetooth Audio Receiver", ImageSource = "ms-appx:///Assets/FluentIcons/Apps/Miscellaneous/BluetoothAudioReceiver.png" },
			new() { Text = "AnyDesk", ImageSource = "ms-appx:///Assets/FluentIcons/Apps/Miscellaneous/AnyDesk.png" },
			new() { Text = "RustDesk", ImageSource = "ms-appx:///Assets/FluentIcons/Apps/Miscellaneous/RustDesk.png" },
			new() { Text = "Apollo", ImageSource = "ms-appx:///Assets/FluentIcons/Apps/Miscellaneous/Apollo.png" },
			new() { Text = "Moonlight", ImageSource = "ms-appx:///Assets/FluentIcons/Apps/Miscellaneous/Moonlight.png" },
			new() { Text = "AutoHotkey", ImageSource = "ms-appx:///Assets/FluentIcons/Apps/Miscellaneous/AutoHotkey.png" },
			new() { Text = "EmEditor", ImageSource = "ms-appx:///Assets/FluentIcons/Apps/Miscellaneous/EmEditor.png" },
			new() { Text = "WinDbg", ImageSource = "ms-appx:///Assets/FluentIcons/Apps/Miscellaneous/WinDbg.png" },
			new() { Text = "qBittorrent", ImageSource = "ms-appx:///Assets/FluentIcons/Apps/Miscellaneous/QBittorrent.png" },
			new() { Text = "Deluge", ImageSource = "ms-appx:///Assets/FluentIcons/Apps/Miscellaneous/Deluge.png" },
			new() { Text = "Free Download Manager", ImageSource = "ms-appx:///Assets/FluentIcons/Apps/Miscellaneous/FreeDownloadManager.png" }
		};
	}

	private void GetMessaging()
	{
		string? selectedMessaging = localSettings.Values["Messaging"] as string;
		var messagingItems = Messaging.ItemsSource as List<GridViewItem>;
		Messaging.SelectedItems.AddRange(
			selectedMessaging?.Split([", "], StringSplitOptions.RemoveEmptyEntries)
			.Select(e => messagingItems?.FirstOrDefault(ext => ext.Text == e))
			.Where(ext => ext != null) ?? []
		);

		isInitializingMessagingState = false;
	}

	private void GetLaunchers()
	{
		string? selectedLaunchers = localSettings.Values["Launchers"] as string;
		var launcherItems = Launchers.ItemsSource as List<GridViewItem>;
		Launchers.SelectedItems.AddRange(
			selectedLaunchers?.Split([", "], StringSplitOptions.RemoveEmptyEntries)
			.Select(e => launcherItems?.FirstOrDefault(ext => ext.Text == e))
			.Where(ext => ext != null) ?? []
		);

		isInitializingLaunchersState = false;
	}

	private void GetMusic()
	{
		string? selectedMusic = localSettings.Values["Music"] as string;
		var musicItems = Music.ItemsSource as List<GridViewItem>;
		Music.SelectedItems.AddRange(
			selectedMusic?.Split([", "], StringSplitOptions.RemoveEmptyEntries)
			.Select(e => musicItems?.FirstOrDefault(ext => ext.Text == e))
			.Where(ext => ext != null) ?? []
		);

		isInitializingMusicState = false;
	}

	private void GetPeripherals()
	{
		string? selectedPeripherals = localSettings.Values["Peripherals"] as string;
		var peripheralItems = Peripherals.ItemsSource as List<GridViewItem>;
		Peripherals.SelectedItems.AddRange(
			selectedPeripherals?.Split([", "], StringSplitOptions.RemoveEmptyEntries)
			.Select(e => peripheralItems?.FirstOrDefault(ext => ext.Text == e))
			.Where(ext => ext != null) ?? []
		);

		isInitializingPeripheralsState = false;
	}

	private void GetControllers()
	{
		string? selectedControllers = localSettings.Values["Controllers"] as string;
		var controllersItems = Controllers.ItemsSource as List<GridViewItem>;
		Controllers.SelectedItems.AddRange(
			selectedControllers?.Split([", "], StringSplitOptions.RemoveEmptyEntries)
			.Select(e => controllersItems?.FirstOrDefault(ext => ext.Text == e))
			.Where(ext => ext != null) ?? []
		);

		isInitializingControllersState = false;
	}

	private void GetDevelopment()
	{
		string? selectedDevelopment = localSettings.Values["Development"] as string;
		var developmentItems = Development.ItemsSource as List<GridViewItem>;
		Development.SelectedItems.AddRange(
			selectedDevelopment?.Split([", "], StringSplitOptions.RemoveEmptyEntries)
			.Select(e => developmentItems?.FirstOrDefault(ext => ext.Text == e))
			.Where(ext => ext != null) ?? []
		);

		isInitializingDevelopmentState = false;
	}

	private void GetSysinternals()
	{
		string? selectedSysinternals = localSettings.Values["Sysinternals"] as string;
		var sysinternalsItems = Sysinternals.ItemsSource as List<GridViewItem>;
		Sysinternals.SelectedItems.AddRange(
			selectedSysinternals?.Split([", "], StringSplitOptions.RemoveEmptyEntries)
			.Select(e => sysinternalsItems?.FirstOrDefault(ext => ext.Text == e))
			.Where(ext => ext != null) ?? []
		);

		isInitializingSysinternalsState = false;
	}

	private void GetOverclocking()
	{
		string? selectedOverclocking = localSettings.Values["Overclocking"] as string;
		var overclockingItems = Overclocking.ItemsSource as List<GridViewItem>;
		Overclocking.SelectedItems.AddRange(
			selectedOverclocking?.Split([", "], StringSplitOptions.RemoveEmptyEntries)
			.Select(e => overclockingItems?.FirstOrDefault(ext => ext.Text == e))
			.Where(ext => ext != null) ?? []
		);

		isInitializingOverclockingState = false;
	}

	private void GetMusicProduction()
	{
		string? selectedMusicProduction = localSettings.Values["MusicProduction"] as string;
		var musicProductionItems = MusicProduction.ItemsSource as List<GridViewItem>;
		MusicProduction.SelectedItems.AddRange(
			selectedMusicProduction?.Split([", "], StringSplitOptions.RemoveEmptyEntries)
			.Select(e => musicProductionItems?.FirstOrDefault(ext => ext.Text == e))
			.Where(ext => ext != null) ?? []
		);

		isInitializingMusicProductionState = false;
	}

	private void GetVideoProduction()
	{
		string? selectedVideoProduction = localSettings.Values["VideoProduction"] as string;
		var videoProductionItems = VideoProduction.ItemsSource as List<GridViewItem>;
		VideoProduction.SelectedItems.AddRange(
			selectedVideoProduction?.Split([", "], StringSplitOptions.RemoveEmptyEntries)
			.Select(e => videoProductionItems?.FirstOrDefault(ext => ext.Text == e))
			.Where(ext => ext != null) ?? []
		);

		isInitializingVideoProductionState = false;
	}

	private void GetMultimedia()
	{
		string? selectedMultimedia = localSettings.Values["Multimedia"] as string;
		var multimediaItems = Multimedia.ItemsSource as List<GridViewItem>;
		Multimedia.SelectedItems.AddRange(
			selectedMultimedia?.Split([", "], StringSplitOptions.RemoveEmptyEntries)
			.Select(e => multimediaItems?.FirstOrDefault(ext => ext.Text == e))
			.Where(ext => ext != null) ?? []
		);

		isInitializingMultimediaState = false;
	}

	private void GetOffice()
	{
		string? selectedOffice = localSettings.Values["Office"] as string;
		var oficeItems = Office.ItemsSource as List<GridViewItem>;
		Office.SelectedItems.AddRange(
			selectedOffice?.Split([", "], StringSplitOptions.RemoveEmptyEntries)
			.Select(e => oficeItems?.FirstOrDefault(ext => ext.Text == e))
			.Where(ext => ext != null) ?? []
		);

		isInitializingOfficeState = false;
	}

	private void GetMiscellaneous()
	{
		string? selectedMiscellaneous = localSettings.Values["Miscellaneous"] as string;
		var miscellaneousItems = Miscellaneous.ItemsSource as List<GridViewItem>;
		Miscellaneous.SelectedItems.AddRange(
			selectedMiscellaneous?.Split([", "], StringSplitOptions.RemoveEmptyEntries)
			.Select(e => miscellaneousItems?.FirstOrDefault(ext => ext.Text == e))
			.Where(ext => ext != null) ?? []
		);

		isInitializingMiscellaneousState = false;
	}

	private void Messaging_Changed(object sender, SelectionChangedEventArgs e)
	{
		if (isInitializingMessagingState)
			return;

		string[] selectedMessaging = Messaging.SelectedItems
			.Cast<GridViewItem>()
			.Select(item => item.Text)
			.ToArray();

		localSettings.Values["Messaging"] = string.Join(", ", selectedMessaging);
	}

	private void Launchers_Changed(object sender, SelectionChangedEventArgs e)
	{
		if (isInitializingLaunchersState)
			return;

		string[] selectedLaunchers = Launchers.SelectedItems
			.Cast<GridViewItem>()
			.Select(item => item.Text)
			.ToArray();

		localSettings.Values["Launchers"] = string.Join(", ", selectedLaunchers);
	}

	private void Music_Changed(object sender, SelectionChangedEventArgs e)
	{
		if (isInitializingMusicState)
			return;

		string[] selectedMusic = Music.SelectedItems
			.Cast<GridViewItem>()
			.Select(item => item.Text)
			.ToArray();

		localSettings.Values["Music"] = string.Join(", ", selectedMusic);
	}

	private void Peripherals_Changed(object sender, SelectionChangedEventArgs e)
	{
		if (isInitializingPeripheralsState)
			return;

		string[] selectedPeripherals = Peripherals.SelectedItems
			.Cast<GridViewItem>()
			.Select(item => item.Text)
			.ToArray();

		localSettings.Values["Peripherals"] = string.Join(", ", selectedPeripherals);
	}

	private void Controllers_Changed(object sender, SelectionChangedEventArgs e)
	{
		if (isInitializingControllersState)
			return;

		string[] selectedControllers = Controllers.SelectedItems
			.Cast<GridViewItem>()
			.Select(item => item.Text)
			.ToArray();

		localSettings.Values["Controllers"] = string.Join(", ", selectedControllers);
	}

	private void Development_Changed(object sender, SelectionChangedEventArgs e)
	{
		if (isInitializingDevelopmentState)
			return;

		string[] selectedDevelopment = Development.SelectedItems
			.Cast<GridViewItem>()
			.Select(item => item.Text)
			.ToArray();

		localSettings.Values["Development"] = string.Join(", ", selectedDevelopment);
	}

	private void Sysinternals_Changed(object sender, SelectionChangedEventArgs e)
	{
		if (isInitializingSysinternalsState)
			return;

		string[] selectedSysinternals = Sysinternals.SelectedItems
			.Cast<GridViewItem>()
			.Select(item => item.Text)
			.ToArray();

		localSettings.Values["Sysinternals"] = string.Join(", ", selectedSysinternals);
	}

	private void Overclocking_Changed(object sender, SelectionChangedEventArgs e)
	{
		if (isInitializingOverclockingState)
			return;

		string[] selectedOverclocking = Overclocking.SelectedItems
			.Cast<GridViewItem>()
			.Select(item => item.Text)
			.ToArray();

		localSettings.Values["Overclocking"] = string.Join(", ", selectedOverclocking);
	}

	private void MusicProduction_Changed(object sender, SelectionChangedEventArgs e)
	{
		if (isInitializingMusicProductionState)
			return;

		string[] selectedMusicProduction = MusicProduction.SelectedItems
			.Cast<GridViewItem>()
			.Select(item => item.Text)
			.ToArray();

		localSettings.Values["MusicProduction"] = string.Join(", ", selectedMusicProduction);
	}

	private void VideoProduction_Changed(object sender, SelectionChangedEventArgs e)
	{
		if (isInitializingVideoProductionState)
			return;

		string[] selectedVideoProduction = VideoProduction.SelectedItems
			.Cast<GridViewItem>()
			.Select(item => item.Text)
			.ToArray();

		localSettings.Values["VideoProduction"] = string.Join(", ", selectedVideoProduction);
	}

	private void Multimedia_Changed(object sender, SelectionChangedEventArgs e)
	{
		if (isInitializingMultimediaState)
			return;

		string[] selectedMultimedia = Multimedia.SelectedItems
			.Cast<GridViewItem>()
			.Select(item => item.Text)
			.ToArray();

		localSettings.Values["Multimedia"] = string.Join(", ", selectedMultimedia);
	}

	private void Office_Changed(object sender, SelectionChangedEventArgs e)
	{
		if (isInitializingOfficeState)
			return;

		string[] selectedOffice = Office.SelectedItems
			.Cast<GridViewItem>()
			.Select(item => item.Text)
			.ToArray();

		localSettings.Values["Office"] = string.Join(", ", selectedOffice);
	}

	private void Miscellaneous_Changed(object sender, SelectionChangedEventArgs e)
	{
		if (isInitializingMiscellaneousState)
			return;

		string[] selectedMiscellaneous = Miscellaneous.SelectedItems
			.Cast<GridViewItem>()
			.Select(item => item.Text)
			.ToArray();

		localSettings.Values["Miscellaneous"] = string.Join(", ", selectedMiscellaneous);
	}
}

[GeneratedBindableCustomProperty]
public partial class GridViewItem
{
	public string Text { get; set; } = string.Empty;
	public string ImageSource { get; set; } = string.Empty;
}
