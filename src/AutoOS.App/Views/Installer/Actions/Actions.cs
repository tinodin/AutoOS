using System.Diagnostics;
using System.Net.Http.Headers;
using System.Xml;
using AutoOS.Core.Helpers.Registry;
using Microsoft.UI.Xaml.Media;
using Microsoft.Win32;
using WinRT.Interop;

namespace AutoOS.App.Views.Installer.Actions;

public static class ProcessActions
{
	public static IntPtr WindowHandle { get; private set; }
	public static readonly HttpClient httpClient = new() { DefaultRequestHeaders = { UserAgent = { ProductInfoHeaderValue.Parse("AutoOS") } } };

	public static async Task RunPowerShell(string command)
	{
		await Process.Start(new ProcessStartInfo("powershell.exe", @$"-NoProfile -ExecutionPolicy Bypass -Command ""{command} """) { CreateNoWindow = true, UseShellExecute = false }).WaitForExitAsync();
	}

	public static async Task RunConnectionCheck()
	{
		WindowHandle = WindowNative.GetWindowHandle(App.MainWindow);
		InstallPage.Info.Severity = InfoBarSeverity.Warning;
		InstallPage.Progress.Foreground = (Brush)Application.Current.Resources["SystemFillColorCautionBrush"];
		TaskbarHelper.SetProgressState(WindowHandle, TaskbarStates.Paused);
		InstallPage.ProgressRingControl.Foreground = (Brush)Application.Current.Resources["SystemFillColorCautionBrush"];

		await Task.Delay(1000);

		while (true)
		{
			try
			{
				using var client = new HttpClient();
				client.DefaultRequestHeaders.UserAgent.Add(ProductInfoHeaderValue.Parse("AutoOS"));
				HttpResponseMessage response = await client.GetAsync("http://www.google.com");
				if (response.IsSuccessStatusCode)
				{
					InstallPage.Info.Severity = InfoBarSeverity.Informational;
					InstallPage.Progress.ClearValue(ProgressBar.ForegroundProperty);
					TaskbarHelper.SetProgressState(WindowHandle, TaskbarStates.Normal);
					InstallPage.ProgressRingControl.Foreground = null;
					InstallPage.Info.Title = "Internet connection successfully established...";
					await Task.Delay(500);
					break;
				}
			}
			catch
			{

			}
		}
	}

	public static async Task PinToTaskbar(string type, string path)
	{
		string xmlPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Setup", "Scripts", "TaskbarLayoutModification.xml");
		var xmlDoc = new XmlDocument();
		xmlDoc.Load(xmlPath);

		XmlNamespaceManager nsMgr = new(xmlDoc.NameTable);
		nsMgr.AddNamespace("taskbar", "http://schemas.microsoft.com/Start/2014/TaskbarLayout");

		XmlNode pinList = xmlDoc.SelectSingleNode("//taskbar:TaskbarPinList", nsMgr);
		string nsUri = nsMgr.LookupNamespace("taskbar");

		XmlNode newNode;
		if (type == "UWA")
		{
			newNode = xmlDoc.CreateElement("taskbar", "UWA", nsUri);
			((XmlElement)newNode).SetAttribute("AppUserModelID", path);
		}
		else
		{
			newNode = xmlDoc.CreateElement("taskbar", "DesktopApp", nsUri);
			((XmlElement)newNode).SetAttribute("DesktopApplicationLinkPath", path);
		}

		pinList.AppendChild(newNode);
		xmlDoc.Save(xmlPath);

		RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\Explorer", "StartLayoutFile", xmlPath, RegistryValueKind.ExpandString);
		RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\Explorer", "LockedStartLayout", 1, RegistryValueKind.DWord);

		foreach (Process process in Process.GetProcessesByName("explorer"))
		{
			process.Kill();
			process.WaitForExit();
		}
	}

	public static async Task PatchStartAllBack()
	{
		Registry.SetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon", "AutoRestartShell", 0, RegistryValueKind.DWord);

		foreach (string? name in new[] { "explorer", "StartAllBackCfg" })
		{
			foreach (Process process in Process.GetProcessesByName(name))
			{
				process.Kill();
				await process.WaitForExitAsync();
			}
		}

		Registry.SetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon", "AutoRestartShell", 1, RegistryValueKind.DWord);

		string dll = @"StartAllBack\StartAllBackX64.dll";
		IEnumerable<string> paths = new[] {
			Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), dll),
			Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), dll),
			Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), dll)
		}.Where(File.Exists);

		foreach (string path in paths)
		{
			string bak = path + ".bak";
			string old = path + ".old";

			if (File.Exists(bak))
			{
				try { if (File.Exists(old)) File.Delete(old); File.Move(path, old); } catch { }
				File.Copy(bak, path, true);
			}
			else
			{
				File.Copy(path, bak, true);
				byte[] b = File.ReadAllBytes(path);
				byte[] pt = [0x48, 0x89, 0x5C, 0x24, 0x08, 0x55, 0x56, 0x57, 0x48, 0x8D, 0xAC, 0x24, 0x70, 0xFF, 0xFF, 0xFF];
				byte[] ph = [0x67, 0xC7, 0x01, 0x01, 0x00, 0x00, 0x00, 0xB8, 0x01, 0x00, 0x00, 0x00, 0xC3, 0x90, 0x90, 0x90];

				for (int i = 0; i <= b.Length - pt.Length; i++)
				{
					if (b.Skip(i).Take(pt.Length).SequenceEqual(pt))
					{
						Buffer.BlockCopy(ph, 0, b, i, ph.Length);

						try
						{
							File.WriteAllBytes(path, b);
						}
						catch (IOException)
						{
							if (File.Exists(old)) File.Delete(old);
							File.Move(path, old);
							File.WriteAllBytes(path, b);
						}
						break;
					}
				}
			}
			try
			{
				if (File.Exists(old))
					File.Delete(old);
			}
			catch { }
		}
	}

	public static void CleanDirectory(string path)
	{
		try
		{
			if (Directory.Exists(path))
			{
				foreach (string file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
				{
					try
					{
						File.Delete(file);
					}
					catch { }
				}

				foreach (string dir in Directory.EnumerateDirectories(path, "*", SearchOption.TopDirectoryOnly))
				{
					try
					{
						Directory.Delete(dir, true);
					}
					catch { }
				}
			}
		}
		catch { }
	}
}
