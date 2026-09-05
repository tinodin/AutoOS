using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using AutoOS.Core.Common;
using AutoOS.Core.Data.Clients.Games;
using AutoOS.Core.Data.Models.Games;
using AutoOS.Core.Helpers.Logging;
using DevWinUI;
using Microsoft.VisualBasic.FileIO;
using Microsoft.Win32;
using Windows.Media.Core;

namespace AutoOS.Core.Helpers.Games;

public static partial class EpicGamesHelper
{
	public static readonly string EpicGamesPath = File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Epic Games", "Launcher", "Portal", "Binaries", "Win64", "EpicGamesLauncher.exe")) ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Epic Games", "Launcher", "Portal", "Binaries", "Win64", "EpicGamesLauncher.exe") : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Epic Games", "Launcher", "Portal", "Binaries", "Win64", "EpicGamesLauncher.exe");
	public static readonly string ActiveEpicGamesAccountPath = File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"EpicGamesLauncher\Saved\Config\WindowsEditor", "GameUserSettings.ini")) ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"EpicGamesLauncher\Saved\Config\WindowsEditor", "GameUserSettings.ini") : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"EpicGamesLauncher\Saved\Config\Windows", "GameUserSettings.ini");
	public static readonly string EpicGamesAccountDir = Directory.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"EpicGamesLauncher\Saved\Config\WindowsEditor")) ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"EpicGamesLauncher\Saved\Config\WindowsEditor") : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"EpicGamesLauncher\Saved\Config\Windows");
	public static readonly string EpicGamesInstalledGamesPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Epic", "UnrealEngineLauncher", "LauncherInstalled.dat");
	public static readonly string EpicGamesManifestDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Epic", "EpicGamesLauncher", "Data", "Manifests");
	public static readonly string EpicGamesThirdPartyManifestDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Epic", "EpicGamesLauncher", "Data", "ThirPartyManagedApps");
	public static readonly string EpicGamesInstalledItemsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Epic", "EpicOnlineServicesShared", "InstallHelper", "InstalledItems");

	private static readonly HttpClient httpClient = new();
	private static readonly HttpClient loginClient = new();
	private static readonly Lazy<TlsClient> tlsClient = new(() => new TlsClient());

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
				productSlug
				urlSlug
				catalogNs {
					mappings {
						pageSlug
						pageType
						productId
					}
				}
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
		public string? DisplayName { get; set; }
		public string? AccountId { get; set; }
		public bool IsActive { get; set; }
	}

	public static List<EpicAccountInfo> GetEpicGamesAccounts()
	{
		List<EpicAccountInfo> accounts = [];

		if (!File.Exists(EpicGamesPath) || !Directory.Exists(EpicGamesAccountDir))
			return accounts;

		// get all configs
		foreach (string file in Directory.GetFiles(EpicGamesAccountDir, "GameUserSettings.ini", System.IO.SearchOption.AllDirectories))
		{
			try
			{
				// check if data is valid
				if (!ValidateData(file))
					continue;

				(string? accountId, string? displayName, string _, int _) = GetAccountData(file);

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
			catch
			{
				continue;
			}
		}

		return [.. accounts.OrderBy(x => x.DisplayName, StringComparer.OrdinalIgnoreCase)];
	}

	public static bool ValidateData(string file)
	{
		(string _, string _, string? token, int _) = GetAccountData(file);

		return !string.IsNullOrWhiteSpace(token);
	}

	public static string Decrypt(string base64)
	{
		byte[] keyBytes = Encoding.ASCII.GetBytes(AesKey);
		using var aes = Aes.Create();
		aes.KeySize = keyBytes.Length * 8;
		aes.Mode = CipherMode.ECB;
		aes.Padding = PaddingMode.PKCS7;

		aes.Key = keyBytes;

		byte[] cipher = Convert.FromBase64String(base64);
		using var ms = new MemoryStream(cipher);
		using var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read);
		using var sr = new StreamReader(cs, Encoding.UTF8);
		//using var sr = new StreamReader(cs, Encoding.GetEncoding("windows-1252"));
		string result = sr.ReadToEnd();
		return result;
	}

	public static string Encrypt(string plainText)
	{
		byte[] keyBytes = Encoding.ASCII.GetBytes(AesKey);
		using var aes = Aes.Create();
		aes.KeySize = keyBytes.Length * 8;
		aes.Mode = CipherMode.ECB;
		aes.Padding = PaddingMode.PKCS7;

		aes.Key = keyBytes;

		byte[] plain = Encoding.UTF8.GetBytes(plainText);
		//byte[] plain = Encoding.GetEncoding("windows-1252").GetBytes(plainText);

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
			var iniHelper = new InIHelper(file);
			string decryptedOffline = Decrypt(iniHelper.ReadValue("Data", "Offline", 2048));
			string decryptedRememberMe = Decrypt(iniHelper.ReadValue("Data", "RememberMe", 2048));

			JsonElement rememberMeRoot = JsonDocument.Parse(decryptedRememberMe.TrimEnd('\0')).RootElement[0];
			string displayName = rememberMeRoot.GetProperty("DisplayName").GetString()!;
			string token = rememberMeRoot.GetProperty("Token").GetString()!;
			int tokenUseCount = rememberMeRoot.GetProperty("TokenUseCount").GetInt32();

			JsonElement offlineArray = JsonDocument.Parse(decryptedOffline.TrimEnd('\0')).RootElement;
			string accountId = null!;

			foreach (JsonElement account in offlineArray.EnumerateArray())
			{
				if (account.TryGetProperty("Email", out JsonElement emailProp) && emailProp.GetString() == rememberMeRoot.GetProperty("Email").GetString())
				{
					accountId = account.GetProperty("UserID").GetString()!;
					break;
				}
			}

			return (accountId, displayName, token, tokenUseCount);
		}
		catch
		{
			return (null!, null!, null!, 0);
		}
	}

	public static async Task<string> Exchange()
	{
		try
		{
			string AccessToken = await UpdateEpicGamesToken(ActiveEpicGamesAccountPath);

			if (AccessToken == null)
				return null!;

			loginClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AccessToken);

			string exchangeUrl = "https://account-public-service-prod.ol.epicgames.com/account/api/oauth/exchange";
			string exchangeFallbackUrl = "https://account-public-service-prod03.ol.epicgames.com/account/api/oauth/exchange";
			HttpResponseMessage response;
			try
			{
				response = await loginClient.GetAsync(exchangeUrl);
			}
			catch (Exception)
			{
				try
				{
					response = await loginClient.GetAsync(exchangeFallbackUrl);
				}
				catch (Exception fallbackEx)
				{
					LogHelper.LogError(fallbackEx, null, $"Failed to exchange Epic Games token from both {exchangeUrl} and {exchangeFallbackUrl}");
					return null!;
				}
			}

			if (!response.IsSuccessStatusCode)
			{
				try
				{
					response = await loginClient.GetAsync(exchangeFallbackUrl);
				}
				catch (Exception)
				{
					LogHelper.LogError(new HttpRequestException($"Exchange request failed with status {response.StatusCode}"), null, $"Failed to exchange Epic Games token from both {exchangeUrl} and {exchangeFallbackUrl}");
					return null!;
				}

				if (!response.IsSuccessStatusCode)
				{
					LogHelper.LogError(new HttpRequestException($"Exchange request failed with status {response.StatusCode}"), null, $"Failed to exchange Epic Games token from both {exchangeUrl} and {exchangeFallbackUrl}");
					return null!;
				}
			}

			var responseJson = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

			return responseJson.RootElement.GetProperty("code").GetString()!;
		}
		catch (Exception ex)
		{
			LogHelper.LogError(ex, null, "Failed to exchange Epic Games token");
			return null!;
		}
	}

	public static async Task<string> UpdateEpicGamesToken(string file)
	{
		// close epic games launcher
		CloseEpicGames();

		// add needed encoding options
		Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

		// read old data
		var iniHelper = new InIHelper(file);
		string rememberMeData = iniHelper.ReadValue("Data", "RememberMe", 2048);

		// decrypt it
		string decryptedFull = Decrypt(rememberMeData);
		string decryptedJson = decryptedFull.Contains('\0') ? decryptedFull[..decryptedFull.IndexOf('\0')] : decryptedFull;
		string trailingData = decryptedFull.Contains('\0') ? decryptedFull[decryptedFull.IndexOf('\0')..] : "";
		JsonArray jsonArray = JsonNode.Parse(decryptedJson)!.AsArray();

		// get old refresh token
		string oldRefreshToken = jsonArray[0]!["Token"]!.GetValue<string>();

		// authenticate
		httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{ClientId}:{ClientSecret}")));

		var content = new FormUrlEncodedContent(
		[
			new KeyValuePair<string, string>("grant_type", "refresh_token"),
			new KeyValuePair<string, string>("refresh_token", oldRefreshToken),
			new KeyValuePair<string, string>("token_type", "eg1"),
		]);

		string authUrl = "https://account-public-service-prod.ol.epicgames.com/account/api/oauth/token";
		string authFallbackUrl = "https://account-public-service-prod03.ol.epicgames.com/account/api/oauth/token";
		HttpResponseMessage response;
		try
		{
			response = await httpClient.PostAsync(authUrl, content);
		}
		catch (Exception)
		{
			try
			{
				response = await httpClient.PostAsync(authFallbackUrl, content);
			}
			catch (Exception fallbackEx)
			{
				LogHelper.LogError(fallbackEx, null, $"Failed to update Epic Games token from both {authUrl} and {authFallbackUrl}");
				return null!;
			}
		}

		if (!response.IsSuccessStatusCode)
		{
			try
			{
				response = await httpClient.PostAsync(authFallbackUrl, content);
			}
			catch (Exception)
			{
				return null!;
			}

			if (!response.IsSuccessStatusCode)
			{
				return null!;
			}
		}

		var responseJson = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

		string newDisplayName = responseJson.RootElement.GetProperty("displayName").GetString()!;
		string newAccessToken = responseJson.RootElement.GetProperty("access_token").GetString()!;
		string newRefreshToken = responseJson.RootElement.GetProperty("refresh_token").GetString()!;

		// replace old display name and refresh token with new data
		jsonArray[0]!["DisplayName"] = newDisplayName;
		jsonArray[0]!["Token"] = newRefreshToken;

		// write changes
		var options = new JsonSerializerOptions
		{
			Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
		};

		string updatedJson = jsonArray.ToJsonString(options);
		string reencrypted = Encrypt(updatedJson + trailingData);
		reencrypted = rememberMeData.StartsWith("\"") && rememberMeData.EndsWith("\"") ? $"\"{reencrypted}\"" : reencrypted;

		iniHelper.AddValue("Data", $"\"{reencrypted}\"", "RememberMe");
		new InIHelper(Path.Combine(EpicGamesAccountDir, GetAccountData(ActiveEpicGamesAccountPath).AccountId, "GameUserSettings.ini")).AddValue("Data", $"\"{reencrypted}\"", "RememberMe");

		return newAccessToken;
	}

	public static void CloseEpicGames()
	{
		foreach (string? name in new[] { "EpicGamesLauncher", "EpicWebHelper" })
		{
			Process.GetProcessesByName(name).ToList().ForEach(process =>
			{
				try
				{
					process.Kill();
					process.WaitForExit(2000);
				}
				catch { }
			});
		}
	}

	public static void DisableMinimizeToTray(string file)
	{
		(string? accountId, string _, string _, int _) = GetAccountData(file);

		var iniHelper = new InIHelper(file);

		iniHelper.AddValue("MinimiseToSystemTray", "False", accountId + "_General");
	}

	public static void DisableNotifications(string file)
	{
		(string? accountId, string _, string _, int _) = GetAccountData(file);

		var iniHelper = new InIHelper(file);

		iniHelper.AddValue("NotificationsEnabled_FreeGame", "False", accountId + "_General");
		iniHelper.AddValue("NotificationsEnabled_Adverts", "False", accountId + "_General");
	}

	public static void AddPlaytime(string artifactId, DateTime startTime, Action<string, string>? onPlayTimeUpdated = null)
	{
		string url = $"https://library-service.live.use1a.on.epicgames.com/library/api/public/playtime/account/{GetAccountData(ActiveEpicGamesAccountPath).AccountId}";
		DateTime endTime = DateTime.UtcNow;
		DateTime startTimeUtc = startTime.ToUniversalTime();

		string startTimeStr = startTimeUtc.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
		string endTimeStr = endTime.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");

		var payload = new PlaytimePayload(
			Guid.NewGuid().ToString(),
			artifactId,
			startTimeStr,
			endTimeStr,
			true,
			true
		);

		string json = JsonSerializer.Serialize(payload, PlaytimeJsonContext.Default.PlaytimePayload);
		var content = new StringContent(json, Encoding.UTF8, "application/json");

		HttpResponseMessage response;
		try
		{
			response = loginClient.PutAsync(url, content).GetAwaiter().GetResult();
		}
		catch (Exception ex)
		{
			LogHelper.LogError(ex, null, $"Failed to submit playtime to {url}");
			return;
		}

		if (response.IsSuccessStatusCode)
		{
			TimeSpan duration = endTime - startTimeUtc;

			string playTimeUrl = $"https://library-service.live.use1a.on.epicgames.com/library/api/public/playtime/account/{GetAccountData(ActiveEpicGamesAccountPath).AccountId}/artifact/{artifactId}";
			HttpResponseMessage playTimeResponse;
			try
			{
				playTimeResponse = loginClient.GetAsync(playTimeUrl).GetAwaiter().GetResult();
			}
			catch (Exception ex)
			{
				LogHelper.LogError(ex, null, $"Failed to get playtime from {playTimeUrl}");
				return;
			}

			if (playTimeResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized)
			{
				return;
			}

			var playTimeJson = JsonNode.Parse(playTimeResponse.Content.ReadAsStringAsync().GetAwaiter().GetResult());
			int newTotalTime = playTimeJson?["totalTime"]?.GetValue<int>() ?? 0;
			var ts = TimeSpan.FromSeconds(newTotalTime);
			string formattedTime = ts.TotalHours >= 1 ? $"{(int)ts.TotalHours}h {ts.Minutes}m" : $"{ts.Minutes}m";
			onPlayTimeUpdated?.Invoke(artifactId, formattedTime);
		}
	}

	public static async Task ImportAccount(IStatusReporter? reporter = null)
	{
		// get all configs from other drives
		string? systemDrive = Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.System));
		var foundFiles = DriveInfo.GetDrives()
			.Where(d => d.DriveType == DriveType.Fixed && d.Name != systemDrive)
			.SelectMany(d =>
			{
				string usersPath = Path.Combine(d.Name, "Users");
				if (!Directory.Exists(usersPath)) return [];

				return Directory.GetDirectories(usersPath)
					.Select(userDir =>
						File.Exists(Path.Combine(userDir, "AppData", "Local", "EpicGamesLauncher", "Saved", "Config", "WindowsEditor", "GameUserSettings.ini"))
						? Path.Combine(userDir, "AppData", "Local", "EpicGamesLauncher", "Saved", "Config", "WindowsEditor", "GameUserSettings.ini")
						: Path.Combine(userDir, "AppData", "Local", "EpicGamesLauncher", "Saved", "Config", "Windows", "GameUserSettings.ini")
					)
					.Where(File.Exists);
			})
			.Select(path => new FileInfo(path))
			.ToList();

		string newestFilePath = null!;

		// check if files are valid
		foreach (FileInfo? file in foundFiles)
		{
			string configContent = await File.ReadAllTextAsync(file.FullName);
			Match dataMatch = Regex.Match(configContent, @"Data=([^\r\n]+)");

			if (ValidateData(file.FullName))
			{
				// use the latest one
				if (newestFilePath == null || file.LastWriteTime > new FileInfo(newestFilePath).LastWriteTime)
				{
					// copy the file
					Directory.CreateDirectory(Path.GetDirectoryName(ActiveEpicGamesAccountPath)!);
					File.Copy(file.FullName, ActiveEpicGamesAccountPath, true);

					// disable tray and notifications
					DisableMinimizeToTray(ActiveEpicGamesAccountPath);
					DisableNotifications(ActiveEpicGamesAccountPath);

					// get accountId
					string accountId = GetAccountData(file.FullName).AccountId;

					// create backup folder
					Directory.CreateDirectory(Path.Combine(EpicGamesAccountDir, accountId));

					// copy config
					File.Copy(ActiveEpicGamesAccountPath, Path.Combine(EpicGamesAccountDir, accountId, "GameUserSettings.ini"), true);

					// create reg file
					File.WriteAllText(Path.Combine(Path.Combine(EpicGamesAccountDir, accountId), "accountId.reg"), $"Windows Registry Editor Version 5.00\r\n\r\n[HKEY_CURRENT_USER\\Software\\Epic Games\\Unreal Engine\\Identifiers]\r\n\"AccountId\"=\"{accountId}\"");

					// update refresh token
					await UpdateEpicGamesToken(ActiveEpicGamesAccountPath);

					// update the backed up config
					File.Copy(file.FullName, Path.Combine(EpicGamesAccountDir, accountId, "GameUserSettings.ini"), true);

					reporter?.SetTitle($"Succesfully logged in as {GetAccountData(ActiveEpicGamesAccountPath).DisplayName}...");

					await Task.Delay(1000);

					return;
				}
			}
		}
	}

	public static async Task ImportGames()
	{
		// get the newest install list from other drives
		string? systemDrive = Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.System));
		var foundFiles = DriveInfo.GetDrives()
			.Where(d => d.DriveType == DriveType.Fixed && d.Name != systemDrive)
			.Select(d => Path.Combine(d.Name, "ProgramData", "Epic", "UnrealEngineLauncher", "LauncherInstalled.dat"))
			.Where(File.Exists)
			.Select(path => new FileInfo(path))
			.OrderByDescending(f => f.LastWriteTime)
			.ToList();

		if (foundFiles.Count == 0)
			return;

		FileInfo? newestFile = null;
		string? oldDrive = null;
		JsonNode? jsonObject = null;
		JsonArray? installationList = null;

		foreach (FileInfo candidate in foundFiles)
		{
			try
			{
				string content = await File.ReadAllTextAsync(candidate.FullName);

				if (string.IsNullOrWhiteSpace(content))
					continue;

				JsonNode? obj = JsonNode.Parse(content);

				if (obj?["InstallationList"] is JsonArray list && list.Count > 0)
				{
					newestFile = candidate;
					oldDrive = Path.GetPathRoot(candidate.FullName)!;
					jsonObject = obj;
					installationList = list;
					break;
				}
			}
			catch
			{
				continue;
			}
		}

		if (newestFile == null || oldDrive == null || jsonObject == null || installationList == null)
			return;

		// check and set new game paths in LauncherInstalled.dat
		foreach (JsonNode? game in installationList)
		{
			if (game is JsonObject gameObj && gameObj.ContainsKey("InstallLocation"))
			{
				string originalPath = gameObj["InstallLocation"]!.ToString();
				string relativePath = originalPath[Path.GetPathRoot(originalPath)!.Length..];

				foreach (DriveInfo? drive in DriveInfo.GetDrives().Where(drive => drive.DriveType == DriveType.Fixed && drive.Name != systemDrive))
				{
					if (Directory.Exists(Path.Combine(drive.Name, relativePath)))
					{
						gameObj["InstallLocation"] = drive.Name[0] + originalPath[1..];
						break;
					}
				}
			}
		}

		// write updated install list to new drive
		Directory.CreateDirectory(Path.GetDirectoryName(EpicGamesInstalledGamesPath)!);
		await File.WriteAllTextAsync(EpicGamesInstalledGamesPath, jsonObject.ToJsonString(new JsonSerializerOptions { WriteIndented = true, IndentCharacter = '\t', IndentSize = 1, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping }));

		// copy manifests folder to new drive
		string srcThirdParty = Path.Combine(oldDrive, EpicGamesThirdPartyManifestDir[Path.GetPathRoot(EpicGamesThirdPartyManifestDir)!.Length..]);
		if (Directory.Exists(srcThirdParty))
			FileSystem.CopyDirectory(srcThirdParty, EpicGamesThirdPartyManifestDir, true);

		string srcManifest = Path.Combine(oldDrive, EpicGamesManifestDir[Path.GetPathRoot(EpicGamesManifestDir)!.Length..]);
		if (Directory.Exists(srcManifest))
		{
			// set new game paths in manifests
			FileSystem.CopyDirectory(srcManifest, EpicGamesManifestDir, true);
			foreach (string file in Directory.GetFiles(EpicGamesManifestDir, "*.item", System.IO.SearchOption.AllDirectories))
			{
				try
				{
					string fileContent = await File.ReadAllTextAsync(file);
					if (string.IsNullOrWhiteSpace(fileContent))
					{
						File.Delete(file);
						continue;
					}
					var itemJson = JsonNode.Parse(fileContent);

					if (itemJson is JsonObject itemObj && itemObj.ContainsKey("InstallLocation"))
					{
						string originalPath = itemObj["InstallLocation"]!.ToString();
						string relativePath = originalPath[Path.GetPathRoot(originalPath)!.Length..];

						foreach (DriveInfo? drive in DriveInfo.GetDrives().Where(drive => drive.DriveType == DriveType.Fixed && drive.Name != systemDrive))
						{
							if (Directory.Exists(Path.Combine(drive.Name, relativePath)))
							{
								// store found drive
								char newDrive = drive.Name[0];

								// update install location
								itemObj["InstallLocation"] = newDrive + originalPath[1..];

								// update other paths
								foreach (string? prop in new[] { "ManifestLocation", "StagingLocation", "CompleteManifestPath", "PendingManifestPath" })
								{
									if (itemObj.ContainsKey(prop) && itemObj[prop]!.ToString() is string val && val.Length >= 2 && val[1] == ':')
										itemObj[prop] = newDrive + val[1..];
								}

								// write updated manifest files
								await File.WriteAllTextAsync(file, itemObj.ToJsonString(new JsonSerializerOptions { WriteIndented = true, IndentCharacter = '\t', IndentSize = 1, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping }));
								break;
							}
						}
					}
				}
				catch
				{
					continue;
				}
			}
		}

		// copy install dir to new drive
		string srcInstalled = Path.Combine(oldDrive, EpicGamesInstalledItemsDir[Path.GetPathRoot(EpicGamesInstalledItemsDir)!.Length..]);
		if (Directory.Exists(srcInstalled))
		{
			// set new game paths in installed items manifests
			FileSystem.CopyDirectory(srcInstalled, EpicGamesInstalledItemsDir, true);
			foreach (string file in Directory.GetFiles(EpicGamesInstalledItemsDir, "*.egi", System.IO.SearchOption.AllDirectories))
			{
				var egiJson = JsonNode.Parse(await File.ReadAllTextAsync(file));

				if (egiJson is JsonObject egiObj && egiObj["v4"] is JsonObject v4Obj)
				{
					string originalPath = v4Obj["dir"]!.ToString();
					string relativePath = originalPath[Path.GetPathRoot(originalPath)!.Length..];

					foreach (DriveInfo? drive in DriveInfo.GetDrives().Where(drive => drive.DriveType == DriveType.Fixed && drive.Name != systemDrive))
					{
						if (Directory.Exists(Path.Combine(drive.Name, relativePath)))
						{
							// store found drive
							char newDrive = drive.Name[0];

							// update dir
							v4Obj["dir"] = newDrive + originalPath[1..];

							// update other paths
							foreach (string? prop in new[] { "metaDir", "manifestPath", "pendingManifestPath" })
							{
								if (v4Obj.ContainsKey(prop) && v4Obj[prop]!.ToString() is string val && val.Length >= 2 && val[1] == ':')
									v4Obj[prop] = newDrive + val[1..];
							}

							// write updated egi files as a single line
							await File.WriteAllTextAsync(file, egiObj.ToJsonString(new JsonSerializerOptions { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping }));
							break;
						}
					}
				}
			}
		}
	}

	public static async Task EpicGamesLogin(IStatusReporter? reporter = null)
	{
		// launch epic games launcher
		Process.Start(EpicGamesPath);

		// check when logged in
		while (true)
		{
			if (File.Exists(ActiveEpicGamesAccountPath))
			{
				if (ValidateData(ActiveEpicGamesAccountPath))
				{
					await Task.Delay(1000);

					// close epic games launcher
					CloseEpicGames();

					// disable tray and notifications
					DisableMinimizeToTray(ActiveEpicGamesAccountPath);
					DisableNotifications(ActiveEpicGamesAccountPath);

					reporter?.SetTitle($"Succesfully logged in as {GetAccountData(ActiveEpicGamesAccountPath).DisplayName}...");
					break;
				}
			}

			if (Process.GetProcessesByName("EpicGamesLauncher").Length == 0)
			{
				// disable tray and notifications
				DisableMinimizeToTray(ActiveEpicGamesAccountPath);
				DisableNotifications(ActiveEpicGamesAccountPath);
				break;
			}

			await Task.Delay(500);
		}

		await Task.Delay(1000);
	}

	public static async Task UpdateInvalidEpicGamesToken(IStatusReporter? reporter = null)
	{
		reporter?.SetTitle("The refresh token is no longer valid. Please enter your password again...");

		// close epic games launcher
		CloseEpicGames();

		// delay
		await Task.Delay(500);

		// launch epic games launcher
		Process.Start(EpicGamesPath);

		// check when logged in
		while (true)
		{
			if (File.Exists(ActiveEpicGamesAccountPath))
			{
				if (ValidateData(ActiveEpicGamesAccountPath))
				{
					break;
				}
			}

			await Task.Delay(500);
		}

		// close epic games launcher
		CloseEpicGames();

		// disable tray and notifications
		DisableMinimizeToTray(ActiveEpicGamesAccountPath);
		DisableNotifications(ActiveEpicGamesAccountPath);

		reporter?.SetTitle($"Succesfully logged in as {GetAccountData(ActiveEpicGamesAccountPath).DisplayName}...");

		await Task.Delay(1000);
	}

	public static async Task<List<GameModel>> GetGames()
	{
		var games = new ConcurrentBag<GameModel>();

		if (File.Exists(EpicGamesPath) && (Directory.Exists(EpicGamesManifestDir) || Directory.Exists(EpicGamesThirdPartyManifestDir)))
		{
			// get access token
			string AccessToken = await UpdateEpicGamesToken(ActiveEpicGamesAccountPath);

			if (AccessToken == null)
			{
				throw new UnauthorizedAccessException("Failed to retrieve the Epic Games access token. Please log in again in the Epic Games Launcher.");
			}

			loginClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AccessToken);

			// get library data
			var libraryData = new List<JsonNode?>();
			string? nextCursor = null;

			do
			{
				string url = $"https://library-service.live.use1a.on.epicgames.com/library/api/public/items?includeMetadata=true&platform=Windows";
				if (nextCursor != null)
					url += $"&cursor={nextCursor}";

				JsonNode? json;
				try
				{
					json = JsonNode.Parse(await loginClient.GetStringAsync(url));
				}
				catch (Exception ex)
				{
					LogHelper.LogError(ex, null, $"Failed to load library data from {url}");
					break;
				}

				JsonArray? records = (json as JsonObject)?["records"] as JsonArray;
				if (records != null)
					libraryData.AddRange(records);

				nextCursor = ((json as JsonObject)?["responseMetadata"] as JsonObject)?["nextCursor"]?.GetValue<string>();

			} while (!string.IsNullOrEmpty(nextCursor));

			// get build data
			JsonArray? buildData = null;
			string buildUrl = "https://launcher-public-service-prod.ol.epicgames.com/launcher/api/public/assets/Windows?label=Live";
			string buildFallbackUrl = "https://launcher-public-service-prod06.ol.epicgames.com/launcher/api/public/assets/Windows?label=Live";
			HttpResponseMessage? buildResponse;
			try
			{
				buildResponse = await loginClient.GetAsync(buildUrl);
			}
			catch (Exception)
			{
				try
				{
					buildResponse = await loginClient.GetAsync(buildFallbackUrl);
				}
				catch (Exception fallbackEx)
				{
					LogHelper.LogError(fallbackEx, null, $"Failed to load build data from both {buildUrl} and {buildFallbackUrl}");
					buildResponse = null;
				}
			}

			if (buildResponse != null && !buildResponse.IsSuccessStatusCode)
			{
				try
				{
					buildResponse = await loginClient.GetAsync(buildFallbackUrl);
				}
				catch (Exception fallbackEx)
				{
					LogHelper.LogError(fallbackEx, null, $"Failed to load build data from both {buildUrl} and {buildFallbackUrl}");
					buildResponse = null;
				}
			}

			if (buildResponse != null && buildResponse.IsSuccessStatusCode)
				buildData = JsonNode.Parse(await buildResponse.Content.ReadAsStringAsync()) as JsonArray;

			// get playtime data
			Dictionary<string, int>? playTimeData = null;
			string playTimeUrl = $"https://library-service.live.use1a.on.epicgames.com/library/api/public/playtime/account/{GetAccountData(ActiveEpicGamesAccountPath).AccountId}/all";
			HttpResponseMessage? playTimeResponse;
			try
			{
				playTimeResponse = await loginClient.GetAsync(playTimeUrl);
			}
			catch (Exception ex)
			{
				LogHelper.LogError(ex, null, $"Failed to load playtime data from {playTimeUrl}");
				playTimeResponse = null;
			}

			if (playTimeResponse != null && playTimeResponse.IsSuccessStatusCode)
			{
				playTimeData = (JsonNode.Parse(await playTimeResponse.Content.ReadAsStringAsync()) as JsonArray)?.Where(playtime => (playtime as JsonObject)?["artifactId"] is not null)
					.ToDictionary(
						playtime => (playtime as JsonObject)?["artifactId"]!.GetValue<string>()!,
						playtime => (playtime as JsonObject)?["totalTime"]?.GetValue<int>() ?? 0
					);
			}

			string region = new RegionInfo(CultureInfo.CurrentCulture.Name).TwoLetterISORegionName.ToUpper();

			string ratingKey = region switch
			{
				"AU" => "ACB",
				"BR" => "ClassInd",
				"KR" => "GRAC",
				"DE" => "USK",
				"US" or "CA" => "ESRB",
				_ => "PEGI"
			};

			List<string> manifestFiles = Directory.Exists(EpicGamesManifestDir) ? Directory.GetFiles(EpicGamesManifestDir, "*.item", System.IO.SearchOption.TopDirectoryOnly).ToList() : [];

			var allManifests = new List<JsonNode>();
			foreach (string file in manifestFiles)
			{
				try
				{
					string fileContent = File.ReadAllText(file);
					if (string.IsNullOrWhiteSpace(fileContent))
					{
						File.Delete(file);
						continue;
					}
					var node = JsonNode.Parse(fileContent);
					allManifests.Add(node!);
				}
				catch
				{
					continue;
				}
			}

			if (Directory.Exists(EpicGamesThirdPartyManifestDir))
			{
				foreach (string file in Directory.GetFiles(EpicGamesThirdPartyManifestDir, "*.json"))
				{
					try
					{
						string fileContent = File.ReadAllText(file);
						if (string.IsNullOrWhiteSpace(fileContent))
						{
							File.Delete(file);
							continue;
						}
						var json = JsonNode.Parse(fileContent)!;

						string? installLocation = null;

						using (RegistryKey? key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey((json as JsonObject)?["RegistryPath"]?.GetValue<string>() ?? ""))
						{
							if (key != null)
							{
								installLocation = key.GetValue((json as JsonObject)?["RegistryKey"]?.GetValue<string>())?.ToString()?.TrimEnd('\\', '/');
							}
						}

						if (Directory.Exists(installLocation))
						{
							string? provider = (json as JsonObject)?["Provider"]?.GetValue<string>();

							string? gameId = null;
							if (provider == "UbisoftConnect")
							{
								gameId = (json as JsonObject)?["GameID"]?.GetValue<string>();
								provider = "Ubisoft Connect";
							}

							allManifests.Add(new JsonObject
							{
								["Provider"] = provider,
								["bIsApplication"] = true,
								["AppCategories"] = new JsonArray(JsonValue.Create("games")),
								["CatalogItemId"] = (json as JsonObject)?["CatalogID"]?.GetValue<string>(),
								["CatalogNamespace"] = (json as JsonObject)?["Namespace"]?.GetValue<string>(),
								["AppName"] = (json as JsonObject)?["AppName"]?.GetValue<string>(),
								["DisplayName"] = (json as JsonObject)?["Title"]?.GetValue<string>(),
								["InstallLocation"] = installLocation,
								["GameID"] = gameId,
								["LaunchExecutable"] = (json as JsonObject)?["MainWindowProcessName"]?.GetValue<string>(),
								["ProcessNames"] = (json as JsonObject)?["ProcessNames"]?.AsArray().DeepClone()
							});
						}
					}
					catch
					{
						continue;
					}
				}
			}

			// for each manifest
			await Parallel.ForEachAsync(allManifests, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount * 2 }, async (itemJson, _) =>
			{
				try
				{
					using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
					CancellationToken token = cts.Token;

					// return if not a game
					if (itemJson is not JsonObject manifest) return;
					if (manifest?["bIsApplication"]?.GetValue<bool>() != true) return;
					JsonArray? appCategories = manifest?["AppCategories"] as JsonArray;
					if (appCategories == null || !appCategories.Any(children => children?.GetValue<string>()?.Equals("games", StringComparison.OrdinalIgnoreCase) == true)) return;
					string catalogItemId = manifest?["CatalogItemId"]?.GetValue<string>() ?? "";
					string catalogNamespace = manifest?["CatalogNamespace"]?.GetValue<string>() ?? "";
					string appName = manifest?["AppName"]?.GetValue<string>() ?? "";

					if (catalogItemId == "1e8bda5cfbb641b9a9aea8bd62285f73")
						appName = manifest?["MainGameAppName"]?.GetValue<string>() ?? appName;

					// return if not in library
					if (!libraryData.Any(x => (x as JsonObject)?["catalogItemId"]?.ToString() == catalogItemId))
						return;

					string installLocation = manifest?["InstallLocation"]?.GetValue<string>()?.Replace("/", "\\") ?? "";
					if (!Directory.Exists(installLocation))
						return;

					TlsResponse offerResponse = await tlsClient.Value.PostAsync(
						"https://store.epicgames.com/graphql",
						new JsonObject
						{
							["query"] = itemOfferQuery,
							["variables"] = new JsonObject
							{
								["allowCountries"] = "US",
								["country"] = "US",
								["locale"] = "en-US",
								["count"] = 1,
								["namespace"] = catalogNamespace
							}
						}.ToJsonString(),
						new Dictionary<string, string> { { "Content-Type", "application/json" } });

					// get offer id, product slug and product id
					string? offerId = null;
					string? productSlug = null;
					string? productId = null;
					if (offerResponse.IsSuccess)
					{
						JsonArray? elements = ((((JsonNode.Parse(offerResponse.Body ?? "") as JsonObject)?["data"] as JsonObject)?["Catalog"] as JsonObject)?["searchStore"] as JsonObject)?["elements"] as JsonArray;
						JsonNode? searchElement = elements != null && elements.Count > 0 ? elements[0] : null;

						offerId = (searchElement as JsonObject)?["id"]?.GetValue<string>();

						JsonArray? mappings = ((searchElement as JsonObject)?["catalogNs"] as JsonObject)?["mappings"] as JsonArray;
						if (mappings != null)
						{
							JsonNode? homeMapping = mappings.FirstOrDefault(mapping => (mapping as JsonObject)?["pageType"]?.GetValue<string>() == "productHome");
							if (homeMapping != null)
							{
								productSlug = (homeMapping as JsonObject)?["pageSlug"]?.GetValue<string>();
								productId = (homeMapping as JsonObject)?["productId"]?.GetValue<string>();
							}
							else
							{
								JsonNode? offerMapping = mappings.FirstOrDefault(m => (m as JsonObject)?["pageType"]?.GetValue<string>() == "offer");
								if (offerMapping != null)
								{
									productSlug = (offerMapping as JsonObject)?["pageSlug"]?.GetValue<string>();
									productId = (offerMapping as JsonObject)?["productId"]?.GetValue<string>();
								}
								else if (mappings.Count > 0)
								{
									productSlug = (mappings[0] as JsonObject)?["pageSlug"]?.GetValue<string>();
									productId = (mappings[0] as JsonObject)?["productId"]?.GetValue<string>();
								}
							}
						}

						if (string.IsNullOrEmpty(productSlug) && searchElement != null)
						{
							productSlug = (searchElement as JsonObject)?["productSlug"]?.GetValue<string>() ?? (searchElement as JsonObject)?["urlSlug"]?.GetValue<string>();
						}
					}

					if (string.IsNullOrEmpty(offerId))
					{
						if (catalogItemId != "6e563a2c0f5f46e3b4e88b5f4ed50cca")
							LogHelper.LogError(new InvalidOperationException($"Failed to get offerId for {catalogItemId}"), null, $"Failed to get offerId for game {(itemJson as JsonObject)?["DisplayName"]?.ToString()}, {catalogItemId}");
						return;
					}

					// Fetch metadata and assets
					string manifestUrl = $"https://catalog-public-service-prod.ol.epicgames.com/catalog/api/shared/namespace/{catalogNamespace}/bulk/items?id={catalogItemId}&includeDLCDetails=false&includeMainGameDetails=true&country=US&locale=en-US";
					string manifestFallbackUrl = $"https://catalog-public-service-prod06.ol.epicgames.com/catalog/api/shared/namespace/{catalogNamespace}/bulk/items?id={catalogItemId}&includeDLCDetails=false&includeMainGameDetails=true&country=US&locale=en-US";
					string offerUrl = $"https://catalog-public-service-prod.ol.epicgames.com/catalog/api/shared/bulk/offers?id={offerId}&returnItemDetails=true&country=US&locale=en-US";
					string offerFallbackUrl = $"https://catalog-public-service-prod06.ol.epicgames.com/catalog/api/shared/bulk/offers?id={offerId}&returnItemDetails=true&country=US&locale=en-US";
					string productOfferUrl = $"https://egs-platform-service.store.epicgames.com/api/v1/egs/products/{productId}/offers/{offerId}?country={region}&locale=en-US&store=EGS";
					string ageRatingUrl = $"https://egs-platform-service.store.epicgames.com/api/v1/egs/products/{productId}/offers/{offerId}/age-rating?country={region}&locale=en-US&store=EGS";
					JsonNode manifestData = null!;
					JsonNode offerData = null!;
					JsonNode ratingData = null!;
					JsonNode productOfferData = null!;
					JsonNode ageRatingData = null!;

					Task manifestTask = new Func<Task>(async () =>
					{
						try
						{
							manifestData = JsonNode.Parse(await loginClient.GetStringAsync(manifestUrl, token).ConfigureAwait(false))!;
						}
						catch (Exception)
						{
							try
							{
								manifestData = JsonNode.Parse(await loginClient.GetStringAsync(manifestFallbackUrl, token).ConfigureAwait(false))!;
							}
							catch (Exception fallbackEx)
							{
								if (fallbackEx is not OperationCanceledException oce || oce.CancellationToken != token)
									LogHelper.LogError(fallbackEx, null, $"Failed to load manifest data for game {(itemJson as JsonObject)?["DisplayName"]?.ToString()}, {catalogItemId}, both {manifestUrl} and {manifestFallbackUrl}");
							}
						}
					})();

					Task offerTask = new Func<Task>(async () =>
					{
						try
						{
							offerData = JsonNode.Parse(await loginClient.GetStringAsync(offerUrl, token).ConfigureAwait(false))!;
						}
						catch (Exception)
						{
							try
							{
								offerData = JsonNode.Parse(await loginClient.GetStringAsync(offerFallbackUrl, token).ConfigureAwait(false))!;
							}
							catch (Exception fallbackEx)
							{
								if (fallbackEx is not OperationCanceledException oce || oce.CancellationToken != token)
									LogHelper.LogError(fallbackEx, null, $"Failed to load offer data for game {(itemJson as JsonObject)?["DisplayName"]?.ToString()}, {offerId}, both {offerUrl} and {offerFallbackUrl}");
							}
						}
					})();

					Task ratingTask = new Func<Task>(async () =>
					{
						try
						{
							TlsResponse ratingResponse = await tlsClient.Value.PostAsync(
								"https://store.epicgames.com/graphql",
								new JsonObject
								{
									["query"] = ratingQuery,
									["variables"] = new JsonObject
									{
										["sandboxId"] = catalogNamespace,
										["locale"] = "en-US"
									}
								}.ToJsonString(),
								new Dictionary<string, string> { { "Content-Type", "application/json" } });
							ratingData = ratingResponse.IsSuccess ? JsonNode.Parse(ratingResponse.Body ?? "")! : JsonNode.Parse("{}")!;
						}
						catch (Exception ex)
						{
							LogHelper.LogError(ex, null, $"Failed to load rating data for game {(itemJson as JsonObject)?["DisplayName"]?.ToString()}, {catalogNamespace}");
						}
					})();

					Task productOfferTask = new Func<Task>(async () =>
					{
						try
						{
							TlsResponse productOfferResponse = await tlsClient.Value.GetAsync(productOfferUrl);
							productOfferData = productOfferResponse.IsSuccess ? JsonNode.Parse(productOfferResponse.Body ?? "")! : JsonNode.Parse("{}")!;
						}
						catch (Exception ex)
						{
							LogHelper.LogError(ex, null, $"Failed to load product offer data for game {(itemJson as JsonObject)?["DisplayName"]?.ToString()}, {productId}, {offerId}, {productOfferUrl}");
						}
					})();

					Task ageRatingTask = new Func<Task>(async () =>
					{
						try
						{
							TlsResponse ageRatingResponse = await tlsClient.Value.GetAsync(ageRatingUrl);
							ageRatingData = ageRatingResponse.IsSuccess ? JsonNode.Parse(ageRatingResponse.Body ?? "")! : JsonNode.Parse("{}")!;
						}
						catch (Exception ex)
						{
							LogHelper.LogError(ex, null, $"Failed to load age rating data for game {(itemJson as JsonObject)?["DisplayName"]?.ToString()}, {offerId}, {ageRatingUrl}");
						}
					})();

					await Task.WhenAll(manifestTask, offerTask, ratingTask, productOfferTask, ageRatingTask).ConfigureAwait(false);

					// get description
					JsonNode? offerEntry = (offerData as JsonObject)?[offerId];
					JsonNode? manifestEntry = (manifestData as JsonObject)?[catalogItemId];
					string? description = (offerEntry as JsonObject)?["description"]?.GetValue<string>();

					if ((offerEntry as JsonObject)?["offerType"]?.GetValue<string>() != "BASE_GAME")
					{
						description = (manifestEntry as JsonObject)?["description"]?.GetValue<string>();
					}

					// get key images
					JsonArray? keyImages = (manifestEntry as JsonObject)?["keyImages"] as JsonArray;
					if (keyImages == null) keyImages = [];

					// get artifactid
					JsonArray? releaseInfo = (manifestEntry as JsonObject)?["releaseInfo"] as JsonArray;
					string artifactId = releaseInfo != null && releaseInfo.Count > 0 ? (releaseInfo[0] as JsonObject)?["appId"]?.ToString() ?? "" : "";
					if (string.IsNullOrEmpty(artifactId))
						LogHelper.LogError(new InvalidOperationException($"Failed to get artifactId for {catalogItemId}"), null, $"Failed to get artifactId for game {(itemJson as JsonObject)?["DisplayName"]?.ToString()}, {catalogItemId}");

					// read playtime json data
					int totalSeconds = playTimeData?.GetValueOrDefault(artifactId) ?? 0;

					var ts = TimeSpan.FromSeconds(totalSeconds);
					string playTime = ts.TotalHours >= 1
						? $"{(int)ts.TotalHours}h {ts.Minutes}m"
						: $"{ts.Minutes}m";

					// get latest version
					string? currentVersion = (itemJson as JsonObject)?["AppVersionString"]?.GetValue<string>();
					string? latestVersion = buildData?.FirstOrDefault(x => (x as JsonObject)?["appName"]?.ToString() == (itemJson as JsonObject)?["AppName"]?.GetValue<string>()) is JsonNode buildEntry ? (buildEntry as JsonObject)?["buildVersion"]?.ToString() : null;

					if (string.IsNullOrEmpty(currentVersion))
						currentVersion = latestVersion;

					string? releaseDateStr = (offerEntry as JsonObject)?["releaseDate"]?.GetValue<string>();
					DateTimeOffset releaseDate = DateTimeOffset.TryParse(releaseDateStr, out DateTimeOffset parsedRelease) ? parsedRelease : DateTimeOffset.MinValue;

					long? sizeBytes = (itemJson as JsonObject)?["InstallSize"]?.GetValue<long>();

					if (!sizeBytes.HasValue)
						sizeBytes = new DirectoryInfo(installLocation).EnumerateFiles("*", System.IO.SearchOption.AllDirectories).Sum(fi => fi.Length);

					// get screenshots
					JsonArray? keyImagesList = (offerEntry as JsonObject)?["keyImages"] as JsonArray;
					var screenshots = new List<string>();
					foreach (JsonNode? image in keyImagesList ?? [])
					{
						if ((image as JsonObject)?["type"]?.GetValue<string>() == "featuredMedia")
						{
							string? url = (image as JsonObject)?["url"]?.GetValue<string>();
							if (!string.IsNullOrEmpty(url))
							{
								screenshots.Add(url);
							}
						}
					}

					if (screenshots.Count == 0 && !string.IsNullOrEmpty(productSlug))
					{
						string cmsUrl = $"https://store-content-ipv4.ak.epicgames.com/api/en-US/content/products/{productSlug}";
						TlsResponse cmsResponse = await tlsClient.Value.GetAsync(cmsUrl).ConfigureAwait(false);
						if (cmsResponse.IsSuccess)
						{
							var cmsJson = JsonNode.Parse(cmsResponse.Body ?? "");
							JsonArray? pages = (cmsJson as JsonObject)?["pages"] as JsonArray;
							if (pages != null)
							{
								var sortedPages = pages.OrderByDescending(page => (page as JsonObject)?["type"]?.GetValue<string>() == "productHome").ToList();
								foreach (JsonNode? page in sortedPages)
								{
									JsonArray? carouselItems = (((page as JsonObject)?["data"] as JsonObject)?["carousel"] as JsonObject)?["items"] as JsonArray;
									if (carouselItems != null)
									{
										foreach (JsonNode? item in carouselItems)
										{
											if (item == null) continue;

											string? src = ((item as JsonObject)?["image"] as JsonObject)?["src"]?.GetValue<string>();
											if (!string.IsNullOrEmpty(src))
											{
												if (!screenshots.Contains(src))
												{
													screenshots.Add(src);
												}
												continue;
											}

											JsonNode? videoNode = (item as JsonObject)?["video"];
											if (videoNode != null)
											{
												string? poster = (videoNode as JsonObject)?["poster"]?.GetValue<string>();
												if (!string.IsNullOrEmpty(poster))
												{
													if (!screenshots.Contains(poster))
													{
														screenshots.Add(poster);
													}
													continue;
												}

												string? videoThumb = (videoNode as JsonObject)?["thumbnail"]?.GetValue<string>();
												if (!string.IsNullOrEmpty(videoThumb))
												{
													if (!screenshots.Contains(videoThumb))
													{
														screenshots.Add(videoThumb);
													}
													continue;
												}

												string? recipesStr = (videoNode as JsonObject)?["recipes"]?.GetValue<string>();
												if (!string.IsNullOrEmpty(recipesStr))
												{
													var recipesJson = JsonNode.Parse(recipesStr);
													if (recipesJson != null)
													{
														string? thumbnail = (recipesJson as JsonObject)?["thumbnail"]?.GetValue<string>();
														if (!string.IsNullOrEmpty(thumbnail))
														{
															if (!screenshots.Contains(thumbnail))
															{
																screenshots.Add(thumbnail);
															}
															continue;
														}

														if (recipesJson is JsonObject obj)
														{
															foreach (KeyValuePair<string, JsonNode?> kvp in obj)
															{
																if (kvp.Value is JsonArray recipeArray)
																{
																	foreach (JsonNode? recipe in recipeArray)
																	{
																		JsonArray? outputs = (recipe as JsonObject)?["outputs"] as JsonArray;
																		if (outputs != null)
																		{
																			foreach (JsonNode? output in outputs)
																			{
																				if ((output as JsonObject)?["key"]?.GetValue<string>() == "thumbnail")
																				{
																					string? url = (output as JsonObject)?["url"]?.GetValue<string>();
																					if (!string.IsNullOrEmpty(url))
																					{
																						if (!screenshots.Contains(url))
																						{
																							screenshots.Add(url);
																						}
																					}
																				}
																			}
																		}
																	}
																}
															}
														}
													}
												}
											}
										}
									}

									JsonArray? galleryImages = (((page as JsonObject)?["data"] as JsonObject)?["gallery"] as JsonObject)?["galleryImages"] as JsonArray;
									if (galleryImages != null)
									{
										foreach (JsonNode? img in galleryImages)
										{
											string? src = (img as JsonObject)?["src"]?.GetValue<string>();
											if (!string.IsNullOrEmpty(src))
											{
												if (!screenshots.Contains(src))
												{
													screenshots.Add(src);
												}
											}
										}
									}

									if (screenshots.Count > 0)
									{
										break;
									}
								}
							}
						}
					}

					JsonArray? ratingDescriptors = ((ageRatingData as JsonObject)?["ageRating"] as JsonObject)?["contentDescriptors"] as JsonArray;
					string? ratingDescription = ratingDescriptors != null
						? string.Join(", ", ratingDescriptors.Select(descriptor => descriptor?.ToString()).Where(descriptor => !string.IsNullOrWhiteSpace(descriptor)))
						: null;

					JsonArray? interactiveElms = ((ageRatingData as JsonObject)?["ageRating"] as JsonObject)?["interactiveElements"] as JsonArray;
					string? interactiveElements = interactiveElms != null
						? string.Join(", ", interactiveElms.Select(element => element?.ToString()).Where(element => !string.IsNullOrWhiteSpace(element)))
						: null;

					games.Add(new GameModel
					{
						Launcher = (itemJson as JsonObject)?["Provider"]?.GetValue<string>() ?? "Epic Games",
						CatalogNamespace = catalogNamespace,
						CatalogItemId = catalogItemId,
						AppName = appName,
						InstallLocation = installLocation,
						LaunchCommand = (itemJson as JsonObject)?["LaunchCommand"]?.GetValue<string>(),
						LaunchExecutable = (itemJson as JsonObject)?["LaunchExecutable"]?.GetValue<string>()?.Replace("/", "\\"),
						GameID = (itemJson as JsonObject)?["GameID"]?.GetValue<string>(),
						ProcessNames = ((itemJson as JsonObject)?["ProcessNames"] as JsonArray)?.Select(p => p!.GetValue<string>()).ToList() ?? [],
						ArtifactId = artifactId,
						UpdateIsAvailable = latestVersion != null && latestVersion != currentVersion,
						ImageUrl = keyImages.FirstOrDefault(image => (image as JsonObject)?["type"]?.GetValue<string>() == "DieselGameBoxTall") is JsonNode tallBox ? (tallBox as JsonObject)?["url"]?.GetValue<string>() : null,
						BackgroundImageUrl = keyImages.FirstOrDefault(image => (image as JsonObject)?["type"]?.GetValue<string>() == "DieselGameBox") is JsonNode box ? (box as JsonObject)?["url"]?.GetValue<string>() : null,
						Title = (offerEntry as JsonObject)?["title"]?.GetValue<string>(),
						Developers = ((offerEntry as JsonObject)?["seller"] as JsonObject)?["name"]?.GetValue<string>(),
						Genres = ((((productOfferData as JsonObject)?["tags"] as JsonObject)?["genres"] as JsonArray) ?? []).Select(genre => (genre as JsonObject)?["name"]?.GetValue<string>()!).Where(n => !string.IsNullOrWhiteSpace(n)).ToList(),
						Features = ((((productOfferData as JsonObject)?["tags"] as JsonObject)?["features"] as JsonArray) ?? []).Select(feature => (feature as JsonObject)?["name"]?.GetValue<string>()!).Where(f => !string.IsNullOrWhiteSpace(f)).ToList(),
						Rating = ((((ratingData as JsonObject)?["data"] as JsonObject)?["RatingsPolls"] as JsonObject)?["getProductResult"] as JsonObject)?["averageRating"]?.GetValue<double?>() ?? 0.0,
						PlayTime = playTime,
						AgeRatingUrl = ((ageRatingData as JsonObject)?["ageRating"] as JsonObject)?["ratingImage"]?.ToString(),
						AgeRatingTitle = ((ageRatingData as JsonObject)?["ageRating"] as JsonObject)?["title"]?.ToString(),
						AgeRatingDescription = ratingDescription,
						Elements = interactiveElements,
						Description = description,
						Screenshots = screenshots,
						ReleaseDate = releaseDate.ToString("d"),
						Size = sizeBytes >= 1_000_000_000 ? $"{sizeBytes.Value / 1_000_000_000d:F1} GB" : $"{sizeBytes.Value / 1_000_000d:F2} MB",
						Version = currentVersion
					});

					if (keyImages.FirstOrDefault(image => (image as JsonObject)?["type"]?.GetValue<string>() == "DieselGameBox") == null)
						LogHelper.LogError(new UriFormatException($"No background image found for game: {(offerEntry as JsonObject)?["title"]?.GetValue<string>() ?? "Unknown"} ({(itemJson as JsonObject)?["Provider"]?.GetValue<string>() ?? "Epic Games"})"));
				}
				catch (Exception ex)
				{
					LogHelper.LogError(ex, null);
				}
			});
		}
		return [.. games];
	}
}

internal record PlaytimePayload(
	[property: JsonPropertyName("machineId")] string MachineId,
	[property: JsonPropertyName("artifactId")] string ArtifactId,
	[property: JsonPropertyName("startTime")] string StartTime,
	[property: JsonPropertyName("endTime")] string EndTime,
	[property: JsonPropertyName("startSegment")] bool StartSegment,
	[property: JsonPropertyName("endSegment")] bool EndSegment
);

[JsonSerializable(typeof(PlaytimePayload))]
internal partial class PlaytimeJsonContext : JsonSerializerContext { }
