# Reference: https://github.com/xamarin/xamarin-macios/blob/main/docs/website/binding_types_reference_guide.md

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $true

$RootPath = (Get-Item $PSScriptRoot).Parent.FullName
$CocoaSdkPath = "$RootPath/modules/sentry-cocoa"
# The Cocoa SDK's SentryObjC headers are staged here by build-sentry-cocoa.sh, which builds
# SentryObjC-Dynamic.xcframework from the modules/sentry-cocoa submodule.
$HeadersPath = "$CocoaSdkPath/Carthage/Headers"
$BindingsPath = "$RootPath/src/Sentry.Bindings.Cocoa"
$BackupPath = "$BindingsPath/obj/_unpatched"

# Ensure running on macOS
if (!$IsMacOS)
{
    Write-Error 'Bindings generation can only be performed on macOS.' `
        -CategoryActivity Error -ErrorAction Stop
}

# Objective Sharpie is pinned in /.config/dotnet-tools.json.
Write-Output 'Restoring the pinned Objective Sharpie dotnet tool.'
dotnet tool restore

# Get iPhone SDK version
$iPhoneSdkVersion = "iphoneos$(xcrun --sdk iphoneos --show-sdk-version)"
Write-Output "iPhoneSdkVersion: $iPhoneSdkVersion"
$iPhoneSdkPath = xcrun --show-sdk-path --sdk iphoneos
Write-Output "iPhoneSdkPath: $iPhoneSdkPath"

# Generate bindings.
# We bind the SentryObjC umbrella header only. It exposes the full public surface (options, SDK
# entry point, event model, scope, enums) plus the structured hybrid API (SentryObjCSDK.internal -
# incl. debug images, scope contexts, and event serialization) via SentryObjC* classes. The classic
# Sentry.framework ObjC surface is no longer bound. See getsentry/sentry-dotnet#5444.
#
# The SentryObjC headers resolve their own imports via `__has_include` guards and don't use the
# `SENTRY_HEADER` macro, so - unlike the classic Sentry.h/Sentry-Swift.h headers we used to bind -
# no header patching is needed before invoking sharpie.
#
# The header must be passed via `--header`; as a bare positional argument sharpie hands it straight
# to Clang, which then parses it as C and fails on the first Objective-C declaration.
Write-Output 'Generating bindings with Objective Sharpie.'
dotnet sharpie bind -sdk $iPhoneSdkVersion `
    --scope "$CocoaSdkPath" `
    --header "$HeadersPath/SentryObjC.h" `
    -o $BindingsPath `
    -c -Wno-objc-property-no-attribute `
    -F"$iPhoneSdkPath/System/Library/SubFrameworks" # needed for UIUtilities.framework in Xcode 26+

# The dotnet tool emits ApiDefinition.cs; the committed file and the csproj both use the plural
# name. Renamed here so this change stays a pure toolchain swap - the file rename is a follow-up.
Move-Item "$BindingsPath/ApiDefinition.cs" "$BindingsPath/ApiDefinitions.cs" -Force

# Ensure backup path exists
if (!(Test-Path $BackupPath))
{
    New-Item -ItemType Directory -Path $BackupPath | Out-Null
}

Push-Location $PSScriptRoot

################################################################################
# Patch StructsAndEnums.cs
################################################################################
$File = 'StructsAndEnums.cs'
Write-Output "Patching $BindingsPath/$File"
Copy-Item "$BindingsPath/$File" -Destination "$BackupPath/$File"
& dotnet run "$PSScriptRoot/patch-cocoa-bindings.cs" "$BindingsPath/$File" | ForEach-Object { Write-Host $_ }

################################################################################
# Patch ApiDefinitions.cs
################################################################################
$File = 'ApiDefinitions.cs'
Write-Output "Patching $BindingsPath/$File"
Copy-Item "$BindingsPath/$File" -Destination "$BackupPath/$File"
& dotnet run "$PSScriptRoot/patch-cocoa-bindings.cs" "$BindingsPath/$File" | ForEach-Object { Write-Host $_ }
