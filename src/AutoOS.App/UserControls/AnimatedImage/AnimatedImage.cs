using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;

namespace AutoOS.App.UserControls;

public sealed partial class AnimatedImage : UserControl
{
	public static readonly DependencyProperty ImageUrlProperty =
		DependencyProperty.Register(nameof(ImageUrl), typeof(Uri), typeof(AnimatedImage), new PropertyMetadata(null, OnImageChanged));

	public static readonly DependencyProperty ImageSourceProperty =
		DependencyProperty.Register(nameof(ImageSource), typeof(BitmapImage), typeof(AnimatedImage), new PropertyMetadata(null, OnImageChanged));

	public static readonly DependencyProperty StretchProperty =
		DependencyProperty.Register(nameof(Stretch), typeof(Stretch), typeof(AnimatedImage), new PropertyMetadata(Stretch.UniformToFill, OnStretchChanged));

	public static readonly DependencyProperty OpacityAnimationDurationProperty =
		DependencyProperty.Register(nameof(OpacityAnimationDuration), typeof(TimeSpan), typeof(AnimatedImage), new PropertyMetadata(TimeSpan.FromMilliseconds(600)));

	private Compositor? _compositor;
	private ScalarKeyFrameAnimation? _opacityAnimation;
	private CompositionScopedBatch? _currentBatch;
	private Visual? _topVisual;
	private bool _isAnimationRunning;
	private bool _isLoaded;

	public Uri? ImageUrl
	{
		get => (Uri?)GetValue(ImageUrlProperty);
		set => SetValue(ImageUrlProperty, value);
	}

	public BitmapImage? ImageSource
	{
		get => (BitmapImage?)GetValue(ImageSourceProperty);
		set => SetValue(ImageSourceProperty, value);
	}

	public Stretch Stretch
	{
		get => (Stretch)GetValue(StretchProperty);
		set => SetValue(StretchProperty, value);
	}

	public TimeSpan OpacityAnimationDuration
	{
		get => (TimeSpan)GetValue(OpacityAnimationDurationProperty);
		set => SetValue(OpacityAnimationDurationProperty, value);
	}

	public AnimatedImage()
	{
		InitializeComponent();
		Loaded += AnimatedImage_Loaded;
		Unloaded += AnimatedImage_Unloaded;
	}

	private static void OnImageChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		((AnimatedImage)d).UpdateImage();
	}

	private static void OnStretchChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		AnimatedImage control = (AnimatedImage)d;
		Stretch stretch = (Stretch)e.NewValue;
		control.BottomImage.Stretch = stretch;
		control.TopImage.Stretch = stretch;
	}

	private void AnimatedImage_Loaded(object sender, RoutedEventArgs e)
	{
		_isLoaded = true;

		if (_compositor == null)
		{
			_compositor = ElementCompositionPreview.GetElementVisual(this).Compositor;
			_topVisual = ElementCompositionPreview.GetElementVisual(TopImage);
		}

		if (_opacityAnimation == null)
			InitializeAnimation();

		UpdateImage();
	}

	private void AnimatedImage_Unloaded(object sender, RoutedEventArgs e)
	{
		_isLoaded = false;
		_isAnimationRunning = false;
		DisposeBatch();

		_topVisual?.StopAnimation("Opacity");
		SetTopOpacity(1f);

		if (!ReferenceEquals(TopImage.Source, BottomImage.Source))
			TopImage.Source = BottomImage.Source;
	}

	private void InitializeAnimation()
	{
		if (_compositor == null)
			return;

		_opacityAnimation = _compositor.CreateScalarKeyFrameAnimation();
		_opacityAnimation.InsertKeyFrame(0f, 1f);
		_opacityAnimation.InsertKeyFrame(1f, 0f);
		_opacityAnimation.Duration = OpacityAnimationDuration;
	}

	private void UpdateImage()
	{
		ImageSource? newSource = ResolveSource();

		if (newSource == null)
			return;

		if (ReferenceEquals(BottomImage.Source, newSource))
		{
			if (!_isAnimationRunning)
				SetTopOpacity(1f);

			return;
		}

		if (!_isLoaded || _compositor == null || _opacityAnimation == null || _topVisual == null)
		{
			ShowCurrentImage(newSource);
			return;
		}

		ImageSource? currentSource = _isAnimationRunning
			? BottomImage.Source
			: TopImage.Source ?? BottomImage.Source;

		if (currentSource == null)
		{
			ShowCurrentImage(newSource);
			return;
		}

		if (ReferenceEquals(currentSource, newSource))
		{
			if (!_isAnimationRunning)
				ShowCurrentImage(newSource);

			return;
		}

		// Keep the image that the running transition is revealing as the new top
		// layer, so a source change never rewinds to the image that was fading out.
		TopImage.Source = currentSource;
		DisposeBatch();
		_topVisual.StopAnimation("Opacity");
		SetTopOpacity(1f);

		_isAnimationRunning = false;

		BottomImage.Source = newSource;
		StartAnimation(newSource);
	}

	private ImageSource? ResolveSource()
	{
		if (ImageSource != null)
			return ImageSource;

		if (ImageUrl == null)
			return null;

		if (BottomImage.Source is BitmapImage currentBitmap && currentBitmap.UriSource == ImageUrl)
			return currentBitmap;

		return new BitmapImage(ImageUrl);
	}

	private void ShowCurrentImage(ImageSource source)
	{
		DisposeBatch();
		_topVisual?.StopAnimation("Opacity");
		_isAnimationRunning = false;

		BottomImage.Source = source;
		TopImage.Source = source;
		SetTopOpacity(1f);
	}

	private void StartAnimation(ImageSource newSource)
	{
		if (_compositor == null || _opacityAnimation == null || _topVisual == null)
			return;

		_opacityAnimation.Duration = OpacityAnimationDuration;

		CompositionScopedBatch batch = _compositor.CreateScopedBatch(CompositionBatchTypes.Animation);
		_currentBatch = batch;

		batch.Completed += (s, e) =>
		{
			if (!ReferenceEquals(_currentBatch, batch))
				return;

			_currentBatch = null;
			_isAnimationRunning = false;

			TopImage.Source = newSource;
			SetTopOpacity(1f);

			batch.Dispose();
		};

		SetTopOpacity(1f);
		_isAnimationRunning = true;
		_topVisual.StartAnimation("Opacity", _opacityAnimation);
		batch.End();
	}

	private void SetTopOpacity(float opacity)
	{
		if (_topVisual != null)
			_topVisual.Opacity = opacity;
		else
			TopImage.Opacity = opacity;
	}

	private void DisposeBatch()
	{
		CompositionScopedBatch? batch = _currentBatch;
		_currentBatch = null;
		batch?.Dispose();
	}
}
