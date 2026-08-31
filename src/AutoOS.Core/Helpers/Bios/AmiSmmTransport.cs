using System.Buffers.Binary;
using System.Runtime.InteropServices;
using AutoOS.Core.Data.Enums.Bios;
using Microsoft.Win32.SafeHandles;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Memory;
using Windows.Win32.System.Services;

namespace AutoOS.Core.Helpers.Bios;

public sealed partial class AmiSmmTransport : IDisposable
{
	private const string SERVICE_NAME = "GENERICDRV";

	private const string DEVICE_NAME = @"\\.\genericdrv";

	private const uint IOCTL_SMM_INIT = 0xFA002F34;

	private const uint IOCTL_SMI_SEND = 0xFA002F1C;

	private const ushort SMI_PORT = 0xB2;

	private const byte SMI_CMD = 0xEF;

	private const uint COMM_BUFFER_SIZE = 0x11000;

	private const uint CHUNK_SIZE = 0xFC4;

	private const uint HDR_OFF = 0xFC4;

	private const uint PARAM_OFF = 0xFF4;

	private const uint SERVICE_STOPPED = 1;

	private const uint SERVICE_RUNNING = 4;

	private const uint SC_MANAGER_ALL_ACCESS_VALUE = 0xF003F;

	private const uint MAX_VARIABLE_SIZE = 0x20000;

	private const uint DEFAULT_GET_VARIABLE_SIZE = 0x4000;

	private const uint UNLOCK_VARIABLE_ATTRIBUTES = 7;

	private static readonly string PrimaryDriverPath = Path.Combine(AppContext.BaseDirectory, "Drivers", "Ami", "amifldrv64.sys");

	private static readonly string FallbackDriverPath = Path.Combine(AppContext.BaseDirectory, "Drivers", "Ami", "amigendrv64.sys");

	private static readonly NotifyCallbackDelegate NotifyCallbackInstance = CallbackImpl;

	private readonly byte[] _smmHeader = new byte[24];

	private SafeFileHandle? _deviceHandle;

	private HANDLE _handle;

	private ulong _commPhys;

	private ulong _commVirt;

	private ulong _mailboxPhys;

	private ulong _mailboxVirt;

	private nint _bv;

	private nint _mbv;

	private bool _disposed;

	public string? LastLoadError { get; private set; }

	public string? LastInitError { get; private set; }

	public bool TryLoad()
	{
		LastLoadError = null;

		if (TryLoadWithPath(PrimaryDriverPath))
			return true;

		string primaryError = LastLoadError ?? "Unknown error";

		if (TryLoadWithPath(FallbackDriverPath))
			return true;

		string fallbackError = LastLoadError ?? "Unknown error";
		LastLoadError = $"Primary ({Path.GetFileName(PrimaryDriverPath)}): {primaryError} | Fallback ({Path.GetFileName(FallbackDriverPath)}): {fallbackError}";
		return false;
	}

	public bool TryLoadAndInit()
	{
		LastLoadError = null;
		LastInitError = null;

		string? primaryLoadError = null;
		string? primaryInitError = null;

		if (TryLoadWithPath(PrimaryDriverPath))
		{
			if (TryInitSmm())
				return true;

			primaryInitError = LastInitError;
			Unload();
		}
		else
		{
			primaryLoadError = LastLoadError;
		}

		string? fallbackLoadError = null;
		string? fallbackInitError = null;

		if (TryLoadWithPath(FallbackDriverPath))
		{
			if (TryInitSmm())
				return true;

			fallbackInitError = LastInitError;
			Unload();
		}
		else
		{
			fallbackLoadError = LastLoadError;
		}

		if (primaryLoadError != null && fallbackLoadError != null)
			LastLoadError = $"Primary ({Path.GetFileName(PrimaryDriverPath)}): {primaryLoadError} | Fallback ({Path.GetFileName(FallbackDriverPath)}): {fallbackLoadError}";
		else if (primaryInitError != null && fallbackInitError != null)
			LastLoadError = $"Primary init ({Path.GetFileName(PrimaryDriverPath)}): {primaryInitError} | Fallback init ({Path.GetFileName(FallbackDriverPath)}): {fallbackInitError}";
		else if (primaryInitError != null && fallbackLoadError != null)
			LastLoadError = $"Primary init ({Path.GetFileName(PrimaryDriverPath)}): {primaryInitError} | Fallback load ({Path.GetFileName(FallbackDriverPath)}): {fallbackLoadError}";
		else if (primaryLoadError != null && fallbackInitError != null)
			LastLoadError = $"Primary load ({Path.GetFileName(PrimaryDriverPath)}): {primaryLoadError} | Fallback init ({Path.GetFileName(FallbackDriverPath)}): {fallbackInitError}";
		else
			LastLoadError = primaryInitError ?? primaryLoadError ?? fallbackInitError ?? fallbackLoadError;

		return false;
	}

	public void Unload()
	{
		if (_deviceHandle != null)
		{
			_deviceHandle.Dispose();
			_deviceHandle = null;
			_handle = HANDLE.Null;
		}
		else if (!_handle.IsNull)
		{
			PInvoke.CloseHandle(_handle);
			_handle = HANDLE.Null;
		}

		using CloseServiceHandleSafeHandle scm = PInvoke.OpenSCManager(null, null, SC_MANAGER_ALL_ACCESS_VALUE);
		if (scm.IsInvalid)
			return;

		using CloseServiceHandleSafeHandle svc = PInvoke.OpenService(scm, SERVICE_NAME, PInvoke.SERVICE_ALL_ACCESS);
		if (svc.IsInvalid)
			return;

		PInvoke.ControlService(svc, PInvoke.SERVICE_CONTROL_STOP, out SERVICE_STATUS _);
		WaitService(svc, SERVICE_STOPPED);
		PInvoke.DeleteService(svc);
	}

	public unsafe bool TryInitSmm()
	{
		LastInitError = null;

		if (_handle.IsNull && _deviceHandle == null)
		{
			LastInitError = "Device not opened (TryLoad failed)";
			return false;
		}

		byte* inBuf = stackalloc byte[62];
		byte* outBuf = stackalloc byte[62];
		new Span<byte>(inBuf, 62).Clear();
		new Span<byte>(outBuf, 62).Clear();

		*(ushort*)inBuf = SMI_PORT;
		*(uint*)(inBuf + 2) = COMM_BUFFER_SIZE;

		uint bytesReturned = 0;
		BOOL ok = PInvoke.DeviceIoControl(_handle, IOCTL_SMM_INIT, inBuf, 62, outBuf, 62, &bytesReturned, null);
		if (ok == 0)
		{
			int err = Marshal.GetLastWin32Error();
			LastInitError = $"IOCTL_SMM_INIT (0x{IOCTL_SMM_INIT:X}) failed: {new System.ComponentModel.Win32Exception(err).Message} (0x{err:X})";
			return false;
		}

		_commPhys = *(ulong*)(outBuf + 6);
		_commVirt = *(ulong*)(outBuf + 0x0E);
		_mailboxPhys = *(ulong*)(outBuf + 0x16);
		_mailboxVirt = *(ulong*)(outBuf + 0x1E);
		new Span<byte>(outBuf + 0x26, 24).CopyTo(_smmHeader);
		_bv = (nint)_commVirt;
		_mbv = (nint)_mailboxVirt;

		MEMORY_BASIC_INFORMATION mbi = default;
		bool queried = PInvoke.VirtualQuery((void*)_bv, &mbi, (nuint)sizeof(MEMORY_BASIC_INFORMATION)) != 0;
		if (!queried)
		{
			int err = Marshal.GetLastWin32Error();
			LastInitError = $"VirtualQuery failed for comm buffer 0x{_commVirt:X}: {new System.ComponentModel.Win32Exception(err).Message} (0x{err:X})";
			return false;
		}

		if (mbi.State != VIRTUAL_ALLOCATION_TYPE.MEM_COMMIT)
		{
			LastInitError = $"Comm buffer not committed (State=0x{(uint)mbi.State:X}, expected MEM_COMMIT 0x1000) — driver may not have mapped buffer (GENERICDRV required)";
			return false;
		}

		return true;
	}

	public unsafe bool TryGetVariable(string name, Guid guid, out byte[]? data, out uint attributes, out uint status, uint maxSize = DEFAULT_GET_VARIABLE_SIZE)
	{
		data = null;
		attributes = 0;
		status = 0;

		if (_bv == 0 || _mbv == 0)
			return false;

		Span<byte> wire = stackalloc byte[16];
		if (!guid.TryWriteBytes(wire))
			return false;

		ReadOnlySpan<char> nameChars = name.AsSpan();
		int nameUtf16Len = (nameChars.Length + 1) * 2;
		uint nameOffset = 0x50;
		uint namePhys = (uint)(_commPhys + nameOffset);
		uint dataOffset = (nameOffset + (uint)nameUtf16Len + 7) & ~7u;
		uint dataPhys = (uint)(_commPhys + dataOffset);

		if (dataOffset >= COMM_BUFFER_SIZE)
			return false;

		uint capacity = maxSize;
		while (capacity <= MAX_VARIABLE_SIZE)
		{
			if (dataOffset + capacity > COMM_BUFFER_SIZE)
				return false;

			new Span<byte>((void*)_bv, (int)COMM_BUFFER_SIZE).Clear();

			int msgSize = (int)(dataOffset + capacity);
			byte[] msg = System.Buffers.ArrayPool<byte>.Shared.Rent(msgSize);
			try
			{
				Span<byte> msgSpan = msg.AsSpan(0, msgSize);
				msgSpan.Clear();
				BinaryPrimitives.WriteUInt32LittleEndian(msgSpan, 0x100);
				BinaryPrimitives.WriteUInt32LittleEndian(msgSpan[0x04..], (uint)(_commPhys + 0x30));
				BinaryPrimitives.WriteUInt32LittleEndian(msgSpan[0x18..], 1);
				wire.CopyTo(msgSpan[0x30..0x40]);
				BinaryPrimitives.WriteUInt32LittleEndian(msgSpan[0x40..], namePhys);
				BinaryPrimitives.WriteUInt32LittleEndian(msgSpan[0x44..], 0);
				BinaryPrimitives.WriteUInt32LittleEndian(msgSpan[0x48..], capacity);
				BinaryPrimitives.WriteUInt32LittleEndian(msgSpan[0x4C..], dataPhys);
				WriteNameUtf16(msgSpan, (int)nameOffset, nameChars);

				Marshal.Copy(msg, 0, _bv, msgSize);
			}
			finally
			{
				System.Buffers.ArrayPool<byte>.Shared.Return(msg);
			}

			WriteMailbox();
			SendSmi((uint)_commPhys);

			byte* retPtr = (byte*)_bv;
			uint dtype = *(uint*)retPtr;
			attributes = *(uint*)(retPtr + 0x44);
			uint retSize = *(uint*)(retPtr + 0x48);
			status = dtype;

			if (dtype == 0)
			{
				data = new byte[retSize];
				Marshal.Copy(_bv + (int)dataOffset, data, 0, (int)retSize);
				return true;
			}

			if (dtype != (uint)EfiStatus.EFI_BUFFER_TOO_SMALL && dtype != (uint)AmiSmmStatus.BufferTooSmallWire && dtype != 0x06)
				return false;

			if (capacity >= MAX_VARIABLE_SIZE)
				return false;

			uint required = retSize > capacity ? retSize : 0;
			uint doubled = Math.Min(capacity * 2, MAX_VARIABLE_SIZE);
			capacity = required != 0 ? Math.Clamp(required, doubled, MAX_VARIABLE_SIZE) : doubled;
			if (required != 0 && required > doubled)
				capacity = Math.Min(required, MAX_VARIABLE_SIZE);
		}

		return false;
	}

	public unsafe bool TrySetVariable(string name, Guid guid, uint attributes, ReadOnlySpan<byte> dataBlob, out uint status)
	{
		status = 0;

		if (_bv == 0 || _mbv == 0)
			return false;

		byte[] guidWire = guid.ToByteArray();
		Span<byte> wire = stackalloc byte[16];
		guidWire.AsSpan().CopyTo(wire);

		ReadOnlySpan<char> nameChars = name.AsSpan();
		int nameUtf16Len = (nameChars.Length + 1) * 2;
		uint nameOffset = 0x50;
		uint namePhys = (uint)(_commPhys + nameOffset);
		uint dataOffset = (nameOffset + (uint)nameUtf16Len + 7) & ~7u;
		uint dataPhys = (uint)(_commPhys + dataOffset);
		uint dataSize = (uint)dataBlob.Length;

		if (dataOffset + dataSize > COMM_BUFFER_SIZE)
			return false;

		new Span<byte>((void*)_bv, (int)COMM_BUFFER_SIZE).Clear();

		int msgSize = (int)(dataOffset + dataSize);
		byte[] msg = System.Buffers.ArrayPool<byte>.Shared.Rent(msgSize);
		try
		{
			Span<byte> msgSpan = msg.AsSpan(0, msgSize);
			msgSpan.Clear();
			BinaryPrimitives.WriteUInt32LittleEndian(msgSpan, 0x300);
			BinaryPrimitives.WriteUInt32LittleEndian(msgSpan[0x04..], (uint)(_commPhys + 0x30));
			BinaryPrimitives.WriteUInt32LittleEndian(msgSpan[0x18..], 1);
			wire.CopyTo(msgSpan[0x30..0x40]);
			BinaryPrimitives.WriteUInt32LittleEndian(msgSpan[0x40..], namePhys);
			BinaryPrimitives.WriteUInt32LittleEndian(msgSpan[0x44..], attributes);
			BinaryPrimitives.WriteUInt32LittleEndian(msgSpan[0x48..], dataSize);
			BinaryPrimitives.WriteUInt32LittleEndian(msgSpan[0x4C..], dataPhys);
			WriteNameUtf16(msgSpan, (int)nameOffset, nameChars);
			dataBlob.CopyTo(msgSpan[(int)dataOffset..]);

			Marshal.Copy(msg, 0, _bv, msgSize);
		}
		finally
		{
			System.Buffers.ArrayPool<byte>.Shared.Return(msg);
		}

		WriteMailbox();
		SendSmi((uint)_commPhys);

		byte* retPtr = (byte*)_bv;
		uint dtype = *(uint*)retPtr;
		status = dtype;

		return dtype == 0;
	}

	public bool TryUnlockWithPassword(string password, out uint status)
	{
		status = 0;
		Guid unlockGuid = new("5855CE1B-FB8E-47E4-BC1A-39ECAA0C96CF");
		byte[] pwdData = System.Text.Encoding.Unicode.GetBytes(password);

		if (TrySetVariable("$SETUPPASSWD", unlockGuid, UNLOCK_VARIABLE_ATTRIBUTES, pwdData, out status))
			return true;

		if (status == (uint)AmiSmmStatus.PasswordUnlockSuccess)
			return true;

		return false;
	}

	public unsafe bool TryPhysRead(ulong phys, uint size, Span<byte> output)
	{
		if (_bv == 0 || _mbv == 0 || output.Length < size)
			return false;

		ulong cur = phys;
		uint remaining = size;
		int offset = 0;

		while (remaining > 0)
		{
			uint chunk = Math.Min(remaining, CHUNK_SIZE);
			new Span<byte>((void*)_bv, 0x1000).Clear();
			byte* cp = (byte*)_bv;
			uint* mb = (uint*)_mbv;

			*(uint*)(cp + HDR_OFF) = 0x500;
			*(uint*)(cp + HDR_OFF + 4) = (uint)(_commPhys + PARAM_OFF);
			*(uint*)(cp + PARAM_OFF + 0) = (uint)cur;
			*(uint*)(cp + PARAM_OFF + 4) = (uint)_commPhys;
			*(uint*)(cp + PARAM_OFF + 8) = chunk;
			mb[4] = 0xC0000004;

			SendSmi((uint)(_commPhys + HDR_OFF));

			new ReadOnlySpan<byte>((void*)_bv, (int)chunk).CopyTo(output.Slice(offset, (int)chunk));

			offset += (int)chunk;
			cur += chunk;
			remaining -= chunk;
		}

		return true;
	}

	public byte[]? PhysRead(ulong phys, uint size)
	{
		byte[] output = new byte[size];
		if (!TryPhysRead(phys, size, output))
			return null;

		return output;
	}

	private unsafe bool TryLoadWithPath(string fullPath)
	{
		if (!File.Exists(fullPath))
		{
			LastLoadError = $"Driver file not found: {fullPath}";
			return false;
		}

		using (CloseServiceHandleSafeHandle scm = PInvoke.OpenSCManager(null, null, SC_MANAGER_ALL_ACCESS_VALUE))
		{
			if (scm.IsInvalid)
			{
				int err = Marshal.GetLastWin32Error();
				LastLoadError = $"OpenSCManager failed: {new System.ComponentModel.Win32Exception(err).Message} (0x{err:X})";
				return false;
			}

			using (CloseServiceHandleSafeHandle svc = PInvoke.OpenService(scm, SERVICE_NAME, PInvoke.SERVICE_ALL_ACCESS))
			{
				if (!svc.IsInvalid)
				{
					PInvoke.ControlService(svc, PInvoke.SERVICE_CONTROL_STOP, out SERVICE_STATUS _);
					WaitService(svc, SERVICE_STOPPED);
				}
			}

			using (CloseServiceHandleSafeHandle svc = PInvoke.OpenService(scm, SERVICE_NAME, PInvoke.SERVICE_ALL_ACCESS))
			{
				if (!svc.IsInvalid)
				{
					if (PInvoke.StartService(svc, null) == 0)
					{
						int err = Marshal.GetLastWin32Error();
						if (!WaitService(svc, SERVICE_RUNNING))
							LastLoadError = $"StartService {SERVICE_NAME} failed: {new System.ComponentModel.Win32Exception(err).Message} (0x{err:X})";
					}
					else
					{
						WaitService(svc, SERVICE_RUNNING);
					}
				}
				else
				{
					int openErr = Marshal.GetLastWin32Error();
					fixed (char* pName = SERVICE_NAME)
					fixed (char* pPath = fullPath)
					{
						var rawScm = (SC_HANDLE)scm.DangerousGetHandle();
						SC_HANDLE created = PInvoke.CreateService(
							rawScm,
							pName,
							pName,
							PInvoke.SERVICE_ALL_ACCESS,
							ENUM_SERVICE_TYPE.SERVICE_KERNEL_DRIVER,
							SERVICE_START_TYPE.SERVICE_DEMAND_START,
							SERVICE_ERROR.SERVICE_ERROR_NORMAL,
							pPath,
							null,
							null,
							null,
							null,
							null);
						if (created.Value != null && created.Value != (void*)-1)
						{
							using CloseServiceHandleSafeHandle createdSafe = new(created, true);
							if (PInvoke.StartService(createdSafe, null) == 0)
							{
								int err = Marshal.GetLastWin32Error();
								if (!WaitService(createdSafe, SERVICE_RUNNING))
									LastLoadError = $"CreateService+StartService {SERVICE_NAME} -> {Path.GetFileName(fullPath)} failed: {new System.ComponentModel.Win32Exception(err).Message} (0x{err:X})";
							}
							else
							{
								WaitService(createdSafe, SERVICE_RUNNING);
							}
						}
						else
						{
							int err = Marshal.GetLastWin32Error();
							LastLoadError = $"CreateService {SERVICE_NAME} failed for {Path.GetFileName(fullPath)}: {new System.ComponentModel.Win32Exception(err).Message} (0x{err:X}) (open error 0x{openErr:X})";
							return false;
						}
					}
				}
			}
		}

		try
		{
			SafeFileHandle handle = File.OpenHandle(DEVICE_NAME, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
			if (handle.IsInvalid)
			{
				int err = Marshal.GetLastWin32Error();
				LastLoadError = $"CreateFile {DEVICE_NAME} failed after service start: {new System.ComponentModel.Win32Exception(err).Message} (0x{err:X}) — driver may be blocked or requires reboot";
				return false;
			}

			_deviceHandle = handle;
			_handle = new HANDLE(handle.DangerousGetHandle());
			LastLoadError = null;
			return true;
		}
		catch (Exception ex)
		{
			LastLoadError = $"CreateFile {DEVICE_NAME} exception: {ex.Message} — driver {Path.GetFileName(fullPath)} at {fullPath}";
			return false;
		}
	}

	private delegate void NotifyCallbackDelegate(nint pParameter);

	private static void CallbackImpl(nint p) => _ = PInvoke.SetEvent(new HANDLE(p));

	private unsafe void WriteMailbox()
	{
		Span<byte> mailbox = stackalloc byte[0x50];
		_smmHeader.AsSpan().CopyTo(mailbox[..24]);
		BinaryPrimitives.WriteUInt32LittleEndian(mailbox[0x10..], 0);
		BinaryPrimitives.WriteUInt32LittleEndian(mailbox[0x18..], SMI_CMD);
		BinaryPrimitives.WriteUInt64LittleEndian(mailbox[0x40..], _commPhys);
		fixed (byte* pMailbox = mailbox)
		{
			Buffer.MemoryCopy(pMailbox, (void*)_mbv, 0x50, 0x50);
		}
	}

	private unsafe void SendSmi(uint commPhys)
	{
		byte* ib = stackalloc byte[38];
		new Span<byte>(ib, 38).Clear();
		*(uint*)ib = SMI_PORT;
		ib[4] = SMI_CMD;
		*(uint*)(ib + 10 + 4) = commPhys;

		uint bytesReturned = 0;
		PInvoke.DeviceIoControl(_handle, IOCTL_SMI_SEND, ib, 38, ib, 38, &bytesReturned, null);
	}

	private static void WriteNameUtf16(Span<byte> dest, int offset, ReadOnlySpan<char> nameChars)
	{
		for (int i = 0; i < nameChars.Length; i++)
		{
			dest[offset + i * 2] = (byte)nameChars[i];
			dest[offset + i * 2 + 1] = (byte)(nameChars[i] >> 8);
		}
	}

	private unsafe bool WaitService(CloseServiceHandleSafeHandle svc, uint desiredState)
	{
		SERVICE_STATUS status = new();
		var handle = (SC_HANDLE)svc.DangerousGetHandle();
		if (PInvoke.QueryServiceStatus(handle, &status) != 0 && (uint)status.dwCurrentState == desiredState)
			return true;

		SafeFileHandle hEvent = PInvoke.CreateEvent(null, true, false, null);
		if (hEvent.IsInvalid)
			return false;

		try
		{
			void* callbackPtr = (void*)Marshal.GetFunctionPointerForDelegate(NotifyCallbackInstance);
			SERVICE_NOTIFY_2W notify = new()
			{
				dwVersion = 2,
				pfnNotifyCallback = (delegate* unmanaged[Stdcall]<void*, void>)callbackPtr,
				pContext = (void*)hEvent.DangerousGetHandle()
			};

			fixed (char* pName = SERVICE_NAME)
			{
				notify.pszServiceNames = pName;

				var notifyMask = (SERVICE_NOTIFY)2;
				uint result = PInvoke.NotifyServiceStatusChange(svc, notifyMask, &notify);
				if (result != 0)
					return false;

				PInvoke.WaitForSingleObject((HANDLE)hEvent.DangerousGetHandle(), 10000);

				if (PInvoke.QueryServiceStatus(handle, &status) != 0 && (uint)status.dwCurrentState == desiredState)
					return true;

				return false;
			}
		}
		finally
		{
			hEvent.Dispose();
			GC.KeepAlive(NotifyCallbackInstance);
		}
	}

	public void Dispose()
	{
		if (_disposed)
			return;

		Unload();
		_disposed = true;
		GC.SuppressFinalize(this);
	}
}
