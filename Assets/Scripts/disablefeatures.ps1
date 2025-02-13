$features = @(
    "Printing-PrintToPDFServices-Features",
    "MicrosoftWindowsPowerShellV2Root",
    "MicrosoftWindowsPowerShellV2",
    "WCF-Services45",
    "WCF-TCP-PortSharing45",
    "MediaPlayback",
    "WindowsMediaPlayer",
    "SearchEngine-Client-Package",
    "WorkFolders-Client",
    "Printing-Foundation-Features",
    "Printing-Foundation-InternetPrinting-Client",
    "SmbDirect",
    "MSRDC-Infrastructure"
)

$totalFeatures = $features.Count
$processedFeatures = 0

foreach ($feature in $features) {
    try {
        Disable-WindowsOptionalFeature -FeatureName $feature -Online -NoRestart -ErrorAction Stop *> $null
    } catch {

    }

    $processedFeatures++
    $percent = [math]::Round(($processedFeatures / $totalFeatures) * 100)
    Write-Host $percent
}
