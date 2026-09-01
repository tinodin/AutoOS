using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json.Nodes;
using AutoOS.Core.Data.Models.Device;
using AutoOS.Core.Data.Clients.Sound;
using AutoOS.Core.Data.Models.Sound;
using Windows.Storage;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Media.Audio;
using Windows.Win32.Media.Audio.Endpoints;
using Windows.Win32.System.Com;
using Windows.Win32.System.Com.StructuredStorage;
using Windows.Win32.System.Variant;
using Windows.Win32.UI.Shell.PropertiesSystem;

namespace AutoOS.Core.Helpers.Sound;

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct IPolicyConfigVtbl
{
	public delegate* unmanaged[Stdcall]<void*, Guid*, void**, int> QueryInterface;
	public delegate* unmanaged[Stdcall]<void*, uint> AddRef;
	public delegate* unmanaged[Stdcall]<void*, uint> Release;
	public delegate* unmanaged[Stdcall]<void*, char*, void**, int> GetMixFormat;
	public delegate* unmanaged[Stdcall]<void*, char*, int, void**, int> GetDeviceFormat;
	public delegate* unmanaged[Stdcall]<void*, char*, int> ResetDeviceFormat;
	public delegate* unmanaged[Stdcall]<void*, char*, void*, void*, int> SetDeviceFormat;
}

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct IPolicyConfigNativeOut
{
	public IPolicyConfigVtbl* Vtbl;
}

public static partial class SoundHelper
{
	private static readonly ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
	private static readonly ConcurrentDictionary<string, object> Observers = new();

	private static readonly Guid KSDATAFORMAT_SUBTYPE_PCM = new("00000001-0000-0010-8000-00aa00389b71");
	private static readonly Guid KSDATAFORMAT_SUBTYPE_IEEE_FLOAT = new("00000003-0000-0010-8000-00aa00389b71");

	public static unsafe AudioDetails GetAudioDetails(DeviceInfo device)
	{
		PInvoke.CoInitializeEx(null, COINIT.COINIT_MULTITHREADED);
		var details = new AudioDetails();

		HRESULT hrEnum = PInvoke.CoCreateInstance<IMMDeviceEnumerator>(typeof(MMDeviceEnumerator).GUID, null, (CLSCTX)7, out IMMDeviceEnumerator* pEnumerator);
		if (hrEnum.Succeeded)
		{
			IMMDeviceEnumerator* enumerator = pEnumerator;
			IMMDevice* endpoint = null;
			fixed (char* pId = device.RegistryPath)
			{
				enumerator->GetDevice(pId, &endpoint);
			}

			if (endpoint != null)
			{
				Guid volId = typeof(IAudioEndpointVolume).GUID;
				endpoint->Activate(volId, (CLSCTX)7, null, out void* pVolume);
				if (pVolume != null)
				{
					var endpointVolume = (IAudioEndpointVolume*)pVolume;
					endpointVolume->GetMasterVolumeLevelScalar(out float vol);
					details.CurrentVolume = MathF.Round(vol * 100f);

					endpointVolume->GetChannelCount(out uint channelCount);
					float left = 0, right = 0;
					if (channelCount >= 2)
					{
						try
						{
							endpointVolume->GetChannelVolumeLevelScalar(0, out left);
							endpointVolume->GetChannelVolumeLevelScalar(1, out right);
						}
						catch (COMException ex) when (ex.HResult == unchecked((int)0x80070057))
						{
							details.SupportPerChannelVolume = false;
						}

						details.LeftVolume = float.IsFinite(left) ? MathF.Round(left * 100f) : 100f;
						details.RightVolume = float.IsFinite(right) ? MathF.Round(right * 100f) : 100f;
					}
					else
					{
						details.SupportPerChannelVolume = false;
						details.LeftVolume = details.CurrentVolume;
						details.RightVolume = details.CurrentVolume;
					}

					endpointVolume->GetMute(out BOOL muted);
					details.IsMuted = (bool)muted;
					endpointVolume->Release();
				}

				IPropertyStore* store = null;
				endpoint->OpenPropertyStore((STGM)0, &store);
				if (store != null)
				{
					PROPERTYKEY keyDeviceFormat = new() { fmtid = new Guid("F19F064D-082C-4E27-BC73-6882A1BB8E4C"), pid = 0 };
					PROPVARIANT prop = default;
					store->GetValue(&keyDeviceFormat, &prop);
					if (prop.Anonymous.Anonymous.vt == VARENUM.VT_BLOB)
					{
						var waveFormat = (WAVEFORMATEX*)prop.Anonymous.Anonymous.Anonymous.blob.pBlobData;
						details.CurrentSampleRate = waveFormat->nSamplesPerSec;
						details.CurrentBitDepth = waveFormat->wBitsPerSample;
						details.CurrentChannels = waveFormat->nChannels;
						if (waveFormat->wFormatTag == 0xFFFE)
						{
							var extDevice = (WAVEFORMATEXTENSIBLE*)waveFormat;
							details.CurrentBitDepth = extDevice->Samples.wValidBitsPerSample;
						}
					}
					PInvoke.PropVariantClear(&prop);

					PROPERTYKEY keyFormFactor = new() { fmtid = new Guid("1da5d803-d492-4edd-8c23-e0c0ffee7f0e"), pid = 0 };
					PROPVARIANT formFactorProp = default;
					store->GetValue(&keyFormFactor, &formFactorProp);
					device.FormFactor = formFactorProp.Anonymous.Anonymous.Anonymous.ulVal;
					PInvoke.PropVariantClear(&formFactorProp);

					store->Release();
				}

				Guid clientId = typeof(IAudioClient3).GUID;
				endpoint->Activate(clientId, (CLSCTX)7, null, out void* pAudioClient);
				if (pAudioClient != null)
				{
					var audioClient = (IAudioClient3*)pAudioClient;
					uint[] testRates = [8000, 16000, 22050, 32000, 44100, 48000, 88200, 96000, 176400, 192000, 352800, 384000];
					ushort[] testBits = [16, 24, 32];
					ushort[] testChannels =
						details.CurrentChannels > 0
							? [(ushort)1, (ushort)2, details.CurrentChannels]
							: [(ushort)1, (ushort)2];

					var formats = new List<AudioFormatOption>();
					foreach (ushort ch in testChannels.Distinct())
					{
						foreach (uint rate in testRates)
						{
							foreach (ushort bit in testBits)
							{
								bool isSupported = false;
								ushort actualBits = bit;
								Guid subFmt = KSDATAFORMAT_SUBTYPE_PCM;

								if (bit == 16)
								{
									WAVEFORMATEXTENSIBLE fmt = CreateWaveFormat(rate, 16, ch, 16, KSDATAFORMAT_SUBTYPE_PCM);
									if (((HRESULT)audioClient->IsFormatSupported(AUDCLNT_SHAREMODE.AUDCLNT_SHAREMODE_EXCLUSIVE, (WAVEFORMATEX*)&fmt, null)).Value == 0)
										isSupported = true;
								}
								else if (bit == 24)
								{
									WAVEFORMATEXTENSIBLE fmt24 = CreateWaveFormat(rate, 24, ch, 24, KSDATAFORMAT_SUBTYPE_PCM);
									if (((HRESULT)audioClient->IsFormatSupported(AUDCLNT_SHAREMODE.AUDCLNT_SHAREMODE_EXCLUSIVE, (WAVEFORMATEX*)&fmt24, null)).Value == 0)
									{
										isSupported = true;
									}
									else
									{
										WAVEFORMATEXTENSIBLE fmt24_32 = CreateWaveFormat(rate, 32, ch, 24, KSDATAFORMAT_SUBTYPE_PCM);
										if (((HRESULT)audioClient->IsFormatSupported(AUDCLNT_SHAREMODE.AUDCLNT_SHAREMODE_EXCLUSIVE, (WAVEFORMATEX*)&fmt24_32, null)).Value == 0)
										{
											isSupported = true;
											actualBits = 32;
										}
									}
								}
								else if (bit == 32)
								{
									WAVEFORMATEXTENSIBLE fmt32 = CreateWaveFormat(rate, 32, ch, 32, KSDATAFORMAT_SUBTYPE_PCM);
									if (((HRESULT)audioClient->IsFormatSupported(AUDCLNT_SHAREMODE.AUDCLNT_SHAREMODE_EXCLUSIVE, (WAVEFORMATEX*)&fmt32, null)).Value == 0)
									{
										isSupported = true;
									}
									else
									{
										WAVEFORMATEXTENSIBLE fmt32f = CreateWaveFormat(rate, 32, ch, 32, KSDATAFORMAT_SUBTYPE_IEEE_FLOAT);
										if (((HRESULT)audioClient->IsFormatSupported(AUDCLNT_SHAREMODE.AUDCLNT_SHAREMODE_EXCLUSIVE, (WAVEFORMATEX*)&fmt32f, null)).Value == 0)
										{
											isSupported = true;
											subFmt = KSDATAFORMAT_SUBTYPE_IEEE_FLOAT;
										}
									}
								}

								if (isSupported)
								{
									string quality = GetQualityLabel(bit, rate);
									formats.Add(new AudioFormatOption
									{
										SampleRate = rate,
										Bits = bit,
										Channels = ch,
										ActualBitsPerSample = actualBits,
										SubFormat = subFmt,
										DisplayName = $"{ch} channels, {bit} bit, {rate} Hz{quality}",
										IsCurrent = rate == details.CurrentSampleRate && bit == details.CurrentBitDepth && ch == details.CurrentChannels
									});
								}
							}
						}
					}

					if (formats.Count == 0)
					{
						foreach (ushort ch in testChannels.Distinct())
						{
							foreach (uint rate in testRates)
							{
								foreach (ushort bit in testBits)
								{
									string quality = GetQualityLabel(bit, rate);
									formats.Add(new AudioFormatOption
									{
										SampleRate = rate,
										Bits = bit,
										Channels = ch,
										ActualBitsPerSample = bit,
										SubFormat = KSDATAFORMAT_SUBTYPE_PCM,
										DisplayName = $"{ch} channels, {bit} bit, {rate} Hz{quality}",
										IsCurrent = rate == details.CurrentSampleRate && bit == details.CurrentBitDepth && ch == details.CurrentChannels
									});
								}
							}
						}

						if (details.CurrentSampleRate > 0 && !formats.Any(f => f.IsCurrent))
						{
							string quality = GetQualityLabel(details.CurrentBitDepth, details.CurrentSampleRate);
							formats.Add(new AudioFormatOption
							{
								SampleRate = details.CurrentSampleRate,
								Bits = details.CurrentBitDepth,
								Channels = details.CurrentChannels,
								ActualBitsPerSample = details.CurrentBitDepth,
								SubFormat = KSDATAFORMAT_SUBTYPE_PCM,
								DisplayName = $"{details.CurrentChannels} channels, {details.CurrentBitDepth} bit, {details.CurrentSampleRate} Hz{quality}",
								IsCurrent = true
							});
						}
					}

					details.Formats = [.. formats
						.OrderBy(f => f.Channels)
						.ThenBy(f => f.Bits)
						.ThenBy(f => f.SampleRate)];
					audioClient->Release();
				}
				endpoint->Release();
			}
			enumerator->Release();
		}
		return details;
	}

	public static unsafe List<BufferSizeOption> GetBufferSizes(DeviceInfo device)
	{
		PInvoke.CoInitializeEx(null, COINIT.COINIT_MULTITHREADED);
		var bufferSizes = new List<BufferSizeOption>();

		HRESULT hrEnum = PInvoke.CoCreateInstance<IMMDeviceEnumerator>(typeof(MMDeviceEnumerator).GUID, null, (CLSCTX)7, out IMMDeviceEnumerator* pEnumerator);
		if (!hrEnum.Succeeded)
			return bufferSizes;

		IMMDeviceEnumerator* enumerator = pEnumerator;
		IMMDevice* endpoint = null;
		fixed (char* pId = device.RegistryPath)
		{ enumerator->GetDevice(pId, &endpoint); }

		if (endpoint == null)
		{
			enumerator->Release();
			return bufferSizes;
		}

		Guid clientId = typeof(IAudioClient3).GUID;
		endpoint->Activate(clientId, (CLSCTX)7, null, out void* pAudioClient);
		if (pAudioClient == null)
		{
			endpoint->Release();
			enumerator->Release();
			return bufferSizes;
		}

		var audioClient = (IAudioClient3*)pAudioClient;
		WAVEFORMATEX* format = null;
		WAVEFORMATEXTENSIBLE selectedFormat = default;
		bool freeFormat = false;
		WAVEFORMATEX* mixFormat = null;
		try
		{
			if (device.SelectedFormat is AudioFormatOption selected)
			{
				selectedFormat = CreateWaveFormat(selected.SampleRate, selected.Bits, selected.Channels, selected.ActualBitsPerSample, selected.SubFormat);
				format = (WAVEFORMATEX*)&selectedFormat;
			}
			else
			{
				audioClient->GetMixFormat(&format);
				freeFormat = true;
			}

			audioClient->GetMixFormat(&mixFormat);

			if (format != null && mixFormat != null)
			{
				uint current = 0;
				WAVEFORMATEX* periodFormat = null;

				if (TryGetEnginePeriod(audioClient, format, out uint def, out uint min, out uint max))
					periodFormat = format;
				else if (TryGetEnginePeriod(audioClient, mixFormat, out def, out min, out max))
					periodFormat = mixFormat;

				if (periodFormat != null)
				{
					try
					{
						audioClient->GetCurrentSharedModeEnginePeriod(out _, out current);
					}
					catch { }

					const uint step = 64;
					var options = new SortedSet<uint> { min };

					uint aligned = ((min + step - 1) / step) * step;
					for (uint frames = aligned; frames <= max; frames += step)
						options.Add(frames);

					if (max > 0)
						options.Add(max);
					if (current > 0)
						options.Add(current);

					double factor = 1000.0 / periodFormat->nSamplesPerSec;
					foreach (uint frames in options)
					{
						float ms = (float)Math.Round(frames * factor, 2);
						bufferSizes.Add(new BufferSizeOption
						{
							Frames = frames,
							Ms = ms,
							DisplayName = $"{frames} samples ({ms:0.#} ms)",
							IsCurrent = frames == current,
							IsDefault = frames == def
						});
					}
				}
			}
		}
		catch { }
		finally
		{
			if (freeFormat && format != null)
				PInvoke.CoTaskMemFree(format);
			if (mixFormat != null)
				PInvoke.CoTaskMemFree(mixFormat);
			audioClient->Release();
			endpoint->Release();
			enumerator->Release();
		}

		return [.. bufferSizes.DistinctBy(x => x.Frames)];
	}

	private static unsafe bool TryGetEnginePeriod(IAudioClient3* client, WAVEFORMATEX* format, out uint def, out uint min, out uint max)
	{
		def = 0;
		min = 0;
		max = 0;
		try
		{
			client->GetSharedModeEnginePeriod(*format, out def, out _, out min, out max);
			return min > 0;
		}
		catch
		{
			return false;
		}
	}

	public static unsafe float SetAudioVolume(DeviceInfo device, float volume)
	{
		float safeVol = float.IsFinite(volume) ? Math.Clamp(volume, 0.0f, 1.0f) : 1.0f;
		float actualVol = 1.0f;

		PInvoke.CoInitializeEx(null, COINIT.COINIT_MULTITHREADED);
		HRESULT hrEnum = PInvoke.CoCreateInstance<IMMDeviceEnumerator>(typeof(MMDeviceEnumerator).GUID, null, (CLSCTX)7, out IMMDeviceEnumerator* pEnumerator);

		if (hrEnum.Succeeded)
		{
			IMMDeviceEnumerator* enumerator = pEnumerator;
			IMMDevice* endpoint = null;
			fixed (char* pId = device.RegistryPath)
			{
				enumerator->GetDevice(pId, &endpoint);
			}

			if (endpoint != null)
			{
				Guid iid = typeof(IAudioEndpointVolume).GUID;
				endpoint->Activate(iid, (CLSCTX)7, null, out void* pVolume);
				if (pVolume != null)
				{
					var ev = (IAudioEndpointVolume*)pVolume;
					ev->SetMasterVolumeLevelScalar(safeVol, null);
					ev->GetMasterVolumeLevelScalar(out actualVol);
					ev->Release();
				}
				endpoint->Release();
			}
			enumerator->Release();
		}
		return actualVol;
	}

	public static unsafe void SetAudioMute(DeviceInfo device, bool muted)
	{
		PInvoke.CoInitializeEx(null, COINIT.COINIT_MULTITHREADED);
		HRESULT hrEnum = PInvoke.CoCreateInstance<IMMDeviceEnumerator>(typeof(MMDeviceEnumerator).GUID, null, (CLSCTX)7, out IMMDeviceEnumerator* pEnumerator);
		if (hrEnum.Succeeded)
		{
			IMMDeviceEnumerator* enumerator = pEnumerator;
			IMMDevice* endpoint = null;
			fixed (char* pId = device.RegistryPath)
			{
				enumerator->GetDevice(pId, &endpoint);
			}
			if (endpoint != null)
			{
				Guid iid = typeof(IAudioEndpointVolume).GUID;
				endpoint->Activate(iid, (CLSCTX)7, null, out void* pVolume);
				if (pVolume != null)
				{
					((IAudioEndpointVolume*)pVolume)->SetMute(muted ? (BOOL)1 : (BOOL)0, null);
					((IAudioEndpointVolume*)pVolume)->Release();
				}
				endpoint->Release();
			}
			enumerator->Release();
		}
	}

	public static unsafe void SetAudioChannelVolume(DeviceInfo device, uint channel, float volume)
	{
		float safeVol = float.IsFinite(volume) ? Math.Clamp(volume, 0.0f, 1.0f) : 0.0f;

		PInvoke.CoInitializeEx(null, COINIT.COINIT_MULTITHREADED);
		HRESULT hrEnum = PInvoke.CoCreateInstance<IMMDeviceEnumerator>(typeof(MMDeviceEnumerator).GUID, null, (CLSCTX)7, out IMMDeviceEnumerator* pEnumerator);

		if (hrEnum.Succeeded)
		{
			IMMDeviceEnumerator* enumerator = pEnumerator;
			IMMDevice* endpoint = null;
			fixed (char* pId = device.RegistryPath)
			{
				enumerator->GetDevice(pId, &endpoint);
			}

			if (endpoint != null)
			{
				Guid iid = typeof(IAudioEndpointVolume).GUID;
				endpoint->Activate(iid, (CLSCTX)7, null, out void* pVolume);

				if (pVolume != null)
				{
					var ev = (IAudioEndpointVolume*)pVolume;
					ev->GetChannelCount(out uint actualChannelCount);

					if (channel < actualChannelCount)
					{
						ev->SetChannelVolumeLevelScalar(channel, safeVol, null);
					}

					ev->Release();
				}
				endpoint->Release();
			}
			enumerator->Release();
		}
	}

	public static unsafe void SetAudioFormat(DeviceInfo device, AudioFormatOption formatOption)
	{
		uint sampleRate = formatOption.SampleRate;
		ushort bits = formatOption.ActualBitsPerSample > 0 ? formatOption.ActualBitsPerSample : formatOption.Bits;
		ushort validBits = formatOption.Bits;
		ushort channels = formatOption.Channels;
		Guid subFormat = formatOption.SubFormat != Guid.Empty ? formatOption.SubFormat : KSDATAFORMAT_SUBTYPE_PCM;

		PInvoke.CoInitializeEx(null, COINIT.COINIT_MULTITHREADED);
		Guid clsidEnum = typeof(MMDeviceEnumerator).GUID;

		if (PInvoke.CoCreateInstance<IMMDeviceEnumerator>(clsidEnum, null, CLSCTX.CLSCTX_ALL, out IMMDeviceEnumerator* pEnumerator).Value >= 0)
		{
			IMMDeviceEnumerator* enumerator = pEnumerator;
			try
			{
				IMMDevice* endpoint = null;
				fixed (char* pId = device.RegistryPath)
				{
					enumerator->GetDevice(pId, &endpoint);
				}

				if (endpoint != null)
				{
					try
					{
						IPropertyStore* store = null;
						endpoint->OpenPropertyStore((STGM)2, &store);
						if (store != null)
						{
							void* pEndpointFormat = null;
							void* pMixFormat = null;
							try
							{
								WAVEFORMATEXTENSIBLE endpointFormat = CreateWaveFormat(sampleRate, bits, channels, validBits, subFormat);
								WAVEFORMATEXTENSIBLE mixFormat = default;
								mixFormat.Format.wFormatTag = 0xFFFE;
								mixFormat.Format.nChannels = channels;
								mixFormat.Format.nSamplesPerSec = sampleRate;
								mixFormat.Format.wBitsPerSample = 32;
								mixFormat.Format.nBlockAlign = (ushort)(channels * 4);
								mixFormat.Format.nAvgBytesPerSec = sampleRate * mixFormat.Format.nBlockAlign;
								mixFormat.Format.cbSize = 22;
								mixFormat.Samples.wValidBitsPerSample = 32;
								mixFormat.dwChannelMask = channels == 1 ? 4u : (channels == 2 ? 3u : 0u);
								mixFormat.SubFormat = KSDATAFORMAT_SUBTYPE_IEEE_FLOAT;

								pEndpointFormat = (void*)Marshal.AllocCoTaskMem(sizeof(WAVEFORMATEXTENSIBLE));
								pMixFormat = (void*)Marshal.AllocCoTaskMem(sizeof(WAVEFORMATEXTENSIBLE));
								*(WAVEFORMATEXTENSIBLE*)pEndpointFormat = endpointFormat;
								*(WAVEFORMATEXTENSIBLE*)pMixFormat = mixFormat;

								Guid clsidPolicy = new("870af99c-171d-4f9e-af0d-e63df40c2bc9");
								Guid iidPolicy = new("f8679f50-850a-41cf-9c72-430f290290c8");

								void* pPolicyOut = null;
								if (PInvoke.CoCreateInstance(&clsidPolicy, null, CLSCTX.CLSCTX_ALL, &iidPolicy, &pPolicyOut).Value >= 0 && pPolicyOut != null)
								{
									try
									{
										var policy = (IPolicyConfigNativeOut*)pPolicyOut;
										fixed (char* pwzDeviceId = device.RegistryPath)
										{
											policy->Vtbl->SetDeviceFormat(pPolicyOut, pwzDeviceId, pEndpointFormat, pMixFormat);
										}
									}
									finally
									{
										((IPolicyConfigNativeOut*)pPolicyOut)->Vtbl->Release(pPolicyOut);
									}
								}

								PROPVARIANT propDev = default;
								propDev.Anonymous.Anonymous.vt = VARENUM.VT_BLOB;
								propDev.Anonymous.Anonymous.Anonymous.blob.cbSize = (uint)sizeof(WAVEFORMATEXTENSIBLE);
								propDev.Anonymous.Anonymous.Anonymous.blob.pBlobData = (byte*)pEndpointFormat;

								PROPVARIANT propMix = default;
								propMix.Anonymous.Anonymous.vt = VARENUM.VT_BLOB;
								propMix.Anonymous.Anonymous.Anonymous.blob.cbSize = (uint)sizeof(WAVEFORMATEXTENSIBLE);
								propMix.Anonymous.Anonymous.Anonymous.blob.pBlobData = (byte*)pMixFormat;

								PROPERTYKEY keyDeviceFormat = new() { fmtid = new Guid("F19F064D-082C-4E27-BC73-6882A1BB8E4C"), pid = 0 };
								PROPERTYKEY keyOemFormat = new() { fmtid = new Guid("E4870E26-3CC5-4CD2-BA46-CA0A9A70ED04"), pid = 0 };

								store->SetValue(in keyDeviceFormat, in propDev);
								store->SetValue(in keyOemFormat, in propMix);
								store->Commit();
							}
							finally
							{
								if (pEndpointFormat != null)
									Marshal.FreeCoTaskMem((IntPtr)pEndpointFormat);
								if (pMixFormat != null)
									Marshal.FreeCoTaskMem((IntPtr)pMixFormat);
								store->Release();
							}
						}
					}
					finally
					{
						endpoint->Release();
					}
				}
			}
			finally
			{
				enumerator->Release();
			}
		}
	}

	private static WAVEFORMATEXTENSIBLE CreateWaveFormat(uint rate, ushort bits, ushort channels, ushort validBits, Guid subFormat)
	{
		WAVEFORMATEXTENSIBLE format = default;
		format.Format.wFormatTag = 0xFFFE;
		format.Format.nChannels = channels;
		format.Format.nSamplesPerSec = rate;
		format.Format.cbSize = 22;
		format.dwChannelMask = channels == 1 ? 4u : (channels == 2 ? 3u : 0u);
		format.Format.wBitsPerSample = bits;
		format.Samples.wValidBitsPerSample = validBits;
		format.SubFormat = subFormat;
		format.Format.nBlockAlign = (ushort)(format.Format.nChannels * (format.Format.wBitsPerSample / 8));
		format.Format.nAvgBytesPerSec = format.Format.nSamplesPerSec * format.Format.nBlockAlign;
		return format;
	}

	public static unsafe void RegisterVolumeCallback(DeviceInfo device, Action<float, bool, float, float> onNotify)
	{
		PInvoke.CoInitializeEx(null, COINIT.COINIT_MULTITHREADED);
		Observers.TryRemove(device.RegistryPath, out object? old);
		if (old is IDisposable disp)
			disp.Dispose();

		HRESULT hrEnum = PInvoke.CoCreateInstance<IMMDeviceEnumerator>(typeof(MMDeviceEnumerator).GUID, null, (CLSCTX)7, out IMMDeviceEnumerator* pEnumerator);
		if (hrEnum.Succeeded)
		{
			IMMDeviceEnumerator* enumerator = pEnumerator;
			IMMDevice* endpoint = null;
			fixed (char* pId = device.RegistryPath)
			{
				enumerator->GetDevice(pId, &endpoint);
			}
			if (endpoint != null)
			{
				Guid iid = typeof(IAudioEndpointVolume).GUID;
				endpoint->Activate(iid, (CLSCTX)7, null, out void* pVol);
				if (pVol != null)
				{
					var client = new VolumeNotificationClient((IAudioEndpointVolume*)pVol, endpoint, onNotify);
					((IAudioEndpointVolume*)pVol)->RegisterControlChangeNotify((IAudioEndpointVolumeCallback*)client.GetComPointer());
					Observers[device.RegistryPath] = client;
				}
				else
					endpoint->Release();
			}
			enumerator->Release();
		}
	}

	public static unsafe void RegisterDeviceChangeCallback(Action onNotify)
	{
		PInvoke.CoInitializeEx(null, COINIT.COINIT_MULTITHREADED);
		Observers.TryRemove("DeviceChange", out object? old);
		if (old is IDisposable disp)
			disp.Dispose();

		HRESULT hrEnum = PInvoke.CoCreateInstance<IMMDeviceEnumerator>(typeof(MMDeviceEnumerator).GUID, null, (CLSCTX)7, out IMMDeviceEnumerator* pEnumerator);
		if (hrEnum.Succeeded)
		{
			IMMDeviceEnumerator* enumerator = pEnumerator;
			var client = new DeviceNotificationClient(onNotify, enumerator);
			enumerator->RegisterEndpointNotificationCallback((IMMNotificationClient*)client.GetComPointer());
			Observers["DeviceChange"] = client;
		}
	}

	internal static unsafe string? GetDefaultAudioEndpointId(EDataFlow flow)
	{
		PInvoke.CoInitializeEx(null, COINIT.COINIT_MULTITHREADED);
		if (PInvoke.CoCreateInstance<IMMDeviceEnumerator>(typeof(MMDeviceEnumerator).GUID, null, CLSCTX.CLSCTX_ALL, out IMMDeviceEnumerator* pEnumerator).Value >= 0)
		{
			IMMDeviceEnumerator* enumerator = pEnumerator;
			IMMDevice* endpoint = null;
			try
			{
				enumerator->GetDefaultAudioEndpoint(flow, ERole.eConsole, &endpoint);
				if (endpoint != null)
				{
					PWSTR id = default;
					endpoint->GetId(&id);
					string result = @"SWD\MMDEVAPI\" + id.ToString();
					PInvoke.CoTaskMemFree(id);
					endpoint->Release();
					enumerator->Release();
					return result;
				}
			}
			catch { }
			enumerator->Release();
		}
		return null;
	}

	public static unsafe DeviceInfo? GetDefaultAudioDeviceInfo(EDataFlow flow)
	{
		PInvoke.CoInitializeEx(null, COINIT.COINIT_MULTITHREADED);

		if (PInvoke.CoCreateInstance<IMMDeviceEnumerator>(typeof(MMDeviceEnumerator).GUID, null, CLSCTX.CLSCTX_ALL, out IMMDeviceEnumerator* pEnumerator).Value >= 0)
		{
			IMMDeviceEnumerator* enumerator = pEnumerator;
			IMMDevice* endpoint = null;

			try
			{
				enumerator->GetDefaultAudioEndpoint(flow, ERole.eConsole, &endpoint);
			}
			catch { }

			if (endpoint != null)
			{
				PWSTR id = default;
				endpoint->GetId(&id);
				string deviceId = id.ToString();

				var device = new DeviceInfo
				{
					FriendlyName = "Unknown",
					PnpDeviceId = deviceId,
					RegistryPath = deviceId
				};

				IPropertyStore* store = null;
				endpoint->OpenPropertyStore((uint)STGM.STGM_READ, &store);
				if (store != null)
				{
					store->GetValue(PInvoke.PKEY_Device_FriendlyName, out PROPVARIANT prop);
					if (prop.Anonymous.Anonymous.vt == VARENUM.VT_LPWSTR)
					{
						string fullName = prop.Anonymous.Anonymous.Anonymous.pwszVal.ToString();
						device.FriendlyName = fullName;
					}
					PInvoke.PropVariantClear(&prop);
					store->Release();
				}

				device.IsInputDevice = flow == EDataFlow.eCapture;

				PInvoke.CoTaskMemFree(id);
				endpoint->Release();
				enumerator->Release();

				return device;
			}

			enumerator->Release();
		}

		return null;
	}

	public static void ApplyAudioSettings(DeviceInfo device, BufferSizeOption option)
	{
		if (option == null)
			return;

		string? json = localSettings.Values["Sound"]?.ToString();
		JsonArray array = JsonNode.Parse(json ?? "[]")?.AsArray() ?? [];

		JsonObject? obj = null;
		foreach (JsonNode? item in array)
		{
			if (item?["PnpDeviceId"]?.ToString() == device.PnpDeviceId)
			{
				obj = item.AsObject();
				break;
			}
		}

		if (obj == null)
		{
			obj = new JsonObject { ["PnpDeviceId"] = device.PnpDeviceId };
			array.Add((JsonNode)obj);
		}

		obj["BufferSize"] = option.Ms;
		obj["IsInput"] = device.IsInputDevice;

		localSettings.Values["Sound"] = array.ToJsonString();
		SetBufferSizes();
	}

	public static unsafe void SetBufferSizes()
	{
		foreach (Process process in Process.GetProcessesByName("AutoOS.Sound"))
		{
			process.Kill();
			process.WaitForExit();
		}

		string? json = localSettings.Values["Sound"]?.ToString();
		if (string.IsNullOrEmpty(json))
			return;

		JsonArray? array = JsonNode.Parse(json)?.AsArray();
		if (array == null || array.Count == 0)
			return;

		PInvoke.CoInitializeEx(null, COINIT.COINIT_MULTITHREADED);
		string? currentOutputId = GetDefaultAudioEndpointId(EDataFlow.eRender)?.Replace(@"SWD\MMDEVAPI\", "");
		string? currentInputId = GetDefaultAudioEndpointId(EDataFlow.eCapture)?.Replace(@"SWD\MMDEVAPI\", "");

		float outputMs = 0;
		float inputMs = 0;

		foreach (JsonNode? item in array)
		{
			string? id = item?["PnpDeviceId"]?.GetValue<string>();
			float ms = item?["BufferSize"]?.GetValue<float>() ?? 0;
			if (ms > 0 && ms < 10)
			{
				if (id == currentOutputId)
					outputMs = ms;
				if (id == currentInputId)
					inputMs = ms;
			}
		}

		if (outputMs <= 0 && inputMs <= 0)
			return;

		File.Copy(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AutoOS.Sound.exe"), Path.Combine(ApplicationData.Current.LocalFolder.Path, "AutoOS.Sound.exe"), true);

		string args = "";
		if (outputMs > 0)
			args += $"-output-ms {outputMs.ToString(System.Globalization.CultureInfo.InvariantCulture)} ";
		if (inputMs > 0)
			args += $"-input-ms {inputMs.ToString(System.Globalization.CultureInfo.InvariantCulture)} ";

		Process.Start(new ProcessStartInfo
		{
			FileName = Path.Combine(ApplicationData.Current.LocalFolder.Path, "AutoOS.Sound.exe"),
			Arguments = args.Trim(),
			CreateNoWindow = true,
			UseShellExecute = false
		});
	}

	private static string GetQualityLabel(ushort bits, uint rate) => (bits, rate) switch
	{
		(16, 8000) => " (Telephone Quality)",
		(16, 16000) => " (Tape Recorder Quality)",
		(16, 22050) => " (AM Radio Quality)",
		(16, 32000) => " (FM Radio Quality)",
		(16, 44100) => " (CD Quality)",
		(16, 48000) => " (DVD Quality)",
		(24 or 32, >= 44100) => " (Studio Quality)",
		(16, >= 88200) => " (Studio Quality)",
		_ => ""
	};

}
