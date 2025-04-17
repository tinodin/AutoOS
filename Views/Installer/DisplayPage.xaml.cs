using Microsoft.Win32;

namespace AutoOS.Views.Installer;

public sealed partial class DisplayPage : Page
{
    public DisplayPage()
    {
        InitializeComponent();
        GetCruProfile();
    }

    private void GetCruProfile()
    {
        // get value
        using var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\AutoOS");
        var value = key?.GetValue("CruProfile") as string;
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
                // delete value
                Registry.CurrentUser.CreateSubKey(@"SOFTWARE\AutoOS", true)?.DeleteValue("CruProfile", false);

                // remove infobar
                CruInfo.Children.Clear();
            };
            CruInfo.Children.Add(infoBar);
        }
    }

    private async void BrowseCru_Click(object sender, RoutedEventArgs e)
    {
        // disable the button to avoid double-clicking
        var senderButton = sender as Button;
        senderButton.IsEnabled = false;

        // remove infobar
        CruInfo.Children.Clear();

        // add infobar
        CruInfo.Children.Add(new InfoBar
        {
            Title = "Please select a Custom Resolution Utility (CRU) profile (.exe).",
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
        picker.FileTypeChoices.Add("CRU profile", new List<string> { "*.exe" });
        var file = await picker.PickSingleFileAsync();

        if (file != null)
        {
            var properties = await file.GetBasicPropertiesAsync();
            ulong fileSizeInBytes = properties.Size;
            const ulong expectedSize = 53 * 1024;

            if (fileSizeInBytes == expectedSize)
            {
                // remove infobar
                CruInfo.Children.Clear();

                // set value
                using (var key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\AutoOS"))
                {
                    key?.SetValue("CruProfile", file.Path, RegistryValueKind.String);
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
                    Registry.CurrentUser.CreateSubKey(@"SOFTWARE\AutoOS", true)?.DeleteValue("CruProfile", false);

                    // remove infobar
                    CruInfo.Children.Clear();
                };
                CruInfo.Children.Add(infoBar);
            }
            else
            {
                // remove infobar
                CruInfo.Children.Clear();

                // add infobar
                CruInfo.Children.Add(new InfoBar
                {
                    Title = "The selected file is not a valid Custom Resolution Utility (CRU) profile.",
                    IsClosable = false,
                    IsOpen = true,
                    Severity = InfoBarSeverity.Error,
                    Margin = new Thickness(5)
                });

                // delay
                await Task.Delay(2000);

                // remove infobar
                CruInfo.Children.Clear();
            }
        }
        else
        {
            // remove infobar
            CruInfo.Children.Clear();

            // get profile
            GetCruProfile();
        }
    }
}


