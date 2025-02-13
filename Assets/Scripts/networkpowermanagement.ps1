# Disable allow the computer to turn off this device to save power
$deviceWakeEnable = Get-WmiObject -Namespace "root\wmi" -Class MSPower_DeviceEnable
Get-WmiObject -Class Win32_NetworkAdapter | Where-Object { $_.PNPDeviceID -like "PCI\VEN_*" } | ForEach-Object {
    $pnpDeviceId = $_.PNPDeviceID
    $regPath = "HKLM:\SYSTEM\CurrentControlSet\Enum\$pnpDeviceId"
    $driver = (Get-ItemProperty -Path $regPath -Name "Driver" -ErrorAction SilentlyContinue).Driver
    $classKey = "HKLM:\SYSTEM\CurrentControlSet\Control\Class\$driver"
    $physicalMediaType = (Get-ItemProperty -Path $classKey -Name "*PhysicalMediaType" -ErrorAction SilentlyContinue)."*PhysicalMediaType"
    if ($physicalMediaType -eq "14" -or $physicalMediaType -eq "9") {
        $matchingWakeDevice = $deviceWakeEnable | Where-Object { $_.InstanceName -like "*$pnpDeviceId*" -and $_.Enable -eq $true }
        foreach ($device in $matchingWakeDevice) {
            $device.Enable = $false
            $device.Put()
        }
    }
}


# Disable allow this device to wake the computer
$deviceWakeEnable = Get-WmiObject -Namespace "root\wmi" -Class MSPower_DeviceWakeEnable
Get-WmiObject -Class Win32_NetworkAdapter | Where-Object { $_.PNPDeviceID -like "PCI\VEN_*" } | ForEach-Object {
    $pnpDeviceId = $_.PNPDeviceID
    $regPath = "HKLM:\SYSTEM\CurrentControlSet\Enum\$pnpDeviceId"
    $driver = (Get-ItemProperty -Path $regPath -Name "Driver" -ErrorAction SilentlyContinue).Driver
    $classKey = "HKLM:\SYSTEM\CurrentControlSet\Control\Class\$driver"
    $physicalMediaType = (Get-ItemProperty -Path $classKey -Name "*PhysicalMediaType" -ErrorAction SilentlyContinue)."*PhysicalMediaType"
    if ($physicalMediaType -eq "14" -or $physicalMediaType -eq "9") {
        $matchingWakeDevice = $deviceWakeEnable | Where-Object { $_.InstanceName -like "*$pnpDeviceId*" -and $_.Enable -eq $true }
        foreach ($device in $matchingWakeDevice) {
            $device.Enable = $false
            $device.Put()
        }
    }
}

# Disable only allow a magic packet to wake the computer
$deviceWakeOnMagicPacketOnly = Get-WmiObject -Namespace "root\wmi" -Class MSNdis_DeviceWakeOnMagicPacketOnly
Get-WmiObject -Class Win32_NetworkAdapter | Where-Object { $_.PNPDeviceID -like "PCI\VEN_*" } | ForEach-Object {
    $pnpDeviceId = $_.PNPDeviceID
    $regPath = "HKLM:\SYSTEM\CurrentControlSet\Enum\$pnpDeviceId"
    $driver = (Get-ItemProperty -Path $regPath -Name "Driver" -ErrorAction SilentlyContinue).Driver
    $classKey = "HKLM:\SYSTEM\CurrentControlSet\Control\Class\$driver"
    $physicalMediaType = (Get-ItemProperty -Path $classKey -Name "*PhysicalMediaType" -ErrorAction SilentlyContinue)."*PhysicalMediaType"
    if ($physicalMediaType -eq "14" -or $physicalMediaType -eq "9") {
        $matchingWakeDevice = $deviceWakeOnMagicPacketOnly | Where-Object { $_.InstanceName -like "*$pnpDeviceId*" }
        if ($matchingWakeDevice) {
            foreach ($device in $matchingWakeDevice) {
                $device.EnableWakeOnMagicPacketOnly = $false
                $device.Put()
            }
        }
    }
}

# Disable PnP Capabilities
Get-WmiObject -Class Win32_NetworkAdapter | Where-Object { $_.PNPDeviceID -like "PCI\VEN_*" } | ForEach-Object {
    $pnpDeviceId = $_.PNPDeviceID
    $regPath = "HKLM:\SYSTEM\CurrentControlSet\Enum\$pnpDeviceId"
    $driver = (Get-ItemProperty -Path $regPath -Name "Driver" -ErrorAction SilentlyContinue).Driver
    $classKey = "HKLM:\SYSTEM\CurrentControlSet\Control\Class\$driver"
    $physicalMediaType = (Get-ItemProperty -Path $classKey -Name "*PhysicalMediaType" -ErrorAction SilentlyContinue)."*PhysicalMediaType"
    if ($physicalMediaType -eq "14" -or $physicalMediaType -eq "9") {
        Set-ItemProperty -Path $classKey -Name "PnPCapabilities" -Value 24 -Type DWord -Force
    }
}