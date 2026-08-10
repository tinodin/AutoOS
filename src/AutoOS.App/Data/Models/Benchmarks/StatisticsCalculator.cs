namespace AutoOS.App.Data.Models.Benchmarks;

public static class StatisticsCalculator
{
	public static Metrics ComputeMetrics(List<double> raw, bool isFps)
	{
		if (raw.Count == 0)
			return new Metrics();
		double[] values = isFps ? raw.Where(v => v > 0).Select(v => 1000.0 / v).ToArray() : [.. raw];
		return CalculateMetrics(values, isFpsMetric: isFps);
	}

	private static Metrics CalculateMetrics(double[] values, bool isFpsMetric)
	{
		var result = new Metrics();
		if (values.Length == 0)
			return result;

		double[] sorted = [.. values.OrderBy(v => v)];
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
			double[] desc = sorted.Reverse().ToArray();
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
			if (values[i - 1] == 0)
				continue;
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
		foreach (double v in values)
		{
			if (v > 0)
			{
				reciprocalSum += 1.0 / v;
				count++;
			}
		}
		return count > 0 ? count / reciprocalSum : 0;
	}

	public static IReadOnlyList<double> ComputeMovingAverage(IReadOnlyList<double> sequence)
	{
		if (sequence.Count == 0)
			return [];

		int sampleSize = Convert.ToInt32(Math.Sqrt(sequence.Average()) * 10);
		double[] result = new double[sequence.Count];

		for (int i = 0; i < sequence.Count; i++)
		{
			int localIndex = i;
			double localSum = 0;
			int localCount = 0;

			while (localIndex >= 0)
			{
				localSum += localIndex > 0 && sequence[localIndex] > sequence[localIndex - 1] * 3 ? sequence[localIndex - 1] : sequence[localIndex];
				localCount++;

				if (localCount >= sampleSize)
					break;

				localIndex--;
			}

			result[i] = localSum / localCount;
		}

		return result;
	}

	public static double GetLowFPSTimePercentage(IReadOnlyList<double> sequence, IReadOnlyList<double> movingAverage, double stutteringFactor, double lowFPSThreshold)
	{
		if (sequence.Count == 0 || movingAverage.Count != sequence.Count)
			return 0;

		double lowFPSTime = 0;
		double totalTime = 0;

		for (int i = 0; i < sequence.Count; i++)
		{
			totalTime += sequence[i];
			if (sequence[i] <= 0)
				continue;
			if (sequence[i] <= stutteringFactor * movingAverage[i] && 1000 / sequence[i] < lowFPSThreshold)
				lowFPSTime += sequence[i];
		}

		return totalTime == 0 ? 0 : 100 * lowFPSTime / totalTime;
	}

	public static double GetStutteringTimePercentage(IReadOnlyList<double> sequence, IReadOnlyList<double> movingAverage, double stutteringFactor)
	{
		if (sequence.Count == 0 || movingAverage.Count != sequence.Count)
			return 0;

		double stutteringTime = 0;
		double totalTime = 0;

		for (int i = 0; i < sequence.Count; i++)
		{
			totalTime += sequence[i];
			if (sequence[i] > stutteringFactor * movingAverage[i])
				stutteringTime += sequence[i];
		}

		return totalTime == 0 ? 0 : 100 * stutteringTime / totalTime;
	}

	public static Metrics GetStutterMetrics(AnalysisResult result, double stutterFactor = 2.5, double lowFpsThreshold = 25)
	{
		double stutter = GetStutteringTimePercentage(result.MsBetweenPresents, result.StutterMovingAverage, stutterFactor);
		double lowFps = GetLowFPSTimePercentage(result.MsBetweenPresents, result.StutterMovingAverage, stutterFactor, lowFpsThreshold);
		double smooth = Math.Max(0, 100 - stutter - lowFps);

		return new Metrics
		{
			Smooth = smooth,
			LowFPS = lowFps,
			Stuttering = stutter,
			TotalSeconds = result.MsBetweenPresents.Sum() / 1000,
			DisplayedAdaptiveStdDev = GetAdaptiveStandardDeviation(result.MsBetweenDisplayChange),
			RenderedAdaptiveStdDev = GetAdaptiveStandardDeviation(result.MsBetweenPresents)
		};
	}

	public static double GetAdaptiveStandardDeviation(IReadOnlyList<double> sequence, double windowMs = 500)
	{
		if (sequence.Count == 0)
			return 0;

		double average = sequence.Average();
		if (average <= 0)
			return 0;

		int sampleSize = Math.Max(1, Convert.ToInt32(windowMs / average));

		double stdDevSum = 0;
		int windowCount = 0;
		for (int start = 0; start < sequence.Count; start += sampleSize)
		{
			int count = Math.Min(sampleSize, sequence.Count - start);

			double mean = 0;
			for (int i = start; i < start + count; i++)
				mean += sequence[i];
			mean /= count;

			double variance = 0;
			for (int i = start; i < start + count; i++)
			{
				double deviation = sequence[i] - mean;
				variance += deviation * deviation;
			}

			stdDevSum += Math.Sqrt(variance / count);
			windowCount++;
		}

		return stdDevSum / windowCount;
	}
}
