using AutoOS.Core.Helpers.Picker;
using AutoOS.Core.Helpers.Registry;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.Win32;
using System.IO;
using Windows.Storage;
using Windows.System;

namespace AutoOS.Views.Settings;

public sealed partial class PersonalizationPage : Page
{
	private const string ModKey = @"HKEY_LOCAL_MACHINE\SOFTWARE\Windhawk\Engine\Mods\auto-theme-switcher";
	private const string SettingsKey = @"HKEY_LOCAL_MACHINE\SOFTWARE\Windhawk\Engine\Mods\auto-theme-switcher\Settings";
	private const ulong MaxWallpaperSizeBytes = 20 * 1024 * 1024;

	private bool isInitializing = true;
	private string hotkeyString = string.Empty;
	private CancellationTokenSource? autoSaveCts;

	public PersonalizationPage()
	{
		InitializeComponent();
		LoadSettings();
		isInitializing = false;
	}

	private void LoadSettings()
	{
		// Determine whether the mod is installed at all.
		if (Registry.GetValue(ModKey, "Version", null) == null)
		{
			ShowInfo("The Windhawk \"Auto Theme Switcher\" mod is not installed. Settings will be saved but have no effect until the mod is installed.", InfoBarSeverity.Warning);
		}

		bool disabled = Convert.ToInt32(Registry.GetValue(ModKey, "Disabled", 0)) == 1;
		string scheduleMode = Registry.GetValue(SettingsKey, "ScheduleMode", "Custom") as string ?? "Custom";
		string switchMode = Registry.GetValue(SettingsKey, "switchMode", "Appearance") as string ?? "Appearance";

		// Scheduling mode.
		string selectedSchedule;
		if (disabled)
			selectedSchedule = "Disabled";
		else
			selectedSchedule = scheduleMode switch
			{
				"Custom" => "Custom",
				"LocationService" => "LocationService",
				"CustomCoordinates" => "CustomCoordinates",
				"Hotkey" => "Hotkey",
				_ => "Custom"
			};
		ScheduleMode.SelectedIndex = selectedSchedule switch
		{
			"Disabled" => 0,
			"Custom" => 1,
			"LocationService" => 2,
			"CustomCoordinates" => 3,
			"Hotkey" => 4,
			_ => 1
		};

		// Custom times.
		LightTime.Time = TryParseTime(Registry.GetValue(SettingsKey, "CustomLight", "07:00") as string) ?? TimeSpan.FromHours(7);
		DarkTime.Time = TryParseTime(Registry.GetValue(SettingsKey, "CustomDark", "19:00") as string) ?? TimeSpan.FromHours(19);

		// Custom coordinates.
		if (double.TryParse(Registry.GetValue(SettingsKey, "Latitude", "0") as string, System.Globalization.CultureInfo.InvariantCulture, out var latitude))
			Latitude.Value = latitude;
		else
			Latitude.Value = 0;

		if (double.TryParse(Registry.GetValue(SettingsKey, "Longitude", "0") as string, System.Globalization.CultureInfo.InvariantCulture, out var longitude))
			Longitude.Value = longitude;
		else
			Longitude.Value = 0;

		// Switching mode.
		SwitchMode.SelectedIndex = switchMode switch
		{
			"Appearance" => 0,
			"Wallpaper" => 1,
			"Theme" => 2,
			_ => 0
		};

		LightWallpaper.Text = Registry.GetValue(SettingsKey, "LightWallpaperPath", "") as string ?? string.Empty;
		DarkWallpaper.Text = Registry.GetValue(SettingsKey, "DarkWallpaperPath", "") as string ?? string.Empty;
		LightTheme.Text = Registry.GetValue(SettingsKey, "LightThemePath", "") as string ?? string.Empty;
		DarkTheme.Text = Registry.GetValue(SettingsKey, "DarkThemePath", "") as string ?? string.Empty;
		ScriptPath.Text = Registry.GetValue(SettingsKey, "ScriptPath", "") as string ?? string.Empty;
		LockScreen.IsOn = Convert.ToInt32(Registry.GetValue(SettingsKey, "LockScreen", 1)) == 1;

		// Hotkey.
		hotkeyString = Registry.GetValue(SettingsKey, "Hotkey", "") as string ?? string.Empty;
		HotkeyShortcut.Keys = ParseHotkey(hotkeyString);

		UpdateScheduleVisibility(selectedSchedule);
		UpdateSwitchVisibility(switchMode);
		_ = UpdateTimelineAsync(selectedSchedule);
		_ = UpdatePreviewAsync();
	}

	private static TimeSpan? TryParseTime(object value)
	{
		if (value is string s && TimeSpan.TryParse(s, out var time))
			return time;
		return null;
	}

	private void ScheduleMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (isInitializing) return;

		string selected = (ScheduleMode.SelectedItem as ComboBoxItem)?.Tag as string ?? "Custom";
		UpdateScheduleVisibility(selected);
		_ = UpdateTimelineAsync(selected);
		ScheduleAutoSave();
		_ = UpdatePreviewAsync();
	}

	private void SwitchMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (isInitializing) return;

		string selected = (SwitchMode.SelectedItem as ComboBoxItem)?.Tag as string ?? "Appearance";
		UpdateSwitchVisibility(selected);
		ScheduleAutoSave();
		_ = UpdatePreviewAsync();
	}

	private void UpdateScheduleVisibility(string mode)
	{
		LightTimeCard.Visibility = mode == "Custom" ? Visibility.Visible : Visibility.Collapsed;
		DarkTimeCard.Visibility = mode == "Custom" ? Visibility.Visible : Visibility.Collapsed;
		LatitudeCard.Visibility = mode == "CustomCoordinates" ? Visibility.Visible : Visibility.Collapsed;
		LongitudeCard.Visibility = mode == "CustomCoordinates" ? Visibility.Visible : Visibility.Collapsed;
		TimelineCard.Visibility = mode is "Custom" or "LocationService" or "CustomCoordinates" ? Visibility.Visible : Visibility.Collapsed;
	}

	private void UpdateSwitchVisibility(string switchMode)
	{
		LightWallpaperCard.Visibility = switchMode == "Wallpaper" ? Visibility.Visible : Visibility.Collapsed;
		DarkWallpaperCard.Visibility = switchMode == "Wallpaper" ? Visibility.Visible : Visibility.Collapsed;
		LightThemeCard.Visibility = switchMode == "Theme" ? Visibility.Visible : Visibility.Collapsed;
		DarkThemeCard.Visibility = switchMode == "Theme" ? Visibility.Visible : Visibility.Collapsed;
	}

	private async Task UpdateTimelineAsync(string mode)
	{
		switch (mode)
		{
			case "Custom":
				TimeLine.Sunrise = null;
				TimeLine.Sunset = null;
				TimeLine.StartTime = LightTime.Time;
				TimeLine.EndTime = DarkTime.Time;
				break;

			case "CustomCoordinates":
				ApplySunTimes(Latitude.Value, Longitude.Value);
				break;

			case "LocationService":
				try
				{
					var pos = await LocationHelper.GetGeoLocationAsync();
					ApplySunTimes(pos.Coordinate.Point.Position.Latitude, pos.Coordinate.Point.Position.Longitude);
				}
				catch
				{
					TimeLine.Sunrise = null;
					TimeLine.Sunset = null;
					TimeLine.StartTime = TimeSpan.FromHours(7);
					TimeLine.EndTime = TimeSpan.FromHours(19);
					ShowInfo("Location service is unavailable. The timeline cannot show sunrise/sunset times.", InfoBarSeverity.Warning);
				}
				break;
		}
	}

	private void ApplySunTimes(double latitude, double longitude)
	{
		if (double.IsNaN(latitude) || double.IsNaN(longitude)) return;

		var sunTimes = SunTimesHelper.CalculateSunriseSunset(latitude, longitude, DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day);

		TimeLine.Sunrise = sunTimes.HasSunrise ? new TimeSpan(sunTimes.SunriseHour, sunTimes.SunriseMinute, 0) : null;
		TimeLine.Sunset = sunTimes.HasSunset ? new TimeSpan(sunTimes.SunsetHour, sunTimes.SunsetMinute, 0) : null;

		// Light mode is active from sunrise until sunset.
		TimeLine.StartTime = TimeLine.Sunrise ?? TimeSpan.FromHours(7);
		TimeLine.EndTime = TimeLine.Sunset ?? TimeSpan.FromHours(19);
	}

	private void LightMode_TimeChanged(object sender, TimePickerValueChangedEventArgs e)
	{
		if (isInitializing) return;
		TimeLine.StartTime = e.NewTime;
		ScheduleAutoSave();
	}

	private void DarkMode_TimeChanged(object sender, TimePickerValueChangedEventArgs e)
	{
		if (isInitializing) return;
		TimeLine.EndTime = e.NewTime;
		ScheduleAutoSave();
	}

	private void Coordinate_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.NumberBoxValueChangedEventArgs e)
	{
		if (isInitializing) return;
		if (double.IsNaN(Latitude.Value) || double.IsNaN(Longitude.Value)) return;
		ApplySunTimes(Latitude.Value, Longitude.Value);
		ScheduleAutoSave();
	}

	private async void BrowseLightWallpaper_Click(object sender, RoutedEventArgs e)
		=> await PickImageAsync(LightWallpaper);

	private async void BrowseDarkWallpaper_Click(object sender, RoutedEventArgs e)
		=> await PickImageAsync(DarkWallpaper);

	private async void BrowseLightTheme_Click(object sender, RoutedEventArgs e)
		=> await PickThemeAsync(LightTheme);

	private async void BrowseDarkTheme_Click(object sender, RoutedEventArgs e)
		=> await PickThemeAsync(DarkTheme);

	private async void BrowseScript_Click(object sender, RoutedEventArgs e)
	{
		var picker = new FilePicker(App.MainWindow)
		{
			ShowAllFilesOption = true
		};
		picker.FileTypeChoices.Add("Scripts", ["*.ps1", "*.bat", "*.cmd", "*.exe"]);

		var file = await picker.PickSingleFileAsync();
		if (file != null)
		{
			ScriptPath.Text = file.Path;
		}
	}

	private async Task PickImageAsync(Microsoft.UI.Xaml.Controls.TextBox target)
	{
		var picker = new FilePicker(App.MainWindow)
		{
			ShowAllFilesOption = false
		};
		picker.FileTypeChoices.Add("Image", ["*.jpg", "*.jpeg", "*.png", "*.bmp"]);

		var file = await picker.PickSingleFileAsync();
		if (file == null) return;

		var properties = await file.GetBasicPropertiesAsync();
		if (properties.Size > MaxWallpaperSizeBytes)
		{
			ShowInfo($"The selected image is {properties.Size / (1024.0 * 1024.0):0.##} MB. Windows does not support wallpapers larger than 20 MB.", InfoBarSeverity.Error);
			return;
		}

		target.Text = file.Path;
	}

	private async Task PickThemeAsync(Microsoft.UI.Xaml.Controls.TextBox target)
	{
		var picker = new FilePicker(App.MainWindow)
		{
			ShowAllFilesOption = false
		};
		picker.FileTypeChoices.Add("Theme", ["*.theme"]);

		var file = await picker.PickSingleFileAsync();
		if (file != null)
		{
			target.Text = file.Path;
		}
	}

	private void HotkeyShortcut_PrimaryButtonClick(object sender, ContentDialogButtonClickEventArgs e)
	{
		HotkeyShortcut.UpdatePreviewKeys();
		HotkeyShortcut.CloseContentDialog();

		hotkeyString = BuildHotkey(HotkeyShortcut.Keys);
		ScheduleAutoSave();
	}

	private static List<object> ParseHotkey(string value)
	{
		var keys = new List<object>();
		if (string.IsNullOrWhiteSpace(value)) return keys;

		foreach (var part in value.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
		{
			keys.Add(part);
		}

		return keys;
	}

	private static string BuildHotkey(IEnumerable<object> keys)
	{
		if (keys == null) return string.Empty;

		var modifiers = new List<string>();
		string? key = null;

		foreach (var keyItem in keys)
		{
			string keyName;
			VirtualKey? virtKey = null;

			if (keyItem is KeyVisualInfo info)
			{
				keyName = info.KeyName ?? string.Empty;
				virtKey = info.Key;
			}
			else
			{
				keyName = keyItem?.ToString() ?? string.Empty;
			}

			if (keyName.Contains("Ctrl", StringComparison.OrdinalIgnoreCase))
				modifiers.Add("Ctrl");
			else if (keyName.Contains("Shift", StringComparison.OrdinalIgnoreCase))
				modifiers.Add("Shift");
			else if (keyName.Contains("Alt", StringComparison.OrdinalIgnoreCase))
				modifiers.Add("Alt");
			else if (keyName.Contains("Win", StringComparison.OrdinalIgnoreCase))
				modifiers.Add("Win");
			else if (virtKey.HasValue && virtKey.Value != VirtualKey.None)
				key = MapKeyName(keyName);
			else if (!string.IsNullOrWhiteSpace(keyName))
				key = keyName;
		}

		if (string.IsNullOrWhiteSpace(key)) return string.Empty;

		return string.Join("+", modifiers.Concat([key]));
	}

	private static string MapKeyName(string keyName)
	{
		return keyName switch
		{
			"Escape" => "Escape",
			"PageUp" => "PageUp",
			"PageDown" => "PageDown",
			"PrintScreen" => "PrintScreen",
			"Number0" => "0",
			"Number1" => "1",
			"Number2" => "2",
			"Number3" => "3",
			"Number4" => "4",
			"Number5" => "5",
			"Number6" => "6",
			"Number7" => "7",
			"Number8" => "8",
			"Number9" => "9",
			"NumberPad0" => "Numpad0",
			"NumberPad1" => "Numpad1",
			"NumberPad2" => "Numpad2",
			"NumberPad3" => "Numpad3",
			"NumberPad4" => "Numpad4",
			"NumberPad5" => "Numpad5",
			"NumberPad6" => "Numpad6",
			"NumberPad7" => "Numpad7",
			"NumberPad8" => "Numpad8",
			"NumberPad9" => "Numpad9",
			"CapitalLock" => "CapsLock",
			"NumberKeyLock" => "NumLock",
			"Decimal" => "Decimal",
			"Scroll" => "ScrollLock",
			"VolumeMute" => "VolumeMute",
			"VolumeUp" => "VolumeUp",
			"VolumeDown" => "VolumeDown",
			"MediaPlayPause" => "MediaPlayPause",
			"MediaNext" => "MediaNext",
			"MediaPrevious" => "MediaPrev",
			"MediaStop" => "MediaStop",
			"Back" => "Backspace",
			"Delete" => "Delete",
			"Insert" => "Insert",
			_ => keyName
		};
	}

	private void ScheduleAutoSave()
	{
		autoSaveCts?.Cancel();
		autoSaveCts = new CancellationTokenSource();
		_ = AutoSaveAsync(autoSaveCts.Token);
	}

	private async Task AutoSaveAsync(CancellationToken token)
	{
		try
		{
			await Task.Delay(500, token);
		}
		catch (OperationCanceledException)
		{
			return;
		}

		if (token.IsCancellationRequested) return;
		SaveSettings();
	}

	private void SaveSettings()
	{
		try
		{
			string scheduleMode = (ScheduleMode.SelectedItem as ComboBoxItem)?.Tag as string ?? "Custom";
			string switchMode = (SwitchMode.SelectedItem as ComboBoxItem)?.Tag as string ?? "Appearance";

			bool disabled = scheduleMode == "Disabled";
			string effectiveSchedule = disabled ? "Custom" : scheduleMode;

			// Enable/disable the mod itself.
			RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, ModKey, "Disabled", disabled ? 1 : 0, RegistryValueKind.DWord);

			RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, SettingsKey, "ScheduleMode", effectiveSchedule, RegistryValueKind.String);
			RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, SettingsKey, "CustomLight", LightTime.Time.ToString(@"hh\:mm"), RegistryValueKind.String);
			RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, SettingsKey, "CustomDark", DarkTime.Time.ToString(@"hh\:mm"), RegistryValueKind.String);
			RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, SettingsKey, "Latitude", Latitude.Value.ToString(System.Globalization.CultureInfo.InvariantCulture), RegistryValueKind.String);
			RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, SettingsKey, "Longitude", Longitude.Value.ToString(System.Globalization.CultureInfo.InvariantCulture), RegistryValueKind.String);
			RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, SettingsKey, "switchMode", switchMode, RegistryValueKind.String);
			RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, SettingsKey, "LightWallpaperPath", LightWallpaper.Text, RegistryValueKind.String);
			RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, SettingsKey, "DarkWallpaperPath", DarkWallpaper.Text, RegistryValueKind.String);
			RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, SettingsKey, "LightThemePath", LightTheme.Text, RegistryValueKind.String);
			RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, SettingsKey, "DarkThemePath", DarkTheme.Text, RegistryValueKind.String);
			RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, SettingsKey, "ScriptPath", ScriptPath.Text, RegistryValueKind.String);
			RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, SettingsKey, "Hotkey", hotkeyString, RegistryValueKind.String);
			RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, SettingsKey, "LockScreen", LockScreen.IsOn ? 1 : 0, RegistryValueKind.DWord);

			// Notify the Windhawk engine that the settings have changed.
			RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, SettingsKey, "SettingsChangeTime", unchecked((int)DateTimeOffset.UtcNow.ToUnixTimeSeconds()), RegistryValueKind.DWord);
		}
		catch (Exception ex)
		{
			ShowInfo($"Failed to save settings: {ex.Message}", InfoBarSeverity.Error);
		}
	}

	private void PathBox_TextChanged(object sender, TextChangedEventArgs e)
	{
		if (isInitializing) return;
		ScheduleAutoSave();
		_ = UpdatePreviewAsync();
	}

	private void LockScreen_Toggled(object sender, RoutedEventArgs e)
	{
		if (isInitializing) return;
		ScheduleAutoSave();
	}

	private async Task UpdatePreviewAsync()
	{
		await LoadWallpaperAsync(PreviewLightBrush, LightWallpaper.Text);
		await LoadWallpaperAsync(PreviewDarkBrush, DarkWallpaper.Text);
	}

	private static async Task LoadWallpaperAsync(ImageBrush brush, string path)
	{
		try
		{
			if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
			{
				brush.ImageSource = null;
				return;
			}

			var file = await StorageFile.GetFileFromPathAsync(path);
			using var stream = await file.OpenAsync(FileAccessMode.Read);
			var bitmap = new BitmapImage();
			await bitmap.SetSourceAsync(stream);
			brush.ImageSource = bitmap;
		}
		catch
		{
			brush.ImageSource = null;
		}
	}

	private void ShowInfo(string message, InfoBarSeverity severity)
	{
		StatusInfo.Message = message;
		StatusInfo.Severity = severity;
		StatusInfo.IsOpen = true;
	}
}
