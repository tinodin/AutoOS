// Credit: LuSlower
// https://github.com/LuSlower/chiptool
// Modified: Uses [LibraryImport] instead of [DllImport]

using System.Runtime.InteropServices;

namespace AutoOS.Core.Drivers.InpOut;

internal static partial class InpOut
{
	private const string DllName = "inpoutx64.dll";

	[LibraryImport(DllName)]
	public static partial IntPtr MapPhysToLin(IntPtr pbPhysAddr, uint dwPhysSize, out IntPtr pHandle);

	[LibraryImport(DllName)]
	public static partial int UnmapPhysicalMemory(IntPtr pHandle, IntPtr pbLinAddr);
}
