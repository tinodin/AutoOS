using System.Diagnostics;
using System.Text.Json.Nodes;
using AutoOS.App.Views.Installer.Actions;
using AutoOS.Core.Helpers.Device;
using AutoOS.Core.Data.Models.Device;
using AutoOS.Core.Helpers.Logging;
using AutoOS.Core.Helpers.Registry;
using AutoOS.Core.Helpers.Services;
using AutoOS.Core.Helpers.Sound;
using Microsoft.UI.Xaml.Media;
using Windows.Storage;
using Windows.Win32.System.Services;

namespace AutoOS.App.Views.Startup.Stages;

public static class StartupStage
{
	private static readonly ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
	public static async Task Run()
	{
		if (localSettings.Values["XHCIs"] == null)
		{
			var json = new JsonArray();
			foreach (DeviceInfo device in DeviceHelper.GetDevices(DeviceType.XHCI))
				json.Add((JsonNode)new JsonObject { ["PnpDeviceId"] = JsonValue.Create(device.PnpDeviceId), ["IsActive"] = JsonValue.Create(false) });
			localSettings.Values["XHCIs"] = json.ToJsonString();
		}

		bool MSI = File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "MSI Afterburner", "MSIAfterburner.exe")) && Directory.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "MSI Afterburner", "Profiles")) && Directory.GetFiles(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "MSI Afterburner", "Profiles")).Any(f => !f.EndsWith("MSIAfterburner.cfg", StringComparison.OrdinalIgnoreCase));
		bool SOUND = JsonNode.Parse(localSettings.Values["Sound"]?.ToString() ?? "[]")?.AsArray()?.Any(x => (x?["BufferSize"]?.GetValue<float>() ?? 0f) < 10f) == true;
		bool IMOD = JsonNode.Parse(localSettings.Values["XHCIs"]?.ToString() ?? "[]")?.AsArray()?.Any(x => x?["IsActive"]?.GetValue<bool>() == false) == true;
		bool OBS = localSettings.Values["OBS"]?.ToString() == "1" && File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "obs-studio", "bin", "64bit", "obs64.exe"));

		string previousTitle = string.Empty;

		var actions = new List<(string Title, Func<Task> Action, Func<bool>? Condition)>
		{
			// sync time
			("Syncing time", async () => ServicesHelper.SetStartupType("W32Time", SERVICE_START_TYPE.SERVICE_DEMAND_START), null),
			("Syncing time", async () => ServicesHelper.StartService("W32Time"), null),
			("Syncing time", async () => await Process.Start(new ProcessStartInfo("w32tm", "/resync") { CreateNoWindow = true })!.WaitForExitAsync(), null),
			("Syncing time", async () => ServicesHelper.StopService("W32Time"), null),

			// apply msi afterburner profile
			("Applying MSI Afterburner profile", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "MSI Afterburner", "MSIAfterburner.exe"), Arguments = "/Profile1 /q" })!.WaitForExitAsync(), () => MSI == true),

			// apply sound buffer sizes
			("Applying sound buffer sizes", async () => SoundHelper.SetBufferSizes(), () => SOUND == true),

			// disable xhci interrupt moderation (imod)
			("Disabling XHCI Interrupt Moderation (IMOD)", async () => { foreach (DeviceInfo device in DeviceHelper.GetDevices(DeviceType.XHCI)) if (JsonNode.Parse(localSettings.Values["XHCIs"]?.ToString() ?? "[]")?.AsArray()?.FirstOrDefault(x => x?["PnpDeviceId"]?.ToString() == device.PnpDeviceId)?["IsActive"]?.GetValue<bool>() == false) DeviceHelper.ToggleImod(device, false); }, () => IMOD),

			// launch obs studio
			("Launching OBS Studio", async () => ProcessActions.CleanDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "obs-studio", ".sentinel")), () => OBS == true),
			("Launching OBS Studio", async () => Process.Start(new ProcessStartInfo { FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "obs-studio", "bin", "64bit", "obs64.exe"), Arguments = "--disable-updater --startreplaybuffer --minimize-to-tray", WorkingDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "obs-studio", "bin", "64bit") }), () => OBS == true),

			// clean temp directories
			("Cleaning temp directories", async () => await RegistryHelper.RunAs(RegistryHelper.Identity.TrustedInstaller, async () => { ProcessActions.CleanDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Logs")); }), null),
			("Cleaning temp directories", async () => await RegistryHelper.RunAs(RegistryHelper.Identity.TrustedInstaller, async () => { ProcessActions.CleanDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Panther")); }), null),
			("Cleaning temp directories", async () => await RegistryHelper.RunAs(RegistryHelper.Identity.TrustedInstaller, async () => { ProcessActions.CleanDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "LogFiles")); }), null),
			("Cleaning temp directories", async () => await RegistryHelper.RunAs(RegistryHelper.Identity.TrustedInstaller, async () => { ProcessActions.CleanDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "SleepStudy")); }), null),
			("Cleaning temp directories", async () => await RegistryHelper.RunAs(RegistryHelper.Identity.TrustedInstaller, async () => { ProcessActions.CleanDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "sru")); }), null),
			("Cleaning temp directories", async () => await RegistryHelper.RunAs(RegistryHelper.Identity.TrustedInstaller, async () => { ProcessActions.CleanDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "WDI")); }), null),
			("Cleaning temp directories", async () => await RegistryHelper.RunAs(RegistryHelper.Identity.TrustedInstaller, async () => { ProcessActions.CleanDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "winevt", "Logs")); }), null),
			("Cleaning temp directories", async () => await RegistryHelper.RunAs(RegistryHelper.Identity.TrustedInstaller, async () => { ProcessActions.CleanDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "SystemTemp")); }), null),
			("Cleaning temp directories", async () => await RegistryHelper.RunAs(RegistryHelper.Identity.TrustedInstaller, async () => { ProcessActions.CleanDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Temp")); }), null),
			("Cleaning temp directories", async () => ProcessActions.CleanDirectory(Path.GetTempPath()), null)
		};

		var filteredActions = actions.Where(a => a.Condition == null || a.Condition.Invoke()).ToList();
		int groupedTitleCount = 0;

		List<Func<Task>> currentGroup = [];

		for (int i = 0; i < filteredActions.Count; i++)
		{
			if (i == 0 || filteredActions[i].Title != filteredActions[i - 1].Title)
			{
				groupedTitleCount++;
			}
		}

		int executedGroupsCount = 0;

		foreach ((string? title, Func<Task> action, Func<bool>? condition) in filteredActions)
		{
			if (previousTitle != string.Empty && previousTitle != title && currentGroup.Count > 0)
			{
				foreach (Func<Task> groupedAction in currentGroup)
				{
					try
					{
						StartupWindow.Status.Text = previousTitle + "...";
						await groupedAction();
					}
					catch (Exception ex)
					{
						try
						{
							LogHelper.LogError(ex, null, previousTitle);
						}
						catch { }
						StartupWindow.Status.Text = ex.Message;
						StartupWindow.Progress.Foreground = (Brush)Application.Current.Resources["SystemFillColorCriticalBrush"];
					}
				}

				executedGroupsCount++;
				StartupWindow.Progress.Value = (executedGroupsCount * 100.0) / groupedTitleCount;
				await Task.Delay(250);
				currentGroup.Clear();
			}

			currentGroup.Add(action);
			previousTitle = title;
		}

		if (currentGroup.Count > 0)
		{
			foreach (Func<Task> groupedAction in currentGroup)
			{
				try
				{
					StartupWindow.Status.Text = previousTitle + "...";
					await groupedAction();
				}
				catch (Exception ex)
				{
					try
					{
						LogHelper.LogError(ex, null, previousTitle);
					}
					catch { }
					StartupWindow.Status.Text = ex.Message;
					StartupWindow.Progress.Foreground = (Brush)Application.Current.Resources["SystemFillColorCriticalBrush"];
				}
			}
			executedGroupsCount++;
			StartupWindow.Progress.Value = (executedGroupsCount * 100.0) / groupedTitleCount;
		}

		await Task.Delay(500);
		StartupWindow.Status.Text = "Done.";
		StartupWindow.Progress.Foreground = new SolidColorBrush((Windows.UI.Color)Application.Current.Resources["SystemFillColorSuccess"]);
		await Task.Delay(250);
		Application.Current.Exit();
	}
}
