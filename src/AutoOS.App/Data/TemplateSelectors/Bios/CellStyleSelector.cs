using AutoOS.App.Data.Enums.Bios;
using AutoOS.App.Data.Models.Bios;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Syncfusion.UI.Xaml.TreeGrid;

namespace AutoOS.App.Data.TemplateSelectors.Bios;

public sealed partial class CellStyleSelector : StyleSelector
{
	public Style? CriticalStyle { get; set; }
	public Style? SuccessStyle { get; set; }
	public Style? CautionStyle { get; set; }

	protected override Style? SelectStyleCore(object item, DependencyObject container)
	{
		if (item is not Node node || node.NodeKind is NodeKind.Root or NodeKind.Path)
			return null;

		if (container is not TreeGridCell cell)
			return null;

		string mappingName = cell.ColumnBase?.TreeGridColumn?.MappingName ?? string.Empty;

		if (node.HasErrors && mappingName == nameof(Node.DisplayCurrent))
			return CautionStyle;

		if (!node.HasPendingRecommendation)
			return null;

		if (mappingName == nameof(Node.DisplayCurrent))
			return CriticalStyle;
		if (mappingName == nameof(Node.DisplayRecommended))
			return SuccessStyle;

		return null;
	}
}

public sealed partial class CompareCellStyleSelector : StyleSelector
{
	public Style? CriticalStyle { get; set; }
	public Style? CautionStyle { get; set; }
	public Style? SuccessStyle { get; set; }

	protected override Style? SelectStyleCore(object item, DependencyObject container)
	{
		if (item is not Node node || node.NodeKind is NodeKind.Root or NodeKind.Path)
			return null;

		if (container is not TreeGridCell cell)
			return null;

		string mappingName = cell.ColumnBase?.TreeGridColumn?.MappingName ?? string.Empty;

		if (node.HasErrors && mappingName == nameof(Node.DisplayCurrent))
			return CautionStyle;

		if (mappingName == nameof(Node.DisplayCurrent))
			return SuccessStyle;

		if (mappingName == nameof(Node.DisplayDefault))
			return CriticalStyle;

		return null;
	}
}

public sealed partial class ChangesCellStyleSelector : StyleSelector
{
	public Style? CriticalStyle { get; set; }
	public Style? CautionStyle { get; set; }
	public Style? SuccessStyle { get; set; }

	protected override Style? SelectStyleCore(object item, DependencyObject container)
	{
		if (item is not Node node || node.NodeKind is NodeKind.Root or NodeKind.Path)
			return null;

		if (container is not TreeGridCell cell)
			return null;

		string mappingName = cell.ColumnBase?.TreeGridColumn?.MappingName ?? string.Empty;

		if (node.HasErrors && mappingName == nameof(Node.DisplayCurrent))
			return CautionStyle;

		if (!node.IsModified)
			return null;

		if (mappingName == nameof(Node.DisplayOriginal))
			return CriticalStyle;

		if (mappingName == nameof(Node.DisplayCurrent))
			return SuccessStyle;

		return null;
	}
}
