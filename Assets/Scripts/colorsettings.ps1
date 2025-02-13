Get-WmiObject -Class Win32_VideoController | Where-Object { $_.PNPDeviceID -like "PCI\VEN_*" } | ForEach-Object {
    $pnpDeviceId = $_.PNPDeviceID
    $regPath = "HKLM:\SYSTEM\CurrentControlSet\Enum\$pnpDeviceId"
    $driver = (Get-ItemProperty -Path $regPath -Name "Driver" -ErrorAction SilentlyContinue).Driver
    $classKey = "HKLM:\SYSTEM\CurrentControlSet\Control\Class\$driver"
    $providerName = (Get-ItemProperty -Path $classKey -Name "ProviderName" -ErrorAction SilentlyContinue).ProviderName
    
    if ($providerName -eq "NVIDIA") {
        # use nvidia settings for edge enhancements
        New-ItemProperty -Path $classKey -Name "_User_SUB0_DFP0_XEN_Edge_Enhance" -Value 2147483649 -Type DWord -Force
        New-ItemProperty -Path $classKey -Name "_User_SUB0_DFP2_XEN_Edge_Enhance" -Value 2147483649 -Type DWord -Force
        New-ItemProperty -Path $classKey -Name "_User_SUB0_DFP4_XEN_Edge_Enhance" -Value 2147483649 -Type DWord -Force

        # set edge enhancements to 0
        New-ItemProperty -Path $classKey -Name "_User_SUB0_DFP0_VAL_Edge_Enhance" -Value 0 -Type DWord -Force
        New-ItemProperty -Path $classKey -Name "_User_SUB0_DFP2_VAL_Edge_Enhance" -Value 0 -Type DWord -Force
        New-ItemProperty -Path $classKey -Name "_User_SUB0_DFP4_VAL_Edge_Enhance" -Value 0 -Type DWord -Force
        New-ItemProperty -Path $classKey -Name "_User_SUB0_DFP0_XALG_Edge_Enhance" -Value ([byte[]](0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00)) -Type Binary -Force
        New-ItemProperty -Path $classKey -Name "_User_SUB0_DFP2_XALG_Edge_Enhance" -Value ([byte[]](0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00)) -Type Binary -Force
        New-ItemProperty -Path $classKey -Name "_User_SUB0_DFP4_XALG_Edge_Enhance" -Value ([byte[]](0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00)) -Type Binary -Force
        
        # use nvidia settings for noise reduction
        New-ItemProperty -Path $classKey -Name "_User_SUB0_DFP0_XEN_Noise_Reduce" -Value 2147483649 -Type DWord -Force
        New-ItemProperty -Path $classKey -Name "_User_SUB0_DFP2_XEN_Noise_Reduce" -Value 2147483649 -Type DWord -Force
        New-ItemProperty -Path $classKey -Name "_User_SUB0_DFP4_XEN_Noise_Reduce" -Value 2147483649 -Type DWord -Force

        # set noise reduction to 0
        New-ItemProperty -Path $classKey -Name "_User_SUB0_DFP0_VAL_Noise_Reduce" -Value 0 -Type DWord -Force
        New-ItemProperty -Path $classKey -Name "_User_SUB0_DFP2_VAL_Noise_Reduce" -Value 0 -Type DWord -Force
        New-ItemProperty -Path $classKey -Name "_User_SUB0_DFP4_VAL_Noise_Reduce" -Value 0 -Type DWord -Force
        New-ItemProperty -Path $classKey -Name "_User_SUB0_DFP0_XALG_Noise_Reduce" -Value ([byte[]](0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00)) -Type Binary -Force
        New-ItemProperty -Path $classKey -Name "_User_SUB0_DFP2_XALG_Noise_Reduce" -Value ([byte[]](0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00)) -Type Binary -Force
        New-ItemProperty -Path $classKey -Name "_User_SUB0_DFP4_XALG_Noise_Reduce" -Value ([byte[]](0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00)) -Type Binary -Force
        
        # disable use inverse telecine
        New-ItemProperty -Path $classKey -Name "_User_SUB0_DFP0_XALG_Cadence" -Value ([byte[]](0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00)) -Type Binary -Force
        New-ItemProperty -Path $classKey -Name "_User_SUB0_DFP2_XALG_Cadence" -Value ([byte[]](0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00)) -Type Binary -Force
        New-ItemProperty -Path $classKey -Name "_User_SUB0_DFP4_XALG_Cadence" -Value ([byte[]](0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00)) -Type Binary -Force
        
        # use nvidia settings for video color settings
        New-ItemProperty -Path $classKey -Name "_User_SUB0_DFP0_XEN_Contrast" -Value 2147483649 -Type DWord -Force
        New-ItemProperty -Path $classKey -Name "_User_SUB0_DFP0_XEN_RGB_Gamma_G" -Value 2147483649 -Type DWord -Force
        New-ItemProperty -Path $classKey -Name "_User_SUB0_DFP0_XEN_RGB_Gamma_R" -Value 2147483649 -Type DWord -Force
        New-ItemProperty -Path $classKey -Name "_User_SUB0_DFP0_XEN_RGB_Gamma_B" -Value 2147483649 -Type DWord -Force
        New-ItemProperty -Path $classKey -Name "_User_SUB0_DFP0_XEN_Hue" -Value 2147483649 -Type DWord -Force
        New-ItemProperty -Path $classKey -Name "_User_SUB0_DFP0_XEN_Saturation" -Value 2147483649 -Type DWord -Force
        New-ItemProperty -Path $classKey -Name "_User_SUB0_DFP0_XEN_Brightness" -Value 2147483649 -Type DWord -Force
        New-ItemProperty -Path $classKey -Name "_User_SUB0_DFP0_XEN_Color_Range" -Value 2147483649 -Type DWord -Force
        New-ItemProperty -Path $classKey -Name "_User_SUB0_DFP2_XEN_Contrast" -Value 2147483649 -Type DWord -Force
        New-ItemProperty -Path $classKey -Name "_User_SUB0_DFP2_XEN_RGB_Gamma_G" -Value 2147483649 -Type DWord -Force
        New-ItemProperty -Path $classKey -Name "_User_SUB0_DFP2_XEN_RGB_Gamma_R" -Value 2147483649 -Type DWord -Force
        New-ItemProperty -Path $classKey -Name "_User_SUB0_DFP2_XEN_RGB_Gamma_B" -Value 2147483649 -Type DWord -Force
        New-ItemProperty -Path $classKey -Name "_User_SUB0_DFP2_XEN_Hue" -Value 2147483649 -Type DWord -Force
        New-ItemProperty -Path $classKey -Name "_User_SUB0_DFP2_XEN_Saturation" -Value 2147483649 -Type DWord -Force
        New-ItemProperty -Path $classKey -Name "_User_SUB0_DFP2_XEN_Brightness" -Value 2147483649 -Type DWord -Force
        New-ItemProperty -Path $classKey -Name "_User_SUB0_DFP2_XEN_Color_Range" -Value 2147483649 -Type DWord -Force
        New-ItemProperty -Path $classKey -Name "_User_SUB0_DFP4_XEN_Contrast" -Value 2147483649 -Type DWord -Force
        New-ItemProperty -Path $classKey -Name "_User_SUB0_DFP4_XEN_RGB_Gamma_G" -Value 2147483649 -Type DWord -Force
        New-ItemProperty -Path $classKey -Name "_User_SUB0_DFP4_XEN_RGB_Gamma_R" -Value 2147483649 -Type DWord -Force
        New-ItemProperty -Path $classKey -Name "_User_SUB0_DFP4_XEN_RGB_Gamma_B" -Value 2147483649 -Type DWord -Force
        New-ItemProperty -Path $classKey -Name "_User_SUB0_DFP4_XEN_Hue" -Value 2147483649 -Type DWord -Force
        New-ItemProperty -Path $classKey -Name "_User_SUB0_DFP4_XEN_Saturation" -Value 2147483649 -Type DWord -Force
        New-ItemProperty -Path $classKey -Name "_User_SUB0_DFP4_XEN_Brightness" -Value 2147483649 -Type DWord -Force
        New-ItemProperty -Path $classKey -Name "_User_SUB0_DFP4_XEN_Color_Range" -Value 2147483649 -Type DWord -Force

        # set dynamic range to full
        New-ItemProperty -Path $classKey -Name "_User_SUB0_DFP0_XALG_Color_Range" -Value ([byte[]](0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00)) -Type Binary -Force
        New-ItemProperty -Path $classKey -Name "_User_SUB0_DFP2_XALG_Color_Range" -Value ([byte[]](0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00)) -Type Binary -Force
        New-ItemProperty -Path $classKey -Name "_User_SUB0_DFP4_XALG_Color_Range" -Value ([byte[]](0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00)) -Type Binary -Force
    }
}







     