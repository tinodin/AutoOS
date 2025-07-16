using Microsoft.Win32;
using System.Diagnostics;
using System.Management;
using System.Text.RegularExpressions;

namespace AutoOS.Views.Settings;

public sealed partial class SchedulingPage : Page
{
    private bool isInitializingAffinities = true;
    private bool isHyperThreadingEnabled = false;
    private int physicalCoreCount = 0;
    private readonly int logicalCoreCount = Environment.ProcessorCount;

    public SchedulingPage()
    {
        InitializeComponent();
        GetCpuCount(GPU, XHCI, NIC);
        GetAffinities();
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
        foreach (var query in new[]
        {
        "SELECT PNPDeviceID FROM Win32_VideoController",
        "SELECT PNPDeviceID FROM Win32_USBController"
        })

        {
            foreach (ManagementObject obj in new ManagementObjectSearcher(query).Get())
            {
                string path = obj["PNPDeviceID"]?.ToString();
                if (path?.StartsWith("PCI\\VEN_") == true)
                {
                    using var key = Registry.LocalMachine.OpenSubKey($@"SYSTEM\CurrentControlSet\Enum\{path}\Device Parameters\Interrupt Management\Affinity Policy");
                    if (key?.GetValue("AssignmentSetOverride") is byte[] value && value.Length > 0)
                    {
                        int coreIndex = -1;
                        for (int i = 0; i < value.Length; i++)
                        {
                            for (int bit = 0; bit < 8; bit++)
                            {
                                if ((value[i] & (1 << bit)) != 0)
                                {
                                    coreIndex = i * 8 + bit;
                                    break;
                                }
                            }
                            if (coreIndex >= 0)
                                break;
                        }
                        if (coreIndex >= 0)
                        {
                            if (query.Contains("VideoController"))
                            {
                                GPU.SelectedIndex = coreIndex;
                            }
                            else if (query.Contains("USBController"))
                            {
                                XHCI.SelectedIndex = coreIndex;
                            }
                        }
                    }
                }
            }
        }

        foreach (ManagementObject obj in new ManagementObjectSearcher("SELECT PNPDeviceID FROM Win32_NetworkAdapter").Get().Cast<ManagementObject>())
        {
            string pnp = obj["PNPDeviceID"]?.ToString();
            if (pnp == null || !pnp.StartsWith("PCI\\VEN_")) continue;

            using var driverKey = Registry.LocalMachine.OpenSubKey($@"SYSTEM\CurrentControlSet\Enum\{pnp}");
            string driver = driverKey?.GetValue("Driver")?.ToString();
            if (string.IsNullOrEmpty(driver) || !driver.Contains('\\')) continue;

            using var classKey = Registry.LocalMachine.OpenSubKey($@"SYSTEM\CurrentControlSet\Control\Class\{driver}");
            if (classKey?.GetValue("*PhysicalMediaType")?.ToString() != "14") continue;

            if (int.TryParse(classKey.GetValue("*RssBaseProcNumber")?.ToString(), out int baseCore))
            {
                NIC.SelectedIndex = baseCore;
            }

            break;
        }

        UpdateComboBoxState(GPU, XHCI, NIC);

        isInitializingAffinities = false;
    }

    private async void GPU_Changed(object sender, SelectionChangedEventArgs e)
    {
        await ApplyGpuAffinity(sender, e);
    }

    private async Task ApplyGpuAffinity(object sender, SelectionChangedEventArgs e)
    {
        if (isInitializingAffinities) return;

        // remove infobar
        AffinityInfo.Children.Clear();

        // add infobar
        if (physicalCoreCount >= 6)
        {
            AffinityInfo.Children.Add(new InfoBar
            {
                Title = "Applying GPU Affinity to CPU " + GPU.SelectedIndex + " and reserving it...",
                IsClosable = false,
                IsOpen = true,
                Severity = InfoBarSeverity.Informational,
                Margin = new Thickness(5)
            });
        }
        else
        {
            AffinityInfo.Children.Add(new InfoBar
            {
                Title = "Applying GPU Affinity to CPU " + GPU.SelectedIndex + "...",
                IsClosable = false,
                IsOpen = true,
                Severity = InfoBarSeverity.Informational,
                Margin = new Thickness(5)
            });
        }

        // delay
        await Task.Delay(800);

        // apply affinity
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $@"/c {Path.Combine(PathHelper.GetAppDataFolderPath(), "AutoGpuAffinity", "AutoGpuAffinity.exe")} --apply-affinity {GPU.SelectedIndex}",
                CreateNoWindow = true
            }
        };

        process.Start();
        await process.WaitForExitAsync();

        // reserve cpus if 6 cores or more
        if (physicalCoreCount >= 6)
        {
            long mask =
                (1L << GPU.SelectedIndex) |
                (1L << XHCI.SelectedIndex) |
                (1L << NIC.SelectedIndex);

            byte[] maskBytes = BitConverter.GetBytes(mask);
            Array.Resize(ref maskBytes, 8);

            using var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Session Manager\kernel", writable: true);
            key?.SetValue("ReservedCpuSets", maskBytes, RegistryValueKind.Binary);
        }

        // delay
        await Task.Delay(800);

        // remove infobar
        AffinityInfo.Children.Clear();

        // add infobar
        if (physicalCoreCount >= 6)
        {
            UpdateComboBoxState(GPU, XHCI, NIC);

            var infoBar = new InfoBar
            {
                Title = "Successfully applied GPU Affinity to CPU " + GPU.SelectedIndex + " and reserved it.",
                IsClosable = false,
                IsOpen = true,
                Severity = InfoBarSeverity.Success,
                Margin = new Thickness(5)
            };
            AffinityInfo.Children.Add(infoBar);

            infoBar.Title += " A restart is required to apply the change.";
            infoBar.ActionButton = new Button
            {
                Content = "Restart",
                HorizontalAlignment = HorizontalAlignment.Right
            };
            ((Button)infoBar.ActionButton).Click += (s, args) =>
            Process.Start("shutdown", "/r /f /t 0");
        }
        else
        {
            UpdateComboBoxState(GPU, XHCI, NIC);

            var infoBar = new InfoBar
            {
                Title = "Successfully applied GPU Affinity to CPU " + GPU.SelectedIndex + ".",
                IsClosable = false,
                IsOpen = true,
                Severity = InfoBarSeverity.Success,
                Margin = new Thickness(5)
            };
            AffinityInfo.Children.Add(infoBar);

            // delay
            await Task.Delay(2000);

            // remove infobar
            AffinityInfo.Children.Clear();
        }
    }

    private async void XHCI_Changed(object sender, SelectionChangedEventArgs e)
    {
        await ApplyXhciAffinity(sender, e);
    }

    private async Task ApplyXhciAffinity(object sender, SelectionChangedEventArgs e)
    {
        if (isInitializingAffinities) return;

        // remove infobar
        AffinityInfo.Children.Clear();

        // add infobar
        if (physicalCoreCount >= 6)
        {
            AffinityInfo.Children.Add(new InfoBar
            {
                Title = "Applying XHCI Affinity to CPU " + XHCI.SelectedIndex + " and reserving it...",
                IsClosable = false,
                IsOpen = true,
                Severity = InfoBarSeverity.Informational,
                Margin = new Thickness(5)
            });
        }
        else
        {
            AffinityInfo.Children.Add(new InfoBar
            {
                Title = "Applying XHCI Affinity to CPU " + XHCI.SelectedIndex + "...",
                IsClosable = false,
                IsOpen = true,
                Severity = InfoBarSeverity.Informational,
                Margin = new Thickness(5)
            });
        }

        // delay
        await Task.Delay(800);

        var query = "SELECT PNPDeviceID FROM Win32_USBController";
        foreach (ManagementObject obj in new ManagementObjectSearcher(query).Get())
        {
            string path = obj["PNPDeviceID"]?.ToString();
            if (path?.StartsWith("PCI\\VEN_") == true)
            {
                using var key = Registry.LocalMachine.OpenSubKey($@"SYSTEM\CurrentControlSet\Enum\{path}\Device Parameters\Interrupt Management\Affinity Policy", true);
                if (key != null)
                {
                    int selectedIndex = XHCI.SelectedIndex;
                    if (selectedIndex >= 0)
                    {
                        byte[] binaryValue = new byte[(selectedIndex / 8) + 1];
                        binaryValue[selectedIndex / 8] = (byte)(1 << (selectedIndex % 8));
                        key.SetValue("AssignmentSetOverride", binaryValue, RegistryValueKind.Binary);
                    }

                    key.SetValue("DevicePolicy", 4, RegistryValueKind.DWord);
                }
            }
        }

        // reserve cpus if 6 cores or more
        if (physicalCoreCount >= 6)
        {
            long mask =
                (1L << GPU.SelectedIndex) |
                (1L << XHCI.SelectedIndex) |
                (1L << NIC.SelectedIndex);

            byte[] maskBytes = BitConverter.GetBytes(mask);
            Array.Resize(ref maskBytes, 8);

            using var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Session Manager\kernel", writable: true);
            key?.SetValue("ReservedCpuSets", maskBytes, RegistryValueKind.Binary);
        }

        // delay
        await Task.Delay(800);

        // remove infobar
        AffinityInfo.Children.Clear();

        // add infobar
        if (physicalCoreCount >= 6)
        {
            UpdateComboBoxState(GPU, XHCI, NIC);

            var infoBar = new InfoBar
            {
                Title = "Successfully applied XHCI Affinity to CPU " + XHCI.SelectedIndex + " and reserved it.",
                IsClosable = false,
                IsOpen = true,
                Severity = InfoBarSeverity.Success,
                Margin = new Thickness(5)
            };
            AffinityInfo.Children.Add(infoBar);

            infoBar.Title += " A restart is required to apply the change.";
            infoBar.ActionButton = new Button
            {
                Content = "Restart",
                HorizontalAlignment = HorizontalAlignment.Right
            };
            ((Button)infoBar.ActionButton).Click += (s, args) =>
            Process.Start("shutdown", "/r /f /t 0");
        }
        else
        {
            UpdateComboBoxState(GPU, XHCI, NIC);

            var infoBar = new InfoBar
            {
                Title = "Successfully applied XHCI Affinity to CPU " + XHCI.SelectedIndex + ".",
                IsClosable = false,
                IsOpen = true,
                Severity = InfoBarSeverity.Success,
                Margin = new Thickness(5)
            };
            AffinityInfo.Children.Add(infoBar);

            // delay
            await Task.Delay(2000);

            // remove infobar
            AffinityInfo.Children.Clear();
        }
    }

    private async void NIC_Changed(object sender, SelectionChangedEventArgs e)
    {
        await ApplyNicAffinity(sender, e);
    }

    private async Task ApplyNicAffinity(object sender, SelectionChangedEventArgs e)
    {
        if (isInitializingAffinities) return;

        // remove infobar
        AffinityInfo.Children.Clear();

        // add infobar
        if (physicalCoreCount >= 6)
        {
            AffinityInfo.Children.Add(new InfoBar
            {
                Title = "Applying NIC Affinity to CPU " + NIC.SelectedIndex + " and reserving it...",
                IsClosable = false,
                IsOpen = true,
                Severity = InfoBarSeverity.Informational,
                Margin = new Thickness(5)
            });
        }
        else
        {
            AffinityInfo.Children.Add(new InfoBar
            {
                Title = "Applying NIC Affinity to CPU " + NIC.SelectedIndex + "...",
                IsClosable = false,
                IsOpen = true,
                Severity = InfoBarSeverity.Informational,
                Margin = new Thickness(5)
            });
        }

        // delay
        await Task.Delay(800);

        foreach (ManagementObject obj in new ManagementObjectSearcher("SELECT PNPDeviceID FROM Win32_NetworkAdapter").Get().Cast<ManagementObject>())
        {
            string pnp = obj["PNPDeviceID"]?.ToString();
            if (pnp == null || !pnp.StartsWith("PCI\\VEN_")) continue;

            using var driverKey = Registry.LocalMachine.OpenSubKey($@"SYSTEM\CurrentControlSet\Enum\{pnp}", writable: true);
            string driver = driverKey?.GetValue("Driver")?.ToString();
            if (string.IsNullOrEmpty(driver) || !driver.Contains('\\')) continue;

            using var classKey = Registry.LocalMachine.OpenSubKey($@"SYSTEM\CurrentControlSet\Control\Class\{driver}", writable: true);
            if (classKey?.GetValue("*PhysicalMediaType")?.ToString() != "14") continue;

            classKey.SetValue("*RSS", "0", RegistryValueKind.String);
            classKey.SetValue("*RssBaseProcNumber", NIC.SelectedIndex.ToString(), RegistryValueKind.String);
            classKey.SetValue("*RssMaxProcNumber", NIC.SelectedIndex.ToString(), RegistryValueKind.String);
            classKey.SetValue("*MaxRssProcessors", "1", RegistryValueKind.String);
            classKey.SetValue("*RssBaseProcGroup", "0", RegistryValueKind.String);
            classKey.SetValue("*RssMaxProcGroup", "0", RegistryValueKind.String);
            
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = @"-Command ""Get-NetAdapter | Where { $_.PhysicalMediaType -eq '802.3' } | ForEach { Restart-NetAdapter -Name $_.Name }""",
                    CreateNoWindow = true
                }
            };

            process.Start();
            await process.WaitForExitAsync();

            break;
        }

        // reserve cpus if 6 cores or more
        if (physicalCoreCount >= 6)
        {
            long mask =
                (1L << GPU.SelectedIndex) |
                (1L << XHCI.SelectedIndex) |
                (1L << NIC.SelectedIndex);

            byte[] maskBytes = BitConverter.GetBytes(mask);
            Array.Resize(ref maskBytes, 8);

            using var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Session Manager\kernel", writable: true);
            key?.SetValue("ReservedCpuSets", maskBytes, RegistryValueKind.Binary);
        }

        // delay
        await Task.Delay(800);

        // remove infobar
        AffinityInfo.Children.Clear();

        // add infobar
        if (physicalCoreCount >= 6)
        {
            UpdateComboBoxState(GPU, XHCI, NIC);

            var infoBar = new InfoBar
            {
                Title = "Successfully applied NIC Affinity to CPU " + NIC.SelectedIndex + " and reserved it.",
                IsClosable = false,
                IsOpen = true,
                Severity = InfoBarSeverity.Success,
                Margin = new Thickness(5)
            };
            AffinityInfo.Children.Add(infoBar);

            infoBar.Title += " A restart is required to apply the change.";
            infoBar.ActionButton = new Button
            {
                Content = "Restart",
                HorizontalAlignment = HorizontalAlignment.Right
            };
            ((Button)infoBar.ActionButton).Click += (s, args) =>
            Process.Start("shutdown", "/r /f /t 0");
        }
        else
        {
            UpdateComboBoxState(GPU, XHCI, NIC);

            var infoBar = new InfoBar
            {
                Title = "Successfully applied NIC Affinity to CPU " + NIC.SelectedIndex + ".",
                IsClosable = false,
                IsOpen = true,
                Severity = InfoBarSeverity.Success,
                Margin = new Thickness(5)
            };
            AffinityInfo.Children.Add(infoBar);

            // delay
            await Task.Delay(2000);

            // remove infobar
            AffinityInfo.Children.Clear();
        }
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

    private void Benchmark_Unchecked(object sender, RoutedEventArgs e)
    {
        foreach (var name in new[] { "AutoGpuAffinity", "restart64", "lava-triangle" })
        {
            Process.GetProcessesByName(name).ToList().ForEach(p =>
            {
                p.Kill();
                p.WaitForExit();
            });
        }
    }

    private async void Benchmark_Checked(object sender, RoutedEventArgs e)
    {
        await Task.Delay(1000);

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $@"/c {Path.Combine(PathHelper.GetAppDataFolderPath(), "AutoGpuAffinity", "AutoGpuAffinity.exe")}",
                CreateNoWindow = true,
                RedirectStandardOutput = true
            }
        };
        process.Start();

        string output = await process.StandardOutput.ReadToEndAsync();

        var match = Regex.Match(output, @"First:\s*(\d+)\s*Second:\s*(\d+)\s*Third:\s*(\d+)");

        if (match.Success)
        {
            isInitializingAffinities = true;
            GPU.SelectedIndex = int.Parse(match.Groups[1].Value);
            XHCI.SelectedIndex = int.Parse(match.Groups[2].Value);
            NIC.SelectedIndex = int.Parse(match.Groups[3].Value);
            isInitializingAffinities = false;

            await ApplyGpuAffinity(null, null);
            await ApplyXhciAffinity(null, null);
            await ApplyNicAffinity(null, null);
        }

        Benchmark.IsChecked = false;
    }
}