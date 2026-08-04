namespace AutoOS.App.Assets.NavViewMenu;

public partial class NavigationPageMappingsInstaller
{
	public static Dictionary<string, Type> PageDictionary { get; } = new Dictionary<string, Type>
	{
		{"AutoOS.App.Views.Installer.HomePage", typeof(AutoOS.App.Views.Installer.HomePage)},
		{"AutoOS.App.Views.Installer.PersonalizationPage", typeof(AutoOS.App.Views.Installer.PersonalizationPage)},
		{"AutoOS.App.Views.Installer.BrowsersPage", typeof(AutoOS.App.Views.Installer.BrowsersPage)},
		{"AutoOS.App.Views.Installer.AppsPage", typeof(AutoOS.App.Views.Installer.AppsPage)},
		{"AutoOS.App.Views.Installer.DisplaysPage", typeof(AutoOS.App.Views.Installer.DisplaysPage)},
		{"AutoOS.App.Views.Installer.GraphicsPage", typeof(AutoOS.App.Views.Installer.GraphicsPage)},
		{"AutoOS.App.Views.Installer.SecurityPage", typeof(AutoOS.App.Views.Installer.SecurityPage)},
		{"AutoOS.App.Views.Installer.InstallPage", typeof(AutoOS.App.Views.Installer.InstallPage)},
	};
}
