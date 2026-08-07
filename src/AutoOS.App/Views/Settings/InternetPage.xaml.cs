using System.Collections.ObjectModel;
using AutoOS.Core.Helpers.Device;
using AutoOS.Core.Helpers.Device.Models;
using AutoOS.Core.Helpers.Network.Models;
using Microsoft.UI.Xaml.Media;
using Microsoft.Win32;
using Windows.Win32;

namespace AutoOS.App.Views.Settings;

public sealed partial class InternetPage : Page
{
	private bool isInitializingAdvancedNetworkSettings = true;
	private readonly Dictionary<DeviceInfo, Dictionary<string, (string Value, string DisplayValue)>> _pendingChanges = [];
	public ObservableCollection<DeviceInfo> NetworkAdapters { get; } = [];

	public InternetPage()
	{
		InitializeComponent();
		GetNetworkAdapters();
		Loaded += InternetPage_Loaded;
	}

	private void InternetPage_Loaded(object sender, RoutedEventArgs e)
	{
		isInitializingAdvancedNetworkSettings = false;
	}

	private void GetNetworkAdapters()
	{
		NetworkAdapters.Clear();
		foreach (DeviceInfo device in DeviceHelper.GetDevices(DeviceType.NIC))
		{
			if (device.NicType == NicDeviceType.WiFi || device.NicType == NicDeviceType.LAN)
			{
				device.AdvancedSettings = Core.Helpers.Network.NetworkHelper.GetAdvancedSettings(device);
				NetworkAdapters.Add(device);
			}
		}
	}

	private void SettingsGroup_Loaded(object sender, RoutedEventArgs e)
	{
		if (sender is not SettingsGroup settingsGroup) return;
		if (settingsGroup.DataContext is not DeviceInfo device) return;
		List<NetworkAdvancedSetting> settings = device.AdvancedSettings;
		settingsGroup.Description = $"Current version: {device.DriverType} {device.CurrentVersion}";

		foreach (NetworkAdvancedSetting setting in settings.OrderBy(s => string.IsNullOrEmpty(s.Name) || !char.IsDigit(s.Name[0])).ThenBy(s => s.Name, Comparer<string>.Create(NaturalSort)))
		{
			FrameworkElement control = setting.Type switch
			{
				NetworkSettingType.Dword or NetworkSettingType.Int => CreateNumberBox(setting),
				NetworkSettingType.Edit => CreateTextBox(setting),
				_ => CreateComboBox(setting)
			};

			var settingsCard = new SettingsCard
			{
				Header = setting.Name,
				Description = setting.Key,
				Content = control
			};

			settingsGroup.Items.Add(settingsCard);
		}
	}

	private ComboBox CreateComboBox(NetworkAdvancedSetting setting)
	{
		var sortedOptions = setting.Options.OrderBy(opt => opt.Name, Comparer<string>.Create(NaturalSort)).ToList();

		int selectedIndex = sortedOptions.FindIndex(opt => string.Equals(opt.Value, setting.CurrentValue, StringComparison.OrdinalIgnoreCase));
		if (selectedIndex < 0)
			selectedIndex = sortedOptions.FindIndex(opt => string.Equals(opt.Value, setting.DefaultValue, StringComparison.OrdinalIgnoreCase));
		if (selectedIndex < 0) selectedIndex = 0;

		var comboBox = new ComboBox
		{
			MinWidth = 300,
			DisplayMemberPath = "Name",
			ItemsSource = sortedOptions,
			SelectedIndex = selectedIndex,
			Tag = setting
		};
		comboBox.SelectionChanged += AdvancedSetting_SelectionChanged;
		return comboBox;
	}

	private NumberBox CreateNumberBox(NetworkAdvancedSetting setting)
	{
		var numberBox = new NumberBox
		{
			MinWidth = 300,
			SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Inline,
			Tag = setting
		};

		string currentValue = setting.CurrentValue;
		if (setting.Base == 16 && currentValue.StartsWith("0x"))
			currentValue = currentValue[2..];

		if (int.TryParse(currentValue, setting.Base == 16 ? System.Globalization.NumberStyles.HexNumber : System.Globalization.NumberStyles.Integer, null, out int value))
			numberBox.Value = value;

		if (setting.Min.HasValue)
			numberBox.Minimum = setting.Min.Value;
		if (setting.Max.HasValue)
			numberBox.Maximum = setting.Max.Value;
		if (setting.Step.HasValue)
			numberBox.SmallChange = setting.Step.Value;

		numberBox.ValueChanged += AdvancedSetting_ValueChanged;
		return numberBox;
	}

	private Microsoft.UI.Xaml.Controls.TextBox CreateTextBox(NetworkAdvancedSetting setting)
	{
		var textBox = new Microsoft.UI.Xaml.Controls.TextBox
		{
			MinWidth = 300,
			Text = setting.CurrentValue,
			Tag = setting
		};

		if (setting.LimitText.HasValue)
			textBox.MaxLength = setting.LimitText.Value;

		if (setting.UpperCase)
		{
			textBox.CharacterCasing = CharacterCasing.Upper;
		}

		textBox.LostFocus += AdvancedSetting_TextChanged;
		return textBox;
	}

	private void AdvancedSetting_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (isInitializingAdvancedNetworkSettings) return;

		var comboBox = (ComboBox)sender;
		if (comboBox.SelectedItem is not NetworkSettingOption selectedOption || comboBox.Tag is not NetworkAdvancedSetting setting) return;

		ChangeSetting(comboBox, setting, selectedOption.Value, selectedOption.Name);
	}

	private void AdvancedSetting_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
	{
		if (isInitializingAdvancedNetworkSettings) return;

		if (sender.Tag is not NetworkAdvancedSetting setting) return;

		string value = setting.Base == 16 ? $"0x{((int)sender.Value):X}" : ((int)sender.Value).ToString();
		ChangeSetting(sender, setting, value, value);
	}

	private void AdvancedSetting_TextChanged(object sender, RoutedEventArgs e)
	{
		if (isInitializingAdvancedNetworkSettings) return;

		var textBox = (Microsoft.UI.Xaml.Controls.TextBox)sender;
		if (textBox.Tag is not NetworkAdvancedSetting setting) return;

		string value = textBox.Text;
		ChangeSetting(textBox, setting, value, value);
	}

	private void ChangeSetting(FrameworkElement control, NetworkAdvancedSetting setting, string value, string displayValue)
	{
		SettingsGroup? settingsGroup = FindParent<SettingsGroup>(control);
		if (settingsGroup?.DataContext is not DeviceInfo device) return;

		if (!_pendingChanges.ContainsKey(device))
			_pendingChanges[device] = [];

		Dictionary<string, (string Value, string DisplayValue)> deviceChanges = _pendingChanges[device];

		if (setting.CurrentValue == value)
			deviceChanges.Remove(setting.Key);
		else
			deviceChanges[setting.Key] = (value, displayValue);

		StackPanel? repeaterItem = FindParent<StackPanel>(settingsGroup);
		if (repeaterItem == null) return;
		var infoBarContainer = (StackPanel)repeaterItem.FindName("AdapterInfo")!;
		if (infoBarContainer == null) return;

		if (!_pendingChanges.TryGetValue(device, out Dictionary<string, (string Value, string DisplayValue)>? changes) || changes == null || changes.Count == 0)
		{
			infoBarContainer.Children.Clear();
			return;
		}

		if (infoBarContainer.Children.Count > 0 && infoBarContainer.Children[0] is InfoBar existingBar && existingBar.Title == "Unsaved changes")
			return;

		var infoBar = new InfoBar
		{
			Title = "You have unsaved changes. Applying changes will restart your network adapter.",
			IsClosable = false,
			IsOpen = true,
			Severity = InfoBarSeverity.Informational,
			Margin = new Thickness(0, 0, 0, 12)
		};

		var stackPanel = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = 8,
			HorizontalAlignment = HorizontalAlignment.Right,
			Margin = new Thickness(0, -52, 16, 0)
		};

		var applyBtn = new Button { Content = "Apply", Style = (Style)Application.Current.Resources["AccentButtonStyle"]! };
		applyBtn.Click += async (s, e) =>
		{
			infoBar.Severity = InfoBarSeverity.Informational;
			infoBar.Title = "Applying changes...";
			infoBar.Message = string.Empty;
			infoBar.Content = null;

			bool success = await Task.Run(() =>
			{
				foreach (KeyValuePair<string, (string Value, string DisplayValue)> change in changes)
					Core.Helpers.Network.NetworkHelper.SetAdvancedSetting(device, change.Key, change.Value.Value);

				return DeviceHelper.RestartDevice(device);
			});

			_pendingChanges.Remove(device);
			UpdateSettings(settingsGroup, device);

			infoBar.Severity = success ? InfoBarSeverity.Success : InfoBarSeverity.Error;
			infoBar.Title = success ? "Successfully applied changes." : "Failed to apply changes.";
			infoBar.IsHitTestVisible = true;

			await Task.Delay(2000);
			infoBarContainer.Children.Clear();
		};

		var cancelBtn = new Button { Content = "Cancel" };
		cancelBtn.Click += (s, e) =>
		{
			_pendingChanges.Remove(device);
			UpdateSettings(settingsGroup, device);
			infoBarContainer.Children.Clear();
		};

		stackPanel.Children.Add(cancelBtn);
		stackPanel.Children.Add(applyBtn);

		infoBar.Content = stackPanel;

		infoBarContainer.Children.Clear();
		infoBarContainer.Children.Add(infoBar);
	}

	private async void Optimize_Checked(object sender, RoutedEventArgs e)
	{
		var button = (ProgressButton)sender;
		SettingsGroup? settingsGroup = FindParent<SettingsGroup>(button);

		if (settingsGroup?.DataContext is not DeviceInfo device) return;

		_pendingChanges.Remove(device);
		UpdateSettings(settingsGroup, device);

		StackPanel? repeaterItem = FindParent<StackPanel>(settingsGroup);
		if (repeaterItem != null)
		{
			var infoBarContainer = (StackPanel)repeaterItem.FindName("AdapterInfo")!;
			if (infoBarContainer != null)
				infoBarContainer.Children.Clear();
		}

		bool anyChanged = Core.Helpers.Network.NetworkHelper.OptimizeAdapter(device);

		if (anyChanged)
		{
			UpdateSettings(settingsGroup, device);
			await Task.Run(() => DeviceHelper.RestartDevice(device));
		}
		else
		{
			await Task.Delay(500);
		}

		button.IsChecked = false;
	}

	private void UpdateSettings(SettingsGroup settingsGroup, DeviceInfo device)
	{
		isInitializingAdvancedNetworkSettings = true;

		using RegistryKey? deviceKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(device.RegistryPath);

		foreach (object? item in settingsGroup.Items)
		{
			if (item is not SettingsCard card || card.Content is not FrameworkElement control) continue;
			if (control.Tag is not NetworkAdvancedSetting setting) continue;

			string newValue = deviceKey?.GetValue(setting.Key)?.ToString() ?? string.Empty;
			setting.CurrentValue = newValue;

			switch (control)
			{
				case ComboBox combobox:
					var options = (List<NetworkSettingOption>)combobox.ItemsSource!;
					int idx = options.FindIndex(opt => string.Equals(opt.Value, newValue, StringComparison.OrdinalIgnoreCase));
					if (idx < 0)
						idx = options.FindIndex(opt => string.Equals(opt.Value, setting.DefaultValue, StringComparison.OrdinalIgnoreCase));
					if (idx < 0) idx = 0;
					combobox.SelectedIndex = idx;
					break;

				case NumberBox numberbox:
					if (setting.Base == 16 && newValue.StartsWith("0x")) newValue = newValue[2..];
					if (int.TryParse(newValue, setting.Base == 16 ? System.Globalization.NumberStyles.HexNumber : System.Globalization.NumberStyles.Integer, null, out int val))
						numberbox.Value = val;
					break;

				case Microsoft.UI.Xaml.Controls.TextBox textbox:
					textbox.Text = newValue;
					break;
			}
		}

		isInitializingAdvancedNetworkSettings = false;
	}

	private static int NaturalSort(string x, string y)
	{
		if (x == y) return 0;
		if (x == null) return -1;
		if (y == null) return 1;

		bool xIsUsec = x.Contains("usec", StringComparison.OrdinalIgnoreCase);
		bool yIsUsec = y.Contains("usec", StringComparison.OrdinalIgnoreCase);
		bool xIsMsec = x.Contains("msec", StringComparison.OrdinalIgnoreCase);
		bool yIsMsec = y.Contains("msec", StringComparison.OrdinalIgnoreCase);

		if (xIsUsec && yIsMsec) return -1;
		if (yIsUsec && xIsMsec) return 1;

		bool xIsMbps = x.Contains("Mbps", StringComparison.OrdinalIgnoreCase);
		bool yIsMbps = y.Contains("Mbps", StringComparison.OrdinalIgnoreCase);
		bool xIsGbps = x.Contains("Gbps", StringComparison.OrdinalIgnoreCase);
		bool yIsGbps = y.Contains("Gbps", StringComparison.OrdinalIgnoreCase);

		if (xIsMbps && yIsGbps) return -1;
		if (yIsMbps && xIsGbps) return 1;

		string[] rates = ["Disabled", "Off", "Minimal", "Low", "Medium", "Middle", "High", "Extreme", "Adaptive"];
		int xInt = Array.FindIndex(rates, i => x.Equals(i, StringComparison.OrdinalIgnoreCase));
		int yInt = Array.FindIndex(rates, i => y.Equals(i, StringComparison.OrdinalIgnoreCase));
		if (xInt != -1 && yInt != -1) return xInt.CompareTo(yInt);

		return PInvoke.StrCmpLogical(x, y);
	}

	public static T? FindParent<T>(DependencyObject child) where T : DependencyObject
	{
		DependencyObject? parent = VisualTreeHelper.GetParent(child);

		while (parent != null && parent is not T)
			parent = VisualTreeHelper.GetParent(parent);

		return parent as T;
	}
}
