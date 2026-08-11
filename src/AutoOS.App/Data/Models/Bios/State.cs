using System.Collections;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AutoOS.App.Data.Models.Bios;

public sealed partial class State(Setting setting) : ObservableObject, INotifyDataErrorInfo
{
	public Setting Setting { get; } = setting;

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(IsModified))]
	[NotifyPropertyChangedFor(nameof(DisplayCurrent))]
	public partial string? Value { get; set; }

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(IsModified))]
	[NotifyPropertyChangedFor(nameof(DisplayCurrent))]
	public partial Option? SelectedOption { get; set; }

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(IsModified))]
	[NotifyPropertyChangedFor(nameof(DisplayOriginal))]
	public partial string? OriginalValue { get; set; }

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(IsModified))]
	[NotifyPropertyChangedFor(nameof(DisplayOriginal))]
	public partial Option? OriginalSelectedOption { get; set; }

	public bool IsModified => SelectedOption != OriginalSelectedOption || Value != OriginalValue;

	public string DisplayCurrent => SelectedOption?.Label ?? Value ?? string.Empty;

	public string DisplayOriginal => OriginalSelectedOption?.Label ?? OriginalValue ?? string.Empty;

	partial void OnValueChanged(string? value) => RaiseErrorsChanged();

	partial void OnSelectedOptionChanged(Option? value) => RaiseErrorsChanged();

	public bool HasErrors => Errors.Length > 0;

	public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

	public IEnumerable GetErrors(string? propertyName)
	{
		if (propertyName != null && propertyName != nameof(Value) && propertyName != nameof(SelectedOption))
			return Array.Empty<string>();

		return Errors;
	}

	public void RaiseErrorsChanged() =>
		ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(Value)));

	private string[] Errors => Validation.GetErrors(this, Setting.HasOptions);
}
