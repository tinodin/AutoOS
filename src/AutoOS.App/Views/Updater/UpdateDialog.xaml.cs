using AutoOS.App.Views.Installer.Stages;
using AutoOS.Core.Common;
using AutoOS.Core.Helpers.Download;
using AutoOS.Core.Helpers.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace AutoOS.App.Views.Updater;

public sealed partial class UpdateDialog : UserControl
{
	public double CurrentGroupStart { get; set; }
	public double CurrentGroupTarget { get; set; }

	public UpdateDialog()
	{
		InitializeComponent();
		CurrentGroupStart = 0;
		CurrentGroupTarget = 100;
	}

	public string CurrentTitle { get; set; } = null!;

	public string GetStatus() => StatusText.Text;

	public void SetStatus(string text)
	{
		StatusText.Text = text;
	}

	public void SetProgress(double value)
	{
		ProgressBar.IsIndeterminate = false;
		ProgressBar.Value = CurrentGroupStart + (value / 100.0 * (CurrentGroupTarget - CurrentGroupStart));
	}

	public void SetSuccess()
	{
		ProgressBar.Foreground = new SolidColorBrush((Windows.UI.Color)Application.Current.Resources["SystemFillColorSuccess"]);
	}

	public void SetError()
	{
		ProgressBar.Foreground = (Brush)Application.Current.Resources["SystemFillColorCriticalBrush"];
	}

	public void SetCaution()
	{
		ProgressBar.Foreground = (Brush)Application.Current.Resources["SystemFillColorCautionBrush"];
	}

	public void ResetProgressColor()
	{
		ProgressBar.ClearValue(ProgressBar.ForegroundProperty);
	}

	public async Task RunActions(List<(string Title, Func<Task> Action, Func<bool>? Condition)> actions)
	{
		var filteredActions = actions.Where(a => a.Condition == null || a.Condition.Invoke()).ToList();

		string previousTitle = string.Empty;
		int groupedTitleCount = 0;
		List<Func<Task>> currentGroup = [];

		for (int i = 0; i < filteredActions.Count; i++)
		{
			if (i == 0 || filteredActions[i].Title != filteredActions[i - 1].Title || filteredActions[i].Title.Contains("downloading", StringComparison.OrdinalIgnoreCase))
			{
				groupedTitleCount++;
			}
		}

		double incrementPerTitle = groupedTitleCount > 0 ? 100.0 / (double)groupedTitleCount : 0;

		ProgressBar.IsIndeterminate = false;

		foreach ((string? title, Func<Task>? action, Func<bool>? condition) in filteredActions)
		{
			if (previousTitle != string.Empty && (previousTitle != title || title.Contains("downloading", StringComparison.OrdinalIgnoreCase)) && currentGroup.Count > 0)
			{
				CurrentGroupStart = ProgressBar.Value;
				CurrentGroupTarget = CurrentGroupStart + incrementPerTitle;

				foreach (Func<Task> groupedAction in currentGroup)
				{
					try
					{
						await groupedAction();
					}
					catch (Exception ex)
					{
						try
						{
							LogHelper.LogError(ex, PreparingStage.GPUs, previousTitle);
						}
						catch (Exception exception)
						{
							StatusText.Text = ex.Message;
							SetError();
							await LogHelper.LogFallbackError(ex);
							await LogHelper.LogFallbackError(exception);
						}
					}
				}

				ProgressBar.Value = CurrentGroupTarget;
				await Task.Delay(500);
				currentGroup.Clear();
			}

			StatusText.Text = title + "...";
			CurrentTitle = title + "...";
			currentGroup.Add(action);
			previousTitle = title;
		}

		if (currentGroup.Count > 0)
		{
			CurrentGroupStart = ProgressBar.Value;
			CurrentGroupTarget = CurrentGroupStart + incrementPerTitle;

			foreach (Func<Task> groupedAction in currentGroup)
			{
				try
				{
					await groupedAction();
				}
				catch (Exception ex)
				{
					StatusText.Text = ex.Message;
					SetError();
				}
			}
			ProgressBar.Value = CurrentGroupTarget;
		}
	}

	public async Task Download(string url, string path, string file, string displayTitle, double startValue, double targetValue)
	{
		SetStatus(displayTitle + "...");

		SynchronizationContext? uiContext = SynchronizationContext.Current;
		var reporter = new UpdateStatusReporter(uiContext!, StatusText, ProgressBar, displayTitle, startValue, targetValue);

		await DownloadHelper.Download(url, path, file, reporter);

		uiContext?.Post(_ =>
		{
			ProgressBar.IsIndeterminate = false;
			ProgressBar.Value = targetValue;
		}, null);
	}

	private class UpdateStatusReporter(SynchronizationContext uiContext, TextBlock statusText, ProgressBar progressBar, string displayTitle, double startValue, double targetValue) : IStatusReporter
	{
		private readonly SynchronizationContext _uiContext = uiContext;
		private readonly TextBlock _statusText = statusText;
		private readonly ProgressBar _progressBar = progressBar;
		private readonly string _displayTitle = displayTitle;
		private readonly double _startValue = startValue;
		private readonly double _targetValue = targetValue;

		public void Report(string? message = null, double? progress = null, bool? isIndeterminate = null)
		{
			_uiContext?.Post(_ =>
			{
				if (message != null)
				{
					_statusText.Text = $"{_displayTitle} ({message})";
				}
				else
				{
					_statusText.Text = $"{_displayTitle}...";
				}

				if (isIndeterminate == true)
				{
					_progressBar.IsIndeterminate = true;
				}
				else
				{
					_progressBar.IsIndeterminate = false;
				}

				if (progress.HasValue)
				{
					_progressBar.Value = _startValue + (progress.Value / 100.0 * (_targetValue - _startValue));
				}
			}, null);
		}

		public void SetTitle(string title)
		{
			_uiContext?.Post(_ =>
			{
				_statusText.Text = title;
			}, null);
		}
	}
}
