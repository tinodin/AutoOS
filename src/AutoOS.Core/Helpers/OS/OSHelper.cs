using Windows.System.Profile;

namespace AutoOS.Core.Helpers.OS;

public static partial class OSHelper
{
	public static (ushort Major, ushort Minor, ushort Build, ushort Ubr) GetWindowsVersion()
	{
		string deviceFamilyVersion = AnalyticsInfo.VersionInfo.DeviceFamilyVersion;
		ulong version = ulong.Parse(deviceFamilyVersion);
		ushort major = (ushort)((version & 0xFFFF000000000000L) >> 48);
		ushort minor = (ushort)((version & 0x0000FFFF00000000L) >> 32);
		ushort build = (ushort)((version & 0x00000000FFFF0000L) >> 16);
		ushort ubr = (ushort)(version & 0x000000000000FFFFL);

		return (major, minor, build, ubr);
	}

	public static string GetWindowsVersionString()
	{
		(ushort Major, ushort Minor, ushort Build, ushort Ubr) version = GetWindowsVersion();
		return $"{version.Build}.{version.Ubr}";
	}
}
