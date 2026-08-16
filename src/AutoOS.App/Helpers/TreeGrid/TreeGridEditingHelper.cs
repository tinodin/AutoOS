using Syncfusion.UI.Xaml.Grids.ScrollAxis;
using Syncfusion.UI.Xaml.TreeGrid;

namespace AutoOS.App.Helpers.TreeGrid;

public static class TreeGridEditingHelper
{
	public static bool EndEditingIfActive(SfTreeGrid treeGrid)
	{
		TreeGridCurrentCellManager manager = treeGrid.SelectionController.CurrentCellManager;
		if (manager.CurrentCell?.IsEditing != true)
			return false;

		RowColumnIndex index = manager.CurrentRowColumnIndex;
		manager.EndEdit();
		treeGrid.SelectionController.MoveCurrentCell(index);
		return true;
	}
}
