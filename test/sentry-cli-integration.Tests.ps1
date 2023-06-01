using namespace System.Runtime.InteropServices

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# In CI, the module is loaded automatically
if (!(Test-Path env:CI ))
{
    Import-Module $PSScriptRoot/../../github-workflows/sentry-cli/integration-test/action.psm1 -Force
}

BeforeAll {
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
            'Sentry.Samples.Console.Basic.pdb',
            'Sentry.Samples.Console.Basic.src.zip')
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
        $result.UploadedDebugFiles() | Sort-Object -Unique | Should -Be @(
            'Sentry.Samples.Console.Basic.src.zip')
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
            'Sentry.Samples.Maui.pdb',
            'Sentry.Samples.Maui.src.zip'
        )
    }

    if (![RuntimeInformation]::IsOSPlatform([OSPlatform]::OSX)) {
        # Remaining tests run on macOS only
        return
    }

    It "uploads symbols and sources for a MAUI iOS app build" {
        $result = DotnetBuild 'Sentry.Samples.Maui' $True $True 'net7.0-ios'
        $result.ScriptOutput | Should -Contain 'Build succeeded.'
        $result.HasErrors() | Should -BeFalse
        $result.UploadedDebugFiles() | Sort-Object -Unique | Should -Be @(
            'Sentry.Bindings.Cocoa.pdb',
            'Sentry.Extensions.Logging.pdb',
            'Sentry.Maui.pdb',
            'Sentry.pdb',
            'Sentry.Samples.Maui.pdb',
            'Sentry.Samples.Maui.src.zip'
        )
    }
}
