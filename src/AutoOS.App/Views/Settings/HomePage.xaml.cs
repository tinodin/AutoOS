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

		// ; 58989092
		// ; Low Latency Profile Feature Bundle June
		if (ubr >= 8524 && (Registry.GetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\FeatureManagement\Overrides\8\1213986446", "EnabledState", null) is not int v1213986446 || v1213986446 != 2))
		{
			Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\FeatureManagement\Overrides\8\1213986446", "EnabledState", 2, RegistryValueKind.DWord);
			Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\FeatureManagement\Overrides\8\1213986446", "EnabledStateOptions", 0, RegistryValueKind.DWord);
			Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\FeatureManagement\Overrides\8\1213986446", "Variant", 0, RegistryValueKind.DWord);
			Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\FeatureManagement\Overrides\8\1213986446", "VariantPayload", 0, RegistryValueKind.DWord);
			Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\FeatureManagement\Overrides\8\1213986446", "VariantPayloadKind", 0, RegistryValueKind.DWord);
		}
		// ; 60716524
		// ; New Low Latency Profile
		if (ubr >= 8524 && (Registry.GetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\FeatureManagement\Overrides\8\3632644751", "EnabledState", null) is not int v3632644751 || v3632644751 != 2))
		{
			Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\FeatureManagement\Overrides\8\3632644751", "EnabledState", 2, RegistryValueKind.DWord);
			Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\FeatureManagement\Overrides\8\3632644751", "EnabledStateOptions", 0, RegistryValueKind.DWord);
			Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\FeatureManagement\Overrides\8\3632644751", "Variant", 0, RegistryValueKind.DWord);
			Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\FeatureManagement\Overrides\8\3632644751", "VariantPayload", 0, RegistryValueKind.DWord);
			Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\FeatureManagement\Overrides\8\3632644751", "VariantPayloadKind", 0, RegistryValueKind.DWord);
		}
		// ; 61391826
		// ; New Low Latency Profile For Application Launch
		if (ubr >= 8524 && (Registry.GetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\FeatureManagement\Overrides\8\2783555215", "EnabledState", null) is not int v2783555215 || v2783555215 != 2))
		{
			Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\FeatureManagement\Overrides\8\2783555215", "EnabledState", 2, RegistryValueKind.DWord);
			Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\FeatureManagement\Overrides\8\2783555215", "EnabledStateOptions", 0, RegistryValueKind.DWord);
			Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\FeatureManagement\Overrides\8\2783555215", "Variant", 0, RegistryValueKind.DWord);
			Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\FeatureManagement\Overrides\8\2783555215", "VariantPayload", 0, RegistryValueKind.DWord);
			Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\FeatureManagement\Overrides\8\2783555215", "VariantPayloadKind", 0, RegistryValueKind.DWord);
		}
		// ; 58989177
		// ; Low Latency Profile Feature Bundle July
		if (ubr >= 8524 && (Registry.GetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\FeatureManagement\Overrides\8\4066113166", "EnabledState", null) is not int v4066113166 || v4066113166 != 2))
		{
			Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\FeatureManagement\Overrides\8\4066113166", "EnabledState", 2, RegistryValueKind.DWord);
			Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\FeatureManagement\Overrides\8\4066113166", "EnabledStateOptions", 0, RegistryValueKind.DWord);
			Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\FeatureManagement\Overrides\8\4066113166", "Variant", 0, RegistryValueKind.DWord);
			Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\FeatureManagement\Overrides\8\4066113166", "VariantPayload", 0, RegistryValueKind.DWord);
			Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\FeatureManagement\Overrides\8\4066113166", "VariantPayloadKind", 0, RegistryValueKind.DWord);
		}
		// ; 61161244
		// ; Main Feature Bundle
		if (ubr >= 9278 && (Registry.GetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\FeatureManagement\Overrides\8\940684430", "EnabledState", null) is not int v940684430 || v940684430 != 2))
		{
			Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\FeatureManagement\Overrides\8\940684430", "EnabledState", 2, RegistryValueKind.DWord);
			Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\FeatureManagement\Overrides\8\940684430", "EnabledStateOptions", 0, RegistryValueKind.DWord);
			Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\FeatureManagement\Overrides\8\940684430", "Variant", 0, RegistryValueKind.DWord);
			Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\FeatureManagement\Overrides\8\940684430", "VariantPayload", 0, RegistryValueKind.DWord);
			Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\FeatureManagement\Overrides\8\940684430", "VariantPayloadKind", 0, RegistryValueKind.DWord);
		}
		// ; 62353331
		// ; New Start Menu Customization:
		if (ubr >= 9278 && (Registry.GetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\FeatureManagement\Overrides\8\1722912911", "EnabledState", null) is not int v1722912911 || v1722912911 != 2))
		{
			Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\FeatureManagement\Overrides\8\1722912911", "EnabledState", 2, RegistryValueKind.DWord);
			Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\FeatureManagement\Overrides\8\1722912911", "EnabledStateOptions", 0, RegistryValueKind.DWord);
			Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\FeatureManagement\Overrides\8\1722912911", "Variant", 0, RegistryValueKind.DWord);
			Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\FeatureManagement\Overrides\8\1722912911", "VariantPayload", 0, RegistryValueKind.DWord);
			Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\FeatureManagement\Overrides\8\1722912911", "VariantPayloadKind", 0, RegistryValueKind.DWord);
		}
		// ; 62762248
		// ; New Search UI
		if (ubr >= 9278 && (Registry.GetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\FeatureManagement\Overrides\8\288720014", "EnabledState", null) is not int v288720014 || v288720014 != 2))
		{
			Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\FeatureManagement\Overrides\8\288720014", "EnabledState", 2, RegistryValueKind.DWord);
			Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\FeatureManagement\Overrides\8\288720014", "EnabledStateOptions", 0, RegistryValueKind.DWord);
			Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\FeatureManagement\Overrides\8\288720014", "Variant", 0, RegistryValueKind.DWord);
			Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\FeatureManagement\Overrides\8\288720014", "VariantPayload", 0, RegistryValueKind.DWord);
			Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\FeatureManagement\Overrides\8\288720014", "VariantPayloadKind", 0, RegistryValueKind.DWord);
		}
		// ; 61754985
		// ; New Search UI
		if (ubr >= 9278 && (Registry.GetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\FeatureManagement\Overrides\8\3539182222", "EnabledState", null) is not int v3539182222 || v3539182222 != 2))
		{
			Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\FeatureManagement\Overrides\8\3539182222", "EnabledState", 2, RegistryValueKind.DWord);
			Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\FeatureManagement\Overrides\8\3539182222", "EnabledStateOptions", 0, RegistryValueKind.DWord);
			Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\FeatureManagement\Overrides\8\3539182222", "Variant", 0, RegistryValueKind.DWord);
			Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\FeatureManagement\Overrides\8\3539182222", "VariantPayload", 0, RegistryValueKind.DWord);
			Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\FeatureManagement\Overrides\8\3539182222", "VariantPayloadKind", 0, RegistryValueKind.DWord);
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
					string changelog = rawChangelog.Replace("`", "")[rawChangelog.IndexOf("- ")..];

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
					contentDialog.Resources["ContentDialogMaxHeight"] = 850;

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
			string json = await httpClient.GetStringAsync("https://api.github.com/repos/tinodin/AutoOS/releases");
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
			JsonElement assets = nextRelease.Json.GetProperty("assets");
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
