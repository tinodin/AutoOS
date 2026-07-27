using System.Diagnostics;
using System.Globalization;

namespace AutoOS.Core.Helpers.Benchmark;

public enum PresentMonRecordingResult
{
	Saved,
	Stopped,
	NotSaved
}

public sealed class PresentMonRecorder
{
	private Process _process;
	private bool _stopRequested;

	public async Task<PresentMonRecordingResult> RecordAsync(
		string presentMonPath,
		string outputDirectory,
		string processName,
		int durationSeconds,
		int delaySeconds)
	{
		_stopRequested = false;
		Directory.CreateDirectory(outputDirectory);
		var delayTimer = Stopwatch.StartNew();
		while (delayTimer.Elapsed < TimeSpan.FromSeconds(delaySeconds) && !_stopRequested)
			await Task.Delay(100);

		if (_stopRequested)
			return PresentMonRecordingResult.Stopped;

		int recordingNumber = 1;
		string outputPath;
		do
		{
			outputPath = Path.Combine(outputDirectory, $"Recording-{recordingNumber++}.csv");
		}
		while (File.Exists(outputPath));
		var startInfo = new ProcessStartInfo
		{
			FileName = presentMonPath,
			UseShellExecute = true,
			Verb = "runas",
			WindowStyle = ProcessWindowStyle.Hidden
		};
		startInfo.ArgumentList.Add("--process_name");
		startInfo.ArgumentList.Add(GetCanonicalProcessName(processName));
		startInfo.ArgumentList.Add("--timed");
		startInfo.ArgumentList.Add(durationSeconds.ToString(CultureInfo.InvariantCulture));
		startInfo.ArgumentList.Add("--terminate_after_timed");
		startInfo.ArgumentList.Add("--date_time");
		startInfo.ArgumentList.Add("--track_gpu_video");
		startInfo.ArgumentList.Add("--track_frame_type");
		startInfo.ArgumentList.Add("--track_hw_measurements");
		startInfo.ArgumentList.Add("--track_app_timing");
		startInfo.ArgumentList.Add("--track_pc_latency");
		startInfo.ArgumentList.Add("--output_file");
		startInfo.ArgumentList.Add(outputPath);

		_process = Process.Start(startInfo) ??
			throw new InvalidOperationException("PresentMon could not be started.");

		try
		{
			await _process.WaitForExitAsync();
		}
		finally
		{
			_process.Dispose();
			_process = null;
		}

		if (_stopRequested)
			return PresentMonRecordingResult.Stopped;

		return File.Exists(outputPath) && new FileInfo(outputPath).Length > 0
			? PresentMonRecordingResult.Saved
			: PresentMonRecordingResult.NotSaved;
	}

	public void Stop()
	{
		_stopRequested = true;
		if (_process != null && !_process.HasExited)
			_process.Kill(entireProcessTree: true);
	}

	private static string GetCanonicalProcessName(string processName)
	{
		Process[] matches = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(processName));
		try
		{
			foreach (Process process in matches)
			{
				try
				{
					if (!process.HasExited)
						return $"{process.ProcessName}.exe";
				}
				catch (InvalidOperationException)
				{
				}
			}
		}
		finally
		{
			foreach (Process process in matches)
				process.Dispose();
		}

		return processName;
	}
}
