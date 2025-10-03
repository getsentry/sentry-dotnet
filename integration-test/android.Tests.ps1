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

Describe 'MAUI app' -ForEach @(
    @{ tfm = "net9.0-android35.0" }
) -Skip:(-not $script:emulator) {
    BeforeAll {
        Remove-Item -Path "$PSScriptRoot/mobile-app" -Recurse -Force -ErrorAction SilentlyContinue
        Copy-Item -Path "$PSScriptRoot/net9-maui" -Destination "$PSScriptRoot/mobile-app" -Recurse -Force
        Push-Location $PSScriptRoot/mobile-app

        function InstallAndroidApp
        {
            param([string] $Dsn)
            $dsn = $Dsn.Replace('http://', 'http://key@') + '/0'

            # replace {{SENTRY_DSN}} in MauiProgram.cs
            (Get-Content MauiProgram.cs) `
                -replace '\{\{SENTRY_DSN\}\}', $dsn `
            | Set-Content MauiProgram.cs

            $arch = [System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture.ToString().ToLower()
            $rid = "android-$arch"

            Write-Host "::group::Build Sentry.Maui.Device.IntegrationTestApp.csproj"
            dotnet build Sentry.Maui.Device.IntegrationTestApp.csproj `
                --configuration Release `
                --framework $tfm `
                --runtime $rid
            | ForEach-Object { Write-Host $_ }
            Write-Host '::endgroup::'
            $LASTEXITCODE | Should -Be 0

            Write-Host "::group::Install bin/Release/$tfm/$rid/io.sentry.dotnet.maui.device.integrationtestapp-Signed.apk"
            xharness android install -v `
                --app bin/Release/$tfm/$rid/io.sentry.dotnet.maui.device.integrationtestapp-Signed.apk `
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

            # Setup port forwarding for accessing sentry-server at 127.0.0.1:8000 from the emulator
            $port = $Dsn.Split(':')[2].Split('/')[0]
            xharness android adb -v -- reverse tcp:$port tcp:$port

            Write-Host "::group::Run Android app (TestArg=$TestArg)"
            xharness android adb -v `
                -- shell am start -S -n io.sentry.dotnet.maui.device.integrationtestapp/.MainActivity `
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

            xharness android adb -v -- reverse --remove tcp:$port
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
    }

    AfterAll {
        Pop-Location
    }

    AfterEach {
        UninstallAndroidApp
    }

    It 'Managed crash' {
        $result = Invoke-SentryServer {
            param([string]$url)
            InstallAndroidApp -Dsn $url
            RunAndroidApp -Dsn $url -TestArg "Managed"
            RunAndroidApp -Dsn $url
        }

        $result.HasErrors() | Should -BeFalse
        $result.Envelopes() | Should -AnyElementMatch "`"type`":`"System.ApplicationException`""
        $result.Envelopes() | Should -Not -AnyElementMatch "`"type`":`"SIGABRT`""
    }

    It 'Java crash' {
        $result = Invoke-SentryServer {
            param([string]$url)
            InstallAndroidApp -Dsn $url
            RunAndroidApp -Dsn $url -TestArg "Java"
            RunAndroidApp -Dsn $url
        }

        $result.HasErrors() | Should -BeFalse
        $result.Envelopes() | Should -AnyElementMatch "`"type`":`"RuntimeException`""
        $result.Envelopes() | Should -Not -AnyElementMatch "`"type`":`"System.\w+Exception`""
    }

    It 'Null reference exception' {
        $result = Invoke-SentryServer {
            param([string]$url)
            InstallAndroidApp -Dsn $url
            RunAndroidApp -Dsn $url -TestArg "NullReferenceException"
            RunAndroidApp -Dsn $url
        }

        $result.HasErrors() | Should -BeFalse
        $result.Envelopes() | Should -AnyElementMatch "`"type`":`"System.NullReferenceException`""
        # TODO: fix redundant RuntimeException (#3954)
        { $result.Envelopes() | Should -Not -AnyElementMatch "`"type`":`"SIGSEGV`"" } | Should -Throw
    }
}
