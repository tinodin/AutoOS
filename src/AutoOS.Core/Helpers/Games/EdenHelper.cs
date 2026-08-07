using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.RegularExpressions;
using AutoOS.Core.Helpers.Games.Models;
using DevWinUI;

namespace AutoOS.Core.Helpers.Games;

public static partial class EdenHelper
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
				string content = await httpClient.GetStringAsync("https://raw.githubusercontent.com/blawar/titledb/refs/heads/master/US.en.json");
				Directory.CreateDirectory(Path.GetDirectoryName(filePath));
				await File.WriteAllTextAsync(filePath, content);
			}

			// get game list
			using FileStream stream = File.OpenRead(Path.Combine(dataPath, "cache", "game_list", "game_metadata_cache.json"));
			using JsonDocument configDoc = await JsonDocument.ParseAsync(stream);
			JsonElement config = configDoc.RootElement;

			// read json database
			using FileStream fs = File.OpenRead(Path.Combine(PathHelper.GetAppDataFolderPath(), "Switch", "US.en.json"));
			using JsonDocument doc = await JsonDocument.ParseAsync(fs);

			var jsonById = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);

			foreach (JsonProperty kvp in doc.RootElement.EnumerateObject())
			{
				if (kvp.Value.TryGetProperty("id", out JsonElement idElem))
				{
					string? key = idElem.GetString()?.ToLowerInvariant();
					if (!string.IsNullOrEmpty(key))
						jsonById.TryAdd(key, kvp.Value);
				}
			}

			if (config.TryGetProperty("entries", out JsonElement entries) && entries.ValueKind == JsonValueKind.Array)
			{
				await Parallel.ForEachAsync(entries.EnumerateArray(), new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount * 2 }, async (edenEntry, _) =>
				{
					// check if game id exists in database
					string id = "0" + edenEntry.GetProperty("program_id").GetString();
					if (!jsonById.TryGetValue(id, out JsonElement entry))
						return;

					// get name from database
					string name = entry.TryGetProperty("name", out JsonElement nameElement) ? nameElement.GetString() : null;

					// clean name for searching
					if (string.IsNullOrWhiteSpace(name)) return;
					string cleanName = CleanNameRegex().Replace(name.Replace('’', '\''), "");

					// get install location
					string installLocation = edenEntry.GetProperty("file_path").GetString()?.Replace("/", "\\");

					// get playtime
					string playTime = "0m";

					byte[] playtimeData = File.Exists(Path.Combine(dataPath, "play_time", "playtime.bin")) ? File.ReadAllBytes(Path.Combine(dataPath, "play_time", "playtime.bin")) : Directory.Exists(Path.Combine(dataPath, "play_time")) ? Directory.GetFiles(Path.Combine(dataPath, "play_time"), "*.bin").FirstOrDefault() is string f ? File.ReadAllBytes(f) : [] : [];

					for (int offset = 0; offset + 16 <= playtimeData.Length; offset += 16)
					{
						if (BitConverter.ToUInt64(playtimeData, offset).ToString("x16").Equals(id, StringComparison.InvariantCultureIgnoreCase))
						{
							ulong seconds = BitConverter.ToUInt64(playtimeData, offset + 8);
							playTime = (seconds / 3600) > 0 ? $"{seconds / 3600}h {(seconds % 3600) / 60}m" : $"{(seconds % 3600) / 60}m";
							break;
						}
					}

					// get size
					long sizeBytes = edenEntry.TryGetProperty("file_size", out JsonElement sizeElem) ? sizeElem.GetInt64() : 0;

					// search on igdb
					Dictionary<string, string> result = await IgdbHelper.SearchCovers(cleanName);
					if (result == null) return;

					using var docData = JsonDocument.Parse(await httpClient.GetStringAsync(result["game_url"], _));
					JsonElement data = docData.RootElement.Clone();

					games.Add(new GameModel
					{
						Launcher = "Eden",
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
