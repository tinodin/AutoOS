using System.Buffers;
using System.Text;
using AutoOS.App.Data.Models.Benchmarks;
using Microsoft.Win32.SafeHandles;
using nietras.SeparatedValues;

namespace AutoOS.App.Services.Benchmarks;

public static class RecordingAnalysisService
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

	public static AnalysisResult? Analyze(string filePath)
	{
		var info = new FileInfo(filePath);
		if (!info.Exists)
			return null;

		List<double> displayChange = [with(4096)];
		List<double> presents = [with(4096)];
		List<double> gpuBusy = [with(4096)];
		List<double> untilDisplayed = [with(4096)];
		List<double> renderPresentLatency = [with(4096)];

		using FileStream stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
		using SepReader reader = Sep.Reader(o => o with { Sep = new Sep(','), Unescape = true, ColNameComparer = StringComparer.OrdinalIgnoreCase }).From(stream, true);

		reader.Header.TryIndexOf("MsBetweenDisplayChange", out int idxDisplayChange);
		reader.Header.TryIndexOf("MsBetweenPresents", out int idxPresents);
		reader.Header.TryIndexOf("MsGPUBusy", out int idxGpuBusy);
		reader.Header.TryIndexOf("MsUntilDisplayed", out int idxUntilDisplayed);
		reader.Header.TryIndexOf("MsRenderPresentLatency", out int idxRenderPresentLatency);

		while (reader.MoveNext())
		{
			SepReader.Row row = reader.Current;
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

		List<double> renderQueueDepth = [with(renderPresentLatency.Count)];
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
			StutterMovingAverage: StatisticsCalculator.ComputeMovingAverage(presents),
			DisplayedFps: StatisticsCalculator.ComputeMetrics(displayChange, isFps: true),
			RenderedFps: StatisticsCalculator.ComputeMetrics(presents, isFps: true),
			MsBetweenDisplayChangeStats: StatisticsCalculator.ComputeMetrics(displayChange, isFps: false),
			MsBetweenPresentsStats: StatisticsCalculator.ComputeMetrics(presents, isFps: false),
			MsGpuBusyStats: StatisticsCalculator.ComputeMetrics(gpuBusy, isFps: false),
			MsUntilDisplayedStats: StatisticsCalculator.ComputeMetrics(untilDisplayed, isFps: false),
			MsRenderPresentLatencyStats: StatisticsCalculator.ComputeMetrics(renderPresentLatency, isFps: false),
			RenderQueueDepthStats: StatisticsCalculator.ComputeMetrics(renderQueueDepth, isFps: false)
		);
	}
}
