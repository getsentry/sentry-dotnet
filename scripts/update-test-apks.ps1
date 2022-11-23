Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$apkPrefix = 'test/Sentry.Tests/Internals/android'

function BuildAndroidSample([bool] $UseAssemblyStore, [bool] $UseAssemblyCompression)
{
    $sampleDir = 'samples/Sentry.Samples.Android'
    $framework = 'net6.0-android'
    $outputApk = "$apkPrefix-Store=$UseAssemblyStore-Compressed=$UseAssemblyCompression.apk"

    Push-Location -Verbose $sampleDir
    try
    {
        # Need to do a clean build otherwise some DLLs would end up being compressed even if it's disabled on this run.
        git clean -ffxd . | Out-Host

        dotnet build --configuration Release --framework $framework `
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

    Move-Item -Verbose "$sampleDir/bin/Release/$framework/io.sentry.dotnet.samples.android-Signed.apk" $outputApk
}

Remove-Item -Verbose "$apkPrefix-*.apk"
BuildAndroidSample $true $true
BuildAndroidSample $true $false
BuildAndroidSample $false $true
BuildAndroidSample $false $false