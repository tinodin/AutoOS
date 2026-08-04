using System.Text;

namespace AutoOS.Core;

public static partial class Secrets
{
	private static byte[] Key;

	static partial void Initialize();

	static Secrets()
	{
		Initialize();
	}

	public static string Bios => Decrypt(GetEncryptedBios());
	public static string Log => Decrypt(GetEncryptedLog());
	public static string Error => Decrypt(GetEncryptedError());
	public static string Network => Decrypt(GetEncryptedNetwork());
	public static string Syncfusion => Decrypt(GetEncryptedSyncfusion());

	private static partial byte[] GetEncryptedBios();
	private static partial byte[] GetEncryptedLog();
	private static partial byte[] GetEncryptedError();
	private static partial byte[] GetEncryptedNetwork();
	private static partial byte[] GetEncryptedSyncfusion();

	private static string Decrypt(byte[] data)
	{
		if (data.Length == 0 || Key.Length == 0)
			return string.Empty;

		byte[] plain = new byte[data.Length];
		for (int i = 0; i < data.Length; i++)
			plain[i] = (byte)(data[i] ^ Key[i % Key.Length]);
		return Encoding.UTF8.GetString(plain);
	}
}
