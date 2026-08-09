using System.Reflection;
using AutoOS.App.Data.Models.Bios;

namespace AutoOS.App.Helpers.TreeGrid;

public class TreeGridSortComparer : IComparer<object>
{
    public string PropertyName { get; set; } = string.Empty;

    public int Compare(object? x, object? y)
    {
        if (x is not Node node1 || y is not Node node2)
            return 0;

        if (node1.IsRoot && node2.IsRoot)
            return 0;

        if (node1.IsRoot) return -1;
        if (node2.IsRoot) return 1;

		PropertyInfo? prop = typeof(Node).GetProperty(PropertyName);
		string val1 = prop?.GetValue(node1)?.ToString() ?? string.Empty;
		string val2 = prop?.GetValue(node2)?.ToString() ?? string.Empty;

        return string.Compare(val1, val2, StringComparison.OrdinalIgnoreCase);
    }
}
