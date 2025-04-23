using Microsoft.Win32;
using System.Diagnostics;

namespace AutoOS.Views.Settings;

public sealed partial class TimerPage : Page
{
    private bool isInitializingTimerResolutionState = true;
    public TimerPage()
    {
        InitializeComponent();
        GetRequestedResolution();
    }

    private void GetRequestedResolution()
    {
        int start = 5000, step = 1;
        for (int i = start; i <= 5100; i += step)
            Resolution.Items.Add(new ComboBoxItem { Content = i.ToString() });

        int? requestedResolution = (int?)Registry.GetValue(@"HKEY_CURRENT_USER\SOFTWARE\AutoOS", "RequestedResolution", null);
        if (requestedResolution.HasValue)
        {
            int index = (requestedResolution.Value - start) / step;
            if (index >= 0 && index < Resolution.Items.Count) Resolution.SelectedIndex = index;
        }
        else
        {
            Resolution.SelectedIndex = 67;
        }

        isInitializingTimerResolutionState = false;
    }

    private void Resolution_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (isInitializingTimerResolutionState) return;

        if (Resolution.SelectedItem is ComboBoxItem selectedItem)
        {
            int selectedResolution = int.Parse(selectedItem.Content.ToString());
            Registry.SetValue(@"HKEY_CURRENT_USER\SOFTWARE\AutoOS", "RequestedResolution", selectedResolution);

            var processes = Process.GetProcessesByName("SetTimerResolution");
            if (processes.Length == 1)
            {
                processes[0].Kill();
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "TimerResolution", "SetTimerResolution.exe"),
                Arguments = $"--resolution {selectedResolution} --no-console",
                UseShellExecute = false,
                CreateNoWindow = true
            });
        }
    }
}

