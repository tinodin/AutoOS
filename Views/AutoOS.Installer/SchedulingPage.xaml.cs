using Microsoft.Win32;

namespace AutoOS.Views.Installer;

public sealed partial class SchedulingPage : Page
{
    private bool isInitializingAffinities = true;
    public SchedulingPage()
    {
        InitializeComponent();
        GetCpuCount(GPU, XHCI);
        GetAffinities();
    }

    private void GetCpuCount(params ComboBox[] comboBoxes)
    {
        int processorCount = Environment.ProcessorCount;

        foreach (var comboBox in comboBoxes)
        {
            comboBox.Items.Clear();

            for (int i = 0; i < processorCount; i++)
            {
                comboBox.Items.Add(new ComboBoxItem { Content = $"CPU {i}" });
            }

            if (comboBox.Items.Count > 0)
                ((ComboBoxItem)comboBox.Items[0]).IsEnabled = false;
        }
    }

    private void GetAffinities()
    {
        using (var key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\AutoOS"))
        {
            if (key.GetValue("Affinities") == null)
            {
                key.SetValue("Affinities", "Automatic", RegistryValueKind.String);
                Affinities.SelectedIndex = 0;
                GpuSettings.IsEnabled = false;
                XhciSettings.IsEnabled = false;  
            }
            else
            {
                var affinityValue = key.GetValue("Affinities") as string;
                if (affinityValue == "Automatic")
                {
                    Affinities.SelectedIndex = 0;
                    GpuSettings.IsEnabled = false;
                    XhciSettings.IsEnabled = false;
                }
                else if (affinityValue == "Manual")
                {
                    Affinities.SelectedIndex = 1;
                    GpuSettings.IsEnabled = true;
                    XhciSettings.IsEnabled = true;
                }
            }

            int gpuAffinity = (int)(key.GetValue("GpuAffinity") ?? -1);
            if (gpuAffinity >= 0 && gpuAffinity < Environment.ProcessorCount)
            {
                GPU.SelectedIndex = gpuAffinity;
            }
            else
            {
                GPU.SelectedIndex = 1;
            }

            int xhciAffinity = (int)(key.GetValue("XhciAffinity") ?? -1);
            if (xhciAffinity >= 0 && xhciAffinity < Environment.ProcessorCount)
            {
                XHCI.SelectedIndex = xhciAffinity;
            }
            else
            {
                XHCI.SelectedIndex = 2;
            }
        }

        isInitializingAffinities = false;
    }

    private void Affinities_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (isInitializingAffinities) return;

        using (var key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\AutoOS"))
        {
            if (Affinities.SelectedIndex == 0)
            {
                key.SetValue("Affinities", "Automatic", RegistryValueKind.String);
                GpuSettings.IsEnabled = false;
                XhciSettings.IsEnabled = false;
            }
            else if (Affinities.SelectedIndex == 1)
            {
                key.SetValue("Affinities", "Manual", RegistryValueKind.String);
                GpuSettings.IsEnabled = true;
                XhciSettings.IsEnabled = true;
            }
        }
    }

    private void Gpu_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (isInitializingAffinities) return;

        using (var key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\AutoOS"))
        {
            int selectedIndex = GPU.SelectedIndex;
            key.SetValue("GpuAffinity", selectedIndex, RegistryValueKind.DWord);
        }

        UpdateComboBoxState(GPU, XHCI);
    }

    private void Xhci_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (isInitializingAffinities) return;

        using (var key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\AutoOS"))
        {
            int selectedIndex = XHCI.SelectedIndex;
            key.SetValue("XhciAffinity", selectedIndex, RegistryValueKind.DWord);
        }

        UpdateComboBoxState(XHCI, GPU);
    }

    private void UpdateComboBoxState(ComboBox activeComboBox, ComboBox otherComboBox)
    {
        int selectedIndex = activeComboBox.SelectedIndex;

        for (int i = 0; i < otherComboBox.Items.Count; i++)
        {
            var item = otherComboBox.Items[i] as ComboBoxItem;
            item.IsEnabled = i != selectedIndex;
        }
    }
}

