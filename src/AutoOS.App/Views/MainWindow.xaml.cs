using AutoOS.App.Assets.NavViewMenu;
using Microsoft.UI.Input;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.Win32;
using Windows.Win32.Foundation;
using WinRT;
using WinRT.Interop;

namespace AutoOS.App.Views;

public sealed partial class MainWindow : Window
{
	public string TitleBarName { get; set; } = string.Empty;
	internal static MainWindow Instance { get; set; } = null!;

	public MainWindow()
	{
		Instance = this;
		InitializeComponent();
		ExtendsContentIntoTitleBar = true;
		SetTitleBar(AppTitleBar);
		AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
		_ = new ModernSystemMenu(this);

		OverlappedPresenter presenter = AppWindow.Presenter.As<OverlappedPresenter>();
		presenter.PreferredMinimumWidth = 660;
		presenter.PreferredMinimumHeight = 715;

		RootGrid.PointerPressed += OnPointerPressed;

		if (App.IsInstalled)
		{
			App.Current.NavService
				.Initialize(NavView, NavFrame, NavigationPageMappingsSettings.PageDictionary)
				.ConfigureDefaultPage(typeof(Settings.HomePage))
				.ConfigureSettingsPage(typeof(Settings.SettingsPage))
				.ConfigureJsonFile("Assets/NavViewMenu/Settings.json")
				.ConfigureTitleBar(AppTitleBar, false)
				.ConfigureBreadcrumbBar(BreadCrumbNav, BreadcrumbPageMappingsSettings.PageDictionary);
			AppTitleBar.Title = "AutoOS Settings";

			NavView.IsSettingsVisible = true;
		}
		else
		{
			App.Current.NavService
				.Initialize(NavView, NavFrame, NavigationPageMappingsInstaller.PageDictionary)
				.ConfigureDefaultPage((Windows.Storage.ApplicationData.Current.LocalSettings.Values["actionStage"] as int? ?? -1) > 0 ? typeof(Installer.InstallPage) : typeof(Installer.HomePage))
				.ConfigureJsonFile("Assets/NavViewMenu/Installer.json")
				.ConfigureTitleBar(AppTitleBar, false)
				.ConfigureBreadcrumbBar(BreadCrumbNav, BreadcrumbPageMappingsInstaller.PageDictionary);
			AppTitleBar.Title = "AutoOS Installer";

			presenter.Maximize();
		}
	}

	private async void RootGrid_Loaded(object sender, RoutedEventArgs e)
	{
		if (!App.IsInstalled)
		{
			await Task.Delay(100);
			foreach (NavigationViewItem item in NavView.FooterMenuItems.OfType<NavigationViewItem>())
			{
				item.IsEnabled = false;
			}
		}
	}

	private readonly HashSet<string> _visitedPages = [];
	public IReadOnlyCollection<string> VisitedPages => _visitedPages;

	public readonly string[] AllPages =
	[
		"PersonalizationPage",
		"AppsPage",
		"BrowsersPage",
		"DisplaysPage",
		"GraphicsPage",
		"SecurityPage"
	];

	public void MarkVisited(string pageName)
	{
		_visitedPages.Add(pageName);
	}

	public bool AllPagesVisited()
	{
		return AllPages.All(page => _visitedPages.Contains(page));
	}

	public void CheckAllPagesVisited()
	{
		if (AllPagesVisited())
		{
			NavigationView navView = GetNavView();
			foreach (NavigationViewItem item in navView.FooterMenuItems.OfType<NavigationViewItem>())
			{
				item.IsEnabled = true;
			}
		}
	}

	public NavigationView GetNavView()
	{
		return NavView;
	}

	public TitleBar GetTitleBar()
	{
		return AppTitleBar;
	}

	private static void NavSearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
	{
		if (args.Reason != AutoSuggestionBoxTextChangeReason.UserInput)
		{
			return;
		}

		string query = sender.Text.Trim();
		if (query.Length == 0)
		{
			sender.ItemsSource = null;
			return;
		}

		string[] querySplit = query.Split(' ', StringSplitOptions.RemoveEmptyEntries);
		List<DataItem> suggestions = [];
		foreach (DataGroup group in DataSource.Instance.Groups)
		{
			foreach (DataItem item in group.Items)
			{
				if (!item.IncludedInBuild || item.IsSeparator || item.Title == null)
				{
					continue;
				}

				bool match = true;
				foreach (string token in querySplit)
				{
					if (item.Title.IndexOf(token, StringComparison.OrdinalIgnoreCase) < 0)
					{
						match = false;
						break;
					}
				}

				if (match)
				{
					suggestions.Add(item);
				}
			}
		}

		if (suggestions.Count > 0)
		{
			sender.ItemsSource = suggestions
				.OrderByDescending(item => item.Title?.StartsWith(query, StringComparison.OrdinalIgnoreCase) ?? false)
				.ThenBy(item => item.Title, StringComparer.CurrentCulture)
				.ToList();
		}
		else
		{
			sender.ItemsSource = new List<DataItem>
			{
				new()
				{
					Title = "No result found",
					ImagePath = "ms-appx:///Assets/AppIcon.png"
				}
			};
		}
	}

	private static void NavSearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
	{
		if (args.ChosenSuggestion is DataItem item)
		{
			App.Current.NavService.NavigateTo(item.UniqueId, item);
		}
	}

	private void NavSearchBox_AcceleratorInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
	{
		NavSearchBox.Focus(FocusState.Programmatic);
	}

	private void AppIcon_PointerPressed(object sender, PointerRoutedEventArgs e)
	{
		PInvoke.PostMessage((HWND)WindowNative.GetWindowHandle(App.MainWindow), PInvoke.WM_SYSCOMMAND, 0xF090, 0);
	}

	private void OnPointerPressed(object sender, PointerRoutedEventArgs e)
	{
		PointerPoint pointerPoint = e.GetCurrentPoint(RootGrid);
		PointerPointProperties properties = pointerPoint.Properties;

		if (properties.IsXButton1Pressed)
		{
			if (App.Current.NavService.CanGoBack)
			{
				App.Current.NavService.GoBack();
			}
			e.Handled = true;
		}
		else if (properties.IsXButton2Pressed)
		{
			if (NavFrame.CanGoForward)
			{
				NavFrame.GoForward();
			}
			e.Handled = true;
		}
	}
}
