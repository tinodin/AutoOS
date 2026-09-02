using AutoOS.App.ViewModels.Dialogs.Bios;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace AutoOS.App.Dialogs.Bios;

public sealed partial class BiosPasswordDialog : ContentDialog, ViewModels.Dialogs.IDialog<BiosPasswordDialogViewModel>
{
	public BiosPasswordDialogViewModel ViewModel
	{
		get => (BiosPasswordDialogViewModel)DataContext;
		set => DataContext = value;
	}

	public BiosPasswordDialog()
	{
		InitializeComponent();
	}

	public new async Task<Data.Enums.DialogResult> ShowAsync()
	{
		return (Data.Enums.DialogResult)await base.ShowAsync();
	}

	private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
	{
		if (ViewModel != null)
			ViewModel.Password = PasswordBox.Password;
	}

	private async void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
	{
		ContentDialogButtonClickDeferral deferral = args.GetDeferral();
		try
		{
			bool success = await ViewModel.TryUnlockAsync();
			if (!success)
				args.Cancel = true;
		}
		finally
		{
			deferral.Complete();
		}
	}
}
