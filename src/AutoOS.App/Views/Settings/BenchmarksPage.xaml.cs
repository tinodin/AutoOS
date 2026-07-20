using System.Globalization;
using AutoOS.Core.Helpers.Picker;
using AutoOS.Views.Settings.Benchmarks;
using Syncfusion.UI.Xaml.DataGrid;
using System.Text.Json;
using Windows.System;
using static AutoOS.Views.Settings.Benchmarks.BenchmarkCsv;
using static AutoOS.Views.Settings.Benchmarks.BenchmarkStatistics;
namespace AutoOS.Views.Settings;

public sealed partial class BenchmarksPage : Page
{
	public BenchmarksViewModel ViewModel { get; } = new();

	private static readonly string RecordingsDirectory = Path.Combine(PathHelper.GetAppDataFolderPath(), "Benchmarks");
	private static readonly string[] PercentileLabels = ["Mean", "P0.1", "P1", "P5", "P10", "P50", "P90", "P95", "P99", "P99.9"];
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
	private readonly Dictionary<(string path, DateTime lastWriteUtc), Dictionary<string, double>> _averagesCache = [];
	private readonly Dictionary<(string path, DateTime lastWriteUtc, string metric), double[]> _sortedFpsCache = [];
	private readonly Dictionary<(string path, DateTime lastWriteUtc, string metric), (double stepwiseRelSD, double cv, double rmssd, double stdDev)> _statsCache = [];
	private readonly Lock _cacheLock = new();
	public BenchmarksPage()
	{
		InitializeComponent();
		PresentingProcesses.ProcessesChanged += PresentingProcesses_ProcessesChanged;
		ViewModel.FpsColor = Colors.DodgerBlue;
		ViewModel.FpsColor2 = Colors.Orange;
		LoadRecordings();
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

	private void ResultsTreeGrid_SizeChanged(object sender, SizeChangedEventArgs e)
	{
		if (e.NewSize.Width > 0)
		{
			foreach (var col in ResultsTreeGrid.Columns)
				col.Width = double.NaN;
			ResultsTreeGrid.InvalidateMeasure();
			ResultsTreeGrid.UpdateLayout();
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
	private async void FpsUnitSelector_SelectionChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
	{
		ViewModel.ShowFpsAsMilliseconds = sender.SelectedItem == MsUnitItem;
		if (ViewModel.ActiveTab != "Analysis")
			return;
		var items = GetSelectedRecordings();
		if (items.Count > 0)
			await RenderFpsChart(items);
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
				TryParseDouble(firstValues[aggregateDurationIndex], out double aggregateDuration))
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
				sourceFileNames = JsonSerializer.Deserialize<List<string>>(sourceJson) ?? [];
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
				TryParseDouble(firstValues[timeSecondsIndex], out double firstTimeSeconds) &&
				TryParseDouble(lastValues[timeSecondsIndex], out double lastTimeSeconds))
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
		ViewModel.ResultsRows.Clear();
		if (items.Count is 0 or > 2)
			return;
		if (ViewModel.ActiveTab == "Analysis")
			await RenderAnalysisChartsForSelection(items);
		else if (ViewModel.ActiveTab == "Results")
			await RefreshResultsTable();
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
		string outPath = Path.Combine(RecordingsDirectory, $"PresentMon_Aggregated_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
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
				.Distinct(StringComparer.OrdinalIgnoreCase)));
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
							TryParseDouble(rows[r][c], out double value))
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
	private async void FpsMetricComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		var items = GetSelectedRecordings();
		if (items.Count == 0 || ViewModel.ActiveTab != "Analysis")
			return;
		await RenderFpsChart(items);
	}

	// ── Data model for analysis results ──────────────────────────────────────
	/// <summary>
	/// Describes whether a column's native unit is milliseconds or FPS.
	/// </summary>
	private enum NativeUnit { Milliseconds, Fps }
	private sealed record AnalysisModel(
			NativeUnit FpsNativeUnit,
			List<(string recordingName, List<(int x, double y)> points)> MetricSeries,
			List<(string recordingName, Dictionary<string, double> stats)> FpsStatsSeries);
	private sealed record ChartPresentation(
			List<BarPoint> FpsBars1,
			List<BarPoint> FpsBars2,
			bool ShowFps2,
			string FpsLabel1,
			string FpsLabel2,
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
	private async Task RenderFpsChart(List<RecordingItem> items)
	{
		var presentation = await CreateAnalysisPresentation(items);
		if (presentation is not null &&
			ViewModel.ActiveTab == "Analysis" &&
			IsCurrentSelection(items))
			ApplyFpsChartPresentation(presentation);
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
		string fpsMetric = FpsMetricComboBox.SelectedItem as string ?? "Displayed FPS";
		if (string.IsNullOrWhiteSpace(metric) || string.IsNullOrWhiteSpace(fpsMetric))
			return Task.FromResult<ChartPresentation>(null);
		bool showMilliseconds = ViewModel.ShowFpsAsMilliseconds;
		return Task.Run(() => BuildAnalysisPresentation(
			items,
			metric,
			fpsMetric,
			showMilliseconds));
	}
	private ChartPresentation BuildAnalysisPresentation(
			List<RecordingItem> items,
			string metric,
			string fpsMetric,
			bool showMs)
	{
		List<(RecordingItem item, DateTime lastWriteUtc)> loaded = [.. items
			.Select(item => (item, lastWriteUtc: File.Exists(item.FilePath)
				? File.GetLastWriteTimeUtc(item.FilePath)
				: DateTime.MinValue))
			.Where(entry => entry.lastWriteUtc != DateTime.MinValue)];
		if (loaded.Count == 0)
			return null;
		string fpsColumn = GetFpsSourceColumn(fpsMetric);
		if (string.IsNullOrWhiteSpace(fpsColumn) ||
			!loaded.Any(entry => HasMetricColumn(entry.item.FilePath, entry.lastWriteUtc, fpsColumn)))
			return null;
		List<(string recordingName, List<(int x, double y)> points)> metricSeries = [];
		List<(string recordingName, Dictionary<string, double> stats)> fpsStatsSeries = [];
		string metricColumn = GetFpsSourceColumn(metric);
		for (int recordingIndex = 0; recordingIndex < loaded.Count; recordingIndex++)
		{
			var (item, lastWriteUtc) = loaded[recordingIndex];
			LoadMetricColumn(item.FilePath, lastWriteUtc, metricColumn, out var rawMetricValues);
			List<double> metricValues = [.. rawMetricValues];
			if (IsFpsMetric(metric))
			{
				for (int index = 0; index < metricValues.Count; index++)
				{
					if (metricValues[index] > 0)
						metricValues[index] = 1000.0 / metricValues[index];
				}
			}
			if (metricValues.Count > 0)
			{
				const int maxPoints = 800;
				int step = Math.Max(1, metricValues.Count / maxPoints);
				var points = new List<(int x, double y)>(
					Math.Min(maxPoints, (metricValues.Count + step - 1) / step));
				for (int index = 0; index < metricValues.Count; index += step)
					points.Add((index, metricValues[index]));
				int finalIndex = metricValues.Count - 1;
				if (points[^1].x != finalIndex)
					points.Add((finalIndex, metricValues[finalIndex]));
				metricSeries.Add((item.FileName, points));
			}
			List<double> fpsValues;
			if (string.Equals(fpsColumn, metricColumn, StringComparison.OrdinalIgnoreCase))
				fpsValues = [.. rawMetricValues];
			else
				LoadMetricColumn(item.FilePath, lastWriteUtc, fpsColumn, out fpsValues);
			List<double> displayValues = showMs
							? [.. fpsValues.Where(value => value > 0)]
							: [.. fpsValues.Where(value => value > 0).Select(value => 1000.0 / value)];
			if (displayValues.Count == 0)
				continue;
			var ordered = displayValues.OrderBy(value => value).ToArray();
			var stats = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
			if (TryGetPercentileFromSorted(ordered, 99.9, out var p999)) stats["P99.9"] = p999;
			if (TryGetPercentileFromSorted(ordered, 99.0, out var p99)) stats["P99"] = p99;
			if (TryGetPercentileFromSorted(ordered, 95.0, out var p95)) stats["P95"] = p95;
			if (TryGetPercentileFromSorted(ordered, 90.0, out var p90)) stats["P90"] = p90;
			if (TryGetPercentileFromSorted(ordered, 50.0, out var p50)) stats["P50"] = p50;
			if (TryGetPercentileFromSorted(ordered, 10.0, out var p10)) stats["P10"] = p10;
			if (TryGetPercentileFromSorted(ordered, 5.0, out var p5)) stats["P5"] = p5;
			if (TryGetPercentileFromSorted(ordered, 1.0, out var p1)) stats["P1"] = p1;
			if (TryGetPercentileFromSorted(ordered, 0.1, out var p01)) stats["P0.1"] = p01;
			stats["Mean"] = displayValues.Average();
			fpsStatsSeries.Add((item.FileName, stats));
		}
		return BuildChartPresentation(
					new AnalysisModel(
						showMs ? NativeUnit.Milliseconds : NativeUnit.Fps,
						metricSeries,
						fpsStatsSeries),
					showMs,
					metric);
	}
	private static ChartPresentation BuildChartPresentation(AnalysisModel model, bool showMs, string metric1)
	{
		string[] order = ["P99.9", "P99", "P95", "P90", "P50", "P10", "P5", "P1", "P0.1", "Mean"];
		List<BarPoint> fpsBars1 = [];
		List<BarPoint> fpsBars2 = [];
		bool showFps2 = false;
		string fpsLabel1 = string.Empty;
		string fpsLabel2 = string.Empty;
		double maxDisplayVal = 0;
		if (model.FpsStatsSeries.Count > 0)
		{
			int seriesIdx = 0;
			foreach (var (recordingName, stats) in model.FpsStatsSeries)
			{
				var target = seriesIdx == 0 ? fpsBars1 : fpsBars2;
				if (seriesIdx == 1)
					showFps2 = true;
				foreach (var k in order)
				{
					if (!stats.TryGetValue(k, out var nativeVal))
						continue;
					double displayVal = ConvertForDisplay(nativeVal, model.FpsNativeUnit, showMs);
					target.Add(new BarPoint { Label = k, Value = displayVal });
					if (displayVal > maxDisplayVal)
						maxDisplayVal = displayVal;
				}
				if (seriesIdx == 0) fpsLabel1 = recordingName;
				else fpsLabel2 = recordingName;
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
				var target = seriesIdx == 0 ? metricPts1 : metricPts2;
				if (seriesIdx == 1)
					showMetric2 = true;
				foreach (var (x, y) in points)
					target.Add(new SeriesPoint { Index = x, Value = y });
				if (seriesIdx == 0) metricLabel1 = recordingName;
				else metricLabel2 = recordingName;
				seriesIdx++;
			}
		}
		return new ChartPresentation(
					fpsBars1,
					fpsBars2,
					showFps2,
					fpsLabel1,
					fpsLabel2,
					metricPts1,
					metricPts2,
					showMetric2,
					metricLabel1,
					metricLabel2,
					showMs ? "ms" : "FPS",
					showMs ? "0.## ms" : "0.# FPS",
					IsFpsMetric(metric1) ? "FPS" : "Milliseconds (ms)");
	}

	private void ApplyAnalysisChartPresentation(ChartPresentation presentation)
	{
		if (ViewModel.ActiveTab != "Analysis")
			return;
		ApplyFpsChartPresentation(presentation);
		ApplyMetricChartPresentation(presentation);
	}
	private void ApplyFpsChartPresentation(ChartPresentation presentation)
	{
		FpsChart1Series.EnableAnimation = false;
		FpsChart2Series.EnableAnimation = false;
		FpsChart1Series.EnableAnimation = true;
		FpsChart2Series.EnableAnimation = true;
		ViewModel.FpsChartYAxisLabel = presentation.FpsYAxisLabel;
		ViewModel.FpsChartLabelFormat = presentation.FpsLabelFormat;
		ViewModel.FpsChartLabel = presentation.FpsLabel1;
		ViewModel.FpsChartLabel2 = presentation.FpsLabel2;
		ViewModel.ShowFpsChart2 = presentation.ShowFps2;
		ViewModel.FpsBarSeries = [.. presentation.FpsBars1];
		ViewModel.FpsBarSeries2 = [.. presentation.FpsBars2];
		FpsChart.InvalidateMeasure();
	}
	private void ApplyMetricChartPresentation(ChartPresentation presentation)
	{
		ViewModel.MetricChartYAxisLabel = presentation.MetricYAxisLabel;
		ViewModel.MetricChartLabel = presentation.MetricLabel1;
		ViewModel.MetricChartLabel2 = presentation.MetricLabel2;
		ViewModel.ShowMetricChart2 = presentation.ShowMetric2;
		ViewModel.MetricLineSeries = [.. presentation.MetricPts1];
		ViewModel.MetricLineSeries2 = [.. presentation.MetricPts2];
	}
	private static double ConvertForDisplay(double nativeValue, NativeUnit nativeUnit, bool showMs)
	{
		if (nativeValue <= 0)
			return 0;
		if (nativeUnit == NativeUnit.Milliseconds)
		{
			// Native is ms. If showing ms, display as-is. If showing FPS, convert.
			return showMs ? nativeValue : 1000.0 / nativeValue;
		}
		else
		{
			// Native is FPS. If showing FPS, display as-is. If showing ms, convert.
			return showMs ? 1000.0 / nativeValue : nativeValue;
		}
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
		ViewModel.ClearAnalysis();
	}
	// ── Results tab ──────────────────────────────────────────────────────────
	private static void ApplyResultComparisons(IEnumerable<ResultRow> rows, bool comparisonEnabled)
	{
		if (!comparisonEnabled)
			return;
		foreach (var row in rows)
		{
			if (!double.TryParse(row.RecordingA, NumberStyles.Float, CultureInfo.InvariantCulture, out double recordingA) ||
				!double.TryParse(row.RecordingB, NumberStyles.Float, CultureInfo.InvariantCulture, out double recordingB) ||
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
	private static List<ResultRow> GroupResultRows(IEnumerable<ResultRow> rows)
	{
		List<ResultRow> groups = [];
		var groupLookup = new Dictionary<string, ResultRow>(StringComparer.Ordinal);
		foreach (var row in rows)
		{
			var (groupName, childLabel) = GetResultGroup(row.Metric);
			if (!groupLookup.TryGetValue(groupName, out var group))
			{
				group = new ResultRow { Metric = groupName };
				groupLookup[groupName] = group;
				groups.Add(group);
			}
			group.Children.Add(new ResultRow
			{
				Metric = childLabel,
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
	private async Task RefreshResultsTable()
	{
		if (ViewModel.ActiveTab != "Results")
			return;
		var selected = GetSelectedRecordings();
		if (selected.Count == 0)
		{
			ViewModel.ResultsRows.Clear();
			ViewModel.RecordingAHeader = "Recording A";
			ViewModel.RecordingBHeader = "Recording B";
			RecordingBColumn.IsHidden = true;
			return;
		}
		if (selected.Count > 2)
		{
			ViewModel.ResultsRows.Clear();
			ViewModel.RecordingAHeader = "Recording A";
			ViewModel.RecordingBHeader = "Recording B";
			RecordingBColumn.IsHidden = true;
			return;
		}
		ViewModel.RecordingAHeader = selected.Count >= 1 ? selected[0].Title : "Recording A";
		ViewModel.RecordingBHeader = selected.Count >= 2 ? selected[1].Title : "Recording B";
		RecordingBColumn.IsHidden = selected.Count < 2;
		var builtRows = await Task.Run(() =>
				{
					List<(RecordingItem item, DateTime lastWriteUtc, Dictionary<string, double> averages)> loaded = [];
					List<ResultRow> resultRows = [];
					foreach (var i in selected.Take(2))
					{
						var lastWriteUtc = File.Exists(i.FilePath) ? File.GetLastWriteTimeUtc(i.FilePath) : DateTime.MinValue;
						if (lastWriteUtc != DateTime.MinValue && LoadNumericAverages(i.FilePath, lastWriteUtc, out var avg))
							loaded.Add((i, lastWriteUtc, avg));
					}
					if (loaded.Count == 0)
						return (loaded, rows: resultRows);
					// Correct order: Mean, P0.1, P1, P5, P10, P50, P90, P95, P99, P99.9
					// Add Displayed FPS percentiles
					foreach (var percentileLabel in PercentileLabels)
					{
						string rowLabel = $"Displayed {percentileLabel} FPS";
						string recordingA = "-";
						string recordingB = loaded.Count < 2 ? string.Empty : "-";
						for (int c = 0; c < loaded.Count; c++)
						{
							var (item, lastWriteUtc, averages) = loaded[c];
							string value = GetFpsPercentileValue(item.FilePath, lastWriteUtc, "MsBetweenDisplayChange", percentileLabel);
							if (c == 0)
								recordingA = value;
							else
								recordingB = value;
						}
						resultRows.Add(new ResultRow { Metric = rowLabel, RecordingA = recordingA, RecordingB = recordingB });
					}
					// Add Displayed FPS time series stats
					resultRows.AddRange(
					[
					CreateTimeSeriesStatRow("Displayed CV", "MsBetweenDisplayChange", loaded, s => s.cv),
					CreateTimeSeriesStatRow("Displayed RMSSD", "MsBetweenDisplayChange", loaded, s => s.rmssd),
					CreateTimeSeriesStatRow("Displayed Stepwise-Relative", "MsBetweenDisplayChange", loaded, s => s.stepwiseRelSD)
					]);
					// Add MsBetweenDisplayChange stats
					resultRows.Add(CreateMetricRow("Average MsBetweenDisplayChange", "MsBetweenDisplayChange", loaded));
					resultRows.AddRange(
					[
					CreateTimeSeriesStatRow("MsBetweenDisplayChange SD", "MsBetweenDisplayChange", loaded, s => s.stdDev),
					CreateTimeSeriesStatRow("MsBetweenDisplayChange CV", "MsBetweenDisplayChange", loaded, s => s.cv),
					CreateTimeSeriesStatRow("MsBetweenDisplayChange RMSSD", "MsBetweenDisplayChange", loaded, s => s.rmssd),
					CreateTimeSeriesStatRow("MsBetweenDisplayChange Stepwise-Relative", "MsBetweenDisplayChange", loaded, s => s.stepwiseRelSD)
					]);
					// Add Rendered FPS percentiles
					foreach (var percentileLabel in PercentileLabels)
					{
						string rowLabel = $"Rendered {percentileLabel} FPS";
						string recordingA = "-";
						string recordingB = loaded.Count < 2 ? string.Empty : "-";
						for (int c = 0; c < loaded.Count; c++)
						{
							var (item, lastWriteUtc, averages) = loaded[c];
							string value = GetFpsPercentileValue(item.FilePath, lastWriteUtc, "MsBetweenPresents", percentileLabel);
							if (c == 0)
								recordingA = value;
							else
								recordingB = value;
						}
						resultRows.Add(new ResultRow { Metric = rowLabel, RecordingA = recordingA, RecordingB = recordingB });
					}
					// Add Rendered FPS time series stats
					resultRows.AddRange(
					[
					CreateTimeSeriesStatRow("Rendered CV", "MsBetweenPresents", loaded, s => s.cv),
					CreateTimeSeriesStatRow("Rendered RMSSD", "MsBetweenPresents", loaded, s => s.rmssd),
					CreateTimeSeriesStatRow("Rendered Stepwise-Relative", "MsBetweenPresents", loaded, s => s.stepwiseRelSD)
					]);
					// Add MsBetweenPresents stats
					resultRows.Add(CreateMetricRow("Average MsBetweenPresents", "MsBetweenPresents", loaded));
					resultRows.AddRange(
					[
					CreateTimeSeriesStatRow("MsBetweenPresents SD", "MsBetweenPresents", loaded, s => s.stdDev),
					CreateTimeSeriesStatRow("MsBetweenPresents CV", "MsBetweenPresents", loaded, s => s.cv),
					CreateTimeSeriesStatRow("MsBetweenPresents RMSSD", "MsBetweenPresents", loaded, s => s.rmssd),
					CreateTimeSeriesStatRow("MsBetweenPresents Stepwise-Relative", "MsBetweenPresents", loaded, s => s.stepwiseRelSD)
					]);
					// Add MsGPUBusy stats
					resultRows.Add(CreateMetricRow("Average MsGPUBusy", "MsGPUBusy", loaded));
					resultRows.AddRange(
					[
					CreateTimeSeriesStatRow("MsGPUBusy SD", "MsGPUBusy", loaded, s => s.stdDev),
					CreateTimeSeriesStatRow("MsGPUBusy CV", "MsGPUBusy", loaded, s => s.cv),
					CreateTimeSeriesStatRow("MsGPUBusy RMSSD", "MsGPUBusy", loaded, s => s.rmssd),
					CreateTimeSeriesStatRow("MsGPUBusy Stepwise-Relative", "MsGPUBusy", loaded, s => s.stepwiseRelSD)
					]);
					// Add MsUntilDisplayed stats
					resultRows.Add(CreateMetricRow("Average MsUntilDisplayed", "MsUntilDisplayed", loaded));
					resultRows.AddRange(
					[
					CreateTimeSeriesStatRow("MsUntilDisplayed SD", "MsUntilDisplayed", loaded, s => s.stdDev),
					CreateTimeSeriesStatRow("MsUntilDisplayed CV", "MsUntilDisplayed", loaded, s => s.cv),
					CreateTimeSeriesStatRow("MsUntilDisplayed RMSSD", "MsUntilDisplayed", loaded, s => s.rmssd),
					CreateTimeSeriesStatRow("MsUntilDisplayed Stepwise-Relative", "MsUntilDisplayed", loaded, s => s.stepwiseRelSD)
					]);
					ApplyResultComparisons(resultRows, loaded.Count == 2);
					return (loaded, rows: GroupResultRows(resultRows));
					// Helper functions
					ResultRow CreateMetricRow(string label, string metric, List<(RecordingItem item, DateTime lastWriteUtc, Dictionary<string, double> averages)> items)
					{
						string a = "-", b = items.Count < 2 ? string.Empty : "-";
						for (int i = 0; i < items.Count; i++)
						{
							var (item, lw, avg) = items[i];
							string val = HasResultMetric(item.FilePath, lw, metric)
								? FormatStat(GetMetricAverage(item.FilePath, lw, metric, avg), metric)
								: "-";
							if (i == 0) a = val; else b = val;
						}
						return new ResultRow { Metric = label, RecordingA = a, RecordingB = b };
					}
					ResultRow CreateTimeSeriesStatRow(string label, string metric, List<(RecordingItem item, DateTime lastWriteUtc, Dictionary<string, double> averages)> items, Func<(double stepwiseRelSD, double cv, double rmssd, double stdDev), double> selector)
					{
						string a = "-", b = items.Count < 2 ? string.Empty : "-";
						for (int i = 0; i < items.Count; i++)
						{
							var (item, lw, avg) = items[i];
							string val = LoadTimeSeriesStats(item.FilePath, lw, metric, out var stats)
								? FormatTimeSeriesStat(selector(stats), label)
								: "-";
							if (i == 0) a = val; else b = val;
						}
						return new ResultRow { Metric = label, RecordingA = a, RecordingB = b };
					}
				});
		if (ViewModel.ActiveTab != "Results")
			return;
		var (loadedItems, resultRows) = builtRows;
		if (loadedItems.Count == 0)
		{
			ViewModel.ResultsRows = [];
			return;
		}
		ViewModel.ResultsRows = [.. resultRows];
		ResultsTreeGrid.ExpandAllNodes();
	}
	private string GetFpsPercentileValue(string filePath, DateTime lastWriteUtc, string msMetricColumn, string percentileLabel)
	{
		var cacheKey = (filePath, lastWriteUtc, msMetricColumn);
		lock (_cacheLock)
		{
			_sortedFpsCache.TryGetValue(cacheKey, out var cached);
			if (cached is not null)
				return FormatFpsPercentile(cached, percentileLabel);
		}
		if (!LoadMetricColumn(filePath, lastWriteUtc, msMetricColumn, out var msValues))
			return "-";
		var fpsValues = msValues.Where(value => value > 0).Select(value => 1000.0 / value).Order().ToArray();
		if (fpsValues.Length == 0)
			return "-";
		lock (_cacheLock)
			_sortedFpsCache[cacheKey] = fpsValues;
		return FormatFpsPercentile(fpsValues, percentileLabel);

		static string FormatFpsPercentile(double[] fpsValues, string percentileLabel)
		{
			double value;
			if (percentileLabel == "Mean")
			{
				value = fpsValues.Average();
			}
			else
			{
				double percentile = double.Parse(percentileLabel.Replace("P", ""), CultureInfo.InvariantCulture);
				if (!TryGetPercentileFromSorted(fpsValues, percentile, out value))
					return "-";
			}
			return FormatStat(value, "Displayed FPS");
		}
	}
	private bool HasResultMetric(string filePath, DateTime lastWriteUtc, string metric)
			=> IsFpsMetric(metric)
				? HasMetricColumn(filePath, lastWriteUtc, GetFpsSourceColumn(metric))
				: HasMetricColumn(filePath, lastWriteUtc, metric);
	/// <summary>
	/// Returns the average value for a metric. FPS metrics are derived from their
	/// underlying millisecond column (1000 / avg ms).
	/// </summary>
	private double GetMetricAverage(string filePath, DateTime lastWriteUtc, string metric, Dictionary<string, double> averages)
	{
		string sourceColumn = GetFpsSourceColumn(metric);
		if (IsFpsMetric(metric))
		{
			if (LoadMetricColumn(filePath, lastWriteUtc, sourceColumn, out var values))
			{
				List<double> fpsValues = [.. values.Where(v => v > 0).Select(v => 1000.0 / v)];
				return fpsValues.Count > 0 ? fpsValues.Average() : 0;
			}
			return 0;
		}
		return averages.TryGetValue(metric, out var v) ? v : 0;
	}
	private static string FormatTimeSeriesStat(double value, string statLabel)
	{
		if (value == 0)
			return "—";
		if (statLabel.Contains("Rel.", StringComparison.OrdinalIgnoreCase) ||
					statLabel.StartsWith("CV", StringComparison.OrdinalIgnoreCase))
		{
			return value.ToString("0.#####", CultureInfo.InvariantCulture);
		}
		return value.ToString("0.####", CultureInfo.InvariantCulture);
	}
	private static string FormatStat(double value, string metric)
	{
		if (value == 0)
			return "—";
		if (IsFpsMetric(metric))
			return value.ToString("0.###", CultureInfo.InvariantCulture);
		// Default ms-like formatting
		return value.ToString("0.####", CultureInfo.InvariantCulture);
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
				headerIndex[NormalizeHeader(h)] = i;
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
				if (TryParseDouble(cols[idx], out var v))
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
	private static bool ResolveHeaderIndex(Dictionary<string, int> headerIndex, string metric, out int idx)
	{
		idx = -1;
		if (headerIndex.TryGetValue(metric, out idx))
			return true;
		var normalized = NormalizeHeader(metric);
		if (string.IsNullOrEmpty(normalized))
			return false;
		return headerIndex.TryGetValue(normalized, out idx);
	}
	private bool LoadNumericAverages(string filePath, DateTime lastWriteUtc, out Dictionary<string, double> averages)
	{
		averages = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
		if (!File.Exists(filePath))
			return false;
		var cacheKey = (filePath, lastWriteUtc);
		lock (_cacheLock)
		{
			if (_averagesCache.TryGetValue(cacheKey, out var cached))
			{
				averages = cached;
				return cached.Count > 0;
			}
		}
		if (!GetHeaderIndex(filePath, lastWriteUtc, out var headerIndex) || headerIndex.Count == 0)
			return false;

		Dictionary<string, (int Index, List<double> Values)> columns = new(StringComparer.OrdinalIgnoreCase);
		ReadOnlySpan<string> resultMetrics = ["MsBetweenDisplayChange", "MsBetweenPresents", "MsGPUBusy", "MsUntilDisplayed"];
		foreach (var metric in resultMetrics)
		{
			if (ResolveHeaderIndex(headerIndex, metric, out int index))
				columns[metric] = (index, new List<double>(4096));
		}
		if (columns.Count == 0)
			return false;

		try
		{
			using var reader = new StreamReader(filePath);
			_ = reader.ReadLine();
			while (!reader.EndOfStream)
			{
				var line = reader.ReadLine();
				if (string.IsNullOrWhiteSpace(line))
					continue;
				var cols = ParseCsvLine(line);
				foreach (var column in columns.Values)
				{
					if (column.Index < cols.Count && TryParseDouble(cols[column.Index], out var value))
						column.Values.Add(value);
				}
			}

			foreach (var (metric, column) in columns)
			{
				if (column.Values.Count > 0)
					averages[metric] = column.Values.Average();
			}
			lock (_cacheLock)
			{
				_averagesCache[cacheKey] = averages;
				foreach (var (metric, column) in columns)
					_columnCache[(filePath, lastWriteUtc, metric)] = column.Values;
			}
			return averages.Count > 0;
		}
		catch
		{
			return false;
		}
	}
	private bool LoadTimeSeriesStats(string filePath, DateTime lastWriteUtc, string metric, out (double stepwiseRelSD, double cv, double rmssd, double stdDev) stats)
	{
		stats = (0, 0, 0, 0);
		var cacheKey = (filePath, lastWriteUtc, metric);
		lock (_cacheLock)
		{
			if (_statsCache.TryGetValue(cacheKey, out var cached))
			{
				stats = cached;
				return true;
			}
		}
		if (!LoadMetricColumn(filePath, lastWriteUtc, metric, out var values))
			return false;
		if (!TryComputeTimeSeriesStats(values, out stats))
		{
			lock (_cacheLock)
				_statsCache[cacheKey] = stats;
			return false;
		}
		lock (_cacheLock)
			_statsCache[cacheKey] = stats;
		return true;
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
		else if (ReferenceEquals(selectedItem, ResultsTab))
		{
			ViewModel.ActiveTab = "Results";
			var selected = GetSelectedRecordings();
			ViewModel.SetSelectedRecordings(selected);
			if (selected.Count is > 0 and <= 2)
				await RefreshResultsTable();
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
