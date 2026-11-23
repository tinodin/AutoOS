namespace AutoOS.App.Assets.NavViewMenu;

public partial class BreadcrumbPageMappingsInstaller
{
	public static Dictionary<Type, BreadcrumbPageConfig> PageDictionary { get; } = new()
	{
		{ typeof(Views.Installer.HomePage), new BreadcrumbPageConfig { IsHeaderVisible = false, ClearNavigation = false } },
		{ typeof(Views.Installer.PersonalizationPage), new BreadcrumbPageConfig { IsHeaderVisible = false, ClearNavigation = false } },
		{ typeof(Views.Installer.BrowsersPage), new BreadcrumbPageConfig { IsHeaderVisible = false, ClearNavigation = false } },
		{ typeof(Views.Installer.AppsPage), new BreadcrumbPageConfig { IsHeaderVisible = false, ClearNavigation = false } },
		{ typeof(Views.Installer.DisplaysPage), new BreadcrumbPageConfig { IsHeaderVisible = false, ClearNavigation = false } },
		{ typeof(Views.Installer.GraphicsPage), new BreadcrumbPageConfig { IsHeaderVisible = false, ClearNavigation = false } },
		{ typeof(Views.Installer.SecurityPage), new BreadcrumbPageConfig { IsHeaderVisible = false, ClearNavigation = false } },
		{ typeof(Views.Installer.InstallPage), new BreadcrumbPageConfig { IsHeaderVisible = false, ClearNavigation = false } }
	};
}
