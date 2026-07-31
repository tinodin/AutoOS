using System.ComponentModel;
using System.Diagnostics;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Session;

namespace AutoOS.Core.Helpers.Benchmark;

public sealed partial class PresentMonProcessDiscovery : IDisposable
{
	private static readonly Guid DxgKrnlProvider = new("802EC45A-1E99-4B83-9920-87C98277BA9D");
	private static readonly Guid DxgiProvider = new("CA11C036-0102-4A2D-A6AD-F03CFED5D3C9");
	private static readonly Guid D3D9Provider = new("783ACA0A-790E-4D7F-8451-AA850511C6B9");
	private static readonly Guid KernelProcessProvider = new("22FB2CD6-0E7B-422B-A0C7-2FAD1FD0E716");
	private const ulong PresentHistoryKeyword = 0x08000000;
	private const ulong DxgiKeyword = 0x3;
	private const ulong RuntimePresentKeyword = 0x2;
	private const ulong ProcessKeyword = 0x10;
	private const int ProcessStartEventId = 0x0001;
	private const int ProcessStopEventId = 0x0002;
	private const int ProcessRundownEventId = 0x000F;
	private const int PresentHistoryStartEventId = 0x00AB;
	private const int PresentHistoryDetailedStartEventId = 0x00D7;
	private const int DxgiPresentStartEventId = 0x002A;
	private const int DxgiPresentStopEventId = 0x002B;
	private const int DxgiPresentMultiplaneOverlayStartEventId = 0x0037;
	private const int DxgiPresentMultiplaneOverlayStopEventId = 0x0038;
	private const int DxgiSwapChainStartEventId = 0x000A;
	private const int D3D9PresentStartEventId = 0x0001;
	private const int D3D9PresentStopEventId = 0x0002;
	private const uint DxgiPresentTest = 0x1;
	private const uint DxgiStatusOccluded = 0x087A0001;
	private const uint DxgiStatusNoDesktopAccess = 0x087A0005;
	private const uint DxgiStatusModeChangeInProgress = 0x087A0008;
	private const uint RedirectedCompositionModel = 7;
	private static readonly HashSet<string> ExcludedProcessNames = new(StringComparer.OrdinalIgnoreCase)
	{
		"ApplicationFrameHost.exe",
		"audiodg.exe",
		"AutoOS.exe",
		"backgroundTaskHost.exe",
		"conhost.exe",
		"CrossDeviceResume.exe",
		"csrss.exe",
		"ctfmon.exe",
		"dllhost.exe",
		"Discord.exe",
		"dwm.exe",
		"explorer.exe",
		"fontdrvhost.exe",
		"LockApp.exe",
		"lsass.exe",
		"Memory Compression.exe",
		"msedgeview.exe",
		"msedgewebview2.exe",
		"PresentMon.exe",
		"Registry.exe",
		"RuntimeBroker.exe",
		"SearchIndexer.exe",
		"SecurityHealthService.exe",
		"SecurityHealthSystray.exe",
		"services.exe",
		"sihost.exe",
		"smss.exe",
		"spoolsv.exe",
		"SearchHost.exe",
		"ShellExperienceHost.exe",
		"ShellHost.exe",
		"StartMenuExperienceHost.exe",
		"svchost.exe",
		"System.exe",
		"SystemIdleProcess.exe",
		"SystemSettings.exe",
		"taskhostw.exe",
		"TextInputHost.exe",
		"wininit.exe",
		"winlogon.exe",
		"Widgets.exe",
		"WidgetService.exe",
		"WmiPrvSE.exe"
	};

	private readonly Lock _sync = new();
	private readonly Dictionary<int, ProcessIdentity> _runningProcesses = [];
	private readonly Dictionary<int, ProcessIdentity> _presentingProcesses = [];
	private readonly HashSet<RuntimePresent> _runtimePresents = [];
	private readonly HashSet<string> _snapshotCandidates = new(StringComparer.OrdinalIgnoreCase);
	private readonly HashSet<string> _redirectedCompositionProcesses = new(StringComparer.OrdinalIgnoreCase);
	private readonly HashSet<string> _confirmedPresentingProcesses = new(StringComparer.OrdinalIgnoreCase);
	private TraceEventSession _session;
	private bool _started;

	public event EventHandler ProcessesChanged;

	public void Start()
	{
		lock (_sync)
		{
			if (_started)
				return;

			_started = true;
		}

		try
		{
			_session = new TraceEventSession($"PresentDiscovery.{Environment.ProcessId}")
			{
				StopOnDispose = true
			};

			var parser = new RegisteredTraceEventParser(_session.Source);
			parser.All += ProcessTraceEvent;
			_session.EnableProvider(KernelProcessProvider, TraceEventLevel.Informational, ProcessKeyword);
			_session.EnableProvider(DxgKrnlProvider, TraceEventLevel.Informational, PresentHistoryKeyword);
			_session.EnableProvider(DxgiProvider, TraceEventLevel.Verbose, DxgiKeyword);
			_session.EnableProvider(D3D9Provider, TraceEventLevel.Verbose, RuntimePresentKeyword);

			TraceEventSession session = _session;
			var traceThread = new Thread(() => ProcessEvents(session))
			{
				IsBackground = true,
				Name = "PresentMon process discovery"
			};
			traceThread.Start();

			_session.CaptureState(KernelProcessProvider, ProcessKeyword);
			_session.CaptureState(DxgiProvider, DxgiKeyword);
		}
		catch
		{
			_session?.Dispose();
			_session = null;
			lock (_sync)
			{
				_started = false;
			}
		}
	}

	public List<string> GetRecordableProcesses(bool refreshRunningProcesses = false)
	{
		if (refreshRunningProcesses)
			RefreshRunningProcesses();

		lock (_sync)
		{
			return [.. _presentingProcesses.Values
				.Select(process => process.Name)
				.Concat(_snapshotCandidates)
				.Where(name => !ExcludedProcessNames.Contains(name))
				.Where(name => !_redirectedCompositionProcesses.Contains(name) ||
					_confirmedPresentingProcesses.Contains(name))
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.OrderBy(name => name, StringComparer.OrdinalIgnoreCase)];
		}
	}

	private static void ProcessEvents(TraceEventSession session)
	{
		var defaultListener = Trace.Listeners.OfType<DefaultTraceListener>().FirstOrDefault();
		if (defaultListener != null)
			Trace.Listeners.Remove(defaultListener);

		try
		{
			session.Source.Process();
		}
		catch
		{	}
	}

	private void ProcessTraceEvent(TraceEvent traceEvent)
	{
		if (traceEvent.ProviderGuid == KernelProcessProvider)
		{
			ProcessKernelProcessEvent(traceEvent);
			return;
		}

		if (traceEvent.ProviderGuid == DxgKrnlProvider)
		{
			ProcessDxgKrnlEvent(traceEvent);
			return;
		}

		if (traceEvent.ProviderGuid == DxgiProvider)
		{
			ProcessDxgiEvent(traceEvent);
			return;
		}

		if (traceEvent.ProviderGuid == D3D9Provider)
			ProcessD3D9Event(traceEvent);
	}

	private void ProcessKernelProcessEvent(TraceEvent processEvent)
	{
		int eventId = (int)processEvent.ID;
		if (!TryReadUInt32(processEvent, "ProcessID", out uint processIdValue) || processIdValue > int.MaxValue)
			return;

		int processId = (int)processIdValue;
		if (eventId == ProcessStopEventId)
		{
			string stoppedProcessName = null;
			lock (_sync)
			{
				if (_runningProcesses.TryGetValue(processId, out ProcessIdentity runningProcess))
					stoppedProcessName = runningProcess.Name;
				else if (_presentingProcesses.TryGetValue(processId, out ProcessIdentity presentingProcess))
					stoppedProcessName = presentingProcess.Name;
				_runningProcesses.Remove(processId);
				_presentingProcesses.Remove(processId);
				_runtimePresents.RemoveWhere(present => present.ProcessId == processId);
			}
			if (stoppedProcessName is not null)
			{
				string nameWithoutExtension;
				bool processIsStillRunning = false;
				try
				{
					nameWithoutExtension = Path.GetFileNameWithoutExtension(stoppedProcessName);
					Process[] matchingProcesses = Process.GetProcessesByName(nameWithoutExtension);
					foreach (Process process in matchingProcesses)
					{
						try
						{
							processIsStillRunning |= !process.HasExited;
						}
						catch (InvalidOperationException)
						{
						}
						catch (Win32Exception)
						{
						}
						finally
						{
							process.Dispose();
						}
					}
				}
				catch (ArgumentException)
				{
				}
				catch (Win32Exception)
				{
				}
				if (!processIsStillRunning)
				{
					lock (_sync)
					{
						_snapshotCandidates.Remove(stoppedProcessName);
						_redirectedCompositionProcesses.Remove(stoppedProcessName);
						_confirmedPresentingProcesses.Remove(stoppedProcessName);
					}
					ProcessesChanged?.Invoke(this, EventArgs.Empty);
				}
			}
			return;
		}

		if (eventId != ProcessStartEventId && eventId != ProcessRundownEventId)
			return;

		int nameIdx = Array.IndexOf(processEvent.PayloadNames, "ImageName");
		string processName = nameIdx >= 0 ? processEvent.PayloadString(nameIdx) : null;
		if (processName is not null)
			RememberRunningProcess(processId, processName);
		else
			RememberRunningProcess(processId);
	}

	private void ProcessDxgiEvent(TraceEvent presentEvent)
	{
		int eventId = (int)presentEvent.ID;
		if (eventId == DxgiSwapChainStartEventId)
		{
			RememberPresentingProcess(presentEvent.ProcessID);
			return;
		}

		if (eventId == DxgiPresentStartEventId || eventId == DxgiPresentMultiplaneOverlayStartEventId)
		{
			if (!TryReadUInt32(presentEvent, "Flags", out uint flags) || (flags & DxgiPresentTest) != 0)
				return;

			RememberRuntimePresentStart(DxgiProvider, presentEvent);
			return;
		}

		if (eventId != DxgiPresentStopEventId && eventId != DxgiPresentMultiplaneOverlayStopEventId)
			return;

		if (!CompleteRuntimePresent(DxgiProvider, presentEvent, out int processId) || !TryReadUInt32(presentEvent, "Result", out uint result) ||
			(result & 0x80000000) != 0 ||
			result == DxgiStatusOccluded ||
			result == DxgiStatusNoDesktopAccess ||
			result == DxgiStatusModeChangeInProgress)
		{
			return;
		}

		RememberPresentingProcess(processId);
	}

	private void ProcessD3D9Event(TraceEvent presentEvent)
	{
		int eventId = (int)presentEvent.ID;
		if (eventId == D3D9PresentStartEventId)
		{
			RememberRuntimePresentStart(D3D9Provider, presentEvent);
			return;
		}

		if (eventId != D3D9PresentStopEventId || !CompleteRuntimePresent(D3D9Provider, presentEvent, out int processId) || !TryReadUInt32(presentEvent, "Result", out uint result) ||
			(result & 0x80000000) != 0)
		{
			return;
		}

		RememberPresentingProcess(processId);
	}

	private void RememberRuntimePresentStart(Guid provider, TraceEvent presentEvent)
	{
		lock (_sync)
		{
			_runtimePresents.Add(new RuntimePresent(provider, presentEvent.ProcessID, presentEvent.ThreadID));
		}
	}

	private bool CompleteRuntimePresent(Guid provider, TraceEvent presentEvent, out int processId)
	{
		var present = new RuntimePresent(provider, presentEvent.ProcessID, presentEvent.ThreadID);
		processId = present.ProcessId;
		lock (_sync)
		{
			return _runtimePresents.Remove(present);
		}
	}

	private void ProcessDxgKrnlEvent(TraceEvent presentEvent)
	{
		int eventId = (int)presentEvent.ID;
		if ((eventId != PresentHistoryStartEventId && eventId != PresentHistoryDetailedStartEventId) || !TryReadUInt32(presentEvent, "Model", out uint model) || !TryGetProcessIdentity(presentEvent.ProcessID, out ProcessIdentity identity))
		{
			return;
		}

		if (model == RedirectedCompositionModel)
		{
			bool processWasExcluded = false;
			lock (_sync)
			{
				if (!_confirmedPresentingProcesses.Contains(identity.Name))
					processWasExcluded = _redirectedCompositionProcesses.Add(identity.Name);
			}
			if (processWasExcluded)
				ProcessesChanged?.Invoke(this, EventArgs.Empty);
			return;
		}

		bool processWasRestored;
		lock (_sync)
		{
			_confirmedPresentingProcesses.Add(identity.Name);
			processWasRestored = _redirectedCompositionProcesses.Remove(identity.Name);
		}

		RememberPresentingProcess(presentEvent.ProcessID);
		if (processWasRestored)
			ProcessesChanged?.Invoke(this, EventArgs.Empty);
	}

	private void RefreshRunningProcesses()
	{
		Dictionary<string, List<Process>> processGroups = new(StringComparer.OrdinalIgnoreCase);

		foreach (Process process in Process.GetProcesses())
		{
			if (!TryGetProcessName(process, out string name) || ExcludedProcessNames.Contains(name) || process.Id == Environment.ProcessId)
			{
				process.Dispose();
				continue;
			}

			if (!processGroups.TryGetValue(name, out List<Process> processes))
				processGroups[name] = processes = [];
			processes.Add(process);
		}

		HashSet<string> candidates = new(StringComparer.OrdinalIgnoreCase);
		foreach (var (name, processes) in processGroups)
		{
			if (IsSnapshotCandidate(processes))
				candidates.Add(name);
			foreach (Process process in processes)
				process.Dispose();
		}
		lock (_sync)
		{
			if (!_started)
				return;
			_snapshotCandidates.Clear();
			_snapshotCandidates.UnionWith(candidates);
		}
	}

	private static bool IsSnapshotCandidate(List<Process> processes)
	{
		return processes.Any(HasVisibleMainWindow);
	}

	private static bool TryGetProcessName(Process process, out string name)
	{
		name = string.Empty;

		try
		{
			string processName = process.ProcessName;
			name = processName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ? processName : $"{processName}.exe";
			return true;
		}
		catch (InvalidOperationException)
		{
			return false;
		}
		catch (Win32Exception)
		{
			return false;
		}
		catch (ArgumentException)
		{
			return false;
		}
	}

	private static bool HasVisibleMainWindow(Process process)
	{
		try
		{
			return process.MainWindowHandle != IntPtr.Zero;
		}
		catch (InvalidOperationException)
		{
			return false;
		}
		catch (ArgumentException)
		{
			return false;
		}
	}

	private void RememberRunningProcess(int processId)
	{
		if (processId <= 4 || processId == Environment.ProcessId || !TryGetProcessIdentity(processId, out ProcessIdentity identity) || ExcludedProcessNames.Contains(identity.Name))
		{
			return;
		}

		lock (_sync)
		{
			_runningProcesses[processId] = identity;
		}
	}

	private void RememberRunningProcess(int processId, string processName)
	{
		string name = processName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ? processName : $"{processName}.exe";
		if (processId <= 4 || processId == Environment.ProcessId || ExcludedProcessNames.Contains(name))
		{
			return;
		}

		lock (_sync)
		{
			_runningProcesses[processId] = new ProcessIdentity(name);
		}
	}

	private void RememberPresentingProcess(int processId)
	{
		if (processId <= 4 || processId == Environment.ProcessId || !TryGetProcessIdentity(processId, out ProcessIdentity identity) || ExcludedProcessNames.Contains(identity.Name))
		{
			return;
		}

		bool isNewProcessName;
		lock (_sync)
		{
			isNewProcessName = !_presentingProcesses.Values.Any(process => string.Equals(process.Name, identity.Name, StringComparison.OrdinalIgnoreCase));
			_runningProcesses[processId] = identity;
			_presentingProcesses[processId] = identity;
		}
		if (isNewProcessName)
			ProcessesChanged?.Invoke(this, EventArgs.Empty);
	}

	private bool TryGetProcessIdentity(int processId, out ProcessIdentity identity)
	{
		lock (_sync)
		{
			if (_runningProcesses.TryGetValue(processId, out identity))
				return true;
			if (!_started)
				return false;
		}

		identity = default;

		try
		{
			using Process process = Process.GetProcessById(processId);
			if (process.HasExited)
				return false;

			string processName = process.ProcessName;
			string name = processName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ? processName : $"{processName}.exe";
			identity = new ProcessIdentity(name);
			return true;
		}
		catch (ArgumentException)
		{
			return false;
		}
		catch (InvalidOperationException)
		{
			return false;
		}
		catch (Win32Exception)
		{
			return false;
		}
	}

	private static bool TryReadUInt32(TraceEvent traceEvent, string payloadName, out uint value)
	{
		value = 0;
		int payloadIndex = Array.IndexOf(traceEvent.PayloadNames, payloadName);
		if (payloadIndex < 0)
			return false;

		object payload = traceEvent.PayloadValue(payloadIndex);
		switch (payload)
		{
			case uint unsignedValue:
				value = unsignedValue;
				return true;
			case int signedValue when signedValue >= 0:
				value = (uint)signedValue;
				return true;
			case ulong longValue when longValue <= uint.MaxValue:
				value = (uint)longValue;
				return true;
			case long signedLongValue when signedLongValue is >= 0 and <= uint.MaxValue:
				value = (uint)signedLongValue;
				return true;
			default:
				return false;
		}
	}

	public void Dispose()
	{
		lock (_sync)
		{
			if (!_started)
				return;

			_started = false;
			_runningProcesses.Clear();
			_presentingProcesses.Clear();
			_runtimePresents.Clear();
			_snapshotCandidates.Clear();
			_redirectedCompositionProcesses.Clear();
			_confirmedPresentingProcesses.Clear();
		}

		_session?.Dispose();
		_session = null;
	}

	private readonly record struct ProcessIdentity(string Name);
	private readonly record struct RuntimePresent(Guid Provider, int ProcessId, int ThreadId);
}
