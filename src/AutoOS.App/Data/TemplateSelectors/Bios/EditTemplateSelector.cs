using AutoOS.App.Data.Models.Bios;
using Syncfusion.UI.Xaml.TreeGrid;

namespace AutoOS.App.Data.TemplateSelectors.Bios;

public sealed partial class EditTemplateSelector : DataTemplateSelector
{
	public DataTemplate ComboBoxTemplate { get; set; }
	public DataTemplate TextBoxTemplate { get; set; }

	protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
	{
		if (item is BiosTreeNode node && node.HasOptions)
			return ComboBoxTemplate;
		return TextBoxTemplate;
	}
}
