using System.ComponentModel;
using System.Diagnostics;
using AutoOS.App.Helpers;
using AutoOS.App.UserControls.Treemap;
using AutoOS.App.ViewModels;
using AutoOS.Core.Services.DiskAnalyzer;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using Syncfusion.UI.Xaml.Grids;
using Syncfusion.UI.Xaml.TreeGrid;
using DoubleTappedEventArgs = Microsoft.UI.Xaml.Input.DoubleTappedRoutedEventArgs;
using RightTappedEventArgs = Microsoft.UI.Xaml.Input.RightTappedRoutedEventArgs;

namespace AutoOS.App.Views.Settings;

public sealed partial class DiskCleanupPage : Page
{
	private static readonly TimeSpan SIZE_DEBOUNCE = TimeSpan.FromMilliseconds(150);

	public DiskCleanupViewModel ViewModel { get; } = new();

	private DispatcherTimer? _sizeTimer;
	private SfTreeGrid? _pendingSizeGrid;
	private readonly List<SfTreeGrid> _suspendedGrids = new(3);
	// Widths already sized per grid instance: star widths persist on the columns, so a repeat
	// pass at the same width (settle ticks, rebinds) would only re-walk 100k rows for nothing.
	private readonly Dictionary<SfTreeGrid, double> _sizedWidths = new();
	private long _treemapSelectionToken;

	public DiskCleanupPage()
	{
		InitializeComponent();
	}

	protected override void OnNavigatedTo(NavigationEventArgs e)
	{
		base.OnNavigatedTo(e);
		// File View filters rebuild the observable on a background debounce; marshal to UI thread.
		ViewModel.RefreshFileFilterAction = () => DispatcherQueue.TryEnqueue(() => ViewModel.ApplyFileFilter());
		// Doc-backed bulk-update bracket (winui-docs TreeGrid Data-Binding: View.BeginInit /
		// EndInit with TreeViewRefreshMode.DeferRefresh recreates the nodes once instead of
		// once per collection change). Both halves run back-to-back on the UI thread around
		// the bind, so navigation cannot interleave them; every EndInit is still guarded.
		ViewModel.SuspendGridUpdatesAction = SuspendGridViews;
		ViewModel.ResumeGridUpdatesAction = ResumeGridViews;
		ViewModel.PropertyChanged += OnViewModelPropertyChanged;
		_treemapSelectionToken = Treemap.RegisterPropertyChangedCallback(TreemapView.SelectedNodeProperty, OnTreemapSelectedNodeChanged);
		// WizTree default: all grids sort by Allocated descending
		// (data arrives pre-sorted; this shows the arrow).
		SetDefaultSort(TreeGrid, "Allocated");
		SetDefaultSort(FileGrid, "Allocated");
		SetDefaultSort(ExtensionGrid, "Allocated");
		// Size the star columns once the page has its real width, then again whenever rows settle.
		ResetAllColumnWidths();
		// Treemap mirrors the current tree, if a scan already ran (folders + files paint together).
		Treemap.SetTree(ViewModel.TreeNodes.FirstOrDefault(), ViewModel.TreemapFileGroups);
		// Auto-scan on entry if desired — keep manual for now
	}

	protected override void OnNavigatedFrom(NavigationEventArgs e)
	{
		ViewModel.RefreshFileFilterAction = null;
		ViewModel.SuspendGridUpdatesAction = null;
		ViewModel.ResumeGridUpdatesAction = null;
		ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
		Treemap.UnregisterPropertyChangedCallback(TreemapView.SelectedNodeProperty, _treemapSelectionToken);
		_treemapSelectionToken = 0;
		_sizeTimer?.Stop();
		_sizedWidths.Clear();
		_suspendedGrids.Clear();
		base.OnNavigatedFrom(e);
	}

	private static bool ExpandPath(DiskNode current, string targetPath)
	{
		if (current.FullPath.Equals(targetPath, StringComparison.OrdinalIgnoreCase))
			return true;

		foreach (var child in current.Children)
		{
			bool isAncestor = targetPath.Equals(child.FullPath, StringComparison.OrdinalIgnoreCase)
				|| targetPath.StartsWith(child.FullPath + "\\", StringComparison.OrdinalIgnoreCase);

			if (isAncestor && ExpandPath(child, targetPath))
			{
				current.IsExpanded = true;

				return true;
			}
		}

		return false;
	}

	private void ExtensionGrid_SelectionChanged(object sender, GridSelectionChangedEventArgs e)
	{
		DiskExtensionStat? stat = ExtensionGrid.SelectedItem as DiskExtensionStat;
		if (stat is not null)
			Treemap.SetHighlightedExtension(stat.Extension);
		else
			Treemap.ClearHighlightedExtension();
	}

	private void OnTreemapSelectedNodeChanged(DependencyObject sender, DependencyProperty dp)
	{
		if (Treemap.SelectedNode is not DiskNode node)
			return;

		bool expanded = false;

		foreach (var root in ViewModel.TreeNodes)
		{
			if (ExpandPath(root, node.FullPath))
			{
				expanded = true;

				break;
			}
		}

		if (!expanded)
			return;

		TreeGrid.SelectedItem = node;

		DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
		{
			if (TreeGrid.View == null || !TreeGrid.IsLoaded)
				return;

			try
			{
				int rowIndex = Syncfusion.UI.Xaml.TreeGrid.TreeGridIndexResolver.ResolveToRowIndex(TreeGrid, node);

				if (rowIndex < 0)
					return;

				TreeGrid.ScrollInView(new Syncfusion.UI.Xaml.Grids.ScrollAxis.RowColumnIndex(rowIndex, 0));
			}
			catch (Exception ex)
			{
				Trace.WriteLine($"[DiskCleanup] treemap jump scroll skipped: {ex.Message}");
			}
		});
	}

	private void DiskCleanupViewSelector_SelectionChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
	{
		if (sender.SelectedItem == FileViewSegment)
			ViewModel.ActiveTab = "FileView";
		else
			ViewModel.ActiveTab = "TreeView";
	}

	private void SuspendGridViews()
	{
		_suspendedGrids.Clear();
		foreach (SfTreeGrid? grid in new SfTreeGrid?[] { TreeGrid, ExtensionGrid, FileGrid })
		{
			if (grid == null || !grid.IsLoaded)
				continue;
			try
			{
				var view = grid.View;
				if (view == null || view.IsInDeferRefresh)
					continue;
				view.BeginInit(TreeViewRefreshMode.DeferRefresh);
				_suspendedGrids.Add(grid);
			}
			catch (Exception ex)
			{
				Trace.WriteLine($"[DiskCleanup] grid suspend skipped: {ex.Message}");
			}
		}
	}

	private void ResumeGridViews()
	{
		for (int i = _suspendedGrids.Count - 1; i >= 0; i--)
		{
			try
			{
				_suspendedGrids[i].View?.EndInit();
			}
			catch (Exception ex)
			{
				Trace.WriteLine($"[DiskCleanup] grid resume skipped: {ex.Message}");
			}
		}

		_suspendedGrids.Clear();
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
			// Treemap mirrors the freshly bound tree (null when the scan found nothing);
			// folders and files paint together in one rebuild.
			Treemap.SetTree(ViewModel.TreeNodes.FirstOrDefault(), ViewModel.TreemapFileGroups);
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
	private void ResetColumnWidths(SfTreeGrid? grid)
	{
		// A grid that is not loaded yet (an unselected tab case) or has no measured width cannot be
		// sized: resetting its widths there would only leave NaN behind until the next pass.
		if (grid == null || !grid.IsLoaded || grid.Columns.Count == 0 || grid.ActualWidth <= 0)
			return;

		// Star widths persist on the columns across rebinds: sizing twice at the same width only
		// re-walks the whole view (100k rows on a full drive scan) for an identical result.
		if (_sizedWidths.TryGetValue(grid, out double sizedWidth) && sizedWidth == grid.ActualWidth)
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
			_sizedWidths[grid] = grid.ActualWidth;
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
		_sizeTimer ??= new DispatcherTimer { Interval = SIZE_DEBOUNCE };
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

	private void ExtensionGrid_CellToolTipOpening(object sender, TreeGridCellToolTipOpeningEventArgs e)
	{
		if (e.Record is not DiskExtensionStat stat)
		{
			e.ToolTip.Visibility = Visibility.Collapsed;
			return;
		}

		string? content = e.Column?.MappingName switch
		{
			nameof(DiskExtensionStat.Extension) => $"{stat.Extension} — {stat.FileType}",
			nameof(DiskExtensionStat.FileType) => stat.FileType,
			nameof(DiskExtensionStat.Percent) => $"{stat.Percent:F2}% of drive",
			nameof(DiskExtensionStat.Size) => $"{stat.Size:N0} bytes ({stat.SizeText})",
			nameof(DiskExtensionStat.Allocated) => $"{stat.Allocated:N0} bytes ({stat.AllocatedText})",
			nameof(DiskExtensionStat.Files) => $"{stat.FilesText} files",
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
			DiskNode? currentRoot = ViewModel.TreeNodes.FirstOrDefault();
			if (!ReferenceEquals(Treemap.Root, currentRoot))
				Treemap.Root = currentRoot;
			// Folder deletes prune descendant tiles via RefreshTree (stale parents); file
			// deletes remove the single tile directly.
			if (node.IsFolder)
				Treemap.RefreshTree();
			else
				Treemap.RemoveFileTile(node.FullPath);
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
				if (ancestor.FileCount < 0)
					ancestor.FileCount = 0;
				if (ancestor.FolderCount < 0)
					ancestor.FolderCount = 0;
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
		// CommandParameter carries the card's item straight from the compiled template;
		// DataContext is the fallback (typed templates don't guarantee it the same way).
		DriveModelLite? drive = (sender as Button)?.CommandParameter as DriveModelLite
			?? (sender as Button)?.DataContext as DriveModelLite;
		if (drive != null)
			ViewModel.StartScan(drive.RootPath);
	}

}
