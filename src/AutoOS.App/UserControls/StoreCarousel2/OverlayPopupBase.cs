using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
namespace AutoOS.App.UserControls.StoreCarousel2;

public partial class OverlayPopupBase : UserControl
{
	public static readonly DependencyProperty CloseOnNavigateBackProperty =
		DependencyProperty.Register(nameof(CloseOnNavigateBack), typeof(bool), typeof(OverlayPopupBase), new PropertyMetadata(false));

	public bool CloseOnNavigateBack
	{
		get => (bool)GetValue(CloseOnNavigateBackProperty);
		set => SetValue(CloseOnNavigateBackProperty, value);
	}

	public static readonly DependencyProperty IsFullScreenPopupProperty =
		DependencyProperty.Register(nameof(IsFullScreenPopup), typeof(bool), typeof(OverlayPopupBase), new PropertyMetadata(false));

	public bool IsFullScreenPopup
	{
		get => (bool)GetValue(IsFullScreenPopupProperty);
		set => SetValue(IsFullScreenPopupProperty, value);
	}
}
