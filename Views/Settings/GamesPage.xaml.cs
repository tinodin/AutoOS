using AutoOS.Views.Settings.Games;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Windows.Storage;

namespace AutoOS.Views.Settings;

public sealed partial class GamesPage : Page
{
    private bool isInitializingAccounts = true;
    private readonly string configFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"EpicGamesLauncher\Saved\Config\Windows\GameUserSettings.ini");

    public GamesPage()
    {
        InitializeComponent();
        LoadGames();
        LoadEpicAccounts();
    }

    private static readonly HttpClient httpClient = new HttpClient();

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
            result = panels.OrderBy(g => g.Title ?? "", StringComparer.CurrentCultureIgnoreCase);
        }
        else
        {
            result = panels.OrderBy(g => g.Launcher ?? "", StringComparer.CurrentCultureIgnoreCase);
        }

        if (!ascending)
            result = result.Reverse();

        container.Children.Clear();
        foreach (var panel in result)
        {
            container.Children.Add(panel);
        }
    }

    private void SaveSortSettings()
    {
        ApplicationData.Current.LocalSettings.Values["SortKey"] = currentSortKey;
        ApplicationData.Current.LocalSettings.Values["SortAscending"] = ascending;
    }

    private void LoadSortSettings()
    {
        var savedSortKey = ApplicationData.Current.LocalSettings.Values["SortKey"];
        var savedAscending = ApplicationData.Current.LocalSettings.Values["SortAscending"];

        currentSortKey = savedSortKey as string ?? "Title";
        ascending = savedAscending is bool b ? b : true;

        UpdateSortIcons();
        ApplySort();
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

            ((StackPanel)games.HeaderContent).Children.Add(gamePanel);

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

    private async void LoadGames()
    {
        // epic games
        if (File.Exists(@"C:\Program Files (x86)\Epic Games\Launcher\Portal\Binaries\Win32\EpicGamesLauncher.exe"))
        {
            // for each manifest
            foreach (var file in Directory.GetFiles(@"C:\ProgramData\Epic\EpicGamesLauncher\Data\Manifests", "*.item", SearchOption.TopDirectoryOnly))
            {
                // read file
                var itemJson = JsonNode.Parse(await File.ReadAllTextAsync(file));

                // check if an game or addon
                if (itemJson?["bIsApplication"]?.GetValue<bool>() == true)
                {
                    // get json data
                    string url = $"https://api.egdata.app/items/{itemJson["MainGameCatalogItemId"]?.GetValue<string>()}";

                    // more up to date than the fortnite item id
                    if (itemJson["AppName"]?.GetValue<string>() == "Fortnite")
                    {
                        url = $"https://api.egdata.app/offers/d69e49517f0f4e49a39253f7b106dc27";
                    }

                    try
                    {
                        // read json data
                        var remoteData = JsonNode.Parse(await httpClient.GetStringAsync(url));

                        // search cover image
                        foreach (var image in remoteData?["keyImages"]?.AsArray())
                        {
                            if (image?["type"]?.GetValue<string>() == "DieselGameBoxTall")
                            {
                                // add game panel
                                var gamePanel = new GamePanel
                                {
                                    Launcher = "Epic Games",
                                    Title = remoteData?["title"].GetValue<string>(),
                                    Description = (remoteData["developer"]?.GetValue<string>()) ?? remoteData["developerDisplayName"]?.GetValue<string>(),
                                    ImageSource = new BitmapImage(new Uri(image["url"]?.GetValue<string>())),
                                    CatalogNamespace = itemJson["MainGameCatalogNamespace"]?.GetValue<string>(),
                                    CatalogItemId = itemJson["MainGameCatalogItemId"]?.GetValue<string>(),
                                    AppName = itemJson["MainGameAppName"]?.GetValue<string>(),
                                    InstallLocation = itemJson["InstallLocation"]?.GetValue<string>(),
                                    LaunchExecutable = itemJson["LaunchExecutable"]?.GetValue<string>()
                                };

                                ((StackPanel)games.HeaderContent).Children.Add(gamePanel);

                                // start watcher
                                gamePanel.CheckGameRunning();
                            }
                        }
                    }
                    catch
                    {
                        break;
                    }
                }
            }
        }

        // steam
        if (File.Exists(@"C:\Program Files (x86)\Steam\steam.exe"))
        {
            string steam = @"C:\Program Files (x86)\Steam";
            string vdf = Path.Combine(steam, @"steamapps\libraryfolders.vdf");
            if (!File.Exists(vdf)) return;

            var pathRegex = new Regex(@"""path""\s+""(.+?)""");
            var nameRegex = new Regex(@"""name""\s+""(.+?)""");
            var dirRegex = new Regex(@"""installdir""\s+""(.+?)""");

            foreach (var line in File.ReadAllLines(vdf))
            {
                var match = pathRegex.Match(line);
                if (!match.Success) continue;

                string lib = match.Groups[1].Value.Replace(@"\\", @"\");
                string steamapps = Path.Combine(lib, "steamapps");
                if (!Directory.Exists(steamapps)) continue;

                foreach (var manifest in Directory.GetFiles(steamapps, "appmanifest_*.acf"))
                {
                    string[] lines = File.ReadAllLines(manifest);
                    string name = lines.Select(l => nameRegex.Match(l)).FirstOrDefault(m => m.Success)?.Groups[1].Value;
                    string dir = lines.Select(l => dirRegex.Match(l)).FirstOrDefault(m => m.Success)?.Groups[1].Value;
                    if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(dir)) continue;

                    string gameId = Path.GetFileNameWithoutExtension(manifest).Replace("appmanifest_", "");

                    // skip steam tools
                    if (gameId == "228980")
                    {
                        continue;
                    }

                    // get developer
                    string developer = "Unknown";

                    try
                    {
                        var data = JsonDocument.Parse(await httpClient.GetStringAsync($"https://store.steampowered.com/api/appdetails?appids={gameId}")).RootElement.GetProperty(gameId);
                        if (data.GetProperty("success").GetBoolean())
                        {
                            developer = string.Join(", ", data.GetProperty("data").GetProperty("developers").EnumerateArray().Select(d => d.GetString()).Where(s => !string.IsNullOrWhiteSpace(s)));
                        }
                    }
                    catch
                    {
                        break;
                    }

                    // add game panel
                    var gamePanel = new GamePanel
                    {
                        Launcher = "Steam",
                        Title = name,
                        Description = developer,
                        GameID = gameId,
                        ImageSource = new BitmapImage(new Uri(Path.Combine(steam, $@"appcache\librarycache\{gameId}\library_600x900.jpg"))),
                        InstallLocation = Path.Combine(lib, "steamapps", "common", dir),
                    };

                    ((StackPanel)games.HeaderContent).Children.Add(gamePanel);

                    // start watcher
                    gamePanel.CheckGameRunning();
                }
            }
        }

        // add custom games
        if (Directory.Exists(Path.Combine(PathHelper.GetAppDataFolderPath(), "Games")))
        {
            foreach (var file in Directory.GetFiles(Path.Combine(PathHelper.GetAppDataFolderPath(), "Games"), "*.json"))
            {
                // read game json
                var game = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(file));

                // add game panel
                var gamePanel = new GamePanel
                {
                    Launcher = game["Launcher"],
                    LauncherLocation = game["LauncherLocation"],
                    DataLocation = game["DataLocation"],
                    InstallLocation = game["GameLocation"],
                    Title = game["GameName"],
                    Description = game["GameDeveloper"],
                    ImageSource = new BitmapImage(new Uri(game["GameCoverUrl"]))
                };

                ((StackPanel)games.HeaderContent).Children.Add(gamePanel);

                // start watcher
                gamePanel.CheckGameRunning();
            }
        }

        // sort games
        LoadSortSettings();

        // align games left
        Games_SwitchPresenter.HorizontalAlignment = HorizontalAlignment.Left;
        
        // show game gallery
        Games_SwitchPresenter.Value = false;
    }

    public async void LoadEpicAccounts()
    {
        // clear list
        Accounts.ItemsSource = null;

        // get all configs
        List<string> accountList = new List<string>();

        if (File.Exists(@"C:\Program Files (x86)\Epic Games\Launcher\Portal\Binaries\Win32\EpicGamesLauncher.exe"))
        {
            foreach (var file in Directory.GetFiles(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"EpicGamesLauncher\Saved\Config\Windows"), "GameUserSettings.ini", SearchOption.AllDirectories))
            {
                // read config
                string configContent = await File.ReadAllTextAsync(file);

                // check if data is valid
                Match dataMatch = Regex.Match(configContent, @"Data=([^\r\n]+)");
                if (dataMatch.Groups[1].Value.Length >= 1000)
                {
                    // get account id
                    string accountId = Regex.Match(configContent, @"\[(.*?)_General\]").Groups[1].Value;

                    // get data
                    string data = Regex.Match(configContent, @"\[RememberMe\][^\[]*?Data=""?([^\r\n""]+)""?").Groups[1].Value;

                    // decrypt data
                    string decryptedData = DecryptDataWithAes(data, "A09C853C9E95409BB94D707EADEFA52E");

                    // get display name
                    string displayName = Regex.Match(decryptedData, "\"DisplayName\":\"([^\"]+)\"").Groups[1].Value;

                    // sanitize display name for folder usage
                    string sanitizedDisplayName = new string(displayName.Where(c => !Path.GetInvalidFileNameChars().Contains(c)).ToArray()).TrimEnd('.', ' ');

                    string configPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"EpicGamesLauncher\Saved\Config\Windows");

                    // check if data exists with old display name
                    string existingDir = Directory.GetDirectories(configPath)
                                                  .FirstOrDefault(dir =>
                                                  {
                                                      string filePath = Path.Combine(dir, "GameUserSettings.ini");
                                                      return File.Exists(filePath) &&
                                                          Regex.Match(File.ReadAllText(filePath), @"\[(.*?)_General\]").Groups[1].Value == accountId &&
                                                          !dir.EndsWith(sanitizedDisplayName);
                                                  });

                    if (existingDir != null)
                    {
                        // rename folder
                        Directory.Move(existingDir, Path.Combine(Path.GetDirectoryName(existingDir), sanitizedDisplayName));

                        // replace config
                        File.Copy(configFile, Path.Combine(Path.GetDirectoryName(existingDir), sanitizedDisplayName, "GameUserSettings.ini"), true);
                    }
                    else
                    {
                        string newDir = Path.Combine(configPath, sanitizedDisplayName);

                        if (Directory.Exists(newDir))
                        {
                            if (File.Exists(configFile))
                            {
                                if (Regex.Match(File.ReadAllText(configFile), @"\[(.*?)_General\]").Groups[1].Value == accountId)
                                {
                                    File.Copy(configFile, Path.Combine(newDir, "GameUserSettings.ini"), true);
                                }
                            }
                        }
                        else
                        {
                            // create folder
                            Directory.CreateDirectory(newDir);

                            // copy config
                            File.Copy(configFile, Path.Combine(newDir, "GameUserSettings.ini"), true);

                            // create reg file
                            File.WriteAllText(Path.Combine(newDir, "accountId.reg"), $"Windows Registry Editor Version 5.00\r\n\r\n[HKEY_CURRENT_USER\\Software\\Epic Games\\Unreal Engine\\Identifiers]\r\n\"AccountId\"=\"{accountId}\"");
                        }
                    }

                    // add account to list
                    if (!accountList.Contains(displayName))
                    {
                        accountList.Add(displayName);
                    }

                    // select active account
                    if (file == configFile)
                    {
                        Accounts.SelectedItem = displayName;
                    }
                }
                else
                {
                    //foreach (var dir in Directory.GetDirectories(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"EpicGamesLauncher\Saved\Config\Windows")))
                    //{
                    //    string backupAccountId = Regex.Match(File.ReadAllText(Path.Combine(dir, "GameUserSettings.ini")), @"\[(.*?)_General\]").Groups[1].Value;

                    //    if (backupAccountId == Regex.Match(configContent, @"\[(.*?)_General\]").Groups[1].Value)
                    //    {
                    //        Directory.Delete(dir, true);
                    //    }

                    //    if (file == configFile)
                    //    {
                    //        File.Delete(configFile);
                    //    }
                    //}
                }
            }
        }

        // sort accounts
        accountList.Sort();
        Accounts.ItemsSource = accountList;

        isInitializingAccounts = false;
    }

    private async void Accounts_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (isInitializingAccounts) return;

        // close epic games launcher
        if (Process.GetProcessesByName("EpicGamesLauncher").Length > 0)
        {
            foreach (var process in Process.GetProcessesByName("EpicGamesLauncher"))
            {
                process.Kill();
                process.WaitForExit();
            }
        }

        // replace file
        File.Copy(Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"EpicGamesLauncher\Saved\Config\Windows\", Accounts.SelectedItem.ToString()), "GameUserSettings.ini"), configFile, true);

        // replace accountId in registry
        Process.Start("regedit.exe", $"/s \"{Path.Combine(Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"EpicGamesLauncher\Saved\Config\Windows\", Accounts.SelectedItem.ToString()), "accountId.reg"))}\"");

        // get current data
        string rawData = Regex.Match(await File.ReadAllTextAsync(configFile), @"\[RememberMe\][^\[]*?Data=""?([^\r\n""]+)""?").Groups[1].Value;
        string decrypted = DecryptDataWithAes(rawData, "A09C853C9E95409BB94D707EADEFA52E");

        await Task.Run(() => Process.Start(new ProcessStartInfo(@"C:\Program Files (x86)\Epic Games\Launcher\Portal\Binaries\Win64\EpicGamesLauncher.exe") { WindowStyle = ProcessWindowStyle.Hidden }));

        string lastRaw = rawData;

        while (true)
        {
            await Task.Delay(100);
            string newRaw = Regex.Match(await File.ReadAllTextAsync(configFile), @"\[RememberMe\][^\[]*?Data=""?([^\r\n""]+)""?").Groups[1].Value;

            if (newRaw == lastRaw) continue;
            lastRaw = newRaw;

            string decryptedNew = DecryptDataWithAes(newRaw, "A09C853C9E95409BB94D707EADEFA52E");
            if (decryptedNew.Length < 1000)
            {
                UpdateInvalidEpicGamesToken();
                return;
            }

            string cleaned = decryptedNew.Trim().TrimEnd('\0');
            using var doc = JsonDocument.Parse(cleaned);
            int tokenUse = doc.RootElement[0].GetProperty("TokenUseCount").GetInt32();

            if (tokenUse == 1) break;
        }

        while (true)
        {
            await Task.Delay(100);
            string newRaw = Regex.Match(await File.ReadAllTextAsync(configFile), @"\[RememberMe\][^\[]*?Data=""?([^\r\n""]+)""?").Groups[1].Value;

            if (newRaw == lastRaw) continue;
            lastRaw = newRaw;

            string decryptedNew = DecryptDataWithAes(newRaw, "A09C853C9E95409BB94D707EADEFA52E");
            if (decryptedNew.Length < 1000)
            {
                UpdateInvalidEpicGamesToken();
                return;
            }

            string cleaned = decryptedNew.Trim().TrimEnd('\0');
            using var doc = JsonDocument.Parse(cleaned);
            int tokenUse = doc.RootElement[0].GetProperty("TokenUseCount").GetInt32();

            if (tokenUse == 0) break;
        }

        Process.GetProcessesByName("EpicGamesLauncher").ToList().ForEach(p =>
        {
            p.Kill();
            p.WaitForExit();
        });

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
            Title = "The login token is no longer valid. Please reenter your password...",
            IsClosable = false,
            IsOpen = true,
            Severity = InfoBarSeverity.Error,
            Margin = new Thickness(5)
        });

        // close epic games launcher
        if (Process.GetProcessesByName("EpicGamesLauncher").Length > 0)
        {
            foreach (var process in Process.GetProcessesByName("EpicGamesLauncher"))
            {
                process.Kill();
                process.WaitForExit();
            }
        }

        // delay
        await Task.Delay(500);

        // launch epic games launcher
        Process.Start(@"C:\Program Files (x86)\Epic Games\Launcher\Portal\Binaries\Win32\EpicGamesLauncher.exe");

        // check when logged in
        while (true)
        {
            if (File.Exists(configFile))
            {
                Match dataMatch = Regex.Match(File.ReadAllText(configFile), @"Data=([^\r\n]+)");

                if (dataMatch.Success && dataMatch.Groups[1].Value.Length >= 1000)
                {
                    break;
                }
            }

            await Task.Delay(500);
        }

        // close epic games launcher
        if (Process.GetProcessesByName("EpicGamesLauncher").Length > 0)
        {
            foreach (var process in Process.GetProcessesByName("EpicGamesLauncher"))
            {
                process.Kill();
                process.WaitForExit();
            }
        }

        // disable tray and notifications
        string generalSection = Regex.Match(await File.ReadAllTextAsync(configFile), @"\[(.*?)_General\]").Groups[1].Value + "_General";

        InIHelper iniHelper = new InIHelper(configFile);

        iniHelper.AddValue("MinimiseToSystemTray", "False", generalSection);
        iniHelper.AddValue("NotificationsEnabled_FreeGame", "False", generalSection);
        iniHelper.AddValue("NotificationsEnabled_Adverts", "False", generalSection);

        // get data
        string data = Regex.Match(await File.ReadAllTextAsync(configFile), @"\[RememberMe\][^\[]*?Data=""?([^\r\n""]+)""?").Groups[1].Value;

        // decrypt data
        string decryptedData = DecryptDataWithAes(data, "A09C853C9E95409BB94D707EADEFA52E");

        // get display name
        string displayName = Regex.Match(decryptedData, "\"DisplayName\":\"([^\"]+)\"").Groups[1].Value;

        // remove infobar
        AccountInfo.Children.Clear();

        // add infobar
        AccountInfo.Children.Add(new InfoBar
        {
            Title = $"Successfully logged in as {displayName}",
            IsClosable = false,
            IsOpen = true,
            Severity = InfoBarSeverity.Success,
            Margin = new Thickness(5)
        });

        // refresh combobox
        isInitializingAccounts = true;
        LoadEpicAccounts();
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
            if (Process.GetProcessesByName("EpicGamesLauncher").Length > 0)
            {
                foreach (var process in Process.GetProcessesByName("EpicGamesLauncher"))
                {
                    process.Kill();
                    process.WaitForExit();
                }
            }

            // delete gameusersettings.ini
            if (File.Exists(configFile))
            {
                File.Delete(configFile);
            }

            // delay
            await Task.Delay(500);

            // launch epic games launcher
            Process.Start(@"C:\Program Files (x86)\Epic Games\Launcher\Portal\Binaries\Win32\EpicGamesLauncher.exe");

            // check when logged in
            while (true)
            {
                if (File.Exists(configFile))
                {
                    Match dataMatch = Regex.Match(File.ReadAllText(configFile), @"Data=([^\r\n]+)");

                    if (dataMatch.Success && dataMatch.Groups[1].Value.Length >= 1000)
                    {
                        break;
                    }
                }

                await Task.Delay(500);
            }

            // close epic games launcher
            if (Process.GetProcessesByName("EpicGamesLauncher").Length > 0)
            {
                foreach (var process in Process.GetProcessesByName("EpicGamesLauncher"))
                {
                    process.Kill();
                    process.WaitForExit();
                }
            }

            // disable tray and notifications
            string generalSection = Regex.Match(await File.ReadAllTextAsync(configFile), @"\[(.*?)_General\]").Groups[1].Value + "_General";

            InIHelper iniHelper = new InIHelper(configFile);

            iniHelper.AddValue("MinimiseToSystemTray", "False", generalSection);
            iniHelper.AddValue("NotificationsEnabled_FreeGame", "False", generalSection);
            iniHelper.AddValue("NotificationsEnabled_Adverts", "False", generalSection);

            // get data
            string data = Regex.Match(await File.ReadAllTextAsync(configFile), @"\[RememberMe\][^\[]*?Data=""?([^\r\n""]+)""?").Groups[1].Value;

            // decrypt data
            string decryptedData = DecryptDataWithAes(data, "A09C853C9E95409BB94D707EADEFA52E");

            // get display name
            string displayName = Regex.Match(decryptedData, "\"DisplayName\":\"([^\"]+)\"").Groups[1].Value;

            // remove infobar
            AccountInfo.Children.Clear();

            // add infobar
            AccountInfo.Children.Add(new InfoBar
            {
                Title = $"Successfully logged in as {displayName}",
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
                if (Process.GetProcessesByName("EpicGamesLauncher").Length > 0)
                {
                    foreach (var process in Process.GetProcessesByName("EpicGamesLauncher"))
                    {
                        process.Kill();
                        process.WaitForExit();
                    }
                }

                // delete gameusersettings.ini
                File.Delete(configFile);

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

    private static string DecryptDataWithAes(string cipherText, string key)
    {
        using (Aes aesAlgorithm = Aes.Create())
        {
            aesAlgorithm.KeySize = 256;
            aesAlgorithm.Mode = CipherMode.ECB;
            aesAlgorithm.Padding = PaddingMode.PKCS7;

            byte[] keyBytes = Encoding.UTF8.GetBytes(key);
            aesAlgorithm.Key = keyBytes;

            byte[] cipher = Convert.FromBase64String(cipherText);

            ICryptoTransform decryptor = aesAlgorithm.CreateDecryptor();

            using (MemoryStream ms = new MemoryStream(cipher))
            using (CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
            using (StreamReader sr = new StreamReader(cs))
            {
                return sr.ReadToEnd();
            }
        }
    }
}