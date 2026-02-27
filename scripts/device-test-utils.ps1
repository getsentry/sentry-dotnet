function Install-XHarness
{
    if (!(Get-Command xharness -ErrorAction SilentlyContinue))
    {
        $CI = Test-Path env:CI
        Push-Location ($CI ? $env:RUNNER_TEMP : $IsWindows ? $env:TMP : $IsMacos ? $env:TMPDIR : '/tmp')
        dotnet tool install Microsoft.DotNet.XHarness.CLI --global --version '10.0.0-prerelease.25466.1' `
            --add-source https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-eng/nuget/v3/index.json
        Pop-Location
    }
}

function Get-AndroidEmulatorId
{
    if ((Test-Path env:CI) -or (Test-Path env:ANDROID_SERIAL))
    {
        return $env:ANDROID_SERIAL
    }
    try
    {
        return & xharness android adb -- devices | Select-String "device$" | ForEach-Object { ($_ -split "`t")[0] } | Select-Object -First 1
    }
    catch
    {
        return $null
    }
}

# mlaunch (inside xharness) checks if simulator watchdogs are disabled before
# launching an app. If not, it shuts down the sim, writes plist files, then
# reboots via Simulator.app — but simctl still sees it as Shutdown, so the
# launch fails. The CI log sequence:
#
#   dbug: Xamarin.Hosting: Simulator watchdogs are not disabled for 'iPhone 17 (...)'
#   dbug: Xamarin.Hosting: Shutting down simulator...
#   dbug: Xamarin.Hosting: Successfully disabled simulator watchdogs for 'iPhone 17 (...)'
#   dbug: Xamarin.Hosting: Launching simulator application 'com.apple.iphonesimulator'
#   dbug: Xamarin.Hosting: Ready notification 'com.apple.iphonesimulator.ready' received from the simulator.
#   dbug: Unable to lookup in current state: Shutdown
#   dbug: error HE0042: Could not launch the app '...' on the device '...': simctl returned exit code 149
#   dbug: Process mlaunch exited with 1
#
# By pre-writing the watchdog plists before booting, mlaunch sees watchdogs
# already disabled and skips its shutdown/reboot dance entirely.
function Disable-IosSimulatorWatchdogs {
    param(
        [Parameter(Mandatory)]
        [string]$Udid
    )
    $prefsPath = "$HOME/Library/Developer/CoreSimulator/Devices/$Udid/data/Library/Preferences"
    New-Item -ItemType Directory -Force -Path $prefsPath | Out-Null
    $keys = @(
        'FBLaunchWatchdogScale',
        'FBLaunchWatchdogFirstPartyScale',
        'FBLaunchWatchdogResumeScale',
        'FBLaunchWatchdogScaleOverride',
        'FBLaunchWatchdogFirstPartyScaleOverride',
        'FBLaunchWatchdogResumeScaleOverride'
    )
    foreach ($plist in @('com.apple.springboard.plist', 'com.apple.frontboard.plist')) {
        $plistPath = "$prefsPath/$plist"
        if (-not (Test-Path $plistPath)) {
            /usr/bin/plutil -create xml1 $plistPath
        }
        foreach ($key in $keys) {
            /usr/libexec/PlistBuddy -c "Delete :$key" $plistPath 2>&1 | Out-Null
            /usr/libexec/PlistBuddy -c "Add :$key real 100" $plistPath
        }
    }
}

function Get-IosSimulatorUdid {
    [CmdletBinding()]
    param(
        [string]$IosVersion = '26.0',
        [string[]]$PreferredDeviceTypes = @(
            'com.apple.CoreSimulator.SimDeviceType.iPhone-XS',
            'com.apple.CoreSimulator.SimDeviceType.iPhone-17',
            'com.apple.CoreSimulator.SimDeviceType.iPhone-16'
        ),
        [string[]]$PreferredStates = @('Shutdown','Booted')
    )

    try {
        $simDevices = & xcrun simctl list devices --json | ConvertFrom-Json
    } catch {
        Write-Verbose "Failed to query simctl: $($_.Exception.Message)"
        return $null
    }
    if (-not $simDevices -or -not $simDevices.devices) {
        Write-Verbose "No devices structure returned."
        return $null
    }

    $devicesByRuntime = $simDevices.devices
    $preferredIndex = @{}
    for ($i = 0; $i -lt $PreferredDeviceTypes.Count; $i++) {
        $preferredIndex[$PreferredDeviceTypes[$i]] = $i
    }

    $dashVer  = $IosVersion -replace '\.','-'
    $exactKey = "com.apple.CoreSimulator.SimRuntime.iOS-$dashVer"
    $runtimeKey = $null

    $allRuntimeNames = $devicesByRuntime.PSObject.Properties.Name
    if ($allRuntimeNames -contains $exactKey) {
        $runtimeKey = $exactKey
        Write-Verbose "Found exact runtime: $runtimeKey"
    } else {
        $major = ($IosVersion.Split('.')[0])
        $candidates = $allRuntimeNames | Where-Object { $_ -match "com\.apple\.CoreSimulator\.SimRuntime\.iOS-$major-" }
        if ($candidates) {
            $runtimeKey = $candidates |
                    Sort-Object {
                        $v = ($_ -replace '.*iOS-','') -replace '-','.'
                        try { [Version]$v } catch { [Version]'0.0' }
                    } -Descending |
                    Select-Object -First 1
            Write-Verbose "Exact runtime $exactKey not found. Using fallback runtime $runtimeKey"
        } else {
            Write-Verbose "No simulator runtime found for iOS major $major"
            return $null
        }
    }

    $runtimeDevices = $devicesByRuntime.PSObject.Properties |
            Where-Object { $_.Name -eq $runtimeKey } |
            Select-Object -ExpandProperty Value

    if (-not $runtimeDevices) {
        Write-Verbose "Runtime key $runtimeKey present but no devices listed."
        return $null
    }

    $usable = $runtimeDevices | Where-Object { $_.isAvailable -and $_.state -in $PreferredStates }
    if (-not $usable) {
        Write-Verbose "No available devices in runtime $runtimeKey"
        return $null
    }

    $ranked = $usable | ForEach-Object {
        $dt = $_.deviceTypeIdentifier
        $weightPref = if ($preferredIndex.ContainsKey($dt)) { $preferredIndex[$dt] } else { 9999 }
        $weightFamily = if ($dt -match 'iPhone') { 0 } else { 1 }  # prefer iPhone if not explicitly listed
        [PSCustomObject]@{
            Device       = $_
            WeightPref   = $weightPref
            WeightFamily = $weightFamily
            WeightBoot   = if ($_.state -eq 'Booted') { 0 } else { 1 }
            SortName     = $_.name
        }
    }

    $sorted = $ranked | Sort-Object WeightPref, WeightFamily, WeightBoot, SortName
    $sorted | Select-Object -First 5 | ForEach-Object {
        Write-Verbose ("Candidate: {0} | {1} | pref={2} fam={3} bootW={4}" -f $_.Device.name, $_.Device.deviceTypeIdentifier, $_.WeightPref, $_.WeightFamily, $_.WeightBoot)
    }

    $selected = $sorted | Select-Object -First 1
    if (-not $selected) {
        Write-Verbose "Failed to select a simulator."
        return $null
    }

    Write-Verbose ("Selected simulator: {0} ({1}) [{2}]" -f $selected.Device.name, $selected.Device.deviceTypeIdentifier, $selected.Device.udid)
    return $selected.Device.udid
}
