using System.Runtime.InteropServices;
using AutoOS.Core.Drivers.InpOut;
using Windows.Win32;

namespace AutoOS.Core.Helpers.ReadWrite;

public partial class ReadWriteHelper : IDisposable
{
	public ReadWriteHelper()
	{
	}

	// MSR - WinRing0 removed; stubs until replacement driver is integrated
	public static bool ReadMsr(uint index, out ulong value)
	{
		value = 0;
		return false;
	}

	public static bool WriteMsr(uint index, ulong value)
	{
		return false;
	}

	// PMC - WinRing0 removed; stubs until replacement driver is integrated
	public static ulong ReadPmc(uint index)
	{
		return 0;
	}

	public static ulong ReadPmcTx(uint index, UIntPtr threadAffinityMask)
	{
		return 0;
	}

	// PCI - WinRing0 removed; stubs until replacement driver is integrated
	public static bool ReadPci(uint bus, uint dev, uint func, byte offset, int size, out uint value)
	{
		value = 0;
		return false;
	}

	public static void WritePci(uint bus, uint dev, uint func, byte offset, uint value, int size)
	{
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

	// IO Port - WinRing0 removed; stubs until replacement driver is integrated
	public static uint ReadIo(ushort port, int size)
	{
		return 0;
	}

	public static void WriteIo(ushort port, uint value, int size)
	{
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

		try
		{
			if (PInvoke.IsBadReadPtr((void*)(pLinAddr + (nint)extra), (nuint)length))
				return false;

			Marshal.Copy(pLinAddr + (nint)extra, output, 0, (int)length);
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

	public unsafe bool WriteMemory(ulong address, byte[] buffer)
	{
		ulong baseAddress = address & ~0xFFFUL;
		uint extra = (uint)(address - baseAddress);
		uint mapLength = extra + (uint)buffer.Length + 0x1000;

		IntPtr pLinAddr = InpOut.MapPhysToLin((IntPtr)baseAddress, mapLength, out nint hMapping);
		if (pLinAddr == IntPtr.Zero) return false;

		try
		{
			if (PInvoke.IsBadWritePtr((void*)(pLinAddr + (nint)extra), (nuint)buffer.Length))
				return false;

			Marshal.Copy(buffer, 0, pLinAddr + (nint)extra, buffer.Length);
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
		GC.SuppressFinalize(this);
	}
}
