using PhantomClientCore.Native;
using System.Runtime.InteropServices;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace AutoOS.Core.Helpers.Games.Clients;

internal sealed class TlsClient
{
	private static readonly JsonSerializerOptions JsonWriteOptions = new()
	{
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
	};

	private static readonly Lock LoadGate = new();
	private static bool _loaded;
	private static nint _handle;
	private static RequestFn _request;
	private static FreeMemoryFn _freeMemory;
	private static DestroySessionFn _destroySession;

	private readonly string _sessionId = Guid.NewGuid().ToString();
	private readonly TlsClientOptions _options;

	public TlsClient(TlsClientOptions options = null)
	{
		_options = options ?? new TlsClientOptions();
		EnsureLoaded();
	}

	public Task<TlsResponse> GetAsync(string url, IReadOnlyDictionary<string, string> headers = null)
		=> Task.Run(() => Execute("GET", url, null, headers));

	public Task<TlsResponse> PostAsync(string url, string body, IReadOnlyDictionary<string, string> headers = null)
		=> Task.Run(() => Execute("POST", url, body, headers));

	private TlsResponse Execute(string method, string url, string body, IReadOnlyDictionary<string, string> headers)
	{
		var mergedHeaders = MergeHeaders(_options.DefaultHeaders, headers);
		var payload = new JsonObject
		{
			["sessionId"] = _sessionId,
			["tlsClientIdentifier"] = _options.ClientIdentifier,
			["followRedirects"] = true,
			["forceHttp1"] = _options.ForceHttp1,
			["withDebug"] = false,
			["catchPanics"] = true,
			["withRandomTLSExtensionOrder"] = _options.RandomTlsExtensionOrder,
			["timeoutMilliseconds"] = _options.Timeout,
			["insecureSkipVerify"] = _options.InsecureSkipVerify,
			["isByteResponse"] = false,
			["isByteRequest"] = false,
			["withDefaultCookieJar"] = true,
			["withoutCookieJar"] = false,
			["headers"] = ToJsonObject(mergedHeaders),
			["proxyUrl"] = _options.Proxy ?? "",
			["isRotatingProxy"] = false,
			["requestUrl"] = url,
			["requestMethod"] = method,
			["requestBody"] = body,
			["requestCookies"] = new JsonArray()
		};

		if (_options.HeaderOrder is { Count: > 0 })
			payload["headerOrder"] = ToJsonArray(_options.HeaderOrder);

		var responseJson = Request(payload.ToJsonString(JsonWriteOptions));
		return Parse(url, responseJson);
	}

	private static TlsResponse Parse(string url, string responseJson)
	{
		using var document = JsonDocument.Parse(responseJson);
		var root = document.RootElement;
		var status = root.TryGetProperty("status", out var statusElement) ? statusElement.GetInt32() : 0;
		var body = root.TryGetProperty("body", out var bodyElement) ? bodyElement.GetString() ?? "" : "";

		if (status == 0)
			throw new InvalidOperationException("Native request failed: " + body);

		return new TlsResponse
		{
			Status = status,
			Body = body,
			Url = root.TryGetProperty("target", out var targetElement) ? targetElement.GetString() ?? url : url
		};
	}

	private static JsonObject ToJsonObject(IReadOnlyDictionary<string, string> values)
	{
		var obj = new JsonObject();
		if (values == null)
			return obj;

		foreach (var (key, value) in values)
			obj[key] = value;

		return obj;
	}

	private static JsonArray ToJsonArray(IEnumerable<string> values)
	{
		var array = new JsonArray();
		foreach (var value in values)
			array.Add(value);

		return array;
	}

	private static Dictionary<string, string> MergeHeaders(
		IReadOnlyDictionary<string, string> baseHeaders,
		IReadOnlyDictionary<string, string> overrideHeaders)
	{
		var headers = new Dictionary<string, string>(StringComparer.Ordinal);
		if (baseHeaders != null)
		{
			foreach (var (key, value) in baseHeaders)
				headers[key.ToLowerInvariant()] = value;
		}

		if (overrideHeaders != null)
		{
			foreach (var (key, value) in overrideHeaders)
				headers[key.ToLowerInvariant()] = value;
		}

		return headers;
	}

	private static void EnsureLoaded()
	{
		if (_loaded)
			return;

		lock (LoadGate)
		{
			if (_loaded)
				return;

			_handle = NativeLibrary.Load(NativeLibraryResolver.Resolve(null));
			_request = Marshal.GetDelegateForFunctionPointer<RequestFn>(NativeLibrary.GetExport(_handle, "request"));
			_freeMemory = Marshal.GetDelegateForFunctionPointer<FreeMemoryFn>(NativeLibrary.GetExport(_handle, "freeMemory"));
			_destroySession = Marshal.GetDelegateForFunctionPointer<DestroySessionFn>(NativeLibrary.GetExport(_handle, "destroySession"));
			_loaded = true;
		}
	}

	private static string Request(string payloadJson)
	{
		EnsureLoaded();
		var ptr = _request(payloadJson);
		if (ptr == IntPtr.Zero)
			throw new InvalidOperationException("Native request returned null.");

		var response = Marshal.PtrToStringUTF8(ptr) ?? "";
		FreeByResponseId(response);
		return response;
	}

	private static void FreeByResponseId(string json)
	{
		try
		{
			using var document = JsonDocument.Parse(json);
			if (document.RootElement.TryGetProperty("id", out var idElement))
			{
				var id = idElement.GetString();
				if (id != null)
					_freeMemory(id);
			}
		}
		catch
		{
		}
	}

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate nint RequestFn([MarshalAs(UnmanagedType.LPUTF8Str)] string payload);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void FreeMemoryFn([MarshalAs(UnmanagedType.LPUTF8Str)] string id);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate nint DestroySessionFn([MarshalAs(UnmanagedType.LPUTF8Str)] string payload);
}

internal sealed class TlsClientOptions
{
	public string ClientIdentifier { get; set; } = "chrome_120";
	public int Timeout { get; set; } = 30000;
	public string Proxy { get; set; }
	public Dictionary<string, string> DefaultHeaders { get; set; } = new()
	{
		{ "User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36" },
		{ "Accept", "application/json" },
		{ "Accept-Language", "en-US,en;q=0.9" }
	};
	public List<string> HeaderOrder { get; set; }
	public bool RandomTlsExtensionOrder { get; set; } = true;
	public bool InsecureSkipVerify { get; set; }
	public bool ForceHttp1 { get; set; }
}

internal sealed class TlsResponse
{
	public int Status { get; init; }
	public string Body { get; init; }
	public string Url { get; init; }
	public bool IsSuccess => Status is >= 200 and <= 299;
}
