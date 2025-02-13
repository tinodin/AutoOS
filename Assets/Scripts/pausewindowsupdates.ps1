$formatter = { param($date) $date.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ssK") }
$now = [datetime]::UtcNow
$start = $formatter.Invoke($now)
$end = $formatter.Invoke($now.AddYears(100))
Set-ItemProperty -LiteralPath HKLM:\SOFTWARE\Microsoft\WindowsUpdate\UX\Settings -Name 'PauseFeatureUpdatesStartTime' -Value $start -Type String -Force
Set-ItemProperty -LiteralPath HKLM:\SOFTWARE\Microsoft\WindowsUpdate\UX\Settings -Name 'PauseFeatureUpdatesEndTime' -Value $end -Type String -Force
Set-ItemProperty -LiteralPath HKLM:\SOFTWARE\Microsoft\WindowsUpdate\UX\Settings -Name 'PauseQualityUpdatesStartTime' -Value $start -Type String -Force
Set-ItemProperty -LiteralPath HKLM:\SOFTWARE\Microsoft\WindowsUpdate\UX\Settings -Name 'PauseQualityUpdatesEndTime' -Value $end -Type String -Force
Set-ItemProperty -LiteralPath HKLM:\SOFTWARE\Microsoft\WindowsUpdate\UX\Settings -Name 'PauseUpdatesStartTime' -Value $start -Type String -Force
Set-ItemProperty -LiteralPath HKLM:\SOFTWARE\Microsoft\WindowsUpdate\UX\Settings -Name 'PauseUpdatesExpiryTime' -Value $end -Type String -Force