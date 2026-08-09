using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace AutoOS.Core.Helpers.Games;

public static partial class IgdbHelper
{
	private static readonly HttpClient httpClient = new();

	public static async Task<Dictionary<string, string?>?> SearchCovers(string name)
	{
		string Clean(string input) => Regex.Replace(input.ToLowerInvariant(), @"\s+", ".");
		string GetSearchBucket(string input)
		{
			string cleaned = Regex.Replace(input.Length >= 2 ? input[..2] : input.ToLowerInvariant(), @"[^a-z\d]", "");
			return string.IsNullOrEmpty(cleaned) ? "@" : cleaned;
		}

		try
		{
			string bucketJson = await httpClient.GetStringAsync($"https://raw.githubusercontent.com/LizardByte/GameDB/gh-pages/buckets/{GetSearchBucket(Clean(name))}.json");

			JsonElement bucketRoot = JsonDocument.Parse(bucketJson).RootElement;

			var matchingIds = new List<string>();
			string cleanName = Clean(name);

			if (bucketRoot.ValueKind == JsonValueKind.Object)
			{
				foreach (JsonProperty property in bucketRoot.EnumerateObject())
				{
					if (property.Value.ValueKind != JsonValueKind.Object)
						continue;

					if (!property.Value.TryGetProperty("name", out JsonElement nameProp))
						continue;

					string itemName = nameProp.GetString() ?? "";

					if (Clean(itemName) == cleanName)
						matchingIds.Add(property.Name);
				}
			}

			JsonElement? maxGame = null;
			int maxFields = 0;

			foreach (string id in matchingIds)
			{
				using HttpResponseMessage response = await httpClient.GetAsync($"https://raw.githubusercontent.com/LizardByte/GameDB/gh-pages/games/{id}.json");

				if (!response.IsSuccessStatusCode)
					continue;

				string json = await response.Content.ReadAsStringAsync();

				JsonElement root = JsonDocument.Parse(json).RootElement;

				if (root.ValueKind != JsonValueKind.Object)
					continue;

				int count = 0;
				foreach (JsonProperty prop in root.EnumerateObject())
				{
					JsonElement val = prop.Value;
					if (val.ValueKind == JsonValueKind.Object)
						continue;

					if (val.ValueKind == JsonValueKind.Null)
						continue;

					if (val.ValueKind == JsonValueKind.String && string.IsNullOrWhiteSpace(val.GetString()))
						continue;

					count++;
				}

				if (count > maxFields)
				{
					maxFields = count;
					maxGame = root.Clone();
				}
			}

			if (maxGame.HasValue && maxGame.Value.TryGetProperty("cover", out JsonElement cover) && cover.ValueKind == JsonValueKind.Object && cover.TryGetProperty("url", out JsonElement url) && url.ValueKind == JsonValueKind.String)
			{
				string thumb = url.GetString() ?? "";
				int dot = thumb.LastIndexOf('.');
				int slash = thumb.LastIndexOf('/');

				if (dot >= 0 && slash >= 0)
				{
					string slug = thumb.Substring(slash + 1, dot - slash - 1);

					var developers = new List<string>();

					if (maxGame.HasValue && maxGame.Value.TryGetProperty("involved_companies", out JsonElement companies))
					{
						foreach (JsonElement company in companies.EnumerateArray())
						{
							if (company.GetProperty("developer").GetBoolean() &&
								company.GetProperty("company").TryGetProperty("name", out JsonElement nameProp))
							{
								string? devName = nameProp.GetString();
								if (!string.IsNullOrWhiteSpace(devName))
									developers.Add(devName);
							}
						}
					}

					string developerNames = developers != null && developers.Any() ? string.Join(", ", developers) : "Unknown";

					string gameUrl = maxGame is JsonElement { ValueKind: JsonValueKind.Object } game &&
										game.TryGetProperty("id", out JsonElement id) &&
										id.ValueKind == JsonValueKind.Number &&
										id.TryGetInt32(out int gameId)
						? $"https://raw.githubusercontent.com/LizardByte/GameDB/gh-pages/games/{gameId}.json"
						: "";

					string region = new RegionInfo(CultureInfo.CurrentCulture.Name).TwoLetterISORegionName.ToUpper();

					string ratingKey = region switch
					{
						"AU" => "ACB",
						"BR" => "DEJUS",
						"KR" => "GRAC",
						"DE" => "USK",
						"US" or "CA" => "ESRB",
						_ => "PEGI"
					};

					string baseUrl = ratingKey.ToLowerInvariant() switch
					{
						"pegI" => "https://www.igdb.com/icons/rating_icons/pegi/pegi_",
						"esrb" => "https://www.igdb.com/icons/rating_icons/esrb/esrb_",
						"cero" => "https://www.igdb.com/icons/rating_icons/cero/cero_",
						"usk" => "https://www.igdb.com/icons/rating_icons/usk/usk_",
						"grac" => "https://www.igdb.com/icons/rating_icons/grac/grac_",
						"class_ind" => "https://www.igdb.com/icons/rating_icons/class_ind/class_ind_",
						"acb" => "https://www.igdb.com/icons/rating_icons/acb/acb_",
						_ => ""
					};

					Dictionary<string, string> ratingTitles = ratingKey switch
					{
						"PEGI" => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
						{
							{"3", "PEGI 3"}, {"7", "PEGI 7"}, {"12", "PEGI 12"}, {"16", "PEGI 16"}, {"18", "PEGI 18"},
						},
						"USK" => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
						{
							{"0", "USK ab 0"}, {"6", "USK ab 6"}, {"12", "USK ab 12"}, {"16", "USK ab 16"}, {"18", "USK ab 18"},
						},
						"ESRB" => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
						{
							{"ec", "Early Childhood"}, {"e", "Everyone"}, {"e10", "Everyone 10+"}, {"e10+", "Everyone 10+"},
							{"t", "Teen"}, {"m", "Mature 17+"}, {"ao", "Adults Only 18+"},
						},
						_ => [with(StringComparer.OrdinalIgnoreCase)]
					};

					JsonElement? ratingEntry = null;

					if (maxGame.HasValue && maxGame.Value.TryGetProperty("age_ratings", out JsonElement ageRatings))
					{
						foreach (JsonElement rating in ageRatings.EnumerateArray())
						{
							if (string.Equals(
								rating.GetProperty("organization").GetProperty("name").GetString(),
								ratingKey,
								StringComparison.OrdinalIgnoreCase))
							{
								ratingEntry = rating;
								break;
							}
						}
					}

					if (ratingEntry is null || !ratingEntry.Value.TryGetProperty("rating_category", out JsonElement ratingCategory) || !ratingCategory.TryGetProperty("rating", out JsonElement ratingValue))
					{
						return new Dictionary<string, string?>
						{
							{ "name", maxGame.Value.GetProperty("name").GetString() ?? "Unknown" },
							{ "game_url", gameUrl },
							{ "cover_url", $"https://images.igdb.com/igdb/image/upload/t_cover_big_2x/{slug}.jpg" },
							{ "developers", developerNames },
							{ "age_rating_url", null },
							{ "age_rating_title", null },
							{ "release_date", null }
						};
					}

					string ratingCode = ratingValue.GetString() ?? "";

					string ratingKeyForUrl = ratingKey == "ESRB" &&
												ratingCode.StartsWith("e10+", StringComparison.OrdinalIgnoreCase) ? "e10" : ratingCode;

					string ratingTitle = ratingTitles.TryGetValue(ratingCode, out string? title) ? title : ratingCode;

					string ratingUrl = $"{baseUrl}{ratingKeyForUrl.ToLowerInvariant()}.png";

					DateTimeOffset? releaseDate = null;

					if (maxGame.HasValue && maxGame.Value.TryGetProperty("release_dates", out JsonElement releaseDates))
					{
						JsonElement firstRelease = releaseDates.EnumerateArray().FirstOrDefault();
						if (firstRelease.ValueKind != JsonValueKind.Undefined &&
							firstRelease.TryGetProperty("date", out JsonElement dateProp) &&
							dateProp.ValueKind == JsonValueKind.Number)
						{
							long unixTime = dateProp.GetInt64();
							releaseDate = DateTimeOffset.FromUnixTimeSeconds(unixTime);
						}
					}

					return new Dictionary<string, string?>
					{
						{ "name", maxGame?.GetProperty("name").GetString() ?? "Unknown" },
						{ "game_url", gameUrl },
						{ "cover_url", $"https://images.igdb.com/igdb/image/upload/t_cover_big_2x/{slug}.jpg" },
						{ "developers", developerNames },
						{ "age_rating_url", ratingUrl },
						{ "age_rating_title", ratingTitle },
						{ "release_date", releaseDate?.ToString("d") }
					};
				}
			}
		}
		catch
		{
			return null;
		}

		return null;
	}
}
