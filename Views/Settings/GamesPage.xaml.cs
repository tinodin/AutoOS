using AutoOS.Helpers;
using AutoOS.Views.Settings.Games;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Diagnostics;
using System.Text.Json;
using Windows.Storage;

namespace AutoOS.Views.Settings;

public sealed partial class GamesPage : Page
{
    private bool isInitializingAccounts = true;
    public static GamesPage Instance { get; private set; }
    public GameGallery Games => games;

    private readonly ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

    public GamesPage()
    {
        Instance = this;
        InitializeComponent();
        LoadGames();
        LoadEpicAccounts();
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private string currentSortKey = "Title";
    private bool ascending = true;

    private void SortByName_Click(object sender, RoutedEventArgs e)
    {
        currentSortKey = "Title";
        UpdateSortIcons();
        ApplySort();
        SaveSortSettings();
    }

    private void SortByLauncher_Click(object sender, RoutedEventArgs e)
    {
        currentSortKey = "Launcher";
        UpdateSortIcons();
        ApplySort();
        SaveSortSettings();
    }

    private void SortAscending_Click(object sender, RoutedEventArgs e)
    {
        ascending = true;
        UpdateSortIcons();
        ApplySort();
        SaveSortSettings();
    }

    private void SortDescending_Click(object sender, RoutedEventArgs e)
    {
        ascending = false;
        UpdateSortIcons();
        ApplySort();
        SaveSortSettings();
    }

    private void UpdateSortIcons()
    {
        SortByName.Icon = currentSortKey == "Title" ? new FontIcon { Glyph = "\uE915" } : null;
        SortByLauncher.Icon = currentSortKey == "Launcher" ? new FontIcon { Glyph = "\uE915" } : null;

        SortAscending.Icon = ascending ? new FontIcon { Glyph = "\uE915" } : null;
        SortDescending.Icon = !ascending ? new FontIcon { Glyph = "\uE915" } : null;
    }

    private void ApplySort()
    {
        var container = games.HeaderContent as StackPanel;
        if (container == null) return;

        var panels = container.Children
            .OfType<GamePanel>()
            .Where(g => g != null)
            .ToList();

        if (panels.Count == 0) return;

        IEnumerable<GamePanel> result;

        if (currentSortKey == "Title")
        {
            result = ascending
                ? panels.OrderBy(g => g.Title ?? "", StringComparer.CurrentCultureIgnoreCase)
                : panels.OrderByDescending(g => g.Title ?? "", StringComparer.CurrentCultureIgnoreCase);
        }
        else
        {
            if (ascending)
            {
                result = panels
                    .OrderBy(g => g.Launcher ?? "", StringComparer.CurrentCultureIgnoreCase)
                    .ThenBy(g => g.Title ?? "", StringComparer.CurrentCultureIgnoreCase);
            }
            else
            {
                result = panels
                    .OrderByDescending(g => g.Launcher ?? "", StringComparer.CurrentCultureIgnoreCase)
                    .ThenBy(g => g.Title ?? "", StringComparer.CurrentCultureIgnoreCase);
            }
        }

        container.Children.Clear();
        foreach (var panel in result)
        {
            container.Children.Add(panel);
        }
    }

    private void SaveSortSettings()
    {
        localSettings.Values["SortKey"] = currentSortKey;
        localSettings.Values["SortAscending"] = ascending;
    }

    private void LoadSortSettings()
    {
        var savedSortKey = localSettings.Values["SortKey"];
        var savedAscending = localSettings.Values["SortAscending"];

        currentSortKey = savedSortKey as string ?? "Title";
        ascending = savedAscending is bool b ? b : true;

        UpdateSortIcons();
        ApplySort();
    }

    private async void LoadGames()
    {
        // epic games
        await EpicGamesHelper.LoadGames();

        // steam
        await SteamHelper.LoadGames();

        // add custom games
        await CustomGameHelper.LoadGames();

        // sort games
        LoadSortSettings();

        // align games left
        Games_SwitchPresenter.HorizontalAlignment = HorizontalAlignment.Left;

        // show game gallery
        Games_SwitchPresenter.Value = false;
    }

    private async void AddCustomGame_Click(object sender, RoutedEventArgs e)
    {
        // show content dialog
        var contentDialog = new ContentDialog
        {
            Title = "Add Game",
            Content = new GameAdd(),
            PrimaryButtonText = "Add",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = this.XamlRoot,
        };

        contentDialog.Resources["ContentDialogMinWidth"] = 850;
        contentDialog.Resources["ContentDialogMaxWidth"] = 850;
        contentDialog.Resources["ContentDialogMinHeight"] = 250;
        contentDialog.Resources["ContentDialogMaxHeight"] = 850;

        ContentDialogResult result = await contentDialog.ShowAsync();

        if (result == ContentDialogResult.Primary)
        {
            // get data from gameadd
            var gameAddPage = (GameAdd)contentDialog.Content;

            // add game
            var gamePanel = new GamePanel
            {
                Launcher = gameAddPage.Launcher,
                LauncherLocation = gameAddPage.LauncherLocation,
                DataLocation = gameAddPage.DataLocation,
                InstallLocation = gameAddPage.GameLocation,
                Title = gameAddPage.GameName,
                Description = gameAddPage.GameDeveloper,
                ImageSource = new BitmapImage(new Uri(gameAddPage.GameCoverUrl))
            };

            var stackPanel = (StackPanel)games.HeaderContent;

            bool exists = stackPanel.Children
                .OfType<GamePanel>()
                .Any(g => g.Title == gamePanel.Title);

            if (!exists)
            {
                stackPanel.Children.Add(gamePanel);
            }

            // save game data
            var gameInfo = new
            {
                gameAddPage.Launcher,
                gameAddPage.LauncherLocation,
                gameAddPage.DataLocation,
                gameAddPage.GameLocation,
                gameAddPage.GameName,
                gameAddPage.GameDeveloper,
                gameAddPage.GameCoverUrl
            };

            Directory.CreateDirectory(Path.Combine(PathHelper.GetAppDataFolderPath(), "Games"));
            File.WriteAllText(Path.Combine(PathHelper.GetAppDataFolderPath(), "Games", $"{string.Concat(gamePanel.Title.Split(Path.GetInvalidFileNameChars()))}.json"), JsonSerializer.Serialize(gameInfo, JsonOptions));

            // sort games
            LoadSortSettings();

            // start watcher
            gamePanel.CheckGameRunning();
        }
    }

    public void LoadEpicAccounts()
    {
        string selectedAccount = null;

        List<(string DisplayName, string AccountId)> accountList = new();

        // get all configs
        if (File.Exists(EpicGamesHelper.EpicGamesPath))
        {
            foreach (var file in Directory.GetFiles(EpicGamesHelper.EpicGamesAccountDir, "GameUserSettings.ini", SearchOption.AllDirectories))
            {
                // check if data is valid
                if (EpicGamesHelper.ValidateData(file))
                {
                    string accountId = EpicGamesHelper.GetAccountData(file).AccountId;

                    // update config if accountids match
                    if (Directory.Exists(Path.Combine(EpicGamesHelper.EpicGamesAccountDir, accountId)))
                    {
                        if (File.Exists(EpicGamesHelper.ActiveEpicGamesAccountPath) && file != EpicGamesHelper.ActiveEpicGamesAccountPath)
                        {
                            if (EpicGamesHelper.GetAccountData(EpicGamesHelper.ActiveEpicGamesAccountPath).AccountId == accountId)
                            {
                                File.Copy(EpicGamesHelper.ActiveEpicGamesAccountPath, Path.Combine(EpicGamesHelper.EpicGamesAccountDir, accountId, "GameUserSettings.ini"), true);
                            }
                        }
                    }
                    // backup config if not already
                    else
                    {
                        // create folder
                        Directory.CreateDirectory(Path.Combine(EpicGamesHelper.EpicGamesAccountDir, accountId));

                        // copy config
                        File.Copy(EpicGamesHelper.ActiveEpicGamesAccountPath, Path.Combine(EpicGamesHelper.EpicGamesAccountDir, accountId, "GameUserSettings.ini"), true);

                        // create reg file
                        File.WriteAllText(Path.Combine(Path.Combine(EpicGamesHelper.EpicGamesAccountDir, accountId), "accountId.reg"), $"Windows Registry Editor Version 5.00\r\n\r\n[HKEY_CURRENT_USER\\Software\\Epic Games\\Unreal Engine\\Identifiers]\r\n\"AccountId\"=\"{accountId}\"");
                    }

                    if (!accountList.Any(x => x.AccountId == accountId))
                    {
                        accountList.Add((EpicGamesHelper.GetAccountData(file).DisplayName, accountId));
                    }

                    // store which to select later
                    if (file == EpicGamesHelper.ActiveEpicGamesAccountPath)
                    {
                        selectedAccount = accountId;
                    }
                }
            }

            // sort
            accountList = accountList.OrderBy(x => x.DisplayName).ToList();
            Accounts.Items.Clear();

            if (accountList.Count == 0)
            {
                Accounts.Items.Add(new ComboBoxItem { Content = "Not logged in", IsEnabled = false });
                Accounts.SelectedIndex = 0;
                Accounts.IsEnabled = false;
            }
            else
            {
                Accounts.IsEnabled = true;

                foreach (var (displayName, accountId) in accountList)
                {
                    var item = new ComboBoxItem
                    {
                        Content = displayName,
                        Tag = accountId
                    };

                    Accounts.Items.Add(item);

                    if (accountId == selectedAccount)
                        Accounts.SelectedItem = item;
                }
            }
        }

        isInitializingAccounts = false;
    }

    private async void Accounts_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (isInitializingAccounts) return;

        // close epic games launcher
        EpicGamesHelper.CloseEpicGames();

        // get accountId
        string accountId = (Accounts.SelectedItem as ComboBoxItem)?.Tag as string;

        // replace file
        File.Copy(Path.Combine(EpicGamesHelper.EpicGamesAccountDir, accountId, "GameUserSettings.ini"), EpicGamesHelper.ActiveEpicGamesAccountPath, true);

        // replace accountid
        Process.Start("regedit.exe", $"/s \"{Path.Combine(EpicGamesHelper.EpicGamesAccountDir, accountId, "accountId.reg")}\"");

        // launch epic games to get new token
        await Task.Run(() => Process.Start(new ProcessStartInfo(EpicGamesHelper.EpicGamesPath) { WindowStyle = ProcessWindowStyle.Hidden }));

        // wait for token to get used
        while (true)
        {
            await Task.Delay(100);

            if (!EpicGamesHelper.ValidateData(EpicGamesHelper.ActiveEpicGamesAccountPath))
            {
                UpdateInvalidEpicGamesToken();
                return;
            }

            if ((EpicGamesHelper.GetAccountData(EpicGamesHelper.ActiveEpicGamesAccountPath)).TokenUseCount == 1)
                break;
        }

        // wait for new token
        while (true)
        {
            await Task.Delay(100);

            if (!EpicGamesHelper.ValidateData(EpicGamesHelper.ActiveEpicGamesAccountPath))
            {
                UpdateInvalidEpicGamesToken();
                return;
            }

            if ((EpicGamesHelper.GetAccountData(EpicGamesHelper.ActiveEpicGamesAccountPath)).TokenUseCount == 0)
                break;
        }

        // close epic games launcher
        EpicGamesHelper.CloseEpicGames();

        // load accounts again to update the new login data
        isInitializingAccounts = true;
        LoadEpicAccounts();
    }

    private async void UpdateInvalidEpicGamesToken()
    {
        // remove infobar
        AccountInfo.Children.Clear();

        // add infobar
        AccountInfo.Children.Add(new InfoBar
        {
            Title = "The login token is no longer valid. Please enter your password again...",
            IsClosable = false,
            IsOpen = true,
            Severity = InfoBarSeverity.Error,
            Margin = new Thickness(5)
        });

        // close epic games launcher
        EpicGamesHelper.CloseEpicGames();

        // delay
        await Task.Delay(500);

        // launch epic games launcher
        Process.Start(EpicGamesHelper.EpicGamesPath);

        // check when logged in
        while (true)
        {
            if (File.Exists(EpicGamesHelper.ActiveEpicGamesAccountPath))
            {
                if (EpicGamesHelper.ValidateData(EpicGamesHelper.ActiveEpicGamesAccountPath))
                {
                    break;
                }
            }

            await Task.Delay(500);
        }

        // close epic games launcher
        EpicGamesHelper.CloseEpicGames();

        // disable tray and notifications
        EpicGamesHelper.DisableMinimizeToTray(EpicGamesHelper.ActiveEpicGamesAccountPath);
        EpicGamesHelper.DisableNotifications(EpicGamesHelper.ActiveEpicGamesAccountPath);

        // remove infobar
        AccountInfo.Children.Clear();

        // add infobar
        AccountInfo.Children.Add(new InfoBar
        {
            Title = $"Successfully logged in as {EpicGamesHelper.GetAccountData(EpicGamesHelper.ActiveEpicGamesAccountPath).DisplayName}",
            IsClosable = false,
            IsOpen = true,
            Severity = InfoBarSeverity.Success,
            Margin = new Thickness(5)
        });

        // refresh combobox
        isInitializingAccounts = true;
        LoadEpicAccounts();

        // delay
        await Task.Delay(2000);

        // remove infobar
        AccountInfo.Children.Clear();
    }

    private async void AddAccount_Click(object sender, RoutedEventArgs e)
    {
        // add content dialog
        var contentDialog = new ContentDialog
        {
            Title = "Add New Account",
            Content = "Are you sure that you want to add another account?",
            PrimaryButtonText = "Yes",
            CloseButtonText = "Cancel",
            XamlRoot = this.XamlRoot
        };

        ContentDialogResult result = await contentDialog.ShowAsync();

        // check result
        if (result == ContentDialogResult.Primary)
        {
            // remove infobar
            AccountInfo.Children.Clear();

            // add infobar
            AccountInfo.Children.Add(new InfoBar
            {
                Title = "Please log in to your Epic Games account...",
                IsClosable = false,
                IsOpen = true,
                Severity = InfoBarSeverity.Informational,
                Margin = new Thickness(5)
            });

            // close epic games launcher
            EpicGamesHelper.CloseEpicGames();

            // delete gameusersettings.ini
            if (File.Exists(EpicGamesHelper.ActiveEpicGamesAccountPath))
            {
                File.Delete(EpicGamesHelper.ActiveEpicGamesAccountPath);
            }

            // delay
            await Task.Delay(500);

            // launch epic games launcher
            Process.Start(EpicGamesHelper.EpicGamesPath);

            // check when logged in
            while (true)
            {
                if (File.Exists(EpicGamesHelper.ActiveEpicGamesAccountPath))
                {
                    if (File.Exists(EpicGamesHelper.ActiveEpicGamesAccountPath))
                    {
                        if (EpicGamesHelper.ValidateData(EpicGamesHelper.ActiveEpicGamesAccountPath))
                        {
                            break;
                        }
                    }
                }

                await Task.Delay(500);
            }

            // close epic games launcher
            EpicGamesHelper.CloseEpicGames();

            // disable tray and notifications
            EpicGamesHelper.DisableMinimizeToTray(EpicGamesHelper.ActiveEpicGamesAccountPath);
            EpicGamesHelper.DisableNotifications(EpicGamesHelper.ActiveEpicGamesAccountPath);
            
            // remove infobar
            AccountInfo.Children.Clear();

            // add infobar
            AccountInfo.Children.Add(new InfoBar
            {
                Title = $"Successfully logged in as {EpicGamesHelper.GetAccountData(EpicGamesHelper.ActiveEpicGamesAccountPath).DisplayName}",
                IsClosable = false,
                IsOpen = true,
                Severity = InfoBarSeverity.Success,
                Margin = new Thickness(5)
            });

            // refresh combobox
            LoadEpicAccounts();

            // delay
            await Task.Delay(2000);

            // remove infobar
            AccountInfo.Children.Clear();
        }
    }

    private async void RemoveAccount_Click(object sender, RoutedEventArgs e)
    {
        // making sure an account is selected
        if (Accounts.SelectedItem != null)
        {
            // add content dialog
            var contentDialog = new ContentDialog
            {
                Title = "Remove Account",
                Content = "Are you sure that you want to remove " + Accounts.SelectedItem + "?",
                PrimaryButtonText = "Yes",
                CloseButtonText = "No",
                XamlRoot = this.XamlRoot,
            };
            ContentDialogResult result = await contentDialog.ShowAsync();

            // check results
            if (result == ContentDialogResult.Primary)
            {
                // add infobar
                AccountInfo.Children.Add(new InfoBar
                {
                    Title = "Removing " + Accounts.SelectedItem + "...",
                    IsClosable = false,
                    IsOpen = true,
                    Severity = InfoBarSeverity.Informational,
                    Margin = new Thickness(5)
                });

                // close epic games launcher
                EpicGamesHelper.CloseEpicGames();

                // get accountId
                string accountId = EpicGamesHelper.GetAccountData(EpicGamesHelper.ActiveEpicGamesAccountPath).AccountId;

                // delete gameusersettings.ini
                File.Delete(EpicGamesHelper.ActiveEpicGamesAccountPath);

                Directory.Delete(Path.Combine(EpicGamesHelper.EpicGamesAccountDir, accountId));

                // delay
                await Task.Delay(500);

                // remove infobar
                AccountInfo.Children.Clear();

                // refresh combobox
                LoadEpicAccounts();

                // add infobar
                AccountInfo.Children.Add(new InfoBar
                {
                    Title = "Successfully removed " + Accounts.SelectedItem + ".",
                    IsClosable = false,
                    IsOpen = true,
                    Severity = InfoBarSeverity.Success,
                    Margin = new Thickness(5)
                });

                // delay
                await Task.Delay(2000);

                // remove infobar
                AccountInfo.Children.Clear();
            }
        }
    }
}