# This file contains test cases for https://pester.dev/
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. $PSScriptRoot/pester.ps1
. $PSScriptRoot/../scripts/device-test-utils.ps1

BeforeDiscovery {
    # Skip Android integration tests unless an emulator has been already started
    # by Android Device Tests, or manually when testing locally. This avoids
    # slowing down non-Device Test CI builds further.
    Install-XHarness
    $script:emulator = Get-AndroidEmulatorId
}

Describe 'MAUI app (<tfm>, <configuration>)' -ForEach @(
    @{ tfm = "net9.0-android35.0"; configuration = "Release" }
    @{ tfm = "net9.0-android35.0"; configuration = "Debug" }
) -Skip:(-not $script:emulator) {
    BeforeAll {
        Remove-Item -Path "$PSScriptRoot/mobile-app" -Recurse -Force -ErrorAction SilentlyContinue
        Copy-Item -Path "$PSScriptRoot/net9-maui" -Destination "$PSScriptRoot/mobile-app" -Recurse -Force
        Push-Location $PSScriptRoot/mobile-app

        $arch = [System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture.ToString().ToLower()
        $rid = "android-$arch"

        Write-Host "::group::Build Sentry.Maui.Device.IntegrationTestApp.csproj"
        dotnet build Sentry.Maui.Device.IntegrationTestApp.csproj `
            --configuration $configuration `
            --framework $tfm `
            --runtime $rid
        | ForEach-Object { Write-Host $_ }
        Write-Host '::endgroup::'
        $LASTEXITCODE | Should -Be 0

        function InstallAndroidApp
        {
            Write-Host "::group::Install bin/$configuration/$tfm/$rid/io.sentry.dotnet.maui.device.integrationtestapp-Signed.apk"
            xharness android install -v `
                --app bin/$configuration/$tfm/$rid/io.sentry.dotnet.maui.device.integrationtestapp-Signed.apk `
                --package-name io.sentry.dotnet.maui.device.integrationtestapp `
                --output-directory=test_output
            | ForEach-Object { Write-Host $_ }
            Write-Host '::endgroup::'
            $LASTEXITCODE | Should -Be 0
        }

        function RunAndroidApp
        {
            param(
                [string] $Dsn,
                [string] $TestArg = 'None'
            )
            Write-Host "::group::Run Android app (TestArg=$TestArg)"
            $dsn = $Dsn.Replace('http://', 'http://key@') + '/0'
            xharness android adb -v `
                -- shell am start -S -n io.sentry.dotnet.maui.device.integrationtestapp/.MainActivity `
                -e SENTRY_DSN $dsn `
                -e SENTRY_TEST_ARG $TestArg
            | ForEach-Object { Write-Host $_ }
            Write-Host '::endgroup::'
            $LASTEXITCODE | Should -Be 0

            do
            {
                Write-Host "Waiting for app..."
                Start-Sleep -Seconds 1

                $procid = (& xharness android adb -- shell pidof "io.sentry.dotnet.maui.device.integrationtestapp") -replace '\s', ''
                $activity = (& xharness android adb -- shell dumpsys activity activities) -match "io\.sentry\.dotnet\.maui\.device\.integrationtestapp"

            } while ($procid -and $activity)
        }

        function UninstallAndroidApp
        {
            Write-Host "::group::Uninstall io.sentry.dotnet.maui.device.integrationtestapp"
            xharness android uninstall -v `
                --package-name io.sentry.dotnet.maui.device.integrationtestapp
            | ForEach-Object { Write-Host $_ }
            $LASTEXITCODE | Should -Be 0
            Write-Host '::endgroup::'
        }

        # Helper to dump server stderr if the test server reported errors
        function Dump-ServerErrors {
            param(
                [Parameter(Mandatory)]
                $Result
            )
            if ($Result.HasErrors()) {
                Write-Host '::group::sentry-server stderr'
                $Result.ServerStdErr | ForEach-Object { Write-Host $_ }
                Write-Host '::endgroup::'
            }
        }

        # Setup port forwarding for accessing sentry-server at 127.0.0.1:8000 from the emulator
        xharness android adb -v -- reverse tcp:8000 tcp:8000
    }

    AfterAll {
        Pop-Location
        xharness android adb -v -- reverse --remove tcp:8000
    }

    BeforeEach {
        InstallAndroidApp
    }

    AfterEach {
        UninstallAndroidApp
    }

    It 'Managed crash (<configuration>)' {
        $result = Invoke-SentryServer {
            param([string]$url)
            RunAndroidApp -Dsn $url -TestArg "Managed"
            RunAndroidApp -Dsn $url
        }

        Dump-ServerErrors -Result $result
        $result.HasErrors() | Should -BeFalse
        $result.Envelopes() | Should -AnyElementMatch "`"type`":`"System.ApplicationException`""
        $result.Envelopes() | Should -Not -AnyElementMatch "`"type`":`"SIGABRT`""
        $result.Envelopes() | Should -HaveCount 1
    }

    It 'Java crash (<configuration>)' {
        $result = Invoke-SentryServer {
            param([string]$url)
            RunAndroidApp -Dsn $url -TestArg "Java"
            RunAndroidApp -Dsn $url
        }

        Dump-ServerErrors -Result $result
        $result.HasErrors() | Should -BeFalse
        $result.Envelopes() | Should -AnyElementMatch "`"type`":`"RuntimeException`""
        $result.Envelopes() | Should -Not -AnyElementMatch "`"type`":`"System.\w+Exception`""
        $result.Envelopes() | Should -HaveCount 1
    }

    It 'Native crash (<configuration>)' {
        $result = Invoke-SentryServer {
            param([string]$url)
            RunAndroidApp -Dsn $url -TestArg "Native"
            RunAndroidApp -Dsn $url
        }

        Dump-ServerErrors -Result $result
        $result.HasErrors() | Should -BeFalse
        $result.Envelopes() | Should -AnyElementMatch "`"type`":`"SIG[A-Z]+`"" # SIGILL (x86_64), SIGTRAP (arm64-v8a)
        $result.Envelopes() | Should -Not -AnyElementMatch "`"type`":`"System.\w+Exception`""
        $result.Envelopes() | Should -HaveCount 1
    }

    It 'Null reference exception (<configuration>)' {
        $result = Invoke-SentryServer {
            param([string]$url)
            RunAndroidApp -Dsn $url -TestArg "NullReferenceException"
            RunAndroidApp -Dsn $url
        }

        Dump-ServerErrors -Result $result
        $result.HasErrors() | Should -BeFalse
        $result.Envelopes() | Should -AnyElementMatch "`"type`":`"System.NullReferenceException`""
        $result.Envelopes() | Should -Not -AnyElementMatch "`"type`":`"SIGSEGV`""
        $result.Envelopes() | Should -HaveCount 1
    }
}
