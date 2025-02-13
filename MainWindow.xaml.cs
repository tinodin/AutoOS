using Microsoft.Win32;
using System.Diagnostics;

namespace AutoOS.Views
{
    public sealed partial class MainWindow : Window
    {
        public string TitleBarName { get; set; }
        internal static MainWindow Instance { get; set; }
        public MainWindow()
        {
            Instance = this;
            this.InitializeComponent();
            ExtendsContentIntoTitleBar = true;
            SetTitleBar(AppTitleBar);

            Registry.SetValue(@"HKEY_CURRENT_USER\SOFTWARE\AutoOS", "Stage", 2, RegistryValueKind.DWord);
            //Registry.SetValue(@"HKEY_CURRENT_USER\SOFTWARE\AutoOS", "Stage", "Installed", RegistryValueKind.String);
            object stageObj = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\AutoOS")?.GetValue("Stage");

            //App.Current.NavService
            //    .ConfigureTitleBar(AppTitleBar, false)
            //    .ConfigureBreadcrumbBar(BreadCrumbNav, BreadcrumbPageMappings.PageDictionary);

            //Registry.SetValue(@"HKEY_CURRENT_USER\SOFTWARE\AutoOS", "Stage", 1, RegistryValueKind.DWord);

            //Registry.SetValue(@"HKEY_CURRENT_USER\SOFTWARE\AutoOS", "Stage", 2, RegistryValueKind.DWord);



            if (stageObj is int stageValue && stageValue >= 1)
            {
                Debug.WriteLine("Stage >= 1");
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
                Debug.WriteLine("Installed");
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
                Debug.WriteLine("doesnt exist");
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