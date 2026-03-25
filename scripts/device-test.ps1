[CmdletBinding()] # -Verbose
param(
    [Parameter(Position = 0, Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [ValidateSet('android', 'ios')] # TODO , 'maccatalyst'
    [String] $Platform,

    [Switch] $Build,
    [Switch] $Run,
    [String] $Tfm
)

Set-StrictMode -Version latest
$ErrorActionPreference = 'Stop'
. $PSScriptRoot/device-test-utils.ps1

if (!$Build -and !$Run)
{
    $Build = $true
    $Run = $true
}
$CI = Test-Path env:CI

Push-Location $PSScriptRoot/..
try
{
    if (!$Tfm)
    {
        $Tfm = 'net10.0'
    }
    $arch = (!$IsWindows -and $(uname -m) -eq 'arm64') ? 'arm64' : 'x64'
    $udid = $null
    if ($Platform -eq 'android')
    {
        $Tfm += '-android'
        $group = 'android'
        $buildDir = $CI ? 'bin' : "test/Sentry.Maui.Device.TestApp/bin/Release/$Tfm/android-$arch"
        $arguments = @(
            '--app', "$buildDir/io.sentry.dotnet.maui.device.testapp-Signed.apk",
            '--package-name', 'io.sentry.dotnet.maui.device.testapp',
            '--launch-timeout', '00:10:00',
            '--timeout', '00:25:00',
            '--instrumentation', 'Sentry.Maui.Device.TestApp.SentryInstrumentation'
        )

        if ($CI)
        {
            $arguments += '--arg'
            $arguments += 'IsGitHubActions=true'
        }
    }
    elseif ($Platform -eq 'ios')
    {
        $Tfm += '-ios'
        $group = 'apple'
        $buildDir = "test/Sentry.Maui.Device.TestApp/bin/Release/$Tfm/iossimulator-$arch"
        $envValue = $CI ? 'true' : 'false'
        $arguments = @(
            '--app', "$buildDir/Sentry.Maui.Device.TestApp.app",
            '--target', 'ios-simulator-64',
            '--launch-timeout', '00:10:00',
            '--timeout', '00:25:00',
            '--set-env', "CI=$envValue"
        )

        $udid = Get-IosSimulatorUdid -Verbose
        if ($udid) {
            $arguments += @('--device', $udid)
        } else {
            Write-Host "No suitable simulator found; proceeding without a specific --device"
        }
    }

    if ($Build)
    {
        # We disable AOT for device tests: https://github.com/nsubstitute/NSubstitute/issues/834
        dotnet build -f $Tfm -c Release -p:EnableAot=false -p:NoSymbolStrip=true test/Sentry.Maui.Device.TestApp
        if ($LASTEXITCODE -ne 0)
        {
            throw 'Failed to build Sentry.Maui.Device.TestApp'
        }
    }

    if ($Run)
    {
        Install-XHarness
        Remove-Item -Recurse -Force test_output -ErrorAction SilentlyContinue

        if ($Platform -eq 'ios' -and $udid)
        {
            Write-Host "Ensuring simulator is booted and ready..."
            xcrun simctl boot $udid 2>&1 | Out-Null  # no-op if already booted
            xcrun simctl bootstatus $udid -b          # block until fully operational
        }

        try
        {
            if ($VerbosePreference)
            {
                $arguments += '-v'
            }

            $maxAttempts = 3
            for ($attempt = 1; $attempt -le $maxAttempts; $attempt++)
            {
                if ($attempt -gt 1)
                {
                    Write-Host "Retrying xharness (attempt $attempt of $maxAttempts)..."
                    Remove-Item -Recurse -Force test_output -ErrorAction SilentlyContinue
                    if ($Platform -eq 'ios' -and $udid)
                    {
                        Write-Host "Resetting simulator before retry..."
                        xcrun simctl shutdown $udid 2>&1 | Out-Null
                        xcrun simctl boot $udid 2>&1 | Out-Null
                        xcrun simctl bootstatus $udid -b
                    }
                }
                xharness $group test $arguments --output-directory=test_output
                if ($LASTEXITCODE -eq 0) { break }
            }

            if ($LASTEXITCODE -ne 0)
            {
                $testResultsXml = './test_output/TestResults.xml'
                if (Test-Path $testResultsXml)
                {
                    $failedTests = Select-String -Path $testResultsXml -Pattern 'result="Fail"'
                    if ($failedTests)
                    {
                        Write-Host "`nFailed tests:"
                        $failedTests | ForEach-Object { Write-Host $_.Line }
                    }
                }
                throw 'xharness run failed with non-zero exit code'
            }
        }
        finally
        {
            if ($CI)
            {
                $xmlFiles = Get-Item ./test_output/*.xml -ErrorAction SilentlyContinue
                if ($xmlFiles)
                {
                    scripts/parse-xunit2-xml.ps1 $xmlFiles.FullName | Out-File $env:GITHUB_STEP_SUMMARY
                }
            }
        }
    }
}
finally
{
    Pop-Location
}
