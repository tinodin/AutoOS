namespace AutoOS.Core;

public static partial class Secrets
{
	static partial void Initialize()
	{
		Key = [];
	}

	private static partial byte[] GetEncryptedLog() => [];
	private static partial byte[] GetEncryptedError() => [];
	private static partial byte[] GetEncryptedSyncfusion() => [];
}
