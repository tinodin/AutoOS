using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Syncfusion.UI.Xaml.TreeGrid;
using Windows.System;

namespace AutoOS.App.Helpers.TreeGrid;

public static class TreeGridSelectionControllerHelper
{
	public static readonly DependencyProperty UseCustomSelectionControllerProperty =
		DependencyProperty.RegisterAttached(
			"UseCustomSelectionController",
			typeof(bool),
			typeof(TreeGridSelectionControllerHelper),
			new PropertyMetadata(false, OnUseCustomSelectionControllerChanged));

	public static bool GetUseCustomSelectionController(DependencyObject obj)
		=> (bool)obj.GetValue(UseCustomSelectionControllerProperty);

	public static void SetUseCustomSelectionController(DependencyObject obj, bool value)
		=> obj.SetValue(UseCustomSelectionControllerProperty, value);

	private static void OnUseCustomSelectionControllerChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is SfTreeGrid treeGrid && e.NewValue is bool value && value)
		{
			treeGrid.SelectionController = new TreeGridSelectionController(treeGrid);
		}
	}
}

public sealed partial class TreeGridSelectionController : TreeGridRowSelectionController
{
	private readonly SfTreeGrid _treeGrid;

	public TreeGridSelectionController(SfTreeGrid treeGrid) : base(treeGrid)
	{
		_treeGrid = treeGrid;
		treeGrid.LostFocus += (_, _) =>
		{
			var focused = FocusManager.GetFocusedElement(_treeGrid.XamlRoot) as DependencyObject;

			if (focused == null)
			{
				ClearSelections(false);
				if (CurrentCellManager.CurrentCell?.IsEditing == true)
					CurrentCellManager.EndEdit();
				return;
			}

			if (focused is ComboBox || focused is ComboBoxItem)
				return;

			if (focused is Microsoft.UI.Xaml.Controls.TextBox textBox)
			{
				DependencyObject parent = VisualTreeHelper.GetParent(textBox);
				while (parent != null)
				{
					if (parent is TreeGridCell)
						return;
					parent = VisualTreeHelper.GetParent(parent);
				}
			}

			DependencyObject current = focused;
			while (current != null)
			{
				if (current == _treeGrid)
					return;
				current = VisualTreeHelper.GetParent(current);
			}

			ClearSelections(false);
			if (CurrentCellManager.CurrentCell?.IsEditing == true)
				CurrentCellManager.EndEdit();
		};
	}

	protected override void ProcessKeyDown(KeyRoutedEventArgs args)
	{
		if (args.Key == VirtualKey.Enter && CurrentCellManager.CurrentCell?.IsEditing != true)
		{
			CurrentCellManager.BeginEdit();
			args.Handled = true;
			return;
		}

		base.ProcessKeyDown(args);
	}
}
