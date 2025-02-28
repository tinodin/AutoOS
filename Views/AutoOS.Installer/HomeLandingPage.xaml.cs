using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace AutoOS.Views.Installer
{
    public sealed partial class HomeLandingPage : Page
    {
        public HomeLandingPage()
        {
            InitializeComponent();
            Test();
        }

        private async void Test()
        {
            var foundFiles = DriveInfo.GetDrives()
                .Where(d => d.DriveType == DriveType.Fixed && d.Name != @"C:\")
                .Select(d => Path.Combine(d.Name, "ProgramData", "Epic", "UnrealEngineLauncher", "LauncherInstalled.dat"))
                .Where(File.Exists)
                .Select(path => new FileInfo(path))
                .OrderByDescending(f => f.LastWriteTime)
                .ToList();

            if (foundFiles.Count == 0)
            {
                Debug.WriteLine("No LauncherInstalled.dat found.");
                return;
            }

            // Determine if InstallationList is valid by checking if it's not empty
            var jsonContent = await File.ReadAllTextAsync(foundFiles.First().FullName);
            var jsonObject = JsonNode.Parse(jsonContent);
            var installationList = jsonObject?["InstallationList"] as JsonArray;

            if (installationList == null || installationList.Count == 0)
            {
                Debug.WriteLine("InstallationList is empty. Nothing to import.");
                return;
            }

            // Now, determine the latest one and copy it over to C: at the same location
            FileInfo newestFile = foundFiles.First();
            Debug.WriteLine($"Using newest LauncherInstalled.dat: {newestFile.FullName}");

            string destinationPath = @"C:\ProgramData\Epic\UnrealEngineLauncher\LauncherInstalled.dat";
            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);

            // Update the InstallLocation for each game in the InstallationList
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
                            Debug.WriteLine($"Updated InstallLocation: {originalPath} → {testPath}");
                            break;
                        }
                    }
                }
            }

            // After processing all games, write the updated jsonObject back to the file
            await File.WriteAllTextAsync(destinationPath, jsonObject.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
            Debug.WriteLine($"Copied updated LauncherInstalled.dat to {destinationPath}");

            // Get the drive from the "newestFile" to ensure we're using the same drive for Manifests
            string sourceManifestsFolder = Path.Combine(Path.GetPathRoot(newestFile.FullName)!, "ProgramData", "Epic", "EpicGamesLauncher", "Data", "Manifests");
            string destinationManifestsFolder = @"C:\ProgramData\Epic\EpicGamesLauncher\Data\Manifests";

            if (Directory.Exists(sourceManifestsFolder))
            {
                // Copy the entire folder and its contents to the destination directory
                Directory.CreateDirectory(destinationManifestsFolder);

                // Copy all files from the source to destination, including subdirectories
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

                Debug.WriteLine($"Copied entire {sourceManifestsFolder} folder to {destinationManifestsFolder}");

                // Process all .item files recursively in the destination folder
                foreach (var file in Directory.GetFiles(destinationManifestsFolder, "*.item", SearchOption.AllDirectories))
                {
                    string fileName = Path.GetFileName(file);
                    string destFilePath = file; // No need to copy the file again
                    Debug.WriteLine($"Processing {fileName} in {destinationManifestsFolder}");

                    string itemContent = await File.ReadAllTextAsync(destFilePath);
                    var itemJson = JsonNode.Parse(itemContent);

                    if (itemJson is JsonObject itemObj)
                    {
                        // Only check InstallLocation for new drive, and if found, update all 3 fields
                        if (itemObj.ContainsKey("InstallLocation"))
                        {
                            string originalInstallLocation = itemObj["InstallLocation"].ToString();
                            string originalDrive = Path.GetPathRoot(originalInstallLocation) ?? "";
                            string relativePath = originalInstallLocation.Substring(originalDrive.Length);

                            // Search for this path on other drives
                            foreach (var drive in DriveInfo.GetDrives().Where(d => d.DriveType == DriveType.Fixed && d.Name != @"C:\"))
                            {
                                string testPath = Path.Combine(drive.Name, relativePath);
                                if (Directory.Exists(testPath))
                                {
                                    // Extract the new drive letter and replace it only for all three paths
                                    string newDrive = Path.GetPathRoot(testPath);

                                    // Replace only the drive letter, keep the rest of the path intact
                                    itemObj["InstallLocation"] = newDrive + relativePath;
                                    itemObj["ManifestLocation"] = itemObj["ManifestLocation"].ToString().Replace(originalDrive, newDrive);
                                    itemObj["StagingLocation"] = itemObj["StagingLocation"].ToString().Replace(originalDrive, newDrive);

                                    Debug.WriteLine($"Updated InstallLocation, ManifestLocation, and StagingLocation to {newDrive + relativePath}");
                                    break;
                                }
                            }
                        }

                        // Write the updated item file back
                        await File.WriteAllTextAsync(destFilePath, itemObj.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
                        Debug.WriteLine($"Updated {fileName} with new paths.");
                    }
                }
            }
            else
            {
                Debug.WriteLine("Manifests folder not found.");
            }
        }
    }
}
