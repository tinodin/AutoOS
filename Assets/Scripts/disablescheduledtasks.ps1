param (
    [string]$path
)

function Toggle-Task($task, $enable) {
    $toggle = if ($enable) { "/enable" } else { "/disable" }

    $userTaskResult = (Start-Process "schtasks.exe" -ArgumentList "/change $($toggle) /tn `"$($task)`"" -PassThru -Wait -WindowStyle Hidden).ExitCode
    $trustedinstallerTaskResult = [int](& "$path" -U:T -P:E -Wait -ShowWindowMode:Hide cmd /c "schtasks.exe /change $($toggle) /tn `"$($task)`" > nul 2>&1 && echo 0 || echo 1")

    return $userTaskResult -band $trustedinstallerTaskResult
}

function main() {

    Set-Location $PSScriptRoot

    $wildcards = @(
        "systemsoundsservice",
        "cachetask",
        "update",
        "helloface",
        "customer experience improvement program",
        "microsoft compatibility appraiser",
        "startupapptask",
        "dssvccleanup",
        "bitlocker",
        "chkdsk",
        "data integrity scan",
        "defrag",
        "languagecomponentsinstaller",
        "upnp",
        "windows filtering platform",
        "tpm",
        "speech",
        "spaceport",
        "power efficiency",
        "cloudexperiencehost",
        "diagnosis",
        "file history",
        "bgtaskregistrationmaintenancetask",
        "autochk\proxy",
        "siuf",
        "device information",
        "edp policy manager",
        "defender",
        "marebackup"
    )

    $scheduledTasks = schtasks /query /fo list
    $taskNames = [System.Collections.ArrayList]@()

    foreach ($line in $scheduledTasks) {
        if ($line.contains("TaskName:")) {
            ($taskNames.Add($line.Split(":")[1].Trim().ToLower())) 2>&1 > $null
        }
    }

    $matchedTasks = @()
    foreach ($wildcard in $wildcards) {
        foreach ($task in $taskNames) {
            if ($task.contains($wildcard)) {
                $matchedTasks += $task
            }
        }
    }

    $totalTasks = $matchedTasks.Count
    $processedTasks = 0

    foreach ($task in $matchedTasks) {
        if ((Toggle-Task -task $task -enable $false) -ne 0) {
            return 1
        }

        $processedTasks++
        $percent = [math]::Round(($processedTasks / $totalTasks) * 100)
        Write-Host $percent
    }
    return 0
}

$_exitCode = main
Write-Host
exit $_exitCode
