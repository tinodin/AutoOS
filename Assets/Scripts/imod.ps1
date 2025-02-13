param(
    [switch]$status,
    [switch]$save,
    [switch]$enable,
    [switch]$disable,
    [string]$rwePath
)

$globalInterval = 0x0
$globalHCSPARAMSOffset = 0x4
$globalRTSOFF = 0x18

function Dec-To-Hex($decimal) {
    $hexValue = $decimal.ToString("X2")
    return "0x$($hexValue)"
}

function Get-Value-From-Address($address) {
    $address = Dec-To-Hex -decimal ([uint64]$address)
    $stdout = & $rwePath /Min /NoLogo /Stdout /Command="R32 $($address)" | Out-String
    $splitString = $stdout -split " "
    return [uint64]$splitString[-1]
}

function Show-Interrupt-Status() {
    $deviceMap = Get-Device-Addresses
    $allDisabled = $true

    foreach ($xhciController in Get-WmiObject Win32_USBController) {
        $deviceId = $xhciController.DeviceID

        if (-not $deviceMap.Contains($deviceId)) {
            Write-Host "Error: Could not obtain base address for $deviceId"
            continue
        }

        $capabilityAddress = $deviceMap[$deviceId]
        $HCSPARAMSValue = Get-Value-From-Address -address ($capabilityAddress + $globalHCSPARAMSOffset)
        $HCSPARAMSBitmask = [Convert]::ToString($HCSPARAMSValue, 2)
        $maxIntrs = [Convert]::ToInt32($HCSPARAMSBitmask.Substring($HCSPARAMSBitmask.Length - 16, 8), 2)
        $RTSOFFValue = Get-Value-From-Address -address ($capabilityAddress + $globalRTSOFF)
        $runtimeAddress = $capabilityAddress + $RTSOFFValue

        for ($i = 0; $i -lt $maxIntrs; $i++) {
            $currentInterrupterAddress = $runtimeAddress + 0x24 + (0x20 * $i)
            $formattedInterrupterAddress = Dec-To-Hex -decimal $currentInterrupterAddress
            $defaultInterval = Get-Value-From-Address -address $currentInterrupterAddress
            $formattedDefaultInterval = Dec-To-Hex -decimal $defaultInterval

            Write-Host "`nInterrupter Address: $formattedInterrupterAddress, Current Interval: $formattedDefaultInterval"

            if ($defaultInterval -ne 0x0) {
                $allDisabled = $false
            }
        }
    }

    if ($null -eq $defaultInterval) {
        Write-Host "`nFAILED"
        return
    }
    elseif ($allDisabled) {
        Write-Host "`nDISABLED"
    } else {
        Write-Host "`nENABLED"
    }
}

function Save-Interrupt-Interval($interrupterAddress, $defaultInterval) {
    $regKey = "HKCU:\SOFTWARE\AutoOS\XHCI Interrupter Addresses"
    $regPath = "$regKey\$interrupterAddress"

    $allDisabled = $true

    $currentInterval = Get-Value-From-Address -address $interrupterAddress
    if ($currentInterval -ne 0x0) {
        $allDisabled = $false 
    }

    if ($allDisabled) {
        Write-Host "Interrupt Moderation is currently DISABLED. Enable Interrupt Moderation before saving."
        return
    }

    if (-not (Test-Path $regKey)) {
        New-Item -Path $regKey -Force
    }

    if (-not (Test-Path $regPath)) {
        New-Item -Path $regPath -Force
    }

    New-ItemProperty -Path $regPath -Name "DefaultInterval" -Value $defaultInterval -PropertyType DWord -Force

    $hexInterrupterAddress = [string]::Format("0x{0:X}", [int64]$interrupterAddress)
    $hexDefaultInterval = [string]::Format("0x{0:X}", $defaultInterval)
    Write-Host "Saved Interrupter Address: $hexInterrupterAddress, Interval: $hexDefaultInterval"
}



function Enable-Interrupt-Moderation() {
    $regKey = "HKCU:\SOFTWARE\AutoOS\XHCI Interrupter Addresses"

    if (-not (Test-Path $regKey)) {
        Write-Host "No saved interrupter intervals found."
        return
    }

    Get-ChildItem -Path $regKey | ForEach-Object {
        $address = $_.PSChildName
        $defaultInterval = Get-ItemProperty -Path $_.PSPath -Name "DefaultInterval" | Select-Object -ExpandProperty DefaultInterval

        $formattedInterval = [string]::Format("0x{0:X}", $defaultInterval)
        Write-Host "`nEnabling for Interrupter Address: $address, Interval: $formattedInterval"
        & $rwePath /Min /NoLogo /Stdout /Command="W32 $address $formattedInterval" | Write-Host
    }
}

function Disable-Interrupt-Moderation() {
    $deviceMap = Get-Device-Addresses

    foreach ($xhciController in Get-WmiObject Win32_USBController) {
        $deviceId = $xhciController.DeviceID

        if (-not $deviceMap.Contains($deviceId)) {
            Write-Host "Error: Could not obtain base address for $deviceId"
            continue
        }

        $capabilityAddress = $deviceMap[$deviceId]
        $HCSPARAMSValue = Get-Value-From-Address -address ($capabilityAddress + $globalHCSPARAMSOffset)
        $HCSPARAMSBitmask = [Convert]::ToString($HCSPARAMSValue, 2)
        $maxIntrs = [Convert]::ToInt32($HCSPARAMSBitmask.Substring($HCSPARAMSBitmask.Length - 16, 8), 2)
        $RTSOFFValue = Get-Value-From-Address -address ($capabilityAddress + $globalRTSOFF)
        $runtimeAddress = $capabilityAddress + $RTSOFFValue

        for ($i = 0; $i -lt $maxIntrs; $i++) {
            $currentInterrupterAddress = $runtimeAddress + 0x24 + (0x20 * $i)
            $formattedInterrupterAddress = Dec-To-Hex -decimal $currentInterrupterAddress

            $formattedInterval = [string]::Format("0x{0:X}", $globalInterval)
            Write-Host "`nDisabling for Interrupter Address: $formattedInterrupterAddress, Interval: $formattedInterval"
            & $rwePath /Min /NoLogo /Stdout /Command="W32 $formattedInterrupterAddress $formattedInterval" | Write-Host
        }
    }
}

function Get-Device-Addresses() {
    $data = @{ }
    $resources = Get-WmiObject -Class Win32_PNPAllocatedResource -ComputerName LocalHost -Namespace root\CIMV2

    foreach ($resource in $resources) {
        $deviceId = $resource.Dependent.Split("=")[1].Replace('"', '').Replace("\\", "\")
        $physicalAddress = $resource.Antecedent.Split("=")[1].Replace('"', '')

        if (-not $data.ContainsKey($deviceId) -and $deviceId -and $physicalAddress) {
            $data[$deviceId] = [uint64]$physicalAddress
        }
    }

    return $data
}

function main() {
    if ($status) {
        Show-Interrupt-Status
        return 0
    }

    if ($enable) {
        Enable-Interrupt-Moderation
        return 0
    }

    if ($disable) {
        Disable-Interrupt-Moderation
        return 0
    }

    if ($save) {
        $deviceMap = Get-Device-Addresses
        foreach ($xhciController in Get-WmiObject Win32_USBController) {
            $deviceId = $xhciController.DeviceID

            if (-not $deviceMap.Contains($deviceId)) {
                Write-Host "Error: Could not obtain base address for $deviceId"
                continue
            }

            $capabilityAddress = $deviceMap[$deviceId]
            $HCSPARAMSValue = Get-Value-From-Address -address ($capabilityAddress + $globalHCSPARAMSOffset)
            $HCSPARAMSBitmask = [Convert]::ToString($HCSPARAMSValue, 2)
            $maxIntrs = [Convert]::ToInt32($HCSPARAMSBitmask.Substring($HCSPARAMSBitmask.Length - 16, 8), 2)
            $RTSOFFValue = Get-Value-From-Address -address ($capabilityAddress + $globalRTSOFF)
            $runtimeAddress = $capabilityAddress + $RTSOFFValue

            for ($i = 0; $i -lt $maxIntrs; $i++) {
                $currentInterrupterAddress = $runtimeAddress + 0x24 + (0x20 * $i)
                $defaultInterval = Get-Value-From-Address -address $currentInterrupterAddress

                Save-Interrupt-Interval -interrupterAddress (Dec-To-Hex -decimal $currentInterrupterAddress) -defaultInterval $defaultInterval
            }
        }

        return 0
    }

    Write-Host "No valid parameter provided. Use -enable, -disable, -status, or -save."
    return 1
}

$_exitCode = main
Write-Host
exit $_exitCode
