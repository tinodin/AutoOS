using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.Foundation;
using Windows.System;

namespace AutoOS.App.UserControls;

public partial class NoPointerWheelListView : ListView
{
	private ScrollViewer? _scrollViewer;
	private readonly PointerEventHandler _pointerWheelHandler;

	public NoPointerWheelListView()
	{
		_pointerWheelHandler = OnScrollViewerPointerWheelChanged;
		Unloaded += OnUnloaded;
	}

	public static readonly DependencyProperty DisposeOnUnloadedProperty =
		DependencyProperty.Register(nameof(DisposeOnUnloaded), typeof(bool), typeof(NoPointerWheelListView), new PropertyMetadata(false));

	public bool DisposeOnUnloaded
	{
		get => (bool)GetValue(DisposeOnUnloadedProperty);
		set => SetValue(DisposeOnUnloadedProperty, value);
	}

	public static readonly DependencyProperty HorizontalScrollModeProperty =
		DependencyProperty.Register(nameof(HorizontalScrollMode), typeof(ScrollMode), typeof(NoPointerWheelListView), new PropertyMetadata(default(ScrollMode)));

	public ScrollMode HorizontalScrollMode
	{
		get => (ScrollMode)GetValue(HorizontalScrollModeProperty);
		set => SetValue(HorizontalScrollModeProperty, value);
	}

	public static readonly DependencyProperty RemoveSelectedItemOnUpDownNavigationProperty =
		DependencyProperty.Register(nameof(RemoveSelectedItemOnUpDownNavigation), typeof(bool), typeof(NoPointerWheelListView), new PropertyMetadata(false));

	public bool RemoveSelectedItemOnUpDownNavigation
	{
		get => (bool)GetValue(RemoveSelectedItemOnUpDownNavigationProperty);
		set => SetValue(RemoveSelectedItemOnUpDownNavigationProperty, value);
	}

	public static readonly DependencyProperty ScrollViewerMarginProperty =
		DependencyProperty.Register(nameof(ScrollViewerMargin), typeof(Thickness), typeof(NoPointerWheelListView), new PropertyMetadata(default(Thickness)));

	public Thickness ScrollViewerMargin
	{
		get => (Thickness)GetValue(ScrollViewerMarginProperty);
		set => SetValue(ScrollViewerMarginProperty, value);
	}

	public static readonly DependencyProperty ScrollViewerPaddingProperty =
		DependencyProperty.Register(nameof(ScrollViewerPadding), typeof(Thickness), typeof(NoPointerWheelListView), new PropertyMetadata(default(Thickness)));

	public Thickness ScrollViewerPadding
	{
		get => (Thickness)GetValue(ScrollViewerPaddingProperty);
		set => SetValue(ScrollViewerPaddingProperty, value);
	}

	protected override void OnApplyTemplate()
	{
		base.OnApplyTemplate();
		_scrollViewer?.RemoveHandler(PointerWheelChangedEvent, _pointerWheelHandler);
		_scrollViewer = null;

		_scrollViewer = GetTemplateChild("ScrollViewer") as ScrollViewer;
		if (_scrollViewer != null)
		{
			_scrollViewer.IsDeferredScrollingEnabled = false;
			_scrollViewer.IsHorizontalRailEnabled = true;
			_scrollViewer.IsVerticalRailEnabled = true;
			_scrollViewer.VerticalScrollMode = ScrollMode.Disabled;
			_scrollViewer.HorizontalScrollMode = HorizontalScrollMode;
			_scrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
			_scrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden;

			if (ScrollViewerMargin != default)
				_scrollViewer.Margin = ScrollViewerMargin;
			if (ScrollViewerPadding != default)
				_scrollViewer.Padding = ScrollViewerPadding;

			if (HorizontalScrollMode == ScrollMode.Enabled)
			{
				_scrollViewer.AddHandler(PointerWheelChangedEvent, _pointerWheelHandler, true);
			}
		}
	}

	private void OnScrollViewerPointerWheelChanged(object sender, PointerRoutedEventArgs e)
	{
		if (_scrollViewer != null && !e.GetCurrentPoint(_scrollViewer).Properties.IsHorizontalMouseWheel)
		{
			e.Handled = false;
		}
	}

	protected override void OnKeyDown(KeyRoutedEventArgs e)
	{
		base.OnKeyDown(e);

		if (e.Key is VirtualKey.Left or VirtualKey.Right)
		{
			ScrollFocusedItemIntoCenter();
			return;
		}

		if (RemoveSelectedItemOnUpDownNavigation && (e.Key == VirtualKey.Up || e.Key == VirtualKey.Down))
		{
			SelectedItem = null;
		}
	}

	private void ScrollFocusedItemIntoCenter()
	{
		if (_scrollViewer == null || XamlRoot == null)
		{
			return;
		}

		if (FocusManager.GetFocusedElement(XamlRoot) is not FrameworkElement focused || !focused.IsLoaded)
		{
			return;
		}

		Point origin = focused.TransformToVisual(_scrollViewer).TransformPoint(new Point(0, 0));
		double target = origin.X + _scrollViewer.HorizontalOffset + focused.ActualWidth * 0.5 - _scrollViewer.ViewportWidth * 0.5;

		if (double.IsNaN(target))
		{
			target = 0;
		}

		if (target < 0)
		{
			target = 0;
		}
		else if (target > _scrollViewer.ScrollableWidth)
		{
			target = _scrollViewer.ScrollableWidth;
		}

		_scrollViewer.ChangeView(target, 0, null, false);
	}

	private void OnUnloaded(object sender, RoutedEventArgs e)
	{
		_scrollViewer?.RemoveHandler(PointerWheelChangedEvent, _pointerWheelHandler);
		_scrollViewer = null;

		if (DisposeOnUnloaded)
			ItemsSource = null;
	}
}
