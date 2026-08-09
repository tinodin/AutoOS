using Syncfusion.UI.Xaml.TreeGrid;

namespace AutoOS.App.Helpers.TreeGrid;

public static class StarRatio
{
	public static readonly DependencyProperty ColumnRatioProperty =
		DependencyProperty.RegisterAttached("ColumnRatio", typeof(int), typeof(StarRatio), new PropertyMetadata(1, null));

	public static int GetColumnRatio(DependencyObject obj) => (int)obj.GetValue(ColumnRatioProperty);
	public static void SetColumnRatio(DependencyObject obj, int value) => obj.SetValue(ColumnRatioProperty, value);
}

public partial class TreeGridStarColumnSizer : TreeGridColumnSizer
{
	public TreeGridStarColumnSizer() : base()
	{
	}

	protected override void SetStarWidth(double remainingColumnWidth, IEnumerable<TreeGridColumn> remainingColumns)
	{
		var removedColumn = new List<TreeGridColumn>();
		var columns = remainingColumns.ToList();
		double totalRemainingStarValue = TreeGrid.ActualWidth - 2.0001;
		double removedWidth = 0;
		bool isRemoved;

		while (columns.Count > 0)
		{
			isRemoved = false;
			removedWidth = 0;
			int columnsCount = 0;

			columns.ForEach((col) =>
			{
				columnsCount += StarRatio.GetColumnRatio(col);
			});

			double starWidth = Math.Floor(totalRemainingStarValue / columnsCount);
			TreeGridColumn column = columns.First();
			starWidth *= StarRatio.GetColumnRatio(column);
			double computedWidth = SetColumnWidth(column, starWidth);

			if (starWidth != computedWidth && starWidth > 0)
			{
				isRemoved = true;
				columns.Remove(column);

				foreach (TreeGridColumn remColumn in removedColumn)
				{
					if (!columns.Contains(remColumn))
					{
						removedWidth += remColumn.ActualWidth;
						columns.Add(remColumn);
					}
				}

				removedColumn.Clear();
				totalRemainingStarValue += removedWidth;
			}

			totalRemainingStarValue -= computedWidth;

			if (!isRemoved)
			{
				columns.Remove(column);

				if (!removedColumn.Contains(column))
					removedColumn.Add(column);
			}
		}
	}
}
