Get-NetAdapter | Where-Object { $_.PhysicalMediaType -eq "802.3" } | ForEach-Object {
    $adapterName = $_.Name
    $adapterProperties = Get-NetAdapterAdvancedProperty -Name $adapterName

    # Wake on Magic Packet
    if ($adapterProperties | Where-Object { $_.DisplayName -eq "Wake on Magic Packet" }) {
        Set-NetAdapterAdvancedProperty -Name $adapterName -DisplayName "Wake on Magic Packet" -DisplayValue "Enabled"
    }

    # Wake from S0ix on Magic Packet
    if ($adapterProperties | Where-Object { $_.DisplayName -eq "Wake from S0ix on Magic Packet" }) {
        Set-NetAdapterAdvancedProperty -Name $adapterName -DisplayName "Wake from S0ix on Magic Packet" -DisplayValue "Enabled"
    }

    # Wake On Magic Packet From S5
    if ($adapterProperties | Where-Object { $_.DisplayName -eq "Wake On Magic Packet From S5" }) {
        Set-NetAdapterAdvancedProperty -Name $adapterName -DisplayName "Wake On Magic Packet From S5" -DisplayValue "Enabled"
    }

    # Enable PME
    if ($adapterProperties | Where-Object { $_.DisplayName -eq "Enable PME" }) {
        Set-NetAdapterAdvancedProperty -Name $adapterName -DisplayName "Enable PME" -DisplayValue "Enabled"
    }
}

# Enable allow the computer to turn off this device to save power
$deviceWakeEnable = Get-WmiObject -Namespace "root\wmi" -Class MSPower_DeviceEnable
Get-WmiObject -Class Win32_NetworkAdapter | Where-Object { $_.PNPDeviceID -like "PCI\VEN_*" } | ForEach-Object {
    $pnpDeviceId = $_.PNPDeviceID
    $regPath = "HKLM:\SYSTEM\CurrentControlSet\Enum\$pnpDeviceId"
    $driver = (Get-ItemProperty -Path $regPath -Name "Driver" -ErrorAction SilentlyContinue).Driver
    $classKey = "HKLM:\SYSTEM\CurrentControlSet\Control\Class\$driver"
    $physicalMediaType = (Get-ItemProperty -Path $classKey -Name "*PhysicalMediaType" -ErrorAction SilentlyContinue)."*PhysicalMediaType"
    if ($physicalMediaType -eq "14") {
        $matchingWakeDevice = $deviceWakeEnable | Where-Object { $_.InstanceName -like "*$pnpDeviceId*" -and $_.Enable -eq $false }
        foreach ($device in $matchingWakeDevice) {
            $device.Enable = $true
            $device.Put()
        }
    }
}

# Enable allow this device to wake the computer
$deviceWakeEnable = Get-WmiObject -Namespace "root\wmi" -Class MSPower_DeviceWakeEnable
Get-WmiObject -Class Win32_NetworkAdapter | Where-Object { $_.PNPDeviceID -like "PCI\VEN_*" } | ForEach-Object {
    $pnpDeviceId = $_.PNPDeviceID
    $regPath = "HKLM:\SYSTEM\CurrentControlSet\Enum\$pnpDeviceId"
    $driver = (Get-ItemProperty -Path $regPath -Name "Driver" -ErrorAction SilentlyContinue).Driver
    $classKey = "HKLM:\SYSTEM\CurrentControlSet\Control\Class\$driver"
    $physicalMediaType = (Get-ItemProperty -Path $classKey -Name "*PhysicalMediaType" -ErrorAction SilentlyContinue)."*PhysicalMediaType"
    if ($physicalMediaType -eq "14") {
        $matchingWakeDevice = $deviceWakeEnable | Where-Object { $_.InstanceName -like "*$pnpDeviceId*" -and $_.Enable -eq $false }
        foreach ($device in $matchingWakeDevice) {
            $device.Enable = $true
            $device.Put()
        }
    }
}

# Enable only allow a magic packet to wake the computer
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
                $device.EnableWakeOnMagicPacketOnly = $true
                $device.Put()
            }
        }
    }
}