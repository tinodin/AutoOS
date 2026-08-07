using System.ComponentModel;
using AutoOS.App.Data.Enums;
using AutoOS.App.ViewModels.Dialogs;
using AutoOS.App.ViewModels.Dialogs.Power;
using AutoOS.App.Views.Settings.Dialogs.Power;

namespace AutoOS.App.Services;

public sealed class DialogService : IDialogService
{
	private readonly Dictionary<Type, Func<ContentDialog>> _dialogFactories = new()
	{
		{ typeof(EditDialogViewModel), () => new EditDialog() }
	};

	public IDialog<TViewModel> GetDialog<TViewModel>(TViewModel viewModel)
		where TViewModel : class, INotifyPropertyChanged
	{
		if (!_dialogFactories.TryGetValue(typeof(TViewModel), out var factory))
			throw new InvalidOperationException($"No dialog registered for ViewModel type '{typeof(TViewModel).Name}'.");

		var dialog = factory();

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
			DefaultButton = ContentDialogButton.Primary,
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
