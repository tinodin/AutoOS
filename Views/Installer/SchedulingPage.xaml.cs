using Microsoft.Win32;
using System.Management;

namespace AutoOS.Views.Installer;

public sealed partial class SchedulingPage : Page
{
    private bool isInitializingAffinities = true;
    private bool isHyperThreadingEnabled = false;
    private readonly int processorCount = Environment.ProcessorCount;

    public SchedulingPage()
    {
        InitializeComponent();
        GetCpuCount(GPU, XHCI);
        GetAffinities();
    }

    private void GetCpuCount(params ComboBox[] comboBoxes)
    {
        isHyperThreadingEnabled = new ManagementObjectSearcher("SELECT NumberOfCores, NumberOfLogicalProcessors FROM Win32_Processor")
           .Get()
           .Cast<ManagementObject>()
           .Any(obj => Convert.ToInt32(obj["NumberOfLogicalProcessors"]) > Convert.ToInt32(obj["NumberOfCores"]));

        foreach (var comboBox in comboBoxes)
        {
            comboBox.Items.Clear();

            for (int i = 0; i < processorCount; i++)
            {
                var item = new ComboBoxItem { Content = $"CPU {i}" };

                if ((processorCount > 2 && (i == 0 || (isHyperThreadingEnabled && i % 2 == 1))))
                {
                    item.IsEnabled = false;
                }

                comboBox.Items.Add(item);
            }
        }

        // copy autogpuaffinity to localstate because of permissions
        string sourcePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "AutoGpuAffinity");
        string destinationPath = Path.Combine(PathHelper.GetAppDataFolderPath(), "AutoGpuAffinity");

        foreach (var directory in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
        {
            string subDirPath = directory.Replace(sourcePath, destinationPath);
            Directory.CreateDirectory(subDirPath);
        }

        foreach (var file in Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories))
        {
            string destFilePath = file.Replace(sourcePath, destinationPath);
            File.Copy(file, destFilePath, true);
        }

        // configure config
        string configPath = Path.Combine(PathHelper.GetAppDataFolderPath(), "AutoGpuAffinity", "config.ini");
        string[] lines = File.ReadAllLines(configPath);

        if (processorCount > 2)
        {
            if (isHyperThreadingEnabled)
            {
                lines = lines.Select(line =>
                {
                    if (line.StartsWith("custom_cpus="))
                    {
                        var cores = Enumerable.Range(2, processorCount - 2).Where(i => i % 2 == 0);
                        return $"custom_cpus=[{string.Join(",", cores)}]";
                    }
                    return line;
                }).ToArray();
            }
            else
            {
                lines = lines.Select(line =>
                {
                    if (line.StartsWith("custom_cpus="))
                        return $"custom_cpus=[1..{processorCount - 1}]";
                    return line;
                }).ToArray();
            }
        }

        if (Directory.Exists(@"C:\Program Files (x86)\MSI Afterburner\Profiles\") &&
            Directory.GetFiles(@"C:\Program Files (x86)\MSI Afterburner\Profiles\")
                     .Any(f => !f.EndsWith("MSIAfterburner.cfg", StringComparison.OrdinalIgnoreCase)))
        {
            lines = lines.Select(line =>
                line.StartsWith("profile=") ? "profile=1" : line).ToArray();
        }

        File.WriteAllLines(configPath, lines);
    }

    private void GetAffinities()
    {
        using (var key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\AutoOS"))
        {
            var affinityValue = key.GetValue("Affinities") as string;
            if (string.IsNullOrEmpty(affinityValue) || affinityValue == "Automatic")
            {
                key.SetValue("Affinities", "Automatic", RegistryValueKind.String);

                Affinities.SelectedIndex = 0;
                GpuSettings.IsEnabled = false;
                XhciSettings.IsEnabled = false;

                int gpuAffinity = (int)(key.GetValue("GpuAffinity") ?? -1);
                if (gpuAffinity >= 0 && gpuAffinity < GPU.Items.Count && ((ComboBoxItem)GPU.Items[gpuAffinity]).IsEnabled)
                {
                    GPU.SelectedIndex = gpuAffinity;
                }
                else
                {
                    GPU.SelectedIndex = GPU.Items.OfType<ComboBoxItem>().ToList().FindIndex(item => item.IsEnabled);
                }
                UpdateComboBoxState(GPU, XHCI);

                int xhciAffinity = (int)(key.GetValue("XhciAffinity") ?? -1);
                if (xhciAffinity >= 0 && xhciAffinity < XHCI.Items.Count && ((ComboBoxItem)XHCI.Items[xhciAffinity]).IsEnabled)
                {
                    XHCI.SelectedIndex = xhciAffinity;
                }
                else
                {
                    XHCI.SelectedIndex = XHCI.Items.OfType<ComboBoxItem>().ToList().FindIndex(item => item.IsEnabled);
                }
                UpdateComboBoxState(XHCI, GPU);
            }
            else if (affinityValue == "Manual")
            {
                Affinities.SelectedIndex = 1;
                GpuSettings.IsEnabled = true;
                XhciSettings.IsEnabled = true;

                int gpuAffinity = (int)(key.GetValue("GpuAffinity") ?? -1);
                if (gpuAffinity >= 0 && gpuAffinity < GPU.Items.Count)
                {
                    GPU.SelectedIndex = gpuAffinity;
                    UpdateComboBoxState(GPU, XHCI);
                }

                int xhciAffinity = (int)(key.GetValue("XhciAffinity") ?? -1);
                if (xhciAffinity >= 0 && xhciAffinity < XHCI.Items.Count)
                {
                    XHCI.SelectedIndex = xhciAffinity;
                    UpdateComboBoxState(XHCI, GPU);
                }
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

                Gpu_Changed(null, null);
                Xhci_Changed(null, null);
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

        if (processorCount > 2)
        {
            for (int i = 0; i < otherComboBox.Items.Count; i++)
            {
                var item = otherComboBox.Items[i] as ComboBoxItem;
                if (item != null)
                {
                    item.IsEnabled = i != selectedIndex && !(i == 0 || (isHyperThreadingEnabled && i % 2 == 1));
                }
            }
        }
        else
        {
            // swap indexes
            if (otherComboBox.SelectedIndex == selectedIndex)
            {
                int swapIndex = otherComboBox.SelectedIndex == 0 ? 1 : 0;
                if (swapIndex < otherComboBox.Items.Count)
                {
                    otherComboBox.SelectedIndex = swapIndex;
                }
            }
        }
    }
}
