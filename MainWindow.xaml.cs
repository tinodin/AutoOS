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
                AppTitleBar.Title = "AutoOS Settings";

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
                AppTitleBar.Title = "AutoOS Installer";

                ((OverlappedPresenter)AppWindow.Presenter).Maximize();
            }
        }

        private void RootGrid_Loaded(object sender, RoutedEventArgs e)
        {
            if (!App.IsInstalled)
            {
                foreach (var item in NavView.FooterMenuItems.OfType<NavigationViewItem>())
                {
                    item.IsEnabled = false;
                }
            }
        }

        private readonly HashSet<string> _visitedPages = [];
        public IReadOnlyCollection<string> VisitedPages => _visitedPages;

        public readonly string[] AllPages =
        [
            "PersonalizationPage",
            "ApplicationsPage",
            "BrowserPage",
            "DisplayPage",
            "GraphicsPage",
            "SchedulingPage",
            "DevicesPage",
            "InternetPage",
            "PowerPage",
            "ServicesPage",
            "SecurityPage"
        ];

        public void MarkVisited(string pageName)
        {
            _visitedPages.Add(pageName);
        }

        public bool AllPagesVisited()
        {
            return AllPages.All(p => _visitedPages.Contains(p));
        }

        public void CheckAllPagesVisited()
        {
            if (AllPagesVisited())
            {
                var navView = GetNavView();
                foreach (var item in navView.FooterMenuItems.OfType<NavigationViewItem>())
                {
                    item.IsEnabled = true;
                }
            }
        }

        public NavigationView GetNavView()
        {
            return NavView;
        }
    }
}