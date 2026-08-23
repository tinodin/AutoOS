using System.ComponentModel;
using System.Runtime.CompilerServices;
using AutoOS.App.Data.Contracts;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.Storage;

namespace AutoOS.App.Services;

public sealed partial class AppearanceSettingsService : IAppearanceSettingsService
{
	private readonly ApplicationDataContainer _localSettings = ApplicationData.Current.LocalSettings;

	public event PropertyChangedEventHandler? PropertyChanged;

	private const string KeySource = "BackgroundImageSource";
	private const string KeyOpacity = "BackgroundImageOpacity";
	private const string KeyFit = "BackgroundImageFit";
	private const string KeyVertical = "BackgroundImageVerticalAlignment";
	private const string KeyHorizontal = "BackgroundImageHorizontalAlignment";

	public string AppThemeBackgroundImageSource
	{
		get => _localSettings.Values[KeySource] as string ?? string.Empty;
		set
		{
			if (AppThemeBackgroundImageSource == value)
				return;

			if (string.IsNullOrEmpty(value))
				_localSettings.Values.Remove(KeySource);
			else
				_localSettings.Values[KeySource] = value;

			OnPropertyChanged();
		}
	}

	public float AppThemeBackgroundImageOpacity
	{
		get
		{
			if (_localSettings.Values.TryGetValue(KeyOpacity, out object? v))
			{
				if (v is double d)
					return (float)d;
				if (v is float f)
					return f;
				if (v is string s && float.TryParse(s, out float parsed))
					return parsed;
			}

			return 1f;
		}
		set
		{
			if (Math.Abs(AppThemeBackgroundImageOpacity - value) < 0.001f)
				return;

			// ApplicationDataContainer prefers double for floating values
			_localSettings.Values[KeyOpacity] = (double)value;
			OnPropertyChanged();
		}
	}

	public Stretch AppThemeBackgroundImageFit
	{
		get
		{
			if (_localSettings.Values.TryGetValue(KeyFit, out object? v))
			{
				if (v is string s && Enum.TryParse(s, out Stretch result))
					return result;
				if (v is int i && Enum.IsDefined(typeof(Stretch), i))
					return (Stretch)i;
			}

			return Stretch.UniformToFill;
		}
		set
		{
			if (AppThemeBackgroundImageFit == value)
				return;

			_localSettings.Values[KeyFit] = value.ToString();
			OnPropertyChanged();
		}
	}

	public VerticalAlignment AppThemeBackgroundImageVerticalAlignment
	{
		get
		{
			if (_localSettings.Values.TryGetValue(KeyVertical, out object? v))
			{
				if (v is string s && Enum.TryParse(s, out VerticalAlignment result))
					return result;
				if (v is int i && Enum.IsDefined(typeof(VerticalAlignment), i))
					return (VerticalAlignment)i;
			}

			return VerticalAlignment.Center;
		}
		set
		{
			if (AppThemeBackgroundImageVerticalAlignment == value)
				return;

			_localSettings.Values[KeyVertical] = value.ToString();
			OnPropertyChanged();
		}
	}

	public HorizontalAlignment AppThemeBackgroundImageHorizontalAlignment
	{
		get
		{
			if (_localSettings.Values.TryGetValue(KeyHorizontal, out object? v))
			{
				if (v is string s && Enum.TryParse(s, out HorizontalAlignment result))
					return result;
				if (v is int i && Enum.IsDefined(typeof(HorizontalAlignment), i))
					return (HorizontalAlignment)i;
			}

			return HorizontalAlignment.Center;
		}
		set
		{
			if (AppThemeBackgroundImageHorizontalAlignment == value)
				return;

			_localSettings.Values[KeyHorizontal] = value.ToString();
			OnPropertyChanged();
		}
	}

	private void OnPropertyChanged([CallerMemberName] string propertyName = "")
		=> PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
