using AutoOS.App.Data.Models.Bios;
using Syncfusion.UI.Xaml.TreeGrid;

namespace AutoOS.App.Data.TemplateSelectors.Bios;

public sealed partial class EditTemplateSelector : DataTemplateSelector
{
	public DataTemplate ComboBoxTemplate { get; set; } = null!;
	public DataTemplate TextBoxTemplate { get; set; } = null!;

	protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
	{
		if (item is Node node && node.HasOptions)
			return ComboBoxTemplate;
		return TextBoxTemplate;
	}
}
