using AutoOS.Core.Helpers.Logging;
using AutoOS.Core;
using AutoOS.Views.Installer.Stages;
using AutoOS.Views.Startup;
using AutoOS.Views;
using Microsoft.UI.Windowing;
using Microsoft.Win32;
using Microsoft.Windows.AppLifecycle;
using Windows.Graphics;
using Windows.Storage;
using WinRT.Interop;

namespace AutoOS
{
	public partial class App : Application
	{
		public new static App Current => (App)Application.Current;
		public static Window MainWindow = Window.Current;
		public static IntPtr Hwnd => WindowNative.GetWindowHandle(MainWindow);
		public JsonNavigationService NavService { get; set; }
		public IThemeService ThemeService { get; set; }
		internal static bool IsInstalled { get; private set; }
		internal static double Scaling { get; set; }
		private readonly ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

		public App()
		{
			InitializeComponent();
			NavService = new JsonNavigationService();
			IsInstalled = (Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\AutoOS", "IsInstalled", 0) as int? ?? 0) == 1 || Registry.CurrentUser.OpenSubKey(@"SOFTWARE\AutoOS")?.GetValue("Stage") as string == "Installed";
			Application.Current.UnhandledException += Current_UnhandledException;
			AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
			TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
			Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense(Secrets.Syncfusion);
		}

		protected override void OnLaunched(LaunchActivatedEventArgs args)
		{
			if (IsInstalled)
			{
				AppActivationArguments appActivationArguments = AppInstance.GetCurrent().GetActivatedEventArgs();

				if (appActivationArguments.Kind is ExtendedActivationKind.StartupTask)
				{
					MainWindow = new StartupWindow();
					MainWindow.Title = MainWindow.AppWindow.Title = "AutoOS Startup";
					MainWindow.AppWindow.SetIcon("Assets/AppIcon.ico");

					Window window = MainWindow;
					var monitor = DisplayMonitorHelper.GetMonitorInfo(window);
					int X = (int)monitor.RectMonitor.Width;
					int Y = (int)monitor.RectMonitor.Height;

					int windowWidth = (int)(340 * Scaling);
					int windowHeight = (int)(130 * Scaling);

					int posX = X - windowWidth - (int)(10 * Scaling);
					int posY = Y - windowHeight - (int)(53 * Scaling);

					MainWindow.AppWindow.MoveAndResize(new RectInt32(posX, posY, windowWidth, windowHeight));

					if (!localSettings.Values.TryGetValue("HideStartup", out object value) || (int)value == 0)
					{
						MainWindow.Activate();
					}
				}
				else
				{
					MainWindow = new MainWindow();
					MainWindow.Title = MainWindow.AppWindow.Title = "AutoOS Settings";
					MainWindow.AppWindow.SetIcon("Assets/AppIcon.ico");
					ThemeService = new ThemeService().Initialize(MainWindow);

					if (localSettings.Values.TryGetValue("RestoreWindowState", out object restore) && (bool)restore)
					{
						if (localSettings.Values.TryGetValue("WindowPositionX", out object posX) && localSettings.Values.TryGetValue("WindowPositionY", out object posY) && localSettings.Values.TryGetValue("WindowWidth", out object windowWidth) && localSettings.Values.TryGetValue("WindowHeight", out object windowHeight))
						{
							int x = (int)posX;
							int y = (int)posY;
							int width = (int)windowWidth;
							int height = (int)windowHeight;

							MainWindow.AppWindow.MoveAndResize(new RectInt32(x, y, width, height));
						}

						if (localSettings.Values.TryGetValue("IsMaximized", out object isMaximized) && (bool)isMaximized)
						{
							if (MainWindow.AppWindow.Presenter is OverlappedPresenter overlappedPresenter)
							{
								overlappedPresenter.Maximize();
							}
						}
					}
					else
					{
						WindowHelper.ResizeAndCenterWindowToPercentageOfWorkArea(MainWindow, 92.9);
					}

					MainWindow.AppWindow.Changed += (s, e) =>
					{
						if (localSettings.Values.TryGetValue("RestoreWindowState", out object restoreState) && (bool)restoreState)
						{
							if (MainWindow.AppWindow.Presenter is OverlappedPresenter overlappedPresenter)
							{
								if (overlappedPresenter.State == OverlappedPresenterState.Minimized)
									return;

								localSettings.Values["IsMaximized"] = overlappedPresenter.State == OverlappedPresenterState.Maximized;

								if (overlappedPresenter.State == OverlappedPresenterState.Maximized)
									return;
							}

							if (e.DidPositionChange || e.DidSizeChange)
							{
								var pos = MainWindow.AppWindow.Position;
								var size = MainWindow.AppWindow.Size;
								localSettings.Values["WindowPositionX"] = pos.X;
								localSettings.Values["WindowPositionY"] = pos.Y;
								localSettings.Values["WindowWidth"] = size.Width;
								localSettings.Values["WindowHeight"] = size.Height;
							}
						}
					};

					MainWindow.Activate();
				}
			}
			else
			{
				MainWindow = new MainWindow();
				MainWindow.Title = MainWindow.AppWindow.Title = "AutoOS Installer";
				MainWindow.AppWindow.SetIcon("Assets/AppIcon.ico");

				ThemeService = new ThemeService().Initialize(MainWindow);

				MainWindow.Activate();
			}
		}

		private async void Current_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
		{
			e.Handled = true;
			await ShowErrorMessage(e.Exception);
		}

		private void CurrentDomain_UnhandledException(object sender, System.UnhandledExceptionEventArgs e)
		{
			if (e.ExceptionObject is Exception ex)
			{
				_ = ShowErrorMessage(ex);
			}
		}

		private void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
		{
			e.SetObserved();

			if (e.Exception != null && !e.Exception.Message.Contains("Response body is unavailable for redirect responses"))
				_ = ShowErrorMessage(e.Exception);
		}

		internal static async Task ShowErrorMessage(Exception ex)
		{
			if (MainWindow?.DispatcherQueue != null)
			{
				MainWindow.DispatcherQueue.TryEnqueue(async () =>
				{
					await MessageBox.ShowErrorAsync(MainWindow, ex.Message, "Unexpected Error");
				});
			}

			try
			{
				await LogHelper.LogError(ex, PreparingStage.GPUs);
			}
			catch (Exception exception)
			{
				await LogHelper.LogFallbackError(ex);
				await LogHelper.LogFallbackError(exception);
			}
		}
	}
}
