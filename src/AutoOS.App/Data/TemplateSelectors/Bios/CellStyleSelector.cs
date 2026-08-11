using AutoOS.App.Data.Enums.Bios;
using AutoOS.App.Data.Models.Bios;
using Syncfusion.UI.Xaml.TreeGrid;

namespace AutoOS.App.Data.TemplateSelectors.Bios;

public sealed partial class CellStyleSelector : StyleSelector
{
	public Style CriticalStyle { get; set; } = null!;
	public Style SuccessStyle { get; set; } = null!;
	public Style CautionStyle { get; set; } = null!;
	public bool IsDiff { get; set; }

	protected override Style? SelectStyleCore(object item, DependencyObject container)
	{
		if (item is not Node node)
			return null;

		if (container is TreeGridCell cell)
		{
			TreeGridColumn? column = cell.ColumnBase?.TreeGridColumn;
			string mappingName = column!.MappingName;

			if (node.HasErrors && mappingName == "DisplayCurrent")
				return CautionStyle;

			if (IsDiff)
			{
				if (mappingName == "DisplayOriginal")
				{
					if (node.NodeKind == NodeKind.Setting)
						return node.IsModified ? CriticalStyle : null;

					if (node.NodeKind == NodeKind.GroupedSetting)
						return node.GetLeaves().All(leaf => leaf.IsModified) ? CriticalStyle : null;
				}

				if (mappingName == "DisplayCurrent")
				{
					if (node.NodeKind == NodeKind.Setting)
						return node.IsModified ? SuccessStyle : null;

					if (node.NodeKind == NodeKind.GroupedSetting && node.GetLeaves().All(leaf => leaf.IsModified))
						return SuccessStyle;
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
