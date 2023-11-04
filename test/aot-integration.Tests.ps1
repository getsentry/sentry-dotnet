# This file contains test cases for https://pester.dev/
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

BeforeAll {
    $env:SENTRY_LOG_LEVEL = 'debug';
    . $PSScriptRoot/test-functions.ps1
    function GetSentryPackageVersion()
    {
        $packageVersion = (Select-Xml -Path "$PSScriptRoot/../Directory.Build.props" -XPath "/Project/PropertyGroup/Version").Node.InnerText
        $packageVersion += '-test' # so we can be sure it won't be fetched from nuget.org
        $packageVersion
    }

    $packageVersion = GetSentryPackageVersion
    $packagePath = "src/Sentry/bin/Release/Sentry.$packageVersion.nupkg"
    if (-not (Test-Path env:CI) -and -not (Test-Path $packagePath))
    {
        Write-Host "Package not found at $packagePath, running dotnet pack"
        dotnet pack src/Sentry -c Release --nologo -p:Version=$packageVersion | ForEach-Object { Write-Host $_ }
        if ($LASTEXITCODE -ne 0)
        {
            throw "Failed to package sentry."
        }
    }
    Write-Host "Using package $packagePath - $((Get-Item $packagePath).Length) bytes"
    Remove-Item -Path ./temp/packages -Recurse -Force -ErrorAction SilentlyContinue

    nuget add $packagePath -source ./temp/packages | ForEach-Object { Write-Host $_ }
    if ($LASTEXITCODE -ne 0)
    {
        throw "Failed to add package sentry to a local nuget source."
    }

    # We need to remove the package from cache or it won't re resolved properly
    Remove-Item -Path ~/.nuget/packages/sentry/$packageVersion -Recurse -Force -ErrorAction SilentlyContinue
}

Describe 'Console app (<framework>)' -ForEach @(
    # @{ framework = "net7.0" },
    @{ framework = "net8.0" }
) {
    BeforeAll {
        $path = './temp/console-app'
        Remove-Item -Path $path -Recurse -Force
        dotnet new console --framework $framework --output $path
        if ($LASTEXITCODE -ne 0)
        {
            throw "Failed to create the test app from template."
        }
        @"
using Sentry;

// Initialize the Sentry SDK.  (It is not necessary to dispose it.)
SentrySdk.Init(options =>
{
    options.Dsn = Environment.GetCommandLineArgs()[1];
    options.Debug = true;
});

throw new ApplicationException("Something happened!");
"@ | Out-File $path/Program.cs
        @"
<Project>
  <PropertyGroup>
    <PublishAot>true</PublishAot>
  </PropertyGroup>
</Project>
"@ | Out-File $path/Directory.build.props
        Push-Location $path
        try
        {
            $packageVersion = GetSentryPackageVersion
            dotnet add package sentry --source integraton-test --version $packageVersion | ForEach-Object { Write-Host $_ }
            if ($LASTEXITCODE -ne 0)
            {
                throw "Failed to add package dependency to the test app project."
            }
        }
        finally
        {
            Pop-Location
        }

        # Publish once, then test the executable in following functions
        RunDotnet 'publish' 'temp/console-app' $True $True $framework
    }

    BeforeEach {
    }

    # TODO migrate CLI integration tests to run on blank template app, such as the following code.
    # As is, this does an additional publish which we don't need to run app-specific integration tests.
    # It "uploads symbols and sources during build" {
    #     $result = RunDotnet 'publish' 'temp/console-app' $True $True $framework
    #     $result.UploadedDebugFiles() | Sort-Object -Unique | Should -Be @('console-app.pdb')
    #     $result.ScriptOutput | Should -AnyElementMatch 'Found 1 debug information file'
    #     $result.ScriptOutput | Should -AnyElementMatch 'Resolved source code for 1 debug information file'
    # }

    It "sends debug images" {
        $result = Invoke-SentryServer {
            Param([string]$url)
            if ($IsMacOS)
            {
                $path = './temp/console-app/bin/Release/net8.0/osx-x64/publish/console-app'
            }
            elseif ($IsWindows)
            {
                $path = './temp/console-app/bin/Release/net8.0/win-x64/publish/console-app.exe'
            }
            else
            {
                $path = './temp/console-app/bin/Release/net8.0/linux-x64/publish/console-app'
            }

            $dsn = $url -replace 'http://', 'http://publickey@'
            $dsn += '/123' # project ID
            Write-Host "::group::Executing $path $url"
            try
            {
                &$path $url | ForEach-Object {
                    Write-Host "  $_"
                    $_
                }
            }
            finally
            {
                Write-Host "::endgroup::"
            }
        }
    }

}
