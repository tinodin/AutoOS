using System.Collections.ObjectModel;
using System.ComponentModel;

namespace AutoOS.Core.Services.DiskAnalyzer;

public enum ScannerKind
{
	Everything,  // Everything IPC (fastest, requires Everything service)
	Enumeration  // Win32 FindFirstFile enumeration (universal fallback)
}

public sealed class ScanOptions
{
	public string RootPath { get; set; } = "C:\\";
	public ScannerKind PreferredScanner { get; set; } = ScannerKind.Everything;
	public string SearchFilter { get; set; } = string.Empty; // Everything search syntax or wildcard
	public bool IncludeModifiedDates { get; set; } = true;
}

public record struct ScanItem
{
	public ScanItem()
	{
		FullPath = string.Empty;
		Attributes = string.Empty;
		Extension = string.Empty;
	}

	public required string FullPath { get; init; }
	public long Size { get; init; }              // logical size
	public long AllocatedSize { get; set; }      // size on disk (cluster aligned)
	public DateTime Modified { get; set; }       // UTC; converted to local only for visible rows
	public bool IsFolder { get; init; }
	public string Attributes { get; set; }       // WizTree-style: e.g. "R", "HS", "A" (null/empty = none)
	public long FileId { get; init; }            // MFT record number for FRN-once sums; 0 = count every row
	public string Extension { get; init; } = string.Empty; // Scanner-provided extension; treemap normalizes before coloring.
	// Lite-row fast path (MFT scans): parent MFT record id + bare file name. The tree
	// resolves the parent node by id (no path hash) and no file path string is ever
	// built for the ~95% of rows that never display. FullPath stays empty; the File View
	// materializes paths lazily for listed rows only. 0 = classic path rows (no MFT
	// record is ever 0 — system records live at 0-15 and are never file parents).
	public long ParentId { get; init; }
	public string? Name { get; init; }
}

public sealed class ScanResult
{
	public required string RootPath { get; init; }
	public required ScannerKind UsedScanner { get; init; }
	public bool UsedFallback { get; init; }
	public TimeSpan Duration { get; init; }
	public long TotalSize { get; init; }
	public long TotalAllocated { get; init; }
	public int FileCount { get; init; }
	public int FolderCount { get; init; }
	public IReadOnlyList<DiskNode> Roots { get; init; } = [];
	// File rows and the extension breakdown are aggregated inside the scan, on the same background
	// pass that builds the tree, so opening File View never waits for a second walk of the drive.
	public FileViewInputs FileView { get; init; } = FileViewInputs.Empty;
	// Treemap file tiles, grouped by parent folder during the tree build itself, so the
	// treemap can paint folders and files in a single pass the moment the scan binds.
	// Lite rows carry no path strings — only names — so this stays compact.
	public Dictionary<DiskNode, List<TreemapFile>> TreemapFileGroups { get; init; } = new();
	// MFT record id → folder node, for treemap file grouping. Null on classic scans
	// (IPC/enumeration rows resolve parents by path instead).
	public IReadOnlyDictionary<long, DiskNode>? RecordNodes { get; init; }
}

/// <summary>
/// One treemap file tile: the bare file name plus a normalized extension. The parent folder
/// is the dictionary key in <see cref="ScanResult.TreemapFileGroups"/>, so no path string
/// is ever built for the ~95% of rows that never display.
/// </summary>
public readonly record struct TreemapFile(string Name, string Extension, long AllocatedSize, long Size, bool IsAggregate = false);

/// <summary>
/// Hierarchical node for SfTreeGrid. WizTree-like: Name, Size, Allocated, Percent, Items, Modified.
/// </summary>
public sealed class DiskNode : INotifyPropertyChanged
{
	private bool _isExpanded;
	private string? _extensionCache;
	private long _size;
	private long _allocated;
	private int _fileCount;
	private int _folderCount;
	private double _percentOfParent;
	private double _percentOfTotal;
	private DateTime _modified;
	private string _attributes = string.Empty;

	// Name/FullPath were `required` until the treemap exposed DiskNode-typed dependency
	// properties: the XAML compiler emits a parameterless activator for such types, which
	// `required` forbids (CS9035). Every construction site still sets both.
	public string Name { get; init; } = string.Empty;
	public string FullPath { get; init; } = string.Empty;
	public bool IsFolder { get; init; }

	public string Extension
	{
		get
		{
			if (IsFolder)
				return string.Empty;

			_extensionCache ??= Path.GetExtension(Name);

			return _extensionCache;
		}
	}

	public long Size
	{
		get => _size;
		set
		{
			if (_size != value)
			{
				_size = value;
				OnPropertyChanged();
				OnPropertyChanged(nameof(SizeText));
			}
		}
	}

	public long Allocated
	{
		get => _allocated;
		set
		{
			if (_allocated != value)
			{
				_allocated = value;
				OnPropertyChanged();
				OnPropertyChanged(nameof(AllocatedText));
			}
		}
	}

	public int FileCount
	{
		get => _fileCount;
		set
		{
			if (_fileCount != value)
			{
				_fileCount = value;
				OnPropertyChanged();
				OnPropertyChanged(nameof(FilesText));
				OnPropertyChanged(nameof(ItemsText));
				OnPropertyChanged(nameof(ItemsCount));
			}
		}
	}

	public int FolderCount
	{
		get => _folderCount;
		set
		{
			if (_folderCount != value)
			{
				_folderCount = value;
				OnPropertyChanged();
				OnPropertyChanged(nameof(FoldersText));
				OnPropertyChanged(nameof(ItemsText));
				OnPropertyChanged(nameof(ItemsCount));
			}
		}
	}

	public DateTime Modified
	{
		get => _modified;
		set
		{
			if (_modified != value)
			{
				_modified = value;
				OnPropertyChanged();
			}
		}
	}

	public string Attributes
	{
		get => _attributes;
		set
		{
			if (_attributes != value)
			{
				_attributes = value ?? string.Empty;
				OnPropertyChanged();
			}
		}
	}

	// Percent relative to parent â€” computed after tree build
	public double PercentOfParent
	{
		get => _percentOfParent;
		set
		{
			if (_percentOfParent != value)
			{
				_percentOfParent = value;
				OnPropertyChanged();
				OnPropertyChanged(nameof(PercentText));
				OnPropertyChanged(nameof(PercentOfParentText));
			}
		}
	}

	public double PercentOfTotal
	{
		get => _percentOfTotal;
		set
		{
			if (_percentOfTotal != value)
			{
				_percentOfTotal = value;
				OnPropertyChanged();
				OnPropertyChanged(nameof(PercentOfTotalText));
			}
		}
	}

	public ObservableCollection<DiskNode> Children { get; } = [];

	public bool IsExpanded
	{
		get => _isExpanded;
		set
		{
			if (_isExpanded != value)
			{
				_isExpanded = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsExpanded)));
			}
		}
	}

	// Display helpers for TreeGrid (WizTree Tree View columns:
	// Folder | % of Parent | Size | Allocated | Items | Files | Folders | Modified | Attributes)
	public string SizeText => FormatBytes(Size);
	public string AllocatedText => FormatBytes(Allocated);
	public string PercentText => $"{PercentOfParent:F1}%";
	public string PercentOfParentText => $"{PercentOfParent:F1}%";
	public string PercentOfTotalText => $"{PercentOfTotal:F1}%";
	public int ItemsCount => FolderCount + FileCount;
	public string ItemsText => $"{ItemsCount:N0}";
	public string FilesText => $"{FileCount:N0}";
	public string FoldersText => $"{FolderCount:N0}";

	public static string FormatBytes(long bytes)
	{
		// WizTree format: 1 decimal for KB/MB/GB/TB ("15,7 MB"), integer + " Bytes" below 1KB.
		if (bytes < 1024)
			return $"{bytes:N0} Bytes";
		double v = bytes;
		int i = 0;
		string[] units = ["Bytes", "KB", "MB", "GB", "TB", "PB"];
		while (v >= 1024 && i < units.Length - 1)
		{
			v /= 1024;
			i++;
		}
		return $"{v:N1} {units[i]}";
	}

	// WizTree shows concatenated flags without Directory/ReparsePoint bits.
	// Pre-formatted per bit combination: 500k rows reuse ~8 strings instead of one
	// allocation each, which is what made the parallel walker GC-bound.
	private static readonly (FileAttributes Flag, char Letter)[] AttributeFlags =
	[
		(FileAttributes.ReadOnly, 'R'),
		(FileAttributes.Hidden, 'H'),
		(FileAttributes.System, 'S'),
		(FileAttributes.Archive, 'A'),
		(FileAttributes.Compressed, 'C'),
		(FileAttributes.Encrypted, 'E'),
		(FileAttributes.Temporary, 'T')
	];

	private static readonly string[] FormattedAttributes = CreateFormattedAttributes();

	private static string[] CreateFormattedAttributes()
	{
		var formatted = new string[1 << AttributeFlags.Length];
		for (int code = 0; code < formatted.Length; code++)
			formatted[code] = BuildAttributes(code);

		return formatted;
	}

	private static string BuildAttributes(int code)
	{
		Span<char> buffer = stackalloc char[AttributeFlags.Length];
		int length = 0;
		for (int index = 0; index < AttributeFlags.Length; index++)
			if ((code & (1 << index)) != 0)
				buffer[length++] = AttributeFlags[index].Letter;

		return length == 0 ? string.Empty : new string(buffer.Slice(0, length));
	}

	public static string FormatAttributes(FileAttributes attributes)
	{
		int code = 0;
		for (int index = 0; index < AttributeFlags.Length; index++)
			if ((attributes & AttributeFlags[index].Flag) != 0)
				code |= 1 << index;

		return FormattedAttributes[code];
	}

	public event PropertyChangedEventHandler? PropertyChanged;

	private void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? name = null)
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
	}
}

/// <summary>
/// Flat file row for WizTree-style File View (lazy-loaded on tab select).
/// Columns: File Name | Path | % of Drive | Size | Allocated | Modified | Dup Count | Dup Size | Attributes
/// </summary>
public sealed class DiskFileRow
{
	// Empty child collection so SfTreeGrid (ChildPropertyName="Children") treats rows as leaves.
	public System.Collections.ObjectModel.ObservableCollection<DiskFileRow> Children { get; } = [];

	public required string FileName { get; init; }

	public required string Directory { get; set; }

	public required string FullPath { get; set; }

	public string Extension { get; init; } = string.Empty;

	// Lite-row carry-over: MFT parent record id so post-tree path fill can materialize
	// FullPath/Directory for listed rows only. 0 = classic rows (paths already set).
	public long ParentId { get; init; }

	public long Size { get; set; }

	public long Allocated { get; set; }

	public DateTime Modified { get; set; }

	public string Attributes { get; set; } = string.Empty;

	public double PercentOfDrive { get; set; }

	public int DupCount { get; set; }

	public long DupSize { get; set; }

	public string SizeText => DiskNode.FormatBytes(Size);

	public string AllocatedText => DiskNode.FormatBytes(Allocated);

	public string PercentText => $"{PercentOfDrive:F1}%";

	public string DupSizeText => DupCount > 1 ? DiskNode.FormatBytes(DupSize) : string.Empty;

	public string DupCountText => DupCount > 1 ? $"{DupCount:N0}" : string.Empty;

	/// <summary>
	/// WizTree default ordering: allocated size descending, logical size as tie-breaker.
	/// </summary>
	public static int CompareByAllocatedDescending(DiskFileRow left, DiskFileRow right)
	{
		int compare = right.Allocated.CompareTo(left.Allocated);
		return compare != 0 ? compare : right.Size.CompareTo(left.Size);
	}
}

/// <summary>
/// File View inputs produced in one background pass by
/// <see cref="DiskAnalyzerService.BuildFileViewInputs"/>. Both lists ship in the scan result, so
/// the File View renders the moment the scan finishes and the Folders / Duplicates only toggles
/// only pick from pre-built rows.
/// </summary>
public sealed record FileViewInputs(IReadOnlyList<DiskFileRow> TopFiles, IReadOnlyList<DiskExtensionStat> Extensions)
{
	public static FileViewInputs Empty { get; } = new([], []);
}

/// <summary>
/// Extension aggregate for WizTree-style right panel.
/// Columns: Extension | File Type | Percent | Size | Allocated | Files
/// </summary>
public sealed class DiskExtensionStat
{
	// Empty child collection so SfTreeGrid (ChildPropertyName="Children") treats rows as leaves.
	public System.Collections.ObjectModel.ObservableCollection<DiskExtensionStat> Children { get; } = [];

	public required string Extension { get; init; } // includes dot, or "(No Extension)"

	public string FileType { get; init; } = string.Empty;

	public long Size { get; set; }

	public long Allocated { get; set; }

	public int Files { get; set; }

	public double Percent { get; set; }

	public string SizeText => DiskNode.FormatBytes(Size);

	public string AllocatedText => DiskNode.FormatBytes(Allocated);

	public string PercentText => $"{Percent:F1}%";

	public string FilesText => $"{Files:N0}";

	public static string GetFileType(string extension)
	{
		if (string.IsNullOrEmpty(extension))
			return "(No Extension)";
		return extension.ToLowerInvariant() switch
		{
			".dll" => "Application extension",
			".sys" => "System file",
			".exe" => "Application",
			".7z" => "ArchiveFolder",
			".jar" => "Executable Jar File",
			".vhdx" or ".vhd" => "Windows Vhd File",
			".lib" => "Object File Library",
			".pdf" => "Firefox PDF Document",
			".msi" => "Windows Installer",
			".vsix" => "Microsoft Visual Studio Extension",
			".dat" => "DAT File",
			".cab" => "Cabinet File",
			".pdb" => "Program Debug Database",
			".nupkg" => "ArchiveFolder",
			".xml" => "Microsoft Edge HTML Document",
			".dmp" => "Memory Dump File",
			".pak" => "PAK File",
			".js" => "JSFile",
			".wim" => "WIM File",
			".zip" => "Compressed (zipped) Folder",
			".node" => "NODE File",
			".png" => "PNG File",
			".jpg" or ".jpeg" => "JPEG File",
			".mp4" => "MP4 Video",
			".iso" => "Disc Image File",
			_ => $"{extension.ToUpperInvariant().TrimStart('.')} File"
		};
	}
}
