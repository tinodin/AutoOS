using AutoOS.Views.Installer.Actions;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Win32;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Text.RegularExpressions;
using Windows.Storage;

namespace AutoOS.Views.Settings;

public sealed partial class GraphicsPage : Page
{
    private bool initialHDCPState = false;
    private bool isInitializingHDCPState = true;

    public GraphicsPage()
    {
        InitializeComponent();
        CheckDriverUpdate();
        GetHDCPState();
    }

    private async void CheckDriverUpdate()
    {
        updateCheck.ProgressBackground = ProcessActions.GetColor("LightNormal", "DarkNormal");

        // get current version
        var currentVersion = (await Task.Run(() => Process.Start(new ProcessStartInfo("nvidia-smi", "--query-gpu=driver_version --format=csv,noheader") { CreateNoWindow = true, RedirectStandardOutput = true })?.StandardOutput.ReadToEndAsync()))?.Trim();
        NvidiaCard.Description = "Current Version: " + currentVersion;

        try
        {
            using (HttpClient client = new HttpClient())
            {
                // check for newest driver version
                string html = await client.GetStringAsync("https://www.techspot.com/downloads/drivers/essentials/nvidia-geforce/");
                string pattern = @"<title>.*?(\d+\.\d+).*?</title>";
                var match = Regex.Match(html, pattern);
                string newestVersion = match.Groups[1].Value;

                // delay
                await Task.Delay(350);

                // check if update is needed
                if (string.Compare(newestVersion, currentVersion, StringComparison.Ordinal) > 0)
                {
                    updateCheck.IsChecked = false;
                    updateCheck.Content = "Update to " + newestVersion;
                }
                else if (string.Compare(newestVersion, currentVersion, StringComparison.Ordinal) == 0)
                {
                    updateCheck.IsChecked = false;
                    updateCheck.Content = "No updates available";
                    updateCheck.IsEnabled = false;
                }
            }
        }
        catch
        {
            //// delay
            //await Task.Delay(800);

            //// hide progress ring
            //updateCheckProgress.Visibility = Visibility.Collapsed;

            //// connection failed message
            //updateCheckButton.Content = "Update check failed";
            //updateCheckButton.IsEnabled = false;
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
        var picker = new FilePicker();
        picker.ShowAllFilesOption = false;
        picker.FileTypeChoices.Add("MSI Afterburner profile", new List<string> { "*.cfg" });
        var file = await picker.PickSingleFileAsync(App.MainWindow);

        if (file != null)
        {
            string fileContent = await FileIO.ReadTextAsync(file);

            if (fileContent.Contains("[Startup]"))
            {
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

