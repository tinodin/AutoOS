using Windows.Storage;

namespace AutoOS.Views.Installer;

public sealed partial class PowerPage : Page
{
    private bool isInitializingIdleStatesState = true;
    private bool isInitializingPowerServiceState = true;

    private ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

    public PowerPage()
    {
        InitializeComponent();
        GetIdleState();
        GetPowerServiceState();
    }

    private void GetIdleState()
    {
        var value = localSettings.Values["IdleStates"];

        if (value == null)
        {
            bool isDesktop = new System.Management.ManagementObjectSearcher("SELECT * FROM Win32_SystemEnclosure")
               .Get()
               .Cast<System.Management.ManagementObject>()
               .Any(obj => ((ushort[])obj["ChassisTypes"])?.Any(type => new ushort[] { 3, 4, 5, 6, 7, 15, 16, 17 }.Contains(type)) == true);

            bool isHyperThreadingEnabled = new System.Management.ManagementObjectSearcher("SELECT NumberOfCores, NumberOfLogicalProcessors FROM Win32_Processor")
               .Get()
               .Cast<System.Management.ManagementObject>()
               .Any(obj => Convert.ToInt32(obj["NumberOfLogicalProcessors"]) > Convert.ToInt32(obj["NumberOfCores"]));

            bool defaultEnabled = !isDesktop || isHyperThreadingEnabled;
            localSettings.Values["IdleStates"] = defaultEnabled ? 1 : 0;
            IdleStates.IsOn = defaultEnabled;
        }
        else
        {
            IdleStates.IsOn = Convert.ToInt32(value) == 1;
        }

        isInitializingIdleStatesState = false;
    }

    private void IdleStates_Toggled(object sender, RoutedEventArgs e)
    {
        if (isInitializingIdleStatesState) return;

        localSettings.Values["IdleStates"] = IdleStates.IsOn ? 1 : 0;
    }

    private void GetPowerServiceState()
    {
        var value = localSettings.Values["PowerService"];

        if (value == null)
        {
            localSettings.Values["PowerService"] = 1;
            PowerService.IsOn = true;
        }
        else if (Convert.ToInt32(value) == 1)
        {
            PowerService.IsOn = true;
        }
        else
        {
            Idle_SettingsCard.IsEnabled = false;
        }

        isInitializingPowerServiceState = false;
    }

    private void PowerService_Toggled(object sender, RoutedEventArgs e)
    {
        if (isInitializingPowerServiceState) return;

        localSettings.Values["PowerService"] = PowerService.IsOn ? 1 : 0;

        Idle_SettingsCard.IsEnabled = PowerService.IsOn;
    }
}