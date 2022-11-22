Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function BuildAndroidSample([bool] $UseAssemblyStore)
{
    $sampleDir = 'samples/Sentry.Samples.Android'
    $framework = "net6.0-android"
    $outputApk = "test/Sentry.Tests/Internals/android-AssemblyStore=$UseAssemblyStore.apk"

    Push-Location -Verbose $sampleDir
    try
    {
        git clean -ffxd . | Out-Host
        dotnet build --configuration Release --framework $framework --property:AndroidUseAssemblyStore=$UseAssemblyStore | Out-Host
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

BuildAndroidSample $true
BuildAndroidSample $false