Get-WmiObject -Class Win32_VideoController | Where-Object { $_.PNPDeviceID -like "PCI\VEN_*" } | ForEach-Object {
    $pnpDeviceId = $_.PNPDeviceID
    $regPath = "HKLM:\SYSTEM\CurrentControlSet\Enum\$pnpDeviceId"
    $driver = (Get-ItemProperty -Path $regPath -Name "Driver" -ErrorAction SilentlyContinue).Driver
    $classKey = "HKLM:\SYSTEM\CurrentControlSet\Control\Class\$driver"
    $providerName = (Get-ItemProperty -Path $classKey -Name "ProviderName" -ErrorAction SilentlyContinue).ProviderName
    
    if ($providerName -eq "Intel Corporation") {
        
        New-ItemProperty -Path $classKey -Name "AdaptiveVsyncEnable" -Value 0 -Type DWord -Force
        New-ItemProperty -Path $classKey -Name "DelayedDetectionForDP" -Value 0 -Type DWord -Force
        New-ItemProperty -Path $classKey -Name "DelayedDetectionForHDMI" -Value 0 -Type DWord -Force
        New-ItemProperty -Path $classKey -Name "DcUserPreferredPolicy" -Value 0 -Type DWord -Force

        New-ItemProperty -Path $classKey -Name "PowerDpstAggressivenessLevel" -Value 0 -Type DWord -Force
        New-ItemProperty -Path $classKey -Name "PowerGpsAggressivenessLevel" -Value 0 -Type DWord -Force

        New-ItemProperty -Path $classKey -Name "Dpst6_3ApplyExtraDimming" -Value 0 -Type DWord -Force
        New-ItemProperty -Path $classKey -Name "FeatureTestControl" -Value 0x8200 -PropertyType DWord -Force

        New-ItemProperty -Path $classKey -Name "DcUserPreferredPolicy" -Value 0 -Type DWord -Force

        New-ItemProperty -Path $classKey -Name "AcUserPreferredPolicy" -Value 131072 -Type DWord -Force
        New-ItemProperty -Path $classKey -Name "PowerAcPolicy" -Value 3232416168 -PropertyType DWord -Force

        New-ItemProperty -Path $classKey -Name "PowerGpsAggressivenessLevel" -Value ([byte[]](0x02,0x01,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x02,0x01)) -Type Binary -Force
    }
}