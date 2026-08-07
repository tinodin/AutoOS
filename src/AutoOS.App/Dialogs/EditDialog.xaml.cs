using AutoOS.App.Data.Enums;
using AutoOS.App.ViewModels.Dialogs;
using AutoOS.App.ViewModels.Dialogs.Power;

namespace AutoOS.App.Dialogs;

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
	}

	public new async Task<DialogResult> ShowAsync()
	{
		return (DialogResult)await base.ShowAsync();
	}
}
