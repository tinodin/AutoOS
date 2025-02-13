using Microsoft.Win32;
using System.Diagnostics;
using System.ServiceProcess;

namespace AutoOS.Views.Settings;

public sealed partial class UpdatePage : Page
{
    private string nsudoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "NSudo", "NSudoLC.exe");
    private bool isInitializingWindowsUpdateState = true;

    public UpdatePage()
    {
        InitializeComponent();
        GetWindowsUpdateState();
    }

    private void GetWindowsUpdateState()
    {
        // check registry
        if (Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate", "DisableOSUpgrade", null) == null)
        {
            WindowsUpdate.IsOn = true;
        }

        var serviceController = new ServiceController("wuauserv");
        bool isRunning = serviceController.Status == ServiceControllerStatus.Running;
        if (WindowsUpdate.IsOn && !isRunning || !WindowsUpdate.IsOn && isRunning)
        {
            // remove infobar
            WindowsUpdateInfo.Children.Clear();

            // add infobar
            var infoBar = new InfoBar
            {
                Title = WindowsUpdate.IsOn ? "Successfully enabled Windows Update. A restart is required to apply the change." : "Successfully disabled Windows Update. A restart is required to apply the change.",
                IsClosable = false,
                IsOpen = true,
                Severity = InfoBarSeverity.Success,
                Margin = new Thickness(5)
            };
            infoBar.ActionButton = new Button
            {
                Content = "Restart",
                HorizontalAlignment = HorizontalAlignment.Right
            };
            ((Button)infoBar.ActionButton).Click += (s, args) =>
            System.Diagnostics.Process.Start("shutdown", "/r /f /t 0");
            WindowsUpdateInfo.Children.Add(infoBar);
        }

        isInitializingWindowsUpdateState = false;
    }

    private async void Update_Toggled(object sender, RoutedEventArgs e)
    {
        if (isInitializingWindowsUpdateState) return;

        // remove infobar
        WindowsUpdateInfo.Children.Clear();

        // add infobar
        WindowsUpdateInfo.Children.Add(new InfoBar
        {
            Title = WindowsUpdate.IsOn ? "Enabling Windows Update..." : "Disabling Windows Update...",
            IsClosable = false,
            IsOpen = true,
            Severity = InfoBarSeverity.Informational,
            Margin = new Thickness(5)
        });

        // toggle windows defender
        if (WindowsUpdate.IsOn)
        {
            await Task.Run(() =>
            {
                Process.Start(new ProcessStartInfo { FileName = nsudoPath, Arguments = @"-U:T -P:E -Wait -ShowWindowMode:Hide reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate"" /v WUStatusServer /f", CreateNoWindow = true }).WaitForExit();
                Process.Start(new ProcessStartInfo { FileName = nsudoPath, Arguments = @"-U:T -P:E -Wait -ShowWindowMode:Hide reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate"" /v DisableOSUpgrade /f", CreateNoWindow = true }).WaitForExit();
                Process.Start(new ProcessStartInfo { FileName = nsudoPath, Arguments = @"-U:T -P:E -Wait -ShowWindowMode:Hide reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate"" /v SetDisableUXWUAccess /f", CreateNoWindow = true }).WaitForExit();
                Process.Start(new ProcessStartInfo { FileName = nsudoPath, Arguments = @"-U:T -P:E -Wait -ShowWindowMode:Hide reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU"" /v UseWUServer /f", CreateNoWindow = true }).WaitForExit();
                Process.Start(new ProcessStartInfo { FileName = nsudoPath, Arguments = @"-U:T -P:E -Wait -ShowWindowMode:Hide reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate\Auto Update"" /v AUOptions /f", CreateNoWindow = true }).WaitForExit();
                Process.Start(new ProcessStartInfo { FileName = nsudoPath, Arguments = @"-U:T -P:E -Wait -ShowWindowMode:Hide reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\UsoSvc"" /v Start /t REG_DWORD /d 2 /f", CreateNoWindow = true }).WaitForExit();
                Process.Start(new ProcessStartInfo { FileName = nsudoPath, Arguments = @"-U:T -P:E -Wait -ShowWindowMode:Hide reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\WaaSMedicSvc"" /v Start /t REG_DWORD /d 3 /f", CreateNoWindow = true }).WaitForExit();
                Process.Start(new ProcessStartInfo { FileName = nsudoPath, Arguments = @"-U:T -P:E -Wait -ShowWindowMode:Hide reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\wuauserv"" /v Start /t REG_DWORD /d 3 /f", CreateNoWindow = true }).WaitForExit();
                Process.Start(new ProcessStartInfo { FileName = nsudoPath, Arguments = @"-U:T -P:E -Wait -ShowWindowMode:Hide reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer"" /v SettingsPageVisibility /f", CreateNoWindow = true }).WaitForExit();
            });
        }
        else
        {
            await Task.Run(() =>
            {
                Process.Start(new ProcessStartInfo { FileName = nsudoPath, Arguments = @"-U:T -P:E -Wait -ShowWindowMode:Hide reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate"" /v WUStatusServer /t REG_SZ /d "" "" /f", CreateNoWindow = true }).WaitForExit();
                Process.Start(new ProcessStartInfo { FileName = nsudoPath, Arguments = @"-U:T -P:E -Wait -ShowWindowMode:Hide reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate"" /v DisableOSUpgrade /t REG_DWORD /d 1 /f", CreateNoWindow = true }).WaitForExit();
                Process.Start(new ProcessStartInfo { FileName = nsudoPath, Arguments = @"-U:T -P:E -Wait -ShowWindowMode:Hide reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate"" /v SetDisableUXWUAccess /t REG_DWORD /d 1 /f", CreateNoWindow = true }).WaitForExit();
                Process.Start(new ProcessStartInfo { FileName = nsudoPath, Arguments = @"-U:T -P:E -Wait -ShowWindowMode:Hide reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU"" /v UseWUServer /t REG_DWORD /d 1 /f", CreateNoWindow = true }).WaitForExit();
                Process.Start(new ProcessStartInfo { FileName = nsudoPath, Arguments = @"-U:T -P:E -Wait -ShowWindowMode:Hide reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate\Auto Update"" /v AUOptions /t REG_DWORD /d 1 /f", CreateNoWindow = true }).WaitForExit();
                Process.Start(new ProcessStartInfo { FileName = nsudoPath, Arguments = @"-U:T -P:E -Wait -ShowWindowMode:Hide reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\UsoSvc"" /v Start /t REG_DWORD /d 4 /f", CreateNoWindow = true }).WaitForExit();
                Process.Start(new ProcessStartInfo { FileName = nsudoPath, Arguments = @"-U:T -P:E -Wait -ShowWindowMode:Hide reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\WaaSMedicSvc"" /v Start /t REG_DWORD /d 4 /f", CreateNoWindow = true }).WaitForExit();
                Process.Start(new ProcessStartInfo { FileName = nsudoPath, Arguments = @"-U:T -P:E -Wait -ShowWindowMode:Hide reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\wuauserv"" /v Start /t REG_DWORD /d 4 /f", CreateNoWindow = true }).WaitForExit();
                Process.Start(new ProcessStartInfo { FileName = nsudoPath, Arguments = @"-U:T -P:E -Wait -ShowWindowMode:Hide reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer"" /v SettingsPageVisibility /t REG_SZ /d hide:windowsupdate /f", CreateNoWindow = true }).WaitForExit();
            });
        }

        // delay
        await Task.Delay(200);

        // remove infobar
        WindowsUpdateInfo.Children.Clear();

        // add infobar
        var infoBar = new InfoBar
        {
            Title = WindowsUpdate.IsOn ? "Successfully enabled Windows Update." : "Successfully disabled Windows Update.",
            IsClosable = false,
            IsOpen = true,
            Severity = InfoBarSeverity.Success,
            Margin = new Thickness(5)
        };
        WindowsUpdateInfo.Children.Add(infoBar);

        // add restart button
        var serviceController = new ServiceController("wuauserv");
        bool isRunning = serviceController.Status == ServiceControllerStatus.Running;

        if (WindowsUpdate.IsOn && !isRunning || !WindowsUpdate.IsOn && isRunning)
        {
            infoBar.Title += " A restart is required to apply the change.";
            infoBar.ActionButton = new Button
            {
                Content = "Restart",
                HorizontalAlignment = HorizontalAlignment.Right
            };
            ((Button)infoBar.ActionButton).Click += (s, args) =>
            System.Diagnostics.Process.Start("shutdown", "/r /f /t 0");
        }
        else
        {
            // delay
            await Task.Delay(2000);

            // remove infobar
            WindowsUpdateInfo.Children.Clear();
        }
    }
}

