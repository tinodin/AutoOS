namespace AutoOS.Common;

public partial class DropdownColorPicker : DevWinUI.DropdownColorPicker
{
	public DropdownColorPicker()
	{
		Loaded += OnDropdownColorPickerLoaded;
	}

	private void OnDropdownColorPickerLoaded(object sender, RoutedEventArgs e)
	{
		UpdateTintBox(Color);

		if (GetTemplateChild("PART_ColorPicker") is ColorPicker colorPicker)
		{
			colorPicker.ColorChanged -= InnerColorPicker_ColorChanged;
			colorPicker.ColorChanged += InnerColorPicker_ColorChanged;
		}

		if (GetTemplateChild("PART_Flyout") is Flyout flyout)
		{
			flyout.Opened -= Flyout_Opened;
			flyout.Opened += Flyout_Opened;
		}
	}

	private void Flyout_Opened(object sender, object e)
	{
		if (GetTemplateChild("PART_ColorPicker") is ColorPicker colorPicker)
		{
			colorPicker.ColorChanged -= InnerColorPicker_ColorChanged;
			colorPicker.ColorChanged += InnerColorPicker_ColorChanged;
		}
	}

	private void InnerColorPicker_ColorChanged(ColorPicker sender, ColorChangedEventArgs args)
	{
		Color = args.NewColor;
		UpdateTintBox(args.NewColor);
	}

	private void UpdateTintBox(Windows.UI.Color color)
	{
		if (GetTemplateChild("PART_Rectangle") is Microsoft.UI.Xaml.Shapes.Rectangle tintBox)
		{
			tintBox.Fill = new Microsoft.UI.Xaml.Media.SolidColorBrush(color);
		}
	}
}
