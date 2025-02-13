using System.Diagnostics;

namespace AutoOS.Views.Installer;

public sealed partial class ServicesPage : Page
{
    private bool isInitializingWIFIState = true;
    private bool isInitializingBluetoothState = true;
    private bool isInitializingCameraState = true;
    private bool isInitializingSnippingState = true;
    private bool isInitializingStartMenuState = true;
    private bool isInitializingTaskManagerState = true;
    private bool isInitializingLaptopState = true;
    private bool isInitializingFACEITState = true;
    private bool isInitializingAMDVRRState = true;
    private string list = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "Service-list-builder", "lists.ini");
    private string[] listContent = File.ReadAllLines(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "Service-list-builder", "lists.ini"));
   
    public ServicesPage()
    {
        InitializeComponent();

        Process.Start(new ProcessStartInfo
        {
            FileName = "cmd.exe", 
            Arguments = $"/c takeown /f \"{list}\" & icacls \"{list}\" /grant Everyone:F /T /C /Q",
            UseShellExecute = false, 
            CreateNoWindow = true
        });

        GetWIFIState();
        GetBluetoothState();
        GetCameraState();
        GetSnippingState();
        GetStartMenuState();
        GetTaskManagerState();
        GetLaptopState();
        GetFACEITState();
        GetAMDVRRState();
    }

    private void GetWIFIState()
    {
        // define services and drivers
        var services = new[] { "WlanSvc", "Dhcp", "EventLog", "Netman", "NetSetupSvc", "NlaSvc", "Wcmsvc" };
        var drivers = new[] { "# tdx", "# vwififlt", "# Netwtw10", "# Netwtw14" };

        // check state
        WIFI.IsChecked = services.All(service => listContent.Any(line => line.Trim() == service))
                         && drivers.All(driver => listContent.Any(line => line.Trim() == driver));

        isInitializingWIFIState = false;
    }

    private async void WIFI_Checked(object sender, RoutedEventArgs e)
    {
        if (isInitializingWIFIState) return;

        // read list
        var lines = await File.ReadAllLinesAsync(list);

        // define services and drivers
        var services = new[] { "WlanSvc", "Dhcp", "EventLog", "Netman", "NetSetupSvc", "NlaSvc", "Wcmsvc" };
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
    }

    private void GetBluetoothState()
    {
        // define services and drivers
        var services = new[] { "BluetoothUserService", "BTAGService", "BthAvctpSvc", "bthserv", "DevicesFlowUserSvc", "DsmSvc", "NcbService", "SystemEventsBroker", "WFDSConMgrSvc" };
        var drivers = new[] { "# BthA2dp", "# BthEnum", "# BthHFAud", "# BthHFEnum", "# BthLEEnum", "# BthMini", "# BTHMODEM", "# BthPan", "# BTHPORT", "# BTHUSB", "# HidBth", "# ibtusb", "# Microsoft_Bluetooth_AvrcpTransport", "# RFCOMM" };
        
        // check state
        Bluetooth.IsChecked = services.All(service => listContent.Any(line => line.Trim() == service))
                         && drivers.All(driver => listContent.Any(line => line.Trim() == driver));

        isInitializingBluetoothState = false;
    }

    private async void Bluetooth_Checked(object sender, RoutedEventArgs e)
    {
        if (isInitializingBluetoothState) return;

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
    }

    private void GetCameraState()
    {
        // define services and drivers
        var drivers = new[] { "# swenum" };

        // check state
        Camera.IsChecked = drivers.All(driver => listContent.Any(line => line.Trim() == driver));

        isInitializingCameraState = false;
    }

    private async void Camera_Checked(object sender, RoutedEventArgs e)
    {
        if (isInitializingCameraState) return;

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
    }

    private void GetSnippingState()
    {
        // define services and drivers
        var services = new[] { "cbdhsvc", "CaptureService" };

        // check state
        Snipping.IsChecked = services.All(service => listContent.Any(line => line.Trim() == service));

        isInitializingSnippingState = false;
    }

    private async void Snipping_Checked(object sender, RoutedEventArgs e)
    {
        if (isInitializingSnippingState) return;

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
    }


    private void GetStartMenuState()
    {
        // define services and drivers
        var services = new[] { "UdkUserSvc" };
        var binaries = new[] { "# \\Windows\\System32\\ctfmon.exe", "# \\Windows\\System32\\RuntimeBroker.exe", "# \\Windows\\SystemApps\\ShellExperienceHost_cw5n1h2txyewy\\ShellExperienceHost.exe", "# \\Windows\\SystemApps\\Microsoft.Windows.StartMenuExperienceHost_cw5n1h2txyewy\\StartMenuExperienceHost.exe" };

        // check state
        StartMenu.IsChecked = services.All(service => listContent.Any(line => line.Trim() == service))
                         && binaries.All(driver => listContent.Any(line => line.Trim() == driver));

        isInitializingStartMenuState = false;
    }

    private async void StartMenu_Checked(object sender, RoutedEventArgs e)
    {
        if (isInitializingStartMenuState) return;

        // read list
        var lines = await File.ReadAllLinesAsync(list);

        // define services and binaries
        var services = new[] { "UdkUserSvc" };
        var binaries = new[]
        {
        "\\Windows\\System32\\ctfmon.exe",
        "\\Windows\\System32\\RuntimeBroker.exe",
        "\\Windows\\SystemApps\\ShellExperienceHost_cw5n1h2txyewy\\ShellExperienceHost.exe",
        "\\Windows\\SystemApps\\Microsoft.Windows.StartMenuExperienceHost_cw5n1h2txyewy\\StartMenuExperienceHost.exe"
    };

        // make changes
        bool isChecked = StartMenu.IsChecked == true;
        for (int i = 0; i < lines.Length; i++)
        {
            if (services.Contains(lines[i].Trim().TrimStart('#', ' ')))
                lines[i] = (isChecked ? lines[i].TrimStart('#', ' ') : "# " + lines[i].TrimStart('#', ' ')).Trim();
            if (binaries.Contains(lines[i].Trim().TrimStart('#', ' ')))
                lines[i] = (isChecked ? "# " + lines[i] : lines[i].TrimStart('#')).Trim();
        }

        // write changes
        await File.WriteAllLinesAsync(list, lines);
    }

    private void GetTaskManagerState()
    {
        // define services and drivers
        var drivers = new[] { "# pcw" };

        // check state
        TaskManager.IsChecked = drivers.All(driver => listContent.Any(line => line.Trim() == driver));

        isInitializingTaskManagerState = false;
    }

    private async void TaskManager_Checked(object sender, RoutedEventArgs e)
    {
        if (isInitializingTaskManagerState) return;

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
    }

    private void GetLaptopState()
    {
        // define services and drivers
        var drivers = new[] { "# msisadrv" };

        // check state
        Laptop.IsChecked = drivers.All(driver => listContent.Any(line => line.Trim() == driver));

        isInitializingLaptopState = false;
    }

    private async void Laptop_Checked(object sender, RoutedEventArgs e)
    {
        if (isInitializingLaptopState) return;

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
    }

    private void GetFACEITState()
    {
        // define services and drivers
        var drivers = new[] { "# FileCrypt" };

        // check state
        FACEIT.IsChecked = drivers.All(driver => listContent.Any(line => line.Trim() == driver));

        isInitializingFACEITState = false;
    }

    private async void FACEIT_Checked(object sender, RoutedEventArgs e)
    {
        if (isInitializingFACEITState) return;

        // read list
        var lines = await File.ReadAllLinesAsync(list);

        // define drivers
        var drivers = new[] { "FileCrypt" };

        // make changes
        bool isChecked = FACEIT.IsChecked == true;
        for (int i = 0; i < lines.Length; i++)
        {
            if (drivers.Contains(lines[i].Trim().TrimStart('#', ' ')))
                lines[i] = (isChecked ? "# " + lines[i] : lines[i].TrimStart('#')).Trim();
        }

        // write changes
        await File.WriteAllLinesAsync(list, lines);
    }

    private void GetAMDVRRState()
    {
        // define services and drivers
        var services = new[] { "AMD External Events Utility" };

        // check state
        AMDVRR.IsChecked = services.All(service => listContent.Any(line => line.Trim() == service));

        isInitializingAMDVRRState = false;
    }

    private async void AMDVRR_Checked(object sender, RoutedEventArgs e)
    {
        if (isInitializingAMDVRRState) return;

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
    }
}

