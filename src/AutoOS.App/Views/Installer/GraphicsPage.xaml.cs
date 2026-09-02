using System.Text.Json.Nodes;
using AutoOS.Core.Data.Models.GPU;
using AutoOS.Core.Helpers.GPU;
using AutoOS.Core.Helpers.Picker;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Windows.Storage;

namespace AutoOS.App.Views.Installer;

public sealed partial class GraphicsPage : Page
{
	private bool isInitializingOBSState = true;
	private readonly ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
	public ObservableCollection<GpuInfo> GPUs { get; } = [];
	public GraphicsPage()
	{
		InitializeComponent();
		GetGpus();
		GetMsiProfile();
		GetOBSState();
		Unloaded += GraphicsPage_Unloaded;
	}

	protected override void OnNavigatedTo(NavigationEventArgs e)
	{
		base.OnNavigatedTo(e);
		MainWindow.Instance.MarkVisited(nameof(GraphicsPage));
		MainWindow.Instance.CheckAllPagesVisited();
	}

	private void GraphicsPage_Unloaded(object sender, RoutedEventArgs e)
	{
		var array = new JsonArray();
		foreach (GpuInfo gpu in GPUs)
		{
			array.Add((JsonNode)new JsonObject
			{
				["Name"] = gpu.DeviceName,
				["PnPDeviceId"] = gpu.PnPDeviceId,
				["VendorId"] = gpu.VendorId,
				["DeviceId"] = gpu.DeviceId,
				["Codename"] = gpu.Codename,
				["Install"] = gpu.Install,
				["IsInstalled"] = gpu.IsInstalled,
				["RegistryPath"] = gpu.RegistryPath,
				["Location"] = gpu.Location,
				["PStates"] = gpu.PStates,
				["ECC"] = gpu.ECC,
				["GspFirmware"] = gpu.GspFirmware,
				["HDCP"] = gpu.HDCP,
				["HDMIDPAudio"] = gpu.HDMIDPAudio,
				["CurrentVersion"] = gpu.CurrentVersion
			});
		}
		localSettings.Values["GPUs"] = array.ToJsonString();
	}

	public void GetGpus()
	{
		GPUs.Clear();

		List<GpuInfo> savedGpus = [];

		if (localSettings.Values.TryGetValue("GPUs", out object? savedObj))
		{
			try
			{
				JsonArray? array = JsonNode.Parse(savedObj!.ToString()!)?.AsArray();
				if (array != null)
				{
					foreach (JsonNode? node in array)
					{
						JsonObject? obj = node?.AsObject();
						if (obj == null)
							continue;

						savedGpus.Add(new GpuInfo
						{
							DeviceName = obj["Name"]?.ToString() ?? string.Empty,
							PnPDeviceId = obj["PnPDeviceId"]?.ToString() ?? string.Empty,
							VendorId = obj["VendorId"]?.ToString() ?? string.Empty,
							DeviceId = obj["DeviceId"]?.ToString() ?? string.Empty,
							Codename = obj["Codename"]?.ToString() ?? string.Empty,
							Install = obj["Install"]?.GetValue<bool>() ?? false,
							IsInstalled = obj["IsInstalled"]?.GetValue<bool>() ?? false,
							RegistryPath = obj["RegistryPath"]?.ToString() ?? string.Empty,
							Location = obj["Location"]?.ToString() ?? string.Empty,
							PStates = obj["PStates"]?.GetValue<bool>() ?? false,
							ECC = obj["ECC"]?.GetValue<bool>() ?? false,
							GspFirmware = obj["GspFirmware"]?.GetValue<bool>() ?? false,
							HDCP = obj["HDCP"]?.GetValue<bool>() ?? false,
							HDMIDPAudio = obj["HDMIDPAudio"]?.GetValue<bool>() ?? false,
							CurrentVersion = obj["CurrentVersion"]?.ToString() ?? string.Empty
						});
					}
				}
			}
			catch { }
		}

		List<GpuInfo> detectedGpus = GpuHelper.GetGPUs();
		detectedGpus = [.. detectedGpus.OrderBy(g => g.Location)];

		foreach (GpuInfo gpu in detectedGpus)
		{
			GpuInfo? saved = savedGpus?.FirstOrDefault(x => x.PnPDeviceId == gpu.PnPDeviceId);

			if (saved != null)
			{
				gpu.Install = saved.Install;
				gpu.PStates = saved.PStates;
				gpu.ECC = saved.ECC;
				gpu.GspFirmware = saved.GspFirmware;
				gpu.HDCP = saved.HDCP;
				gpu.HDMIDPAudio = saved.HDMIDPAudio;
			}

			GPUs.Add(gpu);
		}
	}

	private void GetMsiProfile()
	{
		string? value = localSettings.Values["MsiProfile"] as string;
		if (!string.IsNullOrEmpty(value))
		{
			var infoBar = new InfoBar
			{
				Title = value,
				IsClosable = true,
				IsOpen = true,
				Severity = InfoBarSeverity.Success,
				Margin = new Thickness(0, 0, 0, 12)
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
		senderButton!.IsEnabled = false;
		MsiAfterburnerInfo.Children.Clear();

		MsiAfterburnerInfo.Children.Add(new InfoBar
		{
			Title = "Please select a MSI Afterburner profile (.cfg).",
			IsClosable = false,
			IsOpen = true,
			Severity = InfoBarSeverity.Informational,
			Margin = new Thickness(0, 0, 0, 12)
		});

		await Task.Delay(300);

		var picker = new FilePicker(App.MainWindow)
		{
			ShowAllFilesOption = false
		};
		picker.FileTypeChoices.Add("MSI Afterburner profile", ["*.cfg"]);
		StorageFile? file = await picker.PickSingleFileAsync();

		if (file != null)
		{
			string fileContent = await FileIO.ReadTextAsync(file);

			if (fileContent.Contains("[Startup]"))
			{
				senderButton!.IsEnabled = true;
				MsiAfterburnerInfo.Children.Clear();

				localSettings.Values["MsiProfile"] = file.Path;

				var infoBar = new InfoBar
				{
					Title = file.Path,
					IsClosable = true,
					IsOpen = true,
					Severity = InfoBarSeverity.Success,
					Margin = new Thickness(0, 0, 0, 12)
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
				senderButton!.IsEnabled = true;
				MsiAfterburnerInfo.Children.Clear();

				MsiAfterburnerInfo.Children.Add(new InfoBar
				{
					Title = "The selected file is not a valid MSI Afterburner profile.",
					IsClosable = false,
					IsOpen = true,
					Severity = InfoBarSeverity.Error,
					Margin = new Thickness(0, 0, 0, 12)
				});

				await Task.Delay(2000);
				MsiAfterburnerInfo.Children.Clear();
			}
		}
		else
		{
			senderButton!.IsEnabled = true;
			MsiAfterburnerInfo.Children.Clear();
			GetMsiProfile();
		}
	}

	private void GetOBSState()
	{
		if (!localSettings.Values.TryGetValue("OBS", out object? value))
		{
			localSettings.Values["OBS"] = 0;
		}
		else
		{
			OBS.IsOn = (int)value == 1;
		}

		isInitializingOBSState = false;
	}

	private void OBS_Toggled(object sender, RoutedEventArgs e)
	{
		if (isInitializingOBSState)
			return;
		localSettings.Values["OBS"] = OBS.IsOn ? 1 : 0;
	}
}
