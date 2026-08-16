using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Windows.Win32.Foundation;
using Windows.Win32.Media.Audio;

namespace AutoOS.Core.Data.Clients.Sound;

internal unsafe class DeviceNotificationClient : IDisposable
{
	private static readonly DeviceNotificationVtbl* _vtbl;
	private readonly void* _instance;
	private readonly Action _onNotify;
	private readonly IMMDeviceEnumerator* _enumerator;

	static DeviceNotificationClient()
	{
		_vtbl = (DeviceNotificationVtbl*)RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(DeviceNotificationClient), sizeof(DeviceNotificationVtbl));
		_vtbl->QueryInterface = &QueryInterface;
		_vtbl->AddRef = &AddRef;
		_vtbl->Release = &Release;
		_vtbl->OnStateChanged = &OnStateChanged;
		_vtbl->OnAdded = &OnAdded;
		_vtbl->OnRemoved = &OnRemoved;
		_vtbl->OnDefaultChanged = &OnDefaultChanged;
		_vtbl->OnPropChanged = &OnPropChanged;
	}

	public DeviceNotificationClient(Action onNotify, IMMDeviceEnumerator* enumerator)
	{
		_onNotify = onNotify;
		_enumerator = enumerator;
		_instance = (void*)Marshal.AllocHGlobal(sizeof(IntPtr) + sizeof(IntPtr));
		*(IntPtr*)_instance = (IntPtr)_vtbl;
		*(IntPtr*)((byte*)_instance + sizeof(IntPtr)) = (IntPtr)GCHandle.ToIntPtr(GCHandle.Alloc(this));
	}

	public void* GetComPointer() => _instance;

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
	private static int QueryInterface(IntPtr self, Guid* riid, IntPtr* ppv)
	{
		if (riid->Equals(new Guid("7991222B-0258-4425-9614-D44351C1AF50")))
		{
			*ppv = self;
			return 0;
		}
		*ppv = IntPtr.Zero;
		return unchecked((int)0x80004002);
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
	private static uint AddRef(IntPtr self) => 1;

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
	private static uint Release(IntPtr self) => 1;

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
	private static int OnStateChanged(IntPtr self, char* id, uint state)
	{
		Invoke(self);
		return 0;
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
	private static int OnAdded(IntPtr self, char* id)
	{
		Invoke(self);
		return 0;
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
	private static int OnRemoved(IntPtr self, char* id)
	{
		Invoke(self);
		return 0;
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
	private static int OnDefaultChanged(IntPtr self, EDataFlow f, ERole r, char* id)
	{
		Invoke(self);
		return 0;
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
	private static int OnPropChanged(IntPtr self, char* id, PROPERTYKEY k) => 0;

	private static void Invoke(IntPtr self) => ((DeviceNotificationClient)GCHandle.FromIntPtr(*(IntPtr*)((byte*)self + sizeof(IntPtr))).Target!)._onNotify?.Invoke();

	public void Dispose()
	{
		_enumerator->UnregisterEndpointNotificationCallback((IMMNotificationClient*)_instance);
		_enumerator->Release();
		GCHandle.FromIntPtr(*(IntPtr*)((byte*)_instance + sizeof(IntPtr))).Free();
		Marshal.FreeHGlobal((IntPtr)_instance);
	}

	[StructLayout(LayoutKind.Sequential)]
	struct DeviceNotificationVtbl
	{
		public delegate* unmanaged[Stdcall]<IntPtr, Guid*, IntPtr*, int> QueryInterface;
		public delegate* unmanaged[Stdcall]<IntPtr, uint> AddRef;
		public delegate* unmanaged[Stdcall]<IntPtr, uint> Release;
		public delegate* unmanaged[Stdcall]<IntPtr, char*, uint, int> OnStateChanged;
		public delegate* unmanaged[Stdcall]<IntPtr, char*, int> OnAdded;
		public delegate* unmanaged[Stdcall]<IntPtr, char*, int> OnRemoved;
		public delegate* unmanaged[Stdcall]<IntPtr, EDataFlow, ERole, char*, int> OnDefaultChanged;
		public delegate* unmanaged[Stdcall]<IntPtr, char*, PROPERTYKEY, int> OnPropChanged;
	}
}
