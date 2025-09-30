# This file contains test cases for https://pester.dev/
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. $PSScriptRoot/pester.ps1
. $PSScriptRoot/../scripts/device-test-utils.ps1

BeforeDiscovery {
    # Skip iOS and Android integration tests unless a simulator or an emulator
    # has already been booted by iOS or Android Device Tests, or manually when
    # testing locally. This avoids slowing down the macOS build job further.
    $script:iosDevice = Get-IosSimulatorUdid -PreferredStates @('Booted')
    if ($env:ANDROID_SERIAL)
    {
        $script:androidDevice = $env:ANDROID_SERIAL
    }
    else
    {
        try
        {
            $script:androidDevice = & adb devices | Select-String "device$" | ForEach-Object { ($_ -split "`t")[0] } | Select-Object -First 1
        }
        catch
        {
        }
    }
}

Describe 'MAUI app' {
    BeforeAll {
        . $PSScriptRoot/../scripts/device-test-utils.ps1
        Install-XHarness
    }

    BeforeEach {
        Remove-Item -Path "$PSScriptRoot/mobile-app" -Recurse -Force -ErrorAction SilentlyContinue
        Copy-Item -Path "$PSScriptRoot/net9-maui" -Destination "$PSScriptRoot/mobile-app" -Recurse -Force
        Push-Location $PSScriptRoot/mobile-app
    }

    AfterEach {
        Pop-Location
    }

    Context 'on iOS (<tfm>)' -ForEach @(
        @{ tfm = "net9.0-ios18.0" }
    ) -Skip:(-not $IsMacOS -or -not $script:iosDevice) {
        BeforeAll {
            $arch = ($(uname -m) -eq 'arm64') ? 'arm64' : 'x64'
            $rid = "iossimulator-$arch"
            $arguments = @(
                "-v",
                "--target=ios-simulator-64",
                "--device=$iosDevice",
                "--output-directory=integration_test_output",
                "--timeout=00:10:00"
            )

            Write-Host "::group::Build Sentry.Maui.Device.IntegrationTestApp.csproj"
            dotnet build Sentry.Maui.Device.IntegrationTestApp.csproj `
                --configuration Release `
                --framework $tfm `
                --runtime $rid
            | ForEach-Object { Write-Host $_ }
            $LASTEXITCODE | Should -Be 0
            Write-Host '::endgroup::'

            function RunIosApp
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
                RunIosApp -Dsn $url -CrashType "Managed"
                RunIosApp -Dsn $url -TestAction "Exit"
            }

            $result.HasErrors() | Should -BeFalse
            $result.Envelopes() | Should -AnyElementMatch "`"type`":`"System.ApplicationException`""
            # TODO: fix redundant SIGABRT (#3954)
            { $result.Envelopes() | Should -Not -AnyElementMatch "`"type`":`"SIGABRT`"" } | Should -Throw
        }

        It 'captures native crash' {
            $result = Invoke-SentryServer {
                param([string]$url)
                RunIosApp -Dsn $url -CrashType "Native"
                RunIosApp -Dsn $url -TestAction "Exit"
            }

            $result.HasErrors() | Should -BeFalse
            $result.Envelopes() | Should -AnyElementMatch "`"type`":`"EXC_[A-Z_]+`""
            $result.Envelopes() | Should -Not -AnyElementMatch "`"type`":`"System.\w+Exception`""
        }

        It 'captures null reference exception' {
            $result = Invoke-SentryServer {
                param([string]$url)
                RunIosApp -Dsn $url -TestAction "NullReferenceException"
                RunIosApp -Dsn $url -TestAction "Exit"
            }

            $result.HasErrors() | Should -BeFalse
            $result.Envelopes() | Should -AnyElementMatch "`"type`":`"System.NullReferenceException`""
            # TODO: fix redundant EXC_BAD_ACCESS (#3954)
            { $result.Envelopes() | Should -Not -AnyElementMatch "`"type`":`"EXC_BAD_ACCESS`"" } | Should -Throw
        }
    }

    Context 'on Android (<tfm>)' -ForEach @(
        @{ tfm = "net9.0-android35.0" }
    ) -Skip:(-not $script:androidDevice) {
        BeforeAll {
            function InstallAndroidApp
            {
                param([string] $Dsn)
                $dsn = $Dsn.Replace('http://', 'http://key@') + '/0'

                # replace {{SENTRY_DSN}} in MauiProgram.cs
                (Get-Content MauiProgram.cs) `
                    -replace '\{\{SENTRY_DSN\}\}', $dsn `
                | Set-Content MauiProgram.cs

                $arch = ($(uname -m) -eq 'arm64') ? 'arm64' : 'x64'
                $rid = "android-$arch"

                Write-Host "::group::Build Sentry.Maui.Device.IntegrationTestApp.csproj"
                dotnet build Sentry.Maui.Device.IntegrationTestApp.csproj `
                    --configuration Release `
                    --framework $tfm `
                    --runtime $rid
                | ForEach-Object { Write-Host $_ }
                $LASTEXITCODE | Should -Be 0
                Write-Host '::endgroup::'

                Write-Host "::group::Install bin/Release/$tfm/$rid/io.sentry.dotnet.maui.device.integrationtestapp-Signed.apk"
                xharness android install -v `
                    --app bin/Release/$tfm/$rid/io.sentry.dotnet.maui.device.integrationtestapp-Signed.apk `
                    --package-name io.sentry.dotnet.maui.device.integrationtestapp `
                    --output-directory=integration_test_output
                | ForEach-Object { Write-Host $_ }
                $LASTEXITCODE | Should -Be 0
                Write-Host '::endgroup::'
            }

            function RunAndroidApp
            {
                param(
                    [string] $Dsn,
                    [string] $CrashType = 'None',
                    [string] $TestAction = 'None'
                )

                # Setup port forwarding for accessing sentry-server at 127.0.0.1:8000 from the emulator
                $port = $Dsn.Split(':')[2].Split('/')[0]
                xharness android adb -v -- reverse tcp:$port tcp:$port

                Write-Host "::group::Run Android app"
                xharness android adb -v `
                    -- shell am start -S -n io.sentry.dotnet.maui.device.integrationtestapp/.MainActivity `
                    -e SENTRY_CRASH_TYPE $CrashType `
                    -e SENTRY_TEST_ACTION $TestAction
                | ForEach-Object { Write-Host $_ }
                $LASTEXITCODE | Should -Be 0

                do
                {
                    Write-Host "Waiting for app..."
                    Start-Sleep -Seconds 1

                    $procid = (& adb shell pidof "io.sentry.dotnet.maui.device.integrationtestapp") -replace '\s', ''
                    $activity = (& adb shell dumpsys activity activities) -match "io\.sentry\.dotnet\.maui\.device\.integrationtestapp"

                } while ($procid -and $activity)

                xharness android adb -v -- reverse --remove tcp:$port
                Write-Host '::endgroup::'
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

        AfterEach {
            UninstallAndroidApp
        }

        It 'Managed crash' {
            $result = Invoke-SentryServer {
                param([string]$url)
                InstallAndroidApp -Dsn $url
                RunAndroidApp -Dsn $url -CrashType "Managed"
                RunAndroidApp -Dsn $url -TestAction "Exit"
            }

            $result.HasErrors() | Should -BeFalse
            $result.Envelopes() | Should -AnyElementMatch "`"type`":`"System.ApplicationException`""
            $result.Envelopes() | Should -Not -AnyElementMatch "`"type`":`"SIGABRT`""
        }

        It 'Java crash' {
            $result = Invoke-SentryServer {
                param([string]$url)
                InstallAndroidApp -Dsn $url
                RunAndroidApp -Dsn $url -CrashType "Java"
                RunAndroidApp -Dsn $url -TestAction "Exit"
            }

            $result.HasErrors() | Should -BeFalse
            $result.Envelopes() | Should -AnyElementMatch "`"type`":`"RuntimeException`""
            $result.Envelopes() | Should -Not -AnyElementMatch "`"type`":`"System.\w+Exception`""
        }

        It 'Null reference exception' {
            $result = Invoke-SentryServer {
                param([string]$url)
                InstallAndroidApp -Dsn $url
                RunAndroidApp -Dsn $url -TestAction "NullReferenceException"
                RunAndroidApp -Dsn $url -TestAction "Exit"
            }

            $result.HasErrors() | Should -BeFalse
            $result.Envelopes() | Should -AnyElementMatch "`"type`":`"System.NullReferenceException`""
            # TODO: fix redundant RuntimeException (#3954)
            { $result.Envelopes() | Should -Not -AnyElementMatch "`"type`":`"SIGSEGV`"" } | Should -Throw
        }
    }
}
