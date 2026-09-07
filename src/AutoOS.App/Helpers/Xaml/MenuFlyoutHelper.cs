using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace AutoOS.App.Helpers.Xaml;

public static class MenuFlyoutHelper
{
	public static readonly DependencyProperty KeepOpenOnToggleClickProperty =
		DependencyProperty.RegisterAttached(
			"KeepOpenOnToggleClick",
			typeof(bool),
			typeof(MenuFlyoutHelper),
			new PropertyMetadata(false, OnKeepOpenOnToggleClickChanged));

	public static bool GetKeepOpenOnToggleClick(DependencyObject obj)
		=> (bool)obj.GetValue(KeepOpenOnToggleClickProperty);

	public static void SetKeepOpenOnToggleClick(DependencyObject obj, bool value)
		=> obj.SetValue(KeepOpenOnToggleClickProperty, value);

	private static void OnKeepOpenOnToggleClickChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is not DropDownButton button || e.NewValue is not true)
			return;

		button.Loaded += OnButtonLoaded;
	}

	private static void OnButtonLoaded(object sender, RoutedEventArgs e)
	{
		var button = (DropDownButton)sender;
		button.Loaded -= OnButtonLoaded;

		if (button.Flyout is not MenuFlyout flyout)
			return;

		bool keepOpen = false;
		foreach (ToggleMenuFlyoutItem toggle in flyout.Items.OfType<ToggleMenuFlyoutItem>())
			toggle.Click += (_, _) => keepOpen = true;

		flyout.Closing += (_, args) =>
		{
			if (!keepOpen)
				return;

			keepOpen = false;
			args.Cancel = true;
		};
	}
}
