using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AutoOS.App.Data.Models.Bios;

public sealed partial class State : INotifyPropertyChanged
{
	private string? _value;
	private Option? _selectedOption;

	public string? Value
	{
		get => _value;
		set
		{
			if (_value != value)
			{
				_value = value;
				OnPropertyChanged();
			}
		}
	}

	public Option? SelectedOption
	{
		get => _selectedOption;
		set
		{
			if (_selectedOption != value)
			{
				_selectedOption = value;
				OnPropertyChanged();
			}
		}
	}

	public string? OriginalValue { get; set; }

	public Option? OriginalSelectedOption { get; set; }

	public bool IsModified => SelectedOption != OriginalSelectedOption || Value != OriginalValue;

	public event PropertyChangedEventHandler? PropertyChanged;

	private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}