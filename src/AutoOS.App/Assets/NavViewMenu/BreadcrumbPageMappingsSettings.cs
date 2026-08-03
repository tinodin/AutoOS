using DevWinUI;

namespace AutoOS.Assets.NavViewMenu;

public partial class BreadcrumbPageMappingsSettings
{
	public static Dictionary<Type, BreadcrumbPageConfig> PageDictionary { get; } = new()
	{
		{ typeof(Views.Settings.HomePage), new BreadcrumbPageConfig { PageTitle = null, IsHeaderVisible = false, ClearNavigation = false } },
		{ typeof(Views.Settings.SoundPage), new BreadcrumbPageConfig { PageTitle = null, IsHeaderVisible = false, ClearNavigation = false } },
		{ typeof(Views.Settings.DisplaysPage), new BreadcrumbPageConfig { PageTitle = null, IsHeaderVisible = false, ClearNavigation = false } },
		{ typeof(Views.Settings.GraphicsPage), new BreadcrumbPageConfig { PageTitle = null, IsHeaderVisible = false, ClearNavigation = false } },
		{ typeof(Views.Settings.SchedulingPage), new BreadcrumbPageConfig { PageTitle = null, IsHeaderVisible = false, ClearNavigation = false } },
		{ typeof(Views.Settings.DevicesPage), new BreadcrumbPageConfig { PageTitle = null, IsHeaderVisible = false, ClearNavigation = false } },
		{ typeof(Views.Settings.InternetPage), new BreadcrumbPageConfig { PageTitle = null, IsHeaderVisible = false, ClearNavigation = false } },
		{ typeof(Views.Settings.PowerPage), new BreadcrumbPageConfig { PageTitle = null, IsHeaderVisible = false, ClearNavigation = false } },
		{ typeof(Views.Settings.ServicesPage), new BreadcrumbPageConfig { PageTitle = null, IsHeaderVisible = false, ClearNavigation = false } },
		{ typeof(Views.Settings.BiosSettingsPage), new BreadcrumbPageConfig { PageTitle = null, IsHeaderVisible = false, ClearNavigation = false } },
		{ typeof(Views.Settings.DiskCleanupPage), new BreadcrumbPageConfig { PageTitle = null, IsHeaderVisible = false, ClearNavigation = false } },
		{ typeof(Views.Settings.SecurityPage), new BreadcrumbPageConfig { PageTitle = null, IsHeaderVisible = false, ClearNavigation = false } },
		{ typeof(Views.Settings.UpdatePage), new BreadcrumbPageConfig { PageTitle = null, IsHeaderVisible = false, ClearNavigation = false } },
		{ typeof(Views.Settings.BrowsersPage), new BreadcrumbPageConfig { PageTitle = null, IsHeaderVisible = false, ClearNavigation = false } },
		{ typeof(Views.Settings.AppsPage), new BreadcrumbPageConfig { PageTitle = null, IsHeaderVisible = false, ClearNavigation = false } },
		{ typeof(Views.Settings.BenchmarksPage), new BreadcrumbPageConfig { PageTitle = null, IsHeaderVisible = false, ClearNavigation = false } },
		{ typeof(Views.Settings.GamesPage), new BreadcrumbPageConfig { PageTitle = null, IsHeaderVisible = false, ClearNavigation = false } }
	};
}
