using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace AutoOS.App.UserControls;

public partial class TileImage
{
	public static readonly DependencyProperty FooterTemplateProperty =
		DependencyProperty.Register(nameof(FooterTemplate), typeof(DataTemplate), typeof(TileImage), new PropertyMetadata(null, OnFooterTemplateChanged));

	public DataTemplate FooterTemplate
	{
		get => (DataTemplate)GetValue(FooterTemplateProperty);
		set => SetValue(FooterTemplateProperty, value);
	}

	private static void OnFooterTemplateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		((TileImage)d).UpdateFooterTemplate();
	}

	public static readonly DependencyProperty ImageItemProperty =
		DependencyProperty.Register(nameof(ImageItem), typeof(IImageItem), typeof(TileImage), new PropertyMetadata(null, OnImageItemChanged));

	public IImageItem ImageItem
	{
		get => (IImageItem)GetValue(ImageItemProperty);
		set => SetValue(ImageItemProperty, value);
	}

	private static void OnImageItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		((TileImage)d).ApplyImageItem(e.NewValue as IImageItem);
	}

	public static readonly DependencyProperty StretchProperty =
		DependencyProperty.Register(nameof(Stretch), typeof(Stretch), typeof(TileImage), new PropertyMetadata(Stretch.Uniform));

	public Stretch Stretch
	{
		get => (Stretch)GetValue(StretchProperty);
		set => SetValue(StretchProperty, value);
	}

	public int RequestedImageWidth { get; set; }

	public int RequestedImageHeight { get; set; }

	public static readonly DependencyProperty ShowBackgroundColorProperty =
		DependencyProperty.Register(nameof(ShowBackgroundColor), typeof(bool), typeof(TileImage), new PropertyMetadata(true));

	public bool ShowBackgroundColor
	{
		get => (bool)GetValue(ShowBackgroundColorProperty);
		set => SetValue(ShowBackgroundColorProperty, value);
	}

	public static readonly DependencyProperty ImageProcessedProperty =
		DependencyProperty.Register(nameof(ImageProcessed), typeof(bool), typeof(TileImage), new PropertyMetadata(true));

	public bool ImageProcessed
	{
		get => (bool)GetValue(ImageProcessedProperty);
		set => SetValue(ImageProcessedProperty, value);
	}
}
