using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using AutoOS.Core.Helpers.Device.Models;
using AutoOS.Core.Helpers.Sound;
using AutoOS.Core.Helpers.Sound.Models;
using Microsoft.UI.Xaml.Controls.Primitives;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Media.Audio;
using Windows.Win32.System.Com;
using Windows.Win32.System.Com.StructuredStorage;
using Windows.Win32.System.Variant;
using Windows.Win32.UI.Shell.PropertiesSystem;

namespace AutoOS.App.Views.Settings;

public sealed partial class SoundPage : Page, INotifyPropertyChanged
{
	public event PropertyChangedEventHandler? PropertyChanged;
	private void OnPropertyChanged([CallerMemberName] string name = null!)
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
	}

	private DeviceInfo? _currentOutput;
	public DeviceInfo? CurrentOutput
	{
		get => _currentOutput;
		set
		{
			_currentOutput = value;
			OnPropertyChanged();
			OnPropertyChanged(nameof(HasOutputDevice));
		}
	}

	private DeviceInfo? _currentInput;
	public DeviceInfo? CurrentInput
	{
		get => _currentInput;
		set
		{
			_currentInput = value;
			OnPropertyChanged();
			OnPropertyChanged(nameof(HasInputDevice));
		}
	}

	public bool HasOutputDevice
	{
		get => CurrentOutput != null && !string.IsNullOrEmpty(CurrentOutput.RegistryPath);
	}

	public bool HasInputDevice
	{
		get => CurrentInput != null && !string.IsNullOrEmpty(CurrentInput.RegistryPath);
	}

	private bool isInitializingAudioState = true;
	public ObservableCollection<DeviceInfo> AudioEndpoints { get; } = [];

	public SoundPage()
	{
		GetAudioDevices();
		InitializeComponent();
		SoundHelper.RegisterDeviceChangeCallback(() =>
		{
			DispatcherQueue?.TryEnqueue(() =>
			{
				GetAudioDevices();
			});
		});
	}

	private unsafe void GetAudioDevices()
	{
		isInitializingAudioState = true;
		PInvoke.CoInitializeEx(null, COINIT.COINIT_MULTITHREADED);

		if (PInvoke.CoCreateInstance<IMMDeviceEnumerator>(typeof(MMDeviceEnumerator).GUID, null, (CLSCTX)7, out IMMDeviceEnumerator* pEnumerator).Succeeded)
		{
			IMMDeviceEnumerator* enumerator = pEnumerator;

			DeviceInfo? newOutput = ProcessEndpoint(enumerator, EDataFlow.eRender);
			UpdateDevice(ref _currentOutput, newOutput, nameof(CurrentOutput));

			DeviceInfo? newInput = ProcessEndpoint(enumerator, EDataFlow.eCapture);
			UpdateDevice(ref _currentInput, newInput, nameof(CurrentInput));

			enumerator->Release();
		}
		isInitializingAudioState = false;
	}

	private void UpdateDevice(ref DeviceInfo? currentField, DeviceInfo? newNode, string propertyName)
	{
		if (newNode == null)
		{
			if (currentField != null)
			{
				if (propertyName == nameof(CurrentOutput))
					CurrentOutput = null;
				else
					CurrentInput = null;
			}
			return;
		}

		if (currentField == null || currentField.PnpDeviceId != newNode.PnpDeviceId)
		{
			if (propertyName == nameof(CurrentOutput))
				CurrentOutput = newNode;
			else
				CurrentInput = newNode;
		}
		else
		{
			currentField.AvailableFormats = newNode.AvailableFormats;
			currentField.BufferSizes = newNode.BufferSizes;
			currentField.SelectedFormat = newNode.SelectedFormat;
			currentField.SelectedBufferSize = newNode.SelectedBufferSize;
			OnPropertyChanged(propertyName);
		}
	}

	private unsafe DeviceInfo? ProcessEndpoint(IMMDeviceEnumerator* enumerator, EDataFlow flow)
	{
		IMMDevice* endpoint = null;
		try
		{ enumerator->GetDefaultAudioEndpoint(flow, ERole.eConsole, &endpoint); }
		catch { }
		if (endpoint == null)
			return null;

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
			PROPVARIANT prop = default;
			try
			{
				store->GetValue(PInvoke.PKEY_Device_FriendlyName, out prop);
				if (prop.Anonymous.Anonymous.vt == VARENUM.VT_LPWSTR)
				{
					string fullName = prop.Anonymous.Anonymous.Anonymous.pwszVal.ToString();
					if (fullName.Contains('(') && fullName.EndsWith(')'))
					{
						int splitIdx = fullName.IndexOf('(');
						device.FriendlyName = fullName[..splitIdx].Trim();
						device.Description = fullName.Substring(splitIdx + 1, fullName.Length - splitIdx - 2).Trim();
					}
					else
					{
						device.FriendlyName = fullName;
					}
				}
			}
			finally
			{
				PInvoke.PropVariantClear(&prop);
				store->Release();
			}
		}

		device.IsInputDevice = flow == EDataFlow.eCapture;

		AudioDetails details = SoundHelper.GetAudioDetails(device);
		device.SupportPerChannelVolume = details.SupportPerChannelVolume;
		device.Volume = details.CurrentVolume;
		device.LeftVolume = details.LeftVolume;
		device.RightVolume = details.RightVolume;
		device.IsMuted = details.IsMuted;
		device.AvailableFormats = details.Formats;
		device.SelectedFormat = details.Formats.FirstOrDefault(f => f.IsCurrent) ?? details.Formats.FirstOrDefault();

		List<BufferSizeOption> bufferSizes = SoundHelper.GetBufferSizes(device);
		device.BufferSizes = bufferSizes;
		device.SelectedBufferSize = bufferSizes.FirstOrDefault(x => x.IsCurrent) ?? bufferSizes.FirstOrDefault();

		SoundHelper.RegisterVolumeCallback(device, (vol, muted, left, right) =>
		{
			DispatcherQueue?.TryEnqueue(() =>
			{
				isInitializingAudioState = true;
				device.Volume = vol;
				device.IsMuted = muted;
				device.LeftVolume = left;
				device.RightVolume = right;
				isInitializingAudioState = false;
			});
		});

		PInvoke.CoTaskMemFree(id);
		endpoint->Release();
		return device;
	}

	private void Mute_Click(object sender, RoutedEventArgs e)
	{
		if (sender is Button btn && btn.DataContext is DeviceInfo device)
		{
			device.IsMuted = !device.IsMuted;
			SoundHelper.SetAudioMute(device, device.IsMuted);
		}
	}

	private void AudioEndpointBufferSize_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (isInitializingAudioState)
			return;

		if (sender is ComboBox comboBox && comboBox.DataContext is DeviceInfo device && comboBox.SelectedItem is BufferSizeOption option)
		{
			SoundHelper.ApplyAudioSettings(device, option);
		}
	}

	private void Volume_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
	{
		if (isInitializingAudioState)
			return;

		if (sender is Slider slider && slider.DataContext is DeviceInfo device)
		{
			float vol = (float)e.NewValue / 100f;

			float actualVol = SoundHelper.SetAudioVolume(device, vol);
			float actualPercentage = MathF.Round(actualVol * 100f);

			device.Volume = actualPercentage;
			device.LeftVolume = actualPercentage;
			device.RightVolume = actualPercentage;

			if (Math.Abs(slider.Value - actualPercentage) > 0.1)
			{
				slider.Value = actualPercentage;
			}

			if (!device.IsInputDevice)
			{
				if (actualPercentage == 0 && !device.IsMuted)
				{
					device.IsMuted = true;
					SoundHelper.SetAudioMute(device, true);
				}
				else if (actualPercentage > 0 && device.IsMuted)
				{
					device.IsMuted = false;
					SoundHelper.SetAudioMute(device, false);
				}
			}
		}
	}

	private void LeftVolume_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
	{
		if (isInitializingAudioState)
			return;
		if (sender is Slider slider && slider.DataContext is DeviceInfo device)
		{
			float vol = (float)e.NewValue / 100f;
			SoundHelper.SetAudioChannelVolume(device, 0, vol);
		}
	}

	private void RightVolume_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
	{
		if (isInitializingAudioState)
			return;
		if (sender is Slider slider && slider.DataContext is DeviceInfo device)
		{
			float vol = (float)e.NewValue / 100f;
			SoundHelper.SetAudioChannelVolume(device, 1, vol);
		}
	}

	private void Format_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (isInitializingAudioState)
			return;
		if (sender is ComboBox comboBox && comboBox.DataContext is DeviceInfo device && comboBox.SelectedItem is AudioFormatOption format)
		{
			SoundHelper.SetAudioFormat(device, format);

			List<BufferSizeOption> bufferSizes = SoundHelper.GetBufferSizes(device);
			device.BufferSizes = bufferSizes;
			device.SelectedBufferSize = bufferSizes.FirstOrDefault(x => x.IsCurrent) ?? bufferSizes.FirstOrDefault();
		}
	}
}
