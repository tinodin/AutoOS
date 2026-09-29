using System.Diagnostics;
using AutoOS.Core.Services.DiskAnalyzer;
using Windows.ApplicationModel.DataTransfer;

namespace AutoOS.App.Helpers;

/// <summary>
/// Opens scanned disk entries in Explorer and copies their paths to the clipboard.
/// </summary>
public static class DiskLauncher
{
	public static void Open(DiskNode node)
	{
		OpenInExplorer(node.IsFolder ? node.FullPath : Path.GetDirectoryName(node.FullPath) ?? node.FullPath);
	}

	public static void OpenFile(string fullPath)
	{
		OpenInExplorer(Path.GetDirectoryName(fullPath) ?? fullPath);
	}

	public static void CopyPath(string path)
	{
		try
		{
			var dataPackage = new DataPackage();
			dataPackage.SetText(path);
			Clipboard.SetContent(dataPackage);
		}
		catch
		{
			// Another process can hold the clipboard; copying stays best effort.
		}
	}

	private static void OpenInExplorer(string path)
	{
		try
		{
			Process.Start(new ProcessStartInfo { FileName = "explorer.exe", Arguments = $"\"{path}\"", UseShellExecute = true });
		}
		catch
		{
			// Explorer ships with Windows, so a failure here must not surface as an app crash.
		}
	}
}
