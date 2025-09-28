# This file contains test cases for https://pester.dev/
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. $PSScriptRoot/common.ps1

Describe 'MSBuild app (<framework>)' -ForEach @(
    @{ framework = 'net5.0'; sdk = @{ version = '5.0.400' }; config = "$PSScriptRoot\nuget5.config" }
    @{ framework = 'net8.0'; sdk = @{ version = '8.0.400' } },
    @{ framework = 'net9.0' }
) {
    BeforeAll {
        Write-Host "::group::Create test project"
        DotnetNew 'console' 'msbuild-app' $framework
        Write-Host "::endgroup::"
        Push-Location msbuild-app
        @'
using Sentry;

SentrySdk.Init(options =>
{
    options.Dsn = args[0];
    options.Debug = true;
});

SentrySdk.CaptureMessage("Hello from MSBuild app");
'@ | Out-File Program.cs

        Write-Host "::group::Setup .NET SDK"
        if (Test-Path variable:sdk)
        {
            @"
{
    "sdk": {
            "version": "$($sdk.version)",
            "rollForward": "latestFeature"
    }
}
"@ | Out-File global.json
        }

        dotnet --version | ForEach-Object { Write-Host $_ }
        dotnet msbuild -version | ForEach-Object { Write-Host $_ }
        Write-Host "::endgroup::"
    }

    AfterAll {
        Pop-Location
        Remove-Item msbuild-app -Recurse -Force -ErrorAction SilentlyContinue
    }

    It 'builds without warnings and is able to capture a message' {
        if ($IsMacOS -and $framework -eq 'net5.0' -and "Arm64".Equals([System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture.ToString()))
        {
            Set-ItResult -Skipped -Because ".NET 5.0 SDK does not support ARM64 on macOS"
            return
        }

        if (!(Test-Path variable:config))
        {
            $config = "$PSScriptRoot/nuget.config"
        }
        Write-Host "::group::Restore packages"
        dotnet restore msbuild-app.csproj --configfile $config | ForEach-Object { Write-Host $_ }
        $LASTEXITCODE | Should -Be 0
        Write-Host "::endgroup::"

        Write-Host "::group::Build app"
        # TODO: pass -p:TreatWarningsAsErrors=true after #4554 is fixed
        dotnet msbuild msbuild-app.csproj -t:Build -p:Configuration=Release -p:TreatWarningsAsErrors=false | ForEach-Object { Write-Host $_ }
        $LASTEXITCODE | Should -Be 0
        Write-Host "::endgroup::"

        Write-Host "::group::Test app"
        $result = Invoke-SentryServer {
            param([string]$url)
            $dsn = $url.Replace('http://', 'http://key@') + '/0'
            dotnet msbuild msbuild-app.csproj -t:Run -p:Configuration=Release -p:RunArguments=$dsn | ForEach-Object { Write-Host $_ }
            $LASTEXITCODE | Should -Be 0
        }
        Write-Host "::endgroup::"
        $result.HasErrors() | Should -BeFalse
        $result.Envelopes() | Should -AnyElementMatch "`"message`":`"Hello from MSBuild app`""
    }
}
