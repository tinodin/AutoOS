using AutoOS.Views.Startup.Actions;
using AutoOS.Views.Startup.Stages;
using Microsoft.UI.Windowing;

namespace AutoOS.Views
{
    public sealed partial class StartupWindow : Window
    {
        public static TextBlock Status { get; private set; }
        public static ProgressBar Progress { get; private set; }
        public StartupWindow()
        {
            InitializeComponent();
            ExtendsContentIntoTitleBar = true;
            AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
            AppWindow.IsShownInSwitchers = false;
            SetTitleBar(AppTitleBar);

            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);

            var appWindow = AppWindow.GetFromWindowId(windowId);

            if (appWindow.Presenter is OverlappedPresenter p)
            {
                p.SetBorderAndTitleBar(true, false);
                p.IsResizable = false;
                p.IsAlwaysOnTop = true;
            }

            InitializeView(); 
        }
        private async void InitializeView()
        {
            Status = StatusText;
            Progress = ProgressBar;

            await StartupStage.Run();

            StartupWindow.Status.Text = "Done.";
            StartupWindow.Progress.Foreground = StartupActions.GetColor("LightSuccess", "DarkSuccess");

            await Task.Delay(700);

            Application.Current.Exit();
        }
    }
}