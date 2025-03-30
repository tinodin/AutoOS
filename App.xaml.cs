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

        public App()
        {
            this.InitializeComponent();
            NavService = new JsonNavigationService();
        }

        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            Registry.SetValue(@"HKEY_CURRENT_USER\SOFTWARE\AutoOS", "Stage", "Installed", RegistryValueKind.String);
            //Registry.SetValue(@"HKEY_CURRENT_USER\SOFTWARE\AutoOS", "Stage", 2, RegistryValueKind.DWord);

            string stage = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\AutoOS")?.GetValue("Stage") as string;

            if (stage != null)
            {
                if (int.TryParse(stage, out int stageValue) && stageValue >= 1)
                {
                    MainWindow = new MainWindow();
                    MainWindow.AppWindow.SetIcon("Assets/AppIcon.ico");
                    MainWindow.Title = MainWindow.AppWindow.Title = "AutoOS Installer";

                    ThemeService = new ThemeService(MainWindow);

                    new ModernSystemMenu(MainWindow);

                    if (MainWindow.AppWindow.Presenter is OverlappedPresenter presenter)
                    {
                        presenter.Maximize();
                    }

                    MainWindow.Activate();
                }
                else if (stage.Equals("Installed", StringComparison.OrdinalIgnoreCase))
                {
                    AppActivationArguments appActivationArguments = Microsoft.Windows.AppLifecycle.AppInstance.GetCurrent().GetActivatedEventArgs();

                    if (appActivationArguments.Kind is ExtendedActivationKind.StartupTask)
                    {
                        MainWindow = new StartupWindow();
                        MainWindow.AppWindow.SetIcon("Assets/AppIcon.ico");
                        MainWindow.Title = MainWindow.AppWindow.Title = "AutoOS Startup";

                        ThemeService = new ThemeService(MainWindow);

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
                        MainWindow = new MainWindow();
                        MainWindow.AppWindow.SetIcon("Assets/AppIcon.ico");
                        MainWindow.Title = MainWindow.AppWindow.Title = "AutoOS Settings";

                        ThemeService = new ThemeService(MainWindow);

                        new ModernSystemMenu(MainWindow);

                        if (MainWindow.AppWindow.Presenter is OverlappedPresenter presenter)
                        {
                            presenter.Maximize();
                        }

                        MainWindow.Activate();


                        //MainWindow = new StartupWindow();
                        //MainWindow.AppWindow.SetIcon("Assets/AppIcon.ico");
                        //MainWindow.Title = MainWindow.AppWindow.Title = "AutoOS Startup";

                        //ThemeService = new ThemeService(MainWindow);

                        //new ModernSystemMenu(MainWindow);

                        //Window window = MainWindow;
                        //var monitor = DisplayMonitorHelper.GetMonitorInfo(window);
                        //int X = (int)monitor.RectMonitor.Width;
                        //int Y = (int)monitor.RectMonitor.Height;

                        //int windowWidth = 340;
                        //int windowHeight = 130;

                        //int posX = X - windowWidth - 10;
                        //int posY = Y - windowHeight - 53;

                        //MainWindow.AppWindow.MoveAndResize(new RectInt32(posX, posY, windowWidth, windowHeight));

                        //MainWindow.Activate();
                    }
                }
            }
            else
            {
                MainWindow = new MainWindow();
                MainWindow.AppWindow.SetIcon("Assets/AppIcon.ico");
                MainWindow.Title = MainWindow.AppWindow.Title = "AutoOS Installer";

                ThemeService = new ThemeService(MainWindow);
                ThemeService.SetBackdropType(BackdropType.Mica);

                if (MainWindow.AppWindow.Presenter is OverlappedPresenter presenter)
                {
                    presenter.Maximize();
                }

                MainWindow.Activate();
            }
        }
    }
}