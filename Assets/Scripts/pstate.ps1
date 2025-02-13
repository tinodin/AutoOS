Get-WmiObject -Class Win32_VideoController | Where-Object { $_.PNPDeviceID -like "PCI\VEN_*" } | ForEach-Object {
    $pnpDeviceId = $_.PNPDeviceID
    $regPath = "HKLM:\SYSTEM\CurrentControlSet\Enum\$pnpDeviceId"
    $driver = (Get-ItemProperty -Path $regPath -Name "Driver" -ErrorAction SilentlyContinue).Driver
    $classKey = "HKLM:\SYSTEM\CurrentControlSet\Control\Class\$driver"
    $providerName = (Get-ItemProperty -Path $classKey -Name "ProviderName" -ErrorAction SilentlyContinue).ProviderName
    if ($providerName -eq "NVIDIA") {
        New-ItemProperty -Path $classKey -Name "DisableDynamicPstate" -Value 1 -Type DWord -Force
        New-ItemProperty -Path $classKey -Name "RMElcg" -Value 0x55555555 -Type DWord -Force
        New-ItemProperty -Path $classKey -Name "RMBlcg" -Value 0x11111111 -Type DWord -Force
        New-ItemProperty -Path $classKey -Name "RMElpg" -Value 0xFFF -Type DWord -Force
        New-ItemProperty -Path $classKey -Name "RMSlcg" -Value 0x3FFFF -Type DWord -Force
        New-ItemProperty -Path $classKey -Name "RMFspg" -Value 0xF -Type DWord -Force
        New-ItemProperty -Path $classKey -Name "RMDisableGpuASPMFlags" -Value 0x3 -Type DWord -Force
    }
}
