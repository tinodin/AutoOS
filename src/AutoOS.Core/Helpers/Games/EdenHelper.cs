using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.RegularExpressions;
using AutoOS.Core.Data.Models.Games;
using DevWinUI;

namespace AutoOS.Core.Helpers.Games;

public static partial class EdenHelper
{
	private static readonly HttpClient httpClient = new();

	public static async Task<List<GameModel>> GetGames(string exePath, string dataPath)
	{
		var games = new ConcurrentBag<GameModel>();

		if (string.IsNullOrWhiteSpace(dataPath))
			dataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "eden");

		// if paths defined
		if (!string.IsNullOrEmpty(exePath) && !string.IsNullOrEmpty(dataPath) && File.Exists(exePath) && Directory.Exists(dataPath))
		{
			// download switch game catalog
			string filePath = Path.Combine(PathHelper.GetAppDataFolderPath(), "Switch", "US.en.json");

			if (!File.Exists(filePath))
			{
				string content = await httpClient.GetStringAsync("https://raw.githubusercontent.com/blawar/titledb/refs/heads/master/US.en.json");
				Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
				await File.WriteAllTextAsync(filePath, content);
			}

			// read json database
			using FileStream fs = File.OpenRead(Path.Combine(PathHelper.GetAppDataFolderPath(), "Switch", "US.en.json"));
			using JsonDocument doc = await JsonDocument.ParseAsync(fs);

			var jsonById = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
			var jsonByName = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);

			foreach (JsonProperty kvp in doc.RootElement.EnumerateObject())
			{
				if (kvp.Value.TryGetProperty("id", out JsonElement idElem))
				{
					string? key = idElem.GetString()?.ToLowerInvariant();
					if (!string.IsNullOrEmpty(key))
						jsonById.TryAdd(key, kvp.Value);
				}

				string? name = kvp.Value.TryGetProperty("name", out JsonElement nameElem) ? nameElem.GetString() : null;
				if (string.IsNullOrWhiteSpace(name)) continue;
				if (!kvp.Value.TryGetProperty("id", out JsonElement nameId) || string.IsNullOrWhiteSpace(nameId.GetString())) continue;

				string simpleName = SimpleCleanNameRegex().Replace(CleanNameRegex().Replace(name.Replace('’', '\''), "").ToLowerInvariant(), "");
				if (!string.IsNullOrEmpty(simpleName))
					jsonByName.TryAdd(simpleName, kvp.Value);
			}

			// get game list
			var edenEntries = new List<(string ProgramId, string FilePath, long FileSize)>();
			var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			string configPath = Path.Combine(dataPath, "config", "qt-config.ini");

			if (File.Exists(configPath))
			{
				foreach (string line in File.ReadLines(configPath))
				{
					if (!line.StartsWith("Paths\\gamedirs\\", StringComparison.OrdinalIgnoreCase) || !line.Contains("\\path=", StringComparison.OrdinalIgnoreCase))
						continue;

					string gameDir = line[(line.IndexOf('=') + 1)..].Trim().Trim('"');
					if (string.IsNullOrWhiteSpace(gameDir) || !Directory.Exists(gameDir))
						continue;

					foreach (string gameFile in Directory.EnumerateFiles(gameDir, "*", SearchOption.TopDirectoryOnly))
					{
						string extension = Path.GetExtension(gameFile);
						if (!extension.Equals(".xci", StringComparison.OrdinalIgnoreCase) && !extension.Equals(".nsp", StringComparison.OrdinalIgnoreCase))
							continue;

						string simpleName = SimpleCleanNameRegex().Replace(Path.GetFileNameWithoutExtension(gameFile).ToLowerInvariant(), "");
						string? bestMatch = null;

						foreach (string key in jsonByName.Keys)
						{
							if (key.Length < 4 || !simpleName.StartsWith(key, StringComparison.Ordinal))
								continue;

							if (bestMatch is null || key.Length > bestMatch.Length)
								bestMatch = key;
						}

						if (bestMatch is null) continue;

						JsonElement entry = jsonByName[bestMatch];
						string? id = entry.TryGetProperty("id", out JsonElement idElement) ? idElement.GetString() : null;
						if (string.IsNullOrWhiteSpace(id) || !seenIds.Add(id)) continue;

						edenEntries.Add((id.ToLowerInvariant(), Path.GetFullPath(gameFile), new FileInfo(gameFile).Length));
					}
				}
			}

			if (edenEntries.Count > 0)
			{
				await Parallel.ForEachAsync(edenEntries, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount * 2 }, async (edenEntry, _) =>
				{
					// check if game id exists in database
					string id = edenEntry.ProgramId;
					if (!jsonById.TryGetValue(id, out JsonElement entry))
						return;

					// get name from database
					string? name = entry.TryGetProperty("name", out JsonElement nameElement) ? nameElement.GetString() : null;

					// clean name for searching
					if (string.IsNullOrWhiteSpace(name)) return;
					string cleanName = CleanNameRegex().Replace(name.Replace('’', '\''), "");

					// get install location
					string installLocation = edenEntry.FilePath;

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
					long sizeBytes = edenEntry.FileSize;

					// search on igdb
Dictionary<string, string?>? result = await IgdbHelper.SearchCovers(cleanName);
				if (result == null) return;

					using var docData = JsonDocument.Parse(await httpClient.GetStringAsync(result["game_url"]!, _));
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
						Genres = [.. data.GetProperty("genres").EnumerateArray().Select(g => g.GetProperty("name").GetString() ?? "")],
						Features = [.. data.GetProperty("game_modes").EnumerateArray().Select(m => m.GetProperty("name").GetString() ?? "")],
						Rating = Math.Round(data.GetProperty("aggregated_rating").GetDouble() / 20.0, 2),
						PlayTime = playTime,
						AgeRatingUrl = result["age_rating_url"],
						AgeRatingTitle = result["age_rating_title"],
						AgeRatingDescription = result["age_rating_title"] is not null
												? entry.GetProperty("ratingContent")[0].GetString()
												: null,
						Description = data.GetProperty("summary").GetString(),
						Media = GameMediaModel.CreateScreenshots(entry.GetProperty("screenshots").EnumerateArray().Select(x => x.GetString())),
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
