using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Windows.Foundation;

namespace AutoOS.App.UserControls.StoreCarousel2;

public class PreviewViewerHelperListViewSource(ListViewBase listView)
{
	private UIElement? _hiddenSourceElement;

	public ListViewBase InnerListView { get; set; } = listView;

	public void RestoreSourceVisibility()
	{
		_hiddenSourceElement?.Opacity = 1;
		_hiddenSourceElement = null;
	}

	public static bool GetAnimationSourceElementName(object item, out string name)
	{
		name = string.Empty;
		if (item is ScreenshotTileItem)
		{
			name = "TileImageRootGrid";
		}
		else if (item is VideoPlayerSource)
		{
			name = "VideoPreviewImage";
		}

		return !string.IsNullOrEmpty(name);
	}

	public void PrepareForwardConnectedAnimation(object dataItem)
	{
		if (!GetAnimationSourceElementName(dataItem, out string name))
		{
			return;
		}

		if (InnerListView == null)
		{
			return;
		}

		var container = InnerListView.ContainerFromItem(dataItem) as FrameworkElement;
		if (container == null && dataItem is int idx)
		{
			container = InnerListView.ContainerFromIndex(idx) as FrameworkElement;
		}

		if (container == null || !IsVisibleWithNonZeroSize(container))
		{
			return;
		}

		RestoreSourceVisibility();

		if (container.ActualWidth > 0 && double.IsNaN(container.Width))
		{
			container.Width = container.ActualWidth;
		}
		if (container.ActualHeight > 0 && double.IsNaN(container.Height))
		{
			container.Height = container.ActualHeight;
		}

		UIElement source = container.FindDescendantOrSelf(name) ?? container;
		if (source is FrameworkElement sourceFe)
		{
			if (sourceFe.ActualWidth > 0 && double.IsNaN(sourceFe.Width))
			{
				sourceFe.Width = sourceFe.ActualWidth;
			}
			if (sourceFe.ActualHeight > 0 && double.IsNaN(sourceFe.Height))
			{
				sourceFe.Height = sourceFe.ActualHeight;
			}
		}

		ConnectedAnimation? anim = null;

		try
		{
			anim = InnerListView.PrepareConnectedAnimation("screenshotForwardAnimation", dataItem, name);
		}
		catch { }

		if (anim == null && source != null)
		{
			var service = ConnectedAnimationService.GetForCurrentView();
			anim = service.PrepareToAnimate("screenshotForwardAnimation", source);
		}

		anim?.Configuration = new DirectConnectedAnimationConfiguration();

		if (source != null)
		{
			source.Opacity = 0;
			_hiddenSourceElement = source;
		}
	}

	public void ScrollToIndex(int index)
	{
		UpdateSelectedIndex(index);
	}

	public void UpdateSelectedIndex(int index)
	{
		if (InnerListView == null || InnerListView.Items == null || index < 0 || index >= InnerListView.Items.Count)
		{
			return;
		}

		ScrollViewer? scrollViewer = InnerListView.FindDescendant<ScrollViewer>();
		if (scrollViewer == null)
		{
			InnerListView.ScrollIntoView(InnerListView.Items[index]);
			return;
		}

		if (InnerListView.ContainerFromIndex(index) is not FrameworkElement container)
		{
			InnerListView.ScrollIntoView(InnerListView.Items[index]);
			_ = InnerListView.DispatcherQueue?.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () => FlushItemToEdge(index, scrollViewer));
			return;
		}

		FlushItemToEdge(index, scrollViewer, container);
	}

	public async Task StartBackwardConnectedAnimationAsync(object dataItem)
	{
		if (!GetAnimationSourceElementName(dataItem, out string name))
		{
			return;
		}

		ConnectedAnimation? animation = ConnectedAnimationService.GetForCurrentView().GetAnimation("screenshotBackAnimation");
		if (animation == null || InnerListView == null)
		{
			return;
		}

		var container = InnerListView.ContainerFromItem(dataItem) as FrameworkElement;
		if (container == null && dataItem is int idx)
		{
			container = InnerListView.ContainerFromIndex(idx) as FrameworkElement;
		}

		animation.Configuration = new DirectConnectedAnimationConfiguration();

		bool started = false;
		try
		{
			started = await InnerListView.TryStartConnectedAnimationAsync(animation, dataItem, name);
		}
		catch { }

		if (!started && container != null)
		{
			UIElement target = container.FindDescendantOrSelf(name) ?? container;
			target?.Opacity = 1;
			animation.TryStart(target);
		}

		RestoreSourceVisibility();

		if (container != null)
		{
			container.ClearValue(FrameworkElement.WidthProperty);
			container.ClearValue(FrameworkElement.HeightProperty);
		}

		await Task.CompletedTask;
	}

	private void FlushItemToEdge(int index, ScrollViewer scrollViewer, FrameworkElement? container = null)
	{
		if (scrollViewer == null || InnerListView == null)
		{
			return;
		}

		container ??= InnerListView.ContainerFromIndex(index) as FrameworkElement;
		if (container == null)
		{
			return;
		}

		if (scrollViewer.Content is not UIElement content)
		{
			return;
		}

		Point origin;
		try
		{
			origin = container.TransformToVisual(content).TransformPoint(new Point(0, 0));
		}
		catch
		{
			return;
		}

		double itemLeft = origin.X;
		double itemRight = itemLeft + container.ActualWidth;

		double viewportWidth = scrollViewer.ViewportWidth > 0 ? scrollViewer.ViewportWidth : scrollViewer.ActualWidth;
		double currentOffset = scrollViewer.HorizontalOffset;
		double viewportRight = currentOffset + viewportWidth;

		const double epsilon = 1.0;
		if (itemLeft >= currentOffset - epsilon && itemRight <= viewportRight + epsilon)
		{
			return;
		}

		double targetOffset = itemLeft < currentOffset ? itemLeft : itemRight - viewportWidth;

		if (targetOffset < 0)
			targetOffset = 0;
		if (targetOffset > scrollViewer.ScrollableWidth)
			targetOffset = scrollViewer.ScrollableWidth;

		scrollViewer.ChangeView(targetOffset, null, null, disableAnimation: true);
	}

	private static bool IsVisibleWithNonZeroSize(FrameworkElement element)
	{
		if (element == null)
			return false;
		return element.Visibility == Visibility.Visible && element.ActualWidth > 0 && element.ActualHeight > 0;
	}
}
