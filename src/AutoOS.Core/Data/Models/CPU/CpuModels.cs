using System.ComponentModel;
using System.Runtime.CompilerServices;
using WinRT;

namespace AutoOS.Core.Data.Models.CPU;

public enum CpuVendor
{
	Unknown,
	Intel,
	AMD
}

public sealed class CpuArchitecture
{
	public CpuVendor Vendor { get; set; }
	public uint Family { get; set; }
	public uint Model { get; set; }
	public uint Stepping { get; set; }
	public uint DisplayFamily { get; set; }
	public uint DisplayModel { get; set; }
	public string DisplayName { get; set; } = string.Empty;
	public string ArchitectureName { get; set; } = string.Empty;
}

public sealed class CpuSet
{
	public uint Id { get; set; }
	public byte CoreIndex { get; set; }
	public byte LogicalProcessorIndex { get; set; }
	public byte EfficiencyClass { get; set; }
	public byte LastLevelCacheIndex { get; set; }
	public byte NumaNodeIndex { get; set; }
}

[GeneratedBindableCustomProperty]
public sealed partial class CpuThread : INotifyPropertyChanged
{
	private bool _isSelected;
	public uint CpuId { get; set; }
	public string Name { get; set; } = string.Empty;
	public ulong BitMask { get; set; }

	public bool IsSelected
	{
		get => _isSelected;
		set
		{
			if (_isSelected == value) return;
			_isSelected = value;
			OnPropertyChanged();
		}
	}

public event PropertyChangedEventHandler? PropertyChanged;
	private void OnPropertyChanged([CallerMemberName] string? name = null)
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
	}
}

[GeneratedBindableCustomProperty]
public sealed partial class CpuCore
{
	public byte CoreIndex { get; set; }
	public string Name { get; set; } = string.Empty;
	public List<CpuThread> Threads { get; set; } = [];
}

[GeneratedBindableCustomProperty]
public sealed partial class CpuCoreGroup : INotifyPropertyChanged
{
	public event PropertyChangedEventHandler? PropertyChanged;
	private void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
	public string Name { get; set; } = string.Empty;
	public List<CpuCore> Cores { get; set; } = [];

	private int _maxColumns = 5;
	public int MaxColumns
	{
		get => _maxColumns;
		set { if (_maxColumns != value) { _maxColumns = value; OnPropertyChanged(nameof(RecommendedColumns)); } }
	}

	private int? _fixedColumns;
	public int? FixedColumns
	{
		get => _fixedColumns;
		set { if (_fixedColumns != value) { _fixedColumns = value; OnPropertyChanged(nameof(RecommendedColumns)); } }
	}

	public int ColumnIndex { get; set; }

	public int RecommendedColumns
	{
		get
		{
			if (FixedColumns.HasValue) return FixedColumns.Value;

			int count = Cores.Count;
			if (count == 0) return 1;

			int cols;
			if (count % 5 == 0) cols = 5;
			else if (count % 4 == 0) cols = 4;
			else if (count % 3 == 0) cols = 3;
			else if (count % 2 == 0) cols = 2;
			else cols = 3;

			return Math.Min(cols, MaxColumns);
		}
	}
}

public sealed class CpuSetsInfo
{
	public bool HyperThreading { get; set; }
	public int CoreCount { get; set; }
	public int MaxThreadsPerCore { get; set; }
	public bool NumaNode { get; set; }
	public bool LastLevelCache { get; set; }
	public bool EfficiencyClass { get; set; }
	public List<CpuSet> CpuSets { get; set; } = [];
}
