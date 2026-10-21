using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.Xaml.Interactivity;

namespace AutoOS.App.UserControls;

public sealed partial class ScrollButtonsHostBehavior : Behavior<Grid>
{
	private ScrollViewer? _scrollViewer;
	private bool _isPointerOver;

	public static readonly DependencyProperty IsEnabledProperty =
		DependencyProperty.Register(nameof(IsEnabled), typeof(bool), typeof(ScrollButtonsHostBehavior),
			new PropertyMetadata(true, OnIsEnabledChanged));

	public bool IsEnabled
	{
		get => (bool)GetValue(IsEnabledProperty);
		set => SetValue(IsEnabledProperty, value);
	}

	public static readonly DependencyProperty LeftScrollButtonProperty =
		DependencyProperty.Register(nameof(LeftScrollButton), typeof(Button), typeof(ScrollButtonsHostBehavior),
			new PropertyMetadata(null, OnScrollButtonChanged));

	public Button LeftScrollButton
	{
		get => (Button)GetValue(LeftScrollButtonProperty);
		set => SetValue(LeftScrollButtonProperty, value);
	}

	public static readonly DependencyProperty RightScrollButtonProperty =
		DependencyProperty.Register(nameof(RightScrollButton), typeof(Button), typeof(ScrollButtonsHostBehavior),
			new PropertyMetadata(null, OnScrollButtonChanged));

	public Button RightScrollButton
	{
		get => (Button)GetValue(RightScrollButtonProperty);
		set => SetValue(RightScrollButtonProperty, value);
	}

	protected override void OnAttached()
	{
		base.OnAttached();
		AssociatedObject.Loaded += OnLoaded;
		AssociatedObject.Unloaded += OnUnloaded;
		AssociatedObject.PointerEntered += OnPointerEntered;
		AssociatedObject.PointerExited += OnPointerExited;
		AssociatedObject.PointerCanceled += OnPointerCanceled;
	}

	protected override void OnDetaching()
	{
		base.OnDetaching();
		AssociatedObject.Loaded -= OnLoaded;
		AssociatedObject.Unloaded -= OnUnloaded;
		AssociatedObject.PointerEntered -= OnPointerEntered;
		AssociatedObject.PointerExited -= OnPointerExited;
		AssociatedObject.PointerCanceled -= OnPointerCanceled;
		DetachScrollViewer();
	}

	private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		((ScrollButtonsHostBehavior)d).UpdateButtons();
	}

	private static void OnScrollButtonChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		var behavior = (ScrollButtonsHostBehavior)d;
		if (behavior._scrollViewer != null)
		{
			behavior.WireScrollCommandTargets(behavior._scrollViewer);
		}
	}

	private void OnLoaded(object sender, RoutedEventArgs e)
	{
		AttachScrollViewer();
		UpdateButtons();
	}

	private void OnUnloaded(object sender, RoutedEventArgs e)
	{
		DetachScrollViewer();
	}

	private void OnPointerEntered(object sender, PointerRoutedEventArgs e)
	{
		if (e.Pointer.PointerDeviceType == Microsoft.UI.Input.PointerDeviceType.Touch)
		{
			return;
		}

		_isPointerOver = true;
		UpdateButtons();
	}

	private void OnPointerExited(object sender, PointerRoutedEventArgs e)
	{
		if (e.Pointer.PointerDeviceType == Microsoft.UI.Input.PointerDeviceType.Touch)
		{
			return;
		}

		_isPointerOver = false;
		UpdateButtons();
	}

	private void OnPointerCanceled(object sender, PointerRoutedEventArgs e)
	{
		OnPointerExited(sender, e);
	}

	private void AttachScrollViewer()
	{
		DetachScrollViewer();
		ScrollViewer? scrollViewer = AssociatedObject?.FindDescendant<ScrollViewer>();
		if (scrollViewer != null)
		{
			_scrollViewer = scrollViewer;
			scrollViewer.ViewChanged += OnViewChanged;
			WireScrollCommandTargets(scrollViewer);
			_ = AssociatedObject?.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, UpdateButtons);
		}
	}

	private void WireScrollCommandTargets(ScrollViewer scrollViewer)
	{
		if (LeftScrollButton?.Command is ScrollCommand left)
		{
			left.TargetElement = scrollViewer;
		}

		if (RightScrollButton?.Command is ScrollCommand right)
		{
			right.TargetElement = scrollViewer;
		}
	}

	private void DetachScrollViewer()
	{
		_scrollViewer?.ViewChanged -= OnViewChanged;
		_scrollViewer = null;
	}

	private void OnViewChanged(object? sender, ScrollViewerViewChangedEventArgs e)
	{

		if (_isPointerOver)
		{
			UpdateButtons();
		}
	}

	private void UpdateButtons()
	{
		Button? left = LeftScrollButton;
		Button? right = RightScrollButton;
		if (left == null || right == null)
		{
			return;
		}

		ScrollViewer? sv = _scrollViewer ?? AssociatedObject?.FindDescendant<ScrollViewer>();
		if (sv != null)
		{
			WireScrollCommandTargets(sv);
		}

		if (!_isPointerOver || !IsEnabled)
		{
			left.Visibility = Visibility.Collapsed;
			right.Visibility = Visibility.Collapsed;
			return;
		}

		left.Visibility = Visibility.Visible;
		right.Visibility = Visibility.Visible;
	}
}
