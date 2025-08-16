# Reference: https://github.com/xamarin/xamarin-macios/blob/main/docs/website/binding_types_reference_guide.md

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$RootPath = (Get-Item $PSScriptRoot).Parent.FullName
$CocoaSdkPath = "$RootPath/modules/sentry-cocoa/Sentry.framework"
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
$iPhoneSdkVersion = sharpie xcode -sdks | grep -o -m 1 'iphoneos\S*'
Write-Output "iPhoneSdkVersion: $iPhoneSdkVersion"

## Imports in the various header files are provided in the "new" style of:
#     `#import <Sentry/SomeHeader.h>`
# ...instead of:
#     `#import "SomeHeader.h"`
# This causes sharpie to fail resolve those headers
$filesToPatch = Get-ChildItem -Path "$CocoaSdkPath/Headers" -Filter *.h -Recurse | Select-Object -ExpandProperty FullName
foreach ($file in $filesToPatch)
{
    if (Test-Path $file)
    {
        $content = Get-Content -Path $file -Raw
        $content = $content -replace '<Sentry/([^>]+)>', '"$1"'
        Set-Content -Path $file -Value $content
    }
    else
    {
        Write-Host "File not found: $file"
    }
}
$privateHeaderFile = "$CocoaSdkPath/PrivateHeaders/PrivatesHeader.h"
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

# Generate bindings
Write-Output 'Generating bindings with Objective Sharpie.'
sharpie bind -sdk $iPhoneSdkVersion `
    -scope "$CocoaSdkPath" `
    "$CocoaSdkPath/Headers/Sentry.h" `
    "$CocoaSdkPath/PrivateHeaders/PrivateSentrySDKOnly.h" `
    -o $BindingsPath `
    -c -Wno-objc-property-no-attribute

# Ensure backup path exists
if (!(Test-Path $BackupPath))
{
    New-Item -ItemType Directory -Path $BackupPath | Out-Null
}

# The following header will be added to patched files.  The notice applies
# to those files, not this script which generates the files.
$Header = @"
// -----------------------------------------------------------------------------
// This file is auto-generated by Objective Sharpie and patched via the script
// at /scripts/generate-cocoa-bindings.ps1.  Do not edit this file directly.
// If changes are required, update the script instead.
// -----------------------------------------------------------------------------
"@

################################################################################
# Patch StructsAndEnums.cs
################################################################################
$File = 'StructsAndEnums.cs'
Write-Output "Patching $BindingsPath/$File"
Copy-Item "$BindingsPath/$File" -Destination "$BackupPath/$File"
$Text = Get-Content "$BindingsPath/$File" -Raw

# Tabs to spaces
$Text = $Text -replace '\t', '    '

# Trim extra newline at EOF
$Text = $Text -replace '\n$', ''

# Insert namespace
$Text = $Text -replace 'using .+;\n\n', "$&namespace Sentry.CocoaSdk;`n`n"

# Public to internal
$Text = $Text -replace '\bpublic\b', 'internal'

# Remove static CFunctions class
$Text = $Text -replace '(?ms)\nstatic class CFunctions.*?}\n', ''

# Add header and output file
$Text = "$Header`n`n$Text"
$Text | Out-File "$BindingsPath/$File"

################################################################################
# Patch ApiDefinitions.cs
################################################################################
$File = 'ApiDefinitions.cs'
Write-Output "Patching $BindingsPath/$File"
Copy-Item "$BindingsPath/$File" -Destination "$BackupPath/$File"
$Text = Get-Content "$BindingsPath/$File" -Raw

# Tabs to spaces
$Text = $Text -replace '\t', '    '

# Trim extra newline at EOF
$Text = $Text -replace '\n$', ''

# Insert namespace
$Text = $Text -replace 'using .+;\n\n', "$&namespace Sentry.CocoaSdk;`n`n"

# Set Internal attributes on interfaces and delegates
$Text = $Text -replace '(?m)^(partial interface|interface|delegate)\b', "[Internal]`n$&"

# Fix ISentrySerializable usage
$Text = $Text -replace '\bISentrySerializable\b', 'SentrySerializable'

# Remove INSCopying due to https://github.com/xamarin/xamarin-macios/issues/17130
$Text = $Text -replace ': INSCopying,', ':' -replace '\s?[:,] INSCopying', ''

# Remove iOS attributes like [iOS (13, 0)]
$Text = $Text -replace '\[iOS \(13, 0\)\]\n?', ''

# Fix delegate argument names
$Text = $Text -replace '(NSError) arg\d', '$1 error'
$Text = $Text -replace '(NSHttpUrlResponse) arg\d', '$1 response'
$Text = $Text -replace '(SentryEvent) arg\d', '$1 @event'
$Text = $Text -replace '(SentrySamplingContext) arg\d', '$1 samplingContext'
$Text = $Text -replace '(SentryBreadcrumb) arg\d', '$1 breadcrumb'
$Text = $Text -replace '(SentrySpan) arg\d', '$1 span'
$Text = $Text -replace '(SentryAppStartMeasurement) arg\d', '$1 appStartMeasurement'

# Adjust nullable return delegates (though broken until this is fixed: https://github.com/xamarin/xamarin-macios/issues/17109)
$Text = $Text -replace 'delegate \w+ Sentry(BeforeBreadcrumb|BeforeSendEvent|TracesSampler)Callback', "[return: NullAllowed]`n$&"

# Adjust protocols (some are models)
$Text = $Text -replace '(?ms)(@protocol.+?)/\*.+?\*/', '$1'
$Text = $Text -replace '(?ms)@protocol (SentrySerializable|SentrySpan).+?\[Protocol\]', "`$&`n[Model]"

# Adjust SentrySpan base type
$Text = $Text -replace 'interface SentrySpan\b', "[BaseType (typeof(NSObject))]`n`$&"

# Fix string constants
$Text = $Text -replace '(?m)(.*\n){2}^\s{4}NSString k.+?\n\n?', ''
$Text = $Text -replace '(?m)(.*\n){4}^partial interface Constants\n{\n}\n', ''
$Text = $Text -replace '\[Verify \(ConstantsInterfaceAssociation\)\]\n', ''

# Remove SentryVersionNumber
$Text = $Text -replace '.*SentryVersionNumber.*\n?', ''

# Remove SentryVersionString
$Text = $Text -replace '.*SentryVersionString.*\n?', ''

# Remove duplicate attributes
$s = 'partial interface Constants'
$t = $Text -split $s, 2
$t[1] = $t[1] -replace "\[Static\]\n\[Internal\]\n$s", $s
$Text = $t -join $s

# Remove empty Constants block
$Text = $Text -replace '\[Static\]\s*\[Internal\]\s*partial\s+interface\s+Constants\s\{[\s\n]*\}\n\n', ''

# Update MethodToProperty translations
$Text = $Text -replace '(Export \("get\w+"\)\]\n)\s*\[Verify \(MethodToProperty\)\]\n(.+ \{ get; \})', '$1$2'
$Text = $Text -replace '\[Verify \(MethodToProperty\)\]\n\s*(.+ (?:Hash|Value|DefaultIntegrations|AppStartMeasurementWithSpans|BaggageHttpHeader) \{ get; \})', '$1'
$Text = $Text -replace '\[Verify \(MethodToProperty\)\]\n\s*(.+) \{ get; \}', '$1();'

# Allow weakly typed NSArray
# We have some that accept either NSString or NSRegularExpression, which have no common type so they use NSObject
$Text = $Text -replace '\s*\[Verify \(StronglyTypedNSArray\)\]\n', ''

# Fix broken line comment
$Text = $Text -replace '(DEPRECATED_MSG_ATTRIBUTE\()\n\s*', '$1'

# Remove default IsEqual implementation (already implemented by NSObject)
$Text = $Text -replace '(?ms)\n?^ *// [^\n]*isEqual:.*?$.*?;\n', ''

# Replace obsolete platform availability attributes
$Text = $Text -replace '([\[,] )MacCatalyst \(', '$1Introduced (PlatformName.MacCatalyst, '
$Text = $Text -replace '([\[,] )Mac \(', '$1Introduced (PlatformName.MacOSX, '
$Text = $Text -replace '([\[,] )iOS \(', '$1Introduced (PlatformName.iOS, '

# Make interface partial if we need to access private APIs.  Other parts will be defined in PrivateApiDefinitions.cs
$Text = $Text -replace '(?m)^interface SentryScope', 'partial $&'

# Prefix SentryBreadcrumb.Serialize and SentryScope.Serialize with new (since these hide the base method)
$Text = $Text -replace '(?m)(^\s*\/\/[^\r\n]*$\s*\[Export \("serialize"\)\]$\s*)(NSDictionary)', '${1}new $2'

$Text = $Text -replace '.*SentryEnvelope .*?[\s\S]*?\n\n', ''
$Text = $Text -replace '.*typedef.*SentryOnAppStartMeasurementAvailable.*?[\s\S]*?\n\n', ''

$propertiesToRemove = @(
    'SentryAppStartMeasurement',
    'SentryOnAppStartMeasurementAvailable',
    'SentryMetricsAPI',
    'SentryExperimentalOptions',
    'description',
    'enableMetricKitRawPayload'
)

foreach ($property in $propertiesToRemove)
{
    $Text = $Text -replace "\n.*property.*$property.*?[\s\S]*?\}\n", ''
}


# Add header and output file
$Text = "$Header`n`n$Text"
$Text | Out-File "$BindingsPath/$File"
