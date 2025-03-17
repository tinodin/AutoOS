using AutoOS.Views.Installer.Actions;
using Microsoft.UI.Xaml.Media;

namespace AutoOS.Views.Installer.Stages;

public static class VisualStage
{
    public static async Task Run()
    {
        bool? LegacyContextMenu = PreparingStage.LegacyContextMenu;
        bool? ShowMyTaskbarOnAllDisplays = PreparingStage.ShowMyTaskbarOnAllDisplays;
        bool? AlwaysShowTrayIcons = PreparingStage.AlwaysShowTrayIcons;
        bool? TaskbarAlignment = PreparingStage.TaskbarAlignment;

        InstallPage.Status.Text = "Configuring Visuals...";

        string previousTitle = string.Empty;
        int stagePercentage = 2;

        var actions = new List<(string Title, Func<Task> Action, Func<bool> Condition)>
        {
            // configure visual effects
            ("Configuring visual effects", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects"" /v ""VisualFXSetting"" /t REG_DWORD /d 3 /f"), null),
            ("Configuring visual effects", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Control Panel\Desktop"" /v ""UserPreferencesMask"" /t REG_BINARY /d 9E3E078012000000 /f"), null),

            // restore legacy context menu
            ("Restoring legacy context menu", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}\InprocServer32"" /ve /t REG_SZ /d """" /f"), () => LegacyContextMenu == true),

            // only show my taskbar on the main display
            ("Show the taskbar only on the main display", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced"" /v ""MMTaskbarEnabled"" /t REG_DWORD /d 0 /f"), () => ShowMyTaskbarOnAllDisplays == false),

            // show all tray icons 
            ("Showing all tray icons", async () => await ProcessActions.RunPowerShell(@"Set-ItemProperty -Path 'HKCU:\Control Panel\NotifyIconSettings\*' -Name 'IsPromoted' -Value 1"), () => AlwaysShowTrayIcons == true),

            // aligning the taskbar
            ("Aligning the taskbar to the left", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced"" /v ""TaskbarAl"" /t REG_DWORD /d 0 /f"), () => TaskbarAlignment == true),
            
            // unpin copilot, microsoft edge and microsoft store
            ("Unpinning Copilot, Microsoft Edge, and Microsoft Store", async () => await ProcessActions.RunPowerShell(@"function DoUnpin([string]$appname) { $ErrorActionPreference = 'silentlycontinue'; ((New-Object -Com Shell.Application).NameSpace('shell:::{4234d49b-0245-4df3-b780-3893943456e1}').Items() | Where-Object { $_.Name -eq $appname }).Verbs() | Where-Object { $_.Name.replace('&', '') -match 'Unpin from taskbar' } | ForEach-Object { $_.DoIt(); }; $ErrorActionPreference = 'continue' }; DoUnpin 'Copilot'; DoUnpin 'Microsoft Edge'; DoUnpin 'Microsoft Store'"), null),

            // remove microsoft edge shortcut from the desktop
            ("Removing Microsoft Edge shortcut from the desktop", async () => await ProcessActions.RunNsudo("CurrentUser", @"cmd /c del /f /q ""C:\Users\Public\Desktop\Microsoft Edge.lnk"""), null),

            // changing the search box
            ("Hiding the searchbox", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Search"" /v ""SearchboxTaskbarMode"" /t REG_DWORD /d 0 /f"), null),

            // enable the settings shortcut in the start menu
            ("Enabling the settings shortcut in the start menu", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Start"" /v ""VisiblePlaces"" /t REG_BINARY /d 86087352aa5143429f7b2776584659d4 /f"), null),

            // hide the task view button
            ("Hiding the task view button", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced"" /v ""ShowTaskViewButton"" /t REG_DWORD /d 0 /f"), null),

            // hide widgets button
            ("Hiding the widgets button", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced"" /v ""TaskbarDa"" /t REG_DWORD /d 0 /f"), null),

            // hide the chat button
            ("Hiding the chat button", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced"" /v ""TaskbarMn"" /t REG_DWORD /d 0 /f"), null),

            // hide the bluetooth tray icon
            ("Hiding the bluetooth tray icon", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Control Panel\Bluetooth"" /v ""Notification Area Icon"" /t REG_DWORD /d 0 /f"), null),

            // hide the security and maintenance tray icon
            ("Hiding the security and maintenance tray icon", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Policies\Microsoft\Windows\Explorer"" /v ""HideSCAHealth"" /t REG_DWORD /d 1 /f"), null),

            // hide the windows update tray icon
            ("Hiding the windows update tray icon", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\WindowsUpdate\UX\Settings"" /v ""TrayIconVisibility"" /t REG_DWORD /d 0 /f"), null),

            // enable the end task option
            ("Enabling the end task option", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced\TaskbarDeveloperSettings"" /v ""TaskbarEndTask"" /t REG_DWORD /d 1 /f"), null),
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
                    InstallPage.Info.Title = InstallPage.Info.Title + ": " + ex.Message;
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
