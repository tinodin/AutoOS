using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using System.ComponentModel;

namespace AutoOS.App.Data.Contracts;

public interface IAppearanceSettingsService : INotifyPropertyChanged
{
	/// <summary>
	/// Gets or sets a value for the app background image source
	/// </summary>
	string AppThemeBackgroundImageSource { get; set; }

	/// <summary>
	/// Gets or sets a value for the app background image fit.
	/// </summary>
	Stretch AppThemeBackgroundImageFit { get; set; }

	/// <summary>
	/// Gets or sets a value for the app background image opacity.
	/// </summary>
	float AppThemeBackgroundImageOpacity { get; set; }

	/// <summary>
	/// Gets or sets a value for the app background image Vertical Alignment.
	/// </summary>
	VerticalAlignment AppThemeBackgroundImageVerticalAlignment { get; set; }

	/// <summary>
	/// Gets or sets a value for the app background image Horizontal Alignment.
	/// </summary>
	HorizontalAlignment AppThemeBackgroundImageHorizontalAlignment { get; set; }
}
