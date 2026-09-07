using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace AutoOS.App.Helpers.Xaml;

public static class DependencyObjectHelpers
{
	public static T? FindChild<T>(DependencyObject startNode) where T : DependencyObject
	{
		int count = VisualTreeHelper.GetChildrenCount(startNode);
		for (int i = 0; i < count; i++)
		{
			DependencyObject current = VisualTreeHelper.GetChild(startNode, i);
			if (current.GetType().Equals(typeof(T)) || current.GetType().IsSubclassOf(typeof(T)))
			{
				var asType = (T)current;
				return asType;
			}
			T? retVal = FindChild<T>(current);
			if (retVal is not null)
			{
				return retVal;
			}
		}
		return null;
	}

	public static T? FindChild<T>(DependencyObject startNode, Func<T, bool> predicate) where T : DependencyObject
	{
		int count = VisualTreeHelper.GetChildrenCount(startNode);
		for (int i = 0; i < count; i++)
		{
			DependencyObject current = VisualTreeHelper.GetChild(startNode, i);
			if (current.GetType().Equals(typeof(T)) || current.GetType().IsSubclassOf(typeof(T)))
			{
				var asType = (T)current;
				if (predicate(asType))
				{
					return asType;
				}
			}
			T? retVal = FindChild<T>(current, predicate);
			if (retVal is not null)
			{
				return retVal;
			}
		}
		return null;
	}

	public static IEnumerable<T> FindChildren<T>(DependencyObject startNode) where T : DependencyObject
	{
		int count = VisualTreeHelper.GetChildrenCount(startNode);
		for (int i = 0; i < count; i++)
		{
			DependencyObject current = VisualTreeHelper.GetChild(startNode, i);
			if (current.GetType().Equals(typeof(T)) || (current.GetType().IsSubclassOf(typeof(T))))
			{
				var asType = (T)current;
				yield return asType;
			}
			foreach (T item in FindChildren<T>(current))
			{
				yield return item;
			}
		}
	}

	public static T? FindParent<T>(DependencyObject? child) where T : DependencyObject
	{
		T? parent = null;
		if (child is null)
		{
			return parent;
		}
		DependencyObject? currentParent = VisualTreeHelper.GetParent(child);
		while (currentParent is not null)
		{
			if (currentParent is T matchedParent)
			{
				parent = matchedParent;
				break;
			}
			currentParent = VisualTreeHelper.GetParent(currentParent);
		}
		return parent;
	}
}
