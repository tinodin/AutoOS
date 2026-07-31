using System.Collections.ObjectModel;
using AutoOS.Core.Helpers.Benchmark;
using AutoOS.Views.Settings;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Media;
using nietras.SeparatedValues;
using Syncfusion.UI.Xaml.TreeGrid;

namespace AutoOS.ViewModels;

public sealed partial class BenchmarksPageViewModel : ObservableObject
{
	[ObservableProperty]
	public partial string ActiveTab { get; set; } = "Recordings";

	[ObservableProperty]
	public partial string RecordingState { get; set; } = "Content";

	[ObservableProperty]
	public partial string AnalysisState { get; set; } = "Empty";

	[ObservableProperty]
	public partial string StatisticsState { get; set; } = "Empty";

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(IsAggregateEnabled))]
	[NotifyPropertyChangedFor(nameof(IsAnalysisToolbarEnabled))]
	[NotifyPropertyChangedFor(nameof(HasTwoRecordings))]
	[NotifyPropertyChangedFor(nameof(HasTwoRecordingsVisibility))]
	[NotifyPropertyChangedFor(nameof(IsDeleteEnabled))]
	[NotifyCanExecuteChangedFor(nameof(AggregateCommand))]
	public partial int SelectedRecordingCount { get; set; }

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(IsAggregateEnabled))]
	[NotifyPropertyChangedFor(nameof(IsAnalysisToolbarEnabled))]
	[NotifyPropertyChangedFor(nameof(HasTwoRecordings))]
	[NotifyPropertyChangedFor(nameof(HasTwoRecordingsVisibility))]
	[NotifyCanExecuteChangedFor(nameof(AggregateCommand))]
	public partial bool SelectedRecordingsHaveSameProcess { get; set; }

	public IReadOnlyList<RecordingItem> SelectedRecordings { get; set; } = new List<RecordingItem>();
	public List<RecordingAnalysis> CachedAnalysis { get; set; } = [];
	private readonly HashSet<string> _recordableProcesses = new(StringComparer.OrdinalIgnoreCase);

	[ObservableProperty]
	public partial bool IsRenameEnabled { get; set; }

	public bool IsDeleteEnabled => SelectedRecordingCount > 0;

	[RelayCommand(CanExecute = nameof(IsAggregateEnabled))]
	private void Aggregate()
	{
		var selected = SelectedRecordings;
		string processName = selected[0].Process;

		int aggregateNumber = 1;
		string outPath;
		do
		{
			outPath = Path.Combine(BenchmarkCsv.RecordingsDirectory, $"Aggregate-{aggregateNumber++}.csv");
		}
		while (File.Exists(outPath));

		List<string> headerCols;
		using (var headerReader = Sep.Reader(options => options with { Sep = new Sep(','), Unescape = true }).FromFile(selected[0].FilePath))
		{
			headerCols = new List<string>(headerReader.Header.ColNames.Count);
			for (int i = 0; i < headerReader.Header.ColNames.Count; i++)
				headerCols.Add(headerReader.Header.ColNames[i]);
		}

		int applicationIndex = BenchmarkCsv.EnsureColumn(headerCols, "Application");
		int aggregateDurationIndex = BenchmarkCsv.EnsureColumn(headerCols, "AggregateDurationSeconds");
		int aggregateSourcesIndex = BenchmarkCsv.EnsureColumn(headerCols, "AggregateSources");
		int columnCount = headerCols.Count;

		List<double[]> sums = [];
		List<int[]> counts = [];

		List<string[]> fallbackRows = [];

		for (int fileIndex = 0; fileIndex < selected.Count; fileIndex++)
		{
			using var reader = Sep.Reader(options => options with { Sep = new Sep(','), Unescape = true }).FromFile(selected[fileIndex].FilePath);
			if (reader.Header.IsEmpty)
				continue;

			bool isFallbackFile = fileIndex == 0;
			int rowIndex = 0;

			while (reader.MoveNext())
			{
				var row = reader.Current;

				if (rowIndex == sums.Count)
				{
					sums.Add(new double[columnCount]);
					counts.Add(new int[columnCount]);
				}

				double[] rowSums = sums[rowIndex];
				int[] rowCounts = counts[rowIndex];

				string[] rawRow = isFallbackFile ? new string[row.ColCount] : null;
				int colLimit = Math.Min(row.ColCount, columnCount);

				for (int column = 0; column < colLimit; column++)
				{
					if (row[column].TryParse(out double value))
					{
						rowSums[column] += value;
						rowCounts[column]++;
					}

					if (isFallbackFile)
						rawRow[column] = row[column].ToString();
				}

				if (isFallbackFile)
				{
					for (int column = colLimit; column < row.ColCount; column++)
						rawRow[column] = row[column].ToString();
					fallbackRows.Add(rawRow);
				}

				rowIndex++;
			}
		}

		int maxRows = sums.Count;
		double meanDurationSeconds = selected.Average(recording => recording.DurationSeconds);
		string aggregateSources = string.Join("|", selected.Select(recording => recording.FileName).Distinct(StringComparer.OrdinalIgnoreCase));

		using var writer = Sep.Writer(options => options with { Sep = new Sep(',') }).ToFile(outPath);
		foreach (var col in headerCols)
			writer.Header.Add(col);

		for (int r = 0; r < maxRows; r++)
		{
			using var row = writer.NewRow();
			double[] rowSums = sums[r];
			int[] rowCounts = counts[r];
			string[] fallbackRow = r < fallbackRows.Count ? fallbackRows[r] : null;

			for (int column = 0; column < columnCount; column++)
			{
				if (column == applicationIndex)
				{
					row[headerCols[column]].Set(processName);
					continue;
				}
				if (column == aggregateDurationIndex)
				{
					row[headerCols[column]].Format(meanDurationSeconds);
					continue;
				}
				if (column == aggregateSourcesIndex)
				{
					row[headerCols[column]].Set(r == 0 ? aggregateSources : string.Empty);
					continue;
				}

				if (rowCounts[column] > 0)
					row[headerCols[column]].Format(rowSums[column] / rowCounts[column]);
				else if (fallbackRow != null && column < fallbackRow.Length)
					row[headerCols[column]].Set(fallbackRow[column]);
				else
					row[headerCols[column]].Set(string.Empty);
			}
		}

		var aggregateRecording = new RecordingItem
		{
			FilePath = outPath,
			FileName = Path.GetFileName(outPath),
			Title = Path.GetFileNameWithoutExtension(outPath),
			Process = processName,
			PresentationMode = string.Empty,
			DurationSeconds = meanDurationSeconds,
			Date = DateTimeOffset.Now,
			Time = DateTimeOffset.Now.TimeOfDay
		};

		HashSet<RecordingItem> childSet = [.. selected.Where(recording => recording.FilePath != outPath)];
		foreach (var child in childSet)
			aggregateRecording.Children.Add(child);

		List<RecordingItem> updatedList = [aggregateRecording, .. Recordings.Where(recording => !childSet.Contains(recording))];
		updatedList.Sort((a, b) => b.Date.CompareTo(a.Date));
		SetRecordings(updatedList);
		SetSelectedRecordings([]);
	}

	[ObservableProperty]
	public partial ObservableCollection<string> ProcessSuggestions { get; set; } = [];

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(CanRecord))]
	public partial string ProcessName { get; set; } = string.Empty;

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(CanRecord))]
	public partial double RecordingDelay { get; set; } = 5;

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(CanRecord))]
	public partial double RecordingDuration { get; set; } = 60;

	public List<object> ShortcutKeys { get; } = ["Ctrl", "Shift", "R"];

	public bool CanRecord => IsRecording || (!string.IsNullOrWhiteSpace(ProcessName));
	public string RecordLabel => IsRecording ? "Cancel" : "Record";
	public string RecordIconGlyph => IsRecording ? "\uE711" : "\uE7C8";

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(CanRecord))]
	[NotifyPropertyChangedFor(nameof(RecordLabel))]
	[NotifyPropertyChangedFor(nameof(RecordIconGlyph))]
	public partial bool IsRecording { get; set; }

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(CountdownProgress))]
	[NotifyPropertyChangedFor(nameof(CountdownSecondsLeft))]
	public partial double RecordingCountdown { get; set; }

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(RecordingProgress))]
	[NotifyPropertyChangedFor(nameof(RecordingSecondsLeft))]
	public partial double RecordingRemaining { get; set; }

	public double RecordingProgress => (RecordingRemaining / RecordingDuration) * 100;
	public double CountdownProgress => (RecordingCountdown / RecordingDelay) * 100;
	public int CountdownSecondsLeft => (int)Math.Ceiling(RecordingCountdown);
	public int RecordingSecondsLeft => (int)Math.Ceiling(RecordingRemaining);

	[ObservableProperty]
	public partial ObservableCollection<RecordingItem> Recordings { get; set; } = [];

	public bool IsAnalysisToolbarEnabled => SelectedRecordingCount is > 0 and <= 2 && SelectedRecordingsHaveSameProcess;

	public Visibility BarStatisticsVisibility => (AnalysisChartType is "Bar" or "Column") ? Visibility.Visible : Visibility.Collapsed;

	public Visibility MetricVisibility => (AnalysisChartType is "Bar" or "Column") ? Visibility.Collapsed : Visibility.Visible;

	[ObservableProperty]
	public partial bool ShowLow01 { get; set; } = true;

	[ObservableProperty]
	public partial bool ShowLow1 { get; set; } = true;

	[ObservableProperty]
	public partial bool ShowAvgArithmetic { get; set; } = true;

	[ObservableProperty]
	public partial bool ShowAvgHarmonic { get; set; } = true;

	[ObservableProperty]
	public partial bool ShowMin { get; set; }

	[ObservableProperty]
	public partial bool ShowMax { get; set; }

	[ObservableProperty]
	public partial bool ShowP01 { get; set; }

	[ObservableProperty]
	public partial bool ShowP1 { get; set; }

	[ObservableProperty]
	public partial bool ShowP5 { get; set; }

	[ObservableProperty]
	public partial bool ShowP50Median { get; set; } = true;

	[ObservableProperty]
	public partial bool ShowP95 { get; set; }

	[ObservableProperty]
	public partial bool ShowP99 { get; set; }

	public event Action StatisticToggled;

	partial void OnShowLow01Changed(bool value) => StatisticToggled?.Invoke();
	partial void OnShowLow1Changed(bool value) => StatisticToggled?.Invoke();
	partial void OnShowAvgArithmeticChanged(bool value) => StatisticToggled?.Invoke();
	partial void OnShowAvgHarmonicChanged(bool value) => StatisticToggled?.Invoke();
	partial void OnShowMinChanged(bool value) => StatisticToggled?.Invoke();
	partial void OnShowMaxChanged(bool value) => StatisticToggled?.Invoke();
	partial void OnShowP01Changed(bool value) => StatisticToggled?.Invoke();
	partial void OnShowP1Changed(bool value) => StatisticToggled?.Invoke();
	partial void OnShowP5Changed(bool value) => StatisticToggled?.Invoke();
	partial void OnShowP50MedianChanged(bool value) => StatisticToggled?.Invoke();
	partial void OnShowP95Changed(bool value) => StatisticToggled?.Invoke();
	partial void OnShowP99Changed(bool value) => StatisticToggled?.Invoke();

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

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(RecordingAColorBrush))]
	[NotifyPropertyChangedFor(nameof(RecordingASecondaryColor))]
	public partial Windows.UI.Color RecordingAColor { get; set; }

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(RecordingBColorBrush))]
	[NotifyPropertyChangedFor(nameof(RecordingBSecondaryColor))]
	public partial Windows.UI.Color RecordingBColor { get; set; }

	[ObservableProperty]
	public partial Windows.UI.Color RecordingASecondaryColor { get; set; }

	[ObservableProperty]
	public partial Windows.UI.Color RecordingBSecondaryColor { get; set; }

	[ObservableProperty]
	public partial SolidColorBrush RecordingAColorBrush { get; set; }

	[ObservableProperty]
	public partial SolidColorBrush RecordingBColorBrush { get; set; }

	public bool HasTwoRecordings => SelectedRecordingCount == 2 && SelectedRecordingsHaveSameProcess;
	public Visibility HasTwoRecordingsVisibility => HasTwoRecordings ? Visibility.Visible : Visibility.Collapsed;

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(MetricVisibility))]
	[NotifyPropertyChangedFor(nameof(BarStatisticsVisibility))]
	public partial string AnalysisChartType { get; set; } = "Bar";

	[ObservableProperty]
	public partial string AnalysisProcess { get; set; }

	[ObservableProperty]
	public partial ObservableCollection<BarPoint> BarColumnChartDisplayedData1 { get; set; } = [];

	[ObservableProperty]
	public partial ObservableCollection<BarPoint> BarColumnChartRenderedData1 { get; set; } = [];

	[ObservableProperty]
	public partial ObservableCollection<BarPoint> BarColumnChartDisplayedData2 { get; set; } = [];

	[ObservableProperty]
	public partial ObservableCollection<BarPoint> BarColumnChartRenderedData2 { get; set; } = [];

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(BarColumnRenderedVisibility))]
	public partial bool BarColumnRenderedVisible { get; set; }

	public Visibility BarColumnRenderedVisibility => BarColumnRenderedVisible ? Visibility.Visible : Visibility.Collapsed;

	[ObservableProperty]
	public partial string BarColumnChartDisplayedLabel1 { get; set; } = string.Empty;

	[ObservableProperty]
	public partial string BarColumnChartRenderedLabel1 { get; set; } = string.Empty;

	[ObservableProperty]
	public partial string BarColumnChartDisplayedLabel2 { get; set; } = string.Empty;

	[ObservableProperty]
	public partial string BarColumnChartRenderedLabel2 { get; set; } = string.Empty;

	[ObservableProperty]
	public partial ObservableCollection<SeriesPoint> LineScatterChartData1 { get; set; } = [];

	[ObservableProperty]
	public partial ObservableCollection<SeriesPoint> LineScatterChartData2 { get; set; } = [];

	[ObservableProperty]
	public partial string LineScatterChartLabel1 { get; set; } = string.Empty;

	[ObservableProperty]
	public partial string LineScatterChartLabel2 { get; set; } = string.Empty;

	public string GetStatisticTooltip(string key) => BenchmarkCsv.StatisticDescriptions.TryGetValue(key, out var desc) ? desc : string.Empty;

	public string GetMetricTooltip(string key) => BenchmarkCsv.MetricDescriptions.TryGetValue(key, out var desc) ? desc : string.Empty;

	[ObservableProperty]
	public partial ObservableCollection<string> BaselineItems { get; set; } = ["None"];

	[ObservableProperty]
	public partial int BaselineSelectedIndex { get; set; }

	[ObservableProperty]
	public partial bool IsDeltaModeEnabled { get; set; }

	[ObservableProperty]
	public partial bool IsPercentDelta { get; set; }

	[ObservableProperty]
	public partial ObservableCollection<ResultRow> StatisticsRows { get; set; } = [];

	[ObservableProperty]
	public partial string RecordingAHeader { get; set; } = "Recording A";

	[ObservableProperty]
	public partial string RecordingBHeader { get; set; } = "Recording B";

	[ObservableProperty]
	public partial string DeltaHeader { get; set; }

	public bool IsAggregateEnabled => SelectedRecordingCount > 1 && SelectedRecordingsHaveSameProcess;

	public BenchmarksPageViewModel()
	{
		RecordingAColor = Colors.DodgerBlue;
		RecordingBColor = Colors.Orange;
	}

	partial void OnRecordingAColorChanged(Windows.UI.Color value)
	{
		RecordingAColorBrush = new SolidColorBrush(value);
		RecordingASecondaryColor = DevWinUI.ColorHelper.GetInterpolatedColor(0.35, value, Colors.White);
	}

	partial void OnRecordingBColorChanged(Windows.UI.Color value)
	{
		RecordingBColorBrush = new SolidColorBrush(value);
		RecordingBSecondaryColor = DevWinUI.ColorHelper.GetInterpolatedColor(0.35, value, Colors.White);
	}

	public void ShowRecordingCountdown(int seconds)
	{
		RecordingCountdown = seconds;
		RecordingState = "Countdown";
	}

	public void ShowRecording()
	{
		RecordingState = "Recording";
		RecordingRemaining = RecordingDuration;
	}

	public void SetRecordableProcesses(IEnumerable<string> processNames)
	{
		_recordableProcesses.Clear();
		_recordableProcesses.UnionWith(processNames);
		FilterProcessSuggestions(string.Empty);
	}

	private void FilterProcessSuggestions(string query)
	{
		var suggestions = _recordableProcesses
			.Where(name => string.IsNullOrWhiteSpace(query) || name.Contains(query.Trim(), StringComparison.OrdinalIgnoreCase))
			.OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
			.ToList();

		if (ProcessSuggestions.SequenceEqual(suggestions, StringComparer.OrdinalIgnoreCase))
			return;

		ProcessSuggestions = new ObservableCollection<string>(suggestions);
	}

	public void SetRecordings(IReadOnlyList<RecordingItem> recordings)
	{
		Recordings = new ObservableCollection<RecordingItem>(recordings);
		RecordingState = recordings.Count == 0 ? "Empty" : "Content";
	}

	public void SetSelectedRecordings(IReadOnlyList<RecordingItem> recordings)
	{
		SelectedRecordings = recordings;
		int count = recordings.Count;
		RecordingItem recordingA = count > 0 ? recordings[0] : null;
		SelectedRecordingCount = count;
		IsRenameEnabled = count > 0;

		bool sameProcess = count > 0;
		if (sameProcess)
		{
			string firstProcess = recordingA.Process;
			for (int i = 1; i < count; i++)
			{
				if (!string.Equals(recordings[i].Process, firstProcess, StringComparison.OrdinalIgnoreCase))
				{
					sameProcess = false;
					break;
				}
			}
		}
		SelectedRecordingsHaveSameProcess = sameProcess;
		IsDeltaModeEnabled = false;
		AnalysisChartType = "Bar";
		BaselineItems = new ObservableCollection<string>(["None", .. recordings.Select(recording => recording.Title)]);
		BaselineSelectedIndex = 0;

		AnalysisProcess = recordingA?.Process ?? string.Empty;

		AnalysisState = count switch
		{
			0 => "Empty",
			> 2 => "Error",
			2 when !sameProcess => "ProcessMismatch",
			_ => "Content"
		};

		StatisticsState = AnalysisState;
	}
}

[WinRT.GeneratedBindableCustomProperty]
public sealed partial class RecordingItem : ObservableObject
{
	[ObservableProperty]
	public partial string FilePath { get; set; } = string.Empty;

	[ObservableProperty]
	public partial string FileName { get; set; } = string.Empty;

	[ObservableProperty]
	public partial string Title { get; set; } = string.Empty;

	[ObservableProperty]
	public partial string Process { get; set; } = string.Empty;

	[ObservableProperty]
	public partial string PresentationMode { get; set; } = string.Empty;

	[ObservableProperty]
	public partial double DurationSeconds { get; set; }

	[ObservableProperty]
	public partial DateTimeOffset Date { get; set; }

	[ObservableProperty]
	public partial TimeSpan Time { get; set; }

	public ObservableCollection<RecordingItem> Children { get; } = [];
}

[WinRT.GeneratedBindableCustomProperty]
public sealed partial class ResultRow : ObservableObject
{
	[ObservableProperty]
	public partial string Statistic { get; set; } = string.Empty;

	[ObservableProperty]
	public partial string Tooltip { get; set; } = string.Empty;

	[ObservableProperty]
	public partial string RecordingA { get; set; } = string.Empty;

	[ObservableProperty]
	public partial string RecordingB { get; set; } = string.Empty;

	[ObservableProperty]
	public partial string Delta { get; set; } = string.Empty;

	[ObservableProperty]
	public partial string RecordingAComparison { get; set; }

	[ObservableProperty]
	public partial string RecordingBComparison { get; set; }

	[ObservableProperty]
	public partial string DeltaComparison { get; set; }

	public ObservableCollection<ResultRow> Children { get; } = [];
}

public sealed partial class ResultCellStyleSelector : StyleSelector
{
	public Style SuccessStyle { get; set; }
	public Style CriticalStyle { get; set; }

	protected override Style SelectStyleCore(object item, DependencyObject container)
	{
		if (item is not ResultRow row || container is not TreeGridCell cell)
			return null;
		var value = cell.ColumnBase?.TreeGridColumn.MappingName switch
		{
			nameof(ResultRow.RecordingA) => row.RecordingAComparison,
			nameof(ResultRow.RecordingB) => row.RecordingBComparison,
			nameof(ResultRow.Delta) => row.DeltaComparison,
			_ => null
		};
		return value == "Better" ? SuccessStyle : value == "Worse" ? CriticalStyle : null;
	}
}

[WinRT.GeneratedBindableCustomProperty]
public sealed partial class BarPoint : ObservableObject
{
	[ObservableProperty]
	public partial string Label { get; set; } = string.Empty;

	[ObservableProperty]
	public partial double Value { get; set; }
}

[WinRT.GeneratedBindableCustomProperty]
public sealed partial class SeriesPoint : ObservableObject
{
	[ObservableProperty]
	public partial int Index { get; set; }

	[ObservableProperty]
	public partial double Value { get; set; }
}
