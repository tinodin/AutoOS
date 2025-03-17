using AutoOS.Views.Installer.Actions;
using Microsoft.UI.Xaml.Media;
using Microsoft.Win32;
using System.Diagnostics;
using System.Management;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace AutoOS.Views.Installer.Stages;

public static class PreparingStage
{
    public static bool? Desktop;
    public static bool? IdleStates;
    public static bool? PowerService;

    public static bool? SSD;
    public static bool? Wifi;
    public static bool? Bluetooth;
    public static bool? Rename;

    public static bool? WindowsDefender;
    public static bool? UserAccountControl;
    public static bool? DEP;
    public static bool? INTELCPU;
    public static bool? AMDCPU;
    public static bool? SpectreMeltdownMitigations;
    public static bool? ProcessMitigations;

    public static bool? LegacyContextMenu;
    public static bool? ShowMyTaskbarOnAllDisplays;
    public static bool? AlwaysShowTrayIcons;
    public static bool? TaskbarAlignment;

    public static bool? AppleMusic;
    public static bool? WOL;
    public static int? CoreCount;
    public static bool? RSS;
    public static bool? TxIntDelay;

    public static bool? HID;
    public static bool? IMOD;

    public static bool? Intel10th;
    public static bool? Intel11th;
    public static bool? NVIDIA;
    public static bool? AMD;
    public static bool? HDCP;
    public static bool? MSI;
    public static bool? CRU;

    public static bool? Chrome;
    public static bool? Brave;
    public static bool? Firefox;
    public static bool? Zen;
    public static bool? Arc;

    public static bool? uBlock;
    public static bool? SponsorBlock;
    public static bool? ReturnYouTubeDislike;
    public static bool? Cookies;
    public static bool? DarkReader;
    public static bool? Violentmonkey;
    public static bool? Tampermonkey;
    public static bool? Shazam;
    public static bool? iCloud;
    public static bool? Bitwarden;
    public static bool? OnePassword;

    public static bool? Spotify;
    public static bool? AmazonMusic;
    public static bool? DeezerMusic;

    public static bool? WhatsApp;
    public static bool? Discord;
    public static bool? EpicGames;
    public static bool? EpicGamesAccount;
    public static bool? EpicGamesGames;
    public static bool? Steam;

    public static bool? Scheduling;
    public static bool? Hyperthreading;
    public static bool? Reserve;
    public static bool? TimerResolution;

    public static async Task Run()
    {
        InstallPage.Status.Text = "Preparing...";
        InstallPage.Info.Title = "Please wait...";

        InstallPage.Info.Severity = InfoBarSeverity.Warning;
        InstallPage.Progress.Foreground = (Brush)Application.Current.Resources["SystemFillColorCautionBrush"];
        InstallPage.ProgressRingControl.Foreground = (Brush)Application.Current.Resources["SystemFillColorCautionBrush"];

        await Task.Run(() =>
        {
            string cpuVendor = (string)Registry.GetValue(@"HKEY_LOCAL_MACHINE\HARDWARE\DESCRIPTION\System\CentralProcessor\0", "VendorIdentifier", null);

            if (cpuVendor.Contains("GenuineIntel"))
            {
                INTELCPU = true;
            }
            else if (cpuVendor.Contains("AuthenticAMD"))
            {
                AMDCPU = true;
            }

            var output = Process.Start(new ProcessStartInfo
            {
                FileName = "powershell",
                Arguments = "-Command \"(Get-PhysicalDisk -SerialNumber (Get-Disk -Number (Get-Partition -DriveLetter $env:SystemDrive.Substring(0, 1)).DiskNumber).SerialNumber.TrimStart()).MediaType\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }).StandardOutput.ReadToEnd();

            if (output.Contains("SSD"))
            {
                SSD = true;
            }

            using (var key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\AutoOS"))
            {
                IdleStates = key?.GetValue("IdleStates")?.ToString() == "1";
                PowerService = key?.GetValue("PowerService")?.ToString() == "1";
                Wifi = key?.GetValue("WiFi")?.ToString() == "1";
                Bluetooth = key?.GetValue("Bluetooth")?.ToString() == "1";
                WindowsDefender = key?.GetValue("WindowsDefender")?.ToString() == "1";
                UserAccountControl = key?.GetValue("UserAccountControl")?.ToString() == "1";
                DEP = key?.GetValue("DataExecutionPrevention")?.ToString() == "1";
                SpectreMeltdownMitigations = key?.GetValue("SpectreMeltdownMitigations")?.ToString() == "1";
                ProcessMitigations = key?.GetValue("ProcessMitigations")?.ToString() == "1";
                LegacyContextMenu = key?.GetValue("LegacyContextMenu")?.ToString() == "1";
                ShowMyTaskbarOnAllDisplays = key?.GetValue("ShowMyTaskbarOnAllDisplays")?.ToString() == "1";
                AlwaysShowTrayIcons = key?.GetValue("AlwaysShowTrayIcons")?.ToString() == "1";
                TaskbarAlignment = key?.GetValue("TaskbarAlignment")?.ToString() == "Left";
                AppleMusic = key?.GetValue("Music")?.ToString() == "Apple Music";
                WOL = key?.GetValue("WakeOnLan")?.ToString() == "1";
                HID = key?.GetValue("HumanInterfaceDevices")?.ToString() == "1";
                IMOD = key?.GetValue("XhciInterruptModeration")?.ToString() == "1";
                Intel10th = key?.GetValue("GpuBrand")?.ToString() == "Intel® 7th-10th Gen Processor Graphics";
                Intel11th = key?.GetValue("GpuBrand")?.ToString() == "Intel® Arc™ & Iris® Xe Graphics";
                NVIDIA = key?.GetValue("GpuBrand")?.ToString() == "NVIDIA";
                AMD = key?.GetValue("GpuBrand")?.ToString() == "AMD";
                HDCP = key?.GetValue("HighDefinitionContentProtection")?.ToString() == "1";
                MSI = key?.GetValue("MsiProfile") != null;
                CRU = key?.GetValue("CruProfile") != null;
                Chrome = key?.GetValue("Browser")?.ToString() == "Chrome";
                Brave = key?.GetValue("Browser")?.ToString() == "Brave";
                Firefox = key?.GetValue("Browser")?.ToString() == "Firefox";
                Zen = key?.GetValue("Browser")?.ToString() == "Zen";
                Arc = key?.GetValue("Browser")?.ToString() == "Arc";
                uBlock = key?.GetValue("Extensions")?.ToString()?.Contains("uBlock Origin");
                SponsorBlock = key?.GetValue("Extensions")?.ToString()?.Contains("SponsorBlock");
                ReturnYouTubeDislike = key?.GetValue("Extensions")?.ToString()?.Contains("Return YouTube Dislike");
                Cookies = key?.GetValue("Extensions")?.ToString()?.Contains("I still don't care about cookies");
                DarkReader = key?.GetValue("Extensions")?.ToString()?.Contains("Dark Reader");
                Violentmonkey = key?.GetValue("Extensions")?.ToString()?.Contains("Violentmonkey");
                Tampermonkey = key?.GetValue("Extensions")?.ToString()?.Contains("Tampermonkey");
                Shazam = key?.GetValue("Extensions")?.ToString()?.Contains("Shazam");
                iCloud = key?.GetValue("Extensions")?.ToString()?.Contains("iCloud Passwords");
                Bitwarden = key?.GetValue("Extensions")?.ToString()?.Contains("Bitwarden");
                OnePassword = key?.GetValue("Extensions")?.ToString()?.Contains("1Password");
                Spotify = key?.GetValue("Music")?.ToString() == "Spotify";
                AmazonMusic = key?.GetValue("Music")?.ToString() == "Amazon Music";
                DeezerMusic = key?.GetValue("Music")?.ToString() == "Deezer Music";
                WhatsApp = key?.GetValue("Messaging")?.ToString().Contains("WhatsApp");
                Discord = key?.GetValue("Messaging")?.ToString().Contains("Discord");
                EpicGames = key?.GetValue("Launchers")?.ToString().Contains("Epic Games");
                Steam = key?.GetValue("Launchers")?.ToString().Contains("Steam");
                Scheduling = key?.GetValue("Affinities")?.ToString() == "Automatic";
                TimerResolution = key?.GetValue("TimerResolution")?.ToString() == "Automatic";
            }

            EpicGamesAccount = DriveInfo.GetDrives()
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
                .Any(file =>
                {
                    string configContent = File.ReadAllText(file.FullName);
                    Match dataMatch = Regex.Match(configContent, @"Data=([^\r\n]+)");

                    return dataMatch.Success && dataMatch.Groups[1].Value.Length >= 1000;
                });

            EpicGamesGames = DriveInfo.GetDrives()
                .Where(d => d.DriveType == DriveType.Fixed && d.Name != @"C:\")
                .Select(d => Path.Combine(d.Name, "ProgramData", "Epic", "UnrealEngineLauncher", "LauncherInstalled.dat"))
                .Where(File.Exists)
                .Select(path => new FileInfo(path))
                .OrderByDescending(f => f.LastWriteTime)
                .Select(async file =>
                {
                    string jsonContent = await File.ReadAllTextAsync(file.FullName);
                    var jsonObject = JsonNode.Parse(jsonContent);
                    var installationList = jsonObject?["InstallationList"] as JsonArray;
                    return installationList != null && installationList.Count > 0;
                })
                .Select(t => t.Result)
                .FirstOrDefault(false);

            Rename = "System Product Name".Equals(Registry.GetValue(@"HKEY_LOCAL_MACHINE\HARDWARE\DESCRIPTION\System\BIOS", "SystemProductName", "")?.ToString(), StringComparison.Ordinal);

            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_SystemEnclosure");
            foreach (ManagementObject obj in searcher.Get())
            {
                ushort[] chassisTypes = (ushort[])obj["ChassisTypes"];
                Desktop = chassisTypes != null && chassisTypes.Any(type => new ushort[] { 3, 4, 5, 6, 7, 15, 16, 17 }.Contains(type));
            }

            ManagementObjectSearcher searcher2 = new ManagementObjectSearcher("SELECT * FROM Win32_Processor");
            ManagementObjectCollection queryCollection = searcher2.Get();

            foreach (ManagementObject m in queryCollection)
            {
                CoreCount = Convert.ToInt32(m["NumberOfCores"]);
            }

            RSS = CoreCount >= 4;

            var searcher3 = new ManagementObjectSearcher("SELECT * FROM Win32_NetworkAdapter");
            foreach (var obj in searcher3.Get())
            {
                var pnpDeviceId = obj["PNPDeviceID"]?.ToString();
                if (!string.IsNullOrEmpty(pnpDeviceId) && pnpDeviceId.StartsWith("PCI\\VEN_"))
                {
                    string regPath = $@"SYSTEM\CurrentControlSet\Enum\{pnpDeviceId}";
                    using var regKey = Registry.LocalMachine.OpenSubKey(regPath);
                    var driver = regKey?.GetValue("Driver")?.ToString();
                    string classKeyPath = $@"SYSTEM\CurrentControlSet\Control\Class\{driver}";
                    using var classKey = Registry.LocalMachine.OpenSubKey(classKeyPath);
                    var physicalMediaType = classKey?.GetValue("*PhysicalMediaType")?.ToString();
                    if (physicalMediaType == "14" && classKey.GetValue("TxIntDelay") != null)
                    {
                        TxIntDelay = true;
                        break;
                    }

                }
            }

            Hyperthreading = new ManagementObjectSearcher("SELECT NumberOfCores, NumberOfLogicalProcessors FROM Win32_Processor")
               .Get()
               .Cast<ManagementObject>()
               .Any(obj => Convert.ToInt32(obj["NumberOfLogicalProcessors"]) > Convert.ToInt32(obj["NumberOfCores"]));

            Reserve = Environment.ProcessorCount >= 6;
        });

        InstallPage.Info.Severity = InfoBarSeverity.Informational;
        InstallPage.Progress.Foreground = ProcessActions.GetColor("LightNormal", "DarkNormal");
        InstallPage.ProgressRingControl.Foreground = null;
    }
}
