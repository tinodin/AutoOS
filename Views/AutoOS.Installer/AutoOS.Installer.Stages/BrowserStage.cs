using AutoOS.Views.Installer.Actions;
using Microsoft.UI.Xaml.Media;
using Windows.UI;
using System.Diagnostics;
using System.Text.Json;
using Microsoft.UI.Xaml.Documents;

namespace AutoOS.Views.Installer.Stages;

public static class BrowserStage
{
    public static async Task Run()
    {
        bool? Chrome = PreparingStage.Chrome;
        bool? Brave = PreparingStage.Brave;
        bool? Firefox = PreparingStage.Firefox;
        bool? Arc = PreparingStage.Arc;
        bool? uBlock = PreparingStage.uBlock;
        bool? SponsorBlock = PreparingStage.SponsorBlock;
        bool? Cookies = PreparingStage.Cookies;
        bool? DarkReader = PreparingStage.DarkReader;
        bool? Shazam = PreparingStage.Shazam;
        bool? iCloud = PreparingStage.iCloud;
        bool? Bitwarden = PreparingStage.Bitwarden;
        bool? OnePassword = PreparingStage.OnePassword;

        InstallPage.Status.Text = "Configuring Browser...";

        int validActionsCount = 0;
        int stagePercentage = 5;

        string chromeVersion = "";
        string chromeVersion2 = "";

        string braveVersion = "";
        string arcVersion = "";

        using HttpClient client = new HttpClient();
        string firefoxVersion = JsonDocument.Parse(await client.GetStringAsync("https://product-details.mozilla.org/1.0/firefox_versions.json")).RootElement.GetProperty("LATEST_FIREFOX_VERSION").GetString();

        var actions = new List<(Func<Task> Action, Func<bool> Condition)>
        {
            // download google chrome
            (async () => await ProcessActions.RunDownload("Downloading Google Chrome", "https://www.dl.dropboxusercontent.com/scl/fi/y67wfy3bqlj107deqb1xe/ChromeSetup.exe?rlkey=7deesr6vavgkaatmg2tmrdodk&st=ljimeprm&dl=0", Path.GetTempPath(), "ChromeSetup.exe"), () => Chrome == true),

            // install google chrome
            (async () => await ProcessActions.RunNsudo("Installing Google Chrome", "CurrentUser", @"""%TEMP%\ChromeSetup.exe"" /silent /install"), () => Chrome == true),
            (async () => await ProcessActions.RunCustom("Installing Google Chrome", async () => { chromeVersion = FileVersionInfo.GetVersionInfo(Environment.ExpandEnvironmentVariables(@"%TEMP%\ChromeSetup.exe")).ProductVersion; }), () => Chrome == true),
            (async () => await ProcessActions.RunCustom("Installing Google Chrome", async () => { chromeVersion2 = FileVersionInfo.GetVersionInfo(@"C:\Program Files\Google\Chrome\Application\chrome.exe").ProductVersion; }), () => Chrome == true),

            // disable google chrome services
            (async () => await ProcessActions.RunNsudo("Disabling Google Chrome services", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\GoogleChromeElevationService"" /v ""Start"" /t REG_DWORD /d 4 /f"), () => Chrome == true),
            (async () => await ProcessActions.RunNsudo("Disabling Google Chrome services", "TrustedInstaller", $@"reg add ""HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\GoogleUpdaterInternalService{chromeVersion}"" /v ""Start"" /t REG_DWORD /d 4 /f"), () => Chrome == true),
            (async () => await ProcessActions.RunNsudo("Disabling Google Chrome services", "TrustedInstaller", $@"reg add ""HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\GoogleUpdaterService{chromeVersion}"" /v ""Start"" /t REG_DWORD /d 4 /f"), () => Chrome == true),
            (async () => await ProcessActions.RunNsudo("Disabling Google Chrome services", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{8A69D345-D564-463c-AFF1-A69D9E530F96}"" /v """" /t REG_SZ /d ""Google Chrome"" /f"), () => Chrome == true),
            (async () => await ProcessActions.RunNsudo("Disabling Google Chrome services", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{8A69D345-D564-463c-AFF1-A69D9E530F96}"" /v ""Localized Name"" /t REG_SZ /d ""Google Chrome"" /f"), () => Chrome == true),
            (async () => await ProcessActions.RunNsudo("Disabling Google Chrome services", "TrustedInstaller", $@"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{{8A69D345-D564-463c-AFF1-A69D9E530F96}}"" /v ""StubPath"" /t REG_SZ /d ""\""C:\\Program Files\\Google\\Chrome\\Application\\{chromeVersion2}\\Installer\\chrmstp.exe\"" --configure-user-settings --verbose-logging --system-level --channel=stable"" /f"), () => Chrome == true),
            (async () => await ProcessActions.RunNsudo("Disabling Google Chrome services", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{8A69D345-D564-463c-AFF1-A69D9E530F96}"" /v ""Version"" /t REG_SZ /d ""43,0,0,0"" /f"), () => Chrome == true),
            (async () => await ProcessActions.RunNsudo("Disabling Google Chrome services", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{8A69D345-D564-463c-AFF1-A69D9E530F96}"" /v ""IsInstalled"" /t REG_DWORD /d 1 /f"), () => Chrome == true),
            (async () => await ProcessActions.RunNsudo("Disabling Google Chrome services", "TrustedInstaller", @"reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\{8A69D345-D564-463c-AFF1-A69D9E530F96}"" /f"), () => Chrome == true),
            (async () => await ProcessActions.RunNsudo("Disabling Google Chrome services", "TrustedInstaller", @"cmd /c for /f ""tokens=2,* delims= "" %A in ('reg query ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Schedule\TaskCache\Tasks"" /s /v URI ^| findstr /i ""\\GoogleSystem\\GoogleUpdater\\""') do schtasks /Change /TN ""%B"" /Disable"), () => Chrome == true),

            // install ublock origin extension
            (async () => await ProcessActions.RunPowerShell("Installing uBlock Origin Extension", @"$BaseKey = 'HKLM:\SOFTWARE\Policies\Google\Chrome\ExtensionInstallForcelist'; $Index = (Get-Item $BaseKey).Property | Sort-Object {[int]$_} | Select-Object -Last 1; $NewIndex = [int]$Index + 1; reg add 'HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Google\Chrome\ExtensionInstallForcelist' /v $NewIndex /t REG_SZ /d 'cjpalhdlnbpafiamejdnhcphjbkeiagm' /f"), () => Chrome == true && uBlock == true),

            // install sponsorblock extension
            (async () => await ProcessActions.RunPowerShell("Installing SponsorBlock Extension", @"$BaseKey = 'HKLM:\SOFTWARE\Policies\Google\Chrome\ExtensionInstallForcelist'; $Index = (Get-Item $BaseKey).Property | Sort-Object {[int]$_} | Select-Object -Last 1; $NewIndex = [int]$Index + 1; reg add 'HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Google\Chrome\ExtensionInstallForcelist' /v $NewIndex /t REG_SZ /d 'mnjggcdmjocbbbhaepdhchncahnbgone' /f"), () => Chrome == true && SponsorBlock == true),

            // install i still dont care about cookies extension
            (async () => await ProcessActions.RunPowerShell("Installing I still don't care about cookies Extension", @"$BaseKey = 'HKLM:\SOFTWARE\Policies\Google\Chrome\ExtensionInstallForcelist'; $Index = (Get-Item $BaseKey).Property | Sort-Object {[int]$_} | Select-Object -Last 1; $NewIndex = [int]$Index + 1; reg add 'HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Google\Chrome\ExtensionInstallForcelist' /v $NewIndex /t REG_SZ /d 'edibdbjcniadpccecjdfdjjppcpchdlm' /f"), () => Chrome == true && Cookies == true),

            // install dark reader extension
            (async () => await ProcessActions.RunPowerShell("Installing Dark Reader Extension", @"$BaseKey = 'HKLM:\SOFTWARE\Policies\Google\Chrome\ExtensionInstallForcelist'; $Index = (Get-Item $BaseKey).Property | Sort-Object {[int]$_} | Select-Object -Last 1; $NewIndex = [int]$Index + 1; reg add 'HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Google\Chrome\ExtensionInstallForcelist' /v $NewIndex /t REG_SZ /d 'eimadpbcbfnmbkopoojfekhnkhdbieeh' /f"), () => Chrome == true && DarkReader == true),

            // install shazam extension
            (async () => await ProcessActions.RunPowerShell("Installing Shazam Extension", @"$BaseKey = 'HKLM:\SOFTWARE\Policies\Google\Chrome\ExtensionInstallForcelist'; $Index = (Get-Item $BaseKey).Property | Sort-Object {[int]$_} | Select-Object -Last 1; $NewIndex = [int]$Index + 1; reg add 'HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Google\Chrome\ExtensionInstallForcelist' /v $NewIndex /t REG_SZ /d 'mmioliijnhnoblpgimnlajmefafdfilb' /f"), () => Chrome == true && Shazam == true),

            // install icloud passwords extension
            (async () => await ProcessActions.RunPowerShell("Installing iCloud Passwords Extension", @"$BaseKey = 'HKLM:\SOFTWARE\Policies\Google\Chrome\ExtensionInstallForcelist'; $Index = (Get-Item $BaseKey).Property | Sort-Object {[int]$_} | Select-Object -Last 1; $NewIndex = [int]$Index + 1; reg add 'HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Google\Chrome\ExtensionInstallForcelist' /v $NewIndex /t REG_SZ /d 'pejdijmoenmkgeppbflobdenhhabjlaj' /f"), () => Chrome == true && iCloud == true),

            // install bitwarden extension
            (async () => await ProcessActions.RunPowerShell("Installing Bitwarden Extension", @"$BaseKey = 'HKLM:\SOFTWARE\Policies\Google\Chrome\ExtensionInstallForcelist'; $Index = (Get-Item $BaseKey).Property | Sort-Object {[int]$_} | Select-Object -Last 1; $NewIndex = [int]$Index + 1; reg add 'HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Google\Chrome\ExtensionInstallForcelist' /v $NewIndex /t REG_SZ /d 'nngceckbapebfimnlniiiahkandclblb' /f"), () => Chrome == true && Bitwarden == true),

            // install 1password extension
            (async () => await ProcessActions.RunPowerShell("Installing 1Password Extension", @"$BaseKey = 'HKLM:\SOFTWARE\Policies\Google\Chrome\ExtensionInstallForcelist'; $Index = (Get-Item $BaseKey).Property | Sort-Object {[int]$_} | Select-Object -Last 1; $NewIndex = [int]$Index + 1; reg add 'HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Google\Chrome\ExtensionInstallForcelist' /v $NewIndex /t REG_SZ /d 'aeblfdkhhhdcdjpifhhbdiojplfjncoa' /f"), () => Chrome == true && OnePassword == true),

            // log in to google chrome
            (async () => await ProcessActions.RunCustom("Please log in to your Google Chrome account", async () => await Task.Run(() => Process.Start(new ProcessStartInfo { FileName = Path.Combine(@"C:\Program Files\Google\Chrome\Application", "chrome.exe"), WindowStyle = ProcessWindowStyle.Maximized })!.WaitForExitAsync())), () => Chrome == true),

            // remove desktop shortcut
            (async () => await ProcessActions.RunNsudo("Removing desktop shortcut", "CurrentUser", @"cmd /c del /f /q ""C:\Users\Public\Desktop\Google Chrome.lnk"""), () => Chrome == true),

            // download brave
            (async () => await ProcessActions.RunDownload("Downloading Brave", "https://github.com/brave/brave-browser/releases/latest/download/BraveBrowserStandaloneSetup.exe", Path.GetTempPath(), "BraveBrowserStandaloneSetup.exe"), () => Brave == true),

            // install brave
            (async () => await ProcessActions.RunNsudo("Installing Brave", "CurrentUser", @"""%TEMP%\BraveBrowserStandaloneSetup.exe"" /silent /install"), () => Brave == true),
            (async () => await ProcessActions.RunCustom("Installing Brave", async () => { braveVersion = FileVersionInfo.GetVersionInfo(@"C:\Program Files\BraveSoftware\Brave-Browser\Application\brave.exe").ProductVersion; }), () => Brave == true),
            
            // remove brave shortcut from the desktop
            (async () => await ProcessActions.RunNsudo("Removing Brave shortcut from the desktop", "CurrentUser", @"cmd /c del /f /q ""C:\Users\Public\Desktop\Brave.lnk"""), () => Brave == true),

            // optimize brave settings
            (async () => await ProcessActions.RunCustom("Optimizing Brave settings", async () => await Task.Run(() => File.Copy(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Scripts", "initial_preferences"), @"C:\Program Files\BraveSoftware\Brave-Browser\Application\initial_preferences", true))), () => Brave == true),
            (async () => await ProcessActions.RunNsudo("Optimizing Brave settings", "CurrentUser", @"cmd /c mkdir ""%LOCALAPPDATA%\BraveSoftware\Brave-Browser\User Data"""), () => Brave == true),
            (async () => await ProcessActions.RunCustom("Optimizing Brave settings", async () => await Task.Run(() => File.Copy(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Scripts", "Local State"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BraveSoftware", "Brave-Browser", "User Data", "Local State"), true))), () => Brave == true),

            // disable brave services
            (async () => await ProcessActions.RunNsudo("Disabling Brave services", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\brave"" /v ""Start"" /t REG_DWORD /d 4 /f"), () => Brave == true),
            (async () => await ProcessActions.RunNsudo("Disabling Brave services", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\BraveElevationService"" /v ""Start"" /t REG_DWORD /d 4 /f"), () => Brave == true),
            (async () => await ProcessActions.RunNsudo("Disabling Brave services", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\bravem"" /v ""Start"" /t REG_DWORD /d 4 /f"), () => Brave == true),
            (async () => await ProcessActions.RunNsudo("Disabling Brave services", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{AFE6A462-C574-4B8A-AF43-4CC60DF4563B}"" /v """" /t REG_SZ /d ""Brave"" /f"), () => Brave == true),
            (async () => await ProcessActions.RunNsudo("Disabling Brave services", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{AFE6A462-C574-4B8A-AF43-4CC60DF4563B}"" /v ""Localized Name"" /t REG_SZ /d ""Brave"" /f"), () => Brave == true),
            (async () => await ProcessActions.RunNsudo("Disabling Brave services", "TrustedInstaller", $@"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{{AFE6A462-C574-4B8A-AF43-4CC60DF4563B}}"" /v ""StubPath"" /t REG_SZ /d ""\""C:\\Program Files\\BraveSoftware\\Brave-Browser\\Application\\{braveVersion}\\Installer\\chrmstp.exe\"" --configure-user-settings --verbose-logging --system-level"" /f"), () => Brave == true),
            (async () => await ProcessActions.RunNsudo("Disabling Brave services", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{AFE6A462-C574-4B8A-AF43-4CC60DF4563B}"" /v ""Version"" /t REG_SZ /d ""43,0,0,0"" /f"), () => Brave == true),
            (async () => await ProcessActions.RunNsudo("Disabling Brave services", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{AFE6A462-C574-4B8A-AF43-4CC60DF4563B}"" /v ""IsInstalled"" /t REG_DWORD /d 1 /f"), () => Brave == true),
            (async () => await ProcessActions.RunNsudo("Disabling Brave services", "TrustedInstaller", @"reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\{AFE6A462-C574-4B8A-AF43-4CC60DF4563B}"" /f"), () => Brave == true),
            (async () => await ProcessActions.RunNsudo("Disabling Brave services", "TrustedInstaller", @"cmd /c for /f ""tokens=1 delims=,"" %A in ('schtasks /query /fo csv ^| findstr BraveSoftwareUpdateTaskMachine') do schtasks /change /tn %A /disable"), () => Brave == true),

            // install ublock origin extension
            (async () => await ProcessActions.RunPowerShell("Installing uBlock Origin Extension", @"$BaseKey = 'HKLM:\SOFTWARE\Policies\BraveSoftware\Brave\ExtensionInstallForcelist'; $Index = (Get-Item $BaseKey).Property | Sort-Object {[int]$_} | Select-Object -Last 1; $NewIndex = [int]$Index + 1; reg add 'HKEY_LOCAL_MACHINE\SOFTWARE\Policies\BraveSoftware\Brave\ExtensionInstallForcelist' /v $NewIndex /t REG_SZ /d 'cjpalhdlnbpafiamejdnhcphjbkeiagm' /f"), () => Brave == true && uBlock == true),

            // install sponsorblock extension
            (async () => await ProcessActions.RunPowerShell("Installing SponsorBlock Extension", @"$BaseKey = 'HKLM:\SOFTWARE\Policies\BraveSoftware\Brave\ExtensionInstallForcelist'; $Index = (Get-Item $BaseKey).Property | Sort-Object {[int]$_} | Select-Object -Last 1; $NewIndex = [int]$Index + 1; reg add 'HKEY_LOCAL_MACHINE\SOFTWARE\Policies\BraveSoftware\Brave\ExtensionInstallForcelist' /v $NewIndex /t REG_SZ /d 'mnjggcdmjocbbbhaepdhchncahnbgone' /f"), () => Brave == true && SponsorBlock == true),

            // install i still dont care about cookies extension
            (async () => await ProcessActions.RunPowerShell("Installing I still don't care about cookies Extension", @"$BaseKey = 'HKLM:\SOFTWARE\Policies\BraveSoftware\Brave\ExtensionInstallForcelist'; $Index = (Get-Item $BaseKey).Property | Sort-Object {[int]$_} | Select-Object -Last 1; $NewIndex = [int]$Index + 1; reg add 'HKEY_LOCAL_MACHINE\SOFTWARE\Policies\BraveSoftware\Brave\ExtensionInstallForcelist' /v $NewIndex /t REG_SZ /d 'edibdbjcniadpccecjdfdjjppcpchdlm' /f"), () => Brave == true && Cookies == true),

            // install dark reader extension
            (async () => await ProcessActions.RunPowerShell("Installing Dark Reader Extension", @"$BaseKey = 'HKLM:\SOFTWARE\Policies\BraveSoftware\Brave\ExtensionInstallForcelist'; $Index = (Get-Item $BaseKey).Property | Sort-Object {[int]$_} | Select-Object -Last 1; $NewIndex = [int]$Index + 1; reg add 'HKEY_LOCAL_MACHINE\SOFTWARE\Policies\BraveSoftware\Brave\ExtensionInstallForcelist' /v $NewIndex /t REG_SZ /d 'eimadpbcbfnmbkopoojfekhnkhdbieeh' /f"), () => Brave == true && DarkReader == true),

            // install shazam extension
            (async () => await ProcessActions.RunPowerShell("Installing Shazam Extension", @"$BaseKey = 'HKLM:\SOFTWARE\Policies\BraveSoftware\Brave\ExtensionInstallForcelist'; $Index = (Get-Item $BaseKey).Property | Sort-Object {[int]$_} | Select-Object -Last 1; $NewIndex = [int]$Index + 1; reg add 'HKEY_LOCAL_MACHINE\SOFTWARE\Policies\BraveSoftware\Brave\ExtensionInstallForcelist' /v $NewIndex /t REG_SZ /d 'mmioliijnhnoblpgimnlajmefafdfilb' /f"), () => Brave == true && Shazam == true),

            // install icloud passwords extension
            (async () => await ProcessActions.RunPowerShell("Installing iCloud Passwords Extension", @"$BaseKey = 'HKLM:\SOFTWARE\Policies\BraveSoftware\Brave\ExtensionInstallForcelist'; $Index = (Get-Item $BaseKey).Property | Sort-Object {[int]$_} | Select-Object -Last 1; $NewIndex = [int]$Index + 1; reg add 'HKEY_LOCAL_MACHINE\SOFTWARE\Policies\BraveSoftware\Brave\ExtensionInstallForcelist' /v $NewIndex /t REG_SZ /d 'pejdijmoenmkgeppbflobdenhhabjlaj' /f"), () => Brave == true && iCloud == true),

            // install bitwarden extension
            (async () => await ProcessActions.RunPowerShell("Installing Bitwarden Extension", @"$BaseKey = 'HKLM:\SOFTWARE\Policies\BraveSoftware\Brave\ExtensionInstallForcelist'; $Index = (Get-Item $BaseKey).Property | Sort-Object {[int]$_} | Select-Object -Last 1; $NewIndex = [int]$Index + 1; reg add 'HKEY_LOCAL_MACHINE\SOFTWARE\Policies\BraveSoftware\Brave\ExtensionInstallForcelist' /v $NewIndex /t REG_SZ /d 'nngceckbapebfimnlniiiahkandclblb' /f"), () => Brave == true && Bitwarden == true),

            // install 1password extension
            (async () => await ProcessActions.RunPowerShell("Installing 1Password Extension", @"$BaseKey = 'HKLM:\SOFTWARE\Policies\BraveSoftware\Brave\ExtensionInstallForcelist'; $Index = (Get-Item $BaseKey).Property | Sort-Object {[int]$_} | Select-Object -Last 1; $NewIndex = [int]$Index + 1; reg add 'HKEY_LOCAL_MACHINE\SOFTWARE\Policies\BraveSoftware\Brave\ExtensionInstallForcelist' /v $NewIndex /t REG_SZ /d 'aeblfdkhhhdcdjpifhhbdiojplfjncoa' /f"), () => Brave == true && OnePassword == true),

            // download firefox
            (async () => await ProcessActions.RunDownload("Downloading Firefox", $"https://releases.mozilla.org/pub/firefox/releases/{firefoxVersion}/win64/en-US/Firefox%20Setup%20{firefoxVersion}.exe", Path.GetTempPath(), "FirefoxSetup.exe"), () => Firefox == true),

            // install firefox
            (async () => await ProcessActions.RunNsudo("Installing Firefox", "CurrentUser", @"""%TEMP%\FirefoxSetup.exe"" /S /MaintenanceService=false /DesktopShortcut=false /StartMenuShortcut=true"), () => Firefox == true),

            // debloat firefox
            (async () => await ProcessActions.RunNsudo("Debloating Firefox", "CurrentUser", @"cmd /c del /f /q ""C:\Program Files\Mozilla Firefox\crashreporter.exe"""), () => Firefox == true),
            (async () => await ProcessActions.RunNsudo("Debloating Firefox", "CurrentUser", @"cmd /c del /f /q ""C:\Program Files\Mozilla Firefox\crashreporter.ini"""), () => Firefox == true),
            (async () => await ProcessActions.RunNsudo("Debloating Firefox", "CurrentUser", @"cmd /c del /f /q ""C:\Program Files\Mozilla Firefox\defaultagent.ini"""), () => Firefox == true),
            (async () => await ProcessActions.RunNsudo("Debloating Firefox", "CurrentUser", @"cmd /c del /f /q ""C:\Program Files\Mozilla Firefox\defaultagent_localized.ini"""), () => Firefox == true),
            (async () => await ProcessActions.RunNsudo("Debloating Firefox", "CurrentUser", @"cmd /c del /f /q ""C:\Program Files\Mozilla Firefox\default-browser-agent.exe"""), () => Firefox == true),
            (async () => await ProcessActions.RunNsudo("Debloating Firefox", "CurrentUser", @"cmd /c del /f /q ""C:\Program Files\Mozilla Firefox\maintenanceservice.exe"""), () => Firefox == true),
            (async () => await ProcessActions.RunNsudo("Debloating Firefox", "CurrentUser", @"cmd /c del /f /q ""C:\Program Files\Mozilla Firefox\maintenanceservice_installer.exe"""), () => Firefox == true),
            (async () => await ProcessActions.RunNsudo("Debloating Firefox", "CurrentUser", @"cmd /c del /f /q ""C:\Program Files\Mozilla Firefox\pingsender.exe"""), () => Firefox == true),
            (async () => await ProcessActions.RunNsudo("Debloating Firefox", "CurrentUser", @"cmd /c del /f /q ""C:\Program Files\Mozilla Firefox\updater.exe"""), () => Firefox == true),
            (async () => await ProcessActions.RunNsudo("Debloating Firefox", "CurrentUser", @"cmd /c del /f /q ""C:\Program Files\Mozilla Firefox\updater.ini"""), () => Firefox == true),
            (async () => await ProcessActions.RunNsudo("Debloating Firefox", "CurrentUser", @"cmd /c del /f /q ""C:\Program Files\Mozilla Firefox\update-settings.ini"""), () => Firefox == true),
            (async () => await ProcessActions.RunNsudo("Debloating Firefox", "TrustedInstaller", @"SCHTASKS /Change /TN ""\Mozilla\Firefox Default Browser Agent 308046B0AF4A39CB"" /Disable"), () => Firefox == true),

            // optimize firefox settings
            (async () => await ProcessActions.RunNsudo("Optimizing Firefox settings", "CurrentUser", @"cmd /c mkdir ""C:\Program Files\Mozilla Firefox\distribution"""), () => Firefox == true),
            (async () => await ProcessActions.RunCustom("Optimizing Firefox settings", async () => await Task.Run(() => { string installDir = @"C:\Program Files\Mozilla Firefox"; string autoconfigPath = Path.Combine(installDir, "defaults", "pref", "autoconfig.js"); string autoconfigContent = "pref(\"general.config.filename\", \"firefox.cfg\");\npref(\"general.config.obscure_value\", 0);"; File.WriteAllText(autoconfigPath, autoconfigContent); })), () => Firefox == true),
            (async () => await ProcessActions.RunCustom("Optimizing Firefox settings", async () => await Task.Run(() => { string installDir = @"C:\Program Files\Mozilla Firefox"; string firefoxConfigPath = Path.Combine(installDir, "firefox.cfg"); string firefoxConfigContent = "\ndefaultPref(\"app.shield.optoutstudies.enabled\", false);\ndefaultPref(\"datareporting.healthreport.uploadEnabled\", false);\ndefaultPref(\"browser.newtabpage.activity-stream.feeds.section.topstories\", false);\ndefaultPref(\"browser.newtabpage.activity-stream.feeds.topsites\", false);\ndefaultPref(\"dom.security.https_only_mode\", true);\ndefaultPref(\"browser.uidensity\", 1);\ndefaultPref(\"full-screen-api.transition-duration.enter\", \"0 0\");\ndefaultPref(\"full-screen-api.transition-duration.leave\", \"0 0\");\ndefaultPref(\"full-screen-api.warning.timeout\", 0);\ndefaultPref(\"nglayout.enable_drag_images\", false);\ndefaultPref(\"reader.parse-on-load.enabled\", false);\ndefaultPref(\"browser.tabs.firefox-view\", false);\ndefaultPref(\"browser.tabs.tabmanager.enabled\", false);\nlockPref(\"browser.newtabpage.activity-stream.asrouter.userprefs.cfr.addons\", false);\nlockPref(\"browser.newtabpage.activity-stream.asrouter.userprefs.cfr.features\", false);"; File.WriteAllText(firefoxConfigPath, firefoxConfigContent); })), () => Firefox == true),
            (async () => await ProcessActions.RunCustom("Optimizing Firefox settings", async () => await Task.Run(() => { string installDir = @"C:\Program Files\Mozilla Firefox"; string policiesPath = Path.Combine(installDir, "distribution", "policies.json"); var policiesContent = new { policies = new { DisableAppUpdate = true, OverrideFirstRunPage = "" } }; File.WriteAllText(policiesPath, JsonSerializer.Serialize(policiesContent, new JsonSerializerOptions { WriteIndented = true })); })), () => Firefox == true),

            // download arkenfox user.js
            (async () => await ProcessActions.RunDownload("Downloading Arkenfox user.js", "https://github.com/arkenfox/user.js/raw/refs/heads/master/user.js", @"C:\Program Files\Mozilla Firefox", "user.js"), () => Firefox == true),

            // install ublock origin extension
            (async () => await ProcessActions.RunCustom("Installing uBlock Origin Extension", async () => await Task.Run(() => { string policiesPath = @"C:\Program Files\Mozilla Firefox\distribution\policies.json"; string extensionUrl = "https://addons.mozilla.org/firefox/downloads/latest/ublock-origin"; var policiesContent = JsonSerializer.Deserialize<Dictionary<string, object>>(File.ReadAllText(policiesPath)); if (policiesContent.ContainsKey("policies")) { var policies = (JsonElement)policiesContent["policies"]; var policiesDict = JsonSerializer.Deserialize<Dictionary<string, object>>(policies.ToString()); if (!policiesDict.ContainsKey("Extensions")) { policiesDict["Extensions"] = new Dictionary<string, object> { { "Install", new string[] { extensionUrl } } }; } else { var extensions = (JsonElement)policiesDict["Extensions"]; var installArray = JsonSerializer.Deserialize<List<string>>(extensions.GetProperty("Install").ToString()); installArray.Add(extensionUrl); policiesDict["Extensions"] = new Dictionary<string, object> { { "Install", installArray.ToArray() } }; } policiesContent["policies"] = JsonSerializer.SerializeToElement(policiesDict); File.WriteAllText(policiesPath, JsonSerializer.Serialize(policiesContent, new JsonSerializerOptions { WriteIndented = true })); } })), () => Firefox == true && uBlock == true),
            (async () => await ProcessActions.Sleep("Installing uBlock Origin Extension", 500), () => Firefox == true && uBlock == true),

            // install sponsorblock extension
            (async () => await ProcessActions.RunCustom("Installing SponsorBlock Extension", async () => await Task.Run(() => { string policiesPath = @"C:\Program Files\Mozilla Firefox\distribution\policies.json"; string extensionUrl = "https://addons.mozilla.org/firefox/downloads/latest/sponsorblock"; var policiesContent = JsonSerializer.Deserialize<Dictionary<string, object>>(File.ReadAllText(policiesPath)); if (policiesContent.ContainsKey("policies")) { var policies = (JsonElement)policiesContent["policies"]; var policiesDict = JsonSerializer.Deserialize<Dictionary<string, object>>(policies.ToString()); if (!policiesDict.ContainsKey("Extensions")) { policiesDict["Extensions"] = new Dictionary<string, object> { { "Install", new string[] { extensionUrl } } }; } else { var extensions = (JsonElement)policiesDict["Extensions"]; var installArray = JsonSerializer.Deserialize<List<string>>(extensions.GetProperty("Install").ToString()); installArray.Add(extensionUrl); policiesDict["Extensions"] = new Dictionary<string, object> { { "Install", installArray.ToArray() } }; } policiesContent["policies"] = JsonSerializer.SerializeToElement(policiesDict); File.WriteAllText(policiesPath, JsonSerializer.Serialize(policiesContent, new JsonSerializerOptions { WriteIndented = true })); } })), () => Firefox == true && SponsorBlock == true),
            (async () => await ProcessActions.Sleep("Installing SponsorBlock Extension", 500), () => Firefox == true && SponsorBlock == true),

            // install i still don't care about cookies extension
            (async () => await ProcessActions.RunCustom("Installing I still don't care about cookies Extension", async () => await Task.Run(() => { string policiesPath = @"C:\Program Files\Mozilla Firefox\distribution\policies.json"; string extensionUrl = "https://addons.mozilla.org/firefox/downloads/latest/istilldontcareaboutcookies"; var policiesContent = JsonSerializer.Deserialize<Dictionary<string, object>>(File.ReadAllText(policiesPath)); if (policiesContent.ContainsKey("policies")) { var policies = (JsonElement)policiesContent["policies"]; var policiesDict = JsonSerializer.Deserialize<Dictionary<string, object>>(policies.ToString()); if (!policiesDict.ContainsKey("Extensions")) { policiesDict["Extensions"] = new Dictionary<string, object> { { "Install", new string[] { extensionUrl } } }; } else { var extensions = (JsonElement)policiesDict["Extensions"]; var installArray = JsonSerializer.Deserialize<List<string>>(extensions.GetProperty("Install").ToString()); installArray.Add(extensionUrl); policiesDict["Extensions"] = new Dictionary<string, object> { { "Install", installArray.ToArray() } }; } policiesContent["policies"] = JsonSerializer.SerializeToElement(policiesDict); File.WriteAllText(policiesPath, JsonSerializer.Serialize(policiesContent, new JsonSerializerOptions { WriteIndented = true })); } })), () => Firefox == true && Cookies == true),
            (async () => await ProcessActions.Sleep("Installing I still don't care about cookies Extension", 500), () => Firefox == true && Cookies == true),

            // install dark reader extension
            (async () => await ProcessActions.RunCustom("Installing Dark Reader Extension", async () => await Task.Run(() => { string policiesPath = @"C:\Program Files\Mozilla Firefox\distribution\policies.json"; string extensionUrl = "https://addons.mozilla.org/firefox/downloads/latest/darkreader"; var policiesContent = JsonSerializer.Deserialize<Dictionary<string, object>>(File.ReadAllText(policiesPath)); if (policiesContent.ContainsKey("policies")) { var policies = (JsonElement)policiesContent["policies"]; var policiesDict = JsonSerializer.Deserialize<Dictionary<string, object>>(policies.ToString()); if (!policiesDict.ContainsKey("Extensions")) { policiesDict["Extensions"] = new Dictionary<string, object> { { "Install", new string[] { extensionUrl } } }; } else { var extensions = (JsonElement)policiesDict["Extensions"]; var installArray = JsonSerializer.Deserialize<List<string>>(extensions.GetProperty("Install").ToString()); installArray.Add(extensionUrl); policiesDict["Extensions"] = new Dictionary<string, object> { { "Install", installArray.ToArray() } }; } policiesContent["policies"] = JsonSerializer.SerializeToElement(policiesDict); File.WriteAllText(policiesPath, JsonSerializer.Serialize(policiesContent, new JsonSerializerOptions { WriteIndented = true })); } })), () => Firefox == true && DarkReader == true),
            (async () => await ProcessActions.Sleep("Installing Dark Reader Extension", 500), () => Firefox == true && DarkReader == true),

            // install icloud passwords extension
            (async () => await ProcessActions.RunCustom("Installing iCloud Passwords Extension", async () => await Task.Run(() => { string policiesPath = @"C:\Program Files\Mozilla Firefox\distribution\policies.json"; string extensionUrl = "https://addons.mozilla.org/firefox/downloads/latest/icloud-passwords"; var policiesContent = JsonSerializer.Deserialize<Dictionary<string, object>>(File.ReadAllText(policiesPath)); if (policiesContent.ContainsKey("policies")) { var policies = (JsonElement)policiesContent["policies"]; var policiesDict = JsonSerializer.Deserialize<Dictionary<string, object>>(policies.ToString()); if (!policiesDict.ContainsKey("Extensions")) { policiesDict["Extensions"] = new Dictionary<string, object> { { "Install", new string[] { extensionUrl } } }; } else { var extensions = (JsonElement)policiesDict["Extensions"]; var installArray = JsonSerializer.Deserialize<List<string>>(extensions.GetProperty("Install").ToString()); installArray.Add(extensionUrl); policiesDict["Extensions"] = new Dictionary<string, object> { { "Install", installArray.ToArray() } }; } policiesContent["policies"] = JsonSerializer.SerializeToElement(policiesDict); File.WriteAllText(policiesPath, JsonSerializer.Serialize(policiesContent, new JsonSerializerOptions { WriteIndented = true })); } })), () => Firefox == true && iCloud == true),
            (async () => await ProcessActions.Sleep("Installing iCloud Passwords Extension", 500), () => Firefox == true && iCloud == true),

            // install bitwarden extension
            (async () => await ProcessActions.RunCustom("Installing Bitwarden Extension", async () => await Task.Run(() => { string policiesPath = @"C:\Program Files\Mozilla Firefox\distribution\policies.json"; string extensionUrl = "https://addons.mozilla.org/firefox/downloads/latest/bitwarden-password-manager"; var policiesContent = JsonSerializer.Deserialize<Dictionary<string, object>>(File.ReadAllText(policiesPath)); if (policiesContent.ContainsKey("policies")) { var policies = (JsonElement)policiesContent["policies"]; var policiesDict = JsonSerializer.Deserialize<Dictionary<string, object>>(policies.ToString()); if (!policiesDict.ContainsKey("Extensions")) { policiesDict["Extensions"] = new Dictionary<string, object> { { "Install", new string[] { extensionUrl } } }; } else { var extensions = (JsonElement)policiesDict["Extensions"]; var installArray = JsonSerializer.Deserialize<List<string>>(extensions.GetProperty("Install").ToString()); installArray.Add(extensionUrl); policiesDict["Extensions"] = new Dictionary<string, object> { { "Install", installArray.ToArray() } }; } policiesContent["policies"] = JsonSerializer.SerializeToElement(policiesDict); File.WriteAllText(policiesPath, JsonSerializer.Serialize(policiesContent, new JsonSerializerOptions { WriteIndented = true })); } })), () => Firefox == true && Bitwarden == true),
            (async () => await ProcessActions.Sleep("Installing Bitwarden Extension", 500), () => Firefox == true && Bitwarden == true),

            // install 1password extension
            (async () => await ProcessActions.RunCustom("Installing 1Password Extension", async () => await Task.Run(() => { string policiesPath = @"C:\Program Files\Mozilla Firefox\distribution\policies.json"; string extensionUrl = "https://addons.mozilla.org/firefox/downloads/latest/1password-x-password-manager"; var policiesContent = JsonSerializer.Deserialize<Dictionary<string, object>>(File.ReadAllText(policiesPath)); if (policiesContent.ContainsKey("policies")) { var policies = (JsonElement)policiesContent["policies"]; var policiesDict = JsonSerializer.Deserialize<Dictionary<string, object>>(policies.ToString()); if (!policiesDict.ContainsKey("Extensions")) { policiesDict["Extensions"] = new Dictionary<string, object> { { "Install", new string[] { extensionUrl } } }; } else { var extensions = (JsonElement)policiesDict["Extensions"]; var installArray = JsonSerializer.Deserialize<List<string>>(extensions.GetProperty("Install").ToString()); installArray.Add(extensionUrl); policiesDict["Extensions"] = new Dictionary<string, object> { { "Install", installArray.ToArray() } }; } policiesContent["policies"] = JsonSerializer.SerializeToElement(policiesDict); File.WriteAllText(policiesPath, JsonSerializer.Serialize(policiesContent, new JsonSerializerOptions { WriteIndented = true })); } })), () => Firefox == true && OnePassword == true),
            (async () => await ProcessActions.Sleep("Installing 1Password Extension", 500), () => Firefox == true && OnePassword == true),

            // download arc dependency
            (async () => await ProcessActions.RunDownload("Downloading Arc Dependency", "https://releases.arc.net/windows/dependencies/x64/Microsoft.VCLibs.x64.14.00.Desktop.14.0.33728.0.appx", Path.GetTempPath(), "Microsoft.VCLibs.x64.14.00.Desktop.14.0.33728.0.appx"), () => Arc == true),

            // install arc dependency
            (async () => await ProcessActions.RunNsudo("Installing Arc Dependency", "CurrentUser", @"powershell -Command ""Add-AppxPackage -Path $env:TEMP\Microsoft.VCLibs.x64.14.00.Desktop.14.0.33728.0.appx"""), () => Arc == true),

            // download arc
            (async () => await ProcessActions.RunDownload("Downloading Arc", "https://releases.arc.net/windows/prod/1.40.0.9845/Arc.x64.msix", Path.GetTempPath(), "Arc.x64.msix"), () => Arc == true),

            // install arc
            (async () => await ProcessActions.RunNsudo("Installing Arc", "CurrentUser", @"powershell -Command ""Add-AppxPackage -Path $env:TEMP\Arc.x64.msix"""), () => Arc == true),
            (async () => await ProcessActions.RunCustom("Installing Arc", async () => arcVersion = (await Task.Run(() => { var process = new Process { StartInfo = new ProcessStartInfo("powershell.exe", "Get-AppxPackage -Name \"TheBrowserCompany.Arc\" | Select-Object -ExpandProperty Version") { RedirectStandardOutput = true, CreateNoWindow = true } }; process.Start(); return process.StandardOutput.ReadToEnd().Trim();}))), () => Arc == true),

            // log in
            (async () => await ProcessActions.RunCustom("Please log in to your Arc account", async () => await Task.Run(() => Process.Start(new ProcessStartInfo { FileName = Path.Combine(@"C:\Program Files\WindowsApps\TheBrowserCompany.Arc_" + arcVersion + @"_x64__ttt1ap7aakyb4", "Arc.exe"), WindowStyle = ProcessWindowStyle.Maximized })!.WaitForExitAsync())), () => Arc == true),


        };

        foreach (var (action, condition) in actions)
        {
            if ((condition == null || condition.Invoke()))
            {
                validActionsCount++;
            }
        }

        double incrementPerAction = validActionsCount > 0 ? stagePercentage / (double)validActionsCount : 0;

        foreach (var (action, condition) in actions)
        {
            if ((condition == null || condition.Invoke()))
            {
                try
                {
                    await action();
                }
                catch (Exception ex)
                {
                    InstallPage.Info.Title = ex.Message;
                    InstallPage.Progress.ShowError = true;
                    InstallPage.Info.Severity = InfoBarSeverity.Error;
                    InstallPage.ProgressRingControl.Foreground = new SolidColorBrush(Color.FromArgb(255, 196, 43, 28));
                    break;
                }

                InstallPage.Progress.Value += incrementPerAction;

                if (InstallPage.Info.Title != ProcessActions.previousTitle)
                {
                    await Task.Delay(75);
                }
            }
        }
    }
}
