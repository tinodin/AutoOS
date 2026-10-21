using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Xaml.Interactivity;

namespace AutoOS.App.UserControls.StoreCarousel2;

public sealed partial class ImageFailedEventTriggerBehavior : Trigger<Image>
{
	protected override void OnAttached()
	{
		base.OnAttached();
		AssociatedObject?.ImageFailed += OnImageFailed;
	}

	protected override void OnDetaching()
	{
		base.OnDetaching();
		AssociatedObject?.ImageFailed -= OnImageFailed;
	}

	private void OnImageFailed(object sender, ExceptionRoutedEventArgs e)
	{
		Interaction.ExecuteActions(sender, Actions, e);
	}
}
