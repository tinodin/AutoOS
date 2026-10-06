using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Windows.Win32;

namespace AutoOS.Core.Helpers.Cryptography;

public static class DpopHelper
{
	private const string SoftwareKeyStorageProviderName = "Microsoft Software Key Storage Provider";

	private const string CngKeysFolderName = "Keys";

	private const string EcCpuPublicBlobMagic = "ECS1";

	private const int CoordinateSize = 32;

	private const int EcCpuPublicBlobHeaderSize = 8;

	private const int EcCpuPublicBlobSize = EcCpuPublicBlobHeaderSize + (CoordinateSize * 2);

	private const int SignatureSize = 64;

	private const int MaximumProofAttempts = 3;

	private const int MaximumNonceLength = 512;

	private const string DpopHeaderName = "DPoP";

	private const string NonceHeaderName = "DPoP-Nonce";

	private static readonly ConcurrentDictionary<string, string> _publicJwks = [];

	public static async Task<HttpResponseMessage> SendFormPostAsync(HttpClient client, string url, string keyName, List<KeyValuePair<string, string>> form)
	{
		string? nonce = null;

		for (int attempt = 0; attempt < MaximumProofAttempts; attempt++)
		{
			using var request = new HttpRequestMessage(HttpMethod.Post, url)
			{
				Content = new FormUrlEncodedContent(form)
			};

			if (!TryAddProofHeader(request, keyName, nonce))
			{
				break;
			}

			HttpResponseMessage response = await client.SendAsync(request);

			if (!TryGetNonce(response, ref nonce) || attempt == MaximumProofAttempts - 1)
			{
				return response;
			}

			response.Dispose();
		}

		return await client.PostAsync(url, new FormUrlEncodedContent(form));
	}

	private static bool TryAddProofHeader(HttpRequestMessage request, string keyName, string? nonce)
	{
		if (!TryGetPublicJwk(keyName, out string? jwk))
		{
			return false;
		}

		string header = Base64UrlEncode(JsonSerializer.SerializeToUtf8Bytes(new JsonObject
		{
			["typ"] = "dpop+jwt",
			["alg"] = "ES256",
			["jwk"] = JsonNode.Parse(jwk)
		}));

		var claims = new JsonObject
		{
			["jti"] = Guid.NewGuid().ToString(),
			["htm"] = request.Method.Method,
			["htu"] = NormalizeHttpUri(request.RequestUri!),
			["iat"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
		};

		if (!string.IsNullOrEmpty(nonce))
		{
			claims["nonce"] = nonce;
		}

		string payload = Base64UrlEncode(JsonSerializer.SerializeToUtf8Bytes(claims));
		string signingInput = $"{header}.{payload}";

		if (!TrySignHash(keyName, SHA256.HashData(Encoding.UTF8.GetBytes(signingInput)), out byte[]? signature))
		{
			return false;
		}

		return request.Headers.TryAddWithoutValidation(DpopHeaderName, $"{signingInput}.{Base64UrlEncode(signature)}");
	}

	private static bool TryGetPublicJwk(string keyName, [NotNullWhen(true)] out string? jwk)
	{
		if (_publicJwks.TryGetValue(keyName, out jwk))
		{
			return true;
		}

		jwk = null;

		byte[]? container = FindKeyContainer(keyName);

		if (container == null)
		{
			return false;
		}

		for (int offset = 0; offset <= container.Length - EcCpuPublicBlobSize; offset++)
		{
			if (Encoding.ASCII.GetString(container, offset, EcCpuPublicBlobMagic.Length) != EcCpuPublicBlobMagic)
			{
				continue;
			}

			if (BitConverter.ToInt32(container, offset + 4) != CoordinateSize)
			{
				continue;
			}

			Span<byte> x = stackalloc byte[CoordinateSize];
			Span<byte> y = stackalloc byte[CoordinateSize];

			container.AsSpan(offset + EcCpuPublicBlobHeaderSize, CoordinateSize).CopyTo(x);
			container.AsSpan(offset + EcCpuPublicBlobHeaderSize + CoordinateSize, CoordinateSize).CopyTo(y);

			jwk = new JsonObject
			{
				["kty"] = "EC",
				["crv"] = "P-256",
				["x"] = Base64UrlEncode(x),
				["y"] = Base64UrlEncode(y)
			}.ToJsonString();

			break;
		}

		if (jwk != null)
		{
			_publicJwks[keyName] = jwk;
		}

		return jwk != null;
	}

	private static unsafe bool TrySignHash(string keyName, byte[] hash, out byte[]? signature)
	{
		signature = null;

		if (PInvoke.NCryptOpenStorageProvider(out NCryptFreeObjectSafeHandle provider, SoftwareKeyStorageProviderName, 0).Failed)
		{
			return false;
		}

		using (provider)
		{
			if (PInvoke.NCryptOpenKey(provider, out NCryptFreeObjectSafeHandle key, keyName, 0, 0).Failed)
			{
				return false;
			}

			using (key)
			{
				Span<byte> buffer = stackalloc byte[SignatureSize];

				if (PInvoke.NCryptSignHash(key, null, hash, buffer, out uint signatureLength, 0).Failed)
				{
					return false;
				}

				signature = buffer[..(int)signatureLength].ToArray();

				return true;
			}
		}
	}

	private static byte[]? FindKeyContainer(string keyName)
	{
		string? directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft", "Crypto", CngKeysFolderName);

		if (!Directory.Exists(directory))
		{
			return null;
		}

		byte[] encodedName = Encoding.Unicode.GetBytes(keyName);

		foreach (string file in Directory.EnumerateFiles(directory))
		{
			byte[] bytes;

			try
			{
				bytes = File.ReadAllBytes(file);
			}
			catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
			{
				continue;
			}

			if (bytes.AsSpan().IndexOf(encodedName) >= 0)
			{
				return bytes;
			}
		}

		return null;
	}

	private static bool TryGetNonce(HttpResponseMessage response, ref string? nonce)
	{
		if (!response.Headers.TryGetValues(NonceHeaderName, out IEnumerable<string>? values))
		{
			return false;
		}

		string? candidate = values.FirstOrDefault();

		if (string.IsNullOrEmpty(candidate) || candidate.Length > MaximumNonceLength)
		{
			return false;
		}

		nonce = candidate;

		return true;
	}

	private static string NormalizeHttpUri(Uri uri)
	{
		var builder = new UriBuilder(uri) { Query = "", Fragment = "" };

		return builder.Uri.GetLeftPart(UriPartial.Path);
	}

	private static string Base64UrlEncode(ReadOnlySpan<byte> bytes) =>
		Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
