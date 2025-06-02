using Windows.Storage;

namespace AutoOS.Views.Installer;

public sealed partial class DevicesPage : Page
{
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

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        MainWindow.Instance.MarkVisited(nameof(DevicesPage));
        MainWindow.Instance.CheckAllPagesVisited();
    }

    private void GetBluetoothState()
    {
        if (!localSettings.Values.TryGetValue("Bluetooth", out object value))
        {
            localSettings.Values["Bluetooth"] = 1;
            Bluetooth.IsOn = true;
        }
        else
        {
            Bluetooth.IsOn = (int)value == 1;
        }

        isInitializingBluetoothState = false;
    }

    private void Bluetooth_Toggled(object sender, RoutedEventArgs e)
    {
        if (isInitializingBluetoothState) return;
        localSettings.Values["Bluetooth"] = Bluetooth.IsOn ? 1 : 0;
    }

    private void GetHIDState()
    {
        if (!localSettings.Values.TryGetValue("HumanInterfaceDevices", out object value))
        {
            localSettings.Values["HumanInterfaceDevices"] = 0;
        }
        else
        {
            HID.IsOn = (int)value == 1;
        }

        isInitializingHIDState = false;
    }

    private void HID_Toggled(object sender, RoutedEventArgs e)
    {
        if (isInitializingHIDState) return;
        localSettings.Values["HumanInterfaceDevices"] = HID.IsOn ? 1 : 0;
    }

    private void GetIMODState()
    {
        if (!localSettings.Values.TryGetValue("XhciInterruptModeration", out object value))
        {
            localSettings.Values["XhciInterruptModeration"] = 0;
        }
        else
        {
            IMOD.IsOn = (int)value == 1;
        }

        isInitializingIMODState = false;
    }

    private void IMOD_Toggled(object sender, RoutedEventArgs e)
    {
        if (isInitializingIMODState) return;
        localSettings.Values["XhciInterruptModeration"] = IMOD.IsOn ? 1 : 0;
    }
}