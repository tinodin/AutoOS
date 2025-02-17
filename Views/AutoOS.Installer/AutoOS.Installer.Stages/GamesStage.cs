using AutoOS.Views.Installer.Actions;
using Microsoft.UI.Xaml.Media;
using Windows.UI;
using Microsoft.Win32;
using System.Diagnostics;

namespace AutoOS.Views.Installer.Stages;

public static class GamesStage
{
    public static async Task Run()
    {
        bool? Fortnite = PreparingStage.Fortnite;

        InstallPage.Status.Text = "Configuring Games...";

        int validActionsCount = 0;
        int stagePercentage = 2;

        var actions = new List<(Func<Task> Action, Func<bool> Condition)>
        {
            // download legendary
            (async () => await ProcessActions.RunDownload("Downloading Legendary", "https://github.com/derrod/legendary/releases/latest/download/legendary.exe", @"C:\Windows", "legendary.exe"), () => Fortnite == true),

            // log in to legendary
            (async () => await ProcessActions.Sleep("Please log in to your Epic Games account", 1000), () => Fortnite == true),
            (async () => await ProcessActions.RunCustom("Please log in to your Epic Games account", async () => await Task.Run(() => Process.Start(new ProcessStartInfo { FileName = @"C:\Windows\legendary.exe", Arguments = "auth", WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync())), () => Fortnite == true),

            // import fortnite
            (async () => await ProcessActions.RunCustom("Importing Fortnite", async () => await Task.Run(() => Process.Start(new ProcessStartInfo { FileName = @"C:\Windows\legendary.exe", Arguments = $"import --skip-dlcs Fortnite \"{Registry.GetValue(@"HKEY_CURRENT_USER\SOFTWARE\AutoOS", "GamePath", null)?.ToString()}\"", WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync())), () => Fortnite == true),

            // import fortnite settings
            (async () => await ProcessActions.RunNsudo("Importing Fortnite settings", "CurrentUser", @"cmd /c mkdir ""%LocalAppData%\FortniteGame\Saved\Config\WindowsClient"""), () => Fortnite == true),
            (async () => await ProcessActions.RunNsudo("Importing Fortnite settings", "CurrentUser", @"cmd /c copy /Y """ + Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Scripts", "GameUserSettings.ini") + @""" ""%LocalAppData%\FortniteGame\Saved\Config\WindowsClient\GameUserSettings.ini"""), () => Fortnite == true),
            
            // set refresh rate


            // set gpu preference to high performance for fortnite
            (async () => await ProcessActions.RunNsudo("Setting GPU Preference to High Performance for Fortnite", "CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Microsoft\DirectX\UserGpuPreferences"" /v """ + Registry.GetValue(@"HKEY_CURRENT_USER\SOFTWARE\AutoOS", "GamePath", null).ToString() + @"\FortniteGame\Binaries\Win64\FortniteClient-Win64-Shipping.exe"" /t REG_SZ /d ""SwapEffectUpgradeEnable=1;GpuPreference=2;"" /f"), () => Fortnite == true),

            // create fortnite qos policy
            (async () => await ProcessActions.RunNsudo("Creating Fortnite QoS Policy", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\Software\Policies\Microsoft\Windows\QoS\FortniteClient-Win64-Shipping.exe"" /v ""Application Name"" /t REG_SZ /d ""FortniteClient-Win64-Shipping.exe"" /f"), () => Fortnite == true),
            (async () => await ProcessActions.RunNsudo("Creating Fortnite QoS Policy", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\Software\Policies\Microsoft\Windows\QoS\FortniteClient-Win64-Shipping.exe"" /v ""Version"" /t REG_SZ /d ""1.0"" /f"), () => Fortnite == true),
            (async () => await ProcessActions.RunNsudo("CrCreatingeate Fortnite QoS Policy", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\Software\Policies\Microsoft\Windows\QoS\FortniteClient-Win64-Shipping.exe"" /v ""Protocol"" /t REG_SZ /d ""*"" /f"), () => Fortnite == true),
            (async () => await ProcessActions.RunNsudo("Creating Fortnite QoS Policy", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\Software\Policies\Microsoft\Windows\QoS\FortniteClient-Win64-Shipping.exe"" /v ""Local Port"" /t REG_SZ /d ""*"" /f"), () => Fortnite == true),
            (async () => await ProcessActions.RunNsudo("Creating Fortnite QoS Policy", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\Software\Policies\Microsoft\Windows\QoS\FortniteClient-Win64-Shipping.exe"" /v ""Local IP"" /t REG_SZ /d ""*"" /f"), () => Fortnite == true),
            (async () => await ProcessActions.RunNsudo("Creating Fortnite QoS Policy", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\Software\Policies\Microsoft\Windows\QoS\FortniteClient-Win64-Shipping.exe"" /v ""Local IP Prefix Length"" /t REG_SZ /d ""*"" /f"), () => Fortnite == true),
            (async () => await ProcessActions.RunNsudo("Creating Fortnite QoS Policy", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\Software\Policies\Microsoft\Windows\QoS\FortniteClient-Win64-Shipping.exe"" /v ""Remote Port"" /t REG_SZ /d ""*"" /f"), () => Fortnite == true),
            (async () => await ProcessActions.RunNsudo("Creating Fortnite QoS Policy", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\Software\Policies\Microsoft\Windows\QoS\FortniteClient-Win64-Shipping.exe"" /v ""Remote IP"" /t REG_SZ /d ""*"" /f"), () => Fortnite == true),
            (async () => await ProcessActions.RunNsudo("Creating Fortnite QoS Policy", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\Software\Policies\Microsoft\Windows\QoS\FortniteClient-Win64-Shipping.exe"" /v ""Remote IP Prefix Length"" /t REG_SZ /d ""*"" /f"), () => Fortnite == true),
            (async () => await ProcessActions.RunNsudo("Creating Fortnite QoS Policy", "TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\Software\Policies\Microsoft\Windows\QoS\FortniteClient-Win64-Shipping.exe"" /v ""DSCP Value"" /t REG_SZ /d ""46"" /f"), () => Fortnite == true),
            (async () => await ProcessActions.RunPowerShell("Creating Fortnite QoS Policy", @"New-NetQosPolicy -Name ""FortniteClient-Win64-Shipping.exe"" -AppPathNameMatchCondition ""FortniteClient-Win64-Shipping.exe"" -Precedence 127 -DSCPAction 46 -IPProtocol Both"), () => Fortnite == true),
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
