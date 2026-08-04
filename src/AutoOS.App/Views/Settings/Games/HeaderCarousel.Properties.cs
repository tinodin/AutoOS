using Microsoft.UI.Xaml.Media;
using Windows.Media.Core;

namespace AutoOS.App.Views.Settings.Games;

[Microsoft.UI.Xaml.Data.Bindable]
public partial class HeaderCarousel
{
	public bool IsAutoScrollEnabled
	{
		get { return (bool)GetValue(IsAutoScrollEnabledProperty); }
		set { SetValue(IsAutoScrollEnabledProperty, value); }
	}

	public static readonly DependencyProperty IsAutoScrollEnabledProperty =
		DependencyProperty.Register(nameof(IsAutoScrollEnabled), typeof(bool), typeof(HeaderCarousel), new PropertyMetadata(true, OnIsAutoScrollEnabledChanged));

	private static void OnIsAutoScrollEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		var ctl = (HeaderCarousel)d;
		if (ctl != null)
		{
			ctl.ApplyAutoScroll();
		}
	}

	public string BackgroundImageUrl
	{
		get => (string)GetValue(BackgroundImageUrlProperty);
		set => SetValue(BackgroundImageUrlProperty, value);
	}

	public static readonly DependencyProperty BackgroundImageUrlProperty =
		DependencyProperty.Register(nameof(BackgroundImageUrl), typeof(string), typeof(HeaderCarousel), new PropertyMetadata(null));

	public string Title
	{
		get => (string)GetValue(TitleProperty);
		set => SetValue(TitleProperty, value);
	}
	public static readonly DependencyProperty TitleProperty =
		DependencyProperty.Register(nameof(Title), typeof(string), typeof(HeaderCarousel), new PropertyMetadata(defaultValue: null));

	public string Developers
	{
		get { return (string)GetValue(DevelopersProperty); }
		set { SetValue(DevelopersProperty, value); }
	}

	public static readonly DependencyProperty DevelopersProperty =
		DependencyProperty.Register("Developers", typeof(string), typeof(HeaderCarousel), new PropertyMetadata(null));

	public bool UpdateIsAvailable
	{
		get => (bool)GetValue(UpdateIsAvailableProperty);
		set
		{
			SetValue(UpdateIsAvailableProperty, value);
			UpdateButtonsVisibility();
		}
	}

	public static readonly DependencyProperty UpdateIsAvailableProperty =
		DependencyProperty.Register(nameof(UpdateIsAvailable), typeof(bool), typeof(HeaderCarousel), new PropertyMetadata(false));

	private void UpdateButtonsVisibility()
	{
		Update.Visibility = UpdateIsAvailable ? Visibility.Visible : Visibility.Collapsed;
		Play.Visibility = UpdateIsAvailable ? Visibility.Collapsed : Visibility.Visible;
	}

	public double Rating
	{
		get => (double)GetValue(RatingProperty);
		set => SetValue(RatingProperty, value);
	}

	public static readonly DependencyProperty RatingProperty =
		DependencyProperty.Register(nameof(Rating), typeof(double), typeof(HeaderCarousel), new PropertyMetadata(1.0));

	public string RoundedRating
	{
		get => (string)GetValue(RoundedRatingProperty);
		set => SetValue(RoundedRatingProperty, value);
	}

	public static readonly DependencyProperty RoundedRatingProperty =
		DependencyProperty.Register(nameof(RoundedRating), typeof(string), typeof(HeaderCarousel), new PropertyMetadata(null));

	public string PlayTime
	{
		get => (string)GetValue(PlayTimeProperty);
		set => SetValue(PlayTimeProperty, value);
	}

	public static readonly DependencyProperty PlayTimeProperty =
		DependencyProperty.Register(nameof(PlayTime), typeof(string), typeof(HeaderCarousel), new PropertyMetadata(null));

	public string AgeRatingUrl
	{
		get => (string)GetValue(AgeRatingUrlProperty);
		set => SetValue(AgeRatingUrlProperty, value);
	}

	public static readonly DependencyProperty AgeRatingUrlProperty =
		DependencyProperty.Register(nameof(AgeRatingUrl), typeof(string), typeof(HeaderCarousel), new PropertyMetadata(null));

	public string AgeRatingTitle
	{
		get => (string)GetValue(AgeRatingTitleProperty);
		set => SetValue(AgeRatingTitleProperty, value);
	}
	public static readonly DependencyProperty AgeRatingTitleProperty =
		DependencyProperty.Register(nameof(AgeRatingTitle), typeof(string), typeof(HeaderCarousel), new PropertyMetadata(defaultValue: null));

	public string AgeRatingDescription
	{
		get => (string)GetValue(AgeRatingDescriptionProperty);
		set => SetValue(AgeRatingDescriptionProperty, value);
	}
	public static readonly DependencyProperty AgeRatingDescriptionProperty =
		DependencyProperty.Register(nameof(AgeRatingDescription), typeof(string), typeof(HeaderCarousel), new PropertyMetadata(defaultValue: null));

	public string Elements
	{
		get => (string)GetValue(ElementsProperty);
		set => SetValue(ElementsProperty, value);
	}
	public static readonly DependencyProperty ElementsProperty =
		DependencyProperty.Register(nameof(Elements), typeof(string), typeof(HeaderCarousel), new PropertyMetadata(defaultValue: null));

	public IList<string> Genres
	{
		get => (IList<string>)GetValue(GenresProperty);
		set => SetValue(GenresProperty, value);
	}

	public static readonly DependencyProperty GenresProperty =
		DependencyProperty.Register(nameof(Genres), typeof(IList<string>), typeof(HeaderCarousel), new PropertyMetadata(new List<string>()));

	public IList<string> Features
	{
		get => (IList<string>)GetValue(FeaturesProperty);
		set => SetValue(FeaturesProperty, value);
	}

	public static readonly DependencyProperty FeaturesProperty =
	DependencyProperty.Register(nameof(Features), typeof(IList<string>), typeof(HeaderCarousel), new PropertyMetadata(new List<string>()));

	public string Description
	{
		get { return (string)GetValue(DescriptionProperty); }
		set { SetValue(DescriptionProperty, value); }
	}

	public static readonly DependencyProperty DescriptionProperty =
		DependencyProperty.Register("Description", typeof(string), typeof(HeaderCarousel), new PropertyMetadata(null));

	public IList<string> Screenshots
	{
		get => (IList<string>)GetValue(ScreenshotsProperty);
		set => SetValue(ScreenshotsProperty, value);
	}

	public static readonly DependencyProperty ScreenshotsProperty =
		DependencyProperty.Register(nameof(Screenshots), typeof(IList<string>), typeof(HeaderCarousel), new PropertyMetadata(new List<string>()));

	public IList<MediaSource> Videos
	{
		get => (IList<MediaSource>)GetValue(VideosProperty);
		set => SetValue(VideosProperty, value);
	}

	public static readonly DependencyProperty VideosProperty =
		DependencyProperty.Register(nameof(Videos), typeof(IList<MediaSource>), typeof(HeaderCarousel), new PropertyMetadata(new List<MediaSource>()));

	public string InstallLocation
	{
		get => (string)GetValue(InstallLocationProperty);
		set => SetValue(InstallLocationProperty, value);
	}
	public static readonly DependencyProperty InstallLocationProperty =
		DependencyProperty.Register(nameof(InstallLocation), typeof(string), typeof(HeaderCarousel), new PropertyMetadata(null));

	public string Launcher
	{
		get => (string)GetValue(LauncherProperty);
		set => SetValue(LauncherProperty, value);
	}

	public static readonly DependencyProperty LauncherProperty =
		DependencyProperty.Register(nameof(Launcher), typeof(string), typeof(HeaderCarousel), new PropertyMetadata(null));

	public string CatalogItemId
	{
		get => (string)GetValue(CatalogItemIdProperty);
		set => SetValue(CatalogItemIdProperty, value);
	}
	public static readonly DependencyProperty CatalogItemIdProperty =
		DependencyProperty.Register(nameof(CatalogItemId), typeof(string), typeof(HeaderCarousel), new PropertyMetadata(null));

	public string CatalogNamespace
	{
		get => (string)GetValue(CatalogNamespaceProperty);
		set => SetValue(CatalogNamespaceProperty, value);
	}
	public static readonly DependencyProperty CatalogNamespaceProperty =
		DependencyProperty.Register(nameof(CatalogNamespace), typeof(string), typeof(HeaderCarousel), new PropertyMetadata(null));

	public string AppName
	{
		get => (string)GetValue(AppNameProperty);
		set => SetValue(AppNameProperty, value);
	}
	public static readonly DependencyProperty AppNameProperty =
		DependencyProperty.Register(nameof(AppName), typeof(string), typeof(HeaderCarousel), new PropertyMetadata(null));

	public string LaunchCommand
	{
		get => (string)GetValue(LaunchCommandProperty);
		set => SetValue(LaunchCommandProperty, value);
	}
	public static readonly DependencyProperty LaunchCommandProperty =
		DependencyProperty.Register(nameof(LaunchCommand), typeof(string), typeof(HeaderCarousel), new PropertyMetadata(null));

	public string LaunchExecutable
	{
		get => (string)GetValue(LaunchExecutableProperty);
		set => SetValue(LaunchExecutableProperty, value);
	}
	public static readonly DependencyProperty LaunchExecutableProperty =
		DependencyProperty.Register(nameof(LaunchExecutable), typeof(string), typeof(HeaderCarousel), new PropertyMetadata(null));

	public List<string> ProcessNames
	{
		get => (List<string>)GetValue(ProcessNamesProperty);
		set => SetValue(ProcessNamesProperty, value);
	}
	public static readonly DependencyProperty ProcessNamesProperty =
		DependencyProperty.Register(nameof(ProcessNames), typeof(List<string>), typeof(HeaderCarousel), new PropertyMetadata(null));

	public List<string> BackgroundProcessNames
	{
		get => (List<string>)GetValue(BackgroundProcessNamesProperty);
		set => SetValue(BackgroundProcessNamesProperty, value);
	}
	public static readonly DependencyProperty BackgroundProcessNamesProperty =
		DependencyProperty.Register(nameof(BackgroundProcessNames), typeof(List<string>), typeof(HeaderCarousel), new PropertyMetadata(null));

	public string ArtifactId
	{
		get => (string)GetValue(ArtifactIdProperty);
		set => SetValue(ArtifactIdProperty, value);
	}
	public static readonly DependencyProperty ArtifactIdProperty =
		DependencyProperty.Register(nameof(ArtifactId), typeof(string), typeof(HeaderCarousel), new PropertyMetadata(null));

	public string GameID
	{
		get => (string)GetValue(GameIDProperty);
		set => SetValue(GameIDProperty, value);
	}

	public static readonly DependencyProperty GameIDProperty =
		DependencyProperty.Register(nameof(GameID), typeof(string), typeof(HeaderCarousel), new PropertyMetadata(null));

	public string LauncherLocation
	{
		get => (string)GetValue(LauncherLocationProperty);
		set => SetValue(LauncherLocationProperty, value);
	}
	public static readonly DependencyProperty LauncherLocationProperty =
		DependencyProperty.Register(nameof(LauncherLocation), typeof(string), typeof(HeaderCarousel), new PropertyMetadata(null));

	public string DataLocation
	{
		get => (string)GetValue(DataLocationProperty);
		set => SetValue(DataLocationProperty, value);
	}
	public static readonly DependencyProperty DataLocationProperty =
		DependencyProperty.Register(nameof(DataLocation), typeof(string), typeof(HeaderCarousel), new PropertyMetadata(null));

	public string GameLocation
	{
		get => (string)GetValue(GameLocationProperty);
		set => SetValue(GameLocationProperty, value);
	}
	public static readonly DependencyProperty GameLocationProperty =
		DependencyProperty.Register(nameof(GameLocation), typeof(string), typeof(HeaderCarousel), new PropertyMetadata(null));

	public string ReleaseDate
	{
		get => (string)GetValue(ReleaseDateProperty);
		set => SetValue(ReleaseDateProperty, value);
	}
	public static readonly DependencyProperty ReleaseDateProperty =
		DependencyProperty.Register(nameof(ReleaseDate), typeof(string), typeof(HeaderCarousel), new PropertyMetadata(null));

	public string Size
	{
		get => (string)GetValue(SizeProperty);
		set => SetValue(SizeProperty, value);
	}
	public static readonly DependencyProperty SizeProperty =
		DependencyProperty.Register(nameof(Size), typeof(string), typeof(HeaderCarousel), new PropertyMetadata(null));

	public string Version
	{
		get => (string)GetValue(VersionProperty);
		set => SetValue(VersionProperty, value);
	}
	public static readonly DependencyProperty VersionProperty =
		DependencyProperty.Register(nameof(Version), typeof(string), typeof(HeaderCarousel), new PropertyMetadata(null));

	public Stretch ImageStretch
	{
		get { return (Stretch)GetValue(ImageStretchProperty); }
		set { SetValue(ImageStretchProperty, value); }
	}

	public static readonly DependencyProperty ImageStretchProperty =
		DependencyProperty.Register(nameof(ImageStretch), typeof(Stretch), typeof(HeaderCarousel), new PropertyMetadata(Stretch.UniformToFill));

	public TimeSpan SelectionDuration
	{
		get { return (TimeSpan)GetValue(SelectionDurationProperty); }
		set { SetValue(SelectionDurationProperty, value); }
	}

	public static readonly DependencyProperty SelectionDurationProperty =
		DependencyProperty.Register(nameof(SelectionDuration), typeof(TimeSpan), typeof(HeaderCarousel), new PropertyMetadata(TimeSpan.FromSeconds(4), OnSelectionDurationChanged));

	private static void OnSelectionDurationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		var ctl = (HeaderCarousel)d;
		if (ctl != null)
		{
			ctl.selectionTimer.Interval = (TimeSpan)e.NewValue;
		}
	}

	public TimeSpan DeSelectionDuration
	{
		get { return (TimeSpan)GetValue(DeSelectionDurationProperty); }
		set { SetValue(DeSelectionDurationProperty, value); }
	}

	public static readonly DependencyProperty DeSelectionDurationProperty =
		DependencyProperty.Register(nameof(DeSelectionDuration), typeof(TimeSpan), typeof(HeaderCarousel), new PropertyMetadata(TimeSpan.FromSeconds(3), OnDeSelectionDurationChanged));

	private static void OnDeSelectionDurationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		var ctl = (HeaderCarousel)d;
		if (ctl != null)
		{
			ctl.deselectionTimer.Interval = (TimeSpan)e.NewValue;
		}
	}

	public bool IsBlurEnabled
	{
		get { return (bool)GetValue(IsBlurBackgroundProperty); }
		set { SetValue(IsBlurBackgroundProperty, value); }
	}

	public static readonly DependencyProperty IsBlurBackgroundProperty =
		DependencyProperty.Register(nameof(IsBlurEnabled), typeof(bool), typeof(HeaderCarousel), new PropertyMetadata(true, IsBlurEnabledChanged));

	private static void IsBlurEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		var ctl = (HeaderCarousel)d;
		ctl?.ApplyBackdropBlur();
	}

	public double BlurAmount
	{
		get { return (double)GetValue(BlurAmountProperty); }
		set { SetValue(BlurAmountProperty, value); }
	}

	public static readonly DependencyProperty BlurAmountProperty =
		DependencyProperty.Register(nameof(BlurAmount), typeof(double), typeof(HeaderCarousel), new PropertyMetadata(100.0, OnBlurChanged));

	private static void OnBlurChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		var ctl = (HeaderCarousel)d;
		ctl?.ApplyBackdropBlur();
	}
}
