$registryPath = "HKLM:\SYSTEM\ControlSet001\Services\"
$services = @(
    "DisplayEnhancementService",
    "PcaSvc",
    "WdiSystemHost",
    "AudioEndpointBuilder",
    "DeviceAssociationService",
    "NcbService",
    "StorSvc",
    "SysMain",
    "TrkWks",
    "hidserv",
    "BITS",
    "LanmanServer",
    "SENS",
    "Schedule",
    "ShellHWDetection",
    "Themes",
    "TokenBroker",
    "UsoSvc",
    "WpnService",
    "iphlpsvc",
    "wuauserv",
    "WinHttpAutoProxySvc",
    "EventLog",
    "TimeBrokerSvc",
    "lmhosts",
    "Dhcp",
    "FontCache",
    "nsi",
    "SstpSvc",
    "DispBrokerDesktopSvc",
    "CDPSvc",
    "EventSystem",
    "LicenseManager",
    "SystemEventsBroker",
    "Power",
    "LSM",
    "DcomLaunch",
    "BrokerInfrastructure",
    "CoreMessagingRegistrar",
    "DPS",
    "AppXSvc",
    "ClipSVC"
)

foreach ($service in $services) {
    New-ItemProperty -Path "$registryPath\$service" -Name "SvcHostSplitDisable" -Value 1 -PropertyType DWord -Force
}

$userServices = @(
    "CDPUserSvc_*",
    "OneSyncSvc_*",
    "WpnUserService_*"
)

foreach ($service in $userServices) {
    $matchingServices = Get-Service | Where-Object { $_.Name -like $service }

    foreach ($matchingService in $matchingServices) {
		New-ItemProperty -Path "$registryPath\$($matchingService.Name)" -Name "SvcHostSplitDisable" -Value 1 -PropertyType  DWord -Force
    }
}