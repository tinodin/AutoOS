using AutoOS.App.Data.Contracts;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;

namespace AutoOS.App.Views;

public sealed partial class MainWindowViewModel : ObservableObject
{
	private readonly IAppearanceSettingsService AppearanceSettingsService;

	public MainWindowViewModel(IAppearanceSettingsService appearanceSettingsService)
	{
		AppearanceSettingsService = appearanceSettingsService;

		// Listen to service property changes and forward them
		AppearanceSettingsService.PropertyChanged += (s, e) =>
		{
			switch (e.PropertyName)
			{
				case nameof(AppearanceSettingsService.AppThemeBackgroundImageSource):
					OnPropertyChanged(nameof(BackgroundImageSource));
					break;
				case nameof(AppearanceSettingsService.AppThemeBackgroundImageOpacity):
					OnPropertyChanged(nameof(BackgroundImageOpacity));
					break;
				case nameof(AppearanceSettingsService.AppThemeBackgroundImageFit):
					OnPropertyChanged(nameof(BackgroundImageFit));
					break;
				case nameof(AppearanceSettingsService.AppThemeBackgroundImageVerticalAlignment):
					OnPropertyChanged(nameof(BackgroundImageVerticalAlignment));
					break;
				case nameof(AppearanceSettingsService.AppThemeBackgroundImageHorizontalAlignment):
					OnPropertyChanged(nameof(BackgroundImageHorizontalAlignment));
					break;
			}
		};
	}

	public ImageSource? BackgroundImageSource
	{
		get
		{
			if (string.IsNullOrWhiteSpace(AppearanceSettingsService.AppThemeBackgroundImageSource))
				return null;

			if (!Uri.TryCreate(AppearanceSettingsService.AppThemeBackgroundImageSource, UriKind.Absolute, out Uri? validUri))
				return null;

			try
			{
				return new BitmapImage(validUri);
			}
			catch
			{
				return null;
			}
		}
	}

	public double BackgroundImageOpacity
		=> AppearanceSettingsService.AppThemeBackgroundImageOpacity;

	public Stretch BackgroundImageFit
		=> AppearanceSettingsService.AppThemeBackgroundImageFit;

	public VerticalAlignment BackgroundImageVerticalAlignment
		=> AppearanceSettingsService.AppThemeBackgroundImageVerticalAlignment;

	public HorizontalAlignment BackgroundImageHorizontalAlignment
		=> AppearanceSettingsService.AppThemeBackgroundImageHorizontalAlignment;
}
