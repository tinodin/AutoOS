using AutoOS.App.Common;
using AutoOS.Core.Helpers.TaskScheduler;

namespace AutoOS.App.Views.Installer.Stages;

public static class ScheduledTasksStage
{
	public static List<(string Title, Func<Task> Action, Func<bool>? Condition)> GetActions()
	{
		var actions = new List<(string Title, Func<Task> Action, Func<bool>? Condition)>();

		// add scheduled task actions
		var tasks = new List<string>
		{
			@"\Microsoft\Windows\Autochk\Proxy",
			@"\Microsoft\Windows\Application Experience\Microsoft Compatibility Appraiser Exp",
			@"\Microsoft\Windows\Application Experience\MareBackup",
			@"\Microsoft\Windows\Application Experience\StartupAppTask",
			@"\Microsoft\Windows\Customer Experience Improvement Program\Consolidator",
			@"\Microsoft\Windows\Customer Experience Improvement Program\UsbCeip",
			@"\Microsoft\Windows\DiskDiagnostic\Microsoft-Windows-DiskDiagnosticDataCollector",
			@"\Microsoft\Windows\DUSM\dusmtask",
			@"\Microsoft\Windows\Feedback\Siuf\DmClient",
			@"\Microsoft\Windows\Feedback\Siuf\DmClientOnScenarioDownload",
			@"\Microsoft\Windows\Flighting\FeatureConfig\BootstrapUsageDataReporting",
			@"\Microsoft\Windows\Flighting\FeatureConfig\GovernedFeatureUsageProcessing",
			@"\Microsoft\Windows\Flighting\FeatureConfig\ReconcileConfigs",
			@"\Microsoft\Windows\Flighting\FeatureConfig\ReconcileFeatures",
			@"\Microsoft\Windows\Flighting\FeatureConfig\UsageDataFlushing",
			@"\Microsoft\Windows\Flighting\FeatureConfig\UsageDataReceiver",
			@"\Microsoft\Windows\Flighting\FeatureConfig\UsageDataReporting",
			@"\Microsoft\Windows\Flighting\OneSettings\RefreshCache",
			@"\Microsoft\Windows\Hotpatch\Monitoring",
			@"\Microsoft\Windows\Maps\MapsToastTask",
			@"\Microsoft\Windows\Maps\MapsUpdateTask",
			@"\Microsoft\Windows\Power Efficiency Diagnostics\AnalyzeSystem",
			@"\Microsoft\Windows\Speech\SpeechModelDownloadTask",
			@"\Microsoft\Windows\Sustainability\PowerGridForecastTask",
			@"\Microsoft\Windows\Sustainability\SustainabilityTelemetry",
			@"\Microsoft\Windows\WDI\ResolutionHost",
			@"\Microsoft\Windows\Windows Error Reporting\QueueReporting",
		};

		foreach (string task in tasks)
		{
			actions.Add((@$"Disabling ""{task}""", async () => TaskSchedulerHelper.Toggle(task, false), null));
		}

		return actions;
	}
}

