using System.ComponentModel;
using System.Diagnostics;
using AutoOS.App.Helpers;
using AutoOS.App.Helpers.TreeGrid;
using AutoOS.App.ViewModels;
using AutoOS.Core.Services.DiskAnalyzer;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using Syncfusion.UI.Xaml.TreeGrid;
using RightTappedEventArgs = Microsoft.UI.Xaml.Input.RightTappedRoutedEventArgs;
using DoubleTappedEventArgs = Microsoft.UI.Xaml.Input.DoubleTappedRoutedEventArgs;

namespace AutoOS.App.Views.Settings;

public sealed partial class DiskCleanupPage : Page
{
	private static readonly TimeSpan SizeDebounce = TimeSpan.FromMilliseconds(150);

	public DiskCleanupViewModel ViewModel { get; } = new();

	private DispatcherTimer? _sizeTimer;
	private SfTreeGrid? _pendingSizeGrid;

	public DiskCleanupPage()
	{
		InitializeComponent();
	}

	protected override void OnNavigatedTo(NavigationEventArgs e)
	{
		base.OnNavigatedTo(e);
		ViewModel.RefreshFilterAction = RefreshFilter;
		// File View filters rebuild the observable on a background debounce; marshal to UI thread.
		ViewModel.RefreshFileFilterAction = () => DispatcherQueue.TryEnqueue(() => ViewModel.ApplyFileFilter());
		ViewModel.SuspendGridUpdatesAction = () =>
		{
			// Do not call SfTreeGrid.View.BeginInit here. SwitchPresenter can detach the
			// TreeView case while a scan is still binding data, and Syncfusion does not
			// support ending a deferred view refresh after that detach.
		};
		ViewModel.ResumeGridUpdatesAction = () =>
		{
		};
		ViewModel.PropertyChanged += OnViewModelPropertyChanged;
		// WizTree default: all grids sort by Allocated descending
		// (data arrives pre-sorted; this shows the arrow).
		SetDefaultSort(TreeGrid, "Allocated");
		SetDefaultSort(FileGrid, "Allocated");
		SetDefaultSort(ExtensionGrid, "Allocated");
		// Size the star columns once the page has its real width, then again whenever rows settle.
		ResetAllColumnWidths();
		// Auto-scan on entry if desired — keep manual for now
	}

	protected override void OnNavigatedFrom(NavigationEventArgs e)
	{
		ViewModel.RefreshFilterAction = null;
		ViewModel.RefreshFileFilterAction = null;
		ViewModel.SuspendGridUpdatesAction = null;
		ViewModel.ResumeGridUpdatesAction = null;
		ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
		_sizeTimer?.Stop();
		base.OnNavigatedFrom(e);
	}

	private static DiskNode? FindNodeByPath(IEnumerable<DiskNode> roots, string path)
	{
		foreach (var r in roots)
		{
			if (r.FullPath.Equals(path, StringComparison.OrdinalIgnoreCase))
				return r;
			var found = FindNodeByPath(r.Children, path);
			if (found != null)
				return found;
		}
		return null;
	}

	private static bool ExpandPath(DiskNode current, string targetPath)
	{
		if (current.FullPath.Equals(targetPath, StringComparison.OrdinalIgnoreCase))
			return true;
		foreach (var child in current.Children)
		{
			if (targetPath.StartsWith(child.FullPath, StringComparison.OrdinalIgnoreCase) && ExpandPath(child, targetPath))
			{
				current.IsExpanded = true;
				return true;
			}
		}
		return false;
	}

	private void DiskCleanupViewSelector_SelectionChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
	{
		if (sender.SelectedItem == FileViewSegment)
			ViewModel.ActiveTab = "FileView";
		else
			ViewModel.ActiveTab = "TreeView";
	}

	private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		// Keep the tab selector in sync when ActiveTab changes programmatically (e.g. treemap click).
		if (e.PropertyName == nameof(DiskCleanupViewModel.ActiveTab))
		{
			bool wantFile = ViewModel.ActiveTab == "FileView";
			if (wantFile && !FileViewSegment.IsSelected)
				FileViewSegment.IsSelected = true;
			else if (!wantFile && !TreeViewSegment.IsSelected)
				TreeViewSegment.IsSelected = true;
		}
		else if (e.PropertyName == nameof(DiskCleanupViewModel.IsContentBusy) && !ViewModel.IsContentBusy)
		{
			// While content loads the grids are collapsed behind the scan overlay, so any column
			// sizing measured then is stale once the rows (and their vertical scrollbar) appear.
			ResetAllColumnWidths();
		}
	}

	/// <summary>
	/// Re-runs the star column sizing for every grid once the loaded rows have settled, so the
	/// columns always end flush with the right edge of their grid.
	/// </summary>
	private void ResetAllColumnWidths()
	{
		_pendingSizeGrid = null;
		_sizeTimer?.Stop();
		DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
		{
			ResetColumnWidths(TreeGrid);
			ResetColumnWidths(ExtensionGrid);
			ResetColumnWidths(FileGrid);
		});
	}

	/// <summary>
	/// Clears the explicit column widths and re-runs the star sizer. Per the SfTreeGrid docs the
	/// sizer stops applying once a width was set explicitly (which our own star pass does), so
	/// <c>ColumnSizer.Refresh</c> is what re-fits the columns to the current viewport.
	/// </summary>
	/// <remarks>
	/// Syncfusion's <c>Refresh</c> walks the grid's view and panel, so it must never run before the
	/// grid is loaded and has a view: a grid that is still inside an unselected tab case (or one the
	/// page has not measured yet) throws from inside the Syncfusion call, which used to take the
	/// whole page down. Anything that is not ready is skipped here and sized by
	/// <see cref="TreeGrid_Loaded"/> once it enters the visual tree.
	/// </remarks>
	private static void ResetColumnWidths(SfTreeGrid? grid)
	{
		// A grid that is not loaded yet (an unselected tab case) or has no measured width cannot be
		// sized: resetting its widths there would only leave NaN behind until the next pass.
		if (grid == null || !grid.IsLoaded || grid.Columns.Count == 0 || grid.ActualWidth <= 0)
			return;

		try
		{
			// Both the reset and the Syncfusion refresh walk the grid's view, and either can throw
			// while a grid is mid-teardown or between layouts.
			foreach (var column in grid.Columns)
			{
				if (!double.IsNaN(column.Width))
					column.Width = double.NaN;
			}

			grid.ColumnSizer?.Refresh();
		}
		catch (Exception ex)
		{
			// Cosmetic only: column widths must never break navigation. A grid can be mid-teardown or
			// between layouts inside Syncfusion's refresh, and the next size change or row load
			// revisits the sizer on a grid that is ready for it.
			Trace.WriteLine($"[DiskCleanup] column width reset skipped: {ex.Message}");
		}
	}

	/// <summary>
	/// Sizes a grid's star columns the first time it enters the visual tree. Tab cases (File View,
	/// the extension panel) are only loaded when they are selected, so their sizer is refreshed here
	/// instead of during the page's initial, still unmeasured pass.
	/// </summary>
	private void TreeGrid_Loaded(object sender, RoutedEventArgs e)
	{
		if (sender is SfTreeGrid grid)
			ResetColumnWidths(grid);
	}

	private void TreeGrid_SizeChanged(object sender, SizeChangedEventArgs e)
	{
		if (e.NewSize.Width <= 0 || e.NewSize.Width == e.PreviousSize.Width)
			return;

		if (sender is not SfTreeGrid grid)
			return;

		// Skip expensive column re-measure during scan/load; the settle handler re-runs it for every
		// grid once loading finishes, and resize during that time is debounced (was layout storm per pixel).
		if (ViewModel.IsContentBusy)
			return;

		_pendingSizeGrid = grid;
		_sizeTimer ??= new DispatcherTimer { Interval = SizeDebounce };
		_sizeTimer.Tick -= OnSizeTick;
		_sizeTimer.Tick += OnSizeTick;
		_sizeTimer.Stop();
		_sizeTimer.Start();
	}

	private void OnSizeTick(object? sender, object e)
	{
		_sizeTimer?.Stop();
		if (_pendingSizeGrid == null)
			return;

		ResetColumnWidths(_pendingSizeGrid);
		_pendingSizeGrid = null;
	}

	private void TreeGrid_CellToolTipOpening(object sender, TreeGridCellToolTipOpeningEventArgs e)
	{
		if (e.Record is not DiskNode node)
		{
			e.ToolTip.Visibility = Visibility.Collapsed;
			return;
		}

		string? content = e.Column?.MappingName switch
		{
			nameof(DiskNode.Name) => node.FullPath,
			nameof(DiskNode.Size) => $"{node.Size:N0} bytes ({node.SizeText})",
			nameof(DiskNode.Allocated) => $"{node.Allocated:N0} bytes ({node.AllocatedText})",
			nameof(DiskNode.PercentOfParent) => $"{node.PercentOfParent:F2}% of parent, {node.PercentOfTotal:F2}% of drive",
			nameof(DiskNode.ItemsCount) => $"{node.ItemsText} items — {node.FilesText} files, {node.FoldersText} folders",
			nameof(DiskNode.FileCount) => $"{node.FilesText} files (recursive)",
			nameof(DiskNode.FolderCount) => $"{node.FoldersText} subfolders (recursive)",
			nameof(DiskNode.Modified) => node.Modified == DateTime.MinValue ? null : node.Modified.ToLocalTime().ToString("g"),
			nameof(DiskNode.Attributes) => string.IsNullOrEmpty(node.Attributes) ? null : $"Attributes: {node.Attributes}",
			_ => null
		};
		if (string.IsNullOrWhiteSpace(content))
		{
			e.ToolTip.Visibility = Visibility.Collapsed;
			return;
		}

		e.ToolTip.Content = content;
		e.ToolTip.Visibility = Visibility.Visible;
	}

	private void FileGrid_CellToolTipOpening(object sender, TreeGridCellToolTipOpeningEventArgs e)
	{
		if (e.Record is not DiskFileRow row)
		{
			e.ToolTip.Visibility = Visibility.Collapsed;
			return;
		}

		string? content = e.Column?.MappingName switch
		{
			nameof(DiskFileRow.FileName) => row.FullPath,
			nameof(DiskFileRow.Directory) => row.FullPath,
			nameof(DiskFileRow.Size) => $"{row.Size:N0} bytes ({row.SizeText})",
			nameof(DiskFileRow.Allocated) => $"{row.Allocated:N0} bytes ({row.AllocatedText})",
			nameof(DiskFileRow.PercentOfDrive) => $"{row.PercentOfDrive:F2}% of drive",
			nameof(DiskFileRow.DupCount) => row.DupCount > 1 ? $"{row.DupCount:N0} duplicates — {DiskNode.FormatBytes(row.DupSize)} total" : null,
			_ => null
		};
		if (string.IsNullOrWhiteSpace(content))
		{
			e.ToolTip.Visibility = Visibility.Collapsed;
			return;
		}

		e.ToolTip.Content = content;
		e.ToolTip.Visibility = Visibility.Visible;
	}

	private void Grid_RightTapped(object sender, RightTappedEventArgs e)
	{
		if (sender is not SfTreeGrid grid)
			return;

		var node = grid.SelectedItem as DiskNode;
		if (node == null)
			return;

		ShowContextMenu(node, (UIElement)sender, e);
	}

	private void FileGrid_RightTapped(object sender, RightTappedEventArgs e)
	{
		if (sender is SfTreeGrid grid && grid.SelectedItem is DiskFileRow row)
			ShowFileContextMenu(row, (UIElement)sender, e);
	}

	private void ShowContextMenu(DiskNode node, UIElement sender, RightTappedEventArgs e)
	{
		var flyout = new MenuFlyout();

		var open = new MenuFlyoutItem { Text = node.IsFolder ? "Open folder" : "Open containing folder", Icon = new FontIcon { Glyph = "" } };
		open.Click += (_, _) => DiskLauncher.Open(node);
		flyout.Items.Add(open);

		var copy = new MenuFlyoutItem { Text = "Copy path", Icon = new FontIcon { Glyph = "" } };
		copy.Click += (_, _) => DiskLauncher.CopyPath(node.FullPath);
		flyout.Items.Add(copy);

		flyout.Items.Add(new MenuFlyoutSeparator());

		var del = new MenuFlyoutItem { Text = "Delete (recycle)", Icon = new FontIcon { Glyph = "" } };
		del.Click += (_, _) => _ = DeleteNodeAsync(node);
		flyout.Items.Add(del);

		flyout.ShowAt(sender, e.GetPosition(sender));
	}

	private void ShowFileContextMenu(DiskFileRow row, UIElement sender, RightTappedEventArgs e)
	{
		var flyout = new MenuFlyout();

		var open = new MenuFlyoutItem { Text = "Open containing folder", Icon = new FontIcon { Glyph = "" } };
		open.Click += (_, _) => DiskLauncher.OpenFile(row.FullPath);
		flyout.Items.Add(open);

		var copy = new MenuFlyoutItem { Text = "Copy path", Icon = new FontIcon { Glyph = "" } };
		copy.Click += (_, _) => DiskLauncher.CopyPath(row.FullPath);
		flyout.Items.Add(copy);

		flyout.ShowAt(sender, e.GetPosition(sender));
	}

	private async Task DeleteNodeAsync(DiskNode node)
	{
		// Confirm
		var dlg = new ContentDialog
		{
			Title = "Delete?",
			Content = $"Move to recycle bin:\n{node.FullPath}\n\nSize: {node.SizeText}",
			PrimaryButtonText = "Delete",
			CloseButtonText = "Cancel",
			DefaultButton = ContentDialogButton.Close,
			XamlRoot = XamlRoot
		};
		if (await dlg.ShowAsync() != ContentDialogResult.Primary)
			return;

		try
		{
			if (node.IsFolder)
				Directory.Delete(node.FullPath, true);
			else
				File.Delete(node.FullPath);

			RemoveNodeFromTree(node);
		}
		catch (Exception ex)
		{
			var err = new ContentDialog { Title = "Delete failed", Content = ex.Message, CloseButtonText = "OK", XamlRoot = XamlRoot };
			await err.ShowAsync();
		}
	}

	private void RemoveNodeFromTree(DiskNode node)
	{
		// Roll up sizes AND recursive counts instead of a full drive rescan for one delete.
		if (FindParentChain(ViewModel.TreeNodes, node) is List<DiskNode> chain && chain.Count > 0)
		{
			DiskNode parent = chain[^1];
			parent.Children.Remove(node);
			int foldersRemoved = node.FolderCount + (node.IsFolder ? 1 : 0);
			foreach (var ancestor in chain)
			{
				ancestor.Size -= node.Size;
				ancestor.Allocated -= node.Allocated;
				ancestor.FileCount -= node.FileCount;
				ancestor.FolderCount -= foldersRemoved;
				if (ancestor.FileCount < 0) ancestor.FileCount = 0;
				if (ancestor.FolderCount < 0) ancestor.FolderCount = 0;
			}
			// Recompute % of parent down the affected subtree (root total shrank).
			if (ViewModel.TreeNodes.Count > 0)
			{
				var root = ViewModel.TreeNodes[0];
				RecalcPercents(root, root.Allocated);
			}
		}
		else
		{
			ViewModel.TreeNodes.Remove(node);
		}
	}

	private static void SetDefaultSort(SfTreeGrid grid, string mappingName)
	{
		try
		{
			if (grid.SortColumnDescriptions.Count == 0)
				grid.SortColumnDescriptions.Add(new Syncfusion.UI.Xaml.Grids.SortColumnDescription
				{
					ColumnName = mappingName,
					SortDirection = Syncfusion.UI.Xaml.Data.SortDirection.Descending
				});
		}
		catch
		{
			// Sorting is cosmetic: a grid that is not ready yet must not break navigation.
		}
	}

	private static void RecalcPercents(DiskNode node, long totalAllocated)
	{
		// Allocated-based like WizTree.
		if (totalAllocated > 0)
			node.PercentOfTotal = (double)node.Allocated / totalAllocated * 100;
		if (node.Children.Count > 0 && node.Allocated > 0)
		{
			foreach (var c in node.Children)
			{
				c.PercentOfParent = (double)c.Allocated / node.Allocated * 100;
				RecalcPercents(c, totalAllocated);
			}
		}
	}

	private static List<DiskNode>? FindParentChain(IEnumerable<DiskNode> roots, DiskNode target)
	{
		foreach (var root in roots)
		{
			if (root.Children.Contains(target))
				return [root];

			foreach (var child in root.Children)
				if (FindParentChain(new[] { child }, target) is List<DiskNode> sub)
				{
					sub.Insert(0, root);
					return sub;
				}
		}

		return null;
	}

	private void Grid_DoubleTapped(object sender, DoubleTappedEventArgs e)
	{
		if (sender is SfTreeGrid grid && grid.SelectedItem is DiskNode node)
			DiskLauncher.Open(node);
	}

	private void FileGrid_DoubleTapped(object sender, DoubleTappedEventArgs e)
	{
		if (sender is SfTreeGrid grid && grid.SelectedItem is DiskFileRow row)
			DiskLauncher.OpenFile(row.FullPath);
	}

	private void DriveScan_Click(object sender, RoutedEventArgs e)
	{
		if (sender is Button button && button.DataContext is DriveModelLite drive)
			ViewModel.StartScan(drive.RootPath);
	}

	private void RefreshFilter()
	{
		ApplyFilter(TreeGrid);
	}

	private void ApplyFilter(SfTreeGrid grid)
	{
		var view = grid.View;
		if (view == null)
			return;

		view.Filter = ViewModel.MatchesFilter;
		view.RefreshFilter();
	}
}
