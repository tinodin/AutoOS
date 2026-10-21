namespace AutoOS.App.UserControls;

public partial class PreviewItemsViewModel : ObservableObject
{
	public PreviewItemsViewModel()
	{
		Items = [];
		SelectedIndex = -1;
	}

	[ObservableProperty]
	public partial ObservableCollection<object> Items { get; set; }

	[ObservableProperty]
	public partial object? CurrentItem { get; set; }

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(Caption), nameof(CurrentIndex), nameof(TotalIndex))]
	public partial int SelectedIndex { get; set; }

	public string Caption
	{
		get
		{
			if (CurrentItem is ScreenshotTileItem screenshot)
			{
				return screenshot.Title ?? string.Empty;
			}
			if (CurrentItem is VideoPlayerSource video)
			{
				return video.Title ?? string.Empty;
			}
			return string.Empty;
		}
	}

	public string CurrentIndex => SelectedIndex >= 0 ? (SelectedIndex + 1).ToString() : "1";

	public string TotalIndex => Items != null ? Items.Count.ToString() : "0";

	partial void OnItemsChanged(ObservableCollection<object> value)
	{
		OnPropertyChanged(nameof(TotalIndex));
	}

	partial void OnSelectedIndexChanged(int oldValue, int newValue)
	{
		if (Items != null && newValue >= 0 && newValue < Items.Count)
		{
			CurrentItem = Items[newValue];
		}

		if (Items != null && oldValue >= 0 && oldValue < Items.Count)
		{
			if (Items[oldValue] is VideoPlayerSource oldVideo)
			{
				try
				{
					if (oldVideo.MediaPlayer?.PlaybackSession?.CanPause == true)
					{
						oldVideo.MediaPlayer.Pause();
					}
				}
				catch { }
			}
		}
	}
}
