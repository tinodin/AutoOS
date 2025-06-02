using Microsoft.UI.Xaml.Media;
using Microsoft.Win32;
using System.Diagnostics;
using System.Management;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Windows.Storage;

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

    public static string LightTime;
    public static string DarkTime;

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

    private static readonly ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

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

            IdleStates = (localSettings.Values["IdleStates"]?.ToString() == "1");
            PowerService = (localSettings.Values["PowerService"]?.ToString() == "1");
            Wifi = (localSettings.Values["WiFi"]?.ToString() == "1");
            Bluetooth = (localSettings.Values["Bluetooth"]?.ToString() == "1");
            WindowsDefender = (localSettings.Values["WindowsDefender"]?.ToString() == "1");
            UserAccountControl = (localSettings.Values["UserAccountControl"]?.ToString() == "1");
            DEP = (localSettings.Values["DataExecutionPrevention"]?.ToString() == "1");
            SpectreMeltdownMitigations = (localSettings.Values["SpectreMeltdownMitigations"]?.ToString() == "1");
            ProcessMitigations = (localSettings.Values["ProcessMitigations"]?.ToString() == "1");
            LegacyContextMenu = (localSettings.Values["LegacyContextMenu"]?.ToString() == "1");
            ShowMyTaskbarOnAllDisplays = (localSettings.Values["ShowMyTaskbarOnAllDisplays"]?.ToString() == "1");
            AlwaysShowTrayIcons = (localSettings.Values["AlwaysShowTrayIcons"]?.ToString() == "1");
            TaskbarAlignment = (localSettings.Values["TaskbarAlignment"]?.ToString() == "Left");
            WOL = (localSettings.Values["WakeOnLan"]?.ToString() == "1");
            HID = (localSettings.Values["HumanInterfaceDevices"]?.ToString() == "1");
            IMOD = (localSettings.Values["XhciInterruptModeration"]?.ToString() == "1");

            Intel10th = (localSettings.Values["GpuBrand"]?.ToString().Contains("Intel® 7th-10th Gen Processor Graphics") ?? false);
            Intel11th = (localSettings.Values["GpuBrand"]?.ToString().Contains("Intel® Arc™ & Iris® Xe Graphics") ?? false);
            NVIDIA = (localSettings.Values["GpuBrand"]?.ToString().Contains("NVIDIA") ?? false);
            AMD = (localSettings.Values["GpuBrand"]?.ToString().Contains("AMD") ?? false);

            HDCP = (localSettings.Values["HighDefinitionContentProtection"]?.ToString() == "1");
            MSI = (localSettings.Values["MsiProfile"] != null);
            CRU = (localSettings.Values["CruProfile"] != null);

            Chrome = (localSettings.Values["Browser"]?.ToString().Contains("Chrome") ?? false);
            Brave = (localSettings.Values["Browser"]?.ToString().Contains("Brave") ?? false);
            Firefox = (localSettings.Values["Browser"]?.ToString().Contains("Firefox") ?? false);
            Zen = (localSettings.Values["Browser"]?.ToString().Contains("Zen") ?? false);
            Arc = (localSettings.Values["Browser"]?.ToString().Contains("Arc") ?? false);

            uBlock = (localSettings.Values["Extensions"]?.ToString().Contains("uBlock Origin") ?? false);
            SponsorBlock = (localSettings.Values["Extensions"]?.ToString().Contains("SponsorBlock") ?? false);
            ReturnYouTubeDislike = (localSettings.Values["Extensions"]?.ToString().Contains("Return YouTube Dislike") ?? false);
            Cookies = (localSettings.Values["Extensions"]?.ToString().Contains("I still don't care about cookies") ?? false);
            DarkReader = (localSettings.Values["Extensions"]?.ToString().Contains("Dark Reader") ?? false);
            Violentmonkey = (localSettings.Values["Extensions"]?.ToString().Contains("Violentmonkey") ?? false);
            Tampermonkey = (localSettings.Values["Extensions"]?.ToString().Contains("Tampermonkey") ?? false);
            Shazam = (localSettings.Values["Extensions"]?.ToString().Contains("Shazam") ?? false);
            iCloud = (localSettings.Values["Extensions"]?.ToString().Contains("iCloud Passwords") ?? false);
            Bitwarden = (localSettings.Values["Extensions"]?.ToString().Contains("Bitwarden") ?? false);
            OnePassword = (localSettings.Values["Extensions"]?.ToString().Contains("1Password") ?? false);

            LightTime = localSettings.Values["LightTime"]?.ToString();
            DarkTime = localSettings.Values["DarkTime"]?.ToString();

            Spotify = (localSettings.Values["Music"]?.ToString().Contains("Spotify") ?? false);
            AppleMusic = (localSettings.Values["Music"]?.ToString().Contains("Apple Music") ?? false);
            AmazonMusic = (localSettings.Values["Music"]?.ToString().Contains("Amazon Music") ?? false);
            DeezerMusic = (localSettings.Values["Music"]?.ToString().Contains("Deezer Music") ?? false);

            WhatsApp = (localSettings.Values["Messaging"]?.ToString().Contains("WhatsApp") ?? false);
            Discord = (localSettings.Values["Messaging"]?.ToString().Contains("Discord") ?? false);

            EpicGames = (localSettings.Values["Launchers"]?.ToString().Contains("Epic Games") ?? false);
            Steam = (localSettings.Values["Launchers"]?.ToString().Contains("Steam") ?? false);

            Scheduling = (localSettings.Values["Affinities"]?.ToString() == "Automatic");

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

            Desktop = new ManagementObjectSearcher("SELECT * FROM Win32_SystemEnclosure")
                .Get()
                .Cast<ManagementObject>()
                .Any(obj => ((ushort[])obj["ChassisTypes"])?.Any(type => new ushort[] { 3, 4, 5, 6, 7, 15, 16, 17 }.Contains(type)) == true);

            foreach (ManagementObject m in new ManagementObjectSearcher("SELECT * FROM Win32_Processor").Get())
            {
                CoreCount += Convert.ToInt32(m["NumberOfCores"]);
            }
            
            RSS = CoreCount >= 4;

            foreach (var obj in new ManagementObjectSearcher("SELECT * FROM Win32_NetworkAdapter").Get())
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
        InstallPage.Progress.Foreground = (Brush)Application.Current.Resources["AccentForegroundBrush"];
        InstallPage.ProgressRingControl.Foreground = null;
    }
}