# Disable allow the computer to turn off this device to save power
$adapters = Get-NetAdapter | Where-Object { $_.PhysicalMediaType -eq "802.3" -or $_.PhysicalMediaType -eq "Native 802.11" } | Get-NetAdapterPowerManagement
foreach ($adapter in $adapters) {
    $adapter.AllowComputerToTurnOffDevice = 'Disabled'
    $adapter | Set-NetAdapterPowerManagement
}

# Disable allow the computer to turn off this device to save power for hid devices including bluetooth
$deviceWakeEnable = Get-WmiObject -Namespace "root\wmi" -Class MSPower_DeviceEnable
Get-WmiObject -Class Win32_PnPEntity | Where-Object { $_.PNPDeviceID -like "USB*" } | ForEach-Object {
    $pnpDeviceId = $_.PNPDeviceID
    $matchingWakeDevice = $deviceWakeEnable | Where-Object { $_.InstanceName -like "*$pnpDeviceId*" -and $_.Enable -eq $true }
    foreach ($device in $matchingWakeDevice) {
            $device.Enable = $false
            $device.Put()
    }
}

# Disable allow this device to wake the computer for hid devices
$deviceWakeEnable = Get-WmiObject -Namespace "root\wmi" -Class MSPower_DeviceWakeEnable
Get-WmiObject -Class Win32_PnPEntity | Where-Object { $_.PNPDeviceID -like "HID*" } | ForEach-Object {
    $pnpDeviceId = $_.PNPDeviceID
    $matchingWakeDevice = $deviceWakeEnable | Where-Object { $_.InstanceName -like "*$pnpDeviceId*" -and $_.Enable -eq $true }
    foreach ($device in $matchingWakeDevice) {
            $device.Enable = $false
            $device.Put()
    }
}

# Disable allow the computer to turn off this device to save power for usb controllers
$deviceWakeEnable = Get-WmiObject -Namespace "root\wmi" -Class MSPower_DeviceEnable 
Get-WmiObject -Class Win32_USBController | Where-Object { $_.PNPDeviceID -like "PCI\VEN_*" } | ForEach-Object {
    $pnpDeviceId = $_.PNPDeviceID
    $matchingWakeDevice = $deviceWakeEnable | Where-Object { $_.InstanceName -like "*$pnpDeviceId*" -and $_.Enable -eq $true }
    foreach ($device in $matchingWakeDevice) {
            $device.Enable = $false
            $device.Put()
    }
}

# Disable allow the computer to turn off this device to save power for usb hubs
$deviceWakeEnable = Get-WmiObject -Namespace "root\wmi" -Class MSPower_DeviceEnable 
Get-WmiObject -Class Win32_USBHub | Where-Object { $_.PNPDeviceID -like "USB*" } | ForEach-Object {
    $pnpDeviceId = $_.PNPDeviceID
    $matchingWakeDevice = $deviceWakeEnable | Where-Object { $_.InstanceName -like "*$pnpDeviceId*" -and $_.Enable -eq $true }
    foreach ($device in $matchingWakeDevice) {
            $device.Enable = $false
            $device.Put()
    }
}