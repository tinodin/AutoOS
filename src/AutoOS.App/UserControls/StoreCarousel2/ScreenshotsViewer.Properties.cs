using Microsoft.UI.Xaml;

namespace AutoOS.App.UserControls.StoreCarousel2;

public sealed partial class ScreenshotsViewer
{
	public static readonly DependencyProperty CardDataModelProperty =
		DependencyProperty.Register(nameof(CardDataModel), typeof(ICardDataModel), typeof(ScreenshotsViewer), new PropertyMetadata(null));

	public ICardDataModel CardDataModel
	{
		get => (ICardDataModel)GetValue(CardDataModelProperty);
		set => SetValue(CardDataModelProperty, value);
	}

	public static readonly DependencyProperty IsScrollingEnabledProperty =
		DependencyProperty.Register(nameof(IsScrollingEnabled), typeof(bool), typeof(ScreenshotsViewer), new PropertyMetadata(true));

	public bool IsScrollingEnabled
	{
		get => (bool)GetValue(IsScrollingEnabledProperty);
		set => SetValue(IsScrollingEnabledProperty, value);
	}

	public static readonly DependencyProperty PreviewModeProperty =
		DependencyProperty.Register(nameof(PreviewMode), typeof(PreviewType), typeof(ScreenshotsViewer), new PropertyMetadata(PreviewType.All, OnPreviewModeChanged));

	public PreviewType PreviewMode
	{
		get => (PreviewType)GetValue(PreviewModeProperty);
		set => SetValue(PreviewModeProperty, value);
	}

	private static void OnPreviewModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		((ScreenshotsViewer)d).OnPreviewModeChanged();
	}

	public static readonly DependencyProperty PreviewViewModelProperty =
		DependencyProperty.Register(nameof(PreviewViewModel), typeof(PreviewModuleViewModel), typeof(ScreenshotsViewer), new PropertyMetadata(null, OnPreviewViewModelChanged));

	public PreviewModuleViewModel PreviewViewModel
	{
		get => (PreviewModuleViewModel)GetValue(PreviewViewModelProperty);
		set => SetValue(PreviewViewModelProperty, value);
	}

	private static void OnPreviewViewModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		((ScreenshotsViewer)d).OnPreviewViewModelChanged();
	}

	public static readonly DependencyProperty ScreenshotContainerStyleProperty =
		DependencyProperty.Register(nameof(ScreenshotContainerStyle), typeof(Style), typeof(ScreenshotsViewer), new PropertyMetadata(null));

	public Style ScreenshotContainerStyle
	{
		get => (Style)GetValue(ScreenshotContainerStyleProperty);
		set => SetValue(ScreenshotContainerStyleProperty, value);
	}

	public static readonly DependencyProperty AgeRestrictedProperty =
		DependencyProperty.Register(nameof(AgeRestricted), typeof(bool), typeof(ScreenshotsViewer), new PropertyMetadata(false, OnAgeRestrictedChanged));

	public bool AgeRestricted
	{
		get => (bool)GetValue(AgeRestrictedProperty);
		set => SetValue(AgeRestrictedProperty, value);
	}

	private static void OnAgeRestrictedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		var viewer = (ScreenshotsViewer)d;
		viewer.ApplyAgeRestriction();
	}

	public static readonly DependencyProperty IsAgeBlockedProperty =
		DependencyProperty.Register(nameof(IsAgeBlocked), typeof(bool), typeof(ScreenshotsViewer), new PropertyMetadata(false, OnIsAgeBlockedChanged));

	public bool IsAgeBlocked
	{
		get => (bool)GetValue(IsAgeBlockedProperty);
		set => SetValue(IsAgeBlockedProperty, value);
	}

	private static void OnIsAgeBlockedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		var viewer = (ScreenshotsViewer)d;
		viewer.ApplyAgeRestriction();
	}
}
