using Windows.Storage;
using WinRT;

namespace AutoOS.Views.Installer;

public sealed partial class ApplicationsPage : Page
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

	public ApplicationsPage()
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
		MainWindow.Instance.MarkVisited(nameof(ApplicationsPage));
		MainWindow.Instance.CheckAllPagesVisited();
	}

	private void GetItems()
	{
		Messaging.ItemsSource = new List<GridViewItem>
		{
			new() { Text = "Discord", ImageSource = "ms-appx:///Assets/Fluent/Discord.png" },
			new() { Text = "WhatsApp", ImageSource = "ms-appx:///Assets/Fluent/Whatsapp.png" },
			new() { Text = "Telegram Desktop", ImageSource = "ms-appx:///Assets/Fluent/Telegram.png" },
			new() { Text = "Unigram", ImageSource = "ms-appx:///Assets/Fluent/Unigram.png" },
			new() { Text = "Zoom Workplace", ImageSource = "ms-appx:///Assets/Fluent/Zoom.png" },
			new() { Text = "Thunderbird", ImageSource = "ms-appx:///Assets/Fluent/Thunderbird.png" },
			new() { Text = "Signal", ImageSource = "ms-appx:///Assets/Fluent/Signal.png" }
		};

		Launchers.ItemsSource = new List<GridViewItem>
		{
			new() { Text = "Epic Games", ImageSource = "ms-appx:///Assets/Fluent/EpicGames.png" },
			new() { Text = "Steam", ImageSource = "ms-appx:///Assets/Fluent/Steam.png" },
			new() { Text = "Riot Client", ImageSource = "ms-appx:///Assets/Fluent/RiotClient.png" },
			new() { Text = "Ubisoft Connect", ImageSource = "ms-appx:///Assets/Fluent/UbisoftConnect.png" },
			new() { Text = "EA", ImageSource = "ms-appx:///Assets/Fluent/EA.png" },
			new() { Text = "Battle.Net", ImageSource = "ms-appx:///Assets/Fluent/BattleNet.png" },
			new() { Text = "Minecraft Launcher", ImageSource = "ms-appx:///Assets/Fluent/MinecraftLauncher.png" },
			new() { Text = "CurseForge", ImageSource = "ms-appx:///Assets/Fluent/CurseForge.png" },
			new() { Text = "Prism Launcher", ImageSource = "ms-appx:///Assets/Fluent/PrismLauncher.png" },
			new() { Text = "Lunar Client", ImageSource = "ms-appx:///Assets/Fluent/LunarClient.png" },
			new() { Text = "Feather Client", ImageSource = "ms-appx:///Assets/Fluent/FeatherClient.png" },
			new() { Text = "Froststrap", ImageSource = "ms-appx:///Assets/Fluent/Froststrap.png" },
			new() { Text = "Rockstar Games Launcher", ImageSource = "ms-appx:///Assets/Fluent/RockstarGamesLauncher.png" },
			new() { Text = "FiveM", ImageSource = "ms-appx:///Assets/Fluent/FiveM.jpg" },
			new() { Text = "FACEIT", ImageSource = "ms-appx:///Assets/Fluent/FACEIT.png" },
			new() { Text = "Eden", ImageSource = "ms-appx:///Assets/Fluent/Eden.png" }
		};

		Music.ItemsSource = new List<GridViewItem>
		{
			new() { Text = "Apple Music", ImageSource = "ms-appx:///Assets/Fluent/AppleMusic.png" },
			new() { Text = "TIDAL", ImageSource = "ms-appx:///Assets/Fluent/Tidal.png" },
			new() { Text = "Qobuz", ImageSource = "ms-appx:///Assets/Fluent/Qobuz.png" },
			new() { Text = "Amazon Music", ImageSource = "ms-appx:///Assets/Fluent/AmazonMusic.png" },
			new() { Text = "Deezer Music", ImageSource = "ms-appx:///Assets/Fluent/DeezerMusic.png" },
			new() { Text = "Spotify", ImageSource = "ms-appx:///Assets/Fluent/Spotify.png" },
			new() { Text = "MusicBee", ImageSource = "ms-appx:///Assets/Fluent/MusicBee.png" }
		};

		Peripherals.ItemsSource = new List<GridViewItem>
		{
			new() { Text = "Logitech G HUB", ImageSource = "ms-appx:///Assets/Fluent/Logitech.png" },
			new() { Text = "Logitech Onboard Memory Manager", ImageSource = "ms-appx:///Assets/Fluent/Logitech.png" },
			new() { Text = "Wootility", ImageSource = "ms-appx:///Assets/Fluent/Wootility.png" },
			new() { Text = "Endgame Gear", ImageSource = "ms-appx:///Assets/Fluent/EndgameGear.png" },
			new() { Text = "Glorious CORE", ImageSource = "ms-appx:///Assets/Fluent/GloriousCORE.png" },
			new() { Text = "MCHOSE HUB", ImageSource = "ms-appx:///Assets/Fluent/MCHOSE.png" },
			new() { Text = "SteelSeries GG", ImageSource = "ms-appx:///Assets/Fluent/SteelSeriesGG.png" },
			new() { Text = "Razer Synapse", ImageSource = "ms-appx:///Assets/Fluent/RazerSynapse.png" },
			new() { Text = "Corsair iCUE", ImageSource = "ms-appx:///Assets/Fluent/CorsairICue.png" },
			new() { Text = "OpenRGB", ImageSource = "ms-appx:///Assets/Fluent/OpenRGB.png" },
			new() { Text = "FanControl", ImageSource = "ms-appx:///Assets/Fluent/FanControl.png" },
			new() { Text = "GHelper", ImageSource = "ms-appx:///Assets/Fluent/GHelper.png" }
		};

		Controllers.ItemsSource = new List<GridViewItem>
		{
			new() { Text = "ViGEmBus", ImageSource = "ms-appx:///Assets/Fluent/ViGEmBus.png" },
			new() { Text = "HidHide", ImageSource = "ms-appx:///Assets/Fluent/HidHide.png" },
			new() { Text = "DualSenseY", ImageSource = "ms-appx:///Assets/Fluent/DualSenseY.png" },
			new() { Text = "RaceElement", ImageSource = "ms-appx:///Assets/Fluent/RaceElement.png" },
			new() { Text = "PlayStation® Accessories", ImageSource = "ms-appx:///Assets/Fluent/PlaystationAccessories.png" },
			new() { Text = "Xbox Accessories", ImageSource = "ms-appx:///Assets/Fluent/XboxAccessories.png" }
		};

		Development.ItemsSource = new List<GridViewItem>
		{
			new() { Text = "Visual Studio", ImageSource = "ms-appx:///Assets/Fluent/VisualStudio.png" },
			new() { Text = "Visual Studio Code", ImageSource = "ms-appx:///Assets/Fluent/VisualStudioCode.png" },
			new() { Text = "Antigravity IDE", ImageSource = "ms-appx:///Assets/Fluent/Antigravity.png" },
			new() { Text = "Cursor", ImageSource = "ms-appx:///Assets/Fluent/Cursor.png" },
			new() { Text = "Devin", ImageSource = "ms-appx:///Assets/Fluent/Devin.png" },
			new() { Text = "IntelliJ IDEA", ImageSource = "ms-appx:///Assets/Fluent/IDEA.png" },
			new() { Text = "WinMerge", ImageSource = "ms-appx:///Assets/Fluent/WinMerge.png" },
			new() { Text = "Git", ImageSource = "ms-appx:///Assets/Fluent/Git.png" },
			new() { Text = "CMake", ImageSource = "ms-appx:///Assets/Fluent/CMake.png" },
			new() { Text = "Python", ImageSource = "ms-appx:///Assets/Fluent/Python.png" },
			new() { Text = "Node.js", ImageSource = "ms-appx:///Assets/Fluent/Nodejs.png" },
			new() { Text = "Rust", ImageSource = "ms-appx:///Assets/Fluent/Rust.png" },
			new() { Text = "Java", ImageSource = "ms-appx:///Assets/Fluent/Java.png" },
			new() { Text = "Go", ImageSource = "ms-appx:///Assets/Fluent/Go.png" },
			new() { Text = "Trello", ImageSource = "ms-appx:///Assets/Fluent/Trello.png" }
		};

		Sysinternals.ItemsSource = new List<GridViewItem>
		{
			new() { Text = "Autoruns", ImageSource = "ms-appx:///Assets/Fluent/Autoruns.png" },
			new() { Text = "Process Explorer", ImageSource = "ms-appx:///Assets/Fluent/ProcessExplorer.png" },
			new() { Text = "Process Monitor", ImageSource = "ms-appx:///Assets/Fluent/ProcessMonitor.png" }
		};

		Overclocking.ItemsSource = new List<GridViewItem>
		{
			new() { Text = "HWiNFO® 64", ImageSource = "ms-appx:///Assets/Fluent/HWInfo.png" },
			new() { Text = "ASRock Timing Configurator", ImageSource = "ms-appx:///Assets/Fluent/TimingConfigurator.png" },
			new() { Text = "ZenTimings", ImageSource = "ms-appx:///Assets/Fluent/ZenTimings.png" },
			new() { Text = "TestMem5", ImageSource = "ms-appx:///Assets/Fluent/TestMem5.png" },
			new() { Text = "Prime95", ImageSource = "ms-appx:///Assets/Fluent/Prime95.png" },
			new() { Text = "OCCT", ImageSource = "ms-appx:///Assets/Fluent/OCCT.png" }
		};

		MusicProduction.ItemsSource = new List<GridViewItem>
		{
			new() { Text = "Reaper", ImageSource = "ms-appx:///Assets/Fluent/Reaper.png" },
			new() { Text = "FL Studio", ImageSource = "ms-appx:///Assets/Fluent/FLStudio.png" },
			new() { Text = "Audacity", ImageSource = "ms-appx:///Assets/Fluent/Audacity.png" },
			new() { Text = "FlexASIO", ImageSource = "ms-appx:///Assets/Fluent/FlexASIO.png" },
			new() { Text = "ASIO4ALL", ImageSource = "ms-appx:///Assets/Fluent/ASIO4ALL.png" },
			new() { Text = "Arturia MIDI Control Center", ImageSource = "ms-appx:///Assets/Fluent/ArturiaMidiControlCenter.png" }
		};

		VideoProduction.ItemsSource = new List<GridViewItem>
		{
			new() { Text = "DaVinci Resolve", ImageSource = "ms-appx:///Assets/Fluent/DavinciResolve.png" },
			new() { Text = "Blender", ImageSource = "ms-appx:///Assets/Fluent/Blender.png" },
			new() { Text = "CapCut", ImageSource = "ms-appx:///Assets/Fluent/CapCut.png" },
			new() { Text = "LosslessCut", ImageSource = "ms-appx:///Assets/Fluent/LosslessCut.png" }
		};

		Multimedia.ItemsSource = new List<GridViewItem>
		{
			new() { Text = "MPC-QT", ImageSource = "ms-appx:///Assets/Fluent/MpcQt.png" },
			new() { Text = "mpv", ImageSource = "ms-appx:///Assets/Fluent/MPV.png" },
			new() { Text = "VLC", ImageSource = "ms-appx:///Assets/Fluent/VLC.png" },
			new() { Text = "MediaInfo", ImageSource = "ms-appx:///Assets/Fluent/MediaInfo.png" }
		};

		Office.ItemsSource = new List<GridViewItem>
		{
			new() { Text = "Word", ImageSource = "ms-appx:///Assets/Fluent/Word.png" },
			new() { Text = "Excel", ImageSource = "ms-appx:///Assets/Fluent/Excel.png" },
			new() { Text = "PowerPoint", ImageSource = "ms-appx:///Assets/Fluent/Powerpoint.png" },
			new() { Text = "OneNote", ImageSource = "ms-appx:///Assets/Fluent/OneNote.png" },
			new() { Text = "Teams", ImageSource = "ms-appx:///Assets/Fluent/Teams.png" },
			new() { Text = "Outlook", ImageSource = "ms-appx:///Assets/Fluent/Outlook.png" },
			new() { Text = "OneDrive", ImageSource = "ms-appx:///Assets/Fluent/OneDrive.png" }
		};

		Miscellaneous.ItemsSource = new List<GridViewItem>
		{
			new() { Text = "CapFrameX", ImageSource = "ms-appx:///Assets/Fluent/CapFrameX.png" },
			new() { Text = "MiniTool Partition Wizard", ImageSource = "ms-appx:///Assets/Fluent/MiniToolPartitionWizard.png" },
			new() { Text = "AOMEI Partition Assistant", ImageSource = "ms-appx:///Assets/Fluent/AomeiPartitionAssistant.png" },
			new() { Text = "WizTree", ImageSource = "ms-appx:///Assets/Fluent/WizTree.png" },
			new() { Text = "CrystalDiskMark", ImageSource = "ms-appx:///Assets/Fluent/CrystalDiskMark.png" },
			new() { Text = "Bulk Crap Uninstaller", ImageSource = "ms-appx:///Assets/Fluent/BulkCrapUninstaller.png" },
			new() { Text = "Bluetooth Audio Receiver", ImageSource = "ms-appx:///Assets/Fluent/BluetoothAudioReceiver.png" },
			new() { Text = "AnyDesk", ImageSource = "ms-appx:///Assets/Fluent/AnyDesk.png" },
			new() { Text = "RustDesk", ImageSource = "ms-appx:///Assets/Fluent/RustDesk.png" },
			new() { Text = "Apollo", ImageSource = "ms-appx:///Assets/Fluent/Apollo.png" },
			new() { Text = "AutoHotkey", ImageSource = "ms-appx:///Assets/Fluent/AutoHotkey.png" },
			new() { Text = "EmEditor", ImageSource = "ms-appx:///Assets/Fluent/EmEditor.png" },
			new() { Text = "WinDbg", ImageSource = "ms-appx:///Assets/Fluent/WinDbg.png" }
		};
	}

	private void GetMessaging()
	{
		var selectedMessaging = localSettings.Values["Messaging"] as string;
		var messagingItems = Messaging.ItemsSource as List<GridViewItem>;
		Messaging.SelectedItems.AddRange(
			selectedMessaging?.Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries)
			.Select(e => messagingItems?.FirstOrDefault(ext => ext.Text == e))
			.Where(ext => ext != null) ?? Enumerable.Empty<GridViewItem>()
		);

		isInitializingMessagingState = false;
	}

	private void GetLaunchers()
	{
		var selectedLaunchers = localSettings.Values["Launchers"] as string;
		var launcherItems = Launchers.ItemsSource as List<GridViewItem>;
		Launchers.SelectedItems.AddRange(
			selectedLaunchers?.Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries)
			.Select(e => launcherItems?.FirstOrDefault(ext => ext.Text == e))
			.Where(ext => ext != null) ?? Enumerable.Empty<GridViewItem>()
		);

		isInitializingLaunchersState = false;
	}

	private void GetMusic()
	{
		var selectedMusic = localSettings.Values["Music"] as string;
		var musicItems = Music.ItemsSource as List<GridViewItem>;
		Music.SelectedItems.AddRange(
			selectedMusic?.Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries)
			.Select(e => musicItems?.FirstOrDefault(ext => ext.Text == e))
			.Where(ext => ext != null) ?? Enumerable.Empty<GridViewItem>()
		);

		isInitializingMusicState = false;
	}

	private void GetPeripherals()
	{
		var selectedPeripherals = localSettings.Values["Peripherals"] as string;
		var peripheralItems = Peripherals.ItemsSource as List<GridViewItem>;
		Peripherals.SelectedItems.AddRange(
			selectedPeripherals?.Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries)
			.Select(e => peripheralItems?.FirstOrDefault(ext => ext.Text == e))
			.Where(ext => ext != null) ?? Enumerable.Empty<GridViewItem>()
		);

		isInitializingPeripheralsState = false;
	}

	private void GetControllers()
	{
		var selectedControllers = localSettings.Values["Controllers"] as string;
		var controllersItems = Controllers.ItemsSource as List<GridViewItem>;
		Controllers.SelectedItems.AddRange(
			selectedControllers?.Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries)
			.Select(e => controllersItems?.FirstOrDefault(ext => ext.Text == e))
			.Where(ext => ext != null) ?? Enumerable.Empty<GridViewItem>()
		);

		isInitializingControllersState = false;
	}

	private void GetDevelopment()
	{
		var selectedDevelopment = localSettings.Values["Development"] as string;
		var developmentItems = Development.ItemsSource as List<GridViewItem>;
		Development.SelectedItems.AddRange(
			selectedDevelopment?.Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries)
			.Select(e => developmentItems?.FirstOrDefault(ext => ext.Text == e))
			.Where(ext => ext != null) ?? Enumerable.Empty<GridViewItem>()
		);

		isInitializingDevelopmentState = false;
	}

	private void GetSysinternals()
	{
		var selectedSysinternals = localSettings.Values["Sysinternals"] as string;
		var sysinternalsItems = Sysinternals.ItemsSource as List<GridViewItem>;
		Sysinternals.SelectedItems.AddRange(
			selectedSysinternals?.Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries)
			.Select(e => sysinternalsItems?.FirstOrDefault(ext => ext.Text == e))
			.Where(ext => ext != null) ?? Enumerable.Empty<GridViewItem>()
		);

		isInitializingSysinternalsState = false;
	}

	private void GetOverclocking()
	{
		var selectedOverclocking = localSettings.Values["Overclocking"] as string;
		var overclockingItems = Overclocking.ItemsSource as List<GridViewItem>;
		Overclocking.SelectedItems.AddRange(
			selectedOverclocking?.Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries)
			.Select(e => overclockingItems?.FirstOrDefault(ext => ext.Text == e))
			.Where(ext => ext != null) ?? Enumerable.Empty<GridViewItem>()
		);

		isInitializingOverclockingState = false;
	}

	private void GetMusicProduction()
	{
		var selectedMusicProduction = localSettings.Values["MusicProduction"] as string;
		var musicProductionItems = MusicProduction.ItemsSource as List<GridViewItem>;
		MusicProduction.SelectedItems.AddRange(
			selectedMusicProduction?.Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries)
			.Select(e => musicProductionItems?.FirstOrDefault(ext => ext.Text == e))
			.Where(ext => ext != null) ?? Enumerable.Empty<GridViewItem>()
		);

		isInitializingMusicProductionState = false;
	}

	private void GetVideoProduction()
	{
		var selectedVideoProduction = localSettings.Values["VideoProduction"] as string;
		var videoProductionItems = VideoProduction.ItemsSource as List<GridViewItem>;
		VideoProduction.SelectedItems.AddRange(
			selectedVideoProduction?.Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries)
			.Select(e => videoProductionItems?.FirstOrDefault(ext => ext.Text == e))
			.Where(ext => ext != null) ?? Enumerable.Empty<GridViewItem>()
		);

		isInitializingVideoProductionState = false;
	}

	private void GetMultimedia()
	{
		var selectedMultimedia = localSettings.Values["Multimedia"] as string;
		var multimediaItems = Multimedia.ItemsSource as List<GridViewItem>;
		Multimedia.SelectedItems.AddRange(
			selectedMultimedia?.Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries)
			.Select(e => multimediaItems?.FirstOrDefault(ext => ext.Text == e))
			.Where(ext => ext != null) ?? Enumerable.Empty<GridViewItem>()
		);

		isInitializingMultimediaState = false;
	}

	private void GetOffice()
	{
		var selectedOffice = localSettings.Values["Office"] as string;
		var oficeItems = Office.ItemsSource as List<GridViewItem>;
		Office.SelectedItems.AddRange(
			selectedOffice?.Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries)
			.Select(e => oficeItems?.FirstOrDefault(ext => ext.Text == e))
			.Where(ext => ext != null) ?? Enumerable.Empty<GridViewItem>()
		);

		isInitializingOfficeState = false;
	}

	private void GetMiscellaneous()
	{
		var selectedMiscellaneous = localSettings.Values["Miscellaneous"] as string;
		var miscellaneousItems = Miscellaneous.ItemsSource as List<GridViewItem>;
		Miscellaneous.SelectedItems.AddRange(
			selectedMiscellaneous?.Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries)
			.Select(e => miscellaneousItems?.FirstOrDefault(ext => ext.Text == e))
			.Where(ext => ext != null) ?? Enumerable.Empty<GridViewItem>()
		);

		isInitializingMiscellaneousState = false;
	}

	private void Messaging_Changed(object sender, SelectionChangedEventArgs e)
	{
		if (isInitializingMessagingState) return;

		var selectedMessaging = Messaging.SelectedItems
			.Cast<GridViewItem>()
			.Select(item => item.Text)
			.ToArray();

		localSettings.Values["Messaging"] = string.Join(", ", selectedMessaging);
	}

	private void Launchers_Changed(object sender, SelectionChangedEventArgs e)
	{
		if (isInitializingLaunchersState) return;

		var selectedLaunchers = Launchers.SelectedItems
			.Cast<GridViewItem>()
			.Select(item => item.Text)
			.ToArray();

		localSettings.Values["Launchers"] = string.Join(", ", selectedLaunchers);
	}

	private void Music_Changed(object sender, SelectionChangedEventArgs e)
	{
		if (isInitializingMusicState) return;

		var selectedMusic = Music.SelectedItems
			.Cast<GridViewItem>()
			.Select(item => item.Text)
			.ToArray();

		localSettings.Values["Music"] = string.Join(", ", selectedMusic);
	}

	private void Peripherals_Changed(object sender, SelectionChangedEventArgs e)
	{
		if (isInitializingPeripheralsState) return;

		var selectedPeripherals = Peripherals.SelectedItems
			.Cast<GridViewItem>()
			.Select(item => item.Text)
			.ToArray();

		localSettings.Values["Peripherals"] = string.Join(", ", selectedPeripherals);
	}

	private void Controllers_Changed(object sender, SelectionChangedEventArgs e)
	{
		if (isInitializingControllersState) return;

		var selectedControllers = Controllers.SelectedItems
			.Cast<GridViewItem>()
			.Select(item => item.Text)
			.ToArray();

		localSettings.Values["Controllers"] = string.Join(", ", selectedControllers);
	}

	private void Development_Changed(object sender, SelectionChangedEventArgs e)
	{
		if (isInitializingDevelopmentState) return;

		var selectedDevelopment = Development.SelectedItems
			.Cast<GridViewItem>()
			.Select(item => item.Text)
			.ToArray();

		localSettings.Values["Development"] = string.Join(", ", selectedDevelopment);
	}

	private void Sysinternals_Changed(object sender, SelectionChangedEventArgs e)
	{
		if (isInitializingSysinternalsState) return;

		var selectedSysinternals = Sysinternals.SelectedItems
			.Cast<GridViewItem>()
			.Select(item => item.Text)
			.ToArray();

		localSettings.Values["Sysinternals"] = string.Join(", ", selectedSysinternals);
	}

	private void Overclocking_Changed(object sender, SelectionChangedEventArgs e)
	{
		if (isInitializingOverclockingState) return;

		var selectedOverclocking = Overclocking.SelectedItems
			.Cast<GridViewItem>()
			.Select(item => item.Text)
			.ToArray();

		localSettings.Values["Overclocking"] = string.Join(", ", selectedOverclocking);
	}

	private void MusicProduction_Changed(object sender, SelectionChangedEventArgs e)
	{
		if (isInitializingMusicProductionState) return;

		var selectedMusicProduction = MusicProduction.SelectedItems
			.Cast<GridViewItem>()
			.Select(item => item.Text)
			.ToArray();

		localSettings.Values["MusicProduction"] = string.Join(", ", selectedMusicProduction);
	}

	private void VideoProduction_Changed(object sender, SelectionChangedEventArgs e)
	{
		if (isInitializingVideoProductionState) return;

		var selectedVideoProduction = VideoProduction.SelectedItems
			.Cast<GridViewItem>()
			.Select(item => item.Text)
			.ToArray();

		localSettings.Values["VideoProduction"] = string.Join(", ", selectedVideoProduction);
	}

	private void Multimedia_Changed(object sender, SelectionChangedEventArgs e)
	{
		if (isInitializingMultimediaState) return;

		var selectedMultimedia = Multimedia.SelectedItems
			.Cast<GridViewItem>()
			.Select(item => item.Text)
			.ToArray();

		localSettings.Values["Multimedia"] = string.Join(", ", selectedMultimedia);
	}

	private void Office_Changed(object sender, SelectionChangedEventArgs e)
	{
		if (isInitializingOfficeState) return;

		var selectedOffice = Office.SelectedItems
			.Cast<GridViewItem>()
			.Select(item => item.Text)
			.ToArray();

		localSettings.Values["Office"] = string.Join(", ", selectedOffice);
	}

	private void Miscellaneous_Changed(object sender, SelectionChangedEventArgs e)
	{
		if (isInitializingMiscellaneousState) return;

		var selectedMiscellaneous = Miscellaneous.SelectedItems
			.Cast<GridViewItem>()
			.Select(item => item.Text)
			.ToArray();

		localSettings.Values["Miscellaneous"] = string.Join(", ", selectedMiscellaneous);
	}
}

[GeneratedBindableCustomProperty]
public partial class GridViewItem
{
	public string Text { get; set; }
	public string ImageSource { get; set; }
}
