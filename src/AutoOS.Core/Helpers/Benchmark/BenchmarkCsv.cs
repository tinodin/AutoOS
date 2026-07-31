using System.Buffers;
using System.Globalization;
using System.Text;
using AutoOS.Core.Helpers.Benchmark.Models;
using DevWinUI;
using Microsoft.Win32.SafeHandles;

namespace AutoOS.Core.Helpers.Benchmark;

public static class BenchmarkCsv
{
	public static string RecordingsDirectory => Path.Combine(PathHelper.GetAppDataFolderPath(), "Benchmarks");
	
	public static string ReadLastLine(string path, long length)
	{
		if (length == 0)
			return string.Empty;

		const int InitialTail = 8 * 1024;
		int tail = (int)Math.Min(InitialTail, length);

		using SafeFileHandle handle = File.OpenHandle(path, FileMode.Open, FileAccess.Read, FileShare.Read, FileOptions.RandomAccess);

		byte[] buffer = ArrayPool<byte>.Shared.Rent(tail);
		try
		{
			while (true)
			{
				if (buffer.Length < tail)
				{
					ArrayPool<byte>.Shared.Return(buffer);
					buffer = ArrayPool<byte>.Shared.Rent(tail);
				}

				int read = RandomAccess.Read(handle, buffer.AsSpan(0, tail), length - tail);

				int end = read;
				while (end > 0 && (buffer[end - 1] == (byte)'\n' || buffer[end - 1] == (byte)'\r'))
					end--;

				int start = end;
				while (start > 0 && buffer[start - 1] != (byte)'\n')
					start--;

				if (start > 0 || tail >= length)
					return Encoding.UTF8.GetString(buffer, start, end - start);

				tail = (int)Math.Min((long)tail * 2, length);
			}
		}
		finally
		{
			ArrayPool<byte>.Shared.Return(buffer);
		}
	}

	public static ReadOnlySpan<char> GetField(ReadOnlySpan<char> line, int fieldIndex)
	{
		if (fieldIndex < 0)
			return default;

		int start = 0;
		int currentIndex = 0;

		for (int i = 0; i <= line.Length; i++)
		{
			if (i == line.Length || line[i] == ',')
			{
				if (currentIndex == fieldIndex)
					return line[start..i];

				start = i + 1;
				currentIndex++;
			}
		}

		return default;
	}

	public static int EnsureColumn(List<string> headerCols, string columnName)
	{
		int index = headerCols.FindIndex(header => string.Equals(header, columnName, StringComparison.OrdinalIgnoreCase));
		if (index >= 0)
			return index;

		index = headerCols.Count;
		headerCols.Add(columnName);
		return index;
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
		["Stepwise-Relative"] = "Median percentage change between consecutive frame times. Lower values indicate less severe spikes."
	};

	public static double GetStatistic(Metrics metric, string label) => label switch
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
		_ => 0
	};


	public readonly record struct StatisticDefinition(string Label, string Description, string Format, string Suffix, string DeltaSuffix, bool HigherIsBetter)
	{
		public string FormatValue(double value) => value.ToString(Format, CultureInfo.CurrentCulture) + Suffix;
	}

	public static readonly Dictionary<string, StatisticDefinition> FpsStatistics = new()
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

	public static readonly Dictionary<string, StatisticDefinition> LatencyStatistics = new()
	{
		["Average (Arithmetic)"] = new("Average (Arithmetic)", StatisticDescriptions["Average (Arithmetic)"], "0.####", " ms", " ms", HigherIsBetter: false),
		
		["P50 (Median)"] = new("P50 (Median)", StatisticDescriptions["P50 (Median)"], "0.####", " ms", " ms", HigherIsBetter: false),
		["P5"] = new("P95", StatisticDescriptions["P95"], "0.####", " ms", " ms", HigherIsBetter: false),
		["P1"] = new("P99", StatisticDescriptions["P99"], "0.####", " ms", " ms", HigherIsBetter: false),
		
		["Maximum"] = new("Maximum", StatisticDescriptions["Maximum"], "0.####", " ms", " ms", HigherIsBetter: false),
		["Minimum"] = new("Minimum", StatisticDescriptions["Minimum"], "0.####", " ms", " ms", HigherIsBetter: false),
		
		["Root mean square of successive differences (RMSSD)"] = new("Root mean square of successive differences (RMSSD)", StatisticDescriptions["Root mean square of successive differences (RMSSD)"], "0.####", " ms", " ms", HigherIsBetter: false),
		["Stepwise-Relative"] = new("Stepwise-Relative", StatisticDescriptions["Stepwise-Relative"], "0.0", "%", " pp", HigherIsBetter: false),
		["Standard Deviation"] = new("Standard Deviation (STDEV)", StatisticDescriptions["Standard Deviation"], "0.####", " ms", " ms", HigherIsBetter: false),
		["Coefficient of Variation"] = new("Coefficient of Variation (CV)", StatisticDescriptions["Coefficient of Variation"], "0.#####", "", "", HigherIsBetter: false)
	};

	public static readonly (string Name, Func<AnalysisResult, Metrics> Selector, Dictionary<string, StatisticDefinition> Statistics)[] StatisticGroups =
	[
		("Displayed FPS", result => result.DisplayedFps, FpsStatistics),
		("Rendered FPS", result => result.RenderedFps, FpsStatistics),
		("MsBetweenDisplayChange", result => result.MsBetweenDisplayChangeStats, LatencyStatistics),
		("MsBetweenPresents", result => result.MsBetweenPresentsStats, LatencyStatistics),
		("MsGPUBusy", result => result.MsGpuBusyStats, LatencyStatistics),
		("MsUntilDisplayed", result => result.MsUntilDisplayedStats, LatencyStatistics)
	];
}
