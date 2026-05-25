using AutoOS.Core.Common;
using System.Text.Json.Nodes;

namespace AutoOS.Core.Helpers.Database;

public static partial class DiscordHelper
{
    public static readonly string DiscordRoamingPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "discord");

    public static async Task ImportAccount(IStatusReporter reporter = null)
    {
        // get all leveldb folders from other drives
        //var systemDrive = Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.System));
        //var foundFolders = DriveInfo.GetDrives()
        //    .Where(d => d.DriveType == DriveType.Fixed && d.Name != systemDrive)
        //    .SelectMany(d =>
        //    {
        //        string usersPath = Path.Combine(d.Name, "Users");
        //        if (!Directory.Exists(usersPath)) return [];

        //        return Directory.GetDirectories(usersPath)
        //            .Select(userDir => Path.Combine(userDir, "AppData", "Roaming", "discord", "Local Storage", "leveldb"))
        //            .Where(Directory.Exists);
        //    })
        //    .Select(path => new DirectoryInfo(path))
        //    .ToList();

        //DirectoryInfo newestFolder = null;

        //// check if folders contain valid accounts
        //foreach (var folder in foundFolders)
        //{
        //    string localStoragePath = Path.GetDirectoryName(folder.FullName);
        //    string discordPath = Path.GetDirectoryName(localStoragePath);
        //    string databasePath = Path.Combine(discordPath, "Local Storage", "leveldb");

        //    var accounts = GetAccountData(databasePath);

        //    if (accounts != null && accounts.Count > 0)
        //    {
        //        // use the latest one
        //        if (newestFolder == null || folder.LastWriteTime > newestFolder.LastWriteTime)
        //        {
        //            newestFolder = folder;

        //            // create destination directory
        //            Directory.CreateDirectory(DiscordRoamingPath);

        //            // copy Local Storage folder
        //            string sourceLocalStoragePath = Path.Combine(discordPath, "Local Storage");
        //            string destLocalStoragePath = Path.Combine(DiscordRoamingPath, "Local Storage");

        //            if (Directory.Exists(sourceLocalStoragePath))
        //            {
        //                Directory.CreateDirectory(destLocalStoragePath);

        //                foreach (var directory in Directory.GetDirectories(sourceLocalStoragePath, "*", SearchOption.AllDirectories))
        //                {
        //                    string subDirPath = directory.Replace(sourceLocalStoragePath, destLocalStoragePath);
        //                    Directory.CreateDirectory(subDirPath);
        //                }

        //                foreach (var file in Directory.GetFiles(sourceLocalStoragePath, "*.*", SearchOption.AllDirectories))
        //                {
        //                    string destFilePath = file.Replace(sourceLocalStoragePath, destLocalStoragePath);
        //                    File.Copy(file, destFilePath, true);
        //                }
        //            }

        //            // copy Local State file
        //            string sourceLocalStatePath = Path.Combine(discordPath, "Local State");
        //            string destLocalStatePath = Path.Combine(DiscordRoamingPath, "Local State");

        //            if (File.Exists(sourceLocalStatePath))
        //            {
        //                File.Copy(sourceLocalStatePath, destLocalStatePath, true);
        //            }

        //            var accountNames = accounts.Select(a => a.Username).ToList();
        //            string accountsString = accountNames.Count switch
        //            {
        //                1 => accountNames[0],
        //                2 => $"{accountNames[0]} and {accountNames[1]}",
        //                _ => $"{string.Join(", ", accountNames.Take(accountNames.Count - 1))}, and {accountNames.Last()}"
        //            };

        //            reporter?.SetTitle($"Successfully logged in as {accountsString}...");

        //            await Task.Delay(1000);

        //            return;
        //        }
        //    }
        //}

        var browserPaths = new Dictionary<string, string>
        {
            { @"AppData\Local\Google\Chrome\User Data\Default\Local Storage\leveldb", "Chrome" },
            { @"AppData\Local\Thorium\User Data\Default\Local Storage\leveldb", "Thorium" },
            { @"AppData\Local\imput\Helium\User Data\Default\Local Storage\leveldb", "Helium" },
            { @"AppData\Local\BraveSoftware\Brave-Browser\User Data\Default\Local Storage\leveldb", "Brave" },
            { @"AppData\Local\Vivaldi\User Data\Default\Local Storage\leveldb", "Vivaldi" },
            { @"AppData\Local\Packages\TheBrowserCompany.Arc_ttt1ap7aakyb4\LocalCache\Local\Arc\User Data\Default\Local Storage\leveldb", "Arc" },
            { @"AppData\Local\Perplexity\Comet\User Data\Default\Local Storage\leveldb", "Perplexity" }
        };

		var systemDrive = Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.System));
		var foundDatabasePaths = DriveInfo.GetDrives()
            .Where(d => d.DriveType == DriveType.Fixed && d.Name != systemDrive)
            .SelectMany(d =>
            {
                string usersPath = Path.Combine(d.Name, "Users");
                if (!Directory.Exists(usersPath)) return [];

                return Directory.GetDirectories(usersPath)
                    .SelectMany(userDir => browserPaths.Keys.Select(browserPath => new { Path = Path.Combine(userDir, browserPath), Browser = browserPaths[browserPath] }))
                    .Where(x => Directory.Exists(x.Path));
            })
            .Select(x => new { Path = new DirectoryInfo(x.Path), x.Browser })
            .OrderByDescending(x => x.Path.LastWriteTime)
            .ToList();

        string foundDatabasePath = null;
        string foundBrowser = null;
        string foundToken = null;

        foreach (var databasePath in foundDatabasePaths)
        {
            try
            {
                var tokenNode = DatabaseHelper.Read(databasePath.Path.FullName, "_https://discord.com", "token");
                string token = tokenNode?.ToString();

                if (!string.IsNullOrEmpty(token))
                {
                    foundDatabasePath = databasePath.Path.FullName;
                    foundBrowser = databasePath.Browser;
                    foundToken = token;
                    break;
                }
            }
            catch
            {
                continue;
            }
        }

        if (!string.IsNullOrEmpty(foundToken))
        {
            DatabaseHelper.Write(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "discord", "Local Storage", "leveldb"), "_https://discord.com", "token", foundToken);

            var accounts = GetAccountData(foundDatabasePath);
            if (accounts != null && accounts.Count > 0)
            {
                var accountNames = accounts.Select(a => a.Username).ToList();
                string accountsString = accountNames.Count switch
                {
                    1 => accountNames[0],
                    2 => $"{accountNames[0]} and {accountNames[1]}",
                    _ => $"{string.Join(", ", accountNames.Take(accountNames.Count - 1))}, and {accountNames.Last()}"
                };

                reporter?.SetTitle($"Successfully logged in as {accountsString} from {foundBrowser}...");
                await Task.Delay(1000);
            }
        }
    }

    public class DiscordAccountInfo
    {
        public string UserId { get; set; }
        public string Username { get; set; }
        public string Avatar { get; set; }
        public bool IsActive { get; set; }
        public string Origin { get; set; }
    }

	public static List<DiscordAccountInfo> GetAccountData(string databasePath)
	{
		JsonNode multiAccountStore = DatabaseHelper.Read(databasePath, "_https://discord.com", "MultiAccountStore");
		JsonNode userIdCache = DatabaseHelper.Read(databasePath, "_https://discord.com", "user_id_cache");

		if (multiAccountStore != null)
		{
			var accounts = new List<DiscordAccountInfo>();
			JsonNode users = multiAccountStore["_state"]?["users"];

			if (users != null && users is JsonArray usersArray)
			{
				foreach (JsonNode user in usersArray)
				{
					string id = user?["id"]?.ToString();
					string username = user?["username"]?.ToString();
					string avatar = user?["avatar"]?.ToString();
					bool isActive = id == userIdCache?.ToString();

					accounts.Add(new DiscordAccountInfo { UserId = id, Username = username, Avatar = avatar, IsActive = isActive });
				}
			}

			return accounts;
		}
		return null;
	}

	public static void SetSystemAppearance(string databasePath)
	{
		JsonNode jsonContent = new JsonObject
		{
			["_state"] = new JsonObject
			{
				["darkSidebar"] = false,
				["hdrDynamicRange"] = "no-limit",
				["useSystemTheme"] = 2
			},
			["_version"] = 2
		};
		DatabaseHelper.Write(databasePath, "_https://discord.com", "UnsyncedUserSettingsStore", jsonContent);
	}

	public static void DisableGameOverlay(string databasePath)
	{
		JsonNode jsonContent = new JsonObject
		{
			["legacyEnabled"] = false,
			["oopEnabled"] = false
		};
		DatabaseHelper.Write(databasePath, "_https://discord.com", "OverlayStore6", jsonContent);
	}

	public static void DisableClips(string databasePath)
	{
		JsonNode clipsNode = DatabaseHelper.Read(databasePath, "https://discordapp.com", "ClipsStore");

		if (clipsNode != null)
		{
			clipsNode["_state"]?["clipsSettings"]?["clipsEnabled"] = false;
			DatabaseHelper.Write(databasePath, "https://discord.com", "ClipsStore", clipsNode);
		}
	}
}