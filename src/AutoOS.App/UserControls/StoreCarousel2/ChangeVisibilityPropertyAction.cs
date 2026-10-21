using Microsoft.UI.Xaml;

namespace AutoOS.App.UserControls;

public sealed partial class ChangeVisibilityPropertyAction : ChangePropertyAction<UIElement, Visibility>
{
	protected override void Change(UIElement target, Visibility value)
	{
		target.Visibility = value;
	}
}
