using AutoOS.App.Data.Models;

namespace AutoOS.App.Extensions;

public static class NodeCollectionExtensions
{
	public static void InsertOrdered<T>(this ObservableCollection<T> children, T node) where T : IOrderedNode
	{
		int index = 0;
		while (index < children.Count && children[index].Order <= node.Order)
			index++;
		children.Insert(index, node);
	}
}
