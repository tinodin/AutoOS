using AutoOS.App.Views.Installer.Actions;
using AutoOS.Core.Helpers.Picker;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage;
using Windows.Storage.FileProperties;

namespace AutoOS.App.Views.Settings;

public sealed partial class DisplaysPage : Page
{
	private readonly ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

	public DisplaysPage()
	{
		InitializeComponent();
	}

	private async void BrowseCru_Click(object sender, RoutedEventArgs e)
	{
		// disable the button to avoid double-clicking
		var senderButton = (Button)sender;
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
			Margin = new Thickness(4, -28, 4, 36)
		});

		// delay
		await Task.Delay(300);

		// launch file picker
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
					Margin = new Thickness(4, -28, 4, 36)
				});

				// close obs studio
				if (Process.GetProcessesByName("obs64").Length > 0)
				{
					foreach (Process process in Process.GetProcessesByName("obs64"))
					{
						process.Kill();
						process.WaitForExit();
					}
				}

				// delay
				await Task.Delay(300);

				// import profile
				await Process.Start(new ProcessStartInfo(file.Path) { Arguments = "/i" })!.WaitForExitAsync();

				// restart driver
				await Process.Start(new ProcessStartInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "CRU", "restart64.exe")) { Arguments = "/q" })!.WaitForExitAsync();

				// apply profile
				if (File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "MSI Afterburner", "MSIAfterburner.exe")) && Directory.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "MSI Afterburner", "Profiles")) && Directory.GetFiles(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "MSI Afterburner", "Profiles")).Any(file => !file.EndsWith("MSIAfterburner.cfg", StringComparison.OrdinalIgnoreCase)))
				{
					await Process.Start(new ProcessStartInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "MSI Afterburner", "MSIAfterburner.exe")) { Arguments = "/Profile1 /q" })!.WaitForExitAsync();
				}

				// launch obs studio
				if (!(localSettings.Values["OBS"] as int? == 0) && File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "obs-studio", "bin", "64bit", "obs64.exe")))
				{
					ProcessActions.CleanDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "obs-studio", ".sentinel"));
					Process.Start(new ProcessStartInfo { FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "obs-studio", "bin", "64bit", "obs64.exe"), Arguments = "--disable-updater --startreplaybuffer --minimize-to-tray", WorkingDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "obs-studio", "bin", "64bit") });
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
					Margin = new Thickness(4, -28, 4, 36)
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
					Margin = new Thickness(4, -28, 4, 36)
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
			Margin = new Thickness(4, -28, 4, 36)
		});

		// delay
		await Task.Delay(300);

		// launch
		Process.Start(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "CRU", "CRU.exe"))!.WaitForInputIdle();

		// remove infobar
		CruInfo.Children.Clear();

		// add infobar
		CruInfo.Children.Add(new InfoBar
		{
			Title = "Successfully launched Custom Resolution Utility (CRU).",
			IsClosable = false,
			IsOpen = true,
			Severity = InfoBarSeverity.Success,
			Margin = new Thickness(4, -28, 4, 36)
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
			Margin = new Thickness(4, -28, 4, 36)
		});

		// close obs studio
		if (Process.GetProcessesByName("obs64").Length > 0)
		{
			foreach (Process process in Process.GetProcessesByName("obs64"))
			{
				process.Kill();
				process.WaitForExit();
			}
		}

		// delay
		await Task.Delay(300);

		// reset
		await Process.Start(new ProcessStartInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "CRU", "reset-all.exe")) { Arguments = "/q" })!.WaitForExitAsync();

		// restart
		await Process.Start(new ProcessStartInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "CRU", "restart64.exe")) { Arguments = "/q" })!.WaitForExitAsync();

		// apply profile
		if (File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "MSI Afterburner", "MSIAfterburner.exe")) && Directory.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "MSI Afterburner", "Profiles")) && Directory.GetFiles(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "MSI Afterburner", "Profiles")).Any(file => !file.EndsWith("MSIAfterburner.cfg", StringComparison.OrdinalIgnoreCase)))
		{
			await Process.Start(new ProcessStartInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "MSI Afterburner", "MSIAfterburner.exe")) { Arguments = "/Profile1 /q" })!.WaitForExitAsync();
		}

		// launch obs studio
		if (!(localSettings.Values["OBS"] as int? == 0) && File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "obs-studio", "bin", "64bit", "obs64.exe")))
		{
			ProcessActions.CleanDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "obs-studio", ".sentinel"));
			Process.Start(new ProcessStartInfo { FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "obs-studio", "bin", "64bit", "obs64.exe"), Arguments = "--disable-updater --startreplaybuffer --minimize-to-tray", WorkingDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "obs-studio", "bin", "64bit") });
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
			Margin = new Thickness(4, -28, 4, 36)
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
			Margin = new Thickness(4, -28, 4, 36)
		});

		// close obs studio
		if (Process.GetProcessesByName("obs64").Length > 0)
		{
			foreach (Process process in Process.GetProcessesByName("obs64"))
			{
				process.Kill();
				process.WaitForExit();
			}
		}

		// delay
		await Task.Delay(300);

		// restart
		await Process.Start(new ProcessStartInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "CRU", "restart64.exe")))!.WaitForExitAsync();

		// apply profile
		if (File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "MSI Afterburner", "MSIAfterburner.exe")) && Directory.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "MSI Afterburner", "Profiles")) && Directory.GetFiles(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "MSI Afterburner", "Profiles")).Any(file => !file.EndsWith("MSIAfterburner.cfg", StringComparison.OrdinalIgnoreCase)))
		{
			await Process.Start(new ProcessStartInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "MSI Afterburner", "MSIAfterburner.exe")) { Arguments = "/Profile1 /q" })!.WaitForExitAsync();
		}

		// launch obs studio
		if (!(localSettings.Values["OBS"] as int? == 0) && File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "obs-studio", "bin", "64bit", "obs64.exe")))
		{
			ProcessActions.CleanDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "obs-studio", ".sentinel"));
			Process.Start(new ProcessStartInfo { FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "obs-studio", "bin", "64bit", "obs64.exe"), Arguments = "--disable-updater --startreplaybuffer --minimize-to-tray", WorkingDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "obs-studio", "bin", "64bit") });
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
			Margin = new Thickness(4, -28, 4, 36)
		});

		// delay
		await Task.Delay(2000);

		// remove infobar
		CruInfo.Children.Clear();
	}
}
