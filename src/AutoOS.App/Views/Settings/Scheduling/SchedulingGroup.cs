using System.Runtime.CompilerServices;
using AutoOS.Core.Data.Models.Device;
using WinRT;

namespace AutoOS.App.Views.Settings.Scheduling;

[GeneratedBindableCustomProperty]
public partial class SchedulingGroup : INotifyPropertyChanged
{
	public DeviceType DeviceType { get; set; }

	public string Name { get; set; } = null!;

	private bool _isExpanded;
	public bool IsExpanded
	{
		get => _isExpanded;
		set
		{
			if (_isExpanded != value)
			{
				_isExpanded = value;
				OnPropertyChanged();
			}
		}
	}

	public ObservableCollection<SchedulingItem> SubItems { get; } = [];

	public event PropertyChangedEventHandler? PropertyChanged;
	protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
		=> PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
