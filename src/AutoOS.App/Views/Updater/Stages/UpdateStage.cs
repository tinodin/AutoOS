using AutoOS.Core.Data.Models.GPU;
using AutoOS.Core.Helpers.GPU;
using AutoOS.Core.Helpers.Registry;

namespace AutoOS.App.Views.Updater.Stages;

public static class UpdateStage
{
	public static List<(string Title, Func<Task> Action, Func<bool>? Condition)> UpdateActions(UpdateDialog dialog)
	{
		IEnumerable<GpuInfo> gpus = GpuHelper.GetGPUs().Where(gpu => gpu.NVIDIA);

		var actions = new List<(string Title, Func<Task> Action, Func<bool>? Condition)>
		{

		};

		foreach (GpuInfo gpu in gpus)
		{
			actions.Add(("Enabling Dynamic Performance States (P-States)", async () => RegistryHelper.DeleteValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "DisableDynamicPstate"), null));
			actions.Add(("Enabling Dynamic Performance States (P-States)", async () => RegistryHelper.DeleteValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "DisableAsyncPstates"), null));
		}

		return actions;
	}
}
