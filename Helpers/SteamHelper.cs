using AutoOS.Views.Settings.Games;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.Win32;
using System.Diagnostics;
using System.Text.Json;
using System.Xml.Linq;
using ValveKeyValue;

namespace AutoOS.Helpers
{
    public static class SteamHelper
    {
        public static readonly string SteamDir = (Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam")?.GetValue("SteamPath") as string ?? @"C:\Program Files (x86)\Steam").Replace('/', '\\');
        public static readonly string SteamPath = Path.Combine(SteamDir, "steam.exe");
        public static readonly string SteamLibraryPath = Path.Combine(SteamDir, @"steamapps\libraryfolders.vdf");
        public static readonly string SteamLibraryCacheDir = Path.Combine(SteamDir, @"appcache\librarycache");
        public static readonly string SteamLoginUsersPath = Path.Combine(SteamDir, "config", "loginusers.vdf");

        private static readonly HttpClient httpClient = new();

        public class SteamAccountInfo
        {
            public string AccountName { get; set; }
            public bool MostRecent { get; set; }
            public bool AllowAutoLogin { get; set; }
        }

        public static List<SteamAccountInfo> GetSteamAccounts()
        {
            if (!File.Exists(SteamLoginUsersPath))
                return [];

            var kv = KVSerializer.Create(KVSerializationFormat.KeyValues1Text)
                                 .Deserialize(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(File.ReadAllText(SteamLoginUsersPath))));

            return [.. kv.Children
                .Select(c =>
                {
                    string steam64Id = c.Name.ToString();
                    string accountName = c["AccountName"]?.ToString();
                    bool mostRecent = c["MostRecent"]?.ToString() == "1";
                    bool allowAutoLogin = c["AllowAutoLogin"]?.ToString() == "1";

                    if (string.IsNullOrWhiteSpace(accountName))
                        return null;

                    if (string.IsNullOrWhiteSpace(steam64Id))
                        return null;

                    return new SteamAccountInfo
                    {
                        AccountName = accountName,
                        MostRecent = mostRecent,
                        AllowAutoLogin = allowAutoLogin
                    };
                })
                .Where(x => x != null)
                .OrderBy(x => x.AccountName, StringComparer.OrdinalIgnoreCase)];
        }

        public static string GetSteam64ID()
        {
            if (!File.Exists(SteamLoginUsersPath))
                return null;

            var kv = KVSerializer.Create(KVSerializationFormat.KeyValues1Text)
                                 .Deserialize(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(File.ReadAllText(SteamLoginUsersPath))));
            return kv.Children.FirstOrDefault(c => c["MostRecent"]?.ToString() == "1" && c["AllowAutoLogin"]?.ToString() == "1")?.Name;
        }

        public static void CloseSteam()
        {
            foreach (var name in new[] { "steam", "steamwebhelper" })
            {
                Process.GetProcessesByName(name).ToList().ForEach(p =>
                {
                    p.Kill();
                    p.WaitForExit();
                });
            }
        }

        public static async Task LoadGames()
        {
            // return if either steam or no games are installed
            if (!File.Exists(SteamPath) || !File.Exists(SteamLibraryPath)) return;

            // remove previous games
            foreach (var panel in ((StackPanel)GamesPage.Instance.Games.HeaderContent).Children.OfType<GamePanel>().Where(panel => panel.Launcher == "Steam").ToList())
                ((StackPanel)GamesPage.Instance.Games.HeaderContent).Children.Remove(panel);

            // read libraryfolders.vdf
            var libraryFolderData = KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Deserialize(File.OpenRead(SteamLibraryPath));

            // for each steam install path
            await Parallel.ForEachAsync(libraryFolderData.Children, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount * 2 }, async (folder, _) => 
            {
                string steamAppsDir = Path.Combine(folder["path"]?.ToString().Replace(@"\\", @"\"), "steamapps");

                // skip if no steamapps directory
                if (!Directory.Exists(steamAppsDir)) return;

                // get installed apps dictionary
                var appsNode = folder.Children.FirstOrDefault(c => c.Name == "apps");
                if (appsNode == null) return;

                foreach (var app in appsNode.Children.ToDictionary(x => int.Parse(x.Name), x => (long)x.Value))
                {
                    string gameId = app.Key.ToString();

                    // skip steam tools
                    if (gameId == "228980") continue;

                    try
                    {
                        // read game manifest
                        var appManifestData = KVSerializer.Create(KVSerializationFormat.KeyValues1Text)
                            .Deserialize(File.OpenRead(Path.Combine(steamAppsDir, $"appmanifest_{gameId}.acf")));

                        // get metadata
                        var gameData = JsonDocument.Parse(await httpClient.GetStringAsync($"https://store.steampowered.com/api/appdetails?appids={gameId}", _)).RootElement.GetProperty(gameId);

                        // get playtime data
                        var playTimeData = XDocument.Parse(await httpClient.GetStringAsync($"https://steamcommunity.com/profiles/{GetSteam64ID()}/games?tab=all&xml=1", _));

                        string playTime = playTimeData.Descendants("game")
                            .Where(game => (string)game.Element("appID") == gameId)
                            .Select(game =>
                            {
                                var hoursStr = (string)game.Element("hoursOnRecord");
                                return double.TryParse(hoursStr, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var h)
                                    ? $"{(int)h}h {(int)((h - (int)h) * 60)}min"
                                    : null;
                            })
                            .FirstOrDefault();

                        if (playTime != null)
                        {
                            // get review data
                            var reviewData = JsonDocument.Parse(await httpClient.GetStringAsync($"https://store.steampowered.com/appreviews/{gameId}?json=1", _)).RootElement.GetProperty("query_summary");
                            int totalPositive = reviewData.GetProperty("total_positive").GetInt32();
                            int totalNegative = reviewData.GetProperty("total_negative").GetInt32();

                            GamesPage.Instance.DispatcherQueue.TryEnqueue(() =>
                            {
                                var gamePanel = new GamePanel
                                {
                                    Launcher = "Steam",
                                    ImageTall = new BitmapImage(new Uri($"https://cdn.steamstatic.com/steam/apps/{gameId}/library_600x900.jpg")),
                                    ImageWide = new BitmapImage(new Uri($"https://cdn.steamstatic.com/steam/apps/{gameId}/library_hero.jpg")),
                                    Title = appManifestData["name"]?.ToString(),
                                    Developers = string.Join(", ", gameData.GetProperty("data").GetProperty("developers")
                                                       .EnumerateArray().Select(d => d.GetString()).Where(s => !string.IsNullOrWhiteSpace(s))),
                                    Genres = [.. gameData.GetProperty("data").GetProperty("genres")
                                        .EnumerateArray()
                                        .Select(g => g.GetProperty("description").GetString())
                                        .Where(s => !string.IsNullOrWhiteSpace(s))],
                                    Features = [.. gameData.GetProperty("data").GetProperty("categories")
                                        .EnumerateArray()
                                        .Select(c => c.GetProperty("description").GetString())
                                        .Where(s => !string.IsNullOrWhiteSpace(s))],
                                    Rating = totalPositive + totalNegative > 0
                                        ? Math.Round(5.0 * totalPositive / (totalPositive + totalNegative), 1)
                                        : 0.0,
                                    PlayTime = playTime,
                                    Description = gameData.GetProperty("data").GetProperty("short_description").GetString(),
                                    InstallLocation = Path.Combine(steamAppsDir, "common", appManifestData["installdir"]?.ToString()),
                                    GameID = gameId
                                };

                                ((StackPanel)GamesPage.Instance.Games.HeaderContent).Children.Add(gamePanel);
                                gamePanel.CheckGameRunning();
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex);
                    }
                }
            });
        }
    }
}