using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace AutoOS.App.UserControls;

public partial class ScreenshotDataTemplateSelector : DataTemplateSelector
{
	public DataTemplate? AgeRestrictedDataTemplate { get; set; }

	public DataTemplate? AgeRestrictedBlockedDataTemplate { get; set; }

	public DataTemplate? ScreenshotDataTemplate { get; set; }

	public DataTemplate? VideoItemDataTemplate { get; set; }

	public DataTemplate? VideoPlayerTemplate { get; set; }

	protected override DataTemplate? SelectTemplateCore(object item)
	{
		if (item is AgeRestrictedTileItem ageRestricted)
			return ageRestricted.IsAgeBlocked ? AgeRestrictedBlockedDataTemplate ?? AgeRestrictedDataTemplate : AgeRestrictedDataTemplate;

		if (item is VideoPlayerSource)
			return VideoItemDataTemplate ?? VideoPlayerTemplate;

		return ScreenshotDataTemplate;
	}

	protected override DataTemplate? SelectTemplateCore(object item, DependencyObject container) => SelectTemplateCore(item);
}
