using AutoOS.Core.Data.Models.Device;

namespace AutoOS.App.ViewModels.Dialogs.Network;

public sealed partial class OptimizeDialogViewModel : BaseDialogViewModel
{
	public IReadOnlyList<DeviceInfo> Adapters { get; }

	[ObservableProperty]
	public partial IReadOnlyList<DeviceInfo> SelectedAdapters { get; set; } = [];

	public OptimizeDialogViewModel(IEnumerable<DeviceInfo> adapters, Func<DeviceInfo, bool> canApply)
	{
		Adapters = adapters.Where(canApply).ToList();
		Title = "Optimize";
		PrimaryButtonText = "Optimize";
		CloseButtonText = "Cancel";
		IsPrimaryButtonEnabled = false;
	}

	partial void OnSelectedAdaptersChanged(IReadOnlyList<DeviceInfo> value) => IsPrimaryButtonEnabled = value.Count > 0;
}