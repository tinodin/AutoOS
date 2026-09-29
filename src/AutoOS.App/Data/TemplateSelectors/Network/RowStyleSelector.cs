using AutoOS.App.Data.Enums.Network;
using AutoOS.App.Data.Models.Network;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Syncfusion.UI.Xaml.TreeGrid;

namespace AutoOS.App.Data.TemplateSelectors.Network;

public sealed partial class RowStyleSelector : StyleSelector
{
	public Style? AdapterStyle { get; set; }

	public Style? SettingStyle { get; set; }

	protected override Style? SelectStyleCore(object item, DependencyObject container)
	{
		Node? node = item switch
		{
			Node directNode => directNode,
			TreeDataRowBase { RowData: Node rowNode } => rowNode,
			_ => null
		};

		return node is { NodeKind: NodeKind.Adapter } ? AdapterStyle : SettingStyle;
	}
}
