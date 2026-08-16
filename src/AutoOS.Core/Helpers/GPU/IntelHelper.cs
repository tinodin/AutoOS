using System.Diagnostics;
using System.Text.RegularExpressions;
using AutoOS.Core.Common;
using AutoOS.Core.Helpers.Download;
using AutoOS.Core.Helpers.Extract;
using AutoOS.Core.Data.Models.GPU;
using AutoOS.Core.Helpers.Logging;
using AutoOS.Core.Helpers.Registry;
using AutoOS.Core.Helpers.Services;
using Microsoft.Win32;
using Windows.Win32.System.Services;

namespace AutoOS.Core.Helpers.GPU;

public static partial class IntelHelper
{
	[GeneratedRegex(@"(\d+\.\d+\.\d+\.\d+)\s*\(Latest\)", RegexOptions.IgnoreCase)]
	private static partial Regex VersionRegex();

	[GeneratedRegex(@"downloadmirror\.intel\.com\/(\d+)", RegexOptions.IgnoreCase)]
	private static partial Regex IntelIdRegex();

	[GeneratedRegex(@"(gfx_win_[0-9.]+\.zip)", RegexOptions.IgnoreCase)]
	private static partial Regex ZipFileRegex();

	[GeneratedRegex(@"(gfx_win_[0-9.]+\.exe)", RegexOptions.IgnoreCase)]
	private static partial Regex ExeFileRegex();

	[GeneratedRegex(@"(win64_[0-9.]+\.zip)", RegexOptions.IgnoreCase)]
	private static partial Regex LegacyZipFileRegex();

	[GeneratedRegex(@"^WIN_DCA.*\.msi$", RegexOptions.IgnoreCase)]
	private static partial Regex DcaFileRegex();

	public static async Task<(string newestVersion, string newestDownloadUrl)> CheckUpdate(GpuInfo gpu)
	{
		string codename = gpu.Codename;
		string driverPageUrl = string.Empty;
		string newestVersion = string.Empty;
		string newestDownloadUrl = string.Empty;

		static string Normalize(string s) => s.Replace(" ", "").Replace("-", "").ToLowerInvariant();

		string[] intel3 = ["3rd Gen Core processor", "Ivy Bridge"];
		string[] intel4to5 = ["4th Gen Core Processor", "Crystal Well", "Haswell", "Broadwell"];
		string[] intel6 = ["Skylake", "Apollo Lake"];
		string[] intel7to10 = ["Kaby Lake", "Coffee Lake", "Whiskey Lake", "Comet Lake", "Ice Lake", "Lakefield", "Elkhart Lake"];
		string[] intel11to14 = ["Tiger Lake", "Rocket Lake", "Alder Lake", "Raptor Lake", "DG1"];
		string[] intelArc = ["Arc", "Battlemage", "Meteor Lake", "Lunar Lake", "Arrow Lake", "Panther Lake", "DG2"];

		bool is3rd = intel3.Any(c => Normalize(codename).Contains(Normalize(c)));
		bool is4to5 = intel4to5.Any(c => Normalize(codename).Contains(Normalize(c)));
		bool is6th = intel6.Any(c => Normalize(codename).Contains(Normalize(c)));
		bool is7to10 = intel7to10.Any(c => Normalize(codename).Contains(Normalize(c)));
		bool is11to14 = intel11to14.Any(c => Normalize(codename).Contains(Normalize(c)));
		bool isArc = intelArc.Any(c => Normalize(codename).Contains(Normalize(c)));

		if (is3rd)
			driverPageUrl = "https://www.intel.com/content/www/us/en/download/18606/intel-graphics-driver-for-windows-15-33.html";
		else if (is4to5)
			driverPageUrl = "https://www.intel.com/content/www/us/en/download/18369/intel-graphics-driver-for-windows-15-40.html";
		else if (is6th)
			driverPageUrl = "https://www.intel.com/content/www/us/en/download/762755/intel-6th-gen-processor-graphics-windows.html";
		else if (is7to10)
			driverPageUrl = "https://www.intel.com/content/www/us/en/download/776137/intel-7th-10th-gen-processor-graphics-windows.html";
		else if (is11to14)
			driverPageUrl = "https://www.intel.com/content/www/us/en/download/864990/intel-11th-14th-gen-processor-graphics-windows.html";
		else if (isArc)
			driverPageUrl = "https://www.intel.com/content/www/us/en/download/785597/intel-arc-graphics-windows.html";
		else
			LogHelper.LogError(new Exception($"Unsupported Codename: {codename}"), null, null);

		var startInfo = new ProcessStartInfo
		{
			FileName = "curl.exe",
			Arguments = @$"-sL ""{driverPageUrl}""",
			RedirectStandardOutput = true,
			UseShellExecute = false,
			CreateNoWindow = true
		};

		using var process = Process.Start(startInfo)!;
		string domHtml = await process.StandardOutput.ReadToEndAsync();
		await process.WaitForExitAsync();

		Match versionMatch = VersionRegex().Match(domHtml);
		if (versionMatch.Success)
		{
			newestVersion = versionMatch.Groups[1].Value;
			string[] versionParts = newestVersion.Split('.');
			if (versionParts.Length >= 4)
				newestVersion = versionParts[2] + "." + versionParts[3];
		}

		Match fileMatch = (is3rd || is4to5) ? LegacyZipFileRegex().Match(domHtml) : ((is6th) ? ZipFileRegex().Match(domHtml) : ExeFileRegex().Match(domHtml));
		string fileName = fileMatch.Success ? fileMatch.Groups[1].Value : string.Empty;

		Match idMatch = IntelIdRegex().Match(domHtml);
		string fileId = idMatch.Success ? idMatch.Groups[1].Value : string.Empty;

		if (!string.IsNullOrEmpty(fileId) && !string.IsNullOrEmpty(fileName))
			newestDownloadUrl = $"https://downloadmirror.intel.com/{fileId}/{fileName}";

		return (newestVersion, newestDownloadUrl);
	}

	public static List<(string Title, Func<Task> Action, Func<bool>? Condition)> InstallActions(GpuInfo gpu, string newestVersion, string newestDownloadUrl, IStatusReporter? reporter = null)
	{
		string codename = gpu.Codename;
		static string Normalize(string s) => s.Replace(" ", "").Replace("-", "").ToLowerInvariant();

		string[] intel3 = ["3rd Gen Core processor", "Ivy Bridge"];
		string[] intel4to5 = ["Crystal Well", "Haswell", "Broadwell"];
		string[] intel6 = ["Skylake", "Apollo Lake"];
		string[] intel11to14 = ["Tiger Lake", "Rocket Lake", "Alder Lake", "Raptor Lake", "DG1"];
		string[] intelArc = ["Arc", "Battlemage", "Meteor Lake", "Lunar Lake", "Arrow Lake", "Panther Lake"];

		bool Intel_3rd = false;
		bool Intel_4th_5th = false;
		bool Intel_6th = false;
		bool Intel_11th_14th = false;
		bool Intel_Arc = false;

		if (intel3.Any(c => Normalize(codename).Contains(Normalize(c))))
			Intel_3rd = true;
		else if (intel4to5.Any(c => Normalize(codename).Contains(Normalize(c))))
			Intel_4th_5th = true;
		else if (intel6.Any(c => Normalize(codename).Contains(Normalize(c))))
			Intel_6th = true;
		if (intel11to14.Any(c => Normalize(codename).Contains(Normalize(c))))
			Intel_11th_14th = true;
		if (intelArc.Any(c => Normalize(codename).Contains(Normalize(c))))
			Intel_Arc = true;

		var actions = new List<(string Title, Func<Task> Action, Func<bool>? Condition)>
		{
			// download intel driver
			($@"Downloading INTEL driver {newestVersion}", async () => await DownloadHelper.Download(newestDownloadUrl, Path.Combine(Path.GetTempPath(), "INTEL"), "driver.zip", reporter), () => Intel_3rd == true || Intel_4th_5th == true || Intel_6th == true),

			// extract intel driver
			($@"Extracting INTEL driver {newestVersion}", async () => await ExtractHelper.Extract(Path.Combine(Path.GetTempPath(), "INTEL", "driver.zip"), Path.Combine(Path.GetTempPath(), "INTEL", "driver")), () => Intel_3rd == true || Intel_4th_5th == true || Intel_6th == true),

			// download intel driver
			($@"Downloading INTEL driver {newestVersion}", async () => await DownloadHelper.Download(newestDownloadUrl, Path.Combine(Path.GetTempPath(), "INTEL"), "driver.exe", reporter), () => Intel_3rd == false && Intel_4th_5th == false && Intel_6th == false),

			// extract intel driver
			($@"Extracting INTEL driver {newestVersion}", async () => await ExtractHelper.Extract(Path.Combine(Path.GetTempPath(), "INTEL", "driver.exe"), Path.Combine(Path.GetTempPath(), "INTEL", "driver")), () => Intel_3rd == false && Intel_4th_5th == false && Intel_6th == false),

			// stripping intel driver
			($@"Stripping INTEL driver {newestVersion}", async () => File.Delete(Path.Combine(Path.GetTempPath(), "INTEL", "driver", "Resources", "Extras", "Intel-Driver-and-Support-Assistant-Installer.exe")), () => Intel_11th_14th || Intel_Arc),
			($@"Stripping INTEL driver {newestVersion}", async () => { foreach (string file in Directory.GetFiles(Path.Combine(Path.GetTempPath(), "INTEL", "driver", "Resources", "Extras"))) if (DcaFileRegex().IsMatch(Path.GetFileName(file))) File.Delete(file); }, () => Intel_11th_14th || Intel_Arc),

			// update/install intel driver
			(gpu.IsInstalled ?  $"Updating to INTEL driver {newestVersion}" : $"Installing INTEL driver {newestVersion}", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Path.GetTempPath(), "INTEL", "driver", "igxpin.exe"), Arguments = "-s", UseShellExecute = false, CreateNoWindow = true })!.WaitForExitAsync(), () => Intel_3rd == true || Intel_4th_5th == true),
			(gpu.IsInstalled ?  $"Updating to INTEL driver {newestVersion}" : $"Installing INTEL driver {newestVersion}", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Path.GetTempPath(), "INTEL", "driver", "Installer.exe"), Arguments = "/silent", UseShellExecute = false, CreateNoWindow = true })!.WaitForExitAsync(), () => Intel_3rd == false && Intel_4th_5th == false),
			(gpu.IsInstalled ?  $"Updating to INTEL driver {newestVersion}" : $"Installing INTEL driver {newestVersion}", async () => await Task.Delay(3000), null),
			(gpu.IsInstalled ? $"Updating to INTEL driver {newestVersion}" : $"Installing INTEL driver {newestVersion}", async () => GpuHelper.RefreshGpu(gpu), null),
			("Cleaning up INTEL files", async () => Directory.Delete(Path.Combine(Path.GetTempPath(), "INTEL"), true), null)
		};

		return actions;
	}

	public static List<(string Title, Func<Task> Action, Func<bool>? Condition)> TweakActions(GpuInfo gpu)
	{
		string codename = gpu.Codename;
		static string Normalize(string s) => s.Replace(" ", "").Replace("-", "").ToLowerInvariant();

		string[] intel3 = ["3rd Gen Core processor", "Ivy Bridge"];
		string[] intel4to5 = ["Crystal Well", "Haswell", "Broadwell"];
		string[] intel6 = ["Skylake", "Apollo Lake"];
		string[] intel7to10 = ["Kaby Lake", "Coffee Lake", "Whiskey Lake", "Comet Lake", "Ice Lake", "Lakefield", "Elkhart Lake"];
		string[] intel11to14 = ["Tiger Lake", "Rocket Lake", "Alder Lake", "Raptor Lake", "DG1"];
		string[] intelArc = ["Arc", "Battlemage", "Meteor Lake", "Lunar Lake", "Arrow Lake", "Panther Lake"];

		bool Intel_3rd = false;
		bool Intel_4th_5th = false;
		bool Intel_6th = false;
		bool Intel_7th_10th = false;
		bool Intel_11th_14th = false;

		if (intel3.Any(c => Normalize(codename).Contains(Normalize(c))))
			Intel_3rd = true;
		else if (intel4to5.Any(c => Normalize(codename).Contains(Normalize(c))))
			Intel_4th_5th = true;
		else if (intel6.Any(c => Normalize(codename).Contains(Normalize(c))))
			Intel_6th = true;
		else if (intel7to10.Any(c => Normalize(codename).Contains(Normalize(c))))
			Intel_7th_10th = true;
		else if (intel11to14.Any(c => Normalize(codename).Contains(Normalize(c))))
			Intel_11th_14th = true;

		var actions = new List<(string Title, Func<Task> Action, Func<bool>? Condition)>
		{
			// disable intel® graphics software startup entry
			("Disabling Intel® Graphics Software startup entry", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run32", "Intel® Graphics Software", new byte[] { 0x03 }, RegistryValueKind.Binary), null),

			// disable unnecessary services
			("Disabling unnecessary services", async () => ServicesHelper.SetStartupType("cphs", SERVICE_START_TYPE.SERVICE_DISABLED), () => Intel_3rd == true || Intel_4th_5th == true || Intel_6th == true || Intel_7th_10th == true),
			("Disabling unnecessary services", async () => ServicesHelper.StopService("cphs"), () => Intel_3rd == true || Intel_4th_5th == true || Intel_6th == true || Intel_7th_10th == true),
			("Disabling unnecessary services", async () => ServicesHelper.SetStartupType("cplspcon", SERVICE_START_TYPE.SERVICE_DISABLED), () => Intel_6th == true || Intel_7th_10th == true || Intel_11th_14th == true),
			("Disabling unnecessary services", async () => ServicesHelper.StopService("cplspcon"), () => Intel_6th == true || Intel_7th_10th == true || Intel_11th_14th == true),
			("Disabling unnecessary services", async () => ServicesHelper.SetStartupType("igccservice", SERVICE_START_TYPE.SERVICE_DISABLED), () => Intel_6th == true || Intel_7th_10th == true),
			("Disabling unnecessary services", async () => ServicesHelper.StopService("igccservice"), () => Intel_6th == true || Intel_7th_10th == true),
			("Disabling unnecessary services", async () => ServicesHelper.SetStartupType("igfxCUIService1.0.0.0", SERVICE_START_TYPE.SERVICE_DISABLED), () => Intel_3rd == true),
			("Disabling unnecessary services", async () => ServicesHelper.StopService("igfxCUIService1.0.0.0"), () => Intel_3rd == true),
			("Disabling unnecessary services", async () => ServicesHelper.SetStartupType("igfxCUIService2.0.0.0", SERVICE_START_TYPE.SERVICE_DISABLED), () => Intel_4th_5th == true || Intel_6th == true || Intel_7th_10th == true || Intel_11th_14th == true),
			("Disabling unnecessary services", async () => ServicesHelper.StopService("igfxCUIService2.0.0.0"), () => Intel_4th_5th == true || Intel_6th == true || Intel_7th_10th == true || Intel_11th_14th == true),
		
			// disable high-definition multimedia interface (hdmi)/displayport (dp) audio
			("Disabling High-Definition Multimedia Interface (HDMI)/DisplayPort (DP) Audio", async () => GpuHelper.ToggleHdmiDpAudio(gpu, false), () => gpu.HDMIDPAudio == false)
		};

		return actions;
	}
}
