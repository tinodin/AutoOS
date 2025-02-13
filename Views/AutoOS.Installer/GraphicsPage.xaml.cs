using Microsoft.Win32;
using Windows.Storage;

namespace AutoOS.Views.Installer;

public sealed partial class GraphicsPage : Page
{
    private bool isInitializingBrandsState = true;
    private bool isInitializingHDCPState = true;

    public GraphicsPage()
    {
        InitializeComponent();
        GetItems();
        GetBrand();
        GetHDCPState();
        GetMsiProfile();
    }

    public class GridViewItem
    {
        public string Text { get; set; }
        public string ImageSource { get; set; }
    }

    private void GetItems()
    {
        // add brand items
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
        // get brand
        using var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\AutoOS");
        var selectedBrand = key?.GetValue("GpuBrand") as string;
        var brandItems = Brands.ItemsSource as List<GridViewItem>;
        Brands.SelectedItem = brandItems?.FirstOrDefault(b => b.Text == selectedBrand);

        isInitializingBrandsState = false;
    }

    private void Brand_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (isInitializingBrandsState) return;

        // set value
        if (Brands.SelectedItem is GridViewItem selectedItem)
        {
            using (var key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\AutoOS"))
            {
                key?.SetValue("GpuBrand", selectedItem.Text, RegistryValueKind.String);
            }
        }
    }

    private void GetHDCPState()
    {
        // get state
        using var key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\AutoOS");
        var value = key?.GetValue("HighBandwidthDigitalContentProtection");

        if (value == null)
        {
            key?.SetValue("HighBandwidthDigitalContentProtection", 0, RegistryValueKind.DWord);
        }
        else
        {
            HDCP.IsOn = (int)value == 1;
        }

        isInitializingHDCPState = false;
    }

    private void HDCP_Toggled(object sender, RoutedEventArgs e)
    {
        if (isInitializingHDCPState) return;

        // set value
        using var key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\AutoOS");
        key?.SetValue("HighBandwidthDigitalContentProtection", HDCP.IsOn ? 1 : 0, RegistryValueKind.DWord);
    }

    private void GetMsiProfile()
    {
        using var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\AutoOS");
        var value = key?.GetValue("MsiProfile") as string;
        if (!string.IsNullOrEmpty(value))
        {
            // add infobar
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
                // remove value
                Registry.CurrentUser.CreateSubKey(@"SOFTWARE\AutoOS", true)?.DeleteValue("MsiProfile", false);

                // remove infobar
                MsiAfterburnerInfo.Children.Clear();
            };
            MsiAfterburnerInfo.Children.Add(infoBar);
        }
    }
    private async void BrowseMsi_Click(object sender, RoutedEventArgs e)
    {
        // remove infobar
        MsiAfterburnerInfo.Children.Clear();

        // add infobar
        MsiAfterburnerInfo.Children.Add(new InfoBar
        {
            Title = "Please select a MSI Afterburner profile (.cfg).",
            IsClosable = false,
            IsOpen = true,
            Severity = InfoBarSeverity.Informational,
            Margin = new Thickness(5)
        });

        // delay
        await Task.Delay(300);

        // launch file picker
        var fileTypeFilter = new List<string> { ".cfg" };
        var picker = await FileAndFolderPickerHelper.PickSingleFileAsync(App.MainWindow, fileTypeFilter);
        if (picker != null)
        {
            string fileContent = await FileIO.ReadTextAsync(picker);

            if (fileContent.Contains("[Startup]"))
            {
                // remove infobar
                MsiAfterburnerInfo.Children.Clear();

                // set value
                using (var key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\AutoOS"))
                {
                    key?.SetValue("MsiProfile", picker.Path, RegistryValueKind.String);
                }

                // add infobar
                var infoBar = new InfoBar
                {
                    Title = $"{picker.Path}",
                    IsClosable = true,
                    IsOpen = true,
                    Severity = InfoBarSeverity.Success,
                    Margin = new Thickness(5)
                };

                infoBar.CloseButtonClick += (_, _) =>
                {
                    // delete value
                    Registry.CurrentUser.CreateSubKey(@"SOFTWARE\AutoOS", true)?.DeleteValue("MsiProfile", false);

                    // remove infobar
                    MsiAfterburnerInfo.Children.Clear();
                };
                MsiAfterburnerInfo.Children.Add(infoBar);
            }
            else
            {
                // remove infobar
                MsiAfterburnerInfo.Children.Clear();

                // add infobar
                MsiAfterburnerInfo.Children.Add(new InfoBar
                {
                    Title = "The selected file is not a valid MSI Afterburner profile.",
                    IsClosable = false,
                    IsOpen = true,
                    Severity = InfoBarSeverity.Error,
                    Margin = new Thickness(5)
                });

                // delay
                await Task.Delay(2000);

                // remove infobar
                MsiAfterburnerInfo.Children.Clear();
            }
        }
        else
        {
            // remove infobar
            MsiAfterburnerInfo.Children.Clear();

            // get profile
            GetMsiProfile();
        }
    }
}

