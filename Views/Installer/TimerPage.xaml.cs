﻿using Microsoft.Win32;

namespace AutoOS.Views.Installer;

public sealed partial class TimerPage : Page
{
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

        using (var key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\AutoOS"))
        {
            if (key.GetValue("TimerResolution") == null)
            {
                key.SetValue("TimerResolution", "Automatic", RegistryValueKind.String);
                TimerResolution.SelectedIndex = 0;
                ResolutionSettings.IsEnabled = false;
            }
            else
            {
                var timerResolutionValue = key.GetValue("TimerResolution") as string;
                if (timerResolutionValue == "Automatic")
                {
                    TimerResolution.SelectedIndex = 0;
                    ResolutionSettings.IsEnabled = false;
                }
                else if (timerResolutionValue == "Manual")
                {
                    TimerResolution.SelectedIndex = 1;
                    ResolutionSettings.IsEnabled = true;
                }
            }

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
        }
    }

    private void TimerResolution_Changed(object sender, SelectionChangedEventArgs e)
    {
        using (var key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\AutoOS"))
        {
            if (TimerResolution.SelectedIndex == 0)
            {
                key.SetValue("TimerResolution", "Automatic", RegistryValueKind.String);
                ResolutionSettings.IsEnabled = false;
            }
            else if (TimerResolution.SelectedIndex == 1)
            {
                key.SetValue("TimerResolution", "Manual", RegistryValueKind.String);
                ResolutionSettings.IsEnabled = true;
            }
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

