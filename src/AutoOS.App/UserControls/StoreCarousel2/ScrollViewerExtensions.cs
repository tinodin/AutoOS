using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace AutoOS.App.UserControls;

public static class ScrollViewerExtensions
{
	public static readonly DependencyProperty AutoResetOffsetProperty =
		DependencyProperty.RegisterAttached(
			"AutoResetOffset",
			typeof(bool),
			typeof(ScrollViewerExtensions),
			new PropertyMetadata(false, OnAutoResetOffsetChanged));

	public static bool GetAutoResetOffset(DependencyObject obj) => (bool)obj.GetValue(AutoResetOffsetProperty);
	public static void SetAutoResetOffset(DependencyObject obj, bool value) => obj.SetValue(AutoResetOffsetProperty, value);

	private static void OnAutoResetOffsetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is FrameworkElement fe)
		{
			if ((bool)e.NewValue)
			{
				fe.Loaded += OnTargetLoaded;
				if (fe.IsLoaded)
					ResetOffset(fe);
			}
			else
			{
				fe.Loaded -= OnTargetLoaded;
			}
		}
		else if (d is ScrollViewer sv && (bool)e.NewValue)
		{
			sv.Loaded += (s, _) => sv.ChangeView(0, null, null, true);
			if (sv.IsLoaded)
				sv.ChangeView(0, null, null, true);
		}
	}

	private static void OnTargetLoaded(object sender, RoutedEventArgs e)
	{
		if (sender is DependencyObject d)
			ResetOffset(d);
	}

	private static void ResetOffset(DependencyObject root)
	{
		if (root is ScrollViewer sv)
		{
			sv.ChangeView(0, null, null, true);
			return;
		}

		ScrollViewer? inner = root.FindDescendant<ScrollViewer>();
		inner?.ChangeView(0, null, null, true);
	}
}
