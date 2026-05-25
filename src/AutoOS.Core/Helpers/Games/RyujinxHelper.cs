using DevWinUI;
using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace AutoOS.Core.Helpers.Games;

public static partial class RyujinxHelper
{
    private static readonly HttpClient httpClient = new();

    public static async Task<List<GameModel>> GetGames(string exePath, string dataPath)
    {
        var games = new ConcurrentBag<GameModel>();

        // if paths defined
        if (!string.IsNullOrEmpty(exePath) && !string.IsNullOrEmpty(dataPath) && File.Exists(exePath) && Directory.Exists(dataPath))
        {
            // download switch game catalog
            string filePath = Path.Combine(PathHelper.GetAppDataFolderPath(), "Switch", "US.en.json");

            if (!File.Exists(filePath))
            {
                var content = await httpClient.GetStringAsync("https://raw.githubusercontent.com/blawar/titledb/refs/heads/master/US.en.json");
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                await File.WriteAllTextAsync(filePath, content);
            }

            // get game dirs
            using var stream = File.OpenRead(Path.Combine(dataPath, "Config.json"));
            using var configDoc = await JsonDocument.ParseAsync(stream);
            var config = configDoc.RootElement;

            var gameDirs = new List<string>();
            if (config.TryGetProperty("game_dirs", out var dirs) && dirs.ValueKind == JsonValueKind.Array)
                foreach (var dir in dirs.EnumerateArray())
                    gameDirs.Add(dir.GetString() ?? "");

            // read json database
            using var fs = File.OpenRead(Path.Combine(PathHelper.GetAppDataFolderPath(), "Ryujinx", "US.en.json"));
            using var doc = await JsonDocument.ParseAsync(fs);

            var jsonById = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);

            foreach (var kvp in doc.RootElement.EnumerateObject())
            {
                if (kvp.Value.TryGetProperty("id", out var idElem))
                {
                    var key = idElem.GetString()?.ToLowerInvariant();
                    if (!string.IsNullOrEmpty(key))
                    {
                        jsonById.TryAdd(key, kvp.Value);
                    }
                }
            }

            // get all roms in game dirs
            var candidatesPerDir = new Dictionary<string, List<string>>();
            var validExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".nsp", ".xci" };

            foreach (var gameDir in gameDirs)
            {
                if (!Directory.Exists(gameDir)) continue;

                var matches = Directory.EnumerateFiles(gameDir)
                                       .Where(f => validExtensions.Contains(Path.GetExtension(f)));

                candidatesPerDir[gameDir] = [.. matches];
            }

            await Parallel.ForEachAsync(Directory.GetDirectories(Path.Combine(dataPath, "games")), new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount * 2 }, (Func<string, CancellationToken, ValueTask>)(async (folder, _) =>
            {
                // check if game exists in database
                if (!jsonById.TryGetValue(Path.GetFileName(folder).Trim().ToLowerInvariant(), out var entry))
                    return;

                // get name from database
                string name = entry.TryGetProperty("name", out var nameElement) ? nameElement.GetString() : null;

                // clean name for searching
                if (string.IsNullOrWhiteSpace(name)) return;
                string cleanName = CleanNameRegex().Replace(name.Replace('’', '\''), "");

                // make name simple to find install location
                string simpleCleanName = SimpleCleanNameRegex().Replace(cleanName.ToLowerInvariant(), "");

                // find install location
                string bestInstallLocation = null;

                foreach (var gameDir in candidatesPerDir.Keys)
                {
                    var candidates = candidatesPerDir[gameDir];
                    bestInstallLocation = candidates.FirstOrDefault((Func<string, bool>)(candidate =>
                    {
                        var simpleFileName = SimpleCleanNameRegex().Replace(Path.GetFileNameWithoutExtension(candidate).ToLowerInvariant(), "");
                        return simpleFileName.StartsWith(simpleCleanName, StringComparison.Ordinal);
                    }))?.Replace("/", "\\");
                    if (bestInstallLocation != null)
                        break;
                }

                if (bestInstallLocation == null)
                    return;

                // search on igdb
                var result = await IgdbHelper.SearchCovers(cleanName);
                if (result == null) return;

                // get playtime
                string metadataPath = Path.Combine(folder, "gui", "metadata.json");
                if (!File.Exists(metadataPath)) return;

                var metadataText = await File.ReadAllTextAsync(metadataPath);
                using var metadataDoc = JsonDocument.Parse(metadataText);
                var metadataObj = metadataDoc.RootElement;

                string playTime = "0m";
                if (metadataObj.TryGetProperty("timespan_played", out var timespanElement) && TimeSpan.TryParse(timespanElement.GetString(), out TimeSpan ts))
                {
                    playTime = GameModel.FormatPlayTime((int)ts.TotalMinutes);
                }

                // get size
                long sizeBytes = new FileInfo(bestInstallLocation).Length;

                using var docData = JsonDocument.Parse(await httpClient.GetStringAsync(result["game_url"], _));
                var data = docData.RootElement.Clone();

                games.Add(new GameModel
                {
                    Launcher = "Ryujinx",
                    LauncherLocation = exePath,
                    DataLocation = dataPath,
                    GameLocation = bestInstallLocation,
                    InstallLocation = Path.GetDirectoryName(bestInstallLocation),
                    Title = name,
                    ImageUrl = result["cover_url"],
                    BackgroundImageUrl = entry.GetProperty("bannerUrl").GetString(),
                    Developers = result["developers"],
                    Genres = [.. data.GetProperty("genres").EnumerateArray().Select(g => g.GetProperty("name").GetString())],
                    Features = [.. data.GetProperty("game_modes").EnumerateArray().Select(m => m.GetProperty("name").GetString())],
                    Rating = Math.Round(data.GetProperty("aggregated_rating").GetDouble() / 20.0, 2),
                    PlayTime = playTime,
                    AgeRatingUrl = result["age_rating_url"],
                    AgeRatingTitle = result["age_rating_title"],
                    AgeRatingDescription = result["age_rating_title"] is not null
                                            ? entry.GetProperty("ratingContent")[0].GetString()
                                            : null,
                    Description = data.GetProperty("summary").GetString(),
                    Screenshots = [.. entry.GetProperty("screenshots").EnumerateArray().Select(x => x.GetString())],
                    ReleaseDate = result["release_date"],
                    Size = sizeBytes >= 1024 * 1024 * 1024
                            ? $"{sizeBytes / (1024d * 1024d * 1024d):F1} GB"
                            : $"{sizeBytes / (1024d * 1024d):F2} MB"
                });
            }));
        }

        return [.. games];
    }

    [GeneratedRegex(@"[^\u0000-\u007F'’]+", RegexOptions.Compiled)]
    private static partial Regex CleanNameRegex();
    
    [GeneratedRegex(@"[^a-z0-9]", RegexOptions.Compiled)]
    private static partial Regex SimpleCleanNameRegex();
}
