param (
    [string]$AppxPackageFamilyName,
    [string]$filetype,
    [string]$architecture
)

$DownloadedFiles = @()
$errored = $false
$allFilesDownloaded = $true

$apiUrl = "https://store.rg-adguard.net/api/GetFiles"
$versionRing = "Retail"

if (-not $architecture) {
    $architecture = switch ($env:PROCESSOR_ARCHITECTURE) {
        "x86" { "x86" }
        { @("x64", "amd64") -contains $_ } { "x64" }
        "arm" { "arm" }
        "arm64" { "arm64" }
        default { "neutral" }
    }
}

$filetype = if ($filetype) { $filetype.ToLower() } else { 'msixbundle' }

$body = @{
    type = 'PackageFamilyName'
    url  = $AppxPackageFamilyName
    ring = $versionRing
    lang = 'en-US'
}

$raw = $null
try {
    $raw = Invoke-RestMethod -Method Post -Uri $apiUrl -ContentType 'application/x-www-form-urlencoded' -Body $body -UserAgent $UserAgent
} catch {
    $errorMsg = "An error occurred: " + $_
    Write-Host $errorMsg
    $errored = $true
    return $false
}

[Collections.Generic.Dictionary[string, Collections.Generic.Dictionary[string, array]]] $packageList = @{ }
$patternUrlAndText = '<tr style.*<a href=\"(?<url>.*)"\s.*>(?<text>.*\.(app|msi)x.*)<\/a>'
$raw | Select-String $patternUrlAndText -AllMatches | ForEach-Object { $_.Matches } | ForEach-Object {
    $url = ($_.Groups['url']).Value
    $text = ($_.Groups['text']).Value
    $textSplitUnderscore = $text.split('_')
    $name = $textSplitUnderscore.split('_')[0]
    $version = $textSplitUnderscore.split('_')[1]
    $arch = ($textSplitUnderscore.split('_')[2]).ToLower()
    $publisherId = ($textSplitUnderscore.split('_')[4]).split('.')[0]
    $textSplitPeriod = $text.split('.')
    $type = ($textSplitPeriod[$textSplitPeriod.length - 1]).ToLower()

    if (!($packageList.keys -match ('^' + [Regex]::escape($name) + '$'))) {
        $packageList["$name"] = @{ }
    }

    if (!(($packageList["$name"]).keys -match ('^' + [Regex]::escape($version) + '$'))) {
        ($packageList["$name"])["$version"] = @()
    }

    ($packageList["$name"])["$version"] += @{
        url         = $url
        filename    = $text
        name        = $name
        version     = $version
        arch        = $arch
        publisherId = $publisherId
        type        = $type
    }
}

$latestPackages = @()
$packageList.GetEnumerator() | ForEach-Object { ($_.value).GetEnumerator() | Select-Object -Last 1 } | ForEach-Object {
    $packagesByType = $_.value
    $msixbundle = ($packagesByType | Where-Object { $_.type -match "^msixbundle$" })
    $appxbundle = ($packagesByType | Where-Object { $_.type -match "^appxbundle$" })
    $msix = ($packagesByType | Where-Object { ($_.type -match "^msix$") -And ($_.arch -match ('^' + [Regex]::Escape($architecture) + '$')) })
    $appx = ($packagesByType | Where-Object { ($_.type -match "^appx$") -And ($_.arch -match ('^' + [Regex]::Escape($architecture) + '$')) })
    
    if ($filetype -eq "msixbundle" -and $msixbundle) { $latestPackages += $msixbundle }
    elseif ($filetype -eq "appxbundle" -and $appxbundle) { $latestPackages += $appxbundle }
    elseif ($filetype -eq "msix" -and $msix) { $latestPackages += $msix }
    elseif ($filetype -eq "appx" -and $appx) { $latestPackages += $appx }
}

$latestPackages | ForEach-Object {
    Write-Host "$($_.url)"
}