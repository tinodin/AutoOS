using AutoOS.App.Views.Settings.Scheduling.ViewModels;
using AutoOS.Core.Helpers.CPU.Models;

namespace AutoOS.App.Views.Settings.Scheduling;

public sealed partial class SchedulingDialog : Page
{
	public DeviceAffinityViewModel ViewModel { get; } = null!;

	public string Location { get; } = null!;

	public SchedulingDialog()
	{
		InitializeComponent();
	}

	internal SchedulingDialog(SchedulingItem device, CpuSetsInfo cpuSetsInfo)
	{
		Location = device.Location;
		ViewModel = new DeviceAffinityViewModel(device, cpuSetsInfo);
		InitializeComponent();
	}

	private void GroupItemsControl_Loaded(object sender, RoutedEventArgs e)
	{
		if (sender is not ItemsControl itemsControl)
			return;

		CpuCoreGroup? group = itemsControl.DataContext as CpuCoreGroup;
		if (group == null)
			return;

		if (itemsControl.ItemsPanelRoot is CommunityToolkit.WinUI.Controls.UniformGrid uniformGrid)
		{
			uniformGrid.Columns = group.RecommendedColumns;

			group.PropertyChanged += (s, args) =>
			{
				if (args.PropertyName == nameof(CpuCoreGroup.RecommendedColumns))
				{
					uniformGrid.Columns = group.RecommendedColumns;
				}
			};
		}
	}
}
