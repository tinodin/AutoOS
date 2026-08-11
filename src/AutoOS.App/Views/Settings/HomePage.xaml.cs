using System.Net.Http.Headers;
using System.Text.Json;
using AutoOS.App.Views.Installer.Stages;
using AutoOS.App.Views.Updater;
using AutoOS.App.Views.Updater.Stages;
using AutoOS.Core.Helpers.Database;
using AutoOS.Core.Helpers.Logging;
using AutoOS.Core.Helpers.OS;
using CommunityToolkit.WinUI.Controls;
using Microsoft.Win32;
using Windows.Storage;

namespace AutoOS.App.Views.Settings;

public sealed partial class HomePage : Page
{
	private readonly ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
	//public string WASDKVersion { get; } = $"Windows App SDK {ReleaseInfo.Major}.{ReleaseInfo.Minor}";
	private static readonly HttpClient httpClient = new()
	{
		DefaultRequestHeaders =
		{
			UserAgent =
			{
				new ProductInfoHeaderValue("AutoOS", ProcessInfoHelper.Version)
			}
		}
	};

	public HomePage()
	{
		InitializeComponent();
		Loaded += CheckForUpdates;
	}

	private async void CheckForUpdates(object sender, RoutedEventArgs e)
	{
		if (!(Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\ProfileList")?.GetSubKeyNames()?.Any(sid => new[] { "AutoOS", "user" }.Contains(Path.GetFileName(Registry.GetValue($@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\ProfileList\{sid}", "ProfileImagePath", null) as string), StringComparer.OrdinalIgnoreCase)) == true))
		{
			var dialog = new ContentDialog
			{
				Title = "Unsupported System",
				Content = "AutoOS App is only supported on AutoOS.",
				CloseButtonText = "OK",
				DefaultButton = ContentDialogButton.Close,
				XamlRoot = XamlRoot
			};
			await dialog.ShowAsync();
			Application.Current.Exit();
		}

		(ushort major, ushort minor, ushort build, ushort ubr) = OSHelper.GetWindowsVersion();
		if (build < 26200)
		{
			var dialog = new ContentDialog
			{
				Title = "Unsupported Windows Version",
				Content = $"AutoOS is only supported on Windows 11 25H2. \nPlease follow the installation guide on GitHub.",
				CloseButtonText = "OK",
				DefaultButton = ContentDialogButton.Close,
				XamlRoot = XamlRoot
			};
			await dialog.ShowAsync();
			Application.Current.Exit();
		}

		if (localSettings.Values["ServerPromptShown"] is not true)
		{
			List<DiscordHelper.DiscordAccountInfo> localAccounts = DiscordHelper.GetLocalAccounts();
			if (localAccounts.Count > 0 && !localAccounts.Any(account => account.IsMember))
			{
				localSettings.Values["ServerPromptShown"] = true;

				var serverDialog = new ContentDialog
				{
					Title = "Join the Discord Server",
					Content = "Join to get instant support, and help shape the future of the project.",
					PrimaryButtonText = "Join now",
					CloseButtonText = "No thanks",
					DefaultButton = ContentDialogButton.Close,
					XamlRoot = XamlRoot
				};

				if (await serverDialog.ShowAsync() == ContentDialogResult.Primary)
					await Windows.System.Launcher.LaunchUriAsync(new Uri("https://discord.gg/bZU4dMMWpg"));
			}
		}

		Version currentVersion = new(ProcessInfoHelper.Version);

		localSettings.Values.TryGetValue("Version", out object? storedVersionObj);
		Version? storedVersion = storedVersionObj is string storedVersionStr ? new(storedVersionStr) : null;

		if (currentVersion.CompareTo(storedVersion) > 0)
		{
			try
			{
				using var doc = JsonDocument.Parse(await httpClient.GetStringAsync($"https://api.github.com/repos/tinodin/AutoOS/releases/tags/v{currentVersion}"));

				if (doc.RootElement.TryGetProperty("body", out JsonElement body))
				{
					string rawChangelog = body.GetString()!;
					string cleanedChangelog = rawChangelog.Replace("`", "");
					int changelogStart = cleanedChangelog.IndexOf("- ");
					if (changelogStart > 0 && cleanedChangelog[changelogStart - 1] != '\n')
						changelogStart = -1;
					string changelog = changelogStart >= 0 ? cleanedChangelog[changelogStart..] : cleanedChangelog;

					var contentDialog = new ContentDialog
					{
						Title = $"What's new in AutoOS v{currentVersion}",
						Content = new ScrollViewer
						{
							Content = new MarkdownTextBlock
							{
								Text = changelog,
								Config = new MarkdownConfig()
							},
							Padding = new Thickness(0, 0, 36, 0)
						},
						CloseButtonText = "Close",
						XamlRoot = XamlRoot
					};

					contentDialog.Resources["ContentDialogMaxWidth"] = 1000;
					contentDialog.Resources["ContentDialogMaxHeight"] = 1000;

					await contentDialog.ShowAsync();
				}
			}
			catch
			{ }

			var updateDialog = new UpdateDialog();
			List<(string Title, Func<Task> Action, Func<bool>? Condition)> actions = UpdateStage.UpdateActions(updateDialog);

			if (actions.Count > 0)
			{
				var updater = new ContentDialog
				{
					Title = "Applying Update...",
					Content = updateDialog,
					Resources = new ResourceDictionary
					{
						["ContentDialogMinHeight"] = 0.0,
						["ContentDialogMinWidth"] = 550,
						["ContentDialogMaxWidth"] = 1000
					},
					XamlRoot = XamlRoot
				};

				_ = updater.ShowAsync();
				await updateDialog.RunActions(actions);
				await Task.Delay(500);
				updateDialog.SetStatus("Update complete.");
				updateDialog.SetSuccess();
				await Task.Delay(1000);
				updater.Hide();
			}

			localSettings.Values["Version"] = currentVersion.ToString();
			Registry.SetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\AutoOS", "IsInstalled", 1, RegistryValueKind.DWord);
			try
			{
				_ = LogHelper.Log(PreparingStage.GPUs);
			}
			catch (Exception ex)
			{
				await LogHelper.LogFallbackError(ex);
			}
		}

#if !DEBUG
		try
		{
			var json = await httpClient.GetStringAsync("https://api.github.com/repos/tinodin/AutoOS/releases");
			using var releasesDoc = JsonDocument.Parse(json);

			var releases = releasesDoc.RootElement.EnumerateArray()
				.Select(release =>
				{
					string tag = release.GetProperty("tag_name").GetString()!;
					return new
					{
						Version = Version.Parse(tag.TrimStart('v')),
						Json = release
					};
				})
				.Where(x => x.Version.CompareTo(currentVersion) > 0)
				.OrderBy(x => x.Version)
				.ToList();

			if (releases.Count == 0)
				return;

			var nextRelease = releases.First();
			var assets = nextRelease.Json.GetProperty("assets");
			string downloadUrl = assets.EnumerateArray()
				.First(a => a.GetProperty("name").GetString() == "AutoOS.msix")
					.GetProperty("browser_download_url")
					.GetString()!;

			Version nextVersion = nextRelease.Version;

			var confirmDialog = new ContentDialog
			{
				Title = "Update Available",
				Content = $"Do you want to update AutoOS from v{currentVersion} to v{nextVersion}?",
				PrimaryButtonText = "Yes",
				CloseButtonText = "No",
				DefaultButton = ContentDialogButton.Close,
				XamlRoot = XamlRoot
			};

			if (await confirmDialog.ShowAsync() != ContentDialogResult.Primary)
				return;

			var msixDialog = new UpdateDialog();

			var msixUpdater = new ContentDialog
			{
				Title = $"Updating to AutoOS v{nextVersion}...",
				Content = msixDialog,
				Resources = new ResourceDictionary
				{
					["ContentDialogMinHeight"] = 0.0,
					["ContentDialogMinWidth"] = 500,
					["ContentDialogMaxWidth"] = 1000
				},
				XamlRoot = XamlRoot
			};

			_ = msixUpdater.ShowAsync();

			await PackageStage.PackageActions(downloadUrl, msixDialog);
		}
		catch { }
#endif
	}
}
