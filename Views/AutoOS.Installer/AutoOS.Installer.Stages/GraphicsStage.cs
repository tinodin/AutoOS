using AutoOS.Views.Installer.Actions;
using Microsoft.Win32;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace AutoOS.Views.Installer.Stages;

public static class GraphicsStage
{
    public static async Task Run()
    {
        bool? Intel10th = PreparingStage.Intel10th;
        bool? Intel11th = PreparingStage.Intel11th;
        bool? NVIDIA = PreparingStage.NVIDIA;
        bool? AMD = PreparingStage.AMD;
        bool? Fortnite = PreparingStage.Fortnite;
        bool? HDCP = PreparingStage.HDCP;
        bool? AlwaysShowTrayIcons = PreparingStage.AlwaysShowTrayIcons;
        bool? MSI = PreparingStage.MSI;
        bool? CRU = PreparingStage.CRU;

        InstallPage.Status.Text = "Configuring Graphics Card...";

        string previousTitle = string.Empty;
        int stagePercentage = 5;

        using var client = new HttpClient();
        string html = client.GetStringAsync("https://www.techspot.com/downloads/drivers/essentials/nvidia-geforce/").Result;
        string pattern = @"<title>.*?(\d+\.\d+).*?</title>";
        var match = Regex.Match(html, pattern);
        string version = match.Groups[1].Value;

        var actions = new List<(string Title, Func<Task> Action, Func<bool> Condition)>
        {
            // download the latest intel driver
            ("Downloading the latest Intel Driver", async () => await ProcessActions.RunDownload("https://downloadmirror.intel.com/764512/gfx_win_101.2115.zip", Path.GetTempPath(), "driver.zip"), () => Intel10th == true),

            // extract the driver
            ("Extracting the Intel driver", async () => await ProcessActions.RunExtract(Path.Combine(Path.GetTempPath(), "driver.zip"), Path.Combine(Path.GetTempPath(), "driver")), () => Intel10th == true),

            // install the driver
            ("Installing the Intel driver", async () => await ProcessActions.RunNsudo("CurrentUser", @"""%TEMP%\driver\Installer.exe"" /silent"),() => Intel10th == true),

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

            ("Configuring settings", async () => await ProcessActions.RunPowerShellScript("intelsettings.ps1", ""),() => Intel10th == true),

            // disable unnecessary services
            ("Disabling unnecessary services", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\igccservice"" /v ""Start"" /t REG_DWORD /d 4 /f"), () => Intel10th == true),
            ("Disabling unnecessary services", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\igfxCUIService2.0.0.0"" /v ""Start"" /t REG_DWORD /d 4 /f"), () => Intel10th == true),

            // disable high-definition-content-protection (hdcp)
            ("Disabling high-definition-content-protection (HDCP)", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\cphs"" /v ""Start"" /t REG_DWORD /d 4 /f"), () => Intel10th == true),
            ("Disabling high-definition-content-protection (HDCP)", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\cplspcon"" /v ""Start"" /t REG_DWORD /d 4 /f"), () => Intel10th == true),

            // download the latest nvidia driver
            ("Downloading the latest NVIDIA Driver", async () => await ProcessActions.RunDownload($@"https://us.download.nvidia.com/Windows/{version}/{version}-desktop-win10-win11-64bit-international-dch-whql.exe", Path.GetTempPath(), "driver.exe"), () => NVIDIA == true),

            // extract the driver
            ("Extracting the NVIDIA driver", async () => await ProcessActions.RunExtract(Path.Combine(Path.GetTempPath(), "driver.exe"), Path.Combine(Path.GetTempPath(), "driver")), () => NVIDIA == true),

            // strip the driver
            ("Stripping the NVIDIA driver", async () => await ProcessActions.RunNvidiaStrip(), () => NVIDIA == true),

            // install the nvidia driver
            ("Installing the NVIDIA driver", async () => await ProcessActions.RunNsudo("CurrentUser", @"""%TEMP%\driver\setup.exe"" /s"), () => NVIDIA == true),
            ("Installing the NVIDIA driver", async () => await ProcessActions.RunApplication("CRU", "restart64.exe", "/q"), () => NVIDIA == true),
            ("Installing the NVIDIA driver", async () => await ProcessActions.Sleep(2000), () => NVIDIA == true),

            // enable hardware accelerated gpu scheduling (hags)
            ("Enabling Hardware-accelerated GPU scheduling (HAGS)", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\GraphicsDrivers"" /v ""HwSchMode"" /t REG_DWORD /d 2 /f"), () => NVIDIA == true),

            // enable optimizations for windowed games
            ("Enabling optimizations for windowed games", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Microsoft\DirectX\UserGpuPreferences"" /v ""DirectXUserGlobalSettings"" /t REG_SZ /d ""SwapEffectUpgradeEnable=1;"" /f"), () => NVIDIA == true),

            // disable telemetry
            ("Disabling telemetry", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\Software\Nvidia Corporation\NvControlPanel2\Client"" /v ""OptInOrOutPreference"" /t REG_DWORD /d 0 /f"), () => NVIDIA == true),
            ("Disabling telemetry", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\nvlddmkm\Global\Startup"" /v ""SendTelemetryData"" /t REG_DWORD /d 0 /f"), () => NVIDIA == true),

            // use the advanced 3d image settings
            ("Using the advanced 3D image settings", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\NVIDIA Corporation\Global\NVTweak"" /v ""Gestalt"" /t REG_DWORD /d 515 /f"), () => NVIDIA == true),

            // import profiles
            ("Importing the optimized profile", async () => await ProcessActions.ImportProfile("BaseProfile.nip"), () => NVIDIA == true),
            ("Fortnite.nip", async () => await ProcessActions.ImportProfile("Fortnite.nip"), () => NVIDIA == true && Fortnite == true),

            // use gpu for physx
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

            // show tray icon
            (@"Set-ItemProperty -Path 'HKCU:\Control Panel\NotifyIconSettings\*' -Name 'IsPromoted' -Value 1", async () => await ProcessActions.RunPowerShell(@"Set-ItemProperty -Path 'HKCU:\Control Panel\NotifyIconSettings\*' -Name 'IsPromoted' -Value 1"), () => NVIDIA == true && AlwaysShowTrayIcons == true),

            // disable audio enhancements
            ("Disabling audio enhancements", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"powershell -Command ""$Keys = @('HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\MMDevices\Audio\Render'); foreach ($Key in $Keys) { Get-ChildItem $Key -Recurse | Where-Object { $_.PSPath -match '\\FxProperties$' } | ForEach-Object { Set-ItemProperty -Path $_.PSPath -Name '{1da5d803-d492-4edd-8c23-e0c0ffee7f0e},5' -Value 1 } }"""), null),
            ("Disabling audio enhancements", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"powershell -Command ""$Keys = @('HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\MMDevices\Audio\Capture'); foreach ($Key in $Keys) { Get-ChildItem $Key -Recurse | Where-Object { $_.PSPath -match '\\FxProperties$' } | ForEach-Object { Set-ItemProperty -Path $_.PSPath -Name '{1da5d803-d492-4edd-8c23-e0c0ffee7f0e},5' -Value 1 } }"""), null),

            // disable exclusive control
            ("Disabling exclusive control", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"cmd /c for %k in (Capture Render) do for /f ""delims="" %a in ('reg query ""HKLM\Software\Microsoft\Windows\CurrentVersion\MMDevices\Audio\%k""') do reg add ""%a\Properties"" /v ""{b3f8fa53-0004-438e-9003-51a46e139bfc},3"" /t REG_DWORD /d 0 /f && reg add ""%a\Properties"" /v ""{b3f8fa53-0004-438e-9003-51a46e139bfc},4"" /t REG_DWORD /d 0 /f"), null),

            // apply cru profile
            ("Importing Custom Resolution Utility (CRU) profile", async () => await ProcessActions.Sleep(1000), () => CRU == true),
            ("Importing Custom Resolution Utility (CRU) profile", async () => await ProcessActions.RunCustom(async () => await Task.Run(() => Process.Start(new ProcessStartInfo { FileName = Registry.GetValue(@"HKEY_CURRENT_USER\SOFTWARE\AutoOS", "CruProfile", null).ToString(), Arguments = "-i" })!.WaitForExitAsync())), () => CRU == true),
            ("Applying Custom Resolution Utility (CRU) profile", async () => await ProcessActions.RunApplication("CRU", "restart64.exe", "/q"), () => CRU == true),
            ("Applying Custom Resolution Utility (CRU) profile", async () => await ProcessActions.Sleep(2000), () => CRU == true),

            // configure color settings (again)
            ("Configuring color settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"cmd /c for /f ""delims="" %a in ('reg query HKLM\System\CurrentControlSet\Services\nvlddmkm\State\DisplayDatabase') do reg add ""%a"" /v ""ColorformatConfig"" /t REG_BINARY /d ""DB02000014000000000A00080000000003010000"" /f"), () => CRU == true && NVIDIA == true),

            // download msi afterburner
            ("Downloading MSI Afterburner", async () => await ProcessActions.RunDownload("https://www.dl.dropboxusercontent.com/scl/fi/6dvl62kgm3z38x49752bt/MSI-Afterburner.zip?rlkey=h2m2riyjisrb3ph0i8j0q4eu5&st=pw7u3mte&dl=0", Path.GetTempPath(), "MSI Afterburner.zip"), null),

            // install msi afterburner
            ("Installing MSI Afterburner", async () => await ProcessActions.RunExtract(Path.Combine(Path.GetTempPath(), "MSI Afterburner.zip"), @"C:\Program Files (x86)\MSI Afterburner"), null),
            ("Installing MSI Afterburner", async () => await ProcessActions.RunNsudo("CurrentUser", @"""C:\Program Files (x86)\MSI Afterburner\Redist\vcredist_x86.exe"" /q"), null),
            ("Installing MSI Afterburner", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Afterburner"" /v ""DisplayIcon"" /t REG_SZ /d ""C:\Program Files (x86)\MSI Afterburner\uninstall.exe"" /f"), null),
            ("Installing MSI Afterburner", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Afterburner"" /v ""DisplayName"" /t REG_SZ /d ""MSI Afterburner 4.6.5"" /f"), null),
            ("Installing MSI Afterburner", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Afterburner"" /v ""DisplayVersion"" /t REG_SZ /d ""4.6.5"" /f"), null),
            ("Installing MSI Afterburner", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Afterburner"" /v ""Publisher"" /t REG_SZ /d ""MSI Co., LTD"" /f"), null),
            ("Installing MSI Afterburner", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Afterburner"" /v ""UninstallString"" /t REG_SZ /d ""C:\Program Files (x86)\MSI Afterburner\uninstall.exe"" /f"), null),
            ("Installing MSI Afterburner", async () => await ProcessActions.RunNsudo("CurrentUser", @"cmd /c mkdir ""%APPDATA%\Microsoft\Windows\Start Menu\Programs\MSI Afterburner"" ""%APPDATA%\Microsoft\Windows\Start Menu\Programs\MSI Afterburner\SDK"""), null),
            ("Installing MSI Afterburner", async () => await ProcessActions.RunPowerShell(@"$Shell=New-Object -ComObject WScript.Shell; @(@{P='MSI Afterburner.lnk';T='C:\Program Files (x86)\MSI Afterburner\MSIAfterburner.exe'},@{P='ReadMe.lnk';T='C:\Program Files (x86)\MSI Afterburner\Doc\ReadMe.pdf'},@{P='Uninstall.lnk';T='C:\Program Files (x86)\MSI Afterburner\Uninstall.exe'},@{P='SDK\MSI Afterburner localization reference.lnk';T='C:\Program Files (x86)\MSI Afterburner\SDK\Doc\Localization reference.pdf'},@{P='SDK\MSI Afterburner skin format reference.lnk';T='C:\Program Files (x86)\MSI Afterburner\SDK\Doc\USF skin format reference.pdf'},@{P='SDK\Samples.lnk';T='C:\Program Files (x86)\MSI Afterburner\SDK\Samples\'}) | % {$Shortcut=$Shell.CreateShortcut([System.IO.Path]::Combine($env:APPDATA, 'Microsoft\Windows\Start Menu\Programs\MSI Afterburner', $_.P)); $Shortcut.TargetPath=$_.T; $Shortcut.Save()}"), null),

            // import msi afterburner profile
            ("Importing MSI Afterburner profile", async () => await ProcessActions.RunCustom(async() => await Task.Run(() => File.Copy(Registry.GetValue(@"HKEY_CURRENT_USER\SOFTWARE\AutoOS", "MsiProfile", null).ToString(), Path.Combine(@"C:\Program Files (x86)\MSI Afterburner\Profiles\", Path.GetFileName(Registry.GetValue(@"HKEY_CURRENT_USER\SOFTWARE\AutoOS", "MsiProfile", null).ToString()))))), () => MSI == true),
            
            // apply msi afterburner profile
            ("Applying MSI Afterburner profile", async () => await ProcessActions.RunCustom(async() => await Task.Run(() => Process.Start(new ProcessStartInfo { FileName = @"C:\Program Files (x86)\MSI Afterburner\MSIAfterburner.exe", Arguments = "/Profile1 /q" }))), () => MSI == true),
        };

        var filteredActions = actions.Where(a => a.Condition == null || a.Condition.Invoke()).ToList();
        var uniqueTitles = filteredActions.Select(a => a.Title).Distinct().ToList();
        double incrementPerTitle = uniqueTitles.Count > 0 ? stagePercentage / (double)uniqueTitles.Count : 0;

        foreach (var title in uniqueTitles)
        {
            if (previousTitle != string.Empty && previousTitle != title)
            {
                await Task.Delay(150);
            }

            var actionsForTitle = filteredActions.Where(a => a.Title == title).ToList();
            int actionsForTitleCount = actionsForTitle.Count;

            foreach (var (actionTitle, action, condition) in actionsForTitle)
            {
                InstallPage.Info.Title = actionTitle;

                try
                {
                    await action();
                }
                catch (Exception ex)
                {
                    InstallPage.Info.Title = ex.Message;
                    InstallPage.Progress.ShowError = true;
                    InstallPage.Info.Severity = InfoBarSeverity.Error;
                    InstallPage.ProgressRingControl.Foreground = ProcessActions.GetColor("LightError", "DarkError");
                    return;
                }
            }

            InstallPage.Progress.Value += incrementPerTitle;

            previousTitle = title;
        }
    }
}
