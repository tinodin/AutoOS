﻿using Microsoft.Win32;
using System.Diagnostics;
using System.Management;
using System.ServiceProcess;
using Windows.Storage;

namespace AutoOS.Views.Settings;

public sealed partial class DevicesPage : Page
{
    private bool initialBluetoothState = false;
    private bool isInitializingBluetoothState = true;
    private bool isInitializingHIDState = true;
    private bool isInitializingIMODState = true;

    private readonly ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

    public DevicesPage()
    {
        InitializeComponent();
        GetBluetoothState();
        GetHIDState();
        GetIMODState();
    }

    private void GetBluetoothState()
    {
        // declare services and drivers
        var groups = new[]
        {
                (new[] { "BluetoothUserService", "BTAGService", "BthAvctpSvc", "bthserv", "DeviceAssociationService", "DevicesFlowUserSvc", "DsmSvc", "NcbService", "WFDSConMgrSvc", "BthA2dp", "BthEnum", "BthHFAud", "BthHFEnum", "BthLEEnum", "BTHMODEM", "BthMini", "BthPan", "BTHPORT", "BTHUSB", "HidBth", "Microsoft_Bluetooth_AvrcpTransport", "RFCOMM", "ibtusb" }, 3),
                (new[] { "SystemEventsBroker" }, 2)
        };

        // check if values match
        foreach (var group in groups)
        {
            foreach (var service in group.Item1)
            {
                using (var key = Registry.LocalMachine.OpenSubKey($@"SYSTEM\CurrentControlSet\Services\{service}"))
                {
                    if (key == null) continue;

                    var startValue = key.GetValue("Start");
                    if (startValue == null || (int)startValue != group.Item2)
                    {
                        isInitializingBluetoothState = false;
                        return;
                    }
                }
            }
        }

        // check for real hardware adapter
        var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PnPEntity WHERE Description LIKE '%Bluetooth%'");
        var devices = searcher.Get();

        foreach (ManagementObject device in devices)
        {
            if (devices.Count > 0)
            {
                string pnpDeviceId = device["PNPDeviceID"]?.ToString();
                string deviceName = device["Name"]?.ToString();

                if (!string.IsNullOrEmpty(pnpDeviceId) && (pnpDeviceId.Contains("USB", StringComparison.OrdinalIgnoreCase) || pnpDeviceId.Contains("PCI", StringComparison.OrdinalIgnoreCase)))
                {
                    if (device["Status"]?.ToString() == "OK")
                    {
                        initialBluetoothState = true;
                        Bluetooth.IsOn = true;
                    }
                    else
                    {
                        isInitializingBluetoothState = false;
                        return;
                    }
                }
            }
            else
            {
                Bluetooth_SettingsGroup.Visibility = Visibility.Collapsed;
            }
        }
        isInitializingBluetoothState = false;
    }

    private async void Bluetooth_Toggled(object sender, RoutedEventArgs e)
    {
        if (isInitializingBluetoothState) return;

        // remove infobar
        BluetoothInfo.Children.Clear();

        // add infobar
        BluetoothInfo.Children.Add(new InfoBar
        {
            Title = Bluetooth.IsOn ? "Enabling Bluetooth..." : "Disabling Bluetooth...",
            IsClosable = false,
            IsOpen = true,
            Severity = InfoBarSeverity.Informational,
            Margin = new Thickness(5)
        });

        // declare services and drivers
        var groups = new[]
        {
            (new[] { "BluetoothUserService", "BTAGService", "BthAvctpSvc", "bthserv", "DsmSvc", "BthA2dp", "BthEnum", "BthHFAud", "BthHFEnum", "BthLEEnum", "BTHMODEM", "BthMini", "BthPan", "BTHPORT", "BTHUSB", "HidBth", "Microsoft_Bluetooth_AvrcpTransport", "RFCOMM", "ibtusb" }, 3),
        };

        // set start values
        foreach (var group in groups)
        {
            foreach (var service in group.Item1)
            {
                using (var key = Registry.LocalMachine.OpenSubKey($@"SYSTEM\CurrentControlSet\Services\{service}", writable: true))
                {
                    if (key == null) continue;

                    Registry.SetValue($@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\{service}", "Start", Bluetooth.IsOn ? group.Item2 : 4);
                }
            }
        }

        // delay
        await Task.Delay(500);

        // remove infobar
        BluetoothInfo.Children.Clear();

        // add infobar
        var infoBar = new InfoBar
        {
            Title = Bluetooth.IsOn ? "Successfully enabled Bluetooth." : "Successfully disabled Bluetooth.",
            IsClosable = false,
            IsOpen = true,
            Severity = InfoBarSeverity.Success,
            Margin = new Thickness(5)
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
            Process.Start("shutdown", "/r /f /t 0");
        }
        else
        {
            // delay
            await Task.Delay(2000);

            // remove infobar
            BluetoothInfo.Children.Clear();
        }
    }

    private async void GetHIDState()
    {
        isInitializingHIDState = true;

        HID.IsOn = await Task.Run(() =>
        {
            return new ManagementObjectSearcher("SELECT * FROM Win32_PnPEntity WHERE Description LIKE '%HID%'")
                       .Get()
                       .Cast<ManagementObject>()
                       .Any(device => device["Status"]?.ToString() == "OK" &&
                        new[] {
                            "HID-compliant consumer control device",
                            "HID-compliant device",
                            "HID-compliant game controller",
                            "HID-compliant system controller",
                            "HID-compliant vendor-defined device"
                        }.Contains(device["Description"]?.ToString()));
        });

        isInitializingHIDState = false;
    }

    private async void HID_Toggled(object sender, RoutedEventArgs e)
    {
        if (isInitializingHIDState) return;

        // remove infobar
        DevicesInfo.Children.Clear();

        // add infobar
        DevicesInfo.Children.Add(new InfoBar
        {
            Title = HID.IsOn ? "Enabling Human Interface Devices (HID)..." : "Disabling Human Interface Devices (HID)...",
            IsClosable = false,
            IsOpen = true,
            Severity = InfoBarSeverity.Informational,
            Margin = new Thickness(5)
        });

        localSettings.Values["HumanInterfaceDevices"] = HID.IsOn ? 1 : 0;

        // toggle hid devices
        string command = HID.IsOn
                            ? "Get-PnpDevice -Class HIDClass | Where-Object { $_.FriendlyName -match 'HID-compliant (consumer control device|device|game controller|system controller|vendor-defined device)' -and $_.FriendlyName -notmatch 'Mouse|Keyboard'} | Enable-PnpDevice -Confirm:$false"
                            : "Get-PnpDevice -Class HIDClass | Where-Object { $_.FriendlyName -match 'HID-compliant (consumer control device|device|game controller|system controller|vendor-defined device)' -and $_.FriendlyName -notmatch 'Mouse|Keyboard'} | Disable-PnpDevice -Confirm:$false";

        await Task.Run(() =>
        {
            var processInfo = new ProcessStartInfo("powershell", $"-Command \"{command}\"")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using (var process = Process.Start(processInfo))
            {
                process.WaitForExit();
            }
        });

        // cleanup devices
        await Task.Run(() => Process.Start(new ProcessStartInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "DeviceCleanup", "DeviceCleanupCmd.exe"), "-s *") { CreateNoWindow = true })?.WaitForExit());

        // remove infobar
        DevicesInfo.Children.Clear();

        // add infobar
        DevicesInfo.Children.Add(new InfoBar
        {
            Title = HID.IsOn ? "Successfully enabled Human Interface Devices (HID)." : "Successfully disabled Human Interface Devices (HID).",
            IsClosable = false,
            IsOpen = true,
            Severity = InfoBarSeverity.Success,
            Margin = new Thickness(5)
        });

        // delay
        await Task.Delay(2000);

        // remove infobar
        DevicesInfo.Children.Clear();
    }

    private async void GetIMODState()
    {
        // hide toggle switch
        IMOD.Visibility = Visibility.Collapsed;

        var query = new SelectQuery("SELECT * FROM Win32_SystemDriver WHERE Name = 'EasyAntiCheat_EOSSys'");
        using (var searcher = new ManagementObjectSearcher(query))
        {
            var driver = searcher.Get().Cast<ManagementObject>().FirstOrDefault();
            if (driver != null && driver["State"]?.ToString() == "Running")
            {
                // add infobar
                DevicesInfo.Children.Add(new InfoBar
                {
                    Title = "Failed to run RWEverything because it is already running or an anticheat is blocking the driver.",
                    IsClosable = false,
                    IsOpen = true,
                    Severity = InfoBarSeverity.Error,
                    Margin = new Thickness(5)
                });

                // disable the switch
                IMOD.IsEnabled = false;

                // hide progress ring
                imodProgress.Visibility = Visibility.Collapsed;

                // show toggle
                IMOD.Visibility = Visibility.Visible;

                isInitializingIMODState = false;

                return;
            }
            else
            {
                // check state
                Process.Start(new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c takeown /f \"{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "RwEverything", "Rw.exe")}\" & icacls \"{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "RwEverything", "Rw.exe")}\" /grant Everyone:F /T /C /Q",
                    UseShellExecute = false,
                    CreateNoWindow = true
                });

                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = $"-ExecutionPolicy Bypass -Command \"& '{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Scripts", "imod.ps1")}' -status '{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "RwEverything", "Rw.exe")}'\"",
                        CreateNoWindow = true,
                        RedirectStandardOutput = true
                    }
                };

                process.Start();

                string output = await process.StandardOutput.ReadToEndAsync();

                if (output.Contains("ENABLED"))
                {
                    localSettings.Values["XhciInterruptModeration"] = 1;

                    IMOD.IsOn = true;
                }
                else if (output.Contains("FAILED"))
                {
                    // resort to setting
                    if ((int?)localSettings.Values["XhciInterruptModeration"] == 1)
                    {
                        IMOD.IsOn = true;
                    }

                    IMOD.IsEnabled = false;

                    // hide progress ring
                    imodProgress.Visibility = Visibility.Collapsed;

                    // show toggle
                    IMOD.Visibility = Visibility.Visible;

                    // add infobar
                    DevicesInfo.Children.Add(new InfoBar
                    {
                        Title = "Failed to run RWEverything because it is already running or an anticheat is blocking the driver.",
                        IsClosable = false,
                        IsOpen = true,
                        Severity = InfoBarSeverity.Error,
                        Margin = new Thickness(5)
                    });
                }

                // hide progress ring
                imodProgress.Visibility = Visibility.Collapsed;

                // show toggle
                IMOD.Visibility = Visibility.Visible;

                isInitializingIMODState = false;
            }
        }
    }

    private async void IMOD_Toggled(object sender, RoutedEventArgs e)
    {
        if (isInitializingIMODState) return;

        // remove infobar
        DevicesInfo.Children.Clear();

        // add infobar
        DevicesInfo.Children.Add(new InfoBar
        {
            Title = IMOD.IsOn ? "Enabling XHCI Interrupt Moderation (IMOD)..." : "Disabling XHCI Interrupt Moderation (IMOD)...",
            IsClosable = false,
            IsOpen = true,
            Severity = InfoBarSeverity.Informational,
            Margin = new Thickness(5)
        });

        // toggle imod
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-ExecutionPolicy Bypass -Command \"& '{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Scripts", "imod.ps1")}' {(IMOD.IsOn ? "-enable" : "-disable")} '{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "RwEverything", "Rw.exe")}'\"",
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            }
        };

        process.Start();

        string output = await process.StandardOutput.ReadToEndAsync();
        string error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        // handle error
        if (error.Contains("Exception") || !output.Contains("Write"))
        {
            // toggle back
            isInitializingIMODState = true;
            IMOD.IsOn = !IMOD.IsOn;
            isInitializingIMODState = false;

            // remove infobar
            DevicesInfo.Children.Clear();

            // add infobar
            DevicesInfo.Children.Add(new InfoBar
            {
                Title = "Failed to run RWEverything because it is already running or an anticheat is blocking the driver.",
                IsClosable = false,
                IsOpen = true,
                Severity = InfoBarSeverity.Error,
                Margin = new Thickness(5)
            });

            // delay
            await Task.Delay(2000);

            // remove infobar
            DevicesInfo.Children.Clear();
        }
        else
        {
            // toggle setting
            localSettings.Values["XhciInterruptModeration"] = IMOD.IsOn ? 1 : 0;

            // remove infobar
            DevicesInfo.Children.Clear();

            // add infobar
            DevicesInfo.Children.Add(new InfoBar
            {
                Title = IMOD.IsOn ? "Successfully enabled XHCI Interrupt Moderation (IMOD)." : "Successfully disabled XHCI Interrupt Moderation (IMOD).",
                IsClosable = false,
                IsOpen = true,
                Severity = InfoBarSeverity.Success,
                Margin = new Thickness(5)
            });

            // delay
            await Task.Delay(2000);

            // remove infobar
            DevicesInfo.Children.Clear();
        }
    }
}