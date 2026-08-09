using AutoOS.App.Data.Enums.Power;
using AutoOS.App.Data.Models.Power;
using Syncfusion.UI.Xaml.TreeGrid;

namespace AutoOS.App.Data.TemplateSelectors.Power;

public sealed partial class CellStyleSelector : StyleSelector
{
	public Style? CriticalStyle { get; set; }

	public Style? SuccessStyle { get; set; }

	protected override Style? SelectStyleCore(object item, DependencyObject container)
	{
		if (item is not Node node || container is not TreeGridCell cell)
			return null;

		string? mappingName = cell.ColumnBase?.TreeGridColumn?.MappingName;
		bool isAc = mappingName is nameof(Node.DisplayAc) or nameof(Node.DisplayCompareAc) or nameof(Node.DisplayOriginalAc);
		bool isDc = mappingName is nameof(Node.DisplayDc) or nameof(Node.DisplayCompareDc) or nameof(Node.DisplayOriginalDc);
		if (!isAc && !isDc)
			return null;

		bool isDifferent = isAc ? node.IsAcDifferent : node.IsDcDifferent;
		if (!isDifferent)
			return null;

		if (mappingName is nameof(Node.DisplayCompareAc) or nameof(Node.DisplayCompareDc))
			return SuccessStyle;
		if (mappingName is nameof(Node.DisplayOriginalAc) or nameof(Node.DisplayOriginalDc))
			return CriticalStyle;

		return node.Mode switch
		{
			PageMode.Comparison => CriticalStyle,
			PageMode.ViewChanges => SuccessStyle,
			_ => null
		};
	}
}
