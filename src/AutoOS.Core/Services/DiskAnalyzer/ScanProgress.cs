namespace AutoOS.Core.Services.DiskAnalyzer;

/// <summary>
/// Phase-by-phase progress a scanner reports while a scan is running. Phases are free-form labels
/// ("Querying Everything", "Building tree", ...) used for tracing and for the scan overlay.
/// </summary>
public sealed class ScanProgressReport
{
	public string Phase { get; init; } = string.Empty;

	public int FilesScanned { get; init; }

	public int TotalFiles { get; init; }

	public double Percent => TotalFiles > 0 ? (double)FilesScanned / TotalFiles * 100 : 0;

	public TimeSpan Elapsed { get; init; }

	public string? Detail { get; init; }
}
