using System.Diagnostics;
using AutoOS.App.Common;
using AutoOS.Core.Helpers.Download;
using AutoOS.Core.Helpers.Extract;
using AutoOS.Core.Helpers.Processes;
using AutoOS.Core.Helpers.Registry;
using Microsoft.Win32;

namespace AutoOS.App.Views.Installer.Stages;

public static class RuntimesStage
{
	public static List<(string Title, Func<Task> Action, Func<bool>? Condition)> GetActions()
	{
		return
		[
			// download the visual c++ redistributable
			("Downloading Visual C++ Redistributable", async () => await DownloadHelper.Download("https://github.com/abbodi1406/vcredist/releases/latest/download/VisualCppRedist_AIO_x86_x64.exe", Path.GetTempPath(), "VisualCppRedist_AIO_x86_x64.exe", new InstallPageReporter()), null),

			// install visual c++ redistributable
			("Installing Visual C++ Redistributable", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Path.GetTempPath(), "VisualCppRedist_AIO_x86_x64.exe"), Arguments = "/ai /gm2", WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), null),
			("Cleaning up Visual C++ Redistributable files", async () => { string path = Path.Combine(Path.GetTempPath(), "VisualCppRedist_AIO_x86_x64.exe"); foreach (Process process in ProcessesHelper.GetLockingProcesses(path)) { process.Kill(); process.WaitForExit(); } RegistryHelper.RunAs(RegistryHelper.Identity.TrustedInstaller, () => File.Delete(path)); }, null),

			// download the microsoft edge webview2 runtime
			("Downloading Microsoft Edge WebView2 Runtime", async () => await DownloadHelper.Download("https://msedge.sf.dl.delivery.mp.microsoft.com/filestreamingservice/files/7dedb563-79f6-48af-b588-dd8e97f4b73c/MicrosoftEdgeWebView2RuntimeInstallerX64.exe", Path.GetTempPath(), "MicrosoftEdgeWebView2RuntimeInstallerX64.exe", new InstallPageReporter()), null),

			// install microsoft edge webview2 runtime
			("Installing Microsoft Edge WebView2 Runtime", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Path.GetTempPath(), "MicrosoftEdgeWebView2RuntimeInstallerX64.exe"), Arguments = "/silent /install", WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), null),
			("Installing Microsoft Edge WebView2 Runtime", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\MicrosoftEdgeUpdate.exe", "Debugger", @"%windir%\System32\taskkill.exe", RegistryValueKind.String), null),
			("Cleaning up Microsoft Edge WebView2 Runtime files", async () => { string path = Path.Combine(Path.GetTempPath(), "MicrosoftEdgeWebView2RuntimeInstallerX64.exe"); foreach (Process process in ProcessesHelper.GetLockingProcesses(path)) { process.Kill(); process.WaitForExit(); } RegistryHelper.RunAs(RegistryHelper.Identity.TrustedInstaller, () => File.Delete(path)); }, null),

			// download .net 6 desktop runtime
			("Downloading .NET 6 Desktop Runtime", async () => await DownloadHelper.Download("https://aka.ms/dotnet/8.0/windowsdesktop-runtime-win-x64.exe", Path.GetTempPath(), "windowsdesktop-runtime-win-x64.exe", new InstallPageReporter()), null),

			// install .net 6 desktop runtime
			("Installing .NET 6 Desktop Runtime", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Path.GetTempPath(), "windowsdesktop-runtime-win-x64.exe"), Arguments = "/install /quiet /norestart" , WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), null),
			("Cleaning up .NET 6 Desktop Runtime files", async () => { string path = Path.Combine(Path.GetTempPath(), "windowsdesktop-runtime-win-x64.exe"); foreach (Process process in ProcessesHelper.GetLockingProcesses(path)) { process.Kill(); process.WaitForExit(); } RegistryHelper.RunAs(RegistryHelper.Identity.TrustedInstaller, () => File.Delete(path)); }, null),

			// download .net 8 desktop runtime
			("Downloading .NET 8 Desktop Runtime", async () => await DownloadHelper.Download("https://aka.ms/dotnet/8.0/windowsdesktop-runtime-win-x64.exe", Path.GetTempPath(), "windowsdesktop-runtime-win-x64.exe", new InstallPageReporter()), null),

			// install .net 8 desktop runtime
			("Installing .NET 8 Desktop Runtime", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Path.GetTempPath(), "windowsdesktop-runtime-win-x64.exe"), Arguments = "/install /quiet /norestart" , WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), null),
			("Cleaning up .NET 8 Desktop Runtime files", async () => { string path = Path.Combine(Path.GetTempPath(), "windowsdesktop-runtime-win-x64.exe"); foreach (Process process in ProcessesHelper.GetLockingProcesses(path)) { process.Kill(); process.WaitForExit(); } RegistryHelper.RunAs(RegistryHelper.Identity.TrustedInstaller, () => File.Delete(path)); }, null),

			// download .net 10 desktop runtime
			("Downloading .NET 10 Desktop Runtime", async () => await DownloadHelper.Download("https://aka.ms/dotnet/10.0/windowsdesktop-runtime-win-x64.exe", Path.GetTempPath(), "windowsdesktop-runtime-win-x64.exe", new InstallPageReporter()), null),

			// install .net 10 desktop runtime
			("Installing .NET 10 Desktop Runtime", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Path.GetTempPath(), "windowsdesktop-runtime-win-x64.exe"), Arguments = "/install /quiet /norestart" , WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), null),
			("Cleaning up .NET 10 Desktop Runtime files", async () => { string path = Path.Combine(Path.GetTempPath(), "windowsdesktop-runtime-win-x64.exe"); foreach (Process process in ProcessesHelper.GetLockingProcesses(path)) { process.Kill(); process.WaitForExit(); } RegistryHelper.RunAs(RegistryHelper.Identity.TrustedInstaller, () => File.Delete(path)); }, null),

			// download microsoft windows app runtime
			("Downloading Microsoft Windows App Runtime", async () => await DownloadHelper.Download("https://aka.ms/windowsappsdk/1.6/1.6.250602001/windowsappruntimeinstall-x64.exe", Path.GetTempPath(), "WindowsAppRuntimeInstall-x64.exe", new InstallPageReporter()), null),

			// install microsoft windows app runtime
			("Installing Microsoft Windows App Runtime", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Path.GetTempPath(), "WindowsAppRuntimeInstall-x64.exe"), Arguments = "--quiet --force --msix --license" , WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), null),
			("Cleaning up Microsoft Windows App Runtime files", async () => { string path = Path.Combine(Path.GetTempPath(), "WindowsAppRuntimeInstall-x64.exe"); foreach (Process process in ProcessesHelper.GetLockingProcesses(path)) { process.Kill(); process.WaitForExit(); } RegistryHelper.RunAs(RegistryHelper.Identity.TrustedInstaller, () => File.Delete(path)); }, null),

			// download the directx redistributable
			("Downloading DirectX Redistributable", async () => await DownloadHelper.Download("https://download.microsoft.com/download/8/4/A/84A35BF1-DAFE-4AE8-82AF-AD2AE20B6B14/directx_Jun2010_redist.exe", Path.GetTempPath(), "directx_Jun2010_redist.exe", new InstallPageReporter()), null),

			// extract the directx redistributable
			("Extracting DirectX Redistributable", async () => await ExtractHelper.Extract(Path.Combine(Path.GetTempPath(), "directx_Jun2010_redist.exe"), Path.Combine(Path.GetTempPath(), "directx_Jun2010_redist")),null),

			// install the directx redistributable
			("Installing DirectX Redistributable", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Path.GetTempPath(), "directx_Jun2010_redist", "DXSetup.exe"), Arguments = "/silent", WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), null),
			("Cleaning up DirectX Redistributable files", async () => { string path = Path.Combine(Path.GetTempPath(), "directx_Jun2010_redist.exe"); foreach (Process process in ProcessesHelper.GetLockingProcesses(path)) { process.Kill(); process.WaitForExit(); } RegistryHelper.RunAs(RegistryHelper.Identity.TrustedInstaller, () => File.Delete(path)); }, null),
			("Cleaning up DirectX Redistributable files", async () => { string path = Path.Combine(Path.GetTempPath(), "directx_Jun2010_redist"); foreach (Process process in ProcessesHelper.GetLockingProcesses(path)) { process.Kill(); process.WaitForExit(); } RegistryHelper.RunAs(RegistryHelper.Identity.TrustedInstaller, () => Directory.Delete(path, true)); }, null)
		];
	}
}

