using System.Diagnostics;
using System.Text.Json;
using AutoOS.Core.Helpers.Download;
using AutoOS.Core.Helpers.Games;
using AutoOS.Core.Helpers.Monitor;

namespace AutoOS.App.Views.Installer.Stages;

public static partial class GamesStage
{
	public static List<(string Title, Func<Task> Action, Func<bool> Condition)> GetActions()
	{
		bool Fortnite = AppsStage.Fortnite;
		bool Valorant = AppsStage.Valorant;

		string fortnitePath = string.Empty;

		string fortniteIniPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FortniteGame", "Saved", "Config", "WindowsClient");
		string valorantIniPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "VALORANT", "Saved", "Config", "WindowsClient");

		int maxRefreshRate = (int)MonitorHelper.GetMonitors().Max(max => max.RefreshRate);

		var actions = new List<(string Title, Func<Task> Action, Func<bool> Condition)>
		{
			// download gameusersettings.ini for fortnite
			("Downloading GameUserSettings.ini for Fortnite", async () => await DownloadHelper.Download("https://raw.githubusercontent.com/tinodin/AutoOS-Resources/main/Files/Fortnite/GameUserSettings.ini", fortniteIniPath, "GameUserSettings.ini"), () => Fortnite == true),
			
			// cap frame rate for fortnite
			($"Capping Frame Rate for Fortnite to {maxRefreshRate}fps", async () => new InIHelper(Path.Combine(fortniteIniPath, "GameUserSettings.ini")).AddValue("FrameRateLimit", $"{maxRefreshRate}.000000", "/Script/FortniteGame.FortGameUserSettings"), () => Fortnite == true),
			($"Capping Frame Rate for Fortnite to {maxRefreshRate}fps", async () => Directory.CreateDirectory(Path.Combine(Path.GetPathRoot(Environment.SystemDirectory)!, "Users", "Default", "AppData", "Local", "FortniteGame", "Saved", "Config", "WindowsClient")), () => Fortnite == true),
			($"Capping Frame Rate for Fortnite to {maxRefreshRate}fps", async () => File.Copy(Path.Combine(fortniteIniPath, "GameUserSettings.ini"), Path.Combine(Path.GetPathRoot(Environment.SystemDirectory)!, "Users", "Default", "AppData", "Local", "FortniteGame", "Saved", "Config", "WindowsClient", "GameUserSettings.ini"), true), () => Fortnite == true),
			($"Capping Frame Rate for Fortnite to {maxRefreshRate}fps", async () => await Task.Delay(1000), () => Fortnite == true),

			// install easyanticheat
			("Installing EasyAntiCheat", async () => fortnitePath = JsonDocument.Parse(File.ReadAllText(Path.Combine(EpicGamesHelper.EpicGamesInstalledGamesPath))).RootElement.GetProperty("InstallationList").EnumerateArray().FirstOrDefault(entry => entry.GetProperty("AppName").GetString() == "Fortnite").GetProperty("InstallLocation").GetString(), () => Fortnite == true),
			("Installing EasyAntiCheat", async () => await Process.Start(new ProcessStartInfo($@"{fortnitePath}\FortniteGame\Binaries\Win64\EasyAntiCheat\EasyAntiCheat_EOS_Setup.exe", "install 4fe75bbc5a674f4f9b356b5c90567da5") {  WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), () => Fortnite == true),
			("Installing EasyAntiCheat", async () => await Task.Delay(1000), () => Fortnite == true),

			// download gameusersettings.ini for valorant
			("Downloading GameUserSettings.ini for Valorant", async () => await DownloadHelper.Download("https://raw.githubusercontent.com/tinodin/AutoOS-Resources/main/Files/Valorant/GameUserSettings.ini", valorantIniPath, "GameUserSettings.ini"), () => Valorant == true),
			("Downloading GameUserSettings.ini for Valorant", async () => Directory.CreateDirectory(Path.Combine(Path.GetPathRoot(Environment.SystemDirectory)!, "Users", "Default", "AppData", "Local", "VALORANT", "Saved", "Config", "WindowsClient")), () => Valorant == true),
			("Downloading GameUserSettings.ini for Valorant", async () => File.Copy(Path.Combine(valorantIniPath, "GameUserSettings.ini"), Path.Combine(Path.GetPathRoot(Environment.SystemDirectory)!, "Users", "Default", "AppData", "Local", "VALORANT", "Saved", "Config", "WindowsClient", "GameUserSettings.ini"), true), () => Valorant == true),
			
			//// cap frame rate for valorant
			//($"Capping Frame Rate for Valorant to {maxRefreshRate}fps", async () => new InIHelper(Path.Combine(valorantIniPath, "GameUserSettings.ini")).AddValue("FrameRateLimit", $"{maxRefreshRate}.000000", "/Script/ShooterGame.ShooterGameUserSettings"), () => Valorant == true),
			//($"Capping Frame Rate for Valorant to {maxRefreshRate}fps", async () => await Task.Delay(1000), () => Valorant == true)
		};

		return actions;
	}
}

