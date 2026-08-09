using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AutoOS.App.Data.Models.Bios;

public partial class Option : INotifyPropertyChanged
{
	private bool _isSelected;

	public event PropertyChangedEventHandler? PropertyChanged;

	protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

	public string Index { get; set; } = string.Empty;

	public string Label { get; set; } = string.Empty;

	public BiosSettingsModel? Parent { get; set; }

	public bool IsSelected
	{
		get => _isSelected;
		set
		{
			if (_isSelected != value)
			{
				_isSelected = value;
				OnPropertyChanged();

				if (_isSelected && Parent != null)
				{
					foreach (Option opt in Parent.Options)
					{
						if (!ReferenceEquals(opt, this) && opt.IsSelected)
							opt.IsSelected = false;
					}

					Parent.SelectedOption = this;
				}
			}
		}
	}
}