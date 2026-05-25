using AutoOS.Core.Helpers.Database;
using System.Diagnostics;

namespace AutoOS.Views.Updater.Stages;

public static class UpdateStage
{
	public static List<(string Title, Func<Task> Action, Func<bool> Condition)> UpdateActions(UpdateDialog dialog)
	{
		bool Discord = Directory.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "discord"));

		var actions = new List<(string Title, Func<Task> Action, Func<bool> Condition)>
		{
            // close discord
            ("Closing Discord", async () => { foreach (Process process in Process.GetProcessesByName("Discord")) { process.Kill(); process.WaitForExit(); }}, () => Discord == true),

			// set appearance to system
            ("Setting appearance to system", async () => DiscordHelper.SetSystemAppearance(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "discord", "Local Storage", "leveldb")), () => Discord == true),

            // disable game overlay
            ("Disabling game overlay", async () => DiscordHelper.DisableGameOverlay(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "discord", "Local Storage", "leveldb")), () => Discord == true),

			// disable clips
			("Disabling clips", async () => DiscordHelper.DisableClips(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "discord", "Local Storage", "leveldb")), () => Discord == true),
		};

		return actions;
	}
}