using System.Diagnostics;
using System.ServiceProcess;
using AutoOS.Core.Helpers.Registry;
using Microsoft.Win32;

namespace AutoOS.App.Views.Settings;

public sealed partial class ServicesPage : Page
{
	private bool isInitializingServicesState = true;
	private bool isInitializingWIFIState = true;
	private bool isInitializingBluetoothState = true;
	private readonly string list = Path.Combine(PathHelper.GetAppDataFolderPath(), "Service-list-builder", "lists.ini");

	public ServicesPage()
	{
		InitializeComponent();
		if (!File.Exists(Path.Combine(PathHelper.GetAppDataFolderPath(), "Service-list-builder", "lists.ini")))
		{
			Directory.CreateDirectory(Path.Combine(PathHelper.GetAppDataFolderPath(), "Service-list-builder"));
			File.Copy(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "Service-list-builder", "lists.ini"), list);
		}
		GetServicesState();
		GetWIFIState();
		GetBluetoothState();
	}

	private void GetServicesState()
	{
		// check state
		using (RegistryKey? key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\Beep"))
		{
			Services.IsOn = (int)(key.GetValue("Start", 0)) == 1;
		}

		var serviceController = new ServiceController("Beep");
		bool isRunning = serviceController.Status == ServiceControllerStatus.Running;
		if (Services.IsOn && !isRunning || !Services.IsOn && isRunning)
		{
			// remove infobar
			ServiceInfo.Children.Clear();

			var infoBar = new InfoBar
			{
				Title = Services.IsOn ? "Successfully enabled Services & Drivers. A restart is required to apply the change." : "Successfully disabled Services & Drivers. A restart is required to apply the change.",
				IsClosable = false,
				IsOpen = true,
				Severity = InfoBarSeverity.Success,
				Margin = new Thickness(4, -28, 4, 36),
				ActionButton = new Button
				{
					Content = "Restart",
					HorizontalAlignment = HorizontalAlignment.Right
				}
			};
			((Button)infoBar.ActionButton).Click += (s, args) => Process.Start(new ProcessStartInfo("shutdown", "/r /f /t 0") { CreateNoWindow = true });

			ServiceInfo.Children.Add(infoBar);
		}
		isInitializingServicesState = false;
	}

	private async void Services_Toggled(object sender, RoutedEventArgs e)
	{
		if (isInitializingServicesState) return;

		// disable hittestvisible to avoid double-clicking
		Services.IsHitTestVisible = false;
		WIFI.IsHitTestVisible = false;
		Bluetooth.IsHitTestVisible = false;

		// remove infobar
		ServiceInfo.Children.Clear();

		// add infobar
		ServiceInfo.Children.Add(new InfoBar
		{
			Title = Services.IsOn ? "Enabling Services & Drivers..." : "Disabling Services & Drivers...",
			IsClosable = false,
			IsOpen = true,
			Severity = InfoBarSeverity.Informational,
			Margin = new Thickness(4, -28, 4, 36)
		});

		string buildPath = Path.Combine(PathHelper.GetAppDataFolderPath(), "Service-list-builder", "build");

		if (Directory.Exists(buildPath) && Directory.EnumerateDirectories(buildPath).Any())
		{
			// get latest build
			string folderName = Directory.GetDirectories(Path.Combine(PathHelper.GetAppDataFolderPath(), "Service-list-builder", "build")).OrderByDescending(d => Directory.GetLastWriteTime(d)).FirstOrDefault()?.Split('\\').Last();

			// toggle services
			await RegistryHelper.RunAs(RegistryHelper.Identity.TrustedInstaller, new ProcessStartInfo(Path.Combine(PathHelper.GetAppDataFolderPath(), "Service-list-builder", "build", folderName, Services.IsOn ? "Services-Enable.bat" : "Services-Disable.bat")) { CreateNoWindow = true });
		}
		else
		{
			// build service list
			await RegistryHelper.RunAs(RegistryHelper.Identity.TrustedInstaller, new ProcessStartInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "Service-list-builder", "service-list-builder.exe"), $@"--config ""{Path.Combine(PathHelper.GetAppDataFolderPath(), "Service-list-builder", "lists.ini")}"" --disable-service-warning --output-dir ""{Path.Combine(PathHelper.GetAppDataFolderPath(), "Service-list-builder", "build")}""") { CreateNoWindow = true });

			// get latest build
			string folderName = Directory.GetDirectories(Path.Combine(PathHelper.GetAppDataFolderPath(), "Service-list-builder", "build")).OrderByDescending(d => Directory.GetLastWriteTime(d)).FirstOrDefault()?.Split('\\').Last();

			// toggle services
			await RegistryHelper.RunAs(RegistryHelper.Identity.TrustedInstaller, new ProcessStartInfo(Path.Combine(PathHelper.GetAppDataFolderPath(), "Service-list-builder", "build", folderName, Services.IsOn ? "Services-Enable.bat" : "Services-Disable.bat")) { CreateNoWindow = true });
		}

		// re-enable hittestvisible
		Services.IsHitTestVisible = true;
		WIFI.IsHitTestVisible = true;
		Bluetooth.IsHitTestVisible = true;

		// remove infobar
		ServiceInfo.Children.Clear();

		// add infobar
		var infoBar = new InfoBar
		{
			Title = Services.IsOn ? "Successfully enabled Services & Drivers." : "Successfully disabled Services & Drivers.",
			IsClosable = false,
			IsOpen = true,
			Severity = InfoBarSeverity.Success,
			Margin = new Thickness(4, -28, 4, 36)
		};
		ServiceInfo.Children.Add(infoBar);

		// add restart button
		var serviceController = new ServiceController("Beep");
		bool isRunning = serviceController.Status == ServiceControllerStatus.Running;

		if (Services.IsOn && !isRunning || (!Services.IsOn && isRunning))
		{
			infoBar.Title += " A restart is required to apply the change.";
			infoBar.ActionButton = new Button
			{
				Content = "Restart",
				HorizontalAlignment = HorizontalAlignment.Right
			};
			((Button)infoBar.ActionButton).Click += (s, args) =>
			Process.Start(new ProcessStartInfo("shutdown", "/r /f /t 0") { CreateNoWindow = true });
		}
		else
		{
			// delay
			await Task.Delay(2000);

			// remove infobar
			ServiceInfo.Children.Clear();
		}
	}

	private void GetWIFIState()
	{
		// define services and drivers
		string[] services = new[] { "WlanSvc", "Netman", "NetSetupSvc", "NlaSvc", "Wcmsvc", "WinHttpAutoProxySvc" };
		string[] drivers = new[] { "# tdx", "# vwififlt", "# Netwtw10", "# Netwtw14" };

		// check state
		WIFI.IsChecked = services.All(service => File.ReadAllLines(list).Any(line => line.Trim() == service))
		&& drivers.All(driver => File.ReadAllLines(list).Any(line => line.Trim() == driver));

		isInitializingWIFIState = false;
	}

	private async void WIFI_Checked(object sender, RoutedEventArgs e)
	{
		if (isInitializingWIFIState) return;

		// disable hittestvisible to avoid double-clicking
		Services.IsHitTestVisible = false;
		WIFI.IsHitTestVisible = false;
		Bluetooth.IsHitTestVisible = false;

		// remove infobar
		ServiceInfo.Children.Clear();

		// add infobar
		ServiceInfo.Children.Add(new InfoBar
		{
			Title = WIFI.IsChecked == true ? "Enabling WiFi support..." : "Disabling WiFi support...",
			IsClosable = false,
			IsOpen = true,
			Severity = InfoBarSeverity.Informational,
			Margin = new Thickness(4, -28, 4, 36)
		});

		// read list
		string[] lines = await File.ReadAllLinesAsync(list);

		// define services and drivers
		string[] services = new[] { "WlanSvc", "Netman", "NetSetupSvc", "NlaSvc", "Wcmsvc", "WinHttpAutoProxySvc" };
		string[] drivers = new[] { "tdx", "vwififlt", "Netwtw10", "Netwtw14" };

		// make changes
		bool isChecked = WIFI.IsChecked == true;
		for (int i = 0; i < lines.Length; i++)
		{
			if (services.Contains(lines[i].Trim().TrimStart('#', ' ')))
				lines[i] = (isChecked ? lines[i].TrimStart('#', ' ') : "# " + lines[i].TrimStart('#', ' ')).Trim();
			if (drivers.Contains(lines[i].Trim().TrimStart('#', ' ')))
				lines[i] = (isChecked ? "# " + lines[i] : lines[i].TrimStart('#')).Trim();
		}

		// write changes
		await File.WriteAllLinesAsync(list, lines);

		if (!Services.IsOn)
		{
			// get latest build
			string folderName = Directory.GetDirectories(Path.Combine(PathHelper.GetAppDataFolderPath(), "Service-list-builder", "build")).OrderByDescending(d => Directory.GetLastWriteTime(d)).FirstOrDefault()?.Split('\\').Last();

			// enable services
			await RegistryHelper.RunAs(RegistryHelper.Identity.TrustedInstaller, new ProcessStartInfo(Path.Combine(PathHelper.GetAppDataFolderPath(), "Service-list-builder", "build", folderName, "Services-Enable.bat")) { CreateNoWindow = true });
		}

		if (isChecked)
		{
			// declare services and drivers
			(string[], int)[] groups =
			[
				(["WlanSvc", "Wcmsvc"], 2),
				(["NlaSvc", "WinHttpAutoProxySvc", "Netwtw10", "Netwtw14"], 3),
				(["tdx", "vwififlt"], 1)
			];

			// set start values
			foreach ((string[], int) group in groups)
			{
				foreach (string service in group.Item1)
				{
					using (RegistryKey? key = Registry.LocalMachine.OpenSubKey($@"SYSTEM\CurrentControlSet\Services\{service}", writable: true))
					{
						if (key == null) continue;

						Registry.SetValue($@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\{service}", "Start", group.Item2);
					}
				}
			}
		}

		// build service list
		await RegistryHelper.RunAs(RegistryHelper.Identity.TrustedInstaller, new ProcessStartInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "Service-list-builder", "service-list-builder.exe"), $@"--config ""{Path.Combine(PathHelper.GetAppDataFolderPath(), "Service-list-builder", "lists.ini")}"" --disable-service-warning --output-dir ""{Path.Combine(PathHelper.GetAppDataFolderPath(), "Service-list-builder", "build")}""") { CreateNoWindow = true });

		if (!Services.IsOn)
		{
			// get latest build
			string folderName = Directory.GetDirectories(Path.Combine(PathHelper.GetAppDataFolderPath(), "Service-list-builder", "build")).OrderByDescending(d => Directory.GetLastWriteTime(d)).FirstOrDefault()?.Split('\\').Last();

			// disable services
			await RegistryHelper.RunAs(RegistryHelper.Identity.TrustedInstaller, new ProcessStartInfo(Path.Combine(PathHelper.GetAppDataFolderPath(), "Service-list-builder", "build", folderName, "Services-Disable.bat")) { CreateNoWindow = true });

			// re-enable hittestvisible
			Services.IsHitTestVisible = true;
			WIFI.IsHitTestVisible = true;
			Bluetooth.IsHitTestVisible = true;

			// remove infobar
			ServiceInfo.Children.Clear();

			// add infobar
			var infoBar = new InfoBar
			{
				Title = WIFI.IsChecked == true ? "Successfully enabled WiFi support. A restart is required to apply the change." : "Successfully disabled WiFi support. A restart is required to apply the change.",
				IsClosable = false,
				IsOpen = true,
				Severity = InfoBarSeverity.Success,
				Margin = new Thickness(4, -28, 4, 36),
				ActionButton = new Button
				{
					Content = "Restart",
					HorizontalAlignment = HorizontalAlignment.Right
				}
			};
			((Button)infoBar.ActionButton).Click += (s, args) =>
			Process.Start(new ProcessStartInfo("shutdown", "/r /f /t 0") { CreateNoWindow = true });

			ServiceInfo.Children.Add(infoBar);
		}
		else
		{
			// re-enable hittestvisible
			Services.IsHitTestVisible = true;
			WIFI.IsHitTestVisible = true;
			Bluetooth.IsHitTestVisible = true;

			// remove infobar
			ServiceInfo.Children.Clear();

			// add infobar
			ServiceInfo.Children.Add(new InfoBar
			{
				Title = WIFI.IsChecked == true ? "Successfully enabled WiFi support." : "Successfully disabled WiFi support.",
				IsClosable = false,
				IsOpen = true,
				Severity = InfoBarSeverity.Success,
				Margin = new Thickness(4, -28, 4, 36)
			});

			// delay
			await Task.Delay(2000);

			// remove infobar
			ServiceInfo.Children.Clear();
		}
	}

	private void GetBluetoothState()
	{
		// define services and drivers
		string[] services = new[] { "BluetoothUserService", "BTAGService", "BthAvctpSvc", "bthserv", "DeviceAssociationService", "DevicesFlowUserSvc", "DsmSvc", "NcbService", "WFDSConMgrSvc" };
		string[] drivers = new[] { "# BthA2dp", "# BthEnum", "# BthHFAud", "# BthHFEnum", "# BthLEEnum", "# BthMini", "# BTHMODEM", "# BthPan", "# BTHPORT", "# BTHUSB", "# HidBth", "# ibtusb", "# Microsoft_Bluetooth_AvrcpTransport", "# RFCOMM" };

		// check state
		Bluetooth.IsChecked = services.All(service => File.ReadAllLines(list).Any(line => line.Trim() == service))
		&& drivers.All(driver => File.ReadAllLines(list).Any(line => line.Trim() == driver));

		isInitializingBluetoothState = false;
	}

	private async void Bluetooth_Checked(object sender, RoutedEventArgs e)
	{
		if (isInitializingBluetoothState) return;

		// disable hittestvisible to avoid double-clicking
		Services.IsHitTestVisible = false;
		WIFI.IsHitTestVisible = false;
		Bluetooth.IsHitTestVisible = false;

		// remove infobar
		ServiceInfo.Children.Clear();

		// add infobar
		ServiceInfo.Children.Add(new InfoBar
		{
			Title = Bluetooth.IsChecked == true ? "Enabling Bluetooth support..." : "Disabling Bluetooth support...",
			IsClosable = false,
			IsOpen = true,
			Severity = InfoBarSeverity.Informational,
			Margin = new Thickness(4, -28, 4, 36)
		});

		// read list
		string[] lines = await File.ReadAllLinesAsync(list);

		// define services and drivers
		string[] services = new[] { "BluetoothUserService", "BTAGService", "BthAvctpSvc", "bthserv", "DeviceAssociationService", "DevicesFlowUserSvc", "DsmSvc", "NcbService", "WFDSConMgrSvc" };
		string[] drivers = new[] { "BthA2dp", "BthEnum", "BthHFAud", "BthHFEnum", "BthLEEnum", "BthMini", "BTHMODEM", "BthPan", "BTHPORT", "BTHUSB", "HidBth", "ibtusb", "Microsoft_Bluetooth_AvrcpTransport", "RFCOMM" };

		// make changes
		bool isChecked = Bluetooth.IsChecked == true;
		for (int i = 0; i < lines.Length; i++)
		{
			if (services.Contains(lines[i].Trim().TrimStart('#', ' ')))
				lines[i] = (isChecked ? lines[i].TrimStart('#', ' ') : "# " + lines[i].TrimStart('#', ' ')).Trim();
			if (drivers.Contains(lines[i].Trim().TrimStart('#', ' ')))
				lines[i] = (isChecked ? "# " + lines[i] : lines[i].TrimStart('#')).Trim();
		}

		// write changes
		await File.WriteAllLinesAsync(list, lines);

		if (!Services.IsOn)
		{
			// get latest build
			string folderName = Directory.GetDirectories(Path.Combine(PathHelper.GetAppDataFolderPath(), "Service-list-builder", "build")).OrderByDescending(d => Directory.GetLastWriteTime(d)).FirstOrDefault()?.Split('\\').Last();

			// enable services
			await RegistryHelper.RunAs(RegistryHelper.Identity.TrustedInstaller, new ProcessStartInfo(Path.Combine(PathHelper.GetAppDataFolderPath(), "Service-list-builder", "build", folderName, "Services-Enable.bat")) { CreateNoWindow = true });
		}

		if (isChecked)
		{
			// declare services and drivers
			(string[], int)[] groups = new[]
			{
				(new[] { "BluetoothUserService", "BTAGService", "BthAvctpSvc", "bthserv", "DeviceAssociationService", "DevicesFlowUserSvc", "DsmSvc", "NcbService", "WFDSConMgrSvc", "BthA2dp", "BthEnum", "BthHFAud", "BthHFEnum", "BthLEEnum", "BTHMODEM", "BthMini", "BthPan", "BTHPORT", "BTHUSB", "HidBth", "Microsoft_Bluetooth_AvrcpTransport", "RFCOMM", "ibtusb" }, 3),
			};

			// set start values
			foreach ((string[], int) group in groups)
			{
				foreach (string service in group.Item1)
				{
					using (RegistryKey? key = Registry.LocalMachine.OpenSubKey($@"SYSTEM\CurrentControlSet\Services\{service}", writable: true))
					{
						if (key == null) continue;

						Registry.SetValue($@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\{service}", "Start", group.Item2);
					}
				}
			}
		}

		// build service list
		await RegistryHelper.RunAs(RegistryHelper.Identity.TrustedInstaller, new ProcessStartInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "Service-list-builder", "service-list-builder.exe"), $@"--config ""{Path.Combine(PathHelper.GetAppDataFolderPath(), "Service-list-builder", "lists.ini")}"" --disable-service-warning --output-dir ""{Path.Combine(PathHelper.GetAppDataFolderPath(), "Service-list-builder", "build")}""") { CreateNoWindow = true });

		if (!Services.IsOn)
		{
			// get latest build
			string folderName = Directory.GetDirectories(Path.Combine(PathHelper.GetAppDataFolderPath(), "Service-list-builder", "build")).OrderByDescending(d => Directory.GetLastWriteTime(d)).FirstOrDefault()?.Split('\\').Last();

			// disable services
			await RegistryHelper.RunAs(RegistryHelper.Identity.TrustedInstaller, new ProcessStartInfo(Path.Combine(PathHelper.GetAppDataFolderPath(), "Service-list-builder", "build", folderName, "Services-Disable.bat")) { CreateNoWindow = true });

			// re-enable hittestvisible
			Services.IsHitTestVisible = true;
			WIFI.IsHitTestVisible = true;
			Bluetooth.IsHitTestVisible = true;

			// remove infobar
			ServiceInfo.Children.Clear();

			// add infobar
			var infoBar = new InfoBar
			{
				Title = Bluetooth.IsChecked == true ? "Successfully enabled Bluetooth support. A restart is required to apply the change." : "Successfully disabled Bluetooth support. A restart is required to apply the change.",
				IsClosable = false,
				IsOpen = true,
				Severity = InfoBarSeverity.Success,
				Margin = new Thickness(4, -28, 4, 36),
				ActionButton = new Button
				{
					Content = "Restart",
					HorizontalAlignment = HorizontalAlignment.Right
				}
			};
			((Button)infoBar.ActionButton).Click += (s, args) =>
			Process.Start(new ProcessStartInfo("shutdown", "/r /f /t 0") { CreateNoWindow = true });

			ServiceInfo.Children.Add(infoBar);
		}
		else
		{
			// re-enable hittestvisible
			Services.IsHitTestVisible = true;
			WIFI.IsHitTestVisible = true;
			Bluetooth.IsHitTestVisible = true;

			// remove infobar
			ServiceInfo.Children.Clear();

			// add infobar
			ServiceInfo.Children.Add(new InfoBar
			{
				Title = Bluetooth.IsChecked == true ? "Successfully enabled Bluetooth support." : "Successfully disabled Bluetooth support.",
				IsClosable = false,
				IsOpen = true,
				Severity = InfoBarSeverity.Success,
				Margin = new Thickness(4, -28, 4, 36)
			});

			// delay
			await Task.Delay(2000);

			// remove infobar
			ServiceInfo.Children.Clear();
		}
	}
}
