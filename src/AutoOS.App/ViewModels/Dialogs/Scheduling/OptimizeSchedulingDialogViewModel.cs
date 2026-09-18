using System.Collections.Specialized;
using AutoOS.App.Views.Settings.Scheduling;

namespace AutoOS.App.ViewModels.Dialogs.Scheduling;

public sealed partial class OptimizeSchedulingDialogViewModel : BaseDialogViewModel
{
	public IReadOnlyList<SchedulingItem> Devices { get; }

	public ObservableCollection<SchedulingItem> SelectedDevices { get; } = [];

	public OptimizeSchedulingDialogViewModel(IEnumerable<SchedulingItem> devices)
	{
		Devices = devices.ToList();
		Title = "Optimize";
		PrimaryButtonText = "Optimize";
		CloseButtonText = "Cancel";
		SelectedDevices.CollectionChanged += OnSelectedDevicesChanged;

		foreach (SchedulingItem device in Devices)
			SelectedDevices.Add(device);

		IsPrimaryButtonEnabled = SelectedDevices.Count > 0;
	}

	public void SetSelectedDevices(IEnumerable<SchedulingItem> devices)
	{
		SelectedDevices.CollectionChanged -= OnSelectedDevicesChanged;
		try
		{
			SelectedDevices.Clear();
			foreach (SchedulingItem device in devices)
				SelectedDevices.Add(device);
		}
		finally
		{
			SelectedDevices.CollectionChanged += OnSelectedDevicesChanged;
		}

		IsPrimaryButtonEnabled = SelectedDevices.Count > 0;
	}

	private void OnSelectedDevicesChanged(object? sender, NotifyCollectionChangedEventArgs e) => IsPrimaryButtonEnabled = SelectedDevices.Count > 0;
}
