# This file contains test cases for https://pester.dev/
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. $PSScriptRoot/../integration-test/common.ps1

Describe 'MAUI app' {
    BeforeAll {
        . $PSScriptRoot/ios-simulator-utils.ps1

        $tfm = "net9.0-ios18.0"
        $target = "ios-simulator-64"
        $arch = ($(uname -m) -eq 'arm64') ? 'arm64' : 'x64'
        $rid = "iossimulator-$arch"
        $udid = Get-IosSimulatorUdid -IosVersion '18.5' -Verbose

        $arguments = @(
            "-v",
            "--target=$target",
            "--output-directory=integration_test_output"
        )
        if ($udid)
        {
            $arguments += @("--device=$udid")
        }
        else
        {
            Write-Host "No suitable simulator found; proceeding without a specific --device"
        }

        Push-Location $PSScriptRoot/../test/Sentry.Maui.Device.IntegrationTestApp

        Write-Host "::group::Build Sentry.Maui.Device.IntegrationTestApp.csproj"
        dotnet build Sentry.Maui.Device.IntegrationTestApp.csproj `
            --configuration Release `
            --framework $tfm `
            --runtime $rid
        | ForEach-Object { Write-Host $_ }
        $LASTEXITCODE | Should -Be 0
        Write-Host '::endgroup::'
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

    AfterAll {
        Pop-Location
    }

    It 'Managed crash' {
        $result = Invoke-SentryServer {
            param([string]$url)
            $dsn = $url.Replace('http://', 'http://key@') + '/0'

            Write-Host "::group::Cause managed crash"
            xharness apple just-run $arguments `
                --app io.sentry.dotnet.maui.device.integrationtestapp `
                --set-env SENTRY_DSN=$dsn `
                --set-env SENTRY_CRASH_TYPE=Managed
            | ForEach-Object { Write-Host $_ }
            $LASTEXITCODE | Should -Be 0
            Write-Host '::endgroup::'

            Write-Host "::group::Re-run"
            xharness apple just-run $arguments `
                --app io.sentry.dotnet.maui.device.integrationtestapp `
                --set-env SENTRY_DSN=$dsn `
                --set-env SENTRY_TEST_ACTION=Exit
            | ForEach-Object { Write-Host $_ }
            $LASTEXITCODE | Should -Be 0
            Write-Host '::endgroup::'
        }

        $result.HasErrors() | Should -BeFalse
        $result.Envelopes() | Should -AnyElementMatch "`"type`":`"System.ApplicationException`""
        # TODO: fix redundant SIGABRT (#3954)
        { $result.Envelopes() | Should -Not -AnyElementMatch "`"type`":`"SIGABRT`"" } | Should -Throw
    }

    It 'Native crash' {
        $result = Invoke-SentryServer {
            param([string]$url)
            $dsn = $url.Replace('http://', 'http://key@') + '/0'

            Write-Host "::group::Cause native crash"
            xharness apple just-run $arguments `
                --app io.sentry.dotnet.maui.device.integrationtestapp `
                --set-env SENTRY_DSN=$dsn `
                --set-env SENTRY_CRASH_TYPE=Native
            | ForEach-Object { Write-Host $_ }
            $LASTEXITCODE | Should -Be 0
            Write-Host '::endgroup::'

            Write-Host "::group::Re-run"
            xharness apple just-run $arguments `
                --app io.sentry.dotnet.maui.device.integrationtestapp `
                --set-env SENTRY_DSN=$dsn `
                --set-env SENTRY_TEST_ACTION=Exit
            | ForEach-Object { Write-Host $_ }
            $LASTEXITCODE | Should -Be 0
            Write-Host '::endgroup::'
        }

        $result.HasErrors() | Should -BeFalse
        $result.Envelopes() | Should -AnyElementMatch "`"type`":`"EXC_[A-Z_]+`""
        $result.Envelopes() | Should -Not -AnyElementMatch "`"type`":`"System.\w+Exception`""
    }

    It 'Null reference exception' {
        $result = Invoke-SentryServer {
            param([string]$url)
            $dsn = $url.Replace('http://', 'http://key@') + '/0'

            Write-Host "::group::Trigger null reference exception"
            xharness apple just-run $arguments `
                --app io.sentry.dotnet.maui.device.integrationtestapp `
                --set-env SENTRY_DSN=$dsn `
                --set-env SENTRY_TEST_ACTION=NullReferenceException
            | ForEach-Object { Write-Host $_ }
            $LASTEXITCODE | Should -Be 0
            Write-Host '::endgroup::'

            Write-Host "::group::Re-run"
            xharness apple just-run $arguments `
                --app io.sentry.dotnet.maui.device.integrationtestapp `
                --set-env SENTRY_DSN=$dsn `
                --set-env SENTRY_TEST_ACTION=Exit
            | ForEach-Object { Write-Host $_ }
            $LASTEXITCODE | Should -Be 0
            Write-Host '::endgroup::'
        }

        $result.HasErrors() | Should -BeFalse
        $result.Envelopes() | Should -AnyElementMatch "`"type`":`"System.NullReferenceException`""
        # TODO: fix redundant EXC_BAD_ACCESS (#3954)
        { $result.Envelopes() | Should -Not -AnyElementMatch "`"type`":`"EXC_BAD_ACCESS`"" } | Should -Throw
    }
}
