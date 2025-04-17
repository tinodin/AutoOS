using AutoOS.Views.Startup.Actions;
using Microsoft.UI.Xaml.Media;

namespace AutoOS.Views.Updater.Stages;

public static class UpdaterStage
{
    public static async Task Run()
    {
        string previousTitle = string.Empty;
        int stagePercentage = 100;

        string folderName = "";

        var actions = new List<(string Title, Func<Task> Action, Func<bool> Condition)>
        {
            // download windhawk
            ("Downloading Windhawk", async () => await StartupActions.RunDownload("https://www.dl.dropboxusercontent.com/scl/fi/noz9y3dr10fotn5h11c3r/Windhawk.zip?rlkey=bbfictfl61u7avj7fjjjirj88&st=sg31s8q8&dl=0", Path.GetTempPath(), "Windhawk.zip"), null),
        
            // install windhawk
            ("Installing Windhawk", async () => await StartupActions.RunExtract(Path.Combine(Path.GetTempPath(), "Windhawk.zip"), @"C:\Program Files\Windhawk"), null),
            ("Installing Windhawk", async () => await StartupActions.RunNsudo("TrustedInstaller", @"cmd /c move ""C:\Program Files\Windhawk\Windhawk"" ""%ProgramData%\Windhawk"""), null),
            ("Installing Windhawk", async () => await StartupActions.RunNsudo("CurrentUser", $"cmd /c reg import \"{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Scripts", "windhawk.reg")}\""), null),
            ("Installing Windhawk", async () => await StartupActions.RunNsudo("TrustedInstaller", @"sc create Windhawk binPath= ""\""C:\Program Files\Windhawk\windhawk.exe\"" -service"" start= auto"), null),
            ("Installing Windhawk", async () => await StartupActions.RunPowerShell(@"$s=New-Object -ComObject WScript.Shell;$sc=$s.CreateShortcut([IO.Path]::Combine($env:APPDATA,'Microsoft\Windows\Start Menu\Programs\Windhawk.lnk'));$sc.TargetPath='C:\Program Files\Windhawk\windhawk.exe';$sc.Save()"), null),
            ("Installing Windhawk", async () => await StartupActions.RunNsudo("TrustedInstaller", @"sc start Windhawk"), null),
        
            // disable services and drivers
            ("Disabling services and drivers", async () => await StartupActions.RunCustom(async () => folderName = await Task.Run(() => Directory.GetDirectories(Path.Combine(PathHelper.GetAppDataFolderPath(), "Service-list-builder", "build")).OrderByDescending(d => Directory.GetLastWriteTime(d)).FirstOrDefault()?.Split('\\').Last())), null),
            ("Disabling services and drivers", async () => await StartupActions.RunNsudo("TrustedInstaller", Path.Combine(PathHelper.GetAppDataFolderPath(), "Service-list-builder", "build", folderName, "Services-Disable.bat")), null),
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
                        StartupWindow.Status.Text = ex.Message;
                        StartupWindow.Progress.Foreground = (Brush)Application.Current.Resources["SystemFillColorCriticalBrush"];
                    }
                }

                StartupWindow.Progress.Value += incrementPerTitle;
                await Task.Delay(150);
                currentGroup.Clear();
            }

            StartupWindow.Status.Text = title + "...";
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
                    StartupWindow.Status.Text = ex.Message;
                    StartupWindow.Progress.Foreground = (Brush)Application.Current.Resources["SystemFillColorCriticalBrush"];
                }
            }
            StartupWindow.Progress.Value += incrementPerTitle;
        }

        StartupWindow.Status.Text = "Update finished.";
        StartupWindow.Progress.Foreground = new SolidColorBrush((Windows.UI.Color)Application.Current.Resources["SystemFillColorSuccess"]);

        await StartupActions.RunRestart();
    }
}