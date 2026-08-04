using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using AutoOS.Core.Helpers.Power;

namespace AutoOS.App.Views.Settings.Power;

public sealed partial class PowerDialog : Page
{
	public PowerDialogState State { get; }

	public PowerDialog(PowerSetting setting)
	{
		InitializeComponent();
		State = new PowerDialogState(setting);
		DataContext = State;

		if (State.IsOption)
			LoadEnumValues(setting);
	}

	private void LoadEnumValues(PowerSetting setting)
	{
		uint index = 0;

		while (true)
		{
			string name = PowerHelper.ReadPossibleFriendlyName(setting.SubgroupGuid, setting.Guid, index);
			if (string.IsNullOrWhiteSpace(name))
				break;

			State.EnumValues.Add(new PowerSettingValueInfo
			{
				Index = index,
				FriendlyName = name,
				Description = PowerHelper.ReadPossibleDescription(setting.SubgroupGuid, setting.Guid, index)
			});

			index++;
		}

		State.OnPropertyChanged(nameof(State.AcValueItem));
		State.OnPropertyChanged(nameof(State.DcValueItem));
	}

	public uint GetAcValue() => State.AcValue;
	public uint GetDcValue() => State.DcValue;
}

public sealed partial class PowerDialogState : INotifyPropertyChanged
{
	private readonly uint _originalAc;
	private readonly uint _originalDc;

	public PowerSetting Setting { get; }

	public ObservableCollection<PowerSettingValueInfo> EnumValues { get; } = [];

	private uint _acValue;
	private uint _dcValue;

	public bool IsValue => !Setting.IsOption;

	public bool IsOption => Setting.IsOption;

	public uint AcValue
	{
		get => _acValue;
		set
		{
			if (_acValue != value)
			{
				_acValue = value;
				OnPropertyChanged();
				OnPropertyChanged(nameof(AcValueText));
				OnPropertyChanged(nameof(AcValueItem));
			}
		}
	}

	public uint DcValue
	{
		get => _dcValue;
		set
		{
			if (_dcValue != value)
			{
				_dcValue = value;
				OnPropertyChanged();
				OnPropertyChanged(nameof(DcValueText));
				OnPropertyChanged(nameof(DcValueItem));
			}
		}
	}

	public string AcValueText
	{
		get => _acValue.ToString();
		set
		{
			if (uint.TryParse(value, out uint parsedValue))
			{
				AcValue = parsedValue;
			}
		}
	}

	public string DcValueText
	{
		get => _dcValue.ToString();
		set
		{
			if (uint.TryParse(value, out uint parsedValue))
			{
				DcValue = parsedValue;
			}
		}
	}

	public PowerSettingValueInfo AcValueItem
	{
		get => EnumValues.FirstOrDefault(x => x.Index == _acValue);
		set
		{
			if (value != null && _acValue != value.Index)
			{
				AcValue = value.Index;
			}
		}
	}

	public PowerSettingValueInfo DcValueItem
	{
		get => EnumValues.FirstOrDefault(x => x.Index == _dcValue);
		set
		{
			if (value != null && _dcValue != value.Index)
			{
				DcValue = value.Index;
			}
		}
	}

	public string ValueToolTip
	{
		get
		{
			if (!IsValue)
				return null;

			return $"Range: {Setting.Min} - {Setting.Max}\n" + $"Increment: {Setting.Increment}\n" + $"Unit: {char.ToUpper(Setting.Unit[0]) + Setting.Unit[1..]}";
		}
	}

	public PowerDialogState(PowerSetting setting)
	{
		Setting = setting;
		_acValue = _originalAc = setting.AcValueIndex;
		_dcValue = _originalDc = setting.DcValueIndex;
		OnPropertyChanged(nameof(AcValue));
		OnPropertyChanged(nameof(DcValue));
		OnPropertyChanged(nameof(AcValueText));
		OnPropertyChanged(nameof(DcValueText));
		OnPropertyChanged(nameof(AcValueItem));
		OnPropertyChanged(nameof(DcValueItem));
	}

	public event PropertyChangedEventHandler PropertyChanged;
	internal void OnPropertyChanged([CallerMemberName] string name = null)
		=> PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
