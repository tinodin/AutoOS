Get-NetAdapter | Where-Object { $_.PhysicalMediaType -eq "Native 802.11" } | ForEach-Object {
    $adapterName = $_.Name
    Get-NetAdapterAdvancedProperty -Name $adapterName | ForEach-Object {
        # Sleep on WoWLAN Disconnect
        if ($_.DisplayName -eq "Sleep on WoWLAN Disconnect") {
            Set-NetAdapterAdvancedProperty -Name $adapterName -DisplayName "Sleep on WoWLAN Disconnect" -DisplayValue "Disabled"
        }

        # Packet Coalescing
        if ($_.DisplayName -eq "Packet Coalescing") {
            Set-NetAdapterAdvancedProperty -Name $adapterName -DisplayName "Packet Coalescing" -DisplayValue "Disabled"
        }

        # ARP offload for WoWLAN
        if ($_.DisplayName -eq "ARP offload for WoWLAN") {
            Set-NetAdapterAdvancedProperty -Name $adapterName -DisplayName "ARP offload for WoWLAN" -DisplayValue "Disabled"
        }

        # NS offload for WoWLAN
        if ($_.DisplayName -eq "NS offload for WoWLAN") {
            Set-NetAdapterAdvancedProperty -Name $adapterName -DisplayName "NS offload for WoWLAN" -DisplayValue "Disabled"
        }

        # GTK rekeying for WoWLAN
        if ($_.DisplayName -eq "GTK rekeying for WoWLAN") {
            Set-NetAdapterAdvancedProperty -Name $adapterName -DisplayName "GTK rekeying for WoWLAN" -DisplayValue "Disabled"
        }

        # Wake on Magic Packet
        if ($_.DisplayName -eq "Wake on Magic Packet") {
            Set-NetAdapterAdvancedProperty -Name $adapterName -DisplayName "Wake on Magic Packet" -DisplayValue "Disabled"
        }

        # Wake on Pattern Match
        if ($_.DisplayName -eq "Wake on Pattern Match") {
            Set-NetAdapterAdvancedProperty -Name $adapterName -DisplayName "Wake on Pattern Match" -DisplayValue "Disabled"
        }

        # Global BG Scan blocking
        if ($_.DisplayName -eq "Global BG Scan blocking") {
            Set-NetAdapterAdvancedProperty -Name $adapterName -DisplayName "Global BG Scan blocking" -DisplayValue "Always"
        }

        # Channel Width for 2.4GHz
        if ($_.DisplayName -eq "Channel Width for 2.4GHz") {
            Set-NetAdapterAdvancedProperty -Name $adapterName -DisplayName "Channel Width for 2.4GHz" -DisplayValue "Auto"
        }

        # Channel Width for 5GHz
        if ($_.DisplayName -eq "Channel Width for 5GHz") {
            Set-NetAdapterAdvancedProperty -Name $adapterName -DisplayName "Channel Width for 5GHz" -DisplayValue "Auto"
        }

        # Channel Width for 6GHz
        if ($_.DisplayName -eq "Channel Width for 6GHz") {
            Set-NetAdapterAdvancedProperty -Name $adapterName -DisplayName "Channel Width for 6GHz" -DisplayValue "Auto"
        }

        # Mixed Mode Protection
        if ($_.DisplayName -eq "Mixed Mode Protection") {
            Set-NetAdapterAdvancedProperty -Name $adapterName -DisplayName "Mixed Mode Protection" -DisplayValue "CTS-to-self Enabled"
        }

        # Fat Channel Intolerant
        if ($_.DisplayName -eq "Fat Channel Intolerant") {
            Set-NetAdapterAdvancedProperty -Name $adapterName -DisplayName "Fat Channel Intolerant" -DisplayValue "Disabled"
        }

        # Transmit Power
        if ($_.DisplayName -eq "Transmit Power") {
            Set-NetAdapterAdvancedProperty -Name $adapterName -DisplayName "Transmit Power" -DisplayValue "5. Highest"
        }

        # 802.11/ac/ax Wireless Mode
        if ($_.DisplayName -eq "802.11/ac/ax Wireless Mode") {
            Set-NetAdapterAdvancedProperty -Name $adapterName -DisplayName "802.11/ac/ax Wireless Mode" -DisplayValue "4. 802.11ax"
        }

        # 802.11n/ac Wireless Mode
        if ($_.DisplayName -eq "802.11n/ac Wireless Mode") {
            Set-NetAdapterAdvancedProperty -Name $adapterName -DisplayName "802.11n/ac Wireless Mode" -DisplayValue "802.11ac"
        }

        # MIMO Power Save Mode
        if ($_.DisplayName -eq "MIMO Power Save Mode") {
            Set-NetAdapterAdvancedProperty -Name $adapterName -DisplayName "MIMO Power Save Mode" -DisplayValue "No SMPS"
        }

        # Roaming Aggressiveness
        if ($_.DisplayName -eq "Roaming Aggressiveness") {
            Set-NetAdapterAdvancedProperty -Name $adapterName -DisplayName "Roaming Aggressiveness" -DisplayValue "1. Lowest"
        }

        # Preferred Band
        if ($_.DisplayName -eq "Preferred Band") {
            Set-NetAdapterAdvancedProperty -Name $adapterName -DisplayName "Preferred Band" -DisplayValue "1. No Preference"
        }

        # Throughput Booster
        if ($_.DisplayName -eq "Throughput Booster") {
            Set-NetAdapterAdvancedProperty -Name $adapterName -DisplayName "Throughput Booster" -DisplayValue "Disabled"
        }

        # U-APSD support
        if ($_.DisplayName -eq "U-APSD support") {
            Set-NetAdapterAdvancedProperty -Name $adapterName -DisplayName "U-APSD support" -DisplayValue "Disabled"
        }

        # 802.11a/b/g Wireless Mode
        if ($_.DisplayName -eq "802.11a/b/g Wireless Mode") {
            Set-NetAdapterAdvancedProperty -Name $adapterName -DisplayName "802.11a/b/g Wireless Mode" -DisplayValue "6. Dual Band 802.11a/b/g"
        }
    }
}
