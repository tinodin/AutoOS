using AutoOS.App.Data.Enums;
using AutoOS.App.ViewModels.Dialogs;
using AutoOS.App.ViewModels.Dialogs.Power;
using Microsoft.UI.Xaml.Controls;

namespace AutoOS.App.Dialogs.Power;

public sealed partial class CompareDialog : ContentDialog, IDialog<CompareDialogViewModel>
{
	public CompareDialogViewModel ViewModel
	{
		get => (CompareDialogViewModel)DataContext;
		set => DataContext = value;
	}

	public CompareDialog()
	{
		InitializeComponent();
	}

	public new async Task<DialogResult> ShowAsync()
	{
		return (DialogResult)await base.ShowAsync();
	}
}
