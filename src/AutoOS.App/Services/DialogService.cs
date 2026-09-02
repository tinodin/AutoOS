using AutoOS.App.Data.Enums;
using AutoOS.App.Dialogs.Bios;
using AutoOS.App.Dialogs.Power;
using AutoOS.App.ViewModels.Dialogs;
using AutoOS.App.ViewModels.Dialogs.Bios;
using AutoOS.App.ViewModels.Dialogs.Power;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace AutoOS.App.Services;

public sealed class DialogService : IDialogService
{
	private readonly Dictionary<Type, Func<ContentDialog>> _dialogFactories = new()
	{
		{ typeof(EditDialogViewModel), () => new EditDialog() },
		{ typeof(BiosPasswordDialogViewModel), () => new BiosPasswordDialog() }
	};

	public IDialog<TViewModel> GetDialog<TViewModel>(TViewModel viewModel)
		where TViewModel : class, INotifyPropertyChanged
	{
		if (!_dialogFactories.TryGetValue(typeof(TViewModel), out Func<ContentDialog>? factory))
			throw new InvalidOperationException($"No dialog registered for ViewModel type '{typeof(TViewModel).Name}'.");

		ContentDialog dialog = factory();

		if (dialog is IDialog<TViewModel> typedDialog)
			typedDialog.ViewModel = viewModel;
		else
			dialog.DataContext = viewModel;

		if (dialog is not IDialog<TViewModel> resultDialog)
			throw new InvalidOperationException($"The dialog for ViewModel type '{typeof(TViewModel).Name}' does not implement {typeof(IDialog<TViewModel>).Name}.");

		dialog.XamlRoot = App.MainWindow.Content.XamlRoot;
		return resultDialog;
	}

	public async Task<DialogResult> ShowDialogAsync<TViewModel>(TViewModel viewModel)
		where TViewModel : class, INotifyPropertyChanged
	{
		return await GetDialog(viewModel).ShowAsync();
	}

	public async Task<DialogResult> ShowConfirmationDialogAsync(string title, string message, string primaryButtonText, string closeButtonText)
	{
		var dialog = new ContentDialog
		{
			Title = title,
			Content = new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap },
			PrimaryButtonText = primaryButtonText,
			CloseButtonText = closeButtonText,
			DefaultButton = ContentDialogButton.Close,
			XamlRoot = App.MainWindow.Content.XamlRoot
		};

		return await dialog.ShowAsync() switch
		{
			ContentDialogResult.Primary => DialogResult.Primary,
			ContentDialogResult.Secondary => DialogResult.Secondary,
			_ => DialogResult.None
		};
	}
}
