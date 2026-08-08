using System.Diagnostics;
using System.Text;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Power;

namespace AutoOS.Core.Helpers.Power;

public static unsafe class PowerHelper
{
	private delegate uint StringReader(byte[] buffer, ref uint size);

	private static string ReadString(StringReader reader)
	{
		uint size = 512;
		byte[] buffer = new byte[512];

		uint res = reader(buffer, ref size);

		if (res == (uint)WIN32_ERROR.ERROR_MORE_DATA)
		{
			if (size == 0 || size > int.MaxValue)
				return string.Empty;

			buffer = new byte[size];
			res = reader(buffer, ref size);
			if (res != 0)
				return string.Empty;
		}

		if (res != 0 || size == 0)
			return string.Empty;
		return Encoding.Unicode.GetString(buffer, 0, (int)size).TrimEnd('\0');
	}

	public static Guid GetPlanGuidByName(string name)
	{
		foreach (Guid schemeGuid in EnumerateSchemes())
		{
			if (string.Equals(ReadFriendlyName(schemeGuid, null, null), name, StringComparison.OrdinalIgnoreCase))
				return schemeGuid;
		}
		return Guid.Empty;
	}

	public static Guid ReadActiveScheme()
    {
        WIN32_ERROR result = PInvoke.PowerGetActiveScheme(default, out Guid* activeScheme);
        if (result != WIN32_ERROR.ERROR_SUCCESS || activeScheme == null)
            return Guid.Empty;

        try
        {
            return *activeScheme;
        }
        finally
        {
            PInvoke.LocalFree((HLOCAL)activeScheme);
        }
    }

    public static List<Guid> EnumerateSchemes()
    {
        List<Guid> schemes = [];
        uint index = 0;
        uint size = (uint)sizeof(Guid);
        byte* pBuffer = stackalloc byte[(int)size];

        while (true)
        {
            uint res = (uint)PInvoke.PowerEnumerate(default, null, null, POWER_DATA_ACCESSOR.ACCESS_SCHEME, index++, new Span<byte>(pBuffer, (int)size), ref size);
            if (res != 0) break;

            schemes.Add(new Guid(new ReadOnlySpan<byte>(pBuffer, (int)size)));
        }
        return schemes;
    }

    public static List<Guid> EnumerateSubgroups(Guid scheme)
    {
        List<Guid> subgroups = [];
        uint index = 0;
        uint size = (uint)sizeof(Guid);
        byte* pBuffer = stackalloc byte[(int)size];

        while (true)
        {
            uint res = (uint)PInvoke.PowerEnumerate(default, scheme, null, POWER_DATA_ACCESSOR.ACCESS_SUBGROUP, index++, new Span<byte>(pBuffer, (int)size), ref size);
            if (res != 0) break;

            subgroups.Add(new Guid(new ReadOnlySpan<byte>(pBuffer, (int)size)));
        }
        return subgroups;
    }

    public static List<Guid> EnumerateSettings(Guid scheme, Guid? subgroup)
    {
        List<Guid> settings = [];
        uint index = 0;
        uint size = (uint)sizeof(Guid);
        byte* pBuffer = stackalloc byte[(int)size];

        while (true)
        {
            uint res = (uint)PInvoke.PowerEnumerate(default, scheme, subgroup, POWER_DATA_ACCESSOR.ACCESS_INDIVIDUAL_SETTING, index++, new Span<byte>(pBuffer, (int)size), ref size);
            if (res != 0) break;

            settings.Add(new Guid(new ReadOnlySpan<byte>(pBuffer, (int)size)));
        }
        return settings;
    }

    public static string ReadFriendlyName(Guid scheme, Guid? subgroup, Guid? setting)
    {
        return ReadString((buffer, ref size) => (uint)PInvoke.PowerReadFriendlyName(default, scheme, subgroup, setting, buffer, ref size));
    }

    public static string ReadDescription(Guid scheme, Guid? subgroup = null, Guid? setting = null)
    {
        return ReadString((buffer, ref size) => (uint)PInvoke.PowerReadDescription(default, scheme, subgroup, setting, buffer, ref size));
    }

    public static string ReadPossibleFriendlyName(Guid subgroup, Guid setting, uint index)
    {
        return ReadString((buffer, ref size) => (uint)PInvoke.PowerReadPossibleFriendlyName(default, subgroup, setting, index, buffer, ref size));
    }

    public static string ReadPossibleDescription(Guid subgroup, Guid setting, uint index)
    {
        return ReadString((buffer, ref size) => (uint)PInvoke.PowerReadPossibleDescription(default, subgroup, setting, index, buffer, ref size));
    }

    public static bool TryReadAcValueIndex(Guid scheme, Guid subgroup, Guid setting, out uint value) => PInvoke.PowerReadACValueIndex(default, scheme, subgroup, setting, out value) == WIN32_ERROR.ERROR_SUCCESS;

    public static bool TryReadDcValueIndex(Guid scheme, Guid subgroup, Guid setting, out uint value) => (WIN32_ERROR)PInvoke.PowerReadDCValueIndex(default, scheme, subgroup, setting, out value) == WIN32_ERROR.ERROR_SUCCESS;

    public static bool TryReadValueMin(Guid subgroup, Guid setting, out uint value) => PInvoke.PowerReadValueMin(default, subgroup, setting, out value) == WIN32_ERROR.ERROR_SUCCESS;

    public static bool TryReadValueMax(Guid subgroup, Guid setting, out uint value) => PInvoke.PowerReadValueMax(default, subgroup, setting, out value) == WIN32_ERROR.ERROR_SUCCESS;

    public static bool TryReadValueIncrement(Guid subgroup, Guid setting, out uint value) => PInvoke.PowerReadValueIncrement(default, subgroup, setting, out value) == WIN32_ERROR.ERROR_SUCCESS;

    public static string ReadValueUnitsSpecifier(Guid subgroup, Guid setting)
    {
        return ReadString((buffer, ref size) => (uint)PInvoke.PowerReadValueUnitsSpecifier(default, subgroup, setting, buffer, ref size));
    }

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

    public static Guid ImportPowerScheme(string filePath)
    {
        Guid* destination = null;
        uint result = (uint)PInvoke.PowerImportPowerScheme(default, filePath, ref destination);
        if (result != 0 || destination == null)
            return Guid.Empty;

        try
        {
            return *destination;
        }
        finally
        {
            PInvoke.LocalFree((HLOCAL)destination);
        }
    }

    public static void ExportPowerScheme(Guid scheme, string path)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "powercfg.exe",
            Arguments = @$"-export ""{path}"" {scheme:D}",
            UseShellExecute = false,
            CreateNoWindow = true
        };
        using var process = Process.Start(startInfo);
        process?.WaitForExit();
    }

    public static uint RestoreDefaultPowerSchemes()
    {
        return (uint)PInvoke.PowerRestoreDefaultPowerSchemes();
    }
}
