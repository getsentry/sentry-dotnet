# This file contains test cases for https://pester.dev/
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. $PSScriptRoot/common.ps1

$IsARM64 = "Arm64".Equals([System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture.ToString())

# NOTE: These .NET versions are used to build a test app that consumes the Sentry
# .NET SDK, and are not tied to the .NET version used to build the SDK itself.
Describe 'MSBuild app' {
    BeforeDiscovery {
        $frameworks = @()

        # .NET 5.0 does not support ARM64 on macOS
        if (-not $IsMacOS -or -not $IsARM64)
        {
            $frameworks += @{
                framework = 'net5.0'
                sdk       = '5.0.400'
                # NuGet 5 does not support packageSourceMapping
                config    = "$PSScriptRoot\nuget5.config"
            }
        }

        $frameworks += @(
            @{ framework = 'net8.0'; sdk = '8.0.400' },
            @{ framework = 'net9.0'; sdk = '9.0.300' }
        )
    }

    Context '(<framework>)' -ForEach $frameworks {
        BeforeEach {
            Write-Host "::group::Create msbuild-app"
            dotnet new console --no-restore --output msbuild-app --framework $framework | ForEach-Object { Write-Host $_ }
            $LASTEXITCODE | Should -Be 0
            AddPackageReference msbuild-app Sentry
            Push-Location msbuild-app
            @'
using System.Runtime.InteropServices;
using Sentry;

SentrySdk.Init(options =>
{
    options.Dsn = args[0];
    options.Debug = true;
});

SentrySdk.CaptureMessage($"Hello from MSBuild app");
'@ | Out-File Program.cs
            Write-Host "::endgroup::"

            Write-Host "::group::Setup .NET SDK"
            if (Test-Path variable:sdk)
            {
                # Pin to a specific SDK version to use MSBuild from that version
                @"
{
    "sdk": {
            "version": "$sdk",
            "rollForward": "latestFeature"
    }
}
"@ | Out-File global.json
            }
            Write-Host "Using .NET SDK: $(dotnet --version)"
            Write-Host "Using MSBuild version: $(dotnet msbuild -version)"
            Write-Host "::endgroup::"
        }

        AfterEach {
            Pop-Location
            Remove-Item msbuild-app -Recurse -Force -ErrorAction SilentlyContinue
        }

        It 'builds without warnings and is able to capture a message' {
            Write-Host "::group::Restore packages"
            if (!(Test-Path variable:config))
            {
                $config = "$PSScriptRoot/nuget.config"
            }
            dotnet restore msbuild-app.csproj --configfile $config /p:CheckEolTargetFramework=false | ForEach-Object { Write-Host $_ }
            $LASTEXITCODE | Should -Be 0
            Write-Host "::endgroup::"

            Write-Host "::group::Build msbuild-app"
            # TODO: pass -p:TreatWarningsAsErrors=true after #4554 is fixed
            dotnet msbuild msbuild-app.csproj -t:Build -p:Configuration=Release -p:TreatWarningsAsErrors=false | ForEach-Object { Write-Host $_ }
            $LASTEXITCODE | Should -Be 0
            Write-Host "::endgroup::"

            Write-Host "::group::Run msbuild-app"
            $result = Invoke-SentryServer {
                param([string]$url)
                $dsn = $url.Replace('http://', 'http://key@') + '/0'
                dotnet msbuild msbuild-app.csproj -t:Run -p:Configuration=Release -p:RunArguments=$dsn | ForEach-Object { Write-Host $_ }
                $LASTEXITCODE | Should -Be 0
            }
            $result.HasErrors() | Should -BeFalse
            $result.Envelopes() | Should -AnyElementMatch "`"message`":`"Hello from MSBuild app`""
            Write-Host "::endgroup::"
        }
    }
}
