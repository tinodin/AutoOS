using AutoOS.Helpers;
using Microsoft.Win32;
using System.Diagnostics;
using System.Management;
using Windows.Storage;

namespace AutoOS.Views.Settings;

public sealed partial class GraphicsPage : Page
{
    private bool initialHDCPState = false;
    private bool isInitializingHDCPState = true;

    public GraphicsPage()
    {
        InitializeComponent();
        GetGPUState();
        GetHDCPState();
    }
    private async void GetGPUState()
    {
        using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_VideoController"))
        {
            foreach (var obj in searcher.Get())
            {
                string name = obj["Name"]?.ToString();
                string version = obj["DriverVersion"]?.ToString();

                if (name != null)
                {
                    if (name.Contains("NVIDIA", StringComparison.OrdinalIgnoreCase))
                    {
                        GpuCard.HeaderIcon = new BitmapIcon { UriSource = new Uri("ms-appx:///Assets/Fluent/Nvidia.png"), ShowAsMonochrome = false };
                        GpuCard.Header = "NVIDIA Driver";
                        GpuCard.Description = "Current Version: " + (await Task.Run(() => Process.Start(new ProcessStartInfo("nvidia-smi", "--query-gpu=driver_version --format=csv,noheader") { CreateNoWindow = true, RedirectStandardOutput = true })?.StandardOutput.ReadToEndAsync()))?.Trim();
                    }
                    if (name.Contains("AMD", StringComparison.OrdinalIgnoreCase) || name.Contains("Radeon", StringComparison.OrdinalIgnoreCase))
                    {
                        GpuCard.HeaderIcon = new BitmapIcon { UriSource = new Uri("ms-appx:///Assets/Fluent/Amd.png"), ShowAsMonochrome = false };
                        GpuCard.Header = "AMD Driver";
                    }
                    if (name.Contains("Intel", StringComparison.OrdinalIgnoreCase))
                    {
                        GpuCard.HeaderIcon = new BitmapIcon { UriSource = new Uri("ms-appx:///Assets/Fluent/Intel.png"), ShowAsMonochrome = false };
                        GpuCard.Header = "Intel Driver";
                        GpuCard.Description = "Current Version: " + (version?.Split('.')[2] + "." + version?.Split('.')[3]);
                    }
                }
            }
        }

        UpdateCheck.IsChecked = true;
    }

    private async void UpdateCheck_Checked(object sender, RoutedEventArgs e)
    {
        if (UpdateCheck.Content.ToString().Contains("Update to"))
        {
            //UpdateCheck.CheckedContent = "Downloading the latest NVIDIA driver...";

            //var (_, newestVersion, newestDownloadUrl) = await NvidiaHelper.CheckUpdate();

            //await Task.Delay(500);

            //UpdateCheck.CheckedContent = "Extracting the NVIDIA driver...";
        }
        else
        {
            UpdateCheck.CheckedContent = "Checking for updates...";

            using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_VideoController"))
            {
                foreach (var obj in searcher.Get())
                {
                    string name = obj["Name"]?.ToString();

                    if (name != null)
                    {
                        if (name.Contains("NVIDIA", StringComparison.OrdinalIgnoreCase))
                        {
                            try
                            {
                                var (currentVersion, newestVersion, newestDownloadUrl) = await NvidiaHelper.CheckUpdate();

                                // delay
                                await Task.Delay(800);

                                // check if update is needed
                                if (string.Compare(newestVersion, currentVersion, StringComparison.Ordinal) > 0)
                                {
                                    UpdateCheck.IsChecked = false;
                                    UpdateCheck.Content = "Update to " + newestVersion;
                                }
                                else if (string.Compare(newestVersion, currentVersion, StringComparison.Ordinal) == 0)
                                {
                                    UpdateCheck.IsChecked = false;
                                    UpdateCheck.Content = "No updates available";
                                }
                            }
                            catch
                            {
                                // delay
                                await Task.Delay(800);

                                // connection failed message
                                UpdateCheck.IsChecked = false;
                                UpdateCheck.Content = "Failed to check for updates";
                            }
                        }
                        if (name.Contains("AMD", StringComparison.OrdinalIgnoreCase) || name.Contains("Radeon", StringComparison.OrdinalIgnoreCase))
                        {

                        }
                        if (name.Contains("Intel", StringComparison.OrdinalIgnoreCase))
                        {

                        }
                    }
                }
            }
        }  
    }

    private void GetHDCPState()
    {
        // get registry values
        for (int i = 0; i <= 9; i++)
        {
            if (Registry.GetValue($@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Class\{{4d36e968-e325-11ce-bfc1-08002be10318}}\000{i}", "ProviderName", null)?.ToString() == "NVIDIA" &&
                (int?)Registry.GetValue($@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Class\{{4d36e968-e325-11ce-bfc1-08002be10318}}\000{i}", "RMHdcpKeyglobZero", null) == 0)
            {
                initialHDCPState = true;
                HDCP.IsOn = true;
            }
        }
        isInitializingHDCPState = false;
    }

    private async void HDCP_Toggled(object sender, RoutedEventArgs e)
    {
        // return if still initializing
        if (isInitializingHDCPState) return;

        // remove infobar
        NvidiaInfo.Children.Clear();

        // add infobar
        NvidiaInfo.Children.Add(new InfoBar
        {
            Title = HDCP.IsOn ? "Enabling High-Bandwidth Digital Content Protection (HDCP)..." : "Disabling High-Bandwidth Digital Content Protection (HDCP)...",
            IsClosable = false,
            IsOpen = true,
            Severity = InfoBarSeverity.Informational,
            Margin = new Thickness(5)
        });

        // toggle hdcp
        for (int i = 0; i <= 9; i++)
        {
            var path = $@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Class\{{4d36e968-e325-11ce-bfc1-08002be10318}}\000{i}";
            if (Registry.GetValue(path, "ProviderName", null)?.ToString() == "NVIDIA")
            {
                if (HDCP.IsOn)
                {
                    Registry.SetValue(path, "RMHdcpKeyglobZero", 0, RegistryValueKind.DWord);
                    using (var key = Registry.LocalMachine.OpenSubKey($@"SYSTEM\CurrentControlSet\Control\Class\{{4d36e968-e325-11ce-bfc1-08002be10318}}\000{i}", true))
                    {
                        key?.DeleteValue("RmDisableHdcp22", false);
                        key?.DeleteValue("RmSkipHdcp22Init", false);
                    }
                }
                else
                {
                    Registry.SetValue(path, "RMHdcpKeyglobZero", 1, RegistryValueKind.DWord);
                    Registry.SetValue(path, "RmDisableHdcp22", 1, RegistryValueKind.DWord);
                    Registry.SetValue(path, "RmSkipHdcp22Init", 1, RegistryValueKind.DWord);
                }
            }
        }

        // delay
        await Task.Delay(400);

        // remove infobar
        NvidiaInfo.Children.Clear();

        // add infobar
        var infoBar = new InfoBar
        {
            Title = HDCP.IsOn ? "Successfully enabled High-Bandwidth Digital Content Protection (HDCP)." : "Successfully disabled High-Bandwidth Digital Content Protection (HDCP).",
            IsClosable = false,
            IsOpen = true,
            Severity = InfoBarSeverity.Success,
            Margin = new Thickness(5)
        };
        NvidiaInfo.Children.Add(infoBar);

        // add restart button if needed
        if (HDCP.IsOn != initialHDCPState)
        {
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
            // delay
            await Task.Delay(2000);

            // remove infobar
            NvidiaInfo.Children.Clear();
        }
    }

    private async void BrowseMsi_Click(object sender, RoutedEventArgs e)
    {
        // disable the button to avoid double-clicking
        var senderButton = sender as Button;
        senderButton.IsEnabled = false;

        // remove infobar
        MsiAfterburnerInfo.Children.Clear();

        // add infobar
        MsiAfterburnerInfo.Children.Add(new InfoBar
        {
            Title = "Please select a MSI Afterburner profile (.cfg).",
            IsClosable = false,
            IsOpen = true,
            Severity = InfoBarSeverity.Informational,
            Margin = new Thickness(5)
        });

        // delay
        await Task.Delay(300);

        // launch file picker
        var picker = new FilePicker(App.MainWindow);
        picker.ShowAllFilesOption = false;
        picker.FileTypeChoices.Add("MSI Afterburner profile", new List<string> { "*.cfg" });
        var file = await picker.PickSingleFileAsync();

        if (file != null)
        {
            string fileContent = await FileIO.ReadTextAsync(file);

            if (fileContent.Contains("[Startup]"))
            {
                // re-enable the button
                senderButton.IsEnabled = true;

                // remove infobar
                MsiAfterburnerInfo.Children.Clear();

                // add infobar
                MsiAfterburnerInfo.Children.Add(new InfoBar
                {
                    Title = "Applying the MSI Afterburner profile...",
                    IsClosable = false,
                    IsOpen = true,
                    Severity = InfoBarSeverity.Informational,
                    Margin = new Thickness(5)
                });

                // delay
                await Task.Delay(300);

                // delete old profiles
                Directory.GetFiles(@"C:\Program Files (x86)\MSI Afterburner\Profiles")
                .Where(file => Path.GetFileName(file) != "MSIAfterburner.cfg")
                .ToList()
                .ForEach(File.Delete);

                // import profile
                File.Copy(file.Path, Path.Combine(@"C:\Program Files (x86)\MSI Afterburner\Profiles", file.Name), true);

                // apply profile
                await Task.Run(() => Process.Start(new ProcessStartInfo(@"C:\Program Files (x86)\MSI Afterburner\MSIAfterburner.exe") { Arguments = "/Profile1 /q" })?.WaitForExit());

                // remove infobar
                MsiAfterburnerInfo.Children.Clear();

                // add infobar
                MsiAfterburnerInfo.Children.Add(new InfoBar
                {
                    Title = "Successfully applied the MSI Afterburner profile.",
                    IsClosable = false,
                    IsOpen = true,
                    Severity = InfoBarSeverity.Success,
                    Margin = new Thickness(5)
                });

                // delay
                await Task.Delay(2000);

                // remove infobar
                MsiAfterburnerInfo.Children.Clear();
            }
            else
            {
                // re-enable the button
                senderButton.IsEnabled = true;

                // remove infobar
                MsiAfterburnerInfo.Children.Clear();

                // add infobar
                MsiAfterburnerInfo.Children.Add(new InfoBar
                {
                    Title = "The selected file is not a valid MSI Afterburner profile.",
                    IsClosable = false,
                    IsOpen = true,
                    Severity = InfoBarSeverity.Error,
                    Margin = new Thickness(5)
                });

                // delay
                await Task.Delay(2000);

                // remove infobar
                MsiAfterburnerInfo.Children.Clear();
            }
        }
        else
        {
            // re-enable the button
            senderButton.IsEnabled = true;

            // remove infobar
            MsiAfterburnerInfo.Children.Clear();
        }
    }

    private async void LaunchMsi_Click(object sender, RoutedEventArgs e)
    {
        // remove infobar
        MsiAfterburnerInfo.Children.Clear();

        // add infobar
        MsiAfterburnerInfo.Children.Add(new InfoBar
        {
            Title = "Launching MSI Afterburner...",
            IsClosable = false,
            IsOpen = true,
            Severity = InfoBarSeverity.Informational,
            Margin = new Thickness(5)
        });

        // launch
        await Task.Run(() => Process.Start(@"C:\Program Files (x86)\MSI Afterburner\MSIAfterburner.exe")?.WaitForInputIdle());

        // remove infobar
        MsiAfterburnerInfo.Children.Clear();

        // add infobar
        MsiAfterburnerInfo.Children.Add(new InfoBar
        {
            Title = "Successfully launched MSI Afterburner.",
            IsClosable = false,
            IsOpen = true,
            Severity = InfoBarSeverity.Success,
            Margin = new Thickness(5)
        });

        // delay
        await Task.Delay(2000);

        // remove infobar
        MsiAfterburnerInfo.Children.Clear();
    }

}

