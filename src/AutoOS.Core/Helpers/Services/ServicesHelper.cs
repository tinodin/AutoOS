using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using AutoOS.Core.Helpers.Registry;
using Microsoft.Win32;
using Windows.Win32;
using Windows.Win32.System.Services;

namespace AutoOS.Core.Helpers.Services;

public static partial class ServicesHelper
{
	public unsafe static void KillServiceProcess(string baseServiceName)
	{
		using CloseServiceHandleSafeHandle scmHandle = PInvoke.OpenSCManager(null, null, (uint)PInvoke.SC_MANAGER_ENUMERATE_SERVICE);
		if (scmHandle.IsInvalid) return;

		SC_HANDLE rawScmHandle = (SC_HANDLE)scmHandle.DangerousGetHandle();
		uint bytesNeeded = 0;
		uint servicesReturned = 0;
		uint resumeHandle = 0;

		PInvoke.EnumServicesStatusEx(
			rawScmHandle,
			SC_ENUM_TYPE.SC_ENUM_PROCESS_INFO,
			ENUM_SERVICE_TYPE.SERVICE_WIN32,
			ENUM_SERVICE_STATE.SERVICE_STATE_ALL,
			null,
			0,
			&bytesNeeded,
			&servicesReturned,
			&resumeHandle,
			null);

		if (bytesNeeded == 0) return;

		byte[] buffer = new byte[bytesNeeded];
		fixed (byte* pBuffer = buffer)
		{
			if (PInvoke.EnumServicesStatusEx(
				rawScmHandle,
				SC_ENUM_TYPE.SC_ENUM_PROCESS_INFO,
				ENUM_SERVICE_TYPE.SERVICE_WIN32,
				ENUM_SERVICE_STATE.SERVICE_STATE_ALL,
				pBuffer,
				(uint)buffer.Length,
				&bytesNeeded,
				&servicesReturned,
				&resumeHandle,
				null))
			{
				var services = (ENUM_SERVICE_STATUS_PROCESSW*)pBuffer;
				for (int i = 0; i < servicesReturned; i++)
				{
					string currentName = services[i].lpServiceName.ToString();
					if (currentName.StartsWith(baseServiceName, StringComparison.OrdinalIgnoreCase))
					{
						int pid = (int)services[i].ServiceStatusProcess.dwProcessId;
						if (pid != 0)
						{
							try
							{
								using var proc = Process.GetProcessById(pid);
								proc.Kill();
								proc.WaitForExit();
							}
							catch { }
						}
					}
				}
			}
		}
	}

	public unsafe static int GetServicePid(string serviceName)
	{
		using CloseServiceHandleSafeHandle scmHandle = PInvoke.OpenSCManager(null, null, (uint)PInvoke.SC_MANAGER_ENUMERATE_SERVICE);
		if (scmHandle.IsInvalid) return 0;

		SC_HANDLE rawScmHandle = (SC_HANDLE)scmHandle.DangerousGetHandle();
		uint bytesNeeded = 0;
		uint servicesReturned = 0;
		uint resumeHandle = 0;

		PInvoke.EnumServicesStatusEx(
			rawScmHandle,
			SC_ENUM_TYPE.SC_ENUM_PROCESS_INFO,
			ENUM_SERVICE_TYPE.SERVICE_WIN32,
			ENUM_SERVICE_STATE.SERVICE_STATE_ALL,
			null,
			0,
			&bytesNeeded,
			&servicesReturned,
			&resumeHandle,
			null);

		if (bytesNeeded == 0) return 0;

		byte[] buffer = new byte[bytesNeeded];
		fixed (byte* pBuffer = buffer)
		{
			if (PInvoke.EnumServicesStatusEx(
				rawScmHandle,
				SC_ENUM_TYPE.SC_ENUM_PROCESS_INFO,
				ENUM_SERVICE_TYPE.SERVICE_WIN32,
				ENUM_SERVICE_STATE.SERVICE_STATE_ALL,
				pBuffer,
				(uint)buffer.Length,
				&bytesNeeded,
				&servicesReturned,
				&resumeHandle,
				null))
			{
				var services = (ENUM_SERVICE_STATUS_PROCESSW*)pBuffer;
				for (int i = 0; i < servicesReturned; i++)
				{
					if (services[i].lpServiceName.ToString().Equals(serviceName, StringComparison.OrdinalIgnoreCase))
					{
						return (int)services[i].ServiceStatusProcess.dwProcessId;
					}
				}
			}
		}
		return 0;
	}

	public unsafe static void StartService(string serviceName)
	{
		using CloseServiceHandleSafeHandle scmHandle = PInvoke.OpenSCManager(null, null, (uint)(PInvoke.SC_MANAGER_CONNECT | PInvoke.SC_MANAGER_ENUMERATE_SERVICE));
		if (scmHandle.IsInvalid) throw new Win32Exception(Marshal.GetLastWin32Error(), "OpenSCManager failed");

		SC_HANDLE rawScmHandle = (SC_HANDLE)scmHandle.DangerousGetHandle();
		uint bytesNeeded = 0;
		uint servicesReturned = 0;
		uint resumeHandle = 0;

		PInvoke.EnumServicesStatusEx(
			rawScmHandle,
			SC_ENUM_TYPE.SC_ENUM_PROCESS_INFO,
			ENUM_SERVICE_TYPE.SERVICE_WIN32,
			ENUM_SERVICE_STATE.SERVICE_STATE_ALL,
			null,
			0,
			&bytesNeeded,
			&servicesReturned,
			&resumeHandle,
			null);

		if (bytesNeeded > 0)
		{
			byte[] buffer = new byte[bytesNeeded];
			fixed (byte* pBuffer = buffer)
			{
				if (PInvoke.EnumServicesStatusEx(
					rawScmHandle,
					SC_ENUM_TYPE.SC_ENUM_PROCESS_INFO,
					ENUM_SERVICE_TYPE.SERVICE_WIN32,
					ENUM_SERVICE_STATE.SERVICE_STATE_ALL,
					pBuffer,
					(uint)buffer.Length,
					&bytesNeeded,
					&servicesReturned,
					&resumeHandle,
					null))
				{
					var services = (ENUM_SERVICE_STATUS_PROCESSW*)pBuffer;
					bool startedAny = false;
					for (int i = 0; i < servicesReturned; i++)
					{
						string currentName = services[i].lpServiceName.ToString();
						if (currentName.Equals(serviceName, StringComparison.OrdinalIgnoreCase) ||
							currentName.StartsWith(serviceName + "_", StringComparison.OrdinalIgnoreCase))
						{
							using CloseServiceHandleSafeHandle instanceHandle = PInvoke.OpenService(scmHandle, currentName, (uint)PInvoke.SERVICE_START);
							if (!instanceHandle.IsInvalid)
							{
								if (PInvoke.StartService(instanceHandle, null))
								{
									startedAny = true;
								}
								else
								{
									int error = Marshal.GetLastWin32Error();
									if (error == 1056 || error == 1061) startedAny = true;
								}
							}
						}
					}
					if (startedAny) return;
				}
			}
		}

		using CloseServiceHandleSafeHandle serviceHandleDirect = PInvoke.OpenService(scmHandle, serviceName, (uint)PInvoke.SERVICE_START);
		if (serviceHandleDirect.IsInvalid)
		{
			int error = Marshal.GetLastWin32Error();
			if (error == 1060) return;
			throw new Win32Exception(error, "OpenService failed");
		}

		if (!PInvoke.StartService(serviceHandleDirect, null))
		{
			int error = Marshal.GetLastWin32Error();
			if (error == 1056 || error == 1061) return;
			throw new Win32Exception(error, "StartService failed");
		}
	}

	public unsafe static void StopService(string baseServiceName)
	{
		using CloseServiceHandleSafeHandle scmHandle = PInvoke.OpenSCManager(null, null, (uint)(PInvoke.SC_MANAGER_CONNECT | PInvoke.SC_MANAGER_ENUMERATE_SERVICE));
		if (scmHandle.IsInvalid) throw new Win32Exception(Marshal.GetLastWin32Error(), "OpenSCManager failed");

		SC_HANDLE rawScmHandle = (SC_HANDLE)scmHandle.DangerousGetHandle();
		uint bytesNeeded = 0;
		uint servicesReturned = 0;
		uint resumeHandle = 0;

		PInvoke.EnumServicesStatusEx(
			rawScmHandle,
			SC_ENUM_TYPE.SC_ENUM_PROCESS_INFO,
			ENUM_SERVICE_TYPE.SERVICE_WIN32,
			ENUM_SERVICE_STATE.SERVICE_STATE_ALL,
			null,
			0,
			&bytesNeeded,
			&servicesReturned,
			&resumeHandle,
			null);

		if (bytesNeeded > 0)
		{
			byte[] buffer = new byte[bytesNeeded];
			fixed (byte* pBuffer = buffer)
			{
				if (PInvoke.EnumServicesStatusEx(
					rawScmHandle,
					SC_ENUM_TYPE.SC_ENUM_PROCESS_INFO,
					ENUM_SERVICE_TYPE.SERVICE_WIN32,
					ENUM_SERVICE_STATE.SERVICE_STATE_ALL,
					pBuffer,
					(uint)buffer.Length,
					&bytesNeeded,
					&servicesReturned,
					&resumeHandle,
					null))
				{
					var services = (ENUM_SERVICE_STATUS_PROCESSW*)pBuffer;
					bool stoppedAny = false;
					for (int i = 0; i < servicesReturned; i++)
					{
						string currentName = services[i].lpServiceName.ToString();
						if (currentName.Equals(baseServiceName, StringComparison.OrdinalIgnoreCase) ||
							currentName.StartsWith(baseServiceName + "_", StringComparison.OrdinalIgnoreCase))
						{
							using CloseServiceHandleSafeHandle instanceHandle = PInvoke.OpenService(scmHandle, currentName, (uint)PInvoke.SERVICE_STOP);
							if (!instanceHandle.IsInvalid)
							{
								if (PInvoke.ControlService(instanceHandle, (uint)PInvoke.SERVICE_CONTROL_STOP, out SERVICE_STATUS _))
								{
									stoppedAny = true;
								}
								else
								{
									int error = Marshal.GetLastWin32Error();
									if (error == 1062 || error == 1061 || error == 1052) stoppedAny = true;
								}
							}
						}
					}
					if (stoppedAny) return;
				}
			}
		}

		using CloseServiceHandleSafeHandle directServiceHandle = PInvoke.OpenService(scmHandle, baseServiceName, (uint)PInvoke.SERVICE_STOP);
		if (directServiceHandle.IsInvalid)
		{
			int error = Marshal.GetLastWin32Error();
			if (error == 1060) return;
			throw new Win32Exception(error, "OpenService failed");
		}

		if (!PInvoke.ControlService(directServiceHandle, (uint)PInvoke.SERVICE_CONTROL_STOP, out SERVICE_STATUS _))
		{
			int error = Marshal.GetLastWin32Error();
			if (error == 1062 || error == 1061 || error == 1052) return;
			throw new Win32Exception(error, "ControlService failed");
		}
	}

	public static void SetStartupType(string serviceName, SERVICE_START_TYPE startType)
	{
		using CloseServiceHandleSafeHandle scmHandle = PInvoke.OpenSCManager(null, null, (uint)PInvoke.SC_MANAGER_CONNECT);
		if (scmHandle.IsInvalid)
			throw new Win32Exception(Marshal.GetLastWin32Error());

		using CloseServiceHandleSafeHandle serviceHandle = PInvoke.OpenService(scmHandle, serviceName, (uint)PInvoke.SERVICE_CHANGE_CONFIG);
		if (serviceHandle.IsInvalid)
		{
			int error = Marshal.GetLastWin32Error();
			if (error == 1060) return;
			throw new Win32Exception(error);
		}

		unsafe
		{
			if (!PInvoke.ChangeServiceConfig(
				(SC_HANDLE)serviceHandle.DangerousGetHandle(),
				(ENUM_SERVICE_TYPE)0xFFFFFFFF,
				startType,
				(SERVICE_ERROR)0xFFFFFFFF,
				null,
				null,
				(uint*)null,
				null,
				null,
				null,
				null))
			{
				throw new Win32Exception(Marshal.GetLastWin32Error());
			}
		}
	}

	public unsafe static void CreateService(string serviceName, string path)
	{
		using CloseServiceHandleSafeHandle scmHandle = PInvoke.OpenSCManager(null, null, (uint)PInvoke.SC_MANAGER_CREATE_SERVICE);
		if (scmHandle.IsInvalid) throw new Win32Exception(Marshal.GetLastWin32Error(), "OpenSCManager failed");

		fixed (char* pszServiceName = serviceName)
		fixed (char* pszBinPath = path)
		{
			SC_HANDLE rawScmHandle = (SC_HANDLE)scmHandle.DangerousGetHandle();
			SC_HANDLE serviceHandleRaw = PInvoke.CreateService(
				rawScmHandle,
				pszServiceName,
				pszServiceName,
				(uint)PInvoke.SERVICE_ALL_ACCESS,
				ENUM_SERVICE_TYPE.SERVICE_WIN32_OWN_PROCESS,
				SERVICE_START_TYPE.SERVICE_AUTO_START,
				SERVICE_ERROR.SERVICE_ERROR_NORMAL,
				pszBinPath,
				null,
				null,
				null,
				null,
				null);
		}
	}
}
