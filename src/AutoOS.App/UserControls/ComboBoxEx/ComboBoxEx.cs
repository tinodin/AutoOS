using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace AutoOS.App.UserControls;

public partial class ComboBoxEx : ComboBox
{
	private double _cachedWidth;

	protected override void OnDropDownOpened(object e)
	{
		Width = _cachedWidth;
		base.OnDropDownOpened(e);
	}

	protected override void OnDropDownClosed(object e)
	{
		Width = double.NaN;
		base.OnDropDownClosed(e);
	}

	protected override Size MeasureOverride(Size availableSize)
	{
		Size baseSize = base.MeasureOverride(availableSize);
		if (baseSize.Width != 64)
			_cachedWidth = baseSize.Width;
		return baseSize;
	}
}
