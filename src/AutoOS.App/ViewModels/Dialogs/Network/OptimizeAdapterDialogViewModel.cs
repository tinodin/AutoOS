using System.Collections.Specialized;
using AutoOS.Core.Data.Models.Device;

namespace AutoOS.App.ViewModels.Dialogs.Network;

public sealed partial class OptimizeAdapterDialogViewModel : BaseDialogViewModel
{
	public IReadOnlyList<DeviceInfo> Adapters { get; }

	public ObservableCollection<DeviceInfo> SelectedAdapters { get; } = [];

	public OptimizeAdapterDialogViewModel(IEnumerable<DeviceInfo> adapters)
	{
		Adapters = adapters.ToList();
		Title = "Optimize";
		PrimaryButtonText = "Optimize";
		CloseButtonText = "Cancel";
		SelectedAdapters.CollectionChanged += OnSelectedAdaptersChanged;
		if (Adapters.FirstOrDefault() is { } first)
			SelectedAdapters.Add(first);
		IsPrimaryButtonEnabled = SelectedAdapters.Count > 0;
	}

	public void SetSelectedAdapters(IEnumerable<DeviceInfo> adapters)
	{
		SelectedAdapters.CollectionChanged -= OnSelectedAdaptersChanged;
		try
		{
			SelectedAdapters.Clear();
			foreach (DeviceInfo adapter in adapters)
				SelectedAdapters.Add(adapter);
		}
		finally
		{
			SelectedAdapters.CollectionChanged += OnSelectedAdaptersChanged;
		}

		IsPrimaryButtonEnabled = SelectedAdapters.Count > 0;
	}

	private void OnSelectedAdaptersChanged(object? sender, NotifyCollectionChangedEventArgs e) => IsPrimaryButtonEnabled = SelectedAdapters.Count > 0;
}
