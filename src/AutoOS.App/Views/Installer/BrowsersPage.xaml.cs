using Windows.Storage;

namespace AutoOS.Views.Installer;

public sealed partial class BrowsersPage : Page
{
	private bool isInitializingBrowsersState = true;
	private bool isInitializingExtensionsState = true;

	private readonly ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

	public BrowsersPage()
	{
		InitializeComponent();
		GetItems();
		GetBrowsers();
		GetExtensions();
	}

	protected override void OnNavigatedTo(NavigationEventArgs e)
	{
		base.OnNavigatedTo(e);
		MainWindow.Instance.MarkVisited(nameof(BrowsersPage));
		MainWindow.Instance.CheckAllPagesVisited();
	}

	private void GetItems()
	{
		Browsers.ItemsSource = new List<GridViewItem>
		{
			new() { Text = "Chrome", ImageSource = "ms-appx:///Assets/FluentIcons/Browsers/Chrome.png" },
			new() { Text = "Thorium", ImageSource = "ms-appx:///Assets/FluentIcons/Browsers/Thorium.png" },
			new() { Text = "Helium", ImageSource = "ms-appx:///Assets/FluentIcons/Browsers/Helium.png" },
			new() { Text = "Brave", ImageSource = "ms-appx:///Assets/FluentIcons/Browsers/Brave.png" },
			new() { Text = "Vivaldi", ImageSource = "ms-appx:///Assets/FluentIcons/Browsers/Vivaldi.png" },
			new() { Text = "Arc", ImageSource = "ms-appx:///Assets/FluentIcons/Browsers/Arc.png" },
			new() { Text = "Comet", ImageSource = "ms-appx:///Assets/FluentIcons/Browsers/Comet.png" },
			new() { Text = "Firefox", ImageSource = "ms-appx:///Assets/FluentIcons/Browsers/Firefox.png" },
			new() { Text = "Zen", ImageSource = "ms-appx:///Assets/FluentIcons/Browsers/Zen.png" },
			new() { Text = "Waterfox", ImageSource = "ms-appx:///Assets/FluentIcons/Browsers/Waterfox.png" },
			new() { Text = "LibreWolf", ImageSource = "ms-appx:///Assets/FluentIcons/Browsers/LibreWolf.png" },
			new() { Text = "Floorp", ImageSource = "ms-appx:///Assets/FluentIcons/Browsers/Floorp.png" },
			new() { Text = "Mullvad Browser", ImageSource = "ms-appx:///Assets/FluentIcons/Browsers/Mullvad.png" }
		};

		Extensions.ItemsSource = new List<GridViewItem>
		{
			new() { Text = "uBlock Origin", ImageSource = "ms-appx:///Assets/FluentIcons/Browsers/Extensions/UBlockOrigin.png" },
			new() { Text = "Privacy Badger", ImageSource = "ms-appx:///Assets/FluentIcons/Browsers/Extensions/PrivacyBadger.png" },
			new() { Text = "Decentraleyes", ImageSource = "ms-appx:///Assets/FluentIcons/Browsers/Extensions/Decentraleyes.png" },
			new() { Text = "I still don't care about cookies", ImageSource = "ms-appx:///Assets/FluentIcons/Browsers/Extensions/IStillDontCareAboutCookies.png" },
			new() { Text = "Violentmonkey", ImageSource = "ms-appx:///Assets/FluentIcons/Browsers/Extensions/Violentmonkey.png" },
			new() { Text = "Tampermonkey", ImageSource = "ms-appx:///Assets/FluentIcons/Browsers/Extensions/Tampermonkey.png" },
			new() { Text = "SponsorBlock", ImageSource = "ms-appx:///Assets/FluentIcons/Browsers/Extensions/Sponsorblock.png" },
			new() { Text = "Return YouTube Dislike", ImageSource = "ms-appx:///Assets/FluentIcons/Browsers/Extensions/ReturnYouTubeDislike.png" },
			new() { Text = "YouTube No Translation", ImageSource = "ms-appx:///Assets/FluentIcons/Browsers/Extensions/YouTubeNoTranslation.png" },
			new() { Text = "Dark Reader", ImageSource = "ms-appx:///Assets/FluentIcons/Browsers/Extensions/Darkreader.png" },
			new() { Text = "Shazam", ImageSource = "ms-appx:///Assets/FluentIcons/Browsers/Extensions/Shazam.png" },
			new() { Text = "Wayback Machine", ImageSource = "ms-appx:///Assets/FluentIcons/Browsers/Extensions/WaybackMachine.png" },
			new() { Text = "iCloud Passwords", ImageSource = "ms-appx:///Assets/FluentIcons/Browsers/Extensions/IcloudPasswords.png" },
			new() { Text = "Bitwarden", ImageSource = "ms-appx:///Assets/FluentIcons/Browsers/Extensions/Bitwarden.png" },
			new() { Text = "1Password", ImageSource = "ms-appx:///Assets/FluentIcons/Browsers/Extensions/1Password.png" }
		};
	}

	private void GetBrowsers()
	{
		var selectedBrowsers = localSettings.Values["Browsers"] as string;
		var BrowsersItems = Browsers.ItemsSource as List<GridViewItem>;
		Browsers.SelectedItems.AddRange(
			selectedBrowsers?.Split([", "], StringSplitOptions.RemoveEmptyEntries)
			.Select(e => BrowsersItems?.FirstOrDefault(ext => ext.Text == e))
			.Where(ext => ext != null) ?? []
		);

		isInitializingBrowsersState = false;
	}

	private void Browsers_Changed(object sender, SelectionChangedEventArgs e)
	{
		if (isInitializingBrowsersState) return;

		var selectedBrowsers = Browsers.SelectedItems
			.Cast<GridViewItem>()
			.Select(item => item.Text)
			.ToArray();

		localSettings.Values["Browsers"] = string.Join(", ", selectedBrowsers);
	}

	private void GetExtensions()
	{
		var selectedExtensions = localSettings.Values["Extensions"] as string;
		var extensionsItems = Extensions.ItemsSource as List<GridViewItem>;
		Extensions.SelectedItems.AddRange(
			selectedExtensions?.Split([", "], StringSplitOptions.RemoveEmptyEntries)
			.Select(e => extensionsItems?.FirstOrDefault(ext => ext.Text == e))
			.Where(ext => ext != null) ?? []
		);

		isInitializingExtensionsState = false;
	}

	private void Extensions_Changed(object sender, SelectionChangedEventArgs e)
	{
		if (isInitializingExtensionsState) return;

		var selectedExtensions = Extensions.SelectedItems
			.Cast<GridViewItem>()
			.Select(item => item.Text)
			.ToArray();

		localSettings.Values["Extensions"] = string.Join(", ", selectedExtensions);
	}
}
