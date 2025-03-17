using System.Diagnostics;
using System.Management;
using System.ServiceProcess;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;
namespace AutoOS.Views.Settings;

public sealed partial class GamesPage : Page
{
    private bool isLaunchingFortnite = false;
    private bool isInitializingAccounts = true;
    private readonly string configFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"EpicGamesLauncher\Saved\Config\Windows\GameUserSettings.ini");
    private string nsudoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "NSudo", "NSudoLC.exe");
    public GamesPage()
    {
        InitializeComponent();
        //CheckFortniteUpdate();
        CheckFortniteRunning();
        LoadEpicAccounts();
    }

    //private async void CheckFortniteUpdate()
    //{
    //    if (Process.GetProcessesByName("FortniteClient-Win64-Shipping").Length == 0)
    //    {
    //        // get current version
    //        string currentVersion = Regex.Match(JObject.Parse(File.ReadAllText(Path.Combine(JObject.Parse(File.ReadAllText(Path.Combine(legendaryPath, "installed.json")))["Fortnite"]["install_path"]?.ToString(), "Cloud", "cloudcontent.json")))["BuildVersion"]?.ToString(), @"Release-(\d+\.\d+)").Groups[1].Value;
    //        fortniteVersion.Description = "Current version: " + currentVersion;

    //        // list installed games
    //        var output = await Task.Run(() => Process.Start(new ProcessStartInfo(@"C:\Windows\legendary.exe", "list-installed") { CreateNoWindow = true, RedirectStandardOutput = true })?.StandardOutput.ReadToEndAsync());

    //        // check output
    //        if (output.Contains("Latest: ++Fortnite+Release"))
    //        {
    //            // get newest version
    //            string newestVersion = Regex.Match(output, @"Latest: \+\+Fortnite\+Release-(\d+\.\d+)").Groups[1].Value;

    //            // delay
    //            await Task.Delay(500);

    //            // hide progress ring
    //            updateCheckProgress.Visibility = Visibility.Collapsed;

    //            // check if update is needed
    //            if (string.Compare(newestVersion, currentVersion, StringComparison.Ordinal) > 0)
    //            {
    //                launchFortnite.Content = "Update to " + newestVersion;
    //            }
    //            else if (string.Compare(newestVersion, currentVersion, StringComparison.Ordinal) == 0)
    //            {
    //                launchFortnite.Content = "Launch";
    //            }
    //        }
    //        else
    //        {
    //            // delay
    //            await Task.Delay(650);

    //            // hide progress ring
    //            updateCheckProgress.Visibility = Visibility.Collapsed;

    //            launchFortnite.Content = "Launch";
    //        }
    //    }
    //    else
    //    {
    //        // hide progress ring
    //        updateCheckProgress.Visibility = Visibility.Collapsed;
    //    }
    //}

    private void CheckFortniteRunning()
    {
        if (Process.GetProcessesByName("FortniteClient-Win64-Shipping").Length > 0)
        {
            // rename to running
            launchFortnite.Content = "Running...";

            // check if stop processes button exists
            if (stopProcesses.Visibility == Visibility.Collapsed)
            {
                // add stop processes button
                stopProcesses.Visibility = Visibility.Visible;
            }

            // add launch explorer button if explorer is not running
            if (Process.GetProcessesByName("explorer").Length == 0)
            {
                // check if launch explorer button exists
                if (launchExplorer.Visibility == Visibility.Collapsed)
                {
                    launchExplorer.Visibility = Visibility.Visible;
                }
            }
            isLaunchingFortnite = false;
        }

        var synchronizationContext = SynchronizationContext.Current;

        // watcher
        var watcher = new Thread(() =>
        {
            while (true)
            {
                synchronizationContext.Post(_ =>
                {
                    if (Process.GetProcessesByName("FortniteClient-Win64-Shipping").Length > 0)
                    {
                        // rename to running
                        launchFortnite.Content = "Running...";

                        // check if stop processes button exists
                        if (stopProcesses.Visibility == Visibility.Collapsed)
                        {
                            // add stop processes button
                            stopProcesses.Visibility = Visibility.Visible;
                            isLaunchingFortnite = false;
                        }
                        
                        // check if explorer is not running
                        if (Process.GetProcessesByName("explorer").Length == 0)
                        {
                            // add launch explorer button if explorer is not running
                            if (Process.GetProcessesByName("explorer").Length == 0)
                            {
                                // check if launch explorer button exists
                                if (launchExplorer.Visibility == Visibility.Collapsed)
                                {
                                    launchExplorer.Visibility = Visibility.Visible;
                                }
                            }
                        }
                        else
                        {
                            // remove launch explorer button if explorer is running
                            if (launchExplorer.Visibility == Visibility.Visible)
                            {
                                launchExplorer.Visibility = Visibility.Collapsed;
                            }
                        }
                    }
                    else
                    {
                        if (isLaunchingFortnite == false)
                        {
                            if (launchFortnite.Content?.ToString() == "Running...")
                            {
                                // rename to running
                                launchFortnite.Content = "Launch";
                            }

                            // remove stop processes button
                            if (stopProcesses.Visibility == Visibility.Visible)
                            {
                                stopProcesses.Visibility = Visibility.Collapsed;
                            }

                            // remove launch explorer
                            if (launchExplorer.Visibility == Visibility.Visible)
                            {
                                launchExplorer.Visibility = Visibility.Collapsed;
                            }

                            // launch explorer if not already
                            if (Process.GetProcessesByName("explorer").Length == 0)
                            {
                                Process.Start("explorer.exe");
                            }
                        }
                    }
                }, null);

                Thread.Sleep(1000);
            }
        });

        watcher.IsBackground = true;
        watcher.Start();
    }

    private async void StopProcesses_Click(object sender, RoutedEventArgs e)
    {
        // rename start menu binaries
        await Process.Start(new ProcessStartInfo { FileName = nsudoPath, Arguments = $@"-U:T -P:E -Wait -ShowWindowMode:Hide ren ""C:\Windows\System32\ctfmon.exe"" ctfmon.exee & ren ""C:\Windows\System32\RuntimeBroker.exe"" RuntimeBroker.exee & ren ""C:\Windows\SystemApps\ShellExperienceHost_cw5n1h2txyewy\ShellExperienceHost.exe"" ShellExperienceHost.exee & ren ""C:\Windows\SystemApps\MicrosoftWindows.Client.CBS_cw5n1h2txyewy\SearchHost.exe"" SearchHost.exee & ren ""C:\Windows\SystemApps\Microsoft.Windows.StartMenuExperienceHost_cw5n1h2txyewy\StartMenuExperienceHost.exe"" StartMenuExperienceHost.exee", CreateNoWindow = true }).WaitForExitAsync();

        foreach (var process in Process.GetProcessesByName("dllhost"))
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher(
                    $"SELECT ProcessId, CommandLine FROM Win32_Process WHERE Name = 'dllhost.exe'"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        string cmdLine = obj["CommandLine"]?.ToString() ?? "";
                        int pid = Convert.ToInt32(obj["ProcessId"]);

                        if (cmdLine.Contains("/PROCESSID", StringComparison.OrdinalIgnoreCase))
                        {
                            try
                            {
                                var proc = Process.GetProcessById(pid);
                                proc.Kill();
                                proc.WaitForExit();
                            }
                            catch { }
                        }
                    }
                }
            }
            catch { }
        }

        // close executables
        Registry.SetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon", "AutoRestartShell", 0, RegistryValueKind.DWord);

        string[] processNames = {
            "ApplicationFrameHost",
            "CrashReportClient",
            "ctfmon",
            "EasyAntiCheat_EOS",
            "EpicGamesLauncher",
            "explorer",
            "FortniteClient-Win64-Shipping_EAC_EOS",
            "RuntimeBroker",
            "SearchHost",
            "secd.exe",
            "ShellExperienceHost",
            "sppsvc",
            "StartMenuExperienceHost",
            "TrustedInstaller",
            "useroobebroker",
            "WMIADAP",
            "WmiPrvSE",
            "WUDFHost"
        };

        foreach (var name in processNames)
        {
            foreach (var process in Process.GetProcessesByName(name))
            {
                try { process.Kill(); process.WaitForExit(); } catch { }
            }

            foreach (var process in Process.GetProcessesByName(name))
            {
                try { process.Kill(); process.WaitForExit(); } catch { }
            }
        }

        Registry.SetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon", "AutoRestartShell", 1, RegistryValueKind.DWord);

        // stop services
        string[] serviceNames = {
            "AudioEndpointBuilder",
            "AppXSvc",
            "Appinfo",
            "camsvc",
            "CryptSvc",
            "gpsvc",
            "netprofm",
            "nsi",
            "ProfSvc",
            "StateRepository",
            "TextInputManagementService",
            "TrustedInstaller",
            "UserManager"
        };

        foreach (var serviceName in serviceNames)
        {
            try
            {
                var searcher = new ManagementObjectSearcher($"SELECT ProcessId FROM Win32_Service WHERE Name = '{serviceName}'");
                foreach (ManagementObject service in searcher.Get())
                {
                    try
                    {
                        int pid = Convert.ToInt32(service["ProcessId"]);
                        var process = Process.GetProcessById(pid);
                        process.Kill();
                        process.WaitForExit();
                    }
                    catch { }
                }
            }
            catch { }
        }

        try
        {
            var searcher = new ManagementObjectSearcher("SELECT Name, ProcessId FROM Win32_Service WHERE Name LIKE 'UdkUserSvc%'");
            foreach (ManagementObject service in searcher.Get())
            {
                try
                {
                    int pid = Convert.ToInt32(service["ProcessId"]);
                    var process = Process.GetProcessById(pid);
                    process.Kill();
                    process.WaitForExit();
                }
                catch { }
            }
        }
        catch { }

        try { new ServiceController("Winmgmt").Stop(); } catch { }
    }

    private async void LaunchExplorer_Click(object sender, RoutedEventArgs e)
    {
        // rename start menu binaries
        await Process.Start(new ProcessStartInfo { FileName = nsudoPath, Arguments = $@"-U:T -P:E -Wait -ShowWindowMode:Hide ren ""C:\Windows\System32\ctfmon.exee"" ctfmon.exe & ren ""C:\Windows\System32\RuntimeBroker.exee"" RuntimeBroker.exe & ren ""C:\Windows\SystemApps\ShellExperienceHost_cw5n1h2txyewy\ShellExperienceHost.exee"" ShellExperienceHost.exe & ren ""C:\Windows\SystemApps\MicrosoftWindows.Client.CBS_cw5n1h2txyewy\SearchHost.exee"" SearchHost.exe & ren ""C:\Windows\SystemApps\Microsoft.Windows.StartMenuExperienceHost_cw5n1h2txyewy\StartMenuExperienceHost.exee"" StartMenuExperienceHost.exe", CreateNoWindow = true }).WaitForExitAsync();

        // start audioendpoint builder
        using (ServiceController service = new ServiceController("AudioEndpointBuilder"))
        {
            if (service.Status == ServiceControllerStatus.Stopped)
            {
                service.Start();
            }
        }
        // launch ctfmon
        Process.Start("ctfmon.exe");

        // launch explorer
        Process.Start("explorer.exe");
    }

    private async void FortniteOptions_Click(object sender, RoutedEventArgs e)
    {
        
        var contentDialog = new ContentDialog
        {
            Title = "Fortnite",
            Content = new GameSettings(),
            PrimaryButtonText = "Close",
            XamlRoot = this.XamlRoot,
        };

        contentDialog.Resources["ContentDialogMaxWidth"] = 850;

        ContentDialogResult result = await contentDialog.ShowAsync();
    }

    private async void LaunchFortnite_Click(object sender, RoutedEventArgs e)
    {
        // alternative actions
        if (launchFortnite.Content?.ToString() == "Launching..." || launchFortnite.Content?.ToString() == "Running...")
        {
            return;
        }
        else if (launchFortnite.Content?.ToString().Contains("Update to") == true)
        {
            if (File.Exists(@"C:\Program Files (x86)\Epic Games\Launcher\Engine\Binaries\Win64\EpicGamesLauncher.exe"))
            {
                Process.Start(@"C:\Program Files (x86)\Epic Games\Launcher\Engine\Binaries\Win64\EpicGamesLauncher.exe");
                return;
            }
            else
            {
                return;
            }
        }

        isLaunchingFortnite = true;

        // check services state
        if (new ServiceController("Beep").Status == ServiceControllerStatus.Running)
        {
            var contentDialog = new ContentDialog
            {
                Title = "Attention Required",
                Content = "Are you sure that you want to launch Fortnite in the service enabled state?",
                PrimaryButtonText = "Yes",
                SecondaryButtonText = "No",
                XamlRoot = this.XamlRoot
            };
            ContentDialogResult result = await contentDialog.ShowAsync();

            // check result
            if (result == ContentDialogResult.Secondary)
            {
                isLaunchingFortnite = false;
                return;
            }
        }

        // launch fortnite
        Process.Start(new ProcessStartInfo("com.epicgames.launcher://apps/fn%3A4fe75bbc5a674f4f9b356b5c90567da5%3AFortnite?action=launch&silent=true") { UseShellExecute = true });

        // rename to launching...
        launchFortnite.Content = "Launching...";
    }

    private async void LoadEpicAccounts()
    {
        // clear list
        Accounts.Items.Clear();

        // search for all valid accounts
        var accountData = Directory.GetFiles(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"EpicGamesLauncher\Saved\Config\Windows"), "GameUserSettings.ini", SearchOption.AllDirectories)
            .Select(file =>
            {
                string configContent = File.ReadAllText(file);
                Match dataMatch = Regex.Match(configContent, @"Data=([^\r\n]+)");

                if (dataMatch.Success && dataMatch.Groups[1].Value.Length >= 1000)
                {
                    Match idMatch = Regex.Match(configContent, @"\[(.*?)_General\]");
                    if (idMatch.Success)
                    {
                        return idMatch.Groups[1].Value;
                    }
                }
                return null;
            })
            .Where(accountIdInFile => accountIdInFile != null)
            .Distinct()
            .OrderBy(accountIdInFile => accountIdInFile)
            .ToList();

        // add all valid accounts to the Accounts combobox
        foreach (var accountIdInFile in accountData)
        {
            if (!Accounts.Items.Contains(accountIdInFile))
            {
                Accounts.Items.Add(accountIdInFile);
            }
        }

        if (File.Exists(configFile))
        {
            // check if logged in
            string configContent = await File.ReadAllTextAsync(configFile);
            Match dataMatch = Regex.Match(configContent, @"Data=([^\r\n]+)");

            if (dataMatch.Success && dataMatch.Groups[1].Value.Length >= 1000)
            {
                // get accountId from it
                Match idMatch = Regex.Match(configContent, @"\[(.*?)_General\]");
                if (idMatch.Success)
                {
                    string accountId = idMatch.Groups[1].Value;
                    Accounts.SelectedItem = accountId;

                    // backup if not already
                    string accountDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"EpicGamesLauncher\Saved\Config\Windows\" + accountId);

                    if (!Directory.Exists(accountDir))
                    {
                        Directory.CreateDirectory(accountDir);
                        File.Copy(configFile, Path.Combine(accountDir, "GameUserSettings.ini"));
                    }
                    isInitializingAccounts = false;
                    return;
                }
            }

            if (accountData.Any())
            {
                string selectedAccount = accountData.First();

                // close epic games launcher
                if (Process.GetProcessesByName("EpicGamesLauncher").Length > 0)
                {
                    foreach (var process in Process.GetProcessesByName("EpicGamesLauncher"))
                    {
                        process.Kill();
                        process.WaitForExit();
                    }
                }

                // replace the file
                File.Copy(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"EpicGamesLauncher\Saved\Config\Windows", selectedAccount, "GameUserSettings.ini"), configFile, true);
                Accounts.SelectedItem = selectedAccount;

                // backup if not already
                string accountDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"EpicGamesLauncher\Saved\Config\Windows\" + selectedAccount);
                if (!Directory.Exists(accountDir))
                {
                    Directory.CreateDirectory(accountDir);
                    File.Copy(configFile, Path.Combine(accountDir, "GameUserSettings.ini"));
                }
                isInitializingAccounts = false;
            }
        }
        else
        {
            // show not logged in
            if (!accountData.Any())
            {
                Accounts.Items.Add("Not logged in");
                Accounts.SelectedItem = "Not logged in";
                launchFortnite.IsEnabled = false;
                removeButton.IsEnabled = false;
                isInitializingAccounts = false;
                return;
            }
            else
            {
                Accounts.SelectedItem = accountData.First();

                // close epic games launcher
                if (Process.GetProcessesByName("EpicGamesLauncher").Length > 0)
                {
                    foreach (var process in Process.GetProcessesByName("EpicGamesLauncher"))
                    {
                        process.Kill();
                        process.WaitForExit();
                    }
                }

                // replace the file with the selected account's data
                File.Copy(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"EpicGamesLauncher\Saved\Config\Windows", Accounts.SelectedItem?.ToString(), "GameUserSettings.ini"), configFile, true);

                // backup if not already
                string accountDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"EpicGamesLauncher\Saved\Config\Windows\" + Accounts.SelectedItem?.ToString());
                if (!Directory.Exists(accountDir))
                {
                    Directory.CreateDirectory(accountDir);
                    File.Copy(configFile, Path.Combine(accountDir, "GameUserSettings.ini"));
                }

                isInitializingAccounts = false;
            }
        }
    }

    private void Accounts_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (isInitializingAccounts) return;

        // close epic games launcher
        if (Process.GetProcessesByName("EpicGamesLauncher").Length > 0)
        {
            foreach (var process in Process.GetProcessesByName("EpicGamesLauncher"))
            {
                process.Kill();
                process.WaitForExit();
            }
        }

        // replace file
        File.Copy(Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"EpicGamesLauncher\Saved\Config\Windows", Accounts.SelectedItem?.ToString()), "GameUserSettings.ini"), configFile, true);
    }

    private async void AddAccount_Click(object sender, RoutedEventArgs e)
    {
        // add content dialog
        var contentDialog = new ContentDialog
        {
            Title = "Add New Account",
            Content = "Are you sure that you want to add another account?",
            PrimaryButtonText = "Yes",
            CloseButtonText = "Cancel",
            XamlRoot = this.XamlRoot
        };

        ContentDialogResult result = await contentDialog.ShowAsync();

        // check result
        if (result == ContentDialogResult.Primary)
        {
            // remove infobar
            AccountInfo.Children.Clear();

            // add infobar
            AccountInfo.Children.Add(new InfoBar
            {
                Title = "Please log in to your Epic Games account...",
                IsClosable = false,
                IsOpen = true,
                Severity = InfoBarSeverity.Informational,
                Margin = new Thickness(5)
            });

            // close epic games launcher
            if (Process.GetProcessesByName("EpicGamesLauncher").Length > 0)
            {
                foreach (var process in Process.GetProcessesByName("EpicGamesLauncher"))
                {
                    process.Kill();
                    process.WaitForExit();
                }
            }

            // delete gameusersettings.ini
            if (File.Exists(configFile))
            {
                File.Delete(configFile);
            }

            // delay
            await Task.Delay(500);

            // launch epic games launcher
            Process.Start(@"C:\Program Files (x86)\Epic Games\Launcher\Portal\Binaries\Win64\EpicGamesLauncher.exe");

            // check when logged in
            while (true)
            {
                if (File.Exists(configFile))
                {
                    string configContent = File.ReadAllText(configFile);
                    Match dataMatch = Regex.Match(configContent, @"Data=([^\r\n]+)");

                    if (dataMatch.Success && dataMatch.Groups[1].Value.Length >= 1000)
                    {
                        break;
                    }
                }

                await Task.Delay(500);
            }

            // close epic games launcher
            if (Process.GetProcessesByName("EpicGamesLauncher").Length > 0)
            {
                foreach (var process in Process.GetProcessesByName("EpicGamesLauncher"))
                {
                    process.Kill();
                    process.WaitForExit();
                }
            }

            // get new accountid
            string accountId = Regex.Match(File.ReadAllText(configFile), @"\[(.*?)_General\]").Groups[1].Value;

            // remove infobar
            AccountInfo.Children.Clear();

            // add infobar
            AccountInfo.Children.Add(new InfoBar
            {
                Title = $"Successfully logged in as {accountId}",
                IsClosable = false,
                IsOpen = true,
                Severity = InfoBarSeverity.Success,
                Margin = new Thickness(5)
            });

            // refresh combobox
            LoadEpicAccounts();

            // delay
            await Task.Delay(2000);

            // remove infobar
            AccountInfo.Children.Clear();
        }
    }

    private async void RemoveAccount_Click(object sender, RoutedEventArgs e)
    {
        // making sure an account is selected
        if (Accounts.SelectedItem != null)
        {
            // get accountid
            string accountId = Regex.Match(File.ReadAllText(configFile), @"\[(.*?)_General\]").Groups[1].Value;

            // add content dialog
            var contentDialog = new ContentDialog
            {
                Title = "Remove Account",
                Content = "Are you sure that you want to remove " + accountId + "?",
                PrimaryButtonText = "Yes",
                CloseButtonText = "No",
                XamlRoot = this.XamlRoot,
                DefaultButton = ContentDialogButton.Primary
            };
            ContentDialogResult result = await contentDialog.ShowAsync();

            // check results
            if (result == ContentDialogResult.Primary)
            {
                // add infobar
                AccountInfo.Children.Add(new InfoBar
                {
                    Title = "Removing " + accountId + "...",
                    IsClosable = false,
                    IsOpen = true,
                    Severity = InfoBarSeverity.Informational,
                    Margin = new Thickness(5)
                });

                // close epic games launcher
                if (Process.GetProcessesByName("EpicGamesLauncher").Length > 0)
                {
                    foreach (var process in Process.GetProcessesByName("EpicGamesLauncher"))
                    {
                        process.Kill();
                        process.WaitForExit();
                    }
                }

                // delete gameusersettings.ini
                File.Delete(configFile);

                // delay
                await Task.Delay(500);

                // remove infobar
                AccountInfo.Children.Clear();

                // refresh combobox
                LoadEpicAccounts();

                // add infobar
                AccountInfo.Children.Add(new InfoBar
                {
                    Title = "Successfully removed " + accountId + ".",
                    IsClosable = false,
                    IsOpen = true,
                    Severity = InfoBarSeverity.Success,
                    Margin = new Thickness(5)
                });

                // delay
                await Task.Delay(2000);

                // remove infobar
                AccountInfo.Children.Clear();
            }
        }
    }
}

