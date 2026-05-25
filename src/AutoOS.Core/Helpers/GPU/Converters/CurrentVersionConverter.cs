using Microsoft.UI.Xaml.Data;

namespace AutoOS.Core.Helpers.GPU.Converters;

public partial class CurrentVersionConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        string version = value as string;
        if (string.IsNullOrEmpty(version))
            return "N/A";

        return $"Current Version: {version}";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}
