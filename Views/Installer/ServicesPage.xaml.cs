using System.Management;

namespace AutoOS.Views.Installer;

public sealed partial class ServicesPage : Page
{
    private bool isInitializingWIFIState = true;
    private bool isInitializingBluetoothState = true;
    private bool isInitializingCameraState = true;
    private bool isInitializingTaskManagerState = true;
    private bool isInitializingLaptopState = true;
    private bool isInitializingAMDVRRState = true;
    private readonly string list = Path.Combine(PathHelper.GetAppDataFolderPath(), "Service-list-builder", "lists.ini");

    public ServicesPage()
    {
        InitializeComponent();
        if (!File.Exists(Path.Combine(PathHelper.GetAppDataFolderPath(), "Service-list-builder", "lists.ini")))
        {
            Directory.CreateDirectory(Path.Combine(PathHelper.GetAppDataFolderPath(), "Service-list-builder"));
            File.Copy(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "Service-list-builder", "lists.ini"), Path.Combine(PathHelper.GetAppDataFolderPath(), "Service-list-builder", "lists.ini"));
        }
        GetWIFIState();
        GetBluetoothState();
        GetCameraState();
        GetTaskManagerState();
        GetLaptopState();
        GetAMDVRRState();
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
    }

    private void GetBluetoothState()
    {
        // define services and drivers
        var services = new[] { "BluetoothUserService", "BTAGService", "BthAvctpSvc", "bthserv", "DeviceAssociationService", "DevicesFlowUserSvc", "DsmSvc", "NcbService", "SystemEventsBroker", "WFDSConMgrSvc" };
        var drivers = new[] { "BthA2dp", "BthEnum", "BthHFAud", "BthHFEnum", "BthLEEnum", "BthMini", "BTHMODEM", "BthPan", "BTHPORT", "BTHUSB", "HidBth", "ibtusb", "Microsoft_Bluetooth_AvrcpTransport", "RFCOMM" };

        // check state
        Bluetooth.IsChecked = services.All(service => File.ReadAllLines(list).Any(line => line.Trim() == service))
                         && drivers.All(driver => File.ReadAllLines(list).Any(line => line.Trim() == driver));

        isInitializingBluetoothState = false;
    }

    private async void Bluetooth_Checked(object sender, RoutedEventArgs e)
    {
        if (isInitializingBluetoothState) return;

        // read list
        var lines = await File.ReadAllLinesAsync(list);

        // define services and drivers
        var services = new[] { "BluetoothUserService", "BTAGService", "BthAvctpSvc", "bthserv", "DeviceAssociationService", "DevicesFlowUserSvc", "DsmSvc", "NcbService", "SystemEventsBroker", "WFDSConMgrSvc" };
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
        Camera.IsChecked = drivers.All(driver => File.ReadAllLines(list).Any(line => line.Trim() == driver));

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
        bool isDesktop = new ManagementObjectSearcher("SELECT * FROM Win32_SystemEnclosure")
       .Get()
       .Cast<ManagementObject>()
       .Any(obj => ((ushort[])obj["ChassisTypes"])?.Any(type => new ushort[] { 3, 4, 5, 6, 7, 15, 16, 17 }.Contains(type)) == true);

        if (isDesktop)
        {
            Laptop_SettingsCard.Visibility = Visibility.Collapsed;
        }
        else
        {
            isInitializingLaptopState = false;
            Laptop.IsChecked = true;
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

