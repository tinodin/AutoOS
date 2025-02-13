using Microsoft.Win32;
using Windows.Gaming.Input;

namespace AutoOS.Views.Installer;

public sealed partial class GamesPage : Page
{
    private bool isInitializingGamesState = true;
    const string registryPath = @"SOFTWARE\AutoOS";

    public GamesPage()
    {
        InitializeComponent();
        GetItems();
        GetGames();
        GetGamePath();
        CheckForGames();
    }

    public class GridViewItem
    {
        public string Text { get; set; }
        public string ImageSource { get; set; }
    }

    private void GetItems()
    {
        // add game items
        Games.ItemsSource = new List<GridViewItem>
        {
            new GridViewItem { Text = "Fortnite", ImageSource = "ms-appx:///Assets/Fluent/Fortnite.png" },
            //new GridViewItem { Text = "Valorant", ImageSource = "ms-appx:///Assets/Fluent/Valorant.png" },
            //new GridViewItem { Text = "GTA V", ImageSource = "ms-appx:///Assets/Fluent/Gta.png" }
        };
    }

    private void GetGames()
    {
        // get browser
        using var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\AutoOS");
        var selectedGame = key?.GetValue("Games") as string;
        var gameItems = Games.ItemsSource as List<GridViewItem>;
        Games.SelectedItem = gameItems?.FirstOrDefault(b => b.Text == selectedGame);

        isInitializingGamesState = false;
    }

    private void Games_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (isInitializingGamesState) return;

        // set value
        if (Games.SelectedItem is GridViewItem selectedItem)
        {
            using (var key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\AutoOS"))
            {
                key?.SetValue("Games", selectedItem.Text, RegistryValueKind.String);
            }
        }
    }

    private void GetGamePath()
    {
        using var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\AutoOS");
        var value = key?.GetValue("GamePath") as string;
        if (!string.IsNullOrEmpty(value))
        {
            // add infobar
            var infoBar = new InfoBar
            {
                Title = value,
                IsClosable = true,
                IsOpen = true,
                Severity = InfoBarSeverity.Success,
                Margin = new Thickness(5)
            };

            infoBar.CloseButtonClick += (_, _) =>
            {
                // remove value
                Registry.CurrentUser.CreateSubKey(@"SOFTWARE\AutoOS", true)?.DeleteValue("Games", false);
                Registry.CurrentUser.CreateSubKey(@"SOFTWARE\AutoOS", true)?.DeleteValue("GamePath", false);

                // deselect game
                Games.SelectedIndex = -1;

                // remove infobar
                GamesInfo.Children.Clear();

                using var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\AutoOS");
                var value = key?.GetValue("GamePath") as string;
                if (string.IsNullOrEmpty(value))
                {
                    // check for games
                    CheckForGames();
                }
            };
            GamesInfo.Children.Add(infoBar);
        }
    }

    private void CheckForGames()
    {
        using var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\AutoOS");
        var value = key?.GetValue("GamePath") as string;
        if (string.IsNullOrEmpty(value))
        {
            // declare locations
            string[] searchPaths =
            {
                @"Program Files\Epic Games\Fortnite",
                @"Games\Epic Games\Fortnite",
                @"Epic Games\Fortnite",
                @"Fortnite"
            };

            // check for games
            List<string> foundPaths = System.IO.DriveInfo.GetDrives()
                .SelectMany(drive => searchPaths.Select(path => System.IO.Path.Combine(drive.RootDirectory.FullName, path)))
                .Where(System.IO.Directory.Exists)
                .ToList();

            foreach (var foundPath in foundPaths)
            {
                // add infobar
                var infoBar = new InfoBar
                {
                    Title = $"Found Fortnite at: {foundPath}",
                    IsClosable = false,
                    IsOpen = true,
                    Severity = InfoBarSeverity.Informational,
                    Margin = new Thickness(5),
                    ActionButton = new Button
                    {
                        Content = "Use",
                        HorizontalAlignment = HorizontalAlignment.Right
                    }
                };

                infoBar.ActionButton.Click += (s, e) =>
                {
                    // select fortnite
                    if (Games.ItemsSource is List<GridViewItem> gameItems)
                    {
                        Games.SelectedItem = gameItems.FirstOrDefault(item => item.Text == "Fortnite");
                    }

                    // set value
                    using var updateKey = Registry.CurrentUser.CreateSubKey(registryPath);
                    updateKey?.SetValue("GamePath", foundPath, RegistryValueKind.String);

                    // remove infobar
                    GamesInfo.Children.Clear();

                    using var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\AutoOS");
                    var value = key?.GetValue("GamePath") as string;
                    if (!string.IsNullOrEmpty(value))
                    {
                        // get gamepath
                        GetGamePath();
                    }
                    else
                    {
                        // check for games
                        CheckForGames();
                    }
                };
                GamesInfo.Children.Add(infoBar);
            }
        }
    }

    private async void BrowseGamePath_Click(object sender, RoutedEventArgs e)
    {
        // remove infobar
        GamesInfo.Children.Clear();

        // add infobar
        var infoBar = new InfoBar
        {
            Title = "Please select the path for your game.",
            IsClosable = false,
            IsOpen = true,
            Severity = InfoBarSeverity.Informational,
            Margin = new Thickness(5)
        };
        GamesInfo.Children.Add(infoBar);

        // delay
        await Task.Delay(300);

        // launch picker
        var picker = await FileAndFolderPickerHelper.PickSingleFolderAsync(App.MainWindow);
        if (picker != null)
        {
            // remove infobar
            GamesInfo.Children.Clear();

            // select fortnite
            if (Games.ItemsSource is List<GridViewItem> gameItems)
            {
                Games.SelectedItem = gameItems.FirstOrDefault(item => item.Text == "Fortnite");
            }

            // set value
            using (var key = Registry.CurrentUser.CreateSubKey(registryPath))
            {
                key?.SetValue("GamePath", picker.Path, RegistryValueKind.String);
            }

            // get gamepath
            GetGamePath();
        }
        else
        {
            // remove infobar
            GamesInfo.Children.Clear();

            using var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\AutoOS");
            var value = key?.GetValue("GamePath") as string;
            if (!string.IsNullOrEmpty(value))
            {
                // get gamepath
                GetGamePath();
            }
            else
            {
                // check for games
                CheckForGames();
            }
        }
    }
}

