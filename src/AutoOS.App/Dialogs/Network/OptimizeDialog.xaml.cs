using AutoOS.App.Data.Enums;
using AutoOS.App.ViewModels.Dialogs;
using AutoOS.App.ViewModels.Dialogs.Network;
using AutoOS.Core.Data.Models.Device;
using Microsoft.UI.Xaml.Controls;

namespace AutoOS.App.Dialogs.Network;

public sealed partial class OptimizeDialog : ContentDialog, IDialog<OptimizeDialogViewModel>
{
	public OptimizeDialogViewModel ViewModel
	{
		get => (OptimizeDialogViewModel)DataContext;
		set => DataContext = value;
	}

	public OptimizeDialog()
	{
		InitializeComponent();
	}

	public new async Task<DialogResult> ShowAsync()
	{
		return (DialogResult)await base.ShowAsync();
	}

	private void Adapters_SelectionChanged(object sender, SelectionChangedEventArgs e)
		=> ViewModel.SelectedAdapters = Adapters.SelectedItems.OfType<DeviceInfo>().ToList();
}
