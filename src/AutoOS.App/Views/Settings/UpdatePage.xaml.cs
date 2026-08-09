using Microsoft.Win32;

namespace AutoOS.App.Views.Settings;

public sealed partial class UpdatePage : Page
{
	private bool isInitializingWindowsUpdateState = true;
	private bool isInitializingTargetVersion = true;

	public UpdatePage()
	{
		InitializeComponent();
		GetWindowsUpdateState();
		GetTargetVersion();
	}

	private void GetWindowsUpdateState()
	{
		// check registry
		object? expiryValue = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\WindowsUpdate\UX\Settings", "PauseUpdatesExpiryTime", null);
		bool isPaused = expiryValue is string expiryString && DateTimeOffset.TryParse(expiryString, out DateTimeOffset expiryDate) && expiryDate > DateTimeOffset.UtcNow;
		WindowsUpdate.IsOn = !isPaused;

		isInitializingWindowsUpdateState = false;
	}

	private async void Update_Toggled(object sender, RoutedEventArgs e)
	{
		if (isInitializingWindowsUpdateState) return;

		// disable hittestvisible to avoid double-clicking
		WindowsUpdate.IsHitTestVisible = false;

		// remove infobar
		WindowsUpdateInfo.Children.Clear();

		// add infobar
		WindowsUpdateInfo.Children.Add(new InfoBar
		{
			Title = WindowsUpdate.IsOn ? "Enabling Windows Update..." : "Disabling Windows Update...",
			IsClosable = false,
			IsOpen = true,
			Severity = InfoBarSeverity.Informational,
			Margin = new Thickness(0, 0, 0, 12)
		});

		// toggle windows update
		if (WindowsUpdate.IsOn)
		{
			// delete registry keys
			RegistryKey? key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\WindowsUpdate\UX\Settings", true);
			key?.DeleteValue("PauseFeatureUpdatesStartTime", false);
			key?.DeleteValue("PauseFeatureUpdatesEndTime", false);
			key?.DeleteValue("PauseQualityUpdatesStartTime", false);
			key?.DeleteValue("PauseQualityUpdatesEndTime", false);
			key?.DeleteValue("PauseUpdatesStartTime", false);
			key?.DeleteValue("PauseUpdatesExpiryTime", false);
			key?.Close();

			// delay
			await Task.Delay(500);
		}
		else
		{
			// pause for 100 years
			string start = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssK");
			string end = DateTime.UtcNow.AddYears(100).ToString("yyyy-MM-ddTHH:mm:ssK");
			using RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\WindowsUpdate\UX\Settings");
			key.SetValue("PauseFeatureUpdatesStartTime", start, RegistryValueKind.String);
			key.SetValue("PauseFeatureUpdatesEndTime", end, RegistryValueKind.String);
			key.SetValue("PauseQualityUpdatesStartTime", start, RegistryValueKind.String);
			key.SetValue("PauseQualityUpdatesEndTime", end, RegistryValueKind.String);
			key.SetValue("PauseUpdatesStartTime", start, RegistryValueKind.String);
			key.SetValue("PauseUpdatesExpiryTime", end, RegistryValueKind.String);
		}

		// delay
		await Task.Delay(200);

		// re-enable hittestvisible
		WindowsUpdate.IsHitTestVisible = true;

		// remove infobar
		WindowsUpdateInfo.Children.Clear();

		// add infobar
		var infoBar = new InfoBar
		{
			Title = WindowsUpdate.IsOn ? "Successfully enabled Windows Update." : "Successfully disabled Windows Update.",
			IsClosable = false,
			IsOpen = true,
			Severity = InfoBarSeverity.Success,
			Margin = new Thickness(0, 0, 0, 12)
		};
		WindowsUpdateInfo.Children.Add(infoBar);

		// delay
		await Task.Delay(2000);

		// remove infobar
		WindowsUpdateInfo.Children.Clear();
	}

	private void GetTargetVersion()
	{
		TargetVersion.Items.Add(new ComboBoxItem { Content = "Default" });

		string? current = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion")?.GetValue("DisplayVersion") as string;

		if (!string.IsNullOrEmpty(current))
		{
			if (string.Compare("25H2", current, StringComparison.OrdinalIgnoreCase) >= 0)
				TargetVersion.Items.Add(new ComboBoxItem { Content = "25H2" });
		}

		string version = "Default";

		using RegistryKey? key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate", false);
		if (key?.GetValue("TargetReleaseVersion") is int trv && trv == 1)
			version = key.GetValue("TargetReleaseVersionInfo") as string ?? "Default";

		foreach (ComboBoxItem item in TargetVersion.Items.Cast<ComboBoxItem>())
		{
			if ((item.Content as string) == version)
			{
				TargetVersion.SelectedItem = item;
				break;
			}
		}

		isInitializingTargetVersion = false;
	}

	private void TargetVersion_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (isInitializingTargetVersion) return;

		if (TargetVersion.SelectedItem is ComboBoxItem selectedItem)
		{
			string version = selectedItem.Content.ToString()!;

			using RegistryKey? key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate", true);

			if (version == "Default")
			{
				key?.DeleteValue("TargetReleaseVersion", false);
				key?.DeleteValue("TargetReleaseVersionInfo", false);
			}
			else
			{
				key?.SetValue("TargetReleaseVersion", 1, RegistryValueKind.DWord);
				key?.SetValue("TargetReleaseVersionInfo", version, RegistryValueKind.String);
			}
		}
	}
}
