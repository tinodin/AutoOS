﻿using Windows.Storage;

namespace AutoOS.Views.Installer;

public sealed partial class BrowserPage : Page
{
    private bool isInitializingBrowsersState = true;
    private bool isInitializingExtensionsState = true;

    private readonly ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

    public BrowserPage()
    {
        InitializeComponent();
        GetItems();
        GetBrowser();
        GetExtensions();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        MainWindow.Instance.MarkVisited(nameof(BrowserPage));
        MainWindow.Instance.CheckAllPagesVisited();
    }

    public class GridViewItem
    {
        public string Text { get; set; }
        public string ImageSource { get; set; }
    }

    private void GetItems()
    {
        Browsers.ItemsSource = new List<GridViewItem>
    {
        new GridViewItem { Text = "Chrome", ImageSource = "ms-appx:///Assets/Fluent/Chrome.png" },
        new GridViewItem { Text = "Brave", ImageSource = "ms-appx:///Assets/Fluent/Brave.png" },
        new GridViewItem { Text = "Firefox", ImageSource = "ms-appx:///Assets/Fluent/Firefox.png" },
        //new GridViewItem { Text = "Zen", ImageSource = "ms-appx:///Assets/Fluent/Zen.png" },
        new GridViewItem { Text = "Arc", ImageSource = "ms-appx:///Assets/Fluent/Arc.png" }
    };

        Extensions.ItemsSource = new List<GridViewItem>
    {
        new GridViewItem { Text = "uBlock Origin", ImageSource = "ms-appx:///Assets/Fluent/UBlockorigin.png" },
        new GridViewItem { Text = "SponsorBlock", ImageSource = "ms-appx:///Assets/Fluent/Sponsorblock.png" },
        new GridViewItem { Text = "Return YouTube Dislike", ImageSource = "ms-appx:///Assets/Fluent/ReturnYouTubeDislike.png" },
        new GridViewItem { Text = "I still don't care about cookies", ImageSource = "ms-appx:///Assets/Fluent/IStillDontCareAboutCookies.png" },
        new GridViewItem { Text = "Dark Reader", ImageSource = "ms-appx:///Assets/Fluent/Darkreader.png" },
        new GridViewItem { Text = "Violentmonkey", ImageSource = "ms-appx:///Assets/Fluent/Violentmonkey.png" },
        new GridViewItem { Text = "Tampermonkey", ImageSource = "ms-appx:///Assets/Fluent/Tampermonkey.png" },
        new GridViewItem { Text = "Shazam", ImageSource = "ms-appx:///Assets/Fluent/Shazam.png" },
        new GridViewItem { Text = "iCloud Passwords", ImageSource = "ms-appx:///Assets/Fluent/IcloudPasswords.png" },
        new GridViewItem { Text = "Bitwarden", ImageSource = "ms-appx:///Assets/Fluent/Bitwarden.png" },
        new GridViewItem { Text = "1Password", ImageSource = "ms-appx:///Assets/Fluent/1Password.png" }
    };
    }

    private void GetBrowser()
    {
        var selectedBrowser = localSettings.Values["Browser"] as string;
        var browserItems = Browsers.ItemsSource as List<GridViewItem>;
        Browsers.SelectedItems.AddRange(
            selectedBrowser?.Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries)
            .Select(e => browserItems?.FirstOrDefault(ext => ext.Text == e))
            .Where(ext => ext != null) ?? Enumerable.Empty<GridViewItem>()
        );

        isInitializingBrowsersState = false;
    }

    private void Browsers_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (isInitializingBrowsersState) return;

        var selectedBrowser = Browsers.SelectedItems
            .Cast<GridViewItem>()
            .Select(item => item.Text)
            .ToArray();

        localSettings.Values["Browser"] = string.Join(", ", selectedBrowser);
    }

    private void GetExtensions()
    {
        var selectedExtensions = localSettings.Values["Extensions"] as string;
        var extensionsItems = Extensions.ItemsSource as List<GridViewItem>;
        Extensions.SelectedItems.AddRange(
            selectedExtensions?.Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries)
            .Select(e => extensionsItems?.FirstOrDefault(ext => ext.Text == e))
            .Where(ext => ext != null) ?? Enumerable.Empty<GridViewItem>()
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