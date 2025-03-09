using AutoOS.Views.Installer.Stages;
using Microsoft.Win32;

namespace AutoOS.Views.Installer;

public sealed partial class InstallPage : Page
{
    public static TextBlock Status { get; private set; }
    public static ProgressBar Progress { get; private set; }
    public static InfoBar Info { get; private set; }
    public static Microsoft.UI.Xaml.Controls.ProgressRing ProgressRingControl { get; private set; }
    public static Button ResumeButton{ get; private set; }

    public InstallPage()
    {
        InitializeComponent();
        Loaded += InstallPage_Loaded;
    }

    private void InstallPage_Loaded(object sender, RoutedEventArgs e)
    {
        // get navview
        var navView = MainWindow.Instance.GetNavView();

        // disable all menu items
        foreach (var item in navView.MenuItems.OfType<NavigationViewItem>())
        {
            item.IsEnabled = false;
        }

        // rename footer item to installing autoos...
        foreach (var item in navView.FooterMenuItems.OfType<NavigationViewItem>())
        {
            item.Content = "Installing AutoOS...";
        }

        Status = StatusText;
        Progress = ProgressBar;
        Info = InfoBar;
        ProgressRingControl = ProgressRingItem;
        ResumeButton = ResumeButtonItem;

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
        await NetworkStage.Run();
        await AudioStage.Run();
        await GraphicsStage.Run();
        await DeviceStage.Run();
        await TimeDateRegionStage.Run();
        await ActivationStage.Run();
        await AppxStage.Run();
        await RuntimesStage.Run();
        await BrowserStage.Run();
        await ApplicationStage.Run();
        await GamesStage.Run();
        await SchedulingStage.Run();
        await TimerStage.Run();
        await ServicesStage.Run();
        await CleanupStage.Run();
    }
}