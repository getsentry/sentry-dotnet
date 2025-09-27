# This file contains test cases for https://pester.dev/
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. $PSScriptRoot/common.ps1

Describe 'MSBuild app (<framework>)' -ForEach @(
    @{ framework = 'net5.0'; sdk = '5.0.400' },
    @{ framework = 'net8.0'; sdk = '8.0.400' },
    @{ framework = 'net9.0'; sdk = '9.0.300' }
) -Skip:(-not $IsWindows) {
    BeforeAll {
        $tempDir = Join-Path ([System.IO.Path]::GetTempPath()) ([System.IO.Path]::GetRandomFileName())
        New-Item -ItemType Directory -Path $tempDir | Out-Null
        Push-Location $tempDir
        @"
{
        "sdk": {
                "version": "$sdk",
                "rollForward": "latestFeature"
        }
}
"@ | Out-File global.json

        dotnet --version | ForEach-Object { Write-Host $_ }
        dotnet msbuild -version | ForEach-Object { Write-Host $_ }
        $hasDotnetSdk = $LASTEXITCODE -eq 0

        if ($hasDotnetSdk) {
            DotnetNew 'console' $tempDir/msbuild-app $framework
            Set-Location $tempDir/msbuild-app
        @'
using Sentry;

SentrySdk.Init(options =>
{
    options.Dsn = args[0];
    options.Debug = true;
});

SentrySdk.CaptureMessage("Hello from MSBuild app");
'@ | Out-File Program.cs
        }
    }

    AfterAll {
        Pop-Location
        Remove-Item -Recurse -Force $tempDir
    }

    It 'builds without warnings and is able to capture a message' {
        if (-not $hasDotnetSdk) {
            Set-ItResult -Skipped -Because "$framework is not installed"
        }
        # TODO: pass -p:TreatWarningsAsErrors=true after #4554 is fixed
        dotnet msbuild msbuild-app.csproj -t:Restore,Build -p:Configuration=Release -p:TreatWarningsAsErrors=false
        | ForEach-Object { Write-Host $_ }
        $LASTEXITCODE | Should -Be 0

        $result = Invoke-SentryServer {
            Param([string]$url)
            $dsn = $url.Replace('http://', 'http://key@') + '/0'
            dotnet msbuild msbuild-app.csproj -t:Run -p:Configuration=Release -p:RunArguments=$dsn
            | ForEach-Object { Write-Host $_ }
            $LASTEXITCODE | Should -Be 0
        }
        $result.HasErrors() | Should -BeFalse
        $result.Envelopes() | Should -AnyElementMatch "`"message`":`"Hello from MSBuild app`""
    }
}
