using System.Collections;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AutoOS.Core.Helpers.BIOS;

public partial class BiosSettingModel : INotifyPropertyChanged, INotifyDataErrorInfo
{
	private bool _isLoaded = false;
	private string _value;
	private Option _selectedOption;
	private readonly Dictionary<string, List<string>> _warnings = [];
	private static bool _isBatchMode;

	public static bool IsBatchMode
	{
		get => _isBatchMode;
		set => _isBatchMode = value;
	}

	public event PropertyChangedEventHandler PropertyChanged;
	public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

	protected void OnPropertyChanged([CallerMemberName] string propertyName = null) =>
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

	private void RaiseErrorsChanged(string propertyName) =>
		ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));

	public void MarkLoaded()
	{
		_isLoaded = true;
		ValidateValue();
	}

	public int Line { get; set; }
	public List<string> OriginalLines { get; set; }
	public string OriginalValue { get; set; }
	public Option OriginalSelectedOption { get; set; }
	public string SetupQuestion { get; set; }
	public string HelpString { get; set; }
	public string Token { get; set; }
	public string Offset { get; set; }
	public string Width { get; set; }
	public string BiosDefault { get; set; }
	public bool IsRecommended { get; set; }
	public string RecommendedValue { get; set; }
	public Option RecommendedOption { get; set; }

	public string Value
	{
		get => _value;
		set
		{
			if (_value != value)
			{
				_value = value;
				OnPropertyChanged();

				ValidateValue();

				if (_isLoaded && !_isBatchMode)
				{
					RaiseModifiedChanged();
				}
			}
		}
	}

	public List<Option> Options { get; set; } = [];

	public Option SelectedOption
	{
		get => _selectedOption;
		set
		{
			if (_selectedOption != value)
			{
				_selectedOption = value;
				OnPropertyChanged();

				foreach (var opt in Options)
					opt.IsSelected = opt == value;

				ValidateValue();

				if (_isLoaded && !_isBatchMode)
					RaiseModifiedChanged();
			}
		}
	}
	
	public bool HasOptions => Options.Count > 0;
	public bool HasValueField => Value != null && !HasOptions;

	public void InitializeSelectedOption()
	{
		SelectedOption = Options.Find(o => o.IsSelected);
	}

	public bool IsModified => HasOptions
		? SelectedOption != OriginalSelectedOption
		: Value != OriginalValue;

	public event EventHandler ModifiedChanged;

	private void RaiseModifiedChanged()
	{
		ModifiedChanged?.Invoke(this, EventArgs.Empty);
	}

	public IEnumerable GetErrors(string propertyName)
	{
		if (string.IsNullOrEmpty(propertyName))
			return _warnings.Values.SelectMany(v => v);

		return _warnings.TryGetValue(propertyName, out var warnings) ? warnings : null;
	}

	public bool HasErrors => _warnings.Count > 0;

	private void ValidateValue()
	{
		const string propertyName = nameof(Value);

		if (!HasOptions)
		{
			if (string.IsNullOrWhiteSpace(_value))
			{
				_warnings[propertyName] = ["Value is empty"];
				RaiseErrorsChanged(propertyName);
			}
			else if (_warnings.ContainsKey(propertyName))
			{
				_warnings.Remove(propertyName);
				RaiseErrorsChanged(propertyName);
			}
		}

		if (HasOptions)
		{
			const string optionPropertyName = nameof(SelectedOption);
			if (_selectedOption == null)
			{
				_warnings[optionPropertyName] = ["No option selected"];
				RaiseErrorsChanged(optionPropertyName);
			}
			else if (_warnings.ContainsKey(optionPropertyName))
			{
				_warnings.Remove(optionPropertyName);
				RaiseErrorsChanged(optionPropertyName);
			}
		}
	}
}

public partial class Option : INotifyPropertyChanged
{
	private bool _isSelected;

	public event PropertyChangedEventHandler PropertyChanged;
	protected void OnPropertyChanged([CallerMemberName] string propertyName = null) =>
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

	public string Index { get; set; }
	public string Label { get; set; }

	public BiosSettingModel Parent { get; set; }

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
					foreach (var opt in Parent.Options)
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
