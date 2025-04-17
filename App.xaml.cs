using Microsoft.UI.Windowing;
using Microsoft.Win32;
using Microsoft.Windows.AppLifecycle;
using Windows.Graphics;

namespace AutoOS
{
    public partial class App : Application
    {
        public new static App Current => (App)Application.Current;
        public static Window MainWindow = Window.Current;
        public JsonNavigationService NavService { get; set; }
        public IThemeService ThemeService { get; set; }
        public static IThemeService Theme => (App.Current as App)?.ThemeService;
        internal static bool IsInstalled { get; private set; }

        public App()
        {
            //Registry.SetValue(@"HKEY_CURRENT_USER\SOFTWARE\AutoOS", "Stage", "Installed", RegistryValueKind.String);
            //Registry.CurrentUser.OpenSubKey(@"SOFTWARE\AutoOS", true)?.DeleteValue("Stage", false);

            IsInstalled = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\AutoOS")?.GetValue("Stage") as string == "Installed";
            InitializeComponent();
            NavService = new JsonNavigationService();
        }

        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            if (IsInstalled)
            {
                AppActivationArguments appActivationArguments = AppInstance.GetCurrent().GetActivatedEventArgs();

                if (appActivationArguments.Kind is ExtendedActivationKind.StartupTask)
                {
                    MainWindow = new StartupWindow();
                    MainWindow.AppWindow.SetIcon("Assets/AppIcon.ico");
                    MainWindow.Title = MainWindow.AppWindow.Title = "AutoOS Startup";

                    new ModernSystemMenu(MainWindow);

                    Window window = MainWindow;
                    var monitor = DisplayMonitorHelper.GetMonitorInfo(window);
                    int X = (int)monitor.RectMonitor.Width;
                    int Y = (int)monitor.RectMonitor.Height;

                    int windowWidth = 340;
                    int windowHeight = 130;

                    int posX = X - windowWidth - 10;
                    int posY = Y - windowHeight - 53;

                    MainWindow.AppWindow.MoveAndResize(new RectInt32(posX, posY, windowWidth, windowHeight));

                    MainWindow.Activate();
                }
                else
                {
                    if (!Directory.Exists(@"C:\Program Files\Windhawk"))
                    {
                        MainWindow = new StartupWindow();
                        MainWindow.AppWindow.SetIcon("Assets/AppIcon.ico");
                        MainWindow.Title = MainWindow.AppWindow.Title = "AutoOS Updater";

                        new ModernSystemMenu(MainWindow);

                        Window window = MainWindow;
                        var monitor = DisplayMonitorHelper.GetMonitorInfo(window);
                        int X = (int)monitor.RectMonitor.Width;
                        int Y = (int)monitor.RectMonitor.Height;

                        int windowWidth = 340;
                        int windowHeight = 130;

                        int posX = X - windowWidth - 10;
                        int posY = Y - windowHeight - 53;

                        MainWindow.AppWindow.MoveAndResize(new RectInt32(posX, posY, windowWidth, windowHeight));

                        MainWindow.Activate();
                    }
                    
                    MainWindow = new MainWindow();
                    MainWindow.AppWindow.SetIcon("Assets/AppIcon.ico");
                    MainWindow.Title = MainWindow.AppWindow.Title = "AutoOS Settings";

                    ThemeService = new ThemeService(MainWindow);
                    ThemeService.AutoInitialize(MainWindow).ConfigureTintColor();

                    new ModernSystemMenu(MainWindow);

                    WindowHelper.ResizeAndCenterWindowToPercentageOfWorkArea(MainWindow, 90);

                    if (MainWindow.AppWindow.Presenter is OverlappedPresenter presenter)
                    {
                        presenter.Maximize();
                        presenter.PreferredMinimumWidth = 660;
                        presenter.PreferredMinimumHeight = 715;
                    }

                    MainWindow.Activate();
                }
            }
            else
            {
                MainWindow = new MainWindow();
                MainWindow.AppWindow.SetIcon("Assets/AppIcon.ico");
                MainWindow.Title = MainWindow.AppWindow.Title = "AutoOS Installer";

                ThemeService = new ThemeService(MainWindow);
                ThemeService.AutoInitialize(MainWindow).ConfigureTintColor();
                ThemeService.SetBackdropType(BackdropType.Mica);

                if (MainWindow.AppWindow.Presenter is OverlappedPresenter presenter)
                {
                    presenter.Maximize();
                    presenter.PreferredMinimumWidth = 660;
                    presenter.PreferredMinimumHeight = 715;
                }

                MainWindow.Activate();
            }
        }
    }
}