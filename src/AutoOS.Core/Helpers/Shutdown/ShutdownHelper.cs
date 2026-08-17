using AutoOS.Core.Helpers.Bios;
using AutoOS.Core.Helpers.Registry;
using Windows.Win32;
using Windows.Win32.System.Shutdown;

namespace AutoOS.Core.Helpers.Shutdown;

public static partial class ShutdownHelper
{
	private const ulong EFI_OS_INDICATIONS_BOOT_TO_FIRMWARE_UI = 0x1;
	private const uint FIRMWARE_VARIABLE_ATTRIBUTES = 0x7;

	private static readonly Guid EFI_GLOBAL_VARIABLE = new("8BE4DF61-93CA-11D2-AA0D-00E098032B8C");

	public static void Shutdown()
	{
		EnablePrivileges();
		PInvoke.ExitWindowsEx(EXIT_WINDOWS_FLAGS.EWX_SHUTDOWN | EXIT_WINDOWS_FLAGS.EWX_FORCE, 0);
	}

	public static void Restart()
	{
		EnablePrivileges();
		PInvoke.ExitWindowsEx(EXIT_WINDOWS_FLAGS.EWX_REBOOT | EXIT_WINDOWS_FLAGS.EWX_FORCE, 0);
	}

	public static void RestartIntoBios()
	{
		EnablePrivileges();
		HiiHelper.TrySetVariable("OsIndications", EFI_GLOBAL_VARIABLE, BitConverter.GetBytes(EFI_OS_INDICATIONS_BOOT_TO_FIRMWARE_UI), FIRMWARE_VARIABLE_ATTRIBUTES);
		PInvoke.ExitWindowsEx(EXIT_WINDOWS_FLAGS.EWX_REBOOT | EXIT_WINDOWS_FLAGS.EWX_FORCE, 0);
	}

	private static void EnablePrivileges()
	{
		RegistryHelper.EnablePrivilege("SeSystemEnvironmentPrivilege");
		RegistryHelper.EnablePrivilege("SeShutdownPrivilege");
	}
}
