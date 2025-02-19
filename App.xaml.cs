using Microsoft.UI.Windowing;
using Microsoft.Win32;
using System.Diagnostics;

namespace AutoOS
{
    public partial class App : Application
    {
        public new static App Current => (App)Application.Current;
        public static Window MainWindow = Window.Current;
        public JsonNavigationService NavService { get; set; }
        public IThemeService ThemeService { get; set; }

        public App()
        {
            this.InitializeComponent();
            NavService = new JsonNavigationService();
        }

        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            MainWindow = new MainWindow();

            string stage = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\AutoOS")?.GetValue("Stage") as string;

            if (stage != null)
            {
                if (int.TryParse(stage, out int stageValue) && stageValue >= 1)
                {
                    MainWindow.Title = MainWindow.AppWindow.Title = "AutoOS Installer";
                }
                else if (stage.Equals("Installed", StringComparison.OrdinalIgnoreCase))
                {
                    MainWindow.Title = MainWindow.AppWindow.Title = "AutoOS Settings";
                }
            }
            else
            {
                MainWindow.Title = MainWindow.AppWindow.Title = "AutoOS Installer";
            }
            MainWindow.AppWindow.SetIcon("Assets/AppIcon.ico");

            ThemeService = new ThemeService(MainWindow);

            if (MainWindow.AppWindow.Presenter is OverlappedPresenter presenter)
            {
                presenter.Maximize();
            }

            MainWindow.Activate();

            
            Debug.WriteLine(ThemeService.IsDarkTheme());
        }
    }
}
