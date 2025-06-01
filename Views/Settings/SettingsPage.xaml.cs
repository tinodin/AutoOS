using Microsoft.UI.Xaml.Media;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;

namespace AutoOS.Views.Settings;

public sealed partial class SettingsPage : Page
{
    public SettingsPage()
    {
        this.InitializeComponent();
        LoadSettings();
    }

    private void toCloneRepoCard_Click(object sender, RoutedEventArgs e)
    {
        DataPackage package = new DataPackage();
        package.SetText(gitCloneTextBlock.Text);
        Clipboard.SetContent(package);
    }

    private void ColorPicker_ColorChanged(ColorPicker sender, ColorChangedEventArgs args)
    {
        TintBox.Fill = new SolidColorBrush(args.NewColor);
        App.Current.ThemeService.SetBackdropTintColor(args.NewColor);


    }

    private void ColorPalette_ItemClick(object sender, ItemClickEventArgs e)
    {
        var color = e.ClickedItem as ColorPaletteItem;
        if (color != null)
        {
            if (color.Hex.Contains("#000000"))
            {
                App.Current.ThemeService.ResetBackdropProperties();
            }
            else
            {
                App.Current.ThemeService.SetBackdropTintColor(color.Color);
            }
            TintBox.Fill = new SolidColorBrush(color.Color);
        }
    }

    private void LoadSettings()
    {
        LaunchMinimized.IsOn = (bool?)ApplicationData.Current.LocalSettings.Values["LaunchMinimized"] ?? false;
    }

    private void LaunchMinimized_Toggled(object sender, RoutedEventArgs e)
    {
        ApplicationData.Current.LocalSettings.Values["LaunchMinimized"] = LaunchMinimized.IsOn;
    }
}

