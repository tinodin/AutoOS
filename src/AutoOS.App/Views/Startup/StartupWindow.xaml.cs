using AutoOS.App.Views.Startup.Stages;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.Win32;
using Windows.Win32.Foundation;
using WinRT.Interop;

namespace AutoOS.App.Views.Startup;

public sealed partial class StartupWindow : Window
{
	public string TitleBarName { get; set; } = null!;
	public static TextBlock Status { get; private set; } = null!;
	public static ProgressBar Progress { get; private set; } = null!;

	public StartupWindow()
	{
		InitializeComponent();
		ExtendsContentIntoTitleBar = true;
		SetTitleBar(AppTitleBar);
		AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
		AppWindow.IsShownInSwitchers = false;
		new ModernSystemMenu(this);

		uint dpi = PInvoke.GetDpiForWindow((HWND)WindowNative.GetWindowHandle(this));
		App.Scaling = dpi / 96.0;

		((OverlappedPresenter)AppWindow.Presenter).PreferredMaximumWidth = (int)(340 * App.Scaling);
		((OverlappedPresenter)AppWindow.Presenter).PreferredMaximumHeight = (int)(130 * App.Scaling);
		((OverlappedPresenter)AppWindow.Presenter).IsResizable = false;
		((OverlappedPresenter)AppWindow.Presenter).IsMaximizable = false;
		((OverlappedPresenter)AppWindow.Presenter).IsAlwaysOnTop = true;
		((OverlappedPresenter)AppWindow.Presenter).SetBorderAndTitleBar(true, false);

		StartupWindow_Loaded();
	}

	private async void StartupWindow_Loaded()
	{
		Status = StatusText;
		Progress = ProgressBar;
		TitleBarName = "AutoOS Startup";

		await StartupStage.Run();
	}

	private void AppIcon_PointerPressed(object sender, PointerRoutedEventArgs e)
	{
		PInvoke.PostMessage((HWND)WindowNative.GetWindowHandle(App.MainWindow), PInvoke.WM_SYSCOMMAND, 0xF090, 0);
	}
}
