using AutoOS.App.Data.Enums;
using AutoOS.App.ViewModels.Dialogs;
using AutoOS.App.ViewModels.Dialogs.Power;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace AutoOS.App.Views.Settings.Dialogs.Power;

public sealed partial class EditDialog : ContentDialog, IDialog<EditDialogViewModel>
{
	public EditDialogViewModel ViewModel
	{
		get => (EditDialogViewModel)DataContext;
		set => DataContext = value;
	}

	public EditDialog()
	{
		InitializeComponent();
		Opened += EditDialog_Opened;
	}

	public new async Task<DialogResult> ShowAsync()
	{
		return (DialogResult)await base.ShowAsync();
	}

	public void Hide()
	{
		base.Hide();
	}

	private void EditDialog_Opened(ContentDialog sender, ContentDialogOpenedEventArgs args)
	{
		DispatcherQueue.TryEnqueue(() =>
		{
			NameTextBox.Focus(FocusState.Programmatic);
			NameTextBox.SelectAll();
		});
	}
}