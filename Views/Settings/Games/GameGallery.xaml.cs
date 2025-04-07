using Microsoft.UI.Xaml.Media.Imaging;
using System.Text.Json;

namespace AutoOS.Views.Settings.Games;
public sealed partial class GameGallery : UserControl
{
    public object HeaderContent
    {
        get => (object)GetValue(HeaderContentProperty);
        set => SetValue(HeaderContentProperty, value);
    }
    public static readonly DependencyProperty HeaderContentProperty = DependencyProperty.Register("HeaderContent", typeof(object), typeof(GameGallery), new PropertyMetadata(null));

    public GameGallery()
    {
        InitializeComponent();
    }

    private void scroller_ViewChanging(object sender, ScrollViewerViewChangingEventArgs e)
    {
        if (e.FinalView.HorizontalOffset < 1)
        {
            ScrollBackBtn.Visibility = Visibility.Collapsed;
        }
        else if (e.FinalView.HorizontalOffset > 1)
        {
            ScrollBackBtn.Visibility = Visibility.Visible;
        }

        if (e.FinalView.HorizontalOffset > scroller.ScrollableWidth - 1)
        {
            ScrollForwardBtn.Visibility = Visibility.Collapsed;
        }
        else if (e.FinalView.HorizontalOffset < scroller.ScrollableWidth - 1)
        {
            ScrollForwardBtn.Visibility = Visibility.Visible;
        }
    }

    private void ScrollBackBtn_Click(object sender, RoutedEventArgs e)
    {
        scroller.ChangeView(scroller.HorizontalOffset - scroller.ViewportWidth, null, null);
        ScrollBackBtn.Focus(FocusState.Programmatic);
    }

    private void ScrollForwardBtn_Click(object sender, RoutedEventArgs e)
    {
        scroller.ChangeView(scroller.HorizontalOffset + scroller.ViewportWidth, null, null);
        ScrollBackBtn.Focus(FocusState.Programmatic);
    }

    private void scroller_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateScrollButtonsVisibility();
    }

    private void UpdateScrollButtonsVisibility()
    {
        ScrollForwardBtn.Visibility = scroller.ScrollableWidth > 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private async void AddCustomGame_Click(object sender, RoutedEventArgs e)
    {

        var contentDialog = new ContentDialog
        {
            Title = "Add Game",
            Content = new GameAdd(),
            PrimaryButtonText = "Add",
            CloseButtonText = "Cancel",
            PrimaryButtonStyle = (Style)Resources["AccentButtonStyle"],
            XamlRoot = this.XamlRoot,
        };

        contentDialog.Resources["ContentDialogMinWidth"] = 850;

        ContentDialogResult result = await contentDialog.ShowAsync();

        if (result == ContentDialogResult.Primary)
        {
            // get gameadd data
            var gameAddPage = (GameAdd)contentDialog.Content;

            // add game
            var gamePanel = new GamePanel
            {
                Launcher = gameAddPage.Launcher,
                LauncherLocation = gameAddPage.LauncherLocation,
                DataLocation = gameAddPage.DataLocation,
                GameLocation = gameAddPage.GameLocation,
                Title = gameAddPage.GameName,
                Description = gameAddPage.GameDeveloper,
                ImageSource = new BitmapImage(new Uri(gameAddPage.GameCoverUrl))
            };

            var stackPanel = (StackPanel)GamesPage.Instance?.Games.HeaderContent;
            stackPanel.Children.Add(gamePanel);

            // save game data
            var gameInfo = new
            {
                gameAddPage.Launcher,
                gameAddPage.LauncherLocation,
                gameAddPage.DataLocation,
                gameAddPage.GameLocation,
                gameAddPage.GameName,
                gameAddPage.GameDeveloper,
                gameAddPage.GameCoverUrl
            };

            string sanitizedTitle = string.Concat(gamePanel.Title.Split(Path.GetInvalidFileNameChars()));
            string filePath = Path.Combine(PathHelper.GetAppDataFolderPath(), "Games", $"{sanitizedTitle}.json");

            string json = JsonSerializer.Serialize(gameInfo, JsonOptions);
            File.WriteAllText(filePath, json);

            // sort all entries
            var sorted = stackPanel.Children
                .OfType<GamePanel>()
                .OrderBy(g => g.Title, StringComparer.CurrentCultureIgnoreCase)
                .ToList();

            stackPanel.Children.Clear();

            foreach (var panel in sorted)
            {
                stackPanel.Children.Add(panel);
            }

            await gamePanel.CheckGameRunning();
        }
    }
}