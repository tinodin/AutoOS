using System.Text.RegularExpressions;
using AutoOS.Core.Common;

namespace AutoOS.Core.Helpers.Games;

public static partial class RiotHelper
{
	[GeneratedRegex(@"riot-login[\s\S]*?name:\s""ssid""[\s\S]*?value:\s""([^""]+)""")]
	public static partial Regex SsidRegex();

	[GeneratedRegex(@"product_install_full_path:\s*""([^""]+)""")]
	public static partial Regex ProductInstallFullPathRegex();

	public static readonly string RiotGamesDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\Riot Games\Riot Client\Data";
	public static readonly string RiotGamesConfigPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\Riot Games\Riot Client\Config";
	public static readonly string RiotGamesMetadataPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + @"\Riot Games\Metadata";

	public static async Task ImportAccount(IStatusReporter? reporter = null)
	{
		string? systemDrive = Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.System));
		var foundFolders = DriveInfo.GetDrives()
			.Where(d => d.DriveType == DriveType.Fixed && d.Name != systemDrive)
			.SelectMany(d =>
			{
				string usersPath = Path.Combine(d.Name, "Users");
				if (!Directory.Exists(usersPath)) return [];

				return Directory.GetDirectories(usersPath)
					.Select(userDir => Path.Combine(userDir, "AppData", "Local", "Riot Games", "Riot Client"))
					.Where(Directory.Exists);
			})
			.Select(path => new DirectoryInfo(path))
			.OrderByDescending(f => f.LastWriteTime)
			.ToList();

		foreach (DirectoryInfo? folder in foundFolders)
		{
			string yamlPath = Path.Combine(folder.FullName, "Data", "RiotGamesPrivateSettings.yaml");
			if (!File.Exists(yamlPath))
				continue;

			string fileContent = File.ReadAllText(yamlPath);
			Match ssidMatch = SsidRegex().Match(fileContent);

			if (!ssidMatch.Success || string.IsNullOrWhiteSpace(ssidMatch.Groups[1].Value))
				continue;

			// copy data folder
			string dataSource = Path.Combine(folder.FullName, "Data");
			if (Directory.Exists(dataSource))
			{
				Directory.CreateDirectory(RiotGamesDataPath);

				foreach (string directory in Directory.GetDirectories(dataSource, "*", SearchOption.AllDirectories))
				{
					string subDirPath = directory.Replace(dataSource, RiotGamesDataPath);
					Directory.CreateDirectory(subDirPath);
				}

				foreach (string file in Directory.GetFiles(dataSource, "*.*", SearchOption.AllDirectories))
				{
					string destFilePath = file.Replace(dataSource, RiotGamesDataPath);
					File.Copy(file, destFilePath, true);
				}
			}

			// copy config folder
			string configSource = Path.Combine(folder.FullName, "Config");
			if (Directory.Exists(configSource))
			{
				Directory.CreateDirectory(RiotGamesConfigPath);

				foreach (string directory in Directory.GetDirectories(configSource, "*", SearchOption.AllDirectories))
				{
					string subDirPath = directory.Replace(configSource, RiotGamesConfigPath);
					Directory.CreateDirectory(subDirPath);
				}

				foreach (string file in Directory.GetFiles(configSource, "*.*", SearchOption.AllDirectories))
				{
					string destFilePath = file.Replace(configSource, RiotGamesConfigPath);
					File.Copy(file, destFilePath, true);
				}
			}

			reporter?.SetTitle("Successfully imported Riot Games account...");

			await Task.Delay(1000);

			return;
		}
	}

	public static async Task ImportGames(IStatusReporter? reporter = null)
	{
		// get all metadata folders from other drives
		string? systemDrive = Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.System));
		var foundFolders = DriveInfo.GetDrives()
			.Where(d => d.DriveType == DriveType.Fixed && d.Name != systemDrive)
			.Select(d => Path.Combine(d.Name, "ProgramData", "Riot Games", "Metadata"))
			.Where(Directory.Exists)
			.Select(path => new DirectoryInfo(path))
			.OrderByDescending(d => d.LastWriteTime)
			.ToList();

		if (foundFolders.Count == 0)
			return;

		DirectoryInfo newestFolder = foundFolders.First();

		// create destination directory
		Directory.CreateDirectory(RiotGamesMetadataPath);

		// copy the whole folder
		foreach (string directory in Directory.GetDirectories(newestFolder.FullName, "*", SearchOption.AllDirectories))
		{
			string subDirPath = directory.Replace(newestFolder.FullName, RiotGamesMetadataPath);
			Directory.CreateDirectory(subDirPath);
		}

		foreach (string file in Directory.GetFiles(newestFolder.FullName, "*.*", SearchOption.AllDirectories))
		{
			string destFilePath = file.Replace(newestFolder.FullName, RiotGamesMetadataPath);
			File.Copy(file, destFilePath, true);
		}

		// process each subfolder to update paths
		foreach (string subFolder in Directory.GetDirectories(RiotGamesMetadataPath))
		{
			string folderName = new DirectoryInfo(subFolder).Name;
			string settingsFile = Path.Combine(subFolder, $"{folderName}.product_settings.yaml");

			if (!File.Exists(settingsFile))
				continue;

			string fileContent = await File.ReadAllTextAsync(settingsFile);
			Match pathMatch = ProductInstallFullPathRegex().Match(fileContent);

			if (!pathMatch.Success || string.IsNullOrWhiteSpace(pathMatch.Groups[1].Value))
				continue;

			string originalPath = pathMatch.Groups[1].Value;
			string originalDrive = Path.GetPathRoot(originalPath) ?? "";
			string relativePath = originalPath[originalDrive.Length..];

			string? newPath = null;

			// check other drives for the path
			foreach (DriveInfo? drive in DriveInfo.GetDrives().Where(d => d.DriveType == DriveType.Fixed && d.Name != systemDrive))
			{
				string testPath = Path.Combine(drive.Name, relativePath);
				if (Directory.Exists(testPath))
				{
					newPath = testPath.Replace('\\', '/');
					break;
				}
			}

			if (newPath != null)
			{
				// update the path in the file
				fileContent = ProductInstallFullPathRegex().Replace(fileContent, $"product_install_full_path: \"{newPath}\"");
				await File.WriteAllTextAsync(settingsFile, fileContent);
			}
		}

		reporter?.SetTitle("Successfully imported Riot Client Games...");

		await Task.Delay(1000);
	}
}
