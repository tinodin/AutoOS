using Windows.Win32;
using Windows.Win32.System.SystemInformation;

namespace AutoOS.Core.Helpers.Bios;

public static class AmiPlatformHelper
{
	private const uint FIDT = 0x54444946;
	private const uint IPCA = 0x41435049;
	private const uint BMSR = 0x52534D42;

	public static unsafe bool IsSupported()
	{
		byte[]? fidt = TryGetTable(FIDT);
		if (fidt != null && fidt.Length >= 0x47)
		{
			if (fidt[0x44] == 0x30 && fidt[0x45] == 0x35 && fidt[0x46] == 0x00)
				return true;
		}

		byte[]? bmsr = TryGetTable(BMSR);
		if (bmsr != null && Contains(bmsr, "American Megatrends"u8))
			return true;
		if (bmsr != null && Contains(bmsr, "AMI"u8))
			return true;

		byte[]? facp = TryGetTable(IPCA);
		if (facp != null && Contains(facp, "ALASKA"u8))
			return true;
		if (facp != null && Contains(facp, "AMI"u8))
			return true;

		return false;
	}

	private static unsafe byte[]? TryGetTable(uint provider)
	{
		uint size = PInvoke.GetSystemFirmwareTable((FIRMWARE_TABLE_PROVIDER)provider, 0, null, 0);
		if (size == 0 || size > 0x100000)
			return null;
		byte[] buffer = new byte[size];
		fixed (byte* p = buffer)
		{
			uint ret = PInvoke.GetSystemFirmwareTable((FIRMWARE_TABLE_PROVIDER)provider, 0, p, size);
			if (ret == 0)
				return null;
			if (ret != size)
				Array.Resize(ref buffer, (int)ret);
			return buffer;
		}
	}

	private static bool Contains(byte[] haystack, ReadOnlySpan<byte> needle)
	{
		if (needle.Length == 0 || haystack.Length < needle.Length)
			return false;
		for (int i = 0; i <= haystack.Length - needle.Length; i++)
		{
			bool match = true;
			for (int j = 0; j < needle.Length; j++)
			{
				if (haystack[i + j] != needle[j])
				{
					match = false;
					break;
				}
			}
			if (match)
				return true;
		}
		return false;
	}
}
