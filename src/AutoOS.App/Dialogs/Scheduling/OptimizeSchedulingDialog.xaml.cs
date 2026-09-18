using AutoOS.App.Data.Enums;
using AutoOS.App.ViewModels.Dialogs;
using AutoOS.App.ViewModels.Dialogs.Scheduling;
using AutoOS.App.Views.Settings.Scheduling;
using Microsoft.UI.Xaml.Controls;

namespace AutoOS.App.Dialogs.Scheduling;

public sealed partial class OptimizeSchedulingDialog : ContentDialog, IDialog<OptimizeSchedulingDialogViewModel>
{
	private bool _syncingSelection;

	public OptimizeSchedulingDialogViewModel ViewModel
	{
		get => (OptimizeSchedulingDialogViewModel)DataContext;
		set => DataContext = value;
	}

	public OptimizeSchedulingDialog()
	{
		InitializeComponent();
	}

	public new async Task<DialogResult> ShowAsync()
	{
		return (DialogResult)await base.ShowAsync();
	}

	private void Devices_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
	{
		_syncingSelection = true;
		try
		{
			foreach (SchedulingItem device in ViewModel.SelectedDevices.ToList())
			{
				if (!Devices.SelectedItems.Contains(device))
					Devices.SelectedItems.Add(device);
			}
		}
		finally
		{
			_syncingSelection = false;
		}
	}

	private void Devices_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (_syncingSelection)
			return;

		ViewModel.SetSelectedDevices(Devices.SelectedItems.OfType<SchedulingItem>());
	}
}
