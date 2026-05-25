using DevWinUI;
using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace AutoOS.Core.Helpers.Games;

public static partial class CitronHelper
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
            
            // get game list
            using var stream = File.OpenRead(Path.Combine(dataPath, "cache", "game_list", "game_metadata_cache.json"));
            using var configDoc = await JsonDocument.ParseAsync(stream);
            var config = configDoc.RootElement;

            // read json database
            using var fs = File.OpenRead(Path.Combine(PathHelper.GetAppDataFolderPath(), "Switch", "US.en.json"));
            using var doc = await JsonDocument.ParseAsync(fs);

            var jsonById = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);

            foreach (var kvp in doc.RootElement.EnumerateObject())
            {
                if (kvp.Value.TryGetProperty("id", out var idElem))
                {
                    var key = idElem.GetString()?.ToLowerInvariant();
                    if (!string.IsNullOrEmpty(key))
                        jsonById.TryAdd(key, kvp.Value);
                }
            }

            if (config.TryGetProperty("entries", out var entries) && entries.ValueKind == JsonValueKind.Array)
            {
                await Parallel.ForEachAsync(entries.EnumerateArray(), new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount * 2 }, async (CitronEntry, _) =>
                {
                    // check if game id exists in database
                    string id = "0" + CitronEntry.GetProperty("program_id").GetString();
                    if (!jsonById.TryGetValue(id, out var entry)) 
                        return;

                    // get name from database
                    string name = entry.TryGetProperty("name", out var nameElement) ? nameElement.GetString() : null;

                    // clean name for searching
                    if (string.IsNullOrWhiteSpace(name)) return;
                    string cleanName = CleanNameRegex().Replace(name.Replace('’', '\''), "");

                    // get install location
                    string installLocation = CitronEntry.GetProperty("file_path").GetString()?.Replace("/", "\\");

                    // get playtime
                    string playTime = "0m";

                    byte[] playtimeData = File.Exists(Path.Combine(dataPath, "play_time", "playtime.bin")) ? File.ReadAllBytes(Path.Combine(dataPath, "play_time", "playtime.bin")) : Directory.Exists(Path.Combine(dataPath, "play_time")) ? Directory.GetFiles(Path.Combine(dataPath, "play_time"), "*.bin").FirstOrDefault() is string f ? File.ReadAllBytes(f) : [] : [];

                    for (int offset = 0; offset + 16 <= playtimeData.Length; offset += 16)
                    {
                        if (BitConverter.ToUInt64(playtimeData, offset).ToString("x16").Equals(id, StringComparison.InvariantCultureIgnoreCase))
                        {
                            ulong seconds = BitConverter.ToUInt64(playtimeData, offset + 8);
                            int totalMinutes = (int)(seconds / 60);
                            playTime = GameModel.FormatPlayTime(totalMinutes);
                            break;
                        }
                    }

                    // get size
                    long sizeBytes = CitronEntry.TryGetProperty("file_size", out var sizeElem) ? sizeElem.GetInt64() : 0;

                    // search on igdb
                    var result = await IgdbHelper.SearchCovers(cleanName);
                    if (result == null) return;

                    using var docData = JsonDocument.Parse(await httpClient.GetStringAsync(result["game_url"], _));
                    var data = docData.RootElement.Clone();

                    games.Add(new GameModel
                    {
                        Launcher = "Citron",
                        LauncherLocation = exePath,
                        DataLocation = dataPath,
                        GameLocation = installLocation,
                        InstallLocation = Path.GetDirectoryName(installLocation),
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
                                : $"{sizeBytes / (1024d * 1024d):F2} MB",
                    });
                });
            }
        }

        return [.. games];
    }

    [GeneratedRegex(@"[^\u0000-\u007F'’]+", RegexOptions.Compiled)]
    private static partial Regex CleanNameRegex();

    [GeneratedRegex(@"[^a-z0-9]", RegexOptions.Compiled)]
    private static partial Regex SimpleCleanNameRegex();
}
