using AutoOS.App.Data.Models.Power;

namespace AutoOS.App.Data.TemplateSelectors.Power;

public sealed partial class EditTemplateSelector : DataTemplateSelector
{
	public DataTemplate? ComboBoxTemplate { get; set; }

	public DataTemplate? TextBoxTemplate { get; set; }

	protected override DataTemplate? SelectTemplateCore(object item, DependencyObject container)
	{
		if (item is Node node && node.HasOptions)
			return ComboBoxTemplate;
		return TextBoxTemplate;
	}
}
