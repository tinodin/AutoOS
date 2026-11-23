namespace AutoOS.App.Assets.NavViewMenu;

public partial class BreadcrumbPageMappingsSettings
{
	public static Dictionary<Type, BreadcrumbPageConfig> PageDictionary { get; } = new()
	{
		{ typeof(Views.Settings.HomePage), new BreadcrumbPageConfig { IsHeaderVisible = false, ClearNavigation = false } },
		{ typeof(Views.Settings.SoundPage), new BreadcrumbPageConfig { IsHeaderVisible = false, ClearNavigation = false } },
		{ typeof(Views.Settings.DisplaysPage), new BreadcrumbPageConfig { IsHeaderVisible = false, ClearNavigation = false } },
		{ typeof(Views.Settings.GraphicsPage), new BreadcrumbPageConfig { IsHeaderVisible = false, ClearNavigation = false } },
		{ typeof(Views.Settings.SchedulingPage), new BreadcrumbPageConfig { IsHeaderVisible = false, ClearNavigation = false } },
		{ typeof(Views.Settings.DevicesPage), new BreadcrumbPageConfig { IsHeaderVisible = false, ClearNavigation = false } },
		{ typeof(Views.Settings.NetworkPage), new BreadcrumbPageConfig { IsHeaderVisible = false, ClearNavigation = false } },
		{ typeof(Views.Settings.PowerPage), new BreadcrumbPageConfig { IsHeaderVisible = false, ClearNavigation = false } },
		{ typeof(Views.Settings.ServicesPage), new BreadcrumbPageConfig { IsHeaderVisible = false, ClearNavigation = false } },
		{ typeof(Views.Settings.BiosSettingsPage), new BreadcrumbPageConfig { IsHeaderVisible = false, ClearNavigation = false } },
		{ typeof(Views.Settings.DiskCleanupPage), new BreadcrumbPageConfig { IsHeaderVisible = false, ClearNavigation = false } },
		{ typeof(Views.Settings.SecurityPage), new BreadcrumbPageConfig { IsHeaderVisible = false, ClearNavigation = false } },
		{ typeof(Views.Settings.UpdatePage), new BreadcrumbPageConfig { IsHeaderVisible = false, ClearNavigation = false } },
		{ typeof(Views.Settings.BrowsersPage), new BreadcrumbPageConfig { IsHeaderVisible = false, ClearNavigation = false } },
		{ typeof(Views.Settings.AppsPage), new BreadcrumbPageConfig { IsHeaderVisible = false, ClearNavigation = false } },
		{ typeof(Views.Settings.BenchmarksPage), new BreadcrumbPageConfig { IsHeaderVisible = false, ClearNavigation = false } },
		{ typeof(Views.Settings.GamesPage), new BreadcrumbPageConfig { IsHeaderVisible = false, ClearNavigation = false } }
	};
}
