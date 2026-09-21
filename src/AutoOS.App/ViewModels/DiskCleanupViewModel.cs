using System.Diagnostics;
using AutoOS.Core.Services.DiskAnalyzer;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

namespace AutoOS.App.ViewModels;

public sealed partial class DiskCleanupViewModel : ObservableObject
{
	private readonly DiskAnalyzerService _service = new();
	private readonly DispatcherQueue? _ui;
	private CancellationTokenSource? _cts;
	private CancellationTokenSource? _probeCts;
	// File View row sets. The scan itself ships the largest files, the extension breakdown and the
	// duplicate flags, and the folder rows land right after it, so every Folders / Duplicates only
	// combination is a list pick instead of a fresh walk of the drive.
	private IReadOnlyList<DiskFileRow> _cachedTopFiles = [];
	private IReadOnlyList<DiskExtensionStat> _cachedExtensions = [];
	private IReadOnlyList<DiskFileRow> _cachedDuplicateFiles = [];
	private IReadOnlyList<DiskFileRow> _cachedFilesWithFolders = [];
	private IReadOnlyList<DiskFileRow> _cachedDuplicatesWithFolders = [];
	private long _cachedTotalAllocated;
	private int _scanGen;
	// Treemap file layer: parent-grouped tiles built during the scan itself, handed to the
	// treemap control together with the tree so both paint in one pass. The treemap owns
	// (and mutates, on delete) the dictionary after handoff.
	public Dictionary<DiskNode, List<TreemapFile>> TreemapFileGroups { get; private set; } = new();

	public ObservableCollection<DiskNode> TreeNodes { get; } = [];
	public ObservableCollection<DriveModelLite> Drives { get; } = [];
	// Plain list, not an ObservableCollection: the File View grid rebinds once per filter instead of
	// processing one collection change per row (10k row Adds were the toggle delay).
	[ObservableProperty]
	public partial IReadOnlyList<DiskFileRow> FileRows { get; set; } = [];
	public ObservableCollection<DiskExtensionStat> ExtensionStats { get; } = [];

	[ObservableProperty]
	public partial string SelectedRoot { get; set; } = "C:\\";

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(IsContentBusy))]
	[NotifyPropertyChangedFor(nameof(IsContentReady))]
	[NotifyPropertyChangedFor(nameof(LoadingVisibility))]
	[NotifyPropertyChangedFor(nameof(TreeResultsVisibility))]
	public partial bool IsScanning { get; set; }

	[ObservableProperty]
	public partial bool UseEverything { get; set; } = true;

	[ObservableProperty]
	public partial bool IsEverythingAvailable { get; set; }

	// WizTree File View toolbar (lazy: applied only when the File tab builds)
	[ObservableProperty]
	public partial bool IncludeFoldersInFileView { get; set; }

	[ObservableProperty]
	public partial double MaxFilesToDisplay { get; set; } = 1000;

	[ObservableProperty]
	public partial bool DuplicatesOnly { get; set; }

	[ObservableProperty]
	public partial string FileStatusText { get; set; } = string.Empty;

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(FileToolbarVisibility))]
	public partial string ActiveTab { get; set; } = "TreeView";

	public bool IsContentBusy => IsScanning;

	public bool IsContentReady => !IsContentBusy;

	public Visibility LoadingVisibility => IsContentBusy ? Visibility.Visible : Visibility.Collapsed;

	public Visibility TreeResultsVisibility => IsContentReady ? Visibility.Visible : Visibility.Collapsed;

	public Visibility FileToolbarVisibility => ActiveTab == "FileView" ? Visibility.Visible : Visibility.Collapsed;

	public Action? RefreshFileFilterAction { get; set; }

	/// <summary>
	/// SfTreeGrid bulk-update bracket. Call before TreeNodes / ExtensionStats bulk-Adds to
	/// suppress one CollectionView refresh per row (Syncfusion TreeGrid perf best-practice).
	/// </summary>
	public Action? SuspendGridUpdatesAction { get; set; }

	public Action? ResumeGridUpdatesAction { get; set; }

	public DiskCleanupViewModel()
	{
		_ui = DispatcherQueue.GetForCurrentThread();
		LoadDrives();
		_ = RefreshProbeStateAsync();
	}

	public void LoadDrives()
	{
		Drives.Clear();
		foreach (var d in DriveInfo.GetDrives().Where(x => x.IsReady))
		{
			Drives.Add(new DriveModelLite
			{
				Label = string.IsNullOrWhiteSpace(d.VolumeLabel) ? d.Name.TrimEnd('\\') : $"{d.VolumeLabel} ({d.Name.TrimEnd('\\')})",
				RootPath = d.Name,
				TotalBytes = d.TotalSize,
				FreeBytes = d.TotalFreeSpace
			});
		}

		if (Drives.Count > 0 && !Drives.Any(x => x.RootPath.Equals(SelectedRoot, StringComparison.OrdinalIgnoreCase)))
			SelectedRoot = Drives[0].RootPath;
	}

	public async Task RefreshProbeStateAsync()
	{
		_probeCts?.Cancel();
		_probeCts?.Dispose();
		_probeCts = new CancellationTokenSource();

		try
		{
			IsEverythingAvailable = await _service.IsEverythingAvailableAsync(_probeCts.Token);
			UseEverything = IsEverythingAvailable;
		}
		catch (OperationCanceledException)
		{
		}
	}

	partial void OnActiveTabChanged(string value)
	{
		// WizTree loads File View only when the tab is opened.
		if (value == "FileView")
			EnsureFileViewLoaded();
	}

	// Toggles and the row cap only re-filter pre-built rows, so they apply immediately.
	partial void OnIncludeFoldersInFileViewChanged(bool value) => RefreshFileRows();
	partial void OnMaxFilesToDisplayChanged(double value) => RefreshFileRows();
	partial void OnDuplicatesOnlyChanged(bool value) => RefreshFileRows();

	private void RefreshFileRows()
	{
		// Every row set the check boxes can ask for already exists, so re-filtering is a list pick
		// plus the search text — never a fresh walk of the drive.
		RefreshFileFilterAction?.Invoke();
	}

	/// Called when the File tab is selected. The scan already aggregated the file rows and the
	/// extension breakdown, so opening the tab only re-applies the search text and the toggles.
	/// </summary>
	public void EnsureFileViewLoaded()
	{
		RefreshFileFilterAction?.Invoke();
	}

	/// <summary>
	/// Builds the folder rows for the Folders check box on a background thread once the scan has
	/// bound its tree: a walk of the tree only, no file pass and no duplicate pass, and the File View
	/// already shows the files while it runs. Stale completions are dropped through the scan
	/// generation, so a rescan never resurrects the previous drive's folders.
	/// </summary>
	private void BuildFolderRowsInBackground(int generation, IReadOnlyList<DiskNode> roots, long totalAllocated)
	{
		_ = Task.Run(() => DiskAnalyzerService.BuildFolderRows(roots, totalAllocated))
			.ContinueWith(task =>
			{
				void Apply()
				{
					if (generation != _scanGen)
						return; // newer scan started; discard stale rows
					if (task.IsCanceled)
						return;
					if (task.IsFaulted)
					{
						Trace.WriteLine($"[DiskCleanup] folder-row build failed: {task.Exception?.GetBaseException().Message}");
						return;
					}

					_cachedFilesWithFolders = MergeWithFolders(task.Result, _cachedTopFiles);
					_cachedDuplicatesWithFolders = FilterDuplicates(_cachedFilesWithFolders);
					if (IncludeFoldersInFileView)
						ApplyFileFilter();
				}

				if (_ui != null)
					_ui.TryEnqueue(Apply);
				else
					Apply();
			}, TaskScheduler.Default);
	}

	/// <summary>
	/// Applies the current search text, Folders and Duplicates only state to the pre-built row sets
	/// and rebinds the grid once. Cheap enough to run on every check box click.
	/// </summary>
	public void ApplyFileFilter()
	{
		if (_cachedTopFiles.Count == 0)
		{
			FileRows = [];
			// Never clobber a real scan failure: opening the File View tab re-runs this
			// filter, which used to overwrite "Scan failed: ..." with the text below and
			// hide the actual error.
			if (!FileStatusText.StartsWith("Scan failed", StringComparison.OrdinalIgnoreCase))
			{
				FileStatusText = TreeNodes.Count == 0
					? "Scan a drive first."
					: "Folder-index scan has no file rows — extension and file views need a full scan.";
			}

			return;
		}

		int max = Math.Clamp((int)MaxFilesToDisplay, 100, 10000);
		var rows = new List<DiskFileRow>(Math.Min(max, _cachedTopFiles.Count));
		long shownSize = 0;
		long shownAlloc = 0;
		foreach (DiskFileRow row in SelectFileRows())
		{
			if (rows.Count >= max)
				break;

			rows.Add(row);
			shownSize += row.Size;
			shownAlloc += row.Allocated;
		}

		FileRows = rows;
		FileStatusText = $"{rows.Count:N0} files — Total Size: {DiskNode.FormatBytes(shownSize)}  Allocated: {DiskNode.FormatBytes(shownAlloc)}";
	}

	/// <summary>
	/// Rows matching the current Folders / Duplicates only toggles. Every combination was built
	/// once with the scan, so a toggle never re-walks or re-sorts the drive.
	/// </summary>
	private IReadOnlyList<DiskFileRow> SelectFileRows()
	{
		if (IncludeFoldersInFileView && _cachedFilesWithFolders.Count > 0)
			return DuplicatesOnly ? _cachedDuplicatesWithFolders : _cachedFilesWithFolders;

		return DuplicatesOnly ? _cachedDuplicateFiles : _cachedTopFiles;
	}

	private static List<DiskFileRow> FilterDuplicates(IReadOnlyList<DiskFileRow> rows)
	{
		var duplicates = new List<DiskFileRow>();
		foreach (DiskFileRow row in rows)
			if (row.DupCount > 1)
				duplicates.Add(row);

		return duplicates;
	}

	/// <summary>
	/// Two-way merge of two allocated-descending row sets, so combining folders with files never
	/// re-sorts them.
	/// </summary>
	private static List<DiskFileRow> MergeWithFolders(IReadOnlyList<DiskFileRow> folders, IReadOnlyList<DiskFileRow> files)
	{
		var merged = new List<DiskFileRow>(folders.Count + files.Count);
		int folderIndex = 0;
		int fileIndex = 0;
		while (folderIndex < folders.Count && fileIndex < files.Count)
		{
			if (DiskFileRow.CompareByAllocatedDescending(folders[folderIndex], files[fileIndex]) <= 0)
			{
				merged.Add(folders[folderIndex]);
				folderIndex++;
			}
			else
			{
				merged.Add(files[fileIndex]);
				fileIndex++;
			}
		}

		while (folderIndex < folders.Count)
			merged.Add(folders[folderIndex++]);

		while (fileIndex < files.Count)
			merged.Add(files[fileIndex++]);

		return merged;
	}

	/// <summary>
	/// Starts a scan of <paramref name="rootPath"/> for the drive cards, so the page never invokes
	/// the generated command itself.
	/// </summary>
	public void StartScan(string rootPath)
	{
		SelectedRoot = rootPath;
		_ = ScanAsync();
	}

	[RelayCommand]
	private async Task ScanAsync()
	{
		if (IsScanning)
			return;

		_cts?.Cancel();
		_cts?.Dispose();
		_cts = new CancellationTokenSource();
		CancellationToken token = _cts.Token;
		int generation = ++_scanGen; // invalidate the previous scan's in-flight folder-row build
		IsScanning = true;
		TreeNodes.Clear();
		FileRows = [];
		ExtensionStats.Clear();
		_cachedTopFiles = [];
		_cachedExtensions = [];
		_cachedDuplicateFiles = [];
		_cachedFilesWithFolders = [];
		_cachedDuplicatesWithFolders = [];
		TreemapFileGroups = new();

		try
		{
			var options = new ScanOptions
			{
				RootPath = SelectedRoot,
				PreferredScanner = UseEverything ? ScannerKind.Everything : ScannerKind.Enumeration,
				SearchFilter = string.Empty,
				IncludeModifiedDates = true // Modified column shows last-write time (WizTree parity)
			};

			var scanWatch = Stopwatch.StartNew();
			ScanResult result = await _service.AnalyzeAsync(options, cancellationToken: token);
			scanWatch.Stop();
			Trace.WriteLine($"[DiskCleanup] scan {scanWatch.Elapsed.TotalSeconds:F2}s — {result.FileCount:N0} files, {result.FolderCount:N0} folders, {DiskNode.FormatBytes(result.TotalAllocated)} allocated via {result.UsedScanner}{(result.UsedFallback ? " (fallback)" : string.Empty)}");

			// Batch UI updates to avoid freeze on 45k folder nodes + 1k file nodes
			await Task.Yield();

			var bindWatch = Stopwatch.StartNew();
			SuspendGridUpdatesAction?.Invoke();
			try
			{
				TreeNodes.Clear();
				foreach (DiskNode root in result.Roots)
					TreeNodes.Add(root);

				// The scan aggregated the largest files, the extension breakdown and the duplicate
				// flags while it built the tree, so the File View is ready the moment the tab opens.
				_cachedTotalAllocated = result.TotalAllocated;
				_cachedTopFiles = result.FileView.TopFiles;
				_cachedExtensions = result.FileView.Extensions;
				_cachedDuplicateFiles = FilterDuplicates(_cachedTopFiles);
				TreemapFileGroups = result.TreemapFileGroups;
				ExtensionStats.Clear();
				foreach (DiskExtensionStat stat in _cachedExtensions)
					ExtensionStats.Add(stat);

				ApplyFileFilter();
				BuildFolderRowsInBackground(generation, new List<DiskNode>(TreeNodes), _cachedTotalAllocated);
			}
			finally
			{
				ResumeGridUpdatesAction?.Invoke();
			}

			bindWatch.Stop();
			Trace.WriteLine($"[DiskCleanup] UI bind: {bindWatch.Elapsed.TotalSeconds:F2}s");
			try
			{
				IsEverythingAvailable = await _service.IsEverythingAvailableAsync(token);
			}
			catch (OperationCanceledException)
			{
			}
			catch (Exception ex)
			{
				IsEverythingAvailable = false;
				Trace.WriteLine($"[DiskCleanup] Everything availability probe failed: {ex.Message}");
			}
		}
		catch (OperationCanceledException)
		{
			Trace.WriteLine("[DiskCleanup] scan canceled");
		}
		catch (Exception ex)
		{
			FileStatusText = $"Scan failed: {ex.Message}";
			Trace.WriteLine($"[DiskCleanup] scan failed: {ex}");
		}
		finally
		{
			IsScanning = false;
		}
	}

}

public sealed class DriveModelLite
{
	public string Label { get; set; } = string.Empty;

	public string RootPath { get; set; } = string.Empty;

	public long TotalBytes { get; set; }

	public long FreeBytes { get; set; }

	public long UsedBytes => TotalBytes - FreeBytes;

	// Double projections for the compiled StorageBar bindings (x:Bind has no long→double
	// implicit conversion, and a XAML-side cast trips the experimental markup compiler).
	public double TotalBytesDouble => TotalBytes;

	public double UsedBytesDouble => UsedBytes;

	public string FreeText => $"{DiskNode.FormatBytes(FreeBytes)} free of {DiskNode.FormatBytes(TotalBytes)}";
}
