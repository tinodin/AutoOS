using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Media.Streaming.Adaptive;

namespace AutoOS.App.UserControls;

public class VideoPlayerSource : IImageItem
{
	private MediaPlayer? _mediaPlayer;
	private int _sourceVersion;
	private bool _isSourcePending;

	public object? Data { get; set; }

	public Uri? Source { get; set; }

	public Uri? ImageUri { get; set; }

	public string? Title { get; set; }

	public ImageSource? ImageSource
	{
		get
		{
			if (field == null && ImageUri != null)
			{
				field = new BitmapImage(ImageUri)
				{
					CreateOptions = BitmapCreateOptions.None
				};
			}

			return field;
		}
	}

	public MediaPlayer MediaPlayer
	{
		get
		{
			if (_mediaPlayer == null)
			{
				_mediaPlayer = new MediaPlayer();
				EnsureMediaSource();
			}
			return _mediaPlayer;
		}
	}

	private void EnsureMediaSource()
	{
		if (Source == null || _isSourcePending)
			return;

		_isSourcePending = true;
		_ = PrepareMediaSourceAsync(Source, _sourceVersion);
	}

	private async Task PrepareMediaSourceAsync(Uri source, int version)
	{
		MediaSource? mediaSource = null;

		try
		{
			if (source.AbsolutePath.EndsWith(".m3u8", StringComparison.OrdinalIgnoreCase))
			{
				AdaptiveMediaSourceCreationResult result = await AdaptiveMediaSource.CreateFromUriAsync(source);
				if (version != _sourceVersion)
					return;

				if (result.Status == AdaptiveMediaSourceCreationStatus.Success && result.MediaSource != null)
				{
					if (result.MediaSource.AvailableBitrates.Count > 0)
						result.MediaSource.InitialBitrate = result.MediaSource.AvailableBitrates.Max();

					mediaSource = MediaSource.CreateFromAdaptiveMediaSource(result.MediaSource);
				}
			}

			ApplyMediaSource(mediaSource ?? MediaSource.CreateFromUri(source), version);
		}
		catch
		{
			if (version == _sourceVersion)
			{
				try
				{
					ApplyMediaSource(MediaSource.CreateFromUri(source), version);
				}
				catch { }
			}
		}
		finally
		{
			if (version == _sourceVersion)
				_isSourcePending = false;
		}
	}

	private void ApplyMediaSource(MediaSource mediaSource, int version)
	{
		if (version != _sourceVersion || _mediaPlayer == null || _mediaPlayer.Source != null)
			return;

		_mediaPlayer.Source = mediaSource;
		if (_mediaPlayer.AutoPlay)
			_mediaPlayer.Play();
	}

	public void TeardownPlayer()
	{
		_sourceVersion++;
		_isSourcePending = false;

		if (_mediaPlayer != null)
		{
			try
			{
				if (_mediaPlayer.PlaybackSession?.PlaybackState != MediaPlaybackState.None)
				{
					_mediaPlayer.Pause();
				}
			}
			catch { }
			try
			{
				_mediaPlayer.Source = null!;
			}
			catch { }
			_mediaPlayer = null;
		}
	}
}
