param([switch] $Clean)
Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

Push-Location $PSScriptRoot/..
try
{
    $submodule = 'modules/sentry-native'
    $outDir = 'src/Sentry/Platforms/Native/sentry-native'
    $buildDir = "$submodule/build"
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
        $outDir += '/win-x64'
        $additionalArgs += @('-C', 'src/Sentry/Platforms/Native/windows-config.cmake')
        $actualBuildDir = "$buildDir/RelWithDebInfo"
        $libPrefix = ''
        $libExtension = '.lib'
    }
    elseif ($IsLinux)
    {
        $outDir += '/linux-x64'
    }
    else
    {
        throw "Unsupported platform"
    }

    git submodule update --init --recursive $submodule

    if ($Clean)
    {
        rm -rf $buildDir
    }

    cmake `
        -S $submodule `
        -B $buildDir `
        -D CMAKE_BUILD_TYPE=RelWithDebInfo `
        -D SENTRY_SDK_NAME=sentry.native.dotnet `
        -D SENTRY_BUILD_SHARED_LIBS=0 `
        -D SENTRY_BACKEND=none `
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
finally
{
    Pop-Location
}
