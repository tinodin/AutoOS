namespace AutoOS.App.UserControls.StoreCarousel2;

public partial class PreviewViewerHelper : ObservableObject
{
	public PreviewViewer? PreviewViewer { get; set; }

	public PreviewViewerHelperListViewSource? ListViewSource { get; set; }

	[ObservableProperty]
	public partial PreviewType PreviewMode { get; set; }

	public void SetUpPreviewViewer(object dataItem)
	{
		PreviewItemsViewModel? viewModel = PreviewViewer?.ViewModel;
		if (viewModel?.Items == null || dataItem == null)
		{
			return;
		}

		ListViewSource?.PrepareForwardConnectedAnimation(dataItem);

		viewModel.CurrentItem = dataItem;

		int index = viewModel.Items.IndexOf(dataItem);
		OpenPreviewViewer(index, dataItem);
	}

	public void OnPopupPreviewSelectionChanged(object? sender, PropertyChangedEventArgs e)
	{
		PreviewItemsViewModel? viewModel = PreviewViewer?.ViewModel;
		if (viewModel?.Items == null)
		{
			return;
		}

		if (!string.Equals(e?.PropertyName, nameof(PreviewItemsViewModel.SelectedIndex), StringComparison.Ordinal))
		{
			return;
		}

		int index = viewModel.SelectedIndex;
		if (index < 0 || index >= viewModel.Items.Count)
		{
			return;
		}

		ListViewSource?.ScrollToIndex(index);
	}

	public void OpenPreviewViewer(int index, object dataItem)
	{
		if (index < 0)
		{
			return;
		}

		PreviewItemsViewModel? viewModel = PreviewViewer?.ViewModel;
		if (viewModel?.Items == null)
		{
			return;
		}

		int count = viewModel.Items.Count;
		if (count <= 0)
		{
			return;
		}

		if (count <= index)
		{
			index = 0;
		}

		viewModel.SelectedIndex = index;

		PreviewViewer?.OpenOverlayPopup();
	}
}
