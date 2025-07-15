using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.Win32;
using System.Diagnostics;
using System.Management;
using System.Numerics;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using Windows.System;

namespace AutoOS.Views.Settings.Games.HeaderCarousel;

[TemplatePart(Name = nameof(PART_TextPanel), Type = typeof(StackPanel))]
[TemplatePart(Name = nameof(PART_ShadowHost), Type = typeof(Grid))]
public partial class HeaderCarouselItem : Button
{
    private const string PART_TextPanel = "PART_TextPanel";
    private const string PART_ShadowHost = "PART_ShadowHost";

    private AutoScrollView _autoScrollTitle;
    private AutoScrollView _autoScrollDescription;

    private Button _launch;
    private Button _update;
    private Button _stopProcesses;
    private Button _launchExplorer;
    private Button _settings;

    private StackPanel _textPanel;
    private DropShadow _cardShadow;
    private DropShadow _dropShadow;
    private SpriteVisual _cardShadowVisual;
    private FrameworkElement _shadowHost;
    private Visual _textPanelVisual;
    private Compositor _textPanelCompositor;
    private Visual visual;
    private Compositor compositor;

    private int _animationVersion;
    public HeaderCarouselItem()
    {
        this.DefaultStyleKey = typeof(HeaderCarouselItem);
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _autoScrollTitle = GetTemplateChild("AutoScrollHoverEffectViewTitle") as AutoScrollView;
        _autoScrollDescription = GetTemplateChild("AutoScrollHoverEffectViewDescription") as AutoScrollView;

        _launch = GetTemplateChild("Launch") as Button;
        _update = GetTemplateChild("Update") as Button;
        _stopProcesses = GetTemplateChild("StopProcesses") as Button;
        _launchExplorer = GetTemplateChild("LaunchExplorer") as Button;
        _settings = GetTemplateChild("Settings") as Button;

        _launch.Click += Launch_Click;
        _update.Click += Update_Click;
        _stopProcesses.Click += StopProcesses_Click;
        _launchExplorer.Click += LaunchExplorer_Click;
        _settings.Click += Settings_Click;

        UpdateButtonsVisibility();

        visual = ElementCompositionPreview.GetElementVisual(this);
        compositor = visual.Compositor;

        _textPanel = GetTemplateChild(PART_TextPanel) as StackPanel;
        _shadowHost = GetTemplateChild(PART_ShadowHost) as FrameworkElement;

        if (_textPanel is not null)
        {
            _textPanelVisual = ElementCompositionPreview.GetElementVisual(_textPanel);
            _textPanelCompositor = _textPanelVisual.Compositor;

            ElementCompositionPreview.SetIsTranslationEnabled(_textPanel, true);

            _textPanelVisual.Opacity = 0;
            _textPanelVisual.Properties.InsertVector3("Translation", new Vector3(0, 200, 0));

            _textPanel.Visibility = Visibility.Collapsed;
        }

        InitializeShadow();
        AttachCardShadow();

        Unloaded -= HeaderTile_Unloaded;
        Unloaded += HeaderTile_Unloaded;
    }
    private void SetAccessibleName()
    {
        if (!string.IsNullOrEmpty(Title))
        {
            AutomationProperties.SetName(this, Title);
        }
    }
    private void OnIsSelectedChanged()
    {
        if (IsSelected)
        {
            CheckGameRunning();
            Canvas.SetZIndex(this, 10);
            VisualStateManager.GoToState(this, "Selected", true);
            AnimateShowPanel();
            PlaySelectAnimation();
        }
        else
        {
            gameWatcherTimer?.Stop();
            VisualStateManager.GoToState(this, "NotSelected", true);
            AnimateHidePanel();
            PlayDeselectAnimation();
        }
    }
    private void InitializeShadow()
    {
        _dropShadow = compositor.CreateDropShadow();
        _dropShadow.Opacity = 0.2f;
        _dropShadow.BlurRadius = 12f;

        var shadowVisual = compositor.CreateSpriteVisual();
        shadowVisual.Shadow = _dropShadow;
        shadowVisual.Size = visual.Size;

        ElementCompositionPreview.SetElementChildVisual(this, shadowVisual);
    }
    private void HeaderTile_Unloaded(object sender, RoutedEventArgs e)
    {
        DetachCardShadow();
    }
    private void DetachCardShadow()
    {
        if (_shadowHost != null)
        {
            ElementCompositionPreview.SetElementChildVisual(_shadowHost, null);
            _shadowHost.SizeChanged -= OnShadowHostSizeChanged;
        }

        _cardShadowVisual = null;
        _cardShadow = null;
        _shadowHost = null;
    }

    private void AttachCardShadow()
    {
        if (_shadowHost == null)
            return;

        var hostVisual = ElementCompositionPreview.GetElementVisual(_shadowHost);
        var compositor = hostVisual.Compositor;

        _cardShadow = compositor.CreateDropShadow();
        _cardShadow.BlurRadius = 12f;
        _cardShadow.Opacity = 0.2f;
        _cardShadow.Color = Colors.Black;
        _cardShadow.Offset = new Vector3(0, 0, 0);

        _cardShadowVisual = compositor.CreateSpriteVisual();
        _cardShadowVisual.Shadow = _cardShadow;
        _cardShadowVisual.Size = new Vector2((float)_shadowHost.ActualWidth, (float)_shadowHost.ActualHeight);

        ElementCompositionPreview.SetElementChildVisual(_shadowHost, _cardShadowVisual);

        _shadowHost.SizeChanged += OnShadowHostSizeChanged;
    }
    private void OnShadowHostSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (_cardShadowVisual != null)
        {
            _cardShadowVisual.Size = new Vector2((float)e.NewSize.Width, (float)e.NewSize.Height);
        }
    }
    private void AnimateShowPanel()
    {
        if (_textPanel == null) return;

        _animationVersion++;
        int version = _animationVersion;

        _textPanel.Visibility = Visibility.Visible;
        _textPanel.Opacity = 1;
        ElementCompositionPreview.SetIsTranslationEnabled(_textPanel, true);

        var fadeIn = _textPanelCompositor.CreateScalarKeyFrameAnimation();
        fadeIn.InsertKeyFrame(1f, 1f);
        fadeIn.Duration = TimeSpan.FromMilliseconds(400);

        var slideIn = _textPanelCompositor.CreateVector3KeyFrameAnimation();
        slideIn.InsertKeyFrame(1f, Vector3.Zero);
        slideIn.Duration = TimeSpan.FromMilliseconds(600);

        var batch = _textPanelCompositor.CreateScopedBatch(CompositionBatchTypes.Animation);
        batch.Completed += (s, e) =>
        {
            if (version == _animationVersion)
            {
                _autoScrollTitle?.SetValue(AutoScrollView.IsPlayingProperty, true);
                _autoScrollDescription?.SetValue(AutoScrollView.IsPlayingProperty, true);
            }
        };

        _textPanelVisual.StartAnimation("Opacity", fadeIn);
        _textPanelVisual.Properties.StartAnimation("Translation", slideIn);

        batch.End();
    }
    private void AnimateHidePanel()
    {
        if (_textPanel == null) return;

        int version = _animationVersion;

        var fadeOut = _textPanelCompositor.CreateScalarKeyFrameAnimation();
        fadeOut.InsertKeyFrame(1f, 0f);
        fadeOut.Duration = TimeSpan.FromMilliseconds(350);

        var slideOut = _textPanelCompositor.CreateVector3KeyFrameAnimation();
        slideOut.InsertKeyFrame(1f, new Vector3(0, 200, 0));
        slideOut.Duration = TimeSpan.FromMilliseconds(600);

        var batch = _textPanelCompositor.CreateScopedBatch(CompositionBatchTypes.Animation);
        batch.Completed += (s, e) =>
        {
            if (version == _animationVersion && _textPanel?.DispatcherQueue?.HasThreadAccess == true)
            {
                _textPanel.Visibility = Visibility.Collapsed;
            }
        };

        _textPanelVisual.StartAnimation("Opacity", fadeOut);
        _textPanelVisual.Properties.StartAnimation("Translation", slideOut);

        batch.End();
    }
    private void PlaySelectAnimation()
    {
        // Animate scale to 1.0
        var scaleAnim = compositor.CreateVector3KeyFrameAnimation();
        scaleAnim.InsertKeyFrame(1f, new Vector3(1f, 1f, 1f));
        scaleAnim.Duration = TimeSpan.FromMilliseconds(600);
        visual.StartAnimation("Scale", scaleAnim);

        // Animate shadow opacity to 0.4
        var opacityAnim = compositor.CreateScalarKeyFrameAnimation();
        opacityAnim.InsertKeyFrame(1f, 0.4f);
        opacityAnim.Duration = TimeSpan.FromMilliseconds(600);
        _dropShadow.StartAnimation(nameof(_dropShadow.Opacity), opacityAnim);

        // Animate shadow blur radius to 24
        var blurAnim = compositor.CreateScalarKeyFrameAnimation();
        blurAnim.InsertKeyFrame(1f, 24f);
        blurAnim.Duration = TimeSpan.FromMilliseconds(600);
        _dropShadow.StartAnimation(nameof(_dropShadow.BlurRadius), blurAnim);
    }

    private void PlayDeselectAnimation()
    {
        // Scale animation to 0.8
        var scaleAnim = compositor.CreateVector3KeyFrameAnimation();
        scaleAnim.InsertKeyFrame(1f, new Vector3(0.8f, 0.8f, 1f));
        scaleAnim.Duration = TimeSpan.FromMilliseconds(350);

        // Shadow opacity animation to 0.2
        var opacityAnim = compositor.CreateScalarKeyFrameAnimation();
        opacityAnim.InsertKeyFrame(1f, 0.2f);
        opacityAnim.Duration = TimeSpan.FromMilliseconds(350);

        // Shadow blur radius animation to 12
        var blurAnim = compositor.CreateScalarKeyFrameAnimation();
        blurAnim.InsertKeyFrame(1f, 12f);
        blurAnim.Duration = TimeSpan.FromMilliseconds(350);

        var batch = compositor.CreateScopedBatch(CompositionBatchTypes.Animation);
        batch.Completed += (s, e) =>
        {
            Canvas.SetZIndex(this, 0);
            _autoScrollTitle?.SetValue(AutoScrollView.IsPlayingProperty, false);
            _autoScrollDescription?.SetValue(AutoScrollView.IsPlayingProperty, false);
        };

        // Start animations while batch is active
        visual.StartAnimation("Scale", scaleAnim);
        _dropShadow.StartAnimation(nameof(_dropShadow.Opacity), opacityAnim);
        _dropShadow.StartAnimation(nameof(_dropShadow.BlurRadius), blurAnim);

        batch.End();
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

    private async void Update_Click(object sender, RoutedEventArgs e)
    {
        if (Launcher == "Epic Games")
        {
            Process.Start(new ProcessStartInfo($"com.epicgames.launcher://apps/{CatalogNamespace}%3A{CatalogItemId}%3A{AppName}?action=update") { UseShellExecute = true });

            await Task.Delay(4000);

            Process.Start(new ProcessStartInfo($"com.epicgames.launcher://apps/{CatalogNamespace}%3A{CatalogItemId}%3A{AppName}?action=update") { UseShellExecute = true });
        }
    }

    [DllImport("kernel32.dll")]
    static extern bool SetProcessWorkingSetSize(IntPtr process, int min, int max);

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
            BackgroundImageUrl = BackgroundImageUrl,
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

    private DispatcherTimer gameWatcherTimer;
    private bool? previousGameState = null;
    private bool? previousExplorerState = null;
    private bool servicesState = false;

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
                    _launch.IsEnabled = !isRunning;

                    if (isRunning)
                    {
                        if (!servicesState && _stopProcesses.Visibility == Visibility.Collapsed)
                            _stopProcesses.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        if (!servicesState && _stopProcesses.Visibility == Visibility.Visible)
                            _stopProcesses.Visibility = Visibility.Collapsed;
                    }

                    previousGameState = isRunning;
                }

                if (!servicesState && previousExplorerState != (isRunning && !explorerRunning))
                {
                    _launchExplorer.Visibility = (isRunning && !explorerRunning) ? Visibility.Visible : Visibility.Collapsed;
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
}
