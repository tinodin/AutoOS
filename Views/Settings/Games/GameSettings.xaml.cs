using Microsoft.UI.Xaml.Media;
using Microsoft.Win32;
using System.Diagnostics;

namespace AutoOS.Views.Settings.Games;

public sealed partial class GameSettings : Page
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
        DependencyProperty.Register("Title", typeof(string), typeof(GameSettings), new PropertyMetadata(null));

    public string Developers
    {
        get { return (string)GetValue(DevelopersProperty); }
        set { SetValue(DevelopersProperty, value); }
    }

    public static readonly DependencyProperty DevelopersProperty =
        DependencyProperty.Register("Developers", typeof(string), typeof(GamePanel), new PropertyMetadata(null));

    public string Description
    {
        get { return (string)GetValue(DescriptionProperty); }
        set { SetValue(DescriptionProperty, value); }
    }

    public static readonly DependencyProperty DescriptionProperty =
        DependencyProperty.Register("Description", typeof(string), typeof(GameSettings), new PropertyMetadata(null));

    public string InstallLocationDescription => $"Open the install location of {Title}";

    public static readonly DependencyProperty InstallLocationProperty =
    DependencyProperty.Register(nameof(InstallLocation), typeof(string), typeof(GameSettings), new PropertyMetadata(string.Empty));
    
    public ImageSource ImageWide { get; set; }
    public double Rating { get; set; }
    public string PlayTime { get; set; }
    public List<string> Genres { get; set; } = new();
    public List<string> Features { get; set; } = new();

    public string InstallLocation
    {
        get
        {
            var path = (string)GetValue(InstallLocationProperty);
            if (string.IsNullOrWhiteSpace(path))
                return null;

            if (File.Exists(path))
                return Path.GetDirectoryName(path);

            return path;
        }
        set => SetValue(InstallLocationProperty, value);
    }

    public Visibility IsFortnite => Title == "Fortnite" ? Visibility.Visible : Visibility.Collapsed;
    private void GetPresentationMode()
    {
        using (var key = Registry.CurrentUser.OpenSubKey(@"System\GameConfigStore\Children"))
        {
            foreach (var subKeyName in key.GetSubKeyNames())
            { 
                using (var subKey = key.OpenSubKey(subKeyName))
                {
                    if (subKey.GetValueNames().Any(valueName => subKey.GetValue(valueName) is string strValue && strValue.Contains("Fortnite")))
                    {
                        int flags = Convert.ToInt32(subKey.GetValue("Flags"));
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
                        if (PresentationMode.SelectedIndex == 0)
                        {
                            var process = new Process
                            {
                                StartInfo = new ProcessStartInfo
                                {
                                    FileName = "reg.exe",
                                    Arguments = $@"delete ""HKCU\System\GameConfigStore\Children\{subKeyName}"" /v Flags /f",
                                    CreateNoWindow = true,
                                }
                            };
                            process.Start();
                            process.WaitForExit();
                        }
                        else if (PresentationMode.SelectedIndex == 1)
                        {
                            var process = new Process
                            {
                                StartInfo = new ProcessStartInfo
                                {
                                    FileName = "reg.exe",
                                    Arguments = $@"add ""HKCU\System\GameConfigStore\Children\{subKeyName}"" /v Flags /t REG_DWORD /d 0x211 /f",
                                    CreateNoWindow = true,
                                }
                            };
                            process.Start();
                            process.WaitForExit();
                        }
                    }
                }
            }
        }
    }

    private void OpenInstallLocation_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(InstallLocation) && Directory.Exists(InstallLocation))
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"\"{InstallLocation}\"",
                UseShellExecute = true
            });
        }
    }
}