using AutoOS.Core.Helpers.Device.Models;
using AutoOS.Core.Helpers.Device;
using Microsoft.UI.Xaml.Media;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace AutoOS.Views.Settings;

public sealed partial class DevicesPage : Page
{
	private bool initialBluetoothState = false;
	private bool isInitializingBluetoothState = true;
	private bool isInitializingIMODState = true;

	public ObservableCollection<DeviceInfo> XHCIs { get; } = [];

	public DevicesPage()
	{
		InitializeComponent();
		GetBluetoothState();
		GetXHCIControllers();
		Loaded += DevicesPage_Loaded;
	}

	private void DevicesPage_Loaded(object sender, RoutedEventArgs e)
	{
		isInitializingIMODState = false;
	}

	private void GetBluetoothState()
	{
		// declare services and drivers
		var groups = new[]
		{
			(new[] { "BluetoothUserService", "BTAGService", "BthAvctpSvc", "bthserv", "DeviceAssociationService", "DevicesFlowUserSvc", "DsmSvc", "NcbService", "WFDSConMgrSvc", "BthA2dp", "BthEnum", "BthHFAud", "BthHFEnum", "BthLEEnum", "BTHMODEM", "BthMini", "BthPan", "BTHPORT", "BTHUSB", "HidBth", "Microsoft_Bluetooth_AvrcpTransport", "RFCOMM", "ibtusb" }, 3),
			(["SystemEventsBroker"], 2)
		};

		// check if values match
		foreach (var group in groups)
		{
			foreach (var service in group.Item1)
			{
				using var key = Registry.LocalMachine.OpenSubKey($@"SYSTEM\CurrentControlSet\Services\{service}");
				if (key == null) continue;

				var startValue = key.GetValue("Start");
				if (startValue == null || (int)startValue != group.Item2)
				{
					isInitializingBluetoothState = false;
					return;
				}
			}
		}

		initialBluetoothState = true;
		Bluetooth.IsOn = true;
		isInitializingBluetoothState = false;
	}

	private async void Bluetooth_Toggled(object sender, RoutedEventArgs e)
	{
		if (isInitializingBluetoothState) return;

		// disable hittestvisible to avoid double-clicking
		Bluetooth.IsHitTestVisible = false;

		// remove infobar
		BluetoothInfo.Children.Clear();

		// add infobar
		BluetoothInfo.Children.Add(new InfoBar
		{
			Title = Bluetooth.IsOn ? "Enabling Bluetooth..." : "Disabling Bluetooth...",
			IsClosable = false,
			IsOpen = true,
			Severity = InfoBarSeverity.Informational,
			Margin = new Thickness(0, 0, 0, 12)
		});

		// declare services and drivers
		var groups = new[]
		{
			(new[] { "BluetoothUserService", "BTAGService", "BthAvctpSvc", "bthserv", "DeviceAssociationService", "DevicesFlowUserSvc", "DsmSvc", "NcbService", "WFDSConMgrSvc", "BthA2dp", "BthEnum", "BthHFAud", "BthHFEnum", "BthLEEnum", "BTHMODEM", "BthMini", "BthPan", "BTHPORT", "BTHUSB", "HidBth", "Microsoft_Bluetooth_AvrcpTransport", "RFCOMM", "ibtusb" }, 3),
		};

		// set start values
		foreach (var group in groups)
		{
			foreach (var service in group.Item1)
			{
				using var key = Registry.LocalMachine.OpenSubKey($@"SYSTEM\CurrentControlSet\Services\{service}", writable: true);
				if (key == null) continue;

				Registry.SetValue($@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\{service}", "Start", Bluetooth.IsOn ? group.Item2 : 4);
			}
		}

		// delay
		await Task.Delay(500);

		// re-enable hittestvisible
		Bluetooth.IsHitTestVisible = true;

		// remove infobar
		BluetoothInfo.Children.Clear();

		// add infobar
		var infoBar = new InfoBar
		{
			Title = Bluetooth.IsOn ? "Successfully enabled Bluetooth." : "Successfully disabled Bluetooth.",
			IsClosable = false,
			IsOpen = true,
			Severity = InfoBarSeverity.Success,
			Margin = new Thickness(0, 0, 0, 12)
		};
		BluetoothInfo.Children.Add(infoBar);

		// add restart button
		if (Bluetooth.IsOn != initialBluetoothState)
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
			BluetoothInfo.Children.Clear();
		}
	}

	private void GetXHCIControllers()
	{
		var devices = DeviceHelper.GetDevices(DeviceType.XHCI);
		XHCIs.Clear();

		foreach (var device in devices)
		{
			XHCIs.Add(device);
			device.IsActive = DeviceHelper.GetIMODState(device);
		}
	}

	private async void IMOD_Toggled(object sender, RoutedEventArgs e)
	{
		if (isInitializingIMODState) return;

		ToggleSwitch toggleSwitch = (ToggleSwitch)sender;
		DeviceInfo device = (DeviceInfo)toggleSwitch.DataContext;
		bool isOn = toggleSwitch.IsOn;
		var DevicesInfo = FindParent<StackPanel>(toggleSwitch).FindName("DevicesInfo") as StackPanel;

		// disable hittestvisible to avoid double-clicking
		toggleSwitch.IsHitTestVisible = false;

		// remove infobar
		DevicesInfo.Children.Clear();

		// add infobar
		DevicesInfo.Children.Add(new InfoBar
		{
			Title = isOn ? "Enabling XHCI Interrupt Moderation (IMOD)..." : "Disabling XHCI Interrupt Moderation (IMOD)...",
			IsClosable = false,
			IsOpen = true,
			Severity = InfoBarSeverity.Informational,
			Margin = new Thickness(0, 0, 0, 12)
		});

		// toggle imod
		DeviceHelper.ToggleImod(device, isOn);

		// delay
		await Task.Delay(500);

		// re-enable hittestvisible
		toggleSwitch.IsHitTestVisible = true;

		// remove infobar
		DevicesInfo.Children.Clear();

		// add infobar
		DevicesInfo.Children.Add(new InfoBar
		{
			Title = isOn ? "Successfully enabled XHCI Interrupt Moderation (IMOD)." : "Successfully disabled XHCI Interrupt Moderation (IMOD).",
			IsClosable = false,
			IsOpen = true,
			Severity = InfoBarSeverity.Success,
			Margin = new Thickness(0, 0, 0, 12)
		});

		// delay
		await Task.Delay(2000);

		// remove infobar
		DevicesInfo.Children.Clear();
	}

	public static T FindParent<T>(DependencyObject child) where T : DependencyObject
	{
		DependencyObject parent = VisualTreeHelper.GetParent(child);

		while (parent != null && parent is not T)
			parent = VisualTreeHelper.GetParent(parent);

		return parent as T;
	}
}
