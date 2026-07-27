using AutoOS.Core.Models;

namespace AutoOS.Core.Helpers.Benchmark;

public static class BenchmarkStatistics
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

		result.P01 = Percentile(sorted, isFpsMetric ? 0.1 : 99.9);
		result.P1 = Percentile(sorted, isFpsMetric ? 1 : 99);
		result.P5 = Percentile(sorted, isFpsMetric ? 5 : 95);
		result.P50Median = Percentile(sorted, 50);
		result.P95 = Percentile(sorted, isFpsMetric ? 95 : 5);
		result.P99 = Percentile(sorted, isFpsMetric ? 99 : 1);

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
