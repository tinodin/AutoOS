using Microsoft.UI.Xaml.Data;

namespace AutoOS.App.Converters;

public partial class CurrentVersionConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, string language)
	{
		if (value is string version && !string.IsNullOrEmpty(version))
		{
			return $"Current Version: {version}";
		}

		return "N/A";
	}

	public object ConvertBack(object value, Type targetType, object parameter, string language)
		=> throw new NotImplementedException();
}
