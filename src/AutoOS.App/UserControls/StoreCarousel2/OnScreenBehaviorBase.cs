using Microsoft.UI.Xaml;
using Microsoft.Xaml.Interactivity;
using Windows.Foundation;

namespace AutoOS.App.UserControls.StoreCarousel2;

public abstract class OnScreenBehaviorBase : Behavior<FrameworkElement>
{
	private bool _isEnabled = true;
	private bool _isTracking;
	private bool _isSubscribed;
	private bool _isOnScreen;
	private Rect _effectiveViewport;

	public static readonly DependencyProperty IsEnabledProperty =
		DependencyProperty.Register(nameof(IsEnabled), typeof(bool), typeof(OnScreenBehaviorBase), new PropertyMetadata(true, OnIsEnabledChanged));

	public static readonly DependencyProperty IsOnScreenProperty =
		DependencyProperty.Register(nameof(IsOnScreen), typeof(bool), typeof(OnScreenBehaviorBase), new PropertyMetadata(false, OnIsOnScreenPropertyChanged));

	public static readonly DependencyProperty VisibleRatioThresholdProperty =
		DependencyProperty.Register(nameof(VisibleRatioThreshold), typeof(double), typeof(OnScreenBehaviorBase), new PropertyMetadata(0.5, OnVisibleRatioThresholdChanged));

	public static readonly DependencyProperty IsScrollPortProperty =
		DependencyProperty.Register(nameof(IsScrollPort), typeof(bool), typeof(OnScreenBehaviorBase), new PropertyMetadata(false));

	public static readonly DependencyProperty VisibleAreaProperty =
		DependencyProperty.Register(nameof(VisibleArea), typeof(Rect), typeof(OnScreenBehaviorBase), new PropertyMetadata(default(Rect)));

	public bool IsEnabled
	{
		get => (bool)GetValue(IsEnabledProperty);
		set => SetValue(IsEnabledProperty, value);
	}

	public bool IsOnScreen
	{
		get => (bool)GetValue(IsOnScreenProperty);
		set => SetValue(IsOnScreenProperty, value);
	}

	public double VisibleRatioThreshold
	{
		get => (double)GetValue(VisibleRatioThresholdProperty);
		set => SetValue(VisibleRatioThresholdProperty, value);
	}

	public bool IsScrollPort
	{
		get => (bool)GetValue(IsScrollPortProperty);
		set => SetValue(IsScrollPortProperty, value);
	}

	public Rect VisibleArea
	{
		get => (Rect)GetValue(VisibleAreaProperty);
		set => SetValue(VisibleAreaProperty, value);
	}

	protected override void OnAttached()
	{
		base.OnAttached();

		_isEnabled = IsEnabled;
		_isTracking = true;

		if (AssociatedObject is { } element)
		{
			element.Loaded += OnElementLoaded;
			element.Unloaded += OnElementUnloaded;
		}

		UpdateTrackingState();
	}

	protected override void OnDetaching()
	{
		if (AssociatedObject is { } element)
		{
			element.Loaded -= OnElementLoaded;
			element.Unloaded -= OnElementUnloaded;
		}

		Unsubscribe();
		UpdateIsOnScreen(false);

		base.OnDetaching();
	}

	protected virtual void OnIsOnScreenChanged(bool isOnScreen)
	{
	}

	protected void StopTracking()
	{
		_isTracking = false;
		UpdateTrackingState();
	}

	protected void RestartTracking()
	{
		_isTracking = true;
		UpdateTrackingState();
	}

	private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		var behavior = (OnScreenBehaviorBase)d;
		behavior._isEnabled = (bool)e.NewValue;

		if (behavior._isEnabled)
		{
			behavior._isTracking = true;
		}

		behavior.UpdateTrackingState();
	}

	private static void OnIsOnScreenPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		var behavior = (OnScreenBehaviorBase)d;
		behavior._isOnScreen = (bool)e.NewValue;
		behavior.OnIsOnScreenChanged((bool)e.NewValue);
	}

	private static void OnVisibleRatioThresholdChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((OnScreenBehaviorBase)d).Update();

	private void OnElementLoaded(object sender, RoutedEventArgs e) => UpdateTrackingState();

	private void OnElementUnloaded(object sender, RoutedEventArgs e) => Unsubscribe();

	private void UpdateTrackingState()
	{
		if (_isEnabled && _isTracking)
		{
			Subscribe();
			return;
		}

		Unsubscribe();
		UpdateIsOnScreen(false);
	}

	private void Subscribe()
	{
		if (AssociatedObject is not { } element || !element.IsLoaded || element.Visibility != Visibility.Visible)
		{
			Unsubscribe();
			return;
		}

		if (_isSubscribed)
		{
			return;
		}

		element.EffectiveViewportChanged += OnEffectiveViewportChanged;
		element.SizeChanged += OnElementSizeChanged;
		_isSubscribed = true;

		Update();
	}

	private void Unsubscribe()
	{
		if (!_isSubscribed)
		{
			return;
		}

		if (AssociatedObject is { } element)
		{
			element.EffectiveViewportChanged -= OnEffectiveViewportChanged;
			element.SizeChanged -= OnElementSizeChanged;
		}

		_isSubscribed = false;
	}

	private void OnEffectiveViewportChanged(FrameworkElement sender, EffectiveViewportChangedEventArgs args)
	{
		if (_effectiveViewport == args.EffectiveViewport)
		{
			return;
		}

		_effectiveViewport = args.EffectiveViewport;
		Update();
	}

	private void OnElementSizeChanged(object sender, SizeChangedEventArgs e) => Update();

	private void Update()
	{
		if (AssociatedObject is not { } element || !_isEnabled || !_isTracking)
		{
			return;
		}

		float width = (float)element.ActualWidth;
		float height = (float)element.ActualHeight;

		float visibleWidth = (float)(Math.Min(width, _effectiveViewport.X + _effectiveViewport.Width) - Math.Max(0.0, _effectiveViewport.X));
		float visibleHeight = (float)(Math.Min(height, _effectiveViewport.Y + _effectiveViewport.Height) - Math.Max(0.0, _effectiveViewport.Y));

		bool isOnScreen;

		if (visibleWidth <= 0 || visibleHeight <= 0)
		{
			isOnScreen = false;
		}
		else
		{
			float visibleRatio = visibleWidth * visibleHeight / (width * height);
			isOnScreen = visibleRatio >= VisibleRatioThreshold;
		}

		UpdateIsOnScreen(isOnScreen);
	}

	private void UpdateIsOnScreen(bool isOnScreen)
	{
		if (_isOnScreen == isOnScreen)
		{
			return;
		}

		SetValue(IsOnScreenProperty, isOnScreen);
	}
}
