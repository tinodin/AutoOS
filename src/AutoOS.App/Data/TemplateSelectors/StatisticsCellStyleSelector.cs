using AutoOS.App.Data.Enums;
using AutoOS.App.ViewModels;
using Syncfusion.UI.Xaml.TreeGrid;

namespace AutoOS.App.Data.TemplateSelectors;

public sealed partial class StatisticsCellStyleSelector : StyleSelector
{
	public Style? SuccessStyle { get; set; }
	public Style? CriticalStyle { get; set; }

	protected override Style? SelectStyleCore(object item, DependencyObject container)
	{
		if (item is not ResultRow row || container is not TreeGridCell cell)
			return null;
		ComparisonResult comparison = cell.ColumnBase?.TreeGridColumn.MappingName switch
		{
			nameof(ResultRow.RecordingA) => row.RecordingAComparison,
			nameof(ResultRow.RecordingB) => row.RecordingBComparison,
			nameof(ResultRow.Delta) => row.DeltaComparison,
			_ => ComparisonResult.None
		};
		return comparison switch
		{
			ComparisonResult.Better => SuccessStyle,
			ComparisonResult.Worse => CriticalStyle,
			_ => null
		};
	}
}
