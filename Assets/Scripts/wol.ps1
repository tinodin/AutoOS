$status = $true

Get-NetAdapter | Where-Object { $_.PhysicalMediaType -eq "802.3" } | ForEach-Object {
    $adapterName = $_.Name
    $adapterProperties = Get-NetAdapterAdvancedProperty -Name $adapterName -ErrorAction SilentlyContinue

    # check wake on magic packet
    $wakeOnMagicPacket = $adapterProperties | Where-Object { $_.DisplayName -eq "Wake on Magic Packet" }
    if ($wakeOnMagicPacket -and $wakeOnMagicPacket.DisplayValue -ne "Enabled") {
        $status = $false
    }

    # check wake from s0ix on magic packet
    $wakeFromS0ix = $adapterProperties | Where-Object { $_.DisplayName -eq "Wake from S0ix on Magic Packet" }
    if ($wakeFromS0ix -and $wakeFromS0ix.DisplayValue -ne "Enabled") {
        $status = $false
    }

    # check wake on magic packet from s5
    $wakeFromS5 = $adapterProperties | Where-Object { $_.DisplayName -eq "Wake On Magic Packet From S5" }
    if ($wakeFromS5 -and $wakeFromS5.DisplayValue -ne "Enabled") {
        $status = $false
    }

    # check enable pme
    $enablePME = $adapterProperties | Where-Object { $_.DisplayName -eq "Enable PME" }
    if ($enablePME -and $enablePME.DisplayValue -ne "Enabled") {
        $status = $false
    }
}

# check allow the computer to turn off this device to save power
$deviceWakeEnable = Get-WmiObject -Namespace "root\wmi" -Class MSPower_DeviceEnable
Get-WmiObject -Class Win32_NetworkAdapter | Where-Object { $_.PNPDeviceID -like "PCI\VEN_*" } | ForEach-Object {
    $pnpDeviceId = $_.PNPDeviceID
    $regPath = "HKLM:\SYSTEM\CurrentControlSet\Enum\$pnpDeviceId"
    $driver = (Get-ItemProperty -Path $regPath -Name "Driver" -ErrorAction SilentlyContinue).Driver
    $classKey = "HKLM:\SYSTEM\CurrentControlSet\Control\Class\$driver"
    $physicalMediaType = (Get-ItemProperty -Path $classKey -Name "*PhysicalMediaType" -ErrorAction SilentlyContinue)."*PhysicalMediaType" 
    if ($physicalMediaType -eq "14") {
        $matchingWakeDevice = $deviceWakeEnable | Where-Object { $_.InstanceName -like "*$pnpDeviceId*" }
        if ($matchingWakeDevice) {
            foreach ($device in $matchingWakeDevice) {
                if (-not $device.Enable) {
                    $status = $false
                }
            }
        } else {
            $status = $false
        }
    }
}

# check allow this device to wake the computer
$deviceWakeEnable = Get-WmiObject -Namespace "root\wmi" -Class MSPower_DeviceWakeEnable
Get-WmiObject -Class Win32_NetworkAdapter | Where-Object { $_.PNPDeviceID -like "PCI\VEN_*" } | ForEach-Object {
    $pnpDeviceId = $_.PNPDeviceID
    $regPath = "HKLM:\SYSTEM\CurrentControlSet\Enum\$pnpDeviceId"
    $driver = (Get-ItemProperty -Path $regPath -Name "Driver" -ErrorAction SilentlyContinue).Driver
    $classKey = "HKLM:\SYSTEM\CurrentControlSet\Control\Class\$driver"
    $physicalMediaType = (Get-ItemProperty -Path $classKey -Name "*PhysicalMediaType" -ErrorAction SilentlyContinue)."*PhysicalMediaType" 
    if ($physicalMediaType -eq "14") {
        $matchingWakeDevice = $deviceWakeEnable | Where-Object { $_.InstanceName -like "*$pnpDeviceId*" }
        if ($matchingWakeDevice) {
            foreach ($device in $matchingWakeDevice) {
                if (-not $device.Enable) {
                    $status = $false
                }
            }
        } else {
            $status = $false
        }
    }
}

# check only allow a magic packet to wake the computer
$deviceWakeOnMagicPacketOnly = Get-WmiObject -Namespace "root\wmi" -Class MSNdis_DeviceWakeOnMagicPacketOnly
Get-WmiObject -Class Win32_NetworkAdapter | Where-Object { $_.PNPDeviceID -like "PCI\VEN_*" } | ForEach-Object {
    $pnpDeviceId = $_.PNPDeviceID
    $regPath = "HKLM:\SYSTEM\CurrentControlSet\Enum\$pnpDeviceId"
    $driver = (Get-ItemProperty -Path $regPath -Name "Driver" -ErrorAction SilentlyContinue).Driver
    $classKey = "HKLM:\SYSTEM\CurrentControlSet\Control\Class\$driver"
    $physicalMediaType = (Get-ItemProperty -Path $classKey -Name "*PhysicalMediaType" -ErrorAction SilentlyContinue)."*PhysicalMediaType" 
    if ($physicalMediaType -eq "14") {
        $matchingWakeDevice = $deviceWakeOnMagicPacketOnly | Where-Object { $_.InstanceName -like "*$pnpDeviceId*" }
        if ($matchingWakeDevice) {
            foreach ($device in $matchingWakeDevice) {
                if (-not $device.EnableWakeOnMagicPacketOnly) {
                    $status = $false
                }
            }
        } else {
            $status = $false
        }
    }
}

if ($status) {
    Write-Output "ENABLED"
} else {
    Write-Output "DISABLED"
}