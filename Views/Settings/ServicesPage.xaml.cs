using Microsoft.Win32;
using System.Diagnostics;
using System.Management;
using System.ServiceProcess;

namespace AutoOS.Views.Settings;

public sealed partial class ServicesPage : Page
{
    private bool isInitializingServicesState = true;
    private bool isInitializingWIFIState = true;
    private bool isInitializingBluetoothState = true;
    private bool isInitializingCameraState = true;
    private bool isInitializingSnippingState = true;
    private bool isInitializingTaskManagerState = true;
    private bool isInitializingLaptopState = true;
    private bool isInitializingGTAState = true;
    private bool isInitializingAMDVRRState = true;
    private string list = Path.Combine(PathHelper.GetAppDataFolderPath(), "Service-list-builder", "lists.ini");
    private string nsudoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "NSudo", "NSudoLC.exe");

    public ServicesPage()
    {
        InitializeComponent();
        if (!File.Exists(Path.Combine(PathHelper.GetAppDataFolderPath(), "Service-list-builder", "lists.ini")))
        {
            Directory.CreateDirectory(Path.Combine(PathHelper.GetAppDataFolderPath(), "Service-list-builder"));
            File.Copy(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "Service-list-builder", "lists.ini"), Path.Combine(PathHelper.GetAppDataFolderPath(), "Service-list-builder", "lists.ini"));
        }
        GetServicesState();
        GetWIFIState();
        GetBluetoothState();
        GetCameraState();
        GetSnippingState();
        GetTaskManagerState();
        GetLaptopState();
        GetGTAState();
        GetAMDVRRState();
    }

    private void GetServicesState()
    {
        // check state
        using (var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\Beep"))
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
                Title = Services.IsOn ? "Successfully enabled Services & Drivers. A restart is required to apply the change."
                                      : "Successfully disabled Services & Drivers. A restart is required to apply the change.",
                IsClosable = false,
                IsOpen = true,
                Severity = InfoBarSeverity.Success,
                Margin = new Thickness(5)
            };
            infoBar.ActionButton = new Button
            {
                Content = "Restart",
                HorizontalAlignment = HorizontalAlignment.Right
            };
            ((Button)infoBar.ActionButton).Click += (s, args) => Process.Start("shutdown", "/r /f /t 0");

            ServiceInfo.Children.Add(infoBar);
        }
        isInitializingServicesState = false;
    }

    private async void Services_Toggled(object sender, RoutedEventArgs e)
    {
        if (isInitializingServicesState) return;

        // remove infobar
        ServiceInfo.Children.Clear();

        // add infobar
        ServiceInfo.Children.Add(new InfoBar
        {
            Title = Services.IsOn ? "Enabling Services & Drivers..." : "Disabling Services & Drivers...",
            IsClosable = false,
            IsOpen = true,
            Severity = InfoBarSeverity.Informational,
            Margin = new Thickness(5)
        });
        
        try
        {
            // get latest build
            string folderName = Directory.GetDirectories(Path.Combine(PathHelper.GetAppDataFolderPath(), "Service-list-builder", "build")).OrderByDescending(d => Directory.GetLastWriteTime(d)).FirstOrDefault()?.Split('\\').Last();

            // toggle services
            await Process.Start(new ProcessStartInfo { FileName = nsudoPath, Arguments = $"-U:T -P:E -Wait -ShowWindowMode:Hide \"{Path.Combine(PathHelper.GetAppDataFolderPath(), "Service-list-builder", "build", folderName, Services.IsOn ? "Services-Enable.bat" : "Services-Disable.bat")}\"", CreateNoWindow = true }).WaitForExitAsync();
        }
        catch
        {
            // build service list
            await Process.Start(new ProcessStartInfo { FileName = nsudoPath, Arguments = $@"-U:T -P:E -Wait -ShowWindowMode:Hide ""{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "Service-list-builder", "service-list-builder.exe")}"" --config ""{Path.Combine(PathHelper.GetAppDataFolderPath(), "Service-list-builder", "lists.ini")}"" --disable-service-warning --output-dir ""{Path.Combine(PathHelper.GetAppDataFolderPath(), "Service-list-builder", "build")}""", CreateNoWindow = true }).WaitForExitAsync();

            // get latest build
            string folderName = Directory.GetDirectories(Path.Combine(PathHelper.GetAppDataFolderPath(), "Service-list-builder", "build")).OrderByDescending(d => Directory.GetLastWriteTime(d)).FirstOrDefault()?.Split('\\').Last();

            // toggle services
            await Process.Start(new ProcessStartInfo { FileName = nsudoPath, Arguments = $"-U:T -P:E -Wait -ShowWindowMode:Hide \"{Path.Combine(PathHelper.GetAppDataFolderPath(), "Service-list-builder", "build", folderName, Services.IsOn ? "Services-Enable.bat" : "Services-Disable.bat")}\"", CreateNoWindow = true }).WaitForExitAsync();
        }

        // remove infobar
        ServiceInfo.Children.Clear();

        // add infobar
        var infoBar = new InfoBar
        {
            Title = Services.IsOn ? "Successfully enabled Services & Drivers." : "Successfully disabled Services & Drivers.",
            IsClosable = false,
            IsOpen = true,
            Severity = InfoBarSeverity.Success,
            Margin = new Thickness(5)
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
            Process.Start("shutdown", "/r /f /t 0");
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
        var services = new[] { "WlanSvc", "Dhcp", "EventLog", "Netman", "NetSetupSvc", "NlaSvc", "Wcmsvc", "WinHttpAutoProxySvc" };
        var drivers = new[] { "# tdx", "# vwififlt", "# Netwtw10", "# Netwtw14" };

        // check state
        WIFI.IsChecked = services.All(service => File.ReadAllLines(list).Any(line => line.Trim() == service))
                         && drivers.All(driver => File.ReadAllLines(list).Any(line => line.Trim() == driver));

        isInitializingWIFIState = false;
    }

    private async void WIFI_Checked(object sender, RoutedEventArgs e)
    {
        if (isInitializingWIFIState) return;

        // remove infobar
        ServiceInfo.Children.Clear();

        // add infobar
        ServiceInfo.Children.Add(new InfoBar
        {
            Title = WIFI.IsChecked == true ? "Enabling WiFi support..." : "Disabling WiFi support...",
            IsClosable = false,
            IsOpen = true,
            Severity = InfoBarSeverity.Informational,
            Margin = new Thickness(5)
        });

        // read list
        var lines = await File.ReadAllLinesAsync(list);

        // define services and drivers
        var services = new[] { "WlanSvc", "Dhcp", "EventLog", "Netman", "NetSetupSvc", "NlaSvc", "Wcmsvc", "WinHttpAutoProxySvc" };
        var drivers = new[] { "tdx", "vwififlt", "Netwtw10", "Netwtw14" };

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
            await Process.Start(new ProcessStartInfo { FileName = nsudoPath, Arguments = $"-U:T -P:E -Wait -ShowWindowMode:Hide \"{Path.Combine(PathHelper.GetAppDataFolderPath(), "Service-list-builder", "build", folderName, "Services-Enable.bat")}\"", CreateNoWindow = true }).WaitForExitAsync();
        }

        // build service list
        await Process.Start(new ProcessStartInfo { FileName = nsudoPath, Arguments = $@"-U:T -P:E -Wait -ShowWindowMode:Hide ""{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "Service-list-builder", "service-list-builder.exe")}"" --config ""{Path.Combine(PathHelper.GetAppDataFolderPath(), "Service-list-builder", "lists.ini")}"" --disable-service-warning --output-dir ""{Path.Combine(PathHelper.GetAppDataFolderPath(), "Service-list-builder", "build")}""", CreateNoWindow = true }).WaitForExitAsync();

        if (!Services.IsOn)
        {
            // get latest build
            string folderName = Directory.GetDirectories(Path.Combine(PathHelper.GetAppDataFolderPath(), "Service-list-builder", "build")).OrderByDescending(d => Directory.GetLastWriteTime(d)).FirstOrDefault()?.Split('\\').Last();

            // disable services
            await Process.Start(new ProcessStartInfo { FileName = nsudoPath, Arguments = $"-U:T -P:E -Wait -ShowWindowMode:Hide \"{Path.Combine(PathHelper.GetAppDataFolderPath(), "Service-list-builder", "build", folderName, "Services-Disable.bat")}\"", CreateNoWindow = true }).WaitForExitAsync();

            // remove infobar
            ServiceInfo.Children.Clear();

            // add infobar
            var infoBar = new InfoBar
            {
                Title = WIFI.IsChecked == true ? "Successfully enabled WiFi support. A restart is required to apply the change." : "Successfully disabled WiFi support. A restart is required to apply the change.",
                IsClosable = false,
                IsOpen = true,
                Severity = InfoBarSeverity.Success,
                Margin = new Thickness(5)
            };

            // add restart button
            infoBar.ActionButton = new Button
            {
                Content = "Restart",
                HorizontalAlignment = HorizontalAlignment.Right
            };
            ((Button)infoBar.ActionButton).Click += (s, args) =>
            Process.Start("shutdown", "/r /f /t 0");

            ServiceInfo.Children.Add(infoBar);
        }
        else
        {
            // remove infobar
            ServiceInfo.Children.Clear();

            // add infobar
            ServiceInfo.Children.Add(new InfoBar
            {
                Title = WIFI.IsChecked == true ? "Successfully enabled WiFi support." : "Successfully disabled WiFi support.",
                IsClosable = false,
                IsOpen = true,
                Severity = InfoBarSeverity.Success,
                Margin = new Thickness(5)
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
        var services = new[] { "BluetoothUserService", "BTAGService", "BthAvctpSvc", "bthserv", "DevicesFlowUserSvc", "DsmSvc", "NcbService", "SystemEventsBroker", "WFDSConMgrSvc" };
        var drivers = new[] { "# BthA2dp", "# BthEnum", "# BthHFAud", "# BthHFEnum", "# BthLEEnum", "# BthMini", "# BTHMODEM", "# BthPan", "# BTHPORT", "# BTHUSB", "# HidBth", "# ibtusb", "# Microsoft_Bluetooth_AvrcpTransport", "# RFCOMM" };

        // check state
        Bluetooth.IsChecked = services.All(service => File.ReadAllLines(list).Any(line => line.Trim() == service))
                         && drivers.All(driver => File.ReadAllLines(list).Any(line => line.Trim() == driver));

        isInitializingBluetoothState = false;
    }

    private async void Bluetooth_Checked(object sender, RoutedEventArgs e)
    {
        if (isInitializingBluetoothState) return;

        // remove infobar
        ServiceInfo.Children.Clear();

        // add infobar
        ServiceInfo.Children.Add(new InfoBar
        {
            Title = Bluetooth.IsChecked == true ? "Enabling Bluetooth support..." : "Disabling Bluetooth support...",
            IsClosable = false,
            IsOpen = true,
            Severity = InfoBarSeverity.Informational,
            Margin = new Thickness(5)
        });

        // read list
        var lines = await File.ReadAllLinesAsync(list);

        // define services and drivers
        var services = new[] { "BluetoothUserService", "BTAGService", "BthAvctpSvc", "bthserv", "DevicesFlowUserSvc", "DsmSvc", "NcbService", "SystemEventsBroker", "WFDSConMgrSvc" };
        var drivers = new[] { "BthA2dp", "BthEnum", "BthHFAud", "BthHFEnum", "BthLEEnum", "BthMini", "BTHMODEM", "BthPan", "BTHPORT", "BTHUSB", "HidBth", "ibtusb", "Microsoft_Bluetooth_AvrcpTransport", "RFCOMM" };

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
            await Process.Start(new ProcessStartInfo { FileName = nsudoPath, Arguments = $"-U:T -P:E -Wait -ShowWindowMode:Hide \"{Path.Combine(PathHelper.GetAppDataFolderPath(), "Service-list-builder", "build", folderName, "Services-Enable.bat")}\"", CreateNoWindow = true }).WaitForExitAsync();
        }

        // build service list
        await Process.Start(new ProcessStartInfo { FileName = nsudoPath, Arguments = $@"-U:T -P:E -Wait -ShowWindowMode:Hide ""{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "Service-list-builder", "service-list-builder.exe")}"" --config ""{Path.Combine(PathHelper.GetAppDataFolderPath(), "Service-list-builder", "lists.ini")}"" --disable-service-warning --output-dir ""{Path.Combine(PathHelper.GetAppDataFolderPath(), "Service-list-builder", "build")}""", CreateNoWindow = true }).WaitForExitAsync();

        if (!Services.IsOn)
        {
            // get latest build
            string folderName = Directory.GetDirectories(Path.Combine(PathHelper.GetAppDataFolderPath(), "Service-list-builder", "build")).OrderByDescending(d => Directory.GetLastWriteTime(d)).FirstOrDefault()?.Split('\\').Last();

            // disable services
            await Process.Start(new ProcessStartInfo { FileName = nsudoPath, Arguments = $"-U:T -P:E -Wait -ShowWindowMode:Hide \"{Path.Combine(PathHelper.GetAppDataFolderPath(), "Service-list-builder", "build", folderName, "Services-Disable.bat")}\"", CreateNoWindow = true }).WaitForExitAsync();

            // remove infobar
            ServiceInfo.Children.Clear();

            // add infobar
            var infoBar = new InfoBar
            {
                Title = Bluetooth.IsChecked == true ? "Successfully enabled Bluetooth support. A restart is required to apply the change." : "Successfully disabled Bluetooth support. A restart is required to apply the change.",
                IsClosable = false,
                IsOpen = true,
                Severity = InfoBarSeverity.Success,
                Margin = new Thickness(5)
            };

            // add restart button
            infoBar.ActionButton = new Button
            {
                Content = "Restart",
                HorizontalAlignment = HorizontalAlignment.Right
            };
            ((Button)infoBar.ActionButton).Click += (s, args) =>
            Process.Start("shutdown", "/r /f /t 0");

            ServiceInfo.Children.Add(infoBar);
        }
        else
        {
            // remove infobar
            ServiceInfo.Children.Clear();

            // add infobar
            ServiceInfo.Children.Add(new InfoBar
            {
                Title = Bluetooth.IsChecked == true ? "Successfully enabled Bluetooth support." : "Successfully disabled Bluetooth support.",
                IsClosable = false,
                IsOpen = true,
                Severity = InfoBarSeverity.Success,
                Margin = new Thickness(5)
            });

            // delay
            await Task.Delay(2000);

            // remove infobar
            ServiceInfo.Children.Clear();
        }
    }

    private void GetCameraState()
    {
        // define services and drivers
        var drivers = new[] { "# swenum" };

        // check state
        Camera.IsChecked = drivers.All(driver => File.ReadAllLines(list).Any(line => line.Trim() == driver));

        isInitializingCameraState = false;
    }

    private async void Camera_Checked(object sender, RoutedEventArgs e)
    {
        if (isInitializingCameraState) return;

        // remove infobar
        ServiceInfo.Children.Clear();

        // add infobar
        ServiceInfo.Children.Add(new InfoBar
        {
            Title = Camera.IsChecked == true ? "Enabling Camera support..." : "Disabling Camera support...",
            IsClosable = false,
            IsOpen = true,
            Severity = InfoBarSeverity.Informational,
            Margin = new Thickness(5)
        });

        // read list
        var lines = await File.ReadAllLinesAsync(list);

        // define drivers
        var drivers = new[] { "swenum" };

        // make changes
        bool isChecked = Camera.IsChecked == true;
        for (int i = 0; i < lines.Length; i++)
        {
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
            await Process.Start(new ProcessStartInfo { FileName = nsudoPath, Arguments = $"-U:T -P:E -Wait -ShowWindowMode:Hide \"{Path.Combine(PathHelper.GetAppDataFolderPath(), "Service-list-builder", "build", folderName, "Services-Enable.bat")}\"", CreateNoWindow = true }).WaitForExitAsync();
        }

        // build service list
        await Process.Start(new ProcessStartInfo { FileName = nsudoPath, Arguments = $@"-U:T -P:E -Wait -ShowWindowMode:Hide ""{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "Service-list-builder", "service-list-builder.exe")}"" --config ""{Path.Combine(PathHelper.GetAppDataFolderPath(), "Service-list-builder", "lists.ini")}"" --disable-service-warning --output-dir ""{Path.Combine(PathHelper.GetAppDataFolderPath(), "Service-list-builder", "build")}""", CreateNoWindow = true }).WaitForExitAsync();

        if (!Services.IsOn)
        {
            // get latest build
            string folderName = Directory.GetDirectories(Path.Combine(PathHelper.GetAppDataFolderPath(), "Service-list-builder", "build")).OrderByDescending(d => Directory.GetLastWriteTime(d)).FirstOrDefault()?.Split('\\').Last();

            // disable services
            await Process.Start(new ProcessStartInfo { FileName = nsudoPath, Arguments = $"-U:T -P:E -Wait -ShowWindowMode:Hide \"{Path.Combine(PathHelper.GetAppDataFolderPath(), "Service-list-builder", "build", folderName, "Services-Disable.bat")}\"", CreateNoWindow = true }).WaitForExitAsync();

            // remove infobar
            ServiceInfo.Children.Clear();

            // add infobar
            var infoBar = new InfoBar
            {
                Title = Camera.IsChecked == true ? "Successfully enabled Camera support. A restart is required to apply the change." : "Successfully disabled Camera support. A restart is required to apply the change.",
                IsClosable = false,
                IsOpen = true,
                Severity = InfoBarSeverity.Success,
                Margin = new Thickness(5)
            };

            // add restart button
            infoBar.ActionButton = new Button
            {
                Content = "Restart",
                HorizontalAlignment = HorizontalAlignment.Right
            };
            ((Button)infoBar.ActionButton).Click += (s, args) =>
            Process.Start("shutdown", "/r /f /t 0");

            ServiceInfo.Children.Add(infoBar);
        }
        else
        {
            // remove infobar
            ServiceInfo.Children.Clear();

            // add infobar
            ServiceInfo.Children.Add(new InfoBar
            {
                Title = Camera.IsChecked == true ? "Successfully enabled Camera support." : "Successfully disabled Camera support.",
                IsClosable = false,
                IsOpen = true,
                Severity = InfoBarSeverity.Success,
                Margin = new Thickness(5)
            });

            // delay
            await Task.Delay(2000);

            // remove infobar
            ServiceInfo.Children.Clear();
        }
    }

    private void GetSnippingState()
    {
        // define services and drivers
        var services = new[] { "cbdhsvc", "CaptureService" };

        // check state
        Snipping.IsChecked = services.All(service => File.ReadAllLines(list).Any(line => line.Trim() == service));

        isInitializingSnippingState = false;
    }

    private async void Snipping_Checked(object sender, RoutedEventArgs e)
    {
        if (isInitializingSnippingState) return;

        // remove infobar
        ServiceInfo.Children.Clear();

        // add infobar
        ServiceInfo.Children.Add(new InfoBar
        {
            Title = Snipping.IsChecked == true ? "Enabling Snipping Tool support..." : "Disabling Snipping Tool support...",
            IsClosable = false,
            IsOpen = true,
            Severity = InfoBarSeverity.Informational,
            Margin = new Thickness(5)
        });

        // read list
        var lines = await File.ReadAllLinesAsync(list);

        // define services
        var services = new[] { "cbdhsvc", "CaptureService" };

        // make changes
        bool isChecked = Snipping.IsChecked == true;
        for (int i = 0; i < lines.Length; i++)
        {
            if (services.Contains(lines[i].Trim().TrimStart('#', ' ')))
                lines[i] = (isChecked ? lines[i].TrimStart('#', ' ') : "# " + lines[i].TrimStart('#', ' ')).Trim();
        }

        // write changes
        await File.WriteAllLinesAsync(list, lines);

        if (!Services.IsOn)
        {
            // get latest build
            string folderName = Directory.GetDirectories(Path.Combine(PathHelper.GetAppDataFolderPath(), "Service-list-builder", "build")).OrderByDescending(d => Directory.GetLastWriteTime(d)).FirstOrDefault()?.Split('\\').Last();

            // enable services
            await Process.Start(new ProcessStartInfo { FileName = nsudoPath, Arguments = $"-U:T -P:E -Wait -ShowWindowMode:Hide \"{Path.Combine(PathHelper.GetAppDataFolderPath(), "Service-list-builder", "build", folderName, "Services-Enable.bat")}\"", CreateNoWindow = true }).WaitForExitAsync();
        }

        // build service list
        await Process.Start(new ProcessStartInfo { FileName = nsudoPath, Arguments = $@"-U:T -P:E -Wait -ShowWindowMode:Hide ""{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "Service-list-builder", "service-list-builder.exe")}"" --config ""{Path.Combine(PathHelper.GetAppDataFolderPath(), "Service-list-builder", "lists.ini")}"" --disable-service-warning --output-dir ""{Path.Combine(PathHelper.GetAppDataFolderPath(), "Service-list-builder", "build")}""", CreateNoWindow = true }).WaitForExitAsync();

        if (!Services.IsOn)
        {
            // get latest build
            string folderName = Directory.GetDirectories(Path.Combine(PathHelper.GetAppDataFolderPath(), "Service-list-builder", "build")).OrderByDescending(d => Directory.GetLastWriteTime(d)).FirstOrDefault()?.Split('\\').Last();

            // disable services
            await Process.Start(new ProcessStartInfo { FileName = nsudoPath, Arguments = $"-U:T -P:E -Wait -ShowWindowMode:Hide \"{Path.Combine(PathHelper.GetAppDataFolderPath(), "Service-list-builder", "build", folderName, "Services-Disable.bat")}\"", CreateNoWindow = true }).WaitForExitAsync();

            // remove infobar
            ServiceInfo.Children.Clear();

            // add infobar
            var infoBar = new InfoBar
            {
                Title = Snipping.IsChecked == true ? "Successfully enabled Snipping Tool support. A restart is required to apply the change." : "Successfully disabled Snipping Tool support. A restart is required to apply the change.",
                IsClosable = false,
                IsOpen = true,
                Severity = InfoBarSeverity.Success,
                Margin = new Thickness(5)
            };

            // add restart button
            infoBar.ActionButton = new Button
            {
                Content = "Restart",
                HorizontalAlignment = HorizontalAlignment.Right
            };
            ((Button)infoBar.ActionButton).Click += (s, args) =>
            Process.Start("shutdown", "/r /f /t 0");

            ServiceInfo.Children.Add(infoBar);
        }
        else
        {
            // remove infobar
            ServiceInfo.Children.Clear();

            // add infobar
            ServiceInfo.Children.Add(new InfoBar
            {
                Title = Snipping.IsChecked == true ? "Successfully enabled Snipping Tool support." : "Successfully disabled Snipping Tool support.",
                IsClosable = false,
                IsOpen = true,
                Severity = InfoBarSeverity.Success,
                Margin = new Thickness(5)
            });

            // delay
            await Task.Delay(2000);

            // remove infobar
            ServiceInfo.Children.Clear();
        }
    }

    private void GetTaskManagerState()
    {
        // define services and drivers
        var drivers = new[] { "# pcw" };

        // check state
        TaskManager.IsChecked = drivers.All(driver => File.ReadAllLines(list).Any(line => line.Trim() == driver));

        isInitializingTaskManagerState = false;
    }

    private async void TaskManager_Checked(object sender, RoutedEventArgs e)
    {
        if (isInitializingTaskManagerState) return;

        // remove infobar
        ServiceInfo.Children.Clear();

        // add infobar
        ServiceInfo.Children.Add(new InfoBar
        {
            Title = TaskManager.IsChecked == true ? "Enabling Task Manager support..." : "Disabling Task Manager support...",
            IsClosable = false,
            IsOpen = true,
            Severity = InfoBarSeverity.Informational,
            Margin = new Thickness(5)
        });

        // read list
        var lines = await File.ReadAllLinesAsync(list);

        // define drivers
        var drivers = new[] { "pcw" };

        // make changes
        bool isChecked = TaskManager.IsChecked == true;
        for (int i = 0; i < lines.Length; i++)
        {
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
            await Process.Start(new ProcessStartInfo { FileName = nsudoPath, Arguments = $"-U:T -P:E -Wait -ShowWindowMode:Hide \"{Path.Combine(PathHelper.GetAppDataFolderPath(), "Service-list-builder", "build", folderName, "Services-Enable.bat")}\"", CreateNoWindow = true }).WaitForExitAsync();
        }

        // build service list
        await Process.Start(new ProcessStartInfo { FileName = nsudoPath, Arguments = $@"-U:T -P:E -Wait -ShowWindowMode:Hide ""{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "Service-list-builder", "service-list-builder.exe")}"" --config ""{Path.Combine(PathHelper.GetAppDataFolderPath(), "Service-list-builder", "lists.ini")}"" --disable-service-warning --output-dir ""{Path.Combine(PathHelper.GetAppDataFolderPath(), "Service-list-builder", "build")}""", CreateNoWindow = true }).WaitForExitAsync();

        if (!Services.IsOn)
        {
            // get latest build
            string folderName = Directory.GetDirectories(Path.Combine(PathHelper.GetAppDataFolderPath(), "Service-list-builder", "build")).OrderByDescending(d => Directory.GetLastWriteTime(d)).FirstOrDefault()?.Split('\\').Last();

            // disable services
            await Process.Start(new ProcessStartInfo { FileName = nsudoPath, Arguments = $"-U:T -P:E -Wait -ShowWindowMode:Hide \"{Path.Combine(PathHelper.GetAppDataFolderPath(), "Service-list-builder", "build", folderName, "Services-Disable.bat")}\"", CreateNoWindow = true }).WaitForExitAsync();

            // remove infobar
            ServiceInfo.Children.Clear();

            // add infobar
            var infoBar = new InfoBar
            {
                Title = TaskManager.IsChecked == true ? "Successfully enabled Task Manager support. A restart is required to apply the change." : "Successfully disabled Task Manager support. A restart is required to apply the change.",
                IsClosable = false,
                IsOpen = true,
                Severity = InfoBarSeverity.Success,
                Margin = new Thickness(5)
            };

            // add restart button
            infoBar.ActionButton = new Button
            {
                Content = "Restart",
                HorizontalAlignment = HorizontalAlignment.Right
            };
            ((Button)infoBar.ActionButton).Click += (s, args) =>
            Process.Start("shutdown", "/r /f /t 0");

            ServiceInfo.Children.Add(infoBar);
        }
        else
        {
            // remove infobar
            ServiceInfo.Children.Clear();

            // add infobar
            ServiceInfo.Children.Add(new InfoBar
            {
                Title = TaskManager.IsChecked == true ? "Successfully enabled Task Manager support." : "Successfully disabled Task Manager support.",
                IsClosable = false,
                IsOpen = true,
                Severity = InfoBarSeverity.Success,
                Margin = new Thickness(5)
            });

            // delay
            await Task.Delay(2000);

            // remove infobar
            ServiceInfo.Children.Clear();
        }
    }

    private void GetLaptopState()
    {
        bool isDesktop = new ManagementObjectSearcher("SELECT * FROM Win32_SystemEnclosure")
               .Get()
               .Cast<ManagementObject>()
               .Any(obj => ((ushort[])obj["ChassisTypes"])?.Any(type => new ushort[] { 3, 4, 5, 6, 7, 15, 16, 17 }.Contains(type)) == true);

        if (isDesktop)
        {
            Laptop_SettingsCard.Visibility = Visibility.Collapsed;
        }

        // define services and drivers
        var drivers = new[] { "# msisadrv" };

        // check state
        Laptop.IsChecked = drivers.All(driver => File.ReadAllLines(list).Any(line => line.Trim() == driver));

        isInitializingLaptopState = false;
    }

    private async void Laptop_Checked(object sender, RoutedEventArgs e)
    {
        if (isInitializingLaptopState) return;

        // remove infobar
        ServiceInfo.Children.Clear();

        // add infobar
        ServiceInfo.Children.Add(new InfoBar
        {
            Title = Laptop.IsChecked == true ? "Enabling Laptop Keyboard support..." : "Disabling Laptop Keyboard support...",
            IsClosable = false,
            IsOpen = true,
            Severity = InfoBarSeverity.Informational,
            Margin = new Thickness(5)
        });

        // read list
        var lines = await File.ReadAllLinesAsync(list);

        // define drivers
        var drivers = new[] { "msisadrv" };

        // make changes
        bool isChecked = Laptop.IsChecked == true;
        for (int i = 0; i < lines.Length; i++)
        {
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
            await Process.Start(new ProcessStartInfo { FileName = nsudoPath, Arguments = $"-U:T -P:E -Wait -ShowWindowMode:Hide \"{Path.Combine(PathHelper.GetAppDataFolderPath(), "Service-list-builder", "build", folderName, "Services-Enable.bat")}\"", CreateNoWindow = true }).WaitForExitAsync();
        }

        // build service list
        await Process.Start(new ProcessStartInfo { FileName = nsudoPath, Arguments = $@"-U:T -P:E -Wait -ShowWindowMode:Hide ""{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "Service-list-builder", "service-list-builder.exe")}"" --config ""{Path.Combine(PathHelper.GetAppDataFolderPath(), "Service-list-builder", "lists.ini")}"" --disable-service-warning --output-dir ""{Path.Combine(PathHelper.GetAppDataFolderPath(), "Service-list-builder", "build")}""", CreateNoWindow = true }).WaitForExitAsync();

        if (!Services.IsOn)
        {
            // get latest build
            string folderName = Directory.GetDirectories(Path.Combine(PathHelper.GetAppDataFolderPath(), "Service-list-builder", "build")).OrderByDescending(d => Directory.GetLastWriteTime(d)).FirstOrDefault()?.Split('\\').Last();

            // disable services
            await Process.Start(new ProcessStartInfo { FileName = nsudoPath, Arguments = $"-U:T -P:E -Wait -ShowWindowMode:Hide \"{Path.Combine(PathHelper.GetAppDataFolderPath(), "Service-list-builder", "build", folderName, "Services-Disable.bat")}\"", CreateNoWindow = true }).WaitForExitAsync();

            // remove infobar
            ServiceInfo.Children.Clear();

            // add infobar
            var infoBar = new InfoBar
            {
                Title = Laptop.IsChecked == true ? "Successfully enabled Laptop Keyboard support. A restart is required to apply the change." : "Successfully disabled Laptop Keyboard support. A restart is required to apply the change.",
                IsClosable = false,
                IsOpen = true,
                Severity = InfoBarSeverity.Success,
                Margin = new Thickness(5)
            };

            // add restart button
            infoBar.ActionButton = new Button
            {
                Content = "Restart",
                HorizontalAlignment = HorizontalAlignment.Right
            };
            ((Button)infoBar.ActionButton).Click += (s, args) =>
            Process.Start("shutdown", "/r /f /t 0");

            ServiceInfo.Children.Add(infoBar);
        }
        else
        {
            // remove infobar
            ServiceInfo.Children.Clear();

            // add infobar
            ServiceInfo.Children.Add(new InfoBar
            {
                Title = Laptop.IsChecked == true ? "Successfully enabled Laptop Keyboard support." : "Successfully disabled Laptop Keyboard support.",
                IsClosable = false,
                IsOpen = true,
                Severity = InfoBarSeverity.Success,
                Margin = new Thickness(5)
            });

            // delay
            await Task.Delay(2000);

            // remove infobar
            ServiceInfo.Children.Clear();
        }
    }

    private void GetGTAState()
    {
        // define services and drivers
        var drivers = new[] { "# mssmbios" };

        // check state
        GTA.IsChecked = drivers.All(driver => File.ReadAllLines(list).Any(line => line.Trim() == driver));

        isInitializingGTAState = false;
    }

    private async void GTA_Checked(object sender, RoutedEventArgs e)
    {
        if (isInitializingGTAState) return;

        // remove infobar
        ServiceInfo.Children.Clear();

        // add infobar
        ServiceInfo.Children.Add(new InfoBar
        {
            Title = GTA.IsChecked == true ? "Enabling GTA support..." : "Disabling GTA support...",
            IsClosable = false,
            IsOpen = true,
            Severity = InfoBarSeverity.Informational,
            Margin = new Thickness(5)
        });

        // read list
        var lines = await File.ReadAllLinesAsync(list);

        // define drivers
        var drivers = new[] { "mssmbios" };

        // make changes
        bool isChecked = GTA.IsChecked == true;
        for (int i = 0; i < lines.Length; i++)
        {
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
            await Process.Start(new ProcessStartInfo { FileName = nsudoPath, Arguments = $"-U:T -P:E -Wait -ShowWindowMode:Hide \"{Path.Combine(PathHelper.GetAppDataFolderPath(), "Service-list-builder", "build", folderName, "Services-Enable.bat")}\"", CreateNoWindow = true }).WaitForExitAsync();
        }

        // build service list
        await Process.Start(new ProcessStartInfo { FileName = nsudoPath, Arguments = $@"-U:T -P:E -Wait -ShowWindowMode:Hide ""{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "Service-list-builder", "service-list-builder.exe")}"" --config ""{Path.Combine(PathHelper.GetAppDataFolderPath(), "Service-list-builder", "lists.ini")}"" --disable-service-warning --output-dir ""{Path.Combine(PathHelper.GetAppDataFolderPath(), "Service-list-builder", "build")}""", CreateNoWindow = true }).WaitForExitAsync();

        if (!Services.IsOn)
        {
            // get latest build
            string folderName = Directory.GetDirectories(Path.Combine(PathHelper.GetAppDataFolderPath(), "Service-list-builder", "build")).OrderByDescending(d => Directory.GetLastWriteTime(d)).FirstOrDefault()?.Split('\\').Last();

            // disable services
            await Process.Start(new ProcessStartInfo { FileName = nsudoPath, Arguments = $"-U:T -P:E -Wait -ShowWindowMode:Hide \"{Path.Combine(PathHelper.GetAppDataFolderPath(), "Service-list-builder", "build", folderName, "Services-Disable.bat")}\"", CreateNoWindow = true }).WaitForExitAsync();

            // remove infobar
            ServiceInfo.Children.Clear();

            // add infobar
            var infoBar = new InfoBar
            {
                Title = GTA.IsChecked == true ? "Successfully enabled GTA support. A restart is required to apply the change." : "Successfully disabled GTA support. A restart is required to apply the change.",
                IsClosable = false,
                IsOpen = true,
                Severity = InfoBarSeverity.Success,
                Margin = new Thickness(5)
            };

            // add restart button
            infoBar.ActionButton = new Button
            {
                Content = "Restart",
                HorizontalAlignment = HorizontalAlignment.Right
            };
            ((Button)infoBar.ActionButton).Click += (s, args) =>
            Process.Start("shutdown", "/r /f /t 0");

            ServiceInfo.Children.Add(infoBar);
        }
        else
        {
            // remove infobar
            ServiceInfo.Children.Clear();

            // add infobar
            ServiceInfo.Children.Add(new InfoBar
            {
                Title = GTA.IsChecked == true ? "Successfully enabled GTA support." : "Successfully disabled GTA support.",
                IsClosable = false,
                IsOpen = true,
                Severity = InfoBarSeverity.Success,
                Margin = new Thickness(5)
            });

            // delay
            await Task.Delay(2000);

            // remove infobar
            ServiceInfo.Children.Clear();
        }
    }

    private void GetAMDVRRState()
    {
        // define services and drivers
        var services = new[] { "AMD External Events Utility" };

        // check state
        AMDVRR.IsChecked = services.All(service => File.ReadAllLines(list).Any(line => line.Trim() == service));

        isInitializingAMDVRRState = false;
    }

    private async void AMDVRR_Checked(object sender, RoutedEventArgs e)
    {
        if (isInitializingAMDVRRState) return;

        // remove infobar
        ServiceInfo.Children.Clear();

        // add infobar
        ServiceInfo.Children.Add(new InfoBar
        {
            Title = AMDVRR.IsChecked == true ? "Enabling AMD Variable Refresh Rate (VRR) / FreeSync support..." : "Disabling AMD Variable Refresh Rate (VRR) / FreeSync support...",
            IsClosable = false,
            IsOpen = true,
            Severity = InfoBarSeverity.Informational,
            Margin = new Thickness(5)
        });

        // read list
        var lines = await File.ReadAllLinesAsync(list);

        // define services
        var services = new[] { "AMD External Events Utility" };

        // make changes
        bool isChecked = AMDVRR.IsChecked == true;
        for (int i = 0; i < lines.Length; i++)
        {
            if (services.Contains(lines[i].Trim().TrimStart('#', ' ')))
                lines[i] = (isChecked ? lines[i].TrimStart('#', ' ') : "# " + lines[i].TrimStart('#', ' ')).Trim();
        }

        // write changes
        await File.WriteAllLinesAsync(list, lines);

        if (!Services.IsOn)
        {
            // get latest build
            string folderName = Directory.GetDirectories(Path.Combine(PathHelper.GetAppDataFolderPath(), "Service-list-builder", "build")).OrderByDescending(d => Directory.GetLastWriteTime(d)).FirstOrDefault()?.Split('\\').Last();

            // enable services
            await Process.Start(new ProcessStartInfo { FileName = nsudoPath, Arguments = $"-U:T -P:E -Wait -ShowWindowMode:Hide \"{Path.Combine(PathHelper.GetAppDataFolderPath(), "Service-list-builder", "build", folderName, "Services-Enable.bat")}\"", CreateNoWindow = true }).WaitForExitAsync();
        }

        // build service list
        await Process.Start(new ProcessStartInfo { FileName = nsudoPath, Arguments = $@"-U:T -P:E -Wait -ShowWindowMode:Hide ""{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "Service-list-builder", "service-list-builder.exe")}"" --config ""{Path.Combine(PathHelper.GetAppDataFolderPath(), "Service-list-builder", "lists.ini")}"" --disable-service-warning --output-dir ""{Path.Combine(PathHelper.GetAppDataFolderPath(), "Service-list-builder", "build")}""", CreateNoWindow = true }).WaitForExitAsync();

        if (!Services.IsOn)
        {
            // get latest build
            string folderName = Directory.GetDirectories(Path.Combine(PathHelper.GetAppDataFolderPath(), "Service-list-builder", "build")).OrderByDescending(d => Directory.GetLastWriteTime(d)).FirstOrDefault()?.Split('\\').Last();

            // disable services
            await Process.Start(new ProcessStartInfo { FileName = nsudoPath, Arguments = $"-U:T -P:E -Wait -ShowWindowMode:Hide \"{Path.Combine(PathHelper.GetAppDataFolderPath(), "Service-list-builder", "build", folderName, "Services-Disable.bat")}\"", CreateNoWindow = true }).WaitForExitAsync();

            // remove infobar
            ServiceInfo.Children.Clear();

            // add infobar
            var infoBar = new InfoBar
            {
                Title = AMDVRR.IsChecked == true ? "Successfully enabled AMD Variable Refresh Rate (VRR) / FreeSync support. A restart is required to apply the change." : "Successfully disabled AMD Variable Refresh Rate (VRR) / FreeSync support. A restart is required to apply the change.",
                IsClosable = false,
                IsOpen = true,
                Severity = InfoBarSeverity.Success,
                Margin = new Thickness(5)
            };

            // add restart button
            infoBar.ActionButton = new Button
            {
                Content = "Restart",
                HorizontalAlignment = HorizontalAlignment.Right
            };
            ((Button)infoBar.ActionButton).Click += (s, args) =>
            Process.Start("shutdown", "/r /f /t 0");

            ServiceInfo.Children.Add(infoBar);
        }
        else
        {
            // remove infobar
            ServiceInfo.Children.Clear();

            // add infobar
            ServiceInfo.Children.Add(new InfoBar
            {
                Title = AMDVRR.IsChecked == true ? "Successfully enabled AMD Variable Refresh Rate (VRR) / FreeSync support." : "Successfully disabled AMD Variable Refresh Rate (VRR) / FreeSync support.",
                IsClosable = false,
                IsOpen = true,
                Severity = InfoBarSeverity.Success,
                Margin = new Thickness(5)
            });

            // delay
            await Task.Delay(2000);

            // remove infobar
            ServiceInfo.Children.Clear();
        }
    }
}