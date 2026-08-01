using AutoOS.Core.Helpers.Benchmark.Models;
using nietras.SeparatedValues;

namespace AutoOS.Core.Helpers.Benchmark;

public sealed record AnalysisResult(
    IReadOnlyList<double> MsBetweenDisplayChange,
    IReadOnlyList<double> MsBetweenPresents,
    IReadOnlyList<double> MsGPUBusy,
    IReadOnlyList<double> MsUntilDisplayed,
    IReadOnlyList<double> MsRenderPresentLatency,
    IReadOnlyList<double> RenderQueueDepth,
    IReadOnlyList<double> StutterMovingAverage,
    Metrics DisplayedFps,
    Metrics RenderedFps,
    Metrics MsBetweenDisplayChangeStats,
    Metrics MsBetweenPresentsStats,
    Metrics MsGpuBusyStats,
    Metrics MsUntilDisplayedStats,
	Metrics MsRenderPresentLatencyStats,
	Metrics RenderQueueDepthStats
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
        List<double> renderPresentLatency = new(4096);

        using var reader = Sep.Reader(o => o with { Sep = new Sep(','), Unescape = true, ColNameComparer = StringComparer.OrdinalIgnoreCase }).FromFile(filePath);

        reader.Header.TryIndexOf("MsBetweenDisplayChange", out int idxDisplayChange);
        reader.Header.TryIndexOf("MsBetweenPresents", out int idxPresents);
        reader.Header.TryIndexOf("MsGPUBusy", out int idxGpuBusy);
        reader.Header.TryIndexOf("MsUntilDisplayed", out int idxUntilDisplayed);
        reader.Header.TryIndexOf("MsRenderPresentLatency", out int idxRenderPresentLatency);

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
            if (idxRenderPresentLatency >= 0 && idxRenderPresentLatency < row.ColCount &&
                row[idxRenderPresentLatency].TryParse(out double renderPresentLatencyValue))
                renderPresentLatency.Add(renderPresentLatencyValue);
        }

        List<double> renderQueueDepth = new(renderPresentLatency.Count);
        for (int i = 0; i < renderPresentLatency.Count; i++)
        {
            renderQueueDepth.Add(presents[i] > 0 ? renderPresentLatency[i] / presents[i] : 0);
        }

        return new AnalysisResult(
            MsBetweenDisplayChange: displayChange,
            MsBetweenPresents: presents,
            MsGPUBusy: gpuBusy,
            MsUntilDisplayed: untilDisplayed,
            MsRenderPresentLatency: renderPresentLatency,
            RenderQueueDepth: renderQueueDepth,
            StutterMovingAverage: ComputeMovingAverage(presents),
            DisplayedFps: ComputeMetrics(displayChange, isFps: true),
            RenderedFps: ComputeMetrics(presents, isFps: true),
            MsBetweenDisplayChangeStats: ComputeMetrics(displayChange, isFps: false),
            MsBetweenPresentsStats: ComputeMetrics(presents, isFps: false),
            MsGpuBusyStats: ComputeMetrics(gpuBusy, isFps: false),
            MsUntilDisplayedStats: ComputeMetrics(untilDisplayed, isFps: false),
            MsRenderPresentLatencyStats: ComputeMetrics(renderPresentLatency, isFps: false),
            RenderQueueDepthStats: ComputeMetrics(renderQueueDepth, isFps: false)
        );
    }

    private static Metrics ComputeMetrics(List<double> raw, bool isFps)
    {
        if (raw.Count == 0)
            return new Metrics();
        var values = isFps ? raw.Where(v => v > 0).Select(v => 1000.0 / v).ToArray() : [.. raw];
        return BenchmarkStatistics.CalculateMetrics(values, isFpsMetric: isFps);
    }

    public static IReadOnlyList<double> ComputeMovingAverage(IReadOnlyList<double> sequence)
    {
        if (sequence.Count == 0)
            return [];

        int sampleSize = Convert.ToInt32(Math.Sqrt(sequence.Average()) * 10);
        var result = new double[sequence.Count];

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
