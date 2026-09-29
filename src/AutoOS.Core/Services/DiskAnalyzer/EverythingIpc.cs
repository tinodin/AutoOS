using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace AutoOS.Core.Services.DiskAnalyzer;

internal static class EverythingIpc
{
	public const uint EVERYTHING_IPC_COPYDATA_QUERY2W = 18;
	public const uint QUERY2_REQUEST_FILE_NAME = 0x00000001;
	public const uint QUERY2_REQUEST_PATH = 0x00000002;
	public const uint QUERY2_REQUEST_SIZE = 0x00000010;
	public const uint QUERY2_REQUEST_DATE_MODIFIED = 0x00000040;
	public const uint QUERY2_SORT_NONE = 0;
	// QUERY2 additional request flags (echoed back in LIST2.request_flags).
	public const uint QUERY2_REQUEST_FULL_PATH_AND_NAME = 0x00000004;
	public const uint QUERY2_REQUEST_EXTENSION = 0x00000008;
	public const uint QUERY2_REQUEST_DATE_CREATED = 0x00000020;
	public const uint QUERY2_REQUEST_DATE_ACCESSED = 0x00000080;
	public const uint QUERY2_REQUEST_ATTRIBUTES = 0x00000100;
	public const uint QUERY2_REQUEST_FILE_LIST_FILE_NAME = 0x00000200;
	public const uint QUERY2_REQUEST_RUN_COUNT = 0x00000400;
	public const uint QUERY2_REQUEST_DATE_RUN = 0x00000800;
	public const uint QUERY2_REQUEST_DATE_RECENTLY_CHANGED = 0x00001000;
	public const uint QUERY2_REQUEST_HIGHLIGHTED_NAME = 0x00002000;
	public const uint QUERY2_REQUEST_HIGHLIGHTED_PATH = 0x00004000;
	public const uint QUERY2_REQUEST_HIGHLIGHTED_FULL_PATH_AND_NAME = 0x00008000;
	private const int CLASS_ALREADY_EXISTS = 1410;
	private const int LIST2_HEADER_SIZE = 20;
	private const int ITEM2_SIZE = 8;
	private const int MAX_REPLY_ITEMS = 2000000;
	private const int MAX_STRING_CHARS = 32767;

	public static readonly string[] WindowNames = ["EVERYTHING_TASKBAR_NOTIFICATION_(1.5a)", "EVERYTHING_TASKBAR_NOTIFICATION"];

	private const uint EVERYTHING_IPC_ITEM_IS_FOLDER = 0x01;

	public static bool IsServerRunning() => TryFindServer(out _);

	public static bool TryFindServer(out HWND server)
	{
		foreach (string windowName in WindowNames)
		{
			HWND found = PInvoke.FindWindow(windowName, default);
			if (found != HWND.Null)
			{
				server = found;
				return true;
			}
		}

		server = HWND.Null;
		return false;
	}

	public static int Query(CopyDataReceiver receiver, HWND server, string search, uint requestFlags, int maxResults, int offset, List<ScanItem> rows, ScanRowOptions rowOptions, int timeoutMs)
		=> QueryViaCopyData(server, receiver, search, requestFlags, maxResults, offset, rows, rowOptions, timeoutMs);

	private static unsafe int QueryViaCopyData(HWND server, CopyDataReceiver receiver, string search, uint requestFlags, int maxResults, int offset, List<ScanItem> rows, ScanRowOptions rowOptions, int timeoutMs)
	{
		// EVERYTHING_IPC_QUERY2 is 7 DWORDs (28 bytes): reply_hwnd is DWORD even on x64.
		// Encode the search directly into the payload: no search+'\0' string plus GetBytes alloc.
		int queryBytes = Encoding.Unicode.GetByteCount(search) + 2;
		byte[] payload = new byte[28 + queryBytes];
		Span<byte> span = payload.AsSpan();
		BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(0, 4), unchecked((uint)receiver.Handle.ToInt64()));
		BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(4, 4), receiver.ReplyMessage);
		BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(8, 4), 0);
		BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(12, 4), (uint)Math.Max(0, offset));
		BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(16, 4), (uint)Math.Max(1, maxResults));
		BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(20, 4), requestFlags);
		BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(24, 4), QUERY2_SORT_NONE);
		Encoding.Unicode.GetBytes(search.AsSpan(), span.Slice(28));
		// NUL terminator: payload is zero-initialized, the last 2 bytes already read 0.
		LRESULT send;
		nuint reply = 0;
		// Arm the target BEFORE the send: the server may reply synchronously while
		// SendMessageTimeout blocks, so OnReply must already know where to parse.
		receiver.ArmQuery(rows, rowOptions);
		try
		{
			fixed (byte* pPayload = payload)
			{
				CopyDataStruct cd = new() { dwData = (UIntPtr)EVERYTHING_IPC_COPYDATA_QUERY2W, cbData = (uint)payload.Length, lpData = pPayload };
				send = PInvoke.SendMessageTimeout(server, PInvoke.WM_COPYDATA, receiver.WParamHandle, new LPARAM((nint)(&cd)), SEND_MESSAGE_TIMEOUT_FLAGS.SMTO_BLOCK, (uint)timeoutMs, &reply);
			}
		}
		catch
		{
			receiver.CancelQuery();
			throw;
		}
		if (send.Value == 0)
		{
			receiver.CancelQuery();
			throw new InvalidOperationException("Everything query timed out (SendMessageTimeout).");
		}
		return receiver.WaitReply(timeoutMs);
	}

	/// <summary>
	/// Row-conversion options captured once per scan. Parsing appends <see cref="ScanItem"/>
	/// records straight into the caller's list: no intermediate per-row type, no second pass.
	/// </summary>
	public readonly record struct ScanRowOptions(uint Cluster, bool ClusterIsPowerOfTwo, bool IncludeDates, string Root, bool SkipPrefixCheck, CancellationToken CancellationToken);

	[StructLayout(LayoutKind.Sequential)]
	private unsafe struct CopyDataStruct { public UIntPtr dwData; public uint cbData; public void* lpData; }

	public sealed class CopyDataReceiver : IDisposable
	{
		private const string CLASS_NAME = "AutoOSDiskAnalyzerIpcReceiver";
		private const uint REPLY_MESSAGE_BASE = 0x50000000;
		private static uint _nextId;
		private static readonly Dictionary<nint, CopyDataReceiver> _active = new();
		private static readonly object _lock = new();
		private readonly ManualResetEventSlim _reply = new(false);
		private HWND _hwnd;
		private uint _threadId;
		private Exception? _replyError;
		private List<ScanItem>? _targetRows;
		private ScanRowOptions _targetOptions;
		private int _targetReceived;
		private bool _disposed;
		public uint ReplyMessage { get; }
		public nint Handle { get { unsafe { return (nint)_hwnd.Value; } } }
		public WPARAM WParamHandle { get { unsafe { return new WPARAM(unchecked((nuint)_hwnd.Value)); } } }

		public unsafe CopyDataReceiver()
		{
			ReplyMessage = REPLY_MESSAGE_BASE + (uint)Interlocked.Increment(ref _nextId);
			WNDPROC proc = new(&WndProc);
			// WNDPROC wraps a CsWin32 delegate* unmanaged[Stdcall] — no GCHandle needed;
			// &WndProc is a static UnmanagedCallersOnly entry point that lives forever.
			using var ready = new ManualResetEventSlim(false);
			var thread = new Thread(() => ReceiverThread(proc, ready)) { IsBackground = true, Name = "Everything IPC receiver" };
			thread.Start();
			if (!ready.Wait(TimeSpan.FromSeconds(10)))
				throw new InvalidOperationException("Timed out creating Everything IPC receiver window.");
			if (_hwnd == HWND.Null)
				throw new InvalidOperationException("Failed to create Everything IPC receiver window.");
		}

		/// <summary>
		/// Arms the reply target before the send. The server may reply synchronously
		/// while <c>SendMessageTimeout</c> blocks, so <see cref="OnReply"/> must already
		/// know the destination list.
		/// </summary>
		public void ArmQuery(List<ScanItem> rows, ScanRowOptions rowOptions)
		{
			lock (_lock)
			{
				_targetRows = rows;
				_targetOptions = rowOptions;
				_targetReceived = -1;
				// Drain any stale signal so WaitReply below only observes this query's reply.
				_reply.Reset();
				_replyError = null;
			}
		}

		public void CancelQuery()
		{
			lock (_lock)
			{
				_targetRows = null;
			}
		}

		/// <summary>
		/// Blocks until the server replies. Rows were already appended to the armed
		/// list by <see cref="OnReply"/>; returns the row count from this reply.
		/// </summary>
		public int WaitReply(int ms)
		{
			if (!_reply.Wait(ms))
			{
				lock (_lock)
				{
					_targetRows = null;
				}

				throw new InvalidOperationException("Everything reply timed out (WaitReply).");
			}
			lock (_lock)
			{
				try
				{
					if (_replyError != null)
						throw new InvalidOperationException("Everything reply failed validation.", _replyError);
					return _targetReceived;
				}
				finally
				{
					_targetRows = null;
					_reply.Reset();
					_replyError = null;
				}
			}
		}

		private unsafe void ReceiverThread(WNDPROC proc, ManualResetEventSlim ready)
		{
			try
			{
				_threadId = PInvoke.GetCurrentThreadId();
				fixed (char* pClass = CLASS_NAME)
				fixed (char* pTitle = CLASS_NAME)
				{
					var windowClass = new WNDCLASSW { style = WNDCLASS_STYLES.CS_HREDRAW | WNDCLASS_STYLES.CS_VREDRAW, lpfnWndProc = proc, lpszClassName = pClass };
					if (PInvoke.RegisterClass(windowClass) == 0 && Marshal.GetLastWin32Error() != CLASS_ALREADY_EXISTS)
						return;

					_hwnd = PInvoke.CreateWindowEx(WINDOW_EX_STYLE.WS_EX_NOACTIVATE, pClass, pTitle, WINDOW_STYLE.WS_POPUP, 0, 0, 0, 0, HWND.Null, default, default, null);
					if (_hwnd == HWND.Null)
						return;
					lock (_lock)
						_active[(nint)_hwnd.Value] = this;
				}
			}
			finally
			{
				ready.Set();
			}
			if (_hwnd == HWND.Null)
				return;
			MSG msg;
			while (PInvoke.GetMessage(&msg, HWND.Null, 0, 0))
			{
				PInvoke.TranslateMessage(&msg);
				PInvoke.DispatchMessage(&msg);
			}
		}

		[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
		private static unsafe LRESULT WndProc(HWND hwnd, uint msg, WPARAM wParam, LPARAM lParam)
		{
			if (msg == PInvoke.WM_COPYDATA)
			{
				CopyDataStruct* cds = (CopyDataStruct*)(void*)lParam.Value;
				CopyDataReceiver? recv;
				lock (_lock)
					_active.TryGetValue((nint)hwnd.Value, out recv);
				if (recv != null)
				{
					recv.OnReply(cds);
					return new LRESULT(1);
				}
			}
			return PInvoke.DefWindowProc(hwnd, msg, wParam, lParam);
		}

		private unsafe void OnReply(CopyDataStruct* cds)
		{
			try
			{
				// Parse in place: OnReply runs on the receiver WndProc thread while
				// the COPYDATA pointer is valid, so no ToArray memcpy is needed.
				// Rows append straight into the scan's list: no intermediate per-row
				// type and no second convert pass.
				List<ScanItem>? rows;
				ScanRowOptions rowOptions;
				lock (_lock)
				{
					rows = _targetRows;
					rowOptions = _targetOptions;
				}
				if (rows == null)
					throw new InvalidOperationException("Everything reply arrived with no pending query.");
				ReadOnlySpan<byte> reply = new(cds->lpData, (int)cds->cbData);
				int received = ParseList2(reply, rows, rowOptions);
				lock (_lock)
					_targetReceived = received;
			}
			catch (Exception ex)
			{
				lock (_lock)
				{
					_replyError = ex;
				}
			}
			finally
			{
				_reply.Set();
			}
		}

		/// <summary>
		/// Parses an EVERYTHING_IPC_LIST2 reply, appending <see cref="ScanItem"/> rows to
		/// <paramref name="rows"/>. Returns the parsed row count (used for paging offsets:
		/// prefix-skipped rows still advance the server offset). Throws on any
		/// truncation or implausible extent so the scanner fails over to the walker.
		/// </summary>
		private static int ParseList2(ReadOnlySpan<byte> data, List<ScanItem> rows, ScanRowOptions rowOptions)
		{
			// EVERYTHING_IPC_LIST2: totitems, numitems, offset, request_flags, sort_type (20B),
			// then numitems x { flags, data_offset } (8B), then variable data per item in
			// request_flags order: [len:u32 + WCHAR[len+1]] for strings, Int64 size, FILETIME dates.
			if (data.Length < LIST2_HEADER_SIZE)
				throw new InvalidOperationException("Everything reply too short.");
			uint totItems = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(0, 4));
			uint numItems = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(4, 4));
			uint listRequestFlags = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(12, 4));
			uint n = Math.Min(totItems, numItems);
			if (n > MAX_REPLY_ITEMS)
				throw new InvalidOperationException($"Everything reply count implausible ({n}).");
			long itemsEnd = (long)LIST2_HEADER_SIZE + (long)n * ITEM2_SIZE;
			if (itemsEnd > data.Length)
				throw new InvalidOperationException("Everything reply truncated.");
			bool wantName = (listRequestFlags & QUERY2_REQUEST_FILE_NAME) != 0;
			bool wantPath = (listRequestFlags & QUERY2_REQUEST_PATH) != 0;
			bool wantFull = (listRequestFlags & QUERY2_REQUEST_FULL_PATH_AND_NAME) != 0;
			bool wantSize = (listRequestFlags & QUERY2_REQUEST_SIZE) != 0;
			bool wantModified = (listRequestFlags & QUERY2_REQUEST_DATE_MODIFIED) != 0;
			string root = rowOptions.Root;
			CancellationToken token = rowOptions.CancellationToken;
			// Drive-root unfiltered scans skip the per-row check: the quoted "C:\" search
			// already constrains server-side. Subfolder roots and filtered queries keep the
			// full guard — a quoted substring can match file names on other drives.
			bool guardPrefix = !rowOptions.SkipPrefixCheck;
			for (uint i = 0; i < n; i++)
			{
				// Cooperative cancel: parse runs on the receiver thread, otherwise a 750k
				// reply would delay cancellation by ~150ms.
				if ((i & 0x3FFF) == 0)
					token.ThrowIfCancellationRequested();
				ReadOnlySpan<byte> h = data.Slice(LIST2_HEADER_SIZE + (int)i * ITEM2_SIZE, ITEM2_SIZE);
				uint flags = BinaryPrimitives.ReadUInt32LittleEndian(h.Slice(0, 4));
				uint dataOffset = BinaryPrimitives.ReadUInt32LittleEndian(h.Slice(4, 4));
				if (dataOffset >= data.Length)
					throw new InvalidOperationException("Everything reply strings out of range.");
				int cursor = (int)dataOffset;
				string name = string.Empty;
				string path = string.Empty;
				string full = string.Empty;
				long size = 0;
				long modified = 0;
				// Reply fields arrive in request-flag order; every requested field must be walked
				// through even when the scan only keeps path, size and date.
				if (wantName)
					name = ReadListString(data, ref cursor);

				if (wantPath)
					path = ReadListString(data, ref cursor);

				if (wantFull)
					full = ReadListString(data, ref cursor);

				if ((listRequestFlags & QUERY2_REQUEST_EXTENSION) != 0)
					SkipListString(data, ref cursor);

				if (wantSize)
					size = ReadInt64(data, ref cursor);

				if ((listRequestFlags & QUERY2_REQUEST_DATE_CREATED) != 0)
					SkipBytes(data, ref cursor, 8);

				if (wantModified)
					modified = ReadInt64(data, ref cursor);

				if ((listRequestFlags & QUERY2_REQUEST_DATE_ACCESSED) != 0)
					SkipBytes(data, ref cursor, 8);

				if ((listRequestFlags & QUERY2_REQUEST_ATTRIBUTES) != 0)
					SkipBytes(data, ref cursor, 4);

				if ((listRequestFlags & QUERY2_REQUEST_FILE_LIST_FILE_NAME) != 0)
					SkipListString(data, ref cursor);

				if ((listRequestFlags & QUERY2_REQUEST_RUN_COUNT) != 0)
					SkipBytes(data, ref cursor, 4);

				if ((listRequestFlags & QUERY2_REQUEST_DATE_RUN) != 0)
					SkipBytes(data, ref cursor, 8);

				if ((listRequestFlags & QUERY2_REQUEST_DATE_RECENTLY_CHANGED) != 0)
					SkipBytes(data, ref cursor, 8);

				if ((listRequestFlags & QUERY2_REQUEST_HIGHLIGHTED_NAME) != 0)
					SkipListString(data, ref cursor);

				if ((listRequestFlags & QUERY2_REQUEST_HIGHLIGHTED_PATH) != 0)
					SkipListString(data, ref cursor);

				if ((listRequestFlags & QUERY2_REQUEST_HIGHLIGHTED_FULL_PATH_AND_NAME) != 0)
					SkipListString(data, ref cursor);
				if (full.Length == 0)
				{
					if (name.Length == 0 || path.Length == 0)
						throw new InvalidOperationException("Everything reply failed validation.");
					full = path.EndsWith('\\') ? path + name : path + '\\' + name;
				}
				// A stray row from another root would silently corrupt the tree.
				if (guardPrefix && !full.StartsWith(root, StringComparison.OrdinalIgnoreCase))
					continue;
				bool isFolder = (flags & EVERYTHING_IPC_ITEM_IS_FOLDER) != 0;
				if (isFolder)
				{
					// Server contract: folder rows carry no trailing backslash. Only trim
					// when actually present to avoid the TrimEnd call per folder.
					// Folder-size index (1.5a): SIZE holds the aggregated folder size when
					// indexed — keep it so folder-only scans can skip file rows entirely.
					if (full.Length > 0 && full[^1] == '\\')
						full = full.TrimEnd('\\');
					long folderSize = Math.Max(0, size);
					rows.Add(new ScanItem
					{
						FullPath = full,
						Size = folderSize,
						AllocatedSize = folderSize,
						Modified = rowOptions.IncludeDates ? DiskCluster.FromFileTimeUtcOrMinValue(modified) : DateTime.MinValue,
						IsFolder = true
					});
				}
				else
				{
					long fileSize = Math.Max(0, size);
					rows.Add(new ScanItem
					{
						FullPath = full,
						Size = fileSize,
						AllocatedSize = DiskCluster.AlignSize(fileSize, rowOptions.Cluster, rowOptions.ClusterIsPowerOfTwo),
						Modified = rowOptions.IncludeDates ? DiskCluster.FromFileTimeUtcOrMinValue(modified) : DateTime.MinValue,
						IsFolder = false,
						Extension = Path.GetExtension(full)
					});
				}
			}
			return (int)n;
		}

		private static string ReadListString(ReadOnlySpan<byte> data, ref int cursor)
		{
			if (cursor + 4 > data.Length)
				throw new InvalidOperationException("Everything reply truncated.");
			uint len = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(cursor, 4));
			if (len > MAX_STRING_CHARS)
				throw new InvalidOperationException("Everything reply has invalid string extents.");
			cursor += 4;
			long byteLen = ((long)len + 1) * 2;
			if ((long)cursor + byteLen > data.Length)
				throw new InvalidOperationException("Everything reply strings out of range.");
			string s = len == 0 ? string.Empty : Encoding.Unicode.GetString(data.Slice(cursor, (int)len * 2));
			cursor += (int)byteLen;
			return s;
		}

		private static void SkipListString(ReadOnlySpan<byte> data, ref int cursor)
		{
			if (cursor + 4 > data.Length)
				throw new InvalidOperationException("Everything reply truncated.");
			uint len = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(cursor, 4));
			if (len > MAX_STRING_CHARS)
				throw new InvalidOperationException("Everything reply has invalid string extents.");
			cursor += 4;
			long byteLen = ((long)len + 1) * 2;
			if ((long)cursor + byteLen > data.Length)
				throw new InvalidOperationException("Everything reply strings out of range.");
			cursor += (int)byteLen;
		}

		private static long ReadInt64(ReadOnlySpan<byte> data, ref int cursor)
		{
			if (cursor + 8 > data.Length)
				throw new InvalidOperationException("Everything reply truncated.");
			long v = BinaryPrimitives.ReadInt64LittleEndian(data.Slice(cursor, 8));
			cursor += 8;
			return v;
		}

		private static void SkipBytes(ReadOnlySpan<byte> data, ref int cursor, int count)
		{
			if (cursor + count > data.Length)
				throw new InvalidOperationException("Everything reply truncated.");
			cursor += count;
		}

		public void Dispose()
		{
			if (_disposed)
				return;
			_disposed = true;
			unsafe
			{ lock (_lock) _active.Remove((nint)_hwnd.Value); }
			if (_hwnd != HWND.Null)
			{
				PInvoke.DestroyWindow(_hwnd);
				PInvoke.PostThreadMessage(_threadId, PInvoke.WM_QUIT, default, default);
			}
			_reply.Dispose();
		}
	}
}
