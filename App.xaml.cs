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
        internal static bool IsInstalled { get; private set; }
        internal static double Scaling { get; set; }

        public App()
        {
            Registry.SetValue(@"HKEY_CURRENT_USER\SOFTWARE\AutoOS", "Stage", "Installed", RegistryValueKind.String);
            //Registry.CurrentUser.OpenSubKey(@"SOFTWARE\AutoOS", true)?.DeleteValue("Stage", false);

            IsInstalled = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\AutoOS")?.GetValue("Stage") as string == "Installed";

            InitializeComponent();
            NavService = new JsonNavigationService();

            // Enables Multicore JIT with the specified profile
            System.Runtime.ProfileOptimization.SetProfileRoot(Constants.RootDirectoryPath);
            System.Runtime.ProfileOptimization.StartProfile("Startup.Profile");
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

                    int windowWidth = (int)(340 * Scaling);
                    int windowHeight = (int)(130 * Scaling);

                    int posX = X - windowWidth - (int)(10 * Scaling);
                    int posY = Y - windowHeight - (int)(53 * Scaling);

                    MainWindow.AppWindow.MoveAndResize(new RectInt32(posX, posY, windowWidth, windowHeight));

                    MainWindow.Activate();
                }
                else
                {
                    if (!Directory.Exists(@"C:\Program Files\Windhawk"))
                    {
                        MainWindow = new StartupWindow();
                        MainWindow.AppWindow.SetIcon("Assets/AppIcon.ico");
                        MainWindow.Title = MainWindow.AppWindow.Title = "AutoOS Startup";

                        new ModernSystemMenu(MainWindow);

                        Window window = MainWindow;
                        var monitor = DisplayMonitorHelper.GetMonitorInfo(window);
                        int X = (int)monitor.RectMonitor.Width;
                        int Y = (int)monitor.RectMonitor.Height;

                        int windowWidth = (int)(450 * Scaling);
                        int windowHeight = (int)(130 * Scaling);

                        int posX = X - windowWidth - (int)(10 * Scaling);
                        int posY = Y - windowHeight - (int)(53 * Scaling);

                        MainWindow.AppWindow.MoveAndResize(new RectInt32(posX, posY, windowWidth, windowHeight));

                        MainWindow.Activate();
                    }

                    MainWindow = new MainWindow();
                    MainWindow.AppWindow.SetIcon("Assets/AppIcon.ico");
                    MainWindow.Title = MainWindow.AppWindow.Title = "AutoOS Settings";

                    ThemeService = new ThemeService(MainWindow);
                    ThemeService.AutoInitialize(MainWindow).ConfigureTintColor();

                    new ModernSystemMenu(MainWindow);

                    WindowHelper.ResizeAndCenterWindowToPercentageOfWorkArea(MainWindow, 92);

                    if (MainWindow.AppWindow.Presenter is OverlappedPresenter presenter)
                    {
                        //presenter.Maximize();
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