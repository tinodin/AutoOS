using AutoOS.Views.Installer.Actions;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace AutoOS.Views.Installer.Stages;

public static class RuntimesStage
{
    public static async Task Run()
    {
        InstallPage.Status.Text = "Configuring Runtimes...";

        int validActionsCount = 0;
        int stagePercentage = 5;

        var actions = new List<(Func<Task> Action, Func<bool> Condition)>
        {
            // download the latest visual c++ redistributable
            (async () => await ProcessActions.RunDownload("Downloading the latest Visual C++ Redistributable", "https://github.com/abbodi1406/vcredist/releases/latest/download/VisualCppRedist_AIO_x86_x64.exe", Path.GetTempPath(), "VisualCppRedist_AIO_x86_x64.exe"), null),

            // install visual c++ redistributable
            (async () => await ProcessActions.RunNsudo("Installing the Visual C++ Redistributable", "CurrentUser", @"""%TEMP%\VisualCppRedist_AIO_x86_x64.exe"" /ai /gm2"), null),

            // download the microsoft edge webview2 runtime
            (async () => await ProcessActions.RunDownload("Downloading the Microsoft Edge WebView2 Runtime", "https://msedge.sf.dl.delivery.mp.microsoft.com/filestreamingservice/files/7dedb563-79f6-48af-b588-dd8e97f4b73c/MicrosoftEdgeWebView2RuntimeInstallerX64.exe", Path.GetTempPath(), "MicrosoftEdgeWebView2RuntimeInstallerX64.exe"), null),

            // install microsoft edge webview2 runtime
            (async () => await ProcessActions.RunNsudo("Installing the Microsoft Edge WebView2 Runtime", "CurrentUser", @"""%TEMP%\MicrosoftEdgeWebView2RuntimeInstallerX64.exe"" /silent /install"), null),
            (async () => await ProcessActions.RunNsudo("Installing the Microsoft Edge WebView2 Runtime", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\MicrosoftEdgeUpdate.exe"" /v Debugger /t REG_SZ /d ""%windir%\System32\taskkill.exe"" /f"), null),

            // download the directx redistributable
            (async () => await ProcessActions.RunDownload("Downloading the DirectX Redistributable", "https://download.microsoft.com/download/8/4/A/84A35BF1-DAFE-4AE8-82AF-AD2AE20B6B14/directx_Jun2010_redist.exe", Path.GetTempPath(), "directx_Jun2010_redist.exe"), null),

            // extract the directx redistributable
            (async () => await ProcessActions.RunExtract("Extracting the DirectX Redistributable", Path.Combine(Path.GetTempPath(), "directx_Jun2010_redist.exe"), Path.Combine(Path.GetTempPath(), "directx_Jun2010_redist")),null),

            // install the directx redistributable
            (async () => await ProcessActions.RunNsudo("Installing the DirectX Redistributable", "CurrentUser", @"""%TEMP%\directx_Jun2010_redist\DXSetup.exe"" /silent"), null),
        };

        foreach (var (action, condition) in actions)
        {
            if ((condition == null || condition.Invoke()))
            {
                validActionsCount++;
            }
        }

        double incrementPerAction = validActionsCount > 0 ? stagePercentage / (double)validActionsCount : 0;

        foreach (var (action, condition) in actions)
        {
            if ((condition == null || condition.Invoke()))
            {
                try
                {
                    await action();
                }
                catch (Exception ex)
                {
                    InstallPage.Info.Title = ex.Message;
                    InstallPage.Progress.ShowError = true;
                    InstallPage.Info.Severity = InfoBarSeverity.Error;
                    InstallPage.ProgressRingControl.Foreground = new SolidColorBrush(Color.FromArgb(255, 196, 43, 28));
                    break;
                }

                InstallPage.Progress.Value += incrementPerAction;

                if (InstallPage.Info.Title != ProcessActions.previousTitle)
                {
                    await Task.Delay(75);
                }
            }
        }
    }
}
