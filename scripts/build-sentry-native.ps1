param([switch] $Clean, [ValidateSet('inproc', 'native')] [string] $Backend = 'inproc')
Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

Push-Location $PSScriptRoot/..
try
{
    $submodule = 'modules/sentry-native'
    $sourceDir = 'src/Sentry/Platforms/Native'
    $outDir = 'src/Sentry/Platforms/Native/sentry-native'
    $buildDir = $Backend -eq 'native' ? "$submodule/build-native" : "$submodule/build"
    $actualBuildDir = "$buildDir/sentry-native"

    $additionalArgs = @()
    $libPrefix = 'lib'
    $libExtension = '.a'
    if ($IsMacOS)
    {
        $outDir += '/osx'
        $additionalArgs += @('-D', 'CMAKE_OSX_ARCHITECTURES=arm64;x86_64')
        $additionalArgs += @('-D', 'CMAKE_OSX_DEPLOYMENT_TARGET=12.0')
    }
    elseif ($IsWindows)
    {
        $actualBuildDir += '/RelWithDebInfo'
        $libPrefix = ''
        $libExtension = '.lib'

        # /Z7 embeds debug info in the static lib (see https://github.com/getsentry/sentry-native/issues/895).
        # /guard:cf + /guard:ehcont add Control Flow Guard metadata so a Native AOT consumer
        # (ControlFlowGuard=Guard) doesn't hit LNK4291. /guard:ehcont is x64-only.
        $msvcFlags = '/Z7 /O2 /Ob1 /DNDEBUG /guard:cf'
        if ("Arm64".Equals([System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture.ToString()))
        {
            $outDir += '/win-arm64'
        }
        else
        {
            $outDir += '/win-x64'
            $msvcFlags += ' /guard:ehcont'
        }
        $additionalArgs += @(
            '-D', "CMAKE_C_FLAGS_RELWITHDEBINFO=$msvcFlags",
            '-D', "CMAKE_CXX_FLAGS_RELWITHDEBINFO=$msvcFlags",
            # VS 2022 toolset, so the shipped lib stays linkable by VS 2022. See https://github.com/getsentry/sentry-dotnet/pull/5566#issuecomment-5671353352
            '-T', 'v143'
        )
    }
    elseif ($IsLinux)
    {
        $musl = (ldd --version 2>&1) -match 'musl'
        $arm64 = "Arm64".Equals([System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture.ToString())
        if ($musl -and $arm64)
        {
            $outDir += '/linux-musl-arm64'
        }
        elseif ($arm64)
        {
            $outDir += '/linux-arm64'
        }
        elseif ($musl)
        {
            $outDir += '/linux-musl-x64'
        }
        else
        {
            $outDir += '/linux-x64'
        }
    }
    else
    {
        throw "Unsupported platform"
    }

    $rids = $IsMacOS ? @('osx-arm64', 'osx-x64') : @(Split-Path -Leaf $outDir)
    if ($Backend -eq 'native')
    {
        $outDir += '/native'
    }

    git submodule update --init --recursive $submodule

    if ($Clean)
    {
        Remove-Item -Recurse -Force -ErrorAction SilentlyContinue $buildDir
    }

    cmake `
        -S $sourceDir `
        -B $buildDir `
        -D CMAKE_BUILD_TYPE=RelWithDebInfo `
        -D SENTRY_SDK_NAME=sentry.native.dotnet `
        -D SENTRY_BUILD_SHARED_LIBS=0 `
        -D SENTRY_BACKEND=$Backend `
        -D SENTRY_TRANSPORT=custom `
        $additionalArgs

    cmake `
        --build $buildDir `
        --config RelWithDebInfo `
        --parallel

    cmake `
        --install $buildDir `
        --config RelWithDebInfo `
        --component sentry-preload `
        --prefix $outDir

    function Deploy-Library($srcFile, $outFile)
    {
        # New-Item creates the directory if it doesn't exist.
        New-Item -ItemType File -Path $outFile -Force | Out-Null

        Write-Host "Copying $srcFile to $outFile"
        Copy-Item -Force -Path $srcFile -Destination $outFile

        # Touch the file to mark it as up-to-date for MSBuild
        (Get-Item $outFile).LastWriteTime = Get-Date
    }

    Deploy-Library "$actualBuildDir/${libPrefix}sentry$libExtension" "$outDir/${libPrefix}sentry-native$libExtension"
    if ($IsLinux)
    {
        Deploy-Library "$actualBuildDir/vendor/libunwind/${libPrefix}unwind$libExtension" "$outDir/${libPrefix}unwind$libExtension"
    }
    if ($Backend -eq 'native')
    {
        $exeExtension = $IsWindows ? '.exe' : ''
        Deploy-Library "$actualBuildDir/sentry-crash$exeExtension" "$outDir/sentry-crash$exeExtension"
        if ($IsWindows)
        {
            Deploy-Library "$actualBuildDir/sentry-wer.dll" "$outDir/sentry-wer.dll"
        }

        $transportFile = 'sentry-crash-transport' + ($IsWindows ? '.dll' : $IsMacOS ? '.dylib' : '.so')
        $transports = @(foreach ($rid in $rids)
        {
            # The transport uses no workloads, so it doesn't need the workload set that global.json pins
            dotnet publish src/Sentry.Native.Transport -c Release -r $rid -o "$buildDir/transport/$rid" --nologo -p:MSBuildEnableWorkloadResolver=false | Write-Host
            if ($LASTEXITCODE -ne 0)
            {
                throw "Failed to publish the transport for $rid"
            }
            "$buildDir/transport/$rid/$transportFile"
        })
        if ($IsMacOS)
        {
            lipo -create @transports -output "$buildDir/transport/$transportFile"
            if ($LASTEXITCODE -ne 0)
            {
                throw "Failed to combine the transports"
            }
            $transports = @("$buildDir/transport/$transportFile")
        }
        Deploy-Library $transports[0] "$outDir/$transportFile"
    }
}
finally
{
    Pop-Location
}
