# This file contains test cases for https://pester.dev/
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. $PSScriptRoot/pester.ps1
. $PSScriptRoot/../scripts/device-test-utils.ps1

BeforeDiscovery {
    # Skip iOS integration tests unless a simulator has already been booted by
    # iOS Device Tests, or manually when testing locally. This avoids slowing
    # down the macOS build job further.
    $script:simulator = Get-IosSimulatorUdid -PreferredStates @('Booted')
}

Describe 'iOS app (<tfm>)' -ForEach @(
    @{ tfm = "net9.0-ios18.0" }
) -Skip:(-not $script:simulator) {
    BeforeAll {
        . $PSScriptRoot/../scripts/device-test-utils.ps1

        Remove-Item -Path "$PSScriptRoot/mobile-app" -Recurse -Force -ErrorAction SilentlyContinue
        Copy-Item -Path "$PSScriptRoot/net9-maui" -Destination "$PSScriptRoot/mobile-app" -Recurse -Force
        Push-Location $PSScriptRoot/mobile-app

        $arch = [System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture.ToString().ToLower()
        $rid = "iossimulator-$arch"

        Write-Host "::group::Build Sentry.Maui.Device.IntegrationTestApp.csproj"
        dotnet build Sentry.Maui.Device.IntegrationTestApp.csproj `
            --configuration Release `
            --framework $tfm `
            --runtime $rid
        | ForEach-Object { Write-Host $_ }
        Write-Host '::endgroup::'
        $LASTEXITCODE | Should -Be 0

        function InstallIosApp
        {
            Write-Host "::group::Install bin/Release/$tfm/$rid/Sentry.Maui.Device.IntegrationTestApp.app"
            xcrun simctl install $simulator `
                bin/Release/$tfm/$rid/Sentry.Maui.Device.IntegrationTestApp.app
            | ForEach-Object { Write-Host $_ }
            Write-Host '::endgroup::'
            $LASTEXITCODE | Should -Be 0
        }

        function UninstallIosApp
        {
            Write-Host "::group::Uninstall io.sentry.dotnet.maui.device.integrationtestapp"
            xcrun simctl uninstall $simulator `
                io.sentry.dotnet.maui.device.integrationtestapp
            | ForEach-Object { Write-Host $_ }
            Write-Host '::endgroup::'
            $LASTEXITCODE | Should -Be 0
        }

        function RunIosApp
        {
            param(
                [string] $Dsn,
                [string] $TestArg = 'None'
            )
            $Dsn = $Dsn.Replace('http://', 'http://key@') + '/0'
            Write-Host "::group::Run iOS app (TestArg=$TestArg)"
            xcrun simctl spawn $simulator launchctl setenv SENTRY_DSN $Dsn
            xcrun simctl spawn $simulator launchctl setenv SENTRY_TEST_ARG $TestArg
            xcrun simctl launch `
                --console `
                --terminate-running-process `
                $simulator `
                io.sentry.dotnet.maui.device.integrationtestapp
            | ForEach-Object { Write-Host $_ }
            Write-Host '::endgroup::'
            $LASTEXITCODE | Should -Be 0
        }
    }

    AfterAll {
        Pop-Location
    }

    BeforeEach {
        InstallIosApp
    }

    AfterEach {
        UninstallIosApp
    }

    It 'captures managed crash' {
        $result = Invoke-SentryServer {
            param([string]$url)
            RunIosApp -Dsn $url -TestArg "Managed"
            RunIosApp -Dsn $url
        }

        $result.HasErrors() | Should -BeFalse
        $result.Envelopes() | Should -AnyElementMatch "`"type`":`"System.ApplicationException`""
        # TODO: fix redundant SIGABRT (#3954)
        { $result.Envelopes() | Should -Not -AnyElementMatch "`"type`":`"SIGABRT`"" } | Should -Throw
    }

    It 'captures native crash' {
        $result = Invoke-SentryServer {
            param([string]$url)
            RunIosApp -Dsn $url -TestArg "Native"
            RunIosApp -Dsn $url
        }

        $result.HasErrors() | Should -BeFalse
        $result.Envelopes() | Should -AnyElementMatch "`"type`":`"EXC_[A-Z_]+`""
        $result.Envelopes() | Should -Not -AnyElementMatch "`"type`":`"System.\w+Exception`""
    }

    It 'captures null reference exception' {
        $result = Invoke-SentryServer {
            param([string]$url)
            RunIosApp -Dsn $url -TestArg "NullReferenceException"
            RunIosApp -Dsn $url
        }

        $result.HasErrors() | Should -BeFalse
        $result.Envelopes() | Should -AnyElementMatch "`"type`":`"System.NullReferenceException`""
        # TODO: fix redundant EXC_BAD_ACCESS (#3954)
        { $result.Envelopes() | Should -Not -AnyElementMatch "`"type`":`"EXC_BAD_ACCESS`"" } | Should -Throw
    }
}
