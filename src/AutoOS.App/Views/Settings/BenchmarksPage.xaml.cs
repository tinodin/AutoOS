using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices.WindowsRuntime;
using AutoOS.Core.Helpers.Benchmark;
using AutoOS.Core.Helpers.Benchmark.Models;
using AutoOS.Helpers.Picker;
using AutoOS.App.ViewModels;
using CommunityToolkit.WinUI.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using Syncfusion.UI.Xaml.Charts;
using Syncfusion.UI.Xaml.DataGrid;
using Syncfusion.UI.Xaml.Grids;
using Syncfusion.UI.Xaml.TreeGrid;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.System;
using Windows.UI;
using Windows.UI.ViewManagement;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Media.Audio;

namespace AutoOS.App.Views.Settings;

public sealed record BarColumnChartData(
	List<BarPoint> DisplayedFpsBars1,
	List<BarPoint> RenderedFpsBars1,
	List<BarPoint> DisplayedFpsBars2,
	List<BarPoint> RenderedFpsBars2,
	string DisplayedFpsLabel1, string RenderedFpsLabel1,
	string DisplayedFpsLabel2, string RenderedFpsLabel2
);

public sealed record LineScatterChartData(
	List<SeriesPoint> Pts1,
	List<SeriesPoint> Pts2,
	string Label1,
	string Label2);

public sealed record PiePoint(string Label, double Value);

public sealed record PieChartData(List<PiePoint> Data1, List<PiePoint> Data2);

public sealed record RecordingAnalysis(RecordingItem Recording, AnalysisResult Analysis);

public sealed partial class BenchmarksPage : Page
{
	public BenchmarksPageViewModel ViewModel { get; } = new();

	internal PresentMonProcessDiscovery PresentingProcesses { get; } = new();
	private GlobalKeyboardHook? _globalKeyboardHook;
	private Process? _activeProcess;
	private CancellationTokenSource? _recordingCts;

	public BenchmarksPage()
	{
		InitializeComponent();
	}

	protected override void OnNavigatedTo(NavigationEventArgs e)
	{
		base.OnNavigatedTo(e);
		_globalKeyboardHook = new GlobalKeyboardHook();
		_globalKeyboardHook.KeyDown += OnGlobalKeyDown;
		_globalKeyboardHook.Start();
		ViewModel.StatisticToggled += Statistic_SelectionChanged;
		ViewModel.LoadSettings();
		_ = ViewModel.LoadRecordingsAsync();
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
		ViewModel.StatisticToggled -= Statistic_SelectionChanged;
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

				if (ViewModel.HasSelectedRecordings)
					ReplayAnimation();
				break;

			case TabbedCommandBarItem item when item == StatisticsTab:
				ViewModel.ActiveTab = "Statistics";
				break;
		}
	}

	private void RenameRecording_Click(object sender, RoutedEventArgs e)
	{
		RecordingsTreeGrid.SelectionController.CurrentCellManager.BeginEdit();
	}

	private async void ProcessComboBox_DropDownOpened(object sender, object e)
	{
		PresentingProcesses.Start();
		List<string> processes = await Task.Run(() => PresentingProcesses.GetRecordableProcesses(true));
		ViewModel.SetRecordableProcesses(processes);
		if (ProcessComboBox.IsDropDownOpen)
			PresentingProcesses.ProcessesChanged += ProcessDiscovery_ProcessesChanged;
	}

	private void ProcessComboBox_DropDownClosed(object sender, object e)
	{
		PresentingProcesses.ProcessesChanged -= ProcessDiscovery_ProcessesChanged;
		PresentingProcesses.Dispose();
	}

	private void ProcessDiscovery_ProcessesChanged(object? sender, EventArgs e)
	{
		DispatcherQueue.TryEnqueue(() =>
		{
			ViewModel.SetRecordableProcesses(PresentingProcesses.GetRecordableProcesses());
		});
	}

	private void HotkeyShortcut_PrimaryButtonClick(object sender, ContentDialogButtonClickEventArgs e)
	{
		HotkeyShortcut.UpdatePreviewKeys();
		HotkeyShortcut.CloseContentDialog();

		VirtualKeyModifiers modifiers = VirtualKeyModifiers.None;
		VirtualKey key = VirtualKey.None;

		foreach (object? keyItem in HotkeyShortcut.Keys)
		{
			string keyName;
			VirtualKey? virtKey = null;

			if (keyItem is KeyVisualInfo info)
			{
				keyName = info.KeyName ?? string.Empty;
				virtKey = info.Key;
			}
			else
			{
				keyName = keyItem?.ToString() ?? string.Empty;
			}

			if (keyName.Contains("Ctrl", StringComparison.OrdinalIgnoreCase))
				modifiers |= VirtualKeyModifiers.Control;
			else if (keyName.Contains("Shift", StringComparison.OrdinalIgnoreCase))
				modifiers |= VirtualKeyModifiers.Shift;
			else if (keyName.Contains("Alt", StringComparison.OrdinalIgnoreCase))
				modifiers |= VirtualKeyModifiers.Menu;
			else if (keyName.Contains("Win", StringComparison.OrdinalIgnoreCase))
				modifiers |= VirtualKeyModifiers.Windows;
			else if (virtKey.HasValue && virtKey.Value != VirtualKey.None)
				key = virtKey.Value;
			else if (Enum.TryParse<VirtualKey>(keyName, true, out VirtualKey parsed) &&
				parsed != VirtualKey.None)
				key = parsed;
		}

		ViewModel.ShortcutModifiers = modifiers;
		ViewModel.ShortcutKey = key;
	}

	private void OnGlobalKeyDown(object? sender, KeyboardHookEventArgs e)
	{
		if (e.Key == ViewModel.ShortcutKey &&
			e.IsCtrl == ViewModel.ShortcutModifiers.HasFlag(VirtualKeyModifiers.Control) &&
			e.IsShift == ViewModel.ShortcutModifiers.HasFlag(VirtualKeyModifiers.Shift) &&
			e.IsAlt == ViewModel.ShortcutModifiers.HasFlag(VirtualKeyModifiers.Menu) &&
			e.IsWindows == ViewModel.ShortcutModifiers.HasFlag(VirtualKeyModifiers.Windows))
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

	private async void Record_Checked(object sender, RoutedEventArgs e)
	{
		_recordingCts?.Cancel();
		var cts = new CancellationTokenSource();
		_recordingCts = cts;

		ViewModel.IsRecording = true;
		Record.IsChecked = true;

		int delay = (int)ViewModel.Delay;
		int duration = (int)ViewModel.Duration;

		ViewModel.ShowDelay(delay);

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
				ViewModel.DelayRemaining = Math.Max(0, delay - elapsed);
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
			CreateNoWindow = true
		};

		var process = Process.Start(startInfo);
		if (process is null)
		{
			ViewModel.IsRecording = false;
			Record.IsChecked = false;
			await MessageBox.ShowErrorAsync(App.MainWindow, "PresentMon failed to start.", "Recording Error");
			return;
		}

		_activeProcess = process;
		ViewModel.ShowDuration();

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
			ViewModel.DurationRemaining = Math.Max(0, duration - elapsed);
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
				await MessageBox.ShowErrorAsync(App.MainWindow, $"PresentMon exited without producing a recording file.");
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
			await ViewModel.LoadRecordingsAsync();
		}
	}

	private void Record_Unchecked(object sender, RoutedEventArgs e)
	{
		if (!ViewModel.IsRecording)
			return;
		_recordingCts?.Cancel();
		if (_activeProcess is { HasExited: false })
		{
			bool found = false;
			HWND hwnd = HWND.Null;
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

	private void RecordingsTreeGrid_SizeChanged(object sender, SizeChangedEventArgs e)
	{
		if (e.NewSize.Width > 0)
		{
			foreach (TreeGridColumn? col in RecordingsTreeGrid.Columns)
				col.Width = double.NaN;
			RecordingsTreeGrid.InvalidateMeasure();
			RecordingsTreeGrid.UpdateLayout();
		}
	}

	private void RecordingsTreeGrid_SelectionChanged(object sender, GridSelectionChangedEventArgs e)
	{
		AnalyzeRecordings();
	}

	private void AnalyzeRecordings()
	{
		ViewModel.SetSelectedRecordings(RecordingsTreeGrid.SelectedItems.OfType<RecordingItem>().Append(RecordingsTreeGrid.SelectedItem as RecordingItem).OfType<RecordingItem>().DistinctBy(recording => recording.FilePath, StringComparer.OrdinalIgnoreCase).ToList());

		if (ViewModel.SelectedRecordings.Count is 0 or > 2)
			return;

		var results = ViewModel.SelectedRecordings
			.Select(recording => (Item: recording, Result: RecordingAnalyzer.Analyze(recording.FilePath)))
			.Where(recording => recording.Result != null)
			.Select(recording => new RecordingAnalysis(recording.Item, recording.Result))
			.ToList();

		ViewModel.CachedAnalysis = results;

		BuildAnalysis();
		BuildStatistics();
	}

	private async void RecordingsTreeGrid_CurrentCellEndEdit(object sender, CurrentCellEndEditEventArgs e)
	{
		if (RecordingsTreeGrid.GetNodeAtRowIndex(e.RowColumnIndex.RowIndex)?.Item is not RecordingItem recording)
			return;

		string ext = Path.GetExtension(recording.FilePath);
		string newPath = Path.Combine(Path.GetDirectoryName(recording.FilePath) ?? string.Empty, recording.Title + ext);
		if (newPath == recording.FilePath)
			return;

		if (File.Exists(newPath))
		{
			recording.Title = Path.GetFileNameWithoutExtension(recording.FilePath);
			await MessageBox.ShowErrorAsync(App.MainWindow, "A recording with this name already exists.", "Rename Failed");
			return;
		}

		try
		{
			File.Move(recording.FilePath, newPath);
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
		{
			recording.Title = Path.GetFileNameWithoutExtension(recording.FilePath);
			await MessageBox.ShowErrorAsync(App.MainWindow, $"Could not rename the recording: {ex.Message}", "Rename Failed");
			return;
		}

		recording.FilePath = newPath;
		recording.FileName = recording.Title + ext;

		AnalyzeRecordings();
	}

	private void AnalysisChartTypeSelector_SelectionChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
	{
		string oldChartType = ViewModel.AnalysisChartType;
		ViewModel.AnalysisChartType = sender.SelectedItem switch
		{
			var item when item == BarChartItem => "Bar",
			var item when item == ColumnChartItem => "Column",
			var item when item == LineChartItem => "Line",
			var item when item == ScatterChartItem => "Scatter",
			var item when item == PieChartItem => "Pie",
			_ => string.Empty
		};

		string newChartType = ViewModel.AnalysisChartType;
		if (newChartType == oldChartType)
			return;
		ReplayAnimation();
	}

	private void Statistic_SelectionChanged()
	{
		BarColumnChartData presentation = BuildBarColumnChartData(ViewModel.CachedAnalysis);
		BindBarColumnChart(presentation);
	}

	private void MetricComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		string metric = (Metric1ComboBox.SelectedItem as ComboBoxItem)?.Content as string ?? string.Empty;
		LineScatterChartData data = BuildLineScatterChartData(ViewModel.CachedAnalysis, metric);
		BindLineScatterChart(data.Pts1, data.Pts2, data.Label1, data.Label2);
	}

	private void LowFpsThresholdNumberBox_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
	{
		if (double.IsNaN(args.NewValue) || ViewModel.CachedAnalysis.Count == 0)
			return;

        if (ViewModel.AnalysisChartType == "Pie")
		{
			PieChartData pieData = BuildPieChartData(ViewModel.CachedAnalysis, ViewModel.StutterFactor, args.NewValue);
			BindPieChart(pieData);
		}
		UpdateStutterStatistics();
	}

	private void StutterFactorNumberBox_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
	{
		if (double.IsNaN(args.NewValue) || ViewModel.CachedAnalysis.Count == 0)
			return;

        if (ViewModel.AnalysisChartType == "Pie")
		{
			PieChartData pieData = BuildPieChartData(ViewModel.CachedAnalysis, args.NewValue, ViewModel.LowFpsThreshold);
			BindPieChart(pieData);
		}
		UpdateStutterStatistics();
	}

	private void StatisticsBaselineComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		ApplyStatisticsColumns();
		ApplyStatisticsComparisons(ViewModel.StatisticsRows.SelectMany(group => group.Children), ViewModel.BaselineIndex, ViewModel.IsPercentDelta);
	}

	private void StatisticsDeltaModeSelector_SelectionChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
	{
		ViewModel.IsPercentDelta = sender.SelectedItem == PercentDeltaItem;
		ApplyStatisticsComparisons(ViewModel.StatisticsRows.SelectMany(group => group.Children), ViewModel.BaselineIndex, ViewModel.IsPercentDelta);
	}

	private void StatisticsTreeGrid_SizeChanged(object sender, SizeChangedEventArgs e)
	{
		if (e.NewSize.Width > 0)
		{
			foreach (TreeGridColumn? col in StatisticsTreeGrid.Columns)
				col.Width = double.NaN;
			StatisticsTreeGrid.InvalidateMeasure();
			StatisticsTreeGrid.UpdateLayout();
		}
	}

	private void BuildAnalysis()
	{
		if (ViewModel.CachedAnalysis.Count == 0)
			return;

		string? metric = (Metric1ComboBox.SelectedItem as ComboBoxItem)?.Content as string;
		BarColumnChartData presentation = BuildBarColumnChartData(ViewModel.CachedAnalysis);
		BindBarColumnChart(presentation);

		LineScatterChartData data = BuildLineScatterChartData(ViewModel.CachedAnalysis, metric ?? string.Empty);
		BindLineScatterChart(data.Pts1, data.Pts2, data.Label1, data.Label2);

		PieChartData pieData = BuildPieChartData(ViewModel.CachedAnalysis, ViewModel.StutterFactor, ViewModel.LowFpsThreshold);
		BindPieChart(pieData);
	}

	private static BarColumnChartData BuildBarColumnChartData(List<RecordingAnalysis> results)
	{
		List<BarPoint> displayedFpsBars1 = [];
		List<BarPoint> renderedFpsBars1 = [];
		List<BarPoint> displayedFpsBars2 = [];
		List<BarPoint> renderedFpsBars2 = [];
		string displayedFpsLabel1 = string.Empty;
		string renderedFpsLabel1 = string.Empty;
		string displayedFpsLabel2 = string.Empty;
		string renderedFpsLabel2 = string.Empty;

		int fpsSeriesIdx = 0;
		foreach (RecordingAnalysis result in results)
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
			}
			else
			{
				displayedFpsLabel2 = $"{result.Recording.FileName} · Displayed FPS";
				renderedFpsLabel2 = $"{result.Recording.FileName} · Rendered FPS";
			}
			fpsSeriesIdx++;
		}

		return new BarColumnChartData(displayedFpsBars1, renderedFpsBars1, displayedFpsBars2, renderedFpsBars2, displayedFpsLabel1, renderedFpsLabel1, displayedFpsLabel2, renderedFpsLabel2);
	}

	private static LineScatterChartData BuildLineScatterChartData(List<RecordingAnalysis> results, string metric)
	{
		List<SeriesPoint> metricPts1 = [];
		List<SeriesPoint> metricPts2 = [];
		string metricLabel1 = string.Empty;
		string metricLabel2 = string.Empty;

		int index = 0;
		foreach (RecordingAnalysis result in results)
		{
			IReadOnlyList<double> rawValues = metric switch
			{
				"MsBetweenDisplayChange" => result.Analysis.MsBetweenDisplayChange,
				"MsBetweenPresents" => result.Analysis.MsBetweenPresents,
				"MsGPUBusy" => result.Analysis.MsGPUBusy,
				"MsUntilDisplayed" => result.Analysis.MsUntilDisplayed,
				"MsRenderPresentLatency" => result.Analysis.MsRenderPresentLatency,
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

		ViewModel.BarColumnChartDisplayedData1 = [];
		ViewModel.BarColumnChartRenderedData1 = [];
		ViewModel.BarColumnChartDisplayedData2 = [];
		ViewModel.BarColumnChartRenderedData2 = [];
		ViewModel.BarColumnChartDisplayedLabel1 = presentation.DisplayedFpsLabel1;
		ViewModel.BarColumnChartRenderedLabel1 = presentation.RenderedFpsLabel1;
		ViewModel.BarColumnChartDisplayedLabel2 = presentation.DisplayedFpsLabel2;
		ViewModel.BarColumnChartRenderedLabel2 = presentation.RenderedFpsLabel2;
		ViewModel.BarColumnRenderedVisible = true;

		var displayed1 = presentation.DisplayedFpsBars1.Where(bar => ViewModel.IsStatisticEnabled(bar.Label)).ToList();
		var rendered1 = presentation.RenderedFpsBars1.Where(bar => ViewModel.IsStatisticEnabled(bar.Label)).ToList();
		var displayed2 = presentation.DisplayedFpsBars2.Where(bar => ViewModel.IsStatisticEnabled(bar.Label)).ToList();
		var rendered2 = presentation.RenderedFpsBars2.Where(bar => ViewModel.IsStatisticEnabled(bar.Label)).ToList();

		ViewModel.BarColumnChartDisplayedData1 = [.. displayed1];
		ViewModel.BarColumnChartRenderedData1 = [.. rendered1];
		ViewModel.BarColumnChartDisplayedData2 = hasSecondRecording ? [.. displayed2] : [];
		ViewModel.BarColumnChartRenderedData2 = hasSecondRecording ? [.. rendered2] : [];

		if (BarChart != null)
		{
			BarChart.Series.Clear();
			BarChart.Series.Add(BarDisplayedFpsSeries1);
			BarChart.Series.Add(BarRenderedFpsSeries1);

			if (hasSecondRecording)
			{
				BarChart.Series.Add(BarDisplayedFpsSeries2);
				BarChart.Series.Add(BarRenderedFpsSeries2);
			}

			BarDisplayedFpsSeries1.ShowDataLabels = false;
			BarDisplayedFpsSeries1.ShowDataLabels = true;
			BarRenderedFpsSeries1.ShowDataLabels = false;
			BarRenderedFpsSeries1.ShowDataLabels = true;
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
			ColumnChart.Series.Add(ColumnRenderedFpsSeries1);

			if (hasSecondRecording)
			{
				ColumnChart.Series.Add(ColumnDisplayedFpsSeries2);
				ColumnChart.Series.Add(ColumnRenderedFpsSeries2);
			}

			ColumnDisplayedFpsSeries1.ShowDataLabels = false;
			ColumnDisplayedFpsSeries1.ShowDataLabels = true;
			ColumnRenderedFpsSeries1.ShowDataLabels = false;
			ColumnRenderedFpsSeries1.ShowDataLabels = true;
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

	private static PieChartData BuildPieChartData(List<RecordingAnalysis> results, double stutterFactor, double lowFpsThreshold)
	{
		return new PieChartData(BuildPiePoints(results[0], stutterFactor, lowFpsThreshold), results.Count > 1 ? BuildPiePoints(results[1], stutterFactor, lowFpsThreshold) : []);
	}

	private static List<PiePoint> BuildPiePoints(RecordingAnalysis result, double stutterFactor, double lowFpsThreshold)
	{
		IReadOnlyList<double> sequence = result.Analysis.MsBetweenPresents;
		IReadOnlyList<double> movingAverage = result.Analysis.StutterMovingAverage;
		if (sequence.Count == 0 || movingAverage.Count != sequence.Count)
			return [];

		double stutterPercentage = RecordingAnalyzer.GetStutteringTimePercentage(sequence, movingAverage, stutterFactor);
		double lowFpsPercentage = RecordingAnalyzer.GetLowFPSTimePercentage(sequence, movingAverage, stutterFactor, lowFpsThreshold);
		double smoothPercentage = Math.Max(0, 100 - stutterPercentage - lowFpsPercentage);

		double totalSeconds = sequence.Skip(1).Sum() / 1000;
		double stutterSeconds = Math.Round(stutterPercentage / 100 * totalSeconds, 2, MidpointRounding.AwayFromZero);
		double lowFpsSeconds = Math.Round(lowFpsPercentage / 100 * totalSeconds, 2, MidpointRounding.AwayFromZero);
		double smoothSeconds = Math.Round(smoothPercentage / 100 * totalSeconds, 2, MidpointRounding.AwayFromZero);

		static string formatTime(double seconds) => seconds.ToString("0.00", CultureInfo.InvariantCulture);
		static string formatPercent(double percentage) => Math.Round(percentage, 1, MidpointRounding.AwayFromZero).ToString("0.#", CultureInfo.InvariantCulture);

		return
		[
			new PiePoint($"Smooth: {formatTime(smoothSeconds)}s ({formatPercent(smoothPercentage)}%)", smoothSeconds),
			new PiePoint($"Low FPS: {formatTime(lowFpsSeconds)}s ({formatPercent(lowFpsPercentage)}%)", lowFpsSeconds),
			new PiePoint($"Stuttering: {formatTime(stutterSeconds)}s ({formatPercent(stutterPercentage)}%)", stutterSeconds)
		];
	}

	private void BindPieChart(PieChartData pieData)
	{
		ViewModel.PieChartData1 = [];
		ViewModel.PieChartData2 = [];
		ViewModel.PieChartLabel1 = ViewModel.CachedAnalysis[0].Recording.FileName;
		ViewModel.PieChartLabel2 = ViewModel.HasTwoRecordings ? ViewModel.CachedAnalysis[1].Recording.FileName : string.Empty;
		ViewModel.PieChartData1 = [.. pieData.Data1];
		ViewModel.PieChartData2 = pieData.Data2 is null ? [] : [.. pieData.Data2];
	}

	private void ReplayAnimation()
	{
		if (ViewModel.AnalysisChartType is "Bar" or "Column")
		{
			ObservableCollection<BarPoint> oldDisplayedData1 = ViewModel.BarColumnChartDisplayedData1;
			ObservableCollection<BarPoint> oldRenderedData1 = ViewModel.BarColumnChartRenderedData1;
			ObservableCollection<BarPoint> oldDisplayedData2 = ViewModel.BarColumnChartDisplayedData2;
			ObservableCollection<BarPoint> oldRenderedData2 = ViewModel.BarColumnChartRenderedData2;
			ViewModel.BarColumnChartDisplayedData1 = [];
			ViewModel.BarColumnChartRenderedData1 = [];
			ViewModel.BarColumnChartDisplayedData2 = [];
			ViewModel.BarColumnChartRenderedData2 = [];
			ViewModel.BarColumnChartDisplayedData1 = oldDisplayedData1;
			ViewModel.BarColumnChartRenderedData1 = oldRenderedData1;
			ViewModel.BarColumnChartDisplayedData2 = oldDisplayedData2;
			ViewModel.BarColumnChartRenderedData2 = oldRenderedData2;
		}

		else if (ViewModel.AnalysisChartType is "Line" or "Scatter")
		{
			ObservableCollection<SeriesPoint> oldData1 = ViewModel.LineScatterChartData1;
			ObservableCollection<SeriesPoint> oldData2 = ViewModel.LineScatterChartData2;
			ViewModel.LineScatterChartData1 = [];
			ViewModel.LineScatterChartData2 = [];
			ViewModel.LineScatterChartData1 = oldData1;
			ViewModel.LineScatterChartData2 = oldData2;
		}
		else
		{
			ObservableCollection<PiePoint> oldData1 = ViewModel.PieChartData1;
			ObservableCollection<PiePoint> oldData2 = ViewModel.PieChartData2;
			ViewModel.PieChartData1 = [];
			ViewModel.PieChartData2 = [];
			ViewModel.PieChartData1 = oldData1;
			ViewModel.PieChartData2 = oldData2;
		}
	}

	private void BuildStatistics()
	{
		ApplyStatisticsColumns();

		List<RecordingAnalysis> results = ViewModel.CachedAnalysis;
		if (results.Count == 0)
		{
			ViewModel.StatisticsRows = [];
			return;
		}

		List<ResultRow> groups = [];
		foreach ((string? name, Func<AnalysisResult, Metrics>? selector, Dictionary<string, BenchmarkCsv.StatisticDefinition>? statistics) in BenchmarkCsv.GetStatisticGroups(ViewModel.StutterFactor, ViewModel.LowFpsThreshold))
		{
			Metrics m0 = selector(results[0].Analysis);
			Metrics? m1 = results.Count > 1 ? selector(results[1].Analysis) : null;

			var group = new ResultRow
			{
				Statistic = name,
				Tooltip = BenchmarkCsv.MetricDescriptions.TryGetValue(name, out string? tip) ? tip : "Benchmark statistic."
			};
			foreach ((string? key, BenchmarkCsv.StatisticDefinition definition) in statistics)
			{
				double valueA = BenchmarkCsv.GetStatistic(m0, key);
				double valueB = m1 == null ? 0 : BenchmarkCsv.GetStatistic(m1, key);
				group.Children.Add(new ResultRow
				{
					Statistic = definition.Label,
					Tooltip = definition.Description,
					RecordingA = definition.FormatValue(valueA, m0),
					RecordingB = m1 == null ? "" : definition.FormatValue(valueB, m1),
					RecordingAValue = valueA,
					RecordingBValue = m1 == null ? null : valueB,
					RecordingASeconds = BenchmarkCsv.GetStatisticSeconds(m0, key),
					RecordingBSeconds = m1 == null ? null : BenchmarkCsv.GetStatisticSeconds(m1, key),
					Definition = definition
				});
			}
			groups.Add(group);
		}

		if (groups.Count == 0)
		{
			ViewModel.StatisticsRows = [];
			return;
		}

		ViewModel.StatisticsRows = [.. groups];
		ApplyStatisticsComparisons(ViewModel.StatisticsRows.SelectMany(group => group.Children), ViewModel.BaselineIndex, ViewModel.IsPercentDelta);
		StatisticsTreeGrid.ExpandAllNodes();
	}

	private void ApplyStatisticsColumns()
	{
		StatisticsTreeGrid.Columns.Remove(StatisticsRecordingAColumn);
		StatisticsTreeGrid.Columns.Remove(StatisticsRecordingBColumn);
		StatisticsTreeGrid.Columns.Remove(StatisticsDeltaColumn);
		if (ViewModel.ShowRecordingAColumn)
			StatisticsTreeGrid.Columns.Add(StatisticsRecordingAColumn);
		if (ViewModel.ShowRecordingBColumn)
			StatisticsTreeGrid.Columns.Add(StatisticsRecordingBColumn);
		if (ViewModel.ShowDeltaColumn)
			StatisticsTreeGrid.Columns.Add(StatisticsDeltaColumn);
	}

	private static void ApplyStatisticsComparisons(IEnumerable<ResultRow> rows, int baselineIndex, bool showPercentDelta)
	{
		static string signed(double value, string format, string suffix)
		{
			string sign = value >= 0 ? "+ " : "- ";
			return sign + Math.Abs(value).ToString(format, CultureInfo.CurrentCulture) + suffix;
		}

		foreach (ResultRow row in rows)
		{
			if (row.RecordingAValue is not double valueA || row.RecordingBValue is not double valueB)
			{
				row.Delta = string.Empty;
				row.DeltaComparison = ComparisonResult.None;
				continue;
			}

			if (valueA != valueB)
			{
				bool valueAIsBetter = row.Definition.HigherIsBetter ? valueA > valueB : valueA < valueB;
				row.RecordingAComparison = valueAIsBetter ? ComparisonResult.Better : ComparisonResult.Worse;
				row.RecordingBComparison = valueAIsBetter ? ComparisonResult.Worse : ComparisonResult.Better;
			}
			else
			{
				row.RecordingAComparison = ComparisonResult.None;
				row.RecordingBComparison = ComparisonResult.None;
			}

			if (baselineIndex is 0 or 1)
			{
				double baseline = baselineIndex == 0 ? valueA : valueB;
				double comparison = baselineIndex == 0 ? valueB : valueA;
				double delta = comparison - baseline;
				if (delta != 0)
				{
					bool comparisonIsBetter = row.Definition.HigherIsBetter ? delta > 0 : delta < 0;
					ComparisonResult baselineComparison = comparisonIsBetter ? ComparisonResult.Worse : ComparisonResult.Better;
					if (baselineIndex == 0)
						row.RecordingAComparison = baselineComparison;
					else
						row.RecordingBComparison = baselineComparison;
					row.DeltaComparison = comparisonIsBetter ? ComparisonResult.Better : ComparisonResult.Worse;
				}
				else
				{
					row.RecordingAComparison = ComparisonResult.None;
					row.RecordingBComparison = ComparisonResult.None;
					row.DeltaComparison = ComparisonResult.None;
				}

				string deltaText = showPercentDelta && baseline != 0 ? signed(delta / baseline * 100, "0.##", " %") : signed(delta, row.Definition.Format, row.Definition.DeltaSuffix);
				if (row.RecordingASeconds is double secondsA && row.RecordingBSeconds is double secondsB)
				{
					double secondsDelta = baselineIndex == 0 ? secondsB - secondsA : secondsA - secondsB;
					deltaText = $"{signed(secondsDelta, "0.00", " s")} ({deltaText})";
				}
				row.Delta = deltaText;
			}
			else
			{
				row.Delta = string.Empty;
				row.DeltaComparison = ComparisonResult.None;
			}
		}
	}

    private void UpdateStutterStatistics()
    {
		List<RecordingAnalysis> results = ViewModel.CachedAnalysis;
		ResultRow? stutterGroup = ViewModel.StatisticsRows.FirstOrDefault(row => row.Statistic == "Stutter Analysis");
        if (results.Count == 0 || stutterGroup is null)
            return;

		Metrics m0 = RecordingAnalyzer.GetStutterMetrics(results[0].Analysis, ViewModel.StutterFactor, ViewModel.LowFpsThreshold);
		Metrics? m1 = results.Count > 1 ? RecordingAnalyzer.GetStutterMetrics(results[1].Analysis, ViewModel.StutterFactor, ViewModel.LowFpsThreshold) : null;

        foreach ((string? key, BenchmarkCsv.StatisticDefinition definition) in BenchmarkCsv.StutterStatistics)
        {
			ResultRow? row = stutterGroup.Children.FirstOrDefault(child => child.Statistic == definition.Label);
            if (row is null)
                continue;
            double valueA = BenchmarkCsv.GetStatistic(m0, key);
            double valueB = m1 == null ? 0 : BenchmarkCsv.GetStatistic(m1, key);
            row.RecordingA = definition.FormatValue(valueA, m0);
            row.RecordingB = m1 == null ? "" : definition.FormatValue(valueB, m1);
            row.RecordingAValue = valueA;
            row.RecordingBValue = m1 == null ? null : valueB;
            row.RecordingASeconds = BenchmarkCsv.GetStatisticSeconds(m0, key);
            row.RecordingBSeconds = m1 == null ? null : BenchmarkCsv.GetStatisticSeconds(m1, key);
        }

        ApplyStatisticsComparisons(stutterGroup.Children, ViewModel.BaselineIndex, ViewModel.IsPercentDelta);
        StatisticsTreeGrid.View?.RefreshFilter();
    }

    private void Chart_RightTapped(object sender, RightTappedRoutedEventArgs e)
	{
		if (sender is not FrameworkElement chart) return;

		string[] stats;
		if (ViewModel.AnalysisChartType is "Bar" or "Column")
		{
			bool multiple = ViewModel.SelectedRecordings.Count > 1;
			bool displayed1, rendered1, displayed2, rendered2;

			if (chart == BarChart)
			{
				displayed1 = BarDisplayedFpsSeries1.Visibility == Visibility.Visible;
				rendered1 = BarRenderedFpsSeries1.Visibility == Visibility.Visible;
				displayed2 = multiple && BarDisplayedFpsSeries2.Visibility == Visibility.Visible;
				rendered2 = multiple && BarRenderedFpsSeries2.Visibility == Visibility.Visible;
			}
			else
			{
				displayed1 = ColumnDisplayedFpsSeries1.Visibility == Visibility.Visible;
				rendered1 = ColumnRenderedFpsSeries1.Visibility == Visibility.Visible;
				displayed2 = multiple && ColumnDisplayedFpsSeries2.Visibility == Visibility.Visible;
				rendered2 = multiple && ColumnRenderedFpsSeries2.Visibility == Visibility.Visible;
			}

			stats = new string[ViewModel.SelectedRecordings.Count];
			for (int i = 0; i < stats.Length; i++)
			{
				bool isFirst = i == 0;
				bool displayed = isFirst ? displayed1 : displayed2;
				bool rendered = isFirst ? rendered1 : rendered2;
				stats[i] = string.Join(", ", new[] { displayed ? "Displayed FPS" : null, rendered ? "Rendered FPS" : null }.Where(x => x != null));
			}
		}
		else if (ViewModel.AnalysisChartType == "Pie")
		{
			string lowFps = ViewModel.LowFpsThreshold.ToString("0.##", CultureInfo.InvariantCulture);
			string stutter = ViewModel.StutterFactor.ToString("0.##", CultureInfo.InvariantCulture);
			stats = [.. ViewModel.SelectedRecordings.Select(_ => $"Low FPS {lowFps}, Stutter {stutter}")];
		}
		else
		{
			stats = [.. ViewModel.SelectedRecordings.Select((recording, index) => chart is SfCartesianChart cartesian && index < cartesian.Series.Count && cartesian.Series[index].Visibility == Visibility.Visible ? (Metric1ComboBox.SelectedItem as ComboBoxItem)?.Content as string ?? string.Empty : string.Empty)];
		}

		string recordingNames = string.Join(" vs ", ViewModel.SelectedRecordings.Select((recording, index) => string.IsNullOrEmpty(stats[index]) ? recording.Title : $"{recording.Title} ({stats[index]})"));

		string fileName = $"{recordingNames} - {ViewModel.AnalysisChartType} Chart";

		FrameworkElement saveTarget = ViewModel.AnalysisChartType == "Pie" ? (FrameworkElement)PieChartContainer : chart;

		var flyout = new MenuFlyout();

		var jpegItem = new MenuFlyoutItem { Text = "Save as JPG", Icon = new FontIcon { Glyph = "\uE896" } };
		jpegItem.Click += async (s, args) => await SaveChartAsync(saveTarget, fileName, BitmapEncoder.JpegEncoderId, "jpg", true);
		flyout.Items.Add(jpegItem);

		var pngItem = new MenuFlyoutItem { Text = "Save as PNG", Icon = new FontIcon { Glyph = "\uE896" } };
		pngItem.Click += async (s, args) => await SaveChartAsync(saveTarget, fileName, BitmapEncoder.PngEncoderId, "png", false);
		flyout.Items.Add(pngItem);

		flyout.ShowAt(chart, e.GetPosition(chart));
	}

	private static async Task SaveChartAsync(FrameworkElement chart, string suggestedFileName, Guid encoderId, string extension, bool flattenBackground)
	{
		var picker = new SavePicker(App.MainWindow)
		{
			DefaultFileExtension = $"{extension}",
			ShowAllFilesOption = false,
			SuggestedFileName = suggestedFileName,
			InitialDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads"),
		};
		picker.FileTypeChoices.Add($"{extension} image", [$"*.{extension}"]);

		string? filePath = picker.PickSaveFile();
		if (string.IsNullOrWhiteSpace(filePath))
			return;
		if (!filePath.EndsWith($".{extension}", StringComparison.OrdinalIgnoreCase))
			filePath += $".{extension}";

		var bitmap = new RenderTargetBitmap();
		await bitmap.RenderAsync(chart);
		if (bitmap.PixelWidth == 0 || bitmap.PixelHeight == 0)
			return;

		IBuffer pixels = await bitmap.GetPixelsAsync();
		byte[] pixelData = pixels.ToArray();

		if (flattenBackground)
		{
			Color background = new UISettings().GetColorValue(UIColorType.Background);
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
		using IRandomAccessStream stream = await file.OpenAsync(FileAccessMode.ReadWrite);
		BitmapEncoder encoder = await BitmapEncoder.CreateAsync(encoderId, stream);
		encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied, (uint)bitmap.PixelWidth, (uint)bitmap.PixelHeight, 96, 96, pixelData);
		await encoder.FlushAsync();
	}
}
