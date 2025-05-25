# Credits: imribiy
# https://github.com/imribiy/amd-gpu-tweaks

Get-WmiObject -Class Win32_VideoController | Where-Object { $_.PNPDeviceID -like "PCI\VEN_*" } | ForEach-Object {
    $pnpDeviceId = $_.PNPDeviceID
    $regPath = "HKLM:\SYSTEM\CurrentControlSet\Enum\$pnpDeviceId"
    $driver = (Get-ItemProperty -Path $regPath -Name "Driver" -ErrorAction SilentlyContinue).Driver
    $classKey = "HKLM:\SYSTEM\CurrentControlSet\Control\Class\$driver"
    $providerName = (Get-ItemProperty -Path $classKey -Name "ProviderName" -ErrorAction SilentlyContinue).ProviderName
    
    if ($providerName -eq "Advanced Micro Devices, Inc.") {
        New-ItemProperty -Path $classKey -Name "NotifySubscription" -Value ([byte[]](0x30,0x00)) -PropertyType Binary -Force
        New-ItemProperty -Path $classKey -Name "IsComponentControl" -Value ([byte[]](0x00,0x00,0x00,0x00)) -PropertyType Binary -Force
        New-ItemProperty -Path $classKey -Name "KMD_USUEnable" -Value 0 -PropertyType DWord -Force
        New-ItemProperty -Path $classKey -Name "KMD_RadeonBoostEnabled" -Value 0 -PropertyType DWord -Force
        New-ItemProperty -Path $classKey -Name "IsAutoDefault" -Value ([byte[]](0x01,0x00,0x00,0x00)) -PropertyType Binary -Force
        New-ItemProperty -Path $classKey -Name "KMD_ChillEnabled" -Value 0 -PropertyType DWord -Force
        New-ItemProperty -Path $classKey -Name "KMD_DeLagEnabled" -Value 0 -PropertyType DWord -Force
        New-ItemProperty -Path $classKey -Name "ACE" -Value ([byte[]](0x30,0x00)) -PropertyType Binary -Force
        New-ItemProperty -Path $classKey\UMD -Name "AnisoDegree_SET" -Value ([byte[]](0x30,0x20,0x32,0x20,0x34,0x20,0x38,0x20,0x31,0x36,0x00)) -PropertyType Binary -Force
        New-ItemProperty -Path $classKey\UMD -Name "Main3D_SET" -Value ([byte[]](0x30,0x20,0x31,0x20,0x32,0x20,0x33,0x20,0x34,0x20,0x35,0x00)) -PropertyType Binary -Force
        New-ItemProperty -Path $classKey\UMD -Name "Tessellation_OPTION" -Value ([byte[]](0x32,0x00)) -PropertyType Binary -Force
        New-ItemProperty -Path $classKey\UMD -Name "Tessellation" -Value ([byte[]](0x31,0x00)) -PropertyType Binary -Force
        New-ItemProperty -Path $classKey\UMD -Name "AAF" -Value ([byte[]](0x30,0x00,0x00,0x00)) -PropertyType Binary -Force
        New-ItemProperty -Path $classKey\UMD -Name "GI" -Value ([byte[]](0x31,0x00,0x00,0x00)) -PropertyType Binary -Force
        New-ItemProperty -Path $classKey\UMD -Name "CatalystAI" -Value ([byte[]](0x31,0x00,0x00,0x00)) -PropertyType Binary -Force
        New-ItemProperty -Path $classKey\UMD -Name "TemporalAAMultiplier_NA" -Value ([byte[]](0x31,0x00)) -PropertyType Binary -Force
        New-ItemProperty -Path $classKey\UMD -Name "ForceZBufferDepth" -Value ([byte[]](0x30,0x00,0x00,0x00)) -PropertyType Binary -Force
        New-ItemProperty -Path $classKey\UMD -Name "EnableTripleBuffering" -Value ([byte[]](0x30,0x00)) -PropertyType Binary -Force
        New-ItemProperty -Path $classKey\UMD -Name "ExportCompressedTex" -Value ([byte[]](0x31,0x00,0x00,0x00)) -PropertyType Binary -Force
        New-ItemProperty -Path $classKey\UMD -Name "PixelCenter" -Value ([byte[]](0x30,0x00,0x00,0x00)) -PropertyType Binary -Force
        New-ItemProperty -Path $classKey\UMD -Name "ZFormats_NA" -Value ([byte[]](0x31,0x00)) -PropertyType Binary -Force
        New-ItemProperty -Path $classKey\UMD -Name "DitherAlpha_NA" -Value ([byte[]](0x31,0x00)) -PropertyType Binary -Force
        New-ItemProperty -Path $classKey\UMD -Name "SwapEffect_D3D_SET" -PropertyType Binary -Value ([byte[]](0x30,0x20,0x31,0x20,0x32,0x20,0x33,0x20,0x34,0x20,0x38,0x20,0x39,0x00)) -Force
        New-ItemProperty -Path $classKey\UMD -Name "TFQ" -PropertyType Binary -Value ([byte[]](0x32,0x00)) -Force
        New-ItemProperty -Path $classKey\UMD -Name "VSyncControl" -PropertyType Binary -Value ([byte[]](0x31,0x00)) -Force
        New-ItemProperty -Path $classKey\UMD -Name "TextureOpt" -PropertyType Binary -Value ([byte[]](0x30,0x00,0x00,0x00)) -Force
        New-ItemProperty -Path $classKey\UMD -Name "TextureLod" -PropertyType Binary -Value ([byte[]](0x30,0x00,0x00,0x00)) -Force
        New-ItemProperty -Path $classKey\UMD -Name "ASE" -PropertyType Binary -Value ([byte[]](0x30,0x00)) -Force
        New-ItemProperty -Path $classKey\UMD -Name "ASD" -PropertyType Binary -Value ([byte[]](0x30,0x00)) -Force
        New-ItemProperty -Path $classKey\UMD -Name "ASTT" -PropertyType Binary -Value ([byte[]](0x30,0x00)) -Force
        New-ItemProperty -Path $classKey\UMD -Name "AntiAliasSamples" -PropertyType Binary -Value ([byte[]](0x30,0x00)) -Force
        New-ItemProperty -Path $classKey\UMD -Name "AntiAlias" -PropertyType Binary -Value ([byte[]](0x31,0x00)) -Force
        New-ItemProperty -Path $classKey\UMD -Name "AnisoDegree" -PropertyType Binary -Value ([byte[]](0x30,0x00)) -Force
        New-ItemProperty -Path $classKey\UMD -Name "AnisoType" -PropertyType Binary -Value ([byte[]](0x30,0x00,0x00,0x00)) -Force
        New-ItemProperty -Path $classKey\UMD -Name "AntiAliasMapping_SET" -PropertyType Binary -Value ([byte[]](0x30,0x28,0x30,0x3A,0x30,0x2C,0x31,0x3A,0x30,0x29,0x20,0x32,0x28,0x30,0x3A,0x32,0x2C,0x31,0x3A,0x32,0x29,0x20,0x34,0x28,0x30,0x3A,0x34,0x2C,0x31,0x3A,0x34,0x29,0x20,0x38,0x28,0x30,0x3A,0x38,0x2C,0x31,0x3A,0x38,0x29,0x00)) -Force
        New-ItemProperty -Path $classKey\UMD -Name "AntiAliasSamples_SET" -PropertyType Binary -Value ([byte[]](0x30,0x20,0x32,0x20,0x34,0x20,0x38,0x00)) -Force
        New-ItemProperty -Path $classKey\UMD -Name "ForceZBufferDepth_SET" -PropertyType Binary -Value ([byte[]](0x30,0x20,0x31,0x36,0x20,0x32,0x34,0x00)) -Force
        New-ItemProperty -Path $classKey\UMD -Name "SwapEffect_OGL_SET" -PropertyType Binary -Value ([byte[]](0x30,0x20,0x31,0x20,0x32,0x20,0x33,0x20,0x34,0x20,0x35,0x20,0x36,0x20,0x37,0x20,0x38,0x20,0x39,0x20,0x31,0x31,0x20,0x31,0x32,0x20,0x31,0x33,0x20,0x31,0x34,0x20,0x31,0x35,0x20,0x31,0x36,0x20,0x31,0x37,0x00)) -Force
        New-ItemProperty -Path $classKey\UMD -Name "Tessellation_SET" -PropertyType Binary -Value ([byte[]](0x31,0x20,0x32,0x20,0x34,0x20,0x36,0x20,0x38,0x20,0x31,0x36,0x20,0x33,0x32,0x20,0x36,0x34,0x00)) -Force
        New-ItemProperty -Path $classKey\UMD -Name "HighQualityAF" -PropertyType Binary -Value ([byte[]](0x31,0x00)) -Force
        New-ItemProperty -Path $classKey\UMD -Name "DisplayCrossfireLogo" -PropertyType Binary -Value ([byte[]](0x30,0x00)) -Force
        New-ItemProperty -Path $classKey\UMD -Name "AppGpuId" -PropertyType Binary -Value ([byte[]](0x30,0x00,0x78,0x00,0x30,0x00,0x31,0x00,0x30,0x00,0x30,0x00)) -Force
        New-ItemProperty -Path $classKey\UMD -Name "SwapEffect" -PropertyType Binary -Value ([byte[]](0x30,0x00,0x00,0x00)) -Force
        New-ItemProperty -Path $classKey\UMD -Name "PowerState" -PropertyType Binary -Value ([byte[]](0x30,0x00)) -Force
        New-ItemProperty -Path $classKey\UMD -Name "AntiStuttering" -PropertyType Binary -Value ([byte[]](0x31,0x00)) -Force
        New-ItemProperty -Path $classKey\UMD -Name "TurboSync" -PropertyType Binary -Value ([byte[]](0x30,0x00)) -Force
        New-ItemProperty -Path $classKey\UMD -Name "SurfaceFormatReplacements" -PropertyType Binary -Value ([byte[]](0x31,0x00)) -Force
        New-ItemProperty -Path $classKey\UMD -Name "EQAA" -PropertyType Binary -Value ([byte[]](0x30,0x00)) -Force
        New-ItemProperty -Path $classKey\UMD -Name "ShaderCache" -PropertyType Binary -Value ([byte[]](0x31,0x00)) -Force
        New-ItemProperty -Path $classKey\UMD -Name "MLF" -PropertyType Binary -Value ([byte[]](0x30,0x00)) -Force
        New-ItemProperty -Path $classKey\UMD -Name "TruformMode_NA" -PropertyType Binary -Value ([byte[]](0x31,0x00)) -Force
        New-ItemProperty -Path $classKey\UMD\DXVA -Name "LRTCEnable" -PropertyType Binary -Value ([byte[]](0x30,0x00,0x00,0x00)) -Force
        New-ItemProperty -Path $classKey\UMD\DXVA -Name "3to2Pulldown" -PropertyType Binary -Value ([byte[]](0x31,0x00,0x00,0x00)) -Force
        New-ItemProperty -Path $classKey\UMD\DXVA -Name "MosquitoNoiseRemoval_ENABLE" -PropertyType Binary -Value ([byte[]](0x30,0x00,0x00,0x00)) -Force
        New-ItemProperty -Path $classKey\UMD\DXVA -Name "MosquitoNoiseRemoval" -PropertyType Binary -Value ([byte[]](0x35,0x00,0x30,0x00,0x00,0x00)) -Force
        New-ItemProperty -Path $classKey\UMD\DXVA -Name "Deblocking_ENABLE" -PropertyType Binary -Value ([byte[]](0x30,0x00,0x00,0x00)) -Force
        New-ItemProperty -Path $classKey\UMD\DXVA -Name "Deblocking" -PropertyType Binary -Value ([byte[]](0x35,0x00,0x30,0x00,0x00,0x00)) -Force
        New-ItemProperty -Path $classKey\UMD\DXVA -Name "DemoMode" -PropertyType Binary -Value ([byte[]](0x30,0x00,0x00,0x00)) -Force
        New-ItemProperty -Path $classKey\UMD\DXVA -Name "OverridePA" -PropertyType Binary -Value ([byte[]](0x30,0x00,0x00,0x00)) -Force
        New-ItemProperty -Path $classKey\UMD\DXVA -Name "DynamicRange" -PropertyType Binary -Value ([byte[]](0x30,0x00,0x00,0x00)) -Force
        New-ItemProperty -Path $classKey\UMD\DXVA -Name "StaticGamma_ENABLE" -PropertyType Binary -Value ([byte[]](0x30,0x00,0x00,0x00)) -Force
        New-ItemProperty -Path $classKey\UMD\DXVA -Name "BlueStretch_ENABLE" -PropertyType Binary -Value ([byte[]](0x31,0x00,0x00,0x00)) -Force
        New-ItemProperty -Path $classKey\UMD\DXVA -Name "BlueStretch" -PropertyType Binary -Value ([byte[]](0x31,0x00,0x00,0x00)) -Force
        New-ItemProperty -Path $classKey\UMD\DXVA -Name "LRTCCoef" -PropertyType Binary -Value ([byte[]](0x31,0x00,0x30,0x00,0x30,0x00,0x00,0x00)) -Force
        New-ItemProperty -Path $classKey\UMD\DXVA -Name "DynamicContrast_ENABLE" -PropertyType Binary -Value ([byte[]](0x30,0x00,0x00,0x00)) -Force
        New-ItemProperty -Path $classKey\UMD\DXVA -Name "WhiteBalanceCorrection" -PropertyType Binary -Value ([byte[]](0x30,0x00,0x00,0x00)) -Force
        New-ItemProperty -Path $classKey\UMD\DXVA -Name "Fleshtone_ENABLE" -PropertyType Binary -Value ([byte[]](0x30,0x00,0x00,0x00)) -Force
        New-ItemProperty -Path $classKey\UMD\DXVA -Name "Fleshtone" -PropertyType Binary -Value ([byte[]](0x35,0x00,0x30,0x00,0x00,0x00)) -Force
        New-ItemProperty -Path $classKey\UMD\DXVA -Name "ColorVibrance_ENABLE" -PropertyType Binary -Value ([byte[]](0x31,0x00,0x00,0x00)) -Force
        New-ItemProperty -Path $classKey\UMD\DXVA -Name "ColorVibrance" -PropertyType Binary -Value ([byte[]](0x34,0x00,0x30,0x00,0x00,0x00)) -Force
        New-ItemProperty -Path $classKey\UMD\DXVA -Name "Detail_ENABLE" -PropertyType Binary -Value ([byte[]](0x30,0x00,0x00,0x00)) -Force
        New-ItemProperty -Path $classKey\UMD\DXVA -Name "Detail" -PropertyType Binary -Value ([byte[]](0x31,0x00,0x30,0x00,0x00,0x00)) -Force
        New-ItemProperty -Path $classKey\UMD\DXVA -Name "Denoise_ENABLE" -PropertyType Binary -Value ([byte[]](0x30,0x00,0x00,0x00)) -Force
        New-ItemProperty -Path $classKey\UMD\DXVA -Name "Denoise" -PropertyType Binary -Value ([byte[]](0x36,0x00,0x34,0x00,0x00,0x00)) -Force
        New-ItemProperty -Path $classKey\UMD\DXVA -Name "TrueWhite" -PropertyType Binary -Value ([byte[]](0x30,0x00,0x00,0x00)) -Force
        New-ItemProperty -Path $classKey\UMD\DXVA -Name "OvlTheaterMode" -PropertyType Binary -Value ([byte[]](0x30,0x00,0x00,0x00)) -Force
        New-ItemProperty -Path $classKey\UMD\DXVA -Name "StaticGamma" -PropertyType Binary -Value ([byte[]](0x31,0x00,0x30,0x00,0x30,0x00,0x00,0x00)) -Force
        New-ItemProperty -Path $classKey\UMD\DXVA -Name "InternetVideo" -PropertyType Binary -Value ([byte[]](0x30,0x00,0x00,0x00)) -Force
        New-ItemProperty -Path $classKey\UMD -Name "Main3D_DEF" -PropertyType String -Value "1" -Force
        New-ItemProperty -Path $classKey\UMD -Name "Main3D" -PropertyType Binary -Value ([byte[]](0x31,0x00)) -Force
        New-ItemProperty -Path $classKey -Name "DisableDMACopy" -PropertyType DWord -Value 1 -Force
        New-ItemProperty -Path $classKey -Name "DisableBlockWrite" -PropertyType DWord -Value 0 -Force
        New-ItemProperty -Path $classKey -Name "PP_ThermalAutoThrottlingEnable" -PropertyType DWord -Value 0 -Force
        New-ItemProperty -Path $classKey -Name "DisableDrmdmaPowerGating" -PropertyType DWord -Value 1 -Force

        New-ItemProperty -Path "HKLM:\System\CurrentControlSet\Services\amdwddmg" -Name "ChillEnabled" -PropertyType DWord -Value 0 -Force
        New-ItemProperty -Path "HKLM:\System\CurrentControlSet\Services\AMD Crash Defender Service" -Name "Start" -PropertyType DWord -Value 4 -Force
        New-ItemProperty -Path "HKLM:\System\CurrentControlSet\Services\AMD External Events Utility" -Name "Start" -PropertyType DWord -Value 4 -Force
        New-ItemProperty -Path "HKLM:\System\CurrentControlSet\Services\amdfendr" -Name "Start" -PropertyType DWord -Value 4 -Force
        New-ItemProperty -Path "HKLM:\System\CurrentControlSet\Services\amdfendrmgr" -Name "Start" -PropertyType DWord -Value 4 -Force
        New-ItemProperty -Path "HKLM:\System\CurrentControlSet\Services\amdlog" -Name "Start" -PropertyType DWord -Value 4 -Force    
    }
}