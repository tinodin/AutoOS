namespace AutoOS.App.Data.Models.Benchmarks;

public static class Catalog
{
	public static readonly Dictionary<string, string> MetricDescriptions = new(StringComparer.OrdinalIgnoreCase)
	{
		["Displayed FPS"] = "Measures how fast frames actually change on your screen.",
		["Rendered FPS"] = "Measures how fast the game creates frames before they are sent to your screen.",
		["MsBetweenDisplayChange"] = "The time it takes for a new image to physically appear on your screen.",
		["MsBetweenPresents"] = "The time it takes the game engine to push out each new frame.",
		["MsGPUBusy"] = "How long the graphics card works on a single frame.",
		["MsUntilDisplayed"] = "The delay between the game finishing a frame and it appearing on screen.",
		["MsRenderPresentLatency"] = "The time between a frame being rendered and being presented.",
		["Render Queue Depth"] = "The number of frames currently in the render queue.",
		["Stutter Analysis"] = "Breakdown of smooth periods, low FPS drops, stuttering spikes, and frame pacing variability."
	};

	public static readonly string[] StatisticLabels =
	[
		"0.1% Low Avg", "1% Low Avg",
		"Average (Arithmetic)", "Average (Harmonic)",
		"Minimum", "Maximum",
		"P0.1", "P1", "P5", "P50 (Median)", "P95", "P99"
	];

	public static readonly string[] StatisticLabelsShort =
	[
		"0.1% Low Avg", "1% Low Avg",
		"Avg (Arithmetic)", "Avg (Harmonic)",
		"Min", "Max",
		"P0.1", "P1", "P5", "P50 (Median)", "P95", "P99"
	];

	public static readonly Dictionary<string, string> StatisticDescriptions = new(StringComparer.OrdinalIgnoreCase)
	{
		["0.1% Low Avg"] = "Average across the worst-performing 0.1% of samples.",
		["1% Low Avg"] = "Average across the worst-performing 1% of samples.",

		["Avg (Arithmetic)"] = "Conventional average. Every sampled frame contributes equally.",
		["Avg (Harmonic)"] = "Frame-duration-weighted average. Long, slow frames have more influence, making spikes more visible.",
		["Average (Arithmetic)"] = "Conventional average. Every sampled frame contributes equally.",
		["Average (Harmonic)"] = "Frame-duration-weighted average. Long, slow frames have more influence, making spikes more visible.",

		["Min"] = "Lowest sampled value in the recording.",
		["Max"] = "Highest sampled value in the recording.",
		["Minimum"] = "Lowest sampled value in the recording.",
		["Maximum"] = "Highest sampled value in the recording.",

		["P0.1"] = "Value below which 0.1% of all samples fall.",
		["P1"] = "Value below which 1% of all samples fall.",
		["P5"] = "Value below which 5% of all samples fall.",
		["P50 (Median)"] = "Value below which 50% of all samples fall.",
		["P95"] = "Value below which 95% of all samples fall.",
		["P99"] = "Value below which 99% of all samples fall.",

		["Standard Deviation"] = "Measures how widely values are spread around the average. Lower is more consistent.",
		["Coefficient of Variation"] = "Standard deviation divided by the mean. Useful for comparing consistency across different performance levels.",
		["Root mean square of successive differences (RMSSD)"] = "Measures the magnitude of variations between consecutive frame times. Lower values indicate more consistent frame pacing.",
		["Stepwise-Relative"] = "Median percentage change between consecutive frame times. Lower values indicate less severe spikes.",

		["Smooth"] = "Duration of smooth, consistent frame rates.",
		["Low FPS"] = "Duration of frame rates below the low FPS threshold",
		["Stuttering"] = "Duration of frame time spikes exceeding the stutter threshold.",
		["Displayed Adaptive Standard Deviation"] = "Variation of the frame times your monitor actually receives, measured per 500 ms window. Lower is smoother.",
		["Rendered Adaptive Standard Deviation"] = "Variation of the frame times your PC renders, measured per 500 ms window. Lower is smoother."
	};

	public static readonly Dictionary<string, Statistics> FpsStatistics = new()
	{
		["0.1% Low Avg"] = new("0.1% Low Avg FPS", StatisticDescriptions["0.1% Low Avg"], "0.###", " FPS", " FPS", HigherIsBetter: true),
		["1% Low Avg"] = new("1% Low Avg FPS", StatisticDescriptions["1% Low Avg"], "0.###", " FPS", " FPS", HigherIsBetter: true),

		["Average (Arithmetic)"] = new("Average (Arithmetic) FPS", StatisticDescriptions["Average (Arithmetic)"], "0.###", " FPS", " FPS", HigherIsBetter: true),
		["Average (Harmonic)"] = new("Average (Harmonic) FPS", StatisticDescriptions["Average (Harmonic)"], "0.###", " FPS", " FPS", HigherIsBetter: true),

		["Minimum"] = new("Minimum FPS", StatisticDescriptions["Minimum"], "0.###", " FPS", " FPS", HigherIsBetter: true),
		["Maximum"] = new("Maximum FPS", StatisticDescriptions["Maximum"], "0.###", " FPS", " FPS", HigherIsBetter: true),

		["P0.1"] = new("P0.1 FPS", StatisticDescriptions["P0.1"], "0.###", " FPS", " FPS", HigherIsBetter: true),
		["P1"] = new("P1 FPS", StatisticDescriptions["P1"], "0.###", " FPS", " FPS", HigherIsBetter: true),
		["P5"] = new("P5 FPS", StatisticDescriptions["P5"], "0.###", " FPS", " FPS", HigherIsBetter: true),
		["P50 (Median)"] = new("P50 (Median) FPS", StatisticDescriptions["P50 (Median)"], "0.###", " FPS", " FPS", HigherIsBetter: true),
		["P95"] = new("P95 FPS", StatisticDescriptions["P95"], "0.###", " FPS", " FPS", HigherIsBetter: true),
		["P99"] = new("P99 FPS", StatisticDescriptions["P99"], "0.###", " FPS", " FPS", HigherIsBetter: true),

		["Standard Deviation"] = new("Standard Deviation (STDEV)", StatisticDescriptions["Standard Deviation"], "0.###", " FPS", " FPS", HigherIsBetter: false),
		["Coefficient of Variation"] = new("Coefficient of Variation (CV)", StatisticDescriptions["Coefficient of Variation"], "0.#####", "", "", HigherIsBetter: false)
	};

	public static readonly Dictionary<string, Statistics> LatencyStatistics = new()
	{
		["Average (Arithmetic)"] = new("Average (Arithmetic)", StatisticDescriptions["Average (Arithmetic)"], "0.####", " ms", " ms", HigherIsBetter: false),

		["P50 (Median)"] = new("P50 (Median)", StatisticDescriptions["P50 (Median)"], "0.####", " ms", " ms", HigherIsBetter: false),
		["P5"] = new("P95", StatisticDescriptions["P95"], "0.####", " ms", " ms", HigherIsBetter: false),
		["P1"] = new("P99", StatisticDescriptions["P99"], "0.####", " ms", " ms", HigherIsBetter: false),

		["Minimum"] = new("Minimum", StatisticDescriptions["Minimum"], "0.####", " ms", " ms", HigherIsBetter: false),
		["Maximum"] = new("Maximum", StatisticDescriptions["Maximum"], "0.####", " ms", " ms", HigherIsBetter: false),

		["Root mean square of successive differences (RMSSD)"] = new("Root mean square of successive differences (RMSSD)", StatisticDescriptions["Root mean square of successive differences (RMSSD)"], "0.####", " ms", " ms", HigherIsBetter: false),
		["Stepwise-Relative"] = new("Stepwise-Relative", StatisticDescriptions["Stepwise-Relative"], "0.00", " %", " %", HigherIsBetter: false),
		["Standard Deviation"] = new("Standard Deviation (STDEV)", StatisticDescriptions["Standard Deviation"], "0.####", " ms", " ms", HigherIsBetter: false),
		["Coefficient of Variation"] = new("Coefficient of Variation (CV)", StatisticDescriptions["Coefficient of Variation"], "0.#####", "", "", HigherIsBetter: false)
	};

	public static readonly Dictionary<string, Statistics> RenderQueueDepthStatistics = new()
	{
		["Average (Arithmetic)"] = new("Average (Arithmetic)", StatisticDescriptions["Average (Arithmetic)"], "0.####", " frames", " frames", HigherIsBetter: false),

		["P50 (Median)"] = new("P50 (Median)", StatisticDescriptions["P50 (Median)"], "0.####", " frames", " frames", HigherIsBetter: false),
		["P5"] = new("P95", StatisticDescriptions["P95"], "0.####", " frames", " frames", HigherIsBetter: false),
		["P1"] = new("P99", StatisticDescriptions["P99"], "0.####", " frames", " frames", HigherIsBetter: false),

		["Minimum"] = new("Minimum", StatisticDescriptions["Minimum"], "0.####", " frames", " frames", HigherIsBetter: false),
		["Maximum"] = new("Maximum", StatisticDescriptions["Maximum"], "0.####", " frames", " frames", HigherIsBetter: false),

		["Stepwise-Relative"] = new("Stepwise-Relative", StatisticDescriptions["Stepwise-Relative"], "0.00", " %", " %", HigherIsBetter: false),
		["Standard Deviation"] = new("Standard Deviation (STDEV)", StatisticDescriptions["Standard Deviation"], "0.####", " frames", " frames", HigherIsBetter: false),
		["Coefficient of Variation"] = new("Coefficient of Variation (CV)", StatisticDescriptions["Coefficient of Variation"], "0.#####", "", "", HigherIsBetter: false)
	};

	public static readonly Dictionary<string, Statistics> StutterStatistics = new()
	{
		["Smooth"] = new("Smooth", StatisticDescriptions["Smooth"], "0.##", " %", " %", HigherIsBetter: true, Formatter: (value, metric) => $"{metric.Smooth / 100 * metric.TotalSeconds:0.00} s ({value:0.##} %)"),
		["Low FPS"] = new("Low FPS", StatisticDescriptions["Low FPS"], "0.##", " %", " %", HigherIsBetter: false, Formatter: (value, metric) => $"{metric.LowFPS / 100 * metric.TotalSeconds:0.00} s ({value:0.##} %)"),
		["Stuttering"] = new("Stuttering", StatisticDescriptions["Stuttering"], "0.##", " %", " %", HigherIsBetter: false, Formatter: (value, metric) => $"{metric.Stuttering / 100 * metric.TotalSeconds:0.00} s ({value:0.##} %)"),
		["Displayed Adaptive Standard Deviation"] = new("Displayed Adaptive Standard Deviation", StatisticDescriptions["Displayed Adaptive Standard Deviation"], "0.####", " ms", " ms", HigherIsBetter: false),
		["Rendered Adaptive Standard Deviation"] = new("Rendered Adaptive Standard Deviation", StatisticDescriptions["Rendered Adaptive Standard Deviation"], "0.####", " ms", " ms", HigherIsBetter: false)
	};

	public static (string Name, Func<AnalysisResult, Metrics> Selector, Dictionary<string, Statistics> Statistics)[] GetStatisticGroups(double stutterFactor = 2.5, double lowFpsThreshold = 25) =>
	[
		("Displayed FPS", result => result.DisplayedFps, FpsStatistics),
		("Rendered FPS", result => result.RenderedFps, FpsStatistics),
		("MsBetweenDisplayChange", result => result.MsBetweenDisplayChangeStats, LatencyStatistics),
		("MsBetweenPresents", result => result.MsBetweenPresentsStats, LatencyStatistics),
		("MsGPUBusy", result => result.MsGpuBusyStats, LatencyStatistics),
		("MsUntilDisplayed", result => result.MsUntilDisplayedStats, LatencyStatistics),
		("MsRenderPresentLatency", result => result.MsRenderPresentLatencyStats, LatencyStatistics),
		("Render Queue Depth", result => result.RenderQueueDepthStats, RenderQueueDepthStatistics),
		("Stutter Analysis", result => StatisticsCalculator.GetStutterMetrics(result, stutterFactor, lowFpsThreshold), StutterStatistics)
	];

	public static double GetStatistic(this Metrics metric, string label) => label switch
	{
		"0.1% Low Avg" => metric.Low01,
		"1% Low Avg" => metric.Low1,

		"Average (Arithmetic)" or "Avg (Arithmetic)" => metric.AvgArithmetic,
		"Average (Harmonic)" or "Avg (Harmonic)" => metric.AvgHarmonic,

		"Minimum" or "Min" => metric.Min,
		"Maximum" or "Max" => metric.Max,

		"P0.1" => metric.P01,
		"P1" => metric.P1,
		"P5" => metric.P5,
		"P50 (Median)" => metric.P50Median,
		"P95" => metric.P95,
		"P99" => metric.P99,

		"Standard Deviation" => metric.StdDev,
		"Coefficient of Variation" => metric.Cv,
		"Root mean square of successive differences (RMSSD)" => metric.Rmssd,
		"Stepwise-Relative" => metric.StepwiseRelSD * 100,

		"Smooth" => metric.Smooth,
		"Low FPS" => metric.LowFPS,
		"Stuttering" => metric.Stuttering,
		"Displayed Adaptive Standard Deviation" => metric.DisplayedAdaptiveStdDev,
		"Rendered Adaptive Standard Deviation" => metric.RenderedAdaptiveStdDev,
		_ => 0
	};

	public static double? GetStatisticSeconds(this Metrics metric, string label) => label switch
	{
		"Smooth" => metric.Smooth / 100 * metric.TotalSeconds,
		"Low FPS" => metric.LowFPS / 100 * metric.TotalSeconds,
		"Stuttering" => metric.Stuttering / 100 * metric.TotalSeconds,
		_ => null
	};
}
