using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media.Imaging;

namespace AutoOS.Core.Helpers.GPU.Converters;

public partial class VendorIdToImageIconConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, string language)
	{
		string vendorId = value as string;
		string icon = vendorId switch
		{
			"10de" => "Nvidia.png",
			"1002" => "Amd.png",
			"8086" => "Intel.png",
			_ => null
		};

		if (string.IsNullOrEmpty(icon)) return null;

		return new ImageIcon
		{
			Source = new BitmapImage(new Uri($"ms-appx:///Assets/FluentIcons/Pages/Graphics/{icon}"))
		};
	}

	public object ConvertBack(object value, Type targetType, object parameter, string language)
		=> throw new NotImplementedException();
}
