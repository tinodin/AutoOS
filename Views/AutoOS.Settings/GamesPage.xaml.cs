namespace AutoOS.Views.Settings;

using System.Diagnostics;
using System.Management;
using System.ServiceProcess;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;

public sealed partial class GamesPage : Page
{
    private string legendaryPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config", "legendary");
    private bool isLaunchingFortnite = false;
    private bool isInitializingAccounts = true;
    private bool isInitializingPresentationMode = true;

    public GamesPage()
    {
        InitializeComponent();
        CheckFortniteUpdate();
        CheckFortniteRunning();
        GetPresentationMode();
        LoadAccounts();
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
                        if (PresentationMode.SelectedIndex == 0)
                        {
                            Debug.WriteLine("deleting");
                            subKey.DeleteValue("Flags", false);
                        }
                        else if (PresentationMode.SelectedIndex == 1)
                        {
                            Debug.WriteLine("setting");
                            subKey.SetValue("Flags", 0x211, RegistryValueKind.DWord);
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

        // launch fortnite
        var output = await Task.Run(() => Process.Start(new ProcessStartInfo(@"C:\Windows\legendary.exe", "launch Fortnite") { CreateNoWindow = true, RedirectStandardError = true })?.StandardError.ReadToEndAsync());

        // success or fail
        if (output.Contains("ERROR: Login failed"))
        {
            // get the display name of the current account
            string currentDisplayName = File.Exists(Path.Combine(legendaryPath, "user.json"))
                ? JObject.Parse(File.ReadAllText(Path.Combine(legendaryPath, "user.json")))["displayName"]?.ToString()
                : null;

            // add infobar
            FortniteInfo.Children.Add(new InfoBar
            {
                Title = "The login session is no longer valid.",
                IsClosable = false,
                IsOpen = true,
                Severity = InfoBarSeverity.Error,
                Margin = new Thickness(5)
            });

            // delay
            await Task.Delay(350);

            // add infobar
            AccountInfo.Children.Add(new InfoBar
            {
                Title = "Removing " + currentDisplayName + "...",
                IsClosable = false,
                IsOpen = true,
                Severity = InfoBarSeverity.Informational,
                Margin = new Thickness(5)
            });

            // delay
            await Task.Delay(500);

            // remove current account data
            Directory.Delete(Path.Combine(legendaryPath, currentDisplayName), true);
            File.Delete(Path.Combine(legendaryPath, "user.json"));

            // remove infobar
            AccountInfo.Children.Clear();

            // refresh combobox
            isInitializingAccounts = true;
            LoadAccounts();

            // replace the account data
            if (Accounts.SelectedItem?.ToString() != null)
            {
                File.Copy(Path.Combine(legendaryPath, Accounts.SelectedItem?.ToString(), "user.json"), Path.Combine(legendaryPath, "user.json"), true);
            }

            // add infobar
            AccountInfo.Children.Add(new InfoBar
            {
                Title = "Successfully removed " + currentDisplayName + ".",
                IsClosable = false,
                IsOpen = true,
                Severity = InfoBarSeverity.Success,
                Margin = new Thickness(5)
            });

            // delay
            await Task.Delay(2000);

            // remove infobar
            AccountInfo.Children.Clear();

            isLaunchingFortnite = false;
        }
        else if (output.Contains("ERROR: Game is out of date"))
        {
            isLaunchingFortnite = false;
            launchFortnite.Content = "Update required";
        }
        else if (output.Contains("ConnectionError"))
        {
            isLaunchingFortnite = false;
            launchFortnite.Content = "No internet connection";

            // delay
            await Task.Delay(1000);

            launchFortnite.Content = "Launch";
        }
        else if (output.Contains("INFO: Launching Fortnite..."))
        {
            launchFortnite.Content = "Launching...";
        }
    }

    private void LoadAccounts()
    {
        // clear list
        Accounts.Items.Clear();

        // search for all users
        var accountData = Directory.GetFiles(legendaryPath, "user.json", SearchOption.AllDirectories)
            .Select(file =>
            {
                var jsonData = JObject.Parse(File.ReadAllText(file));
                return jsonData["displayName"]?.ToString();
            })
            .Where(name => !string.IsNullOrEmpty(name))
            .Distinct()
            .OrderBy(name => name)
            .ToList();

        // add all accounts
        foreach (var name in accountData)
        {
            if (!Accounts.Items.Contains(name))
            {
                Accounts.Items.Add(name);
            }
        }

        // get the display name of the current account
        string currentDisplayName = File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config", "legendary", "user.json"))
            ? JObject.Parse(File.ReadAllText(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config", "legendary", "user.json")))["displayName"]?.ToString()
            : null;

        // select the current user / default to the first item if not found
        Accounts.SelectedIndex = Accounts.Items.IndexOf(currentDisplayName) is int index && index != -1 ? index : 0;

        // disable buttons if no account is selected
        if (Accounts.SelectedItem == null)
        {
            launchFortnite.IsEnabled = false;
            removeButton.IsEnabled = false;
        }
        else
        {
            launchFortnite.IsEnabled = true;
            removeButton.IsEnabled = true;
        }

        isInitializingAccounts = false;
    }

    private void Accounts_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (isInitializingAccounts) return;

        // replace the account data
        File.Copy(Path.Combine(legendaryPath, Accounts.SelectedItem?.ToString(), "user.json"), Path.Combine(legendaryPath, "user.json"), true);
    }

    private async void AddAccount_Click(object sender, RoutedEventArgs e)
    {
        // add content dialog
        var contentDialog = new ContentDialog
        {
            Title = "Add New Account",
            Content = "Choose your preferred login method:",
            PrimaryButtonText = "Import from Epic Games Launcher",
            SecondaryButtonText = "Login via WebView",
            CloseButtonText = "Cancel",
            XamlRoot = this.XamlRoot
        };

        // fix button text
        contentDialog.Resources["ContentDialogMaxWidth"] = 1080;
        ContentDialogResult result = await contentDialog.ShowAsync();

        // check result
        if (result == ContentDialogResult.Primary)
        {
            // get all drives
            string[] drives = Environment.GetLogicalDrives().OrderBy(d => d).ToArray();
            string targetDrive = null;
            string targetPath = null;

            // check on every drive
            foreach (string drive in drives)
            {
                // check for user folder
                string usersPath = Path.Combine(drive, "Users");
                if (!Directory.Exists(usersPath)) continue;

                // check every user
                foreach (string userDir in Directory.GetDirectories(usersPath))
                {
                    // check if data is valid (500+ characters)
                    if (File.Exists(Path.Combine(userDir, "AppData", "Local", "EpicGamesLauncher", "Saved", "Config", "Windows", "GameUserSettings.ini")))
                    {
                        string[] lines = File.ReadAllLines(Path.Combine(userDir, "AppData", "Local", "EpicGamesLauncher", "Saved", "Config", "Windows", "GameUserSettings.ini"));
                        string dataLine = lines.FirstOrDefault(line => line.StartsWith("Data="));
                        if (dataLine != null && dataLine.Length > 505)
                        {
                            targetDrive = drive.TrimEnd('\\');
                            targetPath = Path.Combine(userDir, "AppData", "Local", "EpicGamesLauncher", "Saved", "Config", "Windows", "GameUserSettings.ini");
                            break;
                        }
                    }
                }
            }

            // check if found
            if (targetPath != null)
            {
                // if not already on C:
                if (targetDrive != "C:")
                {
                    // create destination directory
                    Directory.CreateDirectory(Path.GetDirectoryName(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "EpicGamesLauncher", "Saved", "Config", "Windows")));

                    // copy the file
                    File.Copy(targetPath, Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "EpicGamesLauncher", "Saved", "Config", "Windows", "GameUserSettings.ini"), true);
                }

                // remove infobar
                AccountInfo.Children.Clear();

                // remove current account data
                File.Delete(Path.Combine(legendaryPath, "user.json"));

                // add infobar
                AccountInfo.Children.Add(new InfoBar
                {
                    Title = "Logging in using Epic Games Launcher account data...",
                    IsClosable = false,
                    IsOpen = true,
                    Severity = InfoBarSeverity.Informational,
                    Margin = new Thickness(5)
                });

                // log in
                var output = await Task.Run(() => Process.Start(new ProcessStartInfo(@"C:\Windows\legendary.exe", "auth --import") { CreateNoWindow = true, RedirectStandardError = true})?.StandardError.ReadToEndAsync());

                // success or fail
                if (output.Contains("Now logged in as user"))
                {
                    // get the display name of the current account
                    string currentDisplayName = File.Exists(Path.Combine(legendaryPath, "user.json"))
                        ? JObject.Parse(File.ReadAllText(Path.Combine(legendaryPath, "user.json")))["displayName"]?.ToString()
                        : null;

                    // create folder with account data
                    Directory.CreateDirectory(Path.Combine(legendaryPath, currentDisplayName));
                    File.Copy(Path.Combine(legendaryPath, "user.json"), Path.Combine(legendaryPath, currentDisplayName, "user.json"), true);

                    // refresh combobox
                    isInitializingAccounts = true;
                    LoadAccounts();

                    // remove infobar
                    AccountInfo.Children.Clear();

                    // add infobar
                    AccountInfo.Children.Add(new InfoBar
                    {
                        Title = $"Successfully logged in as {currentDisplayName}",
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
                else if (output.Contains("WARNING"))
                {
                    // remove infobar
                    AccountInfo.Children.Clear();

                    // add infobar
                    AccountInfo.Children.Add(new InfoBar
                    {
                        Title = "The login session is no longer valid.",
                        IsClosable = false,
                        IsOpen = true,
                        Severity = InfoBarSeverity.Error,
                        Margin = new Thickness(5)
                    });

                    // delay
                    await Task.Delay(2000);

                    // remove infobar
                    AccountInfo.Children.Clear();
                }   
            }
            else
            {
                // add infobar
                AccountInfo.Children.Add(new InfoBar
                {
                    Title = $"No Epic Games Launcher account data found.",
                    IsClosable = false,
                    IsOpen = true,
                    Severity = InfoBarSeverity.Error,
                    Margin = new Thickness(5)
                });

                // delay
                await Task.Delay(2000);

                // remove infobar
                AccountInfo.Children.Clear();
            }
        }
        else if (result == ContentDialogResult.Secondary)
        {
            // remove current account data
            File.Delete(Path.Combine(legendaryPath, "user.json"));

            // add infobar
            AccountInfo.Children.Add(new InfoBar
            {
                Title = "Launching the WebView login window...",
                IsClosable = false,
                IsOpen = true,
                Severity = InfoBarSeverity.Informational,
                Margin = new Thickness(5)
            });

            // log in
            var output = await Task.Run(() => Process.Start(new ProcessStartInfo(@"C:\Windows\legendary.exe", "auth") { CreateNoWindow = true, RedirectStandardError = true })?.StandardError.ReadToEndAsync());

            // success or fail
            if (output.Contains("Successfully logged in as"))
            {
                // get the display name of the current account
                string currentDisplayName = File.Exists(Path.Combine(legendaryPath, "user.json"))
                    ? JObject.Parse(File.ReadAllText(Path.Combine(legendaryPath, "user.json")))["displayName"]?.ToString()
                    : null;

                // create folder with account data
                Directory.CreateDirectory(Path.Combine(legendaryPath, currentDisplayName));
                File.Copy(Path.Combine(legendaryPath, "user.json"), Path.Combine(legendaryPath, currentDisplayName, "user.json"), true);

                // refresh combobox
                isInitializingAccounts = true;
                LoadAccounts();

                // remove infobar
                AccountInfo.Children.Clear();

                // add infobar
                AccountInfo.Children.Add(new InfoBar
                {
                    Title = $"Successfully logged in as {currentDisplayName}",
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
            else
            {
                // remove infobar
                AccountInfo.Children.Clear();

                // refresh combobox
                isInitializingAccounts = true;
                LoadAccounts();
            }
        }
    }

    private async void RemoveAccount_Click(object sender, RoutedEventArgs e)
    {
        // making sure an account is selected
        if (Accounts.SelectedItem != null)
        {
            // get the display name of the current account
            string currentDisplayName = File.Exists(Path.Combine(legendaryPath, "user.json"))
                ? JObject.Parse(File.ReadAllText(Path.Combine(legendaryPath, "user.json")))["displayName"]?.ToString()
                : null;


            // add content dialog
            var contentDialog = new ContentDialog
            {
                Title = "Remove Account",
                Content = "Are you sure that you want to remove " + currentDisplayName + "?",
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
                    Title = "Removing " + currentDisplayName + "...",
                    IsClosable = false,
                    IsOpen = true,
                    Severity = InfoBarSeverity.Informational,
                    Margin = new Thickness(5)
                });

                // delay
                await Task.Delay(500);

                // remove current account data
                Directory.Delete(Path.Combine(legendaryPath, currentDisplayName), true);
                File.Delete(Path.Combine(legendaryPath, "user.json"));

                // remove infobar
                AccountInfo.Children.Clear();

                // refresh combobox
                isInitializingAccounts = true;
                LoadAccounts();

                // replace the account data
                if (Accounts.SelectedItem?.ToString() != null)
                {
                    File.Copy(Path.Combine(legendaryPath, Accounts.SelectedItem?.ToString(), "user.json"), Path.Combine(legendaryPath, "user.json"), true);
                }

                // add infobar
                AccountInfo.Children.Add(new InfoBar
                {
                    Title = "Successfully removed " + currentDisplayName + ".",
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

