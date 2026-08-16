using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
using AutoOS.Core.Common;
using AutoOS.Core.Data.Models.Games;
using Microsoft.VisualBasic.FileIO;
using ValveKeyValue;

namespace AutoOS.Core.Helpers.Games;

public static partial class SteamHelper
{
    public static readonly string SteamDir = (Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"Software\Valve\Steam")?.GetValue("InstallPath") as string ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam")).Replace('/', '\\');
    public static readonly string SteamPath = Path.Combine(SteamDir, "steam.exe");
    public static readonly string SteamLibraryPath = Path.Combine(SteamDir, @"steamapps\libraryfolders.vdf");
    public static readonly string SteamLibraryCacheDir = Path.Combine(SteamDir, @"appcache\librarycache");
    public static readonly string SteamLoginUsersPath = Path.Combine(SteamDir, "config", "loginusers.vdf");
    public static readonly string SteamUserDataDir = Path.Combine(SteamDir, "userdata");

    private static readonly HttpClient httpClient = new();

    public class SteamAccountInfo
    {
        public string AccountName { get; set; } = string.Empty;
        public bool MostRecent { get; set; }
        public bool AllowAutoLogin { get; set; }
        public string Steam64Id { get; set; } = string.Empty;
    }

    public static List<SteamAccountInfo> GetSteamAccounts()
    {
        if (!File.Exists(SteamLoginUsersPath))
            return [];

		string content = File.ReadAllText(SteamLoginUsersPath);
        if (string.IsNullOrWhiteSpace(content))
            return [];

        KVDocument kv;
        try
        {
            using var ms = new MemoryStream(Encoding.UTF8.GetBytes(content));
            kv = KVSerializer.Create(KVSerializationFormat.KeyValues1Text)
                .Deserialize(ms, new KVSerializerOptions { HasEscapeSequences = true });
        }
        catch
        {
            try
            {
                using var ms = new MemoryStream(Encoding.UTF8.GetBytes(content));
                kv = KVSerializer.Create(KVSerializationFormat.KeyValues1Text)
                    .Deserialize(ms, new KVSerializerOptions { HasEscapeSequences = false });
            }
            catch
            {
                return [];
            }
        }

        return
        [
            .. (kv?.Root?.Children ?? [])
                .Select(child =>
                {
                    if (child.Value is not KVObject accountData)
                        return null;

					string? accountName = accountData.FirstOrDefault(x => x.Key.Equals("AccountName", StringComparison.OrdinalIgnoreCase)).Value?.ToString();
                    if (string.IsNullOrWhiteSpace(accountName))
                        return null;

					string? mostRecent = accountData.FirstOrDefault(x => x.Key.Equals("MostRecent", StringComparison.OrdinalIgnoreCase)).Value?.ToString();
					string? autoLogin = accountData.FirstOrDefault(x => x.Key.Equals("AutoLogin", StringComparison.OrdinalIgnoreCase)).Value?.ToString();
					string? allowAutoLogin = accountData.FirstOrDefault(x => x.Key.Equals("AllowAutoLogin", StringComparison.OrdinalIgnoreCase)).Value?.ToString();
					string? rememberPassword = accountData.FirstOrDefault(x => x.Key.Equals("RememberPassword", StringComparison.OrdinalIgnoreCase)).Value?.ToString();

                    return new SteamAccountInfo
                    {
                        AccountName = accountName!,
                        Steam64Id = child.Key,
                        MostRecent = (mostRecent ?? autoLogin) == "1",
                        AllowAutoLogin = (allowAutoLogin ?? autoLogin ?? rememberPassword) == "1"
                    };
                })
                .OfType<SteamAccountInfo>()
                .OrderBy(x => x.AccountName, StringComparer.OrdinalIgnoreCase)
        ];
    }

    public static string? GetSteam64ID()
    {
        if (!File.Exists(SteamLoginUsersPath))
            return null;

		string content = File.ReadAllText(SteamLoginUsersPath);
        if (string.IsNullOrWhiteSpace(content))
            return null;

        KVDocument kv;
        try
        {
            using var ms = new MemoryStream(Encoding.UTF8.GetBytes(content));
            kv = KVSerializer.Create(KVSerializationFormat.KeyValues1Text)
                .Deserialize(ms, new KVSerializerOptions { HasEscapeSequences = true });
        }
        catch
        {
            try
            {
                using var ms = new MemoryStream(Encoding.UTF8.GetBytes(content));
                kv = KVSerializer.Create(KVSerializationFormat.KeyValues1Text)
                    .Deserialize(ms, new KVSerializerOptions { HasEscapeSequences = false });
            }
            catch
            {
                return null;
            }
        }

        return kv?.Root?.Children?.FirstOrDefault(child =>
        {
            if (child.Value is not KVObject accountData)
                return false;

			string? mostRecent = accountData.FirstOrDefault(x => x.Key.Equals("MostRecent", StringComparison.OrdinalIgnoreCase)).Value?.ToString();
			string? autoLogin = accountData.FirstOrDefault(x => x.Key.Equals("AutoLogin", StringComparison.OrdinalIgnoreCase)).Value?.ToString();
			string? allowAutoLogin = accountData.FirstOrDefault(x => x.Key.Equals("AllowAutoLogin", StringComparison.OrdinalIgnoreCase)).Value?.ToString();
			string? rememberPassword = accountData.FirstOrDefault(x => x.Key.Equals("RememberPassword", StringComparison.OrdinalIgnoreCase)).Value?.ToString();

            return (mostRecent ?? autoLogin) == "1"
                && (allowAutoLogin ?? autoLogin ?? rememberPassword) == "1";
        }).Key;
    }

    public static void CloseSteam()
    {
        foreach (string? name in new[] { "steam", "steamwebhelper" })
        {
            Process.GetProcessesByName(name).ToList().ForEach(process =>
            {
                process.Kill();
                process.WaitForExit();
            });
        }
    }

    public static async Task SteamLogin(IStatusReporter? reporter = null)
    {
        // launch steam
        Process.Start(SteamPath);

        // check when logged in
        while (true)
        {
            if (File.Exists(SteamLoginUsersPath))
            {
				string content = File.ReadAllText(SteamLoginUsersPath);
                KVDocument kv;
                try
                {
                    kv = KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Deserialize(new MemoryStream(Encoding.UTF8.GetBytes(content)), new KVSerializerOptions { HasEscapeSequences = true });
                }
                catch (KeyValueException)
                {
                    kv = KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Deserialize(new MemoryStream(Encoding.UTF8.GetBytes(content)), new KVSerializerOptions { HasEscapeSequences = false });
                }

                if (kv.Root.Children.Any())
                {
                    await Task.Delay(3000);

                    // close steam
                    CloseSteam();

                    content = File.ReadAllText(SteamLoginUsersPath);
                    try
                    {
                        kv = KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Deserialize(new MemoryStream(Encoding.UTF8.GetBytes(content)), new KVSerializerOptions { HasEscapeSequences = true });
                    }
                    catch (KeyValueException)
                    {
                        kv = KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Deserialize(new MemoryStream(Encoding.UTF8.GetBytes(content)), new KVSerializerOptions { HasEscapeSequences = false });
                    }

                    reporter?.SetTitle($"Successfully logged in as {kv.Root.Children.Select(children => children.Value["AccountName"]?.ToString()).FirstOrDefault(name => !string.IsNullOrEmpty(name))}...");
                    break;
                }

            }

            if (Process.GetProcessesByName("steam").Length == 0)
            {
                break;
            }

            await Task.Delay(500);
        }

        await Task.Delay(1000);
    }

    public static async Task ImportGames()
    {
		// get install list from other drives
		string? systemDrive = Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.System));
        var candidates = DriveInfo.GetDrives()
            .Where(d => d.DriveType == DriveType.Fixed && d.Name.ToUpperInvariant() != systemDrive?.ToUpperInvariant())
            .Select(d => new { Drive = d.Name, SoftwareHivePath = Path.Combine(d.Name, "Windows", "System32", "config", "SOFTWARE") })
            .Where(x => File.Exists(x.SoftwareHivePath))
            .ToList();

        var libraryFiles = new List<FileInfo>();

        foreach (var candidate in candidates)
        {
            await Process.Start(new ProcessStartInfo
            {
                FileName = "reg.exe",
                Arguments = $@"load HKLM\OfflineSoftware ""{candidate.SoftwareHivePath}""",
                CreateNoWindow = true,
                UseShellExecute = false
            })!.WaitForExitAsync();

            string? steamPath = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"OfflineSoftware\Valve\Steam")?.GetValue("InstallPath") as string;

            await Process.Start(new ProcessStartInfo
            {
                FileName = "reg.exe",
                Arguments = @"unload HKLM\OfflineSoftware",
                CreateNoWindow = true,
                UseShellExecute = false
            })!.WaitForExitAsync();

            if (!string.IsNullOrWhiteSpace(steamPath))
            {
                string registryDrive = Path.GetPathRoot(steamPath)!;
                string relativePath = steamPath[registryDrive.Length..];
                string vdfRelative = Path.Combine(relativePath, "steamapps", "libraryfolders.vdf");

                if (registryDrive.Equals(systemDrive, StringComparison.InvariantCultureIgnoreCase))
                {
                    string actualVdfPath = Path.Combine(candidate.Drive, vdfRelative);

                    if (File.Exists(actualVdfPath))
                        libraryFiles.Add(new FileInfo(actualVdfPath));
                }
                else
                {
                    string originalVdfPath = Path.Combine(registryDrive, vdfRelative);

                    if (File.Exists(originalVdfPath))
                    {
                        libraryFiles.Add(new FileInfo(originalVdfPath));
                    }
                    else
                    {
                        foreach (DriveInfo? drive in DriveInfo.GetDrives().Where(d => d.DriveType == DriveType.Fixed && !d.Name.Equals(systemDrive, StringComparison.InvariantCultureIgnoreCase) && !d.Name.Equals(candidate.Drive, StringComparison.InvariantCultureIgnoreCase)))
                        {
                            string testVdfPath = Path.Combine(drive.Name, vdfRelative);

                            if (File.Exists(testVdfPath))
                            {
                                libraryFiles.Add(new FileInfo(testVdfPath));
                                break;
                            }
                        }
                    }
                }
            }
        }

        if (libraryFiles.Count == 0)
            return;

		FileInfo newestFile = libraryFiles.OrderByDescending(f => f.LastWriteTime).First()!;
        string oldDrive = Path.GetPathRoot(newestFile.FullName)!;

        // copy manifests folder to new drive
        string sourceCacheDir = Path.Combine(oldDrive, SteamLibraryCacheDir[Path.GetPathRoot(SteamLibraryCacheDir)!.Length..]);

        if (Directory.Exists(sourceCacheDir))
            FileSystem.CopyDirectory(sourceCacheDir, SteamLibraryCacheDir, overwrite: true);

        // copy userdata folder to new drive
        string sourceUserDataDir = Path.Combine(oldDrive, SteamUserDataDir[Path.GetPathRoot(SteamUserDataDir)!.Length..]);

        if (Directory.Exists(sourceUserDataDir))
            FileSystem.CopyDirectory(sourceUserDataDir, SteamUserDataDir, overwrite: true);

        // check and set new paths in shortcuts.vdf
        if (Directory.Exists(SteamUserDataDir))
        {
            foreach (string userDir in Directory.GetDirectories(SteamUserDataDir))
            {
                string shortcutsPath = Path.Combine(userDir, "config", "shortcuts.vdf");

                if (!File.Exists(shortcutsPath)) continue;

                KVDocument kv;
                using (FileStream stream = File.OpenRead(shortcutsPath))
                {
                    kv = KVSerializer.Create(KVSerializationFormat.KeyValues1Binary).Deserialize(stream);
                }

                foreach (KeyValuePair<string, KVObject> shortcut in kv.Root.Children)
                {
					KVObject shortcutData = shortcut.Value;

                    foreach (string? key in new[] { "Exe", "StartDir" })
                    {
						KeyValuePair<string, KVObject> node = shortcutData.Children.FirstOrDefault(children => string.Equals(children.Key, key, StringComparison.OrdinalIgnoreCase));

                        if (!node.Equals(default(KeyValuePair<string, KVObject>)))
                        {
                            string pathVal = node.Value?.ToString() ?? "";
                            string cleanVal = pathVal.Replace("\"", "");

                            foreach (DriveInfo? drive in DriveInfo.GetDrives().Where(drive => drive.DriveType == DriveType.Fixed))
                            {
                                string testPath = drive.Name[0] + cleanVal[1..];

                                if (File.Exists(testPath) || Directory.Exists(testPath))
                                {
                                    int driveIndex = pathVal.StartsWith("\"") ? 1 : 0;
                                    string updatedVal = pathVal[..driveIndex] + drive.Name[0] + pathVal[(driveIndex + 1)..];
                                    shortcutData[key] = [with(updatedVal)];
                                    break;
                                }
                            }
                        }
                    }
                }

                using var msOut = new MemoryStream();
                KVSerializer.Create(KVSerializationFormat.KeyValues1Binary).Serialize(msOut, kv);
                File.WriteAllBytes(shortcutsPath, msOut.ToArray());
            }
        }

        KVDocument oldLibraryData;
        using (FileStream stream = File.OpenRead(newestFile.FullName))
        {
            oldLibraryData = KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Deserialize(stream, new KVSerializerOptions { HasEscapeSequences = true });
        }

        // add current empty steam library at index 0
        var rootObj = new KVObject
        {
            ["0"] = new KVObject
            {
                ["path"] = [with(SteamDir)],
                ["label"] = [with("")],
                ["contentid"] = [with("0")],
                ["totalsize"] = [with("0")],
                ["update_clean_bytes_tally"] = [with("0")],
                ["time_last_update_verified"] = [with("0")],
                ["apps"] = []
            }
        };

        int nextIndex = 1;
        foreach (KeyValuePair<string, KVObject> folder in oldLibraryData.Root.Children)
        {
			IEnumerable<KeyValuePair<string, KVObject>> folderChildren = folder.Value.Children;
            string? contentId = folderChildren.FirstOrDefault(children => children.Key == "contentid").Value?.ToString();
            string? originalPath = folderChildren.FirstOrDefault(children => children.Key == "path").Value?.ToString();
            string relativePath = originalPath![Path.GetPathRoot(originalPath)!.Length..];
            string resolvedPath = originalPath ?? "";

            foreach (DriveInfo? drive in DriveInfo.GetDrives().Where(drive => drive.DriveType == DriveType.Fixed))
            {
                string testPath = Path.Combine(drive.Name, relativePath);
                string externalVdfPath = Path.Combine(testPath, "libraryfolder.vdf");
                string internalVdfPath = Path.Combine(testPath, "steamapps", "libraryfolders.vdf");

                if (File.Exists(externalVdfPath) && File.ReadAllText(externalVdfPath).Contains(contentId!))
                {
                    resolvedPath = testPath;
                    break;
                }
                if (File.Exists(internalVdfPath) && File.ReadAllText(internalVdfPath).Contains(contentId!))
                {
                    resolvedPath = testPath;
                    break;
                }
            }

            var entry = new KVObject();
            foreach (KeyValuePair<string, KVObject> child in folderChildren)
            {
                entry[child.Key] = child.Key == "path" ? [with(resolvedPath)] : child.Value;
            }

            rootObj[nextIndex.ToString()] = entry;
            nextIndex++;
        }

        using (var msOut = new MemoryStream())
        {
            var newDoc = new KVDocument(null, "libraryfolders", rootObj);
            KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Serialize(msOut, newDoc, new KVSerializerOptions { HasEscapeSequences = true });
            Directory.CreateDirectory(Path.GetDirectoryName(SteamLibraryPath)!);
            File.WriteAllText(SteamLibraryPath, Encoding.UTF8.GetString(msOut.ToArray()));
        }

        await Task.Delay(1000);
    }

    public static (Dictionary<string, string> PlaytimeData, HashSet<string> OwnedAppIds) GetPlaytime()
    {
        var playtimeData = new Dictionary<string, string>();
        var ownedAppIds = new HashSet<string>();
        if (!Directory.Exists(SteamUserDataDir)) return (playtimeData, ownedAppIds);

        string? steam64Id = GetSteam64ID();
        if (!ulong.TryParse(steam64Id, out ulong steam64IdNum)) return (playtimeData, ownedAppIds);
        const ulong steam64IdBase = 76561197960265728;
        ulong folderId = steam64IdNum - steam64IdBase;
        string folderName = folderId.ToString();

        string localConfigPath = Path.Combine(SteamUserDataDir, folderName, "config", "localconfig.vdf");
        if (!File.Exists(localConfigPath)) return (playtimeData, ownedAppIds);

        KVDocument kv;
        try
        {
            using FileStream stream = File.OpenRead(localConfigPath);
            kv = KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Deserialize(stream, new KVSerializerOptions { HasEscapeSequences = true });
        }
        catch
        {
            return (playtimeData, ownedAppIds);
        }

		KeyValuePair<string, KVObject> softwareNode = (kv?.Root?.Children ?? []).FirstOrDefault(children => string.Equals(children.Key, "Software", StringComparison.OrdinalIgnoreCase));
        if (softwareNode.Equals(default(KeyValuePair<string, KVObject>))) return (playtimeData, ownedAppIds);

		KVObject software = softwareNode.Value;
		KVObject valve = software["Valve"];
        if (valve == null) return (playtimeData, ownedAppIds);

		KVObject steam = valve["Steam"];
        if (steam == null) return (playtimeData, ownedAppIds);

		KeyValuePair<string, KVObject> appsChild = steam.Children.FirstOrDefault(children => string.Equals(children.Key, "apps", StringComparison.OrdinalIgnoreCase));
		KVObject apps = !appsChild.Equals(default(KeyValuePair<string, KVObject>)) ? appsChild.Value : steam["apps"];
        if (apps == null) return (playtimeData, ownedAppIds);

        foreach (KeyValuePair<string, KVObject> app in apps.Children)
        {
            string gameId = app.Key;
            ownedAppIds.Add(gameId);

			KeyValuePair<string, KVObject> playtimeNode = app.Value.Children.FirstOrDefault(children => children.Key == "Playtime");
            string? playtimeValue = playtimeNode.Value?.ToString();
            if (!string.IsNullOrEmpty(playtimeValue) && int.TryParse(playtimeValue, out int playtimeMinutes))
            {
                var ts = TimeSpan.FromMinutes(playtimeMinutes);
                string formattedTime = ts.TotalHours >= 1 ? $"{(int)ts.TotalHours}h {ts.Minutes}m" : $"{ts.Minutes}m";
                playtimeData[gameId] = formattedTime;
            }
        }

        return (playtimeData, ownedAppIds);
    }

    public static async Task<List<GameModel>> GetGames()
    {
        var games = new ConcurrentBag<GameModel>();

        if (!File.Exists(SteamPath) || !File.Exists(SteamLibraryPath)) return [];

        (Dictionary<string, string>? playtimeData, HashSet<string>? ownedAppIds) = GetPlaytime();

        // read libraryfolders.vdf
        KVDocument libraryFolderData;
        using (FileStream libraryStream = File.OpenRead(SteamLibraryPath))
        {
            libraryFolderData = KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Deserialize(libraryStream);
        }

        var rawGames = new ConcurrentBag<(GameModel model, string appId, string? title)>();

        // for each steam install path
        await Parallel.ForEachAsync(libraryFolderData.Root.Children, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount * 2 }, async (folder, cancellationToken) =>
        {
            string steamAppsDir = Path.Combine((folder.Value["path"]?.ToString().Replace(@"\\", @"\")) ?? "", "steamapps");

            // skip if no steamapps directory
            if (!Directory.Exists(steamAppsDir)) return;

			// get installed apps dictionary
			KeyValuePair<string, KVObject> appsNode = folder.Value.Children.FirstOrDefault(children => children.Key == "apps");
            if (appsNode.Value == null) return;

            foreach (KeyValuePair<int, KVObject> app in appsNode.Value.Children.ToDictionary(x => int.Parse(x.Key), x => x.Value))
            {
                string gameId = app.Key.ToString();

                // skip steam tools
                if (gameId == "228980") continue;

                // skip not owned games
                if (ownedAppIds.Count > 0 && !ownedAppIds.Contains(gameId)) continue;

                try
                {
                    // read game manifest
                    string manifestPath = Path.Combine(steamAppsDir, $"appmanifest_{gameId}.acf");
                    if (!File.Exists(manifestPath)) continue;

                    KVDocument appManifestData;
                    using (FileStream manifestStream = File.OpenRead(manifestPath))
                    {
                        appManifestData = KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Deserialize(manifestStream);
                    }
                    long? sizeBytes = long.TryParse(appManifestData["SizeOnDisk"]?.ToString(), out long result) ? result : null;

                    // get playtime
                    string playtimeStr = playtimeData.TryGetValue(gameId, out string? pt) ? pt : "0m";

                    var game = new GameModel
                    {
                        Launcher = "Steam",
                        PlayTime = playtimeStr,
                        InstallLocation = Path.Combine(steamAppsDir, "common", appManifestData["installdir"]?.ToString() ?? "").Replace("/", "\\"),
                        Size = sizeBytes.HasValue
                            ? (sizeBytes.Value >= 1024d * 1024d * 1024d
                                ? $"{sizeBytes.Value / (1024d * 1024d * 1024d):F1} GB"
                                : $"{sizeBytes.Value / (1024d * 1024d):F2} MB")
                                : "Unknown",
                        Version = appManifestData["buildid"]?.ToString(),
                        GameID = gameId,
                        Title = appManifestData["name"]?.ToString()
                    };

                    rawGames.Add((game, gameId, game.Title));
                }
                catch (Exception ex)
                {
                    throw new Exception($"Failed to load game: {gameId}", ex);
                }
            }
        });

        var processList = rawGames.ToList();
        if (processList.Count > 0)
        {
			string appIdsStr = string.Join(",", processList.Select(c => c.appId));

            try
            {
                string url = $"https://store.steampowered.com/api/appdetails?appids={appIdsStr}&l=english";
                string response = await httpClient.GetStringAsync(url);
                using var doc = JsonDocument.Parse(response);
				JsonElement root = doc.RootElement;

                await Parallel.ForEachAsync(processList, new ParallelOptions { MaxDegreeOfParallelism = 10 }, async (item, cancellationToken) =>
                {
                    if (root.TryGetProperty(item.appId, out JsonElement gameData) &&
                        gameData.TryGetProperty("success", out JsonElement success) &&
                        success.GetBoolean())
                    {
						JsonElement data = gameData.GetProperty("data");
                        if (await GetStoreMetadata(item.model, item.appId, data, cancellationToken))
                        {
                            games.Add(item.model);
                        }
                    }
                });
            }
            catch
            {
                await Parallel.ForEachAsync(processList, new ParallelOptions { MaxDegreeOfParallelism = 5 }, async (item, cancellationToken) =>
                {
                    if (await GetStoreMetadata(item.model, item.appId, cancellationToken))
                    {
                        games.Add(item.model);
                    }
                });
            }
        }

        foreach (GameModel nonSteamGame in await GetNonSteamGames())
        {
            games.Add(nonSteamGame);
        }

        return [.. games];
    }


    public static async Task<List<GameModel>> GetNonSteamGames()
    {
        var games = new ConcurrentBag<GameModel>();
        try
        {
            if (!Directory.Exists(SteamUserDataDir)) return [];

            var shortcutList = new List<(string appName, string? exe, string? startDir, string longId, long sizeBytes)>();

            string? steam64Id = GetSteam64ID();
            if (string.IsNullOrEmpty(steam64Id)) return [];

            if (!ulong.TryParse(steam64Id, out ulong steam64IdNum)) return [];
            const ulong steam64IdBase = 76561197960265728;
            ulong folderId = steam64IdNum - steam64IdBase;
            string folderName = folderId.ToString();

            string shortcutsPath = Path.Combine(SteamUserDataDir, folderName, "config", "shortcuts.vdf");
            if (!File.Exists(shortcutsPath)) return [];

            using FileStream stream = File.OpenRead(shortcutsPath);
			KVDocument kv = KVSerializer.Create(KVSerializationFormat.KeyValues1Binary).Deserialize(stream);

            foreach (KeyValuePair<string, KVObject> shortcut in kv.Root.Children)
            {
				KVObject shortcutData = shortcut.Value;
                string? appName = shortcutData.Children.FirstOrDefault(children => string.Equals(children.Key, "AppName", StringComparison.OrdinalIgnoreCase)).Value?.ToString();
                string? exe = shortcutData.Children.FirstOrDefault(children => string.Equals(children.Key, "Exe", StringComparison.OrdinalIgnoreCase)).Value?.ToString()?.Replace("\"", "");
                string? startDir = shortcutData.Children.FirstOrDefault(children => string.Equals(children.Key, "StartDir", StringComparison.OrdinalIgnoreCase)).Value?.ToString()?.Replace("\"", "")?.TrimEnd('\\', '/');
				KVObject appidValue = shortcutData.Children.FirstOrDefault(children => string.Equals(children.Key, "appid", StringComparison.OrdinalIgnoreCase)).Value;

                if (string.IsNullOrEmpty(appName)) continue;

                long appid = 0;
                if (appidValue != null && long.TryParse(appidValue.ToString(), out long id))
                {
                    appid = id;
                }

                ulong longIdValue = ((ulong)(uint)appid << 32) | 0x02000000;
                string longId = longIdValue.ToString();
                string? gameDir = !string.IsNullOrEmpty(startDir) && Directory.Exists(startDir) ? startDir : (!string.IsNullOrEmpty(exe) ? Path.GetDirectoryName(exe) : null);

                long sizeBytes = 0;

                if (!string.IsNullOrEmpty(gameDir) && Directory.Exists(gameDir))
                {
                    sizeBytes = new DirectoryInfo(gameDir).EnumerateFiles("*", System.IO.SearchOption.AllDirectories).Sum(file => file.Length);
                }

                shortcutList.Add((appName, exe, startDir, longId, sizeBytes));
            }

            await Parallel.ForEachAsync(shortcutList, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount * 2 }, async (shortcutItem, cancellationToken) =>
            {
                (string? appName, string? exe, string? startDir, string? longId, long sizeBytes) = shortcutItem;

                string version = "";

                string? exePath = !string.IsNullOrEmpty(startDir) && !string.IsNullOrEmpty(exe) ? Path.Combine(startDir, exe) : exe;
                if (!string.IsNullOrEmpty(exePath) && File.Exists(exePath))
                {
                    version = FileVersionInfo.GetVersionInfo(exePath).FileVersion ?? "";
                }

				string searchUrl = $"https://store.steampowered.com/api/storesearch/?term={Uri.EscapeDataString(appName)}&l=english&cc=US";
				string searchResponse = await httpClient.GetStringAsync(searchUrl, cancellationToken);
                using var searchDoc = JsonDocument.Parse(searchResponse);
				JsonElement searchData = searchDoc.RootElement;

                if (searchData.TryGetProperty("total", out JsonElement totalProp) && totalProp.GetInt32() > 0 && searchData.TryGetProperty("items", out JsonElement itemsProp))
                {
					JsonElement bestMatch = itemsProp.EnumerateArray().FirstOrDefault(item => string.Equals(item.GetProperty("name").GetString(), appName, StringComparison.OrdinalIgnoreCase));

                    if (bestMatch.ValueKind != JsonValueKind.Undefined && bestMatch.TryGetProperty("id", out JsonElement idProp))
                    {
                        string steamAppId = idProp.GetInt32().ToString();

                        var tempGame = new GameModel
                        {
                            Launcher = "Steam",
                            Title = appName,
                            GameID = longId,
                            InstallLocation = startDir,
                            LaunchExecutable = exe,
                            Size = sizeBytes >= 1024d * 1024d * 1024d
                                ? $"{sizeBytes / (1024d * 1024d * 1024d):F1} GB"
                                : $"{sizeBytes / (1024d * 1024d):F2} MB",
ProcessNames = [Path.GetFileNameWithoutExtension(exe) ?? ""],
                            PlayTime = "0m",
                            Version = version
                        };

                        if (await GetStoreMetadata(tempGame, steamAppId, cancellationToken))
                        {
                            games.Add(tempGame);
                            return;
                        }
                    }
                }

				Dictionary<string, string?>? result = await IgdbHelper.SearchCovers(appName);
                if (result != null)
                {
                    string? gameUrl = result.GetValueOrDefault("game_url");
                    string? coverUrl = result.GetValueOrDefault("cover_url");

                    string summary = "";
                    var genres = new List<string>();
                    var features = new List<string>();
                    double rating = 0;

                    if (!string.IsNullOrEmpty(gameUrl))
                    {
						string docResponse = await httpClient.GetStringAsync(gameUrl, cancellationToken);
                        using var docData = JsonDocument.Parse(docResponse);
						JsonElement data = docData.RootElement;

                        summary = data.TryGetProperty("summary", out JsonElement summaryProp) ? summaryProp.GetString() ?? "" : "";

                        if (data.TryGetProperty("genres", out JsonElement genresProp) && genresProp.ValueKind == JsonValueKind.Array)
                        {
                            genres = [.. genresProp.EnumerateArray().Select(g => g.GetProperty("name").GetString() ?? "")];
                        }

                        if (data.TryGetProperty("game_modes", out JsonElement modesProp) && modesProp.ValueKind == JsonValueKind.Array)
                        {
                            features = [.. modesProp.EnumerateArray().Select(m => m.GetProperty("name").GetString() ?? "")];
                        }

                        rating = data.TryGetProperty("aggregated_rating", out JsonElement ratingProp) ? Math.Round(ratingProp.GetDouble() / 20.0, 2) : 0;
                    }

                    var igdbGame = new GameModel
                    {
                        Launcher = "Steam",
                        Title = appName,
                        GameID = longId,
                        InstallLocation = startDir,
                        LaunchExecutable = exe,
                        ImageUrl = coverUrl,
                        BackgroundImageUrl = coverUrl,
                        Developers = result.GetValueOrDefault("developers"),
                        ReleaseDate = result.GetValueOrDefault("release_date") ?? "Unknown",
                        Size = sizeBytes >= 1024d * 1024d * 1024d
                            ? $"{sizeBytes / (1024d * 1024d * 1024d):F1} GB"
                            : $"{sizeBytes / (1024d * 1024d):F2} MB",
                        AgeRatingUrl = result.GetValueOrDefault("age_rating_url"),
                        AgeRatingTitle = result.GetValueOrDefault("age_rating_title"),
                        Description = summary,
                        Genres = genres,
                        Features = features,
                        Rating = rating,
                        ProcessNames = [Path.GetFileNameWithoutExtension(exe ?? "")],
                        PlayTime = "0m",
                        Version = version
                    };

                    games.Add(igdbGame);
                }
            });
        }
        catch
        {

        }

        return [.. games];
    }

    private static async Task<bool> GetStoreMetadata(GameModel model, string steamAppId, JsonElement data, CancellationToken cancellationToken = default)
    {
		bool comingSoon = data.TryGetProperty("release_date", out JsonElement releaseDateElem) &&
            releaseDateElem.TryGetProperty("coming_soon", out JsonElement comingSoonElem) &&
            comingSoonElem.GetBoolean();
        if (comingSoon) return false;

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

		string ratingBaseUrl = ratingKey switch
        {
            "ACB" => "https://store.fastly.steamstatic.com/public/shared/images/game_ratings/ACB/",
            "DEJUS" => "https://store.fastly.steamstatic.com/public/shared/images/game_ratings/DEJUS/",
            "GRAC" => "https://store.fastly.steamstatic.com/public/shared/images/game_ratings/GRAC/",
            "USK" => "https://store.fastly.steamstatic.com/public/shared/images/game_ratings/USK/",
            "ESRB" => "https://store.fastly.steamstatic.com/public/shared/images/game_ratings/ESRB/",
            "PEGI" => "https://store.fastly.steamstatic.com/public/shared/images/game_ratings/PEGI/",
            _ => ""
        };

        Dictionary<string, string> ratingTitles = ratingKey switch
        {
            "PEGI" => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                {"3", "PEGI 3"},
                {"7", "PEGI 7"},
                {"12", "PEGI 12"},
                {"16", "PEGI 16"},
                {"18", "PEGI 18"},
            },
            "USK" => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                {"0", "USK 0"},
                {"6", "USK 6"},
                {"12", "USK 12"},
                {"16", "USK 16"},
                {"18", "USK 18"},
            },
            "ESRB" => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                {"ec", "Early Childhood"},
                {"e", "Everyone"},
                {"e10", "Everyone 10+" },
                {"e10+", "Everyone 10+"},
                {"t", "Teen"},
                {"m", "Mature 17+"},
                {"ao", "Adults Only 18+"},
            },
            _ => [with(StringComparer.OrdinalIgnoreCase)]
        };

        model.Title = data.TryGetProperty("name", out JsonElement nameElem) && nameElem.ValueKind == JsonValueKind.String ? nameElem.GetString() : model.Title;
        string libraryCapsuleUrl = $"https://cdn.steamstatic.com/steam/apps/{steamAppId}/library_600x900.jpg";
        string libraryHeroUrl = $"https://cdn.steamstatic.com/steam/apps/{steamAppId}/library_hero.jpg";

        if (await UrlExists(libraryCapsuleUrl, cancellationToken))
        {
            model.ImageUrl = libraryCapsuleUrl;
        }
        else
        {
			Dictionary<string, string?>? igdbResult = await IgdbHelper.SearchCovers(model.Title ?? "");
            if (igdbResult != null && igdbResult.TryGetValue("cover_url", out string? coverUrl) && !string.IsNullOrEmpty(coverUrl))
            {
                model.ImageUrl = coverUrl;
            }
            else
            {
                model.ImageUrl = data.TryGetProperty("capsule_image", out JsonElement capsuleElem) && capsuleElem.ValueKind == JsonValueKind.String
                    ? capsuleElem.GetString()
                    : (data.TryGetProperty("header_image", out JsonElement headerImageElem) && headerImageElem.ValueKind == JsonValueKind.String ? headerImageElem.GetString() : null);
            }
        }

        if (await UrlExists(libraryHeroUrl, cancellationToken))
        {
            model.BackgroundImageUrl = libraryHeroUrl;
        }
        else
        {
            model.BackgroundImageUrl = data.TryGetProperty("background_raw", out JsonElement bgRawElem) && bgRawElem.ValueKind == JsonValueKind.String
                ? bgRawElem.GetString()
                : (data.TryGetProperty("background", out JsonElement bgElem) && bgElem.ValueKind == JsonValueKind.String ? bgElem.GetString() : null);
        }

        string? rating = null;
        string? descriptors = null;

        if (data.TryGetProperty("ratings", out JsonElement ratings) && ratings.ValueKind == JsonValueKind.Object && ratings.TryGetProperty(ratingKey.ToLowerInvariant(), out JsonElement ratingData))
        {
            if (ratingData.TryGetProperty("rating", out JsonElement ratingElement) && ratingElement.ValueKind == JsonValueKind.String)
            {
                rating = ratingElement.GetString();
            }

            if (ratingData.TryGetProperty("descriptors", out JsonElement descElement) && descElement.ValueKind == JsonValueKind.String)
            {
                descriptors = descElement.GetString()?
                    .Replace("\r\n", ", ")
                    .Replace("\n", ", ")
                    .Replace("\r", ", ");
            }
        }

        model.AgeRatingUrl = !string.IsNullOrEmpty(rating) ? $"{ratingBaseUrl}{rating.ToLowerInvariant()}.png" : null;
        model.AgeRatingTitle = !string.IsNullOrEmpty(rating) ? (ratingTitles.TryGetValue(rating.ToLowerInvariant(), out string? title) ? title : rating) : null;
        model.AgeRatingDescription = !string.IsNullOrEmpty(descriptors) ? descriptors : null;

        try
        {
			JsonElement reviewData = JsonDocument.Parse(await httpClient.GetStringAsync($"https://store.steampowered.com/appreviews/{steamAppId}?json=1", cancellationToken)).RootElement.GetProperty("query_summary");
            int totalPositive = reviewData.GetProperty("total_positive").GetInt32();
            int totalNegative = reviewData.GetProperty("total_negative").GetInt32();

            model.Rating = totalPositive + totalNegative > 0 ? Math.Round(5.0 * totalPositive / (totalPositive + totalNegative), 1) : 0.0;
        }
        catch
        {
            model.Rating = 0.0;
        }

        model.Description = data.TryGetProperty("short_description", out JsonElement shortDescription) && shortDescription.ValueKind == JsonValueKind.String ? shortDescription.GetString() : "";

        model.Developers = data.TryGetProperty("developers", out JsonElement developers) && developers.ValueKind == JsonValueKind.Array
            ? string.Join(", ", developers.EnumerateArray().Select(developers => developers.GetString() ?? "").Where(s => !string.IsNullOrWhiteSpace(s)))
            : "Unknown";

        model.Genres = data.TryGetProperty("genres", out JsonElement genres) && genres.ValueKind == JsonValueKind.Array
            ? [.. genres.EnumerateArray().Select(genres => genres.GetProperty("description").GetString() ?? "").Where(s => !string.IsNullOrWhiteSpace(s))]
            : [];

        model.Features = data.TryGetProperty("categories", out JsonElement category) && category.ValueKind == JsonValueKind.Array
            ? [.. category.EnumerateArray().Select(category => category.GetProperty("description").GetString() ?? "").Where(s => !string.IsNullOrWhiteSpace(s))]
            : [];

        model.Screenshots = data.TryGetProperty("screenshots", out JsonElement screenshots) && screenshots.ValueKind == JsonValueKind.Array
            ? [.. screenshots.EnumerateArray().Select(s => s.GetProperty("path_thumbnail").GetString() ?? "").Where(s => !string.IsNullOrWhiteSpace(s))]
            : [];

        string? dateStr = data.TryGetProperty("release_date", out JsonElement releaseDate) && releaseDate.TryGetProperty("date", out JsonElement releaseDateDate) && releaseDateDate.ValueKind == JsonValueKind.String ? releaseDateDate.GetString() : null;
        model.ReleaseDate = dateStr ?? "Unknown";

        if (DateTimeOffset.TryParse(model.ReleaseDate, out DateTimeOffset parsedDate))
        {
            model.ReleaseDate = parsedDate.ToString("d");
        }

        return true;
    }

    private static async Task<bool> GetStoreMetadata(GameModel model, string steamAppId, CancellationToken cancellationToken = default)
    {
        string url = $"https://store.steampowered.com/api/appdetails?appids={steamAppId}&l=english";
        string response = await httpClient.GetStringAsync(url, cancellationToken);
        using var doc = JsonDocument.Parse(response);
		JsonElement root = doc.RootElement;

        if (!root.TryGetProperty(steamAppId, out JsonElement gameData)) return false;
        if (!gameData.TryGetProperty("success", out JsonElement success) || !success.GetBoolean()) return false;

		JsonElement data = gameData.GetProperty("data");
        return await GetStoreMetadata(model, steamAppId, data, cancellationToken);
    }

    private static async Task<bool> UrlExists(string url, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        using HttpResponseMessage response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        return response.IsSuccessStatusCode;
    }
}
