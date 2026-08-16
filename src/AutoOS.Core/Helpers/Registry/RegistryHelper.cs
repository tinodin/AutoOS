using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.ServiceProcess;
using AutoOS.Core.Helpers.Services;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Security;
using Windows.Win32.System.Registry;
using Windows.Win32.System.Services;
using Windows.Win32.System.Threading;

namespace AutoOS.Core.Helpers.Registry;

public static partial class RegistryHelper
{

	public const uint ApplicationDataBoolean = 0x5f5e10b;
	public const uint ApplicationDataString = 0x5f5e10c;
	public const uint ApplicationDataBool = 0x5f5e104;
	public const uint ApplicationDataInt32 = 0x5f5e107;

	public enum Identity { CurrentUser, TrustedInstaller, System }
	private static SafeAccessTokenHandle? _currentUserToken;
	private static SafeAccessTokenHandle? _trustedInstallerToken;
	private static SafeAccessTokenHandle? _systemToken;

	private static readonly Lock _lock = new();

	public static void RunAs(Identity identity, Action action)
	{
		SafeAccessTokenHandle impersonation = GetToken(identity);
		WindowsIdentity.RunImpersonated(impersonation, action);
	}

	public static async Task RunAs(Identity identity, Func<Task> action)
	{
		SafeAccessTokenHandle impersonation = GetToken(identity);
		await WindowsIdentity.RunImpersonatedAsync(impersonation, action);
	}

	public static async Task RunAs(Identity identity, ProcessStartInfo psi)
	{
		SafeAccessTokenHandle hToken = GetToken(identity);

		await Task.Run(() =>
		{
			EnablePrivilege("SeImpersonatePrivilege");

			using (var sc = new ServiceController("seclogon"))
			{
				if (sc.Status != ServiceControllerStatus.Running)
				{
					ServicesHelper.SetStartupType("seclogon", SERVICE_START_TYPE.SERVICE_DEMAND_START);
					try
					{
						sc.Start();
						sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(5));
					}
					catch (InvalidOperationException ex) when (ex.InnerException is Win32Exception { NativeErrorCode: 1056 })
					{	}
				}
			}

			var si = new STARTUPINFOW();
			si.cb = (uint)Marshal.SizeOf<STARTUPINFOW>();
			si.dwFlags = STARTUPINFOW_FLAGS.STARTF_USESHOWWINDOW;
			si.wShowWindow = (psi.WindowStyle switch
			{
				ProcessWindowStyle.Hidden => 0,
				ProcessWindowStyle.Minimized => 2,
				ProcessWindowStyle.Maximized => 3,
				_ => psi.CreateNoWindow ? (ushort)0 : (ushort)1
			});

			var pi = new PROCESS_INFORMATION();
			string commandLine = (string.IsNullOrEmpty(psi.Arguments) ? $"\"{psi.FileName}\"" : $"\"{psi.FileName}\" {psi.Arguments}") + "\0";
			PROCESS_CREATION_FLAGS creationFlags = 0;
			if (psi.CreateNoWindow) creationFlags |= PROCESS_CREATION_FLAGS.CREATE_NO_WINDOW;

			Span<char> pCommandLine = commandLine.ToCharArray();
			unsafe
			{
				if (!PInvoke.CreateProcessWithToken(hToken, (CREATE_PROCESS_LOGON_FLAGS)1, null, ref pCommandLine, creationFlags, null, string.IsNullOrEmpty(psi.WorkingDirectory) ? null : psi.WorkingDirectory, in si, out pi))
					throw new Win32Exception(Marshal.GetLastWin32Error());
			}

			try
			{
				PInvoke.WaitForSingleObject(pi.hProcess, 0xFFFFFFFF);
			}
			finally
			{
				PInvoke.CloseHandle(pi.hProcess);
				PInvoke.CloseHandle(pi.hThread);
			}
		});
	}

	public static void SetValue(Identity identity, string keyPath, string valueName, object value, RegistryValueKind valueKind = RegistryValueKind.Unknown, bool applyToDefault = false)
	{
		RunAs(identity, () =>
		{
			(RegistryKey? root, string? subKeyPath) = ParseKeyPath(keyPath);
			using RegistryKey key = root.CreateSubKey(subKeyPath, true);
			if (key != null)
			{
				if (valueKind == RegistryValueKind.DWord)
				{
					if (value is uint u) value = unchecked((int)u);
					else if (value is long l) value = unchecked((int)l);
				}
				else if (valueKind == RegistryValueKind.QWord)
				{
					if (value is ulong ul) value = unchecked((long)ul);
				}

				if (valueKind == RegistryValueKind.Unknown)
					key.SetValue(valueName, value);
				else
					key.SetValue(valueName, value, valueKind);
			}

			if (applyToDefault && identity == Identity.CurrentUser)
			{
				(RegistryKey? defaultRoot, string? defaultSubKeyPath) = ParseKeyPath(keyPath.Replace("HKEY_CURRENT_USER", @"HKEY_USERS\DefaultUser"));
				using RegistryKey defaultKey = defaultRoot.CreateSubKey(defaultSubKeyPath, true);
				if (defaultKey != null)
				{
					if (valueKind == RegistryValueKind.Unknown)
						defaultKey.SetValue(valueName, value);
					else
						defaultKey.SetValue(valueName, value, valueKind);
				}
			}
		});
	}

	public static void SetValue(Identity identity, string keyPath, string valueName, uint type, byte[] data)
	{
		RunAs(identity, () =>
		{
			string[] parts = keyPath.Split('\\', 2);
			if (parts.Length < 2)
				throw new ArgumentException($"Invalid registry key path: {keyPath}");

			string rootName = parts[0].ToUpperInvariant();
			string subKey = parts[1];

			unsafe
			{
				SafeRegistryHandle hRoot = rootName switch
				{
					"HKEY_CURRENT_USER" or "HKCU" => new SafeRegistryHandle(unchecked((nint)0x80000001), false),
					"HKEY_LOCAL_MACHINE" or "HKLM" => new SafeRegistryHandle(unchecked((nint)0x80000002), false),
					"HKEY_CLASSES_ROOT" or "HKCR" => new SafeRegistryHandle(unchecked((nint)0x80000000), false),
					"HKEY_USERS" or "HKU" => new SafeRegistryHandle(unchecked((nint)0x80000003), false),
					"HKEY_CURRENT_CONFIG" or "HKCC" => new SafeRegistryHandle(unchecked((nint)0x80000005), false),
					_ => throw new ArgumentException($"Unsupported registry root: {rootName}")
				};

				WIN32_ERROR keyResult = PInvoke.RegCreateKeyEx(hRoot, subKey, default, REG_OPEN_CREATE_OPTIONS.REG_OPTION_NON_VOLATILE, REG_SAM_FLAGS.KEY_WRITE, null, out SafeRegistryHandle hSubKey, out _);
				if (keyResult != WIN32_ERROR.ERROR_SUCCESS)
				{
					throw new Win32Exception((int)keyResult);
				}

				using (hSubKey)
				{
					fixed (char* pValueName = valueName)
					fixed (byte* pData = data)
					{
						WIN32_ERROR setResult = PInvoke.RegSetValueEx(new HKEY(hSubKey.DangerousGetHandle()), pValueName, 0, (REG_VALUE_TYPE)type, pData, (uint)data.Length);
						if (setResult != WIN32_ERROR.ERROR_SUCCESS)
						{
							throw new Win32Exception((int)setResult);
						}
					}
				}
			}
		});
	}

	public static void DeleteValue(Identity identity, string keyPath, string valueName)
	{
		RunAs(identity, () =>
		{
			(RegistryKey? root, string? subKeyPath) = ParseKeyPath(keyPath);
			using RegistryKey? key = root.OpenSubKey(subKeyPath, true);
			key?.DeleteValue(valueName, false);
		});
	}

	public static void DeleteKey(Identity identity, string keyPath)
	{
		RunAs(identity, () =>
		{
			(RegistryKey? root, string? subKeyPath) = ParseKeyPath(keyPath);
			root.DeleteSubKeyTree(subKeyPath, false);
		});
	}

	public static string[] GetValueNames(Identity identity, string keyPath)
	{
		string[] names = [];
		RunAs(identity, () =>
		{
			(RegistryKey? root, string? subKeyPath) = ParseKeyPath(keyPath);
			using RegistryKey? key = root.OpenSubKey(subKeyPath, false);
			if (key != null)
			{
				names = key.GetValueNames();
			}
		});
		return names;
	}

	private static (RegistryKey root, string subKey) ParseKeyPath(string fullPath)
	{
		int firstBackslash = fullPath.IndexOf('\\');
		if (firstBackslash == -1) return (null!, fullPath);

		string rootName = fullPath.Substring(0, firstBackslash).ToUpperInvariant();
		string subKey = fullPath.Substring(firstBackslash + 1);

		RegistryKey root = rootName switch
		{
			"HKEY_CURRENT_USER" or "HKCU" => Microsoft.Win32.Registry.CurrentUser,
			"HKEY_LOCAL_MACHINE" or "HKLM" => Microsoft.Win32.Registry.LocalMachine,
			"HKEY_CLASSES_ROOT" or "HKCR" => Microsoft.Win32.Registry.ClassesRoot,
			"HKEY_USERS" or "HKU" => Microsoft.Win32.Registry.Users,
			"HKEY_CURRENT_CONFIG" or "HKCC" => Microsoft.Win32.Registry.CurrentConfig,
			_ => throw new ArgumentException($"Unsupported registry root: {rootName}")
		};

		return (root, subKey);
	}

	private static SafeAccessTokenHandle GetToken(Identity identity)
	{
		lock (_lock)
		{
			switch (identity)
			{
				case Identity.System:
					if (_systemToken == null || _systemToken.IsInvalid)
					{
						_systemToken = CreateSystemToken();
						EnableAllPrivileges(_systemToken);
					}
					return _systemToken;
				case Identity.TrustedInstaller:
					if (_trustedInstallerToken == null || _trustedInstallerToken.IsInvalid)
					{
						_trustedInstallerToken = CreateTrustedInstallerToken();
						EnableAllPrivileges(_trustedInstallerToken);
					}
					return _trustedInstallerToken;
				case Identity.CurrentUser:
					if (_currentUserToken == null || _currentUserToken.IsInvalid)
					{
						_currentUserToken = CreateCurrentUserToken();
						EnableAllPrivileges(_currentUserToken);
					}
					return _currentUserToken;
				default:
					throw new ArgumentException("Invalid identity.");
			}
		}
	}

	private static unsafe SafeAccessTokenHandle CreateSystemToken()
	{
		Process winlogon = Process.GetProcessesByName("winlogon").FirstOrDefault() ?? throw new Exception("winlogon.exe not found.");
		if (!PInvoke.OpenProcessToken(winlogon.SafeHandle, TOKEN_ACCESS_MASK.TOKEN_DUPLICATE, out SafeFileHandle hToken))
			throw new Win32Exception(Marshal.GetLastWin32Error());

		using (hToken)
		{
			if (!PInvoke.DuplicateTokenEx(hToken, (TOKEN_ACCESS_MASK)0x01FF, null, SECURITY_IMPERSONATION_LEVEL.SecurityImpersonation, TOKEN_TYPE.TokenPrimary, out SafeFileHandle hNewToken))
				throw new Win32Exception(Marshal.GetLastWin32Error());

			IntPtr handle = hNewToken.DangerousGetHandle();
			hNewToken.SetHandleAsInvalid();
			return new SafeAccessTokenHandle(handle);
		}
	}

	private static unsafe SafeAccessTokenHandle CreateTrustedInstallerToken()
	{
		using SafeAccessTokenHandle sysToken = CreateSystemToken();
		return WindowsIdentity.RunImpersonated(sysToken, () =>
		{
			EnablePrivilege("SeDebugPrivilege");

			using (var sc = new ServiceController("TrustedInstaller"))
			{
				if (sc.Status != ServiceControllerStatus.Running)
				{
					try
					{
						ServicesHelper.SetStartupType("TrustedInstaller", SERVICE_START_TYPE.SERVICE_DEMAND_START);
						sc.Start();
						sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(5));
					}
					catch (InvalidOperationException ex) when (ex.InnerException is Win32Exception { NativeErrorCode: 1056 })
					{ }
				}
			}

			Process tiProcess = Process.GetProcessesByName("TrustedInstaller").FirstOrDefault() ?? throw new Exception("TrustedInstaller not found.");
			if (!PInvoke.OpenProcessToken(tiProcess.SafeHandle, (TOKEN_ACCESS_MASK)0x01FF, out SafeFileHandle tiToken))
				throw new Win32Exception(Marshal.GetLastWin32Error());

			using (tiToken)
			{
				if (!PInvoke.DuplicateTokenEx(tiToken, (TOKEN_ACCESS_MASK)0x01FF, null, SECURITY_IMPERSONATION_LEVEL.SecurityImpersonation, TOKEN_TYPE.TokenPrimary, out SafeFileHandle hNewToken))
					throw new Win32Exception(Marshal.GetLastWin32Error());

				IntPtr handle = hNewToken.DangerousGetHandle();
				hNewToken.SetHandleAsInvalid();
				return new SafeAccessTokenHandle(handle);
			}
		});
	}

	private static unsafe SafeAccessTokenHandle CreateCurrentUserToken()
	{
		if (!PInvoke.OpenProcessToken(Process.GetCurrentProcess().SafeHandle, TOKEN_ACCESS_MASK.TOKEN_DUPLICATE | TOKEN_ACCESS_MASK.TOKEN_QUERY, out SafeFileHandle hToken))
			throw new Win32Exception(Marshal.GetLastWin32Error());

		using (hToken)
		{
			if (!PInvoke.DuplicateTokenEx(hToken, (TOKEN_ACCESS_MASK)0x01FF, null, SECURITY_IMPERSONATION_LEVEL.SecurityImpersonation, TOKEN_TYPE.TokenPrimary, out SafeFileHandle hNewToken))
				throw new Win32Exception(Marshal.GetLastWin32Error());

			IntPtr handle = hNewToken.DangerousGetHandle();
			hNewToken.SetHandleAsInvalid();
			return new SafeAccessTokenHandle(handle);
		}
	}

	private static unsafe void EnableAllPrivileges(SafeAccessTokenHandle hToken)
	{
		uint tokenPrivilegesSize = 0;
		PInvoke.GetTokenInformation(new HANDLE(hToken.DangerousGetHandle()), TOKEN_INFORMATION_CLASS.TokenPrivileges, null, 0, &tokenPrivilegesSize);
		if (tokenPrivilegesSize == 0) return;

		byte[] buffer = new byte[tokenPrivilegesSize];
		fixed (byte* pBuffer = buffer)
		{
			if (PInvoke.GetTokenInformation(new HANDLE(hToken.DangerousGetHandle()), TOKEN_INFORMATION_CLASS.TokenPrivileges, pBuffer, tokenPrivilegesSize, &tokenPrivilegesSize))
			{
				TOKEN_PRIVILEGES* tp = (TOKEN_PRIVILEGES*)pBuffer;
				for (uint i = 0; i < tp->PrivilegeCount; i++)
				{
					LUID_AND_ATTRIBUTES* pAttr = (LUID_AND_ATTRIBUTES*)((byte*)tp + sizeof(uint) + (i * sizeof(LUID_AND_ATTRIBUTES)));
					pAttr->Attributes = TOKEN_PRIVILEGES_ATTRIBUTES.SE_PRIVILEGE_ENABLED;
				}
				PInvoke.AdjustTokenPrivileges(new HANDLE(hToken.DangerousGetHandle()), false, tp, tokenPrivilegesSize, null, null);
			}
		}
	}

	public static unsafe void EnablePrivilege(string privilege)
	{
		if (!PInvoke.LookupPrivilegeValue(null, privilege, out LUID luid)) return;

		TOKEN_PRIVILEGES tp = new()
		{
			PrivilegeCount = 1,
		};
		tp.Privileges[0].Luid = luid;
		tp.Privileges[0].Attributes = TOKEN_PRIVILEGES_ATTRIBUTES.SE_PRIVILEGE_ENABLED;

		if (PInvoke.OpenProcessToken(Process.GetCurrentProcess().SafeHandle, TOKEN_ACCESS_MASK.TOKEN_ADJUST_PRIVILEGES, out SafeFileHandle hToken))
		{
			using (hToken)
			{
				PInvoke.AdjustTokenPrivileges(new HANDLE(hToken.DangerousGetHandle()), false, &tp, (uint)sizeof(TOKEN_PRIVILEGES), null, null);
			}
		}
	}
}
