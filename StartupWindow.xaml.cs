using AutoOS.Views.Startup.Stages;
using AutoOS.Views.Updater.Stages;
using Microsoft.UI.Windowing;

namespace AutoOS.Views
{
    public sealed partial class StartupWindow : Window
    {
        public string TitleBarName { get; set; }
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

            StartupWindow_Loaded();
        }

        private async void StartupWindow_Loaded()
        {
            Status = StatusText;
            Progress = ProgressBar;

            if (!Directory.Exists(@"C:\Program Files\Windhawk"))
            {
                TitleBarName = "AutoOS Updater";
                await UpdaterStage.Run();
            }
            else
            {
                TitleBarName = "AutoOS Startup";
                await StartupStage.Run();
            }
        }
    }
}