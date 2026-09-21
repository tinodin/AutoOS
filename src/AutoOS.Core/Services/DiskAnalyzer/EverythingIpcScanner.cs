using System.Diagnostics;

namespace AutoOS.Core.Services.DiskAnalyzer;

/// <summary>
/// Windhawk "Explorer Folder Size Details" style scanner: bulk QUERY2 pages over WM_COPYDATA
/// against the running Everything server — no SDK DLL, no es.exe. The installer already
/// guarantees Everything itself. Unindexed/failed roots fall back to the parallel walker.
/// </summary>
public sealed class EverythingIpcScanner : IDiskScanner
{
	private const int QUERY_TIMEOUT_MS = 12000;
	// 1M/page keeps WM_COPYDATA replies under ~140MB with FULL_PATH_AND_NAME
	// (~140B/row): a single round trip for typical drives (500-800k files). The
	// Everything server serializes WM_COPYDATA queries internally, so fewer
	// round trips always dominate over individual reply size.
	private const int PAGE_SIZE = 1000000;
	private const int DEFAULT_FILE_CAPACITY = 900000;

	// Process-lifetime receiver: one hidden window plus one thread shared by all scans.
	// Creating it per scan costs ~150ms on first scan; scans serialize on this lock
	// anyway because the Everything server serializes WM_COPYDATA queries. The OS
	// reclaims the window and thread on process exit, so no Dispose plumbing is needed.
	private static readonly object _receiverLock = new();
	private static EverythingIpc.CopyDataReceiver? _sharedReceiver;

	public ScannerKind Kind => ScannerKind.Everything;

	public bool IsAvailable => EverythingIpc.IsServerRunning();

	public Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
	{
		cancellationToken.ThrowIfCancellationRequested();
		if (!IsAvailable)
			return Task.FromResult(false);

		// Warm the receiver here: window plus thread creation costs ~150ms, and the
		// probe already runs before the first scan (ViewModel probes on construct),
		// so the timed scan reuses a warm receiver. Opportunistic: never block
		// behind a running scan's query loop.
		if (Monitor.TryEnter(_receiverLock))
		{
			try
			{
				EnsureSharedReceiverLocked();
			}
			finally
			{
				Monitor.Exit(_receiverLock);
			}
		}
		return Task.FromResult(true);
	}

	public Task<IReadOnlyList<ScanItem>> ScanAsync(ScanOptions options, IProgress<double>? progress = null, CancellationToken cancellationToken = default)
	{
		CancellationToken token = cancellationToken;

		return Task.Run(() =>
		{
			token.ThrowIfCancellationRequested();
			string root = DiskCluster.NormalizeRoot(options.RootPath);
			var sw = Stopwatch.StartNew();
			var querySw = new Stopwatch();

			// Direct parse: rows append straight into items on the receiver thread —
			// no intermediate per-row list, no convert pass.
			bool includeDates = options.IncludeModifiedDates;
			uint cluster = DiskCluster.GetClusterSize(root);
			uint flags = EverythingIpc.QUERY2_REQUEST_FULL_PATH_AND_NAME |
				EverythingIpc.QUERY2_REQUEST_SIZE;
			if (includeDates)
				flags |= EverythingIpc.QUERY2_REQUEST_DATE_MODIFIED;

			bool hasFilter = !string.IsNullOrWhiteSpace(options.SearchFilter);
			string search = hasFilter
				? $"\"{root}\" {options.SearchFilter}"
				: $"\"{root}\"";

			// Drive-root scans skip the per-row StartsWith: the quoted "C:\" search
			// already constrains server-side, so a 3-char drive check suffices.
			// Subfolder roots and filtered queries keep the full prefix guard — a
			// quoted substring can match file names on other drives.
			bool isDriveRoot = root.Length == 3 && root[1] == ':' && root[2] == '\\';

			int offset = 0;
			int pages = 0;
			InvalidOperationException? replyError = null;
			if (!EverythingIpc.TryFindServer(out var server))
				throw new InvalidOperationException($"Everything window not found for {root}.");

			int initialCapacity = isDriveRoot && !hasFilter ? DEFAULT_FILE_CAPACITY : 8192;
			var items = new List<ScanItem>(initialCapacity);
			var rowOptions = new EverythingIpc.ScanRowOptions(cluster, DiskCluster.IsPowerOfTwo(cluster), includeDates, root, isDriveRoot && !hasFilter, token);
			// Serialized: reply-target state lives on the shared receiver, and
			// the Everything server serializes WM_COPYDATA queries anyway.
			lock (_receiverLock)
			{
				var receiver = EnsureSharedReceiverLocked();
				try
				{
					// Sequential pages: measured that concurrent receivers (PAR=2/4) give no
					// speedup — the Everything server serializes WM_COPYDATA queries.
					while (true)
					{
						token.ThrowIfCancellationRequested();
						// Parse appends rows straight into items on the receiver thread:
						// no intermediate per-row list, no convert pass.
						int received;
						querySw.Start();
						try
						{
							received = EverythingIpc.Query(receiver, server, search, flags, PAGE_SIZE, offset, items, rowOptions, QUERY_TIMEOUT_MS);
						}
						catch (InvalidOperationException ex) when (ex.InnerException is not OperationCanceledException && ex.InnerException != null)
						{
							// Reply-validation failure: keep the parse error, fail over to the walker.
							// (Cancellation surfaces with an OperationCanceledException inner.)
							replyError = ex;
							break;
						}
						finally
						{
							querySw.Stop();
						}

						if (received == 0)
							break;

						pages++;
						offset += received;
						progress?.Report(Math.Min(0.99d, (double)offset / Math.Max(offset + 1, PAGE_SIZE)));
						if (received < PAGE_SIZE)
							break;
					}
				}
				catch (InvalidOperationException ex) when (ex.InnerException == null)
				{
					// Window/timeout level: surface as scan failure so the service fails over.
					throw new InvalidOperationException($"Everything IPC query failed for {root}.", ex);
				}
			}

			// Partial success wins over a late reply-validation failure: return what we have
			// so the tree still renders.
			if (replyError != null && items.Count == 0)
				throw new InvalidOperationException($"Everything IPC query failed for {root}.", replyError);

			if (items.Count == 0)
				throw new InvalidOperationException($"Everything returned no results for {root}.");

			Trace.WriteLine($"[EverythingIpc] {items.Count:N0} items, {pages} pages in {sw.Elapsed.TotalSeconds:F2}s (query+parse {querySw.Elapsed.TotalSeconds:F2}s)");
			progress?.Report(1.0d);
			return (IReadOnlyList<ScanItem>)items;
		}, token);
	}

	private static EverythingIpc.CopyDataReceiver EnsureSharedReceiverLocked()
	{
		_sharedReceiver ??= new EverythingIpc.CopyDataReceiver();
		return _sharedReceiver;
	}
}
