using Microsoft.UI.Xaml.Media;
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

        private const string itemOfferQuery = @"
        query searchStoreQuery(
          $allowCountries: String, $category: String, $comingSoon: Boolean, $count: Int, $country: String!,
          $keywords: String, $locale: String, $namespace: String, $itemNs: String, $sortBy: String,
          $sortDir: String, $start: Int, $tag: String, $releaseDate: String,
          $priceRange: String, $freeGame: Boolean, $onSale: Boolean,
          $effectiveDate: String
        ) {
          Catalog {
            searchStore(
              allowCountries: $allowCountries, category: $category, comingSoon: $comingSoon, count: $count,
              country: $country, keywords: $keywords, locale: $locale, namespace: $namespace,
              itemNs: $itemNs, sortBy: $sortBy, sortDir: $sortDir, releaseDate: $releaseDate,
              start: $start, tag: $tag, priceRange: $priceRange, freeGame: $freeGame, onSale: $onSale,
              effectiveDate: $effectiveDate
            ) {
              elements {
                id
              }
            }
          }
        }
        ";

        private const string ratingQuery = @"
        query getProductResult($sandboxId: String!, $locale: String!) {
            RatingsPolls {
                getProductResult(sandboxId: $sandboxId, locale: $locale) {
                    averageRating
                }
            }
        }";


        private const string tagQuery = @"
        query getCatalogOffer($sandboxId: String!, $offerId: String!) {
            Catalog {
                catalogOffer(namespace: $sandboxId, id: $offerId) {
                    tags {
                        id
                        name
                        groupName
                    }
                }
            }
        }";


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
                //foreach (var item in GamesPage.Instance.Games.Items.OfType<Views.Settings.Games.HeaderCarousel.HeaderCarouselItem>().Where(item => item.Launcher == "Epic Games").ToList())
                //    GamesPage.Instance.Games.Items.Remove(item);

                // get new login data
                var (AccountId, DisplayName, AccessToken, RefreshToken) = await Authenticate(GetAccountData(ActiveEpicGamesAccountPath).Token);
                loginClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AccessToken);

                // read old data
                var iniHelper = new InIHelper(ActiveEpicGamesAccountPath);
                string rememberMeData = iniHelper.ReadValue("Data", "RememberMe", 2048);

                // decrypt it
                string decryptedJson = Decrypt(rememberMeData);
                JsonArray jsonArray = JsonNode.Parse(decryptedJson).AsArray();

                // replace old display name and refresh token with new data
                jsonArray[0]["DisplayName"] = DisplayName;
                jsonArray[0]["Token"] = RefreshToken;

                // write changes
                var options = new JsonSerializerOptions
                {
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                };

                iniHelper.AddValue("Data", $"\"{Encrypt(jsonArray.ToJsonString(options))}\"", "RememberMe");
                new InIHelper(Path.Combine(EpicGamesAccountDir, AccountId, "GameUserSettings.ini")).AddValue("Data", $"\"{Encrypt(jsonArray.ToJsonString(options))}\"", "RememberMe");

                // get library data
                var recordData = new List<JsonNode>();
                string nextCursor = null;

                do
                {
                    var cursorParam = nextCursor is null ? "" : $"&cursor={nextCursor}";
                    var json = JsonNode.Parse(await loginClient.GetStringAsync($"https://library-service.live.use1a.on.epicgames.com/library/api/public/items?includeMetadata=true&platform=Windows{cursorParam}"));

                    recordData.AddRange(json?["records"]?.AsArray() ?? []);
                    nextCursor = json?["responseMetadata"]?["nextCursor"]?.GetValue<string>();

                } while (!string.IsNullOrEmpty(nextCursor));

                var libraryData = new JsonArray();
                foreach (var record in recordData)
                {
                    libraryData.Add(record.DeepClone());
                }

                // get playtime data
                var playTimeResponse = await loginClient.GetAsync($"https://library-service.live.use1a.on.epicgames.com/library/api/public/playtime/account/{AccountId}/all");

                if (playTimeResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    return;
                }

                var playTimeData = (JsonNode.Parse(await playTimeResponse.Content.ReadAsStringAsync()) as JsonArray)?.ToDictionary(
                    p => p["artifactId"]?.GetValue<string>(),
                    p => p["totalTime"]?.GetValue<int>() ?? 0
                );

                // get build data
                var buildResponse = await loginClient.GetAsync("https://launcher-public-service-prod06.ol.epicgames.com/launcher/api/public/assets/Windows?label=Live");

                if (buildResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    return;
                }

                var buildData = JsonNode.Parse(await buildResponse.Content.ReadAsStringAsync()) as JsonArray;

                // for each manifest
                await Parallel.ForEachAsync(Directory.GetFiles(EpicGamesMainfestDir, "*.item", SearchOption.TopDirectoryOnly), new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount * 2 }, async (file, _) =>
                {
                    try
                    {
                        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                        var token = cts.Token;

                        // read manifest
                        var itemJson = JsonNode.Parse(await File.ReadAllTextAsync(file, token).ConfigureAwait(false));

                        // return if not a game
                        if (itemJson?["bIsApplication"]?.GetValue<bool>() != true) return;

                        // return if not in library
                        if (!libraryData.Any(x => x?["catalogItemId"]?.ToString() == itemJson["MainGameCatalogItemId"]?.GetValue<string>()))
                            return;

                        // get offer id
                        var itemOfferTask = loginClient.PostAsync("https://graphql.epicgames.com/graphql", new StringContent(JsonSerializer.Serialize(new { query = itemOfferQuery, variables = new { allowCountries = "US", country = "US", locale = "en-US", count = 1, withPrice = true, withPromotions = true, sortBy = "releaseDate", sortDir = "DESC", @namespace = itemJson["MainGameCatalogNamespace"]?.GetValue<string>(), category = "games/edition/base" } }), Encoding.UTF8, "application/json"), token);
                        var itemOfferData = JsonNode.Parse(await (await itemOfferTask.ConfigureAwait(false)).Content.ReadAsStringAsync(token).ConfigureAwait(false));

                        string offerId;

                        if (itemOfferData?["data"]?["Catalog"]?["searchStore"]?["elements"] is JsonArray { Count: > 0 })
                        {
                            offerId = itemOfferData?["data"]?["Catalog"]?["searchStore"]?["elements"]?[0]?["id"]?.GetValue<string>();
                        }
                        else
                        {
                            itemOfferData = JsonNode.Parse(await httpClient.GetStringAsync($"https://api.egdata.app/items/{itemJson["MainGameCatalogItemId"]?.GetValue<string>()}/offer", token).ConfigureAwait(false));
                            offerId = itemOfferData?["id"]?.GetValue<string>();
                        }

                        // get metadata
                        var manifestTask = loginClient.GetStringAsync($"https://catalog-public-service-prod06.ol.epicgames.com/catalog/api/shared/namespace/{itemJson["MainGameCatalogNamespace"]?.GetValue<string>()}/bulk/items?id={itemJson["CatalogItemId"]?.GetValue<string>()}&includeDLCDetails=false&includeMainGameDetails=true&country=US&locale=en-US", token);
                        var offerTask = loginClient.GetStringAsync($"https://catalog-public-service-prod06.ol.epicgames.com/catalog/api/shared/bulk/offers?id={offerId}&returnItemDetails=true&country=US&locale=en-US", token);
                        var ratingTask = loginClient.PostAsync("https://graphql.epicgames.com/graphql", new StringContent(JsonSerializer.Serialize(new { query = ratingQuery, variables = new { sandboxId = itemJson["MainGameCatalogNamespace"]?.GetValue<string>(), locale = "en-US" } }), Encoding.UTF8, "application/json"), token);
                        var tagTask = loginClient.PostAsync("https://graphql.epicgames.com/graphql", new StringContent(JsonSerializer.Serialize(new { query = tagQuery, variables = new { sandboxId = itemJson["MainGameCatalogNamespace"]?.GetValue<string>(), offerId } }), Encoding.UTF8, "application/json"), token);

                        await Task.WhenAll(manifestTask, offerTask, ratingTask, tagTask).ConfigureAwait(false);

                        var manifestData = JsonNode.Parse(await manifestTask.ConfigureAwait(false));
                        var offerData = JsonNode.Parse(await offerTask.ConfigureAwait(false));
                        var ratingData = JsonNode.Parse(await (await ratingTask.ConfigureAwait(false)).Content.ReadAsStringAsync(token).ConfigureAwait(false));
                        var tagData = JsonNode.Parse(await (await tagTask.ConfigureAwait(false)).Content.ReadAsStringAsync(token).ConfigureAwait(false));

                        // get description
                        string description = offerData[offerId]?["description"]?.GetValue<string>();

                        if (offerData[offerId]?["offerType"]?.GetValue<string>() != "BASE_GAME")
                        {
                            description = manifestData[itemJson["MainGameCatalogItemId"]?.GetValue<string>()]?["description"]?.GetValue<string>();
                        }

                        // get key images
                        var keyImages = manifestData[itemJson["MainGameCatalogItemId"]?.GetValue<string>()]?["keyImages"]?.AsArray() ?? [];

                        // get artifactid
                        string artifactId = manifestData[itemJson["MainGameCatalogItemId"]?.GetValue<string>()]?["releaseInfo"]?[0]?["appId"]?.ToString();

                        // get playtime
                        var totalSeconds = playTimeData?.GetValueOrDefault(artifactId) ?? 0;

                        var ts = TimeSpan.FromSeconds(totalSeconds);
                        string playTime = ts.TotalHours >= 1
                            ? $"{(int)ts.TotalHours}h {ts.Minutes}m"
                            : $"{ts.Minutes}m";

                        // get latest version
                        string currentVersion = itemJson["AppVersionString"]?.GetValue<string>();
                        string latestVersion = buildData?.FirstOrDefault(x => x?["appName"]?.ToString() == itemJson["AppName"]?.GetValue<string>())?["buildVersion"]?.ToString();

                        GamesPage.Instance.DispatcherQueue.TryEnqueue(() =>
                        {
                            GamesPage.Instance.Games.Items.Add(new Views.Settings.Games.HeaderCarousel.HeaderCarouselItem
                            {
                                Launcher = "Epic Games",
                                CatalogNamespace = itemJson["MainGameCatalogNamespace"]?.GetValue<string>(),
                                CatalogItemId = itemJson["MainGameCatalogItemId"]?.GetValue<string>(),
                                AppName = itemJson["MainGameAppName"]?.GetValue<string>(),
                                InstallLocation = itemJson["InstallLocation"]?.GetValue<string>(),
                                LaunchExecutable = itemJson["LaunchExecutable"]?.GetValue<string>(),
                                UpdateIsAvailable = latestVersion != null && latestVersion != currentVersion,
                                ImageUrl = keyImages.FirstOrDefault(img => img?["type"]?.GetValue<string>() == "DieselGameBoxTall")?["url"]?.GetValue<string>(),
                                BackgroundImageUrl = keyImages.FirstOrDefault(img => img?["type"]?.GetValue<string>() == "DieselGameBox")?["url"]?.GetValue<string>(),
                                Title = offerData[offerId]?["title"]?.GetValue<string>(),
                                Developers = offerData[offerId]?["seller"]?["name"]?.GetValue<string>(),
                                Genres = tagData["data"]?["Catalog"]?["catalogOffer"]?["tags"]?.AsArray()
                                                .Where(t => t["groupName"]?.GetValue<string>() == "genre")
                                                .Select(t => t["name"].GetValue<string>())
                                                .ToList(),
                                Features = tagData["data"]?["Catalog"]?["catalogOffer"]?["tags"]?.AsArray()
                                                 .Where(t => t["groupName"]?.GetValue<string>() == "feature")
                                                 .Select(t => t["name"].GetValue<string>())
                                                 .ToList(),
                                Rating = ratingData?["data"]?["RatingsPolls"]?["getProductResult"]?["averageRating"]?.GetValue<double?>() ?? 0.0,
                                PlayTime = playTime,
                                Description = description,
                                Width = 240,
                                Height = 320,
                            });
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
