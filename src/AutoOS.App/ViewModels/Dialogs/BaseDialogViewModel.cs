using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AutoOS.App.ViewModels.Dialogs;

public abstract partial class BaseDialogViewModel : ObservableObject
{
	[ObservableProperty]
	public partial string? Title { get; set; }

	[ObservableProperty]
	public partial bool IsPrimaryButtonEnabled { get; set; }

	[ObservableProperty]
	public partial bool IsSecondaryButtonEnabled { get; set; }

	[ObservableProperty]
	public partial string? PrimaryButtonText { get; set; }

	[ObservableProperty]
	public partial string? SecondaryButtonText { get; set; }

	[ObservableProperty]
	public partial string? CloseButtonText { get; set; }

	public ICommand? PrimaryButtonClickCommand { get; protected init; }

	public ICommand? SecondaryButtonClickCommand { get; protected init; }

	public ICommand? CloseButtonClickCommand { get; protected init; }
}
