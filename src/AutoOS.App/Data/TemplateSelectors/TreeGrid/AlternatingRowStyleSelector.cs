using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Syncfusion.UI.Xaml.TreeGrid;

namespace AutoOS.App.Data.TemplateSelectors.TreeGrid;

public sealed partial class AlternatingRowStyleSelector : StyleSelector
{
	public Style? AlternatingStyle { get; set; }

	protected override Style? SelectStyleCore(object item, DependencyObject container)
	{
		int rowIndex = item is TreeDataRowBase dataRow ? dataRow.RowIndex : (container as TreeGridRowControl)?.DataRow?.RowIndex ?? -1;

		if (rowIndex < 0 || rowIndex % 2 == 0)
			return null;

		return AlternatingStyle;
	}
}
