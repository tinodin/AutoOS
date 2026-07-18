using AutoOS.Core.Common;
using System.Diagnostics;
using System.Text.Json.Nodes;
using Windows.Win32.Foundation;
using Windows.Win32;

namespace AutoOS.Core.Helpers.Database;

public static partial class DiscordHelper
{
	public class DiscordAccountInfo
	{
		public string UserId { get; set; }
		public string Username { get; set; }
		public string Avatar { get; set; }
		public bool IsActive { get; set; }
		public string Origin { get; set; }
		public bool IsMember { get; set; }
	}

	public static readonly string LevelDbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "discord", "Local Storage", "leveldb");

	private static IEnumerable<(string Path, string Browser, DateTime LastWriteTime)> GetBrowserDatabases(string userDir)
	{
		var chromium = new[]
		{
			(@"AppData\Local\Microsoft\Edge\User Data\Default\Local Storage\leveldb", "Edge"),
			(@"AppData\Local\Google\Chrome\User Data\Default\Local Storage\leveldb", "Chrome"),
			(@"AppData\Local\Thorium\User Data\Default\Local Storage\leveldb", "Thorium"),
			(@"AppData\Local\imput\Helium\User Data\Default\Local Storage\leveldb", "Helium"),
			(@"AppData\Local\BraveSoftware\Brave-Browser\User Data\Default\Local Storage\leveldb", "Brave"),
			(@"AppData\Local\Vivaldi\User Data\Default\Local Storage\leveldb", "Vivaldi"),
			(@"AppData\Local\Packages\TheBrowserCompany.Arc_ttt1ap7aakyb4\LocalCache\Local\Arc\User Data\Default\Local Storage\leveldb", "Arc"),
			(@"AppData\Local\Perplexity\Comet\User Data\Default\Local Storage\leveldb", "Perplexity")
		}
		.Where(entry => Directory.Exists(Path.Combine(userDir, entry.Item1)))
		.Select(entry => (Path: Path.Combine(userDir, entry.Item1), Browser: entry.Item2, LastWriteTime: new DirectoryInfo(Path.Combine(userDir, entry.Item1)).LastWriteTime));

		var firefox = new[]
		{
			(@"AppData\Roaming\Floorp\Profiles", "Floorp"),
			(@"AppData\Roaming\librewolf\Profiles", "LibreWolf"),
			(@"AppData\Roaming\Mozilla\Firefox\Profiles", "Firefox"),
			(@"AppData\Roaming\Mullvad\MullvadBrowser\Profiles", "MullvadBrowser"),
			(@"AppData\Roaming\Waterfox\Profiles", "Waterfox"),
			(@"AppData\Roaming\zen\Profiles", "Zen")
		}
		.Where(entry => Directory.Exists(Path.Combine(userDir, entry.Item1)))
		.SelectMany(entry => Directory.GetDirectories(Path.Combine(userDir, entry.Item1)).Select(profile => (profile, entry.Item2)))
		.SelectMany(profileEntry => new[] { Path.Combine(profileEntry.profile, "storage", "default", "https+++discord.com", "ls", "data.sqlite"), Path.Combine(profileEntry.profile, "storage", "default", "https+++discordapp.com", "ls", "data.sqlite") }.Select(path => (Path: path, Browser: profileEntry.Item2)))
		.Where(pathEntry => File.Exists(pathEntry.Path))
		.Select(pathEntry => (pathEntry.Path, pathEntry.Browser, LastWriteTime: File.GetLastWriteTime(pathEntry.Path)));

		return chromium.Concat(firefox);
	}

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
		//            Directory.CreateDirectory(RoamingPath);

		//            // copy Local Storage folder
		//            string sourceLocalStoragePath = Path.Combine(discordPath, "Local Storage");
		//            string destLocalStoragePath = Path.Combine(RoamingPath, "Local Storage");

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
		//            string destLocalStatePath = Path.Combine(RoamingPath, "Local State");

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

		var systemDrive = Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.System));
		var foundDatabasePaths = DriveInfo.GetDrives()
			.Where(drive => drive.DriveType == DriveType.Fixed && drive.Name != systemDrive)
			.SelectMany(drive =>
			{
				string usersPath = Path.Combine(drive.Name, "Users");
				if (!Directory.Exists(usersPath)) return [];

				return Directory.GetDirectories(usersPath).SelectMany(GetBrowserDatabases);
			})
			.OrderByDescending(pathEntry => pathEntry.LastWriteTime)
			.ToList();

		string foundDatabasePath = null;
		string foundBrowser = null;
		string foundToken = null;

		foreach (var databasePath in foundDatabasePaths)
		{
			try
			{
				var tokenNode = DatabaseHelper.Read(databasePath.Path, "_https://discord.com", "token");
				string token = tokenNode?.ToString();

				if (!string.IsNullOrEmpty(token))
				{
					foundDatabasePath = databasePath.Path;
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
			DatabaseHelper.Write(LevelDbPath, "_https://discord.com", "token", foundToken);

			var accounts = GetAccountData(foundDatabasePath);
			if (accounts != null && accounts.Count > 0)
			{
				var accountNames = accounts.Select(account => account.Username).ToList();
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

	public static async Task ImportKeybinds(IStatusReporter reporter = null)
	{
		var systemDrive = Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.System));
		var foundFolders = DriveInfo.GetDrives()
			.Where(drive => drive.DriveType == DriveType.Fixed && drive.Name != systemDrive)
			.SelectMany(drive =>
			{
				string usersPath = Path.Combine(drive.Name, "Users");
				if (!Directory.Exists(usersPath)) return [];

				return Directory.GetDirectories(usersPath)
					.Select(userDir => Path.Combine(userDir, "AppData", "Roaming", "discord", "Local Storage", "leveldb"))
					.Where(Directory.Exists)
					.Select(path => new DirectoryInfo(path));
			})
			.OrderByDescending(folder => folder.LastWriteTime)
			.ToList();

		DirectoryInfo newestFolder = null;
		string foundKeybinds = null;

		foreach (var folder in foundFolders)
		{
			try
			{
				var keybindsNode = DatabaseHelper.Read(folder.FullName, "_https://discord.com", "keybinds");
				string keybinds = keybindsNode?.ToString();

				if (!string.IsNullOrEmpty(keybinds))
				{
					newestFolder = folder;
					foundKeybinds = keybinds;
					break;
				}
			}
			catch
			{
				continue;
			}
		}

		if (!string.IsNullOrEmpty(foundKeybinds))
		{
			DatabaseHelper.Write(LevelDbPath, "_https://discord.com", "keybinds", foundKeybinds);
			reporter?.SetTitle("Successfully imported Discord keybinds...");
			await Task.Delay(1000);
		}
	}

	public static async Task KillDiscord()
	{
		foreach (Process process in Process.GetProcessesByName("Discord"))
		{
			if (process.MainWindowHandle != IntPtr.Zero)
			{
				PInvoke.PostMessage((HWND)process.MainWindowHandle, PInvoke.WM_CLOSE, default(WPARAM), default(LPARAM));
				process.WaitForExit();
			}
		}
	}

	public static List<DiscordAccountInfo> GetAccountData(string levelDbPath, string origin = null)
	{
		JsonNode multiAccountStore = DatabaseHelper.Read(levelDbPath, "_https://discord.com", "MultiAccountStore");
		JsonNode userIdCache = DatabaseHelper.Read(levelDbPath, "_https://discord.com", "user_id_cache");

		if (multiAccountStore != null)
		{
			var accounts = new List<DiscordAccountInfo>();
			JsonNode users = multiAccountStore["_state"]?["users"];
			JsonNode apexExperimentStore = DatabaseHelper.Read(levelDbPath, "_https://discord.com", "ApexExperimentStore");
			bool isMember = apexExperimentStore != null && apexExperimentStore.ToJsonString().Contains("1148987246746279977", StringComparison.Ordinal);

			if (users != null && users is JsonArray usersArray)
			{
				foreach (JsonNode user in usersArray)
				{
					string id = user?["id"]?.ToString();
					bool isActive = id == userIdCache?.ToString();

					accounts.Add(new DiscordAccountInfo
					{
						UserId = id,
						Username = user?["username"]?.ToString(),
						Avatar = user?["avatar"]?.ToString(),
						IsActive = isActive,
						Origin = origin,
						IsMember = isMember
					});
				}
			}

			return accounts;
		}

		return null;
	}

	public static List<DiscordAccountInfo> GetLocalAccounts()
	{
		var accounts = new List<DiscordAccountInfo>();

		var localDiscordAccounts = GetAccountData(LevelDbPath, "Discord");
		if (localDiscordAccounts != null)
			accounts.AddRange(localDiscordAccounts);

		var systemDrive = Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.System));
		string usersPath = Path.Combine(systemDrive, "Users");
		if (Directory.Exists(usersPath))
		{
			var localDbs = Directory.GetDirectories(usersPath)
				.SelectMany(GetBrowserDatabases)
				.OrderByDescending(db => db.LastWriteTime);

			foreach (var databasePath in localDbs)
			{
				var browserAccounts = GetAccountData(databasePath.Path, databasePath.Browser);
				if (browserAccounts != null)
					accounts.AddRange(browserAccounts);
			}
		}

		return accounts;
	}

	public static List<DiscordAccountInfo> GetOtherAccounts()
	{
		var accounts = new List<DiscordAccountInfo>();
		var systemDrive = Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.System));

		foreach (var drive in DriveInfo.GetDrives().Where(drive => drive.DriveType == DriveType.Fixed && drive.Name != systemDrive))
		{
			string driveLabel = drive.Name.TrimEnd('\\');
			string usersPath = Path.Combine(drive.Name, "Users");
			if (!Directory.Exists(usersPath))
				continue;

			var discordPaths = Directory.GetDirectories(usersPath)
				.Select(userDir => Path.Combine(userDir, "AppData", "Roaming", "discord", "Local Storage", "leveldb"))
				.Where(Directory.Exists)
				.Select(path => new DirectoryInfo(path))
				.OrderByDescending(dir => dir.LastWriteTime);

			foreach (var folder in discordPaths)
			{
				var discordAccounts = GetAccountData(folder.FullName, $"Discord ({driveLabel})");
				if (discordAccounts != null)
					accounts.AddRange(discordAccounts);
			}

			var otherDbs = Directory.GetDirectories(usersPath)
				.SelectMany(GetBrowserDatabases)
				.OrderByDescending(db => db.LastWriteTime);

			foreach (var databasePath in otherDbs)
			{
				var browserAccounts = GetAccountData(databasePath.Path, $"{databasePath.Browser} ({driveLabel})");
				if (browserAccounts != null)
					accounts.AddRange(browserAccounts);
			}
		}

		return accounts;
	}

	public static async Task SetSystemAppearance(string databasePath)
	{
		await KillDiscord();
		JsonNode UnsyncedUserSettingsStore = new JsonObject
		{
			["_state"] = new JsonObject
			{
				["darkSidebar"] = false,
				["hdrDynamicRange"] = "no-limit",
				["useSystemTheme"] = 2
			},
			["_version"] = 2
		};
		DatabaseHelper.Write(databasePath, "_https://discord.com", "UnsyncedUserSettingsStore", UnsyncedUserSettingsStore);

		JsonNode ThemeStore = new JsonObject
		{
			["_state"] = new JsonObject
			{
				["theme"] = "midnight",
				["preferences"] = new JsonObject
				{
					["dark"] = "dmidnightr",
					["light"] = "light",
					["unknown"] = "darker"
				},
				["status"] = 0
			},
			["_version"] = 2
		};
		DatabaseHelper.Write(databasePath, "_https://discord.com", "ThemeStore", ThemeStore);
	}

	public static async Task DisableGameOverlay(string databasePath)
	{
		await KillDiscord();
		JsonNode OverlayStore6 = new JsonObject
		{
			["legacyEnabled"] = false,
			["oopEnabled"] = false
		};
		DatabaseHelper.Write(databasePath, "_https://discord.com", "OverlayStore6", OverlayStore6);
	}

	public static async Task DisableClips(string databasePath)
	{
		await KillDiscord();
		JsonNode ClipsStore = DatabaseHelper.Read(databasePath, "https://discordapp.com", "ClipsStore");

		if (ClipsStore != null)
		{
			ClipsStore["_state"]?["clipsSettings"]?["clipsEnabled"] = false;
			DatabaseHelper.Write(databasePath, "https://discord.com", "ClipsStore", ClipsStore);
		}
	}
}
