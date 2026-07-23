using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;

namespace AutoOS.Views.Settings.Benchmarks;

internal static class BenchmarkCsv
{
	public static List<string> ParseCsvLine(string line)
	{
		if (string.IsNullOrEmpty(line))
			return [];
		using var reader = new StringReader(line);
		using var parser = new CsvParser(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
		{
			HasHeaderRecord = false,
			BadDataFound = null
		});
		if (!parser.Read())
			return [];
		return [.. parser.Record];
	}
}

internal static class BenchmarkStatistics
{
	public static Metrics CalculateMetrics(double[] values, bool isFpsMetric)
	{
		var result = new Metrics();
		if (values.Length == 0)
			return result;

		var sorted = values.OrderBy(v => v).ToArray();
		int n = values.Length;
		double sum = values.Sum();
		double arithmeticMean = sum / n;

		int c1 = Math.Max(1, (int)Math.Ceiling(n * 0.01));
		int c01 = Math.Max(1, (int)Math.Ceiling(n * 0.001));

		if (isFpsMetric)
		{
			result.Low01 = sorted.Take(c01).Average();
			result.Low1 = sorted.Take(c1).Average();
		}
		else
		{
			var desc = sorted.Reverse().ToArray();
			result.Low01 = desc.Take(c01).Average();
			result.Low1 = desc.Take(c1).Average();
		}

		result.AvgArithmetic = arithmeticMean;
		result.AvgHarmonic = HarmonicMean(values);
		result.Min = sorted[0];
		result.Max = sorted[n - 1];

		result.P01 = Percentile(sorted, isFpsMetric ? 99.9 : 0.1);
		result.P1 = Percentile(sorted, isFpsMetric ? 99 : 1);
		result.P5 = Percentile(sorted, isFpsMetric ? 95 : 5);
		result.P50Median = Percentile(sorted, 50);
		result.P95 = Percentile(sorted, isFpsMetric ? 5 : 95);
		result.P99 = Percentile(sorted, isFpsMetric ? 1 : 99);

		double variance = values.Sum(v => (v - arithmeticMean) * (v - arithmeticMean)) / (n - 1);
		result.StdDev = Math.Sqrt(variance);
		result.Cv = arithmeticMean != 0 ? result.StdDev / arithmeticMean : 0;

		double sumSqDiff = 0;
		for (int i = 1; i < n; i++)
		{
			double diff = values[i] - values[i - 1];
			sumSqDiff += diff * diff;
		}
		result.Rmssd = Math.Sqrt(sumSqDiff / (n - 1));

		double sumRelSq = 0;
		int validPairs = 0;
		for (int i = 1; i < n; i++)
		{
			if (values[i - 1] == 0) continue;
			double rel = (values[i] - values[i - 1]) / values[i - 1];
			sumRelSq += rel * rel;
			validPairs++;
		}
		result.StepwiseRelSD = validPairs > 0 ? Math.Sqrt(sumRelSq / validPairs) : 0;

		return result;
	}

	private static double Percentile(double[] sorted, double percentile)
	{
		double position = percentile / 100.0 * (sorted.Length - 1);
		int lower = (int)Math.Floor(position);
		int upper = (int)Math.Ceiling(position);
		if (lower == upper)
			return sorted[lower];
		double fraction = position - lower;
		return sorted[lower] * (1 - fraction) + sorted[upper] * fraction;
	}

	private static double HarmonicMean(double[] values)
	{
		double reciprocalSum = 0;
		int count = 0;
		foreach (var v in values)
		{
			if (v > 0)
			{
				reciprocalSum += 1.0 / v;
				count++;
			}
		}
		return count > 0 ? count / reciprocalSum : 0;
	}
}

public class Metrics
{
	public double Low01 { get; set; }
	public double Low1 { get; set; }
	public double AvgArithmetic { get; set; }
	public double AvgHarmonic { get; set; }
	public double Min { get; set; }
	public double Max { get; set; }
	public double P01 { get; set; }
	public double P1 { get; set; }
	public double P5 { get; set; }
	public double P50Median { get; set; }
	public double P95 { get; set; }
	public double P99 { get; set; }
	public double StdDev { get; set; }
	public double Cv { get; set; }
	public double Rmssd { get; set; }
	public double StepwiseRelSD { get; set; }
}
