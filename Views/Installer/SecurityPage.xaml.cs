using Microsoft.Win32;

namespace AutoOS.Views.Installer;

public sealed partial class SecurityPage: Page
{
    const string registryPath = @"SOFTWARE\AutoOS";
    private bool isInitializingWindowsDefenderState = true;
    private bool isInitializingUACState = true;
    private bool isInitializingDEPState = true;
    private bool isInitializingSpectreMeltdownState = true;
    private bool isInitializingProcessMitigationsState = true;

    public SecurityPage()
    {
        InitializeComponent();
        GetWindowsDefenderState();
        GetUACState();
        GetDEPState();
        GetSpectreMeltdownState();
        GetProcessMitigationsState();
    }

    private void GetWindowsDefenderState()
    {
        // get state
        using var key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\AutoOS");
        var value = key?.GetValue("WindowsDefender");

        if (value == null)
        {
            key?.SetValue("WindowsDefender", 0, RegistryValueKind.DWord);
        }
        else
        {
            WindowsDefender.IsOn = (int)value == 1;
        }

        isInitializingWindowsDefenderState = false;
    }

    private void WindowsDefender_Toggled(object sender, RoutedEventArgs e)
    {
        if (isInitializingWindowsDefenderState) return;

        // set value
        using var key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\AutoOS");
        key?.SetValue("WindowsDefender", WindowsDefender.IsOn ? 1 : 0, RegistryValueKind.DWord);
    }

    private void GetUACState()
    {
        // get state
        using var key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\AutoOS");
        var value = key?.GetValue("UserAccountControl");

        if (value == null)
        {
            key?.SetValue("UserAccountControl", 0, RegistryValueKind.DWord);
        }
        else
        {
            UAC.IsOn = (int)value == 1;
        }

        isInitializingUACState = false;
    }

    private void UAC_Toggled(object sender, RoutedEventArgs e)
    {
        if (isInitializingUACState) return;

        // set value
        using var key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\AutoOS");
        key?.SetValue("UserAccountControl", UAC.IsOn ? 1 : 0, RegistryValueKind.DWord);
    }

    private void GetDEPState()
    {
        // get state
        using var key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\AutoOS");
        var value = key?.GetValue("DataExecutionPrevention");

        if (value == null)
        {
            key?.SetValue("DataExecutionPrevention", 0, RegistryValueKind.DWord);
        }
        else
        {
            DEP.IsOn = (int)value == 1;
        }

        isInitializingDEPState = false;
    }

    private void DEP_Toggled(object sender, RoutedEventArgs e)
    {
        if (isInitializingDEPState) return;

        // set value
        using var key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\AutoOS");
        key?.SetValue("DataExecutionPrevention", DEP.IsOn ? 1 : 0, RegistryValueKind.DWord);
    }

    private void GetSpectreMeltdownState()
    {
        // get state
        using var key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\AutoOS");
        var value = key?.GetValue("SpectreMeltdownMitigations");

        if (value == null)
        {
            string cpuVendor = (string)Registry.GetValue(@"HKEY_LOCAL_MACHINE\HARDWARE\DESCRIPTION\System\CentralProcessor\0", "VendorIdentifier", null);
            if (cpuVendor.Contains("GenuineIntel"))
            {
                key?.SetValue("SpectreMeltdownMitigations", 0, RegistryValueKind.DWord);
            }
            else if (cpuVendor.Contains("AuthenticAMD"))
            {
                key?.SetValue("SpectreMeltdownMitigations", 1, RegistryValueKind.DWord);
                SpectreMeltdown.IsOn = true;
            }   
        }
        else
        {
            SpectreMeltdown.IsOn = (int)value == 1;
        }

        isInitializingSpectreMeltdownState = false;
    }

    private void SpectreMeltdown_Toggled(object sender, RoutedEventArgs e)
    {
        if (isInitializingSpectreMeltdownState) return;

        // set value
        using var key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\AutoOS");
        key?.SetValue("SpectreMeltdownMitigations", SpectreMeltdown.IsOn ? 1 : 0, RegistryValueKind.DWord);
    }

    private void GetProcessMitigationsState()
    {
        // get state
        using var key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\AutoOS");
        var value = key?.GetValue("ProcessMitigations");

        if (value == null)
        {
            key?.SetValue("ProcessMitigations", 0, RegistryValueKind.DWord);
        }
        else
        {
            ProcessMitigations.IsOn = (int)value == 1;
        }

        isInitializingProcessMitigationsState = false;
    }

    private void ProcessMitigations_Toggled(object sender, RoutedEventArgs e)
    {
        if (isInitializingProcessMitigationsState) return;

        // set value
        using var key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\AutoOS");
        key?.SetValue("ProcessMitigations", ProcessMitigations.IsOn ? 1 : 0, RegistryValueKind.DWord);
    }
}

