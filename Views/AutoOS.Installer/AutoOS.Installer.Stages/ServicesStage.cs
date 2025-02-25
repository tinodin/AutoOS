using AutoOS.Views.Installer.Actions;

namespace AutoOS.Views.Installer.Stages;

public static class ServicesStage
{
    public static async Task Run()
    {
        InstallPage.Status.Text = "Configuring Services and Drivers...";

        string previousTitle = string.Empty;
        int stagePercentage = 2;

        string folderName = "";

        var actions = new List<(string Title, Func<Task> Action, Func<bool> Condition)>
        {
            // group services
            ("Grouping services", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"powershell.exe -file ""{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Scripts", "groupservices.ps1")}"""), null),  

            // set failure actions
            ("Disabling failure actions", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\SoftwareProtectionPlatform"" /v ""InactivityShutdownDelay"" /t REG_DWORD /d 4294967295 /f"), null),
            ("Disabling failure actions", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Appinfo"" /v ""FailureActions"" /t REG_BINARY /d 00000000000000000000000003000000010000000000000001000000000000000000000000000000 /f"), null),
            ("Disabling failure actions", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\AppXSvc"" /v ""FailureActions"" /t REG_BINARY /d 00000000000000000000000003000000010000000000000001000000000000000000000000000000 /f"), null),
            ("Disabling failure actions", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\CryptSvc"" /v ""FailureActions"" /t REG_BINARY /d 00000000000000000000000003000000010000000000000001000000000000000000000000000000 /f"), null),
            ("Disabling failure actions", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\netprofm"" /v ""FailureActions"" /t REG_BINARY /d 00000000000000000000000003000000010000000000000001000000000000000000000000000000 /f"), null),
            ("Disabling failure actions", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\nsi"" /v ""FailureActions"" /t REG_BINARY /d 00000000000000000000000003000000010000000000000001000000000000000000000000000000 /f"), null),
            ("Disabling failure actions", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\ProfSvc"" /v ""FailureActions"" /t REG_BINARY /d 00000000000000000000000003000000010000000000000001000000000000000000000000000000 /f"), null),
            ("Disabling failure actions", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\sppsvc"" /v ""FailureActions"" /t REG_BINARY /d 00000000000000000000000003000000010000000000000001000000000000000000000000000000 /f"), null),
            ("Disabling failure actions", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\StateRepository"" /v ""FailureActions"" /t REG_BINARY /d 00000000000000000000000003000000010000000000000001000000000000000000000000000000 /f"), null),
            ("Disabling failure actions", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\TextInputManagementService"" /v ""FailureActions"" /t REG_BINARY /d 00000000000000000000000003000000010000000000000001000000000000000000000000000000 /f"), null),
            ("Disabling failure actions", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Winmgmt"" /v ""FailureActions"" /t REG_BINARY /d 00000000000000000000000003000000010000000000000001000000000000000000000000000000 /f"), null),

            // build service lists
            ("Building service lists", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"""{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "Service-list-builder", "service-list-builder.exe")}"" --config ""{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "Service-list-builder", "lists.ini")}"" --disable-service-warning"), null),
            ("Building service lists", async () => await ProcessActions.RunCustom(async () => folderName = await Task.Run(() => Directory.GetDirectories(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "Service-list-builder", "build")).OrderByDescending(d => Directory.GetLastWriteTime(d)).FirstOrDefault()?.Split('\\').Last())), null),

            // disable services and drivers
            ("Disabling services and drivers", async () => await ProcessActions.RunNsudo("TrustedInstaller", Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "Service-list-builder", "build", folderName, "Services-Disable.bat")), null),
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
