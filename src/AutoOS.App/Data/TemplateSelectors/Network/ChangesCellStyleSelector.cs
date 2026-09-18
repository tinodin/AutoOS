using AutoOS.App.Data.Models.Network;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Syncfusion.UI.Xaml.TreeGrid;

namespace AutoOS.App.Data.TemplateSelectors.Network;

public sealed partial class ChangesCellStyleSelector : StyleSelector
{
	public Style? CriticalStyle { get; set; }

	public Style? CautionStyle { get; set; }

	public Style? SuccessStyle { get; set; }

	protected override Style? SelectStyleCore(object item, DependencyObject container)
	{
		if (item is not Node node || container is not TreeGridCell cell)
			return null;

		string? mappingName = cell.ColumnBase?.TreeGridColumn?.MappingName;
		if (node.HasErrors && mappingName == nameof(Node.DisplayCurrent))
			return CautionStyle;
		if (!node.IsModified || mappingName is not (nameof(Node.DisplayCurrent) or nameof(Node.DisplayOriginal)))
			return null;

		if (mappingName == nameof(Node.DisplayOriginal))
			return CriticalStyle;

		return SuccessStyle;
	}
}
