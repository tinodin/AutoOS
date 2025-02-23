using System.Diagnostics;
using Downloader;
using Microsoft.UI.Xaml.Media;

namespace AutoOS.Views.Startup.Actions;

public static class StartupActions
{
    public static string previousTitle { get; private set; }

    public static async Task RunNsudo(string title, string user, string command)
    {
        previousTitle = StartupWindow.Status.Text;

        StartupWindow.Status.Text = $"{title}...";

        string arguments = user switch
        {
            "TrustedInstaller" => $"-U:T -P:E -Wait -ShowWindowMode:Hide {command}",
            "CurrentUser" => $"-U:P -P:E -Wait -ShowWindowMode:Hide {command}",
            _ => throw new ArgumentException("Invalid user specified.", nameof(user))
        };

        await Process.Start(new ProcessStartInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "NSudo", "NSudoLC.exe"), arguments) { CreateNoWindow = true })!.WaitForExitAsync();
    }

    public static async Task RunPowerShell(string title, string command)
    {
        previousTitle = StartupWindow.Status.Text;
        StartupWindow.Status.Text = $"{title}...";

        await Process.Start(new ProcessStartInfo("powershell.exe", $"-Command \"{command}\"") { CreateNoWindow = true, UseShellExecute = false })!.WaitForExitAsync();
    }

    public static async Task RunConnectionCheck(string title)
    {
        previousTitle = StartupWindow.Status.Text;

        if (!string.IsNullOrEmpty(title))
        {
            StartupWindow.Status.Text = $"{title}...";
        }

        StartupWindow.Progress.ShowPaused = true;

        await Task.Delay(1000);

        using (var httpClient = new HttpClient())
        {
            while (true)
            {
                try
                {
                    var response = await httpClient.GetAsync("http://www.google.com");
                    if (response.IsSuccessStatusCode)
                    {
                        StartupWindow.Progress.ShowPaused = false;
                        await Task.Delay(500);
                        break;
                    }
                }
                catch
                {

                }
            }
        }
    }

    public static async Task RunBatchScript(string title, string script, string arguments)
    {
        previousTitle = StartupWindow.Status.Text;

        StartupWindow.Status.Text = $"{title}...";

        await Process.Start(new ProcessStartInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Scripts", script), arguments) { CreateNoWindow = true })!.WaitForExitAsync();
    }

    public static async Task RunPowerShellScript(string title, string script, string arguments)
    {
        previousTitle = StartupWindow.Status.Text;
        StartupWindow.Status.Text = $"{title}...";

        await Process.Start(new ProcessStartInfo("powershell.exe", $"-ExecutionPolicy Bypass -File \"{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Scripts", script)}\" {arguments}") { CreateNoWindow = true })!.WaitForExitAsync();
    }

    public static async Task RunApplication(string title, string folderName, string executable, string arguments)
    {
        previousTitle = StartupWindow.Status.Text;

        StartupWindow.Status.Text = $"{title}...";

        await Task.Run(() => Process.Start(new ProcessStartInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", folderName, executable), arguments) { CreateNoWindow = true }));
    }

    public static async Task RunDownload(string title, string url, string path, string file)
    {
        var uiContext = SynchronizationContext.Current;
        uiContext?.Post(_ => StartupWindow.Status.Text = $"{title}...", null);

        var download = DownloadBuilder.New()
            .WithUrl(url)
            .WithDirectory(path)
            .WithFileName(file)
            .WithConfiguration(new DownloadConfiguration())
            .Build();

        DateTime lastLoggedTime = DateTime.MinValue;
        bool isPaused = false;

        var downloadTask = download.StartAsync();

        double receivedMB = 0.0;
        double totalMB = 0.0;
        double speedMB = 0.0;
        double percentage = 0.0;

        while (!downloadTask.IsCompleted)
        {
            download.DownloadProgressChanged += (sender, e) =>
            {
                if ((DateTime.Now - lastLoggedTime).TotalMilliseconds >= 50)
                {
                    lastLoggedTime = DateTime.Now;

                    speedMB = e.BytesPerSecondSpeed / (1024.0 * 1024.0);
                    receivedMB = e.ReceivedBytesSize / (1024.0 * 1024.0);
                    totalMB = e.TotalBytesToReceive / (1024.0 * 1024.0);
                    percentage = e.ProgressPercentage;

                    uiContext?.Post(_ =>
                    {
                        StartupWindow.Status.Text = $"{title} ({speedMB:F1} MB/s - {receivedMB:F2} MB of {totalMB:F2} MB)";

                    }, null);
                }
            };

            download.DownloadFileCompleted += (sender, e) =>
            {
                uiContext?.Post(_ =>
                {
                    StartupWindow.Status.Text = $"{title} ({speedMB:F1} MB/s - {totalMB:F2} MB of {totalMB:F2} MB)";
                }, null);
            };

            if (NetworkHelper.IsNetworkAvailable())
            {
                if (isPaused)
                {
                    isPaused = false;
                    uiContext?.Post(_ =>
                    {
                        StartupWindow.Progress.ShowPaused = false;
                        StartupWindow.Status.Text = $"{title} ({speedMB:F1} MB/s - {receivedMB:F2} MB of {totalMB:F2} MB)";
                    }, null);
                }
            }
            else
            {
                if (!isPaused)
                {
                    isPaused = true;
                    uiContext?.Post(_ =>
                    {
                        StartupWindow.Progress.ShowPaused = true;
                        StartupWindow.Status.Text = $"{title} ({speedMB:F1} MB/s - {receivedMB:F2} MB of {totalMB:F2} MB - Waiting for internet connection to reestablish...)";
                    }, null);
                }
            }
            await Task.Delay(800);
        }
    }

    public static async Task RunExtract(string title, string inputPath, string outputPath)
    {
        previousTitle = StartupWindow.Status.Text;

        StartupWindow.Status.Text = $"{title}...";

        await Process.Start(new ProcessStartInfo { FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "7-Zip", "7za.exe"), Arguments = $"x \"{inputPath}\" -y -o\"{outputPath}\"", CreateNoWindow = true })!.WaitForExitAsync();
    }

    public static async Task Sleep(string title, int amount)
    {
        previousTitle = StartupWindow.Status.Text;

        StartupWindow.Status.Text = $"{title}...";

        await Task.Delay(amount);
    }

    public static async Task RunCustom(string title, Func<Task> action)
    {
        previousTitle = StartupWindow.Status.Text;

        StartupWindow.Status.Text = $"{title}...";

        await action();
    }

    public static SolidColorBrush GetColor(string lightKey, string darkKey)
    {
        return (SolidColorBrush)Application.Current.Resources[App.Theme?.IsDarkTheme() == true ? darkKey : lightKey];
    }
}

