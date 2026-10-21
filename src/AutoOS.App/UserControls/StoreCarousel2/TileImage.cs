using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media.Imaging;

namespace AutoOS.App.UserControls;

public partial class TileImage : UserControl
{
	private ImageSource? _currentSource;
	private IImageItem? _pendingImageItem;
	private bool _commitScheduled;

	public TileImage()
	{
		InitializeComponent();
		MainImage.ImageOpened += OnImageOpened;
		MainImage.ImageFailed += OnImageFailed;
		UpdateVisualState();
	}

	internal void ApplyImageItem(IImageItem? item)
	{
		_pendingImageItem = item;
		ImageProcessed = item == null;
		ScheduleImageCommit();
	}

	private void ScheduleImageCommit()
	{
		if (_commitScheduled)
		{
			return;
		}

		DispatcherQueue? dispatcherQueue = DispatcherQueue;
		if (dispatcherQueue == null)
		{
			CommitImage();
			return;
		}

		_commitScheduled = true;

		if (!dispatcherQueue.TryEnqueue(() =>
			{
				_commitScheduled = false;
				CommitImage();
			}))
		{
			_commitScheduled = false;
			CommitImage();
		}
	}

	private void CommitImage()
	{
		IImageItem? item = _pendingImageItem;

		if (item?.ImageUri != null)
		{
			var bmp = new BitmapImage(item.ImageUri)
			{
				CreateOptions = BitmapCreateOptions.None,
				DecodePixelHeight = RequestedImageHeight > 0 ? RequestedImageHeight : 0,
				DecodePixelWidth = RequestedImageWidth > 0 ? RequestedImageWidth : 0
			};
			_currentSource = bmp;
			MainImage.Source = _currentSource;
			MainImage.Stretch = Stretch;
		}
		else
		{
			_currentSource = null;
			MainImage.Source = null;
		}

		RootGrid.Opacity = 0;

		UpdateFooterTemplate();

		ImageProcessed = _currentSource == null && item == null;
		InvalidateMeasure();

		if ((_currentSource == null && item == null) || !ShowBackgroundColor)
		{
			GoToColoredBackplate();
		}
	}

	private void UpdateVisualState()
	{
		RootGrid.Opacity = 0;

		ImageProcessed = _currentSource == null && ImageItem == null;
		InvalidateMeasure();

		if ((_currentSource == null && ImageItem == null) || !ShowBackgroundColor)
			GoToColoredBackplate();
	}

	private void OnImageOpened(object sender, RoutedEventArgs e)
	{
		ImageProcessed = true;
		InvalidateMeasure();

		if (_currentSource != null)
		{
			var fadeIn = new DoubleAnimation
			{
				From = 0,
				To = 1,
				Duration = new Duration(TimeSpan.FromMilliseconds(300))
			};
			Storyboard.SetTarget(fadeIn, RootGrid);
			Storyboard.SetTargetProperty(fadeIn, "Opacity");

			var storyboard = new Storyboard();
			storyboard.Children.Add(fadeIn);
			storyboard.Begin();
		}

		GoToDefaultBackplate();
	}

	private void OnImageFailed(object sender, ExceptionRoutedEventArgs e)
	{
		ImageProcessed = true;
		InvalidateMeasure();
		DispatcherQueue?.TryEnqueue(GoToColoredBackplate);
	}

	private void UpdateFooterTemplate()
	{
		if (FooterPresenter == null)
		{
			return;
		}

		FooterPresenter.ContentTemplate = FooterTemplate;
		FooterPresenter.Content = FooterTemplate != null ? ImageItem : null;
	}

	private void GoToDefaultBackplate()
	{
		VisualStateManager.GoToState(this, "DefaultBackplateVisualState", true);
	}

	private void GoToColoredBackplate()
	{
		VisualStateManager.GoToState(this, "ColoredBackplateVisualState", true);
	}
}
