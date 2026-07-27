using System.Diagnostics;
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
using Syncfusion.UI.Xaml.DataGrid;
using Microsoft.UI.Xaml.Input;

namespace AutoOS.Views.Settings;

public sealed partial class BenchmarksPage : Page
{
	public BenchmarksPageViewModel ViewModel { get; } = new();
	private static readonly string RecordingsDirectory = Path.Combine(PathHelper.GetAppDataFolderPath(), "Benchmarks");
	private sealed record AnalysisModel(List<(string recordingName, List<SeriesPoint> points)> MetricSeries, List<(string recordingName, Metrics displayedStats, Metrics renderedStats)> FpsStatsSeries);
	private sealed record ChartPresentation(List<BarPoint> DisplayedFpsBars1, List<BarPoint> RenderedFpsBars1, List<BarPoint> DisplayedFpsBars2, List<BarPoint> RenderedFpsBars2, bool ShowRenderedFps1, string DisplayedFpsLabel1, string RenderedFpsLabel1, string DisplayedFpsLabel2, string RenderedFpsLabel2, List<SeriesPoint> MetricPts1, List<SeriesPoint> MetricPts2, string MetricLabel1, string MetricLabel2, string FpsYAxisLabel, string FpsLabelFormat, string MetricYAxisLabel);
	private sealed record CachedRecordingMetadata(long Length, DateTime LastWriteTimeUtc, string Process, string PresentationMode, double DurationSeconds, List<string> SourceFileNames);
	private static readonly Lock RecordingMetadataCacheLock = new();
	private static readonly Dictionary<string, CachedRecordingMetadata> RecordingMetadataCache = new(StringComparer.OrdinalIgnoreCase);
	private readonly PresentMonRecorder _recorder = new();
	private List<RecordingItem> _selectedRecordings = [];
	private ChartPresentation _lastChartPresentation;
	private CancellationTokenSource _statsCts = new();
	private CancellationTokenSource _processRefreshCts;
	private volatile bool _isInitialProcessRefresh;
	private GlobalKeyboardHook _globalKeyboardHook;
	private VirtualKeyModifiers _currentModifiers = VirtualKeyModifiers.Shift;
	private VirtualKey _currentKey = VirtualKey.F11;

	internal PresentMonProcessDiscovery PresentingProcesses { get; } = new();

	public BenchmarksPage()
	{
		InitializeComponent();
		ProcessAutoSuggestBox.AddHandler(
			PointerPressedEvent,
			new PointerEventHandler(ProcessAutoSuggestBox_PointerPressed),
			handledEventsToo: true);
		ProcessAutoSuggestBox.RegisterPropertyChangedCallback(
			AutoSuggestBox.IsSuggestionListOpenProperty,
			ProcessAutoSuggestBox_IsSuggestionListOpenChanged);
		LoadRecordings();
	}

	protected override void OnNavigatedTo(NavigationEventArgs e)
	{
		base.OnNavigatedTo(e);
		_globalKeyboardHook = new GlobalKeyboardHook();
		_globalKeyboardHook.KeyDown += OnGlobalKeyDown;
		_globalKeyboardHook.Start();
		PresentingProcesses.ProcessesChanged += PresentingProcesses_ProcessesChanged;
		ViewModel.MetricToggled += OnMetricToggled;
	}

	protected override void OnNavigatedFrom(NavigationEventArgs e)
	{
		base.OnNavigatedFrom(e);
		StopProcessDiscovery();
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
		List<RecordingItem> recordings = DiscoverRecordings();
		ViewModel.SetRecordings(recordings);
		_selectedRecordings = GetSelectedRecordings();
		ViewModel.SetSelectedRecordings(_selectedRecordings);
	}

	private static List<RecordingItem> DiscoverRecordings()
	{
		List<RecordingItem> recordings = [];
		Dictionary<RecordingItem, List<string>> aggregateSources = [];

		if (!Directory.Exists(RecordingsDirectory))
		{
			Directory.CreateDirectory(RecordingsDirectory);
			return recordings;
		}

		List<FileInfo> csvFiles = [.. new DirectoryInfo(RecordingsDirectory)
			.EnumerateFiles("*.csv")
			.OrderByDescending(file => file.LastWriteTime)];

		if (csvFiles.Count > 0)
		{
			var loadedRecordings = csvFiles
				.AsParallel()
				.AsOrdered()
				.Select(info =>
				{
					var (process, presentationMode, durationSeconds, sourceFileNames) = LoadRecordingMetadataCached(info);
					return (Recording: new RecordingItem
					{
						FilePath = info.FullName,
						FileName = info.Name,
						Title = Path.GetFileNameWithoutExtension(info.Name),
						Process = process,
						PresentationMode = presentationMode,
						DurationSeconds = durationSeconds,
						Date = info.LastWriteTime,
						Time = info.LastWriteTime.TimeOfDay
					}, SourceFileNames: sourceFileNames);
				})
				.ToList();

			foreach (var (recording, sourceFileNames) in loadedRecordings)
			{
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
			return [.. recordings.Where(recording => !childRecordings.Contains(recording))];
		}

		return recordings;
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

		RebuildCharts(_selectedRecordings);
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
			if (delay > 0)
				ViewModel.ShowRecordingCountdown(delay);
			else
				ViewModel.ShowRecording();

			Task<PresentMonRecordingResult> recordingTask = _recorder.RecordAsync(presentMonPath, RecordingsDirectory, processName, duration, delay);
			if (delay > 0)
			{
				var delayTimer = Stopwatch.StartNew();
				while (!recordingTask.IsCompleted && delayTimer.Elapsed < TimeSpan.FromSeconds(delay))
				{
					int remainingSeconds = Math.Max(1, (int)Math.Ceiling(delay - delayTimer.Elapsed.TotalSeconds));
					ViewModel.ShowRecordingCountdown(remainingSeconds);
					await Task.WhenAny(recordingTask, Task.Delay(100));
				}
				if (!recordingTask.IsCompleted)
					ViewModel.ShowRecording();
			}
			recordingResult = await recordingTask;
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

		int aggregateDurationIndex = headerCols.FindIndex(header => string.Equals(header, "AutoOSAggregateDurationSeconds", StringComparison.OrdinalIgnoreCase));
		if (aggregateDurationIndex < 0)
		{
			aggregateDurationIndex = headerCols.Count;
			headerCols.Add("AutoOSAggregateDurationSeconds");
		}

		int aggregateSourcesIndex = headerCols.FindIndex(header => string.Equals(header, "AutoOSAggregateSources", StringComparison.OrdinalIgnoreCase));
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
		var metric = (Metric1ComboBox.SelectedItem as ComboBoxItem)?.Content as string ?? string.Empty;
		var presentation = BuildChartData(_selectedRecordings, metric);
		if (presentation != null)
			BindLineScatterChart(presentation);
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

	private async void ProcessAutoSuggestBox_PointerPressed(object sender, PointerRoutedEventArgs e)
	{
		if (ProcessAutoSuggestBox.IsSuggestionListOpen || _processRefreshCts != null)
			return;

		StopProcessDiscovery();
		_processRefreshCts = new CancellationTokenSource();
		_isInitialProcessRefresh = true;
		CancellationToken cancellationToken = _processRefreshCts.Token;

		try
		{
			List<string> processes = await Task.Run(() =>
			{
				PresentingProcesses.Start();
				return PresentingProcesses.GetRecordableProcesses(refreshRunningProcesses: true);
			}, cancellationToken);
			if (!cancellationToken.IsCancellationRequested && ViewModel.ActiveTab == "Recordings")
			{
				ViewModel.SetRecordableProcesses(processes);
				ProcessAutoSuggestBox.IsSuggestionListOpen = ViewModel.ProcessSuggestions.Count > 0;
				if (!ProcessAutoSuggestBox.IsSuggestionListOpen)
					StopProcessDiscovery();
			}
		}
		catch (OperationCanceledException)
		{
		}
		finally
		{
			_isInitialProcessRefresh = false;
		}
	}

	private void ProcessAutoSuggestBox_IsSuggestionListOpenChanged(DependencyObject sender, DependencyProperty dp)
	{
		if (!ProcessAutoSuggestBox.IsSuggestionListOpen)
			StopProcessDiscovery();
	}

	private void StopProcessDiscovery()
	{
		_processRefreshCts?.Cancel();
		_processRefreshCts?.Dispose();
		_processRefreshCts = null;
		PresentingProcesses.Dispose();
	}

	private void PresentingProcesses_ProcessesChanged(object sender, EventArgs e)
	{
		if (_isInitialProcessRefresh)
			return;

		DispatcherQueue.TryEnqueue(() =>
		{
			if (ViewModel.ActiveTab == "Recordings" && ProcessAutoSuggestBox.IsSuggestionListOpen)
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

	private void BenchmarksSelectorBar_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
	{
		var selectedItem = args.SelectedItem ?? sender.SelectedItem;

		if (ReferenceEquals(selectedItem, RecordingsTab))
		{
			ViewModel.ActiveTab = "Recordings";
		}
		else if (ReferenceEquals(selectedItem, AnalysisTab))
		{
			StopProcessDiscovery();
			ViewModel.ActiveTab = "Analysis";
			ViewModel.AnalysisChartType = "Bar";
			if (_selectedRecordings.Count is > 0 and <= 2)
				ReplayAnimation();
		}
		else if (ReferenceEquals(selectedItem, StatisticsTab))
		{
			StopProcessDiscovery();
			ViewModel.ActiveTab = "Statistics";
		}
	}

	private void RebuildCharts(List<RecordingItem> items)
	{
		if (items.Count is 0 or > 2)
		{
			_lastChartPresentation = null;
			return;
		}

		var metric = (Metric1ComboBox.SelectedItem as ComboBoxItem)?.Content as string ?? string.Empty;
		var presentation = BuildChartData(items, metric);
		if (presentation == null)
		{
			_lastChartPresentation = null;
			return;
		}

		BindBarColumnChart(presentation);
		BindLineScatterChart(presentation);
	}

	private static ChartPresentation BuildChartData(List<RecordingItem> items, string metric)
	{
		var results = new List<(RecordingItem item, RecordingAnalyzer.AnalysisResult analysis)>();
		foreach (var item in items)
		{
			var result = RecordingAnalyzer.Analyze(item.FilePath);
			if (result != null)
				results.Add((item, result));
		}

		if (results.Count == 0)
			return null;

		if (results.All(r => r.analysis.MsBetweenDisplayChange.Count == 0 && r.analysis.MsBetweenPresents.Count == 0))
			return null;

		string metricColumn = metric switch
		{
			"Displayed FPS" => "MsBetweenDisplayChange",
			"Rendered FPS" => "MsBetweenPresents",
			_ => metric
		};

		List<(string recordingName, List<SeriesPoint> points)> metricSeries = [];
		List<(string recordingName, Metrics displayedStats, Metrics renderedStats)> fpsStatsSeries = [];

		foreach (var (item, analysis) in results)
		{
			IReadOnlyList<double> rawValues = metricColumn switch
			{
				"MsBetweenDisplayChange" => analysis.MsBetweenDisplayChange,
				"MsBetweenPresents" => analysis.MsBetweenPresents,
				"MsGPUBusy" => analysis.MsGPUBusy,
				"MsUntilDisplayed" => analysis.MsUntilDisplayed,
				_ => []
			};

			var points = new List<SeriesPoint>(rawValues.Count);
			for (int i = 0; i < rawValues.Count; i++)
				points.Add(new SeriesPoint { Index = i + 1, Value = rawValues[i] });
			metricSeries.Add((item.FileName, points));

			fpsStatsSeries.Add((item.FileName, analysis.DisplayedFps, analysis.RenderedFps));
		}

		return ToChartPresentation(new AnalysisModel(metricSeries, fpsStatsSeries));
	}

	private static ChartPresentation ToChartPresentation(AnalysisModel model)
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

				foreach (string percentile in BenchmarkCsv.StatisticLabelsShort)
				{
					displayedTarget.Add(new BarPoint { Label = percentile, Value = BenchmarkCsv.GetStatistic(displayedStats, percentile) });
					renderedTarget.Add(new BarPoint { Label = percentile, Value = BenchmarkCsv.GetStatistic(renderedStats, percentile) });
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

		return new ChartPresentation(displayedFpsBars1, renderedFpsBars1, displayedFpsBars2, renderedFpsBars2, showRenderedFps1, displayedFpsLabel1, renderedFpsLabel1, displayedFpsLabel2, renderedFpsLabel2, metricPts1, metricPts2, metricLabel1, metricLabel2, "FPS", "0.#", "Milliseconds (ms)");
	}

	private void BindBarColumnChart(ChartPresentation presentation)
	{
		_lastChartPresentation = presentation;
		ViewModel.BarColumnChartYAxisLabel = presentation.FpsYAxisLabel;
		ViewModel.BarColumnChartLabelFormat = presentation.FpsLabelFormat;
		ViewModel.BarColumnChartDisplayedLabel1 = presentation.DisplayedFpsLabel1;
		ViewModel.BarColumnChartRenderedLabel1 = presentation.RenderedFpsLabel1;
		ViewModel.BarColumnChartDisplayedLabel2 = presentation.DisplayedFpsLabel2;
		ViewModel.BarColumnChartRenderedLabel2 = presentation.RenderedFpsLabel2;
		ViewModel.BarColumnRenderedVisible = presentation.ShowRenderedFps1;

		var series1Data = presentation.DisplayedFpsBars1.Where(b => ViewModel.IsStatisticEnabled(b.Label)).ToList();
		var series1RenderedData = presentation.RenderedFpsBars1.Where(b => ViewModel.IsStatisticEnabled(b.Label)).ToList();
		var series2Data = presentation.DisplayedFpsBars2.Where(b => ViewModel.IsStatisticEnabled(b.Label)).ToList();
		var series2RenderedData = presentation.RenderedFpsBars2.Where(b => ViewModel.IsStatisticEnabled(b.Label)).ToList();

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

	private void BindLineScatterChart(ChartPresentation presentation)
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
			BindBarColumnChart(_lastChartPresentation);
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

		try
		{
			var files = _selectedRecordings.Take(2).Select(recording => recording.FilePath).ToArray();
			var builtRows = await Task.Run(() =>
			{
				ct.ThrowIfCancellationRequested();

				var results = new RecordingAnalyzer.AnalysisResult[files.Length];
				for (int i = 0; i < files.Length; i++)
				{
					results[i] = RecordingAnalyzer.Analyze(files[i]);
					if (results[i] == null)
						return [];
				}

				List<ResultRow> rows = [];
				rows.AddRange(BuildFpsStatRows("Displayed", results, results[0]?.DisplayedFps, results.Length > 1 ? results[1]?.DisplayedFps : null));
				rows.AddRange(BuildFpsStatRows("Rendered", results, results[0]?.RenderedFps, results.Length > 1 ? results[1]?.RenderedFps : null));
				rows.AddRange(BuildLatencyStatRows("MsBetweenDisplayChange", results, r => r.MsBetweenDisplayChangeStats));
				rows.AddRange(BuildLatencyStatRows("MsBetweenPresents", results, r => r.MsBetweenPresentsStats));
				rows.AddRange(BuildLatencyStatRows("MsGPUBusy", results, r => r.MsGpuBusyStats));
				rows.AddRange(BuildLatencyStatRows("MsUntilDisplayed", results, r => r.MsUntilDisplayedStats));

				ApplyResultComparisons(rows, results.Length == 2);
				return GroupResultRows(rows);
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
		{ }
	}

	private static List<ResultRow> BuildFpsStatRows(string prefix, RecordingAnalyzer.AnalysisResult[] results, Metrics m0, Metrics m1)
	{
		if (m0 == null || m0.AvgArithmetic == 0)
			return [];

		List<ResultRow> rows = [];
		foreach (var label in BenchmarkCsv.StatisticLabels)
		{
			double av = BenchmarkCsv.GetStatistic(m0, label);
			string a = av.ToString("0.###", CultureInfo.InvariantCulture) + " FPS";
			string b = m1 == null ? "" : BenchmarkCsv.GetStatistic(m1, label).ToString("0.###", CultureInfo.InvariantCulture) + " FPS";
			rows.Add(new ResultRow
			{
				Statistic = $"{prefix} {label} FPS",
				RecordingA = a,
				RecordingB = b
			});
		}

		static string fmt(double value, string format) => value == 0 ? "\u2014" : value.ToString(format, CultureInfo.InvariantCulture);

		rows.Add(new ResultRow
		{
			Statistic = $"{prefix} Standard Deviation (STDEV)",
			RecordingA = fmt(m0.StdDev, "0.###") + " FPS",
			RecordingB = m1 == null ? "" : fmt(m1.StdDev, "0.###") + " FPS"
		});
		rows.Add(new ResultRow
		{
			Statistic = $"{prefix} Coefficient of Variation (CV)",
			RecordingA = fmt(m0.Cv, "0.#####"),
			RecordingB = m1 == null ? "" : fmt(m1.Cv, "0.#####")
		});
		return rows;
	}

	private static List<ResultRow> BuildLatencyStatRows(string prefix, RecordingAnalyzer.AnalysisResult[] results, Func<RecordingAnalyzer.AnalysisResult, Metrics> selector)
	{
		var m0 = results.Length > 0 ? selector(results[0]) : null;
		var m1 = results.Length > 1 ? selector(results[1]) : null;
		if (m0 == null || m0.AvgArithmetic == 0)
			return [];

		List<ResultRow> rows = [];

		static string fmtMs(double v) => v.ToString("0.####", CultureInfo.InvariantCulture) + " ms";
		static string fmtSd(double v) => v == 0 ? "\u2014" : v.ToString("0.####", CultureInfo.InvariantCulture) + " ms";
		static string fmtRel(double v) => v == 0 ? "\u2014" : v.ToString("0.#####", CultureInfo.InvariantCulture);

		void add(string label, string a, string b) => rows.Add(new ResultRow { Statistic = $"{prefix} {label}", RecordingA = a, RecordingB = b });

		add("Average (Arithmetic)", fmtMs(m0.AvgArithmetic), m1 == null ? "" : fmtMs(m1.AvgArithmetic));
		add("P50 (Median)", fmtMs(m0.P50Median), m1 == null ? "" : fmtMs(m1.P50Median));
		add("P95", fmtMs(m0.P5), m1 == null ? "" : fmtMs(m1.P5));
		add("P99", fmtMs(m0.P1), m1 == null ? "" : fmtMs(m1.P1));
		add("Maximum", fmtMs(m0.Max), m1 == null ? "" : fmtMs(m1.Max));
		add("Minimum", fmtMs(m0.Min), m1 == null ? "" : fmtMs(m1.Min));

		string fmtPct(double v) => v == 0 ? "\u2014" : v.ToString("0.0", CultureInfo.InvariantCulture) + "%";
		add("Root mean square of successive differences (RMSSD)", fmtMs(m0.Rmssd), m1 == null ? "" : fmtMs(m1.Rmssd));
		add("Stepwise-Relative", fmtPct(m0.StepwiseRelSD * 100), m1 == null ? "" : fmtPct(m1.StepwiseRelSD * 100));
		add("Standard Deviation (STDEV)", fmtSd(m0.StdDev), m1 == null ? "" : fmtSd(m1.StdDev));
		add("Coefficient of Variation (CV)", fmtRel(m0.Cv), m1 == null ? "" : fmtRel(m1.Cv));

		return rows;
	}

	private static void ApplyResultComparisons(IEnumerable<ResultRow> rows, bool comparisonEnabled)
	{
		if (!comparisonEnabled)
			return;

		static bool tryParse(string value, out double result)
		{
			result = 0;
			if (string.IsNullOrEmpty(value))
				return false;
			var trimmed = value.AsSpan();
			if (trimmed.EndsWith(" FPS".AsSpan(), StringComparison.Ordinal))
				trimmed = trimmed[..^4];
			else if (trimmed.EndsWith(" ms".AsSpan(), StringComparison.Ordinal))
				trimmed = trimmed[..^3];
			else if (trimmed.EndsWith("%".AsSpan(), StringComparison.Ordinal))
				trimmed = trimmed[..^1];
			return double.TryParse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture, out result);
		}

		foreach (var row in rows)
		{
			if (!tryParse(row.RecordingA, out double recordingA) || !tryParse(row.RecordingB, out double recordingB) || recordingA == recordingB)
				continue;
			bool higherIsBetter = row.Statistic.EndsWith(" FPS", StringComparison.Ordinal);
			bool recordingAIsBetter = higherIsBetter ? recordingA > recordingB : recordingA < recordingB;
			row.RecordingAComparison = recordingAIsBetter ? ResultComparison.Better : ResultComparison.Worse;
			row.RecordingBComparison = recordingAIsBetter ? ResultComparison.Worse : ResultComparison.Better;
		}
	}

	private static List<ResultRow> GroupResultRows(IEnumerable<ResultRow> rows)
	{
		static string getGroup(string metric)
		{
			if (metric.StartsWith("Displayed ", StringComparison.Ordinal))
				return "Displayed FPS";
			if (metric.StartsWith("Rendered ", StringComparison.Ordinal))
				return "Rendered FPS";
			if (metric.StartsWith("MsBetweenDisplayChange", StringComparison.Ordinal))
				return "MsBetweenDisplayChange";
			if (metric.StartsWith("MsBetweenPresents", StringComparison.Ordinal))
				return "MsBetweenPresents";
			if (metric.StartsWith("MsGPUBusy", StringComparison.Ordinal))
				return "MsGPUBusy";
			if (metric.StartsWith("MsUntilDisplayed", StringComparison.Ordinal))
				return "MsUntilDisplayed";
			return "Other";
		}

		static string getChildLabel(string metric, string group)
		{
			if (metric.StartsWith("Displayed ", StringComparison.Ordinal))
				return metric["Displayed ".Length..];
			if (metric.StartsWith("Rendered ", StringComparison.Ordinal))
				return metric["Rendered ".Length..];
			if (metric.StartsWith(group, StringComparison.Ordinal))
				return metric[group.Length..].TrimStart();
			return metric;
		}

		static string getGroupTooltip(string group) =>
			BenchmarkCsv.MetricDescriptions.TryGetValue(group, out var tip) ? tip : "Benchmark statistic.";

		List<ResultRow> groups = [];
		var groupLookup = new Dictionary<string, ResultRow>(StringComparer.Ordinal);
		foreach (var row in rows)
		{
			var groupName = getGroup(row		.Statistic);
			var childLabel = getChildLabel(row		.Statistic, groupName);
			if (!groupLookup.TryGetValue(groupName, out var group))
			{
				group = new ResultRow
				{
					Statistic = groupName,
					Tooltip = getGroupTooltip(groupName)
				};
				groupLookup[groupName] = group;
				groups.Add(group);
			}
			var lookupKey = childLabel;
			if (lookupKey.EndsWith(" FPS", StringComparison.Ordinal))
				lookupKey = lookupKey[..^4];
			else if (lookupKey.EndsWith(" (STDEV)", StringComparison.Ordinal))
				lookupKey = "Standard Deviation";
			else if (lookupKey.EndsWith(" (CV)", StringComparison.Ordinal))
				lookupKey = "Coefficient of Variation";
			var tooltip = BenchmarkCsv.StatisticDescriptions.TryGetValue(lookupKey, out var desc) ? desc : "Benchmark statistic.";
			group.Children.Add(new ResultRow
			{
				Statistic = childLabel,
				Tooltip = tooltip,
				RecordingA = row.RecordingA,
				RecordingB = row.RecordingB,
				RecordingAComparison = row.RecordingAComparison,
				RecordingBComparison = row.RecordingBComparison
			});
		}
		return groups;
	}

	private static (string Process, string PresentationMode, double DurationSeconds, List<string> SourceFileNames) LoadRecordingMetadataCached(FileInfo info)
	{
		lock (RecordingMetadataCacheLock)
		{
			if (RecordingMetadataCache.TryGetValue(info.FullName, out CachedRecordingMetadata cached) &&
				cached.Length == info.Length && cached.LastWriteTimeUtc == info.LastWriteTimeUtc)
			{
				return (cached.Process, cached.PresentationMode, cached.DurationSeconds, cached.SourceFileNames);
			}
		}

		var metadata = LoadRecordingMetadata(info.FullName, info);
		var cacheEntry = new CachedRecordingMetadata(
			info.Length,
			info.LastWriteTimeUtc,
			metadata.Process,
			metadata.PresentationMode,
			metadata.DurationSeconds,
			metadata.SourceFileNames);
		lock (RecordingMetadataCacheLock)
		{
			RecordingMetadataCache[info.FullName] = cacheEntry;
		}
		return metadata;
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
