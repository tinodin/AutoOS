using System.Reflection;

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
