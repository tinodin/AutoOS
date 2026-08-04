using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;
using Windows.Wdk.System.Threading;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.RestartManager;
using Windows.Win32.System.Threading;

namespace AutoOS.Core.Helpers.Processes;

public static partial class ProcessesHelper
{
	public static unsafe string GetCommandLine(Process proc)
	{
		HANDLE handle = PInvoke.OpenProcess(PROCESS_ACCESS_RIGHTS.PROCESS_QUERY_INFORMATION | PROCESS_ACCESS_RIGHTS.PROCESS_VM_READ, false, (uint)proc.Id);

		if ((IntPtr)handle.Value == IntPtr.Zero)
			return string.Empty;

		try
		{
			PROCESS_BASIC_INFORMATION pbi = new();
			uint returnLength;
			nuint bytesRead;

			NTSTATUS status = Windows.Wdk.PInvoke.NtQueryInformationProcess(handle, PROCESSINFOCLASS.ProcessBasicInformation, &pbi, (uint)sizeof(PROCESS_BASIC_INFORMATION), &returnLength);

			if (status.Value != 0) return string.Empty;

			IntPtr pebAddress = (IntPtr)pbi.PebBaseAddress;
			if (pebAddress == IntPtr.Zero) return string.Empty;

			IntPtr processParametersOffset = pebAddress + (IntPtr.Size == 8 ? 0x20 : 0x10);
			IntPtr processParametersPtr = IntPtr.Zero;

			if (!PInvoke.ReadProcessMemory(handle, (void*)processParametersOffset, &processParametersPtr, (uint)IntPtr.Size, &bytesRead))
				return string.Empty;

			IntPtr commandLineUnicodeStringPtr = processParametersPtr + (IntPtr.Size == 8 ? 0x70 : 0x40);

			byte[] unicodeStringHeader = new byte[16];
			fixed (byte* pHeader = unicodeStringHeader)
			{
				if (!PInvoke.ReadProcessMemory(handle, (void*)commandLineUnicodeStringPtr, pHeader, (uint)(IntPtr.Size == 8 ? 16 : 8), &bytesRead))
					return string.Empty;
			}

			ushort len = BitConverter.ToUInt16(unicodeStringHeader, 0);
			IntPtr bufferPtr = (IntPtr.Size == 8) ? (IntPtr)BitConverter.ToInt64(unicodeStringHeader, 8) : (IntPtr)BitConverter.ToInt32(unicodeStringHeader, 4);

			if (len == 0 || bufferPtr == IntPtr.Zero) return string.Empty;

			byte[] commandLineBuffer = new byte[len];
			fixed (byte* pCmd = commandLineBuffer)
			{
				if (!PInvoke.ReadProcessMemory(handle, (void*)bufferPtr, pCmd, len, &bytesRead))
					return string.Empty;
			}

			return Encoding.Unicode.GetString(commandLineBuffer);
		}
		finally
		{
			PInvoke.CloseHandle(handle);
		}
	}

	public static unsafe string GetProcessPath(Process proc)
	{
		HANDLE handle = PInvoke.OpenProcess(PROCESS_ACCESS_RIGHTS.PROCESS_QUERY_LIMITED_INFORMATION, false, (uint)proc.Id);

		if ((IntPtr)handle.Value == IntPtr.Zero)
			return string.Empty;

		using var safeHandle = new SafeProcessHandle((IntPtr)handle.Value, true);

		uint bufferSize = 256;
		while (true)
		{
			char[] buffer = new char[bufferSize];
			uint size = bufferSize;
			if (PInvoke.QueryFullProcessImageName(safeHandle, PROCESS_NAME_FORMAT.PROCESS_NAME_WIN32, buffer, ref size))
			{
				return new string(buffer, 0, (int)size);
			}

			if (Marshal.GetLastWin32Error() == 122)
			{
				bufferSize *= 2;
				if (bufferSize > 32768)
					return string.Empty;
			}
			else
			{
				return string.Empty;
			}
		}
	}

	public static unsafe IEnumerable<Process> GetLockingProcesses(string path)
	{
		var results = new List<Process>();
		var pathsToCheck = new List<string>();

		if (Directory.Exists(path))
		{
			pathsToCheck.AddRange(Directory.GetFiles(path, "*.*", SearchOption.AllDirectories));
		}
		else if (File.Exists(path))
		{
			pathsToCheck.Add(path);
		}

		char* sessionKey = stackalloc char[257];
		PCWSTR* pathsPointer = stackalloc PCWSTR[1];

		foreach (string filePath in pathsToCheck)
		{
			uint sessionHandle = 0;
			WIN32_ERROR result = PInvoke.RmStartSession(&sessionHandle, 0, sessionKey);
			if (result != WIN32_ERROR.ERROR_SUCCESS) continue;

			try
			{
				fixed (char* pathPointer = filePath)
				{
					pathsPointer[0] = new PCWSTR(pathPointer);
					result = PInvoke.RmRegisterResources(sessionHandle, 1, pathsPointer, 0, null, 0, null);
				}
				if (result != WIN32_ERROR.ERROR_SUCCESS) continue;

				uint processInfoCount = 0;
				result = PInvoke.RmGetList(sessionHandle, out uint processInfoNeeded, ref processInfoCount, default, out uint rebootReasons);
				if (result != WIN32_ERROR.ERROR_MORE_DATA || processInfoNeeded == 0) continue;

				var processInfoBuffer = new RM_PROCESS_INFO[processInfoNeeded];
				result = PInvoke.RmGetList(sessionHandle, out processInfoNeeded, ref processInfoCount, processInfoBuffer, out rebootReasons);

				if (result == WIN32_ERROR.ERROR_SUCCESS)
				{
					for (int index = 0; index < processInfoCount; index++)
					{
						try
						{
							results.Add(Process.GetProcessById((int)processInfoBuffer[index].Process.dwProcessId));
						}
						catch
						{	}
					}
				}
			}
			finally
			{
				PInvoke.RmEndSession(sessionHandle);
			}
		}

		return results.DistinctBy(process => process.Id);
	}
}
