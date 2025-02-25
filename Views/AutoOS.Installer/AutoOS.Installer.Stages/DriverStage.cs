using AutoOS.Views.Installer.Actions;

namespace AutoOS.Views.Installer.Stages;

public static class DriverStage
{
    public static async Task Run()
    {
        bool? Wifi = PreparingStage.Wifi;
        bool? Bluetooth = PreparingStage.Bluetooth;

        InstallPage.Status.Text = "Configuring Drivers...";

        InstallPage.Progress.ShowPaused = true;
        InstallPage.Info.Severity = InfoBarSeverity.Warning;
        InstallPage.ProgressRingControl.Foreground = ProcessActions.GetColor("LightPause", "DarkPause");

        string previousTitle = string.Empty;
        int stagePercentage = 2;

        // set title
        if ((bool)Wifi && (bool)Bluetooth)
        {
            InstallPage.Info.Title = "Install your Ethernet, Wi-Fi, Bluetooth and Audio driver, then connect to your internet.";
        }
        else if ((bool)Wifi && !(bool)Bluetooth)
        {
            InstallPage.Info.Title = "Install your Ethernet, Wi-Fi and Audio driver, then connect to your internet.";
        }
        else if ((bool)Bluetooth && !(bool)Wifi)
        {
            InstallPage.Info.Title = " Install your Ethernet, Bluetooth and Audio driver, then connect to your internet.";
        }
        else if (!(bool)Bluetooth && !(bool)Wifi)
        {
            InstallPage.Info.Title = "Install your Ethernet and Audio driver, then connect to your internet.";
        }

        var actions = new List<(string Title, Func<Task> Action, Func<bool> Condition)>
        {
            // check connection
            ("", async () => await ProcessActions.RunConnectionCheck(), null),
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
