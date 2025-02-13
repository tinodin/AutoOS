namespace AutoOS;
public partial class NavigationPageMappingsInstaller
{
    public static Dictionary<string, Type> PageDictionary { get; } = new Dictionary<string, Type>
    {
        {"AutoOS.Views.Installer.HomeLandingPage", typeof(AutoOS.Views.Installer.HomeLandingPage)},
        {"AutoOS.Views.Installer.PersonalizationPage", typeof(AutoOS.Views.Installer.PersonalizationPage)},
        {"AutoOS.Views.Installer.BrowserPage", typeof(AutoOS.Views.Installer.BrowserPage)},
        {"AutoOS.Views.Installer.ApplicationsPage", typeof(AutoOS.Views.Installer.ApplicationsPage)},
        {"AutoOS.Views.Installer.DisplayPage", typeof(AutoOS.Views.Installer.DisplayPage)},
        {"AutoOS.Views.Installer.GraphicsPage", typeof(AutoOS.Views.Installer.GraphicsPage)},
        {"AutoOS.Views.Installer.SchedulingPage", typeof(AutoOS.Views.Installer.SchedulingPage)},
        {"AutoOS.Views.Installer.TimerPage", typeof(AutoOS.Views.Installer.TimerPage)},
        {"AutoOS.Views.Installer.DevicesPage", typeof(AutoOS.Views.Installer.DevicesPage)},
        {"AutoOS.Views.Installer.InternetPage", typeof(AutoOS.Views.Installer.InternetPage)},
        {"AutoOS.Views.Installer.PowerPage", typeof(AutoOS.Views.Installer.PowerPage)},
        {"AutoOS.Views.Installer.ServicesPage", typeof(AutoOS.Views.Installer.ServicesPage)},
        {"AutoOS.Views.Installer.SecurityPage", typeof(AutoOS.Views.Installer.SecurityPage)},
        {"AutoOS.Views.Installer.GamesPage", typeof(AutoOS.Views.Installer.GamesPage)},
        {"AutoOS.Views.Installer.InstallPage", typeof(AutoOS.Views.Installer.InstallPage)},
    };
}
