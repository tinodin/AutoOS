using System.Collections.Concurrent;
using Windows.Win32;

namespace AutoOS.Core.Services.DiskAnalyzer;

internal static class DiskCluster
{
	// P0: cluster size is queried up to 3x per scan (service + scanners) and never
	// changes without a reformat. Cache per volume root; keys are drive roots so
	// the map stays tiny.
	private static readonly ConcurrentDictionary<string, uint> _clusterCache = new(StringComparer.OrdinalIgnoreCase);
	public static long AlignSize(long size, uint cluster, bool isPowerOfTwo)
	{
		if (cluster == 0 || size == 0)
			return size;

		// Cluster is almost always 4096: single AND vs. 64-bit division (~20-40 cycles saved per file).
		if (isPowerOfTwo)
			return (size + (long)cluster - 1L) & ~((long)cluster - 1L);

		return ((size + cluster - 1) / cluster) * cluster;
	}

	public static bool IsPowerOfTwo(uint value)
	{
		return value != 0 && (value & (value - 1)) == 0;
	}

	public static DateTime FromFileTimeUtcOrMinValue(long fileTime)
	{
		if (fileTime == 0)
			return DateTime.MinValue;

		try
		{
			return DateTime.FromFileTimeUtc(fileTime);
		}
		catch (ArgumentOutOfRangeException)
		{
			return DateTime.MinValue;
		}
	}

	public static uint GetClusterSize(string path)
	{
		try
		{
			string root = (Path.GetPathRoot(path) ?? "C:\\").ToUpperInvariant();
			if (_clusterCache.TryGetValue(root, out uint cached))
				return cached;

			uint size = 4096;
			if (PInvoke.GetDiskFreeSpace(root, out uint sPerCluster, out uint bPerSector, out uint _, out uint _))
				size = sPerCluster * bPerSector;

			_clusterCache[root] = size;
			return size;
		}
		catch
		{
			// Volume root not ready yet: assume the 4K NTFS default.
			return 4096;
		}
	}

	public static string NormalizeRoot(string rootPath)
	{
		string root = rootPath.TrimEnd('\\');
		if (root.Length == 2 && root[1] == ':')
			root += "\\";

		return root;
	}
}
