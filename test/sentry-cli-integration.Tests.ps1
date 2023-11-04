# This file contains test cases for https://pester.dev/
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

BeforeAll {
    $env:SENTRY_LOG_LEVEL = 'debug';
    . $PSScriptRoot/test-functions.ps1
}

Describe 'Console apps - normal build' {
    BeforeAll {
        # Make sure we start with a clean project so that we get the same builds as in CI.
        git clean -ffxd samples/Sentry.Samples.Console.Basic
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
        # Make sure we start with a clean project so that we get the same builds as in CI.
        git clean -ffxd samples/Sentry.Samples.Console.Basic
        $runtime = $IsWindows ? 'win-x64' : $IsLinux ? 'linux-x64' : "osx-" + ($(uname -m) -eq 'arm64' ? 'arm64' : 'x64');
        Write-Host "Running dotnet restore for Sentry.Samples.Console.Basic, runtime: $runtime"
        dotnet restore samples/Sentry.Samples.Console.Basic/Sentry.Samples.Console.Basic.csproj --runtime $runtime
    }

    BeforeEach {
        if (Get-Item 'samples/Sentry.Samples.Console.Basic/bin/Release/*/*/publish' -ErrorAction SilentlyContinue)
        {
            Remove-Item 'samples/Sentry.Samples.Console.Basic/bin/Release/*/*/publish' -Recurse -Verbose
        }
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
