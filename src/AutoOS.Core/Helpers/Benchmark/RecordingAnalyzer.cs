using AutoOS.Core.Helpers.Benchmark.Models;
using nietras.SeparatedValues;

namespace AutoOS.Core.Helpers.Benchmark;

public sealed record AnalysisResult(
	IReadOnlyList<double> MsBetweenDisplayChange,
	IReadOnlyList<double> MsBetweenPresents,
	IReadOnlyList<double> MsGPUBusy,
	IReadOnlyList<double> MsUntilDisplayed,
	Metrics DisplayedFps,
	Metrics RenderedFps,
	Metrics MsBetweenDisplayChangeStats,
	Metrics MsBetweenPresentsStats,
	Metrics MsGpuBusyStats,
	Metrics MsUntilDisplayedStats
);

public static class RecordingAnalyzer
{
	public static AnalysisResult Analyze(string filePath)
	{
		var info = new FileInfo(filePath);
		if (!info.Exists)
			return null;

		List<double> displayChange = new(4096);
		List<double> presents = new(4096);
		List<double> gpuBusy = new(4096);
		List<double> untilDisplayed = new(4096);

		using var reader = Sep.Reader(o => o with { Sep = new Sep(','), Unescape = true, ColNameComparer = StringComparer.OrdinalIgnoreCase }).FromFile(filePath);

		reader.Header.TryIndexOf("MsBetweenDisplayChange", out int idxDisplayChange);
		reader.Header.TryIndexOf("MsBetweenPresents", out int idxPresents);
		reader.Header.TryIndexOf("MsGPUBusy", out int idxGpuBusy);
		reader.Header.TryIndexOf("MsUntilDisplayed", out int idxUntilDisplayed);

		while (reader.MoveNext())
		{
			var row = reader.Current;
			if (idxDisplayChange >= 0 && idxDisplayChange < row.ColCount &&
				row[idxDisplayChange].TryParse(out double displayChangeValue))
				displayChange.Add(displayChangeValue);
			if (idxPresents >= 0 && idxPresents < row.ColCount &&
				row[idxPresents].TryParse(out double presentsValue))
				presents.Add(presentsValue);
			if (idxGpuBusy >= 0 && idxGpuBusy < row.ColCount &&
				row[idxGpuBusy].TryParse(out double gpuBusyValue))
				gpuBusy.Add(gpuBusyValue);
			if (idxUntilDisplayed >= 0 && idxUntilDisplayed < row.ColCount &&
				row[idxUntilDisplayed].TryParse(out double untilDisplayedValue))
				untilDisplayed.Add(untilDisplayedValue);
		}

		return new AnalysisResult(
			MsBetweenDisplayChange: displayChange,
			MsBetweenPresents: presents,
			MsGPUBusy: gpuBusy,
			MsUntilDisplayed: untilDisplayed,
			DisplayedFps: ComputeMetrics(displayChange, isFps: true),
			RenderedFps: ComputeMetrics(presents, isFps: true),
			MsBetweenDisplayChangeStats: ComputeMetrics(displayChange, isFps: false),
			MsBetweenPresentsStats: ComputeMetrics(presents, isFps: false),
			MsGpuBusyStats: ComputeMetrics(gpuBusy, isFps: false),
			MsUntilDisplayedStats: ComputeMetrics(untilDisplayed, isFps: false)
		);
	}

	static Metrics ComputeMetrics(List<double> raw, bool isFps)
	{
		if (raw.Count == 0)
			return new Metrics();
		var values = isFps ? raw.Where(v => v > 0).Select(v => 1000.0 / v).ToArray() : [.. raw];
		return BenchmarkStatistics.CalculateMetrics(values, isFpsMetric: isFps);
	}
}