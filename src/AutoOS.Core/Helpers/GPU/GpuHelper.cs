using AutoOS.Core.Helpers.GPU.Models;
using DevWinUI;
using System.Net.Security;
using Windows.Win32.Devices.DeviceAndDriverInstallation;
using Windows.Win32.Foundation;
using Windows.Win32;

namespace AutoOS.Core.Helpers.GPU;

public static partial class GpuHelper
{
    private static readonly HttpClient httpClient = new(new SocketsHttpHandler
    {
        SslOptions = new SslClientAuthenticationOptions
        {
            EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12 | System.Security.Authentication.SslProtocols.Tls13
        }
    })
    {
    DefaultRequestHeaders =
        {
            UserAgent =
            {
                new System.Net.Http.Headers.ProductInfoHeaderValue("AutoOS", ProcessInfoHelper.Version)
            }
        }
    };

    public unsafe static List<GpuInfo> GetGPUs()
    {
        var gpus = new List<GpuInfo>();
        Dictionary<string, (string Vendor, Dictionary<string, string> Devices)> pciDb = null;

        Guid guid = new("4d36e968-e325-11ce-bfc1-08002be10318");
        HDEVINFO hDevInfo = PInvoke.SetupDiGetClassDevs(&guid, null, HWND.Null, SETUP_DI_GET_CLASS_DEVS_FLAGS.DIGCF_PRESENT);

        if (hDevInfo.Value == (nint)(-1)) return gpus;

        Span<char> idBuffer = stackalloc char[512];
        Span<char> audioBuffer = stackalloc char[512];

        try
        {
            uint index = 0;
            while (true)
            {
                SP_DEVINFO_DATA devInfo = new() { cbSize = (uint)sizeof(SP_DEVINFO_DATA) };
                if (!PInvoke.SetupDiEnumDeviceInfo(hDevInfo, index++, &devInfo)) break;

                string pnpDeviceId = string.Empty;
                fixed (char* pIdBuffer = idBuffer)
                {
                    uint requiredSize;
                    if (PInvoke.SetupDiGetDeviceInstanceId(hDevInfo, &devInfo, pIdBuffer, (uint)idBuffer.Length, &requiredSize))
                    {
                        pnpDeviceId = new string(pIdBuffer);
                    }
                }

                string registryPath = GetRegistryPath(hDevInfo, devInfo);
                string vendorId = pnpDeviceId.Substring(pnpDeviceId.IndexOf("VEN_") + 4, 4).ToLowerInvariant();
                string deviceId = pnpDeviceId.Substring(pnpDeviceId.IndexOf("DEV_") + 4, 4).ToLowerInvariant();

                string currentVersion = GetDriverVersion(hDevInfo, devInfo);
                bool isInstalled = !string.IsNullOrEmpty(currentVersion) && (!currentVersion.StartsWith("10.0.") || !currentVersion.EndsWith(".1"));

                string deviceName = string.Empty;
                string codename = string.Empty;
                bool pstates = false;
                bool hdcp = false;
                bool ecc = false;
                bool gspFirmware = false;
                bool hdmidpaudio = true;

                string gpuLocation = GetLocationInfo(hDevInfo, devInfo);
                int gpuFuncIdx = gpuLocation.IndexOf(", function");
                string gpuBusDev = gpuFuncIdx > -1 ? gpuLocation[..gpuFuncIdx] : gpuLocation;

                if (isInstalled && (vendorId == "10de" || vendorId == "1002" || vendorId == "8086"))
                {
                    deviceName = GetDeviceName(hDevInfo, devInfo);

                    if (vendorId == "10de")
                    {
                        var versionParts = currentVersion.Split('.');
                        if (versionParts.Length >= 4)
                        {
                            currentVersion = string.Concat(versionParts[2].AsSpan()[1..], versionParts[3].AsSpan()[..2], ".", versionParts[3].AsSpan(2, 2));
                        }
                        pstates = Microsoft.Win32.Registry.GetValue(registryPath, "DisableDynamicPstate", null) is not int pstateValue || pstateValue == 0;
                        ecc = Microsoft.Win32.Registry.GetValue(registryPath, "RMEnableL1ECC", null) is not int eccValue || eccValue == 1;
                        gspFirmware = Microsoft.Win32.Registry.GetValue(registryPath, "EnableGpuFirmware", null) is int firmwareValue && firmwareValue == 1;
                        hdcp = Microsoft.Win32.Registry.GetValue(registryPath, "RMHdcpKeyglobZero", null) is int intValue && intValue == 0;
                     }
                    else if (vendorId == "1002")
                    {
                        currentVersion = (Microsoft.Win32.Registry.GetValue(registryPath, "RadeonSoftwareVersion", null) ?? Microsoft.Win32.Registry.GetValue(registryPath, "FireproSoftwareVersion", null))?.ToString();
                    }
                    else if (vendorId == "8086")
                    {
                        pciDb ??= LoadPciDatabase();

                        if (pciDb.TryGetValue(vendorId, out var vendor) && vendor.Devices.TryGetValue(deviceId, out var name))
                        {
                            var versionParts = currentVersion?.Split('.');
                            currentVersion = versionParts?.Length >= 4 ? versionParts[2] + "." + versionParts[3] : currentVersion;
                            codename = name.Split('[')[0].Trim();
                        }
                    }

                    Guid audioGuid = new("4d36e97d-e325-11ce-bfc1-08002be10318");
                    HDEVINFO hAudioInfo = PInvoke.SetupDiGetClassDevs(&audioGuid, null, HWND.Null, SETUP_DI_GET_CLASS_DEVS_FLAGS.DIGCF_PRESENT);
                    if (hAudioInfo.Value != (nint)(-1))
                    {
                        try
                        {
                            uint audioIndex = 0;
                            SP_DEVINFO_DATA audioDev = new() { cbSize = (uint)sizeof(SP_DEVINFO_DATA) };
                            while (PInvoke.SetupDiEnumDeviceInfo(hAudioInfo, audioIndex++, &audioDev))
                            {
                                string audioLoc = GetLocationInfo(hAudioInfo, audioDev);
                                int audioFuncIdx = audioLoc.IndexOf(", function");
                                string audioBusDev = audioFuncIdx > -1 ? audioLoc[..audioFuncIdx] : audioLoc;

                                if (string.IsNullOrEmpty(gpuBusDev) || gpuBusDev != audioBusDev) continue;

                                fixed (char* pAudioBuffer = audioBuffer)
                                {
                                    uint audioReq;
                                    if (PInvoke.SetupDiGetDeviceInstanceId(hAudioInfo, &audioDev, pAudioBuffer, (uint)audioBuffer.Length, &audioReq))
                                    {
                                        if (PInvoke.CM_Locate_DevNode(out uint devInst, pAudioBuffer, CM_LOCATE_DEVNODE_FLAGS.CM_LOCATE_DEVNODE_NORMAL) == CONFIGRET.CR_SUCCESS)
                                        {
                                            if (PInvoke.CM_Get_DevNode_Status(out var status, out var prob, devInst, 0) == CONFIGRET.CR_SUCCESS)
                                            {
                                                hdmidpaudio = (status & CM_DEVNODE_STATUS_FLAGS.DN_STARTED) != 0;
                                                break;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        finally 
                        { 
                            PInvoke.SetupDiDestroyDeviceInfoList(hAudioInfo);
                        }
                    }
                }
                else if (!isInstalled && (vendorId == "10de" || vendorId == "1002" || vendorId == "8086"))
                {
                    pciDb ??= LoadPciDatabase();

                    if (pciDb.TryGetValue(vendorId, out var vendor) && vendor.Devices.TryGetValue(deviceId, out var name))
                    {
                        if (vendorId == "8086")
                            codename = name.Split('[')[0].Trim();

                        deviceName = name.Split('[', ']') is { Length: > 1 } parts ? parts[1] : name;
                        deviceName = vendorId switch { "10de" => "NVIDIA " + deviceName, "1002" => "AMD " + deviceName, _ => "Intel " + deviceName };
                        currentVersion = "N/A";
                    }
                }
                else continue;

                gpus.Add(new GpuInfo
                {
                    PnPDeviceId = pnpDeviceId,
                    DeviceName = deviceName,
                    VendorId = vendorId,
                    DeviceId = deviceId,
                    Codename = codename,
                    CurrentVersion = currentVersion,
                    IsInstalled = isInstalled,
                    RegistryPath = registryPath,
                    PStates = pstates,
                    ECC = ecc,
                    GspFirmware = gspFirmware,
                    HDCP = hdcp,
                    HDMIDPAudio = hdmidpaudio,
                    Location = gpuBusDev
                });
            }
        }
        finally 
        { 
            PInvoke.SetupDiDestroyDeviceInfoList(hDevInfo); 
        }

        return gpus;
    }

    private static Dictionary<string, (string Vendor, Dictionary<string, string> Devices)> LoadPciDatabase()
    {
        string pciPath = Path.Combine(PathHelper.GetAppDataFolderPath(), "pci.ids");
        if (!File.Exists(pciPath))
        {
            var data = httpClient.GetByteArrayAsync("https://raw.githubusercontent.com/pciutils/pciids/master/pci.ids").GetAwaiter().GetResult();
            File.WriteAllBytes(pciPath, data);
        }

        var db = new Dictionary<string, (string Vendor, Dictionary<string, string> Devices)>(StringComparer.OrdinalIgnoreCase);
        string currentVendor = null;

        foreach (var line in File.ReadLines(pciPath))
        {
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;
            if (!char.IsWhiteSpace(line[0]))
            {
                var parts = line.Split(' ', 2);
                if (parts.Length < 2) continue;
                currentVendor = parts[0].ToLowerInvariant();
                db[currentVendor] = (parts[1].Trim(), new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));
            }
            else if (line.StartsWith("\t") && currentVendor != null)
            {
                var parts = line.Trim().Split(' ', 2);
                if (parts.Length < 2) continue;
                db[currentVendor].Devices[parts[0].ToLowerInvariant()] = parts[1].Trim();
            }
        }
        return db;
    }

    public static void RefreshGpu(GpuInfo gpu)
    {
        var all = GetGPUs();

        var updated = all.FirstOrDefault(x => x.PnPDeviceId == gpu.PnPDeviceId);

        if (updated == null)
            return;

        gpu.DeviceName = updated.DeviceName;
        gpu.CurrentVersion = updated.CurrentVersion;
        gpu.IsInstalled = updated.IsInstalled;
        gpu.RegistryPath = updated.RegistryPath;
    }

    private unsafe static string GetDeviceName(HDEVINFO hDevInfo, SP_DEVINFO_DATA devInfo)
    {
        uint regType;
        uint requiredSize;

        byte* buffer = stackalloc byte[1024];

        bool success = PInvoke.SetupDiGetDeviceRegistryProperty(
            hDevInfo,
            &devInfo,
            (uint)SETUP_DI_REGISTRY_PROPERTY.SPDRP_DEVICEDESC,
            &regType,
            buffer,
            1024,
            &requiredSize
        );

        if (!success)
        {
            return string.Empty;
        }

        return new string((char*)buffer);
    }

    private unsafe static string GetDriverVersion(HDEVINFO hDevInfo, SP_DEVINFO_DATA devInfo)
    {
        uint requiredSize;
        Windows.Win32.Devices.Properties.DEVPROPTYPE propType;
        var propertyKey = PInvoke.DEVPKEY_Device_DriverVersion;

        PInvoke.SetupDiGetDeviceProperty(
            hDevInfo,
            &devInfo,
            &propertyKey,
            &propType,
            null,
            0,
            &requiredSize,
            0
        );

        if (requiredSize == 0)
            return null;

        byte* buffer = stackalloc byte[(int)requiredSize];

        if (!PInvoke.SetupDiGetDeviceProperty(
                hDevInfo,
                &devInfo,
                &propertyKey,
                &propType,
                buffer,
                requiredSize,
                null,
                0))
            return null;

        return new string((char*)buffer);
    }

    private static unsafe string GetLocationInfo(HDEVINFO hDevInfo, SP_DEVINFO_DATA devInfo)
    {
        uint propertyType;
        uint requiredSize;
        PInvoke.SetupDiGetDeviceRegistryProperty(hDevInfo, &devInfo, SETUP_DI_REGISTRY_PROPERTY.SPDRP_LOCATION_INFORMATION, &propertyType, null, 0, &requiredSize);
        if (requiredSize == 0) return string.Empty;

        byte[] buffer = new byte[requiredSize];
        fixed (byte* pBuffer = buffer)
        {
            if (PInvoke.SetupDiGetDeviceRegistryProperty(hDevInfo, &devInfo, SETUP_DI_REGISTRY_PROPERTY.SPDRP_LOCATION_INFORMATION, &propertyType, pBuffer, (uint)buffer.Length, &requiredSize))
                return System.Text.Encoding.Unicode.GetString(buffer).TrimEnd('\0');
        }
        return string.Empty;
    }

    private unsafe static string GetRegistryPath(HDEVINFO hDevInfo, SP_DEVINFO_DATA devInfo)
    {
        uint regType;
        uint requiredSize;

        byte* buffer = stackalloc byte[1024];

        PInvoke.SetupDiGetDeviceRegistryProperty(
            hDevInfo,
            &devInfo,
            SETUP_DI_REGISTRY_PROPERTY.SPDRP_DRIVER,
            &regType,
            buffer,
            1024,
            &requiredSize
        );

        string driverKey = new((char*)buffer);

        if (string.IsNullOrEmpty(driverKey))
            return null;

        return $@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Class\{driverKey}";
    }

    public static unsafe void ToggleHdmiDpAudio(GpuInfo gpu, bool enable)
    {
        Guid hdaudioGuid = new("4d36e97d-e325-11ce-bfc1-08002be10318");
        HDEVINFO hAudioInfo = PInvoke.SetupDiGetClassDevs(&hdaudioGuid, null, HWND.Null, SETUP_DI_GET_CLASS_DEVS_FLAGS.DIGCF_PRESENT);

        if (hAudioInfo.Value == (-1)) return;

        try
        {
            uint index = 0;
            SP_DEVINFO_DATA audioDevData = new() { cbSize = (uint)sizeof(SP_DEVINFO_DATA) };

            while (PInvoke.SetupDiEnumDeviceInfo(hAudioInfo, index++, &audioDevData))
            {
                string audioLoc = GetLocationInfo(hAudioInfo, audioDevData);
                int audioFuncIdx = audioLoc.IndexOf(", function");
                string audioBusDev = audioFuncIdx > -1 ? audioLoc[..audioFuncIdx] : audioLoc;

                if (gpu.Location != audioBusDev) continue;

                var propChangeParams = new SP_PROPCHANGE_PARAMS
                {
                    ClassInstallHeader = new SP_CLASSINSTALL_HEADER
                    {
                        cbSize = (uint)sizeof(SP_CLASSINSTALL_HEADER),
                        InstallFunction = DI_FUNCTION.DIF_PROPERTYCHANGE
                    },
                    StateChange = enable ? SETUP_DI_STATE_CHANGE.DICS_ENABLE : SETUP_DI_STATE_CHANGE.DICS_DISABLE,
                    Scope = SETUP_DI_PROPERTY_CHANGE_SCOPE.DICS_FLAG_GLOBAL,
                    HwProfile = 0
                };

                if (PInvoke.SetupDiSetClassInstallParams(hAudioInfo, &audioDevData, (SP_CLASSINSTALL_HEADER*)&propChangeParams, (uint)sizeof(SP_PROPCHANGE_PARAMS)))
                {
                    PInvoke.SetupDiCallClassInstaller(DI_FUNCTION.DIF_PROPERTYCHANGE, hAudioInfo, &audioDevData);
                }
            }
        }
        finally 
        { 
            PInvoke.SetupDiDestroyDeviceInfoList(hAudioInfo); 
        }
    }
}
