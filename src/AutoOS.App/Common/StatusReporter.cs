using AutoOS.Core.Common;
using AutoOS.App.Views.Installer;
using AutoOS.App.Views.Updater;

namespace AutoOS.App.Common;

public class InstallPageReporter : IStatusReporter
{
	private readonly SynchronizationContext _uiContext;

	public InstallPageReporter()
	{
		_uiContext = SynchronizationContext.Current;
	}

	public void Report(string message = null, double? progress = null, bool? isIndeterminate = null)
	{
		_uiContext?.Post(_ =>
		{
			if (InstallPage.Info != null)
			{
				if (!string.IsNullOrEmpty(message))
				{
					InstallPage.Info.Title = $"{InstallPage.CurrentTitle} ({message})";
				}

				if (progress.HasValue)
					InstallPage.ProgressRingControl.Value = progress.Value;

				if (isIndeterminate.HasValue)
					InstallPage.ProgressRingControl.IsIndeterminate = isIndeterminate.Value;
			}
		}, null);
	}

	public void SetTitle(string title)
	{
		_uiContext?.Post(_ =>
		{
			InstallPage.Info?.Title = title;
		}, null);
	}
}

public class ProgressButtonReporter(ProgressButton progressButton) : IStatusReporter
{
	private readonly SynchronizationContext _uiContext = SynchronizationContext.Current;
	private readonly ProgressButton _progressButton = progressButton;

	public void Report(string message = null, double? progress = null, bool? isIndeterminate = null)
	{
		_uiContext?.Post(_ =>
		{
			if (progress.HasValue)
				_progressButton.Progress = progress.Value;

			if (isIndeterminate.HasValue)
				_progressButton.IsIndeterminate = isIndeterminate.Value;
		}, null);
	}

	public void SetTitle(string title) { }
}

public class UpdateDialogReporter(UpdateDialog updateDialog) : IStatusReporter
{
	private readonly SynchronizationContext _uiContext = SynchronizationContext.Current;
	private readonly UpdateDialog _updateDialog = updateDialog;

	public void Report(string message = null, double? progress = null, bool? isIndeterminate = null)
	{
		_uiContext?.Post(_ =>
		{
			if (!string.IsNullOrEmpty(message))
			{
				_updateDialog.SetStatus($"{_updateDialog.CurrentTitle} ({message})");
			}

			if (progress.HasValue)
				_updateDialog.SetProgress(progress.Value);

		}, null);
	}

	public void SetTitle(string title)
	{
		_uiContext?.Post(_ =>
		{
			_updateDialog.SetStatus(title);
		}, null);
	}
}
