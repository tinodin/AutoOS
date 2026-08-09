using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using AutoOS.Core.Common;
using AutoOS.Core.Helpers.Download;
using AutoOS.Core.Helpers.Logging;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Store.Preview.InstallControl;
using Windows.Foundation;
using Windows.Management.Deployment;
using Windows.Storage;

namespace AutoOS.Core.Helpers.Store;

public static partial class StoreHelper
{
	private class StoreInfo
	{
		public string Name { get; set; } = string.Empty;
		public string FileExtension { get; set; } = string.Empty;
		public string ResourceUri { get; set; } = string.Empty;
		public string Hash { get; set; } = string.Empty;
		public string UpdateIdentifier { get; set; } = string.Empty;
		public string Revision { get; set; } = string.Empty;
		public DateTime LastModified { get; set; }
		public string Version { get; set; } = string.Empty;
	}

	private static readonly HttpClient httpClient = new();
	private static string? sessionToken;

	[GeneratedRegex(@"^(.+?)_(\d+\.\d+\.\d+\.\d+)_([a-zA-Z0-9]+)_(.*?)_([a-hjkmnp-tv-z0-9]{13})$", RegexOptions.Compiled)]
	private static partial Regex PackageIdentityRegex();

	public static async Task Download(string identifier, int index = 0, IStatusReporter? reporter = null)
	{
		string product = await GetProductID(identifier);
		if (string.IsNullOrEmpty(product))
		{
			await LogHelper.LogError(new Exception($"[StoreHelper] ProductID not found for {identifier}."));
			return;
		}

		string category = await GetCategoryID(product);
		if (string.IsNullOrEmpty(category))
		{
			await LogHelper.LogError(new Exception($"[StoreHelper] CategoryID not found for {identifier} (Product: {product})."));
			return;
		}

		string folderPath = Path.Combine(Path.Combine(Path.GetTempPath(), "StoreHelper"), identifier);
		Directory.CreateDirectory(folderPath);

		try
		{
			List<StoreInfo> files = await GetFiles(identifier, category, index);
			if (files.Count == 0)
			{
				await LogHelper.LogError(new Exception($"[StoreHelper] No files found for {identifier}"), actionTitle: $"[StoreHelper] Download failed for {identifier}");
				return;
			}
			StoreInfo main = files.First();
			Debug.WriteLine($"[StoreHelper] Selected Package: {main.Name}");

			await DownloadHelper.Download(main.ResourceUri, folderPath, reporter: reporter);
		}
		catch (Exception ex)
		{
			await LogHelper.LogError(ex, actionTitle: $"[StoreHelper] Download failed for {identifier}");
		}
	}

	public static async Task Install(string identifier)
	{
		string folderPath = Path.Combine(Path.Combine(Path.GetTempPath(), "StoreHelper"), identifier);

		try
		{
			var allFiles = Directory.GetFiles(folderPath, "*.*", SearchOption.TopDirectoryOnly)
				.Where(f => f.EndsWith(".appx", StringComparison.OrdinalIgnoreCase) ||
							f.EndsWith(".appxbundle", StringComparison.OrdinalIgnoreCase) ||
							f.EndsWith(".msix", StringComparison.OrdinalIgnoreCase) ||
							f.EndsWith(".msixbundle", StringComparison.OrdinalIgnoreCase))
				.ToList();

			string mainPath = allFiles.FirstOrDefault(f => Path.GetFileName(f).StartsWith(identifier.Split('_')[0], StringComparison.OrdinalIgnoreCase)) ?? allFiles.First();

			await new PackageManager().StagePackageAsync(new Uri(mainPath), null);
			await new PackageManager().ProvisionPackageForAllUsersAsync(identifier);
			await new PackageManager().AddPackageAsync(new Uri(mainPath), null, DeploymentOptions.ForceApplicationShutdown);
		}
		catch (Exception ex)
		{
			await LogHelper.LogError(ex, actionTitle: $"[StoreHelper] Installation failed for {identifier}");
		}
		finally
		{
			Directory.Delete(folderPath, true);
		}
	}

	public static async Task Remove(string packageFamilyName)
	{
		try
		{
			var manager = new PackageManager();

			foreach (Package? package in manager.FindPackagesForUser(string.Empty, packageFamilyName))
			{
				await manager.RemovePackageAsync(package.Id.FullName, RemovalOptions.RemoveForAllUsers);
			}
		}
		catch (UnauthorizedAccessException)
		{
			ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
			int actionIndex = localSettings.Values["actionIndex"] as int? ?? 0;
			localSettings.Values["actionIndex"] = actionIndex - 1;
			Process.Start(new ProcessStartInfo { FileName = "shutdown", Arguments = "/r /t 0", CreateNoWindow = true, UseShellExecute = false });
		}
	}

	public static async Task Deprovision(string packageFamilyName)
	{
		try
		{
			await new PackageManager().DeprovisionPackageForAllUsersAsync(packageFamilyName);
		}
		catch (UnauthorizedAccessException)
		{
			ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
			int actionIndex = localSettings.Values["actionIndex"] as int? ?? 0;
			localSettings.Values["actionIndex"] = actionIndex - 1;
			Process.Start(new ProcessStartInfo { FileName = "shutdown", Arguments = "/r /t 0", CreateNoWindow = true, UseShellExecute = false });
		}
	}

	public static async Task<List<AppInstallItem>> CheckForUpdates()
	{
		var installManager = new AppInstallManager();

		AppUpdateOptions updateOptions = new()
		{
			AutomaticallyDownloadAndInstallUpdateIfFound = false,
			AllowForcedAppRestart = true
		};

		IAsyncOperation<IReadOnlyList<AppInstallItem>> operation = installManager.SearchForAllUpdatesAsync(string.Empty, string.Empty, updateOptions);

		IReadOnlyList<AppInstallItem> results = await operation;

		return [.. results];
	}

	public static async Task Update(string identifier, IStatusReporter? reporter = null)
	{
		SynchronizationContext? uiContext = SynchronizationContext.Current;

		reporter?.Report(isIndeterminate: true, progress: 0);

		var installManager = new AppInstallManager();

		var tcs = new TaskCompletionSource<bool>();

		void OnItemStatusChanged(AppInstallManager sender, AppInstallManagerItemEventArgs args)
		{
			if (args.Item.PackageFamilyName == identifier)
			{
				AppInstallStatus status = args.Item.GetCurrentStatus();

				uiContext?.Post(_ =>
				{
					reporter?.Report($"{status.BytesDownloaded / (1024.0 * 1024.0):F2} MB of {status.DownloadSizeInBytes / (1024.0 * 1024.0):F2} MB", status.PercentComplete, false);
				}, null);

				if (status.InstallState == AppInstallState.Completed || status.InstallState == AppInstallState.Canceled || status.InstallState == AppInstallState.Error)
				{
					tcs.TrySetResult(true);
				}
			}
		}

		installManager.ItemStatusChanged += OnItemStatusChanged;

		AppInstallItem updateItem = await installManager.UpdateAppByPackageFamilyNameAsync(identifier);

		if (updateItem == null)
		{
			installManager.ItemStatusChanged -= OnItemStatusChanged;
			return;
		}

		AppInstallStatus initialStatus = updateItem.GetCurrentStatus();
		if (initialStatus.InstallState == AppInstallState.Completed || initialStatus.InstallState == AppInstallState.Canceled || initialStatus.InstallState == AppInstallState.Error)
		{
			installManager.ItemStatusChanged -= OnItemStatusChanged;
			return;
		}

		await tcs.Task;
		installManager.ItemStatusChanged -= OnItemStatusChanged;

		uiContext?.Post(_ =>
		{
			reporter?.Report(isIndeterminate: true);
		}, null);
	}

	public static string GetVersion(string packageFamilyName)
	{
		var manager = new PackageManager();
		foreach (Package? package in manager.FindPackagesForUser(string.Empty, packageFamilyName))
		{
			PackageVersion version = package.Id.Version;
			return $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
		}
		return string.Empty;
	}

	public static async Task KillProcesses(string packageFamilyName)
	{
		try
		{
			var manager = new PackageManager();
			Package? package = manager.FindPackagesForUser(string.Empty, packageFamilyName).FirstOrDefault();
			if (package == null) return;

			string manifestPath = Path.Combine(package.InstalledLocation.Path, "AppxManifest.xml");
			if (!File.Exists(manifestPath)) return;

			var doc = XDocument.Load(manifestPath);
			XNamespace ns = doc.Root!.Name.Namespace;
			IEnumerable<XElement> applications = doc.Descendants(ns + "Application");

			foreach (XElement app in applications)
			{
				string? exe = app.Attribute("Executable")?.Value;
				if (!string.IsNullOrEmpty(exe))
				{
					string processName = Path.GetFileNameWithoutExtension(exe);
					foreach (Process process in Process.GetProcessesByName(processName))
					{
						try
						{
							process.Kill();
							await process.WaitForExitAsync();
						}
						catch { }
					}
				}
			}
		}
		catch { }

		await Task.Delay(1000);
	}

	private static async Task<string> GetProductID(string term)
	{
		string dcatUrl = $"https://displaycatalog.mp.microsoft.com/v7.0/products/lookup?alternateId=PackageFamilyName&Value={Uri.EscapeDataString(term)}&market=US&languages=en-US";
		string dcatRaw = await httpClient.GetStringAsync(dcatUrl);
		using var dcatJson = JsonDocument.Parse(dcatRaw);
		if (dcatJson.RootElement.TryGetProperty("Products", out JsonElement prods) && prods.GetArrayLength() > 0)
		{
			if (prods[0].TryGetProperty("ProductId", out JsonElement pid))
				return pid.GetString() ?? string.Empty;
		}

		string searchBase = term.Split('_')[0];
		string raw = await httpClient.GetStringAsync($"https://apps.microsoft.com/api/products/search?gl=US&hl=en-us&query={Uri.EscapeDataString(searchBase)}&mediaType=all");
		using var json = JsonDocument.Parse(raw);

		var targets = new List<JsonElement>();
		if (json.RootElement.TryGetProperty("highlightedList", out JsonElement h)) targets.AddRange(h.EnumerateArray());
		if (json.RootElement.TryGetProperty("productsList", out JsonElement p)) targets.AddRange(p.EnumerateArray());

		foreach (JsonElement item in targets)
		{
			string? title = item.TryGetProperty("title", out JsonElement t) ? t.GetString() : null;
			if (title != null && title.Equals(searchBase, StringComparison.OrdinalIgnoreCase))
				return item.GetProperty("productId").GetString() ?? string.Empty;

			if (item.TryGetProperty("packageFamilyNames", out JsonElement pfnList))
			{
				foreach (JsonElement pfn in pfnList.EnumerateArray())
				{
					if (pfn.GetString()?.Contains(term, StringComparison.OrdinalIgnoreCase) == true ||
						pfn.GetString()?.Contains(searchBase, StringComparison.OrdinalIgnoreCase) == true)
						return item.GetProperty("productId").GetString() ?? string.Empty;
				}
			}
		}
		return string.Empty;
	}

	private static async Task<string> GetCategoryID(string productId)
	{
		string url = $"https://storeedgefd.dsx.mp.microsoft.com/v9.0/products/{productId}?market=US&locale=en-us&deviceFamily=Windows.Desktop";
		HttpResponseMessage response = await httpClient.GetAsync(url);
		if (response.IsSuccessStatusCode)
		{
			string jsonResponse = await response.Content.ReadAsStringAsync();
			using JsonDocument document = JsonDocument.Parse(jsonResponse);

			if (document.RootElement.TryGetProperty("Payload", out JsonElement payload) && payload.TryGetProperty("Skus", out JsonElement skus) && skus.ValueKind == JsonValueKind.Array && skus.GetArrayLength() > 0)
			{
				JsonElement firstSku = skus[0];
				if (firstSku.TryGetProperty("FulfillmentData", out JsonElement fulfillmentDataElement))
				{
					string? fulfillmentData = fulfillmentDataElement.GetString();
					if (!string.IsNullOrEmpty(fulfillmentData))
					{
						Match match = Regex.Match(fulfillmentData, "\"WuCategoryId\":\"([^\"]+)\"");
						if (match.Success && match.Groups.Count > 1)
						{
							return match.Groups[1].Value;
						}
					}
				}
			}
		}
		return string.Empty;
	}

	private static async Task<List<StoreInfo>> GetFiles(string name, string catId, int index = 0)
	{
		string token = await Auth();

		string response = await PostSoap(SoapTemplates.Sync(token, catId, "WIF"), "https://fe3.delivery.mp.microsoft.com/ClientWebService/client.asmx");

		if (string.IsNullOrEmpty(response)) return [];

		var doc = XDocument.Parse(response.Replace("&lt;", "<").Replace("&gt;", ">"));

		var filePool = new Dictionary<string, (string ext, string hash, DateTime modified)>();
		foreach (XElement? node in doc.Descendants().Where(x => x.Name.LocalName == "File"))
		{
			string? id = node.Attribute("InstallerSpecificIdentifier")?.Value;
			string? hash = node.Attribute("Digest")?.Value;
			string? fileName = node.Attribute("FileName")?.Value;
			string? lastModified = node.Attribute("Modified")?.Value;
			_ = DateTime.TryParse(lastModified, out DateTime modified);

			if (id != null && hash != null && fileName != null)
			{
				if (!filePool.TryGetValue(id, out (string ext, string hash, DateTime modified) existing) || modified > existing.modified)
				{
					filePool[id] = (Path.GetExtension(fileName).TrimStart('.'), hash, modified);
				}
			}
		}

		string arch = RuntimeInformation.OSArchitecture.ToString().ToLower();
		var results = new List<StoreInfo>();

		string searchName = name.Split('_')[0];

		foreach (XElement? node in doc.Descendants().Where(x => x.Name.LocalName == "SecuredFragment"))
		{
			XElement? root = node.Parent?.Parent;
			if (root == null) continue;

			XElement? appxMetadata = root.Descendants().FirstOrDefault(x => x.Name.LocalName == "AppxMetadata");
			string? identity = appxMetadata?.Attribute("PackageMoniker")?.Value;
			string? versionStr = appxMetadata?.Attribute("Version")?.Value;

			if (identity != null)
			{
				Match match = PackageIdentityRegex().Match(identity);
				if (match.Success)
				{
					if (string.IsNullOrEmpty(versionStr)) versionStr = match.Groups[2].Value;
				}
			}

			if (identity != null && filePool.TryGetValue(identity, out (string ext, string hash, DateTime modified) info))
			{
				if (!identity.Contains(arch) && !identity.Contains("neutral") && !identity.Contains("x86")) continue;
				if (info.ext.StartsWith("e")) continue;

				if (identity.StartsWith(searchName, StringComparison.OrdinalIgnoreCase))
				{
					XElement? idNode = root.Descendants().FirstOrDefault(x => x.Name.LocalName == "UpdateIdentity");
					if (idNode == null) continue;

					string? updateIdentifier = idNode.Attribute("UpdateID")?.Value;
					string? revision = idNode.Attribute("RevisionNumber")?.Value;

					string link = await ResolveUrl(updateIdentifier ?? string.Empty, revision ?? string.Empty, info.hash);
					var infoItem = new StoreInfo
					{
						Name = identity,
						FileExtension = info.ext,
						ResourceUri = link,
						Hash = info.hash,
						UpdateIdentifier = updateIdentifier ?? string.Empty,
						Revision = revision ?? string.Empty,
						LastModified = info.modified,
						Version = versionStr ?? string.Empty
					};
					results.Add(infoItem);
					Debug.WriteLine($"[StoreHelper] Package found: {infoItem.Name}, Version: {infoItem.Version}, Modified: {infoItem.LastModified:yyyy-MM-dd HH:mm:ss}");
				}
			}
		}

		return [.. results
		.OrderByDescending(r => Version.TryParse(r.Version, out Version? v) ? v : new Version(0, 0, 0, 0))
		.ThenByDescending(r => r.Name.Contains(arch))
		.ThenByDescending(r => r.Name.Contains("neutral"))
		.ThenByDescending(r => r.LastModified)
		.Skip(index)
		.Take(1)];
	}

	private static async Task<string> Auth()
	{
		if (sessionToken != null) return sessionToken;
		string res = await PostSoap(SoapTemplates.Cookie, "https://fe3.delivery.mp.microsoft.com/ClientWebService/client.asmx");
		if (string.IsNullOrEmpty(res)) return string.Empty;

		var doc = XDocument.Parse(res);
		XElement? encrypted = doc.Descendants().FirstOrDefault(x => x.Name.LocalName == "EncryptedData");

		sessionToken = encrypted!.Value;
		return sessionToken ?? string.Empty;
	}

	private static async Task<string> ResolveUrl(string uid, string rev, string hash)
	{
		string payload = SoapTemplates.Url(uid, rev, "WIF");
		string response = await PostSoap(payload, "https://fe3.delivery.mp.microsoft.com/ClientWebService/client.asmx/secured");
		if (string.IsNullOrEmpty(response)) return string.Empty;

		var doc = XDocument.Parse(response);

		foreach (XElement? node in doc.Descendants().Where(x => x.Name.LocalName == "FileLocation"))
		{
			string? digest = node.Descendants().FirstOrDefault(x => x.Name.LocalName == "FileDigest")?.Value;
			if (digest == hash)
				return node.Descendants().FirstOrDefault(x => x.Name.LocalName == "Url")?.Value ?? string.Empty;
		}
		return string.Empty;
	}

	private static async Task<string> PostSoap(string body, string url)
	{
		var req = new HttpRequestMessage(HttpMethod.Post, url)
		{
			Content = new StringContent(body, Encoding.UTF8, "application/soap+xml")
		};

		HttpResponseMessage res = await httpClient.SendAsync(req);
		if (!res.IsSuccessStatusCode)
		{
			string error = await res.Content.ReadAsStringAsync();
			Debug.WriteLine($"[StoreHelper] SOAP request failed: {res.StatusCode} - {error}");
			return string.Empty;
		}
		return await res.Content.ReadAsStringAsync();
	}
}

internal static class SoapTemplates
{
	public const string Cookie = @"
	<Envelope xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns=""http://www.w3.org/2003/05/soap-envelope"">
		<Header>
			<Action d3p1:mustUnderstand=""1"" xmlns:d3p1=""http://www.w3.org/2003/05/soap-envelope"" xmlns=""http://www.w3.org/2005/08/addressing"">http://www.microsoft.com/SoftwareDistribution/Server/ClientWebService/GetCookie</Action>
			<MessageID xmlns=""http://www.w3.org/2005/08/addressing"">urn:uuid:b9b43757-2247-4d7b-ae8f-a71ba8a22386</MessageID>
			<To d3p1:mustUnderstand=""1"" xmlns:d3p1=""http://www.w3.org/2003/05/soap-envelope"" xmlns=""http://www.w3.org/2005/08/addressing"">https://fe3.delivery.mp.microsoft.com/ClientWebService/client.asmx</To>
			<Security d3p1:mustUnderstand=""1"" xmlns:d3p1=""http://www.w3.org/2003/05/soap-envelope"" xmlns=""http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd"">
				<Timestamp xmlns=""http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd"">
					<Created>2017-12-02T00:16:15.210Z</Created>
					<Expires>2017-12-29T06:25:43.943Z</Expires>
				</Timestamp>
				<WindowsUpdateTicketsToken d4p1:id=""ClientMSA"" xmlns:d4p1=""http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd"" xmlns=""http://schemas.microsoft.com/msus/2014/10/WindowsUpdateAuthorization"">
					<TicketType Name=""MSA"" Version=""1.0"" Policy=""MBI_SSL"">
						<User />
					</TicketType>
				</WindowsUpdateTicketsToken>
			</Security>
		</Header>
		<Body>
			<GetCookie xmlns=""http://www.microsoft.com/SoftwareDistribution/Server/ClientWebService"">
				<oldCookie>
				</oldCookie>
				<lastChange>2015-10-21T17:01:07.1472913Z</lastChange>
				<currentTime>2017-12-02T00:16:15.217Z</currentTime>
				<protocolVersion>1.40</protocolVersion>
			</GetCookie>
		</Body>
	</Envelope>
	";

	public static string Sync(string tk, string cid, string ring) => @"
	<s:Envelope
		xmlns:a=""http://www.w3.org/2005/08/addressing""
		xmlns:s=""http://www.w3.org/2003/05/soap-envelope"">
		<s:Header>
			<a:Action s:mustUnderstand=""1"">http://www.microsoft.com/SoftwareDistribution/Server/ClientWebService/SyncUpdates</a:Action>
			<a:MessageID>urn:uuid:175df68c-4b91-41ee-b70b-f2208c65438e</a:MessageID>
			<a:To s:mustUnderstand=""1"">https://fe3.delivery.mp.microsoft.com/ClientWebService/client.asmx</a:To>
			<o:Security s:mustUnderstand=""1""
				xmlns:o=""http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd"">
				<Timestamp
					xmlns=""http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd"">
					<Created>2017-08-05T02:03:05.038Z</Created>
					<Expires>2017-08-05T02:08:05.038Z</Expires>
				</Timestamp>
				<wuws:WindowsUpdateTicketsToken wsu:id=""ClientMSA""
					xmlns:wsu=""http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd""
					xmlns:wuws=""http://schemas.microsoft.com/msus/2014/10/WindowsUpdateAuthorization"">
					<TicketType Name=""MSA"" Version=""1.0"" Policy=""MBI_SSL"">
						<Device>dAA9AEUAdwBBAHcAQQBzAE4AMwBCAEEAQQBVADEAYgB5AHMAZQBtAGIAZQBEAFYAQwArADMAZgBtADcAbwBXAHkASAA3AGIAbgBnAEcAWQBtAEEAQQBMAGoAbQBqAFYAVQB2AFEAYwA0AEsAVwBFAC8AYwBDAEwANQBYAGUANABnAHYAWABkAGkAegBHAGwAZABjADEAZAAvAFcAeQAvAHgASgBQAG4AVwBRAGUAYwBtAHYAbwBjAGkAZwA5AGoAZABwAE4AawBIAG0AYQBzAHAAVABKAEwARAArAFAAYwBBAFgAbQAvAFQAcAA3AEgAagBzAEYANAA0AEgAdABsAC8AMQBtAHUAcgAwAFMAdQBtAG8AMABZAGEAdgBqAFIANwArADQAcABoAC8AcwA4ADEANgBFAFkANQBNAFIAbQBnAFIAQwA2ADMAQwBSAEoAQQBVAHYAZgBzADQAaQB2AHgAYwB5AEwAbAA2AHoAOABlAHgAMABrAFgAOQBPAHcAYQB0ADEAdQBwAFMAOAAxAEgANgA4AEEASABzAEoAegBnAFQAQQBMAG8AbgBBADIAWQBBAEEAQQBpAGcANQBJADMAUQAvAFYASABLAHcANABBAEIAcQA5AFMAcQBhADEAQgA4AGsAVQAxAGEAbwBLAEEAdQA0AHYAbABWAG4AdwBWADMAUQB6AHMATgBtAEQAaQBqAGgANQBkAEcAcgBpADgAQQBlAEUARQBWAEcAbQBXAGgASQBCAE0AUAAyAEQAVwA0ADMAZABWAGkARABUAHoAVQB0AHQARQBMAEgAaABSAGYAcgBhAGIAWgBsAHQAQQBUAEUATABmAHMARQBGAFUAYQBRAFMASgB4ADUAeQBRADgAagBaAEUAZQAyAHgANABCADMAMQB2AEIAMgBqAC8AUgBLAGEAWQAvAHEAeQB0AHoANwBUAHYAdAB3AHQAagBzADYAUQBYAEIAZQA4AHMAZwBJAG8AOQBiADUAQQBCADcAOAAxAHMANgAvAGQAUwBFAHgATgBEAEQAYQBRAHoAQQBYAFAAWABCAFkAdQBYAFEARQBzAE8AegA4AHQAcgBpAGUATQBiAEIAZQBUAFkAOQBiAG8AQgBOAE8AaQBVADcATgBSAEYAOQAzAG8AVgArAFYAQQBiAGgAcAAwAHAAUgBQAFMAZQBmAEcARwBPAHEAdwBTAGcANwA3AHMAaAA5AEoASABNAHAARABNAFMAbgBrAHEAcgAyAGYARgBpAEMAUABrAHcAVgBvAHgANgBuAG4AeABGAEQAbwBXAC8AYQAxAHQAYQBaAHcAegB5AGwATABMADEAMgB3AHUAYgBtADUAdQBtAHAAcQB5AFcAYwBLAFIAagB5AGgAMgBKAFQARgBKAFcANQBnAFgARQBJADUAcAA4ADAARwB1ADIAbgB4AEwAUgBOAHcAaQB3AHIANwBXAE0AUgBBAFYASwBGAFcATQBlAFIAegBsADkAVQBxAGcALwBwAFgALwB2AGUATAB3AFMAawAyAFMAUwBIAGYAYQBLADYAagBhAG8AWQB1AG4AUgBHAHIAOABtAGIARQBvAEgAbABGADYASgBDAGEAYQBUAEIAWABCAGMAdgB1AGUAQwBKAG8AOQA4AGgAUgBBAHIARwB3ADQAKwBQAEgAZQBUAGIATgBTAEUAWABYAHoAdgBaADYAdQBXADUARQBBAGYAZABaAG0AUwA4ADgAVgBKAGMAWgBhAEYASwA3AHgAeABnADAAdwBvAG4ANwBoADAAeABDADYAWgBCADAAYwBZAGoATAByAC8ARwBlAE8AegA5AEcANABRAFUASAA5AEUAawB5ADAAZAB5AEYALwByAGUAVQAxAEkAeQBpAGEAcABwAGgATwBQADgAUwAyAHQANABCAHIAUABaAFgAVAB2AEMAMABQADcAegBPACsAZgBHAGsAeABWAG0AKwBVAGYAWgBiAFEANQA1AHMAdwBFAD0AJgBwAD0A</Device>
					</TicketType>
				</wuws:WindowsUpdateTicketsToken>
			</o:Security>
		</s:Header>
		<s:Body>
			<SyncUpdates
				xmlns=""http://www.microsoft.com/SoftwareDistribution/Server/ClientWebService"">
				<cookie>
					<Expiration>2045-03-11T02:02:48Z</Expiration>
					<EncryptedData>{1}</EncryptedData>
				</cookie>
				<parameters>
					<ExpressQuery>false</ExpressQuery>
					<InstalledNonLeafUpdateIDs>
						<int>1</int>
						<int>2</int>
						<int>3</int>
						<int>11</int>
						<int>19</int>
						<int>544</int>
						<int>549</int>
						<int>2359974</int>
						<int>2359977</int>
						<int>5169044</int>
						<int>8788830</int>
						<int>23110993</int>
						<int>23110994</int>
						<int>54341900</int>
						<int>54343656</int>
						<int>59830006</int>
						<int>59830007</int>
						<int>59830008</int>
						<int>60484010</int>
						<int>62450018</int>
						<int>62450019</int>
						<int>62450020</int>
						<int>66027979</int>
						<int>66053150</int>
						<int>97657898</int>
						<int>98822896</int>
						<int>98959022</int>
						<int>98959023</int>
						<int>98959024</int>
						<int>98959025</int>
						<int>98959026</int>
						<int>104433538</int>
						<int>104900364</int>
						<int>105489019</int>
						<int>117765322</int>
						<int>129905029</int>
						<int>130040031</int>
						<int>132387090</int>
						<int>132393049</int>
						<int>133399034</int>
						<int>138537048</int>
						<int>140377312</int>
						<int>143747671</int>
						<int>158941041</int>
						<int>158941042</int>
						<int>158941043</int>
						<int>158941044</int>
						<int>159123858</int>
						<int>159130928</int>
						<int>164836897</int>
						<int>164847386</int>
						<int>164848327</int>
						<int>164852241</int>
						<int>164852246</int>
						<int>164852252</int>
						<int>164852253</int>
						<int>10</int>
						<int>17</int>
						<int>2359977</int>
						<int>5143990</int>
						<int>5169043</int>
						<int>5169047</int>
						<int>8806526</int>
						<int>9125350</int>
						<int>9154769</int>
						<int>10809856</int>
						<int>23110995</int>
						<int>23110996</int>
						<int>23110999</int>
						<int>23111000</int>
						<int>23111001</int>
						<int>23111002</int>
						<int>23111003</int>
						<int>23111004</int>
						<int>24513870</int>
						<int>28880263</int>
						<int>30077688</int>
						<int>30486944</int>
						<int>30526991</int>
						<int>30528442</int>
						<int>30530496</int>
						<int>30530501</int>
						<int>30530504</int>
						<int>30530962</int>
						<int>30535326</int>
						<int>30536242</int>
						<int>30539913</int>
						<int>30545142</int>
						<int>30545145</int>
						<int>30545488</int>
						<int>30546212</int>
						<int>30547779</int>
						<int>30548797</int>
						<int>30548860</int>
						<int>30549262</int>
						<int>30551160</int>
						<int>30551161</int>
						<int>30551164</int>
						<int>30553016</int>
						<int>30553744</int>
						<int>30554014</int>
						<int>30559008</int>
						<int>30559011</int>
						<int>30560006</int>
						<int>30560011</int>
						<int>30561006</int>
						<int>30563261</int>
						<int>30565215</int>
						<int>30578059</int>
						<int>30664998</int>
						<int>30677904</int>
						<int>30681618</int>
						<int>30682195</int>
						<int>30685055</int>
						<int>30702579</int>
						<int>30708772</int>
						<int>30709591</int>
						<int>30711304</int>
						<int>30715418</int>
						<int>30720106</int>
						<int>30720273</int>
						<int>30732075</int>
						<int>30866952</int>
						<int>30866964</int>
						<int>30870749</int>
						<int>30877852</int>
						<int>30878437</int>
						<int>30890151</int>
						<int>30892149</int>
						<int>30990917</int>
						<int>31049444</int>
						<int>31190936</int>
						<int>31196961</int>
						<int>31197811</int>
						<int>31198836</int>
						<int>31202713</int>
						<int>31203522</int>
						<int>31205442</int>
						<int>31205557</int>
						<int>31207585</int>
						<int>31208440</int>
						<int>31208451</int>
						<int>31209591</int>
						<int>31210536</int>
						<int>31211625</int>
						<int>31212713</int>
						<int>31213588</int>
						<int>31218518</int>
						<int>31219420</int>
						<int>31220279</int>
						<int>31220302</int>
						<int>31222086</int>
						<int>31227080</int>
						<int>31229030</int>
						<int>31238236</int>
						<int>31254198</int>
						<int>31258008</int>
						<int>36436779</int>
						<int>36437850</int>
						<int>36464012</int>
						<int>41916569</int>
						<int>47249982</int>
						<int>47283134</int>
						<int>58577027</int>
						<int>58578040</int>
						<int>58578041</int>
						<int>58628920</int>
						<int>59107045</int>
						<int>59125697</int>
						<int>59142249</int>
						<int>60466586</int>
						<int>60478936</int>
						<int>66450441</int>
						<int>66467021</int>
						<int>66479051</int>
						<int>75202978</int>
						<int>77436021</int>
						<int>77449129</int>
						<int>85159569</int>
						<int>90199702</int>
						<int>90212090</int>
						<int>96911147</int>
						<int>97110308</int>
						<int>98528428</int>
						<int>98665206</int>
						<int>98837995</int>
						<int>98842922</int>
						<int>98842977</int>
						<int>98846632</int>
						<int>98866485</int>
						<int>98874250</int>
						<int>98879075</int>
						<int>98904649</int>
						<int>98918872</int>
						<int>98945691</int>
						<int>98959458</int>
						<int>98984707</int>
						<int>100220125</int>
						<int>100238731</int>
						<int>100662329</int>
						<int>100795834</int>
						<int>100862457</int>
						<int>103124811</int>
						<int>103348671</int>
						<int>104369981</int>
						<int>104372472</int>
						<int>104385324</int>
						<int>104465831</int>
						<int>104465834</int>
						<int>104467697</int>
						<int>104473368</int>
						<int>104482267</int>
						<int>104505005</int>
						<int>104523840</int>
						<int>104550085</int>
						<int>104558084</int>
						<int>104659441</int>
						<int>104659675</int>
						<int>104664678</int>
						<int>104668274</int>
						<int>104671092</int>
						<int>104673242</int>
						<int>104674239</int>
						<int>104679268</int>
						<int>104686047</int>
						<int>104698649</int>
						<int>104751469</int>
						<int>104752478</int>
						<int>104755145</int>
						<int>104761158</int>
						<int>104762266</int>
						<int>104786484</int>
						<int>104853747</int>
						<int>104873258</int>
						<int>104983051</int>
						<int>105063056</int>
						<int>105116588</int>
						<int>105178523</int>
						<int>105318602</int>
						<int>105362613</int>
						<int>105364552</int>
						<int>105368563</int>
						<int>105369591</int>
						<int>105370746</int>
						<int>105373503</int>
						<int>105373615</int>
						<int>105376634</int>
						<int>105377546</int>
						<int>105378752</int>
						<int>105379574</int>
						<int>105381626</int>
						<int>105382587</int>
						<int>105425313</int>
						<int>105495146</int>
						<int>105862607</int>
						<int>105939029</int>
						<int>105995585</int>
						<int>106017178</int>
						<int>106129726</int>
						<int>106768485</int>
						<int>107825194</int>
						<int>111906429</int>
						<int>115121473</int>
						<int>115578654</int>
						<int>116630363</int>
						<int>117835105</int>
						<int>117850671</int>
						<int>118638500</int>
						<int>118662027</int>
						<int>118872681</int>
						<int>118873829</int>
						<int>118879289</int>
						<int>118889092</int>
						<int>119501720</int>
						<int>119551648</int>
						<int>119569538</int>
						<int>119640702</int>
						<int>119667998</int>
						<int>119674103</int>
						<int>119697201</int>
						<int>119706266</int>
						<int>119744627</int>
						<int>119773746</int>
						<int>120072697</int>
						<int>120144309</int>
						<int>120214154</int>
						<int>120357027</int>
						<int>120392612</int>
						<int>120399120</int>
						<int>120553945</int>
						<int>120783545</int>
						<int>120797092</int>
						<int>120881676</int>
						<int>120889689</int>
						<int>120999554</int>
						<int>121168608</int>
						<int>121268830</int>
						<int>121341838</int>
						<int>121729951</int>
						<int>121803677</int>
						<int>122165810</int>
						<int>125408034</int>
						<int>127293130</int>
						<int>127566683</int>
						<int>127762067</int>
						<int>127861893</int>
						<int>128571722</int>
						<int>128647535</int>
						<int>128698922</int>
						<int>128701748</int>
						<int>128771507</int>
						<int>129037212</int>
						<int>129079800</int>
						<int>129175415</int>
						<int>129317272</int>
						<int>129319665</int>
						<int>129365668</int>
						<int>129378095</int>
						<int>129424803</int>
						<int>129590730</int>
						<int>129603714</int>
						<int>129625954</int>
						<int>129692391</int>
						<int>129714980</int>
						<int>129721097</int>
						<int>129886397</int>
						<int>129968371</int>
						<int>129972243</int>
						<int>130009862</int>
						<int>130033651</int>
						<int>130040030</int>
						<int>130040032</int>
						<int>130040033</int>
						<int>130091954</int>
						<int>130100640</int>
						<int>130131267</int>
						<int>130131921</int>
						<int>130144837</int>
						<int>130171030</int>
						<int>130172071</int>
						<int>130197218</int>
						<int>130212435</int>
						<int>130291076</int>
						<int>130402427</int>
						<int>130405166</int>
						<int>130676169</int>
						<int>130698471</int>
						<int>130713390</int>
						<int>130785217</int>
						<int>131396908</int>
						<int>131455115</int>
						<int>131682095</int>
						<int>131689473</int>
						<int>131701956</int>
						<int>132142800</int>
						<int>132525441</int>
						<int>132765492</int>
						<int>132801275</int>
						<int>133399034</int>
						<int>134522926</int>
						<int>134524022</int>
						<int>134528994</int>
						<int>134532942</int>
						<int>134536993</int>
						<int>134538001</int>
						<int>134547533</int>
						<int>134549216</int>
						<int>134549317</int>
						<int>134550159</int>
						<int>134550214</int>
						<int>134550232</int>
						<int>134551154</int>
						<int>134551207</int>
						<int>134551390</int>
						<int>134553171</int>
						<int>134553237</int>
						<int>134554199</int>
						<int>134554227</int>
						<int>134555229</int>
						<int>134555240</int>
						<int>134556118</int>
						<int>134557078</int>
						<int>134560099</int>
						<int>134560287</int>
						<int>134562084</int>
						<int>134562180</int>
						<int>134563287</int>
						<int>134565083</int>
						<int>134566130</int>
						<int>134568111</int>
						<int>134624737</int>
						<int>134666461</int>
						<int>134672998</int>
						<int>134684008</int>
						<int>134916523</int>
						<int>135100527</int>
						<int>135219410</int>
						<int>135222083</int>
						<int>135306997</int>
						<int>135463054</int>
						<int>135779456</int>
						<int>135812968</int>
						<int>136097030</int>
						<int>136131333</int>
						<int>136146907</int>
						<int>136157556</int>
						<int>136320962</int>
						<int>136450641</int>
						<int>136466000</int>
						<int>136745792</int>
						<int>136761546</int>
						<int>136840245</int>
						<int>138160034</int>
						<int>138181244</int>
						<int>138210071</int>
						<int>138210107</int>
						<int>138232200</int>
						<int>138237088</int>
						<int>138277547</int>
						<int>138287133</int>
						<int>138306991</int>
						<int>138324625</int>
						<int>138341916</int>
						<int>138372035</int>
						<int>138372036</int>
						<int>138375118</int>
						<int>138378071</int>
						<int>138380128</int>
						<int>138380194</int>
						<int>138534411</int>
						<int>138618294</int>
						<int>138931764</int>
						<int>139536037</int>
						<int>139536038</int>
						<int>139536039</int>
						<int>139536040</int>
						<int>140367832</int>
						<int>140406050</int>
						<int>140421668</int>
						<int>140422973</int>
						<int>140423713</int>
						<int>140436348</int>
						<int>140483470</int>
						<int>140615715</int>
						<int>140802803</int>
						<int>140896470</int>
						<int>141189437</int>
						<int>141192744</int>
						<int>141382548</int>
						<int>141461680</int>
						<int>141624996</int>
						<int>141627135</int>
						<int>141659139</int>
						<int>141872038</int>
						<int>141993721</int>
						<int>142006413</int>
						<int>142045136</int>
						<int>142095667</int>
						<int>142227273</int>
						<int>142250480</int>
						<int>142518788</int>
						<int>142544931</int>
						<int>142546314</int>
						<int>142555433</int>
						<int>142653044</int>
						<int>143191852</int>
						<int>143258496</int>
						<int>143299722</int>
						<int>143331253</int>
						<int>143432462</int>
						<int>143632431</int>
						<int>143695326</int>
						<int>144219522</int>
						<int>144590916</int>
						<int>145410436</int>
						<int>146720405</int>
						<int>150810438</int>
						<int>151258773</int>
						<int>151315554</int>
						<int>151400090</int>
						<int>151429441</int>
						<int>151439617</int>
						<int>151453617</int>
						<int>151466296</int>
						<int>151511132</int>
						<int>151636561</int>
						<int>151823192</int>
						<int>151827116</int>
						<int>151850642</int>
						<int>152016572</int>
						<int>153111675</int>
						<int>153114652</int>
						<int>153123147</int>
						<int>153267108</int>
						<int>153389799</int>
						<int>153395366</int>
						<int>153718608</int>
						<int>154171028</int>
						<int>154315227</int>
						<int>154559688</int>
						<int>154978771</int>
						<int>154979742</int>
						<int>154985773</int>
						<int>154989370</int>
						<int>155044852</int>
						<int>155065458</int>
						<int>155578573</int>
						<int>156403304</int>
						<int>159085959</int>
						<int>159776047</int>
						<int>159816630</int>
						<int>160733048</int>
						<int>160733049</int>
						<int>160733050</int>
						<int>160733051</int>
						<int>160733056</int>
						<int>164824922</int>
						<int>164824924</int>
						<int>164824926</int>
						<int>164824930</int>
						<int>164831646</int>
						<int>164831647</int>
						<int>164831648</int>
						<int>164831650</int>
						<int>164835050</int>
						<int>164835051</int>
						<int>164835052</int>
						<int>164835056</int>
						<int>164835057</int>
						<int>164835059</int>
						<int>164836898</int>
						<int>164836899</int>
						<int>164836900</int>
						<int>164845333</int>
						<int>164845334</int>
						<int>164845336</int>
						<int>164845337</int>
						<int>164845341</int>
						<int>164845342</int>
						<int>164845345</int>
						<int>164845346</int>
						<int>164845349</int>
						<int>164845350</int>
						<int>164845353</int>
						<int>164845355</int>
						<int>164845358</int>
						<int>164845361</int>
						<int>164845364</int>
						<int>164847387</int>
						<int>164847388</int>
						<int>164847389</int>
						<int>164847390</int>
						<int>164848328</int>
						<int>164848329</int>
						<int>164848330</int>
						<int>164849448</int>
						<int>164849449</int>
						<int>164849451</int>
						<int>164849452</int>
						<int>164849454</int>
						<int>164849455</int>
						<int>164849457</int>
						<int>164849461</int>
						<int>164850219</int>
						<int>164850220</int>
						<int>164850222</int>
						<int>164850223</int>
						<int>164850224</int>
						<int>164850226</int>
						<int>164850227</int>
						<int>164850228</int>
						<int>164850229</int>
						<int>164850231</int>
						<int>164850236</int>
						<int>164850237</int>
						<int>164850240</int>
						<int>164850242</int>
						<int>164850243</int>
						<int>164852242</int>
						<int>164852243</int>
						<int>164852244</int>
						<int>164852247</int>
						<int>164852248</int>
						<int>164852249</int>
						<int>164852250</int>
						<int>164852251</int>
						<int>164852254</int>
						<int>164852256</int>
						<int>164852257</int>
						<int>164852258</int>
						<int>164852259</int>
						<int>164852260</int>
						<int>164852261</int>
						<int>164852262</int>
						<int>164853061</int>
						<int>164853063</int>
						<int>164853071</int>
						<int>164853072</int>
						<int>164853075</int>
						<int>168118980</int>
						<int>168118981</int>
						<int>168118983</int>
						<int>168118984</int>
						<int>168180375</int>
						<int>168180376</int>
						<int>168180378</int>
						<int>168180379</int>
						<int>168270830</int>
						<int>168270831</int>
						<int>168270833</int>
						<int>168270834</int>
						<int>168270835</int>
					</InstalledNonLeafUpdateIDs>
					<OtherCachedUpdateIDs>
					</OtherCachedUpdateIDs>
					<SkipSoftwareSync>false</SkipSoftwareSync>
					<NeedTwoGroupOutOfScopeUpdates>true</NeedTwoGroupOutOfScopeUpdates>
					<FilterAppCategoryIds>
						<CategoryIdentifier>
							<Id>{2}</Id>
						</CategoryIdentifier>
					</FilterAppCategoryIds>
					<TreatAppCategoryIdsAsInstalled>true</TreatAppCategoryIdsAsInstalled>
					<AlsoPerformRegularSync>false</AlsoPerformRegularSync>
					<ComputerSpec/>
					<ExtendedUpdateInfoParameters>
						<XmlUpdateFragmentTypes>
							<XmlUpdateFragmentType>Extended</XmlUpdateFragmentType>
						</XmlUpdateFragmentTypes>
					</ExtendedUpdateInfoParameters>
					<ProductsParameters>
						<SyncCurrentVersionOnly>false</SyncCurrentVersionOnly>
						<DeviceAttributes>FlightRing={3};DeviceFamily=Windows.Desktop;</DeviceAttributes>
						<CallerAttributes>Interactive=1;IsSeeker=1;</CallerAttributes>
						<Products/>
					</ProductsParameters>
				</parameters>
			</SyncUpdates>
		</s:Body>
	</s:Envelope>
	".Replace("{1}", tk).Replace("{2}", cid).Replace("{3}", ring);

	public static string Url(string u, string r, string ring) => @"
	<s:Envelope
		xmlns:a=""http://www.w3.org/2005/08/addressing""
		xmlns:s=""http://www.w3.org/2003/05/soap-envelope"">
		<s:Header>
			<a:Action s:mustUnderstand=""1"">http://www.microsoft.com/SoftwareDistribution/Server/ClientWebService/GetExtendedUpdateInfo2</a:Action>
			<a:MessageID>urn:uuid:2cc99c2e-3b3e-4fb1-9e31-0cd30e6f43a0</a:MessageID>
			<a:To s:mustUnderstand=""1"">https://fe3.delivery.mp.microsoft.com/ClientWebService/client.asmx/secured</a:To>
			<o:Security s:mustUnderstand=""1""
				xmlns:o=""http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd"">
				<Timestamp
					xmlns=""http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd"">
					<Created>2017-08-01T00:29:01.868Z</Created>
					<Expires>2017-08-01T00:34:01.868Z</Expires>
				</Timestamp>
				<wuws:WindowsUpdateTicketsToken wsu:id=""ClientMSA""
					xmlns:wsu=""http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd""
					xmlns:wuws=""http://schemas.microsoft.com/msus/2014/10/WindowsUpdateAuthorization"">
					<TicketType Name=""MSA"" Version=""1.0"" Policy=""MBI_SSL"">
						<Device>dAA9AEUAdwBBAHcAQQBzAE4AMwBCAEEAQQBVADEAYgB5AHMAZQBtAGIAZQBEAFYAQwArADMAZgBtADcAbwBXAHkASAA3AGIAbgBnAEcAWQBtAEEAQQBMAGoAbQBqAFYAVQB2AFEAYwA0AEsAVwBFAC8AYwBDAEwANQBYAGUANABnAHYAWABkAGkAegBHAGwAZABjADEAZAAvAFcAeQAvAHgASgBQAG4AVwBRAGUAYwBtAHYAbwBjAGkAZwA5AGoAZABwAE4AawBIAG0AYQBzAHAAVABKAEwARAArAFAAYwBBAFgAbQAvAFQAcAA3AEgAagBzAEYANAA0AEgAdABsAC8AMQBtAHUAcgAwAFMAdQBtAG8AMABZAGEAdgBqAFIANwArADQAcABoAC8AcwA4ADEANgBFAFkANQBNAFIAbQBnAFIAQwA2ADMAQwBSAEoAQQBVAHYAZgBzADQAaQB2AHgAYwB5AEwAbAA2AHoAOABlAHgAMABrAFgAOQBPAHcAYQB0ADEAdQBwAFMAOAAxAEgANgA4AEEASABzAEoAegBnAFQAQQBMAG8AbgBBADIAWQBBAEEAQQBpAGcANQBJADMAUQAvAFYASABLAHcANABBAEIAcQA5AFMAcQBhADEAQgA4AGsAVQAxAGEAbwBLAEEAdQA0AHYAbABWAG4AdwBWADMAUQB6AHMATgBtAEQAaQBqAGgANQBkAEcAcgBpADgAQQBlAEUARQBWAEcAbQBXAGgASQBCAE0AUAAyAEQAVwA0ADMAZABWAGkARABUAHoAVQB0AHQARQBMAEgAaABSAGYAcgBhAGIAWgBsAHQAQQBUAEUATABmAHMARQBGAFUAYQBRAFMASgB4ADUAeQBRADgAagBaAEUAZQAyAHgANABCADMAMQB2AEIAMgBqAC8AUgBLAGEAWQAvAHEAeQB0AHoANwBUAHYAdAB3AHQAagBzADYAUQBYAEIAZQA4AHMAZwBJAG8AOQBiADUAQQBCADcAOAAxAHMANgAvAGQAUwBFAHgATgBEAEQAYQBRAHoAQQBYAFAAWABCAFkAdQBYAFEARQBzAE8AegA4AHQAcgBpAGUATQBiAEIAZQBUAFkAOQBiAG8AQgBOAE8AaQBVADcATgBSAEYAOQAzAG8AVgArAFYAQQBiAGgAcAAwAHAAUgBQAFMAZQBmAEcARwBPAHEAdwBTAGcANwA3AHMAaAA5AEoASABNAHAARABNAFMAbgBrAHEAcgAyAGYARgBpAEMAUABrAHcAVgBvAHgANgBuAG4AeABGAEQAbwBXAC8AYQAxAHQAYQBaAHcAegB5AGwATABMADEAMgB3AHUAYgBtADUAdQBtAHAAcQB5AFcAYwBLAFIAagB5AGgAMgBKAFQARgBKAFcANQBnAFgARQBJADUAcAA4ADAARwB1ADIAbgB4AEwAUgBOAHcAaQB3AHIANwBXAE0AUgBBAFYASwBGAFcATQBlAFIAegBsADkAVQBxAGcALwBwAFgALwB2AGUATAB3AFMAawAyAFMAUwBIAGYAYQBLADYAagBhAG8AWQB1AG4AUgBHAHIAOABtAGIARQBvAEgAbABGADYASgBDAGEAYQBUAEIAWABCAGMAdgB1AGUAQwBKAG8AOQA4AGgAUgBBAHIARwB3ADQAKwBQAEgAZQBUAGIATgBTAEUAWABYAHoAdgBaADYAdQBXADUARQBBAGYAZABaAG0AUwA4ADgAVgBKAGMAWgBhAEYASwA3AHgAeABnADAAdwBvAG4ANwBoADAAeABDADYAWgBCADAAYwBZAGoATAByAC8ARwBlAE8AegA5AEcANABRAFUASAA5AEUAawB5ADAAZAB5AEYALwByAGUAVQAxAEkAeQBpAGEAcABwAGgATwBQADgAUwAyAHQANABCAHIAUABaAFgAVAB2AEMAMABQADcAegBPACsAZgBHAGsAeABWAG0AKwBVAGYAWgBiAFEANQA1AHMAdwBFAD0AJgBwAD0A</Device>
					</TicketType>
				</wuws:WindowsUpdateTicketsToken>
			</o:Security>
		</s:Header>
		<s:Body>
			<GetExtendedUpdateInfo2
				xmlns=""http://www.microsoft.com/SoftwareDistribution/Server/ClientWebService"">
				<updateIDs>
					<UpdateIdentity>
						<UpdateID>{1}</UpdateID>
						<RevisionNumber>{2}</RevisionNumber>
					</UpdateIdentity>
				</updateIDs>
				<infoTypes>
					<XmlUpdateFragmentType>FileUrl</XmlUpdateFragmentType>
					<XmlUpdateFragmentType>FileDecryption</XmlUpdateFragmentType>
				</infoTypes>
				<deviceAttributes>FlightRing={3};DeviceFamily=Windows.Desktop;</deviceAttributes>
			</GetExtendedUpdateInfo2>
		</s:Body>
	</s:Envelope>
	".Replace("{1}", u).Replace("{2}", r).Replace("{3}", ring);
}
