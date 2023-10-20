# This file contains test cases for https://pester.dev/

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Push-Location $PSScriptRoot/../

# In CI, the module is loaded automatically
if (!(Test-Path env:CI ))
{
    Import-Module $PSScriptRoot/../../github-workflows/sentry-cli/integration-test/action.psm1 -Force
}

function ShouldAnyElementMatch ($ActualValue, [string]$ExpectedValue, [switch] $Negate, [string] $Because)
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
    else
    {
        $failureMessage = $null
    }

    return [pscustomobject]@{
        Succeeded      = $succeeded
        FailureMessage = $failureMessage
    }
}

BeforeDiscovery {
    Add-ShouldOperator -Name AnyElementMatch `
        -InternalName 'ShouldAnyElementMatch' `
        -Test ${function:ShouldAnyElementMatch} `
        -SupportsArrayInput
}

BeforeAll {
    $env:SENTRY_LOG_LEVEL = 'debug';

    function RunDotnet([string] $action, [string]$Sample, [bool]$Symbols, [bool]$Sources, [string]$TargetFramework = 'net7.0')
    {
        $rootDir = "$(Get-Item $PSScriptRoot/../../)"

        $result = Invoke-SentryServer {
            Param([string]$url)
            Write-Host "::group::Building $Sample"
            try
            {
                dotnet $action "samples/$sample/$sample.csproj" -c Release -f $TargetFramework --no-restore --nologo `
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
            finally
            {
                Write-Host "::endgroup::"
            }
        }

        if ($action -eq "build")
        {
            $result.ScriptOutput | Should -Contain 'Build succeeded.'
        }
        elseif ($action -eq "publish")
        {
            $result.ScriptOutput | Should -AnyElementMatch "$sample -> .*samples/$sample/bin/Release/$TargetFramework/.*/publish"
        }
        $result.ScriptOutput | Should -Not -AnyElementMatch "Preparing upload to Sentry for project 'Sentry'"
        $result.HasErrors() | Should -BeFalse
        $result
    }
}

Describe 'Console apps - normal build' {
    BeforeAll {
        Remove-Item 'samples/Sentry.Samples.Console.Basic/bin/' -Recurse -Verbose
    }

    BeforeEach {
        if (Get-Item 'samples/Sentry.Samples.Console.Basic/bin/Release/*/*.src.zip' -ErrorAction SilentlyContinue)
        {
            Remove-Item 'samples/Sentry.Samples.Console.Basic/bin/Release/*/*.src.zip'
        }
    }

    It "uploads symbols and sources" {
        $result = RunDotnet 'build' 'Sentry.Samples.Console.Basic' $True $True
        $result.UploadedDebugFiles() | Sort-Object -Unique | Should -Be @(
            'Sentry.pdb',
            'Sentry.Samples.Console.Basic.pdb')
        $result.ScriptOutput | Should -AnyElementMatch 'Found 2 debug information files \(2 with embedded sources\)'
        $result.ScriptOutput | Should -AnyElementMatch 'Resolved source code for 0 debug information files'
    }

    It "uploads symbols" {
        $result = RunDotnet 'build' 'Sentry.Samples.Console.Basic' $True $False
        $result.UploadedDebugFiles() | Sort-Object -Unique | Should -Be @(
            'Sentry.pdb',
            'Sentry.Samples.Console.Basic.pdb')
        $result.ScriptOutput | Should -AnyElementMatch 'Found 2 debug information files \(2 with embedded sources\)'
    }

    It "uploads sources" {
        $result = RunDotnet 'build' 'Sentry.Samples.Console.Basic' $False $True
        $result.ScriptOutput | Should -AnyElementMatch 'Skipping embedded source file: .*/samples/Sentry.Samples.Console.Basic/Program.cs'
        $result.UploadedDebugFiles() | Should -BeNullOrEmpty
    }

    It "uploads nothing when disabled" {
        $result = RunDotnet 'build' 'Sentry.Samples.Console.Basic' $False $False
        $result.UploadedDebugFiles() | Should -BeNullOrEmpty
    }
}

Describe 'Console apps - native AOT publish (<framework>)' -ForEach @(
    @{ framework = "net7.0" },
    @{ framework = "net8.0" }
) {
    BeforeAll {
        $runtime = $IsWindows ? 'win-x64' : $IsLinux ? 'linux-x64' : "osx-$(uname -m)"
        Write-Host "Running dotnet restore for Sentry.Samples.Console.Basic, runtime: $runtime"
        dotnet restore samples/Sentry.Samples.Console.Basic/Sentry.Samples.Console.Basic.csproj --runtime $runtime
    }

    BeforeEach {
        Remove-Item 'samples/Sentry.Samples.Console.Basic/bin/Release/*/*/publish' -Recurse -Verbose
    }

    It "uploads symbols and sources (<framework>)" -Skip:($IsMacOS -and $framework -eq 'net7.0') {
        $result = RunDotnet 'publish' 'Sentry.Samples.Console.Basic' $True $True $framework
        $result.ScriptOutput | Should -AnyElementMatch "Preparing upload to Sentry for project 'Sentry.Samples.Console.Basic'"
        if ($IsWindows -or ($IsLinux -and $framework -eq 'net7.0'))
        {
            $extension = $IsLinux ? '' : '.pdb'
            $result.UploadedDebugFiles() | Sort-Object -Unique | Should -Be @("Sentry.Samples.Console.Basic$extension")
            $result.ScriptOutput | Should -AnyElementMatch 'Found 1 debug information file'
            $result.ScriptOutput | Should -AnyElementMatch 'Resolved source code for 1 debug information file'
        }
        else
        {
            # On macOS, only the dwarf is uploaded from dSYM so it has the same name as the actual executable.
            $debugExtension = $IsLinux ? '.dbg' : ''
            $result.UploadedDebugFiles() | Sort-Object -Unique | Should -Be (@(
                    'Sentry.Samples.Console.Basic', "Sentry.Samples.Console.Basic$debugExtension") | Sort-Object -Unique)
            $result.ScriptOutput | Should -AnyElementMatch 'Found 2 debug information files'
            $result.ScriptOutput | Should -AnyElementMatch 'Resolved source code for 1 debug information file'
        }
    }

    It "uploads symbols (<framework>)" -Skip:($IsMacOS -and $framework -eq 'net7.0') {
        $result = RunDotnet 'publish' 'Sentry.Samples.Console.Basic' $True $False $framework
        $result.ScriptOutput | Should -AnyElementMatch "Preparing upload to Sentry for project 'Sentry.Samples.Console.Basic'"
        if ($IsWindows -or ($IsLinux -and $framework -eq 'net7.0'))
        {
            $extension = $IsLinux ? '' : '.pdb'
            $result.UploadedDebugFiles() | Sort-Object -Unique | Should -Be @("Sentry.Samples.Console.Basic$extension")
            $result.ScriptOutput | Should -AnyElementMatch 'Found 1 debug information file'
        }
        else
        {
            # On macOS, only the dwarf is uploaded from dSYM so it has the same name as the actual executable.
            $debugExtension = $IsLinux ? '.dbg' : ''
            $result.UploadedDebugFiles() | Sort-Object -Unique | Should -Be (@(
                    'Sentry.Samples.Console.Basic', "Sentry.Samples.Console.Basic$debugExtension") | Sort-Object -Unique)
            $result.ScriptOutput | Should -AnyElementMatch 'Found 2 debug information files'
        }
    }

    It "uploads sources (<framework>)" -Skip:($IsMacOS -and $framework -eq 'net7.0') {
        $result = RunDotnet 'publish' 'Sentry.Samples.Console.Basic' $False $True $framework
        $result.ScriptOutput | Should -AnyElementMatch "Preparing upload to Sentry for project 'Sentry.Samples.Console.Basic'"
        $sourceBundle = 'Sentry.Samples.Console.Basic.src.zip'
        if ($IsMacOS -or ($IsLinux -and $framework -eq 'net7.0'))
        {
            $sourceBundle = 'Sentry.Samples.Console.src.zip'
        }
        $result.UploadedDebugFiles() | Sort-Object -Unique | Should -Be @($sourceBundle)
    }

    It "uploads nothing when disabled (<framework>)" -Skip:($IsMacOS -and $framework -eq 'net7.0') {
        $result = RunDotnet 'publish' 'Sentry.Samples.Console.Basic' $False $False $framework
        $result.UploadedDebugFiles() | Should -BeNullOrEmpty
    }
}

Describe 'MAUI' {
    It "uploads symbols and sources for an Android build" {
        $result = RunDotnet 'build' 'Sentry.Samples.Maui' $True $True 'net7.0-android'
        $result.UploadedDebugFiles() | Sort-Object -Unique | Should -Be @(
            'Sentry.Android.AssemblyReader.pdb',
            'Sentry.Bindings.Android.pdb',
            'Sentry.Extensions.Logging.pdb',
            'Sentry.Maui.pdb',
            'Sentry.pdb',
            'Sentry.Samples.Maui.pdb'
        )
        $result.ScriptOutput | Should -AnyElementMatch 'Found 6 debug information files \(6 with embedded sources\)'
    }

    It "uploads symbols and sources for an iOS build" -Skip:(!$IsMacOS) {
        $result = RunDotnet 'build' 'Sentry.Samples.Maui' $True $True 'net7.0-ios'
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
        $nonZeroNumberRegex = '[1-9][0-9]*';
        $result.ScriptOutput | Should -AnyElementMatch "Found $nonZeroNumberRegex debug information files \($nonZeroNumberRegex with embedded sources\)"
    }
}
