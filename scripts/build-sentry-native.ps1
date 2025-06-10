param([switch] $Clean)
Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Get-Runtime-Identifier
{
    if ($IsMacOS)
    {
        return 'osx'
    }
    elseif ($IsLinux -and (ldd --version 2>&1) -match 'musl')
    {
        return 'linux-musl-x64'
    }
    else
    {
        return [System.Runtime.InteropServices.RuntimeInformation]::RuntimeIdentifier
    }
}

function Build-Sentry-Native
{
    param([switch] $Static)

    $submodule = 'modules/sentry-native'
    $package = 'src/Sentry/Platforms/Native'

    if ($Static)
    {
        $buildDir = "$submodule/build/static"
        $outDir = "$package/static/$(Get-Runtime-Identifier)/native"
    }
    else
    {
        $buildDir = "$submodule/build/shared"
        $outDir = "$package/runtimes/$(Get-Runtime-Identifier)/native"
    }
    $actualBuildDir = $buildDir

    $additionalArgs = @()
    if ($IsMacOS)
    {
        $additionalArgs += @('-D', 'CMAKE_OSX_ARCHITECTURES=arm64;x86_64')
        $additionalArgs += @('-D', 'CMAKE_OSX_DEPLOYMENT_TARGET=12.0')
        $libPrefix = 'lib'
        $libExtension = if ($Static) { '.a' } else { '.dylib' }
    }
    elseif ($IsWindows)
    {
        $additionalArgs += @('-C', 'src/Sentry/Platforms/Native/windows-config.cmake')
        $actualBuildDir = "$buildDir/RelWithDebInfo"
        $libPrefix = ''
        $libExtension = '.lib'
    }
    elseif ($IsLinux)
    {
        $libPrefix = 'lib'
        $libExtension = if ($Static) { '.a' } else { '.so' }
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

    if ($Static)
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
    Build-Sentry-Native
    Build-Sentry-Native -Static
}
finally
{
    Pop-Location
}
