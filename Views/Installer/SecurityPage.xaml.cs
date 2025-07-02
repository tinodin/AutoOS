using Microsoft.Win32;
using Windows.Storage;

namespace AutoOS.Views.Installer;

public sealed partial class SecurityPage : Page
{
    private bool isInitializingWindowsDefenderState = true;
    private bool isInitializingUACState = true;
    private bool isInitializingDEPState = true;
    private bool isInitializingMemoryIntegrityState = true;
    private bool isInitializingSpectreMeltdownState = true;
    private bool isInitializingProcessMitigationsState = true;

    private ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

    public SecurityPage()
    {
        InitializeComponent();
        GetWindowsDefenderState();
        GetUACState();
        GetDEPState();
        GetMemoryIntegrityState();
        GetSpectreMeltdownState();
        GetProcessMitigationsState();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        MainWindow.Instance.MarkVisited(nameof(SecurityPage));
        MainWindow.Instance.CheckAllPagesVisited();
    }

    private void GetWindowsDefenderState()
    {
        var value = localSettings.Values["WindowsDefender"];

        if (value == null)
        {
            localSettings.Values["WindowsDefender"] = 0;
            WindowsDefender.IsOn = false;
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

        localSettings.Values["WindowsDefender"] = WindowsDefender.IsOn ? 1 : 0;
    }

    private void GetUACState()
    {
        var value = localSettings.Values["UserAccountControl"];

        if (value == null)
        {
            localSettings.Values["UserAccountControl"] = 0;
            UAC.IsOn = false;
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

        localSettings.Values["UserAccountControl"] = UAC.IsOn ? 1 : 0;
    }

    private void GetDEPState()
    {
        var value = localSettings.Values["DataExecutionPrevention"];

        if (value == null)
        {
            using var secureBootKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\SecureBoot\State");
            var secureBootValue = secureBootKey?.GetValue("UEFISecureBootEnabled");

            if (secureBootValue != null && (int)secureBootValue == 1)
            {
                DEP.IsOn = true;
                localSettings.Values["DataExecutionPrevention"] = 1;
            }
            else
            {
                localSettings.Values["DataExecutionPrevention"] = 0;
            }
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

        localSettings.Values["DataExecutionPrevention"] = DEP.IsOn ? 1 : 0;
    }

    private void GetMemoryIntegrityState()
    {
        var value = localSettings.Values["MemoryIntegrity"];
        if (value == null)
        {
            localSettings.Values["MemoryIntegrity"] = 0;
        }
        else
        {
            MemoryIntegrity.IsOn = (int)value == 1;
        }

        isInitializingMemoryIntegrityState = false;
    }

    private void MemoryIntegrity_Toggled(object sender, RoutedEventArgs e)
    {
        if (isInitializingMemoryIntegrityState) return;

        localSettings.Values["MemoryIntegrity"] = MemoryIntegrity.IsOn ? 1 : 0;
    }

    private void GetSpectreMeltdownState()
    {
        var value = localSettings.Values["SpectreMeltdownMitigations"];

        if (value == null)
        {
            string cpuVendor = (string)Registry.GetValue(@"HKEY_LOCAL_MACHINE\HARDWARE\DESCRIPTION\System\CentralProcessor\0", "VendorIdentifier", null);
            if (cpuVendor.Contains("GenuineIntel"))
            {
                localSettings.Values["SpectreMeltdownMitigations"] = 0;
            }
            else if (cpuVendor.Contains("AuthenticAMD"))
            {
                localSettings.Values["SpectreMeltdownMitigations"] = 1;
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

        localSettings.Values["SpectreMeltdownMitigations"] = SpectreMeltdown.IsOn ? 1 : 0;
    }

    private void GetProcessMitigationsState()
    {
        var value = localSettings.Values["ProcessMitigations"];
        if (value == null)
        {
            localSettings.Values["ProcessMitigations"] = 0;
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

        localSettings.Values["ProcessMitigations"] = ProcessMitigations.IsOn ? 1 : 0;
    }
}