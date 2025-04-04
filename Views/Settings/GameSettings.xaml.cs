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

    public string Title
    {
        get { return (string)GetValue(TitleProperty); }
        set { SetValue(TitleProperty, value); }
    }

    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register("Title", typeof(string), typeof(HeaderTile), new PropertyMetadata(null));

    public string InstallLocationDescription => $"Change the install location of {Title}";

    public static readonly DependencyProperty InstallLocationProperty =
    DependencyProperty.Register(nameof(InstallLocation), typeof(string), typeof(GameSettings), new PropertyMetadata(string.Empty));

    public string InstallLocation
    {
        get => (string)GetValue(InstallLocationProperty);
        set => SetValue(InstallLocationProperty, value);
    }

    public Visibility IsFortnite => Title == "Fortnite" ? Visibility.Visible : Visibility.Collapsed;
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