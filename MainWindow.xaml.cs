using Microsoft.UI.Windowing;

namespace AutoOS.Views
{
    public sealed partial class MainWindow : Window
    {
        public string TitleBarName { get; set; }
        internal static MainWindow Instance { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            Instance = this;
            ExtendsContentIntoTitleBar = true;
            AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
            SetTitleBar(AppTitleBar);

            if (App.IsInstalled)
            {
                NavView.IsSettingsVisible = true;
                App.Current.NavService
                    .Initialize(NavView, NavFrame, NavigationPageMappingsSettings.PageDictionary)
                    .ConfigureDefaultPage(typeof(Settings.HomeLandingPage))
                    .ConfigureSettingsPage(typeof(SettingsPage))
                    .ConfigureJsonFile("Assets/NavViewMenu/Settings.json")
                    .ConfigureTitleBar(AppTitleBar, false)
                    .ConfigureBreadcrumbBar(BreadCrumbNav, BreadcrumbPageMappings.PageDictionary);
                TitleBarName = "AutoOS Settings";
            }
            else
            {
                App.Current.NavService
                    .Initialize(NavView, NavFrame, NavigationPageMappingsInstaller.PageDictionary)
                    .ConfigureJsonFile("Assets/NavViewMenu/Installer.json")
                    .ConfigureDefaultPage(typeof(Installer.HomeLandingPage))
                    .ConfigureTitleBar(AppTitleBar, false)
                    .ConfigureBreadcrumbBar(BreadCrumbNav, BreadcrumbPageMappings.PageDictionary);
                TitleBarName = "AutoOS Installer";
            }
        }

        public NavigationView GetNavView()
        {
            return NavView;
        }
    }
}