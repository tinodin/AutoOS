using AutoOS.Core.Helpers.Registry;
using AutoOS.Core.Helpers.Services;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Microsoft.Win32;
using System.Diagnostics;
using System.ServiceProcess;
using Windows.Win32;

namespace AutoOS.Views.Settings;

public sealed partial class SecurityPage : Page
{
	private bool initialUACState = false;
	private bool initialMemoryIntegrityState = false;
	private bool initialVBSState = false;
	private bool initialSpectreMeltdownState = false;
	private bool initialProcessMitigationsState = false;
	private bool isInitializingWindowsDefenderState = true;
	private bool isInitializingUACState = true;
	private bool isInitializingDEPState = true;
	private bool isInitializingMemoryIntegrityState = true;
	private bool isInitializingVBSState = true;
	private bool isInitializingSpectreMeltdownState = true;
	private bool isInitializingProcessMitigationsState = true;

	public SecurityPage()
	{
		InitializeComponent();
		GetWindowsDefenderState();
		GetUACState();
		GetDEPState();
		GetMemoryIntegrityState();
		GetVBSState();
		GetSpectreMeltdownState();
		GetProcessMitigationsState();
	}

	private void GetWindowsDefenderState()
	{
		// declare services and drivers
		(string[], int)[] groups =
		[
			(["SecurityHealthService", "Sense", "WdNisDrv", "WdNisSvc", "webthreatdefsvc"], 3),
			(["webthreatdefusersvc", "WinDefend"], 2),
			(["MsSecCore", "WdBoot", "WdFilter"], 0)
		];

		// check if values match
		bool isEnabled = true;
		foreach (var group in groups)
		{
			foreach (var service in group.Item1)
			{
				using var key = Registry.LocalMachine.OpenSubKey($@"SYSTEM\CurrentControlSet\Services\{service}");
				if (key == null) continue;

				var startValue = key.GetValue("Start");
				if (startValue == null || (int)startValue != group.Item2)
				{
					isEnabled = false;
					break;
				}
			}
			if (!isEnabled)
				break;
		}

		WindowsDefender.IsOn = isEnabled;

		bool isRunning = false;
		try
		{
			isRunning = new ServiceController("WinDefend").Status == ServiceControllerStatus.Running;
		}
		catch (InvalidOperationException)
		{
			WindowsDefender.IsOn = false;
		}

		if (WindowsDefender.IsOn && !isRunning || !WindowsDefender.IsOn && isRunning)
		{
			// remove infobar
			SecurityInfo.Children.Clear();

			// add infobar
			var infoBar = new InfoBar
			{
				Title = WindowsDefender.IsOn ? "Successfully enabled Windows Defender. A restart is required to apply the change." : "Successfully disabled Windows Defender. A restart is required to apply the change.",
				IsClosable = false,
				IsOpen = true,
				Severity = InfoBarSeverity.Success,
				Margin = new Thickness(0, 0, 0, 12)
			};
			infoBar.ActionButton = new Button
			{
				Content = "Restart",
				HorizontalAlignment = HorizontalAlignment.Right
			};
			((Button)infoBar.ActionButton).Click += (s, args) => Process.Start(new ProcessStartInfo("shutdown", "/r /f /t 0") { CreateNoWindow = true });
			SecurityInfo.Children.Add(infoBar);
		}
		isInitializingWindowsDefenderState = false;
	}

	private async void WindowsDefender_Toggled(object sender, RoutedEventArgs e)
	{
		if (isInitializingWindowsDefenderState) return;

		// disable hittestvisible to avoid double-clicking
		WindowsDefender.IsHitTestVisible = false;

		// remove infobar
		SecurityInfo.Children.Clear();

		// add infobar
		SecurityInfo.Children.Add(new InfoBar
		{
			Title = WindowsDefender.IsOn ? "Enabling Windows Defender..." : "Disabling Windows Defender...",
			IsClosable = false,
			IsOpen = true,
			Severity = InfoBarSeverity.Informational,
			Margin = new Thickness(0, 0, 0, 12)
		});

		// toggle windows defender
		if (WindowsDefender.IsOn)
		{
			RegistryHelper.RunAs(RegistryHelper.Identity.TrustedInstaller, () => ServicesHelper.StopService("wscsvc"));
			RegistryHelper.DeleteValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows Defender", "PassiveMode");
			RegistryHelper.DeleteValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows Defender", "DisableAntiSpyware");
			RegistryHelper.DeleteValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows Defender", "DisableAntiVirus");
			if (File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "SecurityHealthService.exee")))
				RegistryHelper.RunAs(RegistryHelper.Identity.TrustedInstaller, () => File.Move(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "SecurityHealthService.exee"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "SecurityHealthService.exe")));

			if (Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\MsSecCore", false)?.GetValue("Start") as int? != 0)
				RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\MsSecCore", "Start", 0, RegistryValueKind.DWord);
			if (Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\SecurityHealthService", false)?.GetValue("Start") as int? != 3)
				RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\SecurityHealthService", "Start", 3, RegistryValueKind.DWord);
			if (Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\Sense", false)?.GetValue("Start") as int? != 3)
				RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Sense", "Start", 3, RegistryValueKind.DWord);
			if (Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\WdBoot", false)?.GetValue("Start") as int? != 0)
				RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\WdBoot", "Start", 0, RegistryValueKind.DWord);
			if (Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\WdFilter", false)?.GetValue("Start") as int? != 0)
				RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\WdFilter", "Start", 0, RegistryValueKind.DWord);
			if (Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\WdNisDrv", false)?.GetValue("Start") as int? != 3)
				RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\WdNisDrv", "Start", 3, RegistryValueKind.DWord);
			if (Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\WdNisSvc", false)?.GetValue("Start") as int? != 3)
				RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\WdNisSvc", "Start", 3, RegistryValueKind.DWord);
			if (Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\webthreatdefsvc", false)?.GetValue("Start") as int? != 3)
				RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\webthreatdefsvc", "Start", 3, RegistryValueKind.DWord);
			if (Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\webthreatdefusersvc", false)?.GetValue("Start") as int? != 2)
				RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\webthreatdefusersvc", "Start", 2, RegistryValueKind.DWord);
			if (Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\WinDefend", false)?.GetValue("Start") as int? != 2)
				RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\WinDefend", "Start", 2, RegistryValueKind.DWord);
			if (Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\wscsvc", false)?.GetValue("Start") as int? != 2)
				RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\wscsvc", "Start", 2, RegistryValueKind.DWord);

			await RegistryHelper.RunAs(RegistryHelper.Identity.TrustedInstaller, new ProcessStartInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Windows Defender", "MpCmdRun.exe"), "-EnableService -HighPriority") { CreateNoWindow = true });
		}
		else
		{
			bool IsTamperOn()
			{
				return (Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows Defender\Features", false)?.GetValue("TamperProtection") as int?) != 4;
			}

			bool IsRealtimeOn()
			{
				return (Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows Defender\Real-Time Protection", false)?.GetValue("DisableRealtimeMonitoring") as int?) != 1;
			}

			if (IsTamperOn() || IsRealtimeOn())
			{
				var panel = new StackPanel
				{
					Spacing = 8
				};

				var dialogProgressBar = new ProgressBar
				{
					IsIndeterminate = true
				};
				panel.Children.Add(dialogProgressBar);

				var dialogInfoText = new TextBlock { };

				var dialogHyperlink = new Hyperlink { };
				dialogHyperlink.Inlines.Add(new Run
				{
					Text = "Windows Security"
				});

				dialogHyperlink.Click += async (_, __) =>
				{
					await Task.Run(() =>
					{
						try
						{
							Process.Start(new ProcessStartInfo
							{
								FileName = "windowsdefender://threatsettings",
								UseShellExecute = true
							});
						}
						catch { }
					});
				};

				dialogInfoText.Inlines.Add(new Run { Text = "Open " });
				dialogInfoText.Inlines.Add(dialogHyperlink);
				dialogInfoText.Inlines.Add(new Run
				{
					Text = " and disable Real-time protection and Tamper Protection."
				});

				var dialogInfoBar = new InfoBar
				{
					IsOpen = true,
					Severity = InfoBarSeverity.Informational,
					IsClosable = false,
					Content = new Grid
					{
						Padding = new Thickness(12, 0, 16, 0),
						Children = { dialogInfoText }
					}
				};

				panel.Children.Add(dialogInfoBar);

				var contentDialog = new ContentDialog
				{
					Title = "Disable Windows Security",
					Content = panel,
					PrimaryButtonText = "Done",
					IsPrimaryButtonEnabled = false,
					XamlRoot = XamlRoot
				};

				contentDialog.Resources["ContentDialogMaxWidth"] = 800;

				contentDialog.Opened += async (_, __) =>
				{
					while (true)
					{
						await Task.Delay(1000);

						if (!IsTamperOn() && !IsRealtimeOn())
						{
							contentDialog.IsPrimaryButtonEnabled = true;
							dialogProgressBar.IsIndeterminate = false;
							dialogProgressBar.Value = 100;
							dialogProgressBar.Foreground = new SolidColorBrush((Windows.UI.Color)Application.Current.Resources["SystemFillColorSuccess"]);
							dialogInfoBar.Severity = InfoBarSeverity.Success;
							dialogInfoText.Inlines.Clear();
							dialogInfoText.Inlines.Add(new Run
							{
								Text = "Windows Security is disabled. Click done to continue."
							});

							foreach (var process in Process.GetProcessesByName("SecHealthUI"))
								process.Kill();

							break;
						}
					}
				};

				await contentDialog.ShowAsync();
			}

			RegistryHelper.RunAs(RegistryHelper.Identity.TrustedInstaller, () => ServicesHelper.StopService("wscsvc"));
			RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows Defender", "PassiveMode", 1, RegistryValueKind.DWord);
			RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows Defender", "DisableAntiSpyware", 1, RegistryValueKind.DWord);
			RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows Defender", "DisableAntiVirus", 1, RegistryValueKind.DWord);
			await RegistryHelper.RunAs(RegistryHelper.Identity.TrustedInstaller, new ProcessStartInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Windows Defender", "MpCmdRun.exe"), "-DisableService -HighPriority") { CreateNoWindow = true });
			while (new ServiceController("WdFilter").Status != ServiceControllerStatus.Stopped) await Task.Delay(100);
			foreach (Process process in Process.GetProcessesByName("SecurityHealthSystray"))
			{
				process.Kill(); process.WaitForExit();
			}
			RegistryHelper.RunAs(RegistryHelper.Identity.TrustedInstaller, () => ServicesHelper.KillServiceProcess("SecurityHealthService"));
			RegistryHelper.RunAs(RegistryHelper.Identity.TrustedInstaller, () => ServicesHelper.StopService("webthreatdefsvc"));
			RegistryHelper.RunAs(RegistryHelper.Identity.TrustedInstaller, () => ServicesHelper.StopService("webthreatdefusersvc"));
			RegistryHelper.RunAs(RegistryHelper.Identity.TrustedInstaller, () => ServicesHelper.StopService("wscsvc"));
			if (File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "SecurityHealthService.exe")))
				RegistryHelper.RunAs(RegistryHelper.Identity.TrustedInstaller, () => File.Move(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "SecurityHealthService.exe"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "SecurityHealthService.exee")));
			RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\MsSecCore", "Start", 4, RegistryValueKind.DWord);
			RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\SecurityHealthService", "Start", 4, RegistryValueKind.DWord);
			RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Sense", "Start", 4, RegistryValueKind.DWord);
			RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\WdBoot", "Start", 4, RegistryValueKind.DWord);
			RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\WdFilter", "Start", 4, RegistryValueKind.DWord);
			RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\WdNisDrv", "Start", 4, RegistryValueKind.DWord);
			RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\WdNisSvc", "Start", 4, RegistryValueKind.DWord);
			RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\webthreatdefsvc", "Start", 4, RegistryValueKind.DWord);
			RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\webthreatdefusersvc", "Start", 4, RegistryValueKind.DWord);
			RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\WinDefend", "Start", 4, RegistryValueKind.DWord);
			RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\wscsvc", "Start", 4, RegistryValueKind.DWord);
		}

		// delay
		await Task.Delay(200);

		// re-enable hittestvisible
		WindowsDefender.IsHitTestVisible = true;

		// remove infobar
		SecurityInfo.Children.Clear();

		// add infobar
		var infoBar = new InfoBar
		{
			Title = WindowsDefender.IsOn ? "Successfully enabled Windows Defender." : "Successfully disabled Windows Defender.",
			IsClosable = false,
			IsOpen = true,
			Severity = InfoBarSeverity.Success,
			Margin = new Thickness(0, 0, 0, 12)
		};
		SecurityInfo.Children.Add(infoBar);

		// add restart button
		var serviceController = new ServiceController("WinDefend");
		bool isRunning = serviceController.Status == ServiceControllerStatus.Running;

		if (WindowsDefender.IsOn && !isRunning || !WindowsDefender.IsOn && isRunning)
		{
			infoBar.Title += " A restart is required to apply the change.";
			infoBar.ActionButton = new Button
			{
				Content = "Restart",
				HorizontalAlignment = HorizontalAlignment.Right
			};
			((Button)infoBar.ActionButton).Click += (s, args) => Process.Start(new ProcessStartInfo("shutdown", "/r /f /t 0") { CreateNoWindow = true });
		}
		else
		{
			// delay
			await Task.Delay(2000);

			// remove infobar
			SecurityInfo.Children.Clear();
		}
	}

	private void GetUACState()
	{
		// check registry value
		if ((int?)Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System")?.GetValue("EnableLUA") == 1)
		{
			UAC.IsOn = true;
			initialUACState = true;
		}
		isInitializingUACState = false;
	}

	private async void UAC_Toggled(object sender, RoutedEventArgs e)
	{
		if (isInitializingUACState) return;

		// disable hittestvisible to avoid double-clicking
		UAC.IsHitTestVisible = false;

		// remove infobar
		SecurityInfo.Children.Clear();

		// add infobar
		SecurityInfo.Children.Add(new InfoBar
		{
			Title = UAC.IsOn ? "Enabling User Account Control (UAC)..." : "Disabling User Account Control (UAC)...",
			IsClosable = false,
			IsOpen = true,
			Severity = InfoBarSeverity.Informational,
			Margin = new Thickness(0, 0, 0, 12)
		});

		// toggle uac
		using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", true))
		{
			key.SetValue("EnableLUA", UAC.IsOn ? 1 : 0, RegistryValueKind.DWord);
			key.SetValue("PromptOnSecureDesktop", UAC.IsOn ? 1 : 0, RegistryValueKind.DWord);
			key.SetValue("ConsentPromptBehaviorAdmin", UAC.IsOn ? 5 : 0, RegistryValueKind.DWord);
		}

		// delay
		await Task.Delay(500);

		// re-enable hittestvisible
		UAC.IsHitTestVisible = true;

		// remove infobar
		SecurityInfo.Children.Clear();

		// add infobar
		var infoBar = new InfoBar
		{
			Title = UAC.IsOn ? "Successfully enabled User Account Control (UAC)." : "Successfully disabled User Account Control (UAC).",
			IsClosable = false,
			IsOpen = true,
			Severity = InfoBarSeverity.Success,
			Margin = new Thickness(0, 0, 0, 12)
		};
		SecurityInfo.Children.Add(infoBar);

		// add restart button if needed
		if (UAC.IsOn != initialUACState)
		{
			infoBar.Title += " A restart is required to apply the change.";
			infoBar.ActionButton = new Button
			{
				Content = "Restart",
				HorizontalAlignment = HorizontalAlignment.Right
			};
			((Button)infoBar.ActionButton).Click += (s, args) =>
			Process.Start(new ProcessStartInfo("shutdown", "/r /f /t 0") { CreateNoWindow = true });
		}
		else
		{
			// delay
			await Task.Delay(2000);

			// remove infobar
			SecurityInfo.Children.Clear();
		}
	}

	private void GetDEPState()
	{
		// get active state
		int policy = (int)PInvoke.GetSystemDEPPolicy();

		// get state
		var output = Process.Start(new ProcessStartInfo("cmd.exe", "/c bcdedit /enum {current}") { CreateNoWindow = true, RedirectStandardOutput = true }).StandardOutput.ReadToEnd();

		if (output.Contains("nx                      OptIn"))
		{
			if (policy == 0)
			{
				// remove infobar
				SecurityInfo.Children.Clear();

				// add infobar
				var infoBar = new InfoBar
				{
					Title = DEP.IsOn ? "Successfully disabled Data Execution Prevention (DEP). A restart is required to apply the change." : "Successfully enabled Data Execution Prevention (DEP). A restart is required to apply the change.",
					IsClosable = false,
					IsOpen = true,
					Severity = InfoBarSeverity.Success,
					Margin = new Thickness(0, 0, 0, 12)
				};
				infoBar.ActionButton = new Button
				{
					Content = "Restart",
					HorizontalAlignment = HorizontalAlignment.Right
				};
				((Button)infoBar.ActionButton).Click += (s, args) =>
				Process.Start(new ProcessStartInfo("shutdown", "/r /f /t 0") { CreateNoWindow = true });
				SecurityInfo.Children.Add(infoBar);
			}

			DEP.IsOn = true;
		}
		else if (output.Contains("nx                      AlwaysOff"))
		{
			if (policy == 2)
			{
				// remove infobar
				SecurityInfo.Children.Clear();

				// add infobar
				var infoBar = new InfoBar
				{
					Title = DEP.IsOn ? "Successfully enabled Data Execution Prevention (DEP). A restart is required to apply the change." : "Successfully disabled Data Execution Prevention (DEP). A restart is required to apply the change.",
					IsClosable = false,
					IsOpen = true,
					Severity = InfoBarSeverity.Success,
					Margin = new Thickness(0, 0, 0, 12),
					ActionButton = new Button
					{
						Content = "Restart",
						HorizontalAlignment = HorizontalAlignment.Right
					}
				};
				((Button)infoBar.ActionButton).Click += (s, args) =>
				Process.Start(new ProcessStartInfo("shutdown", "/r /f /t 0") { CreateNoWindow = true });
				SecurityInfo.Children.Add(infoBar);
			}
		}

		isInitializingDEPState = false;
	}

	private async void DEP_Toggled(object sender, RoutedEventArgs e)
	{
		if (isInitializingDEPState) return;

		// disable hittestvisible to avoid double-clicking
		DEP.IsHitTestVisible = false;

		// get active state
		int policy = (int)PInvoke.GetSystemDEPPolicy();

		// remove infobar
		SecurityInfo.Children.Clear();

		// add infobar
		SecurityInfo.Children.Add(new InfoBar
		{
			Title = DEP.IsOn ? "Enabling Data Execution Prevention (DEP)..." : "Disabling Data Execution Prevention (DEP)...",
			IsClosable = false,
			IsOpen = true,
			Severity = InfoBarSeverity.Informational,
			Margin = new Thickness(0, 0, 0, 12)
		});

		// toggle dep
		var output = Process.Start(new ProcessStartInfo("cmd.exe", $"/c {(DEP.IsOn ? "bcdedit /set nx OptIn" : "bcdedit /set nx AlwaysOff")}") { CreateNoWindow = true, RedirectStandardOutput = true }).StandardOutput.ReadToEnd();

		if (output.Contains("error"))
		{
			// delay
			await Task.Delay(500);

			// re-enable hittestvisible
			DEP.IsHitTestVisible = true;

			// remove infobar
			SecurityInfo.Children.Clear();

			// toggle back
			isInitializingDEPState = true;
			DEP.IsOn = !DEP.IsOn;
			isInitializingDEPState = false;

			// add infobar
			SecurityInfo.Children.Add(new InfoBar
			{
				Title = "Failed to disable Data Execution Prevention (DEP) because secure boot is enabled.",
				IsClosable = false,
				IsOpen = true,
				Severity = InfoBarSeverity.Error,
				Margin = new Thickness(0, 0, 0, 12)
			});

			// delay
			await Task.Delay(2000);

			// remove infobar
			SecurityInfo.Children.Clear();
		}
		else
		{
			// delay
			await Task.Delay(500);

			// re-enable hittestvisible
			DEP.IsHitTestVisible = true;

			// remove infobar
			SecurityInfo.Children.Clear();

			// add infobar
			var infoBar = new InfoBar
			{
				Title = DEP.IsOn ? "Successfully enabled Data Execution Prevention (DEP)." : "Successfully disabled Data Execution Prevention (DEP).",
				IsClosable = false,
				IsOpen = true,
				Severity = InfoBarSeverity.Success,
				Margin = new Thickness(0, 0, 0, 12)
			};
			SecurityInfo.Children.Add(infoBar);

			// add restart button if needed
			if ((DEP.IsOn && policy == 0) || (!DEP.IsOn && policy == 2))
			{
				infoBar.Title += " A restart is required to apply the change.";
				infoBar.ActionButton = new Button
				{
					Content = "Restart",
					HorizontalAlignment = HorizontalAlignment.Right
				};
				((Button)infoBar.ActionButton).Click += (s, args) =>
				Process.Start(new ProcessStartInfo("shutdown", "/r /f /t 0") { CreateNoWindow = true });
			}
			else
			{
				// delay
				await Task.Delay(2000);

				// remove infobar
				SecurityInfo.Children.Clear();
			}
		}
	}

	private void GetMemoryIntegrityState()
	{
		// get state
		if ((Registry.GetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity", "Enabled", 0) is int val && val == 1))
		{
			MemoryIntegrity.IsOn = true;
			initialMemoryIntegrityState = true;
		}
		isInitializingMemoryIntegrityState = false;
	}

	private async void MemoryIntegrity_Toggled(object sender, RoutedEventArgs e)
	{
		if (isInitializingMemoryIntegrityState) return;

		// disable hittestvisible to avoid double-clicking
		MemoryIntegrity.IsHitTestVisible = false;

		// remove infobar
		SecurityInfo.Children.Clear();

		// add infobar
		SecurityInfo.Children.Add(new InfoBar
		{
			Title = MemoryIntegrity.IsOn ? "Enabling Hypervisor Enforced Code Integrity (HVCI)..." : "Disabling Hypervisor Enforced Code Integrity (HVCI)...",
			IsClosable = false,
			IsOpen = true,
			Severity = InfoBarSeverity.Informational,
			Margin = new Thickness(0, 0, 0, 12)
		});

		// toggle
		Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity", "Enabled", MemoryIntegrity.IsOn ? 1 : 0, RegistryValueKind.DWord);

		// delay
		await Task.Delay(500);

		// re-enable hittestvisible
		MemoryIntegrity.IsHitTestVisible = true;

		// remove infobar
		SecurityInfo.Children.Clear();

		// add infobar
		var infoBar = new InfoBar
		{
			Title = MemoryIntegrity.IsOn ? "Successfully enabled Hypervisor Enforced Code Integrity (HVCI)." : "Successfully disabled Hypervisor Enforced Code Integrity (HVCI).",
			IsClosable = false,
			IsOpen = true,
			Severity = InfoBarSeverity.Success,
			Margin = new Thickness(0, 0, 0, 12)
		};
		SecurityInfo.Children.Add(infoBar);

		// add restart button if needed
		if (MemoryIntegrity.IsOn != initialMemoryIntegrityState)
		{
			infoBar.Title += " A restart is required to apply the change.";
			infoBar.ActionButton = new Button
			{
				Content = "Restart",
				HorizontalAlignment = HorizontalAlignment.Right
			};
			((Button)infoBar.ActionButton).Click += (s, args) =>
			Process.Start(new ProcessStartInfo("shutdown", "/r /f /t 0") { CreateNoWindow = true });
		}
		else
		{
			// delay
			await Task.Delay(2000);

			// remove infobar
			SecurityInfo.Children.Clear();
		}
	}

	private void GetVBSState()
	{
		// get state
		if ((Registry.GetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\WindowsHello", "Enabled", 0) is int helloVal && helloVal == 1) || (Registry.GetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\DeviceGuard", "EnableVirtualizationBasedSecurity", 0) is int vbsVal && vbsVal == 1))
		{
			VirtualizationBasedSecurity.IsOn = true;
			initialVBSState = true;
		}

		isInitializingVBSState = false;
	}

	private async void VirtualizationBasedSecurity_Toggled(object sender, RoutedEventArgs e)
	{
		if (isInitializingVBSState) return;

		// disable hittestvisible to avoid double-clicking
		VirtualizationBasedSecurity.IsHitTestVisible = false;

		// remove infobar
		SecurityInfo.Children.Clear();

		// add infobar
		SecurityInfo.Children.Add(new InfoBar
		{
			Title = VirtualizationBasedSecurity.IsOn ? "Enabling Virtualization-based Security (VBS)..." : "Disabling Virtualization-based Security (VBS)...",
			IsClosable = false,
			IsOpen = true,
			Severity = InfoBarSeverity.Informational,
			Margin = new Thickness(0, 0, 0, 12)
		});

		// toggle
		Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\DeviceGuard", "EnableVirtualizationBasedSecurity", VirtualizationBasedSecurity.IsOn ? 1 : 0, RegistryValueKind.DWord);

		if (!VirtualizationBasedSecurity.IsOn)
		{
			if (Registry.GetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\WindowsHello", "Enabled", 0) is int helloVal && helloVal == 1)
			{
				var dialog = new ContentDialog
				{
					Title = "Disable Windows Hello",
					Content = "Windows Hello has to be disabled in order to disable Virtualization-based Security (VBS). Do you want to disable Windows Hello?",
					PrimaryButtonText = "Yes",
					DefaultButton = ContentDialogButton.Close,
					CloseButtonText = "No",
					XamlRoot = XamlRoot
				};

				var result = await dialog.ShowAsync();

				if (result == ContentDialogResult.Primary)
				{
					Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\WindowsHello", "Enabled", 0, RegistryValueKind.DWord);
				}
				else
				{
					isInitializingVBSState = true;
					VirtualizationBasedSecurity.IsOn = true;
					isInitializingVBSState = false;

					// re-enable hittestvisible
					VirtualizationBasedSecurity.IsHitTestVisible = true;

					// remove infobar
					SecurityInfo.Children.Clear();

					return;
				}
			}
		}

		// delay
		await Task.Delay(500);

		// re-enable hittestvisible
		VirtualizationBasedSecurity.IsHitTestVisible = true;

		// remove infobar
		SecurityInfo.Children.Clear();

		// add infobar
		var infoBar = new InfoBar
		{
			Title = VirtualizationBasedSecurity.IsOn ? "Successfully enabled Virtualization-based Security (VBS)." : "Successfully disabled Virtualization-based Security (VBS).",
			IsClosable = false,
			IsOpen = true,
			Severity = InfoBarSeverity.Success,
			Margin = new Thickness(0, 0, 0, 12)
		};
		SecurityInfo.Children.Add(infoBar);

		// add restart button if needed
		if (VirtualizationBasedSecurity.IsOn != initialVBSState)
		{
			infoBar.Title += " A restart is required to apply the change.";
			infoBar.ActionButton = new Button
			{
				Content = "Restart",
				HorizontalAlignment = HorizontalAlignment.Right
			};
			((Button)infoBar.ActionButton).Click += (s, args) =>
				Process.Start(new ProcessStartInfo("shutdown", "/r /f /t 0") { CreateNoWindow = true });
		}
		else
		{
			// delay
			await Task.Delay(2000);

			// remove infobar
			SecurityInfo.Children.Clear();
		}
	}

	private void GetSpectreMeltdownState()
	{
		// check registry
		string cpuVendor = (string)Registry.GetValue(@"HKEY_LOCAL_MACHINE\HARDWARE\DESCRIPTION\System\CentralProcessor\0", "VendorIdentifier", null);
		int? featureSettings = (int?)Registry.GetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "FeatureSettings", null);
		int? featureSettingsOverrideMask = (int?)Registry.GetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "FeatureSettingsOverrideMask", null);
		int? featureSettingsOverride = (int?)Registry.GetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "FeatureSettingsOverride", null);

		if (cpuVendor.Contains("GenuineIntel"))
		{
			if (featureSettings == 0 && featureSettingsOverrideMask == null && featureSettingsOverride == null)
			{
				initialSpectreMeltdownState = true;
				SpectreMeltdown.IsOn = true;
			}
		}
		else if (cpuVendor.Contains("AuthenticAMD"))
		{
			if (featureSettings == 1 && featureSettingsOverrideMask == 3 && featureSettingsOverride == 64)
			{
				initialSpectreMeltdownState = true;
				SpectreMeltdown.IsOn = true;
			}
		}
		isInitializingSpectreMeltdownState = false;
	}

	private async void SpectreMeltdown_Toggled(object sender, RoutedEventArgs e)
	{
		if (isInitializingSpectreMeltdownState) return;

		// disable hittestvisible to avoid double-clicking
		SpectreMeltdown.IsHitTestVisible = false;

		// remove infobar
		SecurityInfo.Children.Clear();

		// add infobar
		SecurityInfo.Children.Add(new InfoBar
		{
			Title = SpectreMeltdown.IsOn ? "Enabling Spectre & Meltdown Mitigations..." : "Disabling Spectre & Meltdown Mitigations...",
			IsClosable = false,
			IsOpen = true,
			Severity = InfoBarSeverity.Informational,
			Margin = new Thickness(0, 0, 0, 12)
		});

		if (SpectreMeltdown.IsOn)
		{
			string cpuVendor = (string)Registry.GetValue(@"HKEY_LOCAL_MACHINE\HARDWARE\DESCRIPTION\System\CentralProcessor\0", "VendorIdentifier", null);

			if (cpuVendor.Contains("GenuineIntel"))
			{
				// restore default values for enabling on intel
				Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "FeatureSettings", 0, RegistryValueKind.DWord);
				Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", writable: true).DeleteValue("FeatureSettingsOverrideMask", throwOnMissingValue: false);
				Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", writable: true).DeleteValue("FeatureSettingsOverride", throwOnMissingValue: false);
			}
			else if (cpuVendor.Contains("AuthenticAMD"))
			{
				// set custom values to enable all mitigations on amd
				Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "FeatureSettings", 1, RegistryValueKind.DWord);
				Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "FeatureSettingsOverrideMask", 3, RegistryValueKind.DWord);
				Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "FeatureSettingsOverride", 64, RegistryValueKind.DWord);
			}
		}
		else
		{
			// disable spectre & meltdown
			Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "FeatureSettings", 1, RegistryValueKind.DWord);
			Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "FeatureSettingsOverrideMask", 3, RegistryValueKind.DWord);
			Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "FeatureSettingsOverride", 3, RegistryValueKind.DWord);
		}

		// delay
		await Task.Delay(500);

		// re-enable hittestvisible
		SpectreMeltdown.IsHitTestVisible = true;

		// remove infobar
		SecurityInfo.Children.Clear();

		// add infobar
		var infoBar = new InfoBar
		{
			Title = SpectreMeltdown.IsOn ? "Successfully enabled Spectre & Meltdown Mitigations." : "Successfully disabled Spectre & Meltdown Mitigations.",
			IsClosable = false,
			IsOpen = true,
			Severity = InfoBarSeverity.Success,
			Margin = new Thickness(0, 0, 0, 12)
		};
		SecurityInfo.Children.Add(infoBar);

		// add restart button if needed
		if (SpectreMeltdown.IsOn != initialSpectreMeltdownState)
		{
			infoBar.Title += " A restart is required to apply the change.";
			infoBar.ActionButton = new Button
			{
				Content = "Restart",
				HorizontalAlignment = HorizontalAlignment.Right
			};
			((Button)infoBar.ActionButton).Click += (s, args) =>
			Process.Start(new ProcessStartInfo("shutdown", "/r /f /t 0") { CreateNoWindow = true });
		}
		else
		{
			// delay
			await Task.Delay(2000);

			// remove infobar
			SecurityInfo.Children.Clear();
		}
	}

	private void GetProcessMitigationsState()
	{
		// get state
		if (Registry.GetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\kernel", "MitigationOptions", null) == null && Registry.GetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\kernel", "MitigationAuditOptions", null) == null)
		{
			ProcessMitigations.IsOn = true;
			initialProcessMitigationsState = true;
		}
		isInitializingProcessMitigationsState = false;
	}

	private async void ProcessMitigations_Toggled(object sender, RoutedEventArgs e)
	{
		if (isInitializingProcessMitigationsState) return;

		// disable hittestvisible to avoid double-clicking
		ProcessMitigations.IsHitTestVisible = false;

		// remove infobar
		SecurityInfo.Children.Clear();

		// add infobar
		SecurityInfo.Children.Add(new InfoBar
		{
			Title = ProcessMitigations.IsOn ? "Enabling Process Mitigations..." : "Disabling Process Mitigations...",
			IsClosable = false,
			IsOpen = true,
			Severity = InfoBarSeverity.Informational,
			Margin = new Thickness(0, 0, 0, 12)
		});

		// toggle process mitigations
		var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Session Manager\kernel", true);
		if (key != null)
		{
			if (ProcessMitigations.IsOn)
			{
				key.DeleteValue("MitigationAuditOptions", false);
				key.DeleteValue("MitigationOptions", false);
			}
			else
			{
				key.SetValue("MitigationAuditOptions", new byte[] { 0x22, 0x22, 0x22, 0x22, 0x22, 0x22, 0x22, 0x22, 0x22, 0x22, 0x22, 0x22, 0x22, 0x22, 0x22, 0x22, 0x22, 0x22, 0x22, 0x22, 0x22, 0x22, 0x22, 0x22 });
				key.SetValue("MitigationOptions", new byte[] { 0x22, 0x22, 0x22, 0x22, 0x22, 0x22, 0x22, 0x22, 0x22, 0x22, 0x22, 0x22, 0x22, 0x22, 0x22, 0x22, 0x22, 0x22, 0x22, 0x22, 0x22, 0x22, 0x22, 0x22 });
			}
		}

		// delay
		await Task.Delay(500);

		// re-enable hittestvisible
		ProcessMitigations.IsHitTestVisible = true;

		// remove infobar
		SecurityInfo.Children.Clear();

		// add infobar
		var infoBar = new InfoBar
		{
			Title = ProcessMitigations.IsOn ? "Successfully enabled Process Mitigations." : "Successfully disabled Process Mitigations.",
			IsClosable = false,
			IsOpen = true,
			Severity = InfoBarSeverity.Success,
			Margin = new Thickness(0, 0, 0, 12)
		};
		SecurityInfo.Children.Add(infoBar);

		// add restart button if needed
		if (ProcessMitigations.IsOn != initialProcessMitigationsState)
		{
			infoBar.Title += " A restart is required to apply the change.";
			infoBar.ActionButton = new Button
			{
				Content = "Restart",
				HorizontalAlignment = HorizontalAlignment.Right
			};
			((Button)infoBar.ActionButton).Click += (s, args) =>
			Process.Start(new ProcessStartInfo("shutdown", "/r /f /t 0") { CreateNoWindow = true });
		}
		else
		{
			// delay
			await Task.Delay(2000);

			// remove infobar
			SecurityInfo.Children.Clear();
		}
	}
}
