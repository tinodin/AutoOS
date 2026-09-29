using Windows.Win32;

namespace AutoOS.App.Data.Models.Network;

public static class NetworkSettingComparer
{
	private static readonly string[] Rates = ["Disabled", "Off", "Minimal", "Low", "Medium", "Middle", "High", "Extreme", "Adaptive"];

	public static int Compare(string x, string y)
	{
		if (x == y)
			return 0;
		if (x == null)
			return -1;
		if (y == null)
			return 1;

		bool xIsUsec = x.Contains("usec", StringComparison.OrdinalIgnoreCase);
		bool yIsUsec = y.Contains("usec", StringComparison.OrdinalIgnoreCase);
		bool xIsMsec = x.Contains("msec", StringComparison.OrdinalIgnoreCase);
		bool yIsMsec = y.Contains("msec", StringComparison.OrdinalIgnoreCase);

		if (xIsUsec && yIsMsec)
			return -1;
		if (yIsUsec && xIsMsec)
			return 1;

		bool xIsMbps = x.Contains("Mbps", StringComparison.OrdinalIgnoreCase);
		bool yIsMbps = y.Contains("Mbps", StringComparison.OrdinalIgnoreCase);
		bool xIsGbps = x.Contains("Gbps", StringComparison.OrdinalIgnoreCase);
		bool yIsGbps = y.Contains("Gbps", StringComparison.OrdinalIgnoreCase);

		if (xIsMbps && yIsGbps)
			return -1;
		if (yIsMbps && xIsGbps)
			return 1;

		int xInt = Array.FindIndex(Rates, i => x.Equals(i, StringComparison.OrdinalIgnoreCase));
		int yInt = Array.FindIndex(Rates, i => y.Equals(i, StringComparison.OrdinalIgnoreCase));
		if (xInt != -1 && yInt != -1)
			return xInt.CompareTo(yInt);

		return PInvoke.StrCmpLogical(x, y);
	}
}
