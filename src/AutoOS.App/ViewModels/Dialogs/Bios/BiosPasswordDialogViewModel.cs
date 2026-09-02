using AutoOS.Core.Helpers.Bios;

namespace AutoOS.App.ViewModels.Dialogs.Bios;

public sealed partial class BiosPasswordDialogViewModel : BaseDialogViewModel, INotifyDataErrorInfo
{
	[ObservableProperty]
	public partial string Password { get; set; } = string.Empty;

	[ObservableProperty]
	public partial string? ErrorMessage { get; set; }

	[ObservableProperty]
	public partial bool HasError { get; set; }

	private readonly Dictionary<string, List<string>> _validationErrors = [];

	public bool HasErrors => _validationErrors.Count > 0;

	public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

	public IEnumerable GetErrors(string? propertyName)
	{
		if (string.IsNullOrEmpty(propertyName) || !_validationErrors.ContainsKey(propertyName))
			return Array.Empty<string>();

		return _validationErrors[propertyName];
	}

	private void SetErrors(string key, ICollection<string> errors)
	{
		if (errors.Count > 0)
			_validationErrors[key] = [.. errors];
		else
			_validationErrors.Remove(key);

		ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(key));
		OnPropertyChanged(nameof(HasErrors));
	}

	private void ClearErrors(string key)
	{
		if (_validationErrors.Remove(key))
		{
			ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(key));
			OnPropertyChanged(nameof(HasErrors));
		}
	}

	public BiosPasswordDialogViewModel()
	{
		Title = "Unlock Write Protection";
		PrimaryButtonText = "Unlock";
		CloseButtonText = "Cancel";
		IsPrimaryButtonEnabled = false;
	}

	partial void OnPasswordChanged(string value)
	{
		HasError = false;
		ErrorMessage = null;
		ClearErrors(nameof(Password));
		IsPrimaryButtonEnabled = !string.IsNullOrWhiteSpace(value);
	}

	public async Task<bool> TryUnlockAsync()
	{
		if (string.IsNullOrWhiteSpace(Password))
		{
			HasError = true;
			ErrorMessage = "Password is required.";
			SetErrors(nameof(Password), ["Password is required."]);
			return false;
		}

		(bool Success, uint Status) = await Task.Run(() =>
		{
			using AmiSmmTransport transport = new();
			if (!transport.TryLoadAndInit())
				return (false, 0xFFFFFFFEu);

			bool ok = transport.TryUnlockWithPassword(Password, out uint status);
			return (ok, status);
		});

		if (Success)
		{
			HasError = false;
			ErrorMessage = null;
			ClearErrors(nameof(Password));
			return true;
		}

		string message = SmmStatusHelper.Format(Status);

		HasError = true;
		ErrorMessage = message;
		SetErrors(nameof(Password), [message]);

		return false;
	}
}
