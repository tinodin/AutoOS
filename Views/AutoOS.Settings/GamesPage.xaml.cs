using System.Diagnostics;
using System.Management;
using System.ServiceProcess;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;

namespace AutoOS.Views.Settings;

public sealed partial class GamesPage : Page
{
    private string legendaryPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config", "legendary");
    private bool isLaunchingFortnite = false;
    private bool isInitializingAccounts = true;
    private bool isInitializingPresentationMode = true;
    private readonly string configFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"EpicGamesLauncher\Saved\Config\Windows\GameUserSettings.ini");
    public GamesPage()
    {
        InitializeComponent();
        //CheckFortniteUpdate();
        //CheckFortniteRunning();
        GetPresentationMode();
        LoadEpicAccounts();
    }

    private async void CheckFortniteUpdate()
    {
        if (Process.GetProcessesByName("FortniteClient-Win64-Shipping").Length == 0)
        {
            // get current version
            string currentVersion = Regex.Match(JObject.Parse(File.ReadAllText(Path.Combine(JObject.Parse(File.ReadAllText(Path.Combine(legendaryPath, "installed.json")))["Fortnite"]["install_path"]?.ToString(), "Cloud", "cloudcontent.json")))["BuildVersion"]?.ToString(), @"Release-(\d+\.\d+)").Groups[1].Value;
            fortniteVersion.Description = "Current version: " + currentVersion;

            // list installed games
            var output = await Task.Run(() => Process.Start(new ProcessStartInfo(@"C:\Windows\legendary.exe", "list-installed") { CreateNoWindow = true, RedirectStandardOutput = true })?.StandardOutput.ReadToEndAsync());

            // check output
            if (output.Contains("Latest: ++Fortnite+Release"))
            {
                // get newest version
                string newestVersion = Regex.Match(output, @"Latest: \+\+Fortnite\+Release-(\d+\.\d+)").Groups[1].Value;

                // delay
                await Task.Delay(500);

                // hide progress ring
                updateCheckProgress.Visibility = Visibility.Collapsed;

                // check if update is needed
                if (string.Compare(newestVersion, currentVersion, StringComparison.Ordinal) > 0)
                {
                    launchFortnite.Content = "Update to " + newestVersion;
                }
                else if (string.Compare(newestVersion, currentVersion, StringComparison.Ordinal) == 0)
                {
                    launchFortnite.Content = "Launch";
                }
            }
            else
            {
                // delay
                await Task.Delay(650);

                // hide progress ring
                updateCheckProgress.Visibility = Visibility.Collapsed;

                launchFortnite.Content = "Launch";
            }
        }
        else
        {
            // hide progress ring
            updateCheckProgress.Visibility = Visibility.Collapsed;
        }
    }

    private void CheckFortniteRunning()
    {
        if (Process.GetProcessesByName("FortniteClient-Win64-Shipping").Length > 0)
        {
            // rename to running
            launchFortnite.Content = "Running...";

            // check if stop processes button exists
            if (!FortniteCard.Children.OfType<Button>().Any(b => b.Content.ToString() == "Stop processes"))
            {
                // add stop processes button
                var stopProcesses = new Button
                {
                    Content = "Stop processes",
                    Margin = new Thickness(15, 0, 0, 0)
                };
                FortniteCard.Children.Add(stopProcesses);
                stopProcesses.Click += StopProcesses_Click;
            }

            // Check if "explorer.exe" is not running and add "Launch Explorer" button
            if (Process.GetProcessesByName("explorer").Length == 0)
            {
                // check if launch explorer button exists
                if (!FortniteCard.Children.OfType<Button>().Any(b => b.Content.ToString() == "Launch explorer"))
                {
                    // add launch explorer button
                    var launchExplorer = new Button
                    {
                        Content = "Launch explorer",
                        Margin = new Thickness(15, 0, 0, 0)
                    };
                    FortniteCard.Children.Add(launchExplorer);
                    launchExplorer.Click += LaunchExplorer_Click;
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
                        if (!FortniteCard.Children.OfType<Button>().Any(b => b.Content.ToString() == "Stop processes"))
                        {
                            // add stop processes button
                            var stopProcesses = new Button
                            {
                                Content = "Stop processes",
                                Margin = new Thickness(15, 0, 0, 0)
                            };

                            FortniteCard.Children.Add(stopProcesses);
                            stopProcesses.Click += StopProcesses_Click;
                            isLaunchingFortnite = false;
                        }

                        // check if explorer is not running
                        if (Process.GetProcessesByName("explorer").Length == 0)
                        {
                            // check if launch explorer button exists
                            if (!FortniteCard.Children.OfType<Button>().Any(b => b.Content.ToString() == "Launch explorer"))
                            {
                                // add launch explorer button
                                var launchExplorer = new Button
                                {
                                    Content = "Launch explorer",
                                    Margin = new Thickness(15, 0, 0, 0)
                                };
                                FortniteCard.Children.Add(launchExplorer);
                                launchExplorer.Click += LaunchExplorer_Click;
                            }
                        }
                        else
                        {
                            // remove launch explorer
                            FortniteCard.Children.Remove(FortniteCard.Children.OfType<Button>().FirstOrDefault(b => b.Content.ToString() == "Launch explorer"));
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
                            FortniteCard.Children.Remove(FortniteCard.Children.OfType<Button>().FirstOrDefault(b => b.Content.ToString() == "Stop processes"));

                            // remove launch explorer
                            FortniteCard.Children.Remove(FortniteCard.Children.OfType<Button>().FirstOrDefault(b => b.Content.ToString() == "Launch explorer"));

                            // launch explorer if not already
                            if (Process.GetProcessesByName("explorer").Length == 0)
                            {
                                Process.Start(new ProcessStartInfo("cmd.exe") { Arguments = "/c start explorer.exe", CreateNoWindow = true });
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

    private void StopProcesses_Click(object sender, RoutedEventArgs e)
    {
        string[] serviceNames = { "StateRepository", "Appinfo", "AppXSvc", "CryptSvc", "ProfSvc", "TextInputManagementService", "netprofm", "nsi" };

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
                        Process.GetProcessById(pid)?.Kill();
                    }
                    catch { }
                }
            }
            catch { }
        }

        string[] processNames = { "explorer", "ApplicationFrameHost", "WmiPrvSE", "WMIADAP", "useroobebroker", "TrustedInstaller", "FortniteClient-Win64-Shipping_EAC_EOS", "EasyAntiCheat_EOS", "CrashReportClient", "sppsvc", "secd.exe" };

        Registry.SetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon", "AutoRestartShell", 0, RegistryValueKind.DWord);

        foreach (var name in processNames)
        {
            foreach (var process in Process.GetProcessesByName(name))
            {
                try { process.Kill(); } catch { }
            }
        }

        try { new System.ServiceProcess.ServiceController("Winmgmt").Stop(); } catch { }

        Registry.SetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon", "AutoRestartShell", 1, RegistryValueKind.DWord);
    }

    private void LaunchExplorer_Click(object sender, RoutedEventArgs e)
    {
        // launch explorer
        Process.Start(new ProcessStartInfo("cmd.exe") { Arguments = "/c start explorer.exe", CreateNoWindow = true });
    }

    private void GetPresentationMode()
    {
        using (var key = Registry.CurrentUser.OpenSubKey(@"System\GameConfigStore\Children"))
        {
            foreach (var subKeyName in key.GetSubKeyNames())
            using (var subKey = key.OpenSubKey(subKeyName))
            {
                if (subKey.GetValueNames().Any(valueName => subKey.GetValue(valueName) is string strValue && strValue.Contains("Fortnite")))
                {
                    int flags = Convert.ToInt32(subKey.GetValue("Flags"));
                    Debug.WriteLine($"SubKey: {subKeyName}, Flags: {flags}");
                    if (flags == 0x211)
                    {
                        PresentationMode.SelectedIndex = 1;
                        isInitializingPresentationMode = false;
                        return;
                    }
                    else
                    {
                        PresentationMode.SelectedIndex = 0;
                        isInitializingPresentationMode = false;
                    }
                }
            }
        }
    }

    private void PresentationMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (isInitializingPresentationMode) return;

        using (var key = Registry.CurrentUser.OpenSubKey(@"System\GameConfigStore\Children", true))
        {
            foreach (var subKeyName in key.GetSubKeyNames())
            {
                using (var subKey = key.OpenSubKey(subKeyName, true))
                {
                    if (subKey.GetValueNames().Any(valueName => subKey.GetValue(valueName) is string strValue && strValue.Contains("Fortnite")))
                    {
                        string cmd = "";
                        if (PresentationMode.SelectedIndex == 0)
                        {
                            cmd = "reg delete \"HKCU\\System\\GameConfigStore\\Children\\" + subKeyName + "\" /v Flags /f";
                        }
                        else if (PresentationMode.SelectedIndex == 1)
                        {
                            cmd = "reg add \"HKCU\\System\\GameConfigStore\\Children\\" + subKeyName + "\" /v Flags /t REG_DWORD /d 0x211 /f";
                        }

                        if (!string.IsNullOrEmpty(cmd))
                        {
                            var process = new Process
                            {
                                StartInfo = new ProcessStartInfo
                                {
                                    FileName = "cmd.exe",
                                    Arguments = "/C " + cmd,
                                    CreateNoWindow = true,
                                }
                            };
                            process.Start();
                            process.WaitForExit();
                        }

                        return;
                    }
                }
            }
        }
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
                XamlRoot = this.XamlRoot,
                DefaultButton = ContentDialogButton.Primary
            };
            ContentDialogResult result = await contentDialog.ShowAsync();

            // check result
            if (result == ContentDialogResult.Secondary)
            {
                isLaunchingFortnite = false;
                return;
            }
        }
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
                // valid configFile, get accountId from it
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
                    isInitializingAccounts = false; // Set to false when we are done
                    return;
                }
            }

            // if configFile is invalid, replace it with the first found account
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
                isInitializingAccounts = false; // Set to false after handling the first account
            }
        }
        else
        {
            // if configFile doesn't exist and no valid account found, show "Not logged in"
            if (!accountData.Any())
            {
                Accounts.Items.Add("Not logged in");
                Accounts.SelectedItem = "Not logged in";
                launchFortnite.IsEnabled = false;
                removeButton.IsEnabled = false;
                isInitializingAccounts = false; // Set to false when no accounts are found
                return;
            }
            else
            {
                // automatically select the first account from the combobox if no configFile exists
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
                isInitializingAccounts = false; // Set to false after selecting the first account
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
            Content = "Are you sure that you want to add another account",
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

