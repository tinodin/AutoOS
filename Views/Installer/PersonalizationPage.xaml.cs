using System.Diagnostics;
using Microsoft.Win32;

namespace AutoOS.Views.Installer;

public sealed partial class PersonalizationPage : Page
{
    private string nsudoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "NSudo", "NSudoLC.exe");
    private bool isInitializingThemeState = true;
    private bool isInitializingContextMenuState = true;
    private bool isInitializingTrayIconsState = true;
    private bool isInitializingTaskbarAlignmentState = true;

    public PersonalizationPage()
    {
        InitializeComponent();
        GetItems();
        GetTheme();
        GetContextMenuState();
        GetTaskbarAlignmentState();
        GetTrayIconsState();
    }

    public class GridViewItem
    {
        public string ImageSource { get; set; }
    }

    private void GetItems()
    {
        // add theme items
        Themes.ItemsSource = new List<GridViewItem>
        {
            new GridViewItem { ImageSource = "ms-appx:///Assets/Fluent/Light.jpg" },
            new GridViewItem { ImageSource = "ms-appx:///Assets/Fluent/Dark.jpg" }
        };
    }

    private void GetTheme()
    {
        using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
        int systemUsesLightTheme = (int?)key?.GetValue("SystemUsesLightTheme", 1) ?? 1;

        // select the theme
        Themes.SelectedIndex = systemUsesLightTheme == 1 ? 0 : 1;

        isInitializingThemeState = false;
    }


    private async void Theme_Changed(object sender, RoutedEventArgs e)
    {
        if (isInitializingThemeState) return;

        // declare theme
        string theme = Themes.SelectedIndex == 0 ? @"C:\Windows\Resources\Themes\aero.theme" : @"C:\Windows\Resources\Themes\dark.theme";

        // rename settings
        await Task.Run(() => Process.Start(new ProcessStartInfo { FileName = nsudoPath, Arguments = @"-U:T -P:E -Wait -ShowWindowMode:Hide cmd /c taskkill /f /im SystemSettings.exe & ren C:\\Windows\\ImmersiveControlPanel\\SystemSettings.exe SystemSettings.exee", CreateNoWindow = true }).WaitForExit());

        // change theme
        await Task.Run(() => Process.Start(new ProcessStartInfo { FileName = nsudoPath, Arguments = $"-U:P -P:E -Wait -ShowWindowMode:Hide cmd /c start \"\" \"{theme}\"", CreateNoWindow = true }).WaitForExit());

        await Task.Delay(1000);

        // rename settings back
        await Task.Run(() => Process.Start(new ProcessStartInfo { FileName = nsudoPath, Arguments = @"-U:T -P:E -Wait -ShowWindowMode:Hide cmd /c taskkill /f /im SystemSettings.exe & ren C:\\Windows\\ImmersiveControlPanel\\SystemSettings.exee SystemSettings.exe", CreateNoWindow = true }).WaitForExit());
    }


    private void GetContextMenuState()
    {
        // get state
        using var key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\AutoOS");
        var value = key?.GetValue("LegacyContextMenu");

        if (value == null)
        {
            key?.SetValue("LegacyContextMenu", 1, RegistryValueKind.DWord);
            ContextMenu.IsOn = true;
        }
        else
        {
            ContextMenu.IsOn = (int)value == 1;
        }

        isInitializingContextMenuState = false;
    }

    private void ContextMenu_Toggled(object sender, RoutedEventArgs e)
    {
        if (isInitializingContextMenuState) return;

        // set value
        using var key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\AutoOS");
        key?.SetValue("LegacyContextMenu", ContextMenu.IsOn ? 1 : 0, RegistryValueKind.DWord);
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
        using var key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\AutoOS");
        string value = key?.GetValue("TaskbarAlignment") as string ?? "Center";

        TaskbarAlignment.SelectedIndex = value switch
        {
            "Left" => 0,
            "Center" => 1,
            _ => 1
        };

        // change header icon
        TaskbarIcon.HeaderIcon = value switch
        {
            "Left" => new SymbolIcon(Symbol.AlignLeft),
            "Center" => new SymbolIcon(Symbol.AlignCenter),
            _ => new SymbolIcon(Symbol.AlignCenter)
        };

        isInitializingTaskbarAlignmentState = false;
    }


    private void TaskbarAlignment_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (isInitializingTaskbarAlignmentState) return;

        // set value
        using var key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\AutoOS");
        string alignment = TaskbarAlignment.SelectedIndex == 0 ? "Left" : "Center";
        key?.SetValue("TaskbarAlignment", alignment, RegistryValueKind.String);

        // change header icon
        TaskbarIcon.HeaderIcon = new SymbolIcon(alignment == "Left" ? Symbol.AlignLeft : Symbol.AlignCenter);
    }
}

