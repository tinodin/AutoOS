using System.ComponentModel;
using System.Runtime.CompilerServices;
using AutoOS.Core.Helpers.Network.Models;
using WinRT;

namespace AutoOS.Core.Helpers.Device.Models;

public enum DeviceType
{
	GPU,
	XHCI,
	NIC,
	HDAUD,
	AudioEndpoint,
	AudioController
}

public enum DeviceState
{
	Enabled,
	Disabled,
	Error
}

public enum NicDriverType
{
	NDIS,
	NetAdapterCx
}

public enum NicDeviceType
{
	LAN,
	WiFi,
	Virtual
}

public enum XhciDeviceType
{
	Controller,
	Hub
}

[GeneratedBindableCustomProperty]
public partial class DeviceInfo : INotifyPropertyChanged
{
	public string FriendlyName { get; set; } = string.Empty;
	public string DeviceDescription { get; set; } = string.Empty;
	public string PnpDeviceId { get; set; } = string.Empty;
	public string VendorId { get; set; } = string.Empty;
	public string DeviceId { get; set; } = string.Empty;
	public string Location { get; set; } = string.Empty;
	public string RegistryPath { get; set; } = string.Empty;
	public DeviceState State { get; set; } = DeviceState.Error;
	public string DevObjName { get; set; } = string.Empty;
	public uint MsiSupported { get; set; }
	public uint MsiLimit { get; set; }
	public uint MaxMsiLimit { get; set; }
	public uint DevicePolicy { get; set; }
	public uint DevicePriority { get; set; }
	public ulong AssignmentSetOverride { get; set; }
	public bool SupportsIrq { get; set; }
	public NicDriverType DriverType { get; set; }
	public NicDeviceType NicType { get; set; }

	private bool isActive;
	public bool IsActive
	{
		get => isActive;
		set { if (isActive != value) { isActive = value; OnPropertyChanged(); } }
	}

	public XhciDeviceType XhciType { get; set; }
	public DeviceType DeviceType { get; set; }
	public ulong BaseAddress { get; set; }
	public string CurrentVersion { get; set; } = string.Empty;

	public object AudioChannels { get; set; } = null!;
	public object AudioBitDepths { get; set; } = null!;
	public object AudioSampleRates { get; set; } = null!;

	private object availableFormats = null!;
	public object AvailableFormats
	{
		get => availableFormats;
		set { if (availableFormats != value) { availableFormats = value; OnPropertyChanged(); } }
	}

	private object? selectedFormat;
	public object? SelectedFormat
	{
		get => selectedFormat;
		set { if (selectedFormat != value) { selectedFormat = value; OnPropertyChanged(); } }
	}

	public string Description { get; set; } = string.Empty;

	private float _volume;
	public float Volume
	{
		get => _volume;
		set { if (_volume != value) { _volume = value; OnPropertyChanged(); } }
	}

	private bool _isMuted;
	public bool IsMuted
	{
		get => _isMuted;
		set { if (_isMuted != value) { _isMuted = value; OnPropertyChanged(); } }
	}

	private float _leftVolume;
	public float LeftVolume
	{
		get => _leftVolume;
		set { if (_leftVolume != value) { _leftVolume = value; OnPropertyChanged(); } }
	}

	private float _rightVolume;
	public float RightVolume
	{
		get => _rightVolume;
		set { if (_rightVolume != value) { _rightVolume = value; OnPropertyChanged(); } }
	}

	private bool _supportPerChannelVolume = true;
	public bool SupportPerChannelVolume
	{
		get => _supportPerChannelVolume;
		set { if (_supportPerChannelVolume != value) { _supportPerChannelVolume = value; OnPropertyChanged(); } }
	}

	private object bufferSizes = null!;
	public object BufferSizes
	{
		get => bufferSizes;
		set { if (bufferSizes != value) { bufferSizes = value; OnPropertyChanged(); } }
	}

	private object? selectedBufferSize;
	public object? SelectedBufferSize
	{
		get => selectedBufferSize;
		set { if (selectedBufferSize != value) { selectedBufferSize = value; OnPropertyChanged(); } }
	}

	public bool IsInputDevice { get; set; }
	public uint FormFactor { get; set; }

	public bool IsWiFi => NicType == NicDeviceType.WiFi;
	public bool IsLAN => NicType == NicDeviceType.LAN;
	public List<NetworkAdvancedSetting> AdvancedSettings { get; set; } = [];

	public event PropertyChangedEventHandler? PropertyChanged;
	private void OnPropertyChanged([CallerMemberName] string? name = null)
		=> PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public class ApplyResult
{
	public bool Success { get; set; }
	public bool NeedsRestart { get; set; }
	public List<DeviceInfo> ChangedDevices { get; set; } = [];
	public Dictionary<string, DeviceInfo> AppliedSettings { get; set; } = [];
}

public class RestartResult
{
	public int SuccessCount { get; set; }
	public int FailedCount { get; set; }
	public List<string> FailedDevices { get; set; } = [];
}
