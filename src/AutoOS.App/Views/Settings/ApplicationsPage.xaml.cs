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
	private readonly ObservableCollection<GridViewItem> developmentItems = [];
    private readonly ObservableCollection<GridViewItem> officeItems = [];


    public ApplicationsPage()
    {
        InitializeComponent();
        GetItems();
        
        Messaging.ItemsSource = messagingItems;
        Launchers.ItemsSource = launchersItems;
        Music.ItemsSource = musicItems;
        Peripherals.ItemsSource = peripheralsItems;
        Development.ItemsSource = developmentItems;
        Office.ItemsSource = officeItems;
        
        messagingItems.CollectionChanged += (s, e) => Bindings.Update();
        launchersItems.CollectionChanged += (s, e) => Bindings.Update();
        musicItems.CollectionChanged += (s, e) => Bindings.Update();
        peripheralsItems.CollectionChanged += (s, e) => Bindings.Update();
        developmentItems.CollectionChanged += (s, e) => Bindings.Update();
        officeItems.CollectionChanged += (s, e) => Bindings.Update();
    }

    public Visibility GetVisibility(int count) => count > 0 ? Visibility.Visible : Visibility.Collapsed;

    private void GetItems()
    {
        var messagingList = new List<GridViewItem>
        {
            new() { Text = "Discord", ImageSource = "ms-appx:///Assets/Fluent/Discord.png", IsInstalled = Directory.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Discord")) },
            new() { Text = "WhatsApp", ImageSource = "ms-appx:///Assets/Fluent/Whatsapp.png", IsInstalled = Directory.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Packages", "5319275A.WhatsAppDesktop_cv1g1gvanyjgm")) }
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
            new() { Text = "Rockstar Games Launcher", ImageSource = "ms-appx:///Assets/Fluent/RockstarGamesLauncher.png", IsInstalled = File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Rockstar Games", "Launcher", "Launcher.exe")) },
            new() { Text = "FiveM", ImageSource = "ms-appx:///Assets/Fluent/FiveM.jpg", IsInstalled = File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FiveM", "FiveM.exe")) },
            new() { Text = "FACEIT", ImageSource = "ms-appx:///Assets/Fluent/FACEIT.png", IsInstalled = File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FACEIT", "FACEIT.exe")) }
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
            new() { Text = "Spotify", ImageSource = "ms-appx:///Assets/Fluent/Spotify.png", IsInstalled = File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Spotify", "Spotify.exe")) }
        };
        foreach (var item in musicList.Where(item => !item.IsInstalled))
            musicItems.Add(item);

        var peripheralsList = new List<GridViewItem>
        {
            new() { Text = "Logitech G HUB", ImageSource = "ms-appx:///Assets/Fluent/Logitech.png", IsInstalled = File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "LGHUB", "lghub.exe")) },
            new() { Text = "Logitech Onboard Memory Manager", ImageSource = "ms-appx:///Assets/Fluent/Logitech.png", IsInstalled = File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Logitech Onboard Memory Manager", "OnboardMemoryManager.exe")) },
            new() { Text = "Wootility", ImageSource = "ms-appx:///Assets/Fluent/Wootility.png", IsInstalled = File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "wootility", "Wootility.exe")) },
            new() { Text = "SteelSeries GG", ImageSource = "ms-appx:///Assets/Fluent/SteelSeriesGG.png", IsInstalled = File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "SteelSeries", "GG", "SteelSeriesGGEZ.exe")) },
            new() { Text = "Razer Synapse", ImageSource = "ms-appx:///Assets/Fluent/RazerSynapse.png", IsInstalled = File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Razer", "RazerAppEngine", "RazerAppEngine.exe")) },
			new() { Text = "Corsair iCUE", ImageSource = "ms-appx:///Assets/Fluent/CorsairICue.png", IsInstalled = File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Corsair", "Corsair iCUE5 Software", "iCUE.exe")) }
        };
        foreach (var item in peripheralsList.Where(item => !item.IsInstalled))
            peripheralsItems.Add(item);

        var devList = new List<GridViewItem>
        {
            new() { Text = "Visual Studio", ImageSource = "ms-appx:///Assets/Fluent/VisualStudio.png", IsInstalled = Directory.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Microsoft Visual Studio")) },
            new() { Text = "Visual Studio Code", ImageSource = "ms-appx:///Assets/Fluent/VisualStudioCode.png", IsInstalled = File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "Microsoft VS Code", "Code.exe")) },
            new() { Text = "Antigravity", ImageSource = "ms-appx:///Assets/Fluent/Antigravity.png", IsInstalled = File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "Antigravity", "Antigravity.exe")) },
            new() { Text = "Git", ImageSource = "ms-appx:///Assets/Fluent/Git.png", IsInstalled = File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Git", "bin", "git.exe")) },
            new() { Text = "Python", ImageSource = "ms-appx:///Assets/Fluent/Python.png", IsInstalled = File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "py.exe")) },
            new() { Text = "Node.js", ImageSource = "ms-appx:///Assets/Fluent/Nodejs.png", IsInstalled = File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "nodejs", "node.exe")) },
            new() { Text = "Trello", ImageSource = "ms-appx:///Assets/Fluent/Trello.png", IsInstalled = Directory.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Packages", "45273LiamForsyth.PawsforTrello_7pb5ddty8z1pa")) }
		};
        foreach (var item in devList.Where(item => !item.IsInstalled))
            developmentItems.Add(item);

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
    }

    private void GridView_SelectionChanged(object sender, SelectionChangedEventArgs e) 
    {
        InstallButton.IsEnabled = Office.SelectedItems.Count > 0 || Development.SelectedItems.Count > 0 || Music.SelectedItems.Count > 0 || Peripherals.SelectedItems.Count > 0 || Messaging.SelectedItems.Count > 0 || Launchers.SelectedItems.Count > 0;
    }

    private async void InstallButton_Click(object sender, RoutedEventArgs e)
    {
        var selection = new ApplicationSelection();
        
        var selectedMessagingItems = Messaging.SelectedItems.Cast<GridViewItem>().ToList();
        var selectedMessaging = selectedMessagingItems.Select(item => item.Text).ToList();
        selection.Discord = selectedMessaging.Contains("Discord");
        selection.WhatsApp = selectedMessaging.Contains("WhatsApp");

        var selectedLaunchersItems = Launchers.SelectedItems.Cast<GridViewItem>().ToList();
        var selectedLaunchers = selectedLaunchersItems.Select(item => item.Text).ToList();
        selection.EpicGames = selectedLaunchers.Contains("Epic Games");
        selection.Steam = selectedLaunchers.Contains("Steam");
        selection.RiotClient = selectedLaunchers.Contains("Riot Client");
        selection.UbisoftConnect = selectedLaunchers.Contains("Ubisoft Connect");
        selection.EA = selectedLaunchers.Contains("EA");
        selection.BattleNet = selectedLaunchers.Contains("Battle.Net");
        selection.MinecraftLauncher = selectedLaunchers.Contains("Minecraft Launcher");
        selection.RockstarGamesLauncher = selectedLaunchers.Contains("Rockstar Games Launcher");
        selection.FiveM = selectedLaunchers.Contains("FiveM");
        selection.FACEIT = selectedLaunchers.Contains("FACEIT");

        var selectedMusicItems = Music.SelectedItems.Cast<GridViewItem>().ToList();
        var selectedMusic = selectedMusicItems.Select(item => item.Text).ToList();
        selection.AppleMusic = selectedMusic.Contains("Apple Music");
        selection.Tidal = selectedMusic.Contains("TIDAL");
        selection.Qobuz = selectedMusic.Contains("Qobuz");
        selection.AmazonMusic = selectedMusic.Contains("Amazon Music");
        selection.DeezerMusic = selectedMusic.Contains("Deezer Music");
        selection.Spotify = selectedMusic.Contains("Spotify");

        var selectedPeripheralsItems = Peripherals.SelectedItems.Cast<GridViewItem>().ToList();
        var selectedPeripherals = selectedPeripheralsItems.Select(item => item.Text).ToList();
        selection.SteelSeriesGG = selectedPeripherals.Contains("SteelSeries GG");
        selection.RazerSynapse = selectedPeripherals.Contains("Razer Synapse");
        selection.LogitechGHub = selectedPeripherals.Contains("Logitech G HUB");
        selection.LogitechOnboardMemoryManager = selectedPeripherals.Contains("Logitech Onboard Memory Manager");
        selection.Wootility = selectedPeripherals.Contains("Wootility");
        selection.CorsairICue = selectedPeripherals.Contains("Corsair iCUE");

        var selectedDevItems = Development.SelectedItems.Cast<GridViewItem>().ToList();
        var selectedDev = selectedDevItems.Select(item => item.Text).ToList();
        selection.VisualStudio = selectedDev.Contains("Visual Studio");
        selection.VisualStudioCode = selectedDev.Contains("Visual Studio Code");
        selection.Antigravity = selectedDev.Contains("Antigravity");
        selection.Git = selectedDev.Contains("Git");
        selection.Python = selectedDev.Contains("Python");
        selection.Nodejs = selectedDev.Contains("Node.js");
        selection.Trello = selectedDev.Contains("Trello");

        var selectedOfficeItems = Office.SelectedItems.Cast<GridViewItem>().ToList();
        var selectedOffice = selectedOfficeItems.Select(item => item.Text).ToList();
        selection.Word = selectedOffice.Contains("Word");
        selection.Excel = selectedOffice.Contains("Excel");
        selection.PowerPoint = selectedOffice.Contains("PowerPoint");
        selection.OneNote = selectedOffice.Contains("OneNote");
        selection.Teams = selectedOffice.Contains("Teams");
        selection.Outlook = selectedOffice.Contains("Outlook");
        selection.OneDrive = selectedOffice.Contains("OneDrive");

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
                    ["ContentDialogMinWidth"] = 500,
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
            
            foreach (var item in selectedDevItems)
                developmentItems.Remove(item);

            foreach (var item in selectedOfficeItems)
                officeItems.Remove(item);
            
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
