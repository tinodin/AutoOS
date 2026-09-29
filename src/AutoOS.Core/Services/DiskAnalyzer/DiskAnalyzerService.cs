using System.Buffers;
using System.Diagnostics;

namespace AutoOS.Core.Services.DiskAnalyzer;

public sealed class DiskAnalyzerService
{
	private static readonly TimeSpan AvailabilityCacheTtl = TimeSpan.FromSeconds(30);

	private const int TOP_FILES_KEEP = 20000;
	private const string NO_EXTENSION_KEY = "(No Extension)";

	private readonly EverythingIpcScanner _everythingIpc = new();
	private readonly EnumerationScanner _enumerationScanner = new();

	private bool? _cachedAvailability;
	private bool _cachedIpcAvailable;
	private DateTime _cachedAvailabilityAt = DateTime.MinValue;
	private string? _cachedProbeDetails;

	public string? EverythingProbeDetails
	{
		get
		{
			if (_cachedProbeDetails != null)
				return _cachedProbeDetails;

			return "Everything probe pending — first scan probes once and caches for 30s";
		}
	}

	public async Task<bool> IsEverythingAvailableAsync(CancellationToken cancellationToken = default)
	{
		if (_cachedAvailability.HasValue && DateTime.UtcNow - _cachedAvailabilityAt < AvailabilityCacheTtl)
			return _cachedAvailability.Value;

		// Single transport: IPC is a window check (<100ms) against the running server.
		bool ipc = await _everythingIpc.IsAvailableAsync(cancellationToken).ConfigureAwait(false);
		_cachedIpcAvailable = ipc;

		_cachedAvailability = ipc;
		_cachedAvailabilityAt = DateTime.UtcNow;
		_cachedProbeDetails = ipc
			? "Everything IPC available (server running)"
			: "Everything not available — start Everything or use enumeration";

		return _cachedAvailability.Value;
	}

	public async Task<ScanResult> AnalyzeAsync(ScanOptions options, IProgress<double>? progress = null, IProgress<ScanProgressReport>? detailedProgress = null, CancellationToken cancellationToken = default)
	{
		CancellationToken token = cancellationToken;
		var sw = Stopwatch.StartNew();
		void Report(string phase, int scanned = 0, int total = 0, string? detail = null)
			=> detailedProgress?.Report(new ScanProgressReport { Phase = phase, FilesScanned = scanned, TotalFiles = total, Elapsed = sw.Elapsed, Detail = detail });

		IReadOnlyList<ScanItem> items;
		ScannerKind used = options.PreferredScanner;
		bool fallback = false;

		try
		{
			if (options.PreferredScanner == ScannerKind.Everything)
			{
				// Re-probe when the cache is stale or cold so a first post-boot scan
				// lands on IPC instead of racing to the enumeration fallback.
				if (!_cachedAvailability.HasValue || DateTime.UtcNow - _cachedAvailabilityAt > AvailabilityCacheTtl)
					await IsEverythingAvailableAsync(token).ConfigureAwait(false);
				Report("Probing Everything", detail: _cachedProbeDetails);

				// IPC: Windhawk-style server queries (WM_COPYDATA QUERY2) against the
				// running server — no client DLL or es.exe. Anything else falls through
				// to the catch below, which runs the walker.
				if (_cachedIpcAvailable)
				{
					string normalizedRoot = DiskCluster.NormalizeRoot(options.RootPath);
					bool isDriveRoot = normalizedRoot.Length == 3 && normalizedRoot[1] == ':' && normalizedRoot[2] == '\\';
					if (isDriveRoot && string.IsNullOrWhiteSpace(options.SearchFilter))
						items = await ScanDriveRootAsync(options, normalizedRoot, detailedProgress, sw, token).ConfigureAwait(false);
					else
					{
						Report("Querying Everything (IPC)");
						items = await _everythingIpc.ScanAsync(options, progress, token).ConfigureAwait(false);
					}
				}
				else
				{
					Report("Everything not available — falling back to enumeration", detail: EverythingProbeDetails);
					fallback = true;
					used = ScannerKind.Enumeration;
					items = await _enumerationScanner.ScanAsync(options, progress, token).ConfigureAwait(false);
				}
			}
			else
			{
				Report("Enumerating filesystem (single universal fallback)");
				items = await _enumerationScanner.ScanAsync(options, progress, token).ConfigureAwait(false);
				used = ScannerKind.Enumeration;
			}
		}
		catch (Exception ex) when (options.PreferredScanner == ScannerKind.Everything && ex is not OperationCanceledException)
		{
			Trace.WriteLine($"Everything scan failed, falling back: {ex.Message}");
			Report("Everything failed — fallback enumeration", detail: ex.Message);
			fallback = true;
			used = ScannerKind.Enumeration;
			items = await _enumerationScanner.ScanAsync(options, progress, token).ConfigureAwait(false);
		}

		// IPC + MFT align allocated sizes inline, so no fixup pass is needed.
		Report("Building tree", scanned: 0, total: items.Count, detail: $"~{items.Count:N0} items — offloading to background thread");

		var scanSw = Stopwatch.StartNew();

		// Single background pass: accumulate folder sizes (was two full passes).
		var buildResult = await Task.Run(() =>
		{
			// The File View aggregation is a second full pass over the scan rows, but it shares no
			// state with the tree build, so both run at once and the File View data is ready the
			// moment the scan finishes instead of on the first File-tab open. Treemap tiles ride
			// inside the tree's own file pass, so no separate retain/regroup pass exists at all.
			var fileViewSw = Stopwatch.StartNew();
			Task<FileViewInputs> fileViewTask = Task.Run(() => BuildFileViewInputs(items, token), token);

			var treeSw = Stopwatch.StartNew();
			var roots = BuildTree(items, options.RootPath, out long totalSize, out long totalAllocated, out int fileCount, out int folderCount, out Dictionary<long, DiskNode>? recordNodes, out Dictionary<DiskNode, List<TreemapFile>> treemapGroups, detailedProgress, sw, token);
			treeSw.Stop();

			// The aggregation cannot know the drive total on its own, so percents land here.
			FileViewInputs fileView = fileViewTask.GetAwaiter().GetResult();
			fileViewSw.Stop();
			ApplyTotalAllocated(fileView, totalAllocated, recordNodes);

			// P2 telemetry: per-phase splits so the next bottleneck is measured, not guessed.
			Trace.WriteLine($"[DiskAnalyzer] build split: tree {treeSw.Elapsed.TotalMilliseconds:F0}ms, fileview {fileViewSw.Elapsed.TotalMilliseconds:F0}ms for {items.Count:N0} items");

			return (roots, totalSize, totalAllocated, fileCount, folderCount, fileView, treemapGroups, recordNodes);
		}, token).ConfigureAwait(false);

		scanSw.Stop();
		Trace.WriteLine($"[DiskAnalyzer] tree + File View aggregation: {scanSw.Elapsed.TotalSeconds:F2}s for {items.Count:N0} items");

		sw.Stop();
		Report("Scan complete", scanned: buildResult.fileCount, total: buildResult.fileCount, detail: $"{buildResult.fileCount:N0} files, {buildResult.folderCount:N0} folders in {sw.Elapsed.TotalSeconds:F1}s — {buildResult.totalSize:N0} bytes");
		return new ScanResult
		{
			RootPath = options.RootPath,
			UsedScanner = used,
			UsedFallback = fallback,
			Duration = sw.Elapsed,
			TotalSize = buildResult.totalSize,
			TotalAllocated = buildResult.totalAllocated,
			FileCount = buildResult.fileCount,
			FolderCount = buildResult.folderCount,
			Roots = buildResult.roots,
			FileView = buildResult.fileView,
			TreemapFileGroups = buildResult.treemapGroups,
			RecordNodes = buildResult.recordNodes
		};
	}

	// Normalized-extension cache: a drive holds hundreds of thousands of files but only a
	// few hundred distinct extensions, so each distinct spelling is lowered/dotted once and
	// every file after that is a single dictionary hit (no per-file Trim/Lower/Concat garbage).
	private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, string> _treemapExtensionCache =
		new(StringComparer.Ordinal);

	/// <summary>
	/// Normalizes a raw scanner extension to treemap palette form: lowercase with a leading
	/// dot, empty when there is no extension. Mirrors the app-side palette helper so tiles
	/// grouped here hash to exactly the same colors as the extension grid.
	/// </summary>
	private static string NormalizeTreemapExtension(string extension)
		=> _treemapExtensionCache.GetOrAdd(extension, static raw =>
		{
			if (string.IsNullOrWhiteSpace(raw) ||
				raw.Trim().Equals("(No Extension)", StringComparison.OrdinalIgnoreCase))
				return string.Empty;

			string normalized = raw.Trim().ToLowerInvariant();
			if (normalized.Length == 0 || normalized.StartsWith('.'))
				return normalized;

			return string.Concat(".", normalized);
		});

	/// <summary>
	/// Builds the File View inputs in one background pass: the largest files with their duplicate
	/// info, and the extension breakdown. A <see cref="ScanItem"/> list belongs to the scan that
	/// produced it and must never be walked on the UI thread, so this runs inside the scan on a
	/// thread pool thread. Drive-relative percents need the tree total and are filled in afterwards
	/// by <see cref="ApplyTotalAllocated"/>.
	/// </summary>
	public static FileViewInputs BuildFileViewInputs(IReadOnlyList<ScanItem> items, CancellationToken token)
	{
		// Duplicate groups by (name, size, modified): WizTree "Locate by Name, Size, Date" default.
		// P1: keys are ~unique per file, so size for items.Count, not items.Count/4:
		// the old quarter-size forced two full rehashes on a 400k-file drive scan.
		var duplicateGroups = new Dictionary<DuplicateKey, DuplicateGroup>(items.Count, DuplicateKeyComparer.Instance);
		var extensionMap = new Dictionary<string, DiskExtensionStat>(256, StringComparer.OrdinalIgnoreCase);
		var extensionLookup = extensionMap.GetAlternateLookup<ReadOnlySpan<char>>();
		// Top files: bounded min-heap instead of materializing and sorting every row — the previous
		// shape allocated a DiskFileRow per file and sorted ~450k rows on each scan.
		var topFiles = new PriorityQueue<DiskFileRow, (long Allocated, long Size)>(TOP_FILES_KEEP);
		var seenFileIds = new HashSet<long>();
		bool hasFile = false;

		foreach (ScanItem item in items)
		{
			token.ThrowIfCancellationRequested();
			if (item.IsFolder)
				continue;

			hasFile = true;
			// Lite rows carry the bare name (no path string was ever built); classic rows
			// split it out of the full path. Either way nothing allocates per file here.
			string? liteName = item.Name;
			string fullPath = item.FullPath;
			ReadOnlySpan<char> nameSpan = liteName is not null ? liteName.AsSpan()
				: fullPath.AsSpan(GetNameOffset(fullPath));
			ReadOnlySpan<char> extensionSpan = !string.IsNullOrEmpty(item.Extension)
				? item.Extension.AsSpan()
				: Path.GetExtension(nameSpan);

			// Duplicate info is only ever resolved for listed rows, and rows strictly below
			// the heap floor can never be listed (equal keys still tracked: a tie member
			// may join the heap, and its group count must include skipped members).
			bool trackDup = topFiles.Count < TOP_FILES_KEEP
				|| !topFiles.TryPeek(out _, out (long Allocated, long Size) floor)
				|| (item.AllocatedSize, item.Size).CompareTo(floor) >= 0;
			if (trackDup)
			{
				// The key compares only the name part and never allocates: lite rows hand
				// over their stored name, classic rows keep the already-allocated path.
				DuplicateKey duplicateKey = new(fullPath, liteName, item.Size, item.Modified.Ticks);
				if (duplicateGroups.TryGetValue(duplicateKey, out DuplicateGroup group))
				{
					group.Count++;
					group.TotalSize += item.Size;
					group.TotalAllocated += item.AllocatedSize;
					duplicateGroups[duplicateKey] = group;
				}
				else
				{
					duplicateGroups[duplicateKey] = new DuplicateGroup { Count = 1, TotalSize = item.Size, TotalAllocated = item.AllocatedSize };
				}
			}

			// Alternate lookup keeps the per-file extension probe allocation-free: the span only
			// becomes a string when the extension is seen for the first time.
			if (!extensionLookup.TryGetValue(extensionSpan, out DiskExtensionStat? stat))
			{
				string extensionKey = extensionSpan.IsEmpty ? NO_EXTENSION_KEY : extensionSpan.ToString().ToLowerInvariant();
				stat = new DiskExtensionStat { Extension = extensionKey, FileType = DiskExtensionStat.GetFileType(extensionKey) };
				extensionMap[extensionKey] = stat;
			}

			stat.Files++;
			if (item.FileId <= 0 || seenFileIds.Add(item.FileId))
			{
				stat.Size += item.Size;
				stat.Allocated += item.AllocatedSize;
			}

			if (topFiles.Count < TOP_FILES_KEEP)
			{
				topFiles.Enqueue(CreateFileRow(item, fullPath, nameSpan, liteName), (item.AllocatedSize, item.Size));
			}
			else if (topFiles.TryPeek(out _, out (long Allocated, long Size) smallest) && (item.AllocatedSize, item.Size).CompareTo(smallest) > 0)
			{
				topFiles.Dequeue();
				topFiles.Enqueue(CreateFileRow(item, fullPath, nameSpan, liteName), (item.AllocatedSize, item.Size));
			}
		}

		if (!hasFile)
			return FileViewInputs.Empty; // folder-index scan: File View and extension panel stay empty

		var rows = new List<DiskFileRow>(topFiles.Count);
		foreach ((DiskFileRow row, _) in topFiles.UnorderedItems)
		{
			// Duplicate info is only resolved for the rows that actually get listed.
			// row.FileName is the effective name on both paths, so lite and classic keys agree.
			if (duplicateGroups.TryGetValue(new DuplicateKey(row.FullPath, row.FileName, row.Size, row.Modified.Ticks), out DuplicateGroup group) && group.Count > 1)
			{
				row.DupCount = group.Count;
				row.DupSize = group.TotalSize;
			}

			rows.Add(row);
		}
		rows.Sort(DiskFileRow.CompareByAllocatedDescending);

		var extensions = new List<DiskExtensionStat>(extensionMap.Values);
		extensions.Sort(static (a, b) => b.Size.CompareTo(a.Size));

		return new FileViewInputs(rows, extensions);
	}

	/// <summary>
	/// Fills in the drive-relative percents of a File View aggregate. Only the tree knows the drive
	/// total, and the aggregation runs before it, so percents land here — along with the deferred
	/// lite-row paths: MFT file rows ship without path strings, and only listed rows ever pay
	/// for the parent-path concat. <paramref name="recordNodes"/> maps MFT record ids to folder
	/// nodes; null (or ParentId &lt; 0 rows) keeps classic behavior.
	/// </summary>
	public static void ApplyTotalAllocated(FileViewInputs inputs, long totalAllocated, IReadOnlyDictionary<long, DiskNode>? recordNodes = null)
	{
		foreach (DiskFileRow row in inputs.TopFiles)
		{
			row.PercentOfDrive = totalAllocated > 0 ? (double)row.Allocated / totalAllocated * 100 : 0;
			if (row.ParentId > 0 && row.FullPath.Length == 0 && recordNodes != null && recordNodes.TryGetValue(row.ParentId, out DiskNode? parent) && parent != null)
			{
				row.Directory = parent.FullPath;
				row.FullPath = parent.FullPath + "\\" + row.FileName;
			}
		}

		foreach (DiskExtensionStat extension in inputs.Extensions)
			extension.Percent = totalAllocated > 0 ? (double)extension.Allocated / totalAllocated * 100 : 0;
	}

	private static int GetNameOffset(string fullPath)
	{
		int separator = fullPath.LastIndexOf('\\');
		return separator >= 0 ? separator + 1 : 0;
	}

	private static ReadOnlySpan<char> GetFileNameSpan(string fullPath)
		=> fullPath.AsSpan(GetNameOffset(fullPath));

	private static DiskFileRow CreateFileRow(ScanItem item, string fullPath, ReadOnlySpan<char> nameSpan, string? liteName)
	{
		bool isLite = liteName is not null;
		int separator = isLite ? -1 : fullPath.LastIndexOf('\\');

		return new DiskFileRow
		{
			FileName = isLite ? liteName! : new string(nameSpan),
			Directory = isLite ? string.Empty : separator > 0 ? fullPath.Substring(0, separator) : fullPath,
			FullPath = isLite ? string.Empty : fullPath,
			ParentId = item.ParentId,
			Extension = !string.IsNullOrEmpty(item.Extension)
				? item.Extension
				: new string(Path.GetExtension(nameSpan)),
			Size = item.Size,
			Allocated = item.AllocatedSize,
			Modified = item.Modified,
			Attributes = item.Attributes ?? string.Empty
		};
	}

	/// <summary>
	/// Folder rows for the "Folders" File View toggle, allocated-descending so the toggle can merge
	/// them with the file rows instead of re-sorting every folder on each click. Cheap next to the
	/// file pass (one walk of the few hundred thousand tree nodes), so it runs after the scan and
	/// never inside scan timing.
	/// </summary>
	public static List<DiskFileRow> BuildFolderRows(IReadOnlyList<DiskNode> roots, long totalAllocated)
	{
		var rows = new List<DiskFileRow>(4096);
		foreach (DiskNode root in roots)
			CollectFolderRows(root, rows, totalAllocated);

		rows.Sort(DiskFileRow.CompareByAllocatedDescending);
		return rows;
	}

	private static void CollectFolderRows(DiskNode node, List<DiskFileRow> rows, long totalAllocated)
	{
		if (node.IsFolder)
		{
			rows.Add(new DiskFileRow
			{
				FileName = node.Name,
				Directory = Path.GetDirectoryName(node.FullPath) ?? node.FullPath,
				FullPath = node.FullPath,
				Size = node.Size,
				Allocated = node.Allocated,
				Modified = node.Modified,
				Attributes = node.Attributes,
				PercentOfDrive = totalAllocated > 0 ? (double)node.Allocated / totalAllocated * 100 : 0
			});
		}

		foreach (DiskNode child in node.Children)
			CollectFolderRows(child, rows, totalAllocated);
	}

	private static IReadOnlyList<DiskNode> BuildTree(IReadOnlyList<ScanItem> items, string rootPath, out long totalSize, out long totalAllocated, out int fileCount, out int folderCount, out Dictionary<long, DiskNode>? recordNodes, out Dictionary<DiskNode, List<TreemapFile>> treemapGroups, IProgress<ScanProgressReport>? progress = null, Stopwatch? sw = null, CancellationToken token = default)
	{
		totalSize = 0;
		totalAllocated = 0;
		fileCount = 0;
		folderCount = 0;
		treemapGroups = new();
		string rootFull = Path.GetFullPath(rootPath).TrimEnd('\\');
		// ConcurrentDictionary: the file-accumulation pass below runs on all cores with
		// lock-free map reads; folder creation (above) stays sequential, and a rare
		// missing parent falls back to a locked create (structurally impossible: every
		// emitted file's parent ships as a folder row, created before this pass).
		var map = new System.Collections.Concurrent.ConcurrentDictionary<string, DiskNode>(Environment.ProcessorCount, items.Count / 4, StringComparer.OrdinalIgnoreCase);
		// Record-id map for lite rows (MFT scans): file parents resolve by array-like dict
		// hit instead of a path hash — no LastIndexOf, no hashing, no parent compare.
		var recMap = new Dictionary<long, DiskNode>(4096);
		// Span lookup avoids allocating a parent-path key per level: folder creation keeps
		// string keys, but every hit (the common case — IPC rows arrive unordered, so the
		// consecutive-parent cache misses) resolves straight from the FullPath span.
		var lookup = map.GetAlternateLookup<ReadOnlySpan<char>>();
		var folderLock = new object();
		long lastProgressTicksMs = Environment.TickCount64;

		DiskNode GetOrCreateFolder(ReadOnlySpan<char> path)
		{
			// Span-based trim + name split: avoids Path.GetFileName/GetDirectoryName
			// normalization cost per folder. Keys stay trimmed ("C:\Windows", root "C:").
			ReadOnlySpan<char> span = path;
			if (span.Length > 0 && span[^1] == '\\')
				span = span.TrimEnd('\\');
			if (lookup.TryGetValue(span, out DiskNode? existing) && existing != null)
				return existing;

			int sep = span.LastIndexOf('\\');
			string trimmed = new string(span);
			string name = sep < 0 ? trimmed : new string(span.Slice(sep + 1));
			if (name.Length == 0)
				name = trimmed; // C:

			var node = new DiskNode { Name = name, FullPath = trimmed, IsFolder = true };
			map[trimmed] = node;
			if (!trimmed.Equals(rootFull, StringComparison.OrdinalIgnoreCase) && sep > 0)
			{
				var parentNode = GetOrCreateFolder(span.Slice(0, sep));
				parentNode.Children.Add(node);
				parentNode.FolderCount++; // direct child folders; PropagateSortAndPercent rolls up to recursive
			}
			return node;
		}

		var rootNode = GetOrCreateFolder(rootFull);
		rootNode.IsExpanded = true; // only root expanded; children collapsed for virtualization (WizTree-like)
		rootNode.PercentOfParent = 100; // WizTree root shows 100,0 %
		var phaseSw = Stopwatch.StartNew();

		// Add folders — WizTree: folder Modified/Attributes = its own directory entry
		// (not aggregated from children). Sizes are intentionally NOT applied here when
		// file rows exist: Everything's folder SIZE index is already recursive, so
		// applying it plus file accumulation plus the fused propagation would triple-count.
		// Single source of truth: file accumulation + bottom-up propagation (MFT path
		// has folder Size=0 anyway). Folder-only scans use the fast path below.
		bool hasFiles = false;
		foreach (ScanItem probe in items)
		{
			if (!probe.IsFolder)
			{
				hasFiles = true;
				break;
			}
		}

		int folderRows = 0, sizedFolderRows = 0;
		foreach (var it in items)
		{
			token.ThrowIfCancellationRequested();
			if (!it.IsFolder)
				continue;

			folderRows++;
			var n = GetOrCreateFolder(it.FullPath);
			// MFT folder rows carry their record id: lite file rows resolve parents by id.
			if (it.FileId > 0)
				recMap[it.FileId] = n;
			if (!hasFiles && it.Size > 0)
			{
				sizedFolderRows++;
				n.Size = it.Size;
				n.Allocated = it.AllocatedSize > 0 ? it.AllocatedSize : it.Size;
			}
			if (it.Modified != DateTime.MinValue && (n.Modified == DateTime.MinValue || it.Modified > n.Modified))
				n.Modified = it.Modified;
			if (!string.IsNullOrEmpty(it.Attributes))
				n.Attributes = it.Attributes;
		}
		// Folder-first fast path: folder-only rows with recursive sizes — sizes already
		// applied above, so skip file accumulate + propagation (would double-count).
		if (folderRows == items.Count && folderRows > 50000 && sizedFolderRows > folderRows / 2)
		{
			totalSize = rootNode.Size;
			totalAllocated = rootNode.Allocated;
			if (totalSize <= 0)
			{
				foreach (var c in rootNode.Children)
				{
					totalSize += c.Size;
					totalAllocated += c.Allocated;
				}
				rootNode.Size = totalSize;
				rootNode.Allocated = totalAllocated;
			}
			fileCount = 0;
			folderCount = map.Count - 1;
			if (folderCount < 0)
				folderCount = 0;
			// P2: one fused walk sorts, sets percents and reports the latest date —
			// was SortChildren here plus ComputePercents + FindLatestModified walks.
			DateTime fastLatest = SortAndPercent(rootNode, totalAllocated, token);
			ApplyLatestModified(rootNode, rootFull, fastLatest);
			Trace.WriteLine($"[BuildTree] folder-first {folderRows:N0} folders ({sizedFolderRows:N0} sized), sort+percents {phaseSw.ElapsedMilliseconds}ms (no file pass, no propagate)");
			recordNodes = recMap.Count > 0 ? recMap : null;
			return [rootNode];
		}
		long folderMs = phaseSw.ElapsedMilliseconds;
		phaseSw.Restart();
		int parentCacheHits = 0;

		// Add files — folder-only tree for perf (only folder nodes are bound to the TreeGrid).
		// Parallel accumulation: every file's parent folder node already exists (all folder
		// rows were created above), so workers only READ the map lock-free and pile sizes
		// into thread-local per-node deltas merged once at the end. No per-file ancestor
		// walk O(n*depth), no shared mutation during the walk.
		// Select the winning FRN row before partitioning. MFT hard-link aliases can otherwise
		// land on opposite sides of a worker range, which would make thread-local deduping
		// double-count the same allocated bytes in folder and drive totals.
		var keptFileRows = new bool[items.Count];
		var seenFileIds = new HashSet<long>(items.Count);
		int keptFileCount = 0;
		for (int index = 0; index < items.Count; index++)
		{
			var probe = items[index];
			token.ThrowIfCancellationRequested();
			if (probe.IsFolder)
				continue;
			if (probe.FileId <= 0 || seenFileIds.Add(probe.FileId))
			{
				keptFileRows[index] = true;
				keptFileCount++;
			}
		}

		long processedTotal = 0;
		var accSlots = new List<FileThreadState>();
		var accLock = new object();
		long accSize = 0, accAlloc = 0;
		int accFiles = 0, accCacheHits = 0;
		Parallel.ForEach(System.Collections.Concurrent.Partitioner.Create(0, items.Count),
			new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount), CancellationToken = token },
			() => new FileThreadState(),
			(range, _, st) =>
			{
				for (int i = range.Item1; i < range.Item2; i++)
				{
					var it = items[i];
					if (it.IsFolder)
						continue;
					if (!keptFileRows[i])
						continue;

					if ((st.n & 0xFFF) == 0)
					{
						token.ThrowIfCancellationRequested();
						if (sw != null && Environment.TickCount64 - Interlocked.Read(ref lastProgressTicksMs) >= 250)
						{
							Interlocked.Exchange(ref lastProgressTicksMs, Environment.TickCount64);
							progress?.Report(new ScanProgressReport { Phase = "Building tree", FilesScanned = (int)Math.Min(Interlocked.Read(ref processedTotal), int.MaxValue), TotalFiles = keptFileCount, Elapsed = sw.Elapsed, Detail = "accumulating" });
						}
					}
					st.n++;
					Interlocked.Increment(ref processedTotal);

					// Lite rows (MFT scans): parent node by record id — one dict hit, no
					// LastIndexOf, no span hashing, no parent-path compare.
					DiskNode? parent;
					if (it.ParentId > 0 && recMap.TryGetValue(it.ParentId, out DiskNode? byId) && byId != null)
					{
						parent = byId;
					}
					else
					{
						ReadOnlySpan<char> full = it.FullPath.AsSpan();
						int sep = full.LastIndexOf('\\');
						ReadOnlySpan<char> parentSpan = sep < 0 ? rootFull.AsSpan() : full.Slice(0, sep);
						parent = st.lastNode;
						if (parent is null || st.lastPath is null || !parentSpan.Equals(st.lastPath.AsSpan(), StringComparison.Ordinal))
						{
							if (!lookup.TryGetValue(parentSpan, out parent) || parent is null)
								parent = GetOrCreateFolderLocked(parentSpan, lookup, map, folderLock, rootFull);
							st.lastPath = parent.FullPath;
							st.lastNode = parent;
						}
						else
						{
							st.cacheHits++;
						}
					}

					if (!st.acc.TryGetValue(parent, out FileAcc a))
						a = default;
					a.Files++;
					st.files++;
					a.Size += it.Size;
					a.Alloc += it.AllocatedSize;
					st.size += it.Size;
					st.alloc += it.AllocatedSize;
					st.acc[parent] = a;
					// Treemap tile rides the same parent resolution: no second walk of the
					// file rows after the scan, and no path strings for lite rows. Zero-byte
					// entries never render, so they stay out of the groups.
					if (it.AllocatedSize > 0)
					{
						string name = it.Name ?? new string(GetFileNameSpan(it.FullPath));
						string rawExtension = !string.IsNullOrEmpty(it.Extension)
							? it.Extension
							: Path.GetExtension(name);
						st.tiles.Add((parent, new TreemapFile(name, NormalizeTreemapExtension(rawExtension), it.AllocatedSize, it.Size)));
					}
				}
				return st;
			},
			st =>
			{
				lock (accLock)
				{
					accSlots.Add(st);
					accSize += st.size;
					accAlloc += st.alloc;
					accFiles += st.files;
					accCacheHits += st.cacheHits;
				}
			});

		totalSize = accSize;
		totalAllocated = accAlloc;
		fileCount = accFiles;
		parentCacheHits = accCacheHits;
		foreach (var st in accSlots)
			foreach (var kvp in st.acc)
			{
				kvp.Key.FileCount += kvp.Value.Files;
				kvp.Key.Size += kvp.Value.Size;
				kvp.Key.Allocated += kvp.Value.Alloc;
			}

		// Single merge of the per-thread tile lists into parent-keyed groups: one linear
		// pass that replaces the old retain-every-ScanItem plus regroup-after-bind passes.
		// Pre-sized so 70k+ parent keys never trigger a rehash storm mid-merge.
		treemapGroups.EnsureCapacity(110000);
		long groupStart = Stopwatch.GetTimestamp();
		foreach (var st in accSlots)
		{
			foreach (var (parent, file) in st.tiles)
			{
				if (!treemapGroups.TryGetValue(parent, out var list))
				{
					// Most parents hold a handful of files: start tiny and let the few
					// crowded folders grow, instead of pre-allocating 64 slots (~3KB)
					// for all 70k parents (~200MB of instant garbage).
					list = new List<TreemapFile>(4);
					treemapGroups[parent] = list;
				}
				list.Add(file);
			}
			st.tiles.Clear();
		}
		long groupMs = (Stopwatch.GetTimestamp() - groupStart) * 1000 / Stopwatch.Frequency;
		Trace.WriteLine($"[BuildTree] treemap groups {groupMs}ms");

		// Bottom-up propagation via Children links (no GetDirectoryName per folder, no path-length sort).
		// Replaces per-file while(cur) walk that did 3M dict lookups for 500k files.
		// P2: propagation, sibling sort, allocated-based percents and the latest-modified
		// rollup ride a single post-order walk (was 4 separate tree traversals).
		long fileMs = phaseSw.ElapsedMilliseconds;
		phaseSw.Restart();
		DateTime latest = PropagateSortAndPercent(rootNode, totalAllocated, token);
		long fusedMs = phaseSw.ElapsedMilliseconds;
		phaseSw.Restart();

		// WizTree parity: root (C:) shows a last-modified time. Hybrid scans get it
		// from the MFT DirMeta overlay, but folder-index fallback and enumeration
		// synthetic roots have none — propagate the latest descendant time instead
		// of leaving the cell blank. Fall back to the volume's directory time.
		ApplyLatestModified(rootNode, rootFull, latest);

		if (sw != null)
			progress?.Report(new ScanProgressReport { Phase = "Building tree", FilesScanned = fileCount, TotalFiles = items.Count, Elapsed = sw.Elapsed, Detail = $"{fileCount:N0} files aggregated — sorting..." });

		folderCount = map.Count - 1;
		if (folderCount < 0)
			folderCount = 0;

		Trace.WriteLine($"[BuildTree] folders {folderMs}ms, files {fileMs}ms (cache hits {parentCacheHits:N0}/{fileCount:N0}), fused propagate+sort+percents {fusedMs}ms");

		recordNodes = recMap.Count > 0 ? recMap : null;
		return [rootNode];
	}

	/// <summary>
	/// Drive-root scan: one direct MFT parse supplies both the file rows (true allocated sizes,
	/// dates, attributes) and the folder skeleton from its directory overlay.
	/// Single fallback: anything MFT-related that fails (denied handle, non-NTFS volume,
	/// empty record set) throws so <see cref="AnalyzeAsync"/> runs the enumeration
	/// walker — the one universal fallback. There is deliberately no Everything
	/// query in this path: on an NTFS volume the MFT parse is already the fastest
	/// source, and on other volumes the walker is the only correct one.
	/// </summary>
	private async Task<IReadOnlyList<ScanItem>> ScanDriveRootAsync(ScanOptions options, string root, IProgress<ScanProgressReport>? detailedProgress, Stopwatch sw, CancellationToken token)
	{
		void Report(string phase, string? detail = null)
			=> detailedProgress?.Report(new ScanProgressReport { Phase = phase, Elapsed = sw.Elapsed, Detail = detail });

		Report("Reading MFT file records");
		uint cluster = DiskCluster.GetClusterSize(root);
		bool clusterIsPowerOfTwo = DiskCluster.IsPowerOfTwo(cluster);

		(List<ScanItem> files, List<MftDataScanner.MftDirRow> dirs, string diagnostics) mft;
		try
		{
			mft = await Task.Run(() => MftDataScanner.ScanFiles(root, cluster, clusterIsPowerOfTwo, token), token).ConfigureAwait(false);
			Trace.WriteLine($"[MftData] {mft.diagnostics}");
		}
		catch (Exception ex) when (ex is not OperationCanceledException)
		{
			Trace.WriteLine($"MFT pass failed, falling back to enumeration: {ex.Message}");
			throw new InvalidOperationException($"MFT scan failed for {root}, falling back to enumeration.", ex);
		}

		// No file records (non-NTFS volume or fully denied handle): the walker is
		// the single fallback — it works on every filesystem.
		if (mft.files.Count == 0)
			throw new InvalidOperationException($"MFT returned no file records for {root}, falling back to enumeration.");

		// Folder rows carry the directory's own Modified/Attributes; their sizes stay 0 because
		// BuildTree propagates them from the file rows it also receives. FileId carries the
		// MFT record id so lite file rows resolve their parent node by id (no path hashing).
		var combined = new List<ScanItem>(mft.dirs.Count + mft.files.Count);
		foreach (MftDataScanner.MftDirRow dir in mft.dirs)
		{
			combined.Add(new ScanItem
			{
				FullPath = dir.Path,
				Size = 0,
				AllocatedSize = 0,
				Modified = dir.Meta.Modified,
				IsFolder = true,
				Attributes = dir.Meta.Attributes,
				FileId = dir.Record
			});
		}

		combined.AddRange(mft.files);
		return combined;
	}

	/// <summary>
	/// Per-node file deltas collected by one parallel accumulation worker, then merged
	/// once into the shared tree. Keeps all shared mutation out of the hot loop.
	/// </summary>
	private struct FileAcc
	{
		public long Size;
		public long Alloc;
		public int Files;
	}

	private sealed class FileThreadState
	{
		public readonly Dictionary<DiskNode, FileAcc> acc = new(65536);
		public readonly List<(DiskNode Parent, TreemapFile File)> tiles = new();

		public string? lastPath;
		public DiskNode? lastNode;
		public long size;
		public long alloc;
		public int files;
		public int cacheHits;
		public int n;
	}

	/// <summary>
	/// Locked folder create for the structurally-impossible missing parent (every emitted
	/// file's parent ships as a folder row, created before the parallel pass). Single-writer
	/// under the lock, so it simply mirrors the sequential create — no races by construction.
	/// </summary>
	private static DiskNode GetOrCreateFolderLocked(ReadOnlySpan<char> parentSpan,
		System.Collections.Concurrent.ConcurrentDictionary<string, DiskNode>.AlternateLookup<ReadOnlySpan<char>> lookup,
		System.Collections.Concurrent.ConcurrentDictionary<string, DiskNode> map,
		object folderLock, string rootFull)
	{
		lock (folderLock)
		{
			return GetOrCreateFolderUnderLock(parentSpan, lookup, map, rootFull);
		}
	}

	private static DiskNode GetOrCreateFolderUnderLock(ReadOnlySpan<char> path,
		System.Collections.Concurrent.ConcurrentDictionary<string, DiskNode>.AlternateLookup<ReadOnlySpan<char>> lookup,
		System.Collections.Concurrent.ConcurrentDictionary<string, DiskNode> map,
		string rootFull)
	{
		ReadOnlySpan<char> span = path;
		if (span.Length > 0 && span[^1] == '\\')
			span = span.TrimEnd('\\');
		if (lookup.TryGetValue(span, out DiskNode? existing) && existing != null)
			return existing;

		int sep = span.LastIndexOf('\\');
		string trimmed = new string(span);
		string name = sep < 0 ? trimmed : new string(span.Slice(sep + 1));
		if (name.Length == 0)
			name = trimmed;

		var node = new DiskNode { Name = name, FullPath = trimmed, IsFolder = true };
		map[trimmed] = node;
		if (!trimmed.Equals(rootFull, StringComparison.OrdinalIgnoreCase) && sep > 0)
		{
			var parentNode = GetOrCreateFolderUnderLock(span.Slice(0, sep), lookup, map, rootFull);
			parentNode.Children.Add(node);
			parentNode.FolderCount++;
		}
		return node;
	}

	/// <summary>
	/// Single post-order walk that rolls child sizes/counts up, sorts each sibling
	/// group, stamps allocated-based percents (WizTree: % of parent = Allocated /
	/// parent Allocated) and bubbles the latest modified date. Replaces four
	/// separate traversals (propagate, sort, percents, latest-date). Runs before UI
	/// bind so CollectionChanged has no grid subscribers yet. Returns the newest
	/// modified time in the subtree.
	/// </summary>
	private static DateTime PropagateSortAndPercent(DiskNode node, long totalAllocated, CancellationToken token)
	{
		token.ThrowIfCancellationRequested();
		DateTime latest = node.Modified;
		foreach (var child in node.Children)
		{
			DateTime childLatest = PropagateSortAndPercent(child, totalAllocated, token);
			if (childLatest > latest)
				latest = childLatest;
			node.Size += child.Size;
			node.Allocated += child.Allocated;
			node.FileCount += child.FileCount;
			node.FolderCount += child.FolderCount;
		}
		if (totalAllocated > 0)
			node.PercentOfTotal = (double)node.Allocated / totalAllocated * 100;
		SortNodeChildren(node);
		if (node.Children.Count > 0 && node.Allocated > 0)
		{
			foreach (var c in node.Children)
				c.PercentOfParent = (double)c.Allocated / node.Allocated * 100;
		}
		return latest;
	}

	/// <summary>
	/// Sort + percents + latest-date walk for the folder-first fast path, whose
	/// recursive sizes are already applied (no roll-up — that would double-count).
	/// </summary>
	private static DateTime SortAndPercent(DiskNode node, long totalAllocated, CancellationToken token)
	{
		token.ThrowIfCancellationRequested();
		DateTime latest = node.Modified;
		foreach (var child in node.Children)
		{
			DateTime childLatest = SortAndPercent(child, totalAllocated, token);
			if (childLatest > latest)
				latest = childLatest;
		}
		if (totalAllocated > 0)
			node.PercentOfTotal = (double)node.Allocated / totalAllocated * 100;
		SortNodeChildren(node);
		if (node.Children.Count > 0 && node.Allocated > 0)
		{
			foreach (var c in node.Children)
				c.PercentOfParent = (double)c.Allocated / node.Allocated * 100;
		}
		return latest;
	}

	private static void SortNodeChildren(DiskNode node)
	{
		if (node.Children.Count <= 1)
			return;

		// Sort through a pooled buffer: ~40k folders have siblings, and one array per folder
		// was pure GC pressure on a 500k-row scan.
		int count = node.Children.Count;
		DiskNode[] array = ArrayPool<DiskNode>.Shared.Rent(count);
		try
		{
			node.Children.CopyTo(array, 0);
			Array.Sort(array, 0, count, AllocatedDescendingComparer.Instance);
			node.Children.Clear();
			for (int index = 0; index < count; index++)
				node.Children.Add(array[index]);
		}
		finally
		{
			ArrayPool<DiskNode>.Shared.Return(array, clearArray: true);
		}
	}

	private static void ApplyLatestModified(DiskNode rootNode, string rootFull, DateTime latest)
	{
		if (rootNode.Modified != DateTime.MinValue)
			return;

		if (latest != DateTime.MinValue)
		{
			rootNode.Modified = latest;
			return;
		}

		try
		{
			DateTime volTime = Directory.GetLastWriteTimeUtc(rootFull);
			if (volTime != DateTime.MinValue)
				rootNode.Modified = volTime;
		}
		catch
		{
			// Offline or denied volume root: the newest descendant time stands.
		}
	}

	/// <summary>
	/// WizTree default order for siblings: allocated size descending, logical size as tie-breaker.
	/// </summary>
	private sealed class AllocatedDescendingComparer : IComparer<DiskNode>
	{
		public static AllocatedDescendingComparer Instance { get; } = new();

		public int Compare(DiskNode? left, DiskNode? right)
		{
			if (left == null || right == null)
				return 0;

			int compare = right.Allocated.CompareTo(left.Allocated);
			return compare != 0 ? compare : right.Size.CompareTo(left.Size);
		}
	}

	/// <summary>
	/// Duplicate candidate key (WizTree "Locate by Name, Size, Date") compared case-insensitively and
	/// on the file name only, so neither the concatenated key string nor a per-file name string has
	/// to be allocated during a drive walk. <c>Name</c> carries lite-row names; classic rows leave
	/// it null and compare the name span of <c>FullPath</c> instead — same content, same hash.
	/// </summary>
	private readonly record struct DuplicateKey(string FullPath, string? Name, long Size, long ModifiedTicks);

	private sealed class DuplicateKeyComparer : IEqualityComparer<DuplicateKey>
	{
		public static DuplicateKeyComparer Instance { get; } = new();

		public bool Equals(DuplicateKey x, DuplicateKey y)
			=> x.Size == y.Size
			&& x.ModifiedTicks == y.ModifiedTicks
			&& EffectiveName(x).Equals(EffectiveName(y), StringComparison.OrdinalIgnoreCase);

		public int GetHashCode(DuplicateKey key)
			=> HashCode.Combine(key.Size, key.ModifiedTicks, string.GetHashCode(EffectiveName(key), StringComparison.OrdinalIgnoreCase));

		private static ReadOnlySpan<char> EffectiveName(DuplicateKey key)
			=> key.Name is not null ? key.Name.AsSpan() : NameSpan(key.FullPath);

		private static ReadOnlySpan<char> NameSpan(string fullPath)
		{
			int separator = fullPath.LastIndexOf('\\');
			return separator >= 0 ? fullPath.AsSpan(separator + 1) : fullPath.AsSpan();
		}
	}

	private struct DuplicateGroup
	{
		public int Count;
		public long TotalSize;
		public long TotalAllocated;
	}

}
