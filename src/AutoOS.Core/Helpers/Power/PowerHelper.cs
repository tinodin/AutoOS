using System.Runtime.InteropServices;
using System.Text;
using Windows.Win32.Foundation;
using Windows.Win32.System.Power;
using Windows.Win32;

namespace AutoOS.Core.Helpers.Power;

public static unsafe class PowerHelper
{
    public static IntPtr AllocGuid(Guid guid)
    {
        IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf<Guid>());
        Marshal.StructureToPtr(guid, ptr, false);
        return ptr;
    }

    public static string ReadFriendlyName(Guid scheme, Guid? subgroup, Guid? setting)
    {
        uint size = 512;
        byte* pBuffer = stackalloc byte[512];
        uint res = (uint)PInvoke.PowerReadFriendlyName(default, scheme, subgroup, setting, new Span<byte>(pBuffer, 512), ref size);

        if (res == (uint)WIN32_ERROR.ERROR_MORE_DATA && size > 512 && size <= 4096)
        {
            byte* pLargeBuffer = stackalloc byte[(int)size];
            res = (uint)PInvoke.PowerReadFriendlyName(default, scheme, subgroup, setting, new Span<byte>(pLargeBuffer, (int)size), ref size);
            return res == 0 ? new string((sbyte*)pLargeBuffer, 0, (int)size, Encoding.Unicode).TrimEnd('\0') : string.Empty;
        }

        return res == 0 ? new string((sbyte*)pBuffer, 0, (int)size, Encoding.Unicode).TrimEnd('\0') : string.Empty;
    }

    public static string ReadDescription(Guid scheme, Guid? subgroup = null, Guid? setting = null)
    {
        uint size = 512;
        byte* pBuffer = stackalloc byte[512];
        uint res = (uint)PInvoke.PowerReadDescription(default, scheme, subgroup, setting, new Span<byte>(pBuffer, 512), ref size);

        if (res == (uint)WIN32_ERROR.ERROR_MORE_DATA && size > 512 && size <= 4096)
        {
            byte* pLargeBuffer = stackalloc byte[(int)size];
            res = (uint)PInvoke.PowerReadDescription(default, scheme, subgroup, setting, new Span<byte>(pLargeBuffer, (int)size), ref size);
            return res == 0 ? new string((sbyte*)pLargeBuffer, 0, (int)size, Encoding.Unicode).TrimEnd('\0') : string.Empty;
        }

        return res == 0 ? new string((sbyte*)pBuffer, 0, (int)size, Encoding.Unicode).TrimEnd('\0') : string.Empty;
    }

    public static string ReadPossibleFriendlyName(Guid subgroup, Guid setting, uint index)
    {
        uint size = 512;
        byte* pBuffer = stackalloc byte[512];
        WIN32_ERROR res = PInvoke.PowerReadPossibleFriendlyName(default, subgroup, setting, index, new Span<byte>(pBuffer, 512), ref size);

        if (res == WIN32_ERROR.ERROR_MORE_DATA && size > 512 && size <= 4096)
        {
            byte* pLargeBuffer = stackalloc byte[(int)size];
            res = PInvoke.PowerReadPossibleFriendlyName(default, subgroup, setting, index, new Span<byte>(pLargeBuffer, (int)size), ref size);
            return res == WIN32_ERROR.ERROR_SUCCESS ? new string((sbyte*)pLargeBuffer, 0, (int)size, Encoding.Unicode).TrimEnd('\0') : string.Empty;
        }

        return res == WIN32_ERROR.ERROR_SUCCESS ? new string((sbyte*)pBuffer, 0, (int)size, Encoding.Unicode).TrimEnd('\0') : string.Empty;
    }

    public static string ReadPossibleDescription(Guid subgroup, Guid setting, uint index)
    {
        uint size = 512;
        byte* pBuffer = stackalloc byte[512];
        WIN32_ERROR res = PInvoke.PowerReadPossibleDescription(default, subgroup, setting, index, new Span<byte>(pBuffer, 512), ref size);

        if (res == WIN32_ERROR.ERROR_MORE_DATA && size > 512 && size <= 4096)
        {
            byte* pLargeBuffer = stackalloc byte[(int)size];
            res = PInvoke.PowerReadPossibleDescription(default, subgroup, setting, index, new Span<byte>(pLargeBuffer, (int)size), ref size);
            return res == WIN32_ERROR.ERROR_SUCCESS ? new string((sbyte*)pLargeBuffer, 0, (int)size, Encoding.Unicode).TrimEnd('\0') : string.Empty;
        }

        return res == WIN32_ERROR.ERROR_SUCCESS ? new string((sbyte*)pBuffer, 0, (int)size, Encoding.Unicode).TrimEnd('\0') : string.Empty;
    }

    public static uint ReadAcValueIndex(Guid scheme, Guid subgroup, Guid setting)
    {
        return TryReadAcValueIndex(scheme, subgroup, setting, out uint value) ? value : 0;
    }

    public static bool TryReadAcValueIndex(Guid scheme, Guid subgroup, Guid setting, out uint value) => PInvoke.PowerReadACValueIndex(default, scheme, subgroup, setting, out value) == WIN32_ERROR.ERROR_SUCCESS;

    public static uint ReadDcValueIndex(Guid scheme, Guid subgroup, Guid setting)
    {
        return TryReadDcValueIndex(scheme, subgroup, setting, out uint value) ? value : 0;
    }

    public static bool TryReadDcValueIndex(Guid scheme, Guid subgroup, Guid setting, out uint value) => (WIN32_ERROR)PInvoke.PowerReadDCValueIndex(default, scheme, subgroup, setting, out value) == WIN32_ERROR.ERROR_SUCCESS;

    public static uint WriteACValueIndex(Guid scheme, Guid subgroup, Guid setting, uint value)
    {
        return (uint)PInvoke.PowerWriteACValueIndex(default, &scheme, &subgroup, &setting, value);
    }

    public static uint WriteDCValueIndex(Guid scheme, Guid subgroup, Guid setting, uint value)
    {
        return (uint)PInvoke.PowerWriteDCValueIndex(default, &scheme, &subgroup, &setting, value);
    }

    public static uint PowerSetActiveScheme(Guid scheme)
    {
        return (uint)PInvoke.PowerSetActiveScheme(default, scheme);
    }

    public static uint RestoreDefaultPowerSchemes()
    {
        return (uint)PInvoke.PowerRestoreDefaultPowerSchemes();
    }

    public static uint ReadValueMin(Guid subgroup, Guid setting)
    {
        return TryReadValueMin(subgroup, setting, out uint value) ? value : 0;
    }

    public static bool TryReadValueMin(Guid subgroup, Guid setting, out uint value) => PInvoke.PowerReadValueMin(default, subgroup, setting, out value) == WIN32_ERROR.ERROR_SUCCESS;

    public static uint ReadValueMax(Guid subgroup, Guid setting)
    {
        return TryReadValueMax(subgroup, setting, out uint value) ? value : 0;
    }

    public static bool TryReadValueMax(Guid subgroup, Guid setting, out uint value) => PInvoke.PowerReadValueMax(default, subgroup, setting, out value) == WIN32_ERROR.ERROR_SUCCESS;

    public static uint ReadValueIncrement(Guid subgroup, Guid setting)
    {
        return TryReadValueIncrement(subgroup, setting, out uint value) ? value : 0;
    }

    public static bool TryReadValueIncrement(Guid subgroup, Guid setting, out uint value) => PInvoke.PowerReadValueIncrement(default, subgroup, setting, out value) == WIN32_ERROR.ERROR_SUCCESS;

    public static string ReadValueUnitsSpecifier(Guid subgroup, Guid setting)
    {
        uint size = 512;
        byte* pBuffer = stackalloc byte[512];
        WIN32_ERROR res = PInvoke.PowerReadValueUnitsSpecifier(default, subgroup, setting, new Span<byte>(pBuffer, 512), ref size);

        if (res == WIN32_ERROR.ERROR_MORE_DATA && size > 512 && size <= 4096)
        {
            byte* pLargeBuffer = stackalloc byte[(int)size];
            res = PInvoke.PowerReadValueUnitsSpecifier(default, subgroup, setting, new Span<byte>(pLargeBuffer, (int)size), ref size);
            return res == WIN32_ERROR.ERROR_SUCCESS ? new string((sbyte*)pLargeBuffer, 0, (int)size - 2, Encoding.Unicode) : string.Empty;
        }

        return (res == WIN32_ERROR.ERROR_SUCCESS && size >= 2) ? new string((sbyte*)pBuffer, 0, (int)size - 2, Encoding.Unicode) : string.Empty;
    }

    public static bool WriteSchemeFriendlyName(Guid scheme, string name)
    {
        string content = (name ?? string.Empty) + "\0";
        uint size = (uint)content.Length * 2;
        byte[] bytes = Encoding.Unicode.GetBytes(content);
        fixed (byte* pBytes = bytes)
        {
            return (uint)PInvoke.PowerWriteFriendlyName(default, &scheme, null, null, pBytes, size) == 0;
        }
    }

    public static bool WriteSchemeDescription(Guid scheme, string description)
    {
        string content = (description ?? string.Empty) + "\0";
        uint size = (uint)content.Length * 2;
        byte[] bytes = Encoding.Unicode.GetBytes(content);
        fixed (byte* pBytes = bytes)
        {
            return (uint)PInvoke.PowerWriteDescription(default, &scheme, null, null, pBytes, size) == 0;
        }
    }

    public static Guid DuplicateScheme(Guid guid, string name, string description)
    {
        Guid* pDestGuid = null;
        PInvoke.PowerDuplicateScheme(default, guid, ref pDestGuid);
        if (pDestGuid == null) return Guid.Empty;

        Guid newGuid = *pDestGuid;
        PInvoke.LocalFree((HLOCAL)pDestGuid);
        WriteSchemeFriendlyName(newGuid, name);
        WriteSchemeDescription(newGuid, description);

        return newGuid;
    }

    public static bool DeleteScheme(Guid scheme)
    {
        return (uint)PInvoke.PowerDeleteScheme(default, scheme) == 0;
    }

    public static Guid GetPlanGuidByName(string name)
    {
        uint index = 0;
        uint size = (uint)sizeof(Guid);
        byte* pBuffer = stackalloc byte[(int)size];

        while (true)
        {
            uint res = (uint)PInvoke.PowerEnumerate(default, null, null, POWER_DATA_ACCESSOR.ACCESS_SCHEME, index++, new Span<byte>(pBuffer, (int)size), ref size);
            if (res != 0) break;

            Guid schemeGuid = new(new ReadOnlySpan<byte>(pBuffer, (int)size));
            if (string.Equals(ReadFriendlyName(schemeGuid, null, null), name, StringComparison.OrdinalIgnoreCase))
                return schemeGuid;
        }
        return Guid.Empty;
    }
}
