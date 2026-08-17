using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Foundation;
using Windows.Media.Playback;
using Windows.System;

namespace AutoOS.App.UserControls;

public sealed partial class MediaFullscreenViewer : UserControl
{
	private const string AnimationKey = "mediaFullscreen";

	public event EventHandler? Closed;

	private readonly MediaPlayer mediaPlayer = new();
	private IReadOnlyList<MediaCarouselItem>? items;
	private FrameworkElement? animationSource;
	private double animationSourceOpacity = 1d;
	private int currentIndex;
	private bool isShowingVideo;
	private bool isNavigating;
	private bool isClosing;

	public MediaFullscreenViewer()
	{
		InitializeComponent();
		Player.SetMediaPlayer(mediaPlayer);
		mediaPlayer.MediaOpened += MediaPlayer_MediaOpened;

		Unloaded -= MediaFullscreenViewer_Unloaded;
		Unloaded += MediaFullscreenViewer_Unloaded;
	}

	private void MediaFullscreenViewer_Unloaded(object sender, RoutedEventArgs e)
	{
		StopVideo();

		// Detach and release the player. Media player session cleanup can throw at
		// the OS boundary, so it must never escape the unload path.
		try
		{
			Player.SetMediaPlayer(null!);
		}
		catch (Exception)
		{
		}
		finally
		{
			mediaPlayer.Dispose();
		}
	}

	public void Show(IReadOnlyList<MediaCarouselItem> mediaItems, int startIndex, FrameworkElement? sourceElement)
	{
		if (mediaItems.Count == 0)
			return;

		items = mediaItems;
		currentIndex = Math.Clamp(startIndex, 0, mediaItems.Count - 1);
		animationSource = sourceElement;
		isClosing = false;
		double sourceAspectRatio = sourceElement != null && sourceElement.ActualHeight > 0d
			? sourceElement.ActualWidth / sourceElement.ActualHeight
			: 16d / 9d;
		UpdateMediaHostSize(sourceAspectRatio);
		MediaHost.Opacity = sourceElement == null ? 1d : 0d;

		if (sourceElement != null)
		{
			animationSourceOpacity = sourceElement.Opacity;
			try
			{
				ConnectedAnimation animation = ConnectedAnimationService.GetForCurrentView().PrepareToAnimate(AnimationKey, sourceElement);
				animation.Configuration = new DirectConnectedAnimationConfiguration();
				sourceElement.Opacity = 0d;
			}
			catch (Exception)
			{
				MediaHost.Opacity = 1d;
			}
		}

		Visibility = Visibility.Visible;
		UpdateNavigationButtons();
		ShowItem();

		RootGrid.Focus(FocusState.Programmatic);

		// Give layout a chance to measure the viewer, then start the animation.
		DispatcherQueue.TryEnqueue(() =>
		{
			if (Visibility != Visibility.Visible || animationSource == null)
				return;

			try
			{
				ConnectedAnimation? animation = ConnectedAnimationService.GetForCurrentView().GetAnimation(AnimationKey);
				MediaHost.Opacity = 1d;
				_ = animation?.TryStart(MediaHost);
			}
			catch (Exception)
			{
				MediaHost.Opacity = 1d;
			}
		});
	}

	public void Close()
	{
		if (Visibility != Visibility.Visible || isClosing)
			return;

		isClosing = true;
		StopVideo();

		if (TryAnimateBackToSource())
			return;

		FadeOutAndClose();
	}

	private bool TryAnimateBackToSource()
	{
		if (animationSource == null || animationSource.ActualWidth <= 0d || animationSource.ActualHeight <= 0d || MediaHost.ActualWidth <= 0d || MediaHost.ActualHeight <= 0d)
			return false;

		try
		{
			Point sourcePosition = animationSource.TransformToVisual(RootGrid).TransformPoint(new Point());
			Point hostPosition = MediaHost.TransformToVisual(RootGrid).TransformPoint(new Point());
			double sourceAspectRatio = animationSource.ActualWidth / animationSource.ActualHeight;
			double mediaAspectRatio = sourceAspectRatio;
			if (PosterImage.Source is BitmapImage bitmap && bitmap.PixelWidth > 0 && bitmap.PixelHeight > 0)
				mediaAspectRatio = (double)bitmap.PixelWidth / bitmap.PixelHeight;

			double renderedWidth;
			double renderedHeight;
			if (MediaHost.ActualWidth / MediaHost.ActualHeight > mediaAspectRatio)
			{
				renderedHeight = MediaHost.ActualHeight;
				renderedWidth = renderedHeight * mediaAspectRatio;
			}
			else
			{
				renderedWidth = MediaHost.ActualWidth;
				renderedHeight = renderedWidth / mediaAspectRatio;
			}

			double renderedOffsetX = (MediaHost.ActualWidth - renderedWidth) / 2d;
			double renderedOffsetY = (MediaHost.ActualHeight - renderedHeight) / 2d;
			double scale = Math.Max(animationSource.ActualWidth / renderedWidth, animationSource.ActualHeight / renderedHeight);

			if (scale <= 0d)
				return false;

			if (MediaHost.RenderTransform is not CompositeTransform transform)
			{
				transform = new CompositeTransform();
				MediaHost.RenderTransform = transform;
			}

			transform.CenterX = MediaHost.ActualWidth / 2d;
			transform.CenterY = MediaHost.ActualHeight / 2d;
			transform.ScaleX = 1d;
			transform.ScaleY = 1d;
			transform.TranslateX = 0d;
			transform.TranslateY = 0d;

			double targetX = sourcePosition.X - hostPosition.X - (transform.CenterX * (1d - scale)) - (renderedOffsetX * scale);
			double targetY = sourcePosition.Y - hostPosition.Y - (transform.CenterY * (1d - scale)) - (renderedOffsetY * scale);
			var duration = new Duration(TimeSpan.FromMilliseconds(320));
			var easing = new CubicEase { EasingMode = EasingMode.EaseInOut };
			var storyboard = new Storyboard();

			AddTransformAnimation(storyboard, "ScaleX", scale, duration, easing);
			AddTransformAnimation(storyboard, "ScaleY", scale, duration, easing);
			AddTransformAnimation(storyboard, "TranslateX", targetX, duration, easing);
			AddTransformAnimation(storyboard, "TranslateY", targetY, duration, easing);

			AddOpacityAnimation(storyboard, Backdrop, duration, easing);
			AddOpacityAnimation(storyboard, OverlayChrome, duration, easing);

			storyboard.Completed += (s, e) =>
			{
				Backdrop.Opacity = 1d;
				OverlayChrome.Opacity = 1d;
				ResetMediaHostTransform();
				Visibility = Visibility.Collapsed;
				CompleteClose();
			};
			storyboard.Begin();
			return true;
		}
		catch (Exception)
		{
			return false;
		}
	}

	private void UpdateMediaHostSize(double aspectRatio)
	{
		double availableWidth = Math.Max(0d, RootGrid.ActualWidth - 280d);
		double availableHeight = Math.Max(0d, RootGrid.ActualHeight - 158d);
		if (availableWidth <= 0d || availableHeight <= 0d || aspectRatio <= 0d)
			return;

		if (availableWidth / availableHeight > aspectRatio)
		{
			MediaHost.Height = availableHeight;
			MediaHost.Width = availableHeight * aspectRatio;
		}
		else
		{
			MediaHost.Width = availableWidth;
			MediaHost.Height = availableWidth / aspectRatio;
		}
	}

	private static void AddOpacityAnimation(Storyboard storyboard, UIElement target, Duration duration, EasingFunctionBase easing)
	{
		var animation = new DoubleAnimation
		{
			To = 0d,
			Duration = duration,
			EasingFunction = easing
		};
		Storyboard.SetTarget(animation, target);
		Storyboard.SetTargetProperty(animation, "Opacity");
		storyboard.Children.Add(animation);
	}

	private void AddTransformAnimation(Storyboard storyboard, string propertyName, double targetValue, Duration duration, EasingFunctionBase easing)
	{
		var animation = new DoubleAnimation
		{
			To = targetValue,
			Duration = duration,
			EasingFunction = easing
		};
		Storyboard.SetTarget(animation, MediaHost);
		Storyboard.SetTargetProperty(animation, $"(UIElement.RenderTransform).(CompositeTransform.{propertyName})");
		storyboard.Children.Add(animation);
	}

	private void ResetMediaHostTransform()
	{
		if (MediaHost.RenderTransform is not CompositeTransform transform)
			return;

		transform.CenterX = 0d;
		transform.CenterY = 0d;
		transform.ScaleX = 1d;
		transform.ScaleY = 1d;
		transform.TranslateX = 0d;
		transform.TranslateY = 0d;
	}

	private void FadeOutAndClose()
	{
		var fadeOut = new DoubleAnimation
		{
			From = 1d,
			To = 0d,
			Duration = new Duration(TimeSpan.FromMilliseconds(150)),
			EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
		};

		Storyboard.SetTarget(fadeOut, MediaHost);
		Storyboard.SetTargetProperty(fadeOut, "Opacity");

		var storyboard = new Storyboard();
		storyboard.Children.Add(fadeOut);

		storyboard.Completed += (s, e) =>
		{
			Visibility = Visibility.Collapsed;
			CompleteClose();
		};

		storyboard.Begin();
	}

	private void CompleteClose()
	{
		MediaHost.Opacity = 1d;
		Backdrop.Opacity = 1d;
		OverlayChrome.Opacity = 1d;
		if (animationSource != null)
			animationSource.Opacity = animationSourceOpacity;

		items = null;
		animationSource = null;
		animationSourceOpacity = 1d;
		isClosing = false;
		Closed?.Invoke(this, EventArgs.Empty);
	}

	private void ShowItem()
	{
		if (items == null || items.Count == 0)
			return;

		MediaCarouselItem item = items[currentIndex];

		MediaLabelText.Text = item.IsVideo ? $"Video {currentIndex + 1}" : $"Screenshot {currentIndex + 1}";
		CounterText.Text = $"{currentIndex + 1} / {items.Count}";

		PosterImage.Source = !string.IsNullOrEmpty(item.ImageUrl) ? new BitmapImage(new Uri(item.ImageUrl)) : null;

		if (item.IsVideo && item.MediaSource != null)
		{
			isShowingVideo = true;
			PosterImage.Visibility = Visibility.Visible;
			Player.Visibility = Visibility.Visible;
			mediaPlayer.Source = item.MediaSource;
			mediaPlayer.Play();
		}
		else
		{
			StopVideo();
			PosterImage.Visibility = Visibility.Visible;
			Player.Visibility = Visibility.Collapsed;
		}

		UpdateNavigationButtons();
	}

	private void StopVideo()
	{
		isShowingVideo = false;
		mediaPlayer.Pause();
		mediaPlayer.Source = null;
	}

	private void MediaPlayer_MediaOpened(MediaPlayer sender, object args)
	{
		DispatcherQueue.TryEnqueue(() =>
		{
			if (isShowingVideo && PosterImage.Visibility == Visibility.Visible)
				PosterImage.Visibility = Visibility.Collapsed;
		});
	}

	private void Navigate(int direction)
	{
		if (items == null || items.Count == 0 || isNavigating)
			return;

		int next = currentIndex + direction;
		if (next < 0 || next >= items.Count)
			return;

		isNavigating = true;

		if (MediaHost.RenderTransform is not CompositeTransform)
		{
			MediaHost.RenderTransform = new CompositeTransform();
		}

		var ct = (CompositeTransform)MediaHost.RenderTransform;
		// The outgoing item travels opposite the requested direction while the
		// incoming item enters from that direction: right advances from right to
		// left, and left advances from left to right.
		double hostWidth = MediaHost.ActualWidth > 0d ? MediaHost.ActualWidth : RootGrid.ActualWidth;
		double slideDistance = direction * hostWidth;

		var slideOut = new DoubleAnimation
		{
			From = 0d,
			To = -slideDistance,
			Duration = new Duration(TimeSpan.FromMilliseconds(200)),
			EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
		};

		Storyboard.SetTarget(slideOut, MediaHost);
		Storyboard.SetTargetProperty(slideOut, "(UIElement.RenderTransform).(CompositeTransform.TranslateX)");

		var outStoryboard = new Storyboard();
		outStoryboard.Children.Add(slideOut);

		outStoryboard.Completed += (s, e) =>
		{
			currentIndex = next;
			ShowItem();

			ct.TranslateX = slideDistance;

			var slideIn = new DoubleAnimation
			{
				From = slideDistance,
				To = 0d,
				Duration = new Duration(TimeSpan.FromMilliseconds(250)),
				EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
			};

			Storyboard.SetTarget(slideIn, MediaHost);
			Storyboard.SetTargetProperty(slideIn, "(UIElement.RenderTransform).(CompositeTransform.TranslateX)");

			var inStoryboard = new Storyboard();
			inStoryboard.Children.Add(slideIn);

			inStoryboard.Completed += (s2, e2) =>
			{
				ct.TranslateX = 0d;
				isNavigating = false;
			};
			inStoryboard.Begin();
		};

		outStoryboard.Begin();
	}

	private void UpdateNavigationButtons()
	{
		if (items == null || items.Count == 0)
		{
			ScrollBackBtn.Visibility = Visibility.Collapsed;
			ScrollForwardBtn.Visibility = Visibility.Collapsed;
			return;
		}

		ScrollBackBtn.Visibility = currentIndex > 0 ? Visibility.Visible : Visibility.Collapsed;
		ScrollForwardBtn.Visibility = currentIndex < items.Count - 1 ? Visibility.Visible : Visibility.Collapsed;
	}

	private void ScrollBackBtn_Click(object sender, RoutedEventArgs e)
	{
		Navigate(-1);
	}

	private void ScrollForwardBtn_Click(object sender, RoutedEventArgs e)
	{
		Navigate(1);
	}

	private void NavigationButton_PointerEntered(object sender, PointerRoutedEventArgs e)
	{
		if (sender is Button { Content: FontIcon icon })
			icon.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(0xFF, 0x4A, 0x4A, 0x4A));
	}

	private void NavigationButton_PointerExited(object sender, PointerRoutedEventArgs e)
	{
		if (sender is Button { Content: FontIcon icon })
			icon.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(0xFF, 0x8A, 0x8A, 0x8A));
	}

	private void CloseButton_Click(object sender, RoutedEventArgs e)
	{
		Close();
	}

	private void CloseButton_PointerEntered(object sender, PointerRoutedEventArgs e)
	{
		if (sender is Button button)
			button.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(0xFF, 0x48, 0x48, 0x4C));
	}

	private void CloseButton_PointerExited(object sender, PointerRoutedEventArgs e)
	{
		if (sender is Button button)
			button.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(0x00, 0x00, 0x00, 0x00));
	}

	private void Root_KeyDown(object sender, KeyRoutedEventArgs e)
	{
		switch (e.Key)
		{
			case VirtualKey.Left:
			case VirtualKey.GamepadDPadLeft:
				Navigate(-1);
				e.Handled = true;
				break;

			case VirtualKey.Right:
			case VirtualKey.GamepadDPadRight:
				Navigate(1);
				e.Handled = true;
				break;

			case VirtualKey.Escape:
			case VirtualKey.GamepadB:
				Close();
				e.Handled = true;
				break;
		}
	}
}
