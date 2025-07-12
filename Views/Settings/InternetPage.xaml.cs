using System.Diagnostics;
using System.Net.NetworkInformation;
using Microsoft.Win32;

namespace AutoOS.Views.Settings;

public sealed partial class InternetPage : Page
{
    private bool initialWIFIState = false;
    private bool isInitializingWIFIState = true;
    private bool isInitializingWOLState = true;

    public InternetPage()
    {
        InitializeComponent();
        GetWIFIState();
        GetWOLState();
    }

    private void GetWIFIState()
    {
        // declare services and drivers
        var groups = new[]
        {
            (new[] { "WlanSvc", "Dhcp", "EventLog", "Wcmsvc" }, 2),
            (new[] { "NlaSvc", "WinHttpAutoProxySvc", "Netwtw10", "Netwtw14" }, 3),
            (new[] { "tdx", "vwififlt"}, 1)
        };

        // check if values match
        foreach (var group in groups)
        {
            foreach (var service in group.Item1)
            {
                using (var key = Registry.LocalMachine.OpenSubKey($@"SYSTEM\CurrentControlSet\Services\{service}"))
                {
                    if (key == null) continue;

                    var startValue = key.GetValue("Start");
                    if (startValue == null || (int)startValue != group.Item2)
                    {
                        isInitializingWIFIState = false;
                        return;
                    }
                }
            }
        }

        // check for enabled wifi adapters
        var output = Process.Start(new ProcessStartInfo("cmd.exe", "/C netsh interface show interface | findstr /i \"Wi-Fi\" | findstr /i \"Enabled\"") { CreateNoWindow = true, RedirectStandardOutput = true })?.StandardOutput.ReadToEnd();

        if (!string.IsNullOrEmpty(output))
        {
            initialWIFIState = true;
            WiFi.IsOn = true;
        }
        else
        {
            // if no wifi adapters are present hide the whole section
            if (NetworkInterface.GetAllNetworkInterfaces().Where(ni => ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211).Any())
            {
                isInitializingWIFIState = false;
                return;
            }
            else
            {
                WiFi_SettingsGroup.Visibility = Visibility.Collapsed;
            }
        }
        isInitializingWIFIState = false;
    }

    private async void WiFi_Toggled(object sender, RoutedEventArgs e)
    {
        if (isInitializingWIFIState) return;

        // remove infobar
        WiFiInfo.Children.Clear();

        // add infobar
        WiFiInfo.Children.Add(new InfoBar
        {
            Title = WiFi.IsOn ? "Enabling Wi-Fi..." : "Disabling Wi-Fi...",
            IsClosable = false,
            IsOpen = true,
            Severity = InfoBarSeverity.Informational,
            Margin = new Thickness(5)
        });

        // declare services and drivers
        var groups = new[]
        {
            (new[] { "WlanSvc", "Wcmsvc" }, 2),
            (new[] { "NlaSvc", "WinHttpAutoProxySvc", "Netwtw10", "Netwtw14" }, 3),
            (new[] { "tdx", "vwififlt"}, 1)
        };

        // set start values
        foreach (var group in groups)
        {
            foreach (var service in group.Item1)
            {
                using (var key = Registry.LocalMachine.OpenSubKey($@"SYSTEM\CurrentControlSet\Services\{service}", writable: true))
                {
                    if (key == null) continue;

                    Registry.SetValue($@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\{service}", "Start", WiFi.IsOn ? group.Item2 : 4);
                }
            }
        }

        // delay
        await Task.Delay(500);

        // remove infobar
        WiFiInfo.Children.Clear();

        // add infobar
        var infoBar = new InfoBar
        {
            Title = WiFi.IsOn ? "Successfully enabled Wi-Fi." : "Successfully disabled Wi-Fi.",
            IsClosable = false,
            IsOpen = true,
            Severity = InfoBarSeverity.Success,
            Margin = new Thickness(5)
        };
        WiFiInfo.Children.Add(infoBar);

        // add restart button if needed
        if (WiFi.IsOn != initialWIFIState)
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
            WiFiInfo.Children.Clear();
        }
    }

    private async void GetWOLState()
    {
        // hide toggle switch
        WOL.Visibility = Visibility.Collapsed;

        // check state
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $@"-ExecutionPolicy Bypass -File ""{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Scripts", "wol.ps1")}""",
                CreateNoWindow = true,
                RedirectStandardOutput = true
            }
        };
        process.Start();
        WOL.IsOn = (await process.StandardOutput.ReadToEndAsync()).Contains("ENABLED");

        // hide progress ring
        wolProgress.Visibility = Visibility.Collapsed;

        // show toggle
        WOL.Visibility = Visibility.Visible;

        isInitializingWOLState = false;
    }

    private async void WOL_Toggled(object sender, RoutedEventArgs e)
    {
        if (isInitializingWOLState) return;

        // remove infobar
        EthernetInfo.Children.Clear();

        // add infobar
        EthernetInfo.Children.Add(new InfoBar
        {
            Title = WOL.IsOn ? "Enabling Wake-on-LAN (WOL)..." : "Disabling Wake-on-LAN (WOL)...",
            IsClosable = false,
            IsOpen = true,
            Severity = InfoBarSeverity.Informational,
            Margin = new Thickness(5)
        });

        // toggle power service
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-ExecutionPolicy Bypass -File \"{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Scripts", WOL.IsOn ? "wol-enable.ps1" : "wol-disable.ps1")}\"",
                CreateNoWindow = true,
            }
        };

        process.Start();
        await process.WaitForExitAsync();

        // remove infobar
        EthernetInfo.Children.Clear();

        // add infobar
        EthernetInfo.Children.Add(new InfoBar
        {
            Title = WOL.IsOn ? "Successfully enabled Wake-on-LAN (WOL)" : "Successfully disabled Wake-on-LAN (WOL)",
            IsClosable = false,
            IsOpen = true,
            Severity = InfoBarSeverity.Success,
            Margin = new Thickness(5)
        });

        // delay
        await Task.Delay(2000);

        // remove infobar
        EthernetInfo.Children.Clear();
    }
}