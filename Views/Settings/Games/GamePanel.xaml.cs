using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.Win32;
using System.Diagnostics;
using System.Management;
using System.Numerics;
using System.Runtime.InteropServices;
using System.ServiceProcess;

namespace AutoOS.Views.Settings.Games;

public sealed partial class GamePanel : UserControl
{
    [DllImport("kernel32.dll")]
    static extern bool SetProcessWorkingSetSize(IntPtr process, int min, int max);
    public string Launcher { get; set; }
    public string LauncherLocation { get; set; }
    public string DataLocation { get; set; }
    public string GameLocation { get; set; }
    public string CatalogNamespace { get; set; }
    public string CatalogItemId { get; set; }
    public string AppName { get; set; }
    public string InstallLocation { get; set; }
    public string LaunchExecutable { get; set; }
    public string GameID { get; set; }
    public ImageSource ImageTall { get; set; }
    public ImageSource ImageWide { get; set; }
    public string Title
    {
        get { return (string)GetValue(TitleProperty); }
        set { SetValue(TitleProperty, value); }
    }

    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register("Title", typeof(string), typeof(GamePanel), new PropertyMetadata(null));

    public string Developers
    {
        get { return (string)GetValue(DevelopersProperty); }
        set { SetValue(DevelopersProperty, value); }
    }

    public static readonly DependencyProperty DevelopersProperty =
        DependencyProperty.Register("Developers", typeof(string), typeof(GamePanel), new PropertyMetadata(null));

    public List<string> Genres { get; set; } = [];
    public List<string> Features { get; set; } = [];
    public double Rating { get; set; }
    public string PlayTime { get; set; }
    public string Description { get; set; }

    public bool UpdateIsAvailable
    {
        get => UpdateBadge.Visibility == Visibility.Visible;
        set => UpdateBadge.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
    }

    private DispatcherTimer gameWatcherTimer;
    private bool? previousGameState = null;
    private bool? previousExplorerState = null;
    private bool servicesState = false;
    private bool isScaledUp = false;
    private readonly TimeSpan animationDuration = TimeSpan.FromMilliseconds(300);
    private readonly CubicEase easingFunction = new CubicEase { EasingMode = EasingMode.EaseOut };

    public GamePanel()
    {
        this.InitializeComponent();
        this.Unloaded += GamePanel_Unloaded;

        _lightSpotVisual = ElementCompositionPreview.GetElementVisual(LightSpot);
    }

    private void GamePanel_Unloaded(object sender, RoutedEventArgs e)
    {
        // stop watchers when navigation to another page
        gameWatcherTimer?.Stop();
    }

    private void Animate(DependencyObject target, string property, double to, TimeSpan? duration = null)
    {
        var anim = new DoubleAnimation
        {
            To = to,
            Duration = duration ?? animationDuration,
            EasingFunction = easingFunction,
            EnableDependentAnimation = true
        };

        Storyboard.SetTarget(anim, target);
        Storyboard.SetTargetProperty(anim, property);

        var sb = new Storyboard();
        sb.Children.Add(anim);
        sb.Begin();
    }

    private void SetProjectionDirect(double tiltX, double tiltY, double offsetX, double offsetY)
    {
        PanelProjection.RotationX = tiltX;
        PanelProjection.RotationY = tiltY;
        PanelProjection.LocalOffsetX = offsetX;
        PanelProjection.LocalOffsetY = offsetY;
    }

    private void StartScaleAnimation(double toX, double toY)
    {
        Animate(PanelTransform, "ScaleX", toX);
        Animate(PanelTransform, "ScaleY", toY);
    }

    private void ResetHoverEffects()
    {
        SetProjectionDirect(0, 0, 0, 0);
    }

    private void AutoScrollHoverEffectView_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        AutoScrollHoverEffectViewTitle.IsPlaying = true;
        AutoScrollHoverEffectViewDescription.IsPlaying = true;
    }

    private Microsoft.UI.Composition.Visual _lightSpotVisual;

    private void AutoScrollHoverEffectView_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (!isScaledUp)
        {
            StartScaleAnimation(1.02, 1.02);
            isScaledUp = true;
        }

        var pos = e.GetCurrentPoint(Panel).Position;
        var canvas = LightSpot.Parent as UIElement;
        if (canvas == null || _lightSpotVisual == null)
            return;

        var transform = Panel.TransformToVisual(canvas);
        var canvasPos = transform.TransformPoint(pos);

        _lightSpotVisual.Offset = new Vector3(
            (float)(canvasPos.X - LightSpot.Width / 2),
            (float)(canvasPos.Y - LightSpot.Height / 2),
            0);

        if (_lightSpotVisual.Opacity == 0)
            _lightSpotVisual.Opacity = 0.35f;

        double centerX = Panel.ActualWidth / 2;
        double centerY = Panel.ActualHeight / 2;

        double deltaX = pos.X - centerX;
        double deltaY = pos.Y - centerY;

        double maxTilt = 4;
        double maxMove = 4;

        double tiltX = -deltaY / centerY * maxTilt;
        double tiltY = deltaX / centerX * maxTilt;
        double translateX = deltaX / centerX * maxMove;
        double translateY = deltaY / centerY * maxMove;

        SetProjectionDirect(tiltX, tiltY, translateX, translateY);
    }

    private void AutoScrollHoverEffectView_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        EndHoverEffect();
        _lightSpotVisual.Opacity = 0f;
    }

    private void AutoScrollHoverEffectView_PointerCanceled(object sender, PointerRoutedEventArgs e)
    {
        EndHoverEffect();
    }

    private void AutoScrollHoverEffectView_PointerCaptureLost(object sender, PointerRoutedEventArgs e)
    {
        EndHoverEffect();
    }

    private void EndHoverEffect()
    {
        AutoScrollHoverEffectViewTitle.IsPlaying = false;
        AutoScrollHoverEffectViewDescription.IsPlaying = false;
        ResetHoverEffects();
        StartScaleAnimation(1.0, 1.0);
        isScaledUp = false;
    }

    void StartGameWatcher(Func<bool> isGameRunning)
    {
        // define check rate
        gameWatcherTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(750)
        };

        // check services state
        servicesState = new ServiceController("Beep").Status == ServiceControllerStatus.Running;

        void TickHandler()
        {
            bool isRunning = isGameRunning();
            bool explorerRunning = Process.GetProcessesByName("explorer").Length > 0;

            DispatcherQueue.TryEnqueue(() =>
            {
                if (previousGameState != isRunning)
                {
                    Launch.IsEnabled = !isRunning;

                    if (isRunning)
                    {
                        if (!servicesState && stopProcesses.Visibility == Visibility.Collapsed)
                            stopProcesses.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        if (!servicesState && stopProcesses.Visibility == Visibility.Visible)
                            stopProcesses.Visibility = Visibility.Collapsed;
                    }

                    previousGameState = isRunning;
                }

                if (!servicesState && previousExplorerState != (isRunning && !explorerRunning))
                {
                    launchExplorer.Visibility = (isRunning && !explorerRunning) ? Visibility.Visible : Visibility.Collapsed;
                    previousExplorerState = isRunning && !explorerRunning;
                }
            });
        }

        gameWatcherTimer.Tick += (s, e) => TickHandler();

        TickHandler();
        gameWatcherTimer.Start();
    }

    public void CheckGameRunning()
    {
        if (Launcher == "Epic Games")
        {
            string offlineExecutable = Path.GetFileNameWithoutExtension(LaunchExecutable);
            string onlineExecutable = Title switch
            {
                "Fortnite" => "FortniteClient-Win64-Shipping",
                "Fall Guys" => "FallGuys_client_game.exe",
                _ => string.Empty
            };
            if (Title == "Fall Guys") offlineExecutable = "FallGuys_client.exe";

            StartGameWatcher(() =>
                Process.GetProcessesByName(Path.GetFileNameWithoutExtension(offlineExecutable)).Length > 0 ||
                (!string.IsNullOrEmpty(onlineExecutable) &&
                 Process.GetProcessesByName(Path.GetFileNameWithoutExtension(onlineExecutable)).Length > 0)
            );
        }
        else if (Launcher == "Steam")
        {
            var exeNames = Directory.GetFiles(InstallLocation, "*.exe")
                    .Select(f => Path.GetFileNameWithoutExtension(f))
                    .ToList();

            if (exeNames.Count == 0) return;

            StartGameWatcher(() =>
                exeNames.Any(name =>
                    Process.GetProcessesByName(name).Length > 0)
            );
        }
        else if (Launcher == "Ryujinx")
        {
            StartGameWatcher(() =>
            {
                using var searcher = new ManagementObjectSearcher($"SELECT CommandLine FROM Win32_Process WHERE Name = '{Path.GetFileName(LauncherLocation)}'");
                foreach (ManagementObject obj in searcher.Get())
                {
                    string cmdLine = obj["CommandLine"]?.ToString() ?? "";
                    if (cmdLine.Contains($@"-r ""{DataLocation}"" -fullscreen ""{InstallLocation}"""))
                        return true;
                }
                return false;
            });
        }
    }

    private async void Launch_Click(object sender, RoutedEventArgs e)
    {
        // check services state
        if (new ServiceController("Beep").Status == ServiceControllerStatus.Running)
        {
            var contentDialog = new ContentDialog
            {
                Title = "Attention Required",
                Content = "Are you sure that you want to launch " + Title + " while Services & Drivers are enabled?",
                PrimaryButtonText = "Yes",
                SecondaryButtonText = "No",
                DefaultButton = ContentDialogButton.Secondary,
                XamlRoot = this.XamlRoot
            };

            contentDialog.Resources["ContentDialogMaxWidth"] = 850;

            ContentDialogResult result = await contentDialog.ShowAsync();

            // check result
            if (result == ContentDialogResult.Secondary)
            {
                return;
            }
        }

        if (Launcher == "Epic Games")
        {
            // start game silently
            Process.Start(new ProcessStartInfo($"com.epicgames.launcher://apps/{CatalogNamespace}%3A{CatalogItemId}%3A{AppName}?action=launch&silent=true") { UseShellExecute = true });
        }
        else if (Launcher == "Steam")
        {
            Process.Start(new ProcessStartInfo { FileName = @"C:\Program Files (x86)\Steam\steam.exe", Arguments = $"-applaunch {GameID} -silent" });
        }
        else if (Launcher == "Ryujinx")
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = LauncherLocation,
                Arguments = $@"-r ""{DataLocation}"" -fullscreen ""{InstallLocation}""",
                CreateNoWindow = true,
            };

            Process.Start(startInfo);
        }
    }

    private void StopProcesses_Click(object sender, RoutedEventArgs e)
    {
        // close dllhost processes
        foreach (var process in Process.GetProcessesByName("dllhost"))
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher(
                    $"SELECT ProcessId, CommandLine FROM Win32_Process WHERE Name = 'dllhost.exe'"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        string cmdLine = obj["CommandLine"]?.ToString() ?? "";
                        int pid = Convert.ToInt32(obj["ProcessId"]);

                        if (cmdLine.Contains("/PROCESSID", StringComparison.OrdinalIgnoreCase))
                        {
                            try
                            {
                                var proc = Process.GetProcessById(pid);
                                proc.Kill();
                                proc.WaitForExit();
                            }
                            catch { }
                        }
                    }
                }
            }
            catch { }
        }

        // close executables
        Registry.SetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon", "AutoRestartShell", 0, RegistryValueKind.DWord);

        string[] processNames = {
            "ApplicationFrameHost",
            "CrashReportClient",
            "ctfmon",
            "DataExchangeHost",
            "EasyAntiCheat_EOS",
            "EpicGamesLauncher",
            "explorer",
            //"Files",
            "FortniteClient-Win64-Shipping_EAC_EOS",
            "GameBar",
            "GameBarFTServer",
            "mobsync",
            "rundll32",
            "RuntimeBroker",
            "SearchHost",
            "secd",
            "ShellExperienceHost",
            "SpatialAudioLicenseSrv",
            "sppsvc",
            "StartMenuExperienceHost",
            "SystemSettingsBroker",
            "TrustedInstaller",
            "useroobebroker",
            "WMIADAP",
            "WmiPrvSE",
            "WUDFHost"
        };

        foreach (var name in processNames)
        {
            foreach (var process in Process.GetProcessesByName(name))
            {
                try { process.Kill(); process.WaitForExit(); } catch { }
            }

            foreach (var process in Process.GetProcessesByName(name))
            {
                try { process.Kill(); process.WaitForExit(); } catch { }
            }
        }

        Registry.SetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon", "AutoRestartShell", 1, RegistryValueKind.DWord);

        // stop services
        string[] serviceNames = {
            "AudioEndpointBuilder",
            "AppXSvc",
            "Appinfo",
            "CaptureService",
            "cbdhsvc",
            "ClipSvc",
            "CryptSvc",
            "DevicesFlowUserSvc",
            "DoSvc",
            "gpsvc",
            "InstallService",
            "msiserver",
            "netprofm",
            "nsi",
            "ProfSvc",
            "StateRepository",
            "TextInputManagementService",
            "TrustedInstaller",
            "UdkUserSvc",
            "UserManager",
            "WFDSConMgrSvc",
            "Windhawk",
            "Winmgmt"
        };

        foreach (var serviceName in serviceNames)
        {
            try
            {
                var searcher = new ManagementObjectSearcher($"SELECT ProcessId FROM Win32_Service WHERE Name LIKE '{serviceName}%'");
                foreach (ManagementObject service in searcher.Get())
                {
                    try
                    {
                        int pid = Convert.ToInt32(service["ProcessId"]);
                        var process = Process.GetProcessById(pid);
                        process.Kill();
                        process.WaitForExit();
                    }
                    catch { }
                }
            }
            catch { }
        }

        foreach (var process in Process.GetProcesses())
        {
            try
            {
                SetProcessWorkingSetSize(process.Handle, -1, -1);
            }
            catch { }
        }
    }

    private void LaunchExplorer_Click(object sender, RoutedEventArgs e)
    {
        // start audioendpoint builder
        using (ServiceController service = new ServiceController("AudioEndpointBuilder"))
        {
            if (service.Status == ServiceControllerStatus.Stopped)
            {
                service.Start();
            }
        }

        // launch ctfmon
        Process.Start("ctfmon.exe");

        // launch explorer
        Process.Start("explorer.exe");

        // start windhawk service
        using (ServiceController service = new ServiceController("Windhawk"))
        {
            if (service.Status == ServiceControllerStatus.Stopped)
            {
                service.Start();
            }
        }
    }

    private async void Settings_Click(object sender, RoutedEventArgs e)
    {
        var gameSettings = new GameSettings
        {
            ImageWide = ImageWide,
            Title = Title,
            Developers = Developers,
            Genres = Genres,
            Features = Features,
            Rating = Rating,
            PlayTime = PlayTime,
            Description = Description,
            InstallLocation = InstallLocation
        };

        var contentDialog = new ContentDialog
        {
            Content = gameSettings,
            CloseButtonText = "Close",
            XamlRoot = this.XamlRoot,
        };

        contentDialog.Resources["ContentDialogMinWidth"] = 600;
        contentDialog.Resources["ContentDialogMaxWidth"] = 850;
        contentDialog.Resources["ContentDialogMinHeight"] = 250;
        contentDialog.Resources["ContentDialogMaxHeight"] = 1500;
        _ = await contentDialog.ShowAsync();
    }
}