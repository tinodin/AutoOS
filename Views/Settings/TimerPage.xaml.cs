using Microsoft.Win32;

namespace AutoOS.Views.Settings;

public sealed partial class TimerPage : Page
{
    public TimerPage()
    {
        InitializeComponent();
        GetRequestedResolution();
    }

    private void GetRequestedResolution()
    {
        int start = 5000, step = 5;
        for (int i = start; i <= 5200; i += step)
            Resolution.Items.Add(new ComboBoxItem { Content = i.ToString() });

        int? requestedResolution = (int?)Registry.GetValue(@"HKEY_CURRENT_USER\SOFTWARE\AutoOS", "RequestedResolution", null);
        if (requestedResolution.HasValue)
        {
            int index = (requestedResolution.Value - start) / step;
            if (index >= 0 && index < Resolution.Items.Count) Resolution.SelectedIndex = index;
        }
        else
        {
            Resolution.SelectedIndex = 14;
        }
    }

    private void Resolution_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (Resolution.SelectedItem is ComboBoxItem selectedItem)
        {
            int selectedResolution = int.Parse(selectedItem.Content.ToString());
            Registry.SetValue(@"HKEY_CURRENT_USER\SOFTWARE\AutoOS", "RequestedResolution", selectedResolution);
        }
    }
}

