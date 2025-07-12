using Windows.Storage;
using System.Management;

namespace AutoOS.Views.Installer;

public sealed partial class SchedulingPage : Page
{
    private bool isInitializingAffinities = true;
    private bool isHyperThreadingEnabled = false;
    private int physicalCoreCount = 0;
    private readonly int logicalCoreCount = Environment.ProcessorCount;

    private ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

    public SchedulingPage()
    {
        InitializeComponent();
        GetCpuCount(GPU, XHCI, NIC);
        GetAffinities();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        MainWindow.Instance.MarkVisited(nameof(SchedulingPage));
        MainWindow.Instance.CheckAllPagesVisited();
    }

    private void GetCpuCount(params ComboBox[] comboBoxes)
    {
        physicalCoreCount = new ManagementObjectSearcher("SELECT NumberOfCores FROM Win32_Processor")
            .Get()
            .Cast<ManagementObject>()
            .Sum(m => Convert.ToInt32(m["NumberOfCores"]));

        isHyperThreadingEnabled = new ManagementObjectSearcher("SELECT NumberOfCores, NumberOfLogicalProcessors FROM Win32_Processor")
           .Get()
           .Cast<ManagementObject>()
           .Any(obj => Convert.ToInt32(obj["NumberOfLogicalProcessors"]) > Convert.ToInt32(obj["NumberOfCores"]));

        foreach (var comboBox in comboBoxes)
        {
            comboBox.Items.Clear();

            for (int i = 0; i < logicalCoreCount; i++)
            {
                var item = new ComboBoxItem { Content = $"CPU {i}" };

                if ((physicalCoreCount > 2 && (i == 0 || (isHyperThreadingEnabled && i % 2 == 1))))
                {
                    item.IsEnabled = false;
                }
                else
                {
                    if (isHyperThreadingEnabled && i % 2 == 1)
                    {
                        item.IsEnabled = false;
                    }
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

        if (physicalCoreCount > 2)
        {
            if (isHyperThreadingEnabled)
            {
                lines = [.. lines.Select(line =>
                {
                    if (line.StartsWith("custom_cpus="))
                    {
                        var cores = Enumerable.Range(2, logicalCoreCount - 2).Where(i => i % 2 == 0);
                        return $"custom_cpus=[{string.Join(",", cores)}]";
                    }
                    return line;
                })];
            }
            else
            {
                lines = [.. lines.Select(line =>
                {
                    if (line.StartsWith("custom_cpus="))
                        return $"custom_cpus=[1..{logicalCoreCount - 1}]";
                    return line;
                })];
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
        var affinityValue = localSettings.Values["Affinities"] as string;
        if (string.IsNullOrEmpty(affinityValue) || affinityValue == "Automatic")
        {
            localSettings.Values["Affinities"] = "Automatic";

            Affinities.SelectedIndex = 0;
            GpuSettings.IsEnabled = false;
            XhciSettings.IsEnabled = false;
            NicSettings.IsEnabled = false;

            if (localSettings.Values.TryGetValue("GpuAffinity", out var gpuVal))
            {
                GPU.SelectedIndex = (int)gpuVal;
            }
            else
            {
                GPU.SelectedIndex = GPU.Items
                    .OfType<ComboBoxItem>()
                    .Select((item, index) => (item, index))
                    .First(pair => pair.item.IsEnabled)
                    .index;
            }
            UpdateComboBoxState(GPU, XHCI, NIC);

            if (localSettings.Values.TryGetValue("XhciAffinity", out var xhciVal))
            {
                XHCI.SelectedIndex = (int)xhciVal;
            }
            else
            {
                XHCI.SelectedIndex = XHCI.Items
                    .OfType<ComboBoxItem>()
                    .Select((item, index) => (item, index))
                    .First(pair => pair.item.IsEnabled)
                    .index;
            }
            UpdateComboBoxState(GPU, XHCI, NIC);

            if (localSettings.Values.TryGetValue("NicAffinity", out var nicVal))
            {
                NIC.SelectedIndex = (int)nicVal;
            }
            else
            {
                NIC.SelectedIndex = NIC.Items
                    .OfType<ComboBoxItem>()
                    .Select((item, index) => (item, index))
                    .First(pair => pair.item.IsEnabled)
                    .index;
            }
            UpdateComboBoxState(GPU, XHCI, NIC);
        }
        else if (affinityValue == "Manual")
        {
            Affinities.SelectedIndex = 1;
            GpuSettings.IsEnabled = true;
            XhciSettings.IsEnabled = true;
            NicSettings.IsEnabled = true;

            if (localSettings.Values.TryGetValue("GpuAffinity", out var gpuVal))
            {
                GPU.SelectedIndex = (int)gpuVal;
            }

            if (localSettings.Values.TryGetValue("XhciAffinity", out var xhciVal))
            {
                XHCI.SelectedIndex = (int)xhciVal;
            }

            if (localSettings.Values.TryGetValue("NicAffinity", out var nicVal))
            {
                NIC.SelectedIndex = (int)nicVal;
            }
        }

        UpdateComboBoxState(GPU, XHCI, NIC);

        isInitializingAffinities = false;
    }

    private void Affinities_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (isInitializingAffinities) return;

        if (Affinities.SelectedIndex == 0)
        {
            localSettings.Values["Affinities"] = "Automatic";
            GpuSettings.IsEnabled = false;
            XhciSettings.IsEnabled = false;
            NicSettings.IsEnabled = false;
        }
        else if (Affinities.SelectedIndex == 1)
        {
            localSettings.Values["Affinities"] = "Manual";
            GpuSettings.IsEnabled = true;
            XhciSettings.IsEnabled = true;
            NicSettings.IsEnabled = true;

            Gpu_Changed(null, null);
            Xhci_Changed(null, null);
            Nic_Changed(null, null);
        }
    }

    private void Gpu_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (isInitializingAffinities) return;

        int selectedIndex = GPU.SelectedIndex;
        localSettings.Values["GpuAffinity"] = selectedIndex;

        UpdateComboBoxState(GPU, XHCI, NIC);
    }

    private void Xhci_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (isInitializingAffinities) return;

        int selectedIndex = XHCI.SelectedIndex;
        localSettings.Values["XhciAffinity"] = selectedIndex;

        UpdateComboBoxState(GPU, XHCI, NIC);
    }

    private void Nic_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (isInitializingAffinities) return;

        int selectedIndex = NIC.SelectedIndex;
        localSettings.Values["NicAffinity"] = selectedIndex;

        UpdateComboBoxState(GPU, XHCI, NIC);
    }

    private void UpdateComboBoxState(ComboBox combo1, ComboBox combo2, ComboBox combo3)
    {
        if (logicalCoreCount > 4)
        {
            var selected = new[] { combo1.SelectedIndex, combo2.SelectedIndex, combo3.SelectedIndex };

            foreach (var combo in new[] { combo1, combo2, combo3 })
            {
                for (int i = 0; i < combo.Items.Count; i++)
                {
                    if (combo.Items[i] is ComboBoxItem item)
                    {
                        item.IsEnabled = !(selected.Any(index => index == i && combo.SelectedIndex != i)) && !(i == 0 || (isHyperThreadingEnabled && i % 2 == 1) && physicalCoreCount > 2);
                    }
                }
            }
        }
    }
}