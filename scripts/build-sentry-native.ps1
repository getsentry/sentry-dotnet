param([switch] $Clean)
Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Build-SentryNative
{
    param([switch] $Shared)

    $submodule = 'modules/sentry-native'
    $outDir = 'src/Sentry/Platforms/Native/sentry-native'
    $buildDir = if ($Shared) { "$submodule/build-shared" } else { "$submodule/build" }
    $actualBuildDir = $buildDir

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
        $additionalArgs += @('-C', 'src/Sentry/Platforms/Native/windows-config.cmake')
        $actualBuildDir = "$buildDir/RelWithDebInfo"
        $libPrefix = ''
        $libExtension = if ($Shared) { '.dll' } else { '.lib' }

        if ("Arm64".Equals([System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture.ToString()))
        {
            $outDir += if ($Shared) { '/../runtimes/win-arm64/native' } else { '/win-arm64' }
        }
        else
        {
            $outDir += if ($Shared) { '/../runtimes/win-x64/native' } else { '/win-x64' }
        }
    }
    elseif ($IsLinux)
    {
        if ("Arm64".Equals([System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture.ToString()))
        {
            $outDir += '/linux-arm64'
        }
        elseif ((ldd --version 2>&1) -match 'musl')
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

    git submodule update --init --recursive $submodule

    if ($Clean)
    {
        Remove-Item -Recurse -Force -ErrorAction SilentlyContinue $buildDir
    }

    if (-not $Shared)
    {
        $additionalArgs += @('-D', 'SENTRY_BUILD_SHARED_LIBS=0')
    }

    cmake `
        -S $submodule `
        -B $buildDir `
        -D CMAKE_BUILD_TYPE=RelWithDebInfo `
        -D SENTRY_SDK_NAME=sentry.native.dotnet `
        -D SENTRY_BACKEND=inproc `
        -D SENTRY_TRANSPORT=none `
        $additionalArgs

    cmake `
        --build $buildDir `
        --target sentry `
        --config RelWithDebInfo `
        --parallel

    $srcFile = "$actualBuildDir/${libPrefix}sentry$libExtension"
    $outFile = "$outDir/${libPrefix}sentry-native$libExtension"

    # New-Item creates the directory if it doesn't exist.
    New-Item -ItemType File -Path $outFile -Force | Out-Null

    Write-Host "Copying $srcFile to $outFile"
    Copy-Item -Force -Path $srcFile -Destination $outFile

    # Touch the file to mark it as up-to-date for MSBuild
    (Get-Item $outFile).LastWriteTime = Get-Date
}

Push-Location $PSScriptRoot/..
try
{
    Build-SentryNative
    if ($IsWindows)
    {
        Build-SentryNative -Shared
    }
}
finally
{
    Pop-Location
}
