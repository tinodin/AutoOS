using AutoOS.Views.Installer.Actions;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace AutoOS.Views.Installer.Stages;

public static class VisualStage
{
    public static async Task Run()
    {
        bool? LegacyContextMenu = PreparingStage.LegacyContextMenu;
        bool? ShowMyTaskbarOnAllDisplays = PreparingStage.ShowMyTaskbarOnAllDisplays;
        bool? AlwaysShowTrayIcons = PreparingStage.AlwaysShowTrayIcons;
        bool? TaskbarAlignment = PreparingStage.TaskbarAlignment;
        bool? StartAllBack = PreparingStage.StartAllBack;

        InstallPage.Status.Text = "Configuring Visuals...";

        int validActionsCount = 0;
        int stagePercentage = 2;

        var actions = new List<(Func<Task> Action, Func<bool> Condition)>
        {
            // configure visual effects
            (async () => await ProcessActions.RunNsudo("Configuring visual effects", "CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects"" /v ""VisualFXSetting"" /t REG_DWORD /d 3 /f"), null),
            (async () => await ProcessActions.RunNsudo("Configuring visual effects", "CurrentUser", @"reg add ""HKEY_CURRENT_USER\Control Panel\Desktop"" /v ""UserPreferencesMask"" /t REG_BINARY /d 9E3E078012000000 /f"), null),

            // restore legacy context menu
            (async () => await ProcessActions.RunNsudo("Restoring legacy context menu", "CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}\InprocServer32"" /ve /t REG_SZ /d """" /f"), () => LegacyContextMenu == true),

            // only show my taskbar on the main display
            (async () => await ProcessActions.RunNsudo("Show the taskbar only on the main display", "CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced"" /v ""MMTaskbarEnabled"" /t REG_DWORD /d 0 /f"),() => ShowMyTaskbarOnAllDisplays == false),

            // show all tray icons 
            (async () => await ProcessActions.RunPowerShell("Showing all tray icons", @"Set-ItemProperty -Path 'HKCU:\Control Panel\NotifyIconSettings\*' -Name 'IsPromoted' -Value 1"),() => AlwaysShowTrayIcons == true),

            // aligning the taskbar
            (async () => await ProcessActions.RunNsudo("Aligning the taskbar to the left", "CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced"" /v ""TaskbarAl"" /t REG_DWORD /d 0 /f"),() => TaskbarAlignment == true),

            // unpin copilot, microsoft edge and microsoft store
            (async () => await ProcessActions.RunPowerShell("Unpinning Copilot, Microsoft Edge, and Microsoft Store", @"function DoUnpin([string]$appname) { $ErrorActionPreference = 'silentlycontinue'; ((New-Object -Com Shell.Application).NameSpace('shell:::{4234d49b-0245-4df3-b780-3893943456e1}').Items() | Where-Object { $_.Name -eq $appname }).Verbs() | Where-Object { $_.Name.replace('&', '') -match 'Unpin from taskbar' } | ForEach-Object { $_.DoIt(); }; $ErrorActionPreference = 'continue' }; DoUnpin 'Copilot'; DoUnpin 'Microsoft Edge'; DoUnpin 'Microsoft Store'"), null),

            // remove microsoft edge shortcut from the desktop
            (async () => await ProcessActions.RunNsudo("Removing Microsoft Edge shortcut from the desktop", "CurrentUser", @"cmd /c del /f /q ""C:\Users\Public\Desktop\Microsoft Edge.lnk"""), null),

            // changing the search box
            (async () => await ProcessActions.RunNsudo("Hiding the searchbox", "CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Search"" /v ""SearchboxTaskbarMode"" /t REG_DWORD /d 0 /f"),() => StartAllBack == true),
            (async () => await ProcessActions.RunNsudo("Changing the searchbox appearance", "CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Search"" /v ""SearchboxTaskbarMode"" /t REG_DWORD /d 3 /f"),() => StartAllBack == false),

            // enable the settings shortcut in the start menu
            (async () => await ProcessActions.RunNsudo("Enabling the settings shortcut in the start menu", "CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Start"" /v ""VisiblePlaces"" /t REG_BINARY /d 86087352aa5143429f7b2776584659d4 /f"), null),

            // hide the task view button
            (async () => await ProcessActions.RunNsudo("Hiding the task view button", "CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced"" /v ""ShowTaskViewButton"" /t REG_DWORD /d 0 /f"), null),

            // hide widgets button
            (async () => await ProcessActions.RunNsudo("Hiding the widgets button", "CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced"" /v ""TaskbarDa"" /t REG_DWORD /d 0 /f"), null),

            // hide the chat button
            (async () => await ProcessActions.RunNsudo("Hiding the chat button", "CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced"" /v ""TaskbarMn"" /t REG_DWORD /d 0 /f"), null),

            // hide the bluetooth tray icon
            (async () => await ProcessActions.RunNsudo("Hiding the bluetooth tray icon", "CurrentUser", @"reg add ""HKEY_CURRENT_USER\Control Panel\Bluetooth"" /v ""Notification Area Icon"" /t REG_DWORD /d 0 /f"), null),

            // hide the security and maintenance tray icon
            (async () => await ProcessActions.RunNsudo("Hiding the security and maintenance tray icon", "CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Policies\Microsoft\Windows\Explorer"" /v ""HideSCAHealth"" /t REG_DWORD /d 1 /f"), null),

            // hide the windows update tray icon
            (async () => await ProcessActions.RunNsudo("Hiding the windows update tray icon", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\WindowsUpdate\UX\Settings"" /v ""TrayIconVisibility"" /t REG_DWORD /d 0 /f"), null),

            // enable the end task option
            (async () => await ProcessActions.RunNsudo("Enabling the end task option", "CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced\TaskbarDeveloperSettings"" /v ""TaskbarEndTask"" /t REG_DWORD /d 1 /f"), null),
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
                    

                    InstallPage.ProgressRingControl.Foreground = (SolidColorBrush)Application.Current.Resources["LightRed"];
                    InstallPage.ProgressRingControl.Foreground = (SolidColorBrush)Application.Current.Resources["DarkRed"];
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
