using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Media;

namespace AutoOS.Views.Settings.Benchmarks;

public sealed partial class BenchmarksViewModel : ObservableObject
{
	private string _activeTab = "Recordings";
	private string _recordingState = "Empty";
	private string _analysisState = "Empty";
	private string _statisticsState = "Empty";
	private int _selectedRecordingCount;
	private bool _selectedRecordingsHaveSameProcess;
	private string _processName = string.Empty;
	private double _recordingDuration = 30;
	private double _recordingDelay = 5;
	private bool _isRecording;
	private string _analysisChartType = "Bar";
	private readonly HashSet<string> _recordableProcesses = new(StringComparer.OrdinalIgnoreCase);
	private ObservableCollection<string> _processSuggestions = [];
	private ObservableCollection<RecordingItem> _recordings = [];
	private ObservableCollection<ResultRow> _statisticsRows = [];
	private ObservableCollection<BarPoint> _fpsBarSeries = [];
	private ObservableCollection<BarPoint> _fpsRenderedBarSeries = [];
	private ObservableCollection<BarPoint> _fpsBarSeries2 = [];
	private ObservableCollection<BarPoint> _fpsRenderedBarSeries2 = [];
	private ObservableCollection<SeriesPoint> _metricSeries = [];
	private ObservableCollection<SeriesPoint> _metricSeries2 = [];
	private Windows.UI.Color _fpsColor;
	private Windows.UI.Color _fpsColor2;
	private Windows.UI.Color _fpsRenderedColor;
	private Windows.UI.Color _fpsRenderedColor2;
	private SolidColorBrush _fpsChartColor;
	private SolidColorBrush _fpsChartColor2;
	private string _fpsChartYAxisLabel = "FPS";
	private string _metricChartYAxisLabel = "Milliseconds (ms)";
	private string _fpsChartLabelFormat = "0.#";
	private bool _showFpsChart2;
	private bool _showRenderedFps;
	private bool _showRenderedFpsChart2;
	private bool _showMetricChart2;
	private string _fpsChartLabel = string.Empty;
	private string _fpsRenderedChartLabel = string.Empty;
	private string _fpsChartLabel2 = string.Empty;
	private string _fpsRenderedChartLabel2 = string.Empty;
	private string _metricChartLabel = string.Empty;
	private string _metricChartLabel2 = string.Empty;
	private string _recordingAHeader = "Recording A";
	private string _recordingBHeader = "Recording B";

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

	public bool IsMetricEnabled(string metric) => metric switch
	{
		"0.1% Low" => ShowLow01,
		"1% Low" => ShowLow1,
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
				OnPropertyChanged(nameof(BarMetricsVisibility));
			}
		}
	}

	public Visibility MetricVisibility =>
		(AnalysisChartType is "Bar" or "Column") ? Visibility.Collapsed : Visibility.Visible;

	public Visibility BarMetricsVisibility =>
		(AnalysisChartType is "Bar" or "Column") ? Visibility.Visible : Visibility.Collapsed;

	public bool CanRecord =>
		IsRecording ||
		(!string.IsNullOrWhiteSpace(ProcessName) &&
			_recordableProcesses.Contains(ProcessName.Trim()) &&
			RecordingDuration >= 3 &&
			RecordingDelay >= 0);

	public string RecordLabel => IsRecording ? "Recording..." : "Record";
	public List<object> ShortcutKeys { get; } = ["Shift", "F11"];
	public bool IsAggregateEnabled => _selectedRecordingCount > 1 && _selectedRecordingsHaveSameProcess;
	public bool IsAnalysisToolbarEnabled => _selectedRecordingCount is > 0 and <= 2;
	public bool IsSecondColorPickerEnabled => _selectedRecordingCount == 2;

	public void SetRecordableProcesses(IEnumerable<string> processNames)
	{
		bool selectedProcessWasRecordable =
			!string.IsNullOrWhiteSpace(ProcessName) &&
			_recordableProcesses.Contains(ProcessName.Trim());
		_recordableProcesses.Clear();
		_recordableProcesses.UnionWith(processNames);
		if (selectedProcessWasRecordable && !_recordableProcesses.Contains(ProcessName.Trim()))
		{
			ProcessName = string.Empty;
			return;
		}
		FilterProcessSuggestions(ProcessName);
		OnPropertyChanged(nameof(CanRecord));
	}

	private void FilterProcessSuggestions(string query)
	{
		ProcessSuggestions = [.. _recordableProcesses
			.Where(name => string.IsNullOrWhiteSpace(query) ||
				name.Contains(query.Trim(), StringComparison.OrdinalIgnoreCase))
			.OrderBy(name => name, StringComparer.OrdinalIgnoreCase)];
	}

	public ObservableCollection<BarPoint> FpsBarSeries
	{
		get => _fpsBarSeries;
		set => SetProperty(ref _fpsBarSeries, value);
	}

	public ObservableCollection<BarPoint> FpsRenderedBarSeries
	{
		get => _fpsRenderedBarSeries;
		set => SetProperty(ref _fpsRenderedBarSeries, value);
	}

	public ObservableCollection<BarPoint> FpsBarSeries2
	{
		get => _fpsBarSeries2;
		set => SetProperty(ref _fpsBarSeries2, value);
	}

	public ObservableCollection<BarPoint> FpsRenderedBarSeries2
	{
		get => _fpsRenderedBarSeries2;
		set => SetProperty(ref _fpsRenderedBarSeries2, value);
	}

	public ObservableCollection<SeriesPoint> MetricSeries
	{
		get => _metricSeries;
		set => SetProperty(ref _metricSeries, value);
	}

	public ObservableCollection<SeriesPoint> MetricSeries2
	{
		get => _metricSeries2;
		set => SetProperty(ref _metricSeries2, value);
	}

	public Windows.UI.Color FpsColor
	{
		get => _fpsColor;
		set
		{
			if (!SetProperty(ref _fpsColor, value))
				return;
			FpsChartColor = new SolidColorBrush(value);
			FpsRenderedColor = DevWinUI.ColorHelper.GetInterpolatedColor(0.35, value, Colors.White);
		}
	}

	public Windows.UI.Color FpsColor2
	{
		get => _fpsColor2;
		set
		{
			if (!SetProperty(ref _fpsColor2, value))
				return;
			FpsChartColor2 = new SolidColorBrush(value);
			FpsRenderedColor2 = DevWinUI.ColorHelper.GetInterpolatedColor(0.35, value, Colors.White);
		}
	}

	public Windows.UI.Color FpsRenderedColor
	{
		get => _fpsRenderedColor;
		private set => SetProperty(ref _fpsRenderedColor, value);
	}

	public SolidColorBrush FpsChartColor
	{
		get => _fpsChartColor;
		private set => SetProperty(ref _fpsChartColor, value);
	}

	public Windows.UI.Color FpsRenderedColor2
	{
		get => _fpsRenderedColor2;
		private set => SetProperty(ref _fpsRenderedColor2, value);
	}

	public SolidColorBrush FpsChartColor2
	{
		get => _fpsChartColor2;
		private set => SetProperty(ref _fpsChartColor2, value);
	}

	public string FpsChartYAxisLabel
	{
		get => _fpsChartYAxisLabel;
		set => SetProperty(ref _fpsChartYAxisLabel, value);
	}

	public string MetricChartYAxisLabel
	{
		get => _metricChartYAxisLabel;
		set => SetProperty(ref _metricChartYAxisLabel, value);
	}

	public string FpsChartLabelFormat
	{
		get => _fpsChartLabelFormat;
		set => SetProperty(ref _fpsChartLabelFormat, value);
	}

	public bool ShowFpsChart2
	{
		get => _showFpsChart2;
		set
		{
			if (SetProperty(ref _showFpsChart2, value))
				OnPropertyChanged(nameof(ShowFpsChart2Visibility));
		}
	}

	public bool ShowRenderedFps
	{
		get => _showRenderedFps;
		set
		{
			if (SetProperty(ref _showRenderedFps, value))
				OnPropertyChanged(nameof(ShowRenderedFpsVisibility));
		}
	}

	public bool ShowRenderedFpsChart2
	{
		get => _showRenderedFpsChart2;
		set
		{
			if (SetProperty(ref _showRenderedFpsChart2, value))
				OnPropertyChanged(nameof(ShowRenderedFpsChart2Visibility));
		}
	}

	public Visibility ShowRenderedFpsVisibility => ShowRenderedFps ? Visibility.Visible : Visibility.Collapsed;
	public Visibility ShowFpsChart2Visibility => ShowFpsChart2 ? Visibility.Visible : Visibility.Collapsed;
	public Visibility ShowRenderedFpsChart2Visibility => ShowRenderedFpsChart2 ? Visibility.Visible : Visibility.Collapsed;
	public Visibility SecondColorPickerVisibility => _selectedRecordingCount == 2 ? Visibility.Visible : Visibility.Collapsed;

	public bool ShowMetricChart2
	{
		get => _showMetricChart2;
		set => SetProperty(ref _showMetricChart2, value);
	}

	public string FpsChartLabel
	{
		get => _fpsChartLabel;
		set => SetProperty(ref _fpsChartLabel, value);
	}

	public string FpsRenderedChartLabel
	{
		get => _fpsRenderedChartLabel;
		set => SetProperty(ref _fpsRenderedChartLabel, value);
	}

	public string FpsChartLabel2
	{
		get => _fpsChartLabel2;
		set => SetProperty(ref _fpsChartLabel2, value);
	}

	public string FpsRenderedChartLabel2
	{
		get => _fpsRenderedChartLabel2;
		set => SetProperty(ref _fpsRenderedChartLabel2, value);
	}

	public string MetricChartLabel
	{
		get => _metricChartLabel;
		set => SetProperty(ref _metricChartLabel, value);
	}

	public string MetricChartLabel2
	{
		get => _metricChartLabel2;
		set => SetProperty(ref _metricChartLabel2, value);
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

	public void SetRecordings(IEnumerable<RecordingItem> recordings)
	{
		Recordings = new ObservableCollection<RecordingItem>(recordings);
		RecordingState = Recordings.Count == 0 ? "Empty" : "Content";
	}

	public void SetSelectedRecordings(IReadOnlyCollection<RecordingItem> recordings)
	{
		_selectedRecordingCount = recordings.Count;
		_selectedRecordingsHaveSameProcess = recordings.Count > 0 &&
			recordings.Select(recording => recording.Process)
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.Count() == 1;

		AnalysisState = recordings.Count switch
		{
			0 => "Empty",
			> 2 => "Error",
			_ => "Content"
		};
		StatisticsState = AnalysisState;
		OnPropertyChanged(nameof(IsAggregateEnabled));
		OnPropertyChanged(nameof(IsAnalysisToolbarEnabled));
		OnPropertyChanged(nameof(IsSecondColorPickerEnabled));
		OnPropertyChanged(nameof(SecondColorPickerVisibility));
	}

	public void ClearAnalysis()
	{
		FpsBarSeries = null;
		FpsRenderedBarSeries = null;
		FpsBarSeries2 = null;
		FpsRenderedBarSeries2 = null;
		MetricSeries = [];
		MetricSeries2 = [];
		FpsChartLabel = string.Empty;
		FpsRenderedChartLabel = string.Empty;
		FpsChartLabel2 = string.Empty;
		FpsRenderedChartLabel2 = string.Empty;
		MetricChartLabel = string.Empty;
		MetricChartLabel2 = string.Empty;
		ShowFpsChart2 = false;
		ShowRenderedFps = false;
		ShowRenderedFpsChart2 = false;
		ShowMetricChart2 = false;
	}

	public void RefreshChartColors()
	{
		FpsChartColor = new SolidColorBrush(FpsColor);
		FpsRenderedColor = DevWinUI.ColorHelper.GetInterpolatedColor(0.35, FpsColor, Colors.White);
		FpsChartColor2 = new SolidColorBrush(FpsColor2);
		FpsRenderedColor2 = DevWinUI.ColorHelper.GetInterpolatedColor(0.35, FpsColor2, Colors.White);
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
	private double _fileSizeKb;

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

	public double FileSizeKb
	{
		get => _fileSizeKb;
		set => SetProperty(ref _fileSizeKb, value);
	}
}

[WinRT.GeneratedBindableCustomProperty]
public sealed partial class ResultRow : ObservableObject
{
	private string _metric = string.Empty;
	private string _tooltip = string.Empty;
	private string _recordingA = string.Empty;
	private string _recordingB = string.Empty;
	private ResultComparison _recordingAComparison;
	private ResultComparison _recordingBComparison;

	public string Metric
	{
		get => _metric;
		set => SetProperty(ref _metric, value);
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
	public Style SuccessStyle { get; set; }
	public Style CriticalStyle { get; set; }

	protected override Style SelectStyleCore(object item, DependencyObject container)
	{
		if (item is not ResultRow row)
			return null;

		var comparison = IsRecordingA ? row.RecordingAComparison : row.RecordingBComparison;
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

