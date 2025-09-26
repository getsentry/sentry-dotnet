# This file contains test cases for https://pester.dev/
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. $PSScriptRoot/common.ps1

$HasMSBuild = (Get-Command msbuild -ErrorAction SilentlyContinue)

Describe 'MSBuild app (<framework>)' -ForEach @(
    @{ framework = 'net5.0' },
    @{ framework = 'net8.0' },
    @{ framework = 'net9.0' }
) -Skip:(-not $IsWindows -or -not $HasMSBuild) {
    BeforeAll {
        $path = './msbuild-app'
        DotnetNew 'console' $path $framework
        @'
using Sentry;

SentrySdk.Init(options =>
{
    options.Dsn = args[0];
    options.Debug = true;
});

SentrySdk.CaptureMessage("Hello from MSBuild app");
'@ | Out-File $path/Program.cs

        function Test-NetSdkInstalled([string]$framework) {
            $version = $framework -replace 'net(\d+)\.0', '$1'
            $sdks = dotnet --list-sdks
            return $null -ne ($sdks | Where-Object { $_ -match "^$version\." })
        }

        Push-Location $path
    }

    BeforeEach {
        Remove-Item "./bin/Release/$framework" -Recurse -ErrorAction SilentlyContinue
        Remove-Item "./obj/Release/$framework" -Recurse -ErrorAction SilentlyContinue
    }

    AfterAll {
        Pop-Location
    }

    It 'builds without warnings and is able to capture a message' {
        if (-not (Test-NetSdkInstalled $framework)) {
            Set-ItResult -Skipped -Because "$framework is not installed"
        }

        $result = Invoke-SentryServer {
            Param([string]$url)
            $dsn = $url.Replace('http://', 'http://key@') + '/0'

            msbuild msbuild-app.csproj -t:Restore,Build -p:Configuration=Release -p:TreatWarningsAsErrors=true
            | ForEach-Object { Write-Host $_ }
            $LASTEXITCODE | Should -Be 0

            msbuild msbuild-app.csproj -t:Run -p:Configuration=Release -p:RunArguments=$dsn
            | ForEach-Object { Write-Host $_ }
            $LASTEXITCODE | Should -Be 0
        }
        $result.HasErrors() | Should -BeFalse
        $result.Envelopes() | Should -AnyElementMatch "`"message`":`"Hello from MSBuild app`""
    }
}
