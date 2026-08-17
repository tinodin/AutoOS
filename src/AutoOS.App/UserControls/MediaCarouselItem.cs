using Windows.Media.Core;

namespace AutoOS.App.UserControls;

public class MediaCarouselItem
{
	public string ImageUrl { get; set; } = string.Empty;

	public string Label { get; set; } = string.Empty;

	public bool IsVideo { get; set; }

	public MediaSource? MediaSource { get; set; }
}
