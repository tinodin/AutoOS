using System.Diagnostics;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Media.Audio;

namespace AutoOS.App.Services.Benchmarks;

public static class PresentMonRecordingService
{
	private static readonly string PresentMonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "PresentMon", "PresentMon-x64.exe");

	public static string GenerateOutputPath()
	{
		int recordingNumber = 1;
		string outputPath;
		do
		{
			outputPath = Path.Combine(RecordingAnalysisService.RecordingsDirectory, $"Recording-{recordingNumber++}.csv");
		}
		while (File.Exists(outputPath));

		Directory.CreateDirectory(RecordingAnalysisService.RecordingsDirectory);
		return outputPath;
	}

	public static Process? Start(string processName, string outputPath, int durationSeconds)
	{
		var startInfo = new ProcessStartInfo
		{
			FileName = PresentMonPath,
			Arguments = @$"-session_name AutoOS_{Guid.NewGuid():N} -process_name ""{processName}"" -timed {durationSeconds} -terminate_after_timed -date_time -track_gpu_video -track_frame_type -track_hw_measurements -track_app_timing -track_pc_latency -output_file ""{outputPath}""",
			CreateNoWindow = true
		};

		return Process.Start(startInfo);
	}

	public static void Stop(Process process)
	{
		if (process.HasExited)
			return;

		bool found = false;
		HWND hwnd = HWND.Null;
		while ((hwnd = PInvoke.FindWindowEx((HWND)(IntPtr)(-3), hwnd, "PresentMon", "PresentMonWnd")) != HWND.Null)
		{
			PInvoke.GetWindowThreadProcessId(hwnd, out uint pid);
			if (pid == process.Id)
			{
				PInvoke.PostMessage(hwnd, PInvoke.WM_CLOSE, 0, 0);
				found = true;
				break;
			}
		}
		if (!found)
			process.Kill(true);
	}

	public static void DeleteOutputFile(string outputPath)
	{
		if (File.Exists(outputPath))
			File.Delete(outputPath);
	}

	public static void PlayCompletedSound() =>
		PInvoke.PlaySound(@"C:\Windows\Media\Alarm09.wav", null, SND_FLAGS.SND_FILENAME | SND_FLAGS.SND_ASYNC);
}
