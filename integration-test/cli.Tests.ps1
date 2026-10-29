# This file contains test cases for https://pester.dev/
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. $PSScriptRoot/common.ps1

Describe 'Console apps (<framework>) - normal build' -ForEach @(
    foreach ($fw in $currentFrameworks) { @{ framework = $fw } }
) {
    BeforeAll {
        ResetLocalPackages
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

    It "uploads symbols when an earlier trimmed publish left a linked directory" {
        RunDotnetWithSentryCLI 'build' 'console-app' $False $False $framework | Out-Null
        $linked = "./console-app/obj/Release/$framework/linked"
        New-Item -ItemType Directory $linked -Force | Out-Null
        Copy-Item "./console-app/bin/Release/$framework/console-app.pdb" $linked
        try
        {
            $result = RunDotnetWithSentryCLI 'build' 'console-app' $True $False $framework
            $result.UploadedDebugFiles() | Sort-Object -Unique | Should -Be @('console-app.pdb')
        }
        finally
        {
            Remove-Item $linked -Recurse -Force
        }
    }
}

# The dummy Sentry server doesn't implement the releases API, so these tests record the CLI arguments instead.
Describe 'Console apps (<framework>) - release creation' -ForEach @(
    @{ framework = $latestFramework }
) {
    BeforeAll {
        ResetLocalPackages
        DotnetNew 'console' 'release-app' $framework
        (Get-Content release-app/release-app.csproj) -replace '</Project>', @'
  <PropertyGroup>
    <InformationalVersion>1.2.3+abcdef</InformationalVersion>
    <IncludeSourceRevisionInInformationalVersion>false</IncludeSourceRevisionInInformationalVersion>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.Configuration.UserSecrets" Version="10.0.0" />
  </ItemGroup>
</Project>
'@ | Set-Content release-app/release-app.csproj

        $cliLog = Join-Path $PSScriptRoot 'release-app' 'sentry-cli.log'
        if ($IsWindows)
        {
            $cliStub = Join-Path $PSScriptRoot 'release-app' 'sentry-cli.cmd'
            "@echo %* >> `"$cliLog`"" | Set-Content $cliStub
        }
        else
        {
            $cliStub = Join-Path $PSScriptRoot 'release-app' 'sentry-cli.sh'
            "#!/bin/sh`necho `"`$*`" >> `"$cliLog`"" | Set-Content $cliStub
            chmod +x $cliStub
        }

        function NewSentryCLIStubFailingInfo([string] $cliError)
        {
            if ($IsWindows)
            {
                $stub = Join-Path $PSScriptRoot 'release-app' 'sentry-cli-failing.cmd'
                "@echo %* >> `"$cliLog`"`n@if `"%1`"==`"info`" (echo $cliError 1>&2 & exit 1)" | Set-Content $stub
            }
            else
            {
                $stub = Join-Path $PSScriptRoot 'release-app' 'sentry-cli-failing.sh'
                "#!/bin/sh`necho `"`$*`" >> `"$cliLog`"`nif [ `"`$1`" = info ]; then echo '$cliError' >&2; exit 1; fi" | Set-Content $stub
                chmod +x $stub
            }
            $stub
        }

        function BuildWithSentryCLIStub([string[]] $properties, [string] $extraSource, [string] $stub = $cliStub)
        {
            Remove-Item $cliLog -ErrorAction SilentlyContinue
            $extraSourcePath = Join-Path $PSScriptRoot 'release-app' 'Extra.cs'
            if ($extraSource)
            {
                $extraSource | Set-Content $extraSourcePath
            }
            # Not /p:SentryCLI, because the targets can't clear a global property when the CLI isn't configured
            $env:SentryCLI = $stub
            try
            {
                $output = dotnet build release-app -c Release --nologo --framework $framework @properties | ForEach-Object { Write-Host "  $_"; $_ }
                $LASTEXITCODE | Should -Be 0
            }
            finally
            {
                Remove-Item env:SentryCLI
                Remove-Item $extraSourcePath -ErrorAction SilentlyContinue
            }
            [pscustomobject]@{
                Output   = $output
                CliCalls = @(Get-Content $cliLog -ErrorAction SilentlyContinue)
            }
        }
    }

    It "creates a release named after the informational version" {
        (BuildWithSentryCLIStub '/p:SentryCreateRelease=true').CliCalls |
            Should -AnyElementMatch '^releases new release-app@1\.2\.3\+abcdef\s*$'
    }

    It "creates a release when MSBuild can't load the type of another assembly attribute" {
        (BuildWithSentryCLIStub '/p:SentryCreateRelease=true', '/p:UserSecretsId=sentry-test').CliCalls |
            Should -AnyElementMatch '^releases new release-app@1\.2\.3\+abcdef\s*$'
    }

    It "creates a release named after the assembly version when there's no informational version" {
        $result = BuildWithSentryCLIStub '/p:SentryCreateRelease=true', '/p:GenerateAssemblyInformationalVersionAttribute=false', '/p:FileVersion=7.7.7.7'
        $result.CliCalls | Should -AnyElementMatch '^releases new release-app@1\.0\.0\.0\s*$'
        $result.Output | Should -Not -AnyElementMatch 'may report .* at runtime'
    }

    It "creates a release named after a hand-written informational version that matches the file version" {
        $result = BuildWithSentryCLIStub @('/p:SentryCreateRelease=true', '/p:GenerateAssemblyInformationalVersionAttribute=false', '/p:FileVersion=7.7.7.7') `
            -extraSource '[assembly: System.Reflection.AssemblyInformationalVersion("7.7.7.7")]'
        $result.CliCalls | Should -AnyElementMatch '^releases new release-app@7\.7\.7\.7\s*$'
        if ($IsWindows)
        {
            $result.Output | Should -AnyElementMatch 'may report 1\.0\.0\.0 at runtime'
        }
    }

    It "warns on Windows when the app reads an informational version it doesn't have" {
        $result = BuildWithSentryCLIStub @('/p:SentryCreateRelease=true', '/p:GenerateAssemblyInformationalVersionAttribute=false', '/p:FileVersion=7.7.7.7') `
            -extraSource 'public static class AppVersion { public static string? Get() => System.Reflection.CustomAttributeExtensions.GetCustomAttribute<System.Reflection.AssemblyInformationalVersionAttribute>(typeof(AppVersion).Assembly)?.InformationalVersion; }'
        if ($IsWindows)
        {
            $result.CliCalls | Should -AnyElementMatch '^releases new release-app@7\.7\.7\.7\s*$'
            $result.Output | Should -AnyElementMatch 'may report 1\.0\.0\.0 at runtime'
        }
        else
        {
            $result.CliCalls | Should -AnyElementMatch '^releases new release-app@1\.0\.0\.0\s*$'
        }
    }

    It "creates a release named after an informational version that matches the file version" {
        (BuildWithSentryCLIStub '/p:SentryCreateRelease=true', '/p:InformationalVersion=7.7.7.7', '/p:FileVersion=7.7.7.7').CliCalls |
            Should -AnyElementMatch '^releases new release-app@7\.7\.7\.7\s*$'
    }

    It "creates a release and sets commits when only SentrySetCommits is enabled" {
        $calls = (BuildWithSentryCLIStub '/p:SentrySetCommits=true').CliCalls
        $calls | Should -AnyElementMatch '^releases new release-app@1\.2\.3\+abcdef\s*$'
        $calls | Should -AnyElementMatch '^releases set-commits --auto release-app@1\.2\.3\+abcdef\s*$'
    }

    It "skips uploads and release creation without failing the build when <reason>" -ForEach @(
        @{ reason = 'there is no auth token'; cliError = 'error: Auth token is required for this request.'; expected = 'The Sentry CLI is not fully configured' }
        @{ reason = 'the Sentry API request fails'; cliError = 'error: API request failed'; expected = 'warning : Sentry API request failed' }
    ) {
        $result = BuildWithSentryCLIStub '/p:SentryCreateRelease=true', '/p:SentryUploadSymbols=true' -stub (NewSentryCLIStubFailingInfo $cliError)
        $result.Output | Should -AnyElementMatch $expected
        $result.CliCalls | Should -HaveCount 1
        $result.CliCalls | Should -AnyElementMatch '^info\b'
    }
}

Describe 'Console apps (<framework>) - native AOT publish' -ForEach @(
    foreach ($fw in $currentFrameworks) { @{ framework = $fw } }
) {
    BeforeAll {
        ResetLocalPackages
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
