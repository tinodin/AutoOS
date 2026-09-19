using System.Buffers;
using System.Diagnostics;

namespace AutoOS.Core.Services.DiskAnalyzer;

public sealed class DiskAnalyzerService
{
	private static readonly TimeSpan AvailabilityCacheTtl = TimeSpan.FromSeconds(30);

	private const int TOP_FILES_KEEP = 20000;
	private const string NO_EXTENSION_KEY = "(No Extension)";

	private readonly EverythingIpcScanner _everythingIpc = new();
	private readonly MftScanner _mftScanner = new();

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
				// lands on IPC instead of racing to the MFT fallback.
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
					Report("Everything not available — falling back to MFT/fast enumeration", detail: EverythingProbeDetails);
					fallback = true;
					used = ScannerKind.Enumeration;
					items = await _mftScanner.ScanAsync(options, progress, token).ConfigureAwait(false);
				}
			}
			else
			{
				Report("Enumerating filesystem (WizTree-like fast fallback)");
				items = await _mftScanner.ScanAsync(options, progress, token).ConfigureAwait(false);
				used = ScannerKind.Enumeration;
			}
		}
		catch (Exception ex) when (options.PreferredScanner == ScannerKind.Everything && ex is not OperationCanceledException)
		{
			Trace.WriteLine($"Everything scan failed, falling back: {ex.Message}");
			Report("Everything failed — fallback fast enumeration", detail: ex.Message);
			fallback = true;
			used = ScannerKind.Enumeration;
			items = await _mftScanner.ScanAsync(options, progress, token).ConfigureAwait(false);
		}

		// IPC + MFT align allocated sizes inline, so no fixup pass is needed.
		Report("Building tree", scanned: 0, total: items.Count, detail: $"~{items.Count:N0} items — offloading to background thread");

		var scanSw = Stopwatch.StartNew();

		// Single background pass: accumulate folder sizes (was two full passes).
		var buildResult = await Task.Run(() =>
		{
			// The File View aggregation is a second full pass over the scan rows, but it shares no
			// state with the tree build, so both run at once and the File View data is ready the
			// moment the scan finishes instead of on the first File-tab open.
			Task<FileViewInputs> fileViewTask = Task.Run(() => BuildFileViewInputs(items, token), token);

			var roots = BuildTree(items, options.RootPath, out long totalSize, out long totalAllocated, out int fileCount, out int folderCount, detailedProgress, sw, token);

			// Percents are allocated-based like WizTree: % of parent = Allocated / parent Allocated.
			foreach (var r in roots) ComputePercents(r, totalAllocated);

			// The aggregation cannot know the drive total on its own, so percents land here.
			FileViewInputs fileView = fileViewTask.GetAwaiter().GetResult();
			ApplyTotalAllocated(fileView, totalAllocated);

			return (roots, totalSize, totalAllocated, fileCount, folderCount, fileView);
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
			FileView = buildResult.fileView
		};
	}

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
		var duplicateGroups = new Dictionary<DuplicateKey, DuplicateGroup>(items.Count / 4, DuplicateKeyComparer.Instance);
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
			string fullPath = item.FullPath;
			int separator = fullPath.LastIndexOf('\\');
			ReadOnlySpan<char> nameSpan = separator >= 0 ? fullPath.AsSpan(separator + 1) : fullPath.AsSpan();
			ReadOnlySpan<char> extensionSpan = Path.GetExtension(nameSpan);

			// The key holds the already-allocated path and compares only its name part, so a drive
			// walk allocates nothing per file here (was one name string per file).
			DuplicateKey duplicateKey = new(fullPath, item.Size, item.Modified.Ticks);
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
				topFiles.Enqueue(CreateFileRow(item, fullPath, separator), (item.AllocatedSize, item.Size));
			}
			else if (topFiles.TryPeek(out _, out (long Allocated, long Size) smallest) && (item.AllocatedSize, item.Size).CompareTo(smallest) > 0)
			{
				topFiles.Dequeue();
				topFiles.Enqueue(CreateFileRow(item, fullPath, separator), (item.AllocatedSize, item.Size));
			}
		}

		if (!hasFile)
			return FileViewInputs.Empty; // folder-index scan: File View and extension panel stay empty

		var rows = new List<DiskFileRow>(topFiles.Count);
		foreach ((DiskFileRow row, _) in topFiles.UnorderedItems)
		{
			// Duplicate info is only resolved for the rows that actually get listed.
			if (duplicateGroups.TryGetValue(new DuplicateKey(row.FullPath, row.Size, row.Modified.Ticks), out DuplicateGroup group) && group.Count > 1)
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
	/// total, and the aggregation runs before it, so percents are applied as a cheap fix-up pass.
	/// </summary>
	public static void ApplyTotalAllocated(FileViewInputs inputs, long totalAllocated)
	{
		foreach (DiskFileRow row in inputs.TopFiles)
			row.PercentOfDrive = totalAllocated > 0 ? (double)row.Allocated / totalAllocated * 100 : 0;

		foreach (DiskExtensionStat extension in inputs.Extensions)
			extension.Percent = totalAllocated > 0 ? (double)extension.Allocated / totalAllocated * 100 : 0;
	}

	private static DiskFileRow CreateFileRow(ScanItem item, string fullPath, int separator)
	{
		ReadOnlySpan<char> nameSpan = separator >= 0 ? fullPath.AsSpan(separator + 1) : fullPath.AsSpan();

		return new DiskFileRow
		{
			FileName = new string(nameSpan),
			Directory = separator > 0 ? fullPath.Substring(0, separator) : fullPath,
			FullPath = fullPath,
			Extension = new string(Path.GetExtension(nameSpan)),
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

	private static IReadOnlyList<DiskNode> BuildTree(IReadOnlyList<ScanItem> items, string rootPath, out long totalSize, out long totalAllocated, out int fileCount, out int folderCount, IProgress<ScanProgressReport>? progress = null, Stopwatch? sw = null, CancellationToken token = default)
	{
		totalSize = 0; totalAllocated = 0; fileCount = 0; folderCount = 0;
		string rootFull = Path.GetFullPath(rootPath).TrimEnd('\\');
		var map = new Dictionary<string, DiskNode>(items.Count / 4, StringComparer.OrdinalIgnoreCase);
		// Span lookup avoids allocating a parent-path key per level: folder creation keeps
		// string keys, but every hit (the common case — IPC rows arrive unordered, so the
		// consecutive-parent cache misses) resolves straight from the FullPath span.
		var lookup = map.GetAlternateLookup<ReadOnlySpan<char>>();
		long lastProgressTicks = sw?.ElapsedTicks ?? 0;
		const long progressIntervalTicks = TimeSpan.TicksPerSecond / 4; // 250ms throttle

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
				parentNode.FolderCount++; // direct child folders; PropagateSizes rolls up to recursive
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
		// applying it plus file accumulation plus PropagateSizes would triple-count.
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
		// applied above, so skip file accumulate + PropagateSizes (would double-count).
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
			SortChildren(rootNode, token);
			Trace.WriteLine($"[BuildTree] folder-first {folderRows:N0} folders ({sizedFolderRows:N0} sized), sort {phaseSw.ElapsedMilliseconds}ms (no file pass, no propagate)");
			return [rootNode];
		}
		long folderMs = phaseSw.ElapsedMilliseconds;
		phaseSw.Restart();
		int parentCacheHits = 0;

		// Add files — folder-only tree for perf (only folder nodes are bound to the TreeGrid).
		// Accumulate per-direct-folder then propagate bottom-up (no per-file ancestor walk O(n*depth)).
		// Consecutive files in the same directory reuse the parent node: span-compare first so
		// hits skip both the parent-path alloc and the dictionary lookup (common: scans are
		// directory-grouped, so hit rate is high).
		// FRN-deduping keeps the size sums consistent with the grid.
		int processed = 0;
		string? lastParentPath = null;
		DiskNode? lastParentNode = null;
		var seenFileIds = new HashSet<long>();
		foreach (var it in items)
		{
			token.ThrowIfCancellationRequested();
			if (it.IsFolder)
				continue;

		ReadOnlySpan<char> full = it.FullPath.AsSpan();
		int sep = full.LastIndexOf('\\');
		ReadOnlySpan<char> parentSpan = sep < 0 ? rootFull.AsSpan() : full.Slice(0, sep);
			DiskNode parent;
			if (lastParentNode != null && lastParentPath != null && parentSpan.Equals(lastParentPath.AsSpan(), StringComparison.Ordinal))
			{
				parent = lastParentNode;
				parentCacheHits++;
			}
			else
			{
				parent = GetOrCreateFolder(parentSpan);
				lastParentPath = parent.FullPath;
				lastParentNode = parent;
			}
		parent.FileCount++;
		fileCount++;
		if (it.FileId <= 0 || seenFileIds.Add(it.FileId))
		{
			parent.Size += it.Size;
			parent.Allocated += it.AllocatedSize;
			totalSize += it.Size;
			totalAllocated += it.AllocatedSize;
		}

		if (++processed % 50000 == 0 && sw != null && sw.ElapsedTicks - lastProgressTicks >= progressIntervalTicks)
			{
				lastProgressTicks = sw.ElapsedTicks;
				progress?.Report(new ScanProgressReport { Phase = "Building tree", FilesScanned = processed, TotalFiles = items.Count, Elapsed = sw.Elapsed, Detail = $"{processed:N0}/{items.Count:N0} files — accumulating" });
			}
		}

	// Bottom-up propagation via Children links (no GetDirectoryName per folder, no path-length sort).
		// Replaces per-file while(cur) walk that did 3M dict lookups for 500k files.
		long fileMs = phaseSw.ElapsedMilliseconds;
		phaseSw.Restart();
		PropagateSizes(rootNode, token);
		long propagateMs = phaseSw.ElapsedMilliseconds;
		phaseSw.Restart();

		// WizTree parity: root (C:) shows a last-modified time. Hybrid scans get it
		// from the MFT DirMeta overlay, but folder-index fallback and enumeration
		// synthetic roots have none — propagate the latest descendant time instead
		// of leaving the cell blank. Fall back to the volume's directory time.
		if (rootNode.Modified == DateTime.MinValue)
		{
			DateTime latest = FindLatestModified(rootNode);
			if (latest != DateTime.MinValue)
				rootNode.Modified = latest;
			else
			{
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
		}

		if (sw != null)
			progress?.Report(new ScanProgressReport { Phase = "Building tree", FilesScanned = fileCount, TotalFiles = items.Count, Elapsed = sw.Elapsed, Detail = $"{fileCount:N0} files aggregated — sorting..." });

		folderCount = map.Count - 1;
		if (folderCount < 0)
			folderCount = 0;

		// Single recursive sort from root — was foreach(map.Values) SortChildren(n) O(n^2).
		// Runs before UI bind so CollectionChanged has no grid subscribers yet.
		SortChildren(rootNode, token);
		Trace.WriteLine($"[BuildTree] folders {folderMs}ms, files {fileMs}ms (cache hits {parentCacheHits:N0}/{fileCount:N0}), propagate {propagateMs}ms, sort {phaseSw.ElapsedMilliseconds}ms");

		return [rootNode];
	}

	/// <summary>
	/// Drive-root scan: one direct MFT parse supplies both the file rows (true allocated sizes,
	/// dates, attributes) and the folder skeleton from its directory overlay, so no Everything
	/// query runs on the common path. The Everything folder index is only queried when the MFT
	/// pass yields no file records, and the full Everything query backs both failures.
	/// </summary>
	private async Task<IReadOnlyList<ScanItem>> ScanDriveRootAsync(ScanOptions options, string root, IProgress<ScanProgressReport>? detailedProgress, Stopwatch sw, CancellationToken token)
	{
		void Report(string phase, string? detail = null)
			=> detailedProgress?.Report(new ScanProgressReport { Phase = phase, Elapsed = sw.Elapsed, Detail = detail });

		Report("Reading MFT file records");
		uint cluster = DiskCluster.GetClusterSize(root);
		bool clusterIsPowerOfTwo = DiskCluster.IsPowerOfTwo(cluster);

		(List<ScanItem> files, Dictionary<string, MftDataScanner.DirMeta> dirs, string diagnostics) mft;
		try
		{
			mft = await Task.Run(() => MftDataScanner.ScanFiles(root, cluster, clusterIsPowerOfTwo, token), token).ConfigureAwait(false);
			Trace.WriteLine($"[MftData] {mft.diagnostics}");
		}
		catch (Exception ex) when (ex is not OperationCanceledException)
		{
			Trace.WriteLine($"MFT pass failed, full Everything query: {ex}");
			return await _everythingIpc.ScanAsync(options, null, token).ConfigureAwait(false);
		}

		// No file records (non-NTFS volume, denied handle): the Everything folder index is all we have.
		if (mft.files.Count == 0)
		{
			try
			{
				return await _everythingIpc.ScanFoldersAsync(options, null, token).ConfigureAwait(false);
			}
			catch (Exception ex) when (ex is not OperationCanceledException)
			{
				Trace.WriteLine($"Folder index unavailable, full Everything query: {ex.Message}");
				return await _everythingIpc.ScanAsync(options, null, token).ConfigureAwait(false);
			}
		}

		// Folder rows carry the directory's own Modified/Attributes; their sizes stay 0 because
		// BuildTree propagates them from the file rows it also receives.
		var combined = new List<ScanItem>(mft.dirs.Count + mft.files.Count);
		foreach ((string folderPath, MftDataScanner.DirMeta meta) in mft.dirs)
		{
			combined.Add(new ScanItem
			{
				FullPath = folderPath,
				Size = 0,
				AllocatedSize = 0,
				Modified = meta.Modified,
				IsFolder = true,
				Attributes = meta.Attributes
			});
		}

		combined.AddRange(mft.files);
		return combined;
	}

	private static void PropagateSizes(DiskNode node, CancellationToken token)
	{
		token.ThrowIfCancellationRequested();
		foreach (var child in node.Children)
		{
			PropagateSizes(child, token);
			node.Size += child.Size;
			node.Allocated += child.Allocated;
			node.FileCount += child.FileCount;
			node.FolderCount += child.FolderCount;
		}
	}

	private static void SortChildren(DiskNode node, CancellationToken token = default)
	{
		token.ThrowIfCancellationRequested();
		if (node.Children.Count <= 1)
		{
			foreach (DiskNode child in node.Children)
				if (child.IsFolder)
					SortChildren(child, token);

			return;
		}

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
			{
				DiskNode child = array[index];
				node.Children.Add(child);
				if (child.IsFolder)
					SortChildren(child, token);
			}
		}
		finally
		{
			ArrayPool<DiskNode>.Shared.Return(array, clearArray: true);
		}
	}

	private static DateTime FindLatestModified(DiskNode node)
	{
		DateTime best = node.Modified;
		foreach (var child in node.Children)
		{
			DateTime childBest = FindLatestModified(child);
			if (childBest > best)
				best = childBest;
		}
		return best;
	}

	private static void ComputePercents(DiskNode node, long totalAllocated)
	{
		// WizTree percents are allocated-based: % of parent = Allocated / parent Allocated.
		if (totalAllocated > 0)
			node.PercentOfTotal = (double)node.Allocated / totalAllocated * 100;

		if (node.Children.Count > 0 && node.Allocated > 0)
		{
			foreach (var c in node.Children)
			{
				c.PercentOfParent = (double)c.Allocated / node.Allocated * 100;
				ComputePercents(c, totalAllocated);
			}
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
	/// to be allocated during a drive walk.
	/// </summary>
	private readonly record struct DuplicateKey(string FullPath, long Size, long ModifiedTicks);

	private sealed class DuplicateKeyComparer : IEqualityComparer<DuplicateKey>
	{
		public static DuplicateKeyComparer Instance { get; } = new();

		public bool Equals(DuplicateKey x, DuplicateKey y)
			=> x.Size == y.Size
			&& x.ModifiedTicks == y.ModifiedTicks
			&& NameSpan(x.FullPath).Equals(NameSpan(y.FullPath), StringComparison.OrdinalIgnoreCase);

		public int GetHashCode(DuplicateKey key)
			=> HashCode.Combine(key.Size, key.ModifiedTicks, string.GetHashCode(NameSpan(key.FullPath), StringComparison.OrdinalIgnoreCase));

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
