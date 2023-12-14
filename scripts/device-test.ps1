param(
    [Parameter(Position = 0, Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [ValidateSet('android', 'ios')] # TODO , 'maccatalyst'
    [String] $Platform,

    [Switch] $Build,
    [Switch] $Run,
    [Switch] $NativeAot
)

Set-StrictMode -Version latest
$ErrorActionPreference = 'Stop'

if (!$Build -and !$Run)
{
    $Build = $true
    $Run = $true
}
$CI = Test-Path env:CI

Push-Location $PSScriptRoot/..
try
{
    $tfm = 'net8.0-'
    $arch = (!$IsWindows -and $(uname -m) -eq 'arm64') ? 'arm64' : 'x64'
    if ($Platform -eq 'android')
    {
        $tfm += 'android'
        $group = 'android'
        $buildDir = $CI ? 'bin' : "test/Sentry.Maui.Device.TestApp/bin/Release/$tfm/android-$arch"
        $arguments = @(
            '--app', "$buildDir/io.sentry.dotnet.maui.device.testapp-Signed.apk",
            '--package-name', 'io.sentry.dotnet.maui.device.testapp'
        )
    }
    elseif ($Platform -eq 'ios')
    {
        $tfm += 'ios'
        $group = 'apple'
        $buildDir = $CI ? 'bin' : "test/Sentry.Maui.Device.TestApp/bin/Release/$tfm/iossimulator-$arch"
        $arguments = @(
            '--app', "$buildDir/Sentry.Maui.Device.TestApp.app",
            '--target', 'ios-simulator-64_17.0',
            '--launch-timeout', '00:10:00'
        )
    }
    # elseif ($Platform -eq 'maccatalyst')
    # {
    #     $tfm += 'maccatalyst'
    #     $group = 'apple'
    #     $buildDir = $CI ? 'bin' : "test/Sentry.Maui.Device.TestApp/bin/Release/$tfm/iossimulator-$arch"
    #     $arguments = @(
    #         '--app', "$buildDir/Sentry.Maui.Device.TestApp.app",
    #         '--launch-timeout', '00:10:00'
    #     )
    # }

    if ($Build)
    {
        dotnet build -f $tfm -c Release test/Sentry.Maui.Device.TestApp -p:TestNativeAot=$NativeAot
        if ($LASTEXITCODE -ne 0)
        {
            throw 'Failed to build Sentry.Maui.Device.TestApp'
        }
    }

    if ($Run)
    {
        if (!(Get-Command xharness -ErrorAction SilentlyContinue))
        {
            Push-Location ($CI ? $env:RUNNER_TEMP : $IsWindows ? $env:TMP : $IsMacos ? $env:TMPDIR : '/temp')
            dotnet tool install Microsoft.DotNet.XHarness.CLI --global --version '9.*-*' `
                --add-source https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-eng/nuget/v3/index.json
            Pop-Location
        }

        Remove-Item -Recurse -Force test_output -ErrorAction SilentlyContinue
        try
        {
            xharness $group test $arguments --output-directory=test_output
            if ($LASTEXITCODE -ne 0)
            {
                throw 'xharness run failed with non-zero exit code'
            }
        }
        finally
        {
            if ($CI)
            {
                scripts/parse-xunit2-xml.ps1 (Get-Item ./test_output/*.xml).FullName | Out-File $env:GITHUB_STEP_SUMMARY
            }

        }
    }
}
finally
{
    Pop-Location
}
