using AutoOS.App.Data.Enums.Bios;
using AutoOS.App.Data.Models.Bios;
using Syncfusion.UI.Xaml.TreeGrid;

namespace AutoOS.App.Data.TemplateSelectors.Bios;

public sealed partial class CellStyleSelector : StyleSelector
{
	public Style CriticalStyle { get; set; }
	public Style SuccessStyle { get; set; }
	public Style CautionStyle { get; set; }
	public bool IsDiff { get; set; }

	protected override Style SelectStyleCore(object item, DependencyObject container)
	{
		if (item is not BiosTreeNode node) return null;

		if (container is TreeGridCell cell)
		{
			TreeGridColumn? column = cell.ColumnBase?.TreeGridColumn;
			string mappingName = column.MappingName;

			if (node.HasErrors && mappingName == "DisplayCurrent")
				return CautionStyle;

			if (IsDiff)
			{
				if (mappingName == "DisplayOriginal")
				{
					if (node.NodeKind == NodeKind.Leaf)
						return node.IsModified ? CriticalStyle : null;
					
					if (node.NodeKind == NodeKind.Group)
					{
						IEnumerable<BiosTreeNode> leaves = node.GetLeaves();
						return leaves.All(leaf => leaf.IsModified) ? CriticalStyle : null;
					}
				}

				if (mappingName == "DisplayCurrent" && !node.HasErrors)
				{
					if (node.NodeKind == NodeKind.Leaf)
						return node.IsModified ? SuccessStyle : null;
					
					if (node.NodeKind == NodeKind.Group)
					{
						IEnumerable<BiosTreeNode> leaves = node.GetLeaves();
						if (leaves.All(leaf => leaf.IsModified))
						{
							return leaves.Any(leaf => leaf.HasErrors) ? CriticalStyle : SuccessStyle;
						}
					}
				}
			}
			else
			{
				if (!node.HasPendingRecommendation)
					return null;

				if (mappingName == "DisplayCurrent")
					return CriticalStyle;
				if (mappingName == "DisplayRecommended")
					return SuccessStyle;
			}
		}

		return null;
	}
}
