using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.Win32;
using System.Diagnostics;
using System.Management;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Text.Json.Nodes;

namespace AutoOS.Views.Settings.Games;

public sealed partial class GamePanel : UserControl
{
    [DllImport("kernel32.dll")]
    static extern bool SetProcessWorkingSetSize(IntPtr process, int min, int max);

    private static readonly HttpClient httpClient = new HttpClient();

    public string Title
    {
        get { return (string)GetValue(TitleProperty); }
        set { SetValue(TitleProperty, value); }
    }

    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register("Title", typeof(string), typeof(HeaderTile), new PropertyMetadata(null));

    public string Description
    {
        get { return (string)GetValue(DescriptionProperty); }
        set { SetValue(DescriptionProperty, value); }
    }

    public static readonly DependencyProperty DescriptionProperty =
        DependencyProperty.Register("Description", typeof(string), typeof(HeaderTile), new PropertyMetadata(null));

    public static readonly DependencyProperty SourceProperty =
        DependencyProperty.Register("Source", typeof(object), typeof(HeaderTile), new PropertyMetadata(null));

    public string Link
    {
        get { return (string)GetValue(LinkProperty); }
        set { SetValue(LinkProperty, value); }
    }

    public static readonly DependencyProperty LinkProperty =
        DependencyProperty.Register("Link", typeof(string), typeof(HeaderTile), new PropertyMetadata(null));

    private void AutoScrollHoverEffectView_PointerCanceled(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        AutoScrollHoverEffectViewTitle.IsPlaying = false;
        AutoScrollHoverEffectViewDescription.IsPlaying = false;
    }

    private void AutoScrollHoverEffectView_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        AutoScrollHoverEffectViewTitle.IsPlaying = true;
        AutoScrollHoverEffectViewDescription.IsPlaying = true;
    }

    private void AutoScrollHoverEffectView_PointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        AutoScrollHoverEffectViewTitle.IsPlaying = false;
        AutoScrollHoverEffectViewDescription.IsPlaying = false;
    }

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

    private DispatcherTimer gameWatcherTimer;
    private bool? previousGameState = null;
    private bool? previousExplorerState = null;
    private bool servicesState = false;
    public ImageSource ImageSource { get; set; }

    public GamePanel()
    {
        this.InitializeComponent();
        this.Unloaded += GamePanel_Unloaded;
    }

    private void GamePanel_Unloaded(object sender, RoutedEventArgs e)
    {
        // stop watchers when navigation to another page
        gameWatcherTimer?.Stop();
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
                        Debug.WriteLine("is running");
                        Panel.Click -= Launch_Click;

                        if (!servicesState && stopProcesses.Visibility == Visibility.Collapsed)
                            stopProcesses.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        Panel.Click -= Launch_Click;
                        Panel.Click += Launch_Click;

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
                Content = "Are you sure that you want to launch " + Title + " in service enabled state?",
                PrimaryButtonText = "Yes",
                SecondaryButtonText = "No",
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
        // start windhawk service
        using (ServiceController service = new ServiceController("Windhawk"))
        {
            if (service.Status == ServiceControllerStatus.Stopped)
            {
                service.Start();
            }
        }

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
    }

    private async void Settings_Click(object sender, RoutedEventArgs e)
    {
        BitmapImage imageSource = null;

        // epic games
        if (Launcher == "Epic Games")
        {
            // get json data
            string url = $"https://api.egdata.app/items/{CatalogItemId}";

            // more up to date than the fortnite item id
            if (Title == "Fortnite")
            {
                url = $"https://api.egdata.app/offers/d69e49517f0f4e49a39253f7b106dc27";
            }

            try
            {
                // read json data
                var remoteData = JsonNode.Parse(await httpClient.GetStringAsync(url));

                // search cover image
                foreach (var image in remoteData?["keyImages"]?.AsArray())
                {
                    if (image?["type"]?.GetValue<string>() == "DieselGameBox")
                    {
                        imageSource = new BitmapImage(new Uri(image["url"]?.GetValue<string>()));
                        break;
                    }
                }
            }
            catch
            {

            }
        }
        else if (Launcher == "Steam")
        {
            imageSource = new BitmapImage(new Uri(Path.Combine(@"C:\Program Files (x86)\Steam", $@"appcache\librarycache\{GameID}\library_hero.jpg")));
        }
        else if (Launcher == "Ryujinx")
        {
            
        }

        var gameSettings = new GameSettings
        {
            Title = Title,
            ImageSource = imageSource,
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
        contentDialog.Resources["ContentDialogMaxHeight"] = 850;

        ContentDialogResult result = await contentDialog.ShowAsync();
    }
}