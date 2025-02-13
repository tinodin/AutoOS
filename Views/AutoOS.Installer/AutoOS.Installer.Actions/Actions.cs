using System.Diagnostics;
using Downloader;
using Microsoft.UI.Xaml.Media;
using Microsoft.Win32;
using Windows.UI;

namespace AutoOS.Views.Installer.Actions;

public static class ProcessActions
{
    public static string previousTitle { get; private set; }

    public static async Task RunNsudo(string title, string user, string command)
    {
        previousTitle = InstallPage.Info.Title;

        InstallPage.Info.Title = $"{title}...";

        string arguments = user switch
        {
            "TrustedInstaller" => $"-U:T -P:E -Wait -ShowWindowMode:Hide {command}",
            "CurrentUser" => $"-U:P -P:E -Wait -ShowWindowMode:Hide {command}",
            _ => throw new ArgumentException("Invalid user specified.", nameof(user))
        };

        await Process.Start(new ProcessStartInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "NSudo", "NSudoLC.exe"), arguments) { CreateNoWindow = true })!.WaitForExitAsync();
    }

    public static async Task RunRestart()
    {
        previousTitle = InstallPage.Info.Title;

        InstallPage.Status.Text = "Restarting...";
        InstallPage.Info.Title = "Restarting in 3...";
        await Task.Delay(1000);
        InstallPage.Info.Title = "Restarting in 2...";
        await Task.Delay(1000);
        InstallPage.Info.Title = "Restarting in 1...";
        await Task.Delay(1000);
        InstallPage.Info.Title = "Restarting...";
        await Task.Delay(750);
        ProcessStartInfo processStartInfo = new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = $"/c shutdown /r /t 0",
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        Process.Start(processStartInfo);
    }

    public static async Task RunPowerShell(string title, string command)
    {
        previousTitle = InstallPage.Info.Title;
        InstallPage.Info.Title = $"{title}...";

        await Process.Start(new ProcessStartInfo("powershell.exe", $"-Command \"{command}\"") { CreateNoWindow = true, UseShellExecute = false })!.WaitForExitAsync();
    }

    public static async Task RunConnectionCheck(string title)
    {
        previousTitle = InstallPage.Info.Title;

        if (!string.IsNullOrEmpty(title))
        {
            InstallPage.Info.Title = $"{title}...";
        }

        InstallPage.Progress.ShowPaused = true;
        InstallPage.Info.Severity = InfoBarSeverity.Warning;
        InstallPage.ProgressRingControl.Foreground = new SolidColorBrush(Color.FromArgb(255, 157, 93, 0));

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
                        InstallPage.Progress.ShowPaused = false;
                        InstallPage.Info.Severity = InfoBarSeverity.Informational;
                        InstallPage.ProgressRingControl.Foreground = null;
                        InstallPage.Info.Title = "Internet connection successfully established...";
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
        previousTitle = InstallPage.Info.Title;

        InstallPage.Info.Title = $"{title}...";

        await Process.Start(new ProcessStartInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Scripts", script), arguments) { CreateNoWindow = true })!.WaitForExitAsync();
    }

    public static async Task RunPowerShellScript(string title, string script, string arguments)
    {
        previousTitle = InstallPage.Info.Title;
        InstallPage.Info.Title = $"{title}...";

        await Process.Start(new ProcessStartInfo("powershell.exe", $"-ExecutionPolicy Bypass -File \"{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Scripts", script)}\" {arguments}") { CreateNoWindow = true, UseShellExecute = false })!.WaitForExitAsync();
    }


    public static async Task RunApplication(string title, string folderName, string executable, string arguments)
    {
        previousTitle = InstallPage.Info.Title;

        InstallPage.Info.Title = $"{title}...";

        await Process.Start(new ProcessStartInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", folderName, executable), arguments) { CreateNoWindow = true })!.WaitForExitAsync();
    }

    public static async Task RunDownload(string title, string url, string path, string file)
    {
        var uiContext = SynchronizationContext.Current;
        uiContext?.Post(_ => InstallPage.Info.Title = $"{title}...", null);

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
                        InstallPage.Info.Title = $"{title} ({speedMB:F1} MB/s - {receivedMB:F2} MB of {totalMB:F2} MB)";
                        InstallPage.ProgressRingControl.IsIndeterminate = false;
                        InstallPage.ProgressRingControl.Value = percentage;

                    }, null);
                }
            };

            download.DownloadFileCompleted += (sender, e) =>
            {
                uiContext?.Post(_ =>
                {
                    InstallPage.Info.Title = $"{title} ({speedMB:F1} MB/s - {totalMB:F2} MB of {totalMB:F2} MB)";
                    InstallPage.ProgressRingControl.Value = 100;
                    InstallPage.ProgressRingControl.IsIndeterminate = true;
                }, null);
            };

            if (NetworkHelper.IsNetworkAvailable())
            {
                if (isPaused)
                {
                    isPaused = false;
                    uiContext?.Post(_ =>
                    {
                        InstallPage.Progress.ShowPaused = false;
                        InstallPage.Info.Severity = InfoBarSeverity.Informational;
                        InstallPage.ProgressRingControl.Foreground = null;
                        InstallPage.Info.Title = $"{title} ({speedMB:F1} MB/s - {receivedMB:F2} MB of {totalMB:F2} MB)";
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
                        InstallPage.Progress.ShowPaused = true;
                        InstallPage.Info.Severity = InfoBarSeverity.Warning;
                        InstallPage.ProgressRingControl.Foreground = new SolidColorBrush(Color.FromArgb(255, 157, 93, 0));
                        InstallPage.Info.Title = $"{title} ({speedMB:F1} MB/s - {receivedMB:F2} MB of {totalMB:F2} MB - Waiting for internet connection to reestablish...)";
                    }, null);
                }
            }
            await Task.Delay(800);
        }
    }

    public static async Task RunExtract(string title, string inputPath, string outputPath)
    {
        previousTitle = InstallPage.Info.Title;

        InstallPage.Info.Title = $"{title}...";

        await Process.Start(new ProcessStartInfo { FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "7-Zip", "7za.exe"), Arguments = $"x \"{inputPath}\" -y -o\"{outputPath}\"", CreateNoWindow = true })!.WaitForExitAsync();
    }


    public static async Task RunNvidiaStrip(string title)
    {
        previousTitle = InstallPage.Info.Title;

        InstallPage.Info.Title = $"{title}...";

        var directories = Directory.GetDirectories(Path.Combine(Path.GetTempPath(), "driver"));

        foreach (var directory in directories)
        {
            string folderName = Path.GetFileName(directory);

            if (folderName != "Display.Driver" && folderName != "NVI2")
            {
                Directory.Delete(directory, true);
            }
        }

        string setupCfgPath = Path.Combine(Path.Combine(Path.GetTempPath(), "driver"), "setup.cfg");

        if (File.Exists(setupCfgPath))
        {
            var lines = await File.ReadAllLinesAsync(setupCfgPath);
            var newLines = lines.Where(line => !line.Contains("<file name=\"${{EulaHtmlFile}}\"/>") &&
                                                 !line.Contains("<file name=\"${{FunctionalConsentFile}}\"/>") &&
                                                 !line.Contains("<file name=\"${{PrivacyPolicyFile}}\"/>")).ToList();

            await File.WriteAllLinesAsync(setupCfgPath, newLines);
        }

        string presentationsCfgPath = Path.Combine(Path.Combine(Path.GetTempPath(), "driver"), "NVI2", "presentations.cfg");

        if (File.Exists(presentationsCfgPath))
        {
            var lines = await File.ReadAllLinesAsync(presentationsCfgPath);
            var newLines = lines.Select(line =>
            {
                if (line.Contains("<string name=\"ProgressPresentationUrl\""))
                {
                    return "        <string name=\"ProgressPresentationUrl\" value=\"\"/>";
                }
                else if (line.Contains("<string name=\"ProgressPresentationSelectedPackageUrl\""))
                {
                    return "        <string name=\"ProgressPresentationSelectedPackageUrl\" value=\"\"/>";
                }
                return line;
            }).ToList();

            await File.WriteAllLinesAsync(presentationsCfgPath, newLines);
        }
    }

    public static async Task ImportProfile(string title, string file)
    {
        previousTitle = InstallPage.Info.Title;

        InstallPage.Info.Title = $"{title}...";

        await Process.Start(new ProcessStartInfo { FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "NvidiaProfileInspector", "nvidiaProfileInspector.exe"), Arguments = $"-silentimport \"{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "NvidiaProfileInspector", file)}\"", CreateNoWindow = true })!.WaitForExitAsync();
    }

    public static async Task RemoveAppx(string title, string appx)
    {
        previousTitle = InstallPage.Info.Title;

        InstallPage.Info.Title = $"{title}...";

        await Process.Start(new ProcessStartInfo { FileName = "powershell.exe", Arguments = $"Get-AppxPackage \"{appx}\" | Remove-AppxPackage", CreateNoWindow = true })!.WaitForExitAsync();
    }

    public static async Task RemoveAppxProvisioned(string title, string appx)
    {
        previousTitle = InstallPage.Info.Title;

        InstallPage.Info.Title = $"{title}...";

        await Process.Start(new ProcessStartInfo { FileName = "powershell.exe", Arguments = $"Remove-AppxProvisionedPackage -PackageName (Get-AppxProvisionedPackage -Online | Where-Object {{ ('{appx}' -contains $_.DisplayName) }}).PackageName -Online -AllUsers", CreateNoWindow = true })!.WaitForExitAsync();
    }

    public static async Task UpdateAppx(string title, string appx)
    {
        previousTitle = InstallPage.Info.Title;

        InstallPage.Info.Title = $"{title}...";

        ProcessStartInfo processStartInfo = new ProcessStartInfo("powershell.exe", $"-ExecutionPolicy Bypass -File \"{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Scripts", "updateappx.ps1")}\" \"{appx}\"")
        {
            CreateNoWindow = true,
            RedirectStandardOutput = true,
        };

        using (Process process = Process.Start(processStartInfo))
        {
            using (StreamReader reader = process.StandardOutput)
            {
                string line;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    InstallPage.ProgressRingControl.IsIndeterminate = false;
                    InstallPage.ProgressRingControl.Value = Convert.ToDouble(line);
                }
            }

            await process.WaitForExitAsync();
            InstallPage.ProgressRingControl.IsIndeterminate = true;
            InstallPage.ProgressRingControl.Value = 0;
        }
    }

    public static async Task DisableScheduledTasks(string title)
    {
        previousTitle = InstallPage.Info.Title;
        InstallPage.Info.Title = $"{title}...";

        ProcessStartInfo processStartInfo = new ProcessStartInfo("powershell.exe", $"-ExecutionPolicy Bypass -File \"{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Scripts", "disablescheduledtasks.ps1")}\" \"{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "NSudo", "NSudoLC.exe")}\"")
        {
            CreateNoWindow = true,
            RedirectStandardOutput = true,
        };

        using (Process process = Process.Start(processStartInfo))
        {
            using (StreamReader reader = process.StandardOutput)
            {
                string line;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    if (double.TryParse(line, out double progress))
                    {
                        InstallPage.ProgressRingControl.IsIndeterminate = false;
                        InstallPage.ProgressRingControl.Value = progress;
                    }
                }
            }

            await process.WaitForExitAsync();
            InstallPage.ProgressRingControl.IsIndeterminate = true;
            InstallPage.ProgressRingControl.Value = 0;
        }
    }

    public static async Task RemoveWindowsCapabilities(string title)
    {
        previousTitle = InstallPage.Info.Title;
        InstallPage.Info.Title = $"{title}...";

        ProcessStartInfo processStartInfo = new ProcessStartInfo("powershell.exe", $"-ExecutionPolicy Bypass -File \"{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Scripts", "removecapabilities.ps1")}\"")
        {
            CreateNoWindow = true,
            RedirectStandardOutput = true
        };

        using (Process process = Process.Start(processStartInfo))
        {
            using (StreamReader reader = process.StandardOutput)
            {
                string line;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    if (double.TryParse(line, out double progress))
                    {
                        InstallPage.ProgressRingControl.IsIndeterminate = false;
                        InstallPage.ProgressRingControl.Value = progress;
                    }
                }
            }

            await process.WaitForExitAsync();
            InstallPage.ProgressRingControl.IsIndeterminate = true;
            InstallPage.ProgressRingControl.Value = 0;
        }
    }

    public static async Task DisableOptionalFeatures(string title)
    {
        previousTitle = InstallPage.Info.Title;
        InstallPage.Info.Title = $"{title}...";

        ProcessStartInfo processStartInfo = new ProcessStartInfo("powershell.exe", $"-ExecutionPolicy Bypass -File \"{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Scripts", "disablefeatures.ps1")}\"")
        {
            CreateNoWindow = true,
            RedirectStandardOutput = true
        };

        using (Process process = Process.Start(processStartInfo))
        {
            using (StreamReader reader = process.StandardOutput)
            {
                string line;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    if (double.TryParse(line, out double progress))
                    {
                        InstallPage.ProgressRingControl.IsIndeterminate = false;
                        InstallPage.ProgressRingControl.Value = progress;
                    }
                }
            }

            await process.WaitForExitAsync();
            InstallPage.ProgressRingControl.IsIndeterminate = true;
            InstallPage.ProgressRingControl.Value = 0;
        }
    }

    public static async Task DisableWiFiServicesAndDrivers(string title)
    {
        previousTitle = InstallPage.Info.Title;

        InstallPage.Info.Title = $"{title}...";

        string[] services = { "WlanSvc", "Dhcp", "EventLog", "Wcmsvc", "WinHttpAutoProxySvc", "NlaSvc", "tdx", "vwififlt" };

        foreach (string service in services)
            Registry.SetValue($@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\{service}", "Start", 4, RegistryValueKind.DWord);

        using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\Netwtw10", writable: true))
        {
            if (key != null)
            {
                Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Netwtw10", "Start", 4, RegistryValueKind.DWord);
            }
        }

        using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\Netwtw14", writable: true))
        {
            if (key != null)
            {
                Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Netwtw10", "Start", 4, RegistryValueKind.DWord);
            }
        }
    }

    public static async Task DisableBluetoothServicesAndDrivers(string title)
    {
        previousTitle = InstallPage.Info.Title;

        InstallPage.Info.Title = $"{title}...";

        string[] services = {
            "BluetoothUserService", "BTAGService", "BthAvctpSvc", "bthserv", "DevicesFlowUserSvc", "DsmSvc", "NcbService", "WFDSConMgrSvc", "BthA2dp", "BthEnum", "BthHFAud", "BthHFEnum", "BthLEEnum", "BTHMODEM", "BthMini", "BthPan", "BTHPORT", "BTHUSB", "HidBth", "Microsoft_Bluetooth_AvrcpTransport", "RFCOMM"
        };

        foreach (string service in services)
            Registry.SetValue($@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\{service}", "Start", 4, RegistryValueKind.DWord);

        using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\ibtusb", writable: true))
        {
            if (key != null)
            {
                Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Netwtw10", "Start", 4, RegistryValueKind.DWord);
            }
        }
    }

    public static async Task Sleep(string title, int amount)
    {
        previousTitle = InstallPage.Info.Title;

        InstallPage.Info.Title = $"{title}...";

        await Task.Delay(amount);
    }

    public static async Task RunCustom(string title, Func<Task> action)
    {
        previousTitle = InstallPage.Info.Title;

        InstallPage.Info.Title = $"{title}...";

        await action();
    }

    public static async Task RunMicrosoftStoreDownload(string title, string productFamilyName, string fileType, string architecture, string fileName)
    {
        previousTitle = InstallPage.Info.Title;
        InstallPage.Info.Title = $"{title}...";

        var output = await Process.Start(new ProcessStartInfo("powershell.exe", $"-ExecutionPolicy Bypass -File \"{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Scripts", "getmicrosoftstorelink.ps1")}\" \"{productFamilyName}\" \"{fileType}\" \"{architecture}\"")
        {
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardOutput = true
        })!.StandardOutput.ReadToEndAsync();

        var url = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "";

        await RunDownload(title, url, Path.GetTempPath(), fileName);
    }
}

