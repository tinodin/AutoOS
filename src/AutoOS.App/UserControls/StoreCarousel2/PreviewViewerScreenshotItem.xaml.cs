using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;

namespace AutoOS.App.UserControls.StoreCarousel2;

public sealed partial class PreviewViewerScreenshotItem : UserControl
{
	public PreviewViewerScreenshotItem()
	{
		InitializeComponent();
		Loaded += (s, e) => UpdateImage();
	}

	public static readonly DependencyProperty TileItemProperty =
		DependencyProperty.Register(nameof(TileItem), typeof(ScreenshotTileItem), typeof(PreviewViewerScreenshotItem), new PropertyMetadata(null, OnTileItemChanged));

	public ScreenshotTileItem TileItem
	{
		get => (ScreenshotTileItem)GetValue(TileItemProperty);
		set => SetValue(TileItemProperty, value);
	}

	private static void OnTileItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((PreviewViewerScreenshotItem)d).UpdateImage();

	private void UpdateImage()
	{
		var img = FindName("Image") as Image;
		if (img != null && TileItem?.ImageUri != null)
			img.Source = new BitmapImage(TileItem.ImageUri);
	}
}
