namespace AutoOS.Assets.NavViewMenu;

public partial class NavigationPageMappingsInstaller
{
	public static Dictionary<string, Type> PageDictionary { get; } = new Dictionary<string, Type>
	{
		{"AutoOS.Views.Installer.HomePage", typeof(AutoOS.Views.Installer.HomePage)},
		{"AutoOS.Views.Installer.PersonalizationPage", typeof(AutoOS.Views.Installer.PersonalizationPage)},
		{"AutoOS.Views.Installer.BrowsersPage", typeof(AutoOS.Views.Installer.BrowsersPage)},
		{"AutoOS.Views.Installer.AppsPage", typeof(AutoOS.Views.Installer.AppsPage)},
		{"AutoOS.Views.Installer.DisplaysPage", typeof(AutoOS.Views.Installer.DisplaysPage)},
		{"AutoOS.Views.Installer.GraphicsPage", typeof(AutoOS.Views.Installer.GraphicsPage)},
		{"AutoOS.Views.Installer.SecurityPage", typeof(AutoOS.Views.Installer.SecurityPage)},
		{"AutoOS.Views.Installer.InstallPage", typeof(AutoOS.Views.Installer.InstallPage)},
	};
}
