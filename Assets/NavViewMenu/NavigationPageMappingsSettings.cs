namespace AutoOS;
public partial class NavigationPageMappingsSettings
{
    public static Dictionary<string, Type> PageDictionary { get; } = new Dictionary<string, Type>
    {
        {"AutoOS.Views.Settings.HomeLandingPage", typeof(AutoOS.Views.Settings.HomeLandingPage)},
        {"AutoOS.Views.Settings.DisplayPage", typeof(AutoOS.Views.Settings.DisplayPage)},
        {"AutoOS.Views.Settings.GraphicsPage", typeof(AutoOS.Views.Settings.GraphicsPage)},
        {"AutoOS.Views.Settings.SchedulingPage", typeof(AutoOS.Views.Settings.SchedulingPage)},
        {"AutoOS.Views.Settings.TimerPage", typeof(AutoOS.Views.Settings.TimerPage)},
        {"AutoOS.Views.Settings.DevicesPage", typeof(AutoOS.Views.Settings.DevicesPage)},
        {"AutoOS.Views.Settings.InternetPage", typeof(AutoOS.Views.Settings.InternetPage)},
        {"AutoOS.Views.Settings.PowerPage", typeof(AutoOS.Views.Settings.PowerPage)},
        {"AutoOS.Views.Settings.ServicesPage", typeof(AutoOS.Views.Settings.ServicesPage)},
        {"AutoOS.Views.Settings.LoggingPage", typeof(AutoOS.Views.Settings.LoggingPage)},
        {"AutoOS.Views.Settings.SecurityPage", typeof(AutoOS.Views.Settings.SecurityPage)},
        {"AutoOS.Views.Settings.UpdatePage", typeof(AutoOS.Views.Settings.UpdatePage)},
        {"AutoOS.Views.Settings.GamesPage", typeof(AutoOS.Views.Settings.GamesPage)},
    };
}
