using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
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

	private static readonly ConcurrentDictionary<string, (string X, string Y)> _publicCoordinates = [];

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
		if (!TryGetPublicCoordinates(keyName, out (string X, string Y) coordinates))
		{
			return false;
		}

		string header = Base64UrlEncode(WriteJson(writer =>
		{
			writer.WriteStartObject();
			writer.WriteString("typ", "dpop+jwt");
			writer.WriteString("alg", "ES256");
			writer.WritePropertyName("jwk");
			writer.WriteStartObject();
			writer.WriteString("kty", "EC");
			writer.WriteString("crv", "P-256");
			writer.WriteString("x", coordinates.X);
			writer.WriteString("y", coordinates.Y);
			writer.WriteEndObject();
			writer.WriteEndObject();
		}));

		string payload = Base64UrlEncode(WriteJson(writer =>
		{
			writer.WriteStartObject();
			writer.WriteString("jti", Guid.NewGuid().ToString());
			writer.WriteString("htm", request.Method.Method);
			writer.WriteString("htu", NormalizeHttpUri(request.RequestUri!));
			writer.WriteNumber("iat", DateTimeOffset.UtcNow.ToUnixTimeSeconds());

			if (!string.IsNullOrEmpty(nonce))
			{
				writer.WriteString("nonce", nonce);
			}

			writer.WriteEndObject();
		}));

		string signingInput = $"{header}.{payload}";

		if (!TrySignHash(keyName, SHA256.HashData(Encoding.UTF8.GetBytes(signingInput)), out byte[]? signature))
		{
			return false;
		}

		return request.Headers.TryAddWithoutValidation(DpopHeaderName, $"{signingInput}.{Base64UrlEncode(signature)}");
	}

	private static bool TryGetPublicCoordinates(string keyName, out (string X, string Y) coordinates)
	{
		if (_publicCoordinates.TryGetValue(keyName, out coordinates))
		{
			return true;
		}

		coordinates = default;

		byte[]? container = FindKeyContainer(keyName);

		if (container == null)
		{
			return false;
		}

		(string X, string Y)? found = null;

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

			found = (Base64UrlEncode(x), Base64UrlEncode(y));

			break;
		}

		if (found == null)
		{
			return false;
		}

		coordinates = found.Value;
		_publicCoordinates[keyName] = coordinates;

		return true;
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

	private static byte[] WriteJson(Action<Utf8JsonWriter> write)
	{
		using var stream = new MemoryStream();

		using (Utf8JsonWriter writer = new(stream))
		{
			write(writer);
		}

		return stream.ToArray();
	}

	private static string Base64UrlEncode(ReadOnlySpan<byte> bytes) =>
		Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
