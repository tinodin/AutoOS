using Windows.Storage;

namespace AutoOS.Views.Installer;

public sealed partial class GraphicsPage : Page
{
    private bool isInitializingBrandsState = true;
    private bool isInitializingHDCPState = true;

    private readonly ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

    public GraphicsPage()
    {
        InitializeComponent();
        GetItems();
        GetBrand();
        GetHDCPState();
        GetMsiProfile();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        MainWindow.Instance.MarkVisited(nameof(GraphicsPage));
        MainWindow.Instance.CheckAllPagesVisited();
    }

    public class GridViewItem
    {
        public string Text { get; set; }
        public string ImageSource { get; set; }
    }

    private void GetItems()
    {
        Brands.ItemsSource = new List<GridViewItem>
        {
            new GridViewItem { Text = "Intel® 7th-10th Gen Processor Graphics", ImageSource = "ms-appx:///Assets/Fluent/Intel.png" },
            new GridViewItem { Text = "Intel® Arc™ & Iris® Xe Graphics", ImageSource = "ms-appx:///Assets/Fluent/Intel.png" },
            new GridViewItem { Text = "NVIDIA", ImageSource = "ms-appx:///Assets/Fluent/Nvidia.png" },
            new GridViewItem { Text = "AMD", ImageSource = "ms-appx:///Assets/Fluent/Amd.png" }
        };
    }

    private void GetBrand()
    {
        var selectedBrand = localSettings.Values["GpuBrand"] as string;
        var brandItems = Brands.ItemsSource as List<GridViewItem>;
        Brands.SelectedItems.AddRange(
            selectedBrand?.Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries)
            .Select(e => brandItems?.FirstOrDefault(ext => ext.Text == e))
            .Where(ext => ext != null) ?? Enumerable.Empty<GridViewItem>()
        );
        isInitializingBrandsState = false;
    }

    private void Brand_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (isInitializingBrandsState) return;

        var selectedBrand = Brands.SelectedItems
            .Cast<GridViewItem>()
            .Select(item => item.Text)
            .ToArray();

        localSettings.Values["GpuBrand"] = string.Join(", ", selectedBrand);
    }

    private void GetHDCPState()
    {
        object value;
        if (!localSettings.Values.TryGetValue("HighBandwidthDigitalContentProtection", out value))
        {
            localSettings.Values["HighBandwidthDigitalContentProtection"] = 0;
        }
        else
        {
            HDCP.IsOn = Convert.ToInt32(value) == 1;
        }

        isInitializingHDCPState = false;
    }

    private void HDCP_Toggled(object sender, RoutedEventArgs e)
    {
        if (isInitializingHDCPState) return;

        localSettings.Values["HighBandwidthDigitalContentProtection"] = HDCP.IsOn ? 1 : 0;
    }

    private void GetMsiProfile()
    {
        var value = localSettings.Values["MsiProfile"] as string;
        if (!string.IsNullOrEmpty(value))
        {
            var infoBar = new InfoBar
            {
                Title = value,
                IsClosable = true,
                IsOpen = true,
                Severity = InfoBarSeverity.Success,
                Margin = new Thickness(5)
            };

            infoBar.CloseButtonClick += (_, _) =>
            {
                localSettings.Values.Remove("MsiProfile");
                MsiAfterburnerInfo.Children.Clear();
            };
            MsiAfterburnerInfo.Children.Add(infoBar);
        }
    }

    private async void BrowseMsi_Click(object sender, RoutedEventArgs e)
    {
        var senderButton = sender as Button;
        senderButton.IsEnabled = false;
        MsiAfterburnerInfo.Children.Clear();

        MsiAfterburnerInfo.Children.Add(new InfoBar
        {
            Title = "Please select a MSI Afterburner profile (.cfg).",
            IsClosable = false,
            IsOpen = true,
            Severity = InfoBarSeverity.Informational,
            Margin = new Thickness(5)
        });

        await Task.Delay(300);

        var picker = new FilePicker(App.MainWindow);
        picker.ShowAllFilesOption = false;
        picker.FileTypeChoices.Add("MSI Afterburner profile", new List<string> { "*.cfg" });
        var file = await picker.PickSingleFileAsync();

        if (file != null)
        {
            string fileContent = await FileIO.ReadTextAsync(file);

            if (fileContent.Contains("[Startup]"))
            {
                senderButton.IsEnabled = true;
                MsiAfterburnerInfo.Children.Clear();

                localSettings.Values["MsiProfile"] = file.Path;

                var infoBar = new InfoBar
                {
                    Title = file.Path,
                    IsClosable = true,
                    IsOpen = true,
                    Severity = InfoBarSeverity.Success,
                    Margin = new Thickness(5)
                };

                infoBar.CloseButtonClick += (_, _) =>
                {
                    localSettings.Values.Remove("MsiProfile");
                    MsiAfterburnerInfo.Children.Clear();
                };
                MsiAfterburnerInfo.Children.Add(infoBar);
            }
            else
            {
                senderButton.IsEnabled = true;
                MsiAfterburnerInfo.Children.Clear();

                MsiAfterburnerInfo.Children.Add(new InfoBar
                {
                    Title = "The selected file is not a valid MSI Afterburner profile.",
                    IsClosable = false,
                    IsOpen = true,
                    Severity = InfoBarSeverity.Error,
                    Margin = new Thickness(5)
                });

                await Task.Delay(2000);
                MsiAfterburnerInfo.Children.Clear();
            }
        }
        else
        {
            senderButton.IsEnabled = true;
            MsiAfterburnerInfo.Children.Clear();
            GetMsiProfile();
        }
    }
}