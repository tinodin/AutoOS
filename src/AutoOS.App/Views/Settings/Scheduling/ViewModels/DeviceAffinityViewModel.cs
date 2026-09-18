using System.Runtime.CompilerServices;
using AutoOS.Core.Data.Models.CPU;
using AutoOS.Core.Data.Models.Device;
using AutoOS.Core.Helpers.CPU;
using AutoOS.Core.Helpers.Device;
using Microsoft.UI.Xaml;
using WinRT;

namespace AutoOS.App.Views.Settings.Scheduling.ViewModels;

[GeneratedBindableCustomProperty]
public sealed partial class IrqPolicyItem
{
	public uint Value { get; set; }
	public string Name { get; set; } = string.Empty;
}

[GeneratedBindableCustomProperty]
public sealed partial class IrqPriorityItem
{
	public uint Value { get; set; }
	public string Name { get; set; } = string.Empty;
}

public partial class DeviceAffinityViewModel : INotifyPropertyChanged
{
	private readonly SchedulingItem _selectedItem;

	private bool _msiSupported;
	public bool MsiSupported
	{
		get => _msiSupported;
		set
		{
			if (SetProperty(ref _msiSupported, value))
				OnPropertyChanged(nameof(IsMsiLimitEnabled));
		}
	}

	private double _MsiLimit;
	public double MsiLimit
	{
		get => _MsiLimit;
		set => SetProperty(ref _MsiLimit, value);
	}

	public bool IsMsiLimitEnabled => MsiSupported;

	private int _devicePriority;
	public int DevicePriority
	{
		get => _devicePriority;
		set => SetProperty(ref _devicePriority, value);
	}

	private int _devicePolicy;
	public int DevicePolicy
	{
		get => _devicePolicy;
		set => SetProperty(ref _devicePolicy, value);
	}

	private ObservableCollection<CpuCoreGroup> _cpuGroups = [];
	public ObservableCollection<CpuCoreGroup> CpuGroups
	{
		get => _cpuGroups;
		set => SetProperty(ref _cpuGroups, value);
	}

	public GridLength Group0Width => GetGroupWidth(0);
	public GridLength Group1Width => GetGroupWidth(1);
	public GridLength Group2Width => GetGroupWidth(2);

	public CpuCoreGroup? Group0 => CpuGroups.Count > 0 ? CpuGroups[0] : null;
	public CpuCoreGroup? Group1 => CpuGroups.Count > 1 ? CpuGroups[1] : null;
	public CpuCoreGroup? Group2 => CpuGroups.Count > 2 ? CpuGroups[2] : null;

	public Visibility Group1Visibility => CpuGroups.Count > 1 ? Visibility.Visible : Visibility.Collapsed;
	public Visibility Group2Visibility => CpuGroups.Count > 2 ? Visibility.Visible : Visibility.Collapsed;

	public Thickness Group0Margin => new(0, 0, CpuGroups.Count == 2 ? 6 : 0, 0);
	public Thickness Group1Margin => new(CpuGroups.Count == 2 ? 6 : (CpuGroups.Count > 1 ? 12 : 0), 0, 0, 0);
	public Thickness Group2Margin => new(CpuGroups.Count > 2 ? 12 : 0, 0, 0, 0);

	private GridLength GetGroupWidth(int index)
	{
		if (CpuGroups.Count <= index)
			return new GridLength(0);
		return new GridLength(CpuGroups[index].RecommendedColumns, GridUnitType.Star);
	}

	private ulong _processMask;
	public ulong ProcessMask
	{
		get => _processMask;
		set => SetProperty(ref _processMask, value);
	}

	private uint _MaxMsiLimit;
	public uint MaxMsiLimit
	{
		get => _MaxMsiLimit;
		private set
		{
			if (SetProperty(ref _MaxMsiLimit, value))
				OnPropertyChanged(nameof(EffectiveMaxMsiLimit));
		}
	}

	public double EffectiveMaxMsiLimit => MaxMsiLimit > 0 ? MaxMsiLimit : 2048;

	public ObservableCollection<IrqPolicyItem> IrqPolicies { get; } = [];
	public ObservableCollection<IrqPriorityItem> IrqPriorities { get; } = [];

	public DeviceAffinityViewModel(SchedulingItem selectedItem, CpuSetsInfo cpuSetsInfo)
	{
		_selectedItem = selectedItem;
		InitializeIrqOptions();
		LoadCpuInformation(cpuSetsInfo);
		LoadCurrentSettings();
	}

	private void InitializeIrqOptions()
	{
		IrqPolicies.Add(new IrqPolicyItem { Value = 0, Name = "IrqPolicyMachineDefault" });
		IrqPolicies.Add(new IrqPolicyItem { Value = 1, Name = "IrqPolicyAllCloseProcessors" });
		IrqPolicies.Add(new IrqPolicyItem { Value = 2, Name = "IrqPolicyOneCloseProcessor" });
		IrqPolicies.Add(new IrqPolicyItem { Value = 3, Name = "IrqPolicyAllProcessorsInMachine" });
		IrqPolicies.Add(new IrqPolicyItem { Value = 4, Name = "IrqPolicySpecifiedProcessors" });
		IrqPolicies.Add(new IrqPolicyItem { Value = 5, Name = "IrqPolicySpreadMessagesAcrossAllProcessors" });

		IrqPriorities.Add(new IrqPriorityItem { Value = 0, Name = "Undefined" });
		IrqPriorities.Add(new IrqPriorityItem { Value = 1, Name = "Low" });
		IrqPriorities.Add(new IrqPriorityItem { Value = 2, Name = "Normal" });
		IrqPriorities.Add(new IrqPriorityItem { Value = 3, Name = "High" });
	}

	private void LoadCurrentSettings()
	{
		MsiSupported = _selectedItem.MsiSupported == 1u;
		MsiLimit = _selectedItem.MsiLimit;
		DevicePolicy = (int)_selectedItem.DevicePolicy;
		DevicePriority = (int)_selectedItem.DevicePriority;
		ProcessMask = _selectedItem.AssignmentSetOverride;
		MaxMsiLimit = _selectedItem.MaxMsiLimit;

		SetCpuSelectionFromMask(ProcessMask);
	}

	private void LoadCpuInformation(CpuSetsInfo cpuSetsInfo)
	{
		List<CpuCoreGroup> groups = CpuHelper.GroupCpuSetsSequentially(cpuSetsInfo);
		int maxColumns = groups.Count switch { 1 => 5, 2 => 4, _ => 3 };

		foreach (CpuCoreGroup group in groups)
			group.MaxColumns = maxColumns;

		if (groups.Count > 1)
		{
			int maxRows = groups.Max(g => (g.Cores.Count + g.RecommendedColumns - 1) / g.RecommendedColumns);
			foreach (CpuCoreGroup group in groups)
			{
				int targetCols = (group.Cores.Count + maxRows - 1) / maxRows;
				if (targetCols > 0 && targetCols < group.RecommendedColumns)
					group.FixedColumns = targetCols;
			}
		}

		CpuGroups = [with(groups)];
		OnPropertyChanged(nameof(Group0Width));
		OnPropertyChanged(nameof(Group1Width));
		OnPropertyChanged(nameof(Group2Width));
		OnPropertyChanged(nameof(Group0));
		OnPropertyChanged(nameof(Group1));
		OnPropertyChanged(nameof(Group2));
		OnPropertyChanged(nameof(Group1Visibility));
		OnPropertyChanged(nameof(Group2Visibility));
		OnPropertyChanged(nameof(Group0Margin));
		OnPropertyChanged(nameof(Group1Margin));
		OnPropertyChanged(nameof(Group2Margin));

		SetCpuSelectionFromMask(ProcessMask);

		foreach (CpuThread thread in CpuGroups.SelectMany(g => g.Cores).SelectMany(c => c.Threads))
			thread.PropertyChanged += Thread_PropertyChanged;
	}

	private void SetCpuSelectionFromMask(ulong mask)
	{
		foreach (CpuThread thread in CpuGroups.SelectMany(g => g.Cores).SelectMany(c => c.Threads))
			thread.IsSelected = (mask & thread.BitMask) != 0;
	}

	private void Thread_PropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName == nameof(CpuThread.IsSelected) && sender is CpuThread thread)
			ProcessMask = thread.IsSelected ? ProcessMask | thread.BitMask : ProcessMask & ~thread.BitMask;
	}

	public ApplyResult ApplySettings()
	{
		DeviceInfo? targetDevice = DeviceHelper.GetDevices(_selectedItem.DeviceType).FirstOrDefault(device => string.Equals(device.PnpDeviceId, _selectedItem.PnpDeviceId, StringComparison.OrdinalIgnoreCase));
		if (targetDevice == null)
			return new ApplyResult();

		return DeviceHelper.ApplySettingsToDevices(
			[targetDevice],
			MsiSupported,
			(uint)MsiLimit,
			(uint)DevicePolicy,
			(uint)DevicePriority,
			ProcessMask,
			_selectedItem.DeviceType
		);
	}
	public event PropertyChangedEventHandler? PropertyChanged;

	protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
	{
		if (Equals(field, value))
			return false;
		field = value;
		OnPropertyChanged(propertyName);
		return true;
	}
}
