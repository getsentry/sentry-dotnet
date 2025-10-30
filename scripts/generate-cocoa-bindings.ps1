# Reference: https://github.com/xamarin/xamarin-macios/blob/main/docs/website/binding_types_reference_guide.md

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$RootPath = (Get-Item $PSScriptRoot).Parent.FullName
$CocoaSdkPath = "$RootPath/modules/sentry-cocoa"
if (Test-Path "$CocoaSdkPath/.git")
{
    # Cocoa SDK cloned to modules/sentry-cocoa for local development
    $HeadersPath = "$CocoaSdkPath/Carthage/Headers"
    $PrivateHeadersPath = "$CocoaSdkPath/Carthage/Headers"
}
else
{
    # Cocoa SDK downloaded from GitHub releases and extracted into modules/sentry-cocoa
    $HeadersPath = "$CocoaSdkPath/Sentry.framework/Headers"
    $PrivateHeadersPath = "$CocoaSdkPath/Sentry.framework/PrivateHeaders"
}
$BindingsPath = "$RootPath/src/Sentry.Bindings.Cocoa"
$BackupPath = "$BindingsPath/obj/_unpatched"

# Ensure running on macOS
if (!$IsMacOS)
{
    Write-Error 'Bindings generation can only be performed on macOS.' `
        -CategoryActivity Error -ErrorAction Stop
}

# Ensure Objective Sharpie is installed
if (!(Get-Command sharpie -ErrorAction SilentlyContinue))
{
    Write-Output 'Objective Sharpie not found. Attempting to install via Homebrew.'
    brew install --cask objectivesharpie

    if (!(Get-Command sharpie -ErrorAction SilentlyContinue))
    {
        Write-Error 'Could not install Objective Sharpie automatically. Try installing from https://aka.ms/objective-sharpie manually.'
    }
}

# Ensure Xamarin is installed (or sharpie won't produce expected output).
if (!(Test-Path '/Library/Frameworks/Xamarin.iOS.framework/Versions/Current/lib/64bits/iOS/Xamarin.iOS.dll'))
{
    Write-Output 'Xamarin.iOS not found. Attempting to install manually.'

    # Download Xamarin.iOS package
    $packageName = 'xamarin.ios-16.4.0.23.pkg'
    $directDownloadUrl = 'https://github.com/getsentry/sentry-dotnet/releases/download/1.0.0.0-xamarin-ios/Xamarin.iOS.16.4.0.23.pkg'
    $downloadPath = "/tmp/$packageName"
    $expectedSha256 = '3c3a2e3c5adebf7955934862b89c82e4771b0fd44dfcfebad0d160033a6e0a1a'

    Write-Output "Downloading Xamarin.iOS package..."
    curl -L -o $downloadPath $directDownloadUrl

    if ($LASTEXITCODE -ne 0)
    {
        Write-Error "Failed to download Xamarin.iOS package. Exit code: $LASTEXITCODE"
    }

    # Verify checksum
    Write-Output "Verifying package checksum..."
    $actualSha256 = (Get-FileHash -Path $downloadPath -Algorithm SHA256).Hash.ToLower()

    if ($actualSha256 -ne $expectedSha256)
    {
        Write-Error "Checksum verification failed. Expected: $expectedSha256, Actual: $actualSha256"
        Remove-Item $downloadPath -Force -ErrorAction SilentlyContinue
        exit 1
    }

    Write-Output "Checksum verification passed."

    if (Test-Path $downloadPath)
    {
        Write-Output "Downloaded package to $downloadPath"
        Write-Output "Installing Xamarin.iOS package..."

        # Install the package using installer command (requires sudo)
        sudo installer -pkg $downloadPath -target /

        if ($LASTEXITCODE -ne 0)
        {
            Write-Error "Failed to install Xamarin.iOS package. Exit code: $LASTEXITCODE"
        }
        else
        {
            Write-Output "Xamarin.iOS package installed successfully"
        }

        # Clean up downloaded file
        Remove-Item $downloadPath -Force -ErrorAction SilentlyContinue
    }
    else
    {
        Write-Error "Downloaded package not found at $downloadPath"
    }

    if (!(Test-Path '/Library/Frameworks/Xamarin.iOS.framework/Versions/Current/lib/64bits/iOS/Xamarin.iOS.dll'))
    {
        Write-Error 'Xamarin.iOS not found after installation.'
    }
}

# Get iPhone SDK version
$XcodePath = (xcode-select -p) -replace '/Contents/Developer$', ''
$iPhoneSdkVersion = sharpie xcode -xcode $XcodePath -sdks | grep -o -m 1 'iphoneos\S*'
Write-Output "iPhoneSdkVersion: $iPhoneSdkVersion"
$iPhoneSdkPath = xcrun --show-sdk-path --sdk $iPhoneSdkVersion
Write-Output "iPhoneSdkPath: $iPhoneSdkPath"

## Imports in the various header files are provided in the "new" style of:
#     `#import <Sentry/SomeHeader.h>`
# ...or:
#     `#import SENTRY_HEADER(SentryHeader)`
# ...instead of:
#     `#import "SomeHeader.h"`
# This causes sharpie to fail resolve those headers
$filesToPatch = Get-ChildItem -Path "$HeadersPath" -Filter *.h -Recurse | Select-Object -ExpandProperty FullName
foreach ($file in $filesToPatch)
{
    if (Test-Path $file)
    {
        $content = Get-Content -Path $file -Raw
        $content = $content -replace '<Sentry/([^>]+)>', '"$1"'
        $content = $content -replace '#\s*import SENTRY_HEADER\(([^)]+)\)', '#import "$1.h"'
        Set-Content -Path $file -Value $content
    }
    else
    {
        Write-Host "File not found: $file"
    }
}
$privateHeaderFile = "$PrivateHeadersPath/PrivatesHeader.h"
if (Test-Path $privateHeaderFile)
{
    $content = Get-Content -Path $privateHeaderFile -Raw
    $content = $content -replace '"SentryDefines.h"', '"../Headers/SentryDefines.h"'
    $content = $content -replace '"SentryProfilingConditionals.h"', '"../Headers/SentryProfilingConditionals.h"'
    Set-Content -Path $privateHeaderFile -Value $content
    Write-Host "Patched includes: $privateHeaderFile"
}
else
{
    Write-Host "File not found: $privateHeaderFile"
}
$swiftHeaderFile = "$HeadersPath/Sentry-Swift.h"
if (Test-Path $swiftHeaderFile)
{
    $content = Get-Content -Path $swiftHeaderFile -Raw
    # Replace module @imports with traditional #includes
    $content = $content -replace '(?m)^#if\s+(__has_feature\(objc_modules\))', '#if 1 // $1'
    $content = $content -replace '(?m)^@import\s+ObjectiveC;\s*\n', ''
    $content = $content -replace '(?m)^@import\s+(\w+);', '#include <$1/$1.h>'
    $content = $content -replace '(?m)^#import\s+"Sentry.h"\s*\n', ''

    Set-Content -Path $swiftHeaderFile -Value $content
    Write-Host "Patched includes: $swiftHeaderFile"
}
else
{
    Write-Host "File not found: $swiftHeaderFile"
}

# Generate bindings
Write-Output 'Generating bindings with Objective Sharpie.'
sharpie bind -sdk $iPhoneSdkVersion `
    -scope "$CocoaSdkPath" `
    "$HeadersPath/Sentry.h" `
    "$HeadersPath/Sentry-Swift.h" `
    "$PrivateHeadersPath/PrivateSentrySDKOnly.h" `
    -o $BindingsPath `
    -c -Wno-objc-property-no-attribute `
    -F"$iPhoneSdkPath/System/Library/SubFrameworks" # needed for UIUtilities.framework in Xcode 26+

# Ensure backup path exists
if (!(Test-Path $BackupPath))
{
    New-Item -ItemType Directory -Path $BackupPath | Out-Null
}

################################################################################
# Patch StructsAndEnums.cs
################################################################################
$File = 'StructsAndEnums.cs'
Write-Output "Patching $BindingsPath/$File"
Copy-Item "$BindingsPath/$File" -Destination "$BackupPath/$File"
& dotnet run --project "$RootPath/tools/Sentry.Bindings.Cocoa.PostProcessor/Sentry.Bindings.Cocoa.PostProcessor.csproj" -- "$BindingsPath/$File" | ForEach-Object { Write-Host $_ }

################################################################################
# Patch ApiDefinitions.cs
################################################################################
$File = 'ApiDefinitions.cs'
Write-Output "Patching $BindingsPath/$File"
Copy-Item "$BindingsPath/$File" -Destination "$BackupPath/$File"
& dotnet run --project "$RootPath/tools/Sentry.Bindings.Cocoa.PostProcessor/Sentry.Bindings.Cocoa.PostProcessor.csproj" -- "$BindingsPath/$File" | ForEach-Object { Write-Host $_ }
