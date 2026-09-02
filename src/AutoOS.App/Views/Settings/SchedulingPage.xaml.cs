using AutoOS.App.Views.Settings.Scheduling;
using AutoOS.Core.Data.Models.CPU;
using AutoOS.Core.Data.Models.Device;
using AutoOS.Core.Helpers.CPU;
using AutoOS.Core.Helpers.Device;
using AutoOS.Core.Helpers.Scheduling;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace AutoOS.App.Views.Settings;

public sealed partial class SchedulingPage : Page
{
	public ObservableCollection<SchedulingGroup> Nodes { get; } = [];
	private CpuSetsInfo _cpuSetsInfo = null!;

	public SchedulingPage()
	{
		InitializeComponent();
		Loaded += SchedulingPage_Loaded;
	}
	private void SchedulingPage_Loaded(object sender, RoutedEventArgs e)
	{
		_cpuSetsInfo = CpuHelper.GetCpuSets();
		Nodes.Clear();

		var audioGroup = new SchedulingGroup { Name = "Audio Controllers", IsExpanded = true };
		LoadDeviceGroup(DeviceType.AudioController, audioGroup);
		if (audioGroup.SubItems.Count > 0)
			Nodes.Add(audioGroup);

		var gpuGroup = new SchedulingGroup { Name = "Graphics Cards", IsExpanded = true };
		LoadDeviceGroup(DeviceType.GPU, gpuGroup);
		if (gpuGroup.SubItems.Count > 0)
			Nodes.Add(gpuGroup);

		var xhciGroup = new SchedulingGroup { Name = "XHCI Controllers", IsExpanded = true };
		LoadDeviceGroup(DeviceType.XHCI, xhciGroup);
		if (xhciGroup.SubItems.Count > 0)
			Nodes.Add(xhciGroup);

		var nicGroup = new SchedulingGroup { Name = "Network Interface Controllers", IsExpanded = true };
		LoadDeviceGroup(DeviceType.NIC, nicGroup);
		if (nicGroup.SubItems.Count > 0)
			Nodes.Add(nicGroup);
	}
	private static void LoadDeviceGroup(DeviceType type, SchedulingGroup group)
	{
		List<DeviceInfo> devices = DeviceHelper.GetDevices(type);

		var items = devices
			.Where(device => device.SupportsIrq)
			.Select(device => new SchedulingItem
			{
				DeviceType = type,
				DeviceDescription = device.DeviceDescription,
				FriendlyName = device.FriendlyName,
				DevObjName = device.DevObjName,
				PnpDeviceId = device.PnpDeviceId,
				Location = device.Location,
				MsiSupported = device.MsiSupported,
				MsiLimit = device.MsiLimit,
				MaxMsiLimit = device.MaxMsiLimit,
				DevicePolicy = device.DevicePolicy,
				DevicePriority = device.DevicePriority,
				AssignmentSetOverride = device.AssignmentSetOverride,
			})
			.OrderBy(i => i.Name, StringComparer.CurrentCultureIgnoreCase)
			.ToList();

		foreach (SchedulingItem? item in items)
		{
			group.SubItems.Add(item);
		}
	}

	public void UpdateDevice(DeviceType deviceType, string pnpDeviceId, DeviceInfo? targetDevice = null)
	{
		SchedulingItem? item = Nodes.SelectMany(g => g.SubItems).FirstOrDefault(d => string.Equals(d.PnpDeviceId, pnpDeviceId, StringComparison.OrdinalIgnoreCase));
		if (item == null)
			return;

		targetDevice ??= DeviceHelper.GetDevices(item.DeviceType).FirstOrDefault(d => string.Equals(d.PnpDeviceId, pnpDeviceId, StringComparison.OrdinalIgnoreCase));
		if (targetDevice == null)
			return;

		item.MsiSupported = targetDevice.MsiSupported;
		item.MsiLimit = targetDevice.MsiLimit;
		item.MaxMsiLimit = targetDevice.MaxMsiLimit;
		item.DevicePolicy = targetDevice.DevicePolicy;
		item.DevicePriority = targetDevice.DevicePriority;
		item.AssignmentSetOverride = targetDevice.AssignmentSetOverride;
	}

	private async void Optimize_Checked(object sender, RoutedEventArgs e)
	{
		await Task.Delay(1000);
		await SchedulingHelper.OptimizeAffinities(onDeviceUpdated: UpdateDevice);
		Optimize.IsChecked = false;
	}

	private async void TreeView_ItemInvoked(object sender, TreeViewItemInvokedEventArgs args)
	{
		if (args.InvokedItem is SchedulingGroup group)
		{
			group.IsExpanded = !group.IsExpanded;
		}
		else if (args.InvokedItem is SchedulingItem device)
		{
			await ShowAffinityDialog(device);
		}
	}

	private async Task ShowAffinityDialog(SchedulingItem device)
	{
		var schedulingDialog = new SchedulingDialog(device, _cpuSetsInfo);
		var contentDialog = new ContentDialog
		{
			Title = device.Name,
			Content = schedulingDialog,
			PrimaryButtonText = "Apply",
			CloseButtonText = "Cancel",
			DefaultButton = ContentDialogButton.Close,
			XamlRoot = XamlRoot,
		};

		contentDialog.Resources["ContentDialogMaxWidth"] = 1350;
		contentDialog.Resources["ContentDialogMaxHeight"] = 900;

		ApplyResult? applyResult = null;
		var applyEventCompleted = new TaskCompletionSource<bool>();

		schedulingDialog.ViewModel.OnSettingsApplied += result =>
		{
			applyResult = result;
			applyEventCompleted.TrySetResult(true);
		};

		contentDialog.PrimaryButtonClick += async (_, _) =>
		{
			schedulingDialog.ViewModel.ApplySettings();
			await applyEventCompleted.Task;

			if (applyResult != null)
			{
				if (applyResult.AppliedSettings.TryGetValue(device.PnpDeviceId, out DeviceInfo? updatedDevice))
				{
					device.MsiSupported = updatedDevice.MsiSupported;
					device.MsiLimit = updatedDevice.MsiLimit;
					device.MaxMsiLimit = updatedDevice.MaxMsiLimit;
					device.DevicePolicy = updatedDevice.DevicePolicy;
					device.DevicePriority = updatedDevice.DevicePriority;
					device.AssignmentSetOverride = updatedDevice.AssignmentSetOverride;
				}
				else
				{
					UpdateDevice(device.DeviceType, device.PnpDeviceId);
				}
			}
		};

		ContentDialogResult result = await contentDialog.ShowAsync();

		if (result == ContentDialogResult.Primary && applyResult != null && applyResult.Success && applyResult.NeedsRestart)
		{
			var restartDialog = new ContentDialog
			{
				Title = $"Restart {device.Name}?",
				Content = "Your changes will not take effect until the device is restarted.\nWould you like to attempt to restart it now?",
				PrimaryButtonText = "Yes",
				CloseButtonText = "No",
				DefaultButton = ContentDialogButton.Close,
				XamlRoot = XamlRoot
			};

			if (await restartDialog.ShowAsync() == ContentDialogResult.Primary)
			{
				RestartResult restartDevicesResult = await DeviceHelper.RestartDevicesAsync(applyResult.ChangedDevices);

				string message = restartDevicesResult.SuccessCount > 0 && restartDevicesResult.FailedCount == 0
					? $"{device.Name} was successfully restarted."
					: restartDevicesResult.SuccessCount > 0
					? $"{device.Name} was restarted. A reboot may be required."
					: $"{device.Name} could not be restarted. Changes will take effect the next time you reboot.";

				if (restartDevicesResult.SuccessCount > 0)
				{
					await MessageBox.ShowSuccessAsync(App.MainWindow, message, "Success");
				}
				else
				{
					await MessageBox.ShowErrorAsync(App.MainWindow, message, "Failure");
				}
			}
		}
	}
}
