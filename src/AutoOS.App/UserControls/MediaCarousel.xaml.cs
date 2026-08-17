using System.Collections.ObjectModel;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.Media.Core;

namespace AutoOS.App.UserControls;

public sealed partial class MediaCarousel : UserControl
{
	private const double HOVER_FADE_DURATION_MS = 200d;

	public static readonly DependencyProperty ScreenshotsProperty = DependencyProperty.Register(
		nameof(Screenshots), typeof(IList<string>), typeof(MediaCarousel), new PropertyMetadata(null, OnMediaChanged));

	public static readonly DependencyProperty VideosProperty = DependencyProperty.Register(
		nameof(Videos), typeof(IList<MediaSource>), typeof(MediaCarousel), new PropertyMetadata(null, OnMediaChanged));

	public static readonly DependencyProperty PosterUrlProperty = DependencyProperty.Register(
		nameof(PosterUrl), typeof(string), typeof(MediaCarousel), new PropertyMetadata(string.Empty, OnMediaChanged));
	private bool isPointerOver;

	public event EventHandler<MediaCarouselClickedEventArgs>? MediaClicked;

	public IList<string> Screenshots
	{
		get => (IList<string>)GetValue(ScreenshotsProperty);
		set => SetValue(ScreenshotsProperty, value);
	}

	public IList<MediaSource> Videos
	{
		get => (IList<MediaSource>)GetValue(VideosProperty);
		set => SetValue(VideosProperty, value);
	}

	public string PosterUrl
	{
		get => (string)GetValue(PosterUrlProperty);
		set => SetValue(PosterUrlProperty, value);
	}

	public ObservableCollection<MediaCarouselItem> MediaItems { get; } = [];

	public MediaCarousel()
	{
		InitializeComponent();
		MediaScrollViewer.SizeChanged += (s, e) => UpdateNavigationButtons();
	}

	private static void OnMediaChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		((MediaCarousel)d).RebuildItems();
	}

	private void RebuildItems()
	{
		MediaItems.Clear();

		foreach (MediaSource mediaSource in Videos ?? [])
		{
			MediaItems.Add(new MediaCarouselItem
			{
				ImageUrl = PosterUrl ?? string.Empty,
				IsVideo = true,
				Label = $"Video {MediaItems.Count + 1}",
				MediaSource = mediaSource
			});
		}

		foreach (string screenshot in Screenshots ?? [])
		{
			MediaItems.Add(new MediaCarouselItem
			{
				ImageUrl = screenshot,
				IsVideo = false,
				Label = $"Screenshot {MediaItems.Count + 1}"
			});
		}

		UpdateNavigationButtons();
	}

	private void MediaScrollViewer_ViewChanged(object? sender, ScrollViewerViewChangedEventArgs e)
	{
		UpdateNavigationButtons();
	}

	private void UpdateNavigationButtons()
	{
		double offset = MediaScrollViewer.HorizontalOffset;
		double scrollable = MediaScrollViewer.ScrollableWidth;

		bool canScroll = scrollable > 1;
		LeftButton.Visibility = isPointerOver && canScroll && offset > 1 ? Visibility.Visible : Visibility.Collapsed;
		RightButton.Visibility = isPointerOver && canScroll && offset < scrollable - 1 ? Visibility.Visible : Visibility.Collapsed;
	}

	private void CarouselRoot_PointerEntered(object sender, PointerRoutedEventArgs e)
	{
		isPointerOver = true;
		UpdateNavigationButtons();
	}

	private void CarouselRoot_PointerExited(object sender, PointerRoutedEventArgs e)
	{
		isPointerOver = false;
		UpdateNavigationButtons();
	}

	private void LeftButton_Click(object sender, RoutedEventArgs e)
	{
		MediaScrollViewer.ChangeView(MediaScrollViewer.HorizontalOffset - 420, null, null, false);
	}

	private void RightButton_Click(object sender, RoutedEventArgs e)
	{
		MediaScrollViewer.ChangeView(MediaScrollViewer.HorizontalOffset + 420, null, null, false);
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

	private void MediaItem_Click(object sender, RoutedEventArgs e)
	{
		OpenFullscreenForSender(sender);
	}

	private void OpenFullscreenForSender(object sender)
	{
		if (sender is not FrameworkElement element || element.DataContext is not MediaCarouselItem item)
			return;

		int index = MediaItems.IndexOf(item);
		if (index < 0)
			return;

		MediaClicked?.Invoke(this, new MediaCarouselClickedEventArgs(MediaItems.ToList(), index, element));
	}

	private void MediaItem_PointerEntered(object sender, PointerRoutedEventArgs e)
	{
		if (sender is FrameworkElement element && element.FindName("HoverOverlay") is Border overlay)
		{
			var animation = new DoubleAnimation
			{
				From = overlay.Opacity,
				To = 1d,
				Duration = new Duration(TimeSpan.FromMilliseconds(HOVER_FADE_DURATION_MS)),
				EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
			};
			Storyboard.SetTarget(animation, overlay);
			Storyboard.SetTargetProperty(animation, "Opacity");
			var storyboard = new Storyboard();
			storyboard.Children.Add(animation);
			storyboard.Begin();
		}
	}

	private void MediaItem_PointerExited(object sender, PointerRoutedEventArgs e)
	{
		if (sender is FrameworkElement element && element.FindName("HoverOverlay") is Border overlay)
		{
			var animation = new DoubleAnimation
			{
				From = overlay.Opacity,
				To = 0d,
				Duration = new Duration(TimeSpan.FromMilliseconds(HOVER_FADE_DURATION_MS)),
				EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
			};
			var storyboard = new Storyboard();
			storyboard.Children.Add(animation);
			Storyboard.SetTarget(animation, overlay);
			Storyboard.SetTargetProperty(animation, "Opacity");
			storyboard.Begin();
		}
	}
}
