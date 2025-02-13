Get-WmiObject -Class Win32_SoundDevice | Where-Object { $_.PNPDeviceID -like "HDAUDIO*" } | ForEach-Object {
    $pnpDeviceId = $_.PNPDeviceID
    $regPath = "HKLM:\SYSTEM\CurrentControlSet\Enum\$pnpDeviceId"
    $driver = (Get-ItemProperty -Path $regPath -Name "Driver" -ErrorAction SilentlyContinue).Driver
    $classKey = "HKLM:\SYSTEM\CurrentControlSet\Control\Class\$driver\PowerSettings"
    Set-ItemProperty -Path $classKey -Name "ConservationIdleTime" -Value ([byte[]](0,0,0,0))
    Set-ItemProperty -Path $classKey -Name "IdlePowerState" -Value ([byte[]](0,0,0,0))
    Set-ItemProperty -Path $classKey -Name "PerformanceIdleTime" -Value ([byte[]](0,0,0,0))
}
