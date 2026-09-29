using AutoOS.App.Data.Enums.Network;
using AutoOS.App.Data.Models.Network;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace AutoOS.App.Data.TemplateSelectors.Network;

public sealed partial class NameTemplateSelector : DataTemplateSelector
{
	public DataTemplate? AdapterTemplate { get; set; }

	public DataTemplate? SettingTemplate { get; set; }

	protected override DataTemplate? SelectTemplateCore(object item, DependencyObject container) =>
		item is Node { NodeKind: NodeKind.Adapter } ? AdapterTemplate : SettingTemplate;
}
