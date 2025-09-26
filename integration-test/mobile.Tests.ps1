# This file contains test cases for https://pester.dev/
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. $PSScriptRoot/pester.ps1
. $PSScriptRoot/../scripts/device-test-utils.ps1

BeforeDiscovery {
    # Skip iOS integration tests unless a simulator has already been booted by
    # iOS Device Tests, or manually when testing locally. This avoids slowing
    # down the macOS build job further.
    $script:udid = Get-IosSimulatorUdid -PreferredStates @('Booted')
}

Describe 'MAUI app' {
    BeforeAll {
        . $PSScriptRoot/../scripts/device-test-utils.ps1
        Install-XHarness

        Push-Location $PSScriptRoot/mobile-app
    }

    AfterAll {
        Pop-Location
    }

    Context 'on iOS (<tfm>)' -ForEach @(
        @{ tfm = "net9.0-ios18.0" }
    ) -Skip:(-not $IsMacOS -or -not $script:udid) {
        BeforeAll {
            $arch = ($(uname -m) -eq 'arm64') ? 'arm64' : 'x64'
            $rid = "iossimulator-$arch"
            $arguments = @(
                "-v",
                "--target=ios-simulator-64",
                "--device=$udid",
                "--output-directory=integration_test_output",
                "--timeout=00:25:00"
            )

            Write-Host "::group::Build Sentry.Maui.Device.IntegrationTestApp.csproj"
            dotnet build Sentry.Maui.Device.IntegrationTestApp.csproj `
                --configuration Release `
                --framework $tfm `
                --runtime $rid
            | ForEach-Object { Write-Host $_ }
            $LASTEXITCODE | Should -Be 0
            Write-Host '::endgroup::'

            function RunTestApp
            {
                param(
                    [string] $Dsn,
                    [string] $CrashType = 'None',
                    [string] $TestAction = 'None'
                )
                $Dsn = $Dsn.Replace('http://', 'http://key@') + '/0'
                Write-Host "::group::Run app (Crash=$CrashType, Action=$TestAction)"
                xharness apple just-run $arguments `
                    --app io.sentry.dotnet.maui.device.integrationtestapp `
                    --set-env SENTRY_DSN=$Dsn `
                    --set-env SENTRY_CRASH_TYPE=$CrashType `
                    --set-env SENTRY_TEST_ACTION=$TestAction
                | ForEach-Object { Write-Host $_ }
                $LASTEXITCODE | Should -Be 0
                Write-Host '::endgroup::'
            }
        }

        BeforeEach {
            Write-Host "::group::Install bin/Release/$tfm/$rid/Sentry.Maui.Device.IntegrationTestApp.app"
            xharness apple install $arguments `
                --app bin/Release/$tfm/$rid/Sentry.Maui.Device.IntegrationTestApp.app
            | ForEach-Object { Write-Host $_ }
            $LASTEXITCODE | Should -Be 0
            Write-Host '::endgroup::'
        }

        AfterEach {
            Write-Host "::group::Uninstall io.sentry.dotnet.maui.device.integrationtestapp"
            xharness apple uninstall $arguments `
                --app io.sentry.dotnet.maui.device.integrationtestapp
            | ForEach-Object { Write-Host $_ }
            $LASTEXITCODE | Should -Be 0
            Write-Host '::endgroup::'
        }

        It 'captures managed crash' {
            $result = Invoke-SentryServer {
                param([string]$url)
                RunTestApp -Dsn $url -CrashType "Managed"
                RunTestApp -Dsn $url -TestAction "Exit"
            }

            $result.HasErrors() | Should -BeFalse
            $result.Envelopes() | Should -AnyElementMatch "`"type`":`"System.ApplicationException`""
            # TODO: fix redundant SIGABRT (#3954)
            { $result.Envelopes() | Should -Not -AnyElementMatch "`"type`":`"SIGABRT`"" } | Should -Throw
        }

        It 'captures native crash' {
            $result = Invoke-SentryServer {
                param([string]$url)
                RunTestApp -Dsn $url -CrashType "Native"
                RunTestApp -Dsn $url -TestAction "Exit"
            }

            $result.HasErrors() | Should -BeFalse
            $result.Envelopes() | Should -AnyElementMatch "`"type`":`"EXC_[A-Z_]+`""
            $result.Envelopes() | Should -Not -AnyElementMatch "`"type`":`"System.\w+Exception`""
        }

        It 'captures null reference exception' {
            $result = Invoke-SentryServer {
                param([string]$url)
                RunTestApp -Dsn $url -TestAction "NullReferenceException"
                RunTestApp -Dsn $url -TestAction "Exit"
            }

            $result.HasErrors() | Should -BeFalse
            $result.Envelopes() | Should -AnyElementMatch "`"type`":`"System.NullReferenceException`""
            # TODO: fix redundant EXC_BAD_ACCESS (#3954)
            { $result.Envelopes() | Should -Not -AnyElementMatch "`"type`":`"EXC_BAD_ACCESS`"" } | Should -Throw
        }
    }
}
