Get-WmiObject -Class Win32_NetworkAdapter | Where-Object { $_.PNPDeviceID -like "PCI\VEN_*" } | ForEach-Object {
    $pnpDeviceId = $_.PNPDeviceID
    $regPath = "HKLM:\SYSTEM\CurrentControlSet\Enum\$pnpDeviceId"
    $driver = (Get-ItemProperty -Path $regPath -Name "Driver" -ErrorAction SilentlyContinue).Driver

    $classKey = "HKLM:\SYSTEM\CurrentControlSet\Control\Class\$driver"
    $physicalMediaType = (Get-ItemProperty -Path $classKey -Name "*PhysicalMediaType" -ErrorAction SilentlyContinue)."*PhysicalMediaType"
    
    if ($physicalMediaType -eq "14") {
        Set-ItemProperty -Path $classKey -Name "TxIntDelay" -Value 0 -Force
    }
}