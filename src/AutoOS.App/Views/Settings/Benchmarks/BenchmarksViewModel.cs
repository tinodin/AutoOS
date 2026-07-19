using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Media;

namespace AutoOS.Views.Settings.Benchmarks;

public sealed partial class BenchmarksViewModel : ObservableObject
{
	private string _activeTab = "Recordings";
	private string _recordingState = "Empty";
	private string _analysisState = "Empty";
	private string _resultsState = "Empty";
	private int _selectedRecordingCount;
	private bool _selectedRecordingsHaveSameProcess;
	private string _processName = string.Empty;
	private double _recordingDuration = 30;
	private double _recordingDelay = 10;
	private bool _isRecording;
	private bool _showFpsAsMilliseconds;
	private readonly HashSet<string> _recordableProcesses = new(StringComparer.OrdinalIgnoreCase);
	private ObservableCollection<string> _processSuggestions = [];
	private ObservableCollection<RecordingItem> _recordings = [];
	private ObservableCollection<ResultRow> _resultsRows = [];
	private ObservableCollection<BarPoint> _fpsBarSeries = [];
	private ObservableCollection<BarPoint> _fpsBarSeries2 = [];
	private ObservableCollection<SeriesPoint> _metricLineSeries = [];
	private ObservableCollection<SeriesPoint> _metricLineSeries2 = [];
	private Windows.UI.Color _fpsColor;
	private Windows.UI.Color _fpsColor2;
	private SolidColorBrush _fpsChartColor;
	private SolidColorBrush _fpsChartColor2;
	private string _fpsChartYAxisLabel = "FPS";
	private string _metricChartYAxisLabel = "Milliseconds (ms)";
	private string _fpsChartLabelFormat = "0.# FPS";
	private bool _showFpsChart2;
	private bool _showMetricChart2;
	private string _fpsChartLabel = string.Empty;
	private string _fpsChartLabel2 = string.Empty;
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

	public string ResultsState
	{
		get => _resultsState;
		private set => SetProperty(ref _resultsState, value);
	}

	public ObservableCollection<RecordingItem> Recordings
	{
		get => _recordings;
		private set => SetProperty(ref _recordings, value);
	}

	public ObservableCollection<ResultRow> ResultsRows
	{
		get => _resultsRows;
		set => SetProperty(ref _resultsRows, value);
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

	public bool ShowFpsAsMilliseconds
	{
		get => _showFpsAsMilliseconds;
		set => SetProperty(ref _showFpsAsMilliseconds, value);
	}

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

	public ObservableCollection<BarPoint> FpsBarSeries2
	{
		get => _fpsBarSeries2;
		set => SetProperty(ref _fpsBarSeries2, value);
	}

	public ObservableCollection<SeriesPoint> MetricLineSeries
	{
		get => _metricLineSeries;
		set => SetProperty(ref _metricLineSeries, value);
	}

	public ObservableCollection<SeriesPoint> MetricLineSeries2
	{
		get => _metricLineSeries2;
		set => SetProperty(ref _metricLineSeries2, value);
	}

	public Windows.UI.Color FpsColor
	{
		get => _fpsColor;
		set
		{
			if (!SetProperty(ref _fpsColor, value))
				return;
			FpsChartColor = new SolidColorBrush(value);
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
		}
	}

	public SolidColorBrush FpsChartColor
	{
		get => _fpsChartColor;
		private set => SetProperty(ref _fpsChartColor, value);
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
		set => SetProperty(ref _showFpsChart2, value);
	}

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

	public string FpsChartLabel2
	{
		get => _fpsChartLabel2;
		set => SetProperty(ref _fpsChartLabel2, value);
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
		ResultsState = AnalysisState;
		OnPropertyChanged(nameof(IsAggregateEnabled));
		OnPropertyChanged(nameof(IsAnalysisToolbarEnabled));
		OnPropertyChanged(nameof(IsSecondColorPickerEnabled));
	}

	public void ClearAnalysis()
	{
		FpsBarSeries = [];
		FpsBarSeries2 = [];
		MetricLineSeries = [];
		MetricLineSeries2 = [];
		FpsChartLabel = string.Empty;
		FpsChartLabel2 = string.Empty;
		MetricChartLabel = string.Empty;
		MetricChartLabel2 = string.Empty;
		ShowFpsChart2 = false;
		ShowMetricChart2 = false;
	}

	public void RefreshChartColors()
	{
		FpsChartColor = new SolidColorBrush(FpsColor);
		FpsChartColor2 = new SolidColorBrush(FpsColor2);
	}
}

public sealed partial class RecordingItem : ObservableObject
{
	private string _filePath = string.Empty;
	private string _fileName = string.Empty;
	private string _title = string.Empty;
	private string _process = string.Empty;
	private double _durationSeconds;
	private DateTimeOffset _date;
	private TimeSpan _time;
	private double _fileSizeKb;

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

public sealed partial class ResultRow : ObservableObject
{
	private string _metric = string.Empty;
	private string _recordingA = string.Empty;
	private string _recordingB = string.Empty;
	private ResultComparison _recordingAComparison;
	private ResultComparison _recordingBComparison;

	public string Metric
	{
		get => _metric;
		set => SetProperty(ref _metric, value);
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
