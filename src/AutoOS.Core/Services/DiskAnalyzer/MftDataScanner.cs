using System.Buffers;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Text;
using Microsoft.Win32.SafeHandles;
using Windows.Win32;

namespace AutoOS.Core.Services.DiskAnalyzer;

/// <summary>
/// WizTree-style true on-disk size source: parses the NTFS MFT directly and returns
/// per-file logical size, real allocated bytes (compression/sparse aware), modified
/// dates and attributes, plus a directory overlay. CsWin32 does not generate
/// <c>NTFS_VOLUME_DATA_BUFFER</c>, so the 96-byte ioctl payload is parsed explicitly
/// below; volume open/read use <see cref="File.OpenHandle"/> and <see cref="FileStream"/>.
/// Requires elevation for the volume handle; the app manifest already requires administrator.
/// </summary>
public static class MftDataScanner
{
	private const uint FSCTL_GET_NTFS_VOLUME_DATA = 0x90064;
	private const int VOLUME_DATA_LENGTH = 96;
	// 32MB chunks: the Phase-A wall is the scattered volume read (~400ms for 700MB
	// across 8 workers), not parsing (~50ms). Fewer, longer runs per worker keep each
	// stream sequential across MFT fragments; parse stays overlapped on all cores.
	private const int CHUNK_BYTES = 1 << 25; // 32MB sequential reads
	private const uint FILE_RECORD_MAGIC = 0x454C4946; // "FILE"
	private const int ROOT_RECORD_NUMBER = 5;

	/// <summary>
	/// Directory modified date + WizTree-style attributes overlay, keyed by full path.
	/// </summary>
	public readonly record struct DirMeta(DateTime Modified, string Attributes);

	/// <summary>
	/// One resolved directory: its MFT record id (folder rows carry it as
	/// <see cref="ScanItem.FileId"/> so lite file rows can resolve parents by id),
	/// its full path and its overlay metadata.
	/// </summary>
	public readonly record struct MftDirRow(int Record, string Path, DirMeta Meta);

	/// <summary>
	/// Optional debug filter: when set, matching $FILE_NAMEs trace their record.
	/// </summary>
	public static string? DebugNameContains { get; set; }

	public static (List<ScanItem> Files, List<MftDirRow> Dirs, string Diagnostics) ScanFiles(string root, uint cluster, bool clusterIsPowerOfTwo, CancellationToken token)
	{
		string? driveRoot = Path.GetPathRoot(root);
		if (string.IsNullOrEmpty(driveRoot))
			throw new DirectoryNotFoundException(root);

		string drive = driveRoot.TrimEnd('\\');
		var sw = Stopwatch.StartNew();
		using SafeFileHandle volume = File.OpenHandle($@"\\.\{drive}", FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
		(long mftOffset, int recordSize, long mftLength, int bytesPerCluster) = GetLayout(volume, drive);
		Trace.WriteLine($"[MftData] {drive} mftOffset={mftOffset:N0} recordSize={recordSize} mftLength={mftLength:N0}");

		long recordCount = mftLength / recordSize;
		if (recordCount <= 0 || recordCount > int.MaxValue)
			throw new InvalidOperationException($"Implausible MFT size for {drive}.");

		int count = (int)recordCount;
		var parent = new long[count];
		Array.Fill(parent, -1L);
		var names = new string?[count];
		var recordFlags = new byte[count];
		var siAttrs = new uint[count];
		var siModified = new long[count];
		var dataAlloc = new long[count];
		var dataReal = new long[count];
		var fileNameReal = new long[count];
		var hasData = new bool[count];
		var dataResident = new bool[count];
		var dataExtRef = new long[count];
		Array.Fill(dataExtRef, -1L);
		var extraNames = new Dictionary<int, List<(long Parent, string Name)>>();

		using var stream = new FileStream(volume, FileAccess.Read, 1 << 20, false);
		long skippedMagic = 0;
		long skippedTorn = 0;
		long skippedNotInUse = 0;
		long skippedNoName = 0;
		long namelessLive = 0;
		long parsedFiles = 0;
		long parsedNoData = 0;
		long emittedExtras = 0;
		long extDataUsed = 0;
		long readTicksTotal = 0;
		long parseTicksTotal = 0;
		var walkSamples = new List<string>(5);

		// The MFT is usually fragmented: follow $MFT::$DATA runs instead of reading linearly.
		List<(long Lcn, long Clusters)>? runs = ReadDataRuns(stream, mftOffset, recordSize);
		if (runs == null || runs.Count == 0)
		{
			// Fallback: linear read covers the first fragment at least.
			runs = [(mftOffset / Math.Max(1, bytesPerCluster), mftLength / Math.Max(1, bytesPerCluster) + 1)];
		}

		Trace.WriteLine($"[MftData] {runs.Count} runs: {string.Join("; ", runs.Take(20).Select(r => $"{r.Lcn}/{r.Clusters}"))}");

		// Fixed-size chunk plan so parallel workers read disjoint ranges.
		var jobs = new List<(long Phys, long First, int Count)>();
		{
			long cursor = 0;
			foreach (var (lcn, clusters) in runs)
			{
				if (cursor >= count)
					break;

				long runRecords = clusters * bytesPerCluster / recordSize;
				long take = Math.Min(runRecords, count - cursor);
				if (lcn >= 0 && take > 0)
				{
					long phys = lcn * bytesPerCluster;
					long first = cursor;
					long remaining = take;
					while (remaining > 0)
					{
						int piece = (int)Math.Min(remaining, CHUNK_BYTES / recordSize);
						jobs.Add((phys + (take - remaining) * recordSize, first + (take - remaining), piece));
						remaining -= piece;
					}
				}
				cursor += take;
			}
		}

		// Parallel parse: records land in disjoint array slots; per-thread extras and
		// counters merge at the end. Each worker opens ONE volume handle for its
		// lifetime (was: one open per job) and rents smaller 8MB chunk buffers.
		var mergeLock = new object();
		var allExtras = new Dictionary<int, List<(long Parent, string Name)>>();
		var allWalks = new List<string>(5);
		Parallel.ForEach(jobs,
			// Two fat streams: the 700MB scattered read is the whole Phase-A wall and the bench
			// showed 4 streams doubling read time here (seek contention wins over parallelism).
			// Parse stays fully overlapped either way.
			new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, Math.Min(2, Environment.ProcessorCount)), CancellationToken = token },
			() =>
			{
				var local = new MftThreadState();
				// SequentialScan: the 700MB MFT read is the parse-phase bottleneck (~470ms
				// wall); the readahead hint on the handle keeps it streaming across fragments.
				local.handle = File.OpenHandle($@"\\.\{drive}", FileMode.Open, FileAccess.Read, FileShare.ReadWrite, FileOptions.SequentialScan);
				local.stream = new FileStream(local.handle, FileAccess.Read, 1 << 20, false);
				return local;
			},
			(job, _, local) =>
			{
				byte[] chunk = ArrayPool<byte>.Shared.Rent(job.Count * recordSize);
				try
				{
					long r0 = Stopwatch.GetTimestamp();
					local.stream!.Position = job.Phys;
					local.stream.ReadExactly(chunk, 0, job.Count * recordSize);
					local.readTicks += Stopwatch.GetTimestamp() - r0;
					long p0 = Stopwatch.GetTimestamp();
					for (int i = 0; i < job.Count; i++)
					{
						long recNo = job.First + i;
						ParseRecord(new Span<byte>(chunk, i * recordSize, recordSize), (int)recNo, recordSize,
							parent, names, recordFlags, siAttrs, siModified, dataAlloc, dataReal, fileNameReal, hasData, dataResident, dataExtRef, local.extras,
							ref local.magic, ref local.torn, ref local.notInUse, ref local.files, ref local.noData, local.walks, ref local.extUsed);
					}
					local.parseTicks += Stopwatch.GetTimestamp() - p0;
				}
				finally
				{
					ArrayPool<byte>.Shared.Return(chunk);
				}
				return local;
			},
			local =>
			{
				try
				{
					lock (mergeLock)
					{
						skippedMagic += local.magic;
						skippedTorn += local.torn;
						skippedNotInUse += local.notInUse;
						parsedFiles += local.files;
						parsedNoData += local.noData;
						extDataUsed += local.extUsed;
						readTicksTotal += local.readTicks;
						parseTicksTotal += local.parseTicks;
						foreach (var kvp in local.extras)
						{
							if (!allExtras.TryGetValue(kvp.Key, out var list))
							{
								list = new List<(long, string)>(kvp.Value.Count);
								allExtras[kvp.Key] = list;
							}
							list.AddRange(kvp.Value);
						}
						foreach (var walk in local.walks)
							if (allWalks.Count < 5)
								allWalks.Add(walk);
					}
				}
				finally
				{
					local.stream?.Dispose();
					local.handle?.Dispose();
				}
			});

		extraNames = allExtras;
		walkSamples = allWalks;
		Trace.WriteLine("[MftData] phaseA done");

		long parseMs = sw.ElapsedMilliseconds;

		// Phase two: resolve directory paths once (memoized), then emit files with a
		// single concat off the resolved parent instead of a walk per file.
		// P2: the resolve loop is parallel. resolved[] is write-once per record and
		// recomputes are identical, so concurrent fills are benign; each worker owns
		// its scratch list and collects (path, meta) pairs merged lock-free below.
		var resolved = new string?[count];
		resolved[ROOT_RECORD_NUMBER] = drive;
		// P0: size from the actual parse count (was a fixed 450k). parsedFiles counts
		// named non-directory records, so files + hard-link extras fit without regrow.
		var files = new List<ScanItem>((int)Math.Min(int.MaxValue, Math.Max(4096, parsedFiles + extraNames.Count + 1024)));
		var dirPairs = new System.Collections.Concurrent.ConcurrentBag<List<(int Record, string Path, DirMeta Meta)>>();
		var dirFailed = new System.Collections.Concurrent.ConcurrentBag<List<int>>();
		Parallel.ForEach(System.Collections.Concurrent.Partitioner.Create(0, count),
			new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount, CancellationToken = token },
			() => (scratch: new List<int>(32), pairs: new List<(int Record, string Path, DirMeta Meta)>(1024), failed: new List<int>()),
			(range, _, local) =>
			{
				for (int record = range.Item1; record < range.Item2; record++)
				{
					if (names[record] == null || (recordFlags[record] & 0x02) == 0)
						continue;

					string? dirPath = ResolvePath(record, parent, names, resolved, local.scratch, drive);
					if (dirPath != null)
						local.pairs.Add((record, dirPath, new DirMeta(DiskCluster.FromFileTimeUtcOrMinValue(siModified[record]), DiskNode.FormatAttributes((FileAttributes)siAttrs[record]))));
					else
						local.failed.Add(record); // parent chain through a later record: retry below
				}
				return local;
			},
			local =>
			{
				if (local.pairs.Count > 0)
					dirPairs.Add(local.pairs);
				if (local.failed.Count > 0)
					dirFailed.Add(local.failed);
			});

		int dirTotal = 0;
		foreach (var list in dirPairs)
			dirTotal += list.Count;
		var dirs = new List<MftDirRow>(dirTotal + 16);
		var seenDirPaths = new HashSet<string>(dirTotal + 16, StringComparer.Ordinal);
		foreach (var list in dirPairs)
			foreach (var (record, path, meta) in list)
				if (seenDirPaths.Add(path))
					dirs.Add(new MftDirRow(record, path, meta));

		// Fixpoint: records whose parent chain ran through a not-yet-resolved later record
		// resolve now that the memo table is populated. Converges in ~1 extra pass; without
		// it a lite file row could name a parent with no folder row (legacy path built the
		// chain on demand, lite rows cannot).
		if (dirFailed.Count > 0)
		{
			var pending = new List<int>();
			foreach (var list in dirFailed)
				pending.AddRange(list);
			var retryScratch = new List<int>(32);
			for (int pass = 0; pass < 8 && pending.Count > 0; pass++)
			{
				token.ThrowIfCancellationRequested();
				bool progressed = false;
				for (int i = pending.Count - 1; i >= 0; i--)
				{
					int record = pending[i];
					string? dirPath = ResolvePath(record, parent, names, resolved, retryScratch, drive);
					if (dirPath == null)
						continue;

					pending.RemoveAt(i);
					progressed = true;
					if (seenDirPaths.Add(dirPath))
						dirs.Add(new MftDirRow(record, dirPath, new DirMeta(DiskCluster.FromFileTimeUtcOrMinValue(siModified[record]), DiskNode.FormatAttributes((FileAttributes)siAttrs[record]))));
				}
				if (!progressed)
					break;
			}
		}
		long resolveEndMs = sw.ElapsedMilliseconds;

		// P1: the file-emit pass is parallel. The directory-resolve loop above is
		// complete, so resolved[] is effectively read-only here; a ResolvePath miss
		// on an orphan chain only recomputes an identical string (reference writes
		// are atomic). Each worker owns its list + scratch; the merge is one
		// AddRange per worker. Row order is irrelevant — BuildTree re-sorts.
		var emitLock = new object();
		var emitLists = new List<List<ScanItem>>();
		long emitExtras = 0;
		long emitExtData = 0;
		long emitNoName = 0;
		long emitNamelessLive = 0;
		Parallel.ForEach(System.Collections.Concurrent.Partitioner.Create(0, count),
			new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount, CancellationToken = token },
			() => (list: new List<ScanItem>((int)Math.Min(int.MaxValue, Math.Max(4096, parsedFiles / (uint)Math.Max(1, Environment.ProcessorCount) + 1024))), scratch: new List<int>(32), extras: 0L, extData: 0L, noName: 0L, namelessLive: 0L),
			(range, _, local) =>
			{
				for (int record = range.Item1; record < range.Item2; record++)
				{
					if ((record & 0x3FFF) == 0)
						token.ThrowIfCancellationRequested();

					bool hasPrimary = names[record] != null;
					bool hasExtras = extraNames.TryGetValue(record, out var recExtras) && recExtras.Count > 0;
					if (!hasPrimary && !hasExtras)
					{
						local.noName++;
						if (hasData[record] || siAttrs[record] != 0)
							local.namelessLive++;
						continue;
					}

					if ((recordFlags[record] & 0x02) != 0 || record < 16)
						continue; // directories resolved above; system files ($MFT, $LogFile, ...) are not indexed

					long size;
					long allocated;
					int dataSource = hasData[record] ? record : -1;
					if (dataSource < 0 && dataExtRef[record] >= 0 && dataExtRef[record] < count && hasData[(int)dataExtRef[record]])
					{
						dataSource = (int)dataExtRef[record];
						local.extData++;
					}
					if (dataSource >= 0)
					{
						size = dataReal[dataSource];
						allocated = dataResident[dataSource] ? DiskCluster.AlignSize(size, cluster, clusterIsPowerOfTwo) : dataAlloc[dataSource];
					}
					else
					{
						size = fileNameReal[record];
						allocated = DiskCluster.AlignSize(size, cluster, clusterIsPowerOfTwo);
					}
					DateTime modified = DiskCluster.FromFileTimeUtcOrMinValue(siModified[record]);
					string attributes = DiskNode.FormatAttributes((FileAttributes)siAttrs[record]);

					if (hasPrimary)
					{
						// Lite row: no path string is built (saves ~400k concats per drive).
						// The tree resolves the parent node by record id; the File View
						// materializes paths for listed rows only. Parent chains that never
						// resolved (corrupt links) drop the row, as before.
						long parentRecord = parent[record];
						bool parentOk = parentRecord >= 0 && parentRecord < count && resolved[(int)parentRecord] != null;
						if (!parentOk)
							parentOk = ResolvePath(record, parent, names, resolved, local.scratch, drive) != null;
						if (parentOk)
							local.list.Add(new ScanItem { FullPath = string.Empty, Name = names[record], ParentId = parentRecord, Size = Math.Max(0, size), AllocatedSize = Math.Max(0, allocated), Modified = modified, IsFolder = false, Attributes = attributes, FileId = record, Extension = Path.GetExtension(names[record]!) });
					}

					if (hasExtras)
					{
						foreach (var (extraParent, extraName) in recExtras!)
						{
							if (extraParent == parent[record] && extraName == names[record])
								continue; // same as the primary path

							// Lite extra row: the parent id suffices; resolve only validates.
							bool extraOk = extraParent >= 0 && extraParent < count && (extraParent == ROOT_RECORD_NUMBER || resolved[(int)extraParent] != null);
							if (!extraOk)
								extraOk = ResolveExtraPath(extraParent, extraName, parent, names, resolved, local.scratch, drive) != null;
							if (extraOk)
							{
								local.extras++;
								local.list.Add(new ScanItem { FullPath = string.Empty, Name = extraName, ParentId = extraParent, Size = Math.Max(0, size), AllocatedSize = Math.Max(0, allocated), Modified = modified, IsFolder = false, Attributes = attributes, FileId = record, Extension = Path.GetExtension(extraName) });
							}
						}
					}
				}
				return local;
			},
			local =>
			{
				lock (emitLock)
				{
					emitLists.Add(local.list);
					emitExtras += local.extras;
					emitExtData += local.extData;
					emitNoName += local.noName;
					emitNamelessLive += local.namelessLive;
				}
			});

		foreach (var local in emitLists)
			files.AddRange(local);
		emittedExtras += emitExtras;
		extDataUsed += emitExtData;
		skippedNoName += emitNoName;
		namelessLive += emitNamelessLive;

		Trace.WriteLine($"[MftData] {files.Count:N0} files, {dirs.Count:N0} dirs in {sw.Elapsed.TotalSeconds:F2}s (parse {parseMs}ms)");
		double tickMs = 1000.0 / Stopwatch.Frequency;
		string diagnostics = $"records={count:N0} magicFail={skippedMagic:N0} torn={skippedTorn:N0} notInUse={skippedNotInUse:N0} noName={skippedNoName:N0} namelessLive={namelessLive:N0} files={files.Count:N0} dirs={dirs.Count:N0} parsedFiles={parsedFiles:N0} parsedNoData={parsedNoData:N0} extras={emittedExtras:N0} extData={extDataUsed:N0} readSum={(long)(readTicksTotal * tickMs):N0}ms parseSum={(long)(parseTicksTotal * tickMs):N0}ms resolve={resolveEndMs - parseMs:N0}ms emit={sw.ElapsedMilliseconds - resolveEndMs:N0}ms walks=[{string.Join(';', walkSamples)}]";
		return (files, dirs, diagnostics);
	}

	private static (long MftOffset, int RecordSize, long MftLength, int BytesPerCluster) GetLayout(SafeFileHandle volume, string drive)
	{
		Span<byte> output = stackalloc byte[VOLUME_DATA_LENGTH];
		uint returned;
		bool ok = GetVolumeData(volume, output, out returned);
		if (!ok || returned < VOLUME_DATA_LENGTH)
			throw new InvalidOperationException($"FSCTL_GET_NTFS_VOLUME_DATA failed for {drive}.");

		int bytesPerCluster = BinaryPrimitives.ReadInt32LittleEndian(output.Slice(44, 4));
		int bytesPerRecord = BinaryPrimitives.ReadInt32LittleEndian(output.Slice(48, 4));
		int clustersPerRecord = BinaryPrimitives.ReadInt32LittleEndian(output.Slice(52, 4));
		long validLength = BinaryPrimitives.ReadInt64LittleEndian(output.Slice(56, 8));
		long mftStartLcn = BinaryPrimitives.ReadInt64LittleEndian(output.Slice(64, 8));

		int recordSize = bytesPerRecord > 0 ? bytesPerRecord : 1 << -clustersPerRecord;
		if (recordSize is < 512 or > 8192 || bytesPerCluster <= 0)
			throw new InvalidOperationException($"Implausible NTFS geometry for {drive}.");

		return (mftStartLcn * bytesPerCluster, recordSize, validLength, bytesPerCluster);
	}

	/// <summary>
	/// NTFS Update Sequence fixup in place. False on torn writes or implausible headers.
	/// </summary>
	private static bool ApplyFixup(Span<byte> record, int recordSize)
	{
		int sectors = recordSize / 512;
		int usaOffset = BinaryPrimitives.ReadUInt16LittleEndian(record.Slice(4, 2));
		int usaCount = BinaryPrimitives.ReadUInt16LittleEndian(record.Slice(6, 2));
		if (usaOffset + usaCount * 2 > recordSize || usaCount != sectors + 1)
			return false;

		ushort usn = BinaryPrimitives.ReadUInt16LittleEndian(record.Slice(usaOffset, 2));
		for (int s = 0; s < sectors; s++)
		{
			int trailer = s * 512 + 510;
			if (BinaryPrimitives.ReadUInt16LittleEndian(record.Slice(trailer, 2)) != usn)
				return false;

			BinaryPrimitives.WriteUInt16LittleEndian(record.Slice(trailer, 2), BinaryPrimitives.ReadUInt16LittleEndian(record.Slice(usaOffset + 2 + s * 2, 2)));
		}
		return true;
	}

	/// <summary>
	/// Reads $MFT record 0 and follows its unnamed $DATA runs so fragmented MFTs parse
	/// fully. Returns (LCN, cluster count) per run; negative LCN marks a sparse gap.
	/// Null when record 0 cannot be parsed (callers fall back to a linear read).
	/// </summary>
	private static List<(long Lcn, long Clusters)>? ReadDataRuns(FileStream stream, long mftOffset, int recordSize)
	{
		try
		{
			byte[] first = new byte[recordSize];
			stream.Position = mftOffset;
			stream.ReadExactly(first, 0, recordSize);
			var record = new Span<byte>(first);
			if (BinaryPrimitives.ReadUInt32LittleEndian(record) != FILE_RECORD_MAGIC || !ApplyFixup(record, recordSize))
				return null;

			int attrOffset = BinaryPrimitives.ReadUInt16LittleEndian(record.Slice(20, 2));
			int usedSize = BinaryPrimitives.ReadInt32LittleEndian(record.Slice(24, 4));
			int at = attrOffset;
			while (at + 8 <= usedSize)
			{
				uint type = BinaryPrimitives.ReadUInt32LittleEndian(record.Slice(at, 4));
				if (type == 0xFFFFFFFF)
					break;

				int length = BinaryPrimitives.ReadInt32LittleEndian(record.Slice(at + 4, 4));
				if (length < 64 || at + length > usedSize)
					break;

				bool nonResident = record[at + 8] != 0;
				int nameLength = record[at + 9];
				if (type == 0x80 && nameLength == 0 && nonResident)
				{
					int runOffset = BinaryPrimitives.ReadUInt16LittleEndian(record.Slice(at + 32, 2)) + at;
					int runEnd = at + length;
					var runs = new List<(long, long)>();
					long lcn = 0;
					int cursor = runOffset;
					while (cursor < runEnd)
					{
						byte header = record[cursor++];
						if (header == 0)
							break;

						int lengthSize = header & 0x0F;
						int offsetSize = (header >> 4) & 0x0F;
						if (lengthSize <= 0 || lengthSize > 8 || offsetSize > 8 || cursor + lengthSize + offsetSize > runEnd)
							return null;

						long clusters = 0;
						for (int i = 0; i < lengthSize; i++)
							clusters |= (long)record[cursor + i] << (8 * i);
						cursor += lengthSize;

						if (offsetSize == 0)
						{
							runs.Add((-1, clusters)); // sparse gap
							continue;
						}

						long delta = 0;
						for (int i = 0; i < offsetSize; i++)
							delta |= (long)record[cursor + i] << (8 * i);
						if ((record[cursor + offsetSize - 1] & 0x80) != 0)
							delta |= -1L << (8 * offsetSize); // sign-extend
						cursor += offsetSize;
						lcn += delta;
						runs.Add((lcn, clusters));
					}
					return runs.Count > 0 ? runs : null;
				}
				at += length;
			}
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
		{
			Trace.WriteLine($"[MftData] data-run walk failed: {ex.Message}");
		}
		return null;
	}

	private static unsafe bool GetVolumeData(SafeFileHandle volume, Span<byte> output, out uint returned)
	{
		fixed (byte* outPtr = output)
		{
			var outSpan = new Span<byte>(outPtr, output.Length);
			return PInvoke.DeviceIoControl(volume, FSCTL_GET_NTFS_VOLUME_DATA, default, outSpan, out returned, null);
		}
	}

	private static void ParseRecord(Span<byte> record, int recordNumber, int recordSize,
		long[] parent, string?[] names, byte[] recordFlags, uint[] siAttrs, long[] siModified,
		long[] dataAlloc, long[] dataReal, long[] fileNameReal, bool[] hasData, bool[] dataResident,
		long[] dataExtRef,
		Dictionary<int, List<(long Parent, string Name)>> extraNames,
		ref long skippedMagic, ref long skippedTorn, ref long skippedNotInUse, ref long parsedFiles, ref long parsedNoData, List<string> walkSamples, ref long extDataUsed)
	{
		if (record.Length < recordSize || BinaryPrimitives.ReadUInt32LittleEndian(record) != FILE_RECORD_MAGIC)
		{
			skippedMagic++;
			return;
		}

		// Update Sequence fixup: each sector's trailing USN must match, then restore.
		if (!ApplyFixup(record, recordSize))
		{
			skippedTorn++;
			return;
		}

		byte flags = (byte)(record[22] & 0x03);
		if ((flags & 0x01) == 0)
		{
			skippedNotInUse++;
			return; // not in use
		}

		long baseRecord = BinaryPrimitives.ReadInt64LittleEndian(record.Slice(32, 8)) & 0xFFFFFFFFFFFFL;
		bool isExtension = baseRecord != 0 && baseRecord != recordNumber;

		int attrOffset = BinaryPrimitives.ReadUInt16LittleEndian(record.Slice(20, 2));
		int usedSize = BinaryPrimitives.ReadInt32LittleEndian(record.Slice(24, 4));
		if (attrOffset <= 0 || usedSize > recordSize)
			return;

		recordFlags[recordNumber] = flags;
		string? selectedName = null;
		long selectedParent = -1;
		bool selectedWin32 = false;
		int attrCount = 0;
		int firstType = -1;
		int firstLen = 0;
		int lastType = -1;
		bool sawAttrList = false;

		int at = attrOffset;
		while (at + 8 <= usedSize)
		{
			uint type = BinaryPrimitives.ReadUInt32LittleEndian(record.Slice(at, 4));
			if (type == 0xFFFFFFFF)
				break;

			int length = BinaryPrimitives.ReadInt32LittleEndian(record.Slice(at + 4, 4));
			if (length < 16 || at + length > usedSize)
				break;

			attrCount++;
			if (type == 0x20)
				sawAttrList = true;
			if (firstType == -1)
			{
				firstType = (int)type;
				firstLen = length;
			}
			lastType = (int)type;

			bool nonResident = record[at + 8] != 0;
			int nameLength = record[at + 9];
			int nameOffset = BinaryPrimitives.ReadUInt16LittleEndian(record.Slice(at + 10, 2));

			if (type == 0x10 && !nonResident) // $STANDARD_INFORMATION
			{
				int contentLength = BinaryPrimitives.ReadInt32LittleEndian(record.Slice(at + 16, 4));
				int contentOffset = BinaryPrimitives.ReadUInt16LittleEndian(record.Slice(at + 20, 2));
				if (contentLength >= 36 && at + contentOffset + 36 <= usedSize)
				{
					siModified[recordNumber] = BinaryPrimitives.ReadInt64LittleEndian(record.Slice(at + contentOffset + 8, 8));
					siAttrs[recordNumber] = BinaryPrimitives.ReadUInt32LittleEndian(record.Slice(at + contentOffset + 32, 4));
				}
			}
			else if (type == 0x30 && !nonResident) // $FILE_NAME
			{
				int contentLength = BinaryPrimitives.ReadInt32LittleEndian(record.Slice(at + 16, 4));
				int contentOffset = BinaryPrimitives.ReadUInt16LittleEndian(record.Slice(at + 20, 2));
				if (contentLength >= 66 && at + contentOffset + 66 <= usedSize)
				{
					int content = at + contentOffset;
					long fileParent = BinaryPrimitives.ReadInt64LittleEndian(record.Slice(content, 8)) & 0xFFFFFFFFFFFFL;
					int fnNameLength = record[content + 64];
					int fnNamespace = record[content + 65];
					if (fnNameLength > 0 && content + 66 + fnNameLength * 2 <= usedSize)
					{
						string fileName = Encoding.Unicode.GetString(record.Slice(content + 66, fnNameLength * 2));
						if (DebugNameContains != null && fileName.Contains(DebugNameContains, StringComparison.OrdinalIgnoreCase))
							Trace.WriteLine($"[MftData] dbg rec={recordNumber} name={fileName} ns={fnNamespace} base={BinaryPrimitives.ReadInt64LittleEndian(record.Slice(32, 8)) & 0xFFFFFFFFFFFFL} flags={record[22]:X2}");
						fileNameReal[recordNumber] = BinaryPrimitives.ReadInt64LittleEndian(record.Slice(content + 48, 8));
						bool isWin32 = fnNamespace == 1 || fnNamespace == 3;
						// DOS-only (8.3) aliases are never real paths; POSIX names are.
						bool isLinkName = fnNamespace != 2;
						if (isExtension)
						{
							// Overflow names live in extension records; attribute them to the base.
							if (isWin32 && baseRecord >= 0 && baseRecord < parent.Length)
								AddExtraName(extraNames, (int)baseRecord, fileParent, fileName);
						}
						else if (selectedName == null || (isWin32 && !selectedWin32))
						{
							if (isWin32 && selectedName != null && selectedParent >= 0 && (selectedName != fileName || selectedParent != fileParent))
								AddExtraName(extraNames, recordNumber, selectedParent, selectedName);

							selectedName = fileName;
							selectedParent = fileParent;
							selectedWin32 = isWin32;
						}
						else if (isLinkName && (fileName != selectedName || fileParent != selectedParent))
						{
							AddExtraName(extraNames, recordNumber, fileParent, fileName);
						}
					}
				}
			}
			else if (type == 0x20 && !nonResident && dataExtRef[recordNumber] < 0) // $ATTRIBUTE_LIST: $DATA may live in an extension record
			{
				int contentLength = BinaryPrimitives.ReadInt32LittleEndian(record.Slice(at + 16, 4));
				int contentOffset = BinaryPrimitives.ReadUInt16LittleEndian(record.Slice(at + 20, 2));
				int listEnd = Math.Min(at + contentOffset + contentLength, usedSize);
				int entry = at + contentOffset;
				while (entry + 24 <= listEnd)
				{
					uint entryType = BinaryPrimitives.ReadUInt32LittleEndian(record.Slice(entry, 4));
					int entryLength = BinaryPrimitives.ReadUInt16LittleEndian(record.Slice(entry + 4, 2));
					int entryNameLength = record[entry + 6];
					if (entryLength < 24 || entry + entryLength > listEnd)
						break;

					if (entryType == 0x80 && entryNameLength == 0)
					{
						long dataRef = BinaryPrimitives.ReadInt64LittleEndian(record.Slice(entry + 16, 8)) & 0xFFFFFFFFFFFFL;
						if (dataRef > 0 && dataRef < parent.Length)
							dataExtRef[recordNumber] = dataRef;
						break;
					}
					entry += entryLength;
				}
			}
			else if (type == 0x80 && nameLength == 0 && !hasData[recordNumber]) // unnamed $DATA
			{
				if (!nonResident)
				{
					int contentLength = BinaryPrimitives.ReadInt32LittleEndian(record.Slice(at + 16, 4));
					dataReal[recordNumber] = Math.Max(0, contentLength);
					dataResident[recordNumber] = true;
					hasData[recordNumber] = true;
				}
				else
				{
					dataAlloc[recordNumber] = Math.Max(0, BinaryPrimitives.ReadInt64LittleEndian(record.Slice(at + 40, 8)));
					dataReal[recordNumber] = Math.Max(0, BinaryPrimitives.ReadInt64LittleEndian(record.Slice(at + 48, 8)));
					hasData[recordNumber] = true;
				}
			}

			at += length;
		}

		if (selectedName != null)
		{
			names[recordNumber] = selectedName;
			parent[recordNumber] = selectedParent;
		}

		if ((recordFlags[recordNumber] & 0x02) == 0 && selectedName != null)
		{
			parsedFiles++;
			if (!hasData[recordNumber])
			{
				parsedNoData++;
				if (recordNumber >= 16 && walkSamples.Count < 5)
					walkSamples.Add($"rec={recordNumber} attrs={attrCount} first={firstType:X}/{firstLen} last={lastType:X} attrList={sawAttrList} used={BinaryPrimitives.ReadInt32LittleEndian(record.Slice(24, 4))} attrOff={BinaryPrimitives.ReadUInt16LittleEndian(record.Slice(20, 2))} flags={record[22]:X2} name={selectedName}");
			}
		}
	}

	private static void AddExtraName(Dictionary<int, List<(long Parent, string Name)>> extraNames, int record, long fileParent, string fileName)
	{
		if (!extraNames.TryGetValue(record, out var list))
		{
			list = new List<(long, string)>(2);
			extraNames[record] = list;
		}
		foreach (var (existingParent, existingName) in list)
			if (existingParent == fileParent && existingName == fileName)
				return;

		list.Add((fileParent, fileName));
	}

	private static string? ResolvePath(int record, long[] parent, string?[] names, string?[] resolved, List<int> scratch, string drive)
	{
		string? cached = resolved[record];
		if (cached != null)
			return cached;

		scratch.Clear();
		int current = record;
		int steps = 0;
		while (current != ROOT_RECORD_NUMBER && current >= 0 && current < parent.Length && resolved[current] == null && steps++ < 256)
		{
			if (names[current] == null)
				return null;

			scratch.Add(current);
			long fileParent = parent[current];
			if (fileParent < 0 || fileParent >= parent.Length)
				return null;

			current = (int)fileParent;
		}

		if (current != ROOT_RECORD_NUMBER && (current < 0 || current >= parent.Length || resolved[current] == null))
			return null;

		string basePath = current == ROOT_RECORD_NUMBER ? drive : resolved[current] ?? drive;
		scratch.Reverse();
		if (scratch.Count == 0)
		{
			resolved[record] = basePath;
			return basePath;
		}

		// P1: single string.Create over pre-measured length. Was: parts[] alloc +
		// base + "\\" + Join (3 allocs + an intermediate parts array per directory).
		int length = basePath.Length;
		for (int i = 0; i < scratch.Count; i++)
			length += 1 + (names[scratch[i]]?.Length ?? 0);

		string full = string.Create(length, (basePath, names, scratch), static (span, s) =>
		{
			s.basePath.AsSpan().CopyTo(span);
			int pos = s.basePath.Length;
			for (int i = 0; i < s.scratch.Count; i++)
			{
				span[pos++] = '\\';
				string? part = s.names[s.scratch[i]];
				if (!string.IsNullOrEmpty(part))
				{
					part.AsSpan().CopyTo(span.Slice(pos));
					pos += part.Length;
				}
			}
		});
		resolved[record] = full;
		return full;
	}

	private static string? ResolveExtraPath(long fileParent, string fileName, long[] parent, string?[] names, string?[] resolved, List<int> scratch, string drive)
	{
		if (fileParent < 0 || fileParent >= parent.Length)
			return null;

		string? parentPath = fileParent == ROOT_RECORD_NUMBER ? drive : ResolvePath((int)fileParent, parent, names, resolved, scratch, drive);
		if (parentPath == null)
			return null;

		return parentPath + "\\" + fileName;
	}

	/// <summary>
	/// Per-worker parse state: disjoint array slots need no locks, only the extras
	/// dictionary, walk samples and counters merge at the end. The volume handle
	/// and stream are opened once per worker and disposed in the merge step.
	/// </summary>
	private sealed class MftThreadState
	{
		public readonly Dictionary<int, List<(long Parent, string Name)>> extras = new();
		public readonly List<string> walks = new();
		public SafeFileHandle? handle;
		public FileStream? stream;
		public long readTicks;
		public long parseTicks;
		public long magic;
		public long torn;
		public long notInUse;
		public long files;
		public long noData;
		public long extUsed;
	}
}
