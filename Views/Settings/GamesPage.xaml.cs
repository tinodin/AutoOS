using AutoOS.Views.Settings.Games;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace AutoOS.Views.Settings;

public sealed partial class GamesPage : Page
{
    public static GamesPage Instance { get; private set; }
    public GameGallery Games => games;
    private bool isInitializingAccounts = true;
    private readonly string configFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"EpicGamesLauncher\Saved\Config\Windows\GameUserSettings.ini");

    public GamesPage()
    {
        InitializeComponent();
        Instance = this;
        LoadGames();
        LoadEpicAccounts();
    }

    private static readonly HttpClient httpClient = new HttpClient();

    private async void LoadGames()
    {
        // add epic games games
        if (File.Exists(@"C:\Program Files (x86)\Epic Games\Launcher\Portal\Binaries\Win32\EpicGamesLauncher.exe"))
        {
            foreach (var file in Directory.GetFiles(@"C:\ProgramData\Epic\EpicGamesLauncher\Data\Manifests", "*.item", SearchOption.TopDirectoryOnly))
            {
                // read file
                var fileContent = await File.ReadAllTextAsync(file);
                var itemJson = JsonNode.Parse(fileContent);

                // check if an game or addon
                if (itemJson?["bIsApplication"]?.GetValue<bool>() == true)
                {
                    // get json data
                    string url = $"https://api.egdata.app/items/{itemJson["MainGameCatalogItemId"]?.GetValue<string>()}";

                    try
                    {
                        // read json data
                        string remoteJson = await httpClient.GetStringAsync(url);
                        var remoteData = JsonNode.Parse(remoteJson);

                        // search cover image
                        foreach (var image in remoteData?["keyImages"]?.AsArray())
                        {
                            if (image?["type"]?.GetValue<string>() == "DieselGameBoxTall")
                            {
                                string imageUrl = image["url"]?.GetValue<string>();

                                // add the game
                                var gamePanel = new GamePanel
                                {
                                    Launcher = "Epic Games",
                                    Title = remoteData?["title"].GetValue<string>(),
                                    Description = remoteData["developer"].GetValue<string>(),
                                    ImageSource = new BitmapImage(new Uri(imageUrl)),
                                    CatalogNamespace = itemJson["MainGameCatalogNamespace"]?.GetValue<string>(),
                                    CatalogItemId = itemJson["MainGameCatalogItemId"]?.GetValue<string>(),
                                    AppName = itemJson["MainGameAppName"]?.GetValue<string>(),
                                    InstallLocation = itemJson["InstallLocation"]?.GetValue<string>(),
                                    LaunchExecutable = itemJson["LaunchExecutable"]?.GetValue<string>()
                                };

                                ((StackPanel)games.HeaderContent).Children.Add(gamePanel);
                                await gamePanel.CheckGameRunning();

                                break;
                            }
                        }
                    }
                    catch
                    {
                        await Task.Delay(500);
                        break;
                    }
                }
            }
        }
        else
        {
            Games_StackPanel.Height = 0;
        }

        // add custom games
        try
        {
            foreach (var file in Directory.GetFiles(Path.Combine(PathHelper.GetAppDataFolderPath(), "Games"), "*.json"))
            {
                string json = File.ReadAllText(file);
                var game = JsonSerializer.Deserialize<Dictionary<string, string>>(json);

                var panel = new GamePanel
                {
                    Launcher = game["Launcher"],
                    LauncherLocation = game["LauncherLocation"],
                    DataLocation = game["DataLocation"],
                    GameLocation = game["GameLocation"],
                    Title = game["GameName"],
                    Description = game["GameDeveloper"],
                    ImageSource = new BitmapImage(new Uri(game["GameCoverUrl"]))
                };

                ((StackPanel)games.HeaderContent).Children.Add(panel);
                await panel.CheckGameRunning();
            }
        }
        catch
        {
            await Task.Delay(500);
        }

        // sort all entries
        var sorted = ((StackPanel)games.HeaderContent).Children
               .OfType<GamePanel>()
               .OrderBy(g => g.Title, StringComparer.CurrentCultureIgnoreCase)
               .ToList();

        ((StackPanel)games.HeaderContent).Children.Clear();

        foreach (var panel in sorted)
        {
            ((StackPanel)games.HeaderContent).Children.Add(panel);
        }

        Games_SwitchPresenter.HorizontalAlignment = HorizontalAlignment.Left;
        Games_SwitchPresenter.Value = false;
        Games_ProgressRing.IsActive = false;
    }

    private async void LoadEpicAccounts()
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

                    // check if data exists with old display name
                    string existingDir = Directory.GetDirectories(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"EpicGamesLauncher\Saved\Config\Windows"))
                                                  .FirstOrDefault(dir => Regex.Match(File.ReadAllText(Path.Combine(dir, "GameUserSettings.ini")), @"\[(.*?)_General\]").Groups[1].Value == accountId && !dir.Contains(displayName));

                    if (existingDir != null)
                    {
                        // rename folder
                        Directory.Move(existingDir, Path.Combine(Path.GetDirectoryName(existingDir), displayName));

                        // replace config
                        File.Copy(configFile, Path.Combine(Path.GetDirectoryName(existingDir), displayName, "GameUserSettings.ini"), true);
                    }
                    else
                    {
                        // update the backed up config
                        if (Directory.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"EpicGamesLauncher\Saved\Config\Windows\" + displayName)))
                        {
                            if (File.Exists(configFile))
                            {
                                if (Regex.Match(File.ReadAllText(configFile), @"\[(.*?)_General\]").Groups[1].Value == accountId)
                                {
                                    File.Copy(configFile, Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"EpicGamesLauncher\Saved\Config\Windows\" + displayName), "GameUserSettings.ini"), true);
                                }
                            }
                        }
                        else
                        {
                            // create folder
                            Directory.CreateDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"EpicGamesLauncher\Saved\Config\Windows\" + displayName));

                            // copy config
                            File.Copy(configFile, Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"EpicGamesLauncher\Saved\Config\Windows\" + displayName), "GameUserSettings.ini"), true);

                            // create reg file
                            File.WriteAllText(Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"EpicGamesLauncher\Saved\Config\Windows\" + displayName), "accountId.reg"), $"Windows Registry Editor Version 5.00\r\n\r\n[HKEY_CURRENT_USER\\Software\\Epic Games\\Unreal Engine\\Identifiers]\r\n\"AccountId\"=\"{accountId}\"");
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

        // launch epic games launcher
        await Task.Run(() => { var p = Process.Start(new ProcessStartInfo(@"C:\Program Files (x86)\Epic Games\Launcher\Portal\Binaries\Win64\EpicGamesLauncher.exe") { WindowStyle = ProcessWindowStyle.Hidden }); p?.WaitForInputIdle(); Thread.Sleep(6000); p?.Kill(); });

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
            // get accountid
            string accountId = Regex.Match(File.ReadAllText(configFile), @"\[(.*?)_General\]").Groups[1].Value;

            // add content dialog
            var contentDialog = new ContentDialog
            {
                Title = "Remove Account",
                Content = "Are you sure that you want to remove " + accountId + "?",
                PrimaryButtonText = "Yes",
                CloseButtonText = "No",
                XamlRoot = this.XamlRoot,
                DefaultButton = ContentDialogButton.Primary
            };
            ContentDialogResult result = await contentDialog.ShowAsync();

            // check results
            if (result == ContentDialogResult.Primary)
            {
                // add infobar
                AccountInfo.Children.Add(new InfoBar
                {
                    Title = "Removing " + accountId + "...",
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
                    Title = "Successfully removed " + accountId + ".",
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