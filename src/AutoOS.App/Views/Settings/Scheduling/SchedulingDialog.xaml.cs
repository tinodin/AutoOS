using AutoOS.App.Views.Settings.Scheduling.ViewModels;
using AutoOS.Core.Data.Models.CPU;
using AutoOS.Core.Data.Models.Device;
using AutoOS.Core.Helpers.Device;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace AutoOS.App.Views.Settings.Scheduling;

public sealed partial class SchedulingDialog : ContentDialog
{
	public DeviceAffinityViewModel ViewModel
	{
		get => (DeviceAffinityViewModel)DataContext;
		set => DataContext = value;
	}

	public string Location { get; }

	public string DeviceName { get; }

	public ApplyResult? ApplyResult { get; private set; }

	internal SchedulingDialog(SchedulingItem device, CpuSetsInfo cpuSetsInfo)
	{
		Location = device.Location;
		DeviceName = device.Name;
		ViewModel = new DeviceAffinityViewModel(device, cpuSetsInfo);
		InitializeComponent();
	}

	private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
	{
		ContentDialogButtonClickDeferral deferral = args.GetDeferral();

		try
		{
			ApplyResult = ViewModel.ApplySettings();
		}
		finally
		{
			deferral.Complete();
		}
	}

	private void GroupItemsControl_Loaded(object sender, RoutedEventArgs e)
	{
		if (sender is not ItemsControl itemsControl)
			return;

		if (itemsControl.DataContext is not CpuCoreGroup group)
			return;

		if (itemsControl.ItemsPanelRoot is CommunityToolkit.WinUI.Controls.UniformGrid uniformGrid)
		{
			uniformGrid.Columns = group.RecommendedColumns;

			group.PropertyChanged += (s, args) =>
			{
				if (args.PropertyName == nameof(CpuCoreGroup.RecommendedColumns))
					uniformGrid.Columns = group.RecommendedColumns;
			};
		}
	}
}
