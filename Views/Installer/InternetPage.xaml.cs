using Windows.Storage;

namespace AutoOS.Views.Installer;

public sealed partial class InternetPage : Page
{
    private bool isInitializingWIFIState = true;
    private bool isInitializingWOLState = true;

    private readonly ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

    public InternetPage()
    {
        InitializeComponent();
        GetWIFIState();
        GetWOLState();
    }

    private void GetWIFIState()
    {
        object value = localSettings.Values["WiFi"];
        if (value is null)
        {
            localSettings.Values["WiFi"] = 1;
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

        localSettings.Values["WiFi"] = WiFi.IsOn ? 1 : 0;
    }

    private void GetWOLState()
    {
        object value = localSettings.Values["WakeOnLan"];
        if (value is null)
        {
            localSettings.Values["WakeOnLan"] = 0;
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

        localSettings.Values["WakeOnLan"] = WOL.IsOn ? 1 : 0;
    }
}