using AutoOS.Views.Installer.Actions;

namespace AutoOS.Views.Installer.Stages;

public static class DriverStage
{
    public static async Task Run()
    {
        bool? Wifi = PreparingStage.Wifi;
        bool? Bluetooth = PreparingStage.Bluetooth;

        InstallPage.Status.Text = "Configuring Drivers...";

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

        InstallPage.Info.Severity = InfoBarSeverity.Informational;
        InstallPage.Progress.Foreground = ProcessActions.GetColor("LightNormal", "DarkNormal");
        InstallPage.ProgressRingControl.Foreground = null;
    }
}
