Set-StrictMode -Version latest
$ErrorActionPreference = "Stop"

Push-Location $PSScriptRoot/..
try
{
    if (!(Get-Command xharness -ErrorAction SilentlyContinue))
    {
        dotnet tool install Microsoft.DotNet.XHarness.CLI --global --version "1.*-*"
    }

    $tfm = 'net7.0-ios'
    dotnet build -f $tfm test/Sentry.Maui.Device.TestApp
    if ($LASTEXITCODE -ne 0)
    {
        throw "Failed to build Sentry.Maui.Device.TestApp"
    }

    $arch = $(uname -m) -eq 'arm64' ? 'arm64' : 'x64'
    Remove-Item -Recurse -Force test_output -ErrorAction SilentlyContinue
    xharness apple test `
        --app=test/Sentry.Maui.Device.TestApp/bin/Debug/$tfm/iossimulator-$arch/Sentry.Maui.Device.TestApp.app `
        --target=ios-simulator-64 `
        --output-directory=test_output
}
finally
{
    Pop-Location
}
