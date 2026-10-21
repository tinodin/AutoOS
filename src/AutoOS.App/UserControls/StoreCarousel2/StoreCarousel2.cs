using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;

namespace AutoOS.App.UserControls.StoreCarousel2;

[TemplatePart(Name = "ScreenshotsControl", Type = typeof(ScreenshotsViewer))]
public partial class StoreCarousel2 : Control
{
	private ScreenshotsViewer? _card;
	private PreviewViewer? _viewer;
	private PreviewViewerHelper? _helper;
	private PreviewViewerHelperListViewSource? _listViewSource;
	private PreviewItemsViewModel? _viewerVm;
	private PreviewItemsViewModel? _hookedViewerViewModel;
	private PreviewModuleViewModel? _hookedPreviewViewModel;
	private int _viewerTransitionVersion;

	public StoreCarousel2()
	{
		DefaultStyleKey = typeof(StoreCarousel2);
	}

	public event EventHandler? ScreenshotClicked;
	public event EventHandler? SignInClicked;

	protected override void OnApplyTemplate()
	{
		base.OnApplyTemplate();

		DetachCard();

		_card = GetTemplateChild("ScreenshotsControl") as ScreenshotsViewer;
		if (_card != null)
		{
			_card.ScreenshotClicked += OnScreenshotClicked;
			_card.SignInClicked += OnSignInClicked;
			if (ItemsSource != null)
			{
				var vm = new PreviewModuleViewModel();
				foreach (object item in ItemsSource)
					vm.Items.Add(item);
				_card.PreviewViewModel = vm;
			}
			_card.AgeRestricted = AgeRestricted;
			_card.IsAgeBlocked = IsAgeBlocked;

			EnsureHelper();
		}
	}

	public void OpenPreview(int index = 0)
	{
		if (AgeRestricted || _card == null)
		{
			return;
		}

		EnsureHelper();

		_viewer?.XamlRoot = XamlRoot;

		if (_helper == null || _viewerVm == null || _viewerVm.Items.Count == 0)
		{
			return;
		}

		if (index < 0 || index >= _viewerVm.Items.Count)
		{
			index = 0;
		}

		object dataItem = _viewerVm.Items[index];
		_helper.SetUpPreviewViewer(dataItem);
	}

	private void EnsureHelper()
	{
		if (_card?.TileList == null)
		{
			return;
		}

		if (_viewer == null)
		{
			_viewer = new PreviewViewer();
			_viewer.Closed += OnViewerClosed;
		}

		_viewer.SetThemeSource(this);

		_listViewSource ??= new PreviewViewerHelperListViewSource(_card.TileList);
		_listViewSource.InnerListView = _card.TileList;

		EnsureViewerViewModel();
		HookPreviewViewModel();

		if (_helper == null)
		{
			_helper = new PreviewViewerHelper
			{
				PreviewViewer = _viewer,
				ListViewSource = _listViewSource
			};
		}
		else
		{
			_helper.PreviewViewer = _viewer;
			_helper.ListViewSource = _listViewSource;
		}

		if (!ReferenceEquals(_hookedViewerViewModel, _viewerVm))
		{
			_hookedViewerViewModel?.PropertyChanged -= _helper.OnPopupPreviewSelectionChanged;
			_hookedViewerViewModel = _viewerVm;
			_hookedViewerViewModel?.PropertyChanged += _helper.OnPopupPreviewSelectionChanged;
		}

		if (!ReferenceEquals(_viewer.ViewModel, _viewerVm))
		{
			_viewer.ViewModel = _viewerVm;
		}

		_viewer.AgeRestricted = AgeRestricted;
		_viewer.IsAgeBlocked = IsAgeBlocked;
		_card.Helper = _helper;
	}

	private void EnsureViewerViewModel()
	{
		if (_viewerVm == null)
		{
			_viewerVm = new PreviewItemsViewModel();
			FillViewerViewModelItems();
			return;
		}

		if (_viewerVm.Items.Count == 0)
		{
			FillViewerViewModelItems();
		}
	}

	private void FillViewerViewModelItems()
	{
		if (_viewerVm == null)
		{
			return;
		}

		ObservableCollection<object>? previewItems = _card?.PreviewViewModel?.PreviewItems;
		if (previewItems != null && previewItems.Count > 0)
		{
			foreach (object item in previewItems)
			{
				_viewerVm.Items.Add(item);
			}

			return;
		}

		if (ItemsSource != null)
		{
			foreach (object item in ItemsSource)
			{
				_viewerVm.Items.Add(item);
			}
		}
		else if (_card?.PreviewViewModel?.Items != null)
		{
			foreach (object item in _card.PreviewViewModel.Items)
			{
				_viewerVm.Items.Add(item);
			}
		}
	}

	private void ReplaceViewerViewModelItems()
	{
		_viewerVm ??= new PreviewItemsViewModel();
		_viewerVm.Items.Clear();
		FillViewerViewModelItems();

		_viewer?.ViewModel = _viewerVm;
	}

	private void HookPreviewViewModel()
	{
		PreviewModuleViewModel? viewModel = _card?.PreviewViewModel;
		if (ReferenceEquals(_hookedPreviewViewModel, viewModel))
		{
			return;
		}

		UnhookPreviewViewModel();

		_hookedPreviewViewModel = viewModel;

		_hookedPreviewViewModel?.PropertyChanged += OnPreviewViewModelPropertyChanged;
	}

	private void UnhookPreviewViewModel()
	{
		_hookedPreviewViewModel?.PropertyChanged -= OnPreviewViewModelPropertyChanged;
		_hookedPreviewViewModel = null;
	}

	private void OnPreviewViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
	{
		if (string.Equals(e?.PropertyName, nameof(PreviewModuleViewModel.PreviewItems), StringComparison.Ordinal))
		{
			ReplaceViewerViewModelItems();
		}
	}

	private void DetachCard()
	{
		if (_card != null)
		{
			_card.ScreenshotClicked -= OnScreenshotClicked;
			_card.SignInClicked -= OnSignInClicked;
			_card.Helper = null;
			_card = null;
		}

		UnhookPreviewViewModel();
	}

	private void OnSignInClicked(object? sender, EventArgs e) => SignInClicked?.Invoke(this, EventArgs.Empty);

	private void OnScreenshotClicked(object? sender, object e)
	{

		ScreenshotClicked?.Invoke(this, EventArgs.Empty);

		EnsureHelper();
		_viewer?.XamlRoot = XamlRoot;
	}

	private void OnViewerClosed(object? sender, EventArgs e)
	{
		if (_viewer == null || _card == null)
		{
			return;
		}

		int finalIndex = _viewer.SelectedIndex;
		int transitionVersion = ++_viewerTransitionVersion;

		_listViewSource?.RestoreSourceVisibility();

		if (finalIndex >= 0)
		{
			_card.FocusItem(finalIndex, FocusState.Programmatic);
		}

		if (_card.TileList != null)
		{
			_ = _card.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, async () =>
			{
				if (transitionVersion != _viewerTransitionVersion || _viewer?.IsOpen == true)
				{
					return;
				}

				object? item = (finalIndex >= 0 && finalIndex < _card.TileList.Items.Count) ? _card.TileList.Items[finalIndex] : null;

				if (_listViewSource != null && item != null)
				{
					await _listViewSource.StartBackwardConnectedAnimationAsync(item);
				}
				else
				{
					UIElement? target = null;
					try
					{
						if (finalIndex >= 0 && finalIndex < _card.TileList.Items.Count && _card.TileList.ContainerFromIndex(finalIndex) is FrameworkElement container)
						{
							target = container.FindDescendant<Image>() ?? container.FindDescendant<TileImage>() ?? container;
						}
					}
					catch { }
					target ??= _card.TileList as UIElement ?? _card;

					if (target != null)
					{
						try
						{
							ConnectedAnimation? backAnim = ConnectedAnimationService.GetForCurrentView().GetAnimation("screenshotBackAnimation");
							if (backAnim != null)
							{
								backAnim.Configuration = new DirectConnectedAnimationConfiguration();
								backAnim.TryStart(target);
							}
						}
						catch { }
					}
				}

				if (finalIndex >= 0)
				{
					_card.FocusItem(finalIndex, FocusState.Programmatic);
				}
			});
		}
	}
}
