using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using AutoOS.Core.Helpers.Database;
using AutoOS.Core.Helpers.Logging;
using AutoOS.Core.Helpers.OS;
using AutoOS.Core.Helpers.Registry;
using AutoOS.App.Views.Installer.Stages;
using AutoOS.App.Views.Updater;
using AutoOS.App.Views.Updater.Stages;
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

		if (ubr >= 8521 && (Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Windhawk\Engine\Mods\windows-11-start-menu-styler\Settings", "controlStyles[0].target", null) as string) == "")
		{
			RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Windhawk\Engine\Mods\windows-11-start-menu-styler\Settings", "theme", "SideBySide2", RegistryValueKind.String);
			RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Windhawk\Engine\Mods\windows-11-start-menu-styler\Settings", "disableNewStartMenuLayout", "", RegistryValueKind.String);
			RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Windhawk\Engine\Mods\windows-11-start-menu-styler\Settings", "controlStyles[1].target", "ScrollViewer > ScrollContentPresenter > Border > StartMenu.StartBlendedFlexFrame > Grid#FrameRoot > Grid#AnimationRoot > Grid#MainMenu > Grid#MainContent > Frame#StartFrame > ContentPresenter > StartMenu.StartHome > Grid#PageRoot > SemanticZoom#TopLevelRoot > Grid > ScrollViewer#ScrollViewer > ScrollContentPresenter#ScrollContentPresenter > Grid > ContentPresenter#ZoomedInPresenter > GridView#AllAppsGrid > Border > Grid#SideBySidePinnedWrapper > ScrollViewer#SideBySidePinnedScrollViewer > Border#Root > Grid > ScrollContentPresenter#ScrollContentPresenter > Grid#SideBySidePinnedContent > Grid#TopLevelSuggestionsRoot > Grid#TopLevelSuggestionsContainerParent > Grid#TopLevelSuggestionsContainer > StartMenu.RecommendedReactNativeViewControl > ContentPresenter > ExperienceExtensions.ReactNativeExperienceViewControl > Grid#RootViewContainer > Microsoft.ReactNative.ReactRootView > Microsoft.ReactNative.ViewPanel > Microsoft.ReactNative.ViewPanel > GridView#RecommendedList > Border > ScrollViewer#ScrollViewer > Border#Root > Grid > ScrollContentPresenter#ScrollContentPresenter > ItemsPresenter > ItemsWrapGrid > GridViewItem#RecommendedItemRoot0", RegistryValueKind.String);
			RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Windhawk\Engine\Mods\windows-11-start-menu-styler\Settings", "controlStyles[1].styles[0]", "MinWidth=220", RegistryValueKind.String);
			RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Windhawk\Engine\Mods\windows-11-start-menu-styler\Settings", "controlStyles[1].styles[1]", "MaxWidth=220", RegistryValueKind.String);
			RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Windhawk\Engine\Mods\windows-11-start-menu-styler\Settings", "controlStyles[0].target", "ScrollViewer > ScrollContentPresenter > Border > StartMenu.StartBlendedFlexFrame > Grid#FrameRoot > Grid#AnimationRoot > Grid#MainMenu > Grid#MainContent > Frame#StartFrame > ContentPresenter > StartMenu.StartHome > Grid#PageRoot > SemanticZoom#TopLevelRoot > Grid > ScrollViewer#ScrollViewer > ScrollContentPresenter#ScrollContentPresenter > Grid > ContentPresenter#ZoomedInPresenter > GridView#AllAppsGrid > Border > Grid#SideBySidePinnedWrapper > ScrollViewer#SideBySidePinnedScrollViewer > Border#Root > Grid > ScrollContentPresenter#ScrollContentPresenter > Grid#SideBySidePinnedContent > Grid#TopLevelSuggestionsRoot > Grid#TopLevelSuggestionsContainerParent > Grid#TopLevelSuggestionsContainer > StartMenu.RecommendedReactNativeViewControl > ContentPresenter > ExperienceExtensions.ReactNativeExperienceViewControl > Grid#RootViewContainer > Microsoft.ReactNative.ReactRootView > Microsoft.ReactNative.ViewPanel > Microsoft.ReactNative.ViewPanel > GridView#RecommendedList", RegistryValueKind.String);
			RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Windhawk\Engine\Mods\windows-11-start-menu-styler\Settings", "controlStyles[0].styles[0]", "Height=170", RegistryValueKind.String);
			RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Start", "AllAppsViewMode", 2, RegistryValueKind.DWord);
			RegistryHelper.DeleteKey(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\FeatureManagement\Overrides\8\3036241548");

			if (Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Windhawk\Engine\Mods\windows-11-start-menu-styler", "Version", null) as string != "1.6")
			{
				var restartDialog = new ContentDialog
				{
					Title = "Update Windhawk Start Menu Styler Mod",
					Content = "Open Windhawk and update the Start Menu Styler Mod, then click restart.",
					PrimaryButtonText = "Restart",
					DefaultButton = ContentDialogButton.Primary,
					XamlRoot = XamlRoot
				};

				if (await restartDialog.ShowAsync() == ContentDialogResult.Primary)
				{
					ProcessStartInfo processStartInfo = new()
					{
						FileName = "cmd.exe",
						Arguments = $"/c shutdown /r /t 0",
						UseShellExecute = false,
						CreateNoWindow = true,
					};
					Process.Start(processStartInfo);
				}
				return;
			}
		}

		// enable "low latency profile"
		if (ubr >= 8524 && Convert.ToInt32(Registry.GetValue(@"HKEY_LOCAL_MACHINE\System\CurrentControlSet\Control\FeatureManagement\Overrides\8\3650112648", "EnabledState", 0)) != 2)
		{
			RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\System\CurrentControlSet\Control\FeatureManagement\Overrides\8\3650112648", "EnabledState", 2, RegistryValueKind.DWord);
			RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\System\CurrentControlSet\Control\FeatureManagement\Overrides\8\3650112648", "EnabledStateOptions", 0, RegistryValueKind.DWord);
			RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\System\CurrentControlSet\Control\FeatureManagement\Overrides\8\3650112648", "Variant", 0, RegistryValueKind.DWord);
			RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\System\CurrentControlSet\Control\FeatureManagement\Overrides\8\3650112648", "VariantPayload", 0, RegistryValueKind.DWord);
			RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\System\CurrentControlSet\Control\FeatureManagement\Overrides\8\3650112648", "VariantPayloadKind", 0, RegistryValueKind.DWord);
		}

		if (ubr >= 8875 && Convert.ToInt32(Registry.GetValue(@"HKEY_LOCAL_MACHINE\System\CurrentControlSet\Control\FeatureManagement\Overrides\8\4066113166", "EnabledState", 0)) != 2)
		{
			RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\System\CurrentControlSet\Control\FeatureManagement\Overrides\8\4066113166", "EnabledState", 2, RegistryValueKind.DWord);
			RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\System\CurrentControlSet\Control\FeatureManagement\Overrides\8\4066113166", "EnabledStateOptions", 0, RegistryValueKind.DWord);
			RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\System\CurrentControlSet\Control\FeatureManagement\Overrides\8\4066113166", "Variant", 0, RegistryValueKind.DWord);
			RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\System\CurrentControlSet\Control\FeatureManagement\Overrides\8\4066113166", "VariantPayload", 0, RegistryValueKind.DWord);
			RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\System\CurrentControlSet\Control\FeatureManagement\Overrides\8\4066113166", "VariantPayloadKind", 0, RegistryValueKind.DWord);
		}

		if (Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Windhawk\Engine\Mods\auto-theme-switcher", "Version", null) as string != "1.3.2" && Convert.ToInt32(Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Windhawk\Engine\Mods\auto-theme-switcher", "Disabled", 0)) != 1)
		{
			var restartDialog = new ContentDialog
			{
				Title = "Update Windhawk Auto Theme Switcher Mod",
				Content = "Open Windhawk and update the Auto Theme Switcher Mod.",
				PrimaryButtonText = "Done",
				DefaultButton = ContentDialogButton.Primary,
				XamlRoot = XamlRoot
			};
		}

		Version currentVersion = new(ProcessInfoHelper.Version);

		localSettings.Values.TryGetValue("Version", out object? storedVersionObj);
		Version storedVersion = storedVersionObj is string storedVersionStr ? new(storedVersionStr) : null;

		if (currentVersion.CompareTo(storedVersion) > 0)
		{
			try
			{
				using var doc = JsonDocument.Parse(await httpClient.GetStringAsync($"https://api.github.com/repos/tinodin/AutoOS/releases/tags/v{currentVersion}"));

				if (doc.RootElement.TryGetProperty("body", out JsonElement body))
				{
					string rawChangelog = body.GetString();
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
					contentDialog.Resources["ContentDialogMaxHeight"] = 1000;

					await contentDialog.ShowAsync();
				}
			}
			catch
			{ }

			var updateDialog = new UpdateDialog();
			List<(string Title, Func<Task> Action, Func<bool> Condition)> actions = UpdateStage.UpdateActions(updateDialog);

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
					string tag = release.GetProperty("tag_name").GetString();
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
				.GetString();

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
