using AutoOS.App.Data.Enums;

namespace AutoOS.App.ViewModels.Dialogs;

public interface IDialog<TViewModel> where TViewModel : class, INotifyPropertyChanged
{
	TViewModel ViewModel { get; set; }

	Task<DialogResult> ShowAsync();

	void Hide();
}
