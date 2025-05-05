using Newtonsoft.Json.Linq;
using System.Management;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace AutoOS.Helpers
{
    public static class NvidiaHelper
    {
        private static readonly HttpClient httpClient = new HttpClient();

        public static async Task<(string currentVersion, string newestVersion, string newestDownloadUrl)> CheckUpdate()
        {
            bool isNotebook = false;
            string gpuId = "1066";

            string currentVersion = string.Empty;
            string newestVersion = string.Empty;
            string newestDownloadUrl = string.Empty;

            // check if notebook
            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_SystemEnclosure");

            foreach (ManagementObject obj in searcher.Get())
            {
                ushort[] chassisTypes = (ushort[])obj["ChassisTypes"];
                isNotebook = chassisTypes != null && chassisTypes.Any(type => new ushort[] { 1, 8, 9, 10, 11, 12, 14, 18, 21, 31, 32 }.Contains(type));
            }

            // get all gpus
            foreach (ManagementBaseObject gpu in new ManagementObjectSearcher("SELECT Name, DriverVersion, PNPDeviceID FROM Win32_VideoController").Get())
            {
                string rawName = gpu["Name"].ToString();
                string rawVersion = gpu["DriverVersion"].ToString().Replace(".", string.Empty);
                string pnp = gpu["PNPDeviceID"].ToString();

                // if real
                if (pnp.Contains("&DEV_"))
                {
                    string[] split = pnp.Split("&DEV_");

                    Regex nameRegex = new(@"(?<=NVIDIA )(.*(?= \([A-Z]+\))|.*(?= [0-9]+GB)|.*(?= with Max-Q Design)|.*(?= COLLECTORS EDITION)|.*)");

                    if (Regex.IsMatch(rawName, @"^NVIDIA") && nameRegex.IsMatch(rawName))
                    {
                        string gpuName = nameRegex.Match(rawName).Value.Trim().Replace("Super", "SUPER");
                        currentVersion = rawVersion.Substring(rawVersion.Length - 5, 5).Insert(3, ".");

                        string json = await httpClient.GetStringAsync("https://raw.githubusercontent.com/ZenitH-AT/nvidia-data/main/gpu-data.json");

                        var doc = JsonDocument.Parse(json);

                        if (doc.RootElement.TryGetProperty(isNotebook ? "notebook" : "desktop", out JsonElement sectionElement))
                        {
                            if (sectionElement.TryGetProperty(gpuName, out JsonElement idElement))
                            {
                                gpuId = idElement.GetString();
                            }
                        }
                    }
                }
            }

            string response = await httpClient.GetStringAsync($"https://gfwsl.geforce.com/services_toolkit/services/com/nvidia/services/AjaxDriverService.php?func=DriverManualLookup&pfid={gpuId}&osID=135&dch=1&upCRD=0");
            JObject driverObj = JObject.Parse(response);

            if ((int)driverObj["Success"] == 1)
            {
                newestVersion = driverObj["IDS"][0]["downloadInfo"]["Version"].ToString();
                newestDownloadUrl = driverObj["IDS"][0]["downloadInfo"]["DownloadURL"].ToString();
            }

            return (currentVersion, newestVersion, newestDownloadUrl);
        }
    }
}