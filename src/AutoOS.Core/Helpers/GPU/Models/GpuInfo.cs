using System.ComponentModel;
using System.Runtime.CompilerServices;
using WinRT;

namespace AutoOS.Core.Helpers.GPU.Models;

[GeneratedBindableCustomProperty]
public partial class GpuInfo : INotifyPropertyChanged
{
	private string deviceName = null!;
	public string DeviceName
	{
		get => deviceName;
		set { if (deviceName != value) { deviceName = value; OnPropertyChanged(); } }
	}

	private string currentVersion = null!;
	public string CurrentVersion
	{
		get => currentVersion;
		set { if (currentVersion != value) { currentVersion = value; OnPropertyChanged(); } }
	}

	private bool isInstalled;
	public bool IsInstalled
	{
		get => isInstalled;
		set { if (isInstalled != value) { isInstalled = value; OnPropertyChanged(); } }
	}

	private bool pstates = false;
	public bool PStates
	{
		get => pstates;
		set { if (pstates != value) { pstates = value; OnPropertyChanged(); } }
	}

	private bool ecc = false;
	public bool ECC
	{
		get => ecc;
		set { if (ecc != value) { ecc = value; OnPropertyChanged(); } }
	}

	private bool gspFirmware = false;
	public bool GspFirmware
	{
		get => gspFirmware;
		set { if (gspFirmware != value) { gspFirmware = value; OnPropertyChanged(); } }
	}

	private bool hdcp = false;
	public bool HDCP
	{
		get => hdcp;
		set { if (hdcp != value) { hdcp = value; OnPropertyChanged(); } }
	}

	private bool hdmidpaudio = true;
	public bool HDMIDPAudio
	{
		get => hdmidpaudio;
		set { if (hdmidpaudio != value) { hdmidpaudio = value; OnPropertyChanged(); } }
	}

	public string PnPDeviceId { get; set; } = null!;
	public string VendorId { get; set; } = null!;
	public string DeviceId { get; set; } = null!;
	public string Codename { get; set; } = null!;
	public string Location { get; set; } = null!;
	public string RegistryPath { get; set; } = null!;
	public bool NVIDIA => VendorId == "10de";
	public bool RTX => NVIDIA && DeviceName.Contains("RTX");
	public bool ECCSupport => NVIDIA && (DeviceName.Contains("RTX 3090") || DeviceName.Contains("RTX 3090 Ti") || DeviceName.Contains("RTX 4090"));
	public bool Install { get; set; } = true;

	public event PropertyChangedEventHandler? PropertyChanged;
	private void OnPropertyChanged([CallerMemberName] string? name = null)
		=> PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
