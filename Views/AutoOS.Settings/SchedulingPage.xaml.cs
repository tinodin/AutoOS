using Microsoft.Win32;
using System.Diagnostics;
using System.Management;

namespace AutoOS.Views.Settings;

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
                                if (coreIndex >= 0 && coreIndex < XHCI.Items.Count)
                                    ((ComboBoxItem)XHCI.Items[coreIndex]).IsEnabled = false;
                            }
                            else if (query.Contains("USBController"))
                            {
                                XHCI.SelectedIndex = coreIndex;
                                if (coreIndex >= 0 && coreIndex < GPU.Items.Count)
                                    ((ComboBoxItem)GPU.Items[coreIndex]).IsEnabled = false;
                            }
                        }
                    }
                }
            }
        }
        isInitializingAffinities = false;
    }

    private async void Gpu_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (isInitializingAffinities) return;

        int processorCount = Environment.ProcessorCount;

        // remove infobar
        AffinityInfo.Children.Clear();

        // add infobar
        if (processorCount >= 6)
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
                Arguments = $"/c \"{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "AutoGpuAffinity", "AutoGpuAffinity.exe")}\" --apply-affinity {GPU.SelectedIndex}",
                CreateNoWindow = true
            }
        };

        process.Start();
        await process.WaitForExitAsync();

        // apply reserved cpu sets if 6 cores or more
        if (processorCount >= 6)
        {
            using (var key = Registry.LocalMachine.OpenSubKey("SYSTEM\\CurrentControlSet\\Control\\Session Manager\\kernel", true))
            {
                key.SetValue("ReservedCpuSets", BitConverter.GetBytes((long)(1 << GPU.SelectedIndex) | (long)(1 << XHCI.SelectedIndex)).Concat(new byte[8 - BitConverter.GetBytes((long)(1 << GPU.SelectedIndex) | (long)(1 << XHCI.SelectedIndex)).Length]).ToArray(), RegistryValueKind.Binary);
            }
        }

        // delay
        await Task.Delay(800);

        // remove infobar
        AffinityInfo.Children.Clear();

        // add infobar
        if (processorCount >= 6)
        {
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

    private async void Xhci_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (isInitializingAffinities) return;

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
                }
            }
        }
    }

    private async void Benchmark_Click(object sender, RoutedEventArgs e)
    {
        //Process.Start(new ProcessStartInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "NSudo", "NSudoLC.exe"), $"-U:T -P:E -Wait -ShowWindowMode:Hide cmd /c \"{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "AutoGpuAffinity", "AutoGpuAffinity.exe")}\" --analyze \"{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "AutoGpuAffinity", "captures", "AutoGpuAffinity-010325220526", "CSVs")}\"") { CreateNoWindow = true  });


        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "NSudo", "NSudoLC.exe"),
                Arguments = $"-U:T -P:E -Wait -ShowWindowMode:Hide cmd /c \"{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "AutoGpuAffinity", "AutoGpuAffinity.exe")}\" --analyze \"{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "AutoGpuAffinity", "captures", "AutoGpuAffinity-010325221604", "CSVs")}\"",
                CreateNoWindow = true,
                RedirectStandardOutput = true
            }
        };
        process.Start();

        string output = await process.StandardOutput.ReadToEndAsync();

        Debug.WriteLine(output);
    }
}

