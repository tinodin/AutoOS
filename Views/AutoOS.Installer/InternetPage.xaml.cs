using Microsoft.Win32;

namespace AutoOS.Views.Installer;

public sealed partial class InternetPage : Page
{
    private bool isInitializingWIFIState = true;
    private bool isInitializingWOLState = true;

    public InternetPage()
    {
        InitializeComponent();
        GetWIFIState();
        GetWOLState();
    }

    private void GetWIFIState()
    {
        // get state
        using var key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\AutoOS");
        var value = key?.GetValue("WiFi");

        if (value == null)
        {
            key?.SetValue("WiFi", 1, RegistryValueKind.DWord);
            WiFi.IsOn = true;
        }
        else
        {
            WiFi.IsOn = (int)value == 1;
        }

        isInitializingWIFIState = false;
    }

    private void WiFi_Toggled(object sender, RoutedEventArgs e)
    {
        if (isInitializingWIFIState) return;

        // set value
        using var key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\AutoOS");
        key?.SetValue("WiFi", WiFi.IsOn ? 1 : 0, RegistryValueKind.DWord);
    }

    private void GetWOLState()
    {
        // get state
        using var key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\AutoOS");
        var value = key?.GetValue("WakeOnLan");

        if (value == null)
        {
            key?.SetValue("WakeOnLan", 0, RegistryValueKind.DWord);
        }
        else
        {
            WOL.IsOn = (int)value == 1;
        }

        isInitializingWOLState = false;
    }

    private void WOL_Toggled(object sender, RoutedEventArgs e)
    {
        if (isInitializingWOLState) return;

        // set value
        using var key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\AutoOS");
        key?.SetValue("WakeOnLan", WOL.IsOn ? 1 : 0, RegistryValueKind.DWord);
    }
}
