using System.Runtime.InteropServices;
using AutoOS.Core.Data.Models.RAM;
using Windows.Win32;
using Windows.Win32.System.SystemInformation;

namespace AutoOS.Core.Helpers.RAM;

public static partial class RamHelper
{
	private const uint RSMB = 0x52534D42;

	public static unsafe RamInfo GetRam()
	{
		var info = new RamInfo();

		MEMORYSTATUSEX memStatus = new MEMORYSTATUSEX();
		memStatus.dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>();

		if (PInvoke.GlobalMemoryStatusEx(ref memStatus))
		{
			info.CapacityGB = Math.Round(memStatus.ullTotalPhys / 1024.0 / 1024.0 / 1024.0, 1);
		}

		var provider = (FIRMWARE_TABLE_PROVIDER)RSMB;

		uint bufferSize = PInvoke.GetSystemFirmwareTable(provider, 0, null, 0);
		if (bufferSize == 0) return info;

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

			if (type == 17 && offset + length <= buffer.Length)
			{
				int speed = 0;
				if (length >= 0x22) speed = BitConverter.ToUInt16(buffer, offset + 0x20);
				if (speed == 0 && length >= 0x17) speed = BitConverter.ToUInt16(buffer, offset + 0x15);

				if (length >= 0x58)
				{
					int extSpeed = BitConverter.ToInt32(buffer, offset + 0x54);
					if (extSpeed > 0) speed = extSpeed;
				}

				if (speed > info.MaxSpeedMHz) info.MaxSpeedMHz = speed;

				byte memType = buffer[offset + 0x12];
				info.DDRVersion = memType switch
				{
					0x12 => "DDR",
					0x13 => "DDR2",
					0x18 => "DDR3",
					0x1A => "DDR4",
					0x22 => "DDR5",
					_ => info.DDRVersion == "" ? "DDRx" : info.DDRVersion
				};
			}

			offset += length;
			while (offset + 1 < buffer.Length && (buffer[offset] != 0 || buffer[offset + 1] != 0))
				offset++;
			offset += 2;
		}

		return info;
	}
}
