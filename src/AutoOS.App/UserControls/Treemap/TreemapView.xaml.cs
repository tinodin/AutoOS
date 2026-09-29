using System.Numerics;
using AutoOS.App.Helpers;
using AutoOS.Core.Services.DiskAnalyzer;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.UI;

namespace AutoOS.App.UserControls.Treemap;

/// <summary>
/// WizTree-style treemap: transparent nested folder outlines plus individual file tiles, all
/// sized by allocated bytes and colored by file extension (<see cref="ExtensionColorHelper"/>,
/// same palette as the grid). File tiles carry the exact extension color. File groups arrive
/// pre-grouped with the scan (<see cref="SetTree"/>), so folders and files paint together in
/// a single rebuild.
/// Tiles smaller than <see cref="MIN_TILE"/> are culled like WizTree does.
/// </summary>
public sealed partial class TreemapView : UserControl
{
	private const double MIN_TILE = 8.0d;
	private const double MIN_TILE_AREA = MIN_TILE * MIN_TILE;
	private const double MIN_LABELED_TILE_WIDTH = 52.0d;
	private const double MIN_LABELED_TILE_HEIGHT = 32.0d;
	private const double MIN_FOLDER_AREA = MIN_LABELED_TILE_WIDTH * MIN_LABELED_TILE_HEIGHT;
	private const double HEADER_HEIGHT = 20.0d;
	private const double GAP = 1.0d;
	private const double PAD = 2.0d;
	private const float FOLDER_BORDER_THICKNESS = 1.0f;

	public static readonly DependencyProperty RootProperty =
		DependencyProperty.Register(nameof(Root), typeof(DiskNode), typeof(TreemapView), new PropertyMetadata(null, static (d, _) => ((TreemapView)d).OnRootChanged()));

	public static readonly DependencyProperty SelectedNodeProperty =
		DependencyProperty.Register(nameof(SelectedNode), typeof(DiskNode), typeof(TreemapView), new PropertyMetadata(null, static (d, _) => ((TreemapView)d).UpdateChrome()));

	public DiskNode? Root
	{
		get => (DiskNode?)GetValue(RootProperty);
		set => SetValue(RootProperty, value);
	}

	public DiskNode? SelectedNode
	{
		get => (DiskNode?)GetValue(SelectedNodeProperty);
		set => SetValue(SelectedNodeProperty, value);
	}

	public void SetHighlightedExtension(string? extension)
	{
		if (string.IsNullOrEmpty(extension))
		{
			_highlightExtension = null;
			Rebuild();
			return;
		}

		string normalized = ExtensionColorHelper.IsNoExtension(extension)
			? string.Empty
			: ExtensionColorHelper.NormalizeExtension(extension);
		_highlightExtension = normalized;
		Rebuild();
	}

	public void ClearHighlightedExtension()
	{
		_highlightExtension = null;
		Rebuild();
	}

	public void RefreshTree()
	{
		// Drop file groups whose parent disappeared during a tree refresh (for example,
		// after deleting a folder). Keeping only live parents prevents stale tiles from
		// surviving a later root replacement or re-scan.
		if (_fileGroups is not null)
		{
			var live = new HashSet<DiskNode>();
			if (Root is not null)
			{
				var walk = new Stack<DiskNode>();
				walk.Push(Root);
				while (walk.Count > 0)
				{
					DiskNode node = walk.Pop();
					live.Add(node);
					foreach (DiskNode child in node.Children)
						walk.Push(child);
				}
			}

			foreach (DiskNode parent in _fileGroups.Keys.ToArray())
				if (!live.Contains(parent))
					_fileGroups.Remove(parent);
		}

		_dataGen++;
		Rebuild();
	}

	public void RemoveFileTile(string fullPath)
	{
		if (string.IsNullOrWhiteSpace(fullPath) || _fileGroups is null)
			return;

		bool changed = false;
		foreach (var (parent, files) in _fileGroups)
		{
			string parentPath = parent.FullPath.EndsWith('\\') ? parent.FullPath : parent.FullPath + "\\";
			int before = files.Count;
			files.RemoveAll(file =>
				string.Equals(parentPath + file.Name, fullPath, StringComparison.OrdinalIgnoreCase));
			changed |= files.Count != before;
		}

		_dataGen++;
		if (changed)
			Rebuild();
	}

	private struct TileEntry
	{
		public required long Weight;
		public required DiskNode? Folder;
		public required TreemapFile File;
		public required bool IsFile;
		public DiskNode? FileParent;
	}

	private ContainerVisual? _layer;
	private Compositor? _compositor;
	private readonly List<(Rect Rect, DiskNode Node, TreemapFile? File)> _hits = new(1024);
	private readonly List<Visual> _tiles = new(1024);
	private readonly List<Visual> _folderBorders = new(1024);
	private readonly List<SpriteVisual> _highlightTiles = new(64);
	private readonly SpriteVisual[] _hoverBorder = new SpriteVisual[4];
	private readonly SpriteVisual[] _selectionBorder = new SpriteVisual[4];
	private readonly Dictionary<string, Color> _colorCache = new(StringComparer.OrdinalIgnoreCase);
	// Composition brushes cached by color: a full treemap builds thousands of tiles from
	// ~30 palette colors, and each CreateColorBrush is an interop round-trip. The layer
	// owns them for the control's lifetime (a theme flip adds a second small set).
	private readonly Dictionary<Color, CompositionColorBrush> _brushCache = new();
	private static readonly Color HIGHLIGHT_COLOR = Color.FromArgb(255, 255, 196, 0);
	private Dictionary<DiskNode, List<TreemapFile>>? _fileGroups;
	private int _dataGen;
	private int _hoverIndex = -1;
	private string? _highlightExtension;
	private bool _isRebuilding;
	private DispatcherTimer? _layoutTimer;

	public TreemapView()
	{
		InitializeComponent();
		Loaded += TreemapView_Loaded;
		SizeChanged += TreemapView_SizeChanged;
		ActualThemeChanged += TreemapView_ActualThemeChanged;
		Unloaded += TreemapView_Unloaded;
	}

	private void OnRootChanged()
	{
		_dataGen++;
		Rebuild();
	}

	/// <summary>
	/// Binds a freshly scanned tree plus its pre-grouped file tiles and paints everything in
	/// a single rebuild: folders and files appear together instead of the file layer landing
	/// a beat after the grids. Takes ownership of <paramref name="groups"/> (built once per
	/// scan — delete paths mutate it in place).
	/// </summary>
	public void SetTree(DiskNode? root, Dictionary<DiskNode, List<TreemapFile>>? groups)
	{
		_dataGen++;
		_fileGroups = groups;
		// Root's change callback performs the one rebuild, with the file layer present.
		Root = root;
	}

	private void TreemapView_Loaded(object sender, RoutedEventArgs e)
	{
		_compositor ??= ElementCompositionPreview.GetElementVisual(this).Compositor;
		_layer ??= _compositor.CreateContainerVisual();
		ElementCompositionPreview.SetElementChildVisual(VisualHost, _layer);
		for (int i = 0; i < 4; i++)
		{
			_hoverBorder[i] = _compositor.CreateSpriteVisual();
			_hoverBorder[i].Brush = GetBrush(GetHoverBorderColor());
			_hoverBorder[i].IsVisible = false;
			_layer.Children.InsertAtTop(_hoverBorder[i]);
			_selectionBorder[i] = _compositor.CreateSpriteVisual();
			_selectionBorder[i].Brush = _compositor.CreateColorBrush(Color.FromArgb(255, 0, 120, 215));
			_selectionBorder[i].IsVisible = false;
			_layer.Children.InsertAtTop(_selectionBorder[i]);
		}

		Rebuild();
	}

	private void TreemapView_Unloaded(object sender, RoutedEventArgs e)
	{
		_dataGen++;
		_layoutTimer?.Stop();
		_layoutTimer = null;
	}

	private void TreemapView_ActualThemeChanged(FrameworkElement sender, object args)
	{
		_brushCache.Clear();
		_colorCache.Clear();
		UpdateHoverBorderBrush();
		Rebuild();
	}

	private Color GetHoverBorderColor() => ActualTheme == ElementTheme.Dark
		? Color.FromArgb(255, 255, 255, 255)
		: Color.FromArgb(255, 0, 0, 0);

	private void UpdateHoverBorderBrush()
	{
		if (_compositor == null)
			return;

		// Chrome visuals are created in TreemapView_Loaded, which always runs before any
		// theme change can reach a live control.
		var brush = GetBrush(GetHoverBorderColor());
		foreach (var visual in _hoverBorder)
			visual.Brush = brush;
	}

	private void TreemapView_SizeChanged(object sender, SizeChangedEventArgs e)
	{
		if (e.NewSize.Width <= 0 || e.NewSize.Height <= 0)
			return;
		if (e.NewSize.Width == e.PreviousSize.Width && e.NewSize.Height == e.PreviousSize.Height)
			return;

		_layoutTimer ??= new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(150) };
		_layoutTimer.Tick -= OnLayoutTick;
		_layoutTimer.Tick += OnLayoutTick;
		_layoutTimer.Stop();
		_layoutTimer.Start();
	}

	private void OnLayoutTick(object? sender, object e)
	{
		_layoutTimer?.Stop();
		Rebuild();
	}

	private void Rebuild()
	{
		if (_layer == null || _compositor == null || _isRebuilding)
			return;

		_isRebuilding = true;
		try
		{
			// Drop every tile and folder-outline visual (chrome borders are tracked separately and stay).
			foreach (Visual border in _folderBorders)
				_layer.Children.Remove(border);
			_folderBorders.Clear();
			foreach (Visual tile in _highlightTiles)
				_layer.Children.Remove(tile);
			_highlightTiles.Clear();
			foreach (Visual tile in _tiles)
				_layer.Children.Remove(tile);
			_tiles.Clear();
			LabelLayer.Children.Clear();
			_colorCache.Clear();
			_hits.Clear();
			_hoverIndex = -1;
			UpdateChrome();

			DiskNode? root = Root;
			double width = ActualWidth;
			double height = ActualHeight;
			if (root == null || width <= 0 || height <= 0 || root.Allocated <= 0)
				return;

			Tile(root, new Rect(0, 0, width, height));
		}
		catch (Exception ex)
		{
			Trace.WriteLine($"[Treemap] layout failed: {ex}");
		}
		finally
		{
			_isRebuilding = false;
		}
	}

	private static List<TileEntry> AggregateTinyFiles(List<TileEntry> entries, double width, double height)
	{
		if (entries.Count < 2)
			return entries;

		long totalWeight = 0;
		foreach (var entry in entries)
			totalWeight += entry.Weight;
		if (totalWeight <= 0)
			return entries;

		double scale = width * height / totalWeight;
		var kept = new List<TileEntry>(entries.Count);
		var buckets = new Dictionary<(DiskNode? Parent, string Extension), (long Alloc, long Size)>();
		bool aggregated = false;
		foreach (var entry in entries)
		{
			if (entry.IsFile && entry.Weight * scale < MIN_TILE_AREA)
			{
				aggregated = true;
				var key = (entry.FileParent, entry.File.Extension);
				var bucket = buckets.TryGetValue(key, out var current)
					? (current.Alloc + entry.File.AllocatedSize, current.Size + entry.File.Size)
					: (entry.File.AllocatedSize, entry.File.Size);
				buckets[key] = bucket;
				continue;
			}

			kept.Add(entry);
		}

		if (!aggregated)
			return entries;

		foreach (var (key, totals) in buckets)
		{
			string extension = key.Extension;
			string label = string.IsNullOrEmpty(extension) || extension.Equals("(No Extension)", StringComparison.OrdinalIgnoreCase)
				? "(Other files)"
				: $"(Other {extension} files)";
			kept.Add(new TileEntry
			{
				Weight = totals.Alloc,
				Folder = null,
				File = new TreemapFile(label, extension, totals.Alloc, totals.Size, true),
				IsFile = true,
				FileParent = key.Parent
			});
		}

		return kept;
	}

	private void CollectFileEntries(DiskNode folder, List<TileEntry> output)
	{
		if (_fileGroups is not null && _fileGroups.TryGetValue(folder, out var files))
		{
			foreach (var file in files)
			{
				if (file.AllocatedSize > 0)
					output.Add(new TileEntry { Weight = file.AllocatedSize, Folder = null, File = file, IsFile = true, FileParent = folder });
			}
		}

		foreach (DiskNode child in folder.Children)
			CollectFileEntries(child, output);
	}

	private void Tile(DiskNode node, Rect rect)
	{
		if (rect.Width < MIN_TILE || rect.Height < MIN_TILE || node.Allocated <= 0)
			return;

		// Fallback hit for the folder's own frame (header strip, padding, gaps): hits for
		// children and files are added below and take precedence in HitTest, so this only
		// answers otherwise-dead space such as the "C:" header.
		_hits.Add((rect, node, null));

		// A folder name needs enough room to be legible. Smaller folders are still
		// traversed so their files can occupy the space, but they do not get a frame.
		bool showHeader = rect.Width >= MIN_LABELED_TILE_WIDTH && rect.Height >= MIN_LABELED_TILE_HEIGHT;
		double header = showHeader ? HEADER_HEIGHT : 0;
		if (showHeader)
			AddLabel(new Rect(rect.X + 4, rect.Y + 1, rect.Width - 8, HEADER_HEIGHT), node.Name, null, true, true);

		double innerX = rect.X + PAD;
		double innerY = rect.Y + header + (showHeader ? 1 : PAD);
		double innerW = rect.Width - PAD * 2;
		double innerH = rect.Height - header - (showHeader ? 1 : PAD) - PAD;
		if (innerW < MIN_TILE || innerH < MIN_TILE)
		{
			if (showHeader)
				AddFolderBorder(rect);
			return;
		}

		// One squarified space shared by visible folders and file tiles. Small folders
		// are collapsed so their files compete directly for space instead of disappearing
		// inside an unlabeled frame.
		var entries = new List<TileEntry>(node.Children.Count + 64);
		double scale = node.Allocated > 0 ? innerW * innerH / node.Allocated : 0;
		foreach (DiskNode child in node.Children)
		{
			if (child.Allocated <= 0)
				continue;

			bool collapse = scale > 0 && child.Allocated * scale < MIN_FOLDER_AREA;
			if (collapse)
			{
				var promoted = new List<TileEntry>();
				CollectFileEntries(child, promoted);
				if (promoted.Count > 0)
				{
					entries.AddRange(promoted);
					continue;
				}
			}

			entries.Add(new TileEntry { Weight = child.Allocated, Folder = child, File = default, IsFile = false });
		}

		if (_fileGroups != null && _fileGroups.TryGetValue(node, out var files))
		{
			foreach (var file in files)
			{
				if (file.AllocatedSize > 0)
					entries.Add(new TileEntry { Weight = file.AllocatedSize, Folder = null, File = file, IsFile = true, FileParent = node });
			}
		}

		if (entries.Count == 0)
		{
			if (showHeader)
				AddFolderBorder(rect);
			return;
		}

		entries = AggregateTinyFiles(entries, innerW, innerH);
		entries.Sort(static (a, b) => b.Weight.CompareTo(a.Weight));

		foreach (var (entry, tile) in Squarify(entries, new Rect(innerX, innerY, innerW, innerH)))
		{
			if (tile.Width <= GAP * 2 || tile.Height <= GAP * 2)
				continue;

			if (entry.IsFile)
			{
				_hits.Add((tile, entry.FileParent ?? node, entry.File));
				AddRect(tile, FileColor(entry.File));
				AddHighlight(tile, entry.File.Extension);
				MaybeLabel(entry.File.Name, DiskNode.FormatBytes(entry.File.Size), tile);
			}
			else if (entry.Folder != null)
			{
				DiskNode child = entry.Folder;
				bool hasDirectFiles = _fileGroups is not null &&
					_fileGroups.TryGetValue(child, out var childFiles) &&
					childFiles.Count > 0;
				if ((child.Children.Count > 0 || hasDirectFiles) &&
					tile.Width >= MIN_TILE && tile.Height >= MIN_TILE)
				{
					// Keep laying out files inside narrow/tall or short/wide folders.
					// The folder frame below is reserved for folders with a visible name.
					Tile(child, tile);
				}
				else
				{
					_hits.Add((tile, child, null));
					if (tile.Width >= MIN_LABELED_TILE_WIDTH && tile.Height >= MIN_LABELED_TILE_HEIGHT)
						AddFolderBorder(tile);
					MaybeLabel(child.Name, child.SizeText, tile, true);
				}
			}
		}

		if (showHeader)
			AddFolderBorder(rect);
	}

	private void MaybeLabel(string name, string sizeText, Rect tile, bool isFolder = false)
	{
		if (tile.Width < MIN_LABELED_TILE_WIDTH || tile.Height < MIN_LABELED_TILE_HEIGHT)
			return;

		AddLabel(new Rect(tile.X + 3, tile.Y + 2, tile.Width - 6, tile.Height - 4), name, sizeText, false, isFolder);
	}

	private Color FileColor(TreemapFile file)
		=> TileColor(file.Extension);

	private CompositionColorBrush GetBrush(Color color)
	{
		if (!_brushCache.TryGetValue(color, out CompositionColorBrush? brush) || brush is null)
		{
			brush = _compositor!.CreateColorBrush(color);
			_brushCache[color] = brush;
		}

		return brush;
	}

	private Color TileColor(string extension)
	{
		if (!_colorCache.TryGetValue(extension, out Color color))
		{
			color = ExtensionColorHelper.GetColor(extension);
			_colorCache[extension] = color;
		}

		return color;
	}

	private void AddRect(Rect rect, Color color)
	{
		if (_layer == null || _compositor == null)
			return;
		if (rect.Width <= GAP * 2 || rect.Height <= GAP * 2)
			return;

		var visual = _compositor.CreateSpriteVisual();
		visual.Offset = new Vector3((float)(rect.X + GAP), (float)(rect.Y + GAP), 0);
		visual.Size = new Vector2((float)(rect.Width - GAP * 2), (float)(rect.Height - GAP * 2));
		visual.Brush = GetBrush(color);
		// Chrome borders stay on top by inserting content directly beneath them.
		_layer.Children.InsertBelow(visual, _hoverBorder[0]);
		_tiles.Add(visual);
	}

	private void AddFolderBorder(Rect rect)
	{
		if (_layer == null || _compositor == null || rect.Width <= 0 || rect.Height <= 0)
			return;

		var brush = GetBrush(GetFolderBorderColor());
		const float thickness = FOLDER_BORDER_THICKNESS;
		float x = (float)rect.X;
		float y = (float)rect.Y;
		float w = (float)rect.Width;
		float h = (float)rect.Height;

		var top = _compositor.CreateSpriteVisual();
		top.Brush = brush;
		top.Offset = new Vector3(x, y, 0);
		top.Size = new Vector2(w, thickness);
		var bottom = _compositor.CreateSpriteVisual();
		bottom.Brush = brush;
		bottom.Offset = new Vector3(x, y + h - thickness, 0);
		bottom.Size = new Vector2(w, thickness);
		var left = _compositor.CreateSpriteVisual();
		left.Brush = brush;
		left.Offset = new Vector3(x, y, 0);
		left.Size = new Vector2(thickness, h);
		var right = _compositor.CreateSpriteVisual();
		right.Brush = brush;
		right.Offset = new Vector3(x + w - thickness, y, 0);
		right.Size = new Vector2(thickness, h);

		foreach (Visual border in new[] { top, bottom, left, right })
		{
			_layer.Children.InsertBelow(border, _hoverBorder[0]);
			_folderBorders.Add(border);
		}
	}

	private void AddHighlight(Rect rect, string extension)
	{
		if (_highlightExtension is null ||
			!extension.Equals(_highlightExtension, StringComparison.OrdinalIgnoreCase) ||
			_layer == null || _compositor == null || rect.Width <= 0 || rect.Height <= 0)
			return;

		var visual = _compositor.CreateSpriteVisual();
		visual.Offset = new Vector3((float)rect.X, (float)rect.Y, 0);
		visual.Size = new Vector2((float)rect.Width, (float)rect.Height);
		visual.Brush = GetBrush(HIGHLIGHT_COLOR);
		visual.Opacity = 0.35f;
		_layer.Children.InsertBelow(visual, _hoverBorder[0]);
		_highlightTiles.Add(visual);
	}

	private void AddLabel(Rect rect, string name, string? secondLine, bool isHeader, bool isFolder = false)
	{
		if (rect.Width <= 0 || rect.Height <= 0)
			return;

		Brush foreground = isFolder
			? GetThemeBrush("TextFillColorPrimaryBrush", ActualTheme == ElementTheme.Dark
				? Color.FromArgb(255, 255, 255, 255)
				: Color.FromArgb(255, 0, 0, 0))
			: new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
		Brush secondaryForeground = isFolder
			? GetThemeBrush("TextFillColorSecondaryBrush", ActualTheme == ElementTheme.Dark
				? Color.FromArgb(190, 255, 255, 255)
				: Color.FromArgb(150, 0, 0, 0))
			: new SolidColorBrush(Color.FromArgb(190, 255, 255, 255));

		var text = new TextBlock
		{
			Width = rect.Width,
			Height = rect.Height,
			TextTrimming = TextTrimming.CharacterEllipsis,
			TextWrapping = TextWrapping.NoWrap,
			MaxLines = isHeader ? 1 : 2,
			LineHeight = 14,
			FontSize = 11,
			FontWeight = isHeader ? Microsoft.UI.Text.FontWeights.SemiBold : Microsoft.UI.Text.FontWeights.Normal,
			Foreground = foreground,
			IsHitTestVisible = false
		};
		text.Inlines.Add(new Run { Text = name });
		if (!isHeader && secondLine != null)
		{
			text.Inlines.Add(new LineBreak());
			var sizeRun = new Run { Text = secondLine, FontSize = 10, Foreground = secondaryForeground };
			text.Inlines.Add(sizeRun);
		}

		Canvas.SetLeft(text, rect.X);
		Canvas.SetTop(text, rect.Y);
		LabelLayer.Children.Add(text);
	}

	private Brush GetThemeBrush(string key, Color fallback)
	{
		object? resource = Application.Current?.Resources[key];
		return resource is Brush brush ? brush : new SolidColorBrush(fallback);
	}

	private Color GetFolderBorderColor()
	{
		object? resource = Application.Current?.Resources["ControlStrokeColorDefaultBrush"];
		if (resource is SolidColorBrush brush)
			return brush.Color;

		return ActualTheme == ElementTheme.Dark
			? Color.FromArgb(255, 96, 96, 96)
			: Color.FromArgb(255, 160, 160, 160);
	}

	/// <summary>
	/// Standard squarified treemap (Bruls et al.) over pre-sorted entries.
	/// </summary>
	private static List<(TileEntry Entry, Rect Rect)> Squarify(List<TileEntry> entries, Rect bounds)
	{
		var result = new List<(TileEntry, Rect)>(entries.Count);
		double total = 0;
		foreach (var entry in entries)
			total += entry.Weight;
		if (total <= 0 || bounds.Width <= 0 || bounds.Height <= 0)
			return result;

		double scale = bounds.Width * bounds.Height / total;
		var row = new List<(TileEntry Entry, double Area)>();
		double x = bounds.X;
		double y = bounds.Y;
		double w = bounds.Width;
		double h = bounds.Height;

		void LayoutRow()
		{
			if (row.Count == 0)
				return;

			double rowArea = 0;
			foreach (var item in row)
				rowArea += item.Area;
			bool horizontal = w >= h;
			double offset = 0;
			if (horizontal)
			{
				double rowH = rowArea / w;
				foreach (var item in row)
				{
					double tileW = rowArea > 0 ? item.Area / rowH : 0;
					result.Add((item.Entry, new Rect(x + offset, y, tileW, rowH)));
					offset += tileW;
				}

				y += rowH;
				h -= rowH;
			}
			else
			{
				double rowW = rowArea / h;
				foreach (var item in row)
				{
					double tileH = rowArea > 0 ? item.Area / rowW : 0;
					result.Add((item.Entry, new Rect(x, y + offset, rowW, tileH)));
					offset += tileH;
				}

				x += rowW;
				w -= rowW;
			}

			row.Clear();
		}

		static double Worst(List<(TileEntry Entry, double Area)> candidate, int count, double side)
		{
			double max = 0;
			double min = double.MaxValue;
			double sum = 0;
			for (int i = 0; i < count; i++)
			{
				double area = candidate[i].Area;
				if (area > max)
					max = area;
				if (area < min)
					min = area;
				sum += area;
			}

			if (min <= 0 || sum <= 0 || side <= 0)
				return double.MaxValue;
			return Math.Max(side * side * max / (sum * sum), sum * sum / (side * side * min));
		}

		foreach (var entry in entries)
		{
			double area = entry.Weight * scale;
			double side = w >= h ? w : h;
			row.Add((entry, area));
			if (row.Count > 1 && Worst(row, row.Count, side) > Worst(row, row.Count - 1, side))
			{
				var last = row[^1];
				row.RemoveAt(row.Count - 1);
				LayoutRow();
				row.Add(last);
			}
		}

		LayoutRow();
		return result;
	}

	private void HitOverlay_PointerMoved(object sender, PointerRoutedEventArgs e)
	{
		int index = HitTest(e.GetCurrentPoint(HitOverlay).Position);
		if (index == _hoverIndex)
			return;

		_hoverIndex = index;
		if (index < 0)
		{
			HoverTip.IsOpen = false;
		}
		else
		{
			var (rect, node, file) = _hits[index];
			if (file is TreemapFile f)
			{
				string path = node.FullPath + "\\" + f.Name;
				string extension = string.IsNullOrEmpty(f.Extension) ? "(No Extension)" : f.Extension;
				string aggregateNote = f.IsAggregate ? " (aggregated small files)" : string.Empty;
				HoverTip.Content = $"{path}\n{extension} — {DiskNode.FormatBytes(f.AllocatedSize)} allocated ({DiskNode.FormatBytes(f.Size)} size){aggregateNote}";
			}
			else
			{
				HoverTip.Content = $"{node.FullPath}\n{node.SizeText} ({node.Size:N0} bytes)\n{node.PercentOfParent:F2}% of parent";
			}

			// Anchor the tip to the hovered tile: the zero-footprint anchor sits on the
			// tile top, so Top placement opens above the tile, centered, with automatic
			// screen-edge flipping. The close must complete before reopening — a synchronous
			// close/open pair is coalesced and the tip stays frozen — so the reopen runs
			// on the next tick, which also lets layout settle the anchor first.
			HoverTip.Placement = PlacementMode.Top;
			TooltipAnchor.Margin = new Thickness(rect.X, rect.Y, 0, 0);
			TooltipAnchor.Width = Math.Max(rect.Width, 1);
			TooltipAnchor.Height = 1;
			HoverTip.IsOpen = false;
			int captured = index;
			DispatcherQueue.TryEnqueue(() =>
			{
				if (_hoverIndex == captured)
					HoverTip.IsOpen = true;
			});
		}

		UpdateChrome();
	}

	private void HitOverlay_PointerExited(object sender, PointerRoutedEventArgs e)
	{
		_hoverIndex = -1;
		HoverTip.IsOpen = false;
		UpdateChrome();
	}

	private void HitOverlay_PointerPressed(object sender, PointerRoutedEventArgs e)
	{
		int index = HitTest(e.GetCurrentPoint(HitOverlay).Position);
		if (index >= 0)
			SelectedNode = _hits[index].Node;
	}

	private int HitTest(Point position)
	{
		for (int i = _hits.Count - 1; i >= 0; i--)
		{
			var (rect, _, _) = _hits[i];
			if (position.X >= rect.X && position.X < rect.X + rect.Width &&
				position.Y >= rect.Y && position.Y < rect.Y + rect.Height)
				return i;
		}

		return -1;
	}

	private void UpdateChrome()
	{
		if (_layer == null)
			return;

		bool hasHover = _hoverIndex >= 0 && _hoverIndex < _hits.Count;
		// Selection marker: the folder tile itself when a folder is hovered/selected,
		// otherwise the parent tile of a hovered file.
		DiskNode? selected = SelectedNode;
		int selectIndex = -1;
		if (selected != null)
		{
			for (int i = _hits.Count - 1; i >= 0; i--)
			{
				if (ReferenceEquals(_hits[i].Node, selected) && _hits[i].File == null)
				{
					selectIndex = i;
					break;
				}
			}

			if (selectIndex < 0)
			{
				for (int i = _hits.Count - 1; i >= 0; i--)
				{
					if (ReferenceEquals(_hits[i].Node, selected))
					{
						selectIndex = i;
						break;
					}
				}
			}
		}

		bool showHover = hasHover && (selectIndex < 0 || _hoverIndex != selectIndex || _hits[_hoverIndex].File != null);
		Rect hoverRect = showHover ? _hits[_hoverIndex].Rect : default;
		Rect selectRect = selectIndex >= 0 ? _hits[selectIndex].Rect : default;

		PlaceBorder(_hoverBorder, showHover, hoverRect);
		PlaceBorder(_selectionBorder, selectIndex >= 0, selectRect);
	}

	private void PlaceBorder(SpriteVisual[] border, bool visible, Rect rect)
	{
		foreach (var visual in border)
			visual.IsVisible = visible;
		if (!visible)
			return;

		const float thickness = 2f;
		float x = (float)rect.X;
		float y = (float)rect.Y;
		float w = (float)rect.Width;
		float h = (float)rect.Height;
		border[0].Offset = new Vector3(x, y, 0);
		border[0].Size = new Vector2(w, thickness);
		border[1].Offset = new Vector3(x, y + h - thickness, 0);
		border[1].Size = new Vector2(w, thickness);
		border[2].Offset = new Vector3(x, y, 0);
		border[2].Size = new Vector2(thickness, h);
		border[3].Offset = new Vector3(x + w - thickness, y, 0);
		border[3].Size = new Vector2(thickness, h);
	}
}
