using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Animation;
using Windows.Media.Playback;
using Windows.System;

namespace AutoOS.App.UserControls.StoreCarousel2;

public sealed partial class PreviewViewer : OverlayPopupBase
{
	private const string ForwardAnimationKey = "screenshotForwardAnimation";
	private const string BackAnimationKey = "screenshotBackAnimation";

	private readonly Popup _popup;
	private Grid? _viewerRoot;
	private Button? _closeButton;
	private FlipView? _flipView;
	private ScrollViewer? _scrollingHost;
	private TextBlock? _captionTextBlock;
	private TextBlock? _counterTextBlock;
	private Run? _currentIndexRun;
	private Run? _totalIndexRun;
	private FrameworkElement? _themeSource;
	private int _pendingSelectedIndex;
	private int _selectionRequestVersion;
	private bool _pendingSelectionSet;
	private bool _syncingContainerSize;
	private int _videoPlayGeneration;
	private static PreviewViewer? _currentOverlay;

	public PreviewViewer()
	{
		InitializeComponent();

		AttachParts();
		Loaded += OnLoaded;

		_popup = new Popup { IsLightDismissEnabled = false, ShouldConstrainToRootBounds = true };

		_flipView?.LayoutUpdated += OnFlipViewLayoutUpdated;
	}

	public event EventHandler? Closed;

	public bool IsOpen { get; private set; }

	public int SelectedIndex
	{
		get => _pendingSelectionSet
			? _pendingSelectedIndex
			: (_flipView != null && _flipView.SelectedIndex >= 0 ? _flipView.SelectedIndex : _pendingSelectedIndex);
		set
		{
			_pendingSelectedIndex = Math.Max(0, value);
			_pendingSelectionSet = true;
			_selectionRequestVersion++;
			SchedulePendingSelection();
		}
	}

	public static readonly DependencyProperty AgeRestrictedProperty =
		DependencyProperty.Register(nameof(AgeRestricted), typeof(bool), typeof(PreviewViewer), new PropertyMetadata(false, OnAgeRestrictedChanged));

	public bool AgeRestricted
	{
		get => (bool)GetValue(AgeRestrictedProperty);
		set => SetValue(AgeRestrictedProperty, value);
	}

	private static void OnAgeRestrictedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		var viewer = (PreviewViewer)d;
		viewer.ApplyAgeRestriction();
	}

	public static readonly DependencyProperty IsAgeBlockedProperty =
		DependencyProperty.Register(nameof(IsAgeBlocked), typeof(bool), typeof(PreviewViewer), new PropertyMetadata(false, OnIsAgeBlockedChanged));

	public bool IsAgeBlocked
	{
		get => (bool)GetValue(IsAgeBlockedProperty);
		set => SetValue(IsAgeBlockedProperty, value);
	}

	private static void OnIsAgeBlockedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		var viewer = (PreviewViewer)d;
		viewer.ApplyAgeRestriction();
	}

	public PreviewItemsViewModel? ViewModel
	{
		get;
		set
		{
			if (ReferenceEquals(field, value))
			{
				return;
			}

			field?.PropertyChanged -= OnViewModelPropertyChanged;

			field = value;

			if (field != null)
			{
				field.PropertyChanged += OnViewModelPropertyChanged;

				if (_flipView != null)
				{
					BindFlipViewItemsSource(field);
				}
				else
				{
					OnViewModelPropertyChanged(field, new PropertyChangedEventArgs(string.Empty));
				}
			}
		}
	}

	internal void AttachToOverlayHost()
	{
		if (_popup != null && _popup.Child == null)
		{
			_popup.Child = this;
		}
	}

	internal void DetachFromOverlayHost()
	{
		if (_popup != null && _popup.Child != null)
		{
			_popup.Child = null;
		}
	}

	internal void SetThemeSource(FrameworkElement source)
	{
		if (source == null)
		{
			return;
		}

		if (!ReferenceEquals(_themeSource, source))
		{
			_themeSource?.ActualThemeChanged -= OnThemeSourceActualThemeChanged;

			_themeSource = source;
			_themeSource.ActualThemeChanged += OnThemeSourceActualThemeChanged;
		}

		ApplyThemeFromSource();
	}

	public void OpenOverlayPopup()
	{
		Show();
	}

	public void Show()
	{
		if (XamlRoot != null)
		{
			_popup.XamlRoot = XamlRoot;
			UpdateOverlaySize();
			XamlRoot.Changed -= OnXamlRootChanged;
			XamlRoot.Changed += OnXamlRootChanged;
		}

		if (!_popup.IsOpen)
		{
			_popup.IsOpen = true;
		}

		if (_flipView != null && ViewModel != null && _flipView.ItemsSource != ViewModel.Items)
		{
			BindFlipViewItemsSource(ViewModel);
		}
		else
		{
			ApplyViewModelState(null);
		}

		ApplySelection();

		PreviewViewer? current = _currentOverlay;
		current?.Close();

		_currentOverlay = this;
		AttachToOverlayHost();
		Visibility = Visibility.Visible;
		IsOpen = true;

		FocusFlipViewProgrammatic();

		TryStartForwardConnectedAnimation();
		if (_flipView != null)
		{
			_ = _flipView.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () => EnsureCurrentVideoStarted());
		}
	}

	public void Close()
	{
		Hide();
	}

	public void Hide()
	{
		if (!IsOpen)
			return;

		FrameworkElement? currentContainer = _flipView != null && _flipView.SelectedIndex >= 0 ? SafeContainerFromIndex(_flipView.SelectedIndex) as FrameworkElement : null;

		PrepareBackAnimation(currentContainer);

		TeardownAllVideos();

		Closed?.Invoke(this, EventArgs.Empty);

		RemoveOverlayPopup();
	}

	public async void TryStartForwardConnectedAnimation()
	{
		if (_flipView == null)
		{
			return;
		}

		ConnectedAnimation? animation = ConnectedAnimationService.GetForCurrentView().GetAnimation(ForwardAnimationKey);
		if (animation == null)
		{
			FocusFlipViewProgrammatic();
			return;
		}

		animation.Configuration = new DirectConnectedAnimationConfiguration();

		FrameworkElement? container = null;
		for (int attempt = 0; attempt < 6; attempt++)
		{
			await Task.Delay(15);
			int idx = _flipView != null && _flipView.SelectedIndex >= 0 ? _flipView.SelectedIndex : SelectedIndex;
			container = SafeContainerFromIndex(idx) as FrameworkElement;
			if (container != null && container.ActualWidth > 0 && container.ActualHeight > 0)
			{
				break;
			}
		}

		if (container != null)
		{
			UIElement target = container;
			MediaPlayerElement? player = container.FindDescendant<MediaPlayerElement>();
			double duration = player != null && player.MediaPlayer != null ? player.MediaPlayer.NaturalDuration.TotalSeconds : 0;

			if (player != null && duration > 0)
			{
				target = player;
			}
			else if (player != null)
			{
				target = container.FindDescendant<PreviewViewerVideoItem>() ?? container;
			}
			else
			{
				target = container.FindDescendant<Image>() ?? container.FindDescendant<TileImage>() ?? container;
			}

			try
			{
				animation.TryStart(target);
			}
			catch { }
		}

		FocusFlipViewProgrammatic();
		if (_flipView != null)
		{
			_ = _flipView.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () => EnsureCurrentVideoStarted());
		}
	}

	private void OnThemeSourceActualThemeChanged(FrameworkElement sender, object args) => ApplyThemeFromSource();

	private void ApplyThemeFromSource()
	{
		if (_themeSource != null)
		{
			RequestedTheme = _themeSource.ActualTheme;
		}
	}

	private void UpdateOverlaySize()
	{
		if (XamlRoot == null)
		{
			return;
		}

		Width = XamlRoot.Size.Width;
		Height = XamlRoot.Size.Height;
	}

	private void OnFlipViewLayoutUpdated(object? sender, object args)
	{
		if (IsOpen)
		{
			SyncContainerSizeToViewport();
		}
	}

	private void SyncContainerSizeToViewport()
	{
		if (_flipView == null || _syncingContainerSize)
		{
			return;
		}

		_scrollingHost ??= _flipView.FindDescendant<ScrollViewer>();
		if (_scrollingHost == null || _scrollingHost.ActualWidth <= 0 || _scrollingHost.ActualHeight <= 0)
		{
			return;
		}

		double width = _scrollingHost.ActualWidth;
		double height = _scrollingHost.ActualHeight;

		_syncingContainerSize = true;
		try
		{
			foreach (FrameworkElement container in SnapshotRealizedContainers())
			{
				if (Math.Abs(container.Width - width) > 0.01)
				{
					container.Width = width;
				}

				if (Math.Abs(container.Height - height) > 0.01)
				{
					container.Height = height;
				}
			}
		}
		finally
		{
			_syncingContainerSize = false;
		}
	}

	private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		ApplyViewModelState(e?.PropertyName);
	}

	private void ApplyViewModelState(string? prop)
	{
		PreviewItemsViewModel? vm = ViewModel;
		if (vm == null)
			return;

		bool updateAll = string.IsNullOrEmpty(prop);

		if (updateAll || string.Equals(prop, "SelectedIndex", StringComparison.Ordinal))
		{
			if (_flipView != null && vm.SelectedIndex >= 0 && vm.SelectedIndex < (_flipView.Items?.Count ?? 0))
			{
				if (_flipView.SelectedIndex != vm.SelectedIndex)
				{
					_flipView.SelectionChanged -= OnSelectionChanged;
					try
					{
						_flipView.SelectedIndex = vm.SelectedIndex;
					}
					finally
					{
						_flipView.SelectionChanged += OnSelectionChanged;
					}
				}
			}
		}

		SyncCaptionAndIndexText(updateAll, prop);
	}

	private void SyncCaptionAndIndexText(bool updateAll, string? prop)
	{
		PreviewItemsViewModel? vm = ViewModel;
		if (vm == null)
			return;

		if (updateAll || string.Equals(prop, "Caption", StringComparison.Ordinal))
		{
			if (_captionTextBlock != null)
			{
				string caption = vm.Caption;
				_captionTextBlock.Text = caption ?? string.Empty;
				_captionTextBlock.Visibility = string.IsNullOrEmpty(caption) ? Visibility.Collapsed : Visibility.Visible;
			}
		}

		if (updateAll || string.Equals(prop, "CurrentIndex", StringComparison.Ordinal))
		{
			_currentIndexRun?.Text = vm.CurrentIndex;
		}

		if (updateAll || string.Equals(prop, "TotalIndex", StringComparison.Ordinal))
		{
			_totalIndexRun?.Text = vm.TotalIndex;
		}
	}

	private void ApplySelection()
	{
		if (_flipView == null || _flipView.Items == null || _flipView.Items.Count == 0)
			return;

		int index = -1;
		if (ViewModel != null && ViewModel.SelectedIndex >= 0)
			index = Math.Min(ViewModel.SelectedIndex, _flipView.Items.Count - 1);
		if (index < 0 && ViewModel?.CurrentItem != null)
			index = _flipView.Items.IndexOf(ViewModel.CurrentItem);
		if (index < 0 && _pendingSelectionSet)
			index = Math.Min(_pendingSelectedIndex, _flipView.Items.Count - 1);
		if (index < 0)
			return;

		try
		{
			if (_flipView.SelectedIndex != index)
			{
				_flipView.SelectionChanged -= OnSelectionChanged;
				try
				{
					_flipView.SelectedIndex = index;
				}
				finally
				{
					_flipView.SelectionChanged += OnSelectionChanged;
				}
			}
			_pendingSelectedIndex = index;
			_pendingSelectionSet = false;
		}
		catch (ArgumentException)
		{
			_pendingSelectedIndex = index;
			_pendingSelectionSet = true;
		}
	}

	private void SchedulePendingSelection()
	{
		if (_flipView == null || !_pendingSelectionSet)
			return;

		ApplyPendingSelection(_selectionRequestVersion);
	}

	private void ApplyPendingSelection(int requestVersion)
	{
		if (_flipView == null || !_pendingSelectionSet || requestVersion != _selectionRequestVersion)
			return;

		if (_flipView.Items == null || _flipView.Items.Count == 0)
			return;

		int index = Math.Min(_pendingSelectedIndex, _flipView.Items.Count - 1);
		try
		{
			_flipView.SelectedIndex = index;
			_pendingSelectedIndex = index;
			_pendingSelectionSet = false;
		}
		catch (ArgumentException)
		{
			SchedulePendingSelection();
		}
	}

	private void AttachParts()
	{
		_viewerRoot = FindName("ViewerGrid") as Grid;
		_closeButton = FindName("CloseButton") as Button;
		var flipView = FindName("ItemsFlipView") as FlipView;
		_captionTextBlock = FindName("CaptionTextBlock") as TextBlock;
		var indexGrid = FindName("IndexTextBlock") as Grid;
		_counterTextBlock = indexGrid?.FindDescendant<TextBlock>();
		if (_counterTextBlock != null && _counterTextBlock.Inlines.Count >= 5)
		{
			_currentIndexRun = _counterTextBlock.Inlines[0] as Run;
			_totalIndexRun = _counterTextBlock.Inlines[4] as Run;
		}

		_flipView?.SelectionChanged -= OnSelectionChanged;

		_flipView = flipView;
		_flipView?.SelectionChanged += OnSelectionChanged;

		if (_viewerRoot != null)
		{
			_viewerRoot.KeyDown -= OnViewerKeyDown;
			_viewerRoot.KeyDown += OnViewerKeyDown;
		}

		if (_closeButton != null)
		{
			_closeButton.Click -= OnCloseClicked;
			_closeButton.Click += OnCloseClicked;
		}
	}

	private void OnLoaded(object sender, RoutedEventArgs e)
	{
		AttachParts();
		ApplyAgeRestriction();

		if (_flipView != null && ViewModel != null && _flipView.ItemsSource != ViewModel.Items)
		{
			BindFlipViewItemsSource(ViewModel);
		}

		SyncCaptionAndIndexText(true, null);
	}

	private void BindFlipViewItemsSource(PreviewItemsViewModel vm)
	{
		if (_flipView == null || vm == null)
		{
			return;
		}

		_flipView.SelectionChanged -= OnSelectionChanged;
		try
		{
			_flipView.ItemsSource = vm.Items;
			OnViewModelPropertyChanged(vm, new PropertyChangedEventArgs(string.Empty));
		}
		finally
		{
			_flipView.SelectionChanged += OnSelectionChanged;
		}
	}

	private List<FrameworkElement> SnapshotRealizedContainers()
	{
		var result = new List<FrameworkElement>();
		if (_flipView?.Items == null)
		{
			return result;
		}

		for (int i = 0; i < _flipView.Items.Count; i++)
		{
			if (SafeContainerFromIndex(i) is FrameworkElement container)
			{
				result.Add(container);
			}
		}

		return result;
	}

	private void TeardownAllVideos()
	{
		if (ViewModel?.Items != null)
		{
			for (int i = ViewModel.Items.Count - 1; i >= 0; i--)
			{
				if (ViewModel.Items[i] is VideoPlayerSource video)
				{
					video.TeardownPlayer();
				}
			}
		}
		else if (_flipView?.Items != null)
		{
			for (int i = _flipView.Items.Count - 1; i >= 0; i--)
			{
				if (_flipView.Items[i] is VideoPlayerSource video)
				{
					video.TeardownPlayer();
				}
			}
		}
	}

	private bool RemoveOverlayPopup()
	{
		if (ReferenceEquals(_currentOverlay, this))
		{
			Visibility = Visibility.Collapsed;
			_currentOverlay = null;
			DetachFromOverlayHost();
		}

		IsOpen = false;
		XamlRoot?.Changed -= OnXamlRootChanged;
		return true;
	}

	private static void PrepareBackAnimation(FrameworkElement? container)
	{
		if (container == null)
			return;

		try
		{
			ConnectedAnimation? backAnim = ConnectedAnimationService.GetForCurrentView().PrepareToAnimate(BackAnimationKey, container);
			backAnim?.Configuration = new DirectConnectedAnimationConfiguration();
		}
		catch { }
	}

	private void FocusFlipViewProgrammatic()
	{
		_flipView?.Focus(FocusState.Programmatic);
	}

	private DependencyObject? SafeContainerFromIndex(int index)
	{
		if (_flipView == null || index < 0 || _flipView.Items == null || index >= _flipView.Items.Count)
			return null;
		try
		{
			return _flipView.ContainerFromIndex(index);
		}
		catch
		{
			return null;
		}
	}

	private void ApplyAgeRestriction()
	{
		if (_flipView != null && ViewModel != null)
			ApplySelection();
	}

	private void OnXamlRootChanged(XamlRoot sender, XamlRootChangedEventArgs args) => UpdateOverlaySize();
	private void OnCloseClicked(object sender, RoutedEventArgs e) => Hide();
	private void OnViewerKeyDown(object sender, KeyRoutedEventArgs e)
	{
		if (e.Key == VirtualKey.Escape)
		{
			e.Handled = true;
			Hide();
		}
	}

	private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (_flipView != null && ViewModel != null)
		{
			int idx = _flipView.SelectedIndex;
			if (idx >= 0 && idx != ViewModel.SelectedIndex)
			{
				ViewModel.SelectedIndex = idx;
			}
		}

		if (e?.RemovedItems != null)
		{
			foreach (object item in e.RemovedItems)
			{
				if (item is VideoPlayerSource removedVideo)
				{
					try
					{
						if (removedVideo.MediaPlayer?.PlaybackSession?.CanPause == true)
						{
							removedVideo.MediaPlayer.Pause();
						}
					}
					catch { }
				}
			}
		}

		StopOtherVideos();
		UpdateCurrentVideoOnSelectionChanged();
	}

	private void UpdateCurrentVideoOnSelectionChanged()
	{
		if (_flipView == null)
			return;
		object? selectedItem = _flipView.SelectedItem;
		if (selectedItem is VideoPlayerSource videoSource)
		{
			FrameworkElement? container = SafeContainerFromIndex(_flipView.SelectedIndex) as FrameworkElement ?? _flipView.ContainerFromItem(selectedItem) as FrameworkElement;
			if (container != null)
			{
				if (container.FindDescendant<PreviewViewerVideoItem>() is { } videoItem)
				{
					videoItem.ApplySource();
					MediaPlayerElement? element = videoItem.VideoPlayerItemElement ?? container.FindDescendant<MediaPlayerElement>();
					element?.AutoPlay = true;
				}
			}

			ScheduleVideoResume(videoSource);
		}
	}

	private async void ScheduleVideoResume(VideoPlayerSource videoSource)
	{
		int generation = ++_videoPlayGeneration;

		await Task.Delay(1000);

		if (generation != _videoPlayGeneration || !IsOpen || _flipView == null)
		{
			return;
		}

		if (ReferenceEquals(_flipView.SelectedItem, videoSource))
		{
			MediaPlayer player = videoSource.MediaPlayer;
			if (player?.PlaybackSession?.PlaybackState == MediaPlaybackState.Paused)
			{
				try
				{
					player.Play();
				}
				catch { }
			}
		}
	}

	private async void EnsureCurrentVideoStarted()
	{
		if (_flipView == null)
		{
			return;
		}

		for (int attempt = 0; attempt < 6; attempt++)
		{
			if (attempt > 0)
			{
				await Task.Delay(15);
			}

			if (_flipView == null || !IsOpen)
			{
				return;
			}

			if (SafeContainerFromIndex(_flipView.SelectedIndex) is FrameworkElement container && (container.FindDescendant<PreviewViewerVideoItem>() != null || container.FindDescendant<MediaPlayerElement>() != null))
			{
				PlayCurrentVideo();
				return;
			}
		}

		PlayCurrentVideo();
	}

	private void PlayCurrentVideo()
	{
		if (_flipView == null || _flipView.SelectedIndex < 0 || _flipView.Items == null || _flipView.SelectedIndex >= _flipView.Items.Count)
			return;
		var container = SafeContainerFromIndex(_flipView.SelectedIndex) as FrameworkElement;
		if (container == null)
			return;

		if (_flipView.SelectedItem is VideoPlayerSource videoSource)
		{
			if (container.FindDescendant<PreviewViewerVideoItem>() is { } videoItem)
			{
				videoItem.ApplySource();
				MediaPlayerElement? element = videoItem.VideoPlayerItemElement ?? container.FindDescendant<MediaPlayerElement>();
				element?.AutoPlay = true;
			}

			try
			{
				videoSource.MediaPlayer?.Play();
			}
			catch { }
		}
	}

	private void StopOtherVideos()
	{
		if (ViewModel?.Items != null)
		{
			for (int i = 0; i < ViewModel.Items.Count; i++)
			{
				if (_flipView != null && i == _flipView.SelectedIndex)
					continue;
				if (ViewModel.Items[i] is VideoPlayerSource video)
				{
					try
					{
						if (video.MediaPlayer?.PlaybackSession?.CanPause == true)
						{
							video.MediaPlayer.Pause();
						}
					}
					catch { }
				}
			}
		}

		if (_flipView?.Items != null)
		{
			for (int i = 0; i < _flipView.Items.Count; i++)
			{
				if (i == _flipView.SelectedIndex)
					continue;
				if (SafeContainerFromIndex(i) is FrameworkElement container)
				{
					MediaPlayerElement? player = container.FindDescendant<MediaPlayerElement>();
					try
					{ player?.MediaPlayer?.Pause(); }
					catch { }
				}
			}
		}
	}
}
