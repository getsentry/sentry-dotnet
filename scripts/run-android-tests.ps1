Set-StrictMode -Version latest
$ErrorActionPreference = "Stop"

Push-Location $PSScriptRoot/..
try
{
    if (!(Get-Command xharness -ErrorAction SilentlyContinue))
    {
        dotnet tool install Microsoft.DotNet.XHarness.CLI --global --version "1.*-*"
    }

    $tfm = 'net7.0-android'
    dotnet build -f $tfm test/Sentry.Maui.Device.TestApp
    if ($LASTEXITCODE -ne 0)
    {
        throw "Failed to build Sentry.Maui.Device.TestApp"
    }

    $arch = $(uname -m) -eq 'arm64' ? 'arm64' : 'x64'
    Remove-Item -Recurse -Force test_output -ErrorAction SilentlyContinue
    xharness android test `
        --app=test/Sentry.Maui.Device.TestApp/bin/Debug/$tfm/android-$arch/io.sentry.dotnet.maui.device.testapp-Signed.apk `
        --package-name=io.sentry.dotnet.maui.device.testapp `
        --output-directory=test_output
}
finally
{
    Pop-Location
}
