using Microsoft.UI.Xaml;
using Syncfusion.UI.Xaml.TreeGrid;

namespace AutoOS.App.Helpers.TreeGrid;

/// <summary>
/// Per-column star ratio consumed by <see cref="TreeGridStarColumnSizer"/>.
/// </summary>
public static class StarRatio
{
	public static readonly DependencyProperty ColumnRatioProperty =
		DependencyProperty.RegisterAttached("ColumnRatio", typeof(double), typeof(StarRatio), new PropertyMetadata(1.0d, null));

	public static double GetColumnRatio(DependencyObject obj) => (double)obj.GetValue(ColumnRatioProperty);

	public static void SetColumnRatio(DependencyObject obj, double value) => obj.SetValue(ColumnRatioProperty, value);
}

/// <summary>
/// Star column sizer that keeps the weighted ratios of <see cref="StarRatio"/> while filling the
/// width the grid hands the sizer, including the expander allowance the grid takes out of the star
/// pool. The leftover pixels from the per-column rounding land in the last column, so the columns
/// never leave a dead strip on the right and never overflow into a horizontal scrollbar.
/// </summary>
public partial class TreeGridStarColumnSizer : TreeGridColumnSizer
{
	// TreeGridColumnSizer.ExpanderWidth default, used only when the grid reports no allowance.
	private const double DEFAULT_EXPANDER_WIDTH = 18d;

	// Border width SfTreeGrid adds to each expander indent in its expander allowance formula.
	private const double EXPANDER_CELL_BORDER = 2d;

	// Last star pool and grid width seen, used to ignore the pool shrinking while nodes expand.
	private double _lastPoolWidth;
	private double _lastGridWidth = -1d;

	public TreeGridStarColumnSizer()
		: base()
	{
	}

	/// <summary>
	/// Deepest expanded level the grid is showing, the multiplier SfTreeGrid uses for the expander
	/// allowance it takes out of the star pool. A grid that has not built its nodes yet reports none.
	/// </summary>
	private int GetVisibleLevelCount()
	{
		var nodes = TreeGrid?.View?.Nodes;
		return nodes != null ? Math.Max(nodes.MaxLevel, 0) : 0;
	}

	protected override void SetStarWidth(double remainingColumnWidth, IEnumerable<TreeGridColumn> remainingColumns)
	{
		// Refresh can hand this a null set while the grid is still being built.
		if (TreeGrid == null || remainingColumns == null)
			return;

		var columns = remainingColumns.ToList();
		if (columns.Count == 0)
			return;

		double totalRatio = 0;
		foreach (TreeGridColumn column in columns)
			totalRatio += StarRatio.GetColumnRatio(column);

		if (totalRatio <= 0)
			totalRatio = columns.Count;

		// remainingColumnWidth is the width left for star columns: using it instead of
		// TreeGrid.ActualWidth keeps fixed columns and the vertical scrollbar accounted for.
		double available = remainingColumnWidth;
		if (double.IsNaN(available) || available <= 0)
			available = TreeGrid.ActualWidth;

		// An unmeasured grid reports no width at all: leave the widths alone instead of collapsing
		// every column to its minimum, the next pass sizes them once the grid has a viewport.
		if (double.IsNaN(available) || available <= 0)
			return;

		// SfTreeGrid takes the expander allowance out of the star pool and gives it to the expander
		// column while it adjusts that column's width itself. A sizer that assigns every width has to
		// add the allowance back, or the columns stop that much short of the grid's right edge — and
		// because the allowance grows with the deepest expanded level, the strip grew every time a
		// node was opened. Syncfusion's formula for the allowance is an expander width per expanded
		// level plus the cell border, plus the check box column when the grid shows one.
		double expanderAllowance = ExpanderWidth;
		if (double.IsNaN(expanderAllowance) || expanderAllowance <= 0)
			expanderAllowance = DEFAULT_EXPANDER_WIDTH;

		double expanderColumnWidth = (GetVisibleLevelCount() + 1) * expanderAllowance + EXPANDER_CELL_BORDER;
		if (TreeGrid.ShowCheckBox)
			expanderColumnWidth += CheckBoxWidth + EXPANDER_CELL_BORDER;

		available += expanderColumnWidth;

		// Even with the allowance added back, never let a star pass shrink the columns below the
		// widest pool this grid offered at its current width: an expand must redistribute, not
		// narrow. Larger swings are real layout changes (a scrollbar, a resize) and are honoured.
		double gridWidth = TreeGrid.ActualWidth;
		if (gridWidth == _lastGridWidth && available < _lastPoolWidth && _lastPoolWidth - available <= expanderAllowance)
			available = _lastPoolWidth;

		_lastGridWidth = gridWidth;
		_lastPoolWidth = available;

		// Never size past the grid itself.
		if (gridWidth > 0 && available > gridWidth)
			available = gridWidth;

		// SetColumnWidth returns the width it actually applied, so clamped columns (MinimumWidth)
		// are accounted for; the last column then takes the exact remainder and the columns always
		// end flush with the right edge instead of leaving a strip behind the last column.
		double consumed = 0;
		for (int index = 0; index < columns.Count; index++)
		{
			TreeGridColumn column = columns[index];
			double ratio = StarRatio.GetColumnRatio(column);
			double width = index == columns.Count - 1
				? available - consumed // last column absorbs the rounding remainder
				: Math.Floor(available * ratio / totalRatio);

			if (width < 1)
				width = 1; // never collapse a column; the last one absorbs the remainder

			double applied = SetColumnWidth(column, width);
			consumed += applied >= 1 ? applied : width; // unmeasured column: trust the request
		}
	}
}
