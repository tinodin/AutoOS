using Microsoft.UI.Xaml.Media;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;

namespace AutoOS.Views.Settings;

public sealed partial class SettingsPage : Page
{
    private readonly ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
    public SettingsPage()
    {
        this.InitializeComponent();
        LoadSettings();
    }

    private  void RyujinxLocation_TextChanged(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(RyujinxLocationValue?.Text))
        {
            localSettings.Values.Remove("RyujinxLocation");
            return;
        }
    }

    private async void RyujinxLocation_Click(object sender, RoutedEventArgs e)
    {
        var picker = new FilePicker(App.MainWindow)
        {
            ShowAllFilesOption = false
        };
        picker.FileTypeChoices.Add("Ryujinx executable", ["*.exe"]);

        var file = await picker.PickSingleFileAsync();

        if (file != null)
        {
            if (Path.GetFileName(file.Path).Equals("Ryujinx.exe", StringComparison.OrdinalIgnoreCase))
            {
                RyujinxLocationValue.Text = file.Path;
                localSettings.Values["RyujinxLocation"] = file.Path;
            }
            else 
            {
                var dialog = new ContentDialog
                {
                    Title = "Invalid File",
                    Content = "Please select the Ryujinx.exe file.",
                    CloseButtonText = "OK",
                    XamlRoot = App.MainWindow.Content.XamlRoot
                };
                await dialog.ShowAsync();
            }
        }   
    }

    private void RyujinxDataLocation_TextChanged(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(RyujinxDataLocationValue?.Text))
        {
            localSettings.Values.Remove("RyujinxDataLocation");
            return;
        }
    }

    private async void RyujinxDataLocation_Click(object sender, RoutedEventArgs e)
    {
        var picker = new FolderPicker(App.MainWindow);
        var folder = await picker.PickSingleFolderAsync();

        if (folder != null)
        {
            string folderName = Path.GetFileName(folder.Path).ToLowerInvariant();
            if (folderName == "ryujinx" || folderName == "portable")
            {
                RyujinxDataLocationValue.Text = folder.Path;
                localSettings.Values["RyujinxDataLocation"] = folder.Path;
            }
            else
            {
                var dialog = new ContentDialog
                {
                    Title = "Invalid Folder",
                    Content = "Please select the portable folder.",
                    CloseButtonText = "OK",
                    XamlRoot = App.MainWindow.Content.XamlRoot
                };
                await dialog.ShowAsync();
            }
        }
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
        if (localSettings.Values.TryGetValue("RyujinxLocation", out object ryujinxLocationValue) && ryujinxLocationValue is string ryujinxLocationPath)
        {
            RyujinxLocationValue.Text = ryujinxLocationPath;
        }

        if (localSettings.Values.TryGetValue("RyujinxDataLocation", out object ryujinxDataValue) && ryujinxDataValue is string ryujinxDataPath)
        {
            RyujinxDataLocationValue.Text = ryujinxDataPath;
        }
        else
        {
            string defaultPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Ryujinx");
            if (Directory.Exists(defaultPath))
            {
                RyujinxDataLocationValue.Text = defaultPath;
            }
        }

        LaunchMinimized.IsOn = (bool?)ApplicationData.Current.LocalSettings.Values["LaunchMinimized"] ?? false;
    }

    private void LaunchMinimized_Toggled(object sender, RoutedEventArgs e)
    {
        ApplicationData.Current.LocalSettings.Values["LaunchMinimized"] = LaunchMinimized.IsOn;
    }
}

