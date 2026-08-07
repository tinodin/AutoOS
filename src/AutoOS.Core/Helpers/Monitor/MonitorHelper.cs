using System.Diagnostics;
using System.Text;
using AutoOS.Core.Helpers.Monitor.Models;
using Microsoft.Win32;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;

namespace AutoOS.Core.Helpers.Monitor;

public static partial class MonitorHelper
{
	private const uint EDD_GET_DEVICE_INTERFACE_NAME = 0x00000001;

	public static unsafe List<MonitorInfo> GetMonitors()
	{
		List<MonitorInfo> monitors = [];
		Dictionary<string, string> hardwareNames = GetModelNamesFromRegistry();
		DISPLAY_DEVICEW adapter = new() { cb = (uint)sizeof(DISPLAY_DEVICEW) };
		uint i = 0;

		while (PInvoke.EnumDisplayDevices(null, i++, ref adapter, 0))
		{
			string adapterPath = adapter.DeviceName.ToString();
			DISPLAY_DEVICEW monitorDevice = new() { cb = (uint)sizeof(DISPLAY_DEVICEW) };
			uint j = 0;

			while (PInvoke.EnumDisplayDevices(adapterPath, j++, ref monitorDevice, EDD_GET_DEVICE_INTERFACE_NAME))
			{
				if (monitorDevice.StateFlags.HasFlag(DISPLAY_DEVICE_STATE_FLAGS.DISPLAY_DEVICE_ATTACHED_TO_DESKTOP))
				{
					DEVMODEW dm = new() { dmSize = (ushort)sizeof(DEVMODEW) };
					if (PInvoke.EnumDisplaySettings(adapterPath, ENUM_DISPLAY_SETTINGS_MODE.ENUM_CURRENT_SETTINGS, ref dm))
					{
						string interfacePath = monitorDevice.DeviceID.ToString();
						string hwId = ExtractHardwareId(interfacePath);
						string deviceString = monitorDevice.DeviceString.ToString();

						monitors.Add(new MonitorInfo
						{
							DeviceName = hardwareNames.TryGetValue(hwId, out string? name) ? name : deviceString,
							DevicePath = adapterPath,
							Resolution = (dm.dmPelsWidth, dm.dmPelsHeight),
							RefreshRate = dm.dmDisplayFrequency
						});
					}
				}
			}
		}
		return monitors;
	}

	private static Dictionary<string, string> GetModelNamesFromRegistry()
	{
		var results = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		try
		{
			using RegistryKey? monitorKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Enum\DISPLAY");
			if (monitorKey == null) return results;

			foreach (string hwId in monitorKey.GetSubKeyNames())
			{
				using RegistryKey? instanceKey = monitorKey.OpenSubKey(hwId);
				if (instanceKey == null) continue;

				foreach (string instance in instanceKey.GetSubKeyNames())
				{
					using RegistryKey? details = instanceKey.OpenSubKey($@"{instance}\Device Parameters");
					if (details == null) continue;

					if (details.GetValue("EDID") is byte[] edid)
					{
						string model = ParseEdidForModel(edid);
						if (!string.IsNullOrEmpty(model)) results[hwId] = model;
					}
				}
			}
		}
		catch { }
		return results;
	}

	private static string ExtractHardwareId(string path)
	{
		string[] parts = path.Split('#');
		return parts.Length > 1 ? parts[1] : "";
	}

	private static string ParseEdidForModel(byte[] edid)
	{
		for (int i = 54; i < 108; i += 18)
		{
			if (edid.Length >= i + 18 && edid[i] == 0 && edid[i + 1] == 0 && edid[i + 2] == 0 && edid[i + 3] == 0xFC)
			{
				return Encoding.ASCII.GetString(edid, i + 5, 13).Replace("\0", "").Trim();
			}
		}
		return "";
	}

	public static unsafe void SetHighestRefreshRates()
	{
		DISPLAY_DEVICEW adapter = new() { cb = (uint)sizeof(DISPLAY_DEVICEW) };
		uint i = 0;

		while (PInvoke.EnumDisplayDevices(null, i++, ref adapter, 0))
		{
			string adapterPath = adapter.DeviceName.ToString();
			DEVMODEW current = new() { dmSize = (ushort)sizeof(DEVMODEW) };

			if (!PInvoke.EnumDisplaySettings(adapterPath, ENUM_DISPLAY_SETTINGS_MODE.ENUM_CURRENT_SETTINGS, ref current)) continue;

			DEVMODEW best = current;
			for (int j = 0; ; j++)
			{
				DEVMODEW test = new() { dmSize = (ushort)sizeof(DEVMODEW) };
				if (!PInvoke.EnumDisplaySettings(adapterPath, (ENUM_DISPLAY_SETTINGS_MODE)j, ref test)) break;

				if (test.dmPelsWidth == current.dmPelsWidth &&
					test.dmPelsHeight == current.dmPelsHeight &&
					test.dmDisplayFrequency > best.dmDisplayFrequency)
				{
					best = test;
				}
			}

			if (best.dmDisplayFrequency > current.dmDisplayFrequency)
			{
				fixed (char* pAdapterPath = adapterPath)
				{
					PInvoke.ChangeDisplaySettingsEx(pAdapterPath, &best, HWND.Null, CDS_TYPE.CDS_UPDATEREGISTRY | CDS_TYPE.CDS_GLOBAL, null);
				}
			}
		}
	}

	public static async Task ImportMonitorConfig()
	{
		string? systemDrive = Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.System))?.ToUpperInvariant();
		var target = DriveInfo.GetDrives()
			.Where(d => d.DriveType == DriveType.Fixed && d.Name.ToUpperInvariant() != systemDrive)
			.Select(d => new { Drive = d.Name, SystemHivePath = Path.Combine(d.Name, "Windows", "System32", "config", "SYSTEM") })
			.Where(x => File.Exists(x.SystemHivePath))
			.Select(x => new { x.Drive, x.SystemHivePath, LastWriteTime = File.GetLastWriteTime(x.SystemHivePath) })
			.OrderByDescending(x => x.LastWriteTime)
			.FirstOrDefault();

		if (target == null) return;

		await Process.Start(new ProcessStartInfo
		{
			FileName = "reg.exe",
			Arguments = $@"load HKLM\OfflineSystem ""{target.SystemHivePath}""",
			CreateNoWindow = true,
			UseShellExecute = false
		})!.WaitForExitAsync();

		string[] keysToCopy = ["Configuration", "Connectivity", "ScaleFactors"];
		foreach (string key in keysToCopy)
		{
			await Process.Start(new ProcessStartInfo
			{
				FileName = "reg.exe",
				Arguments = $@"copy ""HKLM\OfflineSystem\ControlSet001\Control\GraphicsDrivers\{key}"" ""HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers\{key}"" /s /f",
				CreateNoWindow = true,
				UseShellExecute = false
			})!.WaitForExitAsync();
		}

		await Process.Start(new ProcessStartInfo
		{
			FileName = "reg.exe",
			Arguments = @"unload HKLM\OfflineSystem",
			CreateNoWindow = true,
			UseShellExecute = false
		})!.WaitForExitAsync();
	}
}
