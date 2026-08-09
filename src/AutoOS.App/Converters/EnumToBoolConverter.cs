using Microsoft.UI.Xaml.Data;

namespace AutoOS.App.Converters;

public sealed partial class EnumToBoolConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, string language)
	{
		return value?.ToString() == parameter?.ToString();
	}

	public object ConvertBack(object value, Type targetType, object parameter, string language)
	{
		if (value is not true)
			return DependencyProperty.UnsetValue;

		return Enum.Parse(targetType, parameter?.ToString() ?? string.Empty);
	}
}
