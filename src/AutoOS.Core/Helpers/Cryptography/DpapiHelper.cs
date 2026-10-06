using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Security.Cryptography;

namespace AutoOS.Core.Helpers.Cryptography;

public static class DpapiHelper
{
	private const uint CryptProtectUiForbidden = 0x1;
	private const uint CryptProtectLocalMachine = 0x4;

	public enum Scope { CurrentUser, LocalMachine }

	public static unsafe byte[]? Protect(byte[] data, byte[]? entropy, Scope scope)
	{
		return Exchange(data, entropy, scope, true);
	}

	public static unsafe byte[]? Unprotect(byte[] data, byte[]? entropy, Scope scope)
	{
		return Exchange(data, entropy, scope, false);
	}

	private static unsafe byte[]? Exchange(byte[] data, byte[]? entropy, Scope scope, bool protect)
	{
		CRYPT_INTEGER_BLOB output;
		uint flags = CryptProtectUiForbidden | (scope == Scope.LocalMachine ? CryptProtectLocalMachine : 0);

		fixed (byte* dataPointer = data)
		fixed (byte* entropyPointer = entropy)
		{
			CRYPT_INTEGER_BLOB input = new() { cbData = (uint)data.Length, pbData = dataPointer };
			CRYPT_INTEGER_BLOB? optionalEntropy = null;

			if (entropy != null)
			{
				optionalEntropy = new CRYPT_INTEGER_BLOB { cbData = (uint)entropy.Length, pbData = entropyPointer };
			}

			BOOL result = protect
				? PInvoke.CryptProtectData(input, null, optionalEntropy, null, flags, out output)
				: PInvoke.CryptUnprotectData(input, optionalEntropy, null, flags, out output);

			if (!result)
			{
				return null;
			}
		}

		if (output.pbData == null)
		{
			return null;
		}

		byte[] bytes = new byte[output.cbData];
		Marshal.Copy((IntPtr)output.pbData, bytes, 0, (int)output.cbData);

		_ = PInvoke.LocalFree((HLOCAL)(IntPtr)output.pbData);

		return bytes;
	}
}
