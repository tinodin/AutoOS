using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Windows.Win32;

namespace AutoOS.Core.Helpers.TaskScheduler;

[StructLayout(LayoutKind.Sequential)]
internal struct VARIANT
{
	public ushort vt;
	public ushort wReserved1;
	public ushort wReserved2;
	public ushort wReserved3;
	public long data;

	public static VARIANT Empty => default;
	public static VARIANT FromInt(int value) => new() { vt = 3, data = value };
}

[GeneratedComInterface(StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(BStrStringMarshaller))]
[Guid("2FABA4C7-4DA9-4013-9697-20CC3FD40F85")]
internal partial interface ITaskService
{
	[PreserveSig] int DispSlot3();
	[PreserveSig] int DispSlot4();
	[PreserveSig] int DispSlot5();
	[PreserveSig] int DispSlot6();

	ITaskFolder GetFolder(string path);
	[PreserveSig] int Slot8();
	[PreserveSig] int Slot9();
	void Connect(VARIANT serverName, VARIANT user, VARIANT domain, VARIANT password);
}

[GeneratedComInterface(StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(BStrStringMarshaller))]
[Guid("8CFAC062-A080-4C15-9A88-AA7C2AF80DFC")]
internal partial interface ITaskFolder
{
	[PreserveSig] int DispSlot3();
	[PreserveSig] int DispSlot4();
	[PreserveSig] int DispSlot5();
	[PreserveSig] int DispSlot6();

	string Get_Name();
	string Get_Path();
	ITaskFolder GetFolder(string path);
	ITaskFolderCollection GetFolders(int flags);
	[PreserveSig] int Slot11();
	[PreserveSig] int Slot12();
	[PreserveSig] int Slot13();
	IRegisteredTaskCollection GetTasks(int flags);
	void DeleteTask(string name, int flags);
}

[GeneratedComInterface]
[Guid("79184A66-8664-423F-97F1-637356A5D812")]
internal partial interface ITaskFolderCollection
{
	[PreserveSig] int DispSlot3();
	[PreserveSig] int DispSlot4();
	[PreserveSig] int DispSlot5();
	[PreserveSig] int DispSlot6();

	int Get_Count();
	ITaskFolder Get_Item(VARIANT index);
}

[GeneratedComInterface]
[Guid("86627EB4-42A7-41E4-A4D9-AC33A72F2D52")]
internal partial interface IRegisteredTaskCollection
{
	[PreserveSig] int DispSlot3();
	[PreserveSig] int DispSlot4();
	[PreserveSig] int DispSlot5();
	[PreserveSig] int DispSlot6();

	int Get_Count();
	IRegisteredTask Get_Item(VARIANT index);
}

[GeneratedComInterface(StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(BStrStringMarshaller))]
[Guid("9C86F320-DEE3-4DD1-B972-A303F26B061E")]
internal partial interface IRegisteredTask
{
	[PreserveSig] int DispSlot3();
	[PreserveSig] int DispSlot4();
	[PreserveSig] int DispSlot5();
	[PreserveSig] int DispSlot6();

	string Get_Name();
	string Get_Path();
	int Get_State();
	[return: MarshalAs(UnmanagedType.VariantBool)]
	bool Get_Enabled();
	void Put_Enabled([MarshalAs(UnmanagedType.VariantBool)] bool enabled);
}

public static partial class TaskSchedulerHelper
{
	private static readonly Guid CLSID_TaskScheduler = new("0F87369F-A4E5-4CFC-BD3E-73E6154572DD");

	private static bool SearchTasks(ITaskFolder folder, Func<ITaskFolder, IRegisteredTask, bool> onMatch)
	{
		try
		{
			IRegisteredTaskCollection tasks = folder.GetTasks(1);
			for (int i = 1; i <= tasks.Get_Count(); i++)
			{
				IRegisteredTask task = tasks.Get_Item(VARIANT.FromInt(i));
				if (onMatch(folder, task))
					return true;
			}
		}
		catch (COMException) { }

		try
		{
			ITaskFolderCollection subFolders = folder.GetFolders(0);
			for (int i = 1; i <= subFolders.Get_Count(); i++)
			{
				if (SearchTasks(subFolders.Get_Item(VARIANT.FromInt(i)), onMatch))
					return true;
			}
		}
		catch (COMException) { }

		return false;
	}

	private static unsafe ITaskService CreateTaskService()
	{
		Guid iid = typeof(ITaskService).GUID;
		Marshal.ThrowExceptionForHR((int)PInvoke.CoCreateInstance(in CLSID_TaskScheduler, null, Windows.Win32.System.Com.CLSCTX.CLSCTX_INPROC_SERVER, in iid, out void* ppv));
		var cw = new StrategyBasedComWrappers();
		return (ITaskService)cw.GetOrCreateObjectForComInstance((nint)ppv, CreateObjectFlags.UniqueInstance);
	}

	public static void Toggle(string wildcard, bool enable)
	{
		ITaskService ts = CreateTaskService();
		ts.Connect(VARIANT.Empty, VARIANT.Empty, VARIANT.Empty, VARIANT.Empty);

		SearchTasks(ts.GetFolder("\\"), (_, task) =>
		{
			if (!task.Get_Path().Contains(wildcard, StringComparison.OrdinalIgnoreCase))
				return false;
			if (task.Get_Enabled() != enable)
				task.Put_Enabled(enable);
			return true;
		});
	}

	public static void Unregister(string wildcard)
	{
		ITaskService ts = CreateTaskService();
		ts.Connect(VARIANT.Empty, VARIANT.Empty, VARIANT.Empty, VARIANT.Empty);

		SearchTasks(ts.GetFolder("\\"), (folder, task) =>
		{
			if (!task.Get_Path().Contains(wildcard, StringComparison.OrdinalIgnoreCase))
				return false;
			folder.DeleteTask(task.Get_Name(), 0);
			return true;
		});
	}
}
