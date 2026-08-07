using System.Globalization;

namespace AutoOS.App.Data.Models.Benchmarks;

public readonly record struct Statistics(
	string Label,
	string Description,
	string Format,
	string Suffix,
	string DeltaSuffix,
	bool HigherIsBetter,
	Func<double, Metrics, string>? Formatter = null)
{
	public string FormatValue(double value, Metrics metrics)
		=> Formatter is null ? value.ToString(Format, CultureInfo.CurrentCulture) + Suffix : Formatter(value, metrics);
}
