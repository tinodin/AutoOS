using System.Diagnostics;
using Windows.Storage;

namespace AutoOS.Views.Settings;

public sealed partial class DisplayPage : Page
{
    private readonly ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

    public DisplayPage()
    {
        InitializeComponent();
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
                // re-enable the button
                senderButton.IsEnabled = true;

                // remove infobar
                CruInfo.Children.Clear();

                // add infobar
                CruInfo.Children.Add(new InfoBar
                {
                    Title = "Applying the Custom Resolution Utility (CRU) profile...",
                    IsClosable = false,
                    IsOpen = true,
                    Severity = InfoBarSeverity.Informational,
                    Margin = new Thickness(5)
                });

                // delay
                await Task.Delay(300);

                // import profile
                await Task.Run(() => Process.Start(new ProcessStartInfo { FileName = file.Path, Arguments = "/i" })?.WaitForExit());

                // restart driver
                await Task.Run(() => Process.Start(new ProcessStartInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "CRU", "restart64.exe")) { Arguments = "/q" })?.WaitForExit());

                // apply profile
                if (localSettings.Values["MsiProfile"] != null)
                {
                    await Task.Run(() => Process.Start(new ProcessStartInfo(@"C:\Program Files (x86)\MSI Afterburner\MSIAfterburner.exe") { Arguments = "/Profile1 /q" })?.WaitForExit());
                }

                // remove infobar
                CruInfo.Children.Clear();

                // add infobar
                CruInfo.Children.Add(new InfoBar
                {
                    Title = "Successfully applied the Custom Resolution Utility (CRU) profile.",
                    IsClosable = false,
                    IsOpen = true,
                    Severity = InfoBarSeverity.Success,
                    Margin = new Thickness(5)
                });

                // delay
                await Task.Delay(2000);

                // remove infobar
                CruInfo.Children.Clear();
            }
            else
            {
                // re-enable the button
                senderButton.IsEnabled = true;

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
            // re-enable the button
            senderButton.IsEnabled = true;

            // remove infobar
            CruInfo.Children.Clear();
        }
    }

    private async void LaunchCru_Click(object sender, RoutedEventArgs e)
    {
        // remove infobar
        CruInfo.Children.Clear();

        // add infobar
        CruInfo.Children.Add(new InfoBar
        {
            Title = "Launching Custom Resolution Utility (CRU)...",
            IsClosable = false,
            IsOpen = true,
            Severity = InfoBarSeverity.Informational,
            Margin = new Thickness(5)
        });

        // delay
        await Task.Delay(300);

        // launch
        await Task.Run(() => Process.Start(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "CRU", "CRU.exe"))?.WaitForInputIdle());

        // remove infobar
        CruInfo.Children.Clear();

        // add infobar
        CruInfo.Children.Add(new InfoBar
        {
            Title = "Successfully launched Custom Resolution Utility (CRU).",
            IsClosable = false,
            IsOpen = true,
            Severity = InfoBarSeverity.Success,
            Margin = new Thickness(5)
        });

        // delay
        await Task.Delay(2000);

        // remove infobar
        CruInfo.Children.Clear();
    }

    private async void ResetCru_Click(object sender, RoutedEventArgs e)
    {
        // remove infobar
        CruInfo.Children.Clear();

        // add infobar
        CruInfo.Children.Add(new InfoBar
        {
            Title = "Resetting all custom resolutions...",
            IsClosable = false,
            IsOpen = true,
            Severity = InfoBarSeverity.Informational,
            Margin = new Thickness(5)
        });

        // delay
        await Task.Delay(300);

        // launch
        Process.Start(new ProcessStartInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "CRU", "reset-all.exe")) { Arguments = "/q" }).WaitForExit();
        Process.Start(new ProcessStartInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "CRU", "restart64.exe")) { Arguments = "/q" }).WaitForExit();

        // apply profile
        if (localSettings.Values["MsiProfile"] != null)
        {
            await Task.Run(() => Process.Start(new ProcessStartInfo(@"C:\Program Files (x86)\MSI Afterburner\MSIAfterburner.exe") { Arguments = "/Profile1 /q" })?.WaitForExit());
        }

        // remove infobar
        CruInfo.Children.Clear();

        // add infobar
        CruInfo.Children.Add(new InfoBar
        {
            Title = "Successfully reset all custom resolutions.",
            IsClosable = false,
            IsOpen = true,
            Severity = InfoBarSeverity.Success,
            Margin = new Thickness(5)
        });

        // delay
        await Task.Delay(2000);

        // remove infobar
        CruInfo.Children.Clear();
    }

    private async void RestartCru_Click(object sender, RoutedEventArgs e)
    {
        // remove infobar
        CruInfo.Children.Clear();

        // add infobar
        CruInfo.Children.Add(new InfoBar
        {
            Title = "Restarting the graphics driver...",
            IsClosable = false,
            IsOpen = true,
            Severity = InfoBarSeverity.Informational,
            Margin = new Thickness(5)
        });

        // delay
        await Task.Delay(300);

        // launch
        Process.Start(new ProcessStartInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "CRU", "restart64.exe")) { Arguments = "/q" }).WaitForExit();

        // apply profile
        if (localSettings.Values["MsiProfile"] != null)
        {
            await Task.Run(() => Process.Start(new ProcessStartInfo(@"C:\Program Files (x86)\MSI Afterburner\MSIAfterburner.exe") { Arguments = "/Profile1 /q" })?.WaitForExit());
        }

        // remove infobar
        CruInfo.Children.Clear();

        // add infobar
        CruInfo.Children.Add(new InfoBar
        {
            Title = "Successfully restarted the graphics driver.",
            IsClosable = false,
            IsOpen = true,
            Severity = InfoBarSeverity.Success,
            Margin = new Thickness(5)
        });

        // delay
        await Task.Delay(2000);

        // remove infobar
        CruInfo.Children.Clear();
    }
}