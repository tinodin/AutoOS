using Microsoft.Win32;

namespace AutoOS.Views.Installer;

public sealed partial class DevicesPage : Page
{
    private bool isInitializingBluetoothState = true;
    private bool isInitializingHIDState = true;
    private bool isInitializingIMODState = true;

    public DevicesPage()
    {
        InitializeComponent();
        GetBluetoothState();
        GetHIDState();
        GetIMODState();
    }

    private void GetBluetoothState()
    {
        // get state
        using var key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\AutoOS");
        var value = key?.GetValue("Bluetooth");

        if (value == null)
        {
            key?.SetValue("Bluetooth", 1, RegistryValueKind.DWord);
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

        // set value
        using var key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\AutoOS");
        key?.SetValue("Bluetooth", Bluetooth.IsOn ? 1 : 0, RegistryValueKind.DWord);
    }

    private void GetHIDState()
    {
        // get state
        using var key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\AutoOS");
        var value = key?.GetValue("HumanInterfaceDevices");

        if (value == null)
        {
            key?.SetValue("HumanInterfaceDevices", 0, RegistryValueKind.DWord);
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

        // set value
        using var key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\AutoOS");
        key?.SetValue("HumanInterfaceDevices", HID.IsOn ? 1 : 0, RegistryValueKind.DWord);
    }

    private void GetIMODState()
    {
        // get state
        using var key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\AutoOS");
        var value = key?.GetValue("XhciInterruptModeration");

        if (value == null)
        {
            key?.SetValue("XhciInterruptModeration", 0, RegistryValueKind.DWord);
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

        // set value
        using var key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\AutoOS");
        key?.SetValue("XhciInterruptModeration", IMOD.IsOn ? 1 : 0, RegistryValueKind.DWord);
    }
}

