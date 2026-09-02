namespace AutoOS.App.ViewModels.Dialogs.Power;

public sealed partial class EditDialogViewModel : BaseDialogViewModel
{
	public EditDialogViewModel(string name, string description)
	{
		Name = name;
		Description = description;
		Title = "Edit Power Plan";
		IsPrimaryButtonEnabled = !string.IsNullOrWhiteSpace(name);
		PrimaryButtonText = "Save";
		CloseButtonText = "Cancel";
	}

	[ObservableProperty]
	public partial string Name { get; set; }

	[ObservableProperty]
	public partial string Description { get; set; }

	partial void OnNameChanged(string value) => IsPrimaryButtonEnabled = !string.IsNullOrWhiteSpace(value);
}
