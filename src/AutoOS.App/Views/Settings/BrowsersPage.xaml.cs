using AutoOS.Common;
using AutoOS.Views.Installer.Stages;
using AutoOS.Views.Updater;
using System.Collections.ObjectModel;

namespace AutoOS.Views.Settings;

public sealed partial class BrowsersPage : Page
{
	private readonly ObservableCollection<GridViewItem> browserItems = [];
	private readonly ObservableCollection<GridViewItem> extensionItems = [];

	public BrowsersPage()
	{
		InitializeComponent();
		GetItems();

		Browsers.ItemsSource = browserItems;
		Extensions.ItemsSource = extensionItems;

		browserItems.CollectionChanged += (s, e) => Bindings.Update();
		extensionItems.CollectionChanged += (s, e) => Bindings.Update();
	}

	public Visibility GetVisibility(int count) => count > 0 ? Visibility.Visible : Visibility.Collapsed;

	private void GetItems()
	{
		var browsers = new List<GridViewItem>
		{
			new() { Text = "Chrome", ImageSource = "ms-appx:///Assets/Fluent/Chrome.png", IsInstalled = File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Google", "Chrome", "Application", "chrome.exe")) },
			new() { Text = "Thorium", ImageSource = "ms-appx:///Assets/Fluent/Thorium.png", IsInstalled = File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Thorium", "Application", "thorium.exe")) },
			new() { Text = "Helium", ImageSource = "ms-appx:///Assets/Fluent/Helium.png", IsInstalled = File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "imput", "Helium", "Application", "chrome.exe")) },
			new() { Text = "Brave", ImageSource = "ms-appx:///Assets/Fluent/Brave.png", IsInstalled = File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "BraveSoftware", "Brave-Browser", "Application", "brave.exe")) },
			new() { Text = "Vivaldi", ImageSource = "ms-appx:///Assets/Fluent/Vivaldi.png", IsInstalled = File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Vivaldi", "Application", "vivaldi.exe")) },
			new() { Text = "Arc", ImageSource = "ms-appx:///Assets/Fluent/Arc.png", IsInstalled = Directory.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Packages", "TheBrowserCompany.Arc_ttt1ap7aakyb4")) },
			new() { Text = "Comet", ImageSource = "ms-appx:///Assets/Fluent/Comet.png", IsInstalled = File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Perplexity", "Comet", "Application", "comet.exe")) },
			new() { Text = "Firefox", ImageSource = "ms-appx:///Assets/Fluent/Firefox.png", IsInstalled = File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Mozilla Firefox", "firefox.exe")) },
			new() { Text = "Zen", ImageSource = "ms-appx:///Assets/Fluent/Zen.png", IsInstalled = File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Zen Browser", "zen.exe")) },
			new() { Text = "Waterfox", ImageSource = "ms-appx:///Assets/Fluent/Waterfox.png", IsInstalled = File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Waterfox", "waterfox.exe")) },
			new() { Text = "LibreWolf", ImageSource = "ms-appx:///Assets/Fluent/Librewolf.png", IsInstalled = File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "LibreWolf", "librewolf.exe")) },
			new() { Text = "Mullvad Browser", ImageSource = "ms-appx:///Assets/Fluent/Mullvad.png", IsInstalled = File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Mullvad", "MullvadBrowser", "Release", "mullvadbrowser.exe")) },
			new() { Text = "Tor Browser", ImageSource = "ms-appx:///Assets/Fluent/Tor.png", IsInstalled = File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Tor Browser", "Browser", "firefox.exe")) }

		};
		foreach (var item in browsers.Where(item => !item.IsInstalled))
			browserItems.Add(item);

		var extensions = new List<GridViewItem>
		{
			new() { Text = "uBlock Origin", ImageSource = "ms-appx:///Assets/Fluent/UBlockorigin.png" },
			new() { Text = "Privacy Badger", ImageSource = "ms-appx:///Assets/Fluent/PrivacyBadger.png" },
			new() { Text = "Decentraleyes", ImageSource = "ms-appx:///Assets/Fluent/Decentraleyes.png" },
			new() { Text = "I still don't care about cookies", ImageSource = "ms-appx:///Assets/Fluent/IStillDontCareAboutCookies.png" },
			new() { Text = "Violentmonkey", ImageSource = "ms-appx:///Assets/Fluent/Violentmonkey.png" },
			new() { Text = "Tampermonkey", ImageSource = "ms-appx:///Assets/Fluent/Tampermonkey.png" },
			new() { Text = "SponsorBlock", ImageSource = "ms-appx:///Assets/Fluent/Sponsorblock.png" },
			new() { Text = "Return YouTube Dislike", ImageSource = "ms-appx:///Assets/Fluent/ReturnYouTubeDislike.png" },
			new() { Text = "Dark Reader", ImageSource = "ms-appx:///Assets/Fluent/Darkreader.png" },
			new() { Text = "Shazam", ImageSource = "ms-appx:///Assets/Fluent/Shazam.png" },
			new() { Text = "Wayback Machine", ImageSource = "ms-appx:///Assets/Fluent/WaybackMachine.png" },
			new() { Text = "iCloud Passwords", ImageSource = "ms-appx:///Assets/Fluent/IcloudPasswords.png" },
			new() { Text = "Bitwarden", ImageSource = "ms-appx:///Assets/Fluent/Bitwarden.png" },
			new() { Text = "1Password", ImageSource = "ms-appx:///Assets/Fluent/1Password.png" }
		};
		foreach (var item in extensions)
			extensionItems.Add(item);
	}

	private void GridView_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		InstallButton.IsEnabled = Browsers.SelectedItems.Count > 0;
	}

	private async void InstallButton_Click(object sender, RoutedEventArgs e)
	{
		var selectedBrowsersItems = Browsers.SelectedItems.Cast<GridViewItem>().ToList();
		var selectedBrowsers = selectedBrowsersItems.Select(item => item.Text).ToList();

		var selectedExtensionsItems = Extensions.SelectedItems.Cast<GridViewItem>().ToList();
		var selectedExtensions = selectedExtensionsItems.Select(item => item.Text).ToList();

		if (selectedBrowsers.Count == 0 && selectedExtensions.Count == 0) return;

		var selection = new BrowserSelection
		{
			Chrome = selectedBrowsers.Contains("Chrome"),
			Thorium = selectedBrowsers.Contains("Thorium"),
			Helium = selectedBrowsers.Contains("Helium"),
			Brave = selectedBrowsers.Contains("Brave"),
			Vivaldi = selectedBrowsers.Contains("Vivaldi"),
			Arc = selectedBrowsers.Contains("Arc"),
			Comet = selectedBrowsers.Contains("Comet"),
			Firefox = selectedBrowsers.Contains("Firefox"),
			Zen = selectedBrowsers.Contains("Zen"),
			Waterfox = selectedBrowsers.Contains("Waterfox"),
			LibreWolf = selectedBrowsers.Contains("LibreWolf"),
			Mullvad = selectedBrowsers.Contains("Mullvad Browser"),

			uBlock = selectedExtensions.Contains("uBlock Origin"),
			SponsorBlock = selectedExtensions.Contains("SponsorBlock"),
			ReturnYouTubeDislike = selectedExtensions.Contains("Return YouTube Dislike"),
			Cookies = selectedExtensions.Contains("I still don't care about cookies"),
			DarkReader = selectedExtensions.Contains("Dark Reader"),
			Violentmonkey = selectedExtensions.Contains("Violentmonkey"),
			Tampermonkey = selectedExtensions.Contains("Tampermonkey"),
			Shazam = selectedExtensions.Contains("Shazam"),
			WaybackMachine = selectedExtensions.Contains("Wayback Machine"),
			iCloud = selectedExtensions.Contains("iCloud Passwords"),
			Bitwarden = selectedExtensions.Contains("Bitwarden"),
			OnePassword = selectedExtensions.Contains("1Password")
		};

		var updateDialog = new UpdateDialog();
		var reporter = new UpdateDialogReporter(updateDialog);
		var actions = BrowsersStage.GetActions(reporter, selection);

		var dialog = new ContentDialog
		{
			Title = "Installing Browsers",
			Content = updateDialog,
			Resources = new ResourceDictionary
			{
				["ContentDialogMinHeight"] = 0.0,
				["ContentDialogMinWidth"] = 500,
				["ContentDialogMaxWidth"] = 1000
			},
			XamlRoot = XamlRoot
		};

		_ = dialog.ShowAsync();
		await updateDialog.RunActions(actions);
		await Task.Delay(500);
		updateDialog.SetStatus("Installation complete.");
		updateDialog.SetSuccess();
		await Task.Delay(1000);
		dialog.Hide();

		foreach (var item in selectedBrowsersItems)
			browserItems.Remove(item);

		Extensions.SelectedItems.Clear();

		GridView_SelectionChanged(null, null);
	}
}
