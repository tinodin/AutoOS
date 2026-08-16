using System.Collections.Concurrent;
using System.Text.Json.Nodes;
using AutoOS.Core.Data.Models.Device;
using AutoOS.Core.Helpers.ReadWrite;
using Microsoft.Win32;
using Windows.Storage;
using Windows.Win32;
using Windows.Win32.Devices.DeviceAndDriverInstallation;
using Windows.Win32.Foundation;
using Windows.Win32.NetworkManagement.IpHelper;
using Windows.Win32.NetworkManagement.Ndis;
using static Windows.Win32.Devices.DeviceAndDriverInstallation.SETUP_DI_GET_CLASS_DEVS_FLAGS;
using static Windows.Win32.Devices.DeviceAndDriverInstallation.SETUP_DI_REGISTRY_PROPERTY;

namespace AutoOS.Core.Helpers.Device;

public static partial class DeviceHelper
{
	private static readonly Guid GUID_DEVCLASS_DISPLAY = new("4d36e968-e325-11ce-bfc1-08002be10318");
	private static readonly Guid GUID_DEVCLASS_USB = new("36fc9e60-c465-11cf-8056-444553540000");
	private static readonly Guid GUID_DEVCLASS_NET = new("4d36e972-e325-11ce-bfc1-08002be10318");
	private static readonly Guid GUID_DEVCLASS_HDAUD = new("4d36e96c-e325-11ce-bfc1-08002be10318");
	private static readonly Guid GUID_DEVCLASS_AUDIOENDPOINT = new("c166523c-fe0c-4a94-a586-f1a80cfbbf3e");
	private static readonly Guid GUID_DEVCLASS_SYSTEM = new("4d36e97d-e325-11ce-bfc1-08002be10318");

	public unsafe static List<DeviceInfo> GetDevices(DeviceType type)
	{
		var devices = new List<DeviceInfo>();
		Guid classGuid = type switch
		{
			DeviceType.GPU => GUID_DEVCLASS_DISPLAY,
			DeviceType.XHCI => GUID_DEVCLASS_USB,
			DeviceType.NIC => GUID_DEVCLASS_NET,
			DeviceType.HDAUD => GUID_DEVCLASS_HDAUD,
			DeviceType.AudioEndpoint => GUID_DEVCLASS_AUDIOENDPOINT,
			DeviceType.AudioController => GUID_DEVCLASS_SYSTEM,
			_ => throw new ArgumentException("Unknown device type")
		};

		SETUP_DI_GET_CLASS_DEVS_FLAGS flags = DIGCF_PRESENT;
		HDEVINFO deviceInfoSetHandle = PInvoke.SetupDiGetClassDevs(&classGuid, null, HWND.Null, flags);
		if ((IntPtr)deviceInfoSetHandle == (IntPtr)(-1)) return devices;

		const int MAX_DEVICE_ID_LEN = 256;
		char* pIdBuffer = stackalloc char[MAX_DEVICE_ID_LEN];

		uint index = 0;
		while (true)
		{
			SP_DEVINFO_DATA deviceInfoData = default;
			deviceInfoData.cbSize = (uint)sizeof(SP_DEVINFO_DATA);

			if (!PInvoke.SetupDiEnumDeviceInfo(deviceInfoSetHandle, index++, &deviceInfoData)) break;

			string enumerator = GetDeviceRegistryPropertyString(deviceInfoSetHandle, &deviceInfoData, SPDRP_ENUMERATOR_NAME);
			string service = GetDeviceRegistryPropertyString(deviceInfoSetHandle, &deviceInfoData, SPDRP_SERVICE);

			if (type == DeviceType.GPU)
			{
				if (!string.Equals(enumerator, "PCI", StringComparison.OrdinalIgnoreCase)) continue;
				if (string.Equals(service, "BasicDisplay", StringComparison.OrdinalIgnoreCase)) continue;
			}
			else if (type == DeviceType.XHCI)
			{
				if (!string.Equals(service, "USBXHCI", StringComparison.OrdinalIgnoreCase)) continue;
			}
			else if (type == DeviceType.AudioController)
			{
				string desc = GetDeviceRegistryPropertyString(deviceInfoSetHandle, &deviceInfoData, SPDRP_DEVICEDESC);
				if (!desc.Contains("High Definition Audio Controller", StringComparison.OrdinalIgnoreCase)) continue;
			}
			else if (type == DeviceType.NIC)
			{
				if (!string.Equals(enumerator, "PCI", StringComparison.OrdinalIgnoreCase) &&
					!string.Equals(enumerator, "USB", StringComparison.OrdinalIgnoreCase)) continue;
			}

			string pnpDeviceId = string.Empty;
			uint requiredSize = 0;
			PInvoke.SetupDiGetDeviceInstanceId(deviceInfoSetHandle, &deviceInfoData, null, 0, &requiredSize);
			if (requiredSize > 0)
			{
				if (requiredSize <= MAX_DEVICE_ID_LEN)
				{
					if (PInvoke.SetupDiGetDeviceInstanceId(deviceInfoSetHandle, &deviceInfoData, pIdBuffer, MAX_DEVICE_ID_LEN, null))
						pnpDeviceId = new string(pIdBuffer).TrimEnd('\0');
				}
				else
				{
					char[] heapBuffer = new char[requiredSize];
					fixed (char* pHeap = heapBuffer)
						if (PInvoke.SetupDiGetDeviceInstanceId(deviceInfoSetHandle, &deviceInfoData, pHeap, requiredSize, null))
							pnpDeviceId = new string(pHeap).TrimEnd('\0');
				}
			}

			bool supportsIrq = false;
			fixed (char* pId = pnpDeviceId)
			{
				if (PInvoke.CM_Locate_DevNode(out uint devInst, pId, CM_LOCATE_DEVNODE_FLAGS.CM_LOCATE_DEVNODE_NORMAL) == CONFIGRET.CR_SUCCESS)
				{
					foreach (CM_LOG_CONF confType in new[] { CM_LOG_CONF.ALLOC_LOG_CONF, CM_LOG_CONF.FILTERED_LOG_CONF, CM_LOG_CONF.BASIC_LOG_CONF })
					{
						if (PInvoke.CM_Get_First_Log_Conf(out nuint logConf, devInst, confType) == CONFIGRET.CR_SUCCESS)
						{
							try
							{
								if (PInvoke.CM_Get_Next_Res_Des(out nuint resDes, logConf, (CM_RESTYPE)4, out _, 0) == CONFIGRET.CR_SUCCESS)
								{
									PInvoke.CM_Free_Res_Des_Handle(resDes);
									supportsIrq = true;
								}
							}
							finally
							{
								PInvoke.CM_Free_Log_Conf_Handle(logConf);
							}
						}
					}
				}
			}

			string vendorId = string.Empty;
			string deviceId = string.Empty;

			if (pnpDeviceId.Contains("VEN_"))
				vendorId = pnpDeviceId.Substring(pnpDeviceId.IndexOf("VEN_") + 4, 4).ToLowerInvariant();
			else if (pnpDeviceId.Contains("VID_"))
				vendorId = pnpDeviceId.Substring(pnpDeviceId.IndexOf("VID_") + 4, 4).ToLowerInvariant();

			if (pnpDeviceId.Contains("DEV_"))
				deviceId = pnpDeviceId.Substring(pnpDeviceId.IndexOf("DEV_") + 4, 4).ToLowerInvariant();
			else if (pnpDeviceId.Contains("PID_"))
				deviceId = pnpDeviceId.Substring(pnpDeviceId.IndexOf("PID_") + 4, 4).ToLowerInvariant();
			string registryPath = $@"SYSTEM\CurrentControlSet\Control\Class\{GetDeviceRegistryPropertyString(deviceInfoSetHandle, &deviceInfoData, SPDRP_DRIVER)}";

			string? driverVersion = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(registryPath)?.GetValue("DriverVersion")?.ToString();

			uint msiSupported = 2, msiLimit = 0, devicePolicy = 0, devicePriority = 0;
			ulong assignmentSetOverride = 0;

			using (RegistryKey? deviceRegKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey($@"SYSTEM\CurrentControlSet\Enum\{pnpDeviceId}\Device Parameters", false))
			{
				if (deviceRegKey != null)
				{
					using (RegistryKey? msiKeySub = deviceRegKey.OpenSubKey(@"Interrupt Management\MessageSignaledInterruptProperties"))
						if (msiKeySub != null)
						{
							msiSupported = Convert.ToUInt32(msiKeySub.GetValue("MSISupported") ?? 2);
							msiLimit = Convert.ToUInt32(msiKeySub.GetValue("MessageNumberLimit") ?? 0);
						}

					using RegistryKey? affinityKey = deviceRegKey.OpenSubKey(@"Interrupt Management\Affinity Policy");
					if (affinityKey != null)
					{
						devicePolicy = Convert.ToUInt32(affinityKey.GetValue("DevicePolicy") ?? 0);
						devicePriority = Convert.ToUInt32(affinityKey.GetValue("DevicePriority") ?? 0);
						if ((object?)affinityKey.GetValue("AssignmentSetOverride") is byte[] bytes && bytes.Length > 0)
						{
							byte[] full = new byte[8];
							Array.Copy(bytes, full, Math.Min(bytes.Length, 8));
							assignmentSetOverride = BitConverter.ToUInt64(full, 0);
						}
					}

				}
			}

			uint maxMsiLimit = 0;
			DEVPROPKEY msiPropKey = PInvoke.DEVPKEY_PciDevice_InterruptMessageMaximum;
			Windows.Win32.Devices.Properties.DEVPROPTYPE propertyType;
			byte[] msiBuffer = new byte[4];
			fixed (byte* pMsiBuf = msiBuffer)
			{
				if (PInvoke.SetupDiGetDeviceProperty(deviceInfoSetHandle, &deviceInfoData, &msiPropKey, &propertyType, pMsiBuf, (uint)msiBuffer.Length, &requiredSize, 0))
					if (requiredSize >= 4) maxMsiLimit = BitConverter.ToUInt32(msiBuffer, 0);
			}

			NicDriverType nicDriverType = NicDriverType.NDIS;
			NicDeviceType nicDeviceType = NicDeviceType.Virtual;
			bool isActive = false;

			XhciDeviceType xhciType = XhciDeviceType.Hub;
			ulong baseAddress = 0;

			if (type == DeviceType.NIC)
			{
				using RegistryKey? classKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(registryPath);
				nicDeviceType = classKey?.GetValue("*PhysicalMediaType")?.ToString() switch
				{
					"9" => NicDeviceType.WiFi,
					"14" => NicDeviceType.LAN,
					_ => NicDeviceType.Virtual
				};
				nicDriverType = GetNicDriverType(classKey) ? NicDriverType.NetAdapterCx : NicDriverType.NDIS;

				isActive = GetActive(pnpDeviceId);
			}
			else if (type == DeviceType.XHCI)
			{
				xhciType = XhciDeviceType.Controller;
				baseAddress = GetDeviceBaseAddress(pnpDeviceId) ?? 0;
			}

			var device = new DeviceInfo
			{
				FriendlyName = GetDeviceRegistryPropertyString(deviceInfoSetHandle, &deviceInfoData, SPDRP_FRIENDLYNAME),
				DeviceDescription = GetDeviceRegistryPropertyString(deviceInfoSetHandle, &deviceInfoData, SPDRP_DEVICEDESC),
				PnpDeviceId = pnpDeviceId,
				VendorId = vendorId,
				DeviceId = deviceId,
				Location = GetDeviceRegistryPropertyString(deviceInfoSetHandle, &deviceInfoData, SPDRP_LOCATION_INFORMATION),
				RegistryPath = registryPath,
				State = GetDeviceState(pnpDeviceId),
				DevObjName = GetDeviceRegistryPropertyString(deviceInfoSetHandle, &deviceInfoData, SPDRP_PHYSICAL_DEVICE_OBJECT_NAME),
				MsiSupported = msiSupported,
				MsiLimit = msiLimit,
				MaxMsiLimit = maxMsiLimit,
				DevicePolicy = devicePolicy,
				DevicePriority = devicePriority,
				AssignmentSetOverride = assignmentSetOverride,
				SupportsIrq = supportsIrq,
				DriverType = nicDriverType,
				NicType = nicDeviceType,
				IsActive = isActive,
				XhciType = xhciType,
				DeviceType = type,
				BaseAddress = baseAddress,
				CurrentVersion = driverVersion ?? string.Empty
			};

			if (device.State != DeviceState.Disabled)
				devices.Add(device);
		}

		PInvoke.SetupDiDestroyDeviceInfoList(deviceInfoSetHandle);
		return devices;
	}

	private unsafe static DeviceState GetDeviceState(string pnpDeviceId)
	{
		fixed (char* pId = pnpDeviceId)
		{
			if (PInvoke.CM_Locate_DevNode(out uint devInst, pId, CM_LOCATE_DEVNODE_FLAGS.CM_LOCATE_DEVNODE_NORMAL) == CONFIGRET.CR_SUCCESS)
			{
				if (PInvoke.CM_Get_DevNode_Status(out CM_DEVNODE_STATUS_FLAGS status, out CM_PROB prob, devInst, 0) == CONFIGRET.CR_SUCCESS)
				{
					if ((status & CM_DEVNODE_STATUS_FLAGS.DN_STARTED) != 0)
						return DeviceState.Enabled;

					if ((status & CM_DEVNODE_STATUS_FLAGS.DN_HAS_PROBLEM) != 0)
						return DeviceState.Error;

					return DeviceState.Disabled;
				}
			}
		}
		return DeviceState.Error;
	}

	private unsafe static string GetDeviceRegistryPropertyString(HDEVINFO deviceInfoSet, SP_DEVINFO_DATA* deviceInfoData, SETUP_DI_REGISTRY_PROPERTY property)
	{
		uint requiredSize = 0;
		uint regType;

		PInvoke.SetupDiGetDeviceRegistryProperty(deviceInfoSet, deviceInfoData, property, &regType, null, 0, &requiredSize);

		if (requiredSize == 0) return string.Empty;

		byte[] buffer = new byte[requiredSize];
		fixed (byte* pBuffer = buffer)
		{
			if (PInvoke.SetupDiGetDeviceRegistryProperty(deviceInfoSet, deviceInfoData, property, &regType, pBuffer, requiredSize, null))
			{
				return System.Text.Encoding.Unicode.GetString(buffer).TrimEnd('\0');
			}
		}
		return string.Empty;
	}

	public static void SetMSIMode(string pnpDeviceId, bool msiSupported, uint msiLimit)
	{
		using RegistryKey interruptKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey($@"SYSTEM\CurrentControlSet\Enum\{pnpDeviceId}\Device Parameters", true)!.CreateSubKey("Interrupt Management");

		if (msiSupported)
		{
			using RegistryKey msiKey = interruptKey.CreateSubKey("MessageSignaledInterruptProperties");

			msiKey.SetValue("MSISupported", 1, RegistryValueKind.DWord);

			if (msiLimit == 0)
				msiKey.DeleteValue("MessageNumberLimit", false);
			else
				msiKey.SetValue("MessageNumberLimit", msiLimit, RegistryValueKind.DWord);
		}
		else
		{
			interruptKey?.DeleteSubKeyTree("MessageSignaledInterruptProperties", false);
		}
	}

	public static void SetAffinityPolicy(string pnpDeviceId, uint devicePolicy, uint devicePriority, ulong assignmentSetOverride)
	{
		using RegistryKey? devParamsKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey($@"SYSTEM\CurrentControlSet\Enum\{pnpDeviceId}\Device Parameters", true);
		if (devParamsKey == null) return;

		if (devicePolicy == 0 && devicePriority == 0 && assignmentSetOverride == 0)
		{
			devParamsKey.OpenSubKey("Interrupt Management", true)?.DeleteSubKeyTree("Affinity Policy", false);
			return;
		}

		using RegistryKey affinityKey = devParamsKey.CreateSubKey("Interrupt Management").CreateSubKey("Affinity Policy");
		affinityKey.SetValue("DevicePolicy", devicePolicy, RegistryValueKind.DWord);

		if (devicePolicy == 4)
		{
			byte[] bytes = BitConverter.GetBytes(assignmentSetOverride);
			int length = bytes.Length;
			while (length > 1 && bytes[length - 1] == 0) length--;

			byte[] trimmed = new byte[length];
			Array.Copy(bytes, trimmed, length);
			affinityKey.SetValue("AssignmentSetOverride", trimmed, RegistryValueKind.Binary);
		}
		else
			affinityKey.DeleteValue("AssignmentSetOverride", false);

		if (devicePriority == 0)
			affinityKey.DeleteValue("DevicePriority", false);
		else
			affinityKey.SetValue("DevicePriority", devicePriority, RegistryValueKind.DWord);
	}

	public static ApplyResult ApplySettingsToDevices(List<DeviceInfo> devices, bool msiSupported, uint msiLimit, uint devicePolicy, uint devicePriority, ulong assignmentSetOverride, DeviceType deviceType = DeviceType.GPU)
	{
		var result = new ApplyResult();

		foreach (DeviceInfo device in devices)
		{
			bool changed = false;

			if (device.MsiSupported != (msiSupported ? 1u : 0u) || device.MsiLimit != msiLimit)
			{
				SetMSIMode(device.PnpDeviceId, msiSupported, msiLimit);
				device.MsiSupported = msiSupported ? 1u : 0u;
				device.MsiLimit = msiLimit;
				changed = true;
			}

			if (device.DevicePolicy != devicePolicy || device.DevicePriority != devicePriority || device.AssignmentSetOverride != ((devicePolicy == 4) ? assignmentSetOverride : 0))
			{
				SetAffinityPolicy(device.PnpDeviceId, devicePolicy, devicePriority, (devicePolicy == 4) ? assignmentSetOverride : 0);
				device.DevicePolicy = devicePolicy;
				device.DevicePriority = devicePriority;
				device.AssignmentSetOverride = (devicePolicy == 4) ? assignmentSetOverride : 0;
				changed = true;
			}

			if (changed)
			{
				if (deviceType == DeviceType.NIC && device.DriverType == NicDriverType.NDIS && ((devicePolicy == 4) ? assignmentSetOverride : 0) != 0)
					SetRSS(device, (devicePolicy == 4) ? assignmentSetOverride : 0);

				result.ChangedDevices.Add(device);
				result.AppliedSettings[device.PnpDeviceId] = device;
			}
		}

		result.Success = result.ChangedDevices.Count > 0;
		result.NeedsRestart = result.Success;
		return result;
	}

	public static void SetRSS(DeviceInfo device, ulong assignmentSetOverride)
	{
		using RegistryKey? classKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(device.RegistryPath, true);
		if (classKey?.GetValue("*PhysicalMediaType")?.ToString() != "14") return;

		if (device.DevicePolicy == 4 && assignmentSetOverride != 0)
		{
			var threads = Enumerable.Range(0, 64).Where(i => (assignmentSetOverride & (1UL << i)) != 0).ToList();
			if (threads.Count == 0) return;

			classKey.SetValue("*RSS", "1", RegistryValueKind.String);
			classKey.SetValue("*RssBaseProcGroup", "0", RegistryValueKind.String);
			classKey.SetValue("*RssBaseProcNumber", threads.Min().ToString(), RegistryValueKind.String);
			classKey.SetValue("*MaxProcessors", threads.Count.ToString(), RegistryValueKind.String);
		}
		else
		{
			foreach (string? key in new[] { "*RssBaseProcGroup", "*RssBaseProcNumber", "*MaxProcessors" })
				classKey.DeleteValue(key, false);
		}
	}

	private static bool GetNicDriverType(RegistryKey? classKey)
	{
		if (classKey is null) return false;

		using RegistryKey? ndiKey = classKey.OpenSubKey("Ndi");
		string? serviceName = ndiKey?.GetValue("Service")?.ToString()?.TrimEnd('.');
		if (string.IsNullOrEmpty(serviceName)) return false;

		using RegistryKey? serviceKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey($@"SYSTEM\CurrentControlSet\Services\{serviceName}");
		if (serviceKey?.GetValue("ImagePath") is not string imagePath) return false;

		string systemRoot = Environment.GetEnvironmentVariable("SystemRoot")!;
		string resolved = Environment.ExpandEnvironmentVariables(imagePath.StartsWith(@"\??\") ? imagePath[4..] : imagePath);

		resolved = resolved.StartsWith(@"\SystemRoot", StringComparison.OrdinalIgnoreCase)
			? resolved.Replace(@"\SystemRoot", systemRoot, StringComparison.OrdinalIgnoreCase)
			: resolved.StartsWith("System32", StringComparison.OrdinalIgnoreCase)
				? Path.Combine(systemRoot, resolved)
				: resolved;

		if (!File.Exists(resolved)) return false;

		return System.Text.Encoding.ASCII.GetString(File.ReadAllBytes(resolved)).Contains("NetAdapter", StringComparison.OrdinalIgnoreCase);
	}

	private unsafe static bool GetActive(string pnpDeviceId)
	{
		uint size = 15000;
		byte[] buffer = new byte[size];
		fixed (byte* pBuf = buffer)
		{
			if (PInvoke.GetAdaptersAddresses(0, GET_ADAPTERS_ADDRESSES_FLAGS.GAA_FLAG_SKIP_ANYCAST, null, (IP_ADAPTER_ADDRESSES_LH*)pBuf, &size) != 0) return false;

			for (var p = (IP_ADAPTER_ADDRESSES_LH*)pBuf; p != null; p = p->Next)
			{
				if (p->IfType == 24 || p->OperStatus != IF_OPER_STATUS.IfOperStatusUp) continue;

				string guid = new((sbyte*)p->AdapterName.Value);
				using RegistryKey? netKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey($@"SYSTEM\CurrentControlSet\Control\Network\{{4D36E972-E325-11CE-BFC1-08002BE10318}}\{guid}\Connection");
				if (string.Equals(netKey?.GetValue("PnpInstanceID")?.ToString(), pnpDeviceId, StringComparison.OrdinalIgnoreCase)) return true;
			}
		}
		return false;
	}

	public unsafe static bool RestartDevice(DeviceInfo device)
	{
		using SetupDiDestroyDeviceInfoListSafeHandle hDevInfo = PInvoke.SetupDiCreateDeviceInfoList(null, default);
		if (hDevInfo.IsInvalid) return false;

		SP_DEVINFO_DATA devData = new() { cbSize = (uint)sizeof(SP_DEVINFO_DATA) };

		if (!PInvoke.SetupDiOpenDeviceInfo(hDevInfo, device.PnpDeviceId, HWND.Null, 0, ref devData))
			return false;

		var params_ = new SP_PROPCHANGE_PARAMS
		{
			ClassInstallHeader = new()
			{
				cbSize = (uint)sizeof(SP_CLASSINSTALL_HEADER),
				InstallFunction = DI_FUNCTION.DIF_PROPERTYCHANGE
			},
			StateChange = SETUP_DI_STATE_CHANGE.DICS_PROPCHANGE,
			Scope = SETUP_DI_PROPERTY_CHANGE_SCOPE.DICS_FLAG_CONFIGSPECIFIC
		};

		HDEVINFO hDev = (HDEVINFO)hDevInfo.DangerousGetHandle();

		if (PInvoke.SetupDiSetClassInstallParams(hDev, &devData, (SP_CLASSINSTALL_HEADER*)&params_, (uint)sizeof(SP_PROPCHANGE_PARAMS)))
			return PInvoke.SetupDiCallClassInstaller(DI_FUNCTION.DIF_PROPERTYCHANGE, hDev, &devData);

		return false;
	}

	public static async Task<RestartResult> RestartDevicesAsync(List<DeviceInfo> devices)
	{
		var result = new RestartResult();
		int successCount = 0;
		int failedCount = 0;
		var failedDevices = new ConcurrentBag<string>();

		IEnumerable<Task> tasks = devices.Select(async device =>
		{
			bool success = await Task.Run(() => RestartDevice(device));
			if (success)
				Interlocked.Increment(ref successCount);
			else
			{
				Interlocked.Increment(ref failedCount);
				failedDevices.Add(device.DeviceDescription);
			}
		});

		await Task.WhenAll(tasks);

		result.SuccessCount = successCount;
		result.FailedCount = failedCount;
		result.FailedDevices = [.. failedDevices];

		return result;
	}

	public unsafe static string? GetParentPnpId(string pnpDeviceId)
	{
		fixed (char* pId = pnpDeviceId)
		{
			if (PInvoke.CM_Locate_DevNode(out uint devInst, pId, 0) == CONFIGRET.CR_SUCCESS)
			{
				if (PInvoke.CM_Get_Parent(out uint parentHandle, devInst, 0) == CONFIGRET.CR_SUCCESS)
				{
					const int MAX_DEVICE_ID_LEN = 256;
					char* pBuffer = stackalloc char[MAX_DEVICE_ID_LEN];

					if (PInvoke.CM_Get_Device_IDW(parentHandle, pBuffer, MAX_DEVICE_ID_LEN, 0) == CONFIGRET.CR_SUCCESS)
					{
						return new string(pBuffer);
					}
				}
			}
		}
		return null;
	}

	private unsafe static ulong? GetDeviceBaseAddress(string pnpDeviceId)
	{
		fixed (char* pId = pnpDeviceId)
		{
			if (PInvoke.CM_Locate_DevNode(out uint devInst, pId, CM_LOCATE_DEVNODE_FLAGS.CM_LOCATE_DEVNODE_NORMAL) != CONFIGRET.CR_SUCCESS)
				return null;

			if (PInvoke.CM_Get_First_Log_Conf(out nuint logConf, devInst, CM_LOG_CONF.ALLOC_LOG_CONF) != CONFIGRET.CR_SUCCESS && PInvoke.CM_Get_First_Log_Conf(out logConf, devInst, CM_LOG_CONF.BOOT_LOG_CONF) != CONFIGRET.CR_SUCCESS)
				return null;

			try
			{
				nuint resDes = 0;
				nuint currentHandle = logConf;
				bool isFirst = true;
				uint resType = 0;

				while (true)
				{
					CONFIGRET result = PInvoke.CM_Get_Next_Res_Des(out nuint nextResDes, currentHandle, 0, out CM_RESTYPE outResType, 0);
					resType = (uint)outResType;

					if (!isFirst && resDes != 0)
						PInvoke.CM_Free_Res_Des_Handle(resDes);

					if (result != CONFIGRET.CR_SUCCESS)
						break;

					resDes = nextResDes;
					isFirst = false;
					currentHandle = resDes;

					if (resType == 1 || resType == 7)
					{
						uint dataSize;
						if (PInvoke.CM_Get_Res_Des_Data_Size(&dataSize, resDes, 0) == CONFIGRET.CR_SUCCESS && dataSize >= 24)
						{
							byte[] data = new byte[dataSize];
							fixed (byte* pData = data)
							{
								if (PInvoke.CM_Get_Res_Des_Data(resDes, pData, dataSize, 0) == CONFIGRET.CR_SUCCESS)
								{
									ulong addr = BitConverter.ToUInt64(data, 8);
									PInvoke.CM_Free_Res_Des_Handle(resDes);
									return addr;
								}
							}
						}
					}
				}

				if (resDes != 0)
					PInvoke.CM_Free_Res_Des_Handle(resDes);
			}
			finally
			{
				PInvoke.CM_Free_Log_Conf_Handle(logConf);
			}
		}
		return null;
	}

	private static bool TryGetRuntimeOffset(ReadWriteHelper? hw, DeviceInfo device, out ulong runtime, out int max)
	{
		runtime = 0;
		max = 0;

		if (hw is null) return false;

		if (device.BaseAddress == 0) return false;

		if (!hw.ReadMemory32(device.BaseAddress + 0x18, out uint rtsoff)) return false;
		if (rtsoff == 0xFFFFFFFF || rtsoff == 0) return false;

		runtime = device.BaseAddress + (rtsoff & ~0x1Fu);

		if (!hw.ReadMemory32(device.BaseAddress + 0x04, out uint hcs1)) return false;
		if (hcs1 == 0xFFFFFFFF) return false;

		max = (int)((hcs1 >> 8) & 0x7FF);
		return max <= 128;
	}

	public static bool GetIMODState(DeviceInfo device, ReadWriteHelper? sharedHw = null)
	{
		using ReadWriteHelper? localHw = sharedHw == null ? new ReadWriteHelper() : null;
		ReadWriteHelper? hw = sharedHw ?? localHw;

		if (!TryGetRuntimeOffset(hw, device, out ulong runtime, out int max)) return false;

		for (int i = 0; i < max; i++)
		{
			if (!hw!.ReadMemory32(runtime + 0x24 + (0x20 * (ulong)i), out uint imod)) return false;
			if (imod != 0) return true;
		}

		return false;
	}

	public static void ToggleImod(DeviceInfo device, bool enable)
	{
		using var hw = new ReadWriteHelper();

		if (!TryGetRuntimeOffset(hw, device, out ulong runtime, out int max)) return;

		string? json = ApplicationData.Current.LocalSettings.Values["XHCIs"]?.ToString();
		JsonArray? array = !string.IsNullOrEmpty(json) ? (JsonNode.Parse(json)?.AsArray() ?? []) : [];

		JsonObject? obj = null;
		foreach (JsonNode? item in array)
		{
			if (item?["PnpDeviceId"]?.ToString() == device.PnpDeviceId)
			{
				obj = item!.AsObject();
				break;
			}
		}

		if (obj == null)
		{
			obj = new JsonObject { ["PnpDeviceId"] = device.PnpDeviceId };
			array.Add((JsonNode)obj);
		}

		obj["IsActive"] = enable;
		ApplicationData.Current.LocalSettings.Values["XHCIs"] = array.ToJsonString();

		if (enable)
		{
			JsonObject intervals = (obj["Intervals"]?.AsObject()) ?? throw new Exception("No saved intervals found.");
			foreach (KeyValuePair<string, JsonNode?> kvp in intervals)
				if (ulong.TryParse(kvp.Key, out ulong addr) && uint.TryParse(kvp.Value?.ToString(), out uint val))
				{
					if (!hw.WriteMemory32(addr, val))
						throw new Exception("Failed to write memory address 0x" + addr.ToString("X"));
				}
		}
		else
		{
			SaveImod(device, hw);
			for (int i = 0; i < max; i++)
			{
				if (!hw.WriteMemory32(runtime + 0x24 + (0x20 * (ulong)i), 0))
					throw new Exception("Failed to write memory address 0x" + (runtime + 0x24 + (0x20 * (ulong)i)).ToString("X"));
			}
		}
	}

	public static void SaveImod(DeviceInfo device, ReadWriteHelper? sharedHw = null)
	{
		using ReadWriteHelper? localHw = sharedHw == null ? new ReadWriteHelper() : null;
		ReadWriteHelper? hw = sharedHw ?? localHw;

		if (!GetIMODState(device, hw)) return;
		if (!TryGetRuntimeOffset(hw, device, out ulong runtime, out int max)) return;

		var intervals = new JsonObject();
		for (int i = 0; i < max; i++)
		{
			ulong addr = runtime + 0x24 + (0x20 * (ulong)i);
			if (!hw!.ReadMemory32(addr, out uint val)) return;
			intervals[addr.ToString()] = val;
		}

		string? json = ApplicationData.Current.LocalSettings.Values["XHCIs"]?.ToString();
		JsonArray? array = !string.IsNullOrEmpty(json) ? (JsonNode.Parse(json)?.AsArray() ?? []) : [];

		JsonObject? obj = null;
		foreach (JsonNode? node in array)
		{
			if (node?["PnpDeviceId"]?.ToString() == device.PnpDeviceId)
			{
				obj = node!.AsObject();
				break;
			}
		}

		if (obj == null)
		{
			obj = new JsonObject { ["PnpDeviceId"] = device.PnpDeviceId };
			array.Add((JsonNode)obj);
		}

		obj["Intervals"] = intervals;
		ApplicationData.Current.LocalSettings.Values["XHCIs"] = array.ToJsonString();
	}
}
