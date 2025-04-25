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

        // set value
        var selectedBrand = Brands.SelectedItems
            .Cast<GridViewItem>()
            .Select(item => item.Text)
            .ToArray();

        using (var key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\AutoOS"))
        {
            key?.SetValue("GpuBrand", string.Join(", ", selectedBrand), RegistryValueKind.String);
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
        // disable the button to avoid double-clicking
        var senderButton = sender as Button;
        senderButton.IsEnabled = false;

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
        var picker = new FilePicker(App.MainWindow);
        picker.ShowAllFilesOption = false;
        picker.FileTypeChoices.Add("MSI Afterburner profile", new List<string> { "*.cfg" });
        var file = await picker.PickSingleFileAsync();

        if (file != null)
        {
            string fileContent = await FileIO.ReadTextAsync(file);

            if (fileContent.Contains("[Startup]"))
            {
                // re-enable the button
                senderButton.IsEnabled = true;

                // remove infobar
                MsiAfterburnerInfo.Children.Clear();

                // set value
                using (var key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\AutoOS"))
                {
                    key?.SetValue("MsiProfile", file.Path, RegistryValueKind.String);
                }

                // add infobar
                var infoBar = new InfoBar
                {
                    Title = $"{file.Path}",
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
                // re-enable the button
                senderButton.IsEnabled = true;

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
            // re-enable the button
            senderButton.IsEnabled = true;

            // remove infobar
            MsiAfterburnerInfo.Children.Clear();

            // get profile
            GetMsiProfile();
        }
    }
}

