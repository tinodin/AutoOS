using AutoOS.App.Data.Models.Bios;
using AutoOS.Core.Data.Models.Bios;

namespace AutoOS.App.Data.TemplateSelectors.Bios;

public sealed partial class EditTemplateSelector : DataTemplateSelector
{
	public DataTemplate? ComboBoxTemplate { get; set; }

	public DataTemplate? TextBoxTemplate { get; set; }

	protected override DataTemplate? SelectTemplateCore(object item, DependencyObject container)
	{
		if (item is Node { Setting: { Options.Count: > 0 } })
			return ComboBoxTemplate;
		return TextBoxTemplate;
	}
}
