using Microsoft.Win32;
using Windows.Storage;
using System.Diagnostics;

namespace AutoOS.Views.Installer;

public sealed partial class PersonalizationPage : Page
{
    private bool isInitializingThemeState = true;
    private bool isInitializingSchedule = true;
    private bool isInitializingContextMenuState = true;
    private bool isInitializingTrayIconsState = true;
    private bool isInitializingTaskbarAlignmentState = true;

    private readonly ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

    public PersonalizationPage()
    {
        InitializeComponent();
        GetItems();
        GetTheme();
        GetSchedule();
        GetContextMenuState();
        GetTaskbarAlignmentState();
        GetTrayIconsState();
    }

    public class GridViewItem
    {
        public string ImageSource { get; set; }
    }

    public class ThemeItem
    {
        public string ImageSource1 { get; set; }
        public string ImageSource2 { get; set; }
    }

    private void GetItems()
    {
        Themes.ItemsSource = new List<ThemeItem>
        {
            new ThemeItem { ImageSource1 = @"C:\Windows\Web\Wallpaper\Windows\img0.jpg", ImageSource2 = @"C:\Windows\Web\Wallpaper\Windows\img19.jpg" }
        };
    }

    private void GetTheme()
    {
        using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes");
        string currentTheme = key?.GetValue("CurrentTheme") as string ?? string.Empty;

        if (currentTheme == @"C:\Windows\resources\Themes\aero.theme" || currentTheme == @"C:\Windows\resources\Themes\dark.theme")
        {
            Themes.SelectedIndex = 0;
        }

        isInitializingThemeState = false;
    }

    private void Theme_Changed(object sender, RoutedEventArgs e)
    {
        if (isInitializingThemeState) return;
    }

    private void GetSchedule()
    {
        if (localSettings.Values["LightTime"] is string lightTimeStr && TimeSpan.TryParse(lightTimeStr, out var lightTime))
        {
            LightTime.Time = lightTime;
        }
        else
        {
            localSettings.Values["LightTime"] = "07:00";
            LightTime.Time = TimeSpan.Parse("07:00");
        }

        if (localSettings.Values["DarkTime"] is string darkTimeStr && TimeSpan.TryParse(darkTimeStr, out var darkTime))
        {
            DarkTime.Time = darkTime;
        }
        else
        {
            localSettings.Values["DarkTime"] = "19:00";
            DarkTime.Time = TimeSpan.Parse("19:00");
        }

        isInitializingSchedule = false;
    }
    private void LightMode_TimeChanged(object sender, TimePickerValueChangedEventArgs e)
    {
        if (isInitializingSchedule) return;

        localSettings.Values["LightTime"] = e.NewTime.ToString(@"hh\:mm");
    }

    private void DarkMode_TimeChanged(object sender, TimePickerValueChangedEventArgs e)
    {
        if (isInitializingSchedule) return;

        localSettings.Values["DarkTime"] = e.NewTime.ToString(@"hh\:mm");
    }

    private void GetContextMenuState()
    {        
        ContextMenu.IsOn = Registry.CurrentUser.OpenSubKey(@"Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}\InprocServer32") != null;

        isInitializingContextMenuState = false;
    }

    private async void ContextMenu_Toggled(object sender, RoutedEventArgs e)
    {
        if (isInitializingContextMenuState) return;

        if (ContextMenu.IsOn)
        {
            await Task.Run(() => Process.Start(new ProcessStartInfo
            {
                FileName = "reg.exe",
                Arguments = @"add ""HKEY_CURRENT_USER\Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}\InprocServer32"" /ve /t REG_SZ /d """" /f",
                CreateNoWindow = true,
                UseShellExecute = false
            })?.WaitForExit());
        }
        else
        {
            await Task.Run(() => Process.Start(new ProcessStartInfo
            {
                FileName = "reg.exe",
                Arguments = @"delete ""HKEY_CURRENT_USER\Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}"" /f",
                CreateNoWindow = true,
                UseShellExecute = false
            })?.WaitForExit());
        }
    }

    private void GetTrayIconsState()
    {
        if (localSettings.Values["AlwaysShowTrayIcons"] is int value)
        {
            TrayIcons.IsChecked = value == 1;
        }
        else
        {
            localSettings.Values["AlwaysShowTrayIcons"] = 1;
            TrayIcons.IsChecked = true;
        }

        isInitializingTrayIconsState = false;
    }

    private void TrayIcons_Click(object sender, RoutedEventArgs e)
    {
        if (isInitializingTrayIconsState) return;

        localSettings.Values["AlwaysShowTrayIcons"] = (TrayIcons.IsChecked ?? false) ? 1 : 0;
    }

    private void GetTaskbarAlignmentState()
    {
        using var key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced");
        var obj = key?.GetValue("TaskbarAl");
        int alignment = obj is int i && (i == 0 || i == 1) ? i : 1;

        TaskbarAlignment.SelectedIndex = alignment;
        TaskbarIcon.HeaderIcon = alignment == 0 ? new SymbolIcon(Symbol.AlignLeft) : new SymbolIcon(Symbol.AlignCenter);

        isInitializingTaskbarAlignmentState = false;
    }

    private async void TaskbarAlignment_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (isInitializingTaskbarAlignmentState) return;

        string value = TaskbarAlignment.SelectedIndex == 0 ? "0" : "1";
        Symbol icon = TaskbarAlignment.SelectedIndex == 0 ? Symbol.AlignLeft : Symbol.AlignCenter;

        await Task.Run(() => Process.Start(new ProcessStartInfo
        {
            FileName = "reg.exe",
            Arguments = $@"add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced"" /v TaskbarAl /t REG_DWORD /d {value} /f",
            CreateNoWindow = true,
            UseShellExecute = false
        })?.WaitForExit());

        TaskbarIcon.HeaderIcon = new SymbolIcon(icon);
    }
}