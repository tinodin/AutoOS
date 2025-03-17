using System.Diagnostics;
using Microsoft.Win32;

namespace AutoOS.Views.Settings;

public sealed partial class LoggingPage : Page
{
    private bool isInitializingETSState = true;

    public LoggingPage()
    {
        InitializeComponent();
        GetETSState();
    }
    public void GetETSState()
    {
        // check registry
        ETS.IsOn = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\WMI\Autologger") != null;
        isInitializingETSState = false;
    }

    private async void ETS_Toggled(object sender, RoutedEventArgs e)
    {
        if (isInitializingETSState) return;

        // remove infobar
        EventTraceSessionsInfo.Children.Clear();

        // add infobar
        EventTraceSessionsInfo.Children.Add(new InfoBar
        {
            Title = ETS.IsOn ? "Enabling Event Trace Sessions (ETS)..." : "Disabling Event Trace Sessions (ETS)...",
            IsClosable = false,
            IsOpen = true,
            Severity = InfoBarSeverity.Informational,
            Margin = new Thickness(5)
        });

        // toggle event trace sessions
        if (ETS.IsOn)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "NSudo", "NSudoLC.exe"),
                    Arguments = $"-U:T -P:E -Wait -ShowWindowMode:Hide regedit /s \"{Path.Combine(PathHelper.GetAppDataFolderPath(), "EventTraceSessions", "ets-enable.reg")}\"",
                    CreateNoWindow = true,
                }
            };
            process.Start();
        }
        else
        {
            Registry.LocalMachine.DeleteSubKeyTree(@"SYSTEM\CurrentControlSet\Control\WMI\Autologger", false);
        }

        // delay
        await Task.Delay(500);

        // remove infobar
        EventTraceSessionsInfo.Children.Clear();

        // add infobar
        EventTraceSessionsInfo.Children.Add(new InfoBar
        {
            Title = ETS.IsOn ? "Successfully enabled Event Trace Sessions (ETS)." : "Successfully disabled Event Trace Sessions (ETS).",
            IsClosable = false,
            IsOpen = true,
            Severity = InfoBarSeverity.Success,
            Margin = new Thickness(5)
        });

        // delay
        await Task.Delay(2000);

        // remove infobar
        EventTraceSessionsInfo.Children.Clear();
    }
}
