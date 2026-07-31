using System.Collections.ObjectModel;
using System.Globalization;
using AutoOS.Core.Helpers.Benchmark;
using AutoOS.Core.Helpers.Picker;
using AutoOS.Views.Settings;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Media;
using nietras.SeparatedValues;
using Syncfusion.UI.Xaml.TreeGrid;
using Windows.Storage;
using Windows.System;

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
	[NotifyPropertyChangedFor(nameof(IsDeleteEnabled))]
	[NotifyPropertyChangedFor(nameof(IsAggregateEnabled))]
	[NotifyPropertyChangedFor(nameof(IsAnalysisToolbarEnabled))]
	[NotifyPropertyChangedFor(nameof(HasTwoRecordingsVisibility))]
	[NotifyPropertyChangedFor(nameof(HasTwoRecordings))]
	[NotifyPropertyChangedFor(nameof(PieChartColumnSpan))]
	[NotifyCanExecuteChangedFor(nameof(DeleteCommand))]
	[NotifyCanExecuteChangedFor(nameof(AggregateCommand))]
	public partial int SelectedRecordingCount { get; set; }

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(IsAggregateEnabled))]
	[NotifyPropertyChangedFor(nameof(IsAnalysisToolbarEnabled))]
	[NotifyPropertyChangedFor(nameof(HasTwoRecordingsVisibility))]
	[NotifyPropertyChangedFor(nameof(HasTwoRecordings))]
	[NotifyPropertyChangedFor(nameof(PieChartColumnSpan))]
	[NotifyCanExecuteChangedFor(nameof(AggregateCommand))]
	public partial bool SelectedRecordingsHaveSameProcess { get; set; }

	public IReadOnlyList<RecordingItem> SelectedRecordings { get; set; } = new List<RecordingItem>();
	public List<RecordingAnalysis> CachedAnalysis { get; set; } = [];
	private readonly HashSet<string> _recordableProcesses = new(StringComparer.OrdinalIgnoreCase);
	private readonly ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
	
	public async Task LoadRecordingsAsync()
	{
		List<RecordingItem> finalRecordings = await Task.Run(() =>
		{
			if (!Directory.Exists(BenchmarkCsv.RecordingsDirectory))
			{
				Directory.CreateDirectory(BenchmarkCsv.RecordingsDirectory);
				return [];
			}

			List<FileInfo> csvFiles = [.. new DirectoryInfo(BenchmarkCsv.RecordingsDirectory).EnumerateFiles("*.csv")];

			if (csvFiles.Count == 0)
			{
				return [];
			}

			var sepReader = Sep.Reader(options => options with { Sep = new Sep(','), Unescape = true, ColNameComparer = StringComparer.OrdinalIgnoreCase });

			List<RecordingItem> recordings = new(csvFiles.Count);
			Dictionary<RecordingItem, List<string>> aggregateSources = new();

			var loadedRecordings = csvFiles
				.AsParallel()
				.Select(info =>
				{
					try
					{
						double durationSeconds = Math.Max(0, (info.LastWriteTime - info.CreationTime).TotalSeconds);
						string nameWithoutExtension = Path.GetFileNameWithoutExtension(info.Name);

						RecordingItem result = new()
						{
							FilePath = info.FullName,
							FileName = info.Name,
							Title = nameWithoutExtension,
							Process = nameWithoutExtension,
							PresentationMode = string.Empty,
							DurationSeconds = durationSeconds,
							Date = info.LastWriteTime,
							Time = info.LastWriteTime.TimeOfDay
						};

						List<string> sourceFileNames = [];

						using var reader = sepReader.FromFile(info.FullName);

						reader.Header.TryIndexOf("Application", out int appIdx);
						reader.Header.TryIndexOf("PresentMode", out int presentModeIdx);
						reader.Header.TryIndexOf("AggregateDurationSeconds", out int aggDurationIdx);
						bool hasAggSources = reader.Header.TryIndexOf("AggregateSources", out int aggSourcesIdx);
						reader.Header.TryIndexOf("TimeInDateTime", out int dateTimeIdx);
						reader.Header.TryIndexOf("TimeInSeconds", out int timeSecondsIdx);

						if (!reader.MoveNext())
							return (Recording: result, SourceFileNames: sourceFileNames);

						var firstRow = reader.Current;

						if (appIdx >= 0)
						{
							string application = firstRow[appIdx].ToString();
							if (!string.IsNullOrWhiteSpace(application))
								result.Process = application;
						}
						if (presentModeIdx >= 0)
						{
							string presentMode = firstRow[presentModeIdx].ToString();
							if (!string.IsNullOrWhiteSpace(presentMode))
								result.PresentationMode = presentMode;
						}

						bool hasCsvDuration = false;
						if (aggDurationIdx >= 0 && firstRow[aggDurationIdx].TryParse(out double aggregateDuration))
						{
							result.DurationSeconds = Math.Max(0, aggregateDuration);
							hasCsvDuration = true;
						}

						if (hasAggSources && aggSourcesIdx >= 0)
						{
							var sourceText = firstRow[aggSourcesIdx].ToString();
							if (!string.IsNullOrWhiteSpace(sourceText))
								sourceFileNames = [.. sourceText.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)];
						}

						if (!hasCsvDuration && (dateTimeIdx >= 0 || timeSecondsIdx >= 0))
						{
							string firstDateTimeStr = dateTimeIdx >= 0 ? firstRow[dateTimeIdx].ToString() : null;
							string firstTimeSecondsStr = timeSecondsIdx >= 0 ? firstRow[timeSecondsIdx].ToString() : null;

							string lastLine = BenchmarkCsv.ReadLastLine(info.FullName, info.Length);
							ReadOnlySpan<char> lastLineSpan = lastLine;

							if (dateTimeIdx >= 0 && firstDateTimeStr != null)
							{
								var lastDateTimeSpan = BenchmarkCsv.GetField(lastLineSpan, dateTimeIdx);
								if (!lastDateTimeSpan.IsEmpty &&
									DateTime.TryParse(firstDateTimeStr, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var start) &&
									DateTime.TryParse(lastDateTimeSpan, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var end))
								{
									result.DurationSeconds = Math.Max(0, (end - start).TotalSeconds);
									hasCsvDuration = true;
								}
							}

							if (!hasCsvDuration && timeSecondsIdx >= 0 && firstTimeSecondsStr != null &&
								double.TryParse(firstTimeSecondsStr, NumberStyles.Float, CultureInfo.InvariantCulture, out double firstTimeSec))
							{
								var lastTimeSecondsSpan = BenchmarkCsv.GetField(lastLineSpan, timeSecondsIdx);
								if (!lastTimeSecondsSpan.IsEmpty &&
									double.TryParse(lastTimeSecondsSpan, NumberStyles.Float, CultureInfo.InvariantCulture, out double lastTimeSec))
								{
									result.DurationSeconds = Math.Max(0, lastTimeSec - firstTimeSec);
								}
							}
						}

						return (Recording: result, SourceFileNames: sourceFileNames);
					}
					catch (IOException)
					{
						return (Recording: null, SourceFileNames: null);
					}
				})
				.Where(recording => recording.Recording != null)
				.Select(recording => (recording.Recording, recording.SourceFileNames))
				.ToList();

			loadedRecordings.Sort((a, b) => b.Recording.Date.CompareTo(a.Recording.Date));

			Dictionary<string, RecordingItem> recordingsByFileName = new(loadedRecordings.Count, StringComparer.OrdinalIgnoreCase);

			foreach (var (recording, sourceFileNames) in loadedRecordings)
			{
				recordings.Add(recording);
				recordingsByFileName[recording.FileName] = recording;
				if (sourceFileNames.Count > 0)
					aggregateSources[recording] = sourceFileNames;
			}

			if (aggregateSources.Count > 0)
			{
				HashSet<RecordingItem> childRecordings = [];
				foreach (var (aggregate, sourceFileNames) in aggregateSources)
				{
					foreach (string sourceFileName in sourceFileNames.Distinct(StringComparer.OrdinalIgnoreCase))
					{
						if (recordingsByFileName.TryGetValue(sourceFileName, out RecordingItem source) && !ReferenceEquals(source, aggregate))
						{
							aggregate.Children.Add(source);
							childRecordings.Add(source);
						}
					}
				}
				recordings = [.. recordings.Where(recording => !childRecordings.Contains(recording))];
			}

			return recordings;
		});

		SetRecordings(finalRecordings);
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

	[ObservableProperty]
	public partial bool IsRenameEnabled { get; set; }

	public bool IsAddEnabled => !IsRecording;

	[RelayCommand(CanExecute = nameof(IsAddEnabled))]
	private async Task AddAsync()
	{
		var picker = new FilePicker(App.MainWindow)
		{
			ShowAllFilesOption = false,
			InitialDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads"),
			Title = "Add Recordings"
		};
		picker.FileTypeChoices.Add("PresentMon recordings", ["*.csv"]);
		var files = await picker.PickMultipleFilesAsync();
		if (files.Count == 0)
			return;

		foreach (var file in files)
			File.Copy(file.Path, Path.Combine(BenchmarkCsv.RecordingsDirectory, file.Name), true);

		await LoadRecordingsAsync();
	}
	
	public bool IsDeleteEnabled => SelectedRecordingCount > 0;
	
	[RelayCommand(CanExecute = nameof(IsDeleteEnabled))]
	private async Task DeleteAsync(XamlRoot xamlRoot)
	{
		if (SelectedRecordings.Count == 0)
			return;

		var dialog = new ContentDialog
		{
			Title = "Delete recordings",
			Content = $"Are you sure you want to delete {SelectedRecordings.Count} recording{(SelectedRecordings.Count == 1 ? "" : "s")}?",
			PrimaryButtonText = "Delete",
			CloseButtonText = "Cancel",
			DefaultButton = ContentDialogButton.Close,
			XamlRoot = xamlRoot
		};
		if (await dialog.ShowAsync() != ContentDialogResult.Primary)
			return;

		foreach (var recording in SelectedRecordings)
		{
			try
			{
				File.Delete(recording.FilePath);
			}
			catch (IOException)
			{
			}
			catch (UnauthorizedAccessException)
			{
			}
		}

		await LoadRecordingsAsync();
	}

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
	public partial double Delay { get; set; } = 5;

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(CanRecord))]
	public partial double Duration { get; set; } = 60;
	  
	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(ShortcutKeys))]
	public partial VirtualKeyModifiers ShortcutModifiers { get; set; } = VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift;

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(ShortcutKeys))]
	public partial VirtualKey ShortcutKey { get; set; } = VirtualKey.R;

	public List<object> ShortcutKeys
	{
		get
		{
			var keys = new List<object>();
			if (ShortcutModifiers.HasFlag(VirtualKeyModifiers.Control)) keys.Add("Ctrl");
			if (ShortcutModifiers.HasFlag(VirtualKeyModifiers.Shift)) keys.Add("Shift");
			if (ShortcutModifiers.HasFlag(VirtualKeyModifiers.Menu)) keys.Add("Alt");
			if (ShortcutModifiers.HasFlag(VirtualKeyModifiers.Windows)) keys.Add("Win");
			if (ShortcutKey != VirtualKey.None) keys.Add(ShortcutKey.ToString());
			return keys;
		}
	}

	partial void OnDelayChanged(double value) => localSettings.Values["BenchmarkDelay"] = value;
	partial void OnDurationChanged(double value) => localSettings.Values["BenchmarkDuration"] = value;
	partial void OnShortcutModifiersChanged(VirtualKeyModifiers value) => localSettings.Values["BenchmarkShortcut"] = $"{ShortcutModifiers}|{ShortcutKey}";
	partial void OnShortcutKeyChanged(VirtualKey value) => localSettings.Values["BenchmarkShortcut"] = $"{ShortcutModifiers}|{ShortcutKey}";

	public void LoadSettings()
	{
		if (localSettings.Values.TryGetValue("BenchmarkDelay", out var delayObj) && delayObj is double delay)
			Delay = delay;

		if (localSettings.Values.TryGetValue("BenchmarkDuration", out var durationObj) && durationObj is double duration)
			Duration = duration;

		if (localSettings.Values.TryGetValue("BenchmarkShortcut", out var shortcutObj) && shortcutObj is string shortcut && !string.IsNullOrWhiteSpace(shortcut))
		{
			var parts = shortcut.Split('|');
			if (parts.Length == 2 &&
				Enum.TryParse<VirtualKeyModifiers>(parts[0], out var modifiers) &&
				Enum.TryParse<VirtualKey>(parts[1], out var key))
			{
				ShortcutModifiers = modifiers;
				ShortcutKey = key;
			}
		}
	}

	public bool CanRecord => IsRecording || (!string.IsNullOrWhiteSpace(ProcessName) && !double.IsNaN(Delay) && !double.IsNaN(Duration));
	public string RecordLabel => IsRecording ? "Cancel" : "Record";
	public string RecordIconGlyph => IsRecording ? "\uE711" : "\uE7C8";

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(IsAddEnabled))]
	[NotifyCanExecuteChangedFor(nameof(AddCommand))]
	[NotifyPropertyChangedFor(nameof(CanRecord))]
	[NotifyPropertyChangedFor(nameof(RecordIconGlyph))]
	[NotifyPropertyChangedFor(nameof(RecordLabel))]
	public partial bool IsRecording { get; set; }

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(DelayProgress))]
	[NotifyPropertyChangedFor(nameof(DelaySecondsLeft))]
	public partial double DelayRemaining { get; set; }

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(DurationProgress))]
	[NotifyPropertyChangedFor(nameof(DurationSecondsLeft))]
	public partial double DurationRemaining { get; set; }

	public double DurationProgress => (DurationRemaining / Duration) * 100;
	public double DelayProgress => (DelayRemaining / Delay) * 100;
	public int DelaySecondsLeft => (int)Math.Ceiling(DelayRemaining);
	public int DurationSecondsLeft => (int)Math.Ceiling(DurationRemaining);

	[ObservableProperty]
	public partial ObservableCollection<RecordingItem> Recordings { get; set; } = [];

	public bool IsAnalysisToolbarEnabled => SelectedRecordingCount is > 0 and <= 2 && SelectedRecordingsHaveSameProcess;

	public Visibility StatisticsVisibility => (AnalysisChartType is "Bar" or "Column") ? Visibility.Visible : Visibility.Collapsed;

	public Visibility MetricVisibility => AnalysisChartType is "Line" or "Scatter" ? Visibility.Visible : Visibility.Collapsed;
	public Visibility ThresholdsVisibility => AnalysisChartType == "Pie" ? Visibility.Visible : Visibility.Collapsed;

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
	public partial Windows.UI.Color RecordingATertiaryColor { get; set; }

	[ObservableProperty]
	public partial Windows.UI.Color RecordingBTertiaryColor { get; set; }

	[ObservableProperty]
	public partial BrushCollection PieChart1Palette { get; set; }

	[ObservableProperty]
	public partial BrushCollection PieChart2Palette { get; set; }

	[ObservableProperty]
	public partial SolidColorBrush RecordingAColorBrush { get; set; }

	[ObservableProperty]
	public partial SolidColorBrush RecordingBColorBrush { get; set; }

	public bool HasTwoRecordings => SelectedRecordingCount == 2 && SelectedRecordingsHaveSameProcess;
	public Visibility HasTwoRecordingsVisibility => HasTwoRecordings ? Visibility.Visible : Visibility.Collapsed;
	public int PieChartColumnSpan => HasTwoRecordings ? 1 : 2;

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(StatisticsVisibility))]
	[NotifyPropertyChangedFor(nameof(MetricVisibility))]
	[NotifyPropertyChangedFor(nameof(ThresholdsVisibility))]
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

	[ObservableProperty]
	public partial ObservableCollection<PiePoint> PieChartData1 { get; set; } = [];

	[ObservableProperty]
	public partial ObservableCollection<PiePoint> PieChartData2 { get; set; } = [];

	[ObservableProperty]
	public partial string PieChartLabel1 { get; set; } = string.Empty;

	[ObservableProperty]
	public partial string PieChartLabel2 { get; set; } = string.Empty;

	[ObservableProperty]
	public partial double LowFpsThreshold { get; set; } = 25;

	partial void OnLowFpsThresholdChanged(double value)
	{
		if (double.IsNaN(value) || double.IsInfinity(value))
			return;
	}

	[ObservableProperty]
	public partial double StutterFactor { get; set; } = 2.5;

	partial void OnStutterFactorChanged(double value)
	{
		if (double.IsNaN(value) || double.IsInfinity(value))
			return;
		double rounded = Math.Round(value, 1);
		if (value != rounded)
			StutterFactor = rounded;
	}

	public string GetStatisticTooltip(string key) => BenchmarkCsv.StatisticDescriptions.TryGetValue(key, out var desc) ? desc : string.Empty;

	public string GetMetricTooltip(string key) => BenchmarkCsv.MetricDescriptions.TryGetValue(key, out var desc) ? desc : string.Empty;

	[ObservableProperty]
	public partial ObservableCollection<string> BaselineItems { get; set; } = ["None"];

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(IsDeltaModeEnabled))]
	public partial int BaselineSelectedIndex { get; set; }

	public bool IsDeltaModeEnabled => BaselineSelectedIndex >= 1;

	[ObservableProperty]
	public partial bool IsPercentDelta { get; set; }

	[ObservableProperty]
	public partial ObservableCollection<ResultRow> StatisticsRows { get; set; } = [];

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(RecordingAColorTooltip))]
	public partial string RecordingAHeader { get; set; } = "Recording A";

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(RecordingBColorTooltip))]
	public partial string RecordingBHeader { get; set; } = "Recording B";

	public string RecordingAColorTooltip => $"{RecordingAHeader} Color";
	public string RecordingBColorTooltip => $"{RecordingBHeader} Color";

	[ObservableProperty]
	public partial string DeltaHeader { get; set; }

	[ObservableProperty]
	public partial bool ShowRecordingAColumn { get; set; }

	[ObservableProperty]
	public partial bool ShowRecordingBColumn { get; set; }

	[ObservableProperty]
	public partial bool ShowDeltaColumn { get; set; }

	partial void OnBaselineSelectedIndexChanged(int value)
	{
		UpdateStatisticsColumns();
	}

	partial void OnIsPercentDeltaChanged(bool value)
	{
		UpdateStatisticsColumns();
	}

	public int BaselineIndex => SelectedRecordings.Count == 2 && BaselineSelectedIndex is 1 or 2 ? BaselineSelectedIndex - 1 : -1;

	private void UpdateStatisticsColumns()
	{
		int count = SelectedRecordings.Count;
		int baseline = BaselineIndex;

		ShowRecordingAColumn = count >= 1 && baseline != 1;
		ShowRecordingBColumn = count == 2 && baseline != 0;
		ShowDeltaColumn = baseline >= 0;

		RecordingAHeader = count >= 1 ? SelectedRecordings[0].Title + (baseline == 0 ? " (Baseline)" : string.Empty) : "Recording A";
		RecordingBHeader = count >= 2 ? SelectedRecordings[1].Title + (baseline == 1 ? " (Baseline)" : string.Empty) : "Recording B";
		DeltaHeader = baseline >= 0 ? $"{SelectedRecordings[1 - baseline].Title} (Delta)" : IsPercentDelta ? "Delta (%)" : "Delta (+/-)";
	}

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
		RecordingATertiaryColor = DevWinUI.ColorHelper.GetInterpolatedColor(0.35, RecordingASecondaryColor, Colors.White);
		PieChart1Palette = new BrushCollection
		{
			new SolidColorBrush(value),
			new SolidColorBrush(RecordingASecondaryColor),
			new SolidColorBrush(RecordingATertiaryColor)
		};
	}

	partial void OnRecordingBColorChanged(Windows.UI.Color value)
	{
		RecordingBColorBrush = new SolidColorBrush(value);
		RecordingBSecondaryColor = DevWinUI.ColorHelper.GetInterpolatedColor(0.35, value, Colors.White);
		RecordingBTertiaryColor = DevWinUI.ColorHelper.GetInterpolatedColor(0.35, RecordingBSecondaryColor, Colors.White);
		PieChart2Palette = new BrushCollection
		{
			new SolidColorBrush(value),
			new SolidColorBrush(RecordingBSecondaryColor),
			new SolidColorBrush(RecordingBTertiaryColor)
		};
	}

	public void ShowDelay(int seconds)
	{
		DelayRemaining = seconds;
		RecordingState = "Delay";
	}

	public void ShowDuration()
	{
		RecordingState = "Duration";
		DurationRemaining = Duration;
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

		for (int i = ProcessSuggestions.Count - 1; i >= 0; i--)
		{
			if (!suggestions.Contains(ProcessSuggestions[i], StringComparer.OrdinalIgnoreCase))
				ProcessSuggestions.RemoveAt(i);
		}

		int insertIndex = 0;
		foreach (string suggestion in suggestions)
		{
			if (!ProcessSuggestions.Contains(suggestion, StringComparer.OrdinalIgnoreCase))
				ProcessSuggestions.Insert(insertIndex, suggestion);
			insertIndex++;
		}
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

public enum ComparisonResult
{
	None,
	Better,
	Worse
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
	public partial ComparisonResult RecordingAComparison { get; set; }

	[ObservableProperty]
	public partial ComparisonResult RecordingBComparison { get; set; }

	[ObservableProperty]
	public partial ComparisonResult DeltaComparison { get; set; }

	public ObservableCollection<ResultRow> Children { get; } = [];

	internal double? RecordingAValue { get; set; }
	internal double? RecordingBValue { get; set; }
	internal BenchmarkCsv.StatisticDefinition Definition { get; set; }
}

public sealed partial class ResultCellStyleSelector : StyleSelector
{
	public Style SuccessStyle { get; set; }
	public Style CriticalStyle { get; set; }

	protected override Style SelectStyleCore(object item, DependencyObject container)
	{
		if (item is not ResultRow row || container is not TreeGridCell cell)
			return null;
		var comparison = cell.ColumnBase?.TreeGridColumn.MappingName switch
		{
			nameof(ResultRow.RecordingA) => row.RecordingAComparison,
			nameof(ResultRow.RecordingB) => row.RecordingBComparison,
			nameof(ResultRow.Delta) => row.DeltaComparison,
			_ => ComparisonResult.None
		};
		return comparison switch
		{
			ComparisonResult.Better => SuccessStyle,
			ComparisonResult.Worse => CriticalStyle,
			_ => null
		};
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
