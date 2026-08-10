using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media.Imaging;

namespace AutoOS.App.Converters;

public partial class FormFactorToImageIconConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, string language)
	{
		uint formFactor = 0;
		if (value is uint u)
			formFactor = u;
		else if (value is int i)
			formFactor = (uint)i;

		string icon = formFactor switch
		{
			3 => "Headphones.png",
			4 or 5 => "Microphone.png",
			_ => "Speaker.png"
		};

		return new ImageIcon
		{
			Source = new BitmapImage(new Uri($"ms-appx:///Assets/FluentIcons/Pages/Sound/{icon}"))
		};
	}

	public object ConvertBack(object value, Type targetType, object parameter, string language)
		=> throw new NotImplementedException();
}
