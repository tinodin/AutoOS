using Microsoft.UI.Xaml.Media.Imaging;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

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

    private async void LoadGames()
    {
        string manifestsFolder = @"C:\ProgramData\Epic\EpicGamesLauncher\Data\Manifests";
        var gameList = new List<(string ImageUrl, string Title, string Developer, string CatalogNamespace, string CatalogItemId, string AppName, string InstallLocation, string LaunchExecutable)>();

        foreach (var file in Directory.GetFiles(manifestsFolder, "*.item", SearchOption.TopDirectoryOnly))
        {
            var fileContent = await File.ReadAllTextAsync(file);
            var itemJson = JsonNode.Parse(fileContent);

            if (itemJson?["bIsApplication"]?.GetValue<bool>() == true)
            {
                string catalogNamespace = itemJson["MainGameCatalogNamespace"]?.GetValue<string>();
                string catalogItemId = itemJson["MainGameCatalogItemId"]?.GetValue<string>();
                string appName = itemJson["MainGameAppName"]?.GetValue<string>();
                string installLocation = itemJson["InstallLocation"]?.GetValue<string>();
                string launchExecutable = itemJson["LaunchExecutable"]?.GetValue<string>();

                if (!string.IsNullOrEmpty(catalogItemId))
                {
                    string url = $"https://api.egdata.app/items/{catalogItemId}";

                    try
                    {
                        string remoteJson = await httpClient.GetStringAsync(url);
                        var remoteData = JsonNode.Parse(remoteJson);

                        var keyImages = remoteData?["keyImages"]?.AsArray();
                        var title = remoteData?["title"]?.GetValue<string>() ?? "Unknown Title";
                        var developer = remoteData?["developer"]?.GetValue<string>() ?? "Unknown Developer";

                        if (keyImages != null)
                        {
                            foreach (var image in keyImages)
                            {
                                if (image?["type"]?.GetValue<string>() == "DieselGameBoxTall")
                                {
                                    string imageUrl = image["url"]?.GetValue<string>();

                                    if (!string.IsNullOrEmpty(imageUrl))
                                    {
                                        gameList.Add((imageUrl, title, developer, catalogNamespace, catalogItemId, appName, installLocation, launchExecutable));
                                    }
                                    break;
                                }
                            }
                        }
                    }
                    catch (HttpRequestException ex)
                    {

                    }
                }
            }
        }

        var sortedGames = gameList.OrderBy(g => g.Title).ToList();

        foreach (var game in sortedGames)
        {
            AddGameToStackPanel(game.ImageUrl, game.Title, game.Developer, game.CatalogNamespace, game.CatalogItemId, game.AppName, game.InstallLocation, game.LaunchExecutable);
        }

        Games_SwitchPresenter.HorizontalAlignment = HorizontalAlignment.Left;
        Games_SwitchPresenter.Value = false;
        Games_ProgressRing.IsActive = false;
    }

    private void AddGameToStackPanel(string imageUrl, string title, string developer, string catalogNamespace, string catalogItemId, string appName, string installLocation, string launchExecutable)
    {
        var gamePanel = new GamePanel
        {
            Title = title,
            Description = developer,
            ImageSource = new BitmapImage(new Uri(imageUrl)),
            CatalogNamespace = catalogNamespace,
            CatalogItemId = catalogItemId,
            AppName = appName,
            InstallLocation = installLocation,
            LaunchExecutable = launchExecutable
        };

        if (games.HeaderContent == null)
        {
            games.HeaderContent = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 0),
                Spacing = 10
            };
        }

        ((StackPanel)games.HeaderContent).Children.Add(gamePanel);
        gamePanel.CheckGameRunning();
    }


    private async void LoadEpicAccounts()
    {
        // clear list
        Accounts.Items.Clear();

        // get all configs
        List<string> accountList = new List<string>();

        foreach (var file in Directory.GetFiles(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"EpicGamesLauncher\Saved\Config\Windows"), "GameUserSettings.ini", SearchOption.AllDirectories))
        {
            // read config asynchronously
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

                // backup data
                string accountDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"EpicGamesLauncher\Saved\Config\Windows\" + displayName);

                // check if data exists with old display name
                string existingDir = Directory.GetDirectories(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"EpicGamesLauncher\Saved\Config\Windows"))
                                              .FirstOrDefault(dir => Regex.Match(File.ReadAllText(Path.Combine(dir, "GameUserSettings.ini")), @"\[(.*?)_General\]").Groups[1].Value == accountId
                                                                     && !dir.Contains(displayName));
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
                    if (Directory.Exists(accountDir))
                    {
                        if (File.Exists(configFile))
                        {
                            if (Regex.Match(File.ReadAllText(configFile), @"\[(.*?)_General\]").Groups[1].Value == accountId)
                            {
                                File.Copy(configFile, Path.Combine(accountDir, "GameUserSettings.ini"), true);
                            }
                        }
                    }
                    else
                    {
                        // create folder
                        Directory.CreateDirectory(accountDir);

                        // copy config
                        File.Copy(configFile, Path.Combine(accountDir, "GameUserSettings.ini"), true);
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
                foreach (var dir in Directory.GetDirectories(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"EpicGamesLauncher\Saved\Config\Windows")))
                {
                    string backupAccountId = Regex.Match(File.ReadAllText(Path.Combine(dir, "GameUserSettings.ini")), @"\[(.*?)_General\]").Groups[1].Value;

                    if (backupAccountId == Regex.Match(configContent, @"\[(.*?)_General\]").Groups[1].Value)
                    {
                        Directory.Delete(dir, true);
                    }

                    if (file == configFile)
                    {
                        File.Delete(configFile);
                    }
                }
            }
        }

        // sort accounts
        accountList.Sort();
        Accounts.ItemsSource = accountList;

        isInitializingAccounts = false;
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

    private void Accounts_SelectionChanged(object sender, SelectionChangedEventArgs e)
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
            Process.Start(@"C:\Program Files (x86)\Epic Games\Launcher\Portal\Binaries\Win64\EpicGamesLauncher.exe");

            // check when logged in
            while (true)
            {
                if (File.Exists(configFile))
                {
                    string configContent = File.ReadAllText(configFile);
                    Match dataMatch = Regex.Match(configContent, @"Data=([^\r\n]+)");

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

            // get new accountid
            string accountId = Regex.Match(File.ReadAllText(configFile), @"\[(.*?)_General\]").Groups[1].Value;

            // remove infobar
            AccountInfo.Children.Clear();

            // add infobar
            AccountInfo.Children.Add(new InfoBar
            {
                Title = $"Successfully logged in as {accountId}",
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
}