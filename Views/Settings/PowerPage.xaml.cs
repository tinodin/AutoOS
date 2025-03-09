using System.ServiceProcess;
using System.Diagnostics;
using Microsoft.Win32;

namespace AutoOS.Views.Settings;

public sealed partial class PowerPage : Page
{
    private bool isInitializingIdleStatesState = true;
    private bool isInitializingPowerServiceState = true;
    public PowerPage()
    {
        this.InitializeComponent();
        GetIdleState();
        GetPowerServiceState();
    }

    public async void GetIdleState()
    {
        var serviceController = new ServiceController("Power");

        if (serviceController.Status != ServiceControllerStatus.Running)
        {
            // remove infobar
            PowerInfo.Children.Clear();

            // add infobar
            var infoBar = new InfoBar
            {
                Title = "Idle states are being managed by your BIOS.",
                IsClosable = false,
                IsOpen = true,
                Severity = InfoBarSeverity.Warning,
                Margin = new Thickness(5)
            };
            PowerInfo.Children.Add(infoBar);

            // disable idle states toggle
            IdleStates.IsEnabled = false;
            return;
        }

        // get idle state
        var idleEnabled = await Task.Run(() =>
        {
            var searcher = new System.Management.ManagementObjectSearcher("SELECT PercentIdleTime FROM Win32_PerfFormattedData_PerfOS_Processor WHERE Name='_Total'");
            foreach (System.Management.ManagementObject obj in searcher.Get())
                if (obj["PercentIdleTime"] != null && Convert.ToInt32(obj["PercentIdleTime"]) > 0)
                    return true;
            return false;
        });

        // toggle idle state
        IdleStates.IsOn = idleEnabled;
        IdleStates.IsEnabled = true;
        isInitializingIdleStatesState = false;
    }

    public void GetPowerServiceState()
    {
        // toggle power service state
        using (var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\Power"))
        PowerService.IsOn = (int?)key?.GetValue("Start") == 2;

        // enable idle state toggle if running
        var serviceController = new ServiceController("Power");
        bool isRunning = serviceController.Status == ServiceControllerStatus.Running;
        IdleStates.IsEnabled = isRunning;

        // add restart button
        if (PowerService.IsOn && !isRunning || !PowerService.IsOn && isRunning)
        {
            // remove infobar
            PowerInfo.Children.Clear();

            // add infobar
            var infoBar = new InfoBar
            {
                Title = PowerService.IsOn ? "Successfully enabled the power service. A restart is required to apply the change." : "Successfully disabled the power service. A restart is required to apply the change.",
                IsClosable = false,
                IsOpen = true,
                Severity = InfoBarSeverity.Success,
                Margin = new Thickness(5)
            };
            infoBar.ActionButton = new Button
            {
                Content = "Restart",
                HorizontalAlignment = HorizontalAlignment.Right
            };
            ((Button)infoBar.ActionButton).Click += (s, args) =>
            Process.Start("shutdown", "/r /f /t 0");
            PowerInfo.Children.Add(infoBar);
        }

        isInitializingPowerServiceState = false;
    }

    private async void IdleStates_Toggled(object sender, RoutedEventArgs e)
    {
        if (isInitializingIdleStatesState) return;

        // remove infobar
        PowerInfo.Children.Clear();

        // add infobar
        PowerInfo.Children.Add(new InfoBar
        {
            Title = IdleStates.IsOn ? "Enabling idle states..." : "Disabling idle states...",
            IsClosable = false,
            IsOpen = true,
            Severity = InfoBarSeverity.Informational,
            Margin = new Thickness(5)
        });

        // delay
        await Task.Delay(400);

        // toggle idle state
        using (var process = Process.Start(new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = $"/c {(IdleStates.IsOn ? "powercfg /setacvalueindex scheme_current sub_processor 5d76a2ca-e8c0-402f-a133-2158492d58ad 0 && powercfg /setactive scheme_current" : "powercfg /setacvalueindex scheme_current sub_processor 5d76a2ca-e8c0-402f-a133-2158492d58ad 1 && powercfg /setactive scheme_current")}",
            WindowStyle = ProcessWindowStyle.Hidden,
            CreateNoWindow = true
        }))
        {
            process.WaitForExit();
        }


        // remove infobar
        PowerInfo.Children.Clear();

        // add infobar
        PowerInfo.Children.Add(new InfoBar
        {
            Title = IdleStates.IsOn ? "Successfully enabled idle states." : "Successfully disabled idle states.",
            IsClosable = false,
            IsOpen = true,
            Severity = InfoBarSeverity.Success,
            Margin = new Thickness(5)
        });

        // delay 
        await Task.Delay(2000);

        // remove infobar
        PowerInfo.Children.Clear();

        GetPowerServiceState();
    }

    private async void PowerService_Toggled(object sender, RoutedEventArgs e)
    {
        if (isInitializingPowerServiceState) return;

        // remove infobar
        PowerInfo.Children.Clear();

        // add infobar
        PowerInfo.Children.Add(new InfoBar
        {
            Title = PowerService.IsOn ? "Enabling the power service..." : "Disabling the power service...",
            IsClosable = false,
            IsOpen = true,
            Severity = InfoBarSeverity.Informational,
            Margin = new Thickness(5)
        });

        // delay
        await Task.Delay(400);

        // toggle power service
        using (var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\Power", true))
        {
            key.SetValue("Start", PowerService.IsOn ? 2 : 4, RegistryValueKind.DWord);
        }
        if (PowerService.IsOn && new ServiceController("Power").Status != ServiceControllerStatus.Running)
        {
            new ServiceController("Power").Start();
        }

        // remove infobar
        PowerInfo.Children.Clear();

        // add infobar
        var serviceController = new ServiceController("Power");
        bool isRunning = serviceController.Status == ServiceControllerStatus.Running;
        var infoBar = new InfoBar
        {
            Title = PowerService.IsOn ? "Successfully enabled the power service." : "Successfully disabled the power service.",
            IsClosable = false,
            IsOpen = true,
            Severity = InfoBarSeverity.Success,
            Margin = new Thickness(5)
        };
        PowerInfo.Children.Add(infoBar);

        // add restart button
        if (!PowerService.IsOn && isRunning)
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
            PowerInfo.Children.Clear();
        }
    }
}

