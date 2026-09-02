using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.Media.Core;

namespace AutoOS.App.Views.Settings.Games;

public partial class HeaderCarouselItem
{
	public string Id
	{
		get => (string)GetValue(IdProperty);
		set => SetValue(IdProperty, value);
	}
	public static readonly DependencyProperty IdProperty =
		DependencyProperty.Register(nameof(Id), typeof(string), typeof(HeaderCarouselItem), new PropertyMetadata(defaultValue: string.Empty));

	public bool IsSelected
	{
		get => (bool)GetValue(IsSelectedProperty);
		set => SetValue(IsSelectedProperty, value);
	}
	public static readonly DependencyProperty IsSelectedProperty =
		DependencyProperty.Register(nameof(IsSelected), typeof(bool), typeof(HeaderCarouselItem), new PropertyMetadata(defaultValue: false, (d, e) => ((HeaderCarouselItem)d).IsSelectedChanged((bool)e.OldValue, (bool)e.NewValue)));
	protected virtual void IsSelectedChanged(object oldValue, object newValue)
	{
		OnIsSelectedChanged();
	}

	public Stretch Stretch
	{
		get { return (Stretch)GetValue(StretchProperty); }
		set { SetValue(StretchProperty, value); }
	}

	public static readonly DependencyProperty StretchProperty =
		DependencyProperty.Register(nameof(Stretch), typeof(Stretch), typeof(HeaderCarouselItem), new PropertyMetadata(Stretch.UniformToFill));

	public string ImageUrl
	{
		get => (string)GetValue(ImageUrlProperty);
		set => SetValue(ImageUrlProperty, value);
	}

	public static readonly DependencyProperty ImageUrlProperty =
		DependencyProperty.Register(nameof(ImageUrl), typeof(string), typeof(HeaderCarouselItem), new PropertyMetadata(null));

	public string BackgroundImageUrl
	{
		get => (string)GetValue(BackgroundImageUrlProperty);
		set => SetValue(BackgroundImageUrlProperty, value);
	}

	public static readonly DependencyProperty BackgroundImageUrlProperty =
		DependencyProperty.Register(nameof(BackgroundImageUrl), typeof(string), typeof(HeaderCarouselItem), new PropertyMetadata(null));

	public string Title
	{
		get => (string)GetValue(TitleProperty);
		set => SetValue(TitleProperty, value);
	}
	public static readonly DependencyProperty TitleProperty =
		DependencyProperty.Register(nameof(Title), typeof(string), typeof(HeaderCarouselItem), new PropertyMetadata(defaultValue: null));

	public string Developers
	{
		get => (string)GetValue(DevelopersProperty);
		set => SetValue(DevelopersProperty, value);
	}
	public static readonly DependencyProperty DevelopersProperty =
		DependencyProperty.Register(nameof(Developers), typeof(string), typeof(HeaderCarouselItem), new PropertyMetadata(defaultValue: null));

	public bool UpdateIsAvailable
	{
		get => (bool)GetValue(UpdateIsAvailableProperty);
		set
		{
			SetValue(UpdateIsAvailableProperty, value);
		}
	}

	public static readonly DependencyProperty UpdateIsAvailableProperty =
		DependencyProperty.Register(nameof(UpdateIsAvailable), typeof(bool), typeof(HeaderCarouselItem), new PropertyMetadata(false));

	public double Rating
	{
		get => (double)GetValue(RatingProperty);
		set => SetValue(RatingProperty, value);
	}

	public static readonly DependencyProperty RatingProperty =
		DependencyProperty.Register(nameof(Rating), typeof(double), typeof(HeaderCarouselItem), new PropertyMetadata(0.0));

	public string PlayTime
	{
		get => (string)GetValue(PlayTimeProperty);
		set => SetValue(PlayTimeProperty, value);
	}

	public static readonly DependencyProperty PlayTimeProperty =
		DependencyProperty.Register(nameof(PlayTime), typeof(string), typeof(HeaderCarouselItem), new PropertyMetadata(null));

	public string AgeRatingUrl
	{
		get => (string)GetValue(AgeRatingUrlProperty);
		set => SetValue(AgeRatingUrlProperty, value);
	}

	public static readonly DependencyProperty AgeRatingUrlProperty =
		DependencyProperty.Register(nameof(AgeRatingUrl), typeof(string), typeof(HeaderCarouselItem), new PropertyMetadata(null));

	public string AgeRatingTitle
	{
		get => (string)GetValue(AgeRatingTitleProperty);
		set => SetValue(AgeRatingTitleProperty, value);
	}
	public static readonly DependencyProperty AgeRatingTitleProperty =
		DependencyProperty.Register(nameof(AgeRatingTitle), typeof(string), typeof(HeaderCarouselItem), new PropertyMetadata(defaultValue: null));

	public string AgeRatingDescription
	{
		get => (string)GetValue(AgeRatingDescriptionProperty);
		set => SetValue(AgeRatingDescriptionProperty, value);
	}
	public static readonly DependencyProperty AgeRatingDescriptionProperty =
		DependencyProperty.Register(nameof(AgeRatingDescription), typeof(string), typeof(HeaderCarouselItem), new PropertyMetadata(defaultValue: null));

	public string Elements
	{
		get => (string)GetValue(ElementsProperty);
		set => SetValue(ElementsProperty, value);
	}
	public static readonly DependencyProperty ElementsProperty =
		DependencyProperty.Register(nameof(Elements), typeof(string), typeof(HeaderCarouselItem), new PropertyMetadata(defaultValue: null));

	public IList<string> Genres
	{
		get => (IList<string>)GetValue(GenresProperty);
		set => SetValue(GenresProperty, value);
	}

	public static readonly DependencyProperty GenresProperty =
		DependencyProperty.Register(nameof(Genres), typeof(IList<string>), typeof(HeaderCarouselItem), new PropertyMetadata(new List<string>()));

	public IList<string> Features
	{
		get => (IList<string>)GetValue(FeaturesProperty);
		set => SetValue(FeaturesProperty, value);
	}

	public static readonly DependencyProperty FeaturesProperty =
		DependencyProperty.Register(nameof(Features), typeof(IList<string>), typeof(HeaderCarouselItem), new PropertyMetadata(new List<string>()));


	public string Description
	{
		get => (string)GetValue(DescriptionProperty);
		set => SetValue(DescriptionProperty, value);
	}
	public static readonly DependencyProperty DescriptionProperty =
		DependencyProperty.Register(nameof(Description), typeof(string), typeof(HeaderCarouselItem), new PropertyMetadata(defaultValue: null));

	public IList<string> Screenshots
	{
		get => (IList<string>)GetValue(ScreenshotsProperty);
		set => SetValue(ScreenshotsProperty, value);
	}

	public static readonly DependencyProperty ScreenshotsProperty =
		DependencyProperty.Register(nameof(Screenshots), typeof(IList<string>), typeof(HeaderCarouselItem), new PropertyMetadata(new List<string>()));

	public IList<MediaSource> Videos
	{
		get => (IList<MediaSource>)GetValue(VideosProperty);
		set => SetValue(VideosProperty, value);
	}

	public static readonly DependencyProperty VideosProperty =
		DependencyProperty.Register(nameof(Videos), typeof(IList<MediaSource>), typeof(HeaderCarouselItem), new PropertyMetadata(new List<MediaSource>()));

	public string InstallLocation
	{
		get => (string)GetValue(InstallLocationProperty);
		set => SetValue(InstallLocationProperty, value);
	}
	public static readonly DependencyProperty InstallLocationProperty =
		DependencyProperty.Register(nameof(InstallLocation), typeof(string), typeof(HeaderCarouselItem), new PropertyMetadata(null));

	public string Launcher
	{
		get => (string)GetValue(LauncherProperty);
		set => SetValue(LauncherProperty, value);
	}

	public static readonly DependencyProperty LauncherProperty =
		DependencyProperty.Register(nameof(Launcher), typeof(string), typeof(HeaderCarouselItem), new PropertyMetadata(null));

	public string CatalogItemId
	{
		get => (string)GetValue(CatalogItemIdProperty);
		set => SetValue(CatalogItemIdProperty, value);
	}
	public static readonly DependencyProperty CatalogItemIdProperty =
		DependencyProperty.Register(nameof(CatalogItemId), typeof(string), typeof(HeaderCarouselItem), new PropertyMetadata(null));

	public string CatalogNamespace
	{
		get => (string)GetValue(CatalogNamespaceProperty);
		set => SetValue(CatalogNamespaceProperty, value);
	}
	public static readonly DependencyProperty CatalogNamespaceProperty =
		DependencyProperty.Register(nameof(CatalogNamespace), typeof(string), typeof(HeaderCarouselItem), new PropertyMetadata(null));

	public string AppName
	{
		get => (string)GetValue(AppNameProperty);
		set => SetValue(AppNameProperty, value);
	}
	public static readonly DependencyProperty AppNameProperty =
		DependencyProperty.Register(nameof(AppName), typeof(string), typeof(HeaderCarouselItem), new PropertyMetadata(null));

	public string LaunchCommand
	{
		get => (string)GetValue(LaunchCommandProperty);
		set => SetValue(LaunchCommandProperty, value);
	}
	public static readonly DependencyProperty LaunchCommandProperty =
		DependencyProperty.Register(nameof(LaunchCommand), typeof(string), typeof(HeaderCarouselItem), new PropertyMetadata(null));

	public string LaunchExecutable
	{
		get => (string)GetValue(LaunchExecutableProperty);
		set => SetValue(LaunchExecutableProperty, value);
	}
	public static readonly DependencyProperty LaunchExecutableProperty =
		DependencyProperty.Register(nameof(LaunchExecutable), typeof(string), typeof(HeaderCarouselItem), new PropertyMetadata(null));

	public List<string> ProcessNames
	{
		get => (List<string>)GetValue(ProcessNamesProperty);
		set => SetValue(ProcessNamesProperty, value);
	}
	public static readonly DependencyProperty ProcessNamesProperty =
		DependencyProperty.Register(nameof(ProcessNames), typeof(List<string>), typeof(HeaderCarouselItem), new PropertyMetadata(null));

	public List<string> BackgroundProcessNames
	{
		get => (List<string>)GetValue(BackgroundProcessNamesProperty);
		set => SetValue(BackgroundProcessNamesProperty, value);
	}
	public static readonly DependencyProperty BackgroundProcessNamesProperty =
		DependencyProperty.Register(nameof(BackgroundProcessNames), typeof(List<string>), typeof(HeaderCarouselItem), new PropertyMetadata(null));

	public string ArtifactId
	{
		get => (string)GetValue(ArtifactIdProperty);
		set => SetValue(ArtifactIdProperty, value);
	}
	public static readonly DependencyProperty ArtifactIdProperty =
		DependencyProperty.Register(nameof(ArtifactId), typeof(string), typeof(HeaderCarouselItem), new PropertyMetadata(null));

	public string GameID
	{
		get => (string)GetValue(GameIDProperty);
		set => SetValue(GameIDProperty, value);
	}

	public static readonly DependencyProperty GameIDProperty =
		DependencyProperty.Register(nameof(GameID), typeof(string), typeof(HeaderCarouselItem), new PropertyMetadata(null));

	public string LauncherLocation
	{
		get => (string)GetValue(LauncherLocationProperty);
		set => SetValue(LauncherLocationProperty, value);
	}
	public static readonly DependencyProperty LauncherLocationProperty =
		DependencyProperty.Register(nameof(LauncherLocation), typeof(string), typeof(HeaderCarouselItem), new PropertyMetadata(null));

	public string DataLocation
	{
		get => (string)GetValue(DataLocationProperty);
		set => SetValue(DataLocationProperty, value);
	}
	public static readonly DependencyProperty DataLocationProperty =
		DependencyProperty.Register(nameof(DataLocation), typeof(string), typeof(HeaderCarouselItem), new PropertyMetadata(null));

	public string GameLocation
	{
		get => (string)GetValue(GameLocationProperty);
		set => SetValue(GameLocationProperty, value);
	}
	public static readonly DependencyProperty GameLocationProperty =
		DependencyProperty.Register(nameof(GameLocation), typeof(string), typeof(HeaderCarouselItem), new PropertyMetadata(null));

	public string ReleaseDate
	{
		get => (string)GetValue(ReleaseDateProperty);
		set => SetValue(ReleaseDateProperty, value);
	}
	public static readonly DependencyProperty ReleaseDateProperty =
		DependencyProperty.Register(nameof(ReleaseDate), typeof(string), typeof(HeaderCarouselItem), new PropertyMetadata(null));

	public string Size
	{
		get => (string)GetValue(SizeProperty);
		set => SetValue(SizeProperty, value);
	}
	public static readonly DependencyProperty SizeProperty =
		DependencyProperty.Register(nameof(Size), typeof(string), typeof(HeaderCarouselItem), new PropertyMetadata(null));

	public string Version
	{
		get => (string)GetValue(VersionProperty);
		set => SetValue(VersionProperty, value);
	}
	public static readonly DependencyProperty VersionProperty =
		DependencyProperty.Register(nameof(Version), typeof(string), typeof(HeaderCarouselItem), new PropertyMetadata(null));
}
