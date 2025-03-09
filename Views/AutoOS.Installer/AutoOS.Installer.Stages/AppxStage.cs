using AutoOS.Views.Installer.Actions;

namespace AutoOS.Views.Installer.Stages;

public static class AppxStage
{
    public static async Task Run()
    {
        InstallPage.Status.Text = "Configuring Appx Packages...";

        string previousTitle = string.Empty;
        int stagePercentage = 10;

        var actions = new List<(string Title, Func<Task> Action, Func<bool> Condition)>
        {
            // onedrive
            ("Uninstalling OneDrive", async () => await ProcessActions.RunNsudo("CurrentUser", @"cmd /c for %a in (""SysWOW64"" ""System32"") do (if exist ""%windir%\%~a\OneDriveSetup.exe"" (""%windir%\%~a\OneDriveSetup.exe"" /uninstall)) && reg delete ""HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Desktop\NameSpace\{018D5C66-4533-4307-9B53-224DE2ED1FE6}"" /f"), null),
            ("Uninstalling OneDrive", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"cmd /c rmdir /s /q ""C:\ProgramData\Microsoft OneDrive"""), null),
            ("Uninstalling OneDrive", async () => await ProcessActions.RunNsudo("CurrentUser", @"cmd /c rmdir /s /q ""%LOCALAPPDATA%\Microsoft\OneDrive"""), null),

            // clipchamp
            ("Uninstalling Clipchamp.Clipchamp_yxz26nhyzhsrt", async () => await ProcessActions.RemoveAppx("Clipchamp.Clipchamp"), null),
            ("Uninstalling provisioned Clipchamp.Clipchamp_yxz26nhyzhsrt", async () => await ProcessActions.RemoveAppxProvisioned("Clipchamp.Clipchamp"), null),
            ("Deprovisioning Clipchamp.Clipchamp_yxz26nhyzhsrt", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Appx\AppxAllUserStore\Deprovisioned\Clipchamp.Clipchamp_yxz26nhyzhsrt"" /f"), null),

            // cortana
            ("Uninstalling Microsoft.549981C3F5F10_8wekyb3d8bbwe", async () => await ProcessActions.RemoveAppx("Microsoft.549981C3F5F10"), null),
            ("Uninstalling provisioned Microsoft.549981C3F5F10_8wekyb3d8bbwe", async () => await ProcessActions.RemoveAppxProvisioned("Microsoft.549981C3F5F10"), null),
            ("Deprovisioning Microsoft.549981C3F5F10_8wekyb3d8bbwe", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Appx\AppxAllUserStore\Deprovisioned\Microsoft.549981C3F5F10_8wekyb3d8bbwe"" /f"), null),

            // bing news
            ("Uninstalling Microsoft.BingNews_8wekyb3d8bbwe", async () => await ProcessActions.RemoveAppx("Microsoft.BingNews"), null),
            ("Uninstalling provisioned Microsoft.BingNews_8wekyb3d8bbwe", async () => await ProcessActions.RemoveAppxProvisioned("Microsoft.BingNews"), null),
            ("Deprovisioning Microsoft.BingNews_8wekyb3d8bbwe", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Appx\AppxAllUserStore\Deprovisioned\Microsoft.BingNews_8wekyb3d8bbwe"" /f"), null),

            // bing weather
            ("Uninstalling Microsoft.BingWeather_8wekyb3d8bbwe", async () => await ProcessActions.RemoveAppx("Microsoft.BingWeather"), null),
            ("Uninstalling provisioned Microsoft.BingWeather_8wekyb3d8bbwe", async () => await ProcessActions.RemoveAppxProvisioned("Microsoft.BingWeather"), null),
            ("Deprovisioning Microsoft.BingWeather_8wekyb3d8bbwe", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Appx\AppxAllUserStore\Deprovisioned\Microsoft.BingWeather_8wekyb3d8bbwe"" /f"), null),

            // copilot
            ("Uninstalling Microsoft.Copilot_8wekyb3d8bbwe", async () => await ProcessActions.RemoveAppx("Microsoft.Copilot"), null),
            //("Microsoft.Copilot", async () => await ProcessActions.RemoveAppxProvisioned("Microsoft.Copilot"), null),
            ("Deprovisioning Copilot_8wekyb3d8bbwe", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Appx\AppxAllUserStore\Deprovisioned\Microsoft.Copilot_8wekyb3d8bbwe"" /f"), null),

            // get help
            ("Uninstalling Microsoft.GetHelp_8wekyb3d8bbwe", async () => await ProcessActions.RemoveAppx("Microsoft.GetHelp"), null),
            ("Uninstalling provisioned Microsoft.GetHelp_8wekyb3d8bbwe", async () => await ProcessActions.RemoveAppxProvisioned("Microsoft.GetHelp"), null),
            ("Deprovisioning Microsoft.GetHelp_8wekyb3d8bbwe", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Appx\AppxAllUserStore\Deprovisioned\Microsoft.GetHelp_8wekyb3d8bbwe"" /f"), null),

            // get started
            ("Uninstalling Microsoft.Getstarted_8wekyb3d8bbwe", async () => await ProcessActions.RemoveAppx("Microsoft.Getstarted"), null),
            ("Uninstalling provisioned Microsoft.Getstarted_8wekyb3d8bbwe", async () => await ProcessActions.RemoveAppxProvisioned("Microsoft.Getstarted"), null),
            ("Deprovisioning Microsoft.Getstarted_8wekyb3d8bbwe", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Appx\AppxAllUserStore\Deprovisioned\Microsoft.Getstarted_8wekyb3d8bbwe"" /f"), null),

            // office hub
            ("Uninstalling Microsoft.MicrosoftOfficeHub_8wekyb3d8bbwe", async () => await ProcessActions.RemoveAppx("Microsoft.MicrosoftOfficeHub"), null),
            ("Uninstalling provisioned Microsoft.MicrosoftOfficeHub_8wekyb3d8bbwe", async () => await ProcessActions.RemoveAppxProvisioned("Microsoft.MicrosoftOfficeHub"), null),
            ("Deprovisioning Microsoft.MicrosoftOfficeHub_8wekyb3d8bbwe", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Appx\AppxAllUserStore\Deprovisioned\Microsoft.MicrosoftOfficeHub_8wekyb3d8bbwe"" /f"), null),

            // solitaire collection
            ("Uninstalling Microsoft.MicrosoftSolitaireCollection_8wekyb3d8bbwe", async () => await ProcessActions.RemoveAppx("Microsoft.MicrosoftSolitaireCollection"), null),
            ("Uninstalling provisioned Microsoft.MicrosoftSolitaireCollection_8wekyb3d8bbwe", async () => await ProcessActions.RemoveAppxProvisioned("Microsoft.MicrosoftSolitaireCollection"), null),
            ("Deprovisioning Microsoft.MicrosoftSolitaireCollection_8wekyb3d8bbwe", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Appx\AppxAllUserStore\Deprovisioned\Microsoft.MicrosoftSolitaireCollection_8wekyb3d8bbwe"" /f"), null),

            // sticky notes
            ("Uninstalling Microsoft.MicrosoftStickyNotes_8wekyb3d8bbwe", async () => await ProcessActions.RemoveAppx("Microsoft.MicrosoftStickyNotes"), null),
            ("Uninstalling provisioned Microsoft.MicrosoftStickyNotes_8wekyb3d8bbwe", async () => await ProcessActions.RemoveAppxProvisioned("Microsoft.MicrosoftStickyNotes"), null),
            ("Deprovisioning Microsoft.MicrosoftStickyNotes_8wekyb3d8bbwe", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Appx\AppxAllUserStore\Deprovisioned\Microsoft.MicrosoftStickyNotes_8wekyb3d8bbwe"" /f"), null),

            // outlook
            ("Uninstalling Microsoft.OutlookForWindows_8wekyb3d8bbwe", async () => await ProcessActions.RemoveAppx("Microsoft.OutlookForWindows"), null),
            ("Uninstalling provisioned Microsoft.OutlookForWindows_8wekyb3d8bbwe", async () => await ProcessActions.RemoveAppxProvisioned("Microsoft.OutlookForWindows"), null),
            ("Deprovisioning Microsoft.OutlookForWindows_8wekyb3d8bbwe", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Appx\AppxAllUserStore\Deprovisioned\Microsoft.OutlookForWindows_8wekyb3d8bbwe"" /f"), null),

            // paint
            ("Uninstalling Microsoft.Paint_8wekyb3d8bbwe", async () => await ProcessActions.RemoveAppx("Microsoft.Paint"), null),
            ("Uninstalling provisioned Microsoft.Paint_8wekyb3d8bbwe", async () => await ProcessActions.RemoveAppxProvisioned("Microsoft.Paint"), null),
            ("Deprovisioning Microsoft.Paint_8wekyb3d8bbwe", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Appx\AppxAllUserStore\Deprovisioned\Microsoft.Paint_8wekyb3d8bbwe"" /f"), null),

            // people
            ("Uninstalling Microsoft.People_8wekyb3d8bbwe", async () => await ProcessActions.RemoveAppx("Microsoft.People"), null),
            ("Uninstalling provisioned Microsoft.People_8wekyb3d8bbwe", async () => await ProcessActions.RemoveAppxProvisioned("Microsoft.People"), null),
            ("Deprovisioning Microsoft.People_8wekyb3d8bbwe", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Appx\AppxAllUserStore\Deprovisioned\Microsoft.People_8wekyb3d8bbwe"" /f"), null),

            // power automate
            ("Uninstalling Microsoft.PowerAutomateDesktop_8wekyb3d8bbwe", async () => await ProcessActions.RemoveAppx("Microsoft.PowerAutomateDesktop"), null),
            ("Uninstalling provisioned Microsoft.PowerAutomateDesktop_8wekyb3d8bbwe", async () => await ProcessActions.RemoveAppxProvisioned("Microsoft.PowerAutomateDesktop"), null),
            ("Deprovisioning Microsoft.PowerAutomateDesktop_8wekyb3d8bbwe", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Appx\AppxAllUserStore\Deprovisioned\Microsoft.PowerAutomateDesktop_8wekyb3d8bbwe"" /f"), null),

            // todos
            ("Uninstalling Microsoft.Todos_8wekyb3d8bbwe", async () => await ProcessActions.RemoveAppx("Microsoft.Todos"), null),
            ("Uninstalling provisioned Microsoft.Todos_8wekyb3d8bbwe", async () => await ProcessActions.RemoveAppxProvisioned("Microsoft.Todos"), null),
            ("Deprovisioning Microsoft.Todos_8wekyb3d8bbwe", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Appx\AppxAllUserStore\Deprovisioned\Microsoft.Todos_8wekyb3d8bbwe"" /f"), null),

            // dev home
            ("Uninstalling Microsoft.Windows.DevHome_8wekyb3d8bbwe", async () => await ProcessActions.RemoveAppx("Microsoft.Windows.DevHome"), null),
            ("Uninstalling provisioned Microsoft.Windows.DevHome_8wekyb3d8bbwe", async () => await ProcessActions.RemoveAppxProvisioned("Microsoft.Windows.DevHome"), null),
            ("Deprovisioning Microsoft.Windows.DevHome_8wekyb3d8bbwe", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Appx\AppxAllUserStore\Deprovisioned\Microsoft.Windows.DevHome_8wekyb3d8bbwe"" /f"), null),

            // alarms
            ("Uninstalling Microsoft.WindowsAlarms_8wekyb3d8bbwe", async () => await ProcessActions.RemoveAppx("Microsoft.WindowsAlarms"), null),
            ("Uninstalling provisioned Microsoft.WindowsAlarms_8wekyb3d8bbwe", async () => await ProcessActions.RemoveAppxProvisioned("Microsoft.WindowsAlarms"), null),
            ("Deprovisioning Microsoft.WindowsAlarms_8wekyb3d8bbwe", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Appx\AppxAllUserStore\Deprovisioned\Microsoft.WindowsAlarms_8wekyb3d8bbwe"" /f"), null),

            // calculator
            ("Uninstalling Microsoft.WindowsCalculator_8wekyb3d8bbwe", async () => await ProcessActions.RemoveAppx("Microsoft.WindowsCalculator"), null),
            ("Uninstalling provisioned Microsoft.WindowsCalculator_8wekyb3d8bbwe", async () => await ProcessActions.RemoveAppxProvisioned("Microsoft.WindowsCalculator"), null),
            ("Deprovisioning Microsoft.WindowsCalculator_8wekyb3d8bbwe", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Appx\AppxAllUserStore\Deprovisioned\Microsoft.WindowsCalculator_8wekyb3d8bbwe"" /f"), null),

            // camera
            ("Uninstalling Microsoft.WindowsCamera_8wekyb3d8bbwe", async () => await ProcessActions.RemoveAppx("Microsoft.WindowsCamera"), null),
            ("Uninstalling provisioned Microsoft.WindowsCamera_8wekyb3d8bbwe", async () => await ProcessActions.RemoveAppxProvisioned("Microsoft.WindowsCamera"), null),
            ("Deprovisioning Microsoft.WindowsCamera_8wekyb3d8bbwe", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Appx\AppxAllUserStore\Deprovisioned\Microsoft.WindowsCamera_8wekyb3d8bbwe"" /f"), null),

            // communicationsapps
            ("Uninstalling microsoft.windowscommunicationsapps_8wekyb3d8bbwe", async () => await ProcessActions.RemoveAppx("microsoft.windowscommunicationsapps"), null),
            ("Uninstalling provisioned microsoft.windowscommunicationsapps_8wekyb3d8bbwe", async () => await ProcessActions.RemoveAppxProvisioned("microsoft.windowscommunicationsapps"), null),
            ("Deprovisioning microsoft.windowscommunicationsapps_8wekyb3d8bbwe", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Appx\AppxAllUserStore\Deprovisioned\microsoft.windowscommunicationsapps_8wekyb3d8bbwe"" /f"), null),

            // feedback hub
            ("Uninstalling Microsoft.WindowsFeedbackHub_8wekyb3d8bbwe", async () => await ProcessActions.RemoveAppx("Microsoft.WindowsFeedbackHub"), null),
            ("Uninstalling provisioned Microsoft.WindowsFeedbackHub_8wekyb3d8bbwe", async () => await ProcessActions.RemoveAppxProvisioned("Microsoft.WindowsFeedbackHub"), null),
            ("Deprovisioning Microsoft.WindowsFeedbackHub_8wekyb3d8bbwe", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Appx\AppxAllUserStore\Deprovisioned\Microsoft.WindowsFeedbackHub_8wekyb3d8bbwe"" /f"), null),

            // maps
            ("Uninstalling Microsoft.WindowsMaps_8wekyb3d8bbwe", async () => await ProcessActions.RemoveAppx("Microsoft.WindowsMaps"), null),
            ("Uninstalling provisioned Microsoft.WindowsMaps_8wekyb3d8bbwe", async () => await ProcessActions.RemoveAppxProvisioned("Microsoft.WindowsMaps"), null),
            ("Deprovisioning Microsoft.WindowsMaps_8wekyb3d8bbwe", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Appx\AppxAllUserStore\Deprovisioned\Microsoft.WindowsMaps_8wekyb3d8bbwe"" /f"), null),

            // sound recorder
            ("Uninstalling Microsoft.WindowsSoundRecorder_8wekyb3d8bbwe", async () => await ProcessActions.RemoveAppx("Microsoft.WindowsSoundRecorder"), null),
            ("Uninstalling provisioned Microsoft.WindowsSoundRecorder_8wekyb3d8bbwe", async () => await ProcessActions.RemoveAppxProvisioned("Microsoft.WindowsSoundRecorder"), null),
            ("Deprovisioning Microsoft.WindowsSoundRecorder_8wekyb3d8bbwe", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Appx\AppxAllUserStore\Deprovisioned\Microsoft.WindowsSoundRecorder_8wekyb3d8bbwe"" /f"), null),

            // terminal
            ("Uninstalling Microsoft.WindowsTerminal_8wekyb3d8bbwe", async () => await ProcessActions.RemoveAppx("Microsoft.WindowsTerminal"), null),
            ("Uninstalling provisioned Microsoft.WindowsTerminal_8wekyb3d8bbwe", async () => await ProcessActions.RemoveAppxProvisioned("Microsoft.WindowsTerminal"), null),
            ("Deprovisioning Microsoft.WindowsTerminal_8wekyb3d8bbwe", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Appx\AppxAllUserStore\Deprovisioned\Microsoft.WindowsTerminal_8wekyb3d8bbwe"" /f"), null),

            // xbox speech to text overlay
            ("Uninstalling Microsoft.XboxSpeechToTextOverlay_8wekyb3d8bbwe", async () => await ProcessActions.RemoveAppx("Microsoft.XboxSpeechToTextOverlay"), null),
            ("Uninstalling provisioned Microsoft.XboxSpeechToTextOverlay_8wekyb3d8bbwe", async () => await ProcessActions.RemoveAppxProvisioned("Microsoft.XboxSpeechToTextOverlay"), null),
            ("Deprovisioning Microsoft.XboxSpeechToTextOverlay_8wekyb3d8bbwe", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Appx\AppxAllUserStore\Deprovisioned\Microsoft.XboxSpeechToTextOverlay_8wekyb3d8bbwe"" /f"), null),

            // your phone
            ("Uninstalling Microsoft.YourPhone_8wekyb3d8bbwe", async () => await ProcessActions.RemoveAppx("Microsoft.YourPhone"), null),
            ("Uninstalling provisioned Microsoft.YourPhone_8wekyb3d8bbwe", async () => await ProcessActions.RemoveAppxProvisioned("Microsoft.YourPhone"), null),
            ("Deprovisioning Microsoft.YourPhone_8wekyb3d8bbwe", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Appx\AppxAllUserStore\Deprovisioned\Microsoft.YourPhone_8wekyb3d8bbwe"" /f"), null),

            // zune music
            ("Uninstalling Microsoft.ZuneMusic_8wekyb3d8bbwe", async () => await ProcessActions.RemoveAppx("Microsoft.ZuneMusic"), null),
            ("Uninstalling provisioned Microsoft.ZuneMusic_8wekyb3d8bbwe", async () => await ProcessActions.RemoveAppxProvisioned("Microsoft.ZuneMusic"), null),
            ("Deprovisioning Microsoft.ZuneMusic_8wekyb3d8bbwe", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Appx\AppxAllUserStore\Deprovisioned\Microsoft.ZuneMusic_8wekyb3d8bbwe"" /f"), null),

            // family
            ("Uninstalling MicrosoftCorporationII.MicrosoftFamily_8wekyb3d8bbwe", async () => await ProcessActions.RemoveAppx("MicrosoftCorporationII.MicrosoftFamily"), null),
            ("Uninstalling provisioned MicrosoftCorporationII.MicrosoftFamily_8wekyb3d8bbwe", async () => await ProcessActions.RemoveAppxProvisioned("MicrosoftCorporationII.MicrosoftFamily"), null),
            ("Deprovisioning MicrosoftCorporationII.MicrosoftFamily_8wekyb3d8bbwe", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Appx\AppxAllUserStore\Deprovisioned\MicrosoftCorporationII.MicrosoftFamily_8wekyb3d8bbwe"" /f"), null),

            // quick assist
            ("Uninstalling MicrosoftCorporationII.QuickAssist_8wekyb3d8bbwe", async () => await ProcessActions.RemoveAppx("MicrosoftCorporationII.QuickAssist"), null),
            ("Uninstalling provisioned MicrosoftCorporationII.QuickAssist_8wekyb3d8bbwe", async () => await ProcessActions.RemoveAppxProvisioned("MicrosoftCorporationII.QuickAssist"), null),
            ("Deprovisioning MicrosoftCorporationII.QuickAssist_8wekyb3d8bbwe", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Appx\AppxAllUserStore\Deprovisioned\MicrosoftCorporationII.QuickAssist_8wekyb3d8bbwe"" /f"), null),

            // teams  
            ("Uninstalling MicrosoftTeams_8wekyb3d8bbwe", async () => await ProcessActions.RemoveAppx("MicrosoftTeams"), null),
            ("Uninstalling provisioned MicrosoftTeams_8wekyb3d8bbwe", async () => await ProcessActions.RemoveAppxProvisioned("MicrosoftTeams"), null),
            ("Deprovisioning MicrosoftTeams_8wekyb3d8bbwe", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Appx\AppxAllUserStore\Deprovisioned\MicrosoftTeams_8wekyb3d8bbwe"" /f"), null),

            // client web experience
            ("Uninstalling MicrosoftWindows.Client.WebExperience_cw5n1h2txyewy", async () => await ProcessActions.RemoveAppx("MicrosoftWindows.Client.WebExperience"), null),
            //("MicrosoftWindows.Client.WebExperience", async () => await ProcessActions.RemoveAppxProvisioned("MicrosoftWindows.Client.WebExperience"), null),

            // remove all user deleted packages
            ("Removing all user deleted packages", async () => await ProcessActions.RunNsudo("CurrentUser", @"cmd /c rmdir /s /q ""C:\Program Files\WindowsApps\DeletedAllUserPackages"""), null),

            // update microsoft store
            ("Updating Microsoft.StorePurchaseApp_8wekyb3d8bbwe", async () => await ProcessActions.UpdateAppx("Microsoft.StorePurchaseApp_8wekyb3d8bbwe"), null),
            ("Updating Microsoft.WindowsStore_8wekyb3d8bbwe", async () => await ProcessActions.UpdateAppx("Microsoft.WindowsStore_8wekyb3d8bbwe"), null),
            ("Updating Microsoft.DesktopAppInstaller_8wekyb3d8bbwe", async () => await ProcessActions.UpdateAppx("Microsoft.DesktopAppInstaller_8wekyb3d8bbwe"), null),

            // update notepad
            ("Updating Microsoft.WindowsNotepad_8wekyb3d8bbwe", async () => await ProcessActions.UpdateAppx("Microsoft.WindowsNotepad_8wekyb3d8bbwe"), null),

            // update photos
            ("Updating Microsoft.Windows.Photos_8wekyb3d8bbwe", async () => await ProcessActions.UpdateAppx("Microsoft.Windows.Photos_8wekyb3d8bbwe"), null),

            // update movies & tv
            ("Updating Microsoft.ZuneVideo_8wekyb3d8bbwe", async () => await ProcessActions.UpdateAppx("Microsoft.ZuneVideo_8wekyb3d8bbwe"), null),

            // update snipping tool
            ("Updating Microsoft.ScreenSketch_8wekyb3d8bbwe", async () => await ProcessActions.UpdateAppx("Microsoft.ScreenSketch_8wekyb3d8bbwe"), null),

            // update xbox identity provider
            ("Updating Microsoft.XboxIdentityProvider_8wekyb3d8bbwe", async () => await ProcessActions.UpdateAppx("Microsoft.XboxIdentityProvider_8wekyb3d8bbwe"), null),
            ("Updating Microsoft.Xbox.TCUI_8wekyb3d8bbwe", async () => await ProcessActions.UpdateAppx("Microsoft.Xbox.TCUI_8wekyb3d8bbwe"), null),

            // update xbox app
            ("Updating Microsoft.GamingApp_8wekyb3d8bbwe", async () => await ProcessActions.UpdateAppx("Microsoft.GamingApp_8wekyb3d8bbwe"), null),

            // update xbox game overlay
            ("Updating Microsoft.XboxGameOverlay_8wekyb3d8bbwe", async () => await ProcessActions.UpdateAppx("Microsoft.XboxGameOverlay_8wekyb3d8bbwe"), null),
            ("Updating Microsoft.XboxGamingOverlay_8wekyb3d8bbwe", async () => await ProcessActions.UpdateAppx("Microsoft.XboxGamingOverlay_8wekyb3d8bbwe"), null),

            // update HEIF image extensions
            ("Updating Microsoft.HEIFImageExtension_8wekyb3d8bbwe", async () => await ProcessActions.UpdateAppx("Microsoft.HEIFImageExtension_8wekyb3d8bbwe"), null),

            // update VP9 video extensions
            ("Updating Microsoft.VP9VideoExtensions_8wekyb3d8bbwe", async () => await ProcessActions.UpdateAppx("Microsoft.VP9VideoExtensions_8wekyb3d8bbwe"), null),

            // update web media extensions
            ("Updating Microsoft.WebMediaExtensions_8wekyb3d8bbwe", async () => await ProcessActions.UpdateAppx("Microsoft.WebMediaExtensions_8wekyb3d8bbwe"), null),
                
            // update webp image extension
            ("Updating WebpImageExtension_8wekyb3d8bbwe", async () => await ProcessActions.UpdateAppx("Microsoft.WebpImageExtension_8wekyb3d8bbwe"), null),

            // update hevc video extension
            ("Updating Microsoft.HEVCVideoExtension_8wekyb3d8bbwe", async () => await ProcessActions.UpdateAppx("Microsoft.HEVCVideoExtension_8wekyb3d8bbwe"), null),

            // update raw image extension
            ("Updating Microsoft.RawImageExtension_8wekyb3d8bbwe", async () => await ProcessActions.UpdateAppx("Microsoft.RawImageExtension_8wekyb3d8bbwe"), null),
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
                InstallPage.Info.Title = actionTitle + "...";

                try
                {
                    await action();
                }
                catch (Exception ex)
                {
                    InstallPage.Info.Title = ex.Message;
                    InstallPage.Info.Severity = InfoBarSeverity.Error;
                    InstallPage.Progress.Foreground = ProcessActions.GetColor("LightError", "DarkError");
                    InstallPage.ProgressRingControl.Foreground = ProcessActions.GetColor("LightError", "DarkError");
                    InstallPage.ProgressRingControl.Visibility = Visibility.Collapsed;
                    InstallPage.ResumeButton.Visibility = Visibility.Visible;

                    var tcs = new TaskCompletionSource<bool>();

                    InstallPage.ResumeButton.Click += (sender, e) =>
                    {
                        tcs.TrySetResult(true);
                        InstallPage.Info.Severity = InfoBarSeverity.Informational;
                        InstallPage.Progress.Foreground = ProcessActions.GetColor("LightNormal", "DarkNormal");
                        InstallPage.ProgressRingControl.Foreground = null;
                        InstallPage.ProgressRingControl.Visibility = Visibility.Visible;
                        InstallPage.ResumeButton.Visibility = Visibility.Collapsed;

                    };

                    await tcs.Task;
                }
            }

            InstallPage.Progress.Value += incrementPerTitle;

            previousTitle = title;
        }
    }
}
