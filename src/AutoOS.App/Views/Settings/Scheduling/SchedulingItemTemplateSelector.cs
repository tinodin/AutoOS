namespace AutoOS.App.Views.Settings.Scheduling;

public sealed partial class SchedulingItemTemplateSelector : DataTemplateSelector
{
	public DataTemplate GroupTemplate { get; set; }
	public DataTemplate ItemTemplate { get; set; }

	protected override DataTemplate SelectTemplateCore(object item)
	{
		if (item is SchedulingGroup) return GroupTemplate;
		if (item is SchedulingItem) return ItemTemplate;
		return base.SelectTemplateCore(item);
	}
}
