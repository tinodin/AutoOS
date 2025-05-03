using AutoOS.Views.Settings.Games;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Text.Json;
using ValveKeyValue;

namespace AutoOS.Helpers
{
    public static class SteamHelper
    {
        public const string SteamDir = @"C:\Program Files (x86)\Steam";
        public const string SteamPath = @"C:\Program Files (x86)\Steam\steam.exe";
        public const string SteamLibraryPath = @"C:\Program Files (x86)\Steam\steamapps\libraryfolders.vdf";
        public const string SteamLibraryCacheDir = @"C:\Program Files (x86)\Steam\appcache\librarycache";

        private static readonly HttpClient httpClient = new HttpClient();

        public static async Task LoadGames()
        {
            // return if either steam or no games are installed
            if (!File.Exists(SteamPath) || !File.Exists(SteamLibraryPath)) return;

            // read libraryfolders.vdf
            KVObject libraryFolderData = KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Deserialize(File.OpenRead(SteamLibraryPath));
            
            // for each steam install path
            foreach (var libraryFolderDataChild in libraryFolderData.Children)
            {
                string steamAppsDir = Path.Combine(libraryFolderDataChild["path"]?.ToString().Replace(@"\\", @"\"), "steamapps");

                // return if no games installed in directory
                if (!Directory.Exists(steamAppsDir)) continue;

                // for each game in install directory
                foreach (var app in libraryFolderDataChild.Children.FirstOrDefault(obj => obj.Name == "apps").Children.ToDictionary(x => int.Parse(x.Name), x => (long)x.Value))
                {
                    string gameId = app.Key.ToString();

                    // skip if steam tools
                    if (gameId == "228980")
                    {
                        continue;
                    }

                    // read game manifest
                    var appManifestData = KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Deserialize(File.OpenRead(Path.Combine(steamAppsDir, $"appmanifest_{gameId}.acf")));

                    string name = appManifestData["name"]?.ToString();
                    string installDir = Path.Combine(steamAppsDir, "common", appManifestData["installdir"]?.ToString());

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
                        ImageSource = new BitmapImage(new Uri(Path.Combine(SteamLibraryCacheDir, gameId, "library_600x900.jpg"))),
                        InstallLocation = installDir
                    };

                    ((StackPanel)GamesPage.Instance.Games.HeaderContent).Children.Add(gamePanel);

                    // start watcher
                    gamePanel.CheckGameRunning();
                }
            }
        }
    }
}