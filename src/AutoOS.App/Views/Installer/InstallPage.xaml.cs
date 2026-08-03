using AutoOS.Core.Helpers.Logging;
using AutoOS.Core.Helpers.OS;
using AutoOS.Views.Installer.Stages;
using Microsoft.UI.Xaml.Media;
using Microsoft.Win32;
using System.Diagnostics;
using Windows.Storage;
using WinRT.Interop;

namespace AutoOS.Views.Installer;

public sealed partial class InstallPage : Page
{
	public static TextBlock Status { get; private set; }
	public static ProgressBar Progress { get; private set; }
	public static InfoBar Info { get; private set; }
	public static Microsoft.UI.Xaml.Controls.ProgressRing ProgressRingControl { get; private set; }
	public static string CurrentTitle { get; set; }
	public static Button ResumeButton { get; private set; }
	private static readonly ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
	private static int currentStageCounter = 0;

	public InstallPage()
	{
		InitializeComponent();
		Loaded += InstallPage_Loaded;
	}

	private async void InstallPage_Loaded(object sender, RoutedEventArgs e)
	{
		// get navview
		var navView = MainWindow.Instance.GetNavView();

		// disable all menu items
		foreach (var item in navView.MenuItems.OfType<NavigationViewItem>())
		{
			item.IsEnabled = false;
		}

		// rename footer item to installing autoos...
		foreach (var item in navView.FooterMenuItems.OfType<NavigationViewItem>())
		{
			item.Content = "Installing AutoOS...";
		}

		Status = StatusText;
		Progress = ProgressBar;
		Info = InfoBar;
		ProgressRingControl = ProgressRingItem;
		ResumeButton = ResumeButtonItem;

		Progress.ValueChanged += (s, e) =>
		{
			PercentageText.Text = $"{(int)e.NewValue}%";
		};

		currentStageCounter = 0;
		int savedStage = localSettings.Values["actionStage"] as int? ?? -1;

		Progress.Value = localSettings.Values["actionProgress"] as double? ?? 0;
		PercentageText.Text = $"{(int)Progress.Value}%";
		TaskbarHelper.SetProgressValue(WindowNative.GetWindowHandle(App.MainWindow), Progress.Value, 100);

		if (savedStage <= 0)
		{
			await PreparingStage.Run();
			localSettings.Values["actionStage"] = 1;
			localSettings.Values["actionIndex"] = -1;
			localSettings.Values["actionProgress"] = 0.0;
		}
		else
		{
			await PreparingStage.Run();
		}
		currentStageCounter = 1;
		await RunStage("Configuring Security...", await SecurityStage.GetActions(), 5);
		await RunStage("Configuring Powerplans...", PowerStage.GetActions(), 5);
		await RunStage("Configuring Windows Activation...", ActivationStage.GetActions(), 2);
		await RunStage("Configuring Graphics Cards...", await GraphicsStage.GetActions(), 10);
		await RunStage("Configuring Network Adapters...", InternetStage.GetActions(), 5);
		await RunStage("Configuring Audio Devices...", AudioStage.GetActions(), 5);
		await RunStage("Configuring Affinities...", SchedulingStage.GetActions(), 5);
		await RunStage("Configuring Devices...", DevicesStage.GetActions(), 5);
		await RunStage("Configuring Scheduled Tasks...", ScheduledTasksStage.GetActions(), 5);
		await RunStage("Configuring Optional Features...", OptionalFeatureStage.GetActions(), 5);
		await RunStage("Configuring AppX Packages...", AppxStage.GetActions(), 15);
		await RunStage("Configuring Runtimes...", RuntimesStage.GetActions(), 5);
		await RunStage("Configuring Browsers...", BrowsersStage.GetActions(), 5);
		await RunStage("Configuring Apps...", AppsStage.GetActions(), 15);
		await RunStage("Configuring Games...", GamesStage.GetActions(), 3);
		await RunStage("Cleaning up...", CleanupStage.GetActions(), 5);
		Status.Text = "Installation finished.";
		Info.Title = "Done.";
		Info.Severity = InfoBarSeverity.Success;
		Progress.Foreground = new SolidColorBrush((Windows.UI.Color)Application.Current.Resources["SystemFillColorSuccess"]);
		ProgressRingControl.Foreground = new SolidColorBrush((Windows.UI.Color)Application.Current.Resources["SystemFillColorSuccess"]);
		localSettings.Values["Version"] = ProcessInfoHelper.Version;
		localSettings.Values["Install_Version"] = ProcessInfoHelper.Version;
		localSettings.Values["Install_Build"] = OSHelper.GetWindowsVersionString();
		localSettings.Values["Install_End"] = DateTimeOffset.Now.ToString("O");
		Registry.SetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\AutoOS", "IsInstalled", 1, RegistryValueKind.DWord);
		Registry.SetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\Explorer", "LockedStartLayout", 0, RegistryValueKind.DWord);
		localSettings.Values.Remove("actionStage");
		localSettings.Values.Remove("actionIndex");
		try
		{
			await LogHelper.Log(PreparingStage.GPUs);
			await LogHelper.LogNetworkSettings(PreparingStage.GPUs);
		}
		catch (Exception ex)
		{
			await LogHelper.LogFallbackError(ex);
		}
		Info.Title = "Restarting in 3...";
		await Task.Delay(1000);
		Info.Title = "Restarting in 2...";
		await Task.Delay(1000);
		Info.Title = "Restarting in 1...";
		await Task.Delay(1000);
		Info.Title = "Restarting...";
		await Task.Delay(750);
		ProcessStartInfo processStartInfo = new()
		{
			FileName = "cmd.exe",
			Arguments = $"/c shutdown /r /t 0",
			UseShellExecute = false,
			CreateNoWindow = true,
		};
		Process.Start(processStartInfo);
	}

	public static async Task RunStage(string status, List<(string Title, Func<Task> Action, Func<bool> Condition)> actions, double stagePercentage)
	{
		int stageIndex = currentStageCounter++;
		int savedStage = localSettings.Values["actionStage"] as int? ?? -1;
		int savedAction = localSettings.Values["actionIndex"] as int? ?? -1;

		if (stageIndex < savedStage)
		{
			return;
		}

		var windowHandle = WindowNative.GetWindowHandle(App.MainWindow);
		Status.Text = status;

		var filteredActions = actions.Where(a => a.Condition == null || a.Condition.Invoke()).ToList();
		int groupedTitleCount = 0;

		for (int i = 0; i < filteredActions.Count; i++)
		{
			if (i == 0 || filteredActions[i].Title != filteredActions[i - 1].Title || filteredActions[i].Title.Contains("downloading", StringComparison.OrdinalIgnoreCase))
			{
				groupedTitleCount++;
			}
		}

		double startProgress = Progress.Value;
		int executedGroupsCount = 0;
		string previousTitle = string.Empty;
		List<Func<Task>> currentGroup = [];

		int globalIndex = 0;
		foreach (var (title, action, _) in filteredActions)
		{
			if (previousTitle != string.Empty && (previousTitle != title || title.Contains("downloading", StringComparison.OrdinalIgnoreCase)) && currentGroup.Count > 0)
			{
				int groupIndex = globalIndex - currentGroup.Count;
				bool executed = false;
				foreach (var groupedAction in currentGroup)
				{
					if (stageIndex == savedStage && groupIndex <= savedAction) { groupIndex++; continue; }

					executed = true;
					try
					{
						CurrentTitle = previousTitle + "...";
						Info.Title = CurrentTitle;
						await groupedAction();
						localSettings.Values["actionStage"] = stageIndex;
						localSettings.Values["actionIndex"] = groupIndex;
					}
					catch (Exception ex)
					{
						try
						{
							await LogHelper.LogError(ex, PreparingStage.GPUs, previousTitle);
						}
						catch (Exception exception)
						{
							await LogHelper.LogFallbackError(ex);
							await LogHelper.LogFallbackError(exception);
						}

						Info.Title = $"{previousTitle}: {ex.Message}";
						Info.Severity = InfoBarSeverity.Error;
						Progress.Foreground = (Brush)Application.Current.Resources["SystemFillColorCriticalBrush"];
						TaskbarHelper.SetProgressState(windowHandle, TaskbarStates.Error);
						ProgressRingControl.Visibility = Visibility.Collapsed;
						ResumeButton.Visibility = Visibility.Visible;

						var tcs = new TaskCompletionSource<bool>();
						void resumeHandler(object sender, RoutedEventArgs e)
						{
							ResumeButton.Click -= resumeHandler;
							Info.Severity = InfoBarSeverity.Informational;
							Progress.ClearValue(ProgressBar.ForegroundProperty);
							TaskbarHelper.SetProgressState(windowHandle, TaskbarStates.Normal);
							ProgressRingControl.Visibility = Visibility.Visible;
							ResumeButton.Visibility = Visibility.Collapsed;
							tcs.TrySetResult(true);
						}

						ResumeButton.Click += resumeHandler;
						await tcs.Task;

						localSettings.Values["actionStage"] = stageIndex;
						localSettings.Values["actionIndex"] = groupIndex;
					}
					groupIndex++;
				}

				if (executed)
				{
					executedGroupsCount++;
					Progress.Value = startProgress + (stagePercentage * executedGroupsCount) / groupedTitleCount;
					localSettings.Values["actionProgress"] = Progress.Value;
					TaskbarHelper.SetProgressValue(windowHandle, Progress.Value, 100);
					await Task.Delay(150);
				}
				currentGroup.Clear();
			}

			currentGroup.Add(action);
			previousTitle = title;
			globalIndex++;
		}

		if (currentGroup.Count > 0)
		{
			int groupIndex = filteredActions.Count - currentGroup.Count;
			bool executed = false;
			foreach (var groupedAction in currentGroup)
			{
				if (stageIndex == savedStage && groupIndex <= savedAction) { groupIndex++; continue; }

				executed = true;
				try
				{
					CurrentTitle = previousTitle + "...";
					Info.Title = CurrentTitle;
					await groupedAction();
					localSettings.Values["actionStage"] = stageIndex;
					localSettings.Values["actionIndex"] = groupIndex;
				}
				catch (Exception ex)
				{
					try
					{
						await LogHelper.LogError(ex, PreparingStage.GPUs, previousTitle);
					}
					catch { }

					Info.Title = $"{previousTitle}: {ex.Message}";
					Info.Severity = InfoBarSeverity.Error;
					Progress.Foreground = (Brush)Application.Current.Resources["SystemFillColorCriticalBrush"];
					TaskbarHelper.SetProgressState(windowHandle, TaskbarStates.Error);
					ProgressRingControl.Visibility = Visibility.Collapsed;
					ResumeButton.Visibility = Visibility.Visible;

					var tcs = new TaskCompletionSource<bool>();
					void resumeHandler(object sender, RoutedEventArgs e)
					{
						ResumeButton.Click -= resumeHandler;
						Info.Severity = InfoBarSeverity.Informational;
						Progress.ClearValue(ProgressBar.ForegroundProperty);
						TaskbarHelper.SetProgressState(windowHandle, TaskbarStates.Normal);
						ProgressRingControl.Visibility = Visibility.Visible;
						ResumeButton.Visibility = Visibility.Collapsed;
						tcs.TrySetResult(true);
					}

					ResumeButton.Click += resumeHandler;
					await tcs.Task;

					localSettings.Values["actionStage"] = stageIndex;
					localSettings.Values["actionIndex"] = groupIndex;
				}
				groupIndex++;
			}

			if (executed)
			{
				executedGroupsCount++;
				Progress.Value = startProgress + (stagePercentage * executedGroupsCount) / groupedTitleCount;
				localSettings.Values["actionProgress"] = Progress.Value;
				TaskbarHelper.SetProgressValue(windowHandle, Progress.Value, 100);
			}
		}

		localSettings.Values["actionStage"] = stageIndex + 1;
		localSettings.Values["actionIndex"] = -1;

		if (filteredActions.Count == 0)
		{
			Progress.Value = startProgress + stagePercentage;
			localSettings.Values["actionProgress"] = Progress.Value;
			TaskbarHelper.SetProgressValue(windowHandle, Progress.Value, 100);
		}
	}
}
