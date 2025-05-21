$features = @(
    "App.StepsRecorder~~~~0.0.1.0",
    "Browser.InternetExplorer~~~~0.0.11.0",
    "DirectX.Configuration.Database~~~~0.0.1.0",
    "Hello.Face.20134~~~~0.0.1.0",
    "MathRecognizer~~~~0.0.1.0",
    "Media.WindowsMediaPlayer~~~~0.0.12.0",
    "Microsoft.Wallpapers.Extended~~~~0.0.1.0",
    "Microsoft.Windows.PowerShell.ISE~~~~0.0.1.0",
    "Microsoft.Windows.WordPad~~~~0.0.1.0",
    "OneCoreUAP.OneSync~~~~0.0.1.0",
    "OpenSSH.Client~~~~0.0.1.0",
    "Print.Management.Console~~~~0.0.1.0",
    "VBSCRIPT~~~~",
    "Windows.Kernel.LA57~~~~0.0.1.0",
    "WMIC~~~~"
)

$totalFeatures = $features.Count
$processedFeatures = 0

foreach ($feature in $features) {
    try {
        Remove-WindowsCapability -Online -Name $feature -ErrorAction Stop *> $null
    } catch {
        Remove-WindowsCapability -Online -Name $feature -ErrorAction SilentlyContinue *> $null
    }

    $processedFeatures++
    $percent = [math]::Round(($processedFeatures / $totalFeatures) * 100)
    Write-Host $percent
}
