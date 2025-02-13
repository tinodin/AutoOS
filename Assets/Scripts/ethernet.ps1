Get-NetAdapter | Where-Object { $_.PhysicalMediaType -eq "802.3" } | ForEach-Object {
    $adapterName = $_.Name
    $adapterProperties = Get-NetAdapterAdvancedProperty -Name $adapterName

    # Flow Control
    if ($adapterProperties | Where-Object { $_.DisplayName -eq "Flow Control" }) {
        Set-NetAdapterAdvancedProperty -Name $adapterName -DisplayName "Flow Control" -DisplayValue "Disabled"
    }

    # Idle power down restriction
    if ($adapterProperties | Where-Object { $_.DisplayName -eq "Idle power down restriction" }) {
        Set-NetAdapterAdvancedProperty -Name $adapterName -DisplayName "Idle power down restriction" -DisplayValue "Disabled"
    }

    # Interrupt Moderation
    if ($adapterProperties | Where-Object { $_.DisplayName -eq "Interrupt Moderation" }) {
        Set-NetAdapterAdvancedProperty -Name $adapterName -DisplayName "Interrupt Moderation" -DisplayValue "Enabled"
    }

    # IPv4 Checksum Offload
    if ($adapterProperties | Where-Object { $_.DisplayName -eq "IPv4 Checksum Offload" }) {
        Set-NetAdapterAdvancedProperty -Name $adapterName -DisplayName "IPv4 Checksum Offload" -DisplayValue "Rx & Tx Enabled"
    }

    # Jumbo Packet
    if ($adapterProperties | Where-Object { $_.DisplayName -eq "Jumbo Packet" }) {
        Set-NetAdapterAdvancedProperty -Name $adapterName -DisplayName "Jumbo Packet" -DisplayValue "Disabled"
    }

    # Large Send Offload V2 (IPv4)
    if ($adapterProperties | Where-Object { $_.DisplayName -eq "Large Send Offload V2 (IPv4)" }) {
        Set-NetAdapterAdvancedProperty -Name $adapterName -DisplayName "Large Send Offload V2 (IPv4)" -DisplayValue "Disabled"
    }

    # Large Send Offload V2 (IPv6)
    if ($adapterProperties | Where-Object { $_.DisplayName -eq "Large Send Offload V2 (IPv6)" }) {
        Set-NetAdapterAdvancedProperty -Name $adapterName -DisplayName "Large Send Offload V2 (IPv6)" -DisplayValue "Disabled"
    }

    # Wake from S0ix on Magic Packet
    if ($adapterProperties | Where-Object { $_.DisplayName -eq "Wake from S0ix on Magic Packet" }) {
        Set-NetAdapterAdvancedProperty -Name $adapterName -DisplayName "Wake from S0ix on Magic Packet" -DisplayValue "Disabled"
    }

    # Wake On Magic Packet From S5
    if ($adapterProperties | Where-Object { $_.DisplayName -eq "Wake On Magic Packet From S5" }) {
        Set-NetAdapterAdvancedProperty -Name $adapterName -DisplayName "Wake On Magic Packet From S5" -DisplayValue "Disabled"
    }

    # Maximum Number of RSS Queues
    if ($adapterProperties | Where-Object { $_.DisplayName -eq "Maximum Number of RSS Queues" }) {
        Set-NetAdapterAdvancedProperty -Name $adapterName -DisplayName "Maximum Number of RSS Queues" -DisplayValue "4 Queues"
    }

    # ARP Offload
    if ($adapterProperties | Where-Object { $_.DisplayName -eq "ARP Offload" }) {
        Set-NetAdapterAdvancedProperty -Name $adapterName -DisplayName "ARP Offload" -DisplayValue "Enabled"
    }

    # NS Offload
    if ($adapterProperties | Where-Object { $_.DisplayName -eq "NS Offload" }) {
        Set-NetAdapterAdvancedProperty -Name $adapterName -DisplayName "NS Offload" -DisplayValue "Enabled"
    }

    # Packet Priority & VLAN
    if ($adapterProperties | Where-Object { $_.DisplayName -eq "Packet Priority & VLAN" }) {
        Set-NetAdapterAdvancedProperty -Name $adapterName -DisplayName "Packet Priority & VLAN" -DisplayValue "Packet Priority & VLAN Enabled"
    }

    # Receive Segment Coalescing (IPv4)
    if ($adapterProperties | Where-Object { $_.DisplayName -eq "Recv Segment Coalescing (IPv4)" }) {
        Set-NetAdapterAdvancedProperty -Name $adapterName -DisplayName "Recv Segment Coalescing (IPv4)" -DisplayValue "Disabled"
    }

    # Receive Segment Coalescing (IPv6)
    if ($adapterProperties | Where-Object { $_.DisplayName -eq "Recv Segment Coalescing (IPv6)" }) {
        Set-NetAdapterAdvancedProperty -Name $adapterName -DisplayName "Recv Segment Coalescing (IPv6)" -DisplayValue "Disabled"
    }

    # Receive Buffers
    if ($adapterProperties | Where-Object { $_.DisplayName -eq "Receive Buffers" }) {
        Set-NetAdapterAdvancedProperty -Name $adapterName -DisplayName "Receive Buffers" -DisplayValue "512"
        Set-NetAdapterAdvancedProperty -Name $adapterName -DisplayName "Receive Buffers" -DisplayValue "1024"
        Set-NetAdapterAdvancedProperty -Name $adapterName -DisplayName "Receive Buffers" -DisplayValue "2048"
        Set-NetAdapterAdvancedProperty -Name $adapterName -DisplayName "Receive Buffers" -DisplayValue "4096"
    }

    # Selective Suspend
    if ($adapterProperties | Where-Object { $_.DisplayName -eq "Selective Suspend" }) {
        Set-NetAdapterAdvancedProperty -Name $adapterName -DisplayName "Selective Suspend" -DisplayValue "Disabled"
    }

    # Speed & Duplex
    if ($adapterProperties | Where-Object { $_.DisplayName -eq "Speed & Duplex" }) {
        Set-NetAdapterAdvancedProperty -Name $adapterName -DisplayName "Speed & Duplex" -DisplayValue "Auto Negotiation"
    }

    # Selective Suspend Idle Timeout
    if ($adapterProperties | Where-Object { $_.DisplayName -eq "Selective Suspend Idle Timeout" }) {
        Set-NetAdapterAdvancedProperty -Name $adapterName -DisplayName "Selective Suspend Idle Timeout" -DisplayValue "60"
    }

    # TCP Checksum Offload (IPv4)
    if ($adapterProperties | Where-Object { $_.DisplayName -eq "TCP Checksum Offload (IPv4)" }) {
        Set-NetAdapterAdvancedProperty -Name $adapterName -DisplayName "TCP Checksum Offload (IPv4)" -DisplayValue "Rx & Tx Enabled"
    }

    # TCP Checksum Offload (IPv6)
    if ($adapterProperties | Where-Object { $_.DisplayName -eq "TCP Checksum Offload (IPv6)" }) {
        Set-NetAdapterAdvancedProperty -Name $adapterName -DisplayName "TCP Checksum Offload (IPv6)" -DisplayValue "Rx & Tx Enabled"
    }

    # Transmit Buffers
    if ($adapterProperties | Where-Object { $_.DisplayName -eq "Transmit Buffers" }) {
        Set-NetAdapterAdvancedProperty -Name $adapterName -DisplayName "Transmit Buffers" -DisplayValue "512"
        Set-NetAdapterAdvancedProperty -Name $adapterName -DisplayName "Transmit Buffers" -DisplayValue "1024"
        Set-NetAdapterAdvancedProperty -Name $adapterName -DisplayName "Transmit Buffers" -DisplayValue "2048"
        Set-NetAdapterAdvancedProperty -Name $adapterName -DisplayName "Transmit Buffers" -DisplayValue "4096"
    }

    # UDP Checksum Offload (IPv4)
    if ($adapterProperties | Where-Object { $_.DisplayName -eq "UDP Checksum Offload (IPv4)" }) {
        Set-NetAdapterAdvancedProperty -Name $adapterName -DisplayName "UDP Checksum Offload (IPv4)" -DisplayValue "Rx & Tx Enabled"
    }

    # Wake on Magic Packet
    if ($adapterProperties | Where-Object { $_.DisplayName -eq "Wake on Magic Packet" }) {
        Set-NetAdapterAdvancedProperty -Name $adapterName -DisplayName "Wake on Magic Packet" -DisplayValue "Disabled"
    }

    # Wake on Pattern Match
    if ($adapterProperties | Where-Object { $_.DisplayName -eq "Wake on Pattern Match" }) {
        Set-NetAdapterAdvancedProperty -Name $adapterName -DisplayName "Wake on Pattern Match" -DisplayValue "Disabled"
    }

    # Wake on pattern match
    if ($adapterProperties | Where-Object { $_.DisplayName -eq "Wake on pattern match" }) {
        Set-NetAdapterAdvancedProperty -Name $adapterName -DisplayName "Wake on pattern match" -DisplayValue "Disabled"
    }

    # DMA Coalescing
    if ($adapterProperties | Where-Object { $_.DisplayName -eq "DMA Coalescing" }) {
        Set-NetAdapterAdvancedProperty -Name $adapterName -DisplayName "DMA Coalescing" -DisplayValue "Disabled"
    }

    # Energy Efficient Ethernet
    if ($adapterProperties | Where-Object { $_.DisplayName -eq "Energy Efficient Ethernet" }) {
        Set-NetAdapterAdvancedProperty -Name $adapterName -DisplayName "Energy Efficient Ethernet" -DisplayValue "Off"
    }

    # Enable PME
    if ($adapterProperties | Where-Object { $_.DisplayName -eq "Enable PME" }) {
        Set-NetAdapterAdvancedProperty -Name $adapterName -DisplayName "Enable PME" -DisplayValue "Disabled"
    }

    # Interrupt Moderation Rate
    if ($adapterProperties | Where-Object { $_.DisplayName -eq "Interrupt Moderation Rate" }) {
        Set-NetAdapterAdvancedProperty -Name $adapterName -DisplayName "Interrupt Moderation Rate" -DisplayValue "Medium"
    }

    # Log Link State Event
    if ($adapterProperties | Where-Object { $_.DisplayName -eq "Log Link State Event" }) {
        Set-NetAdapterAdvancedProperty -Name $adapterName -DisplayName "Log Link State Event" -DisplayValue "Disabled"
    }

    # Gigabit Master Slave Mode
    if ($adapterProperties | Where-Object { $_.DisplayName -eq "Gigabit Master Slave Mode" }) {
        Set-NetAdapterAdvancedProperty -Name $adapterName -DisplayName "Gigabit Master Slave Mode" -DisplayValue "Auto Detect"
    }

    # Locally Administered Address
    if ($adapterProperties | Where-Object { $_.DisplayName -eq "Locally Administered Address" }) {
        Set-NetAdapterAdvancedProperty -Name $adapterName -DisplayName "Locally Administered Address" -DisplayValue "--"
    }

    # Wait for Link
    if ($adapterProperties | Where-Object { $_.DisplayName -eq "Wait for Link" }) {
        Set-NetAdapterAdvancedProperty -Name $adapterName -DisplayName "Wait for Link" -DisplayValue "Off"
    }

    # Wake on Link Settings
    if ($adapterProperties | Where-Object { $_.DisplayName -eq "Wake on Link Settings" }) {
        Set-NetAdapterAdvancedProperty -Name $adapterName -DisplayName "Wake on Link Settings" -DisplayValue "Disabled"
    }

    # Shutdown Wake-On-Lan
    if ($adapterProperties | Where-Object { $_.DisplayName -eq "Shutdown Wake-On-Lan" }) {
        Set-NetAdapterAdvancedProperty -Name $adapterName -DisplayName "Shutdown Wake-On-Lan" -DisplayValue "Disabled"
    }

    # WOL & Shutdown Wake-On-Lan
    if ($adapterProperties | Where-Object { $_.DisplayName -eq "WOL & Shutdown Link Speed" }) {
        Set-NetAdapterAdvancedProperty -Name $adapterName -DisplayName "WOL & Shutdown Link Speed" -DisplayValue "Not Speed Down"
    }
}
