using System.Buffers;
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
		["P0.1"] = "Value below which 0.1% of all frames fall. Captures severe spikes.",
		["P1"] = "Value below which 1% of all frames fall. Captures moderate spikes.",
		["P5"] = "Value below which 5% of all frames fall. Captures noticeable drops.",
		["P50 (Median)"] = "Median value. Represents typical performance.",
		["P95"] = "Value below which 95% of all frames fall.",
		["P99"] = "Value below which 99% of all frames fall.",
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
		_ => 0
	};
}
