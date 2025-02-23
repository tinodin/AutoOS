using AutoOS.Views.Installer.Actions;

namespace AutoOS.Views.Installer.Stages;

public static class AppxStage
{
    public static async Task Run()
    {
        InstallPage.Status.Text = "Configuring Appx Packages...";

        int validActionsCount = 0;
        int stagePercentage = 10;

        var actions = new List<(Func<Task> Action, Func<bool> Condition)>
        {
            // onedrive
            (async () => await ProcessActions.RunNsudo("Uninstalling OneDrive", "CurrentUser", @"cmd /c for %a in (""SysWOW64"" ""System32"") do (if exist ""%windir%\%~a\OneDriveSetup.exe"" (""%windir%\%~a\OneDriveSetup.exe"" /uninstall)) && reg delete ""HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Desktop\NameSpace\{018D5C66-4533-4307-9B53-224DE2ED1FE6}"" /f"), null),
            (async () => await ProcessActions.RunNsudo("Uninstalling OneDrive", "TrustedInstaller", @"cmd /c rmdir /s /q ""C:\ProgramData\Microsoft OneDrive"""), null),
            (async () => await ProcessActions.RunNsudo("Uninstalling OneDrive", "CurrentUser", @"cmd /c rmdir /s /q ""%LOCALAPPDATA%\Microsoft\OneDrive"""), null),

            // clipchamp
            (async () => await ProcessActions.RemoveAppx("Uninstalling Clipchamp.Clipchamp_yxz26nhyzhsrt", "Clipchamp.Clipchamp"), null),
            (async () => await ProcessActions.RemoveAppxProvisioned("Uninstalling provisioned Clipchamp.Clipchamp_yxz26nhyzhsrt", "Clipchamp.Clipchamp"), null),
            (async () => await ProcessActions.RunNsudo("Deprovisioning Clipchamp.Clipchamp_yxz26nhyzhsrt", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Appx\AppxAllUserStore\Deprovisioned\Clipchamp.Clipchamp_yxz26nhyzhsrt"" /f"), null),

            // cortana
            (async () => await ProcessActions.RemoveAppx("Uninstalling Microsoft.549981C3F5F10_8wekyb3d8bbwe", "Microsoft.549981C3F5F10"), null),
            (async () => await ProcessActions.RemoveAppxProvisioned("Uninstalling provisioned Microsoft.549981C3F5F10_8wekyb3d8bbwe", "Microsoft.549981C3F5F10"), null),
            (async () => await ProcessActions.RunNsudo("Deprovisioning Microsoft.549981C3F5F10_8wekyb3d8bbwe", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Appx\AppxAllUserStore\Deprovisioned\Microsoft.549981C3F5F10_8wekyb3d8bbwe"" /f"), null),

            // bing news
            (async () => await ProcessActions.RemoveAppx("Uninstalling Microsoft.BingNews_8wekyb3d8bbwe", "Microsoft.BingNews"), null),
            (async () => await ProcessActions.RemoveAppxProvisioned("Uninstalling provisioned Microsoft.BingNews_8wekyb3d8bbwe", "Microsoft.BingNews"), null),
            (async () => await ProcessActions.RunNsudo("Deprovisioning Microsoft.BingNews_8wekyb3d8bbwe", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Appx\AppxAllUserStore\Deprovisioned\Microsoft.BingNews_8wekyb3d8bbwe"" /f"), null),

            // bing weather
            (async () => await ProcessActions.RemoveAppx("Uninstalling Microsoft.BingWeather_8wekyb3d8bbwe", "Microsoft.BingWeather"), null),
            (async () => await ProcessActions.RemoveAppxProvisioned("Uninstalling provisioned Microsoft.BingWeather_8wekyb3d8bbwe", "Microsoft.BingWeather"), null),
            (async () => await ProcessActions.RunNsudo("Deprovisioning Microsoft.BingWeather_8wekyb3d8bbwe", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Appx\AppxAllUserStore\Deprovisioned\Microsoft.BingWeather_8wekyb3d8bbwe"" /f"), null),

            // copilot
            (async () => await ProcessActions.RemoveAppx("Uninstalling Microsoft.Copilot_8wekyb3d8bbwe", "Microsoft.Copilot"), null),
            //(async () => await ProcessActions.RemoveAppxProvisioned("Uninstalling provisioned Microsoft.Copilot_8wekyb3d8bbwe", "Microsoft.Copilot"), null),
            (async () => await ProcessActions.RunNsudo("Deprovisioning Copilot_8wekyb3d8bbwe", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Appx\AppxAllUserStore\Deprovisioned\Microsoft.Copilot_8wekyb3d8bbwe"" /f"), null),

            // get help
            (async () => await ProcessActions.RemoveAppx("Uninstalling Microsoft.GetHelp_8wekyb3d8bbwe", "Microsoft.GetHelp"), null),
            (async () => await ProcessActions.RemoveAppxProvisioned("Uninstalling provisioned Microsoft.GetHelp_8wekyb3d8bbwe", "Microsoft.GetHelp"), null),
            (async () => await ProcessActions.RunNsudo("Deprovisioning Microsoft.GetHelp_8wekyb3d8bbwe", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Appx\AppxAllUserStore\Deprovisioned\Microsoft.GetHelp_8wekyb3d8bbwe"" /f"), null),

            // get started
            (async () => await ProcessActions.RemoveAppx("Uninstalling Microsoft.Getstarted_8wekyb3d8bbwe", "Microsoft.Getstarted"), null),
            (async () => await ProcessActions.RemoveAppxProvisioned("Uninstalling provisioned Microsoft.Getstarted_8wekyb3d8bbwe", "Microsoft.Getstarted"), null),
            (async () => await ProcessActions.RunNsudo("Deprovisioning Microsoft.Getstarted_8wekyb3d8bbwe", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Appx\AppxAllUserStore\Deprovisioned\Microsoft.Getstarted_8wekyb3d8bbwe"" /f"), null),

            // office hub
            (async () => await ProcessActions.RemoveAppx("Uninstalling Microsoft.MicrosoftOfficeHub_8wekyb3d8bbwe", "Microsoft.MicrosoftOfficeHub"), null),
            (async () => await ProcessActions.RemoveAppxProvisioned("Uninstalling provisioned Microsoft.MicrosoftOfficeHub_8wekyb3d8bbwe", "Microsoft.MicrosoftOfficeHub"), null),
            (async () => await ProcessActions.RunNsudo("Deprovisioning Microsoft.MicrosoftOfficeHub_8wekyb3d8bbwe", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Appx\AppxAllUserStore\Deprovisioned\Microsoft.MicrosoftOfficeHub_8wekyb3d8bbwe"" /f"), null),

            // solitaire collection
            (async () => await ProcessActions.RemoveAppx("Uninstalling Microsoft.MicrosoftSolitaireCollection_8wekyb3d8bbwe", "Microsoft.MicrosoftSolitaireCollection"), null),
            (async () => await ProcessActions.RemoveAppxProvisioned("Uninstalling provisioned Microsoft.MicrosoftSolitaireCollection_8wekyb3d8bbwe", "Microsoft.MicrosoftSolitaireCollection"), null),
            (async () => await ProcessActions.RunNsudo("Deprovisioning Microsoft.MicrosoftSolitaireCollection_8wekyb3d8bbwe", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Appx\AppxAllUserStore\Deprovisioned\Microsoft.MicrosoftSolitaireCollection_8wekyb3d8bbwe"" /f"), null),

            // sticky notes
            (async () => await ProcessActions.RemoveAppx("Uninstalling Microsoft.MicrosoftStickyNotes_8wekyb3d8bbwe", "Microsoft.MicrosoftStickyNotes"), null),
            (async () => await ProcessActions.RemoveAppxProvisioned("Uninstalling provisioned Microsoft.MicrosoftStickyNotes_8wekyb3d8bbwe", "Microsoft.MicrosoftStickyNotes"), null),
            (async () => await ProcessActions.RunNsudo("Deprovisioning Microsoft.MicrosoftStickyNotes_8wekyb3d8bbwe", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Appx\AppxAllUserStore\Deprovisioned\Microsoft.MicrosoftStickyNotes_8wekyb3d8bbwe"" /f"), null),

            // outlook
            (async () => await ProcessActions.RemoveAppx("Uninstalling Microsoft.OutlookForWindows_8wekyb3d8bbwe", "Microsoft.OutlookForWindows"), null),
            (async () => await ProcessActions.RemoveAppxProvisioned("Uninstalling provisioned Microsoft.OutlookForWindows_8wekyb3d8bbwe", "Microsoft.OutlookForWindows"), null),
            (async () => await ProcessActions.RunNsudo("Deprovisioning Microsoft.OutlookForWindows_8wekyb3d8bbwe", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Appx\AppxAllUserStore\Deprovisioned\Microsoft.OutlookForWindows_8wekyb3d8bbwe"" /f"), null),

            // paint
            (async () => await ProcessActions.RemoveAppx("Uninstalling Microsoft.Paint_8wekyb3d8bbwe", "Microsoft.Paint"), null),
            (async () => await ProcessActions.RemoveAppxProvisioned("Uninstalling provisioned Microsoft.Paint_8wekyb3d8bbwe", "Microsoft.Paint"), null),
            (async () => await ProcessActions.RunNsudo("Deprovisioning Microsoft.Paint_8wekyb3d8bbwe", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Appx\AppxAllUserStore\Deprovisioned\Microsoft.Paint_8wekyb3d8bbwe"" /f"), null),

            // people
            (async () => await ProcessActions.RemoveAppx("Uninstalling Microsoft.People_8wekyb3d8bbwe", "Microsoft.People"), null),
            (async () => await ProcessActions.RemoveAppxProvisioned("Uninstalling provisioned Microsoft.People_8wekyb3d8bbwe", "Microsoft.People"), null),
            (async () => await ProcessActions.RunNsudo("Deprovisioning Microsoft.People_8wekyb3d8bbwe", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Appx\AppxAllUserStore\Deprovisioned\Microsoft.People_8wekyb3d8bbwe"" /f"), null),

            // power automate
            (async () => await ProcessActions.RemoveAppx("Uninstalling Microsoft.PowerAutomateDesktop_8wekyb3d8bbwe", "Microsoft.PowerAutomateDesktop"), null),
            (async () => await ProcessActions.RemoveAppxProvisioned("Uninstalling provisioned Microsoft.PowerAutomateDesktop_8wekyb3d8bbwe", "Microsoft.PowerAutomateDesktop"), null),
            (async () => await ProcessActions.RunNsudo("Deprovisioning Microsoft.PowerAutomateDesktop_8wekyb3d8bbwe", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Appx\AppxAllUserStore\Deprovisioned\Microsoft.PowerAutomateDesktop_8wekyb3d8bbwe"" /f"), null),

            // todos
            (async () => await ProcessActions.RemoveAppx("Uninstalling Microsoft.Todos_8wekyb3d8bbwe", "Microsoft.Todos"), null),
            (async () => await ProcessActions.RemoveAppxProvisioned("Uninstalling provisioned Microsoft.Todos_8wekyb3d8bbwe", "Microsoft.Todos"), null),
            (async () => await ProcessActions.RunNsudo("Deprovisioning Microsoft.Todos_8wekyb3d8bbwe", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Appx\AppxAllUserStore\Deprovisioned\Microsoft.Todos_8wekyb3d8bbwe"" /f"), null),

            // dev home
            (async () => await ProcessActions.RemoveAppx("Uninstalling Microsoft.Windows.DevHome_8wekyb3d8bbwe", "Microsoft.Windows.DevHome"), null),
            (async () => await ProcessActions.RemoveAppxProvisioned("Uninstalling provisioned Microsoft.Windows.DevHome_8wekyb3d8bbwe", "Microsoft.Windows.DevHome"), null),
            (async () => await ProcessActions.RunNsudo("Deprovisioning Microsoft.Windows.DevHome_8wekyb3d8bbwe", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Appx\AppxAllUserStore\Deprovisioned\Microsoft.Windows.DevHome_8wekyb3d8bbwe"" /f"), null),

            // alarms
            (async () => await ProcessActions.RemoveAppx("Uninstalling Microsoft.WindowsAlarms_8wekyb3d8bbwe", "Microsoft.WindowsAlarms"), null),
            (async () => await ProcessActions.RemoveAppxProvisioned("Uninstalling provisioned Microsoft.WindowsAlarms_8wekyb3d8bbwe", "Microsoft.WindowsAlarms"), null),
            (async () => await ProcessActions.RunNsudo("Deprovisioning Microsoft.WindowsAlarms_8wekyb3d8bbwe", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Appx\AppxAllUserStore\Deprovisioned\Microsoft.WindowsAlarms_8wekyb3d8bbwe"" /f"), null),

            // calculator
            (async () => await ProcessActions.RemoveAppx("Uninstalling Microsoft.WindowsCalculator_8wekyb3d8bbwe", "Microsoft.WindowsCalculator"), null),
            (async () => await ProcessActions.RemoveAppxProvisioned("Uninstalling provisioned Microsoft.WindowsCalculator_8wekyb3d8bbwe", "Microsoft.WindowsCalculator"), null),
            (async () => await ProcessActions.RunNsudo("Deprovisioning Microsoft.WindowsCalculator_8wekyb3d8bbwe", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Appx\AppxAllUserStore\Deprovisioned\Microsoft.WindowsCalculator_8wekyb3d8bbwe"" /f"), null),

            // camera
            (async () => await ProcessActions.RemoveAppx("Uninstalling Microsoft.WindowsCamera_8wekyb3d8bbwe", "Microsoft.WindowsCamera"), null),
            (async () => await ProcessActions.RemoveAppxProvisioned("Uninstalling provisioned Microsoft.WindowsCamera_8wekyb3d8bbwe", "Microsoft.WindowsCamera"), null),
            (async () => await ProcessActions.RunNsudo("Deprovisioning Microsoft.WindowsCamera_8wekyb3d8bbwe", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Appx\AppxAllUserStore\Deprovisioned\Microsoft.WindowsCamera_8wekyb3d8bbwe"" /f"), null),

            // communicationsapps
            (async () => await ProcessActions.RemoveAppx("Uninstalling microsoft.windowscommunicationsapps_8wekyb3d8bbwe", "microsoft.windowscommunicationsapps"), null),
            (async () => await ProcessActions.RemoveAppxProvisioned("Uninstalling provisioned microsoft.windowscommunicationsapps_8wekyb3d8bbwe", "microsoft.windowscommunicationsapps"), null),
            (async () => await ProcessActions.RunNsudo("Deprovisioning microsoft.windowscommunicationsapps_8wekyb3d8bbwe", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Appx\AppxAllUserStore\Deprovisioned\microsoft.windowscommunicationsapps_8wekyb3d8bbwe"" /f"), null),

            // feedback hub
            (async () => await ProcessActions.RemoveAppx("Uninstalling Microsoft.WindowsFeedbackHub_8wekyb3d8bbwe", "Microsoft.WindowsFeedbackHub"), null),
            (async () => await ProcessActions.RemoveAppxProvisioned("Uninstalling provisioned Microsoft.WindowsFeedbackHub_8wekyb3d8bbwe", "Microsoft.WindowsFeedbackHub"), null),
            (async () => await ProcessActions.RunNsudo("Deprovisioning Microsoft.WindowsFeedbackHub_8wekyb3d8bbwe", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Appx\AppxAllUserStore\Deprovisioned\Microsoft.WindowsFeedbackHub_8wekyb3d8bbwe"" /f"), null),

            // maps
            (async () => await ProcessActions.RemoveAppx("Uninstalling Microsoft.WindowsMaps_8wekyb3d8bbwe", "Microsoft.WindowsMaps"), null),
            (async () => await ProcessActions.RemoveAppxProvisioned("Uninstalling provisioned Microsoft.WindowsMaps_8wekyb3d8bbwe", "Microsoft.WindowsMaps"), null),
            (async () => await ProcessActions.RunNsudo("Deprovisioning Microsoft.WindowsMaps_8wekyb3d8bbwe", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Appx\AppxAllUserStore\Deprovisioned\Microsoft.WindowsMaps_8wekyb3d8bbwe"" /f"), null),

            // sound recorder
            (async () => await ProcessActions.RemoveAppx("Uninstalling Microsoft.WindowsSoundRecorder_8wekyb3d8bbwe", "Microsoft.WindowsSoundRecorder"), null),
            (async () => await ProcessActions.RemoveAppxProvisioned("Uninstalling provisioned Microsoft.WindowsSoundRecorder_8wekyb3d8bbwe", "Microsoft.WindowsSoundRecorder"), null),
            (async () => await ProcessActions.RunNsudo("Deprovisioning Microsoft.WindowsSoundRecorder_8wekyb3d8bbwe", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Appx\AppxAllUserStore\Deprovisioned\Microsoft.WindowsSoundRecorder_8wekyb3d8bbwe"" /f"), null),

            // terminal
            (async () => await ProcessActions.RemoveAppx("Uninstalling Microsoft.WindowsTerminal_8wekyb3d8bbwe", "Microsoft.WindowsTerminal"), null),
            (async () => await ProcessActions.RemoveAppxProvisioned("Uninstalling provisioned Microsoft.WindowsTerminal_8wekyb3d8bbwe", "Microsoft.WindowsTerminal"), null),
            (async () => await ProcessActions.RunNsudo("Deprovisioning Microsoft.WindowsTerminal_8wekyb3d8bbwe", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Appx\AppxAllUserStore\Deprovisioned\Microsoft.WindowsTerminal_8wekyb3d8bbwe"" /f"), null),

            // xbox speech to text overlay
            (async () => await ProcessActions.RemoveAppx("Uninstalling Microsoft.XboxSpeechToTextOverlay_8wekyb3d8bbwe", "Microsoft.XboxSpeechToTextOverlay"), null),
            (async () => await ProcessActions.RemoveAppxProvisioned("Uninstalling provisioned Microsoft.XboxSpeechToTextOverlay_8wekyb3d8bbwe", "Microsoft.XboxSpeechToTextOverlay"), null),
            (async () => await ProcessActions.RunNsudo("Deprovisioning Microsoft.XboxSpeechToTextOverlay_8wekyb3d8bbwe", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Appx\AppxAllUserStore\Deprovisioned\Microsoft.XboxSpeechToTextOverlay_8wekyb3d8bbwe"" /f"), null),

            // your phone
            (async () => await ProcessActions.RemoveAppx("Uninstalling Microsoft.YourPhone_8wekyb3d8bbwe", "Microsoft.YourPhone"), null),
            (async () => await ProcessActions.RemoveAppxProvisioned("Uninstalling provisioned Microsoft.YourPhone_8wekyb3d8bbwe", "Microsoft.YourPhone"), null),
            (async () => await ProcessActions.RunNsudo("Deprovisioning Microsoft.YourPhone_8wekyb3d8bbwe", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Appx\AppxAllUserStore\Deprovisioned\Microsoft.YourPhone_8wekyb3d8bbwe"" /f"), null),

            // zune music
            (async () => await ProcessActions.RemoveAppx("Uninstalling Microsoft.ZuneMusic_8wekyb3d8bbwe", "Microsoft.ZuneMusic"), null),
            (async () => await ProcessActions.RemoveAppxProvisioned("Uninstalling provisioned Microsoft.ZuneMusic_8wekyb3d8bbwe", "Microsoft.ZuneMusic"), null),
            (async () => await ProcessActions.RunNsudo("Deprovisioning Microsoft.ZuneMusic_8wekyb3d8bbwe", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Appx\AppxAllUserStore\Deprovisioned\Microsoft.ZuneMusic_8wekyb3d8bbwe"" /f"), null),

            // family
            (async () => await ProcessActions.RemoveAppx("Uninstalling MicrosoftCorporationII.MicrosoftFamily_8wekyb3d8bbwe", "MicrosoftCorporationII.MicrosoftFamily"), null),
            (async () => await ProcessActions.RemoveAppxProvisioned("Uninstalling provisioned MicrosoftCorporationII.MicrosoftFamily_8wekyb3d8bbwe", "MicrosoftCorporationII.MicrosoftFamily"), null),
            (async () => await ProcessActions.RunNsudo("Deprovisioning MicrosoftCorporationII.MicrosoftFamily_8wekyb3d8bbwe", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Appx\AppxAllUserStore\Deprovisioned\MicrosoftCorporationII.MicrosoftFamily_8wekyb3d8bbwe"" /f"), null),

            // quick assist
            (async () => await ProcessActions.RemoveAppx("Uninstalling MicrosoftCorporationII.QuickAssist_8wekyb3d8bbwe", "MicrosoftCorporationII.QuickAssist"), null),
            (async () => await ProcessActions.RemoveAppxProvisioned("Uninstalling provisioned MicrosoftCorporationII.QuickAssist_8wekyb3d8bbwe", "MicrosoftCorporationII.QuickAssist"), null),
            (async () => await ProcessActions.RunNsudo("Deprovisioning MicrosoftCorporationII.QuickAssist_8wekyb3d8bbwe", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Appx\AppxAllUserStore\Deprovisioned\MicrosoftCorporationII.QuickAssist_8wekyb3d8bbwe"" /f"), null),

            // teams  
            (async () => await ProcessActions.RemoveAppx("Uninstalling MicrosoftTeams_8wekyb3d8bbwe", "MicrosoftTeams"), null),
            (async () => await ProcessActions.RemoveAppxProvisioned("Uninstalling provisioned MicrosoftTeams_8wekyb3d8bbwe", "MicrosoftTeams"), null),
            (async () => await ProcessActions.RunNsudo("Deprovisioning MicrosoftTeams_8wekyb3d8bbwe", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Appx\AppxAllUserStore\Deprovisioned\MicrosoftTeams_8wekyb3d8bbwe"" /f"), null),

            // client web experience
            (async () => await ProcessActions.RemoveAppx("Uninstalling MicrosoftWindows.Client.WebExperience_cw5n1h2txyewy", "MicrosoftWindows.Client.WebExperience"), null),
            //(async () => await ProcessActions.RemoveAppxProvisioned("Uninstalling provisioned MicrosoftWindows.Client.WebExperience_cw5n1h2txyewy", "MicrosoftWindows.Client.WebExperience"), null),

            // remove all user deleted packages
            (async () => await ProcessActions.RunNsudo("Removing all user deleted packages", "CurrentUser", @"cmd /c rmdir /s /q ""C:\Program Files\WindowsApps\DeletedAllUserPackages"""), null),

            // update microsoft store
            (async () => await ProcessActions.UpdateAppx("Updating Microsoft.StorePurchaseApp_8wekyb3d8bbwe", "Microsoft.StorePurchaseApp_8wekyb3d8bbwe"), null),
            (async () => await ProcessActions.UpdateAppx("Updating Microsoft.WindowsStore_8wekyb3d8bbwe", "Microsoft.WindowsStore_8wekyb3d8bbwe"), null),
            (async () => await ProcessActions.UpdateAppx("Updating Microsoft.DesktopAppInstaller_8wekyb3d8bbwe", "Microsoft.DesktopAppInstaller_8wekyb3d8bbwe"), null),

            // update notepad
            (async () => await ProcessActions.UpdateAppx("Updating Microsoft.WindowsNotepad_8wekyb3d8bbwe", "Microsoft.WindowsNotepad_8wekyb3d8bbwe"), null),

            // update photos
            (async () => await ProcessActions.UpdateAppx("Updating Microsoft.Windows.Photos_8wekyb3d8bbwe", "Microsoft.Windows.Photos_8wekyb3d8bbwe"), null),

            // update movies & tv
            (async () => await ProcessActions.UpdateAppx("Updating Microsoft.ZuneVideo_8wekyb3d8bbwe", "Microsoft.ZuneVideo_8wekyb3d8bbwe"), null),

            // update snipping tool
            (async () => await ProcessActions.UpdateAppx("Updating Microsoft.ScreenSketch_8wekyb3d8bbwe", "Microsoft.ScreenSketch_8wekyb3d8bbwe"), null),

            // update xbox identity provider
            (async () => await ProcessActions.UpdateAppx("Updating Microsoft.XboxIdentityProvider_8wekyb3d8bbwe", "Microsoft.XboxIdentityProvider_8wekyb3d8bbwe"), null),
            (async () => await ProcessActions.UpdateAppx("Updating Microsoft.Xbox.TCUI_8wekyb3d8bbwe", "Microsoft.Xbox.TCUI_8wekyb3d8bbwe"), null),

            // update xbox app
            (async () => await ProcessActions.UpdateAppx("Updating Microsoft.GamingApp_8wekyb3d8bbwe", "Microsoft.GamingApp_8wekyb3d8bbwe"), null),

            // update xbox game overlay
            (async () => await ProcessActions.UpdateAppx("Updating Microsoft.XboxGameOverlay_8wekyb3d8bbwe", "Microsoft.XboxGameOverlay_8wekyb3d8bbwe"), null),
            (async () => await ProcessActions.UpdateAppx("Updating Microsoft.XboxGamingOverlay_8wekyb3d8bbwe", "Microsoft.XboxGamingOverlay_8wekyb3d8bbwe"), null),

            // update HEIF image extensions
            (async () => await ProcessActions.UpdateAppx("Updating Microsoft.HEIFImageExtension_8wekyb3d8bbwe", "Microsoft.HEIFImageExtension_8wekyb3d8bbwe"), null),

            // update VP9 video extensions
            (async () => await ProcessActions.UpdateAppx("Updating Microsoft.VP9VideoExtensions_8wekyb3d8bbwe", "Microsoft.VP9VideoExtensions_8wekyb3d8bbwe"), null),

            // update web media extensions
            (async () => await ProcessActions.UpdateAppx("Updating Microsoft.WebMediaExtensions_8wekyb3d8bbwe", "Microsoft.WebMediaExtensions_8wekyb3d8bbwe"), null),
                
            // update webp image extension
            (async () => await ProcessActions.UpdateAppx("Updating WebpImageExtension_8wekyb3d8bbwe", "Microsoft.WebpImageExtension_8wekyb3d8bbwe"), null),

            // update hevc video extension
            (async () => await ProcessActions.UpdateAppx("Updating Microsoft.HEVCVideoExtension_8wekyb3d8bbwe", "Microsoft.HEVCVideoExtension_8wekyb3d8bbwe"), null),

            // update raw image extension
            (async () => await ProcessActions.UpdateAppx("Updating Microsoft.RawImageExtension_8wekyb3d8bbwe", "Microsoft.RawImageExtension_8wekyb3d8bbwe"), null),
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
                    InstallPage.ProgressRingControl.Foreground = ProcessActions.GetColor("LightError", "DarkError");
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
