using System.Reflection;
using AutoOS.Core.Services.DiskAnalyzer;

namespace AutoOS.App.Helpers.TreeGrid;

public sealed class TreeGridSortComparer : IComparer<object>
{
	private readonly Dictionary<Type, PropertyInfo?> _propertyCache = new();
	private readonly Dictionary<Type, PropertyInfo?> _nodeKindCache = new();

	public string PropertyName { get; set; } = string.Empty;

	public int Compare(object? x, object? y)
	{
		if (ReferenceEquals(x, y))
			return 0;

		if (x == null)
			return -1;

		if (y == null)
			return 1;

		// Fast paths for disk-analyzer rows: direct member access with no reflection,
		// no boxing, no per-comparison ToString. Sorting 100k folder nodes issues ~2M
		// comparisons — the old reflective path (GetValue + box + interface dispatch per
		// call) dominated scan-complete bind time. These types have no NodeKind concept,
		// so the root-pinning below never applied to them anyway.
		if (x is DiskNode a && y is DiskNode b)
			return CompareDiskNode(a, b, PropertyName);
		if (x is DiskFileRow fa && y is DiskFileRow fb)
			return CompareFileRow(fa, fb, PropertyName);
		if (x is DiskExtensionStat ea && y is DiskExtensionStat eb)
			return CompareExtensionStat(ea, eb, PropertyName);

		// Handle root nodes specially (e.g., Bios "Recommended" root should stay first)
		string? kind1 = GetNodeKind(x);
		string? kind2 = GetNodeKind(y);
		if (kind1 == "Root" && kind2 == "Root")
			return 0;

		if (kind1 == "Root")
			return -1;

		if (kind2 == "Root")
			return 1;

		return CompareValues(GetValue(x), GetValue(y));
	}

	private int CompareDiskNode(DiskNode a, DiskNode b, string property) => property switch
	{
		nameof(DiskNode.Name) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase),
		nameof(DiskNode.PercentOfParent) => a.PercentOfParent.CompareTo(b.PercentOfParent),
		nameof(DiskNode.Size) => a.Size.CompareTo(b.Size),
		nameof(DiskNode.Allocated) => a.Allocated.CompareTo(b.Allocated),
		nameof(DiskNode.ItemsCount) => a.ItemsCount.CompareTo(b.ItemsCount),
		nameof(DiskNode.FileCount) => a.FileCount.CompareTo(b.FileCount),
		nameof(DiskNode.FolderCount) => a.FolderCount.CompareTo(b.FolderCount),
		nameof(DiskNode.Modified) => a.Modified.CompareTo(b.Modified),
		nameof(DiskNode.Attributes) => string.Compare(a.Attributes, b.Attributes, StringComparison.OrdinalIgnoreCase),
		_ => CompareValues(GetValue(a), GetValue(b)),
	};

	private int CompareFileRow(DiskFileRow a, DiskFileRow b, string property) => property switch
	{
		nameof(DiskFileRow.FileName) => string.Compare(a.FileName, b.FileName, StringComparison.OrdinalIgnoreCase),
		nameof(DiskFileRow.Directory) => string.Compare(a.Directory, b.Directory, StringComparison.OrdinalIgnoreCase),
		nameof(DiskFileRow.PercentOfDrive) => a.PercentOfDrive.CompareTo(b.PercentOfDrive),
		nameof(DiskFileRow.Size) => a.Size.CompareTo(b.Size),
		nameof(DiskFileRow.Allocated) => a.Allocated.CompareTo(b.Allocated),
		nameof(DiskFileRow.Modified) => a.Modified.CompareTo(b.Modified),
		nameof(DiskFileRow.DupCount) => a.DupCount.CompareTo(b.DupCount),
		nameof(DiskFileRow.DupSize) => a.DupSize.CompareTo(b.DupSize),
		nameof(DiskFileRow.Extension) => string.Compare(a.Extension, b.Extension, StringComparison.OrdinalIgnoreCase),
		nameof(DiskFileRow.Attributes) => string.Compare(a.Attributes, b.Attributes, StringComparison.OrdinalIgnoreCase),
		_ => CompareValues(GetValue(a), GetValue(b)),
	};

	private int CompareExtensionStat(DiskExtensionStat a, DiskExtensionStat b, string property) => property switch
	{
		nameof(DiskExtensionStat.Extension) => string.Compare(a.Extension, b.Extension, StringComparison.OrdinalIgnoreCase),
		nameof(DiskExtensionStat.FileType) => string.Compare(a.FileType, b.FileType, StringComparison.OrdinalIgnoreCase),
		nameof(DiskExtensionStat.Percent) => a.Percent.CompareTo(b.Percent),
		nameof(DiskExtensionStat.Size) => a.Size.CompareTo(b.Size),
		nameof(DiskExtensionStat.Allocated) => a.Allocated.CompareTo(b.Allocated),
		nameof(DiskExtensionStat.Files) => a.Files.CompareTo(b.Files),
		_ => CompareValues(GetValue(a), GetValue(b)),
	};

	private string? GetNodeKind(object node)
	{
		Type type = node.GetType();
		if (!_nodeKindCache.TryGetValue(type, out PropertyInfo? property))
		{
			property = type.GetProperty("NodeKind");
			_nodeKindCache[type] = property;
		}

		return property?.GetValue(node)?.ToString();
	}

	private object? GetValue(object node)
	{
		Type type = node.GetType();
		if (!_propertyCache.TryGetValue(type, out PropertyInfo? property))
		{
			property = type.GetProperty(PropertyName);
			_propertyCache[type] = property;
		}

		return property?.GetValue(node);
	}

	private static int CompareValues(object? value1, object? value2)
	{
		if (ReferenceEquals(value1, value2))
			return 0;

		if (value1 == null)
			return -1;

		if (value2 == null)
			return 1;

		if (value1 is string text1 && value2 is string text2)
			return string.Compare(text1, text2, StringComparison.OrdinalIgnoreCase);

		if (value1.GetType() == value2.GetType() && value1 is IComparable comparable)
			return comparable.CompareTo(value2);

		return string.Compare(value1.ToString(), value2.ToString(), StringComparison.OrdinalIgnoreCase);
	}
}
