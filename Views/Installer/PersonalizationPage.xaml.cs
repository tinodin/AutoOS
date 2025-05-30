﻿using Microsoft.Win32;
using System.Diagnostics;

namespace AutoOS.Views.Installer;

public sealed partial class PersonalizationPage : Page
{
    private string nsudoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "NSudo", "NSudoLC.exe");
    private bool isInitializingThemeState = true;
    private bool isInitializingSchedule = true;
    private bool isInitializingContextMenuState = true;
    private bool isInitializingTrayIconsState = true;
    private bool isInitializingTaskbarAlignmentState = true;

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
        // add theme items
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

    private async void Theme_Changed(object sender, RoutedEventArgs e)
    {
        if (isInitializingThemeState) return;

        //// declare theme
        //string theme = Themes.SelectedIndex == 0 ? @"C:\Windows\Resources\Themes\aero.theme" : @"C:\Windows\Resources\Themes\dark.theme";

        //// apply theme
        //await Task.Run(() =>
        //{
        //    Process.Start(new ProcessStartInfo
        //    {
        //        FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "ThemeTool", "ThemeTool.exe"),
        //        Arguments = $"ChangeTheme {theme}",
        //        UseShellExecute = false,
        //        CreateNoWindow = true
        //    });
        //});
    }

    private void GetSchedule()
    {
        using var key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\AutoOS");

        if (key.GetValue("LightTime") is string lightTimeStr && TimeSpan.TryParse(lightTimeStr, out var lightTime))
        {
            LightTime.Time = lightTime;
        }
        else
        {
            key.SetValue("LightTime", "07:00", RegistryValueKind.String);
            LightTime.Time = TimeSpan.Parse("07:00");
        }

        if (key.GetValue("DarkTime") is string darkTimeStr && TimeSpan.TryParse(darkTimeStr, out var darkTime))
        {
            DarkTime.Time = darkTime;
        }
        else
        {
            key.SetValue("DarkTime", "19:00", RegistryValueKind.String);
            DarkTime.Time = TimeSpan.Parse("19:00");
        }

        isInitializingSchedule = false;
    }

    private void LightMode_TimeChanged(object sender, TimePickerValueChangedEventArgs e)
    {
        if (isInitializingSchedule) return;

        using var key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\AutoOS");
        string timeString = e.NewTime.ToString(@"hh\:mm");
        key.SetValue("LightTime", timeString, RegistryValueKind.String);
    }

    private void DarkMode_TimeChanged(object sender, TimePickerValueChangedEventArgs e)
    {
        if (isInitializingSchedule) return;

        using var key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\AutoOS");
        string timeString = e.NewTime.ToString(@"hh\:mm");
        key.SetValue("DarkTime", timeString, RegistryValueKind.String);
    }

    private async void GetContextMenuState()
    {
        // get state
        using var key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}\InprocServer32");
        var value = key?.GetValue("LegacyContextMenu");

        if (value == null)
        {
            ContextMenu.IsOn = true;
            await Task.Run(() => Process.Start(new ProcessStartInfo { FileName = nsudoPath, Arguments = @"-U:P -P:E -Wait -ShowWindowMode:Hide cmd /c reg add ""HKEY_CURRENT_USER\Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}\InprocServer32"" /ve /t REG_SZ /d """" /f", CreateNoWindow = true }).WaitForExit());
        }
        else
        {
            ContextMenu.IsOn = (int)value == 1;
        }

        isInitializingContextMenuState = false;
    }

    private async void ContextMenu_Toggled(object sender, RoutedEventArgs e)
    {
        if (isInitializingContextMenuState) return;

        // set value
        if (ContextMenu.IsOn)
        {
            await Task.Run(() => Process.Start(new ProcessStartInfo { FileName = nsudoPath, Arguments = @"-U:P -P:E -Wait -ShowWindowMode:Hide cmd /c reg add ""HKEY_CURRENT_USER\Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}\InprocServer32"" /ve /t REG_SZ /d """" /f", CreateNoWindow = true }).WaitForExit());
        }
        else
        {
            await Task.Run(() => Process.Start(new ProcessStartInfo { FileName = nsudoPath, Arguments = @"-U:P -P:E -Wait -ShowWindowMode:Hide cmd /c reg delete ""HKEY_CURRENT_USER\Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}"" /f", CreateNoWindow = true }).WaitForExit());
        }
    }

    private void GetTrayIconsState()
    {
        // get state
        using var key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\AutoOS");
        var value = key?.GetValue("AlwaysShowTrayIcons");

        if (value == null)
        {
            key?.SetValue("AlwaysShowTrayIcons", 1, RegistryValueKind.DWord);
            TrayIcons.IsChecked = true;
        }
        else
        {
            TrayIcons.IsChecked = (int)value == 1;
        }

        isInitializingTrayIconsState = false;
    }

    private void TrayIcons_Click(object sender, RoutedEventArgs e)
    {
        if (isInitializingTrayIconsState) return;

        // set value
        using var key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\AutoOS");
        key?.SetValue("AlwaysShowTrayIcons", TrayIcons.IsChecked ?? false ? 1 : 0, RegistryValueKind.DWord);
    }

    private void GetTaskbarAlignmentState()
    {
        // get state
        using var key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced");
        var obj = key?.GetValue("TaskbarAl");
        int alignment = obj is int i ? i : 1;

        TaskbarAlignment.SelectedIndex = alignment switch
        {
            0 => 0,
            1 => 1,
            _ => 1
        };

        // change header icon
        TaskbarIcon.HeaderIcon = alignment switch
        {
            0 => new SymbolIcon(Symbol.AlignLeft),
            1 => new SymbolIcon(Symbol.AlignCenter),
            _ => null
        };

        isInitializingTaskbarAlignmentState = false;
    }


    private async void TaskbarAlignment_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (isInitializingTaskbarAlignmentState) return;

        // set value
        if (TaskbarAlignment.SelectedIndex == 0)
        {
            await Task.Run(() => Process.Start(new ProcessStartInfo { FileName = nsudoPath, Arguments = @"-U:P -P:E -Wait -ShowWindowMode:Hide reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced"" /v TaskbarAl /t REG_DWORD /d 0 /f", CreateNoWindow = true }).WaitForExit());
            TaskbarIcon.HeaderIcon = new SymbolIcon(Symbol.AlignLeft);
        }
        else
        {
            await Task.Run(() => Process.Start(new ProcessStartInfo { FileName = nsudoPath, Arguments = @"-U:P -P:E -Wait -ShowWindowMode:Hide reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced"" /v TaskbarAl /t REG_DWORD /d 1 /f", CreateNoWindow = true }).WaitForExit());
            TaskbarIcon.HeaderIcon = new SymbolIcon(Symbol.AlignCenter);
        }
    }
}