using System.Collections.ObjectModel;
using System.Globalization;
using System.Runtime.InteropServices.WindowsRuntime;
using AutoOS.App.ViewModels;
using AutoOS.Core.Helpers.Picker;
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

namespace AutoOS.App.Views.Settings;

public sealed partial class BenchmarksPage : Page
{
	public BenchmarksPageViewModel ViewModel { get; } = new();

	private GlobalKeyboardHook? _globalKeyboardHook;

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
		await ViewModel.StartProcessDiscoveryAsync();
		if (ProcessComboBox.IsDropDownOpen)
			ViewModel.SubscribeProcessDiscovery();
	}

	private void ProcessComboBox_DropDownClosed(object sender, object e)
	{
		ViewModel.UnsubscribeProcessDiscovery();
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
				if (ViewModel.IsRecording || !string.IsNullOrWhiteSpace(ViewModel.ProcessName))
					ViewModel.RecordCommand.Execute(null);
			});
		}
	}

	private void RecordingsTreeGrid_SizeChanged(object sender, SizeChangedEventArgs e)
	{
		if (e.NewSize.Width <= 0 || e.NewSize.Width == e.PreviousSize.Width)
		{
			return;
		}

		foreach (TreeGridColumn? col in RecordingsTreeGrid.Columns)
			col.Width = double.NaN;
		RecordingsTreeGrid.InvalidateMeasure();
		RecordingsTreeGrid.UpdateLayout();
	}

	private void RecordingsTreeGrid_SelectionChanged(object sender, GridSelectionChangedEventArgs e)
	{
		AnalyzeRecordings();
	}

	private async void AnalyzeRecordings()
	{
		ViewModel.SetSelectedRecordings(RecordingsTreeGrid.SelectedItems.OfType<RecordingItem>().Append(RecordingsTreeGrid.SelectedItem as RecordingItem).OfType<RecordingItem>().DistinctBy(recording => recording.FilePath, StringComparer.OrdinalIgnoreCase).ToList());

		if (ViewModel.SelectedRecordings.Count is 0 or > 2)
			return;

		await ViewModel.AnalyzeSelectedAsync();
		ViewModel.BuildAnalysis();
		ViewModel.BuildStatistics();
		BindBarColumnChart();
		StatisticsTreeGrid.ExpandAllNodes();
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
		ViewModel.BuildBarColumnChartData();
		BindBarColumnChart();
	}

	private void MetricComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		ViewModel.SelectedMetric = (Metric1ComboBox.SelectedItem as ComboBoxItem)?.Content as string ?? string.Empty;
		ViewModel.BuildLineScatterChartData();
	}

	private void LowFpsThresholdNumberBox_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
	{
		if (double.IsNaN(args.NewValue) || ViewModel.CachedAnalysis.Count == 0)
			return;

		if (ViewModel.AnalysisChartType == "Pie")
			ViewModel.BuildPieChartData(lowFpsThreshold: args.NewValue);
		ViewModel.UpdateStutterStatistics(lowFpsThreshold: args.NewValue);
		StatisticsTreeGrid.View?.RefreshFilter();
	}

	private void StutterFactorNumberBox_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
	{
		if (double.IsNaN(args.NewValue) || ViewModel.CachedAnalysis.Count == 0)
			return;

		if (ViewModel.AnalysisChartType == "Pie")
			ViewModel.BuildPieChartData(stutterFactor: args.NewValue);
		ViewModel.UpdateStutterStatistics(stutterFactor: args.NewValue);
		StatisticsTreeGrid.View?.RefreshFilter();
	}

	private void StatisticsBaselineComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		ApplyStatisticsColumns();
		ViewModel.ApplyStatisticsComparisons();
	}

	private void StatisticsDeltaModeSelector_SelectionChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
	{
		ViewModel.IsPercentDelta = sender.SelectedItem == PercentDeltaItem;
		ViewModel.ApplyStatisticsComparisons();
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

	private void BindBarColumnChart()
	{
		bool hasSecondRecording = ViewModel.HasTwoRecordings;

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

	private void Chart_RightTapped(object sender, RightTappedRoutedEventArgs e)
	{
		if (sender is not FrameworkElement chart)
			return;

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
