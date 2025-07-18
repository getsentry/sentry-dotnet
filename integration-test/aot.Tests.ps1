# This file contains test cases for https://pester.dev/
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Describe 'Publish' {
    BeforeAll {
        $package = Get-ChildItem -Path "$PSScriptRoot/../src/Sentry/bin/Release/Sentry.*.nupkg" -File | Select-Object -First 1
        if (-not $package)
        {
            throw "No NuGet package found in src/Sentry/bin/Release."
        }

        $tempDir = Resolve-Path ([System.IO.Path]::GetTempPath())
        $tempDir = Join-Path $tempDir ([System.IO.Path]::GetRandomFileName())
        New-Item -ItemType Directory -Path $tempDir -Force | Out-Null
        Set-Location $tempDir
        Write-Host "Testing $package in $tempDir"

        Write-Host "::group::Setup local NuGet source"
        $localPackages = Join-Path $tempDir "packages"
        New-Item -ItemType Directory -Path $localPackages -Force | Out-Null
        Copy-Item $package $localPackages
        $localConfig = Join-Path $tempDir "nuget.config"
        Copy-Item $PSScriptRoot/nuget.config $localConfig
        (Get-Content $localConfig) -replace '\./packages', $localPackages | Set-Content $localConfig
        $env:NUGET_PACKAGES = Join-Path $tempDir "nuget"
        New-Item -ItemType Directory -Path $env:NUGET_PACKAGES -Force | Out-Null
        dotnet nuget list source | Write-Host
        Write-Host "::endgroup::"

        Write-Host "::group::Create test project"
        dotnet new console --aot --name hello-sentry --output . | Write-Host
        dotnet add package Sentry --prerelease --source $localPackages | Write-Host
        @"
SentrySdk.Init(options =>
{
    options.Dsn = "https://foo@sentry.invalid/42";
    options.Debug = true;
});
Console.WriteLine("Hello, Sentry!");
"@ | Set-Content Program.cs
        Write-Host "::endgroup::"
    }

    AfterAll {
        if ($tempDir -and (Test-Path $tempDir))
        {
            Remove-Item $tempDir -Recurse -Force -ErrorAction SilentlyContinue
        }
    }

    It 'Aot' {
        $rid = $env:RuntimeIdentifier
        if ($rid)
        {
            dotnet publish -c Release -r $rid | Write-Host
        }
        else
        {
            dotnet publish -c Release | Write-Host
        }
        $LASTEXITCODE | Should -Be 0

        $tfm = (Get-ChildItem -Path "bin/Release" -Directory | Select-Object -First 1).Name
        if (-not $rid)
        {
            $rid = (Get-ChildItem -Path "bin/Release/$tfm" -Directory | Select-Object -First 1).Name
        }
        & "bin/Release/$tfm/$rid/publish/hello-sentry" | Write-Host
        $LASTEXITCODE | Should -Be 0
    }

    It 'Container' -Skip:(!$IsLinux -or !(Get-Command docker -ErrorAction SilentlyContinue)) {
        dotnet publish -p:EnableSdkContainerSupport=true -t:PublishContainer | Write-Host
        $LASTEXITCODE | Should -Be 0

        docker run --rm hello-sentry | Write-Host
        $LASTEXITCODE | Should -Be 0
    }
}
