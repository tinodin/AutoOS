using AutoOS.Core.Models;

namespace AutoOS.Core.Helpers.Benchmark;

public static class RecordingAnalyzer
{
    private static string[] ColumnNames => [.. BenchmarkCsv.MetricDescriptions.Keys.Where(key => !key.Contains("FPS", StringComparison.Ordinal))];

    public sealed record AnalysisResult(
        string FilePath,
        string FileName,
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

    public static AnalysisResult Analyze(string filePath)
    {
        var info = new FileInfo(filePath);
        if (!info.Exists)
            return null;

        Dictionary<string, int> headerIndex;
        using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
        using (var reader = new StreamReader(fs))
        {
            var headerLine = reader.ReadLine();
            if (string.IsNullOrWhiteSpace(headerLine))
                return null;
            var headers = BenchmarkCsv.ParseCsvLine(headerLine);
            headerIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < headers.Count; i++)
                headerIndex[headers[i].Trim()] = i;
        }

        int[] colIndices = new int[ColumnNames.Length];
        for (int i = 0; i < ColumnNames.Length; i++)
        {
            if (!headerIndex.TryGetValue(ColumnNames[i], out int idx))
                colIndices[i] = -1;
            else
                colIndices[i] = idx;
        }

        List<double>[] columns = [new(4096), new(4096), new(4096), new(4096)];

        using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
        using (var reader = new StreamReader(fs))
        {
            reader.ReadLine();
            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                if (string.IsNullOrWhiteSpace(line))
                    continue;
                var values = BenchmarkCsv.ParseCsvLine(line);
                for (int i = 0; i < ColumnNames.Length; i++)
                {
                    if (colIndices[i] < 0 || colIndices[i] >= values.Count)
                        continue;
                    if (double.TryParse(values[colIndices[i]], System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out double v))
                        columns[i].Add(v);
                }
            }
        }

        static Metrics computeFps(List<double> raw) => raw.Count == 0 ? new Metrics() : BenchmarkStatistics.CalculateMetrics([.. raw.Where(v => v > 0).Select(v => 1000.0 / v)], isFpsMetric: true);

        static Metrics computeLatency(List<double> raw) => raw.Count == 0 ? new Metrics() : BenchmarkStatistics.CalculateMetrics([.. raw], isFpsMetric: false);

        return new AnalysisResult(
            FilePath: filePath,
            FileName: info.Name,
            MsBetweenDisplayChange: columns[0],
            MsBetweenPresents: columns[1],
            MsGPUBusy: columns[2],
            MsUntilDisplayed: columns[3],
            DisplayedFps: computeFps(columns[0]),
            RenderedFps: computeFps(columns[1]),
            MsBetweenDisplayChangeStats: computeLatency(columns[0]),
            MsBetweenPresentsStats: computeLatency(columns[1]),
            MsGpuBusyStats: computeLatency(columns[2]),
            MsUntilDisplayedStats: computeLatency(columns[3])
        );
    }
}
