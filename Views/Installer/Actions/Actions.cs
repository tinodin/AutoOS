using Downloader;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml.Media;
using Microsoft.Win32;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Windows.Graphics;

namespace AutoOS.Views.Installer.Actions;

public static class ProcessActions
{
    public static async Task RunNsudo(string user, string command)
    {
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

    public static async Task RunPowerShell(string command)
    {
        await Process.Start(new ProcessStartInfo("powershell.exe", $"-Command \"{command}\"") { CreateNoWindow = true, UseShellExecute = false })!.WaitForExitAsync();
    }

    public static async Task RunConnectionCheck()
    {
        InstallPage.Info.Severity = InfoBarSeverity.Warning;
        InstallPage.Progress.Foreground = (Brush)Application.Current.Resources["SystemFillColorCautionBrush"];
        InstallPage.ProgressRingControl.Foreground = (Brush)Application.Current.Resources["SystemFillColorCautionBrush"];

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
                        InstallPage.Info.Severity = InfoBarSeverity.Informational;
                        InstallPage.Progress.Foreground = ProcessActions.GetColor("LightNormal", "DarkNormal");
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

    public static async Task RunBatchScript(string script, string arguments)
    {
        await Process.Start(new ProcessStartInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Scripts", script), arguments) { CreateNoWindow = true })!.WaitForExitAsync();
    }

    public static async Task RunPowerShellScript(string script, string arguments)
    {
        await Process.Start(new ProcessStartInfo("powershell.exe", $"-ExecutionPolicy Bypass -File \"{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Scripts", script)}\" {arguments}") { CreateNoWindow = true, UseShellExecute = false })!.WaitForExitAsync();
    }

    public static async Task RunApplication(string folderName, string executable, string arguments)
    {
        await Process.Start(new ProcessStartInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", folderName, executable), arguments) { CreateNoWindow = true })!.WaitForExitAsync();
    }

    public static async Task RunDownload(string url, string path, string file)
    {
        string title = InstallPage.Info.Title;

        var uiContext = SynchronizationContext.Current;

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
                        InstallPage.Info.Severity = InfoBarSeverity.Informational;
                        InstallPage.Progress.Foreground = ProcessActions.GetColor("LightNormal", "DarkNormal");
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
                        InstallPage.Info.Severity = InfoBarSeverity.Warning;
                        InstallPage.Progress.Foreground = (Brush)Application.Current.Resources["SystemFillColorCautionBrush"];
                        InstallPage.ProgressRingControl.Foreground = (Brush)Application.Current.Resources["SystemFillColorCautionBrush"];
                        InstallPage.Info.Title = $"{title} ({speedMB:F1} MB/s - {receivedMB:F2} MB of {totalMB:F2} MB - Waiting for internet connection to reestablish...)";
                    }, null);
                }
            }
            await Task.Delay(800);
        }
    }

    public static async Task RunExtract(string inputPath, string outputPath)
    {
        await Process.Start(new ProcessStartInfo { FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "7-Zip", "7za.exe"), Arguments = $"x \"{inputPath}\" -y -o\"{outputPath}\"", CreateNoWindow = true })!.WaitForExitAsync();
    }

    public static async Task RunNvidiaStrip()
    {
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

    public static async Task ImportProfile(string file)
    {
        await Process.Start(new ProcessStartInfo { FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "NvidiaProfileInspector", "nvidiaProfileInspector.exe"), Arguments = $"-silentimport \"{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "NvidiaProfileInspector", file)}\"", CreateNoWindow = true })!.WaitForExitAsync();
    }

    public static async Task RemoveAppx(string appx)
    {
        await Process.Start(new ProcessStartInfo { FileName = "powershell.exe", Arguments = $"Get-AppxPackage \"{appx}\" | Remove-AppxPackage", CreateNoWindow = true })!.WaitForExitAsync();
    }

    public static async Task RemoveAppxProvisioned(string appx)
    {
        await Process.Start(new ProcessStartInfo { FileName = "powershell.exe", Arguments = $"Remove-AppxProvisionedPackage -PackageName (Get-AppxProvisionedPackage -Online | Where-Object {{ ('{appx}' -contains $_.DisplayName) }}).PackageName -Online -AllUsers", CreateNoWindow = true })!.WaitForExitAsync();
    }

    public static async Task UpdateAppx(string appx)
    {
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

    public static async Task DisableScheduledTasks()
    {
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

    public static async Task RemoveWindowsCapabilities()
    {
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

    public static async Task DisableOptionalFeatures()
    {
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

    public static async Task DisableWiFiServicesAndDrivers()
    {
        // set start values
        string[] services = { "WlanSvc", "EventLog", "Wcmsvc", "WinHttpAutoProxySvc", "NlaSvc", "tdx", "vwififlt", "Netwtw10", "Netwtw14" };
        
        foreach (var service in services)
        {
            using (var key = Registry.LocalMachine.OpenSubKey($@"SYSTEM\CurrentControlSet\Services\{service}", writable: true))
            {
                if (key == null) continue;

                Registry.SetValue($@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\{service}", "Start", 4);
            }
        }

        await Task.Delay(300);
    }

    public static async Task DisableBluetoothServicesAndDrivers()
    {
        string[] services = { "BluetoothUserService", "BTAGService", "BthAvctpSvc", "bthserv", "DevicesFlowUserSvc", "DsmSvc", "WFDSConMgrSvc", "BthA2dp", "BthEnum", "BthHFAud", "BthHFEnum", "BthLEEnum", "BTHMODEM", "BthMini", "BthPan", "BTHPORT", "BTHUSB", "HidBth", "Microsoft_Bluetooth_AvrcpTransport", "RFCOMM", "ibtusb" };

        foreach (var service in services)
        {
            using (var key = Registry.LocalMachine.OpenSubKey($@"SYSTEM\CurrentControlSet\Services\{service}", writable: true))
            {
                if (key == null) continue;

                Registry.SetValue($@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\{service}", "Start", 4);
            }
        }

        await Task.Delay(300);
    }

    public static async Task Sleep(int amount)
    {
        await Task.Delay(amount);
    }

    public static async Task RunCustom(Func<Task> action)
    {
        await action();
    }

    public static async Task RunMicrosoftStoreDownload(string productFamilyName, string fileType, string architecture, string fileName)
    {
        var output = await Process.Start(new ProcessStartInfo("powershell.exe", $"-ExecutionPolicy Bypass -File \"{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Scripts", "getmicrosoftstorelink.ps1")}\" \"{productFamilyName}\" \"{fileType}\" \"{architecture}\"")
        {
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardOutput = true
        })!.StandardOutput.ReadToEndAsync();

        var url = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "";

        await RunDownload(url, Path.GetTempPath(), fileName);
    }

    public static async Task RunImportEpicGamesLauncherAccount()
    {
        // get all configs from other drives
        var foundFiles = DriveInfo.GetDrives()
            .Where(d => d.DriveType == DriveType.Fixed && d.Name != @"C:\")
            .SelectMany(d =>
            {
                string usersPath = Path.Combine(d.Name, "Users");
                if (!Directory.Exists(usersPath)) return Array.Empty<string>();

                return Directory.GetDirectories(usersPath)
                    .Select(userDir => Path.Combine(userDir, "AppData", "Local", "EpicGamesLauncher", "Saved", "Config", "Windows", "GameUserSettings.ini"))
                    .Where(File.Exists);
            })
            .Select(path => new FileInfo(path))
            .ToList();

        string destinationPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "EpicGamesLauncher", "Saved", "Config", "Windows", "GameUserSettings.ini");
        string newestFilePath = null;

        // check if files are valid
        foreach (var file in foundFiles)
        {
            string configContent = await File.ReadAllTextAsync(file.FullName);
            Match dataMatch = Regex.Match(configContent, @"Data=([^\r\n]+)");

            if (dataMatch.Success && dataMatch.Groups[1].Value.Length >= 1000)
            {
                // use the latest one
                if (newestFilePath == null || file.LastWriteTime > new FileInfo(newestFilePath).LastWriteTime)
                {
                    newestFilePath = file.FullName;
                    Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
                    File.Copy(newestFilePath, destinationPath, true);

                    // disable tray and notifications
                    Match match = Regex.Match(await File.ReadAllTextAsync(destinationPath), @"\[(.*?)_General\]");
                    string section = match.Groups[1].Value + "_General";

                    InIHelper iniHelper = new InIHelper(destinationPath);

                    try
                    {
                        if (!iniHelper.IsKeyExists("MinimiseToSystemTray", section))
                        {
                            iniHelper.AddValue("MinimiseToSystemTray", "False", section);
                        }
                    }
                    catch (Exception)
                    {
                        iniHelper.AddValue("MinimiseToSystemTray", "False", section);
                    }

                    try
                    {
                        if (!iniHelper.IsKeyExists("NotificationsEnabled_FreeGame", section))
                        {
                            iniHelper.AddValue("NotificationsEnabled_FreeGame", "False", section);
                        }
                    }
                    catch (Exception)
                    {
                        iniHelper.AddValue("NotificationsEnabled_FreeGame", "False", section);
                    }

                    try
                    {
                        if (!iniHelper.IsKeyExists("NotificationsEnabled_Adverts", section))
                        {
                            iniHelper.AddValue("NotificationsEnabled_Adverts", "False", section);
                        }
                    }
                    catch (Exception)
                    {
                        iniHelper.AddValue("NotificationsEnabled_Adverts", "False", section);
                    }

                    Match dataMatch2 = Regex.Match(configContent, @"\[RememberMe\][^\[]*?Data=""?([^\r\n""]+)""?");
                    string data = dataMatch2.Groups[1].Value;
                    string key = "A09C853C9E95409BB94D707EADEFA52E";
                    string plainText = DecryptDataWithAes(data, key);

                    // get displayname
                    Match displayNameMatch = Regex.Match(plainText, "\"DisplayName\":\"([^\"]+)\"");
                    string displayName = displayNameMatch.Groups[1].Value;

                    // create directory and backup
                    Directory.CreateDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"EpicGamesLauncher\Saved\Config\Windows\" + displayName));
                    File.Copy(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"EpicGamesLauncher\Saved\Config\Windows\GameUserSettings.ini"), Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"EpicGamesLauncher\Saved\Config\Windows\", displayName), "GameUserSettings.ini"), true);
                    return;
                }
            }
        }
    }

    private static string DecryptDataWithAes(string cipherText, string key)
    {
        using (Aes aesAlgorithm = Aes.Create())
        {
            aesAlgorithm.KeySize = 256;
            aesAlgorithm.Mode = CipherMode.ECB;
            aesAlgorithm.Padding = PaddingMode.PKCS7;

            byte[] keyBytes = Encoding.UTF8.GetBytes(key);
            aesAlgorithm.Key = keyBytes;

            byte[] cipher = Convert.FromBase64String(cipherText);

            ICryptoTransform decryptor = aesAlgorithm.CreateDecryptor();

            using (MemoryStream ms = new MemoryStream(cipher))
            using (CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
            using (StreamReader sr = new StreamReader(cs))
            {
                return sr.ReadToEnd();
            }
        }
    }
    public static async Task RunImportEpicGamesLauncherGames()
    {
        // get all install lists from other drives
        var foundFiles = DriveInfo.GetDrives()
            .Where(d => d.DriveType == DriveType.Fixed && d.Name != @"C:\")
            .Select(d => Path.Combine(d.Name, "ProgramData", "Epic", "UnrealEngineLauncher", "LauncherInstalled.dat"))
            .Where(File.Exists)
            .Select(path => new FileInfo(path))
            .OrderByDescending(f => f.LastWriteTime)
            .ToList();

        var jsonContent = await File.ReadAllTextAsync(foundFiles.First().FullName);
        var jsonObject = JsonNode.Parse(jsonContent);
        var installationList = jsonObject?["InstallationList"] as JsonArray;

        // return if install list is empty
        if (installationList == null || installationList.Count == 0)
        {
            return;
        }

        FileInfo newestFile = foundFiles.First();

        string destinationPath = @"C:\ProgramData\Epic\UnrealEngineLauncher\LauncherInstalled.dat";
        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);

        // set new game paths
        foreach (var game in installationList)
        {
            if (game is JsonObject gameObj && gameObj.ContainsKey("InstallLocation"))
            {
                string originalPath = gameObj["InstallLocation"].ToString();
                string originalDrive = Path.GetPathRoot(originalPath) ?? "";
                string relativePath = originalPath.Substring(originalDrive.Length);

                foreach (var drive in DriveInfo.GetDrives().Where(d => d.DriveType == DriveType.Fixed && d.Name != @"C:\"))
                {
                    string testPath = Path.Combine(drive.Name, relativePath);
                    if (Directory.Exists(testPath))
                    {
                        gameObj["InstallLocation"] = testPath;
                        break;
                    }
                }
            }
        }

        await File.WriteAllTextAsync(destinationPath, jsonObject.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));

        // copy over the manifest folder
        string sourceManifestsFolder = Path.Combine(Path.GetPathRoot(newestFile.FullName)!, "ProgramData", "Epic", "EpicGamesLauncher", "Data", "Manifests");
        string destinationManifestsFolder = @"C:\ProgramData\Epic\EpicGamesLauncher\Data\Manifests";

        if (Directory.Exists(sourceManifestsFolder))
        {
            Directory.CreateDirectory(destinationManifestsFolder);

            foreach (var directory in Directory.GetDirectories(sourceManifestsFolder, "*", SearchOption.AllDirectories))
            {
                string subDirPath = directory.Replace(sourceManifestsFolder, destinationManifestsFolder);
                Directory.CreateDirectory(subDirPath);
            }

            foreach (var file in Directory.GetFiles(sourceManifestsFolder, "*.*", SearchOption.AllDirectories))
            {
                string destFilePath = file.Replace(sourceManifestsFolder, destinationManifestsFolder);
                File.Copy(file, destFilePath, true);
            }

            // set new game paths
            foreach (var file in Directory.GetFiles(destinationManifestsFolder, "*.item", SearchOption.AllDirectories))
            {
                string fileName = Path.GetFileName(file);
                string destFilePath = file;

                var itemJson = JsonNode.Parse(await File.ReadAllTextAsync(destFilePath));

                if (itemJson is JsonObject itemObj)
                {
                    if (itemObj.ContainsKey("InstallLocation"))
                    {
                        string originalInstallLocation = itemObj["InstallLocation"].ToString();
                        string originalDrive = Path.GetPathRoot(originalInstallLocation) ?? "";
                        string relativePath = originalInstallLocation.Substring(originalDrive.Length);

                        foreach (var drive in DriveInfo.GetDrives().Where(d => d.DriveType == DriveType.Fixed && d.Name != @"C:\"))
                        {
                            string testPath = Path.Combine(drive.Name, relativePath);
                            if (Directory.Exists(testPath))
                            {
                                string newDrive = Path.GetPathRoot(testPath);

                                itemObj["InstallLocation"] = newDrive + relativePath;
                                itemObj["ManifestLocation"] = itemObj["ManifestLocation"].ToString().Replace(originalDrive, newDrive);
                                itemObj["StagingLocation"] = itemObj["StagingLocation"].ToString().Replace(originalDrive, newDrive);

                                break;
                            }
                        }
                    }

                    await File.WriteAllTextAsync(destFilePath, itemObj.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
                }
            }
        }
        else
        {
            return;
        }
    }

    public static async Task RefreshUI()
    {
        if (MainWindow.Instance.AppWindow.Presenter is OverlappedPresenter presenter)
        {
            MainWindow.Instance.AppWindow.Resize(new SizeInt32(MainWindow.Instance.AppWindow.Size.Width - 500, MainWindow.Instance.AppWindow.Size.Height - 500));

            await Task.Delay(1);

            presenter.Restore();
            presenter.Maximize();
        }
    }

    public static SolidColorBrush GetColor(string lightKey, string darkKey)
    {
        return (SolidColorBrush)Application.Current.Resources[App.Theme?.IsDarkTheme() == true ? darkKey : lightKey];
    }
}

