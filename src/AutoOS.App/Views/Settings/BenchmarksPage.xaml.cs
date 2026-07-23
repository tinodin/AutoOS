using System.Globalization;
using AutoOS.Core.Helpers.Picker;
using AutoOS.Views.Settings.Benchmarks;
using Syncfusion.UI.Xaml.Charts;
using Syncfusion.UI.Xaml.DataGrid;
using System.Text.Json;
using Windows.System;
using static AutoOS.Views.Settings.Benchmarks.BenchmarkCsv;
namespace AutoOS.Views.Settings;

public sealed partial class BenchmarksPage : Page
{
	public BenchmarksViewModel ViewModel { get; } = new();

	private static readonly string RecordingsDirectory = Path.Combine(PathHelper.GetAppDataFolderPath(), "Benchmarks");
	private static readonly string[] MetricLabels = ["0.1% Low Avg", "1% Low Avg", "Average (Arithmetic)", "Average (Harmonic)", "Minimum", "Maximum", "P0.1", "P1", "P5", "P50 (Median)", "P95", "P99"];
	private const string AggregateDurationColumn = "AutoOSAggregateDurationSeconds";
	private const string AggregateSourcesColumn = "AutoOSAggregateSources";
	private GlobalKeyboardHook _globalKeyboardHook;
	private VirtualKeyModifiers _currentModifiers = VirtualKeyModifiers.Shift;
	private VirtualKey _currentKey = VirtualKey.F11;
	internal PresentMonProcessDiscovery PresentingProcesses { get; } = new();
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
	private readonly PresentMonRecorder _recorder = new();
	private sealed record CachedFile(string Path, DateTime LastWriteUtc, Dictionary<string, int> HeaderIndex);
	private readonly Dictionary<string, CachedFile> _headerCache = new(StringComparer.OrdinalIgnoreCase);
	private readonly Dictionary<(string path, DateTime lastWriteUtc, string metric), List<double>> _columnCache = [];
	private readonly Dictionary<(string path, DateTime lastWriteUtc, string metric, bool isFps), Metrics> _metricsCache = [];
	private readonly Dictionary<string, ChartPresentation> _analysisPresentationCache = new(StringComparer.OrdinalIgnoreCase);
	private readonly Lock _cacheLock = new();
	public BenchmarksPage()
	{
		InitializeComponent();
		PresentingProcesses.ProcessesChanged += PresentingProcesses_ProcessesChanged;
		ViewModel.FpsColor = Colors.DodgerBlue;
		ViewModel.FpsColor2 = Colors.Orange;
		ViewModel.MetricToggled += OnMetricToggled;
		LoadRecordings();
	}

	private void OnMetricToggled()
	{
		if (_lastChartPresentation != null && ViewModel.ActiveTab == "Analysis" && (ViewModel.AnalysisChartType is "Bar" or "Column"))
		{
			ApplyFpsChartPresentation(_lastChartPresentation);
		}
	}

	protected override void OnNavigatedTo(NavigationEventArgs e)
	{
		base.OnNavigatedTo(e);
		PresentingProcesses.Start();
		_globalKeyboardHook = new GlobalKeyboardHook();
		_globalKeyboardHook.KeyDown += OnGlobalKeyDown;
		_globalKeyboardHook.Start();
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
			else if (Enum.TryParse<VirtualKey>(keyName, ignoreCase: true, out var parsed) && parsed != VirtualKey.None)
				_currentKey = parsed;
		}
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


	private List<RecordingItem> GetSelectedRecordings()
	{
		if (RecordingsTreeGrid is null)
			return [];

		List<RecordingItem> selected = RecordingsTreeGrid.SelectedItems is null
			? []
			: [.. RecordingsTreeGrid.SelectedItems.OfType<RecordingItem>()];
		if (selected.Count == 0 && RecordingsTreeGrid.SelectedItem is RecordingItem item)
			selected.Add(item);

		selected = [.. selected.DistinctBy(recording => recording.FilePath, StringComparer.OrdinalIgnoreCase)];
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
	private void AnalysisChartTypeSelector_SelectionChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
	{
		string chartType = ReferenceEquals(sender.SelectedItem, BarChartItem)
			? "Bar"
			: ReferenceEquals(sender.SelectedItem, ColumnChartItem)
				? "Column"
				: ReferenceEquals(sender.SelectedItem, ScatterChartItem)
					? "Scatter"
					: "Line";
		if (ViewModel.AnalysisChartType == chartType)
			return;

		string oldType = ViewModel.AnalysisChartType;
		ViewModel.AnalysisChartType = chartType;
		if (ViewModel.ActiveTab != "Analysis")
			return;

		if (chartType is "Bar" or "Column")
		{
			if (_lastChartPresentation != null)
			{
				DispatcherQueue.TryEnqueue(() =>
				{
					if (_lastChartPresentation != null)
						ApplyFpsChartPresentation(_lastChartPresentation);
				});
			}
			return;
		}

		if (ViewModel.MetricSeries.Count == 0)
			return;

		List<SeriesPoint> firstSeries = [.. ViewModel.MetricSeries];
		List<SeriesPoint> secondSeries = [.. ViewModel.MetricSeries2];
		ViewModel.MetricSeries = [];
		ViewModel.MetricSeries2 = [];
		DispatcherQueue.TryEnqueue(() =>
		{
			if (ViewModel.AnalysisChartType != chartType)
				return;
			ViewModel.MetricSeries = [.. firstSeries];
			ViewModel.MetricSeries2 = [.. secondSeries];
		});
	}
	// ── Play / Record ────────────────────────────────────────────────────────
	private async void AddRecording_Click(object sender, RoutedEventArgs e)
	{
		var picker = new FilePicker(App.MainWindow)
		{
			ShowAllFilesOption = false,
			Title = "Add benchmark recordings"
		};
		picker.FileTypeChoices.Add("PresentMon recordings", ["*.csv"]);
		var files = await picker.PickMultipleFilesAsync();
		if (files.Count == 0)
			return;
		try
		{
			Directory.CreateDirectory(RecordingsDirectory);
			foreach (var file in files)
			{
				string destination = Path.Combine(RecordingsDirectory, file.Name);
				File.Copy(file.Path, destination, overwrite: false);
			}
			LoadRecordings();
		}
		catch (Exception ex)
		{
			await new ContentDialog
			{
				Title = "Recording Error",
				Content = ex.Message,
				CloseButtonText = "OK",
				XamlRoot = XamlRoot
			}.ShowAsync();
		}
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
		if (!File.Exists(presentMonPath))
		{
			await new ContentDialog
			{
				Title = "Recording Error",
				Content = $"PresentMon.exe was not found at {presentMonPath}. Place PresentMon in the benchmarks directory and try again.",
				CloseButtonText = "OK",
				XamlRoot = XamlRoot
			}.ShowAsync();
			ViewModel.IsRecording = false;
			Record.IsChecked = false;
			return;
		}
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
			await MessageBox.ShowErrorAsync(
				App.MainWindow,
				"Process either wasn't in foreground or cannot be recorded with PresentMon",
				"Recording failed");
		}
	}
	private void Record_Unchecked(object sender, RoutedEventArgs e)
	{
		if (!ViewModel.IsRecording)
			return;
		_recorder.Stop();
	}
	// ── Recordings list ──────────────────────────────────────────────────────
	private void LoadRecordings()
	{
		List<RecordingItem> recordings = [];
		Dictionary<RecordingItem, List<string>> aggregateSources = [];
		if (!Directory.Exists(RecordingsDirectory))
		{
			ViewModel.SetRecordings(recordings);
			ViewModel.SetSelectedRecordings([]);
			return;
		}
		List<string> csvFiles = [.. Directory.GetFiles(RecordingsDirectory, "*.csv")
					.OrderByDescending(File.GetLastWriteTime)];
		if (csvFiles.Count == 0)
		{
			ViewModel.SetRecordings(recordings);
		}
		else
		{
			foreach (var file in csvFiles)
			{
				var info = new FileInfo(file);
				var (process, presentationMode, durationSeconds, sourceFileNames) =
					ReadRecordingMetadata(file, info);
				var recording = new RecordingItem
				{
					FilePath = file,
					FileName = info.Name,
					Title = Path.GetFileNameWithoutExtension(info.Name),
					Process = process,
					PresentationMode = presentationMode,
					DurationSeconds = durationSeconds,
					Date = info.LastWriteTime,
					Time = info.LastWriteTime.TimeOfDay,
					FileSizeKb = info.Length / 1024.0
				};
				recordings.Add(recording);
				if (sourceFileNames.Count > 0)
					aggregateSources[recording] = sourceFileNames;
			}

			Dictionary<string, RecordingItem> recordingsByFileName = recordings.ToDictionary(
				recording => recording.FileName,
				StringComparer.OrdinalIgnoreCase);
			HashSet<RecordingItem> childRecordings = [];
			foreach (var (aggregate, sourceFileNames) in aggregateSources)
			{
				foreach (string sourceFileName in sourceFileNames.Distinct(StringComparer.OrdinalIgnoreCase))
				{
					if (recordingsByFileName.TryGetValue(sourceFileName, out RecordingItem source) &&
						!ReferenceEquals(source, aggregate))
					{
						aggregate.Children.Add(source);
						childRecordings.Add(source);
					}
				}
			}
			ViewModel.SetRecordings(recordings.Where(recording => !childRecordings.Contains(recording)));
		}
		ViewModel.SetSelectedRecordings(GetSelectedRecordings());
	}
	private static (
		string Process,
		string PresentationMode,
		double DurationSeconds,
		List<string> SourceFileNames) ReadRecordingMetadata(
		string filePath,
		FileInfo info)
	{
		string process = Path.GetFileNameWithoutExtension(info.Name);
		string presentationMode = string.Empty;
		double durationSeconds = Math.Max(0, (info.LastWriteTime - info.CreationTime).TotalSeconds);
		List<string> sourceFileNames = [];
		try
		{
			using var reader = new StreamReader(filePath);
			var headerLine = reader.ReadLine();
			var firstLine = reader.ReadLine();
			if (string.IsNullOrWhiteSpace(headerLine) || string.IsNullOrWhiteSpace(firstLine))
				return (process, presentationMode, durationSeconds, sourceFileNames);
			var headers = ParseCsvLine(headerLine);
			var firstValues = ParseCsvLine(firstLine);
			string lastLine = firstLine;
			const int tailBytes = 64 * 1024;
			using var tailStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
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
			var lastValues = ParseCsvLine(lastLine);
			int applicationIndex = headers.FindIndex(h => string.Equals(h, "Application", StringComparison.OrdinalIgnoreCase));
			if (applicationIndex >= 0 && applicationIndex < firstValues.Count && !string.IsNullOrWhiteSpace(firstValues[applicationIndex]))
				process = firstValues[applicationIndex];
			int presentModeIndex = headers.FindIndex(header =>
				string.Equals(header, "PresentMode", StringComparison.OrdinalIgnoreCase));
			if (presentModeIndex >= 0 &&
				presentModeIndex < firstValues.Count &&
				!string.IsNullOrWhiteSpace(firstValues[presentModeIndex]))
			{
				presentationMode = firstValues[presentModeIndex];
			}
			bool hasCsvDuration = false;
			int aggregateDurationIndex = headers.FindIndex(h => string.Equals(h, AggregateDurationColumn, StringComparison.OrdinalIgnoreCase));
			if (aggregateDurationIndex >= 0 && aggregateDurationIndex < firstValues.Count &&
				double.TryParse(firstValues[aggregateDurationIndex], NumberStyles.Float, CultureInfo.InvariantCulture, out double aggregateDuration))
			{
				durationSeconds = Math.Max(0, aggregateDuration);
				hasCsvDuration = true;
			}
			int aggregateSourcesIndex = headers.FindIndex(h =>
				string.Equals(h, AggregateSourcesColumn, StringComparison.OrdinalIgnoreCase));
			if (aggregateSourcesIndex >= 0 &&
				aggregateSourcesIndex < firstValues.Count &&
				!string.IsNullOrWhiteSpace(firstValues[aggregateSourcesIndex]))
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
		}
		catch
		{
		}
		return (process, presentationMode, durationSeconds, sourceFileNames);
	}
	private async void RecordingsTreeGrid_SelectionChanged(object sender, Syncfusion.UI.Xaml.Grids.GridSelectionChangedEventArgs e)
	{
		var items = GetSelectedRecordings();
		ViewModel.SetSelectedRecordings(items);
		ViewModel.ClearAnalysis();
		ViewModel.RefreshChartColors();
		ViewModel.StatisticsRows.Clear();
		if (items.Count is 0 or > 2)
			return;
		if (ViewModel.ActiveTab == "Analysis")
			await RenderAnalysisChartsForSelection(items);
		else if (ViewModel.ActiveTab == "Statistics")
			await RefreshStatisticsTable();
	}
	private async void RecordingsTreeGrid_CurrentCellEndEdit(object sender, CurrentCellEndEditEventArgs e)
	{
		if (!string.Equals(RecordingsTreeGrid.CurrentColumn?.MappingName, nameof(RecordingItem.Title), StringComparison.Ordinal))
			return;
		if (RecordingsTreeGrid.CurrentItem is not RecordingItem recording)
			return;
		string oldPath = recording.FilePath;
		string oldTitle = Path.GetFileNameWithoutExtension(oldPath);
		string requestedTitle = recording.Title?.Trim() ?? string.Empty;
		if (string.IsNullOrWhiteSpace(requestedTitle))
		{
			recording.Title = oldTitle;
			return;
		}
		if (string.Equals(requestedTitle, oldTitle, StringComparison.Ordinal))
			return;
		string safeTitle = string.Join("_",
					requestedTitle.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
		if (string.IsNullOrWhiteSpace(safeTitle))
		{
			recording.Title = oldTitle;
			return;
		}
		string newPath = Path.Combine(Path.GetDirectoryName(oldPath)!, safeTitle + ".csv");
		try
		{
			File.Move(oldPath, newPath, overwrite: false);
			recording.FilePath = newPath;
			recording.FileName = Path.GetFileName(newPath);
			recording.Title = safeTitle;
			DispatcherQueue.TryEnqueue(LoadRecordings);
		}
		catch (Exception ex)
		{
			recording.Title = oldTitle;
			await new ContentDialog
			{
				Title = "Recording Error",
				Content = $"Rename failed: {ex.Message}",
				CloseButtonText = "OK",
				XamlRoot = XamlRoot
			}.ShowAsync();
		}
	}
	private async void DeleteRecordingFlyoutItem_Click(object sender, RoutedEventArgs e)
	{
		if (sender is not MenuFlyoutItem item)
			return;
		string filePath = string.Empty;
		if (item.CommandParameter is GridRecordContextFlyoutInfo { Record: RecordingItem recording })
			filePath = recording.FilePath;
		else if (item.DataContext is GridRecordContextFlyoutInfo { Record: RecordingItem dataContextRecording })
			filePath = dataContextRecording.FilePath;
		else if (RecordingsTreeGrid.CurrentItem is RecordingItem currentRecording)
			filePath = currentRecording.FilePath;
		if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
			return;
		var dialog = new ContentDialog
		{
			Title = "Delete Recording",
			Content = $"Confirm that you want to delete {Path.GetFileName(filePath)}.",
			PrimaryButtonText = "Delete",
			CloseButtonText = "Cancel",
			DefaultButton = ContentDialogButton.Close,
			XamlRoot = XamlRoot
		};
		var result = await dialog.ShowAsync();
		if (result != ContentDialogResult.Primary)
			return;
		try
		{
			File.Delete(filePath);
			LoadRecordings();
		}
		catch (Exception ex)
		{
			await new ContentDialog
			{
				Title = "Recording Error",
				Content = $"Delete failed: {ex.Message}",
				CloseButtonText = "OK",
				XamlRoot = XamlRoot
			}.ShowAsync();
		}
	}
	// ── Aggregate ────────────────────────────────────────────────────────────
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
			var lines = await File.ReadAllLinesAsync(item.FilePath);
			if (lines.Length == 0) continue;
			var headerList = ParseCsvLine(lines[0]);
			List<string[]> rows = [.. lines.Skip(1).Select(l => ParseCsvLine(l).ToArray())];
			fileData.Add((rows, headerList));
		}
		if (fileData.Count < 2) throw new Exception("Not enough data to aggregate.");
		List<string> headerCols = [.. fileData[0].HeaderList];
		int applicationIndex = headerCols.FindIndex(header => string.Equals(header, "Application", StringComparison.OrdinalIgnoreCase));
		if (applicationIndex < 0)
		{
			applicationIndex = headerCols.Count;
			headerCols.Add("Application");
		}
		int aggregateDurationIndex = headerCols.FindIndex(header => string.Equals(header, AggregateDurationColumn, StringComparison.OrdinalIgnoreCase));
		if (aggregateDurationIndex < 0)
		{
			aggregateDurationIndex = headerCols.Count;
			headerCols.Add(AggregateDurationColumn);
		}
		int aggregateSourcesIndex = headerCols.FindIndex(header =>
			string.Equals(header, AggregateSourcesColumn, StringComparison.OrdinalIgnoreCase));
		if (aggregateSourcesIndex < 0)
		{
			aggregateSourcesIndex = headerCols.Count;
			headerCols.Add(AggregateSourcesColumn);
		}
		string aggregateSources = Convert.ToBase64String(JsonSerializer.SerializeToUtf8Bytes(
			selected.Select(recording => recording.FileName)
				.Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
			BenchmarksJsonContext.Default.StringArray));
		double meanDurationSeconds = selected.Average(recording => recording.DurationSeconds);
		int maxRows = fileData.Max(f => f.Rows.Count);
		await using (var writer = new StreamWriter(outPath))
		{
			await writer.WriteLineAsync(string.Join(",", headerCols));
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
						if (r < rows.Count && c < rows[r].Length &&
							double.TryParse(rows[r][c], NumberStyles.Float, CultureInfo.InvariantCulture, out double value))
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
				await writer.WriteLineAsync(string.Join(",", averagedRow));
			}
		}
		LoadRecordings();
	}
	// ── Analyse ──────────────────────────────────────────────────────────────
	private Task RenderAnalysisChartsForSelection(List<RecordingItem> items)
	{
		if (items.Count is <= 0 or > 2 || ViewModel.ActiveTab != "Analysis")
			return Task.CompletedTask;
		return RenderAnalysisCharts(items);
	}
	private async void MetricComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		var items = GetSelectedRecordings();
		if (items.Count == 0 || ViewModel.ActiveTab != "Analysis")
			return;
		await RenderMetricChart(items);
	}
	// ── Data model for analysis results ──────────────────────────────────────
	private sealed record AnalysisModel(
			List<(string recordingName, List<SeriesPoint> points)> MetricSeries,
			List<(
				string recordingName,
				Dictionary<string, double> displayedStats,
				Dictionary<string, double> renderedStats)> FpsStatsSeries);
	private sealed record ChartPresentation(
			List<BarPoint> DisplayedFpsBars1,
			List<BarPoint> RenderedFpsBars1,
			List<BarPoint> DisplayedFpsBars2,
			List<BarPoint> RenderedFpsBars2,
			bool ShowRenderedFps1,
			bool ShowDisplayedFps2,
			bool ShowRenderedFps2,
			string DisplayedFpsLabel1,
			string RenderedFpsLabel1,
			string DisplayedFpsLabel2,
			string RenderedFpsLabel2,
			List<SeriesPoint> MetricPts1,
			List<SeriesPoint> MetricPts2,
			bool ShowMetric2,
			string MetricLabel1,
			string MetricLabel2,
			string FpsYAxisLabel,
			string FpsLabelFormat,
			string MetricYAxisLabel);
	private async Task RenderAnalysisCharts(List<RecordingItem> items)
	{
		if (ViewModel.ActiveTab != "Analysis")
			return;
		if (items.Count is 0 or > 2)
		{
			ClearAnalysisCharts();
			return;
		}
		var presentation = await CreateAnalysisPresentation(items);
		if (ViewModel.ActiveTab != "Analysis" || !IsCurrentSelection(items))
			return;
		if (presentation == null)
		{
			ClearAnalysisCharts();
			return;
		}
		ApplyAnalysisChartPresentation(presentation);
	}
	private async Task RenderMetricChart(List<RecordingItem> items)
	{
		var presentation = await CreateAnalysisPresentation(items);
		if (presentation is not null &&
			ViewModel.ActiveTab == "Analysis" &&
			IsCurrentSelection(items))
			ApplyMetricChartPresentation(presentation);
	}
	private bool IsCurrentSelection(List<RecordingItem> items)
	{
		var selected = GetSelectedRecordings();
		return selected.Count == items.Count &&
			selected.All(current => items.Any(original =>
				string.Equals(original.FilePath, current.FilePath, StringComparison.OrdinalIgnoreCase)));
	}
	private Task<ChartPresentation> CreateAnalysisPresentation(List<RecordingItem> items)
	{
		string metric = Metric1ComboBox.SelectedItem as string ?? string.Empty;
		if (string.IsNullOrWhiteSpace(metric))
			return Task.FromResult<ChartPresentation>(null);
		return Task.Run(() => BuildAnalysisPresentation(
			items,
			metric));
	}
	private ChartPresentation BuildAnalysisPresentation(
			List<RecordingItem> items,
			string metric)
	{
		List<(RecordingItem item, DateTime lastWriteUtc)> loaded = [.. items
			.Select(item => (item, lastWriteUtc: File.Exists(item.FilePath)
				? File.GetLastWriteTimeUtc(item.FilePath)
				: DateTime.MinValue))
			.Where(entry => entry.lastWriteUtc != DateTime.MinValue)];
		if (loaded.Count == 0)
			return null;
		string presentationCacheKey = string.Join(
			"|",
			loaded.Select(entry =>
				$"{entry.item.FilePath}\u001f{entry.lastWriteUtc.Ticks}")) +
			$"|\u001e{metric}";
		lock (_cacheLock)
		{
			if (_analysisPresentationCache.TryGetValue(presentationCacheKey, out ChartPresentation cached))
				return cached;
		}
		if (!loaded.Any(entry =>
			HasMetricColumn(entry.item.FilePath, entry.lastWriteUtc, "MsBetweenDisplayChange") ||
			HasMetricColumn(entry.item.FilePath, entry.lastWriteUtc, "MsBetweenPresents")))
			return null;
		List<(string recordingName, List<SeriesPoint> points)> metricSeries = [];
		List<(
			string recordingName,
			Dictionary<string, double> displayedStats,
			Dictionary<string, double> renderedStats)> fpsStatsSeries = [];
		string metricColumn = GetFpsSourceColumn(metric);
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
			if (string.Equals(metricColumn, "MsBetweenDisplayChange", StringComparison.OrdinalIgnoreCase))
				displayedFrameTimes = [.. rawMetricValues];
			else
				LoadMetricColumn(item.FilePath, lastWriteUtc, "MsBetweenDisplayChange", out displayedFrameTimes);

			List<double> renderedFrameTimes;
			if (string.Equals(metricColumn, "MsBetweenPresents", StringComparison.OrdinalIgnoreCase))
				renderedFrameTimes = [.. rawMetricValues];
			else
				LoadMetricColumn(item.FilePath, lastWriteUtc, "MsBetweenPresents", out renderedFrameTimes);

			fpsStatsSeries.Add((
				item.FileName,
				StatsToDict(BenchmarkStatistics.CalculateMetrics([.. displayedFrameTimes
					.Where(v => v > 0).Select(v => 1000.0 / v)], isFpsMetric: true)),
				StatsToDict(BenchmarkStatistics.CalculateMetrics([.. renderedFrameTimes
					.Where(v => v > 0).Select(v => 1000.0 / v)], isFpsMetric: true))));
		}
		ChartPresentation presentation = BuildChartPresentation(
			new AnalysisModel(metricSeries, fpsStatsSeries));
		lock (_cacheLock)
			_analysisPresentationCache[presentationCacheKey] = presentation;
		return presentation;
	}
	private static Dictionary<string, double> StatsToDict(Metrics m)
	{
		return new(StringComparer.OrdinalIgnoreCase)
		{
			["0.1% Low Avg"] = m.Low01, ["1% Low Avg"] = m.Low1,
			["Avg (Arithmetic)"] = m.AvgArithmetic, ["Avg (Harmonic)"] = m.AvgHarmonic,
			["Min"] = m.Min, ["Max"] = m.Max,
			["P0.1"] = m.P01, ["P1"] = m.P1, ["P5"] = m.P5,
			["P50 (Median)"] = m.P50Median, ["P95"] = m.P95, ["P99"] = m.P99
		};
	}
	private static ChartPresentation BuildChartPresentation(AnalysisModel model)
	{
		string[] order = ["0.1% Low Avg", "1% Low Avg", "Avg (Arithmetic)", "Avg (Harmonic)", "Min", "Max", "P0.1", "P1", "P5", "P50 (Median)", "P95", "P99"];
		List<BarPoint> displayedFpsBars1 = [];
		List<BarPoint> renderedFpsBars1 = [];
		List<BarPoint> displayedFpsBars2 = [];
		List<BarPoint> renderedFpsBars2 = [];
		bool showRenderedFps1 = false;
		bool showDisplayedFps2 = false;
		bool showRenderedFps2 = false;
		string displayedFpsLabel1 = string.Empty;
		string renderedFpsLabel1 = string.Empty;
		string displayedFpsLabel2 = string.Empty;
		string renderedFpsLabel2 = string.Empty;
		if (model.FpsStatsSeries.Count > 0)
		{
			int seriesIdx = 0;
			foreach (var (recordingName, displayedStats, renderedStats) in model.FpsStatsSeries)
			{
				List<BarPoint> displayedTarget =
					seriesIdx == 0 ? displayedFpsBars1 : displayedFpsBars2;
				List<BarPoint> renderedTarget =
					seriesIdx == 0 ? renderedFpsBars1 : renderedFpsBars2;
				foreach (string percentile in order)
				{
					if (displayedStats.TryGetValue(percentile, out double displayedValue))
						displayedTarget.Add(new BarPoint { Label = percentile, Value = displayedValue });
					if (renderedStats.TryGetValue(percentile, out double renderedValue))
						renderedTarget.Add(new BarPoint { Label = percentile, Value = renderedValue });
				}
				if (seriesIdx == 0)
				{
					displayedFpsLabel1 = $"{recordingName} · Displayed FPS";
					renderedFpsLabel1 = $"{recordingName} · Rendered FPS";
					showRenderedFps1 = renderedTarget.Count > 0;
				}
				else
				{
					displayedFpsLabel2 = $"{recordingName} · Displayed FPS";
					renderedFpsLabel2 = $"{recordingName} · Rendered FPS";
					showDisplayedFps2 = displayedTarget.Count > 0;
					showRenderedFps2 = renderedTarget.Count > 0;
				}
				seriesIdx++;
			}
		}
		List<SeriesPoint> metricPts1 = [];
		List<SeriesPoint> metricPts2 = [];
		bool showMetric2 = false;
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
					showMetric2 = true;
				}
				seriesIdx++;
			}
		}
		return new ChartPresentation(
					displayedFpsBars1,
					renderedFpsBars1,
					displayedFpsBars2,
					renderedFpsBars2,
					showRenderedFps1,
					showDisplayedFps2,
					showRenderedFps2,
					displayedFpsLabel1,
					renderedFpsLabel1,
					displayedFpsLabel2,
					renderedFpsLabel2,
					metricPts1,
					metricPts2,
					showMetric2,
					metricLabel1,
					metricLabel2,
					"FPS",
					"0.#",
					"Milliseconds (ms)");
	}

	private void ApplyAnalysisChartPresentation(ChartPresentation presentation)
	{
		if (ViewModel.ActiveTab != "Analysis")
			return;
		ApplyFpsChartPresentation(presentation);
		ApplyMetricChartPresentation(presentation);
	}
	private ChartPresentation _lastChartPresentation;

	private void ApplyFpsChartPresentation(ChartPresentation presentation)
	{
		_lastChartPresentation = presentation;
		ViewModel.FpsChartYAxisLabel = presentation.FpsYAxisLabel;
		ViewModel.FpsChartLabelFormat = presentation.FpsLabelFormat;
		ViewModel.FpsChartLabel = presentation.DisplayedFpsLabel1;
		ViewModel.FpsRenderedChartLabel = presentation.RenderedFpsLabel1;
		ViewModel.FpsChartLabel2 = presentation.DisplayedFpsLabel2;
		ViewModel.FpsRenderedChartLabel2 = presentation.RenderedFpsLabel2;
		ViewModel.ShowRenderedFps = presentation.ShowRenderedFps1;
		ViewModel.ShowFpsChart2 = presentation.ShowDisplayedFps2;
		ViewModel.ShowRenderedFpsChart2 = presentation.ShowRenderedFps2;
		var series1Data = presentation.DisplayedFpsBars1.Where(b => ViewModel.IsMetricEnabled(b.Label)).ToList();
		var series1RenderedData = presentation.RenderedFpsBars1.Where(b => ViewModel.IsMetricEnabled(b.Label)).ToList();
		var series2Data = presentation.DisplayedFpsBars2.Where(b => ViewModel.IsMetricEnabled(b.Label)).ToList();
		var series2RenderedData = presentation.RenderedFpsBars2.Where(b => ViewModel.IsMetricEnabled(b.Label)).ToList();

		ViewModel.FpsBarSeries = [.. series1Data];
		ViewModel.FpsRenderedBarSeries = presentation.ShowRenderedFps1 ? [.. series1RenderedData] : null;
		ViewModel.FpsBarSeries2 = presentation.ShowDisplayedFps2 ? [.. series2Data] : null;
		ViewModel.FpsRenderedBarSeries2 = presentation.ShowRenderedFps2 ? [.. series2RenderedData] : null;


		// Manage series collection for Bar chart (only exists when AnalysisChartType == "Bar")
		if (FpsChart != null)
		{
			FpsChart.Series.Clear();
			FpsChart.Series.Add(FpsChart1Series);
			if (presentation.ShowRenderedFps1)
				FpsChart.Series.Add(FpsRenderedChart1Series);
			if (presentation.ShowDisplayedFps2)
				FpsChart.Series.Add(FpsChart2Series);
			if (presentation.ShowRenderedFps2)
				FpsChart.Series.Add(FpsRenderedChart2Series);

			FpsChart1Series.ShowDataLabels = false;
			FpsChart1Series.ShowDataLabels = true;
			FpsRenderedChart1Series.ShowDataLabels = false;
			FpsRenderedChart1Series.ShowDataLabels = presentation.ShowRenderedFps1;
			FpsChart2Series.ShowDataLabels = false;
			FpsChart2Series.ShowDataLabels = presentation.ShowDisplayedFps2;
			FpsRenderedChart2Series.ShowDataLabels = false;
			FpsRenderedChart2Series.ShowDataLabels = presentation.ShowRenderedFps2;

			FpsChart.IsTransposed = false;
			FpsChart.IsTransposed = true;
		}

		// Manage series collection for Column chart (only exists when AnalysisChartType == "Column")
		if (ColumnFpsChart != null)
		{
			ColumnFpsChart.Series.Clear();
			ColumnFpsChart.Series.Add(ColumnFpsChart1Series);
			if (presentation.ShowRenderedFps1)
				ColumnFpsChart.Series.Add(ColumnFpsRenderedChart1Series);
			if (presentation.ShowDisplayedFps2)
				ColumnFpsChart.Series.Add(ColumnFpsChart2Series);
			if (presentation.ShowRenderedFps2)
				ColumnFpsChart.Series.Add(ColumnFpsRenderedChart2Series);

			ColumnFpsChart1Series.ShowDataLabels = false;
			ColumnFpsChart1Series.ShowDataLabels = true;
			ColumnFpsRenderedChart1Series.ShowDataLabels = false;
			ColumnFpsRenderedChart1Series.ShowDataLabels = presentation.ShowRenderedFps1;
			ColumnFpsChart2Series.ShowDataLabels = false;
			ColumnFpsChart2Series.ShowDataLabels = presentation.ShowDisplayedFps2;
			ColumnFpsRenderedChart2Series.ShowDataLabels = false;
			ColumnFpsRenderedChart2Series.ShowDataLabels = presentation.ShowRenderedFps2;
		}
	}
	private void ApplyMetricChartPresentation(ChartPresentation presentation)
	{
		ViewModel.MetricChartYAxisLabel = presentation.MetricYAxisLabel;
		ViewModel.MetricChartLabel = presentation.MetricLabel1;
		ViewModel.MetricChartLabel2 = presentation.MetricLabel2;
		ViewModel.ShowMetricChart2 = presentation.ShowMetric2;
		ViewModel.MetricSeries = [.. presentation.MetricPts1];
		ViewModel.MetricSeries2 = [.. presentation.MetricPts2];
	}
	private static bool IsFpsMetric(string metric) =>
			metric.EndsWith("FPS", StringComparison.OrdinalIgnoreCase) ||
			metric.Contains("FPS", StringComparison.OrdinalIgnoreCase);
	private static string GetFpsSourceColumn(string metric) => metric switch
	{
		"Displayed FPS" => "MsBetweenDisplayChange",
		"Rendered FPS" => "MsBetweenPresents",
		_ => metric
	};
	private void ClearAnalysisCharts()
	{
		_lastChartPresentation = null;
		ViewModel.ClearAnalysis();
	}
	// ── Statistics tab ──────────────────────────────────────────────────────────
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
	private async Task RefreshStatisticsTable()
	{
		if (ViewModel.ActiveTab != "Statistics")
			return;
		var selected = GetSelectedRecordings();
		bool showRecordingB = selected.Count == 2;
		bool containsRecordingB = StatisticsTreeGrid.Columns.Contains(StatisticsRecordingBColumn);
		if (showRecordingB && !containsRecordingB)
			StatisticsTreeGrid.Columns.Add(StatisticsRecordingBColumn);
		else if (!showRecordingB && containsRecordingB)
			StatisticsTreeGrid.Columns.Remove(StatisticsRecordingBColumn);
		if (selected.Count == 0)
		{
			ViewModel.StatisticsRows.Clear();
			ViewModel.RecordingAHeader = "Recording A";
			ViewModel.RecordingBHeader = "Recording B";
			return;
		}
		if (selected.Count > 2)
		{
			ViewModel.StatisticsRows.Clear();
			ViewModel.RecordingAHeader = "Recording A";
			ViewModel.RecordingBHeader = "Recording B";
			return;
		}
		ViewModel.RecordingAHeader = selected.Count >= 1 ? selected[0].Title : "Recording A";
		ViewModel.RecordingBHeader = selected.Count >= 2 ? selected[1].Title : "Recording B";
		var builtRows = await Task.Run(() =>
				{
					List<(RecordingItem item, DateTime lastWriteUtc)> loaded = [];
					List<ResultRow> resultRows = [];
					foreach (var i in selected.Take(2))
					{
						var lastWriteUtc = File.Exists(i.FilePath) ? File.GetLastWriteTimeUtc(i.FilePath) : DateTime.MinValue;
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
							if (!TryGetMetricsCached(loaded[i].item.FilePath, loaded[i].lastWriteUtc, column, isFps, out var mm))
								return;
							m[i] = mm;
						}
						foreach (var label in MetricLabels)
						{
							string a = FormatStat(NumericMetric(m[0], label), isFps);
							string b = loaded.Count < 2 ? "" : FormatStat(NumericMetric(m[1], label), isFps);
							resultRows.Add(new ResultRow { Metric = $"{prefix} {label} FPS", RecordingA = a, RecordingB = b });
						}
						string FormatFpsPacing(double value, string format) =>
							value == 0 ? "—" : value.ToString(format, CultureInfo.InvariantCulture);
						resultRows.Add(new ResultRow
						{
							Metric = $"{prefix} Standard Deviation (STDEV)",
							RecordingA = FormatFpsPacing(m[0].StdDev, "0.###"),
							RecordingB = loaded.Count < 2 ? "" : FormatFpsPacing(m[1].StdDev, "0.###")
						});
						resultRows.Add(new ResultRow
						{
							Metric = $"{prefix} Coefficient of Variation (CV)",
							RecordingA = FormatFpsPacing(m[0].Cv, "0.#####"),
							RecordingB = loaded.Count < 2 ? "" : FormatFpsPacing(m[1].Cv, "0.#####")
						});
					}
					AddStatsRows("Displayed", "MsBetweenDisplayChange", isFps: true);
					AddStatsRows("Rendered", "MsBetweenPresents", isFps: true);

					void AddMsStats(string prefix, string column)
					{
						Metrics[] m = new Metrics[loaded.Count];
						for (int i = 0; i < loaded.Count; i++)
						{
							if (!TryGetMetricsCached(loaded[i].item.FilePath, loaded[i].lastWriteUtc, column, isFps: false, out var mm))
								return;
							m[i] = mm;
						}
						string fmtMs(double v) => FormatStat(v, isFps: false);
						string fmtSd(double v) => v == 0 ? "—" : v.ToString("0.####", CultureInfo.InvariantCulture) + " ms";
						string fmtRel(double v) => v == 0 ? "—" : v.ToString("0.#####", CultureInfo.InvariantCulture);
						resultRows.Add(new ResultRow { Metric = $"{prefix} Average (Arithmetic)", RecordingA = fmtMs(m[0].AvgArithmetic), RecordingB = loaded.Count < 2 ? "" : fmtMs(m[1].AvgArithmetic) });
						resultRows.Add(new ResultRow { Metric = $"{prefix} P50 (Median)", RecordingA = fmtMs(m[0].P50Median), RecordingB = loaded.Count < 2 ? "" : fmtMs(m[1].P50Median) });
						resultRows.Add(new ResultRow { Metric = $"{prefix} P95", RecordingA = fmtMs(m[0].P5), RecordingB = loaded.Count < 2 ? "" : fmtMs(m[1].P5) });
						resultRows.Add(new ResultRow { Metric = $"{prefix} P99", RecordingA = fmtMs(m[0].P1), RecordingB = loaded.Count < 2 ? "" : fmtMs(m[1].P1) });
						resultRows.Add(new ResultRow { Metric = $"{prefix} P99.9", RecordingA = fmtMs(m[0].P01), RecordingB = loaded.Count < 2 ? "" : fmtMs(m[1].P01) });
						resultRows.Add(new ResultRow { Metric = $"{prefix} Maximum", RecordingA = fmtMs(m[0].Max), RecordingB = loaded.Count < 2 ? "" : fmtMs(m[1].Max) });
						resultRows.Add(new ResultRow { Metric = $"{prefix} Minimum", RecordingA = fmtMs(m[0].Min), RecordingB = loaded.Count < 2 ? "" : fmtMs(m[1].Min) });
						string fmtPct(double v) => v == 0 ? "—" : v.ToString("0.0") + "%";
						string aRmssdPct = m[0].AvgArithmetic != 0 ? fmtPct(m[0].Rmssd / m[0].AvgArithmetic * 100) : "—";
						string bRmssdPct = loaded.Count < 2 ? "" : (m[1].AvgArithmetic != 0 ? fmtPct(m[1].Rmssd / m[1].AvgArithmetic * 100) : "—");
						resultRows.Add(new ResultRow { Metric = $"{prefix} Root mean square of successive differences (RMSSD)", RecordingA = aRmssdPct, RecordingB = bRmssdPct });
						string aSr = fmtRel(m[0].StepwiseRelSD);
						string bSr = loaded.Count < 2 ? "" : fmtRel(m[1].StepwiseRelSD);
						resultRows.Add(new ResultRow { Metric = $"{prefix} Stepwise-Relative", RecordingA = aSr, RecordingB = bSr });
						resultRows.Add(new ResultRow { Metric = $"{prefix} Standard Deviation (STDEV)", RecordingA = fmtSd(m[0].StdDev), RecordingB = loaded.Count < 2 ? "" : fmtSd(m[1].StdDev) });
						string aCv = fmtRel(m[0].Cv);
						string bCv = loaded.Count < 2 ? "" : fmtRel(m[1].Cv);
						resultRows.Add(new ResultRow { Metric = $"{prefix} Coefficient of Variation (CV)", RecordingA = aCv, RecordingB = bCv });
					}
					AddMsStats("MsBetweenDisplayChange", "MsBetweenDisplayChange");
					AddMsStats("MsBetweenPresents", "MsBetweenPresents");
					AddMsStats("MsGPUBusy", "MsGPUBusy");
					AddMsStats("MsUntilDisplayed", "MsUntilDisplayed");

					ApplyResultComparisons(resultRows, loaded.Count == 2);
					return GroupResultRows(resultRows);
				});
		if (ViewModel.ActiveTab != "Statistics")
			return;
		if (builtRows.Count == 0)
		{
			ViewModel.StatisticsRows = [];
			return;
		}
		ViewModel.StatisticsRows = [.. builtRows];
		StatisticsTreeGrid.ExpandAllNodes();
	}
	private bool TryGetMetricsCached(string filePath, DateTime lastWriteUtc, string column, bool isFps, out Metrics metrics)
	{
		var cacheKey = (filePath, lastWriteUtc, column, isFps);
		lock (_cacheLock)
		{
			if (_metricsCache.TryGetValue(cacheKey, out var cached))
			{
				metrics = cached;
				return true;
			}
		}
		if (!LoadMetricColumn(filePath, lastWriteUtc, column, out var values))
		{
			metrics = null;
			return false;
		}
		var array = isFps
			? values.Where(v => v > 0).Select(v => 1000.0 / v).ToArray()
			: values.ToArray();
		if (array.Length == 0)
		{
			metrics = null;
			return false;
		}
		metrics = BenchmarkStatistics.CalculateMetrics(array, isFps);
		lock (_cacheLock)
			_metricsCache[cacheKey] = metrics;
		return true;
	}
	private string GetMetricValue(string filePath, DateTime lastWriteUtc, string column, bool isFps, string label)
	{
		if (!TryGetMetricsCached(filePath, lastWriteUtc, column, isFps, out var metrics))
			return "-";
		double value = label switch
		{
			"0.1% Low" => metrics.Low01,
			"1% Low" => metrics.Low1,
			"Avg (Arithmetic)" => metrics.AvgArithmetic,
			"Avg (Harmonic)" => metrics.AvgHarmonic,
			"Min" => metrics.Min,
			"Max" => metrics.Max,
			"P0.1" => metrics.P01,
			"P1" => metrics.P1,
			"P5" => metrics.P5,
			"P50 (Median)" => metrics.P50Median,
			"P95" => metrics.P95,
			"P99" => metrics.P99,
			_ => 0
		};
		return value == 0 ? "—" : value.ToString(isFps ? "0.###" : "0.####", CultureInfo.InvariantCulture);
	}
	private bool HasResultMetric(string filePath, DateTime lastWriteUtc, string column)
	{
		return HasMetricColumn(filePath, lastWriteUtc, column);
	}
	private static double NumericMetric(Metrics m, string label) => label switch
	{
		"0.1% Low Avg" => m.Low01,
		"1% Low Avg" => m.Low1,
		"Average (Arithmetic)" => m.AvgArithmetic,
		"Average (Harmonic)" => m.AvgHarmonic,
		"Minimum" => m.Min,
		"Maximum" => m.Max,
		"P0.1" => m.P01,
		"P1" => m.P1,
		"P5" => m.P5,
		"P50 (Median)" => m.P50Median,
		"P95" => m.P95,
		"P99" => m.P99,
		_ => 0
	};
	private static string FormatStat(double value, bool isFps)
	{
		if (value == 0)
			return "—";
		return value.ToString(isFps ? "0.###" : "0.####", CultureInfo.InvariantCulture) + (isFps ? " FPS" : " ms");
	}
	// ── CSV loading / stats ──────────────────────────────────────────────────
	private bool HasMetricColumn(string filePath, DateTime lastWriteUtc, string metric)
	{
		return GetHeaderIndex(filePath, lastWriteUtc, out var headerIndex) && ResolveHeaderIndex(headerIndex, metric, out _);
	}
	private bool GetHeaderIndex(string filePath, DateTime lastWriteUtc, out Dictionary<string, int> headerIndex)
	{
		headerIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		if (!File.Exists(filePath))
			return false;
		lock (_cacheLock)
		{
			if (_headerCache.TryGetValue(filePath, out var cached) && cached.LastWriteUtc == lastWriteUtc)
			{
				headerIndex = cached.HeaderIndex;
				return true;
			}
		}
		try
		{
			using var reader = new StreamReader(filePath);
			var headerLine = reader.ReadLine();
			if (string.IsNullOrWhiteSpace(headerLine))
				return false;
			var headers = ParseCsvLine(headerLine);
			if (headers.Count == 0)
				return false;
			headerIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
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
			lock (_cacheLock)
			{
				_headerCache[filePath] = new CachedFile(filePath, lastWriteUtc, headerIndex);
			}
			return true;
		}
		catch
		{
			return false;
		}
	}
	private bool LoadMetricColumn(string filePath, DateTime lastWriteUtc, string metric, out List<double> values)
	{
		values = [];
		if (!GetHeaderIndex(filePath, lastWriteUtc, out var headerIndex))
			return false;
		if (!ResolveHeaderIndex(headerIndex, metric, out int idx))
			return false;
		var key = (filePath, lastWriteUtc, metric);
		lock (_cacheLock)
		{
			if (_columnCache.TryGetValue(key, out var cached))
			{
				values = cached;
				return cached.Count > 0;
			}
		}
		try
		{
			using var reader = new StreamReader(filePath);
			_ = reader.ReadLine(); // header
			var list = new List<double>(capacity: 4096);
			while (!reader.EndOfStream)
			{
				var line = reader.ReadLine();
				if (string.IsNullOrWhiteSpace(line))
					continue;
				var cols = ParseCsvLine(line);
				if (idx < 0 || idx >= cols.Count)
					continue;
				if (double.TryParse(cols[idx], NumberStyles.Float, CultureInfo.InvariantCulture, out var v))
					list.Add(v);
			}
			lock (_cacheLock)
			{
				_columnCache[key] = list;
			}
			values = list;
			return list.Count > 0;
		}
		catch
		{
			return false;
		}
	}
	private bool LoadAnalysisColumns(string filePath, DateTime lastWriteUtc)
	{
		if (!GetHeaderIndex(filePath, lastWriteUtc, out var headerIndex))
			return false;

		string[] metrics =
		[
			"MsBetweenDisplayChange",
			"MsBetweenPresents",
			"MsGPUBusy",
			"MsUntilDisplayed"
		];
		List<(string Metric, int Index, List<double> Values)> columns = [];
		foreach (string metric in metrics)
		{
			if (ResolveHeaderIndex(headerIndex, metric, out int index))
				columns.Add((metric, index, new List<double>(4096)));
		}
		if (columns.Count == 0)
			return false;

		lock (_cacheLock)
		{
			if (columns.All(column =>
				_columnCache.ContainsKey((filePath, lastWriteUtc, column.Metric))))
				return true;
		}

		try
		{
			using var reader = new StreamReader(filePath);
			_ = reader.ReadLine();
			while (!reader.EndOfStream)
			{
				string line = reader.ReadLine();
				if (string.IsNullOrWhiteSpace(line))
					continue;
				List<string> values = ParseCsvLine(line);
				for (int index = 0; index < columns.Count; index++)
				{
					var column = columns[index];
					if (column.Index < values.Count &&
						double.TryParse(values[column.Index], NumberStyles.Float, CultureInfo.InvariantCulture, out double value))
						column.Values.Add(value);
				}
			}

			lock (_cacheLock)
			{
				foreach (var column in columns)
					_columnCache[(filePath, lastWriteUtc, column.Metric)] = column.Values;
			}
			return true;
		}
		catch
		{
			return false;
		}
	}
	private static bool ResolveHeaderIndex(Dictionary<string, int> headerIndex, string metric, out int idx)
	{
		return headerIndex.TryGetValue(metric, out idx);
	}

	private bool TryGetMetricsCached(string filePath, DateTime lastWriteUtc, string metric, out Metrics metrics)
	{
		return TryGetMetricsCached(filePath, lastWriteUtc, metric, isFps: false, out metrics);
	}
	// ── Process name ─────────────────────────────────────────────────────────
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
	[System.Diagnostics.CodeAnalysis.SuppressMessage(
		"Performance",
		"CA1822:Mark members as static",
		Justification = "XAML event handlers must be instance methods.")]
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
			ViewModel.SetRecordableProcesses(
				PresentingProcesses.GetRecordableProcesses(refreshRunningProcesses: true));
		}
		else if (ReferenceEquals(selectedItem, AnalysisTab))
		{
			ViewModel.ActiveTab = "Analysis";
			if (HeaderFpsColorPicker is not null)
				ReapplyColorPickerTemplate(HeaderFpsColorPicker, ViewModel.FpsColor);
			if (HeaderFpsColorPicker2 is not null)
				ReapplyColorPickerTemplate(HeaderFpsColorPicker2, ViewModel.FpsColor2);
			var selected = GetSelectedRecordings();
			ViewModel.SetSelectedRecordings(selected);
			if (selected.Count is > 0 and <= 2)
				await RenderAnalysisChartsForSelection(selected);
		}
		else if (ReferenceEquals(selectedItem, StatisticsTab))
		{
			ViewModel.ActiveTab = "Statistics";
			var selected = GetSelectedRecordings();
			ViewModel.SetSelectedRecordings(selected);
			if (selected.Count is > 0 and <= 2)
				await RefreshStatisticsTable();
		}
	}
	private static void ReapplyColorPickerTemplate(DevWinUI.DropdownColorPicker picker, Windows.UI.Color color)
	{
		var template = picker.Template;
		if (template is null)
			return;

		picker.Template = null;
		picker.Template = template;
		picker.ApplyTemplate();
		picker.Color = color;
	}
}

