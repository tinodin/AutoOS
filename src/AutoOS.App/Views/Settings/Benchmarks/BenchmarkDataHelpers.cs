using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;

namespace AutoOS.Views.Settings.Benchmarks;

internal static class BenchmarkCsv
{
	public static string NormalizeHeader(string value)
	{
		if (string.IsNullOrWhiteSpace(value))
			return string.Empty;

		var span = value.AsSpan().Trim();
		var normalized = new StringBuilder(span.Length);
		foreach (char character in span)
		{
			if (char.IsLetterOrDigit(character))
				normalized.Append(char.ToLowerInvariant(character));
		}
		return normalized.ToString();
	}

	public static bool TryParseDouble(string value, out double result)
	{
		value = value.Trim();
		if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result) ||
			double.TryParse(value, NumberStyles.Float, CultureInfo.CurrentCulture, out result))
		{
			return true;
		}

		if (value.Contains(','))
		{
			string normalized = value.Replace(',', '.');
			return double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out result);
		}

		return false;
	}

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
	public static bool TryGetPercentileFromSorted(double[] ordered, double percentile, out double value)
	{
		value = 0;
		if (ordered.Length == 0)
			return false;

		double position = percentile / 100.0 * (ordered.Length - 1);
		int lower = (int)Math.Floor(position);
		int upper = (int)Math.Ceiling(position);
		if (lower == upper)
		{
			value = ordered[lower];
			return true;
		}

		double fraction = position - lower;
		value = ordered[lower] * (1 - fraction) + ordered[upper] * fraction;
		return true;
	}

	public static bool TryComputeTimeSeriesStats(
		IReadOnlyList<double> values,
		out (double stepwiseRelSD, double cv, double rmssd, double stdDev) stats)
	{
		stats = (0, 0, 0, 0);
		if (values.Count < 2)
			return false;

		double mean = values.Average();
		double sumSquaredDifference = values.Sum(value => Math.Pow(value - mean, 2));
		double standardDeviation = Math.Sqrt(sumSquaredDifference / values.Count);
		double coefficientOfVariation = mean != 0 ? standardDeviation / mean : 0;
		double consecutiveDifference = 0;
		double relativeDifference = 0;
		int validPairs = 0;

		for (int index = 1; index < values.Count; index++)
		{
			double difference = values[index] - values[index - 1];
			consecutiveDifference += difference * difference;
			if (values[index - 1] == 0)
				continue;

			double relative = difference / values[index - 1];
			relativeDifference += relative * relative;
			validPairs++;
		}

		double rmssd = Math.Sqrt(consecutiveDifference / (values.Count - 1));
		double stepwiseRelative = validPairs > 0 ? Math.Sqrt(relativeDifference / validPairs) : 0;
		stats = (stepwiseRelative, coefficientOfVariation, rmssd, standardDeviation);
		return true;
	}
}
