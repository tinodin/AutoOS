$cpuCores = (Get-WmiObject -Class Win32_Processor | Measure-Object -Property NumberOfCores -Sum).Sum
$number = if ($cpuCores -ge 8) { "4" } elseif ($cpuCores -ge 4) { "2" }

netsh int tcp set global rss=enabled

Get-NetAdapter | Where-Object { $_.PhysicalMediaType -eq "802.3" } | ForEach-Object {
    $adapterName = $_.Name
    Get-NetAdapterAdvancedProperty -Name $adapterName -ErrorAction SilentlyContinue | ForEach-Object {
        # Maximum Number of RSS Queues
        if ($_.DisplayName -eq "Maximum Number of RSS Queues") {
            Set-NetAdapterAdvancedProperty -Name $adapterName -DisplayName "Maximum Number of RSS Queues" -DisplayValue ($number + " Queues") -ErrorAction SilentlyContinue
        }
    }
}

Get-WmiObject -Class Win32_NetworkAdapter | Where-Object { $_.PNPDeviceID -like "PCI\VEN_*" } | ForEach-Object {
    $pnpDeviceId = $_.PNPDeviceID
    $regPath = "HKLM:\SYSTEM\CurrentControlSet\Enum\$pnpDeviceId"
    $driver = (Get-ItemProperty -Path $regPath -Name "Driver" -ErrorAction SilentlyContinue).Driver

    $classKey = "HKLM:\SYSTEM\CurrentControlSet\Control\Class\$driver"
    $physicalMediaType = (Get-ItemProperty -Path $classKey -Name "*PhysicalMediaType" -ErrorAction SilentlyContinue)."*PhysicalMediaType"
    
    if ($physicalMediaType -eq "14") {
        New-ItemProperty -Path $classKey -Name "*RSS" -Value "1" -PropertyType String -Force
        New-ItemProperty -Path $classKey -Name "*RSSProfile" -Value "4" -PropertyType String -Force
        New-ItemProperty -Path $classKey -Name "*NumaNodeId" -Value "0" -PropertyType String -Force
        New-ItemProperty -Path $classKey -Name "*RssBaseProcNumber" -Value "2" -PropertyType String -Force
        New-ItemProperty -Path $classKey -Name "*NumRssQueues" -Value $number -PropertyType String -Force
        New-ItemProperty -Path $classKey -Name "*MaxRssProcessors" -Value $number -PropertyType String -Force
        New-ItemProperty -Path $classKey -Name "*RssMaxProcNumber" -Value (1 + [int]$number) -PropertyType String -Force
        New-ItemProperty -Path $classKey -Name "*RssBaseProcGroup" -Value "0" -PropertyType String -Force
        New-ItemProperty -Path $classKey -Name "*RssMaxProcGroup" -Value "0" -PropertyType String -Force
    }
}

Get-NetAdapter | Where-Object { $_.PhysicalMediaType -eq "802.3" } | ForEach-Object {
    Restart-NetAdapter -Name $_.Name
}
