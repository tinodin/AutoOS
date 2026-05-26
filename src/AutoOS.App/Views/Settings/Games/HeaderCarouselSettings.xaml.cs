using AutoOS.Core.Helpers.Games;
using AutoOS.Core.Helpers.Processes;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using ValveKeyValue;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace AutoOS.Views.Settings.Games;

public partial class HeaderCarouselSettings : ContentDialog
{
    private readonly ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
    private bool isInitializingEpicGamesAccounts = true;
    private bool isInitializingSteamAccounts = true;
    private bool isInitializingSwitchEmulatorState = true;
    private bool isInitializingPlaytimeFormat = true;

    public HeaderCarouselSettings()
    {
        InitializeComponent();
        Loaded += HeaderCarouselSettings_Loaded;
    }

    private void HeaderCarouselSettings_Loaded(object sender, RoutedEventArgs e)
    {
        LoadPlaytimeFormatPreference();
        LoadEpicGamesAccounts();
        LoadSteamAccounts();
        GetSwitchEmulator();
        LoadExpanderStates();
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        // Save expander states before closing
        localSettings.Values["EmulatorsSettingsExpander_IsExpanded"] = EmulatorsSettingsExpander.IsExpanded;
        Hide();
    }

    private void LoadExpanderStates()
    {
        // Load Emulators SettingsExpander state
        var emulatorsExpanded = localSettings.Values["EmulatorsSettingsExpander_IsExpanded"] as bool? ?? false;
        EmulatorsSettingsExpander.IsExpanded = emulatorsExpanded;
    }

    private void LoadPlaytimeFormatPreference()
    {
        var format = localSettings.Values["PlaytimeFormat"]?.ToString() ?? "Compact";
        GameModel.PlaytimeFormat = format;
        
        if (format == "Compact")
        {
            PlaytimeFormatComboBox.SelectedIndex = 0;
        }
        else
        {
            PlaytimeFormatComboBox.SelectedIndex = 1;
        }

        isInitializingPlaytimeFormat = false;
    }

    private void PlaytimeFormatComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (isInitializingPlaytimeFormat) return;

        if (PlaytimeFormatComboBox.SelectedItem is ComboBoxItem selectedItem)
        {
            var format = selectedItem.Tag?.ToString() ?? "Compact";
            localSettings.Values["PlaytimeFormat"] = format;
            GameModel.SetPlaytimeFormat(format);
        }
    }

    private void LoadEpicGamesAccounts()
    {
        if (File.Exists(EpicGamesHelper.EpicGamesPath))
        {
            var accounts = EpicGamesHelper.GetEpicGamesAccounts();
            EpicGamesAccounts.Items.Clear();
            EpicGamesAccounts.IsEnabled = accounts.Count > 0;

            if (accounts.Count == 0)
            {
                var notLoggedIn = new ComboBoxItem { Content = "Not logged in", IsEnabled = false };
                EpicGamesAccounts.Items.Add(notLoggedIn);
                EpicGamesAccounts.SelectedItem = notLoggedIn;
                EpicGamesAccounts.IsEnabled = false;
            }
            else if (!accounts.Any(a => a.IsActive))
            {
                var notLoggedIn = new ComboBoxItem { Content = "Not logged in", IsEnabled = false };
                EpicGamesAccounts.Items.Add(notLoggedIn);
                EpicGamesAccounts.SelectedItem = notLoggedIn;

                foreach (var account in accounts)
                {
                    var item = new ComboBoxItem
                    {
                        Content = account.DisplayName,
                        Tag = account.AccountId
                    };
                    EpicGamesAccounts.Items.Add(item);
                }
            }
            else
            {
                foreach (var account in accounts)
                {
                    var item = new ComboBoxItem
                    {
                        Content = account.DisplayName,
                        Tag = account.AccountId
                    };
                    EpicGamesAccounts.Items.Add(item);

                    if (account.IsActive)
                        EpicGamesAccounts.SelectedItem = item;
                }
            }
        }
        EpicGamesAccounts.SelectionChanged += EpicGamesAccounts_SelectionChanged;
        isInitializingEpicGamesAccounts = false;
    }

    private async void EpicGamesAccounts_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (isInitializingEpicGamesAccounts) return;

        // close epic games launcher
        EpicGamesHelper.CloseEpicGames();

        // update config before switching
        if (EpicGamesHelper.ValidateData(EpicGamesHelper.ActiveEpicGamesAccountPath))
        {
            var (oldAccountId, _, _, _) = EpicGamesHelper.GetAccountData(EpicGamesHelper.ActiveEpicGamesAccountPath);

            string accountDir = Path.Combine(EpicGamesHelper.EpicGamesAccountDir, oldAccountId);
            if (Directory.Exists(accountDir))
                File.Copy(EpicGamesHelper.ActiveEpicGamesAccountPath, Path.Combine(accountDir, "GameUserSettings.ini"), true);
        }

        // get accountId
        string accountId = (EpicGamesAccounts.SelectedItem as ComboBoxItem)?.Tag as string;

        // replace file
        File.Copy(Path.Combine(EpicGamesHelper.EpicGamesAccountDir, accountId, "GameUserSettings.ini"), EpicGamesHelper.ActiveEpicGamesAccountPath, true);

        // replace accountid
        Process.Start("regedit.exe", $@"/s ""{Path.Combine(EpicGamesHelper.EpicGamesAccountDir, accountId, "accountId.reg")}""");

        // update refresh token
        if (await EpicGamesHelper.UpdateEpicGamesToken(EpicGamesHelper.ActiveEpicGamesAccountPath) == null)
        {
            isInitializingEpicGamesAccounts = true;
            LoadEpicGamesAccounts();
            return;
        }

        // close epic games launcher
        EpicGamesHelper.CloseEpicGames();

        isInitializingEpicGamesAccounts = true;
        LoadEpicGamesAccounts();
    }

    private async void AddEpicGamesAccount_Click(object sender, RoutedEventArgs e)
    {
        // Open Epic Games account management page in browser
        await Windows.System.Launcher.LaunchUriAsync(new Uri("https://www.epicgames.com/account"));
    }

    private void RemoveEpicGamesAccount_Click(object sender, RoutedEventArgs e)
    {
        if (EpicGamesAccounts.SelectedItem is ComboBoxItem selectedItem && selectedItem.Tag is string accountId)
        {
            string accountDir = Path.Combine(EpicGamesHelper.EpicGamesAccountDir, accountId);
            if (Directory.Exists(accountDir))
            {
                Directory.Delete(accountDir, true);
            }

            isInitializingEpicGamesAccounts = true;
            LoadEpicGamesAccounts();
        }
    }

    private void LoadSteamAccounts()
    {
        if (File.Exists(SteamHelper.SteamPath))
        {
            var accounts = SteamHelper.GetSteamAccounts();
            SteamAccounts.Items.Clear();
            SteamAccounts.IsEnabled = true;

            if (accounts.Count == 0)
            {
                var notLoggedIn = new ComboBoxItem { Content = "Not logged in", IsEnabled = false };
                SteamAccounts.Items.Add(notLoggedIn);
                SteamAccounts.SelectedItem = notLoggedIn;
                SteamAccounts.IsEnabled = false;
            }
            else if (accounts.All(a => !a.MostRecent) || accounts.All(a => !a.AllowAutoLogin))
            {
                var notLoggedIn = new ComboBoxItem { Content = "Not logged in", IsEnabled = false };
                SteamAccounts.Items.Add(notLoggedIn);
                SteamAccounts.SelectedItem = notLoggedIn;

                foreach (var account in accounts)
                {
                    SteamAccounts.Items.Add(account.AccountName);
                }
            }
            else
            {
                foreach (var account in accounts)
                {
                    SteamAccounts.Items.Add(account.AccountName);
                }

                int selectedIndex = accounts.FindIndex(a => a.MostRecent);
                if (selectedIndex < 0)
                    selectedIndex = accounts.FindIndex(a => a.AllowAutoLogin);

                SteamAccounts.SelectedIndex = selectedIndex >= 0 ? selectedIndex : 0;
            }
        }
        SteamAccounts.SelectionChanged += SteamAccounts_SelectionChanged;
        isInitializingSteamAccounts = false;
    }

    private async void SteamAccounts_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (isInitializingSteamAccounts) return;

        // close steam
        SteamHelper.CloseSteam();

        // read file
        var options = new KVSerializerOptions
        {
            HasEscapeSequences = true,
        };

        var kv = KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Deserialize(new MemoryStream(Encoding.UTF8.GetBytes(File.ReadAllText(SteamHelper.SteamLoginUsersPath))), options);

        // make all accounts inactive
        foreach (var user in kv.Root.Children)
        {
            if (user.Value["AccountName"]?.ToString() == SteamAccounts.SelectedItem.ToString())
            {
                user.Value["MostRecent"] = "1";
                user.Value["AllowAutoLogin"] = "1";
                user.Value["Timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
            }
            else
            {
                user.Value["MostRecent"] = "0";
                user.Value["AllowAutoLogin"] = "0";
            }
        }

        // write changes
        using var msOut = new MemoryStream();
        KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Serialize(msOut, kv);
        msOut.Position = 0;
        File.WriteAllText(SteamHelper.SteamLoginUsersPath, new StreamReader(msOut).ReadToEnd());

        // update registry key
        Registry.SetValue(@"HKEY_CURRENT_USER\Software\Valve\Steam", "AutoLoginUser", SteamAccounts.SelectedItem.ToString(), RegistryValueKind.String);

        isInitializingSteamAccounts = true;
        LoadSteamAccounts();
    }

    private void AddSteamAccount_Click(object sender, RoutedEventArgs e)
    {
        if (File.Exists(SteamHelper.SteamPath))
        {
            // Open Steam account management screen
            Process.Start(new ProcessStartInfo(SteamHelper.SteamPath) { UseShellExecute = true, Arguments = "steam://open/account" });
        }
    }

    private void RemoveSteamAccount_Click(object sender, RoutedEventArgs e)
    {
        if (SteamAccounts.SelectedItem == null) return;
        string selectedAccount = SteamAccounts.SelectedItem.ToString();
        if (selectedAccount == "Not logged in") return;

        var options = new KVSerializerOptions { HasEscapeSequences = true };
        var kv = KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Deserialize(new MemoryStream(Encoding.UTF8.GetBytes(File.ReadAllText(SteamHelper.SteamLoginUsersPath))), options);

        var userToRemove = kv.Root.Children.FirstOrDefault(user => user.Value["AccountName"]?.ToString() == selectedAccount);
        
        if (userToRemove.Key != null)
        {
            // FIX: Call Remove natively on the root node instead of looking up explicit extension methods on Children
            kv.Root.Remove(userToRemove.Key);

            using var msOut = new MemoryStream();
            KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Serialize(msOut, kv);
            msOut.Position = 0;
            File.WriteAllText(SteamHelper.SteamLoginUsersPath, new StreamReader(msOut).ReadToEnd());
        }

        isInitializingSteamAccounts = true;
        LoadSteamAccounts();
    }

    private void GetSwitchEmulator()
    {
        var selectedSwitchEmulator = localSettings.Values["SwitchEmulator"] as string ?? "Eden";
        
        foreach (ComboBoxItem item in EmulatorComboBox.Items)
        {
            if (item.Tag?.ToString() == selectedSwitchEmulator)
            {
                EmulatorComboBox.SelectedItem = item;
                break;
            }
        }

        UpdateEmulatorIcon(selectedSwitchEmulator);

        DataLocationValue.IsEnabled = selectedSwitchEmulator == "Ryujinx";
        DataLocationValue.IsReadOnly = selectedSwitchEmulator != "Ryujinx";

        ExecutableLocationValue.Text = localSettings.Values[$"{selectedSwitchEmulator}Location"] as string ?? string.Empty;
        DataLocationValue.Text = localSettings.Values[$"{selectedSwitchEmulator}DataLocation"] as string ?? string.Empty;

        EmulatorComboBox.SelectionChanged += EmulatorComboBox_SelectionChanged;
        ExecutableLocationValue.TextChanged += ExecutableLocation_TextChanged;
        DataLocationValue.TextChanged += DataLocation_TextChanged;

        isInitializingSwitchEmulatorState = false;
    }

    private void UpdateEmulatorIcon(string emulator)
    {
        var iconSource = emulator switch
        {
            "Eden" => new BitmapImage(new Uri("ms-appx:///Assets/Fluent/Eden.png")),
            "Citron" => new BitmapImage(new Uri("ms-appx:///Assets/Fluent/Citron.png")),
            "Ryujinx" => new BitmapImage(new Uri("ms-appx:///Assets/Fluent/Ryujinx.png")),
            _ => new BitmapImage(new Uri("ms-appx:///Assets/Fluent/Eden.png"))
        };
        SelectedEmulatorIcon.Source = iconSource;
    }

    private void EmulatorComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (isInitializingSwitchEmulatorState) return;

        if (EmulatorComboBox.SelectedItem is ComboBoxItem selectedItem)
        {
            var emulator = selectedItem.Tag?.ToString() ?? "Eden";
            
            UpdateEmulatorIcon(emulator);
            
            DataLocationValue.IsEnabled = emulator == "Ryujinx";
            DataLocationValue.IsReadOnly = emulator != "Ryujinx";

            ExecutableLocationValue.Text = localSettings.Values[$"{emulator}Location"] as string ?? string.Empty;
            DataLocationValue.Text = localSettings.Values[$"{emulator}DataLocation"] as string ?? string.Empty;

            localSettings.Values["SwitchEmulator"] = emulator;
        }
    }

    private void ExecutableLocation_TextChanged(object sender, RoutedEventArgs e)
    {
        if (EmulatorComboBox.SelectedItem is ComboBoxItem selectedItem)
        {
            string emulator = selectedItem.Tag?.ToString() ?? "Eden";

            if (!string.IsNullOrWhiteSpace(ExecutableLocationValue?.Text))
            {
                localSettings.Values[$"{emulator}Location"] = ExecutableLocationValue.Text;
            }
        }
    }

    private void DataLocation_TextChanged(object sender, RoutedEventArgs e)
    {
        if (EmulatorComboBox.SelectedItem is ComboBoxItem selectedItem && selectedItem.Tag?.ToString() == "Ryujinx")
        {
            if (!string.IsNullOrWhiteSpace(DataLocationValue?.Text))
            {
                localSettings.Values["RyujinxDataLocation"] = DataLocationValue.Text;
            }
        }
    }

    private async void ExecutableLocation_Click(object sender, RoutedEventArgs e)
    {
        if (EmulatorComboBox.SelectedItem is not ComboBoxItem selectedItem)
            return;

        var emulator = selectedItem.Tag?.ToString() ?? "Eden";
        var picker = new FileOpenPicker
        {
            ViewMode = PickerViewMode.List,
            SuggestedStartLocation = PickerLocationId.ComputerFolder
        };

        picker.FileTypeFilter.Add(".exe");

        var hwnd = WindowNative.GetWindowHandle(App.MainWindow);
        InitializeWithWindow.Initialize(picker, hwnd);

        var file = await picker.PickSingleFileAsync();
        if (file != null)
        {
            string exeName = Path.GetFileName(file.Path).ToLowerInvariant();

            if (exeName.Contains("eden") && emulator == "Eden")
            {
                ExecutableLocationValue.Text = file.Path;
                localSettings.Values[$"{emulator}Location"] = file.Path;
            }
            else if (exeName.Contains("citron") && emulator == "Citron")
            {
                ExecutableLocationValue.Text = file.Path;
                localSettings.Values[$"{emulator}Location"] = file.Path;
            }
            else if (exeName.Contains("ryujinx") && emulator == "Ryujinx")
            {
                ExecutableLocationValue.Text = file.Path;
                localSettings.Values[$"{emulator}Location"] = file.Path;
            }
        }
    }

    private async void DataLocation_Click(object sender, RoutedEventArgs e)
    {
        if (EmulatorComboBox.SelectedItem is not ComboBoxItem selectedItem || selectedItem.Tag?.ToString() != "Ryujinx")
            return;

        var picker = new FolderPicker
        {
            SuggestedStartLocation = PickerLocationId.ComputerFolder
        };

        var hwnd = WindowNative.GetWindowHandle(App.MainWindow);
        InitializeWithWindow.Initialize(picker, hwnd);

        var folder = await picker.PickSingleFolderAsync();
        if (folder != null)
        {
            DataLocationValue.Text = folder.Path;
            localSettings.Values["RyujinxDataLocation"] = folder.Path;
        }
    }
}