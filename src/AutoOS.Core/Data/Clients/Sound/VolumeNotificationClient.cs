using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Windows.Win32.Foundation;
using Windows.Win32.Media.Audio;
using Windows.Win32.Media.Audio.Endpoints;

namespace AutoOS.Core.Data.Clients.Sound;

internal unsafe class VolumeNotificationClient : IDisposable
{
	private static readonly VolumeCallbackVtbl* _vtbl;
	private readonly void* _instance;
	private readonly IAudioEndpointVolume* _volume;
	private readonly IMMDevice* _endpoint;
	private readonly Action<float, bool, float, float> _onNotify;

	static VolumeNotificationClient()
	{
		_vtbl = (VolumeCallbackVtbl*)RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(VolumeNotificationClient), sizeof(VolumeCallbackVtbl));
		_vtbl->QueryInterface = &QueryInterface;
		_vtbl->AddRef = &AddRef;
		_vtbl->Release = &Release;
		_vtbl->OnNotify = &OnNotify;
	}

	public VolumeNotificationClient(IAudioEndpointVolume* volume, IMMDevice* endpoint, Action<float, bool, float, float> onNotify)
	{
		_volume = volume;
		_endpoint = endpoint;
		_onNotify = onNotify;
		_instance = (void*)Marshal.AllocHGlobal(sizeof(IntPtr) + sizeof(IntPtr));
		*(IntPtr*)_instance = (IntPtr)_vtbl;
		*(IntPtr*)((byte*)_instance + sizeof(IntPtr)) = (IntPtr)GCHandle.ToIntPtr(GCHandle.Alloc(this));
	}

	public void* GetComPointer() => _instance;

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
	private static int QueryInterface(IntPtr self, Guid* riid, IntPtr* ppv)
	{
		if (riid->Equals(new Guid("657804FA-D6AD-4496-8A60-352752AF4F89")))
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
	private static int OnNotify(IntPtr self, IntPtr pNotify)
	{
		var client = (VolumeNotificationClient)GCHandle.FromIntPtr(*(IntPtr*)((byte*)self + sizeof(IntPtr))).Target!;
		client._volume->GetMasterVolumeLevelScalar(out float v);
		client._volume->GetMute(out BOOL m);
		client._volume->GetChannelCount(out uint c);
		float l = 0, r = 0;
		if (c >= 1) client._volume->GetChannelVolumeLevelScalar(0, out l);
		if (c >= 2) client._volume->GetChannelVolumeLevelScalar(1, out r);
		client._onNotify?.Invoke(MathF.Round(v * 100f), (bool)m, MathF.Round(l * 100f), MathF.Round(r * 100f));
		return 0;
	}

	public void Dispose()
	{
		try
		{
			_volume->UnregisterControlChangeNotify((IAudioEndpointVolumeCallback*)_instance);
		}
		catch { }
		_volume->Release();
		_endpoint->Release();
		GCHandle.FromIntPtr(*(IntPtr*)((byte*)_instance + sizeof(IntPtr))).Free();
		Marshal.FreeHGlobal((IntPtr)_instance);
	}

	[StructLayout(LayoutKind.Sequential)]
	struct VolumeCallbackVtbl
	{
		public delegate* unmanaged[Stdcall]<IntPtr, Guid*, IntPtr*, int> QueryInterface;
		public delegate* unmanaged[Stdcall]<IntPtr, uint> AddRef;
		public delegate* unmanaged[Stdcall]<IntPtr, uint> Release;
		public delegate* unmanaged[Stdcall]<IntPtr, IntPtr, int> OnNotify;
	}
}
