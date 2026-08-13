using AutoOS.App.Data.Models.Power;
using Syncfusion.UI.Xaml.TreeGrid;

namespace AutoOS.App.Data.TemplateSelectors.Power;

public sealed partial class CompareCellStyleSelector : StyleSelector
{
	public Style? CriticalStyle { get; set; }

	public Style? SuccessStyle { get; set; }

	protected override Style? SelectStyleCore(object item, DependencyObject container)
	{
		if (item is not Node node || container is not TreeGridCell cell)
			return null;

		string? mappingName = cell.ColumnBase?.TreeGridColumn?.MappingName;
		bool isAc = mappingName is nameof(Node.DisplayAc) or nameof(Node.DisplayCompareAc);
		bool isDc = mappingName is nameof(Node.DisplayDc) or nameof(Node.DisplayCompareDc);
		if (!isAc && !isDc)
			return null;

		if ((isAc && !node.IsAcDifferent) || (isDc && !node.IsDcDifferent))
			return null;

		if (mappingName is nameof(Node.DisplayCompareAc) or nameof(Node.DisplayCompareDc))
			return SuccessStyle;

		return CriticalStyle;
	}
}

public sealed partial class ChangesCellStyleSelector : StyleSelector
{
	public Style? CriticalStyle { get; set; }

	public Style? SuccessStyle { get; set; }

	protected override Style? SelectStyleCore(object item, DependencyObject container)
	{
		if (item is not Node node || container is not TreeGridCell cell)
			return null;

		string? mappingName = cell.ColumnBase?.TreeGridColumn?.MappingName;
		bool isAc = mappingName is nameof(Node.DisplayAc) or nameof(Node.DisplayOriginalAc);
		bool isDc = mappingName is nameof(Node.DisplayDc) or nameof(Node.DisplayOriginalDc);
		if (!isAc && !isDc)
			return null;

		if ((isAc && !node.IsAcModified) || (isDc && !node.IsDcModified))
			return null;

		if (mappingName is nameof(Node.DisplayOriginalAc) or nameof(Node.DisplayOriginalDc))
			return CriticalStyle;

		return SuccessStyle;
	}
}