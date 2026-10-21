using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace AutoOS.App.UserControls.StoreCarousel2;

public partial class StaticListItemsPresenterWrapper : ContentControl
{
	public StaticListItemsPresenterWrapper()
	{
		DefaultStyleKey = typeof(ContentControl);
		IsTabStop = false;
		HorizontalContentAlignment = HorizontalAlignment.Stretch;
		VerticalContentAlignment = VerticalAlignment.Stretch;

		AddHandler(PointerWheelChangedEvent, new PointerEventHandler(OnPointerWheelChangedInternal), handledEventsToo: true);
	}

	protected override void OnPointerWheelChanged(PointerRoutedEventArgs e)
	{
		if (!e.GetCurrentPoint(this).Properties.IsHorizontalMouseWheel)
		{
			e.Handled = true;
		}
		base.OnPointerWheelChanged(e);
	}

	private void OnPointerWheelChangedInternal(object sender, PointerRoutedEventArgs e)
	{
		if (!e.GetCurrentPoint(this).Properties.IsHorizontalMouseWheel)
		{
			e.Handled = true;
		}
	}
}
