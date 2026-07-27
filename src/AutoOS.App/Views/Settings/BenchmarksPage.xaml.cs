using System.Globalization;
using AutoOS.Core.Helpers.Benchmark;
using AutoOS.Core.Helpers.Picker;
using AutoOS.Core.Models;
using AutoOS.Views.Settings.Benchmarks;
using Syncfusion.UI.Xaml.Charts;
using Syncfusion.UI.Xaml.Grids;
using Syncfusion.UI.Xaml.TreeGrid;
using System.Text.Json;
using Windows.System;
using Syncfusion.UI.Xaml.Grids.ScrollAxis;
using Syncfusion.UI.Xaml.DataGrid;

namespace AutoOS.Views.Settings;

public sealed partial class BenchmarksPage : Page
{
	public BenchmarksPageViewModel ViewModel { get; } = new();
	private static readonly string RecordingsDirectory = Path.Combine(PathHelper.GetAppDataFolderPath(), "Benchmarks");

	private readonly PresentMonRecorder _recorder = new();
	private List<RecordingItem> _selectedRecordings = [];
	private ChartPresentation _lastChartPresentation;
	private CancellationTokenSource _statsCts = new();
	private GlobalKeyboardHook _globalKeyboardHook;
	private VirtualKeyModifiers _currentModifiers = VirtualKeyModifiers.Shift;
	private VirtualKey _currentKey = VirtualKey.F11;

	internal PresentMonProcessDiscovery PresentingProcesses { get; } = new();

	public BenchmarksPage()
	{
		InitializeComponent();
		LoadRecordings();
	}

	protected override void OnNavigatedTo(NavigationEventArgs e)
	{
		base.OnNavigatedTo(e);
		PresentingProcesses.Start();
		_globalKeyboardHook = new GlobalKeyboardHook();
		_globalKeyboardHook.KeyDown += OnGlobalKeyDown;
		_globalKeyboardHook.Start();
		PresentingProcesses.ProcessesChanged += PresentingProcesses_ProcessesChanged;
		ViewModel.MetricToggled += OnMetricToggled;
	}

	protected override void OnNavigatedFrom(NavigationEventArgs e)
	{
		base.OnNavigatedFrom(e);
		if (_globalKeyboardHook != null)
		{
			_globalKeyboardHook.KeyDown -= OnGlobalKeyDown;
			_globalKeyboardHook.Stop();
			_globalKeyboardHook.Dispose();
			_globalKeyboardHook = null;
		}
		PresentingProcesses.ProcessesChanged -= PresentingProcesses_ProcessesChanged;
		ViewModel.MetricToggled -= OnMetricToggled;
	}
	private void LoadRecordings()
	{
		List<RecordingItem> recordings = [];
		Dictionary<RecordingItem, List<string>> aggregateSources = [];

		if (!Directory.Exists(RecordingsDirectory))
		{
			Directory.CreateDirectory(RecordingsDirectory);
			ViewModel.SetRecordings(recordings);
			ViewModel.SetSelectedRecordings([]);
			_selectedRecordings = [];
			return;
		}

		List<string> csvFiles = [.. Directory.GetFiles(RecordingsDirectory, "*.csv").OrderByDescending(File.GetLastWriteTime)];

		if (csvFiles.Count == 0)
		{
			ViewModel.SetRecordings(recordings);
		}
		else
		{
			foreach (var file in csvFiles)
			{
				var info = new FileInfo(file);
				var (process, presentationMode, durationSeconds, sourceFileNames) = LoadRecordingMetadata(file, info);
				var recording = new RecordingItem
				{
					FilePath = file,
					FileName = info.Name,
					Title = Path.GetFileNameWithoutExtension(info.Name),
					Process = process,
					PresentationMode = presentationMode,
					DurationSeconds = durationSeconds,
					Date = info.LastWriteTime,
					Time = info.LastWriteTime.TimeOfDay
				};
				recordings.Add(recording);
				if (sourceFileNames.Count > 0)
					aggregateSources[recording] = sourceFileNames;
			}

			Dictionary<string, RecordingItem> recordingsByFileName = recordings.ToDictionary(recording => recording.FileName, StringComparer.OrdinalIgnoreCase);
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
			ViewModel.SetRecordings(recordings.Where(recording => !childRecordings.Contains(recording)));
		}

		_selectedRecordings = GetSelectedRecordings();
		ViewModel.SetSelectedRecordings(_selectedRecordings);
	}

	private void RecordingsTreeGrid_SizeChanged(object sender, SizeChangedEventArgs e)
	{
		if (e.NewSize.Width > 0)
		{
			foreach (var col in RecordingsTreeGrid.Columns)
				col.Width = double.NaN;
			RecordingsTreeGrid.InvalidateMeasure();
			RecordingsTreeGrid.UpdateLayout();
		}
	}

	private async void RecordingsTreeGrid_SelectionChanged(object sender, GridSelectionChangedEventArgs e)
	{
		_selectedRecordings = GetSelectedRecordings();
		ViewModel.SetSelectedRecordings(_selectedRecordings);
		ViewModel.ClearAnalysis();
		ViewModel.RefreshChartColors();
		ViewModel.StatisticsRows.Clear();
		ViewModel.AnalysisChartType = "Bar";

		if (_selectedRecordings.Count is 0 or > 2)
			return;

		UpdateAnalysisCharts(_selectedRecordings);
		await UpdateStatisticsTable();
	}

	private void StatisticsTreeGrid_SizeChanged(object sender, SizeChangedEventArgs e)
	{
		if (e.NewSize.Width > 0)
		{
			foreach (var col in StatisticsTreeGrid.Columns)
				col.Width = double.NaN;
			StatisticsTreeGrid.InvalidateMeasure();
			StatisticsTreeGrid.UpdateLayout();
		}
	}

	private void AnalysisChartTypeSelector_SelectionChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
	{
		var oldChartType = ViewModel.AnalysisChartType;
		ViewModel.AnalysisChartType = sender.SelectedItem switch
		{
			var item when item == BarChartItem => "Bar",
			var item when item == ColumnChartItem => "Column",
			var item when item == LineChartItem => "Line",
			var item when item == ScatterChartItem => "Scatter",
			_ => null
		};

		var newChartType = ViewModel.AnalysisChartType;
		if (newChartType == oldChartType)
			return;
		ReplayAnimation();
	}

	private void ReplayAnimation()
	{
		if (ViewModel.AnalysisChartType is "Bar" or "Column")
		{
			var oldDisplayedData1 = ViewModel.BarColumnChartDisplayedData1;
			var oldRenderedData1 = ViewModel.BarColumnChartRenderedData1;
			var oldDisplayedData2 = ViewModel.BarColumnChartDisplayedData2;
			var oldRenderedData2 = ViewModel.BarColumnChartRenderedData2;
			ViewModel.BarColumnChartDisplayedData1 = [];
			ViewModel.BarColumnChartRenderedData1 = [];
			ViewModel.BarColumnChartDisplayedData2 = [];
			ViewModel.BarColumnChartRenderedData2 = [];
			ViewModel.BarColumnChartDisplayedData1 = oldDisplayedData1;
			ViewModel.BarColumnChartRenderedData1 = oldRenderedData1;
			ViewModel.BarColumnChartDisplayedData2 = oldDisplayedData2;
			ViewModel.BarColumnChartRenderedData2 = oldRenderedData2;
		}
		else
		{
			var oldData1 = ViewModel.LineScatterChartData1;
			var oldData2 = ViewModel.LineScatterChartData2;
			ViewModel.LineScatterChartData1 = [];
			ViewModel.LineScatterChartData2 = [];
			ViewModel.LineScatterChartData1 = oldData1;
			ViewModel.LineScatterChartData2 = oldData2;
		}
	}

	private async void AddRecording_Click(object sender, RoutedEventArgs e)
	{
		var picker = new FilePicker(App.MainWindow)
		{
			ShowAllFilesOption = false,
			InitialDirectory = RecordingsDirectory,
			Title = "Add Recordings"
		};
		picker.FileTypeChoices.Add("PresentMon recordings", ["*.csv"]);
		var files = await picker.PickMultipleFilesAsync();
		if (files.Count == 0)
			return;

		foreach (var file in files)
			File.Copy(file.Path, Path.Combine(RecordingsDirectory, file.Name), true);

		LoadRecordings();
	}

	private void RenameRecording_Click(object sender, RoutedEventArgs e)
	{
		var selected = GetSelectedRecordings();
		if (selected.Count == 0)
			return;

		var titleColumn = RecordingsTreeGrid.Columns.FirstOrDefault(c => c.MappingName == "Title");
		if (titleColumn is null)
			return;

		var colIndex = RecordingsTreeGrid.Columns.IndexOf(titleColumn);
		var rowIndex = TreeGridIndexResolver.ResolveToRowIndex(RecordingsTreeGrid, selected[0]);
		if (rowIndex < 0)
			return;

		RecordingsTreeGrid.SelectionController.MoveCurrentCell(new RowColumnIndex(rowIndex, colIndex));
		RecordingsTreeGrid.SelectionController.CurrentCellManager.BeginEdit();
	}

	private async void RecordingsTreeGrid_CurrentCellEndEdit(object sender, CurrentCellEndEditEventArgs e)
	{
		if (RecordingsTreeGrid.GetNodeAtRowIndex(e.RowColumnIndex.RowIndex)?.Item is not RecordingItem recording)
			return;

		var dir = Path.GetDirectoryName(recording.FilePath);
		var ext = Path.GetExtension(recording.FilePath);
		var newPath = Path.Combine(dir, recording.Title + ext);
		if (newPath == recording.FilePath)
			return;

		File.Move(recording.FilePath, newPath);
		recording.FilePath = newPath;
		recording.FileName = recording.Title + ext;

		RecordingsTreeGrid_SelectionChanged(RecordingsTreeGrid, null);
	}

	private async void DeleteRecording_Click(object sender, RoutedEventArgs e)
	{
		var selected = GetSelectedRecordings();
		if (selected.Count == 0)
			return;

		var dialog = new ContentDialog
		{
			Title = "Delete recordings",
			Content = $"Are you sure you want to delete {selected.Count} recording{(selected.Count == 1 ? "" : "s")}?",
			PrimaryButtonText = "Delete",
			CloseButtonText = "Cancel",
			DefaultButton = ContentDialogButton.Close,
			XamlRoot = XamlRoot
		};
		if (await dialog.ShowAsync() != ContentDialogResult.Primary)
			return;

		foreach (var recording in selected)
		{
			File.Delete(recording.FilePath);
		}

		LoadRecordings();
	}

	private async void Record_Checked(object sender, RoutedEventArgs e)
	{
		if (ViewModel.IsRecording)
			return;

		if (!ViewModel.CanRecord)
		{
			Record.IsChecked = false;
			return;
		}

		ViewModel.IsRecording = true;
		Record.IsChecked = true;

		int duration = (int)ViewModel.RecordingDuration;
		int delay = (int)ViewModel.RecordingDelay;
		string processName = ViewModel.ProcessName.Trim();
		string presentMonPath = Path.Combine(RecordingsDirectory, "PresentMon.exe");
		string errorMessage = string.Empty;
		PresentMonRecordingResult recordingResult = PresentMonRecordingResult.Stopped;

		try
		{
			recordingResult = await _recorder.RecordAsync(presentMonPath, RecordingsDirectory, processName, duration, delay);
		}
		catch (Exception ex)
		{
			errorMessage = ex.Message;
		}
		finally
		{
			ViewModel.IsRecording = false;
			Record.IsChecked = false;
			LoadRecordings();
			if (!string.IsNullOrWhiteSpace(errorMessage))
			{
				await new ContentDialog
				{
					Title = "Recording Error",
					Content = errorMessage,
					CloseButtonText = "OK",
					XamlRoot = XamlRoot
				}.ShowAsync();
			}
		}

		if (string.IsNullOrWhiteSpace(errorMessage) && recordingResult == PresentMonRecordingResult.NotSaved)
		{
			await MessageBox.ShowErrorAsync(App.MainWindow, "Process either wasn't in foreground or cannot be recorded with PresentMon", "Recording failed");
		}
	}

	private void Record_Unchecked(object sender, RoutedEventArgs e)
	{
		if (!ViewModel.IsRecording)
			return;
		_recorder.Stop();
	}

	private async void Aggregate_Click(object sender, RoutedEventArgs e)
	{
		var selected = GetSelectedRecordings();

		string processName = selected[0].Process;
		if (selected.Any(recording => !string.Equals(recording.Process, processName, StringComparison.OrdinalIgnoreCase)))
		{
			await new ContentDialog
			{
				Title = "Recording Error",
				Content = "Only recordings from the same process can be aggregated.",
				CloseButtonText = "OK",
				XamlRoot = XamlRoot
			}.ShowAsync();
			return;
		}

		int aggregateNumber = 1;
		string outPath;
		do
		{
			outPath = Path.Combine(RecordingsDirectory, $"Aggregate-{aggregateNumber++}.csv");
		}
		while (File.Exists(outPath));

		var fileData = new List<(List<string[]> Rows, List<string> HeaderList)>();
		foreach (var item in selected)
		{
			var lines = File.ReadAllLines(item.FilePath);
			if (lines.Length == 0) continue;
			var headerList = BenchmarkCsv.ParseCsvLine(lines[0]);
			List<string[]> rows = [.. lines.Skip(1).Select(l => BenchmarkCsv.ParseCsvLine(l).ToArray())];
			fileData.Add((rows, headerList));
		}

		if (fileData.Count < 2)
			throw new Exception("Not enough data to aggregate.");

		List<string> headerCols = [.. fileData[0].HeaderList];
		int applicationIndex = headerCols.FindIndex(header =>
			string.Equals(header, "Application", StringComparison.OrdinalIgnoreCase));
		if (applicationIndex < 0)
		{
			applicationIndex = headerCols.Count;
			headerCols.Add("Application");
		}

		int aggregateDurationIndex = headerCols.FindIndex(header =>
			string.Equals(header, "AutoOSAggregateDurationSeconds", StringComparison.OrdinalIgnoreCase));
		if (aggregateDurationIndex < 0)
		{
			aggregateDurationIndex = headerCols.Count;
			headerCols.Add("AutoOSAggregateDurationSeconds");
		}

		int aggregateSourcesIndex = headerCols.FindIndex(header =>
			string.Equals(header, "AutoOSAggregateSources", StringComparison.OrdinalIgnoreCase));
		if (aggregateSourcesIndex < 0)
		{
			aggregateSourcesIndex = headerCols.Count;
			headerCols.Add("AutoOSAggregateSources");
		}

		string aggregateSources = Convert.ToBase64String(JsonSerializer.SerializeToUtf8Bytes([.. selected.Select(recording => recording.FileName).Distinct(StringComparer.OrdinalIgnoreCase)], BenchmarksJsonContext.Default.StringArray));

		double meanDurationSeconds = selected.Average(recording => recording.DurationSeconds);
		int maxRows = fileData.Max(f => f.Rows.Count);

		using var writer = new StreamWriter(outPath);
		writer.WriteLine(string.Join(",", headerCols));
		for (int r = 0; r < maxRows; r++)
		{
			var averagedRow = new string[headerCols.Count];
			for (int c = 0; c < headerCols.Count; c++)
			{
				if (c == applicationIndex)
				{
					averagedRow[c] = processName;
					continue;
				}
				if (c == aggregateDurationIndex)
				{
					averagedRow[c] = meanDurationSeconds.ToString(CultureInfo.InvariantCulture);
					continue;
				}
				if (c == aggregateSourcesIndex)
				{
					averagedRow[c] = r == 0 ? aggregateSources : string.Empty;
					continue;
				}

				double sum = 0;
				int count = 0;
				foreach (var (rows, _) in fileData)
				{
					if (r < rows.Count && c < rows[r].Length && double.TryParse(rows[r][c], NumberStyles.Float, CultureInfo.InvariantCulture, out double value))
					{
						sum += value;
						count++;
					}
				}
				if (count > 0)
					averagedRow[c] = (sum / count).ToString(CultureInfo.InvariantCulture);
				else if (r < fileData[0].Rows.Count && c < fileData[0].Rows[r].Length)
					averagedRow[c] = fileData[0].Rows[r][c];
				else
					averagedRow[c] = string.Empty;
			}
			writer.WriteLine(string.Join(",", averagedRow));
		}

		LoadRecordings();
	}

	private void MetricComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		var presentation = BuildAnalysisPresentation(_selectedRecordings, Metric1ComboBox.SelectedItem as string ?? string.Empty);
		if (presentation != null)
			ApplyMetricChartPresentation(presentation);
	}

	private void HotkeyShortcut_PrimaryButtonClick(object sender, ContentDialogButtonClickEventArgs e)
	{
		HotkeyShortcut.UpdatePreviewKeys();
		HotkeyShortcut.CloseContentDialog();
		if (HotkeyShortcut.Keys == null) return;

		_currentModifiers = VirtualKeyModifiers.None;
		_currentKey = VirtualKey.None;

		foreach (var item in HotkeyShortcut.Keys)
		{
			string keyName = item switch
			{
				KeyVisualInfo kvi => kvi.KeyName ?? string.Empty,
				string s => s,
				_ => item?.ToString() ?? string.Empty
			};
			VirtualKey virtKey = item is KeyVisualInfo k ? k.Key.GetValueOrDefault() : VirtualKey.None;

			if (keyName.Contains("Ctrl", StringComparison.OrdinalIgnoreCase))
				_currentModifiers |= VirtualKeyModifiers.Control;
			else if (keyName.Contains("Shift", StringComparison.OrdinalIgnoreCase))
				_currentModifiers |= VirtualKeyModifiers.Shift;
			else if (keyName.Contains("Alt", StringComparison.OrdinalIgnoreCase))
				_currentModifiers |= VirtualKeyModifiers.Menu;
			else if (keyName.Contains("Win", StringComparison.OrdinalIgnoreCase))
				_currentModifiers |= VirtualKeyModifiers.Windows;
			else if (virtKey != VirtualKey.None)
				_currentKey = virtKey;
			else if (Enum.TryParse<VirtualKey>(keyName, ignoreCase: true, out var parsed) &&
				parsed != VirtualKey.None)
				_currentKey = parsed;
		}
	}

	private void OnGlobalKeyDown(object sender, KeyboardHookEventArgs e)
	{
		if (e.Key == _currentKey &&
			e.IsCtrl == _currentModifiers.HasFlag(VirtualKeyModifiers.Control) &&
			e.IsShift == _currentModifiers.HasFlag(VirtualKeyModifiers.Shift) &&
			e.IsAlt == _currentModifiers.HasFlag(VirtualKeyModifiers.Menu) &&
			e.IsWindows == _currentModifiers.HasFlag(VirtualKeyModifiers.Windows))
		{
			DispatcherQueue.TryEnqueue(() =>
			{
				if (ViewModel.CanRecord)
					Record_Checked(this, null);
			});
		}
	}

	private void ProcessAutoSuggestBox_GotFocus(object sender, RoutedEventArgs e)
	{
		ProcessAutoSuggestBox.IsSuggestionListOpen = ViewModel.ProcessSuggestions.Count > 0;
	}

	private void PresentingProcesses_ProcessesChanged(object sender, EventArgs e)
	{
		DispatcherQueue.TryEnqueue(() =>
		{
			if (ViewModel.ActiveTab == "Recordings")
				ViewModel.SetRecordableProcesses(PresentingProcesses.GetRecordableProcesses());
		});
	}

	private void ProcessAutoSuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
	{
		if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
		{
			ViewModel.ProcessName = sender.Text;
			sender.IsSuggestionListOpen = ViewModel.ProcessSuggestions.Count > 0;
		}
	}

	private void ProcessAutoSuggestBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
	{
		sender.Text = args.SelectedItem as string ?? string.Empty;
		ViewModel.ProcessName = sender.Text;
	}

	private async void BenchmarksSelectorBar_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
	{
		var selectedItem = args.SelectedItem ?? sender.SelectedItem;

		if (ReferenceEquals(selectedItem, RecordingsTab))
		{
			ViewModel.ActiveTab = "Recordings";
			PresentingProcesses.Start();
			ViewModel.SetRecordableProcesses(PresentingProcesses.GetRecordableProcesses(refreshRunningProcesses: true));
		}
		else if (ReferenceEquals(selectedItem, AnalysisTab))
		{
			ViewModel.ActiveTab = "Analysis";
			ViewModel.AnalysisChartType = "Bar";
			if (_selectedRecordings.Count is > 0 and <= 2)
				ReplayAnimation();
		}
		else if (ReferenceEquals(selectedItem, StatisticsTab))
			ViewModel.ActiveTab = "Statistics";
	}

	private void UpdateAnalysisCharts(List<RecordingItem> items)
	{
		if (items.Count is 0 or > 2)
		{
			_lastChartPresentation = null;
			return;
		}

		var presentation = BuildAnalysisPresentation(items, Metric1ComboBox.SelectedItem as string ?? string.Empty);
		if (presentation == null)
		{
			_lastChartPresentation = null;
			return;
		}

		ApplyBarColumnChartPresentation(presentation);
		ApplyMetricChartPresentation(presentation);
	}

	private sealed record AnalysisModel(List<(string recordingName, List<SeriesPoint> points)> MetricSeries, List<(string recordingName, Dictionary<string, double> displayedStats, Dictionary<string, double> renderedStats)> FpsStatsSeries);

	private sealed record ChartPresentation(
		List<BarPoint> DisplayedFpsBars1,
		List<BarPoint> RenderedFpsBars1,
		List<BarPoint> DisplayedFpsBars2,
		List<BarPoint> RenderedFpsBars2,
		bool ShowRenderedFps1,
		string DisplayedFpsLabel1,
		string RenderedFpsLabel1,
		string DisplayedFpsLabel2,
		string RenderedFpsLabel2,
		List<SeriesPoint> MetricPts1,
		List<SeriesPoint> MetricPts2,
		string MetricLabel1,
		string MetricLabel2,
		string FpsYAxisLabel,
		string FpsLabelFormat,
		string MetricYAxisLabel);


	private ChartPresentation BuildAnalysisPresentation(List<RecordingItem> items, string metric)
	{
		List<(RecordingItem item, DateTime lastWriteUtc)> loaded = [.. items
			.Select(item => (item, lastWriteUtc: File.Exists(item.FilePath)
				? File.GetLastWriteTimeUtc(item.FilePath)
				: DateTime.MinValue))
			.Where(entry => entry.lastWriteUtc != DateTime.MinValue)];

		if (loaded.Count == 0)
			return null;

		if (!loaded.Any(entry =>
			GetHeaderIndex(entry.item.FilePath, entry.lastWriteUtc, out var h) &&
			(h.TryGetValue("MsBetweenDisplayChange", out _) ||
			 h.TryGetValue("MsBetweenPresents", out _))))
			return null;

		List<(string recordingName, List<SeriesPoint> points)> metricSeries = [];
		List<(string recordingName,
			Dictionary<string, double> displayedStats,
			Dictionary<string, double> renderedStats)> fpsStatsSeries = [];

		string metricColumn = metric switch
		{
			"Displayed FPS" => "MsBetweenDisplayChange",
			"Rendered FPS" => "MsBetweenPresents",
			_ => metric
		};

		for (int recordingIndex = 0; recordingIndex < loaded.Count; recordingIndex++)
		{
			var (item, lastWriteUtc) = loaded[recordingIndex];
			LoadAnalysisColumns(item.FilePath, lastWriteUtc);
			LoadMetricColumn(item.FilePath, lastWriteUtc, metricColumn, out var rawMetricValues);

			List<SeriesPoint> points = new(rawMetricValues.Count);
			for (int index = 0; index < rawMetricValues.Count; index++)
				points.Add(new SeriesPoint { Index = index + 1, Value = rawMetricValues[index] });
			metricSeries.Add((item.FileName, points));

			List<double> displayedFrameTimes;
			if (string.Equals(metricColumn, "MsBetweenDisplayChange",
				StringComparison.OrdinalIgnoreCase))
				displayedFrameTimes = [.. rawMetricValues];
			else
				LoadMetricColumn(item.FilePath, lastWriteUtc, "MsBetweenDisplayChange", out displayedFrameTimes);

			List<double> renderedFrameTimes;
			if (string.Equals(metricColumn, "MsBetweenPresents", StringComparison.OrdinalIgnoreCase))
				renderedFrameTimes = [.. rawMetricValues];
			else
				LoadMetricColumn(item.FilePath, lastWriteUtc, "MsBetweenPresents", out renderedFrameTimes);

			fpsStatsSeries.Add((item.FileName, BenchmarkCsv.StatsToDict(BenchmarkStatistics.CalculateMetrics([.. displayedFrameTimes.Where(v => v > 0).Select(v => 1000.0 / v)], isFpsMetric: true)), BenchmarkCsv.StatsToDict(BenchmarkStatistics.CalculateMetrics([.. renderedFrameTimes.Where(v => v > 0).Select(v => 1000.0 / v)], isFpsMetric: true))));
		}

		return BuildChartPresentation(new AnalysisModel(metricSeries, fpsStatsSeries));
	}

	private static ChartPresentation BuildChartPresentation(AnalysisModel model)
	{
		List<BarPoint> displayedFpsBars1 = [];
		List<BarPoint> renderedFpsBars1 = [];
		List<BarPoint> displayedFpsBars2 = [];
		List<BarPoint> renderedFpsBars2 = [];
		bool showRenderedFps1 = false;
		string displayedFpsLabel1 = string.Empty;
		string renderedFpsLabel1 = string.Empty;
		string displayedFpsLabel2 = string.Empty;
		string renderedFpsLabel2 = string.Empty;

		if (model.FpsStatsSeries.Count > 0)
		{
			int seriesIdx = 0;
			foreach (var (recordingName, displayedStats, renderedStats) in model.FpsStatsSeries)
			{
				List<BarPoint> displayedTarget = seriesIdx == 0 ? displayedFpsBars1 : displayedFpsBars2;
				List<BarPoint> renderedTarget = seriesIdx == 0 ? renderedFpsBars1 : renderedFpsBars2;

				foreach (string percentile in BenchmarkCsv.MetricLabelsShort)
				{
					if (displayedStats.TryGetValue(percentile, out double displayedValue))
						displayedTarget.Add(new BarPoint { Label = percentile, Value = displayedValue });
					if (renderedStats.TryGetValue(percentile, out double renderedValue))
						renderedTarget.Add(new BarPoint { Label = percentile, Value = renderedValue });
				}

				if (seriesIdx == 0)
				{
					displayedFpsLabel1 = $"{recordingName} \u00b7 Displayed FPS";
					renderedFpsLabel1 = $"{recordingName} \u00b7 Rendered FPS";
					showRenderedFps1 = renderedTarget.Count > 0;
				}
				else
				{
					displayedFpsLabel2 = $"{recordingName} \u00b7 Displayed FPS";
					renderedFpsLabel2 = $"{recordingName} \u00b7 Rendered FPS";
				}
				seriesIdx++;
			}
		}

		List<SeriesPoint> metricPts1 = [];
		List<SeriesPoint> metricPts2 = [];
		string metricLabel1 = string.Empty;
		string metricLabel2 = string.Empty;

		if (model.MetricSeries.Count > 0)
		{
			int seriesIdx = 0;
			foreach (var (recordingName, points) in model.MetricSeries)
			{
				if (seriesIdx == 0)
				{
					metricPts1 = points;
					metricLabel1 = recordingName;
				}
				else
				{
					metricPts2 = points;
					metricLabel2 = recordingName;
				}
				seriesIdx++;
			}
		}

		return new ChartPresentation(
			displayedFpsBars1, renderedFpsBars1,
			displayedFpsBars2, renderedFpsBars2,
			showRenderedFps1,
			displayedFpsLabel1, renderedFpsLabel1,
			displayedFpsLabel2, renderedFpsLabel2,
			metricPts1, metricPts2,
			metricLabel1, metricLabel2,
			"FPS", "0.#", "Milliseconds (ms)");
	}

	private void ApplyBarColumnChartPresentation(ChartPresentation presentation)
	{
		_lastChartPresentation = presentation;
		ViewModel.BarColumnChartYAxisLabel = presentation.FpsYAxisLabel;
		ViewModel.BarColumnChartLabelFormat = presentation.FpsLabelFormat;
		ViewModel.BarColumnChartDisplayedLabel1 = presentation.DisplayedFpsLabel1;
		ViewModel.BarColumnChartRenderedLabel1 = presentation.RenderedFpsLabel1;
		ViewModel.BarColumnChartDisplayedLabel2 = presentation.DisplayedFpsLabel2;
		ViewModel.BarColumnChartRenderedLabel2 = presentation.RenderedFpsLabel2;
		ViewModel.BarColumnRenderedVisible = presentation.ShowRenderedFps1;

		var series1Data = presentation.DisplayedFpsBars1.Where(b => ViewModel.IsMetricEnabled(b.Label)).ToList();
		var series1RenderedData = presentation.RenderedFpsBars1.Where(b => ViewModel.IsMetricEnabled(b.Label)).ToList();
		var series2Data = presentation.DisplayedFpsBars2.Where(b => ViewModel.IsMetricEnabled(b.Label)).ToList();
		var series2RenderedData = presentation.RenderedFpsBars2.Where(b => ViewModel.IsMetricEnabled(b.Label)).ToList();

		bool hasSecondRecording = _selectedRecordings.Count == 2;

		ViewModel.BarColumnChartDisplayedData1 = [.. series1Data];
		ViewModel.BarColumnChartRenderedData1 = presentation.ShowRenderedFps1 ? [.. series1RenderedData] : null;
		ViewModel.BarColumnChartDisplayedData2 = hasSecondRecording ? [.. series2Data] : null;
		ViewModel.BarColumnChartRenderedData2 = hasSecondRecording ? [.. series2RenderedData] : null;

		if (FpsChart != null)
		{
			FpsChart.Series.Clear();
			FpsChart.Series.Add(FpsChart1Series);
			if (presentation.ShowRenderedFps1)
				FpsChart.Series.Add(FpsRenderedChart1Series);
			if (hasSecondRecording)
				FpsChart.Series.Add(FpsChart2Series);
			if (hasSecondRecording)
				FpsChart.Series.Add(FpsRenderedChart2Series);

			FpsChart1Series.ShowDataLabels = false;
			FpsChart1Series.ShowDataLabels = true;
			FpsRenderedChart1Series.ShowDataLabels = false;
			FpsRenderedChart1Series.ShowDataLabels = presentation.ShowRenderedFps1;
			FpsChart2Series.ShowDataLabels = false;
			FpsChart2Series.ShowDataLabels = hasSecondRecording;
			FpsRenderedChart2Series.ShowDataLabels = false;
			FpsRenderedChart2Series.ShowDataLabels = hasSecondRecording;

			FpsChart.IsTransposed = false;
			FpsChart.IsTransposed = true;
		}

		if (ColumnFpsChart != null)
		{
			ColumnFpsChart.Series.Clear();
			ColumnFpsChart.Series.Add(ColumnFpsChart1Series);
			if (presentation.ShowRenderedFps1)
				ColumnFpsChart.Series.Add(ColumnFpsRenderedChart1Series);
			if (hasSecondRecording)
				ColumnFpsChart.Series.Add(ColumnFpsChart2Series);
			if (hasSecondRecording)
				ColumnFpsChart.Series.Add(ColumnFpsRenderedChart2Series);

			ColumnFpsChart1Series.ShowDataLabels = false;
			ColumnFpsChart1Series.ShowDataLabels = true;
			ColumnFpsRenderedChart1Series.ShowDataLabels = false;
			ColumnFpsRenderedChart1Series.ShowDataLabels = presentation.ShowRenderedFps1;
			ColumnFpsChart2Series.ShowDataLabels = false;
			ColumnFpsChart2Series.ShowDataLabels = hasSecondRecording;
			ColumnFpsRenderedChart2Series.ShowDataLabels = false;
			ColumnFpsRenderedChart2Series.ShowDataLabels = hasSecondRecording;
		}
	}

	private void ApplyMetricChartPresentation(ChartPresentation presentation)
	{
		ViewModel.LineScatterChartYAxisLabel = presentation.MetricYAxisLabel;
		ViewModel.LineScatterChartLabel1 = presentation.MetricLabel1;
		ViewModel.LineScatterChartLabel2 = presentation.MetricLabel2;
		ViewModel.LineScatterChartData1 = [.. presentation.MetricPts1];
		ViewModel.LineScatterChartData2 = [.. presentation.MetricPts2];
	}

	private void OnMetricToggled()
	{
		if (_lastChartPresentation != null)
		{
			ApplyBarColumnChartPresentation(_lastChartPresentation);
		}
	}

	private async Task UpdateStatisticsTable()
	{
		var oldCts = _statsCts;
		_statsCts = new();
		oldCts.Cancel();
		var ct = _statsCts.Token;

		bool showRecordingB = _selectedRecordings.Count == 2;
		bool containsRecordingB = StatisticsTreeGrid.Columns.Contains(StatisticsRecordingBColumn);

		if (showRecordingB && !containsRecordingB)
			StatisticsTreeGrid.Columns.Add(StatisticsRecordingBColumn);
		else if (!showRecordingB && containsRecordingB)
			StatisticsTreeGrid.Columns.Remove(StatisticsRecordingBColumn);

		if (_selectedRecordings.Count == 0)
		{
			ViewModel.StatisticsRows.Clear();
			ViewModel.RecordingAHeader = "Recording A";
			ViewModel.RecordingBHeader = "Recording B";
			return;
		}

		if (_selectedRecordings.Count > 2)
		{
			ViewModel.StatisticsRows.Clear();
			ViewModel.RecordingAHeader = "Recording A";
			ViewModel.RecordingBHeader = "Recording B";
			return;
		}

		ViewModel.RecordingAHeader = _selectedRecordings.Count >= 1 ? _selectedRecordings[0].Title : "Recording A";
		ViewModel.RecordingBHeader = _selectedRecordings.Count >= 2 ? _selectedRecordings[1].Title : "Recording B";

		var selected = _selectedRecordings;

		try
		{
			var builtRows = await Task.Run(() =>
			{
				ct.ThrowIfCancellationRequested();
				List<(RecordingItem item, DateTime lastWriteUtc)> loaded = [];
				List<ResultRow> resultRows = [];

				foreach (var i in selected.Take(2))
				{
					var lastWriteUtc = File.Exists(i.FilePath)
						? File.GetLastWriteTimeUtc(i.FilePath)
						: DateTime.MinValue;
					if (lastWriteUtc != DateTime.MinValue)
						loaded.Add((i, lastWriteUtc));
				}

				if (loaded.Count == 0)
					return resultRows;

				void AddStatsRows(string prefix, string column, bool isFps)
				{
					Metrics[] m = new Metrics[loaded.Count];
					for (int i = 0; i < loaded.Count; i++)
					{
						if (!LoadMetricColumn(loaded[i].item.FilePath, loaded[i].lastWriteUtc, column, out var values))
							return;
						var array = isFps ? values.Where(v => v > 0).Select(v => 1000.0 / v).ToArray() : [.. values];
						if (array.Length == 0)
							return;
						m[i] = BenchmarkStatistics.CalculateMetrics(array, isFpsMetric: isFps);
					}

					foreach (var label in BenchmarkCsv.MetricLabels)
					{
						double av = BenchmarkCsv.NumericMetric(m[0], label);
						string a = av.ToString(isFps ? "0.###" : "0.####", CultureInfo.InvariantCulture) + (isFps ? " FPS" : " ms");
						string b = loaded.Count < 2 ? "" : BenchmarkCsv.NumericMetric(m[1], label).ToString(isFps ? "0.###" : "0.####", CultureInfo.InvariantCulture) + (isFps ? " FPS" : " ms");
						resultRows.Add(new ResultRow
						{
							Metric = $"{prefix} {label} FPS",
							RecordingA = a,
							RecordingB = b
						});
					}

					string fmtPacing(double value, string format) => value == 0 ? "\u2014" : value.ToString(format, CultureInfo.InvariantCulture);

					resultRows.Add(new ResultRow
					{
						Metric = $"{prefix} Standard Deviation (STDEV)",
						RecordingA = fmtPacing(m[0].StdDev, "0.###"),
						RecordingB = loaded.Count < 2 ? "" : fmtPacing(m[1].StdDev, "0.###")
					});
					resultRows.Add(new ResultRow
					{
						Metric = $"{prefix} Coefficient of Variation (CV)",
						RecordingA = fmtPacing(m[0].Cv, "0.#####"),
						RecordingB = loaded.Count < 2 ? "" : fmtPacing(m[1].Cv, "0.#####")
					});
				}

				AddStatsRows("Displayed", "MsBetweenDisplayChange", isFps: true);
				AddStatsRows("Rendered", "MsBetweenPresents", isFps: true);

				void AddMsStats(string prefix, string column)
				{
					Metrics[] m = new Metrics[loaded.Count];
					for (int i = 0; i < loaded.Count; i++)
					{
						if (!LoadMetricColumn(loaded[i].item.FilePath, loaded[i].lastWriteUtc, column, out var values))
							return;
						double[] array = [.. values];
						if (array.Length == 0)
							return;
						m[i] = BenchmarkStatistics.CalculateMetrics(array, isFpsMetric: false);
					}

					string fmtMs(double v) => v.ToString("0.####", CultureInfo.InvariantCulture) + " ms";
					string fmtSd(double v) => v == 0 ? "\u2014" : v.ToString("0.####", CultureInfo.InvariantCulture) + " ms";
					string fmtRel(double v) => v == 0 ? "\u2014" : v.ToString("0.#####", CultureInfo.InvariantCulture);

					resultRows.Add(new ResultRow
					{
						Metric = $"{prefix} Average (Arithmetic)",
						RecordingA = fmtMs(m[0].AvgArithmetic),
						RecordingB = loaded.Count < 2 ? "" : fmtMs(m[1].AvgArithmetic)
					});
					resultRows.Add(new ResultRow
					{
						Metric = $"{prefix} P50 (Median)",
						RecordingA = fmtMs(m[0].P50Median),
						RecordingB = loaded.Count < 2 ? "" : fmtMs(m[1].P50Median)
					});
					resultRows.Add(new ResultRow
					{
						Metric = $"{prefix} P95",
						RecordingA = fmtMs(m[0].P5),
						RecordingB = loaded.Count < 2 ? "" : fmtMs(m[1].P5)
					});
					resultRows.Add(new ResultRow
					{
						Metric = $"{prefix} P99",
						RecordingA = fmtMs(m[0].P1),
						RecordingB = loaded.Count < 2 ? "" : fmtMs(m[1].P1)
					});
					resultRows.Add(new ResultRow
					{
						Metric = $"{prefix} P99.9",
						RecordingA = fmtMs(m[0].P01),
						RecordingB = loaded.Count < 2 ? "" : fmtMs(m[1].P01)
					});
					resultRows.Add(new ResultRow
					{
						Metric = $"{prefix} Maximum",
						RecordingA = fmtMs(m[0].Max),
						RecordingB = loaded.Count < 2 ? "" : fmtMs(m[1].Max)
					});
					resultRows.Add(new ResultRow
					{
						Metric = $"{prefix} Minimum",
						RecordingA = fmtMs(m[0].Min),
						RecordingB = loaded.Count < 2 ? "" : fmtMs(m[1].Min)
					});

					string fmtPct(double v) => v == 0 ? "\u2014" : v.ToString("0.0") + "%";
					string aRmssdPct = m[0].AvgArithmetic != 0
						? fmtPct(m[0].Rmssd / m[0].AvgArithmetic * 100) : "\u2014";
					string bRmssdPct = loaded.Count < 2 ? ""
						: (m[1].AvgArithmetic != 0
							? fmtPct(m[1].Rmssd / m[1].AvgArithmetic * 100) : "\u2014");
					resultRows.Add(new ResultRow
					{
						Metric = $"{prefix} Root mean square of successive differences (RMSSD)",
						RecordingA = aRmssdPct,
						RecordingB = bRmssdPct
					});

					resultRows.Add(new ResultRow
					{
						Metric = $"{prefix} Stepwise-Relative",
						RecordingA = fmtRel(m[0].StepwiseRelSD),
						RecordingB = loaded.Count < 2 ? "" : fmtRel(m[1].StepwiseRelSD)
					});
					resultRows.Add(new ResultRow
					{
						Metric = $"{prefix} Standard Deviation (STDEV)",
						RecordingA = fmtSd(m[0].StdDev),
						RecordingB = loaded.Count < 2 ? "" : fmtSd(m[1].StdDev)
					});
					resultRows.Add(new ResultRow
					{
						Metric = $"{prefix} Coefficient of Variation (CV)",
						RecordingA = fmtRel(m[0].Cv),
						RecordingB = loaded.Count < 2 ? "" : fmtRel(m[1].Cv)
					});
				}

				AddMsStats("MsBetweenDisplayChange", "MsBetweenDisplayChange");
				AddMsStats("MsBetweenPresents", "MsBetweenPresents");
				AddMsStats("MsGPUBusy", "MsGPUBusy");
				AddMsStats("MsUntilDisplayed", "MsUntilDisplayed");

				ApplyResultComparisons(resultRows, loaded.Count == 2);
				return GroupResultRows(resultRows);
			}, ct);

		if (builtRows.Count == 0)
		{
			ViewModel.StatisticsRows = [];
			return;
		}

		ViewModel.StatisticsRows = [.. builtRows];
		StatisticsTreeGrid.ExpandAllNodes();
		}
		catch (OperationCanceledException)
		{
		}
	}

	private static void ApplyResultComparisons(IEnumerable<ResultRow> rows, bool comparisonEnabled)
	{
		if (!comparisonEnabled)
			return;
		foreach (var row in rows)
		{
			if (!TryParseNumeric(row.RecordingA, out double recordingA) ||
				!TryParseNumeric(row.RecordingB, out double recordingB) ||
				recordingA == recordingB)
			{
				continue;
			}
			bool higherIsBetter = row.Metric.EndsWith(" FPS", StringComparison.Ordinal);
			bool recordingAIsBetter = higherIsBetter ? recordingA > recordingB : recordingA < recordingB;
			row.RecordingAComparison = recordingAIsBetter ? ResultComparison.Better : ResultComparison.Worse;
			row.RecordingBComparison = recordingAIsBetter ? ResultComparison.Worse : ResultComparison.Better;
		}
	}

	private static bool TryParseNumeric(string value, out double result)
	{
		if (string.IsNullOrEmpty(value))
		{
			result = 0;
			return false;
		}
		var trimmed = value.AsSpan();
		if (trimmed.EndsWith(" FPS".AsSpan(), StringComparison.Ordinal))
			trimmed = trimmed[..^4];
		else if (trimmed.EndsWith(" ms".AsSpan(), StringComparison.Ordinal))
			trimmed = trimmed[..^3];
		else if (trimmed.EndsWith("%".AsSpan(), StringComparison.Ordinal))
			trimmed = trimmed[..^1];
		return double.TryParse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture, out result);
	}

	private static List<ResultRow> GroupResultRows(IEnumerable<ResultRow> rows)
	{
		List<ResultRow> groups = [];
		var groupLookup = new Dictionary<string, ResultRow>(StringComparer.Ordinal);
		foreach (var row in rows)
		{
			var (groupName, childLabel) = GetResultGroup(row.Metric);
			if (!groupLookup.TryGetValue(groupName, out var group))
			{
				group = new ResultRow
				{
					Metric = groupName,
					Tooltip = GetResultTooltip(groupName, string.Empty)
				};
				groupLookup[groupName] = group;
				groups.Add(group);
			}
			group.Children.Add(new ResultRow
			{
				Metric = childLabel,
				Tooltip = GetResultTooltip(groupName, childLabel),
				RecordingA = row.RecordingA,
				RecordingB = row.RecordingB,
				RecordingAComparison = row.RecordingAComparison,
				RecordingBComparison = row.RecordingBComparison
			});
		}
		return groups;
	}

	private static (string Group, string ChildLabel) GetResultGroup(string label)
	{
		if (label.StartsWith("Displayed ", StringComparison.Ordinal))
			return ("Displayed FPS", label["Displayed ".Length..]);
		if (label.StartsWith("Rendered ", StringComparison.Ordinal))
			return ("Rendered FPS", label["Rendered ".Length..]);
		if (label.Equals("Average MsBetweenDisplayChange", StringComparison.Ordinal))
			return ("MsBetweenDisplayChange", "Average");
		if (label.StartsWith("MsBetweenDisplayChange ", StringComparison.Ordinal))
			return ("MsBetweenDisplayChange", label["MsBetweenDisplayChange ".Length..]);
		if (label.Equals("Average MsBetweenPresents", StringComparison.Ordinal))
			return ("MsBetweenPresents", "Average");
		if (label.StartsWith("MsBetweenPresents ", StringComparison.Ordinal))
			return ("MsBetweenPresents", label["MsBetweenPresents ".Length..]);
		if (label.Equals("Average MsGPUBusy", StringComparison.Ordinal))
			return ("MsGPUBusy", "Average");
		if (label.StartsWith("MsGPUBusy ", StringComparison.Ordinal))
			return ("MsGPUBusy", label["MsGPUBusy ".Length..]);
		if (label.Equals("Average MsUntilDisplayed", StringComparison.Ordinal))
			return ("MsUntilDisplayed", "Average");
		if (label.StartsWith("MsUntilDisplayed ", StringComparison.Ordinal))
			return ("MsUntilDisplayed", label["MsUntilDisplayed ".Length..]);
		return ("Other", label);
	}

	private static string GetResultTooltip(string group, string metric)
	{
		if (string.IsNullOrEmpty(metric))
		{
			return group switch
			{
				"Rendered FPS" => "Measures how fast the game creates frames before they are sent to your screen.",
				"Displayed FPS" => "Measures how fast frames actually change on your screen.",
				"MsBetweenPresents" => "The time it takes the game engine to push out each new frame.",
				"MsBetweenDisplayChange" => "The time it takes for a new image to physically appear on your screen.",
				"MsGPUBusy" => "How long the graphics card works on a single frame.",
				"MsUntilDisplayed" => "The delay between the game finishing a frame and it appearing on screen.",
				_ => "Benchmark statistic."
			};
		}

		if (metric.StartsWith("0.1% Low Avg", StringComparison.Ordinal))
			return "Average FPS across the worst-performing 0.1% of frames. Higher values indicate smoother performance.";
		if (metric.StartsWith("1% Low Avg", StringComparison.Ordinal))
			return "Average FPS across the worst-performing 1% of frames. Higher values indicate smoother performance";
		if (metric.StartsWith("Average (Arithmetic)", StringComparison.Ordinal))
		{
			if (metric.EndsWith(" FPS", StringComparison.Ordinal))
				return "Conventional average FPS. Every sampled frame contributes equally.";
			return "Conventional average frametime. Every sampled frame contributes equally.";
		}
		if (metric.StartsWith("Average (Harmonic)", StringComparison.Ordinal))
		{
			if (metric.EndsWith(" FPS", StringComparison.Ordinal))
				return "Frame-duration-weighted average FPS. Long, low-FPS frames have more influence, making lag spikes more visible.";
			return "Frame-duration-weighted average frametime. Long, slow frames have more influence, making spikes more visible.";
		}
		if (metric.StartsWith("Minimum", StringComparison.Ordinal))
		{
			if (metric.EndsWith(" FPS", StringComparison.Ordinal))
				return "Lowest sampled FPS value in the recording.";
			return "Shortest single frametime in the recording.";
		}
		if (metric.StartsWith("Maximum", StringComparison.Ordinal))
		{
			if (metric.EndsWith(" FPS", StringComparison.Ordinal))
				return "Highest sampled FPS value in the recording.";
			return "Longest single frametime in the recording.";
		}
		if (metric.StartsWith("P0.1", StringComparison.Ordinal))
			return "FPS threshold containing the bottom 0.1% of sampled frames.";
		if (metric.StartsWith("P1", StringComparison.Ordinal))
			return "FPS threshold containing the bottom 1% of sampled frames.";
		if (metric.StartsWith("P50", StringComparison.Ordinal))
		{
			if (metric.EndsWith(" FPS", StringComparison.Ordinal))
				return "FPS threshold containing 50% of the sampled frames.";
			return "Frametime threshold below which 50% of all frames fall. Represents typical frame duration.";
		}
		if (metric.StartsWith("P5", StringComparison.Ordinal))
			return "FPS threshold containing the bottom 5% of sampled frames.";
		if (metric.StartsWith("P95", StringComparison.Ordinal))
		{
			if (metric.EndsWith(" FPS", StringComparison.Ordinal))
				return "FPS threshold containing 95% of the sampled frames.";
			return "Frametime threshold below which 95% of all frames fall. Captures moderate spikes.";
		}
		if (metric.StartsWith("P99.", StringComparison.Ordinal))
			return "Frametime threshold below which 99.9% of all frames fall. Captures extreme peak delays.";
		if (metric.StartsWith("P99", StringComparison.Ordinal))
		{
			if (metric.EndsWith(" FPS", StringComparison.Ordinal))
				return "FPS threshold containing 99% of the sampled frames.";
			return "Frametime threshold below which 99% of all frames fall. Captures severe hitches.";
		}
		if (metric.StartsWith("Standard Deviation", StringComparison.Ordinal))
		{
			if (group is "Displayed FPS" or "Rendered FPS")
				return "Standard deviation: Measures how widely FPS values are spread around the average. Lower values indicate more consistent frame rates.";
			return "Standard deviation: Measures how far individual frametimes typically stray from the average frametime. Lower values indicate more consistent performance. Measured in ms.";
		}
		if (metric.StartsWith("Coefficient of Variation", StringComparison.Ordinal))
		{
			if (group is "Displayed FPS" or "Rendered FPS")
				return "Coefficient of variation: Standard deviation divided by the arithmetic mean (StDev / Avg). Useful for comparing FPS consistency across different performance levels. Lower values indicate more stable FPS.";
			return "Coefficient of variation: Standard deviation divided by the arithmetic mean (StDev / Avg). Useful for comparing stutter severity across different performance levels. Lower values indicate better frametime stability";
		}
		if (metric.StartsWith("Root mean square", StringComparison.Ordinal))
			return "Root mean square of successive differences: Measures frame pacing by comparing the timing of adjacent frames. Lower values indicate smoother frame pacing. Measured in ms.";
		if (metric.StartsWith("Stepwise-Relative", StringComparison.Ordinal))
			return "Typical percentage change in rendering time from one frame to the next. Lower values indicate lower spike severity.";
		return "Benchmark statistic.";
	}

	private static bool GetHeaderIndex(string filePath, DateTime lastWriteUtc, out Dictionary<string, int> headerIndex)
	{
		headerIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

		try
		{
			using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
			using var reader = new StreamReader(fs);
			var headerLine = reader.ReadLine();
			if (string.IsNullOrWhiteSpace(headerLine))
				return false;
			var headers = BenchmarkCsv.ParseCsvLine(headerLine);
			if (headers.Count == 0)
				return false;
			for (int i = 0; i < headers.Count; i++)
			{
				var h = headers[i].Trim();
				if (string.IsNullOrEmpty(h))
					continue;
				headerIndex[h] = i;
				if (string.Equals(h, "Render Queue Depth", StringComparison.OrdinalIgnoreCase))
					headerIndex["Render Queue Depth (RQD)"] = i;
				if (string.Equals(h, "Render Queue Depth (RQD)", StringComparison.OrdinalIgnoreCase))
					headerIndex["Render Queue Depth"] = i;
			}
			return true;
		}
		catch
		{
			return false;
		}
	}

	private static bool LoadMetricColumn(string filePath, DateTime lastWriteUtc, string metric, out List<double> values)
	{
		values = [];
		if (!GetHeaderIndex(filePath, lastWriteUtc, out var headerIndex))
			return false;
		if (!headerIndex.TryGetValue(metric, out int idx))
			return false;

		try
		{
			using var fs1 = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
			using var reader = new StreamReader(fs1);
			reader.ReadLine();
			var list = new List<double>(capacity: 4096);
			while (!reader.EndOfStream)
			{
				var line = reader.ReadLine();
				if (string.IsNullOrWhiteSpace(line))
					continue;
				var cols = BenchmarkCsv.ParseCsvLine(line);
				if (idx < 0 || idx >= cols.Count)
					continue;
				if (double.TryParse(cols[idx], NumberStyles.Float, CultureInfo.InvariantCulture, out var v))
					list.Add(v);
			}
			values = list;
			return list.Count > 0;
		}
		catch
		{
			return false;
		}
	}

	private static bool LoadAnalysisColumns(string filePath, DateTime lastWriteUtc)
	{
		if (!GetHeaderIndex(filePath, lastWriteUtc, out var headerIndex))
			return false;
		string[] metrics = ["MsBetweenDisplayChange", "MsBetweenPresents", "MsGPUBusy", "MsUntilDisplayed"];
		List<(string Metric, int Index, List<double> Values)> columns = [];
		foreach (string metric in metrics)
		{
			if (headerIndex.TryGetValue(metric, out int index))
				columns.Add((metric, index, new List<double>(4096)));
		}
		if (columns.Count == 0)
			return false;

		using var fs2 = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
		using var reader = new StreamReader(fs2);
		reader.ReadLine();
		while (!reader.EndOfStream)
		{
			string line = reader.ReadLine();
			if (string.IsNullOrWhiteSpace(line))
				continue;
			List<string> values = BenchmarkCsv.ParseCsvLine(line);
			for (int index = 0; index < columns.Count; index++)
			{
				var column = columns[index];
				if (column.Index < values.Count &&
					double.TryParse(values[column.Index], NumberStyles.Float, CultureInfo.InvariantCulture, out double value))
					column.Values.Add(value);
			}
		}
		return true;
	}

	private static (string Process, string PresentationMode, double DurationSeconds, List<string> SourceFileNames) LoadRecordingMetadata(string filePath, FileInfo info)
	{
		string process = Path.GetFileNameWithoutExtension(info.Name);
		string presentationMode = string.Empty;
		double durationSeconds = Math.Max(0, (info.LastWriteTime - info.CreationTime).TotalSeconds);
		List<string> sourceFileNames = [];
		using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
		using var reader = new StreamReader(fs);
		var headerLine = reader.ReadLine();
		var firstLine = reader.ReadLine();
		if (string.IsNullOrWhiteSpace(headerLine) || string.IsNullOrWhiteSpace(firstLine))
			return (process, presentationMode, durationSeconds, sourceFileNames);
		var headers = BenchmarkCsv.ParseCsvLine(headerLine);
		var firstValues = BenchmarkCsv.ParseCsvLine(firstLine);
		string lastLine = firstLine;
		const int tailBytes = 64 * 1024;
		using var tailStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
		long tailStart = Math.Max(0, tailStream.Length - tailBytes);
		tailStream.Seek(tailStart, SeekOrigin.Begin);
		using var tailReader = new StreamReader(tailStream);
		if (tailStart > 0)
			tailReader.ReadLine();
		while (!tailReader.EndOfStream)
		{
			var line = tailReader.ReadLine();
			if (!string.IsNullOrWhiteSpace(line))
				lastLine = line;
		}
		var lastValues = BenchmarkCsv.ParseCsvLine(lastLine);
		int applicationIndex = headers.FindIndex(h => string.Equals(h, "Application", StringComparison.OrdinalIgnoreCase));
		if (applicationIndex >= 0 && applicationIndex < firstValues.Count && !string.IsNullOrWhiteSpace(firstValues[applicationIndex]))
			process = firstValues[applicationIndex];
		int presentModeIndex = headers.FindIndex(header => string.Equals(header, "PresentMode", StringComparison.OrdinalIgnoreCase));
		if (presentModeIndex >= 0 && presentModeIndex < firstValues.Count && !string.IsNullOrWhiteSpace(firstValues[presentModeIndex]))
			presentationMode = firstValues[presentModeIndex];
		bool hasCsvDuration = false;
		int aggregateDurationIndex = headers.FindIndex(h => string.Equals(h, "AutoOSAggregateDurationSeconds", StringComparison.OrdinalIgnoreCase));
		if (aggregateDurationIndex >= 0 && aggregateDurationIndex < firstValues.Count &&
			double.TryParse(firstValues[aggregateDurationIndex], NumberStyles.Float, CultureInfo.InvariantCulture, out double aggregateDuration))
		{
			durationSeconds = Math.Max(0, aggregateDuration);
			hasCsvDuration = true;
		}
		int aggregateSourcesIndex = headers.FindIndex(h => string.Equals(h, "AutoOSAggregateSources", StringComparison.OrdinalIgnoreCase));
		if (aggregateSourcesIndex >= 0 && aggregateSourcesIndex < firstValues.Count && !string.IsNullOrWhiteSpace(firstValues[aggregateSourcesIndex]))
		{
			byte[] sourceJson = Convert.FromBase64String(firstValues[aggregateSourcesIndex]);
			sourceFileNames = [.. JsonSerializer.Deserialize(sourceJson, BenchmarksJsonContext.Default.ListString) ?? []];
		}
		int dateTimeIndex = headers.FindIndex(h => string.Equals(h, "TimeInDateTime", StringComparison.OrdinalIgnoreCase));
		if (!hasCsvDuration && dateTimeIndex >= 0 && dateTimeIndex < firstValues.Count && dateTimeIndex < lastValues.Count &&
			DateTime.TryParse(firstValues[dateTimeIndex], CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var start) &&
			DateTime.TryParse(lastValues[dateTimeIndex], CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var end))
		{
			durationSeconds = Math.Max(0, (end - start).TotalSeconds);
			hasCsvDuration = true;
		}
		int timeSecondsIndex = headers.FindIndex(h => string.Equals(h, "TimeInSeconds", StringComparison.OrdinalIgnoreCase));
		if (!hasCsvDuration && timeSecondsIndex >= 0 && timeSecondsIndex < firstValues.Count && timeSecondsIndex < lastValues.Count &&
			double.TryParse(firstValues[timeSecondsIndex], NumberStyles.Float, CultureInfo.InvariantCulture, out double firstTimeSeconds) &&
			double.TryParse(lastValues[timeSecondsIndex], NumberStyles.Float, CultureInfo.InvariantCulture, out double lastTimeSeconds))
		{
			durationSeconds = Math.Max(0, lastTimeSeconds - firstTimeSeconds);
		}
		return (process, presentationMode, durationSeconds, sourceFileNames);
	}

	private List<RecordingItem> GetSelectedRecordings()
	{
		if (RecordingsTreeGrid is null)
			return [];

		List<RecordingItem> selected = RecordingsTreeGrid.SelectedItems is null ? [] : [.. RecordingsTreeGrid.SelectedItems.OfType<RecordingItem>()];

		if (selected.Count == 0 && RecordingsTreeGrid.SelectedItem is RecordingItem item)
			selected.Add(item);

		selected = [.. selected.DistinctBy(
			recording => recording.FilePath, StringComparer.OrdinalIgnoreCase)];

		HashSet<string> selectedPaths = selected
			.Select(recording => recording.FilePath)
			.ToHashSet(StringComparer.OrdinalIgnoreCase);

		List<RecordingItem> normalizedSelection = [];
		foreach (RecordingItem recording in selected)
		{
			bool hasSelectedDescendant = false;
			HashSet<RecordingItem> visited = [];
			Stack<RecordingItem> descendants = new(recording.Children);
			while (descendants.TryPop(out RecordingItem descendant) && visited.Add(descendant))
			{
				if (selectedPaths.Contains(descendant.FilePath))
				{
					hasSelectedDescendant = true;
					break;
				}
				foreach (RecordingItem child in descendant.Children)
					descendants.Push(child);
			}
			if (!hasSelectedDescendant)
				normalizedSelection.Add(recording);
		}
		return normalizedSelection;
	}
}
