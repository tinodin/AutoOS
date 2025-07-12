using AutoOS.Views.Settings.Games;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Diagnostics;
using System.Net.Http.Headers;
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

        private static readonly HttpClient httpClient = new();
        private static readonly HttpClient loginClient = new();

        private const string ClientId = "34a02cf8f4414e29b15921876da36f9a";

        private const string ClientSecret = "daafbccc737745039dffe53d94fc76cf";

        private const string AesKey = "A09C853C9E95409BB94D707EADEFA52E";

        public class EpicAccountInfo
        {
            public string DisplayName { get; set; }
            public string AccountId { get; set; }
            public bool IsActive { get; set; }
        }

        public static List<EpicAccountInfo> GetEpicGamesAccounts()
        {
            List<EpicAccountInfo> accounts = [];

            if (!File.Exists(EpicGamesPath))
                return accounts;

            // get all configs
            foreach (var file in Directory.GetFiles(EpicGamesAccountDir, "GameUserSettings.ini", SearchOption.AllDirectories))
            {
                // check if data is valid
                if (!ValidateData(file))
                    continue;

                var (accountId, displayName, _, _) = GetAccountData(file);

                // update config if accountids match
                string accountDir = Path.Combine(EpicGamesAccountDir, accountId);
                if (Directory.Exists(accountDir))
                {
                    if (File.Exists(ActiveEpicGamesAccountPath) && file != ActiveEpicGamesAccountPath)
                    {
                        if (GetAccountData(ActiveEpicGamesAccountPath).AccountId == accountId)
                        {
                            File.Copy(ActiveEpicGamesAccountPath, Path.Combine(accountDir, "GameUserSettings.ini"), true);
                        }
                    }
                }
                // backup config if not already
                else
                {
                    // create folder
                    Directory.CreateDirectory(accountDir);

                    // copy config
                    File.Copy(file, Path.Combine(accountDir, "GameUserSettings.ini"), true);

                    // create reg file
                    File.WriteAllText(Path.Combine(accountDir, "accountId.reg"), $"Windows Registry Editor Version 5.00\r\n\r\n[HKEY_CURRENT_USER\\Software\\Epic Games\\Unreal Engine\\Identifiers]\r\n\"AccountId\"=\"{accountId}\"");
                }

                if (!accounts.Any(x => x.AccountId == accountId))
                {
                    accounts.Add(new EpicAccountInfo
                    {
                        DisplayName = displayName,
                        AccountId = accountId,
                        IsActive = file == ActiveEpicGamesAccountPath
                    });
                }
            }

            return [.. accounts.OrderBy(x => x.DisplayName, StringComparer.OrdinalIgnoreCase)];
        }

        public static async Task<(string AccountId, string DisplayName, string AccessToken, string RefreshToken)> Authenticate(string RefreshToken)
        {
            var client = new HttpClient();

            // set auth to client
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{ClientId}:{ClientSecret}")));

            // set data to refresh token
            var content = new FormUrlEncodedContent(
            [
                new KeyValuePair<string, string>("grant_type", "refresh_token"),
                new KeyValuePair<string, string>("refresh_token", RefreshToken),
                new KeyValuePair<string, string>("token_type", "eg1"),
            ]);

            //var response = await client.PostAsync("https://account-public-service-prod03.ol.epicgames.com/account/api/oauth/token", content);
            var response = await client.PostAsync("https://account-public-service-prod.ol.epicgames.com/account/api/oauth/token", content);

            if (!response.IsSuccessStatusCode)
                return (null, null, null, null);

            string responseBody = await response.Content.ReadAsStringAsync();
            Debug.WriteLine(responseBody);

            // return new data
            var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

            string accountId = json.RootElement.GetProperty("account_id").GetString();
            string displayName = json.RootElement.GetProperty("displayName").GetString();
            string accessToken = json.RootElement.GetProperty("access_token").GetString();
            string refreshToken = json.RootElement.GetProperty("refresh_token").GetString();

            return (accountId, displayName, accessToken, refreshToken);
        }

        //public static async Task<bool> UpdateEpicGamesToken(string file)
        //{
        //    // get new data
        //    var authResult = await Authenticate(GetAccountData(ActiveEpicGamesAccountPath).Token);
        //    if (authResult.RefreshToken == null)
        //        return false;

        //    try
        //    {
        //        // read old data
        //        var iniHelper = new InIHelper(file);
        //        string rememberMeData = iniHelper.ReadValue("Data", "RememberMe", 2048);

        //        Debug.WriteLine(rememberMeData);

        //        // decrypt it
        //        string decryptedJson = Decrypt(rememberMeData);
        //        JsonArray jsonArray = JsonNode.Parse(decryptedJson).AsArray();

        //        Debug.WriteLine(decryptedJson);

        //        // replace old display name and refresh token with new data
        //        jsonArray[0]["DisplayName"] = authResult.DisplayName;
        //        jsonArray[0]["Token"] = authResult.RefreshToken;

        //        // write changes
        //        var options = new JsonSerializerOptions
        //        {
        //            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        //        };

        //        Debug.WriteLine(jsonArray.ToJsonString(options));

        //        string encryptedJson = Encrypt(jsonArray.ToJsonString(options));

        //        Debug.WriteLine(encryptedJson);

        //        iniHelper.AddValue("Data", $"\"{encryptedJson}\"", "RememberMe");
        //        //iniHelper.AddValue("Data", encryptedJson, "RememberMe");

        //        return true;
        //    }
        //    catch
        //    {
        //        return false;
        //    }
        //}

        private static string Decrypt(string base64)
        {
            byte[] keyBytes = Encoding.UTF8.GetBytes(AesKey);
            using var aes = Aes.Create();
            aes.KeySize = 256;
            aes.Mode = CipherMode.ECB;
            aes.Padding = PaddingMode.PKCS7;
            aes.Key = keyBytes;

            byte[] cipher = Convert.FromBase64String(base64);
            using var ms = new MemoryStream(cipher);
            using var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read);
            using var sr = new StreamReader(cs);
            return sr.ReadToEnd().Replace("\0", "");
        }

        private static string Encrypt(string plainText)
        {
            byte[] keyBytes = Encoding.UTF8.GetBytes(AesKey);
            using var aes = Aes.Create();
            aes.KeySize = 256;
            aes.Mode = CipherMode.ECB;
            aes.Padding = PaddingMode.PKCS7;
            aes.Key = keyBytes;

            byte[] plain = Encoding.UTF8.GetBytes(plainText);
            using var ms = new MemoryStream();
            using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
            {
                cs.Write(plain, 0, plain.Length);
                cs.FlushFinalBlock();
            }
            return Convert.ToBase64String(ms.ToArray());
        }

        public static (string AccountId, string DisplayName, string Token, int TokenUseCount) GetAccountData(string file)
        {
            try
            {
                // read and decrypt offline and remember me data
                var iniHelper = new InIHelper(file);
                string decryptedOffline = Decrypt(iniHelper.ReadValue("Data", "Offline", 2048));
                string decryptedRememberMe = Decrypt(iniHelper.ReadValue("Data", "RememberMe", 2048));

                // get data from remember me
                var rememberMeRoot = JsonDocument.Parse(decryptedRememberMe).RootElement[0];
                string displayName = rememberMeRoot.GetProperty("DisplayName").GetString();
                string token = rememberMeRoot.GetProperty("Token").GetString();
                int tokenUseCount = rememberMeRoot.GetProperty("TokenUseCount").GetInt32();

                // get data from offline
                var offlineArray = JsonDocument.Parse(decryptedOffline).RootElement;
                string accountId = null;

                foreach (var account in offlineArray.EnumerateArray())
                {
                    if (account.TryGetProperty("DisplayName", out var nameProp) && nameProp.GetString() == displayName)
                    {
                        accountId = account.GetProperty("UserID").GetString();
                        break;
                    }
                }

                return (accountId, displayName, token, tokenUseCount);
            }
            catch
            {
                return (null, null, null, 0);
            }
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

            var iniHelper = new InIHelper(file);

            iniHelper.AddValue("MinimiseToSystemTray", "False", accountId + "_General");
        }

        public static void DisableNotifications(string file)
        {
            var (accountId, _, _, _) = GetAccountData(file);

            var iniHelper = new InIHelper(file);

            iniHelper.AddValue("NotificationsEnabled_FreeGame", "False", accountId + "_General");
            iniHelper.AddValue("NotificationsEnabled_Adverts", "False", accountId + "_General");
        }

        public static async Task LoadGames()
        {
            if (File.Exists(EpicGamesPath) && Directory.Exists(EpicGamesMainfestDir))
            {
                // remove previous games
                foreach (var panel in ((StackPanel)GamesPage.Instance.Games.HeaderContent).Children.OfType<GamePanel>().Where(panel => panel.Launcher == "Epic Games").ToList())
                    ((StackPanel)GamesPage.Instance.Games.HeaderContent).Children.Remove(panel);

                //// get new login data
                //var (AccountId, DisplayName, AccessToken, RefreshToken) = await Authenticate(GetAccountData(ActiveEpicGamesAccountPath).Token);
                //loginClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AccessToken);

                //// read old data
                //var iniHelper = new InIHelper(ActiveEpicGamesAccountPath);
                //string rememberMeData = iniHelper.ReadValue("Data", "RememberMe", 2048);

                //// decrypt it
                //string decryptedJson = Decrypt(rememberMeData);
                //JsonArray jsonArray = JsonNode.Parse(decryptedJson).AsArray();

                //// replace old display name and refresh token with new data
                //jsonArray[0]["DisplayName"] = DisplayName;
                //jsonArray[0]["Token"] = RefreshToken;

                //// write changes
                //var options = new JsonSerializerOptions
                //{
                //    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                //};

                //iniHelper.AddValue("Data", $"\"{Encrypt(jsonArray.ToJsonString(options))}\"", "RememberMe");
                //new InIHelper(Path.Combine(EpicGamesAccountDir, AccountId, "GameUserSettings.ini")).AddValue("Data", $"\"{Encrypt(jsonArray.ToJsonString(options))}\"", "RememberMe");

                //// get library items
                //var libraryResponse = await loginClient.GetAsync("https://launcher-public-service-prod06.ol.epicgames.com/launcher/api/public/assets/Windows?label=Live");

                //if (libraryResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                //{
                //    return;
                //}

                //var libraryData = await libraryResponse.Content.ReadAsStringAsync();

                //// get playtime data
                //var playTimeResponse = await loginClient.GetAsync($"https://library-service.live.use1a.on.epicgames.com/library/api/public/playtime/account/{GetAccountData(ActiveEpicGamesAccountPath).AccountId}/all");

                //if (playTimeResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                //{
                //    return;
                //}

                //var playTimeData = await playTimeResponse.Content.ReadAsStringAsync();

                // for each manifest
                await Parallel.ForEachAsync(Directory.GetFiles(EpicGamesMainfestDir, "*.item", SearchOption.TopDirectoryOnly), new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount * 2 }, async (file, _) =>
                {
                    try
                    {
                        // read manifest
                        var itemJson = JsonNode.Parse(await File.ReadAllTextAsync(file).ConfigureAwait(false));

                        // return if not a game
                        if (itemJson?["bIsApplication"]?.GetValue<bool>() != true) return;

                        //// return if not in library
                        //if (!libraryData.Contains(itemJson["MainGameCatalogItemId"]?.GetValue<string>()))
                        //{
                        //    return;
                        //}

                        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1.5));
                        var token = cts.Token;

                        // get offer id
                        var itemOfferData = JsonNode.Parse(await httpClient.GetStringAsync($"https://api.egdata.app/items/{itemJson["MainGameCatalogItemId"]?.GetValue<string>()}/offer", token).ConfigureAwait(false));
                        var offerId = itemOfferData?["id"]?.GetValue<string>();

                        // get metadata
                        var itemTask = httpClient.GetStringAsync($"https://api.egdata.app/items/{itemJson["MainGameCatalogItemId"]?.GetValue<string>()}", token);
                        var buildsTask = httpClient.GetStringAsync($"https://api.egdata.app/items/{itemJson["MainGameCatalogItemId"]?.GetValue<string>()}/builds", token);
                        var offerTask = httpClient.GetStringAsync($"https://api.egdata.app/offers/{offerId}", token);
                        var ratingTask = httpClient.GetStringAsync($"https://api.egdata.app/offers/{offerId}/polls", token);
                        var genresTask = httpClient.GetStringAsync($"https://api.egdata.app/offers/{offerId}/genres", token);
                        var featuresTask = httpClient.GetStringAsync($"https://api.egdata.app/offers/{offerId}/features", token);

                        await Task.WhenAll(offerTask, ratingTask, genresTask, featuresTask).ConfigureAwait(false);

                        var itemData = JsonNode.Parse(await itemTask.ConfigureAwait(false));
                        var buildsData = JsonNode.Parse(await buildsTask.ConfigureAwait(false));
                        var offerData = JsonNode.Parse(await offerTask.ConfigureAwait(false));
                        var ratingData = JsonNode.Parse(await ratingTask.ConfigureAwait(false));
                        var genresData = JsonNode.Parse(await genresTask.ConfigureAwait(false));
                        var featuresData = JsonNode.Parse(await featuresTask.ConfigureAwait(false));

                        // get images
                        var itemModified = DateTime.TryParse(itemData["lastModifiedDate"]?.GetValue<string>(), out var itemDate) ? itemDate : DateTime.MinValue;
                        var offerModified = DateTime.TryParse(offerData["lastModifiedDate"]?.GetValue<string>(), out var offerDate) ? offerDate : DateTime.MinValue;

                        string imageTallUrl, imageWideUrl;

                        if (itemModified > offerModified)
                        {
                            var itemImages = itemData["keyImages"]?.AsArray() ?? [];
                            imageTallUrl = itemImages.FirstOrDefault(img => img?["type"]?.GetValue<string>() == "DieselGameBoxTall")?["url"]?.GetValue<string>();
                            imageWideUrl = itemImages.FirstOrDefault(img => img?["type"]?.GetValue<string>() == "DieselGameBox")?["url"]?.GetValue<string>();
                        }
                        else
                        {
                            var offerImages = offerData["keyImages"]?.AsArray() ?? [];
                            imageTallUrl = offerImages.FirstOrDefault(img => img?["type"]?.GetValue<string>() == "OfferImageTall")?["url"]?.GetValue<string>();
                            imageWideUrl = offerImages.FirstOrDefault(img => img?["type"]?.GetValue<string>() == "OfferImageWide")?["url"]?.GetValue<string>();
                        }

                        // get artifactid
                        string artifactId = itemData?["releaseInfo"]?[0]?["appId"]?.ToString();

                        // read playtime json data
                        //var entries = JsonSerializer.Deserialize<List<Dictionary<string, JsonElement>>>(playTimeData);
                        //var match = entries?.FirstOrDefault(e => e["artifactId"].GetString() == artifactId);

                        //var totalSeconds = (match?.TryGetValue("totalTime", out var totalTimeElem) == true && totalTimeElem.TryGetInt32(out var seconds))
                        //    ? seconds
                        //    : 0;

                        //var ts = TimeSpan.FromSeconds(totalSeconds);
                        //string playTime = ts.TotalHours >= 1
                        //    ? $"{(int)ts.TotalHours}h {ts.Minutes}m"
                        //    : $"{ts.Minutes}m";

                        string playTime = "0m";

                        //// get latest build
                        //var buildResponse = await loginClient.GetAsync($"https://launcher-public-service-prod06.ol.epicgames.com/launcher/api/public/assets/v2/platform/Windows/namespace/{itemJson["MainGameCatalogNamespace"]?.GetValue<string>()}/catalogItem/{itemJson["CatalogItemId"]?.GetValue<string>()}/app/{itemJson["AppName"]?.GetValue<string>()}/label/Live", token);

                        //if (buildResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                        //{
                        //    return;
                        //}

                        //var buildData = await buildResponse.Content.ReadAsStringAsync(token);

                        string currentVersion = itemJson["AppVersionString"]?.GetValue<string>();
                        string latestVersion = itemJson["AppVersionString"]?.GetValue<string>();
                        //var latestVersion = JsonNode.Parse(buildData)?["elements"]?[0]?["buildVersion"]?.GetValue<string>();

                        GamesPage.Instance.DispatcherQueue.TryEnqueue(() =>
                        {
                            var gamePanel = new GamePanel
                            {
                                Launcher = "Epic Games",
                                CatalogNamespace = itemJson["MainGameCatalogNamespace"]?.GetValue<string>(),
                                CatalogItemId = itemJson["MainGameCatalogItemId"]?.GetValue<string>(),
                                AppName = itemJson["MainGameAppName"]?.GetValue<string>(),
                                InstallLocation = itemJson["InstallLocation"]?.GetValue<string>(),
                                LaunchExecutable = itemJson["LaunchExecutable"]?.GetValue<string>(),
                                UpdateIsAvailable = latestVersion != null && latestVersion != currentVersion,
                                ImageTall = new BitmapImage(new Uri(imageTallUrl)),
                                ImageWide = new BitmapImage(new Uri(imageWideUrl)),
                                Title = offerData["title"]?.GetValue<string>(),
                                Developers = offerData["seller"]?["name"]?.GetValue<string>(),
                                Genres = genresData?.AsArray()?.Select(g => g?["name"]?.GetValue<string>())
                                                    .Where(n => !string.IsNullOrWhiteSpace(n)).ToList() ?? [],
                                Features = featuresData?["features"]?.AsArray()?.Select(f => f?.GetValue<string>())
                                                      .Where(f => !string.IsNullOrWhiteSpace(f)).ToList() ?? [],
                                Rating = ratingData["averageRating"]?.GetValue<double?>() ?? 0.0,
                                PlayTime = playTime,
                                Description = offerData["description"]?.GetValue<string>(),
                            };

                            ((StackPanel)GamesPage.Instance.Games.HeaderContent).Children.Add(gamePanel);
                            gamePanel.CheckGameRunning();
                        });
                    }
                    catch (Exception ex)
                    {
                        GamesPage.Instance.DispatcherQueue.TryEnqueue(() =>
                        {
                            GamesPage.Instance.EgDataStatus.Fill = new SolidColorBrush((Windows.UI.Color)Application.Current.Resources["SystemFillColorCritical"]);
                        });
                        Debug.WriteLine(ex);
                    }
                });
            }
        }
    }
}