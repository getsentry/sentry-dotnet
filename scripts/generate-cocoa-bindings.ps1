Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$RootPath = (Get-Item $PSScriptRoot).Parent.FullName
$CocoaSdkPath = "$RootPath/modules/sentry-cocoa"
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
    Write-Output 'Xamarin.iOS not found. Attempting to install via Homebrew.'
    brew install --cask xamarin-ios

    if (!(Test-Path '/Library/Frameworks/Xamarin.iOS.framework/Versions/Current/lib/64bits/iOS/Xamarin.iOS.dll'))
    {
        Write-Error 'Xamarin.iOS not found. Try installing manually from: https://learn.microsoft.com/en-us/xamarin/ios/get-started/installation/.'
    }
}

# Get iPhone SDK version
$iPhoneSdkVersion = sharpie xcode -sdks | grep -o -m 1 'iphoneos\S*'
Write-Output "iPhoneSdkVersion: $iPhoneSdkVersion"

# Generate bindings
Write-Output 'Generating bindings with Objective Sharpie.'
sharpie bind -sdk $iPhoneSdkVersion `
    -scope "$CocoaSdkPath/Carthage/Headers" `
    "$CocoaSdkPath/Carthage/Headers/Sentry.h" `
    "$CocoaSdkPath/Carthage/Headers/PrivateSentrySDKOnly.h" `
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

# This enum resides in the Sentry-Swift.h
# Appending it here so we don't need to import and create bindings for the entire header
$SentryLevel = @'

    [Native]
    internal enum SentryLevel : ulong
    {
        None = 0,
        Debug = 1,
        Info = 2,
        Warning = 3,
        Error = 4,
        Fatal = 5
    }
'@

# This enum resides in the Sentry-Swift.h
# Appending it here so we don't need to import and create bindings for the entire header
$SentryTransactionNameSource = @'

    [Native]
    internal enum SentryTransactionNameSource : long
    {
        Custom = 0,
        Url = 1,
        Route = 2,
        View = 3,
        Component = 4,
        Task = 5
    }
'@

$Text += "`r`n$SentryLevel"
$Text += "`r`n$SentryTransactionNameSource"

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
$Text = $Text -replace '\[Verify \(MethodToProperty\)\]\n\s*(.+ (?:Hash|Value|DefaultIntegrations) \{ get; \})', '$1'
$Text = $Text -replace '\[Verify \(MethodToProperty\)\]\n\s*(.+) \{ get; \}', '$1();'

# Allow weakly typed NSArray
# We have some that accept either NSString or NSRegularExpression, which have no common type so they use NSObject
$Text = $Text -replace '\s*\[Verify \(StronglyTypedNSArray\)\]\n', ''

# Fix broken line comment
$Text = $Text -replace '(DEPRECATED_MSG_ATTRIBUTE\()\n\s*', '$1'

# Remove default IsEqual implementation (already implemented by NSObject)
$Text = $Text -replace '(?ms)\n?^ *// [^\n]*isEqual:.*?$.*?;\n', ''

# Replace obsolete platform avaialbility attributes
$Text = $Text -replace '([\[,] )MacCatalyst \(', '$1Introduced (PlatformName.MacCatalyst, '
$Text = $Text -replace '([\[,] )Mac \(', '$1Introduced (PlatformName.MacOSX, '
$Text = $Text -replace '([\[,] )iOS \(', '$1Introduced (PlatformName.iOS, '

# Make interface partial if we need to access private APIs.  Other parts will be defined in PrivateApiDefinitions.cs
$Text = $Text -replace '(?m)^interface SentryScope', 'partial $&'

# Prefix SentryBreadcrumb.Serialize and SentryScope.Serialize with new (since these hide the base method)
$Text = $Text -replace '(?m)(^\s*\/\/[^\r\n]*$\s*\[Export \("serialize"\)\]$\s*)(NSDictionary)', '${1}new $2'

$Text = $Text -replace '.*SentryEnvelope .*?[\s\S]*?\n\n', ''
$Text = $Text -replace '.*typedef.*SentryOnAppStartMeasurementAvailable.*?[\s\S]*?\n\n', ''
$Text = $Text -replace '\n.*SentryReplayBreadcrumbConverter.*?[\s\S]*?\);\n', ''

$propertiesToRemove = @(
    'SentryAppStartMeasurement',
    'SentryOnAppStartMeasurementAvailable',
    'SentryMetricsAPI',
    'SentryExperimentalOptions',
    'description',
    'enableMetricKitRawPayload'
)

foreach ($property in $propertiesToRemove) {
    $Text = $Text -replace "\n.*property.*$property.*?[\s\S]*?\}\n", ''
}

# This interface resides in the Sentry-Swift.h
# Appending it here so we don't need to import and create bindings for the entire header
$SentryId = @'

// @interface SentryId : NSObject
[BaseType (typeof(NSObject), Name = "_TtC6Sentry8SentryId")]
[Internal]
interface SentryId
{
    // @property (nonatomic, strong, class) SentryId * _Nonnull empty;
    [Static]
    [Export ("empty", ArgumentSemantic.Strong)]
    SentryId Empty { get; set; }

    // @property (readonly, copy, nonatomic) NSString * _Nonnull sentryIdString;
    [Export ("sentryIdString")]
    string SentryIdString { get; }

    // -(instancetype _Nonnull)initWithUuid:(NSUUID * _Nonnull)uuid __attribute__((objc_designated_initializer));
    [Export ("initWithUuid:")]
    [DesignatedInitializer]
    NativeHandle Constructor (NSUuid uuid);

    // -(instancetype _Nonnull)initWithUUIDString:(NSString * _Nonnull)uuidString __attribute__((objc_designated_initializer));
    [Export ("initWithUUIDString:")]
    [DesignatedInitializer]
    NativeHandle Constructor (string uuidString);

    // @property (readonly, nonatomic) NSUInteger hash;
    [Export ("hash")]
    nuint Hash { get; }
}
'@

$Text += "`r`n$SentryId"

# Add header and output file
$Text = "$Header`n`n$Text"
$Text | Out-File "$BindingsPath/$File"
