using AutoOS.Core.Helpers.Download;
using AutoOS.Core.Helpers.Games;
using AutoOS.Core.Helpers.Monitor;
using AutoOS.Core.Helpers.Registry;
using Microsoft.Win32;
using System.Diagnostics;
using System.Text.Json;

namespace AutoOS.Views.Installer.Stages;

public static partial class GamesStage
{
    public static List<(string Title, Func<Task> Action, Func<bool> Condition)> GetActions()
    {
        bool Fortnite = ApplicationStage.Fortnite;
        bool Valorant = ApplicationStage.Valorant;

        string fortnitePath = string.Empty;
		string valorantPath = string.Empty;

        string fortniteIniPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FortniteGame", "Saved", "Config", "WindowsClient");
        string valorantIniPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "VALORANT", "Saved", "Config", "WindowsClient");

		int maxRefreshRate = (int)MonitorHelper.GetMonitors().Max(max => max.RefreshRate);

        var actions = new List<(string Title, Func<Task> Action, Func<bool> Condition)>
        {
            // download gameusersettings.ini for fortnite
            ("Downloading GameUserSettings.ini for Fortnite", async () => await DownloadHelper.Download("https://www.dl.dropboxusercontent.com/scl/fi/x7ymbpu9hf6myle0an2ef/GameUserSettings.ini?rlkey=i9v5oc1nccx7k58g12dd1k33j&st=hwo4v2du&dl=0", fortniteIniPath, "GameUserSettings.ini"), () => Fortnite == true),
            
            // cap frame rate for fortnite
            ($"Capping Frame Rate for Fortnite to {maxRefreshRate}fps", async () => new InIHelper(Path.Combine(fortniteIniPath, "GameUserSettings.ini")).AddValue("FrameRateLimit", $"{maxRefreshRate}.000000", "/Script/FortniteGame.FortGameUserSettings"), () => Fortnite == true),
            ($"Capping Frame Rate for Fortnite to {maxRefreshRate}fps", async () => await Task.Delay(1000), () => Fortnite == true),

            // disable fullscreen optimizations for fortnite
            ("Disabling fullscreen optimizations for Fortnite", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Layers", $@"{fortnitePath}\FortniteGame\Binaries\Win64\FortniteClient-Win64-Shipping.exe", "~ DISABLEDXMAXIMIZEDWINDOWEDMODE", RegistryValueKind.String), () => Fortnite == true),

            // set gpu preference to high performance for fortnite
            (@"Setting ""GPU Preference"" to ""High Performance"" for Fortnite", async () => fortnitePath = JsonDocument.Parse(File.ReadAllText(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Epic", "UnrealEngineLauncher", "LauncherInstalled.dat"))).RootElement.GetProperty("InstallationList").EnumerateArray().FirstOrDefault(e => e.GetProperty("AppName").GetString() == "Fortnite").GetProperty("InstallLocation").GetString(), () => Fortnite == true),
            (@"Setting ""GPU Preference"" to ""High Performance"" for Fortnite", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Microsoft\DirectX\UserGpuPreferences", fortnitePath + @"\FortniteGame\Binaries\Win64\FortniteClient-Win64-Shipping.exe", "SwapEffectUpgradeEnable=1;GpuPreference=2;", RegistryValueKind.String), () => Fortnite == true),
            (@"Setting ""GPU Preference"" to ""High Performance"" for Fortnite", async () => await Task.Delay(1000), () => Fortnite == true),

			// install easyanticheat
            ("Installing EasyAntiCheat", async () => await Process.Start(new ProcessStartInfo($@"{fortnitePath}\FortniteGame\Binaries\Win64\EasyAntiCheat\EasyAntiCheat_EOS_Setup.exe", "install 4fe75bbc5a674f4f9b356b5c90567da5") {  WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), () => Fortnite == true),
			("Installing EasyAntiCheat", async () => await Task.Delay(1000), () => Fortnite == true),

            // download gameusersettings.ini for valorant
            ("Downloading GameUserSettings.ini for Valorant", async () => await DownloadHelper.Download("https://www.dl.dropboxusercontent.com/scl/fi/v8t7zr92smdwp6c0u43li/GameUserSettings.ini?rlkey=9utj5hcekf4ddvpvvxbzzhbua&st=y7q4r3hm&dl=0", valorantIniPath, "GameUserSettings.ini"), () => Valorant == true),
            
            //// cap frame rate for valorant
            //($"Capping Frame Rate for Valorant to {maxRefreshRate}fps", async () => new InIHelper(Path.Combine(valorantIniPath, "GameUserSettings.ini")).AddValue("FrameRateLimit", $"{maxRefreshRate}.000000", "/Script/ShooterGame.ShooterGameUserSettings"), () => Valorant == true),
            //($"Capping Frame Rate for Valorant to {maxRefreshRate}fps", async () => await Task.Delay(1000), () => Valorant == true),

            // set "gpu preference" to "high performance" for valorant
            (@"Setting ""GPU Preference"" to ""High Performance"" for Valorant", async () => valorantPath = RiotHelper.ProductInstallFullPathRegex().Match(File.ReadAllText(RiotHelper.RiotGamesMetadataPath + @"\valorant.live\valorant.live.product_settings.yaml")).Groups[1].Value.Replace('/', '\\'), () => Valorant == true),
			(@"Setting ""GPU Preference"" to ""High Performance"" for Valorant", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Microsoft\DirectX\UserGpuPreferences", $@"{valorantPath}\ShooterGame\Binaries\Win64\VALORANT-Win64-Shipping.exe", "SwapEffectUpgradeEnable=1;GpuPreference=2;", RegistryValueKind.String), () => Valorant == true),
            (@"Setting ""GPU Preference"" to ""High Performance"" for Valorant", async () => await Task.Delay(1000), () => Valorant == true),
            
            // disable fullscreen optimizations for valorant
            ("Disabling fullscreen optimizations for Valorant", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Layers", $@"{valorantPath}\ShooterGame\Binaries\Win64\VALORANT-Win64-Shipping.exe", "~ DISABLEDXMAXIMIZEDWINDOWEDMODE", RegistryValueKind.String), () => Valorant == true),
        };

        return actions;
    }
}

