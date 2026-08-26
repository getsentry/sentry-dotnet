param(
    [ValidateSet('iOSSimulator', 'iOSDevice')]
    [string] $Platform = 'iOSSimulator',
    [string] $Target,
    [string] $Tfm = 'net10.0-ios26.5',
    [ValidateSet('Debug', 'Release')]
    [string[]] $Configuration = @('Release', 'Debug'),
    [ValidateSet('mono', 'coreclr')]
    [string[]] $Runtime = @('mono'),
    [ValidateRange(1, 300)]
    [int] $DuplicateObservationSeconds = 30,
    [string] $CodesignKey,
    [string] $CodesignProvision
)

# Sentry-backed iOS end-to-end crash tests.
#
# Required environment variables:
#   SENTRY_E2E_DSN        DSN for the dedicated E2E project
#   SENTRY_E2E_AUTH_TOKEN API token with event:read access
#
# The simulator is used by default. To run on a physical device:
#   pwsh integration-test/ios.e2e.Tests.ps1 -Platform iOSDevice -Target <UDID>
#
# CodesignKey and CodesignProvision are optional MSBuild overrides for device
# builds. If omitted, the installed signing configuration is auto-detected.

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. $PSScriptRoot/pester.ps1

$cases = foreach ($configurationValue in $Configuration) {
    foreach ($runtimeValue in $Runtime) {
        @{
            configuration = $configurationValue
            runtime       = $runtimeValue
            platform      = $Platform
        }
    }
}

Describe 'iOS E2E app (<configuration>, <runtime>, <platform>)' -ForEach $cases {
    BeforeAll {
        $script:locationPushed = $false
        $script:deviceConnected = $false
        $script:apiConnected = $false
        $script:observedEvents = @()

        if ([string]::IsNullOrEmpty($env:SENTRY_E2E_DSN)) {
            throw 'SENTRY_E2E_DSN environment variable is not set.'
        }
        if ([string]::IsNullOrEmpty($env:SENTRY_E2E_AUTH_TOKEN)) {
            throw 'SENTRY_E2E_AUTH_TOKEN environment variable is not set.'
        }

        . $PSScriptRoot/../modules/app-runner/import-modules.ps1

        $script:bundleId = 'io.sentry.dotnet.maui.device.integrationtestapp'
        $script:outputPath = Join-Path $PSScriptRoot "ios-e2e-$configuration-$runtime-app/test_output"
        $script:appRoot = Split-Path $script:outputPath -Parent

        Remove-Item -Path $script:appRoot -Recurse -Force -ErrorAction SilentlyContinue
        Copy-Item -Path "$PSScriptRoot/net9-maui" -Destination $script:appRoot -Recurse -Force
        Remove-Item -Path (Join-Path $script:appRoot 'bin'), (Join-Path $script:appRoot 'obj') `
            -Recurse -Force -ErrorAction SilentlyContinue
        New-Item -Path $script:outputPath -ItemType Directory -Force | Out-Null
        Push-Location $script:appRoot
        $script:locationPushed = $true

        $arch = [System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture.ToString().ToLower()
        $rid = if ($platform -eq 'iOSDevice') { 'ios-arm64' } else { "iossimulator-$arch" }
        $useMonoRuntime = if ($runtime -eq 'mono') { 'true' } else { 'false' }
        $buildArguments = @(
            'build',
            'Sentry.Maui.Device.IntegrationTestApp.csproj',
            '--configuration', $configuration,
            '--framework', $Tfm,
            '--runtime', $rid,
            "-p:UseMonoRuntime=$useMonoRuntime"
        )
        if ($platform -eq 'iOSDevice' -and -not [string]::IsNullOrEmpty($CodesignKey)) {
            $buildArguments += "-p:CodesignKey=$CodesignKey"
        }
        if ($platform -eq 'iOSDevice' -and -not [string]::IsNullOrEmpty($CodesignProvision)) {
            $buildArguments += "-p:CodesignProvision=$CodesignProvision"
        }

        Write-Host '::group::Build Sentry.Maui.Device.IntegrationTestApp.csproj'
        & dotnet @buildArguments | ForEach-Object { Write-Host $_ }
        Write-Host '::endgroup::'
        $LASTEXITCODE | Should -Be 0

        $script:appPath = Join-Path $script:appRoot "bin/$configuration/$Tfm/$rid/Sentry.Maui.Device.IntegrationTestApp.app"
        $script:appPath | Should -Exist

        Connect-SentryApi -ApiToken $env:SENTRY_E2E_AUTH_TOKEN -DSN $env:SENTRY_E2E_DSN
        $script:apiConnected = $true
        Connect-Device -Platform $platform -Target $Target | Out-Null
        $script:deviceConnected = $true

        function Invoke-IosE2eApp {
            param(
                [Parameter(Mandatory)]
                [string] $RunId,
                [string] $TestArg = 'None'
            )

            $prefix = if ($platform -eq 'iOSDevice') { 'DEVICECTL_CHILD_' } else { 'SIMCTL_CHILD_' }
            $dsnName = "${prefix}SENTRY_DSN"
            $testArgName = "${prefix}SENTRY_TEST_ARG"
            $runIdName = "${prefix}SENTRY_TEST_RUN_ID"

            try {
                Set-Item -Path "env:$dsnName" -Value $env:SENTRY_E2E_DSN
                Set-Item -Path "env:$testArgName" -Value $TestArg
                Set-Item -Path "env:$runIdName" -Value $RunId

                Write-Host "::group::Run iOS E2E app (TestArg=$TestArg, RunId=$RunId)"
                $result = Invoke-DeviceApp -ExecutablePath $script:bundleId
                $result.Output | ForEach-Object { Write-Host $_ }
                Write-Host '::endgroup::'
                return $result
            } finally {
                Remove-Item -Path "env:$dsnName" -ErrorAction SilentlyContinue
                Remove-Item -Path "env:$testArgName" -ErrorAction SilentlyContinue
                Remove-Item -Path "env:$runIdName" -ErrorAction SilentlyContinue
            }
        }

        function Wait-SentryE2eEvent {
            param(
                [Parameter(Mandatory)]
                [string] $RunId,
                [int] $TimeoutSeconds = 300
            )

            $startedAt = Get-Date
            $deadline = $startedAt.AddSeconds($TimeoutSeconds)
            $nextStatusAt = $startedAt.AddSeconds(10)
            $attempt = 0
            $lastError = $null

            Write-Host "Waiting up to $TimeoutSeconds seconds for Sentry event with test_run_id=$RunId"

            do {
                $attempt++
                $now = Get-Date
                if ($now -ge $nextStatusAt) {
                    $elapsedSeconds = [int](($now - $startedAt).TotalSeconds)
                    Write-Host "Still waiting for Sentry event ($elapsedSeconds seconds, $attempt polls)..."
                    $nextStatusAt = $now.AddSeconds(10)
                }

                $events = @()
                try {
                    $events = Find-SentryEventByTag -TagName 'test_run_id' -TagValue $RunId -Limit 10
                    $lastError = $null
                } catch {
                    $lastError = $_.Exception.Message
                }

                if ($events.Count -gt 1) {
                    throw "Expected one event for test_run_id=$RunId but found $($events.Count)."
                }

                if ($events.Count -eq 1) {
                    $sentryEvent = $events[0]
                    $elapsedSeconds = [int](((Get-Date) - $startedAt).TotalSeconds)
                    Write-Host "Found Sentry event $($sentryEvent.id) after $elapsedSeconds seconds ($attempt polls)."
                    $sentryEvent | ConvertTo-Json -Depth 20 |
                        Out-File -FilePath (Join-Path $script:outputPath "$RunId-event.json") -Encoding utf8
                    return $sentryEvent
                }

                Start-Sleep -Seconds 2
            } while ((Get-Date) -lt $deadline)

            throw "Expected one event for test_run_id=$RunId within $TimeoutSeconds seconds. Last error: $lastError"
        }

        function Get-ExceptionType {
            param(
                [Parameter(Mandatory)]
                $SentryEvent
            )

            return @($SentryEvent.entries |
                    Where-Object { $_.type -eq 'exception' } |
                    ForEach-Object { $_.data.values } |
                    ForEach-Object { $_.type })
        }

        function Invoke-CrashTest {
            param(
                [Parameter(Mandatory)]
                [string] $TestArg
            )

            $runId = [Guid]::NewGuid().ToString('N')
            Invoke-IosE2eApp -RunId $runId -TestArg $TestArg | Out-Null
            Invoke-IosE2eApp -RunId $runId | Out-Null
            $sentryEvent = Wait-SentryE2eEvent -RunId $runId
            $script:observedEvents += @{
                RunId   = $runId
                EventId = $sentryEvent.id
                TestArg = $TestArg
            }
            return $sentryEvent
        }
    }

    AfterAll {
        try {
            if ($script:observedEvents.Count -gt 0) {
                Write-Host "Observing $($script:observedEvents.Count) Sentry events for late duplicates for $DuplicateObservationSeconds seconds..."
                $observationDeadline = (Get-Date).AddSeconds($DuplicateObservationSeconds)
                while ((Get-Date) -lt $observationDeadline) {
                    $remainingSeconds = [int][Math]::Ceiling(($observationDeadline - (Get-Date)).TotalSeconds)
                    Start-Sleep -Seconds ([Math]::Min(10, $remainingSeconds))
                    $remainingSeconds = [int][Math]::Ceiling(($observationDeadline - (Get-Date)).TotalSeconds)
                    if ($remainingSeconds -gt 0) {
                        Write-Host "Still observing for late duplicates ($remainingSeconds seconds remaining)..."
                    }
                }

                Write-Host 'Verifying that each crash produced exactly one Sentry event...'
                foreach ($observed in $script:observedEvents) {
                    $events = @()
                    $lastError = $null
                    for ($attempt = 1; $attempt -le 5; $attempt++) {
                        try {
                            $events = Find-SentryEventByTag -TagName 'test_run_id' -TagValue $observed.RunId -Limit 10
                            $lastError = $null
                        } catch {
                            $events = @()
                            $lastError = $_.Exception.Message
                        }

                        if ($events.Count -gt 0) {
                            break
                        }
                        if ($attempt -lt 5) {
                            Start-Sleep -Seconds 2
                        }
                    }

                    if ($events.Count -ne 1) {
                        throw "Expected exactly one event for $($observed.TestArg) crash with test_run_id=$($observed.RunId) but found $($events.Count). Last error: $lastError"
                    }
                    if ($events[0].id -ne $observed.EventId) {
                        throw "Event changed for $($observed.TestArg) crash with test_run_id=$($observed.RunId): expected $($observed.EventId), found $($events[0].id)."
                    }

                    Write-Host "Confirmed one event for $($observed.TestArg) crash: $($observed.EventId)"
                }
            }
        } finally {
            try {
                if ($script:deviceConnected) {
                    Disconnect-Device
                }
            } finally {
                try {
                    if ($script:apiConnected) {
                        Disconnect-SentryApi
                    }
                } finally {
                    if ($script:locationPushed) {
                        Pop-Location
                    }
                }
            }
        }
    }

    BeforeEach {
        Install-DeviceApp -Path $script:appPath | Out-Host
    }

    It 'captures managed crash' {
        $sentryEvent = Invoke-CrashTest -TestArg 'Managed'
        $types = Get-ExceptionType -SentryEvent $sentryEvent

        $types | Should -Contain 'System.ApplicationException'
        $types | Should -Not -AnyElementMatch '^(EXC_[A-Z_]+|SIG[A-Z]+)$'
    }

    It 'captures native crash' {
        $sentryEvent = Invoke-CrashTest -TestArg 'Native'
        $types = Get-ExceptionType -SentryEvent $sentryEvent

        $types | Should -AnyElementMatch '^(EXC_[A-Z_]+|SIG[A-Z]+)$'
        $types | Should -Not -AnyElementMatch '^System\.\w+Exception$'
    }

    It 'captures null reference exception' {
        $sentryEvent = Invoke-CrashTest -TestArg 'NullReferenceException'
        $types = Get-ExceptionType -SentryEvent $sentryEvent

        $types | Should -Contain 'System.NullReferenceException'
        $types | Should -Not -AnyElementMatch '^(EXC_[A-Z_]+|SIG[A-Z]+)$'
    }
}
