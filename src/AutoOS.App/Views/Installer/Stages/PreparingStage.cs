using AutoOS.Core.Helpers.CPU.Models;
using AutoOS.Core.Helpers.CPU;
using AutoOS.Core.Helpers.Database;
using AutoOS.Core.Helpers.Device.Models;
using AutoOS.Core.Helpers.Device;
using AutoOS.Core.Helpers.GPU.Models;
using Microsoft.UI.Xaml.Media;
using Microsoft.Win32;
using System.Diagnostics;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using ValveKeyValue;
using Windows.Storage;
using WinRT.Interop;
using AutoOS.Core.Helpers.Games;

namespace AutoOS.Views.Installer.Stages;

public static partial class PreparingStage
{
    public static IntPtr WindowHandle { get; private set; }

    public static bool SSD;
    public static string ScheduleMode = string.Empty;
    public static string LightTime = string.Empty;
    public static string DarkTime = string.Empty;
    public static bool LegacyContextMenu;
    public static bool AlwaysShowTrayIcons;
    public static bool TaskbarAlignment;
    public static bool Chrome;
    public static bool Thorium;
    public static bool Helium;
    public static bool Brave;
    public static bool Vivaldi;
    public static bool Arc;
    public static bool Comet;
    public static bool Firefox;
    public static bool Zen;
    public static bool uBlock;
    public static bool PrivacyBadger;
    public static bool Decentraleyes;
    public static bool Cookies;
    public static bool Violentmonkey;
    public static bool Tampermonkey;
    public static bool SponsorBlock;
    public static bool ReturnYouTubeDislike;
    public static bool DarkReader;
    public static bool Shazam;
    public static bool iCloud;
    public static bool Bitwarden;
    public static bool OnePassword;
    
    public static bool Discord;
    public static bool DiscordAccount;
    public static bool WhatsApp;

    public static bool EpicGames;
    public static bool EpicGamesAccount;
    public static bool EpicGamesGames;
    public static bool Steam;
    public static bool SteamGames;
    public static bool RiotClient;
    public static bool RiotClientAccount;
    public static bool RiotClientGames;
    public static bool EA;
    public static bool UbisoftConnect;
    public static bool BattleNet;
    public static bool MinecraftLauncher;
    public static bool RockstarGamesLauncher;
    public static bool FiveM;
    public static bool FACEIT;
    
    public static bool AppleMusic;
    public static bool Tidal;
    public static bool Qobuz;
    public static bool AmazonMusic;
    public static bool DeezerMusic;
    public static bool Spotify;

    public static bool SteelSeriesGG;
    public static bool RazerSynapse;
    public static bool LogitechGHub;
    public static bool LogitechOnboardMemoryManager;
    public static bool Wootility;
    public static bool CorsairICue;

    public static bool VisualStudio;
    public static bool VisualStudioCode;
    public static bool Antigravity;
    public static bool Git;
    public static bool Python;
    public static bool Nodejs;
    public static bool Trello;

    public static bool Word;
    public static bool Excel;
    public static bool PowerPoint;
    public static bool OneNote;
    public static bool Teams;
    public static bool Outlook;
    public static bool OneDrive;

    public static List<GpuInfo> GPUs { get; set; } = [];
    public static bool MSI;
    public static bool CRU;

    public static bool Wifi;
    public static bool TxIntDelay;
    public static bool NetAdapterCx;
    public static bool SOUND;

    public static bool INTELCPU;
    public static bool AMDCPU;
    public static bool WindowsDefender;
    public static bool UserAccountControl;
    public static bool DEP;
    public static bool MemoryIntegrity;
    public static bool VirtualizationBasedSecurity;
    public static bool SpectreMeltdownMitigations;
    public static bool ProcessMitigations;

    public static int PCores;
    public static int ECores;
    public static int TCores;
    public static int LogicalCores;
    public static bool HyperThreading;

    private static readonly ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

    public static async Task Run()
    {
        WindowHandle = WindowNative.GetWindowHandle(App.MainWindow);
        InstallPage.Status.Text = "Preparing...";
        InstallPage.Info.Title = "Please wait...";

        InstallPage.Info.Severity = InfoBarSeverity.Warning;
        InstallPage.Progress.Foreground = (Brush)Application.Current.Resources["SystemFillColorCautionBrush"];
		TaskbarHelper.SetProgressState(WindowHandle, TaskbarStates.Paused);
        InstallPage.ProgressRingControl.Foreground = (Brush)Application.Current.Resources["SystemFillColorCautionBrush"];
        if (localSettings.Values["Install_Start"] == null)
            localSettings.Values["Install_Start"] = DateTimeOffset.Now.ToString("O");

        await Task.Run(async () =>
        {
            CpuArchitecture CpuArch = CpuHelper.GetCpuArchitecture();
            INTELCPU = CpuArch.Vendor == CpuVendor.Intel;
            AMDCPU = CpuArch.Vendor == CpuVendor.AMD;

            var output = Process.Start(new ProcessStartInfo
            {
                FileName = "powershell",
                Arguments = @"-Command ""(Get-PhysicalDisk -SerialNumber (Get-Disk -Number (Get-Partition -DriveLetter $env:SystemDrive.Substring(0, 1)).DiskNumber).SerialNumber.TrimStart()).MediaType""",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }).StandardOutput.ReadToEnd();

            if (output.Contains("SSD"))
            {
                SSD = true;
            }

            ScheduleMode = localSettings.Values["ScheduleMode"]?.ToString();
            LightTime = localSettings.Values["LightTime"]?.ToString();
            DarkTime = localSettings.Values["DarkTime"]?.ToString();

            LegacyContextMenu = (localSettings.Values["LegacyContextMenu"]?.ToString() == "1");
            AlwaysShowTrayIcons = (localSettings.Values["AlwaysShowTrayIcons"]?.ToString() == "1");
            TaskbarAlignment = (localSettings.Values["TaskbarAlignment"]?.ToString() == "Left");

            Chrome = (localSettings.Values["Browsers"]?.ToString().Contains("Chrome") ?? false);
            Thorium = (localSettings.Values["Browsers"]?.ToString().Contains("Thorium") ?? false);
            Helium = (localSettings.Values["Browsers"]?.ToString().Contains("Helium") ?? false);
            Brave = (localSettings.Values["Browsers"]?.ToString().Contains("Brave") ?? false);
            Vivaldi = (localSettings.Values["Browsers"]?.ToString().Contains("Vivaldi") ?? false);
            Arc = (localSettings.Values["Browsers"]?.ToString().Contains("Arc") ?? false);
            Comet = (localSettings.Values["Browsers"]?.ToString().Contains("Comet") ?? false);
            Firefox = (localSettings.Values["Browsers"]?.ToString().Contains("Firefox") ?? false);
            Zen = (localSettings.Values["Browsers"]?.ToString().Contains("Zen") ?? false);

            uBlock = (localSettings.Values["Extensions"]?.ToString().Contains("uBlock Origin") ?? false);
            PrivacyBadger = (localSettings.Values["Extensions"]?.ToString().Contains("Privacy Badger") ?? false);
            Decentraleyes = (localSettings.Values["Extensions"]?.ToString().Contains("Decentraleyes") ?? false);
            Cookies = (localSettings.Values["Extensions"]?.ToString().Contains("I still don't care about cookies") ?? false);
            Violentmonkey = (localSettings.Values["Extensions"]?.ToString().Contains("Violentmonkey") ?? false);
            Tampermonkey = (localSettings.Values["Extensions"]?.ToString().Contains("Tampermonkey") ?? false);
            SponsorBlock = (localSettings.Values["Extensions"]?.ToString().Contains("SponsorBlock") ?? false);
            ReturnYouTubeDislike = (localSettings.Values["Extensions"]?.ToString().Contains("Return YouTube Dislike") ?? false);
            DarkReader = (localSettings.Values["Extensions"]?.ToString().Contains("Dark Reader") ?? false);
            Shazam = (localSettings.Values["Extensions"]?.ToString().Contains("Shazam") ?? false);
            iCloud = (localSettings.Values["Extensions"]?.ToString().Contains("iCloud Passwords") ?? false);
            Bitwarden = (localSettings.Values["Extensions"]?.ToString().Contains("Bitwarden") ?? false);
            OnePassword = (localSettings.Values["Extensions"]?.ToString().Contains("1Password") ?? false);

            Discord = (localSettings.Values["Messaging"]?.ToString().Contains("Discord") ?? false);
            WhatsApp = (localSettings.Values["Messaging"]?.ToString().Contains("WhatsApp") ?? false);

            EpicGames = (localSettings.Values["Launchers"]?.ToString().Contains("Epic Games") ?? false);
            Steam = (localSettings.Values["Launchers"]?.ToString().Contains("Steam") ?? false);
            RiotClient = (localSettings.Values["Launchers"]?.ToString().Contains("Riot Client") ?? false);
            EA = (localSettings.Values["Launchers"]?.ToString().Contains("EA") ?? false);
            UbisoftConnect = (localSettings.Values["Launchers"]?.ToString().Contains("Ubisoft Connect") ?? false);
            BattleNet = (localSettings.Values["Launchers"]?.ToString().Contains("Battle.Net") ?? false);
            MinecraftLauncher = (localSettings.Values["Launchers"]?.ToString().Contains("Minecraft Launcher") ?? false);
            RockstarGamesLauncher = (localSettings.Values["Launchers"]?.ToString().Contains("Rockstar Games Launcher") ?? false);
            FiveM = (localSettings.Values["Launchers"]?.ToString().Contains("FiveM") ?? false);
            FACEIT = (localSettings.Values["Launchers"]?.ToString().Contains("FACEIT") ?? false);

            AppleMusic = (localSettings.Values["Music"]?.ToString().Contains("Apple Music") ?? false);
            Tidal = (localSettings.Values["Music"]?.ToString().Contains("TIDAL") ?? false);
            Qobuz = (localSettings.Values["Music"]?.ToString().Contains("Qobuz") ?? false);
            AmazonMusic = (localSettings.Values["Music"]?.ToString().Contains("Amazon Music") ?? false);
            DeezerMusic = (localSettings.Values["Music"]?.ToString().Contains("Deezer Music") ?? false);
            Spotify = (localSettings.Values["Music"]?.ToString().Contains("Spotify") ?? false);

            SteelSeriesGG = (localSettings.Values["Peripherals"]?.ToString().Contains("SteelSeries GG") ?? false);
            RazerSynapse = (localSettings.Values["Peripherals"]?.ToString().Contains("Razer Synapse") ?? false);
            LogitechGHub = (localSettings.Values["Peripherals"]?.ToString().Contains("Logitech G HUB") ?? false);
            LogitechOnboardMemoryManager = (localSettings.Values["Peripherals"]?.ToString().Contains("Logitech Onboard Memory Manager") ?? false);
            Wootility = (localSettings.Values["Peripherals"]?.ToString().Contains("Wootility") ?? false);
            CorsairICue = (localSettings.Values["Peripherals"]?.ToString().Contains("Corsair iCUE") ?? false);

            VisualStudio = (localSettings.Values["Development"]?.ToString().Contains("Visual Studio") ?? false);
            VisualStudioCode = (localSettings.Values["Development"]?.ToString().Contains("Visual Studio Code") ?? false);
            Antigravity = (localSettings.Values["Development"]?.ToString().Contains("Antigravity") ?? false);
            Git = (localSettings.Values["Development"]?.ToString().Contains("Git") ?? false);
            Python = (localSettings.Values["Development"]?.ToString().Contains("Python") ?? false);
            Nodejs = (localSettings.Values["Development"]?.ToString().Contains("Node.js") ?? false);
            Trello = (localSettings.Values["Development"]?.ToString().Contains("Trello") ?? false);

            Word = (localSettings.Values["Office"]?.ToString().Contains("Word") ?? false);
            Excel = (localSettings.Values["Office"]?.ToString().Contains("Excel") ?? false);
            PowerPoint = (localSettings.Values["Office"]?.ToString().Contains("PowerPoint") ?? false);
            OneNote = (localSettings.Values["Office"]?.ToString().Contains("OneNote") ?? false);
            Teams = (localSettings.Values["Office"]?.ToString().Contains("Teams") ?? false);
            Outlook = (localSettings.Values["Office"]?.ToString().Contains("Outlook") ?? false);
            OneDrive = (localSettings.Values["Office"]?.ToString().Contains("OneDrive") ?? false);

            var gpuArray = JsonNode.Parse(localSettings.Values["GPUs"]?.ToString() ?? "[]")?.AsArray();
            if (gpuArray != null)
            {
                foreach (var node in gpuArray)
                {
                    var obj = node?.AsObject();
                    if (obj == null) continue;

                    GPUs.Add(new GpuInfo
                    {
                        DeviceName = obj["Name"]?.ToString(),
                        PnPDeviceId = obj["PnPDeviceId"]?.ToString(),
                        VendorId = obj["VendorId"]?.ToString(),
                        DeviceId = obj["DeviceId"]?.ToString(),
                        Codename = obj["Codename"]?.ToString(),
                        Install = obj["Install"]?.GetValue<bool>() ?? false,
                        IsInstalled = obj["IsInstalled"]?.GetValue<bool>() ?? false,
                        RegistryPath = obj["RegistryPath"]?.ToString(),
                        Location = obj["Location"]?.ToString(),
                        PStates = obj["PStates"]?.GetValue<bool>() ?? false,
                        ECC = obj["ECC"]?.GetValue<bool>() ?? false,
                        GspFirmware = obj["GspFirmware"]?.GetValue<bool>() ?? false,
                        HDCP = obj["HDCP"]?.GetValue<bool>() ?? false,
                        HDMIDPAudio = obj["HDMIDPAudio"]?.GetValue<bool>() ?? false,
                        CurrentVersion = obj["CurrentVersion"]?.ToString()
                    });
                }
            }

            MSI = (localSettings.Values["MsiProfile"] != null);
            CRU = (localSettings.Values["CruProfile"] != null);

            WindowsDefender = (localSettings.Values["WindowsDefender"]?.ToString() == "1");
            UserAccountControl = (localSettings.Values["UserAccountControl"]?.ToString() == "1");
            DEP = (localSettings.Values["DataExecutionPrevention"]?.ToString() == "1");
            MemoryIntegrity = (localSettings.Values["MemoryIntegrity"]?.ToString() == "1");
            VirtualizationBasedSecurity = (localSettings.Values["VirtualizationBasedSecurity"]?.ToString() == "1");
            SpectreMeltdownMitigations = (localSettings.Values["SpectreMeltdownMitigations"]?.ToString() == "1");
            ProcessMitigations = (localSettings.Values["ProcessMitigations"]?.ToString() == "1");

            var cpuSetsInfo = CpuHelper.GetCpuSets();
            var (pCores, _) = CpuHelper.GroupCpuSetsByEfficiencyClass(cpuSetsInfo);
            PCores = pCores.Count;
            HyperThreading = cpuSetsInfo.HyperThreading;
            var systemDrive = Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.System));

            EpicGamesAccount = DriveInfo.GetDrives()
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
                .Any(file =>
                {
                    string configContent = File.ReadAllText(file.FullName);
                    Match dataMatch = Regex.Match(configContent, @"Data=([^\r\n]+)");

                    return dataMatch.Success && dataMatch.Groups[1].Value.Length >= 1000;
                });

            EpicGamesGames = DriveInfo.GetDrives()
                .Where(d => d.DriveType == DriveType.Fixed && d.Name != systemDrive)
                .Select(d => Path.Combine(d.Name, "ProgramData", "Epic", "UnrealEngineLauncher", "LauncherInstalled.dat"))
                .Where(File.Exists)
                .Select(path => new FileInfo(path))
                .OrderByDescending(f => f.LastWriteTime)
                .Select(async file =>
                {
                    string jsonContent = await File.ReadAllTextAsync(file.FullName);
                    var jsonObject = JsonNode.Parse(jsonContent);
                    JsonArray installationList = jsonObject?["InstallationList"] as JsonArray;
                    return installationList != null && installationList.Count > 0;
                })
                .Select(t => t.Result)
                .FirstOrDefault(false);

            SteamGames = DriveInfo.GetDrives()
                .Where(d => d.DriveType == DriveType.Fixed && d.Name != systemDrive)
                .Select(d => Path.Combine(d.Name, "Program Files (x86)", "Steam", "steamapps", "libraryfolders.vdf"))
                .Where(File.Exists)
                .Select(path => new FileInfo(path))
                .OrderByDescending(f => f.LastWriteTime)
                .Select(file =>
                {
                    using var stream = File.OpenRead(file.FullName);
                    var kv = KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Deserialize(stream);
                    return kv?.Root.Children.Any() == true;
                })
                .FirstOrDefault(false);

			RiotClientAccount = DriveInfo.GetDrives()
				.Where(d => d.DriveType == DriveType.Fixed && d.Name != systemDrive)
				.SelectMany(d =>
				{
					string usersPath = Path.Combine(d.Name, "Users");
					if (!Directory.Exists(usersPath)) return [];

					return Directory.GetDirectories(usersPath)
						.Select(userDir => Path.Combine(userDir, "AppData", "Local", "Riot Games", "Riot Client", "Data", "RiotGamesPrivateSettings.yaml"))
						.Where(File.Exists);
				})
				.Any(file =>
				{
					string fileContent = File.ReadAllText(file);
					Match ssidMatch = RiotHelper.SsidRegex().Match(fileContent);

					return ssidMatch.Success && !string.IsNullOrWhiteSpace(ssidMatch.Groups[1].Value);
				});

			RiotClientGames = DriveInfo.GetDrives()
				.Where(d => d.DriveType == DriveType.Fixed && d.Name != systemDrive)
				.SelectMany(d =>
				{
					string metadataPath = Path.Combine(d.Name, "ProgramData", "Riot Games", "Metadata");
					if (!Directory.Exists(metadataPath)) return [];

					return Directory.GetDirectories(metadataPath)
						.Select(subFolder =>
						{
							string folderName = new DirectoryInfo(subFolder).Name;
							string settingsFile = Path.Combine(subFolder, $"{folderName}.product_settings.yaml");

							if (!File.Exists(settingsFile))
								return false;

							string fileContent = File.ReadAllText(settingsFile);
							Match pathMatch = RiotHelper.ProductInstallFullPathRegex().Match(fileContent);

							return pathMatch.Success && !string.IsNullOrWhiteSpace(pathMatch.Groups[1].Value);
						});
				})
				.Any(hasGame => hasGame);

			// DiscordAccount = DriveInfo.GetDrives()
			// 	.Where(d => d.DriveType == DriveType.Fixed && d.Name != systemDrive)
			// 	.SelectMany(d =>
			// 	{
			// 		string usersPath = Path.Combine(d.Name, "Users");
			// 		if (!Directory.Exists(usersPath)) return [];

			// 		return Directory.GetDirectories(usersPath)
			// 			.Select(userDir => Path.Combine(userDir, "AppData", "Roaming", "discord", "Local Storage", "leveldb"))
			// 			.Where(Directory.Exists);
			// 	})
			// 	.Any(leveldbPath =>
			// 	{
			// 		var accounts = DiscordHelper.GetAccountData(leveldbPath);
			// 		return accounts != null && accounts.Count > 0;
			// 	});

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

			DiscordAccount = DriveInfo.GetDrives()
				.Where(d => d.DriveType == DriveType.Fixed && d.Name != systemDrive)
				.SelectMany(d =>
				{
					string usersPath = Path.Combine(d.Name, "Users");
					if (!Directory.Exists(usersPath)) return [];

					return Directory.GetDirectories(usersPath)
						.SelectMany(userDir => browserPaths.Keys.Select(browserPath => new { Path = Path.Combine(userDir, browserPath), Browser = browserPaths[browserPath] }))
						.Where(x => Directory.Exists(x.Path));
				})
				.Any(databasePath =>
				{
					try
					{
						var tokenNode = DatabaseHelper.Read(databasePath.Path, "_https://discord.com", "token");
						string token = tokenNode?.ToString();
						return !string.IsNullOrEmpty(token);
					}
					catch
					{
						return false;
					}
				});

			var nics = DeviceHelper.GetDevices(DeviceType.NIC);
            Wifi = nics.Any(device => device.NicType == NicDeviceType.WiFi);
            TxIntDelay = nics.Any(device => Registry.LocalMachine.OpenSubKey(device.RegistryPath).GetValue("TxIntDelay") != null);
            NetAdapterCx = nics.Any(device => device.IsActive && device.DriverType == NicDriverType.NetAdapterCx);
        });

        InstallPage.Info.Severity = InfoBarSeverity.Informational;
        InstallPage.Progress.ClearValue(ProgressBar.ForegroundProperty);
		TaskbarHelper.SetProgressState(WindowHandle, TaskbarStates.Normal);
        InstallPage.ProgressRingControl.Foreground = null;
		TaskbarHelper.SetProgressValue(WindowHandle, InstallPage.Progress.Value, 100);
    }
}

