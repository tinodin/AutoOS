﻿using AutoOS.Helpers;
using AutoOS.Views.Installer.Actions;
using Microsoft.UI.Xaml.Media;
using System.Diagnostics;
using Windows.Storage;

namespace AutoOS.Views.Installer.Stages;

public static class GraphicsStage
{
    private static readonly ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
    public static async Task Run()
    {
        bool? Intel10th = PreparingStage.Intel10th;
        bool? Intel11th = PreparingStage.Intel11th;
        bool? NVIDIA = PreparingStage.NVIDIA;
        bool? AMD = PreparingStage.AMD;
        bool? HDCP = PreparingStage.HDCP;
        bool? AlwaysShowTrayIcons = PreparingStage.AlwaysShowTrayIcons;
        bool? MSI = PreparingStage.MSI;
        bool? CRU = PreparingStage.CRU;

        InstallPage.Status.Text = "Configuring Graphics Card...";

        string previousTitle = string.Empty;
        int stagePercentage = 5;

        var (_, _, newestDownloadUrl) = await NvidiaHelper.CheckUpdate();

        var actions = new List<(string Title, Func<Task> Action, Func<bool> Condition)>
        {
            //// download the latest intel driver
            //("Downloading the latest Intel Driver", async () => await ProcessActions.RunDownload("https://downloadmirror.intel.com/764512/gfx_win_101.2115.zip", Path.GetTempPath(), "driver.zip"), () => Intel10th == true),

            //// extract the driver
            //("Extracting the Intel driver", async () => await ProcessActions.RunExtract(Path.Combine(Path.GetTempPath(), "driver.zip"), Path.Combine(Path.GetTempPath(), "driver")), () => Intel10th == true),

            //// install the driver
            //("Installing the Intel driver", async () => await ProcessActions.RunNsudo("CurrentUser", @"""%TEMP%\driver\Installer.exe"" /silent"), () => Intel10th == true),
            //("Installing the Intel driver", async () => await ProcessActions.RefreshUI(), () => Intel10th == true),
           
            // download the latest intel driver
            ("Downloading the latest Intel Driver", async () => await ProcessActions.RunDownload("https://downloadmirror.intel.com/850983/gfx_win_101.2135.exe", Path.GetTempPath(), "driver.exe"), () => Intel10th == true),

            // extract the driver
            ("Extracting the Intel driver", async () => await ProcessActions.RunExtract(Path.Combine(Path.GetTempPath(), "driver.exe"), Path.Combine(Path.GetTempPath(), "driver")), () => Intel10th == true),

            // install the driver
            ("Installing the Intel driver", async () => await ProcessActions.RunNsudo("CurrentUser", @"""%TEMP%\driver\Installer.exe"" /silent"), () => Intel10th == true),
            ("Installing the Intel driver", async () => await ProcessActions.RefreshUI(), () => Intel10th == true),

            // download the latest intel driver
            ("Downloading the latest Intel Driver", async () => await ProcessActions.RunDownload("https://downloadmirror.intel.com/851966/gfx_win_101.6734.exe", Path.GetTempPath(), "driver.exe"), () => Intel11th == true),

            // extract the driver
            ("Extracting the Intel driver", async () => await ProcessActions.RunExtract(Path.Combine(Path.GetTempPath(), "driver.exe"), Path.Combine(Path.GetTempPath(), "driver")), () => Intel11th == true),

            // install the driver
            ("Installing the Intel driver", async () => await ProcessActions.RunNsudo("CurrentUser", @"""%TEMP%\driver\Installer.exe"" /silent"), () => Intel11th == true),
            ("Installing the Intel driver", async () => await ProcessActions.RefreshUI(), () => Intel11th == true),

            // download the latest amd driver
            ("Downloading the latest AMD Driver", async () => await ProcessActions.RunDownload("https://www.dl.dropboxusercontent.com/scl/fi/pa7ifyh2d1y9fifhp95w3/AMD-Driver.zip?rlkey=65b49zip2e8i7m26eqw8uaxcx&st=9vhf0xo6&dl=0", Path.GetTempPath(), "driver.zip"), () => AMD == true),

            // extract the driver
            ("Extracting the AMD driver", async () => await ProcessActions.RunExtract(Path.Combine(Path.GetTempPath(), "driver.zip"), Path.Combine(Path.GetTempPath(), "driver")), () => AMD == true),

            // install the driver
            ("Installing the AMD driver", async () => await ProcessActions.RunNsudo("CurrentUser", @"""%TEMP%\driver\Setup.exe"" -install"), () => AMD == true),

            // download the latest nvidia driver                                                     
            ("Downloading the latest NVIDIA Driver", async () => await ProcessActions.RunDownload(newestDownloadUrl, Path.GetTempPath(), "driver.exe"), () => NVIDIA == true),

            // extract the driver
            ("Extracting the NVIDIA driver", async () => await ProcessActions.RunExtract(Path.Combine(Path.GetTempPath(), "driver.exe"), Path.Combine(Path.GetTempPath(), "driver")), () => NVIDIA == true),

            // strip the driver
            ("Stripping the NVIDIA driver", async () => await ProcessActions.RunNvidiaStrip(), () => NVIDIA == true),

            // install the nvidia driver
            ("Installing the NVIDIA driver", async () => await ProcessActions.RunNsudo("CurrentUser", @"""%TEMP%\driver\setup.exe"" /s"), () => NVIDIA == true),
            ("Installing the NVIDIA driver", async () => await ProcessActions.Sleep(3000), () => NVIDIA == true),
            ("Installing the NVIDIA driver", async () => await ProcessActions.RefreshUI(), () => NVIDIA == true),

            // apply custom resolution utility (cru) profile
            ("Importing Custom Resolution Utility (CRU) profile", async () => await ProcessActions.Sleep(1500), () => CRU == true),
            ("Importing Custom Resolution Utility (CRU) profile", async () => await ProcessActions.RunCustom(async () => await Task.Run(() => Process.Start(new ProcessStartInfo { FileName = localSettings.Values["CruProfile"]?.ToString(), Arguments = "-i" })!.WaitForExitAsync())), () => CRU == true),
            ("Applying Custom Resolution Utility (CRU) profile", async () => await ProcessActions.Sleep(1500), () => CRU == true),
            ("Applying Custom Resolution Utility (CRU) profile", async () => await ProcessActions.RunApplication("CRU", "restart64.exe", "/q"), () => CRU == true),
            ("Applying Custom Resolution Utility (CRU) profile", async () => await ProcessActions.Sleep(2000), () => CRU == true),
            ("Applying Custom Resolution Utility (CRU) profile", async () => await ProcessActions.RefreshUI(), () => CRU == true),

            // set the highest supported refresh rate for every monitor
            ("Setting the highest supported refresh rate for every monitor", async () => await ProcessActions.Sleep(1000), null),
            ("Setting the highest supported refresh rate for every monitor", async () => await ProcessActions.SetHighestRefreshRates(), null),

            // enable optimizations for windowed games
            ("Enabling optimizations for windowed games", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Microsoft\DirectX\UserGpuPreferences"" /v ""DirectXUserGlobalSettings"" /t REG_SZ /d ""SwapEffectUpgradeEnable=1;"" /f"), () => Intel10th == true),

            // configure settings
            ("Configuring settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Intel\Display\igfxcui\MediaKeys"" /v ""ProcAmpApplyAlways"" /t REG_DWORD /d 0 /f"), () => Intel10th == true),
            ("Configuring settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Intel\Display\igfxcui\MediaKeys"" /v ""ProcAmpBrightness"" /t REG_DWORD /d 0 /f"), () => Intel10th == true),
            ("Configuring settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Intel\Display\igfxcui\MediaKeys"" /v ""ProcAmpContrast"" /t REG_DWORD /d 0 /f"), () => Intel10th == true),
            ("Configuring settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Intel\Display\igfxcui\MediaKeys"" /v ""ProcAmpHue"" /t REG_DWORD /d 3221225472 /f"), () => Intel10th == true),
            ("Configuring settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Intel\Display\igfxcui\MediaKeys"" /v ""ProcAmpSaturation"" /t REG_DWORD /d 0 /f"), () => Intel10th == true),

            ("Configuring settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Intel\Display\igfxcui\MediaKeys"" /v ""SharpnessEnabledAlways"" /t REG_DWORD /d 0 /f"), () => Intel10th == true),
            ("Configuring settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Intel\Display\igfxcui\MediaKeys"" /v ""SharpnessFactor"" /t REG_DWORD /d 0 /f"), () => Intel10th == true),

            ("Configuring settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Intel\Display\igfxcui\MediaKeys"" /v ""NoiseReductionEnabledAlways"" /t REG_DWORD /d 0 /f"), () => Intel10th == true),
            ("Configuring settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Intel\Display\igfxcui\MediaKeys"" /v ""NoiseReductionAutoDetectEnabledAlways"" /t REG_DWORD /d 0 /f"), () => Intel10th == true),
            ("Configuring settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Intel\Display\igfxcui\MediaKeys"" /v ""NoiseReductionEnabledChroma"" /t REG_DWORD /d 0 /f"), () => Intel10th == true),
            ("Configuring settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Intel\Display\igfxcui\MediaKeys"" /v ""NoiseReductionFactor"" /t REG_DWORD /d 0 /f"), () => Intel10th == true),

            ("Configuring settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Intel\Display\igfxcui\MediaKeys"" /v ""EnableFMD"" /t REG_DWORD /d 0 /f"), () => Intel10th == true),

            ("Configuring settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Intel\Display\igfxcui\MediaKeys"" /v ""EnableSTE"" /t REG_DWORD /d 0 /f"), () => Intel10th == true),
            ("Configuring settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Intel\Display\igfxcui\MediaKeys"" /v ""SkinTone"" /t REG_DWORD /d 0 /f"), () => Intel10th == true),

            ("Configuring settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Intel\Display\igfxcui\MediaKeys"" /v ""EnableTCC"" /t REG_DWORD /d 0 /f"), () => Intel10th == true),
            ("Configuring settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Intel\Display\igfxcui\MediaKeys"" /v ""SatFactorRed"" /t REG_DWORD /d 0 /f"), () => Intel10th == true),
            ("Configuring settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Intel\Display\igfxcui\MediaKeys"" /v ""SatFactorGreen"" /t REG_DWORD /d 0 /f"), () => Intel10th == true),
            ("Configuring settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Intel\Display\igfxcui\MediaKeys"" /v ""SatFactorBlue"" /t REG_DWORD /d 0 /f"), () => Intel10th == true),
            ("Configuring settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Intel\Display\igfxcui\MediaKeys"" /v ""SatFactorCyan"" /t REG_DWORD /d 0 /f"), () => Intel10th == true),
            ("Configuring settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Intel\Display\igfxcui\MediaKeys"" /v ""SatFactorMagenta"" /t REG_DWORD /d 0 /f"), () => Intel10th == true),
            ("Configuring settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Intel\Display\igfxcui\MediaKeys"" /v ""SatFactorYellow"" /t REG_DWORD /d 0 /f"), () => Intel10th == true),

            ("Configuring settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Intel\Display\igfxcui\MediaKeys"" /v ""EnableACE"" /t REG_DWORD /d 0 /f"), () => Intel10th == true),
            ("Configuring settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Intel\Display\igfxcui\MediaKeys"" /v ""AceLevel"" /t REG_DWORD /d 0 /f"), () => Intel10th == true),

            ("Configuring settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Intel\Display\igfxcui\MediaKeys"" /v ""InputYUVRangeApplyAlways"" /t REG_DWORD /d 1 /f"), () => Intel10th == true),
            ("Configuring settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Intel\Display\igfxcui\MediaKeys"" /v ""InputYUVRange"" /t REG_DWORD /d 2 /f"), () => Intel10th == true),

            ("Configuring settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Intel\Display\igfxcui\MediaKeys"" /v ""EnableIS"" /t REG_DWORD /d 0 /f"), () => Intel10th == true),
            ("Configuring settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Intel\Display\igfxcui\MediaKeys"" /v ""EnableNLAS"" /t REG_DWORD /d 0 /f"), () => Intel10th == true),

            ("Configuring settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Intel\Display\igfxcui\MediaKeys"" /v ""UISharpnessOptimalEnabledAlways"" /t REG_DWORD /d 0 /f"), () => Intel10th == true),

            ("Configuring settings", async () => await ProcessActions.RunPowerShellScript("intelsettings.ps1", ""), () => Intel10th == true),

            // disable unnecessary services
            ("Disabling unnecessary services", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\igccservice"" /v ""Start"" /t REG_DWORD /d 4 /f"), () => Intel10th == true),
            ("Disabling unnecessary services", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\igfxCUIService2.0.0.0"" /v ""Start"" /t REG_DWORD /d 4 /f"), () => Intel10th == true),

            // disable high-definition-content-protection (hdcp)
            ("Disabling high-definition-content-protection (HDCP)", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\cphs"" /v ""Start"" /t REG_DWORD /d 4 /f"), () => Intel10th == true),
            ("Disabling high-definition-content-protection (HDCP)", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\cplspcon"" /v ""Start"" /t REG_DWORD /d 4 /f"), () => Intel10th == true),

            // configure settings
            ("Configuring settings", async () => await ProcessActions.RunPowerShellScript("amdsettings.ps1", ""), () => AMD == true),

            // disable the nvidia tray icon
            ("Disabling the NVIDIA tray icon", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\NVIDIA Corporation\NvTray"" /v StartOnLogin /t REG_DWORD /d 0 /f"), () => NVIDIA == true),

            // enable hardware accelerated gpu scheduling (hags)
            ("Enabling Hardware-accelerated GPU scheduling (HAGS)", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\GraphicsDrivers"" /v ""HwSchMode"" /t REG_DWORD /d 2 /f"), () => NVIDIA == true),

            // enable optimizations for windowed games
            ("Enabling optimizations for windowed games", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Microsoft\DirectX\UserGpuPreferences"" /v ""DirectXUserGlobalSettings"" /t REG_SZ /d ""SwapEffectUpgradeEnable=1;"" /f"), () => NVIDIA == true),

            // disable telemetry
            ("Disabling telemetry", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\Software\Nvidia Corporation\NvControlPanel2\Client"" /v ""OptInOrOutPreference"" /t REG_DWORD /d 0 /f"), () => NVIDIA == true),
            ("Disabling telemetry", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\nvlddmkm\Global\Startup"" /v ""SendTelemetryData"" /t REG_DWORD /d 0 /f"), () => NVIDIA == true),

            // use the advanced 3d image settings
            ("Using the advanced 3D image settings", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\NVIDIA Corporation\Global\NVTweak"" /v ""Gestalt"" /t REG_DWORD /d 515 /f"), () => NVIDIA == true),

            // import the optimized profile
            ("Importing the optimized profile", async () => await ProcessActions.ImportProfile("BaseProfile.nip"), () => NVIDIA == true),

            // configure physx to use gpu
            ("Configuring PhysX to use GPU", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\System\ControlSet001\Services\nvlddmkm\Global\NVTweak"" /v ""NvCplPhysxAuto"" /t REG_DWORD /d 0 /f"), () => NVIDIA == true),

            // configure color settings
            ("Configuring color settings", async () => await ProcessActions.RunPowerShellScript("colorsettings.ps1", ""), () => NVIDIA == true),
            ("Configuring color settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"cmd /c for /f ""delims="" %a in ('reg query HKLM\System\CurrentControlSet\Services\nvlddmkm\State\DisplayDatabase') do reg add ""%a"" /v ""ColorformatConfig"" /t REG_BINARY /d ""DB02000014000000000A00080000000003010000"" /f"), () => NVIDIA == true),

            // disable high-definition-content-protection (hdcp)
            ("Disabling high-definition-content-protection (HDCP)", async () => await ProcessActions.RunPowerShellScript("hdcp.ps1", ""), () => NVIDIA == true && HDCP == false),

            // disable scaling
            ("Disabling scaling", async () => await ProcessActions.RunNsudo("CurrentUser", @"cmd /c for %i in (Scaling) do for /f ""tokens=*"" %a in ('reg query ""HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers"" /s /f ""%i""^| findstr ""HKEY""') do reg add ""%a"" /v ""Scaling"" /t REG_DWORD /d 1 /f"), () => NVIDIA == true),

            // disable dynamic p-state
            ("Disabling dynamic p-state", async () => await ProcessActions.RunPowerShellScript("pstate.ps1", ""), () => NVIDIA == true),

            // download msi afterburner
            ("Downloading MSI Afterburner", async () => await ProcessActions.RunDownload("https://www.dl.dropboxusercontent.com/scl/fi/6dvl62kgm3z38x49752bt/MSI-Afterburner.zip?rlkey=h2m2riyjisrb3ph0i8j0q4eu5&st=l87whmmi&dl=0", Path.GetTempPath(), "MSI Afterburner.zip"), null),

            // install msi afterburner
            ("Installing MSI Afterburner", async () => await ProcessActions.RunExtract(Path.Combine(Path.GetTempPath(), "MSI Afterburner.zip"), @"C:\Program Files (x86)\MSI Afterburner"), null),
            ("Installing MSI Afterburner", async () => await ProcessActions.RunNsudo("CurrentUser", @"""C:\Program Files (x86)\MSI Afterburner\Redist\vcredist_x86.exe"" /q"), null),
            ("Installing MSI Afterburner", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Afterburner"" /v ""DisplayIcon"" /t REG_SZ /d ""C:\Program Files (x86)\MSI Afterburner\uninstall.exe"" /f"), null),
            ("Installing MSI Afterburner", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Afterburner"" /v ""DisplayName"" /t REG_SZ /d ""MSI Afterburner 4.6.6 Beta 6"" /f"), null),
            ("Installing MSI Afterburner", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Afterburner"" /v ""DisplayVersion"" /t REG_SZ /d ""4.6.6 Beta 6"" /f"), null),
            ("Installing MSI Afterburner", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Afterburner"" /v ""Publisher"" /t REG_SZ /d ""MSI Co., LTD"" /f"), null),
            ("Installing MSI Afterburner", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Afterburner"" /v ""UninstallString"" /t REG_SZ /d ""C:\Program Files (x86)\MSI Afterburner\uninstall.exe"" /f"), null),
            ("Installing MSI Afterburner", async () => await ProcessActions.RunNsudo("CurrentUser", @"cmd /c mkdir ""%APPDATA%\Microsoft\Windows\Start Menu\Programs\MSI Afterburner"" ""%APPDATA%\Microsoft\Windows\Start Menu\Programs\MSI Afterburner\SDK"""), null),
            ("Installing MSI Afterburner", async () => await ProcessActions.RunPowerShell(@"$Shell=New-Object -ComObject WScript.Shell; @(@{P='MSI Afterburner.lnk';T='C:\Program Files (x86)\MSI Afterburner\MSIAfterburner.exe'},@{P='ReadMe.lnk';T='C:\Program Files (x86)\MSI Afterburner\Doc\ReadMe.pdf'},@{P='Uninstall.lnk';T='C:\Program Files (x86)\MSI Afterburner\Uninstall.exe'},@{P='SDK\MSI Afterburner localization reference.lnk';T='C:\Program Files (x86)\MSI Afterburner\SDK\Doc\Localization reference.pdf'},@{P='SDK\MSI Afterburner skin format reference.lnk';T='C:\Program Files (x86)\MSI Afterburner\SDK\Doc\USF skin format reference.pdf'},@{P='SDK\Samples.lnk';T='C:\Program Files (x86)\MSI Afterburner\SDK\Samples\'}) | % {$Shortcut=$Shell.CreateShortcut([System.IO.Path]::Combine($env:APPDATA, 'Microsoft\Windows\Start Menu\Programs\MSI Afterburner', $_.P)); $Shortcut.TargetPath=$_.T; $Shortcut.Save()}"), null),

            // import msi afterburner profile
            ("Importing MSI Afterburner profile", async () => await ProcessActions.RunCustom(async () => await Task.Run(() => File.Copy(localSettings.Values["MsiProfile"]?.ToString(), Path.Combine(@"C:\Program Files (x86)\MSI Afterburner\Profiles\", Path.GetFileName(localSettings.Values["MsiProfile"]?.ToString()))))), () => MSI == true),

            // apply msi afterburner profile
            ("Applying MSI Afterburner profile", async () => await ProcessActions.RunCustom(async () => await Task.Run(() => Process.Start(new ProcessStartInfo { FileName = @"C:\Program Files (x86)\MSI Afterburner\MSIAfterburner.exe", Arguments = "/Profile1 /q" }))), () => MSI == true),
        };

        var filteredActions = actions.Where(a => a.Condition == null || a.Condition.Invoke()).ToList();
        int groupedTitleCount = 0;

        List<Func<Task>> currentGroup = new();

        for (int i = 0; i < filteredActions.Count; i++)
        {
            if (i == 0 || filteredActions[i].Title != filteredActions[i - 1].Title)
            {
                groupedTitleCount++;
            }
        }

        double incrementPerTitle = groupedTitleCount > 0 ? stagePercentage / (double)groupedTitleCount : 0;

        foreach (var (title, action, condition) in filteredActions)
        {
            if (previousTitle != string.Empty && previousTitle != title && currentGroup.Count > 0)
            {
                foreach (var groupedAction in currentGroup)
                {
                    try
                    {
                        await groupedAction();
                    }
                    catch (Exception ex)
                    {
                        InstallPage.Info.Title += ": " + ex.Message;
                        InstallPage.Info.Severity = InfoBarSeverity.Error;
                        InstallPage.Progress.Foreground = (Brush)Application.Current.Resources["SystemFillColorCriticalBrush"];
                        InstallPage.ProgressRingControl.Foreground = (Brush)Application.Current.Resources["SystemFillColorCriticalBrush"];
                        InstallPage.ProgressRingControl.Visibility = Visibility.Collapsed;
                        InstallPage.ResumeButton.Visibility = Visibility.Visible;

                        var tcs = new TaskCompletionSource<bool>();

                        InstallPage.ResumeButton.Click += (sender, e) =>
                        {
                            tcs.TrySetResult(true);
                            InstallPage.Info.Severity = InfoBarSeverity.Informational;
                            InstallPage.Progress.Foreground = (Brush)Application.Current.Resources["AccentForegroundBrush"];
                            InstallPage.ProgressRingControl.Foreground = null;
                            InstallPage.ProgressRingControl.Visibility = Visibility.Visible;
                            InstallPage.ResumeButton.Visibility = Visibility.Collapsed;
                        };

                        await tcs.Task;
                    }
                }

                InstallPage.Progress.Value += incrementPerTitle;
                await Task.Delay(150);
                currentGroup.Clear();
            }

            InstallPage.Info.Title = title + "...";
            currentGroup.Add(action);
            previousTitle = title;
        }

        if (currentGroup.Count > 0)
        {
            foreach (var groupedAction in currentGroup)
            {
                try
                {
                    await groupedAction();
                }
                catch (Exception ex)
                {
                    InstallPage.Info.Title += ": " + ex.Message;
                    InstallPage.Info.Severity = InfoBarSeverity.Error;
                    InstallPage.Progress.Foreground = (Brush)Application.Current.Resources["SystemFillColorCriticalBrush"];
                    InstallPage.ProgressRingControl.Foreground = (Brush)Application.Current.Resources["SystemFillColorCriticalBrush"];
                    InstallPage.ProgressRingControl.Visibility = Visibility.Collapsed;
                    InstallPage.ResumeButton.Visibility = Visibility.Visible;

                    var tcs = new TaskCompletionSource<bool>();

                    InstallPage.ResumeButton.Click += (sender, e) =>
                    {
                        tcs.TrySetResult(true);
                        InstallPage.Info.Severity = InfoBarSeverity.Informational;
                        InstallPage.Progress.Foreground = (Brush)Application.Current.Resources["AccentForegroundBrush"];
                        InstallPage.ProgressRingControl.Foreground = null;
                        InstallPage.ProgressRingControl.Visibility = Visibility.Visible;
                        InstallPage.ResumeButton.Visibility = Visibility.Collapsed;
                    };

                    await tcs.Task;
                }
            }

            InstallPage.Progress.Value += incrementPerTitle;
        }
    }
}