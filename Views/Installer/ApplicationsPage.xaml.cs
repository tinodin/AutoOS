using Microsoft.Win32;

namespace AutoOS.Views.Installer;

public sealed partial class ApplicationsPage : Page
{
    private bool isInitializingMusicState = true;
    private bool isInitializingMessagingState = true;
    private bool isInitializingLaunchersState = true;

    public ApplicationsPage()
    {
        InitializeComponent();
        GetItems();
        GetMusic();
        GetMessaging();
        GetLaunchers();
    }

    public class GridViewItem
    {
        public string Text { get; set; }
        public string ImageSource { get; set; }
    }

    private void GetItems()
    {
        // add music items
        Music.ItemsSource = new List<GridViewItem>
        {
            new GridViewItem { Text = "Spotify", ImageSource = "ms-appx:///Assets/Fluent/Spotify.png" },
            new GridViewItem { Text = "Apple Music", ImageSource = "ms-appx:///Assets/Fluent/AppleMusic.png" },
            new GridViewItem { Text = "Amazon Music", ImageSource = "ms-appx:///Assets/Fluent/AmazonMusic.png" },
            new GridViewItem { Text = "Deezer Music", ImageSource = "ms-appx:///Assets/Fluent/DeezerMusic.png" }
        };

        // add messaging items
        Messaging.ItemsSource = new List<GridViewItem>
        {
            new GridViewItem { Text = "WhatsApp", ImageSource = "ms-appx:///Assets/Fluent/Whatsapp.png" },
            new GridViewItem { Text = "Discord", ImageSource = "ms-appx:///Assets/Fluent/Discord.png" }
        };

        // add launchers items
        Launchers.ItemsSource = new List<GridViewItem>
        {
            new GridViewItem { Text = "Epic Games", ImageSource = "ms-appx:///Assets/Fluent/EpicGames.png" },
            new GridViewItem { Text = "Steam", ImageSource = "ms-appx:///Assets/Fluent/Steam.png" }
        };
    }

    private void GetMusic()
    {
        // get music
        using var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\AutoOS");
        var selectedMusic = key?.GetValue("Music") as string;
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
        // get messaging
        using var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\AutoOS");
        var selectedMessaging = key?.GetValue("Messaging") as string;
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
        // get launchers
        using var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\AutoOS");
        var selectedLaunchers = key?.GetValue("Launchers") as string;
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

        // set value
        var selectedMusic = Music.SelectedItems
            .Cast<GridViewItem>()
            .Select(item => item.Text)
            .ToArray();

        using (var key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\AutoOS"))
        {
            key?.SetValue("Music", string.Join(", ", selectedMusic), RegistryValueKind.String);
        }
    }

    private void Messaging_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (isInitializingMessagingState) return;

        // set value
        var selectedMessaging = Messaging.SelectedItems
            .Cast<GridViewItem>()
            .Select(item => item.Text)
            .ToArray();

        using (var key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\AutoOS"))
        {
            key?.SetValue("Messaging", string.Join(", ", selectedMessaging), RegistryValueKind.String);
        }
    }

    private void Launchers_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (isInitializingLaunchersState) return;

        // set value
        var selectedLaunchers = Launchers.SelectedItems
            .Cast<GridViewItem>()
            .Select(item => item.Text)
            .ToArray();

        using (var key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\AutoOS"))
        {
            key?.SetValue("Launchers", string.Join(", ", selectedLaunchers), RegistryValueKind.String);
        }
    }
}
