using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;

namespace AutoOS.App.UserControls.StoreCarousel2;

public sealed partial class ScreenshotsViewer : UserControl
{
	public ScreenshotsViewer()
	{
		InitializeComponent();
		Loaded += OnLoaded;
	}

	public event EventHandler<object>? ScreenshotClicked;
	public event EventHandler? SignInClicked;

	public ListViewBase TileList => ScreenshotTileList;

	public PreviewViewerHelper? Helper { get; set; }

	protected override void OnApplyTemplate()
	{
		base.OnApplyTemplate();
		AttachTileList();
	}

	private void OnLoaded(object sender, RoutedEventArgs e)
	{
		AttachTileList();
		ApplyAgeRestriction();
	}

	private void ApplyAgeRestriction()
	{
		RefreshTileListSource();
		_ = DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, HookAgeGateSignInButtons);
	}

	private void RefreshTileListSource()
	{
		if (ScreenshotTileList == null)
		{
			return;
		}

		if (AgeRestricted)
		{
			ScreenshotTileList.ItemsSource = new List<object> { AgeRestrictedTileItem.Create(PreviewViewModel?.Items, IsAgeBlocked) };
		}
		else
		{
			ScreenshotTileList.ItemsSource = PreviewViewModel?.Items;
		}

		UpdateScrollingState();
	}

	private void UpdateScrollingState()
	{
		ScrollViewer? scrollViewer = ScreenshotTileList.FindDescendant<ScrollViewer>();
		if (scrollViewer == null)
		{
			return;
		}

		scrollViewer.HorizontalScrollMode = IsScrollingEnabled ? ScrollMode.Auto : ScrollMode.Disabled;
	}

	private void AttachTileList()
	{
		if (ScreenshotTileList == null)
			return;

		ScreenshotTileList.ItemClick -= OnTileListClick;
		ScreenshotTileList.ItemClick += OnTileListClick;
		ScreenshotTileList.ContainerContentChanging -= OnContainerContentChanging;
		ScreenshotTileList.ContainerContentChanging += OnContainerContentChanging;
		ScreenshotTileList.Loaded -= OnTileListLoaded;
		ScreenshotTileList.Loaded += OnTileListLoaded;

		ApplyAgeRestriction();
		_ = DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, ForceUpdateVideoThumbnails);
	}

	private void OnTileListLoaded(object sender, RoutedEventArgs e)
	{
		ForceUpdateVideoThumbnails();
		HookAgeGateSignInButtons();
	}

	private void ForceUpdateVideoThumbnails()
	{
		if (ScreenshotTileList == null)
			return;
		for (int i = 0; i < ScreenshotTileList.Items.Count; i++)
		{
			if (ScreenshotTileList.Items[i] is not VideoPlayerSource videoItem || videoItem.ImageUri == null)
				continue;
			var container = ScreenshotTileList.ContainerFromIndex(i) as FrameworkElement;
			if (container == null)
				continue;
			DependencyObject searchRoot = container;
			if (container is ListViewItem lvi && lvi.ContentTemplateRoot is DependencyObject lviRoot)
				searchRoot = lviRoot;
			Image? image = searchRoot.FindDescendant<Image>() ?? container.FindDescendant<Image>();
			if (image == null)
				continue;

			LoadVideoThumbnail(image, videoItem.ImageUri);
		}
	}

	private static void LoadVideoThumbnail(Image image, Uri uri)
	{
		if (image.Source is BitmapImage existing && existing.UriSource?.ToString() == uri.ToString())
			return;

		try
		{
			image.Source = new BitmapImage(uri) { CreateOptions = BitmapCreateOptions.None, DecodePixelHeight = 316 };
		}
		catch { }
	}

	private bool HookAgeGateSignInButton(FrameworkElement container)
	{
		if (container == null)
		{
			return false;
		}

		DependencyObject searchRoot = container;
		if (container is ListViewItem lvi && lvi.ContentTemplateRoot is DependencyObject lviRoot)
			searchRoot = lviRoot;

		Button? button = searchRoot.FindDescendant<Button>() ?? container.FindDescendant<Button>();
		if (button == null)
		{
			return false;
		}

		button.Click -= OnAgeGatingSignInClicked;
		button.Click += OnAgeGatingSignInClicked;
		return true;
	}

	private void HookAgeGateSignInButtons()
	{
		if (!AgeRestricted || ScreenshotTileList == null)
		{
			return;
		}

		for (int i = 0; i < ScreenshotTileList.Items.Count; i++)
		{
			if (ScreenshotTileList.ContainerFromIndex(i) is FrameworkElement container)
			{
				HookAgeGateSignInButton(container);
			}
		}
	}

	private void OnAgeGatingSignInClicked(object sender, RoutedEventArgs e) => SignInClicked?.Invoke(this, EventArgs.Empty);

	private void OnContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
	{
		if (args.InRecycleQueue)
			return;

		if (AgeRestricted && !HookAgeGateSignInButton(args.ItemContainer) && args.Phase < 3)
		{
			args.RegisterUpdateCallback(OnContainerContentChanging);
			return;
		}

		if (args.Item is not VideoPlayerSource videoItem || videoItem.ImageUri == null)
			return;

		if (args.Phase == 0)
		{
			args.RegisterUpdateCallback(OnContainerContentChanging);
			return;
		}

		var container = args.ItemContainer as FrameworkElement;
		if (container == null)
		{
			if (args.Phase < 3)
				args.RegisterUpdateCallback(OnContainerContentChanging);
			return;
		}

		DependencyObject searchRoot = container;
		if (args.ItemContainer is ListViewItem lvi && lvi.ContentTemplateRoot is DependencyObject lviRoot)
			searchRoot = lviRoot;

		Image? image = searchRoot.FindDescendant<Image>() ?? container.FindDescendant<Image>();
		if (image == null)
		{
			if (args.Phase < 3)
				args.RegisterUpdateCallback(OnContainerContentChanging);
			return;
		}

		LoadVideoThumbnail(image, videoItem.ImageUri);
	}

	private void OnTileListClick(object sender, ItemClickEventArgs e)
	{
		int index = ScreenshotTileList.Items.IndexOf(e.ClickedItem);
		if (index < 0)
			index = 0;
		ScreenshotClicked?.Invoke(this, e.ClickedItem ?? index);

		if (AgeRestricted)
		{
			return;
		}

		if (Helper != null && e.ClickedItem != null)
		{
			Helper.SetUpPreviewViewer(e.ClickedItem);
		}
		else if (Helper?.ListViewSource != null)
		{
			Helper.ListViewSource.ScrollToIndex(index);
		}
	}

	internal void OnPreviewModeChanged()
	{
		PreviewViewerHelper? helper = Helper;
		if (helper != null && helper.PreviewMode != PreviewMode)
		{
			helper.PreviewMode = PreviewMode;
		}

		PreviewViewModel?.PreviewMode = PreviewMode;
	}

	internal void OnPreviewViewModelChanged()
	{
		PreviewModuleViewModel? viewModel = PreviewViewModel;
		viewModel?.PreviewMode = PreviewMode;
		viewModel?.RebuildPreviewItems();

		if (Helper != null)
		{
			if (viewModel == null)
			{
				Helper.ListViewSource = null;
			}
		}

		RefreshTileListSource();
	}

	public void FocusItem(int index, FocusState state)
	{
		if (ScreenshotTileList == null || index < 0 || index >= ScreenshotTileList.Items.Count)
			return;

		try
		{
			if (ScreenshotTileList.ContainerFromIndex(index) is Control c)
			{
				c.Focus(state);
				return;
			}
		}
		catch { }

		_ = DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
		{
			try
			{
				if (ScreenshotTileList.ContainerFromIndex(index) is Control d)
				{
					d.Focus(state);
				}
			}
			catch { }
		});
	}
}
