namespace AutoOS.App.Views.Installer.Stages;

public static class ActivationStage
{
	public static List<(string Title, Func<Task> Action, Func<bool>? Condition)> GetActions()
	{
		return
		[
			// input the activation key
			("Inputting the activation key", async () => await Process.Start(new ProcessStartInfo { FileName = "cmd.exe", Arguments = @"/c slmgr //B /ipk W269N-WFGWX-YVC9B-4J6C9-T83GX", CreateNoWindow = true })!.WaitForExitAsync(), null),

			// input the kms server
			("Inputting the KMS server", async () => await Process.Start(new ProcessStartInfo { FileName = "cmd.exe", Arguments = @"/c slmgr //B /skms kms8.msguides.com", CreateNoWindow = true })!.WaitForExitAsync(), null),

			// activate windows
			("Activating Windows", async () => await Process.Start(new ProcessStartInfo { FileName = "cmd.exe", Arguments = @"/c slmgr //B /ato", CreateNoWindow = true })!.WaitForExitAsync(), null)
		];
	}
}

