using Microsoft.UI.Xaml;

namespace AutoOS.App.UserControls;

public partial class ViewDisplayedBehavior : OnScreenBehaviorBase
{
	public static readonly DependencyProperty ViewDisplayedActionProperty =
		DependencyProperty.Register(nameof(ViewDisplayedAction), typeof(Action), typeof(ViewDisplayedBehavior), new PropertyMetadata(null, OnViewDisplayedActionChanged));

	public Action ViewDisplayedAction
	{
		get => (Action)GetValue(ViewDisplayedActionProperty);
		set => SetValue(ViewDisplayedActionProperty, value);
	}

	private static void OnViewDisplayedActionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		((ViewDisplayedBehavior)d).RestartTracking();
	}

	protected override void OnIsOnScreenChanged(bool isOnScreen)
	{
		if (!isOnScreen)
		{
			return;
		}

		if (AssociatedObject is not { } element || !element.IsLoaded || element.Visibility != Visibility.Visible)
		{
			return;
		}

		ViewDisplayedAction?.Invoke();
		StopTracking();
	}
}
