using System.Reflection;
using AutoOS.App.Data.Enums.Bios;
using AutoOS.App.Data.Models.Bios;

namespace AutoOS.App.Helpers.TreeGrid;

public sealed class TreeGridSortComparer : IComparer<object>
{
	private PropertyInfo? _propertyInfo;

	public string PropertyName { get; set; } = string.Empty;

	public int Compare(object? x, object? y)
	{
		if (x is not Node node1 || y is not Node node2)
			return 0;

		if (node1.NodeKind == NodeKind.Root && node2.NodeKind == NodeKind.Root)
			return 0;

		if (node1.NodeKind == NodeKind.Root)
			return -1;
		if (node2.NodeKind == NodeKind.Root)
			return 1;

		string val1 = GetValue(node1);
		string val2 = GetValue(node2);

		return string.Compare(val1, val2, StringComparison.OrdinalIgnoreCase);
	}

	private string GetValue(Node node)
	{
		_propertyInfo ??= typeof(Node).GetProperty(PropertyName);
		return _propertyInfo?.GetValue(node)?.ToString() ?? string.Empty;
	}
}
