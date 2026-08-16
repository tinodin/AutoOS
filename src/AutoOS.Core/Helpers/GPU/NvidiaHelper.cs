using System.Diagnostics;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Security.Authentication;
using System.Text.Json;
using AutoOS.Core.Common;
using AutoOS.Core.Helpers.Download;
using AutoOS.Core.Helpers.Extract;
using AutoOS.Core.Data.Models.GPU;
using AutoOS.Core.Helpers.Processes;
using AutoOS.Core.Helpers.Registry;
using AutoOS.Core.Helpers.Store;
using DevWinUI;
using Microsoft.Win32;
using Windows.Win32;
using Windows.Win32.System.Power;

namespace AutoOS.Core.Helpers.GPU;

public static partial class NvidiaHelper
{
    private static readonly HttpClient httpClient = new(new SocketsHttpHandler
    {
        SslOptions = new SslClientAuthenticationOptions
        {
            EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13
        }
    })
    {
        DefaultRequestHeaders =
            {
                UserAgent =
                {
                    new ProductInfoHeaderValue("AutoOS", ProcessInfoHelper.Version)
                }
            }
    };

    public static async Task<(string newestVersion, string newestDownloadUrl)> CheckUpdate(GpuInfo gpu)
    {
        string deviceName = gpu.DeviceName;
        string? gpuId = string.Empty;
        string newestVersion = string.Empty;
        string newestDownloadUrl = string.Empty;
        bool isNotebook = GetIsNotebook();

        unsafe static bool GetIsNotebook()
        {
            SYSTEM_POWER_CAPABILITIES caps = default;
            if (PInvoke.GetPwrCapabilities(&caps))
            {
                return caps.LidPresent;
            }
            return false;
        }

        string json = await httpClient.GetStringAsync("https://raw.githubusercontent.com/ZenitH-AT/nvidia-data/main/gpu-data.json");
        using var gpuDoc = JsonDocument.Parse(json);

        if (gpuDoc.RootElement.TryGetProperty(isNotebook ? "notebook" : "desktop", out JsonElement section))
        {
            string deviceNameLower = deviceName.ToLower();
            string? bestMatchKey = null;
            double bestScore = -1;

            foreach (JsonProperty prop in section.EnumerateObject())
            {
                string keyLower = prop.Name.ToLower();
                var deviceWords = new HashSet<string>(deviceNameLower.Split(' ', '/', '-', '+'));
                var keyWords = new HashSet<string>(keyLower.Split(' ', '/', '-', '+'));

                int common = deviceWords.Intersect(keyWords).Count();
                double score = (double)common / keyWords.Count;

                if (score > bestScore)
                {
                    bestScore = score;
                    bestMatchKey = prop.Name;
                }
            }

            if (bestMatchKey != null)
            {
                gpuId = section.GetProperty(bestMatchKey).GetString();
            }
        }

        if (!string.IsNullOrEmpty(gpuId))
        {
            string response = await httpClient.GetStringAsync($"https://gfwsl.geforce.com/services_toolkit/services/com/nvidia/services/AjaxDriverService.php?func=DriverManualLookup&pfid={gpuId}&osID=135&dch=1&upCRD=0");

            using var respDoc = JsonDocument.Parse(response);
			JsonElement root = respDoc.RootElement;

            if (int.TryParse(root.GetProperty("Success").GetString(), out int success) && success == 1)
            {
				JsonElement info = root.GetProperty("IDS")[0].GetProperty("downloadInfo");
                newestVersion = info.GetProperty("Version").GetString() ?? string.Empty;
                newestDownloadUrl = info.GetProperty("DownloadURL").GetString() ?? string.Empty;
            }
            else
            {
                response = await httpClient.GetStringAsync($"https://gfwsl.geforce.com/services_toolkit/services/com/nvidia/services/AjaxDriverService.php?func=DriverManualLookup&pfid={gpuId}&osID=57&dch=1&upCRD=0");

                using var respDoc2 = JsonDocument.Parse(response);
				JsonElement root2 = respDoc2.RootElement;

                if (int.TryParse(root2.GetProperty("Success").GetString(), out int success2) && success2 == 1)
                {
					JsonElement info = root2.GetProperty("IDS")[0].GetProperty("downloadInfo");
                    newestVersion = info.GetProperty("Version").GetString() ?? string.Empty;
                    newestDownloadUrl = info.GetProperty("DownloadURL").GetString() ?? string.Empty;
                }
            }
        }

        return (newestVersion, newestDownloadUrl);
    }

    public static async Task StripDriver()
    {
        foreach (string directory in Directory.GetDirectories(Path.Combine(Path.GetTempPath(), "NVIDIA", "driver")))
        {
            string folderName = Path.GetFileName(directory);

            if (folderName != "Display.Driver" && folderName != "NVI2")
            {
                Directory.Delete(directory, true);
            }
        }

        string setupCfgPath = Path.Combine(Path.Combine(Path.GetTempPath(), "NVIDIA", "driver"), "setup.cfg");

        if (File.Exists(setupCfgPath))
        {
			string[] lines = await File.ReadAllLinesAsync(setupCfgPath);
            var newLines = lines.Where(line => !line.Contains("<file name=\"${{EulaHtmlFile}}\"/>") && !line.Contains("<file name=\"${{FunctionalConsentFile}}\"/>") && !line.Contains("<file name=\"${{PrivacyPolicyFile}}\"/>")).ToList();

            await File.WriteAllLinesAsync(setupCfgPath, newLines);
        }

        string presentationsCfgPath = Path.Combine(Path.Combine(Path.GetTempPath(), "NVIDIA", "driver"), "NVI2", "presentations.cfg");

        if (File.Exists(presentationsCfgPath))
        {
			string[] lines = await File.ReadAllLinesAsync(presentationsCfgPath);
            var newLines = lines.Select(line =>
            {
                if (line.Contains("<string name=\"ProgressPresentationUrl\""))
                {
                    return "        <string name=\"ProgressPresentationUrl\" value=\"\"/>";
                }
                else if (line.Contains("<string name=\"ProgressPresentationSelectedPackageUrl\""))
                {
                    return "        <string name=\"ProgressPresentationSelectedPackageUrl\" value=\"\"/>";
                }
                return line;
            }).ToList();

            await File.WriteAllLinesAsync(presentationsCfgPath, newLines);
        }
    }

    public static List<(string Title, Func<Task> Action, Func<bool>? Condition)> InstallActions(GpuInfo gpu, string newestVersion, string newestDownloadUrl, IStatusReporter? reporter = null)
    {
        var actions = new List<(string Title, Func<Task> Action, Func<bool>? Condition)>
            {
				// download nvidia driver
				($@"Downloading NVIDIA driver {newestVersion}", async () => await DownloadHelper.Download(newestDownloadUrl, Path.Combine(Path.GetTempPath(), "NVIDIA"), "driver.exe", reporter), null),

				// extract nvidia driver
				($@"Extracting NVIDIA driver {newestVersion}", async () => await ExtractHelper.Extract(Path.Combine(Path.GetTempPath(), "NVIDIA", "driver.exe"), Path.Combine(Path.GetTempPath(), "NVIDIA", "driver")), null),

				// strip nvidia driver
				($@"Stripping NVIDIA driver {newestVersion}", async () => await NvidiaHelper.StripDriver(), null),

				// update/install nvidia driver
				(gpu.IsInstalled ? $"Updating to NVIDIA driver {newestVersion}" : $"Installing NVIDIA driver {newestVersion}", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Path.GetTempPath(), "NVIDIA", "driver", "setup.exe"), Arguments = $"/s{(gpu.IsInstalled ? " /clean" : "")}", CreateNoWindow = true })!.WaitForExitAsync(), null),
                (gpu.IsInstalled ? $"Updating to NVIDIA driver {newestVersion}" : $"Installing NVIDIA driver {newestVersion}", async () => await Task.Delay(3000), null),
                (gpu.IsInstalled ? $"Updating to NVIDIA driver {newestVersion}" : $"Installing NVIDIA driver {newestVersion}", async () => GpuHelper.RefreshGpu(gpu), null),
                (gpu.IsInstalled ? $"Updating to NVIDIA driver {newestVersion}" : $"Installing NVIDIA driver {newestVersion}", async () => await Task.Delay(3000), null),
                ("Cleaning up NVIDIA files", async () => { string path = Path.Combine(Path.GetTempPath(), "NVIDIA"); foreach (Process process in ProcessesHelper.GetLockingProcesses(path)) { process.Kill(); process.WaitForExit(); } RegistryHelper.RunAs(RegistryHelper.Identity.TrustedInstaller, () => Directory.Delete(path, true)); }, null)
            };

        return actions;
    }

    public static List<(string Title, Func<Task> Action, Func<bool>? Condition)> TweakActions(GpuInfo gpu, string newestVersion)
    {
        var actions = new List<(string Title, Func<Task> Action, Func<bool>? Condition)>
            {
				// download nvidia control panel
				("Downloading NVIDIA Control Panel", async () => await StoreHelper.Download("NVIDIACorp.NVIDIAControlPanel_56jybvy8sckqj", 0), () => newestVersion.StartsWith("6")),

				// install nvidia control panel
				("Installing NVIDIA Control Panel", async () => await StoreHelper.Install("NVIDIACorp.NVIDIAControlPanel_56jybvy8sckqj"), () => newestVersion.StartsWith("6")),

				// disable nvidia tray icon
				(@"Disabling ""Show Notification Tray Icon""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\NVIDIA Corporation\NvTray", "StartOnLogin", 0, RegistryValueKind.DWord), null),
                (@"Disabling ""Show Notification Tray Icon""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\nvlddmkm\Global\NVTweak", "HideXGpuTrayIcon", 1, RegistryValueKind.DWord), null),
                (@"Disabling ""Show Notification Tray Icon""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\NVIDIA Corporation\Global\CoProcManager", "ShowTrayIcon", 0, RegistryValueKind.DWord), null),

				// select "use the advanced 3d image settings"
				(@"Selecting ""Use the advanced 3D image settings""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\SOFTWARE\NVIDIA Corporation\Global\NVTweak", "Gestalt", 515, RegistryValueKind.DWord), null),

				// import optimized nvidia profile
				("Importing optimized NVIDIA profile", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "NvidiaProfileInspector", "nvidiaProfileInspector.exe"), Arguments = $@"-silentimport ""{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "NvidiaProfileInspector", "BaseProfile.nip")}""", CreateNoWindow = true })!.WaitForExitAsync(), null),
				
				// configure physx to use gpu
				("Configuring PhysX to use GPU", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\nvlddmkm\Global\NVTweak", "NvCplPhysxAuto", 0, RegistryValueKind.DWord), null),

				// use nvidia settings for edge enhancements
				(@"Setting ""Edge enhancements"" to ""Use the NVIDIA setting""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "_User_SUB0_DFP0_XEN_Edge_Enhance", 2147483649, RegistryValueKind.DWord), null),
                (@"Setting ""Edge enhancements"" to ""Use the NVIDIA setting""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "_User_SUB0_DFP1_XEN_Edge_Enhance", 2147483649, RegistryValueKind.DWord), null),
                (@"Setting ""Edge enhancements"" to ""Use the NVIDIA setting""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "_User_SUB0_DFP2_XEN_Edge_Enhance", 2147483649, RegistryValueKind.DWord), null),
                (@"Setting ""Edge enhancements"" to ""Use the NVIDIA setting""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "_User_SUB0_DFP3_XEN_Edge_Enhance", 2147483649, RegistryValueKind.DWord), null),
                (@"Setting ""Edge enhancements"" to ""Use the NVIDIA setting""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "_User_SUB0_DFP4_XEN_Edge_Enhance", 2147483649, RegistryValueKind.DWord), null),

				// set edge enhancements to 0
				(@"Setting ""Edge enhancements"" to ""0%""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "_User_SUB0_DFP0_VAL_Edge_Enhance", 0, RegistryValueKind.DWord), null),
                (@"Setting ""Edge enhancements"" to ""0%""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "_User_SUB0_DFP1_VAL_Edge_Enhance", 0, RegistryValueKind.DWord), null),
                (@"Setting ""Edge enhancements"" to ""0%""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "_User_SUB0_DFP2_VAL_Edge_Enhance", 0, RegistryValueKind.DWord), null),
                (@"Setting ""Edge enhancements"" to ""0%""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "_User_SUB0_DFP3_VAL_Edge_Enhance", 0, RegistryValueKind.DWord), null),
                (@"Setting ""Edge enhancements"" to ""0%""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "_User_SUB0_DFP4_VAL_Edge_Enhance", 0, RegistryValueKind.DWord), null),
                (@"Setting ""Edge enhancements"" to ""0%""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "_User_SUB0_DFP0_XALG_Edge_Enhance", new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, RegistryValueKind.Binary), null),
                (@"Setting ""Edge enhancements"" to ""0%""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "_User_SUB0_DFP1_XALG_Edge_Enhance", new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, RegistryValueKind.Binary), null),
                (@"Setting ""Edge enhancements"" to ""0%""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "_User_SUB0_DFP2_XALG_Edge_Enhance", new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, RegistryValueKind.Binary), null),
                (@"Setting ""Edge enhancements"" to ""0%""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "_User_SUB0_DFP3_XALG_Edge_Enhance", new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, RegistryValueKind.Binary), null),
                (@"Setting ""Edge enhancements"" to ""0%""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "_User_SUB0_DFP4_XALG_Edge_Enhance", new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, RegistryValueKind.Binary), null),

				// use nvidia settings for noise reduction
				(@"Setting ""Noise reduction"" to ""Use the NVIDIA setting""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "_User_SUB0_DFP0_XEN_Noise_Reduce", 2147483649, RegistryValueKind.DWord), null),
                (@"Setting ""Noise reduction"" to ""Use the NVIDIA setting""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "_User_SUB0_DFP1_XEN_Noise_Reduce", 2147483649, RegistryValueKind.DWord), null),
                (@"Setting ""Noise reduction"" to ""Use the NVIDIA setting""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "_User_SUB0_DFP2_XEN_Noise_Reduce", 2147483649, RegistryValueKind.DWord), null),
                (@"Setting ""Noise reduction"" to ""Use the NVIDIA setting""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "_User_SUB0_DFP3_XEN_Noise_Reduce", 2147483649, RegistryValueKind.DWord), null),
                (@"Setting ""Noise reduction"" to ""Use the NVIDIA setting""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "_User_SUB0_DFP4_XEN_Noise_Reduce", 2147483649, RegistryValueKind.DWord), null),

				// set noise reduction to 0
				(@"Setting ""Noise reduction"" to ""0%""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "_User_SUB0_DFP0_VAL_Noise_Reduce", 0, RegistryValueKind.DWord), null),
                (@"Setting ""Noise reduction"" to ""0%""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "_User_SUB0_DFP1_VAL_Noise_Reduce", 0, RegistryValueKind.DWord), null),
                (@"Setting ""Noise reduction"" to ""0%""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "_User_SUB0_DFP2_VAL_Noise_Reduce", 0, RegistryValueKind.DWord), null),
                (@"Setting ""Noise reduction"" to ""0%""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "_User_SUB0_DFP3_VAL_Noise_Reduce", 0, RegistryValueKind.DWord), null),
                (@"Setting ""Noise reduction"" to ""0%""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "_User_SUB0_DFP4_VAL_Noise_Reduce", 0, RegistryValueKind.DWord), null),
                (@"Setting ""Noise reduction"" to ""0%""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "_User_SUB0_DFP0_XALG_Noise_Reduce", new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, RegistryValueKind.Binary), null),
                (@"Setting ""Noise reduction"" to ""0%""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "_User_SUB0_DFP1_XALG_Noise_Reduce", new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, RegistryValueKind.Binary), null),
                (@"Setting ""Noise reduction"" to ""0%""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "_User_SUB0_DFP2_XALG_Noise_Reduce", new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, RegistryValueKind.Binary), null),
                (@"Setting ""Noise reduction"" to ""0%""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "_User_SUB0_DFP3_XALG_Noise_Reduce", new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, RegistryValueKind.Binary), null),
                (@"Setting ""Noise reduction"" to ""0%""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "_User_SUB0_DFP4_XALG_Noise_Reduce", new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, RegistryValueKind.Binary), null),

				// disable use inverse telecine
				(@"Disabling ""Use inverse incline""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "_User_SUB0_DFP0_XALG_Cadence", new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, RegistryValueKind.Binary), null),
                (@"Disabling ""Use inverse incline""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "_User_SUB0_DFP1_XALG_Cadence", new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, RegistryValueKind.Binary), null),
                (@"Disabling ""Use inverse incline""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "_User_SUB0_DFP2_XALG_Cadence", new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, RegistryValueKind.Binary), null),
                (@"Disabling ""Use inverse incline""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "_User_SUB0_DFP3_XALG_Cadence", new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, RegistryValueKind.Binary), null),
                (@"Disabling ""Use inverse incline""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "_User_SUB0_DFP4_XALG_Cadence", new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, RegistryValueKind.Binary), null),

				// use nvidia settings for video color settings
				(@"Setting ""How do you make color adjustments?"" to ""With the NVIDIA settings""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "_User_SUB0_DFP0_XEN_Contrast", 2147483649, RegistryValueKind.DWord), null),
                (@"Setting ""How do you make color adjustments?"" to ""With the NVIDIA settings""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "_User_SUB0_DFP0_XEN_RGB_Gamma_G", 2147483649, RegistryValueKind.DWord), null),
                (@"Setting ""How do you make color adjustments?"" to ""With the NVIDIA settings""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "_User_SUB0_DFP0_XEN_RGB_Gamma_R", 2147483649, RegistryValueKind.DWord), null),
                (@"Setting ""How do you make color adjustments?"" to ""With the NVIDIA settings""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "_User_SUB0_DFP0_XEN_RGB_Gamma_B", 2147483649, RegistryValueKind.DWord), null),
                (@"Setting ""How do you make color adjustments?"" to ""With the NVIDIA settings""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "_User_SUB0_DFP0_XEN_Hue", 2147483649, RegistryValueKind.DWord), null),
                (@"Setting ""How do you make color adjustments?"" to ""With the NVIDIA settings""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "_User_SUB0_DFP0_XEN_Saturation", 2147483649, RegistryValueKind.DWord), null),
                (@"Setting ""How do you make color adjustments?"" to ""With the NVIDIA settings""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "_User_SUB0_DFP0_XEN_Brightness", 2147483649, RegistryValueKind.DWord), null),
                (@"Setting ""How do you make color adjustments?"" to ""With the NVIDIA settings""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "_User_SUB0_DFP0_XEN_Color_Range", 2147483649, RegistryValueKind.DWord), null),
                (@"Setting ""How do you make color adjustments?"" to ""With the NVIDIA settings""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "_User_SUB0_DFP1_XEN_Contrast", 2147483649, RegistryValueKind.DWord), null),
                (@"Setting ""How do you make color adjustments?"" to ""With the NVIDIA settings""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "_User_SUB0_DFP1_XEN_RGB_Gamma_G", 2147483649, RegistryValueKind.DWord), null),
                (@"Setting ""How do you make color adjustments?"" to ""With the NVIDIA settings""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "_User_SUB0_DFP1_XEN_RGB_Gamma_R", 2147483649, RegistryValueKind.DWord), null),
                (@"Setting ""How do you make color adjustments?"" to ""With the NVIDIA settings""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "_User_SUB0_DFP1_XEN_RGB_Gamma_B", 2147483649, RegistryValueKind.DWord), null),
                (@"Setting ""How do you make color adjustments?"" to ""With the NVIDIA settings""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "_User_SUB0_DFP1_XEN_Hue", 2147483649, RegistryValueKind.DWord), null),
                (@"Setting ""How do you make color adjustments?"" to ""With the NVIDIA settings""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "_User_SUB0_DFP1_XEN_Saturation", 2147483649, RegistryValueKind.DWord), null),
                (@"Setting ""How do you make color adjustments?"" to ""With the NVIDIA settings""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "_User_SUB0_DFP1_XEN_Brightness", 2147483649, RegistryValueKind.DWord), null),
                (@"Setting ""How do you make color adjustments?"" to ""With the NVIDIA settings""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "_User_SUB0_DFP1_XEN_Color_Range", 2147483649, RegistryValueKind.DWord), null),
                (@"Setting ""How do you make color adjustments?"" to ""With the NVIDIA settings""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "_User_SUB0_DFP2_XEN_Contrast", 2147483649, RegistryValueKind.DWord), null),
                (@"Setting ""How do you make color adjustments?"" to ""With the NVIDIA settings""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "_User_SUB0_DFP2_XEN_RGB_Gamma_G", 2147483649, RegistryValueKind.DWord), null),
                (@"Setting ""How do you make color adjustments?"" to ""With the NVIDIA settings""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "_User_SUB0_DFP2_XEN_RGB_Gamma_R", 2147483649, RegistryValueKind.DWord), null),
                (@"Setting ""How do you make color adjustments?"" to ""With the NVIDIA settings""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "_User_SUB0_DFP2_XEN_RGB_Gamma_B", 2147483649, RegistryValueKind.DWord), null),
                (@"Setting ""How do you make color adjustments?"" to ""With the NVIDIA settings""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "_User_SUB0_DFP2_XEN_Hue", 2147483649, RegistryValueKind.DWord), null),
                (@"Setting ""How do you make color adjustments?"" to ""With the NVIDIA settings""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "_User_SUB0_DFP2_XEN_Saturation", 2147483649, RegistryValueKind.DWord), null),
                (@"Setting ""How do you make color adjustments?"" to ""With the NVIDIA settings""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "_User_SUB0_DFP2_XEN_Brightness", 2147483649, RegistryValueKind.DWord), null),
                (@"Setting ""How do you make color adjustments?"" to ""With the NVIDIA settings""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "_User_SUB0_DFP2_XEN_Color_Range", 2147483649, RegistryValueKind.DWord), null),
                (@"Setting ""How do you make color adjustments?"" to ""With the NVIDIA settings""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "_User_SUB0_DFP3_XEN_Contrast", 2147483649, RegistryValueKind.DWord), null),
                (@"Setting ""How do you make color adjustments?"" to ""With the NVIDIA settings""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "_User_SUB0_DFP3_XEN_RGB_Gamma_G", 2147483649, RegistryValueKind.DWord), null),
                (@"Setting ""How do you make color adjustments?"" to ""With the NVIDIA settings""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "_User_SUB0_DFP3_XEN_RGB_Gamma_R", 2147483649, RegistryValueKind.DWord), null),
                (@"Setting ""How do you make color adjustments?"" to ""With the NVIDIA settings""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "_User_SUB0_DFP3_XEN_RGB_Gamma_B", 2147483649, RegistryValueKind.DWord), null),
                (@"Setting ""How do you make color adjustments?"" to ""With the NVIDIA settings""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "_User_SUB0_DFP3_XEN_Hue", 2147483649, RegistryValueKind.DWord), null),
                (@"Setting ""How do you make color adjustments?"" to ""With the NVIDIA settings""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "_User_SUB0_DFP3_XEN_Saturation", 2147483649, RegistryValueKind.DWord), null),
                (@"Setting ""How do you make color adjustments?"" to ""With the NVIDIA settings""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "_User_SUB0_DFP3_XEN_Brightness", 2147483649, RegistryValueKind.DWord), null),
                (@"Setting ""How do you make color adjustments?"" to ""With the NVIDIA settings""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "_User_SUB0_DFP3_XEN_Color_Range", 2147483649, RegistryValueKind.DWord), null),
                (@"Setting ""How do you make color adjustments?"" to ""With the NVIDIA settings""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "_User_SUB0_DFP4_XEN_Contrast", 2147483649, RegistryValueKind.DWord), null),
                (@"Setting ""How do you make color adjustments?"" to ""With the NVIDIA settings""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "_User_SUB0_DFP4_XEN_RGB_Gamma_G", 2147483649, RegistryValueKind.DWord), null),
                (@"Setting ""How do you make color adjustments?"" to ""With the NVIDIA settings""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "_User_SUB0_DFP4_XEN_RGB_Gamma_R", 2147483649, RegistryValueKind.DWord), null),
                (@"Setting ""How do you make color adjustments?"" to ""With the NVIDIA settings""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "_User_SUB0_DFP4_XEN_RGB_Gamma_B", 2147483649, RegistryValueKind.DWord), null),
                (@"Setting ""How do you make color adjustments?"" to ""With the NVIDIA settings""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "_User_SUB0_DFP4_XEN_Hue", 2147483649, RegistryValueKind.DWord), null),
                (@"Setting ""How do you make color adjustments?"" to ""With the NVIDIA settings""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "_User_SUB0_DFP4_XEN_Saturation", 2147483649, RegistryValueKind.DWord), null),
                (@"Setting ""How do you make color adjustments?"" to ""With the NVIDIA settings""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "_User_SUB0_DFP4_XEN_Brightness", 2147483649, RegistryValueKind.DWord), null),
                (@"Setting ""How do you make color adjustments?"" to ""With the NVIDIA settings""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "_User_SUB0_DFP4_XEN_Color_Range", 2147483649, RegistryValueKind.DWord), null),

				// set "dynamic range" to "full (0-255)"
				(@"Setting ""Dynamic range"" to ""Full (0-255)""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "_User_SUB0_DFP0_XALG_Color_Range", new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, RegistryValueKind.Binary), null),
                (@"Setting ""Dynamic range"" to ""Full (0-255)""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "_User_SUB0_DFP1_XALG_Color_Range", new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, RegistryValueKind.Binary), null),
                (@"Setting ""Dynamic range"" to ""Full (0-255)""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "_User_SUB0_DFP2_XALG_Color_Range", new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, RegistryValueKind.Binary), null),
                (@"Setting ""Dynamic range"" to ""Full (0-255)""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "_User_SUB0_DFP3_XALG_Color_Range", new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, RegistryValueKind.Binary), null),
                (@"Setting ""Dynamic range"" to ""Full (0-255)""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "_User_SUB0_DFP4_XALG_Color_Range", new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, RegistryValueKind.Binary), null),

				// configure color settings
				("Configuring color settings", async () => { foreach (string key in Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"System\CurrentControlSet\Services\nvlddmkm\State\DisplayDatabase")?.GetSubKeyNames() ?? []) RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, $@"HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\nvlddmkm\State\DisplayDatabase\{key}", "ColorformatConfig", new byte[] { 0xDB, 0x02, 0x00, 0x00, 0x14, 0x00, 0x00, 0x00, 0x00, 0x0A, 0x00, 0x08, 0x00, 0x00, 0x00, 0x00, 0x03, 0x01, 0x00, 0x00 }, RegistryValueKind.Binary); }, null),

				// rmpowerfeature
				("Configuring Miscellaneous NVIDIA Settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "RmPowerFeature", 1413829989, RegistryValueKind.DWord), null),
				
				// rmpowerfeature2
				("Configuring Miscellaneous NVIDIA Settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "RmPowerFeature2", 89478485, RegistryValueKind.DWord), null),

				// enable d3 pc latency
				("Configuring Miscellaneous NVIDIA Settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "D3PCLatency", 1, RegistryValueKind.DWord), null),
				
				// enableperformancemode
				("Configuring Miscellaneous NVIDIA Settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "EnablePerformanceMode", 1, RegistryValueKind.DWord), null),

				// disable runtime power management
				("Configuring Miscellaneous NVIDIA Settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "EnableRuntimePowerManagement", 0, RegistryValueKind.DWord), null),

				// rmelcg
				("Configuring Miscellaneous NVIDIA Settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "RMElcg", 1431655764, RegistryValueKind.DWord), null),

				// disable gc6
				("Configuring Miscellaneous NVIDIA Settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "RMGC6Feature", 699050, RegistryValueKind.DWord), null),

				// disable all latency optimizations for gc6
				("Configuring Miscellaneous NVIDIA Settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "RMGC6Parameters", 55, RegistryValueKind.DWord), null),

				// rmgcofffeature
				("Configuring Miscellaneous NVIDIA Settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "RMGCOffFeature", 2, RegistryValueKind.DWord), null),

				// disable all gc5 idle features
				("Configuring Miscellaneous NVIDIA Settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "RMDidleFeatureGC5", 44731050, RegistryValueKind.DWord), null),

				// disable d3 related features
				("Configuring Miscellaneous NVIDIA Settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "RMD3Feature", 2, RegistryValueKind.DWord), null),

				// disable slowdowns
				("Configuring Miscellaneous NVIDIA Settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "RMClkSlowDown", 67108864, RegistryValueKind.DWord), null),
                ("Configuring Miscellaneous NVIDIA Settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "RmOverrideIdleSlowdownSettings", 0, RegistryValueKind.DWord), null),

				// disable lowering mclk
				("Configuring Miscellaneous NVIDIA Settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "RmOptp2LowerMclk", 0, RegistryValueKind.DWord), null),

				// disable the power-off-dram-pll-when-unused feature
				("Configuring Miscellaneous NVIDIA Settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "RmClkPowerOffDramPllWhenUnused", 0, RegistryValueKind.DWord), null),

				// configure low power features
				("Configuring Miscellaneous NVIDIA Settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "RMLpwrArch", 349525, RegistryValueKind.DWord), null),
                ("Configuring Miscellaneous NVIDIA Settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "RMLpwrEiClient", 5, RegistryValueKind.DWord), null),
                ("Configuring Miscellaneous NVIDIA Settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "RmLpwrCtrlGrRgParameters", 89478485, RegistryValueKind.DWord), null),
                ("Configuring Miscellaneous NVIDIA Settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "RmLpwrCtrlMsDifrCgParameters", 1365, RegistryValueKind.DWord), null),
                ("Configuring Miscellaneous NVIDIA Settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "RmLpwrCtrlMsDifrSwAsrParameters", 5461, RegistryValueKind.DWord), null),
                ("Configuring Miscellaneous NVIDIA Settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "RmLpwrCtrlMsLtcParameters", 5, RegistryValueKind.DWord), null),
                ("Configuring Miscellaneous NVIDIA Settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "RmLpwrGrPgSwFilterFunction", 0, RegistryValueKind.DWord), null),
                ("Configuring Miscellaneous NVIDIA Settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "RmLpwrCacheStatsOnD3", 0, RegistryValueKind.DWord), null),

				// keep mscg enabled from rm side
				("Configuring Miscellaneous NVIDIA Settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "RmDwbMscg", 1, RegistryValueKind.DWord), null),

				// disable slides mclk
				("Configuring Miscellaneous NVIDIA Settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "SlideMCLK", 0, RegistryValueKind.DWord), null),

				// disable aspm
				("Configuring Miscellaneous NVIDIA Settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "RMEnableASPMAtLoad", 0, RegistryValueKind.DWord), null),
                ("Configuring Miscellaneous NVIDIA Settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "RMDisableGpuASPMFlags", 3, RegistryValueKind.DWord), null),
				
				// disable optimal power for padlink pll
				("Configuring Miscellaneous NVIDIA Settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "RMDisableOptimalPowerForPadlinkPll", 1, RegistryValueKind.DWord), null),

				// disable rtd3 d3hot
				("Configuring Miscellaneous NVIDIA Settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "RMForceRtd3D3Hot", 2, RegistryValueKind.DWord), null),

				// force never power off the mios
				("Configuring Miscellaneous NVIDIA Settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "RmMIONoPowerOff", 1, RegistryValueKind.DWord), null),

				// enable war
				("Configuring Miscellaneous NVIDIA Settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "RM2644249", 1, RegistryValueKind.DWord), null),

                ("Configuring Miscellaneous NVIDIA Settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "RM303107", 16, RegistryValueKind.DWord), null),

				// disable clkreq and deep l1
				("Configuring Miscellaneous NVIDIA Settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "RM2779240", 5, RegistryValueKind.DWord), null),

				// enable opsb feature
				("Configuring Miscellaneous NVIDIA Settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "RM580312", 1, RegistryValueKind.DWord), null),

				// rmbug2519005war
				("Configuring Miscellaneous NVIDIA Settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "RMBug2519005War", 1, RegistryValueKind.DWord), null),
				
				// rmceelcgwar1895530
				("Configuring Miscellaneous NVIDIA Settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "RmCeElcgWar1895530", 1, RegistryValueKind.DWord), null),
				
				// rmwar1760398
				("Configuring Miscellaneous NVIDIA Settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "RmWar1760398", 1, RegistryValueKind.DWord), null),

				// disable opsb (optional power saving bundle)
				("Configuring Miscellaneous NVIDIA Settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "RMOPSB", 10914, RegistryValueKind.DWord), null),
				
				// disable bbx inform
				("Configuring Miscellaneous NVIDIA Settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "RmDisableInforomBBX", 15, RegistryValueKind.DWord), null),

				// configure paging features
				("Configuring Miscellaneous NVIDIA Settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "RmPgCtrlParameters", 1431655765, RegistryValueKind.DWord), null),
                ("Configuring Miscellaneous NVIDIA Settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "RmPgCtrlGrParameters", 1431655765, RegistryValueKind.DWord), null),

				// disable lpwr fsms on init
				("Configuring Miscellaneous NVIDIA Settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "RMElpgStateOnInit", 3, RegistryValueKind.DWord), null),

				// disable 10 types of acpi calls from the resource manager to the sbios
				("Configuring Miscellaneous NVIDIA Settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "RmDisableACPI", 1023, RegistryValueKind.DWord), null),

				// disable post l2 compression
				("Configuring Miscellaneous NVIDIA Settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "RMDisablePostL2Compression", 1, RegistryValueKind.DWord), null),

				// disable non-contiguous allocation
				("Configuring Miscellaneous NVIDIA Settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "RMDisableNoncontigAlloc", 1, RegistryValueKind.DWord), null),
				
				// configure bandwidth feature
				("Configuring Miscellaneous NVIDIA Settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "RMBandwidthFeature", 1896072192, RegistryValueKind.DWord), null),

				// disable mempool compression
				("Configuring Miscellaneous NVIDIA Settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "RMBandwidthFeature2", 1, RegistryValueKind.DWord), null),

				// prefer system memory contiguous
				("Configuring Miscellaneous NVIDIA Settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "PreferSystemMemoryContiguous", 1, RegistryValueKind.DWord), null),
				
				// disable edc replay
				("Configuring Miscellaneous NVIDIA Settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "RMDisableEDC", 1, RegistryValueKind.DWord), null),
				
				// disable illegal compstat access
				("Configuring Miscellaneous NVIDIA Settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "RMDisableIntrIllegalCompstatAccess", 1, RegistryValueKind.DWord), null),

				// disable lrc coalescing
				("Configuring Miscellaneous NVIDIA Settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "RMDisableLRCCoalescing", 1, RegistryValueKind.DWord), null),

				// disables hw fault buffers on pascal+ chips
				("Configuring Miscellaneous NVIDIA Settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "RmDisableHwFaultBuffer", 1, RegistryValueKind.DWord), null),

				// force "hardware composed: independent flip"
				(@"Forcing ""Hardware Composed: Independent Flip""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "enableRS2FlipCollapse", 1, RegistryValueKind.DWord), null),
                (@"Forcing ""Hardware Composed: Independent Flip""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "enableRS2ImmediateFlipCompletionReporting", 1, RegistryValueKind.DWord), null),

				// disable display power savings
				("Disabling Display Power Savings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\nvlddmkm\Global\NVTweak", "DisplayPowerSaving", 0, RegistryValueKind.DWord), null),
                ("Disabling Display Power Savings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\Software\NVIDIA Corporation\Global\NVTweak", "DisplayPowerSaving", 0, RegistryValueKind.DWord), null),

				// disable hd audio power savings
				("Disabling HD Audio Power Savings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\nvlddmkm", "EnableHDAudioD3Cold", 0, RegistryValueKind.DWord), null),
			
				// disable dlss indicator
				("Disabling DLSS Indicator", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\NVIDIA Corporation\Global\NGXCore", "ShowDlssIndicator", 0, RegistryValueKind.DWord), null),

				// disable automatic updates
				("Disabling automatic updates", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\NVIDIA Corporation\Global\CoProcManager", "AutoDownload", 0, RegistryValueKind.DWord), null),

				// disable telemetry
				("Disabling telemetry", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\Software\Nvidia Corporation\NvControlPanel2\Client", "OptInOrOutPreference", 0, RegistryValueKind.DWord), null),
                ("Disabling telemetry", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\nvlddmkm\Global\Startup", "SendTelemetryData", 0, RegistryValueKind.DWord), null),
                ("Disabling telemetry", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\NVIDIA Corporation\Global\FTS", "EnableRID44231", 0, RegistryValueKind.DWord), null),
                ("Disabling telemetry", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\NVIDIA Corporation\Global\FTS", "EnableRID64640", 0, RegistryValueKind.DWord), null),
                ("Disabling telemetry", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\NVIDIA Corporation\Global\FTS", "EnableRID66610", 0, RegistryValueKind.DWord), null),
                ("Disabling telemetry", async () => await RegistryHelper.RunAs(RegistryHelper.Identity.TrustedInstaller, new ProcessStartInfo { FileName = "cmd.exe", Arguments = $@"/c cd /d ""{Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "DriverStore", "FileRepository")}"" & dir NvTelemetry64.dll /a /b /s & del NvTelemetry64.dll /a /s", CreateNoWindow = true }), null),

				// disable logging
				("Disabling logging", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\nvlddmkm\Parameters", "LogDisableMasks", Enumerable.Range(0, "00ffff0f01ffff0f02ffff0f03ffff0f04ffff0f05ffff0f06ffff0f07ffff0f08ffff0f09ffff0f0affff0f0bffff0f0cffff0f0dffff0f0effff0f0fffff0f10ffff0f11ffff0f12ffff0f13ffff0f14ffff0f15ffff0f16ffff0f00ffff1f01ffff1f02ffff1f03ffff1f04ffff1f05ffff1f06ffff1f07ffff1f08ffff1f09ffff1f0affff1f0bffff1f0cffff1f0dffff1f0effff1f0fffff1f00ffff2f01ffff2f02ffff2f03ffff2f04ffff2f05ffff2f06ffff2f07ffff2f08ffff2f09ffff2f0affff2f0bffff2f0cffff2f0dffff2f0effff2f0fffff2f00ffff3f01ffff3f02ffff3f03ffff3f04ffff3f05ffff3f06ffff3f07ffff3f".Length / 2).Select(x => Convert.ToByte("00ffff0f01ffff0f02ffff0f03ffff0f04ffff0f05ffff0f06ffff0f07ffff0f08ffff0f09ffff0f0affff0f0bffff0f0cffff0f0dffff0f0effff0f0fffff0f10ffff0f11ffff0f12ffff0f13ffff0f14ffff0f15ffff0f16ffff0f00ffff1f01ffff1f02ffff1f03ffff1f04ffff1f05ffff1f06ffff1f07ffff1f08ffff1f09ffff1f0affff1f0bffff1f0cffff1f0dffff1f0effff1f0fffff1f00ffff2f01ffff2f02ffff2f03ffff2f04ffff2f05ffff2f06ffff2f07ffff2f08ffff2f09ffff2f0affff2f0bffff2f0cffff2f0dffff2f0effff2f0fffff2f00ffff3f01ffff3f02ffff3f03ffff3f04ffff3f05ffff3f06ffff3f07ffff3f".Substring(x * 2, 2), 16)).ToArray(), RegistryValueKind.Binary), null),
                ("Disabling logging", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\nvlddmkm\Parameters", "LogWarningEntries", 0, RegistryValueKind.DWord), null),
                ("Disabling logging", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\nvlddmkm\Parameters", "LogPagingEntries", 0, RegistryValueKind.DWord), null),
                ("Disabling logging", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\nvlddmkm\Parameters", "LogEventEntries", 0, RegistryValueKind.DWord), null),
                ("Disabling logging", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\nvlddmkm\Parameters", "LogErrorEntries", 0, RegistryValueKind.DWord), null),

				// disable dynamic P-State/adaptive clocking
				("Disabling Dynamic Performance States (P-States)", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "DisableDynamicPstate", 1, RegistryValueKind.DWord), () => gpu.PStates == false),
				
				// disable asynchronous p-state changes
				("Disabling Dynamic Performance States (P-States)", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "DisableAsyncPstates", 1, RegistryValueKind.DWord), () => gpu.PStates == false),

				// disable error code correction (ecc)
				("Disabling Error Code Correction (ECC)", async () => await RegistryHelper.RunAs(RegistryHelper.Identity.TrustedInstaller, new ProcessStartInfo { FileName = "nvidia-smi.exe", Arguments = "-e 0", CreateNoWindow = true }), () => gpu.ECCSupport && gpu.ECC == false),
                ("Disabling Error Code Correction (ECC)", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "RMEnableL1ECC", 0, RegistryValueKind.DWord), () => gpu.ECCSupport && gpu.ECC == false),
                ("Disabling Error Code Correction (ECC)", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "RMEnableSMECC", 0, RegistryValueKind.DWord), () => gpu.ECCSupport && gpu.ECC == false),
                ("Disabling Error Code Correction (ECC)", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "RMEnableSHMECC", 0, RegistryValueKind.DWord), () => gpu.ECCSupport && gpu.ECC == false),
                ("Disabling Error Code Correction (ECC)", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "RMAssertOnEccErrors", 0, RegistryValueKind.DWord), () => gpu.ECCSupport && gpu.ECC == false),

				// enable gsp firmware
				("Enabling GSP Firmware", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "EnableGpuFirmware", 1, RegistryValueKind.DWord), () => gpu.GspFirmware == true),
                ("Enabling GSP Firmware", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "EnableGpuFirmwareLogs", 0, RegistryValueKind.DWord), () => gpu.GspFirmware == true),

				// disable high-bandwidth digital content protection (hdcp)
				("Disabling High-Bandwidth Digital Content Protection (HDCP)", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "RMHdcpKeyglobZero", 1, RegistryValueKind.DWord), () => gpu.HDCP == false),
                ("Disabling High-Bandwidth Digital Content Protection (HDCP)", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "RmDisableHdcp22", 1, RegistryValueKind.DWord), () => gpu.HDCP == false),

				// disable high-definition multimedia interface (hdmi)/displayport (dp) audio
				("Disabling High-Definition Multimedia Interface (HDMI)/DisplayPort (DP) Audio", async () => GpuHelper.ToggleHdmiDpAudio(gpu, false), () => gpu.HDMIDPAudio == false)
            };

        return actions;
    }
}
