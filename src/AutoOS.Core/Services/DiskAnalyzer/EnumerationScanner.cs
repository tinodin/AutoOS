using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO.Enumeration;

namespace AutoOS.Core.Services.DiskAnalyzer;

/// <summary>
/// The single universal fallback: parallel bulk enumeration.
/// Depth-2 shards enumerate in parallel. Single <c>FileSystemEnumerable</c> pass per
/// directory builds records straight from Find data: no FileInfo/DirectoryInfo per
/// row, no extra syscalls per file. Timestamps stay UTC. Work shards at depth 2 so one
/// huge subtree (Windows, Program Files) cannot pin a single thread while others idle.
/// Metadata-I/O-bound, not CPU. Works on every filesystem (unlike the MFT parse).
/// </summary>
public sealed class EnumerationScanner : IDiskScanner
{
	// Parallelism: metadata enumeration scales to full core count on NVMe SSD (no cap).

	public ScannerKind Kind => ScannerKind.Enumeration;
	public bool IsAvailable => true; // always available; this is the single fallback

	private static readonly EnumerationOptions SharedEnumerationOptions = new()
	{
		RecurseSubdirectories = false,
		IgnoreInaccessible = true,
		AttributesToSkip = 0
	};

	public Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
	{
		return Task.FromResult(true);
	}

	public Task<IReadOnlyList<ScanItem>> ScanAsync(ScanOptions options, IProgress<double>? progress = null, CancellationToken cancellationToken = default)
	{
		CancellationToken token = cancellationToken;

		return Task.Run(() =>
		{
			token.ThrowIfCancellationRequested();
			string root = options.RootPath;
			if (!Directory.Exists(root))
				throw new DirectoryNotFoundException(root);

			return ScanParallel(root, options.IncludeModifiedDates, token, progress);
		}, token);
	}

	private static IReadOnlyList<ScanItem> ScanParallel(string root, bool includeDates, CancellationToken token, IProgress<double>? progress)
	{
		uint cluster = DiskCluster.GetClusterSize(root);
		bool clusterIsPowerOfTwo = DiskCluster.IsPowerOfTwo(cluster);
		string rootTrim = DiskCluster.NormalizeRoot(root);
		var sw = Stopwatch.StartNew();

		// Phase 1: shallow root listing to collect level-1 directories.
		var level1Dirs = new List<string>();
		var topLevel = new List<ScanItem>(4096);
		try
		{
			foreach (var item in EnumerateScanItems(rootTrim, cluster, clusterIsPowerOfTwo, includeDates))
			{
				token.ThrowIfCancellationRequested();
				if (item.IsFolder)
					level1Dirs.Add(item.FullPath);
				topLevel.Add(item);
			}
		}
		catch (UnauthorizedAccessException)
		{
		}

		// Phase 1b (parallel): shallow listing of each level-1 directory. Direct children
		// are recorded once here; level-2 directories become the parallel work roots.
		// Parallel: level-1 fan-out was serial (~0.5-1s on C:\) before workers started.
		var workRootsBag = new ConcurrentBag<string>();
		var phase1bLocals = new ConcurrentBag<List<ScanItem>>();
		Parallel.ForEach(level1Dirs, new ParallelOptions { CancellationToken = token, MaxDegreeOfParallelism = Math.Max(4, Environment.ProcessorCount) }, level1 =>
		{
			var local = new List<ScanItem>(4096);
			try
			{
				foreach (var item in EnumerateScanItems(level1, cluster, clusterIsPowerOfTwo, includeDates))
				{
					token.ThrowIfCancellationRequested();
					if (item.IsFolder)
						workRootsBag.Add(item.FullPath);
					local.Add(item);
				}
			}
			catch (Exception ex) when (ex is UnauthorizedAccessException || ex is DirectoryNotFoundException || ex is IOException)
			{
				Trace.WriteLine($"Scan skipped {level1}: {ex.Message}");
			}
			if (local.Count > 0)
				phase1bLocals.Add(local);
		});
		foreach (var local in phase1bLocals)
			topLevel.AddRange(local);
		var workRoots = new List<string>(workRootsBag.Count + 64);
		workRoots.AddRange(workRootsBag);
		long phase1bMs = sw.ElapsedMilliseconds;

		if (workRoots.Count == 0)
		{
			// Flat hierarchy (or capped/denied): phase 1 + 1b already collected everything.
			Trace.WriteLine($"[Enum] {topLevel.Count:N0} items, no subtrees in {sw.Elapsed.TotalSeconds:F2}s");
			return topLevel;
		}

		// Phase 2: parallel subtree enumeration.
		// Progress throttle ticks live on a shared holder: Parallel.ForEach lambdas
		// cannot capture ref parameters (CS1628).
		var progressState = new ScanProgressState();

		// One task per shard: each thread walks a contiguous subtree on a
		// private stack with zero synchronization during the walk. A shared
		// work-stealing stack measured slower on NVMe — contention cost more
		// than the imbalance it fixed; depth-2 shards fix the imbalance instead.
		var parallelOptions = new ParallelOptions
		{
			CancellationToken = token,
			// Oversubscribed 4x: enumeration blocks on metadata I/O, so extra
			// workers hide syscall latency on NVMe (CPU is not the bottleneck).
			MaxDegreeOfParallelism = Math.Max(16, Environment.ProcessorCount * 4)
		};

		int localCapacity = workRoots.Count > 64 ? 4096 : 16384;
		// P1: indexed slots instead of ConcurrentBag + Interlocked per shard. One
		// thread owns each slot, so no contention during the walk, and the merge
		// is a single ordered pass (was: bag enumeration + atomic adds).
		var slots = new List<ScanItem>?[workRoots.Count];
		Parallel.For(0, workRoots.Count, parallelOptions, i =>
		{
			var local = new List<ScanItem>(localCapacity);
			ScanSubtree(workRoots[i], cluster, clusterIsPowerOfTwo, includeDates, local, token, progress, progressState);
			slots[i] = local;
		});

		long totalCount = topLevel.Count;
		for (int i = 0; i < slots.Length; i++)
			totalCount += slots[i]?.Count ?? 0;

		var merged = new List<ScanItem>((int)Math.Min(totalCount, int.MaxValue));
		merged.AddRange(topLevel);
		for (int i = 0; i < slots.Length; i++)
			if (slots[i] is { Count: > 0 } slot)
				merged.AddRange(slot);

		Trace.WriteLine($"[Enum] {merged.Count:N0} items, {workRoots.Count} shards in {sw.Elapsed.TotalSeconds:F2}s (phase1b {phase1bMs}ms, dop {parallelOptions.MaxDegreeOfParallelism})");
		return merged;
	}

	/// <summary>
	/// Single-directory enumeration that builds <see cref="ScanItem"/> records straight
	/// from the Find-data entry — no <c>FileInfo</c>/<c>DirectoryInfo</c> object per row
	/// (654k allocations saved on C:\). Reparse-point directories are excluded via
	/// predicate so junctions/symlinks never recurse; reparse-point files still count.
	/// Timestamps are skipped entirely when <paramref name="includeDates"/> is false.
	/// </summary>
	private static IEnumerable<ScanItem> EnumerateScanItems(string directory, uint cluster, bool clusterIsPowerOfTwo, bool includeDates)
	{
		var enumerable = new FileSystemEnumerable<ScanItem>(
			directory,
			(ref FileSystemEntry entry) =>
			{
				string fullPath = entry.ToFullPath();
				bool isDirectory = entry.IsDirectory;
				long size = isDirectory ? 0 : entry.Length;
				return new ScanItem
				{
					FullPath = fullPath,
					Size = size,
					AllocatedSize = isDirectory ? 0 : DiskCluster.AlignSize(size, cluster, clusterIsPowerOfTwo),
					Modified = includeDates ? entry.LastWriteTimeUtc.UtcDateTime : DateTime.MinValue,
					IsFolder = isDirectory,
					Attributes = DiskNode.FormatAttributes(entry.Attributes),
					Extension = isDirectory ? string.Empty : Path.GetExtension(fullPath)
				};
			},
			SharedEnumerationOptions)
		{
			ShouldIncludePredicate = static (ref FileSystemEntry entry) =>
				entry.FileName.Length != 0 &&
				!(entry.IsDirectory && (entry.Attributes & FileAttributes.ReparsePoint) != 0)
		};
		return enumerable;
	}
	private static void ScanSubtree(string subRoot, uint cluster, bool clusterIsPowerOfTwo, bool includeDates, List<ScanItem> local, CancellationToken token, IProgress<double>? progress, ScanProgressState progressState)
	{
		var dirs = new Stack<string>();
		dirs.Push(subRoot);
		long localFiles = 0;

		while (dirs.Count > 0)
		{
			token.ThrowIfCancellationRequested();
			string dir = dirs.Pop();

			IEnumerable<ScanItem> entries;
			try
			{
				entries = EnumerateScanItems(dir, cluster, clusterIsPowerOfTwo, includeDates);
			}
			catch (Exception ex) when (ex is UnauthorizedAccessException || ex is DirectoryNotFoundException || ex is IOException)
			{
				Trace.WriteLine($"Scan skipped {dir}: {ex.Message}");
				continue;
			}

			foreach (var item in entries)
			{
				token.ThrowIfCancellationRequested();
				if (item.IsFolder)
					dirs.Push(item.FullPath);
				local.Add(item);

				if (!item.IsFolder && (++localFiles & 0x3FFF) == 0)
				{
					long now = Environment.TickCount64;
					if (Interlocked.Read(ref progressState.lastReportTicks) is long last && now - last >= 250)
					{
						Interlocked.Exchange(ref progressState.lastReportTicks, now);
						progress?.Report(-1);
					}
				}
			}
		}
	}

	private sealed class ScanProgressState
	{
		public long lastReportTicks = Environment.TickCount64;
	}
}
