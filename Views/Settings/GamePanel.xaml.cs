using Microsoft.UI.Xaml.Media;
using Microsoft.Win32;
using System.Diagnostics;
using System.Management;
using System.ServiceProcess;

namespace AutoOS.Views.Settings;

public sealed partial class GamePanel : UserControl
{
    private readonly string nsudoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "NSudo", "NSudoLC.exe");

    public event EventHandler<RoutedEventArgs> OnItemClick;

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
        AutoScrollHoverEffectView.IsPlaying = false;
    }

    private void AutoScrollHoverEffectView_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        AutoScrollHoverEffectView.IsPlaying = true;
    }

    private void AutoScrollHoverEffectView_PointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        AutoScrollHoverEffectView.IsPlaying = false;
    }

    public ImageSource ImageSource { get; set; }

    public GamePanel()
    {
        this.InitializeComponent();
    }

    public string CatalogNamespace { get; set; }
    public string CatalogItemId { get; set; }
    public string AppName { get; set; }
    public string InstallLocation { get; set; }
    public string LaunchExecutable { get; set; }

    public void CheckGameRunning()
    {
        string offlineExecutable = string.Empty;
        string onlineExecutable = string.Empty;

        offlineExecutable = Path.GetFileNameWithoutExtension(LaunchExecutable);

        if (Title == "Fortnite")
        {
            onlineExecutable = "FortniteClient-Win64-Shipping";
        }

        // if running
        if (Process.GetProcessesByName(Path.GetFileNameWithoutExtension(offlineExecutable)).Length > 0 || (!string.IsNullOrEmpty(onlineExecutable) && Process.GetProcessesByName(Path.GetFileNameWithoutExtension(onlineExecutable)).Length > 0))
        {
            // disable launch button
            Launch.IsEnabled = false;
            Panel.Click -= Launch_Click;

            if (new ServiceController("Beep").Status == ServiceControllerStatus.Stopped)
            {
                // show stop processes button if not already
                if (stopProcesses.Visibility == Visibility.Collapsed)
                {
                    // show stop processes button
                    stopProcesses.Visibility = Visibility.Visible;
                }

                // show launch explorer button if not already
                if (Process.GetProcessesByName("explorer").Length == 0)
                {
                    if (launchExplorer.Visibility == Visibility.Collapsed)
                    {
                        launchExplorer.Visibility = Visibility.Visible;
                    }
                }
            }
        }

        var synchronizationContext = SynchronizationContext.Current;

        // watcher
        var watcher = new Thread(() =>
        {
            while (true)
            {
                synchronizationContext.Post(_ =>
                {
                    if (Process.GetProcessesByName(Path.GetFileNameWithoutExtension(offlineExecutable)).Length > 0 || (!string.IsNullOrEmpty(onlineExecutable) && Process.GetProcessesByName(Path.GetFileNameWithoutExtension(onlineExecutable)).Length > 0))
                    {
                        // disable launch button
                        Launch.IsEnabled = false;
                        Panel.Click -= Launch_Click;

                        if (new ServiceController("Beep").Status == ServiceControllerStatus.Stopped)
                        {
                            // show stop processes button if not already
                            if (stopProcesses.Visibility == Visibility.Collapsed)
                            {
                                stopProcesses.Visibility = Visibility.Visible;
                            }

                            // show launch explorer button if not already
                            if (Process.GetProcessesByName("explorer").Length == 0)
                            {
                                if (launchExplorer.Visibility == Visibility.Collapsed)
                                {
                                    launchExplorer.Visibility = Visibility.Visible;
                                }
                            }
                            else
                            {
                                // hide launch explorer button if explorer is running
                                if (launchExplorer.Visibility == Visibility.Visible)
                                {
                                    launchExplorer.Visibility = Visibility.Collapsed;
                                }
                            }
                        }
                    }
                    else
                    {
                        if (Launch.IsEnabled == false)
                        {
                            // enable launch button
                            Launch.IsEnabled = true;
                            Panel.Click += Launch_Click;
                        }
                        if (new ServiceController("Beep").Status == ServiceControllerStatus.Stopped)
                        {
                            // hide stop processes button
                            if (stopProcesses.Visibility == Visibility.Visible)
                            {
                                stopProcesses.Visibility = Visibility.Collapsed;
                            }

                            // hide launch explorer
                            if (launchExplorer.Visibility == Visibility.Visible)
                            {
                                launchExplorer.Visibility = Visibility.Collapsed;
                            }
                        }
                    }
                }, null);

                Thread.Sleep(500);
            }
        });

        watcher.IsBackground = true;
        watcher.Start();
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

        Process.Start(new ProcessStartInfo($"com.epicgames.launcher://apps/{CatalogNamespace}%3A{CatalogItemId}%3A{AppName}?action=launch&silent=true") { UseShellExecute = true });
    }

    private async void StopProcesses_Click(object sender, RoutedEventArgs e)
    {
        // rename start menu binaries
        await Process.Start(new ProcessStartInfo { FileName = nsudoPath, Arguments = $@"-U:T -P:E -Wait -ShowWindowMode:Hide ren ""C:\Windows\System32\ctfmon.exe"" ctfmon.exee & ren ""C:\Windows\System32\RuntimeBroker.exe"" RuntimeBroker.exee & ren ""C:\Windows\SystemApps\ShellExperienceHost_cw5n1h2txyewy\ShellExperienceHost.exe"" ShellExperienceHost.exee & ren ""C:\Windows\SystemApps\MicrosoftWindows.Client.CBS_cw5n1h2txyewy\SearchHost.exe"" SearchHost.exee & ren ""C:\Windows\SystemApps\Microsoft.Windows.StartMenuExperienceHost_cw5n1h2txyewy\StartMenuExperienceHost.exe"" StartMenuExperienceHost.exee", CreateNoWindow = true }).WaitForExitAsync();

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
            "FortniteClient-Win64-Shipping_EAC_EOS",
            "rundll32",
            "RuntimeBroker",
            "SearchHost",
            "secd",
            "ShellExperienceHost",
            "sppsvc",
            "StartMenuExperienceHost",
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
            "camsvc",
            "CryptSvc",
            "gpsvc",
            "netprofm",
            "nsi",
            "ProfSvc",
            "StateRepository",
            "TextInputManagementService",
            "TrustedInstaller",
            "UserManager"
        };

        foreach (var serviceName in serviceNames)
        {
            try
            {
                var searcher = new ManagementObjectSearcher($"SELECT ProcessId FROM Win32_Service WHERE Name = '{serviceName}'");
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

        try
        {
            var searcher = new ManagementObjectSearcher("SELECT Name, ProcessId FROM Win32_Service WHERE Name LIKE 'UdkUserSvc%'");
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

        try { new ServiceController("Winmgmt").Stop(); } catch { }
    }

    private async void LaunchExplorer_Click(object sender, RoutedEventArgs e)
    {
        // rename start menu binaries
        await Process.Start(new ProcessStartInfo { FileName = nsudoPath, Arguments = $@"-U:T -P:E -Wait -ShowWindowMode:Hide ren ""C:\Windows\System32\ctfmon.exee"" ctfmon.exe & ren ""C:\Windows\System32\RuntimeBroker.exee"" RuntimeBroker.exe & ren ""C:\Windows\SystemApps\ShellExperienceHost_cw5n1h2txyewy\ShellExperienceHost.exee"" ShellExperienceHost.exe & ren ""C:\Windows\SystemApps\MicrosoftWindows.Client.CBS_cw5n1h2txyewy\SearchHost.exee"" SearchHost.exe & ren ""C:\Windows\SystemApps\Microsoft.Windows.StartMenuExperienceHost_cw5n1h2txyewy\StartMenuExperienceHost.exee"" StartMenuExperienceHost.exe", CreateNoWindow = true }).WaitForExitAsync();

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
        var gameSettings = new GameSettings
        {
            Title = Title,
            InstallLocation = InstallLocation
        };

        var contentDialog = new ContentDialog
        {
            Title = Title,
            Content = gameSettings,
            PrimaryButtonText = "Close",
            XamlRoot = this.XamlRoot,
        };

        contentDialog.Resources["ContentDialogMinWidth"] = 850;

        ContentDialogResult result = await contentDialog.ShowAsync();
    }
}