using Microsoft.UI.Windowing;
using Microsoft.Win32;

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
            AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
            SetTitleBar(AppTitleBar);

            object stageObj = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\AutoOS")?.GetValue("Stage");

            if (stageObj is int stageValue && stageValue >= 1)
            {
                App.Current.NavService
                    .Initialize(NavView, NavFrame, NavigationPageMappingsInstaller.PageDictionary)
                    .ConfigureJsonFile("Assets/NavViewMenu/Installer.json")
                    .ConfigureDefaultPage(typeof(Installer.HomeLandingPage))
                    .ConfigureTitleBar(AppTitleBar, false)
                    .ConfigureBreadcrumbBar(BreadCrumbNav, BreadcrumbPageMappings.PageDictionary);
                TitleBarName = "AutoOS Installer";
            }
            else if (stageObj is string stageStr && stageStr.Equals("Installed", StringComparison.OrdinalIgnoreCase))
            {
                App.Current.NavService
                    .Initialize(NavView, NavFrame, NavigationPageMappingsSettings.PageDictionary)
                    .ConfigureJsonFile("Assets/NavViewMenu/Settings.json")
                    .ConfigureDefaultPage(typeof(Settings.HomeLandingPage))
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