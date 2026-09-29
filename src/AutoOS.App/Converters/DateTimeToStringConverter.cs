using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace AutoOS.App.Converters;

/// <summary>
/// Renders a scanned timestamp as a culture-invariant local time, so the File View and Tree View
/// date columns stay stable and sortable-looking. <see cref="DateTime.MinValue"/> renders empty.
/// </summary>
public sealed partial class DateTimeToStringConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, string language)
	{
		if (value is not DateTime dateTime || dateTime == DateTime.MinValue)
			return string.Empty;

		// Scanners store UTC; render local for visible rows only.
		// WizTree format with seconds (16/09/2026 16:14:41).
		DateTime local = dateTime.Kind == DateTimeKind.Utc ? dateTime.ToLocalTime() : dateTime;
		return local.ToString("dd/MM/yyyy HH:mm:ss");
	}

	public object ConvertBack(object value, Type targetType, object parameter, string language) => DependencyProperty.UnsetValue;
}
