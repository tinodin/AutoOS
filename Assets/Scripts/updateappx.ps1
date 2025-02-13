param (
    [string]$app
)

Add-Type -AssemblyName System.Runtime.WindowsRuntime
$asTaskGeneric = ([System.WindowsRuntimeSystemExtensions].GetMethods() | ? { $_.Name -eq 'AsTask' -and $_.GetParameters().Count -eq 1 -and $_.GetParameters()[0].ParameterType.Name -eq 'IAsyncOperation`1' })[0]
function Await($WinRtTask, $ResultType) {
    $asTask = $asTaskGeneric.MakeGenericMethod($ResultType)
    $netTask = $asTask.Invoke($null, @($WinRtTask))
    $netTask.Wait(-1) | Out-Null
    $netTask.Result
}

[Windows.ApplicationModel.Store.Preview.InstallControl.AppInstallManager,Windows.ApplicationModel.Store.Preview,ContentType=WindowsRuntime] | Out-Null
$appManager = New-Object -TypeName Windows.ApplicationModel.Store.Preview.InstallControl.AppInstallManager

try
{
    $updateOp = $appManager.UpdateAppByPackageFamilyNameAsync($app)
    $updateResult = Await $updateOp ([Windows.ApplicationModel.Store.Preview.InstallControl.AppInstallItem])
    while ($true)
    {
        if ($null -eq $updateResult)
        {
            break
        }

        Write-Host $updateResult.GetCurrentStatus().PercentComplete
        if ($updateResult.GetCurrentStatus().PercentComplete -eq 100)
        {
            break
        }
        Start-Sleep -Milliseconds 50
    }
}
catch [System.AggregateException]
{
    break
}

