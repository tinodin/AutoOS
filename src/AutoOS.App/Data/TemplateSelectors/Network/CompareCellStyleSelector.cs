using AutoOS.App.Data.Models.Network;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Syncfusion.UI.Xaml.TreeGrid;

namespace AutoOS.App.Data.TemplateSelectors.Network;

public sealed partial class CompareCellStyleSelector : StyleSelector
{
	public Style? CriticalStyle { get; set; }
	public Style? CautionStyle { get; set; }
	public Style? SuccessStyle { get; set; }

	protected override Style? SelectStyleCore(object item, DependencyObject container)
	{
		if (item is not Node node || node.Setting == null)
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
