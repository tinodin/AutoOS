using AutoOS.Helpers;
using AutoOS.Views.Settings.Games;
using System.Diagnostics;
using System.Text.RegularExpressions;
using ValveKeyValue;
using Windows.Storage;

namespace AutoOS.Views.Settings;

public sealed partial class GamesPage : Page
{
    private bool isInitializingEpicGamesAccounts = true;
    private bool isInitializingSteamAccounts = true;
    public static GamesPage Instance { get; private set; }
    public GameGallery Games => games;
    public Microsoft.UI.Xaml.Shapes.Ellipse EgDataStatus => EgDataStatusEllipse;

    private readonly ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

    public GamesPage()
    {
        Instance = this;
        InitializeComponent();
        LoadEpicGamesAccounts();
        LoadSteamAccounts();
        LoadGames();
        UpdateGridLayout();
    }

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

    private void SortByPlayTime_Click(object sender, RoutedEventArgs e)
    {
        currentSortKey = "PlayTime";
        UpdateSortIcons();
        ApplySort();
        SaveSortSettings();
    }

    private void SortByRating_Click(object sender, RoutedEventArgs e)
    {
        currentSortKey = "Rating";
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
        SortByPlayTime.Icon = currentSortKey == "PlayTime" ? new FontIcon { Glyph = "\uE915" } : null;
        SortByRating.Icon = currentSortKey == "Rating" ? new FontIcon { Glyph = "\uE915" } : null;

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

        IEnumerable<GamePanel> result = currentSortKey switch
        {
            "Title" => ascending
                ? panels.OrderBy(g => g.Title ?? "", StringComparer.CurrentCultureIgnoreCase)
                : panels.OrderByDescending(g => g.Title ?? "", StringComparer.CurrentCultureIgnoreCase),

            "Launcher" => ascending
                ? panels.OrderBy(g => g.Launcher ?? "", StringComparer.CurrentCultureIgnoreCase)
                        .ThenBy(g => g.Title ?? "", StringComparer.CurrentCultureIgnoreCase)
                : panels.OrderByDescending(g => g.Launcher ?? "", StringComparer.CurrentCultureIgnoreCase)
                        .ThenBy(g => g.Title ?? "", StringComparer.CurrentCultureIgnoreCase),

            "PlayTime" => ascending
                ? panels.OrderBy(g => ParseMinutes(g.PlayTime))
                        .ThenBy(g => g.Title ?? "", StringComparer.CurrentCultureIgnoreCase)
                : panels.OrderByDescending(g => ParseMinutes(g.PlayTime))
                        .ThenBy(g => g.Title ?? "", StringComparer.CurrentCultureIgnoreCase),
            "Rating" => ascending
                ? panels.OrderBy(g => g.Rating).ThenBy(g => g.Title ?? "", StringComparer.CurrentCultureIgnoreCase)
                : panels.OrderByDescending(g => g.Rating).ThenBy(g => g.Title ?? "", StringComparer.CurrentCultureIgnoreCase),

            _ => panels
        };

        container.Children.Clear();
        foreach (var panel in result)
        {
            container.Children.Add(panel);
        }
    }

    private static int ParseMinutes(string time)
    {
        if (string.IsNullOrWhiteSpace(time))
            return 0;

        var match = Regex.Match(time, @"(?:(\d+)h)?\s*(\d+)m");
        if (match.Success)
        {
            int hours = match.Groups[1].Success ? int.Parse(match.Groups[1].Value) : 0;
            int minutes = int.Parse(match.Groups[2].Value);
            return hours * 60 + minutes;
        }

        return 0;
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
        ascending = savedAscending is not bool b || b;

        UpdateSortIcons();
        ApplySort();
    }

    private async void LoadGames()
    {
        // reset
        LoadSortSettings();
        Games_SwitchPresenter.Value = true;
        Games_SwitchPresenter.HorizontalAlignment = HorizontalAlignment.Center;
        GamesGrid.Height = 350;

        // load games
        await Task.WhenAll(
            EpicGamesHelper.LoadGames(),
            SteamHelper.LoadGames(),
            CustomGameHelper.LoadGames()
        );

        // sort games
        LoadSortSettings();

        if (games.HeaderContent is StackPanel { Children.Count: 0 })
        {
            NoGamesInstalled.Visibility = Visibility.Visible;
        }
        else
        {
            // set height to auto
            GamesGrid.Height = Double.NaN;

            // align games left
            Games_SwitchPresenter.HorizontalAlignment = HorizontalAlignment.Left;
        }

        // show game gallery
        Games_SwitchPresenter.Value = false;
    }

    public void LoadEpicGamesAccounts()
    {
        if (File.Exists(EpicGamesHelper.EpicGamesPath))
        {
            EpicGames_SettingsGroup.Visibility = Visibility.Visible;

            // get all accounts
            var accounts = EpicGamesHelper.GetEpicGamesAccounts();

            // reset ui elements
            EpicGamesAccounts.Items.Clear();
            EpicGamesAccounts.IsEnabled = accounts.Count > 0;
            RemoveEpicGamesAccountButton.IsEnabled = accounts.Count > 0;

            // add accounts to combobox
            if (accounts.Count == 0)
            {
                var notLoggedIn = new ComboBoxItem { Content = "Not logged in", IsEnabled = false };
                EpicGamesAccounts.Items.Add(notLoggedIn);
                EpicGamesAccounts.SelectedItem = notLoggedIn;
                EpicGamesAccounts.IsEnabled = false;
                RemoveEpicGamesAccountButton.IsEnabled = false;
            }
            else if (!accounts.Any(a => a.IsActive))
            {
                var notLoggedIn = new ComboBoxItem { Content = "Not logged in", IsEnabled = false };
                EpicGamesAccounts.Items.Add(notLoggedIn);
                EpicGamesAccounts.SelectedItem = notLoggedIn;
                RemoveEpicGamesAccountButton.IsEnabled = false;

                foreach (var account in accounts)
                {
                    var item = new ComboBoxItem
                    {
                        Content = account.DisplayName,
                        Tag = account.AccountId
                    };

                    EpicGamesAccounts.Items.Add(item);
                }
            }
            else
            {
                foreach (var account in accounts)
                {
                    var item = new ComboBoxItem
                    {
                        Content = account.DisplayName,
                        Tag = account.AccountId
                    };

                    EpicGamesAccounts.Items.Add(item);

                    if (account.IsActive)
                        EpicGamesAccounts.SelectedItem = item;
                }
            }
        }

        isInitializingEpicGamesAccounts = false;
    }

    private async void EpicGamesAccounts_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (isInitializingEpicGamesAccounts) return;

        // close epic games launcher
        EpicGamesHelper.CloseEpicGames();

        // update config
        if (File.Exists(EpicGamesHelper.ActiveEpicGamesAccountPath))
        {
            var (oldAccountId, _, _, _) = EpicGamesHelper.GetAccountData(EpicGamesHelper.ActiveEpicGamesAccountPath);

            string accountDir = Path.Combine(EpicGamesHelper.EpicGamesAccountDir, oldAccountId);
            if (Directory.Exists(accountDir))
                File.Copy(EpicGamesHelper.ActiveEpicGamesAccountPath, Path.Combine(accountDir, "GameUserSettings.ini"), true);
        }

        // get accountId
        string accountId = (EpicGamesAccounts.SelectedItem as ComboBoxItem)?.Tag as string;

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

        // refresh combobox
        isInitializingEpicGamesAccounts = true;
        LoadEpicGamesAccounts();
        await EpicGamesHelper.LoadGames();
        LoadSortSettings();
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
        isInitializingEpicGamesAccounts = true;
        LoadEpicGamesAccounts();
        await EpicGamesHelper.LoadGames();
        LoadSortSettings();

        // delay
        await Task.Delay(2000);

        // remove infobar
        AccountInfo.Children.Clear();
    }

    private async void AddEpicGamesAccount_Click(object sender, RoutedEventArgs e)
    {
        // add content dialog
        var contentDialog = new ContentDialog
        {
            Title = "Add Epic Games Account",
            Content = "Are you sure that you want to add an Epic Games account?",
            PrimaryButtonText = "Yes",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Close,
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
            isInitializingEpicGamesAccounts = true;
            LoadEpicGamesAccounts();
            await EpicGamesHelper.LoadGames();
            LoadSortSettings();

            // delay
            await Task.Delay(2000);

            // remove infobar
            AccountInfo.Children.Clear();
        }
    }

    private async void RemoveEpicGamesAccount_Click(object sender, RoutedEventArgs e)
    {
        // add content dialog
        var contentDialog = new ContentDialog
        {
            Title = "Remove Epic Games Account",
            Content = $"Are you sure that you want to remove {(EpicGamesAccounts.SelectedItem as ComboBoxItem).Content}?",
            PrimaryButtonText = "Yes",
            CloseButtonText = "No",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = this.XamlRoot,
        };
        ContentDialogResult result = await contentDialog.ShowAsync();

        // check results
        if (result == ContentDialogResult.Primary)
        {
            // add infobar
            AccountInfo.Children.Add(new InfoBar
            {
                Title = $"Removing {(EpicGamesAccounts.SelectedItem as ComboBoxItem).Content}...",
                IsClosable = false,
                IsOpen = true,
                Severity = InfoBarSeverity.Informational,
                Margin = new Thickness(5)
            });

            // close epic games launcher
            EpicGamesHelper.CloseEpicGames();

            // get accountId
            string accountId = EpicGamesHelper.GetAccountData(EpicGamesHelper.ActiveEpicGamesAccountPath).AccountId;

            // remove account
            File.Delete(EpicGamesHelper.ActiveEpicGamesAccountPath);
            Directory.Delete(Path.Combine(EpicGamesHelper.EpicGamesAccountDir, accountId), true);

            // delay
            await Task.Delay(500);

            // remove infobar
            AccountInfo.Children.Clear();

            // add infobar
            AccountInfo.Children.Add(new InfoBar
            {
                Title = $"Successfully removed {(EpicGamesAccounts.SelectedItem as ComboBoxItem).Content}.",
                IsClosable = false,
                IsOpen = true,
                Severity = InfoBarSeverity.Success,
                Margin = new Thickness(5)
            });

            // refresh combobox
            isInitializingEpicGamesAccounts = true;
            LoadEpicGamesAccounts();
            await EpicGamesHelper.LoadGames();
            LoadSortSettings();

            // delay
            await Task.Delay(2000);

            // remove infobar
            AccountInfo.Children.Clear();
        }
    }

    public void LoadSteamAccounts()
    {
        if (File.Exists(SteamHelper.SteamPath))
        {
            // show steam settings group
            Steam_SettingsGroup.Visibility = Visibility.Visible;

            // get all accounts
            var accounts = SteamHelper.GetSteamAccounts();

            // reset ui elements
            SteamAccounts.Items.Clear();
            SteamAccounts.IsEnabled = true;
            RemoveSteamAccountButton.IsEnabled = true;

            // add accounts to combobox
            if (accounts.Count == 0)
            {
                var notLoggedIn = new ComboBoxItem { Content = "Not logged in", IsEnabled = false };
                SteamAccounts.Items.Add(notLoggedIn);
                SteamAccounts.SelectedItem = notLoggedIn;
                SteamAccounts.IsEnabled = false;
                RemoveSteamAccountButton.IsEnabled = false;
            }
            else if (accounts.All(a => !a.MostRecent) || accounts.All(a => !a.AllowAutoLogin))
            {
                var notLoggedIn = new ComboBoxItem { Content = "Not logged in", IsEnabled = false };
                SteamAccounts.Items.Add(notLoggedIn);
                SteamAccounts.SelectedItem = notLoggedIn;
                RemoveSteamAccountButton.IsEnabled = false;

                foreach (var account in accounts)
                {
                    SteamAccounts.Items.Add(account.AccountName);
                }
            }
            else
            {
                foreach (var account in accounts)
                {
                    SteamAccounts.Items.Add(account.AccountName);
                }

                int selectedIndex = accounts.FindIndex(a => a.MostRecent);
                if (selectedIndex < 0)
                    selectedIndex = accounts.FindIndex(a => a.AllowAutoLogin);

                SteamAccounts.SelectedIndex = selectedIndex >= 0 ? selectedIndex : 0;
            }
        }

        isInitializingSteamAccounts = false;
    }

    private async void SteamAccounts_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (isInitializingSteamAccounts) return;

        // close steam
        SteamHelper.CloseSteam();

        // read file
        var kv = KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Deserialize(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(File.ReadAllText(SteamHelper.SteamLoginUsersPath))));

        // make all accounts inactive
        foreach (var user in kv.Children)
        {
            if (user["AccountName"]?.ToString() == SteamAccounts.SelectedItem.ToString())
            {
                user["MostRecent"] = "1";
                user["AllowAutoLogin"] = "1";
                user["Timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
            }
            else
            {
                user["MostRecent"] = "0";
                user["AllowAutoLogin"] = "0";
            }
        }

        // write changes
        using var msOut = new MemoryStream();
        KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Serialize(msOut, kv);
        msOut.Position = 0;
        File.WriteAllText(SteamHelper.SteamLoginUsersPath, new StreamReader(msOut).ReadToEnd());

        // update registry key
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "reg.exe",
                Arguments = @$"add ""HKEY_CURRENT_USER\Software\Valve\Steam"" /v AutoLoginUser /t REG_SZ /d {SteamAccounts.SelectedItem.ToString()} /f",
                CreateNoWindow = true,
                UseShellExecute = false
            }
        };
        process.Start();

        // refresh combobox
        isInitializingSteamAccounts = true;
        LoadSteamAccounts();
        await SteamHelper.LoadGames();
        LoadSortSettings();
    }

    private async void AddSteamAccount_Click(object sender, RoutedEventArgs e)
    {
        // add content dialog
        var contentDialog = new ContentDialog
        {
            Title = "Add Steam Account",
            Content = "Are you sure that you want to add a Steam account?",
            PrimaryButtonText = "Yes",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Close,
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
                Title = "Please log in to your Steam account...",
                IsClosable = false,
                IsOpen = true,
                Severity = InfoBarSeverity.Informational,
                Margin = new Thickness(5)
            });

            // close steam
            SteamHelper.CloseSteam();

            // read file
            var kv = KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Deserialize(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(File.ReadAllText(SteamHelper.SteamLoginUsersPath))));

            // make all accounts inactive
            foreach (var user in kv.Children)
            {
                user["MostRecent"] = "0";
                user["AllowAutoLogin"] = "0";
            }

            // write changes
            using var msOut = new MemoryStream();
            KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Serialize(msOut, kv);
            msOut.Position = 0;
            File.WriteAllText(SteamHelper.SteamLoginUsersPath, new StreamReader(msOut).ReadToEnd());

            // delay
            await Task.Delay(500);

            // get initial user count
            int initialUserCount = KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Deserialize(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(File.ReadAllText(SteamHelper.SteamLoginUsersPath)))).Children.Count();

            // launch steam
            Process.Start(SteamHelper.SteamPath);

            // check when logged in
            while (true)
            {
                if (KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Deserialize(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(File.ReadAllText(SteamHelper.SteamLoginUsersPath)))).Children.Count() > initialUserCount)
                    break;

                await Task.Delay(500);
            }

            // close steam
            SteamHelper.CloseSteam();

            // remove infobar
            AccountInfo.Children.Clear();

            // refresh combobox
            isInitializingSteamAccounts = true;
            LoadSteamAccounts();
            await SteamHelper.LoadGames();
            LoadSortSettings();

            // add infobar
            AccountInfo.Children.Add(new InfoBar
            {
                Title = $"Successfully logged in as {SteamAccounts.SelectedItem}",
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

    private async void RemoveSteamAccount_Click(object sender, RoutedEventArgs e)
    {
        // add content dialog
        var contentDialog = new ContentDialog
        {
            Title = "Remove Steam Account",
            Content = $"Are you sure that you want to remove {SteamAccounts.SelectedItem}?",
            PrimaryButtonText = "Yes",
            CloseButtonText = "No",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = this.XamlRoot,
        };
        ContentDialogResult result = await contentDialog.ShowAsync();

        // check results
        if (result == ContentDialogResult.Primary)
        {
            // add infobar
            AccountInfo.Children.Add(new InfoBar
            {
                Title = $"Removing {SteamAccounts.SelectedItem}...",
                IsClosable = false,
                IsOpen = true,
                Severity = InfoBarSeverity.Informational,
                Margin = new Thickness(5)
            });

            // delay
            await Task.Delay(500);

            // close steam
            SteamHelper.CloseSteam();

            // read file
            var kv = KVSerializer.Create(KVSerializationFormat.KeyValues1Text)
                                 .Deserialize(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(File.ReadAllText(SteamHelper.SteamLoginUsersPath))));
            // remove selected account
            var newChildren = kv.Children.Where(c => c != kv.Children.First(child => child.Value["AccountName"]?.ToString() == SteamAccounts.SelectedItem.ToString()));
            var newRoot = new KVObject(kv.Name, newChildren);

            // write changes
            using var msOut = new MemoryStream();
            KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Serialize(msOut, newRoot);
            msOut.Position = 0;
            File.WriteAllText(SteamHelper.SteamLoginUsersPath, new StreamReader(msOut).ReadToEnd());

            // remove infobar
            AccountInfo.Children.Clear();

            // add infobar
            AccountInfo.Children.Add(new InfoBar
            {
                Title = $"Successfully removed {SteamAccounts.SelectedItem}.",
                IsClosable = false,
                IsOpen = true,
                Severity = InfoBarSeverity.Success,
                Margin = new Thickness(5)
            });

            // refresh combobox
            isInitializingSteamAccounts = true;
            LoadSteamAccounts();
            await SteamHelper.LoadGames();
            LoadSortSettings();

            // delay
            await Task.Delay(2000);

            // remove infobar
            AccountInfo.Children.Clear();
        }
    }

    private void UpdateGridLayout()
    {
        bool epicVisible = EpicGames_SettingsGroup.Visibility == Visibility.Visible;
        bool steamVisible = Steam_SettingsGroup.Visibility == Visibility.Visible;

        if (epicVisible && steamVisible)
        {
            Grid.SetColumn(EpicGames_SettingsGroup, 0);
            Grid.SetColumnSpan(EpicGames_SettingsGroup, 1);
            Grid.SetColumn(Steam_SettingsGroup, 1);
            Grid.SetColumnSpan(Steam_SettingsGroup, 1);
        }
        else if (epicVisible)
        {
            Grid.SetColumn(EpicGames_SettingsGroup, 0);
            Grid.SetColumnSpan(EpicGames_SettingsGroup, 2);
        }
        else if (steamVisible)
        {
            Grid.SetColumn(Steam_SettingsGroup, 0);
            Grid.SetColumnSpan(Steam_SettingsGroup, 2);
        }
    }
}