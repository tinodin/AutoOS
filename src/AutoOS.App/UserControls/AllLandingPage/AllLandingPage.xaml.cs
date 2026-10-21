using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace AutoOS.App.UserControls;

public sealed partial class AllLandingPage : ItemsPageBase
{
	public static readonly DependencyProperty HeaderContentProperty =
		DependencyProperty.Register(nameof(HeaderContent), typeof(object), typeof(AllLandingPage), new PropertyMetadata(null));

	public object? HeaderContent
	{
		get => GetValue(HeaderContentProperty);
		set => SetValue(HeaderContentProperty, value);
	}

	public AllLandingPage()
	{
		InitializeComponent();
		GridViewPadding = new Thickness(24, 0, 24, 24);
		Loading -= AllLandingPage_Loading;
		Loading += AllLandingPage_Loading;
		OnItemClick += AllLandingPage_OnItemClick;
	}

	private void AllLandingPage_Loading(FrameworkElement sender, object args)
	{
		if (CanExecuteInternalCommand)
		{
			DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () => GetData(item => item.Title));
		}
	}

	private void AllLandingPage_OnItemClick(object? sender, RoutedEventArgs e)
	{
		if (e is ItemClickEventArgs args && args.ClickedItem is DataItem item)
		{
			JsonNavigationService?.NavigateTo(item.UniqueId, item);
		}
	}

	public void GetData()
	{
		Items = GetAllItems().ToList();
	}

	public void GetData(Func<DataItem, object>? orderBy, bool descending = false)
	{
		IEnumerable<DataItem> items = GetAllItems();
		if (orderBy != null)
		{
			items = descending ? items.OrderByDescending(orderBy) : items.OrderBy(orderBy);
		}

		Items = items.ToList();
	}

	public async Task GetDataAsync(string jsonFilePath, PathType pathType = PathType.Relative)
	{
		await DataSource.Instance.GetGroupsAsync(jsonFilePath, pathType);

		Items = GetAllItems().ToList();
	}

	public void OrderBy(Func<DataItem, object>? orderBy = null)
	{
		Items = orderBy != null
			? Items.OrderBy(orderBy).ToList()
			: Items.OrderBy(item => item.Title).ToList();
	}

	public void OrderByDescending(Func<DataItem, object>? orderByDescending = null)
	{
		Items = orderByDescending != null
			? Items.OrderByDescending(orderByDescending).ToList()
			: Items.OrderByDescending(item => item.Title).ToList();
	}

	private static IEnumerable<DataItem> GetAllItems()
	{
		return DataSource.Instance.Groups
			.Where(group => !group.HideGroup && !group.IsSpecialSection)
			.SelectMany(group => group.Items)
			.Where(item => !item.HideItem);
	}
}
