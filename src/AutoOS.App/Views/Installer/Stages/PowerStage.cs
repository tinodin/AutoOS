using System.Diagnostics;
using AutoOS.Core.Helpers.Power;
using AutoOS.Core.Helpers.Registry;
using Microsoft.Win32;

namespace AutoOS.App.Views.Installer.Stages;

public static class PowerStage
{
	public static List<(string Title, Func<Task> Action, Func<bool> Condition)> GetActions()
	{
		Guid guid = Guid.Empty;

		return
		[
			// reset power plans
			("Resetting Power plans", async () => PowerHelper.RestoreDefaultPowerSchemes(), null),

			// create "autoos" power plan
			(@"Creating ""AutoOS"" Power plan", async () => guid = PowerHelper.DuplicateScheme(new Guid("8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c"), "AutoOS", "AutoOS Power Plan"), null),

			// hard disk
			(@"Setting ""Turn off hard disk after"" to 0", async () => PowerHelper.WriteACValueIndex(guid, new Guid("0012ee47-9041-4b5d-9b77-535fba8b1442"), new Guid("6738e2c4-e8a5-4a42-b16a-e040e769756e"), 0), null),
			(@"Setting ""Secondary NVMe Idle Timeout"" to 0", async () => PowerHelper.WriteACValueIndex(guid, new Guid("0012ee47-9041-4b5d-9b77-535fba8b1442"), new Guid("d3d55efd-c1ff-424e-9dc3-441be7833010"), 0), null),
			(@"Setting ""Primary NVMe Idle Timeout"" to 0", async () => PowerHelper.WriteACValueIndex(guid, new Guid("0012ee47-9041-4b5d-9b77-535fba8b1442"), new Guid("d639518a-e56d-4345-8af2-b9f32fb26109"), 0), null),
			(@"Disabling ""NVMe NOPPME""", async () => PowerHelper.WriteACValueIndex(guid, new Guid("0012ee47-9041-4b5d-9b77-535fba8b1442"), new Guid("fc7372b6-ab2d-43ee-8797-15e9841f2cca"), 0), null),

			// sleep
			(@"Disabling ""Allow Away Mode Policy""", async () => PowerHelper.WriteACValueIndex(guid, new Guid("238c9fa8-0aad-41ed-83f4-97be242c8f20"), new Guid("25dfa149-5dd1-4736-b5ab-e8a37b5b8187"), 0), null),
			(@"Disabling ""Allow hybrid sleep""", async () => PowerHelper.WriteACValueIndex(guid, new Guid("238c9fa8-0aad-41ed-83f4-97be242c8f20"), new Guid("94ac6d29-73ce-41a6-809f-6363ba21b47e"), 0), null),
			(@"Disabling ""Allow wake timers""", async () => PowerHelper.WriteACValueIndex(guid, new Guid("238c9fa8-0aad-41ed-83f4-97be242c8f20"), new Guid("bd3b718a-0680-4d9d-8ab2-e1d2b4ac806d"), 0), null),
			(@"Setting ""System unattended sleep timeout"" to 0", async () => PowerHelper.WriteACValueIndex(guid, new Guid("238c9fa8-0aad-41ed-83f4-97be242c8f20"), new Guid("7bc4a2f9-d8fc-4469-b07b-33eb785aaca0"), 0), null),

			// usb settings
			(@"Setting ""Hub Selective Suspend Timeout"" to 0", async () => PowerHelper.WriteACValueIndex(guid, new Guid("2a737441-1930-4402-8d77-b2bebba308a3"), new Guid("0853a681-27c8-4100-a2fd-82013e970683"), 0), null),
			(@"Disabling ""USB 3 Link Power Mangement""", async () => PowerHelper.WriteACValueIndex(guid, new Guid("2a737441-1930-4402-8d77-b2bebba308a3"), new Guid("d4e98f31-5ffe-4ce1-be31-1b38b384c009"), 0), null),
			(@"Disabling ""USB selective suspend setting""", async () => PowerHelper.WriteACValueIndex(guid, new Guid("2a737441-1930-4402-8d77-b2bebba308a3"), new Guid("48e6b7a6-50f5-4782-a5d4-53bb8f07e226"), 0), null),

			// idle resiliency
			(@"Setting ""Deep Sleep Enabled/Disabled"" to ""Deep Sleep Disabled""", async () => PowerHelper.WriteACValueIndex(guid, new Guid("2e601130-5351-4d9d-8e04-252966bad054"), new Guid("d502f7ee-1dc7-4efd-a55d-f04b6f5c0545"), 0), null),
			(@"Setting ""Execution Required power request timeout"" to 0", async () => PowerHelper.WriteACValueIndex(guid, new Guid("2e601130-5351-4d9d-8e04-252966bad054"), new Guid("3166bc41-7e98-4e03-b34e-ec0f5f2b218e"), 0), null),

			// interrupt steering settings
			(@"Setting ""Target Load"" to 0", async () => PowerHelper.WriteACValueIndex(guid, new Guid("48672f38-7a9a-4bb2-8bf8-3d85be19de4e"), new Guid("73cde64d-d720-4bb2-a860-c755afe77ef2"), 0), null),
			(@"Setting ""Interrupt Steering Mode"" to ""Lock Interrupt Routing""", async () => PowerHelper.WriteACValueIndex(guid, new Guid("48672f38-7a9a-4bb2-8bf8-3d85be19de4e"), new Guid("2bfc24f9-5ea2-4801-8213-3dbae01aa39d"), 4), null),
			(@"Setting ""Interrupt Steering Mode"" to ""Lock Interrupt Routing""", async () => PowerHelper.WriteDCValueIndex(guid, new Guid("48672f38-7a9a-4bb2-8bf8-3d85be19de4e"), new Guid("2bfc24f9-5ea2-4801-8213-3dbae01aa39d"), 4), null),
			(@"Setting ""Unparked time trigger"" to 0", async () => PowerHelper.WriteACValueIndex(guid, new Guid("48672f38-7a9a-4bb2-8bf8-3d85be19de4e"), new Guid("d6ba4903-386f-4c2c-8adb-5c21b3328d25"), 0), null),

			// power buttons and lid
			(@"Setting ""Start menu power button"" to ""Shut down""", async () => PowerHelper.WriteACValueIndex(guid, new Guid("4f971e89-eebd-4455-a8de-9e59040e7347"), new Guid("a7066653-8d6c-40a8-910e-a1f54b84c7e5"), 2), null),
			(@"Setting ""Start menu power button"" to ""Shut down""", async () => PowerHelper.WriteDCValueIndex(guid, new Guid("4f971e89-eebd-4455-a8de-9e59040e7347"), new Guid("a7066653-8d6c-40a8-910e-a1f54b84c7e5"), 2), null),

			// processor power management
			(@"Disabling ""Allow Throttle States""", async () => PowerHelper.WriteACValueIndex(guid, new Guid("54533251-82be-4824-96c1-47b60b740d00"), new Guid("3b04d4fd-1cc7-4f23-ab1c-d1337819c4bb"), 0), null),
			(@"Setting ""Complex unpark policy"" to ""Round robin""", async () => PowerHelper.WriteACValueIndex(guid, new Guid("54533251-82be-4824-96c1-47b60b740d00"), new Guid("b669a5e9-7b1d-4132-baaa-49190abcfeb6"), 1), null),
			(@"Setting ""Complex unpark policy"" to ""Round robin""", async () => PowerHelper.WriteDCValueIndex(guid, new Guid("54533251-82be-4824-96c1-47b60b740d00"), new Guid("b669a5e9-7b1d-4132-baaa-49190abcfeb6"), 1), null),
			(@"Disabling ""Hetero containment policy.""", async () => PowerHelper.WriteACValueIndex(guid, new Guid("54533251-82be-4824-96c1-47b60b740d00"), new Guid("60fbe21b-efd9-49f2-b066-8674d8e9f423"), 0), null),
			(@"Disabling ""Hetero containment policy.""", async () => PowerHelper.WriteDCValueIndex(guid, new Guid("54533251-82be-4824-96c1-47b60b740d00"), new Guid("60fbe21b-efd9-49f2-b066-8674d8e9f423"), 0), null),
			(@"Setting ""Heterogeneous policy in effect"" to ""Use heterogeneous policy 0""", async () => PowerHelper.WriteACValueIndex(guid, new Guid("54533251-82be-4824-96c1-47b60b740d00"), new Guid("7f2f5cfa-f10c-4823-b5e1-e93ae85f46b5"), 0), null),
			(@"Setting ""Heterogeneous policy in effect"" to ""Use heterogeneous policy 0""", async () => PowerHelper.WriteDCValueIndex(guid, new Guid("54533251-82be-4824-96c1-47b60b740d00"), new Guid("7f2f5cfa-f10c-4823-b5e1-e93ae85f46b5"), 0), null),
			(@"Setting ""Heterogeneous short running thread scheduling policy"" to ""All processors""", async () => PowerHelper.WriteACValueIndex(guid, new Guid("54533251-82be-4824-96c1-47b60b740d00"), new Guid("bae08b81-2d5e-4688-ad6a-13243356654b"), 0), null),
			(@"Setting ""Heterogeneous short running thread scheduling policy"" to ""All processors""", async () => PowerHelper.WriteDCValueIndex(guid, new Guid("54533251-82be-4824-96c1-47b60b740d00"), new Guid("bae08b81-2d5e-4688-ad6a-13243356654b"), 0), null),
			(@"Setting ""Heterogeneous thread scheduling policy"" to ""Automatic""", async () => PowerHelper.WriteACValueIndex(guid, new Guid("54533251-82be-4824-96c1-47b60b740d00"), new Guid("93b8b6dc-0698-4d1c-9ee4-0644e900c85d"), 5), null),
			(@"Setting ""Heterogeneous thread scheduling policy"" to ""Automatic""", async () => PowerHelper.WriteDCValueIndex(guid, new Guid("54533251-82be-4824-96c1-47b60b740d00"), new Guid("93b8b6dc-0698-4d1c-9ee4-0644e900c85d"), 5), null),
			(@"Setting ""Initial performance for Processor Power Efficiency Class 1 when unparked"" to 100%", async () => PowerHelper.WriteACValueIndex(guid, new Guid("54533251-82be-4824-96c1-47b60b740d00"), new Guid("1facfc65-a930-4bc5-9f38-504ec097bbc0"), 100), null),
			(@"Setting ""Latency sensitivity hint min unparked cores/packages"" to 100%", async () => PowerHelper.WriteACValueIndex(guid, new Guid("54533251-82be-4824-96c1-47b60b740d00"), new Guid("616cdaa5-695e-4545-97ad-97dc2d1bdd88"), 100), null),
			(@"Setting ""Latency sensitivity hint min unparked cores/packages for Processor Power Efficiency Class 1"" to 100%", async () => PowerHelper.WriteACValueIndex(guid, new Guid("54533251-82be-4824-96c1-47b60b740d00"), new Guid("616cdaa5-695e-4545-97ad-97dc2d1bdd89"), 100), null),
			(@"Setting ""Latency sensitivity hint processor performance"" to 100%", async () => PowerHelper.WriteACValueIndex(guid, new Guid("54533251-82be-4824-96c1-47b60b740d00"), new Guid("619b7505-003b-4e82-b7a6-4dd29c300971"), 100), null),
			(@"Setting ""Latency sensitivity hint processor performance for Processor Power Efficiency Class 1"" to 100%", async () => PowerHelper.WriteACValueIndex(guid, new Guid("54533251-82be-4824-96c1-47b60b740d00"), new Guid("619b7505-003b-4e82-b7a6-4dd29c300972"), 100), null),
			(@"Setting ""Long running threads' processor architecture upper limit"" to 0", async () => PowerHelper.WriteACValueIndex(guid, new Guid("54533251-82be-4824-96c1-47b60b740d00"), new Guid("bf903d33-9d24-49d3-a468-e65e0325046a"), 0), null),
			(@"Setting ""Processor autonomous activity window"" to 0", async () => PowerHelper.WriteACValueIndex(guid, new Guid("54533251-82be-4824-96c1-47b60b740d00"), new Guid("cfeda3d0-7697-4566-a922-a9086cd49dfa"), 0), null),
			(@"Setting ""Processor efficiency containment concurrency threshold"" to 100", async () => PowerHelper.WriteACValueIndex(guid, new Guid("54533251-82be-4824-96c1-47b60b740d00"), new Guid("69439b22-221b-4830-bd34-f7bcece24583"), 100), null),
			(@"Setting ""Processor hybrid containment concurrency threshold"" to 100", async () => PowerHelper.WriteACValueIndex(guid, new Guid("54533251-82be-4824-96c1-47b60b740d00"), new Guid("6788488b-1b90-4d11-8fa7-973e470dff47"), 100), null),
			(@"Setting ""Processor idle demote threshold"" to 1", async () => PowerHelper.WriteACValueIndex(guid, new Guid("54533251-82be-4824-96c1-47b60b740d00"), new Guid("4b92d758-5a24-4851-a470-815d78aee119"), 1), null),
			(@"Setting ""Processor idle promote threshold"" to 0", async () => PowerHelper.WriteACValueIndex(guid, new Guid("54533251-82be-4824-96c1-47b60b740d00"), new Guid("7b224883-b3cc-4d79-819f-8374152cbe7c"), 0), null),
			(@"Setting ""Processor idle time check"" to 200000", async () => PowerHelper.WriteACValueIndex(guid, new Guid("54533251-82be-4824-96c1-47b60b740d00"), new Guid("c4581c31-89ab-4597-8e2b-9c9cab440e6b"), 200000), null),
			(@"Disabling ""Processor performance autonomous mode""", async () => PowerHelper.WriteACValueIndex(guid, new Guid("54533251-82be-4824-96c1-47b60b740d00"), new Guid("8baa4a8a-14c6-4451-8e8b-14bdbd197537"), 0), null),
			(@"Setting ""Processor performance core parking concurrency headroom threshold"" to 100", async () => PowerHelper.WriteACValueIndex(guid, new Guid("54533251-82be-4824-96c1-47b60b740d00"), new Guid("f735a673-2066-4f80-a0c5-ddee0cf1bf5d"), 100), null),
			(@"Setting ""Processor performance core parking concurrency threshold"" to 100", async () => PowerHelper.WriteACValueIndex(guid, new Guid("54533251-82be-4824-96c1-47b60b740d00"), new Guid("2430ab6f-a520-44a2-9601-f7f23b5134b1"), 100), null),
			(@"Setting ""Processor performance core parking decrease policy"" to ""Single Core""", async () => PowerHelper.WriteACValueIndex(guid, new Guid("54533251-82be-4824-96c1-47b60b740d00"), new Guid("71021b41-c749-4d21-be74-a00f335d582b"), 1), null),
			(@"Setting ""Processor performance core parking decrease time"" to 1", async () => PowerHelper.WriteACValueIndex(guid, new Guid("54533251-82be-4824-96c1-47b60b740d00"), new Guid("dfd10d17-d5eb-45dd-877a-9a34ddd15c82"), 1), null),
			(@"Setting ""Processor performance core parking distribution threshold"" to 0", async () => PowerHelper.WriteACValueIndex(guid, new Guid("54533251-82be-4824-96c1-47b60b740d00"), new Guid("4bdaf4e9-d103-46d7-a5f0-6280121616ef"), 0), null),
			(@"Setting ""Processor performance core parking increase policy"" to ""All possible cores""", async () => PowerHelper.WriteACValueIndex(guid, new Guid("54533251-82be-4824-96c1-47b60b740d00"), new Guid("c7be0679-2817-4d69-9d02-519a537ed0c6"), 2), null),
			(@"Setting ""Processor performance core parking increase time"" to 1", async () => PowerHelper.WriteACValueIndex(guid, new Guid("54533251-82be-4824-96c1-47b60b740d00"), new Guid("2ddd5a84-5a71-437e-912a-db0b8c788732"), 1), null),
			(@"Setting ""Processor performance core parking min cores for Processor Power Efficiency Class 1"" to 100%", async () => PowerHelper.WriteACValueIndex(guid, new Guid("54533251-82be-4824-96c1-47b60b740d00"), new Guid("0cc5b647-c1df-4637-891a-dec35c318584"), 100), null),
			(@"Setting ""Processor performance core parking overutilization threshold"" to 5", async () => PowerHelper.WriteACValueIndex(guid, new Guid("54533251-82be-4824-96c1-47b60b740d00"), new Guid("943c8cb6-6f93-4227-ad87-e9a3feec08d1"), 5), null),
			(@"Setting ""Processor performance core parking parked performance state"" to ""Lightest Performance State""", async () => PowerHelper.WriteACValueIndex(guid, new Guid("54533251-82be-4824-96c1-47b60b740d00"), new Guid("447235c7-6a8d-4cc0-8e24-9eaf70b96e2b"), 2), null),
			(@"Setting ""Processor performance core parking parked performance state for Processor Power Efficiency Class 1"" to ""Lightest Performance State""", async () => PowerHelper.WriteACValueIndex(guid, new Guid("54533251-82be-4824-96c1-47b60b740d00"), new Guid("447235c7-6a8d-4cc0-8e24-9eaf70b96e2c"), 2), null),
			(@"Setting ""Processor performance decrease policy"" to ""Rocket""", async () => PowerHelper.WriteACValueIndex(guid, new Guid("58533251-82be-4824-96c1-47b60b740d00"), new Guid("40fbefc7-2e9d-4d25-a185-0cfd8574bac6"), 2), null),
			(@"Setting ""Processor performance decrease policy for Processor Power Efficiency Class 1"" to ""Rocket""", async () => PowerHelper.WriteACValueIndex(guid, new Guid("54533251-82be-4824-96c1-47b60b740d00"), new Guid("40fbefc7-2e9d-4d25-a185-0cfd8574bac7"), 2), null),
			(@"Setting ""Processor performance decrease threshold"" to 0", async () => PowerHelper.WriteACValueIndex(guid, new Guid("54533251-82be-4824-96c1-47b60b740d00"), new Guid("12a0ab44-fe28-4fa9-b3bd-4b64f44960a6"), 0), null),
			(@"Setting ""Processor performance decrease threshold for Processor Power Efficiency Class 1"" to 0", async () => PowerHelper.WriteACValueIndex(guid, new Guid("54533251-82be-4824-96c1-47b60b740d00"), new Guid("12a0ab44-fe28-4fa9-b3bd-4b64f44960a7"), 0), null),
			(@"Setting ""Processor performance decrease time for Processor Power Efficiency Class 1"" to 1", async () => PowerHelper.WriteACValueIndex(guid, new Guid("54533251-82be-4824-96c1-47b60b740d00"), new Guid("7f2492b6-60b1-45e5-ae55-773f8cd5caec"), 1), null),
			(@"Setting ""Processor performance decrease time for Processor Power Efficiency Class 1"" to 1", async () => PowerHelper.WriteACValueIndex(guid, new Guid("54533251-82be-4824-96c1-47b60b740d00"), new Guid("d8edeb9b-95cf-4f95-a73c-b061973693c9"), 1), null),
			(@"Setting ""Processor performance increase policy for Processor Power Efficiency Class 1"" to ""Rocket""", async () => PowerHelper.WriteACValueIndex(guid, new Guid("54533251-82be-4824-96c1-47b60b740d00"), new Guid("465e1f50-b610-473a-ab58-00d1077dc419"), 2), null),
			(@"Setting ""Processor performance increase threshold"" to 1", async () => PowerHelper.WriteACValueIndex(guid, new Guid("54533251-82be-4824-96c1-47b60b740d00"), new Guid("06cadf0e-64ed-448a-8927-ce7bf90eb35d"), 1), null),
			(@"Setting ""Processor performance increase threshold for Processor Power Efficiency Class 1"" to 1", async () => PowerHelper.WriteACValueIndex(guid, new Guid("54533251-82be-4824-96c1-47b60b740d00"), new Guid("06cadf0e-64ed-448a-8927-ce7bf90eb35e"), 1), null),
			(@"Setting ""Processor performance time check interval"" to 5000ms", async () => PowerHelper.WriteACValueIndex(guid, new Guid("54533251-82be-4824-96c1-47b60b740d00"), new Guid("4d2b0152-7d5c-498b-88e2-34345392a2c5"), 5000), null),
			(@"Setting ""Processor performance time check interval"" to 5000ms", async () => PowerHelper.WriteDCValueIndex(guid, new Guid("54533251-82be-4824-96c1-47b60b740d00"), new Guid("4d2b0152-7d5c-498b-88e2-34345392a2c5"), 5000), null),
			(@"Setting ""Short running threads' processor architecture upper limit"" to 0", async () => PowerHelper.WriteACValueIndex(guid, new Guid("54533251-82be-4824-96c1-47b60b740d00"), new Guid("828423eb-8662-4344-90f7-52bf15870f5a"), 0), null),

			// display
			(@"Setting ""Console lock display off timeout"" to 0", async () => PowerHelper.WriteACValueIndex(guid, new Guid("7516b95f-f776-4464-8c53-06167f40cc99"), new Guid("8ec4b3a5-6868-48c2-be75-4f3044be88a7"), 0), null),
			(@"Setting ""Dim display after"" to 0", async () => PowerHelper.WriteACValueIndex(guid, new Guid("7516b95f-f776-4464-8c53-06167f40cc99"), new Guid("17aaa29b-8b43-4b94-aafe-35f64daaf1ee"), 0), null),
			(@"Setting ""Turn off display after"" to 0", async () => PowerHelper.WriteACValueIndex(guid, new Guid("7516b95f-f776-4464-8c53-06167f40cc99"), new Guid("3c0bc021-c8a8-4e07-a973-6b14cbcb2b7e"), 0), null),

			// presence aware power behavior
			(@"Setting ""Human Presence Sensor Adaptive Away Display Timeout"" to 0", async () => PowerHelper.WriteACValueIndex(guid, new Guid("8619b916-e004-4dd8-9b66-dae86f806698"), new Guid("0a7d6ab6-ac83-4ad1-8282-eca5b58308f3"), 0), null),
			(@"Setting ""Human Presence Sensor Adaptive Inattentive Dim Timeout"" to 0", async () => PowerHelper.WriteACValueIndex(guid, new Guid("8619b916-e004-4dd8-9b66-dae86f806698"), new Guid("cf8c6097-12b8-4279-bbdd-44601ee5209d"), 0), null),
			(@"Setting ""Non-sensor Input Presence Timeout"" to 0", async () => PowerHelper.WriteACValueIndex(guid, new Guid("8619b916-e004-4dd8-9b66-dae86f806698"), new Guid("5adbbfbc-074e-4da1-ba38-db8b36b2c8f3"), 0), null),

			// battery
			(@"Disabling ""Critical battery notification""", async () => PowerHelper.WriteACValueIndex(guid, new Guid("e73a048d-bf27-4f12-9731-8b2076e8891f"), new Guid("5dbb7c9f-38e9-40d2-9749-4f8a0e9f640f"), 0), null),
			(@"Disabling ""Low battery notification""", async () => PowerHelper.WriteACValueIndex(guid, new Guid("e73a048d-bf27-4f12-9731-8b2076e8891f"), new Guid("bcded951-187b-4d05-bccc-f7e51960c258"), 0), null),
			
			// apply changes
			("Applying Changes", async () => PowerHelper.PowerSetActiveScheme(guid), null),

			// disable hibernation
			("Disabling hibernation", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Power", "HibernateEnabled", 0, RegistryValueKind.DWord), null),
			("Disabling hibernation", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Power", "HiberbootEnabled", 0, RegistryValueKind.DWord), null),
			("Disabling hibernation", async () => await Process.Start(new ProcessStartInfo { FileName = "powercfg", Arguments = "/h off", UseShellExecute = false, CreateNoWindow = true })!.WaitForExitAsync(), null),
		];
	}
}

