
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
}