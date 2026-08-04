using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Windows.Win32;
using Windows.Win32.System.Com;

namespace AutoOS.Core.Helpers.Shortcut;

[GeneratedComInterface(StringMarshalling = StringMarshalling.Utf16)]
[Guid("000214F9-0000-0000-C000-000000000046")]
internal partial interface IShellLinkW
{
	[PreserveSig] int GetPath(nint pszFile, int cchMaxPath, nint pfd, int fFlags);
	[PreserveSig] int GetIDList(out nint ppidl);
	[PreserveSig] int SetIDList(nint pidl);
	[PreserveSig] int GetDescription(nint pszName, int cchMaxName);
	[PreserveSig] int SetDescription(string pszName);
	[PreserveSig] int GetWorkingDirectory(nint pszDir, int cchMaxPath);
	[PreserveSig] int SetWorkingDirectory(string pszDir);
	[PreserveSig] int GetArguments(nint pszArgs, int cchMaxPath);
	[PreserveSig] int SetArguments(string pszArgs);
	[PreserveSig] int GetHotkey(out short pwHotkey);
	[PreserveSig] int SetHotkey(short wHotkey);
	[PreserveSig] int GetShowCmd(out int piShowCmd);
	[PreserveSig] int SetShowCmd(int iShowCmd);
	[PreserveSig] int GetIconLocation(nint pszIconPath, int cchIconPath, out int piIcon);
	[PreserveSig] int SetIconLocation(string pszIconPath, int iIcon);
	[PreserveSig] int SetRelativePath(string pszPathRel, int dwReserved);
	[PreserveSig] int Resolve(nint hwnd, int fFlags);
	void SetPath(string pszFile);
}

[GeneratedComInterface(StringMarshalling = StringMarshalling.Utf16)]
[Guid("0000010b-0000-0000-C000-000000000046")]
internal partial interface IPersistFile
{
	void GetClassID(out Guid pClassID);
	[PreserveSig] int IsDirty();
	void Load(string pszFileName, int dwMode);
	void Save(string pszFileName, [MarshalAs(UnmanagedType.Bool)] bool fRemember);
	void SaveCompleted(string pszFileName);
	void GetCurFile(out nint ppszFileName);
}

public static partial class ShortcutHelper
{
	private static readonly Guid CLSID_ShellLink = new("00021401-0000-0000-C000-000000000046");

	public static unsafe void Create(string shortcutPath, string targetPath, string startIn = null)
	{
		Guid iidLink = typeof(IShellLinkW).GUID;
		Marshal.ThrowExceptionForHR((int)PInvoke.CoCreateInstance(in CLSID_ShellLink, null, CLSCTX.CLSCTX_INPROC_SERVER, in iidLink, out void* ppv));

		var cw = new StrategyBasedComWrappers();
		var link = (IShellLinkW)cw.GetOrCreateObjectForComInstance((nint)ppv, CreateObjectFlags.UniqueInstance);
		link.SetPath(targetPath);
		if (startIn != null)
			link.SetWorkingDirectory(startIn);

		Guid iidFile = typeof(IPersistFile).GUID;
		Marshal.ThrowExceptionForHR(Marshal.QueryInterface((nint)ppv, in iidFile, out nint ppvFile));
		try
		{
			var file = (IPersistFile)cw.GetOrCreateObjectForComInstance(ppvFile, CreateObjectFlags.UniqueInstance);
			file.Save(shortcutPath, false);
		}
		finally
		{
			Marshal.Release(ppvFile);
		}
	}
}
