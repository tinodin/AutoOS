using Microsoft.Win32;
using System.Management;

namespace AutoOS.Views.Installer;

public sealed partial class PowerPage : Page
{
    private bool isInitializingIdleStatesState = true;
    private bool isInitializingPowerServiceState = true;

    public PowerPage()
    {
        InitializeComponent();
        GetIdleState();
        GetPowerServiceState();
    }

    private void GetIdleState()
    {
        // get state
        using var key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\AutoOS");
        var value = key?.GetValue("IdleStates");

        if (value == null)
        {
            bool isDesktop = new ManagementObjectSearcher("SELECT * FROM Win32_SystemEnclosure")
                .Get()
                .Cast<ManagementObject>()
                .Any(obj => ((ushort[])obj["ChassisTypes"])?.Any(type => new ushort[] { 3, 4, 5, 6, 7, 15, 16, 17 }.Contains(type)) == true);

            bool isHyperThreadingEnabled = new ManagementObjectSearcher("SELECT NumberOfCores, NumberOfLogicalProcessors FROM Win32_Processor")
           .Get()
           .Cast<ManagementObject>()
           .Any(obj => Convert.ToInt32(obj["NumberOfLogicalProcessors"]) > Convert.ToInt32(obj["NumberOfCores"]));

            bool defaultEnabled = !isDesktop || isHyperThreadingEnabled;
            key?.SetValue("IdleStates", defaultEnabled ? 1 : 0, RegistryValueKind.DWord);
            IdleStates.IsOn = defaultEnabled;
        }
        else
        {
            IdleStates.IsOn = (int)value == 1;
        }

        isInitializingIdleStatesState = false;
    }

    private void IdleStates_Toggled(object sender, RoutedEventArgs e)
    {
        if (isInitializingIdleStatesState) return;

        // set value
        using var key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\AutoOS");
        key?.SetValue("IdleStates", IdleStates.IsOn ? 1 : 0, RegistryValueKind.DWord);
    }

    private void GetPowerServiceState()
    {
        // get state
        using var key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\AutoOS");
        var value = key?.GetValue("PowerService");

        if (value == null)
        {
            key?.SetValue("PowerService", 1, RegistryValueKind.DWord);
            PowerService.IsOn = true;
        }
        else if ((int)value == 1)
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

        // set value
        using var key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\AutoOS");
        key?.SetValue("PowerService", PowerService.IsOn ? 1 : 0, RegistryValueKind.DWord);

        // disable idle settings
        Idle_SettingsCard.IsEnabled = PowerService.IsOn;
    }
}

