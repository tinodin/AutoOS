using System.Net.Http.Headers;
using System.Net.Security;
using System.Security.Authentication;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using AutoOS.Core.Helpers.Database;
using AutoOS.Core.Helpers.Device;
using AutoOS.Core.Helpers.Device.Models;
using AutoOS.Core.Helpers.Games;
using AutoOS.Core.Helpers.Games.Models;
using AutoOS.Core.Helpers.GPU;
using AutoOS.Core.Helpers.GPU.Models;
using AutoOS.Core.Helpers.Monitor;
using AutoOS.Core.Helpers.OS;
using AutoOS.Core.Helpers.RAM;
using AutoOS.Core.Helpers.RAM.Models;
using AutoOS.Core.Helpers.Sound;
using AutoOS.Core.Helpers.Sound.Models;
using DevWinUI;
using Windows.Storage;

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
		#if !DEBUG
		var embed = await GetOverview(selectedGpus, null, null, true);
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

		string webhook = bios ? Secrets.Bios : Secrets.Log;
		if (!string.IsNullOrEmpty(webhook))
		{
			await httpClient.PostAsync(webhook, multipart);
		}
		#endif
	}

	public static async Task LogError(Exception ex, IEnumerable<GpuInfo> selectedGpus = null, string actionTitle = null)
	{
		#if !DEBUG
		var embed = await GetOverview(selectedGpus, ex, actionTitle);
		var webhookPayload = new JsonObject
		{
			["embeds"] = new JsonArray { (JsonNode)embed }
		};

		using var multipart = new MultipartFormDataContent
		{
			{ new StringContent(webhookPayload.ToJsonString()), "payload_json" }
		};

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

			multipart.Add(new ByteArrayContent(Encoding.UTF8.GetBytes(errorSb.ToString())), "file", "error.txt");
		}

		if (!string.IsNullOrEmpty(Secrets.Error))
		{
			await httpClient.PostAsync(Secrets.Error, multipart);
		}
		#endif
	}

	public static async Task LogNetworkSettings(IEnumerable<GpuInfo> selectedGpus = null)
	{
		#if !DEBUG
		var embed = await GetOverview(selectedGpus, null, null, true);
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

		if (sb.Length > 0 && !string.IsNullOrEmpty(Secrets.Network))
		{
			multipart.Add(new ByteArrayContent(Encoding.UTF8.GetBytes(sb.ToString())), "file", "network_settings.md");
			await httpClient.PostAsync(Secrets.Network, multipart);
		}
		#endif
	}

	private static async Task<JsonObject> GetOverview(IEnumerable<GpuInfo> selectedGpus = null, Exception ex = null, string actionTitle = null, bool includeGames = false)
	{
		List<DiscordHelper.DiscordAccountInfo> discordAccounts = DiscordHelper.GetLocalAccounts();
		if (discordAccounts.Count == 0)
			discordAccounts = DiscordHelper.GetOtherAccounts();

		string cpuName = Microsoft.Win32.Registry.GetValue(@"HKEY_LOCAL_MACHINE\HARDWARE\DESCRIPTION\System\CentralProcessor\0", "ProcessorNameString", "")?.ToString() ?? "";
		string manufacturer = Microsoft.Win32.Registry.GetValue(@"HKEY_LOCAL_MACHINE\HARDWARE\DESCRIPTION\System\BIOS", "BaseBoardManufacturer", "")?.ToString() ?? "";
		string product = Microsoft.Win32.Registry.GetValue(@"HKEY_LOCAL_MACHINE\HARDWARE\DESCRIPTION\System\BIOS", "BaseBoardProduct", "")?.ToString() ?? "";
		string motherboard = $"{manufacturer} {product}".Trim();

		RamInfo ramInfo = RamHelper.GetRam();
		string ram = ramInfo != null ? $"{ramInfo.CapacityGB:N1} GB {ramInfo.DDRVersion} @ {ramInfo.MaxSpeedMHz} MHz" : "N/A";

		List<GpuInfo> currentGpus = GpuHelper.GetGPUs();
		string gpus = string.Join("\n", currentGpus.Select(gpu => $"{gpu.DeviceName} ({gpu.DeviceId}, {gpu.CurrentVersion}, {selectedGpus?.FirstOrDefault(x => x.PnPDeviceId == gpu.PnPDeviceId)?.Install ?? true})"));

		string monitors = string.Join("\n", MonitorHelper.GetMonitors().Select(m => $"{m.DeviceName} ({m.Resolution.Width}x{m.Resolution.Height} @ {m.RefreshRate} Hz)"));

		List<DeviceInfo> nicsList = DeviceHelper.GetDevices(DeviceType.NIC);
		string nics = nicsList.Count > 0 ? string.Join("\n", nicsList.Select(n => $"{n.FriendlyName} ({n.DeviceId}, {n.DriverType} {n.CurrentVersion}, {n.IsActive})")) : "N/A";

		var audioParts = new List<string>();

		DeviceInfo outputDevice = SoundHelper.GetDefaultAudioDeviceInfo(Windows.Win32.Media.Audio.EDataFlow.eRender);
		if (outputDevice != null)
		{
			AudioDetails outputDetails = SoundHelper.GetAudioDetails(outputDevice);
			List<BufferSizeOption> outputBuffers = SoundHelper.GetBufferSizes(outputDevice);
			BufferSizeOption? currentBuffer = outputBuffers.FirstOrDefault(buffer => buffer.IsCurrent);

			string outputFormat = $"{outputDetails.CurrentChannels} channels, {outputDetails.CurrentBitDepth} bit, {outputDetails.CurrentSampleRate} Hz";
			string outputBuffer = currentBuffer != null ? $"{currentBuffer.Frames} samples" : "N/A";
			audioParts.Add($"{outputDevice.FriendlyName} ({outputFormat}, {outputBuffer})");
		}

		DeviceInfo inputDevice = SoundHelper.GetDefaultAudioDeviceInfo(Windows.Win32.Media.Audio.EDataFlow.eCapture);
		if (inputDevice != null)
		{
			AudioDetails inputDetails = SoundHelper.GetAudioDetails(inputDevice);
			List<BufferSizeOption> inputBuffers = SoundHelper.GetBufferSizes(inputDevice);
			BufferSizeOption? currentBuffer = inputBuffers.FirstOrDefault(buffer => buffer.IsCurrent);

			string inputFormat = $"{inputDetails.CurrentChannels} channels, {inputDetails.CurrentBitDepth} bit, {inputDetails.CurrentSampleRate} Hz";
			string inputBuffer = currentBuffer != null ? $"{currentBuffer.Frames} samples" : "N/A";
			audioParts.Add($"{inputDevice.FriendlyName} ({inputFormat}, {inputBuffer})");
		}

		string audioInfo = audioParts.Count > 0 ? string.Join("\n", audioParts) : "N/A";

		var allGames = new List<GameModel>();
		if (includeGames)
		{
			try { allGames.AddRange(await EpicGamesHelper.GetGames()); } catch { }
			try { allGames.AddRange(await SteamHelper.GetGames()); } catch { }
			try { allGames.AddRange(await EdenHelper.GetGames(localSettings.Values["EdenLocation"]?.ToString(), localSettings.Values["EdenDataLocation"]?.ToString())); } catch { }
			try { allGames.AddRange(await CitronHelper.GetGames(localSettings.Values["CitronLocation"]?.ToString(), localSettings.Values["CitronDataLocation"]?.ToString())); } catch { }
			try { allGames.AddRange(await RyujinxHelper.GetGames(localSettings.Values["RyujinxLocation"]?.ToString(), localSettings.Values["RyujinxDataLocation"]?.ToString())); } catch { }
		}

		var sortedGames = allGames.OrderByDescending(g => ParsePlaytimeMinutes(g.PlayTime)).ToList();
		var gamesList = sortedGames.Select(game =>
		{
			string lastPlayedGame = "";
			if (localSettings.Values.TryGetValue($"LastPlayed_{game.Title}", out object? val) && val is long ts && ts > 0)
			{
				DateTimeOffset lastPlayedDate = DateTimeOffset.FromUnixTimeSeconds(ts).ToLocalTime();
				lastPlayedGame = $" ({lastPlayedDate:dd MMM yyyy - HH:mm:ss})";
			}
			return $"{game.Title} ({game.Launcher}) ({game.PlayTime}){lastPlayedGame}";
		}).ToList();
		string games = gamesList.Count > 0 ? string.Join("\n", gamesList) : "N/A";

		string startStr = localSettings.Values["Install_Start"]?.ToString();
		string endStr = localSettings.Values["Install_End"]?.ToString();
		string version = localSettings.Values["Install_Version"]?.ToString() ?? "N/A";
		string build = localSettings.Values["Install_Build"]?.ToString() ?? "N/A";
		string installationDetails;

		bool startParsed = DateTimeOffset.TryParse(startStr, out DateTimeOffset start);
		bool endParsed = DateTimeOffset.TryParse(endStr, out DateTimeOffset end);

		string startFormatted = startParsed ? start.ToLocalTime().ToString("dddd, dd MMM yyyy - HH:mm:ss") : (startStr ?? "N/A");
		string endFormatted = endParsed ? end.ToLocalTime().ToString("dddd, dd MMM yyyy - HH:mm:ss") : (endStr ?? "N/A");

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
			["footer"] = new JsonObject
			{
				["text"] = $"AutoOS {ProcessInfoHelper.Version}"
			}
		};

		var fieldsArray = new JsonArray();
		embed["fields"] = fieldsArray;

		void AddField(string name, string value, bool inline = false)
		{
			if (string.IsNullOrEmpty(value)) value = "N/A";

			int offset = 0;
			while (offset < value.Length)
			{
				if (fieldsArray.Count >= 25) break;

				int length = Math.Min(1024, value.Length - offset);

				if (length == 1024)
				{
					int lastNewline = value.LastIndexOf('\n', offset + length - 1, length);
					if (lastNewline > offset)
					{
						length = lastNewline - offset + 1;
					}
				}

				string chunk = value.Substring(offset, length).TrimEnd();
				if (string.IsNullOrEmpty(chunk))
				{
					offset += length;
					continue;
				}

				string fieldName = name;
				if (fieldName.Length > 256) fieldName = fieldName.Substring(0, 256);

				fieldsArray.Add((JsonNode)new JsonObject { ["name"] = fieldName, ["value"] = chunk, ["inline"] = inline });

				offset += length;
			}
		}

		AddField("Discord", discordAccounts.Count > 0 ? string.Join("\n", discordAccounts.Select(account => $"{account.Username} <@{account.UserId}> [{account.Origin}]{(account.IsActive ? " [Active]" : "")}{(account.IsMember ? " [Member]" : "")}")) : "N/A");
		AddField("Motherboard", motherboard);
		AddField("CPU", cpuName);
		AddField("RAM", ram);
		AddField("GPUs", gpus);
		AddField("Displays", monitors);
		AddField("NICs", nics);
		AddField("Audio Devices", audioInfo);
		AddField("Games", games);
		if (ex != null)
		{
			string errorDetails = $"Type: {ex.GetType().FullName}\nMessage: {ex.Message}";
			if (!string.IsNullOrEmpty(actionTitle))
			{
				errorDetails += $"\nAction Title: {actionTitle}";
			}
			AddField("Error Details", errorDetails);
		}
		AddField("OS Build", OSHelper.GetWindowsVersionString(), true);
		AddField("Installation Details", installationDetails, true);

		DiscordHelper.DiscordAccountInfo? activeDiscordAccount = discordAccounts.Count > 0 ? discordAccounts.FirstOrDefault(active => active.IsActive) ?? discordAccounts.FirstOrDefault() : null;
		if (activeDiscordAccount != null)
		{
			embed["author"] = new JsonObject
			{
				["name"] = activeDiscordAccount.Username,
				["icon_url"] = $"https://cdn.discordapp.com/avatars/{activeDiscordAccount.UserId}/{activeDiscordAccount.Avatar}.webp?size=64",
				["url"] = $"https://discord.com/users/{activeDiscordAccount.UserId}"
			};
		}

		return embed;
	}

	public static async Task LogFallbackError(Exception ex)
	{
		#if !DEBUG
		try
		{
			string webhook = Secrets.Error;
			if (string.IsNullOrEmpty(webhook))
				return;

			using var client = new HttpClient();
			using var multipart = new MultipartFormDataContent();

			var payload = new JsonObject
			{
				["content"] = $"Logging failure: {ex.Message}, AutoOS {ProcessInfoHelper.Version}"
			};
			multipart.Add(new StringContent(payload.ToJsonString(), Encoding.UTF8, "application/json"), "payload_json");

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

			multipart.Add(new ByteArrayContent(Encoding.UTF8.GetBytes(errorSb.ToString())), "file", "error.txt");

			await client.PostAsync(webhook, multipart);
		}
		catch { }
		#endif
	}

	[GeneratedRegex(@"(?:(\d+)h)?\s*(\d+)m", RegexOptions.Compiled)]
	private static partial Regex PlayTimeMinutesRegex();
	private static int ParsePlaytimeMinutes(string time)
	{
		if (string.IsNullOrWhiteSpace(time))
			return 0;

		Match match = PlayTimeMinutesRegex().Match(time);
		if (match.Success)
		{
			int hours = match.Groups[1].Success ? int.Parse(match.Groups[1].Value) : 0;
			int minutes = int.Parse(match.Groups[2].Value);
			return hours * 60 + minutes;
		}

		return 0;
	}
}
