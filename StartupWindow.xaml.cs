using AutoOS.Views.Startup.Stages;
using Microsoft.UI.Windowing;
using System.Runtime.InteropServices;

namespace AutoOS.Views
{
    public sealed partial class StartupWindow : Window
    {
        [DllImport("user32.dll")]
        static extern uint GetDpiForWindow(IntPtr hWnd);
        public string TitleBarName { get; set; }
        public static TextBlock Status { get; private set; }
        public static ProgressBar Progress { get; private set; }

        public StartupWindow()
        {
            InitializeComponent();
            ExtendsContentIntoTitleBar = true;
            SetTitleBar(AppTitleBar);
            AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
            AppWindow.IsShownInSwitchers = false;
            new ModernSystemMenu(this);

            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            uint dpi = GetDpiForWindow(hwnd);
            int scalingPercent = (int)(dpi * 100 / 96);

            App.Scaling = dpi / 96.0;

            ((OverlappedPresenter)AppWindow.Presenter).PreferredMaximumWidth = (int)(340 * App.Scaling);
            ((OverlappedPresenter)AppWindow.Presenter).PreferredMaximumHeight = (int)(130 * App.Scaling);
            ((OverlappedPresenter)AppWindow.Presenter).IsResizable = false;
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
    }
}