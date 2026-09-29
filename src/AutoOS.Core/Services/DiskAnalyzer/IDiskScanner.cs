namespace AutoOS.Core.Services.DiskAnalyzer;

/// <summary>
/// One way of turning a root path into flat <see cref="ScanItem"/> rows. Implementations differ in
/// speed and fidelity: the Everything index, the direct MFT parse and the parallel file-system
/// walker all satisfy this contract so the analyzer can fall back between them.
/// </summary>
public interface IDiskScanner
{
	/// <summary>
	/// Scanner identity reported on <see cref="ScanResult.UsedScanner"/>.
	/// </summary>
	ScannerKind Kind { get; }

	/// <summary>
	/// Whether the scanner can run right now (for example, whether the Everything server is up).
	/// </summary>
	bool IsAvailable { get; }

	/// <summary>
	/// Asynchronously verifies availability and warms whatever the scanner needs before
	/// <see cref="ScanAsync"/> is timed.
	/// </summary>
	Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);

	/// <summary>
	/// Enumerates every row under the scan root. Rows arrive in an unordered, depth-first shape;
	/// callers build the tree from the file paths.
	/// </summary>
	Task<IReadOnlyList<ScanItem>> ScanAsync(ScanOptions options, IProgress<double>? progress = null, CancellationToken cancellationToken = default);
}
