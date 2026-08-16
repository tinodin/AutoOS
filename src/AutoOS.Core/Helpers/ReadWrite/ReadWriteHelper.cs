using Windows.Win32;
using Windows.Win32.System.Memory;

namespace AutoOS.Core.Helpers.ReadWrite;

public partial class ReadWriteHelper : IDisposable
{
	public ReadWriteHelper()
	{
		//if (WinRing.InitializeOls() == 0)
		//    throw new Exception("Failed to initialize WinRing0 driver.");

		//if (WinRing.GetDllStatus() != 0)
		//    throw new Exception($"WinRing0 status error: {WinRing.GetDllStatus()}");
	}

	// MSR
	public static bool ReadMsr(uint index, out ulong value)
	{
		uint eax = 0, edx = 0;
		if (WinRing.Rdmsr(index, ref eax, ref edx) != 0)
		{
			value = ((ulong)edx << 32) | eax;
			return true;
		}
		value = 0;
		return false;
	}

	public static bool WriteMsr(uint index, ulong value)
	{
		uint eax = (uint)(value & 0xFFFFFFFF);
		uint edx = (uint)(value >> 32);
		return WinRing.Wrmsr(index, eax, edx) != 0;
	}

	// PMC
	public static ulong ReadPmc(uint index)
	{
		uint eax = 0, edx = 0;
		WinRing.Rdpmc(index, ref eax, ref edx);
		return ((ulong)edx << 32) | eax;
	}

	public static ulong ReadPmcTx(uint index, UIntPtr threadAffinityMask)
	{
		uint eax = 0, edx = 0;
		WinRing.RdpmcTx(index, ref eax, ref edx, threadAffinityMask);
		return ((ulong)edx << 32) | eax;
	}

	// PCI
	public static bool ReadPci(uint bus, uint dev, uint func, byte offset, int size, out uint value)
	{
		uint address = WinRing.PciBusDevFunc(bus, dev, func);
		value = size switch
		{
			8 => WinRing.ReadPciConfigByte(address, offset),
			16 => WinRing.ReadPciConfigWord(address, offset),
			32 => WinRing.ReadPciConfigDword(address, offset),
			_ => 0
		};
		return true;
	}

	public static void WritePci(uint bus, uint dev, uint func, byte offset, uint value, int size)
	{
		uint address = WinRing.PciBusDevFunc(bus, dev, func);
		switch (size)
		{
			case 8: WinRing.WritePciConfigByte(address, offset, (byte)value); break;
			case 16: WinRing.WritePciConfigWord(address, offset, (ushort)value); break;
			case 32: WinRing.WritePciConfigDword(address, offset, value); break;
		}
	}

	public static ulong ReadPciBit(string bdf, byte offset, string bitRange, int size)
	{
		if (!TryParseBdf(bdf, out uint b, out uint d, out uint f)) return 0;
		if (ReadPci(b, d, f, offset, size, out uint val))
		{
			return GetBits(val, bitRange);
		}
		return 0;
	}

	public static void WritePciBit(string bdf, byte offset, string bitRange, ulong value, int size)
	{
		if (!TryParseBdf(bdf, out uint b, out uint d, out uint f)) return;
		if (ReadPci(b, d, f, offset, size, out uint current))
		{
			uint updated = (uint)SetBits(current, bitRange, value);
			WritePci(b, d, f, offset, updated, size);
		}
	}

	private static bool TryParseBdf(string bdf, out uint bus, out uint dev, out uint func)
	{
		bus = dev = func = 0;
		string[] parts = bdf.Split(':');
		if (parts.Length != 3) return false;

		return uint.TryParse(parts[0], System.Globalization.NumberStyles.HexNumber, null, out bus) &&
		uint.TryParse(parts[1], System.Globalization.NumberStyles.HexNumber, null, out dev) &&
		uint.TryParse(parts[2], System.Globalization.NumberStyles.HexNumber, null, out func);
	}

	// IO Port
	public static uint ReadIo(ushort port, int size)
	{
		return size switch
		{
			8 => WinRing.ReadIoPortByte(port),
			16 => WinRing.ReadIoPortWord(port),
			32 => WinRing.ReadIoPortDword(port),
			_ => 0
		};
	}

	public static void WriteIo(ushort port, uint value, int size)
	{
		switch (size)
		{
			case 8: WinRing.WriteIoPortByte(port, (byte)value); break;
			case 16: WinRing.WriteIoPortWord(port, (ushort)value); break;
			case 32: WinRing.WriteIoPortDword(port, value); break;
		}
	}

	// Physical Memory
	public unsafe bool ReadMemory(ulong address, uint length, byte[] output)
	{
		if (length == 0 || output.Length < length)
			return false;

		ulong baseAddress = address & ~0xFFFUL;
		uint extra = (uint)(address - baseAddress);
		uint mapLength = extra + length + 0x1000;

		IntPtr pLinAddr = InpOut.MapPhysToLin((IntPtr)baseAddress, mapLength, out nint hMapping);
		if (pLinAddr == IntPtr.Zero)
			return false;

		if (!IsReadableMemory(pLinAddr, (int)(extra + length)))
		{
			_ = InpOut.UnmapPhysicalMemory(hMapping, pLinAddr);
			return false;
		}

		try
		{
			fixed (byte* pOutput = output)
			{
				Buffer.MemoryCopy((void*)(pLinAddr + (nint)extra), pOutput, length, length);
			}
			return true;
		}
		catch
		{
			return false;
		}
		finally
		{
			_ = InpOut.UnmapPhysicalMemory(hMapping, pLinAddr);
		}
	}

	private static unsafe bool IsReadableMemory(IntPtr address, int length)
	{
		ulong addr = (ulong)address.ToInt64();
		long remaining = length;

		while (remaining > 0)
		{
			MEMORY_BASIC_INFORMATION mbi;
			if (PInvoke.VirtualQuery((void*)addr, &mbi, (nuint)sizeof(MEMORY_BASIC_INFORMATION)) == 0)
				return false;

			if (mbi.State != VIRTUAL_ALLOCATION_TYPE.MEM_COMMIT)
				return false;

			if ((mbi.Protect & (PAGE_PROTECTION_FLAGS.PAGE_GUARD | PAGE_PROTECTION_FLAGS.PAGE_NOACCESS)) != 0)
				return false;

			if ((mbi.Protect & (PAGE_PROTECTION_FLAGS.PAGE_READONLY | PAGE_PROTECTION_FLAGS.PAGE_READWRITE | PAGE_PROTECTION_FLAGS.PAGE_WRITECOPY | PAGE_PROTECTION_FLAGS.PAGE_EXECUTE_READ | PAGE_PROTECTION_FLAGS.PAGE_EXECUTE_READWRITE | PAGE_PROTECTION_FLAGS.PAGE_EXECUTE_WRITECOPY)) == 0)
				return false;

			ulong regionSize = (ulong)mbi.RegionSize;
			if (regionSize == 0)
				return false;

			ulong regionEnd = (ulong)mbi.BaseAddress + regionSize;
			remaining -= (long)Math.Min((ulong)remaining, regionEnd - addr);
			addr = regionEnd;
		}

		return true;
	}

	public unsafe bool WriteMemory(ulong address, byte[] buffer)
	{
		IntPtr pLinAddr = InpOut.MapPhysToLin((IntPtr)address, (uint)buffer.Length, out nint hMapping);
		if (pLinAddr == IntPtr.Zero) return false;

		try
		{
			fixed (byte* pBuffer = buffer)
			{
				Buffer.MemoryCopy(pBuffer, (void*)pLinAddr, buffer.Length, buffer.Length);
			}
			return true;
		}
		catch
		{
			return false;
		}
		finally
		{
			_ = InpOut.UnmapPhysicalMemory(hMapping, pLinAddr);
		}
	}

	public bool ReadMemory32(ulong address, out uint value)
	{
		byte[] buffer = new byte[4];
		if (ReadMemory(address, 4, buffer))
		{
			value = BitConverter.ToUInt32(buffer, 0);
			return true;
		}
		value = 0;
		return false;
	}

	public bool WriteMemory32(ulong address, uint value)
	{
		return WriteMemory(address, BitConverter.GetBytes(value));
	}

	public static ulong GetBits(ulong value, string bitRange)
	{
		if (!TryParseBitRange(bitRange, out int start, out int end)) return 0;
		int low = Math.Min(start, end);
		int high = Math.Max(start, end);
		ulong mask = high == 63 ? ulong.MaxValue : (1UL << (high + 1)) - 1;
		return (value & mask) >> low;
	}

	public static ulong SetBits(ulong original, string bitRange, ulong newValue)
	{
		if (!TryParseBitRange(bitRange, out int start, out int end)) return original;
		int low = Math.Min(start, end);
		int high = Math.Max(start, end);
		ulong mask = high == 63 ? ulong.MaxValue : (1UL << (high + 1)) - 1;
		mask &= ~((1UL << low) - 1);
		return (original & ~mask) | ((newValue << low) & mask);
	}

	private static bool TryParseBitRange(string range, out int start, out int end)
	{
		start = end = 0;
		string[] parts = range.Split(':');
		if (parts.Length == 1 && int.TryParse(parts[0], out start)) { end = start; return true; }
		if (parts.Length == 2 && int.TryParse(parts[0], out start) && int.TryParse(parts[1], out end)) return true;
		return false;
	}

	public void Dispose()
	{
		WinRing.DeinitializeOls();
		GC.SuppressFinalize(this);
	}
}
