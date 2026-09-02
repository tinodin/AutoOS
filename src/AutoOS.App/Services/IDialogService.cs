using AutoOS.App.Data.Enums;
using AutoOS.App.ViewModels.Dialogs;

namespace AutoOS.App.Services;

public interface IDialogService
{
	IDialog<TViewModel> GetDialog<TViewModel>(TViewModel viewModel) where TViewModel : class, INotifyPropertyChanged;

	Task<DialogResult> ShowDialogAsync<TViewModel>(TViewModel viewModel) where TViewModel : class, INotifyPropertyChanged;

	Task<DialogResult> ShowConfirmationDialogAsync(string title, string message, string primaryButtonText, string closeButtonText);
}
