using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.UI.Text;
using WinRT;

namespace AutoOS.App.Views.Settings.Power;

[GeneratedBindableCustomProperty]
public abstract partial class PowerModelItem : INotifyPropertyChanged
{
	public Guid Guid { get; set; }

	private string _name;
	public string Name
	{
		get => _name;
		set { if (_name != value) { _name = value; OnPropertyChanged(); } }
	}

	private string _description;
	public string Description
	{
		get => _description;
		set { if (_description != value) { _description = value; OnPropertyChanged(); } }
	}

	private bool _isVisible = true;
	public bool IsVisible
	{
		get => _isVisible;
		set { if (_isVisible != value) { _isVisible = value; OnPropertyChanged(); } }
	}

	public virtual bool IsExpanded { get; set; }
	public virtual Windows.UI.Text.FontWeight FontWeight { get; set; }
	public virtual ObservableCollection<PowerSetting> Settings { get; set; }

	public virtual uint AcValueIndex { get; set; }
	public virtual uint DcValueIndex { get; set; }
	public virtual string FriendlyAcValue { get; set; }
	public virtual string FriendlyDcValue { get; set; }

	public event PropertyChangedEventHandler PropertyChanged;
	protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
		=> PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

[GeneratedBindableCustomProperty]
public sealed partial class PowerPlan : PowerModelItem
{
}

[GeneratedBindableCustomProperty]
public sealed partial class PowerSubgroup : PowerModelItem
{
	public override Windows.UI.Text.FontWeight FontWeight { get; set; } = FontWeights.SemiBold;
	public override ObservableCollection<PowerSetting> Settings { get; set; } = [];

	private bool _isExpanded = true;
	public override bool IsExpanded
	{
		get => _isExpanded;
		set { if (_isExpanded != value) { _isExpanded = value; OnPropertyChanged(); } }
	}
}

[GeneratedBindableCustomProperty]
public sealed partial class PowerSetting : PowerModelItem
{
	private uint _acValueIndex;
	private uint _dcValueIndex;

	public Guid SubgroupGuid { get; set; }

	private string _friendlyAcValue;
	private string _friendlyDcValue;

	public override string FriendlyAcValue
	{
		get => _friendlyAcValue;
		set
		{
			if (_friendlyAcValue != value)
			{
				_friendlyAcValue = value;
				OnPropertyChanged();
			}
		}
	}

	public override string FriendlyDcValue
	{
		get => _friendlyDcValue;
		set
		{
			if (_friendlyDcValue != value)
			{
				_friendlyDcValue = value;
				OnPropertyChanged();
			}
		}
	}

	public override uint AcValueIndex
	{
		get => _acValueIndex;
		set
		{
			if (_acValueIndex != value)
			{
				_acValueIndex = value;
				OnPropertyChanged();
			}
		}
	}

	public override uint DcValueIndex
	{
		get => _dcValueIndex;
		set
		{
			if (_dcValueIndex != value)
			{
				_dcValueIndex = value;
				OnPropertyChanged();
			}
		}
	}

	public uint? Min { get; set; }
	public uint? Max { get; set; }
	public uint? Increment { get; set; }
	public string Unit { get; set; }

	public bool IsOption => !(Min.HasValue && Max.HasValue && Increment.HasValue && Max.Value > Min.Value && Increment.Value > 0);
}

public sealed class PowerSettingValueInfo
{
	public uint Index { get; set; }
	public string FriendlyName { get; set; }
	public string Description { get; set; }
}

public sealed partial class PowerItemTemplateSelector : DataTemplateSelector
{
	public DataTemplate SubgroupTemplate { get; set; }
	public DataTemplate SettingTemplate { get; set; }

	protected override DataTemplate SelectTemplateCore(object item)
		=> item switch
		{
			PowerSubgroup => SubgroupTemplate,
			PowerSetting => SettingTemplate,
			_ => base.SelectTemplateCore(item)
		};
}
