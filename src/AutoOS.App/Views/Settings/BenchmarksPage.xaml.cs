using System.Buffers;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices.WindowsRuntime;
using AutoOS.Core.Helpers.Benchmark;
using AutoOS.Core.Helpers.Benchmark.Models;
using AutoOS.Core.Helpers.Picker;
using AutoOS.Helpers.Picker;
using AutoOS.ViewModels;
using CommunityToolkit.WinUI.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using nietras.SeparatedValues;
using Syncfusion.UI.Xaml.Charts;
using Syncfusion.UI.Xaml.DataGrid;
using Syncfusion.UI.Xaml.Grids;
using Syncfusion.UI.Xaml.TreeGrid;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.System;
using Windows.UI.ViewManagement;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Media.Audio;

namespace AutoOS.Views.Settings;

public sealed record BarColumnChartData(
	List<BarPoint> DisplayedFpsBars1,
	List<BarPoint> RenderedFpsBars1,
	List<BarPoint> DisplayedFpsBars2,
	List<BarPoint> RenderedFpsBars2,
	bool ShowRenderedFps1,
	string DisplayedFpsLabel1, string RenderedFpsLabel1,
	string DisplayedFpsLabel2, string RenderedFpsLabel2
);

public sealed record LineScatterChartData(
	List<SeriesPoint> Pts1,
	List<SeriesPoint> Pts2,
	string Label1,
	string Label2);

public sealed record RecordingAnalysis(RecordingItem Recording, AnalysisResult Analysis);

public sealed partial class BenchmarksPage : Page
{
	public BenchmarksPageViewModel ViewModel { get; } = new();

	internal PresentMonProcessDiscovery PresentingProcesses { get; } = new();
	private GlobalKeyboardHook _globalKeyboardHook;
	private VirtualKeyModifiers _currentModifiers;
	private VirtualKey _currentKey;

	private Process _activeProcess;
	private CancellationTokenSource _recordingCts;

	public BenchmarksPage()
	{
		InitializeComponent();
		ApplyShortcut(ViewModel.ShortcutKeys);
		LoadRecordings();
	}

	protected override void OnNavigatedTo(NavigationEventArgs e)
	{
		base.OnNavigatedTo(e);
		_globalKeyboardHook = new GlobalKeyboardHook();
		_globalKeyboardHook.KeyDown += OnGlobalKeyDown;
		_globalKeyboardHook.Start();
		ViewModel.StatisticToggled += Statistic_SelectionChanged;
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
		ViewModel.StatisticToggled -= Statistic_SelectionChanged;
	}

	private void LoadRecordings()
	{
		if (!Directory.Exists(BenchmarkCsv.RecordingsDirectory))
		{
			Directory.CreateDirectory(BenchmarkCsv.RecordingsDirectory);
			ViewModel.SetRecordings([]);
			return;
		}

		List<FileInfo> csvFiles = [.. new DirectoryInfo(BenchmarkCsv.RecordingsDirectory).EnumerateFiles("*.csv")];

		if (csvFiles.Count == 0)
		{
			ViewModel.SetRecordings([]);
			return;
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

		ViewModel.SetRecordings(recordings);
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

	private void RecordingsTreeGrid_SelectionChanged(object sender, GridSelectionChangedEventArgs e)
	{
		ViewModel.SetSelectedRecordings(RecordingsTreeGrid.SelectedItems.OfType<RecordingItem>().Append(RecordingsTreeGrid.SelectedItem as RecordingItem).Where(recording => recording is not null).DistinctBy(recording => recording.FilePath, StringComparer.OrdinalIgnoreCase).ToList());
		BuildAnalysis();
		BuildStatistics();
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


	private async void AddRecording_Click(object sender, RoutedEventArgs e)
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

		var ext = Path.GetExtension(recording.FilePath);
		var newPath = Path.Combine(Path.GetDirectoryName(recording.FilePath), recording.Title + ext);
		if (newPath == recording.FilePath)
			return;

		File.Move(recording.FilePath, newPath);
		recording.FilePath = newPath;
		recording.FileName = recording.Title + ext;

		RecordingsTreeGrid_SelectionChanged(RecordingsTreeGrid, null);
	}

	private async void DeleteRecording_Click(object sender, RoutedEventArgs e)
	{
		var selected = RecordingsTreeGrid.SelectedItems.OfType<RecordingItem>().Append(RecordingsTreeGrid.SelectedItem as RecordingItem).Where(recording => recording is not null).DistinctBy(recording => recording.FilePath, StringComparer.OrdinalIgnoreCase).ToList();
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

	private void HotkeyShortcut_PrimaryButtonClick(object sender, ContentDialogButtonClickEventArgs e)
	{
		HotkeyShortcut.UpdatePreviewKeys();
		HotkeyShortcut.CloseContentDialog();
		ApplyShortcut(HotkeyShortcut.Keys);
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
				if (ViewModel.IsRecording)
					Record.IsChecked = false;
				else if (!string.IsNullOrWhiteSpace(ViewModel.ProcessName))
					Record.IsChecked = true;
			});
		}
	}
	
	private void ApplyShortcut(IEnumerable<object> keys)
	{
		_currentModifiers = VirtualKeyModifiers.None;
		_currentKey = VirtualKey.None;

		foreach (var key in keys)
		{
			string keyName;
			VirtualKey? virtKey = null;

			if (key is KeyVisualInfo info)
			{
				keyName = info.KeyName ?? string.Empty;
				virtKey = info.Key;
			}
			else
			{
				keyName = key?.ToString() ?? string.Empty;
			}

			if (keyName.Contains("Ctrl", StringComparison.OrdinalIgnoreCase))
				_currentModifiers |= VirtualKeyModifiers.Control;
			else if (keyName.Contains("Shift", StringComparison.OrdinalIgnoreCase))
				_currentModifiers |= VirtualKeyModifiers.Shift;
			else if (keyName.Contains("Alt", StringComparison.OrdinalIgnoreCase))
				_currentModifiers |= VirtualKeyModifiers.Menu;
			else if (keyName.Contains("Win", StringComparison.OrdinalIgnoreCase))
				_currentModifiers |= VirtualKeyModifiers.Windows;
			else if (virtKey.HasValue && virtKey.Value != VirtualKey.None)
				_currentKey = virtKey.Value;
			else if (Enum.TryParse<VirtualKey>(keyName, ignoreCase: true, out var parsed) &&
				parsed != VirtualKey.None)
				_currentKey = parsed;
		}
	}

	private async void Record_Checked(object sender, RoutedEventArgs e)
	{
		_recordingCts?.Cancel();
		var cts = new CancellationTokenSource();
		_recordingCts = cts;

		ViewModel.IsRecording = true;
		Record.IsChecked = true;

		int delay = (int)ViewModel.RecordingDelay;
		int duration = (int)ViewModel.RecordingDuration;

		ViewModel.ShowRecordingCountdown(delay);

		var delayTcs = new TaskCompletionSource();
		var countdownTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
		long start = Stopwatch.GetTimestamp();
		countdownTimer.Tick += (s, args) =>
		{
			if (cts.IsCancellationRequested)
			{
				countdownTimer.Stop();
				delayTcs.TrySetResult();
				return;
			}
			double elapsed = (Stopwatch.GetTimestamp() - start) / (double)Stopwatch.Frequency;
			if (elapsed < delay)
				ViewModel.RecordingCountdown = Math.Max(0, delay - elapsed);
			else
			{
				countdownTimer.Stop();
				delayTcs.TrySetResult();
			}
		};
		countdownTimer.Start();
		await delayTcs.Task;

		if (cts.IsCancellationRequested)
		{
			if (_activeProcess == null)
			{
				ViewModel.IsRecording = false;
				Record.IsChecked = false;
			}
			return;
		}

		int recordingNumber = 1;
		string outputPath;
		do
		{
			outputPath = Path.Combine(BenchmarkCsv.RecordingsDirectory, $"Recording-{recordingNumber++}.csv");
		}
		while (File.Exists(outputPath));

		Directory.CreateDirectory(BenchmarkCsv.RecordingsDirectory);

		var startInfo = new ProcessStartInfo
		{
			FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "PresentMon", "PresentMon-x64.exe"),
			Arguments = @$"-session_name AutoOS_{Guid.NewGuid():N} -process_name ""{ViewModel.ProcessName}"" -timed {duration} -terminate_after_timed -date_time -track_gpu_video -track_frame_type -track_hw_measurements -track_app_timing -track_pc_latency -output_file ""{outputPath}""",
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			CreateNoWindow = true
		};

		var process = Process.Start(startInfo);
		_activeProcess = process;

		var stdOutTask = process.StandardOutput.ReadToEndAsync();
		var stdErrTask = process.StandardError.ReadToEndAsync();

		ViewModel.ShowRecording();

		start = Stopwatch.GetTimestamp();
		var recordingTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
		recordingTimer.Tick += (s, args) =>
		{
			if (cts.IsCancellationRequested)
			{
				recordingTimer.Stop();
				return;
			}
			double elapsed = (Stopwatch.GetTimestamp() - start) / (double)Stopwatch.Frequency;
			ViewModel.RecordingRemaining = Math.Max(0, duration - elapsed);
			if (process.HasExited)
				recordingTimer.Stop();
		};
		recordingTimer.Start();

		try
		{
			await process.WaitForExitAsync();

			if (cts.IsCancellationRequested)
			{
				if (File.Exists(outputPath))
					File.Delete(outputPath);
				return;
			}

			if (!File.Exists(outputPath) || new FileInfo(outputPath).Length == 0)
			{
				string stdOut = await stdOutTask;
				string stdErr = await stdErrTask;
				throw new InvalidOperationException($"PresentMon exited (code {process.ExitCode}) without producing a recording file.\nstdout: {stdOut}\nstderr: {stdErr}");
			}
			PInvoke.PlaySound(@"C:\Windows\Media\Alarm09.wav", null, SND_FLAGS.SND_FILENAME | SND_FLAGS.SND_ASYNC);
		}
		catch (Exception ex)
		{
			await MessageBox.ShowErrorAsync(App.MainWindow, $"An error occurred while recording: {ex.Message}", "Recording Error");
			return;
		}
		finally
		{
			recordingTimer.Stop();
			if (_activeProcess == process)
			{
				_activeProcess = null;
				ViewModel.IsRecording = false;
				Record.IsChecked = false;
			}
			LoadRecordings();
		}
	}

	private void Record_Unchecked(object sender, RoutedEventArgs e)
	{
		if (!ViewModel.IsRecording)
			return;
		_recordingCts?.Cancel();
		if (_activeProcess is { HasExited: false })
		{
			var found = false;
			var hwnd = HWND.Null;
			while ((hwnd = PInvoke.FindWindowEx((HWND)(IntPtr)(-3), hwnd, "PresentMon", "PresentMonWnd")) != HWND.Null)
			{
				PInvoke.GetWindowThreadProcessId(hwnd, out uint pid);
				if (pid == _activeProcess.Id)
				{
					PInvoke.PostMessage(hwnd, PInvoke.WM_CLOSE, 0, 0);
					found = true;
					break;
				}
			}
			if (!found)
				_activeProcess.Kill(true);
		}
		ViewModel.IsRecording = false;
		ViewModel.RecordingState = ViewModel.Recordings.Count == 0 ? "Empty" : "Content";
	}

	private void Statistic_SelectionChanged()
	{
		var presentation = BuildBarColumnChartData(ViewModel.CachedAnalysis);
		BindBarColumnChart(presentation);
	}

	private void MetricComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		var metric = (Metric1ComboBox.SelectedItem as ComboBoxItem)?.Content as string ?? string.Empty;
		var data = BuildLineScatterChartData(ViewModel.CachedAnalysis, metric);
		BindLineScatterChart(data.Pts1, data.Pts2, data.Label1, data.Label2);
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

	private void StatisticsBaselineComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		ViewModel.IsDeltaModeEnabled = ViewModel.BaselineSelectedIndex >= 1;
		RefreshStatisticsDelta();
	}

	private void StatisticsDeltaModeSelector_SelectionChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
	{
		ViewModel.IsPercentDelta = sender.SelectedItem == PercentDeltaItem;
		RefreshStatisticsDelta();
	}

	private void RefreshStatisticsDelta()
	{
		List<ResultRow> rows = [.. ViewModel.StatisticsRows.SelectMany(group => group.Children)];
		foreach (ResultRow row in rows)
		{
			row.Delta = string.Empty;
			row.RecordingAComparison = null;
			row.RecordingBComparison = null;
			row.DeltaComparison = null;
		}

		int baselineIdx = ViewModel.BaselineSelectedIndex - 1;
		if (baselineIdx is 0 or 1)
			ApplyResultDeltas(rows, baselineIdx, ViewModel.IsPercentDelta);
		ApplyResultComparisons(rows, ViewModel.SelectedRecordings.Count == 2);
		ConfigureStatisticsColumns();
	}

	private void StopProcessDiscovery()
	{
		PresentingProcesses.ProcessesChanged -= ProcessDiscovery_ProcessesChanged;
		PresentingProcesses.Dispose();
	}

	private void ProcessComboBox_DropDownOpened(object sender, object e)
	{
		PresentingProcesses.Start();
		ViewModel.SetRecordableProcesses(PresentingProcesses.GetRecordableProcesses(true));
		PresentingProcesses.ProcessesChanged += ProcessDiscovery_ProcessesChanged;
	}

	private void ProcessComboBox_DropDownClosed(object sender, object e)
	{
		StopProcessDiscovery();
	}

	private void ProcessDiscovery_ProcessesChanged(object sender, EventArgs e)
	{
		DispatcherQueue.TryEnqueue(() =>
		{
			ViewModel.SetRecordableProcesses(PresentingProcesses.GetRecordableProcesses());
		});
	}

	private void BenchmarksSelectorBar_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
	{
		if (ViewModel.IsRecording && (args.SelectedItem is not TabbedCommandBarItem selectedItem || selectedItem != RecordingsTab))
		{
			BenchmarksSelectorBar.SelectedItem = RecordingsTab;
			return;
		}

		switch (args.SelectedItem ?? sender.SelectedItem)
		{
			case TabbedCommandBarItem item when item == RecordingsTab:
				ViewModel.ActiveTab = "Recordings";
				break;

			case TabbedCommandBarItem item when item == AnalysisTab:
				ViewModel.ActiveTab = "Analysis";
				ViewModel.AnalysisChartType = "Bar";
				if (ViewModel.IsAnalysisToolbarEnabled)
					ReplayAnimation();
				break;

			case TabbedCommandBarItem item when item == StatisticsTab:
				ViewModel.ActiveTab = "Statistics";
				break;
		}
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

	private void BuildAnalysis()
	{
		if (ViewModel.SelectedRecordings.Count is 0 or > 2)
			return;

		var results = ViewModel.SelectedRecordings
			.Select(recording => (Item: recording, Result: RecordingAnalyzer.Analyze(recording.FilePath)))
			.Where(recording => recording.Result != null)
			.Select(recording => new RecordingAnalysis(recording.Item, recording.Result))
			.ToList();

		ViewModel.CachedAnalysis = results;

		var metric = (Metric1ComboBox.SelectedItem as ComboBoxItem)?.Content as string;
		var presentation = BuildBarColumnChartData(results);
		BindBarColumnChart(presentation);

		var data = BuildLineScatterChartData(results, metric);
		BindLineScatterChart(data.Pts1, data.Pts2, data.Label1, data.Label2);
	}

	private static BarColumnChartData BuildBarColumnChartData(List<RecordingAnalysis> results)
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

		int fpsSeriesIdx = 0;
		foreach (var result in results)
		{
			List<BarPoint> displayedTarget = fpsSeriesIdx == 0 ? displayedFpsBars1 : displayedFpsBars2;
			List<BarPoint> renderedTarget = fpsSeriesIdx == 0 ? renderedFpsBars1 : renderedFpsBars2;

			foreach (string percentile in BenchmarkCsv.StatisticLabelsShort)
			{
				displayedTarget.Add(new BarPoint { Label = percentile, Value = BenchmarkCsv.GetStatistic(result.Analysis.DisplayedFps, percentile) });
				renderedTarget.Add(new BarPoint { Label = percentile, Value = BenchmarkCsv.GetStatistic(result.Analysis.RenderedFps, percentile) });
			}

			if (fpsSeriesIdx == 0)
			{
				displayedFpsLabel1 = $"{result.Recording.FileName} · Displayed FPS";
				renderedFpsLabel1 = $"{result.Recording.FileName} · Rendered FPS";
				showRenderedFps1 = renderedTarget.Count > 0;
			}
			else
			{
				displayedFpsLabel2 = $"{result.Recording.FileName} · Displayed FPS";
				renderedFpsLabel2 = $"{result.Recording.FileName} · Rendered FPS";
			}
			fpsSeriesIdx++;
		}

		return new BarColumnChartData(displayedFpsBars1, renderedFpsBars1, displayedFpsBars2, renderedFpsBars2, showRenderedFps1, displayedFpsLabel1, renderedFpsLabel1, displayedFpsLabel2, renderedFpsLabel2);
	}

	private static LineScatterChartData BuildLineScatterChartData(List<RecordingAnalysis> results, string metric)
	{
		List<SeriesPoint> metricPts1 = [];
		List<SeriesPoint> metricPts2 = [];
		string metricLabel1 = string.Empty;
		string metricLabel2 = string.Empty;

		int index = 0;
		foreach (var result in results)
		{
			IReadOnlyList<double> rawValues = metric switch
			{
				"MsBetweenDisplayChange" => result.Analysis.MsBetweenDisplayChange,
				"MsBetweenPresents" => result.Analysis.MsBetweenPresents,
				"MsGPUBusy" => result.Analysis.MsGPUBusy,
				"MsUntilDisplayed" => result.Analysis.MsUntilDisplayed,
				_ => []
			};

			var points = new List<SeriesPoint>(rawValues.Count);
			for (int i = 0; i < rawValues.Count; i++)
				points.Add(new SeriesPoint { Index = i + 1, Value = rawValues[i] });

			if (index == 0)
			{
				metricPts1 = points;
				metricLabel1 = $"{result.Recording.FileName} · {metric}";
			}
			else
			{
				metricPts2 = points;
				metricLabel2 = $"{result.Recording.FileName} · {metric}";
			}
			index++;
		}

		return new LineScatterChartData(metricPts1, metricPts2, metricLabel1, metricLabel2);
	}

	private void BindBarColumnChart(BarColumnChartData presentation)
	{
		bool hasSecondRecording = ViewModel.HasTwoRecordings;

		ViewModel.BarColumnChartDisplayedData1 = null;
		ViewModel.BarColumnChartRenderedData1 = null;
		ViewModel.BarColumnChartDisplayedData2 = null;
		ViewModel.BarColumnChartRenderedData2 = null;
		ViewModel.BarColumnChartDisplayedLabel1 = presentation.DisplayedFpsLabel1;
		ViewModel.BarColumnChartRenderedLabel1 = presentation.RenderedFpsLabel1;
		ViewModel.BarColumnChartDisplayedLabel2 = presentation.DisplayedFpsLabel2;
		ViewModel.BarColumnChartRenderedLabel2 = presentation.RenderedFpsLabel2;
		ViewModel.BarColumnRenderedVisible = presentation.ShowRenderedFps1;

		var displayed1 = presentation.DisplayedFpsBars1.Where(bar => ViewModel.IsStatisticEnabled(bar.Label)).ToList();
		var rendered1 = presentation.RenderedFpsBars1.Where(bar => ViewModel.IsStatisticEnabled(bar.Label)).ToList();
		var displayed2 = presentation.DisplayedFpsBars2.Where(bar => ViewModel.IsStatisticEnabled(bar.Label)).ToList();
		var rendered2 = presentation.RenderedFpsBars2.Where(bar => ViewModel.IsStatisticEnabled(bar.Label)).ToList();

		ViewModel.BarColumnChartDisplayedData1 = [.. displayed1];
		ViewModel.BarColumnChartRenderedData1 = presentation.ShowRenderedFps1 ? [.. rendered1] : null;
		ViewModel.BarColumnChartDisplayedData2 = hasSecondRecording ? [.. displayed2] : null;
		ViewModel.BarColumnChartRenderedData2 = hasSecondRecording ? [.. rendered2] : null;

		if (BarChart != null)
		{
			BarChart.Series.Clear();
			BarChart.Series.Add(BarDisplayedFpsSeries1);

			if (presentation.ShowRenderedFps1)
				BarChart.Series.Add(BarRenderedFpsSeries1);

			if (hasSecondRecording)
			{
				BarChart.Series.Add(BarDisplayedFpsSeries2);
				BarChart.Series.Add(BarRenderedFpsSeries2);
			}

			BarDisplayedFpsSeries1.ShowDataLabels = false;
			BarDisplayedFpsSeries1.ShowDataLabels = true;
			BarRenderedFpsSeries1.ShowDataLabels = false;
			BarRenderedFpsSeries1.ShowDataLabels = presentation.ShowRenderedFps1;
			BarDisplayedFpsSeries2.ShowDataLabels = false;
			BarDisplayedFpsSeries2.ShowDataLabels = hasSecondRecording;
			BarRenderedFpsSeries2.ShowDataLabels = false;
			BarRenderedFpsSeries2.ShowDataLabels = hasSecondRecording;

			BarChart.IsTransposed = false;
			BarChart.IsTransposed = true;
		}

		if (ColumnChart != null)
		{
			ColumnChart.Series.Clear();
			ColumnChart.Series.Add(ColumnDisplayedFpsSeries1);

			if (presentation.ShowRenderedFps1)
				ColumnChart.Series.Add(ColumnRenderedFpsSeries1);

			if (hasSecondRecording)
			{
				ColumnChart.Series.Add(ColumnDisplayedFpsSeries2);
				ColumnChart.Series.Add(ColumnRenderedFpsSeries2);
			}

			ColumnDisplayedFpsSeries1.ShowDataLabels = false;
			ColumnDisplayedFpsSeries1.ShowDataLabels = true;
			ColumnRenderedFpsSeries1.ShowDataLabels = false;
			ColumnRenderedFpsSeries1.ShowDataLabels = presentation.ShowRenderedFps1;
			ColumnDisplayedFpsSeries2.ShowDataLabels = false;
			ColumnDisplayedFpsSeries2.ShowDataLabels = hasSecondRecording;
			ColumnRenderedFpsSeries2.ShowDataLabels = false;
			ColumnRenderedFpsSeries2.ShowDataLabels = hasSecondRecording;
		}
	}

	private void BindLineScatterChart(List<SeriesPoint> metricPts1, List<SeriesPoint> metricPts2, string metricLabel1, string metricLabel2)
	{
		ViewModel.LineScatterChartData1 = [];
		ViewModel.LineScatterChartData2 = [];
		ViewModel.LineScatterChartLabel1 = metricLabel1;
		ViewModel.LineScatterChartLabel2 = metricLabel2;
		ViewModel.LineScatterChartData1 = [.. metricPts1];
		ViewModel.LineScatterChartData2 = [.. metricPts2];
	}

	private static async Task SaveChartAsync(SfCartesianChart chart, string suggestedFileName, Guid encoderId, string extension, bool flattenBackground)
	{
		var picker = new SavePicker(App.MainWindow)
		{
			DefaultFileExtension = $"{extension}",
			ShowAllFilesOption = false,
			SuggestedFileName = suggestedFileName,
			InitialDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads"),
		};
		picker.FileTypeChoices.Add($"{extension} image", [$"*.{extension}"]);

		string filePath = picker.PickSaveFile();
		if (string.IsNullOrWhiteSpace(filePath))
			return;
		if (!filePath.EndsWith($".{extension}", StringComparison.OrdinalIgnoreCase))
			filePath += $".{extension}";

		var bitmap = new RenderTargetBitmap();
		await bitmap.RenderAsync(chart);
		if (bitmap.PixelWidth == 0 || bitmap.PixelHeight == 0)
			return;

		var pixels = await bitmap.GetPixelsAsync();
		byte[] pixelData = pixels.ToArray();

		if (flattenBackground)
		{
			var background = new UISettings().GetColorValue(UIColorType.Background);
			for (int i = 0; i < pixelData.Length; i += 4)
			{
				int alpha = pixelData[i + 3];
				if (alpha < 255)
				{
					int inverseAlpha = 255 - alpha;
					pixelData[i] = (byte)(pixelData[i] + background.B * inverseAlpha / 255);
					pixelData[i + 1] = (byte)(pixelData[i + 1] + background.G * inverseAlpha / 255);
					pixelData[i + 2] = (byte)(pixelData[i + 2] + background.R * inverseAlpha / 255);
					pixelData[i + 3] = 255;
				}
			}
		}

		StorageFolder folder = await StorageFolder.GetFolderFromPathAsync(Path.GetDirectoryName(filePath));
		StorageFile file = await folder.CreateFileAsync(Path.GetFileName(filePath), CreationCollisionOption.ReplaceExisting);
		using var stream = await file.OpenAsync(FileAccessMode.ReadWrite);
		BitmapEncoder encoder = await BitmapEncoder.CreateAsync(encoderId, stream);
		encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied, (uint)bitmap.PixelWidth, (uint)bitmap.PixelHeight, 96, 96, pixelData);
		await encoder.FlushAsync();
	}

	private void Chart_RightTapped(object sender, RightTappedRoutedEventArgs e)
	{
		if (sender is SfCartesianChart chart)
		{
			string[] stats;
			if (ViewModel.AnalysisChartType is "Bar" or "Column")
			{
				bool displayed1, rendered1, displayed2, rendered2;
				if (chart == BarChart)
				{
					displayed1 = BarDisplayedFpsSeries1.Visibility == Visibility.Visible;
					rendered1 = BarRenderedFpsSeries1.Visibility == Visibility.Visible;
					displayed2 = ViewModel.SelectedRecordings.Count > 1 && BarDisplayedFpsSeries2.Visibility == Visibility.Visible;
					rendered2 = ViewModel.SelectedRecordings.Count > 1 && BarRenderedFpsSeries2.Visibility == Visibility.Visible;
				}
				else
				{
					displayed1 = ColumnDisplayedFpsSeries1.Visibility == Visibility.Visible;
					rendered1 = ColumnRenderedFpsSeries1.Visibility == Visibility.Visible;
					displayed2 = ViewModel.SelectedRecordings.Count > 1 && ColumnDisplayedFpsSeries2.Visibility == Visibility.Visible;
					rendered2 = ViewModel.SelectedRecordings.Count > 1 && ColumnRenderedFpsSeries2.Visibility == Visibility.Visible;
				}
				stats = new string[ViewModel.SelectedRecordings.Count];
				for (int i = 0; i < ViewModel.SelectedRecordings.Count; i++)
				{
					var s = new List<string>();
					bool isFirst = i == 0;
					if ((isFirst && displayed1) || (!isFirst && displayed2)) s.Add("Displayed FPS");
					if ((isFirst && rendered1) || (!isFirst && rendered2)) s.Add("Rendered FPS");
					stats[i] = string.Join(", ", s);
				}
			}
			else
			{
				stats = [.. ViewModel.SelectedRecordings.Select((recording, index) =>
				{
					if (index < chart.Series.Count && chart.Series[index].Visibility == Visibility.Visible)
						return (Metric1ComboBox.SelectedItem as ComboBoxItem)?.Content as string ?? string.Empty;
					return string.Empty;
				})];
			}
			string recordingNames = string.Join(" vs ", ViewModel.SelectedRecordings.Select((recording, index) => string.IsNullOrEmpty(stats[index]) ? recording.Title : $"{recording.Title} ({stats[index]})"));
			string chartLabel = $"{ViewModel.AnalysisChartType} Chart";
			string fileName = $"{recordingNames} - {chartLabel}";
			var flyout = new MenuFlyout();

			var jpegItem = new MenuFlyoutItem
			{
				Text = "Save as JPG",
				Icon = new FontIcon { Glyph = "\uE896" }
			};
			jpegItem.Click += async (sender, args) => await SaveChartAsync(chart, fileName, BitmapEncoder.JpegEncoderId, "jpg", true);
			flyout.Items.Add(jpegItem);

			var pngItem = new MenuFlyoutItem
			{
				Text = "Save as PNG",
				Icon = new FontIcon { Glyph = "\uE896" }
			};
			pngItem.Click += async (sender, args) => await SaveChartAsync(chart, fileName, BitmapEncoder.PngEncoderId, "png", false);
			flyout.Items.Add(pngItem);

			flyout.ShowAt(chart, e.GetPosition(chart));
		}
	}

	private void ConfigureStatisticsColumns()
	{
		bool showRecordingB = ViewModel.SelectedRecordings.Count == 2;
		int baselineIdx = ViewModel.BaselineSelectedIndex - 1;
		int baselineIndex = showRecordingB && baselineIdx is 0 or 1 ? baselineIdx : -1;

		StatisticsTreeGrid.Columns.Remove(StatisticsRecordingAColumn);
		StatisticsTreeGrid.Columns.Remove(StatisticsRecordingBColumn);
		StatisticsTreeGrid.Columns.Remove(StatisticsDeltaColumn);
		if (baselineIndex == 1)
			StatisticsTreeGrid.Columns.Add(StatisticsRecordingBColumn);
		else
			StatisticsTreeGrid.Columns.Add(StatisticsRecordingAColumn);
		if (baselineIndex < 0 && showRecordingB)
			StatisticsTreeGrid.Columns.Add(StatisticsRecordingBColumn);
		if (baselineIndex >= 0)
			StatisticsTreeGrid.Columns.Add(StatisticsDeltaColumn);

		if (baselineIndex >= 0)
			ViewModel.DeltaHeader = $"{ViewModel.SelectedRecordings[baselineIndex == 0 ? 1 : 0].Title} (Delta)";
		else
			ViewModel.DeltaHeader = ViewModel.IsPercentDelta ? "Delta (%)" : "Delta (+/-)";
		ViewModel.RecordingAHeader = ViewModel.SelectedRecordings.Count >= 1 ? ViewModel.SelectedRecordings[0].Title + (baselineIndex == 0 ? " (Baseline)" : string.Empty) : "Recording A";
		ViewModel.RecordingBHeader = ViewModel.SelectedRecordings.Count >= 2 ? ViewModel.SelectedRecordings[1].Title + (baselineIndex == 1 ? " (Baseline)" : string.Empty) : "Recording B";
	}

	private void BuildStatistics()
	{
		ConfigureStatisticsColumns();

		if (ViewModel.SelectedRecordings.Count == 0)
		{
			ViewModel.StatisticsRows.Clear();
			ViewModel.RecordingAHeader = "Recording A";
			ViewModel.RecordingBHeader = "Recording B";
			return;
		}

		if (ViewModel.SelectedRecordings.Count > 2)
		{
			ViewModel.StatisticsRows.Clear();
			ViewModel.RecordingAHeader = "Recording A";
			ViewModel.RecordingBHeader = "Recording B";
			return;
		}

		var results = ViewModel.CachedAnalysis;
		if (results.Count == 0)
		{
			ViewModel.StatisticsRows = [];
			return;
		}

		List<ResultRow> rows = [];
		rows.AddRange(BuildFpsStatRows("Displayed", results, result => result.Analysis.DisplayedFps));
		rows.AddRange(BuildFpsStatRows("Rendered", results, result => result.Analysis.RenderedFps));
		rows.AddRange(BuildLatencyStatRows("MsBetweenDisplayChange", results, result => result.Analysis.MsBetweenDisplayChangeStats));
		rows.AddRange(BuildLatencyStatRows("MsBetweenPresents", results, result => result.Analysis.MsBetweenPresentsStats));
		rows.AddRange(BuildLatencyStatRows("MsGPUBusy", results, result => result.Analysis.MsGpuBusyStats));
		rows.AddRange(BuildLatencyStatRows("MsUntilDisplayed", results, result => result.Analysis.MsUntilDisplayedStats));

		ApplyResultComparisons(rows, results.Count == 2);
		var builtRows = GroupResultRows(rows);

		if (builtRows.Count == 0)
		{
			ViewModel.StatisticsRows = [];
			return;
		}

		ViewModel.StatisticsRows = [.. builtRows];
		RefreshStatisticsDelta();
		StatisticsTreeGrid.ExpandAllNodes();
	}

	private static List<ResultRow> BuildFpsStatRows(string prefix, List<RecordingAnalysis> results, Func<RecordingAnalysis, Metrics> selector)
	{
		var m0 = results.Count > 0 ? selector(results[0]) : null;
		if (m0 == null || m0.AvgArithmetic == 0)
			return [];
		var m1 = results.Count > 1 ? selector(results[1]) : null;

		List<ResultRow> rows = [];
		foreach (var label in BenchmarkCsv.StatisticLabels)
		{
			double av = BenchmarkCsv.GetStatistic(m0, label);
			string a = av.ToString("0.###", CultureInfo.CurrentCulture) + " FPS";
			string b = m1 == null ? "" : BenchmarkCsv.GetStatistic(m1, label).ToString("0.###", CultureInfo.CurrentCulture) + " FPS";
			rows.Add(new ResultRow
			{
				Statistic = $"{prefix} {label} FPS",
				RecordingA = a,
				RecordingB = b
			});
		}

		static string fmt(double value, string format) => value == 0 ? "\u2014" : value.ToString(format, CultureInfo.CurrentCulture);

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

	private static List<ResultRow> BuildLatencyStatRows(string prefix, List<RecordingAnalysis> results, Func<RecordingAnalysis, Metrics> selector)
	{
		var m0 = results.Count > 0 ? selector(results[0]) : null;
		var m1 = results.Count > 1 ? selector(results[1]) : null;
		if (m0 == null || m0.AvgArithmetic == 0)
			return [];

		List<ResultRow> rows = [];

		static string fmtMs(double value) => value.ToString("0.####", CultureInfo.CurrentCulture) + " ms";
		static string fmtSd(double value) => value == 0 ? "\u2014" : value.ToString("0.####", CultureInfo.CurrentCulture) + " ms";
		static string fmtRel(double value) => value == 0 ? "\u2014" : value.ToString("0.#####", CultureInfo.CurrentCulture);

		void add(string label, string recordingA, string recordingB) => rows.Add(new ResultRow { Statistic = $"{prefix} {label}", RecordingA = recordingA, RecordingB = recordingB });

		add("Average (Arithmetic)", fmtMs(m0.AvgArithmetic), m1 == null ? "" : fmtMs(m1.AvgArithmetic));
		add("P50 (Median)", fmtMs(m0.P50Median), m1 == null ? "" : fmtMs(m1.P50Median));
		add("P95", fmtMs(m0.P5), m1 == null ? "" : fmtMs(m1.P5));
		add("P99", fmtMs(m0.P1), m1 == null ? "" : fmtMs(m1.P1));
		add("Maximum", fmtMs(m0.Max), m1 == null ? "" : fmtMs(m1.Max));
		add("Minimum", fmtMs(m0.Min), m1 == null ? "" : fmtMs(m1.Min));

		string fmtPct(double value) => value == 0 ? "\u2014" : value.ToString("0.0", CultureInfo.CurrentCulture) + "%";
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
			return double.TryParse(trimmed, NumberStyles.Float, CultureInfo.CurrentCulture, out result);
		}

		foreach (var row in rows)
		{
			if (!tryParse(row.RecordingA, out double recordingA) || !tryParse(row.RecordingB, out double recordingB) || recordingA == recordingB)
				continue;
			bool higherIsBetter = row.Statistic.EndsWith(" FPS", StringComparison.Ordinal);
			bool recordingAIsBetter = higherIsBetter ? recordingA > recordingB : recordingA < recordingB;
			row.RecordingAComparison = recordingAIsBetter ? "Better" : "Worse";
			row.RecordingBComparison = recordingAIsBetter ? "Worse" : "Better";
		}
	}

	private static void ApplyResultDeltas(IEnumerable<ResultRow> rows, int baselineIndex, bool showPercentDelta)
	{
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
			return double.TryParse(trimmed, NumberStyles.Float, CultureInfo.CurrentCulture, out result);
		}

		static string signed(double v, string f, string u)
		{
			string s = v > 0 ? "+ " : v < 0 ? "- " : "";
			return s + Math.Abs(v).ToString(f, CultureInfo.InvariantCulture) + u;
		}

		foreach (ResultRow row in rows)
		{
			string baselineText = baselineIndex == 0 ? row.RecordingA : row.RecordingB;
			string comparisonText = baselineIndex == 0 ? row.RecordingB : row.RecordingA;
			if (!tryParse(baselineText, out double baseline) ||
				!tryParse(comparisonText, out double comparison))
			{
				continue;
			}

			double delta = comparison - baseline;
			if (delta != 0)
			{
				string baselineComparison = delta > 0 ? "Worse" : "Better";
				if (baselineIndex == 0)
					row.RecordingAComparison = baselineComparison;
				else
					row.RecordingBComparison = baselineComparison;
				row.DeltaComparison = delta > 0 ? "Better" : "Worse";
			}

			if (showPercentDelta)
			{
				if (baseline == 0)
					continue;
				delta = delta / Math.Abs(baseline) * 100;
				row.Delta = signed(delta, "0.##", "%");
				continue;
			}

			if (baselineText.EndsWith(" FPS", StringComparison.Ordinal))
				row.Delta = signed(delta, "0.###", " FPS");
			else if (baselineText.EndsWith(" ms", StringComparison.Ordinal))
				row.Delta = signed(delta, "0.####", " ms");
			else if (baselineText.EndsWith('%'))
				row.Delta = signed(delta, "0.0", " pp");
			else
				row.Delta = signed(delta, "0.#####", string.Empty);
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

		static string getGroupTooltip(string group) => BenchmarkCsv.MetricDescriptions.TryGetValue(group, out var tip) ? tip : "Benchmark statistic.";

		List<ResultRow> groups = [];
		var groupLookup = new Dictionary<string, ResultRow>(StringComparer.Ordinal);
		foreach (var row in rows)
		{
			var groupName = getGroup(row.Statistic);
			var childLabel = getChildLabel(row.Statistic, groupName);
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
				Delta = row.Delta,
				RecordingAComparison = row.RecordingAComparison,
				RecordingBComparison = row.RecordingBComparison,
				DeltaComparison = row.DeltaComparison
			});
		}
		return groups;
	}
}
