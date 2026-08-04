namespace AutoOS.App.Assets.NavViewMenu;

public partial class NavigationPageMappingsSettings
{
	public static Dictionary<string, Type> PageDictionary { get; } = new Dictionary<string, Type>
	{
		{"AutoOS.App.Views.Settings.HomePage", typeof(AutoOS.App.Views.Settings.HomePage)},
		{"AutoOS.App.Views.Settings.SoundPage", typeof(AutoOS.App.Views.Settings.SoundPage)},
		{"AutoOS.App.Views.Settings.DisplaysPage", typeof(AutoOS.App.Views.Settings.DisplaysPage)},
		{"AutoOS.App.Views.Settings.GraphicsPage", typeof(AutoOS.App.Views.Settings.GraphicsPage)},
		{"AutoOS.App.Views.Settings.SchedulingPage", typeof(AutoOS.App.Views.Settings.SchedulingPage)},
		{"AutoOS.App.Views.Settings.DevicesPage", typeof(AutoOS.App.Views.Settings.DevicesPage)},
		{"AutoOS.App.Views.Settings.InternetPage", typeof(AutoOS.App.Views.Settings.InternetPage)},
		{"AutoOS.App.Views.Settings.PowerPage", typeof(AutoOS.App.Views.Settings.PowerPage)},
		{"AutoOS.App.Views.Settings.ServicesPage", typeof(AutoOS.App.Views.Settings.ServicesPage)},
		{"AutoOS.App.Views.Settings.BiosSettingsPage", typeof(AutoOS.App.Views.Settings.BiosSettingsPage)},
		{"AutoOS.App.Views.Settings.DiskCleanupPage", typeof(AutoOS.App.Views.Settings.DiskCleanupPage)},
		{"AutoOS.App.Views.Settings.SecurityPage", typeof(AutoOS.App.Views.Settings.SecurityPage)},
		{"AutoOS.App.Views.Settings.UpdatePage", typeof(AutoOS.App.Views.Settings.UpdatePage)},
		{"AutoOS.App.Views.Settings.BrowsersPage", typeof(AutoOS.App.Views.Settings.BrowsersPage)},
		{"AutoOS.App.Views.Settings.AppsPage", typeof(AutoOS.App.Views.Settings.AppsPage)},
		{"AutoOS.App.Views.Settings.BenchmarksPage", typeof(AutoOS.App.Views.Settings.BenchmarksPage)},
		{"AutoOS.App.Views.Settings.GamesPage", typeof(AutoOS.App.Views.Settings.GamesPage)}
	};
}
