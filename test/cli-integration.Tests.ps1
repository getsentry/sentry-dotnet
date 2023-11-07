# This file contains test cases for https://pester.dev/
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. $PSScriptRoot/integration-test-setup.ps1

Describe 'Console apps (<framework>) - normal build' -ForEach @(
    @{ framework = "net7.0" }
) {
    BeforeAll {
        DotnetNew 'console' './temp/console-app' $framework
    }

    BeforeEach {
        Remove-Item "./temp/console-app/bin/Release/$framework/*.src.zip" -ErrorAction SilentlyContinue
    }

    It "uploads symbols and sources" {
        $result = RunDotnet 'build' './temp/console-app' $True $True $framework
        $result.UploadedDebugFiles() | Sort-Object -Unique | Should -Be @('console-app.pdb')
        $result.ScriptOutput | Should -AnyElementMatch 'Found 1 debug information file \(1 with embedded sources\)'
        $result.ScriptOutput | Should -AnyElementMatch 'Resolved source code for 0 debug information files'
    }

    It "uploads symbols" {
        $result = RunDotnet 'build' './temp/console-app' $True $False $framework
        $result.UploadedDebugFiles() | Sort-Object -Unique | Should -Be @('console-app.pdb')
        $result.ScriptOutput | Should -AnyElementMatch 'Found 1 debug information file \(1 with embedded sources\)'
    }

    It "uploads sources" {
        $result = RunDotnet 'build' './temp/console-app' $False $True $framework
        $result.ScriptOutput | Should -AnyElementMatch 'Skipping embedded source file: .*/temp/console-app/Program.cs'
        $result.UploadedDebugFiles() | Should -BeNullOrEmpty
    }

    It "uploads nothing when disabled" {
        $result = RunDotnet 'build' './temp/console-app' $False $False $framework
        $result.UploadedDebugFiles() | Should -BeNullOrEmpty
    }
}

Describe 'Console apps (<framework>) - native AOT publish' -ForEach @(
    @{ framework = "net7.0" },
    @{ framework = "net8.0" }
) {
    BeforeAll {
        DotnetNew 'console' './temp/console-app' $framework
    }

    BeforeEach {
        Remove-Item "./temp/console-app/bin/Release/$framework/publish" -Recurse -ErrorAction SilentlyContinue
    }

    It "uploads symbols and sources" -Skip:($IsMacOS -and $framework -eq 'net7.0') {
        $result = RunDotnet 'publish' './temp/console-app' $True $True $framework
        $result.ScriptOutput | Should -AnyElementMatch "Preparing upload to Sentry for project 'console-app'"
        if ($IsWindows -or ($IsLinux -and $framework -eq 'net7.0'))
        {
            $extension = $IsLinux ? '' : '.pdb'
            $result.UploadedDebugFiles() | Sort-Object -Unique | Should -Be @("console-app$extension")
            $result.ScriptOutput | Should -AnyElementMatch 'Found 1 debug information file'
            $result.ScriptOutput | Should -AnyElementMatch 'Resolved source code for 1 debug information file'
        }
        else
        {
            # On macOS, only the dwarf is uploaded from dSYM so it has the same name as the actual executable.
            $debugExtension = $IsLinux ? '.dbg' : ''
            $result.UploadedDebugFiles() | Sort-Object -Unique | Should -Be (@(
                    'console-app', "console-app$debugExtension") | Sort-Object -Unique)
            $result.ScriptOutput | Should -AnyElementMatch 'Found 2 debug information files'
            $result.ScriptOutput | Should -AnyElementMatch 'Resolved source code for 1 debug information file'
        }
    }

    It "uploads symbols" -Skip:($IsMacOS -and $framework -eq 'net7.0') {
        $result = RunDotnet 'publish' './temp/console-app' $True $False $framework
        $result.ScriptOutput | Should -AnyElementMatch "Preparing upload to Sentry for project 'console-app'"
        if ($IsWindows -or ($IsLinux -and $framework -eq 'net7.0'))
        {
            $extension = $IsLinux ? '' : '.pdb'
            $result.UploadedDebugFiles() | Sort-Object -Unique | Should -Be @("console-app$extension")
            $result.ScriptOutput | Should -AnyElementMatch 'Found 1 debug information file'
        }
        else
        {
            # On macOS, only the dwarf is uploaded from dSYM so it has the same name as the actual executable.
            $debugExtension = $IsLinux ? '.dbg' : ''
            $result.UploadedDebugFiles() | Sort-Object -Unique | Should -Be (@(
                    'console-app', "console-app$debugExtension") | Sort-Object -Unique)
            $result.ScriptOutput | Should -AnyElementMatch 'Found 2 debug information files'
        }
    }

    It "uploads sources" -Skip:($IsMacOS -and $framework -eq 'net7.0') {
        $result = RunDotnet 'publish' './temp/console-app' $False $True $framework
        $result.ScriptOutput | Should -AnyElementMatch "Preparing upload to Sentry for project 'console-app'"
        $sourceBundle = 'console-app.src.zip'
        if ($IsMacOS -or ($IsLinux -and $framework -eq 'net7.0'))
        {
            $sourceBundle = 'console-app.src.zip'
        }
        $result.UploadedDebugFiles() | Sort-Object -Unique | Should -Be @($sourceBundle)
    }

    It "uploads nothing when disabled" -Skip:($IsMacOS -and $framework -eq 'net7.0') {
        $result = RunDotnet 'publish' './temp/console-app' $False $False $framework
        $result.UploadedDebugFiles() | Should -BeNullOrEmpty
    }
}

Describe 'MAUI' {
    BeforeAll {
        RegisterLocalPackage 'Sentry.Android.AssemblyReader'
        RegisterLocalPackage 'Sentry.Bindings.Android'
        RegisterLocalPackage 'Sentry.Extensions.Logging'
        RegisterLocalPackage 'Sentry.Maui'
        if ($IsMacOS)
        {
            RegisterLocalPackage 'Sentry.Bindings.Cocoa'
        }
        DotnetNew 'maui' './temp/maui-app' 'net7.0'
    }

    It "uploads symbols and sources for an Android build" {
        $result = RunDotnet 'build' './temp/maui-app' $True $True 'net7.0-android'
        $result.UploadedDebugFiles() | Sort-Object -Unique | Should -Be @('maui-app.pdb')
        $result.ScriptOutput | Should -AnyElementMatch 'Found 1 debug information file \(1 with embedded sources\)'
    }

    It "uploads symbols and sources for an iOS build" -Skip:(!$IsMacOS) {
        $result = RunDotnet 'build' './temp/maui-app' $True $True 'net7.0-ios'
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
            'maui-app',
            'maui-app.pdb'
        )
        $nonZeroNumberRegex = '[1-9][0-9]*';
        $result.ScriptOutput | Should -AnyElementMatch "Found $nonZeroNumberRegex debug information files \($nonZeroNumberRegex with embedded sources\)"
    }
}
