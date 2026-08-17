using Microsoft.UI.Xaml;

namespace AutoOS.App.UserControls;

public class MediaCarouselClickedEventArgs : EventArgs
{
	public IReadOnlyList<MediaCarouselItem> Items { get; }

	public int Index { get; }

	public FrameworkElement SourceElement { get; }

	public MediaCarouselClickedEventArgs(IReadOnlyList<MediaCarouselItem> items, int index, FrameworkElement sourceElement)
	{
		Items = items;
		Index = index;
		SourceElement = sourceElement;
	}
}
