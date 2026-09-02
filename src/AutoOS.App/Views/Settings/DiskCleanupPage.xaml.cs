using AutoOS.App.Views.Installer.Actions;
using AutoOS.Core.Helpers.Registry;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Storage;
using Windows.Storage.FileProperties;

namespace AutoOS.App.Views.Settings;

public sealed partial class DiskCleanupPage : Page
{
	private ObservableCollection<DriveModel> drives = [];

	public DiskCleanupPage()
	{
		InitializeComponent();
		_ = InitializeDrives();
	}

	private async Task InitializeDrives()
	{
		drives = await GetDrives();
		DrivesRepeater.ItemsSource = drives;
	}

	private static string FormatSize(double sizeGiB)
	{
		if (sizeGiB < 1)
			return $"{sizeGiB * 1024:N2} MiB";
		if (sizeGiB >= 1024)
			return $"{sizeGiB / 1024:N2} TiB";
		return $"{sizeGiB:N2} GiB";
	}

	private static async Task<ObservableCollection<DriveModel>> GetDrives()
	{
		var result = new ObservableCollection<DriveModel>();

		foreach (DriveInfo drive in DriveInfo.GetDrives().Where(d => d.IsReady))
		{
			double totalGiB = drive.TotalSize / 1073741824d;
			double freeGiB = drive.TotalFreeSpace / 1073741824d;

			var model = new DriveModel
			{
				Name = drive.Name.TrimEnd('\\'),
				Label = $"{drive.VolumeLabel} ({drive.Name.TrimEnd('\\')})",
				Total = totalGiB,
				Free = $"{FormatSize(freeGiB)} free of {FormatSize(totalGiB)}",
				Used = totalGiB - freeGiB
			};

			try
			{
				StorageFolder folder = await StorageFolder.GetFolderFromPathAsync(drive.Name);
				using StorageItemThumbnail? thumb = await folder.GetThumbnailAsync(ThumbnailMode.SingleItem, 32, ThumbnailOptions.UseCurrentScale);
				if (thumb != null)
				{
					var bmp = new BitmapImage();
					await bmp.SetSourceAsync(thumb);
					model.Icon = bmp;
				}
			}
			catch { }

			result.Add(model);
		}

		return result;
	}

	private void UpdateDrives()
	{
		foreach (DriveInfo drive in DriveInfo.GetDrives().Where(d => d.IsReady))
		{
			DriveModel? model = drives.FirstOrDefault(d => d.Name == drive.Name.TrimEnd('\\'));
			if (model == null)
				continue;

			double totalGiB = drive.TotalSize / 1073741824d;
			double freeGiB = drive.TotalFreeSpace / 1073741824d;

			model.Total = totalGiB;
			model.Used = totalGiB - freeGiB;
			model.Free = $"{FormatSize(freeGiB)} free of {FormatSize(totalGiB)}";
		}
	}

	private async void RunDiskCleanup_Checked(object sender, RoutedEventArgs e)
	{
		// clean temp directories
		await Task.WhenAll(Process.GetProcessesByName("TiWorker").Select(async process => { process.Kill(); await process.WaitForExitAsync(); }));

		RegistryHelper.RunAs(RegistryHelper.Identity.TrustedInstaller, () =>
		{
			ProcessActions.CleanDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Logs"));
			ProcessActions.CleanDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Panther"));
			ProcessActions.CleanDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "LogFiles"));
			ProcessActions.CleanDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "SleepStudy"));
			ProcessActions.CleanDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "sru"));
			ProcessActions.CleanDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "WDI"));
			ProcessActions.CleanDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "winevt", "Logs"));
			ProcessActions.CleanDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "SystemTemp"));
			ProcessActions.CleanDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Temp"));
			ProcessActions.CleanDirectory(Path.GetTempPath());
			File.Delete(Path.Combine(Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.System))!, "DumpStack.log"));
		});

		// run disk cleanup
		await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "cleanmgr.exe"), Arguments = "/sagerun:0", UseShellExecute = false, CreateNoWindow = true })!.WaitForExitAsync();

		CleanDisks.IsChecked = false;

		UpdateDrives();
	}

	private void RunDiskCleanup_Unchecked(object sender, RoutedEventArgs e)
	{
		foreach (Process proc in Process.GetProcessesByName("cleanmgr"))
			proc.Kill(true);

		UpdateDrives();
	}
}

public partial class DriveModel : INotifyPropertyChanged
{
	private double total;
	private double used;
	private string free = "";
	private ImageSource? icon;

	public string Name { get; set; } = string.Empty;
	public string Label { get; set; } = string.Empty;

	public double Total
	{
		get => total;
		set { total = value; OnPropertyChanged(nameof(Total)); }
	}

	public double Used
	{
		get => used;
		set { used = value; OnPropertyChanged(nameof(Used)); }
	}

	public ImageSource? Icon
	{
		get => icon;
		set { icon = value; OnPropertyChanged(nameof(Icon)); }
	}

	public string Free
	{
		get => free;
		set { free = value; OnPropertyChanged(nameof(Free)); }
	}

	public event PropertyChangedEventHandler? PropertyChanged;
	private void OnPropertyChanged(string propertyName) =>
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
