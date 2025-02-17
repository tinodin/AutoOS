using Microsoft.Win32;
using AutoOS.Views.Installer.Stages;
using Microsoft.UI.Xaml.Media;
using System.Diagnostics;
using Windows.UI;

namespace AutoOS.Views.Installer;

public sealed partial class InstallPage : Page
{
    public static TextBlock Status { get; private set; }
    public static ProgressBar Progress { get; private set; }
    public static InfoBar Info { get; private set; }
    public static Microsoft.UI.Xaml.Controls.ProgressRing ProgressRingControl { get; private set; }

    public InstallPage()
    {
        InitializeComponent();

        Actions();
    }

    private async void Actions() 
    {
        var navView = MainWindow.Instance.GetNavView();

        await Task.Delay(125);

        // disable all menu items
        foreach (var item in navView.MenuItems.OfType<NavigationViewItem>())
        {
            item.IsEnabled = false;
        }

        // rename footer item to installing autoos
        foreach (var item in navView.FooterMenuItems.OfType<NavigationViewItem>())
        {
            item.Content = "Installing AutoOS...";
        }

        Status = StatusText;
        Progress = ProgressBar;
        Info = InfoBar;
        ProgressRingControl = ProgressRingItem;
        Registry.SetValue(@"HKEY_CURRENT_USER\SOFTWARE\AutoOS", "Stage", 2, RegistryValueKind.DWord);
        using (RegistryKey key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\AutoOS"))
        {
            var stageValue = key?.GetValue("Stage");
            int stage = stageValue == null ? 1 : (int)stageValue;

            if (stage == 1)
            {
                ExecuteFirstStage();
            }
            else if (stage == 2)
            {
                Progress.Value = 30;
                ExecuteSecondStage();
            }
            else if (stage == 3)
            {
                Progress.Value = 85;
                ExecuteThirdStage();
            }
        }
    }

    private async void ExecuteFirstStage() 
    {
        await PreparingStage.Run();
        await PowerStage.Run();
        await RegistryStage.Run();
        await VisualStage.Run();
        await SecurityStage.Run();
        await BcdStage.Run();
        await FileSystemStage.Run();
        await MemoryManagementStage.Run();
        await EventTraceSessionsStage.Run();
        await ScheduledTasksStage.Run();
        await OptionalFeatureStage.Run();
    }

    private async void ExecuteSecondStage()
    {
        await PreparingStage.Run();
        await DriverStage.Run();
        //await NetworkStage.Run();
        //await AudioStage.Run();
        //await GraphicsStage.Run();
        //await DeviceStage.Run();
        //await TimeDateRegionStage.Run();
        //await ActivationStage.Run();
        //await AppxStage.Run();
        //await RuntimesStage.Run();
        //await BrowserStage.Run();
        await ApplicationStage.Run();
        //await GamesStage.Run();
        await ServicesStage.Run();
    }

    private async void ExecuteThirdStage()
    {
        await PreparingStage.Run();
        //await SchedulingStage.Run();
        //await TimerStage.Run();
        await CleanupStage.Run();

        InstallPage.Status.Text = "Installation finished";
        InstallPage.Info.Severity = InfoBarSeverity.Success;
        InstallPage.Progress.Foreground = new SolidColorBrush(Color.FromArgb(255, 108, 203, 95));
        InstallPage.ProgressRingControl.Foreground = new SolidColorBrush(Color.FromArgb(255, 108, 203, 95));

        InstallPage.Info.Title = "Restarting in 3...";
        await Task.Delay(1000);
        InstallPage.Info.Title = "Restarting in 2...";
        await Task.Delay(1000);
        InstallPage.Info.Title = "Restarting in 1...";
        await Task.Delay(1000);
        InstallPage.Info.Title = "Restarting...";
        await Task.Delay(750);
        ProcessStartInfo processStartInfo = new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = $"/c shutdown /r /t 0",
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        Process.Start(processStartInfo);
    }
}
