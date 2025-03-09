using Microsoft.Win32;
using System.Diagnostics;

namespace AutoOS.Views.Settings;

public sealed partial class GameSettings: Page
{
    private bool isInitializingPresentationMode = true;
    public GameSettings()
    {
        InitializeComponent();
        GetPresentationMode();
    }

    private void GetPresentationMode()
    {
        using (var key = Registry.CurrentUser.OpenSubKey(@"System\GameConfigStore\Children"))
        {
            foreach (var subKeyName in key.GetSubKeyNames())
            using (var subKey = key.OpenSubKey(subKeyName))
            {
                if (subKey.GetValueNames().Any(valueName => subKey.GetValue(valueName) is string strValue && strValue.Contains("Fortnite")))
                {
                    int flags = Convert.ToInt32(subKey.GetValue("Flags"));
                    Debug.WriteLine($"SubKey: {subKeyName}, Flags: {flags}");
                    if (flags == 0x211)
                    {
                        PresentationMode.SelectedIndex = 1;
                        isInitializingPresentationMode = false;
                        return;
                    }
                    else
                    {
                        PresentationMode.SelectedIndex = 0;
                        isInitializingPresentationMode = false;
                    }
                }
            }
        }
    }

    private void PresentationMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (isInitializingPresentationMode) return;

        using (var key = Registry.CurrentUser.OpenSubKey(@"System\GameConfigStore\Children", true))
        {
            foreach (var subKeyName in key.GetSubKeyNames())
            {
                using (var subKey = key.OpenSubKey(subKeyName, true))
                {
                    if (subKey.GetValueNames().Any(valueName => subKey.GetValue(valueName) is string strValue && strValue.Contains("Fortnite")))
                    {
                        string cmd = "";
                        if (PresentationMode.SelectedIndex == 0)
                        {
                            cmd = "reg delete \"HKCU\\System\\GameConfigStore\\Children\\" + subKeyName + "\" /v Flags /f";
                        }
                        else if (PresentationMode.SelectedIndex == 1)
                        {
                            cmd = "reg add \"HKCU\\System\\GameConfigStore\\Children\\" + subKeyName + "\" /v Flags /t REG_DWORD /d 0x211 /f";
                        }

                        if (!string.IsNullOrEmpty(cmd))
                        {
                            var process = new Process
                            {
                                StartInfo = new ProcessStartInfo
                                {
                                    FileName = "cmd.exe",
                                    Arguments = "/C " + cmd,
                                    CreateNoWindow = true,
                                }
                            };
                            process.Start();
                            process.WaitForExit();
                        }

                        return;
                    }
                }
            }
        }
    }
}