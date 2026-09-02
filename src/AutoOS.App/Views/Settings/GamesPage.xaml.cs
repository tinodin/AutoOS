using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using WinRT.Interop;

namespace AutoOS.App.Views.Settings;

public sealed partial class GamesPage : Page
{
	public static GamesPage Instance { get; private set; } = null!;
	public Games.HeaderCarousel Games => games;

	public GamesPage()
	{
		Instance = this;
		InitializeComponent();

	}

	private void ToggleFullscreen(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
	{
		IntPtr hWnd = WindowNative.GetWindowHandle(App.MainWindow);
		WindowId windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
		AppWindow appWindow = AppWindow.GetFromWindowId(windowId);

		NavigationView navView = MainWindow.Instance.GetNavView();
		TitleBar titleBar = MainWindow.Instance.GetTitleBar();

		if (appWindow.Presenter.Kind == AppWindowPresenterKind.FullScreen)
		{
			appWindow.SetPresenter(AppWindowPresenterKind.Overlapped);
			navView.IsPaneVisible = true;
			titleBar.Visibility = Visibility.Visible;
		}
		else
		{
			appWindow.SetPresenter(AppWindowPresenterKind.FullScreen);
			navView.IsPaneVisible = false;
			titleBar.Visibility = Visibility.Collapsed;
		}

		args.Handled = true;
	}
}
