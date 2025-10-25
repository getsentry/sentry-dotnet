# This file contains test cases for https://pester.dev/
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. $PSScriptRoot/common.ps1

Describe 'Console apps (<framework>) - normal build' -ForEach @(
    foreach ($fw in $currentFrameworks) { @{ framework = $fw } }
) {
    BeforeAll {
        DotnetNew 'console' 'console-app' $framework
    }

    BeforeEach {
        Remove-Item "./console-app/bin/Release/$framework/*.src.zip" -ErrorAction SilentlyContinue
    }

    It "uploads symbols and sources" {
        $result = RunDotnetWithSentryCLI 'build' 'console-app' $True $True $framework
        $result.UploadedDebugFiles() | Sort-Object -Unique | Should -Be @('console-app.pdb')
        $result.ScriptOutput | Should -AnyElementMatch 'Found 1 debug information file \(1 with embedded sources\)'
        $result.ScriptOutput | Should -AnyElementMatch 'Resolved source code for 0 debug information files'
    }

    It "uploads symbols" {
        $result = RunDotnetWithSentryCLI 'build' 'console-app' $True $False $framework
        $result.UploadedDebugFiles() | Sort-Object -Unique | Should -Be @('console-app.pdb')
        $result.ScriptOutput | Should -AnyElementMatch 'Found 1 debug information file \(1 with embedded sources\)'
    }

    It "uploads sources" {
        $result = RunDotnetWithSentryCLI 'build' 'console-app' $False $True $framework
        $result.ScriptOutput | Should -AnyElementMatch 'Skipping embedded source file: .*/console-app/Program.cs'
        $result.UploadedDebugFiles() | Should -BeNullOrEmpty
    }

    It "uploads nothing when disabled" {
        $result = RunDotnetWithSentryCLI 'build' 'console-app' $False $False $framework
        $result.UploadedDebugFiles() | Should -BeNullOrEmpty
    }
}

Describe 'Console apps (<framework>) - native AOT publish' -ForEach @(
    foreach ($fw in $currentFrameworks) { @{ framework = $fw } }
) {
    BeforeAll {
        DotnetNew 'console' 'console-app' $framework
    }

    BeforeEach {
        Remove-Item "./console-app/bin/Release/$framework/publish" -Recurse -ErrorAction SilentlyContinue
    }

    It "uploads symbols and sources" {
        $result = RunDotnetWithSentryCLI 'publish' 'console-app' $True $True $framework
        $result.ScriptOutput | Should -AnyElementMatch "Preparing upload to Sentry for project 'console-app'"
        if ($IsWindows)
        {
            $result.UploadedDebugFiles() | Sort-Object -Unique | Should -Be @("console-app.pdb")
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

    It "uploads symbols" {
        $result = RunDotnetWithSentryCLI 'publish' 'console-app' $True $False $framework
        $result.ScriptOutput | Should -AnyElementMatch "Preparing upload to Sentry for project 'console-app'"
        if ($IsWindows)
        {
            $result.UploadedDebugFiles() | Sort-Object -Unique | Should -Be @("console-app.pdb")
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

    It "uploads sources" {
        $result = RunDotnetWithSentryCLI 'publish' 'console-app' $False $True $framework
        $result.ScriptOutput | Should -AnyElementMatch "Preparing upload to Sentry for project 'console-app'"
        $sourceBundle = 'console-app.src.zip'
        $result.UploadedDebugFiles() | Sort-Object -Unique | Should -Be @($sourceBundle)
    }

    It "uploads nothing when disabled" {
        $result = RunDotnetWithSentryCLI 'publish' 'console-app' $False $False $framework
        $result.UploadedDebugFiles() | Should -BeNullOrEmpty
    }
}
