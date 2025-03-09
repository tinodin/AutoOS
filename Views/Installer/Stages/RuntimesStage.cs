using AutoOS.Views.Installer.Actions;
using Microsoft.UI.Xaml.Media;

namespace AutoOS.Views.Installer.Stages;

public static class RuntimesStage
{
    public static async Task Run()
    {
        InstallPage.Status.Text = "Configuring Runtimes...";

        string previousTitle = string.Empty;
        int stagePercentage = 5;

        var actions = new List<(string Title, Func<Task> Action, Func<bool> Condition)>
        {
            // download the latest visual c++ redistributable
            ("Downloading the latest Visual C++ Redistributable", async () => await ProcessActions.RunDownload("https://github.com/abbodi1406/vcredist/releases/latest/download/VisualCppRedist_AIO_x86_x64.exe", Path.GetTempPath(), "VisualCppRedist_AIO_x86_x64.exe"), null),

            // install visual c++ redistributable
            ("Installing the Visual C++ Redistributable", async () => await ProcessActions.RunNsudo("CurrentUser", @"""%TEMP%\VisualCppRedist_AIO_x86_x64.exe"" /ai /gm2"), null),

            // download the microsoft edge webview2 runtime
            ("Downloading the Microsoft Edge WebView2 Runtime", async () => await ProcessActions.RunDownload("https://msedge.sf.dl.delivery.mp.microsoft.com/filestreamingservice/files/7dedb563-79f6-48af-b588-dd8e97f4b73c/MicrosoftEdgeWebView2RuntimeInstallerX64.exe", Path.GetTempPath(), "MicrosoftEdgeWebView2RuntimeInstallerX64.exe"), null),

            // install microsoft edge webview2 runtime
            ("Installing the Microsoft Edge WebView2 Runtime", async () => await ProcessActions.RunNsudo("CurrentUser", @"""%TEMP%\MicrosoftEdgeWebView2RuntimeInstallerX64.exe"" /silent /install"), null),
            ("Installing the Microsoft Edge WebView2 Runtime", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\MicrosoftEdgeUpdate.exe"" /v Debugger /t REG_SZ /d ""%windir%\System32\taskkill.exe"" /f"), null),

            // download the directx redistributable
            ("Downloading the DirectX Redistributable", async () => await ProcessActions.RunDownload("https://download.microsoft.com/download/8/4/A/84A35BF1-DAFE-4AE8-82AF-AD2AE20B6B14/directx_Jun2010_redist.exe", Path.GetTempPath(), "directx_Jun2010_redist.exe"), null),

            // extract the directx redistributable
            ("Extracting the DirectX Redistributable", async () => await ProcessActions.RunExtract(Path.Combine(Path.GetTempPath(), "directx_Jun2010_redist.exe"), Path.Combine(Path.GetTempPath(), "directx_Jun2010_redist")),null),

            // install the directx redistributable
            ("Installing the DirectX Redistributable", async () => await ProcessActions.RunNsudo("CurrentUser", @"""%TEMP%\directx_Jun2010_redist\DXSetup.exe"" /silent"), null),
        };

        var filteredActions = actions.Where(a => a.Condition == null || a.Condition.Invoke()).ToList();
        var uniqueTitles = filteredActions.Select(a => a.Title).Distinct().ToList();
        double incrementPerTitle = uniqueTitles.Count > 0 ? stagePercentage / (double)uniqueTitles.Count : 0;

        foreach (var title in uniqueTitles)
        {
            if (previousTitle != string.Empty && previousTitle != title)
            {
                await Task.Delay(150);
            }

            var actionsForTitle = filteredActions.Where(a => a.Title == title).ToList();
            int actionsForTitleCount = actionsForTitle.Count;

            foreach (var (actionTitle, action, condition) in actionsForTitle)
            {
                InstallPage.Info.Title = actionTitle + "...";

                try
                {
                    await action();
                }
                catch (Exception ex)
                {
                    InstallPage.Info.Title = ex.Message;
                    InstallPage.Info.Severity = InfoBarSeverity.Error;
                    InstallPage.Progress.Foreground = (Brush)Application.Current.Resources["SystemFillColorCriticalBrush"];
                    InstallPage.ProgressRingControl.Foreground = (Brush)Application.Current.Resources["SystemFillColorCriticalBrush"];
                    InstallPage.ProgressRingControl.Visibility = Visibility.Collapsed;
                    InstallPage.ResumeButton.Visibility = Visibility.Visible;

                    var tcs = new TaskCompletionSource<bool>();

                    InstallPage.ResumeButton.Click += (sender, e) =>
                    {
                        tcs.TrySetResult(true);
                        InstallPage.Info.Severity = InfoBarSeverity.Informational;
                        InstallPage.Progress.Foreground = ProcessActions.GetColor("LightNormal", "DarkNormal");
                        InstallPage.ProgressRingControl.Foreground = null;
                        InstallPage.ProgressRingControl.Visibility = Visibility.Visible;
                        InstallPage.ResumeButton.Visibility = Visibility.Collapsed;

                    };

                    await tcs.Task;
                }
            }

            InstallPage.Progress.Value += incrementPerTitle;

            previousTitle = title;
        }
    }
}
