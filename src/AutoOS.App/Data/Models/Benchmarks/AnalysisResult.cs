namespace AutoOS.App.Data.Models.Benchmarks;

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
