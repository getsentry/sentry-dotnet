Set-StrictMode -Version Latest

$RootPath = (Get-Item $PSScriptRoot).Parent.FullName
$CocoaSdkPath = "$RootPath/modules/sentry-cocoa"
$BindingsPath = "$RootPath/src/Sentry.Bindings.Cocoa"
$BackupPath = "$BindingsPath/obj/_unpatched"

# Ensure running on macOS
if (!$IsMacOS) {
    Write-Error 'Bindings generation can only be performed on macOS.' `
        -CategoryActivity Error -ErrorAction Stop
}

# Ensure Objective Sharpie is installed
if (!(Get-Command sharpie -ErrorAction SilentlyContinue)) {
    Write-Output 'Objective Sharpie not found.  Attempting to install via Homebrew.'
    brew install --cask objectivesharpie
}
if (!(Get-Command sharpie -ErrorAction SilentlyContinue)) {
    Write-Error 'Could not install Objective Sharpie automatically.  Try installing from https://aka.ms/objective-sharpie manually.' `
        -CategoryActivity Error -ErrorAction Stop
}

# Generate bindings
Write-Output 'Generating bindings with Objective Sharpie.'
sharpie bind -sdk iphoneos -quiet `
    -scope "$CocoaSdkPath/Carthage/Headers" `
    "$CocoaSdkPath/Carthage/Headers/Sentry.h" `
    "$CocoaSdkPath/Carthage/Headers/PrivateSentrySDKOnly.h" `
    -o $BindingsPath

# Ensure backup path exists
if (!(Test-Path $BackupPath)) {
    New-Item -ItemType Directory -Path $BackupPath | Out-Null
}

################################################################################
# Patch StructsAndEnums.cs
################################################################################
$File = 'StructsAndEnums.cs'
Write-Output "Patching $File"
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

$Text | Out-File "$BindingsPath/$File"

################################################################################
# Patch ApiDefinitions.cs
################################################################################
$File = 'ApiDefinitions.cs'
Write-Output "Patching $File"
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
$Text = $Text -replace ': INSCopying,', ':' -replace '[:,] INSCopying', ''

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
$Text = $Text -replace 'byte\[\] SentryVersionString', "[return: PlainString]`n    NSString SentryVersionString"
$Text = $Text -replace '(?m)(.*\n){2}^\s{4}NSString k.+?\n\n?', ''
$Text = $Text -replace '(?m)(.*\n){4}^partial interface Constants\n{\n}\n', ''
$Text = $Text -replace '\[Verify \(ConstantsInterfaceAssociation\)\]\n', ''

# Remove duplicate attributes
$s = 'partial interface Constants'
$t = $Text -split $s, 2
$t[1] = $t[1] -replace "\[Static\]\n\[Internal\]\n$s", $s
$Text = $t -join $s

# Update MethodToProperty translations
$Text = $Text -replace '(Export \("get\w+"\)\]\n)\s*\[Verify \(MethodToProperty\)\]\n(.+ \{ get; \})', '$1$2'
$Text = $Text -replace '\[Verify \(MethodToProperty\)\]\n\s*(.+ (?:Hash|Value|DefaultIntegrations) \{ get; \})', '$1'
$Text = $Text -replace '\[Verify \(MethodToProperty\)\]\n\s*(.+) \{ get; \}', '$1();'

# Allow weakly typed NSArray
# We have some that accept either NSString or NSRegularExpression, which have no common type so they use NSObject
$Text = $Text -replace '\s*\[Verify \(StronglyTypedNSArray\)\]\n', ''

# Fix broken line comment
$Text = $Text -replace '(DEPRECATED_MSG_ATTRIBUTE\()\n\s*', '$1'

# Remove APIs that use non-public objects
$Text = $Text -replace '(?ms)\n?^ *// [^\n]*(?:SentryTraceContext|SentryEnvelope|SentrySession)\b.*?$.*?[;}]\n', ''

# Remove default IsEqual implementation (already implemented by NSObject)
$Text = $Text -replace '(?ms)\n?^ *// [^\n]*isEqual:.*?$.*?;\n', ''

# Replace obsolete platform avaialbility attributes
$Text = $Text -replace '([\[,] )MacCatalyst \(', '$1Introduced (PlatformName.MacCatalyst, '
$Text = $Text -replace '([\[,] )Mac \(', '$1Introduced (PlatformName.MacOSX, '
$Text = $Text -replace '([\[,] )iOS \(', '$1Introduced (PlatformName.iOS, '

# Make interface partial if we need to access private APIs.  Other parts will be defined in PrivateApiDefinitions.cs
$Text = $Text -replace '(?m)^interface SentryScope', 'partial $&'

$Text | Out-File "$BindingsPath/$File"
Write-Output 'Done!'
