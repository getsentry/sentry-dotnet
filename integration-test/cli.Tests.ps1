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

Describe 'MAUI (<framework>)' -ForEach @(
    @{ framework = $previousFramework }
) -Skip:($env:NO_MOBILE -eq "true") {
    BeforeAll {
        RegisterLocalPackage 'Sentry.Android.AssemblyReader'
        RegisterLocalPackage 'Sentry.Bindings.Android'
        RegisterLocalPackage 'Sentry.Extensions.Logging'
        RegisterLocalPackage 'Sentry.Maui'
        if ($IsMacOS)
        {
            RegisterLocalPackage 'Sentry.Bindings.Cocoa'
        }

        $name = 'maui-app'
        $androidTpv = GetAndroidTpv $framework
        $iosTpv = GetIosTpv $framework

        DotnetNew 'maui' $name $framework

        # Workaround for the missing "ios" workload on Linux, see https://github.com/dotnet/maui/pull/18580
        $tfs = $IsMacos ? "$framework-android$androidTpv;$framework-ios$iosTpv;$framework-maccatalyst$iosTpv" : "$framework-android$androidTpv"
        (Get-Content $name/$name.csproj) -replace '<TargetFrameworks>[^<]+</TargetFrameworks>', "<TargetFrameworks>$tfs</TargetFrameworks>" | Set-Content $name/$name.csproj

        dotnet remove $name/$name.csproj package 'Microsoft.Extensions.Logging.Debug' | ForEach-Object { Write-Host $_ }
        if ($LASTEXITCODE -ne 0)
        {
            throw "Failed to remove package"
        }

        if (Test-Path env:CI)
        {
            dotnet workload restore $name/$name.csproj | ForEach-Object { Write-Host $_ }
            if ($LASTEXITCODE -ne 0)
            {
                throw "Failed to restore workloads."
            }
        }

        AddPackageReference $name 'Sentry.Maui'
    }

    It "uploads symbols and sources for an Android build" {
        $result = RunDotnetWithSentryCLI 'build' 'maui-app' $True $True "$framework-android$androidTpv"
        Write-Host "UploadedDebugFiles: $($result.UploadedDebugFiles() | Out-String)"
        $result.UploadedDebugFiles() | Sort-Object -Unique | Should -Be @(
            'libsentry-android.so',
            'libsentry.so',
            'libsentrysupplemental.so',
            'libxamarin-app.so',
            'maui-app.pdb'
        )
        $result.ScriptOutput | Should -AnyElementMatch 'Uploaded a total of 1 new mapping files'
        $result.ScriptOutput | Should -AnyElementMatch "Found 23 debug information files \(1 with embedded sources\)"
    }

    It "uploads symbols and sources for an iOS build" -Skip:(!$IsMacOS) {
        $result = RunDotnetWithSentryCLI 'build' 'maui-app' $True $True "$framework-ios$iosTpv"
        Write-Host "UploadedDebugFiles: $($result.UploadedDebugFiles() | Out-String)"
        $result.UploadedDebugFiles() | Sort-Object -Unique | Should -Be @(
            'libmono-component-debugger.dylib',
            'libmono-component-diagnostics_tracing.dylib',
            'libmono-component-hot_reload.dylib',
            'libmono-component-marshal-ilgen.dylib',
            'libmonosgen-2.0.dylib',
            'libSystem.Globalization.Native.dylib',
            'libSystem.IO.Compression.Native.dylib',
            'libSystem.Native.dylib',
            'libSystem.Net.Security.Native.dylib',
            'libSystem.Security.Cryptography.Native.Apple.dylib',
            'libxamarin-dotnet-debug.dylib',
            'libxamarin-dotnet.dylib',
            'maui-app',
            'maui-app.pdb',
            'Microsoft.iOS.pdb',
            'Microsoft.Maui.Controls.Compatibility.pdb',
            'Microsoft.Maui.Controls.pdb',
            'Microsoft.Maui.Controls.Xaml.pdb',
            'Microsoft.Maui.Essentials.pdb',
            'Microsoft.Maui.Graphics.pdb',
            'Microsoft.Maui.pdb',
            'Sentry'
        )
        $result.ScriptOutput | Should -AnyElementMatch "Found 77 debug information files \(8 with embedded sources\)"
    }
}
