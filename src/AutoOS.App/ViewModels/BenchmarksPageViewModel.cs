using System.Collections.ObjectModel;
using AutoOS.Core.Helpers.Benchmark;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Media;

namespace AutoOS.Views.Settings.Benchmarks;

public sealed partial class BenchmarksPageViewModel : ObservableObject
{
	private string _activeTab = "Recordings";
	private string _recordingState = "Empty";
	private string _analysisState = "Empty";
	private string _statisticsState = "Empty";
	private int _selectedRecordingCount;
	private bool _selectedRecordingsHaveSameProcess;
	private string _processName = string.Empty;
	private double _recordingDuration = 60;
	private double _recordingDelay = 5;
	private bool _isRecording;
	private int _recordingCountdown;
	private string _analysisChartType = "Bar";
	private readonly HashSet<string> _recordableProcesses = new(StringComparer.OrdinalIgnoreCase);
	private bool _selectedProcessIsRecordable;
	private ObservableCollection<string> _processSuggestions = [];
	private ObservableCollection<RecordingItem> _recordings = [];
	private ObservableCollection<ResultRow> _statisticsRows = [];
	private ObservableCollection<BarPoint> _barColumnChartDisplayedData1 = [];
	private ObservableCollection<BarPoint> _barColumnChartRenderedData1 = [];
	private ObservableCollection<BarPoint> _barColumnChartDisplayedData2 = [];
	private ObservableCollection<BarPoint> _barColumnChartRenderedData2 = [];
	private ObservableCollection<SeriesPoint> _lineScatterChartData1 = [];
	private ObservableCollection<SeriesPoint> _lineScatterChartData2 = [];
	private Windows.UI.Color _chartColor1 = Colors.DodgerBlue;
	private Windows.UI.Color _chartColor2 = Colors.Orange;
	private Windows.UI.Color _chartRenderedColor1;
	private Windows.UI.Color _chartRenderedColor2;
	private SolidColorBrush _chartColorBrush1;
	private SolidColorBrush _chartColorBrush2;
	private string _barColumnChartYAxisLabel = "FPS";
	private string _lineScatterChartYAxisLabel = "Milliseconds (ms)";
	private string _barColumnChartLabelFormat = "0.#";
	private bool _barColumnRenderedVisible;
	private string _barColumnChartDisplayedLabel1 = string.Empty;
	private string _barColumnChartRenderedLabel1 = string.Empty;
	private string _barColumnChartDisplayedLabel2 = string.Empty;
	private string _barColumnChartRenderedLabel2 = string.Empty;
	private string _lineScatterChartLabel1 = string.Empty;
	private string _lineScatterChartLabel2 = string.Empty;
	private string _recordingAHeader = "Recording A";
	private string _recordingBHeader = "Recording B";
	private string _deltaHeader = "Delta (%)";
	private string _analysisProcessText = string.Empty;

	public string ActiveTab
	{
		get => _activeTab;
		set => SetProperty(ref _activeTab, value);
	}

	public string RecordingState
	{
		get => _recordingState;
		private set => SetProperty(ref _recordingState, value);
	}

	public string AnalysisState
	{
		get => _analysisState;
		private set => SetProperty(ref _analysisState, value);
	}

	public string StatisticsState
	{
		get => _statisticsState;
		private set => SetProperty(ref _statisticsState, value);
	}

	public ObservableCollection<RecordingItem> Recordings
	{
		get => _recordings;
		private set => SetProperty(ref _recordings, value);
	}

	public ObservableCollection<ResultRow> StatisticsRows
	{
		get => _statisticsRows;
		set => SetProperty(ref _statisticsRows, value);
	}

	public ObservableCollection<string> ProcessSuggestions
	{
		get => _processSuggestions;
		private set => SetProperty(ref _processSuggestions, value);
	}

	public string ProcessName
	{
		get => _processName;
		set
		{
			if (SetProperty(ref _processName, value))
			{
				_selectedProcessIsRecordable =
					!string.IsNullOrWhiteSpace(value) &&
					_recordableProcesses.Contains(value.Trim());
				FilterProcessSuggestions(value);
				OnPropertyChanged(nameof(CanRecord));
			}
		}
	}

	public double RecordingDuration
	{
		get => _recordingDuration;
		set
		{
			if (SetProperty(ref _recordingDuration, value))
				OnPropertyChanged(nameof(CanRecord));
		}
	}

	public double RecordingDelay
	{
		get => _recordingDelay;
		set
		{
			if (SetProperty(ref _recordingDelay, value))
				OnPropertyChanged(nameof(CanRecord));
		}
	}

	public bool IsRecording
	{
		get => _isRecording;
		set
		{
			if (SetProperty(ref _isRecording, value))
			{
				OnPropertyChanged(nameof(CanRecord));
				OnPropertyChanged(nameof(RecordLabel));
			}
		}
	}

	public int RecordingCountdown
	{
		get => _recordingCountdown;
		private set => SetProperty(ref _recordingCountdown, value);
	}

	public event Action MetricToggled;

	private bool _showLow01 = true;
	private bool _showLow1 = true;
	private bool _showAvgArithmetic = true;
	private bool _showAvgHarmonic = true;
	private bool _showMin;
	private bool _showMax;
	private bool _showP01;
	private bool _showP1;
	private bool _showP5;
	private bool _showP50Median = true;
	private bool _showP95;
	private bool _showP99;

	public bool ShowLow01
	{
		get => _showLow01;
		set
		{
			if (SetProperty(ref _showLow01, value))
				MetricToggled?.Invoke();
		}
	}

	public bool ShowLow1
	{
		get => _showLow1;
		set
		{
			if (SetProperty(ref _showLow1, value))
				MetricToggled?.Invoke();
		}
	}

	public bool ShowAvgArithmetic
	{
		get => _showAvgArithmetic;
		set
		{
			if (SetProperty(ref _showAvgArithmetic, value))
				MetricToggled?.Invoke();
		}
	}

	public bool ShowAvgHarmonic
	{
		get => _showAvgHarmonic;
		set
		{
			if (SetProperty(ref _showAvgHarmonic, value))
				MetricToggled?.Invoke();
		}
	}

	public bool ShowMin
	{
		get => _showMin;
		set
		{
			if (SetProperty(ref _showMin, value))
				MetricToggled?.Invoke();
		}
	}

	public bool ShowMax
	{
		get => _showMax;
		set
		{
			if (SetProperty(ref _showMax, value))
				MetricToggled?.Invoke();
		}
	}

	public bool ShowP01
	{
		get => _showP01;
		set
		{
			if (SetProperty(ref _showP01, value))
				MetricToggled?.Invoke();
		}
	}

	public bool ShowP1
	{
		get => _showP1;
		set
		{
			if (SetProperty(ref _showP1, value))
				MetricToggled?.Invoke();
		}
	}

	public bool ShowP5
	{
		get => _showP5;
		set
		{
			if (SetProperty(ref _showP5, value))
				MetricToggled?.Invoke();
		}
	}

	public bool ShowP50Median
	{
		get => _showP50Median;
		set
		{
			if (SetProperty(ref _showP50Median, value))
				MetricToggled?.Invoke();
		}
	}

	public bool ShowP95
	{
		get => _showP95;
		set
		{
			if (SetProperty(ref _showP95, value))
				MetricToggled?.Invoke();
		}
	}

	public bool ShowP99
	{
		get => _showP99;
		set
		{
			if (SetProperty(ref _showP99, value))
				MetricToggled?.Invoke();
		}
	}

		public bool IsStatisticEnabled(string statistic) => statistic switch
	{
		"0.1% Low Avg" => ShowLow01,
		"1% Low Avg" => ShowLow1,
		"Avg (Arithmetic)" => ShowAvgArithmetic,
		"Avg (Harmonic)" => ShowAvgHarmonic,
		"Min" => ShowMin,
		"Max" => ShowMax,
		"P0.1" => ShowP01,
		"P1" => ShowP1,
		"P5" => ShowP5,
		"P50 (Median)" => ShowP50Median,
		"P95" => ShowP95,
		"P99" => ShowP99,
		_ => true
	};

	public string AnalysisChartType
	{
		get => _analysisChartType;
		set
		{
			if (SetProperty(ref _analysisChartType, value))
			{
				OnPropertyChanged(nameof(MetricVisibility));
				OnPropertyChanged(nameof(BarStatisticsVisibility));
			}
		}
	}

	public Visibility MetricVisibility => (AnalysisChartType is "Bar" or "Column") ? Visibility.Collapsed : Visibility.Visible;

	public Visibility BarStatisticsVisibility => (AnalysisChartType is "Bar" or "Column") ? Visibility.Visible : Visibility.Collapsed;

	public string GetStatisticTooltip(string key) => BenchmarkCsv.StatisticDescriptions.TryGetValue(key, out var desc) ? desc : string.Empty;

	public string GetMetricTooltip(string key) => BenchmarkCsv.MetricDescriptions.TryGetValue(key, out var desc) ? desc : string.Empty;

	public bool CanRecord => IsRecording || (!string.IsNullOrWhiteSpace(ProcessName) && _selectedProcessIsRecordable && RecordingDuration >= 3 && RecordingDelay >= 0);
	public string RecordLabel => IsRecording ? "Recording..." : "Record";
	public List<object> ShortcutKeys { get; } = ["Shift", "F11"];
	public bool IsAggregateEnabled => _selectedRecordingCount > 1 && _selectedRecordingsHaveSameProcess;
	public bool IsAnalysisToolbarEnabled => _selectedRecordingCount is > 0 and <= 2 && _selectedRecordingsHaveSameProcess;
	public bool CanCompareSelectedRecordings => _selectedRecordingCount == 2 && _selectedRecordingsHaveSameProcess;
	public bool IsSecondColorPickerEnabled => _selectedRecordingCount == 2;
	public bool IsRenameEnabled => _selectedRecordingCount > 0;
	public bool IsDeleteEnabled => _selectedRecordingCount > 0;
	public bool HasSecondRecording => _selectedRecordingCount == 2;
	public Visibility HasSecondRecordingVisibility => _selectedRecordingCount == 2 ? Visibility.Visible : Visibility.Collapsed;

	public void ShowRecordingCountdown(int seconds)
	{
		RecordingCountdown = seconds;
		RecordingState = "Countdown";
	}

	public void ShowRecording()
	{
		RecordingState = "Recording";
	}

	public void SetRecordableProcesses(IEnumerable<string> processNames)
	{
		_recordableProcesses.Clear();
		_recordableProcesses.UnionWith(processNames);
		_selectedProcessIsRecordable =
			!string.IsNullOrWhiteSpace(ProcessName) &&
			_recordableProcesses.Contains(ProcessName.Trim());
		FilterProcessSuggestions(ProcessName);
		OnPropertyChanged(nameof(CanRecord));
	}

	private void FilterProcessSuggestions(string query)
	{
		List<string> suggestions = [.. _recordableProcesses
			.Where(name => string.IsNullOrWhiteSpace(query) ||
				name.Contains(query.Trim(), StringComparison.OrdinalIgnoreCase))
			.OrderBy(name => name, StringComparer.OrdinalIgnoreCase)];

		if (ProcessSuggestions.SequenceEqual(suggestions, StringComparer.OrdinalIgnoreCase))
			return;

		ProcessSuggestions.Clear();
		foreach (string suggestion in suggestions)
			ProcessSuggestions.Add(suggestion);
	}

	public ObservableCollection<BarPoint> BarColumnChartDisplayedData1
	{
		get => _barColumnChartDisplayedData1;
		set => SetProperty(ref _barColumnChartDisplayedData1, value);
	}

	public ObservableCollection<BarPoint> BarColumnChartRenderedData1
	{
		get => _barColumnChartRenderedData1;
		set => SetProperty(ref _barColumnChartRenderedData1, value);
	}

	public ObservableCollection<BarPoint> BarColumnChartDisplayedData2
	{
		get => _barColumnChartDisplayedData2;
		set => SetProperty(ref _barColumnChartDisplayedData2, value);
	}

	public ObservableCollection<BarPoint> BarColumnChartRenderedData2
	{
		get => _barColumnChartRenderedData2;
		set => SetProperty(ref _barColumnChartRenderedData2, value);
	}

	public ObservableCollection<SeriesPoint> LineScatterChartData1
	{
		get => _lineScatterChartData1;
		set => SetProperty(ref _lineScatterChartData1, value);
	}

	public ObservableCollection<SeriesPoint> LineScatterChartData2
	{
		get => _lineScatterChartData2;
		set => SetProperty(ref _lineScatterChartData2, value);
	}

	public Windows.UI.Color ChartColor1
	{
		get => _chartColor1;
		set
		{
			if (!SetProperty(ref _chartColor1, value))
				return;
			ChartColorBrush1 = new SolidColorBrush(value);
			ChartRenderedColor1 = DevWinUI.ColorHelper.GetInterpolatedColor(0.35, value, Colors.White);
		}
	}

	public Windows.UI.Color ChartColor2
	{
		get => _chartColor2;
		set
		{
			if (!SetProperty(ref _chartColor2, value))
				return;
			ChartColorBrush2 = new SolidColorBrush(value);
			ChartRenderedColor2 = DevWinUI.ColorHelper.GetInterpolatedColor(0.35, value, Colors.White);
		}
	}

	public Windows.UI.Color ChartRenderedColor1
	{
		get => _chartRenderedColor1;
		private set => SetProperty(ref _chartRenderedColor1, value);
	}

	public SolidColorBrush ChartColorBrush1
	{
		get => _chartColorBrush1;
		private set => SetProperty(ref _chartColorBrush1, value);
	}

	public Windows.UI.Color ChartRenderedColor2
	{
		get => _chartRenderedColor2;
		private set => SetProperty(ref _chartRenderedColor2, value);
	}

	public SolidColorBrush ChartColorBrush2
	{
		get => _chartColorBrush2;
		private set => SetProperty(ref _chartColorBrush2, value);
	}

	public string BarColumnChartYAxisLabel
	{
		get => _barColumnChartYAxisLabel;
		set => SetProperty(ref _barColumnChartYAxisLabel, value);
	}

	public string LineScatterChartYAxisLabel
	{
		get => _lineScatterChartYAxisLabel;
		set => SetProperty(ref _lineScatterChartYAxisLabel, value);
	}

	public string BarColumnChartLabelFormat
	{
		get => _barColumnChartLabelFormat;
		set => SetProperty(ref _barColumnChartLabelFormat, value);
	}

	public bool BarColumnRenderedVisible
	{
		get => _barColumnRenderedVisible;
		set
		{
			if (SetProperty(ref _barColumnRenderedVisible, value))
				OnPropertyChanged(nameof(BarColumnRenderedVisibility));
		}
	}

	public Visibility BarColumnRenderedVisibility => BarColumnRenderedVisible ? Visibility.Visible : Visibility.Collapsed;

	public string BarColumnChartDisplayedLabel1
	{
		get => _barColumnChartDisplayedLabel1;
		set => SetProperty(ref _barColumnChartDisplayedLabel1, value);
	}

	public string BarColumnChartRenderedLabel1
	{
		get => _barColumnChartRenderedLabel1;
		set => SetProperty(ref _barColumnChartRenderedLabel1, value);
	}

	public string BarColumnChartDisplayedLabel2
	{
		get => _barColumnChartDisplayedLabel2;
		set => SetProperty(ref _barColumnChartDisplayedLabel2, value);
	}

	public string BarColumnChartRenderedLabel2
	{
		get => _barColumnChartRenderedLabel2;
		set => SetProperty(ref _barColumnChartRenderedLabel2, value);
	}

	public string LineScatterChartLabel1
	{
		get => _lineScatterChartLabel1;
		set => SetProperty(ref _lineScatterChartLabel1, value);
	}

	public string LineScatterChartLabel2
	{
		get => _lineScatterChartLabel2;
		set => SetProperty(ref _lineScatterChartLabel2, value);
	}

	public string RecordingAHeader
	{
		get => _recordingAHeader;
		set => SetProperty(ref _recordingAHeader, value);
	}

	public string RecordingBHeader
	{
		get => _recordingBHeader;
		set => SetProperty(ref _recordingBHeader, value);
	}

	public string DeltaHeader
	{
		get => _deltaHeader;
		set => SetProperty(ref _deltaHeader, value);
	}

	public string AnalysisProcessText
	{
		get => _analysisProcessText;
		private set => SetProperty(ref _analysisProcessText, value);
	}

	public void SetRecordings(IEnumerable<RecordingItem> recordings)
	{
		Recordings = new ObservableCollection<RecordingItem>(recordings);
		RecordingState = Recordings.Count == 0 ? "Empty" : "Content";
	}

	public void SetSelectedRecordings(IReadOnlyCollection<RecordingItem> recordings)
	{
		RecordingItem recordingA = recordings.FirstOrDefault();
		_selectedRecordingCount = recordings.Count;
		_selectedRecordingsHaveSameProcess = recordings.Count > 0 &&
			recordings.Select(recording => recording.Process)
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.Count() == 1;
		AnalysisProcessText = recordingA?.Process ?? string.Empty;

		AnalysisState = recordings.Count switch
		{
			0 => "Empty",
			> 2 => "Error",
			2 when !_selectedRecordingsHaveSameProcess => "ProcessMismatch",
			_ => "Content"
		};
		StatisticsState = AnalysisState;
		OnPropertyChanged(nameof(IsAggregateEnabled));
		OnPropertyChanged(nameof(IsAnalysisToolbarEnabled));
		OnPropertyChanged(nameof(CanCompareSelectedRecordings));
		OnPropertyChanged(nameof(IsSecondColorPickerEnabled));
		OnPropertyChanged(nameof(IsRenameEnabled));
		OnPropertyChanged(nameof(IsDeleteEnabled));
		OnPropertyChanged(nameof(HasSecondRecording));
		OnPropertyChanged(nameof(HasSecondRecordingVisibility));
	}

	public void ClearAnalysis()
	{
		BarColumnChartDisplayedData1 = null;
		BarColumnChartRenderedData1 = null;
		BarColumnChartDisplayedData2 = null;
		BarColumnChartRenderedData2 = null;
		LineScatterChartData1 = [];
		LineScatterChartData2 = [];
		BarColumnChartDisplayedLabel1 = string.Empty;
		BarColumnChartRenderedLabel1 = string.Empty;
		BarColumnChartDisplayedLabel2 = string.Empty;
		BarColumnChartRenderedLabel2 = string.Empty;
		LineScatterChartLabel1 = string.Empty;
		LineScatterChartLabel2 = string.Empty;
		BarColumnRenderedVisible = false;
	}

	public void RefreshChartColors()
	{
		ChartColorBrush1 = new SolidColorBrush(ChartColor1);
		ChartRenderedColor1 = DevWinUI.ColorHelper.GetInterpolatedColor(0.35, ChartColor1, Colors.White);
		ChartColorBrush2 = new SolidColorBrush(ChartColor2);
		ChartRenderedColor2 = DevWinUI.ColorHelper.GetInterpolatedColor(0.35, ChartColor2, Colors.White);
	}

}

[WinRT.GeneratedBindableCustomProperty]
public sealed partial class RecordingItem : ObservableObject
{
	private string _filePath = string.Empty;
	private string _fileName = string.Empty;
	private string _title = string.Empty;
	private string _process = string.Empty;
	private string _presentationMode = string.Empty;
	private double _durationSeconds;
	private DateTimeOffset _date;
	private TimeSpan _time;

	public ObservableCollection<RecordingItem> Children { get; } = [];

	public string FilePath
	{
		get => _filePath;
		set => SetProperty(ref _filePath, value);
	}

	public string FileName
	{
		get => _fileName;
		set => SetProperty(ref _fileName, value);
	}

	public string Title
	{
		get => _title;
		set => SetProperty(ref _title, value);
	}

	public string Process
	{
		get => _process;
		set => SetProperty(ref _process, value);
	}

	public string PresentationMode
	{
		get => _presentationMode;
		set => SetProperty(ref _presentationMode, value);
	}

	public double DurationSeconds
	{
		get => _durationSeconds;
		set => SetProperty(ref _durationSeconds, value);
	}

	public DateTimeOffset Date
	{
		get => _date;
		set => SetProperty(ref _date, value);
	}

	public TimeSpan Time
	{
		get => _time;
		set => SetProperty(ref _time, value);
	}
}

[WinRT.GeneratedBindableCustomProperty]
public sealed partial class ResultRow : ObservableObject
{
	private string _statistic = string.Empty;
	private string _tooltip = string.Empty;
	private string _recordingA = string.Empty;
	private string _recordingB = string.Empty;
	private string _delta = string.Empty;
	private ResultComparison _recordingAComparison;
	private ResultComparison _recordingBComparison;
	private ResultComparison _deltaComparison;

	public string Statistic
	{
		get => _statistic;
		set => SetProperty(ref _statistic, value);
	}

	public string Tooltip
	{
		get => _tooltip;
		set => SetProperty(ref _tooltip, value);
	}

	public string RecordingA
	{
		get => _recordingA;
		set => SetProperty(ref _recordingA, value);
	}

	public string RecordingB
	{
		get => _recordingB;
		set => SetProperty(ref _recordingB, value);
	}

	public string Delta
	{
		get => _delta;
		set => SetProperty(ref _delta, value);
	}

	public ResultComparison RecordingAComparison
	{
		get => _recordingAComparison;
		set => SetProperty(ref _recordingAComparison, value);
	}

	public ResultComparison RecordingBComparison
	{
		get => _recordingBComparison;
		set => SetProperty(ref _recordingBComparison, value);
	}

	public ResultComparison DeltaComparison
	{
		get => _deltaComparison;
		set => SetProperty(ref _deltaComparison, value);
	}

	public ObservableCollection<ResultRow> Children { get; } = [];
}

public enum ResultComparison
{
	None,
	Better,
	Worse
}

public sealed partial class ResultCellStyleSelector : StyleSelector
{
	public bool IsRecordingA { get; set; }
	public bool IsDelta { get; set; }
	public Style SuccessStyle { get; set; }
	public Style CriticalStyle { get; set; }

	protected override Style SelectStyleCore(object item, DependencyObject container)
	{
		if (item is not ResultRow row)
			return null;

		var comparison = IsDelta ? row.DeltaComparison : IsRecordingA ? row.RecordingAComparison : row.RecordingBComparison;
		return comparison switch
		{
			ResultComparison.Better => SuccessStyle,
			ResultComparison.Worse => CriticalStyle,
			_ => null
		};
	}
}

[WinRT.GeneratedBindableCustomProperty]
public sealed partial class BarPoint : ObservableObject
{
	private string _label = string.Empty;
	private double _value;

	public string Label
	{
		get => _label;
		set => SetProperty(ref _label, value);
	}

	public double Value
	{
		get => _value;
		set => SetProperty(ref _value, value);
	}
}

[WinRT.GeneratedBindableCustomProperty]
public sealed partial class SeriesPoint : ObservableObject
{
	private int _index;
	private double _value;

	public int Index
	{
		get => _index;
		set => SetProperty(ref _index, value);
	}

	public double Value
	{
		get => _value;
		set => SetProperty(ref _value, value);
	}
}

[System.Text.Json.Serialization.JsonSerializable(typeof(string[]))]
[System.Text.Json.Serialization.JsonSerializable(typeof(List<string>))]
internal sealed partial class BenchmarksJsonContext : System.Text.Json.Serialization.JsonSerializerContext
{
}
