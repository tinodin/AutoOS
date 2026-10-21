namespace AutoOS.App.UserControls;

public partial class PreviewModuleViewModel : ObservableObject
{
	public ObservableCollection<object> Items { get; } = [];

	public ObservableCollection<object> PreviewItems { get; } = [];

	[ObservableProperty]
	public partial PreviewType PreviewMode { get; set; }

	partial void OnPreviewModeChanged(PreviewType value)
	{
		RebuildPreviewItems();
	}

	internal void RebuildPreviewItems()
	{
		PreviewItems.Clear();

		foreach (object item in Items)
		{
			if (PreviewMode != PreviewType.VideoPlayerSource || item is VideoPlayerSource)
			{
				PreviewItems.Add(item);
			}
		}

		OnPropertyChanged(nameof(PreviewItems));
	}
}
