using AutoOS.Core.Helpers.Picker;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Windows.Storage;
using Windows.Storage.FileProperties;

namespace AutoOS.App.Views.Installer;

public sealed partial class DisplaysPage : Page
{
	private readonly ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

	public DisplaysPage()
	{
		InitializeComponent();
		GetCruProfile();
	}

	protected override void OnNavigatedTo(NavigationEventArgs e)
	{
		base.OnNavigatedTo(e);
		MainWindow.Instance.MarkVisited(nameof(DisplaysPage));
		MainWindow.Instance.CheckAllPagesVisited();
	}

	private void GetCruProfile()
	{
		string? value = localSettings.Values["CruProfile"] as string;
		if (!string.IsNullOrEmpty(value))
		{
			var infoBar = new InfoBar
			{
				Title = value,
				IsClosable = true,
				IsOpen = true,
				Severity = InfoBarSeverity.Success,
				Margin = new Thickness(4, -28, 4, 36)
			};

			infoBar.CloseButtonClick += (_, _) =>
			{
				localSettings.Values.Remove("CruProfile");
				CruInfo.Children.Clear();
			};

			CruInfo.Children.Clear();
			CruInfo.Children.Add(infoBar);
		}
	}

	private async void BrowseCru_Click(object sender, RoutedEventArgs e)
	{
		var senderButton = (Button)sender;
		senderButton.IsEnabled = false;
		CruInfo.Children.Clear();

		CruInfo.Children.Add(new InfoBar
		{
			Title = "Please select a Custom Resolution Utility (CRU) profile (.exe).",
			IsClosable = false,
			IsOpen = true,
			Severity = InfoBarSeverity.Informational,
			Margin = new Thickness(4, -28, 4, 36)
		});

		await Task.Delay(300);

		var picker = new FilePicker(App.MainWindow)
		{
			ShowAllFilesOption = false
		};
		picker.FileTypeChoices.Add("CRU profile", ["*.exe"]);
		StorageFile? file = await picker.PickSingleFileAsync();

		if (file != null)
		{
			BasicProperties properties = await file.GetBasicPropertiesAsync();
			ulong fileSizeInBytes = properties.Size;
			const ulong expectedSize = 53 * 1024;

			if (fileSizeInBytes == expectedSize)
			{
				senderButton.IsEnabled = true;
				CruInfo.Children.Clear();

				localSettings.Values["CruProfile"] = file.Path;

				var infoBar = new InfoBar
				{
					Title = file.Path,
					IsClosable = true,
					IsOpen = true,
					Severity = InfoBarSeverity.Success,
					Margin = new Thickness(4, -28, 4, 36)
				};

				infoBar.CloseButtonClick += (_, _) =>
				{
					localSettings.Values.Remove("CruProfile");
					CruInfo.Children.Clear();
				};

				CruInfo.Children.Add(infoBar);
			}
			else
			{
				senderButton.IsEnabled = true;
				CruInfo.Children.Clear();

				CruInfo.Children.Add(new InfoBar
				{
					Title = "The selected file is not a valid Custom Resolution Utility (CRU) profile.",
					IsClosable = false,
					IsOpen = true,
					Severity = InfoBarSeverity.Error,
					Margin = new Thickness(4, -28, 4, 36)
				});

				await Task.Delay(2000);
				CruInfo.Children.Clear();
			}
		}
		else
		{
			senderButton.IsEnabled = true;
			CruInfo.Children.Clear();
			GetCruProfile();
		}
	}
}
