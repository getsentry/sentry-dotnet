# This file contains test cases for https://pester.dev/
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. $PSScriptRoot/../integration-test/common.ps1

Describe 'MAUI app' {
    BeforeAll {
        . $PSScriptRoot/ios-simulator-utils.ps1
    }
    It 'Produces the expected exceptions' {
        $result = Invoke-SentryServer {
            Param([string]$url)
            $dsn = $url.Replace('http://', 'http://key@') + '/0'

            Push-Location $PSScriptRoot/../samples/Sentry.Samples.Maui
            try
            {
                $tfm = "net9.0-ios18.0"
                $target = "ios-simulator-64"
                $arch = ($(uname -m) -eq 'arm64') ? 'arm64' : 'x64'
                $rid = "iossimulator-$arch"
                $udid = Get-IosSimulatorUdid -IosVersion '18.5' -Verbose

                $arguments = @(
                    "-v",
                    "--target=$target",
                    "--output-directory=test_output"
                )
                if ($udid) {
                    $arguments += @("--device=$udid")
                } else {
                    Write-Host "No suitable simulator found; proceeding without a specific --device"
                }

                Write-Host "::group::Build"
                $env:SENTRY_DSN = $dsn
                dotnet build Sentry.Samples.Maui.csproj `
                    --configuration Release `
                    --framework $tfm `
                    --runtime $rid
                | ForEach-Object { Write-Host $_ }
                Write-Host '::endgroup::'

                Write-Host "::group::Install"
                xharness apple install $arguments `
                    --app bin/Release/$tfm/$rid/Sentry.Samples.Maui.app
                | ForEach-Object { Write-Host $_ }
                Write-Host '::endgroup::'

                Write-Host "::group::Crash"
                xharness apple just-run $arguments `
                    --app io.sentry.dotnet.samples.maui `
                    --set-env SENTRY_DSN=$dsn `
                    --set-env SENTRY_CRASH_TYPE=Managed
                | ForEach-Object { Write-Host $_ }
                Write-Host '::endgroup::'

                Write-Host "::group::Re-run"
                xharness apple just-run $arguments `
                    --app io.sentry.dotnet.samples.maui `
                    --set-env SENTRY_DSN=$dsn `
                    --set-env SENTRY_CRASH_TYPE=Exit
                | ForEach-Object { Write-Host $_ }
                Write-Host '::endgroup::'

                Write-Host "::group::Uninstall"
                xharness apple uninstall $arguments `
                    --app io.sentry.dotnet.samples.maui
                | ForEach-Object { Write-Host $_ }
                Write-Host '::endgroup::'
            }
            finally
            {
                Pop-Location
            }
        }

        $result.HasErrors() | Should -BeFalse
        $result.Envelopes() | Should -AnyElementMatch "`"type`":`"System.ApplicationException`""
        # TODO: fix redundant SIGABRT (#3954)
        { $result.Envelopes() | Should -Not -AnyElementMatch "`"type`":`"SIGABRT`"" } | Should -Throw
    }
}
