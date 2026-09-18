using AutoOS.App.Data.Enums;
using AutoOS.App.ViewModels.Dialogs;
using AutoOS.App.ViewModels.Dialogs.Network;
using AutoOS.Core.Data.Models.Device;
using Microsoft.UI.Xaml.Controls;

namespace AutoOS.App.Dialogs.Network;

public sealed partial class OptimizeAdapterDialog : ContentDialog, IDialog<OptimizeAdapterDialogViewModel>
{
	private bool _syncingSelection;

	public OptimizeAdapterDialogViewModel ViewModel
	{
		get => (OptimizeAdapterDialogViewModel)DataContext;
		set => DataContext = value;
	}

	public OptimizeAdapterDialog()
	{
		InitializeComponent();
	}

	public new async Task<DialogResult> ShowAsync()
	{
		return (DialogResult)await base.ShowAsync();
	}

	private void Adapters_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
	{
		_syncingSelection = true;
		try
		{
			foreach (DeviceInfo adapter in ViewModel.SelectedAdapters.ToList())
			{
				if (!Adapters.SelectedItems.Contains(adapter))
					Adapters.SelectedItems.Add(adapter);
			}
		}
		finally
		{
			_syncingSelection = false;
		}
	}

	private void Adapters_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (_syncingSelection)
			return;

		ViewModel.SetSelectedAdapters(Adapters.SelectedItems.OfType<DeviceInfo>());
	}
}
