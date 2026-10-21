using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace AutoOS.App.UserControls.StoreCarousel2;

public sealed partial class PreviewViewerVideoItem : UserControl
{
	public PreviewViewerVideoItem()
	{
		InitializeComponent();
		Loaded += (s, e) => UpdateSource();
	}

	public static readonly DependencyProperty VideoPlayerSourceProperty =
		DependencyProperty.Register(nameof(VideoPlayerSource), typeof(VideoPlayerSource), typeof(PreviewViewerVideoItem), new PropertyMetadata(null, OnSourceChanged));

	public VideoPlayerSource VideoPlayerSource
	{
		get => (VideoPlayerSource)GetValue(VideoPlayerSourceProperty);
		set => SetValue(VideoPlayerSourceProperty, value);
	}

	private static void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((PreviewViewerVideoItem)d).UpdateSource();

	private void UpdateSource()
	{
		MediaPlayerElement? element = VideoPlayerItemElement;
		if (element == null || VideoPlayerSource == null)
			return;

		try
		{
			element.AutoPlay = false;
			element.SetMediaPlayer(VideoPlayerSource.MediaPlayer);

			element.PosterSource = VideoPlayerSource.ImageSource;
		}
		catch { }
	}

	internal void ApplySource()
	{
		UpdateSource();
	}
}
