using AutoOS.Views.Settings.Games;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace AutoOS.Helpers
{
    public static class EpicGamesHelper
    {
        public const string EpicGamesPath = @"C:\Program Files (x86)\Epic Games\Launcher\Portal\Binaries\Win64\EpicGamesLauncher.exe";

        public static readonly string ActiveEpicGamesAccountPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"EpicGamesLauncher\Saved\Config\Windows", "GameUserSettings.ini");

        public static readonly string EpicGamesAccountDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"EpicGamesLauncher\Saved\Config\Windows");

        public const string EpicGamesInstalledGamesPath = @"C:\ProgramData\Epic\UnrealEngineLauncher\LauncherInstalled.dat";

        public const string EpicGamesMainfestDir = @"C:\ProgramData\Epic\EpicGamesLauncher\Data\Manifests";

        private static readonly HttpClient httpClient = new HttpClient();

        public static (string AccountId, string DisplayName, string Token, int TokenUseCount) GetAccountData(string file)
        {
            InIHelper iniHelper = new InIHelper(file);
            string offlineData = iniHelper.ReadValue("Data", "Offline", 2048);
            string rememberMeData = iniHelper.ReadValue("Data", "RememberMe", 2048);

            string key = "A09C853C9E95409BB94D707EADEFA52E";

            using (Aes aesAlgorithm = Aes.Create())
            {
                aesAlgorithm.KeySize = 256;
                aesAlgorithm.Mode = CipherMode.ECB;
                aesAlgorithm.Padding = PaddingMode.PKCS7;

                byte[] keyBytes = Encoding.UTF8.GetBytes(key);
                aesAlgorithm.Key = keyBytes;

                string Decrypt(string base64)
                {
                    byte[] cipher = Convert.FromBase64String(base64);
                    using var ms = new MemoryStream(cipher);
                    using var cs = new CryptoStream(ms, aesAlgorithm.CreateDecryptor(), CryptoStreamMode.Read);
                    using var sr = new StreamReader(cs);
                    return sr.ReadToEnd().Replace("\0", string.Empty);
                }

                string decryptedOffline = Decrypt(offlineData);
                var offlineDoc = JsonDocument.Parse(decryptedOffline);
                var offlineRoot = offlineDoc.RootElement[0];
                string accountId = offlineRoot.GetProperty("UserID").GetString();

                string decryptedRememberMe = Decrypt(rememberMeData);
                var rememberMeDoc = JsonDocument.Parse(decryptedRememberMe);
                var rememberMeRoot = rememberMeDoc.RootElement[0];
                string displayName = rememberMeRoot.GetProperty("DisplayName").GetString();
                string token = rememberMeRoot.GetProperty("Token").GetString();
                int tokenUseCount = rememberMeRoot.GetProperty("TokenUseCount").GetInt32();

                return (accountId, displayName, token, tokenUseCount);
            }
        }

        public static string SanitizeDisplayName(string file)
        {
            var (_, displayName, _, _) = GetAccountData(file);

            return new string(displayName
                .Where(c => !Path.GetInvalidFileNameChars().Contains(c))
                .ToArray())
                .TrimEnd('.', ' ');
        }

        public static bool ValidateData(string file)
        {
            var (_, _, token, _) = GetAccountData(file);

            return !string.IsNullOrWhiteSpace(token);
        }

        public static void CloseEpicGames()
        {
            foreach (var name in new[] { "EpicGamesLauncher", "EpicWebHelper" })
            {
                Process.GetProcessesByName(name).ToList().ForEach(p =>
                {
                    p.Kill();
                    p.WaitForExit();
                });
            }
        }

        public static void DisableMinimizeToTray(string file)
        {
            var (accountId, _, _, _) = GetAccountData(file);

            InIHelper iniHelper = new InIHelper(accountId + "_General");

            iniHelper.AddValue("MinimiseToSystemTray", "False");
            iniHelper.AddValue("NotificationsEnabled_FreeGame", "False");
            iniHelper.AddValue("NotificationsEnabled_Adverts", "False");
        }

        public static void DisableNotifications(string file)
        {
            var (accountId, _, _, _) = GetAccountData(file);

            InIHelper iniHelper = new InIHelper(accountId + "_General");

            iniHelper.AddValue("NotificationsEnabled_FreeGame", "False");
            iniHelper.AddValue("NotificationsEnabled_Adverts", "False");
        }

        public static async Task LoadGames()
        {
            if (File.Exists(EpicGamesHelper.EpicGamesPath))
            {
                // for each manifest
                foreach (var file in Directory.GetFiles(EpicGamesHelper.EpicGamesMainfestDir, "*.item", SearchOption.TopDirectoryOnly))
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
                            url = $"https://api.egdata.app/offers/d69e49517f0f4e49a39253f7b106dc27";

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

                                    ((StackPanel)GamesPage.Instance.Games.HeaderContent).Children.Add(gamePanel);

                                    // start watcher
                                    gamePanel.CheckGameRunning();

                                    break;
                                }
                            }
                        }
                        catch
                        {
                            return;
                        }
                    }
                }
            }
        }
    }
}