using AutoOS.Core.Helpers.Registry;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Win32;
//using Microsoft.Windows.ApplicationModel.WindowsAppRuntime;

namespace AutoOS.App.Views.Installer;

public sealed partial class HomePage : Page
{
	private static readonly HttpClient httpClient = new();
	//public string WASDKVersion { get; } = $"Windows App SDK {ReleaseInfo.Major}.{ReleaseInfo.Minor}";
	public HomePage()
	{
		InitializeComponent();
		Loaded += HomePage_Loaded;
	}

	private async void HomePage_Loaded(object sender, RoutedEventArgs e)
	{
#if !DEBUG
		using RegistryKey? key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");

		if (key?.GetValue("InstallDate") is int unixSeconds)
		{
			DateTime installDate = DateTimeOffset.FromUnixTimeSeconds(unixSeconds).LocalDateTime;
			if ((DateTime.Now - installDate).TotalDays > 2)
			{
				var dialog = new ContentDialog
				{
					Title = "Fresh Windows Required",
					Content = "AutoOS is only supported on fresh installations of Windows.\nPlease follow the installation guide on GitHub.",
					CloseButtonText = "OK",
					DefaultButton = ContentDialogButton.Close,
					XamlRoot = XamlRoot
				};
				await dialog.ShowAsync();
				Application.Current.Exit();
			}
		}

		(ushort major, ushort minor, ushort build, ushort ubr) = OSHelper.GetWindowsVersion();
		if (build < 26200 || (build == 26200 && ubr < 8737))
		{
			var dialog = new ContentDialog
			{
				Title = "Unsupported Windows Version",
				Content = $"AutoOS is only supported on new versions of Windows 11 25H2. \nPlease follow the installation guide on GitHub.",
				CloseButtonText = "OK",
				DefaultButton = ContentDialogButton.Close,
				XamlRoot = XamlRoot
			};
			await dialog.ShowAsync();
			Application.Current.Exit();
		}
			
#endif

		if (!(Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\ProfileList")?.GetSubKeyNames()?.Any(sid => string.Equals(Path.GetFileName(Registry.GetValue($@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\ProfileList\{sid}", "ProfileImagePath", null) as string), "AutoOS", StringComparison.OrdinalIgnoreCase)) == true))
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

		// enable app access to location
		await RegistryHelper.RunAs(RegistryHelper.Identity.TrustedInstaller, new ProcessStartInfo { FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "SystemSettingsAdminFlows.exe"), Arguments = "SetCamSystemGlobal location 1", CreateNoWindow = true });
		RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\AppPrivacy", "LetAppsAccessLocation", 1, RegistryValueKind.DWord);
	}
}
