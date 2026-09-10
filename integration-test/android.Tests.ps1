param(
    [string] $dotnet_version = "net10.0"
)

# This file contains test cases for https://pester.dev/
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. $PSScriptRoot/pester.ps1
. $PSScriptRoot/common.ps1
. $PSScriptRoot/../scripts/device-test-utils.ps1

BeforeDiscovery {
    # Skip Android integration tests unless an emulator has been already started
    # by Android Device Tests, or manually when testing locally. This avoids
    # slowing down non-Device Test CI builds further.
    Install-XHarness
    $script:emulator = Get-AndroidEmulatorId
}

# CoreCLR runs on every framework we still support. Mono is tested only where it is supported:
# as of .NET 11 preview 6 CoreCLR is the only runtime for MAUI mobile apps and the UseMonoRuntime
# property was removed, so net11.0 is CoreCLR-only. See
# https://devblogs.microsoft.com/dotnet/coreclr-progress-and-mono-timeline-dotnet-maui/
$cases = @(
    @{ configuration = 'Release'; runtime = 'coreclr' }
    @{ configuration = 'Debug';   runtime = 'coreclr' }
)
if ([version]($dotnet_version -replace '^net', '') -lt [version]'11.0') {
    $cases += @(
        @{ configuration = 'Release'; runtime = 'mono' }
        @{ configuration = 'Debug';   runtime = 'mono' }
    )
}
Describe 'MAUI app (<dotnet_version>, <configuration>, <runtime>)' -ForEach $cases -Skip:(-not $script:emulator) {
    BeforeAll {
        $tfm = "$dotnet_version-android$(GetAndroidTpv $dotnet_version)"

        Remove-Item -Path "$PSScriptRoot/mobile-app" -Recurse -Force -ErrorAction SilentlyContinue
        # Note: maui-device, not maui-app. cli.Tests.ps1 generates a fresh app from the
        # MAUI template at integration-test/maui-app and deletes whatever is there first, so
        # this source app must not share that name.
        Copy-Item -Path "$PSScriptRoot/maui-device" -Destination "$PSScriptRoot/mobile-app" -Recurse -Force
        Push-Location $PSScriptRoot/mobile-app

        # replace {{SENTRY_DSN}} in MauiProgram.cs
        (Get-Content MauiProgram.cs) `
            -replace '\{\{SENTRY_DSN\}\}', 'http://key@127.0.0.1:8000/0' `
        | Set-Content MauiProgram.cs

        $arch = [System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture.ToString().ToLower()
        $rid = "android-$arch"

        Write-Host "::group::Build Sentry.Maui.Device.IntegrationTestApp.csproj"
        $useMonoRuntime = if ($runtime -eq 'mono') { 'true' } else { 'false' }
        # Set UseMonoRuntime on the app project, scoped to the framework under test, rather than
        # passing it with -p. As a global property it reaches every framework in the graph during
        # restore, and it has to be set at restore time so the right runtime pack is downloaded
        # (NETSDK1112).
        #
        # Set UseMonoRuntime on the app project, scoped to the framework under test, rather than
        # passing it with -p. As a global property it reaches every framework in the graph during
        # restore - including net11.0-android, where the SDK rejects it (NETSDK1242) - and it has
        # to be set at restore time so the right runtime pack is downloaded (NETSDK1112).
        (Get-Content Sentry.Maui.Device.IntegrationTestApp.csproj) `
            -replace '<UseMaui>true</UseMaui>', ("<UseMaui>true</UseMaui>`n    " + `
                "<UseMonoRuntime Condition=`"'`$(TargetFramework)' == '$tfm'`">$useMonoRuntime</UseMonoRuntime>") `
        | Set-Content Sentry.Maui.Device.IntegrationTestApp.csproj

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
        $result.Events() | Should -AnyElementMatch "`"type`":`"System.ApplicationException`""
        $result.Events() | Should -Not -AnyElementMatch "`"type`":`"SIGABRT`""
        $result.Events() | Should -HaveCount 1
    }

    It 'Java crash (<configuration>)' {
        $result = Invoke-SentryServer {
            param([string]$url)
            RunAndroidApp -Dsn $url -TestArg "Java"
            RunAndroidApp -Dsn $url
        }

        Dump-ServerErrors -Result $result
        $result.HasErrors() | Should -BeFalse
        $result.Events() | Should -AnyElementMatch "`"type`":`"RuntimeException`""
        $result.Events() | Should -Not -AnyElementMatch "`"type`":`"System.\w+Exception`""
        $result.Events() | Should -HaveCount 1
    }

    It 'Native crash (<configuration>)' {
        $result = Invoke-SentryServer {
            param([string]$url)
            RunAndroidApp -Dsn $url -TestArg "Native"
            RunAndroidApp -Dsn $url
        }

        Dump-ServerErrors -Result $result
        $result.HasErrors() | Should -BeFalse
        $result.Events() | Should -AnyElementMatch "`"type`":`"SIGSEGV`""
        $result.Events() | Should -Not -AnyElementMatch "`"type`":`"System.\w+Exception`""
        $result.Events() | Should -HaveCount 1
    }

    It 'Null reference exception (<configuration>)' {
        $result = Invoke-SentryServer {
            param([string]$url)
            RunAndroidApp -Dsn $url -TestArg "NullReferenceException"
            RunAndroidApp -Dsn $url
        }

        Dump-ServerErrors -Result $result
        $result.HasErrors() | Should -BeFalse
        $result.Events() | Should -AnyElementMatch "`"type`":`"System.NullReferenceException`""
        $result.Events() | Should -Not -AnyElementMatch "`"type`":`"SIGSEGV`""
        $result.Events() | Should -HaveCount 1
    }

    It 'Delivers battery breadcrumbs in main thread (<configuration>)' {
        $result = Invoke-SentryServer {
            param([string]$url)
            RunAndroidApp -Dsn $url -TestArg "BATTERY_CHANGED"
        }

        Dump-ServerErrors -Result $result
        $result.HasErrors() | Should -BeFalse
        $result.Events() | Should -AnyElementMatch "`"type`":`"system`",`"thread_id`":`"1`",`"category`":`"device.event`",`"action`":`"BATTERY_CHANGED`""
        $result.Events() | Should -HaveCount 1
    }

    It 'Delivers network breadcrumbs in main thread (<configuration>)' {
        $result = Invoke-SentryServer {
            param([string]$url)
            RunAndroidApp -Dsn $url -TestArg "NETWORK_CAPABILITIES_CHANGED"
        }

        Dump-ServerErrors -Result $result
        $result.HasErrors() | Should -BeFalse
        $result.Events() | Should -AnyElementMatch "`"type`":`"system`",`"thread_id`":`"1`",`"category`":`"network.event`",`"action`":`"NETWORK_CAPABILITIES_CHANGED`""
        $result.Events() | Should -HaveCount 1
    }
}
