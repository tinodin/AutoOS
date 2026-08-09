namespace AutoOS.App.UserControls;

public partial class DropdownColorPicker : DevWinUI.DropdownColorPicker
{
	private bool _isCleared;

	public DropdownColorPicker()
	{
		Loaded += OnDropdownColorPickerLoaded;
		RegisterPropertyChangedCallback(DevWinUI.DropdownColorPicker.ColorProperty, OnColorPropertyChanged);
	}

	private void OnColorPropertyChanged(DependencyObject sender, DependencyProperty dp)
	{
		UpdateVisuals(Color);
	}

	private void UpdateVisuals(Windows.UI.Color color)
	{
		if (color != Colors.White && color.A != 0)
			_isCleared = false;

		UpdateTintBox(_isCleared ? Colors.Transparent : color);

		if (GetTemplateChild("PART_ColorPicker") is ColorPicker colorPicker &&
			!colorPicker.Color.Equals(color))
		{
			colorPicker.Color = color;
		}
	}

	public void ResetColor()
	{
		_isCleared = true;
		ClearValue(DropdownColorPicker.ColorProperty);
		UpdateTintBox(Colors.Transparent);
	}

	private void OnDropdownColorPickerLoaded(object sender, RoutedEventArgs e)
	{
		UpdateVisuals(Color);

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

	private void Flyout_Opened(object? sender, object? e)
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
	}

	private void UpdateTintBox(Windows.UI.Color color)
	{
		if (GetTemplateChild("PART_Rectangle") is Microsoft.UI.Xaml.Shapes.Rectangle tintBox)
		{
			tintBox.Fill = new Microsoft.UI.Xaml.Media.SolidColorBrush(color);
		}
	}
}
