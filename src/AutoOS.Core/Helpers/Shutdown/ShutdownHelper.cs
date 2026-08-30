using System.Runtime.InteropServices;
using AutoOS.Core.Helpers.Registry;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Shutdown;

namespace AutoOS.Core.Helpers.Shutdown;

public static partial class ShutdownHelper
{
	private const ulong EFI_OS_INDICATIONS_BOOT_TO_FIRMWARE_UI = 0x1;

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

	public static bool TrySetOsIndications(out int win32Error)
	{
		EnablePrivileges();
		win32Error = 0;

		unsafe
		{
			string guidString = $"{{{EFI_GLOBAL_VARIABLE.ToString().ToUpperInvariant()}}}";

			byte[] supportedBuffer = new byte[8];
			uint supportedAttrs = 0;
			uint supportedSize;
			fixed (char* namePtr = "OsIndicationsSupported")
			fixed (char* guidPtr = guidString)
			fixed (byte* bufPtr = supportedBuffer)
			{
				supportedSize = PInvoke.GetFirmwareEnvironmentVariableEx(new PCWSTR(namePtr), new PCWSTR(guidPtr), bufPtr, (uint)supportedBuffer.Length, &supportedAttrs);
			}

			if (supportedSize > 0 && supportedSize <= (uint)supportedBuffer.Length)
			{
				ulong supported = 0;
				for (int i = 0; i < supportedSize && i < 8; i++)
					supported |= (ulong)supportedBuffer[i] << (8 * i);

				if ((supported & EFI_OS_INDICATIONS_BOOT_TO_FIRMWARE_UI) == 0)
				{
					win32Error = 19;
					return false;
				}
			}
			else if (supportedSize == 0)
			{
				int supportedError = Marshal.GetLastWin32Error();
				if (supportedError != 0 && supportedError != 203)
				{
					win32Error = supportedError;
					return false;
				}
			}

			byte[] readBuffer = new byte[8];
			uint attrs = 0;
			uint readSize;
			fixed (char* namePtr = "OsIndications")
			fixed (char* guidPtr = guidString)
			fixed (byte* bufPtr = readBuffer)
			{
				readSize = PInvoke.GetFirmwareEnvironmentVariableEx(new PCWSTR(namePtr), new PCWSTR(guidPtr), bufPtr, (uint)readBuffer.Length, &attrs);
			}

			ulong newValue = EFI_OS_INDICATIONS_BOOT_TO_FIRMWARE_UI;
			uint attributesToUse = 0;
			bool hasAttributes = false;

			if (readSize > 0 && readSize <= (uint)readBuffer.Length)
			{
				ulong existing = 0;
				for (int i = 0; i < readSize && i < 8; i++)
					existing |= (ulong)readBuffer[i] << (8 * i);

				newValue |= existing;
				attributesToUse = attrs;
				hasAttributes = true;
			}
			else if (readSize == 0)
			{
				int readError = Marshal.GetLastWin32Error();
				if (readError != 0 && readError != 203)
				{
					win32Error = readError;
					return false;
				}

				if (supportedSize > 0)
				{
					attributesToUse = supportedAttrs;
					hasAttributes = true;
				}
				else
				{
					win32Error = readError != 0 ? readError : 203;
					return false;
				}
			}

			if (!hasAttributes)
			{
				win32Error = 203;
				return false;
			}

			byte[] data = BitConverter.GetBytes(newValue);
			BOOL ok;
			fixed (char* namePtr = "OsIndications")
			fixed (char* guidPtr = guidString)
			fixed (byte* dataPtr = data)
			{
				ok = PInvoke.SetFirmwareEnvironmentVariableEx(new PCWSTR(namePtr), new PCWSTR(guidPtr), dataPtr, (uint)data.Length, attributesToUse);
			}

			if (ok == 0)
			{
				win32Error = Marshal.GetLastWin32Error();
				return false;
			}
		}

		return true;
	}

	public static string FormatWin32Error(int error) => error switch
	{
		0 => "Success",
		5 => "ERROR_ACCESS_DENIED (5) - privilege not granted",
		19 => "ERROR_WRITE_PROTECT (19) - variable is write-protected/locked",
		87 => "ERROR_INVALID_PARAMETER (87) - attributes or data rejected by firmware",
		122 => "ERROR_INSUFFICIENT_BUFFER (122) - data too large",
		203 => "ERROR_ENVVAR_NOT_FOUND (203) - environment variable not found",
		998 => "ERROR_NOACCESS (998)",
		_ => $"Win32 error {error}"
	};

	private static void EnablePrivileges()
	{
		RegistryHelper.EnablePrivilege("SeSystemEnvironmentPrivilege");
		RegistryHelper.EnablePrivilege("SeShutdownPrivilege");
	}
}
