using Windows.Storage;

namespace AutoOS.Views.Installer;

public sealed partial class ApplicationsPage : Page
{
    private bool isInitializingMusicState = true;
    private bool isInitializingMessagingState = true;
    private bool isInitializingLaunchersState = true;

    private readonly ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

    public ApplicationsPage()
    {
        InitializeComponent();
        GetItems();
        GetMusic();
        GetMessaging();
        GetLaunchers();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        MainWindow.Instance.MarkVisited(nameof(ApplicationsPage));
        MainWindow.Instance.CheckAllPagesVisited();
    }

    public class GridViewItem
    {
        public string Text { get; set; }
        public string ImageSource { get; set; }
    }

    private void GetItems()
    {
        Music.ItemsSource = new List<GridViewItem>
        {
            new GridViewItem { Text = "Spotify", ImageSource = "ms-appx:///Assets/Fluent/Spotify.png" },
            new GridViewItem { Text = "Apple Music", ImageSource = "ms-appx:///Assets/Fluent/AppleMusic.png" },
            new GridViewItem { Text = "Amazon Music", ImageSource = "ms-appx:///Assets/Fluent/AmazonMusic.png" },
            new GridViewItem { Text = "Deezer Music", ImageSource = "ms-appx:///Assets/Fluent/DeezerMusic.png" }
        };

        Messaging.ItemsSource = new List<GridViewItem>
        {
            new GridViewItem { Text = "WhatsApp", ImageSource = "ms-appx:///Assets/Fluent/Whatsapp.png" },
            new GridViewItem { Text = "Discord", ImageSource = "ms-appx:///Assets/Fluent/Discord.png" }
        };

        Launchers.ItemsSource = new List<GridViewItem>
        {
            new GridViewItem { Text = "Epic Games", ImageSource = "ms-appx:///Assets/Fluent/EpicGames.png" },
            new GridViewItem { Text = "Steam", ImageSource = "ms-appx:///Assets/Fluent/Steam.png" }
        };
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

    private void Music_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (isInitializingMusicState) return;

        var selectedMusic = Music.SelectedItems
            .Cast<GridViewItem>()
            .Select(item => item.Text)
            .ToArray();

        localSettings.Values["Music"] = string.Join(", ", selectedMusic);
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
}