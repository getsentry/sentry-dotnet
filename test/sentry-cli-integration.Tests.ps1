using namespace System.Runtime.InteropServices

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# In CI, the module is loaded automatically
if (!(Test-Path env:CI ))
{
    Import-Module $PSScriptRoot/../../github-workflows/sentry-cli/integration-test/action.psm1 -Force
}

function Should-AnyElementMatch ($ActualValue, [string]$ExpectedValue, [switch] $Negate, [string] $Because)
{
    <#
    .SYNOPSIS
        Asserts whether any item in the collection matches the expected value
    .EXAMPLE
        'foo','bar','foobar' | Should -AnyElementMatch 'oob'

        This should pass because 'oob' is a substring of 'foobar'.
    #>

    $filtered = $ActualValue | Where-Object { $_ -match $ExpectedValue }
    [bool] $succeeded = @($filtered).Count -gt 0
    if ($Negate) { $succeeded = -not $succeeded }

    if (-not $succeeded)
    {
        if ($Negate)
        {
            $failureMessage = "Expected string '$ExpectedValue' to match no elements in collection @($($ActualValue -join ', '))$(if($Because) { " because $Because"})."
        }
        else
        {
            $failureMessage = "Expected string '$ExpectedValue' to match any element in collection @($($ActualValue -join ', '))$(if($Because) { " because $Because"})."
        }
    }

    return [pscustomobject]@{
        Succeeded      = $succeeded
        FailureMessage = $failureMessage
    }
}

BeforeDiscovery {
    Add-ShouldOperator -Name AnyElementMatch `
        -InternalName 'Should-AnyElementMatch' `
        -Test ${function:Should-AnyElementMatch} `
        -SupportsArrayInput
}

BeforeAll {
    $env:SENTRY_LOG_LEVEL = 'debug';

    function DotnetBuild([string]$Sample, [bool]$Symbols, [bool]$Sources, [string]$TargetFramework = '')
    {
        $rootDir = "$(Get-Item $PSScriptRoot/../../)"
        $framework = $TargetFramework -eq '' ? '' : @('-f', $TargetFramework)

        Invoke-SentryServer {
            Param([string]$url)
            Write-Host "Building $Sample"
            dotnet build "samples/$sample/$sample.csproj" -c Release $framework --no-restore --nologo `
                /p:UseSentryCLI=true `
                /p:SentryUploadSymbols=$Symbols `
                /p:SentryUploadSources=$Sources `
                /p:SentryOrg=org `
                /p:SentryProject=project `
                /p:SentryUrl=$url `
                /p:SentryAuthToken=dummy `
            | ForEach-Object {
                if ($_ -match "^Time Elapsed ")
                {
                    "Time Elapsed [value removed]"
                }
                elseif ($_ -match "\[[0-9/]+\]")
                {
                    # Skip lines like `[102/103] Sentry.Samples.Maui.dll -> Sentry.Samples.Maui.dll.so`
                }
                else
                {
                    "$_". `
                        Replace($rootDir, '').  `
                        Replace('\', '/')
                }
            }
            | ForEach-Object {
                Write-Host "  $_"
                $_
            }
        }
    }
}

Describe 'CLI-integration' {

    It "uploads symbols and sources for a console app build" {
        $result = DotnetBuild 'Sentry.Samples.Console.Basic' $True $True
        $result.ScriptOutput | Should -Contain 'Build succeeded.'
        $result.HasErrors() | Should -BeFalse
        $result.UploadedDebugFiles() | Sort-Object -Unique | Should -Be @(
            'Sentry.pdb',
            'Sentry.Samples.Console.Basic.pdb')
        $result.ScriptOutput | Should -AnyElementMatch 'Skipping embedded source file: dotnet/samples/Sentry.Samples.Console.Basic/Program.cs'
    }

    It "uploads symbols for a console app build" {
        $result = DotnetBuild 'Sentry.Samples.Console.Basic' $True $False
        $result.ScriptOutput | Should -Contain 'Build succeeded.'
        $result.HasErrors() | Should -BeFalse
        $result.UploadedDebugFiles() | Sort-Object -Unique | Should -Be @(
            'Sentry.pdb',
            'Sentry.Samples.Console.Basic.pdb')
    }

    It "uploads sources for a console app build" {
        $result = DotnetBuild 'Sentry.Samples.Console.Basic' $False $True
        $result.ScriptOutput | Should -Contain 'Build succeeded.'
        $result.HasErrors() | Should -BeFalse
        $result.ScriptOutput | Should -AnyElementMatch 'Skipping embedded source file: dotnet/samples/Sentry.Samples.Console.Basic/Program.cs'
        $result.UploadedDebugFiles() | Should -BeNullOrEmpty
    }

    It "uploads nothing for a console app build when disabled" {
        $result = DotnetBuild 'Sentry.Samples.Console.Basic' $False $False
        $result.ScriptOutput | Should -Contain 'Build succeeded.'
        $result.HasErrors() | Should -BeFalse
        $result.UploadedDebugFiles() | Sort-Object -Unique | Should -Be @()
    }

    It "uploads symbols and sources for a MAUI Android app build" {
        $result = DotnetBuild 'Sentry.Samples.Maui' $True $True 'net7.0-android'
        $result.ScriptOutput | Should -Contain 'Build succeeded.'
        $result.HasErrors() | Should -BeFalse
        $result.UploadedDebugFiles() | Sort-Object -Unique | Should -Be @(
            'Sentry.Android.AssemblyReader.pdb',
            'Sentry.Bindings.Android.pdb',
            'Sentry.Extensions.Logging.pdb',
            'Sentry.Maui.pdb',
            'Sentry.pdb',
            'Sentry.Samples.Maui.pdb'
        )
        $result.ScriptOutput | Should -AnyElementMatch 'Skipping embedded source file: dotnet/samples/Sentry.Samples.Maui/MauiProgram.cs'
    }

    if (![RuntimeInformation]::IsOSPlatform([OSPlatform]::OSX))
    {
        # Remaining tests run on macOS only
        return
    }

    It "uploads symbols and sources for a MAUI iOS app build" {
        $result = DotnetBuild 'Sentry.Samples.Maui' $True $True 'net7.0-ios'
        $result.ScriptOutput | Should -Contain 'Build succeeded.'
        $result.HasErrors() | Should -BeFalse
        $result.UploadedDebugFiles() | Sort-Object -Unique | Should -Be @(
            'libmono-component-debugger.dylib',
            'libmono-component-diagnostics_tracing.dylib',
            'libmono-component-hot_reload.dylib',
            'libmonosgen-2.0.dylib',
            'libSystem.IO.Compression.Native.dylib',
            'libSystem.Native.dylib',
            'libSystem.Net.Security.Native.dylib',
            'libSystem.Security.Cryptography.Native.Apple.dylib',
            'libxamarin-dotnet-debug.dylib',
            'libxamarin-dotnet.dylib',
            'Sentry',
            'Sentry.Bindings.Cocoa.pdb',
            'Sentry.Extensions.Logging.pdb',
            'Sentry.Maui.pdb',
            'Sentry.pdb',
            'Sentry.Samples.Maui',
            'Sentry.Samples.Maui.pdb'
        )
        $result.ScriptOutput | Should -AnyElementMatch 'Skipping embedded source file: dotnet/samples/Sentry.Samples.Maui/MauiProgram.cs'
    }
}
