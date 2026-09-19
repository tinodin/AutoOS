using AutoOS.App.Helpers;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace AutoOS.App.Converters;

public sealed partial class ExtensionToColorBrushConverter : IValueConverter
{
	private static readonly Dictionary<string, SolidColorBrush> BrushCache = new(StringComparer.OrdinalIgnoreCase);

	public object Convert(object value, Type targetType, object parameter, string language)
	{
		string extension = value as string ?? string.Empty;

		string cacheKey = extension.Length == 0 ? "###NOEXT###" : extension;
		if (BrushCache.TryGetValue(cacheKey, out var cached))
			return cached;

		Color color = ExtensionColorHelper.GetColor(extension);
		var brush = new SolidColorBrush(color);
		BrushCache[cacheKey] = brush;
		return brush;
	}

	public object ConvertBack(object value, Type targetType, object parameter, string language)
		=> throw new NotImplementedException();
}
