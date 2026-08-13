using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using AutoOS.App.Data.Enums;
using AutoOS.App.Data.Enums.Benchmarks;
using AutoOS.App.Data.Models.Benchmarks;
using AutoOS.App.Services;
using AutoOS.App.Services.Benchmarks;
using AutoOS.Core.Helpers.Picker;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Media;
using nietras.SeparatedValues;
using Windows.Storage;
using Windows.System;

namespace AutoOS.App.ViewModels;

public sealed partial class BenchmarksPageViewModel(IDialogService dialogService) : ObservableObject
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
	[NotifyPropertyChangedFor(nameof(HasSelectedRecordings))]
	[NotifyPropertyChangedFor(nameof(HasTwoRecordingsVisibility))]
	[NotifyPropertyChangedFor(nameof(HasTwoRecordings))]
	[NotifyPropertyChangedFor(nameof(PieChartColumnSpan))]
	[NotifyCanExecuteChangedFor(nameof(DeleteCommand))]
	[NotifyCanExecuteChangedFor(nameof(AggregateCommand))]
	public partial int SelectedRecordingCount { get; set; }

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(IsAggregateEnabled))]
	[NotifyPropertyChangedFor(nameof(HasSelectedRecordings))]
	[NotifyPropertyChangedFor(nameof(HasTwoRecordingsVisibility))]
	[NotifyPropertyChangedFor(nameof(HasTwoRecordings))]
	[NotifyPropertyChangedFor(nameof(PieChartColumnSpan))]
	[NotifyCanExecuteChangedFor(nameof(AggregateCommand))]
	public partial bool SelectedRecordingsHaveSameProcess { get; set; }

	public IReadOnlyList<RecordingItem> SelectedRecordings { get; set; } = [];
	private readonly ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
	public List<RecordingAnalysis> CachedAnalysis { get; set; } = [];
	private readonly HashSet<string> _recordableProcesses = [with(StringComparer.OrdinalIgnoreCase)];
	private ProcessDiscoveryService? _processDiscovery;
	private CancellationTokenSource? _recordingCts;
	private Process? _activeProcess;

	public void LoadSettings()
	{
		if (localSettings.Values.TryGetValue("BenchmarkDelay", out object? delayObj) && delayObj is double delay)
			Delay = delay;

		if (localSettings.Values.TryGetValue("BenchmarkDuration", out object? durationObj) && durationObj is double duration)
			Duration = duration;

		if (localSettings.Values.TryGetValue("BenchmarkShortcut", out object? shortcutObj) && shortcutObj is string shortcut && !string.IsNullOrWhiteSpace(shortcut))
		{
			string[] parts = shortcut.Split('|');
			if (parts.Length == 2 && Enum.TryParse(parts[0], out VirtualKeyModifiers modifiers) && Enum.TryParse(parts[1], out VirtualKey key))
			{
				ShortcutModifiers = modifiers;
				ShortcutKey = key;
			}
		}
	}

	public async Task LoadRecordingsAsync()
	{
		List<RecordingItem> finalRecordings = await Task.Run(() =>
		{
			if (!Directory.Exists(RecordingAnalysisService.RecordingsDirectory))
			{
				Directory.CreateDirectory(RecordingAnalysisService.RecordingsDirectory);
				return [];
			}

			List<FileInfo> csvFiles = [.. new DirectoryInfo(RecordingAnalysisService.RecordingsDirectory).EnumerateFiles("*.csv")];

			if (csvFiles.Count == 0)
			{
				return [];
			}

			SepReaderOptions sepReader = Sep.Reader(options => options with { Sep = new Sep(','), Unescape = true, ColNameComparer = StringComparer.OrdinalIgnoreCase });

			List<RecordingItem> recordings = [with(csvFiles.Count)];
			Dictionary<RecordingItem, List<string>> aggregateSources = [];

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

						using SepReader reader = sepReader.FromFile(info.FullName);

						reader.Header.TryIndexOf("Application", out int appIdx);
						reader.Header.TryIndexOf("PresentMode", out int presentModeIdx);
						reader.Header.TryIndexOf("AggregateDurationSeconds", out int aggDurationIdx);
						bool hasAggSources = reader.Header.TryIndexOf("AggregateSources", out int aggSourcesIdx);
						reader.Header.TryIndexOf("TimeInDateTime", out int dateTimeIdx);
						reader.Header.TryIndexOf("TimeInSeconds", out int timeSecondsIdx);

						if (!reader.MoveNext())
							return (Recording: result, SourceFileNames: sourceFileNames);

						SepReader.Row firstRow = reader.Current;

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
							string sourceText = firstRow[aggSourcesIdx].ToString();
							if (!string.IsNullOrWhiteSpace(sourceText))
								sourceFileNames = [.. sourceText.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)];
						}

						if (!hasCsvDuration && (dateTimeIdx >= 0 || timeSecondsIdx >= 0))
						{
							string? firstDateTimeStr = dateTimeIdx >= 0 ? firstRow[dateTimeIdx].ToString() : null;
							string? firstTimeSecondsStr = timeSecondsIdx >= 0 ? firstRow[timeSecondsIdx].ToString() : null;

							string lastLine = RecordingAnalysisService.ReadLastLine(info.FullName, info.Length);
							ReadOnlySpan<char> lastLineSpan = lastLine;

							if (dateTimeIdx >= 0 && firstDateTimeStr != null)
							{
								ReadOnlySpan<char> lastDateTimeSpan = RecordingAnalysisService.GetField(lastLineSpan, dateTimeIdx);
								if (!lastDateTimeSpan.IsEmpty &&
									DateTime.TryParse(firstDateTimeStr, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out DateTime start) &&
									DateTime.TryParse(lastDateTimeSpan, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out DateTime end))
								{
									result.DurationSeconds = Math.Max(0, (end - start).TotalSeconds);
									hasCsvDuration = true;
								}
							}

							if (!hasCsvDuration && timeSecondsIdx >= 0 && firstTimeSecondsStr != null && double.TryParse(firstTimeSecondsStr, NumberStyles.Float, CultureInfo.InvariantCulture, out double firstTimeSec))
							{
								ReadOnlySpan<char> lastTimeSecondsSpan = RecordingAnalysisService.GetField(lastLineSpan, timeSecondsIdx);
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
						return (Recording: null!, SourceFileNames: null!);
					}
				})
				.Where(recording => recording.Recording != null)
				.Select(recording => (recording.Recording, recording.SourceFileNames))
				.ToList();

			loadedRecordings.Sort((a, b) => b.Recording.Date.CompareTo(a.Recording.Date));

			Dictionary<string, RecordingItem> recordingsByFileName = new(loadedRecordings.Count, StringComparer.OrdinalIgnoreCase);

			foreach ((RecordingItem? recording, List<string>? sourceFileNames) in loadedRecordings)
			{
				recordings.Add(recording);
				recordingsByFileName[recording.FileName] = recording;
				if (sourceFileNames.Count > 0)
					aggregateSources[recording] = sourceFileNames;
			}

			if (aggregateSources.Count > 0)
			{
				HashSet<RecordingItem> childRecordings = [];
				foreach ((RecordingItem? aggregate, List<string>? sourceFileNames) in aggregateSources)
				{
					foreach (string sourceFileName in sourceFileNames.Distinct(StringComparer.OrdinalIgnoreCase))
					{
						if (recordingsByFileName.TryGetValue(sourceFileName, out RecordingItem? source) && !ReferenceEquals(source, aggregate))
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
		Recordings = [with(recordings)];
		RecordingState = recordings.Count == 0 ? "Empty" : "Content";
	}

	[ObservableProperty]
	public partial ObservableCollection<RecordingItem> Recordings { get; set; } = [];

	public void SetSelectedRecordings(IReadOnlyList<RecordingItem> recordings)
	{
		SelectedRecordings = recordings;
		int count = recordings.Count;
		RecordingItem? recordingA = count > 0 ? recordings[0] : null;
		SelectedRecordingCount = count;
		IsRenameEnabled = count > 0;

		bool sameProcess = count > 0;
		if (sameProcess)
		{
			string firstProcess = recordings[0].Process;
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
		BaselineItems = [with(["None", .. recordings.Select(recording => recording.Title)])];
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

	public bool HasSelectedRecordings => SelectedRecordingCount is > 0 and <= 2 && SelectedRecordingsHaveSameProcess;

	public bool HasTwoRecordings => SelectedRecordingCount == 2 && SelectedRecordingsHaveSameProcess;
	public Visibility HasTwoRecordingsVisibility => HasTwoRecordings ? Visibility.Visible : Visibility.Collapsed;
	public int PieChartColumnSpan => HasTwoRecordings ? 1 : 2;

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
		List<StorageFile> files = await picker.PickMultipleFilesAsync();
		if (files.Count == 0)
			return;

		foreach (StorageFile file in files)
			File.Copy(file.Path, Path.Combine(RecordingAnalysisService.RecordingsDirectory, file.Name), true);

		await LoadRecordingsAsync();
	}

	[ObservableProperty]
	public partial bool IsRenameEnabled { get; set; }

	public bool IsDeleteEnabled => SelectedRecordingCount > 0;

	[RelayCommand(CanExecute = nameof(IsDeleteEnabled))]
	private async Task DeleteAsync()
	{
		if (SelectedRecordings.Count == 0)
			return;

		int count = SelectedRecordings.Count;
		if (await dialogService.ShowConfirmationDialogAsync("Delete recordings", $"Are you sure you want to delete {count} recording{(count == 1 ? "" : "s")}?", "Delete", "Cancel") != DialogResult.Primary)
			return;

		foreach (RecordingItem recording in SelectedRecordings)
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

	public bool IsAggregateEnabled => SelectedRecordingCount > 1 && SelectedRecordingsHaveSameProcess;

	[RelayCommand(CanExecute = nameof(IsAggregateEnabled))]
	private void Aggregate()
	{
		IReadOnlyList<RecordingItem> selected = SelectedRecordings;
		string processName = selected[0].Process;

		int aggregateNumber = 1;
		string outPath;
		do
		{
			outPath = Path.Combine(RecordingAnalysisService.RecordingsDirectory, $"Aggregate-{aggregateNumber++}.csv");
		}
		while (File.Exists(outPath));

		List<string> headerCols;
		using (SepReader headerReader = Sep.Reader(options => options with { Sep = new Sep(','), Unescape = true }).FromFile(selected[0].FilePath))
		{
			headerCols = [with(headerReader.Header.ColNames.Count)];
			for (int i = 0; i < headerReader.Header.ColNames.Count; i++)
				headerCols.Add(headerReader.Header.ColNames[i]);
		}

		int applicationIndex = RecordingAnalysisService.EnsureColumn(headerCols, "Application");
		int aggregateDurationIndex = RecordingAnalysisService.EnsureColumn(headerCols, "AggregateDurationSeconds");
		int aggregateSourcesIndex = RecordingAnalysisService.EnsureColumn(headerCols, "AggregateSources");
		int columnCount = headerCols.Count;

		List<double[]> sums = [];
		List<int[]> counts = [];

		List<string[]> fallbackRows = [];

		for (int fileIndex = 0; fileIndex < selected.Count; fileIndex++)
		{
			using SepReader reader = Sep.Reader(options => options with { Sep = new Sep(','), Unescape = true }).FromFile(selected[fileIndex].FilePath);
			if (reader.Header.IsEmpty)
				continue;

			bool isFallbackFile = fileIndex == 0;
			int rowIndex = 0;

			while (reader.MoveNext())
			{
				SepReader.Row row = reader.Current;

				if (rowIndex == sums.Count)
				{
					sums.Add(new double[columnCount]);
					counts.Add(new int[columnCount]);
				}

				double[] rowSums = sums[rowIndex];
				int[] rowCounts = counts[rowIndex];

				string[]? rawRow = isFallbackFile ? new string[row.ColCount] : null;
				int colLimit = Math.Min(row.ColCount, columnCount);

				for (int column = 0; column < colLimit; column++)
				{
					if (row[column].TryParse(out double value))
					{
						rowSums[column] += value;
						rowCounts[column]++;
					}

					if (isFallbackFile)
						rawRow![column] = row[column].ToString();
				}

				if (isFallbackFile)
				{
					for (int column = colLimit; column < row.ColCount; column++)
						rawRow![column] = row[column].ToString();
					fallbackRows.Add(rawRow!);
				}

				rowIndex++;
			}
		}

		int maxRows = sums.Count;
		double meanDurationSeconds = selected.Average(recording => recording.DurationSeconds);
		string aggregateSources = string.Join("|", selected.Select(recording => recording.FileName).Distinct(StringComparer.OrdinalIgnoreCase));

		using SepWriter writer = Sep.Writer(options => options with { Sep = new Sep(',') }).ToFile(outPath);
		foreach (string col in headerCols)
			writer.Header.Add(col);

		for (int r = 0; r < maxRows; r++)
		{
			using SepWriter.Row row = writer.NewRow();
			double[] rowSums = sums[r];
			int[] rowCounts = counts[r];
			string[]? fallbackRow = r < fallbackRows.Count ? fallbackRows[r] : null;

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
		foreach (RecordingItem child in childSet)
			aggregateRecording.Children.Add(child);

		List<RecordingItem> updatedList = [aggregateRecording, .. Recordings.Where(recording => !childSet.Contains(recording))];
		updatedList.Sort((a, b) => b.Date.CompareTo(a.Date));
		SetRecordings(updatedList);
		SetSelectedRecordings([]);
	}

	[ObservableProperty]
	public partial ObservableCollection<string> ProcessSuggestions { get; set; } = [];

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

	public async Task StartProcessDiscoveryAsync()
	{
		_processDiscovery?.Dispose();
		var discovery = new ProcessDiscoveryService();
		_processDiscovery = discovery;
		discovery.Start();
		List<string> processes = await Task.Run(() => discovery.GetRecordableProcesses(true));
		SetRecordableProcesses(processes);
	}

	public void SubscribeProcessDiscovery()
		=> _processDiscovery?.ProcessesChanged += OnProcessesChanged;

	public void UnsubscribeProcessDiscovery()
	{
		if (_processDiscovery != null)
		{
			_processDiscovery.ProcessesChanged -= OnProcessesChanged;
			_processDiscovery.Dispose();
		}
	}

	private void OnProcessesChanged(object? sender, EventArgs e)
	{
		App.MainWindow.DispatcherQueue.TryEnqueue(() => SetRecordableProcesses(_processDiscovery?.GetRecordableProcesses() ?? []));
	}

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(CanRecord))]
	[NotifyCanExecuteChangedFor(nameof(RecordCommand))]
	public partial string ProcessName { get; set; } = string.Empty;

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(CanRecord))]
	[NotifyCanExecuteChangedFor(nameof(RecordCommand))]
	public partial double Delay { get; set; } = 5;

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(CanRecord))]
	[NotifyCanExecuteChangedFor(nameof(RecordCommand))]
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
			if (ShortcutModifiers.HasFlag(VirtualKeyModifiers.Control))
				keys.Add("Ctrl");
			if (ShortcutModifiers.HasFlag(VirtualKeyModifiers.Shift))
				keys.Add("Shift");
			if (ShortcutModifiers.HasFlag(VirtualKeyModifiers.Menu))
				keys.Add("Alt");
			if (ShortcutModifiers.HasFlag(VirtualKeyModifiers.Windows))
				keys.Add("Win");
			if (ShortcutKey != VirtualKey.None)
				keys.Add(ShortcutKey.ToString());
			return keys;
		}
	}

	partial void OnDelayChanged(double value) => localSettings.Values["BenchmarkDelay"] = value;
	partial void OnDurationChanged(double value) => localSettings.Values["BenchmarkDuration"] = value;
	partial void OnShortcutModifiersChanged(VirtualKeyModifiers value) => localSettings.Values["BenchmarkShortcut"] = $"{ShortcutModifiers}|{ShortcutKey}";
	partial void OnShortcutKeyChanged(VirtualKey value) => localSettings.Values["BenchmarkShortcut"] = $"{ShortcutModifiers}|{ShortcutKey}";

	public bool CanRecord => IsRecording || (!string.IsNullOrWhiteSpace(ProcessName) && !double.IsNaN(Delay) && !double.IsNaN(Duration));
	public string RecordLabel => IsRecording ? "Cancel" : "Record";
	public string RecordIconGlyph => IsRecording ? "\uE711" : "\uE7C8";

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(IsAddEnabled))]
	[NotifyCanExecuteChangedFor(nameof(AddCommand))]
	[NotifyCanExecuteChangedFor(nameof(RecordCommand))]
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

	[RelayCommand(CanExecute = nameof(CanRecord), AllowConcurrentExecutions = true)]
	private async Task RecordAsync()
	{
		if (IsRecording)
		{
			CancelRecording();
			return;
		}

		_recordingCts?.Cancel();
		var cts = new CancellationTokenSource();
		_recordingCts = cts;

		IsRecording = true;

		int delay = (int)Delay;
		int duration = (int)Duration;

		ShowDelay(delay);

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
				DelayRemaining = Math.Max(0, delay - elapsed);
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
				IsRecording = false;
			return;
		}

		string outputPath = PresentMonRecordingService.GenerateOutputPath();

		Process? process = PresentMonRecordingService.Start(ProcessName, outputPath, duration);
		if (process is null)
		{
			IsRecording = false;
			await MessageBox.ShowErrorAsync(App.MainWindow, "PresentMon failed to start.", "Recording Error");
			return;
		}

		_activeProcess = process;
		ShowDuration();

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
			DurationRemaining = Math.Max(0, duration - elapsed);
			if (process.HasExited)
				recordingTimer.Stop();
		};
		recordingTimer.Start();

		try
		{
			await process.WaitForExitAsync();

			if (cts.IsCancellationRequested)
			{
				PresentMonRecordingService.DeleteOutputFile(outputPath);
				return;
			}

			if (!File.Exists(outputPath) || new FileInfo(outputPath).Length == 0)
			{
				await MessageBox.ShowErrorAsync(App.MainWindow, "PresentMon exited without producing a recording file.");
			}

			PresentMonRecordingService.PlayCompletedSound();
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
				IsRecording = false;
			}
			await LoadRecordingsAsync();
		}
	}

	private void CancelRecording()
	{
		_recordingCts?.Cancel();
		if (_activeProcess is { HasExited: false })
			PresentMonRecordingService.Stop(_activeProcess);
		IsRecording = false;
		RecordingState = Recordings.Count == 0 ? "Empty" : "Content";
	}

	public async Task AnalyzeSelectedAsync()
	{
		RecordingAnalysis?[] analyses = await Task.WhenAll(SelectedRecordings.Select(recording =>
			Task.Run(() =>
			{
				AnalysisResult? result = RecordingAnalysisService.Analyze(recording.FilePath);
				return result is null ? null : new RecordingAnalysis(recording, result);
			})));

		CachedAnalysis = [.. analyses.OfType<RecordingAnalysis>()];
	}

	public void BuildAnalysis()
	{
		if (CachedAnalysis.Count == 0)
			return;

		BuildBarColumnChartData();
		BuildLineScatterChartData();
		BuildPieChartData();
	}

	public void BuildBarColumnChartData()
	{
		List<RecordingAnalysis> results = CachedAnalysis;
		if (results.Count == 0)
			return;

		List<BarPoint> displayedFpsBars1 = [];
		List<BarPoint> renderedFpsBars1 = [];
		List<BarPoint> displayedFpsBars2 = [];
		List<BarPoint> renderedFpsBars2 = [];

		int fpsSeriesIdx = 0;
		foreach (RecordingAnalysis result in results)
		{
			List<BarPoint> displayedTarget = fpsSeriesIdx == 0 ? displayedFpsBars1 : displayedFpsBars2;
			List<BarPoint> renderedTarget = fpsSeriesIdx == 0 ? renderedFpsBars1 : renderedFpsBars2;

			foreach (string percentile in Catalog.StatisticLabelsShort)
			{
				if (IsStatisticEnabled(percentile))
				{
					displayedTarget.Add(new BarPoint { Label = percentile, Value = Catalog.GetStatistic(result.Analysis.DisplayedFps, percentile) });
					renderedTarget.Add(new BarPoint { Label = percentile, Value = Catalog.GetStatistic(result.Analysis.RenderedFps, percentile) });
				}
			}

			if (fpsSeriesIdx == 0)
			{
				BarColumnChartDisplayedLabel1 = $"{result.Recording.FileName} · Displayed FPS";
				BarColumnChartRenderedLabel1 = $"{result.Recording.FileName} · Rendered FPS";
			}
			else
			{
				BarColumnChartDisplayedLabel2 = $"{result.Recording.FileName} · Displayed FPS";
				BarColumnChartRenderedLabel2 = $"{result.Recording.FileName} · Rendered FPS";
			}
			fpsSeriesIdx++;
		}

		BarColumnChartDisplayedData1 = [.. displayedFpsBars1];
		BarColumnChartRenderedData1 = [.. renderedFpsBars1];
		BarColumnChartDisplayedData2 = HasTwoRecordings ? [.. displayedFpsBars2] : [];
		BarColumnChartRenderedData2 = HasTwoRecordings ? [.. renderedFpsBars2] : [];
		BarColumnRenderedVisible = true;
	}

	public void BuildLineScatterChartData()
	{
		List<RecordingAnalysis> results = CachedAnalysis;
		if (results.Count == 0)
			return;

		string metric = SelectedMetric;
		List<SeriesPoint> metricPts1 = [];
		List<SeriesPoint> metricPts2 = [];

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
				LineScatterChartLabel1 = $"{result.Recording.FileName} · {metric}";
			}
			else
			{
				metricPts2 = points;
				LineScatterChartLabel2 = $"{result.Recording.FileName} · {metric}";
			}
			index++;
		}

		LineScatterChartData1 = [.. metricPts1];
		LineScatterChartData2 = [.. metricPts2];
	}

	public void BuildPieChartData(double? stutterFactor = null, double? lowFpsThreshold = null)
	{
		List<RecordingAnalysis> results = CachedAnalysis;
		if (results.Count == 0)
			return;

		double factor = stutterFactor ?? StutterFactor;
		double threshold = lowFpsThreshold ?? LowFpsThreshold;

		PieChartLabel1 = results[0].Recording.FileName;
		PieChartLabel2 = HasTwoRecordings ? results[1].Recording.FileName : string.Empty;
		PieChartData1 = [.. BuildPiePoints(results[0], factor, threshold)];
		PieChartData2 = HasTwoRecordings ? [.. BuildPiePoints(results[1], factor, threshold)] : [];
	}

	private static List<PiePoint> BuildPiePoints(RecordingAnalysis result, double stutterFactor, double lowFpsThreshold)
	{
		IReadOnlyList<double> sequence = result.Analysis.MsBetweenPresents;
		IReadOnlyList<double> movingAverage = result.Analysis.StutterMovingAverage;
		if (sequence.Count == 0 || movingAverage.Count != sequence.Count)
			return [];

		double stutterPercentage = StatisticsCalculator.GetStutteringTimePercentage(sequence, movingAverage, stutterFactor);
		double lowFpsPercentage = StatisticsCalculator.GetLowFPSTimePercentage(sequence, movingAverage, stutterFactor, lowFpsThreshold);
		double smoothPercentage = Math.Max(0, 100 - stutterPercentage - lowFpsPercentage);

		double totalSeconds = sequence.Skip(1).Sum() / 1000;
		double stutterSeconds = Math.Round(stutterPercentage / 100 * totalSeconds, 2, MidpointRounding.AwayFromZero);
		double lowFpsSeconds = Math.Round(lowFpsPercentage / 100 * totalSeconds, 2, MidpointRounding.AwayFromZero);
		double smoothSeconds = Math.Round(smoothPercentage / 100 * totalSeconds, 2, MidpointRounding.AwayFromZero);

		static string formatTime(double seconds) => seconds.ToString("0.00", CultureInfo.InvariantCulture);
		static string formatPercent(double percentage) => Math.Round(percentage, 1, MidpointRounding.AwayFromZero).ToString("0.#", CultureInfo.InvariantCulture);

		return
		[
			new PiePoint { Label = $"Smooth: {formatTime(smoothSeconds)}s ({formatPercent(smoothPercentage)}%)", Value = smoothSeconds },
			new PiePoint { Label = $"Low FPS: {formatTime(lowFpsSeconds)}s ({formatPercent(lowFpsPercentage)}%)", Value = lowFpsSeconds },
			new PiePoint { Label = $"Stuttering: {formatTime(stutterSeconds)}s ({formatPercent(stutterPercentage)}%)", Value = stutterSeconds }
		];
	}

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(StatisticsVisibility))]
	[NotifyPropertyChangedFor(nameof(MetricVisibility))]
	[NotifyPropertyChangedFor(nameof(ThresholdsVisibility))]
	public partial string AnalysisChartType { get; set; } = "Bar";

	public Visibility StatisticsVisibility => (AnalysisChartType is "Bar" or "Column") ? Visibility.Visible : Visibility.Collapsed;

	public Visibility MetricVisibility => AnalysisChartType is "Line" or "Scatter" ? Visibility.Visible : Visibility.Collapsed;
	public Visibility ThresholdsVisibility => AnalysisChartType == "Pie" ? Visibility.Visible : Visibility.Collapsed;

	[ObservableProperty]
	public partial string AnalysisProcess { get; set; } = string.Empty;

	[ObservableProperty]
	public partial string SelectedMetric { get; set; } = "MsBetweenDisplayChange";

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

	public event Action? StatisticToggled;

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

	public string GetStatisticTooltip(string key) => Catalog.StatisticDescriptions.TryGetValue(key, out string? desc) ? desc : string.Empty;

	public string GetMetricTooltip(string key) => Catalog.MetricDescriptions.TryGetValue(key, out string? desc) ? desc : string.Empty;

	public void BuildStatistics()
	{
		List<RecordingAnalysis> results = CachedAnalysis;
		if (results.Count == 0)
		{
			StatisticsRows = [];
			return;
		}

		List<ResultRow> groups = [];
		foreach ((string? name, Func<AnalysisResult, Metrics>? selector, Dictionary<string, Statistics>? statistics) in Catalog.GetStatisticGroups(StutterFactor, LowFpsThreshold))
		{
			Metrics m0 = selector(results[0].Analysis);
			Metrics? m1 = results.Count > 1 ? selector(results[1].Analysis) : null;

			var group = new ResultRow
			{
				Statistic = name,
				Tooltip = Catalog.MetricDescriptions.TryGetValue(name, out string? tip) ? tip : "Benchmark statistic."
			};
			foreach ((string? key, Statistics definition) in statistics)
			{
				double valueA = Catalog.GetStatistic(m0, key);
				double valueB = m1 == null ? 0 : Catalog.GetStatistic(m1, key);
				group.Children.Add(new ResultRow
				{
					Statistic = definition.Label,
					Tooltip = definition.Description,
					RecordingA = definition.FormatValue(valueA, m0),
					RecordingB = m1 == null ? "" : definition.FormatValue(valueB, m1),
					RecordingAValue = valueA,
					RecordingBValue = m1 == null ? null : valueB,
					RecordingASeconds = Catalog.GetStatisticSeconds(m0, key),
					RecordingBSeconds = m1 == null ? null : Catalog.GetStatisticSeconds(m1, key),
					Definition = definition
				});
			}
			groups.Add(group);
		}

		if (groups.Count == 0)
		{
			StatisticsRows = [];
			return;
		}

		StatisticsRows = [.. groups];
		ApplyStatisticsComparisons();
	}

	public void UpdateStutterStatistics(double? stutterFactor = null, double? lowFpsThreshold = null)
	{
		List<RecordingAnalysis> results = CachedAnalysis;
		ResultRow? stutterGroup = StatisticsRows.FirstOrDefault(row => row.Statistic == "Stutter Analysis");
		if (results.Count == 0 || stutterGroup is null)
			return;

		double factor = stutterFactor ?? StutterFactor;
		double threshold = lowFpsThreshold ?? LowFpsThreshold;

		Metrics m0 = StatisticsCalculator.GetStutterMetrics(results[0].Analysis, factor, threshold);
		Metrics? m1 = results.Count > 1 ? StatisticsCalculator.GetStutterMetrics(results[1].Analysis, factor, threshold) : null;

		foreach ((string? key, Statistics definition) in Catalog.StutterStatistics)
		{
			ResultRow? row = stutterGroup.Children.FirstOrDefault(child => child.Statistic == definition.Label);
			if (row is null)
				continue;
			double valueA = Catalog.GetStatistic(m0, key);
			double valueB = m1 == null ? 0 : Catalog.GetStatistic(m1, key);
			row.RecordingA = definition.FormatValue(valueA, m0);
			row.RecordingB = m1 == null ? "" : definition.FormatValue(valueB, m1);
			row.RecordingAValue = valueA;
			row.RecordingBValue = m1 == null ? null : valueB;
			row.RecordingASeconds = Catalog.GetStatisticSeconds(m0, key);
			row.RecordingBSeconds = m1 == null ? null : Catalog.GetStatisticSeconds(m1, key);
		}

		ApplyStatisticsComparisons(stutterGroup.Children);
	}

	public void ApplyStatisticsComparisons()
		=> ApplyStatisticsComparisons(StatisticsRows.SelectMany(group => group.Children));

	private void ApplyStatisticsComparisons(IEnumerable<ResultRow> rows)
	{
		int baselineIndex = BaselineIndex;
		bool showPercentDelta = IsPercentDelta;

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
	public partial string DeltaHeader { get; set; } = string.Empty;

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

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(RecordingAColorBrush))]
	[NotifyPropertyChangedFor(nameof(RecordingASecondaryColor))]
	public partial Windows.UI.Color RecordingAColor { get; set; } = Colors.DodgerBlue;

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(RecordingBColorBrush))]
	[NotifyPropertyChangedFor(nameof(RecordingBSecondaryColor))]
	public partial Windows.UI.Color RecordingBColor { get; set; } = Colors.Orange;

	[ObservableProperty]
	public partial Windows.UI.Color RecordingASecondaryColor { get; set; }

	[ObservableProperty]
	public partial Windows.UI.Color RecordingBSecondaryColor { get; set; }

	[ObservableProperty]
	public partial Windows.UI.Color RecordingATertiaryColor { get; set; }

	[ObservableProperty]
	public partial Windows.UI.Color RecordingBTertiaryColor { get; set; }

	[ObservableProperty]
	public partial BrushCollection PieChart1Palette { get; set; } = new BrushCollection();

	[ObservableProperty]
	public partial BrushCollection PieChart2Palette { get; set; } = new BrushCollection();

	[ObservableProperty]
	public partial SolidColorBrush RecordingAColorBrush { get; set; } = new SolidColorBrush();

	[ObservableProperty]
	public partial SolidColorBrush RecordingBColorBrush { get; set; } = new SolidColorBrush();

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
	public partial ComparisonResult RecordingAComparison { get; set; }

	[ObservableProperty]
	public partial ComparisonResult RecordingBComparison { get; set; }

	[ObservableProperty]
	public partial ComparisonResult DeltaComparison { get; set; }

	public ObservableCollection<ResultRow> Children { get; } = [];

	internal double? RecordingAValue { get; set; }
	internal double? RecordingBValue { get; set; }
	internal double? RecordingASeconds { get; set; }
	internal double? RecordingBSeconds { get; set; }
	internal Statistics Definition { get; set; }
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

[WinRT.GeneratedBindableCustomProperty]
public sealed partial class PiePoint : ObservableObject
{
	[ObservableProperty]
	public partial string Label { get; set; } = string.Empty;

	[ObservableProperty]
	public partial double Value { get; set; }
}
