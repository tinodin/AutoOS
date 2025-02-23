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

        int validActionsCount = 0;
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

        var actions = new List<(Func<Task> Action, Func<bool> Condition)>
        {
            // check connection
            (async () => await ProcessActions.RunConnectionCheck(""), null),
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
