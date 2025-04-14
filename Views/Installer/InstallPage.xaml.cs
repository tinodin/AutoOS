using AutoOS.Views.Installer.Stages;
using Microsoft.Win32;

namespace AutoOS.Views.Installer;

public sealed partial class InstallPage : Page
{
    public static TextBlock Status { get; private set; }
    public static ProgressBar Progress { get; private set; }
    public static InfoBar Info { get; private set; }
    public static Microsoft.UI.Xaml.Controls.ProgressRing ProgressRingControl { get; private set; }
    public static Button ResumeButton { get; private set; }

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
        }
    }

    private async void ExecuteFirstStage()
    {
        await PreparingStage.Run();
        await TimeDateRegionStage.Run();
        await PowerStage.Run();
        await RegistryStage.Run();
        await SecurityStage.Run();
        await BcdStage.Run();
        await FileSystemStage.Run();
        await MemoryManagementStage.Run();
        await EventTraceSessionsStage.Run();
        await VisualStage.Run();
        await ActivationStage.Run();
        await GraphicsStage.Run();
        await NetworkStage.Run();
        await AudioStage.Run();
        await DeviceStage.Run();
        await AppxStage.Run();
        await ScheduledTasksStage.Run();
        await OptionalFeatureStage.Run();
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