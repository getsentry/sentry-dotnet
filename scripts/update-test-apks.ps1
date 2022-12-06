param([switch] $IfNotExist)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = "$PSScriptRoot/.."
$apkPrefix = "$repoRoot/test/Sentry.Tests/Internals/android"

function BuildAndroidSample([bool] $UseAssemblyStore, [bool] $UseAssemblyCompression)
{
    $sampleDir = "$repoRoot/samples/Sentry.Samples.Android"
    $outputApk = "$apkPrefix-Store=$UseAssemblyStore-Compressed=$UseAssemblyCompression.apk"

    if ($IfNotExist -and (Test-Path $outputApk))
    {
        Write-Host "$outputApk already exists, skipping build"
        return
    }

    Push-Location -Verbose $sampleDir
    try
    {
        # Need to do a clean build otherwise some DLLs would end up being compressed even if it's disabled on this run.
        git clean -ffxd . | Out-Host

        dotnet build --configuration Release `
            --property:AndroidUseAssemblyStore=$UseAssemblyStore `
            --property:AndroidEnableAssemblyCompression=$UseAssemblyCompression `
        | Out-Host
        if ($LASTEXITCODE -ne 0)
        {
            exit $LASTEXITCODE
        }
    }
    finally
    {
        Pop-Location
    }

    Move-Item -Verbose "$sampleDir/bin/Release/*/io.sentry.dotnet.samples.android-Signed.apk" $outputApk
}

if (!$IfNotExist)
{
    Remove-Item -Verbose "$apkPrefix-*.apk"
}
BuildAndroidSample $true $true
BuildAndroidSample $true $false
BuildAndroidSample $false $true
BuildAndroidSample $false $false
