using AutoOS.Core.Data.Enums.Bios;
using AutoOS.Core.Data.Models.Bios;
using Windows.Win32;
using Windows.Win32.System.SystemInformation;

namespace AutoOS.Core.Helpers.Bios;

public static partial class SmbiosHelper
{
	private const uint RSMB = 0x52534D42;
	private const int BIOS_VENDOR_OFFSET = 4;
	private const int BIOS_VERSION_OFFSET = 5;
	private const int BIOS_RELEASE_DATE_OFFSET = 8;
	private const int MANUFACTURER_OFFSET = 4;
	private const int PRODUCT_OFFSET = 5;

	public static unsafe SmbiosInfo GetInfo()
	{
		var info = new SmbiosInfo();
		var provider = (FIRMWARE_TABLE_PROVIDER)RSMB;

		uint bufferSize = PInvoke.GetSystemFirmwareTable(provider, 0, null, 0);
		if (bufferSize == 0)
			return info;

		byte[] buffer = new byte[bufferSize];
		fixed (byte* pBuffer = buffer)
		{
			PInvoke.GetSystemFirmwareTable(provider, 0, pBuffer, bufferSize);
		}

		int offset = 8;
		while (offset + 4 < buffer.Length)
		{
			byte type = buffer[offset];
			byte length = buffer[offset + 1];
			if (length < 4 || offset + length >= buffer.Length)
				break;

			int stringsStart = offset + length;
			int stringsEnd = stringsStart;
			while (stringsEnd + 1 < buffer.Length && (buffer[stringsEnd] != 0 || buffer[stringsEnd + 1] != 0))
				stringsEnd++;
			stringsEnd += 2;

			switch ((SmbiosType)type)
			{
				case SmbiosType.Bios:
					if (length >= BIOS_RELEASE_DATE_OFFSET + 1)
					{
						info.BiosVendor = GetString(buffer, stringsStart, stringsEnd, buffer[offset + BIOS_VENDOR_OFFSET]);
						info.BiosVersion = GetString(buffer, stringsStart, stringsEnd, buffer[offset + BIOS_VERSION_OFFSET]);
						info.BiosReleaseDate = GetString(buffer, stringsStart, stringsEnd, buffer[offset + BIOS_RELEASE_DATE_OFFSET]);
					}
					break;

				case SmbiosType.System:
					if (length >= PRODUCT_OFFSET + 1)
					{
						info.SystemManufacturer = GetString(buffer, stringsStart, stringsEnd, buffer[offset + MANUFACTURER_OFFSET]);
						info.SystemProduct = GetString(buffer, stringsStart, stringsEnd, buffer[offset + PRODUCT_OFFSET]);
					}
					break;

				case SmbiosType.Baseboard:
					if (length >= PRODUCT_OFFSET + 1)
					{
						info.BaseboardManufacturer = GetString(buffer, stringsStart, stringsEnd, buffer[offset + MANUFACTURER_OFFSET]);
						info.BaseboardProduct = GetString(buffer, stringsStart, stringsEnd, buffer[offset + PRODUCT_OFFSET]);
					}
					break;
			}

			offset = stringsEnd;
		}

		return info;
	}

	private static string GetString(byte[] buffer, int stringsStart, int stringsEnd, byte index)
	{
		if (index == 0 || stringsStart >= stringsEnd)
			return string.Empty;

		int position = stringsStart;
		for (int i = 1; i < index && position < stringsEnd; i++)
		{
			while (position < stringsEnd && buffer[position] != 0)
				position++;
			position++;
		}

		if (position >= stringsEnd)
			return string.Empty;

		int end = position;
		while (end < stringsEnd && buffer[end] != 0)
			end++;

		return System.Text.Encoding.ASCII.GetString(buffer, position, end - position);
	}
}
