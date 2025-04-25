using Microsoft.UI.Windowing;

namespace AutoOS.Views
{
    public sealed partial class MainWindow : Window
    {
        public string TitleBarName { get; set; }
        internal static MainWindow Instance { get; set; }

        public MainWindow()
        {
            Instance = this;
            InitializeComponent();
            ExtendsContentIntoTitleBar = true;
            SetTitleBar(AppTitleBar);
            AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
            new ModernSystemMenu(this);

            ((OverlappedPresenter)AppWindow.Presenter).PreferredMinimumWidth = 660;
            ((OverlappedPresenter)AppWindow.Presenter).PreferredMinimumHeight = 715;

            if (App.IsInstalled)
            {
                App.Current.NavService
                    .Initialize(NavView, NavFrame, NavigationPageMappingsSettings.PageDictionary)
                    .ConfigureDefaultPage(typeof(Settings.HomeLandingPage))
                    .ConfigureSettingsPage(typeof(SettingsPage))
                    .ConfigureJsonFile("Assets/NavViewMenu/Settings.json")
                    .ConfigureTitleBar(AppTitleBar, false)
                    .ConfigureBreadcrumbBar(BreadCrumbNav, BreadcrumbPageMappings.PageDictionary);
                TitleBarName = "AutoOS Settings";

                NavView.IsSettingsVisible = true;
            }
            else
            {
                App.Current.NavService
                    .Initialize(NavView, NavFrame, NavigationPageMappingsInstaller.PageDictionary)
                    .ConfigureDefaultPage(typeof(Installer.HomeLandingPage))
                    .ConfigureJsonFile("Assets/NavViewMenu/Installer.json")
                    .ConfigureTitleBar(AppTitleBar, false)
                    .ConfigureBreadcrumbBar(BreadCrumbNav, BreadcrumbPageMappings.PageDictionary);
                TitleBarName = "AutoOS Installer";

                ((OverlappedPresenter)AppWindow.Presenter).Maximize();
            }
        }

        public NavigationView GetNavView()
        {
            return NavView;
        }
    }
}