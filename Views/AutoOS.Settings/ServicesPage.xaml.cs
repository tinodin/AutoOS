using System.Diagnostics;
using Microsoft.Win32;
using System.ServiceProcess;

namespace AutoOS.Views.Settings;

public sealed partial class ServicesPage : Page
{
    private bool isInitializingServicesState = true;
    private bool isInitializingWIFIState = true;
    private bool isInitializingBluetoothState = true;
    private bool isInitializingCameraState = true;
    private bool isInitializingSnippingState = true;
    private bool isInitializingStartMenuState = true;
    private bool isInitializingTaskManagerState = true;
    private bool isInitializingLaptopState = true;
    private bool isInitializingFACEITState = true;
    private bool isInitializingGTAState = true;
    private bool isInitializingAMDVRRState = true;

    public ServicesPage()
    {
        InitializeComponent();
        GetServicesState();
        GetWIFIState();
        GetBluetoothState();
        GetCameraState();
        GetSnippingState();
        GetStartMenuState();
        GetTaskManagerState();
        GetLaptopState();
        GetFACEITState();
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
                Title = Services.IsOn ? "Successfully enabled services. A restart is required to apply the change."
                                          : "Successfully disabled services. A restart is required to apply the change.",
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

    private void GetWIFIState()
    {
        // define services and drivers
        var services = new[] { "WlanSvc", "Dhcp", "EventLog", "Netman", "NetSetupSvc", "NlaSvc", "Wcmsvc" };
        var drivers = new[] { "# tdx", "# vwififlt" };
        var lines = File.ReadAllLines(@"C:\AutoOS\Utilities\Services\service-list-builder\lists.ini");

        // check state
        WIFI.IsChecked = services.All(service => lines.Any(line => line.Trim() == service))
                         && drivers.All(driver => lines.Any(line => line.Trim() == driver));

        isInitializingWIFIState = false;
    }

    private async void WIFI_Checked(object sender, RoutedEventArgs e)
    {
    }

    private void GetBluetoothState()
    {
        // define services and drivers
        var services = new[] { "BluetoothUserService", "BTAGService", "BthAvctpSvc", "bthserv", "DevicesFlowUserSvc", "DsmSvc", "NcbService", "SystemEventsBroker", "WFDSConMgrSvc" };
        var drivers = new[] { "# BthA2dp", "# BthEnum", "# BthHFAud", "# BthHFEnum", "# BthLEEnum", "# BthMini", "# BTHMODEM", "# BthPan", "# BTHPORT", "# BTHUSB", "# HidBth", "# ibtusb", "# Microsoft_Bluetooth_AvrcpTransport", "# RFCOMM" };
        var lines = File.ReadAllLines(@"C:\AutoOS\Utilities\Services\service-list-builder\lists.ini");

        // check state
        Bluetooth.IsChecked = services.All(service => lines.Any(line => line.Trim() == service))
                         && drivers.All(driver => lines.Any(line => line.Trim() == driver));

        isInitializingBluetoothState = false;
    }

    private async void Bluetooth_Checked(object sender, RoutedEventArgs e)
    {
    }

    private void GetCameraState()
    {
        // define services and drivers
        var drivers = new[] { "# swenum" };
        var lines = File.ReadAllLines(@"C:\AutoOS\Utilities\Services\service-list-builder\lists.ini");

        // check state
        Camera.IsChecked = drivers.All(driver => lines.Any(line => line.Trim() == driver));

        isInitializingCameraState = false;
    }

    private async void Camera_Checked(object sender, RoutedEventArgs e)
    {
    }

    private void GetSnippingState()
    {
        // define services and drivers
        var services = new[] { "cbdhsvc", "CaptureService" };
        var lines = File.ReadAllLines(@"C:\AutoOS\Utilities\Services\service-list-builder\lists.ini");

        // check state
        Snipping.IsChecked = services.All(service => lines.Any(line => line.Trim() == service));

        isInitializingSnippingState = false;
    }

    private async void Snipping_Checked(object sender, RoutedEventArgs e)
    {
    }

    private void GetStartMenuState()
    {
        // define services and drivers
        var services = new[] { "UdkUserSvc" };
        var binaries = new[] { "# \\Windows\\System32\\ctfmon.exe", "# \\Windows\\System32\\RuntimeBroker.exe", "# \\Windows\\SystemApps\\ShellExperienceHost_cw5n1h2txyewy\\ShellExperienceHost.exe", "# \\Windows\\SystemApps\\Microsoft.Windows.StartMenuExperienceHost_cw5n1h2txyewy\\StartMenuExperienceHost.exe" };
        var lines = File.ReadAllLines(@"C:\AutoOS\Utilities\Services\service-list-builder\lists.ini");

        // check state
        StartMenu.IsChecked = services.All(service => lines.Any(line => line.Trim() == service))
                         && binaries.All(driver => lines.Any(line => line.Trim() == driver));

        isInitializingStartMenuState = false;
    }

    private async void StartMenu_Checked(object sender, RoutedEventArgs e)
    {
    }

    private void GetTaskManagerState()
    {
        // define services and drivers
        var drivers = new[] { "# pcw" };
        var lines = File.ReadAllLines(@"C:\AutoOS\Utilities\Services\service-list-builder\lists.ini");

        // check state
        TaskManager.IsChecked = drivers.All(driver => lines.Any(line => line.Trim() == driver));

        isInitializingTaskManagerState = false;
    }

    private async void TaskManager_Checked(object sender, RoutedEventArgs e)
    {
    }

    private void GetLaptopState()
    {
        // define services and drivers
        var drivers = new[] { "# msisadrv" };
        var lines = File.ReadAllLines(@"C:\AutoOS\Utilities\Services\service-list-builder\lists.ini");

        // check state
        Laptop.IsChecked = drivers.All(driver => lines.Any(line => line.Trim() == driver));

        isInitializingLaptopState = false;
    }

    private async void Laptop_Checked(object sender, RoutedEventArgs e)
    {
    }

    private void GetFACEITState()
    {
        // define services and drivers
        var drivers = new[] { "# FileCrypt" };
        var lines = File.ReadAllLines(@"C:\AutoOS\Utilities\Services\service-list-builder\lists.ini");

        // check state
        FACEIT.IsChecked = drivers.All(driver => lines.Any(line => line.Trim() == driver));

        isInitializingFACEITState = false;
    }

    private async void FACEIT_Checked(object sender, RoutedEventArgs e)
    {
    }

    private void GetGTAState()
    {
        // define services and drivers
        var drivers = new[] { "# mssmbios" };
        var lines = File.ReadAllLines(@"C:\AutoOS\Utilities\Services\service-list-builder\lists.ini");

        // check state
        GTA.IsChecked = drivers.All(driver => lines.Any(line => line.Trim() == driver));

        isInitializingGTAState = false;
    }

    private async void GTA_Checked(object sender, RoutedEventArgs e)
    {
    }

    private void GetAMDVRRState()
    {
        // define services and drivers
        var services = new[] { "AMD External Events Utility" };
        var lines = File.ReadAllLines(@"C:\AutoOS\Utilities\Services\service-list-builder\lists.ini");

        // check state
        AMDVRR.IsChecked = services.All(service => lines.Any(line => line.Trim() == service));

        isInitializingAMDVRRState = false;
    }

    private async void AMDVRR_Checked(object sender, RoutedEventArgs e)
    {
    }

    private async void LaunchServiWin_Click(object sender, RoutedEventArgs e)
    {
        // remove infobar
        ServiWinInfo.Children.Clear();

        // add infobar
        ServiWinInfo.Children.Add(new InfoBar
        {
            Title = "Launching ServiWin...",
            IsClosable = false,
            IsOpen = true,
            Severity = InfoBarSeverity.Informational,
            Margin = new Thickness(5)
        });

        // delay
        await Task.Delay(300);

        // launch
        await Task.Run(() => Process.Start(new ProcessStartInfo { FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "ServiWin", "serviwin.exe"), WindowStyle = ProcessWindowStyle.Maximized }));

        // remove infobar
        ServiWinInfo.Children.Clear();

        // add infobar
        ServiWinInfo.Children.Add(new InfoBar
        {
            Title = "Successfully launched ServiWin.",
            IsClosable = false,
            IsOpen = true,
            Severity = InfoBarSeverity.Success,
            Margin = new Thickness(5)
        });

        // delay
        await Task.Delay(2000);

        // remove infobar
        ServiWinInfo.Children.Clear();
    }
}

