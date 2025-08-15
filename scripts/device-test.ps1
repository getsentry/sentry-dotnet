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
        $Tfm = 'net9.0'
    }
    $arch = (!$IsWindows -and $(uname -m) -eq 'arm64') ? 'arm64' : 'x64'
    if ($Platform -eq 'android')
    {
        $Tfm += '-android'
        $group = 'android'
        $buildDir = $CI ? 'bin' : "test/Sentry.Maui.Device.TestApp/bin/Release/$Tfm/android-$arch"
        $arguments = @(
            '--app', "$buildDir/io.sentry.dotnet.maui.device.testapp-Signed.apk",
            '--package-name', 'io.sentry.dotnet.maui.device.testapp',
            '--launch-timeout', '00:10:00',
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
        # Always use x64 on iOS, since arm64 doesn't support JIT, which is required for tests using NSubstitute
        $arch = 'x64'
        $buildDir = "test/Sentry.Maui.Device.TestApp/bin/Release/$Tfm/iossimulator-$arch"
        $envValue = $CI ? 'true' : 'false'
        $arguments = @(
            '--app', "$buildDir/Sentry.Maui.Device.TestApp.app",
            '--target', 'ios-simulator-64',
            '--launch-timeout', '00:10:00',
            '--set-env', "CI=$envValue"
        )

        # Version you want to pin (make this a param later if needed)
        $IosVersion = '18.5'

        $udid = $null
        $simDevices = & xcrun simctl list devices --json | ConvertFrom-Json
        $devicesByRuntime = $simDevices.devices

        # Preferred device types (ordered)
        $preferredTypes = @(
            'com.apple.CoreSimulator.SimDeviceType.iPhone-XS',
            'com.apple.CoreSimulator.SimDeviceType.iPhone-16',
            'com.apple.CoreSimulator.SimDeviceType.iPhone-15'
        )
        $preferredIndex = @{}
        for ($i = 0; $i -lt $preferredTypes.Count; $i++) { $preferredIndex[$preferredTypes[$i]] = $i }

        # Build exact runtime key (e.g. com.apple.CoreSimulator.SimRuntime.iOS-18-0)
        $dashVer = $IosVersion -replace '\.','-'
        $exactKey = "com.apple.CoreSimulator.SimRuntime.iOS-$dashVer"

        $runtimeKey = $null
        if ($devicesByRuntime.PSObject.Properties.Name -contains $exactKey) {
            $runtimeKey = $exactKey
        } else {
            # Fallback: pick highest patch for requested major
            $major = ($IosVersion.Split('.')[0])
            $candidates = $devicesByRuntime.PSObject.Properties.Name |
                    Where-Object { $_ -match "com\.apple\.CoreSimulator\.SimRuntime\.iOS-$major-" }
            if ($candidates) {
                $runtimeKey = $candidates |
                        Sort-Object {
                            # Extract trailing iOS-x-y -> x.y
                            $v = ($_ -replace '.*iOS-','') -replace '-','.'
                            try { [Version]$v } catch { [Version]'0.0' }
                        } -Descending |
                        Select-Object -First 1
                Write-Host "Exact runtime $exactKey not found. Using fallback runtime $runtimeKey"
            } else {
                throw "No simulator runtime found for iOS major $major"
            }
        }

        $runtimeDevices = $devicesByRuntime.PSObject.Properties |
                Where-Object { $_.Name -eq $runtimeKey } |
                Select-Object -ExpandProperty Value

        if (-not $runtimeDevices) {
            throw "Runtime key $runtimeKey present but no devices listed."
        }

        # Filter usable devices
        $usable = $runtimeDevices | Where-Object { $_.isAvailable -and $_.state -in @('Shutdown','Booted') }
        if (-not $usable) { throw "No available devices in runtime $runtimeKey" }

        # Compute weights
        $ranked = $usable | ForEach-Object {
            $dt = $_.deviceTypeIdentifier
            $weightPref = if ($preferredIndex.ContainsKey($dt)) { $preferredIndex[$dt] } else { 9999 }
            $isIphone = ($dt -match 'iPhone') ? 0 : 1  # prefer iPhone over iPad if not explicitly preferred
            [PSCustomObject]@{
                Device        = $_
                WeightPref    = $weightPref
                WeightFamily  = $isIphone
                WeightBoot    = if ($_.state -eq 'Booted') { 0 } else { 1 }
                SortName      = $_.name
            }
        }

        $selected = $ranked |
                Sort-Object WeightPref, WeightFamily, WeightBoot, SortName |
                Select-Object -First 1

        Write-Host "Candidate devices (top 5 by weight):"
        $ranked |
                Sort-Object WeightPref, WeightFamily, WeightBoot, SortName |
                Select-Object -First 5 |
                ForEach-Object {
                    Write-Host ("  {0} | {1} | pref={2} fam={3}" -f $_.Device.name, $_.Device.deviceTypeIdentifier, $_.WeightPref, $_.WeightFamily)
                }

        if (-not $selected) {
            throw "Failed to select a simulator."
        }

        $udid = $selected.Device.udid
        Write-Host "Selected simulator: $($selected.Device.name) ($($selected.Device.deviceTypeIdentifier)) [$udid]"
        $arguments += @('--device', $udid)
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
        if (!(Get-Command xharness -ErrorAction SilentlyContinue))
        {
            Push-Location ($CI ? $env:RUNNER_TEMP : $IsWindows ? $env:TMP : $IsMacos ? $env:TMPDIR : '/temp')
            dotnet tool install Microsoft.DotNet.XHarness.CLI --global --version '10.0.0-prerelease.25412.1' `
                --add-source https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-eng/nuget/v3/index.json
            Pop-Location
        }

        Remove-Item -Recurse -Force test_output -ErrorAction SilentlyContinue
        try
        {
            xharness $group test $arguments --output-directory=test_output
            if ($LASTEXITCODE -ne 0)
            {
                throw 'xharness run failed with non-zero exit code'
            }
        }
        finally
        {
            if ($CI)
            {
                scripts/parse-xunit2-xml.ps1 (Get-Item ./test_output/*.xml).FullName | Out-File $env:GITHUB_STEP_SUMMARY
            }

        }
    }
}
finally
{
    Pop-Location
}
