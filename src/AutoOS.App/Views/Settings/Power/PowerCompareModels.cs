using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.UI.Text;
using WinRT;

namespace AutoOS.App.Views.Settings.Power;

[GeneratedBindableCustomProperty]
public abstract partial class PowerCompareModelItem : INotifyPropertyChanged
{
	public Guid Guid { get; set; }
	public string Name { get; set; }

	private bool _isVisible = true;
	public bool IsVisible
	{
		get => _isVisible;
		set { if (_isVisible != value) { _isVisible = value; OnPropertyChanged(); } }
	}

	public virtual bool IsExpanded { get; set; }
	public virtual Windows.UI.Text.FontWeight FontWeight { get; set; }
	public virtual ObservableCollection<PowerCompareSetting> Settings { get; set; }

	public virtual string Description { get; set; }
	public virtual bool IsAcDifferent { get; set; }
	public virtual bool IsDcDifferent { get; set; }
	public virtual uint Plan1AcValue { get; set; }
	public virtual uint Plan1DcValue { get; set; }
	public virtual uint Plan2AcValue { get; set; }
	public virtual uint Plan2DcValue { get; set; }
	public virtual string Plan1AcFriendlyValue { get; set; }
	public virtual string Plan1DcFriendlyValue { get; set; }
	public virtual string Plan2AcFriendlyValue { get; set; }
	public virtual string Plan2DcFriendlyValue { get; set; }

	public event PropertyChangedEventHandler PropertyChanged;
	protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
		=> PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

[GeneratedBindableCustomProperty]
public sealed partial class PowerCompareSubgroup : PowerCompareModelItem
{
	public override Windows.UI.Text.FontWeight FontWeight { get; set; } = FontWeights.SemiBold;
	public override ObservableCollection<PowerCompareSetting> Settings { get; set; } = [];

	private bool _isExpanded = true;
	public override bool IsExpanded
	{
		get => _isExpanded;
		set { if (_isExpanded != value) { _isExpanded = value; OnPropertyChanged(); } }
	}
}

[GeneratedBindableCustomProperty]
public sealed partial class PowerCompareSetting : PowerCompareModelItem
{
	public Guid SubgroupGuid { get; set; }

	public override string Description { get; set; }

	public uint? Min { get; set; }
	public uint? Max { get; set; }
	public uint? Increment { get; set; }
	public string Unit { get; set; }

	public bool IsOption => !(Min.HasValue && Max.HasValue && Increment.HasValue && Max.Value > Min.Value && Increment.Value > 0);

	private uint _plan1AcValue;
	public override uint Plan1AcValue
	{
		get => _plan1AcValue;
		set { if (_plan1AcValue != value) { _plan1AcValue = value; OnPropertyChanged(); } }
	}

	private uint _plan1DcValue;
	public override uint Plan1DcValue
	{
		get => _plan1DcValue;
		set { if (_plan1DcValue != value) { _plan1DcValue = value; OnPropertyChanged(); } }
	}

	private uint _plan2AcValue;
	public override uint Plan2AcValue
	{
		get => _plan2AcValue;
		set { if (_plan2AcValue != value) { _plan2AcValue = value; OnPropertyChanged(); } }
	}

	private uint _plan2DcValue;
	public override uint Plan2DcValue
	{
		get => _plan2DcValue;
		set { if (_plan2DcValue != value) { _plan2DcValue = value; OnPropertyChanged(); } }
	}

	private string _plan1AcFriendlyValue;
	public override string Plan1AcFriendlyValue
	{
		get => _plan1AcFriendlyValue;
		set { if (_plan1AcFriendlyValue != value) { _plan1AcFriendlyValue = value; OnPropertyChanged(); } }
	}

	private string _plan1DcFriendlyValue;
	public override string Plan1DcFriendlyValue
	{
		get => _plan1DcFriendlyValue;
		set { if (_plan1DcFriendlyValue != value) { _plan1DcFriendlyValue = value; OnPropertyChanged(); } }
	}

	private string _plan2AcFriendlyValue;
	public override string Plan2AcFriendlyValue
	{
		get => _plan2AcFriendlyValue;
		set { if (_plan2AcFriendlyValue != value) { _plan2AcFriendlyValue = value; OnPropertyChanged(); } }
	}

	private string _plan2DcFriendlyValue;
	public override string Plan2DcFriendlyValue
	{
		get => _plan2DcFriendlyValue;
		set { if (_plan2DcFriendlyValue != value) { _plan2DcFriendlyValue = value; OnPropertyChanged(); } }
	}

	private bool _isAcDifferent = false;
	public override bool IsAcDifferent
	{
		get => _isAcDifferent;
		set
		{
			if (_isAcDifferent != value)
			{
				_isAcDifferent = value;
				OnPropertyChanged();
			}
		}
	}

	private bool _isDcDifferent = false;
	public override bool IsDcDifferent
	{
		get => _isDcDifferent;
		set
		{
			if (_isDcDifferent != value)
			{
				_isDcDifferent = value;
				OnPropertyChanged();
			}
		}
	}
}

public sealed partial class PowerCompareItemTemplateSelector : DataTemplateSelector
{
	public DataTemplate SubgroupTemplate { get; set; }
	public DataTemplate SettingTemplate { get; set; }

	protected override DataTemplate SelectTemplateCore(object item)
		=> item switch
		{
			PowerCompareSubgroup => SubgroupTemplate,
			PowerCompareSetting => SettingTemplate,
			_ => base.SelectTemplateCore(item)
		};
}


