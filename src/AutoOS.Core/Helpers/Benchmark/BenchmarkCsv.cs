using AutoOS.Core.Models;

namespace AutoOS.Core.Helpers.Benchmark;

public static class BenchmarkCsv
{
	public static List<string> ParseCsvLine(string line)
	{
		if (string.IsNullOrEmpty(line))
			return [];
		var result = new List<string>();
		bool inQuotes = false;
		int start = 0;
		for (int i = 0; i < line.Length; i++)
		{
			if (line[i] == '"')
				inQuotes = !inQuotes;
			else if (line[i] == ',' && !inQuotes)
			{
				result.Add(line[start..i].Trim('"'));
				start = i + 1;
			}
		}
		result.Add(line[start..].Trim('"'));
		return result;
	}

	public static readonly Dictionary<string, string> MetricDescriptions = new(StringComparer.OrdinalIgnoreCase)
	{
		["Displayed FPS"] = "Measures how fast frames actually change on your screen.",
		["Rendered FPS"] = "Measures how fast the game creates frames before they are sent to your screen.",
		["MsBetweenDisplayChange"] = "The time it takes for a new image to physically appear on your screen.",
		["MsBetweenPresents"] = "The time it takes the game engine to push out each new frame.",
		["MsGPUBusy"] = "How long the graphics card works on a single frame.",
		["MsUntilDisplayed"] = "The delay between the game finishing a frame and it appearing on screen."
	};


	public static readonly string[] StatisticLabels =
	[
		"0.1% Low Avg", "1% Low Avg", "Average (Arithmetic)", "Average (Harmonic)",
		"Minimum", "Maximum", "P0.1", "P1", "P5", "P50 (Median)", "P95", "P99"
	];

	public static readonly string[] StatisticLabelsShort =
	[
		"0.1% Low Avg", "1% Low Avg", "Avg (Arithmetic)", "Avg (Harmonic)",
		"Min", "Max", "P0.1", "P1", "P5", "P50 (Median)", "P95", "P99"
	];

	public static readonly Dictionary<string, string> StatisticDescriptions = new(StringComparer.OrdinalIgnoreCase)
	{
		["0.1% Low Avg"] = "Average FPS across the worst-performing 0.1% of frames. Higher values indicate smoother performance.",
		["1% Low Avg"] = "Average FPS across the worst-performing 1% of frames. Higher values indicate smoother performance.",
		["Avg (Arithmetic)"] = "Conventional average. Every sampled frame contributes equally.",
		["Avg (Harmonic)"] = "Frame-duration-weighted average. Long, slow frames have more influence, making spikes more visible.",
		["Average (Arithmetic)"] = "Conventional average. Every sampled frame contributes equally.",
		["Average (Harmonic)"] = "Frame-duration-weighted average. Long, slow frames have more influence, making spikes more visible.",
		["Min"] = "Lowest sampled value in the recording.",
		["Max"] = "Highest sampled value in the recording.",
		["Minimum"] = "Lowest sampled value in the recording.",
		["Maximum"] = "Highest sampled value in the recording.",
		["P0.1"] = "Threshold containing the bottom 0.1% of sampled frames.",
		["P1"] = "Threshold containing the bottom 1% of sampled frames.",
		["P5"] = "Threshold containing the bottom 5% of sampled frames.",
		["P50 (Median)"] = "Median value. Represents typical performance.",
		["P95"] = "Threshold below which 95% of all frames fall. Captures moderate spikes.",
		["P99"] = "Threshold below which 99% of all frames fall. Captures severe hitches.",
		["Standard Deviation"] = "Measures how widely values are spread around the average. Lower is more consistent.",
		["Coefficient of Variation"] = "Standard deviation divided by the mean. Useful for comparing consistency across different performance levels.",
		["Root mean square of successive differences (RMSSD)"] = "Measures frame pacing by comparing timing of adjacent frames. Lower values indicate smoother pacing.",
		["Stepwise-Relative"] = "Typical percentage change from one frame to the next. Lower values indicate lower spike severity."
	};

	public static double GetStatistic(Metrics m, string label) => label switch
	{
		"0.1% Low Avg" => m.Low01,
		"1% Low Avg" => m.Low1,
		"Average (Arithmetic)" or "Avg (Arithmetic)" => m.AvgArithmetic,
		"Average (Harmonic)" or "Avg (Harmonic)" => m.AvgHarmonic,
		"Minimum" or "Min" => m.Min,
		"Maximum" or "Max" => m.Max,
		"P0.1" => m.P01,
		"P1" => m.P1,
		"P5" => m.P5,
		"P50 (Median)" => m.P50Median,
		"P95" => m.P95,
		"P99" => m.P99,
		_ => 0
	};
}
