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

	public static readonly string[] MetricLabels =
	[
		"0.1% Low Avg", "1% Low Avg", "Average (Arithmetic)", "Average (Harmonic)",
		"Minimum", "Maximum", "P0.1", "P1", "P5", "P50 (Median)", "P95", "P99"
	];

	public static readonly string[] MetricLabelsShort =
	[
		"0.1% Low Avg", "1% Low Avg", "Avg (Arithmetic)", "Avg (Harmonic)",
		"Min", "Max", "P0.1", "P1", "P5", "P50 (Median)", "P95", "P99"
	];

	public static double NumericMetric(Metrics m, string label) => label switch
	{
		"0.1% Low Avg" => m.Low01,
		"1% Low Avg" => m.Low1,
		"Average (Arithmetic)" => m.AvgArithmetic,
		"Average (Harmonic)" => m.AvgHarmonic,
		"Minimum" => m.Min,
		"Maximum" => m.Max,
		"P0.1" => m.P01,
		"P1" => m.P1,
		"P5" => m.P5,
		"P50 (Median)" => m.P50Median,
		"P95" => m.P95,
		"P99" => m.P99,
		_ => 0
	};

	public static Dictionary<string, double> StatsToDict(Metrics m)
	{
		return new(StringComparer.OrdinalIgnoreCase)
		{
			["0.1% Low Avg"] = m.Low01, ["1% Low Avg"] = m.Low1,
			["Avg (Arithmetic)"] = m.AvgArithmetic, ["Avg (Harmonic)"] = m.AvgHarmonic,
			["Min"] = m.Min, ["Max"] = m.Max,
			["P0.1"] = m.P01, ["P1"] = m.P1, ["P5"] = m.P5,
			["P50 (Median)"] = m.P50Median, ["P95"] = m.P95, ["P99"] = m.P99
		};
	}
}
