namespace AutoOS.Assets.NavViewMenu;

public partial class NavigationPageMappingsSettings
{
	public static Dictionary<string, Type> PageDictionary { get; } = new Dictionary<string, Type>
	{
		{"AutoOS.Views.Settings.HomeLandingPage", typeof(AutoOS.Views.Settings.HomeLandingPage)},
		{"AutoOS.Views.Settings.SoundPage", typeof(AutoOS.Views.Settings.SoundPage)},
		{"AutoOS.Views.Settings.DisplayPage", typeof(AutoOS.Views.Settings.DisplayPage)},
		{"AutoOS.Views.Settings.GraphicsPage", typeof(AutoOS.Views.Settings.GraphicsPage)},
		{"AutoOS.Views.Settings.SchedulingPage", typeof(AutoOS.Views.Settings.SchedulingPage)},
		{"AutoOS.Views.Settings.DevicesPage", typeof(AutoOS.Views.Settings.DevicesPage)},
		{"AutoOS.Views.Settings.InternetPage", typeof(AutoOS.Views.Settings.InternetPage)},
		{"AutoOS.Views.Settings.PowerPage", typeof(AutoOS.Views.Settings.PowerPage)},
		{"AutoOS.Views.Settings.ServicesPage", typeof(AutoOS.Views.Settings.ServicesPage)},
		{"AutoOS.Views.Settings.BiosSettingPage", typeof(AutoOS.Views.Settings.BiosSettingPage)},
		{"AutoOS.Views.Settings.DiskCleanupPage", typeof(AutoOS.Views.Settings.DiskCleanupPage)},
		{"AutoOS.Views.Settings.SecurityPage", typeof(AutoOS.Views.Settings.SecurityPage)},
		{"AutoOS.Views.Settings.UpdatePage", typeof(AutoOS.Views.Settings.UpdatePage)},
		{"AutoOS.Views.Settings.BrowsersPage", typeof(AutoOS.Views.Settings.BrowsersPage)},
		{"AutoOS.Views.Settings.ApplicationsPage", typeof(AutoOS.Views.Settings.ApplicationsPage)},
		{"AutoOS.Views.Settings.BenchmarksPage", typeof(AutoOS.Views.Settings.BenchmarksPage)},
		{"AutoOS.Views.Settings.GamesPage", typeof(AutoOS.Views.Settings.GamesPage)}
	};
}
