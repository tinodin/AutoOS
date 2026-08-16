using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using AutoOS.Core.Helpers.CPU;
using AutoOS.Core.Data.Models.CPU;
using AutoOS.Core.Helpers.Device;
using AutoOS.Core.Data.Models.Device;
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
		set
		{
			if (SetProperty(ref _devicePolicy, value))
				OnPropertyChanged(nameof(IsCoreSelectionEnabled));
		}
	}

	public bool IsCoreSelectionEnabled => DevicePolicy == 4;

	private ObservableCollection<CpuCoreGroup> _cpuGroups = [];
	public ObservableCollection<CpuCoreGroup> CpuGroups
	{
		get => _cpuGroups;
		set => SetProperty(ref _cpuGroups, value);
	}

	private string _groupColumnDefinitions = "1*";
	public string GroupColumnDefinitions
	{
		get => _groupColumnDefinitions;
		set => SetProperty(ref _groupColumnDefinitions, value);
	}

	private int _totalColumns = 1;
	public int TotalColumns
	{
		get => _totalColumns;
		set => SetProperty(ref _totalColumns, value);
	}

	public GridLength Group0Width => GetGroupWidth(0);
	public GridLength Group1Width => GetGroupWidth(1);
	public GridLength Group2Width => GetGroupWidth(2);

	public CpuCoreGroup? Group0 => CpuGroups.Count > 0 ? CpuGroups[0] : null;
	public CpuCoreGroup? Group1 => CpuGroups.Count > 1 ? CpuGroups[1] : null;
	public CpuCoreGroup? Group2 => CpuGroups.Count > 2 ? CpuGroups[2] : null;

	public int Group0Columns => CpuGroups.Count > 0 ? CpuGroups[0].RecommendedColumns : 1;
	public int Group1Columns => CpuGroups.Count > 1 ? CpuGroups[1].RecommendedColumns : 1;
	public int Group2Columns => CpuGroups.Count > 2 ? CpuGroups[2].RecommendedColumns : 1;

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

	public double Group1Spacing => CpuGroups.Count > 1 ? 12 : 0;
	public double Group2Spacing => CpuGroups.Count > 2 ? 12 : 0;

	private ulong _processMask;
	public ulong ProcessMask
	{
		get => _processMask;
		set => SetProperty(ref _processMask, value);
	}

	public bool HasEfficiencyClass { get; private set; }

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

	public GridLength ECoreColumnWidth => HasEfficiencyClass ? new GridLength(1, GridUnitType.Star) : GridLength.Auto;
	public double ColumnSpacing => HasEfficiencyClass ? 12 : 0;

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

		for (int i = 0; i < groups.Count; i++)
			groups[i].ColumnIndex = i;

		GroupColumnDefinitions = string.Join(", ", groups.Select(g => $"{g.RecommendedColumns}*"));
		TotalColumns = groups.Sum(g => g.RecommendedColumns);

		CpuGroups = [with(groups)];
		OnPropertyChanged(nameof(Group0Width));
		OnPropertyChanged(nameof(Group1Width));
		OnPropertyChanged(nameof(Group2Width));
		OnPropertyChanged(nameof(Group0));
		OnPropertyChanged(nameof(Group1));
		OnPropertyChanged(nameof(Group2));
		OnPropertyChanged(nameof(Group0Columns));
		OnPropertyChanged(nameof(Group1Columns));
		OnPropertyChanged(nameof(Group2Columns));
		OnPropertyChanged(nameof(Group1Visibility));
		OnPropertyChanged(nameof(Group2Visibility));
		OnPropertyChanged(nameof(Group0Margin));
		OnPropertyChanged(nameof(Group1Margin));
		OnPropertyChanged(nameof(Group2Margin));

		HasEfficiencyClass = cpuSetsInfo.EfficiencyClass && CpuGroups.Any(g => g.Name == "E-Cores");

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

	public void ApplySettings()
	{
		DeviceInfo? targetDevice = DeviceHelper.GetDevices(_selectedItem.DeviceType).FirstOrDefault(device => string.Equals(device.PnpDeviceId, _selectedItem.PnpDeviceId, StringComparison.OrdinalIgnoreCase));

		ApplyResult result = DeviceHelper.ApplySettingsToDevices(
			[targetDevice!],
			MsiSupported,
			(uint)MsiLimit,
			(uint)DevicePolicy,
			(uint)DevicePriority,
			ProcessMask,
			_selectedItem.DeviceType
		);

		OnSettingsApplied?.Invoke(result);
	}

	internal event Action<ApplyResult>? OnSettingsApplied;
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
