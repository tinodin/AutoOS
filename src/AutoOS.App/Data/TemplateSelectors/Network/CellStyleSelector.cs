using AutoOS.App.Data.Enums.Network;
using AutoOS.App.Data.Models.Network;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Syncfusion.UI.Xaml.TreeGrid;

namespace AutoOS.App.Data.TemplateSelectors.Network;

public sealed partial class CellStyleSelector : StyleSelector
{
	public Style? CriticalStyle { get; set; }
	public Style? CautionStyle { get; set; }
	public Style? SuccessStyle { get; set; }

	protected override Style? SelectStyleCore(object item, DependencyObject container)
	{
		if (item is not Node node || node.NodeKind == NodeKind.Adapter)
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
