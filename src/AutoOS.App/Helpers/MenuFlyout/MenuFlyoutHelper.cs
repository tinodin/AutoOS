using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace AutoOS.App.Helpers.MenuFlyout;

public static class MenuFlyoutHelper
{
	public static readonly DependencyProperty KeepOpenOnItemClickProperty =
		DependencyProperty.RegisterAttached(
			"KeepOpenOnItemClick",
			typeof(bool),
			typeof(MenuFlyoutHelper),
			new PropertyMetadata(false, OnKeepOpenOnItemClickChanged));

	public static bool GetKeepOpenOnItemClick(DependencyObject obj)
		=> (bool)obj.GetValue(KeepOpenOnItemClickProperty);

	public static void SetKeepOpenOnItemClick(DependencyObject obj, bool value)
		=> obj.SetValue(KeepOpenOnItemClickProperty, value);

	private static void OnKeepOpenOnItemClickChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is not DropDownButton button || e.NewValue is not true)
			return;

		button.Loaded += OnButtonLoaded;
	}

	private static void OnButtonLoaded(object sender, RoutedEventArgs e)
	{
		var button = (DropDownButton)sender;
		button.Loaded -= OnButtonLoaded;

		if (button.Flyout is not Microsoft.UI.Xaml.Controls.MenuFlyout flyout)
			return;

		bool keepOpen = false;
		foreach (MenuFlyoutItem item in flyout.Items.OfType<MenuFlyoutItem>())
		{
			if (item is ToggleMenuFlyoutItem or RadioMenuFlyoutItem)
				item.Click += (_, _) => keepOpen = true;
		}

		flyout.Closing += (_, args) =>
		{
			if (!keepOpen)
				return;

			keepOpen = false;
			args.Cancel = true;
		};
	}
}
